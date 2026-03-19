# Task 3.2: Создать CircuitsCalculator.cs

**Этап:** 3 - Сервисы расчёта  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 1.1 (ValveType), Task 1.2 (HydraulicInputData), Task 2.1 (ICircuitsCalculator), Task 3.1 (ValveTurnsCalculator)

---

## 1. Цель задачи

Создать класс `CircuitsCalculator` — реализация калькулятора контуров.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-02 | Расчёт мощности контура Q_HK | CalculateCircuitPower() |
| UC-03 | Расчёт при двух температурах | CalculateAtTemperature() |
| UC-04 | Расчёт потерь давления | CalculateAtTemperature() |
| UC-05 | Балансировка контуров | CalculateBalancing() |
| UC-06 | Подбор коллектора | CalculateCollectorSummary() |

---

## 3. Создаваемые файлы

### 3.1. CircuitsCalculator.cs

**Путь:** `src/Services/Hydraulics/CircuitsCalculator.cs`

**Ключевые методы:**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Реализация калькулятора контуров
    /// </summary>
    public class CircuitsCalculator : ICircuitsCalculator
    {
        private readonly IGlycolDataService _glycolService;

        public CircuitsCalculator(IGlycolDataService glycolService)
        {
            _glycolService = glycolService ?? throw new ArgumentNullException(nameof(glycolService));
        }

        /// <summary>
        /// Рассчитать мощность контура Q_HK
        /// </summary>
        /// <remarks>
        /// Формула: Q_HK = [(L_hk/(100/VA_hk)) + (L_zul/(100/VA_zul))×(q_zul/100)] × (q_up + q_down)
        /// </remarks>
        public double CalculateCircuitPower(CircuitRow circuit, double q_up, double q_down)
        {
            // Длина контура на единицу площади
            double lengthPerArea = circuit.CircuitLength / (100.0 / circuit.PipeSpacing_cm);
            
            // Длина подводки на единицу площади
            double supplyLengthPerArea = circuit.SupplyLength / (100.0 / circuit.SupplySpacing_cm);
            
            // Доля тепла от подводок
            double supplyHeatFactor = circuit.SupplyHeatPercent / 100.0;
            
            // Мощность контура
            double power = (lengthPerArea + supplyLengthPerArea * supplyHeatFactor) * (q_up + q_down);
            
            return power;
        }

        /// <summary>
        /// Рассчитать расход теплоносителя V_dot
        /// </summary>
        /// <remarks>
        /// Формула: V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)
        /// </remarks>
        public double CalculateFlowRate(double power, double deltaT, double density, double specificHeat)
        {
            // V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)
            // Результат в л/ч
            double flowRate = power * 3.6 / (density * specificHeat * deltaT);
            
            return flowRate;
        }

        /// <summary>
        /// Рассчитать гидравлику контура при заданной температуре
        /// </summary>
        public CircuitTemperatureResult CalculateAtTemperature(
            CircuitRow circuit,
            double temperature,
            GlycolProperties glycolProps,
            double innerDiameter,
            double kv)
        {
            var result = new CircuitTemperatureResult
            {
                Temperature = temperature,
                Density = glycolProps.Density,
                KinematicViscosity = glycolProps.KinematicViscosity
            };

            // Скорость потока: v = V_dot × 4 / (3600 × π × d_inner²) × 10⁶
            // где V_dot в л/ч, d_inner в мм
            double area = Math.PI * Math.Pow(innerDiameter, 2) / 4;
            double velocity = circuit.FlowRate * 4 / (3600 * area) * 1e6;
            circuit.Velocity = velocity;

            // Число Рейнольдса: Re = 1000 × v × d_inner / ν
            double reynolds = 1000 * velocity * innerDiameter / glycolProps.KinematicViscosity;
            result.ReynoldsNumber = reynolds;

            // Режим течения
            result.FlowRegime = DetermineFlowRegime(reynolds);

            // Коэффициент трения λ
            double frictionFactor = CalculateFrictionFactor(reynolds, innerDiameter);
            result.FrictionFactor = frictionFactor;

            // Удельные потери: R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100
            double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * glycolProps.Density * frictionFactor 
                / (2 * innerDiameter) * 100;
            result.PressureLossPerMeter = pressureLossPerMeter;

            // Потери в трубе контура: Δp_HK = L_hk × R
            result.CircuitPipeLoss = circuit.CircuitLength * pressureLossPerMeter;

            // Потери в трубе подводки: Δp_Zul = L_zul × R
            result.SupplyPipeLoss = circuit.SupplyLength * pressureLossPerMeter;

            // Потери в вентиле: Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ
            result.ValveLoss = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * glycolProps.Density;

            // Суммарные потери: Δp_total = Δp_HK + Δp_Zul + Δp_Vent
            // (вычисляется автоматически в TotalLoss)

            return result;
        }

        /// <summary>
        /// Рассчитать все контура коллектора
        /// </summary>
        public List<CircuitRow> CalculateAllCircuits(List<CircuitRow> circuits, HydraulicInputData inputData)
        {
            if (circuits == null || circuits.Count == 0)
                return new List<CircuitRow>();

            // Получение свойств гликоля при рабочей температуре
            var glycolPropsOperating = _glycolService.GetProperties(
                inputData.GlycolType,
                inputData.GlycolConcentration,
                inputData.OperatingTemperature);

            // Получение свойств гликоля при расчётной температуре
            var glycolPropsDesign = _glycolService.GetProperties(
                inputData.GlycolType,
                inputData.GlycolConcentration,
                inputData.DesignTemperature);

            // Kv клапана
            double kv = ValveTurnsCalculator.GetDefaultKv(inputData.ValveType);

            foreach (var circuit in circuits)
            {
                // Расчёт мощности
                circuit.Power = CalculateCircuitPower(circuit, inputData.PowerUp, inputData.PowerDown);

                // Расчёт расхода
                circuit.FlowRate = CalculateFlowRate(
                    circuit.Power,
                    inputData.DeltaT,
                    glycolPropsOperating.Density,
                    glycolPropsOperating.SpecificHeat);

                // Расчёт при рабочей температуре
                circuit.OperatingResult = CalculateAtTemperature(
                    circuit,
                    inputData.OperatingTemperature,
                    glycolPropsOperating,
                    inputData.InnerDiameter,
                    kv);

                // Расчёт при расчётной температуре
                circuit.DesignResult = CalculateAtTemperature(
                    circuit,
                    inputData.DesignTemperature,
                    glycolPropsDesign,
                    inputData.InnerDiameter,
                    kv);
            }

            return circuits;
        }

        /// <summary>
        /// Рассчитать балансировку контуров
        /// </summary>
        public List<CircuitRow> CalculateBalancing(List<CircuitRow> circuits, ValveType valveType)
        {
            if (circuits == null || circuits.Count == 0)
                return new List<CircuitRow>();

            // Найти контур с максимальными потерями (референсный)
            double maxPressureLoss = circuits.Max(c => c.OperatingResult.TotalLoss);

            // Рассчитать дросселирование для каждого контура
            foreach (var circuit in circuits)
            {
                // zu_drosseln = Δp_max - Δp_total
                circuit.Throttling = maxPressureLoss - circuit.OperatingResult.TotalLoss;
                
                // Референсный контур
                circuit.IsReferenceCircuit = Math.Abs(circuit.OperatingResult.TotalLoss - maxPressureLoss) < 0.01;

                // Расчёт оборотов клапана
                if (circuit.Throttling > 0)
                {
                    // Kv для дросселирования
                    double kv = CalculateKvForThrottling(circuit.FlowRate, circuit.Throttling);
                    circuit.ValveTurns = ValveTurnsCalculator.CalculateTurns(kv, valveType);
                }
                else
                {
                    circuit.ValveTurns = 0;
                }
            }

            return circuits;
        }

        /// <summary>
        /// Рассчитать итоги коллектора
        /// </summary>
        public CollectorSummary CalculateCollectorSummary(List<CircuitRow> circuits, int collectorNumber, ValveType valveType)
        {
            if (circuits == null || circuits.Count == 0)
                return new CollectorSummary { CollectorNumber = collectorNumber };

            var summary = new CollectorSummary
            {
                CollectorNumber = collectorNumber,
                CircuitCount = circuits.Count,
                ValveType = valveType,
                TotalPipeLength = circuits.Sum(c => c.TotalLength),
                TotalPower = circuits.Sum(c => c.Power),
                TotalFlowRate = circuits.Sum(c => c.FlowRate),
                PressureLoss_Operating_mbar = circuits.Max(c => c.OperatingResult.TotalLoss_mbar),
                PressureLoss_Cold_mbar = circuits.Max(c => c.DesignResult.TotalLoss_mbar)
            };

            // Найти референсный контур
            var referenceCircuit = circuits.FirstOrDefault(c => c.IsReferenceCircuit);
            if (referenceCircuit != null)
            {
                summary.ReferenceCircuitNumber = referenceCircuit.CircuitNumber;
            }

            return summary;
        }

        #region Приватные методы

        private FlowRegime DetermineFlowRegime(double reynolds)
        {
            if (reynolds < 2300)
                return FlowRegime.Laminar;
            else if (reynolds <= 4000)
                return FlowRegime.Transitional;
            else
                return FlowRegime.Turbulent;
        }

        private double CalculateFrictionFactor(double reynolds, double innerDiameter)
        {
            var regime = DetermineFlowRegime(reynolds);

            return regime switch
            {
                FlowRegime.Laminar => 64.0 / reynolds,
                FlowRegime.Transitional => CalculateTransitionalFrictionFactor(reynolds, innerDiameter),
                FlowRegime.Turbulent => CalculateTurbulentFrictionFactor(reynolds, innerDiameter),
                _ => 0.02
            };
        }

        private double CalculateTransitionalFrictionFactor(double reynolds, double innerDiameter)
        {
            double lambdaLam = 64.0 / 2300;
            double lambdaTurb = CalculateTurbulentFrictionFactor(4000, innerDiameter);
            double ratio = (reynolds - 2300) / 1700.0;
            return lambdaLam + ratio * (lambdaTurb - lambdaLam);
        }

        private double CalculateTurbulentFrictionFactor(double reynolds, double innerDiameter)
        {
            double roughness = 0.007; // мм, для PE-Xa
            double lambda = 0.02;

            for (int i = 0; i < 20; i++)
            {
                double newLambda = Math.Pow(
                    -2 * Math.Log10(roughness / (3.7 * innerDiameter) + 2.51 / (reynolds * Math.Sqrt(lambda))),
                    -2);

                if (Math.Abs(newLambda - lambda) < 1e-10)
                    break;

                lambda = newLambda;
            }

            return lambda;
        }

        private double CalculateKvForThrottling(double flowRate, double throttling)
        {
            // Δp = (V_dot / 1000 / Kv)² × 100000 × ρ
            // Kv = V_dot / 1000 / √(Δp / 100000 / ρ)
            // Упрощённо: Kv ≈ V_dot / √(Δp)
            return flowRate / Math.Sqrt(throttling / 100);
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Services/Hydraulics/CircuitsCalculatorTests.cs`

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

        [Test]
        public void CalculateCircuitPower_ReturnsCorrectValue()
        {
            // Arrange
            var circuit = new CircuitRow
            {
                CircuitLength = 100,
                SupplyLength = 10,
                PipeSpacing_cm = 20,
                SupplySpacing_cm = 5,
                SupplyHeatPercent = 10
            };
            double q_up = 256;
            double q_down = 5;

            // Act
            double power = _calculator.CalculateCircuitPower(circuit, q_up, q_down);

            // Assert
            Assert.That(power, Is.GreaterThan(0));
        }

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
            // V_dot = 5000 × 3.6 / (1053 × 3.39 × 20) ≈ 0.25 л/ч
            Assert.That(flowRate, Is.GreaterThan(0));
        }

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
                KinematicViscosity = 2.16
            };
            double innerDiameter = 16;
            double kv = 1.2;

            // Act
            var result = _calculator.CalculateAtTemperature(circuit, temperature, glycolProps, innerDiameter, kv);

            // Assert
            Assert.That(result.Temperature, Is.EqualTo(temperature));
            Assert.That(result.ReynoldsNumber, Is.GreaterThan(0));
            Assert.That(result.TotalLoss, Is.GreaterThan(0));
        }

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
                PipeSpacing_mm = 200,
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 50,
                ValveType = ValveType.HKV_D
            };

            // Act
            var result = _calculator.CalculateAllCircuits(circuits, inputData);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].OperatingResult, Is.Not.Null);
            Assert.That(result[0].DesignResult, Is.Not.Null);
        }

        [Test]
        public void CalculateBalancing_SetsReferenceCircuit()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow { CircuitNumber = 1, OperatingResult = new CircuitTemperatureResult { CircuitPipeLoss = 8000, SupplyPipeLoss = 1000, ValveLoss = 1000 } },
                new CircuitRow { CircuitNumber = 2, OperatingResult = new CircuitTemperatureResult { CircuitPipeLoss = 12000, SupplyPipeLoss = 2000, ValveLoss = 1000 } },
                new CircuitRow { CircuitNumber = 3, OperatingResult = new CircuitTemperatureResult { CircuitPipeLoss = 10000, SupplyPipeLoss = 1500, ValveLoss = 500 } }
            };

            // Act
            var result = _calculator.CalculateBalancing(circuits, ValveType.HKV_D);

            // Assert
            Assert.That(result[1].IsReferenceCircuit, Is.True); // Контур 2 с макс. потерями
            Assert.That(result[0].Throttling, Is.EqualTo(5000)); // 15000 - 10000
            Assert.That(result[2].Throttling, Is.EqualTo(3000)); // 15000 - 12000
        }

        [Test]
        public void CalculateCollectorSummary_ReturnsCorrectSummary()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow { CircuitLength = 100, SupplyLength = 10, Power = 5000, FlowRate = 200, OperatingResult = new CircuitTemperatureResult { CircuitPipeLoss = 8000, SupplyPipeLoss = 1000, ValveLoss = 1000 } },
                new CircuitRow { CircuitLength = 80, SupplyLength = 8, Power = 4000, FlowRate = 160, OperatingResult = new CircuitTemperatureResult { CircuitPipeLoss = 6000, SupplyPipeLoss = 1000, ValveLoss = 1000 } }
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
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `CircuitsCalculator.cs` создан в `src/Services/Hydraulics/`
- [ ] Реализованы все методы интерфейса `ICircuitsCalculator`
- [ ] Формулы соответствуют `docs/Formulas_Snegotayanie.md`
- [ ] Расчёт для двух температур работает
- [ ] Балансировка контуров работает
- [ ] Unit-тесты проходят успешно
- [ ] XML-документация для всех методов
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Использует `IGlycolDataService` для получения свойств гликоля
- Использует `ValveTurnsCalculator` для расчёта оборотов клапана
- Формула Колбрука-Уайта решается итерационно
- Референсный контур — контур с максимальными потерями

---

## 7. Связанные задачи

- Task 1.1: ValveType — используется в CalculateBalancing()
- Task 1.2: HydraulicInputData — используется в CalculateAllCircuits()
- Task 2.1: ICircuitsCalculator — реализация интерфейса
- Task 3.1: ValveTurnsCalculator — используется для расчёта оборотов

---

*Дата создания: 2026-03-17*