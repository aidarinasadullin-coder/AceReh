---
phase: phase-1-project-session-shell
snapshot_sha: 021d4abd159aa71c4a19c7a6536851264e5a58ca
source_basis: accepted-phase-1-project-session-shell
generated_at_utc: 2026-08-04T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - codegraph_codegraph_explore "Phase 0 Todo 6 compile-time DI runtime mapping: ProjectLoadOrchestrator ClimateViewModel ConstructionViewModel ThermalViewModel CircuitsViewModel ConstructionRepository MaterialNotFoundException ResultsViewModel DiRegistrationTests module ViewModel service registrations constructor dependencies compose resolve create paths"
  - Read SnowMeltingCalculator.sln
  - Read src/SnowMeltingCalculator.csproj
  - Read tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj
  - Read src/Configuration/ServiceCollectionExtensions.cs
  - Read tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs
  - codegraph_codegraph_explore "ProjectLoadOrchestrator ResultsViewModel constructors full direct dependencies; ServiceCollectionExtensions DI registrations IMaterialRepository MaterialRepository IConstructionService ConstructionService IDialogService MessageBoxService exact AddSingleton lifetimes"
  - PowerShell read-only structural assertions across maps/compile-time.md and maps/di-runtime.md
  - node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2
  - node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2
  - node docs/architecture-migration/widget/generate-widget.mjs --check
exit_code: 0
status: pass
raw_output: Current indexed source plus targeted project/config/test reads; the source selection and the executed structural-QA output are recorded below. Phase 1 lifecycle shell nodes added.
limitations:
  - This is a selected, provisional compile-time filter, not a complete compiler semantic graph or repository-wide namespace/type census.
  - SCCs and cycles are unavailable: evidence/metrics-baseline.json reports SCC null/degraded and cycle count null/not-reproducible; no cycle claim is made here.
  - A compile-time type reference does not prove DI resolution, runtime invocation, user flow, or ownership.
  - Phase 1 added only `ProjectSession`/`IProjectSession` under `src/Services/Project`; module slices and their compile-time graph remain unchanged.
---

# Compile-Time Research View

## Filter and Evidence Rules

**View membership:** `compile-time` only. This receipt admits solution/project references, namespace/type declarations, constructor parameter type references, inheritance/type-use evidence, and direct `using` evidence. It excludes DI registrations, service-provider resolution, and runtime invocation, which belong exclusively to `di-runtime.md`.

Every declared edge has one allowed kind, direct source evidence, and confidence:

| Confidence | Meaning |
| --- | --- |
| `verified` | A current source/project file directly states the reference. |
| `derived` | A bounded conclusion combines verified source facts without asserting an unobserved path. |
| `degraded` | The requested completeness/property is unavailable; the gap remains explicit. |

Allowed edge kinds in this filter: `solution-project-reference`, `project-reference`, `namespace-declaration`, `constructor-type-reference`, `used-type-reference`, `inheritance-reference`, `using-reference`.

## Provisional Nodes

Stable prefix: `CTN-`. All nodes below are members of `compile-time`.

