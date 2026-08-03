---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: HEAD-plus-approved-dossier
generated_at_utc: 2026-07-31T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - Read both architecture migration AGENTS files, TASK_CONTEXT.md, approved Todo 11, and the canonical model
  - PowerShell deterministic target-invariant structural validator (inline below)
exit_code: 0
status: pass
raw_output: Inline observed validation output.
limitations:
  - This artifact defines target constraints; it does not assert that ProjectSession or its slices exist in production.
  - Deferred owner decisions remain unresolved and are not compatibility or restore guarantees.
---

# Target Architecture Invariants

## Interpretation Boundary

The canonical baseline remains observed current state. `ProjectSession` and the
four state slices below are target-only and `unimplemented`. Current evidence
identifies the migration seam or present violation; it is not proof that a
target invariant already holds. Production work requires a separately approved
phase, one sequential vertical slice at a time.

## Invariant Matrix

| Invariant ID | Normative statement | Affected views | Current evidence | Later verification method | Status | Blocker |
| --- | --- | --- | --- | --- | --- | --- |
| `INV-001` | `ProjectSession` SHALL be the composite aggregate root for project lifecycle and identity without becoming a flat god object. | state-ownership, di-runtime | `ST-001..ST-005`; `maps/state-inventory.md`; `ProjectSession` absent from current model nodes | Assert a target contract with explicit lifecycle members, DI ownership, and no module input flattening; compile and run lifecycle characterization tests. | unverified | Target type is unimplemented. |
| `INV-002` | Climate values SHALL have one writable canonical owner in `ProjectSession.ClimateState`; ViewModels and contexts SHALL be adapters or projections. | state-ownership, reactive, persistence | `ST-006..ST-007`, `ST-020`; `RE-003`; `COV-PP` | Enumerate all writers, assert one canonical writer per migrated value, then run climate edit/load/save/reload characterization tests. | unverified | Current ViewModel/context paths remain writable or ambiguous. |
| `INV-003` | Construction values and layer collections SHALL have one writable canonical owner in `ProjectSession.ConstructionState`. | state-ownership, reactive, persistence | `ST-008..ST-011`; `RE-009`; `COV-PP` | Assert writer uniqueness and collection identity across edit/reset/load/save/reload tests. | unverified | Current model/ViewModel ownership is ambiguous. |
| `INV-004` | Thermal inputs SHALL be owned by `ProjectSession.ThermalState`; thermal results SHALL be derived and SHALL NOT become a second writable input store. | state-ownership, reactive | `ST-012..ST-015`, `ST-021..ST-022`; `RE-001..RE-007` | Assert owner/writer uniqueness and exact invalidation/recalculation counts in thermal and downstream hydraulics tests. | unverified | Current state is split across ViewModel, CalculationStateService, and CalculationContext. |
| `INV-005` | Hydraulics inputs and collectors SHALL be owned by `ProjectSession.HydraulicsState`; hydraulic results SHALL be derived. | state-ownership, reactive, persistence | `ST-016..ST-019`; `RE-002..RE-008`; `COV-PP` | Assert collection ownership, one writer, stale-state absence, and exact calculator/Results update counts. | unverified | Current Circuits collections, input data, and calculator ownership remain ambiguous. |
| `INV-006` | Every migrated value SHALL have exactly one writable canonical owner; transitional dual-write paths SHALL be short-lived, compiling, and explicitly recorded as risk. | state-ownership | `ST-001..ST-027`; `maps/state-ownership.md` | Machine-check writer inventory per slice and reject more than one canonical writer at each phase gate. | unverified | Baseline records legacy, seam, split, and ambiguous owners. |
| `INV-007` | ViewModels SHALL be WPF adapters and SHALL NOT serve as shared canonical state stores after their slice migrates. | compile-time, di-runtime, state-ownership | `CTN-008..CTN-011`, `CTN-020`; `DRN-017..DRN-021`; `ST-003`, `ST-006..ST-019`, `ST-024..ST-027` | Inspect constructor contracts and writer inventory; run UI adapter characterization tests against state interfaces. | unverified | Current ViewModels own substantial writable and derived state. |
| `INV-008` | Application services SHALL NOT depend on concrete ViewModels. | compile-time, di-runtime | `CTE-005..CTE-008`; `DRE-032..DRE-035`; `DRN-016` | Static architecture test rejects application-service constructors referencing concrete ViewModel types. | unverified | `ProjectLoadOrchestrator` currently depends on four concrete module ViewModels. |
| `INV-009` | Results SHALL be a derived projection and SHALL NOT own module inputs or become a second canonical store. | compile-time, di-runtime, state-ownership, reactive | `CTN-020`; `DRE-038..DRE-050`, `DRE-055..DRE-057`; `ST-024..ST-027`; `RE-014` | Assert read-only projection contracts, source-state derivation, reset behavior, and exact Results update counts. | unverified | ResultsViewModel currently retains module references and writable projection state. |
| `INV-010` | Every reactive subscription SHALL have explicit owner, lifetime, unsubscribe/disposal rule, and multiplicity expectation. | reactive, di-runtime | `RE-001..RE-009`; `maps/reactive.md` records unknown lifetimes and counters | Subscription-lifecycle tests exercise repeated reset/load and assert stable handler/event/calculator counts. | unverified | Several unsubscribe paths and all runtime multiplicities are unknown. |
| `INV-011` | New/load/second-load/reset/repeated-reset SHALL leave no stale project state and SHALL NOT multiply subscriptions, recalculations, or Results updates. | reactive, user-flow, state-ownership | `RE-010..RE-012`; `CF-001..CF-012`; `COV-CF` | Add real-file second-load and repeated reset/load characterization tests with exact ContextChanged, StateChanged, calculator, Results, and dirty counters. | unverified | Required capabilities remain partial or missing and exact counters are unknown. |
| `INV-012` | Supported `.smc` read behavior and wire format SHALL remain compatible unless an owner-approved migration explicitly changes the contract. | persistence, user-flow | `PN-01..PN-09`; `PE-01..PE-08`; `COV-PP`, `COV-SMC`, `COV-PC`; `CF-002..CF-003`, `CF-015` | Re-run fixture ledger/hash checks, semantic round-trip tests, compatibility matrix, and save/reload flows for each slice. | unverified | Compatibility duration is owner-deferred; byte identity and transactional restore are not established. |
| `INV-013` | Restore SHALL have an explicit ordered coordinator boundary covering validation, reset, module restore, context propagation, projection refresh, path, guard, and dirty finalization. | persistence, reactive, user-flow | `PE-01..PE-04`, `PE-07..PE-08`; `RE-011..RE-012`; `CF-002..CF-004` | Integration tests assert ordering, guard lifetime, final state, and failure behavior at every boundary. | unverified | Transactional in-memory failure policy is deferred and second-load coverage is missing. |
| `INV-014` | Production ownership migration SHALL proceed in one sequential implementation lane by vertical slice: Climate, Construction, Thermal, Hydraulics, snapshot/save, restore, then Results and legacy cleanup. | state-ownership, reactive, persistence, user-flow | `docs/architecture-migration/AGENTS.md`; approved `plans/phase-0-baseline.md` | Each approved phase plan names one state slice, rollback boundary, invariant checks, build/test gate, and affected user flow. | verified | Process invariant is established by repository instructions; implementation has not begun. |
| `INV-015` | Target facts SHALL remain distinguishable from observed baseline/current facts in the model, maps, evidence, and future widget. | compile-time, di-runtime, state-ownership, reactive, persistence, user-flow | model `snapshots`: baseline/current observed, target unimplemented; `ProjectSession` absent from nodes | Structural validator rejects target-as-current records and widget acceptance tests preserve mode labels. | verified | None for the documentation contract; production target remains unimplemented. |

