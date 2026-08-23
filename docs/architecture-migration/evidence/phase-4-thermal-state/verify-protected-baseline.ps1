#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Todo 1 (phase-4-thermal-state): symmetric protected-baseline verifier.

.DESCRIPTION
  Parameters (exact): -Baseline <path> -AllowedHunks <path> -EvidenceRoot <path> -Output <path>

  -Baseline accepts EITHER
    a) baseline-manifest.json  ({"files":[{"path","sha256"}...]}) -> HASH mode:
       recomputes the current tracked/untracked universe and performs a fully
       symmetric comparison (deleted-from-baseline AND added-since-baseline,
       plus content modifications), or
    b) baseline-git-status.bin (raw `git status --porcelain=v1 -z --branch`)
       -> PATHSET mode: symmetric diff of the dirty-path sets.

  Baseline row validation (fail-closed, exit 2): missing file, malformed rows
  (empty path, control chars incl. NUL, rooted/absolute paths, '.'/'..' or
  empty segments, bad sha256), duplicate paths, and any path resolving outside
  the verified repository root.

  Drift classification: ALLOWED when under -EvidenceRoot, under generated dirs
  (src/bin, src/obj, tests/SnowMeltingCalculator.Tests/bin|obj), any path with
  a TestResults segment, or listed in the AllowedHunks manifest; everything
  else is PROTECTED MISMATCH.

  Output JSON contains exactly: protected_mismatch_count, allowed_hunk_count,
  changed_paths[{path, classification, kind}]. Exit 0 iff mismatch count == 0;
  exit 3 otherwise. allowed_hunk_count counts only drift admitted via the
  AllowedHunks manifest.
#>
param(
    [Parameter(Mandatory = $true)][string]$Baseline,
    [Parameter(Mandatory = $true)][string]$AllowedHunks,
    [Parameter(Mandatory = $true)][string]$EvidenceRoot,
    [Parameter(Mandatory = $true)][string]$Output
)

$ErrorActionPreference = 'Continue'
Set-StrictMode -Version Latest

$exitInvalidInput = 2
$exitProtectedMismatch = 3
$exitInternal = 4

