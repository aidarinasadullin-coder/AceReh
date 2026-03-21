# План задач: Механизм удаления контура из коллектора

## Мета-информация

| Параметр | Значение |
|----------|----------|
| Модуль | Hydraulics |
| Задача | Реализация механизма удаления контура из коллектора |
| Вариант реализации | Вариант 1 (кнопка под таблицей) |
| ТЗ | `Work/Hydraulics/technical_specification_circuit_delete.md` |
| Дата создания | 2026-03-21 |

---

## Обзор

### Текущее состояние

**Уже реализовано в ViewModel:**
- `RemoveCircuitCommand` — команда удаления контура (строки 350-369)
- `CanRemoveCircuit(CircuitRow circuit)` — проверка возможности удаления (строки 561-574)
- `RenumberCircuits(CollectorData collector)` — перенумерация контуров (строки 508-514)
- `ConfirmDeleteCircuit(int circuitNumber)` — диалог подтверждения (строки 529-538)

**Отсутствует:**
- Свойство `SelectedCircuit` в ViewModel для отслеживания выбранного контура
- Привязка `SelectedItem` в DataGrid
- Кнопка "- Удалить контур" в UI

### Что нужно сделать

| Компонент | Изменение |
|-----------|-----------|
| `CircuitsViewModel.cs` | Добавить свойство `SelectedCircuit` |
| `CircuitsView.xaml` | Добавить привязку `SelectedItem` к DataGrid |
| `CircuitsView.xaml` | Добавить кнопку "- Удалить контур" |

---

## Задачи

### Задача 1: Добавить свойство SelectedCircuit и обработку переключения коллекторов

**Приоритет:** Высокий  
**Зависимости:** Нет  
**Оценка времени:** 20 минут  
**Файлы:** `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

#### Описание

Добавить observable-свойство `SelectedCircuit` для отслеживания выбранного контура в DataGrid и реализовать сброс этого свойства при переключении коллекторов (митигация риска).

#### Изменения

**Файл:** `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

**Изменение 1: Добавить свойство SelectedCircuit**

**Место вставки:** После строки 108 (после свойства `CanAddCircuit`)

```csharp
/// <summary>
/// Выбранный контур в DataGrid
/// </summary>
/// <remarks>
/// Используется для команды удаления контура.
/// Привязан к SelectedItem DataGrid.
/// Сбрасывается в null при переключении коллекторов.
/// </remarks>
[ObservableProperty]
private CircuitRow? _selectedCircuit;
```

**Изменение 2: Добавить обработчик изменения SelectedCollectorIndex**

**Место:** В методе `OnSelectedCollectorIndexChanged` или в partial-методе `OnSelectedCollectorIndexChanged` (если используется Source Generator)

```csharp
/// <summary>
/// Обработчик изменения выбранного коллектора.
/// Сбрасывает выбранный контур при переключении коллекторов.
/// </summary>
partial void OnSelectedCollectorIndexChanged(int value)
{
    // Митигация риска: SelectedCircuit не обновляется при переключении коллекторов
    // Сбрасываем SelectedCircuit в null, чтобы избежать удаления контура из другого коллектора
    SelectedCircuit = null;
}
```

**Примечание:** Если в ViewModel уже используется атрибут `[ObservableProperty]` для `SelectedCollectorIndex`, то partial-метод `OnSelectedCollectorIndexChanged` генерируется автоматически. Нужно только добавить его реализацию.

#### Обоснование

- Свойство `SelectedCircuit` необходимо для привязки к `SelectedItem` DataGrid
- Используется командой `RemoveCircuitCommand` для определения контура для удаления
- Атрибут `[ObservableProperty]` автоматически генерирует INPC
- **Митигация риска:** Сброс `SelectedCircuit` при переключении коллекторов предотвращает случайное удаление контура из другого коллектора

#### Критерии приёмки

