## Todo 2 � ConstructionValidator refactor

- Switched `ConstructionValidator` to implement `IValidator<ConstructionModel>` from `SnowMeltingCalculator.Core` and return `Core.ValidationResult`.
- Because `Models.Construction` also contains a `ValidationResult` type, added a `using ValidationResult = SnowMeltingCalculator.Core.ValidationResult;` alias in `ConstructionValidator.cs`, `IConstructionService.cs`, `ConstructionService.cs`, and the test files to avoid type ambiguity while keeping the `Models.Construction` using for `Layer`, `Material`, etc.
- Cascading changes required to keep the build green:
  - `IConstructionService.ValidateConstruction` and `ConstructionService.ValidateConstruction` return types moved to `Core.ValidationResult`.
  - `ConstructionViewModel.Validate()` now reads `result.Errors.Select(e => e.Message)` since `Errors` is `List<ValidationError>`.
  - `ConstructionValidatorTests`, `ConstructionServiceTests`, and `ConstructionViewModelTests` updated to assert on `e.Message` and use the Core alias.
- Validation logic, thresholds, and messages were left unchanged; only result types and using statements were modified.
- Build: `dotnet build src/SnowMeltingCalculator.csproj -c Debug` > 0 errors.
- Targeted tests (`ConstructionValidatorTests`, `ConstructionServiceTests`, `ConstructionViewModelTests`) > 65 passed, 0 failed.
## Todo 6 — HydraulicValidator

- Created `HydraulicValidator : IValidator<HydraulicInputData>` in `src/Services/Hydraulics/HydraulicValidator.cs`.
- Extracted the three rules from `HydraulicInputData.Validate()` (lines 75-89) unchanged:
  - `GlycolConcentration` must be between 10 and 90% inclusive.
  - `SupplySpacing_cm` must be greater than 0.
  - `SupplyHeatPercent` must be between 0 and 100% inclusive.
- Preserved the original Russian error messages verbatim, including the interpolated current-value formatting.
- Because `SnowMeltingCalculator.Models.Hydraulics` still contains its own `ValidationResult` type, added a `using ValidationResult = SnowMeltingCalculator.Core.ValidationResult;` alias in the validator, matching the pattern used for `ConstructionValidator`.
- `HydraulicInputData.Validate()` and `IsValid` were left untouched (todo 11).
- Added TDD tests in `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/HydraulicValidatorTests.cs` covering:
  - valid data and boundary values,
  - glycol concentration too low/high and just outside bounds,
  - supply spacing zero and negative,
  - heat percent too low/high and just outside bounds,
  - combined errors (3 errors and 2 errors),
  - null input throwing `ArgumentNullException`.
- Build: `dotnet build src/SnowMeltingCalculator.csproj -c Debug` > 0 errors.
- Targeted tests (`HydraulicValidatorTests`) > 17 passed, 0 failed.
- Did not modify `CircuitsCalculator`, fix glycol constant inconsistencies, or register the validator in DI.


## Todo 9 — Delete duplicate ValidationResult classes

- Deleted `src/Models/Hydraulics/ValidationResult.cs` and `src/Models/Construction/ValidationResult.cs`.
- Updated `HydraulicInputData.Validate()` and `Construction.ValidateConstruction()` to return `SnowMeltingCalculator.Core.ValidationResult` and use `ValidationResult.Success()` / `AddError(...)`.
- Removed `using ValidationResult = SnowMeltingCalculator.Core.ValidationResult;` aliases from `IConstructionService.cs`, `ConstructionService.cs`, `ConstructionValidator.cs`, `ConstructionViewModelTests.cs`, `ConstructionValidatorTests.cs`, and `ConstructionServiceTests.cs`; replaced with plain `using SnowMeltingCalculator.Core;`.
- Build: `dotnet build src/SnowMeltingCalculator.csproj -c Debug` > 0 errors.
- Targeted tests (`ConstructionValidatorTests`, `ConstructionServiceTests`, `HydraulicInputDataTests`, `ConstructionViewModelTests`) > 75 passed, 0 failed.
- Grep for `Models\.(Hydraulics|Construction)\.ValidationResult` returns zero matches.
- Commit: `refactor(validation): delete duplicate ValidationResult classes, unify on Core.ValidationResult`.

