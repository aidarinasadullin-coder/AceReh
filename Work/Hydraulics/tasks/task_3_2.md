# Задача 3.2: Добавить Unit-тесты

## 1. Метаданные

| Параметр | Значение |
|----------|----------|
| **ID** | task_3_2 |
| **Приоритет** | P3 (Рекомендуется) |
| **Этап** | 3 |
| **Зависимости** | Task 1.1, Task 2.1 |
| **Юзер-кейсы** | Нет (техническая задача) |
| **Оценка времени** | 1.0 час |

---

## 2. Цель задачи

Добавить Unit-тесты для методов `RenumberCircuits()` и `RenumberCollectors()` для обеспечения качества кода.

---

## 3. Описание

### 3.1. Область тестирования
- Метод `RenumberCircuits(CollectorData collector)` — перенумерация контуров
- Метод `RenumberCollectors()` — перенумерация коллекторов
- Методы `RemoveCircuit()` и `RemoveCollector()` — удаление с перенумерацией

### 3.2. Инструменты
- **Фреймворк**: xUnit или NUnit
- **Мок-фреймворк**: Moq (если требуется)
- **Проект тестов**: `tests/ViewModels/Hydraulics/CircuitsViewModelTests.cs`

---

## 4. Изменения

### 4.1. Создать файл тестов

**Файл**: `tests/ViewModels/Hydraulics/CircuitsViewModelTests.cs`

**Структура файла**:
```csharp
using System;
using System.Collections.ObjectModel;
using Xunit;
using Moq;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.ViewModels.Hydraulics
{
    public class CircuitsViewModelTests
    {
        // Тесты для RenumberCircuits
        // Тесты для RenumberCollectors
        // Тесты для RemoveCircuit
        // Тесты для RemoveCollector
    }
}
```

---

## 5. Тест-кейсы

### TC-3.2.1: RenumberCircuits — базовый сценарий
**Метод**: `RenumberCircuits_CorrectlyRenumbersCircuits`

**Предусловия**:
- Коллектор с 5 контурами (номера: 1, 2, 3, 4, 5)

**Шаги**:
1. Удалить контур №3
2. Вызвать `RenumberCircuits(collector)`

**Ожидаемый результат**:
- Контур 1: `CircuitNumber == 1`
- Контур 2: `CircuitNumber == 2`
- Контур 3: `CircuitNumber == 3` (бывший №4)
- Контур 4: `CircuitNumber == 4` (бывший №5)

```csharp
[Fact]
public void RenumberCircuits_CorrectlyRenumbersCircuits()
{
    // Arrange
    var collector = new CollectorData(1);
    for (int i = 1; i <= 5; i++)
    {
        collector.Circuits.Add(new CircuitRow { CircuitNumber = i });
    }
    
    // Удалить контур №3
    collector.Circuits.RemoveAt(2);
    
    // Act
    // RenumberCircuits вызывается через RemoveCircuit
    
    // Assert
    Assert.Equal(1, collector.Circuits[0].CircuitNumber);
    Assert.Equal(2, collector.Circuits[1].CircuitNumber);
    Assert.Equal(3, collector.Circuits[2].CircuitNumber);
    Assert.Equal(4, collector.Circuits[3].CircuitNumber);
}
```

### TC-3.2.2: RenumberCircuits — пустой коллектор
**Метод**: `RenumberCircuits_EmptyCollector_NoException`

**Предусловия**:
- Коллектор без контуров

**Шаги**:
1. Вызвать `RenumberCircuits(collector)`

**Ожидаемый результат**:
- Нет исключений
- Коллектор остаётся пустым

```csharp
[Fact]
public void RenumberCircuits_EmptyCollector_NoException()
{
    // Arrange
    var collector = new CollectorData(1);
    
    // Act & Assert
    // Не должно быть исключений
    // (метод вызывается через RemoveCircuit)
}
```

### TC-3.2.3: RenumberCollectors — базовый сценарий
**Метод**: `RenumberCollectors_CorrectlyRenumbersCollectors`

**Предусловия**:
- 3 коллектора (номера: 1, 2, 3)

**Шаги**:
1. Удалить коллектор №2
2. Вызвать `RenumberCollectors()`

**Ожидаемый результат**:
- Коллектор 1: `CollectorNumber == 1`
- Коллектор 2: `CollectorNumber == 2` (бывший №3)

```csharp
[Fact]
public void RenumberCollectors_CorrectlyRenumbersCollectors()
{
    // Arrange
    var viewModel = CreateViewModel();
    viewModel.AddCollector(); // Коллектор №2
    viewModel.AddCollector(); // Коллектор №3
    
    // Удалить коллектор №2
    var collectorToRemove = viewModel.Collectors[1];
    viewModel.Collectors.Remove(collectorToRemove);
    
    // Act
    // RenumberCollectors вызывается через RemoveCollector
    
    // Assert
    Assert.Equal(1, viewModel.Collectors[0].CollectorNumber);
    Assert.Equal(2, viewModel.Collectors[1].CollectorNumber);
}
```

### TC-3.2.4: RemoveCircuit — удаление с перенумерацией
**Метод**: `RemoveCircuit_RenumbersRemainingCircuits`

**Предусловия**:
- Коллектор с 5 контурами

**Шаги**:
1. Удалить контур №3
2. Проверить номера оставшихся контуров

**Ожидаемый результат**:
- Контур 1: `CircuitNumber == 1`
- Контур 2: `CircuitNumber == 2`
- Контур 3: `CircuitNumber == 3` (бывший №4)
- Контур 4: `CircuitNumber == 4` (бывший №5)

