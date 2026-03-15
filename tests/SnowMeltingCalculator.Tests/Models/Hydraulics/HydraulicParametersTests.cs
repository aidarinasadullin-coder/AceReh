using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    /// <summary>
    /// Тесты для класса HydraulicParameters
    /// </summary>
    [TestFixture]
    public class HydraulicParametersTests
    {
        #region Вычисляемые свойства

        [Test]
        public void MeanTemperature_CalculatesCorrectly()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                SupplyTemperature = 50,
                ReturnTemperature = 30
            };
            
            // Act & Assert
            Assert.That(parameters.MeanTemperature, Is.EqualTo(40));
        }

        [Test]
        public void MeanTemperature_WithEqualTemperatures_ReturnsSameValue()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                SupplyTemperature = 40,
                ReturnTemperature = 40
            };
            
            // Act & Assert
            Assert.That(parameters.MeanTemperature, Is.EqualTo(40));
        }

        [Test]
        public void MeanTemperature_WithNegativeTemperatures_CalculatesCorrectly()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                SupplyTemperature = -10,
                ReturnTemperature = -20
            };
            
            // Act & Assert
            Assert.That(parameters.MeanTemperature, Is.EqualTo(-15));
        }

        [Test]
        public void CircuitFlowRate_CalculatesCorrectly()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                VolumeFlowRate = 10, // л/(ч·м²)
                CircuitArea = 20     // м²
            };
            
            // Act & Assert
            Assert.That(parameters.CircuitFlowRate, Is.EqualTo(200)); // 10 × 20 = 200 л/ч
        }

        [Test]
        public void CircuitFlowRate_WithZeroArea_ReturnsZero()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                VolumeFlowRate = 10,
                CircuitArea = 0
            };
            
            // Act & Assert
            Assert.That(parameters.CircuitFlowRate, Is.EqualTo(0));
        }

        [Test]
        public void CircuitFlowRate_WithZeroFlowRate_ReturnsZero()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                VolumeFlowRate = 0,
                CircuitArea = 20
            };
            
            // Act & Assert
            Assert.That(parameters.CircuitFlowRate, Is.EqualTo(0));
        }

        [Test]
        public void InnerDiameter_CalculatesCorrectly()
        {
            // Arrange
            var pipe = new PipeType
            {
                OuterDiameter = 20,
                WallThickness = 2
            };
            var parameters = new HydraulicParameters { Pipe = pipe };
            
            // Act & Assert
            Assert.That(parameters.InnerDiameter, Is.EqualTo(16)); // 20 - 2×2 = 16 мм
        }

        [Test]
        public void InnerDiameter_WithNullPipe_ReturnsZero()
        {
            // Arrange
            var parameters = new HydraulicParameters { Pipe = null };
            
            // Act & Assert
            Assert.That(parameters.InnerDiameter, Is.EqualTo(0));
        }

        [Test]
        public void InnerDiameter_WithStandardPipe_ReturnsCorrectValue()
        {
            // Arrange
            var parameters = new HydraulicParameters 
            { 
                Pipe = PipeType.StandardPipes[1] // 20x2,0
            };
            
            // Act & Assert
            Assert.That(parameters.InnerDiameter, Is.EqualTo(16)); // 20 - 2×2 = 16 мм
        }

        #endregion

        #region Значения по умолчанию

        [Test]
        public void Default_GlycolConcentration_Is50()
        {
            // Arrange & Act
            var parameters = new HydraulicParameters();
            
            // Assert
            Assert.That(parameters.GlycolConcentration, Is.EqualTo(50.0));
        }

        [Test]
        public void Default_GlycolType_IsEthylene()
        {
            // Arrange & Act
            var parameters = new HydraulicParameters();
            
            // Assert
            Assert.That(parameters.GlycolType, Is.EqualTo(GlycolType.Ethylene));
        }

        [Test]
        public void Default_Roughness_Is007()
        {
            // Arrange & Act
            var parameters = new HydraulicParameters();
            
            // Assert
            Assert.That(parameters.Roughness, Is.EqualTo(0.007));
        }

        [Test]
        public void Default_SupplySpacing_Is5()
        {
            // Arrange & Act
            var parameters = new HydraulicParameters();
            
            // Assert
            Assert.That(parameters.SupplySpacing, Is.EqualTo(5.0));
        }

        #endregion

        #region Валидация

        [Test]
        public void Validate_ReturnsValidForCorrectParameters()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void Validate_ReturnsInvalidForCircuitLengthTooSmall()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 5, // < 10
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Длина контура"));
        }

        [Test]
        public void Validate_ReturnsInvalidForCircuitLengthTooLarge()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 600, // > 500
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Длина контура"));
        }

        [Test]
        public void Validate_ReturnsInvalidForSupplyLengthTooSmall()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 0.5, // < 1
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Длина подводки"));
        }

        [Test]
        public void Validate_ReturnsInvalidForSupplyLengthTooLarge()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 200, // > 100
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Длина подводки"));
        }

        [Test]
        public void Validate_ReturnsInvalidForGlycolConcentrationTooSmall()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 5, // < 10
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Доля гликоля"));
        }

        [Test]
        public void Validate_ReturnsInvalidForGlycolConcentrationTooLarge()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 95, // > 90
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Доля гликоля"));
        }

        [Test]
        public void Validate_ReturnsInvalidForSupplyTemperatureTooLow()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 10, // < 20
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Температура подачи"));
        }

        [Test]
        public void Validate_ReturnsInvalidForSupplyTemperatureTooHigh()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 100, // > 90
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Температура подачи"));
        }

        [Test]
        public void Validate_ReturnsInvalidForReturnTemperatureTooLow()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 10, // < 15
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Температура обратки"));
        }

        [Test]
        public void Validate_ReturnsInvalidForReturnTemperatureTooHigh()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 90, // > 80
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Температура обратки"));
        }

        [Test]
        public void Validate_ReturnsInvalidForNullPipe()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = null,
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Тип трубы"));
        }

        [Test]
        public void Validate_ReturnsInvalidForZeroDensity()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 0,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Плотность"));
        }

        [Test]
        public void Validate_ReturnsInvalidForNegativeDensity()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = -100,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Плотность"));
        }

        [Test]
        public void Validate_ReturnsInvalidForZeroKinematicViscosity()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 0
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Кинематическая вязкость"));
        }

        [Test]
        public void Validate_ReturnsInvalidForNegativeKinematicViscosity()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = -1
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("Кинематическая вязкость"));
        }

        [Test]
        public void Validate_ReturnsMultipleErrorsForMultipleInvalidParameters()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 5, // < 10
                SupplyLength = 200, // > 100
                GlycolConcentration = 5, // < 10
                SupplyTemperature = 100, // > 90
                ReturnTemperature = 10, // < 15
                Pipe = null,
                Density = 0,
                KinematicViscosity = 0
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(8));
        }

        [Test]
        public void IsValid_ReturnsTrueForValidParameters()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act & Assert
            Assert.That(parameters.IsValid, Is.True);
        }

        [Test]
        public void IsValid_ReturnsFalseForInvalidParameters()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 5,
                SupplyLength = 200,
                GlycolConcentration = 5,
                SupplyTemperature = 100,
                ReturnTemperature = 10,
                Pipe = null,
                Density = 0,
                KinematicViscosity = 0
            };
            
            // Act & Assert
            Assert.That(parameters.IsValid, Is.False);
        }

        #endregion

        #region Граничные значения

        [Test]
        public void Validate_AcceptsMinimumCircuitLength()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 10, // минимальное значение
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_AcceptsMaximumCircuitLength()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 500, // максимальное значение
                SupplyLength = 10,
                GlycolConcentration = 50,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_AcceptsMinimumGlycolConcentration()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 10, // минимальное значение
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_AcceptsMaximumGlycolConcentration()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 90, // максимальное значение
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType(),
                Density = 1053,
                KinematicViscosity = 2.16
            };
            
            // Act
            var result = parameters.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region Типы гликоля

        [Test]
        public void GlycolType_CanBeSetToEthylene()
        {
            // Arrange & Act
            var parameters = new HydraulicParameters
            {
                GlycolType = GlycolType.Ethylene
            };
            
            // Assert
            Assert.That(parameters.GlycolType, Is.EqualTo(GlycolType.Ethylene));
        }

        [Test]
        public void GlycolType_CanBeSetToPropylene()
        {
            // Arrange & Act
            var parameters = new HydraulicParameters
            {
                GlycolType = GlycolType.Propylene
            };
            
            // Assert
            Assert.That(parameters.GlycolType, Is.EqualTo(GlycolType.Propylene));
        }

        #endregion
    }
}