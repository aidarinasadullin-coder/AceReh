# =============================================================================
# Todo 13 (phase-5-hydraulics-state) fixture generator (QA-only, deterministic).
# Adapted from evidence/phase-4-thermal-state/prepare-ui-fixtures.ps1 (V9).
# Contract preserved:
#   - uses Get-Content -Raw | ConvertFrom-Json;
#   - modifies only existing camel-case JSON properties (or clones an existing
#     array element when adding the second Project-B circuit);
#   - writes UTF-8 JSON with ConvertTo-Json -Depth 100 (no BOM via
#     [IO.File]::WriteAllText + UTF8Encoding($false));
#   - never invokes or bypasses production persistence code.
# Outputs (under -OutputDirectory):
#   project-a.smc      — byte-identical copy of v1-sample.smc (ethylene 30%,
#                        one circuit L=80/Lzul=8, saved hydraulics results);
#   project-b.smc      — projectNumber="PHASE5-B"; thermalData.supplyTemperature=55.0,
#                        groundTemperature=5.0, pipeSpacing=150, RAUTHERM S 17x2,0,
#                        thermalData.result=null; hydraulicsData.glycolType="propylene",
#                        glycolConcentration=40.0, supplySpacingCm=7.0,
#                        supplyHeatPercent=15.0; two circuits (L=60/Lzul=6 and
#                        L=90/Lzul=9, pipeSpacingCm=15) carrying DISTINCT saved
#                        sentinel results (power 11111/33333, flow 2222/4444) so
#                        restored-vs-stale display values are unambiguous;
#                        summary.circuitCount=2, totalPipeLength=165.0,
#                        totalPower=44444.0, totalFlowRate=6666.0;
#   unknown-pipe.smc   — from Project B with projectNumber="PHASE5-UNKNOWN-PIPE"
#                        and an INVALID pipe/valve reference:
#                        hydraulicsData.collectors[0].valveType="PHASE5 UNKNOWN PIPE"
#                        (undefined ValveType enum string -> deserialization
#                        rejects the candidate -> graceful validation dialog);
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
$requiredPaths = @(
    @{ n = 'projectNumber';                              get = { param($o) $o.projectNumber } },
    @{ n = 'thermalData.supplyTemperature';              get = { param($o) $o.thermalData.supplyTemperature } },
    @{ n = 'thermalData.groundTemperature';              get = { param($o) $o.thermalData.groundTemperature } },
    @{ n = 'thermalData.pipeSpacing';                    get = { param($o) $o.thermalData.pipeSpacing } },
    @{ n = 'thermalData.selectedPipe.name';              get = { param($o) $o.thermalData.selectedPipe.name } },
    @{ n = 'thermalData.selectedPipe.outerDiameter';     get = { param($o) $o.thermalData.selectedPipe.outerDiameter } },
    @{ n = 'thermalData.selectedPipe.innerDiameter';     get = { param($o) $o.thermalData.selectedPipe.innerDiameter } },
    @{ n = 'thermalData.selectedPipe.wallThickness';     get = { param($o) $o.thermalData.selectedPipe.wallThickness } },
    @{ n = 'thermalData.result';                         get = { param($o) $o.thermalData.result } },
    @{ n = 'hydraulicsData.glycolType';                  get = { param($o) $o.hydraulicsData.glycolType } },
    @{ n = 'hydraulicsData.glycolConcentration';         get = { param($o) $o.hydraulicsData.glycolConcentration } },
    @{ n = 'hydraulicsData.supplySpacingCm';             get = { param($o) $o.hydraulicsData.supplySpacingCm } },
    @{ n = 'hydraulicsData.supplyHeatPercent';           get = { param($o) $o.hydraulicsData.supplyHeatPercent } },
    @{ n = 'hydraulicsData.collectors[0]';               get = { param($o) $o.hydraulicsData.collectors[0] } },
    @{ n = 'collectors[0].valveType';                    get = { param($o) $o.hydraulicsData.collectors[0].valveType } },
    @{ n = 'collectors[0].circuits[0].circuitNumber';    get = { param($o) $o.hydraulicsData.collectors[0].circuits[0].circuitNumber } },
    @{ n = 'collectors[0].circuits[0].circuitLength';    get = { param($o) $o.hydraulicsData.collectors[0].circuits[0].circuitLength } },
    @{ n = 'collectors[0].circuits[0].supplyLength';     get = { param($o) $o.hydraulicsData.collectors[0].circuits[0].supplyLength } },
    @{ n = 'collectors[0].circuits[0].pipeSpacingCm';    get = { param($o) $o.hydraulicsData.collectors[0].circuits[0].pipeSpacingCm } },
    @{ n = 'collectors[0].summary.circuitCount';         get = { param($o) $o.hydraulicsData.collectors[0].summary.circuitCount } },
    @{ n = 'collectors[0].summary.totalPipeLength';      get = { param($o) $o.hydraulicsData.collectors[0].summary.totalPipeLength } }
)
foreach ($req in $requiredPaths) {
    $value = & $req.get $raw
    if ($null -eq $value) { Fail "required source path is missing/null: $($req.n)" }
}
foreach ($prop in @('thermalData', 'hydraulicsData')) {
    if (-not $raw.PSObject.Properties[$prop]) { Fail "source has no $prop property" }
}
if (-not $raw.hydraulicsData.PSObject.Properties['collectors']) { Fail 'source hydraulicsData has no collectors property' }

