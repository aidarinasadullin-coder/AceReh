# Phase 2 Task 6 - ClimateViewModel Adapter Evidence

Date: 2026-08-07

## Scope and files

- `src/ViewModels/Climate/ClimateViewModel.cs`: public DI constructor now receives `IProjectSession`; user city/scalar/high-requirements/reset mutations cross `ClimateState`; `Changed` snapshots are mirrored under `_isMirroringClimateState`.
- `src/Services/Project/ProjectSession.cs`, `ProjectSessionClimateState.cs`, and `ClimateStateSnapshot.cs`: the canonical owner retains UI-compatible defaults/table 1.6 behavior and performs the one compatibility projection/context publication after a changed mutation.
- `tests/SnowMeltingCalculator.Tests/Climate/ClimateViewModelTests.cs`: canonical snapshot, same-value, and repeated-reset adapter assertions.
- `tests/SnowMeltingCalculator.Tests/Climate/ClimateMultiplicityCharacterizationTests.cs`: target counts updated from duplicate legacy cascades to one canonical completion, and repeated reset to zero.
- `tests/SnowMeltingCalculator.Tests/Climate/ClimateStateLegacyStoreGuardTests.cs`: rejects direct dirty/context calls in `ClimateViewModel`; Task 7 restore bypass inventory remains unchanged.
- `tests/SnowMeltingCalculator.Tests/Services/Project/ClimateStateTests.cs`: canonical defaults/table 1.6 expectations aligned with the stable WPF behavior.

## Behavior

- City selection, scalar edits, and high requirements use `ClimateMutationOrigin.User`; dirty semantics remain owned by `ProjectSessionClimateState`.
- UI reset uses `ClimateMutationOrigin.Reset`; reset-to-city-data remains a non-user reset.
- A changed canonical snapshot is projected once to `ClimateData`, published once to `CalculationContext`, then mirrored into observable VM properties with handler suppression.
- `ClimateViewModel` contains no `MarkDirty`, `UpdateClimate`, `_markDirtyService`, or `_calculationContext` reference.
- Same-value UI setters and repeated reset produce no canonical completion, compatibility projection, or context update.

## Commands and results

```powershell
dotnet build "src\SnowMeltingCalculator.csproj" -c Debug
# PASS: 0 warnings, 0 errors

dotnet test "tests\SnowMeltingCalculator.Tests" --filter "FullyQualifiedName~ClimateViewModelTests"
# PASS: 24 passed, 0 failed, 0 skipped

dotnet test "tests\SnowMeltingCalculator.Tests" --filter "FullyQualifiedName~ClimateStateTests|FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ClimateViewModelTests|FullyQualifiedName~ClimateMultiplicity|FullyQualifiedName~ClimateStateLegacyStoreGuard|FullyQualifiedName~ThermalViewModelTests.ClimateDataChanged|FullyQualifiedName~ClimateToHydraulicsIntegrationTests"
# PASS: 98 passed, 0 failed, 0 skipped
```

`lsp_diagnostics` was attempted for each changed C# production file and returned the known harness error `LSP file path must be inside request cwd`. The executable C# gates are the build and tests above.

## Scope confirmation

Task 7 not started. `ProjectLoadOrchestrator`, `MainViewModel`, `ResultsViewModel`, persistence/restore routes, `.smc` schema, maps/widget, Phase 1 docs, packages, installer, and publish artifacts were not edited.
