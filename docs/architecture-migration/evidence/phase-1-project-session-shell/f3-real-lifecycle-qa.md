# F3 — Real Agent-Executed Lifecycle QA

## Scope

Independent Phase 1 F3 lifecycle QA for the narrow `ProjectSession` shell.
Verify the real NUnit/WPF test harness proves every required lifecycle
scenario through the production DI graph and public seams.

## Required scenarios and test mapping

| F3 scenario | Covering test(s) | Test class |
|---|---|---|
| new/dirty decisions | `NewCalculation_WhenClean_DoesNotShowDialog_AndResets`, `NewCalculation_WhenDirtyAndCancel_DoesNotReset`, `NewCalculation_WhenDirtyAndNo_Resets`, `NewCalculation_WhenDirtyAndYesAndSaveSucceeds_Resets`, `NewCalculation_WhenDirtyAndYesButSaveCancelled_DoesNotReset`, `Closing_WhenDirtyAndCancel_SetsCancelTrue`, `Closing_WhenDirtyAndNo_DoesNotCancel`, `Closing_WhenDirtyAndYes_SetsCancelTrueAndReinvokesClose`, `WindowTitle_DirtyNoPath`, `WindowTitle_DirtyWithPath`, `WindowTitle_CleanWithPath`, `WindowTitle_CleanNoPath` | `MainViewModelTests` |
| open-project dirty decisions | `OpenProject_WhenDirty_ShowsReplacePrompt`, `OpenProject_WhenClean_DoesNotShowPrompt`, `OpenProject_WhenDirtyAndUserPicksNo_DoesNotLoad` | `ResultsViewModelOpenProjectTests` |
| save failure | `SaveProjectResultAsync_OnIoFailure_ReturnsFailureWithMessage`, `SaveProjectAsync_IsAtomic_OriginalIntactOnWriteFailure`, `SaveProjectAsync_TempFileCleanedUpOnFailure`, `NewCalculation_WhenDirtyAndYesButSaveCancelled_DoesNotReset` | `ProjectFileServiceResultTests`, `ProjectFileServiceAtomicityTests`, `MainViewModelTests` |
| v1.0 then v1.1 second load | `Load_v1_Fixture_PreservesCanonicalFields`, `SaveThenLoad_NewProject_RoundTripsFields`, `ProjectFileService_RoundTripPreservesSchemaVersionAndJsonShape`, `LoadProjectDataAsync_TwiceOnSingletonGraph_ReplacesIdentityWithoutStaleState` | `ProjectRoundTripTests`, `ResultsViewModelOpenProjectTests`, `ProjectLifecycleFlowCharacterizationTests` |
| repeated reset/load | `RepeatedResetCycles_DoNotDuplicateCircuitsEventSubscriptions`, `ClimateViewModel_Reset_*`, `ConstructionViewModel_Reset_*`, `ThermalViewModel_Reset_*`, `CircuitsViewModel_Reset_*`, `ResultsViewModel_Reset_*` | `ProjectLifecycleFlowCharacterizationTests`, `ResetOrchestrationTests` |
| one post-load edit | `LoadProjectDataAsync_ThenEdit_MarksDirtyThroughExistingStateService` | `ProjectLifecycleFlowCharacterizationTests` |
| corrupt file | `LoadProjectResultAsync_OnCorruptJson_ReturnsFailureWithDeserializationError` | `ProjectFileServiceResultTests` |
| injected early restore failure | `LoadProjectDataAsync_EarlyRestoreFailure_LeavesPartialStateAndClearsGuard` | `ProjectLifecycleFlowCharacterizationTests` |
| injected late restore failure | `LoadProjectDataAsync_LateRestoreFailure_LeavesPartialStateAndClearsGuard` | `ProjectLifecycleFlowCharacterizationTests` |
| exact final state/events/recalculations | `InitialState_HasExpectedDefaults`, `ProjectNumber_Setter_RaisesPropertyChanged_Once`, `ProjectNumber_Setter_DoesNotRaisePropertyChanged_WhenValueUnchanged`, `MarkDirty_SetsIsDirtyTrue_And_RaisesPropertyChanged_Once`, `BeginProjectRestore_*`, `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`, `ProjectRoundTrip_DoesNotMarkDirtyOnLoad`, `ResultsViewModel_LoadProjectData_RestoresCityAndClimateParameters` | `ProjectSessionTests`, `ProjectLifecycleFlowCharacterizationTests`, `ResultsViewModelOpenProjectTests` |
| nested restore lease regression | `BeginProjectRestore_NestedLeases_DisposeInnerThenOuter_PreservesGuardUntilFinalExit`, `BeginProjectRestore_NestedLeases_DisposeOuterThenInner_PreservesGuardUntilFinalExit`, `BeginProjectRestore_NestedLeases_FinalExitSubscriberThrows_ClearsGuardAndKeepsLeasesIdempotent` | `ProjectSessionTests` |

## Freshly executed commands and results

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
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests" --no-build
```

Result:

```text
Пройден!   : не пройдено     0, пройдено     6, пропущено     0, всего     6, длительность 131 ms.
```

### Targeted owner/guard/flow lane

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectStateServiceTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests|FullyQualifiedName~CalculationStateServiceGuardTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests" --no-build
```

