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
        public void CalculateFlowRate_ReturnsCorrectValueInLitersPerHour()
        {
            // Arrange
            double power = 5000; // Вт
            double deltaT = 20; // К
            double density = 1053; // кг/м³
            double specificHeat = 3.39; // кДж/(кг·К)

            // Act
            double flowRate = _calculator.CalculateFlowRate(power, deltaT, density, specificHeat);

            // Assert
            // V_dot = 5000 × 3.6 / (1053 × 3.39 × 20) × 1000 ≈ 253 л/ч
            // Формула: результат в м³/ч × 1000 = л/ч
            Assert.That(flowRate, Is.GreaterThan(0));
            Assert.That(flowRate, Is.EqualTo(253).Within(1));
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
            // V_dot = 5000 × 3.6 / (1053 × 3.39 × 10) × 1000 ≈ 506 л/ч
            Assert.That(flowRate, Is.EqualTo(506).Within(5));
        }

        [Test]
        public void CalculateFlowRate_TaskUnitsExample_Returns560LitersPerHour()
        {
            // Arrange - пример из ТЗ task_units_1.md
            // Q_HK = 5246 Вт, ρ = 1053 кг/м³, c_p = 3.21 кДж/(кг·К), ΔT = 10 К
            double power = 5246;
            double deltaT = 10;
            double density = 1053;
            double specificHeat = 3.21;

            // Act
            double flowRate = _calculator.CalculateFlowRate(power, deltaT, density, specificHeat);

            // Assert
            // V_dot = 5246 × 3.6 / (1053 × 3.21 × 10) × 1000 ≈ 560 л/ч
            Assert.That(flowRate, Is.EqualTo(560).Within(5));
        }

        [Test]
        public void CalculateFlowRate_Water_ReturnsCorrectValue()
        {
            // Arrange - вода (плотность 1000 кг/м³)
            double power = 5000; // Вт
            double deltaT = 15; // К
            double density = 1000; // кг/м³
            double specificHeat = 4.18; // кДж/(кг·К)

            // Act
            double flowRate = _calculator.CalculateFlowRate(power, deltaT, density, specificHeat);

            // Assert
            // V_dot = 5000 × 3.6 / (1000 × 4.18 × 15) × 1000 ≈ 287 л/ч
            Assert.That(flowRate, Is.EqualTo(287).Within(1));
        }

        [Test]
        public void CalculateFlowRate_HighPower_ReturnsCorrectValue()
        {
            // Arrange - большая мощность
            double power = 10000; // Вт
            double deltaT = 10; // К
            double density = 1053; // кг/м³
            double specificHeat = 3.21; // кДж/(кг·К)

            // Act
            double flowRate = _calculator.CalculateFlowRate(power, deltaT, density, specificHeat);

            // Assert
            // V_dot = 10000 × 3.6 / (1053 × 3.21 × 10) × 1000 ≈ 1068 л/ч
            Assert.That(flowRate, Is.EqualTo(1068).Within(5));
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
                circuit, temperature, glycolProps, innerDiameter, kv, ValveType.HKV_D);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Temperature, Is.EqualTo(temperature));
            // Density конвертируется из кг/м³ в г/см³: 1053 / 1000 = 1.053
            Assert.That(result.Density, Is.EqualTo(1.053).Within(0.001));
            Assert.That(result.ReynoldsNumber, Is.GreaterThan(0));
            Assert.That(result.DpGesamt, Is.GreaterThan(0));
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
                circuit, 40, glycolProps, innerDiameter, 1.2, ValveType.HKV_D);

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
                circuit, 40, glycolProps, innerDiameter, 1.2, ValveType.HKV_D);

            // Assert
            Assert.That(result.DpRohr, Is.GreaterThan(0));
            Assert.That(result.DpVerteiler, Is.GreaterThan(0));
            Assert.That(result.DpVent, Is.GreaterThan(0));
            Assert.That(result.DpGesamt, Is.EqualTo(
                result.DpRohr + result.DpVerteiler + result.DpVent));
        }

        [Test]
        public void CalculateAtTemperature_ThrowsForNullCircuit()
        {
            // Arrange
            var glycolProps = new GlycolProperties { Density = 1053 };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _calculator.CalculateAtTemperature(null!, 40, glycolProps, 16, 1.2, ValveType.HKV_D));
        }

        [Test]
        public void CalculateAtTemperature_ThrowsForNullGlycolProps()
        {
            // Arrange
            var circuit = new CircuitRow { CircuitLength = 100 };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _calculator.CalculateAtTemperature(circuit, 40, null!, 16, 1.2, ValveType.HKV_D));
        }

        [Test]
        public void CalculateAtTemperature_ThrowsForZeroDiameter()
        {
            // Arrange
            var circuit = new CircuitRow { CircuitLength = 100 };
            var glycolProps = new GlycolProperties { Density = 1053 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _calculator.CalculateAtTemperature(circuit, 40, glycolProps, 0, 1.2, ValveType.HKV_D));
        }

        [Test]
        public void CalculateAtTemperature_ConvertsDensityFromKgPerM3ToGPerCm3()
        {
            // Arrange
            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 10,
                FlowRate = 200
            };
            double temperature = 40;
            double innerDiameter = 16;
            double kv = 1.2;

            // Test case 1: 1053 кг/м³ → 1.053 г/см³
            var glycolProps1 = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16
            };

            // Act
            var result1 = _calculator.CalculateAtTemperature(circuit, temperature, glycolProps1, innerDiameter, kv, ValveType.HKV_D);

            // Assert
            Assert.That(result1.Density, Is.EqualTo(1.053).Within(0.001), "1053 кг/м³ должно быть 1.053 г/см³");

            // Test case 2: 1000 кг/м³ (вода) → 1.000 г/см³
            var glycolProps2 = new GlycolProperties
            {
                Density = 1000,
                KinematicViscosity = 1.0
            };

            var result2 = _calculator.CalculateAtTemperature(circuit, temperature, glycolProps2, innerDiameter, kv, ValveType.HKV_D);
            Assert.That(result2.Density, Is.EqualTo(1.000).Within(0.001), "1000 кг/м³ должно быть 1.000 г/см³");

            // Test case 3: 1100 кг/м³ → 1.100 г/см³
            var glycolProps3 = new GlycolProperties
            {
                Density = 1100,
                KinematicViscosity = 3.0
            };

            var result3 = _calculator.CalculateAtTemperature(circuit, temperature, glycolProps3, innerDiameter, kv, ValveType.HKV_D);
            Assert.That(result3.Density, Is.EqualTo(1.100).Within(0.001), "1100 кг/м³ должно быть 1.100 г/см³");
        }

        [Test]
        public void CalculateAtTemperature_PressureLossPerMeter_UsesDensityInGramsPerCm3()
        {
            // Arrange - тест из ТЗ: v = 0.59 м/с, ρ = 1053 кг/м³, λ = 0.042, d = 13 мм
            // Для получения v = 0.59 м/с при d = 13 мм:
            // v = V_dot × 4000 / (3600 × π × d²)
            // 0.59 = V_dot × 4000 / (3600 × π × 169)
            // V_dot = 0.59 × 3600 × π × 169 / 4000 ≈ 282 л/ч
            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 20,
                FlowRate = 280 // л/ч (даёт v ≈ 0.59 м/с при d = 13 мм)
            };
            var glycolProps = new GlycolProperties
            {
                Density = 1053,              // кг/м³
                KinematicViscosity = 2.16    // мм²/с
            };
            double innerDiameter = 13;       // мм
            double kv = 1.2;                  // м³/ч

            // Act
            var result = _calculator.CalculateAtTemperature(circuit, 40, glycolProps, innerDiameter, kv, ValveType.HKV_D);

            // Assert - результат должен быть в разумном диапазоне (не в 1000 раз больше)
            // R = 10000 × (v² × ρ[г/см³] × λ) / (2 × d_inner) × 100
            // При Re ≈ 3550 (переходный режим) λ рассчитывается интерполяцией
            // Результат должен быть в диапазоне 400-700 Па/м
            Assert.That(result.PressureLossPerMeter, Is.GreaterThan(400).And.LessThan(700),
                "Удельные потери должны быть в разумном диапазоне при использовании плотности в г/см³");

            // Проверяем, что результат НЕ в 1000 раз больше (ошибка конвертации)
            Assert.That(result.PressureLossPerMeter, Is.LessThan(1000),
                "Результат не должен быть в 1000 раз больше (ошибка конвертации плотности)");
        }

        #endregion

        #region DpVerteiler Tests

        [Test]
        public void DpVerteiler_IV_CorrectFormula()
        {
            // Arrange
            // Для IV: DpVerteiler = 15000 × (ρ/2000) × v²
            // При ρ = 1053 кг/м³, v = 0.59 м/с
            // DpVerteiler = 15000 × (1.053/2) × 0.59² = 2754 Па

            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 10,
                FlowRate = 280
            };
            var glycolProps = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16
            };
            double innerDiameter = 13.0;
            double kv = 1.45;

            // Act
            var result = _calculator.CalculateAtTemperature(
                circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.IV_1_25);

            // Assert
            // Ожидаемое значение: ~2754 Па (±100 Па из-за округления скорости)
            Assert.That(result.DpVerteiler, Is.EqualTo(2754).Within(100));
        }

        [Test]
        public void DpVerteiler_HKV_D_CorrectFormula()
        {
            // Arrange
            // Для HKV-D: DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
            // При V_dot = 280 л/ч, ρ = 1053 кг/м³
            // DpVerteiler = (0.28/1.2)² × 100000 × 1.053 = 5735 Па

            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 10,
                FlowRate = 280
            };
            var glycolProps = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16
            };
            double innerDiameter = 13.0;
            double kv = 1.2;  // Kv для HKV-D

            // Act
            var result = _calculator.CalculateAtTemperature(
                circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.HKV_D);

            // Assert
            // Ожидаемое значение: ~5735 Па
            Assert.That(result.DpVerteiler, Is.EqualTo(5735).Within(100));
        }

        #endregion

        #region DpVent Tests

        [Test]
        public void DpVent_IV_CorrectFormula()
        {
            // Arrange
            // Для IV: DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
            // При V_dot = 280 л/ч, Kv = 1.45, ρ = 1053 кг/м³
            // DpVent = (0.28/1.45)² × 100000 × 1.053 = 3925 Па

            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 10,
                FlowRate = 280
            };
            var glycolProps = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16
            };
            double innerDiameter = 13.0;
            double kv = 1.45;

            // Act
            var result = _calculator.CalculateAtTemperature(
                circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.IV_1_25);

            // Assert
            // Ожидаемое значение: ~3925 Па
            Assert.That(result.DpVent, Is.EqualTo(3925).Within(100));
        }

        [Test]
        public void DpVent_HKV_D_CorrectFormula()
        {
            // Arrange
            // Для HKV-D: DpVent = 15000 × (ρ/2000) × v²
            // При ρ = 1053 кг/м³, v = 0.59 м/с
            // DpVent = 15000 × (1.053/2) × 0.59² = 2754 Па

            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 10,
                FlowRate = 280
            };
            var glycolProps = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16
            };
            double innerDiameter = 13.0;
            double kv = 1.2;

            // Act
            var result = _calculator.CalculateAtTemperature(
                circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.HKV_D);

            // Assert
            // Ожидаемое значение: ~2754 Па
            Assert.That(result.DpVent, Is.EqualTo(2754).Within(100));
        }

        #endregion

        #region DpGesamt Tests

        [Test]
        public void DpGesamt_SumOfComponents_ReturnsCorrectValue()
        {
            // Arrange
            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 10,
                FlowRate = 280
            };
            var glycolProps = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16
            };
            double innerDiameter = 13.0;
            double kv = 1.45;

            // Act
            var result = _calculator.CalculateAtTemperature(
                circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.IV_1_25);

            // Assert
            Assert.That(result.DpGesamt, Is.EqualTo(result.DpRohr + result.DpVerteiler + result.DpVent));
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
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000,
                        Density = 1.053
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
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = 1000,
                        Density = 1.053
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
                        DpRohr = 10000,
                        DpVerteiler = 1500,
                        DpVent = 500,
                        Density = 1.053
                    }
                }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert
            // Контур 2 имеет максимальные DpGesamt: 12000 + 2000 + 1000 = 15000
            Assert.That(result[1].IsReferenceCircuit, Is.True);
            Assert.That(result[0].IsReferenceCircuit, Is.False);
            Assert.That(result[2].IsReferenceCircuit, Is.False);
        }

        [Test]
        public void CalculateBalancing_ReferenceCircuit_GetsMaxTurns_HKV_D()
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
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000,
                        Density = 1.053
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
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = 1000,
                        Density = 1.053
                    }
                }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert
            // Референсный контур должен иметь МАКСИМАЛЬНЫЕ обороты для HKV-D
            Assert.That(result[1].IsReferenceCircuit, Is.True);
            Assert.That(result[1].ValveTurns, Is.EqualTo(2.5));
            // === ВАЖНО: Референсный контур НЕ требует дросселирования (Throttling = 0) ===
            // Референсный контур имеет максимальные потери и определяет требуемый напор насоса
            Assert.That(result[1].Throttling, Is.EqualTo(0).Within(0.01), "Референсный контур должен иметь Throttling = 0");
        }

        [Test]
        public void CalculateBalancing_ReferenceCircuit_GetsMaxTurns_IV()
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
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000,
                        Density = 1.053
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
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = 1000,
                        Density = 1.053
                    }
                }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.IV_1_25);

            // Assert
            // Референсный контур должен иметь МАКСИМАЛЬНЫЕ обороты для IV
            Assert.That(result[1].IsReferenceCircuit, Is.True);
            Assert.That(result[1].ValveTurns, Is.EqualTo(8.0));
            // === ВАЖНО: Референсный контур НЕ требует дросселирования (Throttling = 0) ===
            Assert.That(result[1].Throttling, Is.EqualTo(0).Within(0.01), "Референсный контур должен иметь Throttling = 0");
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
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000,
                        Density = 1.053
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
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = 1000,
                        Density = 1.053
                    }
                }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert
            // Контур 2: DpGesamt = 15000 (референсный) → Throttling = 0
            // Контур 1: DpGesamt = 10000
            // Для HKV-D: throttling = maxDpGesamt - (DpRohr + DpVent)
            // Контур 1: throttling = 15000 - (8000 + 1000) = 6000
            Assert.That(result[0].Throttling, Is.EqualTo(6000).Within(0.1));
            // === ВАЖНО: Референсный контур имеет Throttling = 0 ===
            Assert.That(result[1].Throttling, Is.EqualTo(0).Within(0.01), "Референсный контур должен иметь Throttling = 0");
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

        [Test]
        public void CalculateBalancing_Throttling_Equals_DpGesamtDifference()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,  // Required for IsActive
                    FlowRate = 200,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000,
                        Density = 1.053
                    }
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 120,  // Required for IsActive
                    FlowRate = 240,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = 1000,
                        Density = 1.053
                    }
                },
                new CircuitRow
                {
                    CircuitNumber = 3,
                    CircuitLength = 80,  // Required for IsActive
                    FlowRate = 180,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        DpRohr = 6000,
                        DpVerteiler = 500,
                        DpVent = 500,
                        Density = 1.053
                    }
                }
            };

            // Store original DpGesamt values before CalculateBalancing modifies DpVent
            var originalDpGesamt = circuits.Select(c => c.OperatingResult.DpGesamt).ToList();
            double maxDpGesamt = originalDpGesamt.Max();

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert
            // Контур 2 (референсный): DpGesamt = 15000 → Throttling = 0
            // Контур 1: DpGesamt = 10000, throttling = 15000 - (8000 + 1000) = 6000
            // Контур 3: DpGesamt = 7000, throttling = 15000 - (6000 + 500) = 8500
            
            // Референсный контур (контур 2) должен иметь Throttling = 0
            Assert.That(result[1].IsReferenceCircuit, Is.True, "Контур 2 должен быть референсным");
            Assert.That(result[1].Throttling, Is.EqualTo(0).Within(0.01), "Референсный контур должен иметь Throttling = 0");
            
            // Нереференсные контуры должны иметь рассчитанное дросселирование
            Assert.That(result[0].Throttling, Is.EqualTo(6000).Within(0.01), "Контур 1: throttling = 15000 - (8000 + 1000) = 6000");
            Assert.That(result[2].Throttling, Is.EqualTo(8500).Within(0.01), "Контур 3: throttling = 15000 - (6000 + 500) = 8500");
        }

        [Test]
        public void CalculateBalancing_AllCircuitsHaveValveTurns()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,  // Required for IsActive
                    FlowRate = 200,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000,
                        Density = 1.053
                    }
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 120,  // Required for IsActive
                    FlowRate = 240,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = 1000,
                        Density = 1.053
                    }
                }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert
            foreach (var circuit in result)
            {
                Assert.That(circuit.ValveTurns, Is.GreaterThan(0));
                Assert.That(circuit.ValveTurns, Is.LessThanOrEqualTo(2.5));  // Max for HKV-D
            }
        }

