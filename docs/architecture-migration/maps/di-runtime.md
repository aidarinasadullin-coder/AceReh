---
phase: phase-1-project-session-shell
snapshot_sha: 021d4abd159aa71c4a19c7a6536851264e5a58ca
source_basis: accepted-phase-1-project-session-shell
generated_at_utc: 2026-08-04T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - codegraph_codegraph_explore "Phase 0 Todo 6 compile-time DI runtime mapping: ProjectLoadOrchestrator ClimateViewModel ConstructionViewModel ThermalViewModel CircuitsViewModel ConstructionRepository MaterialNotFoundException ResultsViewModel DiRegistrationTests module ViewModel service registrations constructor dependencies compose resolve create paths"
  - Read src/Configuration/ServiceCollectionExtensions.cs
  - Read tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs
  - codegraph_codegraph_explore "ProjectLoadOrchestrator ResultsViewModel constructors full direct dependencies; ServiceCollectionExtensions DI registrations IMaterialRepository MaterialRepository IConstructionService ConstructionService IDialogService MessageBoxService exact AddSingleton lifetimes"
  - PowerShell read-only structural assertions across maps/compile-time.md and maps/di-runtime.md
  - node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2
  - node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2
  - node docs/architecture-migration/widget/generate-widget.mjs --check
exit_code: 0
status: pass
raw_output: Current registration and constructor source, selected Codegraph paths, DiRegistrationTests setup/resolution assertions, post-write structural QA output, and Phase 1 lifecycle DI overlay recorded below.
limitations:
  - Registration records constructibility rules, not proof that an application runtime invocation or user flow occurred.
  - DiRegistrationTests directly resolves only the named editor services/ViewModels/views and ConstructionViewModel; it is not an exhaustive runtime-resolvability test of every registration in this map.
  - Constructor selection, factory delegates, WPF composition, lazy materialization, reflection, and unobserved dynamic paths are not fully traced.
  - This is a selected provisional runtime/DI filter, not a complete object graph, lifecycle trace, SCC/cycle proof, or canonical model.
  - Phase 1 added only the lifecycle shell; module state slices remain in their existing owners.
---

# DI/Runtime Research View

## Phase 2 ClimateState runtime overlay

| Node ID | Runtime role | Lifetime / owner | Evidence | Status |
| --- | --- | --- | --- | --- |
| `DRN-P2-CLIMATE-001` | `ProjectSessionClimateState` canonical Climate state slice | owned private instance of singleton `ProjectSession`; not independently registered in DI | `ProjectSession.cs`; `DiRegistrationTests`; `di-guards.md` | verified |
| `DRN-P2-CLIMATE-002` | `IProjectSessionClimateState` access surface | exposed through `IProjectSession.ClimateState`; consumers share the `ProjectSession` instance | `IProjectSession.cs`; `ProjectSession.cs`; `di-guards.md` | verified |
| `DRN-P2-CLIMATE-003` | `ClimateViewModel` adapter | singleton VM observes/routes through canonical state; mirrors snapshot values | `ClimateViewModel.cs`; `climate-viewmodel-adapter.md` | verified |
| `DRN-P2-CLIMATE-004` | `ClimateData` / `IClimateData` projection | singleton compatibility projection updated by canonical completion | `ClimateData.cs`; `climate-data-projection.md`; `downstream-invalidation.md` | verified |
| `DRN-P2-CLIMATE-005` | `CalculationContext` Climate projection seam | singleton downstream compatibility context updated once per changed canonical completion | `ProjectSessionClimateState.cs`; `CalculationContext.cs`; `downstream-invalidation.md` | verified |

| Edge ID | Runtime relation | From | To | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| `DRE-P2-CLIMATE-001` | owns/exposes | `ProjectSession` | `ProjectSessionClimateState` / `IProjectSessionClimateState` | `ProjectSession.cs`; `di-guards.md` | verified |
| `DRE-P2-CLIMATE-002` | adapter consumes | `ClimateViewModel` | `IProjectSession.ClimateState` | `ClimateViewModel.cs`; `climate-viewmodel-adapter.md` | verified |
| `DRE-P2-CLIMATE-003` | non-user restore/reset applies | `ProjectLoadOrchestrator` / `MainViewModel` | `IProjectSession.ClimateState` | `restore-reset-routing.md` | verified |
| `DRE-P2-CLIMATE-004` | compatibility completion | `ProjectSessionClimateState` | `ClimateData.ApplyProjection` then `CalculationContext.UpdateClimate` | `downstream-invalidation.md`; `multiplicity-characterization.md` | verified |
| `DRE-P2-CLIMATE-005` | persistence projection read | `ResultsViewModel` | `IProjectSession.ClimateState.Snapshot` | `persistence-results.md`; `affected-gates.md` | verified |

