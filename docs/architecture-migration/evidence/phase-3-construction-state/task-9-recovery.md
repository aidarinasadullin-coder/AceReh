# Phase 3 Task 9 recovery verification

Дата: 2026-08-16

Verifier: Atlas orchestrator in the current session. Prior Todo 7 worker
attempts `ses_ff895a3c3ffeX4JvHdoOc322Hs` and
`ses_ff88c206dffeuM2QkBBr2EYHPs` returned planning-only responses and are not
acceptance authority.

## Verdict

`PASS` — Phase 3 Task 9 recovery is accepted by independent verification.

This accepts only the Task 9 recovery for mutation-time canonical Construction
persistence. The original failure handoff remains retained as historical RED
evidence. Task 10 was not started. Phase 3 is not completed or owner-accepted;
the recovery plan Final Verification Wave remains pending.

Historical RED evidence retained:
`docs/architecture-migration/evidence/phase-3-construction-state/task-9-failure-investigation-handoff.md`.

## Commands and results

### Direct recovery gate

Command:

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo --filter "FullyQualifiedName~DirectCollectionClear_ShadowWritesCanonicalSnapshot_AndDetachesRemovedLayers|FullyQualifiedName~DirectCollectionAdd_ShadowWritesCompleteLayer_AndSubscribesExactlyOnce|FullyQualifiedName~DirectCollectionRemove_ShadowWritesCanonicalSnapshot_AndDetachesLayer|FullyQualifiedName~DirectCollectionMove_ShadowWritesOrder_AndPreservesSingleSubscription|FullyQualifiedName~DirectThicknessChange_ShadowWritesCanonicalLayer|FullyQualifiedName~DirectCalculatedLambdaChange_ShadowWritesExactCanonicalValue|FullyQualifiedName~DirectMaterialChange_ShadowWritesFinalMaterialLambdaAndOverrideState|FullyQualifiedName~DirectIsLambdaOverriddenChange_ShadowWritesCanonicalFlag|FullyQualifiedName~LifecycleGuards_SuppressCanonicalWriteBack_AndReconcileSubscriptions|FullyQualifiedName~RejectedShadowWrite_PreservesLastValidCanonicalSnapshot|FullyQualifiedName~ExistingScalarShadowWrites_RemainGreen" --logger "trx;LogFileName=phase-3-task-9-recovery-atlas-direct.trx"
```

Result: exit `0`; `11 passed / 0 failed / 0 skipped / 11 total`.

Raw artifact:
`tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-9-recovery-atlas-direct.trx`.

### Focused persistence gate

Command:

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo --filter "FullyQualifiedName~ProjectData_Load_ReindexesOrder|FullyQualifiedName~ProjectData_LayerOrder_RoundTrip_PreservesLambdaE|FullyQualifiedName~ProjectData_Save_v1_1_SetsVersion|FullyQualifiedName~ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation|FullyQualifiedName~ProjectRoundTrip_PreservesLambdaValueButResetsOverrideFlag|FullyQualifiedName~SaveCurrentProject_PersistsConstructionStateSnapshot_NotConstructionViewModelMirror" --logger "trx;LogFileName=phase-3-task-9-recovery-atlas-focused.trx"
```

Result: exit `0`; `6 passed / 0 failed / 0 skipped / 6 total`.

Raw artifact:
`tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-9-recovery-atlas-focused.trx`.

### Full Task 9 targeted gate

Command:

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ConstructionRepositoryTests|FullyQualifiedName~ConstructionServiceTests|FullyQualifiedName~ConstructionViewModelTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests" --logger "trx;LogFileName=phase-3-task-9-recovery-atlas-targeted.trx"
```

Result: exit `0`; `138 passed / 0 failed / 1 skipped / 139 total`.

Exact skipped test: `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.

Raw artifact:
`tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-9-recovery-atlas-targeted.trx`.

### Debug build

Command:

```powershell
dotnet build "src\SnowMeltingCalculator.csproj" -c Debug --nologo
```

Result: exit `0`; warnings `0`; errors `0`.

### LSP diagnostics

`lsp_diagnostics` for
`src/ViewModels/Construction/ConstructionViewModel.cs` reproduced the known
harness/root mismatch:

```text
LSP file path must be inside request cwd: D:\IA\ace v.2\src\ViewModels\Construction\ConstructionViewModel.cs
```

The authoritative C# gates for this recovery are the fresh `dotnet test` and
`dotnet build` runs above.

## Source and scope audit

- `ResultsViewModel.SaveCurrentProject()` writes project Construction data from
  `_projectSession.ConstructionState.Snapshot` through
  `ConstructionPersistenceMapper.ToProjectData(...)`.
- No `SyncToCanonicalState()` call exists under `src/ViewModels/Results`.
- `ConstructionViewModel` recovery is mutation-time only: reference-identity
  `_subscribedLayers`, `_pendingMaterialLambdaUpdates`,
  `ReconcileLayerSubscriptions()`, collection shadow-write in
  `OnLayersCollectionChanged()`, guarded property shadow-write in
  `OnLayerPropertyChanged()`, and
  `SyncStateFromCollections(ConstructionMutationOrigin.SystemApply)`.
