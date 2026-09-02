# phase-8-results-derived-projection - Work Plan

## TL;DR (For humans)

This plan keeps Phase 8 focused on one thing: make `ResultsViewModel` a pure derived projection that reads every displayed value from canonical state (`ProjectSession` slice snapshots and the approved `CalculationContext` read-projection seam), then prove that Results does not own module inputs, does not write canonical state, and does not become a second canonical store. This closes the open invariant `INV-009` and the partial state rows `ST-003`, `ST-024..ST-027`, and it supports the Results clause of `INV-016` ("Results SHALL consume completed changes as a derived projection").

The plan preserves the approved Phase 2-7 foundation: the Phase 7 restore boundary (`ProjectLoadOrchestrator`, validation before mutation, four-slice canonical order, exactly-once calculation publication), the Phase 6 save boundary (`IProjectSaveService` reading canonical snapshots), and `DEC-001 = A` (`CalculationContext` stays as the downstream compatibility/read-projection seam; the Thermal and Hydraulics coordinators remain its only production result publishers — the shell/orchestrator `CalculationContext.Reset()` null-writers stay as characterized). The work is split into 8 execution slices plus 4 final verification gates. The main risk is any remaining ambiguity about the canonical source of a projected value (for example a climate field that exists only on the module ViewModel); if the live code cannot establish a canonical source without behavior change, the worker must stop and surface that as an owner decision rather than inventing a new contract or silently widening `CalculationContext`.

Explicitly out of scope (next planning boundary, not this phase): `INV-008` (removal of the concrete ViewModel dependencies inside `ProjectLoadOrchestrator`), removal of the legacy forwarding aliases `IProjectStateService` / `IProjectInfoService` / `IMarkDirtyService`, any change to the `ST-020..ST-022` `CalculationContext` writer disposition, global closure of the unknown reactive counters (`INV-010`), Markdown removal, and any export/PDF/Excel/preview/print behavior change.

## Scope

### Authority and frozen-plan lifecycle

- Authoring candidate: `docs/architecture-migration/plans/phase-8-results-derived-projection.md`, authored directly in the canonical plans location per owner direction. Terminal review and owner plan approval freeze this file; the `.omo/plans/` mirror, if created later, is an operational execution ledger only and is not a second authority.
- Active dossier authority: `docs/architecture-migration/AGENTS.md` and the latest `docs/architecture-migration/TASK_CONTEXT.md`.
- Planning approval authorizes plan writing only; execution still belongs to a separate worker session started by the user.
- Todo write-sets and commands below describe downstream worker execution, not this planning session.
- The plan stays on the approved Phase 2-7 foundation: no `.smc` format change, no `Version = "1.1"` change, no legacy compatibility expansion, no owner gate for private implementation choices, and no mutation of unrelated baseline artifacts.

### In scope

- Re-sourcing every `ResultsViewModel` projection read from canonical state: `IProjectSession.ClimateState`, `IProjectSession.ConstructionState` (plus `ConstructionStateProjection`), `IProjectSession.ThermalState`, `IProjectSession.HydraulicsState`, `CalculationContext.Climate` / `Construction` / `ThermalResult` / `HydraulicsResults` (read-only consumption per `DEC-001 = A`), and `IProjectDisplayModeState` for the persisted display mode (`ST-003`).
- Removal of the four concrete module-ViewModel references (`ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`, `CircuitsViewModel`) from `ResultsViewModel` construction, fields, and the `AddResultsModule` DI wiring.
- Read-only projection proof: negative probe that Results cannot write into `ProjectSession` slices or `CalculationContext`, and that no module input is mutated through Results.
- Projection rebuild multiplicity: exactly one `RefreshAll()` per successful restore (Phase 7 contract preserved), no projection rebuild on rejected restore, one rebuild per export/navigation refresh trigger.
- Fresh-vs-stale sentinel at the Results layer: after restore and calculation, projection values are session-derived; stale persisted DTO result fields never surface through Results.
- Frozen external trigger surface: `ResultsPdfDataBuilder` calling `RefreshAll()`, `MainWindow` calling `LoadHydraulicsDataOnNavigate()`, `LoadProjectDataAsync` restore handoff, `Reset()` orchestration, and the `ProjectChanged` event remain the same public boundaries.
- Architecture evidence refresh only where needed to keep the dossier aligned with the live write-set, including the `INV-009` status flip with evidence references.

