# =============================================================================
# Todo 13 / V9 fixture generator (QA-only, deterministic).
# Plan contract (frozen plan phase-4-thermal-state.md, line 308, EXACT):
#   - uses Get-Content -Raw | ConvertFrom-Json;
#   - modifies only existing camel-case JSON properties;
#   - writes UTF-8 JSON with ConvertTo-Json -Depth 100 (no BOM via
#     [IO.File]::WriteAllText + UTF8Encoding($false) applied to the string
#     produced by ConvertTo-Json);
#   - never invokes or bypasses production persistence code.
# Outputs (under -OutputDirectory):
#   project-a.smc      — byte-identical copy of the source fixture;
#   project-b.smc      — projectNumber="PHASE4-B", thermalData.supplyTemperature=55.0,
#                        groundTemperature=5.0, pipeSpacing=150, standard pipe
#                        {name:"RAUTHERM S 17x2,0",outerDiameter:17.0,innerDiameter:13.0,
#                         wallThickness:2.0}, result=null;
#   unknown-pipe.smc   — from Project B with projectNumber="PHASE4-UNKNOWN-PIPE" and pipe
#                        {name:"PHASE4 UNKNOWN PIPE",outerDiameter:99.0,innerDiameter:95.0,
#                         wallThickness:2.0};
#   fixture-manifest.json — source/output SHA-256.
# Missing paths, parse errors or mismatched values exit nonzero.
# =============================================================================
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$message) { throw [InvalidOperationException]::new("prepare-ui-fixtures: $message") }

if ($PSVersionTable.PSEdition -ne 'Core') {
    Fail "this script must run under pwsh (PowerShell Core); got edition '$($PSVersionTable.PSEdition)'"
}

$repoRoot = (Get-Location).Path
$sourcePath = Join-Path $repoRoot $Source
if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) { Fail "source fixture not found: $sourcePath" }

