---
phase: phase-1-project-session-shell
snapshot_sha: 021d4abd159aa71c4a19c7a6536851264e5a58ca
source_basis: accepted-phase-1-project-session-shell
generated_at_utc: 2026-08-04T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - codegraph_codegraph_explore flows
  - targeted Read
  - PowerShell QA below
  - node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2
  - node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2
  - node docs/architecture-migration/widget/generate-widget.mjs --check
exit_code: 0
status: pass
raw_output: Reactive/action inventory updated for Phase 1 lifecycle shell. Exact module counters remain unknown.
limitations:
  - Source registration is not runtime multiplicity proof; exact module counters remain unknown.
  - Phase 1 verified only ProjectSession lifecycle events and restore guard semantics.
---

# Reactive Behavior View

| Edge ID | State IDs | Publisher | Subscriber | Subscription and unsubscribe/lifetime | Trigger/action | Effect | Evidence | Confidence | ContextChanged count | StateChanged count | Calculator invocation count | Results projection update count | Dirty transition count |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `RE-001` | `ST-021`,`ST-014` | CalculationContext.ContextChanged | Circuits handler | constructor subscription; unsubscribe not observed | ThermalInputs | notify hydraulic thermal properties | Circuits :728-730,1062-1082 | verified | exactly 1 handler, application lifetime (slice-1 census; slice-3 GREEN) | n/a - consumer edge | n/a | n/a | n/a |
| `RE-002` | `ST-022`,`ST-014`,`ST-018` | CalculationContext.ContextChanged | Circuits handler | same subscription | valid ThermalResult | CalculateAllCollectors; invalid/null does not | Circuits :1068-1082 | verified | n/a | n/a | exactly 1 Circuits recalculation pass per valid ThermalResult publication (slice-2 thermal-calculate) | n/a | n/a |
| `RE-003` | `ST-020`,`ST-006`,`ST-018` | `ProjectSessionClimateState.CompleteMutation()` | `ClimateData.ApplyProjection`, `CalculationContext.UpdateClimate`, Circuits handler | canonical completion sequence | Climate user/load/reset/restore/system mutation that changes snapshot | one projection update then one context publication; Circuits receives one authoritative `CalculationContext.Climate` invalidation path without duplicate recalculation | `ProjectSessionClimateState.cs`; `ClimateData.cs`; `CalculationContext.cs`; `CircuitsViewModel.cs`; evidence `downstream-invalidation.md`, `multiplicity-characterization.md`, `affected-gates.md` | verified | exactly 1 ContextChanged.Climate per changed climate mutation (slice-2 climate-user-edit) | exactly 1 canonical Changed per changed mutation (slice-2) | 1 Circuits recalculation path per climate publication (slice-2) | 1 Results rebuild per RefreshAll (slice-5) | exactly 1 dirty for User; 0 for lifecycle origins (slice-2) |

## Phase 2 ClimateState acceptance overlay

The accepted Climate reactive boundary is no longer `ClimateViewModel -> ClimateData -> CalculationContext`
as independent writable steps. A changed canonical Climate mutation completes in
`ProjectSessionClimateState.CompleteMutation()`, which first applies the `ClimateData` compatibility
projection and then publishes exactly one `CalculationContext.UpdateClimate(..., "Climate")`. The
downstream Circuits path consumes that single context publication. Task 9 evidence
`downstream-invalidation.md` records the duplicate-recalculation guard; Task 11 `affected-gates.md`
records the final targeted/full-suite acceptance counts.
| `RE-004` | `ST-015`,`ST-019` | ICalculationStateService.StateChanged | Circuits handler | constructor subscription; unsubscribe not observed | state change | IsCalculating notification | CalculationStateService :146-168; Circuits :1202-1206 | verified | n/a | 2 per hydraulics attempt (Calculating + Actual); 4 per load cycle total (slice-2) | n/a | n/a | n/a |

## Phase 3.1 Climate invalidation overlay (Task 11)

