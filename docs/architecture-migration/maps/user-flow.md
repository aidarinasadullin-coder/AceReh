---
phase: phase-1-project-session-shell
snapshot_sha: 021d4abd159aa71c4a19c7a6536851264e5a58ca
source_basis: accepted-phase-1-project-session-shell
generated_at_utc: 2026-08-04T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - same assertion inspection as characterization-tests.md
  - targeted production-flow read
  - read-only PowerShell QA
  - node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2
  - node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2
  - node docs/architecture-migration/widget/generate-widget.mjs --check
exit_code: 0
status: pass
raw_output: Ordered user-action filter updated for Phase 1 lifecycle shell.
limitations:
  - Evidence filter, not executable end-to-end run; unasserted counters remain unknown.
  - Phase 1 verified lifecycle shell flows only; module edit/calculate/reset counters remain as previously recorded.
---

# User Flow Evidence Filter

This ordered filter mirrors exactly `CF-001`--`CF-022` in [characterization-tests.md](characterization-tests.md). Future test details are stable `FG-*` references in [user-flow-baseline.md](../evidence/user-flow-baseline.md).

| Order | ID | User action | Observable asserted result | Evidence / status | Current counters `ContextChanged;StateChanged;calculator;Results;dirty` | Future gap |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `CF-001` | Start new/cold project | Results reset clears identity/path and is clean | `ResultsViewModel_Reset_ClearsProjectInfoAndDoesNotMarkDirty`; partial | unknown;unknown;unknown;unknown;0 mock calls | `FG-001` |
| 2 | `CF-002` | Open current `.smc` | DTO fixture fields restore | `Load_v1_Fixture_PreservesCanonicalFields`; partial | unknown;unknown;unknown;unknown;unknown | `FG-002` |
| 3 | `CF-003` | Open legacy `.smc` | missing spacing falls back to 200 | `Load_MissingPipeSpacing_FallsBackToDefault`; partial | unknown;unknown;unknown;unknown;unknown | `FG-003` |
| 4 | `CF-004` | Open a second project | Phase 1 characterization replaces lifecycle identity/path with B and clears the guard | `Load_AfterPreviousLoad_UpdatesProjectInfoOnlyOnce`; covered overlay below | asserted by Phase 1 characterization | none for lifecycle shell |
| 5 | `CF-005` | Edit climate | User Climate edit crosses `ProjectSession.ClimateState`; AirTemperature=-28; GetProperties=2 for one Circuits pass; save/reload reads canonical snapshot | `ClimateStateTests`; `ClimateViewModelTests`; `ClimateMultiplicityCharacterizationTests`; `DoubleCalculationPreventionTests.UpdateFromClimateModule_TriggersSingleCalculate`; `ProjectRoundTripTests`; Task 11 targeted/full TRX receipts | one canonical projection/context path; one inferred Circuits recalculation; Results/save/export consume canonical snapshot/projection; user origin marks dirty while load/reset/restore origins remain non-user | `FG-005`; evidence `climate-state-api.md`, `climate-viewmodel-adapter.md`, `downstream-invalidation.md`, `persistence-results.md`, `affected-gates.md` |
| 6 | `CF-006` | Edit construction | scalar/layer/template changes cross one canonical mutation boundary; valid changes invalidate Thermal once; user-visible origins dirty | Phase 3 state, adapter, multiplicity, persistence, lifecycle and Construction-to-Thermal suites | one canonical completion; zero for no-op/rejected/cancelled; lifecycle origins do not dirty | covered; Tasks 3-12.1 and pre-Task 13 correction |

## Phase 3 Construction user-flow overlay

