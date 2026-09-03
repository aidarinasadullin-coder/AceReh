# Slice 1 — Reactive baseline and full production subscription census

Phase 10 (`phase-10-reactive-ownership-multiplicity-closure`), frozen plan
`docs/architecture-migration/plans/phase-10-reactive-ownership-multiplicity-closure.md`
(SHA-256 `D8F893B20AA468D10ED42C275A3FC1D951A3354409E37CDF06B3412F411135B7`, 41832 bytes,
byte-identical to the owner-approved candidate; re-verified 2026-09-03 via
`Get-FileHash -Algorithm SHA256` before execution).

Baseline: tracked worktree clean at phase-9 dossier state; only this phase's
plan/evidence files, the appended dossier, and protected unrelated
`docs/workspace/*` presentation files are dirty. Delta-relative discipline
applies.

## (c) Pre-measurement counter column state (from `maps/reactive.md`)

Every `RE-` row currently carries `unknown` in all five runtime counter columns
(ContextChanged count, StateChanged count, Calculator invocation count, Results
projection update count, Dirty transition count), except:

- `RE-003`: annotated contract counts (1 projection event per changed mutation,
  1 `CalculationContext.Climate` publication, 1 Circuits recalculation path,
  user origin marks dirty / lifecycle origins do not) — receipt-backed but not
  cycle-counted.
- `RE-009`: annotated contract counts (at most 1 valid user/template
  publication, 1 canonical `Changed`, 1 Thermal invalidation after correction,
  1 dirty for changed User/Template, 0 for lifecycle/no-op/rejected).
- Overlays `RE-P4-*`/`RE-P5-HYD-*`: multiplicity described as receipt-backed
  characterization facts (41 + 13 executed cases, duplicate-attach guards),
  still without per-cycle runtime counters.

These columns are the pre-measurement baseline; Slices 2–4 replace them with
measured, provenance-linked facts.

## (a)+(b) Production subscription census — every site with owner, lifetime, unsubscribe rule, multiplicity expectation

Grounding method: `grep -rn "+=" src --include="*.cs"` (excluding `obj/`, `bin/`)
plus read-back of every hit. All plan-time anchors were re-grounded against the
live post-Phase-9 tree; drifted anchors are corrected in place below.

Legend — lifetime classes: **APP** = DI singleton / application-lifetime owner,
no teardown path by design; **PER-ITEM** = subscription tracks a replaceable
item with explicit detach; **WINDOW** = single main window, lives as long as
the application.

### Canonical domain surfaces (map edges `RE-001..RE-014` + overlays)

