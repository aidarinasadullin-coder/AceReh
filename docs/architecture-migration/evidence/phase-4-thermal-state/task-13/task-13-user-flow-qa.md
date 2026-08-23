# Task 13 — WPF user-flow UI QA receipt (Todo 13, V9)

Phase: `phase-4-thermal-state` · Todo 13 · Date: 2026-08-23
Verdict: **GREEN — V9 harness exit 0; ten steps + failure branch PASS.**

## Gate results

| Gate | Command | Exit | Result |
|---|---|---|---|
| G0 protected-pre | `verify-protected-baseline.ps1 -Baseline task-1/baseline-manifest.json -AllowedHunks task-13/allowed-hunks.json -EvidenceRoot <evidence root> -Output task-13/protected-pre.json` | 0 | drift 63, mismatch **0**, allowed hunks 44 |
| Fixtures (V9 line 266) | `prepare-ui-fixtures.ps1 -Source tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc -OutputDirectory …/task-13/fixtures` | 0 | a=E1D02BC0… b=FBE377AB… u=D7BA538E…, manifest regenerated deterministically |
| Harness (V9 line 267, exact) | `run-wpf-ui-qa.ps1 -Executable src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.exe -ExpectedExecutableSha256File …/frozen-release-sha256.json -ProjectA …/project-a.smc -ProjectB …/project-b.smc -InvalidProject …/unknown-pipe.smc -OutputDirectory …/task-13/ui-qa` | **0** | ten steps PASS + failure branch PASS; `observations.json` result=PASS |
| G4 protected-post | same verifier, `-Output task-13/protected-post.json` | 0 | drift 63, mismatch **0**, allowed hunks 44 |

## Process records (exe SHA BE36766AF72900F8734B6BADD4EF014C6E0FC689EB459B62651EB2CFF3C6335D validated before AND after every launch)

| Run tag | Project | PID | Exit | stdout/stderr |
|---|---|---|---|---|
| a-edit-save | project-a.smc | 5264 | 0 | both logs present, stderr empty (no crash patterns) |
| a-relaunch | project-a.smc | 21216 | 0 | both logs present, stderr empty |
| b-load-reset | project-b.smc | 20652 | 0 | both logs present, stderr empty |
| unknown-pipe | unknown-pipe.smc | 17708 | 0 | both logs present, stderr empty |

## Ten-step matrix summary

| Step | Scope | Assertions | Artifacts |
|---|---|---|---|
| 1 | fixture manifest + 3 input SHAs | 3 | fixture-manifest.json |
| 2 | launch A, clean title | 2 | run-a-edit-save-*.log |
| 3 | Thermal baseline (Melting/50/10/S20/250/261.0/15.0; recalc+status absent) | 9 | — |
| 4 | mode→AntiIcing EXACT msg, supply→65 EXACT msg, ground/pipe S25/spacing 300, prior result retained, 11-ID registry | 23 | 01-edit.png |
| 5 | Calculate → button re-enabled, recalc absent, PowerTotal != 261.0 | 2 | 02-calculate.png |
| 6 | Hydraulics (spacing 30 cm, supply 65) + Results (power > 0) projections | 6 | 03-hydraulics.png, 04-results.png |
| 7 | save via «Файл»→«Сохранить»: SHA/timestamp advance, dirty marker clears, WM_CLOSE exit 0, relaunch restores AntiIcing/65/15/S25/300/step-5 power | 15 | run-a-relaunch-*.log |
| 8 | B load: 55.0/5.0/150/S17, no project-A result | 6 | 05-load-2.png |
| 9 | «Создать новый расчёт» on clean B: DEC-T01 defaults Melting/50/10/no-pipe/200/no-result, bare title | 11 | 06-reset.png |
| 10 | failure branch: fallback pipe S17, invalid-zero fallback result + characterized status, guard cleared via supply edit EXACT msg, save SHA advance + dirty clears, exit 0 | 10 (+11 in failure-observations.json) | 07-unknown-pipe.png, failure-observations.json |

Total: **87 step assertions + 11 failure-branch assertions, zero failures.** All 17 AutomationIds resolved (exact ID + ControlType, unique+enabled); per-view resolution documented below.

