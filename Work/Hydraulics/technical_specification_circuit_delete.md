# Техническое задание: Механизм удаления контура из коллектора

## 1. Описание задачи

Реализовать механизм удаления контура из коллектора в приложении "Калькулятор снеготаяния РЕХАУ". Пользователь должен иметь возможность удалить выбранный контур из таблицы контуров с подтверждением операции и автоматической перенумерацией оставшихся контуров.

---

## 2. Текущее состояние

### 2.1. Модель данных (CircuitRow.cs)

**Файл:** `src/Models/Hydraulics/CircuitRow.cs`

**Свойства:**
- `CircuitNumber` (int) — номер контура
- `CircuitLength` (double) — длина греющего контура, м
- `SupplyLength` (double) — длина подводки, м
- `CircuitArea` (double) — площадь контура, м²
- `PipeSpacing_cm` (double) — шаг укладки, см
- `Power` (double) — мощность контура, Вт
- `FlowRate` (double) — расход теплоносителя, л/ч
- `Velocity` (double) — скорость потока, м/с
- `ValveTurns` (double) — обороты балансировочного клапана
- `OperatingResult` — результаты при рабочей температуре
- `DesignResult` — результаты при расчётной температуре

### 2.2. ViewModel (CircuitsViewModel.cs)

**Файл:** `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

**Уже реализовано:**

| Метод | Строки | Описание |
|-------|--------|----------|
| `RemoveCircuitCommand` | 350-369 | Команда удаления контура |
| `RemoveCircuit(CircuitRow)` | 350-369 | Удаляет контур из коллекции |
| `CanRemoveCircuit(CircuitRow)` | 561-574 | Проверяет возможность удаления (минимум 1 контур) |
| `RenumberCircuits(CollectorData)` | 508-514 | Перенумеровывает контуры после удаления |
| `ConfirmDeleteCircuit(int)` | 529-538 | Диалог подтверждения удаления |

**Логика CanRemoveCircuit:**
```csharp
private bool CanRemoveCircuit(CircuitRow circuit)
{
    // Нельзя удалить, если:
    // 1. Контур не выбран (circuit == null)
    // 2. В коллекторе только 1 контур (минимум 1 контур должен остаться)
    if (circuit == null)
        return false;

    var collector = SelectedCollector;
    if (collector == null)
        return false;

    return collector.Circuits.Count > 1;
}
```

**Логика RemoveCircuit:**
```csharp
[RelayCommand(CanExecute = nameof(CanRemoveCircuit))]
private void RemoveCircuit(CircuitRow circuit)
{
    if (circuit == null)
        return;

    if (!ConfirmDeleteCircuit(circuit.CircuitNumber))
        return;

    var collector = SelectedCollector;
    if (collector == null) return;

    if (collector.Circuits.Contains(circuit))
    {
        collector.Circuits.Remove(circuit);
        RenumberCircuits(collector);
        AddCircuitCommand.NotifyCanExecuteChanged();
        RemoveCircuitCommand.NotifyCanExecuteChanged();
    }
}
```

### 2.3. View (CircuitsView.xaml)

**Файл:** `src/Views/Hydraulics/CircuitsView.xaml`

**Структура DataGrid (строки 599-772):**
- DataGrid с колонками: №, Длина, Подводка, Площадь, Шаг, Мощность, Расход, Скорость, Re, λ, Режим, Уд.потери, Δp контур, Δp клапан, Δp сумма, **Обороты**
- Последний столбец — "Обороты" (ValveTurns)
- **НЕТ столбца с кнопкой удаления**

**Кнопки управления (строки 779-788):**
- Кнопка "+ Добавить контур" — привязана к `AddCircuitCommand`
- Кнопка "Рассчитать" — привязана к `CalculateCommand`
- **НЕТ кнопки удаления контура**

---

## 3. Требования к UI

### 3.1. Расположение кнопки удаления

**Вариант А: Кнопка под таблицей (рекомендуется)**
```
+--------------------------------------------------+
| DataGrid с контурами                              |
| ...                                              |
+--------------------------------------------------+
| [+ Добавить контур]  [- Удалить контур]  [Рассчитать] |
+--------------------------------------------------+
```

**Вариант Б: Кнопка в каждой строке**
```
| № | Длина | ... | Обороты | [X] |
| 1 | 100   | ... | 3.5     | [X] |
| 2 | 120   | ... | 2.8     | [X] |
```

### 3.2. Требования к кнопке удаления

| Параметр | Значение |
|----------|----------|
| Текст | "- Удалить контур" |
| Стиль | `SecondaryButtonStyle` (серый фон) |
| Расположение | Справа от кнопки "+ Добавить контур" |
| Состояние | Неактивна, если контур не выбран или в коллекторе 1 контур |
| Команда | `RemoveCircuitCommand` |
| Параметр | Выбранный контур (`SelectedItem` DataGrid) |

### 3.3. Требования к DataGrid

| Параметр | Текущее значение | Требуемое значение |
|----------|------------------|-------------------|
| `SelectedItem` | Не привязан | Привязать к `SelectedCircuit` в ViewModel |
| `SelectedIndex` | Не привязан | Опционально, для отслеживания выбора |
| `IsEnabled` кнопки | — | Привязать к `CanRemoveCircuit` |

---

## 4. Требования к логике

### 4.1. Основной сценарий удаления

```
1. Пользователь выбирает контур в DataGrid (клик по строке)
2. Кнопка "- Удалить контур" становится активной
3. Пользователь нажимает кнопку
4. Система показывает диалог подтверждения:
   "Вы уверены, что хотите удалить контур №{N}?"
   [Да] [Нет]