### Must NOT have

- No change to `ProjectLoadOrchestrator` restore semantics and no `INV-008` work in this phase: the orchestrator keeps its current ViewModel dependencies until a separately planned legacy-cleanup phase.
- No removal or replacement of `CalculationContext`, no new production writer into `CalculationContext`, no change to the `ST-020..ST-022` seam disposition (`DEC-001 = A`).
- No removal of the legacy forwarding aliases `IProjectStateService` / `IProjectInfoService` / `IMarkDirtyService` and no broad legacy-owner removal.
- No `.smc` schema/persistence-format change, no `ProjectData` wire-shape change, no relaxation of the approved Phase 2-7 semantics.
- No new canonical store, no second calculation pass, no second restore boundary, no new subscription framework, and no Markdown/export feature work.
- No production-code implementation in this planning session; no test execution in this planning session.
- No `TASK_CONTEXT.md` change unless a live blocker proves it is strictly required.

## Verification strategy

- Grounding is exploration-first: live code and tests define the current Results projection reads, trigger paths, and canonical sources before any plan item is finalized.
- Each execution slice must have agent-executable happy/failure QA, with a concrete command or test filter and a receipt path; the downstream worker, not the planner, executes those commands.
- The main proof sequence is: projection baseline lock → canonical source map → climate/construction re-sourcing → thermal/hydraulics re-sourcing → readiness/KPI/display-mode → module-VM decoupling + DI → multiplicity/sentinel/user-flow → architecture evidence → phase-wide gates.
- Characterization is never weakened: where an existing stabilization contract pins the module-ViewModel seam as the data source (for example the frozen contract that clearing the adapter thermal result zeroes KPI without recalculation), the user-visible behavior contract is preserved and the seam-pinning test may be updated only to the equivalent canonical-source assertion, with the change recorded in the slice receipt. Silently deleting or weakening a frozen contract is a failed verification.
- If the worker cannot prove a canonical source for a projected value from the live codebase, the correct stop condition is `OWNER_DECISION_REQUIRED`, not an invented fallback API and not a new `CalculationContext` writer.
- Before each focused test command, the worker must create the evidence directory and run `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; only after a successful build may the listed `dotnet test ... --no-build` command run. Any focused test command that executes 0 tests is a failed verification even if the process exit code is 0.

## Execution strategy

One sequential lane. Read-only inspection and evidence gathering can be parallel where independent. The worker locks the projection baseline first, then re-sources reads slice by slice, removing the module-ViewModel coupling only after every read has a proven canonical source. Any slice that would alter behavior outside the approved Results-projection boundary must stop for owner decision. Prometheus does not run product tests in this session; the worker creates the evidence directory, builds the test project, then runs the exact focused test command listed in each todo and rejects 0-test executions.

## Todos

- [ ] 1. `src/ViewModels/Results/ResultsViewModel.cs` and the Results stabilization suites: lock the projection baseline and the module-ViewModel read inventory - expect current projection behavior frozen before any production change

  **Goal:** Freeze the current Results projection behavior and record the complete inventory of module-ViewModel reads before any production change.

  **Write-set / change class:** tests/evidence only at this stage. No production edits in this slice.

  **References:** `src/ViewModels/Results/ResultsViewModel.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsStabilizationPhase1BehaviorContractsTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsStabilizationPhase1ContractsTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs`; `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md`.

  **Acceptance:** The worker can point to every module-ViewModel read site (`LoadClimateData`, `LoadConstructionData`, `LoadThermalData`, `LoadHydraulicsData`, `UpdateCollectorsList`, `RebuildHydraulicSummaryCards`, `UpdateCollectorEquipmentItems`, `UpdateCircuitsFilter`, `UpdateCollectorSpecifications`, `CheckDataReadiness`, `RecalculateKpi` including its KPI helper chain, selection sync, `SaveCurrentProject` custom templates, legacy `HasUnsavedData`) and to the frozen external triggers (`ResultsPdfDataBuilder` → `RefreshAll()`, `MainWindow` → `LoadHydraulicsDataOnNavigate()`, `LoadProjectDataAsync` restore handoff, `Reset()` orchestration). The stabilization suites pass unmodified on the dirty baseline.

  **Happy QA:** The existing stabilization and collector-equipment suites pass unchanged, proving the baseline is green before re-sourcing.

  **Failure QA:** Any observed Results write-back into module state or canonical state during projection rebuild is recorded as a blocker.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-8-results-derived-projection docs/architecture-migration/evidence/phase-8-results-derived-projection/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests|FullyQualifiedName~ResultsViewModelCollectorEquipmentItemsTests" --logger "trx;LogFileName=slice-1-projection-baseline.trx" --results-directory "docs/architecture-migration/evidence/phase-8-results-derived-projection/logs"`; receipt `docs/architecture-migration/evidence/phase-8-results-derived-projection/slice-1-projection-baseline.md`.

  **Next gate / Commit:** only Todo 2 after PASS. No commit in planning mode; expected receipt is a projection-baseline characterization note with the full read inventory.

