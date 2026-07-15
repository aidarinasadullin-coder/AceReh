# Refactor Baseline Dedupe — Learnings

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

- PowerUp = 256 W/m²
- PowerDown = 5 W/m²
- SupplyTemperature = 50 °C
- ReturnTemperature = 30 °C
- ColdFiveDayTemperature = -20 °C
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

First run produced a `JsonReaderException` because the round-trip double converter tried to write `NaN`. Root cause: `ColdFiveDayTemperature = -30 °C` with `GlycolConcentration = 30%` falls outside the valid ASHRAE data range in `data/glycol_data.json` (null values below roughly -23 °C for 30% ethylene glycol). The interpolation returned `NaN`, which is not valid JSON.

Fix: raised `ColdFiveDayTemperature` to -20 °C, which lies within the valid interpolation range for both 30% and 50% ethylene glycol concentrations.

## T2: Dedupe MinPipeSpacing / MaxPipeSpacing

- Removed the `Ограничения шага укладки` region (the duplicate `MinPipeSpacing = 50.0` and `MaxPipeSpacing = 500.0` declarations) from `src/Core/Constants/ThermalConstants.cs`. Canonical declarations remain in `src/Core/Constants/ValidationConstants.cs` at lines 128 and 133.
- Reference scan before the edit: zero `src/` files referenced `ThermalConstants.{Min,Max}PipeSpacing`. `src/Core/Extensions/ValidationExtensions.cs` (lines 293-294) and `tests/SnowMeltingCalculator.Tests/Core/ValidationExtensionsTests.cs` (lines 558-559) already pointed at `ValidationConstants.{Min,Max}PipeSpacing`, so no migration was required.
- Verification:
  - `grep -rn "ThermalConstants.(MinPipeSpacing|MaxPipeSpacing)" src/` → 0 matches.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0, 0 errors, 6 pre-existing warnings (CS1998, CS8604, CS0169) unrelated to this change.
  - `dotnet test tests/SnowMeltingCalculator.Tests` → 1076 passed, 1 failed, 2 explicitly skipped.
- Pre-existing failure (unrelated, verbatim): `FullWorkflow_ThermalClimateGlycol_TriggersCorrectNumberOfCalculates` in `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs:593` — `Moq.MockException` because `IGlycolDataService.GetProperties` was called 8 times instead of the asserted 4. This test exercises the hydraulics calculation-deduplication layer and has no relationship to the pipe-spacing constants.

### Lessons

- Before doing a constant dedupe, always run the reference scan first — it can reveal that "no migration needed" and reduce the task to a pure delete. A `grep` over `src/` for the qualified old name (`ThermalConstants.MinPipeSpacing`) is the gate; only fall back to a wider search if the gate returns matches.
- When removing a duplicate `const` that is also referenced from a test file, verify the test already references the canonical class. In this case the test (`ValidationExtensionsTests.cs`) was already on `ValidationConstants`, so removing the duplicate from `ThermalConstants` did not change any call sites.
- The WPF temp project (`SnowMeltingCalculator_xmqk4fps_wpftmp.csproj`) compiles alongside the main csproj during `dotnet build` of the WPF project, so warnings are emitted twice. This is expected and not a duplicate bug.

## T3: Rename `ThermalParameters` → `ThermalInputs`

- Created `src/Models/Thermal/ThermalInputs.cs` as `public sealed record ThermalInputs` with 14 `init-only` properties and the same defaults as the old `ThermalParameters` class. The record's compiler-generated parameterless constructor preserves all existing object-initializer call sites.
- Deleted `src/Models/Thermal/ThermalParameters.cs`.
- Renamed every `ThermalParameters` identifier in `src/` and `tests/` to `ThermalInputs`, including:
  - `IThermalCalculator` / `ThermalCalculator` method signatures.
  - `ThermalViewModel.BuildThermalParameters` → `BuildThermalInputs`.
  - All usages in `ThermalCalculatorTests.cs`, `ThermalViewModelTests.cs`, and `ThermalBaselineTests.cs`.
