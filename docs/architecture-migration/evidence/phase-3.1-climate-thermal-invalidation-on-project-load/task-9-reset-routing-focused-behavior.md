# Phase 3.1 Task 9 - Reset Routing and Focused Behavior

Date: 2026-08-20

## Scope

- Verified every approved Climate reset/load call-site classification.
- Reproduced and resolved the two deferred lifecycle clean-state test failures.
- Did not modify production code because current routing already matches the approved contract.
- Kept `src/ViewModels/Thermal/ThermalViewModel.cs` read-only.

## Routing Verification

- `ClimateViewModel.Reset()` uses `ClimateMutationOrigin.UserReset`.
- `ClimateViewModel.ResetToCityData()` uses `ClimateMutationOrigin.UserReset`.
- `ProjectLoadOrchestrator.ResetModules()` uses `ClimateMutationOrigin.ProjectLoadReset`.
- `MainViewModel.PerformNewCalculationReset()` uses `ClimateMutationOrigin.ProjectLoadReset`.
- `ProjectLoadOrchestrator.RestoreModulesFromProjectAsync()` uses silent `ClimateMutationOrigin.Load`.

## Failure Investigation

The initial focused Debug gate produced 74 passed, 2 failed, and 0 skipped. The failing tests were:

- `RepeatedResetAndLoad_DoesNotMultiplyClimateOrThermalEvents`
- `ProjectLoadWithoutSavedThermalResult_CalculatesOnceWithoutClimateInvalidation`

Both failed only on the final `Session.IsDirty == false` assertion. Stage instrumentation confirmed that the first cycle remained clean after `ResetModules()` and became dirty during restore-time module setters or fallback calculation. Compatibility publication and Climate-triggered Thermal invalidation remained zero.

The production public load graph already establishes the canonical restore boundary in `ResultsViewModel.LoadProjectDataAsync()` with `ProjectSession.BeginProjectRestore()` and calls `MarkClean()` after successful restore. The focused fixture modeled only the legacy `CalculationStateService.IsLoadProjectInProgress` guard. The fixture was corrected to model both real boundaries and successful-load clean completion. No assertion was skipped, removed, or weakened.

## Executable Gates

Focused filter:

`FullyQualifiedName~ClimateThermalInvalidationRegressionTests|FullyQualifiedName~ClimateStateTests|FullyQualifiedName~ClimateMultiplicityCharacterizationTests|FullyQualifiedName~ClimateDataProjectionTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~MainViewModelTests`

- Debug: 76 passed, 0 failed, 0 skipped.
- Release: 76 passed, 0 failed, 0 skipped.
- TRX reconciliation: zero `outcome="NotExecuted"` results in both focused receipts.
- Protected Thermal SHA-256: `27334159C03405747F7488116D23ED7FDF24F5769FC44F202C4B7622FF4411D2`.

TRX files:

- `tests/SnowMeltingCalculator.Tests/TestResults/phase-3.1-focused-debug.trx`
- `tests/SnowMeltingCalculator.Tests/TestResults/phase-3.1-focused-release.trx`

## Result

Task 9 is GREEN. Task 10 build, affected integration, and full Release gates are the next dependency-released action. Phase 3.1 remains executing and incomplete.
