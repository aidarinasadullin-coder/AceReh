using System;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Thermal;

namespace SnowMeltingCalculator.Tests.Services.Thermal
{
    /// <summary>
    /// Тесты для ThermalResultValidator
    /// </summary>
    [TestFixture]
    public class ThermalResultValidatorTests
    {
        private ThermalResultValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new ThermalResultValidator();
        }

        #region Valid Data

        [Test]
        public void Validate_ValidResult_ReturnsValid()
        {
            // Arrange
            var result = CreateValidResult();

            // Act
            var validationResult = _validator.Validate(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(validationResult.Errors, Is.Empty);
        }

        #endregion

        #region Return Temperature

        [Test]
        public void Validate_NegativeReturnTemperature_ReturnsError()
        {
            // Arrange
            var result = CreateValidResult();
            result.MeanTemperature = 10.0;
            result.SupplyTemperature = 50.0;
            // T_обратки = 2 * 10 - 50 = -30 < 0

            // Act
            var validationResult = _validator.Validate(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors.Count, Is.EqualTo(1));
            Assert.That(validationResult.Errors[0].PropertyName, Is.EqualTo("ReturnTemperature"));
            Assert.That(validationResult.Errors[0].Message, Does.Contain("обратки"));
            Assert.That(validationResult.Errors[0].Message, Does.Contain("-30,0°C"));
        }

        [Test]
        public void Validate_ZeroReturnTemperature_ReturnsValid()
        {
            // Arrange
            var result = CreateValidResult();
            result.MeanTemperature = 25.0;
            result.SupplyTemperature = 50.0;
            // T_обратки = 2 * 25 - 50 = 0, граница допустима

            // Act
            var validationResult = _validator.Validate(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
        }

        #endregion

        #region DeltaT

        [Test]
        public void Validate_ExcessiveDeltaT_ReturnsError()
        {
            // Arrange
            var result = CreateValidResult();
            result.DeltaT = ValidationConstants.MaxDeltaT + 1.0;

            // Act
            var validationResult = _validator.Validate(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors.Count, Is.EqualTo(1));
            Assert.That(validationResult.Errors[0].PropertyName, Is.EqualTo("DeltaT"));
            Assert.That(validationResult.Errors[0].Message, Does.Contain("превышает максимально допустимый"));
        }

        [Test]
        public void Validate_ZeroDeltaT_ReturnsError()
        {
            // Arrange
            var result = CreateValidResult();
            result.DeltaT = 0.0;

            // Act
            var validationResult = _validator.Validate(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors.Count, Is.EqualTo(1));
            Assert.That(validationResult.Errors[0].PropertyName, Is.EqualTo("DeltaT"));
            Assert.That(validationResult.Errors[0].Message, Does.Contain("должен быть положительным"));
        }

        [Test]
        public void Validate_NegativeDeltaT_ReturnsError()
        {
            // Arrange
            var result = CreateValidResult();
            result.DeltaT = -5.0;

            // Act
            var validationResult = _validator.Validate(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors.Count, Is.EqualTo(1));
            Assert.That(validationResult.Errors.Any(e => e.PropertyName == "DeltaT" && e.Message.Contains("должен быть положительным")), Is.True);
        }

        [Test]
        public void Validate_MaxDeltaT_ReturnsValid()
        {
            // Arrange
            var result = CreateValidResult();
            result.DeltaT = ValidationConstants.MaxDeltaT;

            // Act
            var validationResult = _validator.Validate(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
        }

        #endregion

        #region Combined Errors

        [Test]
        public void Validate_NegativeReturnTemperatureAndExcessiveDeltaT_ReturnsBothErrors()
        {
            // Arrange
            var result = CreateValidResult();
            result.MeanTemperature = 10.0;
            result.SupplyTemperature = 50.0;
            result.DeltaT = ValidationConstants.MaxDeltaT + 5.0;

            // Act
            var validationResult = _validator.Validate(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors.Count, Is.EqualTo(2));
            Assert.That(validationResult.Errors.Any(e => e.PropertyName == "ReturnTemperature"), Is.True);
            Assert.That(validationResult.Errors.Any(e => e.PropertyName == "DeltaT"), Is.True);
        }

        [Test]
        public void Validate_NegativeReturnTemperatureAndZeroDeltaT_ReturnsBothErrors()
        {
            // Arrange
            var result = CreateValidResult();
            result.MeanTemperature = 10.0;
            result.SupplyTemperature = 50.0;
            result.DeltaT = 0.0;

            // Act
            var validationResult = _validator.Validate(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors.Count, Is.EqualTo(2));
            Assert.That(validationResult.Errors.Any(e => e.PropertyName == "ReturnTemperature"), Is.True);
            Assert.That(validationResult.Errors.Any(e => e.PropertyName == "DeltaT"), Is.True);
        }

        #endregion

        #region Null Input

        [Test]
        public void Validate_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _validator.Validate(null!));
        }

        #endregion

        #region Helpers

        private static ThermalCalculationResult CreateValidResult()
        {
            return new ThermalCalculationResult
            {
                Alpha = 10.0,
                PowerUp = 100.0,
                PowerDown = 20.0,
                PowerTotal = 120.0,
                MeltingHeat = 80.0,
                RadiationHeat = 20.0,
                ConvectionHeat = 20.0,
                ExcessTemperature = 30.0,
                MeanTemperature = 42.5,
                SupplyTemperature = 50.0,
                ReturnTemperature = 35.0,
                DeltaT = 15.0,
                RFb = 0.1,
                RD = 0.2,
                ParameterM = 5.0,
                EfficiencyEtaR = 0.9,
                MassFlowRate = 100.0,
                VolumeFlowRate = 100.0,
                IsValid = true
            };
        }

        #endregion
    }
}
