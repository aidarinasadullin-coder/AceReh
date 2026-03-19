# Task 7.2: Тесты CircuitsCalculator

**Этап:** 7 - Тестирование  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 3.2 (CircuitsCalculator)

---

## 1. Цель задачи

Создать unit-тесты для `CircuitsCalculator`.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-02 | Расчёт мощности контура Q_HK | TestCalculateCircuitPower |
| UC-03 | Расчёт при двух температурах | TestCalculateAtTemperature |
| UC-04 | Расчёт потерь давления | TestCalculatePressureLoss |
| UC-05 | Балансировка контуров | TestCalculateBalancing |
| UC-06 | Подбор коллектора | TestCalculateCollectorSummary |

---

## 3. Создаваемые файлы

### 3.1. CircuitsCalculatorTests.cs

**Путь:** `tests/Services/Hydraulics/CircuitsCalculatorTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;
using Moq;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    [TestFixture]
    public class CircuitsCalculatorTests
    {
        private Mock<IGlycolDataService> _glycolServiceMock;
        private CircuitsCalculator _calculator;

        [SetUp]
        public void Setup()
        {
            _glycolServiceMock = new Mock<IGlycolDataService>();
            _glycolServiceMock
                .Setup(s => s.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1053,
                    KinematicViscosity = 2.16,
                    SpecificHeat = 3.39
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
                PipeSpacing_cm = 20 
            };
            double q_up = 256;
            double q_down = 5;

            // Act
            double power = _calculator.CalculateCircuitPower(circuit, q_up, q_down);

            // Assert
            // Формула: Q_HK = [(L_hk/(100/VA_hk)) + (L_zul/(100/VA_zul))×(q_zul/100)] × (q_up + q_down)
            Assert.That(power, Is.GreaterThan(0));
        }

        [Test]
        public void CalculateCircuitPower_ZeroLength_ReturnsZero()
        {
            // Arrange
            var circuit = new CircuitRow { CircuitLength = 0, SupplyLength = 0, PipeSpacing_cm = 20 };
            
            // Act
            double power = _calculator.CalculateCircuitPower(circuit, 256, 5);
            
            // Assert
            Assert.That(power, Is.EqualTo(0));
        }

        #endregion

        #region CalculateFlowRate Tests

        [Test]
        public void CalculateFlowRate_ReturnsCorrectValue()
        {
            // Arrange
            double power = 5000; // Вт
            double deltaT = 20; // °C
            double density = 1053; // кг/м³
            double specificHeat = 3.39; // кДж/(кг·К)

            // Act
            double flowRate = _calculator.CalculateFlowRate(power, deltaT, density, specificHeat);

            // Assert
            // Формула: V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)
            Assert.That(flowRate, Is.GreaterThan(0));
        }

        [Test]
        public void CalculateFlowRate_ZeroPower_ReturnsZero()
        {
            // Act
            double flowRate = _calculator.CalculateFlowRate(0, 20, 1053, 3.39);
            
            // Assert
            Assert.That(flowRate, Is.EqualTo(0));
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
                FlowRate = 200 
            };
            double temperature = 40;
            var glycolProps = new GlycolProperties 
            { 
                Density = 1053, 
                KinematicViscosity = 2.16,
                SpecificHeat = 3.39
            };
            double innerDiameter = 16;
            double kv = 1.2;

            // Act
            var result = _calculator.CalculateAtTemperature(circuit, temperature, glycolProps, innerDiameter, kv);

            // Assert
            Assert.That(result.Velocity, Is.GreaterThan(0));
            Assert.That(result.ReynoldsNumber, Is.GreaterThan(0));
            Assert.That(result.FrictionFactor, Is.GreaterThan(0));
            Assert.That(result.PressureLossPerMeter, Is.GreaterThan(0));
            Assert.That(result.TotalLoss, Is.GreaterThan(0));
        }

        [Test]
        public void CalculateAtTemperature_LaminarFlow_CorrectFrictionFactor()
        {
            // Arrange - низкая скорость для ламинарного режима
            var circuit = new CircuitRow { CircuitLength = 100, SupplyLength = 10, FlowRate = 50 };
            var glycolProps = new GlycolProperties { Density = 1053, KinematicViscosity = 10, SpecificHeat = 3.39 };
            
            // Act
            var result = _calculator.CalculateAtTemperature(circuit, 20, glycolProps, 16, 1.2);
            
            // Assert
            // Ламинарный режим: λ = 64 / Re
            Assert.That(result.ReynoldsNumber, Is.LessThan(2300));
            Assert.That(result.FrictionFactor, Is.EqualTo(64.0 / result.ReynoldsNumber).Within(0.01));
        }

        #endregion

        #region CalculateAllCircuits Tests

        [Test]
        public void CalculateAllCircuits_CalculatesBothTemperatures()
        {
            // Arrange
            var circuits = new List<CircuitRow> 
            { 
                new CircuitRow { CircuitLength = 100, SupplyLength = 10, PipeSpacing_cm = 20 } 
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

            // Act
            var result = _calculator.CalculateAllCircuits(circuits, inputData);

            // Assert
            Assert.That(result[0].OperatingResult, Is.Not.Null);
            Assert.That(result[0].DesignResult, Is.Not.Null);
        }

        [Test]
        public void CalculateAllCircuits_MultipleCircuits_ReturnsAll()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow { CircuitLength = 100, SupplyLength = 10, PipeSpacing_cm = 20 },
                new CircuitRow { CircuitLength = 80, SupplyLength = 8, PipeSpacing_cm = 20 },
                new CircuitRow { CircuitLength = 120, SupplyLength = 12, PipeSpacing_cm = 25 }
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

            // Act
            var result = _calculator.CalculateAllCircuits(circuits, inputData);

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
        }

        #endregion

        #region CalculateBalancing Tests

        [Test]
        public void CalculateBalancing_SetsReferenceCircuit()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow { OperatingResult = new CircuitTemperatureResult { TotalLoss = 10000 } },
                new CircuitRow { OperatingResult = new CircuitTemperatureResult { TotalLoss = 15000 } },
                new CircuitRow { OperatingResult = new CircuitTemperatureResult { TotalLoss = 12000 } }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert
            // Контур с максимальными потерями - референтный
            Assert.That(result[1].IsReferenceCircuit, Is.True);
        }

        [Test]
        public void CalculateBalancing_CalculatesThrottling()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow { OperatingResult = new CircuitTemperatureResult { TotalLoss = 10000 } },
                new CircuitRow { OperatingResult = new CircuitTemperatureResult { TotalLoss = 15000 } }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert
            // zu_drosseln = Δp_max - Δp_total
            Assert.That(result[0].Throttling, Is.EqualTo(5000));
            Assert.That(result[1].Throttling, Is.EqualTo(0));
        }

        #endregion

        #region CalculateCollectorSummary Tests

        [Test]
        public void CalculateCollectorSummary_ReturnsCorrectSummary()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow { CircuitLength = 100, SupplyLength = 10, Power = 5000, FlowRate = 200 },
                new CircuitRow { CircuitLength = 80, SupplyLength = 8, Power = 4000, FlowRate = 160 }
            };

            // Act
            var summary = _calculator.CalculateCollectorSummary(circuits, 1, ValveType.HKV_D);

            // Assert
            Assert.That(summary.CircuitCount, Is.EqualTo(2));
            Assert.That(summary.TotalPower, Is.EqualTo(9000));
            Assert.That(summary.TotalFlowRate, Is.EqualTo(360));
            Assert.That(summary.ValveType, Is.EqualTo(ValveType.HKV_D));
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

| Тест | Описание | Ожидаемый результат |
|------|----------|---------------------|
| CalculateCircuitPower | Расчёт мощности Q_HK | Положительное значение |
| CalculateFlowRate | Расчёт расхода V_dot | Положительное значение |
| CalculateAtTemperature | Расчёт при температуре | Все параметры > 0 |
| CalculateAllCircuits | Расчёт всех контуров | OperatingResult и DesignResult не null |
| CalculateBalancing | Балансировка контуров | Референтный контур определён |
| CalculateCollectorSummary | Итоги коллектора | Корректная сумма |

---

## 5. Критерии приёмки

- [ ] Файл тестов создан
- [ ] Все тесты проходят
- [ ] Покрытие кода > 90%
- [ ] Тесты для всех формул расчёта
- [ ] Тесты для граничных случаев

---

## 6. Связанные задачи

- Task 3.2: CircuitsCalculator — тестируемый класс
- Task 3.3: FlowRegimeCalculator — расчёт λ
- Task 1.2: HydraulicInputData — входные данные

---

*Дата создания: 2026-03-17*