| Node ID | Kind | Display name | Source evidence |
| --- | --- | --- | --- |
| `CTN-001` | solution | `SnowMeltingCalculator.sln` | `SnowMeltingCalculator.sln:5-8` |
| `CTN-002` | project | `SnowMeltingCalculator` | `src/SnowMeltingCalculator.csproj:1-53` |
| `CTN-003` | project | `SnowMeltingCalculator.Tests` | `tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj:1-29` |
| `CTN-004` | namespace | `SnowMeltingCalculator.Configuration` | `src/Configuration/ServiceCollectionExtensions.cs:27-32` |
| `CTN-005` | type | `ServiceCollectionExtensions` | `src/Configuration/ServiceCollectionExtensions.cs:32-37` |
| `CTN-006` | namespace | `SnowMeltingCalculator.Services.Project` | `src/Services/Project/ProjectLoadOrchestrator.cs:38-53` |
| `CTN-007` | type | `ProjectLoadOrchestrator` | `src/Services/Project/ProjectLoadOrchestrator.cs:38-53` |
| `CTN-008` | type | `ClimateViewModel` | `src/ViewModels/Climate/ClimateViewModel.cs:217-231` |
| `CTN-009` | type | `ConstructionViewModel` | `src/ViewModels/Construction/ConstructionViewModel.cs:214-248` |
| `CTN-010` | type | `ThermalViewModel` | `src/ViewModels/Thermal/ThermalViewModel.cs:241-287` |
| `CTN-011` | type | `CircuitsViewModel` | `src/ViewModels/Hydraulics/CircuitsViewModel.cs:704-742` |
| `CTN-012` | interface | `ICalculationStateService` | `src/Services/Project/ProjectLoadOrchestrator.cs:43-53` |
| `CTN-013` | interface | `IConstructionService` | `src/Services/Project/ProjectLoadOrchestrator.cs:44-53` |
| `CTN-014` | type | `CalculationContext` | `src/Services/Project/ProjectLoadOrchestrator.cs:45-53` |
| `CTN-015` | namespace | `SnowMeltingCalculator.Repositories.Construction` | `src/Repositories/Construction/ConstructionRepository.cs:19-32` |
| `CTN-016` | type | `ConstructionRepository` | `src/Repositories/Construction/ConstructionRepository.cs:21-32` |
| `CTN-017` | interface | `IMaterialRepository` | `src/Repositories/Construction/ConstructionRepository.cs:21-24` |
| `CTN-018` | namespace | `SnowMeltingCalculator.Services.Construction` | `src/Services/Construction/MaterialNotFoundException.cs:3-9` |
| `CTN-019` | type | `MaterialNotFoundException` | `src/Services/Construction/MaterialNotFoundException.cs:9-41` |
| `CTN-020` | type | `ResultsViewModel` | `src/ViewModels/Results/ResultsViewModel.cs:478-520` |
| `CTN-021` | interface | `IProjectStateService` | `src/ViewModels/Results/ResultsViewModel.cs:478-511` |
| `CTN-022` | interface | `IMarkDirtyService` | `src/ViewModels/Results/ResultsViewModel.cs:478-511` |
| `CTN-023` | interface | `IDialogService` | `src/ViewModels/Results/ResultsViewModel.cs:478-511` |
| `CTN-024` | interface | `IPdfExportService` | `src/ViewModels/Results/ResultsViewModel.cs:478-511` |
| `CTN-025` | interface | `ICalculationReportExportService` | `src/ViewModels/Results/ResultsViewModel.cs:478-511` |
| `CTN-026` | interface | `IProjectFileService` | `src/ViewModels/Results/ResultsViewModel.cs:478-511` |
| `CTN-027` | type | `ResultsPdfDataBuilder` | `src/ViewModels/Results/ResultsViewModel.cs:493-511` |
| `CTN-028` | type | `HydraulicSummaryBuilder` | `src/ViewModels/Results/ResultsViewModel.cs:494-511` |
| `CTN-029` | test type | `DiRegistrationTests` | `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:18-30` |
| `CTN-030` | type | `MaterialEditorViewModel` | `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:39-46` |

### Phase 2 ClimateState compile-time overlay

| Node ID | Kind | Display name | Source evidence |
| --- | --- | --- | --- |
| `CTN-P2-CLIMATE-001` | interface | `IProjectSessionClimateState` | `src/Services/Project/IProjectSessionClimateState.cs`; evidence `docs/architecture-migration/evidence/phase-2-climate-state/climate-state-api.md` |
| `CTN-P2-CLIMATE-002` | type | `ProjectSessionClimateState` | `src/Services/Project/ProjectSessionClimateState.cs`; evidence `climate-state-api.md`, `downstream-invalidation.md` |
| `CTN-P2-CLIMATE-003` | record/type set | `ClimateStateSnapshot`, `ClimateEdit`, `ClimateMutationOrigin`, `ClimateMutationResult`, `ClimateStateChangedEventArgs` | `src/Services/Project/Climate*.cs`; evidence `climate-state-api.md` |
| `CTN-P2-CLIMATE-004` | type/interface | `ClimateData` / `IClimateData` projection boundary | `src/Models/Climate/ClimateData.cs`; evidence `climate-data-projection.md` |

