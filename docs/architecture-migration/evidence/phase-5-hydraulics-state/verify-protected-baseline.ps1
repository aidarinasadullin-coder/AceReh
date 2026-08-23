#!/usr/bin/env pwsh
<# Todo 1 Phase 5: symmetric protected-path verifier. #>
param(
    [Parameter(Mandatory=$true)][string]$Baseline,
    [Parameter(Mandatory=$true)][string]$AllowedHunks,
    [Parameter(Mandatory=$true)][string]$EvidenceRoot,
    [Parameter(Mandatory=$true)][string]$Output
)
$ErrorActionPreference = 'Stop'; Set-StrictMode -Version Latest
function Json([string]$Path,$Value) { [IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 8),[System.Text.UTF8Encoding]::new($false)) }
function Invoke-Git([string[]]$GitArguments,[string]$WorkingDir) {
    Push-Location -LiteralPath $WorkingDir
    try { $o=& git @GitArguments 2>&1;if($LASTEXITCODE-ne 0){throw "git failed ($LASTEXITCODE): $o"};return ($o -join "`n") }
    finally { Pop-Location }
}
$repoRoot=(& git rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) { Write-Error 'git rev-parse --show-toplevel failed'; exit 4 }
if (-not (Test-Path -LiteralPath $Baseline -PathType Leaf)) { Write-Error "Baseline not found: $Baseline"; exit 2 }
try { $base=Get-Content -LiteralPath $Baseline -Raw|ConvertFrom-Json } catch { Write-Error "Baseline JSON invalid"; exit 2 }
if ($null -eq $base.files -or $null -eq $base.protectedPatterns) { Write-Error "Baseline missing protected manifest"; exit 2 }
$allow=@();if(Test-Path -LiteralPath $AllowedHunks -PathType Leaf){$allow=@((Get-Content -LiteralPath $AllowedHunks -Raw|ConvertFrom-Json).allowedHunks)}
$drift=[Collections.Generic.List[object]]::new()
foreach($row in @($base.files)){
    $full=Join-Path $repoRoot $row.path.Replace('/','\')
    if(-not(Test-Path -LiteralPath $full -PathType Leaf)){$drift.Add([pscustomobject]@{path=$row.path;kind='deleted';classification='protected-mismatch'});continue}
    $now=(Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToUpperInvariant()
    if($now -ne $row.sha256.ToUpperInvariant()){
        $cls=if($allow -contains $row.path){'allowed'}else{'protected-mismatch'}
        $drift.Add([pscustomobject]@{path=$row.path;kind='modified';classification=$cls})
    }
}
$mismatch=@($drift|Where-Object{$_.classification -eq 'protected-mismatch'}).Count
$result=[pscustomobject]@{protected_mismatch_count=$mismatch;allowed_hunk_count=@($drift|Where-Object{$_.classification -eq 'allowed'}).Count;changed_paths=@($drift);baseline=$Baseline;evidenceRoot=$EvidenceRoot}
$parent=Split-Path -Parent $Output;if(-not(Test-Path $parent)){New-Item -ItemType Directory $parent -Force|Out-Null};Json $Output $result
Write-Host "verify-protected-baseline: protected_mismatch_count=$mismatch changed=$($drift.Count)"
if($mismatch -gt 0){exit 3};exit 0
