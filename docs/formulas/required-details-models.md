# Required Details Models — Phase 1 Audit Contracts

**Project:** SnowMeltingCalculator (REHAU snow-melting calculator)  
**Phase:** 1 — Audit / Baseline  
**Task ID:** task-7-required-details-models  
**Date:** 2026-07-27

This document derives the contracts that any future Phase 2 "detailed calculation" details model / PDF renderer must satisfy. Contracts are produced from the accepted Formula/Branch IDs and their traceability decisions in `docs/formulas/traceability-matrix.md`. No C# implementation is created here.

## Contract notation

| Column | Meaning |
|---|---|
| Contract name | Logical unit a details model must expose. |
| Formula IDs | Accepted Formula/Branch IDs that the contract owns. |
| Cardinality | `OnePerProject`, `OnePerCollector`, `OnePerCircuit`, `OnePerCircuitTemperature`, `OnePerLayer`, or scalar constant. |
| Inputs | Values the contract receives from the calculation surface or user/project data. |
| Intermediates | Values produced inside the calculation that the report must be able to display and that are not currently persisted or are currently lost. |
| Result | Primary computed outputs the contract exposes. |
| Units | Explicit SI or project units for every numeric field. |
| Branch outcome | What happens on successful path, early return, or guard failure. |
| NotCalculated/failure representation | How to distinguish numeric `0` from `NotCalculatedDefault` and how exceptions are surfaced. |
| Lifetime | `Transient` (recomputed per request), `CachedWithResult` (stored alongside the result object), or `Persisted` (saved to project). |
| Why separate | Why this contract is isolated from the general result object. |
| Required characterization tests | Phase 2 test-protection prerequisites derived from `docs/formulas/baseline-coverage.md`. |

---

## 1. Construction Details Contract

| Attribute | Value |
|---|---|
| **Contract name** | `ConstructionDetails` |
| **Formula IDs** | CON-F-002, CON-F-003, CON-F-004, CON-F-005, CON-F-006A, CON-F-006B, CON-F-008, CON-F-009, CON-B-001A, CON-B-001B, CON-B-002, CON-B-003A, CON-B-003B, CON-B-003C, CON-B-007A, CON-B-007B, CON-B-007C, CON-B-010A, CON-B-010B, CON-B-010C, CON-B-010D, CON-B-011A, CON-B-011B, CON-B-011C, CON-B-012 |
| **Cardinality** | `OnePerLayer` for per-layer rows; `OnePerConstruction` for totals. |
| **Inputs** | Layer `Thickness` (mm), material `LambdaA`/`LambdaB` (W/(m·K)), layer `Position`, `GroundwaterLevel` (m). |
| **Intermediates** | Per-layer `CalculatedLambda` (W/(m·K)), per-layer `CalculatedR` (m²·K/W), groundwater dry/wet decision per below-pipe layer. |
| **Result** | `R1Total` (m²·K/W), `R2Total` (m²·K/W), `LambdaE` (W/(m·K)), per-layer resistance table. |
| **Units** | Thermal resistance: m²·K/W; thermal conductivity: W/(m·K); thickness: mm; groundwater level: m. |
| **Branch outcome** | Above-pipe layers always use `LambdaA`; below-pipe layers use `LambdaB` when `GroundwaterLevel < 1.0 m`, otherwise `LambdaA`. Manual override skips update. |
| **NotCalculated/failure representation** | Missing construction returns all fields `NotCalculatedDefault`. Zero-thickness layer contributes `0` to the sum but is a calculated zero, distinct from `NotCalculatedDefault` for the whole construction. Invalid lambda throws; the exception is captured by the caller/validation, not rendered as a numeric value. |
| **Lifetime** | `Transient` for details rendering; source values are `Persisted` in the project. |
| **Why separate** | `ResultsViewModel` and report builders currently only show totals. Per-layer lambda selection, resistance breakdown, and the groundwater branch are required for a traceable PDF details section. |
| **Required characterization tests** | Layer dry/wet lambda selection at 1.0 m boundary; manual-override short-circuit; zero-lambda guard; R1/R2 aggregation with and without layers. |

---

## 2. Thermal Successful Path Contract

