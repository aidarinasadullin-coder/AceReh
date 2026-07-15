using System;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using ValidationResult = SnowMeltingCalculator.Core.ValidationResult;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    /// <summary>
    /// Тесты для CircuitValidator
    /// </summary>
    [TestFixture]
    public class CircuitValidatorTests
    {
        private CircuitValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new CircuitValidator();
        }

        #region Valid Data

        [Test]
        public void Validate_ValidCircuit_ReturnsValid()
        {
            // Arrange
            var input = CreateValidCircuit();

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void Validate_LengthAtUpperBoundary_ReturnsValid()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.CircuitLength = 120.0;

            // Act & Assert
            Assert.That(_validator.Validate(input).IsValid, Is.True);
        }

        [Test]
        public void Validate_VelocityAtBoundaries_ReturnsValid()
        {
            // Arrange
            var lower = CreateValidCircuit();
            lower.Velocity = 0.1;

            var upper = CreateValidCircuit();
            upper.Velocity = 2.0;

            // Act & Assert
            Assert.That(_validator.Validate(lower).IsValid, Is.True);
            Assert.That(_validator.Validate(upper).IsValid, Is.True);
        }

        [Test]
        public void Validate_PressureLossAtUpperBoundary_ReturnsValid()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.OperatingResult = new CircuitTemperatureResult { DpRohr = 32000.0 };

            // Act & Assert
            Assert.That(_validator.Validate(input).IsValid, Is.True);
        }

        #endregion

        #region Invalid Length

        [Test]
        public void Validate_LengthTooLong_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.CircuitLength = 150.0;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Длина контура")), Is.True);
        }

        [Test]
        public void Validate_LengthJustAboveMaximum_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.CircuitLength = 120.1;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        #endregion

        #region Invalid Velocity

        [Test]
        public void Validate_VelocityTooLow_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.Velocity = 0.05;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Скорость потока")), Is.True);
        }

        [Test]
        public void Validate_VelocityTooHigh_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.Velocity = 2.5;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Скорость потока")), Is.True);
        }

        [Test]
        public void Validate_VelocityJustBelowMinimum_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.Velocity = 0.09;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_VelocityJustAboveMaximum_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.Velocity = 2.01;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        #endregion

        #region Invalid Pressure Loss

        [Test]
        public void Validate_PressureLossTooHigh_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.OperatingResult = new CircuitTemperatureResult { DpRohr = 35000.0 };

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Потери давления")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("мбар")), Is.True);
        }

        [Test]
        public void Validate_PressureLossJustAboveMaximum_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.OperatingResult = new CircuitTemperatureResult { DpRohr = 32001.0 };

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        #endregion

        #region Combined Errors

        [Test]
        public void Validate_MultipleInvalidProperties_ReturnsAllErrors()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.CircuitLength = 150.0;
            input.Velocity = 0.05;
            input.OperatingResult = new CircuitTemperatureResult { DpRohr = 35000.0 };

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(3));
            Assert.That(result.Errors.Any(e => e.Message.Contains("Длина контура")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Скорость потока")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Потери давления")), Is.True);
        }

        [Test]
        public void Validate_TwoInvalidProperties_ReturnsTwoErrors()
        {
            // Arrange
            var input = CreateValidCircuit();
            input.CircuitLength = 150.0;
            input.OperatingResult = new CircuitTemperatureResult { DpRohr = 35000.0 };

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(2));
            Assert.That(result.Errors.Any(e => e.Message.Contains("Длина контура")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Потери давления")), Is.True);
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

        #region Helper

        private static CircuitRow CreateValidCircuit()
        {
            return new CircuitRow
            {
                CircuitLength = 80.0,
                Velocity = 0.8,
                OperatingResult = new CircuitTemperatureResult { DpRohr = 25000.0 }
            };
        }

        #endregion
    }
}
