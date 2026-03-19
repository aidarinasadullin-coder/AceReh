using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class HydraulicInputDataTests
    {
        [Test]
        public void OperatingTemperature_CalculatesCorrectly()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                SupplyTemperature = 50,
                ReturnTemperature = 30
            };
            
            // Act & Assert
            Assert.That(data.OperatingTemperature, Is.EqualTo(40));
        }
        
        [Test]
        public void DesignTemperature_EqualsColdFiveDayTemperature()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                ColdFiveDayTemperature = -30
            };
            
            // Act & Assert
            Assert.That(data.DesignTemperature, Is.EqualTo(-30));
        }
        
        [Test]
        public void DeltaT_CalculatesCorrectly()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                SupplyTemperature = 50,
                ReturnTemperature = 30
            };
            
            // Act & Assert
            Assert.That(data.DeltaT, Is.EqualTo(20));
        }
        
        [Test]
        public void Validate_ReturnsValidForCorrectData()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                PowerUp = 256,
                PowerDown = 5,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                InnerDiameter = 16,
                ColdFiveDayTemperature = -30,
                GlycolConcentration = 50,
                SupplyHeatPercent = 10
            };
            
            // Act
            var result = data.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasErrors, Is.False);
        }
        
        [Test]
        public void Validate_ReturnsInvalidForIncorrectData()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                PowerUp = 0, // Невалидно
                PowerDown = -1, // Невалидно
                SupplyTemperature = 30,
                ReturnTemperature = 50, // Невалидно: подача < обратки
                InnerDiameter = 0, // Невалидно
                GlycolConcentration = 5, // Невалидно: < 10
                SupplyHeatPercent = 25 // Невалидно: > 20
            };
            
            // Act
            var result = data.Validate();
            
            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.GreaterThan(0));
        }
        
        [Test]
        public void DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var data = new HydraulicInputData();
            
            // Assert
            Assert.That(data.GlycolType, Is.EqualTo(GlycolType.Ethylene));
            Assert.That(data.GlycolConcentration, Is.EqualTo(50.0));
            Assert.That(data.SupplySpacing_cm, Is.EqualTo(5.0));
            Assert.That(data.SupplyHeatPercent, Is.EqualTo(10.0));
            Assert.That(data.ValveType, Is.EqualTo(ValveType.HKV_D));
        }
        
        [Test]
        public void IsValid_ReturnsTrueForValidData()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                PowerUp = 256,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                InnerDiameter = 16
            };
            
            // Act & Assert
            Assert.That(data.IsValid, Is.True);
        }
        
        [Test]
        public void IsValid_ReturnsFalseForInvalidData()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                PowerUp = 0 // Невалидно
            };
            
            // Act & Assert
            Assert.That(data.IsValid, Is.False);
        }
        
        [Test]
        public void OperatingTemperature_WithNegativeTemperatures_CalculatesCorrectly()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                SupplyTemperature = 10,
                ReturnTemperature = 0
            };
            
            // Act & Assert
            Assert.That(data.OperatingTemperature, Is.EqualTo(5));
        }
        
        [Test]
        public void DeltaT_WithNegativeTemperatures_CalculatesCorrectly()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                SupplyTemperature = 5,
                ReturnTemperature = -5
            };
            
            // Act & Assert
            Assert.That(data.DeltaT, Is.EqualTo(10));
        }
        
        [Test]
        public void Validate_GlycolConcentrationAtLowerBound_IsValid()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                PowerUp = 256,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                InnerDiameter = 16,
                GlycolConcentration = 10 // Минимальное значение
            };
            
            // Act & Assert
            Assert.That(data.IsValid, Is.True);
        }
        
        [Test]
        public void Validate_GlycolConcentrationAtUpperBound_IsValid()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                PowerUp = 256,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                InnerDiameter = 16,
                GlycolConcentration = 90 // Максимальное значение
            };
            
            // Act & Assert
            Assert.That(data.IsValid, Is.True);
        }
        
        [Test]
        public void Validate_SupplyHeatPercentAtLowerBound_IsValid()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                PowerUp = 256,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                InnerDiameter = 16,
                SupplyHeatPercent = 0 // Минимальное значение
            };
            
            // Act & Assert
            Assert.That(data.IsValid, Is.True);
        }
        
        [Test]
        public void Validate_SupplyHeatPercentAtUpperBound_IsValid()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                PowerUp = 256,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                InnerDiameter = 16,
                SupplyHeatPercent = 20 // Максимальное значение
            };
            
            // Act & Assert
            Assert.That(data.IsValid, Is.True);
        }
    }
}