| Attribute | Value |
|---|---|
| **Contract name** | `ThermalSuccessfulPathDetails` |
| **Formula IDs** | TH-F-001, TH-F-002, TH-F-003, TH-F-004, TH-F-005, TH-F-007A, TH-F-007B, TH-F-007C, TH-F-007D, TH-F-007E, TH-F-007F, TH-F-007G, TH-F-007H, TH-F-007I, TH-F-007J, TH-F-007K, TH-F-007L, TH-B-007D, TH-F-007M, TH-F-007N, TH-F-007O, TH-F-007P, TH-B-001B, TH-B-002C, TH-B-004A, TH-B-004B, TH-B-006A |
| **Cardinality** | `OnePerProject` per climate/pipe/mode combination. |
| **Inputs** | `surfaceTemp` (°C), `airTemp` (°C), `windSpeed` (m/s), `snowfallIntensity` (mm/h), `R1Total`/`R2Total` (m²·K/W), `lambdaE` (W/(m·K)), pipe outer diameter/wall thickness/thermal conductivity/spacing, coolant density (kg/m³) and heat capacity (kJ/(kg·K)), `supplyTemperature` (°C), `groundTemperature` (°C). |
| **Intermediates** | `deltaTemp` (K), snowfall `h` (m/s), `qMelting` (W/m²), `qConvection` (W/m²), `RFb`/`RD` (m²·K/W), `ParameterM` (1/m), `EfficiencyEtaR` (dimensionless), pipe spacing/diameter/wall thickness in meters, excess-temperature A/B/C/D/E coefficients, `meanTemperature` (°C). |
| **Result** | `Alpha` (W/(m²·K)), `PowerUp` (W/m²), `PowerDown` (W/m²), `ExcessTemperature` (°C), `MeanTemperature` (°C), `SupplyTemperature` (°C), `ReturnTemperature` (°C), `DeltaT` (K), `MeltingHeat` (W/m²), `RadiationHeat` (W/m²), `ConvectionHeat` (W/m²), `PowerTotal` (W/m²), `MassFlowRate` (kg/(h·m²)), `VolumeFlowRate` (l/(h·m²)). |
| **Units** | As above; dimensionless values explicitly marked dimensionless. |
| **Branch outcome** | Successful path computes every field. `deltaTemp <= 0` is clamped to 0.1 K. Rod theory uses `etaR = 1.0` when `|x| < 0.001`, otherwise `tanh(x)/x`. `PowerUp` is reassigned as `MeltingHeat + ConvectionHeat`. |
| **NotCalculated/failure representation** | All numeric fields are calculated; no `NotCalculatedDefault` on the successful path. A separate validity flag must distinguish the successful path from early-return branches. |
| **Lifetime** | `Transient` or `CachedWithResult` inside a new details DTO; current `ThermalCalculationResult` loses intermediates. |
| **Why separate** | `ThermalCalculationResult` only stores final values. The PDF details section needs every intermediate (A–E, qMelting, qConvection, etc.) and must show that `PowerUp` is computed twice by identity. |
| **Required characterization tests** | Non-zero snowfall `PowerUp` melting/convection split; `deltaTemp <= 0` clamp; rod small-x branch both sides of 0.001; mode-to-surface-temp mapping for 3/5/7; flow-rate conversions with explicit cp/rho; repeated PowerUp identity. |

---

## 3. Thermal Validation and Early-Return Contract

| Attribute | Value |
|---|---|
| **Contract name** | `ThermalValidationAndEarlyReturnDetails` |
| **Formula IDs** | TH-B-008A, TH-B-008B, TH-B-008C, TH-B-008D, TH-B-008E, TH-B-008F, TH-B-008G, TH-B-008H, TH-B-008I, TH-B-008J, TH-B-008K, TH-B-008L, TH-B-007A, TH-B-007B, TH-B-007C, TH-B-007E, TH-B-007F |
| **Cardinality** | `OnePerProject` per validation attempt. |
| **Inputs** | All `ThermalInputs`, `ClimateData`, `Construction` (same inputs as `ThermalCalculator.Calculate` and `Validate`). |
| **Intermediates** | Validation error list; `minSupplyTemp` (°C) computed as `Ceiling(MeanTemperature * 10) / 10`. |
| **Result** | `IsValid` (bool), `ValidationErrors` (string[]), partitioned field list: which fields were calculated before the early return and which remain `NotCalculatedDefault`. |
| **Units** | Temperatures in °C, wind speed in m/s, snowfall intensity in mm/h, resistances in m²·K/W. |
| **Branch outcome** | `Validate` returns false and `Calculate` returns early when inputs are null, pipe is null/missing, or any numeric input is outside accepted ranges. Insufficient supply temperature returns after `SupplyTemperature`, `MeanTemperature`, and earlier fields are set. Negative `PowerDown` returns after all fields up to `PowerDown` are set. |
| **NotCalculated/failure representation** | `NotCalculatedDefault` is represented by the C# default value of the corresponding `ThermalCalculationResult` property (`0`, `0.0`, `false`, empty array). The contract must list, per branch, exactly which fields are calculated and which are `NotCalculatedDefault`, and must never treat a calculated zero as `NotCalculatedDefault`. |
| **Lifetime** | `Transient`; the validation result is currently computed on demand. |
| **Why separate** | A details PDF must show *why* a calculation stopped and which values are real versus defaulted. `ResultsViewModel` collapses this distinction. |
| **Required characterization tests** | Every `Validate` guard (null inputs, pipe null, pipe properties, air/ground/wind/snowfall/spacing/R1/R2/lambdaE/supply/coolant ranges); insufficient-supply boundary at `SupplyTemperature == MeanTemperature`; negative `PowerDown` boundary; exception-to-invalid mapping with deterministic failure injection and field partition assertion. |