Task 10 guard evidence confirms no `IProjectSessionClimateState` / `ProjectSessionClimateState`
DI descriptor creates a transient or second owner; consumers observe the same canonical projection chain.

## Filter and Evidence Rules

**View membership:** `di-runtime` only. This receipt admits `IServiceCollection` registration, lifetime, explicit factory delegation, explicit service-provider resolution, and selected composition/create paths. It does not admit a bare `using` directive, a constructor type reference by itself, or a compile-time namespace reference as DI/runtime evidence.

Allowed edge kinds: `di-registration`, `di-factory-resolution`, `constructor-dependency`, `compose-call`, `provider-resolution-test`, `create-path`.

Every edge is source-backed and has one confidence value: `verified` for direct registration/constructor/test source, `derived` for a bounded composition conclusion from them, and `degraded` only for explicit unavailable graph/path properties.

## Provisional Nodes

Stable prefix: `DRN-`. All nodes below are members of `di-runtime`.

| Node ID | Kind | Display name | Source evidence |
| --- | --- | --- | --- |
| `DRN-001` | composition root | `ServiceCollectionExtensions.AddApplicationServices` | `src/Configuration/ServiceCollectionExtensions.cs:195-205` |
| `DRN-002` | composition module | `AddClimateModule` | `src/Configuration/ServiceCollectionExtensions.cs:37-65` |
| `DRN-003` | composition module | `AddThermalModule` | `src/Configuration/ServiceCollectionExtensions.cs:71-81` |
| `DRN-004` | composition module | `AddConstructionModule` | `src/Configuration/ServiceCollectionExtensions.cs:87-110` |
| `DRN-005` | composition module | `AddHydraulicsModule` | `src/Configuration/ServiceCollectionExtensions.cs:116-140` |
| `DRN-006` | composition module | `AddNavigationServices` | `src/Configuration/ServiceCollectionExtensions.cs:146-163` |
| `DRN-007` | composition module | `AddResultsModule` | `src/Configuration/ServiceCollectionExtensions.cs:169-189` |
| `DRN-008` | service interface | `ICalculationStateService` | `src/Configuration/ServiceCollectionExtensions.cs:149` |
| `DRN-009` | implementation | `CalculationStateService` | `src/Configuration/ServiceCollectionExtensions.cs:149` |
| `DRN-010` | service | `CalculationContext` | `src/Configuration/ServiceCollectionExtensions.cs:152` |
| `DRN-011` | service interface | `IProjectStateService` | `src/Configuration/ServiceCollectionExtensions.cs:174` |
| `DRN-012` | implementation | `ProjectStateService` | `src/Configuration/ServiceCollectionExtensions.cs:172-175` |
| `DRN-013` | service interface | `IMarkDirtyService` | `src/Configuration/ServiceCollectionExtensions.cs:175` |
| `DRN-014` | service interface | `IProjectFileService` | `src/Configuration/ServiceCollectionExtensions.cs:180` |
| `DRN-015` | implementation | `ProjectFileService` | `src/Configuration/ServiceCollectionExtensions.cs:180` |
| `DRN-016` | service | `ProjectLoadOrchestrator` | `src/Configuration/ServiceCollectionExtensions.cs:182` |
| `DRN-017` | ViewModel | `ClimateViewModel` | `src/Configuration/ServiceCollectionExtensions.cs:60` |
| `DRN-018` | ViewModel | `ConstructionViewModel` | `src/Configuration/ServiceCollectionExtensions.cs:102` |
| `DRN-019` | ViewModel | `ThermalViewModel` | `src/Configuration/ServiceCollectionExtensions.cs:77` |
| `DRN-020` | ViewModel | `CircuitsViewModel` | `src/Configuration/ServiceCollectionExtensions.cs:135` |
| `DRN-021` | ViewModel | `ResultsViewModel` | `src/Configuration/ServiceCollectionExtensions.cs:187` |
| `DRN-022` | builder | `ResultsPdfDataBuilder` | `src/Configuration/ServiceCollectionExtensions.cs:183` |
| `DRN-023` | builder | `HydraulicSummaryBuilder` | `src/Configuration/ServiceCollectionExtensions.cs:184` |
| `DRN-024` | service interface | `IPdfExportService` | `src/Configuration/ServiceCollectionExtensions.cs:176` |
| `DRN-025` | implementation | `PdfExportService` | `src/Configuration/ServiceCollectionExtensions.cs:176` |
| `DRN-026` | service interface | `ICalculationReportExportService` | `src/Configuration/ServiceCollectionExtensions.cs:179` |
| `DRN-027` | implementation | `CalculationReportExportService` | `src/Configuration/ServiceCollectionExtensions.cs:179` |
| `DRN-028` | ViewModel | `MaterialEditorViewModel` | `src/Configuration/ServiceCollectionExtensions.cs:103` |
| `DRN-029` | ViewModel | `TemplateEditorViewModel` | `src/Configuration/ServiceCollectionExtensions.cs:104` |
| `DRN-030` | service interface | `IEditorDialogService` | `src/Configuration/ServiceCollectionExtensions.cs:158` |
| `DRN-031` | implementation | `EditorDialogService` | `src/Configuration/ServiceCollectionExtensions.cs:158` |
| `DRN-032` | test fixture | `DiRegistrationTests` | `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:18-35` |
| `DRN-033` | provider | `ServiceProvider` | `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:27-30` |
| `DRN-034` | service interface | `IConstructionService` | `src/Configuration/ServiceCollectionExtensions.cs:95` |
| `DRN-035` | implementation | `ConstructionService` | `src/Configuration/ServiceCollectionExtensions.cs:95`; implementation at `src/Services/Construction/ConstructionService.cs:11-31` |
| `DRN-036` | service interface | `IDialogService` | `src/Configuration/ServiceCollectionExtensions.cs:155` |
| `DRN-037` | implementation | `MessageBoxService` | `src/Configuration/ServiceCollectionExtensions.cs:155` |
| `DRN-038` | repository interface | `IMaterialRepository` | `src/Configuration/ServiceCollectionExtensions.cs:90` |
| `DRN-039` | implementation | `MaterialRepository` | `src/Configuration/ServiceCollectionExtensions.cs:90`; constructor at `src/Repositories/Construction/MaterialRepository.cs:31-45` |

