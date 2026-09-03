---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T19:13:03.9618828Z
working_directory: D:/IA/ace v.2
commands: [codegraph_codegraph_explore test symbols and production entry points, targeted Read assertions/setup, read-only PowerShell QA in evidence/user-flow-baseline.md]
exit_code: 0
status: pass
raw_output: Assertion-backed matrix; no test was edited or run.
limitations: [TRX green status does not prove user flows, unasserted counters remain unknown, mocks are not real persistence.]
---

# Assertion-Backed Characterization Matrix

Shared capability IDs are `CF-001` through `CF-022`; the decision-complete future specifications `FG-001`--`FG-020` are in [user-flow-baseline.md](../evidence/user-flow-baseline.md). Counters are independent: `ContextChanged; StateChanged; calculator; Results; dirty`.

| ID | Capability | Current test assertion and boundary | Final values/events/stale state | Current counters | Status | Future gap |
| --- | --- | --- | --- | --- | --- | --- |
| `CF-001` | cold/new | `tests/.../ViewModels/ResetOrchestrationTests.cs`; `ResultsViewModel_Reset_ClearsProjectInfoAndDoesNotMarkDirty`; in-memory Results VM, mocks, no persistence | number/object empty; paths null; operating true; clean; `MarkDirty` never | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=0 mock calls | partial | `FG-001` |
| `CF-002` | current `.smc` load | `tests/.../Project/ProjectRoundTripTests.cs`; `Load_v1_Fixture_PreservesCanonicalFields`; checked-in real file, real `ProjectFileService`, DTO only | v1.0; spacing 250; pipe dimensions; R1/R2; one collector/circuit | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=unknown | partial | `FG-002` |
| `CF-003` | legacy `.smc` load | `tests/.../Project/ProjectRoundTripTests.cs`; `Load_MissingPipeSpacing_FallsBackToDefault`; synthetic temp JSON, real serializer/file service, DTO only | missing spacing=200; pipe non-null; R1=.0875; circuit spacing=25 | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=unknown | partial | `FG-003` |
| `CF-004` | second load after first | no evidence supports it | no evidence supports it | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=unknown | missing | `FG-004` |
| `CF-005` | climate edit | `tests/.../IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs`; `ClimateMultiplicityCharacterizationTests.cs`; `ClimateViewModelTests.cs`; `ClimateStateTests.cs`; `ProjectRoundTripTests.cs`; Task 11 TRX receipts | canonical scalar/city/reset mutation through `ProjectSession.ClimateState`; AirTemperature=-28; mocked GetProperties exactly 2 for one Circuits pass; save/reload snapshot from session | canonical completion emits one projection/context path; calculator=1 inferred from two GetProperties calls for one collector calculation; targeted Task 11 gate 330/329/329/0 and full rerun 1616/1613/1613/0 | covered | `FG-005`; evidence `multiplicity-characterization.md`, `climate-state-api.md`, `climate-viewmodel-adapter.md`, `downstream-invalidation.md`, `persistence-results.md`, `affected-gates.md` |
| `CF-006` | construction edit | Phase 3 state, multiplicity, ViewModel, lifecycle, persistence, DI and Construction-to-Thermal suites | changed scalar/layer/template actions produce one ordered canonical completion; no-op/rejected/cancelled produce zero; reset/load use non-user origins | Changed=1 per changed action; downstream at most 1 for valid User/Template; dirty=1 for changed User/Template and 0 for lifecycle/no-op/rejected | covered | Tasks 3-12.1; pre-Task 13 correction |

## Phase 3 Construction coverage overlay

