---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T17:04:53.5429420Z
working_directory: D:/IA/ace v.2
commands:
  - codegraph_codegraph_explore "ProjectLoadOrchestrator ProjectFileService ProjectData ResultsViewModel load restore save backup .smc persistence user-visible results flow. Return constructor dependencies, ResetModules/restore/save source, files symbols and call paths."
  - codegraph_codegraph_explore "ServiceCollectionExtensions CalculationContext CalculationStateService ProjectStateService DI registrations lifetimes reactive events dirty state recalculation. Return exact registration lifetimes, SetPipeSpacing source and events, and source files/symbols/call paths."
  - codegraph_codegraph_explore "MainViewModel navigation commands ResultsViewModel export PDF calculation report summary open new reset save navigation entry points. Trace MainViewModel.NewCalculation PerformNewCalculationReset CalculationContext.Reset and show source files symbols call paths."
  - codegraph_codegraph_explore "src/Configuration/ServiceCollectionExtensions.cs RegisterServices registration lines for CalculationContext CalculationStateService ProjectStateService ClimateViewModel ConstructionViewModel ThermalViewModel CircuitsViewModel ProjectFileService ProjectLoadOrchestrator ResultsPdfDataBuilder HydraulicSummaryBuilder ResultsViewModel IPdfExportService ICalculationReportExportService. Return exact AddSingleton/AddScoped/AddTransient source."
  - codegraph_codegraph_explore "ResultsViewModel summary PDF export calculation report export commands SaveProject OpenProject LoadProject. Return command method symbols, direct service calls, source line locations, and call paths."
  - Get-Date -AsUTC -Format o
  - PowerShell structural receipt assertions (read-only)
exit_code: 0
status: degraded
raw_output: Inline query-provenance, coverage, representative-edge, and structural-QA sections. Codegraph returned current verbatim source, blast-radius information, and selected call paths.
limitations:
  - No Codegraph response displayed an index-staleness or auto-sync-disabled banner; no targeted source read was required to cure staleness.
  - Codegraph did not expose a complete repository-wide directed graph, SCC computation, or cycle proof. Graph completeness is therefore degraded; this receipt makes no repository-wide completeness claim.
  - Returned source is selected query coverage, not proof that all writers, subscribers, persistence paths, navigation routes, or runtime invocations were found.
  - Compile-time references, DI registrations, and dynamic interface-to-implementation dispatch are recorded as different edge kinds; a type reference or registration alone is not treated as proof of a user-triggered runtime invocation.
---

# Codegraph Baseline: Source-Evidence Coverage

## Binding and Interpretation

This is a point-in-time receipt for the current working tree rooted at `D:/IA/ace v.2`, bound to snapshot SHA `f0d19c34ac03075d64548f1059e9c6626d3596b5`. It does not reuse historical audit claims or historical metrics as current evidence.

Evidence status meanings:

| Status | Meaning |
| --- | --- |
| `verified` | Current verbatim source returned by the listed Codegraph query directly supports the statement. |
| `derived` | The statement combines directly returned current source facts without asserting an unobserved runtime path or repository-wide set. |
| `degraded` | The queried surface could not prove the requested completeness/property; the gap is explicit. |

## Exact Codegraph Query Provenance