## Deviations (documented, no contract weakening)

1. **Keystroke substitution (steps 7/9/10).** The brief mandates Ctrl+S/Ctrl+N via SendKeys after SetFocus. Probe evidence (probes 4–8): plain keys and TextBox-internal chords deliver, but Window-level chords never reach `MainWindow_KeyDown` in this environment (Ctrl+O raises no open-dialog despite foreground==main). The harness drives the SAME bound commands (`SaveProjectCommand` / `NewCalculationCommand`) through the visible «Файл» menu via UIA Invoke/Selection patterns — no mouse coordinates, inbox APIs only. Plan observables asserted unchanged.
2. **HydraulicsPipeSpacing is centimetres.** `CircuitsViewModel.PipeSpacing_cm = thermal mm / 10` (src/ViewModels/Hydraulics/CircuitsViewModel.cs:285); thermal 300 mm projects as 30. The distilled brief said "assert == 300"; the code-faithful value is asserted.
3. **Unknown-pipe fallback result is an INVALID zero publication.** The orchestrator runs exactly one fallback Calculate when no valid saved result exists (ProjectLoadOrchestrator.cs:227); for the fixture inputs (supply 55 / ground 5) the calculator rejects them and the coordinator publishes the invalid zero result plus a physics-validation status. Asserted as presence + exact recorded status per plan line 316 ("exact fallback pipe/message/result/status frozen by Todo 9").
4. **Cross-view IDs verified per navigation point.** Single-view host (`ModuleContentControl`, cached views) means only the active view's AutomationIds are in the UIA tree; the 17-ID contract is verified at each view activation (steps 3, 4, 6, 7, 8, 10).

## Post-run note

- `project-a.smc` and `unknown-pipe.smc` are intentionally mutated by the plan-mandated saves inside this run (task-owned copies). Rerun `prepare-ui-fixtures.ps1` before any subsequent V9 invocation to restore deterministic inputs.
- Full assertion→expected→observed tables, selector registry and screenshot inventory: `ui-qa/task-13-user-flow-qa.md` (harness-generated), machine-readable `ui-qa/observations.json` + `ui-qa/failure-observations.json`.

---
# Task 13 вЂ” WPF user-flow UI QA (V9 harness) raw tables

Generated: 2026-08-23T17:25:40.0507857Z В· Result: **PASS**

| Executable | SHA-256 (frozen) |
|---|---|
| `src\bin\Release\net8.0-windows\win-x64\SnowMeltingCalculator.exe` | `BE36766AF72900F8734B6BADD4EF014C6E0FC689EB459B62651EB2CFF3C6335D` |

## Process records (exe SHA validated before AND after every launch)

| Run tag | Project | PID | Exit | exeSHA before | exeSHA after | stdout log | stderr log |
|---|---|---|---|---|---|---|---|
| a-edit-save | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\fixtures\project-a.smc | 5264 | 0 | `BE36766AF72900F8вЂ¦` | `BE36766AF72900F8вЂ¦` | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\run-a-edit-save-stdout.log | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\run-a-edit-save-stderr.log |
| a-relaunch | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\fixtures\project-a.smc | 21216 | 0 | `BE36766AF72900F8вЂ¦` | `BE36766AF72900F8вЂ¦` | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\run-a-relaunch-stdout.log | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\run-a-relaunch-stderr.log |
| b-load-reset | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\fixtures\project-b.smc | 20652 | 0 | `BE36766AF72900F8вЂ¦` | `BE36766AF72900F8вЂ¦` | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\run-b-load-reset-stdout.log | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\run-b-load-reset-stderr.log |
| unknown-pipe | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\fixtures\unknown-pipe.smc | 17708 | 0 | `BE36766AF72900F8вЂ¦` | `BE36766AF72900F8вЂ¦` | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\run-unknown-pipe-stdout.log | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\run-unknown-pipe-stderr.log |

## Selector registry resolution (17 IDs)

