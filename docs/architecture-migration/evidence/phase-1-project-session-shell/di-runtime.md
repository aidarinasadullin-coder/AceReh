# Phase 1 Task 7 — DI singleton lifecycle graph and consumer rewire

## Scope

Register `ProjectSession` once as the singleton lifecycle aggregate and verify
that every alias and consumer receives the same canonical instance. No new
coordinator or module ownership was introduced; only existing consumers were
wired to the canonical owner.

## Changed files

- `src/Configuration/ServiceCollectionExtensions.cs` — `ProjectSession` is
  registered as singleton and mapped to `IProjectSession`, `IProjectInfoService`,
  `IProjectStateService`, and `IMarkDirtyService`.
- `src/ViewModels/Results/ResultsViewModel.cs` — constructor now accepts
  `IProjectSession` so DI can inject the canonical instance.
- `src/Services/Navigation/CalculationStateService.cs` — constructor accepts
  `IProjectSession` so DI can inject the canonical instance.
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs`
  — added `DependencyInjection_LifecycleConsumersShareCanonicalSession` to prove
  `ResultsViewModel` and `CalculationStateService` receive the same
  `IProjectSession` instance that resolves from `AddApplicationServices`.

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

### DI registration test

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~DiRegistrationTests"
```

Result:

```text
Пройден!   : не пройдено     0, пройдено     8, пропущено     0, всего     8
```

### Targeted owner/guard lane

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectStateServiceTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests|FullyQualifiedName~CalculationStateServiceGuardTests"
```

Result:

```text
Пройден!   : не пройдено     0, пройдено    40, пропущено     0, всего    40
```

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

- `IProjectSession`, `IProjectInfoService`, `IProjectStateService`, and
  `IMarkDirtyService` all resolve to the same singleton `ProjectSession`.
- `ResultsViewModel` and `CalculationStateService` each receive the same
  canonical `IProjectSession` instance through DI.
- No circular dependencies, no transient/scoped lifecycle adapter, and no
  constructor gained module-state ownership or orchestration responsibilities.

## Next step

Task 8: preserve lifecycle orchestration, partial-failure semantics, and `.smc`
compatibility through the new owner. The remaining RED characterization tests
(`ProjectLifecycleFlowCharacterizationTests`) must be turned GREEN by verifying
that load A → load B, repeated reset/load cycles, one edit after load, and
injected early/late restore failures all match the characterized behavior.
