using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
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
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Тесты команды открытия проекта в ResultsViewModel
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ResultsViewModelOpenProjectTests
    {
        private ProjectStateService _projectStateService = null!;
        private Mock<IDialogService> _dialogServiceMock = null!;
        private Mock<IProjectFileService> _projectFileServiceMock = null!;
        private ResultsViewModel _viewModel = null!;

        private const string TestFilePath = @"C:\temp\test-project.smc";

        [SetUp]
        public void SetUp()
        {
            _projectStateService = new ProjectStateService();
            _dialogServiceMock = new Mock<IDialogService>();
            _projectFileServiceMock = new Mock<IProjectFileService>();
            _viewModel = CreateViewModel();
        }

        [Test]
        public async Task OpenProject_WhenDirty_ShowsReplacePrompt()
        {
            // Arrange
            _projectStateService.MarkDirty();
            _projectFileServiceMock
                .Setup(p => p.GetOpenFilePathAsync())
                .ReturnsAsync(TestFilePath);
            _projectFileServiceMock
                .Setup(p => p.LoadProjectAsync(TestFilePath))
                .ReturnsAsync(new ProjectData());
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), MessageBoxButton.YesNo, MessageBoxImage.Question))
                .Returns(MessageBoxResult.Yes);

            // Act
            await _viewModel.OpenProjectCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(
                d => d.Show("Текущий проект будет заменён. Продолжить?", "Открытие проекта", MessageBoxButton.YesNo, MessageBoxImage.Question),
                Times.Once);
            _projectFileServiceMock.Verify(p => p.LoadProjectAsync(TestFilePath), Times.Once);
            Assert.That(_projectStateService.CurrentFilePath, Is.EqualTo(TestFilePath));
            Assert.That(_projectStateService.IsDirty, Is.False);
        }

        [Test]
        public async Task OpenProject_WhenClean_DoesNotShowPrompt()
        {
            // Arrange
            _projectStateService.MarkClean();
            _projectFileServiceMock
                .Setup(p => p.GetOpenFilePathAsync())
                .ReturnsAsync(TestFilePath);
            _projectFileServiceMock
                .Setup(p => p.LoadProjectAsync(TestFilePath))
                .ReturnsAsync(new ProjectData());

            // Act
            await _viewModel.OpenProjectCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(
                d => d.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()),
                Times.Never);
            _projectFileServiceMock.Verify(p => p.LoadProjectAsync(TestFilePath), Times.Once);
            Assert.That(_projectStateService.CurrentFilePath, Is.EqualTo(TestFilePath));
            Assert.That(_projectStateService.IsDirty, Is.False);
        }

        [Test]
        public async Task OpenProject_WhenDirtyAndUserPicksNo_DoesNotLoad()
        {
            // Arrange
            _projectStateService.MarkDirty();
            _viewModel.ProjectNumber = "PRJ-001";
            _projectFileServiceMock
                .Setup(p => p.GetOpenFilePathAsync())
                .ReturnsAsync(TestFilePath);
            _projectFileServiceMock
                .Setup(p => p.LoadProjectAsync(TestFilePath))
                .ReturnsAsync(new ProjectData());
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), MessageBoxButton.YesNo, MessageBoxImage.Question))
                .Returns(MessageBoxResult.No);

            // Act
            await _viewModel.OpenProjectCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(
                d => d.Show("Текущий проект будет заменён. Продолжить?", "Открытие проекта", MessageBoxButton.YesNo, MessageBoxImage.Question),
                Times.Once);
            Assert.That(_viewModel.ProjectNumber, Is.EqualTo("PRJ-001"));
            Assert.That(_projectStateService.CurrentFilePath, Is.Null);
            Assert.That(_projectStateService.IsDirty, Is.True);
        }

        private ResultsViewModel CreateViewModel()
        {
            var climateVm = CreateClimateViewModel();
            var constructionVm = CreateConstructionViewModel();
            var thermalVm = CreateThermalViewModel();
            var circuitsVm = CreateCircuitsViewModel();

            return new ResultsViewModel(
                _projectStateService,
                _projectStateService,
                _dialogServiceMock.Object,
                new Mock<IPdfExportService>().Object,
                _projectFileServiceMock.Object,
                new Mock<IConstructionVisualizationImageService>().Object,
                new CalculationStateService(),
                climateVm,
                constructionVm,
                thermalVm,
                circuitsVm);
        }

        private static ClimateViewModel CreateClimateViewModel()
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
                new Mock<IMarkDirtyService>().Object,
                new CalculationContext());
        }

        private static ConstructionViewModel CreateConstructionViewModel()
        {
            var materials = new List<Material>
            {
                new Material { Id = 1, Name = "Sand", LambdaA = 0.8, LambdaB = 0.9 },
                new Material { Id = 2, Name = "Soil", LambdaA = 1.0, LambdaB = 1.1 },
                new Material { Id = 5, Name = "Concrete", LambdaA = 1.5, LambdaB = 1.6 }
            };

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepositoryMock.Object,
                new Mock<IConstructionRepository>().Object,
                new CalculationStateService(),
                new CalculationContext(),
                new ConstructionValidator(),
                new ConstructionModel(),
                new Mock<IMarkDirtyService>().Object);
        }

        private static ThermalViewModel CreateThermalViewModel()
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
                new Mock<IMarkDirtyService>().Object);
        }

        private static CircuitsViewModel CreateCircuitsViewModel()
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
                new Mock<IMarkDirtyService>().Object);
        }
    }
}
