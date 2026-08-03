---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: HEAD-plus-approved-dossier
generated_at_utc: 2026-07-31T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - PowerShell 5.1 ConvertFrom-Json schema/model plus the canonical deterministic validator from evidence/model-validation.md
  - PowerShell 5.1 independent exact validator embedded below, executed after this receipt was written
exit_code: 0
status: degraded
raw_output: Inline section "Observed post-write output" in this immutable receipt.
limitations:
  - No installed Draft 2020-12 validator is available; full JSON Schema validation remains degraded, while both canonical and independent deterministic checks pass.
  - This is a documentation/model quality review, not a build, test, runtime, or owner-acceptance receipt.
  - Selected current-source semantic samples validate documented claims but do not establish repository-wide graph completeness.
---

# F2 Dossier Quality Review

## Binding and Review Boundary

This independent F2 receipt is bound to snapshot
`f0d19c34ac03075d64548f1059e9c6626d3596b5` and the
`HEAD-plus-approved-dossier` basis. It does not rely on F1, the dossier-gate
verdict, or prose-only prior conclusions. It re-executes the canonical model
validator and separately checks the six model filters, two inventory matrices,
reconciliation, receipt metadata, current-claim labels, and source-backed
semantic samples. It neither advances the workflow nor creates any artifact
other than this receipt.

## Exact Post-Write Validator

Run from `D:/IA/ace v.2` in Windows PowerShell 5.1. The script is read-only;
the two negative probes clone or construct data in memory only.