---

## 4. PowerDown Details Contract

| Attribute | Value |
|---|---|
| **Contract name** | `PowerDownDetails` |
| **Formula IDs** | TH-F-006, TH-B-006A |
| **Cardinality** | `OnePerProject` per thermal request. |
| **Inputs** | `meanTemperature` (°C), `groundTemperature` (°C), `airTemperature` (°C), `RFb`/`RD` (m²·K/W), `etaR` (dimensionless), `pipeSpacing` (mm), `pipeOuterDiameter` (mm), `pipeWallThickness` (mm), `pipeThermalConductivity` (W/(m·K)). |
| **Intermediates** | `jhmuLow` = meanTemperature − groundTemperature (°C); `a` = 1/etaR (dimensionless); `b` = 1/RFb + 1/RD (1/(m²·K/W)); `c` = \|airTemperature − groundTemperature\| (K); `spacingM` (m); `dCoefficient` = spacingM / (π · pipeThermalConductivity) (m·K/W); `wallThicknessM` (m); `outerDiameterM` (m); `eCoefficient` = wallThicknessM / (outerDiameterM − wallThicknessM) (dimensionless); `numerator` (W·K/m²); `denominator` (W·K/m²). |
| **Result** | `PowerDown` (W/m²) plus every intermediate above. |
| **Units** | As above; `a` and `eCoefficient` dimensionless; `b` 1/(m²·K/W); `dCoefficient` m·K/W. |
| **Branch outcome** | Main branch computes `PowerDown = numerator / denominator`. Private method has no internal guards; invalid inputs propagate from caller. |
| **NotCalculated/failure representation** | On negative result the caller sets `IsValid = false` and leaves `PowerTotal`, `MassFlowRate`, `VolumeFlowRate` as `NotCalculatedDefault`. The `PowerDown` field itself is still calculated (even when negative) and must be displayable as a negative intermediate. |
| **Lifetime** | `Transient` inside a details DTO; currently the intermediates are local variables in a private method. |
| **Why separate** | Inherited wisdom explicitly states that `ThermalCalculator.CalculatePowerDown` returns only `PowerDown` while losing `jhmuLow`, A–E, numerator, denominator. The PDF details section must retain these. |
| **Required characterization tests** | Golden values for every intermediate with known inputs; boundary producing negative `PowerDown`; unit conversion correctness for spacing/diameter/wall thickness. |

---

## 5. Rod and Excess Temperature Details Contract

| Attribute | Value |
|---|---|
| **Contract name** | `RodAndExcessTemperatureDetails` |
| **Formula IDs** | TH-F-004, TH-B-004A, TH-B-004B, TH-B-004C, TH-F-005, TH-B-005A, TH-B-005B, TH-B-005C, TH-B-005D, TH-B-005E |
| **Cardinality** | `OnePerProject` per thermal request. |
| **Inputs** | `RFb`/`RD` (m²·K/W), `lambdaE` (W/(m·K)), pipe outer diameter (mm), pipe spacing (mm), `ThermalInputs`, `ClimateData`, `Construction`. |
| **Intermediates** | Rod: `sumReciprocal`, `denominator`, `m`, `x`, `tanhX`. Excess: `a`, `b`, `c`, `spacingM`, `dCoefficient`, `wallThicknessM`, `outerDiameterM`, `eCoefficient`. |
| **Result** | `ParameterM` (1/m), `EfficiencyEtaR` (dimensionless), `ExcessTemperature` (°C). |
| **Units** | As above; `x` dimensionless; `tanhX` dimensionless. |
| **Branch outcome** | Rod small-x branch returns `etaR = 1.0` without calling `Math.Tanh`. Excess validates null inputs and positive ranges before computing. |
| **NotCalculated/failure representation** | Guard failures throw `ArgumentNullException`/`ArgumentOutOfRangeException`; the caller maps exceptions to `IsValid = false` with partial calculated/default field partition. |
| **Lifetime** | `Transient` inside a details DTO. |
| **Why separate** | These two methods produce coefficients that the report must show explicitly; `ThermalCalculationResult` stores only final values. |
| **Required characterization tests** | Rod normal vs small-x threshold; exact `etaR = 1.0` only inside `|x| < 0.001`; every argument guard; excess-temperature conversion-sensitive pipe variants and guard branches. |

