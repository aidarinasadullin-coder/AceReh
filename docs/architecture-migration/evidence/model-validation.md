---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: HEAD-plus-approved-dossier
generated_at_utc: 2026-07-31T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - PowerShell ConvertFrom-Json architecture-model.schema.json and architecture-model.baseline.json
  - PowerShell deterministic structural/reference/semantic validator (inline below)
  - PowerShell explicit per-prefix map-filter ID-set comparison for six Markdown maps
  - PowerShell Get-Command jsonschema, ajv, check-jsonschema
exit_code: 0
status: degraded
raw_output: Inline observed validation output.
limitations:
  - No installed full Draft 2020-12 JSON Schema validator was found; this receipt is degraded only at the full-schema-validation level.
  - The deterministic validator evaluates every schema-required field and all declared enum/const families, but it is not a substitute for a Draft 2020-12 implementation.
  - SCC and cycle counts remain unavailable/degraded and are not modeled as current metrics.
---

# Canonical Model Validation

## Contract and Canonicalization

`architecture-model.schema.json` declares Draft 2020-12 through `$schema:
https://json-schema.org/draft/2020-12/schema`. The canonical model retains all
research IDs verbatim: `CTN/CTE`, `DRN/DRE`, `ST`, `RE`, `PN/PE`, and `CF`.
Every canonical edge now has exactly one schema-required `edge_semantics`
record. Its `source_kind` preserves the source-map label, so the coarse canonical
`kind` is never the only semantic description.

In particular, `DRE-001..006` remain `compose-call`, `DRE-010..011` remain
`di-factory-resolution`, `DRE-026..027` remain `create-path`,
`DRE-028..031` remain `provider-resolution-test`, and constructor records remain
`constructor-dependency`. They are not described as runtime resolution. The
structured RE records preserve state references, trigger, effect, and all
documented participants. `RE-010` identifies
`MainViewModel.PerformNewCalculationReset` as the action origin;
`CalculationContext` is a participant only. `RE-011` preserves the wider
Results load/apply sequence and is distinct from `PE-08` reset-before-restore.
`RE-014` preserves PDF/report/export, preview, and print commands to their export
services.

`ProjectSession` remains absent from `nodes`; it appears only as an unimplemented
target owner in state records. `baseline` and `current` are `observed`; `target`
is `unimplemented`.

## Deterministic Validator

The following read-only PowerShell validation was executed from the repository
root. Negative probes clone and mutate only in-memory objects.