## Deferred Decisions

These rows classify policy only; they do not resolve it.

| Decision ID | Decision | Classification | Owner / next phase | Blocker |
| --- | --- | --- | --- | --- |
| `DEC-001` | Whether `CalculationContext` remains a seam/facade, moves behind `ProjectSession`, or is replaced. | blocking-for-target | Owner plus the first affected state-slice plan | `ST-020..ST-022` remain shared writable/derived seams. |
| `DEC-002` | Required duration and version range for legacy `.smc` read compatibility. | blocking-for-target | Owner before snapshot/save boundary implementation | Phase 0 proves observed fixtures, not future support duration. |
| `DEC-003` | Whether restore must be transactional in memory when one module fails. | blocking-for-target | Owner before restore coordinator implementation | Current rollback/failure semantics are not established. |
| `DEC-004` | Whether a future OpenCode migration skill lives in the repository or user configuration. | out-of-scope | Owner in a separate tooling task | No product architecture dependency in Phase 0. |
| `DEC-005` | Whether C# LSP installation is authorized before production migration. | record-only | Owner before implementation verification setup | LSP is configured but not installed; Phase 0 prohibits installation. |
| `DEC-006` | When and under which separately approved plan the architecture widget is implemented. | out-of-scope | Owner after accepting the specification and baseline | Todo 11 specifies behavior only; current widget is immutable in Phase 0. |

## Rollback And Phase Boundary

No target invariant authorizes production work. A future slice must name the
allow-listed implementation paths and rollback boundary before edits. Rollback
is path-specific and owner-approved; broad Git reset, clean, checkout, or
restore remains prohibited. A failed invariant/test gate stops that slice and
must not be hidden by a compatibility shim or a second canonical store.

## Deterministic QA

The following PowerShell is read-only. Its negative probes mutate only strings
and a deserialized model copy in memory.

