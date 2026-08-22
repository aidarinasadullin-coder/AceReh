# Task 2 — Writer and Subscriber Inventory: Current Construction Ownership Surface

Plan SHA-256: `B81E82DEFC2DC2D2108F9240BDED6575FD1244DFCBC164AB2602829249CC5FB2`

> This receipt inventories the CURRENT (pre-migration) Construction
> writer/subscriber surface. It is the evidence base for Task 3
> characterization and the exact bypass list that Tasks 4-11 must eliminate.
> No production code was modified in this task; only a guard test class and
> this document were created.

## 1. Guard test class

`tests/SnowMeltingCalculator.Tests/Services/Project/ConstructionStateLegacyStoreGuardTests.cs`

Four tests, all passing against the current codebase:

| Test | Purpose |
| --- | --- |
| `ConstructionStateLegacyStoreGuard_CapturesExactCurrentWriterInventory` | Enumerates every current mutation boundary and bypass (see §2) |
| `ConstructionStateLegacyStoreGuard_CapturesExactCurrentSubscriptionInventory` | Enumerates attach/detach sites for collection/item/model subscriptions (see §3) |
| `ConstructionStateLegacyStoreGuard_RejectsNewDirectConstructionViewModelSetterInForbiddenCallers` | Negative fixture: detects a NEW direct `_constructionViewModel.<Property> =` write pattern in any caller |
| `ConstructionStateLegacyStoreGuard_RejectsMissingLayerPropertyChangedUnsubscribe` | Negative fixture: detects a regression that removes the per-layer `PropertyChanged` detach call |

Run receipt:

```text
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ConstructionStateLegacyStoreGuardTests"
-> Пройден!   : не пройдено 0, пройдено 4, пропущено 0, всего 4
```

Debug build of `src\SnowMeltingCalculator.csproj`: `0` warnings, `0` errors.

## 2. Current writer inventory (bypass list)

### 2.1 `ConstructionViewModel` — canonical backing store today

`ConstructionViewModel` currently holds the entire canonical Construction
project state directly as its own `[ObservableProperty]` fields:

- `_groundwaterLevel` (double, default `2.0`)
- `_hasLoads` (bool)
- `_layersAbovePipe` (`ObservableCollection<Layer>`)
- `_layersBelowPipe` (`ObservableCollection<Layer>`)

It also owns a private mutable `ConstructionModel _construction` instance and
three private sync methods that copy data bidirectionally:

- `SyncFromModel()` — model → ViewModel collections/scalars
- `SyncToModel()` — ViewModel collections/scalars → model, then `_construction.ReindexLayers()`
- `CopyConstructionData(ConstructionModel source)` — external model → `_construction`

### 2.2 Direct dirty-marking bypass (9 call sites)

`_markDirtyService.MarkDirty()` is called directly from `ConstructionViewModel`
at 9 sites (line numbers from current source):

| Line | Method |
| --- | --- |
| 298 | `AddLayerAbovePipe()` |
| 325 | `AddLayerBelowPipe()` |
| 349 | `RemoveLayer(Layer? layer)` |
| 457 | `ApplyTemplateCore(ConstructionTemplate template)` |
| 855 | `OnLayerChanged(Layer layer)` |
| 944 | `OnGroundwaterLevelChanged(double value)` (partial property-changed handler) |
| 956 | `OnHasLoadsChanged(bool value)` (partial property-changed handler) |
| 1056 | `OnLayersCollectionChanged(...)` (collection add/remove, guarded by `!_isSyncing && !_isResetting`) |
| 1075 | `OnLayerPropertyChanged(...)` (per-layer Thickness/Lambda/Material change, guarded by `_isSyncing`/`_isResetting`) |

Task 6/10 must eliminate all 9 as direct calls and replace them with dirty
semantics derived from canonical mutation origin (`User`/`Template` dirty;
`Reset`/`ProjectLoad`/`Restore`/`Initialization` do not dirty).

### 2.3 Direct `CalculationContext` publication bypass (1 call site)

`UpdateCalculations()` (line 888) calls
`_calculationContext.UpdateConstruction(_construction, "Construction")`
directly whenever `IsValid` is true. This is the only current downstream
publication path; Task 10 must route it through the canonical completion
sequence instead.

### 2.4 Construction model's own mutation API (not currently used as canonical path from VM)

`Construction.cs` still exposes public mutation methods that were NOT used by
`ConstructionViewModel` directly (the VM manipulates
`LayersAbovePipe`/`LayersBelowPipe` collections and `SyncToModel()` instead),
but they remain reachable by any other caller and must be accounted for in
Task 5's projection boundary:

- `AddLayerAbovePipe(Material material, double thickness)`
- `AddLayerBelowPipe(Material material, double thickness)`
- `RemoveLayer(Layer layer)`
- `ReindexLayers()`
- `ClearLayers()`
- `UpdateLambdaForGroundwater()`
- settable `GroundwaterLevel`, `HasLoads` properties