- `ProjectSessionConstructionState` remains the writable canonical owner via
  `Snapshot` and `ApplySnapshot(...)`.
- `ConstructionPersistenceMapper` remains a pure canonical snapshot to project
  DTO mapper; `.smc` schema, version, fields, order, material fallback/custom
  import and lambda value/override semantics are unchanged.
- Task 10 was not started: no completion multiplicity cleanup, downstream
  invalidation consolidation, publication redesign, or broad batching/coalescing
  was introduced beyond the narrow material setter cascade guard required for
  Task 9 correctness.
- The six architecture views remain truthful for this narrow recovery and do not
  require map/model/widget edits before the Final Verification Wave.

## Context outcome

Task 9 recovery is accepted by this independent verification. Final Wave F1-F4
must still approve before the recovery boulder is complete, and owner acceptance
of Phase 3 remains a separate later gate.

## Final Verification Wave

### F1. Plan compliance and dirty-worktree audit

`F1 VERDICT: APPROVE`

- Reviewer note: three delegated F1 attempts returned planning-only text and no
  usable verdict, so Atlas performed the F1 gate directly without editing source
  code or staging/resetting the dirty worktree.
- Active recovery plan shows Todos 1-7 complete and F1-F4 pending before this
  verdict. Todo 7 receipt exists and records Task 9 recovery acceptance, Task 10
  not started, and Phase 3 not owner-accepted/completed.
- Focused six-test gate command:

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo --filter "FullyQualifiedName~ProjectData_Load_ReindexesOrder|FullyQualifiedName~ProjectData_LayerOrder_RoundTrip_PreservesLambdaE|FullyQualifiedName~ProjectData_Save_v1_1_SetsVersion|FullyQualifiedName~ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation|FullyQualifiedName~ProjectRoundTrip_PreservesLambdaValueButResetsOverrideFlag|FullyQualifiedName~SaveCurrentProject_PersistsConstructionStateSnapshot_NotConstructionViewModelMirror" --logger "trx;LogFileName=phase-3-task-9-recovery-f1-focused.trx"
```

- Focused six-test result: exit `0`; `6 passed / 0 failed / 0 skipped / 6 total`.
  Raw artifact:
  `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-9-recovery-f1-focused.trx`.
- Dirty worktree remains heavily polluted with unrelated pre-existing changes;
  F1 does not treat those unrelated files as Task 9 recovery scope. The relevant
  Task 9 recovery set remains the closed recovery source/test/doc/evidence set:
  `ConstructionViewModel.cs`, `ConstructionViewModelTests.cs`,
  `ConstructionServiceTests.cs`, `ResultsViewModelOpenProjectTests.cs`,
  `TASK_CONTEXT.md`, this receipt, notepad entries, and Task 9 TRX artifacts.
- No evidence was found that Todo 7 or F1 overwrote the original failure handoff,
  architecture maps, model/widget, or started Task 10.

### F2. Canonical ownership and code-quality audit

`F2 VERDICT: APPROVE`

- Reviewer note: delegated F2 attempt returned planning-only text and no usable
  verdict, so Atlas performed the F2 gate directly without editing source code
  or staging/resetting the dirty worktree.
- Source audit confirmed `ConstructionViewModel` keeps a reference-identity
  `_subscribedLayers` set, `ReconcileLayerSubscriptions()` removes stale layer
  handlers and adds missing current handlers, and `OnLayersCollectionChanged()`
  reconciles subscriptions before mutation-time canonical shadow-write outside
  `_isSyncing`, `_isResetting`, and `_isRefreshing`.
- Source audit confirmed `OnLayerPropertyChanged()` handles `Thickness`,
  `CalculatedLambda`, `Material`, and `IsLambdaOverridden` through guarded
  mutation-time `SyncStateFromCollections(ConstructionMutationOrigin.SystemApply)`.
  `_pendingMaterialLambdaUpdates` is limited to the material setter cascade and
  is not a broad Task 10 batching/coalescing/publication redesign.
- `ProjectSessionConstructionState` remains the writable canonical owner via
  `Snapshot`/`ApplySnapshot(...)`; `ConstructionPersistenceMapper` remains a pure
  canonical snapshot to project DTO mapper; `ResultsViewModel.SaveCurrentProject()`
  reads `_projectSession.ConstructionState.Snapshot` through that mapper and no
  `SyncToCanonicalState()` call exists under `src/ViewModels/Results`.
- Recovery-specific 11-test command:

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo --filter "FullyQualifiedName~DirectCollectionClear_ShadowWritesCanonicalSnapshot_AndDetachesRemovedLayers|FullyQualifiedName~DirectCollectionAdd_ShadowWritesCompleteLayer_AndSubscribesExactlyOnce|FullyQualifiedName~DirectCollectionRemove_ShadowWritesCanonicalSnapshot_AndDetachesLayer|FullyQualifiedName~DirectCollectionMove_ShadowWritesOrder_AndPreservesSingleSubscription|FullyQualifiedName~DirectThicknessChange_ShadowWritesCanonicalLayer|FullyQualifiedName~DirectCalculatedLambdaChange_ShadowWritesExactCanonicalValue|FullyQualifiedName~DirectMaterialChange_ShadowWritesFinalMaterialLambdaAndOverrideState|FullyQualifiedName~DirectIsLambdaOverriddenChange_ShadowWritesCanonicalFlag|FullyQualifiedName~LifecycleGuards_SuppressCanonicalWriteBack_AndReconcileSubscriptions|FullyQualifiedName~RejectedShadowWrite_PreservesLastValidCanonicalSnapshot|FullyQualifiedName~ExistingScalarShadowWrites_RemainGreen" --logger "trx;LogFileName=phase-3-task-9-recovery-f2-direct.trx"
```

