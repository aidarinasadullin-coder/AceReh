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
    /// Тесты для HydraulicValidator
    /// </summary>
    [TestFixture]
    public class HydraulicValidatorTests
    {
        private HydraulicValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new HydraulicValidator();
        }

        #region Valid Data

        [Test]
        public void Validate_ValidData_ReturnsValid()
        {
            // Arrange
            var input = CreateValidInput();

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void Validate_GlycolConcentrationAtBoundaries_ReturnsValid()
        {
            // Arrange
            var lower = CreateValidInput();
            lower.GlycolConcentration = 10.0;

            var upper = CreateValidInput();
            upper.GlycolConcentration = 90.0;

            // Act & Assert
            Assert.That(_validator.Validate(lower).IsValid, Is.True);
            Assert.That(_validator.Validate(upper).IsValid, Is.True);
        }

        [Test]
        public void Validate_SupplySpacingCmAtPositiveBoundary_ReturnsValid()
        {
            // Arrange
            var input = CreateValidInput();
            input.SupplySpacing_cm = 0.1;

            // Act & Assert
            Assert.That(_validator.Validate(input).IsValid, Is.True);
        }

        [Test]
        public void Validate_HeatPercentAtBoundaries_ReturnsValid()
        {
            // Arrange
            var lower = CreateValidInput();
            lower.SupplyHeatPercent = 0.0;

            var upper = CreateValidInput();
            upper.SupplyHeatPercent = 100.0;

            // Act & Assert
            Assert.That(_validator.Validate(lower).IsValid, Is.True);
            Assert.That(_validator.Validate(upper).IsValid, Is.True);
        }

        #endregion

        #region Invalid Glycol Concentration

        [Test]
        public void Validate_GlycolConcentrationTooLow_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidInput();
            input.GlycolConcentration = 5.0;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Концентрация гликоля")), Is.True);
        }

        [Test]
        public void Validate_GlycolConcentrationTooHigh_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidInput();
            input.GlycolConcentration = 95.0;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Концентрация гликоля")), Is.True);
        }

        [Test]
        public void Validate_GlycolConcentrationJustBelowMinimum_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidInput();
            input.GlycolConcentration = 9.9;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_GlycolConcentrationJustAboveMaximum_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidInput();
            input.GlycolConcentration = 90.1;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        #endregion

        #region Invalid Supply Spacing

        [Test]
        public void Validate_SupplySpacingCmZero_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidInput();
            input.SupplySpacing_cm = 0.0;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Шаг подводки")), Is.True);
        }

        [Test]
        public void Validate_SupplySpacingCmNegative_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidInput();
            input.SupplySpacing_cm = -1.0;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Шаг подводки")), Is.True);
        }

        #endregion

        #region Invalid Heat Percent

        [Test]
        public void Validate_HeatPercentTooLow_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidInput();
            input.SupplyHeatPercent = -1.0;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Доля тепла от подводок")), Is.True);
        }

        [Test]
        public void Validate_HeatPercentTooHigh_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidInput();
            input.SupplyHeatPercent = 105.0;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Доля тепла от подводок")), Is.True);
        }

        [Test]
        public void Validate_HeatPercentJustBelowMinimum_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidInput();
            input.SupplyHeatPercent = -0.1;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_HeatPercentJustAboveMaximum_ReturnsInvalid()
        {
            // Arrange
            var input = CreateValidInput();
            input.SupplyHeatPercent = 100.1;

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
            var input = CreateValidInput();
            input.GlycolConcentration = 5.0;
            input.SupplySpacing_cm = 0.0;
            input.SupplyHeatPercent = -1.0;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(3));
            Assert.That(result.Errors.Any(e => e.Message.Contains("Концентрация гликоля")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Шаг подводки")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Доля тепла от подводок")), Is.True);
        }

        [Test]
        public void Validate_TwoInvalidProperties_ReturnsTwoErrors()
        {
            // Arrange
            var input = CreateValidInput();
            input.GlycolConcentration = 95.0;
            input.SupplyHeatPercent = 101.0;

            // Act
            var result = _validator.Validate(input);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(2));
            Assert.That(result.Errors.Any(e => e.Message.Contains("Концентрация гликоля")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Доля тепла от подводок")), Is.True);
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

        private static HydraulicInputData CreateValidInput()
        {
            return new HydraulicInputData
            {
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 50.0,
                SupplySpacing_cm = 5.0,
                SupplyHeatPercent = 10.0,
                ValveType = ValveType.HKV_D
            };
        }

        #endregion
    }
}