Task 12 recorded green executable Construction/lifecycle/persistence gates;
Task 12.1 added cold-start, immediate save/Thermal, NewCalculation and project
pre-load reset coverage with canonical seven-layer defaults. The pre-Task 13
correction adds accepted Construction-to-Thermal invalidation evidence. These
results supersede only `CF-006`.
| `CF-007` | thermal edit | `tests/.../Core/CalculationContextInvalidationTests.cs`; `UpdateThermalInputs_RaisesContextChangedEvent`; real context | one ThermalInputs event, source Test | ContextChanged=1; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=unknown | partial | `FG-007` |
| `CF-008` | hydraulics edit | `tests/.../HydraulicsMultiplicityCharacterizationTests.cs`; `HydraulicsStateLegacyStoreGuardTests` (8/8); `DoubleCalculationPreventionTests.cs`; Todo 13 UI QA steps 2-5; evidence `task-9/divergence-notes.md`, `task-11/trx-guards-release.json`, `task-12/arithmetic.json` | edits commit through `ProjectSession.HydraulicsState` (User origin, slice-raised dirty); one coordinator attempt per action with unconditional per-attempt status termination; serialized round-trip preserves the eight wire fields | ContextChanged=unknown; StateChanged=unknown; calculator=one attempt per edit (coordinator-bounded); Results=one publication per completed attempt; dirty=1 per changed user commit, raised by the slice | covered | none for ownership; Phase 5 overlay below |
| `CF-009` | invalidation | `tests/.../Core/CalculationContextInvalidationTests.cs`; `UpdateClimate_ResetsThermalAndHydraulicsResults`; seeded context | upstream changes null downstream results; no stale hydraulics | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=unknown | partial | `FG-009` |
| `CF-010` | calculate | `tests/.../IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs`; `FullWorkflow_ThermalClimateGlycol_TriggersCorrectNumberOfCalculates`; real VMs/context, mocks | four edits yield GetProperties exactly 8 | ContextChanged=unknown; StateChanged=unknown; calculator=4 inferred from two GetProperties calls per one collector calculation; Results=unknown; dirty=unknown | partial | `FG-010` |
| `CF-011` | reset | `tests/.../Core/CalculationContextInvalidationTests.cs`; `Reset_RaisesSingleContextChangedEvent`; seeded real context | one Reset event; companion assertion clears four context values | ContextChanged=1; StateChanged=unknown; calculator=0; Results=unknown; dirty=unknown | covered | none: narrow context-reset boundary only; repeated lifecycle is CF-012 |
| `CF-012` | repeated reset/load subscription safety | no evidence supports it | no evidence supports it | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=unknown | missing | `FG-011` |
| `CF-013` | save/reload | `tests/.../Project/ProjectRoundTripTests.cs`; `SaveThenLoad_NewProject_RoundTripsFields`; temp real file, DTO boundary | save true; spacing 300, pipe, R1/R2, circuit spacing 30 | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=unknown | partial | `FG-012` |
| `CF-014` | summary | `tests/.../Project/ProjectRoundTripTests.cs`; `ProjectRoundTrip_TwoCollectors_PreservesPerCollectorSummaries`; temp real file, DTO | summaries retain powers 22700/20700, counts 4, flows/pressures/Kv | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=unknown | partial | `FG-013` |
| `CF-015` | PDF | `tests/.../ViewModels/ResultsViewModelOpenProjectTests.cs`; `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`; DTO/VM + builder | PDF data matches live values; thermal calculator never | ContextChanged=unknown; StateChanged=unknown; calculator=0; Results=1 builder RefreshAll; dirty=unknown | partial | `FG-014` |
| `CF-016` | Markdown export | `tests/.../Services/Reports/Calculation/CalculationReportExportServiceTests.cs`; `ExportReportAsync_OperatingMode_CreatesNonEmptyMarkdownWithOperatingLabel`; temp real file/service | Assert.That true, file exists/nonempty, mode and methodology text | ContextChanged=unknown; StateChanged=unknown; calculator=0 isolated service boundary; Results=unknown; dirty=unknown | covered | none: narrow real Markdown service/file boundary only |
| `CF-017` | Excel export | no evidence supports it | no evidence supports it | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=unknown | missing | `FG-015` |
| `CF-018` | preview | no evidence supports it | no evidence supports it | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=unknown | missing | `FG-016` |
| `CF-019` | print | no evidence supports it | no evidence supports it | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=unknown | missing | `FG-017` |
| `CF-020` | dirty state | `tests/.../ViewModels/ResultsViewModelOpenProjectTests.cs`; `OpenProject_WhenDirtyAndUserPicksNo_DoesNotLoad`; mocked persistence | declined load preserves number, null path, dirty=true | ContextChanged=unknown; StateChanged=unknown; calculator=0; Results=0; dirty=0 remains dirty | partial | `FG-018` |
| `CF-021` | load guard | `tests/.../ViewModels/ResultsViewModelOpenProjectTests.cs`; `ProjectRoundTrip_DoesNotMarkDirtyOnLoad`; in-memory ProjectData | restore ends clean | ContextChanged=unknown; StateChanged=unknown; calculator=unknown; Results=unknown; dirty=1 clean | partial | `FG-019` |
| `CF-022` | navigation | `tests/.../ViewModels/ResultsViewModelOpenProjectTests.cs`; `OpenProject_WhenClean_DoesNotShowPrompt`; dialog/file mocks | prompt never; load once; path set; clean | ContextChanged=unknown; StateChanged=unknown; calculator=0; Results=unknown; dirty=1 clean | partial | `FG-020` |