```csharp
[Fact]
public void RemoveCircuit_RenumbersRemainingCircuits()
{
    // Arrange
    var viewModel = CreateViewModel();
    var collector = viewModel.Collectors[0];
    
    // Добавить контуры до 5
    for (int i = 0; i < 3; i++)
    {
        viewModel.AddCircuit();
    }
    
    // Удалить контур №3 (индекс 2)
    var circuitToRemove = collector.Circuits[2];
    
    // Act
    viewModel.RemoveCircuitCommand.Execute(circuitToRemove);
    
    // Assert
    Assert.Equal(4, collector.Circuits.Count);
    Assert.Equal(1, collector.Circuits[0].CircuitNumber);
    Assert.Equal(2, collector.Circuits[1].CircuitNumber);
    Assert.Equal(3, collector.Circuits[2].CircuitNumber);
    Assert.Equal(4, collector.Circuits[3].CircuitNumber);
}
```

### TC-3.2.5: RemoveCollector — удаление с перенумерацией
**Метод**: `RemoveCollector_RenumbersRemainingCollectors`

**Предусловия**:
- 3 коллектора

**Шаги**:
1. Удалить коллектор №2
2. Проверить номера оставшихся коллекторов

**Ожидаемый результат**:
- Коллектор 1: `CollectorNumber == 1`
- Коллектор 2: `CollectorNumber == 2` (бывший №3)

```csharp
[Fact]
public void RemoveCollector_RenumbersRemainingCollectors()
{
    // Arrange
    var viewModel = CreateViewModel();
    viewModel.AddCollector(); // Коллектор №2
    viewModel.AddCollector(); // Коллектор №3
    
    // Удалить коллектор №2
    var collectorToRemove = viewModel.Collectors[1];
    
    // Act
    viewModel.RemoveCollectorCommand.Execute(collectorToRemove);
    
    // Assert
    Assert.Equal(2, viewModel.Collectors.Count);
    Assert.Equal(1, viewModel.Collectors[0].CollectorNumber);
    Assert.Equal(2, viewModel.Collectors[1].CollectorNumber);
}
```

### TC-3.2.6: CanRemoveCircuit — блокировка при одном контуре
**Метод**: `CanRemoveCircuit_ReturnsFalse_WhenOneCircuit`

**Предусловия**:
- Коллектор с 1 контуром

**Шаги**:
1. Проверить `CanRemoveCircuit`

**Ожидаемый результат**:
- `CanRemoveCircuit == false`

```csharp
[Fact]
public void CanRemoveCircuit_ReturnsFalse_WhenOneCircuit()
{
    // Arrange
    var viewModel = CreateViewModel();
    var collector = viewModel.Collectors[0];
    
    // Удалить все контуры кроме одного
    while (collector.Circuits.Count > 1)
    {
        collector.Circuits.RemoveAt(0);
    }
    
    // Act
    var canRemove = viewModel.RemoveCircuitCommand.CanExecute(collector.Circuits[0]);
    
    // Assert
    Assert.False(canRemove);
}
```

### TC-3.2.7: CanRemoveCollector — блокировка при одном коллекторе
**Метод**: `CanRemoveCollector_ReturnsFalse_WhenOneCollector`

**Предусловия**:
- 1 коллектор

**Шаги**:
1. Проверить `CanRemoveCollector`

**Ожидаемый результат**:
- `CanRemoveCollector == false`

```csharp
[Fact]
public void CanRemoveCollector_ReturnsFalse_WhenOneCollector()
{
    // Arrange
    var viewModel = CreateViewModel();
    // По умолчанию 1 коллектор
    
    // Act
    var canRemove = viewModel.RemoveCollectorCommand.CanExecute(viewModel.Collectors[0]);
    
    // Assert
    Assert.False(canRemove);
}
```

---

## 6. Критерии приёмки

- [ ] Создан файл `tests/ViewModels/Hydraulics/CircuitsViewModelTests.cs`
- [ ] Тест `RenumberCircuits_CorrectlyRenumbersCircuits` проходит
- [ ] Тест `RenumberCircuits_EmptyCollector_NoException` проходит
- [ ] Тест `RenumberCollectors_CorrectlyRenumbersCollectors` проходит
- [ ] Тест `RemoveCircuit_RenumbersRemainingCircuits` проходит
- [ ] Тест `RemoveCollector_RenumbersRemainingCollectors` проходит
- [ ] Тест `CanRemoveCircuit_ReturnsFalse_WhenOneCircuit` проходит
- [ ] Тест `CanRemoveCollector_ReturnsFalse_WhenOneCollector` проходит
- [ ] Все тесты выполняются без ошибок

---

## 7. Примечания

### 7.1. Вспомогательный метод CreateViewModel
```csharp
private CircuitsViewModel CreateViewModel()
{
    var circuitsCalculatorMock = new Mock<ICircuitsCalculator>();
    var glycolServiceMock = new Mock<IGlycolDataService>();
    var thermalViewModelMock = new Mock<ThermalViewModel>();
    var climateViewModelMock = new Mock<ClimateViewModel>();
    
    // Настройка моков...
    
    return new CircuitsViewModel(
        circuitsCalculatorMock.Object,
        glycolServiceMock.Object,
        thermalViewModelMock.Object,
        climateViewModelMock.Object
    );
}
```

### 7.2. Зависимости
- **Task 1.1**: `CircuitNumber` должен быть `ObservableProperty`
- **Task 2.1**: Метод `RenumberCollectors()` должен существовать

---

## 8. Ссылки

- **ТЗ**: `Work/Hydraulics/technical_specification.md`, раздел 5.3
- **Файл**: `tests/ViewModels/Hydraulics/CircuitsViewModelTests.cs`
- **ViewModel**: `src/ViewModels/Hydraulics/CircuitsViewModel.cs`