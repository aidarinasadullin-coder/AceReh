using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class CollectorSummaryTests
    {
        [Test]
        public void ValveType_DefaultValue_IsHKV_D()
        {
            // Arrange & Act
            var summary = new CollectorSummary();
            
            // Assert
            Assert.That(summary.ValveType, Is.EqualTo(ValveType.HKV_D));
        }
        
        [Test]
        public void ValveType_CanBeSet()
        {
            // Arrange
            var summary = new CollectorSummary();
            
            // Act
            summary.ValveType = ValveType.IV_1_5;
            
            // Assert
            Assert.That(summary.ValveType, Is.EqualTo(ValveType.IV_1_5));
        }
        
        [Test]
        public void PressureLoss_Operating_Pa_CanBeSet()
        {
            // Arrange
            var summary = new CollectorSummary();
            
            // Act
            summary.PressureLoss_Operating_Pa = 32000; // Па
            
            // Assert
            Assert.That(summary.PressureLoss_Operating_Pa, Is.EqualTo(32000));
        }
        
        [Test]
        public void PressureLoss_Cold_Pa_CanBeSet()
        {
            // Arrange
            var summary = new CollectorSummary();
            
            // Act
            summary.PressureLoss_Cold_Pa = 45000; // Па
            
            // Assert
            Assert.That(summary.PressureLoss_Cold_Pa, Is.EqualTo(45000));
        }
        
        [Test]
        public void PressureLoss_Operating_mbar_ConvertsCorrectly()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Operating_Pa = 32000 // Па
            };
            
            // Assert
            Assert.That(summary.PressureLoss_Operating_mbar, Is.EqualTo(320)); // мбар
        }
        
        [Test]
        public void PressureLoss_Cold_mbar_ConvertsCorrectly()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Cold_Pa = 45000 // Па
            };
            
            // Assert
            Assert.That(summary.PressureLoss_Cold_mbar, Is.EqualTo(450)); // мбар
        }
        
        [Test]
        public void IsColdPressureExceeded_ReturnsTrueWhenExceeded()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Cold_Pa = 35000 // 35000 Па > 32000 Па
            };
            
            // Assert
            Assert.That(summary.IsColdPressureExceeded, Is.True);
        }
        
        [Test]
        public void IsColdPressureExceeded_ReturnsFalseWhenNotExceeded()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Cold_Pa = 30000 // 30000 Па < 32000 Па
            };
            
            // Assert
            Assert.That(summary.IsColdPressureExceeded, Is.False);
        }
        
        [Test]
        public void IsColdPressureExceeded_ReturnsFalseWhenExactlyAtLimit()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Cold_Pa = 32000 // 32000 Па = 32000 Па
            };
            
            // Assert
            Assert.That(summary.IsColdPressureExceeded, Is.False);
        }
        
        [Test]
        public void IsOperatingPressureExceeded_ReturnsTrueWhenExceeded()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Operating_Pa = 43700 // 43700 Па > 32000 Па
            };
            
            // Assert
            Assert.That(summary.IsOperatingPressureExceeded, Is.True);
        }
        
        [Test]
        public void IsOperatingPressureExceeded_ReturnsFalseWhenNotExceeded()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Operating_Pa = 25000 // 25000 Па < 32000 Па
            };
            
            // Assert
            Assert.That(summary.IsOperatingPressureExceeded, Is.False);
        }
        
        [Test]
        public void IsOperatingPressureExceeded_ReturnsFalseWhenExactlyAtLimit()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Operating_Pa = 32000 // 32000 Па = 32000 Па
            };
            
            // Assert
            Assert.That(summary.IsOperatingPressureExceeded, Is.False);
        }
        
        [Test]
        public void BothPressuresExceeded_BothFlagsTrue()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Operating_Pa = 45000, // 45000 Па > 32000 Па
                PressureLoss_Cold_Pa = 50000 // 50000 Па > 32000 Па
            };
            
            // Assert
            Assert.That(summary.IsOperatingPressureExceeded, Is.True);
            Assert.That(summary.IsColdPressureExceeded, Is.True);
        }
        
        [Test]
        public void OnlyOperatingPressureExceeded_OnlyOperatingFlagTrue()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Operating_Pa = 43700, // 43700 Па > 32000 Па
                PressureLoss_Cold_Pa = 25000 // 25000 Па < 32000 Па
            };
            
            // Assert
            Assert.That(summary.IsOperatingPressureExceeded, Is.True);
            Assert.That(summary.IsColdPressureExceeded, Is.False);
        }
        
        [Test]
        public void OnlyColdPressureExceeded_OnlyColdFlagTrue()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Operating_Pa = 25000, // 25000 Па < 32000 Па
                PressureLoss_Cold_Pa = 45000 // 45000 Па > 32000 Па
            };
            
            // Assert
            Assert.That(summary.IsOperatingPressureExceeded, Is.False);
            Assert.That(summary.IsColdPressureExceeded, Is.True);
        }
        
        [Test]
        public void TotalFlowRate_m3h_ConvertsCorrectly()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                TotalFlowRate = 1500 // л/ч
            };
            
            // Assert
            Assert.That(summary.TotalFlowRate_m3h, Is.EqualTo(1.5)); // м³/ч
        }
        
        [Test]
        public void MaxAllowedPressure_mbar_Is320()
        {
            // Assert
            Assert.That(CollectorSummary.MaxAllowedPressure_mbar, Is.EqualTo(320));
        }
        
        [Test]
        public void MaxAllowedPressure_Pa_Is32000()
        {
            // Assert
            Assert.That(CollectorSummary.MaxAllowedPressure_Pa, Is.EqualTo(32000));
        }
        
        [Test]
        public void DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var summary = new CollectorSummary();
            
            // Assert
            Assert.That(summary.CollectorType, Is.EqualTo("HKV-D"));
            Assert.That(summary.Kv, Is.EqualTo(1.2));
            Assert.That(summary.ValveType, Is.EqualTo(ValveType.HKV_D));
            Assert.That(summary.CircuitCount, Is.EqualTo(0));
            Assert.That(summary.TotalPipeLength, Is.EqualTo(0));
            Assert.That(summary.TotalPower, Is.EqualTo(0));
            Assert.That(summary.TotalFlowRate, Is.EqualTo(0));
        }
    }
}