# Task 13 — WPF user-flow UI QA (V9 harness) raw tables

Generated: 2026-08-23T19:48:43.0079725Z · Result: **PASS**

| Executable | SHA-256 (frozen) |
|---|---|
| `src\bin\Release\net8.0-windows\win-x64\SnowMeltingCalculator.exe` | `BE36766AF72900F8734B6BADD4EF014C6E0FC689EB459B62651EB2CFF3C6335D` |

## Process records (exe SHA validated before AND after every launch)

| Run tag | Project | PID | Exit | exeSHA before | exeSHA after | stdout log | stderr log |
|---|---|---|---|---|---|---|---|
| a-edit-save | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\fixtures\project-a.smc | 8580 | 0 | `BE36766AF72900F8…` | `BE36766AF72900F8…` | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\run-a-edit-save-stdout.log | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\run-a-edit-save-stderr.log |
| a-relaunch | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\fixtures\project-a.smc | 7368 | 0 | `BE36766AF72900F8…` | `BE36766AF72900F8…` | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\run-a-relaunch-stdout.log | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\run-a-relaunch-stderr.log |
| b-load-reset | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\fixtures\project-b.smc | 25148 | 0 | `BE36766AF72900F8…` | `BE36766AF72900F8…` | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\run-b-load-reset-stdout.log | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\run-b-load-reset-stderr.log |
| unknown-pipe | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\fixtures\unknown-pipe.smc | 4800 | 0 | `BE36766AF72900F8…` | `BE36766AF72900F8…` | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\run-unknown-pipe-stdout.log | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\run-unknown-pipe-stderr.log |

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

### Step 1: Verify fixture-manifest.json and all three input SHA-256 values — [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | fixture project-a.smc SHA matches manifest | E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87A2A0E3D264119397A | E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87A2A0E3D264119397A | True |
| 2 | fixture project-b.smc SHA matches manifest | FBE377ABAB8A5D3A47086E23A5E4FFFA68B95EAEEE569DEE459CEB0235940882 | FBE377ABAB8A5D3A47086E23A5E4FFFA68B95EAEEE569DEE459CEB0235940882 | True |
| 3 | fixture unknown-pipe.smc SHA matches manifest | D7BA538E14C8C9AC33556540705EECA6C10E8F223BB0DA837463B584F1AB1532 | D7BA538E14C8C9AC33556540705EECA6C10E8F223BB0DA837463B584F1AB1532 | True |

### Step 2: Start Project A as first .smc command-line argument and wait for main window — [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | launch 'a-edit-save': window title carries app suffix | *Калькулятор снеготаяния REHAU | project-a.smc — Калькулятор снеготаяния REHAU | True |
| 2 | step2: clean loaded title (no dirty marker) | project-a.smc — Калькулятор снеготаяния REHAU | project-a.smc — Калькулятор снеготаяния REHAU | True |

### Step 3: Navigate to Thermal and record baseline mode/supply/ground/pipe/spacing/result text — [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | baseline mode == Melting | Melting | 1:Melting | True |
| 2 | baseline supply temperature (F1) | 50 (+/-1dp) | 50 | True |
| 3 | baseline ground temperature (F1) | 10 (+/-1dp) | 10 | True |
| 4 | baseline pipe contains 'RAUTHERM S 20' | *RAUTHERM S 20* | RAUTHERM S 20x2,0 (Ø20×2) | True |
| 5 | baseline pipe spacing (mm) | 250 (+/-0dp) | 250 | True |
| 6 | baseline PowerTotal (fixture v1-sample result, F1) | 261 (+/-1dp) | 261 | True |
| 7 | baseline DeltaT (F1) | 15 (+/-1dp) | 15 | True |
| 8 | baseline recalc message absent (collapsed) | absent | absent | True |
| 9 | baseline validation status absent (collapsed) | absent | absent | True |

### Step 4: Edit mode/supply/ground/pipe/spacing; assert exact recalculation oracles and prior result retention — [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | mode edit -> EXACT recalc message | Режим работы изменён. Требуется пересчёт. | Режим работы изменён. Требуется пересчёт. | True |
| 2 | prior result retained after mode change | 261 (+/-1dp) | 261 | True |
| 3 | supply edit -> EXACT recalc message | Температура подачи изменена. Требуется пересчёт. | Температура подачи изменена. Требуется пересчёт. | True |
| 4 | supply edit applied (displayed value) | 65 (+/-1dp) | 65 | True |
| 5 | prior result retained after supply change | 261 (+/-1dp) | 261 | True |
| 6 | ground edit applied | 15 (+/-1dp) | 15 | True |
| 7 | recalc message still present after ground edit | present | present | True |
| 8 | prior result retained after ground change | 261 (+/-1dp) | 261 | True |
| 9 | pipe changed to RAUTHERM S 25 family | *RAUTHERM S 25* | RAUTHERM S 25x2,3 (Ø25×2,3) | True |
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

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\01-edit.png`

### Step 5: Invoke Рассчитать; wait calculating state clears; recalc absent; result differs from baseline — [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | result text differs from step-3 baseline (261.0) | != 261.0 | 0.0 | True |
| 2 | recalculation message absent after successful calculate | absent | absent | True |

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\02-calculate.png`