## Registrations, Lifetimes, and Runtime/DI Edges

Stable prefix: `DRE-`. Every row has `view_membership=di-runtime`; registration edges record registration only.

| Edge ID | Kind | From | To | Lifetime / path | Source evidence | Confidence | View membership |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `DRE-001` | compose-call | `DRN-001` | `DRN-006` | ordered fluent call | `AddApplicationServices` calls `AddNavigationServices` first at `src/Configuration/ServiceCollectionExtensions.cs:195-199` | verified | di-runtime |
| `DRE-002` | compose-call | `DRN-001` | `DRN-002` | ordered fluent call | `AddApplicationServices` calls `AddClimateModule` at `src/Configuration/ServiceCollectionExtensions.cs:197-200` | verified | di-runtime |
| `DRE-003` | compose-call | `DRN-001` | `DRN-003` | ordered fluent call | `AddApplicationServices` calls `AddThermalModule` at `src/Configuration/ServiceCollectionExtensions.cs:199-201` | verified | di-runtime |
| `DRE-004` | compose-call | `DRN-001` | `DRN-004` | ordered fluent call | `AddApplicationServices` calls `AddConstructionModule` at `src/Configuration/ServiceCollectionExtensions.cs:200-202` | verified | di-runtime |
| `DRE-005` | compose-call | `DRN-001` | `DRN-005` | ordered fluent call | `AddApplicationServices` calls `AddHydraulicsModule` at `src/Configuration/ServiceCollectionExtensions.cs:201-203` | verified | di-runtime |
| `DRE-006` | compose-call | `DRN-001` | `DRN-007` | ordered fluent call | `AddApplicationServices` calls `AddResultsModule` at `src/Configuration/ServiceCollectionExtensions.cs:202-203` | verified | di-runtime |
| `DRE-007` | di-registration | `DRN-008` | `DRN-009` | singleton | `services.AddSingleton<ICalculationStateService, CalculationStateService>()` at `src/Configuration/ServiceCollectionExtensions.cs:149` | verified | di-runtime |
| `DRE-008` | di-registration | `DRN-010` | `DRN-010` | singleton self-registration | `services.AddSingleton<CalculationContext>()` at `src/Configuration/ServiceCollectionExtensions.cs:152` | verified | di-runtime |
| `DRE-009` | di-registration | `DRN-012` | `DRN-012` | singleton self-registration | `services.AddSingleton<ProjectStateService>()` at `src/Configuration/ServiceCollectionExtensions.cs:172` | verified | di-runtime |
| `DRE-010` | di-factory-resolution | `DRN-011` | `DRN-012` | singleton factory resolves same `ProjectStateService` | `services.AddSingleton<IProjectStateService>(sp => sp.GetRequiredService<ProjectStateService>())` at `src/Configuration/ServiceCollectionExtensions.cs:174` | verified | di-runtime |
| `DRE-011` | di-factory-resolution | `DRN-013` | `DRN-012` | singleton factory resolves same `ProjectStateService` | `services.AddSingleton<IMarkDirtyService>(sp => sp.GetRequiredService<ProjectStateService>())` at `src/Configuration/ServiceCollectionExtensions.cs:175` | verified | di-runtime |
| `DRE-012` | di-registration | `DRN-014` | `DRN-015` | singleton | `services.AddSingleton<IProjectFileService, ProjectFileService>()` at `src/Configuration/ServiceCollectionExtensions.cs:180` | verified | di-runtime |
| `DRE-013` | di-registration | `DRN-016` | `DRN-016` | singleton self-registration | `services.AddSingleton<ProjectLoadOrchestrator>()` at `src/Configuration/ServiceCollectionExtensions.cs:182` | verified | di-runtime |
| `DRE-014` | di-registration | `DRN-017` | `DRN-017` | singleton self-registration | `services.AddSingleton<ClimateViewModel>()` at `src/Configuration/ServiceCollectionExtensions.cs:60` | verified | di-runtime |
| `DRE-015` | di-registration | `DRN-018` | `DRN-018` | singleton self-registration | `services.AddSingleton<ConstructionViewModel>()` at `src/Configuration/ServiceCollectionExtensions.cs:102` | verified | di-runtime |
| `DRE-016` | di-registration | `DRN-019` | `DRN-019` | singleton self-registration | `services.AddSingleton<ThermalViewModel>()` at `src/Configuration/ServiceCollectionExtensions.cs:77` | verified | di-runtime |
| `DRE-017` | di-registration | `DRN-020` | `DRN-020` | singleton self-registration | `services.AddSingleton<CircuitsViewModel>()` at `src/Configuration/ServiceCollectionExtensions.cs:135` | verified | di-runtime |
| `DRE-018` | di-registration | `DRN-021` | `DRN-021` | singleton self-registration | `services.AddSingleton<ResultsViewModel>()` at `src/Configuration/ServiceCollectionExtensions.cs:187` | verified | di-runtime |
| `DRE-019` | di-registration | `DRN-022` | `DRN-022` | singleton self-registration | `services.AddSingleton<ResultsPdfDataBuilder>()` at `src/Configuration/ServiceCollectionExtensions.cs:183` | verified | di-runtime |
| `DRE-020` | di-registration | `DRN-023` | `DRN-023` | singleton self-registration | `services.AddSingleton<HydraulicSummaryBuilder>()` at `src/Configuration/ServiceCollectionExtensions.cs:184` | verified | di-runtime |
| `DRE-021` | di-registration | `DRN-024` | `DRN-025` | singleton | `services.AddSingleton<IPdfExportService, PdfExportService>()` at `src/Configuration/ServiceCollectionExtensions.cs:176` | verified | di-runtime |
| `DRE-022` | di-registration | `DRN-026` | `DRN-027` | singleton | `services.AddSingleton<ICalculationReportExportService, CalculationReportExportService>()` at `src/Configuration/ServiceCollectionExtensions.cs:179` | verified | di-runtime |
| `DRE-023` | di-registration | `DRN-028` | `DRN-028` | transient self-registration | `services.AddTransient<MaterialEditorViewModel>()` at `src/Configuration/ServiceCollectionExtensions.cs:103` | verified | di-runtime |
| `DRE-024` | di-registration | `DRN-029` | `DRN-029` | transient self-registration | `services.AddTransient<TemplateEditorViewModel>()` at `src/Configuration/ServiceCollectionExtensions.cs:104` | verified | di-runtime |
| `DRE-025` | di-registration | `DRN-030` | `DRN-031` | singleton | `services.AddSingleton<IEditorDialogService, EditorDialogService>()` at `src/Configuration/ServiceCollectionExtensions.cs:158` | verified | di-runtime |
| `DRE-026` | create-path | `DRN-032` | `DRN-001` | test setup invokes composition root | `services.AddApplicationServices()` at `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:27-29` | verified | di-runtime |
| `DRE-027` | create-path | `DRN-032` | `DRN-033` | `BuildServiceProvider()` in test setup | `_provider = services.BuildServiceProvider()` at `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:27-30` | verified | di-runtime |
| `DRE-028` | provider-resolution-test | `DRN-033` | `DRN-028` | test resolves transient editor VM | `MaterialEditorViewModel_ResolvesFromProvider` calls `GetService<MaterialEditorViewModel>()` and asserts non-null at `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:39-46` | verified | di-runtime |
| `DRE-029` | provider-resolution-test | `DRN-033` | `DRN-029` | test resolves transient editor VM | `TemplateEditorViewModel_ResolvesFromProvider` calls `GetService<TemplateEditorViewModel>()` and asserts non-null at `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:49-56` | verified | di-runtime |
| `DRE-030` | provider-resolution-test | `DRN-033` | `DRN-031` | test resolves interface implementation | `EditorDialogService_ResolvesFromProvider` resolves `IEditorDialogService` and asserts `EditorDialogService` at `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:79-87` | verified | di-runtime |
| `DRE-031` | provider-resolution-test | `DRN-033` | `DRN-018` | test resolves singleton module VM | `ConstructionViewModel_ResolvesFromProvider` resolves `ConstructionViewModel` and asserts non-null at `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:89-97` | verified | di-runtime |
| `DRE-032` | constructor-dependency | `DRN-016` | `DRN-017` | constructor parameter | `ProjectLoadOrchestrator(ClimateViewModel, ConstructionViewModel, ThermalViewModel, CircuitsViewModel, ICalculationStateService, IConstructionService, CalculationContext)` at `src/Services/Project/ProjectLoadOrchestrator.cs:38-53` | verified | di-runtime |
| `DRE-033` | constructor-dependency | `DRN-016` | `DRN-018` | constructor parameter | same constructor at `src/Services/Project/ProjectLoadOrchestrator.cs:38-53` | verified | di-runtime |
| `DRE-034` | constructor-dependency | `DRN-016` | `DRN-019` | constructor parameter | same constructor at `src/Services/Project/ProjectLoadOrchestrator.cs:38-53` | verified | di-runtime |
| `DRE-035` | constructor-dependency | `DRN-016` | `DRN-020` | constructor parameter | same constructor at `src/Services/Project/ProjectLoadOrchestrator.cs:38-53` | verified | di-runtime |
| `DRE-036` | constructor-dependency | `DRN-016` | `DRN-008` | constructor parameter abstraction | same constructor at `src/Services/Project/ProjectLoadOrchestrator.cs:43-53` | verified | di-runtime |
| `DRE-037` | constructor-dependency | `DRN-016` | `DRN-010` | constructor parameter | same constructor at `src/Services/Project/ProjectLoadOrchestrator.cs:45-53` | verified | di-runtime |
| `DRE-038` | constructor-dependency | `DRN-021` | `DRN-011` | constructor parameter abstraction | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:478-511` | verified | di-runtime |
| `DRE-039` | constructor-dependency | `DRN-021` | `DRN-013` | constructor parameter abstraction | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:478-511` | verified | di-runtime |
| `DRE-040` | constructor-dependency | `DRN-021` | `DRN-024` | constructor parameter abstraction | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:481-511` | verified | di-runtime |
| `DRE-041` | constructor-dependency | `DRN-021` | `DRN-026` | constructor parameter abstraction | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:482-511` | verified | di-runtime |
| `DRE-042` | constructor-dependency | `DRN-021` | `DRN-014` | constructor parameter abstraction | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:483-511` | verified | di-runtime |
| `DRE-043` | constructor-dependency | `DRN-021` | `DRN-008` | constructor parameter abstraction | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:485-511` | verified | di-runtime |
| `DRE-044` | constructor-dependency | `DRN-021` | `DRN-017` | concrete module VM parameter | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:488-511` | verified | di-runtime |
| `DRE-045` | constructor-dependency | `DRN-021` | `DRN-018` | concrete module VM parameter | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:489-511` | verified | di-runtime |
| `DRE-046` | constructor-dependency | `DRN-021` | `DRN-019` | concrete module VM parameter | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:490-511` | verified | di-runtime |
| `DRE-047` | constructor-dependency | `DRN-021` | `DRN-020` | concrete module VM parameter | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:491-511` | verified | di-runtime |
| `DRE-048` | constructor-dependency | `DRN-021` | `DRN-016` | concrete orchestrator parameter | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:492-511` | verified | di-runtime |
| `DRE-049` | constructor-dependency | `DRN-021` | `DRN-022` | concrete builder parameter | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:493-511` | verified | di-runtime |
| `DRE-050` | constructor-dependency | `DRN-021` | `DRN-023` | concrete builder parameter | `ResultsViewModel` constructor at `src/ViewModels/Results/ResultsViewModel.cs:494-511` | verified | di-runtime |
| `DRE-051` | di-registration | `DRN-038` | `DRN-039` | singleton | `services.AddSingleton<IMaterialRepository, MaterialRepository>()` at `src/Configuration/ServiceCollectionExtensions.cs:90` | verified | di-runtime |
| `DRE-052` | di-registration | `DRN-034` | `DRN-035` | singleton | `services.AddSingleton<IConstructionService, ConstructionService>()` at `src/Configuration/ServiceCollectionExtensions.cs:95` | verified | di-runtime |
| `DRE-053` | di-registration | `DRN-036` | `DRN-037` | singleton | `services.AddSingleton<IDialogService, MessageBoxService>()` at `src/Configuration/ServiceCollectionExtensions.cs:155` | verified | di-runtime |
| `DRE-054` | constructor-dependency | `DRN-016` | `DRN-034` | constructor parameter abstraction | `ProjectLoadOrchestrator` constructor parameter `IConstructionService` at `src/Services/Project/ProjectLoadOrchestrator.cs:38-53` | verified | di-runtime |
| `DRE-055` | constructor-dependency | `DRN-021` | `DRN-036` | constructor parameter abstraction | `ResultsViewModel` constructor parameter `IDialogService` at `src/ViewModels/Results/ResultsViewModel.cs:478-511` | verified | di-runtime |
| `DRE-056` | constructor-dependency | `DRN-021` | `DRN-038` | constructor parameter abstraction | `ResultsViewModel` constructor parameter `IMaterialRepository` at `src/ViewModels/Results/ResultsViewModel.cs:478-511` | verified | di-runtime |
| `DRE-057` | constructor-dependency | `DRN-021` | `DRN-034` | constructor parameter abstraction | `ResultsViewModel` constructor parameter `IConstructionService` at `src/ViewModels/Results/ResultsViewModel.cs:478-511` | verified | di-runtime |