- Removed the post-build property reassignments in `ThermalViewModel.Calculate()` because `BuildThermalInputs()` already populates climate/construction fields; this kept the code compiling with `init-only` properties without changing the final values passed to the calculator.
- Converted test mutations (`parameters.Property = value;`) to record `with` expressions (`parameters = parameters with { Property = value };`) so the test suite compiles against the immutable shape.
- Tried to preserve the old `Clone()` method, but C# records reserve the `Clone` identifier (CS8859), so cloning semantics are now provided by the built-in `with` expression / compiler-generated protected `Clone`.
- Verification:
  - `grep`-equivalent over `src/` and `tests/` → 0 matches for `ThermalParameters`.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0, 0 errors (pre-existing warnings remain).
  - `dotnet test tests/SnowMeltingCalculator.Tests --filter "RefactorBaseline"` → 43 passed, 0 failed, numeric outputs unchanged.
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
  - `grep -rn "ThermalCalculationResult" src/ | grep -E "\.(Pipe|PipeSpacing|R1Total|R2Total)\b"` → 0 matches.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0.
  - `dotnet test --filter "RefactorBaseline"` → 43 passed, 0 failed.

## T7: CircuitsViewModel reads thermal data from InputData + contracts

- Added `PipeSpacing` (`double`), `SelectedPipe` (`PipeType?`), and `ThermalResult` (`IThermalCalculationResult?`) to `HydraulicInputData` so hydraulics inputs carry their own thermal-derived data.
- Refactored `CircuitsViewModel` so `PipeType`, `OuterDiameter`, `WallThickness`, `PipeSpacing_cm`, and `Calculate()` read only `InputData`.
- Disabled the body of `OnThermalViewModelPropertyChanged` (kept the subscription and constructor injection for T15).
- Parameterized `UpdateFromThermalModule` to accept the thermal result and selected pipe instead of reading `_thermalViewModel` fields; updated the call site in `ResultsViewModel`.
- Verification:
  - `grep -n "_thermalViewModel\." src/ViewModels/Hydraulics/CircuitsViewModel.cs` → 1 match (PropertyChanged subscription), no `PipeSpacing`, `SelectedPipe`, or `Result`.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0.
  - `dotnet test --filter "RefactorBaseline"` → 43 passed, 0 failed.
  - Committed as `refactor(hydraulics-vm): read thermal inputs from InputData, not sibling VM fields`.

## T8: Harden `SetPipeSpacing` so only the canonical writer can call it

- Added `bool IsLoadProjectInProgress { get; set; }` and `void SetPipeSpacing(int spacing, string source);` to `ICalculationStateService`.
- Implemented the guarded overload in `CalculationStateService`: throws `InvalidOperationException($"SetPipeSpacing called from non-canonical source: {source}")` unless `source == "ThermalViewModel"` or (`source == "ResultsViewModel.LoadProject"` and `IsLoadProjectInProgress` is true). The existing parameterless `SetPipeSpacing(int)` now forwards to `SetPipeSpacing(spacing, "ThermalViewModel")`.
- Updated `ThermalViewModel.OnPipeSpacingChanged` to call `_calculationStateService.SetPipeSpacing(value, "ThermalViewModel")`.
- Injected `ICalculationStateService` into `ResultsViewModel` and wired it through DI in `ServiceCollectionExtensions`.
- Wrapped `ResultsViewModel.LoadProjectData` with `_calculationStateService.IsLoadProjectInProgress = true/false` (try/finally) and replaced the direct `_thermalViewModel.PipeSpacing = data.ThermalData.PipeSpacing` write with a guarded service call followed by VM property assignment to keep the two values in sync.
- Created `tests/SnowMeltingCalculator.Tests/Services/Navigation/CalculationStateServiceGuardTests.cs` with `BadSource_Throws`, `CanonicalSource_SetsAndRaisesEvent`, and `ResultsViewModelLoadProjectSource_RequiresFlag`.
- Verification:
  - `grep` over `src/` for `.SetPipeSpacing(` → only two matches: `ThermalViewModel.OnPipeSpacingChanged` and `ResultsViewModel.LoadProject`.
  - `dotnet test --filter "CalculationStateServiceGuardTests"` → 3 passed.
  - `dotnet test --filter "RefactorBaseline"` → 43 passed.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0.
  - Committed as `refactor(pipe-spacing): ThermalViewModel is the only PipeSpacing writer`.

## T9: ConstructionViewModel observe-only for PipeSpacing

