# Task 13 — WPF user-flow UI QA (V9 harness) raw tables

Generated: 2026-08-23T19:47:41.5043348Z · Result: **FAIL**

| Executable | SHA-256 (frozen) |
|---|---|
| `src\bin\Release\net8.0-windows\win-x64\SnowMeltingCalculator.exe` | `BE36766AF72900F8734B6BADD4EF014C6E0FC689EB459B62651EB2CFF3C6335D` |

## Process records (exe SHA validated before AND after every launch)

| Run tag | Project | PID | Exit | exeSHA before | exeSHA after | stdout log | stderr log |
|---|---|---|---|---|---|---|---|

## Selector registry resolution (17 IDs)

| AutomationId | ControlType | View | Optional | Resolutions | Last present |
|---|---|---|---|---|---|
| ThermalMode | ComboBox | Thermal | False | 0 | False |
| ThermalSupplyTemperature | Edit | Thermal | False | 0 | False |
| ThermalGroundTemperature | Edit | Thermal | False | 0 | False |
| ThermalPipe | ComboBox | Thermal | False | 0 | False |
| ThermalPipeSpacing | ComboBox | Thermal | False | 0 | False |
| ThermalCalculate | Button | Thermal | False | 0 | False |
| ThermalReset | Button | Thermal | False | 0 | False |
| ThermalRecalcMessage | Text | Thermal | True | 0 | False |
| ThermalDeltaT | Text | Thermal | False | 0 | False |
| ThermalPowerTotal | Text | Thermal | False | 0 | False |
| ThermalResultStatus | Text | Thermal | True | 0 | False |
| HydraulicsPipeSpacing | Text | Hydraulics | False | 0 | False |
| HydraulicsSupplyTemperature | Text | Hydraulics | False | 0 | False |
| HydraulicsReturnTemperature | Text | Hydraulics | False | 0 | False |
| ResultsThermalPower | Text | Results | False | 0 | False |
| ResultsSupplyTemperature | Text | Results | False | 0 | False |
| ResultsReturnTemperature | Text | Results | False | 0 | False |

## Ten-step matrix (assertion -> expected -> observed -> artifact)

### Step 1: Verify fixture-manifest.json and all three input SHA-256 values — [FAILED]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | fixture project-a.smc SHA matches manifest | E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87A2A0E3D264119397A | E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87A2A0E3D264119397A | True |
| 2 | fixture project-b.smc SHA matches manifest | FBE377ABAB8A5D3A47086E23A5E4FFFA68B95EAEEE569DEE459CEB0235940882 | FBE377ABAB8A5D3A47086E23A5E4FFFA68B95EAEEE569DEE459CEB0235940882 | True |
| 3 | fixture unknown-pipe.smc SHA matches manifest | D7BA538E14C8C9AC33556540705EECA6C10E8F223BB0DA837463B584F1AB1532 | 339E37F5AD33C1AE6555FEE9D661A6743FE2C051A256420450945C8CE81AEF42 | False |

## Screenshots

| Name | File | Bytes | Dimensions | SHA-256 |
|---|---|---|---|---|

## Post-run note

- `project-a.smc` and `unknown-pipe.smc` are intentionally mutated by the plan-mandated Ctrl+S saves inside this run (task-owned copies); rerun `prepare-ui-fixtures.ps1` before any subsequent V9 invocation to restore deterministic inputs.
- HydraulicsPipeSpacing displays cm (`PipeSpacing_cm` = thermal mm / 10): thermal 300 mm projects as 30.
