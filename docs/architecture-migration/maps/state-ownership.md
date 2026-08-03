---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T18:38:01.5504380Z
working_directory: D:/IA/ace v.2
commands: [codegraph_codegraph_explore lifecycle state flows, PowerShell structural QA in reactive.md]
exit_code: 0
status: pass
raw_output: Ownership filter over the state inventory.
limitations: [Current ambiguity is retained; ProjectSession is target-only.]
---

# State Ownership View

| State ID | Current writable authority | Multiple writable authorities / copies | Future owner | Dual-write or ambiguity risk | Evidence | Coverage |
| --- | --- | --- | --- | --- | --- | --- |
| `ST-001` | ProjectStateService/Results pass-through | DTO copy | ProjectSession.Lifecycle.Identity | direct service setters possible | ProjectStateService.cs:17-22; Results :1515-1621 | partial |
| `ST-002` | ProjectStateService | Results/Main title copy | Lifecycle.FilePath | several Results paths write it | ProjectStateService.cs:27-39; Results :753-821 | partial |
| `ST-003` | ResultsViewModel | DTO copy | Lifecycle.DisplayMode | load/reset writers | Results :1519-1621 | partial |
| `ST-004` | ProjectStateService | Main title projection | Lifecycle.IsDirty | transition count unknown | ProjectStateService.cs:45-72 | partial |
| `ST-005` | CalculationStateService | reader guards | Lifecycle.RestoreGuard | public setter protocol incomplete | Results :1577-1607 | partial |
| `ST-006` | Climate VM/IClimateData seam | context/Results | ClimateState | source/context copy | Climate :389-409 | partial |
| `ST-007` | ClimateViewModel | UI search | ClimateState/UI decision | nonpersisted members | Climate :75-92,389-409 | partial |
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
