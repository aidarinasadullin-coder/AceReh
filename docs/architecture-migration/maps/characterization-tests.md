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
| `CF-008` | hydraulics edit | `tests/.../IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs`; `OnGlycolConcentrationChanged_TriggersSingleCalculate`; real Circuits VM, mocks | concentration=40; mocked GetProperties exactly 2 | ContextChanged=unknown; StateChanged=unknown; calculator=1 inferred from two GetProperties calls for one collector calculation; Results=unknown; dirty=unknown | partial | `FG-008` |
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