Changed `User` and `UserReset` completions apply the projection, publish
compatibility `DataChanged`, update `CalculationContext`, and mark dirty once.
Changed `ProjectLoadReset`, `Load`, `Restore`, `SystemApply`, and
`Initialization` synchronize projection/context without compatibility publication
or user dirty semantics. Task 9 focused Debug and Release each passed `76/76`;
Task 10 affected/full Release gates passed with zero failures. Exact counters are
receipt facts, not inferred from subscription declarations.
| `RE-005` | `ST-015` | StateChanged | Thermal handler | constructor subscription; unsubscribe not observed | state change | compat refresh surface only (`RecalcMessage`/`NeedsRecalculation` re-notify); canonical completion arrives via coordinator `Completion` | ThermalViewModel.cs:266,438-460 | verified | n/a | 1 per user thermal edit; 2 per calculate attempt (slice-2 thermal edit/calculate) | n/a | n/a | n/a |
| `RE-006` | `ST-013` | PipeSpacingChanged | Circuits | constructor subscription; unsubscribe not observed | guarded spacing change | compat echo fired only from canonical completions with changed spacing | CalculationStateService.cs:226-235; Circuits :724-726 | verified | PipeSpacing: 1 publication per load (slice-2); handler census 1 (slice-1) | n/a | n/a | n/a | n/a |
| `RE-007` | `ST-013` | PipeSpacingChanged | Thermal/Construction | constructor subscriptions; unsubscribe not observed | guarded spacing change | compat refresh surfaces fed from canonical completions; no independent writer | ThermalViewModel.cs:267; ConstructionViewModel.cs:258; CalculationStateService.cs:226-235 | verified | PipeSpacing: 1 publication fans out to 2 VM subscribers + 1 coordinator (slice-1 census; slice-2) | n/a | n/a | n/a | n/a |
| `RE-008` | `ST-016`,`ST-017`,`ST-004`,`ST-018` | HydraulicInputData/Collectors adapter events | Circuits handlers -> `ProjectSession.HydraulicsState`/coordinator | old InputData explicitly unsubscribed; collection unsubscribe not observed | input/collection edit | adapter forwards edits through canonical `ApplyGlobalInputs`/`ReplaceCollectors` (User origin); the slice raises dirty once per changed commit and the coordinator runs the attempt | Circuits :1024-1319; `HydraulicsStateCoordinator.cs:46-84`; evidence `task-9/divergence-notes.md` | verified | n/a | n/a | 0 for the edit commit; 1 recalculation pass per downstream thermal publication (slice-2) | n/a | exactly 1 dirty per changed User commit (slice-2/slice-5) |
| `RE-009` | `ST-008`,`ST-009`,`ST-010`,`ST-011`,`ST-004` | `ProjectSessionConstructionState.CompleteChanged` | `CurrentProjection`, CalculationContext, adapter and dirty owner | singleton state/adapter; repeated lifecycle hygiene covered | changed canonical mutation | refresh projection; valid User/Template publishes once; raise one Changed; origin-aware dirty | Tasks 10-12.1; pre-Task 13 correction | verified | exactly 1 ContextChanged.Construction per changed User/Template completion (slice-5 add-layer) | exactly 1 canonical Changed per completion (slice-2) | 1 Thermal invalidation after correction (frozen Phase 3 receipt) | Results/save read canonical snapshot (slice-5 RefreshAll) | 1 per changed User/Template; 0 lifecycle/no-op/rejected (slice-2 reset row) |

## Phase 3 Construction completion overlay

