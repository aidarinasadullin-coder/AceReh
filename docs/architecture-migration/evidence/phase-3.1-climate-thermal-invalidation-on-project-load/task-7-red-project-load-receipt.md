# Task 7 Project-Load RED Receipt

- Date: 2026-08-20
- Scope: approved Phase 3.1 Task 7 RED capture and independent full characterization reconciliation.
- Approved plan SHA-256: `355A81BD354EF3E3F0A4636C154DA27EB2C596FFA9F14BA4EBE1757FCAD4D0C9` (53357 UTF-8 bytes).

## Named RED Authority

- Test: `SnowMeltingCalculator.Tests.Services.Project.ClimateThermalInvalidationRegressionTests.ProjectLoad_DoesNotInvalidateRestoredThermalResult`
- Historical command: `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~ClimateThermalInvalidationRegressionTests.ProjectLoad_DoesNotInvalidateRestoredThermalResult" --logger "trx;LogFileName=phase-3.1-red-project-load.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"`
- Atlas TRX: `tests/SnowMeltingCalculator.Tests/TestResults/phase-3.1-red-project-load-atlas.trx`
- Discovery and execution: 1 of 1 NUnit test cases discovered and executed.
- Counters: `total=1, executed=1, passed=0, failed=1, NotExecuted=0`.
- Compilation, discovery, execution, real restore completion, and distinguishable saved-result validity all succeeded.
- Restore retained the named saved Thermal result: `PowerTotal=777.0`, `IsValid=true`.
- The RED contract mismatch is assertion-based: `fixture.CalculationStateService.ThermalNeedsRecalculation` observed `true` where the contract requires `false`, and `thermalStates` observed exactly one Thermal state event where the contract requires an empty collection.
- No compile, fixture, absent-result, discovery, or `NotExecuted` failure occurred.

## Full Characterization Matrix Authority

- Atlas TRX: `tests/SnowMeltingCalculator.Tests/TestResults/phase-3.1-task-7-characterization-atlas.trx`
- Discovery and execution: 76 of 76 NUnit test cases discovered and executed.
- Counters: `total=76, executed=76, passed=66, failed=10, NotExecuted=0`.
- The ten failures are factual contract mismatches, not infrastructure failures:
  1. `ChangedProjectLoadReset_SynchronizesWithoutThermalInvalidationOrDirty` - lifecycle compatibility/Thermal silence mismatch: observed one compatibility event and one Thermal state event instead of zero; the load/reset contract also requires clean dirty state.
  2. `RepeatedResetAndLoad_DoesNotMultiplyClimateOrThermalEvents` - repeated-cycle silence/dirty mismatch: observed compatibility events, Thermal state events, and `fixture.Session.IsDirty=true` where the contract requires no events and clean state.
  3. `ClimateViewModel_UserReset_ReturnsToDefaultsAndMarksDirty` - user-reset dirty semantics mismatch: `IMarkDirtyService.MarkDirty()` was not invoked.
  4. `NewCalculation_ChangedClimateReset_SynchronizesOnceWithoutCompatibilityThermalOrDirty` - public new-calculation lifecycle silence mismatch: observed one compatibility event and one Thermal state event instead of zero.
  5. `ChangedUserResetToCityData_InvalidatesThermalExactlyOnceAndMarksDirty` - user-reset dirty semantics mismatch: observed `fixture.Session.IsDirty=false` where the contract requires `true`.
  6. `ProjectLoad_DoesNotInvalidateRestoredThermalResult` - saved-result preservation mismatch: retained `PowerTotal=777.0` and `IsValid=true`, but observed `ThermalNeedsRecalculation=true` and exactly one Thermal state event.
  7. `ClimateMultiplicity_ChangedUserReset_EmitsOneCompletionAndCompatibilityUpdateAndMarksDirty` - user-reset dirty semantics mismatch: observed `MarkDirtyCalls=0` where the contract requires `1`.
  8. `ClimateMultiplicity_ChangedUserResetToCityData_EmitsOneCompletionAndCompatibilityUpdateAndMarksDirty` - user-reset dirty semantics mismatch: observed `MarkDirtyCalls=0` where the contract requires `1`.
  9. `ChangedUserResetToDefaults_InvalidatesThermalExactlyOnceAndMarksDirty` - user-reset dirty semantics mismatch: observed `fixture.Session.IsDirty=false` where the contract requires `true`.
  10. `ProjectLoadWithoutSavedThermalResult_CalculatesOnceWithoutClimateInvalidation` - no-saved-result extra invalidation/dirty mismatch: observed two compatibility events, three Thermal state events, and `fixture.Session.IsDirty=true`; the contract requires one calculation without climate invalidation and clean state.
- The matrix therefore establishes the requested factual categories: user-reset dirty semantics; lifecycle compatibility/Thermal silence; saved-result preservation; repeated-cycle silence/dirty; no-saved-result extra invalidation/dirty; and public new-calculation lifecycle silence.
- No compile, fixture, absent-result, discovery, or `NotExecuted` failure occurred in the characterization run; all ten failures reached their assertions after execution.

## Protected Baseline Integrity

- Baseline metadata: `docs/architecture-migration/evidence/phase-3.1-climate-thermal-invalidation-on-project-load/task-7-baseline/preimage-metadata.txt`.
- After both independent Atlas runs, all seven protected production/read-only hashes matched the baseline metadata exactly:
  - `src/Services/Project/ClimateMutationOrigin.cs` -> `A27791CB886335A56403E94A402FC49C5E2DC5583E833327DDC9E7AFF7FBA691`
  - `src/Services/Project/ProjectSessionClimateState.cs` -> `BCCF7A6D18DC3D08F1A7369EF3590E89E1932C56A6C82C250296B101E65D0FED`
  - `src/Models/Climate/ClimateData.cs` -> `ED5CDD0B88A92FFE3449AAEBA0A835C2FF9771B3D190D0D78E0FE9227740EB6D`
  - `src/Services/Project/ProjectLoadOrchestrator.cs` -> `4EE41EF1BFABA4D84B604063FA7366F32F625AA3D6BCD4CBCE3F63819F9B9549`
  - `src/ViewModels/Climate/ClimateViewModel.cs` -> `D14A7C69555169E1BEEE70EC44BF984F0F034651FB34888F364D59E4E4B9370A`
  - `src/ViewModels/Shell/MainViewModel.cs` -> `7EFC382D4C8CA8D962DD9CE98E8CF97010F439AECE54617AD4D5F3A8093AAD57`
  - `src/ViewModels/Thermal/ThermalViewModel.cs` (read-only reference) -> `27334159C03405747F7488116D23ED7FDF24F5769FC44F202C4B7622FF4411D2`

The prior receipt's stale source line references are intentionally removed. Current evidence uses assertion names and observed values from the Atlas TRX artifacts above. No production or test source files were changed by this evidence correction.
