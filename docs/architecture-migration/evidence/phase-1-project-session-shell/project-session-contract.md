# Phase 1 Task 4 — Canonical ProjectSession lifecycle owner

## Scope

Implement the minimal `IProjectSession`/`ProjectSession` lifecycle aggregate root
exactly as specified in the approved Phase 1 plan. Legacy interfaces
(`IProjectInfoService`, `IProjectStateService`, `IMarkDirtyService`) resolve to
the same singleton instance.

## Changed files

- `src/Services/Project/IProjectSession.cs` — new lifecycle-only contract.
- `src/Services/Project/ProjectSession.cs` — canonical implementation.
- `src/Configuration/ServiceCollectionExtensions.cs` — DI registration for the
  singleton lifecycle graph.
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`
  — test helper signature fix only (`IProjectStateService` -> `ProjectStateService`
  so the helper can satisfy both `IProjectStateService` and `IMarkDirtyService`
  consumers during the transitional contract surface).

No files under `src/` other than the three above were edited. Existing legacy
`ProjectStateService` and `CalculationStateService` implementations remain
untouched; their duplication will be removed in Tasks 5 and 6.

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

### Task 2/4 targeted test lane

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectStateServiceTests|FullyQualifiedName~CalculationStateServiceGuardTests"
```

Result:

```text
Пройден!   : не пройдено     0, пройдено    37, пропущено     0, всего    37
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

## Contract checks verified by tests

- Initial state: empty identities, null path, clean, guard false.
- `ProjectNumber`/`ProjectObject` reject `null` with `ArgumentNullException`, no
  mutation, no event.
- String setters raise exactly one `PropertyChanged` per real mutation and zero
  events for equal assignments (ordinal comparison).
- `MarkDirty`/`MarkClean` are idempotent and raise exactly one event per real
  transition.
- `CurrentFilePath` accepts `null` and verbatim non-null strings.
- `BeginProjectRestore()` returns a lease; nested entries do not raise events;
  final disposal clears the guard and raises one event; double-dispose is safe.
- DI resolves `IProjectSession`, `IProjectInfoService`, `IProjectStateService`,
  and `IMarkDirtyService` to the same canonical singleton instance.

## Known remaining RED items (by design)

- `ProjectSessionLegacyStoreGuardTests.cs` still fails because
  `ProjectStateService` and `CalculationStateService` retain legacy mutable
  lifecycle fields. These are removed in Tasks 5 and 6.
- `ProjectLifecycleFlowCharacterizationTests.cs` still fails because production
  restore flow has not yet been migrated to `BeginProjectRestore()` and the
  guard is not yet forwarded through `ICalculationStateService`. This is covered
  in Tasks 6 and 8.

## Next step

Task 5: convert legacy project-state contracts to forwarding-only compatibility
surfaces over `ProjectSession` and remove duplicate mutable lifecycle storage
from `ProjectStateService`.
