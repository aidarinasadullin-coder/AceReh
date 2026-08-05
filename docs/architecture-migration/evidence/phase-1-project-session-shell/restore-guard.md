# Phase 1 Task 6 — Restore guard centralized in ProjectSession

## Scope

Move `IsLoadProjectInProgress` storage from `CalculationStateService` to
`ProjectSession`. `CalculationStateService` becomes a forwarding compatibility
view with a single lease reference and no local bool/depth field. Production
restore flow in `ResultsViewModel.LoadProjectDataAsync` now uses
`_projectSession.BeginProjectRestore()` instead of toggling the compatibility
setter.

## Changed files

- `src/Services/Navigation/CalculationStateService.cs` — injects
  `IProjectSession`; `IsLoadProjectInProgress` delegates reads to the session and
  implements the compatibility lease setter rule; removed local guard backing
  field.
- `src/ViewModels/Results/ResultsViewModel.cs` — accepts `IProjectSession` and
  uses `BeginProjectRestore()` for the restore scope; `ProjectNumber`/`ProjectObject`
  dirty checks use the canonical session guard.
- `src/Services/Results/ProjectStateService.cs` — exposed `Session` property so
  test adapters can share the canonical session during the transition.
- Test helper adjustments in:
  - `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs`
  - `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`
  - `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs`
  - `tests/SnowMeltingCalculator.Tests/ViewModels/MainViewModelTests.cs`
  - `tests/SnowMeltingCalculator.Tests/Services/Navigation/DialogServiceThreadAffinityTests.cs`
  - `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs`
  - `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`

These test changes only wire the existing test doubles to the same
`IProjectSession` instance; no test semantics changed.

## Commands and results

### Debug production build

```bash
dotnet build src/SnowMeltingCalculator.csproj -c Debug
```

Result: 0 warnings, 0 errors.

### Release production build

```bash
dotnet build src/SnowMeltingCalculator.csproj -c Release
```

Result: 0 warnings, 0 errors.

### Targeted owner/guard lane

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectStateServiceTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests|FullyQualifiedName~CalculationStateServiceGuardTests"
```

Result:

```text
Пройден!   : не пройдено     0, пройдено    39, пропущено     0, всего    39
```

Both legacy-store guard tests and calculation-state guard tests are now GREEN.

### Affected lifecycle lane

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~MainViewModelTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~CircuitsViewModelEventLeakTests|FullyQualifiedName~ResultsStabilizationPhase1|FullyQualifiedName~DoubleCalculationPreventionTests"
```

Result:

```text
Пройден!   : не пройдено     0, пройдено   100, пропущено     1, всего   101
```

### Persistence lane

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ProjectFileServiceAtomicityTests|FullyQualifiedName~ProjectFileServiceMutationTests"
```

Result:

```text
Пройден!   : не пройдено     0, пройдено    18, пропущено     0, всего    18
```

## Architecture checks

- `CalculationStateService` no longer has a local `IsLoadProjectInProgress`
  backing field; the legacy store-guard test passes.
- `ResultsViewModel` restores use the canonical `IProjectSession.BeginProjectRestore()`
  lease; the guard is cleared in `finally` semantics via `using` disposal.
- `SetPipeSpacing` source validation still rejects non-canonical writers and still
  permits `ProjectLoadOrchestrator.RestoreModules` only while the canonical guard
  is true.

## Next step

Task 7: register one singleton lifecycle graph and rewire only existing consumers.
Confirm DI resolves `IProjectSession`, legacy interfaces, `MainWindow`,
`MainViewModel`, `ResultsViewModel`, `ProjectLoadOrchestrator`, and
`CalculationStateService` to one canonical instance without circular dependencies
or transient adapters.