| # | Site (live anchor) | Publisher → handler | Owner / lifetime | Unsubscribe rule | Multiplicity expectation | Edge mapping |
|---|---|---|---|---|---|---|
| 1 | `src/Services/Project/HydraulicsStateCoordinator.cs:31` | `CalculationContext.ContextChanged` → `OnContextChanged` | HydraulicsStateCoordinator, APP | none by design (singleton never recreated; `Connect` delegates are not event subscriptions) | exactly 1 handler for application lifetime | `RE-001`, `RE-002`, `RE-P5-HYD-001` |
| 2 | `src/Services/Project/HydraulicsStateCoordinator.cs:32` | `ICalculationStateService.PipeSpacingChanged` → `OnPipeSpacingChanged` | HydraulicsStateCoordinator, APP | none by design | exactly 1 | `RE-006`, `RE-P5-HYD-001` |
| 3 | `src/Services/Project/HydraulicsStateCoordinator.cs:33` | `ICalculationStateService.StateChanged` → `OnStateChanged` (intentionally empty body) | HydraulicsStateCoordinator, APP | none by design | exactly 1 | `RE-004` (hydraulics phase notifications), `RE-P5-HYD-001` |
| 4 | `src/Services/Navigation/CalculationStateService.cs:55` | `ProjectSession.ThermalState.Changed` → `OnThermalStateChanged` (stored `_thermalChangedHandler`) | CalculationStateService, APP | none by design (singleton wraps the singleton session) | exactly 1 | `RE-P4-004`, `RE-005` (translation source) |
| 5 | `src/Services/Navigation/CalculationStateService.cs:57` | `ProjectSession.HydraulicsState.Changed` → `OnHydraulicsStateChanged` (stored `_hydraulicsChangedHandler`) | CalculationStateService, APP | none by design | exactly 1 | `RE-P4-004` (hydraulics analog), `RE-004` |
| 6 | `src/Services/Project/ThermalStateCoordinator.cs:89` | `ClimateData.DataChanged` → `OnClimateUpstream` (stored `_climateUpstreamHandler`; attach guarded to `ClimateData` impl) | ThermalStateCoordinator, APP (`IDisposable`) | explicit `Dispose()` unsubscribe (`:254`); duplicate attach rejected by guard tests | exactly 1 | `RE-P4-001` |
| 7 | `src/Services/Project/ThermalStateCoordinator.cs:92` | `IConstructionData.DataChanged` → `OnConstructionUpstream` (stored `_constructionUpstreamHandler`) | ThermalStateCoordinator, APP (`IDisposable`) | explicit `Dispose()` (`:257`); duplicate attach rejected | exactly 1 | `RE-P4-001` |
| 8 | `src/ViewModels/Climate/ClimateViewModel.cs:242` | `ProjectSessionClimateState.Changed` → `OnClimateStateChanged` | ClimateViewModel, APP | none by design | exactly 1 | `RE-003` (adapter mirror portion) |
| 9 | `src/ViewModels/Construction/ConstructionViewModel.cs:261` | `ProjectSessionConstructionState.Changed` → `OnConstructionStateChanged` | ConstructionViewModel, APP | none by design | exactly 1 | `RE-009` |
| 10 | `src/ViewModels/Hydraulics/CircuitsViewModel.cs:915` | `ProjectSessionHydraulicsState.Changed` → `OnHydraulicsStateChanged` (ProjectLoad-origin mirror only) | CircuitsViewModel, APP | none by design | exactly 1 | `RE-P5-HYD-004` |
| 11 | `src/ViewModels/Hydraulics/CircuitsViewModel.cs:919` | `Collectors.CollectionChanged` → `OnCollectorsCollectionChanged` (VM-owned collection) | CircuitsViewModel, APP | none by design | exactly 1 | `RE-008` |
| 12 | `src/ViewModels/Hydraulics/CircuitsViewModel.cs:1036` attach / `:1056` detach | per-collector `CollectorData.PropertyChanged` → `OnCollectorPropertyChanged` | CircuitsViewModel, PER-ITEM (`_subscribedCollectors` set; `AttachCircuitEvents`/`DetachCircuitEvents`) | detach on Remove/Replace; set guard prevents double attach | exactly 1 per live collector, 0 per removed collector | `RE-008` |
| 13 | `src/ViewModels/Hydraulics/CircuitsViewModel.cs:1037` attach / `:1057` detach | per-collector `Circuits.CollectionChanged` → `OnCircuitsCollectionChanged` | CircuitsViewModel, PER-ITEM (same guard) | detach on Remove/Replace | exactly 1 per live collector | `RE-008` |
| 14 | `src/ViewModels/Hydraulics/CircuitsViewModel.cs:1040`+`:1075` attach / `:1060`+`:1083` detach | per-circuit `CircuitRow.PropertyChanged` → `OnCircuitPropertyChanged` | CircuitsViewModel, PER-ITEM | detach on collector detach and on circuit Remove | exactly 1 per live circuit | `RE-008` |
| 15 | `src/ViewModels/Hydraulics/CircuitsViewModel.cs:1341` attach / `:1305` old-instance detach | `HydraulicInputData.PropertyChanged` → forwarding handler (`_inputDataPropertyChangedHandler`) | CircuitsViewModel, PER-INSTANCE (atomic replace in `SetInputData`) | explicit old-instance unsubscribe before each replace | exactly 1 (current `InputData` only) | `RE-008` |
| 16 | `src/ViewModels/Construction/ConstructionViewModel.cs:251`,`:252` | `LayersAbovePipe`/`LayersBelowPipe.CollectionChanged` → `OnLayersCollectionChanged` (VM-owned collections) | ConstructionViewModel, APP | none by design | exactly 1 each | `RE-009` adapter portion |
| 17 | `src/ViewModels/Construction/ConstructionViewModel.cs:255` | `Construction.DataChanged` → `OnConstructionDataChanged` | ConstructionViewModel, APP | none by design | exactly 1 | `RE-009` adapter portion |
| 18 | `src/ViewModels/Construction/ConstructionViewModel.cs:1055`/`:1061` (`ReconcileLayerSubscriptions`) | per-layer `Layer.PropertyChanged` → `OnSubscribedLayerPropertyChanged` | ConstructionViewModel, PER-ITEM (`_subscribedLayers` set) | reconcile detaches stale, attaches current | exactly 1 per live layer | `RE-009` |
| 19 | `src/ViewModels/Construction/ConstructionViewModel.cs:258` | `ICalculationStateService.PipeSpacingChanged` → `OnPipeSpacingChanged` | ConstructionViewModel, APP | none by design | exactly 1 | `RE-007` |
| 20 | `src/ViewModels/Thermal/ThermalViewModel.cs:266` | `ICalculationStateService.StateChanged` → `OnCalculationStateChanged` | ThermalViewModel, APP | none by design | exactly 1 | `RE-005` |
| 21 | `src/ViewModels/Thermal/ThermalViewModel.cs:267` | `ICalculationStateService.PipeSpacingChanged` → `OnPipeSpacingServiceChanged` | ThermalViewModel, APP | none by design | exactly 1 | `RE-007` |
| 22 | `src/ViewModels/Thermal/ThermalViewModel.cs:271` | `ThermalStateCoordinator.Completion` → `OnCoordinatorCompletion` | ThermalViewModel, APP | none by design (adapter and coordinator are the same DI singleton pair) | exactly 1 | `RE-P4-002` (adapter binding refresh) |
| 23 | `src/ViewModels/Thermal/ThermalViewModel.cs:272` | `ThermalStateCoordinator.UpstreamObserved` → `OnUpstreamObserved` | ThermalViewModel, APP | none by design | exactly 1 | `RE-P4-001` (adapter refresh signal) |
| 24 | `src/ViewModels/Shell/MainViewModel.cs:75` | `ICalculationStateService.StateChanged` → `OnCalculationStateChanged` | MainViewModel, APP | none by design | exactly 1 | `RE-004` (shell) |
| 25 | `src/ViewModels/Shell/MainViewModel.cs:78` | `IProjectSession.PropertyChanged` → `OnProjectStateChanged` (field `_projectStateService` is `IProjectSession` after Phase 9 alias removal) | MainViewModel, APP | none by design | exactly 1 | `RE-P1-001` (live subscriber) |
| 26 | `src/MainWindow.xaml.cs:114` | `MainViewModel.PropertyChanged` → `ViewModel_PropertyChanged` | MainWindow, WINDOW | none by design | exactly 1 | `RE-P1-001` (UI shell portion) |
| 27 | `src/Models/Construction/Construction.cs:97`,`:98` | `LayersAbovePipe.CollectionChanged`/`Layers.CollectionChanged` → `OnDataChanged` (model-internal) | `Construction` model (DI singleton `Construction`/`IConstructionData`), APP | none by design | exactly 1 each | upstream source of `RE-P4-001` |
| 28 | `src/Models/Hydraulics/CollectorData.cs:87` | `Circuits.CollectionChanged` → `OnPropertyChanged(CollectorTypeDisplayWithCount)` (model-internal) | `CollectorData`, PER-ITEM model lifetime | none by design | exactly 1 per collector | `RE-008` UI-notification portion |

