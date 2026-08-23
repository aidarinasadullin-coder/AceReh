#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Todo 12 (phase-4-thermal-state): F4 consolidated final-receipt verifier.

.DESCRIPTION
  Parameters (exact): -ReceiptF1 <path> -ReceiptF2 <path> -ReceiptF3 <path> -Manifest <frozen-release-sha256.json>

  For EACH of the three domain receipts (F1/F2/F3):
    - the file must exist;
    - machine fields are lines matching ^[A-Z][A-Z0-9_]*: ; EXACTLY the five
      fields REVIEW_ID, SUBJECT, RECEIPT, VERDICT, REASON must each appear
      exactly once (omissions, duplicates and any other uppercase-colon
      machine field reject; keep prose off such line shapes);
    - VERDICT must be APPROVE (REJECT/BLOCKED/anything else rejects);
    - SUBJECT must equal "phase-4-thermal-state@<manifest plan sha256>" and be
      identical across all three receipts.

  Frozen-hash reconciliation per lane (receipt parent directory):
    - frozen-hashes-before.json AND frozen-hashes-after.json must exist;
    - both carry manifestSha256 equal to the actual SHA-256 of -Manifest,
      lane equal to the domain lane and distinct moments;
    - before/after artifact sets are identical (key/resolvedPath/sha256);
    - the artifact key set equals exactly executable|productDll|testDll|plan;
    - every artifact file exists as a regular file under the repository root
      and its recomputed SHA-256 equals the stored hash AND the manifest hash.
  Cross-lane: F1/F2/F3 artifact sets must be identical.

  Exit codes: 0 ok; 2 invalid input/manifest; 3 rejection; 4 internal error.
#>
param(
    [Parameter(Mandatory = $true)][string]$ReceiptF1,
    [Parameter(Mandatory = $true)][string]$ReceiptF2,
    [Parameter(Mandatory = $true)][string]$ReceiptF3,
    [Parameter(Mandatory = $true)][string]$Manifest
)

$ErrorActionPreference = 'Continue'
Set-StrictMode -Version Latest