### 2.5 `ProjectLoadOrchestrator` direct ViewModel writes (bypass)

`ProjectLoadOrchestrator.RestoreModulesFromProjectAsync` writes exactly two
scalars directly on the ViewModel (outside `LoadLayersFromProjectData`):

```csharp
_constructionViewModel.GroundwaterLevel = data.ConstructionData.GroundwaterLevel;
_constructionViewModel.HasLoads = data.ConstructionData.HasLoads;
```

`LoadLayersFromProjectData` (private helper in the orchestrator) additionally
writes the collections directly:

```csharp
_constructionViewModel.LayersAbovePipe.Clear();
_constructionViewModel.LayersBelowPipe.Clear();
_constructionViewModel.LayersAbovePipe.Add(layer);   // per above-pipe layer
_constructionViewModel.LayersBelowPipe.Add(layer);   // per below-pipe layer
```

`ResetModules()` delegates Construction reset directly to the ViewModel:

```csharp
_constructionViewModel.Reset();
```

The guard's regex-based negative fixture proves any NEW direct
`_constructionViewModel.<Property> =` write (beyond the two currently
allow-listed scalars) would be detected the same way.

### 2.6 `ResultsViewModel` — currently read/save-only for Construction

No direct `_constructionViewModel.<Property> =` write exists in
`ResultsViewModel` today (verified by the guard's
`GetDirectConstructionViewModelWrites` regex returning empty). It reads
Construction data via `_constructionViewModel.GetConstruction()` for save and
via `ReloadMaterialsAsync()` for catalog refresh; this must remain read-only
in Task 9.

### 2.7 DI / `ProjectSession` — no `ConstructionState` exists yet

`ServiceCollectionExtensions.cs` contains no
`IProjectSessionConstructionState` registration; `ProjectSession.cs` contains
no `ConstructionState` member. This confirms the pre-migration baseline: no
second partial owner has been introduced by accident before Task 4.

## 3. Current subscription inventory

### 3.1 Constructor-time attach sites (`ConstructionViewModel` ctor)

| Subscription | Target |
| --- | --- |
| `LayersAbovePipe.CollectionChanged += OnLayersCollectionChanged;` | own collection |
| `LayersBelowPipe.CollectionChanged += OnLayersCollectionChanged;` | own collection |
| `_construction.DataChanged += OnConstructionDataChanged;` | mutable model event |
| `_calculationStateService.PipeSpacingChanged += OnPipeSpacingChanged;` | external service event |

None of these four are detached anywhere (VM lifetime == app lifetime today;
no dispose path exists). This is a known characterization fact, not a defect
introduced by Task 2.

### 3.2 Per-item layer subscription (inside `OnLayersCollectionChanged`)

Exactly one attach site and one detach site (counted via source-text
occurrence, confirmed `1`/`1` by the guard):

```csharp
if (e.NewItems != null) { foreach (Layer layer in e.NewItems) { layer.PropertyChanged += OnLayerPropertyChanged; } }
if (e.OldItems != null) { foreach (Layer layer in e.OldItems) { layer.PropertyChanged -= OnLayerPropertyChanged; } }
```

This detach path is exercised whenever a layer is removed from either
collection (`Remove`, `Clear`, or `SyncFromModel`/`SyncToModel`'s
`Clear()`+`Add()` pattern). The negative-fixture test proves that removing
the unsubscribe line is caught by source-text inspection; Task 3 must still
measure the *runtime* handler-count behavior (e.g. repeated
load/reset cycles) rather than relying on source inspection alone.

### 3.3 `Construction` model's own collection subscriptions (ctor)

```csharp
LayersAbovePipe.CollectionChanged += (s, e) => OnDataChanged();
Layers.CollectionChanged += (s, e) => OnDataChanged();
```

Anonymous lambda subscriptions with no detach path (model lifetime ==
ViewModel lifetime today).

## 4. Guardrails

- No production file (`ConstructionViewModel.cs`, `Construction.cs`,
  `Layer.cs`, `ProjectLoadOrchestrator.cs`, `ResultsViewModel.cs`,
  `CalculationContext.cs`, `ServiceCollectionExtensions.cs`) was modified.
- Only two files were created: this evidence document and
  `tests/SnowMeltingCalculator.Tests/Services/Project/ConstructionStateLegacyStoreGuardTests.cs`.
- No `git add`, `commit`, `reset`, `clean`, `checkout`, `push` or `stash` was
  run.
- HEAD remains `e655735dfa66c00cf9c53be93d511eda8989e8bf`; staged set remains
  empty.

## 5. Handoff to Task 3

Task 3 must now measure, with counter-based characterization tests, the exact
*runtime* multiplicity for every logical action listed in the plan (scalar
edits, add/remove/reorder, template apply, missing-material import paths,
editor true/false/null, initialization, resets, project load/second-load,
repeated cycles) against this bypass inventory — in particular the 9
`MarkDirty()` sites and the single `UpdateConstruction` publication site
identified above.