if (-not (Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}
$outRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Write-Utf8NoBomFile([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}

function Get-Sha256([string]$path) {
    return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToUpperInvariant()
}

# --- 1) Parse source ---------------------------------------------------------
$raw = Get-Content -LiteralPath $sourcePath -Raw | ConvertFrom-Json
if ($null -eq $raw) { Fail "source parsed to null JSON: $sourcePath" }

# --- 2) Pre-assert every mutated path already exists in the source DTO -------
# (values intentionally replaced are still required to EXIST as properties;
#  only their values are replaced, never added out of thin air)
$requiredPaths = @(
    @{ n = 'projectNumber';                          get = { param($o) $o.projectNumber } },
    @{ n = 'thermalData.supplyTemperature';          get = { param($o) $o.thermalData.supplyTemperature } },
    @{ n = 'thermalData.groundTemperature';          get = { param($o) $o.thermalData.groundTemperature } },
    @{ n = 'thermalData.pipeSpacing';                get = { param($o) $o.thermalData.pipeSpacing } },
    @{ n = 'thermalData.selectedPipe.name';          get = { param($o) $o.thermalData.selectedPipe.name } },
    @{ n = 'thermalData.selectedPipe.outerDiameter'; get = { param($o) $o.thermalData.selectedPipe.outerDiameter } },
    @{ n = 'thermalData.selectedPipe.innerDiameter'; get = { param($o) $o.thermalData.selectedPipe.innerDiameter } },
    @{ n = 'thermalData.selectedPipe.wallThickness'; get = { param($o) $o.thermalData.selectedPipe.wallThickness } },
    @{ n = 'thermalData.result';                     get = { param($o) $o.thermalData.result } }
)
foreach ($req in $requiredPaths) {
    $value = & $req.get $raw
    if ($null -eq $value) { Fail "required source path is missing/null: $($req.n)" }
}
if (-not $raw.PSObject.Properties['thermalData']) { Fail 'source has no thermalData property' }
if (-not $raw.thermalData.PSObject.Properties['selectedPipe']) { Fail 'source thermalData has no selectedPipe property' }
if (-not $raw.thermalData.PSObject.Properties['result']) { Fail 'source thermalData has no result property' }

# --- 3) Project A: unchanged copy --------------------------------------------
$pathA = Join-Path $outRoot 'project-a.smc'
Copy-Item -LiteralPath $sourcePath -Destination $pathA -Force

# --- 4) Project B: mutate only existing camel-case properties ----------------
$jsonB = Get-Content -LiteralPath $sourcePath -Raw | ConvertFrom-Json
$jsonB.projectNumber = 'PHASE4-B'
$jsonB.thermalData.supplyTemperature = 55.0
$jsonB.thermalData.groundTemperature = 5.0
$jsonB.thermalData.pipeSpacing = 150
$jsonB.thermalData.selectedPipe.name = 'RAUTHERM S 17x2,0'
$jsonB.thermalData.selectedPipe.outerDiameter = 17.0
$jsonB.thermalData.selectedPipe.innerDiameter = 13.0
$jsonB.thermalData.selectedPipe.wallThickness = 2.0
$jsonB.thermalData.result = $null
$pathB = Join-Path $outRoot 'project-b.smc'
Write-Utf8NoBomFile $pathB ($jsonB | ConvertTo-Json -Depth 100)

# --- 5) unknown-pipe: from Project B -----------------------------------------
$jsonU = Get-Content -LiteralPath $pathB -Raw | ConvertFrom-Json
$jsonU.projectNumber = 'PHASE4-UNKNOWN-PIPE'
$jsonU.thermalData.selectedPipe.name = 'PHASE4 UNKNOWN PIPE'
$jsonU.thermalData.selectedPipe.outerDiameter = 99.0
$jsonU.thermalData.selectedPipe.innerDiameter = 95.0
$jsonU.thermalData.selectedPipe.wallThickness = 2.0
$pathU = Join-Path $outRoot 'unknown-pipe.smc'
Write-Utf8NoBomFile $pathU ($jsonU | ConvertTo-Json -Depth 100)

# --- 6) Post-reparse all three and assert exact values -----------------------
$shaSource = Get-Sha256 $sourcePath
$shaA = Get-Sha256 $pathA
if ($shaA -ne $shaSource) { Fail "Project A SHA differs from source SHA: $shaA != $shaSource" }

$parsedA = Get-Content -LiteralPath $pathA -Raw | ConvertFrom-Json
$parsedB = Get-Content -LiteralPath $pathB -Raw | ConvertFrom-Json
$parsedU = Get-Content -LiteralPath $pathU -Raw | ConvertFrom-Json

function Assert-Number($actual, [double]$expected, [string]$label) {
    if ($null -eq $actual) { Fail "$label is null" }
    if ([double]$actual -ne $expected) { Fail "$label expected $expected, got $actual" }
}
function Assert-String([string]$actual, [string]$expected, [string]$label) {
    if ($null -eq $actual -or $actual -cne $expected) { Fail "$label expected '$expected', got '$actual'" }
}

Assert-String $parsedB.projectNumber 'PHASE4-B' 'B.projectNumber'
Assert-Number $parsedB.thermalData.supplyTemperature 55.0 'B.thermalData.supplyTemperature'
Assert-Number $parsedB.thermalData.groundTemperature 5.0 'B.thermalData.groundTemperature'
Assert-Number $parsedB.thermalData.pipeSpacing 150 'B.thermalData.pipeSpacing'
Assert-String $parsedB.thermalData.selectedPipe.name 'RAUTHERM S 17x2,0' 'B.thermalData.selectedPipe.name'
Assert-Number $parsedB.thermalData.selectedPipe.outerDiameter 17.0 'B.thermalData.selectedPipe.outerDiameter'
Assert-Number $parsedB.thermalData.selectedPipe.innerDiameter 13.0 'B.thermalData.selectedPipe.innerDiameter'
Assert-Number $parsedB.thermalData.selectedPipe.wallThickness 2.0 'B.thermalData.selectedPipe.wallThickness'
if ($null -ne $parsedB.thermalData.result) { Fail 'B.thermalData.result expected null' }

Assert-String $parsedU.projectNumber 'PHASE4-UNKNOWN-PIPE' 'unknown.projectNumber'
Assert-String $parsedU.thermalData.selectedPipe.name 'PHASE4 UNKNOWN PIPE' 'unknown.thermalData.selectedPipe.name'
Assert-Number $parsedU.thermalData.selectedPipe.outerDiameter 99.0 'unknown.thermalData.selectedPipe.outerDiameter'
Assert-Number $parsedU.thermalData.selectedPipe.innerDiameter 95.0 'unknown.thermalData.selectedPipe.innerDiameter'
Assert-Number $parsedU.thermalData.selectedPipe.wallThickness 2.0 'unknown.thermalData.selectedPipe.wallThickness'
if ($null -ne $parsedU.thermalData.result) { Fail 'unknown.thermalData.result expected null' }
# unknown-pipe inherits every other Project B value verbatim
Assert-Number $parsedU.thermalData.supplyTemperature 55.0 'unknown.thermalData.supplyTemperature'
Assert-Number $parsedU.thermalData.groundTemperature 5.0 'unknown.thermalData.groundTemperature'
Assert-Number $parsedU.thermalData.pipeSpacing 150 'unknown.thermalData.pipeSpacing'

# sanity: Project A still carries its original characterized values
Assert-String $parsedA.projectNumber 'T19-REGRESSION-001' 'A.projectNumber'
Assert-Number $parsedA.thermalData.supplyTemperature 50.0 'A.thermalData.supplyTemperature'
Assert-Number $parsedA.thermalData.pipeSpacing 250 'A.thermalData.pipeSpacing'

# --- 7) Manifest with source/output SHA-256 ---------------------------------
$manifest = [ordered]@{
    todo        = 'task-13'
    generator   = 'docs/architecture-migration/evidence/phase-4-thermal-state/prepare-ui-fixtures.ps1'
    source      = [ordered]@{
        path  = ($Source -replace '\\', '/')
        sha256 = $shaSource
    }
    outputs     = @(
        [ordered]@{ name = 'project-a.smc';    relativePath = 'project-a.smc';    sha256 = $shaA },
        [ordered]@{ name = 'project-b.smc';    relativePath = 'project-b.smc';    sha256 = (Get-Sha256 $pathB) },
        [ordered]@{ name = 'unknown-pipe.smc'; relativePath = 'unknown-pipe.smc'; sha256 = (Get-Sha256 $pathU) }
    )
    assertions  = [ordered]@{
        projectACopyOfSource       = ($shaA -eq $shaSource)
        projectBValuesExact        = $true
        unknownPipeValuesExact     = $true
        mutatedPathsPreExisted     = $true
        utf8NoBom                  = $true
        convertToJsonDepth         = 100
    }
}
$manifestPath = Join-Path $outRoot 'fixture-manifest.json'
Write-Utf8NoBomFile $manifestPath ($manifest | ConvertTo-Json -Depth 100)

Write-Output ("prepare-ui-fixtures OK: source={0} a={1} b={2} u={3} manifest={4}" -f `
    $shaSource.Substring(0, 12), $shaA.Substring(0, 12),
    $manifest.outputs[1].sha256.Substring(0, 12), $manifest.outputs[2].sha256.Substring(0, 12), $manifestPath)
exit 0