# --- 3) Project A: unchanged copy --------------------------------------------
$pathA = Join-Path $outRoot 'project-a.smc'
Copy-Item -LiteralPath $sourcePath -Destination $pathA -Force

# --- 4) Project B: mutate only existing camel-case properties ----------------
$jsonB = Get-Content -LiteralPath $sourcePath -Raw | ConvertFrom-Json
$jsonB.projectNumber = 'PHASE5-B'
$jsonB.thermalData.supplyTemperature = 55.0
$jsonB.thermalData.groundTemperature = 5.0
$jsonB.thermalData.pipeSpacing = 150
$jsonB.thermalData.selectedPipe.name = 'RAUTHERM S 17x2,0'
$jsonB.thermalData.selectedPipe.outerDiameter = 17.0
$jsonB.thermalData.selectedPipe.innerDiameter = 13.0
$jsonB.thermalData.selectedPipe.wallThickness = 2.0
$jsonB.thermalData.result = $null

$jsonB.hydraulicsData.glycolType = 'propylene'
$jsonB.hydraulicsData.glycolConcentration = 40.0
$jsonB.hydraulicsData.supplySpacingCm = 7.0
$jsonB.hydraulicsData.supplyHeatPercent = 15.0

$c1 = $jsonB.hydraulicsData.collectors[0].circuits[0]
$c1.circuitNumber = 1
$c1.circuitLength = 60.0
$c1.supplyLength = 6.0
$c1.pipeSpacingCm = 15.0
$c1.power = 11111.0
$c1.flowRate = 2222.0
# the grid renders OperatingResult.Power/FlowRate (adapter mirror), so the
# saved result blocks must carry the sentinels as well
$c1.operatingResult.power = 11111.0
$c1.operatingResult.flowRate = 2222.0
$c1.designResult.power = 11111.0
$c1.designResult.flowRate = 2222.0

# second circuit: clone of the first existing element (no invented schema);
# nested result objects need explicit copies (PSObject.Copy() is shallow)
$c2 = $c1.PSObject.Copy()
$c2.operatingResult = $c1.operatingResult.PSObject.Copy()
$c2.designResult = $c1.designResult.PSObject.Copy()
$c2.circuitNumber = 2
$c2.circuitLength = 90.0
$c2.supplyLength = 9.0
$c2.power = 33333.0
$c2.flowRate = 4444.0
$c2.operatingResult.power = 33333.0
$c2.operatingResult.flowRate = 4444.0
$c2.designResult.power = 33333.0
$c2.designResult.flowRate = 4444.0
$jsonB.hydraulicsData.collectors[0].circuits = @($c1, $c2)

$jsonB.hydraulicsData.collectors[0].summary.circuitCount = 2
$jsonB.hydraulicsData.collectors[0].summary.totalPipeLength = 165.0
$jsonB.hydraulicsData.collectors[0].summary.totalPower = 44444.0
$jsonB.hydraulicsData.collectors[0].summary.totalFlowRate = 6666.0

$pathB = Join-Path $outRoot 'project-b.smc'
Write-Utf8NoBomFile $pathB ($jsonB | ConvertTo-Json -Depth 100)

# --- 5) unknown-pipe: from Project B, invalid pipe/valve reference -----------
$jsonU = Get-Content -LiteralPath $pathB -Raw | ConvertFrom-Json
$jsonU.projectNumber = 'PHASE5-UNKNOWN-PIPE'
$jsonU.hydraulicsData.collectors[0].valveType = 'PHASE5 UNKNOWN PIPE'
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

Assert-String $parsedB.projectNumber 'PHASE5-B' 'B.projectNumber'
Assert-Number $parsedB.thermalData.supplyTemperature 55.0 'B.thermalData.supplyTemperature'
Assert-Number $parsedB.thermalData.groundTemperature 5.0 'B.thermalData.groundTemperature'
Assert-Number $parsedB.thermalData.pipeSpacing 150 'B.thermalData.pipeSpacing'
Assert-String $parsedB.thermalData.selectedPipe.name 'RAUTHERM S 17x2,0' 'B.thermalData.selectedPipe.name'
if ($null -ne $parsedB.thermalData.result) { Fail 'B.thermalData.result expected null' }

