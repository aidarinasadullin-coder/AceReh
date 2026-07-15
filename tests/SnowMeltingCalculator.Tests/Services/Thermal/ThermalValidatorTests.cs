using System;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Thermal;

namespace SnowMeltingCalculator.Tests.Services.Thermal
{
    /// <summary>
    /// Тесты для ThermalValidator
    /// </summary>
    [TestFixture]
    public class ThermalValidatorTests
    {
        private ThermalValidator _validator = null!;
        private IThermalCalculator _calculator = null!;
        private IClimateData _climate = null!;
        private IConstructionData _construction = null!;

        [SetUp]
        public void Setup()
        {
            _calculator = new ThermalCalculator();
            _climate = CreateClimateData();
            _construction = CreateConstructionData();
            _validator = new ThermalValidator(_calculator, _climate, _construction);
        }

        #region Valid Data

        [Test]
        public void Validate_ValidInputs_ReturnsValid()
        {
            // Arrange
            var inputs = CreateValidInputs();

            // Act
            var result = _validator.Validate(inputs);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        #endregion

        #region Invalid Data

        [Test]
        public void Validate_InvalidInputs_ReturnsInvalid()
        {
            // Arrange
            var inputs = CreateValidInputs();
            inputs = inputs with { SupplyTemperature = 5.0 };

            // Act
            var result = _validator.Validate(inputs);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Температура подачи")), Is.True);
        }

        [Test]
        public void Validate_MultipleInvalidInputs_ReturnsAllErrors()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var invalidClimate = new ClimateData
            {
                AirTemperature = 15.0,
                WindSpeed = -1.0,
                SnowfallIntensity = 25.0,
                Humidity = 50.0
            };
            _validator = new ThermalValidator(_calculator, invalidClimate, _construction);

            // Act
            var result = _validator.Validate(inputs);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(3));
            Assert.That(result.Errors.Any(e => e.Message.Contains("Температура")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("ветра")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Интенсивность")), Is.True);
        }

        [Test]
        public void Validate_InvalidConstruction_ReturnsConstructionErrors()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var invalidConstruction = new ConstructionData
            {
                R1Total = -0.1,
                R2Total = -0.1,
                LambdaE = 1.6
            };
            _validator = new ThermalValidator(_calculator, _climate, invalidConstruction);

            // Act
            var result = _validator.Validate(inputs);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Сопротивление слоёв над трубой")), Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Сопротивление слоёв под трубой")), Is.True);
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

        private static ThermalInputs CreateValidInputs()
        {
            return new ThermalInputs
            {
                Mode = OperatingMode.Melting,
                SupplyTemperature = 50.0,
                DeltaT = 15.0,
                GroundTemperature = 10.0,
                Pipe = PipeType.StandardPipes[1],
                PipeSpacing = 200.0,
                LambdaE = 1.6,
                CoolantDensity = 1053.0,
                CoolantHeatCapacity = 3.39
            };
        }

        private static IClimateData CreateClimateData()
        {
            return new ClimateData
            {
                AirTemperature = -10.0,
                WindSpeed = 5.0,
                SnowfallIntensity = 5.0,
                Humidity = 50.0
            };
        }

        private static IConstructionData CreateConstructionData()
        {
            return new ConstructionData
            {
                R1Total = 0.05,
                R2Total = 0.1,
                LambdaE = 1.6
            };
        }

        #endregion
    }
}