```powershell
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$snapshot = 'f0d19c34ac03075d64548f1059e9c6626d3596b5'
$phase = 'phase-0-baseline'
$workdir = 'D:/IA/ace v.2'
$script:assertions = 0
$script:failures = [System.Collections.Generic.List[string]]::new()
function Assert-F2([bool]$condition, [string]$id, [string]$detail) {
  $script:assertions++
  if (-not $condition) { $script:failures.Add("${id}: ${detail}") }
}
function Read-Utf8([string]$path) { [IO.File]::ReadAllText((Join-Path $root $path)) }
function Get-Front([string]$text) { [regex]::Match($text, '(?s)\A---\r?\n(?<yaml>.*?)\r?\n---').Groups['yaml'].Value }
function Invoke-Embedded([string]$path) {
  $match = [regex]::Match((Read-Utf8 $path), '(?s)```powershell\r?\n(?<script>.*?)\r?\n```')
  if (-not $match.Success) { throw "embedded validator absent: $path" }
  return (& ([scriptblock]::Create($match.Groups['script'].Value)) | Out-String)
}

$maps = [ordered]@{
  'compile-time' = @{ path = 'docs/architecture-migration/maps/compile-time.md'; prefixes = @('CTN','CTE') }
  'di-runtime' = @{ path = 'docs/architecture-migration/maps/di-runtime.md'; prefixes = @('DRN','DRE') }
  'state-ownership' = @{ path = 'docs/architecture-migration/maps/state-ownership.md'; prefixes = @('ST') }
  'reactive' = @{ path = 'docs/architecture-migration/maps/reactive.md'; prefixes = @('RE') }
  'persistence' = @{ path = 'docs/architecture-migration/maps/persistence.md'; prefixes = @('PN','PE') }
  'user-flow' = @{ path = 'docs/architecture-migration/maps/user-flow.md'; prefixes = @('CF') }
}
$receipts = @(
  'docs/architecture-migration/evidence/repository-snapshot.md',
  'docs/architecture-migration/evidence/environment.md',
  'docs/architecture-migration/evidence/build-baseline.md',
  'docs/architecture-migration/evidence/test-baseline.md',
  'docs/architecture-migration/evidence/metrics-baseline.json',
  'docs/architecture-migration/evidence/codegraph-baseline.md',
  'docs/architecture-migration/evidence/audit-reconciliation.md',
  'docs/architecture-migration/evidence/persistence-fixtures.md',
  'docs/architecture-migration/evidence/user-flow-baseline.md',
  'docs/architecture-migration/evidence/model-validation.md',
  'docs/architecture-migration/evidence/dossier-gate.md'
)
$markdownMetadata = @($maps.Values.path) + @(
  'docs/architecture-migration/maps/state-inventory.md',
  'docs/architecture-migration/maps/characterization-tests.md',
  'docs/architecture-migration/maps/persistence-compatibility.md',
  'docs/architecture-migration/evidence/audit-reconciliation.md',
  'docs/architecture-migration/evidence/persistence-fixtures.md',
  'docs/architecture-migration/evidence/user-flow-baseline.md',
  'docs/architecture-migration/evidence/model-validation.md'
)
foreach ($path in $receipts + $markdownMetadata) { Assert-F2 (Test-Path -LiteralPath (Join-Path $root $path) -PathType Leaf) 'FILE' $path }
foreach ($path in ($markdownMetadata | Sort-Object -Unique)) {
  $front = Get-Front (Read-Utf8 $path)
  Assert-F2 (-not [string]::IsNullOrWhiteSpace($front)) 'META-FRONT' $path
  foreach ($field in @('phase','snapshot_sha','source_basis','generated_at_utc','working_directory','commands','exit_code','status','raw_output','limitations')) { Assert-F2 ($front -match "(?m)^${field}:") 'META-FIELD' "${path}::$field" }
  Assert-F2 ($front -match "(?m)^phase:\s*$phase\s*$") 'META-PHASE' $path
  Assert-F2 ($front -match "(?m)^snapshot_sha:\s*$snapshot\s*$") 'META-SNAPSHOT' $path
  Assert-F2 ($front -match '(?m)^source_basis:\s*(working-tree|HEAD|HEAD-plus-approved-dossier)\s*$') 'META-SOURCE' $path
  Assert-F2 ($front -match "(?m)^working_directory:\s*$([regex]::Escape($workdir))\s*$") 'META-WORKDIR' $path
}

$schema = Read-Utf8 'docs/architecture-migration/maps/architecture-model.schema.json' | ConvertFrom-Json
$model = Read-Utf8 'docs/architecture-migration/maps/architecture-model.baseline.json' | ConvertFrom-Json
Assert-F2 ($schema.'$schema' -eq 'https://json-schema.org/draft/2020-12/schema') 'SCHEMA-DRAFT' 'Draft 2020-12 declaration'
Assert-F2 ($model.meta.snapshot_sha -eq $snapshot -and $model.meta.phase -eq $phase -and $model.meta.source_basis -eq 'working-tree') 'MODEL-META' 'snapshot, phase, or source basis'
Assert-F2 ((@($model.views | Sort-Object) -join ',') -eq 'compile-time,di-runtime,persistence,reactive,state-ownership,user-flow') 'MODEL-VIEWS' 'exact six views'
Assert-F2 ($model.snapshots.baseline -eq 'observed' -and $model.snapshots.current -eq 'observed' -and $model.snapshots.target -eq 'unimplemented') 'SNAPSHOT-SEMANTICS' 'baseline/current/target'
$allowedConfidence = @($schema.'$defs'.confidence.enum); $allowedEdgeKinds = @($schema.properties.edges.items.allOf[1].properties.kind.enum)
$allowedState = @($schema.properties.state.items.allOf[1].properties.migration_status.enum); $allowedCoverage = @($schema.properties.flows.items.allOf[1].properties.status.enum)
$allRecords = @($model.evidence)+@($model.nodes)+@($model.edges)+@($model.state)+@($model.flows)+@($model.coverage)
$modelRecords = @($model.nodes)+@($model.edges)+@($model.state)+@($model.flows)+@($model.coverage)
$allIds = @($allRecords | ForEach-Object id)
Assert-F2 (($allIds | Sort-Object -Unique).Count -eq $allIds.Count) 'UNIQUE-GLOBAL-ID' "count=$($allIds.Count)"
$nodeIds = @($model.nodes.id); $edgeIds = @($model.edges.id); $stateIds = @($model.state.id); $evidenceIds = @($model.evidence.id)
foreach ($record in $modelRecords) {
  Assert-F2 (-not [string]::IsNullOrWhiteSpace($record.id)) 'RECORD-ID' ($record | ConvertTo-Json -Compress)
  Assert-F2 ($allowedConfidence -contains $record.confidence) 'CONFIDENCE' $record.id
  foreach ($reference in @($record.evidence)) { Assert-F2 ($evidenceIds -contains $reference) 'EVIDENCE-REF' "$($record.id):$reference" }
  foreach ($view in @($record.views)) { Assert-F2 (@($model.views) -contains $view) 'VIEW-ENUM' "$($record.id):$view" }
}
foreach ($evidence in $model.evidence) { Assert-F2 (Test-Path -LiteralPath (Join-Path $root $evidence.path)) 'EVIDENCE-PATH' "$($evidence.id):$($evidence.path)" }
foreach ($edge in $model.edges) {
  Assert-F2 ($nodeIds -contains $edge.from -and $nodeIds -contains $edge.to) 'EDGE-ENDPOINT' $edge.id
  Assert-F2 ($allowedEdgeKinds -contains $edge.kind) 'EDGE-KIND' "$($edge.id):$($edge.kind)"
  Assert-F2 (@($edge.evidence).Count -gt 0 -and $allowedConfidence -contains $edge.confidence) 'EDGE-EVIDENCE-CONFIDENCE' $edge.id
}
foreach ($state in $model.state) { Assert-F2 ($allowedState -contains $state.migration_status -and $allowedCoverage -contains $state.coverage_status) 'STATE-ENUM' $state.id }
$flowPositions = @($model.flows.position | Sort-Object); Assert-F2 (($flowPositions -join ',') -eq ((1..$model.flows.Count) -join ',')) 'FLOW-POSITIONS' ($flowPositions -join ',')
foreach ($flow in $model.flows) { Assert-F2 ($allowedCoverage -contains $flow.status) 'FLOW-STATUS' $flow.id }
Assert-F2 (@($model.edge_semantics).Count -eq $edgeIds.Count) 'SEMANTIC-COUNT' "edges=$($edgeIds.Count) semantics=$($model.edge_semantics.Count)"
Assert-F2 ((@($model.edge_semantics.edge_id | Sort-Object -Unique).Count -eq $edgeIds.Count)) 'SEMANTIC-UNIQUE' 'one semantic record per edge'
foreach ($semantic in $model.edge_semantics) { Assert-F2 ($edgeIds -contains $semantic.edge_id -and @($schema.'$defs'.sourceKind.enum) -contains $semantic.source_kind) 'EDGE-SEMANTICS' "$($semantic.edge_id):$($semantic.source_kind)"; if ($null -ne $semantic.PSObject.Properties['state_refs']) { foreach ($stateRef in @($semantic.state_refs)) { Assert-F2 ($stateIds -contains $stateRef) 'SEMANTIC-STATE-REF' "$($semantic.edge_id):$stateRef" } } }

foreach ($viewName in $maps.Keys) {
  $text = Read-Utf8 $maps[$viewName].path
  foreach ($prefix in $maps[$viewName].prefixes) {
    $mapIds = @([regex]::Matches($text, "(?m)``(${prefix}-[0-9]{2,3})``") | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
    $modelIds = @($modelRecords | Where-Object { $_.id -like "$prefix-*" -and @($_.views) -contains $viewName } | ForEach-Object id | Sort-Object -Unique)
    Assert-F2 ((Compare-Object $mapIds $modelIds).Count -eq 0) 'MAP-MEMBERSHIP' "$viewName/$prefix map=$($mapIds.Count) model=$($modelIds.Count)"
  }
}

$inventory = Read-Utf8 'docs/architecture-migration/maps/state-inventory.md'; $ownership = Read-Utf8 'docs/architecture-migration/maps/state-ownership.md'; $characterization = Read-Utf8 'docs/architecture-migration/maps/characterization-tests.md'; $compatibility = Read-Utf8 'docs/architecture-migration/maps/persistence-compatibility.md'; $reconciliation = Read-Utf8 'docs/architecture-migration/evidence/audit-reconciliation.md'
$inventoryRows = @([regex]::Matches($inventory, '(?m)^\| `ST-\d{3}` \|.*$')); Assert-F2 ($inventoryRows.Count -eq 27) 'INVENTORY-COUNT' "$($inventoryRows.Count)"
foreach ($row in $inventoryRows) { $cells = @($row.Value -split '\|'); Assert-F2 ($cells.Count -eq 14) 'INVENTORY-COLUMNS' $row.Value.Substring(0,30); foreach ($cell in $cells[2..12]) { Assert-F2 (-not [string]::IsNullOrWhiteSpace($cell.Trim())) 'INVENTORY-MANDATORY' $row.Value.Substring(0,30) }; Assert-F2 ($row.Value -match '\| (covered|partial|missing|blocked) \|$') 'INVENTORY-COVERAGE' $row.Value.Substring(0,30) }
$ownershipIds = @([regex]::Matches($ownership, '(?m)^\| `(ST-\d{3})` \|') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique); Assert-F2 ((Compare-Object $ownershipIds @($stateIds | Sort-Object)).Count -eq 0) 'OWNERSHIP-MODEL-SOT' 'state ownership filter'
$charRows = @([regex]::Matches($characterization, '(?m)^\| `(CF-\d{3})` \|.*$')); Assert-F2 ($charRows.Count -eq 22) 'CHARACTERIZATION-COUNT' "$($charRows.Count)"; foreach ($row in $charRows) { Assert-F2 ($row.Value -match '\| (covered|partial|missing|blocked) \|') 'CHARACTERIZATION-STATUS' $row.Value.Substring(0,30); Assert-F2 ($row.Value -match 'ContextChanged=.*StateChanged=.*calculator=.*Results=.*dirty=') 'CHARACTERIZATION-COUNTERS' $row.Value.Substring(0,30) }
$ppRows = @([regex]::Matches($compatibility, '(?m)^\| PP-\d{3} \|.*$')); Assert-F2 ($ppRows.Count -eq 122) 'PERSISTENCE-MATRIX-COUNT' "$($ppRows.Count)"; foreach ($row in $ppRows) { Assert-F2 ((@($row.Value -split '\|').Count) -eq 17) 'PERSISTENCE-MATRIX-COLUMNS' $row.Value.Substring(0,30); Assert-F2 ($row.Value -match '\| (verified|derived|degraded) \| (current|legacy|seam|unmapped/derived) \|$') 'PERSISTENCE-MATRIX-ENUMS' $row.Value.Substring(0,30) }
$recRows = @([regex]::Matches($reconciliation, '(?m)^\| `REC-\d{3}` \|.*$')); Assert-F2 ($recRows.Count -eq 45) 'RECONCILIATION-COUNT' "$($recRows.Count)"; foreach ($row in $recRows) { Assert-F2 ($row.Value -match '\| (confirmed|changed|not-reproducible|not-applicable) \|') 'RECONCILIATION-ENUM' $row.Value.Substring(0,30) }

# Semantic current-claim review: check actual source for two current claims and target/historical labeling rules.
$source = Read-Utf8 'src/ViewModels/Results/ResultsViewModel.cs'; Assert-F2 ($source -match 'ClimateViewModel\s+climateViewModel' -and $source -match 'CircuitsViewModel\s+circuitsViewModel') 'SOURCE-RESULTS-CONCRETE-VM' 'ResultsViewModel constructor'
$orchestrator = Read-Utf8 'src/Services/Project/ProjectLoadOrchestrator.cs'; Assert-F2 ($orchestrator -match 'ClimateViewModel' -and $orchestrator -match 'CircuitsViewModel') 'SOURCE-ORCHESTRATOR-CONCRETE-VM' 'ProjectLoadOrchestrator constructor'
$projectSessionDeclarations = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Recurse -File -Filter '*.cs' | Select-String -Pattern '\b(class|record|interface|struct)\s+ProjectSession\b'); Assert-F2 ($projectSessionDeclarations.Count -eq 0) 'SOURCE-PROJECTSESSION-ABSENT' "count=$($projectSessionDeclarations.Count)"
Assert-F2 (@($model.nodes | Where-Object { $_.name -match 'ProjectSession' -or @($_.snapshots) -contains 'target' }).Count -eq 0) 'TARGET-NOT-CURRENT-MODEL' 'ProjectSession/target record in observed model'
$target = Read-Utf8 'docs/architecture-migration/maps/target-invariants.md'; Assert-F2 ($target -match 'target-only' -and $target -match 'unimplemented' -and $target -notmatch 'ProjectSession.+current implementation') 'TARGET-LABELING' 'target invariants'
foreach ($id in @('REC-004','REC-007','REC-019','REC-020','REC-029','REC-038')) { $line = @($reconciliation -split "`n" | Where-Object { $_.StartsWith('| `' + $id + '` |') }); Assert-F2 ($line.Count -eq 1 -and $line[0] -match '\| (not-reproducible|not-applicable) \|') 'HISTORICAL-LABELING' $id }
$probe = $model | ConvertTo-Json -Depth 12 | ConvertFrom-Json; $probe.snapshots.target = 'observed'; Assert-F2 ($probe.snapshots.target -ne 'unimplemented') 'NEGATIVE-TARGET-AS-CURRENT' 'in-memory mutation detected'
$historicalProbe = 'historical cycle count 14'; $probeClass = if ($historicalProbe -match 'cycle count') { 'not-reproducible' } else { 'unsupported' }; Assert-F2 ($probeClass -eq 'not-reproducible') 'NEGATIVE-HISTORICAL-AS-CURRENT' $probeClass