| Edge ID | Kind | From | To | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| `CTE-P2-CLIMATE-001` | ownership/type-use | `ProjectSession` | `ProjectSessionClimateState` | `ProjectSession.cs` private readonly field and `ClimateState` property; `climate-state-api.md` | verified |
| `CTE-P2-CLIMATE-002` | interface implementation | `ProjectSessionClimateState` | `IProjectSessionClimateState` | `ProjectSessionClimateState.cs`; `IProjectSessionClimateState.cs` | verified |
| `CTE-P2-CLIMATE-003` | constructor/interface reference | `ClimateViewModel` | `IProjectSessionClimateState` / `IProjectSession` | `ClimateViewModel.cs`; `climate-viewmodel-adapter.md` | verified |
| `CTE-P2-CLIMATE-004` | compatibility projection reference | `ProjectSessionClimateState` | `ClimateData` / `CalculationContext` | `ProjectSessionClimateState.cs`; `downstream-invalidation.md` | verified |
| `CTE-P2-CLIMATE-005` | persistence snapshot read | `ResultsViewModel` | `IProjectSession.ClimateState` | `ResultsViewModel.cs`; `persistence-results.md`; `affected-gates.md` | verified |
| `CTN-031` | type | `TemplateEditorViewModel` | `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:49-56` |

## Typed Edges

Stable prefix: `CTE-`. Every row has `view_membership=compile-time`.

