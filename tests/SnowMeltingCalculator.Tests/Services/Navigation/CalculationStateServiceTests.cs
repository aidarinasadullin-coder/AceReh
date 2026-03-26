// ================================================================================
// REHAU Снеготаяние - Тесты для CalculationStateService
// ================================================================================

using NUnit.Framework;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Services.Navigation;

namespace SnowMeltingCalculator.Tests.Services.Navigation
{
    /// <summary>
    /// Тесты для CalculationStateService
    /// </summary>
    [TestFixture]
    public class CalculationStateServiceTests
    {
        private CalculationStateService _service = null!;

        [SetUp]
        public void Setup()
        {
            _service = new CalculationStateService();
        }

        #region Тепловой расчёт - Начальное состояние

        [Test]
        public void ThermalNeedsRecalculation_Initially_False()
        {
            // Assert
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
        }

        [Test]
        public void ThermalIsCalculating_Initially_False()
        {
            // Assert
            Assert.That(_service.ThermalIsCalculating, Is.False);
        }

        [Test]
        public void ThermalValidationMessage_Initially_Empty()
        {
            // Assert
            Assert.That(_service.ThermalValidationMessage, Is.EqualTo(string.Empty));
        }

        #endregion

        #region Тепловой расчёт - SetThermalNeedsRecalculation

        [Test]
        public void SetThermalNeedsRecalculation_SetsFlagToTrue()
        {
            // Act
            _service.SetThermalNeedsRecalculation("Test message");

            // Assert
            Assert.That(_service.ThermalNeedsRecalculation, Is.True);
        }

        [Test]
        public void SetThermalNeedsRecalculation_SetsMessage()
        {
            // Act
            _service.SetThermalNeedsRecalculation("Test message");

            // Assert
            Assert.That(_service.ThermalValidationMessage, Is.EqualTo("Test message"));
        }

        [Test]
        public void SetThermalNeedsRecalculation_RaisesStateChangedEvent()
        {
            // Arrange
            ModuleStateChangedEventArgs? eventArgs = null;
            _service.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _service.SetThermalNeedsRecalculation("Test message");

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.Module, Is.EqualTo("Thermal"));
            Assert.That(eventArgs.State, Is.EqualTo(ModuleState.NeedsRecalculation));
            Assert.That(eventArgs.Message, Is.EqualTo("Test message"));
        }

        #endregion

        #region Тепловой расчёт - SetThermalCalculating

        [Test]
        public void SetThermalCalculating_SetsIsCalculatingToTrue()
        {
            // Act
            _service.SetThermalCalculating();

            // Assert
            Assert.That(_service.ThermalIsCalculating, Is.True);
        }

        [Test]
        public void SetThermalCalculating_ResetsNeedsRecalculation()
        {
            // Arrange
            _service.SetThermalNeedsRecalculation("Test message");

            // Act
            _service.SetThermalCalculating();

            // Assert
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
        }

