# Refactor Baseline Dedupe � Learnings

## T1b: Circuits Baseline

- Created `tests/SnowMeltingCalculator.Tests/RefactorBaseline/CircuitsBaselineTests.cs` mirroring the structure of `ThermalBaselineTests.cs`.
- Populated the `"circuits"` array in `baseline_refactor_dedupe.json` with 16 deterministic cases while preserving the existing `"thermal"` array (27 cases).
- Verification: `dotnet test tests/SnowMeltingCalculator.Tests --filter "RefactorBaseline"` passes (43 tests total: 27 thermal + 16 circuits).

### Case matrix

| Variable | Values |
|----------|--------|
| Circuit length | 80 m, 120 m |
| Supply length | 8 m, 12 m |
| Valve type | HKV_D, IV_1_25 |
| Glycol concentration | 30%, 50% |

Fixed inputs:

- PowerUp = 256 W/m?
- PowerDown = 5 W/m?
- SupplyTemperature = 50 �C
- ReturnTemperature = 30 �C
- ColdFiveDayTemperature = -20 �C
- InnerDiameter = 16 mm
- GlycolType = Ethylene
- PipeSpacing = 20 cm
- SupplySpacing = 5 cm
- SupplyHeatPercent = 10%

### Key implementation notes

- Reused the same JSON serializer options as the thermal baseline: `PropertyNamingPolicy.CamelCase`, `WriteIndented = true`, and a custom `RoundTripDoubleConverter` for round-trip double fidelity.
- Added `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` so `FlowRegime` serializes as `"laminar"`, `"transitional"`, `"turbulent"`.
- The baseline DTO references `ThermalBaselineTests.ThermalCaseDto` for the thermal array to avoid duplicating the thermal shape while still writing a strongly-typed circuits array.
- All captured circuit outputs are asserted: `Power`, `FlowRate`, `Velocity`, and every numeric field of `OperatingResult` and `DesignResult` (temperature, density, kinematic viscosity, Reynolds number, flow regime, friction factor, pressure loss per meter, DpRohr, DpVerteiler, DpVent, DpGesamt, ZuDrosseln).

### Gotcha: NaN during regeneration

First run produced a `JsonReaderException` because the round-trip double converter tried to write `NaN`. Root cause: `ColdFiveDayTemperature = -30 �C` with `GlycolConcentration = 30%` falls outside the valid ASHRAE data range in `data/glycol_data.json` (null values below roughly -23 �C for 30% ethylene glycol). The interpolation returned `NaN`, which is not valid JSON.

Fix: raised `ColdFiveDayTemperature` to -20 �C, which lies within the valid interpolation range for both 30% and 50% ethylene glycol concentrations.

## T2: Dedupe MinPipeSpacing / MaxPipeSpacing

- Removed the `����������� ���� �������` region (the duplicate `MinPipeSpacing = 50.0` and `MaxPipeSpacing = 500.0` declarations) from `src/Core/Constants/ThermalConstants.cs`. Canonical declarations remain in `src/Core/Constants/ValidationConstants.cs` at lines 128 and 133.
- Reference scan before the edit: zero `src/` files referenced `ThermalConstants.{Min,Max}PipeSpacing`. `src/Core/Extensions/ValidationExtensions.cs` (lines 293-294) and `tests/SnowMeltingCalculator.Tests/Core/ValidationExtensionsTests.cs` (lines 558-559) already pointed at `ValidationConstants.{Min,Max}PipeSpacing`, so no migration was required.
- Verification:
  - `grep -rn "ThermalConstants.(MinPipeSpacing|MaxPipeSpacing)" src/` > 0 matches.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` > exit 0, 0 errors, 6 pre-existing warnings (CS1998, CS8604, CS0169) unrelated to this change.
  - `dotnet test tests/SnowMeltingCalculator.Tests` > 1076 passed, 1 failed, 2 explicitly skipped.
- Pre-existing failure (unrelated, verbatim): `FullWorkflow_ThermalClimateGlycol_TriggersCorrectNumberOfCalculates` in `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs:593` � `Moq.MockException` because `IGlycolDataService.GetProperties` was called 8 times instead of the asserted 4. This test exercises the hydraulics calculation-deduplication layer and has no relationship to the pipe-spacing constants.

### Lessons

- Before doing a constant dedupe, always run the reference scan first � it can reveal that "no migration needed" and reduce the task to a pure delete. A `grep` over `src/` for the qualified old name (`ThermalConstants.MinPipeSpacing`) is the gate; only fall back to a wider search if the gate returns matches.
- When removing a duplicate `const` that is also referenced from a test file, verify the test already references the canonical class. In this case the test (`ValidationExtensionsTests.cs`) was already on `ValidationConstants`, so removing the duplicate from `ThermalConstants` did not change any call sites.
- The WPF temp project (`SnowMeltingCalculator_xmqk4fps_wpftmp.csproj`) compiles alongside the main csproj during `dotnet build` of the WPF project, so warnings are emitted twice. This is expected and not a duplicate bug.

## T3: Rename `ThermalParameters` > `ThermalInputs`

- Created `src/Models/Thermal/ThermalInputs.cs` as `public sealed record ThermalInputs` with 14 `init-only` properties and the same defaults as the old `ThermalParameters` class. The record's compiler-generated parameterless constructor preserves all existing object-initializer call sites.
- Deleted `src/Models/Thermal/ThermalParameters.cs`.
- Renamed every `ThermalParameters` identifier in `src/` and `tests/` to `ThermalInputs`, including:
  - `IThermalCalculator` / `ThermalCalculator` method signatures.
  - `ThermalViewModel.BuildThermalParameters` > `BuildThermalInputs`.
  - All usages in `ThermalCalculatorTests.cs`, `ThermalViewModelTests.cs`, and `ThermalBaselineTests.cs`.
- Removed the post-build property reassignments in `ThermalViewModel.Calculate()` because `BuildThermalInputs()` already populates climate/construction fields; this kept the code compiling with `init-only` properties without changing the final values passed to the calculator.
- Converted test mutations (`parameters.Property = value;`) to record `with` expressions (`parameters = parameters with { Property = value };`) so the test suite compiles against the immutable shape.
- Tried to preserve the old `Clone()` method, but C# records reserve the `Clone` identifier (CS8859), so cloning semantics are now provided by the built-in `with` expression / compiler-generated protected `Clone`.
- Verification:
  - `grep`-equivalent over `src/` and `tests/` > 0 matches for `ThermalParameters`.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` > exit 0, 0 errors (pre-existing warnings remain).
  - `dotnet test tests/SnowMeltingCalculator.Tests --filter "RefactorBaseline"` > 43 passed, 0 failed, numeric outputs unchanged.
