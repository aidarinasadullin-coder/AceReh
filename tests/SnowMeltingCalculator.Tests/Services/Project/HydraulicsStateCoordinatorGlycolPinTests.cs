using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Пин P4 (ADR-013): свойства теплоносителя фиксируются в канонический
    /// снимок тем же расчётом гидравлики — снимок == GetProperties(входы),
    /// capture-функции циркулируют через координатор из CircuitsViewModel.
    /// </summary>
    [TestFixture]
    public class HydraulicsStateCoordinatorGlycolPinTests
    {
        [Test]
        public void RunCalculation_FixesGlycolProperties_EqualToGetPropertiesByCalculationInputs()
        {
            var state = new ProjectSessionHydraulicsState();
            var session = new ProjectSession();
            var calculationStateService = new Mock<ICalculationStateService>();
            calculationStateService.SetupGet(s => s.HydraulicsValidationMessage).Returns(string.Empty);
            var coordinator = new HydraulicsStateCoordinator(state, calculationStateService.Object, new CalculationContext());

            var glycolService = new GlycolDataService();
            var inputs = new HydraulicGlobalInputsSnapshot(GlycolType.Ethylene, 50.0, 5.0, 10.0);
            const double operatingTemperature = 5.0;
            const double designTemperature = -20.0;

            coordinator.Connect(
                () => Summaries(),
                () => Summaries(),
                () => Array.Empty<HydraulicCollectorSnapshot>(),
                () => { },
                () => { },
                _ => { },
                () => GlycolPropertiesSnapshot.FromModel(glycolService.GetProperties(
                    inputs.GlycolType, inputs.GlycolConcentration, operatingTemperature)),
                () => GlycolPropertiesSnapshot.FromModel(glycolService.GetProperties(
                    inputs.GlycolType, inputs.GlycolConcentration, designTemperature)));

            coordinator.Calculate(() => Summaries());

            Assert.Multiple(() =>
            {
                Assert.That(state.Snapshot.OperatingGlycolProperties, Is.EqualTo(
                    GlycolPropertiesSnapshot.FromModel(glycolService.GetProperties(
                        inputs.GlycolType, inputs.GlycolConcentration, operatingTemperature))),
                    "снимок Operating == GetProperties(входы расчёта)");
                Assert.That(state.Snapshot.DesignGlycolProperties, Is.EqualTo(
                    GlycolPropertiesSnapshot.FromModel(glycolService.GetProperties(
                        inputs.GlycolType, inputs.GlycolConcentration, designTemperature))),
                    "снимок Design == GetProperties(входы расчёта)");
                Assert.That(state.Snapshot.Status.Phase, Is.EqualTo(HydraulicsCalculationPhase.Actual));
            });
        }

        private static List<CollectorSummary> Summaries() => new()
        {
            new CollectorSummary
            {
                CollectorNumber = 1,
                CollectorType = "HKV-D",
                CircuitCount = 1,
                TotalPipeLength = 100.0,
                TotalPower = 6700.0,
                TotalFlowRate = 320.0,
                PressureLoss_Operating_Pa = 45000.0,
                PressureLoss_Cold_Pa = 150000.0,
                Kv = 1.2
            }
        };
    }
}
