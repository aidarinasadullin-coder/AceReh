using System;
using System.Collections.Generic;
using NUnit.Framework;
using SnowMeltingCalculator.Core.Extensions;
using SnowMeltingCalculator.Core.Constants;

namespace SnowMeltingCalculator.Tests.Core
{
    /// <summary>
    /// Тесты для ValidationExtensions
    /// </summary>
    [TestFixture]
    public class ValidationExtensionsTests
    {
        #region Тесты ValidateRange

        [Test]
        public void ValidateRange_ValidValue_DoesNotThrow()
        {
            // Arrange
            double value = 50.0;

            // Act & Assert - не должно выбросить исключение
            value.ValidateRange(0.0, 100.0, "test");
        }

        [Test]
        public void ValidateRange_ValueBelowMin_ThrowsException()
        {
            // Arrange
            double value = -10.0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                value.ValidateRange(0.0, 100.0, "test"));
        }

        [Test]
        public void ValidateRange_ValueAboveMax_ThrowsException()
        {
            // Arrange
            double value = 110.0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                value.ValidateRange(0.0, 100.0, "test"));
        }

        [Test]
        public void ValidateRange_WithErrorsList_AddsError()
        {
            // Arrange
            double value = -10.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateRange(0.0, 100.0, "test", errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0], Does.Contain("test"));
        }

        [Test]
        public void ValidateRange_WithErrorsList_ValidValue_NoError()
        {
            // Arrange
            double value = 50.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateRange(0.0, 100.0, "test", errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidateRange_IntValidValue_DoesNotThrow()
        {
            // Arrange
            int value = 50;

            // Act & Assert - не должно выбросить исключение
            value.ValidateRange(0, 100, "test");
        }

        [Test]
        public void ValidateRange_IntValueBelowMin_ThrowsException()
        {
            // Arrange
            int value = -10;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                value.ValidateRange(0, 100, "test"));
        }

        #endregion

        #region Тесты ValidatePositive

        [Test]
        public void ValidatePositive_PositiveValue_DoesNotThrow()
        {
            // Arrange
            double value = 10.0;

            // Act & Assert - не должно выбросить исключение
            value.ValidatePositive("test");
        }

        [Test]
        public void ValidatePositive_Zero_ThrowsException()
        {
            // Arrange
            double value = 0.0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                value.ValidatePositive("test"));
        }

        [Test]
        public void ValidatePositive_Negative_ThrowsException()
        {
            // Arrange
            double value = -10.0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                value.ValidatePositive("test"));
        }

        [Test]
        public void ValidatePositive_WithErrorsList_AddsError()
        {
            // Arrange
            double value = -10.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidatePositive("test", errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidatePositive_IntPositiveValue_DoesNotThrow()
        {
            // Arrange
            int value = 10;

            // Act & Assert - не должно выбросить исключение
            value.ValidatePositive("test");
        }

        #endregion

        #region Тесты ValidateNonNegative

        [Test]
        public void ValidateNonNegative_PositiveValue_DoesNotThrow()
        {
            // Arrange
            double value = 10.0;

            // Act & Assert - не должно выбросить исключение
            value.ValidateNonNegative("test");
        }

        [Test]
        public void ValidateNonNegative_Zero_DoesNotThrow()
        {
            // Arrange
            double value = 0.0;

            // Act & Assert - не должно выбросить исключение
            value.ValidateNonNegative("test");
        }

        [Test]
        public void ValidateNonNegative_Negative_ThrowsException()
        {
            // Arrange
            double value = -10.0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                value.ValidateNonNegative("test"));
        }

        [Test]
        public void ValidateNonNegative_WithErrorsList_AddsError()
        {
            // Arrange
            double value = -10.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateNonNegative("test", errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        #endregion

        #region Тесты ValidateNotNull

        [Test]
        public void ValidateNotNull_NonNullValue_DoesNotThrow()
        {
            // Arrange
            string value = "test";

            // Act & Assert - не должно выбросить исключение
            value.ValidateNotNull("test");
        }

        [Test]
        public void ValidateNotNull_Null_ThrowsException()
        {
            // Arrange
            string? value = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                value.ValidateNotNull("test"));
        }

        [Test]
        public void ValidateNotNull_WithErrorsList_AddsError()
        {
            // Arrange
            string? value = null;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateNotNull("test", errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        #endregion

        #region Тесты специализированных валидаций

        [Test]
        public void ValidateAirTemperature_ValidValue_ReturnsTrue()
        {
            // Arrange
            double value = -15.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateAirTemperature(errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidateAirTemperature_ValueBelowMin_ReturnsFalse()
        {
            // Arrange
            double value = -70.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateAirTemperature(errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidateAirTemperature_ValueAboveMax_ReturnsFalse()
        {
            // Arrange
            double value = 20.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateAirTemperature(errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidateWindSpeed_ValidValue_ReturnsTrue()
        {
            // Arrange
            double value = 5.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateWindSpeed(errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidateWindSpeed_NegativeValue_ReturnsFalse()
        {
            // Arrange
            double value = -5.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateWindSpeed(errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidateSnowfallIntensity_ValidValue_ReturnsTrue()
        {
            // Arrange
            double value = 0.3;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateSnowfallIntensity(errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidateSnowfallIntensity_ValueAboveMax_ReturnsFalse()
        {
            // Arrange
            double value = 25.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateSnowfallIntensity(errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidateSupplyTemperature_ValidValue_ReturnsTrue()
        {
            // Arrange
            double value = 50.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateSupplyTemperature(errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidateSupplyTemperature_ValueBelowMin_ReturnsFalse()
        {
            // Arrange
            double value = 10.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateSupplyTemperature(errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidatePipeSpacing_ValidValue_ReturnsTrue()
        {
            // Arrange
            double value = 200.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidatePipeSpacing(errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidatePipeSpacing_ValueBelowMin_ReturnsFalse()
        {
            // Arrange
            double value = 30.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidatePipeSpacing(errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidateDeltaT_ValidValue_ReturnsTrue()
        {
            // Arrange
            double value = 10.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateDeltaT(errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidateDeltaT_ValueAboveMax_ReturnsFalse()
        {
            // Arrange
            double value = 35.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateDeltaT(errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidateCircuitLength_ValidValue_ReturnsTrue()
        {
            // Arrange
            double value = 80.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateCircuitLength(errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidateCircuitLength_ValueAboveMax_ReturnsFalse()
        {
            // Arrange
            double value = 150.0;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateCircuitLength(errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidateVelocity_ValidValue_ReturnsTrue()
        {
            // Arrange
            double value = 0.8;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateVelocity(errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidateVelocity_ValueBelowMin_ReturnsFalse()
        {
            // Arrange
            double value = 0.05;
            var errors = new List<string>();

            // Act
            bool result = value.ValidateVelocity(errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidatePressureLoss_ValidValue_ReturnsTrue()
        {
            // Arrange
            double value = 25000.0; // 250 мбар
            var errors = new List<string>();

            // Act
            bool result = value.ValidatePressureLoss(errors);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidatePressureLoss_ValueAboveMax_ReturnsFalse()
        {
            // Arrange
            double value = 35000.0; // 350 мбар
            var errors = new List<string>();

            // Act
            bool result = value.ValidatePressureLoss(errors);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0], Does.Contain("мбар"));
        }

        #endregion

        #region Тесты констант

        [Test]
        public void ValidationConstants_HaveCorrectValues()
        {
            // Assert - проверка значений констант
            Assert.That(ValidationConstants.MinAirTemperature, Is.EqualTo(-60.0));
            Assert.That(ValidationConstants.MaxAirTemperature, Is.EqualTo(10.0));
            Assert.That(ValidationConstants.MinWindSpeed, Is.EqualTo(0.0));
            Assert.That(ValidationConstants.MaxWindSpeed, Is.EqualTo(50.0));
            Assert.That(ValidationConstants.MinSnowfallIntensity, Is.EqualTo(0.0));
            Assert.That(ValidationConstants.MaxSnowfallIntensity, Is.EqualTo(20.0));
            Assert.That(ValidationConstants.MinSupplyTemperature, Is.EqualTo(20.0));
            Assert.That(ValidationConstants.MaxSupplyTemperature, Is.EqualTo(90.0));
            Assert.That(ValidationConstants.MinPipeSpacing, Is.EqualTo(50.0));
            Assert.That(ValidationConstants.MaxPipeSpacing, Is.EqualTo(500.0));
            Assert.That(ValidationConstants.MaxCircuitLength, Is.EqualTo(120.0));
            Assert.That(ValidationConstants.MaxPressureLoss, Is.EqualTo(32000));
        }

        [Test]
        public void ThermalConstants_HaveCorrectValues()
        {
            // Assert - проверка значений констант
            Assert.That(ThermalConstants.SnowDensity, Is.EqualTo(900.0));
            Assert.That(ThermalConstants.IceHeatCapacity, Is.EqualTo(2100.0));
            Assert.That(ThermalConstants.IceMeltingHeat, Is.EqualTo(330000.0));
            Assert.That(ThermalConstants.WaterHeatCapacity, Is.EqualTo(4200.0));
            Assert.That(ThermalConstants.HeatTransferCoefficientA, Is.EqualTo(2.26));
            Assert.That(ThermalConstants.HeatTransferCoefficientB, Is.EqualTo(0.33));
            Assert.That(ThermalConstants.HeatTransferCoefficientC, Is.EqualTo(2.6));
        }

        [Test]
        public void HydraulicsConstants_HaveCorrectValues()
        {
            // Assert - проверка значений констант
            Assert.That(HydraulicsConstants.MaxPressureLoss_Pa, Is.EqualTo(32000));
            Assert.That(HydraulicsConstants.MaxPressureLoss_mbar, Is.EqualTo(320.0));
            Assert.That(HydraulicsConstants.MaxCircuitLength_m, Is.EqualTo(120));
            Assert.That(HydraulicsConstants.MinVelocity, Is.EqualTo(0.5));
            Assert.That(HydraulicsConstants.MaxVelocity, Is.EqualTo(2.0));
            Assert.That(HydraulicsConstants.Kv_HKV_D, Is.EqualTo(1.2));
            Assert.That(HydraulicsConstants.Kv_IV_DN25, Is.EqualTo(1.45));
            Assert.That(HydraulicsConstants.Kv_IV_DN32, Is.EqualTo(1.5));
        }

        #endregion
    }
}