- Recovery-specific 11-test result: exit `0`; `11 passed / 0 failed / 0 skipped / 11 total`.
  Raw artifact:
  `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-9-recovery-f2-direct.trx`.

### F3. Persistence/lifecycle real QA

`F3 VERDICT: APPROVE`

- Atlas performed the F3 gate directly after F1/F2 delegation attempts proved
  unreliable. No source code, tests, maps, model/widget, staging, reset or Task
  10 work was performed for this verdict.
- Source/test audit confirmed the in-gate round-trip assertions cover ordered
  layer sequences, material fallback/custom import paths through existing
  `ConstructionRepositoryTests`/`ConstructionServiceTests`, exact lambda value
  persistence, override reset semantics, second-load replacement, and scalar
  preservation. The focused tests include
  `ProjectData_Load_ReindexesOrder`,
  `ProjectData_LayerOrder_RoundTrip_PreservesLambdaE`,
  `ProjectData_Save_v1_1_SetsVersion`,
  `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`,
  `ProjectRoundTrip_PreservesLambdaValueButResetsOverrideFlag`, and
  `SaveCurrentProject_PersistsConstructionStateSnapshot_NotConstructionViewModelMirror`.
- Complete Task 9 targeted command:

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ConstructionRepositoryTests|FullyQualifiedName~ConstructionServiceTests|FullyQualifiedName~ConstructionViewModelTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests" --logger "trx;LogFileName=phase-3-task-9-recovery-f3-targeted.trx"
```

- Complete Task 9 targeted result: exit `0`; `138 passed / 0 failed / 1 skipped / 139 total`.
  Exact skipped test: `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.
  Raw artifact:
  `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-9-recovery-f3-targeted.trx`.
- Debug build command:

```powershell
dotnet build "src\SnowMeltingCalculator.csproj" -c Debug --nologo
```

- Debug build result: exit `0`; warnings `0`; errors `0`.

### F4. Scope fidelity and workflow stop audit

`F4 VERDICT: APPROVE`

- Atlas performed the F4 gate directly after reading all six architecture views:
  `compile-time.md`, `di-runtime.md`, `state-ownership.md`, `reactive.md`,
  `persistence.md`, and `user-flow.md`.
- The recovery does not require map/model/widget edits. The current maps still
  describe Construction as a partial/transitioning vertical slice where
  `ProjectSession.ConstructionState` is the target/canonical owner for the
  recovered persistence path, while later cleanup remains outside Task 9 scope.
- `.smc` schema/version and formula behavior remain unchanged: project save still
  uses the existing `ProjectData` / `ConstructionProjectData` / `LayerProjectData`
  shape and version behavior, with Construction data mapped from canonical
  `ConstructionStateSnapshot` by `ConstructionPersistenceMapper`.
- DI and production constructor scope remain bounded. The production
  `ProjectLoadOrchestrator` optional `IProjectSession` seam and
  `ConstructionViewModel` optional construction-state seam are pre-existing Phase
  3 migration seams; Task 9 recovery did not introduce Task 10 constructor or DI
  redesign.
- `TASK_CONTEXT.md` records Task 9 recovery only after green Todo 7 evidence,
  retains the 2026-08-14 failure handoff as historical RED evidence, says Task 10
  was not started, and keeps Phase 3 as not owner-accepted/completed.
- Final status audit confirms the dirty worktree remains heavily polluted with
  unrelated pre-existing changes; no reset/stage/checkout/clean was performed.
  New recovery artifacts are limited to the Task 9 receipt/context/notepad/TRX
  evidence and plan checkbox updates needed by this boulder.
- Scope scan found only forward-looking Task 10 comments in
  `ConstructionViewModel.cs`; no Task 10 implementation was started: no
  completion multiplicity cleanup, downstream invalidation consolidation,
  publication redesign, or broad batching/coalescing was added.