- [ ] Свойство `SelectedCircuit` добавлено в класс `CircuitsViewModel`
- [ ] Свойство имеет тип `CircuitRow?`
- [ ] Свойство помечено атрибутом `[ObservableProperty]`
- [ ] Добавлен обработчик `OnSelectedCollectorIndexChanged`
- [ ] При переключении коллектора `SelectedCircuit` сбрасывается в `null`
- [ ] Код компилируется без ошибок

---

### Задача 2: Добавить привязку SelectedItem к DataGrid

**Приоритет:** Высокий  
**Зависимости:** Задача 1  
**Оценка времени:** 10 минут  
**Файлы:** `src/Views/Hydraulics/CircuitsView.xaml`

#### Описание

Добавить привязку `SelectedItem` к DataGrid для отслеживания выбранного контура.

#### Изменения

**Файл:** `src/Views/Hydraulics/CircuitsView.xaml`

**Место изменения:** Строка 599-611 (открывающий тег DataGrid)

**Было:**
```xml
<DataGrid Grid.Row="1"
          x:Name="CircuitsDataGrid"
          ItemsSource="{Binding Circuits}"
          AutoGenerateColumns="False"
          CanUserAddRows="False"
          CanUserDeleteRows="False"
          IsReadOnly="False"
          GridLinesVisibility="All"
          HorizontalGridLinesBrush="#E0E0E0"
          VerticalGridLinesBrush="#E0E0E0"
          AlternatingRowBackground="#FAFAFA"
          HeadersVisibility="Column"
          ColumnHeaderStyle="{StaticResource DataGridHeaderStyle}">
```

**Стало:**
```xml
<DataGrid Grid.Row="1"
          x:Name="CircuitsDataGrid"
          ItemsSource="{Binding Circuits}"
          SelectedItem="{Binding SelectedCircuit, Mode=TwoWay}"
          AutoGenerateColumns="False"
          CanUserAddRows="False"
          CanUserDeleteRows="False"
          IsReadOnly="False"
          GridLinesVisibility="All"
          HorizontalGridLinesBrush="#E0E0E0"
          VerticalGridLinesBrush="#E0E0E0"
          AlternatingRowBackground="#FAFAFA"
          HeadersVisibility="Column"
          ColumnHeaderStyle="{StaticResource DataGridHeaderStyle}">
```

#### Обоснование

- Привязка `SelectedItem` синхронизирует выбор в DataGrid со свойством `SelectedCircuit` в ViewModel
- `Mode=TwoWay` обеспечивает двустороннюю привязку (из UI в ViewModel и обратно)

#### Критерии приёмки

- [ ] Привязка `SelectedItem="{Binding SelectedCircuit, Mode=TwoWay}"` добавлена к DataGrid
- [ ] При выборе строки в DataGrid свойство `SelectedCircuit` обновляется
- [ ] При программном изменении `SelectedCircuit` строка в DataGrid выделяется

---

### Задача 3: Добавить кнопку "- Удалить контур" в UI

**Приоритет:** Высокий  
**Зависимости:** Задача 1, Задача 2  
**Оценка времени:** 15 минут  
**Файлы:** `src/Views/Hydraulics/CircuitsView.xaml`

#### Описание

Добавить кнопку "- Удалить контур" рядом с кнопкой "+ Добавить контур" под таблицей DataGrid.

#### Изменения

**Файл:** `src/Views/Hydraulics/CircuitsView.xaml`

**Место изменения:** Строки 774-789 (блок кнопок управления)

**Было:**
```xml
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

    <Button Content="Рассчитать"
            Command="{Binding DataContext.CalculateCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
            Style="{StaticResource PrimaryButtonStyle}"
            Margin="0,5,0,5"/>
</StackPanel>
```

**Стало:**
```xml
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
            Style="{StaticResource SecondaryButtonStyle}"
            Margin="0,5,10,5"/>

    <Button Content="Рассчитать"
            Command="{Binding DataContext.CalculateCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
            Style="{StaticResource PrimaryButtonStyle}"
            Margin="0,5,0,5"/>
</StackPanel>
```

