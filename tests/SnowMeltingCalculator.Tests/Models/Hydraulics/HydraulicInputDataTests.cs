using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class HydraulicInputDataTests
    {
        [Test]
        public void Validate_ReturnsValidForCorrectData()
        {
            // Arrange
            var data = new HydraulicInputData
            {
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
                GlycolConcentration = 5, // Невалидно: < 10
                SupplyHeatPercent = 101 // Невалидно: > 100
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
                GlycolConcentration = 50,
                SupplyHeatPercent = 10
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
                GlycolConcentration = 5 // Невалидно: < 10
            };

            // Act & Assert
            Assert.That(data.IsValid, Is.False);
        }

        [Test]
        public void Validate_GlycolConcentrationAtLowerBound_IsValid()
        {
            // Arrange
            var data = new HydraulicInputData
            {
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
                SupplyHeatPercent = 100 // Максимальное значение
            };

            // Act & Assert
            Assert.That(data.IsValid, Is.True);
        }

        [Test]
        public void Validate_SupplySpacingCmMustBePositive()
        {
            // Arrange
            var data = new HydraulicInputData
            {
                SupplySpacing_cm = 0
            };

            // Act
            var result = data.Validate();

            // Assert
            Assert.That(result.IsValid, Is.False);
        }
    }
}
