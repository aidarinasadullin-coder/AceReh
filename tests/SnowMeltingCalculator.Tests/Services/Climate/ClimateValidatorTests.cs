using System;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Services.Climate;
using ValidationResult = SnowMeltingCalculator.Core.ValidationResult;

namespace SnowMeltingCalculator.Tests.Services.Climate
{
    /// <summary>
    /// Тесты для ClimateValidator
    /// </summary>
    [TestFixture]
    public class ClimateValidatorTests
    {
        private ClimateValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new ClimateValidator();
        }

        #region Valid Data

        [Test]
        public void Validate_ValidClimateData_ReturnsValid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -10, windSpeed: 5, snowfallIntensity: 5);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void Validate_BoundaryAirTemperature_ReturnsValid()
        {
            // Arrange
            var lowerBoundary = CreateClimateData(airTemperature: -50, windSpeed: 5, snowfallIntensity: 5);
            var upperBoundary = CreateClimateData(airTemperature: 10, windSpeed: 5, snowfallIntensity: 5);

            // Act & Assert
            Assert.That(_validator.Validate(lowerBoundary).IsValid, Is.True);
            Assert.That(_validator.Validate(upperBoundary).IsValid, Is.True);
        }

        [Test]
        public void Validate_BoundaryWindSpeed_ReturnsValid()
        {
            // Arrange
            var lowerBoundary = CreateClimateData(airTemperature: -10, windSpeed: 0.1, snowfallIntensity: 5);
            var upperBoundary = CreateClimateData(airTemperature: -10, windSpeed: 30, snowfallIntensity: 5);

            // Act & Assert
            Assert.That(_validator.Validate(lowerBoundary).IsValid, Is.True);
            Assert.That(_validator.Validate(upperBoundary).IsValid, Is.True);
        }

        [Test]
        public void Validate_BoundarySnowfallIntensity_ReturnsValid()
        {
            // Arrange
            var lowerBoundary = CreateClimateData(airTemperature: -10, windSpeed: 5, snowfallIntensity: 0);
            var upperBoundary = CreateClimateData(airTemperature: -10, windSpeed: 5, snowfallIntensity: 20);

            // Act & Assert
            Assert.That(_validator.Validate(lowerBoundary).IsValid, Is.True);
            Assert.That(_validator.Validate(upperBoundary).IsValid, Is.True);
        }

        #endregion

        #region Invalid Air Temperature

        [Test]
        public void Validate_AirTemperatureTooLow_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -60, windSpeed: 5, snowfallIntensity: 5);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Температура")), Is.True);
        }

        [Test]
        public void Validate_AirTemperatureTooHigh_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: 15, windSpeed: 5, snowfallIntensity: 5);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Температура")), Is.True);
        }

        [Test]
        public void Validate_AirTemperatureJustBelowMinimum_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -50.1, windSpeed: 5, snowfallIntensity: 5);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_AirTemperatureJustAboveMaximum_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: 10.1, windSpeed: 5, snowfallIntensity: 5);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        #endregion

        #region Invalid Wind Speed

        [Test]
        public void Validate_WindSpeedTooLow_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -10, windSpeed: 0, snowfallIntensity: 5);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("ветра")), Is.True);
        }

        [Test]
        public void Validate_WindSpeedTooHigh_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -10, windSpeed: 40, snowfallIntensity: 5);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("ветра")), Is.True);
        }

        [Test]
        public void Validate_WindSpeedJustBelowMinimum_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -10, windSpeed: 0.09, snowfallIntensity: 5);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_WindSpeedJustAboveMaximum_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -10, windSpeed: 30.1, snowfallIntensity: 5);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        #endregion

        #region Invalid Snowfall Intensity

        [Test]
        public void Validate_SnowfallIntensityTooLow_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -10, windSpeed: 5, snowfallIntensity: -1);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Интенсивность")), Is.True);
        }

        [Test]
        public void Validate_SnowfallIntensityTooHigh_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -10, windSpeed: 5, snowfallIntensity: 25);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Интенсивность")), Is.True);
        }

        [Test]
        public void Validate_SnowfallIntensityJustBelowMinimum_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -10, windSpeed: 5, snowfallIntensity: -0.1);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_SnowfallIntensityJustAboveMaximum_ReturnsInvalid()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -10, windSpeed: 5, snowfallIntensity: 20.1);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        #endregion

        #region Combined Errors

        [Test]
        public void Validate_MultipleInvalidProperties_ReturnsAllErrors()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -60, windSpeed: 0, snowfallIntensity: 25);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(3));
            Assert.That(result.Errors.Any(e => e.Message.Contains("Температура")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("ветра")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Интенсивность")), Is.True);
        }

        [Test]
        public void Validate_TwoInvalidProperties_ReturnsTwoErrors()
        {
            // Arrange
            var data = CreateClimateData(airTemperature: -10, windSpeed: 0, snowfallIntensity: 25);

            // Act
            var result = _validator.Validate(data);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(2));
            Assert.That(result.Errors.Any(e => e.Message.Contains("ветра")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Интенсивность")), Is.True);
        }

        #endregion

        #region Null Input

        [Test]
        public void Validate_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _validator.Validate(null!));
        }

        #endregion

        #region Helper

        private static ClimateData CreateClimateData(double airTemperature, double windSpeed, double snowfallIntensity)
        {
            return new ClimateData
            {
                AirTemperature = airTemperature,
                WindSpeed = windSpeed,
                SnowfallIntensity = snowfallIntensity,
                Humidity = 50
            };
        }

        #endregion
    }
}
