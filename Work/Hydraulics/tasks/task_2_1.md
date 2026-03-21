# Задача 2.1: Добавить RenumberCollectors()

## 1. Метаданные

| Параметр | Значение |
|----------|----------|
| **ID** | task_2_1 |
| **Приоритет** | P2 (Важно) |
| **Этап** | 2 |
| **Зависимости** | Task 1.1 (CircuitNumber должен быть ObservableProperty) |
| **Юзер-кейсы** | UC-4 |
| **Оценка времени** | 0.25 часа |

---

## 2. Цель задачи

Добавить метод `RenumberCollectors()` для перенумерации коллекторов после удаления одного из них.

---

## 3. Описание проблемы

### 3.1. Симптом
При удалении коллектора №2 из трёх, остаются коллекторы №1 и №3 вместо №1 и №2.

### 3.2. Корневая причина
**Файл**: `src/ViewModels/Hydraulics/CircuitsViewModel.cs`, строки 186-198

```csharp
[RelayCommand]
private void RemoveCollector(CollectorData collector)
{
    if (collector != null && Collectors.Contains(collector))
    {
        Collectors.Remove(collector);
        // НЕТ перенумерации!
        if (SelectedCollectorIndex >= Collectors.Count)
        {
            SelectedCollectorIndex = Math.Max(0, Collectors.Count - 1);
        }
        ...
    }
}
```

Метод `RenumberCircuits()` есть для контуров (строки 358-364), но **НЕТ** метода `RenumberCollectors()` для коллекторов.

### 3.3. Примечание
Метод `RenumberCircuits` уже существует и корректно реализован:
```csharp
private void RenumberCircuits(CollectorData collector)
{
    for (int i = 0; i < collector.Circuits.Count; i++)
    {
        collector.Circuits[i].CircuitNumber = i + 1;
    }
}
```

---

## 4. Изменения

### 4.1. Файл: `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

#### Добавить метод RenumberCollectors

**Место**: После метода `RenumberCircuits` (строка 364)

```csharp
/// <summary>
/// Перенумерация коллекторов после удаления
/// </summary>
/// <remarks>
/// Вызывается после удаления коллектора для корректной нумерации.
/// Пример: при удалении коллектора №2 из [1,2,3] получаем [1,2].
/// </remarks>
private void RenumberCollectors()
{
    for (int i = 0; i < Collectors.Count; i++)
    {
        Collectors[i].CollectorNumber = i + 1;
    }
}
```

---

## 5. Тест-кейсы

### TC-2.1.1: Перенумерация после удаления коллектора
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- 3 коллектора (№1, №2, №3)

**Шаги**:
1. Удалить коллектор №2
2. Проверить номера оставшихся коллекторов

**Ожидаемый результат**:
- Коллекторы перенумерованы: №1, №2 (бывший №3)
- `Collectors[0].CollectorNumber == 1`
- `Collectors[1].CollectorNumber == 2`

### TC-2.1.2: Перенумерация после удаления первого коллектора
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- 3 коллектора (№1, №2, №3)

**Шаги**:
1. Удалить коллектор №1
2. Проверить номера оставшихся коллекторов

**Ожидаемый результат**:
- Коллекторы перенумерованы: №1, №2 (бывшие №2, №3)
- `Collectors[0].CollectorNumber == 1`
- `Collectors[1].CollectorNumber == 2`

### TC-2.1.3: Перенумерация после удаления последнего коллектора
**Предусловия**:
- Открыт экран "Гидравлический расчёт"
- 3 коллектора (№1, №2, №3)

**Шаги**:
1. Удалить коллектор №3
2. Проверить номера оставшихся коллекторов

**Ожидаемый результат**:
- Коллекторы перенумерованы: №1, №2
- `Collectors[0].CollectorNumber == 1`
- `Collectors[1].CollectorNumber == 2`

---

## 6. Критерии приёмки

- [ ] Метод `RenumberCollectors()` добавлен
- [ ] Метод корректно перенумеровывает коллекторы
- [ ] После удаления коллектора номера последовательны: 1, 2, 3, ...
- [ ] UI обновляется мгновенно (< 100 мс)
- [ ] Существующий функционал не нарушен

---

## 7. Примечания

### 7.1. Зависимость от Task 1.1
Метод `RenumberCollectors()` устанавливает `CollectorNumber`, который уже является `ObservableProperty` (класс `CollectorData` использует `ObservableObject`). Поэтому UI будет автоматически обновляться.

### 7.2. Связь с Task 2.2
Метод `RenumberCollectors()` должен вызываться в методе `RemoveCollector()` (Задача 2.2).

---

## 8. Ссылки

- **ТЗ**: `Work/Hydraulics/technical_specification.md`, раздел 4.3
- **Файл**: `src/ViewModels/Hydraulics/CircuitsViewModel.cs`
- **Юзер-кейс**: UC-4