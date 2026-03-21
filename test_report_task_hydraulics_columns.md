# Отчёт о тестировании: Добавление столбцов Re и λ в таблицу контуров

## Дата: 2026-03-20

## Задача
Добавить в таблицу контуров новые столбцы:
- Число Рейнольдса (Re)
- Лямбда (λ) - коэффициент трения, 4 знака после запятой
- Удельные потери (Па/м)
- Потери в трубе (Па) → изменено на мбар
- Потери в вентиле (Па) → изменено на мбар

---

## Выполненные изменения

### 1. Модель CircuitTemperatureResult (CircuitRow.cs)

**Добавлены свойства:**
```csharp
/// <summary>
/// Потери в трубе контура, мбар
/// </summary>
public double CircuitPipeLoss_mbar => CircuitPipeLoss / 100.0;

/// <summary>
/// Потери в вентиле, мбар
/// </summary>
public double ValveLoss_mbar => ValveLoss / 100.0;
```

### 2. Представление CircuitsView.xaml

**Изменения в таблице контуров:**

| № | Столбец | Binding | Формат | Статус |
|---|---------|---------|--------|--------|
| 1 | № | CircuitNumber | F0 | Без изменений |
| 2 | Длина (м) | CircuitLength | F1 | Без изменений |
| 3 | Подводка (м) | SupplyLength | F1 | Без изменений |
| 4 | Площадь (м²) | CircuitArea | F1 | Без изменений |
| 5 | Шаг (см) | PipeSpacing_cm | F0 | Без изменений |
| 6 | Мощность (Вт) | Power | F0 | Без изменений |
| 7 | Расход (л/ч) | FlowRate | F1 | Без изменений |
| 8 | Скорость (м/с) | Velocity | F3 | Без изменений |
| 9 | Re | CurrentResult.ReynoldsNumber | F0 | Без изменений |
| 10 | **λ** | **CurrentResult.FrictionFactor** | **F4** | **НОВЫЙ** |
| 11 | Режим | FlowRegimeDescription | - | Без изменений |
| 12 | Уд.потери (Па/м) | CurrentResult.PressureLossPerMeter | F1 | Без изменений |
| 13 | Δp контур (мбар) | CurrentResult.CircuitPipeLoss_mbar | F1 | **ИЗМЕНЕНО** |
| 14 | Δp клапан (мбар) | CurrentResult.ValveLoss_mbar | F1 | **ИЗМЕНЕНО** |
| 15 | Обороты | ValveTurns | F1 | Перемещён в конец |

**Удалённые столбцы:**
- "Потери (мбар)" - заменён на отдельные столбцы Δp контур и Δp клапан

---

## Новые тесты

### CircuitTemperatureResult Tests

| Тест | Описание | Статус |
|------|----------|--------|
| `CircuitTemperatureResult_CircuitPipeLoss_mbar_ConvertsCorrectly` | Проверка конвертации Па → мбар | ✅ PASSED |
| `CircuitTemperatureResult_ValveLoss_mbar_ConvertsCorrectly` | Проверка конвертации Па → мбар | ✅ PASSED |
| `CircuitTemperatureResult_TotalLoss_CalculatesCorrectly` | Проверка суммарных потерь | ✅ PASSED |
| `CircuitTemperatureResult_TotalLoss_mbar_ConvertsCorrectly` | Проверка конвертации суммарных потерь | ✅ PASSED |
| `CircuitTemperatureResult_ZeroLosses_ReturnsZero_mbar` | Проверка нулевых значений | ✅ PASSED |
| `CircuitTemperatureResult_FrictionFactor_CanBeSet` | Проверка установки λ | ✅ PASSED |
| `CircuitTemperatureResult_ReynoldsNumber_CanBeSet` | Проверка установки Re | ✅ PASSED |
| `CircuitTemperatureResult_PressureLossPerMeter_CanBeSet` | Проверка удельных потерь | ✅ PASSED |

---

## Регрессионные тесты

### Существующие тесты CircuitRowTests

| Категория | Количество тестов | Статус |
|-----------|-------------------|--------|
| Базовые свойства | 1 | ✅ PASSED |
| Сценарий 2.1: Ввод длины | 3 | ✅ PASSED |
| Сценарий 2.2: Ввод площади | 2 | ✅ PASSED |
| Сценарий 2.3: Очистка длины | 1 | ✅ PASSED |
| Сценарий 2.4: Очистка площади | 1 | ✅ PASSED |
| Переключение ввода | 2 | ✅ PASSED |
| Изменение шага | 3 | ✅ PASSED |
| Граничные случаи | 4 | ✅ PASSED |
| Формулы | 3 | ✅ PASSED |
| CircuitNumber | 6 | ✅ PASSED |

**Всего:** 26 тестов - все пройдены

---

## Сборка проекта

| Конфигурация | Статус | Предупреждения | Ошибки |
|--------------|--------|----------------|--------|
| Release | ✅ Успешно | 10 (существующие) | 0 |

**Примечание:** Предупреждения MVVMTK0034 и CS8603 - существующие, не связаны с изменениями.

---

## Проверка соответствия ТЗ

### Требования из technical_specification_interface.md

| Требование | Статус | Примечание |
|------------|--------|------------|
| Столбец λ отображается после Re | ✅ | Строка 606-614 |
| Значение λ с точностью 4 знака | ✅ | StringFormat=F4 |
| Значение из CircuitTemperatureResult.FrictionFactor | ✅ | Binding корректный |
| Обновление при переключении режима | ✅ | Binding к CurrentResult |
| Фон столбца — серый | ✅ | ReadOnlyCellStyle |
| Пустые контуры с "—" | ✅ | Binding Mode=OneWay |

---

## Итог

✅ **Все тесты прошли успешно**

### Изменённые файлы:
1. `src/Models/Hydraulics/CircuitRow.cs` - добавлены свойства CircuitPipeLoss_mbar и ValveLoss_mbar
2. `src/Views/Hydraulics/CircuitsView.xaml` - добавлен столбец λ, изменены привязки для потерь
3. `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CircuitRowTests.cs` - добавлены тесты для новых свойств

### Новые функциональные возможности:
- Столбец λ (коэффициент трения) с форматом F4
- Корректное отображение потерь в мбар
- Соответствие порядка столбцов ТЗ