$exitInvalidInput = 2
$exitRejection = 3
$exitInternal = 4

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
    if (-not (Test-Path -LiteralPath $Manifest -PathType Leaf)) {
        Write-Error "manifest not found: $Manifest"
        exit $exitInvalidInput
    }

    # ------------------------------------------------------- resolve repo root
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $rootBytes = Invoke-GitRaw -GitArgs @('rev-parse', '--show-toplevel') -WorkingDir $scriptDir
    $repoRoot = [System.Text.Encoding]::UTF8.GetString($rootBytes).Trim().TrimEnd('/', '\').Replace('/', '\')
    $rootPrefix = $repoRoot + [System.IO.Path]::DirectorySeparatorChar

    # ------------------------------------------------- validate frozen manifest
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
            Write-Error "manifest has unexpected key '$k'"
            exit $exitInvalidInput
        }
    }
    $manifestHashes = @{}
    foreach ($key in $requiredKeys) {
        $entry = $manifestDoc.$key
        if ($null -eq $entry -or $entry -isnot [pscustomobject]) {
            Write-Error "manifest entry '$key' is not an object"
            exit $exitInvalidInput
        }
        $rel = [string]$entry.path
        $hash = [string]$entry.sha256
        if ([string]::IsNullOrWhiteSpace($rel) -or $hash -notmatch '^[0-9A-F]{64}$') {
            Write-Error "manifest entry '$key' has malformed path or sha256"
            exit $exitInvalidInput
        }
        $manifestHashes[$key] = @{ Path = $rel.Replace('\', '/'); Sha256 = $hash }
    }
    $manifestSha = Get-Sha256Upper -Path $Manifest
    $expectedSubject = "phase-4-thermal-state@$([string]$manifestDoc.plan.sha256)"

    # ------------------------------------------------------------- parse receipts
    $lanes = @(
        @{ Lane = 'F1'; Path = $ReceiptF1 },
        @{ Lane = 'F2'; Path = $ReceiptF2 },
        @{ Lane = 'F3'; Path = $ReceiptF3 }
    )
    $requiredFields = @('REVIEW_ID', 'SUBJECT', 'RECEIPT', 'VERDICT', 'REASON')
    $subjects = [System.Collections.Generic.List[string]]::new()
    $laneArtifacts = @{}

    foreach ($laneInfo in $lanes) {
        $lane = $laneInfo.Lane
        $receiptPath = $laneInfo.Path
        if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
            Write-Error "${lane} receipt not found: $receiptPath"
            exit $exitRejection
        }
        $text = [System.IO.File]::ReadAllText($receiptPath)
        $fieldMap = @{}
        foreach ($f in $requiredFields) { $fieldMap[$f] = [System.Collections.Generic.List[string]]::new() }
        foreach ($line in ($text -split "`r?`n")) {
            if ($line -match '^([A-Z][A-Z0-9_]*):(.*)$') {
                $name = $Matches[1]
                $value = $Matches[2].Trim()
                if (-not $fieldMap.ContainsKey($name)) {
                    Write-Error "${lane} receipt has unexpected machine field '$name' (exactly REVIEW_ID|SUBJECT|RECEIPT|VERDICT|REASON allowed)"
                    exit $exitRejection
                }
                [void]$fieldMap[$name].Add($value)
            }
        }
        foreach ($f in $requiredFields) {
            if ($fieldMap[$f].Count -eq 0) {
                Write-Error "${lane} receipt is missing machine field '$f'"
                exit $exitRejection
            }
            if ($fieldMap[$f].Count -gt 1) {
                Write-Error "${lane} receipt has duplicate machine field '$f' ($($fieldMap[$f].Count) occurrences)"
                exit $exitRejection
            }
        }
        $verdict = $fieldMap['VERDICT'][0]
        if ($verdict -ne 'APPROVE') {
            Write-Error "${lane} receipt VERDICT is '$verdict' (only APPROVE is accepted)"
            exit $exitRejection
        }
        $subject = $fieldMap['SUBJECT'][0]
        if ($subject -ne $expectedSubject) {
            Write-Error "${lane} receipt SUBJECT '$subject' != expected '$expectedSubject'"
            exit $exitRejection
        }
        [void]$subjects.Add($subject)

        # ------------------------------------------------ frozen hashes per lane
        $laneDir = Split-Path -Parent $receiptPath
        $momentSets = @{}
        foreach ($moment in @('before', 'after')) {
            $fhPath = Join-Path $laneDir ("frozen-hashes-" + $moment + ".json")
            if (-not (Test-Path -LiteralPath $fhPath -PathType Leaf)) {
                Write-Error "${lane} lane is missing frozen-hashes-${moment}.json (expected at $fhPath)"
                exit $exitRejection
            }
            try {
                $fh = Get-Content -LiteralPath $fhPath -Raw | ConvertFrom-Json
            }
            catch {
                Write-Error "${lane} frozen-hashes-${moment}.json is not valid JSON: $($_.Exception.Message)"
                exit $exitRejection
            }
            if ([string]$fh.manifestSha256 -ne $manifestSha) {
                Write-Error "${lane} frozen-hashes-${moment}.json manifestSha256 mismatch (expected $manifestSha)"
                exit $exitRejection
            }
            if ([string]$fh.lane -ne $lane) {
                Write-Error "${lane} frozen-hashes-${moment}.json declares lane '$($fh.lane)'"
                exit $exitRejection
            }
            $mom = ([string]$fh.moment).ToLowerInvariant()
            if ($mom -ne $moment) {
                Write-Error "${lane} frozen-hashes-${moment}.json declares moment '$($fh.moment)'"
                exit $exitRejection
            }
            $artArr = @($fh.artifacts)
            if ($artArr.Count -eq 0) {
                Write-Error "${lane} frozen-hashes-${moment}.json has no artifacts"
                exit $exitRejection
            }
            $map = @{}
            foreach ($a in $artArr) {
                $k = [string]$a.key
                $rp = [string]$a.resolvedPath
                $sh = [string]$a.sha256
                if ([string]::IsNullOrWhiteSpace($k) -or [string]::IsNullOrWhiteSpace($rp) -or $sh -notmatch '^[0-9A-F]{64}$') {
                    Write-Error "${lane} frozen-hashes-${moment}.json has a malformed artifact entry"
                    exit $exitRejection
                }
                if ($map.ContainsKey($k)) {
                    Write-Error "${lane} frozen-hashes-${moment}.json has duplicate artifact key '$k'"
                    exit $exitRejection
                }
                $map[$k] = @{ ResolvedPath = $rp.Replace('\', '/'); Sha256 = $sh }
            }
            $momentSets[$moment] = $map
        }

        # before/after equality per lane
        foreach ($k in $requiredKeys) {
            $b = $momentSets['before']; $a = $momentSets['after']
            if (-not $b.ContainsKey($k)) {
                Write-Error "${lane} frozen-hashes-before.json is missing artifact key '$k'"
                exit $exitRejection
            }
            if (-not $a.ContainsKey($k)) {
                Write-Error "${lane} frozen-hashes-after.json is missing artifact key '$k'"
                exit $exitRejection
            }
            if ($b[$k].ResolvedPath -ine $a[$k].ResolvedPath -or $b[$k].Sha256 -cne $a[$k].Sha256) {
                Write-Error "${lane} before/after drift for artifact '$k'"
                exit $exitRejection
            }
            # artifact hash matches manifest and the actual file on disk
            if ($b[$k].Sha256 -cne $manifestHashes[$k].Sha256 -or $b[$k].ResolvedPath -ine $manifestHashes[$k].Path) {
                Write-Error "${lane} artifact '$k' does not match frozen manifest entry"
                exit $exitRejection
            }
            $norm = $b[$k].ResolvedPath
            if ([System.IO.Path]::IsPathRooted($norm)) {
                Write-Error "${lane} artifact '$k': rooted path not allowed: $norm"
                exit $exitRejection
            }
            foreach ($seg in $norm.Split('/')) {
                if ($seg -eq '' -or $seg -eq '.' -or $seg -eq '..') {
                    Write-Error "${lane} artifact '$k': illegal path segment in $norm"
                    exit $exitRejection
                }
            }
            $full = [System.IO.Path]::GetFullPath((Join-Path $repoRoot ($norm.Replace('/', '\'))))
            if (-not $full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                Write-Error "${lane} artifact '$k': path escapes repository root: $norm"
                exit $exitRejection
            }
            if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
                Write-Error "${lane} artifact '$k': file not found or not a regular file: $norm"
                exit $exitRejection
            }
            $actual = Get-Sha256Upper -Path $full
            if ($actual -cne $b[$k].Sha256) {
                Write-Error "${lane} artifact '$k': recomputed hash mismatch for $norm (expected $($b[$k].Sha256), actual $actual)"
                exit $exitRejection
            }
        }
        $laneArtifacts[$lane] = $momentSets['before']
    }

    # ------------------------------------------------------------- cross-lane equality
    $first = $lanes[0].Lane
    foreach ($laneInfo in $lanes[1..2]) {
        $lane = $laneInfo.Lane
        foreach ($k in $requiredKeys) {
            if ($laneArtifacts[$lane][$k].ResolvedPath -ine $laneArtifacts[$first][$k].ResolvedPath -or
                $laneArtifacts[$lane][$k].Sha256 -cne $laneArtifacts[$first][$k].Sha256) {
                Write-Error "cross-lane drift for artifact '$k' between ${first} and ${lane}"
                exit $exitRejection
            }
        }
    }
    foreach ($s in $subjects) {
        if ($s -cne $subjects[0]) {
            Write-Error "SUBJECT values differ across receipts"
            exit $exitRejection
        }
    }

    Write-Host ("verify-final-receipts: lanes=3 subject=" + $subjects[0] +
        " manifestSha256=" + $manifestSha + " verdicts=APPROVE/APPROVE/APPROVE")
    exit 0
}
catch {
    Write-Error $_
    exit $exitInternal
}