Assert-String $parsedB.hydraulicsData.glycolType 'propylene' 'B.hydraulicsData.glycolType'
Assert-Number $parsedB.hydraulicsData.glycolConcentration 40.0 'B.hydraulicsData.glycolConcentration'
Assert-Number $parsedB.hydraulicsData.supplySpacingCm 7.0 'B.hydraulicsData.supplySpacingCm'
Assert-Number $parsedB.hydraulicsData.supplyHeatPercent 15.0 'B.hydraulicsData.supplyHeatPercent'
if ($parsedB.hydraulicsData.collectors[0].circuits.Count -ne 2) { Fail 'B expects exactly 2 circuits' }
Assert-Number $parsedB.hydraulicsData.collectors[0].circuits[0].circuitLength 60.0 'B.circuits[0].circuitLength'
Assert-Number $parsedB.hydraulicsData.collectors[0].circuits[0].supplyLength 6.0 'B.circuits[0].supplyLength'
Assert-Number $parsedB.hydraulicsData.collectors[0].circuits[0].power 11111.0 'B.circuits[0].power'
Assert-Number $parsedB.hydraulicsData.collectors[0].circuits[0].operatingResult.power 11111.0 'B.circuits[0].operatingResult.power'
Assert-Number $parsedB.hydraulicsData.collectors[0].circuits[0].designResult.power 11111.0 'B.circuits[0].designResult.power'
Assert-Number $parsedB.hydraulicsData.collectors[0].circuits[1].circuitNumber 2 'B.circuits[1].circuitNumber'
Assert-Number $parsedB.hydraulicsData.collectors[0].circuits[1].circuitLength 90.0 'B.circuits[1].circuitLength'
Assert-Number $parsedB.hydraulicsData.collectors[0].circuits[1].supplyLength 9.0 'B.circuits[1].supplyLength'
Assert-Number $parsedB.hydraulicsData.collectors[0].circuits[1].power 33333.0 'B.circuits[1].power'
Assert-Number $parsedB.hydraulicsData.collectors[0].circuits[1].operatingResult.power 33333.0 'B.circuits[1].operatingResult.power'
Assert-Number $parsedB.hydraulicsData.collectors[0].summary.circuitCount 2 'B.summary.circuitCount'
Assert-Number $parsedB.hydraulicsData.collectors[0].summary.totalPipeLength 165.0 'B.summary.totalPipeLength'
Assert-Number $parsedB.hydraulicsData.collectors[0].summary.totalPower 44444.0 'B.summary.totalPower'

Assert-String $parsedU.projectNumber 'PHASE5-UNKNOWN-PIPE' 'unknown.projectNumber'
Assert-String $parsedU.hydraulicsData.collectors[0].valveType 'PHASE5 UNKNOWN PIPE' 'unknown.valveType (invalid reference)'
# sanity: Project A keeps its characterized v1-sample values
Assert-String $parsedA.projectNumber 'T19-REGRESSION-001' 'A.projectNumber'
Assert-Number $parsedA.thermalData.pipeSpacing 250 'A.thermalData.pipeSpacing'
Assert-String $parsedA.hydraulicsData.glycolType 'ethylene' 'A.hydraulicsData.glycolType'
Assert-Number $parsedA.hydraulicsData.collectors[0].circuits[0].circuitLength 80.0 'A.circuits[0].circuitLength'

# --- 7) Manifest with source/output SHA-256 ---------------------------------
$manifest = [ordered]@{
    todo        = 'task-13-phase-5'
    generator   = 'docs/architecture-migration/evidence/phase-5-hydraulics-state/ui-qa/prepare-ui-fixtures.ps1'
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
        projectACopyOfSource        = ($shaA -eq $shaSource)
        projectBValuesExact         = $true
        unknownPipeValuesExact      = $true
        mutatedPathsPreExisted      = $true
        secondCircuitClonedElement  = $true
        utf8NoBom                   = $true
        convertToJsonDepth          = 100
    }
}
$manifestPath = Join-Path $outRoot 'fixture-manifest.json'
Write-Utf8NoBomFile $manifestPath ($manifest | ConvertTo-Json -Depth 100)

Write-Output ("prepare-ui-fixtures OK: source={0} a={1} b={2} u={3} manifest={4}" -f `
    $shaSource.Substring(0, 12), $shaA.Substring(0, 12),
    $manifest.outputs[1].sha256.Substring(0, 12), $manifest.outputs[2].sha256.Substring(0, 12), $manifestPath)
exit 0
