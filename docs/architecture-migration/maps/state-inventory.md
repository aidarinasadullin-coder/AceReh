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
| `ST-008` | Construction scalar inputs including groundwater and loads | `ProjectSession.ConstructionState` | adapter, `CurrentProjection`, CalculationContext, Results DTO | canonical mutations and lifecycle snapshot apply | adapter, Thermal, Results | `CompleteChanged`: projection, valid downstream publication, Changed, origin-aware dirty | unchanged DTO fields | same | migrated/verified | Tasks 8-12.1 | covered |
| `ST-009` | ordered layers above pipe | `ProjectSession.ConstructionState.LayersAbovePipe` | adapter/projection/DTO copies | canonical add/remove/edit/reorder/template/snapshot operations | adapter, Thermal, Results | one logical completion; identity/order normalized | unchanged `ConstructionProjectData.Layers` | same | migrated/verified | Task 9 recovery; Task 12 | covered |
| `ST-010` | ordered layers below pipe | `ProjectSession.ConstructionState.LayersBelowPipe` | adapter/projection/DTO copies | canonical add/remove/edit/reorder/template/snapshot operations | adapter, Thermal, Results | one logical completion; identity/order normalized | unchanged `ConstructionProjectData.Layers` | same | migrated/verified | Task 9 recovery; Task 12 | covered |
| `ST-011` | Construction derived `R1`, `R2`, `LambdaE`, calculated lambdas | `ConstructionStateProjection` from canonical snapshot | Thermal `IConstructionData`, CalculationContext, Results DTO | canonical completion only | Thermal, Results | valid User/Template completion publishes once | mapped by `ConstructionPersistenceMapper` | same derived projection | migrated/verified | Tasks 10-12.1; correction | covered |
| `ST-012` | Thermal inputs `SelectedMode`, `SupplyTemperature`, `GroundTemperature`, `SelectedPipe` plus spacing member `PipeSpacing`; excludes Result, ValidationMessage | `ProjectSession.ThermalState` (`ProjectSessionThermalState`) | `ThermalViewModel` adapter mirror; `CalculationContext.ThermalInputs` projection; Results DTO via mapper | canonical mutations only: coordinator commands (`ApplyInputEdit`), lifecycle `Restore`, upstream invalidations; no production writer outside the state slice (guard suite) | calculator, Results, Circuits, Thermal UI | coordinator publishes context inputs once per DEC-T05 orchestration; user origin marks dirty exactly once per changed edit | `ThermalProjectData.SelectedMode`, `SupplyTemperature`, `GroundTemperature`, `SelectedPipe`, `PipeSpacing` unchanged; mapped by `ThermalPersistenceMapper` | `ProjectSession.ThermalState` | migrated/verified for Thermal slice | `IProjectSessionThermalState.cs:14-100`; `ProjectSessionThermalState.cs:37-157`; `ThermalStateCoordinator.cs:108-129`; `ThermalViewModel.cs:114-165,227-276`; evidence `task-3/task-3-thermal-state-contract.md`, `task-6/task-567-merged-boundary.md`, `task-11/task-11-ownership-guards.md` | covered |
| `ST-013` | Thermal pipe spacing `PipeSpacing` member of the canonical Thermal inputs snapshot | `ProjectSession.ThermalState` (`Inputs.PipeSpacing`) | legacy read-through surface `ICalculationStateService.PipeSpacing`/`PipeSpacingChanged` (compat translation, zero backing store); Construction/Thermal/Circuits adapters | canonical mutations only; guarded restore applies spacing before adapter finalization; compat setter is a no-op echo when equal | Construction visualization, Thermal UI, Circuits, Results | compat `PipeSpacingChanged` fires only from canonical completions with changed spacing (`CalculationStateService.cs:226-235`) | `ThermalProjectData.PipeSpacing` unchanged; missing value still defaults to `200` on restore | `ProjectSession.ThermalState.PipeSpacing` | migrated/verified; AMZ-1 compat seam documented | `ProjectSessionThermalState.cs:37-54`; `CalculationStateService.cs:84-103,226-235`; `ProjectLoadOrchestrator.cs:132-155`; evidence `task-6/task-567-merged-boundary.md`, `task-9/task-9-lifecycle-restore.md` | covered |
| `ST-014` | Thermal last-derived result `Result` / `ThermalResultSnapshot` | `ProjectSession.ThermalState` (sole writable owner; derived value, not an input store) | `CalculationContext.ThermalResult` projection; Results values; adapter display mirror | coordinator `CompleteCalculation`/`FailCalculation` and lifecycle `Restore` only; upstream invalidation clears it once when present | Circuits, Results | valid completion publishes context result once; failure publishes compatible invalid result once | `ThermalProjectData.Result` exact 8-field wire contract via `ThermalPersistenceMapper.BuildResultProjectData` | derived thermal result owned by `ProjectSession.ThermalState` | migrated/verified | `ProjectSessionThermalState.cs:96-136,160-218`; `ThermalStateCoordinator.cs:132-202`; `ThermalPersistenceMapper.cs:79-98`; evidence `task-8/task-8-context-hydraulics.md`, `task-10/task-10-persistence-results.md` | covered |
| `ST-015` | Thermal status `Phase`/`RecalculationMessage`/`ValidationMessage` (`ThermalStatusSnapshot`) | `ProjectSession.ThermalState` | compat getters `ThermalNeedsRecalculation`/`ThermalIsCalculating`/`ThermalValidationMessage` translated from snapshot; adapter `RecalcMessage`/`NeedsRecalculation` mirrors | state-slice mutations only (inputs apply, calculation begin/complete/fail, upstream invalidation, AMZ-1 `ApplyNeedsRecalculation` bridge) | Thermal/Circuits UI | one canonical `Changed` completion per changed mutation; NoChange/Rejected emit none | not persisted (runtime status) | `ProjectSession.ThermalState.Status` | migrated/verified; AMZ-1 transitional mutation documented | `ThermalStateSnapshots.cs`; `ProjectSessionThermalState.cs:172-195`; `CalculationStateService.cs:56-103`; evidence `task-5/blocker-analysis.md`, `task-6/task-567-merged-boundary.md` | covered |
| `ST-016` | Hydraulics global inputs `GlycolType`, `GlycolConcentration`, `SupplySpacing_cm`, `SupplyHeatPercent` | Circuits `InputData` | circuit copies, Results | input handler/reset/restore incomplete | calculator, Results/export | dirty, propagation, calculate | `HydraulicsProjectData` fields | `ProjectSession.HydraulicsState.GlobalInputs` | legacy | Circuits `:1113-1180`; Results `:1739-1744` | partial |
| `ST-017` | Hydraulics collectors, circuits and topology | Circuits `Collectors` | Results lists/cards/export DTO | commands/reset/restore incomplete | calculator, Results/export | collection protocol | serialized collectors/circuits | `ProjectSession.HydraulicsState.Collectors` | legacy | Circuits `:319-404,732-739`; Results `:1745-1814` | partial |
| `ST-018` | Hydraulics derived circuit/collector results | Circuits/calculator ambiguity | Results cards/summaries/context | calculate paths | Results/export | summary publication | serialized circuit results/summaries | derived hydraulics state | legacy | Circuits `:430-459`; Results `:1764-1813` | partial |
| `ST-019` | Hydraulics status `HydraulicsIsCalculating`, validation message | CalculationStateService | Circuits IsCalculating UI | state-service methods | Circuits | StateChanged/UI property | not persisted | status seam disposition open | seam | `CalculationStateService.cs:77-104`; Circuits `:430-459,1202-1206` | partial |
| `ST-020` | CalculationContext `Climate`, `Construction` | CalculationContext | source module objects | `UpdateClimate`, `UpdateConstruction`, `Reset` | Circuits/other consumers | reset thermal/hydraulics results and ContextChanged | not directly persisted | CalculationContext disposition open | seam | `CalculationContext.cs:53-74,142-169,222-230` | partial |
| `ST-021` | CalculationContext `ThermalInputs` projection | CalculationContext (projection bus; sole production writer is `ThermalStateCoordinator`) | Thermal/Circuits consumers, Results export reads | `ThermalStateCoordinator` publications only (DEC-T05 orchestration and restore finalization); guard suite rejects any other production writer | Circuits | clears hydraulics results, one ContextChanged per publication | not directly persisted | downstream compatibility projection of `ProjectSession.ThermalState` | migrated/verified as single-writer projection | `CalculationContext.cs:192-204`; `ThermalStateCoordinator.cs:147,239`; evidence `task-8/task-8-context-hydraulics.md`, `task-11/task-11-ownership-guards.md` | covered |
| `ST-022` | CalculationContext `ThermalResult`, `HydraulicsResults` projections | CalculationContext (projection bus; Thermal-side writer is `ThermalStateCoordinator`, Hydraulics writer remains Circuits) | Circuits/Results consumers | coordinator Calculate/failure publications for Thermal results; Circuits calculate for HydraulicsResults | Circuits | invalidation and ContextChanged per publication | not directly persisted | derived context seam fed from canonical owners | migrated/verified single-writer semantics per side | `CalculationContext.cs:176-217`; `ThermalStateCoordinator.cs:147-187,239-240`; evidence `task-8/task-8-context-hydraulics.md` | covered |
| `ST-023` | Save/load DTO `ProjectData` snapshot | file boundary DTO | all module/Results materializations | SaveCurrentProject, file deserialize, restore | file service/orchestrator/Results | reset then restore/refresh; counts unknown | JSON: temp write, conditional existing-original `.bak` copy, `File.Move(..., overwrite:true)`; atomicity/crash recovery not established | ProjectSession snapshot adapter | legacy | `ProjectFileService.cs:115-190`; Results `:1613-1817` | partial |
| `ST-024` | Results identity/status `ProjectNumber`, `ProjectObject`, `StatusMessage`, `IsOperatingMode`, `IsDataReady`, `MissingModules` | ResultsViewModel | UI/export | Reset, load, refresh/export | UI/export | property changes; counters unknown | identity values snapshot; status/readiness not observed persisted | derived Results projection | legacy | Results `:1515-1520,1559-1562,1573-1607` | partial |
| `ST-025` | Results numeric derived fields `TotalThermalPower_kW`, `SystemVolume_L`, `PumpFlowRate_m3h`, `PumpHead_kPa`, `ExpansionTankVolume_L`, `SupplyTemperature`, `ReturnTemperature`, `OperatingTemperature`, `GroundTemperature`, `WindSpeed`, `SnowfallIntensity`, `SurfaceTemperature`, `GlycolConcentration`, `TotalPowerDensity`, `R1`, `R2`, `LambdaE`, `PowerUp`, `PowerDown`, `TotalPipeLength`, `RzsCount`, `PumpQ`, `PumpH`, `ExpansionTankV` | ResultsViewModel | PDF/report/export | Reset, RefreshAll/load methods | UI/export | RefreshAll/KPI updates | not observed persisted | derived Results projection | legacy | Results `:1493-1505,1522-1545,986-1055` | partial |
| `ST-026` | Results collections `Layers`, `Collectors`, `Circuits`, `CollectorSpecifications`, `CollectorEquipmentItems`, `HydraulicSummaryCards` | ResultsViewModel | PDF/report/export | Reset, RefreshAll rebuild/load methods | UI/export | cards deliberately clear before later rebuild | not observed persisted | derived Results projection | legacy | Results `:1493-1505,1546-1556` | partial |
| `ST-027` | Results selection `SelectedCollectorIndex`, `CollectorSummary` | ResultsViewModel | UI/export selection | Reset, selection UI | Results UI | selection update | not observed persisted | derived Results projection | legacy | Results `:1547-1559` | partial |

