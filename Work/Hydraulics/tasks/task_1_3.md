# Задача 1.3: Добавить диалоговые окна подтверждения

## 1. Метаданные

| Параметр | Значение |
|----------|----------|
| **ID** | task_1_3 |
| **Приоритет** | P1 (Критично) |
| **Этап** | 1 |
| **Зависимости** | Нет |
| **Юзер-кейсы** | UC-2, UC-4 |
| **Оценка времени** | 0.5 часа |

---

## 2. Цель задачи

Добавить диалоговые окна подтверждения при удалении контура и коллектора для защиты пользователя от случайного удаления данных.

---

## 3. Описание проблемы

### 3.1. Симптом
При нажатии кнопки "- Удалить контур" или "- Удалить коллектор" удаление происходит без подтверждения.

### 3.2. Требование
Диалоговое окно подтверждения должно отображаться при любой попытке удаления.

---

## 4. Изменения

### 4.1. Файл: `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

#### 4.1.1. Добавить метод ConfirmDeleteCircuit

**Место**: После метода `RenumberCircuits` (строка 364)

```csharp
/// <summary>
/// Диалоговое окно подтверждения удаления контура
/// </summary>
/// <param name="circuitNumber">Номер контура</param>
/// <returns>true — удалить, false — отменить</returns>
private bool ConfirmDeleteCircuit(int circuitNumber)
{
    var result = MessageBox.Show(
        $"Вы уверены, что хотите удалить контур №{circuitNumber}?",
        "Удаление контура",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning
    );
    return result == MessageBoxResult.Yes;
}
```

#### 4.1.2. Добавить метод ConfirmDeleteCollector

**Место**: После метода `ConfirmDeleteCircuit`

```csharp
/// <summary>
/// Диалоговое окно подтверждения удаления коллектора
/// </summary>
/// <param name="collectorNumber">Номер коллектора</param>
/// <returns>true — удалить, false — отменить</returns>
private bool ConfirmDeleteCollector(int collectorNumber)
{
    var result = MessageBox.Show(
        $"Вы уверены, что хотите удалить коллектор №{collectorNumber}?\nВсе контуры этого коллектора будут удалены.",
        "Удаление коллектора",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning
    );
    return result == MessageBoxResult.Yes;
}
```

#### 4.1.3. Добавить using для MessageBox

**Место**: В начале файла (после строки 6)

```csharp
using System.Windows;
```

#### 4.1.4. Обновить метод RemoveCircuit

**Текущий код** (строки 219-231):
```csharp
[RelayCommand]
private void RemoveCircuit(CircuitRow circuit)
{
    var collector = SelectedCollector;
    if (collector == null) return;

    if (circuit != null && collector.Circuits.Contains(circuit))
    {
        collector.Circuits.Remove(circuit);
        RenumberCircuits(collector);
        AddCircuitCommand.NotifyCanExecuteChanged();
    }
}
```

**Требуемый код**:
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

#### 4.1.5. Обновить метод RemoveCollector

**Текущий код** (строки 185-198):
```csharp
[RelayCommand]
private void RemoveCollector(CollectorData collector)
{
    if (collector != null && Collectors.Contains(collector))
    {
        Collectors.Remove(collector);
        if (SelectedCollectorIndex >= Collectors.Count)
        {
            SelectedCollectorIndex = Math.Max(0, Collectors.Count - 1);
        }
        AddCollectorCommand.NotifyCanExecuteChanged();
        AddCircuitCommand.NotifyCanExecuteChanged();
    }
}
```

**Требуемый код**:
```csharp
[RelayCommand(CanExecute = nameof(CanRemoveCollector))]
private void RemoveCollector(CollectorData collector)
{
    if (collector == null)
        return;
    
    if (!ConfirmDeleteCollector(collector.CollectorNumber))
        return;
    
    if (Collectors.Contains(collector))
    {
        Collectors.Remove(collector);
        RenumberCollectors();  // <-- Добавить вызов (Задача 2.2)
        if (SelectedCollectorIndex >= Collectors.Count)
        {
            SelectedCollectorIndex = Math.Max(0, Collectors.Count - 1);
        }
        AddCollectorCommand.NotifyCanExecuteChanged();
        AddCircuitCommand.NotifyCanExecuteChanged();
        RemoveCollectorCommand.NotifyCanExecuteChanged();
    }
}
```