### Re-grounding corrections to the map (no new edge, no boundary change)

- `RE-P1-001` subscriber column names "ResultsViewModel / MainWindow". Live
  code: `ResultsViewModel` contains **zero** event subscriptions (only
  arithmetic `+=` accumulations at `:1199,:1229,:1268`); the live
  `ProjectSession.PropertyChanged` subscriber is `MainViewModel`
  (`:78`, row 25), and `MainWindow` subscribes `MainViewModel.PropertyChanged`,
  not the session (row 26). Slice 7 refreshes the map wording accordingly.
- `RE-001/RE-002` anchors (`Circuits :728-730,1062-1082`) drifted; the live
  subscription site is `CircuitsViewModel.cs:915`-region + coordinator rows
  above; consumer handling lives in `OnContextChanged`-driven
  `NotifyThermalPropertiesChanged` / `UpdateFromClimateModule` and coordinator
  `OnContextChanged` (`HydraulicsStateCoordinator.cs:99-118`).
- `RE-P4-004` anchor `CalculationStateService.cs:53-58` remains accurate
  (rows 4–5).
- `ProjectRestoreAdapters.cs` introduces no subscriptions (interfaces only);
  the plan's hypothesis of "adapter-lifetime subscriptions introduced by
  Phase 9 slice 5/6" is refuted by live code — no row required.