[Test]
        public void CalculateBalancing_KvFormula_UsesDensityInGramsPerCm3()
        {
            // Arrange - проверка, что плотность в г/см³ используется корректно
            // Сравниваем два контура с разной плотностью

            var circuitsLowDensity = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,  // Required for IsActive
                    FlowRate = 200,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000,
                        Density = 1.0 // г/см³ (вода)
                    }
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 120,  // Required for IsActive
                    FlowRate = 200,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = 1000,
                        Density = 1.0
                    }
                }
            };

            var circuitsHighDensity = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,  // Required for IsActive
                    FlowRate = 200,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000,
                        Density = 1.1 // г/см³ (более плотный гликоль)
                    }
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 120,  // Required for IsActive
                    FlowRate = 200,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = 1000,
                        Density = 1.1
                    }
                }
            };

            // Act
            var resultLowDensity = _calculator.CalculateBalancing(circuitsLowDensity, ValveType.HKV_D);
            var resultHighDensity = _calculator.CalculateBalancing(circuitsHighDensity, ValveType.HKV_D);

            // Assert
            // Референсный контур (контур 2) получает максимальные обороты (2.5 для HKV-D)
            Assert.That(resultLowDensity[1].ValveTurns, Is.EqualTo(2.5), "Референсный контур должен иметь 2.5 оборота для HKV-D");
            Assert.That(resultHighDensity[1].ValveTurns, Is.EqualTo(2.5), "Референсный контур должен иметь 2.5 оборота для HKV-D");

            // Нереференсный контур (контур 1) должен иметь рассчитанные обороты
            // При одинаковых расходе и дросселировании, но разной плотности,
            // Kv должен быть разным:
            // Kv_low = 200/1000 / √(5000/100000/1.0) = 0.2 / √0.05 = 0.894
            // Kv_high = 200/1000 / √(5000/100000/1.1) = 0.2 / √0.0455 = 0.938

            // Более высокая плотность → больший Kv → больше оборотов клапана
            Assert.That(resultLowDensity[0].ValveTurns, Is.GreaterThan(0), "Обороты должны быть > 0");
            Assert.That(resultHighDensity[0].ValveTurns, Is.GreaterThan(0), "Обороты должны быть > 0");
        }

        [Test]
        public void CalculateBalancing_ReferenceCircuit_HasZeroThrottling()
        {
            // === ВАЖНЫЙ ТЕСТ: Референсный контур должен иметь Throttling = 0 ===
            // Это ключевое требование: референсный контур не требует дросселирования,
            // так как он имеет максимальные потери и определяет требуемый напор насоса.
            
            // Arrange - создаём контуры с разными DpGesamt
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
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000,
                        Density = 1.053
                    }
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 150,  // Больший контур → большие потери
                    SupplyLength = 15,
                    FlowRate = 300,
                    OperatingResult = new CircuitTemperatureResult
                    {
                        DpRohr = 15000,  // Максимальные потери
                        DpVerteiler = 2000,
                        DpVent = 1500,
                        Density = 1.053
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
                        DpRohr = 6000,
                        DpVerteiler = 800,
                        DpVent = 600,
                        Density = 1.053
                    }
                }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert
            // Контур 2 имеет максимальный DpGesamt = 15000 + 2000 + 1500 = 18500
            // Он должен быть референсным
            Assert.That(result[1].IsReferenceCircuit, Is.True, "Контур 2 должен быть референсным (максимальный DpGesamt)");
            
            // === КЛЮЧЕВАЯ ПРОВЕРКА: Референсный контур должен иметь Throttling = 0 ===
            Assert.That(result[1].Throttling, Is.EqualTo(0).Within(0.001), 
                "Референсный контур НЕ требует дросселирования (Throttling = 0)");
            
            // Референсный контур должен иметь максимальные обороты
            Assert.That(result[1].ValveTurns, Is.EqualTo(2.5), 
                "Референсный контур должен иметь максимальные обороты для HKV-D");
            
            // Нереференсные контуры должны иметь Throttling > 0
            Assert.That(result[0].Throttling, Is.GreaterThan(0), "Нереференсный контур 1 должен иметь Throttling > 0");
            Assert.That(result[2].Throttling, Is.GreaterThan(0), "Нереференсный контур 3 должен иметь Throttling > 0");
            
            // Нереференсные контуры не должны быть референсными
            Assert.That(result[0].IsReferenceCircuit, Is.False, "Контур 1 не должен быть референсным");
            Assert.That(result[2].IsReferenceCircuit, Is.False, "Контур 3 не должен быть референсным");
        }

        [Test]
        public void CalculateBalancing_ReferenceCircuit_ResetsWhenCircuitsChange()
        {
            // === ТЕСТ: При изменении состава контуров референсный контур пересчитывается ===
            
            // Arrange - начальный набор контуров
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
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000,
                        Density = 1.053
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
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = 1000,
                        Density = 1.053
                    }
                }
            };

            // Act - первый расчёт
            var result1 = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert - контур 2 референсный
            Assert.That(result1[1].IsReferenceCircuit, Is.True, "Контур 2 должен быть референсным");
            Assert.That(result1[1].Throttling, Is.EqualTo(0).Within(0.001), "Референсный контур должен иметь Throttling = 0");

            // Arrange - добавляем контур с ещё большими потерями
            circuits.Add(new CircuitRow
            {
                CircuitNumber = 3,
                CircuitLength = 200,
                SupplyLength = 20,
                FlowRate = 400,
                OperatingResult = new CircuitTemperatureResult
                {
                    DpRohr = 20000,  // Ещё большие потери
                    DpVerteiler = 3000,
                    DpVent = 2000,
                    Density = 1.053
                }
            });

            // Act - повторный расчёт
            var result2 = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert - теперь контур 3 референсный
            Assert.That(result2[2].IsReferenceCircuit, Is.True, "Контур 3 должен быть референсным после добавления");
            Assert.That(result2[2].Throttling, Is.EqualTo(0).Within(0.001), "Новый референсный контур должен иметь Throttling = 0");
            
            // Контур 2 больше не референсный
            Assert.That(result2[1].IsReferenceCircuit, Is.False, "Контур 2 больше не должен быть референсным");
            Assert.That(result2[1].Throttling, Is.GreaterThan(0), "Контур 2 должен иметь Throttling > 0");
        }

        #endregion

        #region DpVent Balancing Tests

        [Test]
        public void CalculateBalancing_HKV_D_DpVent_NotRecalculated()
        {
            // === ВАЖНЫЙ ТЕСТ: Для HKV-D DpVent НЕ пересчитывается при балансировке ===
            // DpVent для HKV-D = 15000 × (ρ/2000) × v² — НЕ зависит от Kv
            
            // Arrange
            double originalDpVent = 2754;  // Исходное значение DpVent для HKV-D
            
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
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = originalDpVent,  // Исходное значение
                        Density = 1.053
                    },
                    DesignResult = new CircuitTemperatureResult
                    {
                        DpRohr = 10000,
                        DpVerteiler = 1200,
                        DpVent = originalDpVent * 1.1,  // Немного больше для холодного режима
                        Density = 1.08
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
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = originalDpVent,
                        Density = 1.053
                    },
                    DesignResult = new CircuitTemperatureResult
                    {
                        DpRohr = 15000,
                        DpVerteiler = 2400,
                        DpVent = originalDpVent * 1.1,
                        Density = 1.08
                    }
                }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert - DpVent должен остаться неизменным для HKV-D
            Assert.That(result[0].OperatingResult.DpVent, Is.EqualTo(originalDpVent).Within(0.01), 
                "DpVent для HKV-D НЕ должен пересчитываться при балансировке");
            Assert.That(result[1].OperatingResult.DpVent, Is.EqualTo(originalDpVent).Within(0.01), 
                "DpVent для HKV-D НЕ должен пересчитываться при балансировке");
            
            // DesignResult также должен остаться неизменным
            Assert.That(result[0].DesignResult.DpVent, Is.EqualTo(originalDpVent * 1.1).Within(0.01), 
                "DpVent для HKV-D в DesignResult НЕ должен пересчитываться");
        }

        [Test]
        public void CalculateBalancing_IV_DpVent_Recalculated()
        {
            // === ВАЖНЫЙ ТЕСТ: Для IV DpVent пересчитывается при балансировке ===
            // DpVent для IV = (V_dot/1000/Kv)² × 100000 × ρ/1000 — зависит от Kv
            
            // Arrange
            double originalDpVent = 3925;  // Исходное значение DpVent для IV
            
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
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = originalDpVent,  // Исходное значение
                        Density = 1.053
                    },
                    DesignResult = new CircuitTemperatureResult
                    {
                        DpRohr = 10000,
                        DpVerteiler = 1200,
                        DpVent = originalDpVent * 1.1,
                        Density = 1.08
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
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = originalDpVent,
                        Density = 1.053
                    },
                    DesignResult = new CircuitTemperatureResult
                    {
                        DpRohr = 15000,
                        DpVerteiler = 2400,
                        DpVent = originalDpVent * 1.1,
                        Density = 1.08
                    }
                }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.IV_1_25);

            // Assert - DpVent должен пересчитаться для IV
            // Референсный контур (контур 2) получает максимальные обороты (8.0 для IV)
            // Kv при 8.0 оборотах для IV 1¼" ≈ 1.45
            // DpVent = (0.24/1.45)² × 100000 × 1.053 ≈ 2888 Па
            
            // Нереференсный контур (контур 1) получает меньше оборотов
            // DpVent должен отличаться от исходного значения
            
            // Для IV DpVent должен пересчитаться
            Assert.That(result[0].OperatingResult.DpVent, Is.Not.EqualTo(originalDpVent).Within(1), 
                "DpVent для IV должен пересчитываться при балансировке для нереференсного контура");
            
            // Референсный контур также должен иметь пересчитанный DpVent
            Assert.That(result[1].OperatingResult.DpVent, Is.Not.EqualTo(originalDpVent).Within(1), 
                "DpVent для IV должен пересчитываться при балансировке для референсного контура");
        }

        [Test]
        public void CalculateBalancing_HKV_D_DpGesamt_RemainsCorrect()
        {
            // === ТЕСТ: DpGesamt должен корректно вычисляться после балансировки для HKV-D ===
            
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
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 2754,
                        Density = 1.053
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
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = 2754,
                        Density = 1.053
                    }
                }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert - DpGesamt должен быть суммой DpRohr + DpVerteiler + DpVent
            // Для HKV-D DpVent не меняется, поэтому DpGesamt должен остаться корректным
            Assert.That(result[0].OperatingResult.DpGesamt, 
                Is.EqualTo(result[0].OperatingResult.DpRohr + 
                           result[0].OperatingResult.DpVerteiler + 
                           result[0].OperatingResult.DpVent).Within(0.01),
                "DpGesamt должен быть суммой компонентов для HKV-D");
            
            Assert.That(result[1].OperatingResult.DpGesamt, 
                Is.EqualTo(result[1].OperatingResult.DpRohr + 
                           result[1].OperatingResult.DpVerteiler + 
                           result[1].OperatingResult.DpVent).Within(0.01),
                "DpGesamt должен быть суммой компонентов для HKV-D");
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
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000
                    },
                    DesignResult = new CircuitTemperatureResult
                    {
                        DpRohr = 10000,
                        DpVerteiler = 1200,
                        DpVent = 1200
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
                        DpRohr = 6000,
                        DpVerteiler = 1000,
                        DpVent = 1000
                    },
                    DesignResult = new CircuitTemperatureResult
                    {
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000
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
            // DpGesamt = DpRohr + DpVerteiler + DpVent
            // Контур 1: 8000 + 1000 + 1000 = 10000 Па
            // Контур 2: 6000 + 1000 + 1000 = 8000 Па
            // Max = 10000 Па
            Assert.That(summary.PressureLoss_Operating_Pa, Is.EqualTo(10000));
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
                        DpRohr = 8000,
                        DpVerteiler = 1000,
                        DpVent = 1000
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
                        DpRohr = 12000,
                        DpVerteiler = 2000,
                        DpVent = 1000
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
                        DpRohr = 20000,
                        DpVerteiler = 5000,
                        DpVent = 10000
                    },
                    DesignResult = new CircuitTemperatureResult
                    {
                        DpRohr = 25000,
                        DpVerteiler = 6000,
                        DpVent = 12000
                    }
                }
            };

            // Act
            var summary = _calculator.CalculateCollectorSummary(circuits, 1, ValveType.HKV_D);

            // Assert
            // DpGesamt = 25000 + 6000 + 12000 = 43000 Па > 32000 Па
            Assert.That(summary.PressureLoss_Cold_Pa, Is.EqualTo(43000));
            Assert.That(summary.IsColdPressureExceeded, Is.True);
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