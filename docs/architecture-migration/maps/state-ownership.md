---
phase: phase-1-project-session-shell
snapshot_sha: 021d4abd159aa71c4a19c7a6536851264e5a58ca
source_basis: accepted-phase-1-project-session-shell
generated_at_utc: 2026-08-04T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands: [node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2, node docs/architecture-migration/widget/generate-widget.mjs --check]
exit_code: 0
status: pass
raw_output: Ownership filter updated for Phase 1 lifecycle shell.
limitations: [Climate/Construction/Thermal/Hydraulics slices remain target-only; ST-003 DisplayMode was not migrated in Phase 1.]
---

# State Ownership View

| State ID | Current writable authority | Multiple writable authorities / copies | Future owner | Dual-write or ambiguity risk | Evidence | Coverage |
| --- | --- | --- | --- | --- | --- | --- |
| `ST-001` | ProjectSession | ProjectStateService/IProjectStateService/IProjectInfoService/IMarkDirtyService forwarding aliases | ProjectSession.Lifecycle.Identity | none after Phase 1; legacy interfaces are read-through | ProjectSession.cs; ProjectStateService.cs (forwarder); ProjectSessionTests.cs; final-gates.md | covered |
| `ST-002` | ProjectSession | ProjectStateService/IProjectStateService forwarding aliases; Main title reads | Lifecycle.FilePath | none after Phase 1 | ProjectSession.cs; ProjectStateService.cs (forwarder); final-gates.md | covered |
| `ST-003` | ResultsViewModel | DTO copy | Lifecycle.DisplayMode | load/reset writers | Results :1519-1621 | partial |
| `ST-004` | ProjectSession | ProjectStateService/IMarkDirtyService forwarding aliases; MainWindow dirty prompt reads | Lifecycle.IsDirty | none after Phase 1 | ProjectSession.cs; ProjectStateService.cs (forwarder); ProjectLifecycleFlowCharacterizationTests.cs; final-gates.md | covered |
| `ST-005` | ProjectSession | CalculationStateService compatibility lease (one lease, no local bool/depth) | Lifecycle.RestoreGuard | none after Phase 1; compatibility setter is temporary | ProjectSession.cs; CalculationStateService.cs; restore-guard.md; final-gates.md | covered |
| `ST-006` | `ProjectSession.ClimateState` / `ProjectSessionClimateState` | `ClimateViewModel` mirrors snapshot values; `ClimateData`/`IClimateData` is compatibility projection; `CalculationContext.Climate` is downstream projection; Results reads snapshot for save/export | `ProjectSession.ClimateState` | no second writable canonical Climate owner after Phase 2; legacy no-session test seam remains isolated and documented | `ProjectSession.cs`; `ProjectSessionClimateState.cs`; `IProjectSession.cs`; `ClimateViewModel.cs`; `ClimateData.cs`; `ResultsViewModel.cs`; `docs/architecture-migration/evidence/phase-2-climate-state/{climate-state-api.md,climate-data-projection.md,climate-viewmodel-adapter.md,persistence-results.md,downstream-invalidation.md,di-guards.md,affected-gates.md}` | covered |
| `ST-007` | split: persisted/reset Climate fields in `ProjectSession.ClimateState`; UI-only search state in `ClimateViewModel` adapter | `SearchQuery`, filtered/recent city UI collections stay non-persisted adapter state; `ColdFiveDayTemperature`, `HasUserModifications`, city/scalar/high-requirements/reset values mirror canonical snapshot | `ProjectSession.ClimateState` for project Climate values; UI adapter for search-only state | no canonical ownership ambiguity for persisted Climate values; UI search state intentionally outside `.smc` | `ProjectSessionClimateState.cs`; `ClimateViewModel.cs`; `ProjectLoadOrchestrator.cs`; `ResultsViewModel.cs`; `docs/architecture-migration/evidence/phase-2-climate-state/{climate-viewmodel-adapter.md,restore-reset-routing.md,persistence-results.md,affected-gates.md}` | covered |

