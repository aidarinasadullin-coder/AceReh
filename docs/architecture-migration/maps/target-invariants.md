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
| `INV-002` | Climate values SHALL have one writable canonical owner in `ProjectSession.ClimateState`; ViewModels and contexts SHALL be adapters or projections. | state-ownership, reactive, persistence | `ST-006..ST-007`; `RE-003`; `PP-006`, `PP-013..PP-020`; Task 4-11 Phase 2 evidence | Writer guard, DI guards, adapter/projection tests, restore/reset routing, persistence/results tests, downstream invalidation guard, targeted Release matrix, and full Release rerun prove the Climate slice contract currently implemented. | verified | None for migrated Climate values. Construction/Thermal/Hydraulics remain separate future slices. |
| `INV-003` | Construction values and layer collections SHALL have one writable canonical owner in `ProjectSession.ConstructionState`. | state-ownership, reactive, persistence | `ST-008..ST-011`; `RE-009`; `COV-PP`; Phase 3 Tasks 4-12.1 and correction | Writer/DI guards, ordered structural equality, adapter/multiplicity, reset/restore, canonical persistence and downstream gates prove one Construction owner and completion boundary. | verified | None for the migrated Construction slice. |

## Phase 3 status overlay

`INV-003` is verified for Construction. The Construction portions of broader
single-owner, adapter, persistence and mutation-boundary invariants are
evidenced, but those invariants remain unverified globally because Thermal,
Hydraulics, Results and shared orchestration retain documented legacy seams.
`INV-008` remains open because `ProjectLoadOrchestrator` still depends on
concrete module ViewModels.
| `INV-004` | Thermal inputs SHALL be owned by `ProjectSession.ThermalState`; thermal results SHALL be derived and SHALL NOT become a second writable input store. | state-ownership, reactive | `ST-012..ST-015`, `ST-021..ST-022`; `RE-001..RE-007`; Phase 4 evidence `docs/architecture-migration/evidence/phase-4-thermal-state/task-3/task-3-thermal-state-contract.md`, `task-6/task-567-merged-boundary.md`, `task-8/task-8-context-hydraulics.md`, `task-11/task-11-ownership-guards.md`, `task-12/task-12-executable-gates.md` | Owner/writer uniqueness proven by the Todo 11 guard suite (8 NegativeFixture categories); every user-visible thermal action crosses the closed state/coordinator mutation boundary with one logical-change completion (41-case characterization); non-user origins distinguished; exact invalidation/recalculation counts asserted in thermal and downstream hydraulics tests; full Release 1946/1943/0/3. | verified | None for the migrated Thermal slice. Hydraulics (`INV-005`) and shared orchestration seams remain open. |
| `INV-005` | Hydraulics inputs and collectors SHALL be owned by `ProjectSession.HydraulicsState`; hydraulic results SHALL be derived. | state-ownership, reactive, persistence | `ST-016..ST-019`, `ST-022`; `RE-001..RE-008`; `COV-PP`; Phase 5 evidence `docs/architecture-migration/evidence/phase-5-hydraulics-state/task-9/divergence-notes.md`, `task-6/correction-notes.md`, `task-8/writer-authority-updates.md`, `task-11/trx-guards-release.json`, `task-12/arithmetic.json` | Owner/writer uniqueness proven by the Todo 11 guard suite (8 NegativeFixture categories); every user-visible hydraulics action commits through the closed slice mutations with User-origin dirty raised by the slice itself; per-attempt status termination asserted; serialized eight-field round-trip characterization proves the exact wire contract; full Release reconciliation 1976 passed / 0 failed / 3 accepted NotExecuted identities. | verified | None for the migrated Hydraulics slice. Shared orchestration seams (`INV-008`) remain open. |
| `INV-006` | Every migrated value SHALL have exactly one writable canonical owner; transitional dual-write paths SHALL be short-lived, compiling, and explicitly recorded as risk. | state-ownership | `ST-001..ST-027`; `maps/state-ownership.md` | Machine-check writer inventory per slice and reject more than one canonical writer at each phase gate. | unverified | Baseline records legacy, seam, split, and ambiguous owners. |
| `INV-007` | ViewModels SHALL be WPF adapters and SHALL NOT serve as shared canonical state stores or required mutation interception points after their slice migrates. | compile-time, di-runtime, state-ownership | `CTN-008..CTN-011`, `CTN-020`; `DRN-017..DRN-021`; `ST-003`, `ST-006..ST-019`, `ST-024..ST-027` | Inspect constructor contracts and writer inventory; run UI adapter characterization tests proving user actions use public state/application mutation boundaries and no future history recorder must intercept ViewModel setters, commands, or internal details. | unverified | Current ViewModels own substantial writable and derived state. |
| `INV-008` | Application services SHALL NOT depend on concrete ViewModels. | compile-time, di-runtime | `CTE-005..CTE-008`; `DRE-032..DRE-035`; `DRN-016` | Static architecture test rejects application-service constructors referencing concrete ViewModel types. | unverified | `ProjectLoadOrchestrator` currently depends on four concrete module ViewModels. |
| `INV-009` | Results SHALL be a derived projection and SHALL NOT own module inputs or become a second canonical store. | compile-time, di-runtime, state-ownership, reactive | `CTN-020`; `DRE-038..DRE-050`, `DRE-055..DRE-057`; `ST-024..ST-027`; `RE-014` | Assert read-only projection contracts, source-state derivation, reset behavior, and exact Results update counts. | unverified | ResultsViewModel currently retains module references and writable projection state. |
| `INV-010` | Every reactive subscription SHALL have explicit owner, lifetime, unsubscribe/disposal rule, and multiplicity expectation; downstream invalidation SHALL consume completed logical changes rather than ViewModel implementation details. | reactive, di-runtime | `RE-001..RE-009`; `maps/reactive.md` records unknown lifetimes and counters | Subscription-lifecycle tests exercise repeated reset/load and assert stable handler/event/calculator counts; per migrated slice, prove one identifiable completion boundary drives downstream invalidation for one logical user action, including an action with multiple internal field changes. | unverified | Several unsubscribe paths and all runtime multiplicities are unknown. |
| `INV-016` | Every migrated state slice SHALL expose explicit state/application mutation boundaries and an identifiable logical-change completion boundary suitable for future Undo/Redo recording. User mutations SHALL be distinguishable from load, reset, restore, and other system apply paths; multiple internal changes MAY form one user action. Snapshot/save and restore SHALL use designated non-user paths; Results SHALL consume completed changes as a derived projection; legacy cleanup SHALL remove bypassing ViewModel mutation paths. No phase is required to implement undo/redo stacks, history persistence, snapshots, or UI commands. | state-ownership, reactive, persistence, user-flow | `INV-002..INV-007`, `INV-009..INV-010`, `INV-012..INV-014`; accepted cross-phase decision in `docs/architecture-migration/TASK_CONTEXT.md` | For each migrated slice, acceptance tests SHALL prove that every user-visible mutation crosses a public state/application mutation boundary and produces one identifiable logical-change completion boundary; load/reset/restore/system apply use distinguishable non-user paths and create no user history candidate; multiple internal field changes can be observed as one user action; no test or future recorder requires interception of ViewModel setters, commands, or internals. Snapshot/save, restore, Results, and legacy-cleanup phase plans SHALL preserve these proofs. | unverified | State slices and their mutation/completion contracts are not yet implemented. |
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
if($invariants.Count-lt16){throw 'invariant coverage'}
if((@($invariants|%{$_.Groups[1].Value}|Sort-Object -Unique)).Count-ne$invariants.Count){throw 'duplicate invariant ID'}
if($decisions.Count-ne6-or(@($decisions|%{$_.Groups[1].Value}|Sort-Object -Unique)).Count-ne6){throw 'decision IDs'}
foreach($match in $invariants){$cells=@($match.Value-split'\|');if($cells.Count-ne9){throw "invariant columns $($match.Groups[1].Value)"};if($match.Value-notmatch'\| (verified|unverified|deferred) \|'){throw "invariant status $($match.Groups[1].Value)"};$viewCell=$cells[3].Trim();foreach($view in @($viewCell-split',\s*')){if($views-notcontains$view){throw "invalid view $view"}}}
foreach($match in $decisions){$cells=@($match.Value-split'\|');if($cells.Count-ne7){throw "decision columns $($match.Groups[1].Value)"};if($match.Value-notmatch'\| (record-only|blocking-for-target|out-of-scope) \|'){throw "decision classification $($match.Groups[1].Value)"}}
foreach($token in 'ProjectSession','ClimateState','ConstructionState','ThermalState','HydraulicsState','one writable canonical owner','WPF adapters','concrete ViewModels','derived projection','subscription','stale','wire format','sequential implementation lane','mutation boundaries','logical-change completion boundary','non-user paths','ViewModel setters','CalculationContext','compatibility','transactional','OpenCode','C# LSP','widget'){if($text-notmatch[regex]::Escape($token)){throw "missing $token"}}
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
invariants                 : 16
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