- [ ] 2. `src/Services/Project/ProjectSession*.cs`, `src/Core/CalculationContext.cs`, and `src/Services/Project/IProjectDisplayModeState.cs`: build the canonical source map for every projected value - expect one proven canonical source per read or an explicit owner decision

  **Goal:** Map every Results projection read to its canonical source: `ProjectSession` slice snapshots, `ConstructionStateProjection`, the read-only `CalculationContext` projections (`Climate`, `Construction`, `ThermalResult`, `HydraulicsResults` with coordinator-only production writers), and `IProjectDisplayModeState`.

  **Write-set / change class:** tests/evidence only. No production edits in this slice.

  **References:** `src/Services/Project/ProjectSession.cs`; `src/Services/Project/ProjectSessionClimateState.cs`; `src/Services/Project/ConstructionStateProjection.cs`; `src/Services/Project/ThermalStateCoordinator.cs`; `src/Services/Project/HydraulicsStateCoordinator.cs`; `src/Core/CalculationContext.cs`; `src/Services/Project/IProjectDisplayModeState.cs`; `docs/architecture-migration/maps/state-ownership.md`.

  **Acceptance:** A complete read-to-source table exists in the receipt. Any projected value without a proven canonical equivalent is either resolved through the approved `CalculationContext` read projection or stops the slice as `OWNER_DECISION_REQUIRED`. The known candidate is `ColdPeriodDays` (read from `CityInfo.Period_0_Days` via `ClimateViewModel.SelectedCity`): `ClimateStateSnapshot` carries only the city name string plus scalar fields, no `Period_0_Days`, and the module adapter re-resolves the city by name with a fabricated fallback; the slice must present the owner a concrete choice rather than invent a canonical field or silently keep the module-ViewModel read.

  **Happy QA:** Session and coordinator tests prove the canonical sources carry the projected values.

  **Failure QA:** A canonical source that cannot produce the projected value without behavior change must stop the slice; inventing a new API or adding a new `CalculationContext` writer is a blocker.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-8-results-derived-projection docs/architecture-migration/evidence/phase-8-results-derived-projection/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ClimateStateTests|FullyQualifiedName~ThermalStateCoordinatorTests|FullyQualifiedName~ProjectSessionHydraulicsStateTests" --logger "trx;LogFileName=slice-2-canonical-source-map.trx" --results-directory "docs/architecture-migration/evidence/phase-8-results-derived-projection/logs"`; receipt `docs/architecture-migration/evidence/phase-8-results-derived-projection/slice-2-canonical-source-map.md`.

  **Next gate / Commit:** only Todo 3 after PASS. Expected receipt is a canonical-source-map note.

