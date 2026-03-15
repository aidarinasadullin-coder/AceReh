using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    /// <summary>
    /// Тесты для класса CircuitResult
    /// </summary>
    [TestFixture]
    public class CircuitResultTests
    {
        #region Вычисляемые свойства

        [Test]
        public void TotalLength_CalculatesCorrectly()
        {
            // Arrange
            var result = new CircuitResult
            {
                Length = 100,
                SupplyLength = 20
            };
            
            // Act & Assert
            Assert.That(result.TotalLength, Is.EqualTo(120));
        }
        
        [Test]
        public void TotalLength_WithZeroSupplyLength_ReturnsCircuitLength()
        {
            // Arrange
            var result = new CircuitResult
            {
                Length = 100,
                SupplyLength = 0
            };
            
            // Act & Assert
            Assert.That(result.TotalLength, Is.EqualTo(100));
        }
        
        [Test]
        public void TotalLength_WithZeroCircuitLength_ReturnsSupplyLength()
        {
            // Arrange
            var result = new CircuitResult
            {
                Length = 0,
                SupplyLength = 20
            };
            
            // Act & Assert
            Assert.That(result.TotalLength, Is.EqualTo(20));
        }
        
        [Test]
        public void TotalPressureLoss_kPa_CalculatesCorrectly()
        {
            // Arrange
            var result = new CircuitResult { TotalPressureLoss = 5000 };
            
            // Act & Assert
            Assert.That(result.TotalPressureLoss_kPa, Is.EqualTo(5));
        }
        
        [Test]
        public void TotalPressureLoss_mbar_CalculatesCorrectly()
        {
            // Arrange
            var result = new CircuitResult { TotalPressureLoss = 32000 };
            
            // Act & Assert
            Assert.That(result.TotalPressureLoss_mbar, Is.EqualTo(320));
        }
        
        [Test]
        public void Throttling_mbar_CalculatesCorrectly()
        {
            // Arrange
            var result = new CircuitResult { Throttling = 5000 };
            
            // Act & Assert
            Assert.That(result.Throttling_mbar, Is.EqualTo(50));
        }
        
        [Test]
        public void Throttling_kPa_CalculatesCorrectly()
        {
            // Arrange
            var result = new CircuitResult { Throttling = 5000 };
            
            // Act & Assert
            Assert.That(result.Throttling_kPa, Is.EqualTo(5));
        }
        
        [Test]
        public void RequiresThrottling_ReturnsTrueWhenPositive()
        {
            // Arrange
            var result = new CircuitResult { Throttling = 100 };
            
            // Act & Assert
            Assert.That(result.RequiresThrottling, Is.True);
        }
        
        [Test]
        public void RequiresThrottling_ReturnsFalseWhenZero()
        {
            // Arrange
            var result = new CircuitResult { Throttling = 0 };
            
            // Act & Assert
            Assert.That(result.RequiresThrottling, Is.False);
        }
        
        [Test]
        public void RequiresThrottling_ReturnsFalseWhenNegative()
        {
            // Arrange
            var result = new CircuitResult { Throttling = -100 };
            
            // Act & Assert
            Assert.That(result.RequiresThrottling, Is.False);
        }

        #endregion

        #region GetSummary

        [Test]
        public void GetSummary_ReturnsCorrectString()
        {
            // Arrange
            var result = new CircuitResult
            {
                CircuitNumber = 1,
                Length = 100,
                FlowRate = 200,
                TotalPressureLoss = 20000
            };
            
            // Act
            var summary = result.GetSummary();
            
            // Assert
            Assert.That(summary, Does.Contain("Контур 1"));
            Assert.That(summary, Does.Contain("100м"));
            Assert.That(summary, Does.Contain("200л/ч"));
            Assert.That(summary, Does.Contain("200мбар"));
        }
        
        [Test]
        public void GetSummary_WithDifferentValues_FormatsCorrectly()
        {
            // Arrange
            var result = new CircuitResult
            {
                CircuitNumber = 5,
                Length = 150.5,
                FlowRate = 350.75,
                TotalPressureLoss = 45000
            };
            
            // Act
            var summary = result.GetSummary();
            
            // Assert
            Assert.That(summary, Does.Contain("Контур 5"));
            Assert.That(summary, Does.Contain("150.5м"));
            Assert.That(summary, Does.Contain("350.8л/ч"));
            Assert.That(summary, Does.Contain("450.0мбар"));
        }

        #endregion

        #region GetBalancingInfo

        [Test]
        public void GetBalancingInfo_ReturnsReferenceCircuitInfo()
        {
            // Arrange
            var result = new CircuitResult
            {
                CircuitNumber = 1,
                IsReferenceCircuit = true
            };
            
            // Act
            var info = result.GetBalancingInfo();
            
            // Assert
            Assert.That(info, Does.Contain("опорный"));
            Assert.That(info, Does.Contain("Контур 1"));
        }
        
        [Test]
        public void GetBalancingInfo_ReturnsThrottlingInfo()
        {
            // Arrange
            var result = new CircuitResult
            {
                CircuitNumber = 2,
                Throttling = 5000,
                RecommendedValveSetting = 5
            };
            
            // Act
            var info = result.GetBalancingInfo();
            
            // Assert
            Assert.That(info, Does.Contain("дросселирование"));
            Assert.That(info, Does.Contain("50мбар"));
            Assert.That(info, Does.Contain("вентиль 5"));
        }
        
        [Test]
        public void GetBalancingInfo_ReturnsNoBalancingNeeded()
        {
            // Arrange
            var result = new CircuitResult
            {
                CircuitNumber = 3,
                Throttling = 0,
                IsReferenceCircuit = false
            };
            
            // Act
            var info = result.GetBalancingInfo();
            
            // Assert
            Assert.That(info, Does.Contain("балансировка не требуется"));
        }

        #endregion

        #region Empty

        [Test]
        public void Empty_CreatesEmptyResult()
        {
            // Act
            var result = CircuitResult.Empty;
            
            // Assert
            Assert.That(result.CircuitNumber, Is.EqualTo(0));
            Assert.That(result.TotalPressureLoss, Is.EqualTo(0));
            Assert.That(result.HydraulicResult, Is.Not.Null);
        }

        #endregion

        #region Значения по умолчанию

        [Test]
        public void Default_HydraulicResultIsNotNull()
        {
            // Arrange & Act
            var result = new CircuitResult();
            
            // Assert
            Assert.That(result.HydraulicResult, Is.Not.Null);
        }
        
        [Test]
        public void Default_IsReferenceCircuitIsFalse()
        {
            // Arrange & Act
            var result = new CircuitResult();
            
            // Assert
            Assert.That(result.IsReferenceCircuit, Is.False);
        }
        
        [Test]
        public void Default_CircuitNameIsNull()
        {
            // Arrange & Act
            var result = new CircuitResult();
            
            // Assert
            Assert.That(result.CircuitName, Is.Null);
        }

        #endregion

        #region Интеграция с HydraulicResult

        [Test]
        public void CircuitResult_CanStoreHydraulicResult()
        {
            // Arrange
            var hydraulicResult = new HydraulicResult
            {
                Velocity = 0.5,
                ReynoldsNumber = 5000,
                FlowRegime = FlowRegime.Turbulent,
                PressureLossPerMeter = 150,
                TotalPressureLoss = 30000
            };
            
            // Act
            var result = new CircuitResult
            {
                CircuitNumber = 1,
                HydraulicResult = hydraulicResult
            };
            
            // Assert
            Assert.That(result.HydraulicResult, Is.Not.Null);
            Assert.That(result.HydraulicResult.Velocity, Is.EqualTo(0.5));
            Assert.That(result.HydraulicResult.ReynoldsNumber, Is.EqualTo(5000));
            Assert.That(result.HydraulicResult.FlowRegime, Is.EqualTo(FlowRegime.Turbulent));
        }

        #endregion

        #region Граничные значения

        [Test]
        public void TotalPressureLoss_WithZeroValue_ReturnsZero()
        {
            // Arrange
            var result = new CircuitResult { TotalPressureLoss = 0 };
            
            // Act & Assert
            Assert.That(result.TotalPressureLoss_kPa, Is.EqualTo(0));
            Assert.That(result.TotalPressureLoss_mbar, Is.EqualTo(0));
        }
        
        [Test]
        public void Throttling_WithZeroValue_ReturnsZero()
        {
            // Arrange
            var result = new CircuitResult { Throttling = 0 };
            
            // Act & Assert
            Assert.That(result.Throttling_kPa, Is.EqualTo(0));
            Assert.That(result.Throttling_mbar, Is.EqualTo(0));
        }
        
        [Test]
        public void TotalLength_WithBothZero_ReturnsZero()
        {
            // Arrange
            var result = new CircuitResult
            {
                Length = 0,
                SupplyLength = 0
            };
            
            // Act & Assert
            Assert.That(result.TotalLength, Is.EqualTo(0));
        }

        #endregion
    }
}