- `RE-011`/`RE-012` (Results load/apply, orchestrator reset) and
  `RE-013`/`RE-014` (save/export commands) are action/command paths, not event
  subscriptions; they carry no `+=` rows and are covered by the Slice 2–4
  counters (orchestrator invocations, publication counts), not by the census.
- No `NEW` edge was found: every live site maps to an existing `RE-` row or
  overlay row. No `OWNER_DECISION_REQUIRED` arises from this census.

### View-infrastructure subscriptions (out of the six canonical domain views; classified for completeness)

Symmetric WPF lifetime bindings, each with matching unsubscribe or a
per-control XAML-generated lifetime: `MainWindow.xaml.cs:69,72` (`KeyDown`,
`Loaded`, window-lifetime); `Behaviors/TextBoxBehavior.cs` (5 attach groups /
12 subscription lines — `:47-49,:132-134,:212,:301-302,:370-372` — each with
matching detach on dependency-property change, `:53-55,:138-140,:217,:306-307,:376-378`);
`Behaviors/DataGridBehavior.cs:52/:56`; `Views/Shared/ConstructionVisualizationView.xaml.cs`
(`DataContextChanged`-driven re-subscription with detach at `:229-239`);
`Views/Construction/{MaterialEditorView,TemplateEditorView}.xaml.cs`
(`Loaded`/`Unloaded` symmetric; `vm.RequestClose +=` with `-=` at `:34`);
`Views/Construction/ConstructionView.xaml.cs:27/:35` (`SizeChanged` symmetric);
`Controls/Climate/CityAutoCompleteBox.xaml.cs:104` + XAML-generated child
subscriptions (per-control instance lifetime). These carry no canonical state
ownership and are not measured by the Phase 10 counters.

## (d) Baseline stabilization suites — PASS unmodified

Command (plan-exact):

```
dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ThermalMultiplicityCharacterizationTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests" --logger "trx;LogFileName=slice-1-reactive-census.trx" --results-directory "docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs"
```

Result: build 0 warnings / 0 errors; test run **79 passed / 0 failed / 0 skipped**
(413 ms). TRX:
`docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs/slice-1-reactive-census.trx`.

The accepted Phase 9 baseline is confirmed stable. Write-set of this slice:
this receipt + the TRX under `logs/`. No production or test code changed.

**SLICE 1: PASS**