| Edge ID | Kind | From | To | Source evidence | Confidence | View membership |
| --- | --- | --- | --- | --- | --- | --- |
| `CTE-001` | solution-project-reference | `CTN-001` | `CTN-002` | Solution project entry at `SnowMeltingCalculator.sln:5` | verified | compile-time |
| `CTE-002` | solution-project-reference | `CTN-001` | `CTN-003` | Solution project entry at `SnowMeltingCalculator.sln:7` | verified | compile-time |
| `CTE-003` | project-reference | `CTN-003` | `CTN-002` | `<ProjectReference Include="..\..\src\SnowMeltingCalculator.csproj" />` at `tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj:25-27` | verified | compile-time |
| `CTE-004` | namespace-declaration | `CTN-005` | `CTN-004` | `namespace SnowMeltingCalculator.Configuration` and `ServiceCollectionExtensions` at `src/Configuration/ServiceCollectionExtensions.cs:27-32` | verified | compile-time |
| `CTE-005` | constructor-type-reference | `CTN-007` | `CTN-008` | Constructor parameter `ClimateViewModel` at `src/Services/Project/ProjectLoadOrchestrator.cs:38-47`; also `REC-033` | verified | compile-time |
| `CTE-006` | constructor-type-reference | `CTN-007` | `CTN-009` | Constructor parameter `ConstructionViewModel` at `src/Services/Project/ProjectLoadOrchestrator.cs:38-48`; also `REC-033` | verified | compile-time |
| `CTE-007` | constructor-type-reference | `CTN-007` | `CTN-010` | Constructor parameter `ThermalViewModel` at `src/Services/Project/ProjectLoadOrchestrator.cs:38-49`; also `REC-033` | verified | compile-time |
| `CTE-008` | constructor-type-reference | `CTN-007` | `CTN-011` | Constructor parameter `CircuitsViewModel` at `src/Services/Project/ProjectLoadOrchestrator.cs:38-50`; also `REC-033` | verified | compile-time |
| `CTE-009` | constructor-type-reference | `CTN-007` | `CTN-012` | Constructor parameter `ICalculationStateService` at `src/Services/Project/ProjectLoadOrchestrator.cs:43-51` | verified | compile-time |
| `CTE-010` | constructor-type-reference | `CTN-007` | `CTN-013` | Constructor parameter `IConstructionService` at `src/Services/Project/ProjectLoadOrchestrator.cs:44-52` | verified | compile-time |
| `CTE-011` | constructor-type-reference | `CTN-007` | `CTN-014` | Constructor parameter `CalculationContext` at `src/Services/Project/ProjectLoadOrchestrator.cs:45-53` | verified | compile-time |
| `CTE-012` | constructor-type-reference | `CTN-016` | `CTN-017` | Constructor parameter `IMaterialRepository` at `src/Repositories/Construction/ConstructionRepository.cs:21-24` | verified | compile-time |
| `CTE-013` | used-type-reference | `CTN-016` | `CTN-019` | Current `ConstructionRepository` source is reconciled as a type-level dependency on `MaterialNotFoundException`; `REC-008`, `TASK_CONTEXT.md:100-102`, `src/Services/Construction/MaterialNotFoundException.cs:1-42` | verified | compile-time |
| `CTE-014` | constructor-type-reference | `CTN-020` | `CTN-021` | Parameter `IProjectStateService` at `src/ViewModels/Results/ResultsViewModel.cs:478-496` | verified | compile-time |
| `CTE-015` | constructor-type-reference | `CTN-020` | `CTN-022` | Parameter `IMarkDirtyService` at `src/ViewModels/Results/ResultsViewModel.cs:479-497` | verified | compile-time |
| `CTE-016` | constructor-type-reference | `CTN-020` | `CTN-023` | Parameter `IDialogService` at `src/ViewModels/Results/ResultsViewModel.cs:480-498` | verified | compile-time |
| `CTE-017` | constructor-type-reference | `CTN-020` | `CTN-024` | Parameter `IPdfExportService` at `src/ViewModels/Results/ResultsViewModel.cs:481-499` | verified | compile-time |
| `CTE-018` | constructor-type-reference | `CTN-020` | `CTN-025` | Parameter `ICalculationReportExportService` at `src/ViewModels/Results/ResultsViewModel.cs:482-500` | verified | compile-time |
| `CTE-019` | constructor-type-reference | `CTN-020` | `CTN-026` | Parameter `IProjectFileService` at `src/ViewModels/Results/ResultsViewModel.cs:483-501` | verified | compile-time |
| `CTE-020` | constructor-type-reference | `CTN-020` | `CTN-012` | Parameter `ICalculationStateService` at `src/ViewModels/Results/ResultsViewModel.cs:485-502` | verified | compile-time |
| `CTE-021` | constructor-type-reference | `CTN-020` | `CTN-017` | Parameter `IMaterialRepository` at `src/ViewModels/Results/ResultsViewModel.cs:486-503` | verified | compile-time |
| `CTE-022` | constructor-type-reference | `CTN-020` | `CTN-013` | Parameter `IConstructionService` at `src/ViewModels/Results/ResultsViewModel.cs:487-504` | verified | compile-time |
| `CTE-023` | constructor-type-reference | `CTN-020` | `CTN-008` | Concrete parameter `ClimateViewModel` at `src/ViewModels/Results/ResultsViewModel.cs:488-505`; `REC-030` | verified | compile-time |
| `CTE-024` | constructor-type-reference | `CTN-020` | `CTN-009` | Concrete parameter `ConstructionViewModel` at `src/ViewModels/Results/ResultsViewModel.cs:489-506`; `REC-030` | verified | compile-time |
| `CTE-025` | constructor-type-reference | `CTN-020` | `CTN-010` | Concrete parameter `ThermalViewModel` at `src/ViewModels/Results/ResultsViewModel.cs:490-507`; `REC-030` | verified | compile-time |
| `CTE-026` | constructor-type-reference | `CTN-020` | `CTN-011` | Concrete parameter `CircuitsViewModel` at `src/ViewModels/Results/ResultsViewModel.cs:491-508`; `REC-030` | verified | compile-time |
| `CTE-027` | constructor-type-reference | `CTN-020` | `CTN-007` | Concrete parameter `ProjectLoadOrchestrator` at `src/ViewModels/Results/ResultsViewModel.cs:492-509`; `REC-018` | verified | compile-time |
| `CTE-028` | constructor-type-reference | `CTN-020` | `CTN-027` | Concrete parameter `ResultsPdfDataBuilder` at `src/ViewModels/Results/ResultsViewModel.cs:493-510`; `REC-018` | verified | compile-time |
| `CTE-029` | constructor-type-reference | `CTN-020` | `CTN-028` | Concrete parameter `HydraulicSummaryBuilder` at `src/ViewModels/Results/ResultsViewModel.cs:494-511`; `REC-018` | verified | compile-time |
| `CTE-030` | used-type-reference | `CTN-029` | `CTN-005` | `services.AddApplicationServices()` in test setup at `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:27-29` | verified | compile-time |
| `CTE-031` | used-type-reference | `CTN-029` | `CTN-030` | `GetService<MaterialEditorViewModel>()` test at `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:39-46` | verified | compile-time |
| `CTE-032` | used-type-reference | `CTN-029` | `CTN-031` | `GetService<TemplateEditorViewModel>()` test at `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:49-56` | verified | compile-time |
| `CTE-033` | used-type-reference | `CTN-029` | `CTN-009` | `GetService<ConstructionViewModel>()` test at `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:89-97` | verified | compile-time |