`unknown` is never zero. `CF-005`, `CF-008`, and `CF-010` are labeled bounded source-backed inferences, not direct calculator invocation observations. Real persistence requires a real `ProjectFileService` and filesystem path.

## Phase 1 ProjectSession Shell Coverage Overlay

The Phase 0 matrix remains historical baseline. Phase 1 adds characterization
coverage without claiming a migration of module state ownership:

| Coverage | Current Phase 1 result | Evidence |
| --- | --- | --- |
| lifecycle owner and alias identity | `ProjectSessionTests` and `ProjectSessionLegacyStoreGuardTests`: 40 passed in the owner/guard lane | `tdd-owner-red.md`, `project-session-contract.md`, `compatibility-adapters.md`, `restore-guard.md` |
| new/load/second-load/edit/repeated reset-load/failure flow | 83 passed, 1 skipped in the flow lane; expected partial restore remains without rollback and the guard clears on every exit | `tdd-flows-red.md`, `lifecycle-user-flows.md` |
| persistence compatibility | 18 passed in the persistence lane; v1.0 and catalogued v1.1 fixture behavior remains accepted | `persistence-compatibility.md`, `lifecycle-user-flows.md`, `final-gates.md` |
| DI lifecycle graph | 8 passed in the DI lane; aliases and lifecycle consumers resolve the canonical session | `di-runtime.md` |

Parent Phase 1 QA also recorded the full Release suite as 1565 passed and 1
skipped. These receipts prove the Phase 1 shell boundary only; they do not turn
unknown Phase 0 module counters into measured facts.

## Phase 4 Thermal characterization overlay (Task 14)

Phase 4 adds assertion-backed Thermal coverage without weakening any prior row:

| Coverage | Current Phase 4 result | Evidence |
| --- | --- | --- |
| canonical state contract | `ProjectSessionThermalStateTests`: closed mutation API, validation/rejection, exhaustive origins, single completion per changed mutation | `task-3/task-3-thermal-state-contract.md` (`trx-state-debug.json`, `trx-state-negative.json`) |
| multiplicity characterization (NEW, 41 executed cases) | `ThermalMultiplicityCharacterizationTests`: single completion, reentrancy, restore-under-guard, upstream invalidation no-op without result, exact context publication order; AMZ-2 updated exactly two rows (`SecondProjectLoad_...UntilTodo9`, `LifecycleResetModules_...`) to DEC-T08 target semantics | `task-2/task-2-thermal-characterization.md` §3; AMZ-2 journal in `TASK_CONTEXT.md` |
| coordinator/adapter/compat service | `ThermalStateCoordinatorTests`, `ThermalViewModelTests`, `CalculationStateServiceTests`/`CalculationStateServiceGuardTests` | `task-6/task-567-merged-boundary.md` (focused gates 72/72, 98/98, 20/20) |
| ownership guards (NEW, 8 NegativeFixture categories) | `ThermalStateLegacyStoreGuardTests`: VM writable stores, service Thermal/spacing stores, orchestrator direct assignment, Results non-canonical save, unapproved context writer, snapshot mutability, duplicate upstream subscriber, independent DI state registration | `task-11/task-11-ownership-guards.md` (V11 TRX) |
| persistence/lifecycle/results | `ThermalPersistenceMapperTests` (exact 8-field wire contract), `ProjectRoundTripTests`, `ResultsViewModelOpenProjectTests`, `ProjectLifecycleFlowCharacterizationTests` | `task-10/task-10-persistence-results.md`; `task-9/task-9-lifecycle-restore.md` |
| full Release closure | 1946 total / 1943 passed / 0 failed / exactly 3 accepted baseline NotExecuted identities | `task-12/task-12-executable-gates.md` (`trx-v6.json`) |

