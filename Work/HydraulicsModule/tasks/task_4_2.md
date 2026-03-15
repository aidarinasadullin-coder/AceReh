# Task 4.2: CircuitViewModel (ViewModel контура)

**Этап:** 4 - ViewModels  
**Приоритет:** Средний  
**Статус:** Не начато  
**Зависимости:** Task 4.1

---

## 1. Цель задачи

Создать ViewModel для отдельного контура (для поддержки нескольких контуров).

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-06 | Расчёт дросселирования контуров | Список контуров |

---

## 3. Создаваемые файлы

### 3.1. CircuitViewModel.cs

**Путь:** `src/ViewModels/Hydraulics/CircuitViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace SnowMeltingCalculator.ViewModels.Hydraulics
{
    /// <summary>
    /// ViewModel для отдельного контура системы снеготаяния
    /// </summary>
    public partial class CircuitViewModel : ObservableObject
    {
        #region Observable Properties

        /// <summary>
        /// Номер контура
        /// </summary>
        [ObservableProperty]
        private int _circuitNumber;

        /// <summary>
        /// Название контура
        /// </summary>
        [ObservableProperty]
        private string _circuitName;

        /// <summary>
        /// Длина контура (м)
        /// </summary>
        [ObservableProperty]
        private double _length;

        /// <summary>
        /// Длина подводки (м)
        /// </summary>
        [ObservableProperty]
        private double _supplyLength;

        /// <summary>
        /// Площадь контура (м²)
        /// </summary>
        [ObservableProperty]
        private double _area;

        /// <summary>
        /// Расход контура (л/ч)
        /// </summary>
        [ObservableProperty]
        private double _flowRate;

        /// <summary>
        /// Потери давления (Па)
        /// </summary>
        [ObservableProperty]
        private double _pressureLoss;

        /// <summary>
        /// Дросселирование (Па)
        /// </summary>
        [ObservableProperty]
        private double _throttling;

        /// <summary>
        /// Настройка вентиля (1-8)
        /// </summary>
        [ObservableProperty]
        private int _valveSetting = 1;

        /// <summary>
        /// Признак опорного контура
        /// </summary>
        [ObservableProperty]
        private bool _isReferenceCircuit;

        /// <summary>
        /// Скорость потока (м/с)
        /// </summary>
        [ObservableProperty]
        private double _velocity;

        /// <summary>
        /// Число Рейнольдса
        /// </summary>
        [ObservableProperty]
        private double _reynoldsNumber;

        /// <summary>
        /// Режим течения
        /// </summary>
        [ObservableProperty]
        private string _flowRegime;

        /// <summary>
        /// Признак валидности
        /// </summary>
        [ObservableProperty]
        private bool _isValid = true;

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        [ObservableProperty]
        private string _errorMessage;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Потери давления в кПа
        /// </summary>
        public double PressureLossKPa => PressureLoss / 1000;

        /// <summary>
        /// Потери давления в мбар
        /// </summary>
        public double PressureLossMbar => PressureLoss / 100;

        /// <summary>
        /// Дросселирование в кПа
        /// </summary>
        public double ThrottlingKPa => Throttling / 1000;

        /// <summary>
        /// Дросселирование в мбар
        /// </summary>
        public double ThrottlingMbar => Throttling / 100;

        /// <summary>
        /// Удельный расход на м² (л/ч/м²)
        /// </summary>
        public double SpecificFlowRate => Area > 0 ? FlowRate / Area : 0;

        /// <summary>
        /// Статус контура
        /// </summary>
        public string Status
        {
            get
            {
                if (!IsValid)
                    return $"Ошибка: {ErrorMessage}";

                if (IsReferenceCircuit)
                    return "Опорный контур";

                if (Throttling > 0)
                    return $"Дросселирование: {ThrottlingMbar:F1} мбар";

                return "Готов";
            }
        }

        /// <summary>
        /// Цвет статуса для UI
        /// </summary>
        public string StatusColor
        {
            get
            {
                if (!IsValid)
                    return "Red";

                if (IsReferenceCircuit)
                    return "Green";

                if (Throttling > 0)
                    return "Orange";

                return "Gray";
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public CircuitViewModel()
        {
            CircuitName = "Новый контур";
        }

        /// <summary>
        /// Конструктор с параметрами
        /// </summary>
        public CircuitViewModel(int number, string name, double length, double supplyLength, double area)
        {
            CircuitNumber = number;
            CircuitName = name;
            Length = length;
            SupplyLength = supplyLength;
            Area = area;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Сбросить значения
        /// </summary>
        public void Reset()
        {
            Length = 0;
            SupplyLength = 0;
            Area = 0;
            FlowRate = 0;
            PressureLoss = 0;
            Throttling = 0;
            ValveSetting = 1;
            IsReferenceCircuit = false;
            Velocity = 0;
            ReynoldsNumber = 0;
            FlowRegime = string.Empty;
            IsValid = true;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Клонировать контур
        /// </summary>
        public CircuitViewModel Clone()
        {
            return new CircuitViewModel
            {
                CircuitNumber = CircuitNumber,
                CircuitName = CircuitName,
                Length = Length,
                SupplyLength = SupplyLength,
                Area = Area,
                FlowRate = FlowRate,
                PressureLoss = PressureLoss,
                Throttling = Throttling,
                ValveSetting = ValveSetting,
                IsReferenceCircuit = IsReferenceCircuit,
                Velocity = Velocity,
                ReynoldsNumber = ReynoldsNumber,
                FlowRegime = FlowRegime,
                IsValid = IsValid,
                ErrorMessage = ErrorMessage
            };
        }

        /// <summary>
        /// Строковое представление
        /// </summary>
        public override string ToString()
        {
            return $"Контур {CircuitNumber}: {CircuitName} (L={Length}м, Q={FlowRate}л/ч)";
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/ViewModels/Hydraulics/CircuitViewModelTests.cs`

