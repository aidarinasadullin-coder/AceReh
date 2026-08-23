#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Todo 12 (phase-4-thermal-state): immutable four-artifact frozen-release verifier (V13).

.DESCRIPTION
  Parameters (exact): -Manifest <frozen-release-sha256.json> -Lane <F1|F2|F3|F4> -Moment <Before|After>

  The manifest must contain EXACTLY the four keys executable, productDll,
  testDll, plan; each value is an object with EXACTLY {path, sha256} where
  path is a repository-relative forward-slash path and sha256 is UPPERCASE
  64-hex. Rejects (fail-closed): extra/missing keys, extra/missing entry
  fields, malformed or non-uppercase sha256, rooted/escaping/illegal paths,
  duplicate resolved paths, missing/non-regular files and hash mismatch.

  On success writes a lane-owned receipt next to this script at
    final/<lane>/frozen-hashes-<moment-lowercase>.json
  (directories created), containing:
    {"manifestSha256":"...","lane":"F1","moment":"before",
     "artifacts":[{"key":"executable","resolvedPath":"src/bin/...","sha256":"..."}, ...]}

  Exit codes: 0 ok; 2 invalid input/manifest/path violation; 3 verification
  failure (missing/non-regular file, hash mismatch); 4 internal error.
#>
param(
    [Parameter(Mandatory = $true)][string]$Manifest,
    [Parameter(Mandatory = $true)][string]$Lane,
    [Parameter(Mandatory = $true)][string]$Moment
)

$ErrorActionPreference = 'Continue'
Set-StrictMode -Version Latest

$exitInvalidInput = 2
$exitVerification = 3
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

function Get-Sha256Upper {
    param([string]$Path)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha.ComputeHash([System.IO.File]::ReadAllBytes($Path)))).Replace('-', '')
    }
    finally { $sha.Dispose() }
}

