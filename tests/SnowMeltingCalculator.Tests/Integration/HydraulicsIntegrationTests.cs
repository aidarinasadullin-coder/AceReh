using System.Collections.Generic;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;

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
            _glycolService = new GlycolDataService();
            _circuitsCalculator = new CircuitsCalculator(_glycolService);
        }

        [Test]
        public void FullCalculation_WorksCorrectly()
        {
            var circuits = new List<CircuitRow>
            {
                new CircuitRow { CircuitLength = 100, SupplyLength = 10, PipeSpacing_cm = 20 },
                new CircuitRow { CircuitLength = 80, SupplyLength = 8, PipeSpacing_cm = 20 }
            };

            double powerUp = 256;
            double powerDown = 5;
            double supplyTemperature = 50;
            double returnTemperature = 30;
            double operatingTemperature = (supplyTemperature + returnTemperature) / 2.0;
            double designTemperature = -30;
            double deltaT = supplyTemperature - returnTemperature;
            double innerDiameter = 16;
            double pipeSpacing_cm = 20;

            var inputData = new HydraulicInputData
            {
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 50,
                ValveType = ValveType.HKV_D
            };

            var result = _circuitsCalculator.CalculateAllCircuits(circuits, inputData, pipeSpacing_cm, powerUp, powerDown, operatingTemperature, designTemperature, deltaT, innerDiameter);
            var balanced = _circuitsCalculator.CalculateBalancing(result, ValveType.HKV_D);
            var summary = _circuitsCalculator.CalculateCollectorSummary(balanced, 1, ValveType.HKV_D);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(summary.CircuitCount, Is.EqualTo(2));
            Assert.That(summary.TotalPower, Is.GreaterThan(0));
            Assert.That(summary.TotalFlowRate, Is.GreaterThan(0));
        }

        [Test]
        public void Integration_WithGlycolService()
        {
            double powerUp = 300;
            double powerDown = 10;
            double supplyTemperature = 60;
            double returnTemperature = 40;
            double operatingTemperature = (supplyTemperature + returnTemperature) / 2.0;
            double designTemperature = -35;
            double deltaT = supplyTemperature - returnTemperature;
            double innerDiameter = 16;

            var inputData = new HydraulicInputData
            {
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 40,
                ValveType = ValveType.IV_1_25
            };

            var glycolProps = _glycolService.GetProperties(
                inputData.GlycolType,
                inputData.GlycolConcentration,
                operatingTemperature);

            Assert.That(glycolProps.Density, Is.GreaterThan(1000));
            Assert.That(glycolProps.KinematicViscosity, Is.GreaterThan(0));
        }

        [Test]
        public void Integration_MultipleCollectors()
        {
            double powerUp = 256;
            double powerDown = 5;
            double supplyTemperature = 50;
            double returnTemperature = 30;
            double operatingTemperature = (supplyTemperature + returnTemperature) / 2.0;
            double designTemperature = -30;
            double deltaT = supplyTemperature - returnTemperature;
            double innerDiameter = 16;
            double pipeSpacing_cm = 20;

            var inputData = new HydraulicInputData
            {
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 50,
                ValveType = ValveType.HKV_D
            };

            var collector1Circuits = new List<CircuitRow>
            {
                new CircuitRow { CircuitNumber = 1, CircuitLength = 100, SupplyLength = 10, PipeSpacing_cm = 20 },
                new CircuitRow { CircuitNumber = 2, CircuitLength = 90, SupplyLength = 9, PipeSpacing_cm = 20 }
            };

            var collector2Circuits = new List<CircuitRow>
            {
                new CircuitRow { CircuitNumber = 1, CircuitLength = 110, SupplyLength = 11, PipeSpacing_cm = 20 },
                new CircuitRow { CircuitNumber = 2, CircuitLength = 85, SupplyLength = 8, PipeSpacing_cm = 20 }
            };

            var result1 = _circuitsCalculator.CalculateAllCircuits(collector1Circuits, inputData, pipeSpacing_cm, powerUp, powerDown, operatingTemperature, designTemperature, deltaT, innerDiameter);
            var balanced1 = _circuitsCalculator.CalculateBalancing(result1, inputData.ValveType);
            var summary1 = _circuitsCalculator.CalculateCollectorSummary(balanced1, 1, inputData.ValveType);

            var result2 = _circuitsCalculator.CalculateAllCircuits(collector2Circuits, inputData, pipeSpacing_cm, powerUp, powerDown, operatingTemperature, designTemperature, deltaT, innerDiameter);
            var balanced2 = _circuitsCalculator.CalculateBalancing(result2, inputData.ValveType);
            var summary2 = _circuitsCalculator.CalculateCollectorSummary(balanced2, 2, inputData.ValveType);

            Assert.That(summary1.CircuitCount, Is.EqualTo(2));
            Assert.That(summary2.CircuitCount, Is.EqualTo(2));
            Assert.That(summary1.TotalPower, Is.GreaterThan(0));
            Assert.That(summary2.TotalPower, Is.GreaterThan(0));
            Assert.That(summary1.TotalFlowRate, Is.GreaterThan(0));
            Assert.That(summary2.TotalFlowRate, Is.GreaterThan(0));
        }

        [Test]
        public void Integration_Balancing()
        {
            var circuits = new List<CircuitRow>
            {
                new CircuitRow
                {
                    CircuitNumber = 1,
                    CircuitLength = 100,
                    SupplyLength = 10,
                    PipeSpacing_cm = 20,
                    SupplySpacing_cm = 5,
                    SupplyHeatPercent = 10
                },
                new CircuitRow
                {
                    CircuitNumber = 2,
                    CircuitLength = 150,
                    SupplyLength = 15,
                    PipeSpacing_cm = 20,
                    SupplySpacing_cm = 5,
                    SupplyHeatPercent = 10
                },
                new CircuitRow
                {
                    CircuitNumber = 3,
                    CircuitLength = 80,
                    SupplyLength = 8,
                    PipeSpacing_cm = 20,
                    SupplySpacing_cm = 5,
                    SupplyHeatPercent = 10
                }
            };

            double powerUp = 256;
            double powerDown = 5;
            double supplyTemperature = 50;
            double returnTemperature = 30;
            double operatingTemperature = (supplyTemperature + returnTemperature) / 2.0;
            double designTemperature = -30;
            double deltaT = supplyTemperature - returnTemperature;
            double innerDiameter = 16;
            double pipeSpacing_cm = 20;

            var inputData = new HydraulicInputData
            {
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 50,
                ValveType = ValveType.HKV_D
            };

            var calculated = _circuitsCalculator.CalculateAllCircuits(circuits, inputData, pipeSpacing_cm, powerUp, powerDown, operatingTemperature, designTemperature, deltaT, innerDiameter);
            var balanced = _circuitsCalculator.CalculateBalancing(calculated, inputData.ValveType);
            var summary = _circuitsCalculator.CalculateCollectorSummary(balanced, 1, inputData.ValveType);

            var referenceCircuits = new List<CircuitRow>();
            foreach (var c in balanced)
            {
                if (c.IsReferenceCircuit)
                    referenceCircuits.Add(c);
            }

            Assert.That(referenceCircuits.Count, Is.EqualTo(1), "Должен быть один референсный контур");

            var reference = referenceCircuits[0];
            // Для референсного контура throttling = DpVerteiler (для HKV-D) или DpVent (для IV)
            // Это не равно 0, потому что throttling = maxDpGesamt - (DpRohr + DpVent) = DpVerteiler
            Assert.That(reference.Throttling, Is.GreaterThanOrEqualTo(0), "У референсного контура дросселирование >= 0");

            foreach (var circuit in balanced)
            {
                if (!circuit.IsReferenceCircuit)
                {
                    Assert.That(circuit.Throttling, Is.GreaterThan(0), "У нереференсных контуров дросселирование > 0");
                }
            }

            Assert.That(summary.CircuitCount, Is.EqualTo(3));
            Assert.That(summary.TotalPower, Is.GreaterThan(0));
        }
    }
}