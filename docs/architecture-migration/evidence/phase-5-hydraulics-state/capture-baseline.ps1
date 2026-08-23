#!/usr/bin/env pwsh
<# Todo 1 Phase 5: capture a fail-closed protected baseline. #>
param([string]$OutputDir = "")
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Json([string]$Path, $Value) {
    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 8), [System.Text.UTF8Encoding]::new($false))
}
function Invoke-Git([string[]]$GitArguments, [string]$WorkingDir) {
    Push-Location -LiteralPath $WorkingDir
    try {
        $out = & git @GitArguments 2>&1
        if ($LASTEXITCODE -ne 0) { throw "git $($GitArguments -join ' ') failed ($LASTEXITCODE): $out" }
        return ($out -join "`n")
    }
    finally { Pop-Location }
}
function Normalize([string]$Path) { return $Path.Replace('\','/').TrimStart('./') }

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (& git rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) { throw 'git rev-parse --show-toplevel failed' }
if ([string]::IsNullOrWhiteSpace($OutputDir)) { $OutputDir = Join-Path $scriptDir 'task-1' }
$protectedPatterns = @(
    'src/Services/Project/**', 'src/Services/Navigation/CalculationStateService.cs',
    'src/Services/Navigation/ICalculationStateService.cs', 'src/Models/Navigation/CalculationContext.cs',
    'src/ViewModels/Hydraulics/CircuitsViewModel.cs', 'src/ViewModels/Results/ResultsViewModel.cs',
    'src/Configuration/ServiceCollectionExtensions.cs', 'src/Models/Project/ProjectData.cs',
    'src/Views/Hydraulics/*.xaml', 'tests/SnowMeltingCalculator.Tests/**',
    'docs/architecture-migration/maps/**', 'docs/architecture-migration/widget/**',
    'docs/architecture-migration/workflow/validate-state.mjs', 'docs/architecture-migration/STATE.json'
)
$statusText = Invoke-Git -GitArguments @('status','--porcelain') -WorkingDir $repoRoot
$statusLines = @($statusText -split "`r?`n" | Where-Object { $_ -ne '' })
$status = @($statusLines | ForEach-Object {
    [pscustomobject]@{ code = $_.Substring(0,2); path = Normalize $_.Substring(3) }
})
$trackedText = Invoke-Git -GitArguments @('ls-files') -WorkingDir $repoRoot
$tracked = @($trackedText -split "`r?`n" | Where-Object { $_ -ne '' } | ForEach-Object { Normalize $_ })
$rows = [Collections.Generic.List[object]]::new()
$dirtyPreimages = [Collections.Generic.List[object]]::new()
$protectedDirty = [Collections.Generic.List[object]]::new()
foreach ($rel in $tracked) {
    $match = $false
    foreach ($pattern in $protectedPatterns) {
        $regex = '^' + [regex]::Escape($pattern).Replace('\*\*','.*').Replace('\*','[^/]*') + '$'
        if ($rel -match $regex) { $match = $true; break }
    }
    if (-not $match) { continue }
    $full = Join-Path $repoRoot ($rel.Replace('/','\'))
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "protected tracked path missing: $rel" }
    $hash = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToUpperInvariant()
    $rows.Add([pscustomobject]@{ path = $rel; sha256 = $hash })
}
foreach ($entry in $status) {
    $path = $entry.path
    $isProtected = $false
    foreach ($pattern in $protectedPatterns) {
        $regex = '^' + [regex]::Escape($pattern).Replace('\*\*','.*').Replace('\*','[^/]*') + '$'
        if ($path -match $regex) { $isProtected = $true; break }
    }
    if ($isProtected) { $protectedDirty.Add($entry) }
    $full = Join-Path $repoRoot ($path.Replace('/','\'))
    $sha = $null
    if (Test-Path -LiteralPath $full -PathType Leaf) { $sha = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToUpperInvariant() }
    $dirtyPreimages.Add([pscustomobject]@{ path = $path; code = $entry.code; sha256 = $sha })
}
$head = (Invoke-Git -GitArguments @('rev-parse','HEAD') -WorkingDir $repoRoot).Trim()
$branch = (Invoke-Git -GitArguments @('branch','--show-current') -WorkingDir $repoRoot).Trim()
$manifest = [pscustomobject]@{
    phase = 'phase-5-hydraulics-state'; planPath = 'docs/architecture-migration/plans/phase-5-hydraulics-state.md'
    planSha256 = '0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38'
    protectedPatterns = $protectedPatterns; files = @($rows); capturedHead = $head; branch = $branch
}
Write-Json (Join-Path $OutputDir 'protected-manifest.json') $manifest
Write-Json (Join-Path $OutputDir 'protected-pre.json') ([pscustomobject]@{
    phase = 'phase-5-hydraulics-state'; capturedAt = 'execution-time'; worktreeClean = ($status.Count -eq 0)
    gitStatusPorcelain = $statusLines; status = @($status); protectedDirtyPreimages = @($protectedDirty)
    dirtyPreimages = @($dirtyPreimages); protectedPatterns = $protectedPatterns
    note = if ($status.Count -eq 0) { 'worktree clean; protected-path and dirty preimage lists are empty' } else { 'pre-existing dirty paths recorded verbatim; no files were overwritten' }
})
Write-Host "capture-baseline: status=$($status.Count) protected=$($rows.Count) protectedDirty=$($protectedDirty.Count)"
exit 0
