# Task 6 — ConstructionViewModel Adapter: Shadow-Write Dirty Origin Fix

Plan SHA-256: `B81E82DEFC2DC2D2108F9240BDED6575FD1244DFCBC164AB2602829249CC5FB2`

> Receipt for the Task 6 correction lane: shadow-writes from
> `ConstructionViewModel` into `ProjectSessionConstructionState` no longer add
> extra `IMarkDirtyService.MarkDirty()` calls on top of the legacy path while
> Task 6 remains a transitional adapter (shadow-write; canonical ownership
> takeover stays in Task 10).

## 1. Root cause

`SyncStateFromCollections(origin)` in
`src/ViewModels/Construction/ConstructionViewModel.cs` mirrors the live VM
collections/scalars into `IProjectSessionConstructionState` via
`ApplySnapshot`. `ProjectSessionConstructionState.CompleteChanged` marks the
project dirty for user-visible origins:

```csharp
if (origin == ConstructionMutationOrigin.User || origin == ConstructionMutationOrigin.Template)
{
    _markDirtyService?.MarkDirty();
}
```

Task 6 deliberately retains the legacy VM dirty path, so every mutation
already calls `_markDirtyService.MarkDirty()` at least once through the legacy
code. Passing `User`/`Template` to the shadow-write therefore added one extra
`MarkDirty()` per changed shadow-write.

Runtime evidence (latest targeted run before the fix): 4 failed, 51 passed in
`ConstructionMultiplicityCharacterizationTests`/`ConstructionViewModelTests`:

| Test | Expected | Observed |
| --- | --- | --- |
| `ApplyTemplate_WithOneAboveAndOneBelowLayer_MarksDirtyExactCountMeasured` | 6 | 7 |
| `RemoveLayer_MarksDirtyExactlyTwice_DueToDirectCallPlusCollectionChangedHandler` | 2 | 3 |
| `ScalarGroundwaterLevel_ChangedValue_MarksDirtyExactlyTwice_DirectPlusLayerLambdaUpdate` | 2 | 3 |
| `ScalarHasLoads_ChangedValue_MarksDirtyExactlyOnceAndPublishesContextOnce` | 1 | 2 |

Each failure is exactly one extra dirty call from the shadow-write origin.

## 2. Fix

File changed: `src/ViewModels/Construction/ConstructionViewModel.cs` only.

All six `SyncStateFromCollections(...)` shadow-write call sites now use
`ConstructionMutationOrigin.SystemApply` (the legacy VM code performs the
dirty semantics, so the canonical state must not add its own):

| Method | Before | After |
| --- | --- | --- |
| `AddLayerAbovePipe()` | `SystemApply` | `SystemApply` (unchanged) |
| `AddLayerBelowPipe()` | `SystemApply` | `SystemApply` (unchanged) |
| `RemoveLayer(Layer?)` | `User` | `SystemApply` |
| `ApplyTemplateCore(ConstructionTemplate)` | `Template` | `SystemApply` |
| `OnGroundwaterLevelChanged(double)` | `User` | `SystemApply` |
| `OnHasLoadsChanged(bool)` | `User` | `SystemApply` |

`OnConstructionStateChanged` remains a no-op: project-loaded VM collections
are still driven by the legacy path, and state-driven collection refresh stays
disabled until Task 10.

## 3. Verification

Targeted tests:

```text
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ConstructionMultiplicityCharacterizationTests|FullyQualifiedName~ConstructionViewModelTests"
-> Пройден!   : не пройдено 0, пройдено 55, пропущено 0, всего 55
```

Debug build:

```text
dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo
-> Ошибок: 0
```

Pre-existing warnings only (nullable-assignment at
`ConstructionViewModel.cs` 1290/1310 and never-assigned `_isRefreshing` at
line 40); no new errors introduced by this change.

## 4. Scope notes

- Test expectations were NOT changed; the fix removes the extra dirty call at
  its source instead.
- No changes to `ProjectLoadOrchestrator`, `ResultsViewModel`,
  `CalculationContext`, DI, state implementation, models, or XAML.
- Task 6 remains transitional: legacy VM collections/scalars stay active while
  shadow-writing to `ConstructionState` until Task 8/10 move restore and
  downstream ownership.