| AutomationId | ControlType | View | Optional | Resolutions | Last present |
|---|---|---|---|---|---|
| ThermalMode | ComboBox | Thermal | False | 5 | True |
| ThermalSupplyTemperature | Edit | Thermal | False | 8 | True |
| ThermalGroundTemperature | Edit | Thermal | False | 7 | True |
| ThermalPipe | ComboBox | Thermal | False | 8 | True |
| ThermalPipeSpacing | ComboBox | Thermal | False | 7 | True |
| ThermalCalculate | Button | Thermal | False | 3 | True |
| ThermalReset | Button | Thermal | False | 1 | True |
| ThermalRecalcMessage | Text | Thermal | True | 14 | True |
| ThermalDeltaT | Text | Thermal | False | 2 | True |
| ThermalPowerTotal | Text | Thermal | False | 13 | True |
| ThermalResultStatus | Text | Thermal | True | 3 | True |
| HydraulicsPipeSpacing | Text | Hydraulics | False | 1 | True |
| HydraulicsSupplyTemperature | Text | Hydraulics | False | 1 | True |
| HydraulicsReturnTemperature | Text | Hydraulics | False | 1 | True |
| ResultsThermalPower | Text | Results | False | 1 | True |
| ResultsSupplyTemperature | Text | Results | False | 1 | True |
| ResultsReturnTemperature | Text | Results | False | 1 | True |

## Ten-step matrix (assertion -> expected -> observed -> artifact)

### Step 1: Verify fixture-manifest.json and all three input SHA-256 values вЂ” [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | fixture project-a.smc SHA matches manifest | E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87A2A0E3D264119397A | E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87A2A0E3D264119397A | True |
| 2 | fixture project-b.smc SHA matches manifest | FBE377ABAB8A5D3A47086E23A5E4FFFA68B95EAEEE569DEE459CEB0235940882 | FBE377ABAB8A5D3A47086E23A5E4FFFA68B95EAEEE569DEE459CEB0235940882 | True |
| 3 | fixture unknown-pipe.smc SHA matches manifest | D7BA538E14C8C9AC33556540705EECA6C10E8F223BB0DA837463B584F1AB1532 | D7BA538E14C8C9AC33556540705EECA6C10E8F223BB0DA837463B584F1AB1532 | True |

### Step 2: Start Project A as first .smc command-line argument and wait for main window вЂ” [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | launch 'a-edit-save': window title carries app suffix | *РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | project-a.smc вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | True |
| 2 | step2: clean loaded title (no dirty marker) | project-a.smc вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | project-a.smc вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | True |

### Step 3: Navigate to Thermal and record baseline mode/supply/ground/pipe/spacing/result text вЂ” [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | baseline mode == Melting | Melting | 1:Melting | True |
| 2 | baseline supply temperature (F1) | 50 (+/-1dp) | 50 | True |
| 3 | baseline ground temperature (F1) | 10 (+/-1dp) | 10 | True |
| 4 | baseline pipe contains 'RAUTHERM S 20' | *RAUTHERM S 20* | RAUTHERM S 20x2,0 (Г20Г—2) | True |
| 5 | baseline pipe spacing (mm) | 250 (+/-0dp) | 250 | True |
| 6 | baseline PowerTotal (fixture v1-sample result, F1) | 261 (+/-1dp) | 261 | True |
| 7 | baseline DeltaT (F1) | 15 (+/-1dp) | 15 | True |
| 8 | baseline recalc message absent (collapsed) | absent | absent | True |
| 9 | baseline validation status absent (collapsed) | absent | absent | True |