- [ ] 3. `src/ViewModels/Results/ResultsViewModel.cs` (`LoadClimateData`, `LoadConstructionData`) and Results tests: re-source the Climate and Construction projection reads from canonical state - expect byte-identical projection output

  **Goal:** Replace `_climateViewModel` and `_constructionViewModel` reads in the projection path with `ProjectSession.ClimateState.Snapshot`, `ConstructionStateProjection`, or the approved `CalculationContext.Construction` read projection, with no observable projection change.

  **Write-set / change class:** production/test. This is the first slice that may require production changes; writes stay inside `ResultsViewModel` and its tests.

  **References:** `src/ViewModels/Results/ResultsViewModel.cs`; `src/Services/Project/ProjectSessionClimateState.cs`; `src/Services/Project/ConstructionStateProjection.cs`; `src/Core/CalculationContext.cs`.

  **Acceptance:** The Climate and Construction projection fields (city, design temperature, zone, cold period days, wind, snowfall, R1/R2, LambdaE, layer list) are produced from canonical state and match the frozen baseline values in every characterized scenario. No `_climateViewModel`/`_constructionViewModel` reads remain in the projection methods.

  **Happy QA:** Stabilization and open-project tests prove identical projection output after re-sourcing.

  **Failure QA:** A canonical snapshot change without a projection rebuild must not surface stale projection values; any Results write into the session slices is a blocker.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-8-results-derived-projection docs/architecture-migration/evidence/phase-8-results-derived-projection/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests|FullyQualifiedName~ResultsViewModelOpenProjectTests" --logger "trx;LogFileName=slice-3-climate-construction-resourcing.trx" --results-directory "docs/architecture-migration/evidence/phase-8-results-derived-projection/logs"`; receipt `docs/architecture-migration/evidence/phase-8-results-derived-projection/slice-3-climate-construction-resourcing.md`.

  **Next gate / Commit:** only Todo 4 after PASS. Expected receipt is a climate/construction re-sourcing note.

- [ ] 4. `src/ViewModels/Results/ResultsViewModel.cs` (`LoadThermalData`, `LoadHydraulicsData`, collectors/summary/selection rebuild) and results tests: re-source the Thermal and Hydraulics result reads from the coordinator-written projections - expect no second calculation and no stale result read

  **Goal:** Replace `_thermalViewModel.Result` and `_circuitsViewModel` result/collector reads with `CalculationContext.ThermalResult`, `CalculationContext.HydraulicsResults`, and `ProjectSession.HydraulicsState`, the projections written only by `ThermalStateCoordinator` and `HydraulicsStateCoordinator`.

  **Write-set / change class:** production/test. Potential production writes stay inside `ResultsViewModel` and its tests.

  **References:** `src/ViewModels/Results/ResultsViewModel.cs`; `src/Core/CalculationContext.cs`; `src/Services/Project/ThermalStateCoordinator.cs`; `src/Services/Project/HydraulicsStateCoordinator.cs`; `src/Services/Project/ProjectSessionHydraulicsState.cs`; `docs/architecture-migration/maps/state-ownership.md` rows `ST-014`, `ST-018`, `ST-021`, `ST-022`.

  **Acceptance:** Thermal and Hydraulics projection values are read from canonical coordinator-written state. Where a stabilization contract pins the module-ViewModel seam as the data source, the equivalent canonical-source assertion replaces it and the replacement is recorded in the receipt; the user-visible behavior contract (for example result invalidation zeroes KPI without recalculation) is preserved.

  **Happy QA:** Thermal/hydraulics coordinator and Results tests prove the projection follows the canonical result, with no additional calculation or publication triggered by projection rebuild.

  **Failure QA:** A projection rebuild that triggers a second calculation, a second publication, or a stale-result read is a blocker; the Phase 7 exactly-once calculation contract must remain intact.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-8-results-derived-projection docs/architecture-migration/evidence/phase-8-results-derived-projection/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ThermalStateCoordinatorTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests|FullyQualifiedName~ResultsViewModelCollectorEquipmentItemsTests|FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests" --logger "trx;LogFileName=slice-4-thermal-hydraulics-resourcing.trx" --results-directory "docs/architecture-migration/evidence/phase-8-results-derived-projection/logs"`; receipt `docs/architecture-migration/evidence/phase-8-results-derived-projection/slice-4-thermal-hydraulics-resourcing.md`.

  **Next gate / Commit:** only Todo 5 after PASS. Expected receipt is a thermal/hydraulics re-sourcing note.