## Required Findings and Boundaries

1. `ProjectLoadOrchestrator` has four direct concrete module-ViewModel constructor references (`CTE-005` through `CTE-008`). This is compile-time coupling, not evidence that its reset/load methods ran.
2. `ConstructionRepository -> MaterialNotFoundException` (`CTE-013`) is a repository-to-service type coupling. `REC-008` controls interpretation: it is neither a runtime invocation nor proof of a cycle.
3. `ResultsViewModel` has all 16 direct selected constructor references at `CTE-014` through `CTE-029`: seven abstraction references, four concrete module VMs, and three concrete collaborators. The corresponding DI constructibility cross-check is in `di-runtime.md`; this remains compile-time coupling rather than proof of an invocation.
4. `DiRegistrationTests` contributes test-source type evidence (`CTE-030` through `CTE-033`). Its provider construction/resolution behavior is modeled separately, and only where explicitly observed, in `di-runtime.md`.

## Explicit Incomplete-Graph Boundary

The provisional node/edge set is intentionally selected to satisfy Todo 6 required seams. It does **not** enumerate all namespaces, all declared types, all project/package references, all `using` directives, all constructor parameters, indirect generic type uses, reflection, generated code, or dynamic dispatch. It makes no repository-wide completeness, SCC, or cycle claim. `REC-004` and `evidence/codegraph-baseline.md:64,101-108` control this limitation.

## QA Record

The read-only PowerShell structural QA and synthetic bare-`using` probe are recorded in full in `di-runtime.md` because the validation spans both receipts. The corrected QA additionally asserts all 7 `ProjectLoadOrchestrator` and all 16 `ResultsViewModel` selected constructor parameters have DI/runtime rows. Result: `pass`; this map contributes 31 declared nodes and 33 typed edges. The final full-file read-back occurred after QA.

## Phase 1 ProjectSession lifecycle shell overlay

Added two new compile-time nodes under `src/Services/Project`:

| Node ID | Kind | Display name | Source evidence |
| --- | --- | --- | --- |
| `CTN-PS` | implementation | `ProjectSession` | `src/Services/Project/ProjectSession.cs` |
| `CTN-IPS` | interface | `IProjectSession` | `src/Services/Project/IProjectSession.cs` |

No new compile-time edges from application services to concrete ViewModels were
introduced. `ProjectSession` references only `System` and `System.ComponentModel`;
module slices (Climate, Construction, Thermal, Hydraulics) and `CalculationContext`
remain untouched. See `docs/architecture-migration/evidence/phase-1-project-session-shell/final-gates.md`.

## Phase 3 ConstructionState compile-time overlay

