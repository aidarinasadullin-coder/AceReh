# Authoritative Calculation Surface Manifest

**Project:** SnowMeltingCalculator (REHAU snow-melting calculator)
**Phase:** 1 — Audit / Baseline
**Task ID:** task-1-manifest-reconciliation
**Date:** 2026-07-27

This manifest enumerates every formula-bearing or behavior-owning symbol that defines the authoritative calculation surface for the thermal and hydraulic calculations. It does **not** include report builders as formula authorities (they are listed only as consumers/gap indicators).

## Scope Rule

- `Included` — symbol contains arithmetic, branching, interpolation, unit conversion, validation, or default/fallback logic that directly affects numeric outputs of the calculation surface.
- `Excluded` — symbol is a pass-through, wiring, persistence, or UI-only member with no calculation authority. Rationale is required for every exclusion.
- `Unresolved inclusion/exclusion decisions:` **0**

## Manifest Columns

`Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory`

---

## 1. Construction Service (`src/Services/Construction/ConstructionService.cs`)

| Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory |
|---|---|---|---|---|---|---|---|---|
| CON-001 | `CalculateThermalResistances` | method | `src/Services/Construction/ConstructionService.cs:36-50` | Included | Triggers `Layer.UpdateLambda` for all layers based on groundwater level, changing λ before R1/R2 aggregation. | Groundwater-driven lambda selection | `Layer.CalculatedLambda`, `Construction.R1Total`, `Construction.R2Total` | Layer.UpdateLambda, LambdaA/B branches |
| CON-002 | `CalculateR1` | method | `src/Services/Construction/ConstructionService.cs:58-73` | Included | Authoritative R1 = Σ(thickness / λ / 1000) for layers above pipe; throws if λ ≤ 0. | `Layer.CalculatedR` aggregation | `ThermalCalculator.CalculateThermalResistance` → `RFb` | Positive-lambda guard, mm→m divisor |
| CON-003 | `CalculateR2` | method | `src/Services/Construction/ConstructionService.cs:82-105` | Included | Authoritative R2 = Σ(thickness / λ / 1000) for layers below pipe; re-applies `UpdateLambda` per call; groundwater branch. | `Layer.UpdateLambda`, groundwater level | `ThermalCalculator.CalculateThermalResistance` → `RD` | Non-negative UGV guard, mm→m divisor, LambdaA/B branch |
| CON-004 | `ValidateConstruction` | method | `src/Services/Construction/ConstructionService.cs:110-115` | Excluded | Delegates to external validator; contains no calculation formula. | — | Validation messages | — |
| CON-005 | `CreateFromTemplate` | method | `src/Services/Construction/ConstructionService.cs:120-166` | Excluded | Factory/assembly logic only; no numeric formulas. | — | `ConstructionModel` | — |
| CON-006 | `ImportMissingMaterialAsync` / `ImportProjectMaterialsAsync` / `ImportProjectTemplatesAsync` | methods | `src/Services/Construction/ConstructionService.cs:171-361` | Excluded | Persistence and catalog import; no calculation behavior. | — | Material catalog | — |
| CON-007 | `GetTotalThicknessAbovePipe` / `GetTotalThicknessBelowPipe` | methods | `src/Services/Construction/ConstructionService.cs:366-383` | Excluded | Summation helpers used for display; no authoritative thermal or hydraulic output depends on them. | — | UI / display | — |

### Related construction model surfaces

| Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory |
|---|---|---|---|---|---|---|---|---|
| CON-MODEL-001 | `Construction.CalculateR1` | method | `src/Models/Construction/Construction.cs:220-223` | Included | Duplicates R1 aggregation at model level; authoritative for callers that use the model directly. | `Layer.CalculatedR` | `IConstructionData.R1Total` | Same as CON-002 |
| CON-MODEL-002 | `Construction.CalculateR2` | method | `src/Models/Construction/Construction.cs:229-232` | Included | Duplicates R2 aggregation at model level; authoritative for callers that use the model directly. | `Layer.CalculatedR` | `IConstructionData.R2Total` | Same as CON-003 |
| CON-MODEL-003 | `Construction.GetLambdaForLayer` | method | `src/Models/Construction/Construction.cs:252-264` | Included | Defines λA/λB selection based on layer position and groundwater level. | Groundwater level, layer position | `Layer.CalculatedLambda` | Above-pipe always λA; below-pipe λB if UGV<1m |
| CON-MODEL-004 | `Construction.R1Total` | property | `src/Models/Construction/Construction.cs:72` | Included | Computed aggregate consumed by thermal calculation. | `Layer.CalculatedR` | `ThermalCalculator` | — |
| CON-MODEL-005 | `Construction.R2Total` | property | `src/Models/Construction/Construction.cs:78` | Included | Computed aggregate consumed by thermal calculation. | `Layer.CalculatedR` | `ThermalCalculator` | — |
| CON-MODEL-006 | `Construction.LambdaE` | property | `src/Models/Construction/Construction.cs:84` | Included | Selects λ of layer closest to pipe (defaults to 1.6 if missing). | Material around pipe | `ThermalCalculator.CalculateRodTheory` | Fallback constant 1.6 |
| CON-MODEL-007 | `Layer.CalculatedR` | property | `src/Models/Construction/Layer.cs:119-127` | Included | Authoritative per-layer R = d / λ / 1000 with zero guard. | Thickness, CalculatedLambda | R1/R2 totals | Zero-lambda guard |
| CON-MODEL-008 | `Layer.UpdateLambda` | method | `src/Models/Construction/Layer.cs:151-166` | Included | Selects λA or λB depending on layer position and groundwater; ignores update if manually overridden. | `IsLambdaOverridden`, position, groundwater | `Layer.CalculatedLambda` | Manual-override branch |
| CON-MODEL-009 | `Layer.Material` setter / `Layer.Thickness` setter / `Layer.CalculatedLambda` setter | properties | `src/Models/Construction/Layer.cs:32-86` | Included | Value-changing handlers that recalculate `CalculatedR` and raise property-changed events. | User/project data | `CalculatedR` | INotifyPropertyChanged side effects |

---

## 2. Thermal Calculator (`src/Services/Thermal/ThermalCalculator.cs`)

| Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory |
|---|---|---|---|---|---|---|---|---|
| TH-001 | `CalculateHeatTransferCoefficient` | method | `src/Services/Thermal/ThermalCalculator.cs:75-97` | Included | Authoritative α = 2.26·ΔT^0.33 + 2.6·v; clamps negative ΔT to 0.1; validates wind speed. | Wind-speed validation, deltaTemp floor branch | `PowerUp`, `ConvectionHeat`, `RFb`, `RD` | ΔT ≤ 0 → 0.1 |
| TH-002 | `CalculatePowerUp` | method | `src/Services/Thermal/ThermalCalculator.cs:115-150` | Included | Authoritative q_FB = q_melting + q_convection; converts snowfall intensity mm/h → m/s. | Snowfall intensity validation, alpha validation, mm/h→m/s conversion | `ThermalCalculationResult.PowerUp` | q_melting formula (h/3600 already folded) |
| TH-003 | `CalculateThermalResistance` | method | `src/Services/Thermal/ThermalCalculator.cs:163-188` | Included | Authoritative RFb = R1 + 1/α and RD = R2 + 1/AlphaBottom; validates all inputs. | R1/R2/alpha validation | `RFb`, `RD`, rod theory, excess temperature, power down | AlphaBottom constant from class |
| TH-004 | `CalculateRodTheory` | method | `src/Services/Thermal/ThermalCalculator.cs:204-259` | Included | Authoritative m and ηR; includes tanh small-x branch (x < 0.001 → 1.0). | Validation of all inputs, small-x branch | `ParameterM`, `EfficiencyEtaR`, excess temperature, power down | RodCoefficient constant, tanh implementation |
| TH-005 | `CalculateExcessTemperature` | method | `src/Services/Thermal/ThermalCalculator.cs:280-357` | Included | Authoritative JHmü = [A + (B − C/(q_FB·RFb·RD))·D·E]·q_FB·RFb; validates ranges. | A/B/C/D/E coefficients | `MeanTemperature`, climate-section temperature chain | Pipe-property unit conversions (mm→m) |
| TH-006 | `CalculatePowerDown` | method | `src/Services/Thermal/ThermalCalculator.cs:385-423` | Included | Authoritative q_D = (JHmü_low·RFb + C·D·E) / (RFb·RD·(A + B·D·E)); private. | Coefficients A/B/C/D/E | `PowerTotal`, `MassFlowRate`, `VolumeFlowRate` | Negative result guard in Calculate |
| TH-007 | `Calculate` | method | `src/Services/Thermal/ThermalCalculator.cs:432-591` | Included | Orchestrates full thermal calculation; owns surface-temp mapping, supply-temp sufficiency branch, return/deltaT formulas, radiation heat reference, and mass/volume flow. | OperatingMode→surfaceTemp cast, supply-temp ceiling guard, negative PowerDown guard | All `ThermalCalculationResult` fields | Multiple validation/early-return branches |
| TH-008 | `Validate` | method | `src/Services/Thermal/ThermalCalculator.cs:601-730` | Included | Authoritative validation ranges for climate, pipe, construction, and coolant inputs; directly affects whether Calculate returns early. | Null checks and numeric ranges | `Calculate` early-return branch | All listed guard ranges |

