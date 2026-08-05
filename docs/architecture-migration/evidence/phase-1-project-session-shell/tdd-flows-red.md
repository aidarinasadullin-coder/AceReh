# Task 3 — lifecycle-flow, repeated-cycle, and failure characterization in RED

## Scope

This evidence records the RED characterization state for Phase 1 Task 3. Only
one test file was added/modified; no production code under `src/` was modified,
and Task 2 files were not changed.

The test file uses current public seams only (`ProjectStateService`,
`CalculationStateService`, `ProjectLoadOrchestrator`, `ResultsViewModel`,
`CircuitsViewModel`) and does not reference the future `IProjectSession`/
`ProjectSession` contract.

## Changed files

- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs` (modified)
- `docs/architecture-migration/evidence/phase-1-project-session-shell/tdd-flows-red.md` (this file)

No files under `src/` were modified.
Task 2 files (`ProjectSessionTests.cs`, `ProjectSessionLegacyStoreGuardTests.cs`)
were not edited.

## Note on Task 2 compile-RED

The working tree already contains untracked production files
`src/Services/Project/IProjectSession.cs` and `src/Services/Project/ProjectSession.cs`.
Because these types are present on disk, the test project compiles and the
expected Task 2 `CS0246` blocker does not occur. Task 3 was executed against
that existing state; no edits were made to the `IProjectSession`/`ProjectSession`
files or to any DI registration as part of this Task 3 work.

## Task 3 characterization tests added

1. `LoadProjectDataAsync_Success_ClearsRestoreGuard`  
   Verifies that the current `ICalculationStateService.IsLoadProjectInProgress`
   seam is `false` after a successful `ResultsViewModel.LoadProjectDataAsync` call.

2. `LoadProjectDataAsync_TwiceOnSingletonGraph_ReplacesIdentityWithoutStaleState`  
   Loads project A, then project B on the same `ResultsViewModel` / shared
   `IProjectStateService` instance and asserts that the final identity reflects
   B only, with no stale A state.

3. `LoadProjectDataAsync_ThenEdit_MarksDirtyThroughExistingStateService`  
   After a successful load, mutates a climate value and asserts that the project
   becomes dirty through the existing `IProjectStateService` / `IMarkDirtyService`
   seam.

4. `RepeatedResetCycles_DoNotDuplicateCircuitsEventSubscriptions`  
   Calls `ProjectLoadOrchestrator.ResetModules()` three times on the same
   `CircuitsViewModel` and asserts that old-circuit event handlers do not leak
   and new-circuit changes mark dirty exactly once.

5. `LoadProjectDataAsync_EarlyRestoreFailure_LeavesPartialStateAndClearsGuard`  
   Injects an exception in `IConstructionService.ImportProjectMaterialsAsync`.
   Asserts the exception propagates, the guard is cleared, identity is already
   mutated, `CurrentFilePath` is not rolled back, and the current behavior leaves
   the project dirty because `MarkClean()` is never reached.

6. `LoadProjectDataAsync_LateRestoreFailure_LeavesPartialStateAndClearsGuard`  
   Injects an exception in `IConstructionService.ImportProjectTemplatesAsync`.
   Asserts the exception propagates, the guard is cleared, identity and climate
   are already mutated, `CurrentFilePath` is not rolled back, `IsDirty` is left
   true, and `PipeSpacing` remains at its default because thermal restore happens
   after the failure point.

## Command run

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterization|FullyQualifiedName~MainViewModelTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~CircuitsViewModelEventLeakTests|FullyQualifiedName~DoubleCalculationPreventionTests"
```

## Result

Exit code: `0`

Key output:

```text
  SnowMeltingCalculator -> D:\IA\ace v.2\src\bin\Debug\net8.0-windows\win-x64\SnowMeltingCalculator.dll
  SnowMeltingCalculator.Tests -> D:\IA\ace v.2\tests\SnowMeltingCalculator.Tests\bin\Debug\net8.0-windows\SnowMeltingCalculator.Tests.dll
Тестовый запуск для D:\IA\ace v.2\tests\SnowMeltingCalculator.Tests\bin\Debug\net8.0-windows\SnowMeltingCalculator.Tests.dll (.NETCoreApp,Version=v8.0)
Версия VSTest 17.11.1 (x64)

Запуск выполнения тестов; подождите...
Общее количество тестовых файлов (1), соответствующих указанному шаблону.
  Пропущен ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile [15 ms]

Пройден!   : не пройдено     0, пройдено    83, пропущено     1, всего    84, длительность 15 s. - SnowMeltingCalculator.Tests.dll (net8.0)
```

A full `dotnet build` of the test project completed with exit code `0` and no
errors from the new Task 3 test file.

## Why this is still RED characterization

- The tests assert the *current* observable behavior of the existing public seams,
  not the desired future `ProjectSession` contract.
- The failure/partial-restore tests deliberately encode current no-rollback
  semantics (identity and climate mutate, guard clears, project is left dirty)
  rather than improving them.
- No production code was changed to make these tests pass.
- `IProjectSession`/`ProjectSession` exist as untracked files but are not used by
  Task 3 tests; Task 4 GREEN implementation of the canonical lifecycle owner and
  DI forwarding remains the next scheduled step.

## Deferred assertions

- Dirty Yes/No/Cancel and save-result failure for new/close flows: existing
  `MainViewModelTests.cs` already covers Yes/No/Cancel behavior. A dedicated
  save-result-failure characterization would require either editing that existing
  file or duplicating the full `MainViewModel` construction harness in the new
  file. To avoid broad existing-file edits, this is deferred to Task 8
  integration/user-flow QA and can be added later as a focused characterization.

- JSON parse failure leaving pre-load state untouched: existing
  `ResultsViewModelOpenProjectTests.cs` covers load-failure paths; the exact
  parse-failure boundary is deferred to the persistence/user-flow gate rather
  than duplicated here.

- Post-load full save/reload cycle: the existing `ProjectRoundTripTests.cs` and
  `ProjectFileService` tests already cover save/reload round-trips; a dedicated
  lifecycle-level save/reload characterization is deferred to the persistence
  vertical slice.

## Status

Task 3 characterization tests are written, compile, and pass against the current
public seams. The targeted RED command reports `0 failed, 83 passed, 1 skipped`.
Task 4 GREEN implementation of the canonical lifecycle owner remains forbidden
until Task 3 is accepted.