| Node ID | Kind | Display name | Source evidence |
| --- | --- | --- | --- |
| `CTN-P3-CONSTRUCTION-001` | interface | `IProjectSessionConstructionState` | `src/Services/Project/IProjectSessionConstructionState.cs`; Task 11 evidence |
| `CTN-P3-CONSTRUCTION-002` | implementation | `ProjectSessionConstructionState` | `src/Services/Project/ProjectSessionConstructionState.cs`; Task 10 evidence |
| `CTN-P3-CONSTRUCTION-003` | immutable contracts | Construction snapshot, layer snapshot, mutation/origin/result | `src/Services/Project/Construction*.cs`; Tasks 4-5 evidence |
| `CTN-P3-CONSTRUCTION-004` | read projection | `ConstructionStateProjection` / `IConstructionData` | Task 11 DI evidence |
| `CTN-P3-CONSTRUCTION-005` | pure mapper | `ConstructionPersistenceMapper` | Task 9 recovery evidence |

`ProjectSession` owns/exposes the state implementation; `ConstructionViewModel`
consumes its interface as a WPF adapter; `ResultsViewModel` maps its snapshot for
save; Thermal consumes its read projection. The existing concrete ViewModel
dependencies in `ProjectLoadOrchestrator` remain and keep `INV-008` open.

## Phase 3.1 Climate invalidation overlay (Task 11)

No new project or package reference was introduced. Verified type-level changes
are `ClimateMutationOrigin.UserReset`, `ClimateMutationOrigin.ProjectLoadReset`,
the explicit `ClimateData.ApplyProjection` publication parameter, and the
existing `ProjectSessionClimateState` completion call path. DI/runtime evidence
is excluded from this filter. Construction type references in shared files are
pre-existing Phase 3 Construction content, not Task 11 Climate edges.

## Phase 4 ThermalState compile-time overlay (Task 14)

| Node ID | Kind | Display name | Source evidence |
| --- | --- | --- | --- |
| `CTN-P4-THERMAL-001` | interface | `IProjectSessionThermalState` | `src/Services/Project/IProjectSessionThermalState.cs:14-100`; evidence `task-3/task-3-thermal-state-contract.md` |
| `CTN-P4-THERMAL-002` | implementation | `ProjectSessionThermalState` (sealed) | `src/Services/Project/ProjectSessionThermalState.cs:16`; evidence `task-3/task-3-thermal-state-contract.md` |
| `CTN-P4-THERMAL-003` | immutable contracts | `ThermalStateSnapshot`, `ThermalInputsSnapshot`, `ThermalResultSnapshot`, `ThermalStatusSnapshot`, `ThermalInputEdit`, `ThermalStateChangedEventArgs` | `src/Services/Project/ThermalStateSnapshots.cs`; evidence `task-3/task-3-thermal-state-contract.md` |
| `CTN-P4-THERMAL-004` | enum/result set | `ThermalMutationOrigin`, `ThermalMutationStatus`, `ThermalMutationResult` | `src/Services/Project/ThermalMutationOrigin.cs:8-36`; `src/Services/Project/ThermalMutationResult.cs`; evidence `task-3/task-3-thermal-state-contract.md` |
| `CTN-P4-THERMAL-005` | interface | `IThermalStateCoordinator` (+ `ThermalCalculationOutcome`) | `src/Services/Project/IThermalStateCoordinator.cs:23-93`; evidence `task-6/task-567-merged-boundary.md` |
| `CTN-P4-THERMAL-006` | implementation | `ThermalStateCoordinator` (sealed) | `src/Services/Project/ThermalStateCoordinator.cs:34`; evidence `task-6/task-567-merged-boundary.md` |
| `CTN-P4-THERMAL-007` | pure mapper | `ThermalPersistenceMapper` | `src/Services/Project/ThermalPersistenceMapper.cs:49,103,182,216`; evidence `task-10/task-10-persistence-results.md` |

