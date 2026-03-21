# Отчёт о тестировании: Исправление диапазонов расхода для автоматического выбора коллектора

## Дата: 20.03.2026

## Изменённые файлы

### 1. CircuitsViewModel.cs
**Путь:** `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

**Изменения в методе `AutoSelectCollectorType()`:**
- Изменён порог предупреждения с `> 4.0` на `>= 7.0` м³/ч
- Изменено условие для IV 1½" с `> 2.5` на `>= 2.5` м³/ч
- Обновлён текст предупреждения: `≥ 7.0 м³/ч` вместо `> 4.0 м³/ч`

### 2. CircuitsViewModelTests.cs
**Путь:** `tests/SnowMeltingCalculator.Tests/ViewModels/Hydraulics/CircuitsViewModelTests.cs`

**Изменённые тесты:**
- `AutoSelectCollectorType_FlowRate2_5To4_0_SelectsIV_1_5` → `AutoSelectCollectorType_FlowRate2_5To7_0_SelectsIV_1_5`
- `AutoSelectCollectorType_FlowRateAbove4_0_SetsWarning` → `AutoSelectCollectorType_FlowRateAbove7_0_SetsWarning`
- `AutoSelectCollectorType_FlowRateExactly2_5_SelectsIV_1_25` → `AutoSelectCollectorType_FlowRateExactly2_5_SelectsIV_1_5`
- `AutoSelectCollectorType_FlowRateExactly4_0_SelectsIV_1_5` → `AutoSelectCollectorType_FlowRateExactly7_0_SetsWarning`
- `AutoSelectCollectorType_ClearsWarningWhenFlowRateDecreases` — обновлён расход с 5000 на 8000 л/ч

**Добавленные тесты:**
- `AutoSelectCollectorType_FlowRate5_0_SelectsIV_1_5` — тест для расхода 5.0 м³/ч
- `AutoSelectCollectorType_FlowRateJustBelow7_0_SelectsIV_1_5` — тест для расхода 6.99 м³/ч

---

## Новые тесты

| Тест | Описание | Результат |
|------|----------|----------|
| `AutoSelectCollectorType_FlowRateBelow1_5_SelectsHKV_D` | Расход ≤ 1.5 м³/ч → HKV-D | ✅ PASSED |
| `AutoSelectCollectorType_FlowRate1_5To2_5_SelectsIV_1_25` | Расход 1.5 < G < 2.5 м³/ч → IV 1¼" | ✅ PASSED |
| `AutoSelectCollectorType_FlowRate2_5To7_0_SelectsIV_1_5` | Расход 2.5 ≤ G < 7 м³/ч → IV 1½" | ✅ PASSED |
| `AutoSelectCollectorType_FlowRate5_0_SelectsIV_1_5` | Расход 5.0 м³/ч → IV 1½" | ✅ PASSED |
| `AutoSelectCollectorType_FlowRateAbove7_0_SetsWarning` | Расход ≥ 7.0 м³/ч → предупреждение | ✅ PASSED |
| `AutoSelectCollectorType_FlowRateExactly1_5_SelectsHKV_D` | Расход ровно 1.5 м³/ч → HKV-D | ✅ PASSED |
| `AutoSelectCollectorType_FlowRateExactly2_5_SelectsIV_1_5` | Расход ровно 2.5 м³/ч → IV 1½" | ✅ PASSED |
| `AutoSelectCollectorType_FlowRateExactly7_0_SetsWarning` | Расход ровно 7.0 м³/ч → предупреждение | ✅ PASSED |
| `AutoSelectCollectorType_FlowRateJustBelow7_0_SelectsIV_1_5` | Расход 6.99 м³/ч → IV 1½" | ✅ PASSED |
| `AutoSelectCollectorType_ClearsWarningWhenFlowRateDecreases` | Предупреждение очищается при уменьшении расхода | ✅ PASSED |

---

## Регрессионные тесты

Всего тестов в наборе AutoSelectCollectorType: **12**
Пройдено: **12**
Упавших: **0**

---

## Критерии приёмки

| Критерий | Статус |
|----------|--------|
| При расходе ≤ 1.5 м³/ч → HKV-D | ✅ Выполнено |
| При расходе 1.5 < G < 2.5 м³/ч → IV 1¼" | ✅ Выполнено |
| При расходе 2.5 ≤ G < 7 м³/ч → IV 1½" | ✅ Выполнено |
| При расходе ≥ 7.0 м³/ч → предупреждение | ✅ Выполнено |

---

## Итог

✅ **Все тесты прошли успешно**

Диапазоны расхода для автоматического выбора коллектора исправлены в соответствии с требованиями:
- ≤ 1.5 м³/ч → HKV-D (2-12 контуров)
- 1.5 < G < 2.5 м³/ч → IV 1¼" (2-12 контуров)
- 2.5 ≤ G < 7 м³/ч → IV 1½" (2-12 контуров)
- ≥ 7 м³/ч → предупреждение (расход слишком большой)