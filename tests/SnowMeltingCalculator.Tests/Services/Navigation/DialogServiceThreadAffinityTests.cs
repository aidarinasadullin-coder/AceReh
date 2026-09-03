// ================================================================================
// REHAU Снеготаяние - Тесты привязки диалогов к UI-потоку (T3)
// ================================================================================

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Moq;
using SnowMeltingCalculator.Services.Reports.Calculation;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Core.Results;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
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
using SnowMeltingCalculator.Tests.Fixtures;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.Services.Navigation
{
    /// <summary>
    /// Тесты проверяют, что WPF-диалоги сохранения/открытия файлов
    /// вызываются через <see cref="IDialogService"/> на UI-потоке,
    /// а не через <see cref="IProjectFileService"/> в фоновом потоке.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class DialogServiceThreadAffinityTests
    {
        private const string TestFilePath = @"C:\temp\test-project.smc";

        /// <summary>
        /// Методы GetSaveFilePathAsync и GetOpenFilePathAsync должны быть
        /// полностью удалены из ProjectFileService.
        /// </summary>
        [Test]
        public void ProjectFileService_NoSaveFileDialogReferenceLeft()
        {
            Assert.That(
                typeof(ProjectFileService).GetMethod("GetSaveFilePathAsync"),
                Is.Null,
                "GetSaveFilePathAsync must be removed from ProjectFileService");

            Assert.That(
                typeof(ProjectFileService).GetMethod("GetOpenFilePathAsync"),
                Is.Null,
                "GetOpenFilePathAsync must be removed from ProjectFileService");
        }

        /// <summary>
        /// ResultsViewModel.SaveProjectAs должен получать путь через
        /// IDialogService.ShowSaveFileDialog, а не через IProjectFileService.GetSaveFilePathAsync.
        /// </summary>
        [Test]
        public async Task ResultsViewModel_SaveProject_UsesDialogServiceNotProjectFileServiceForPath()
        {
            // Compile-time guard: IProjectFileService no longer exposes file dialog methods.
            Assert.That(
                typeof(IProjectFileService).GetMethod("GetSaveFilePathAsync"),
                Is.Null,
                "IProjectFileService must not expose GetSaveFilePathAsync");

            // Arrange
            var projectStateService = new ProjectStateService();
            var dialogServiceMock = new Mock<IDialogService>();
            var projectFileServiceMock = new Mock<IProjectFileService>();

            dialogServiceMock
                .Setup(d => d.ShowSaveFileDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .Returns(TestFilePath);

            projectFileServiceMock
                .Setup(p => p.SaveProjectResultAsync(It.IsAny<string>(), It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<object?>.Success(null));

            var viewModel = CreateResultsViewModel(
                projectStateService,
                projectFileServiceMock.Object,
                dialogServiceMock.Object);

            viewModel.ProjectNumber = "PRJ-001";

            // Act
            await viewModel.SaveProjectAsCommand.ExecuteAsync(null);

            // Assert
            dialogServiceMock.Verify(
                d => d.ShowSaveFileDialog(It.IsRegex(@"^PRJ-001_\d{8}$"), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()),
                Times.Once);

            projectFileServiceMock.Verify(
                p => p.SaveProjectResultAsync(TestFilePath, It.IsAny<ProjectData>(), It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.That(projectStateService.CurrentFilePath, Is.EqualTo(TestFilePath));
        }

        private static ResultsViewModel CreateResultsViewModel(
            ProjectStateService projectStateService,
            IProjectFileService projectFileService,
            IDialogService dialogService)
        {
            var materials = Material.GetDefaultMaterials().ToList();
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);
            materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(materials);
            materialRepositoryMock.Setup(r => r.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materials.SingleOrDefault(material => material.Id == id));

            var climateVm = CreateClimateViewModel(projectStateService);
            var constructionDefaultStateInitializer = new ConstructionDefaultStateInitializer(
                materialRepositoryMock.Object,
                projectStateService.Session.ConstructionState);
            var constructionVm = CreateConstructionViewModel(
                projectStateService,
                materialRepositoryMock.Object,
                constructionDefaultStateInitializer);
            var thermalVm = CreateThermalViewModel(projectStateService);
            var circuitsVm = CreateCircuitsViewModel(projectStateService);

            var constructionServiceMock = new Mock<IConstructionService>();
            constructionServiceMock.Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                .Returns(Task.CompletedTask);

            var calculationStateService = new CalculationStateService(projectStateService.Session);
            var calculationContext = new CalculationContext();

            return new ResultsViewModel(
                projectStateService.Session,
                dialogService,
                new Mock<IPdfExportService>().Object,
                new Mock<ICalculationReportExportService>().Object,
                projectFileService,
                calculationStateService,
                materialRepositoryMock.Object,
                constructionServiceMock.Object,
                new ProjectLoadOrchestrator(
                    climateVm,
                    constructionVm,
                    thermalVm,
                    circuitsVm,
                    calculationStateService,
                    constructionServiceMock.Object,
                    calculationContext,
                    projectStateService.Session,
                    constructionDefaultStateInitializer),
                new ResultsPdfDataBuilder(
                    new Mock<IConstructionVisualizationImageService>().Object,
                    calculationStateService,
                    constructionVm,
                    circuitsVm),
                new HydraulicSummaryBuilder());
        }

        private static ClimateViewModel CreateClimateViewModel(ProjectStateService projectStateService)
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
                projectStateService,
                new CalculationContext());
        }

        private static ConstructionViewModel CreateConstructionViewModel(
            ProjectStateService projectStateService,
            IMaterialRepository materialRepository,
            ConstructionDefaultStateInitializer constructionDefaultStateInitializer)
        {
            var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepository,
                new Mock<IConstructionRepository>().Object,
                new CalculationStateService(projectStateService.Session),
                new CalculationContext(),
                new ConstructionValidator(),
                new ConstructionModel(),
                projectStateService,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object,
                projectStateService.Session.ConstructionState,
                constructionDefaultStateInitializer);
        }

        private static ThermalViewModel CreateThermalViewModel(ProjectStateService projectStateService)
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
                projectStateService);
        }

        private static CircuitsViewModel CreateCircuitsViewModel(ProjectStateService projectStateService)
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

            var calculationStateService = new CalculationStateService();
            var calculationContext = new CalculationContext();
            var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(calculationStateService, calculationContext);
            return new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                 calculationStateService,
                new Mock<ICircuitsValidator>().Object,
                selectorMock.Object,
                  calculationContext,
                  hydraulicsDependencies.Coordinator,
                  hydraulicsDependencies.Session);
        }
    }
}