---

## 6. Circuit Temperature Details Contract

| Attribute | Value |
|---|---|
| **Contract name** | `CircuitTemperatureDetails` |
| **Formula IDs** | CIR-F-003A, CIR-F-003B, CIR-F-003C, CIR-F-003D, CIR-F-003E, CIR-F-003F, CIR-B-003G, CIR-B-003H, CIR-B-003I, CIR-F-003J, CIR-F-004A, CIR-F-004B, CIR-F-004C, CIR-F-004D, CIR-F-004E, CIR-F-004F, CIR-F-004G, CR-F-001, CR-F-002, CR-F-003, CR-F-004, CR-F-009, CR-F-010, CR-F-011, CR-F-012A, CR-F-012B, CR-F-012C |
| **Cardinality** | `OnePerCircuitTemperature` (operating and design per active circuit). |
| **Inputs** | `FlowRate` (l/h), `innerDiameter` (mm), glycol `Density` (kg/m³) / `KinematicViscosity` (mm²/s), `CircuitLength`/`SupplyLength` (m), valve type, default `Kv` (m³/h). |
| **Intermediates** | `velocity` (m/s), `reynolds` (dimensionless), `flowRegime`, `frictionFactor` λ (dimensionless), `pressureLossPerMeter` (Pa/m), `density_g_cm3` (g/cm³). |
| **Result** | `Velocity` (m/s), `ReynoldsNumber`, `FlowRegime`, `FrictionFactor`, `PressureLossPerMeter` (Pa/m), `DpRohr` (Pa), `DpVerteiler` (Pa), `DpVent` (Pa), `DpGesamt` (Pa), `FlowRate_Ls` (l/s), `TotalLength` (m), `IsActive`, `PressureLossWarning`, `TotalLoss_mbar` (mbar), regime description. |
| **Units** | Flow rate: l/h and l/s; pressure: Pa and mbar; length: m; viscosity: mm²/s; density: kg/m³ and g/cm³. |
| **Branch outcome** | HKV-D branch computes distributor loss from default Kv=1.2 and vent loss from velocity. IV branch swaps the two loss components. Inactive circuits are skipped. |
| **NotCalculated/failure representation** | Null/empty circuit collection returns empty list. Missing operating/design result is treated as `0` in `Max` aggregation, which is a default value, not a calculated pressure. `IsPressureLossPerMeterExceeded` is `false` when the result is missing/default. |
| **Lifetime** | `Transient` for details; `CircuitTemperatureResult` is currently stored on `CircuitRow.OperatingResult`/`DesignResult`. |
| **Why separate** | Report must show per-component pressure losses, unit conversions, and regime details. `ResultsViewModel` and existing report builders display only summarized fields. |
| **Required characterization tests** | HKV-D vs IV 1.25 vs IV 1.5 pressure component split; density kg/m³→g/cm³ conversion; inactive circuit skip; `DpGesamt` = sum of components; `Re=2300` and `Re=4000` regime boundaries; max-pressure-loss predicate. |

---

## 7. Glycol and Interpolation Details Contract

