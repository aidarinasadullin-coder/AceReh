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