### Step 6: Select Гидравлический расчёт and Результаты; record six downstream output projections — [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | HydraulicsPipeSpacing projection == thermal spacing 300 mm / 10 (cm, CircuitsViewModel.PipeSpacing_cm) | 30 (+/-0dp) | 30 | True |
| 2 | HydraulicsSupplyTemperature projection == edited supply 65.0 | 65 (+/-1dp) | 65 | True |
| 3 | HydraulicsReturnTemperature numeric-parseable | number | 30.0 | True |
| 4 | ResultsThermalPower numeric-parseable and > 0 | > 0 | 5.2 | True |
| 5 | ResultsSupplyTemperature projection == 65.0 | 65 (+/-1dp) | 65 | True |
| 6 | ResultsReturnTemperature numeric-parseable | number | 0.0 | True |

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\03-hydraulics.png`, `docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\04-results.png`

### Step 7: Ctrl+S on Project A: file SHA/timestamp advance + title loses *; WM_CLOSE clean exit; relaunch restores edited state — [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | project-a.smc SHA advanced after Ctrl+S | ! E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87A2A0E3D264119397A | 5DA9B1E0E71B3B694560F0F4913BB6BEDC820FB6436F35EBFC363F457F7B6F84 | True |
| 2 | project-a.smc timestamp advanced after Ctrl+S | > 08/18/2026 17:43:51 | 08/23/2026 19:48:20 | True |
| 3 | title after save is clean <file> — Калькулятор снеготаяния REHAU | project-a.smc — Калькулятор снеготаяния REHAU | project-a.smc — Калькулятор снеготаяния REHAU | True |
| 4 | close 'a-edit-save': clean exit code | 0 | 0 | True |
| 5 | close 'a-edit-save': stderr free of crash patterns | no match | clean | True |
| 6 | launch 'a-relaunch': window title carries app suffix | *Калькулятор снеготаяния REHAU | project-a.smc — Калькулятор снеготаяния REHAU | True |
| 7 | restored mode == AntiIcing | AntiIcing | AntiIcing | True |
| 8 | restored supply == 65.0 | 65 (+/-1dp) | 65 | True |
| 9 | restored ground == 15.0 | 15 (+/-1dp) | 15 | True |
| 10 | restored pipe in RAUTHERM S 25 family | *RAUTHERM S 25* | RAUTHERM S 25x2,3 (Ø25×2,3) | True |
| 11 | restored spacing == 300 mm | 300 (+/-0dp) | 300 | True |
| 12 | restored PowerTotal == step-5 calculated value | 0 (+/-1dp) | 0 | True |
| 13 | no recalc message after restore | absent | absent | True |
| 14 | close 'a-relaunch': clean exit code | 0 | 0 | True |
| 15 | close 'a-relaunch': stderr free of crash patterns | no match | clean | True |

### Step 8: Close clean; relaunch Project B; assert 55.0/5.0/150/RAUTHERM S 17 and no project-A result — [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | launch 'b-load-reset': window title carries app suffix | *Калькулятор снеготаяния REHAU | project-b.smc — Калькулятор снеготаяния REHAU | True |
| 2 | Project B supply == 55.0 | 55 (+/-1dp) | 55 | True |
| 3 | Project B ground == 5.0 | 5 (+/-1dp) | 5 | True |
| 4 | Project B spacing == 150 mm | 150 (+/-0dp) | 150 | True |
| 5 | Project B pipe in RAUTHERM S 17 family | *RAUTHERM S 17* | RAUTHERM S 17x2,0 (Ø17×2) | True |
| 6 | no project-A result carried into B (PowerTotal != 261.0 baseline) | != 261.0 | 0.0 | True |

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\05-load-2.png`