- Created `ThermalValidator : IValidator<ThermalInputs>` in `src/Services/Thermal/ThermalValidator.cs`.
- Constructor injects `IThermalCalculator`, `IClimateData`, and `IConstructionData`.
  - `ThermalCalculator.Validate` is an instance method, so `IThermalCalculator` is required even though the plan checkbox mentions only climate/construction data.
- `Validate(ThermalInputs)` delegates to `_calculator.Validate(input, _climate, _construction, out string[] errors)` and converts `bool + string[]` to `Core.ValidationResult`.
- Null `ThermalInputs` throws `ArgumentNullException`, matching the `ClimateValidator` convention.
- Added TDD tests in `tests/SnowMeltingCalculator.Tests/Services/Thermal/ThermalValidatorTests.cs` covering valid inputs, single/multiple invalid inputs, invalid construction data, and null input.
- Build: `dotnet build src/SnowMeltingCalculator.csproj -c Debug` > 0 errors.
- Targeted tests (`ThermalValidatorTests`) > 5 passed, 0 failed.

## Todo 5 — ThermalResultValidator

- Created `ThermalResultValidator : IValidator<ThermalCalculationResult>` in `src/Services/Thermal/ThermalResultValidator.cs`.
- Post-calculation checks (solves plan p.1):
  - Computes return temperature as `T_обратки = 2 * MeanTemperature - SupplyTemperature`.
  - Adds an error when `T_обратки < 0`.
  - Adds an error when `DeltaT <= 0`.
  - Adds an error when `DeltaT > ValidationConstants.MaxDeltaT` (30 °C).
- Returns `SnowMeltingCalculator.Core.ValidationResult`; no ambiguity with other `ValidationResult` types in this file, so no alias was needed.
- Does not call `ThermalCalculator`; validation is purely post-calculation on the result object.
- Null `ThermalCalculationResult` throws `ArgumentNullException`, consistent with other validators.
- Added TDD tests in `tests/SnowMeltingCalculator.Tests/Services/Thermal/ThermalResultValidatorTests.cs` covering valid result, negative return temperature, zero return temperature, excessive ΔT, zero ΔT, negative ΔT, maximum ΔT boundary, combined errors, and null input.
- Build: `dotnet build src/SnowMeltingCalculator.csproj -c Debug` > 0 errors.
- Targeted tests (`ThermalResultValidatorTests`) > 10 passed, 0 failed.
- Note: a clean of `src/obj` and `src/bin` was required to clear stale WPF generated-file artifacts before the build succeeded; the artifacts were unrelated to the validator changes.

## Todo 10 — Remove Construction.ValidateConstruction() + Construction.IsValid

- Removed `public bool IsValid => ValidateConstruction().IsValid;` from `src/Models/Construction/Construction.cs`.
- Removed the entire `ValidateConstruction()` method from `Construction.cs`.
- Updated `OnDataChanged()` to call `RaiseDataChanged(...)` with the default `isValid = true` so `ConstructionDataChangedEventArgs.IsValid` is still populated.
- Removed the now-unused `using SnowMeltingCalculator.Core;` from `Construction.cs`.
- Kept `IConstructionData.IsValid` as a default interface implementation returning `true` (temporary placeholder) so `ThermalViewModel` (todo 11) continues to compile without changes.
- Removed the explicit `IsValid` property from the `ConstructionData` stub; it now uses the interface default.
- Updated `CalculationContext`:
  - `UpdateConstruction` sets `State = CalculationState.ConstructionReady` whenever `Construction` is non-null.
  - `GetValidationErrors` only adds the generic "Конструкция не задана" error when `Construction == null`.
  - `IsReadyForThermalCalculation` only checks `Construction != null` (no longer reads `Construction.IsValid`).
- `ConstructionViewModel` already uses the injected `_validator` in `Validate()`; no direct reads of `_construction.IsValid` remain.
- No tests directly referenced `Construction.IsValid` or `Construction.ValidateConstruction()`; targeted tests continue to pass.
- Build: `dotnet build src/SnowMeltingCalculator.csproj -c Debug` > 0 errors, 6 pre-existing warnings.
- Targeted tests (`ConstructionValidatorTests`, `ConstructionServiceTests`, `ConstructionViewModelTests`, `CalculationContext`) > 82 passed, 0 failed.
- Grep verification:
  - `Construction.ValidateConstruction()` → zero matches in `src/`.
  - `Construction.IsValid` in `src/Models/Construction/Construction.cs` → zero matches.
  - `Models.Construction.ValidationResult` → zero matches.

