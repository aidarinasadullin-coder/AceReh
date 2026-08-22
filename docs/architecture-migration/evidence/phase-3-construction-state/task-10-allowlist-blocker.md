# Phase 3 Task 10 allow-list blocker

Date: 2026-08-16

## Verdict

`BLOCKED` before Task 10 RED-test or production edits.

The approved Task 10 production allow-list does not include
`src/ViewModels/Construction/ConstructionViewModel.cs`, but the live source proves
that this file still owns all legacy dirty and downstream publication bypasses
that Task 10 requires removing or suppressing:

- eight direct `_markDirtyService.MarkDirty()` calls remain in the Construction
  ViewModel;
- `UpdateCalculations()` still raises the ViewModel compatibility event and calls
  `CalculationContext.UpdateConstruction(_construction, "Construction")`;
- user collection/scalar/layer shadow-writes call canonical state with
  `ConstructionMutationOrigin.SystemApply`, so the canonical completion cannot
  distinguish those user actions from genuine lifecycle/system application;
- the mutable `Construction` model still raises `DataChanged`, and
  `ThermalViewModel` subscribes directly to the DI `IConstructionData` model.

Changing only `CalculationContext.cs` and Construction state/projection files
could suppress one context call, but could not make canonical dirty semantics
origin-aware or prevent the direct mutable-model event path from invalidating
Thermal. Such a partial change would preserve two downstream authorities and
would not satisfy Task 10.

## Reproduction

Repository root:

```text
D:/IA/ace v.2
```

Command:

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo --filter "FullyQualifiedName~ScalarHasLoads_ChangedValue_MarksDirtyExactlyOnceAndPublishesContextOnce|FullyQualifiedName~AddLayerAbovePipe_MarksDirtyExactlyTwice_DueToDirectCallPlusCollectionChangedHandler|FullyQualifiedName~DirectLayerThicknessEdit_OnExistingLayer_MarksDirtyExactlyOnce|FullyQualifiedName~ApplyTemplate_WithOneAboveAndOneBelowLayer_MarksDirtyExactCountMeasured" --logger "trx;LogFileName=phase-3-task-10-allowlist-blocker.trx"
```

Result: exit `0`; `4 passed / 0 failed / 0 skipped / 4 total`.

The passing characterization confirms the live duplicate boundary rather than
Task 10 acceptance: add-above publishes context twice and marks dirty twice;
template apply records three dirty calls; scalar and direct layer edits still use
the legacy ViewModel path.

## Scope audit

- No production C# file was edited.
- No test source was edited or weakened.
- Task 9 canonical save via `ConstructionPersistenceMapper` was not changed.
- `.smc` schema/version/formulas, Thermal/Results ownership, UI, packages and
  artifacts were not changed.
- Task 11 and later work were not started.
- Known skip identity was not selected by this four-test command; skip count was
  zero.

## Required owner decision

Authorize `src/ViewModels/Construction/ConstructionViewModel.cs` as a narrow Task
10 production exception (and only if RED tests prove it necessary, the existing
mutable Construction compatibility event seam). This is required to route user
actions with `User`/`Template` origins, remove direct ViewModel dirty/context
publication, and leave lifecycle origins non-user. Otherwise Task 10 must remain
blocked rather than shipping a partial context-only suppression.

## Resolution

The owner subsequently authorized the exact narrow exception with the statement
`да, разрешаю ConstructionViewModel.cs для Task 10`. The blocker is therefore
historical discovery evidence, not the current Task 10 status. RED-first
implementation and final executable verification are recorded in
`task-10-downstream-dirty-completion.md`.
