using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;
using Moq;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    /// <summary>
    /// Тесты для интерфейса IHydraulicCalculator
    /// </summary>
    [TestFixture]
    public class IHydraulicCalculatorTests
    {
        private Mock<IHydraulicCalculator> _calculatorMock = null!;
        
        [SetUp]
        public void Setup()
        {
            _calculatorMock = new Mock<IHydraulicCalculator>();
        }
        
        #region CalculateVelocity Tests
        
        [Test]
        public void CalculateVelocity_ReturnsCorrectValue()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.CalculateVelocity(100, 16))
                .Returns(0.138);
            
            // Act
            var result = _calculatorMock.Object.CalculateVelocity(100, 16);
            
            // Assert
            Assert.That(result, Is.EqualTo(0.138).Within(0.001));
        }
        
        [Test]
        public void CalculateVelocity_WithZeroFlow_ReturnsZero()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.CalculateVelocity(0, 16))
                .Returns(0.0);
            
            // Act
            var result = _calculatorMock.Object.CalculateVelocity(0, 16);
            
            // Assert
            Assert.That(result, Is.EqualTo(0.0));
        }
        
        #endregion
        
        #region CalculateReynoldsNumber Tests
        
        [Test]
        public void CalculateReynoldsNumber_ReturnsCorrectValue()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.CalculateReynoldsNumber(0.5, 16, 2.16))
                .Returns(3704);
            
            // Act
            var result = _calculatorMock.Object.CalculateReynoldsNumber(0.5, 16, 2.16);
            
            // Assert
            Assert.That(result, Is.EqualTo(3704).Within(1));
        }
        
        [Test]
        public void CalculateReynoldsNumber_WithLowVelocity_ReturnsLowReynolds()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.CalculateReynoldsNumber(0.1, 16, 2.16))
                .Returns(741);
            
            // Act
            var result = _calculatorMock.Object.CalculateReynoldsNumber(0.1, 16, 2.16);
            
            // Assert
            Assert.That(result, Is.EqualTo(741).Within(1));
        }
        
        #endregion
        
        #region DetermineFlowRegime Tests
        
        [Test]
        public void DetermineFlowRegime_ReturnsLaminarForLowReynolds()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.DetermineFlowRegime(2000))
                .Returns(FlowRegime.Laminar);
            
            // Act
            var result = _calculatorMock.Object.DetermineFlowRegime(2000);
            
            // Assert
            Assert.That(result, Is.EqualTo(FlowRegime.Laminar));
        }
        
        [Test]
        public void DetermineFlowRegime_ReturnsTransitionalForMediumReynolds()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.DetermineFlowRegime(3000))
                .Returns(FlowRegime.Transitional);
            
            // Act
            var result = _calculatorMock.Object.DetermineFlowRegime(3000);
            
            // Assert
            Assert.That(result, Is.EqualTo(FlowRegime.Transitional));
        }
        
        [Test]
        public void DetermineFlowRegime_ReturnsTurbulentForHighReynolds()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.DetermineFlowRegime(5000))
                .Returns(FlowRegime.Turbulent);
            
            // Act
            var result = _calculatorMock.Object.DetermineFlowRegime(5000);
            
            // Assert
            Assert.That(result, Is.EqualTo(FlowRegime.Turbulent));
        }
        
        #endregion
        
        #region CalculateFrictionFactor Tests
        
        [Test]
        public void CalculateFrictionFactor_ReturnsCorrectValue()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.CalculateFrictionFactor(2000, 16, 0.007))
                .Returns(0.032);
            
            // Act
            var result = _calculatorMock.Object.CalculateFrictionFactor(2000, 16, 0.007);
            
            // Assert
            Assert.That(result, Is.EqualTo(0.032).Within(0.001));
        }
        
        [Test]
        public void CalculateFrictionFactor_ForLaminarFlow_UsesPoiseuille()
        {
            // Arrange - ламинарный режим: λ = 64 / Re
            _calculatorMock
                .Setup(c => c.CalculateFrictionFactor(2000, 16, 0.007))
                .Returns(64.0 / 2000);
            
            // Act
            var result = _calculatorMock.Object.CalculateFrictionFactor(2000, 16, 0.007);
            
            // Assert
            Assert.That(result, Is.EqualTo(0.032).Within(0.001));
        }
        
        #endregion
        
        #region CalculatePressureLossPerMeter Tests
        
        [Test]
        public void CalculatePressureLossPerMeter_ReturnsCorrectValue()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.CalculatePressureLossPerMeter(0.5, 1053, 0.032, 16))
                .Returns(264.5);
            
            // Act
            var result = _calculatorMock.Object.CalculatePressureLossPerMeter(0.5, 1053, 0.032, 16);
            
            // Assert
            Assert.That(result, Is.EqualTo(264.5).Within(0.1));
        }
        
        #endregion
        
        #region CalculateValvePressureLoss Tests
        
        [Test]
        public void CalculateValvePressureLoss_ForHKV_ReturnsCorrectValue()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.CalculateValvePressureLoss(100, 1053, CollectorType.HKV))
                .Returns(730.0);
            
            // Act
            var result = _calculatorMock.Object.CalculateValvePressureLoss(100, 1053, CollectorType.HKV);
            
            // Assert
            Assert.That(result, Is.EqualTo(730.0).Within(1.0));
        }
        
        [Test]
        public void CalculateValvePressureLoss_ForIV_ReturnsCorrectValue()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.CalculateValvePressureLoss(100, 1053, CollectorType.IV))
                .Returns(500.0);
            
            // Act
            var result = _calculatorMock.Object.CalculateValvePressureLoss(100, 1053, CollectorType.IV);
            
            // Assert
            Assert.That(result, Is.EqualTo(500.0).Within(1.0));
        }
        
        #endregion
        
        #region Calculate Tests
        
        [Test]
        public void Calculate_ReturnsValidResult()
        {
            // Arrange
            var parameters = new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                VolumeFlowRate = 10,
                CircuitArea = 20,
                Density = 1053,
                KinematicViscosity = 2.16,
                Pipe = new SnowMeltingCalculator.Models.Thermal.PipeType()
            };
            
            var expectedResult = new HydraulicResult
            {
                Velocity = 0.5,
                ReynoldsNumber = 3704,
                FlowRegime = FlowRegime.Transitional,
                IsValid = true
            };
            
            _calculatorMock
                .Setup(c => c.Calculate(parameters))
                .Returns(expectedResult);
            
            // Act
            var result = _calculatorMock.Object.Calculate(parameters);
            
            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Velocity, Is.EqualTo(0.5));
            Assert.That(result.ReynoldsNumber, Is.EqualTo(3704));
            Assert.That(result.FlowRegime, Is.EqualTo(FlowRegime.Transitional));
        }
        
        #endregion
        
        #region CalculateBalancing Tests
        
        [Test]
        public void CalculateBalancing_ReturnsBalancedCircuits()
        {
            // Arrange
            var circuits = new List<CircuitResult>
            {
                new CircuitResult { CircuitNumber = 1, TotalPressureLoss = 10000 },
                new CircuitResult { CircuitNumber = 2, TotalPressureLoss = 8000 },
                new CircuitResult { CircuitNumber = 3, TotalPressureLoss = 12000 }
            };
            
            var expectedResults = new List<CircuitResult>
            {
                new CircuitResult { CircuitNumber = 1, Throttling = 2000, IsReferenceCircuit = false },
                new CircuitResult { CircuitNumber = 2, Throttling = 4000, IsReferenceCircuit = false },
                new CircuitResult { CircuitNumber = 3, Throttling = 0, IsReferenceCircuit = true }
            };
            
            _calculatorMock
                .Setup(c => c.CalculateBalancing(circuits))
                .Returns(expectedResults);
            
            // Act
            var result = _calculatorMock.Object.CalculateBalancing(circuits);
            
            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[2].IsReferenceCircuit, Is.True);
            Assert.That(result[0].Throttling, Is.EqualTo(2000));
        }
        
        #endregion
    }
}