- [ ] 5. `src/ViewModels/Results/ResultsViewModel.cs` (`CheckDataReadiness`, `RecalculateKpi`, display mode) and results tests: re-source readiness, KPI inputs, and the persisted display mode - expect MissingModules/IsDataReady derived only from canonical state

  **Goal:** Re-source the data-readiness inputs (city selected, construction valid, thermal result valid, pipe selected, collectors present) and the KPI input reads from canonical state, and route the persisted `IsOperatingMode` lifecycle through `IProjectDisplayModeState` where registered.

  **Write-set / change class:** production/test.

  **References:** `src/ViewModels/Results/ResultsViewModel.cs`; `src/Services/Project/IProjectDisplayModeState.cs`; `src/Services/Project/ProjectDisplayModeState.cs`; `docs/architecture-migration/maps/state-inventory.md` row `ST-003`.

  **Acceptance:** `MissingModules` and `IsDataReady` derive only from canonical state. `IsOperatingMode` keeps its exact `.smc` save/restore wire behavior; the nullable `IProjectDisplayModeState` seam stays compatible with the legacy no-seam test setup. The obsolete `HasUnsavedData` legacy path either reads canonical state or remains provably dead with the proof recorded.

  **Happy QA:** Readiness and open-project tests prove MissingModules/IsDataReady match the canonical state in ready and not-ready scenarios.

  **Failure QA:** A readiness flip caused by module-ViewModel-only state (not mirrored in canonical state) is a blocker; any `.smc` wire change to `IsOperatingMode` is a blocker.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-8-results-derived-projection docs/architecture-migration/evidence/phase-8-results-derived-projection/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResultsStabilizationPhase1ContractsTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectSaveServiceTests" --logger "trx;LogFileName=slice-5-readiness-display-mode.trx" --results-directory "docs/architecture-migration/evidence/phase-8-results-derived-projection/logs"`; receipt `docs/architecture-migration/evidence/phase-8-results-derived-projection/slice-5-readiness-display-mode.md`.

  **Next gate / Commit:** only Todo 6 after PASS. Expected receipt is a readiness/display-mode note.

