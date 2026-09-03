using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Phase 9 slice 3 (ST-026): Results owns its circuit projection rows,
    /// reconstructed from the canonical HydraulicsState snapshot. The module
    /// CircuitsViewModel rows are neither referenced nor mutated by the
    /// Results projection rebuild (negative ownership probe).
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class ResultsOwnedCircuitProjectionTests
    {
        private ProjectStateService _projectStateService = null!;

        [SetUp]
        public void SetUp()
        {
            _projectStateService = new ProjectStateService();
        }

        private static HydraulicCircuitResultSnapshot CreateResult(
            double power, double flowRate, double dpRohr, double dpVerteiler, double dpVent) =>
            new(power, flowRate, velocity: 0.4, dpRohr, dpVerteiler, dpVent,
                dpGesamt: dpRohr + dpVerteiler + dpVent, throttling: 120.0, valveTurns: 2.5,
                density: 1.05, kinematicViscosity: 4.2, reynoldsNumber: 8500.0,
                frictionFactor: 0.03, pressureLossPerMeter: 250.0, FlowRegime.Turbulent);

        private static HydraulicCollectorSnapshot CreateCanonicalCollectorWithResults(int number) =>
            new(number, "HKV-D (2-12 контуров)", ValveType.HKV_D,
                new[]
                {
                    new HydraulicCircuitSnapshot(1, 50, 10, 5, 10, 20,
                        CreateResult(1200.0, 300.0, 1500.0, 400.0, 600.0),
                        CreateResult(900.0, 250.0, 1800.0, 450.0, 650.0)),
                    new HydraulicCircuitSnapshot(2, 60, 12, 5, 10, 20,
                        CreateResult(1400.0, 350.0, 1600.0, 420.0, 620.0),
                        CreateResult(1000.0, 280.0, 1900.0, 470.0, 670.0))
                },
                new HydraulicCollectorSummarySnapshot(2, 132.0, 2600.0, 0.65, 2500.0, 2700.0, 4.5, "HKV-D"));

        [Test]
        public async Task UpdateCircuitsFilter_ReconstructsResultsOwnedRows_FromCanonicalSnapshot()
        {
            // Given
            var circuitsVm = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, circuitCount: 2));
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(_projectStateService, circuitsVm);
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);

            _projectStateService.Session.HydraulicsState.ReplaceCollectors(
                new[] { CreateCanonicalCollectorWithResults(1) },
                HydraulicsMutationOrigin.Calculation);

            // When
            viewModel.LoadHydraulicsDataOnNavigate();

            // Then — строки восстановлены из канонического снапшота вместе с результатами
            Assert.That(viewModel.Circuits, Has.Count.EqualTo(2));
            var first = viewModel.Circuits[0];
            Assert.That(first.CircuitNumber, Is.EqualTo(1));
            Assert.That(first.CircuitLength, Is.EqualTo(50));
            Assert.That(first.SupplyLength, Is.EqualTo(10));
            Assert.That(first.TotalLength, Is.EqualTo(60));
            Assert.That(first.Power, Is.EqualTo(1200.0));
            Assert.That(first.FlowRate, Is.EqualTo(300.0));
            Assert.That(first.OperatingResult!.DpRohr, Is.EqualTo(1500.0));
            Assert.That(first.OperatingResult.DpVerteiler, Is.EqualTo(400.0));
            Assert.That(first.OperatingResult.DpVent, Is.EqualTo(600.0));
            Assert.That(first.OperatingResult.DpGesamt, Is.EqualTo(2500.0));
            Assert.That(first.DesignResult!.DpRohr, Is.EqualTo(1800.0));
            Assert.That(first.Throttling, Is.EqualTo(120.0));
            Assert.That(first.ValveTurns, Is.EqualTo(2.5));
            // Режим отображения по умолчанию — рабочий
            Assert.That(first.DisplayMode, Is.EqualTo(HydraulicMode.OperatingTemperature));
        }

        [Test]
        public async Task UpdateCircuitsFilter_RowsAreNotSharedWithModuleViewModel()
        {
            // Given
            var circuitsVm = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, circuitCount: 2),
                ResultsViewModelTestHelpers.CreateCollector(2, ValveType.HKV_D, circuitCount: 2));
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(_projectStateService, circuitsVm);
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            ResultsViewModelTestHelpers.ReplaceCollectorsCanonical(_projectStateService.Session, circuitsVm,
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, circuitCount: 2),
                ResultsViewModelTestHelpers.CreateCollector(2, ValveType.HKV_D, circuitCount: 2));

            var moduleRows = circuitsVm.Collectors.SelectMany(c => c.Circuits ?? new System.Collections.ObjectModel.ObservableCollection<CircuitRow>())
                .ToList();
            var moduleStateBefore = moduleRows.Select(row => (row.CircuitNumber, row.CircuitLength, row.DisplayMode)).ToList();

            // When
            viewModel.LoadHydraulicsDataOnNavigate();

            // Then — негативный проб владения: ни одна строка Results не является
            // объектом модуля; состояние модульных строк не изменилось.
            Assert.That(viewModel.Circuits, Has.Count.EqualTo(2));
            foreach (var resultsRow in viewModel.Circuits)
            {
                Assert.That(moduleRows.Any(moduleRow => ReferenceEquals(moduleRow, resultsRow)), Is.False,
                    "Results must not share CircuitRow instances with the module ViewModel.");
            }
            var moduleStateAfter = moduleRows.Select(row => (row.CircuitNumber, row.CircuitLength, row.DisplayMode)).ToList();
            Assert.That(moduleStateAfter, Is.EqualTo(moduleStateBefore),
                "The Results projection rebuild must not mutate module CircuitRow objects.");
        }

        [Test]
        public async Task ToggleMode_WritesDisplayModeOnlyOnResultsOwnedRows()
        {
            // Given
            var circuitsVm = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, circuitCount: 2));
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(_projectStateService, circuitsVm);
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            ResultsViewModelTestHelpers.ReplaceCollectorsCanonical(_projectStateService.Session, circuitsVm,
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, circuitCount: 2));
            viewModel.LoadHydraulicsDataOnNavigate();

            // When
            viewModel.ToggleModeCommand.Execute(null);

            // Then — режим меняется только на Results-owned копиях
            Assert.That(viewModel.Circuits, Has.Count.EqualTo(2));
            Assert.That(viewModel.Circuits.All(row => row.DisplayMode == HydraulicMode.DesignTemperature), Is.True);
            var moduleRows = circuitsVm.Collectors.SelectMany(c => c.Circuits ?? new System.Collections.ObjectModel.ObservableCollection<CircuitRow>());
            Assert.That(moduleRows.All(row => row.DisplayMode == HydraulicMode.OperatingTemperature), Is.True,
                "The module rows must keep their own display mode; Results must not write into them.");
        }
    }
}