5. При подтверждении:
   a. Контур удаляется из коллекции Circuits
   b. Оставшиеся контуры перенумеровываются (1, 2, 3...)
   c. Выполняется пересчёт коллектора
   d. Обновляется UI
6. При отмене — ничего не происходит
```

### 4.2. Альтернативные сценарии

#### А1: Попытка удалить последний контур
```
1. В коллекторе 1 контур
2. Кнопка "- Удалить контур" неактивна (IsEnabled = false)
3. Пользователь не может удалить контур
```

#### А2: Контур не выбран
```
1. Пользователь не выбрал контур в DataGrid
2. Кнопка "- Удалить контур" неактивна
3. Пользователь не может удалить контур
```

#### А3: Отмена подтверждения
```
1. Пользователь нажал кнопку удаления
2. Показан диалог подтверждения
3. Пользователь нажал [Нет]
4. Контур НЕ удаляется
5. UI остаётся без изменений
```

### 4.3. Постусловия

После успешного удаления:
- ✅ Контур удалён из коллекции `collector.Circuits`
- ✅ Номера контуров перенумерованы (1, 2, 3...)
- ✅ `SelectedCircuit` сброшен или установлен на следующий контур
- ✅ Кнопка удаления обновила состояние (CanExecute)
- ✅ Результаты коллектора пересчитаны

---

## 5. Лучшие практики WPF MVVM для удаления строк

### 5.1. Паттерн Command с параметром

```csharp
// В ViewModel
[RelayCommand(CanExecute = nameof(CanRemoveCircuit))]
private void RemoveCircuit(CircuitRow circuit)
{
    // Логика удаления
}

private bool CanRemoveCircuit(CircuitRow circuit)
{
    return circuit != null && collector.Circuits.Count > 1;
}
```

```xml
<!-- В XAML -->
<Button Content="- Удалить контур"
        Command="{Binding RemoveCircuitCommand}"
        CommandParameter="{Binding SelectedCircuit}"/>
```

### 5.2. Паттерн с SelectedItem

```csharp
// В ViewModel
[ObservableProperty]
private CircuitRow? _selectedCircuit;

[RelayCommand(CanExecute = nameof(CanRemoveCircuit))]
private void RemoveCircuit()
{
    if (SelectedCircuit == null) return;
    // Логика удаления
}

private bool CanRemoveCircuit()
{
    return SelectedCircuit != null && collector.Circuits.Count > 1;
}
```

```xml
<!-- В XAML -->
<DataGrid SelectedItem="{Binding SelectedCircuit, Mode=TwoWay}">
    ...
</DataGrid>

<Button Content="- Удалить контур"
        Command="{Binding RemoveCircuitCommand}"/>
```

### 5.3. Паттерн с кнопкой в строке (DataGridTemplateColumn)

```xml
<DataGridTemplateColumn Header="" Width="Auto">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Button Content="X"
                    Command="{Binding DataContext.RemoveCircuitCommand, 
                             RelativeSource={RelativeSource AncestorType=UserControl}}"
                    CommandParameter="{Binding}"
                    Width="25" Height="25"/>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

---

## 6. Варианты реализации

### Вариант 1: Кнопка под таблицей (рекомендуется)

**Описание:**
Добавить кнопку "- Удалить контур" рядом с кнопкой "+ Добавить контур" под таблицей DataGrid.

**Изменения:**

1. **ViewModel (CircuitsViewModel.cs):**
   - Добавить свойство `SelectedCircuit` для хранения выбранного контура
   - Изменить `CanRemoveCircuit()` для работы без параметра (использовать `SelectedCircuit`)
   - Изменить `RemoveCircuit()` для работы без параметра

2. **View (CircuitsView.xaml):**
   - Добавить привязку `SelectedItem="{Binding SelectedCircuit, Mode=TwoWay}"` к DataGrid
   - Добавить кнопку "- Удалить контур" рядом с "+ Добавить контур"