## Singleton Instance Implications

1. `DRE-007` through `DRE-022`, `DRE-025`, and `DRE-051` through `DRE-053` show selected singleton registrations. Within one `ServiceProvider`, each self/abstraction registration has singleton lifetime according to source registration semantics; the factory edges `DRE-010` and `DRE-011` explicitly obtain the already registered `ProjectStateService` instance. This is a DI lifetime statement, not a claim of process-global singleton behavior.
2. The four module VMs (`DRN-017` through `DRN-020`), `ProjectLoadOrchestrator` (`DRN-016`), and `ResultsViewModel` (`DRN-021`) are registered singleton. Their selected constructor coverage is complete: `ProjectLoadOrchestrator` has 7 direct dependencies at `DRE-032` through `DRE-037` plus `DRE-054`; `ResultsViewModel` has 16 direct dependencies at `DRE-038` through `DRE-050` plus `DRE-055` through `DRE-057`. No edge here claims either singleton was invoked by normal application startup.
3. `MaterialEditorViewModel` and `TemplateEditorViewModel` are transient (`DRE-023`, `DRE-024`); `DiRegistrationTests` explicitly performs provider resolutions (`DRE-028`, `DRE-029`). The tests do not assert identity inequality between repeated resolves, so transient multiplicity is taken from registration declaration rather than a test observation.