| Edge ID | Kind | From | To | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| `CTE-P4-THERMAL-001` | ownership/type-use | `ProjectSession` | `ProjectSessionThermalState` / `IProjectSessionThermalState` | private readonly field and `ThermalState` property at `ProjectSession.cs:26,35,41`; `IProjectSession.cs` | verified |
| `CTE-P4-THERMAL-002` | interface implementation | `ProjectSessionThermalState` | `IProjectSessionThermalState` | `ProjectSessionThermalState.cs:16` | verified |
| `CTE-P4-THERMAL-003` | constructor/interface reference | `ThermalViewModel` | `IThermalStateCoordinator` | ctor parameter and command routing at `ThermalViewModel.cs:227-276,322-385` | verified |
| `CTE-P4-THERMAL-004` | coordinator/state reference | `ThermalStateCoordinator` | `IProjectSessionThermalState`, `CalculationContext`, `IMarkDirtyService`, `IThermalCalculator`, `IClimateData`, `IConstructionData` | ctor at `ThermalStateCoordinator.cs:61-93` | verified |
| `CTE-P4-THERMAL-005` | persistence mapper references | `ResultsViewModel` / `ProjectLoadOrchestrator` | `ThermalPersistenceMapper` | save `ResultsViewModel.cs:1705-1706`; restore `ProjectLoadOrchestrator.cs:132-150,218-221` | verified |

`ProjectSession` owns/exposes the Thermal state implementation;
`ThermalViewModel` consumes the coordinator as a WPF adapter; Results maps the
canonical snapshot for save; restore goes through canonical `Restore`. The
existing concrete ViewModel dependencies in `ProjectLoadOrchestrator` remain
(`INV-008` still open). No new project or package reference was introduced.

## Phase 5 HydraulicsState compile-time overlay (Task 14)

| Node ID | Kind | Display name | Source evidence |
| --- | --- | --- | --- |
| `CTN-P5-HYDRAULICS-001` | interface | `IProjectSessionHydraulicsState` | `src/Services/Project/IProjectSessionHydraulicsState.cs:6-18`; evidence `task-4/di-negative-probe.md` |
| `CTN-P5-HYDRAULICS-002` | implementation | `ProjectSessionHydraulicsState` | `src/Services/Project/ProjectSessionHydraulicsState.cs:41-97`; evidence `task-9/divergence-notes.md` |
| `CTN-P5-HYDRAULICS-003` | immutable contracts | `HydraulicsStateSnapshot`, `HydraulicGlobalInputsSnapshot`, `HydraulicCollectorSnapshot`, `HydraulicCircuitSnapshot`, `HydraulicCircuitResultSnapshot`, `HydraulicCollectorSummarySnapshot`, `HydraulicsStatusSnapshot`, `HydraulicsStateChangedEventArgs` | `src/Services/Project/HydraulicsStateSnapshots.cs`; evidence `task-2/trx-characterization-release.json` |
| `CTN-P5-HYDRAULICS-004` | enum/result set | `HydraulicsMutationOrigin`, `HydraulicsMutationStatus`, `HydraulicsMutationResult` | `src/Services/Project/HydraulicsMutationOrigin.cs`; evidence `task-9/divergence-notes.md` |
| `CTN-P5-HYDRAULICS-005` | interface | `IHydraulicsStateCoordinator` | `src/Services/Project/IHydraulicsStateCoordinator.cs`; evidence `task-7/trx-coordinator-release.json` |
| `CTN-P5-HYDRAULICS-006` | implementation | `HydraulicsStateCoordinator` (sealed) | `src/Services/Project/HydraulicsStateCoordinator.cs:11`; evidence `task-7/trx-coordinator-release.json` |
| `CTN-P5-HYDRAULICS-007` | pure mapper | `HydraulicsPersistenceMapper` | `src/Services/Project/HydraulicsPersistenceMapper.cs:13-15`; evidence `task-6/correction-notes.md` |

| Edge ID | Kind | From | To | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| `CTE-P5-HYDRAULICS-001` | ownership/type-use | `ProjectSession` | `ProjectSessionHydraulicsState` / `IProjectSessionHydraulicsState` | private readonly field and `HydraulicsState` property at `ProjectSession.cs:27,39,41-47`; ctor third parameter `IMarkDirtyService? hydraulicsDirtyService = null` | verified |
| `CTE-P5-HYDRAULICS-002` | constructor/interface reference | `CircuitsViewModel` (`CTN-011`) | `IHydraulicsStateCoordinator`, `IProjectSession` | required ctor parameters and slice binding at `CircuitsViewModel.cs:898-918` | verified |
| `CTE-P5-HYDRAULICS-003` | coordinator/state reference | `HydraulicsStateCoordinator` | `IProjectSessionHydraulicsState`, `ICalculationStateService`, `CalculationContext` | ctor at `HydraulicsStateCoordinator.cs:23-34` | verified |
| `CTE-P5-HYDRAULICS-004` | persistence mapper references | `ResultsViewModel` / `ProjectLoadOrchestrator` | `HydraulicsPersistenceMapper` | save `ResultsViewModel.cs:1711-1712`; restore `ProjectLoadOrchestrator.cs:171,200` | verified |
| `CTE-P5-HYDRAULICS-005` | factory/type reference | `ServiceCollectionExtensions` | `ProjectSession` explicit factory, `HydraulicsStateCoordinator` factory | `AddResultsModule` at `ServiceCollectionExtensions.cs:196-199`; `AddHydraulicsModule` at `:148-151` | verified |

