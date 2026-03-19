# Отчёт о тестировании: Изменения в модуле Тепловой расчёт

## Задача
Внести 2 изменения в модуль Тепловой расчёт:
1. Шаг укладки неактивен без выбора Типа трубы
2. Шаг укладки — выпадающий список с предустановленными значениями

## Изменённые файлы

### 1. ThermalViewModel.cs
**Изменения:**
- `SelectedPipe` изменён с `PipeType` на `PipeType?` (nullable)
- `PipeSpacing` изменён с `double` на `int`
- Добавлено свойство `AvailablePipeSpacings` — массив [150, 200, 250, 300]
- Добавлено свойство `IsPipeSpacingEnabled` — зависит от `SelectedPipe != null`
- Добавлен обработчик `OnSelectedPipeChanged` для уведомления об изменении `IsPipeSpacingEnabled`
- Обновлена валидация — добавлена проверка `SelectedPipe != null`
- Обновлён метод `Reset()` — сбрасывает `SelectedPipe` в `null`

### 2. ThermalView.xaml
**Изменения:**
- TextBox для Шага укладки заменён на ComboBox
- Добавлена привязка `ItemsSource="{Binding AvailablePipeSpacings}"`
- Добавлена привязка `IsEnabled="{Binding IsPipeSpacingEnabled}"`
- Добавлен ItemTemplate для отображения значений с суффиксом "мм"

### 3. ThermalViewModelTests.cs
**Обновлены тесты:**
- `Constructor_InitializesDefaultValues` — проверяет, что `SelectedPipe = null` и `IsPipeSpacingEnabled = false`
- `Constructor_InitializesCollections` — проверяет `AvailablePipeSpacings`
- `Constructor_SelectedPipeIsNullByDefault` — новый тест
- `Reset_ResetsAllPropertiesToDefaults` — проверяет сброс `SelectedPipe` в `null`
- `Validate_SelectedPipeNull_ReturnsFalse` — новый тест для валидации
- `IsPipeSpacingEnabled_FalseWhenNoPipeSelected` — новый тест
- `IsPipeSpacingEnabled_TrueWhenPipeSelected` — новый тест
- `IsPipeSpacingEnabled_RaisesPropertyChangedWhenPipeChanges` — новый тест
- Все тесты, использующие `PipeSpacing`, обновлены для работы с `int`

## Новые тесты

### IsPipeSpacingEnabled Tests
- ✅ `IsPipeSpacingEnabled_FalseWhenNoPipeSelected` — PASSED
- ✅ `IsPipeSpacingEnabled_TrueWhenPipeSelected` — PASSED
- ✅ `IsPipeSpacingEnabled_RaisesPropertyChangedWhenPipeChanges` — PASSED

### SelectedPipe Validation Tests
- ✅ `Validate_SelectedPipeNull_ReturnsFalse` — PASSED
- ✅ `Constructor_SelectedPipeIsNullByDefault` — PASSED

## Регрессионные тесты

### ThermalViewModel Tests
- Всего: 29
- Пройдено: 29
- Не пройдено: 0

## Итог
✅ Все тесты прошли успешно

## Валидация изменений

### Изменение 1: Шаг укладки неактивен без выбора Типа трубы
- ✅ При запуске `SelectedPipe = null`
- ✅ `IsPipeSpacingEnabled = false` когда `SelectedPipe = null`
- ✅ `IsPipeSpacingEnabled = true` когда `SelectedPipe` выбран
- ✅ PropertyChanged вызывается при изменении `SelectedPipe`

### Изменение 2: Шаг укладки — выпадающий список
- ✅ `AvailablePipeSpacings = [150, 200, 250, 300]`
- ✅ `PipeSpacing` имеет тип `int`
- ✅ ComboBox привязан к `AvailablePipeSpacings`
- ✅ ComboBox отключен когда `IsPipeSpacingEnabled = false`

## Открытые вопросы
Открытых вопросов нет