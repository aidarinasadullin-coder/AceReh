# Task 7.3: Тесты CircuitsViewModel

**Этап:** 7 - Тестирование  
**Приоритет:** Средний  
**Статус:** К разработке  
**Зависимости:** Task 4.1 (CircuitsViewModel)

---

## 1. Цель задачи

Создать unit-тесты для `CircuitsViewModel`.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-01 | Ввод параметров контуров | TestAddCircuit, TestRemoveCircuit |
| UC-08 | Управление контурами и коллекторами | TestAddCollector, TestRemoveCollector |

---

## 3. Создаваемые файлы

### 3.1. CircuitsViewModelTests.cs

**Путь:** `tests/ViewModels/Hydraulics/CircuitsViewModelTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using NUnit.Framework;
using Moq;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Tests.ViewModels.Hydraulics
{
    [TestFixture]
    public class CircuitsViewModelTests
    {
        private Mock<ICircuitsCalculator> _calculatorMock;
        private CircuitsViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _calculatorMock = new Mock<ICircuitsCalculator>();
            _viewModel = new CircuitsViewModel(_calculatorMock.Object);
        }

        #region Initial State Tests

        [Test]
        public void Constructor_InitialState_HasOneCollectorWithFourCircuits()
        {
            // Assert
            Assert.That(_viewModel.Collectors, Is.Not.Null);
            Assert.That(_viewModel.Collectors.Count, Is.EqualTo(1));
            Assert.That(_viewModel.Collectors[0].Circuits.Count, Is.EqualTo(4));
        }

        [Test]
        public void Constructor_SelectedCollectorIndex_IsZero()
        {
            // Assert
            Assert.That(_viewModel.SelectedCollectorIndex, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_CanAddCollector_IsTrue()
        {
            // Assert
            Assert.That(_viewModel.CanAddCollector, Is.True);
        }

        [Test]
        public void Constructor_CanAddCircuit_IsTrue()
        {
            // Assert
            Assert.That(_viewModel.CanAddCircuit, Is.True);
        }

        #endregion

        #region AddCollector Tests

        [Test]
        public void AddCollector_IncreasesCollectorCount()
        {
            // Arrange
            int initialCount = _viewModel.Collectors.Count;

            // Act
            _viewModel.AddCollectorCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Collectors.Count, Is.EqualTo(initialCount + 1));
        }

        [Test]
        public void AddCollector_NewCollectorHasFourCircuits()
        {
            // Act
            _viewModel.AddCollectorCommand.Execute(null);

            // Assert
            var newCollector = _viewModel.Collectors[_viewModel.Collectors.Count - 1];
            Assert.That(newCollector.Circuits.Count, Is.EqualTo(4));
        }

        [Test]
        public void AddCollector_MaximumReached_CanAddCollectorIsFalse()
        {
            // Arrange - добавить 3 коллектора (итого 4)
            for (int i = 0; i < 3; i++)
            {
                _viewModel.AddCollectorCommand.Execute(null);
            }

            // Assert
            Assert.That(_viewModel.Collectors.Count, Is.EqualTo(4));
            Assert.That(_viewModel.CanAddCollector, Is.False);
        }

        [Test]
        public void AddCollector_AtMaximum_CommandCannotExecute()
        {
            // Arrange - добавить 3 коллектора (итого 4)
            for (int i = 0; i < 3; i++)
            {
                _viewModel.AddCollectorCommand.Execute(null);
            }

            // Assert
            Assert.That(_viewModel.AddCollectorCommand.CanExecute(null), Is.False);
        }

        #endregion

        #region RemoveCollector Tests

        [Test]
        public void RemoveCollector_DecreasesCollectorCount()
        {
            // Arrange
            _viewModel.AddCollectorCommand.Execute(null);
            int countAfterAdd = _viewModel.Collectors.Count;

            // Act
            _viewModel.RemoveCollectorCommand.Execute(_viewModel.Collectors.Count - 1);

            // Assert
            Assert.That(_viewModel.Collectors.Count, Is.EqualTo(countAfterAdd - 1));
        }

        [Test]
        public void RemoveCollector_LastCollector_CannotRemove()
        {
            // Assert
            Assert.That(_viewModel.RemoveCollectorCommand.CanExecute(0), Is.False);
        }

        [Test]
        public void RemoveCollector_UpdatesSelectedIndex()
        {
            // Arrange
            _viewModel.AddCollectorCommand.Execute(null);
            _viewModel.AddCollectorCommand.Execute(null);
            _viewModel.SelectedCollectorIndex = 2;

            // Act
            _viewModel.RemoveCollectorCommand.Execute(2);

            // Assert
            Assert.That(_viewModel.SelectedCollectorIndex, Is.LessThan(_viewModel.Collectors.Count));
        }

        #endregion

        #region AddCircuit Tests

        [Test]
        public void AddCircuit_IncreasesCircuitCount()
        {
            // Arrange
            int initialCount = _viewModel.Collectors[0].Circuits.Count;

            // Act
            _viewModel.AddCircuitCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Collectors[0].Circuits.Count, Is.EqualTo(initialCount + 1));
        }

        [Test]
        public void AddCircuit_MaximumReached_CanAddCircuitIsFalse()
        {
            // Arrange - добавить 8 контуров (итого 12)
            for (int i = 0; i < 8; i++)
            {
                _viewModel.AddCircuitCommand.Execute(null);
            }

            // Assert
            Assert.That(_viewModel.Collectors[0].Circuits.Count, Is.EqualTo(12));
            Assert.That(_viewModel.CanAddCircuit, Is.False);
        }

        [Test]
        public void AddCircuit_AtMaximum_CommandCannotExecute()
        {
            // Arrange - добавить 8 контуров (итого 12)
            for (int i = 0; i < 8; i++)
            {
                _viewModel.AddCircuitCommand.Execute(null);
            }

            // Assert
            Assert.That(_viewModel.AddCircuitCommand.CanExecute(null), Is.False);
        }

        #endregion

        #region RemoveCircuit Tests

        [Test]
        public void RemoveCircuit_DecreasesCircuitCount()
        {
            // Arrange
            _viewModel.AddCircuitCommand.Execute(null);
            int countAfterAdd = _viewModel.Collectors[0].Circuits.Count;

            // Act
            _viewModel.RemoveCircuitCommand.Execute(0);

            // Assert
            Assert.That(_viewModel.Collectors[0].Circuits.Count, Is.EqualTo(countAfterAdd - 1));
        }

        [Test]
        public void RemoveCircuit_MinimumReached_CannotRemove()
        {
            // Arrange - оставить 1 контур
            while (_viewModel.Collectors[0].Circuits.Count > 1)
            {
                _viewModel.RemoveCircuitCommand.Execute(0);
            }

            // Assert
            Assert.That(_viewModel.RemoveCircuitCommand.CanExecute(0), Is.False);
        }

        #endregion

        #region CalculateCommand Tests

        [Test]
        public void CalculateCommand_CallsCalculator()
        {
            // Arrange
            _viewModel.GlycolType = GlycolType.Ethylene;
            _viewModel.GlycolConcentration = 50;

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            _calculatorMock.Verify(c => c.CalculateAllCircuits(
                It.IsAny<List<CircuitRow>>(),
                It.IsAny<HydraulicInputData>()), Times.Once);
        }

        [Test]
        public void CalculateCommand_UpdatesResults()
        {
            // Arrange
            var mockResult = new List<CircuitRow>
            {
                new CircuitRow { CircuitLength = 100, Power = 5000, OperatingResult = new CircuitTemperatureResult() }
            };
            _calculatorMock
                .Setup(c => c.CalculateAllCircuits(It.IsAny<List<CircuitRow>>(), It.IsAny<HydraulicInputData>()))
                .Returns(mockResult);

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Collectors[0].Circuits[0].OperatingResult, Is.Not.Null);
        }

        #endregion

        #region SwitchModeCommand Tests

        [Test]
        public void SwitchModeCommand_ChangesMode()
        {
            // Arrange
            var initialMode = _viewModel.CurrentMode;

            // Act
            _viewModel.SwitchModeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.CurrentMode, Is.Not.EqualTo(initialMode));
        }

        [Test]
        public void SwitchModeCommand_TogglesBetweenOperatingAndDesign()
        {
            // Arrange
            _viewModel.CurrentMode = HydraulicMode.OperatingTemperature;

            // Act
            _viewModel.SwitchModeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.CurrentMode, Is.EqualTo(HydraulicMode.DesignTemperature));

            // Act again
            _viewModel.SwitchModeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.CurrentMode, Is.EqualTo(HydraulicMode.OperatingTemperature));
        }

        #endregion

        #region PropertyChanged Tests

        [Test]
        public void AddCollector_RaisesPropertyChanged()
        {
            // Arrange
            var propertyChangedRaised = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.Collectors))
                    propertyChangedRaised = true;
            };

            // Act
            _viewModel.AddCollectorCommand.Execute(null);

            // Assert
            Assert.That(propertyChangedRaised, Is.True);
        }

        [Test]
        public void SwitchMode_RaisesPropertyChanged()
        {
            // Arrange
            var propertyChangedRaised = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.CurrentMode))
                    propertyChangedRaised = true;
            };

            // Act
            _viewModel.SwitchModeCommand.Execute(null);

            // Assert
            Assert.That(propertyChangedRaised, Is.True);
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

| Тест | Описание | Ожидаемый результат |
|------|----------|---------------------|
| Constructor_InitialState | Начальное состояние | 1 коллектор, 4 контура |
| AddCollector | Добавление коллектора | Увеличение на 1 |
| RemoveCollector | Удаление коллектора | Уменьшение на 1 |
| AddCircuit | Добавление контура | Увеличение на 1 |
| RemoveCircuit | Удаление контура | Уменьшение на 1 |
| CalculateCommand | Команда расчёта | Вызов калькулятора |
| SwitchModeCommand | Переключение режима | Operating ↔ Design |

---

## 5. Критерии приёмки

- [ ] Файл тестов создан
- [ ] Все тесты проходят
- [ ] Покрытие кода > 80%
- [ ] Тесты для всех команд
- [ ] Тесты для PropertyChanged

---

## 6. Связанные задачи

- Task 4.1: CircuitsViewModel — тестируемый класс
- Task 4.2: CollectorViewModel — связанная ViewModel

---

*Дата создания: 2026-03-17*