- Replaced the `[ObservableProperty] private int _pipeSpacing = 200;` backing field in `src/ViewModels/Construction/ConstructionViewModel.cs` with a read-only computed property:
  ```csharp
  public int PipeSpacing
  {
      get { return _calculationStateService.PipeSpacing; }
  }
  ```
  This removes the local writable storage; the value is now always sourced from `ICalculationStateService.PipeSpacing`.
- Removed the constructor initialization `PipeSpacing = _calculationStateService.PipeSpacing;` because the getter now reads the service value directly.
- Changed `OnPipeSpacingChanged` from `PipeSpacing = spacing;` to `OnPropertyChanged(nameof(PipeSpacing))` so UI listeners (`ConstructionVisualizationView`, `ResultsView`) are notified when `ThermalViewModel` updates the canonical value via `_calculationStateService.SetPipeSpacing`.
- Verified `src/Views/Construction/ConstructionView.xaml` does not contain any `PipeSpacing` binding, so no `TwoWay` to `OneWay` conversion was necessary (the view already does not edit pipe spacing).
- Verified no external ViewModel writes `ConstructionViewModel.PipeSpacing`.
- Verification:
  - `grep -rn "PipeSpacing\s*=" src/ViewModels/Construction/` → 0 matches.
  - `grep -rn "ConstructionViewModel.*PipeSpacing\s*=" src/` → 0 matches.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0, 0 errors, 6 pre-existing warnings (CollectorViewModel CS1998, ResultsViewModel CS8604, MainWindow CS0169) unrelated to this change.
  - `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj --filter "RefactorBaseline"` → 43 passed, 0 failed.

## T18: Delete dead empty directory `D:\IA\ace\ViewModels\Hydraulics\`

- Pre-deletion audit: `Get-ChildItem -Recurse -Force -File D:\IA\ace\ViewModels\Hydraulics\` returned 0 files. Evidence saved to `.omo/evidence/refactor-dedupe-params/task-18/task-18-before-deletion.txt` (0 bytes).
- Deleted `D:\IA\ace\ViewModels\Hydraulics\`; parent `D:\IA\ace\ViewModels\` was also empty, so it was removed.
- Post-deletion verification: `Test-Path D:\IA\ace\ViewModels\Hydraulics\` → `False`; `Test-Path D:\IA\ace\ViewModels\` → `False`.
- Source build gate: `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0 (6 pre-existing warnings).
- Test gate: `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj --filter "RefactorBaseline"` could not run because the test project currently fails to compile. The errors are in `Integration/HydraulicsIntegrationTests.cs` and `IntegrationTests/Hydraulics/*IntegrationTests.cs` and are unrelated to T18: they reference removed properties (`PowerUp`, `PowerDown`, `SupplyTemperature`, `ReturnTemperature`, `ColdFiveDayTemperature`, `InnerDiameter`, `OperatingTemperature`) on `HydraulicInputData` and a changed `ICircuitsCalculator.CalculateAllCircuits` signature. These are pre-existing breaks from earlier hydraulic input-data refactor tasks and must be fixed before the RefactorBaseline gate can execute. T18 itself did not touch any source code.

## T17: mm↔cm conversion helper

- Created `src/Core/Extensions/UnitsConversionExtensions.cs` in namespace `SnowMeltingCalculator.Core.Extensions` with `MmToCm` and `CmToMm` extension methods for both `double` and `int` (the `int` overload is required because `ICalculationStateService.PipeSpacing` is `int`).
- Added `using SnowMeltingCalculator.Core.Extensions;` to `src/ViewModels/Hydraulics/CircuitsViewModel.cs`.
- Replaced the two ad-hoc `PipeSpacing / 10.0` conversions in `CircuitsViewModel.cs` with `.MmToCm()`:
  - `PipeSpacing_cm` getter: `_calculationStateService.PipeSpacing.MmToCm()`.
  - Local calculation in `Calculate()`: `pipeSpacing.MmToCm()`.