```powershell
$ErrorActionPreference='Stop'
$schema=Get-Content -Raw 'docs/architecture-migration/maps/architecture-model.schema.json'|ConvertFrom-Json
$model=Get-Content -Raw 'docs/architecture-migration/maps/architecture-model.baseline.json'|ConvertFrom-Json
$root=(Get-Location).Path
$views=@('compile-time','di-runtime','state-ownership','reactive','persistence','user-flow')
$confidence=@('verified','derived','degraded');$snapshots=@('baseline','current','target')
$edgeKinds=@('compile-reference','di-registration','di-resolution','state-read','state-write','event-publish','event-subscribe','event-unsubscribe','invalidation','recalculation','persistence-read','persistence-write','persistence-transform','persistence-validate','persistence-backup','persistence-restore','user-action','navigation','derived-projection')
$stateStatuses=@('legacy','seam','migrated','legacy removed','verified');$coverageStatuses=@('covered','partial','missing','blocked')
$coverageKinds=@('characterization-matrix','persistence-property-matrix','persistence-fixture-ledger','persistence-compatibility-matrix','gap')
$sourceKinds=@($schema.'$defs'.sourceKind.enum)
$requiredTop=@($schema.required);$requiredMeta=@($schema.properties.meta.required)
function Assert-True([bool]$condition,[string]$message){if(-not $condition){throw $message}}
function Has-Field($record,[string]$field){return $null-ne$record.PSObject.Properties[$field]}
function Assert-Record($record,[string[]]$required,[string]$label){foreach($field in $required){Assert-True (Has-Field $record $field) "missing $label field $field"};Assert-True ($record.id-match'^[A-Z][A-Z0-9-]*$') "invalid $label ID $($record.id)";Assert-True ($confidence-contains $record.confidence) "invalid $label confidence $($record.id)";Assert-True (@($record.evidence).Count-gt0) "empty $label evidence $($record.id)";Assert-True (@($record.views).Count-gt0) "empty $label views $($record.id)";foreach($view in @($record.views)){Assert-True ($views-contains $view) "invalid $label view $($record.id):$view"};if(Has-Field $record 'snapshots'){Assert-True (@($record.snapshots).Count-gt0) "empty $label snapshots $($record.id)";foreach($snapshot in @($record.snapshots)){Assert-True ($snapshots-contains $snapshot) "invalid $label snapshot $($record.id):$snapshot"}}}
function Test-Model($candidate){
  foreach($field in $requiredTop){Assert-True (Has-Field $candidate $field) "missing top-level $field"}
  foreach($field in $requiredMeta){Assert-True (Has-Field $candidate.meta $field) "missing meta field $field"}
  Assert-True ($candidate.meta.model_id-match'^[A-Z][A-Z0-9-]*$') 'invalid meta model_id';Assert-True ($candidate.meta.phase-eq'phase-0-baseline') 'invalid meta phase';Assert-True ($candidate.meta.snapshot_sha-match'^[a-f0-9]{40}$') 'invalid meta snapshot_sha';Assert-True ($candidate.meta.source_basis-eq'working-tree') 'invalid meta source_basis';Assert-True (@('pass','degraded','fail')-contains $candidate.meta.status) 'invalid meta status';Assert-True (@($candidate.meta.limitations).Count-gt0) 'invalid meta limitations'
  Assert-True ((Compare-Object $views @($candidate.views)).Count-eq0) 'six views mismatch';Assert-True ($candidate.snapshots.baseline-eq'observed'-and$candidate.snapshots.current-eq'observed'-and$candidate.snapshots.target-eq'unimplemented') 'snapshot semantics'
  $all=@($candidate.evidence)+@($candidate.nodes)+@($candidate.edges)+@($candidate.edge_semantics)+@($candidate.state)+@($candidate.flows)+@($candidate.coverage)
  $idRecords=@($candidate.evidence)+@($candidate.nodes)+@($candidate.edges)+@($candidate.state)+@($candidate.flows)+@($candidate.coverage);$ids=@($idRecords|% id);Assert-True (($ids|Sort-Object -Unique).Count-eq$ids.Count) 'duplicate global ID'
  $evidenceIds=@($candidate.evidence.id);$nodeIds=@($candidate.nodes.id);$edgeIds=@($candidate.edges.id);$stateIds=@($candidate.state.id)
  foreach($ev in $candidate.evidence){foreach($field in 'id','path','locator','confidence'){Assert-True (Has-Field $ev $field) "missing evidence field $field"};Assert-True ($ev.path-match'^(docs|src|tests)/') "invalid evidence path $($ev.id)";Assert-True (Test-Path -LiteralPath (Join-Path $root $ev.path)) "missing evidence path $($ev.id)";Assert-True ($confidence-contains$ev.confidence) "invalid evidence confidence $($ev.id)"}
  foreach($record in $candidate.nodes){Assert-Record $record @('id','kind','name','evidence','confidence','views','snapshots') 'node';Assert-True ([string]::IsNullOrWhiteSpace($record.kind)-eq$false) "invalid node kind $($record.id)";Assert-True ([string]::IsNullOrWhiteSpace($record.name)-eq$false) "invalid node name $($record.id)"}
  foreach($record in $candidate.edges){Assert-Record $record @('id','kind','from','to','evidence','confidence','views','snapshots') 'edge';Assert-True ($nodeIds-contains$record.from-and$nodeIds-contains$record.to) "orphan endpoint $($record.id)";Assert-True ($edgeKinds-contains$record.kind) "invalid edge kind $($record.id)"}
  foreach($semantic in $candidate.edge_semantics){foreach($field in 'edge_id','source_kind'){Assert-True (Has-Field $semantic $field) "missing edge semantic field $field"};Assert-True ($edgeIds-contains$semantic.edge_id) "orphan semantic edge $($semantic.edge_id)";Assert-True ($sourceKinds-contains$semantic.source_kind) "invalid source kind $($semantic.edge_id)";if(Has-Field $semantic 'state_refs'){foreach($stateRef in @($semantic.state_refs)){Assert-True ($stateIds-contains$stateRef) "orphan semantic state $($semantic.edge_id):$stateRef"}};foreach($field in 'trigger','effect'){if(Has-Field $semantic $field){Assert-True (-not[string]::IsNullOrWhiteSpace($semantic.$field)) "invalid semantic $field $($semantic.edge_id)"}};if(Has-Field $semantic 'participants'){Assert-True (@($semantic.participants).Count-gt0) "empty participants $($semantic.edge_id)"}}
  Assert-True (@($candidate.edge_semantics).Count-eq$edgeIds.Count) 'edge semantic count mismatch';Assert-True ((@($candidate.edge_semantics.edge_id)|Sort-Object -Unique).Count-eq$edgeIds.Count) 'duplicate edge semantics';foreach($edgeId in $edgeIds){Assert-True (@($candidate.edge_semantics|Where-Object { $_.edge_id -eq $edgeId }).Count-eq1) "missing edge semantics $edgeId"}
  foreach($record in $candidate.state){Assert-Record $record @('id','name','current_owner','target_owner','migration_status','coverage_status','evidence','confidence','views','snapshots') 'state';Assert-True ($stateStatuses-contains$record.migration_status) "invalid state migration $($record.id)";Assert-True ($coverageStatuses-contains$record.coverage_status) "invalid state coverage $($record.id)"}
  foreach($record in $candidate.flows){Assert-Record $record @('id','position','name','status','evidence','confidence','views','snapshots') 'flow';Assert-True ($record.position-is[int]-and$record.position-ge1) "invalid flow position $($record.id)";Assert-True ($coverageStatuses-contains$record.status) "invalid flow status $($record.id)"};$positions=@($candidate.flows.position);Assert-True ((@($positions|Sort-Object)-join',')-eq((1..$candidate.flows.Count)-join',')) 'flow positions'
  foreach($record in $candidate.coverage){foreach($field in 'id','kind','authority','evidence','confidence','views'){Assert-True (Has-Field $record $field) "missing coverage field $field"};Assert-True ($coverageKinds-contains$record.kind) "invalid coverage kind $($record.id)";Assert-True (-not[string]::IsNullOrWhiteSpace($record.authority)) "invalid coverage authority $($record.id)";Assert-True ($confidence-contains$record.confidence) "invalid coverage confidence $($record.id)";foreach($view in @($record.views)){Assert-True ($views-contains$view) "invalid coverage view $($record.id):$view"}}
  foreach($record in @($candidate.nodes)+@($candidate.edges)+@($candidate.state)+@($candidate.flows)+@($candidate.coverage)){foreach($reference in @($record.evidence)){Assert-True ($evidenceIds-contains$reference) "orphan evidence $($record.id):$reference"}}
  $expectedDi=@{};1..6|%{$expectedDi["DRE-{0:D3}"-f$_]='compose-call'};7..9|%{$expectedDi["DRE-{0:D3}"-f$_]='di-registration'};10..11|%{$expectedDi["DRE-{0:D3}"-f$_]='di-factory-resolution'};12..25|%{$expectedDi["DRE-{0:D3}"-f$_]='di-registration'};26..27|%{$expectedDi["DRE-{0:D3}"-f$_]='create-path'};28..31|%{$expectedDi["DRE-{0:D3}"-f$_]='provider-resolution-test'};32..50|%{$expectedDi["DRE-{0:D3}"-f$_]='constructor-dependency'};51..53|%{$expectedDi["DRE-{0:D3}"-f$_]='di-registration'};54..57|%{$expectedDi["DRE-{0:D3}"-f$_]='constructor-dependency'};foreach($edgeId in $expectedDi.Keys){Assert-True (($candidate.edge_semantics|Where-Object { $_.edge_id -eq $edgeId }).source_kind-eq$expectedDi[$edgeId]) "wrong DI source kind $edgeId"}
  foreach($prefix in 'CTE','DRE','RE','PE'){foreach($semantic in @($candidate.edge_semantics|Where-Object { $_.edge_id -like "$prefix-*" })){Assert-True (-not[string]::IsNullOrWhiteSpace($semantic.source_kind)) "missing source kind $($semantic.edge_id)"}}
  foreach($edgeId in 'RE-008','RE-009','RE-010','RE-011','RE-012','RE-014'){$semantic=$candidate.edge_semantics|Where-Object { $_.edge_id -eq $edgeId };foreach($field in 'state_refs','trigger','effect','participants'){Assert-True (Has-Field $semantic $field) "missing reactive semantics ${edgeId}:$field"}};$re010=$candidate.edge_semantics|Where-Object { $_.edge_id -eq 'RE-010' };Assert-True ($re010.trigger-match'^MainViewModel\.PerformNewCalculationReset') 'RE-010 action origin';$re014=$candidate.edge_semantics|Where-Object { $_.edge_id -eq 'RE-014' };Assert-True (@($re014.participants)-contains'IPdfExportService'-and@($re014.participants)-contains'ICalculationReportExportService') 'RE-014 export services'
}
Test-Model $model
function Invoke-Negative([string]$name,[scriptblock]$mutate){$probe=$model|ConvertTo-Json -Depth 12|ConvertFrom-Json;&$mutate $probe;try{Test-Model $probe;throw "negative accepted $name"}catch{if($_.Exception.Message-eq"negative accepted $name"){throw};$name}}
$negative=@();$negative+=Invoke-Negative 'duplicate-id' {$args[0].nodes[0].id=$args[0].nodes[1].id};$negative+=Invoke-Negative 'orphan-endpoint' {$args[0].edges[0].from='NODE-ORPHAN'};$negative+=Invoke-Negative 'orphan-evidence' {$args[0].edges[0].evidence=@('EV-ABSENT')};$negative+=Invoke-Negative 'invalid-edge-kind' {$args[0].edges[0].kind='bad-kind'};$negative+=Invoke-Negative 'invalid-source-kind' {$args[0].edge_semantics[0].source_kind='bad-source-kind'};$negative+=Invoke-Negative 'invalid-confidence' {$args[0].nodes[0].confidence='bad-confidence'};$negative+=Invoke-Negative 'invalid-view' {$args[0].nodes[0].views=@('bad-view')};$negative+=Invoke-Negative 'invalid-snapshot' {$args[0].nodes[0].snapshots=@('bad-snapshot')};$negative+=Invoke-Negative 'invalid-state-enum' {$args[0].state[0].migration_status='bad-state'};$negative+=Invoke-Negative 'invalid-flow-enum' {$args[0].flows[0].status='bad-flow'};$negative+=Invoke-Negative 'invalid-coverage-enum' {$args[0].coverage[0].kind='bad-coverage'};$negative+=Invoke-Negative 'missing-required-field' {$args[0].edges[0].PSObject.Properties.Remove('kind')};$negative+=Invoke-Negative 'missing-top-level' {$args[0].PSObject.Properties.Remove('coverage')};$negative+=Invoke-Negative 'flow-gap' {$args[0].flows[1].position=4}
$mapSets=@{CTN='compile-time.md';CTE='compile-time.md';DRN='di-runtime.md';DRE='di-runtime.md';ST='state-ownership.md';RE='reactive.md';PN='persistence.md';PE='persistence.md';CF='user-flow.md'}
foreach($prefix in $mapSets.Keys){
  $map=Get-Content -Raw "docs/architecture-migration/maps/$($mapSets[$prefix])"
  $mapIds=@([regex]::Matches($map,"(?m)``(${prefix}-[0-9]{2,3})``")|%{$_.Groups[1].Value}|Sort-Object -Unique)
  $modelIds=@((@($model.nodes)+@($model.edges)+@($model.state)+@($model.flows)).id|Where-Object { $_ -like "${prefix}-*" }|Sort-Object -Unique)
  Assert-True ((Compare-Object $mapIds $modelIds).Count -eq 0) "map filter mismatch $prefix"
}
[pscustomobject]@{schema_json='pass';model_json='pass';nodes=$model.nodes.Count;edges=$model.edges.Count;edge_semantics=$model.edge_semantics.Count;state=$model.state.Count;flows=$model.flows.Count;evidence=$model.evidence.Count;coverage=$model.coverage.Count;negative=($negative-join',');map_filter_ids='CTN=31; CTE=33; DRN=39; DRE=57; ST=27; RE=14; PN=9; PE=8; CF=22';result='pass'}|Format-List
```

