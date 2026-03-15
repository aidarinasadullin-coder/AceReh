using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using System;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    /// <summary>
    /// Тесты для HydraulicCalculator
    /// </summary>
    [TestFixture]
    public class HydraulicCalculatorTests
    {
        private GlycolDataService _glycolService = null!;
        private HydraulicCalculator _calculator = null!;

        [SetUp]
        public void Setup()
        {
            _glycolService = new GlycolDataService("data/glycol_data.json");
            _calculator = new HydraulicCalculator(_glycolService);
        }

        #region CalculateVelocity Tests

        [Test]
        public void CalculateVelocity_ReturnsCorrectValue()
        {
            // Arrange
            double flowRate = 100; // л/ч
            double diameter = 16; // мм

            // Act
            double velocity = _calculator.CalculateVelocity(flowRate, diameter);

            // Assert
            // w = 100 × 1000 / (3600 × π × 16² / 4) ≈ 0.138 м/с
            Assert.That(velocity, Is.EqualTo(0.138).Within(0.01));
        }

        [Test]
        public void CalculateVelocity_WithDifferentDiameters_ReturnsCorrectValues()
        {
            // Arrange
            double flowRate = 200; // л/ч

            // Act
            double velocity17 = _calculator.CalculateVelocity(flowRate, 13); // di для 17x2
            double velocity20 = _calculator.CalculateVelocity(flowRate, 16); // di для 20x2
            double velocity25 = _calculator.CalculateVelocity(flowRate, 20.4); // di для 25x2.3

            // Assert - больший диаметр = меньшая скорость
            Assert.That(velocity17, Is.GreaterThan(velocity20));
            Assert.That(velocity20, Is.GreaterThan(velocity25));
        }

        [Test]
        public void CalculateVelocity_ThrowsForInvalidFlowRate()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _calculator.CalculateVelocity(0, 16));
            Assert.Throws<ArgumentException>(() => _calculator.CalculateVelocity(-100, 16));
        }

        [Test]
        public void CalculateVelocity_ThrowsForInvalidDiameter()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _calculator.CalculateVelocity(100, 0));
            Assert.Throws<ArgumentException>(() => _calculator.CalculateVelocity(100, -16));
        }

        #endregion

        #region CalculateReynoldsNumber Tests

        [Test]
        public void CalculateReynoldsNumber_ReturnsCorrectValue()
        {
            // Arrange
            double velocity = 0.5; // м/с
            double diameter = 16; // мм
            double viscosity = 2.16; // мм²/с

            // Act
            double re = _calculator.CalculateReynoldsNumber(velocity, diameter, viscosity);

            // Assert
            // Re = 1000 × 0.5 × 16 / 2.16 ≈ 3704
            Assert.That(re, Is.EqualTo(3704).Within(10));
        }

        [Test]
        public void CalculateReynoldsNumber_LaminarFlow_ReturnsLowValue()
        {
            // Arrange
            double velocity = 0.1; // м/с
            double diameter = 16; // мм
            double viscosity = 5.0; // мм²/с (высокая вязкость)

            // Act
            double re = _calculator.CalculateReynoldsNumber(velocity, diameter, viscosity);

            // Assert
            Assert.That(re, Is.LessThan(2300));
        }

        [Test]
        public void CalculateReynoldsNumber_TurbulentFlow_ReturnsHighValue()
        {
            // Arrange
            double velocity = 1.0; // м/с
            double diameter = 16; // мм
            double viscosity = 1.0; // мм²/с (низкая вязкость)

            // Act
            double re = _calculator.CalculateReynoldsNumber(velocity, diameter, viscosity);

            // Assert
            Assert.That(re, Is.GreaterThan(4000));
        }

        #endregion

        #region DetermineFlowRegime Tests

        [Test]
        public void DetermineFlowRegime_ReturnsCorrectRegime()
        {
            // Act & Assert
            Assert.That(_calculator.DetermineFlowRegime(2000), Is.EqualTo(FlowRegime.Laminar));
            Assert.That(_calculator.DetermineFlowRegime(3000), Is.EqualTo(FlowRegime.Transitional));
            Assert.That(_calculator.DetermineFlowRegime(5000), Is.EqualTo(FlowRegime.Turbulent));
        }

        #endregion

        #region CalculateFrictionFactor Tests

        [Test]
        public void CalculateFrictionFactor_ReturnsCorrectValueForLaminar()
        {
            // Arrange
            double re = 2000;
            double diameter = 16;
            double roughness = 0.007;

            // Act
            double lambda = _calculator.CalculateFrictionFactor(re, diameter, roughness);

            // Assert
            // Ламинарный: λ = 64 / Re = 64 / 2000 = 0.032
            Assert.That(lambda, Is.EqualTo(0.032).Within(0.0001));
        }

        [Test]
        public void CalculateFrictionFactor_ReturnsCorrectValueForTurbulent()
        {
            // Arrange
            double re = 10000;
            double diameter = 16;
            double roughness = 0.007;

            // Act
            double lambda = _calculator.CalculateFrictionFactor(re, diameter, roughness);

            // Assert
            // Для турбулентного режима λ ≈ 0.03-0.04
            Assert.That(lambda, Is.GreaterThan(0.02));
            Assert.That(lambda, Is.LessThan(0.05));
        }

        #endregion

        #region CalculatePressureLossPerMeter Tests

        [Test]
        public void CalculatePressureLossPerMeter_ReturnsCorrectValue()
        {
            // Arrange
            double velocity = 0.5; // м/с
            double density = 1053; // кг/м³
            double lambda = 0.04;
            double diameter = 16; // мм

            // Act
            double pressureLoss = _calculator.CalculatePressureLossPerMeter(velocity, density, lambda, diameter);

            // Assert
            // R = 1000 × (0.5² × 1053 × 0.04) / (2 × 16) ≈ 329 Па/м
            Assert.That(pressureLoss, Is.EqualTo(329).Within(10));
        }

        [Test]
        public void CalculatePressureLossPerMeter_ThrowsForInvalidParameters()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculatePressureLossPerMeter(-0.5, 1053, 0.04, 16));
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculatePressureLossPerMeter(0.5, 0, 0.04, 16));
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculatePressureLossPerMeter(0.5, 1053, 0, 16));
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculatePressureLossPerMeter(0.5, 1053, 0.04, 0));
        }

        #endregion

        #region CalculateValvePressureLoss Tests

        [Test]
        public void CalculateValvePressureLoss_ReturnsCorrectValueForHKV()
        {
            // Arrange
            double flowRate = 200; // л/ч
            double density = 1053; // кг/м³

            // Act
            double pressureLoss = _calculator.CalculateValvePressureLoss(flowRate, density, CollectorType.HKV);

            // Assert
            // Δp = (200 / 1000 / 1.2)² × 100000 × 1053 ≈ 2925 Па
            Assert.That(pressureLoss, Is.EqualTo(2925).Within(50));
        }

        [Test]
        public void CalculateValvePressureLoss_ReturnsCorrectValueForIV()
        {
            // Arrange
            double flowRate = 200; // л/ч
            double density = 1053; // кг/м³

            // Act
            double pressureLoss = _calculator.CalculateValvePressureLoss(flowRate, density, CollectorType.IV);

            // Assert
            // Δp = (200 / 1000 / 1.45)² × 100000 × 1053 ≈ 2000 Па
            Assert.That(pressureLoss, Is.EqualTo(2000).Within(100));
        }

        #endregion

        #region Calculate Tests

        [Test]
        public void Calculate_ReturnsValidResult()
        {
            // Arrange
            var parameters = CreateValidParameters();

            // Act
            var result = _calculator.Calculate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Velocity, Is.GreaterThan(0));
            Assert.That(result.ReynoldsNumber, Is.GreaterThan(0));
            Assert.That(result.FrictionFactor, Is.GreaterThan(0));
            Assert.That(result.PressureLossPerMeter, Is.GreaterThan(0));
        }

        [Test]
        public void Calculate_WithInvalidParameters_ReturnsInvalidResult()
        {
            // Arrange
            var parameters = new HydraulicParameters(); // Пустые параметры

            // Act
            var result = _calculator.Calculate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ValidationErrors.Length, Is.GreaterThan(0));
        }

        #endregion

        #region CalculateBalancing Tests

        [Test]
        public void CalculateBalancing_WithMultipleCircuits_CalculatesThrottling()
        {
            // Arrange
            var circuits = new System.Collections.Generic.List<CircuitResult>
            {
                new CircuitResult { CircuitNumber = 1, TotalPressureLoss = 10000 },
                new CircuitResult { CircuitNumber = 2, TotalPressureLoss = 8000 },
                new CircuitResult { CircuitNumber = 3, TotalPressureLoss = 12000 }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits);

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[0].Throttling, Is.EqualTo(2000).Within(1)); // 12000 - 10000
            Assert.That(result[1].Throttling, Is.EqualTo(4000).Within(1)); // 12000 - 8000
            Assert.That(result[2].Throttling, Is.EqualTo(0)); // Опорный контур
            Assert.That(result[2].IsReferenceCircuit, Is.True);
        }

        [Test]
        public void CalculateBalancing_WithEmptyList_ReturnsEmptyList()
        {
            // Act
            var result = _calculator.CalculateBalancing(new System.Collections.Generic.List<CircuitResult>());

            // Assert
            Assert.That(result.Count, Is.EqualTo(0));
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

        #endregion
    }
}