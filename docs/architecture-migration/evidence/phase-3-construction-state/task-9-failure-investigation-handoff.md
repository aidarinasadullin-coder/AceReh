# Phase 3 Task 9 failure investigation handoff

Date: 2026-08-14

Scope: read-only investigation. Phase 3 Task 9 was not implemented or accepted
by this investigation. Phase 3 Task 10 was not started.

## Verified gate state

The latest targeted command was:

```powershell
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ConstructionRepositoryTests|FullyQualifiedName~ConstructionServiceTests|FullyQualifiedName~ConstructionViewModelTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests"
```

Observed result: `5 failed / 122 passed / 1 skipped / 128 total`.

The remaining failures are:

1. `ProjectData_LayerOrder_RoundTrip_PreservesLambdaE`: expected two above-pipe
   layers after round-trip, observed one.
2. `ProjectData_Load_ReindexesOrder`: expected three above-pipe layers after
   round-trip, observed one.
3. `ProjectData_Save_v1_1_SetsVersion`: saved Construction layers are empty.
4. `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`:
   the live material is absent because saved Construction layers are empty.
5. `ProjectRoundTrip_PreservesLambdaValueButResetsOverrideFlag`: expected saved
   `CalculatedLambda = 9.999`, observed the prior/default value `1.6` after load.

## Root cause

`ResultsViewModel.SaveCurrentProject()` now correctly reads only canonical
`_projectSession.ConstructionState.Snapshot` through
`ConstructionPersistenceMapper`. It no longer projects writable
`ConstructionViewModel` collections directly.

The adapter write boundary is incomplete:

- `ConstructionViewModel.OnLayersCollectionChanged()` subscribes/unsubscribes
  layer events, marks dirty and recalculates, but does not call
  `SyncStateFromCollections(...)`. Direct `Clear`, `Add`, `Remove` and similar
  collection mutations therefore remain only in the VM/model mirror.
- `ConstructionViewModel.OnLayerPropertyChanged()` recalculates for
  `Thickness`, `CalculatedLambda` and `Material`, but does not update canonical
  state. It also does not include `IsLambdaOverridden` in the handled property
  set.
- Scalar handlers for `GroundwaterLevel` and `HasLoads` already call
  `SyncStateFromCollections(...)`, which explains why equivalent scalar tests
  are not among the five remaining failures.
- The affected tests mutate VM collections and layer objects directly, then
  call `SaveCurrentProject()`. Because the canonical snapshot is stale or
  empty, the pure persistence mapper faithfully saves stale or empty data.

A save-time `_constructionViewModel.SyncToCanonicalState()` is not an acceptable
fix. Runtime evidence showed that it overwrites a deliberately newer canonical
snapshot with stale VM defaults and breaks
`SaveCurrentProject_PersistsConstructionStateSnapshot_NotConstructionViewModelMirror`.
The correct direction is mutation-time adapter-to-state synchronization, while
save remains read-only over canonical state.

Some test helpers also construct `ProjectLoadOrchestrator` without passing the
same `IProjectSession` supplied to `ResultsViewModel` and the session-backed
`ConstructionViewModel`. The new plan must inventory and normalize this wiring
where canonical lifecycle behavior is under test. This helper inconsistency is
secondary to the missing mutation-time writes but can select the legacy load
path and obscure round-trip behavior.

## Planning constraints

- Do not restore blind VM-to-state synchronization in `SaveCurrentProject()`.
- Keep `ProjectSession.ConstructionState` as the sole writable canonical owner.
- Keep `ConstructionViewModel` as an adapter; direct UI-bound mutations must be
  captured at their mutation boundary.
- Preserve guards for `_isSyncing`, `_isResetting` and `_isRefreshing` to avoid
  loops and lifecycle write-back.
- Cover collection changes plus `Thickness`, `CalculatedLambda`, `Material` and
  `IsLambdaOverridden`; verify scalar behavior remains intact.
- Preserve `.smc` v1.0/v1.1 schema, fields, ordering, lambda override/value and
  material fallback/custom import behavior.
- Do not implement Task 10 concerns such as completion multiplicity cleanup,
  downstream invalidation consolidation or broad publication redesign. Multiple
  intermediate shadow writes may be characterized, but Task 9 must not solve
  Task 10 prematurely.
- Normalize test helper session identity only where required to exercise the
  production canonical path; do not weaken assertions or hide failures.
- Treat existing Task 9 acceptance/evidence claims as superseded by this red
  gate until a separately reviewed and authorized recovery plan is executed and
  independently verified.

## Required recovery verification

The recovery plan must first keep these six tests together as a focused gate:

1. `ProjectData_Load_ReindexesOrder`
2. `ProjectData_LayerOrder_RoundTrip_PreservesLambdaE`
3. `ProjectData_Save_v1_1_SetsVersion`
4. `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`
5. `ProjectRoundTrip_PreservesLambdaValueButResetsOverrideFlag`
6. `SaveCurrentProject_PersistsConstructionStateSnapshot_NotConstructionViewModelMirror`

The first five must turn green while the sixth remains green. Then run the full
Task 9 targeted command above and a Debug build. Evidence, plan state and
`TASK_CONTEXT.md` may claim Task 9 acceptance only after those gates pass under
independent verification.

## Next action

Create and review a decision-complete Task 9 recovery plan only. Do not execute
it in the planning session. Do not start Task 10.