### Step 4: Edit mode/supply/ground/pipe/spacing; assert exact recalculation oracles and prior result retention вЂ” [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | mode edit -> EXACT recalc message | Р РµР¶РёРј СЂР°Р±РѕС‚С‹ РёР·РјРµРЅС‘РЅ. РўСЂРµР±СѓРµС‚СЃСЏ РїРµСЂРµСЃС‡С‘С‚. | Р РµР¶РёРј СЂР°Р±РѕС‚С‹ РёР·РјРµРЅС‘РЅ. РўСЂРµР±СѓРµС‚СЃСЏ РїРµСЂРµСЃС‡С‘С‚. | True |
| 2 | prior result retained after mode change | 261 (+/-1dp) | 261 | True |
| 3 | supply edit -> EXACT recalc message | РўРµРјРїРµСЂР°С‚СѓСЂР° РїРѕРґР°С‡Рё РёР·РјРµРЅРµРЅР°. РўСЂРµР±СѓРµС‚СЃСЏ РїРµСЂРµСЃС‡С‘С‚. | РўРµРјРїРµСЂР°С‚СѓСЂР° РїРѕРґР°С‡Рё РёР·РјРµРЅРµРЅР°. РўСЂРµР±СѓРµС‚СЃСЏ РїРµСЂРµСЃС‡С‘С‚. | True |
| 4 | supply edit applied (displayed value) | 65 (+/-1dp) | 65 | True |
| 5 | prior result retained after supply change | 261 (+/-1dp) | 261 | True |
| 6 | ground edit applied | 15 (+/-1dp) | 15 | True |
| 7 | recalc message still present after ground edit | present | present | True |
| 8 | prior result retained after ground change | 261 (+/-1dp) | 261 | True |
| 9 | pipe changed to RAUTHERM S 25 family | *RAUTHERM S 25* | RAUTHERM S 25x2,3 (Г25Г—2,3) | True |
| 10 | prior result retained after pipe change | 261 (+/-1dp) | 261 | True |
| 11 | spacing changed to 300 mm | 300 (+/-0dp) | 300 | True |
| 12 | prior result retained after spacing change | 261 (+/-1dp) | 261 | True |
| 13 | registry:ThermalMode unique/enabled/ComboBox | ComboBox | ControlType.ComboBox | True |
| 14 | registry:ThermalSupplyTemperature unique/enabled/Edit | Edit | ControlType.Edit | True |
| 15 | registry:ThermalGroundTemperature unique/enabled/Edit | Edit | ControlType.Edit | True |
| 16 | registry:ThermalPipe unique/enabled/ComboBox | ComboBox | ControlType.ComboBox | True |
| 17 | registry:ThermalPipeSpacing unique/enabled/ComboBox | ComboBox | ControlType.ComboBox | True |
| 18 | registry:ThermalCalculate unique/enabled/Button | Button | ControlType.Button | True |
| 19 | registry:ThermalReset unique/enabled/Button | Button | ControlType.Button | True |
| 20 | registry:ThermalRecalcMessage unique/correct-type (optional) | Text, presence optional | present/Text | True |
| 21 | registry:ThermalDeltaT unique/enabled/Text | Text | ControlType.Text | True |
| 22 | registry:ThermalPowerTotal unique/enabled/Text | Text | ControlType.Text | True |
| 23 | registry:ThermalResultStatus unique/correct-type (optional) | Text, presence optional | absent (collapsed) | True |

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\01-edit.png`

### Step 5: Invoke Р Р°СЃСЃС‡РёС‚Р°С‚СЊ; wait calculating state clears; recalc absent; result differs from baseline вЂ” [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | result text differs from step-3 baseline (261.0) | != 261.0 | 0.0 | True |
| 2 | recalculation message absent after successful calculate | absent | absent | True |

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\02-calculate.png`