| Attribute | Value |
|---|---|
| **Contract name** | `GlycolInterpolationDetails` |
| **Formula IDs** | GLY-F-001A, GLY-F-001B, GLY-F-002, GLY-F-003A, GLY-F-003B, GLY-F-003C, GLY-F-003D, GLY-F-004A, GLY-F-004B, GLY-F-005A, GLY-F-005B, GLY-F-005C, GLY-F-005D, GLY-F-006, GLY-F-007A, GLY-F-007B, GLY-F-007C, GLY-F-007D, GLY-F-008, GLY-F-009A, GLY-F-009B, GLY-F-009C, GLY-F-009D, GLY-F-010A, GLY-F-010B, GLY-F-010C, GLY-F-010D, GLY-F-010E, GLY-F-010F, GLY-F-011A, GLY-F-011B, GLY-F-011C, GLY-F-012A, GLY-F-012B, GLY-F-012C, GLY-F-012D, GLY-F-012E, GLY-F-013A, GLY-F-013B, GLY-F-013C, GLY-F-014, GLY-F-015A, GLY-F-015B, GLY-F-015C, GLY-F-015D, GLY-F-016A, GLY-F-016B, GLY-F-016C, GLY-F-017A, GLY-F-017B, GLY-F-017C, GLY-F-017D, GLY-F-018A, GLY-F-018B, GLY-F-018C, GLY-F-018D, GLY-F-018E, GLY-F-018F, GLY-F-018G, GLY-F-018H |
| **Cardinality** | `OnePerCircuitTemperature` lookup (operating and design). |
| **Inputs** | `GlycolType`, `GlycolConcentration` (%), temperature (°C), optional JSON file path. |
| **Intermediates** | Cached raw JSON data, selected `GlycolTypeData`, lower indices for concentration and temperature, bilinear interpolation ratio, 1D interpolation values, NaN handling decisions, water-property table interpolated values. |
| **Result** | `Density` (kg/m³), `SpecificHeat` (kJ/(kg·K)), `KinematicViscosity` (mm²/s), `ThermalConductivity` (W/(m·K)). |
| **Units** | As above. |
| **Branch outcome** | Concentration == 0 selects water properties (0–100°C). Ethylene/propylene use bilinear interpolation over ASHRAE matrices. Missing/invalid JSON falls back to embedded default tables. Empty table returns `NaN`. |
| **NotCalculated/failure representation** | Validation throws `ArgumentOutOfRangeException` for unsupported concentration/temperature. Missing/invalid data returns embedded fallback values, not `NotCalculatedDefault`. Unsupported matrix cell returns `NaN`, which must be rendered distinctly from a calculated numeric value. |
| **Lifetime** | `CachedWithResult` for the looked-up `GlycolProperties`; raw data is cached in service instance. |
| **Why separate** | The report details section needs to show whether the result came from water branch, exact grid point, temperature-only, concentration-only, or full bilinear interpolation, plus fallback-vs-file provenance. |
| **Required characterization tests** | Water 0/100/interior temperatures; exact-grid, temperature-only, concentration-only, bilinear interpolation; boundary clamps; NaN handling; missing/null/parse fallback to embedded data; ethylene and propylene end-to-end cases. |

---

## 8. Flow Regime and Friction Details Contract

| Attribute | Value |
|---|---|
| **Contract name** | `FlowRegimeAndFrictionDetails` |
| **Formula IDs** | FR-F-001, FR-F-002A, FR-F-002B, FR-F-002C, FR-F-003A, FR-F-003B, FR-F-003C, FR-F-004, FR-F-005, FR-F-006A, FR-F-006B, FR-F-007A, FR-F-007B, FR-F-007C, FR-F-007D, FR-F-007E |
| **Cardinality** | `OnePerCircuitTemperature` per Reynolds/innerDiameter pair. |
| **Inputs** | `ReynoldsNumber` (dimensionless), `innerDiameter` (mm), optional `roughness` (mm; default 0.007 mm). |
| **Intermediates** | `lambda_lam` at Re=2300, `lambda_turb` at Re=4000, interpolation `ratio`, Blasius initial guess, Colebrook iteration values (`sqrtLambda`, `term1`, `term2`, `newLambda`), iteration count, convergence flag. |
| **Result** | `FlowRegime`, `FrictionFactor` λ (dimensionless), iteration metadata (initial value, max iterations = 20, tolerance = 1e-10, final value, converged flag). |
| **Units** | Dimensionless for λ and Re; mm for diameters/roughness. |
| **Branch outcome** | Re < 2300 → laminar (`64/Re`); 2300 ≤ Re ≤ 4000 → transitional linear interpolation; Re > 4000 → turbulent Colebrook-White with Blasius initial guess. Default roughness 0.007 mm when omitted. Unrecognized enum throws `ArgumentOutOfRangeException`. |
| **NotCalculated/failure representation** | Invalid Re for a branch throws `ArgumentException`. If Colebrook does not converge within 20 iterations, the last λ is returned with `converged = false`. |
| **Lifetime** | `Transient` for details rendering; `FrictionFactor` and `FlowRegime` are stored on `CircuitTemperatureResult`. |
| **Why separate** | Iteration metadata and exact boundary membership are not persisted. The details PDF must show how the friction factor was obtained. |
| **Required characterization tests** | Exact Re=2300 and Re=4000 boundary membership; laminar formula; transitional ratio; Colebrook convergence and non-convergence metadata; default roughness; invalid enum guard. |

---

## 9. Balancing and Collector Summary Details Contract