- [ ] 6. `src/ViewModels/Results/ResultsViewModel.cs` constructor, `src/Configuration/ServiceCollectionExtensions.cs`, and `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs`: remove the concrete module-ViewModel coupling - expect a ResultsViewModel with no module-VM references and unchanged DI/save behavior

  **Goal:** Remove the four concrete module-ViewModel constructor parameters and fields from `ResultsViewModel`, update `AddResultsModule` registration and test helpers, and resolve the one remaining module-ViewModel read (`SaveCurrentProject` custom templates at the report-export and legacy-fallback call sites) from the canonical templates seam with byte-identical `ProjectData` output, or stop with `OWNER_DECISION_REQUIRED` when the live code shows a divergence between the ViewModel mirror and the repository.

  **Write-set / change class:** production/test. The `ProjectData` output is frozen by Phase 6/7 characterization (`.smc` wire compatibility and report content): the switch is allowed only with executable proof that the assembled DTO is byte-identical before and after. The production file-save path already reads templates from the repository through `ProjectSnapshotPersistenceInputs`; the legacy read serves the report-export and no-save-service fallback paths. If the ViewModel mirror and the repository are provably equivalent, the switch proceeds under the frozen contract. If a divergence is found or identity cannot be proven, the slice stops with `OWNER_DECISION_REQUIRED`: the owner chooses between keeping the exact legacy output (deferral recorded in the receipt, `INV-009` claimed only partially with the exact residual named) and approving a scoped canonicalization of the report/legacy DTO source as a separate plan amendment with re-pinned characterization and manual QA.

  **References:** `src/ViewModels/Results/ResultsViewModel.cs`; `src/Configuration/ServiceCollectionExtensions.cs`; `src/Services/Project/ProjectSnapshotPersistenceInputs.cs`; `src/Repositories/Construction/IConstructionTemplateRepository.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs`; `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs`.

  **Acceptance:** Either (a) the custom-templates read is re-sourced to the canonical repository seam, the `ProjectData` output is proven byte-identical by characterization, and `ResultsViewModel` holds no concrete `ClimateViewModel`/`ConstructionViewModel`/`ThermalViewModel`/`CircuitsViewModel` reference; or (b) the slice stopped with `OWNER_DECISION_REQUIRED` and the recorded owner choice is implemented — a documented deferral (only the fully re-sourced ViewModel references are removed, the remaining read is named as legacy-cleanup debt in the receipt) or an owner-approved scoped canonicalization executed as a separate amendment. In both branches: the DI graph resolves, save and report-export output is unchanged against the frozen fixtures, and external callers (`ResultsPdfDataBuilder`, `MainWindow`, reset orchestration) use the same public surface as before.

  **Happy QA:** DI registration, save-service, and open-project round-trip tests pass with the decoupled constructor.

  **Failure QA:** A static or runtime probe asserting that `ResultsViewModel` references no concrete module ViewModel type must pass; any Results write into canonical state found by the probe is a blocker.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-8-results-derived-projection docs/architecture-migration/evidence/phase-8-results-derived-projection/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ProjectSaveServiceTests|FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests" --logger "trx;LogFileName=slice-6-module-vm-decoupling.trx" --results-directory "docs/architecture-migration/evidence/phase-8-results-derived-projection/logs"`; receipt `docs/architecture-migration/evidence/phase-8-results-derived-projection/slice-6-module-vm-decoupling.md`.

  **Next gate / Commit:** only Todo 7 after PASS. Expected receipt is a module-ViewModel decoupling note.

- [ ] 7. `src/ViewModels/Results/ResultsViewModel.cs` and flow characterization tests: prove projection multiplicity, rejected-restore preservation, and the fresh-vs-stale sentinel - expect exact update counts and no stale DTO surfacing

  **Goal:** Prove the Results-layer behavioral contracts: exactly one `RefreshAll()` per successful restore, no projection rebuild on rejected restore, one rebuild per export/navigation trigger, and a positive fresh-vs-stale sentinel where persisted DTO result fields differ from current session/calculation state.

  **Write-set / change class:** production/test.

  **References:** `src/ViewModels/Results/ResultsViewModel.cs`; `src/Services/Project/ProjectLoadOrchestrator.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`; `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs`; `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md` Slice 5.

  **Acceptance:** The projection rebuild count matches the characterized expectation per scenario, the rejected-restore path leaves the prior projection intact (Phase 7 slice-7 contract preserved), and after restore plus calculation the projection exposes session-derived values with the stale persisted DTO sentinel absent.

  **Happy QA:** Open-project and lifecycle flow tests prove one refresh per successful restore and the fresh-vs-stale sentinel holds.

  **Failure QA:** A rejected restore that refreshes or partially rebuilds the projection, a multiplied rebuild count, or stale DTO sentinel data surfacing through the projection is a blocker.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-8-results-derived-projection docs/architecture-migration/evidence/phase-8-results-derived-projection/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResetOrchestrationTests" --logger "trx;LogFileName=slice-7-multiplicity-sentinel.trx" --results-directory "docs/architecture-migration/evidence/phase-8-results-derived-projection/logs"`; receipt `docs/architecture-migration/evidence/phase-8-results-derived-projection/slice-7-multiplicity-sentinel.md`.

  **Next gate / Commit:** only Todo 8 after PASS. Expected receipt is a multiplicity/sentinel note.

