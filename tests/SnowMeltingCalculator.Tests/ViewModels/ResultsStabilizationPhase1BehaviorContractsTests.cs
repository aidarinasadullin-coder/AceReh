using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public sealed class ResultsStabilizationPhase1BehaviorContractsTests
    {
        private ProjectStateService _projectStateService = null!;

        [SetUp]
        public void SetUp()
        {
            _projectStateService = new ProjectStateService();
        }

        [Test]
        public async Task RefreshAll_WhenSourceResultIsCleared_ZerosOutputAndMarksNotReady()
        {
            var viewModel = CreateReadyViewModel();
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            var thermalViewModel = GetField<ThermalViewModel>(viewModel, "_thermalViewModel");
            Assert.That(viewModel.TotalPowerDensity, Is.EqualTo(100));

            thermalViewModel.Result = null;
            viewModel.RefreshAll();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.TotalPowerDensity, Is.Zero);
                Assert.That(viewModel.SupplyTemperature, Is.Zero);
                Assert.That(viewModel.IsDataReady, Is.False);
                Assert.That(viewModel.MissingModules, Does.Contain("Тепловой расчёт - нет результата"));
            });
        }

        [Test]
        public async Task RefreshAll_WhenInputsChangeButValidResultIsRetained_PreservesOutputWithoutCalculation()
        {
            var viewModel = CreateReadyViewModel();
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            var climateViewModel = GetField<ClimateViewModel>(viewModel, "_climateViewModel");
            var thermalViewModel = GetField<ThermalViewModel>(viewModel, "_thermalViewModel");
            var calculator = GetField<IThermalCalculator>(thermalViewModel, "_calculator");
            var retainedPower = viewModel.TotalPowerDensity;

            climateViewModel.SelectedCity = new CityInfo { Name = "Новый город", Region = "Новый регион" };
            climateViewModel.AirTemperature = -31;
            viewModel.RefreshAll();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.SelectedCity, Is.EqualTo("Новый город"));
                Assert.That(viewModel.DesignTemperature, Is.EqualTo(-31));
                Assert.That(viewModel.TotalPowerDensity, Is.EqualTo(retainedPower));
            });
            Mock.Get(calculator).Verify(
                instance => instance.Calculate(
                    It.IsAny<SnowMeltingCalculator.Models.Thermal.ThermalInputs>(),
                    It.IsAny<SnowMeltingCalculator.Models.Climate.IClimateData>(),
                    It.IsAny<SnowMeltingCalculator.Models.Thermal.IConstructionData>()),
                Times.Never);
        }

        [Test]
        public async Task RefreshAll_ProjectsCollectorCircuitSpecificationsEquipmentCardsAndKpi()
        {
            var circuitsViewModel = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, 2));
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(_projectStateService, circuitsViewModel);
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            var collector = ResultsViewModelTestHelpers.CreateCollector(7, ValveType.IV_1_25, 3);
            collector.Summary = new CollectorSummary
            {
                CollectorNumber = 7,
                CircuitCount = 3,
                TotalPipeLength = 180,
                TotalPower = 12000,
                TotalFlowRate = 720,
                PressureLoss_Operating_Pa = 24000,
                Kv = 1.45,
                CollectorType = "IV"
            };
            ResultsViewModelTestHelpers.ReplaceCollectors(circuitsViewModel, collector);

            viewModel.RefreshAll();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.Collectors, Has.Count.EqualTo(1));
                Assert.That(viewModel.Circuits, Has.Count.EqualTo(3));
                Assert.That(viewModel.CollectorSpecifications, Has.Count.EqualTo(1));
                Assert.That(viewModel.CollectorEquipmentItems, Has.Count.EqualTo(1));
                Assert.That(viewModel.HydraulicSummaryCards, Has.Count.EqualTo(1));
                Assert.That(viewModel.TotalThermalPower_kW, Is.EqualTo(12));
                Assert.That(viewModel.TotalPipeLength, Is.EqualTo(180));
            });
        }

        [Test]
        public async Task RefreshAll_WhenSourceStateIsUnchanged_IsValueIdempotentWithoutDuplicateRowsOrCalculation()
        {
            var circuitsViewModel = ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, 2));
            var viewModel = ResultsViewModelTestHelpers.CreateResultsViewModel(_projectStateService, circuitsViewModel);
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            var thermalViewModel = GetField<ThermalViewModel>(viewModel, "_thermalViewModel");
            var thermalCalculator = GetField<IThermalCalculator>(thermalViewModel, "_calculator");
            var circuitsCalculator = GetField<SnowMeltingCalculator.Services.Hydraulics.ICircuitsCalculator>(
                circuitsViewModel,
                "_circuitsCalculator");

            viewModel.RefreshAll();
            var firstProjection = new
            {
                viewModel.SelectedCity,
                viewModel.TotalPowerDensity,
                viewModel.TotalThermalPower_kW,
                CollectorCount = viewModel.Collectors.Count,
                CircuitCount = viewModel.Circuits.Count,
                SpecificationCount = viewModel.CollectorSpecifications.Count,
                EquipmentCount = viewModel.CollectorEquipmentItems.Count,
                CardCount = viewModel.HydraulicSummaryCards.Count,
                MissingModuleCount = viewModel.MissingModules.Count
            };

            viewModel.RefreshAll();
            var secondProjection = new
            {
                viewModel.SelectedCity,
                viewModel.TotalPowerDensity,
                viewModel.TotalThermalPower_kW,
                CollectorCount = viewModel.Collectors.Count,
                CircuitCount = viewModel.Circuits.Count,
                SpecificationCount = viewModel.CollectorSpecifications.Count,
                EquipmentCount = viewModel.CollectorEquipmentItems.Count,
                CardCount = viewModel.HydraulicSummaryCards.Count,
                MissingModuleCount = viewModel.MissingModules.Count
            };

            Assert.That(secondProjection, Is.EqualTo(firstProjection));
            Mock.Get(thermalCalculator).Verify(
                calculator => calculator.Calculate(
                    It.IsAny<SnowMeltingCalculator.Models.Thermal.ThermalInputs>(),
                    It.IsAny<SnowMeltingCalculator.Models.Climate.IClimateData>(),
                    It.IsAny<SnowMeltingCalculator.Models.Thermal.IConstructionData>()),
                Times.Never);
            Mock.Get(circuitsCalculator).VerifyNoOtherCalls();
        }

        [Test]
        public async Task ResultsPdfDataBuilder_AfterInputMutation_RequiresCurrentScalarAndDerivedGeneration()
        {
            var viewModel = CreateReadyViewModel();
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            var climateViewModel = GetField<ClimateViewModel>(viewModel, "_climateViewModel");
            var circuitsViewModel = GetField<CircuitsViewModel>(viewModel, "_circuitsViewModel");
            var builder = GetField<ResultsPdfDataBuilder>(viewModel, "_resultsPdfDataBuilder");
            climateViewModel.SelectedCity = new CityInfo { Name = "PDF current city", Region = "PDF region" };
            climateViewModel.WindSpeed = 8.5;
            var collector = ResultsViewModelTestHelpers.CreateCollector(9, ValveType.IV_1_5, 1);
            collector.Summary = new CollectorSummary { CollectorNumber = 9, CircuitCount = 1, TotalPower = 9000, TotalPipeLength = 70 };
            ResultsViewModelTestHelpers.ReplaceCollectors(circuitsViewModel, collector);

            var pdfData = builder.Build(viewModel);

            Assert.Multiple(() =>
            {
                Assert.That(pdfData.City, Is.EqualTo("PDF current city"), "Scalar inputs must come from the current synchronized generation.");
                Assert.That(pdfData.WindSpeed, Is.EqualTo(8.5));
                Assert.That(pdfData.Collectors.Select(item => item.Number).ToArray(), Is.EqualTo(new[] { 9 }));
                Assert.That(pdfData.Collectors.SelectMany(item => item.Circuits).Count(), Is.EqualTo(1));
                Assert.That(pdfData.CollectorSpecifications.Select(item => item.Number).ToArray(), Is.EqualTo(new[] { 9 }));
                Assert.That(pdfData.TotalThermalPower_kW, Is.EqualTo(9));
            });
        }

        [Test]
        public async Task ResultsPdfDataBuilder_UsesConstructionLayersAndImageParametersFromSameCurrentSource()
        {
            var viewModel = CreateReadyViewModel();
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            var builder = GetField<ResultsPdfDataBuilder>(viewModel, "_resultsPdfDataBuilder");
            var constructionViewModel = GetField<ConstructionViewModel>(viewModel, "_constructionViewModel");
            var imageService = GetField<IConstructionVisualizationImageService>(builder, "_constructionVisualizationImageService");
            SnowMeltingCalculator.Services.Visualization.ConstructionVisualizationParameters? captured = null;
            constructionViewModel.LayersAbovePipe.Add(new Layer
            {
                Material = new Material { Name = "Current PDF material", LambdaA = 1.8, LambdaB = 2.1 },
                Thickness = 75,
                Position = LayerPosition.AbovePipe
            });
            Mock.Get(imageService)
                .Setup(service => service.GenerateImage(It.IsAny<SnowMeltingCalculator.Services.Visualization.ConstructionVisualizationParameters>(), 400, 300))
                .Callback<SnowMeltingCalculator.Services.Visualization.ConstructionVisualizationParameters, double, double>((parameters, _, _) => captured = parameters)
                .Returns(new byte[] { 1, 2, 3 });

            var pdfData = builder.Build(viewModel);

            Assert.Multiple(() =>
            {
                Assert.That(pdfData.Layers.Select(layer => layer.MaterialName).ToArray(), Is.EqualTo(new[] { "Current PDF material" }),
                    "Layer DTO and construction image must use the same current construction generation.");
                Assert.That(pdfData.ConstructionImageBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
                Assert.That(captured, Is.Not.Null);
                Assert.That(captured!.LayersAbovePipe, Is.SameAs(constructionViewModel.LayersAbovePipe));
                Assert.That(captured.LayersBelowPipe, Is.SameAs(constructionViewModel.LayersBelowPipe));
                Assert.That(captured.PipeSpacing, Is.EqualTo(viewModel.PipeSpacing));
            });
        }

        [Test]
        public async Task Reset_ClearsResultsProjectionAndMarksProjectClean()
        {
            var viewModel = CreateReadyViewModel();
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            _projectStateService.MarkDirty();

            viewModel.Reset();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.IsDataReady, Is.False);
                Assert.That(viewModel.Collectors, Is.Empty);
                Assert.That(viewModel.Circuits, Is.Empty);
                Assert.That(viewModel.CollectorSpecifications, Is.Empty);
                Assert.That(viewModel.CollectorEquipmentItems, Is.Empty);
                Assert.That(viewModel.HydraulicSummaryCards, Is.Empty);
                Assert.That(_projectStateService.IsDirty, Is.False);
            });
        }

        [Test]
        public async Task SaveCurrentProject_ProjectsLiveModuleStateInsteadOfResultsCache()
        {
            var viewModel = CreateReadyViewModel();
            await ResultsViewModelTestHelpers.LoadReadyModulesAsync(viewModel);
            var climateViewModel = GetField<ClimateViewModel>(viewModel, "_climateViewModel");
            climateViewModel.SelectedCity = new CityInfo { Name = "Live save city", Region = "Live region" };
            climateViewModel.WindSpeed = 11;

            var saved = viewModel.SaveCurrentProject();

            Assert.Multiple(() =>
            {
                Assert.That(saved.ClimateData.SelectedCity, Is.EqualTo("Live save city"));
                Assert.That(saved.ClimateData.WindSpeed, Is.EqualTo(11));
                Assert.That(viewModel.SelectedCity, Is.Not.EqualTo("Live save city"), "The fixture must prove save did not read the stale Results cache.");
            });
        }

        [Test]
        public async Task LoadAndReopen_RefreshesProjectionAndLeavesProjectClean()
        {
            var viewModel = CreateReadyViewModel();
            var first = ResultsViewModelTestHelpers.CreateReadyProjectData();
            var reopened = ResultsViewModelTestHelpers.CreateReadyProjectData();
            reopened.ProjectNumber = "REOPENED";
            reopened.ClimateData.SelectedCity = "Тестовый город";
            reopened.ClimateData.WindSpeed = 14;
            await viewModel.LoadProjectDataAsync(first);
            _projectStateService.MarkDirty();

            await viewModel.LoadProjectDataAsync(reopened);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ProjectNumber, Is.EqualTo("REOPENED"));
                Assert.That(viewModel.WindSpeed, Is.EqualTo(14));
                Assert.That(_projectStateService.IsDirty, Is.False);
            });
        }

        [Test]
        public void ProjectLoadOrchestrator_PreservesLoadOnlyThermalFallbackBoundary()
        {
            var sourcePath = FindRepositoryFile("src/Services/Project/ProjectLoadOrchestrator.cs");
            var source = File.ReadAllText(sourcePath);

            Assert.Multiple(() =>
            {
                Assert.That(source, Does.Contain("_thermalViewModel.Result != null && _thermalViewModel.Result.IsValid"));
                Assert.That(source, Does.Contain("await _thermalViewModel.CalculateCommand.ExecuteAsync(null);"));
                Assert.That(File.ReadAllText(FindRepositoryFile("src/ViewModels/Results/ResultsViewModel.cs")),
                    Does.Not.Contain("RefreshAll()\r\n        {\r\n            await _thermalViewModel.CalculateCommand"));
            });
        }

        private ResultsViewModel CreateReadyViewModel()
        {
            return ResultsViewModelTestHelpers.CreateResultsViewModel(
                _projectStateService,
                ResultsViewModelTestHelpers.CreateCircuitsViewModelWithCollectors(
                    ResultsViewModelTestHelpers.CreateCollector(1, ValveType.HKV_D, 2)));
        }

        private static T GetField<T>(object instance, string fieldName) where T : class
        {
            return (T)instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
        }

        private static string FindRepositoryFile(string relativePath)
        {
            var directory = TestContext.CurrentContext.TestDirectory;
            while (!File.Exists(Path.Combine(directory, "SnowMeltingCalculator.sln")))
                directory = Directory.GetParent(directory)!.FullName;
            return Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