`CF-007` is covered by this overlay; all other `CF-001..CF-022` rows keep their
recorded status. The negative-category manifest was extended under owner-approved
AMZ-3 to CF=4/PF=6/RF=3 with strict lane equality re-proven in Todo 12.

## Phase 5 Hydraulics characterization overlay (Task 14)

Phase 5 adds assertion-backed Hydraulics coverage without weakening any prior row:

| Coverage | Current Phase 5 result | Evidence |
| --- | --- | --- |
| canonical state contract | `ProjectSessionHydraulicsStateTests`: closed mutation API, validation/rejection, exhaustive origins, `Restore` accepts only `ProjectLoad` | `task-3/trx-state-debug.json`, `task-3/divergence-notes.md` |
| multiplicity characterization (13 executed cases) | `HydraulicsMultiplicityCharacterizationTests`: single attempt per action, adapter/coordinator wiring, lifecycle/save shared-session fixtures | `task-2/trx-characterization-release.json`, `task-2/arithmetic.json` |
| correction lane integrity | serialized eight-field round-trip through the production save boundary (`BuildHydraulicsProjectData`) with exact `System.Text.Json` options; 13/13 characterization green after owner-directed rewrite | `task-6/correction-notes.md` |
| ownership guards (8 NegativeFixture categories) | `HydraulicsStateLegacyStoreGuardTests`: VM stores, service stores, orchestrator direct assignment, non-canonical save, unapproved context writer, snapshot mutability, duplicate subscriber, independent DI registration | `task-11/trx-guards-release.json`, refreshed post-fix |
| semantic adjudications | four owner-adjudicated adaptations: slice-raised User dirty, unconditional per-attempt status termination (FIX B), auto-recalc dirty churn eliminated, DI construction-cycle deadlock fix reference | `task-9/divergence-notes.md` |
| full Release closure | reconciliation: 1979 parser outcome rows = 1976 passed / 0 failed / 3 NotExecuted outcome rows, all within the baseline accepted set {RegenerateBaseline, RegenerateCircuitsBaseline, ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile} | `task-12/arithmetic.json` |

`CF-008` is covered by this overlay; all other `CF-001..CF-022` rows keep their recorded status.


## Phase 7 Restore Coordinator characterization overlay (docs-only refresh)

Phase 7 maps accepted receipts to executable coverage without weakening any prior row and without claiming the missing Excel/preview/print flows:

| Coverage | Current Phase 7 result | Evidence |
| --- | --- | --- |
| restore boundary and guard | `ProjectSessionTests` + `ProjectLifecycleFlowCharacterizationTests` + `ProjectSessionLegacyStoreGuardTests`: 38 passed / 0 failed | `slice-1-restore-boundary.md`, `logs/slice-1-restore-boundary.trx` |
| load boundary | `ProjectFileServiceResultTests` + `ProjectFileServiceMutationTests` + `ResultsViewModelOpenProjectTests`: 46 passed / 1 skipped fixture-gate | `slice-2-load-boundary.md`, `logs/slice-2-load-boundary.trx` |
| validation before mutation | `RestoreModulesFromProjectAsync_InvalidThermalInput_DoesNotMutatePriorClimateOrThermalSlices` added; focused suite 119 passed / 0 failed | `slice-3-validation-order.md`, `logs/slice-3-validation-order.trx` |
| calculation publication multiplicity | `HydraulicsMultiplicityCharacterizationTests` incl. `ThermalContextRouting_ValidResultPublishesFreshHydraulicsStateOnce` and `ThermalContextRouting_CalculationFailurePublishesTerminalFailureOnce`: 102 passed / 0 failed | `slice-4-calculation-publication.md`, `logs/slice-4-calculation-publication.trx` |
| report source of truth | `Build_UsesCurrentProjection_WhenPersistedDtoHasStaleSentinel`, `ExportReportAsync_BuildsAndRendersOnce_WithoutMutatingProject`: 42 passed / 0 failed | `slice-5-report-source-of-truth.md`, `logs/slice-5-report-source-of-truth.trx` |
| catalog boundary and restore regressions | 57 passed / 1 skipped fixture-gate plus regression-check run (5 named tests passed) | `slice-6-catalog-boundary.md`, `logs/slice-6-catalog-boundary.trx`, `logs/slice-6-regression-check.trx` |
| DI/UI alignment | `ResultsViewModel_RestorePath_UsesTheSingletonOrchestratorWithCanonicalSessionSlices`, rejected-restore and fresh-UI/PDF assertions: 94 passed / 1 skipped fixture-gate | `slice-7-di-ui-alignment.md`, `logs/slice-7-di-ui-alignment.trx` |

The one repeatedly skipped test is the pre-existing accepted baseline `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` (fixture gate), unchanged by Phase 7. Evidence mapping to dossier statements: `slice-8-dossier-alignment.md`; model records `EV-P7-SLICE-7`, `EV-P7-SLICE-8`.

Phase 7.5 docs-only dossier refresh (plan `docs/architecture-migration/plans/phase-7.5-project-restore-coordinator-relaunch.md`, owner-approved 2026-09-03, worktree `D:/IA/ace — копия`); this overlay adds no production or test claim beyond the accepted Phase 7 receipts.

## Phase 8 Results-Derived-Projection Overlay

Phase 8 receipts: slice 1 baseline 27 passed; slice 2 canonical-source map 69 passed; slice 3 re-sourcing 111 passed/1 known skip; slice 4 thermal/hydraulics 63 passed (frozen adapter-seam contract re-pinned to the canonical `InvalidateFromClimate` equivalent — recorded); slice 5 readiness/display mode 73 passed/1 known skip; slice 6 full regression 2023 passed with 5 pre-existing import-removal baseline failures flagged (outside Phase 8 write-set; owner decision required); slice 7 multiplicity/sentinel 59 passed. New canonical seeding helpers (`ReplaceCollectorsCanonical`) and the `Period0Days` climate tests are part of the frozen write-set. Evidence: `logs/*.trx` under `evidence/phase-8-results-derived-projection/`.

## Phase 9 Legacy-Seams-Cleanup Overlay

Characterization changes, all recorded: (1) LIM-P8-2 owner decision B — the 5 restore-failure/import tests re-pinned (`LoadProjectDataAsync_{Early,Late}RestoreFailure_*` inject the failure at the live `SetPipeSpacing` boundary; `ProjectData_Load_KeepsCatalogsReadOnly_CustomMaterialsStayProjectLocal` asserts catalogs stay read-only); (2) `ConstructionStateLegacyStoreGuard` inventory re-pinned to "no direct VM collection writes" after the dead legacy loader removal; (3) source-pins updated for removed seams (`CalculateFromRestoreAsync`, `_projectSession.MarkClean`); (4) new suites: `ResultsOwnedCircuitProjectionTests` (ownership negative probes), `ApplicationServiceViewModelDecouplingTests` (static INV-008 guard, RED→GREEN). Full regression 2032 passed / 0 failed / 1 known skip. Evidence: `slice-2`, `slice-5`, `slice-7` receipts.