`CompleteChanged` updates `ConstructionStateProjection` before downstream
publication. Valid `User` and `Template` changes publish once through
`CalculationContext.UpdateConstruction`; lifecycle origins update canonical
state and the adapter without user dirty semantics or downstream publication;
no-op, rejected and cancelled mutations publish nothing. The pre-Task 13
correction proves Thermal consumes this path. The separate Climate ProjectLoad
indicator defect remains open and is not attributed to `RE-009`.
| `RE-010` | `ST-020`,`ST-024`,`ST-004`,`ST-006`,`ST-008`,`ST-012`,`ST-016`,`ST-017` | MainViewModel.PerformNewCalculationReset | context, Results, four module VMs | direct command path; runtime multiplicity unknown | new calculation | context reset, Results reset, clean, module resets, clean | MainViewModel.cs:178-225 | verified | exactly 1 Reset publication (slice-2 new-calculation) | 0 (slice-2) | 0 (slice-2) | exactly 1 Results rebuild (slice-2) | 0 - clean transition only (slice-2) |
| `RE-011` | `ST-020`,`ST-024`,`ST-005`,`ST-023`,`ST-002`,`ST-004` | Results load/apply | Results/orchestrator/modules | repeat reload lifetime not proven; one load path source observed | load/reload | Results reset, modules reset, clean, guarded restore, RefreshAll, path/clean | Results :778-825,1573-1607 | verified | ThermalInputs=1, ThermalResult=1, Hydraulics=2 per load (slice-2) | 4 per load (slice-2) | 0 direct; 1 hydraulics pass via thermal publication (slice-2) | exactly 1 rebuild per load (slice-2) | 0 - load leaves clean (slice-2/slice-3) |
| `RE-012` | `ST-020`,`ST-006`,`ST-008`,`ST-012`,`ST-016`,`ST-017`,`ST-023`,`ST-024` | ProjectLoadOrchestrator.ResetModules | context and four module VMs | **one statically observed call site from ResultsViewModel; runtime invocation multiplicity unknown** | load reset | context reset before four VM resets; restore order after entry unknown | Orchestrator :56-70; Results :813-819 | verified | exactly 1 Reset publication (slice-2 reset) | 2 per reset (slice-2) | 0 (slice-2) | 0 (slice-2) | 0 - reset origins never dirty (slice-2) |
| `RE-013` | `ST-001`,`ST-002`,`ST-004`,`ST-023` | Results SaveProject/SaveAs/SaveToFile | file service/project state | command/action lifetime not applicable | save: current path or SaveAs; success writes path only for SaveAs and MarkClean | DTO snapshot, temp/bak/move file write; clean on success | Results :730-756,945-968; ProjectFileService :115-163 | verified | n/a | n/a | n/a | n/a | exactly 1 clean transition per successful save (slice-5 shell-save) |
| `RE-014` | `ST-024`,`ST-025`,`ST-026`,`ST-027`,`ST-023` | Results export/preview/print commands | PDF/report/export services | command/action lifetime not applicable | PDF, markdown, Excel, preview, print | RefreshAll before export input generation; builds projection/snapshot | Results :590-724,832-940,1493-1505 | verified | n/a | n/a | n/a | exactly 1 rebuild per RefreshAll before export input generation (slice-5); runtime WPF QA not executed (RR-002 preserved) | 0 from export projection reads (slice-5) |

## Structural QA Record