### Thermal calculator excluded members

| Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory |
|---|---|---|---|---|---|---|---|---|
| TH-EX-001 | `IThermalCalculator` interface | interface | `src/Services/Thermal/IThermalCalculator.cs` | Excluded | Contract only; all formula authority lives in implementation. | — | — | — |
| TH-EX-002 | Physical constants (`SnowDensity`, `IceHeatCapacity`, `IceMeltingHeat`, `WaterHeatCapacity`, `EmissionCoefficient`, `StefanBoltzmann`, `AlphaBottom`, `RodCoefficient`) | fields | `src/Services/Thermal/ThermalCalculator.cs` (class level) | Excluded | Constants are operands, not behavior; listed here for inventory only. | — | All thermal formulas | Inventory captured in TH rows |

---

## 3. Circuits Calculator (`src/Services/Hydraulics/CircuitsCalculator.cs`)

| Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory |
|---|---|---|---|---|---|---|---|---|
| CIR-001 | `CalculateCircuitPower` | method | `src/Services/Hydraulics/CircuitsCalculator.cs:20-40` | Included | Authoritative Q_HK = (lengthPerArea + supplyLengthPerArea·q_zul)·(q_up + q_down); contains length/area conversion and supply-heat factor. | Pipe-spacing conversion (cm), supply-spacing branch | `CircuitRow.Power` | 100.0 divisors |
| CIR-002 | `CalculateFlowRate` | method | `src/Services/Hydraulics/CircuitsCalculator.cs:42-60` | Included | Authoritative V_dot (l/h) = P·3.6 / (ρ·c_p·ΔT)·1000; validates all inputs. | Power/deltaT/density/cp validation | `CircuitRow.FlowRate`, `FlowRate_Ls` | m³/h → l/h factor |
| CIR-003 | `CalculateAtTemperature` | method | `src/Services/Hydraulics/CircuitsCalculator.cs:62-120` | Included | Authoritative velocity, Reynolds, regime, friction factor, pressure-loss-per-meter, DpRohr, DpVerteiler, DpVent; contains HKV-D vs IV branch. | Valve-type branch, glycol density conversion (kg/m³→g/cm³) | `CircuitTemperatureResult` | All hydraulic pressure-loss formulas |
| CIR-004 | `CalculateAllCircuits` | method | `src/Services/Hydraulics/CircuitsCalculator.cs:122-185` | Included | Iterates circuits, skips inactive, calls power/flow/temperature methods for operating and design temperatures. | Active-circuit skip branch | Per-circuit `Power`, `FlowRate`, `OperatingResult`, `DesignResult` | Kv default from `ValveTurnsCalculator` |
| CIR-005 | `CalculateBalancing` | method | `src/Services/Hydraulics/CircuitsCalculator.cs:187-241` | Included | Authoritative reference-circuit selection, throttling pressure, Kv-for-throttling, valve turns; contains HKV-D vs IV branch and comment about not recalculating DpVent. | Reference-circuit epsilon branch, HKV-D/IV throttling branch | `CircuitRow.Throttling`, `ValveTurns`, `ValveTurnsWarning`, `IsReferenceCircuit` | DpVent not recalculated post-balancing |
| CIR-006 | `CalculateCollectorSummary` | method | `src/Services/Hydraulics/CircuitsCalculator.cs:243-285` | Included | Aggregates max DpGesamt (operating/cold), totals, and max-pressure warning. | Active-circuit guards, max aggregation, pressure-threshold branch | `CollectorSummary` | `MaxAllowedPressure_Pa` constant |
| CIR-007 | `CalculateKvForThrottling` | method | `src/Services/Hydraulics/CircuitsCalculator.cs:287-299` | Included | Authoritative Kv = (V/1000) / sqrt(Δp_bar / ρ); returns 0 if throttling ≤ 0; validates density. | Throttling ≤ 0 branch | `CalculateBalancing` → `ValveTurnsCalculator.CalculateTurnsWithWarning` | bar/Pa conversion |

---

