---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T19:13:03.9618828Z
working_directory: D:/IA/ace v.2
commands: [same assertion inspection as characterization-tests.md, targeted production-flow read, read-only PowerShell QA]
exit_code: 0
status: pass
raw_output: Ordered user-action filter over the same capability IDs.
limitations: [Evidence filter, not executable end-to-end run; unasserted counters remain unknown.]
---

# User Flow Evidence Filter

This ordered filter mirrors exactly `CF-001`--`CF-022` in [characterization-tests.md](characterization-tests.md). Future test details are stable `FG-*` references in [user-flow-baseline.md](../evidence/user-flow-baseline.md).

| Order | ID | User action | Observable asserted result | Evidence / status | Current counters `ContextChanged;StateChanged;calculator;Results;dirty` | Future gap |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `CF-001` | Start new/cold project | Results reset clears identity/path and is clean | `ResultsViewModel_Reset_ClearsProjectInfoAndDoesNotMarkDirty`; partial | unknown;unknown;unknown;unknown;0 mock calls | `FG-001` |
| 2 | `CF-002` | Open current `.smc` | DTO fixture fields restore | `Load_v1_Fixture_PreservesCanonicalFields`; partial | unknown;unknown;unknown;unknown;unknown | `FG-002` |
| 3 | `CF-003` | Open legacy `.smc` | missing spacing falls back to 200 | `Load_MissingPipeSpacing_FallsBackToDefault`; partial | unknown;unknown;unknown;unknown;unknown | `FG-003` |
| 4 | `CF-004` | Open a second project | no evidence supports it | missing | unknown;unknown;unknown;unknown;unknown | `FG-004` |
| 5 | `CF-005` | Edit climate | AirTemperature=-28; GetProperties=2 | `UpdateFromClimateModule_TriggersSingleCalculate`; partial | unknown;unknown;1 inferred bounded source-backed;unknown;unknown | `FG-005` |
| 6 | `CF-006` | Edit construction | groundwater change yields LambdaB | `GroundwaterLevelChange_AfterProjectLoad_UpdatesLambdaForBelowPipeLayers`; partial | unknown;unknown;unknown;unknown;unknown | `FG-006` |
| 7 | `CF-007` | Edit thermal | one named ThermalInputs event | `UpdateThermalInputs_RaisesContextChangedEvent`; partial | 1;unknown;unknown;unknown;unknown | `FG-007` |
| 8 | `CF-008` | Edit hydraulics | concentration=40; GetProperties=2 | `OnGlycolConcentrationChanged_TriggersSingleCalculate`; partial | unknown;unknown;1 inferred bounded source-backed;unknown;unknown | `FG-008` |
| 9 | `CF-009` | Change upstream input | downstream results null; no stale hydraulics | `UpdateClimate_ResetsThermalAndHydraulicsResults`; partial | unknown;unknown;unknown;unknown;unknown | `FG-009` |
| 10 | `CF-010` | Calculate | four edits cause GetProperties=8 | `FullWorkflow_ThermalClimateGlycol_TriggersCorrectNumberOfCalculates`; partial | unknown;unknown;4 inferred bounded source-backed;unknown;unknown | `FG-010` |
| 11 | `CF-011` | Reset | one Reset event and cleared context | `Reset_RaisesSingleContextChangedEvent`; covered | 1;unknown;0;unknown;unknown | none |
| 12 | `CF-012` | Repeat reset/load then edit | no evidence supports it | missing | unknown;unknown;unknown;unknown;unknown | `FG-011` |
| 13 | `CF-013` | Save then reload | real DTO file preserves selected fields | `SaveThenLoad_NewProject_RoundTripsFields`; partial | unknown;unknown;unknown;unknown;unknown | `FG-012` |
| 14 | `CF-014` | View summary | independent DTO summaries retain values | `ProjectRoundTrip_TwoCollectors_PreservesPerCollectorSummaries`; partial | unknown;unknown;unknown;unknown;unknown | `FG-013` |
| 15 | `CF-015` | Export PDF | builder receives current values; thermal calculator never | `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`; partial | unknown;unknown;0;1 builder RefreshAll;unknown | `FG-014` |
| 16 | `CF-016` | Export Markdown | real `.md` has mode and methodology | `ExportReportAsync_OperatingMode_CreatesNonEmptyMarkdownWithOperatingLabel`; covered | unknown;unknown;0 isolated service boundary;unknown;unknown | none |
| 17 | `CF-017` | Export Excel | no evidence supports it | missing | unknown;unknown;unknown;unknown;unknown | `FG-015` |
| 18 | `CF-018` | Preview | no evidence supports it | missing | unknown;unknown;unknown;unknown;unknown | `FG-016` |
| 19 | `CF-019` | Print | no evidence supports it | missing | unknown;unknown;unknown;unknown;unknown | `FG-017` |
| 20 | `CF-020` | Decline replacement while dirty | project and dirty state remain | `OpenProject_WhenDirtyAndUserPicksNo_DoesNotLoad`; partial | unknown;unknown;0;0;0 remains dirty | `FG-018` |
| 21 | `CF-021` | Restore under load guard | load ends clean | `ProjectRoundTrip_DoesNotMarkDirtyOnLoad`; partial | unknown;unknown;unknown;unknown;1 clean | `FG-019` |
| 22 | `CF-022` | Navigate/open while clean | no prompt; one load; path set | `OpenProject_WhenClean_DoesNotShowPrompt`; partial | unknown;unknown;0;unknown;1 clean | `FG-020` |