| ID | Exact query text | Banner | Returned current files/symbols/call paths | Methodology | Confidence | Limitations |
| --- | --- | --- | --- | --- | --- | --- |
| CG-01 | `ProjectLoadOrchestrator ProjectFileService ProjectData ResultsViewModel load restore save backup .smc persistence user-visible results flow. Return constructor dependencies, ResetModules/restore/save source, files symbols and call paths.` | none | `ProjectLoadOrchestrator`, `ProjectFileService`, `IProjectFileService`, `ProjectData`, `ResultsViewModel`; `ResetModules` blast-radius caller in `ResultsViewModel` | `codegraph` | high | Output selected source and a reset caller, not a complete persistence/user-flow graph. |
| CG-02 | `ServiceCollectionExtensions CalculationContext CalculationStateService ProjectStateService DI registrations lifetimes reactive events dirty state recalculation. Return exact registration lifetimes, SetPipeSpacing source and events, and source files/symbols/call paths.` | none | `CalculationStateService.SetPipeSpacing`, `ICalculationStateService`, `ProjectStateService`, `CalculationContext`, `CircuitsViewModel`; dynamic `ICalculationStateService.SetPipeSpacing -> CalculationStateService.SetPipeSpacing` at `src/Services/Navigation/CalculationStateService.cs:120` | `codegraph` | high | The returned ServiceCollection source was incomplete in this response; CG-04 supplied exact registration lines. |
| CG-03 | `MainViewModel navigation commands ResultsViewModel export PDF calculation report summary open new reset save navigation entry points. Trace MainViewModel.NewCalculation PerformNewCalculationReset CalculationContext.Reset and show source files symbols call paths.` | none | Call path `MainViewModel.NewCalculation` -> `MainViewModel.PerformNewCalculationReset` -> `CalculationContext.Reset`; `ResultsViewModel`; `CalculationReportExportService.ExportReportAsync` with interface-to-implementation dispatch | `codegraph` | high | A selected new-calculation path is not coverage of every navigation command or export command. |
| CG-04 | `src/Configuration/ServiceCollectionExtensions.cs RegisterServices registration lines for CalculationContext CalculationStateService ProjectStateService ClimateViewModel ConstructionViewModel ThermalViewModel CircuitsViewModel ProjectFileService ProjectLoadOrchestrator ResultsPdfDataBuilder HydraulicSummaryBuilder ResultsViewModel IPdfExportService ICalculationReportExportService. Return exact AddSingleton/AddScoped/AddTransient source.` | none | `ServiceCollectionExtensions` registrations; constructors for `CircuitsViewModel`, `ConstructionViewModel`, `ClimateViewModel`, `ResultsViewModel`; `ResultsPdfDataBuilder` | `codegraph` plus `targeted-read` | high | Codegraph did not print the registration body despite the named file; targeted read of the exact file supplied lifetime lines, not a staleness fallback. |
| CG-05 | `ResultsViewModel summary PDF export calculation report export commands SaveProject OpenProject LoadProject. Return command method symbols, direct service calls, source line locations, and call paths.` | none | `ResultsViewModel` constructor services, `IProjectFileService`, `IProjectStateService`, `IPdfExportService`, `ICalculationReportExportService`; dynamic `ExportReportAsync` interface -> implementation at `src/Services/Reports/Calculation/CalculationReportExportService.cs:25` | `codegraph` | medium | Query returned selected constructor/export-service evidence, but not every requested command body; coverage is correspondingly derived/degraded below. |

## Current Source-Evidence Coverage Matrix