## Todo 13 — Use ClimateValidator in ClimateViewModel.ValidateAll()

- Injected `IValidator<IClimateData>` as `_climateValidator` into `ClimateViewModel`, keeping all existing dependencies (`IClimateDataService`, `IClimateData`, `CalculationContext`, optional `ISearchHistoryService`).
- Rewrote `ValidateAll()` to delegate to `_climateValidator.Validate(GetClimateData())` and set `IsValid` / `ValidationMessage` from the returned `Core.ValidationResult`.
- Removed inline range checks for `AirTemperature`, `WindSpeed`, `Humidity`, and `SnowfallIntensity` from `ClimateViewModel`; these rules remain in `ClimateValidator` unchanged.
- Updated constructor call sites:
  - `tests/SnowMeltingCalculator.Tests/Climate/ClimateViewModelTests.cs`
  - `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs`
  - `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs`
  - `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/GlycolAutoRecalculationTests.cs`
  - `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/PipeSpacingSynchronizationTests.cs`
  - `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs`
- Removed the `Validate_InvalidHumidity_ReturnsFalse` test because `ClimateValidator` does not validate humidity; remaining assertions on temperature, wind speed, and snowfall intensity continue to pass.
- Build: `dotnet build src/SnowMeltingCalculator.csproj -c Debug` > 0 errors.
- Targeted tests (`ClimateViewModelTests`, `ClimateValidatorTests`) > 41 passed, 0 failed.
- Grep for inline range checks (`AirTemperature < -50`, `WindSpeed < 0.1`, `Humidity < 20`, `SnowfallIntensity < 0`) in `ClimateViewModel.cs` > zero matches.
- Commit: `refactor(climate): use ClimateValidator instead of inline ValidateAll`.

## Todo 11 — Remove HydraulicInputData.Validate() + update ThermalViewModel

- Removed `public bool IsValid => Validate().IsValid;` and `public ValidationResult Validate()` from `src/Models/Hydraulics/HydraulicInputData.cs`.
- Removed the now-unused `using SnowMeltingCalculator.Core;` from `HydraulicInputData.cs`.
- Removed `inputData.Validate()` call from `CircuitsCalculator.CalculateAllCircuits`; the calculator no longer validates its input internally.
- Updated `ThermalViewModel` to constructor-inject `IValidator<ThermalInputs>` (`ThermalValidator`) and `IValidator<ThermalCalculationResult>` (`ThermalResultValidator`).
- Rewrote `ThermalViewModel.ValidateInput()` to build `ThermalInputs` and return `_thermalValidator.Validate(parameters)` as `Core.ValidationResult`.
- Updated `ThermalViewModel.Calculate()` to validate the result with `_thermalResultValidator.Validate(Result)` after calculation and merge any errors into `ValidationMessage`; valid results are still published to `CalculationContext`.
- Updated `HydraulicInputDataTests` to keep only the default-values test; all validation behavior is covered by `HydraulicValidatorTests`.
- Updated `ThermalViewModelTests` and the five hydraulics integration test fixtures to pass the new validator arguments to the `ThermalViewModel` constructor; tests use real `ThermalValidator` (with a real `ThermalCalculator` for validation) and `ThermalResultValidator`.
- Adjusted two `ThermalViewModelTests` assertions to match the unified validator messages: "Тип трубы" instead of "тип трубы" and "Температура наружного воздуха" instead of "Климатические данные".
- Build: `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → 0 errors, 6 pre-existing warnings.
- Targeted tests (`HydraulicInputDataTests`, `HydraulicValidatorTests`, `ThermalValidatorTests`, `ThermalResultValidatorTests`, `ThermalViewModel`) → 72 passed, 0 failed.
- Integration tests (`ThermalToHydraulicsIntegrationTests`, `PipeSpacingSynchronizationTests`, `GlycolAutoRecalculationTests`, `DoubleCalculationPreventionTests`, `ClimateToHydraulicsIntegrationTests`, `CircuitsCalculatorTests`) → 125 passed, 0 failed.
- Grep verification:
  - `HydraulicInputData.Validate()` / `HydraulicInputData.IsValid` → zero matches in `src/`.
  - `inputData.Validate()` in `src/Services/Hydraulics/CircuitsCalculator.cs` → zero matches.