## 4. Flow Regime Calculator (`src/Services/Hydraulics/FlowRegimeCalculator.cs`)

| Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory |
|---|---|---|---|---|---|---|---|---|
| FR-001 | `LaminarBoundary` / `TurbulentBoundary` / `PEXaRoughness` | constants | `src/Services/Hydraulics/FlowRegimeCalculator.cs:25-35` | Included | Threshold constants that drive regime selection and friction branches. | — | Regime predicates and friction formulas | — |
| FR-002 | `DetermineFlowRegime` | method | `src/Services/Hydraulics/FlowRegimeCalculator.cs:42-50` | Included | Authoritative three-way regime predicate (Re < 2300, 2300–4000, > 4000). | Boundary constants | `FlowRegime`, `CalculateFrictionFactor` | — |
| FR-003 | `IsLaminar` / `IsTransitional` / `IsTurbulent` | methods | `src/Services/Hydraulics/FlowRegimeCalculator.cs:57-80` | Included | Public regime predicates with same boundaries. | Boundary constants | Consumers/tests | — |
| FR-004 | `CalculateLaminarFrictionFactor` | method | `src/Services/Hydraulics/FlowRegimeCalculator.cs:89-95` | Included | Authoritative λ = 64 / Re; validates Re > 0. | Re > 0 guard | `CalculateFrictionFactor` | — |
| FR-005 | `CalculateTransitionalFrictionFactor` | method | `src/Services/Hydraulics/FlowRegimeCalculator.cs:106-125` | Included | Authoritative linear interpolation between laminar and turbulent values at boundaries. | Boundary validation | `CalculateFrictionFactor` | λ_lam at 2300, λ_turb at 4000 |
| FR-006 | `CalculateTurbulentFrictionFactor` | method | `src/Services/Hydraulics/FlowRegimeCalculator.cs:136-167` | Included | Authoritative Colebrook-White iterative solution; Blasius initial guess; convergence branch. | Iteration convergence guard (1e-10) | `CalculateFrictionFactor` | Max 20 iterations |
| FR-007 | `CalculateFrictionFactor` | method | `src/Services/Hydraulics/FlowRegimeCalculator.cs:176-192` | Included | Dispatches to laminar/transitional/turbulent branch; default roughness 0.007 mm. | Regime dispatch | `CircuitsCalculator.CalculateAtTemperature` | Default roughness constant |
| FR-008 | `GetFlowRegimeDescription` / `GetFlowRegimeRecommendation` | methods | `src/Services/Hydraulics/FlowRegimeCalculator.cs:199-224` | Excluded | Display text only; no numeric effect. | — | UI | — |

---

## 5. Valve Turns Calculator (`src/Services/Hydraulics/ValveTurnsCalculator.cs`)

| Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory |
|---|---|---|---|---|---|---|---|---|
| VT-001 | `KV_HKV_D` / `KV_IV_1_25` / `KV_IV_1_5` / `MaxTurns` | constants | `src/Services/Hydraulics/ValveTurnsCalculator.cs:24-44` | Included | Default Kv and max-turns constants; `MaxTurns` is obsolete but still a numeric constant in surface. | — | `GetDefaultKv`, `GetMaxTurns` | `MaxTurns` marked obsolete; use `GetMaxTurns` |
| VT-002 | `GetMaxTurns` | method | `src/Services/Hydraulics/ValveTurnsCalculator.cs:64-73` | Included | Authoritative max turns per valve type (HKV-D=2.5, IV=8.0). | Valve-type switch | `CalculateTurnsWithWarning`, report warnings | — |
| VT-003 | `CalculateTurns` | method | `src/Services/Hydraulics/ValveTurnsCalculator.cs:96-100` | Included | Forward wrapper returning turns only. | `CalculateTurnsWithWarning` | UI / report | — |
| VT-004 | `CalculateTurnsWithWarning` | method | `src/Services/Hydraulics/ValveTurnsCalculator.cs:121-147` | Included | Authoritative forward calculation + max-turns clamp + 0.25 rounding + warning generation. | Per-type formula branch, max-turns clamp, Math.Round(*4)/4 | `CircuitRow.ValveTurns`, `ValveTurnsWarning` | HKV-D cubic, IV linear |
| VT-005 | `GetDefaultKv` | method | `src/Services/Hydraulics/ValveTurnsCalculator.cs:155-164` | Included | Authoritative default Kv selection by valve type. | Valve-type switch | `CircuitsCalculator.CalculateAllCircuits`, `CollectorSummary.Kv` | — |
| VT-006 | `GetValveTypeName` | method | `src/Services/Hydraulics/ValveTurnsCalculator.cs:171-180` | Excluded | Display string only. | — | UI | — |
| VT-007 | `IsValidKv` | method | `src/Services/Hydraulics/ValveTurnsCalculator.cs:188-197` | Included | Range validation for Kv per valve type (used by tests/consumers). | Valve-type ranges | Tests, potential UI validation | HKV-D 0.8–4.0, IV 1¼ 0.5–3.0, IV 1½ 0.5–3.5 |
| VT-008 | `CalculateKvFromTurns` | method | `src/Services/Hydraulics/ValveTurnsCalculator.cs:217-229` | Included | Inverse function dispatcher; validates turns ≥ 0. | Valve-type switch, negative-turns guard | Tests, potential calibration tools | — |
| VT-009 | `CalculateTurnsIV_1_5` / `CalculateTurnsIV_1_25` / `CalculateTurnsHKV_D` | private methods | `src/Services/Hydraulics/ValveTurnsCalculator.cs:239-263` | Included | Forward polynomial/linear formulas. | — | `CalculateTurnsWithWarning` | Exact coefficients |
| VT-010 | `CalculateKvFromTurnsIV_1_5` / `CalculateKvFromTurnsIV_1_25` | private methods | `src/Services/Hydraulics/ValveTurnsCalculator.cs:269-281` | Included | Linear inverse formulas. | — | `CalculateKvFromTurns` | Exact coefficients |
| VT-011 | `CalculateKvFromTurnsHKV_D` | private method | `src/Services/Hydraulics/ValveTurnsCalculator.cs:288-324` | Included | Newton-Raphson solution of cubic for HKV-D; includes convergence branch and Kv clamp 0.8–4.0. | Newton iteration, fPrime near-zero guard, Kv clamps | `CalculateKvFromTurns` | Max 20 iterations, 1e-6 tolerance |

---

## 6. Glycol Data Service (`src/Services/Hydraulics/GlycolDataService.cs`)

| Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory |
|---|---|---|---|---|---|---|---|---|
| GLY-001 | `MIN_TEMPERATURE` / `MAX_TEMPERATURE` | constants | `src/Services/Hydraulics/GlycolDataService.cs:38-43` | Included | Supported temperature bounds used by validation and water branch. | — | `ValidateParameters`, `IsTemperatureSupported`, `GetWaterProperties` | — |
| GLY-002 | `GetProperties` | method | `src/Services/Hydraulics/GlycolDataService.cs:68-96` | Included | Main entry point; water branch at concentration 0%; dispatches bilinear interpolation for all four properties. | Concentration == 0 branch | `CircuitsCalculator` (density, cp, viscosity) | — |
| GLY-003 | `GetDensity` / `GetSpecificHeat` / `GetKinematicViscosity` / `GetThermalConductivity` | methods | `src/Services/Hydraulics/GlycolDataService.cs:105-164` | Included | Single-property wrappers validating and interpolating from table. | `ValidateParameters`, `InterpolateProperty` | `CircuitsCalculator.CalculateFlowRate`, `CalculateAtTemperature` | — |
| GLY-004 | `IsTemperatureSupported` / `IsConcentrationSupported` | methods | `src/Services/Hydraulics/GlycolDataService.cs:171-188` | Included | Range predicates; concentration 0% allowed for water. | 0% water exception, `ValidationConstants` bounds | Validation and tests | — |
| GLY-005 | `GetMinTemperature` / `GetMaxTemperature` / `GetMinConcentration` / `GetMaxConcentration` | methods | `src/Services/Hydraulics/GlycolDataService.cs:194-212` | Included | Expose bounds to consumers/tests. | Constants / `ValidationConstants` | Validation, tests | — |
| GLY-006 | `GetWaterProperties` | method | `src/Services/Hydraulics/GlycolDataService.cs:226-250` | Included | Authoritative water property assembly; validates 0–100°C; uses table interpolation + approximation for cp. | Temperature range guard | `GetProperties` concentration==0 branch | Water density/viscosity/conductivity tables |
| GLY-007 | `GetWaterDensity` / `GetWaterKinematicViscosity` / `GetWaterSpecificHeat` / `GetWaterThermalConductivity` | private methods | `src/Services/Hydraulics/GlycolDataService.cs:255-305` | Included | Water property tables and linear interpolation; cp uses linear approximation. | `LinearInterpolateTable` | `GetWaterProperties` | IAPWS table values, cp approximation |
| GLY-008 | `LinearInterpolateTable` | private method | `src/Services/Hydraulics/GlycolDataService.cs:310-336` | Included | Linear interpolation for 1D water tables with empty-array and boundary guards. | Empty-array guard, lower/upper clamp | Water property methods | Returns first/last value outside range |
| GLY-009 | `LoadData` | private method | `src/Services/Hydraulics/GlycolDataService.cs:343-388` | Included | JSON file loading with file-missing fallback and parse-exception fallback to embedded default data. | File.Exists branch, null-container branch, catch branch | All `Get*` methods | Cached with lock |
| GLY-010 | `ConvertToInterpolationFormat` / `ConvertGlycolTypeData` / `GetArrayValue` | private methods | `src/Services/Hydraulics/GlycolDataService.cs:393-502` | Included | JSON-to-matrix conversion; NaN for missing values; array-bounds handling. | Null raw-data branches, `GetArrayValue` NaN | `InterpolateProperty` | NaN semantics for missing table cells |
| GLY-011 | `GetGlycolData` | private method | `src/Services/Hydraulics/GlycolDataService.cs:507-515` | Included | Selects ethylene/propylene data; fallback to embedded defaults if missing. | GlycolType switch, fallback defaults | `GetProperties`, single-property getters | — |
| GLY-012 | `InterpolateProperty` | private method | `src/Services/Hydraulics/GlycolDataService.cs:520-581` | Included | Authoritative bilinear interpolation over concentration×temperature; handles exact match, 1D only, and full bilinear cases. | `FindLowerIndex`, `LinearInterpolateWithNaN` | All glycol property getters | Exact/linear/bilinear branches |
| GLY-013 | `LinearInterpolateWithNaN` | private method | `src/Services/Hydraulics/GlycolDataService.cs:586-600` | Included | Linear interpolation with NaN handling: both NaN → NaN, one NaN → other value. | NaN branches | `InterpolateProperty` | Boundary/NaN behavior |
| GLY-014 | `LinearInterpolate` | private method | `src/Services/Hydraulics/GlycolDataService.cs:605-612` | Included | Plain linear interpolation; returns y1 if x2≈x1. | Degenerate-interval guard | `LinearInterpolateWithNaN` | — |
| GLY-015 | `FindLowerIndex` | private method | `src/Services/Hydraulics/GlycolDataService.cs:617-635` | Included | Lower-bound index search with clamping. | Empty-array, below-min, above-max branches | `InterpolateProperty` | — |
| GLY-016 | `ValidateParameters` | private method | `src/Services/Hydraulics/GlycolDataService.cs:640-666` | Included | Validates concentration and temperature ranges; water 0% special case. | Concentration 0% branch, glycol/water temperature bounds | All public getters | — |
| GLY-017 | `GetDefaultData` / `GetDefaultEthyleneData` / `GetDefaultPropyleneData` / `CreateDefaultTable` | private methods | `src/Services/Hydraulics/GlycolDataService.cs:671-734` | Included | Embedded fallback tables when JSON is missing or invalid. | — | `LoadData` fallback branches | ASHRAE fallback matrices |
| GLY-018 | Default ethylene/propylene value arrays | private methods | `src/Services/Hydraulics/GlycolDataService.cs:736-1108` | Included | Fallback numeric matrices (contain NaN for unsupported T/concentration combos). | — | Fallback tables | Audit inventory captured in GLY-017 |

---

## 7. Circuit Row / CircuitTemperatureResult (`src/Models/Hydraulics/CircuitRow.cs`)

| Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory |
|---|---|---|---|---|---|---|---|---|
| CR-001 | `CircuitTemperatureResult.MaxPressureLossPerMeter` | field | `src/Models/Hydraulics/CircuitRow.cs:18` | Included | Constant threshold R ≤ 300 Pa/m used by pressure-loss warning. | — | `IsPressureLossPerMeterExceeded`, `PressureLossWarning` | — |
| CR-002 | `CircuitTemperatureResult.IsPressureLossPerMeterExceeded` | property | `src/Models/Hydraulics/CircuitRow.cs:68` | Included | Predicate comparing pressure loss to max threshold. | `MaxPressureLossPerMeter` | `PressureLossWarning` | — |
| CR-003 | `CircuitTemperatureResult.DpGesamt` | property | `src/Models/Hydraulics/CircuitRow.cs:137` | Included | Authoritative total pressure loss = DpRohr + DpVerteiler + DpVent. | Sum of settable result fields | `CollectorSummary.PressureLoss`, balancing reference, report | — |
| CR-004 | `CircuitRow.TotalLength` | property | `src/Models/Hydraulics/CircuitRow.cs:218` | Included | Authoritative total length = CircuitLength + SupplyLength. | Sum of observable/user inputs | `CircuitsCalculator.CalculateAtTemperature`, `CollectorSummary.TotalPipeLength`, report | — |
| CR-005 | `CircuitRow.CircuitArea` / `CircuitLength` / `PipeSpacing_cm` | observable properties | `src/Models/Hydraulics/CircuitRow.cs:207-236` | Included | Value-changing fields with handlers that drive area↔length conversion. | User input flags | `OnCircuitLengthChanged`, `OnCircuitAreaChanged`, `OnPipeSpacing_cmChanged` | — |
| CR-006 | `CircuitRow.OnCircuitLengthChanged` | partial method | `src/Models/Hydraulics/CircuitRow.cs:246-279` | Included | Sets input mode, computes area = length · spacing / 100 when length changes. | IsLengthUserInput, IsAreaUserInput, spacing > 0 guard | `CircuitArea` | cm→m conversion |
| CR-007 | `CircuitRow.OnCircuitAreaChanged` | partial method | `src/Models/Hydraulics/CircuitRow.cs:284-317` | Included | Sets input mode, computes length = area · 100 / spacing when area changes. | IsAreaUserInput, IsLengthUserInput, spacing > 0 guard | `CircuitLength` | cm→m conversion |
| CR-008 | `CircuitRow.OnPipeSpacing_cmChanged` | partial method | `src/Models/Hydraulics/CircuitRow.cs:322-351` | Included | Recalculates dependent area or length when spacing changes. | IsLengthUserInput / IsAreaUserInput, positive-value guards | `CircuitArea` or `CircuitLength` | cm conversion |
| CR-009 | `CircuitRow.FlowRate_Ls` | property | `src/Models/Hydraulics/CircuitRow.cs:380` | Included | Conversion l/h → l/s. | `FlowRate` | UI / report | — |
| CR-010 | `CircuitRow.IsActive` | property | `src/Models/Hydraulics/CircuitRow.cs:450` | Included | Active flag derived from circuit length; affects aggregation and balancing skips. | `CircuitLength` | `CalculateAllCircuits`, `CalculateBalancing`, `CalculateCollectorSummary` | — |
| CR-011 | `CircuitRow.PressureLossWarning` | property | `src/Models/Hydraulics/CircuitRow.cs:459-462` | Included | Warning text for R > 300 Pa/m. | `OperatingResult.PressureLossPerMeter` | UI / report | Only operating temperature |
| CR-012 | `CircuitRow.CurrentResult` / `TotalLoss_mbar` / `FlowRegimeDescription` | properties | `src/Models/Hydraulics/CircuitRow.cs:478-495` | Included | Display-mode selection and unit conversion (Pa → mbar). | `DisplayMode` | UI / report | — |

### CircuitRow excluded members

| Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory |
|---|---|---|---|---|---|---|---|---|
| CR-EX-001 | `IsLengthReadOnly` / `IsAreaReadOnly` | properties | `src/Models/Hydraulics/CircuitRow.cs:189-194` | Excluded | UI read-only flags derived from input mode; no numeric output authority. | — | UI binding | — |
| CR-EX-002 | Plain observable auto-properties without handlers (CircuitNumber, SupplyLength, SupplyHeatPercent, Power, FlowRate, Velocity, OperatingResult, DesignResult, Throttling, RecommendedValveSetting, ValveTurns, ValveTurnsWarning, IsReferenceCircuit, DisplayMode) | properties | `src/Models/Hydraulics/CircuitRow.cs` | Excluded | Storage/backing fields; calculation authority lives in handlers and calculators that mutate them. | — | Calculators / UI | Listed for inventory |

---

## 8. Report Section Builders (`src/Services/Reports/Calculation/Builders/*.cs`)

These files are **explicitly not formula authorities**. They consume values from `ProjectData` and expose formula strings for documentation/gap indication. They are included in the manifest only as gap indicators and consumers.