## Risks

- `ST-014`/`ST-022` thermal-result dual paths are resolved: `ProjectSession.ThermalState` owns the
  canonical last-derived result and `CalculationContext.ThermalResult` is a single-writer projection;
  `ST-020` remains an explicit shared context seam for Climate/Construction.
- `ST-017`/`ST-018` and `ST-023` retain multi-writer or restore ambiguity; Phase 3 removed the
  Construction `ST-009`/`ST-010` ambiguity; Phase 4 removed the Thermal `ST-012..ST-015` ambiguity.
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

## Phase 4 ThermalState overlay (Task 14)

`ProjectSession.ThermalState` (`ProjectSessionThermalState`, sealed, owned private
instance of singleton `ProjectSession`) is the sole writable owner of Thermal
inputs (including pipe spacing), the last-derived result and the calculation
status. `ThermalStateCoordinator` (sealed singleton, DEC-T04A) is the canonical
command boundary: it translates adapter commands into closed mutations, owns the
single dirty-intent path for changed user edits, orchestrates DEC-T05
calculation, and holds the only upstream Climate/Construction subscriptions.
`ThermalViewModel` is a WPF adapter; `CalculationStateService` is a compat
adapter with zero Thermal/spacing backing stores (canonical getters plus
one-shot completion translation and ProjectLoadReset suppression);
`CalculationContext` remains a downstream projection bus whose Thermal-side
writer is exclusively the coordinator. The AMZ-1 transitional mutation
`ApplyNeedsRecalculation` has exactly one production caller (the
`CalculationStateService.SetThermalNeedsRecalculation` compat route), proven by
the Todo 11 guard suite. Lifecycle restore goes through the canonical `Restore`
(second-load zero-stale fixed per DEC-T08/AMZ-2). Rows `ST-012..ST-015` and
`ST-021..ST-022` above are updated to this reality; all other rows are unchanged.

Evidence: `docs/architecture-migration/evidence/phase-4-thermal-state/task-3/task-3-thermal-state-contract.md`,
`task-6/task-567-merged-boundary.md`, `task-8/task-8-context-hydraulics.md`,
`task-9/task-9-lifecycle-restore.md`, `task-11/task-11-ownership-guards.md`,
`task-12/task-12-executable-gates.md` (full Release 1946 total / 1943 passed /
0 failed / 3 accepted NotExecuted).