- Evidence captured in `.omo/evidence/refactor-dedupe-params/task-3/task-3-refactor-dedupe-params.txt`.

## T4/T5: Calculator API + ViewModel contract pass

- T4: Migrated `IThermalCalculator` / `ThermalCalculator` signatures to `Calculate(ThermalInputs inputs, IClimateData climate, IConstructionData construction)` and `Validate` likewise. Dropped `AirTemperature`, `WindSpeed`, `SnowfallIntensity`, `R1Total`, `R2Total` from `ThermalInputs`. Calculator now reads those 5 fields from the contract arguments.
- Direct orchestrator edits were required in two test files after the implementing subagent left compile errors:
  - `tests/SnowMeltingCalculator.Tests/RefactorBaseline/ThermalBaselineTests.cs`: added `BuildClimateData`/`BuildConstructionData` helpers and updated both `GenerateCases` and `ThermalOutput_MatchesBaseline` to call the 3-arg `Calculate`.
  - `tests/SnowMeltingCalculator.Tests/Thermal/ThermalViewModelTests.cs`: updated climate/construction assertions to read from `_mockCalculator.LastClimateData` / `_mockCalculator.LastConstructionData`, and renamed the old `BuildThermalInputs_IncludesClimateData` / `BuildThermalInputs_IncludesConstructionData` tests to `Calculate_PassesClimateData` / `Calculate_PassesConstructionData`.
- T5: `ThermalViewModel.BuildThermalInputs()` already returns only thermal-owned fields (`Mode`, `SupplyTemperature`, `DeltaT`, `GroundTemperature`, `Pipe`, `PipeSpacing`, `LambdaE`) and `Calculate()` passes `_climateData` / `_constructionData` to the calculator. No further changes needed.
- Verification: `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj` exit 0 (0 errors, 52 pre-existing warnings); `dotnet test tests/SnowMeltingCalculator.Tests --filter "RefactorBaseline"` -> 43 passed, 0 failed.

## T6: Drop echo-in fields from ThermalCalculationResult

- Removed `R1Total`, `R2Total` from interface `IThermalCalculationResult` and class `ThermalCalculationResult`.
- Removed `Pipe`, `PipeSpacing` from class `ThermalCalculationResult` (they were class-only).
- Removed echo assignments `result.R1Total = construction.R1Total` and `result.R2Total = construction.R2Total` from `ThermalCalculator.Calculate`.
- Verified no remaining readers of the removed fields on `ThermalCalculationResult`/`IThermalCalculationResult` in `src/`.
- Verification:
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` -> exit 0, 0 errors, 0 warnings.
  - `dotnet test tests/SnowMeltingCalculator.Tests --filter "RefactorBaseline"` -> 43 passed, 0 failed.
  - `grep` checks for `Result.(Pipe|PipeSpacing|R1Total|R2Total)` in `src/` returned 0 matches.

## T11: Update echo-field readers (PDF export, Project serialize)

- Verified that no `src/` code still reads the removed echo fields (`Pipe`, `PipeSpacing`, `R1Total`, `R2Total`) from `ThermalCalculationResult` or `IThermalCalculationResult`.
- Project serialization DTO (`ProjectData.cs`) and PDF export data (`ResultsPdfData.cs`/`PdfExportService.cs`) use their own properties populated from the canonical contract sources (`IConstructionData`, `ICalculationStateService`, `ThermalInputs`) via the ViewModels, not from the result object.
- No serialized JSON property names were changed, preserving v1.0 `.snowproj` compatibility.
- Verification:
  - `grep -rn "ThermalCalculationResult" src/ | grep -E "\.(Pipe|PipeSpacing|R1Total|R2Total)\b"` > 0 matches.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` > exit 0.
  - `dotnet test --filter "RefactorBaseline"` > 43 passed, 0 failed.