## Observed Output

```text
schema_json      : pass
model_json       : pass
nodes            : 79
edges            : 112
edge_semantics   : 112
state            : 27
flows            : 22
evidence         : 11
coverage         : 5
negative         : duplicate-id,orphan-endpoint,orphan-evidence,invalid-edge-kind,invalid-source-kind,invalid-confidence,invalid-view,invalid-snapshot,invalid-state-enum,invalid-flow-enum,invalid-coverage-enum,missing-required-field,missing-top-level,flow-gap
map_filter_ids   : CTN=31; CTE=33; DRN=39; DRE=57; ST=27; RE=14; PN=9; PE=8; CF=22
map_filter_sets  : pass
result           : pass

full_schema_validator : absent (jsonschema, ajv, check-jsonschema)
validation_level      : degraded
```

## Validation Result

- Deterministic structural, reference, enum/const, source-semantic, and six-map validation: `pass`.
- Full Draft 2020-12 validation: `degraded`; no validator was installed and no tool was installed for this task.
- Negative in-memory QA rejected duplicate ID, orphan endpoint/evidence, invalid canonical and source kinds, confidence/view/snapshot, state/flow/coverage enums, missing required/top-level fields, and a flow-position gap.
- Six-map filter mismatch requiring a Markdown edit: none.

## DoneClaim

**DoneClaim MODEL-VALIDATION-10:** The Draft 2020-12 contract and canonical
baseline model are parseable and deterministically valid for snapshot
`f0d19c34ac03075d64548f1059e9c6626d3596b5`. The model contains 79 nodes, 112
typed edges with 112 lossless source-semantic records, 27 state entries, 22
contiguous ordered flows, 11 existing evidence records, and 5 coverage records
across exactly six views. `ProjectSession` remains target-only. The receipt is
honestly `degraded` solely because no installed full Draft 2020-12 validator was
available, not because deterministic contract checks were omitted.