| Surface ID | Symbol | Kind | Source path:lines | Included/Excluded | Reason | Formula/branch owners | Downstream values | Audit inventory |
|---|---|---|---|---|---|---|---|---|
| REP-001 | `ProjectSectionBuilder.Build` | method | `src/Services/Reports/Calculation/Builders/ProjectSectionBuilder.cs:11-31` | Excluded | No formulas; copies project metadata. | — | Report project section | — |
| REP-002 | `ClimateSectionBuilder.Build` | method | `src/Services/Reports/Calculation/Builders/ClimateSectionBuilder.cs:13-84` | Excluded (consumer/gap indicator) | Contains formula strings (`T_return`, `T_mean`, `DeltaT`, `t_P`) pointing to authoritative sources in `ThermalCalculator.Calculate`/`Validate`. Does not compute numeric values. | `ThermalCalculator` | Climate section of report | Gap indicator for TH-007/TH-008 |
| REP-003 | `ConstructionSectionBuilder.Build` | method | `src/Services/Reports/Calculation/Builders/ConstructionSectionBuilder.cs:13-74` | Excluded (consumer/gap indicator) | Contains formula strings for R_i, R1, R2, lambdaA/lambdaB pointing to `Layer.CalculatedR`, `ConstructionService`, and documentation. Does not compute numeric values. | `Layer.CalculatedR`, `ConstructionService.CalculateR1/R2` | Construction section of report | Gap indicator for CON-002/CON-003/CON-MODEL-007 |
| REP-004 | `ThermalSectionBuilder.Build` | method | `src/Services/Reports/Calculation/Builders/ThermalSectionBuilder.cs:16-120` | Excluded (consumer/gap indicator) | Contains many formula strings and intermediate-coefficient metadata pointing to `ThermalCalculator`. Does not compute numeric values. | `ThermalCalculator` all methods | Thermal section of report | Gap indicator for TH-001 through TH-008 |
| REP-005 | `HydraulicsSectionBuilder.Build` | method | `src/Services/Reports/Calculation/Builders/HydraulicsSectionBuilder.cs:14-37` | Excluded (consumer/gap indicator) | Consumes `ProjectData`; formula strings for circuit area/length/pressure losses point to `CircuitsCalculator`/`CircuitRow`. Does not compute numeric values. | `CircuitsCalculator`, `CircuitRow`, `FlowRegimeCalculator`, `ValveTurnsCalculator` | Hydraulics section of report | Gap indicator for CIR-001/003, CR-003/004, FR, VT |
| REP-006 | `HydraulicsReportMetadataBuilder.BuildFormulas` | method | `src/Services/Reports/Calculation/Builders/HydraulicsReportMetadataBuilder.cs:30-59` | Excluded (consumer/gap indicator) | Central registry of hydraulics formula strings with source path hints. Not an authority. | All hydraulic calculators | Formulas appendix | Gap indicator / inventory |
| REP-007 | `EquipmentSectionBuilder.Build` | method | `src/Services/Reports/Calculation/Builders/EquipmentSectionBuilder.cs:13-110` | Excluded (consumer/gap indicator) | Aggregates project-level totals and contains formula strings for system volume, pump head, etc. Derived from `ProjectData`, not authoritative. | `CollectorSummary` / `CircuitRow` | Equipment section of report | Gap indicator for collector-level aggregation |
| REP-008 | `CalculationReportDataBuilder.Build` | method | `src/Services/Reports/Calculation/CalculationReportDataBuilder.cs:66-120` | Excluded (consumer) | Orchestrates section builders; no formulas. | — | `CalculationReportData` | — |
| REP-009 | `CalculationReportDataBuilder.CollectWarnings` | method | `src/Services/Reports/Calculation/CalculationReportDataBuilder.cs:130-241` | Excluded (consumer) | Compares already-calculated values against constants and emits warnings; no formula authority. | `ValidationConstants`, `CircuitTemperatureResult.MaxPressureLossPerMeter`, `ValveTurnsCalculator.GetMaxTurns` | Report warnings | Threshold checks only |

---

## Summary

- **Total Included surfaces:** 74
- **Total Excluded surfaces:** 19
- **Unresolved inclusion/exclusion decisions:** 0
- **Formula authorities:** `ConstructionService`, `Construction` model, `Layer`, `ThermalCalculator`, `CircuitsCalculator`, `FlowRegimeCalculator`, `ValveTurnsCalculator`, `GlycolDataService`, `CircuitTemperatureResult`, `CircuitRow` value-changing handlers and computed properties.
- **Consumers/gap indicators only:** Report builders under `src/Services/Reports/Calculation/Builders/` and `CalculationReportDataBuilder`.

This manifest is the authoritative scope for Phase 1 audit. Any numeric discrepancy traceable to symbols listed as `Included` is in scope; symbols listed as `Excluded` are out of scope unless they later prove to contain hidden calculation branches.