## T8: Harden SetPipeSpacing so only the canonical writer can call it

- Added ool IsLoadProjectInProgress { get; set; } and oid SetPipeSpacing(int spacing, string source); to ICalculationStateService.
- Implemented the guarded overload in CalculationStateService: throws InvalidOperationException($"SetPipeSpacing called from non-canonical source: {source}") unless source == "ThermalViewModel" or (source == "ResultsViewModel.LoadProject" and IsLoadProjectInProgress is true). The existing parameterless SetPipeSpacing(int) now forwards to SetPipeSpacing(spacing, "ThermalViewModel").
- Updated ThermalViewModel.OnPipeSpacingChanged to call _calculationStateService.SetPipeSpacing(value, "ThermalViewModel").
- Injected ICalculationStateService into ResultsViewModel and wired it through DI in ServiceCollectionExtensions.
- Wrapped ResultsViewModel.LoadProjectData with _calculationStateService.IsLoadProjectInProgress = true/false (try/finally) and replaced the direct _thermalViewModel.PipeSpacing = data.ThermalData.PipeSpacing write with a guarded service call followed by VM property assignment to keep the two values in sync.
- Created 	ests/SnowMeltingCalculator.Tests/Services/Navigation/CalculationStateServiceGuardTests.cs with BadSource_Throws, CanonicalSource_SetsAndRaisesEvent, and ResultsViewModelLoadProjectSource_RequiresFlag.
- Verification:
  - grep over src/ for .SetPipeSpacing( -> only two matches: ThermalViewModel.OnPipeSpacingChanged and ResultsViewModel.LoadProject.
  - dotnet test --filter "CalculationStateServiceGuardTests" -> 3 passed.
  - dotnet test --filter "RefactorBaseline" -> 43 passed.
  - dotnet build src/SnowMeltingCalculator.csproj -c Debug -> exit 0.
  - Committed as 
efactor(pipe-spacing): ThermalViewModel is the only PipeSpacing writer.

## T7: CircuitsViewModel reads thermal data from InputData + contracts

- Added `PipeSpacing`, `SelectedPipe`, and `ThermalResult` properties to `src/Models/Hydraulics/HydraulicInputData.cs` so the hydraulics module can carry the thermal-derived inputs it needs.
- In `src/ViewModels/Hydraulics/CircuitsViewModel.cs`:
  - Replaced all `_thermalViewModel.SelectedPipe` reads with `InputData.SelectedPipe` (used by `PipeType`, `OuterDiameter`, `WallThickness`).
  - Replaced all `_thermalViewModel.PipeSpacing` reads with `InputData.PipeSpacing` (used by `PipeSpacing_cm` and inside `Calculate()`).
  - Replaced all `_thermalViewModel.Result` reads by parameterizing `UpdateFromThermalModule(IThermalCalculationResult? thermalResult, PipeType? selectedPipe)`; the method now stores the result/pipe in `InputData` and propagates the scalar values.
  - Disabled the body of `OnThermalViewModelPropertyChanged` while keeping the `_thermalViewModel` constructor injection and `PropertyChanged` subscription intact for T15.
  - At the top of `Calculate()`, `InputData.PipeSpacing` is seeded from `_calculationStateService.PipeSpacing` if not already populated, and `InputData.InnerDiameter` is seeded from `InputData.SelectedPipe` when available; fallback defaults remain unchanged.
- Updated the single external caller in `src/ViewModels/Results/ResultsViewModel.cs` to pass `_thermalViewModel.Result` and `_thermalViewModel.SelectedPipe` into `UpdateFromThermalModule`.
- Verification:
  - `grep -n "_thermalViewModel\." src/ViewModels/Hydraulics/CircuitsViewModel.cs` -> 1 match (`PropertyChanged` subscription), none of `PipeSpacing`, `SelectedPipe`, or `Result`.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` -> exit 0, 0 new errors (6 pre-existing warnings).
  - `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj --filter "RefactorBaseline"` -> 43 passed, 0 failed.