| Attribute | Value |
|---|---|
| **Contract name** | `BalancingAndCollectorSummaryDetails` |
| **Formula IDs** | CIR-F-005A, CIR-F-005B, CIR-F-005C, CIR-F-005D, CIR-F-005E, CIR-F-005F, CIR-F-005G, CIR-F-005H, CIR-F-005I, CIR-F-005J, CIR-F-006A, CIR-F-006B, CIR-F-006C, CIR-F-006D, CIR-F-006E, CIR-F-006F, CIR-F-006G, CIR-F-007 |
| **Cardinality** | `OnePerCollector` for summary; `OnePerCircuit` for balancing rows. |
| **Inputs** | Active circuit collection, `ValveType`, operating/design `DpGesamt` per circuit, circuit `FlowRate` (l/h), operating `Density` (g/cm³). |
| **Intermediates** | `maxDpGesamt` (Pa), `flowRate_m3h` (m³/h), `throttling_bar` (bar), throttling Kv (m³/h), reference circuit selection epsilon = 0.01 Pa. |
| **Result** | Per circuit: `IsReferenceCircuit` (bool), `Throttling` (Pa), `ValveTurns` (turns), `ValveTurnsWarning`. Per collector: `CircuitCount`, `TotalPipeLength` (m), `TotalPower` (W), `TotalFlowRate` (l/h), `PressureLoss_Operating_Pa`, `PressureLoss_Cold_Pa`, `Kv`, `ValveType`, `ReferenceCircuitNumber`, `Warnings`, `IsValid`. |
| **Units** | Pressure: Pa and bar; length: m; flow rate: l/h and m³/h; turns: turns. |
| **Branch outcome** | Reference circuit has `Throttling = 0`, `ValveTurns = maxTurns`, no warning. Non-reference HKV-D throttles on DpRohr + DpVent; IV throttles on DpRohr + DpVerteiler. `DpVent` is intentionally not recalculated after balancing. Empty/null input returns defaults. |
| **NotCalculated/failure representation** | Empty/no-active collector returns summary with zeroed numeric fields and supplied collector number; these zeros are defaults, not calculated values. `throttling ≤ 0` causes `CalculateKvForThrottling` to return 0. |
| **Lifetime** | `Transient` for details rendering; current `CircuitRow` stores balancing outputs but no cohesive collector snapshot. |
| **Why separate** | Existing fixture never invokes `CalculateBalancing` or `CalculateCollectorSummary`; the details PDF needs a reproducible collector-level snapshot that is currently absent. |
| **Required characterization tests** | Multi-circuit HKV-D, IV 1.25, IV 1.5 balancing; reference selection with epsilon; unchanged DpVent after balancing; empty/no-active collector defaults; max-pressure warning; `CalculateKvForThrottling` zero branch. |

---

## 10. Valve Forward and Inverse Details Contract

| Attribute | Value |
|---|---|
| **Contract name** | `ValveForwardInverseDetails` |
| **Formula IDs** | VT-F-001, VT-F-002, VT-F-003, VT-F-004A, VT-F-004B, VT-F-004C, VT-F-004D, VT-F-004E, VT-F-005, VT-F-006, VT-F-007, VT-F-008A, VT-F-008B, VT-F-008C, VT-F-008D, VT-F-009A, VT-F-009B, VT-F-009C |
| **Cardinality** | `OnePerValve` per circuit/collector. |
| **Inputs** | Forward: `Kv` (m³/h), `ValveType`. Inverse: `Turns` (turns), `ValveType`. |
| **Intermediates** | Forward: polynomial/linear turns before clamp/round. Inverse IV: linear inverse. Inverse HKV-D: Newton-Raphson `target`, `f`, `fPrime`, `newKv`, iteration count, convergence flag. |
| **Result** | Forward: `Turns` (turns), `Warning` (string?). Inverse: `Kv` (m³/h). Per-type max turns and default Kv are also exposed. |
| **Units** | Kv: m³/h; turns: turns. |
| **Branch outcome** | HKV-D forward uses cubic; IV 1¼ and IV 1½ use linear. Turns are clamped to max turns (2.5 for HKV-D, 8.0 for IV), rounded to nearest 0.25. HKV-D inverse solves cubic by Newton-Raphson with max 20 iterations, derivative guard 1e-10, convergence tolerance 1e-6, and clamps result to [0.8, 4.0]. |
| **NotCalculated/failure representation** | Unsupported `ValveType` throws `ArgumentException`. Negative turns throw `ArgumentException`. Forward clamp produces a warning string instead of failure. |
| **Lifetime** | `Transient` for details rendering; current `CircuitRow` stores final `ValveTurns`/`ValveTurnsWarning`. |
| **Why separate** | The PDF details section must expose the forward/inverse polynomials, clamp/rounding behavior, and Newton-Raphson metadata; none of this is currently persisted. |
| **Required characterization tests** | All valve types forward below/at/above clamp; quarter rounding; max-turns warning; default Kv selection; IV linear inverse round-trip; HKV-D Newton convergence, derivative guard, max-iteration fallback, and [0.8, 4.0] clamps; unsupported type and negative-turns guards. |