try {
    if ($Lane -notin @('F1', 'F2', 'F3', 'F4')) {
        Write-Error "Lane must be one of F1|F2|F3|F4 (got '$Lane')"
        exit $exitInvalidInput
    }
    if ($Moment -notin @('Before', 'After')) {
        Write-Error "Moment must be one of Before|After (got '$Moment')"
        exit $exitInvalidInput
    }
    if (-not (Test-Path -LiteralPath $Manifest -PathType Leaf)) {
        Write-Error "manifest not found: $Manifest"
        exit $exitInvalidInput
    }

    # ------------------------------------------------------------- resolve root
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $rootBytes = Invoke-GitRaw -GitArgs @('rev-parse', '--show-toplevel') -WorkingDir $scriptDir
    $repoRoot = [System.Text.Encoding]::UTF8.GetString($rootBytes).Trim().TrimEnd('/', '\').Replace('/', '\')
    $rootPrefix = $repoRoot + [System.IO.Path]::DirectorySeparatorChar

    # ------------------------------------------------------- parse + validate manifest
    # NOTE: local name must differ from the $Manifest parameter (PowerShell
    # variable names are case-insensitive; same-name assignment corrupts the
    # parameter read on the RHS).
    try {
        $manifestDoc = Get-Content -LiteralPath $Manifest -Raw | ConvertFrom-Json
    }
    catch {
        Write-Error "manifest is not valid JSON: $($_.Exception.Message)"
        exit $exitInvalidInput
    }

    $requiredKeys = @('executable', 'productDll', 'testDll', 'plan')
    $manifestKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($p in @($manifestDoc.PSObject.Properties.Name)) { [void]$manifestKeys.Add([string]$p) }
    foreach ($k in $requiredKeys) {
        if (-not $manifestKeys.Contains($k)) {
            Write-Error "manifest is missing required key '$k'"
            exit $exitInvalidInput
        }
    }
    foreach ($k in $manifestKeys) {
        if ($k -notin $requiredKeys) {
            Write-Error "manifest has unexpected key '$k' (exactly executable|productDll|testDll|plan required)"
            exit $exitInvalidInput
        }
    }

    $resolved = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $artifacts = [System.Collections.Generic.List[object]]::new()
    foreach ($key in $requiredKeys) {
        $entry = $manifestDoc.$key
        if ($null -eq $entry -or $entry -isnot [pscustomobject]) {
            Write-Error "manifest entry '$key' is not an object"
            exit $exitInvalidInput
        }
        $entryFields = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($p in @($entry.PSObject.Properties.Name)) { [void]$entryFields.Add([string]$p) }
        foreach ($f in @('path', 'sha256')) {
            if (-not $entryFields.Contains($f)) {
                Write-Error "manifest entry '$key' is missing field '$f'"
                exit $exitInvalidInput
            }
        }
        foreach ($f in $entryFields) {
            if ($f -notin @('path', 'sha256')) {
                Write-Error "manifest entry '$key' has unexpected field '$f' (exactly path|sha256 required)"
                exit $exitInvalidInput
            }
        }
        $rel = [string]$entry.path
        $hash = [string]$entry.sha256
        if ([string]::IsNullOrWhiteSpace($rel)) {
            Write-Error "manifest entry '$key': empty path"
            exit $exitInvalidInput
        }
        foreach ($ch in $rel.ToCharArray()) {
            if ([int]$ch -lt 0x20) {
                Write-Error ("manifest entry '{0}': control character U+{1:X4} in path" -f $key, [int]$ch)
                exit $exitInvalidInput
            }
        }
        $norm = $rel.Replace('\', '/')
        if ([System.IO.Path]::IsPathRooted($norm)) {
            Write-Error "manifest entry '$key': rooted path not allowed: $rel"
            exit $exitInvalidInput
        }
        foreach ($seg in $norm.Split('/')) {
            if ($seg -eq '' -or $seg -eq '.' -or $seg -eq '..') {
                Write-Error "manifest entry '$key': illegal path segment '$seg' in $rel"
                exit $exitInvalidInput
            }
        }
        if ($hash -notmatch '^[0-9A-F]{64}$') {
            Write-Error "manifest entry '$key': sha256 must be uppercase 64-hex (got '$hash')"
            exit $exitInvalidInput
        }
        $full = [System.IO.Path]::GetFullPath((Join-Path $repoRoot ($norm.Replace('/', '\'))))
        if (-not $full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            Write-Error "manifest entry '$key': path escapes repository root: $rel"
            exit $exitInvalidInput
        }
        if (-not $resolved.Add($full)) {
            Write-Error "manifest entries resolve to duplicate path: $rel"
            exit $exitInvalidInput
        }
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
            Write-Error "manifest entry '$key': file not found or not a regular file: $rel"
            exit $exitVerification
        }
        $item = Get-Item -LiteralPath $full -Force
        if ($item.PSIsContainer -or ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            Write-Error "manifest entry '$key': not a regular file: $rel"
            exit $exitVerification
        }
        $actual = Get-Sha256Upper -Path $full
        if ($actual -cne $hash) {
            Write-Error "manifest entry '$key': hash mismatch for $rel (expected $hash, actual $actual)"
            exit $exitVerification
        }
        $artifacts.Add([pscustomobject]@{ Key = $key; ResolvedPath = $norm; Sha256 = $actual })
    }

    $manifestSha = Get-Sha256Upper -Path $Manifest

    # ------------------------------------------------------------- write receipt
    $momentLower = $Moment.ToLowerInvariant()
    $receiptDir = Join-Path $scriptDir ('final\' + $Lane)
    New-Item -ItemType Directory -Force -Path $receiptDir | Out-Null
    $receiptPath = Join-Path $receiptDir ("frozen-hashes-" + $momentLower + ".json")

    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('{')
    [void]$sb.AppendLine(('  "manifestSha256": "' + $manifestSha + '",'))
    [void]$sb.AppendLine(('  "lane": "' + $Lane + '",'))
    [void]$sb.AppendLine(('  "moment": "' + $momentLower + '",'))
    [void]$sb.AppendLine('  "artifacts": [')
    for ($i = 0; $i -lt $artifacts.Count; $i++) {
        $a = $artifacts[$i]
        $comma = if ($i -lt $artifacts.Count - 1) { ',' } else { '' }
        [void]$sb.AppendLine(('    {"key": "' + $a.Key + '", "resolvedPath": ' + (ConvertTo-JsonString $a.ResolvedPath) + ', "sha256": "' + $a.Sha256 + '"}' + $comma))
    }
    [void]$sb.AppendLine('  ]')
    [void]$sb.Append('}')
    Write-Utf8NoBom -Path $receiptPath -Text $sb.ToString()

    Write-Host ("verify-frozen-release: lane=" + $Lane + " moment=" + $momentLower +
        " artifacts=" + $artifacts.Count + " manifestSha256=" + $manifestSha +
        " receipt=" + $receiptPath)
    exit 0
}
catch {
    Write-Error $_
    exit $exitInternal
}