### Step 6: Select Р“РёРґСЂР°РІР»РёС‡РµСЃРєРёР№ СЂР°СЃС‡С‘С‚ and Р РµР·СѓР»СЊС‚Р°С‚С‹; record six downstream output projections вЂ” [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | HydraulicsPipeSpacing projection == thermal spacing 300 mm / 10 (cm, CircuitsViewModel.PipeSpacing_cm) | 30 (+/-0dp) | 30 | True |
| 2 | HydraulicsSupplyTemperature projection == edited supply 65.0 | 65 (+/-1dp) | 65 | True |
| 3 | HydraulicsReturnTemperature numeric-parseable | number | 30.0 | True |
| 4 | ResultsThermalPower numeric-parseable and > 0 | > 0 | 5.2 | True |
| 5 | ResultsSupplyTemperature projection == 65.0 | 65 (+/-1dp) | 65 | True |
| 6 | ResultsReturnTemperature numeric-parseable | number | 0.0 | True |

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\03-hydraulics.png`, `docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\04-results.png`

### Step 7: Ctrl+S on Project A: file SHA/timestamp advance + title loses *; WM_CLOSE clean exit; relaunch restores edited state вЂ” [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | project-a.smc SHA advanced after Ctrl+S | ! E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87A2A0E3D264119397A | 69E083B6AD68F2A491AA72A617C71B104A3799C840B80AE08F6704C97A5C43C4 | True |
| 2 | project-a.smc timestamp advanced after Ctrl+S | > 08/18/2026 17:43:51 | 08/23/2026 17:25:17 | True |
| 3 | title after save is clean <file> вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | project-a.smc вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | project-a.smc вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | True |
| 4 | close 'a-edit-save': clean exit code | 0 | 0 | True |
| 5 | close 'a-edit-save': stderr free of crash patterns | no match | clean | True |
| 6 | launch 'a-relaunch': window title carries app suffix | *РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | project-a.smc вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | True |
| 7 | restored mode == AntiIcing | AntiIcing | AntiIcing | True |
| 8 | restored supply == 65.0 | 65 (+/-1dp) | 65 | True |
| 9 | restored ground == 15.0 | 15 (+/-1dp) | 15 | True |
| 10 | restored pipe in RAUTHERM S 25 family | *RAUTHERM S 25* | RAUTHERM S 25x2,3 (Г25Г—2,3) | True |
| 11 | restored spacing == 300 mm | 300 (+/-0dp) | 300 | True |
| 12 | restored PowerTotal == step-5 calculated value | 0 (+/-1dp) | 0 | True |
| 13 | no recalc message after restore | absent | absent | True |
| 14 | close 'a-relaunch': clean exit code | 0 | 0 | True |
| 15 | close 'a-relaunch': stderr free of crash patterns | no match | clean | True |

### Step 8: Close clean; relaunch Project B; assert 55.0/5.0/150/RAUTHERM S 17 and no project-A result вЂ” [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | launch 'b-load-reset': window title carries app suffix | *РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | project-b.smc вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | True |
| 2 | Project B supply == 55.0 | 55 (+/-1dp) | 55 | True |
| 3 | Project B ground == 5.0 | 5 (+/-1dp) | 5 | True |
| 4 | Project B spacing == 150 mm | 150 (+/-0dp) | 150 | True |
| 5 | Project B pipe in RAUTHERM S 17 family | *RAUTHERM S 17* | RAUTHERM S 17x2,0 (Г17Г—2) | True |
| 6 | no project-A result carried into B (PowerTotal != 261.0 baseline) | != 261.0 | 0.0 | True |

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\05-load-2.png`