| Required architecture area | Methodology | Status | Current source evidence | Confidence |
| --- | --- | --- | --- | --- |
| `ProjectLoadOrchestrator` | `codegraph` | `verified` | Constructor directly takes `ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`, `CircuitsViewModel`, `ICalculationStateService`, `IConstructionService`, and `CalculationContext` at `src/Services/Project/ProjectLoadOrchestrator.cs:38-53`. `ResetModules` resets context and all four module VMs at `:60-67`. | high |
| `ResultsViewModel` | `codegraph` | `verified` | Constructor directly takes four module VMs and `IPdfExportService`, `ICalculationReportExportService`, `IProjectFileService`, and `ProjectLoadOrchestrator` at `src/ViewModels/Results/ResultsViewModel.cs:478-511`. | high |
| `CalculationContext` | `codegraph` | `verified` | `Reset` nulls climate, construction, thermal result, and hydraulics results, then publishes `ContextChanged` through `OnContextChanged` at `src/Core/CalculationContext.cs:222-230`. | high |
| `CalculationStateService` | `codegraph` | `verified` | `SetPipeSpacing` permits `ThermalViewModel` and `ProjectLoadOrchestrator.RestoreModules` only while `IsLoadProjectInProgress`; it publishes `PipeSpacingChanged` on value change at `src/Services/Navigation/CalculationStateService.cs:120-139`. `OnStateChanged` publishes `StateChanged` at `:160-168`. | high |
| `ProjectStateService` | `codegraph` | `verified` | `MarkDirty`/`MarkClean` change `IsDirty` and publish `PropertyChanged`; `CurrentFilePath` also publishes it at `src/Services/Results/ProjectStateService.cs:27-85`. | high |
| `ProjectData` | `codegraph` | `verified` | Serializable project model declares `Version`, project identity fields, climate/construction/thermal/hydraulics data, custom materials/templates, and operating mode at `src/Models/Project/ProjectData.cs:11-70`. | high |
| DI registrations | `codegraph|targeted-read` | `verified` | `AddNavigationServices` registers `ICalculationStateService -> CalculationStateService`, `CalculationContext`, and `MainViewModel` as singleton at `src/Configuration/ServiceCollectionExtensions.cs:146-163`; `AddResultsModule` registers `ProjectStateService`, all its exposed interfaces, `IPdfExportService`, calculation-report services, `IProjectFileService`, `ProjectLoadOrchestrator`, both results builders, and `ResultsViewModel` as singleton at `:169-188`; module VMs are singleton at `:59-63`, `:76-78`, `:101-104`, `:134-139`. | high |
| Reactive handlers | `codegraph` | `verified` | `CircuitsViewModel` subscribes to `StateChanged`, `PipeSpacingChanged`, and `CalculationContext.ContextChanged` at `src/ViewModels/Hydraulics/CircuitsViewModel.cs:721-730`; its context handler reacts to thermal/climate events at `:1062-1088`. | high |
| Persistence flows | `codegraph` | `verified` | `ProjectFileService` serializes/deserializes JSON `ProjectData`, enforces `.smc`, writes a same-volume `.tmp`, copies `.bak`, moves temp to target, and exposes result-based save/load APIs at `src/Services/Project/ProjectFileService.cs:40-190`. | high |
| Navigation entry points | `codegraph` | `verified` | `MainViewModel.NewCalculation` is a relay command that calls `PerformNewCalculationReset`; the observed path reaches `CalculationContext.Reset` at `src/ViewModels/Shell/MainViewModel.cs:177-225` and `src/Core/CalculationContext.cs:222-230`. | high |
| Summary/PDF/calculation-report export entry points | `codegraph` | `derived` | `ResultsViewModel` directly receives `IPdfExportService`, `ICalculationReportExportService`, `ResultsPdfDataBuilder`, and `HydraulicSummaryBuilder` at `src/ViewModels/Results/ResultsViewModel.cs:478-511`; `CalculationReportExportService.ExportReportAsync` builds, renders, and writes Markdown at `src/Services/Reports/Calculation/CalculationReportExportService.cs:25-77`; `ResultsPdfDataBuilder.Build` refreshes results and builds PDF data at `src/Services/Results/ResultsPdfDataBuilder.cs:41-205`. | medium |
| Graph completeness / SCCs / cycles | `codegraph` | `degraded` | Current queries returned verbatim source, selected dynamic dispatch, blast radius, and selected call paths. They did not return a complete directed graph, SCCs, or cycle results. | high for the limitation |

## Required Six Views: Representative Typed Edges

Each row is one representative current edge, not a collapsed graph and not a proof that it is the only edge in its view.

| View | Edge kind | From | To | Current source evidence | Status | Confidence |
| --- | --- | --- | --- | --- | --- | --- |
| Compile-time | constructor dependency | `ProjectLoadOrchestrator` | `ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`, `CircuitsViewModel`, `ICalculationStateService`, `IConstructionService`, `CalculationContext` | `src/Services/Project/ProjectLoadOrchestrator.cs:38-53` | `verified` | high |
| DI/runtime | DI singleton registration | `IProjectFileService` | `ProjectFileService` | `services.AddSingleton<IProjectFileService, ProjectFileService>()` at `src/Configuration/ServiceCollectionExtensions.cs:180`; this is a registration/resolution edge, not proof of a user action. | `verified` | high |
| State ownership | state mutation and notification | `ProjectStateService.MarkDirty/MarkClean` | `ProjectStateService.IsDirty` and `PropertyChanged` observers | `src/Services/Results/ProjectStateService.cs:45-85` | `verified` | high |
| Reactive | event subscription | `CalculationContext.ContextChanged` | `CircuitsViewModel.OnCalculationContextChanged` | Subscription at `src/ViewModels/Hydraulics/CircuitsViewModel.cs:728-730`; selected reactions at `:1062-1088`. | `verified` | high |
| Persistence | filesystem save/load | `ProjectFileService.SaveProjectResultAsync/LoadProjectResultAsync` | JSON `.smc` `ProjectData` file | `.smc` normalization, JSON serialization, `.tmp` write, `.bak` copy, `File.Move`, and result-based load/save at `src/Services/Project/ProjectFileService.cs:115-190`. | `verified` | high |
| User flow | observed call path | `MainViewModel.NewCalculation` | `PerformNewCalculationReset` -> `CalculationContext.Reset` | Codegraph call path and source at `src/ViewModels/Shell/MainViewModel.cs:177-225`, `src/Core/CalculationContext.cs:222-230`; reset also calls `ResultsViewModel.Reset`, four module `Reset`s, and `MarkClean`. | `verified` | high |

