# fix-thermal-to-hydraulics-sync — draft (durable resume point)

- intent: clear
- review_required: false
- classify: Standard (1-3 files, clear refactor)
- status: plan-written (approved by user with "да" to "оформлю это как план")

## Problem
После изменения параметров теплового расчёта и пересчёта часть данных вкладки
«Гидравлический расчёт» не обновляется. Root cause: `CircuitsViewModel.OnCalculationContextChanged`
реагирует на `ThermalResult` только `Calculate()` без `NotifyThermalPropertiesChanged()`,
а на `ThermalInputs` не реагирует вовсе. `UpdateFromThermalModule` (правильный путь) вызывается
только при загрузке проекта.

## Approach (agreed)
- CircuitsViewModel = потребитель `CalculationContext`, не пишет обратно тепловые данные в runtime-потоке.
- `OnCalculationContextChanged`:
  - `ThermalInputs` -> `NotifyThermalPropertiesChanged()` (без Calculate, результат ещё не готов)
  - `ThermalResult` -> `NotifyThermalPropertiesChanged()` + `Calculate()`
  - сохранить игнор source == "CircuitsViewModel" (защита от feedback при проектной загрузке)
- `UpdateFromThermalModule` оставить для явного пуша (загрузка проекта/тесты), не трогать его запись в контекст.
- Добавить интеграционный тест реального пути: context event (source "Thermal") -> PropertyChanged thermal props + Calculate.
- Не чинить мёртвое поле `HydraulicsResults`, не обрабатывать Reset/invalid thermal (out of scope).

## Components
- C1 CircuitsViewModel context handler — outcome: UI thermal props обновляются после тепло→контекст. evidence: tests + manual QA.
- C2 Regression test — outcome: реальный путь покрыт, регрессия ловится. evidence: new test.
- C3 Build/test gate + notepad.

## Decisions
- D1: `ThermalInputs` event => notify only, no Calculate (result may be stale).
- D2: `ThermalResult` event => notify + Calculate.
- D3: не переписывать `UpdateFromThermalModule`, не трогать проектную загрузку.
- D4: не подключать `HydraulicsResults`/`UpdateHydraulics` (отдельный долг).
- D5: invalid/Reset thermal не в скоупе.
- D6: тест — TDD (сначала failing test, потом фикс).

## Forks asked
- none blocking (approach agreed). Optional: invalid thermal handling — defaulted OUT.

## Approval gate
- approved: user said "да" to writing the plan.

## Review receipts
- Metis gap analysis: ses_09de7dc61ffefDjCrjHZeJinXw — completed.
- MUST-FIX folded: M1 test isolation (seed ThermalInputs before subscribe, assert only UpdateThermal), M2 regression scope (DoubleCalculationPreventionTests + ClimateToHydraulicsIntegrationTests), M3 manual QA Playwright->WPF/human.
- SHOULD-FIX folded: S3 update stale comment, S4 F1 git command, S6 failing evidence acceptance, S1 double Notify documented as accepted.
- high-accuracy review: COMPLETE.
  - Momus (ses_09dd9ec64ffeI8pHRsk0MI714S / bg_1c49bb6b): OKAY, no blocking issues. Minor: second test also fails pre-fix (non-blocking).
  - Oracle (ses_09dd9c3cfffeV0glRNJsLfZDRw / bg_2f3d0b80): OKAY, verified TDD isolation, regression safety, no recursion, guardrails.
  - Both verdicts unconditional approval. No retries needed.
