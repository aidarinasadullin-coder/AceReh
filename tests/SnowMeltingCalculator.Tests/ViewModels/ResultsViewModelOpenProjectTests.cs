using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Core.Results;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories;
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
            _dialogServiceMock
                .Setup(d => d.ShowOpenFileDialog(It.IsAny<string>()))
                .Returns(TestFilePath);
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));
            _dialogServiceMock
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), MessageBoxButton.YesNo, MessageBoxImage.Question))
                .Returns(MessageBoxResult.Yes);

            // Act
            await _viewModel.OpenProjectCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(
                d => d.Show("Текущий проект будет заменён. Продолжить?", "Открытие проекта", MessageBoxButton.YesNo, MessageBoxImage.Question),
                Times.Once);
            _projectFileServiceMock.Verify(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(_projectStateService.CurrentFilePath, Is.EqualTo(TestFilePath));
            Assert.That(_projectStateService.IsDirty, Is.False);
        }

        [Test]
        public async Task OpenProject_WhenClean_DoesNotShowPrompt()
        {
            // Arrange
            _projectStateService.MarkClean();
            _dialogServiceMock
                .Setup(d => d.ShowOpenFileDialog(It.IsAny<string>()))
                .Returns(TestFilePath);
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));

            // Act
            await _viewModel.OpenProjectCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(
                d => d.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()),
                Times.Never);
            _projectFileServiceMock.Verify(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(_projectStateService.CurrentFilePath, Is.EqualTo(TestFilePath));
            Assert.That(_projectStateService.IsDirty, Is.False);
        }

        [Test]
        public async Task OpenProject_WhenDirtyAndUserPicksNo_DoesNotLoad()
        {
            // Arrange
            _projectStateService.MarkDirty();
            _viewModel.ProjectNumber = "PRJ-001";
            _dialogServiceMock
                .Setup(d => d.ShowOpenFileDialog(It.IsAny<string>()))
                .Returns(TestFilePath);
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));
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

        [Test]
        public async Task ResultsViewModel_LoadProjectData_SelectsFirstCollectorAndEnablesCommands()
        {
            // Arrange
            var projectData = new ProjectData
            {
                ProjectNumber = "P-T2",
                ProjectObject = "Test Object",
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData(),
                ConstructionData = new ConstructionProjectData(),
                ThermalData = new ThermalProjectData(),
                HydraulicsData = new HydraulicsProjectData
                {
                    Collectors = new List<CollectorProjectData>
                    {
                        new CollectorProjectData
                        {
                            CollectorNumber = 1,
                            CollectorType = "HKV-D (2-12 контуров)",
                            ValveType = ValveType.HKV_D,
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData
                                {
                                    CircuitNumber = 1,
                                    CircuitLength = 50,
                                    SupplyLength = 10,
                                    SupplySpacingCm = 5,
                                    SupplyHeatPercent = 10,
                                    PipeSpacingCm = 20
                                }
                            }
                        }
                    }
                }
            };

            var circuitsVm = CreateCircuitsViewModel(allowRemoveCircuit: true);
            var viewModel = CreateViewModel(circuitsVm);

            // Act
            await viewModel.LoadProjectDataAsync(projectData);

            // Assert
            Assert.That(circuitsVm.SelectedCollectorIndex, Is.EqualTo(0));
            Assert.That(circuitsVm.AddCircuitCommand.CanExecute(null), Is.True);
            Assert.That(circuitsVm.RemoveCircuitCommand.CanExecute(circuitsVm.SelectedCollector!.Circuits[0]), Is.True);
        }

        [Test]
        public async Task ProjectRoundTrip_PipeSelectionRestored()
        {
            // Arrange
            const string pipeName = "RAUTHERM S 25x2,3";
            var projectData = new ProjectData
            {
                ProjectNumber = "P-T4",
                ProjectObject = "Pipe Restore Test",
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData(),
                ConstructionData = new ConstructionProjectData(),
                ThermalData = new ThermalProjectData
                {
                    SelectedPipe = new PipeTypeProjectData
                    {
                        Name = pipeName,
                        OuterDiameter = 25.0,
                        InnerDiameter = 20.4,
                        WallThickness = 2.3
                    }
                },
                HydraulicsData = new HydraulicsProjectData()
            };

            var climateVm = CreateClimateViewModel();
            var circuitsVm = CreateCircuitsViewModel();
            var thermalVm = CreateThermalViewModel();
            var viewModel = CreateViewModel(climateVm, constructionVm: CreateConstructionViewModel(), thermalVm, circuitsVm);

            // Act
            await viewModel.LoadProjectDataAsync(projectData);

            // Assert
            Assert.That(thermalVm.SelectedPipe, Is.Not.Null);
            Assert.That(thermalVm.SelectedPipe!.Name, Is.EqualTo(pipeName));
        }

        [Test]
        public async Task ProjectRoundTrip_DoesNotMarkDirtyOnLoad()
        {
            // Arrange
            var projectData = new ProjectData
            {
                ProjectNumber = "P-T6",
                ProjectObject = "Dirty Load Test",
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData
                {
                    SelectedCity = "Москва",
                    AirTemperature = -18.0,
                    WindSpeed = 3.5,
                    Humidity = 65.0,
                    SnowfallIntensity = 2.5,
                    SelectedZone = ClimateZone.Zone_M15,
                    IsHighRequirements = false
                },
                ConstructionData = new ConstructionProjectData
                {
                    R1 = 0.1,
                    R2 = 0.2
                },
                ThermalData = new ThermalProjectData
                {
                    SelectedMode = OperatingMode.Melting,
                    SupplyTemperature = 45.0,
                    GroundTemperature = 5.0,
                    PipeSpacing = 250,
                    SelectedPipe = new PipeTypeProjectData
                    {
                        Name = "RAUTHERM S 20x2,0",
                        OuterDiameter = 20.0,
                        InnerDiameter = 16.0,
                        WallThickness = 2.0
                    },
                    Result = new ThermalResultProjectData
                    {
                        PowerUp = 100.0,
                        PowerDown = 100.0,
                        PowerTotal = 200.0,
                        SupplyTemperature = 45.0,
                        ReturnTemperature = 35.0,
                        MeanTemperature = 40.0,
                        DeltaT = 10.0,
                        IsValid = true
                    }
                },
                HydraulicsData = new HydraulicsProjectData
                {
                    GlycolType = GlycolType.Ethylene,
                    GlycolConcentration = 30.0,
                    SupplySpacingCm = 10.0,
                    SupplyHeatPercent = 20.0,
                    Collectors = new List<CollectorProjectData>
                    {
                        new CollectorProjectData
                        {
                            CollectorNumber = 1,
                            CollectorType = "HKV-D (2-12 контуров)",
                            ValveType = ValveType.HKV_D,
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData
                                {
                                    CircuitNumber = 1,
                                    CircuitLength = 50.0,
                                    SupplyLength = 10.0,
                                    SupplySpacingCm = 5.0,
                                    SupplyHeatPercent = 10.0,
                                    PipeSpacingCm = 25.0
                                }
                            }
                        }
                    }
                }
            };

            var calculationStateService = new CalculationStateService();
            var viewModel = CreateViewModel(
                CreateClimateViewModel(calculationStateService, _projectStateService),
                CreateConstructionViewModel(calculationStateService, _projectStateService),
                CreateThermalViewModel(calculationStateService, _projectStateService),
                CreateCircuitsViewModel(calculationStateService, _projectStateService, allowRemoveCircuit: true),
                calculationStateService);

            // Act
            await viewModel.LoadProjectDataAsync(projectData);

            // Assert
            Assert.That(_projectStateService.IsDirty, Is.False);
        }

        [Test]
        public async Task ResultsViewModel_LoadProjectData_RestoresCityAndClimateParameters()
        {
            // Arrange
            const string cityName = "Москва";
            const string region = "Московская область";
            const double savedAirTemperature = -18.0;
            const double savedWindSpeed = 3.5;
            const double savedHumidity = 65.0;
            const double savedSnowfallIntensity = 2.5;

            var projectData = new ProjectData
            {
                ProjectNumber = "P-T3",
                ProjectObject = "Climate Restore Test",
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData
                {
                    SelectedCity = cityName,
                    Region = region,
                    AirTemperature = savedAirTemperature,
                    WindSpeed = savedWindSpeed,
                    Humidity = savedHumidity,
                    SnowfallIntensity = savedSnowfallIntensity,
                    SelectedZone = ClimateZone.Zone_M15,
                    IsHighRequirements = false
                },
                ConstructionData = new ConstructionProjectData(),
                ThermalData = new ThermalProjectData(),
                HydraulicsData = new HydraulicsProjectData()
            };

            var climateVm = CreateClimateViewModelWithCity(
                cityName,
                region,
                t5Days: -28,
                windAvg: 4.0,
                humidity: 70.0);
            var circuitsVm = CreateCircuitsViewModel();
            var viewModel = CreateViewModel(climateVm, circuitsVm);

            // Act
            await viewModel.LoadProjectDataAsync(projectData);

            // Assert
            Assert.That(climateVm.SelectedCity, Is.Not.Null);
            Assert.That(climateVm.SelectedCity!.Name, Is.EqualTo(cityName));
            Assert.That(climateVm.SelectedCity!.Region, Is.EqualTo(region));
            Assert.That(climateVm.AirTemperature, Is.EqualTo(savedAirTemperature));
            Assert.That(climateVm.WindSpeed, Is.EqualTo(savedWindSpeed));
            Assert.That(climateVm.Humidity, Is.EqualTo(savedHumidity));
            Assert.That(climateVm.SnowfallIntensity, Is.EqualTo(savedSnowfallIntensity));
            Assert.That(climateVm.SelectedZone, Is.EqualTo(ClimateZone.Zone_M15));
        }

        [Test]
        public async Task ProjectRoundTrip_CitySurvivesRealSaveLoad()
        {
            // Arrange
            const string cityName = "ТестовыйГород";
            const string region = "Тестовый регион";
            const double savedAirTemperature = -18.0;
            const double savedWindSpeed = 3.5;
            const double savedHumidity = 65.0;
            const double savedSnowfallIntensity = 2.5;

            var repositoryMock = new Mock<IClimateDataRepository>();
            repositoryMock.Setup(r => r.LoadCitiesAsync())
                .ReturnsAsync(new List<CityInfo>
                {
                    new CityInfo
                    {
                        Name = cityName,
                        Region = region,
                        T5Days092 = -28,
                        WindAvgTempLe8 = 4.0,
                        Humidity15hCold = 70.0
                    }
                });

            var climateService = new ClimateDataService(repositoryMock.Object);
            climateService.LoadClimateDataAsync().Wait();

            var climateVm = new ClimateViewModel(
                climateService,
                new ClimateData(),
                new ClimateValidator(),
                new Mock<IMarkDirtyService>().Object,
                new CalculationContext());

            climateVm.SelectedCity = climateService.GetCityByName(cityName);
            climateVm.AirTemperature = savedAirTemperature;
            climateVm.WindSpeed = savedWindSpeed;
            climateVm.Humidity = savedHumidity;
            climateVm.SnowfallIntensity = savedSnowfallIntensity;
            climateVm.SelectedZone = ClimateZone.Zone_M15;

            var viewModel = CreateViewModel(
                climateVm,
                CreateConstructionViewModel(),
                CreateThermalViewModel(),
                CreateCircuitsViewModel());

            // Act — сохраняем через реальный путь SaveCurrentProject
            var savedData = viewModel.SaveCurrentProject();

            // Assert — имя города сохранилось (primary bug: было пустым)
            Assert.That(savedData.ClimateData.SelectedCity, Is.EqualTo(cityName));

            // Act — сериализуем / десериализуем через тот же JSON-формат, что и ProjectFileService
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
            var json = JsonSerializer.Serialize(savedData, jsonOptions);
            var loadedData = JsonSerializer.Deserialize<ProjectData>(json, jsonOptions)!;

            // Act — загружаем в чистый ViewModel
            var climateVm2 = new ClimateViewModel(
                climateService,
                new ClimateData(),
                new ClimateValidator(),
                new Mock<IMarkDirtyService>().Object,
                new CalculationContext());

            var viewModel2 = CreateViewModel(
                climateVm2,
                CreateConstructionViewModel(),
                CreateThermalViewModel(),
                CreateCircuitsViewModel());

            await viewModel2.LoadProjectDataAsync(loadedData);

            // Assert — город и пользовательские климатические параметры восстановлены
            Assert.That(climateVm2.SelectedCity, Is.Not.Null);
            Assert.That(climateVm2.SelectedCity!.Name, Is.EqualTo(cityName));
            Assert.That(climateVm2.SelectedCity!.Region, Is.EqualTo(region));
            Assert.That(climateVm2.IsCitySelected, Is.True);
            Assert.That(climateVm2.AirTemperature, Is.EqualTo(savedAirTemperature));
            Assert.That(climateVm2.WindSpeed, Is.EqualTo(savedWindSpeed));
            Assert.That(climateVm2.Humidity, Is.EqualTo(savedHumidity));
            Assert.That(climateVm2.SnowfallIntensity, Is.EqualTo(savedSnowfallIntensity));
        }

        private ResultsViewModel CreateViewModel()
        {
            return CreateViewModel(CreateCircuitsViewModel());
        }

        private ResultsViewModel CreateViewModel(CircuitsViewModel circuitsVm)
        {
            return CreateViewModel(CreateClimateViewModel(), circuitsVm);
        }

        private ResultsViewModel CreateViewModel(ClimateViewModel climateVm, CircuitsViewModel circuitsVm)
        {
            return CreateViewModel(climateVm, CreateConstructionViewModel(), CreateThermalViewModel(), circuitsVm);
        }

        private ResultsViewModel CreateViewModel(
            ClimateViewModel climateVm,
            ConstructionViewModel constructionVm,
            ThermalViewModel thermalVm,
            CircuitsViewModel circuitsVm)
        {
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(new List<Material>());
            materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(new List<Material>());

            var constructionServiceMock = new Mock<IConstructionService>();
            constructionServiceMock.Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                .Returns(Task.CompletedTask);

            return new ResultsViewModel(
                _projectStateService,
                _projectStateService,
                _dialogServiceMock.Object,
                new Mock<IPdfExportService>().Object,
                _projectFileServiceMock.Object,
                new Mock<IConstructionVisualizationImageService>().Object,
                new CalculationStateService(),
                materialRepositoryMock.Object,
                constructionServiceMock.Object,
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

        private static ClimateViewModel CreateClimateViewModelWithCity(
            string cityName,
            string region,
            double t5Days,
            double windAvg,
            double humidity)
        {
            var climateServiceMock = new Mock<IClimateDataService>();
            climateServiceMock.Setup(s => s.LoadClimateDataAsync()).Returns(Task.CompletedTask);
            climateServiceMock.Setup(s => s.GetAllCities()).Returns(Enumerable.Empty<CityInfo>());
            climateServiceMock.Setup(s => s.GetCityByName(cityName)).Returns(new CityInfo
            {
                Name = cityName,
                Region = region,
                T5Days092 = t5Days,
                WindAvgTempLe8 = windAvg,
                Humidity15hCold = humidity
            });
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
                new Mock<IMarkDirtyService>().Object,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object);
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

        private static CircuitsViewModel CreateCircuitsViewModel(bool allowRemoveCircuit = false)
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

            var validatorMock = new Mock<ICircuitsValidator>();
            if (allowRemoveCircuit)
            {
                validatorMock
                    .Setup(v => v.CanRemoveCircuit(It.Is<CircuitRow>(c => c != null), It.IsAny<CollectorData>()))
                    .Returns(true);
            }

            return new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                new CalculationStateService(),
                validatorMock.Object,
                selectorMock.Object,
                new CalculationContext(),
                new Mock<IMarkDirtyService>().Object);
        }

        private ResultsViewModel CreateViewModel(
            ClimateViewModel climateVm,
            ConstructionViewModel constructionVm,
            ThermalViewModel thermalVm,
            CircuitsViewModel circuitsVm,
            CalculationStateService calculationStateService)
        {
            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(new List<Material>());
            materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(new List<Material>());

            var constructionServiceMock = new Mock<IConstructionService>();
            constructionServiceMock.Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                .Returns(Task.CompletedTask);

            return new ResultsViewModel(
                _projectStateService,
                _projectStateService,
                _dialogServiceMock.Object,
                new Mock<IPdfExportService>().Object,
                _projectFileServiceMock.Object,
                new Mock<IConstructionVisualizationImageService>().Object,
                calculationStateService,
                materialRepositoryMock.Object,
                constructionServiceMock.Object,
                climateVm,
                constructionVm,
                thermalVm,
                circuitsVm);
        }

        private static ClimateViewModel CreateClimateViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService)
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

        private static ConstructionViewModel CreateConstructionViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService)
        {
            var materials = new List<Material>
            {
                new Material { Id = 1, Name = "Sand", LambdaA = 0.8, LambdaB = 0.9 },
                new Material { Id = 2, Name = "Soil", LambdaA = 1.0, LambdaB = 1.1 },
                new Material { Id = 5, Name = "Concrete", LambdaA = 1.5, LambdaB = 1.6 }
            };

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);

            var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepositoryMock.Object,
                new Mock<IConstructionRepository>().Object,
                calculationStateService,
                new CalculationContext(),
                new ConstructionValidator(),
                new ConstructionModel(),
                markDirtyService,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object);
        }

        private static ThermalViewModel CreateThermalViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService)
        {
            var climateData = new ClimateData();
            var constructionData = new ConstructionData();
            return new ThermalViewModel(
                new Mock<IThermalCalculator>().Object,
                climateData,
                constructionData,
                calculationStateService,
                new CalculationContext(),
                new ThermalValidator(new ThermalCalculator(), climateData, constructionData),
                new ThermalResultValidator(),
                markDirtyService);
        }

        private static CircuitsViewModel CreateCircuitsViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService,
            bool allowRemoveCircuit = false)
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

            var validatorMock = new Mock<ICircuitsValidator>();
            if (allowRemoveCircuit)
            {
                validatorMock
                    .Setup(v => v.CanRemoveCircuit(It.Is<CircuitRow>(c => c != null), It.IsAny<CollectorData>()))
                    .Returns(true);
            }

            return new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                calculationStateService,
                validatorMock.Object,
                selectorMock.Object,
                new CalculationContext(),
                markDirtyService);
        }
    }
}