Startup/new-project reset, project restore/second load, scalar and layer edits,
template apply/cancel/failure, save/reload and downstream Thermal invalidation
now use `ProjectSession.ConstructionState`. Task 12.1 proves the seven-layer
default snapshot is immediately available to save and Thermal. Owner manual QA
covers thickness, material, template, groundwater and lambda/override behavior.
The Climate-labelled ProjectLoad indicator remains a separate open defect.
| 7 | `CF-007` | Edit thermal | one canonical mutation + one dirty-intent per changed edit; recalculation indicator exact text | `ThermalMultiplicityCharacterizationTests` (41 cases); `ThermalViewModelTests`; `ThermalStateCoordinatorTests`; Task 13 UI QA step 4 | one canonical completion; context inputs published once; no-op/rejected emit nothing | covered; Phase 4 overlay below |
| 8 | `CF-008` | Edit hydraulics | glycol/length edits commit through `ProjectSession.HydraulicsState` (User origin) and recalculate once via the coordinator; summary card updates observed | canonical state/coordinator suites (`task-2`, `task-7`, `task-9`); Todo 13 UI QA steps 2-5 PASS | one canonical commit; slice raises dirty for user edits; calculation-origin work never dirties | covered; Phase 5 overlay below |
| 9 | `CF-009` | Change upstream input | downstream results null; no stale hydraulics | `UpdateClimate_ResetsThermalAndHydraulicsResults`; partial | unknown;unknown;unknown;unknown;unknown | `FG-009` |
| 10 | `CF-010` | Calculate | four edits cause GetProperties=8 | `FullWorkflow_ThermalClimateGlycol_TriggersCorrectNumberOfCalculates`; partial | unknown;unknown;4 inferred bounded source-backed;unknown;unknown | `FG-010` |
| 11 | `CF-011` | Reset | one Reset event and cleared context | `Reset_RaisesSingleContextChangedEvent`; covered | 1;unknown;0;unknown;unknown | none |
| 12 | `CF-012` | Repeat reset/load then edit | Phase 1 repeats three cycles without handler multiplication | `RepeatedResetLoad_DoesNotMultiplyHandlers`; covered overlay below | asserted by Phase 1 characterization | none for lifecycle shell |
| 13 | `CF-013` | Save then reload | real DTO file preserves selected fields | `SaveThenLoad_NewProject_RoundTripsFields`; partial | unknown;unknown;unknown;unknown;unknown | `FG-012` |
| 14 | `CF-014` | View summary | independent DTO summaries retain values | `ProjectRoundTrip_TwoCollectors_PreservesPerCollectorSummaries`; partial | unknown;unknown;unknown;unknown;unknown | `FG-013` |
| 15 | `CF-015` | Export PDF | builder receives current values; thermal calculator never | `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`; partial | unknown;unknown;0;1 builder RefreshAll;unknown | `FG-014` |
| 16 | `CF-016` | Export Markdown | real `.md` has mode and methodology | `ExportReportAsync_OperatingMode_CreatesNonEmptyMarkdownWithOperatingLabel`; covered | unknown;unknown;0 isolated service boundary;unknown;unknown | none |
| 17 | `CF-017` | Export Excel | no evidence supports it | missing | unknown;unknown;unknown;unknown;unknown | `FG-015` |
| 18 | `CF-018` | Preview | no evidence supports it | missing | unknown;unknown;unknown;unknown;unknown | `FG-016` |
| 19 | `CF-019` | Print | no evidence supports it | missing | unknown;unknown;unknown;unknown;unknown | `FG-017` |
| 20 | `CF-020` | Decline replacement while dirty | project and dirty state remain | `OpenProject_WhenDirtyAndUserPicksNo_DoesNotLoad`; partial | unknown;unknown;0;0;0 remains dirty | `FG-018` |
| 21 | `CF-021` | Restore under load guard | load ends clean and the canonical `ProjectSession` guard exits false | `ProjectRoundTrip_DoesNotMarkDirtyOnLoad`; Phase 1 guard characterization; covered overlay below | asserted outer false -> true -> false transition | none for lifecycle shell |
| 22 | `CF-022` | Navigate/open while clean | no prompt; one load; path set | `OpenProject_WhenClean_DoesNotShowPrompt`; partial | unknown;unknown;0;unknown;1 clean | `FG-020` |

## Phase 1 ProjectSession lifecycle shell overlay

Phase 1 verified the following lifecycle user flows through
`ProjectLifecycleFlowCharacterizationTests` and affected integration tests:

| Flow | Action | Asserted result | Evidence | Status |
| --- | --- | --- | --- | --- |
| `CF-P1-001` | Load project A then project B | Lifecycle identity/path reflect B only; guard false after exit; no stale Results | `Load_AfterPreviousLoad_UpdatesProjectInfoOnlyOnce` | covered |
| `CF-P1-002` | Repeat reset/load cycles (x3) | Stable handler/calculation counts; no duplicate subscriptions | `RepeatedResetLoad_DoesNotMultiplyHandlers` | covered |
| `CF-P1-003` | Edit after load | Exactly one dirty transition; `IsDirty` true | `Edit_AfterLoad_MarksDirtyOnce` | covered |
| `CF-P1-004` | Dirty Yes/No/Cancel on new/close | Save-failure preserves dirty and blocks destructive continuation | `New_WhenDirtyAndSaveFails_KeepsDirty` | covered |

## Phase 3.1 Climate invalidation overlay (Task 11)

