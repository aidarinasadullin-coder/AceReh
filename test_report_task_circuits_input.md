# Отчёт о тестировании: Сценарии ввода длины и площади контура

## Дата: 2026-03-19

## Новые тесты

### CircuitRowTests (20 тестов)

| Тест | Статус | Описание |
|------|--------|----------|
| Constructor_DefaultValues_AreCorrect | ✅ PASSED | Проверка начальных значений |
| OnCircuitLengthChanged_WhenUserEntersLength_AreaIsCalculated | ✅ PASSED | Сценарий 2.1: Ввод длины → вычисление площади |
| OnCircuitLengthChanged_WithDifferentPipeSpacing_AreaIsCorrect | ✅ PASSED | Проверка разных шагов укладки |
| OnCircuitLengthChanged_WhenUserEntersLengthMultipleTimes_AreaIsRecalculated | ✅ PASSED | Повторный ввод длины |
| OnCircuitAreaChanged_WhenUserEntersArea_LengthIsCalculated | ✅ PASSED | Сценарий 2.2: Ввод площади → вычисление длины |
| OnCircuitAreaChanged_WithDifferentPipeSpacing_LengthIsCorrect | ✅ PASSED | Проверка разных шагов укладки |
| OnCircuitLengthChanged_WhenUserClearsLength_BothFieldsAreActive | ✅ PASSED | Сценарий 2.3: Очистка длины |
| OnCircuitAreaChanged_WhenUserClearsArea_BothFieldsAreActive | ✅ PASSED | Сценарий 2.4: Очистка площади |
| Switching_FromLengthToArea_UpdatesFlagsCorrectly | ✅ PASSED | Переключение с длины на площадь |
| Switching_FromAreaToLength_UpdatesFlagsCorrectly | ✅ PASSED | Переключение с площади на длину |
| OnPipeSpacingChanged_WhenLengthWasEntered_AreaIsRecalculated | ✅ PASSED | Изменение шага при введённой длине |
| OnPipeSpacingChanged_WhenAreaWasEntered_LengthIsRecalculated | ✅ PASSED | Изменение шага при введённой площади |
| OnPipeSpacingChanged_WhenNoUserInput_NoRecalculation | ✅ PASSED | Изменение шага без пользовательского ввода |
| OnCircuitLengthChanged_ZeroValue_ClearsFlags | ✅ PASSED | Граничный случай: нулевая длина |
| OnCircuitAreaChanged_ZeroValue_ClearsFlags | ✅ PASSED | Граничный случай: нулевая площадь |
| IsLengthReadOnly_WhenAreaIsZero_ReturnsFalse | ✅ PASSED | Граничный случай: площадь = 0 |
| IsAreaReadOnly_WhenLengthIsZero_ReturnsFalse | ✅ PASSED | Граничный случай: длина = 0 |
| Formula_AreaFromLength_IsCorrect | ✅ PASSED | Проверка формулы S = L / (100 / VA_hk) |
| Formula_LengthFromArea_IsCorrect | ✅ PASSED | Проверка формулы L = S × (100 / VA_hk) |
| Formula_RoundTrip_IsConsistent | ✅ PASSED | Круговой расчёт |

## Регрессионные тесты

### Исправленные тесты (HydraulicsIntegrationTests)

| Тест | Статус | Изменение |
|------|--------|-----------|
| FullCalculation_WorksCorrectly | ✅ PASSED | Обновлён API (PipeSpacing_cm) |
| Integration_WithGlycolService | ✅ PASSED | Без изменений |
| Integration_MultipleCollectors | ✅ PASSED | Обновлён API (PipeSpacing_cm) |
| Integration_Balancing | ✅ PASSED | Обновлён API (PipeSpacing_cm) |

## Итог

✅ **Все тесты прошли успешно**

- Новых тестов: 20
- Пройдено: 20
- Не пройдено: 0

## Изменённые файлы

### Новые файлы:
- `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CircuitRowTests.cs` — тесты для CircuitRow

### Изменённые файлы:
- `src/Models/Hydraulics/CircuitRow.cs` — добавлены свойства и обработчики
- `src/ViewModels/Hydraulics/CircuitsViewModel.cs` — подписка на изменения PipeSpacing
- `src/Views/Hydraulics/CircuitsView.xaml` — стили для заблокированных полей
- `tests/SnowMeltingCalculator.Tests/Integration/HydraulicsIntegrationTests.cs` — обновлён API

## Реализованные сценарии

### 2.1 Пользователь ввёл длину
- ✅ Площадь вычисляется автоматически
- ✅ Ячейка площади становится только для чтения
- ✅ Визуально: серый фон, IsEnabled=False

### 2.2 Пользователь ввёл площадь
- ✅ Длина вычисляется автоматически
- ✅ Ячейка длины становится только для чтения
- ✅ Визуально: серый фон, IsEnabled=False

### 2.3 Пользователь очистил длину
- ✅ Оба поля становятся активными

### 2.4 Пользователь очистил площадь
- ✅ Оба поля становятся активными

### Дополнительно
- ✅ При изменении шага укладки пересчитывается связанное поле
- ✅ Переключение между вводом длины и площади работает корректно