using SnowMeltingCalculator.ViewModels.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.ViewModels.Hydraulics
{
    /// <summary>
    /// Тесты для CircuitViewModel
    /// </summary>
    [TestFixture]
    public class CircuitViewModelTests
    {
        #region Constructor Tests

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

        #endregion

        #region Computed Properties Tests

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
        public void ThrottlingMbar_ConvertsCorrectly()
        {
            // Arrange
            var circuit = new CircuitViewModel { Throttling = 5000 };

            // Assert
            Assert.That(circuit.ThrottlingMbar, Is.EqualTo(50));
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

        #endregion

        #region Status Tests

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
        public void Status_WhenReady_ReturnsReadyStatus()
        {
            // Arrange
            var circuit = new CircuitViewModel
            {
                IsValid = true,
                IsReferenceCircuit = false,
                Throttling = 0
            };

            // Assert
            Assert.That(circuit.Status, Is.EqualTo("Готов"));
        }

        #endregion

        #region StatusColor Tests

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
        public void StatusColor_WhenReady_ReturnsGray()
        {
            // Arrange
            var circuit = new CircuitViewModel
            {
                IsValid = true,
                IsReferenceCircuit = false,
                Throttling = 0
            };

            // Assert
            Assert.That(circuit.StatusColor, Is.EqualTo("Gray"));
        }

        #endregion

        #region Reset Tests

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

        #endregion

        #region Clone Tests

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
        public void Clone_CopiesAllProperties()
        {
            // Arrange
            var original = new CircuitViewModel
            {
                CircuitNumber = 2,
                CircuitName = "Контур Б",
                Length = 150,
                SupplyLength = 15,
                Area = 25,
                FlowRate = 300,
                PressureLoss = 15000,
                Throttling = 3000,
                ValveSetting = 4,
                IsReferenceCircuit = true,
                Velocity = 0.5,
                ReynoldsNumber = 5000,
                FlowRegime = "Turbulent",
                IsValid = true,
                ErrorMessage = ""
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.That(clone.CircuitNumber, Is.EqualTo(2));
            Assert.That(clone.CircuitName, Is.EqualTo("Контур Б"));
            Assert.That(clone.Length, Is.EqualTo(150));
            Assert.That(clone.SupplyLength, Is.EqualTo(15));
            Assert.That(clone.Area, Is.EqualTo(25));
            Assert.That(clone.FlowRate, Is.EqualTo(300));
            Assert.That(clone.PressureLoss, Is.EqualTo(15000));
            Assert.That(clone.Throttling, Is.EqualTo(3000));
            Assert.That(clone.ValveSetting, Is.EqualTo(4));
            Assert.That(clone.IsReferenceCircuit, Is.True);
            Assert.That(clone.Velocity, Is.EqualTo(0.5));
            Assert.That(clone.ReynoldsNumber, Is.EqualTo(5000));
            Assert.That(clone.FlowRegime, Is.EqualTo("Turbulent"));
            Assert.That(clone.IsValid, Is.True);
            Assert.That(clone.ErrorMessage, Is.EqualTo(""));
        }

        #endregion

        #region ToString Tests

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

        #endregion

        #region Property Change Notification Tests

        [Test]
        public void PropertyChange_ComputedPropertiesReturnCorrectValues()
        {
            // Arrange
            var circuit = new CircuitViewModel();

            // Act
            circuit.PressureLoss = 10000;

            // Assert - проверяем, что вычисляемые свойства возвращают правильные значения
            Assert.That(circuit.PressureLossKPa, Is.EqualTo(10));
            Assert.That(circuit.PressureLossMbar, Is.EqualTo(100));
        }

        #endregion
    }
}