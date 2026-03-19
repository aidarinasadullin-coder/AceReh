using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Moq;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    /// <summary>
    /// Тесты для CircuitsCalculator
    /// </summary>
    [TestFixture]
    public class CircuitsCalculatorTests
    {
        private Mock<IGlycolDataService> _glycolServiceMock;
        private CircuitsCalculator _calculator;

        [SetUp]
        public void Setup()
        {
            _glycolServiceMock = new Mock<IGlycolDataService>();
            
            // Настройка мока для возврата свойств гликоля
            _glycolServiceMock
                .Setup(s => s.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1053,
                    KinematicViscosity = 2.16,
                    SpecificHeat = 3.39,
                    ThermalConductivity = 0.42
                });

            _calculator = new CircuitsCalculator(_glycolServiceMock.Object);
        }

        #region CalculateCircuitPower Tests

        [Test]
        public void CalculateCircuitPower_ReturnsCorrectValue()
        {
            // Arrange
            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 10,
                SupplySpacing_cm = 5,
                SupplyHeatPercent = 10
            };
            double q_up = 256;
            double q_down = 5;
            double pipeSpacing_cm = 20;

            // Act
            double power = _calculator.CalculateCircuitPower(circuit, q_up, q_down, pipeSpacing_cm);

            // Assert
            // Формула: [(L_hk/(100/VA_hk)) + (L_zul/(100/VA_zul))×(q_zul/100)] × (q_up + q_down)
            // = [(100/(100/20)) + (10/(100/5))×(10/100)] × (256 + 5)
            // = [20 + 0.5×0.1] × 261
            // = 20.05 × 261 = 5233.05
            Assert.That(power, Is.EqualTo(5233.05).Within(0.1));
        }

        [Test]
        public void CalculateCircuitPower_WithZeroSupplyLength_ReturnsCorrectValue()
        {
            // Arrange
            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 0,
                SupplySpacing_cm = 5,
                SupplyHeatPercent = 10
            };
            double q_up = 256;
            double q_down = 5;
            double pipeSpacing_cm = 20;

            // Act
            double power = _calculator.CalculateCircuitPower(circuit, q_up, q_down, pipeSpacing_cm);

            // Assert
            // = [20 + 0] × 261 = 5220
            Assert.That(power, Is.EqualTo(5220).Within(0.1));
        }

        [Test]
        public void CalculateCircuitPower_ThrowsForNullCircuit()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _calculator.CalculateCircuitPower(null!, 256, 5, 20));
        }

        [Test]
        public void CalculateCircuitPower_ThrowsForNegativePowerUp()
        {
            // Arrange
            var circuit = new CircuitRow { CircuitLength = 100 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculateCircuitPower(circuit, -10, 5, 20));
        }

        [Test]
        public void CalculateCircuitPower_ThrowsForNegativePowerDown()
        {
            // Arrange
            var circuit = new CircuitRow { CircuitLength = 100 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculateCircuitPower(circuit, 256, -5, 20));
        }

        [Test]
        public void CalculateCircuitPower_ThrowsForZeroPipeSpacing()
        {
            // Arrange
            var circuit = new CircuitRow { CircuitLength = 100 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculateCircuitPower(circuit, 256, 5, 0));
        }

        #endregion

        #region CalculateFlowRate Tests

        [Test]
        public void CalculateFlowRate_ReturnsCorrectValue()
        {
            // Arrange
            double power = 5000; // Вт
            double deltaT = 20; // К
            double density = 1053; // кг/м³
            double specificHeat = 3.39; // кДж/(кг·К)

            // Act
            double flowRate = _calculator.CalculateFlowRate(power, deltaT, density, specificHeat);

            // Assert
            // V_dot = 5000 × 3.6 / (1053 × 3.39 × 20) ≈ 0.253 л/ч
            Assert.That(flowRate, Is.GreaterThan(0));
            Assert.That(flowRate, Is.EqualTo(0.253).Within(0.01));
        }

        [Test]
        public void CalculateFlowRate_WithTypicalValues_ReturnsReasonableValue()
        {
            // Arrange - типичные значения для системы снеготаяния
            double power = 5000; // Вт
            double deltaT = 10; // К (подача 50, обратка 40)
            double density = 1053; // кг/м³ (50% этиленгликоль)
            double specificHeat = 3.39; // кДж/(кг·К)

            // Act
            double flowRate = _calculator.CalculateFlowRate(power, deltaT, density, specificHeat);

            // Assert
            // V_dot = 5000 × 3.6 / (1053 × 3.39 × 10) ≈ 0.506 л/ч
            Assert.That(flowRate, Is.EqualTo(0.506).Within(0.01));
        }

        [Test]
        public void CalculateFlowRate_ThrowsForZeroPower()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculateFlowRate(0, 20, 1053, 3.39));
        }

        [Test]
        public void CalculateFlowRate_ThrowsForNegativePower()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculateFlowRate(-1000, 20, 1053, 3.39));
        }

        [Test]
        public void CalculateFlowRate_ThrowsForZeroDeltaT()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculateFlowRate(5000, 0, 1053, 3.39));
        }

        [Test]
        public void CalculateFlowRate_ThrowsForZeroDensity()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculateFlowRate(5000, 20, 0, 3.39));
        }

        #endregion

        #region CalculateAtTemperature Tests

        [Test]
        public void CalculateAtTemperature_ReturnsValidResult()
        {
            // Arrange
            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 10,
                FlowRate = 200 // л/ч
            };
            double temperature = 40;
            var glycolProps = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16
            };
            double innerDiameter = 16; // мм
            double kv = 1.2;

            // Act
            var result = _calculator.CalculateAtTemperature(
                circuit, temperature, glycolProps, innerDiameter, kv);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Temperature, Is.EqualTo(temperature));
            Assert.That(result.Density, Is.EqualTo(1053));
            Assert.That(result.ReynoldsNumber, Is.GreaterThan(0));
            Assert.That(result.TotalLoss, Is.GreaterThan(0));
        }

        [Test]
        public void CalculateAtTemperature_CalculatesReynoldsCorrectly()
        {
            // Arrange
            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 10,
                FlowRate = 200 // л/ч
            };
            var glycolProps = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16
            };
            double innerDiameter = 16; // мм

            // Act
            var result = _calculator.CalculateAtTemperature(
                circuit, 40, glycolProps, innerDiameter, 1.2);

            // Assert
            // Re = 1000 × v × d_inner / ν
            // v = V_dot × 4 / (3600 × π × d²) × 10⁶
            // Для V_dot = 200 л/ч, d = 16 мм:
            // v ≈ 0.276 м/с
            // Re ≈ 1000 × 0.276 × 16 / 2.16 ≈ 2044
            Assert.That(result.ReynoldsNumber, Is.EqualTo(2044).Within(50));
        }

        [Test]
        public void CalculateAtTemperature_CalculatesPressureLossCorrectly()
        {
            // Arrange
            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 10,
                FlowRate = 200 // л/ч
            };
            var glycolProps = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16
            };
            double innerDiameter = 16; // мм

            // Act
            var result = _calculator.CalculateAtTemperature(
                circuit, 40, glycolProps, innerDiameter, 1.2);

            // Assert
            Assert.That(result.CircuitPipeLoss, Is.GreaterThan(0));
            Assert.That(result.SupplyPipeLoss, Is.GreaterThan(0));
            Assert.That(result.ValveLoss, Is.GreaterThan(0));
            Assert.That(result.TotalLoss, Is.EqualTo(
                result.CircuitPipeLoss + result.SupplyPipeLoss + result.ValveLoss));
        }

        [Test]
        public void CalculateAtTemperature_ThrowsForNullCircuit()
        {
            // Arrange
            var glycolProps = new GlycolProperties { Density = 1053 };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _calculator.CalculateAtTemperature(null!, 40, glycolProps, 16, 1.2));
        }

        [Test]
        public void CalculateAtTemperature_ThrowsForNullGlycolProps()
        {
            // Arrange
            var circuit = new CircuitRow { CircuitLength = 100 };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _calculator.CalculateAtTemperature(circuit, 40, null!, 16, 1.2));
        }

        [Test]
        public void CalculateAtTemperature_ThrowsForZeroDiameter()
        {
            // Arrange
            var circuit = new CircuitRow { CircuitLength = 100 };
            var glycolProps = new GlycolProperties { Density = 1053 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculateAtTemperature(circuit, 40, glycolProps, 0, 1.2));
        }

        #endregion

        #region CalculateAllCircuits Tests

        [Test]
        public void CalculateAllCircuits_CalculatesBothTemperatures()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,
                    SupplyLength = 10,
                    SupplySpacing_cm = 5,
                    SupplyHeatPercent = 10
                }
            };
            var inputData = new HydraulicInputData
            {
                PowerUp = 256,
                PowerDown = 5,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                ColdFiveDayTemperature = -30,
                InnerDiameter = 16,
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 50,
                ValveType = ValveType.HKV_D
            };
            double pipeSpacing_cm = 20;

            // Act
            var result = _calculator.CalculateAllCircuits(circuits, inputData, pipeSpacing_cm);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Power, Is.GreaterThan(0));
            Assert.That(result[0].FlowRate, Is.GreaterThan(0));
            Assert.That(result[0].OperatingResult, Is.Not.Null);
            Assert.That(result[0].DesignResult, Is.Not.Null);
        }

        [Test]
        public void CalculateAllCircuits_WithMultipleCircuits_CalculatesAll()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,
                    SupplyLength = 10
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 80,
                    SupplyLength = 8
                },
                new CircuitRow
                {
                    CircuitNumber = 3,
                    CircuitLength = 120,
                    SupplyLength = 12
                }
            };
            var inputData = new HydraulicInputData
            {
                PowerUp = 256,
                PowerDown = 5,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                ColdFiveDayTemperature = -30,
                InnerDiameter = 16,
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 50,
                ValveType = ValveType.HKV_D
            };
            double pipeSpacing_cm = 20;

            // Act
            var result = _calculator.CalculateAllCircuits(circuits, inputData, pipeSpacing_cm);

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.All(c => c.Power > 0), Is.True);
            Assert.That(result.All(c => c.FlowRate > 0), Is.True);
            Assert.That(result.All(c => c.OperatingResult != null), Is.True);
            Assert.That(result.All(c => c.DesignResult != null), Is.True);
        }

        [Test]
        public void CalculateAllCircuits_ReturnsEmptyListForNullInput()
        {
            // Act
            var result = _calculator.CalculateAllCircuits(null!, new HydraulicInputData(), 20);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void CalculateAllCircuits_ReturnsEmptyListForEmptyList()
        {
            // Act
            var result = _calculator.CalculateAllCircuits(new List<CircuitRow>(), new HydraulicInputData(), 20);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void CalculateAllCircuits_ThrowsForNullInputData()
        {
            // Arrange
            var circuits = new List<CircuitRow> { new CircuitRow() };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _calculator.CalculateAllCircuits(circuits, null!, 20));
        }

        [Test]
        public void CalculateAllCircuits_ThrowsForZeroPipeSpacing()
        {
            // Arrange
            var circuits = new List<CircuitRow> { new CircuitRow { CircuitLength = 100 } };
            var inputData = new HydraulicInputData
            {
                PowerUp = 256,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                InnerDiameter = 16
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _calculator.CalculateAllCircuits(circuits, inputData, 0));
        }

        #endregion

        #region CalculateBalancing Tests

        [Test]
        public void CalculateBalancing_SetsReferenceCircuit()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,
                    SupplyLength = 10,
                    FlowRate = 200,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 8000,
                        SupplyPipeLoss = 1000,
                        ValveLoss = 1000
                    }
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 120,
                    SupplyLength = 12,
                    FlowRate = 240,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 12000,
                        SupplyPipeLoss = 2000,
                        ValveLoss = 1000
                    }
                },
                new CircuitRow
                {
                    CircuitNumber = 3,
                    CircuitLength = 80,
                    SupplyLength = 8,
                    FlowRate = 160,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 10000,
                        SupplyPipeLoss = 1500,
                        ValveLoss = 500
                    }
                }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert
            // Контур 2 имеет максимальные потери: 12000 + 2000 + 1000 = 15000
            Assert.That(result[1].IsReferenceCircuit, Is.True);
            Assert.That(result[0].IsReferenceCircuit, Is.False);
            Assert.That(result[2].IsReferenceCircuit, Is.False);
        }

        [Test]
        public void CalculateBalancing_CalculatesThrottling()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,
                    SupplyLength = 10,
                    FlowRate = 200,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 8000,
                        SupplyPipeLoss = 1000,
                        ValveLoss = 1000
                    }
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 120,
                    SupplyLength = 12,
                    FlowRate = 240,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 12000,
                        SupplyPipeLoss = 2000,
                        ValveLoss = 1000
                    }
                }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert
            // Контур 2: TotalLoss = 15000 (референсный)
            // Контур 1: TotalLoss = 10000
            // Throttling для контура 1 = 15000 - 10000 = 5000
            Assert.That(result[0].Throttling, Is.EqualTo(5000).Within(0.1));
            Assert.That(result[1].Throttling, Is.EqualTo(0).Within(0.01));
        }

        [Test]
        public void CalculateBalancing_ReturnsEmptyListForNullInput()
        {
            // Act
            var result = _calculator.CalculateBalancing(null!, ValveType.HKV_D);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void CalculateBalancing_ReturnsEmptyListForEmptyList()
        {
            // Act
            var result = _calculator.CalculateBalancing(new List<CircuitRow>(), ValveType.HKV_D);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        #endregion

        #region CalculateCollectorSummary Tests

        [Test]
        public void CalculateCollectorSummary_ReturnsCorrectSummary()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,
                    SupplyLength = 10,
                    Power = 5000,
                    FlowRate = 200,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 8000,
                        SupplyPipeLoss = 1000,
                        ValveLoss = 1000
                    },
                    DesignResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 10000,
                        SupplyPipeLoss = 1200,
                        ValveLoss = 1200
                    }
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 80,
                    SupplyLength = 8,
                    Power = 4000,
                    FlowRate = 160,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 6000,
                        SupplyPipeLoss = 1000,
                        ValveLoss = 1000
                    },
                    DesignResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 8000,
                        SupplyPipeLoss = 1000,
                        ValveLoss = 1000
                    }
                }
            };

            // Act
            var summary = _calculator.CalculateCollectorSummary(circuits, 1, ValveType.HKV_D);

            // Assert
            Assert.That(summary.CircuitCount, Is.EqualTo(2));
            Assert.That(summary.TotalPipeLength, Is.EqualTo(198)); // 100+10 + 80+8
            Assert.That(summary.TotalPower, Is.EqualTo(9000));
            Assert.That(summary.TotalFlowRate, Is.EqualTo(360));
            Assert.That(summary.PressureLoss_Operating_mbar, Is.EqualTo(100)); // 10000 Па / 100 = 100 мбар
        }

        [Test]
        public void CalculateCollectorSummary_SetsReferenceCircuit()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,
                    SupplyLength = 10,
                    Power = 5000,
                    FlowRate = 200,
                    IsReferenceCircuit = false,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 8000,
                        SupplyPipeLoss = 1000,
                        ValveLoss = 1000
                    }
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 120,
                    SupplyLength = 12,
                    Power = 6000,
                    FlowRate = 240,
                    IsReferenceCircuit = true,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 12000,
                        SupplyPipeLoss = 2000,
                        ValveLoss = 1000
                    }
                }
            };

            // Act
            var summary = _calculator.CalculateCollectorSummary(circuits, 1, ValveType.HKV_D);

            // Assert
            Assert.That(summary.ReferenceCircuitNumber, Is.EqualTo(2));
        }

        [Test]
        public void CalculateCollectorSummary_ReturnsEmptySummaryForNullInput()
        {
            // Act
            var summary = _calculator.CalculateCollectorSummary(null!, 1, ValveType.HKV_D);

            // Assert
            Assert.That(summary, Is.Not.Null);
            Assert.That(summary.CollectorNumber, Is.EqualTo(1));
            Assert.That(summary.CircuitCount, Is.EqualTo(0));
        }

        [Test]
        public void CalculateCollectorSummary_ReturnsEmptySummaryForEmptyList()
        {
            // Act
            var summary = _calculator.CalculateCollectorSummary(new List<CircuitRow>(), 1, ValveType.HKV_D);

            // Assert
            Assert.That(summary, Is.Not.Null);
            Assert.That(summary.CollectorNumber, Is.EqualTo(1));
            Assert.That(summary.CircuitCount, Is.EqualTo(0));
        }

        [Test]
        public void CalculateCollectorSummary_DetectsPressureExceeded()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,
                    SupplyLength = 10,
                    Power = 5000,
                    FlowRate = 200,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 20000,
                        SupplyPipeLoss = 5000,
                        ValveLoss = 10000
                    },
                    DesignResult = new CircuitTemperatureResult
                    {
                        CircuitPipeLoss = 25000,
                        SupplyPipeLoss = 6000,
                        ValveLoss = 12000
                    }
                }
            };

            // Act
            var summary = _calculator.CalculateCollectorSummary(circuits, 1, ValveType.HKV_D);

            // Assert
            // TotalLoss = 25000 + 6000 + 12000 = 43000 Па = 430 мбар > 320 мбар
            Assert.That(summary.PressureLoss_Cold_mbar, Is.EqualTo(430));
            Assert.That(summary.IsPressureExceeded, Is.True);
            Assert.That(summary.Warnings.Length, Is.GreaterThan(0));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void FullCalculation_Workflow_WorksCorrectly()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,
                    SupplyLength = 10,
                    SupplySpacing_cm = 5,
                    SupplyHeatPercent = 10
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 80,
                    SupplyLength = 8,
                    SupplySpacing_cm = 5,
                    SupplyHeatPercent = 10
                }
            };

            var inputData = new HydraulicInputData
            {
                PowerUp = 256,
                PowerDown = 5,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                ColdFiveDayTemperature = -30,
                InnerDiameter = 16,
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 50,
                ValveType = ValveType.HKV_D
            };
            double pipeSpacing_cm = 20;

            // Act - полный расчёт
            var calculatedCircuits = _calculator.CalculateAllCircuits(circuits, inputData, pipeSpacing_cm);
            var balancedCircuits = _calculator.CalculateBalancing(calculatedCircuits, inputData.ValveType);
            var summary = _calculator.CalculateCollectorSummary(balancedCircuits, 1, inputData.ValveType);

            // Assert
            Assert.That(calculatedCircuits.Count, Is.EqualTo(2));
            Assert.That(balancedCircuits.Count, Is.EqualTo(2));
            Assert.That(summary.CircuitCount, Is.EqualTo(2));
            Assert.That(summary.TotalPower, Is.GreaterThan(0));
            Assert.That(summary.TotalFlowRate, Is.GreaterThan(0));
            
            // Проверяем, что референсный контур определён
            var referenceCircuits = balancedCircuits.Where(c => c.IsReferenceCircuit).ToList();
            Assert.That(referenceCircuits.Count, Is.EqualTo(1));
        }

        #endregion
    }
}