        [Test]
        public void SetThermalCalculating_RaisesStateChangedEvent()
        {
            // Arrange
            ModuleStateChangedEventArgs? eventArgs = null;
            _service.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _service.SetThermalCalculating();

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.Module, Is.EqualTo("Thermal"));
            Assert.That(eventArgs.State, Is.EqualTo(ModuleState.Calculating));
            Assert.That(eventArgs.Message, Is.Null);
        }

        #endregion

        #region Тепловой расчёт - ResetThermalState

        [Test]
        public void ResetThermalState_ResetsIsCalculating()
        {
            // Arrange
            _service.SetThermalCalculating();

            // Act
            _service.ResetThermalState();

            // Assert
            Assert.That(_service.ThermalIsCalculating, Is.False);
        }

        [Test]
        public void ResetThermalState_ResetsNeedsRecalculation()
        {
            // Arrange
            _service.SetThermalNeedsRecalculation("Test message");

            // Act
            _service.ResetThermalState();

            // Assert
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
        }

        [Test]
        public void ResetThermalState_ClearsMessage()
        {
            // Arrange
            _service.SetThermalNeedsRecalculation("Test message");

            // Act
            _service.ResetThermalState();

            // Assert
            Assert.That(_service.ThermalValidationMessage, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ResetThermalState_RaisesStateChangedEvent()
        {
            // Arrange
            ModuleStateChangedEventArgs? eventArgs = null;
            _service.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _service.ResetThermalState();

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.Module, Is.EqualTo("Thermal"));
            Assert.That(eventArgs.State, Is.EqualTo(ModuleState.Actual));
            Assert.That(eventArgs.Message, Is.Null);
        }

        #endregion

        #region Гидравлический расчёт - Начальное состояние

        [Test]
        public void HydraulicsIsCalculating_Initially_False()
        {
            // Assert
            Assert.That(_service.HydraulicsIsCalculating, Is.False);
        }

        #endregion

        #region Гидравлический расчёт - SetHydraulicsCalculating

        [Test]
        public void SetHydraulicsCalculating_SetsFlagToTrue()
        {
            // Act
            _service.SetHydraulicsCalculating();

            // Assert
            Assert.That(_service.HydraulicsIsCalculating, Is.True);
        }

        [Test]
        public void SetHydraulicsCalculating_RaisesStateChangedEvent()
        {
            // Arrange
            ModuleStateChangedEventArgs? eventArgs = null;
            _service.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _service.SetHydraulicsCalculating();

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.Module, Is.EqualTo("Hydraulics"));
            Assert.That(eventArgs.State, Is.EqualTo(ModuleState.Calculating));
            Assert.That(eventArgs.Message, Is.Null);
        }

        #endregion

        #region Гидравлический расчёт - ResetHydraulicsState

        [Test]
        public void ResetHydraulicsState_ResetsIsCalculating()
        {
            // Arrange
            _service.SetHydraulicsCalculating();

            // Act
            _service.ResetHydraulicsState();

            // Assert
            Assert.That(_service.HydraulicsIsCalculating, Is.False);
        }

        [Test]
        public void ResetHydraulicsState_RaisesStateChangedEvent()
        {
            // Arrange
            ModuleStateChangedEventArgs? eventArgs = null;
            _service.StateChanged += (sender, args) => eventArgs = args;

            // Act
            _service.ResetHydraulicsState();

            // Assert
            Assert.That(eventArgs, Is.Not.Null);
            Assert.That(eventArgs!.Module, Is.EqualTo("Hydraulics"));
            Assert.That(eventArgs.State, Is.EqualTo(ModuleState.Actual));
            Assert.That(eventArgs.Message, Is.Null);
        }

        #endregion

        #region Интеграционные тесты

        [Test]
        public void ThermalAndHydraulics_States_AreIndependent()
        {
            // Act
            _service.SetThermalNeedsRecalculation("Thermal needs recalc");
            _service.SetHydraulicsCalculating();

            // Assert
            Assert.That(_service.ThermalNeedsRecalculation, Is.True);
            Assert.That(_service.ThermalIsCalculating, Is.False);
            Assert.That(_service.HydraulicsIsCalculating, Is.True);
        }

        [Test]
        public void FullWorkflow_Thermal()
        {
            // Arrange
            var eventCount = 0;
            _service.StateChanged += (sender, args) => eventCount++;

            // Act & Assert - Initial state
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
            Assert.That(_service.ThermalIsCalculating, Is.False);

            // Act - Set needs recalculation
            _service.SetThermalNeedsRecalculation("Parameters changed");
            Assert.That(_service.ThermalNeedsRecalculation, Is.True);
            Assert.That(_service.ThermalIsCalculating, Is.False);
            Assert.That(_service.ThermalValidationMessage, Is.EqualTo("Parameters changed"));

            // Act - Start calculating
            _service.SetThermalCalculating();
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
            Assert.That(_service.ThermalIsCalculating, Is.True);

            // Act - Reset
            _service.ResetThermalState();
            Assert.That(_service.ThermalNeedsRecalculation, Is.False);
            Assert.That(_service.ThermalIsCalculating, Is.False);
            Assert.That(_service.ThermalValidationMessage, Is.EqualTo(string.Empty));

            // Assert - Event raised 3 times
            Assert.That(eventCount, Is.EqualTo(3));
        }

        [Test]
        public void FullWorkflow_Hydraulics()
        {
            // Arrange
            var eventCount = 0;
            _service.StateChanged += (sender, args) => eventCount++;

            // Act & Assert - Initial state
            Assert.That(_service.HydraulicsIsCalculating, Is.False);

            // Act - Start calculating
            _service.SetHydraulicsCalculating();
            Assert.That(_service.HydraulicsIsCalculating, Is.True);

            // Act - Reset
            _service.ResetHydraulicsState();
            Assert.That(_service.HydraulicsIsCalculating, Is.False);

            // Assert - Event raised 2 times
            Assert.That(eventCount, Is.EqualTo(2));
        }

        #endregion
    }
}