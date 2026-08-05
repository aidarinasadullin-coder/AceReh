# Phase 1 Task 8 — Preserve lifecycle orchestration, partial-failure semantics, and `.smc` compatibility

## Scope

Verify that the new `ProjectSession` owner does not change observable behavior
for new/open/save/close/reset flows, that partial restore failures keep their
characterized behavior (no rollback), and that the supported `.smc` corpus still
loads and round-trips.

## Changed files

No production files were changed for Task 8. The `ProjectLifecycleFlowCharacterizationTests`
were already written in Task 3 and now pass against the implementation from
Tasks 4-7. Test helpers were adjusted in Task 6 to share the canonical
`IProjectSession` between `ResultsViewModel` and `CalculationStateService`.

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

### Lifecycle characterization tests

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests"
```

Result:

```text
Пройден!   : не пройдено     0, пройдено     6, пропущено     0, всего     6
```

### Targeted owner/guard/flow lane

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectStateServiceTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests|FullyQualifiedName~CalculationStateServiceGuardTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests"
```

Result:

```text
Пройден!   : не пройдено     0, пройдено    46, пропущено     0, всего    46
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

### Full Release test gate

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release
```

Result:

```text
Пройден!   : не пройдено     0, пройдено  1565, пропущено     1, всего  1566
```

## Behavior checks

- Lifecycle characterization tests prove: successful load clears the guard;
  second load replaces identity without stale state; one edit after load marks
  dirty; repeated reset/load cycles do not duplicate event subscriptions;
  injected early/late restore failures leave partial state and clear the guard
  with no rollback.
- Full Release test suite passes with only the pre-existing skipped test
  `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.

## Next step

Task 9: run full gates and prove scope/single-owner invariants. This includes
Codegraph/LSP inspection to confirm one lifecycle store, unchanged
`CalculationContext` contract, no module slices in `ProjectSession`, no new
application-service → concrete-ViewModel edge, and a dirty-worktree comparison.
