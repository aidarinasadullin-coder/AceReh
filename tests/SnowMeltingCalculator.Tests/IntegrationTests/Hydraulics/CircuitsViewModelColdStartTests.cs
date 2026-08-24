using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.Tests.Fixtures;

namespace SnowMeltingCalculator.Tests.IntegrationTests.Hydraulics
{
    /// <summary>
    /// Characterization tests for the cold-start deltaT fallback in CircuitsViewModel.
    /// </summary>
    /// <remarks>
    /// Proves that when no thermal result is available, CircuitsViewModel.Calculate()
    /// falls back to deltaT = 5.0 (supply=35.0, return=30.0) rather than the old
    /// inputs.DeltaT value of 15.0.
    /// </remarks>
    [TestFixture]
    public class CircuitsViewModelColdStartTests
    {
        private Mock<IGlycolDataService> _glycolServiceMock = null!;
        private Mock<ICircuitsValidator> _validatorMock = null!;
        private Mock<ICollectorTypeSelector> _collectorTypeSelectorMock = null!;
        private Mock<ICalculationStateService> _calculationStateServiceMock = null!;
        private Mock<IMarkDirtyService> _markDirtyServiceMock = null!;
        private CircuitsCalculator _realCircuitsCalculator = null!;
        private CalculationContext _calculationContext = null!;
        private CircuitsViewModel _viewModel = null!;

        [SetUp]
        public void Setup()
        {
            _glycolServiceMock = new Mock<IGlycolDataService>();
            _validatorMock = new Mock<ICircuitsValidator>();
            _collectorTypeSelectorMock = new Mock<ICollectorTypeSelector>();
            _calculationStateServiceMock = new Mock<ICalculationStateService>();
            _markDirtyServiceMock = new Mock<IMarkDirtyService>();

            // Real calculator so FlowRate reflects the actual deltaT used.
            _realCircuitsCalculator = new CircuitsCalculator(_glycolServiceMock.Object);

            // Cold-start context: no thermal result at all.
            _calculationContext = new CalculationContext();
            _calculationContext.UpdateClimate(new ClimateData(), "Climate");

            // Back PipeSpacing as a mutable property because the interface exposes it as read-only.
            var pipeSpacingBacking = 200;
            _calculationStateServiceMock.SetupGet(s => s.PipeSpacing).Returns(() => pipeSpacingBacking);
            _calculationStateServiceMock
                .Setup(s => s.SetPipeSpacing(It.IsAny<int>(), It.IsAny<string>()))
                .Callback<int, string>((spacing, _) =>
                {
                    pipeSpacingBacking = spacing;
                    _calculationStateServiceMock.Raise(s => s.PipeSpacingChanged += null, _calculationStateServiceMock.Object, spacing);
                });
            _calculationStateServiceMock
                .Setup(s => s.SetPipeSpacing(It.IsAny<int>()))
                .Callback<int>(spacing =>
                {
                    pipeSpacingBacking = spacing;
                    _calculationStateServiceMock.Raise(s => s.PipeSpacingChanged += null, _calculationStateServiceMock.Object, spacing);
                });

            // Glycol service returns stable properties for any temperature the fallback uses.
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005,
                    ThermalConductivity = 0.5
                });

            // Validator allows any circuit/collector manipulation.
            _validatorMock
                .Setup(v => v.CanRemoveCircuit(It.IsAny<CircuitRow>(), It.IsAny<CollectorData>()))
                .Returns((CircuitRow circuit, CollectorData collector) => collector != null && collector.Circuits.Count > 1);
            _validatorMock
                .Setup(v => v.CanRemoveCollector(It.IsAny<CollectorData>(), It.IsAny<int>()))
                .Returns((CollectorData collector, int count) => collector != null && count > 1);
            _validatorMock
                .Setup(v => v.ConfirmDeleteCircuit(It.IsAny<int>()))
                .Returns(true);
            _validatorMock
                .Setup(v => v.ConfirmDeleteCollector(It.IsAny<int>()))
                .Returns(true);

            // Collector selector always picks a small HKV-D collector.
            _collectorTypeSelectorMock
                .Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>()))
                .Returns(new CollectorSelectionResult
                {
                    CollectorType = "HKV-D (2-12 контуров)",
                    ValveType = ValveType.HKV_D,
                    Warning = null
                });

            var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(_calculationStateServiceMock.Object, _calculationContext);
            _viewModel = new CircuitsViewModel(
                _realCircuitsCalculator,
                _glycolServiceMock.Object,
                _calculationStateServiceMock.Object,
                _validatorMock.Object,
                _collectorTypeSelectorMock.Object,
                _calculationContext,
                 _markDirtyServiceMock.Object,
                 hydraulicsDependencies.Coordinator,
                  hydraulicsDependencies.Session);

            // Prepare a single active circuit with known length and no supply length.
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow
            {
                CircuitNumber = 1,
                CircuitLength = 100.0,
                SupplyLength = 0.0
            });
        }

        [Test]
        public void CircuitsViewModel_ColdStart_NoThermalResult_Uses5KDeltaTFallback()
        {
            // Arrange: assert the context has no thermal result (cold start).
            Assert.That(_calculationContext.ThermalResult, Is.Null, "Precondition: no thermal result in context");
            Assert.That(_calculationContext.IsThermalValid, Is.False, "Precondition: thermal result not valid");

            var circuit = _viewModel.Collectors[0].Circuits[0];
            const double density = 1050.0;
            const double specificHeat = 3800.0;

            // Expected power for a 100m circuit at 20 cm spacing: 100 / (100/20) * (180+80)
            const double expectedPower = 5200.0;
            const double expectedFlowRateForDeltaT5 = expectedPower * 3.6 / (density * specificHeat * 5.0) * 1000.0;
            const double expectedFlowRateForDeltaT15 = expectedPower * 3.6 / (density * specificHeat * 15.0) * 1000.0;

            // Sanity: the two expected values differ by exactly a factor of 3.
            Assert.That(expectedFlowRateForDeltaT5, Is.EqualTo(expectedFlowRateForDeltaT15 * 3.0).Within(1e-9),
                "Falsifiable: if deltaT=15 were used, FlowRate would be three times smaller");

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert: FlowRate matches deltaT=5.0 and is far from the deltaT=15.0 value.
            Assert.That(circuit.FlowRate, Is.EqualTo(expectedFlowRateForDeltaT5).Within(0.001),
                "FlowRate must match the formula using deltaT=5.0 fallback");
            Assert.That(circuit.FlowRate, Is.GreaterThan(expectedFlowRateForDeltaT15 * 2.5),
                "FlowRate must be falsifiably larger than the deltaT=15.0 value");
        }
    }
}
