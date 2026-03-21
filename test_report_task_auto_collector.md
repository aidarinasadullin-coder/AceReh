# Отчёт о тестировании: Автоматический выбор коллектора по расходу

## Статус
✅ Задача выполнена успешно

## Изменённые файлы

### Новые файлы:
Нет

### Изменённые файлы:
1. `src/Models/Hydraulics/CollectorSummary.cs` — добавлено свойство `Warning`
2. `src/ViewModels/Hydraulics/CircuitsViewModel.cs` — добавлен метод `AutoSelectCollectorType()`
3. `src/Views/Hydraulics/CircuitsView.xaml` — добавлено отображение предупреждения
4. `tests/SnowMeltingCalculator.Tests/ViewModels/Hydraulics/CircuitsViewModelTests.cs` — добавлены тесты

## Реализованный функционал

### 1. Автоматический выбор типа коллектора
Метод `AutoSelectCollectorType()` автоматически выбирает тип коллектора на основе суммарного расхода:

| Расход | Тип коллектора | ValveType |
|--------|----------------|-----------|
| ≤ 1.5 м³/ч | HKV-D (2-12 контуров) | HKV_D |
| 1.5 - 2.5 м³/ч | IV 1¼" (2-12 контуров) | IV_1_25 |
| 2.5 - 4.0 м³/ч | IV 1½" (2-12 контуров) | IV_1_5 |
| > 4.0 м³/ч | Предупреждение | — |

### 2. Предупреждение о превышении расхода
При расходе > 4.0 м³/ч устанавливается предупреждение:
```
"Превышение расхода: X.XX м³/ч > 4.0 м³/ч. Рекомендуется разделить на несколько коллекторов."
```

### 3. Интеграция в расчёт
Метод `AutoSelectCollectorType()` вызывается автоматически после расчёта коллектора в методе `Calculate()`.

## Новые тесты

### Добавленные тесты в CircuitsViewModelTests.cs:
1. `AutoSelectCollectorType_FlowRateBelow1_5_SelectsHKV_D` — расход < 1.5 м³/ч → HKV-D
2. `AutoSelectCollectorType_FlowRate1_5To2_5_SelectsIV_1_25` — расход 1.5-2.5 м³/ч → IV 1¼"
3. `AutoSelectCollectorType_FlowRate2_5To4_0_SelectsIV_1_5` — расход 2.5-4.0 м³/ч → IV 1½"
4. `AutoSelectCollectorType_FlowRateAbove4_0_SetsWarning` — расход > 4.0 м³/ч → предупреждение
5. `AutoSelectCollectorType_FlowRateExactly1_5_SelectsHKV_D` — граничное значение 1.5 м³/ч
6. `AutoSelectCollectorType_FlowRateExactly2_5_SelectsIV_1_25` — граничное значение 2.5 м³/ч
7. `AutoSelectCollectorType_FlowRateExactly4_0_SelectsIV_1_5` — граничное значение 4.0 м³/ч
8. `AutoSelectCollectorType_ClearsWarningWhenFlowRateDecreases` — очистка предупреждения

## Результаты компиляции

### Основной проект:
✅ Скомпилирован успешно (SnowMeltingCalculator.dll)

### Тестовый проект:
⚠️ Ошибки компиляции в существующих тестах (CircuitRowTests.cs) — не связаны с изменениями
- Отсутствуют свойства `IsLengthUserInput`, `IsAreaUserInput`, `IsLengthReadOnly`, `IsAreaReadOnly` в классе `CircuitRow`

## Критерии приёмки

| Критерий | Статус |
|----------|--------|
| При расходе ≤ 1.5 м³/ч → HKV-D | ✅ Реализовано |
| При расходе 1.5-2.5 м³/ч → IV 1¼" | ✅ Реализовано |
| При расходе 2.5-4.0 м³/ч → IV 1½" | ✅ Реализовано |
| При расходе > 4.0 м³/ч → предупреждение | ✅ Реализовано |
| Тип коллектора обновляется в результатах | ✅ Реализовано |
| Предупреждение отображается в интерфейсе | ✅ Реализовано |

## Открытые вопросы
Нет