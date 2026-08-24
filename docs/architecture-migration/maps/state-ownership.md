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
| `ST-008` | `ProjectSession.ConstructionState` | `ConstructionViewModel` adapter; `CurrentProjection`; Results DTO | same | no second writable owner | `ProjectSessionConstructionState.cs`; Tasks 8-12.1 | covered |
| `ST-009` | `ProjectSession.ConstructionState.LayersAbovePipe` | ordered adapter/projection/DTO copies | same | identity/order guarded | Task 9 recovery; Task 12 gates | covered |
| `ST-010` | `ProjectSession.ConstructionState.LayersBelowPipe` | ordered adapter/projection/DTO copies | same | identity/order guarded | Task 9 recovery; Task 12 gates | covered |
| `ST-011` | `ConstructionStateProjection` derived from canonical snapshot | Thermal `IConstructionData`; Results DTO | same derived projection | no independent writer | Tasks 10-12.1; pre-Task 13 correction | covered |

## Phase 3 ConstructionState acceptance overlay

`ProjectSession` owns one `ProjectSessionConstructionState`. Its snapshot is the
only writable authority for groundwater, loads and both ordered layer sequences.
`ConstructionViewModel` is a WPF adapter, Thermal resolves `IConstructionData`
from `CurrentProjection`, save maps the canonical snapshot through
`ConstructionPersistenceMapper`, and `CompleteChanged` is the authoritative
completion, dirty and downstream boundary. The `.smc` version remains `1.1`.
| `ST-012` | `ProjectSession.ThermalState` (`ProjectSessionThermalState`) — sole writable owner of Thermal inputs incl. spacing | `ThermalViewModel` adapter mirror; `CalculationContext.ThermalInputs` projection; Results DTO via `ThermalPersistenceMapper` | same canonical slice | no second writable owner; guard suite rejects VM/service/context stores | `IProjectSessionThermalState.cs`; `ProjectSessionThermalState.cs`; `ThermalStateCoordinator.cs`; evidence `task-3/task-3-thermal-state-contract.md`, `task-6/task-567-merged-boundary.md`, `task-11/task-11-ownership-guards.md` | covered |
| `ST-013` | `ProjectSession.ThermalState` (`Inputs.PipeSpacing`) | legacy read-through `ICalculationStateService.PipeSpacing`/`PipeSpacingChanged` compat surface (zero backing store, AMZ-1) | same canonical slice | compat setter is a no-op echo when equal; restore applies spacing before adapter finalization | `CalculationStateService.cs:226-235`; `ProjectLoadOrchestrator.cs:132-155`; evidence `task-9/task-9-lifecycle-restore.md` | covered |
| `ST-014` | `ProjectSession.ThermalState` last-derived result (derived value; not an input store) | `CalculationContext.ThermalResult` single-writer projection; Results values | same derived ownership | upstream invalidation clears once only when a result exists (DEC-T04 frozen behavior) | `ProjectSessionThermalState.cs:160-218`; `ThermalStateCoordinator.cs:132-202`; evidence `task-8/task-8-context-hydraulics.md` | covered |
| `ST-015` | `ProjectSession.ThermalState` status (`ThermalStatusSnapshot`) | compat getters/`StateChanged` translation in `CalculationStateService`; adapter mirrors | same canonical slice | AMZ-1 transitional `ApplyNeedsRecalculation` has exactly one production caller (compat route) | `ProjectSessionThermalState.cs:172-195`; `CalculationStateService.cs:56-103`; evidence `task-5/blocker-analysis.md`, `task-11/task-11-ownership-guards.md` | covered |
| `ST-016` | `ProjectSession.HydraulicsState` (`ProjectSessionHydraulicsState`) — sole writable owner of hydraulics global inputs | adapter `InputData` mirror; `HydraulicsProjectData` via `HydraulicsPersistenceMapper`; Results/export projection | same canonical slice | no second writable owner; guard suite rejects VM/service/context stores; slice raises dirty for User origin only (`hydraulicsDirtyService ?? this`) | `ProjectSessionHydraulicsState.cs`; `CircuitsViewModel.cs` (adapter ctor); evidence `task-9/divergence-notes.md`, `task-11/trx-guards-release.json` | covered |
| `ST-017` | `ProjectSession.HydraulicsState` collectors/circuits snapshots | adapter collection mirror captured via `CaptureCanonicalCollectors`; serialized DTO via mapper | same canonical slice | restore applies `Restore(ProjectLoad)` then read-only mirror; guard suite rejects bypass writers | `ProjectSessionHydraulicsState.cs:83-91`; `ProjectLoadOrchestrator.cs:171-173,200-201`; evidence `task-9/divergence-notes.md` | covered |
| `ST-018` | `ProjectSession.HydraulicsState` derived results (sole writable owner via coordinator `CompleteCalculation`) | `CalculationContext.HydraulicsResults` single-writer projection published by `HydraulicsStateCoordinator.PublishHydraulics`; Results cards/summaries | same derived ownership | zero `UpdateHydraulics` calls remain in the VM; one publication per completed attempt | `HydraulicsStateCoordinator.cs:56-84`; evidence `task-8/writer-authority-updates.md`, `task-6/correction-notes.md` | covered |
| `ST-019` | `ProjectSession.HydraulicsState` status (`HydraulicsStatusSnapshot`) | compat translation in `CalculationStateService`; Circuits `IsCalculating` UI mirror | same canonical slice | per-attempt status termination is unconditional in `RunCalculation finally` (FIX B); no sticky Error state across attempts | `HydraulicsStateCoordinator.cs:59-84`; evidence `task-9/divergence-notes.md` | covered |
| `ST-020` | CalculationContext | source modules | context disposition open | context versus module writes | CalculationContext.cs:142-169 | partial |
| `ST-021` | CalculationContext projection bus; sole Thermal-side production writer is `ThermalStateCoordinator` | Circuits/Results consumers | downstream projection of canonical Thermal inputs | guard suite rejects any non-coordinator production writer | `CalculationContext.cs:192-204`; `ThermalStateCoordinator.cs:147,239`; evidence `task-8/task-8-context-hydraulics.md`, `task-11/task-11-ownership-guards.md` | covered |
| `ST-022` | CalculationContext projection bus (Thermal results written only by the Thermal coordinator; Hydraulics results written only by `HydraulicsStateCoordinator`) | Circuits/Results consumers | derived context seam fed from canonical owners | no second per-side writer; invalidation paths measured | `CalculationContext.cs:176-217`; `HydraulicsStateCoordinator.cs:56-57`; evidence `task-8/writer-authority-updates.md` | covered |
| `ST-023` | ProjectData boundary | live state materializations | snapshot adapter | restore transactional behavior unknown | ProjectFileService.cs:115-190 | partial |
| `ST-024` | ResultsViewModel | UI/export | derived projection | mutable projection | Results :1510-1607 | partial |
| `ST-025` | ResultsViewModel | PDF/report | derived projection | mutable cached values | Results :1493-1545 | partial |
| `ST-026` | ResultsViewModel | PDF/report | derived projection | clear/rebuild timing | Results :1546-1556 | partial |
| `ST-027` | ResultsViewModel | UI | derived projection | selection reset | Results :1557-1559 | partial |

