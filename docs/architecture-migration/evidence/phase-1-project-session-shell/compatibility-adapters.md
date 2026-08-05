# Phase 1 Task 5 — Legacy project-state contracts as forwarding-only surfaces

## Scope

Convert `ProjectStateService` from a mutable lifecycle store into a
forwarding-only compatibility adapter over `IProjectSession`. All legacy
interfaces (`IProjectInfoService`, `IProjectStateService`, `IMarkDirtyService`)
must resolve to the canonical `ProjectSession` singleton, and the adapter must
hold no duplicate lifecycle state.

## Changed files

- `src/Services/Results/ProjectStateService.cs` — replaced mutable fields with a
  single `IProjectSession` reference; all properties/methods/events forward to
  the canonical session.

No other production files were changed. `CalculationStateService` still retains
its local `IsLoadProjectInProgress` auto-property; that is removed in Task 6.

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

### Legacy adapter / store-guard lane

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectStateServiceTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests"
```

Result:

```text
Пройдено 35, не пройдено 1, всего 36
```

The single failure is the expected `CalculationStateService` guard-field test,
which is addressed in Task 6. `ProjectStateService_HasNoMutableLifecycleBackingFields`
is now GREEN, proving the adapter contains no duplicate lifecycle store.

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

- `ProjectStateService` now has only one instance field: `_session` of type
  `IProjectSession`.
- `ProjectStateServiceTests` still pass; the class is retained as a compatibility
  adapter rather than deleted.
- DI resolves `IProjectInfoService`, `IProjectStateService`, and `IMarkDirtyService`
  to the same `ProjectSession` singleton.

## Known remaining RED item (by design)

- `CalculationStateService_HasNoLocalRestoreGuardBackingField` fails until Task 6
  moves the restore guard into `ProjectSession`.

## Next step

Task 6: move restore-guard storage to `ProjectSession` without changing
calculation semantics. `CalculationStateService.IsLoadProjectInProgress` becomes
a forwarding compatibility view with a single lease reference and no local
bool/depth field.
