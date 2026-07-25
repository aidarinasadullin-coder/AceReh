using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
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
    /// <summary>
    /// Тесты публичного метода Reset() для всех ViewModel
    /// </summary>
    [TestFixture]
    public class ResetOrchestrationTests
    {
        #region ClimateViewModel

        [Test]
        public void ClimateViewModel_Reset_ReturnsToDefaultsAndDoesNotMarkDirty()
        {
            var markDirtyMock = new Mock<IMarkDirtyService>();
            var vm = CreateClimateViewModel(markDirtyMock.Object);

            vm.AirTemperature = -25.0;
            vm.WindSpeed = 10.0;
            vm.Humidity = 50.0;
            vm.SnowfallIntensity = 5.0;
            vm.IsHighRequirements = true;
            vm.SelectedCity = new CityInfo { Name = "Test", T5Days092 = -20 };

            markDirtyMock.Invocations.Clear();
            vm.Reset();

            Assert.That(vm.SelectedCity, Is.Null);
            Assert.That(vm.AirTemperature, Is.EqualTo(-15.0));
            Assert.That(vm.ColdFiveDayTemperature, Is.EqualTo(0));
            Assert.That(vm.IsCitySelected, Is.False);
            Assert.That(vm.WindSpeed, Is.EqualTo(5.0));
            Assert.That(vm.Humidity, Is.EqualTo(70.0));
            Assert.That(vm.SnowfallIntensity, Is.EqualTo(0));
            Assert.That(vm.SelectedZone, Is.EqualTo(ClimateZone.Zone_M15));
            Assert.That(vm.IsHighRequirements, Is.False);
            Assert.That(vm.HasUserModifications, Is.False);
            Assert.That(vm.SearchQuery, Is.Empty);
            markDirtyMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        #endregion

        #region ConstructionViewModel

        [Test]
        public async Task ConstructionViewModel_Reset_ReturnsToDefaultsAndDoesNotMarkDirty()
        {
            var markDirtyMock = new Mock<IMarkDirtyService>();
            var vm = CreateConstructionViewModel(markDirtyMock.Object);

            await vm.InitializeCommand.ExecuteAsync(null);

            // Вносим изменение, чтобы убедиться, что сброс возвращает к дефолтам
            vm.AddLayerAbovePipeCommand.Execute(null);

            markDirtyMock.Invocations.Clear();
            vm.Reset();

            Assert.That(vm.LayersAbovePipe.Count, Is.EqualTo(1));
            Assert.That(vm.LayersBelowPipe.Count, Is.EqualTo(6));
            Assert.That(vm.GroundwaterLevel, Is.EqualTo(2.0));
            Assert.That(vm.HasLoads, Is.False);
            Assert.That(vm.SelectedGroundwaterOption, Is.EqualTo("УГВ >= 1 м (сухие условия)"));
            Assert.That(vm.HasUnsavedChanges, Is.False);
            markDirtyMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        #endregion

        #region ThermalViewModel

        [Test]
        public void ThermalViewModel_Reset_ReturnsToDefaultsAndDoesNotMarkDirty()
        {
            var markDirtyMock = new Mock<IMarkDirtyService>();
            var vm = CreateThermalViewModel(markDirtyMock.Object);

            vm.SelectedMode = OperatingMode.Intensive;
            vm.SupplyTemperature = 60.0;
            vm.GroundTemperature = 15.0;
            vm.SelectedPipe = PipeType.StandardPipes.First();
            vm.PipeSpacing = 250;

            markDirtyMock.Invocations.Clear();
            vm.Reset();

            Assert.That(vm.SelectedMode, Is.EqualTo(OperatingMode.Melting));
            Assert.That(vm.SupplyTemperature, Is.EqualTo(50.0));
            Assert.That(vm.GroundTemperature, Is.EqualTo(10.0));
            Assert.That(vm.SelectedPipe, Is.Null);
            Assert.That(vm.PipeSpacing, Is.EqualTo(200));
            Assert.That(vm.Result, Is.Null);
            Assert.That(vm.ValidationMessage, Is.Empty);
            markDirtyMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        #endregion

        #region CircuitsViewModel

        [Test]
        public void CircuitsViewModel_Reset_ReturnsToOneCollectorWithTwoCircuitsAndDoesNotMarkDirty()
        {
            var markDirtyMock = new Mock<IMarkDirtyService>();
            var vm = CreateCircuitsViewModel(markDirtyMock.Object);

            // Вносим изменение, чтобы проверить, что Reset очищает коллекторы
            vm.AddCollectorCommand.Execute(null);

            markDirtyMock.Invocations.Clear();
            vm.Reset();

            Assert.That(vm.Collectors.Count, Is.EqualTo(1));
            Assert.That(vm.Collectors[0].Circuits.Count, Is.EqualTo(2));
            Assert.That(vm.CurrentMode, Is.EqualTo(HydraulicMode.OperatingTemperature));
            markDirtyMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        #endregion

        #region ResultsViewModel

        [Test]
        public void ResultsViewModel_Reset_ClearsProjectInfoAndDoesNotMarkDirty()
        {
            var markDirtyMock = new Mock<IMarkDirtyService>();
            var projectStateService = new ProjectStateService();
            var vm = CreateResultsViewModel(projectStateService, markDirtyMock.Object);

            vm.ProjectNumber = "PRJ-001";
            vm.ProjectObject = "Test Object";
            SetCurrentFilePath(vm, @"C:\temp\project.smc");
            projectStateService.CurrentFilePath = @"C:\temp\project.smc";
            vm.IsOperatingMode = false;

            markDirtyMock.Invocations.Clear();
            vm.Reset();

            Assert.That(vm.ProjectNumber, Is.Empty);
            Assert.That(vm.ProjectObject, Is.Empty);
            Assert.That(GetCurrentFilePath(vm), Is.Null);
            Assert.That(projectStateService.CurrentFilePath, Is.Null);
            Assert.That(vm.IsOperatingMode, Is.True);
            Assert.That(vm.StatusMessage, Is.Empty);
            Assert.That(projectStateService.IsDirty, Is.False);
            markDirtyMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        #endregion

        #region Helpers

        private static ClimateViewModel CreateClimateViewModel(IMarkDirtyService markDirtyService)
        {
            var climateServiceMock = new Mock<IClimateDataService>();
            climateServiceMock.Setup(s => s.LoadClimateDataAsync()).Returns(Task.CompletedTask);
            climateServiceMock.Setup(s => s.GetAllCities()).Returns(Enumerable.Empty<CityInfo>());
            climateServiceMock.Setup(s => s.DetermineZone(It.IsAny<double>(), It.IsAny<bool>()))
                .Returns((double t, bool high) =>
                {
                    if (high) return ClimateZone.Zone_M20_Plus;
                    if (t >= -27) return ClimateZone.Zone_M10;
                    if (t > -37) return ClimateZone.Zone_M15;
                    return ClimateZone.Zone_M20;
                });

            return new ClimateViewModel(
                climateServiceMock.Object,
                new ClimateData(),
                new ClimateValidator(),
                markDirtyService,
                new CalculationContext());
        }

        private static ConstructionViewModel CreateConstructionViewModel(IMarkDirtyService markDirtyService)
        {
            var materials = new List<Material>
            {
                new Material { Id = 1, Name = "Sand", LambdaA = 0.8, LambdaB = 0.9 },
                new Material { Id = 2, Name = "Soil", LambdaA = 1.0, LambdaB = 1.1 },
                new Material { Id = 5, Name = "Concrete", LambdaA = 1.74, LambdaB = 1.74 },
                new Material { Id = 6, Name = "Concrete with mesh", LambdaA = 1.69, LambdaB = 2.04 },
                new Material { Id = 10, Name = "XPS", LambdaA = 0.035, LambdaB = 0.035 },
                new Material { Id = 13, Name = "PGS", LambdaA = 1.0, LambdaB = 1.8 }
            };

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);

            var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepositoryMock.Object,
                new Mock<IConstructionRepository>().Object,
                new CalculationStateService(),
                new CalculationContext(),
                new ConstructionValidator(),
                new ConstructionModel(),
                markDirtyService,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object);
        }

        private static ThermalViewModel CreateThermalViewModel(IMarkDirtyService markDirtyService)
        {
            var climateData = new ClimateData();
            var constructionData = new ConstructionData();
            return new ThermalViewModel(
                new Mock<IThermalCalculator>().Object,
                climateData,
                constructionData,
                new CalculationStateService(),
                new CalculationContext(),
                new ThermalValidator(new ThermalCalculator(), climateData, constructionData),
                new ThermalResultValidator(),
                markDirtyService);
        }

        private static CircuitsViewModel CreateCircuitsViewModel(IMarkDirtyService markDirtyService)
        {
            var calculatorMock = new Mock<ICircuitsCalculator>();
            calculatorMock.Setup(c => c.CalculateCircuitPower(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(0.0);
            calculatorMock.Setup(c => c.CalculateFlowRate(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(0.0);
            calculatorMock.Setup(c => c.CalculateCollectorSummary(It.IsAny<List<CircuitRow>>(), It.IsAny<int>(), It.IsAny<ValveType>())).Returns(new CollectorSummary());
            calculatorMock.Setup(c => c.CalculateAtTemperature(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<GlycolProperties>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<ValveType>())).Returns(new CircuitTemperatureResult());
            calculatorMock.Setup(c => c.CalculateBalancing(It.IsAny<List<CircuitRow>>(), It.IsAny<ValveType>())).Returns((List<CircuitRow> circuits, ValveType _) => circuits);

            var glycolMock = new Mock<IGlycolDataService>();
            glycolMock.Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>())).Returns(new GlycolProperties { Density = 1050, SpecificHeat = 3800, KinematicViscosity = 0.000005 });

            var selectorMock = new Mock<ICollectorTypeSelector>();
            selectorMock.Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>())).Returns(new CollectorSelectionResult { ValveType = ValveType.HKV_D });

            return new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                new CalculationStateService(),
                new Mock<ICircuitsValidator>().Object,
                selectorMock.Object,
                new CalculationContext(),
                markDirtyService);
        }

        private static ResultsViewModel CreateResultsViewModel(IProjectStateService projectStateService, IMarkDirtyService markDirtyService)
        {
            var climateVm = CreateClimateViewModel(new Mock<IMarkDirtyService>().Object);
            var constructionVm = CreateConstructionViewModel(new Mock<IMarkDirtyService>().Object);
            var thermalVm = CreateThermalViewModel(new Mock<IMarkDirtyService>().Object);
            var circuitsVm = CreateCircuitsViewModel(new Mock<IMarkDirtyService>().Object);

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(new List<Material>());
            materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(new List<Material>());

            var constructionServiceMock = new Mock<IConstructionService>();
            constructionServiceMock.Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                .Returns(Task.CompletedTask);

            var calculationStateService = new CalculationStateService();
            var calculationContext = new CalculationContext();

            return new ResultsViewModel(
                projectStateService,
                markDirtyService,
                new Mock<IDialogService>().Object,
                new Mock<IPdfExportService>().Object,
                new Mock<IProjectFileService>().Object,
                new Mock<IConstructionVisualizationImageService>().Object,
                calculationStateService,
                materialRepositoryMock.Object,
                constructionServiceMock.Object,
                calculationContext,
                climateVm,
                constructionVm,
                thermalVm,
                circuitsVm,
                new ProjectLoadOrchestrator(
                    climateVm,
                    constructionVm,
                    thermalVm,
                    circuitsVm,
                    calculationStateService,
                    constructionServiceMock.Object,
                    calculationContext));
        }

        private static string? GetCurrentFilePath(ResultsViewModel vm)
        {
            var field = typeof(ResultsViewModel).GetField("_currentFilePath", BindingFlags.NonPublic | BindingFlags.Instance);
            return (string?)field?.GetValue(vm);
        }

        private static void SetCurrentFilePath(ResultsViewModel vm, string? value)
        {
            var field = typeof(ResultsViewModel).GetField("_currentFilePath", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(vm, value);
        }

        #endregion
    }
}