`ProjectSession` owns/exposes the Hydraulics state implementation; `CircuitsViewModel` consumes the
coordinator and the session slice as a WPF adapter; Results maps the canonical snapshot for save;
restore goes through canonical `Restore`. The existing concrete ViewModel dependencies in
`ProjectLoadOrchestrator` remain (`INV-008` still open). No new project or package reference was
introduced.

## Phase 6 Save-Boundary Overlay

The source-backed save boundary is `ProjectSession -> ProjectSnapshot -> ProjectPersistenceMapper -> ProjectData -> IProjectFileService/ProjectFileService`. `ProjectSnapshot` is assembled from the aggregate session, the mapper is pure, and the existing DTO/file-service contract remains unchanged. Evidence: `task-5-save-boundary.md`; model records `PN-P6-SNAPSHOT`, `PN-P6-MAPPER`, `PN-P6-DATA`, `PN-P6-SERVICE`, `PE-P6-SESSION-SNAPSHOT`, `PE-P6-SNAPSHOT-MAPPER`, `PE-P6-MAPPER-DATA`, `PE-P6-SERVICE-DATA`. This overlay does not claim restore migration, Markdown/export completion, calculation completion, or broad ownership cleanup.


## Phase 7 Restore Coordinator Overlay (docs-only refresh)

The accepted Phase 7 receipts fix the compile-time restore facts: the single canonical restore path is `ResultsViewModel.LoadProjectFromPathAsync -> ApplyLoadedProjectAsync -> LoadProjectDataAsync`, which acquires the `ProjectSession.BeginProjectRestore()` lease and calls `ProjectLoadOrchestrator.RestoreModulesFromProjectAsync`; `ProjectLoadOrchestrator` captures exactly the four canonical slice interfaces (`ClimateState`, `ConstructionState`, `ThermalState`, `HydraulicsState`) exposed through `IProjectSession`, and no second restore coordinator type was introduced. The read-only negative probe found no restore entrypoint bypassing `BeginProjectRestore`. `INV-008` stays open: the orchestrator's concrete ViewModel dependencies are not removed by Phase 7. Evidence: `slice-1-restore-boundary.md`, `slice-7-di-ui-alignment.md`; model records `EV-P7-SCOPE`, `EV-P7-ACCEPTANCE`.

Phase 7.5 docs-only dossier refresh (plan `docs/architecture-migration/plans/phase-7.5-project-restore-coordinator-relaunch.md`, owner-approved 2026-09-03, worktree `D:/IA/ace — копия`); this overlay adds no production or test claim beyond the accepted Phase 7 receipts.

## Phase 8 Results-Derived-Projection Overlay

`ResultsViewModel` constructor no longer takes `ClimateViewModel`, `ConstructionViewModel` or `ThermalViewModel`; the public `ConstructionViewModel` exposure was removed (no consumers). Remaining module reference: `CircuitsViewModel` (staged Phase 9 residual). New optional dependency: `IProjectSnapshotPersistenceInputs` (repository-backed, no ViewModel). `ProjectSessionClimateState`/`ClimateStateSnapshot` gained the additive `Period0Days` field (Amendment 1, owner decision B). Evidence: `slice-3`, `slice-6` receipts; model records `EV-P8-SLICE-3`, `EV-P8-SLICE-6`.