- [ ] 8. `docs/architecture-migration/` maps, model, and widget: refresh the Phase 8 dossier only as needed - expect evidence that matches the live write-set and no scope creep

  **Goal:** Update the architecture dossier so it describes the accepted Results derived-projection boundary: `INV-009` flipped to verified with the Phase 8 evidence references, `ST-003` and `ST-024..ST-027` marked covered, `INV-016` Results clause noted, and `INV-008`/`INV-010` explicitly left open for the next phase.

  **Write-set / change class:** architecture evidence only.

  **References:** the Phase 8 plan; `docs/architecture-migration/maps/{state-ownership,state-inventory,target-invariants,reactive,compile-time,di-runtime,user-flow,characterization-tests}.md`; `docs/architecture-migration/maps/architecture-model.json`; `docs/architecture-migration/TASK_CONTEXT.md`; the phase-specific evidence directory.

  **Acceptance:** The maps record the canonical read sources and the removed module-ViewModel coupling exactly as implemented; the model/widget reflect the live boundary; no unknown reactive counter is erased without a receipt; no Markdown-removal or export-behavior claim appears.

  **Happy QA:** Evidence artifacts reference the same live boundaries and invariants established in Todos 1-7, including the build-before-test receipts, the canonical-source map, and the fresh-vs-stale sentinel proof.

  **Failure QA:** Any attempt to claim `INV-008`, `INV-010`, legacy-alias removal, or `CalculationContext` writer changes as part of Phase 8 must be rejected and recorded as out of scope.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-8-results-derived-projection 2>nul`; `node docs/architecture-migration/widget/verify-widget.mjs`; `git diff --check`; worker content-review of the architecture/evidence diff mapping each changed dossier statement to Todos 1-7 evidence; receipt `docs/architecture-migration/evidence/phase-8-results-derived-projection/slice-8-dossier-alignment.md`.

  **Next gate / Commit:** only Final verification wave after PASS. Expected receipt is a dossier-alignment note.

## Final verification wave

- [ ] F1. Scope, provenance and invariant check - expect the plan to preserve the approved Phases 2-7 contracts and the current Phase 8 boundary

  Verify canonical plan identity, scope, must-not-have rules, and the preserved invariants from the approved earlier phases. Confirm the plan still rejects `.smc` format drift, `CalculationContext` writer changes (`DEC-001 = A`), `INV-008`/legacy-cleanup creep, alias removal, and any new owner gate for private implementation details.

- [ ] F2. Code-boundary and architecture check - expect one canonical source per projection read, a read-only Results layer, and no second canonical store

  Independently inspect the live source paths for the Results projection reads, trigger paths, DI wiring, and canonical sources. Confirm the worker can point to the exact current source of truth for each projected value and that no hidden module-ViewModel path or canonical write-back remains.

- [ ] F3. Executable QA check - expect every slice to have agent-executed happy/failure coverage with named commands and receipts

  Verify that each slice lists a concrete command or test filter, an explicit build-before-test step where focused tests run with `--no-build`, an explicit receipt path, and at least one happy and one failure assertion. Do not re-run product tests in this planning session; this wave audits the worker-facing commands and receipts recorded in Todos 1-8. Any command plan that could pass with 0 matching tests fails this gate.

- [ ] F4. Consolidated stop check - expect one final receipt set and then a stop for owner acceptance

  Consolidate the three review domains, confirm the plan is still within scope, and stop without execution. The result is only a plan handoff; any later execution still requires the separate worker session started by the user.

## Commit strategy

The planner does not stage, commit, or run product code. Only the Phase 8 plan artifact is authored in this session. Execution, if approved later, belongs to the separate worker session.

## Success criteria

- The plan contains 8 execution slices plus 4 final verification gates, each with concrete commands, receipts, and happy/failure QA.
- Every Results projection read is re-sourced from canonical state with a proven source map, and the fully re-sourced module-ViewModel references are removed from `ResultsViewModel` and its DI wiring; any deferred read (the custom-templates seam) is explicitly recorded as legacy-cleanup debt rather than silently kept.
- Read-only projection, projection-rebuild multiplicity, rejected-restore preservation, and the fresh-vs-stale sentinel are all proven with executable evidence.
- The Phase 7 restore/calculation/save contracts, `.smc` wire compatibility, and `DEC-001 = A` `CalculationContext` disposition are preserved unchanged.
- The plan contains no `INV-008`/legacy-owner-removal work, no new canonical store, no export/markdown feature work, and no scope drift beyond the approved Phase 8 boundary.