**DoneClaim TARGET-INVARIANTS-11:** Sixteen measurable target invariants cover
the composite lifecycle and four state slices, canonical ownership, adapter and
dependency boundaries, explicit user/system mutation paths and logical-change
completion boundaries for future Undo/Redo compatibility, Results projection,
reactive lifetime, stale-state and subscription multiplicity, persistence
compatibility, ordered restore, and sequential migration. Six owner-deferred
decisions remain explicitly classified. No target design is presented as
current implementation.

## Phase 1 Status Overlay

`INV-001` and the lifecycle portion of `INV-006` are now verified for the narrow
shell: `ProjectSession` is the sole writable owner of `ProjectNumber`,
`ProjectObject`, `CurrentFilePath`, `IsDirty`, and restore depth/guard. Legacy
interfaces are aliases or forwarding-only surfaces. The shell has no Climate,
Construction, Thermal, Hydraulics, Results, persistence, command, dialog, or
orchestration slice.

All remaining module-slice invariants remain target-only. In particular,
`CalculationContext` is unchanged and `DEC-001` remains open; `DEC-002` remains
open for compatibility duration; `DEC-003` remains open because Phase 1 preserves
partial restore instead of adding transactional rollback. Evidence:
`project-session-contract.md`, `compatibility-adapters.md`, `restore-guard.md`,
`di-runtime.md`, `lifecycle-user-flows.md`, and `final-gates.md`.

