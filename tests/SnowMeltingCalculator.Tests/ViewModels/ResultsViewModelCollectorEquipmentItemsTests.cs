using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class ResultsViewModelCollectorEquipmentItemsTests
    {
        private ProjectStateService _projectStateService = null!;

        [SetUp]
        public void SetUp()
        {
            _projectStateService = new ProjectStateService();
        }

        [Test]
        public async Task CollectorEquipmentItems_GroupsSameValveTypeAndCircuitCount()
        {
            // Given
            var circuitsVm = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, circuitCount: 2),
                ResultsViewModelTestHelpers.CreateCollector(2, ValveType.HKV_D, circuitCount: 2));
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(_projectStateService, circuitsVm);

            // When
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            ResultsViewModelTestHelpers.ReplaceCollectors(circuitsVm,
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, circuitCount: 2),
                ResultsViewModelTestHelpers.CreateCollector(2, ValveType.HKV_D, circuitCount: 2));
            viewModel.LoadHydraulicsDataOnNavigate();

            // Then
            Assert.That(viewModel.CollectorEquipmentItems, Has.Count.EqualTo(1));
            var item = viewModel.CollectorEquipmentItems.Single();
            Assert.That(item.ValveType, Is.EqualTo(ValveType.HKV_D));
            Assert.That(item.CircuitCount, Is.EqualTo(2),
                "CircuitCount must stay the per-collector contour count, not the grouped quantity.");
            Assert.That(item.CollectorQuantity, Is.EqualTo(2));
        }

        [Test]
        public async Task CollectorEquipmentItems_DoesNotGroupDifferentCircuitCountsOrValveTypes()
        {
            // Given
            var circuitsVm = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, circuitCount: 2),
                ResultsViewModelTestHelpers.CreateCollector(2, ValveType.HKV_D, circuitCount: 3),
                ResultsViewModelTestHelpers.CreateCollector(3, ValveType.IV_1_25, circuitCount: 2));
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(_projectStateService, circuitsVm);

            // When
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            ResultsViewModelTestHelpers.ReplaceCollectors(circuitsVm,
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, circuitCount: 2),
                ResultsViewModelTestHelpers.CreateCollector(2, ValveType.HKV_D, circuitCount: 3),
                ResultsViewModelTestHelpers.CreateCollector(3, ValveType.IV_1_25, circuitCount: 2));
            viewModel.LoadHydraulicsDataOnNavigate();

            // Then
            Assert.That(viewModel.CollectorEquipmentItems, Has.Count.EqualTo(3));
            Assert.That(viewModel.CollectorEquipmentItems.Select(item => item.CollectorQuantity),
                Is.All.EqualTo(1));
            Assert.That(viewModel.CollectorEquipmentItems.Select(item => item.CircuitCount),
                Is.EqualTo(new[] { 2, 3, 2 }),
                "CircuitCount must remain the contour count for each first-seen collector group.");
            Assert.That(viewModel.CollectorEquipmentItems.Select(item => item.ValveType),
                Is.EqualTo(new[] { ValveType.HKV_D, ValveType.HKV_D, ValveType.IV_1_25 }));
        }

        [Test]
        public async Task CollectorEquipmentItems_GroupsMixedValveTypesWithExactQuantityAndCircuitCount()
        {
            // Given: one HKV_D collector with 4 circuits followed by three IV_1_25 collectors with 5 circuits,
            // placed so the HKV_D is first-seen to assert the documented row order.
            var firstHkvD = ResultsViewModelTestHelpers.CreateCollector(2, ValveType.HKV_D, circuitCount: 4);
            var firstIv125 = ResultsViewModelTestHelpers.CreateCollector(1, ValveType.IV_1_25, circuitCount: 5);
            var secondIv125 = ResultsViewModelTestHelpers.CreateCollector(3, ValveType.IV_1_25, circuitCount: 5);
            var thirdIv125 = ResultsViewModelTestHelpers.CreateCollector(4, ValveType.IV_1_25, circuitCount: 5);

            var circuitsVm = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                firstHkvD, firstIv125, secondIv125, thirdIv125);
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(_projectStateService, circuitsVm);

            // When
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            ResultsViewModelTestHelpers.ReplaceCollectors(circuitsVm,
                firstHkvD, firstIv125, secondIv125, thirdIv125);
            viewModel.LoadHydraulicsDataOnNavigate();

            // Then: two grouped rows in first-seen order, HKV_D first then IV_1_25.
            Assert.That(viewModel.CollectorEquipmentItems, Has.Count.EqualTo(2),
                "Mixed HKV_D and IV_1_25 must produce exactly two grouped equipment rows.");
            Assert.That(viewModel.CollectorEquipmentItems.Select(item => item.ValveType),
                Is.EqualTo(new[] { ValveType.HKV_D, ValveType.IV_1_25 }),
                "Grouped rows must preserve first-seen valve type order.");
            Assert.That(viewModel.CollectorEquipmentItems.Select(item => item.CircuitCount),
                Is.EqualTo(new[] { 4, 5 }),
                "Each grouped row must carry the per-collector circuit count, not a sum or quantity.");
            Assert.That(viewModel.CollectorEquipmentItems.Select(item => item.CollectorQuantity),
                Is.EqualTo(new[] { 1, 3 }),
                "Grouped row quantities must reflect the number of collectors merged into each first-seen group.");
            Assert.That(viewModel.CollectorEquipmentItems.Select(item => item.Type),
                Is.EqualTo(new[] { "HKV-D (4 контура)", "IV (5 контуров)" }));
        }

        [Test]
        public async Task CollectorEquipmentItems_ClearsOnResetOrEmptyState()
        {
            // Given
            var circuitsVm = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, circuitCount: 2),
                ResultsViewModelTestHelpers.CreateCollector(2, ValveType.HKV_D, circuitCount: 2));
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(_projectStateService, circuitsVm);
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            ResultsViewModelTestHelpers.ReplaceCollectors(circuitsVm,
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, circuitCount: 2),
                ResultsViewModelTestHelpers.CreateCollector(2, ValveType.HKV_D, circuitCount: 2));
            viewModel.LoadHydraulicsDataOnNavigate();
            Assert.That(viewModel.CollectorEquipmentItems, Is.Not.Empty,
                "Sanity: populated collectors must produce equipment rows before reset/empty checks.");

            // When
            viewModel.Reset();

            // Then
            Assert.That(viewModel.CollectorEquipmentItems, Is.Empty,
                "Reset must clear grouped collector equipment rows so stale rows cannot survive.");

            // Given
            var emptyViewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(
                _projectStateService,
                ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors());

            // When
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(emptyViewModel);
            emptyViewModel.LoadHydraulicsDataOnNavigate();

            // Then
            Assert.That(emptyViewModel.CollectorEquipmentItems, Is.Empty,
                "Public refresh with no collectors must leave CollectorEquipmentItems empty.");
        }
    }
}