## Phase 3.1 Climate invalidation overlay (Task 11)

`ProjectSession.ClimateState` remains the sole writable canonical Climate owner.
`ClimateViewModel` mirrors and routes mutations; `ClimateData` is only the
compatibility projection; `CalculationContext` is downstream compatibility
state; Results reads the canonical snapshot. `UserReset` retains user dirty and
publication semantics, while `ProjectLoadReset` and `Load` are lifecycle origins
without user dirty/history semantics. No second writable Climate owner exists.

## Phase 4 ThermalState acceptance overlay (Task 14)

`ProjectSession` owns one sealed `ProjectSessionThermalState`
(`IProjectSessionThermalState`); it is not independently registered in DI and is
reached reference-identically through `IProjectSession.ThermalState`. It is the
sole writable owner of Thermal inputs (mode, supply, ground, pipe, spacing), the
last-derived result and the status snapshot. All mutations are closed
(`ApplyInputs`, `ApplyInputEdit`, `ResetToDefaults`, `BeginCalculation`,
`CompleteCalculation`, `FailCalculation`, `Restore`,
`InvalidateFromClimate/Construction`, AMZ-1 `ApplyNeedsRecalculation`) with
immutable snapshots, exhaustive `ThermalMutationOrigin` values and a single
`Changed` completion per changed mutation. `ThermalStateCoordinator` (sealed
singleton) is the sole writer outside persistence/restore paths, the single
dirty-intent owner for changed user edits, the DEC-T05 calculation orchestrator,
and the sole upstream Climate/Construction subscriber. `ThermalViewModel` is a
WPF adapter; `CalculationStateService` is a compat adapter with zero Thermal
stores; `CircuitsViewModel`/Hydraulics and Results remain consumers;
`ResultsViewModel` saves/reads canonical via `ThermalPersistenceMapper`. The
`.smc` Thermal wire fields and version are unchanged. Acceptance evidence:
`task-3/task-3-thermal-state-contract.md`, `task-6/task-567-merged-boundary.md`,
`task-8/task-8-context-hydraulics.md`, `task-9/task-9-lifecycle-restore.md`,
`task-10/task-10-persistence-results.md`, `task-11/task-11-ownership-guards.md`,
`task-12/task-12-executable-gates.md` (full Release 1946/1943/0/3),
`task-13/task-13-user-flow-qa.md`.

## Phase 5 HydraulicsState acceptance overlay (Task 14)

`ProjectSession` owns one `ProjectSessionHydraulicsState`; it is not independently registered in DI
and is reached reference-identically through `IProjectSession.HydraulicsState`. It is the sole
writable owner of hydraulics global inputs, the collectors/circuits topology, the derived results and
the status snapshot; dirty intent for user edits is raised by the slice itself through
`hydraulicsDirtyService ?? this`, never owned by the ViewModel. `HydraulicsStateCoordinator` (sealed
singleton, factory-resolved from the session slice) is the sole production writer of the
`CalculationContext` hydraulics results projection and terminates every calculation attempt with
exactly one unconditional `ResetHydraulicsState`. `CircuitsViewModel` is a WPF adapter whose
constructor requires `IHydraulicsStateCoordinator` + `IProjectSession` and which contains zero
`UpdateHydraulics` calls and no `BuildCanonicalSnapshot`. Save reads exclusively
`_projectSession.HydraulicsState.Snapshot` via `HydraulicsPersistenceMapper.BuildHydraulicsProjectData`;
restore goes only through slice `Restore(origin=ProjectLoad)` from `ProjectLoadOrchestrator`.
`.smc` hydraulics wire fields and version are unchanged. The DI construction-cycle deadlock
(`ProjectSession <- IMarkDirtyService <- ProjectSession`) was fixed composition-only by the explicit
factory registration in `AddResultsModule`. Acceptance evidence:
`task-6/correction-notes.md`, `task-8/writer-authority-updates.md`,
`task-9/divergence-notes.md` (four owner-adjudicated semantic adaptations),
`task-11/trx-guards-release.json` (8/8 guard categories),
`task-12/arithmetic.json` (full Release 1976/0/3 accepted NotExecuted),
`ui-qa/observations.json` (nine-step agent QA PASS).