Climate user reset and reset-to-city-data use `UserReset` and retain dirty and
compatibility publication semantics. Pre-load and new-calculation resets use
`ProjectLoadReset`; restore uses silent `Load`; these lifecycle flows do not
invalidate a restored Thermal result through Climate compatibility publication.
Task 9 focused Debug and Release each passed `76/76` with no skips or
`NotExecuted`. Task 10 affected/full Release gates passed with the accepted
explicit identities recorded in the Task 11 receipt. DI and persistence are
verified unchanged for these flows.
| `CF-P1-005` | Corrupt/parse failure | Pre-load state untouched; guard false | `Load_WhenParseFails_KeepsPreviousProject` | covered |
| `CF-P1-006` | Injected early/late restore failure | Partial state preserved (no rollback); guard cleared | `Load_WhenRestoreThrows_LeavesPartialStateAndClearsGuard` | covered |

The restore guard now uses `using var restoreScope = _projectSession.BeginProjectRestore()`
in `ResultsViewModel.LoadProjectDataAsync`. Legacy `ICalculationStateService.IsLoadProjectInProgress`
remains a temporary compatibility read-through. Module-level flows (`CF-001` through
`CF-022`) are otherwise unchanged.

Evidence: `docs/architecture-migration/evidence/phase-1-project-session-shell/lifecycle-user-flows.md`,
`restore-guard.md`, `final-gates.md`.

## Phase 4 Thermal user-flow overlay (Task 14)

Thermal user flows now cross the canonical boundary: every edit
(`CF-007`) routes through `ThermalStateCoordinator.ApplyInputEdit` (one canonical
mutation, one dirty-intent, exact indicator texts); Calculate (`CF-010` Thermal
half) is the DEC-T05 orchestration with reentrancy no-op; reset keeps the
frozen adapter-silent observable behavior; save/reload (`CF-013`) and second
load (`CF-004`) read/restore canonical state with zero stale project-A values
(DEC-T08/AMZ-2). Hydraulics (`CF-008`) and Results remain consumers.

The Todo 13 agent-operated UI QA (V9) exercised the full happy flow in ten
numbered steps plus the failure branch against task-owned fixtures — all PASS:
mode/supply/ground/pipe/spacing edits retain the last result and show exactly
`Температура подачи изменена. Требуется пересчёт.`; Calculate refreshes
Hydraulics/Results once; Ctrl+S advances file SHA/timestamp and clears the dirty
marker; relaunch restores edited values; Project B load shows its own inputs
with no project-A result; reset behaves; unknown-pipe load asserts the frozen
fallback pipe/message/status and a cleared restore guard. Evidence:
`task-13/task-13-user-flow-qa.md`, `task-13/ui-qa/observations.json`,
`failure-observations.json`, six screenshots `01-edit.png`..`06-reset.png` and
`07-unknown-pipe.png`. The 17 AutomationIds across ThermalView/CircuitsView/
ResultsView are contract-tested by `ThermalAutomationIdSelectorContractTests`.
Executable gates: `task-12/task-12-executable-gates.md` (full Release
1946/1943/0/3).

## Phase 5 Hydraulics user-flow overlay (Task 14)

Hydraulics user flows now cross the canonical boundary: every edit (`CF-008`) commits through
`ProjectSession.HydraulicsState` closed mutations with User origin (the slice raises dirty, not the
VM); Calculate is a coordinator attempt that publishes context results once and terminates the
status machine unconditionally; reset routes `UserReset` through the slice while load/reset use
`ProjectLoadReset`/`ProjectLoad` origins; save/reload (`CF-013`) and second load (`CF-004`) read/
restore canonical state with zero stale project-A values. Results remain consumers.

The Todo 13 agent-operated UI QA exercised the full happy flow in nine numbered steps � all PASS
(`ui-qa/observations.json`, frozen exe SHA-256 recorded before/after): launch project-a and read
the hydraulics card; glycol edits recalc per fixture math; validation message appears on an invalid
concentration without crashing; circuit length edit updates the summary card; Ctrl+S save > reload
> identical outputs; second load project-b performs a clean replace; reset restores defaults; and
the corrupt `unknown-pipe.smc` fixture fails gracefully into the validation dialog (failure branch,
`ui-qa/failure-observations.json`). Nine screenshots `01-launch-project-a-hydraulics-card.png` ..
`09-unknown-pipe-validation-dialog.png`; accessibility AutomationIds
`HydraulicsGlycolType`, `HydraulicsGlycolConcentration`, `HydraulicsSupplyHeatPercent`,
`HydraulicsCalculateButton`, `HydraulicsValidationMessage`. Executable gates:
`task-12/arithmetic.json` (full Release 1976/0/3 accepted NotExecuted).