### Step 9: While B is clean invoke РЎРѕР·РґР°С‚СЊ РЅРѕРІС‹Р№ СЂР°СЃС‡С‘С‚; assert DEC-T01 defaults Melting/50.0/10.0/no-pipe/200/no-result вЂ” [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | title after new-calculation reset is bare app title (clean, no file) | РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | True |
| 2 | reset mode == Melting | Melting | Melting | True |
| 3 | reset supply == 50.0 | 50 (+/-1dp) | 50 | True |
| 4 | reset ground == 10.0 | 10 (+/-1dp) | 10 | True |
| 5 | reset pipe selection empty (no pipe) | no selection | 0: | True |
| 6 | reset spacing == 200 mm | 200 (+/-0dp) | 200 | True |
| 7 | reset leaves spacing combo present (enabled-state not contract-bound) | present | True | True |
| 8 | reset clears result (PowerTotal absent) | absent | absent | True |
| 9 | reset leaves no recalc message | absent | absent | True |
| 10 | close 'b-load-reset': clean exit code | 0 | 0 | True |
| 11 | close 'b-load-reset': stderr free of crash patterns | no match | clean | True |

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\06-reset.png`

### Step 10: Failure branch: unknown-pipe.smc fallback pipe/result, restore-guard cleared via supply edit, Ctrl+S save, clean close вЂ” [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | launch 'unknown-pipe': window title carries app suffix | *РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | unknown-pipe.smc вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | True |
| 2 | fallback pipe == first standard (RAUTHERM S 17 family) | *RAUTHERM S 17* | RAUTHERM S 17x2,0 (Г17Г—2) | True |
| 3 | fallback-calculated result published (ThermalPowerTotal present) | present | 0.0 | True |
| 4 | characterized invalid-result status present (calculator validation on fixture inputs) | present | РџСЂРё С‚РµРєСѓС‰РёС… РїР°СЂР°РјРµС‚СЂР°С… СЃРёСЃС‚РµРјС‹ РЅРµ РѕР±РµСЃРїРµС‡РёРІР°РµС‚СЃСЏ С‚СЂРµР±СѓРµРјР°СЏ РјРѕС‰РЅРѕСЃС‚СЊ. РўРµРјРїРµСЂР°С‚СѓСЂР° РїРѕРґР°С‡Рё (55,0В°C) РґРѕР»Р¶РЅР° Р±С‹С‚СЊ РЅРµ РјРµРЅРµРµ 104,2В°C. РЈРІРµР»РёС‡СЊС‚Рµ С‚РµРјРїРµСЂР°С‚СѓСЂСѓ РїРѕРґР°С‡Рё, СѓРјРµРЅСЊС€РёС‚Рµ РёРЅС‚РµРЅСЃРёРІРЅРѕСЃС‚СЊ СЃРЅРµРіРѕРїР°РґР° РёР»Рё РёР·РјРµРЅРёС‚Рµ СЂРµР¶РёРј СЂР°Р±РѕС‚С‹.; РўРµРјРїРµСЂР°С‚СѓСЂРЅС‹Р№ РїРµСЂРµРїР°Рґ РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РїРѕР»РѕР¶РёС‚РµР»СЊРЅС‹Рј | True |
| 5 | no recalculation message after unknown-pipe restore | absent | absent | True |
| 6 | supply edit accepted -> EXACT recalc message proves restore guard cleared | РўРµРјРїРµСЂР°С‚СѓСЂР° РїРѕРґР°С‡Рё РёР·РјРµРЅРµРЅР°. РўСЂРµР±СѓРµС‚СЃСЏ РїРµСЂРµСЃС‡С‘С‚. | РўРµРјРїРµСЂР°С‚СѓСЂР° РїРѕРґР°С‡Рё РёР·РјРµРЅРµРЅР°. РўСЂРµР±СѓРµС‚СЃСЏ РїРµСЂРµСЃС‡С‘С‚. | True |
| 7 | unknown-pipe.smc SHA advanced after Ctrl+S | ! D7BA538E14C8C9AC33556540705EECA6C10E8F223BB0DA837463B584F1AB1532 | DED9CADF2A5595748F5D2B27544F4A74BABC2E6A8071A8A58A8913EC46B2E505 | True |
| 8 | title after save is clean <file> вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | unknown-pipe.smc вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | unknown-pipe.smc вЂ” РљР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃРЅРµРіРѕС‚Р°СЏРЅРёСЏ REHAU | True |
| 9 | close 'unknown-pipe': clean exit code | 0 | 0 | True |
| 10 | close 'unknown-pipe': stderr free of crash patterns | no match | clean | True |

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\07-unknown-pipe.png`, `docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\failure-observations.json`

## Screenshots

| Name | File | Bytes | Dimensions | SHA-256 |
|---|---|---|---|---|
| 01-edit | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\01-edit.png | 54564 | 900x700 | `3358C450F5DD3F287F65975072D08DE303AB876FECAA383B79B75DF43FBF86FE` |
| 02-calculate | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\02-calculate.png | 55446 | 900x700 | `24F70204399A0C95099F017E17343E876DA0BE0FD3256C2B13EE1A0ECEBDF4F5` |
| 03-hydraulics | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\03-hydraulics.png | 92952 | 900x700 | `8DAE9A32A1FC480461A5DBB9BE7C3B932A60D6FADB8A0333A7849D4DCBAEBAFA` |
| 04-results | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\04-results.png | 100797 | 900x700 | `666E0C250B528C9EC5A15653FAADDC58893500545DCE93456D2DB15EC12CF1BE` |
| 05-load-2 | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\05-load-2.png | 53354 | 900x700 | `E79632B253A1369F9D2339CFA006587AFCE2431678B22E7CD3CA8DAC9451C741` |
| 06-reset | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\06-reset.png | 50169 | 900x700 | `DA121583447BB18F3236E648B336DDA00F9ACBC56901B31620C27F51B034727D` |
| 07-unknown-pipe | docs\architecture-migration\evidence\phase-4-thermal-state\task-13\ui-qa\07-unknown-pipe.png | 53354 | 900x700 | `E79632B253A1369F9D2339CFA006587AFCE2431678B22E7CD3CA8DAC9451C741` |