#### Обоснование

- Кнопка размещается между "+ Добавить контур" и "Рассчитать" для консистентности с UI удаления коллекторов
- Используется `SecondaryButtonStyle` (серый фон) для визуального отличия от основных действий
- `CommandParameter` передаёт выбранный контур в команду `RemoveCircuitCommand`
- Кнопка автоматически становится неактивной, когда `CanRemoveCircuit` возвращает `false`

#### Критерии приёмки

- [ ] Кнопка "- Удалить контур" отображается под таблицей контуров
- [ ] Кнопка использует стиль `SecondaryButtonStyle`
- [ ] Кнопка привязана к `RemoveCircuitCommand`
- [ ] Кнопка передаёт `SelectedCircuit` как `CommandParameter`
- [ ] Кнопка неактивна, если контур не выбран
- [ ] Кнопка неактивна, если в коллекторе только 1 контур

---

### Задача 4: Тестирование функциональности

**Приоритет:** Средний  
**Зависимости:** Задача 1, Задача 2, Задача 3  
**Оценка времени:** 30 минут  
**Файлы:** `tests/SnowMeltingCalculator.Tests/ViewModels/Hydraulics/CircuitsViewModelTests.cs`

#### Описание

Добавить модульные тесты для проверки функциональности удаления контура.

#### Тест-кейсы

| ID | Тест | Описание |
|----|------|----------|
| FT-1 | `RemoveCircuit_SelectedCircuit_RemovesAndRenumbers` | Удаление выбранного контура, проверка перенумерации |
| FT-2 | `RemoveCircuit_LastCircuit_CannotRemove` | Попытка удалить последний контур — кнопка неактивна |
| FT-3 | `RemoveCircuit_NoSelection_CannotRemove` | Контур не выбран — кнопка неактивна |
| FT-4 | `RemoveCircuit_ConfirmationDialog_ShowsCircuitNumber` | Диалог подтверждения содержит номер контура |
| FT-5 | `RemoveCircuit_CancelConfirmation_DoesNotRemove` | Отмена подтверждения — контур не удалён |
| FT-6 | `SelectedCircuit_PropertyChanged_RaisesEvent` | Изменение SelectedCircuit вызывает PropertyChanged |
| FT-7 | `SelectedCircuit_ResetOnCollectorChange` | Проверка сброса SelectedCircuit при переключении коллекторов |

#### Критерии приёмки

- [ ] Все тесты проходят
- [ ] Покрытие кода ≥ 80% для изменённых методов

---

## Диаграмма зависимостей

```
┌─────────────────────────────────────────────────────────────┐
│                     Задача 1                                │
│   Добавить SelectedCircuit + обработчик переключения        │
│                   (20 мин)                                   │
└────────────────────────┬────────────────────────────────────┘
                          │
           ┌──────────────┴──────────────┐
           │                              │
           ▼                              ▼
┌─────────────────────┐    ┌─────────────────────────────────┐
│     Задача 2        │    │          Задача 3               │
│  SelectedItem в     │    │   Кнопка "- Удалить контур"     │
│     DataGrid        │    │         в UI                    │
│    (10 мин)         │    │        (15 мин)                  │
└─────────────────────┘    └─────────────────────────────────┘
           │                              │
           └──────────────┬───────────────┘
                          │
                          ▼
           ┌─────────────────────────────────┐
           │          Задача 4               │
           │       Тестирование              │
           │          (30 мин)               │
           └─────────────────────────────────┘
```

---

## Общая оценка времени

| Задача | Время |
|--------|-------|
| Задача 1 | 20 мин |
| Задача 2 | 10 мин |
| Задача 3 | 15 мин |
| Задача 4 | 30 мин |
| **Итого** | **75 мин (≈ 1.5 часа)** |

---

## Файлы для изменения

