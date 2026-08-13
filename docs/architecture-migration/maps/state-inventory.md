---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T18:38:01.5504380Z
working_directory: D:/IA/ace v.2
commands:
  - codegraph_codegraph_explore lifecycle climate thermal CalculationContext ResultsViewModel flows
  - targeted Read ProjectStateService.cs MainViewModel.cs ResultsViewModel.cs CalculationContext.cs
  - PowerShell read-only structural QA in reactive.md
exit_code: 0
status: pass
raw_output: Current source-backed state inventory.
limitations:
  - Runtime invocation multiplicity and exact reactive counters are unknown unless explicitly asserted.
  - ProjectSession is absent; target owners are migration targets, not current facts.
---

# State Inventory

`ST-` IDs are shared exactly with [state-ownership.md](state-ownership.md). `unknown/not observed` is explicit. All current rows are `legacy` or `seam`; none claims migration completion.

| State ID | State/value group and explicit members/boundary | Current canonical owner | Copies/projections | Writers | Readers | Reactive effects | Persistence | Target owner | Migration status | Current evidence | Test coverage status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ST-001` | Lifecycle identity `ProjectNumber`, `ProjectObject` | `ProjectStateService` through Results pass-through | Results properties, `ProjectData` | Results `Reset`, `LoadProjectDataAsync`, save snapshot; direct service setters possible | Results save/export; project DTO | no observed PropertyChanged for these auto-properties | `ProjectData.ProjectNumber`, `ProjectObject` | `ProjectSession.Lifecycle.Identity` | legacy | `ProjectStateService.cs:17-22`; `ResultsViewModel.cs:1515-1516,1587-1588,1615-1621` | partial |
| `ST-002` | Lifecycle path `CurrentFilePath` | `ProjectStateService` | Main window title | Results `Reset`, SaveAs success, loaded file apply; direct setter | MainViewModel title, SaveProject branch | `PropertyChanged(CurrentFilePath)` | process path, not serialized in `ProjectData` | `ProjectSession.Lifecycle.FilePath` | legacy | `ProjectStateService.cs:27-39`; `ResultsViewModel.cs:732-738,753-756,817-821,1517`; `MainViewModel.cs:149-171` | partial |
| `ST-003` | Lifecycle display mode `IsOperatingMode` | `ResultsViewModel` | `ProjectData.IsOperatingMode` | Results `Reset`, load | Results/export snapshot | property change implementation not observed | saved/restored `ProjectData.IsOperatingMode` | `ProjectSession.Lifecycle.DisplayMode` | legacy | `ResultsViewModel.cs:1519,1582,1615-1621` | partial |
| `ST-004` | Lifecycle dirty `IsDirty` | `ProjectStateService` | Main window title | `MarkDirty`, `MarkClean`; module VMs call interface; Results reset/save/load and Main new flow call clean | MainViewModel, load guard | `PropertyChanged(IsDirty)` only on transition | not persisted | `ProjectSession.Lifecycle.IsDirty` | legacy | `ProjectStateService.cs:45-72`; `MainViewModel.cs:149-225`; Results `:1562,1602,967` | partial |
| `ST-005` | Load/restore guard `IsLoadProjectInProgress` | `CalculationStateService` | Circuits input handler | Results `LoadProjectDataAsync`; other setters unknown | Circuits input handler; pipe-spacing guard | suppresses dirty/recalculation during restore | not persisted | `ProjectSession.Lifecycle.RestoreGuard` | seam | `ResultsViewModel.cs:1577-1607`; `CalculationStateService.cs:117-139`; Circuits `:1125-1133` | partial |
| `ST-006` | Climate persisted inputs `SelectedCity`, `AirTemperature`, `WindSpeed`, `Humidity`, `SnowfallIntensity`, `SelectedZone`, `IsHighRequirements` plus snapshot fields used for projection | `ProjectSession.ClimateState` (`ProjectSessionClimateState`) | `ClimateViewModel` mirror; `ClimateData`/`IClimateData` projection; `CalculationContext.Climate`; Results/export DTO projection | `IProjectSessionClimateState.ApplyCitySelection`, `ApplyIndividualEdit`, `ApplyProjectSnapshot`, `ResetToDefaults`; no production writer outside the state slice is accepted | Results save/reload/export, Thermal, Circuits, Climate UI, project load/reset | `ProjectSessionClimateState.CompleteMutation()` emits one `ClimateData.ApplyProjection()` and one `CalculationContext.UpdateClimate()`; no-op mutations emit none; user origin marks dirty | unchanged `ClimateProjectData` fields; no schema/version change | `ProjectSession.ClimateState` | migrated/verified for Climate slice | `ProjectSession.cs`; `ProjectSessionClimateState.cs`; `IProjectSession.cs`; `ClimateViewModel.cs`; `ClimateData.cs`; `ResultsViewModel.cs`; evidence `climate-state-api.md`, `climate-data-projection.md`, `climate-viewmodel-adapter.md`, `persistence-results.md`, `downstream-invalidation.md`, `di-guards.md`, `affected-gates.md` | covered |
| `ST-007` | Climate reset/UI state `ColdFiveDayTemperature`, `HasUserModifications`, `SearchQuery`, filtered/recent city UI collections | split: project Climate values in `ProjectSession.ClimateState`; `SearchQuery` and filtered/recent UI collections in `ClimateViewModel` adapter | adapter mirror and UI collections only; no persisted second owner | canonical reset/load/restore through `IProjectSessionClimateState`; adapter writes only search UI state | Climate UI and search; Results reads canonical snapshot for project values | load/reset/restore use non-user origins; search clearing/search command remains UI-only and not part of `.smc` | `ColdFiveDayTemperature`, search state, filtered/recent city UI collections remain not newly persisted; `HasUserModifications` is snapshot metadata | `ProjectSession.ClimateState` for project values; WPF adapter for search-only UI | migrated/verified for project Climate values; UI-only state documented | `ClimateViewModel.cs`; `ProjectLoadOrchestrator.cs`; `MainViewModel.cs`; `ResultsViewModel.cs`; evidence `climate-viewmodel-adapter.md`, `restore-reset-routing.md`, `persistence-results.md`, `affected-gates.md` | covered |
| `ST-008` | Construction scalar inputs including groundwater and loads | ConstructionModel/ConstructionViewModel ambiguity | context, Thermal, Results | VM/editor/reset | Thermal, Results | DataChanged, dirty, update calculations | `ConstructionProjectData.GroundwaterLevel`, `HasLoads` | `ProjectSession.ConstructionState` | legacy | `ConstructionViewModel.cs:694-807,834-856`; Results `:1682-1709` | partial |
| `ST-009` | Construction layers above pipe and `Layer` members | VM collection/model ambiguity | Results `Layers` | collection/editor/reset | service, Results | CollectionChanged, dirty, calculations | serialized `ConstructionProjectData.Layers` | `ProjectSession.ConstructionState.LayersAbovePipe` | legacy | `ConstructionViewModel.cs:702-724,834-856`; Results `:1689-1708` | partial |
| `ST-010` | Construction layers below pipe and `Layer` members | VM collection/model ambiguity | Results `Layers` | collection/editor/reset | service, Results | CollectionChanged, dirty, calculations | serialized `ConstructionProjectData.Layers` | `ProjectSession.ConstructionState.LayersBelowPipe` | legacy | `ConstructionViewModel.cs:702-792,834-856`; Results `:1689-1708` | partial |
| `ST-011` | Construction derived `R1`, `R2`, `LambdaE`, calculated lambdas | Construction VM/model ambiguity | Thermal/Results | `UpdateCalculations`, layer changes | Thermal, Results | construction invalidation | saved in `ConstructionProjectData` | derived construction state | legacy | `ConstructionViewModel.cs:800,834-856`; Results `:1683-1688` | partial |
| `ST-012` | Thermal inputs `SelectedMode`, `SupplyTemperature`, `GroundTemperature`, `SelectedPipe`; excludes `PipeSpacing`, Result, ValidationMessage | ThermalViewModel | `CalculationContext.ThermalInputs`, Results | UI, `Reset`, `LoadResult`/restore | calculator, Results, Circuits | thermal calculate updates context inputs | `ThermalProjectData.SelectedMode`, `SupplyTemperature`, `GroundTemperature`, `SelectedPipe` | `ProjectSession.ThermalState.Inputs` | legacy | `ThermalViewModel.cs:321-326,378-406`; Results `:1711-1736` | partial |
| `ST-013` | Thermal pipe spacing `CalculationStateService.PipeSpacing` | guarded CalculationStateService | Thermal/Circuits/Results projection | ThermalViewModel; guarded restore only while `ST-005` true | Construction, Thermal, Circuits, Results | `PipeSpacingChanged` when changed | `ThermalProjectData.PipeSpacing` | `ProjectSession.ThermalState.PipeSpacing` | seam | `CalculationStateService.cs:110-139`; Results `:1714-1718` | partial |
| `ST-014` | Thermal result `Result` / `ThermalCalculationResult` | ThermalViewModel with context copy | `CalculationContext.ThermalResult`, Results values | calculate, `Reset`, `LoadResult` | Circuits, Results | valid context result can calculate hydraulics | `ThermalProjectData.Result` | derived thermal result | legacy | `ThermalViewModel.cs:326-367,378-406`; Results `:1725-1735` | partial |
| `ST-015` | Thermal status `ThermalNeedsRecalculation`, `ThermalIsCalculating`, `ThermalValidationMessage`, VM `ValidationMessage` | CalculationStateService/VM split | thermal UI | state-service methods; VM calculation/reset | Thermal/Circuits | `StateChanged` | not persisted | status seam disposition open | seam | `CalculationStateService.cs:23-70`; Thermal `:314-371,388-389` | partial |
| `ST-016` | Hydraulics global inputs `GlycolType`, `GlycolConcentration`, `SupplySpacing_cm`, `SupplyHeatPercent` | Circuits `InputData` | circuit copies, Results | input handler/reset/restore incomplete | calculator, Results/export | dirty, propagation, calculate | `HydraulicsProjectData` fields | `ProjectSession.HydraulicsState.GlobalInputs` | legacy | Circuits `:1113-1180`; Results `:1739-1744` | partial |
| `ST-017` | Hydraulics collectors, circuits and topology | Circuits `Collectors` | Results lists/cards/export DTO | commands/reset/restore incomplete | calculator, Results/export | collection protocol | serialized collectors/circuits | `ProjectSession.HydraulicsState.Collectors` | legacy | Circuits `:319-404,732-739`; Results `:1745-1814` | partial |
| `ST-018` | Hydraulics derived circuit/collector results | Circuits/calculator ambiguity | Results cards/summaries/context | calculate paths | Results/export | summary publication | serialized circuit results/summaries | derived hydraulics state | legacy | Circuits `:430-459`; Results `:1764-1813` | partial |
| `ST-019` | Hydraulics status `HydraulicsIsCalculating`, validation message | CalculationStateService | Circuits IsCalculating UI | state-service methods | Circuits | StateChanged/UI property | not persisted | status seam disposition open | seam | `CalculationStateService.cs:77-104`; Circuits `:430-459,1202-1206` | partial |
| `ST-020` | CalculationContext `Climate`, `Construction` | CalculationContext | source module objects | `UpdateClimate`, `UpdateConstruction`, `Reset` | Circuits/other consumers | reset thermal/hydraulics results and ContextChanged | not directly persisted | CalculationContext disposition open | seam | `CalculationContext.cs:53-74,142-169,222-230` | partial |
| `ST-021` | CalculationContext `ThermalInputs` | CalculationContext | Thermal VM inputs | `UpdateThermalInputs`; `Reset` does **not** assign it | Circuits | clears hydraulics results, ContextChanged | not directly persisted | disposition open | seam | `CalculationContext.cs:109-112,192-204,222-230` | partial |
| `ST-022` | CalculationContext `ThermalResult`, `HydraulicsResults` | CalculationContext | source VM/calculator results | update methods and Reset | Circuits | invalidation and ContextChanged | not directly persisted | derived context seam | seam | `CalculationContext.cs:76-124,176-217,222-230` | partial |
| `ST-023` | Save/load DTO `ProjectData` snapshot | file boundary DTO | all module/Results materializations | SaveCurrentProject, file deserialize, restore | file service/orchestrator/Results | reset then restore/refresh; counts unknown | JSON: temp write, conditional existing-original `.bak` copy, `File.Move(..., overwrite:true)`; atomicity/crash recovery not established | ProjectSession snapshot adapter | legacy | `ProjectFileService.cs:115-190`; Results `:1613-1817` | partial |
| `ST-024` | Results identity/status `ProjectNumber`, `ProjectObject`, `StatusMessage`, `IsOperatingMode`, `IsDataReady`, `MissingModules` | ResultsViewModel | UI/export | Reset, load, refresh/export | UI/export | property changes; counters unknown | identity values snapshot; status/readiness not observed persisted | derived Results projection | legacy | Results `:1515-1520,1559-1562,1573-1607` | partial |
| `ST-025` | Results numeric derived fields `TotalThermalPower_kW`, `SystemVolume_L`, `PumpFlowRate_m3h`, `PumpHead_kPa`, `ExpansionTankVolume_L`, `SupplyTemperature`, `ReturnTemperature`, `OperatingTemperature`, `GroundTemperature`, `WindSpeed`, `SnowfallIntensity`, `SurfaceTemperature`, `GlycolConcentration`, `TotalPowerDensity`, `R1`, `R2`, `LambdaE`, `PowerUp`, `PowerDown`, `TotalPipeLength`, `RzsCount`, `PumpQ`, `PumpH`, `ExpansionTankV` | ResultsViewModel | PDF/report/export | Reset, RefreshAll/load methods | UI/export | RefreshAll/KPI updates | not observed persisted | derived Results projection | legacy | Results `:1493-1505,1522-1545,986-1055` | partial |
| `ST-026` | Results collections `Layers`, `Collectors`, `Circuits`, `CollectorSpecifications`, `CollectorEquipmentItems`, `HydraulicSummaryCards` | ResultsViewModel | PDF/report/export | Reset, RefreshAll rebuild/load methods | UI/export | cards deliberately clear before later rebuild | not observed persisted | derived Results projection | legacy | Results `:1493-1505,1546-1556` | partial |
| `ST-027` | Results selection `SelectedCollectorIndex`, `CollectorSummary` | ResultsViewModel | UI/export selection | Reset, selection UI | Results UI | selection update | not observed persisted | derived Results projection | legacy | Results `:1547-1559` | partial |

## Risks

- `ST-014`/`ST-022` retain thermal-result/context dual paths; `ST-020`/`ST-021` are explicit context seams.
- `ST-009`/`ST-010`, `ST-017`/`ST-018`, and `ST-023` retain multi-writer or restore ambiguity.
- File replacement mechanics do not prove atomic persistence or crash recovery.

## Phase 1 ProjectSession Shell Overlay

This append-only overlay supersedes only the Phase 0 lifecycle observations. The
shared `architecture-model.json` is the canonical source for the current
`ST-001`, `ST-002`, `ST-004`, and `ST-005` records.

| State IDs | Current canonical owner | Compatibility/projection boundary | Status and evidence |
| --- | --- | --- | --- |
| `ST-001` | `ProjectSession`: `ProjectNumber`, `ProjectObject` | `IProjectInfoService`, `IProjectStateService`, `IMarkDirtyService`, and `ProjectStateService` are aliases or a forwarding-only adapter over the same session. | migrated/covered; `project-session-contract.md`, `compatibility-adapters.md`, `final-gates.md` |
| `ST-002` | `ProjectSession`: `CurrentFilePath` | `ProjectStateService` forwards path reads/writes; UI title remains a projection. | migrated/covered; `compatibility-adapters.md`, `lifecycle-user-flows.md`, `final-gates.md` |
| `ST-004` | `ProjectSession`: `IsDirty` | Legacy dirty aliases forward to the canonical session; the MainWindow dirty prompt remains a reader. | migrated/covered; `project-session-contract.md`, `lifecycle-user-flows.md`, `final-gates.md` |
| `ST-005` | `ProjectSession`: restore depth and `IsLoadProjectInProgress` | `CalculationStateService` reads the session guard and retains only one temporary compatibility lease, not a bool/depth store. | migrated/covered; `restore-guard.md`, `lifecycle-user-flows.md`, `final-gates.md` |

`ST-003` remains owned by `ResultsViewModel`; `ST-006..ST-019` retain their
Climate, Construction, Thermal, and Hydraulics owners. `ST-020..ST-022` remain
`CalculationContext` seams, and `ST-024..ST-027` remain Results projections.
Phase 1 does not move module state, persistence schema, formulas, or UI design.

### ST-021 correction

The Phase 0 wording is retained above as history. The current source fact is:
`CalculationContext.Reset()` does **not** assign `ThermalInputs`; it clears the
documented result-side context values and raises the reset notification. This is
a documentation correction only, not a runtime behavior change. Evidence:
`CalculationContext.cs`, `CalculationContextInvalidationTests`, and
`final-gates.md`.