function Write-Utf8NoBom {
    param([string]$Path, [string]$Text)
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function ConvertTo-JsonString {
    param([string]$Value)
    $sb = [System.Text.StringBuilder]::new('"')
    foreach ($ch in $Value.ToCharArray()) {
        $code = [int]$ch
        # NOTE: if/elseif chain - `continue` inside a scalar PowerShell switch
        # falls through to post-switch code, corrupting escaped characters.
        if ($ch -eq '"') { [void]$sb.Append('\"') }
        elseif ($ch -eq '\') { [void]$sb.Append('\\') }
        elseif ($ch -eq "`b") { [void]$sb.Append('\b') }
        elseif ($ch -eq "`f") { [void]$sb.Append('\f') }
        elseif ($ch -eq "`n") { [void]$sb.Append('\n') }
        elseif ($ch -eq "`r") { [void]$sb.Append('\r') }
        elseif ($ch -eq "`t") { [void]$sb.Append('\t') }
        elseif ($code -lt 0x20) { [void]$sb.Append(('\u{0:x4}' -f $code)) }
        else { [void]$sb.Append($ch) }
    }
    [void]$sb.Append('"')
    return $sb.ToString()
}

function Invoke-GitRaw {
    param([string[]]$GitArgs, [string]$WorkingDir)
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = 'git'
    foreach ($a in $GitArgs) { [void]$psi.ArgumentList.Add($a) }
    $psi.WorkingDirectory = $WorkingDir
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $p = [System.Diagnostics.Process]::Start($psi)
    $ms = [System.IO.MemoryStream]::new()
    $p.StandardOutput.BaseStream.CopyTo($ms)
    $errTask = $p.StandardError.ReadToEndAsync()
    $p.WaitForExit()
    if ($p.ExitCode -ne 0) {
        throw "git $($GitArgs -join ' ') failed with exit code $($p.ExitCode): $($errTask.Result.Trim())"
    }
    return , $ms.ToArray()
}

function Split-NulStream {
    param([byte[]]$Bytes)
    $text = [System.Text.Encoding]::UTF8.GetString($Bytes)
    return $text.Split([char]0)
}

function Parse-StatusZPaths {
    # Returns pscustomobject { BranchLine, Entries } where each entry has X,Y,Path,OrigPath.
    param([byte[]]$Bytes)
    $segments = Split-NulStream $Bytes
    $branchLine = ''
    $entries = [System.Collections.Generic.List[object]]::new()
    $i = 0
    while ($i -lt $segments.Length) {
        $seg = $segments[$i]
        $i++
        if ($seg.Length -eq 0) { continue }
        if ($seg.StartsWith('##')) { $branchLine = $seg; continue }
        if ($seg.Length -lt 4) { throw "malformed porcelain segment: '$seg'" }
        $x = $seg[0]; $y = $seg[1]
        if ($seg[2] -ne ' ') { throw "malformed porcelain segment: '$seg'" }
        $path = $seg.Substring(3)
        $origPath = $null
        if ($x -in @('R', 'C') -or $y -in @('R', 'C')) {
            if ($i -ge $segments.Length) { throw "rename entry missing origPath" }
            $origPath = $segments[$i]
            $i++
        }
        $entries.Add([pscustomobject]@{ X = [string]$x; Y = [string]$y; Path = $path; OrigPath = $origPath })
    }
    return [pscustomobject]@{ BranchLine = $branchLine; Entries = $entries }
}

# --------------------------------------------------------------- resolve root
try {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $rootBytes = Invoke-GitRaw -GitArgs @('rev-parse', '--show-toplevel') -WorkingDir $scriptDir
    # Normalize to Windows separators so prefix comparisons match GetFullPath output.
    $repoRoot = [System.Text.Encoding]::UTF8.GetString($rootBytes).Trim().TrimEnd('/', '\').Replace('/', '\')

    function Get-FullUnderRoot {
        param([string]$RelFwdSlash)
        return [System.IO.Path]::GetFullPath((Join-Path $repoRoot ($RelFwdSlash.Replace('/', '\'))))
    }

    # ------------------------------------------------------------ allowed hunks
    if (-not (Test-Path -LiteralPath $AllowedHunks -PathType Leaf)) {
        Write-Error "AllowedHunks manifest not found: $AllowedHunks"
        exit $exitInvalidInput
    }
    try {
        $hunksJson = Get-Content -LiteralPath $AllowedHunks -Raw | ConvertFrom-Json
    }
    catch {
        Write-Error "AllowedHunks manifest is not valid JSON: $($_.Exception.Message)"
        exit $exitInvalidInput
    }
    $hunkSet = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    if ($null -ne $hunksJson.allowedHunks) {
        foreach ($h in @($hunksJson.allowedHunks)) {
            if ($null -eq $h -or $h -isnot [string]) {
                Write-Error "AllowedHunks contains a non-string entry"
                exit $exitInvalidInput
            }
            [void]$hunkSet.Add(($h.Replace('\', '/')))
        }
    }

    # ------------------------------------------------------- baseline detection
    if (-not (Test-Path -LiteralPath $Baseline -PathType Leaf)) {
        Write-Error "Baseline not found: $Baseline"
        exit $exitInvalidInput
    }
    $baselineBytes = [System.IO.File]::ReadAllBytes($Baseline)
    $isStatusBin = ($baselineBytes.Length -ge 2 -and $baselineBytes[0] -eq 0x23 -and $baselineBytes[1] -eq 0x23)

    $baselineHashes = [System.Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    $baselineDirty = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

    function Assert-BaselineRow {
        param([string]$RawPath, [int]$RowIndex)
        if ([string]::IsNullOrWhiteSpace($RawPath)) {
            Write-Error "baseline row ${RowIndex}: empty path"
            exit $exitInvalidInput
        }
        foreach ($ch in $RawPath.ToCharArray()) {
            if ([int]$ch -lt 0x20) {
                Write-Error ("baseline row ${RowIndex}: control character U+{0:X4} in path" -f [int]$ch)
                exit $exitInvalidInput
            }
        }
        $norm = $RawPath.Replace('\', '/')
        if ([System.IO.Path]::IsPathRooted($norm)) {
            Write-Error "baseline row ${RowIndex}: rooted path not allowed: $RawPath"
            exit $exitInvalidInput
        }
        foreach ($seg in $norm.Split('/')) {
            if ($seg -eq '' -or $seg -eq '.' -or $seg -eq '..') {
                Write-Error "baseline row ${RowIndex}: illegal path segment '$seg' in $RawPath"
                exit $exitInvalidInput
            }
        }
        $full = Get-FullUnderRoot $norm
        $rootPrefix = $repoRoot + [System.IO.Path]::DirectorySeparatorChar
        if (-not $full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            Write-Error "baseline row ${RowIndex}: path escapes repository root: $RawPath"
            exit $exitInvalidInput
        }
        return $norm
    }

    if ($isStatusBin) {
        $parsed = Parse-StatusZPaths $baselineBytes
        $idx = 0
        foreach ($e in $parsed.Entries) {
            $n = Assert-BaselineRow $e.Path $idx; $idx++
            [void]$baselineDirty.Add($n)
            if ($null -ne $e.OrigPath) {
                $n2 = Assert-BaselineRow $e.OrigPath $idx; $idx++
                [void]$baselineDirty.Add($n2)
            }
        }
    }
    else {
        $baselineText = [System.Text.Encoding]::UTF8.GetString($baselineBytes)
        try {
            $manifest = $baselineText | ConvertFrom-Json
        }
        catch {
            Write-Error "Baseline JSON is not valid: $($_.Exception.Message)"
            exit $exitInvalidInput
        }
        if ($null -eq $manifest.files -or @($manifest.files).Count -eq 0) {
            Write-Error "Baseline manifest has no files array"
            exit $exitInvalidInput
        }
        $idx = 0
        foreach ($row in @($manifest.files)) {
            if ($null -eq $row.path -or $row.path -isnot [string]) {
                Write-Error "baseline row ${idx}: missing/malformed path"
                exit $exitInvalidInput
            }
            if ($null -eq $row.sha256 -or $row.sha256 -isnot [string] -or $row.sha256 -notmatch '^[0-9a-fA-F]{64}$') {
                Write-Error "baseline row ${idx}: missing/malformed sha256 for '$($row.path)'"
                exit $exitInvalidInput
            }
            $norm = Assert-BaselineRow $row.path $idx
            if ($baselineHashes.ContainsKey($norm)) {
                Write-Error "baseline row ${idx}: duplicate path '$norm'"
                exit $exitInvalidInput
            }
            $baselineHashes[$norm] = $row.sha256.ToUpperInvariant()
            $idx++
        }
    }

    # ------------------------------------------------------ recompute current state
    # NOTE: `git ls-files` is scoped to the current directory and returns
    # cwd-relative paths; run it from the verified repository ROOT.
    $lsBytes = Invoke-GitRaw -GitArgs @('ls-files', '-z') -WorkingDir $repoRoot
    $tracked = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($p in (Split-NulStream $lsBytes)) {
        if ($p.Length -gt 0) { [void]$tracked.Add($p.Replace('\', '/')) }
    }
    $statusNow = Parse-StatusZPaths (
        Invoke-GitRaw -GitArgs @('status', '--porcelain=v1', '-z', '--branch') -WorkingDir $scriptDir)
    $currentUntracked = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $currentDirty = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($e in $statusNow.Entries) {
        if ($e.X -eq '?' -and $e.Y -eq '?') {
            [void]$currentUntracked.Add($e.Path.Replace('\', '/'))
            [void]$currentDirty.Add($e.Path.Replace('\', '/'))
            continue
        }
        [void]$currentDirty.Add($e.Path.Replace('\', '/'))
        if ($null -ne $e.OrigPath) { [void]$currentDirty.Add($e.OrigPath.Replace('\', '/')) }
    }

    # ------------------------------------------------------------- symmetric drift
    $drift = [System.Collections.Generic.List[object]]::new() # {path, kind}
    if ($isStatusBin) {
        foreach ($p in $baselineDirty) {
            if (-not $currentDirty.Contains($p)) { $drift.Add([pscustomobject]@{ Path = $p; Kind = 'deleted' }) }
        }
        foreach ($p in $currentDirty) {
            if (-not $baselineDirty.Contains($p)) { $drift.Add([pscustomobject]@{ Path = $p; Kind = 'added' }) }
        }
    }
    else {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            foreach ($rel in @($baselineHashes.Keys)) {
                $baselineHex = $baselineHashes[$rel]
                if (-not $tracked.Contains($rel)) {
                    $drift.Add([pscustomobject]@{ Path = $rel; Kind = 'deleted' })
                    continue
                }
                $abs = Get-FullUnderRoot $rel
                if (-not (Test-Path -LiteralPath $abs -PathType Leaf)) {
                    $drift.Add([pscustomobject]@{ Path = $rel; Kind = 'deleted' })
                    continue
                }
                $hex = ([System.BitConverter]::ToString($sha.ComputeHash([System.IO.File]::ReadAllBytes($abs)))).Replace('-', '')
                if ($hex -ne $baselineHex) {
                    $drift.Add([pscustomobject]@{ Path = $rel; Kind = 'modified' })
                }
            }
        }
        finally { $sha.Dispose() }
        foreach ($t in $tracked) {
            if (-not $baselineHashes.ContainsKey($t)) { $drift.Add([pscustomobject]@{ Path = $t; Kind = 'added' }) }
        }
        foreach ($u in $currentUntracked) {
            if (-not $baselineHashes.ContainsKey($u)) { $drift.Add([pscustomobject]@{ Path = $u; Kind = 'added' }) }
        }
    }

    # -------------------------------------------------------------- classification
    $eviNorm = $EvidenceRoot.Replace('\', '/').TrimEnd('/').ToLowerInvariant()
    $genDirs = @(
        'src/bin/', 'src/obj/',
        'tests/SnowMeltingCalculator.Tests/bin/', 'tests/SnowMeltingCalculator.Tests/obj/'
    )

    function Get-Classification {
        param([string]$RelPath)
        $norm = $RelPath.Replace('\', '/')
        $lower = $norm.ToLowerInvariant()
        if ($hunkSet.Contains($norm)) {
            return [pscustomobject]@{ Classification = 'allowed'; ByHunks = $true }
        }
        foreach ($g in $genDirs) {
            if ($lower.StartsWith($g)) { return [pscustomobject]@{ Classification = 'allowed'; ByHunks = $false } }
        }
        foreach ($seg in $norm.Split('/')) {
            if ($seg -eq 'TestResults') { return [pscustomobject]@{ Classification = 'allowed'; ByHunks = $false } }
        }
        if ($lower -eq $eviNorm -or $lower.StartsWith($eviNorm + '/')) {
            return [pscustomobject]@{ Classification = 'allowed'; ByHunks = $false }
        }
        return [pscustomobject]@{ Classification = 'protected-mismatch'; ByHunks = $false }
    }

    $sortedDrift = $drift.ToArray()
    [Array]::Sort($sortedDrift, [Comparison[object]] {
            param($a, $b)
            [StringComparer]::Ordinal.Compare([string]$a.Path, [string]$b.Path)
        })

    $mismatchCount = 0
    $allowedByHunksCount = 0
    $cpSb = [System.Text.StringBuilder]::new()
    for ($j = 0; $j -lt $sortedDrift.Count; $j++) {
        $d = $sortedDrift[$j]
        $cls = Get-Classification $d.Path
        if ($cls.ByHunks) { $allowedByHunksCount++ }
        if ($cls.Classification -eq 'protected-mismatch') { $mismatchCount++ }
        $comma = if ($j -lt $sortedDrift.Count - 1) { ',' } else { '' }
        [void]$cpSb.AppendLine(('    {"path": ' + (ConvertTo-JsonString $d.Path) +
                ', "classification": "' + $cls.Classification +
                '", "kind": "' + $d.Kind + '"}' + $comma))
    }

    $ob = [System.Text.StringBuilder]::new()
    [void]$ob.AppendLine('{')
    [void]$ob.AppendLine(('  "protected_mismatch_count": ' + $mismatchCount + ','))
    [void]$ob.AppendLine(('  "allowed_hunk_count": ' + $allowedByHunksCount + ','))
    [void]$ob.AppendLine('  "changed_paths": [')
    [void]$ob.Append($cpSb.ToString())
    [void]$ob.AppendLine('  ]')
    [void]$ob.Append('}')
    Write-Utf8NoBom -Path $Output -Text $ob.ToString()

    Write-Host ("verify-protected-baseline: mode=" + $(if ($isStatusBin) { 'pathset' } else { 'hash' }) +
        " drift=" + $sortedDrift.Count +
        " protected_mismatch_count=" + $mismatchCount +
        " allowed_hunk_count=" + $allowedByHunksCount)

    if ($mismatchCount -gt 0) { exit $exitProtectedMismatch }
    exit 0
}
catch {
    Write-Error $_
    exit $exitInternal
}
