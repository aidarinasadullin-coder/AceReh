# Draft: fix-design-temperature-source

## Intent & review
- intent: clear
- review_required: false
- Пользователь указал конкретный баг и источник спецификации (README v.2.1.md, таблица 1.6 СП 131.13330.2025).

## Problem
После рефакторинга `CircuitsViewModel` берёт «расчётную температуру» холодного пуска из
`IClimateData.ColdFiveDayTemperature` (температура холодной пятидневки, для Москвы −23 °C),
а должен из `IClimateData.AirTemperature` (расчётная T_воздуха по таблице 1.6, для Москвы −10 °C).
Бегает только при выбранном городе; в существующих тестах город не выбран, поэтому fallback
маскирует баг.

## Root cause (verified)
- `src/ViewModels/Hydraulics/CircuitsViewModel.cs:176` — `DesignTemperature => Climate?.ColdFiveDayTemperature ?? 0.0`
- `src/ViewModels/Hydraulics/CircuitsViewModel.cs:385,391,401` — local `coldFiveDayTemperature`/`designTemp` из `ColdFiveDayTemperature`
- `src/ViewModels/Climate/ClimateViewModel.cs:722` — `SyncToClimateData` корректно хранит `AirTemperature` и `ColdFiveDayTemperature` раздельно
- `src/ViewModels/Results/ResultsViewModel.cs:1015` — уже корректно: `DesignTemperature = _climateViewModel.AirTemperature`
- `src/Core/CalculationContext.cs:114` — уже есть `AirTemperature` свойство

## Decision (adopted defaults)
- Источник `DesignTemperature` в гидравлике — `CalculationContext.AirTemperature` (не `ColdFiveDayTemperature`).
- `ColdFiveDayTemperature` остаётся информационным полем для отображения, НЕ используется для расчёта.
- Базовый JSON `baseline_refactor_dedupe.json` НЕ трогается (баг в VM, не в калькуляторе; baseline-тесты вызывают калькулятор напрямую с фикстурой −20).
- Тесты с выбранным городом добавляются (все 4 зоны + повышенные требования).

## Gate
- status: awaiting-approval
- pending action: write `.omo/plans/fix-design-temperature-source.md`