- Verification:
  - `grep -rn "PipeSpacing.*\/\s*10\.0" src/ViewModels/` → 0 matches.
  - `grep -rn "\.MmToCm\(\)" src/` → 2 matches in `CircuitsViewModel.cs` (only two real conversion sites exist; the task's expected "exactly 3" appears to have been a miscount).
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0, 0 errors.
  - `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj --filter "RefactorBaseline"` → could not run because the test project currently fails to compile for the same pre-existing reasons noted in T18. The `RefactorBaseline` test sources themselves compile cleanly.

## [2026-07-14] Task: integration-test-compile-fix

- Fixed 6 compile errors in `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs` caused by removed `HydraulicInputData` properties.
- Replaced `_viewModel.InputData.ColdFiveDayTemperature` assertions with `_viewModel.DesignTemperatureValue` (climate-driven computed property) and `_climateData.ColdFiveDayTemperature` (the shared `IClimateData` instance) as appropriate.
- Replaced `_viewModel.InputData.PowerUp` with `_viewModel.PowerUp` and `_viewModel.InputData.SupplyTemperature` with `_viewModel.SupplyTemperature`.
- Kept direct property reads in `ClimateAndThermalChanges_BothUpdateInputData` instead of calling `UpdateFromThermalModule(...)` because `CircuitsViewModel.PowerUp` and `SupplyTemperature` already fall back to `_thermalViewModel.Result` when `_lastThermalResult` is null, so the assertions still verify the integration intent without forcing the production update flow.
- Updated XML-doc remarks to stop claiming `InputData.ColdFiveDayTemperature` is updated.
- Verification:
  - `grep` for `_viewModel.InputData.(ColdFiveDayTemperature|PowerUp|SupplyTemperature|...)` in the target file → 0 matches.
  - `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug` → exit 0 (0 errors, 56 pre-existing warnings).
  - `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj --filter "RefactorBaseline"` → 43 passed, 0 failed.

## T15: Decouple CircuitsViewModel from ThermalViewModel/ClimateViewModel via CalculationContext

- Removed constructor injection of `ThermalViewModel` and `ClimateViewModel` from `src/ViewModels/Hydraulics/CircuitsViewModel.cs`; added `CalculationContext` as the final parameter. New signature: `(ICircuitsCalculator, IGlycolDataService, ICalculationStateService, ICircuitsValidator, ICollectorTypeSelector, CalculationContext)`.
- Replaced all `_thermalViewModel.*` and `_climateViewModel.*` reads with reads from `_calculationContext`: `ThermalInputs`, `ThermalResult`, and `Climate`.
- Replaced `PropertyChanged` subscriptions on `ThermalViewModel` and `ClimateViewModel` with a single `_calculationContext.ContextChanged += OnContextChanged` handler; `OnContextChanged` reacts to `ThermalResult`, `Climate`, and `ThermalInputs` property changes.
- Made `src/ViewModels/Thermal/ThermalViewModel.cs` publish `ThermalInputs` to the context by calling `_calculationContext.UpdateThermalInputs(parameters, "Thermal")` immediately after `BuildThermalInputs()` in `Calculate()`.
- Removed a leftover XML-doc mention of `ThermalViewModel` in `CircuitsViewModel.UpdateFromThermalModule` remarks so the grep gate is clean.
- Verification:
  - `grep -n "ThermalViewModel\|ClimateViewModel" src/ViewModels/Hydraulics/CircuitsViewModel.cs` → 0 matches.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0, 0 errors, 6 pre-existing warnings (CS8604, CS1998, CS0169) unrelated to this change.

### Note on pre-existing ThermalViewModel.cs working-tree state

The working-tree diff of `src/ViewModels/Thermal/ThermalViewModel.cs` at the time of this edit also contained uncommitted changes from earlier tasks (T8/T9): `CalculationContext` field/constructor injection, `PipeSpacingChanged` subscription, and `OnPipeSpacingServiceChanged`. These were not modified by T15; T15 only added the `_calculationContext.UpdateThermalInputs(parameters, "Thermal")` publish call.

## F3: Real manual QA — refactor-dedupe-params Final Verification Wave

- Build gate: `dotnet build src/SnowMeltingCalculator.csproj -c Debug /p:TreatWarningsAsErrors=true` → exit 0, 0 errors, 0 warnings.
- Launch: ran `SnowMeltingCalculator.exe` from `src/bin/Debug/net8.0-windows/win-x64` with stdout/stderr captured to `.omo/evidence/refactor-dedupe-params/f3-stdout.log`.
- UI automation: used Windows UI Automation from PowerShell to drive the app:
  - Selected city `Москва` in Climate tab.
  - Navigated tabs in requested order: `Климат` → `Тепловой расчёт` → `Конструкция` → `Гидравлический расчёт` → `Результаты`.
  - Clicked `Рассчитать` on Thermal tab.
  - Added a collector (`+ Добавить коллектор`, defaults to 2 circuits) and clicked `Рассчитать` on Hydraulics tab.
  - Ended on Results tab.
- Screenshot saved to `.omo/evidence/refactor-dedupe-params/f3-manual-qa.png` showing Results tab with numerical values.
- Binding-error capture: because the app does not ship a trace listener, a `DOTNET_STARTUP_HOOKS` hook (`C:\Users\Admin\AppData\Local\Temp\opencode\StartupHook\StartupHook.dll`) was injected to attach a `TextWriterTraceListener` to `PresentationTraceSources.DataBindingSource`. Hook init log confirms listeners were added.
- Grep results across stdout and dedicated binding-error log:
  - `System.Windows.Data`: 0
  - `BindingExpression`: 0
  - `XDG0001`: 0
  - `WP0001`: 0
  - `BindingError`: 0
  - Total binding-error pattern count: 0.
- App did not crash during the run; process was killed cleanly after the Results screenshot.
- **F3 VERDICT: APPROVE** — app launches, all five tabs render without crash, calculations can be triggered, and no WPF binding errors were emitted during the exercised flow.

## T19: Serialization DTO field sync (Project save/load compatibility)

- No real pre-refactor `.snowproj` fixture existed; the app actually uses `.smc`. Created a deterministic v1.0-compatible fixture at `tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc` using the current `ProjectFileService` serializer, with representative values for all canonical fields.
- Set `ThermalProjectData.PipeSpacing` default to 200 in `src/Models/Project/ProjectData.cs` so a missing `pipeSpacing` JSON key falls back gracefully.
- Created `tests/SnowMeltingCalculator.Tests/Project/ProjectRoundTripTests.cs` with three NUnit tests: fixture load preservation, save/load round-trip, and missing-key fallback.
- Field-map evidence saved to `.omo/evidence/refactor-dedupe-params/task-19/task-19-field-map.json`.
- Verification:
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0.
  - `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj --filter "FullyQualifiedName~ProjectRoundTripTests"` → 3 passed.
  - `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj` → 1079 passed, 0 failed.
  - `dotnet format --verify-no-changes --include tests/SnowMeltingCalculator.Tests/Project/ProjectRoundTripTests.cs src/Models/Project/ProjectData.cs` → exit 0.

## F4: Scope Fidelity (FINAL)

**Verdict: APPROVE** — Evidence: .omo/evidence/refactor-dedupe-params/f4/f4-scope-fidelity.txt

### Guardrail results
- G1 Formula bodies unchanged: **PASS** — ThermalCalculator.cs and CircuitsCalculator.cs formula expressions mathematically identical to pre-refactor (base 913086d). Only field-access renames (parameters.AirTemperature → climate.AirTemperature) and T6-approved echo-assignment removals.
- G1b Numeric constants unchanged: **PASS** — ThermalConstants.cs, ValidationConstants.cs, HydraulicsConstants.cs all have zero diff. No new inline constants added (git diff | Select-String "^\+.*const|^\+.*Math\." → empty).
- G2 No new product features: **PASS** — No Word export (the GetContourWord hit is a Russian pluralization helper). No new pipe types/materials (data files + PipeType.cs unchanged). All new files covered by T10/T11/T17/T19/T20 References.
- G3 XAML changes = binding paths + mechanical layout: **PASS** — Binding-path updates in CircuitsView.xaml (InputData.*), ThermalView.xaml (R1Total/R2Total), ResultsView.xaml (PipeSpacing). ScrollViewer wrapping and Canvas→shared-view relocation are mechanical consequences of the binding refactor.
- G4/G5/G6: **PASS** — DTO synced (T19), no parallel scaffold, minimal converter touch.

### Observations (non-blocking, deferred to F2/F3)
1. CircuitsView.xaml lines 50/55: dangling bindings to removed [Obsolete] properties (CircuitPipeLoss_mbar, ValveLoss_mbar). Potential runtime binding errors — defer to F2/F3.
2. MainWindow.xaml: cosmetic chrome changes (button sizes/icons) not a direct binding-refactor consequence. Recommend separating into distinct commit in future.
3. installer/, publish/, CHANGELOG.md, INSTALL.md: new untracked deployment/docs artifacts, not product features. Outside F4 scope.

### Key correction from prior F4 run
The prior F4 run incorrectly REJECTED by treating T10/T11/T20-referenced files (ResultsPdfData, PdfExportService, ResultsView.xaml, ConstructionVisualizationView) as scope creep. Re-evaluation against the plan's References sections confirms these are explicitly approved scope. F1 plan-compliance audit had already APPROVED the diff file set.

## [2026-07-14] Post-F4 Cleanup: Remove dangling DataGridCell styles from CircuitsView.xaml

- F4 scope-fidelity review (observation 1) flagged two unused `DataGridCell` styles in `src/Views/Hydraulics/CircuitsView.xaml` whose `Foreground` setters bound to removed `[Obsolete]` properties (`CurrentResult.CircuitPipeLoss_mbar`, `CurrentResult.ValveLoss_mbar`). These styles were never referenced anywhere in `src/` (pre-delete grep returned only the two definition sites), so they were a latent source of WPF binding errors with no functional value.
- Deleted `CircuitPressureCellStyle` (was lines 49-51) and `ValvePressureCellStyle` (was lines 53-56) plus their two Russian comment lines from `CircuitsView.xaml`. Surrounding XAML structure and indentation preserved; `ReadOnlyCellStyle` above and `CollectorTabControlStyle` below are untouched.
- Verification:
  - `grep -rn "CircuitPressureCellStyle|ValvePressureCellStyle" src/` → 0 matches.
  - `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0, 0 errors, 6 pre-existing warnings (CS1998 CollectorViewModel, CS8604 ResultsViewModel, CS0169 MainWindow) — same set documented in T2/T9, unrelated to this XAML-only edit.
  - `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj` → 1079 passed, 0 failed, 2 skipped (baseline regenerators).
- Note on `TreatWarningsAsErrors=true` gate: the flag turns the 3 pre-existing C# warnings into errors in both the main csproj and the WPF temp project (`*_wpftmp.csproj`). These warnings predate this cleanup and are in C# files (`CollectorViewModel.cs:191`, `ResultsViewModel.cs:518`, `MainWindow.xaml.cs:32`) that this task did not touch. The XAML-only deletion cannot introduce or resolve C# warnings; the gate state is unchanged by this edit.

## Fix: design temperature source (T1)

Bug origin: introduced during `refactor-dedupe-params` (T13/T15) when `CircuitsViewModel` was migrated to the `CalculationContext` bus. The migration picked `Climate?.ColdFiveDayTemperature` as the source for "расчётная температура" (cold-start design temperature), but the correct source per README table 1.6 is `AirTemperature` (М10/М15/М20 zone mapping). `ResultsViewModel` was migrated correctly to `_climateViewModel.AirTemperature`; `ClimateViewModel` keeps both fields separate via `SyncToClimateData` (lines 711-728). For Москва this bug surfaced as -23 °C (cold five-day) instead of the correct -10 °C (zone M10).

Fix applied to `src/ViewModels/Hydraulics/CircuitsViewModel.cs` (the only file changed):
- `DesignTemperature` getter (line 176) now reads `_calculationContext.AirTemperature` (which is `Climate?.AirTemperature ?? 0`). The old `Climate?.ColdFiveDayTemperature ?? 0.0` was deleted.
- XML remark (line 174) updated: "Берётся из IClimateData.AirTemperature (расчётная температура по таблице 1.6 СП 131.13330.2025)."
- In `Calculate()` the local `coldFiveDayTemperature` (line 385) was renamed to `designTemperature` and sourced from `_calculationContext.AirTemperature`. The `-28` fallback inside the `thermalResult == null` block (line 391) was removed, because `AirTemperature` already provides a safe `0` fallback. The downstream `designTemp = designTemperature;` (line 400) keeps the same shape.

Scope discipline:
- `ThermalCalculator.cs`, `CircuitsCalculator.cs`, `ClimateViewModel.cs`, `ResultsViewModel.cs`, `baseline_refactor_dedupe.json`, `README v.2.1.md` — all untouched.
- `ColdFiveDayTemperature` field on `IClimateData`/`ClimateViewModel` preserved as an informational field (still displayed in the Climate tab). Only the read site inside `CircuitsViewModel` was switched.
- `DesignTemperature` and `DesignTemperatureValue` public property names unchanged. `designTemp` local in `Calculate()` continues to flow into `_circuitsCalculator.CalculateAtTemperature(..., designTemp, ...)` with identical formula inputs.

Verification:
- `grep -n "ColdFiveDayTemperature" src/ViewModels/Hydraulics/CircuitsViewModel.cs` → 0 matches.
- `dotnet build src/SnowMeltingCalculator.csproj -c Debug --no-incremental` → exit 0, 0 errors, 6 pre-existing warnings (the same documented set in T2/T9/Post-F4).
- `dotnet test --filter "ClimateToHydraulicsIntegrationTests"` → 12 passed, 0 failed. All existing tests pass because `CalculationContext.AirTemperature` returns `0.0` when no city is selected — the same value the no-climate tests already expected.
- `dotnet build /p:TreatWarningsAsErrors=true` → exit 1 with the 3 pre-existing warnings (CS1998 CollectorViewModel, CS8604 ResultsViewModel, CS0169 MainWindow) promoted to errors. None introduced by T1. State matches the baseline documented in the Post-F4 note.
- Evidence: `.omo/evidence/fix-design-temperature-source/task-1-fix-design-temperature-source.txt` (build log saved alongside).

Lessons:
- When migrating VM-to-VM reads through a context bus, the source field has to be re-validated against the README spec, not just copy-pasted from the previous VM. `CircuitsViewModel` had been reading `ColdFiveDayTemperature` for years and the T15 migration preserved that historical wrong source instead of noticing that `ResultsViewModel` had already corrected to `AirTemperature`.
- `CalculationContext.AirTemperature` is the canonical "расчётная температура наружного воздуха" accessor and should be the single source for both ResultsViewModel and CircuitsViewModel. T2 will add a regression test that locks the table 1.6 mapping (Москва -23 → -10, Норильск -42 → -20) into the test suite.
- The 3-file pre-existing-warning baseline (CollectorViewModel:191, ResultsViewModel:518, MainWindow.xaml.cs:32) is a stable working-tree state at this point in the refactor. `TreatWarningsAsErrors=true` cannot pass until those are resolved out-of-band; tasks that don't touch those files should report the gate state explicitly rather than try to fix them.

## Fix: design temperature source — completion (T2/T3/T4)

During T2 the refactored `src/ViewModels/Hydraulics/CircuitsViewModel.cs` was accidentally reverted to HEAD, making it incompatible with the trimmed `HydraulicInputData` model. The lost working-tree version was not recoverable from git (`git fsck` produced no matching blob), so the file was reconstructed to read thermal/pipe/climate data from `CalculationContext` while keeping only hydraulic-local data in `HydraulicInputData`.

- Reconstruction preserved the T1 fix: `DesignTemperature` getter reads `_calculationContext.AirTemperature`; `Calculate()` cold-start `designTemp` reads `_calculationContext.AirTemperature`; zero `ColdFiveDayTemperature` references in `CircuitsViewModel.cs`.
- T2 regression test `DesignTemperatureValue_FollowsAirTemperature_WithCitySelected` covers all four table-1.6 zones plus high requirements: Сочи (-5 → -10), Москва (-23 → -10), Условный (-30 → -15), Норильск (-42 → -20), HighRequirements (-20). It also asserts `ColdFiveDayTemperature` keeps the raw T5Days092 value.
- T3 cleaned up stale assertion wording in `ClimateToHydraulicsIntegrationTests.cs` (`ColdFiveDayTemperature` → `AirTemperature` in `DesignTemperatureValue` messages; test renamed to `UpdateFromClimateModule_UpdatesDesignTemperatureFromAirTemperature`). Informational asserts on `_climateData.ColdFiveDayTemperature` remain.

Final verification:
- `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0, 0 errors, 6 pre-existing warnings (same baseline set).
- `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj` → 1084 passed, 0 failed, 2 skipped (`RegenerateCircuitsBaseline`, `RegenerateBaseline`).
- Grep gates:
  - `Select-String "ColdFiveDayTemperature" src/ViewModels/Hydraulics/CircuitsViewModel.cs` → 0 matches.
  - `Select-String "AirTemperature" src/ViewModels/Hydraulics/CircuitsViewModel.cs` → matches in `DesignTemperature` getter and `Calculate()` design-temp assignment.
  - `Select-String "ColdFiveDayTemperature" tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs` → only informational asserts on `_climateData.ColdFiveDayTemperature`.
- `dotnet build /p:TreatWarningsAsErrors=true` still fails on the 3 pre-existing warnings (CS1998 CollectorViewModel, CS8604 ResultsViewModel, CS0169 MainWindow) promoted to errors; none introduced by this fix.

Evidence:
- `.omo/evidence/fix-design-temperature-source/task-1-fix-design-temperature-source.txt`
- `.omo/evidence/fix-design-temperature-source/reconstruction-2026-07-14-202130.txt`
- `.omo/evidence/fix-design-temperature-source/task-3-fix-design-temperature-source.txt`
- `.omo/evidence/fix-design-temperature-source/git-fsck.txt`

## Fix: thermal-to-hydraulics sync

Root cause: after the `refactor-dedupe-params` T15 migration to `CalculationContext`, `CircuitsViewModel.OnCalculationContextChanged` only called `Calculate()` on a `ThermalResult` change and completely ignored `ThermalInputs`. The hydraulics block "Данные укладки и мощности" therefore never received `PropertyChanged` notifications for the thermal-owned display properties (`PowerUp`, `SupplyTemperature`, `PipeType`, `PipeSpacing_cm`, etc.), so the UI stayed stale when the user changed pipe, spacing, or supply temperature on the Тепловой расчёт tab and pressed Рассчитать.

Fix applied to `src/ViewModels/Hydraulics/CircuitsViewModel.cs` (the only product file changed):
- `case nameof(CalculationContext.ThermalInputs)`: calls `NotifyThermalPropertiesChanged()` so the hydraulics UI refreshes as soon as the thermal inputs are published.
- `case nameof(CalculationContext.ThermalResult)`: calls `NotifyThermalPropertiesChanged()` first, then `Calculate()` so the block both displays the new parameters and recomputes circuit powers.
- Own context changes (`e.Source == "CircuitsViewModel"`) remain ignored, preserving the single-writer rule.

Regression tests added in `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs`:
- `ThermalResultChangedViaContext_NotifiesThermalPropertiesAndRecalculates` — isolates the `UpdateThermal` path and asserts `PropertyChanged` for `PowerUp`, `SupplyTemperature`, `PipeType`, `PipeSpacing_cm` plus non-zero circuit power.
- `ThermalInputsChangedViaContext_NotifiesThermalProperties` — isolates the `UpdateThermalInputs` path and asserts `PropertyChanged` for pipe/spacing/diameter properties without triggering an extra `Calculate()`.

Verification:
- `dotnet build src/SnowMeltingCalculator.csproj -c Debug` → exit 0, 0 errors, 6 pre-existing warnings (CS1998 `CollectorViewModel.cs:191`, CS8604 `ResultsViewModel.cs:518`, CS0169 `MainWindow.xaml.cs:32`) unrelated to this fix.
- `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj` → 1086 passed, 0 failed, 2 skipped (`RegenerateCircuitsBaseline`, `RegenerateBaseline`).
- Targeted gates:
  - `UpdateFromThermalModule_*` → 10 passed.
  - `DoubleCalculationPreventionTests` → 15 passed.
  - `ClimateToHydraulicsIntegrationTests` → 17 passed.
  - `PipeSpacingSynchronizationTests` → 12 passed.
  - `*ViaContext*` (`ThermalResultChangedViaContext_*`, `ThermalInputsChangedViaContext_*`) → 2 passed.
- `dotnet build /p:TreatWarningsAsErrors=true` still fails because the 3 pre-existing warnings above are promoted to errors; none were introduced by this fix and they remain out of scope.

Manual QA:
- Launched `SnowMeltingCalculator.exe`, selected Москва in the Climate tab, navigated to Тепловой расчёт, selected a pipe, set Шаг укладки to 200 мм, set Температура подачи to 45°C, and clicked Рассчитать.
- Switched to Гидравлический расчёт and captured the block "Данные укладки и мощности": Шаг = 20 см.
- Returned to Тепловой расчёт, changed Шаг укладки to 150 мм, changed Температура подачи to 40°C, and clicked Рассчитать again.
- Switched back to Гидравлический расчёт; the block updated to Шаг = 15 см, confirming the thermal-to-hydraulics sync.

Evidence:
- `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-build.log`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-test.log`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-updatefromthermal.log`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-doublecalc.log`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-climate.log`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-pipespacing.log`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-manual-qa.txt`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-manual-qa-before.png`
- `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-manual-qa.png`
