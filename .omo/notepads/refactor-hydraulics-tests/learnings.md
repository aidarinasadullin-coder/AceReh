# T15 Hydraulics Integration Tests — Refactor Learnings

## Summary

Fixed 23 failing integration tests in `SnowMeltingCalculator.Tests.IntegrationTests.Hydraulics` after the T15 refactor decoupled `CircuitsViewModel` from direct `ThermalViewModel`/`ClimateViewModel` subscriptions. All tests now exercise the new `CalculationContext` / `ICalculationStateService` contracts.

## Production changes (minimal)

1. **`src/ViewModels/Hydraulics/CircuitsViewModel.cs`**
   - `Calculate()` now updates `CircuitRow.PipeSpacing_cm` for every circuit in every collector when the canonical pipe spacing changes. This restores the intended synchronization between `ThermalViewModel.PipeSpacing` and circuit rows.
   - `SupplyTemperature` computed property now prefers the cached thermal result (`_lastThermalResult?.SupplyTemperature`) before falling back to `ThermalInputs`, matching how `Calculate()` consumes supply temperature.
   - `UpdateFromThermalModule()` clears `_lastThermalResult` when the provided result is `null` or invalid, so the VM truly resets to documented fallback defaults instead of retaining stale values.

## Test changes

### All four hydraulics integration test fixtures

- Configured `_calculationStateServiceMock` in `SetUp` to back `PipeSpacing` with a local variable (the interface property is read-only) and raise `PipeSpacingChanged(sender, spacing)` from both `SetPipeSpacing` overloads. This lets `_thermalViewModel.PipeSpacing = value` flow to `_viewModel` under the T15 contract.

### `ClimateToHydraulicsIntegrationTests`

- `ClimateAndThermalChanges_BothUpdateInputData` now pushes the thermal result through `_calculationContext.UpdateThermalInputs` / `UpdateThermal` instead of relying on the removed `ThermalViewModel.PropertyChanged` subscription.
- `OnClimatePropertyChanged_WhenOtherPropertyChanged_DoesNotTriggerCalculate` was renamed to `OnClimatePropertyChanged_WhenHumidityChanged_TriggersCalculate` and now asserts that `Calculate` **is** called, because all climate changes now flow through `CalculationContext`.

### `DoubleCalculationPreventionTests`

- `UpdateFromThermalModule_TriggersSingleCalculate` now calls `_viewModel.UpdateFromThermalModule(...)` directly.
- `SequentialThermalChanges_TriggersSeparateCalculates` now publishes each thermal result into `CalculationContext`, so each triggers a separate `Calculate`.
- `FullWorkflow_ThermalClimateGlycol_TriggersCorrectNumberOfCalculates` updated expected `GetProperties` count from 4 to 8 (4 `Calculate` calls × 2 glycol lookups each), reflecting that climate changes now always recalculate and reset the thermal result.

### `ThermalToHydraulicsIntegrationTests`

- Tests named `UpdateFromThermalModule_*` now call the public `UpdateFromThermalModule` method.
- Pipe data is seeded into `CalculationContext.ThermalInputs` where `InnerDiameter` assertions are needed, because `UpdateFromThermalModule` reads pipe info from the context.
- Reset tests (`WhenResultIsNull`, `WhenResultInvalid`) now assert the documented fallback defaults (`PowerUp = 180.0`, `PowerDown = 80.0`, `SupplyTemperature = 50.0`, `ReturnTemperature = 30.0`) after the production fix to clear the cached invalid result.

## Verification

- `dotnet test --filter "FullyQualifiedName~SnowMeltingCalculator.Tests.IntegrationTests.Hydraulics"` — 65/65 passed.
- `dotnet test` — 1076/1076 passed, no regressions.