| Файл | Изменения |
|------|-----------|
| `src/ViewModels/Hydraulics/CircuitsViewModel.cs` | Добавить свойство `SelectedCircuit` и обработчик `OnSelectedCollectorIndexChanged` |
| `src/Views/Hydraulics/CircuitsView.xaml` | Добавить привязку `SelectedItem` и кнопку удаления |
| `tests/SnowMeltingCalculator.Tests/ViewModels/Hydraulics/CircuitsViewModelTests.cs` | Добавить тесты (опционально) |

---

## Критерии приёмки (общие)

- [ ] Кнопка "- Удалить контур" отображается под таблицей контуров
- [ ] Кнопка активна только при выбранном контуре и наличии 2+ контуров
- [ ] При нажатии показывается диалог подтверждения с номером контура
- [ ] После подтверждения контур удаляется
- [ ] Оставшиеся контуры автоматически перенумеровываются (1, 2, 3...)
- [ ] UI консистентен с кнопками удаления коллекторов
- [ ] Код соответствует MVVM паттерну
- [ ] Все тесты проходят
- [ ] При переключении коллектора выбранный контур сбрасывается (SelectedCircuit = null)

---

## Риски и митигация

| Риск | Вероятность | Влияние | Митигация |
|------|-------------|---------|-----------|
| SelectedCircuit не обновляется при переключении коллекторов | Средняя | Среднее | **Реализовано в Задаче 1:** Сбрасывать SelectedCircuit в null при изменении SelectedCollectorIndex (через partial-метод `OnSelectedCollectorIndexChanged`) |
| Кнопка остаётся активной после удаления | Низкая | Низкое | Вызвать `RemoveCircuitCommand.NotifyCanExecuteChanged()` после удаления |
| Диалог подтверждения не показывается | Низкая | Высокое | Проверить работу `ConfirmDeleteCircuit()` в существующем коде |

---

## Примечания

1. **Существующий код:** Методы `RemoveCircuit`, `CanRemoveCircuit`, `RenumberCircuits`, `ConfirmDeleteCircuit` уже реализованы в ViewModel. Задачи 1-3 только добавляют UI-привязки.

2. **Консистентность:** Реализация следует тому же паттерну, что и удаление коллекторов (кнопка "- Удалить коллектор" под таблицей коллекторов).

3. **Автоматический пересчёт:** После удаления контура не требуется вызывать `Calculate()` — пользователь нажимает "Рассчитать" вручную. Это соответствует текущему поведению приложения.
   - **Примечание:** В ТЗ рекомендуется "автоматически пересчитывать для удобства пользователя", но в плане реализован ручной пересчёт. Это решение обосновано:
     - Соответствует существующему поведению метода `RemoveCircuit` (не вызывает `Calculate()`)
     - Позволяет пользователю удалить несколько контуров перед пересчётом
     - Снижает нагрузку на UI при частых операциях удаления

4. **Выбор варианта реализации (Вариант 1 vs Вариант 2):**
   - **ТЗ предлагает два варианта:**
     - Вариант 1: Кнопка "- Удалить контур" под таблицей (выбран)
     - Вариант 2: Кнопка "X" в каждой строке DataGrid
   - **Выбран Вариант 1 по причинам:**
     - Консистентность с существующим UI (кнопки удаления коллекторов)
     - Меньше визуального шума в таблице (уже 15+ столбцов)
     - Стандартный паттерн для CRUD-операций в WPF
     - Проще поддержка (одна кнопка vs кнопка в каждой строке)

5. **Митигация риска переключения коллекторов:**
   - Риск: Пользователь выбирает контур в коллекторе 1, переключается на коллектор 2, и случайно удаляет контур из коллектора 1 (если SelectedCircuit не сброшен)
   - Митигация: При изменении `SelectedCollectorIndex` свойство `SelectedCircuit` сбрасывается в `null`
   - Реализация: Partial-метод `OnSelectedCollectorIndexChanged` в Задаче 1

---

*План создан: 2026-03-21*
*Модуль: Hydraulics*
*Задача: Механизм удаления контура из коллектора*