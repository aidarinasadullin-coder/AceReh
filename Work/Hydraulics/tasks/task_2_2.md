# Задача 2.2: Вызвать RenumberCollectors() в RemoveCollector

## 1. Метаданные

| Параметр | Значение |
|----------|----------|
| **ID** | task_2_2 |
| **Приоритет** | P2 (Важно) |
| **Этап** | 2 |
| **Зависимости** | Task 1.3, Task 2.1 |
| **Юзер-кейсы** | UC-4 |
| **Оценка времени** | 0.25 часа |

---

## 2. Цель задачи

Вызвать метод `RenumberCollectors()` в методе `RemoveCollector()` после удаления коллектора.

---

## 3. Описание проблемы

### 3.1. Симптом
При удалении коллектора номера не перенумеровываются.

### 3.2. Корневая причина
Метод `RemoveCollector()` не вызывает `RenumberCollectors()` после удаления.

---

## 4. Изменения

### 4.1. Файл: `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

#### Обновить метод RemoveCollector

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
        RenumberCollectors();  // <-- ДОБАВИТЬ ВЫЗОВ
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

#### Примечание
Методы `ConfirmDeleteCollector()` и `CanRemoveCollector()` добавляются в Задаче 1.3.

---

## 5. Тест-кейсы

### TC-2.2.1: Удаление коллектора с перенумерацией
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- 3 коллектора (№1, №2, №3)

**Шаги**:
1. Выбрать коллектор №2
2. Нажать "- Удалить коллектор"
3. Подтвердить удаление
4. Проверить номера оставшихся коллекторов

**Ожидаемый результат**:
- Коллектор №2 удалён
- Коллекторы перенумерованы: №1, №2 (бывший №3)
- TabControl отображает вкладки "Коллектор №1" и "Коллектор №2"

### TC-2.2.2: Удаление первого коллектора
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- 3 коллектора (№1, №2, №3)

**Шаги**:
1. Выбрать коллектор №1
2. Нажать "- Удалить коллектор"
3. Подтвердить удаление

**Ожидаемый результат**:
- Коллектор №1 удалён
- Коллекторы перенумерованы: №1, №2 (бывшие №2, №3)
- Активна вкладка "Коллектор №1"

### TC-2.2.3: Удаление последнего коллектора
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- 3 коллектора (№1, №2, №3)

**Шаги**:
1. Выбрать коллектор №3
2. Нажать "- Удалить коллектор"
3. Подтвердить удаление

**Ожидаемый результат**:
- Коллектор №3 удалён
- Коллекторы перенумерованы: №1, №2
- Активна вкладка "Коллектор №2"

### TC-2.2.4: Отмена удаления коллектора
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- 3 коллектора (№1, №2, №3)

**Шаги**:
1. Выбрать коллектор №2
2. Нажать "- Удалить коллектор"
3. Нажать "Нет" в диалоговом окне

**Ожидаемый результат**:
- Коллектор НЕ удалён
- Коллекторы остались: №1, №2, №3
- Номера не изменились

---

## 6. Критерии приёмки

- [ ] Метод `RemoveCollector()` вызывает `RenumberCollectors()`
- [ ] После удаления коллектора номера перенумеровываются
- [ ] UI отображает корректные номера коллекторов
- [ ] При отмене удаления коллектор не удаляется
- [ ] Диалоговое окно подтверждения отображается
- [ ] Существующий функционал не нарушен

---

## 7. Примечания

### 7.1. Зависимости
- **Task 1.3**: Методы `ConfirmDeleteCollector()` и `CanRemoveCollector()`
- **Task 2.1**: Метод `RenumberCollectors()`

### 7.2. Порядок выполнения
1. Выполнить Task 1.3 (диалоговые окна)
2. Выполнить Task 2.1 (метод RenumberCollectors)
3. Выполнить Task 2.2 (вызов RenumberCollectors в RemoveCollector)

---

## 8. Ссылки

- **ТЗ**: `Work/Hydraulics/technical_specification.md`, раздел 4.3
- **Файл**: `src/ViewModels/Hydraulics/CircuitsViewModel.cs`
- **Юзер-кейс**: UC-4