---

## 11. Circuit Geometry Input Contract

| Attribute | Value |
|---|---|
| **Contract name** | `CircuitGeometryDetails` |
| **Formula IDs** | CR-B-005A, CR-B-005B, CR-B-005C, CR-B-006, CR-B-007, CR-B-008 |
| **Cardinality** | `OnePerCircuit`. |
| **Inputs** | User-provided `CircuitLength` (m), `CircuitArea` (m²), `PipeSpacing_cm` (cm), input-mode flags. |
| **Intermediates** | `IsLengthUserInput`, `IsAreaUserInput`. |
| **Result** | `CircuitLength` (m), `CircuitArea` (m²), `PipeSpacing_cm` (cm), `TotalLength` (m). |
| **Units** | Length: m; area: m²; spacing: cm. |
| **Branch outcome** | Changing length recalculates area when spacing > 0 and length > 0. Changing area recalculates length under the same guards. Changing spacing recalculates the dependent field according to current input mode. |
| **NotCalculated/failure representation** | Default values (`0` length/area, `20.0` cm spacing) are persisted defaults, not calculated. If spacing or input value is not positive, the dependent field is left unchanged. |
| **Lifetime** | `Persisted` in project; derived `TotalLength` is computed on read. |
| **Why separate** | The report must show whether length or area was the user input and the conversion via spacing. This is currently only implicit in observable handlers. |
| **Required characterization tests** | Length-input → area derivation; area-input → length derivation; spacing change with both modes; non-positive guard behavior. |

---

## 12. Circuit Power and Flow Rate Contract

| Attribute | Value |
|---|---|
| **Contract name** | `CircuitPowerAndFlowDetails` |
| **Formula IDs** | CIR-F-001, CIR-B-001A, CIR-B-001B, CIR-B-001C, CIR-B-001D, CIR-F-002, CIR-B-002A, CIR-B-002B, CIR-B-002C, CIR-B-002D |
| **Cardinality** | `OnePerCircuit` per operating/design context. |
| **Inputs** | `CircuitLength`/`SupplyLength` (m), `PipeSpacing_cm`/`SupplySpacing_cm` (cm), `SupplyHeatPercent` (%), `q_up`/`q_down` (W/m²), `Power` (W), `DeltaT` (K), glycol `Density` (kg/m³), `SpecificHeat` (kJ/(kg·K)). |
| **Intermediates** | `lengthPerArea` (m²), `supplyLengthPerArea` (m²), `supplyHeatFactor` (dimensionless), `flowRate_m3h` (m³/h). |
| **Result** | `Power` (W), `FlowRate` (l/h). |
| **Units** | Power: W; flow rate: l/h and m³/h; length: m; spacing: cm. |
| **Branch outcome** | Main branches compute after validation. Negative q_up/q_down or non-positive pipe spacing throws `ArgumentException`; non-positive power/deltaT/density/specificHeat throws `ArgumentException`. |
| **NotCalculated/failure representation** | Validation throws exceptions; there is no `NotCalculatedDefault` path for these helpers. |
| **Lifetime** | `Transient` for details; `CircuitRow.Power` and `CircuitRow.FlowRate` are computed and stored by `CalculateAllCircuits`. |
| **Why separate** | The report details section needs the intermediate area-corrected lengths and heat factor, which are not stored on the row. |
| **Required characterization tests** | Zero-supply-length case; exact cm→m conversion; supply-heat factor; flow-rate conversion with explicit density/cp/deltaT; all guard branches. |

---

## Formula ID coverage summary

All 237 unique Formula/Branch IDs from the accepted inventories are assigned to exactly one of the contracts above or classified as `NotNeededForReport` below. Intentional CR-* duplicates are counted once.

### NotNeededForReport classification

These Formula/Branch IDs are control flow, validation branches, or simple constant accessors whose decision is already captured by a parent contract or result field. They do not require a separate details-model field, but they must remain test-protected in Phase 2.