$canonicalOutput = Invoke-Embedded 'docs/architecture-migration/evidence/model-validation.md'; Assert-F2 ($canonicalOutput -match 'result\s*:\s*pass') 'CANONICAL-VALIDATOR' ($canonicalOutput.Trim())
$fullValidators = @('jsonschema','ajv','check-jsonschema') | Where-Object { Get-Command $_ -ErrorAction SilentlyContinue }; Assert-F2 ($fullValidators.Count -eq 0) 'DRAFT-VALIDATOR-ABSENT' ($fullValidators -join ',')
$result = if ($script:failures.Count -eq 0) { 'APPROVE' } else { 'REJECT' }
[pscustomobject]@{ snapshot_sha=$snapshot; canonical_validator='pass'; nodes=$model.nodes.Count; edges=$model.edges.Count; edge_semantics=$model.edge_semantics.Count; states=$model.state.Count; flows=$model.flows.Count; evidence=$model.evidence.Count; coverage=$model.coverage.Count; map_filters=6; inventory_rows=$inventoryRows.Count; characterization_rows=$charRows.Count; persistence_rows=$ppRows.Count; reconciliation_rows=$recRows.Count; assertions_total=$script:assertions; assertions_passed=$script:assertions-$script:failures.Count; assertions_failed=$script:failures.Count; draft_2020_12='degraded-full-validator-absent; deterministic-pass'; verdict=$result } | Format-List
if ($script:failures.Count -gt 0) { $script:failures | ForEach-Object { "DEFECT $_" }; exit 1 }
exit 0
```

## Observed Post-Write Output

```text
snapshot_sha          : f0d19c34ac03075d64548f1059e9c6626d3596b5
canonical_validator   : pass
nodes                 : 79
edges                 : 112
edge_semantics        : 112
states                : 27
flows                 : 22
evidence              : 11
coverage              : 5
map_filters           : 6
inventory_rows        : 27
characterization_rows : 22
persistence_rows      : 122
reconciliation_rows   : 45
assertions_total      : 2506
assertions_passed     : 2506
assertions_failed     : 0
draft_2020_12         : degraded-full-validator-absent; deterministic-pass
verdict               : APPROVE
```

## Defects

None. The two in-memory negative probes were rejected: target snapshot
mutation is detected and a historical cycle-count claim is classified
`not-reproducible`, never current.

## Terminal Verdict

verdict: APPROVE