## Phase 4 Status Overlay (Task 14)

`INV-004` is verified for the Thermal slice: `ProjectSession.ThermalState`
(`ProjectSessionThermalState`) is the sole writable owner of Thermal inputs,
spacing, last-derived result and status; `ThermalStateCoordinator` is the single
command boundary, dirty-intent owner, DEC-T05 orchestrator and sole upstream
subscriber; `ThermalViewModel`/`CalculationStateService` are adapters;
`CalculationContext` is a single-writer projection bus on the Thermal side;
Results saves/reads canonical via `ThermalPersistenceMapper`; `.smc` wire fields
and version are unchanged. The AMZ-1 transitional mutation
`ApplyNeedsRecalculation` keeps exactly one production caller (compat route),
documented in the journal and guard-proved.

The Thermal portions of the broader single-owner (`INV-006`), adapter
(`INV-007`) and mutation-boundary (`INV-016`) invariants are evidenced by the
same Phase 4 receipts, but those invariants remain unverified globally because
Hydraulics, Results projections and shared orchestration retain documented
legacy seams. `INV-008` remains open because `ProjectLoadOrchestrator` still
depends on concrete module ViewModels (`ProjectLoadOrchestrator.cs:42-51`).
Evidence: `docs/architecture-migration/evidence/phase-4-thermal-state/task-3/task-3-thermal-state-contract.md`,
`task-6/task-567-merged-boundary.md`, `task-9/task-9-lifecycle-restore.md`,
`task-10/task-10-persistence-results.md`, `task-11/task-11-ownership-guards.md`,
`task-12/task-12-executable-gates.md`, `task-13/task-13-user-flow-qa.md`.

