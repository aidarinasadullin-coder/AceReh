#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Todo 1 (phase-4-thermal-state): capture the protected baseline.

.DESCRIPTION
  Emits deterministic, NUL-safe artifacts into <evidence-root>\task-1:
    - baseline-git-status.bin   : raw bytes of `git status --porcelain=v1 -z --branch`
    - baseline-manifest.json    : SHA-256 manifest of every git-tracked file (sorted)
    - baseline-index-sets.json  : staged / unstaged / untracked NUL-safe sets
    - baseline-environment.json : git root/HEAD/branch/upstream, dotnet --info,
                                  node --version, pwsh version

  Determinism contract: two consecutive runs with no intervening repository
  change produce byte-identical artifacts. To keep the status capture stable
  across consecutive runs, this script deletes its own four output files
  BEFORE capturing status (they are untracked and would otherwise appear in
  the second capture). No timestamps are written into any artifact.

  All JSON is written UTF-8 WITHOUT BOM via [System.IO.File]::WriteAllText.
#>
param(
    [string]$OutputDir = ""
)

$ErrorActionPreference = 'Continue'
Set-StrictMode -Version Latest

$exitGitFailure = 10
$exitTrackedMissing = 11

function Write-Utf8NoBom {
    param([string]$Path, [string]$Text)
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-GitRaw {
    # Runs git and returns raw stdout bytes without any decoding.
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

function ConvertTo-JsonString {
    # Minimal deterministic JSON string encoder (control chars escaped, non-ASCII kept raw as UTF-8).
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

function Split-NulStream {
    # Decodes a NUL-delimited byte stream (UTF-8) into segments. NUL never occurs
    # inside a UTF-8 multi-byte sequence, so decode-then-split is byte-faithful.
    param([byte[]]$Bytes)
    $text = [System.Text.Encoding]::UTF8.GetString($Bytes)
    return $text.Split([char]0)
}

function Parse-StatusZ {
    # Parses `git status --porcelain=v1 -z --branch` segments into structured entries.
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
        $x = $seg[0]
        $y = $seg[1]
        if ($seg[2] -ne ' ') { throw "malformed porcelain segment: '$seg'" }
        $path = $seg.Substring(3)
        $origPath = $null
        if ($x -in @('R', 'C') -or $y -in @('R', 'C')) {
            if ($i -ge $segments.Length) { throw "rename entry missing origPath" }
            $origPath = $segments[$i]
            $i++
        }
        $entries.Add([pscustomobject]@{
                X        = [string]$x
                Y        = [string]$y
                Path     = $path
                OrigPath = $origPath
            })
    }
    return [pscustomobject]@{ BranchLine = $branchLine; Entries = $entries }
}

# ---------------------------------------------------------------- resolve root
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $scriptDir 'task-1'
}

    $rootBytes = Invoke-GitRaw -GitArgs @('rev-parse', '--show-toplevel') -WorkingDir $scriptDir
    # Normalize to Windows separators for consistent Join-Path/StartsWith behavior.
    $repoRoot = [System.Text.Encoding]::UTF8.GetString($rootBytes).Trim().TrimEnd('/', '\').Replace('/', '\')

try {
    # ------------------------------------------------ delete own prior outputs
    # Keeps consecutive runs byte-identical: our own untracked outputs would
    # otherwise appear in the second run's status capture.
    $ownOutputs = @('baseline-git-status.bin', 'baseline-manifest.json',
        'baseline-environment.json', 'baseline-index-sets.json')
    foreach ($name in $ownOutputs) {
        $p = Join-Path $OutputDir $name
        if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Force }
    }

    # ---------------------------------------------- capture status RAW BYTES
    # Nothing may be written into the repository between here and the capture.
    $statusBytes = Invoke-GitRaw -GitArgs @('status', '--porcelain=v1', '-z', '--branch') -WorkingDir $scriptDir

    if (-not (Test-Path -LiteralPath $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }
    [System.IO.File]::WriteAllBytes((Join-Path $OutputDir 'baseline-git-status.bin'), $statusBytes)

    # ------------------------------------------------------- index sets (NUL-safe)
    $status = Parse-StatusZ $statusBytes
    $staged = [System.Collections.Generic.List[string]]::new()
    $unstaged = [System.Collections.Generic.List[string]]::new()
    $untracked = [System.Collections.Generic.List[string]]::new()
    foreach ($e in $status.Entries) {
        if ($e.X -eq '?' -and $e.Y -eq '?') {
            $untracked.Add($e.Path)
            continue
        }
        if ($e.X -ne ' ' -and $e.X -ne '?') { $staged.Add($e.Path) }
        if ($e.Y -ne ' ' -and $e.Y -ne '?') { $unstaged.Add($e.Path) }
        if ($null -ne $e.OrigPath) { $untracked.Add($e.OrigPath) } # rename provenance preserved
    }
    $stagedArr = $staged.ToArray()
    [Array]::Sort($stagedArr, [StringComparer]::Ordinal)
    $unstagedArr = $unstaged.ToArray()
    [Array]::Sort($unstagedArr, [StringComparer]::Ordinal)
    $untrackedArr = $untracked.ToArray()
    [Array]::Sort($untrackedArr, [StringComparer]::Ordinal)

    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('{')
    [void]$sb.AppendLine(('  "branchLine": ' + (ConvertTo-JsonString $status.BranchLine) + ','))
    [void]$sb.AppendLine('  "staged": [')
    for ($j = 0; $j -lt $stagedArr.Count; $j++) {
        $comma = if ($j -lt $stagedArr.Count - 1) { ',' } else { '' }
        [void]$sb.AppendLine(('    ' + (ConvertTo-JsonString $stagedArr[$j]) + $comma))
    }
    [void]$sb.AppendLine('  ],')
    [void]$sb.AppendLine('  "unstaged": [')
    for ($j = 0; $j -lt $unstagedArr.Count; $j++) {
        $comma = if ($j -lt $unstagedArr.Count - 1) { ',' } else { '' }
        [void]$sb.AppendLine(('    ' + (ConvertTo-JsonString $unstagedArr[$j]) + $comma))
    }
    [void]$sb.AppendLine('  ],')
    [void]$sb.AppendLine('  "untracked": [')
    for ($j = 0; $j -lt $untrackedArr.Count; $j++) {
        $comma = if ($j -lt $untrackedArr.Count - 1) { ',' } else { '' }
        [void]$sb.AppendLine(('    ' + (ConvertTo-JsonString $untrackedArr[$j]) + $comma))
    }
    [void]$sb.AppendLine('  ]')
    [void]$sb.Append('}')
    Write-Utf8NoBom -Path (Join-Path $OutputDir 'baseline-index-sets.json') -Text $sb.ToString()

    # --------------------------------------------- tracked-file hash manifest
    # NOTE: `git ls-files` is scoped to the current directory and returns
    # cwd-relative paths; run it from the verified repository ROOT so paths
    # are complete and repository-relative.
    $lsBytes = Invoke-GitRaw -GitArgs @('ls-files', '-z') -WorkingDir $repoRoot
    $pathList = [System.Collections.Generic.List[string]]::new()
    foreach ($p in (Split-NulStream $lsBytes)) {
        if ($p.Length -gt 0) { $pathList.Add($p) }
    }
    $sorted = $pathList.ToArray()
    [Array]::Sort($sorted, [StringComparer]::Ordinal)

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $fileSb = [System.Text.StringBuilder]::new()
        for ($j = 0; $j -lt $sorted.Count; $j++) {
            $rel = $sorted[$j]
            $abs = Join-Path $repoRoot ($rel.Replace('/', '\'))
            if (-not (Test-Path -LiteralPath $abs -PathType Leaf)) {
                Write-Error "tracked file missing on disk: $rel"
                exit $exitTrackedMissing
            }
            $hash = $sha.ComputeHash([System.IO.File]::ReadAllBytes($abs))
            $hex = ([System.BitConverter]::ToString($hash)).Replace('-', '')
            $comma = if ($j -lt $sorted.Count - 1) { ',' } else { '' }
            [void]$fileSb.AppendLine(('    {"path": ' + (ConvertTo-JsonString $rel) + ', "sha256": "' + $hex + '"}' + $comma))
        }

        $mb = [System.Text.StringBuilder]::new()
        [void]$mb.AppendLine('{')
        [void]$mb.AppendLine('  "algorithm": "SHA-256",')
        [void]$mb.AppendLine('  "hashCase": "uppercase",')
        [void]$mb.AppendLine('  "pathStyle": "git-forward-slash-repository-relative",')
        [void]$mb.AppendLine(('  "fileCount": ' + $sorted.Count + ','))
        [void]$mb.AppendLine('  "files": [')
        [void]$mb.Append($fileSb.ToString())
        [void]$mb.AppendLine('  ]')
        [void]$mb.Append('}')
        Write-Utf8NoBom -Path (Join-Path $OutputDir 'baseline-manifest.json') -Text $mb.ToString()
    }
    finally {
        $sha.Dispose()
    }

    # ------------------------------------------------------------ environment
    $headBytes = Invoke-GitRaw -GitArgs @('rev-parse', 'HEAD') -WorkingDir $scriptDir
    $head = [System.Text.Encoding]::UTF8.GetString($headBytes).Trim()
    $branchBytes = Invoke-GitRaw -GitArgs @('branch', '--show-current') -WorkingDir $scriptDir
    $branch = [System.Text.Encoding]::UTF8.GetString($branchBytes).Trim()
    $upstream = ''
    try {
        $upstreamBytes = Invoke-GitRaw -GitArgs @('rev-parse', '--abbrev-ref', '--symbolic-full-name', '@{upstream}') -WorkingDir $scriptDir
        $upstream = [System.Text.Encoding]::UTF8.GetString($upstreamBytes).Trim()
    }
    catch { $upstream = '' }
    $dotnetInfo = (& dotnet --info 2>&1 | Out-String).Trim()
    $nodeVersion = (& node --version 2>&1 | Out-String).Trim()
    $pwshVersion = $PSVersionTable.PSVersion.ToString()

    $envPairs = @(
        @('gitRoot', (ConvertTo-JsonString ($repoRoot.Replace('\', '/')))),
        @('head', (ConvertTo-JsonString $head)),
        @('branch', (ConvertTo-JsonString $branch)),
        @('upstream', (ConvertTo-JsonString $upstream)),
        @('nodeVersion', (ConvertTo-JsonString $nodeVersion)),
        @('pwshVersion', (ConvertTo-JsonString $pwshVersion))
    )
    $eb = [System.Text.StringBuilder]::new()
    [void]$eb.AppendLine('{')
    foreach ($pair in $envPairs) {
        [void]$eb.AppendLine(('  "' + $pair[0] + '": ' + $pair[1] + ','))
    }
    [void]$eb.AppendLine('  "dotnetInfo": [')
    $lines = $dotnetInfo -split "`r?`n"
    for ($j = 0; $j -lt $lines.Count; $j++) {
        $comma = if ($j -lt $lines.Count - 1) { ',' } else { '' }
        [void]$eb.AppendLine(('    ' + (ConvertTo-JsonString $lines[$j]) + $comma))
    }
    [void]$eb.AppendLine('  ]')
    [void]$eb.Append('}')
    Write-Utf8NoBom -Path (Join-Path $OutputDir 'baseline-environment.json') -Text $eb.ToString()

    Write-Host ("capture-baseline: OK files=" + $sorted.Count +
        " staged=" + $stagedArr.Count +
        " unstaged=" + $unstagedArr.Count +
        " untracked=" + $untrackedArr.Count +
        " head=" + $head + " branch=" + $branch)
    exit 0
}
catch {
    Write-Error $_
    exit $exitGitFailure
}