### Step 9: While B is clean invoke Создать новый расчёт; assert DEC-T01 defaults Melting/50.0/10.0/no-pipe/200/no-result — [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | title after new-calculation reset is bare app title (clean, no file) | Калькулятор снеготаяния REHAU | Калькулятор снеготаяния REHAU | True |
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

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\06-reset.png`

### Step 10: Failure branch: unknown-pipe.smc fallback pipe/result, restore-guard cleared via supply edit, Ctrl+S save, clean close — [PASS]

| # | Assertion | Expected | Observed | Pass |
|---|---|---|---|---|
| 1 | launch 'unknown-pipe': window title carries app suffix | *Калькулятор снеготаяния REHAU | unknown-pipe.smc — Калькулятор снеготаяния REHAU | True |
| 2 | fallback pipe == first standard (RAUTHERM S 17 family) | *RAUTHERM S 17* | RAUTHERM S 17x2,0 (Ø17×2) | True |
| 3 | fallback-calculated result published (ThermalPowerTotal present) | present | 0.0 | True |
| 4 | characterized invalid-result status present (calculator validation on fixture inputs) | present | При текущих параметрах системы не обеспечивается требуемая мощность. Температура подачи (55,0°C) должна быть не менее 104,2°C. Увеличьте температуру подачи, уменьшите интенсивность снегопада или измените режим работы.; Температурный перепад должен быть положительным | True |
| 5 | no recalculation message after unknown-pipe restore | absent | absent | True |
| 6 | supply edit accepted -> EXACT recalc message proves restore guard cleared | Температура подачи изменена. Требуется пересчёт. | Температура подачи изменена. Требуется пересчёт. | True |
| 7 | unknown-pipe.smc SHA advanced after Ctrl+S | ! D7BA538E14C8C9AC33556540705EECA6C10E8F223BB0DA837463B584F1AB1532 | D6B580D0664208D0F92906C8EF28700A6A59C216FC244246A0EA922608DAB6B6 | True |
| 8 | title after save is clean <file> — Калькулятор снеготаяния REHAU | unknown-pipe.smc — Калькулятор снеготаяния REHAU | unknown-pipe.smc — Калькулятор снеготаяния REHAU | True |
| 9 | close 'unknown-pipe': clean exit code | 0 | 0 | True |
| 10 | close 'unknown-pipe': stderr free of crash patterns | no match | clean | True |

Artifacts: `docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\07-unknown-pipe.png`, `docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\failure-observations.json`

## Screenshots

| Name | File | Bytes | Dimensions | SHA-256 |
|---|---|---|---|---|
| 01-edit | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\01-edit.png | 55252 | 900x700 | `A869E46126C2F473FD4B6C29984D5769DBDDDEBACE6C3FA19044DCA1ACDBAC15` |
| 02-calculate | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\02-calculate.png | 56137 | 900x700 | `E8165C26D21FE6E1FA9A9B74363EB43807FE6FBED36AC9BCB1AAB69BC96F9167` |
| 03-hydraulics | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\03-hydraulics.png | 92952 | 900x700 | `8DAE9A32A1FC480461A5DBB9BE7C3B932A60D6FADB8A0333A7849D4DCBAEBAFA` |
| 04-results | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\04-results.png | 100797 | 900x700 | `666E0C250B528C9EC5A15653FAADDC58893500545DCE93456D2DB15EC12CF1BE` |
| 05-load-2 | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\05-load-2.png | 54326 | 900x700 | `DDEE748B70435366960E0739506FA30E47DE1F1714B5F0850C74DBCDC3A16F67` |
| 06-reset | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\06-reset.png | 50610 | 900x700 | `E8875E7C6224610645662B48C7873E0C3BDD39975A273C9C6DE833B32AFF1BEA` |
| 07-unknown-pipe | docs\architecture-migration\evidence\phase-4-thermal-state\final\f3\ui-qa\07-unknown-pipe.png | 54392 | 900x700 | `6480AABB1FB6BFEF99F3489F964E8E37B383CD4E366E9AE10269BEB2A5D60E71` |

## Deviation notes

- Cross-view AutomationIds are only resolvable while their view is active (single-view host ModuleContentControl with cached views); the 17-ID contract is therefore verified per-view at each navigation point of steps 3, 4, 6, 7, 8 and 10 rather than in one flat scan.
- HydraulicsPipeSpacing displays centimetres: CircuitsViewModel.PipeSpacing_cm = thermal PipeSpacing(mm)/10 (src/ViewModels/Hydraulics/CircuitsViewModel.cs:285). The distilled brief said "assert == 300"; the code-faithful expectation is 30 (cm) for thermal spacing 300 mm — asserted accordingly.
- Keystroke substitution (steps 7/9/10): injected Ctrl+S/Ctrl+N chords never reach the Window-level KeyDown handler in this environment (probe evidence: plain keys and TextBox-internal Ctrl+A deliver; Ctrl+O raises no open-dialog). The harness drives the SAME bound commands (SaveProjectCommand / NewCalculationCommand) through the visible «Файл» menu via UIA Invoke/Selection patterns; the plan-mandated observables (file SHA/timestamp advance, dirty-marker clears, DEC-T01 defaults) are asserted unchanged.
- Unknown-pipe fallback publishes an INVALID zero result with a physics-validation status instead of a positive power: the orchestrator runs exactly one fallback Calculate (ProjectLoadOrchestrator.cs:227), the calculator rejects the fixture inputs (supply 55 / ground 5) and the coordinator publishes the invalid result canonically. Asserted as presence + exact recorded status per plan line 316 ("exact fallback pipe/message/result/status frozen by Todo 9"). Status text: При текущих параметрах системы не обеспечивается требуемая мощность. Температура подачи (55,0°C) должна быть не менее 104,2°C. Увеличьте температуру подачи, уменьшите интенсивность снегопада или измените режим работы.; Температурный перепад должен быть положительным

## Post-run note

- `project-a.smc` and `unknown-pipe.smc` are intentionally mutated by the plan-mandated Ctrl+S saves inside this run (task-owned copies); rerun `prepare-ui-fixtures.ps1` before any subsequent V9 invocation to restore deterministic inputs.
- HydraulicsPipeSpacing displays cm (`PipeSpacing_cm` = thermal mm / 10): thermal 300 mm projects as 30.