## Phase 5 Status Overlay (Task 14)

`INV-005` is verified for the Hydraulics slice: `ProjectSession.HydraulicsState`
(`ProjectSessionHydraulicsState`) is the sole writable owner of hydraulics global inputs,
collectors/circuits, derived results and the status snapshot; `HydraulicsStateCoordinator`
(sealed singleton) is the single command boundary, the sole production writer of the
`CalculationContext` hydraulics results projection, and terminates every calculation attempt with
exactly one unconditional `ResetHydraulicsState` (FIX B). `CircuitsViewModel` is a WPF adapter with
required `IHydraulicsStateCoordinator` + `IProjectSession` constructor parameters and zero
`UpdateHydraulics` calls. Dirty authority lives in the slice (`User` origin raises
`IMarkDirtyService` via `hydraulicsDirtyService ?? this`); auto-recalculation dirty churn is
eliminated because calculation-origin work never dirties. Save maps only the canonical snapshot via
`HydraulicsPersistenceMapper.BuildHydraulicsProjectData`; restore goes only through slice
`Restore(origin=ProjectLoad)`. The DI construction-cycle deadlock was fixed composition-only by an
explicit `ProjectSession` factory registration in `AddResultsModule`.

The Hydraulics portions of the broader single-owner (`INV-006`), adapter (`INV-007`) and
mutation-boundary (`INV-016`) invariants are evidenced by the same Phase 5 receipts, but those
invariants remain unverified globally because Results projections and shared orchestration retain
documented seams. `INV-008` remains open because `ProjectLoadOrchestrator` still depends on concrete
module ViewModels. Evidence: `task-4/di-negative-probe.md`, `task-5/blocker-analysis.md`,
`task-6/correction-notes.md`, `task-7/trx-coordinator-release.json`,
`task-8/writer-authority-updates.md`, `task-9/divergence-notes.md`,
`task-11/trx-guards-release.json`, `task-12/arithmetic.json`, `ui-qa/observations.json`.


## Phase 7 Status Overlay (docs-only refresh)

The "Interpretation Boundary" paragraph above is the historical Phase 0 baseline language; the accepted Phase 7 result is the current interpretation for the restore/report/UI boundary. The Phase 7 receipts verify, for the accepted write-set: one project restore boundary (singleton `ProjectLoadOrchestrator` reached from `ResultsViewModel.LoadProjectDataAsync` under the `BeginProjectRestore()` lease), validation before canonical mutation with deterministic Climate -> Construction -> Thermal -> Hydraulics order, exactly-once calculation publication on valid and failure paths, fresh session/current projection as the report/PDF source of truth, and read-only global catalog behavior on project open. On 2026-09-03 the owner explicitly directed that fulfilled invariants be marked as fulfilled in the canonical model. Accordingly, the canonical `maps/architecture-model.json` status cells were updated to `verified / implemented` for four invariants whose fulfillment is established by the accepted receipts: `INV-001` (ProjectSession composite aggregate root; Phase 1 shell plus the four canonical slices, Phase 7 slice-1 negative probe), `INV-011` (no stale state or multiplied subscriptions across new/load/second-load/reset/repeated-reset; Phase 1 repeated-cycle characterization, Phase 3 task-8 reset/restore, Phase 5 second-load clean replace, Phase 7 slice-7 rejected-restore preservation), `INV-012` (supported `.smc` behavior and wire format remain compatible; unmodified round-trip evidence through Phases 5-7, F1 confirmed no format drift) and `INV-013` (explicit ordered restore coordinator boundary; Phase 7 slice-1/slice-3/slice-7). The INV-001 flip required the owner-approved companion amendment to `widget/verify-widget.mjs` recorded in the same receipt: the runtime suite used `INV-001` as the unverified-invariant exemplar in the `changed-unverified` and `added-survivor-unverified` assertions, and the exemplar now points to the genuinely open `INV-008`; the runtime semantics of the verifier are unchanged, and `model-v2`, `runtime-v2` and the widget generation check all pass after the amendment. `INV-006`, `INV-007` remain globally unverified because Results projections and shared orchestration retain documented seams; `INV-008`, `INV-009` and `INV-010` remain open. `INV-008` (`ProjectLoadOrchestrator` concrete ViewModel dependencies), `INV-009` (Results derived cleanup) and the unknown reactive counters remain open; transactional restore (`DEC-003`) stays deferred. Evidence: `slice-1-restore-boundary.md` through `slice-8-dossier-alignment.md`, `final-f1-scope-provenance.md`, `final-f4-consolidated-stop.md`, `owner-result-acceptance.md`; model records `EV-P7-SCOPE`, `EV-P7-ACCEPTANCE`.

