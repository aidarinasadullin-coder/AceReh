# learnings.md

## 2026-07-18 — T1: Remove DeltaT from ThermalInputs + validation + cold-start fallback

### Files changed
- `src/Models/Thermal/ThermalInputs.cs` — removed `DeltaT` property and its XML doc.
- `src/ViewModels/Thermal/ThermalViewModel.cs` — removed `DeltaT = 15.0` line from `BuildThermalInputs()`.
- `src/Services/Thermal/ThermalCalculator.cs` — removed `inputs.DeltaT` validation block from `Validate()`.
- `src/ViewModels/Hydraulics/CircuitsViewModel.cs` — simplified fallback to `thermalResult?.DeltaT ?? (supplyTemperature - returnTemperature)` with updated comment.

### Build result
`dotnet build src/SnowMeltingCalculator.csproj -c Debug -p:TreatWarningsAsErrors=false` → SUCCESS (0 warnings, 0 errors).

### Grep-gate results
- `grep -n "DeltaT" src/Models/Thermal/ThermalInputs.cs` → 0 matches.
- `grep -n "inputs.DeltaT" src/Services/Thermal/ThermalCalculator.cs` → 0 matches.
- `grep -n "thermalInputs?.DeltaT" src/ViewModels/Hydraulics/CircuitsViewModel.cs` → 0 matches.
- `grep -rn "new ThermalInputs" src/` → 1 match in `src/ViewModels/Thermal/ThermalViewModel.cs:418`; construction site no longer references `DeltaT`.

### Issues / notes
- `lsp_diagnostics` could not be run: C# LSP server (`csharp-ls`) is not installed and user has not requested installation. Build success substitutes for static-analysis errors.
- Expected test compile errors remain in test files (`ThermalCalculatorTests.cs`, `ThermalValidatorTests.cs`, `ThermalBaselineTests.cs`) and will be fixed in T2; `dotnet test` was not run per plan.

## 2026-07-18 — T2: Update tests + characterization for cold-start fallback

### Files changed
- `tests/SnowMeltingCalculator.Tests/Thermal/ThermalViewModelTests.cs` — replaced `inputs.DeltaT` in mock `ThermalCalculationResult` construction with explicit `const double fakeDeltaT = 15.0`; removed obsolete `parameters.DeltaT` assertion in `BuildThermalInputs_ReturnsCorrectParameters`.
- `tests/SnowMeltingCalculator.Tests/Thermal/ThermalCalculatorTests.cs` — removed `DeltaT = 15.0` from `CreateValidInputs()`.
- `tests/SnowMeltingCalculator.Tests/Services/Thermal/ThermalValidatorTests.cs` — removed `DeltaT = 15.0` from `CreateValidInputs()`.
- `tests/SnowMeltingCalculator.Tests/RefactorBaseline/ThermalBaselineTests.cs` — removed `DeltaT = 15.0` from `BuildParameters()`.
- `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/CircuitsViewModelColdStartTests.cs` — new characterization test `CircuitsViewModel_ColdStart_NoThermalResult_Uses5KDeltaTFallback` proving cold-start fallback uses `deltaT = 5.0` via observable `FlowRate`.

### Build result
`dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug -p:TreatWarningsAsErrors=false` → SUCCESS (0 errors, 78 pre-existing warnings).

### Test results
- `dotnet test --filter "FullyQualifiedName~ColdStart_NoThermalResult_Uses5KDeltaTFallback"` → 1 passed.
- `dotnet test --filter RefactorBaseline` → 43 passed.
- `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj --logger "console;verbosity=normal"` → 1249 passed, 0 failed, 2 skipped (`RegenerateCircuitsBaseline`, `RegenerateBaseline`).

### Grep-gate result
Multi-line-aware scan for `DeltaT` within 8 lines after every `new ThermalInputs` in `tests/` → 0 matches.

### Issues / notes
- `ThermalViewModelTests.cs` had an additional `parameters.DeltaT` assertion at line 414 not explicitly called out in the plan; it was removed because `ThermalInputs` no longer exposes `DeltaT`.
- `.omo/boulder.json` was updated by the runtime to reflect the active work; it was NOT staged in the commit.