**Плюсы:**
- ✅ Консистентность с существующим UI (кнопки добавления/удаления коллекторов)
- ✅ Минимальные изменения в коде
- ✅ Понятный UX для пользователя
- ✅ Кнопка всегда видна в одном месте

**Минусы:**
- ⚠️ Требуется дополнительный клик для выбора контура перед удалением
- ⚠️ Нужно добавить свойство `SelectedCircuit` в ViewModel

**Оценка трудозатрат:** 1-2 часа

---

### Вариант 2: Кнопка в каждой строке DataGrid

**Описание:**
Добавить столбец с кнопкой удаления в конце каждой строки DataGrid.

**Изменения:**

1. **View (CircuitsView.xaml):**
   - Добавить `DataGridTemplateColumn` с кнопкой "X" в конце таблицы
   - Кнопка привязана к `RemoveCircuitCommand` с `CommandParameter="{Binding}"`

2. **ViewModel:** Без изменений (использует существующий `RemoveCircuit(CircuitRow circuit)`)

**Плюсы:**
- ✅ Не нужно добавлять `SelectedCircuit` в ViewModel
- ✅ Удаление в один клик (без предварительного выбора)
- ✅ Визуально понятно, какую строку удаляешь

**Минусы:**
- ⚠️ Кнопка занимает место в каждой строке
- ⚠️ Может выглядеть загромождённо при большом количестве столбцов
- ⚠️ Несовместимо с существующим стилем (кнопки управления под таблицей)

**Оценка трудозатрат:** 1 час

---

### Вариант 3: Контекстное меню (правый клик)

**Описание:**
Добавить контекстное меню для строк DataGrid с пунктом "Удалить контур".

**Изменения:**

1. **View (CircuitsView.xaml):**
   - Добавить `DataGrid.ContextMenu` с пунктом "Удалить контур"
   - Привязать к `RemoveCircuitCommand` с `CommandParameter="{Binding SelectedItem}"`

2. **ViewModel:** Добавить свойство `SelectedCircuit`

**Плюсы:**
- ✅ Не занимает место в UI
- ✅ Стандартный паттерн для Windows-приложений

**Минусы:**
- ⚠️ Неочевидно для пользователя (нужно знать о контекстном меню)
- ⚠️ Требуется дополнительный клик (правый клик → выбор пункта)

**Оценка трудозатрат:** 1 час

---

### Вариант 4: Клавиша Delete

**Описание:**
Обработать нажатие клавиши Delete для удаления выбранного контура.

**Изменения:**

1. **View (CircuitsView.xaml):**
   - Добавить `KeyBinding` для клавиши Delete
   - Привязать к `RemoveCircuitCommand`

2. **ViewModel:** Добавить свойство `SelectedCircuit`

**Плюсы:**
- ✅ Быстрое удаление для опытных пользователей
- ✅ Не занимает место в UI

**Минусы:**
- ⚠️ Неочевидно для пользователя
- ⚠️ Требуется документация или подсказка

**Оценка трудозатрат:** 30 минут

---

## 7. Рекомендуемый вариант

### Вариант 1: Кнопка под таблицей

**Обоснование:**

1. **Консистентность:** В приложении уже есть кнопки "+ Добавить коллектор" и "- Удалить коллектор" под таблицей. Аналогичный подход для контуров обеспечит единообразие UI.

2. **Понятность:** Пользователь сразу видит кнопку удаления и понимает, как ей пользоваться.

3. **Минимальные изменения:** Требуется только добавить свойство `SelectedCircuit` и кнопку в XAML.

4. **Соответствие MVVM:** Используется стандартный паттерн с `SelectedItem` и командой.

**Рекомендуемая реализация:**

```xml
<!-- DataGrid с привязкой SelectedItem -->
<DataGrid Grid.Row="1"
          x:Name="CircuitsDataGrid"
          ItemsSource="{Binding Circuits}"
          SelectedItem="{Binding SelectedCircuit, Mode=TwoWay}"
          ...>

<!-- Кнопки управления -->
<StackPanel Grid.Row="2"
            Orientation="Horizontal"
            HorizontalAlignment="Left"
            Margin="0,15,0,0">
    <Button Content="+ Добавить контур"
            Command="{Binding DataContext.AddCircuitCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
            IsEnabled="{Binding DataContext.CanAddCircuit, RelativeSource={RelativeSource AncestorType=UserControl}}"
            Style="{StaticResource PrimaryButtonStyle}"
            Margin="0,5,10,5"/>

    <Button Content="- Удалить контур"
            Command="{Binding DataContext.RemoveCircuitCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
            CommandParameter="{Binding DataContext.SelectedCircuit, RelativeSource={RelativeSource AncestorType=UserControl}}"
            IsEnabled="{Binding DataContext.CanRemoveCircuit, RelativeSource={RelativeSource AncestorType=UserControl}}"
            Style="{StaticResource SecondaryButtonStyle}"
            Margin="0,5,10,5"/>

    <Button Content="Рассчитать"
            Command="{Binding DataContext.CalculateCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
            Style="{StaticResource PrimaryButtonStyle}"
            Margin="0,5,0,5"/>
</StackPanel>
```