## Phase 2 ClimateState acceptance overlay

Task 12 refresh records Phase 2 Climate ownership as accepted through Task 11 gates:
`ProjectSession` owns the private `ProjectSessionClimateState` instance and exposes it through
`IProjectSession.ClimateState`; `ClimateViewModel` routes user city/scalar/high-requirements/reset
mutations to that state slice and mirrors snapshots; `ClimateData`/`IClimateData` is a projection
boundary updated through `ApplyProjection`; `CalculationContext` remains the downstream compatibility
projection/invalidation seam. Load/reset/restore use non-user `ClimateMutationOrigin` paths, and
`ResultsViewModel.SaveCurrentProject()` persists from `_projectSession.ClimateState.Snapshot`, keeping
the existing `.smc` Climate DTO fields and version behavior. Task 11 acceptance evidence is
`docs/architecture-migration/evidence/phase-2-climate-state/affected-gates.md` with targeted TRX
`total 330 / executed 329 / passed 329 / failed 0` and full rerun TRX
`total 1616 / executed 1613 / passed 1613 / failed 0`.
| `ST-008` | model/VM ambiguous | context/Results | ConstructionState | model/VM split | Construction :694-856 | partial |
| `ST-009` | collection/model ambiguous | Results list | ConstructionState.LayersAbovePipe | dual representation | Construction :702-724 | partial |
| `ST-010` | collection/model ambiguous | Results list | ConstructionState.LayersBelowPipe | dual representation | Construction :702-792 | partial |
| `ST-011` | model/VM ambiguous | Results/Thermal | derived construction | recomputation/persistence ambiguity | Results :1683-1688 | partial |
| `ST-012` | ThermalViewModel | context/Results | ThermalState.Inputs | context copy | Thermal :321-406 | partial |
| `ST-013` | guarded CalculationStateService | Thermal/Circuits/Results | ThermalState.PipeSpacing | intentional seam | CalculationStateService.cs:120-139 | partial |
| `ST-014` | Thermal VM/context ambiguous | Results | derived thermal | dual path | Thermal :326-406 | partial |
| `ST-015` | service/VM split | UI | status disposition open | split status owners | CalculationStateService.cs:23-70 | partial |
| `ST-016` | Circuits InputData | circuit copies/Results | HydraulicsState.GlobalInputs | fan-out writes | Circuits :1113-1180 | partial |
| `ST-017` | Circuits Collectors | Results/export | HydraulicsState.Collectors | restore census incomplete | Results :1745-1814 | partial |
| `ST-018` | calculator/Circuits ambiguous | Results/context | derived hydraulics | result authority incomplete | Circuits :430-459 | partial |
| `ST-019` | CalculationStateService | Circuits UI | status disposition open | distinct status seam | CalculationStateService.cs:77-104 | partial |
| `ST-020` | CalculationContext | source modules | context disposition open | context versus module writes | CalculationContext.cs:142-169 | partial |
| `ST-021` | CalculationContext | Thermal inputs | context disposition open | Reset retains inputs | CalculationContext.cs:192-230 | partial |
| `ST-022` | CalculationContext | Thermal/Circuits results | derived context seam | invalidation paths | CalculationContext.cs:176-230 | partial |
| `ST-023` | ProjectData boundary | live state materializations | snapshot adapter | restore transactional behavior unknown | ProjectFileService.cs:115-190 | partial |
| `ST-024` | ResultsViewModel | UI/export | derived projection | mutable projection | Results :1510-1607 | partial |
| `ST-025` | ResultsViewModel | PDF/report | derived projection | mutable cached values | Results :1493-1545 | partial |
| `ST-026` | ResultsViewModel | PDF/report | derived projection | clear/rebuild timing | Results :1546-1556 | partial |
| `ST-027` | ResultsViewModel | UI | derived projection | selection reset | Results :1557-1559 | partial |