## Unresolved Dynamic Paths and Explicit Non-Claims

| Gap ID | Status | Statement |
| --- | --- | --- |
| `DRG-001` | degraded | No selected source/test evidence establishes which application event resolves `ResultsViewModel`, `ProjectLoadOrchestrator`, or every module VM in the normal WPF process. Registration is not invocation. |
| `DRG-002` | degraded | Constructor activation behavior, order, and all nested dependency resolutions are not exhaustively observed; the map lists direct selected constructor dependencies only. |
| `DRG-003` | degraded | Factory delegates shown for `IProjectStateService` and `IMarkDirtyService` establish service-provider resolution of `ProjectStateService`, but do not trace consumers or runtime calls. |
| `DRG-004` | degraded | `DiRegistrationTests` does not resolve every listed service and does not establish a complete runtime object graph, lifecycle disposal behavior beyond its test fixture, or user interaction. |
| `DRG-005` | degraded | Dynamic dispatch, WPF lazy view materialization, reflection, direct `new`, static service locators, and paths outside the selected source are not modeled. |

## Cross-Check With Constructor Evidence

The registration claims used for selected results/orchestrator dependencies are cross-checked with current constructors: `ProjectLoadOrchestrator` has 7 parameters at `src/Services/Project/ProjectLoadOrchestrator.cs:38-53`, represented by `DRE-032` through `DRE-037` and `DRE-054`; `ResultsViewModel` has 16 parameters at `src/ViewModels/Results/ResultsViewModel.cs:478-511`, represented by `DRE-038` through `DRE-050` and `DRE-055` through `DRE-057`. Registration/lifetime rows include the four module VMs (`DRE-014` through `DRE-017`), `ICalculationStateService`/`CalculationContext` (`DRE-007`, `DRE-008`), the corrected `IConstructionService` mapping (`DRE-052`), `IDialogService` mapping (`DRE-053`), and `IMaterialRepository` mapping (`DRE-051`). This cross-check proves source-level DI constructibility evidence only, subject to the explicit unresolved paths above.