```csharp
using SnowMeltingCalculator.ViewModels.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.ViewModels.Hydraulics
{
    [TestFixture]
    public class CircuitViewModelTests
    {
        [Test]
        public void Constructor_Default_SetsDefaultValues()
        {
            // Act
            var circuit = new CircuitViewModel();

            // Assert
            Assert.That(circuit.CircuitName, Is.EqualTo("Новый контур"));
            Assert.That(circuit.ValveSetting, Is.EqualTo(1));
            Assert.That(circuit.IsValid, Is.True);
        }

        [Test]
        public void Constructor_WithParameters_SetsValues()
        {
            // Act
            var circuit = new CircuitViewModel(1, "Контур А", 100, 10, 20);

            // Assert
            Assert.That(circuit.CircuitNumber, Is.EqualTo(1));
            Assert.That(circuit.CircuitName, Is.EqualTo("Контур А"));
            Assert.That(circuit.Length, Is.EqualTo(100));
            Assert.That(circuit.SupplyLength, Is.EqualTo(10));
            Assert.That(circuit.Area, Is.EqualTo(20));
        }

        [Test]
        public void PressureLossKPa_ConvertsCorrectly()
        {
            // Arrange
            var circuit = new CircuitViewModel { PressureLoss = 10000 };

            // Assert
            Assert.That(circuit.PressureLossKPa, Is.EqualTo(10));
        }

        [Test]
        public void PressureLossMbar_ConvertsCorrectly()
        {
            // Arrange
            var circuit = new CircuitViewModel { PressureLoss = 10000 };

            // Assert
            Assert.That(circuit.PressureLossMbar, Is.EqualTo(100));
        }

        [Test]
        public void ThrottlingKPa_ConvertsCorrectly()
        {
            // Arrange
            var circuit = new CircuitViewModel { Throttling = 5000 };

            // Assert
            Assert.That(circuit.ThrottlingKPa, Is.EqualTo(5));
        }

        [Test]
        public void SpecificFlowRate_CalculatesCorrectly()
        {
            // Arrange
            var circuit = new CircuitViewModel
            {
                FlowRate = 200,
                Area = 20
            };

            // Assert
            Assert.That(circuit.SpecificFlowRate, Is.EqualTo(10));
        }

        [Test]
        public void SpecificFlowRate_WithZeroArea_ReturnsZero()
        {
            // Arrange
            var circuit = new CircuitViewModel
            {
                FlowRate = 200,
                Area = 0
            };

            // Assert
            Assert.That(circuit.SpecificFlowRate, Is.EqualTo(0));
        }

        [Test]
        public void Status_WhenInvalid_ReturnsErrorMessage()
        {
            // Arrange
            var circuit = new CircuitViewModel
            {
                IsValid = false,
                ErrorMessage = "Ошибка расчёта"
            };

            // Assert
            Assert.That(circuit.Status, Does.Contain("Ошибка"));
        }

        [Test]
        public void Status_WhenReferenceCircuit_ReturnsReferenceStatus()
        {
            // Arrange
            var circuit = new CircuitViewModel
            {
                IsValid = true,
                IsReferenceCircuit = true
            };

            // Assert
            Assert.That(circuit.Status, Does.Contain("Опорный"));
        }

        [Test]
        public void Status_WhenThrottling_ReturnsThrottlingStatus()
        {
            // Arrange
            var circuit = new CircuitViewModel
            {
                IsValid = true,
                IsReferenceCircuit = false,
                Throttling = 5000
            };

            // Assert
            Assert.That(circuit.Status, Does.Contain("Дросселирование"));
        }

        [Test]
        public void StatusColor_WhenInvalid_ReturnsRed()
        {
            // Arrange
            var circuit = new CircuitViewModel { IsValid = false };

            // Assert
            Assert.That(circuit.StatusColor, Is.EqualTo("Red"));
        }

        [Test]
        public void StatusColor_WhenReferenceCircuit_ReturnsGreen()
        {
            // Arrange
            var circuit = new CircuitViewModel
            {
                IsValid = true,
                IsReferenceCircuit = true
            };

            // Assert
            Assert.That(circuit.StatusColor, Is.EqualTo("Green"));
        }

        [Test]
        public void StatusColor_WhenThrottling_ReturnsOrange()
        {
            // Arrange
            var circuit = new CircuitViewModel
            {
                IsValid = true,
                IsReferenceCircuit = false,
                Throttling = 5000
            };

            // Assert
            Assert.That(circuit.StatusColor, Is.EqualTo("Orange"));
        }

        [Test]
        public void Reset_ClearsAllValues()
        {
            // Arrange
            var circuit = new CircuitViewModel
            {
                Length = 100,
                FlowRate = 200,
                PressureLoss = 10000,
                Throttling = 5000,
                ValveSetting = 5,
                IsReferenceCircuit = true
            };

            // Act
            circuit.Reset();

            // Assert
            Assert.That(circuit.Length, Is.EqualTo(0));
            Assert.That(circuit.FlowRate, Is.EqualTo(0));
            Assert.That(circuit.PressureLoss, Is.EqualTo(0));
            Assert.That(circuit.Throttling, Is.EqualTo(0));
            Assert.That(circuit.ValveSetting, Is.EqualTo(1));
            Assert.That(circuit.IsReferenceCircuit, Is.False);
        }

        [Test]
        public void Clone_CreatesCopy()
        {
            // Arrange
            var original = new CircuitViewModel
            {
                CircuitNumber = 1,
                CircuitName = "Контур А",
                Length = 100,
                FlowRate = 200,
                PressureLoss = 10000
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.That(clone.CircuitNumber, Is.EqualTo(original.CircuitNumber));
            Assert.That(clone.CircuitName, Is.EqualTo(original.CircuitName));
            Assert.That(clone.Length, Is.EqualTo(original.Length));
            Assert.That(clone.FlowRate, Is.EqualTo(original.FlowRate));
            Assert.That(clone.PressureLoss, Is.EqualTo(original.PressureLoss));

            // Проверка, что это разные объекты
            Assert.That(clone, Is.Not.SameAs(original));
        }

        [Test]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var circuit = new CircuitViewModel
            {
                CircuitNumber = 1,
                CircuitName = "Контур А",
                Length = 100,
                FlowRate = 200
            };

            // Act
            var str = circuit.ToString();

            // Assert
            Assert.That(str, Does.Contain("Контур 1"));
            Assert.That(str, Does.Contain("Контур А"));
            Assert.That(str, Does.Contain("100"));
            Assert.That(str, Does.Contain("200"));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `CircuitViewModel.cs` создан
- [ ] MVVM паттерн реализован (CommunityToolkit.Mvvm)
- [ ] Все свойства реализованы
- [ ] Вычисляемые свойства работают
- [ ] Методы Reset, Clone работают
- [ ] Unit-тесты проходят успешно
- [ ] XML-документация для всех методов

---

## 6. Примечания

- Используется CommunityToolkit.Mvvm для MVVM
- Поддержка нескольких контуров через ObservableCollection в HydraulicsViewModel
- Вычисляемые свойства для конвертации единиц
- Статус и цвет для отображения в UI