#### 4.1.6. Добавить метод CanRemoveCircuit

**Место**: После метода `ConfirmDeleteCollector`

```csharp
/// <summary>
/// Проверка возможности удаления контура
/// </summary>
/// <param name="circuit">Контур для удаления</param>
/// <returns>true — можно удалить, false — нельзя</returns>
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

#### 4.1.7. Добавить метод CanRemoveCollector

**Место**: После метода `CanRemoveCircuit`

```csharp
/// <summary>
/// Проверка возможности удаления коллектора
/// </summary>
/// <param name="collector">Коллектор для удаления</param>
/// <returns>true — можно удалить, false — нельзя</returns>
private bool CanRemoveCollector(CollectorData collector)
{
    // Нельзя удалить, если:
    // 1. Коллектор не выбран (collector == null)
    // 2. В системе только 1 коллектор (минимум 1 коллектор должен остаться)
    if (collector == null)
        return false;
    
    return Collectors.Count > 1;
}
```

---

## 5. Тест-кейсы

### TC-1.3.1: Подтверждение удаления контура
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- В коллекторе 3 контура

**Шаги**:
1. Выбрать контур №2
2. Нажать кнопку "- Удалить контур"

**Ожидаемый результат**:
- Отображается диалоговое окно "Удаление контура"
- Текст: "Вы уверены, что хотите удалить контур №2?"
- Кнопки: "Да" / "Нет"

### TC-1.3.2: Отмена удаления контура
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- В коллекторе 3 контура

**Шаги**:
1. Выбрать контур №2
2. Нажать кнопку "- Удалить контур"
3. В диалоговом окне нажать "Нет"

**Ожидаемый результат**:
- Контур НЕ удалён
- Контур №2 остался в списке

### TC-1.3.3: Подтверждение удаления коллектора
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- 2 коллектора

**Шаги**:
1. Выбрать коллектор №2
2. Нажать кнопку "- Удалить коллектор"

**Ожидаемый результат**:
- Отображается диалоговое окно "Удаление коллектора"
- Текст: "Вы уверены, что хотите удалить коллектор №2?\nВсе контуры этого коллектора будут удалены."
- Кнопки: "Да" / "Нет"

### TC-1.3.4: Блокировка кнопки удаления контура
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- В коллекторе 1 контур

**Шаги**:
1. Проверить состояние кнопки "- Удалить контур"

**Ожидаемый результат**:
- Кнопка заблокирована (IsEnabled=False)

### TC-1.3.5: Блокировка кнопки удаления коллектора
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- 1 коллектор

**Шаги**:
1. Проверить состояние кнопки "- Удалить коллектор"

**Ожидаемый результат**:
- Кнопка заблокирована (IsEnabled=False)

---

## 6. Критерии приёмки

- [ ] Диалоговое окно отображается при удалении контура
- [ ] Диалоговое окно отображается при удалении коллектора
- [ ] Текст диалога содержит номер контура/коллектора
- [ ] При нажатии "Нет" контур/коллектор не удаляется
- [ ] При нажатии "Да" контур/коллектор удаляется
- [ ] Кнопка удаления контура заблокирована при 1 контуре
- [ ] Кнопка удаления коллектора заблокирована при 1 коллекторе
- [ ] Существующий функционал не нарушен

---

## 7. Примечания

### 7.1. Решение пользователя
**Предупреждение всегда** — диалоговое окно с подтверждением отображается при любой попытке удаления.

### 7.2. Использование MessageBox
Использовать `MessageBox.Show()` из пространства имён `System.Windows`:
- `MessageBoxButton.YesNo` — кнопки "Да" / "Нет"
- `MessageBoxImage.Warning` — иконка предупреждения

### 7.3. Связь с Задачей 2.2
Метод `RemoveCollector` должен вызывать `RenumberCollectors()` после удаления (Задача 2.2).

---

## 8. Ссылки

- **ТЗ**: `Work/Hydraulics/technical_specification.md`, раздел 4.5
- **Файл**: `src/ViewModels/Hydraulics/CircuitsViewModel.cs`
- **Юзер-кейс**: UC-2, UC-4