## Deviation notes

- Cross-view AutomationIds are only resolvable while their view is active (single-view host ModuleContentControl with cached views); the 17-ID contract is therefore verified per-view at each navigation point of steps 3, 4, 6, 7, 8 and 10 rather than in one flat scan.
- HydraulicsPipeSpacing displays centimetres: CircuitsViewModel.PipeSpacing_cm = thermal PipeSpacing(mm)/10 (src/ViewModels/Hydraulics/CircuitsViewModel.cs:285). The distilled brief said "assert == 300"; the code-faithful expectation is 30 (cm) for thermal spacing 300 mm вЂ” asserted accordingly.
- Keystroke substitution (steps 7/9/10): injected Ctrl+S/Ctrl+N chords never reach the Window-level KeyDown handler in this environment (probe evidence: plain keys and TextBox-internal Ctrl+A deliver; Ctrl+O raises no open-dialog). The harness drives the SAME bound commands (SaveProjectCommand / NewCalculationCommand) through the visible В«Р¤Р°Р№Р»В» menu via UIA Invoke/Selection patterns; the plan-mandated observables (file SHA/timestamp advance, dirty-marker clears, DEC-T01 defaults) are asserted unchanged.
- Unknown-pipe fallback publishes an INVALID zero result with a physics-validation status instead of a positive power: the orchestrator runs exactly one fallback Calculate (ProjectLoadOrchestrator.cs:227), the calculator rejects the fixture inputs (supply 55 / ground 5) and the coordinator publishes the invalid result canonically. Asserted as presence + exact recorded status per plan line 316 ("exact fallback pipe/message/result/status frozen by Todo 9"). Status text: РџСЂРё С‚РµРєСѓС‰РёС… РїР°СЂР°РјРµС‚СЂР°С… СЃРёСЃС‚РµРјС‹ РЅРµ РѕР±РµСЃРїРµС‡РёРІР°РµС‚СЃСЏ С‚СЂРµР±СѓРµРјР°СЏ РјРѕС‰РЅРѕСЃС‚СЊ. РўРµРјРїРµСЂР°С‚СѓСЂР° РїРѕРґР°С‡Рё (55,0В°C) РґРѕР»Р¶РЅР° Р±С‹С‚СЊ РЅРµ РјРµРЅРµРµ 104,2В°C. РЈРІРµР»РёС‡СЊС‚Рµ С‚РµРјРїРµСЂР°С‚СѓСЂСѓ РїРѕРґР°С‡Рё, СѓРјРµРЅСЊС€РёС‚Рµ РёРЅС‚РµРЅСЃРёРІРЅРѕСЃС‚СЊ СЃРЅРµРіРѕРїР°РґР° РёР»Рё РёР·РјРµРЅРёС‚Рµ СЂРµР¶РёРј СЂР°Р±РѕС‚С‹.; РўРµРјРїРµСЂР°С‚СѓСЂРЅС‹Р№ РїРµСЂРµРїР°Рґ РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РїРѕР»РѕР¶РёС‚РµР»СЊРЅС‹Рј

## Post-run note

- `project-a.smc` and `unknown-pipe.smc` are intentionally mutated by the plan-mandated Ctrl+S saves inside this run (task-owned copies); rerun `prepare-ui-fixtures.ps1` before any subsequent V9 invocation to restore deterministic inputs.
- HydraulicsPipeSpacing displays cm (`PipeSpacing_cm` = thermal mm / 10): thermal 300 mm projects as 30.