Result:

```text
Пройден!   : не пройдено     0, пройдено    49, пропущено     0, всего    49, длительность 201 ms.
```

### Affected lifecycle lane

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~MainViewModelTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~CircuitsViewModelEventLeakTests|FullyQualifiedName~ResultsStabilizationPhase1|FullyQualifiedName~DoubleCalculationPreventionTests" --no-build
```

Result:

```text
  Пропущен ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile [8 ms]
Пройден!   : не пройдено     0, пройдено   100, пропущено     1, всего   101, длительность 15 s.
```

The single skipped test is pre-existing and not one of the required F3 scenarios.

### Persistence lane

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ProjectFileServiceAtomicityTests|FullyQualifiedName~ProjectFileServiceMutationTests" --no-build
```

Result:

```text
Пройден!   : не пройдено     0, пройдено    18, пропущено     0, всего    18, длительность 219 ms.
```

### Full Release test gate

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release --no-build
```

Result:

```text
  Пропущен RegenerateCircuitsBaseline [< 1 ms]
  Пропущен RegenerateBaseline [< 1 ms]
  Пропущен ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile [13 ms]
Пройден!   : не пройдено     0, пройдено  1568, пропущено     1, всего  1569, длительность 39 s.
```

The single skipped test in the Release gate is the same pre-existing
`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`; the other
two skipped names are baseline-regeneration helpers that do not run in normal
execution and are also unrelated to F3.

## Scenario-specific findings

- **new/dirty decisions**: `MainViewModelTests` and `ResultsViewModelOpenProjectTests`
  prove clean/dirty branching for NewCalculation, OpenProject, window closing,
  and title rendering. All pass.
- **save failure**: `ProjectFileServiceResultTests.SaveProjectResultAsync_OnIoFailure_ReturnsFailureWithMessage`,
  `ProjectFileServiceAtomicityTests.SaveProjectAsync_IsAtomic_OriginalIntactOnWriteFailure`,
  `ProjectFileServiceAtomicityTests.SaveProjectAsync_TempFileCleanedUpOnFailure`,
  and `MainViewModelTests.NewCalculation_WhenDirtyAndYesButSaveCancelled_DoesNotReset`
  cover I/O failure, atomicity, temp cleanup, and user cancellation. All pass.
- **v1.0 then v1.1 second load**: `ProjectRoundTripTests.Load_v1_Fixture_PreservesCanonicalFields`
  and `SaveThenLoad_NewProject_RoundTripsFields` exercise v1.0 schema.
  `ResultsViewModelOpenProjectTests.ProjectFileService_RoundTripPreservesSchemaVersionAndJsonShape`
  exercises v1.1 schema.
  `ProjectLifecycleFlowCharacterizationTests.LoadProjectDataAsync_TwiceOnSingletonGraph_ReplacesIdentityWithoutStaleState`
  proves second-load identity replacement. All pass.
- **repeated reset/load**: `ProjectLifecycleFlowCharacterizationTests.RepeatedResetCycles_DoNotDuplicateCircuitsEventSubscriptions`
  and the `ResetOrchestrationTests` module resets prove event-subscription
  hygiene and default restoration. All pass.
- **one post-load edit**: `ProjectLifecycleFlowCharacterizationTests.LoadProjectDataAsync_ThenEdit_MarksDirtyThroughExistingStateService`
  proves dirtiness propagates through the canonical session after a load. Pass.
- **corrupt file**: `ProjectFileServiceResultTests.LoadProjectResultAsync_OnCorruptJson_ReturnsFailureWithDeserializationError`
  proves graceful deserialization failure. Pass.
- **injected early/late restore failure**: `ProjectLifecycleFlowCharacterizationTests.LoadProjectDataAsync_EarlyRestoreFailure_LeavesPartialStateAndClearsGuard`
  and `LoadProjectDataAsync_LateRestoreFailure_LeavesPartialStateAndClearsGuard`
  prove partial-state preservation, guard clearance, and no rollback. Both pass.
- **exact final state/events/recalculations**: `ProjectSessionTests` counts events
  and verifies idempotency; `ProjectLifecycleFlowCharacterizationTests` asserts
  exact final state after success/failure flows; `ResultsViewModelOpenProjectTests`
  locks recalculation avoidance (`ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`)
  and clean load state. All pass.
- **nested restore lease regression**: `ProjectSessionTests.BeginProjectRestore_NestedLeases_*`
  prove distinct leases, inner-then-outer and outer-then-inner disposal, repeated
  disposal no-op, and exception-safe final exit. All pass. Source inspection
  confirms the current implementation has no shared `_currentLease` and returns a
  fresh `ProjectRestoreLease(this)` per successful `BeginProjectRestore()` call.

## Conclusion

All named F3 scenarios are covered by freshly executed real test-harness lanes.
No required scenario was skipped. Debug/Release production builds are clean.
The only skipped test is a pre-existing unrelated case.

VERDICT: APPROVE