```powershell
$ErrorActionPreference='Stop'
$path='docs/architecture-migration/maps/target-invariants.md'
$text=Get-Content -Raw -LiteralPath $path
$model=Get-Content -Raw -LiteralPath 'docs/architecture-migration/maps/architecture-model.baseline.json'|ConvertFrom-Json
$views=@('compile-time','di-runtime','state-ownership','reactive','persistence','user-flow')
$invariants=@([regex]::Matches($text,'(?m)^\| `(INV-\d{3})` \|(?<row>.*)$'))
$decisions=@([regex]::Matches($text,'(?m)^\| `(DEC-\d{3})` \|(?<row>.*)$'))
if($invariants.Count-lt15){throw 'invariant coverage'}
if((@($invariants|%{$_.Groups[1].Value}|Sort-Object -Unique)).Count-ne$invariants.Count){throw 'duplicate invariant ID'}
if($decisions.Count-ne6-or(@($decisions|%{$_.Groups[1].Value}|Sort-Object -Unique)).Count-ne6){throw 'decision IDs'}
foreach($match in $invariants){$cells=@($match.Value-split'\|');if($cells.Count-ne9){throw "invariant columns $($match.Groups[1].Value)"};if($match.Value-notmatch'\| (verified|unverified|deferred) \|'){throw "invariant status $($match.Groups[1].Value)"};$viewCell=$cells[3].Trim();foreach($view in @($viewCell-split',\s*')){if($views-notcontains$view){throw "invalid view $view"}}}
foreach($match in $decisions){$cells=@($match.Value-split'\|');if($cells.Count-ne7){throw "decision columns $($match.Groups[1].Value)"};if($match.Value-notmatch'\| (record-only|blocking-for-target|out-of-scope) \|'){throw "decision classification $($match.Groups[1].Value)"}}
foreach($token in 'ProjectSession','ClimateState','ConstructionState','ThermalState','HydraulicsState','one writable canonical owner','WPF adapters','concrete ViewModels','derived projection','subscription','stale','wire format','sequential implementation lane','CalculationContext','compatibility','transactional','OpenCode','C# LSP','widget'){if($text-notmatch[regex]::Escape($token)){throw "missing $token"}}
foreach($evidencePath in @([regex]::Matches($text,'`((?:docs/architecture-migration|maps)/[^`]+\.md)`')|%{$_.Groups[1].Value}|Sort-Object -Unique)){if($evidencePath-like'maps/*'){$evidencePath="docs/architecture-migration/$evidencePath"};if(-not(Test-Path -LiteralPath $evidencePath)){throw "missing evidence path $evidencePath"}}
$modelIds=@(@($model.nodes.id)+@($model.edges.id)+@($model.state.id)+@($model.flows.id)+@($model.coverage.id))
$idPattern='(?:CTN|CTE|DRN|DRE|ST|RE|PN|PE|CF|COV)-[A-Z0-9]+'
foreach($id in @([regex]::Matches($text,$idPattern)|%{$_.Value}|Sort-Object -Unique)){if($modelIds-notcontains$id){throw "unresolved model ID $id"}}
foreach($range in [regex]::Matches($text,'(?<prefix>CTN|CTE|DRN|DRE|ST|RE|PN|PE|CF)-(?<start>\d{2,3})\.\.(?:\k<prefix>-)?(?<end>\d{2,3})')){$prefix=$range.Groups['prefix'].Value;$start=[int]$range.Groups['start'].Value;$end=[int]$range.Groups['end'].Value;$width=$range.Groups['start'].Value.Length;foreach($number in $start..$end){$id="{0}-{1}"-f$prefix,$number.ToString("D$width");if($modelIds-notcontains$id){throw "unresolved model range ID $id"}}}
$missingField='| `INV-999` | statement | reactive | evidence | method | | blocker |';if($missingField-match'\| (verified|unverified|deferred) \|'){throw 'negative missing status accepted'}
function Assert-TargetBoundary($candidate){if($candidate.snapshots.target-ne'unimplemented'){throw 'target snapshot presented as current'};if(@($candidate.nodes|?{$_.name-match'ProjectSession'-and$_.snapshots-contains'current'}).Count){throw 'ProjectSession presented as current node'}}
Assert-TargetBoundary $model
$targetProbe=$model|ConvertTo-Json -Depth 12|ConvertFrom-Json;$targetProbe.snapshots.target='observed';try{Assert-TargetBoundary $targetProbe;throw 'negative target-as-current accepted'}catch{if($_.Exception.Message-eq'negative target-as-current accepted'){throw}}
[pscustomobject]@{invariants=$invariants.Count;decisions=$decisions.Count;views=$views.Count;required_concepts='pass';evidence_paths='pass';model_references='pass';negative_missing_status='rejected';negative_target_as_current='rejected';result='pass'}|Format-List
```

Observed output:

```text
invariants                 : 15
decisions                  : 6
views                      : 6
required_concepts          : pass
evidence_paths             : pass
model_references           : pass
negative_missing_status    : rejected
negative_target_as_current : rejected
result                     : pass
```

## DoneClaim

**DoneClaim TARGET-INVARIANTS-11:** Fifteen measurable target invariants cover
the composite lifecycle and four state slices, canonical ownership, adapter and
dependency boundaries, Results projection, reactive lifetime, stale-state and
subscription multiplicity, persistence compatibility, ordered restore, and
sequential migration. Six owner-deferred decisions remain explicitly
classified. No target design is presented as current implementation.
