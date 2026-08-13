# Phase 2 Task 9 - Downstream Invalidation

## Scope

- Production: `src/ViewModels/Climate/ClimateViewModel.cs`.
- Tests: `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs` and `tests/SnowMeltingCalculator.Tests/Climate/ClimateStateLegacyStoreGuardTests.cs`.
- Evidence: this receipt and the append-only Phase 2 learnings notepad.

## Behavioral Finding

`ProjectSessionClimateState.CompleteMutation()` is the authoritative completion
sequence for a changed Climate snapshot:

1. `ClimateData.ApplyProjection()` raises one `ClimateData.DataChanged` event,
   which invalidates `ThermalViewModel`.
2. `CalculationContext.UpdateClimate()` raises one `ContextChanged(Climate)`
   event, which makes `CircuitsViewModel` recalculate once.

The new canonical integration assertion creates real `ProjectSession`,
`ClimateData`, `CalculationContext`, `ThermalViewModel`, and
`CircuitsViewModel` consumers. One `ApplyIndividualEdit(..., User)` produced
exactly one projection event, exactly one `CalculationContext.Climate`
publication, and two glycol-property requests, which is one Circuits pass for
its operating and design temperatures.

No-op mutations remain suppressed by `ProjectSessionClimateState` before this
sequence. Load and reset continue to use non-user origins, so they retain
compatibility projection/context updates without `MarkDirty`.

The public, unused `ClimateViewModel.SyncToClimateData()` bridge was removed.
It could independently publish another projection event outside canonical
completion. The legacy-store guard now forbids that bridge from returning.

## Source Scan Notes

- Production search found no caller of `SyncToClimateData()` before removal.
- `SyncToClimateData|UpdateClimate\(` in `src` found only
  `CalculationContext.UpdateClimate` declaration and
  `_calculationContext?.UpdateClimate(_climateData, "Climate")` in
  `ProjectSessionClimateState.cs`.
- `ApplyProjection\(` in `src` found only `ClimateData.ApplyProjection`
  declaration and `_climateData.ApplyProjection(newSnapshot, isValid)` in
  `ProjectSessionClimateState.cs`.
- `ThermalViewModel` is the ClimateData projection-event consumer; it clears an
  existing thermal result and marks thermal recalculation needed.
- `CircuitsViewModel` consumes `CalculationContext.ContextChanged`; its
  `Climate` branch calls `UpdateFromClimateModule()` once. Its own context
  publications remain ignored by the existing `Source == "CircuitsViewModel"`
  guard.
- No Thermal or Circuits ownership, formulas, persistence wire format, Results
  ownership, DI registrations, maps, widget, or Task 10 artifacts changed.

## Verification

| Command | Result |
| --- | --- |
| `dotnet build "src\SnowMeltingCalculator.csproj" -c Debug` | PASS: 0 warnings, 0 errors |
| `dotnet test "tests\SnowMeltingCalculator.Tests" --filter "FullyQualifiedName~DoubleCalculationPreventionTests.CanonicalClimateMutation_WithThermalAndCircuitsConsumers_PublishesOneProjectionAndOneDownstreamUpdate" -c Debug` | PASS: 1 passed, 0 failed, 0 skipped |
| `dotnet test "tests\SnowMeltingCalculator.Tests" --filter "FullyQualifiedName~ClimateToHydraulicsIntegrationTests\|FullyQualifiedName~DoubleCalculationPreventionTests\|FullyQualifiedName~ThermalViewModelTests" -c Debug` | PASS: 68 passed, 0 failed, 0 skipped |
| `dotnet test "tests\SnowMeltingCalculator.Tests" --filter "FullyQualifiedName~ClimateToHydraulicsIntegrationTests\|FullyQualifiedName~DoubleCalculationPreventionTests\|FullyQualifiedName~ThermalViewModelTests\|FullyQualifiedName~ClimateStateLegacyStoreGuardTests" -c Debug` | PASS: 71 passed, 0 failed, 0 skipped |

Scoped `git diff --check` for Task 9 files plus plan/context produced only known
LF-to-CRLF warnings and no whitespace errors.

`lsp_diagnostics` was attempted for every changed C# file and hit the known
external harness error: `LSP file path must be inside request cwd`. The .NET
build and test gates are the executable correctness authority.

Task 10 was not started.
