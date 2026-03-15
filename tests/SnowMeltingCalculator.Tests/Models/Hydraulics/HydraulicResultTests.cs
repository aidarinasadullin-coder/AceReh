using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using System.Linq;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    /// <summary>
    /// Тесты для класса HydraulicResult
    /// </summary>
    [TestFixture]
    public class HydraulicResultTests
    {
        #region Вычисляемые свойства

        [Test]
        public void TotalPressureLoss_kPa_CalculatesCorrectly()
        {
            // Arrange
            var result = new HydraulicResult { TotalPressureLoss = 5000 };
            
            // Act & Assert
            Assert.That(result.TotalPressureLoss_kPa, Is.EqualTo(5));
        }
        
        [Test]
        public void TotalPressureLoss_mbar_CalculatesCorrectly()
        {
            // Arrange
            var result = new HydraulicResult { TotalPressureLoss = 32000 };
            
            // Act & Assert
            Assert.That(result.TotalPressureLoss_mbar, Is.EqualTo(320));
        }
        
        [Test]
        public void IsTransitionalFlow_ReturnsTrueForTransitional()
        {
            // Arrange
            var result = new HydraulicResult { FlowRegime = FlowRegime.Transitional };
            
            // Act & Assert
            Assert.That(result.IsTransitionalFlow, Is.True);
        }
        
        [Test]
        public void IsTransitionalFlow_ReturnsFalseForTurbulent()
        {
            // Arrange
            var result = new HydraulicResult { FlowRegime = FlowRegime.Turbulent };
            
            // Act & Assert
            Assert.That(result.IsTransitionalFlow, Is.False);
        }
        
        [Test]
        public void IsTransitionalFlow_ReturnsFalseForLaminar()
        {
            // Arrange
            var result = new HydraulicResult { FlowRegime = FlowRegime.Laminar };
            
            // Act & Assert
            Assert.That(result.IsTransitionalFlow, Is.False);
        }
        
        [Test]
        public void IsLowVelocity_ReturnsTrueForLowVelocity()
        {
            // Arrange
            var result = new HydraulicResult { Velocity = 0.1 };
            
            // Act & Assert
            Assert.That(result.IsLowVelocity, Is.True);
        }
        
        [Test]
        public void IsLowVelocity_ReturnsFalseForNormalVelocity()
        {
            // Arrange
            var result = new HydraulicResult { Velocity = 0.5 };
            
            // Act & Assert
            Assert.That(result.IsLowVelocity, Is.False);
        }
        
        [Test]
        public void IsLowVelocity_ReturnsFalseAtBoundary()
        {
            // Arrange
            var result = new HydraulicResult { Velocity = 0.2 };
            
            // Act & Assert
            Assert.That(result.IsLowVelocity, Is.False);
        }
        
        [Test]
        public void IsHighVelocity_ReturnsTrueForHighVelocity()
        {
            // Arrange
            var result = new HydraulicResult { Velocity = 2.0 };
            
            // Act & Assert
            Assert.That(result.IsHighVelocity, Is.True);
        }
        
        [Test]
        public void IsHighVelocity_ReturnsFalseForNormalVelocity()
        {
            // Arrange
            var result = new HydraulicResult { Velocity = 1.0 };
            
            // Act & Assert
            Assert.That(result.IsHighVelocity, Is.False);
        }
        
        [Test]
        public void IsHighVelocity_ReturnsFalseAtBoundary()
        {
            // Arrange
            var result = new HydraulicResult { Velocity = 1.5 };
            
            // Act & Assert
            Assert.That(result.IsHighVelocity, Is.False);
        }
        
        [Test]
        public void IsPressureLossExceeded_ReturnsTrueWhenExceeded()
        {
            // Arrange
            var result = new HydraulicResult { PressureLossPerMeter = 350 };
            
            // Act & Assert
            Assert.That(result.IsPressureLossExceeded, Is.True);
        }
        
        [Test]
        public void IsPressureLossExceeded_ReturnsFalseWhenWithinLimit()
        {
            // Arrange
            var result = new HydraulicResult { PressureLossPerMeter = 250 };
            
            // Act & Assert
            Assert.That(result.IsPressureLossExceeded, Is.False);
        }
        
        [Test]
        public void IsPressureLossExceeded_ReturnsFalseAtBoundary()
        {
            // Arrange
            var result = new HydraulicResult { PressureLossPerMeter = 300 };
            
            // Act & Assert
            Assert.That(result.IsPressureLossExceeded, Is.False);
        }

        #endregion

        #region GetFlowRegimeDescription

        [Test]
        public void GetFlowRegimeDescription_ReturnsCorrectDescriptionForLaminar()
        {
            // Arrange
            var result = new HydraulicResult { FlowRegime = FlowRegime.Laminar };
            
            // Act
            var description = result.GetFlowRegimeDescription();
            
            // Assert
            Assert.That(description, Does.Contain("Ламинарный"));
            Assert.That(description, Does.Contain("Re < 2300"));
        }
        
        [Test]
        public void GetFlowRegimeDescription_ReturnsCorrectDescriptionForTransitional()
        {
            // Arrange
            var result = new HydraulicResult { FlowRegime = FlowRegime.Transitional };
            
            // Act
            var description = result.GetFlowRegimeDescription();
            
            // Assert
            Assert.That(description, Does.Contain("Переходный"));
            Assert.That(description, Does.Contain("2300"));
            Assert.That(description, Does.Contain("4000"));
        }
        
        [Test]
        public void GetFlowRegimeDescription_ReturnsCorrectDescriptionForTurbulent()
        {
            // Arrange
            var result = new HydraulicResult { FlowRegime = FlowRegime.Turbulent };
            
            // Act
            var description = result.GetFlowRegimeDescription();
            
            // Assert
            Assert.That(description, Does.Contain("Турбулентный"));
            Assert.That(description, Does.Contain("Re > 4000"));
        }

        #endregion

        #region GetWarnings

        [Test]
        public void GetWarnings_ReturnsWarningsForTransitionalFlow()
        {
            // Arrange
            var result = new HydraulicResult
            {
                FlowRegime = FlowRegime.Transitional,
                ReynoldsNumber = 3000
            };
            
            // Act
            var warnings = result.GetWarnings();
            
            // Assert
            Assert.That(warnings.Count, Is.GreaterThan(0));
            Assert.That(warnings[0], Does.Contain("Переходный режим"));
        }
        
        [Test]
        public void GetWarnings_ReturnsWarningsForLowVelocity()
        {
            // Arrange
            var result = new HydraulicResult
            {
                Velocity = 0.1,
                FlowRegime = FlowRegime.Laminar
            };
            
            // Act
            var warnings = result.GetWarnings();
            
            // Assert
            Assert.That(warnings.Any(w => w.Contains("Низкая скорость")), Is.True);
        }
        
        [Test]
        public void GetWarnings_ReturnsWarningsForHighVelocity()
        {
            // Arrange
            var result = new HydraulicResult
            {
                Velocity = 2.0,
                FlowRegime = FlowRegime.Turbulent
            };
            
            // Act
            var warnings = result.GetWarnings();
            
            // Assert
            Assert.That(warnings.Any(w => w.Contains("Высокая скорость")), Is.True);
        }
        
        [Test]
        public void GetWarnings_ReturnsWarningsForPressureLossExceeded()
        {
            // Arrange
            var result = new HydraulicResult
            {
                PressureLossPerMeter = 350,
                FlowRegime = FlowRegime.Turbulent
            };
            
            // Act
            var warnings = result.GetWarnings();
            
            // Assert
            Assert.That(warnings.Any(w => w.Contains("Превышение удельных потерь")), Is.True);
        }
        
        [Test]
        public void GetWarnings_ReturnsMultipleWarnings()
        {
            // Arrange
            var result = new HydraulicResult
            {
                Velocity = 0.1,
                PressureLossPerMeter = 350,
                FlowRegime = FlowRegime.Laminar
            };
            
            // Act
            var warnings = result.GetWarnings();
            
            // Assert
            Assert.That(warnings.Count, Is.EqualTo(2));
        }
        
        [Test]
        public void GetWarnings_ReturnsEmptyListForNormalConditions()
        {
            // Arrange
            var result = new HydraulicResult
            {
                Velocity = 0.5,
                PressureLossPerMeter = 200,
                FlowRegime = FlowRegime.Turbulent
            };
            
            // Act
            var warnings = result.GetWarnings();
            
            // Assert
            Assert.That(warnings.Count, Is.EqualTo(0));
        }

        #endregion

        #region Empty

        [Test]
        public void Empty_CreatesEmptyResult()
        {
            // Act
            var result = HydraulicResult.Empty;
            
            // Assert
            Assert.That(result.Velocity, Is.EqualTo(0));
            Assert.That(result.ReynoldsNumber, Is.EqualTo(0));
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FlowRegime, Is.EqualTo(FlowRegime.Laminar));
        }

        #endregion

        #region Значения по умолчанию

        [Test]
        public void Default_ValidationErrorsIsEmptyArray()
        {
            // Arrange & Act
            var result = new HydraulicResult();
            
            // Assert
            Assert.That(result.ValidationErrors, Is.Not.Null);
            Assert.That(result.ValidationErrors.Length, Is.EqualTo(0));
        }
        
        [Test]
        public void Default_WarningsIsEmptyArray()
        {
            // Arrange & Act
            var result = new HydraulicResult();
            
            // Assert
            Assert.That(result.Warnings, Is.Not.Null);
            Assert.That(result.Warnings.Length, Is.EqualTo(0));
        }

        #endregion

        #region Граничные значения

        [Test]
        public void TotalPressureLoss_kPa_WithZeroValue_ReturnsZero()
        {
            // Arrange
            var result = new HydraulicResult { TotalPressureLoss = 0 };
            
            // Act & Assert
            Assert.That(result.TotalPressureLoss_kPa, Is.EqualTo(0));
        }
        
        [Test]
        public void TotalPressureLoss_mbar_WithZeroValue_ReturnsZero()
        {
            // Arrange
            var result = new HydraulicResult { TotalPressureLoss = 0 };
            
            // Act & Assert
            Assert.That(result.TotalPressureLoss_mbar, Is.EqualTo(0));
        }

        #endregion
    }
}