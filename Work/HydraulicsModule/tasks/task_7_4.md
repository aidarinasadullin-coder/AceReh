# Task 7.4: Интеграционные тесты

**Этап:** 7 - Тестирование  
**Приоритет:** Средний  
**Статус:** К разработке  
**Зависимости:** Task 6.1, Task 6.2, Task 6.3

---

## 1. Цель задачи

Создать интеграционные тесты для полного цикла расчёта.

---

## 2. Создаваемые файлы

### 7.4. HydraulicsIntegrationTests.cs

**Путь:** `tests/Integration/HydraulicsIntegrationTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Tests.Integration
{
    [TestFixture]
    public class HydraulicsIntegrationTests
    {
        private IGlycolDataService _glycolService;
        private ICircuitsCalculator _circuitsCalculator;

        [SetUp]
        public void Setup()
        {
            _glycolService = new GlycolDataService("data/glycol_data.json");
            _circuitsCalculator = new CircuitsCalculator(_glycolService);
        }

        [Test]
        public void FullCalculation_WorksCorrectly()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow { CircuitLength = 100, SupplyLength = 10, PipeSpacing_cm = 20 },
                new CircuitRow { CircuitLength = 80, SupplyLength = 8, PipeSpacing_cm = 20 }
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
            var result = _circuitsCalculator.CalculateAllCircuits(circuits, inputData);
            var balanced = _circuitsCalculator.CalculateBalancing(result, ValveType.HKV_D);
            var summary = _circuitsCalculator.CalculateCollectorSummary(balanced, 1, ValveType.HKV_D);
            
            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(summary.CircuitCount, Is.EqualTo(2));
            Assert.That(summary.TotalPower, Is.GreaterThan(0));
            Assert.That(summary.TotalFlowRate, Is.GreaterThan(0));
        }

        [Test]
        public void IntegrationWithThermalModule_UpdatesData()
        {
            // Arrange
            var inputData = new HydraulicInputData
            {
                PowerUp = 300,
                PowerDown = 10,
                SupplyTemperature = 60,
                ReturnTemperature = 40,
                ColdFiveDayTemperature = -35,
                InnerDiameter = 16,
                PipeSpacing_mm = 250,
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 40,
                ValveType = ValveType.IV_1_25
            };
            
            // Act
            var glycolProps = _glycolService.GetProperties(
                inputData.GlycolType,
                inputData.GlycolConcentration,
                inputData.OperatingTemperature);
            
            // Assert
            Assert.That(glycolProps.Density, Is.GreaterThan(1000));
            Assert.That(glycolProps.KinematicViscosity, Is.GreaterThan(0));
        }

        [Test]
        public void Balancing_FindsReferenceCircuit()
        {
            // Arrange
            var circuits = new List<CircuitRow>
            {
                new CircuitRow { 
                    CircuitLength = 100, 
                    SupplyLength = 10, 
                    PipeSpacing_cm = 20,
                    OperatingResult = new CircuitTemperatureResult { TotalLoss = 10000 }
                },
                new CircuitRow { 
                    CircuitLength = 150, 
                    SupplyLength = 15, 
                    PipeSpacing_cm = 20,
                    OperatingResult = new CircuitTemperatureResult { TotalLoss = 20000 }
                }
            };
            
            // Act
            var balanced = _circuitsCalculator.CalculateBalancing(circuits, ValveType.HKV_D);
            
            // Assert
            Assert.That(balanced[1].IsReferenceCircuit, Is.True);
            Assert.That(balanced[0].Throttling, Is.EqualTo(10000));
        }
    }
}
```

---

## 3. Критерии приёмки

- [ ] Файл тестов создан
- [ ] Все тесты проходят
- [ ] End-to-end сценарий работает

---

## 4. Примечания

- Интеграционные тесты проверяют полный цикл расчёта
- Используются реальные сервисы (не mock)

---

*Дата создания: 2026-03-17*