## Executable Structural QA

The following PowerShell 5.1 script was run read-only after both files were written. It checks cross-file unique provisional IDs, declared endpoint resolution, required receipt/section/column markers, allowed edge kinds, membership, and the required synthetic bare-`using` policy. It does not modify a repository file.

```powershell
$root = 'D:/IA/ace v.2'
$compilePath = "$root/docs/architecture-migration/maps/compile-time.md"
$runtimePath = "$root/docs/architecture-migration/maps/di-runtime.md"
$compile = Get-Content -Raw -LiteralPath $compilePath
$runtime = Get-Content -Raw -LiteralPath $runtimePath
$all = "$compile`n$runtime"
$nodeIds = @([regex]::Matches($all, '(?m)^\| `(CTN|DRN)-\d{3}` \|') | ForEach-Object { ([regex]::Match($_.Value, '(CTN|DRN)-\d{3}')).Value })
$edgeIds = @([regex]::Matches($all, '(?m)^\| `(CTE|DRE)-\d{3}` \|') | ForEach-Object { ([regex]::Match($_.Value, '(CTE|DRE)-\d{3}')).Value })
if (($nodeIds | Select-Object -Unique).Count -ne $nodeIds.Count) { throw 'duplicate provisional node ID' }
if (($edgeIds | Select-Object -Unique).Count -ne $edgeIds.Count) { throw 'duplicate provisional edge ID' }
foreach ($line in ($all -split "`n" | Where-Object { $_ -match '^\| `(CTE|DRE)-\d{3}` \|' })) {
  $endpoints = @([regex]::Matches($line, '`((?:CTN|DRN)-\d{3})`') | ForEach-Object { $_.Groups[1].Value })
  if ($endpoints.Count -ne 2) { throw "edge endpoint count invalid: $line" }
  foreach ($endpoint in $endpoints) { if ($nodeIds -notcontains $endpoint) { throw "orphan endpoint: $endpoint" } }
}
$requiredYaml = 'phase','snapshot_sha','source_basis','generated_at_utc','working_directory','commands','exit_code','status','raw_output','limitations'
foreach ($field in $requiredYaml) { foreach ($text in @($compile,$runtime)) { if ($text -notmatch "(?m)^${field}:") { throw "missing YAML field: $field" } } }
$requiredMarkers = '## Filter and Evidence Rules','## Provisional Nodes','## Typed Edges','## Explicit Incomplete-Graph Boundary','## Registrations, Lifetimes, and Runtime/DI Edges','## Unresolved Dynamic Paths and Explicit Non-Claims','## Executable Structural QA'
foreach ($marker in $requiredMarkers) { if ($all -notmatch [regex]::Escape($marker)) { throw "missing section: $marker" } }
$allowedCompile = 'solution-project-reference','project-reference','namespace-declaration','constructor-type-reference','used-type-reference','inheritance-reference','using-reference'
$allowedRuntime = 'di-registration','di-factory-resolution','constructor-dependency','compose-call','provider-resolution-test','create-path'
foreach ($line in ($compile -split "`n" | Where-Object { $_ -match '^\| `CTE-\d{3}` \|' })) { $kind = (($line -split '\|')[2]).Trim(); if ($allowedCompile -notcontains $kind) { throw "invalid compile-time edge kind: $kind" }; if ($line -notmatch '\| (verified|derived|degraded) \| compile-time \|$') { throw "invalid compile membership/confidence: $line" } }
foreach ($line in ($runtime -split "`n" | Where-Object { $_ -match '^\| `DRE-\d{3}` \|' })) { $kind = (($line -split '\|')[2]).Trim(); if ($allowedRuntime -notcontains $kind) { throw "invalid DI/runtime edge kind: $kind" }; if ($line -notmatch '\| (verified|derived|degraded) \| di-runtime \|$') { throw "invalid DI/runtime membership/confidence: $line" } }
$orchestratorDependencies = @($runtime -split "`n" | Where-Object { $_ -match '^\| `DRE-\d{3}` \| constructor-dependency \| `DRN-016` \|' })
$resultsDependencies = @($runtime -split "`n" | Where-Object { $_ -match '^\| `DRE-\d{3}` \| constructor-dependency \| `DRN-021` \|' })
if ($orchestratorDependencies.Count -ne 7) { throw "ProjectLoadOrchestrator constructor coverage is $($orchestratorDependencies.Count), expected 7" }
if ($resultsDependencies.Count -ne 16) { throw "ResultsViewModel constructor coverage is $($resultsDependencies.Count), expected 16" }
foreach ($registration in 'DRE-051','DRE-052','DRE-053') { if ($runtime -notmatch ('(?m)^\| `' + $registration + '` \| di-registration \|')) { throw "missing required registration: $registration" } }
$syntheticUsing = 'using SnowMeltingCalculator.ViewModels.Hydraulics;'
$compileEvidenceAllowed = $syntheticUsing -match '^using\s+[A-Za-z0-9_.]+;$'
$runtimeEvidenceAllowed = $syntheticUsing -match 'Add(Singleton|Transient|Scoped)|GetRequiredService|GetService|BuildServiceProvider|new\s+[A-Za-z_]'
if (-not $compileEvidenceAllowed) { throw 'synthetic bare using was not accepted for compile-time evidence' }
if ($runtimeEvidenceAllowed) { throw 'synthetic bare using was incorrectly accepted for DI/runtime evidence' }
[pscustomobject]@{ node_ids=$nodeIds.Count; edge_ids=$edgeIds.Count; compile_nodes=@($nodeIds | Where-Object { $_ -like 'CTN-*' }).Count; runtime_nodes=@($nodeIds | Where-Object { $_ -like 'DRN-*' }).Count; compile_edges=@($edgeIds | Where-Object { $_ -like 'CTE-*' }).Count; runtime_edges=@($edgeIds | Where-Object { $_ -like 'DRE-*' }).Count; registration_edges=@($runtime -split "`n" | Where-Object { $_ -match '^\| `DRE-\d{3}` \| (di-registration|di-factory-resolution) \|' }).Count; project_load_orchestrator_constructor_dependencies=$orchestratorDependencies.Count; results_view_model_constructor_dependencies=$resultsDependencies.Count; required_registrations='DRE-051,DRE-052,DRE-053 present'; endpoints='resolved'; required_fields='present'; edge_kinds='allowed'; synthetic_bare_using_compile_time='accepted'; synthetic_bare_using_di_runtime='rejected'; result='pass' } | Format-List
```

Observed output:

```text
node_ids                       : 70
edge_ids                       : 90
compile_nodes                  : 31
runtime_nodes                  : 39
compile_edges                  : 33
runtime_edges                  : 57
registration_edges             : 22
project_load_orchestrator_constructor_dependencies: 7
results_view_model_constructor_dependencies        : 16
required_registrations          : DRE-051,DRE-052,DRE-053 present
endpoints                      : resolved
required_fields                : present
edge_kinds                     : allowed
synthetic_bare_using_compile_time: accepted
synthetic_bare_using_di_runtime  : rejected
result                         : pass
```

The QA record is a structural validation of these two provisional receipts. It does not claim build/test execution, full graph completeness, or runtime/user-flow behavior.

## Phase 1 ProjectSession lifecycle shell overlay

Added after the `phase-1-project-session-shell` implementation. The six core
module slices remain untouched; only the project lifecycle/identity/dirty/restore
guard ownership moved to a new canonical aggregate.

### Phase 1 nodes

| Node ID | Kind | Display name | Source evidence |
| --- | --- | --- | --- |
| `DRN-PS` | implementation | `ProjectSession` | `src/Services/Project/ProjectSession.cs` |
| `DRN-IPS` | interface | `IProjectSession` | `src/Services/Project/IProjectSession.cs` |

### Phase 1 edges

| Edge ID | Kind | From | To | Lifetime / path | Source evidence | Confidence |
| --- | --- | --- | --- | --- | --- | --- |
| `DRE-P1-001` | di-registration | `DRN-006` | `DRN-IPS` | singleton | `services.AddSingleton<IProjectSession, ProjectSession>()` in `AddNavigationServices` | verified |
| `DRE-P1-002` | di-registration | `DRN-006` | `DRN-PS` | singleton implementation | same registration | verified |
| `DRE-P1-003` | di-resolution | `DRN-011` | `DRN-PS` | singleton factory alias | `services.AddSingleton<IProjectStateService>(sp => sp.GetRequiredService<IProjectSession>())` | verified |
| `DRE-P1-004` | di-resolution | `DRN-013` | `DRN-PS` | singleton factory alias | `services.AddSingleton<IMarkDirtyService>(sp => sp.GetRequiredService<IProjectSession>())` | verified |
| `DRE-P1-005` | di-resolution | `DRN-008` | `DRN-PS` | read-through compatibility | `CalculationStateService` holds `IProjectSession` lease and delegates `IsLoadProjectInProgress` | verified |
| `DRE-P1-006` | state-write | `DRN-021` | `DRN-PS` | lifecycle mutation | `ResultsViewModel` writes `ProjectNumber`, `ProjectObject`, `CurrentFilePath`, `IsDirty` via `IProjectSession` | verified |
| `DRE-P1-007` | state-read | `DRN-012` | `DRN-PS` | forwarding adapter | `ProjectStateService` forwards all reads to `IProjectSession` | verified |
| `DRE-P1-008` | state-read | `DRN-009` | `DRN-PS` | compatibility guard read | `CalculationStateService` delegates `IsLoadProjectInProgress` to `IProjectSession` | verified |
| `DRE-P1-009` | event-publish | `DRN-PS` | `DRN-021` | `PropertyChanged` | `ResultsViewModel` subscribes to `ProjectSession.PropertyChanged` | verified |

### Phase 1 DI verification

- `DependencyInjection_LifecycleConsumersShareCanonicalSession` proves that
  `IProjectSession`, `IProjectStateService`, `IMarkDirtyService`,
  `ICalculationStateService`, `ResultsViewModel`, and `ProjectStateService`
  resolve to a single canonical `ProjectSession` instance.
- `ProjectSessionLegacyStoreGuardTests` proves that `ProjectStateService` and
  `CalculationStateService` contain no mutable lifecycle backing fields.
- See `docs/architecture-migration/evidence/phase-1-project-session-shell/di-runtime.md`
  and `final-gates.md` for full test output.
