using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;

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

        #region Validate Parameters Tests

        [Test]
        public void Validate_ValidParameters_ReturnsValidResult()
        {
            // Arrange
            var parameters = CreateValidParameters();

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void Validate_NullParameters_ReturnsInvalidResult()
        {
            // Act
            var result = _validator.Validate(null);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Validate_CircuitLengthTooSmall_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.CircuitLength = 5; // Меньше минимума (10 м)

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0], Does.Contain("Длина контура"));
        }

        [Test]
        public void Validate_CircuitLengthTooLarge_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.CircuitLength = 600; // Больше максимума (500 м)

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0], Does.Contain("Длина контура"));
        }

        [Test]
        public void Validate_GlycolConcentrationTooSmall_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.GlycolConcentration = 5; // Меньше минимума (10%)

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0], Does.Contain("Доля гликоля"));
        }

        [Test]
        public void Validate_SupplyTempLowerThanReturnTemp_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.SupplyTemperature = 30;
            parameters.ReturnTemperature = 50; // Обратка выше подачи

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("Температура подачи"));
        }

        [Test]
        public void Validate_SmallTemperatureDelta_ReturnsWarning()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.SupplyTemperature = 40;
            parameters.ReturnTemperature = 39; // Перепад 1°C

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Validate_LargeTemperatureDelta_ReturnsWarning()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.SupplyTemperature = 70;
            parameters.ReturnTemperature = 30; // Перепад 40°C

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Validate Result Tests

        [Test]
        public void ValidateResult_ValidResult_ReturnsValidResult()
        {
            // Arrange
            var result = CreateValidResult();

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
        }

        [Test]
        public void ValidateResult_NullResult_ReturnsInvalidResult()
        {
            // Act
            var validationResult = _validator.ValidateResult(null);

            // Assert
            Assert.That(validationResult.IsValid, Is.False);
        }

        [Test]
        public void ValidateResult_LowVelocity_ReturnsWarning()
        {
            // Arrange
            var result = CreateValidResult();
            result.Velocity = 0.1; // Меньше минимума (0.2 м/с)

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(validationResult.Warnings.Count, Is.GreaterThan(0));
            Assert.That(validationResult.Warnings[0], Does.Contain("скорость"));
        }

        [Test]
        public void ValidateResult_HighVelocity_ReturnsWarning()
        {
            // Arrange
            var result = CreateValidResult();
            result.Velocity = 2.0; // Больше максимума (1.5 м/с)

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(validationResult.Warnings.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ValidateResult_TransitionalFlowRegime_ReturnsWarning()
        {
            // Arrange
            var result = CreateValidResult();
            result.ReynoldsNumber = 3000; // Переходный режим
            result.FlowRegime = FlowRegime.Transitional;

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(validationResult.Warnings.Count, Is.GreaterThan(0));
            Assert.That(validationResult.Warnings[0], Does.Contain("переходный"));
        }

        [Test]
        public void ValidateResult_HighPressureLoss_ReturnsWarning()
        {
            // Arrange
            var result = CreateValidResult();
            result.PressureLossPerMeter = 350; // Высокие потери (> 300 Па/м)

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(validationResult.Warnings.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Validate SupplyLength Tests

        [Test]
        public void Validate_SupplyLengthTooSmall_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.SupplyLength = 0.5; // Меньше минимума (1 м)

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0], Does.Contain("подводки"));
        }

        [Test]
        public void Validate_SupplyLengthTooLarge_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.SupplyLength = 150; // Больше максимума (100 м)

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0], Does.Contain("подводки"));
        }

        #endregion

        #region Boundary Cases Tests

        [Test]
        public void Validate_CircuitLengthAtMinimum_ReturnsValid()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.CircuitLength = 10; // Минимум

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_CircuitLengthAtMaximum_ReturnsValid()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.CircuitLength = 500; // Максимум

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_SupplyLengthAtMinimum_ReturnsValid()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.SupplyLength = 1; // Минимум

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_SupplyLengthAtMaximum_ReturnsValid()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.SupplyLength = 100; // Максимум

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_GlycolConcentrationAtMinimum_ReturnsValid()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.GlycolConcentration = 10; // Минимум

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_GlycolConcentrationAtMaximum_ReturnsValid()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.GlycolConcentration = 90; // Максимум

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_TemperatureAtMinimum_ReturnsValid()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.SupplyTemperature = 20; // Минимум
            parameters.ReturnTemperature = 15; // Минимум

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_TemperatureAtMaximum_ReturnsValid()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.SupplyTemperature = 90; // Максимум
            parameters.ReturnTemperature = 80; // Максимум

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public void Validate_NaNValues_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.CircuitLength = double.NaN;

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_InfinityValues_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.CircuitLength = double.PositiveInfinity;

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_NegativeValues_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.VolumeFlowRate = -10; // Отрицательный расход

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void ValidateResult_NaNValues_ReturnsError()
        {
            // Arrange
            var result = CreateValidResult();
            result.Velocity = double.NaN;

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.False);
        }

        [Test]
        public void ValidateResult_NegativeReynoldsNumber_ReturnsError()
        {
            // Arrange
            var result = CreateValidResult();
            result.ReynoldsNumber = -1000;

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.False);
        }

        [Test]
        public void ValidateResult_NegativePressureLoss_ReturnsError()
        {
            // Arrange
            var result = CreateValidResult();
            result.PressureLossPerMeter = -100;

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.False);
        }

        #endregion

        #region Pipe Validation Tests

        [Test]
        public void Validate_InvalidPipe_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.Pipe = null;

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0], Does.Contain("трубы"));
        }

        [Test]
        public void Validate_InvalidPipeDiameter_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.Pipe = new PipeType { OuterDiameter = 0, WallThickness = 2 };

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_InvalidPipeWallThickness_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.Pipe = new PipeType { OuterDiameter = 20, WallThickness = 0 };

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_WallThicknessTooLarge_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.Pipe = new PipeType { OuterDiameter = 20, WallThickness = 15 }; // Толщина слишком большая

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        #endregion

        #region Static Validation Helpers Tests

        [Test]
        public void IsValidParameters_WithValidParameters_ReturnsTrue()
        {
            // Arrange
            var parameters = CreateValidParameters();

            // Act
            bool isValid = HydraulicValidator.IsValidParameters(parameters);

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void IsValidParameters_WithInvalidParameters_ReturnsFalse()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.CircuitLength = 5; // Слишком мало

            // Act
            bool isValid = HydraulicValidator.IsValidParameters(parameters);

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsValidResult_WithValidResult_ReturnsTrue()
        {
            // Arrange
            var result = CreateValidResult();

            // Act
            bool isValid = HydraulicValidator.IsValidResult(result);

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void IsValidResult_WithInvalidResult_ReturnsFalse()
        {
            // Arrange
            var result = CreateValidResult();
            result.ReynoldsNumber = -1000; // Невалидное число Рейнольдса

            // Act
            bool isValid = HydraulicValidator.IsValidResult(result);

            // Assert
            Assert.That(isValid, Is.False);
        }

        #endregion

        #region Helper Methods

        private HydraulicParameters CreateValidParameters()
        {
            return new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                GlycolType = GlycolType.Ethylene,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType 
                { 
                    OuterDiameter = 20, 
                    WallThickness = 2 
                },
                Roughness = 0.007,
                VolumeFlowRate = 10,
                CircuitArea = 20,
                Density = 1053,
                KinematicViscosity = 2.16
            };
        }

        private HydraulicResult CreateValidResult()
        {
            return new HydraulicResult
            {
                Velocity = 0.5,
                ReynoldsNumber = 3700,
                FlowRegime = FlowRegime.Turbulent,
                FrictionFactor = 0.04,
                PressureLossPerMeter = 100,
                TotalPressureLoss = 10000,
                IsValid = true
            };
        }

        #endregion
    }
}