**Изменения в ViewModel:**

```csharp
// Добавить свойство
[ObservableProperty]
private CircuitRow? _selectedCircuit;

// CanRemoveCircuit уже существует и работает с параметром
// Можно оставить как есть или адаптировать
```

---

## 8. План тестирования

### 8.1. Функциональные тесты

| ID | Тест | Ожидаемый результат |
|----|------|---------------------|
| FT-1 | Удаление контура (2+ контура) | Контур удалён, номера перенумерованы |
| FT-2 | Удаление последнего контура | Кнопка неактивна, удаление невозможно |
| FT-3 | Удаление без выбора | Кнопка неактивна |
| FT-4 | Отмена в диалоге | Контур не удалён |
| FT-5 | Перенумерация после удаления | Номера: 1, 2, 3... (без пропусков) |
| FT-6 | Пересчёт после удаления | Результаты коллектора обновлены |

### 8.2. UI тесты

| ID | Тест | Ожидаемый результат |
|----|------|---------------------|
| UI-1 | Кнопка видима | Кнопка "- Удалить контур" отображается |
| UI-2 | Кнопка активна (2+ контура, выбран) | IsEnabled = true |
| UI-3 | Кнопка неактивна (1 контур) | IsEnabled = false |
| UI-4 | Кнопка неактивна (не выбран) | IsEnabled = false |
| UI-5 | Диалог подтверждения | Показывается с номером контура |
| UI-6 | Стиль кнопки | SecondaryButtonStyle (серый фон) |

### 8.3. Интеграционные тесты

| ID | Тест | Ожидаемый результат |
|----|------|---------------------|
| IT-1 | Удаление → Пересчёт | Мощность, расход, потери пересчитаны |
| IT-2 | Удаление → Балансировка | Обороты клапанов пересчитаны |
| IT-3 | Удаление → Результаты коллектора | Итоги обновлены |
| IT-4 | Переключение коллекторов | SelectedCircuit сброшен |

### 8.4. Краевые случаи

| ID | Тест | Ожидаемый результат |
|----|------|---------------------|
| EC-1 | Удаление первого контура | Остальные перенумерованы |
| EC-2 | Удаление последнего контура | Остальные без изменений |
| EC-3 | Удаление среднего контура | Остальные перенумерованы |
| EC-4 | Быстрое удаление нескольких | Корректная перенумерация |

---

## 9. Открытые вопросы

### Вопрос 1: Выбор варианта реализации
**Статус:** Рекомендуется Вариант 1 (кнопка под таблицей)
**Требует подтверждения:** Да

### Вопрос 2: Сброс SelectedCircuit после удаления
**Варианты:**
- A: Сбросить в null (ничего не выбрано)
- B: Выбрать следующий контур
- C: Выбрать предыдущий контур

**Рекомендация:** Вариант A (сброс в null) — самый простой и безопасный.

### Вопрос 3: Автоматический пересчёт после удаления
**Варианты:**
- A: Автоматически вызывать Calculate()
- B: Требовать ручного нажатия "Рассчитать"

**Рекомендация:** Вариант A — автоматически пересчитывать для удобства пользователя.

---

## 10. Файлы для изменения

| Файл | Изменения |
|------|-----------|
| `src/ViewModels/Hydraulics/CircuitsViewModel.cs` | Добавить `SelectedCircuit` свойство |
| `src/Views/Hydraulics/CircuitsView.xaml` | Добавить привязку `SelectedItem` и кнопку удаления |

---

## 11. Критерии приёмки

- ✅ Кнопка "- Удалить контур" отображается под таблицей контуров
- ✅ Кнопка активна только при выбранном контуре и наличии 2+ контуров
- ✅ При нажатии показывается диалог подтверждения с номером контура
- ✅ После подтверждения контур удаляется
- ✅ Оставшиеся контуры автоматически перенумеровываются
- ✅ Результаты коллектора автоматически пересчитываются
- ✅ UI консистентен с кнопками удаления коллекторов
- ✅ Код соответствует MVVM паттерну

---

*ТЗ создано: 2026-03-21*
*Модуль: Hydraulics*
*Задача: Механизм удаления контура из коллектора*