Phase 7.5 docs-only dossier refresh (plan `docs/architecture-migration/plans/phase-7.5-project-restore-coordinator-relaunch.md`, owner-approved 2026-09-03, worktree `D:/IA/ace — копия`); this overlay adds no production or test claim beyond the accepted Phase 7 receipts.

## Phase 8 Results-Derived-Projection Overlay

`INV-009` (Results SHALL be a derived projection and not a second canonical store) is verified/implemented for the executed Phase 8 write-set: every projected value has a proven canonical source (slice-2 source map), `ResultsViewModel` holds no `ClimateViewModel`/`ConstructionViewModel`/`ThermalViewModel` references, the read-only projection and fresh-vs-stale sentinel are executable-proven, and the DI graph resolves without the removed references. `INV-016` Results clause: Results consumes completed changes as a derived projection (partial — shared `CircuitRow` display objects remain a Phase 9 legacy-cleanup item). `INV-008`, `INV-010` remain open. `ColdPeriodDays` canonicalization follows owner decision B (Amendment 1). Evidence: `slice-1..slice-7` receipts; model records `EV-P8-*`.

## Phase 9 Legacy-Seams-Cleanup Overlay

`INV-008` (Application services SHALL NOT depend on concrete ViewModels) is verified/implemented: `ProjectLoadOrchestrator` depends only on application-owned `IProjectLoad*Adapter` interfaces; `ResultsPdfDataBuilder` reads via `IReport*Source` on the same singletons; the static architecture test `ApplicationServiceViewModelDecouplingTests` scans all `SnowMeltingCalculator.Services.*` constructors and was demonstrated RED on the pre-slice violating code and GREEN after (TRX `slice-5-static-test-RED/GREEN.trx`). Phase 7 restore contracts re-proven unchanged (validation order, validate-first, exactly-once publication, rejected-restore preservation). `INV-016` Results clause closed (no bypassing ViewModel mutation path from Results); the invariant's broader mutation-boundary portions remain open. `INV-006`/`INV-007`: progress — Results and the restore orchestrator no longer create shared seams; global closure remains blocked by the still-open `INV-010` reactive counters and remaining legacy owner cleanups. `INV-010` untouched except the pending verifier exemplar question (below). Verifier exemplar: `widget/verify-widget.mjs` still cites `INV-008` in its synthetic unverified-invariant scenarios (lines 33-34); both suites PASS with the verified INV-008 (exemplar refs are set synthetically), but the Phase 7.5 precedent expects the exemplar to be a genuinely open invariant (→ `INV-010`). The re-point was subsequently owner-authorized and executed (2026-09-03): exemplar now cites `INV-010`; both suites PASS; see `evidence/phase-9-legacy-seams-cleanup/verifier-exemplar-amendment.md` (which also records the model-consistency fix for the INV-008 status flip and supersedes earlier Phase 9 model/widget hashes). Evidence: `slice-3..slice-7` receipts; model records `EV-P9-SLICE-2..7`; model hash `fddf315226eb07da7a980ffdc2823e33e06746f583ad88223b8d4400c5529c34`.