## Direct Findings Preserved From Current Source

1. `ProjectLoadOrchestrator` directly injects the four concrete module ViewModels plus `ICalculationStateService`, `IConstructionService`, and `CalculationContext`; its `ResetModules` invokes `CalculationContext.Reset` and all four module reset methods. Status: `verified`, confidence: high.
2. `ServiceCollectionExtensions` uses `AddSingleton` for `ICalculationStateService -> CalculationStateService`, `CalculationContext`, `ProjectStateService` and its exposed interfaces, `IProjectFileService -> ProjectFileService`, `ProjectLoadOrchestrator`, `ResultsPdfDataBuilder`, `HydraulicSummaryBuilder`, `ResultsViewModel`, `IPdfExportService`, calculation-report services, and all four module VMs. `AddTransient` applies to editor/child VMs and validators, not to the listed baseline services. Status: `verified`, confidence: high.
3. `CalculationStateService.SetPipeSpacing` accepts the canonical `ThermalViewModel` source or guarded `ProjectLoadOrchestrator.RestoreModules` while loading; a changed value raises `PipeSpacingChanged`, while `OnStateChanged` raises `StateChanged`. Status: `verified`, confidence: high.
4. `ProjectFileService` reads/writes JSON `.smc` `ProjectData`, writes to a `.tmp` path, copies `.bak` before move, moves temp to target, and provides result-based APIs. Status: `verified`, confidence: high.
5. The observed Codegraph call path is `MainViewModel.NewCalculation` -> `PerformNewCalculationReset` -> `CalculationContext.Reset`; reset also touches `ResultsViewModel`, all four module VMs, and clean state. Status: `verified`, confidence: high.
6. `ResultsViewModel` directly depends on four module ViewModels and export/file/load services. Status: `verified`, confidence: high.

## Structural QA

Read-only PowerShell inspected this receipt after creation.

| Assertion | Result |
| --- | --- |
| Common YAML fields `phase`, `snapshot_sha`, `source_basis`, `generated_at_utc`, `working_directory`, `commands`, `exit_code`, `status`, `raw_output`, and `limitations` are present | pass |
| Snapshot SHA, `working-tree` source basis, root, and UTC timestamp are recorded | pass |
| Every required architecture area has a matrix row with methodology, status, current evidence, and confidence | pass |
| Six distinct view rows are present: compile-time, DI/runtime, state ownership, reactive, persistence, and user flow | pass |
| Every current finding has source evidence and confidence | pass |
| Every exact Codegraph query declared in YAML appears in the provenance table | pass |
| No stale/disabled Codegraph banner appeared; the only targeted read is explicitly documented as an exact DI-line completion, not a stale-index cure | pass |
| Graph completeness is explicitly `degraded`; no SCC/cycle or repository-wide completeness claim is made | pass |

## Limitations and Safe Reuse

- Treat these rows as a baseline sampling receipt for later maps and characterization work, not as a complete architecture model.
- The selected `ResultsViewModel` query did not return every save/open/export command body. Those entry points are only represented where direct source or the observed dynamic dispatch supports them.
- `ProjectLoadOrchestrator` has concrete ViewModel constructor dependencies. This records current compile-time coupling and its DI constructibility; it does not establish a future migration decision.
- Later evidence must use current source or a fresh query/read if the working tree changes; this receipt remains bound to its timestamp and snapshot SHA.