| Formula IDs | Rationale |
|---|---|
| CIR-B-001A, CIR-B-001B, CIR-B-001C, CIR-B-001D, CIR-B-002A, CIR-B-002B, CIR-B-002C, CIR-B-002D, CIR-B-003I, TH-B-001A, TH-B-002A, TH-B-002B, TH-B-003A, TH-B-003B, TH-B-003C, TH-B-004C, TH-B-005A, TH-B-005B, TH-B-008A, TH-B-008B, TH-B-008C, TH-B-008D, TH-B-008E, TH-B-008F, TH-B-008G, TH-B-008H, TH-B-008I, TH-B-008J, TH-B-008K, TH-B-008L, GLY-F-001A, GLY-F-001B, GLY-F-004A, GLY-F-004B, GLY-F-005A, GLY-F-005B, GLY-F-005C, GLY-F-005D, GLY-F-016A, GLY-F-016B, GLY-F-016C, FR-F-003A, FR-F-003B, FR-F-003C, FR-F-007E, VT-F-001, VT-F-003, VT-F-004E, VT-F-006 | Pure guards, predicates, constant accessors, or thin wrappers. Their behavior is exercised by the characterization tests of the owning contract and by existing focused tests. |
| GLY-F-009A, GLY-F-009B, GLY-F-009C, GLY-F-009D, GLY-F-010A, GLY-F-010B, GLY-F-010C, GLY-F-010D, GLY-F-010E, GLY-F-010F, GLY-F-011A, GLY-F-011B, GLY-F-011C, GLY-F-013A, GLY-F-013B, GLY-F-013C, GLY-F-014, GLY-F-015A, GLY-F-015B, GLY-F-015C, GLY-F-015D, GLY-F-017A, GLY-F-017B, GLY-F-017C, GLY-F-017D, GLY-F-018A, GLY-F-018B, GLY-F-018C, GLY-F-018D, GLY-F-018E, GLY-F-018F, GLY-F-018G, GLY-F-018H | JSON loading, conversion, fallback table construction, and helper interpolation routines. Their observable effect is fully covered by the `GlycolInterpolationDetails` contract tests and by the bilinear/water/fallback characterizations. |

### Trace.<FormulaID> mapping

The traceability matrix assigns `Trace.<FormulaID>` to Ephemeral rows whose intermediate must be captured. The contracts above subsume those traces:

- `Trace.CIR-F-003J` → `CircuitTemperatureDetails.density_g_cm3`
- `Trace.CIR-F-004A` → `BalancingAndCollectorSummaryDetails` empty-input branch
- `Trace.CIR-F-004B`/`Trace.CIR-F-004C` → `GlycolInterpolationDetails` lookup result
- `Trace.CIR-F-004D` → `CircuitTemperatureDetails` default Kv
- `Trace.CIR-F-004G` → `CircuitTemperatureDetails` inactive skip
- `Trace.CIR-F-005A` → `BalancingAndCollectorSummaryDetails` empty-input branch
- `Trace.CIR-F-005B` → `BalancingAndCollectorSummaryDetails` active-filter intermediate
- `Trace.CIR-F-005H` → `BalancingAndCollectorSummaryDetails` throttling Kv intermediate

---

## Numeric zero vs NotCalculatedDefault rule

1. A field is `NotCalculatedDefault` only when an early-return branch executed before the line that would assign it. It is represented by the C# default of the result property (`0`, `0.0`, `false`, empty array/string).
2. A calculated numeric `0` is a value produced by a formula that legitimately yields zero (for example, `lengthPerArea` when `CircuitLength > 0` and `PipeSpacing_cm` is finite but inputs combine to zero, or `Throttling` for the reference circuit).
3. The details model must carry an explicit `Calculated` flag or field-set bitmask per contract so that rendering can distinguish case 1 from case 2.
4. `NaN` from unsupported glycol table cells is a third distinct state: known unsupported, not default and not calculated.

---

## Phase 2 prerequisites derived from baseline coverage

The following gaps from `docs/formulas/baseline-coverage.md` must be closed as characterization tests before product refactoring in Phase 2:

1. Thermal invalid-input, insufficient-supply, negative-PowerDown, and exception-to-invalid early returns — field-by-field calculated vs `NotCalculatedDefault` assertions.
2. Thermal `RFb`, `RD`, rod values, heat components, and private PowerDown intermediates — direct numeric golden cases.
3. Hydraulic fixture expansion: `IV_1_5`, water, propylene, multiple circuits.
4. Balancing and collector summary — immutable multi-circuit numeric snapshots.
5. Glycol interpolation — exact-grid, temperature-only, concentration-only, bilinear, boundary, NaN, malformed/missing data.
6. Valve forward/inverse — all types, clamp/quarter rounding, Newton metadata, clamps.
7. Flow regime — exact Re=2300/Re=4000 boundaries and Colebrook iteration metadata.

---

## Statement

This document was created from accepted Phase 1 audit artifacts only. No `src/**`, `tests/**`, `data/**`, `*.csproj`, `.sln`, baseline fixture, or sample `.smc` files were modified. No C# details models, coordinators, snapshots, builders, renderers, product code, or tests were created.