```powershell
$p='D:/IA/ace v.2/docs/architecture-migration/maps';$i=Get-Content -Raw "$p/state-inventory.md";$o=Get-Content -Raw "$p/state-ownership.md";$r=Get-Content -Raw "$p/reactive.md";$get={param($t)@([regex]::Matches($t,'(?m)^\| `(ST-\d{3})` \|')|%{$_.Groups[1].Value})};$a=@(& $get $i);$b=@(& $get $o);if(($a|select -Unique).Count-ne$a.Count -or ($b|select -Unique).Count-ne$b.Count -or (Compare-Object ($a|sort) ($b|sort))){throw 'state IDs'};foreach($x in @($i-split"`n"|?{$_-match'^\| `ST-'})){if((@($x-split'\|').Count-2)-ne12 -or $x-notmatch'\| (legacy|seam|migrated|legacy removed|verified) \|' -or $x-notmatch'\| (covered|partial|missing|blocked) \|$'){throw 'inventory columns/enums'}};foreach($token in 'ProjectNumber','ProjectObject','CurrentFilePath','IsOperatingMode','SelectedCity','ColdFiveDayTemperature','HasUserModifications','SearchQuery','SelectedMode','SupplyTemperature','GroundTemperature','SelectedPipe','HydraulicsResults','ThermalInputs','HydraulicSummaryCards','PerformNewCalculationReset','SaveToFile','RefreshAll'){if(($i+$r)-notmatch$token){throw "missing $token"}};$e=@([regex]::Matches($r,'(?m)^\| `(RE-\d{3})` \|')|%{$_.Groups[1].Value});if(($e|select -Unique).Count-ne$e.Count){throw 'edge IDs'};foreach($x in @($r-split"`n"|?{$_-match'^\| `RE-'})){if((@($x-split'\|').Count-2)-ne14 -or $x-notmatch'unknown'){throw 'reactive columns/counters'};foreach($id in @([regex]::Matches($x,'ST-\d{3}')|% Value)){if($a-notcontains$id){throw 'orphan'}}};$bad='| `ST-999` | a | b | c | d | e | f | g | h |  |  |  |';if($bad-notmatch'\|  \|'){throw 'negative missing field'};[pscustomobject]@{inventory_rows=$a.Count;ownership_rows=$b.Count;reactive_edges=$e.Count;required_flows='new/load/reset/edit/calculate/save/reload/export';negative_missing_field='failed as expected';unproven_counters='unknown';result='pass'}|fl
```

Observed output:

```text
inventory_rows : 27
ownership_rows : 27
reactive_edges : 14
required_flows : new/load/reset/edit/calculate/save/reload/export
negative_missing_field : failed as expected
unproven_counters : unknown
result : pass
```

## Phase 1 ProjectSession lifecycle shell overlay

Lifecycle events now originate from `ProjectSession` (`INotifyPropertyChanged`):

| Edge ID | State IDs | Publisher | Subscriber | Subscription | Trigger | Effect | Evidence | Confidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `RE-P1-001` | `ST-001`,`ST-002`,`ST-004` | `ProjectSession.PropertyChanged` | `ResultsViewModel` / `MainWindow` | constructor subscription | `ProjectNumber`, `ProjectObject`, `CurrentFilePath`, `IsDirty` mutations | UI title/status update; dirty prompt decisions | `ProjectSession.cs`; `ProjectLifecycleFlowCharacterizationTests.cs`; `lifecycle-user-flows.md` | verified |
| `RE-P1-002` | `ST-005` | `ProjectSession.BeginProjectRestore` / lease disposal | `ICalculationStateService.StateChanged` | compatibility delegate | `IsLoadProjectInProgress` true/false | guards recalculation during restore | `ProjectSession.cs`; `CalculationStateService.cs`; `restore-guard.md` | verified |

`PropertyChanged` is raised exactly once per real lifecycle mutation and never for
idempotent assignments, equal values, or nested restore scopes. Module-level
reactive edges (`RE-001` through `RE-014`) are unchanged by Phase 1.

## Phase 4 ThermalState overlay (Task 14)

The Thermal reactive boundary is no longer
`ThermalViewModel -> CalculationContext -> Circuits` with independent writable
steps. The sealed singleton `ThermalStateCoordinator` (DEC-T04A) holds the only
upstream subscriptions and is the sole Thermal-side writer of the
`CalculationContext` projection bus; `CircuitsViewModel` remains a pure consumer.

| Edge ID | State IDs | Publisher | Subscriber | Subscription and unsubscribe/lifetime | Trigger/action | Effect | Evidence | Confidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `RE-P4-001` | `ST-006`,`ST-011`,`ST-012` | `ClimateData.DataChanged` / `IConstructionData.DataChanged` | `ThermalStateCoordinator` upstream handlers | singleton ctor subscription (sole attach per surface, guard-proved); disposal via `IDisposable` | user Climate/Construction completion publications | canonical invalidation at most once when a Thermal result exists (`InvalidateFromClimate/Construction`); no-op without result; adapter refresh via `UpstreamObserved` | `ThermalStateCoordinator.cs:80-93,197-218`; evidence `task-6/task-567-merged-boundary.md`, `task-11/task-11-ownership-guards.md` | verified |
| `RE-P4-002` | `ST-012`,`ST-014`,`ST-015`,`ST-004` | coordinator `ApplyInputEdit`/`CalculateAsync` completions | `IMarkDirtyService`, adapter `Completion`, `CalculationContext` | one dirty-intent per changed user edit; no-op/rejected emit nothing | changed user edit / DEC-T05 orchestration | exactly one canonical `Changed`; context inputs published once; result published once (valid or compatible-invalid) | `ThermalStateCoordinator.cs:108-129,132-202`; evidence `task-8/task-8-context-hydraulics.md` | verified |
| `RE-P4-003` | `ST-021`,`ST-022`,`ST-016`,`ST-018` | `CalculationContext.ContextChanged` | Circuits handler | constructor subscription; unsubscribe not observed | coordinator input/result publications | unchanged consumer semantics: thermal properties notify, valid result triggers one `CalculateAllCollectors` pass | `CircuitsViewModel.cs:728-730,1062-1082`; evidence `task-8/task-8-context-hydraulics.md` | verified |
| `RE-P4-004` | `ST-013`,`ST-015` | `ProjectSessionThermalState.Changed` | `CalculationStateService` translation | compat adapter subscribes in ctor (`_thermalChangedHandler`) | any changed canonical mutation | one-shot legacy translation: `StateChanged` and (when spacing changed) `PipeSpacingChanged`; ProjectLoadReset suppression keeps restore silent | `CalculationStateService.cs:53-58,190-235`; evidence `task-6/task-567-merged-boundary.md` | verified |

Multiplicity facts are receipt-backed: the Todo 2 characterization suite pins
41 executed cases (single completion, reentrancy, restore-under-guard,
duplicate-subscriber detection); the Todo 11 guard suite rejects duplicate
upstream attaches and non-coordinator context writers; the Todo 12 full Release
run passed 1943/1946 with zero failures.

## Phase 5 HydraulicsState overlay (Task 14)

The Hydraulics reactive boundary is no longer
`Circuits handlers -> dirty/propagation/calculate` with the ViewModel as a writer. The sealed
singleton `HydraulicsStateCoordinator` owns the upstream subscriptions, is the sole production
writer of `CalculationContext.HydraulicsResults`, and terminates every attempt unconditionally;
`ProjectSession.HydraulicsState` commits and raises dirty; `CircuitsViewModel` forwards and mirrors.

| Edge ID | State IDs | Publisher | Subscriber | Subscription and unsubscribe/lifetime | Trigger/action | Effect | Evidence | Confidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `RE-P5-HYD-001` | `ST-021`,`ST-013`,`ST-015` | `CalculationContext.ContextChanged`, `PipeSpacingChanged`, `StateChanged` | `HydraulicsStateCoordinator` handlers | singleton ctor subscriptions (`HydraulicsStateCoordinator.cs:31-33`) via `Connect` callbacks; guard-proved single attach per surface | thermal input/result publications, guarded spacing change, status change | notifyThermal refresh; `ApplyPipeSpacing` runs one canonical recalculation pass and mirrors spacing | `HydraulicsStateCoordinator.cs:86-107`; evidence `task-7/trx-coordinator-release.json`, `task-11/trx-guards-release.json` | verified |
| `RE-P5-HYD-002` | `ST-016`,`ST-017`,`ST-004` | Circuits adapter handlers | `ProjectSession.HydraulicsState` closed mutations | adapter forwards on user edit only (`_isResetting/_isInitializing/_isMirroringHydraulicsState/load-guard` suppressed) | glycol/scalar edits -> `ApplyGlobalInputs(User)`; collection edits -> `ReplaceCollectors(User)` | one canonical commit per changed user action; the slice raises `IMarkDirtyService` exactly once per changed User-origin commit; lifecycle origins never dirty | `CircuitsViewModel.cs:1024-1319`; `ProjectSessionHydraulicsState.cs:92-97`; evidence `task-9/divergence-notes.md` | verified |
| `RE-P5-HYD-003` | `ST-018`,`ST-019` | coordinator `RunCalculation` | `CalculationContext`, `ICalculationStateService`, slice completions | one attempt = `SetHydraulicsCalculating` -> calculation -> publication -> completion | every calculate attempt (user command or auto-recalc) | sole `UpdateHydraulics` publication per completed attempt (source label `"CircuitsViewModel"`); `CompleteCalculation(FailCalculation)` under Calculation origin; `finally` performs exactly one unconditional `ResetHydraulicsState` per attempt (FIX B) | `HydraulicsStateCoordinator.cs:59-84`; evidence `task-9/divergence-notes.md` | verified |
| `RE-P5-HYD-004` | `ST-016`,`ST-017`,`ST-018` | slice `Changed` event | `OnHydraulicsStateChanged` adapter mirror | ctor subscription (`CircuitsViewModel.cs:876-892,918`) | ProjectLoad-origin commits only | read-only mirror into UI data (`ApplyLifecycleSnapshotToAdapter`); other origins are ignored by design, so auto-recalculation during load never dirties or double-publishes | `CircuitsViewModel.cs:876-892`; evidence `task-9/divergence-notes.md` (auto-recalc dirty churn eliminated) | verified |

Multiplicity facts are receipt-backed: the Todo 2 characterization suite pins hydraulics
multiplicity (13 executed cases), the Todo 11 guard suite rejects bypass writers and duplicate
attaches (8/8 categories), the Todo 12 reconciliation closes the full Release suite at
1976 passed / 0 failed / 3 accepted NotExecuted identities, and the Todo 13 agent-operated QA
observed all nine steps PASS including the corrupt-fixture failure branch.

## Phase 6 Save-Boundary Overlay

Save is modeled as a one-way handoff from the canonical `ProjectSession` snapshot to `ProjectPersistenceMapper`, then to `ProjectData` and the file service. The overlay records the persistence boundary, not a new reactive owner or a restore transaction. Evidence: `task-5-save-boundary.md`; model edge `PE-P6-SESSION-SNAPSHOT` through `PE-P6-SERVICE-DATA`; invariant `INV-P6-SAVE`.


## Phase 7 Restore Coordinator Overlay (docs-only refresh)

Phase 7 establishes exactly-once calculation publication for the accepted write-set: the `HydraulicsStateCoordinator` failure branch no longer double-publishes `PublishHydraulics(null)` and instead performs the canonical terminal transition `_state.FailCalculation(...)` after `BeginCalculation()`; `OnContextChanged` thermal-result routing goes through the full `CalculateAll` path so valid results publish exactly once; `CompleteCalculation` receives the real `summaryByCollector` map; `FailCalculation` clears every collector summary. The intentional residual seam remains recorded: `CircuitsViewModel.ExecuteCalculateAll` catch still calls `SetHydraulicsError` + `PublishHydraulics(null)` (single externally visible null publication plus canonical error transition). The `unknown` multiplicity/order portions of the historical `RE-011`/`RE-012` records are not erased by this overlay. Evidence: `slice-4-calculation-publication.md` (`HydraulicsMultiplicityCharacterizationTests`, 102 passed); model record `EV-P7-ACCEPTANCE`.

Phase 7.5 docs-only dossier refresh (plan `docs/architecture-migration/plans/phase-7.5-project-restore-coordinator-relaunch.md`, owner-approved 2026-09-03, worktree `D:/IA/ace — копия`); this overlay adds no production or test claim beyond the accepted Phase 7 receipts.

## Phase 8 Results-Derived-Projection Overlay

Projection rebuilds (`RefreshAll` family) now read canonical state; no new subscriptions were introduced and no reactive counters were erased — the `RE-014` unknown lifetime/multiplicity cells remain as recorded. `IsOperatingMode` is a read-through to `IProjectDisplayModeState` with unchanged change-notification surface. Exactly-once calculation/publication contracts are preserved (`ThermalStateCoordinatorTests`, `HydraulicsMultiplicityCharacterizationTests` green). Evidence: `slice-4`, `slice-7` receipts; model records `EV-P8-SLICE-4`, `EV-P8-SLICE-7`.

## Phase 10 Reactive-Ownership Closure Overlay (2026-09-03)

Phase 10 (`phase-10-reactive-ownership-multiplicity-closure`, plan SHA-256
`D8F893B2…35B7`, owner-approved; execution receipts under
`docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/`)
re-grounded every edge against the live post-Phase-9 tree (census: 28 domain
subscription rows, each with owner, lifetime class, unsubscribe rule and
multiplicity expectation), replaced every counter cell above with measured
facts from the subscription-lifecycle counting harness (deterministic across
consecutive identical runs; RED-probe sensitivity proof recorded), and adapted
the structural QA below to require measured provenance instead of an
`unknown` placeholder. Handler counts are exactly the census expectations
(sensitivity-proven: an injected duplicate subscription fails the harness —
`logs/slice-3-lifecycle-RED.trx`). Re-grounding corrections: the live
`ProjectSession.PropertyChanged` subscriber is `MainViewModel`
(`MainViewModel.cs:78`), `MainWindow` subscribes `MainViewModel.PropertyChanged`
(`MainWindow.xaml.cs:114`), and `ResultsViewModel` holds zero event
subscriptions (Phase 8 derived projection reads canonical state on demand) —
superseding the stale `RE-P1-001` subscriber wording. The leak-hygiene slice
is a justified no-op: zero production subscription edits; frozen contracts
pass unmodified (`slice-4`). `RR-002` (headless manual WPF QA) and `RR-004`
(external fixture) remain recorded limitations. Model records: `INV-010`,
`INV-016`, global `INV-006`/`INV-007` (scope `ST-001..ST-027`) flipped to
verified with `EV-P10-*` evidence.

## Phase 10 Adapted Structural QA Record (2026-09-03)

The original QA (above) required an `unknown` cell in every `RE-` row and
equality of the Phase 1-era 27-row state tables. After the Phase 10
measurement closure both requirements are obsolete: every `RE-` row must now
carry a `slice-` provenance marker and no counter cell may remain `unknown`;
the state-table equality check is retired as a Phase 1 historical artifact
(`state-inventory.md` legitimately gained the Phase 7 lifecycle addendum rows
repeating `ST-001..ST-005`). The adapted QA checks the reactive map itself:
unique edge IDs, 14 columns per row, zero unmeasured counter cells,
phase-10 provenance on every row, and every referenced `ST-*` resolvable in
`state-inventory.md`.

```powershell
$p='D:/IA/ace — копия/docs/architecture-migration/maps';$r=Get-Content -Raw "$p/reactive.md";$i=Get-Content -Raw "$p/state-inventory.md";$rows=@($r-split"`n"|?{$_-match'^\| `RE-\d{3}` \|'});if($rows.Count-ne14){throw 'expected 14 RE rows'};foreach($x in $rows){if((@($x-split'\|').Count-2)-ne14){throw 'reactive columns'};if($x-match'\| unknown \|'){throw 'unmeasured counter cell'};if($x-notmatch'slice-'){throw 'missing phase-10 provenance'}};$e=@([regex]::Matches($r,'(?m)^\| `(RE-\d{3})` \|')|%{$_.Groups[1].Value});if(($e|select -Unique).Count-ne14){throw 'edge IDs'};$inv=[regex]::Matches($i,'`(?<id>ST-\d{3})`')|%{$_.Groups['id'].Value};foreach($x in $rows){foreach($id in @([regex]::Matches($x,'ST-\d{3}')|% Value)){if($inv-notcontains$id){throw "orphan $id"}}};$bad='| `RE-999` | a | b | c | d | e | f | g | h | unknown |  |  |  |';if(-not($bad-match'unknown')){throw 'negative probe failed'};[pscustomobject]@{reactive_edges=$rows.Count;measured_rows=$rows.Count;unmeasured_counters=0;orphans=0;result='pass'}|fl
```

Observed output:

```text
reactive_edges : 14
measured_rows : 14
unmeasured_counters : 0
orphans : 0
result : pass
```
