using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using SnowMeltingCalculator.Tests.Fixtures;
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
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNo, DialogIcon.Question))
                .Returns(DialogResult.Yes);

            // Act
            await _viewModel.OpenProjectCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(
                d => d.Show("Текущий проект будет заменён. Продолжить?", "Открытие проекта", DialogButtons.YesNo, DialogIcon.Question),
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
                d => d.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DialogButtons>(), It.IsAny<DialogIcon>()),
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
                .Setup(d => d.Show(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNo, DialogIcon.Question))
                .Returns(DialogResult.No);

            // Act
            await _viewModel.OpenProjectCommand.ExecuteAsync(null);

            // Assert
            _dialogServiceMock.Verify(
                d => d.Show("Текущий проект будет заменён. Продолжить?", "Открытие проекта", DialogButtons.YesNo, DialogIcon.Question),
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
            var viewModel = CreateViewModel(climateVm, constructionVm: CreateConstructionViewModel(_projectStateService.Session), thermalVm, circuitsVm);

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

            var calculationStateService = new CalculationStateService(_projectStateService.Session);
            var viewModel = CreateViewModel(
                CreateClimateViewModel(calculationStateService, _projectStateService),
                CreateConstructionViewModel(calculationStateService, _projectStateService, _projectStateService.Session),
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
                humidity: 70.0,
                _projectStateService.Session);
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
        public async Task ResultsViewModel_LoadProjectData_SyncsClimateToSingletonData()
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
                ProjectObject = "Climate Singleton Sync Test",
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

            var climateDataSingleton = new ClimateData();
            _projectStateService = new ProjectStateService(
                new ProjectSession(climateDataSingleton, new CalculationContext()));
            var climateVm = CreateClimateViewModelWithCityAndSingleton(
                climateDataSingleton,
                cityName,
                region,
                t5Days: -28,
                windAvg: 4.0,
                humidity: 70.0,
                _projectStateService.Session);
            var circuitsVm = CreateCircuitsViewModel();
            var viewModel = CreateViewModel(climateVm, circuitsVm);

            // Act
            await viewModel.LoadProjectDataAsync(projectData);

            // Assert — singleton IClimateData must receive the same values as the ViewModel
            Assert.That(climateDataSingleton.SelectedCity, Is.EqualTo(cityName));
            Assert.That(climateDataSingleton.SelectedRegion, Is.EqualTo(region));
            Assert.That(climateDataSingleton.AirTemperature, Is.EqualTo(savedAirTemperature));
            Assert.That(climateDataSingleton.WindSpeed, Is.EqualTo(savedWindSpeed));
            Assert.That(climateDataSingleton.Humidity, Is.EqualTo(savedHumidity));
            Assert.That(climateDataSingleton.SnowfallIntensity, Is.EqualTo(savedSnowfallIntensity));
            Assert.That(climateDataSingleton.Zone, Is.EqualTo(ClimateZone.Zone_M15));
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

            // ClimateViewModel теперь адаптер над каноническим ClimateState.
            // Для проверки persistence используем ViewModel, привязанную к той же сессии,
            // что и ResultsViewModel, чтобы мутации шли в canonical snapshot.
            var climateVm = new ClimateViewModel(
                climateService,
                new ClimateData(),
                new ClimateValidator(),
                _projectStateService.Session);

            climateVm.SelectedCity = climateService.GetCityByName(cityName);
            climateVm.AirTemperature = savedAirTemperature;
            climateVm.WindSpeed = savedWindSpeed;
            climateVm.Humidity = savedHumidity;
            climateVm.SnowfallIntensity = savedSnowfallIntensity;
            climateVm.SelectedZone = ClimateZone.Zone_M15;

            var viewModel = CreateViewModel(
                climateVm,
                CreateConstructionViewModel(_projectStateService.Session),
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

            // Act — загружаем в чистый ViewModel, привязанный к той же канонической сессии.
            // Так load boundary обновляет ClimateState snapshot, и адаптер climateVm2
            // получает актуальные значения через Changed-событие.
            var climateVm2 = new ClimateViewModel(
                climateService,
                new ClimateData(),
                new ClimateValidator(),
                _projectStateService.Session);

            var viewModel2 = CreateViewModel(
                climateVm2,
                CreateConstructionViewModel(_projectStateService.Session),
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

        [Test]
        public void SaveCurrentProject_PersistsClimateStateSnapshot_NotClimateViewModelMirror()
        {
            // Arrange: climateVm и projectSession.ClimateState — разные экземпляры
            // ProjectSessionClimateState, потому что helper использует legacy constructor.
            // Устанавливаем зеркальные значения в climateVm и канонические — в ProjectSession.ClimateState.
            // SaveCurrentProject должен сохранить именно канонический snapshot.
            const string canonicalCity = "Канонический город";
            const string canonicalRegion = "Канонический регион";
            const double canonicalAirTemperature = -22.0;
            const double canonicalWindSpeed = 5.5;
            const double canonicalHumidity = 80.0;
            const double canonicalSnowfallIntensity = 3.5;
            const ClimateZone canonicalZone = ClimateZone.Zone_M20;
            const bool canonicalHighRequirements = true;

            var climateVm = CreateClimateViewModelWithCity(
                "Зеркальный город",
                "Зеркальный регион",
                t5Days: -25,
                windAvg: 4.0,
                humidity: 70.0);

            var viewModel = CreateViewModel(
                climateVm,
                CreateConstructionViewModel(_projectStateService.Session),
                CreateThermalViewModel(),
                CreateCircuitsViewModel());

            _projectStateService.Session.ClimateState.ApplyProjectSnapshot(
                new ClimateProjectData
                {
                    SelectedCity = canonicalCity,
                    Region = canonicalRegion,
                    AirTemperature = canonicalAirTemperature,
                    WindSpeed = canonicalWindSpeed,
                    Humidity = canonicalHumidity,
                    SnowfallIntensity = canonicalSnowfallIntensity,
                    SelectedZone = canonicalZone,
                    IsHighRequirements = canonicalHighRequirements
                },
                city: null,
                ClimateMutationOrigin.Load);

            // Sanity: канонический snapshot и climateVm mirror действительно различаются.
            Assert.That(_projectStateService.Session.ClimateState.Snapshot.SelectedCity,
                Is.Not.EqualTo(climateVm.SelectedCity?.Name),
                "Sanity: canonical ClimateState snapshot must differ from ClimateViewModel mirror city.");
            Assert.That(_projectStateService.Session.ClimateState.Snapshot.AirTemperature,
                Is.Not.EqualTo(climateVm.AirTemperature),
                "Sanity: canonical ClimateState snapshot must differ from ClimateViewModel mirror temperature.");

            // Act
            var saved = viewModel.SaveCurrentProject();

            // Assert: сохранённые поля совпадают с каноническим snapshot.
            Assert.That(saved.ClimateData.SelectedCity, Is.EqualTo(canonicalCity));
            Assert.That(saved.ClimateData.Region, Is.EqualTo(canonicalRegion));
            Assert.That(saved.ClimateData.AirTemperature, Is.EqualTo(canonicalAirTemperature));
            Assert.That(saved.ClimateData.WindSpeed, Is.EqualTo(canonicalWindSpeed));
            Assert.That(saved.ClimateData.Humidity, Is.EqualTo(canonicalHumidity));
            Assert.That(saved.ClimateData.SnowfallIntensity, Is.EqualTo(canonicalSnowfallIntensity));
            Assert.That(saved.ClimateData.SelectedZone, Is.EqualTo(canonicalZone));
            Assert.That(saved.ClimateData.IsHighRequirements, Is.EqualTo(canonicalHighRequirements));

            // Assert: сохранённые поля не совпадают с зеркальными значениями climateVm.
            Assert.That(saved.ClimateData.SelectedCity, Is.Not.EqualTo(climateVm.SelectedCity?.Name));
            Assert.That(saved.ClimateData.Region, Is.Not.EqualTo(climateVm.SelectedCity?.Region));
            Assert.That(saved.ClimateData.AirTemperature, Is.Not.EqualTo(climateVm.AirTemperature));
            Assert.That(saved.ClimateData.WindSpeed, Is.Not.EqualTo(climateVm.WindSpeed));
            Assert.That(saved.ClimateData.Humidity, Is.Not.EqualTo(climateVm.Humidity));
            Assert.That(saved.ClimateData.SnowfallIntensity, Is.Not.EqualTo(climateVm.SnowfallIntensity));
            Assert.That(saved.ClimateData.SelectedZone, Is.Not.EqualTo(climateVm.SelectedZone));
            Assert.That(saved.ClimateData.IsHighRequirements, Is.Not.EqualTo(climateVm.IsHighRequirements));
        }

        [Test]
        public void SaveCurrentProject_PersistsConstructionStateSnapshot_NotConstructionViewModelMirror()
        {
            // Arrange: constructionVm подключён к канонической сессии, но VM-коллекции
            // и канонический snapshot различаются. SaveCurrentProject должен сохранить
            // именно канонический snapshot, а не stale VM-кэш.
            const double canonicalGroundwater = 0.5;
            const bool canonicalHasLoads = true;
            const double canonicalThickness = 333.0;
            const double canonicalLambda = 9.5;
            const bool canonicalOverride = true;

            var constructionVm = CreateConstructionViewModel(_projectStateService.Session);
            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());

            // Sanity: VM стартует с default-значениями, отличными от канонических.
            Assert.That(_projectStateService.Session.ConstructionState.Snapshot.GroundwaterLevel,
                Is.Not.EqualTo(canonicalGroundwater).Within(0.001),
                "Sanity: canonical ConstructionState must differ from target canonical values.");

            // Применяем канонический snapshot напрямую через state API (non-user origin).
            var canonicalSnapshot = new ConstructionStateSnapshot(
                canonicalGroundwater,
                canonicalHasLoads,
                new[]
                {
                    new ConstructionLayerSnapshot(
                        Guid.NewGuid(),
                        5,
                        "Concrete",
                        canonicalThickness,
                        canonicalLambda,
                        canonicalOverride,
                        LayerPosition.AbovePipe,
                        0)
                },
                Array.Empty<ConstructionLayerSnapshot>());

            var mutation = _projectStateService.Session.ConstructionState.ApplySnapshot(
                canonicalSnapshot,
                ConstructionMutationOrigin.ProjectLoad);

            Assert.That(mutation.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
            Assert.That(_projectStateService.Session.ConstructionState.Snapshot.GroundwaterLevel,
                Is.EqualTo(canonicalGroundwater).Within(1e-9));
            Assert.That(_projectStateService.Session.ConstructionState.Snapshot.HasLoads, Is.True);
            Assert.That(_projectStateService.Session.ConstructionState.Snapshot.LayersAbovePipe, Has.Count.EqualTo(1));
            Assert.That(constructionVm.GroundwaterLevel, Is.Not.EqualTo(canonicalGroundwater).Within(0.001));
            Assert.That(constructionVm.HasLoads, Is.False);
            Assert.That(constructionVm.LayersAbovePipe, Is.Empty);

            // Act
            var saved = viewModel.SaveCurrentProject();

            // Assert: сохранённые поля совпадают с каноническим snapshot.
            Assert.Multiple(() =>
            {
                Assert.That(saved.ConstructionData.GroundwaterLevel,
                    Is.EqualTo(canonicalGroundwater).Within(1e-9));
                Assert.That(saved.ConstructionData.HasLoads, Is.EqualTo(canonicalHasLoads));
                Assert.That(saved.ConstructionData.Layers, Has.Count.EqualTo(1));
                var savedLayer = saved.ConstructionData.Layers[0];
                Assert.That(savedLayer.Position, Is.EqualTo(LayerPosition.AbovePipe));
                Assert.That(savedLayer.MaterialName, Is.EqualTo("Concrete"));
                Assert.That(savedLayer.Thickness, Is.EqualTo(canonicalThickness).Within(1e-9));
                Assert.That(savedLayer.CalculatedLambda, Is.EqualTo(canonicalLambda).Within(1e-9));
                Assert.That(savedLayer.IsLambdaOverridden, Is.EqualTo(canonicalOverride));
                Assert.That(savedLayer.Order, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task ProjectRoundTrip_FieldCompleteRoundTrip_SecondLoadReplacesProjectA()
        {
            var sourceAConstruction = await CreateInitializedConstructionViewModelAsync();
            var sourceAViewModel = CreateViewModel(
                CreateClimateViewModel(),
                sourceAConstruction,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());
            var sourceAIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            _projectStateService.Session.ConstructionState.ApplySnapshot(
                new ConstructionStateSnapshot(
                    0.45,
                    true,
                    new[]
                    {
                        new ConstructionLayerSnapshot(sourceAIds[0], 5, "Concrete", 81, 2.51, true, LayerPosition.AbovePipe, 0),
                        new ConstructionLayerSnapshot(sourceAIds[1], 1, "Sand", 42, 0.82, true, LayerPosition.AbovePipe, 1)
                    },
                    new[]
                    {
                        new ConstructionLayerSnapshot(sourceAIds[2], 2, "Soil", 133, 1.12, true, LayerPosition.BelowPipe, 0),
                        new ConstructionLayerSnapshot(sourceAIds[3], 5, "Concrete", 244, 1.63, true, LayerPosition.BelowPipe, 1)
                    }),
                ConstructionMutationOrigin.User);
            var projectA = sourceAViewModel.SaveCurrentProject();

            var projectFileService = new ProjectFileService();
            var projectAPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"construction-a-{Guid.NewGuid():N}.smc");
            var projectBPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"construction-b-{Guid.NewGuid():N}.smc");
            try
            {
                Assert.That(await projectFileService.SaveProjectAsync(projectAPath, projectA), Is.True);
                var loadedProjectA = await projectFileService.LoadProjectAsync(projectAPath);
                Assert.That(loadedProjectA, Is.Not.Null);

                _projectStateService = new ProjectStateService();
                var targetConstruction = await CreateInitializedConstructionViewModelAsync();
                var targetViewModel = CreateViewModel(
                    CreateClimateViewModel(),
                    targetConstruction,
                    CreateThermalViewModel(),
                    CreateCircuitsViewModel());
                var targetSession = _projectStateService.Session;
                var completionCount = 0;
                targetSession.ConstructionState.Changed += (_, _) => completionCount++;

                await targetViewModel.LoadProjectDataAsync(loadedProjectA!);

                var roundTrippedA = targetSession.ConstructionState.Snapshot;
                Assert.Multiple(() =>
                {
                    Assert.That(roundTrippedA.GroundwaterLevel, Is.EqualTo(0.45).Within(1e-9));
                    Assert.That(roundTrippedA.HasLoads, Is.True);
                    Assert.That(roundTrippedA.LayersAbovePipe.Select(layer => layer.Order), Is.EqualTo(new[] { 0, 1 }));
                    Assert.That(roundTrippedA.LayersAbovePipe.Select(layer => layer.MaterialId), Is.EqualTo(new[] { 5, 1 }));
                    Assert.That(roundTrippedA.LayersAbovePipe.Select(layer => layer.MaterialName), Is.EqualTo(new[] { "Concrete", "Sand" }));
                    Assert.That(roundTrippedA.LayersAbovePipe.Select(layer => layer.Thickness), Is.EqualTo(new[] { 81d, 42d }));
                    Assert.That(roundTrippedA.LayersAbovePipe.Select(layer => layer.CalculatedLambda), Is.EqualTo(new[] { 2.51, 0.82 }));
                    Assert.That(roundTrippedA.LayersBelowPipe.Select(layer => layer.Order), Is.EqualTo(new[] { 0, 1 }));
                    Assert.That(roundTrippedA.LayersBelowPipe.Select(layer => layer.MaterialId), Is.EqualTo(new[] { 2, 5 }));
                    Assert.That(roundTrippedA.LayersBelowPipe.Select(layer => layer.MaterialName), Is.EqualTo(new[] { "Soil", "Concrete" }));
                    Assert.That(roundTrippedA.LayersBelowPipe.Select(layer => layer.Thickness), Is.EqualTo(new[] { 133d, 244d }));
                    Assert.That(roundTrippedA.LayersBelowPipe.Select(layer => layer.CalculatedLambda), Is.EqualTo(new[] { 1.12, 1.63 }));
                    Assert.That(roundTrippedA.LayersAbovePipe.Concat(roundTrippedA.LayersBelowPipe).All(layer => !layer.IsLambdaOverridden), Is.True,
                        ".smc stores override flags, while project restore intentionally resets them after preserving lambda values.");
                    Assert.That(roundTrippedA.LayersAbovePipe.Concat(roundTrippedA.LayersBelowPipe).All(layer => layer.Id != Guid.Empty), Is.True);
                    Assert.That(roundTrippedA.LayersAbovePipe.Concat(roundTrippedA.LayersBelowPipe).Select(layer => layer.Id), Is.Unique);
                    Assert.That(roundTrippedA.LayersAbovePipe.Concat(roundTrippedA.LayersBelowPipe).Select(layer => layer.Id), Is.Not.EquivalentTo(sourceAIds),
                        ".smc does not persist layer IDs; restore regenerates stable non-empty IDs for the loaded session.");
                    Assert.That(completionCount, Is.EqualTo(1));
                    Assert.That(_projectStateService.IsDirty, Is.False);
                });

                var sourceBStateService = new ProjectStateService();
                _projectStateService = sourceBStateService;
                var sourceBConstruction = await CreateInitializedConstructionViewModelAsync();
                var sourceBViewModel = CreateViewModel(
                    CreateClimateViewModel(),
                    sourceBConstruction,
                    CreateThermalViewModel(),
                    CreateCircuitsViewModel());
                sourceBStateService.Session.ConstructionState.ApplySnapshot(
                    new ConstructionStateSnapshot(
                        3.75,
                        false,
                        new[]
                        {
                            new ConstructionLayerSnapshot(Guid.NewGuid(), 2, "Soil", 17, 1.07, true, LayerPosition.AbovePipe, 0)
                        },
                        new[]
                        {
                            new ConstructionLayerSnapshot(Guid.NewGuid(), 1, "Sand", 318, 0.93, true, LayerPosition.BelowPipe, 0)
                        }),
                    ConstructionMutationOrigin.User);
                var projectB = sourceBViewModel.SaveCurrentProject();
                Assert.That(await projectFileService.SaveProjectAsync(projectBPath, projectB), Is.True);
                var loadedProjectB = await projectFileService.LoadProjectAsync(projectBPath);
                Assert.That(loadedProjectB, Is.Not.Null);

                _projectStateService = new ProjectStateService();
                await targetViewModel.LoadProjectDataAsync(loadedProjectB!);

                var roundTrippedB = targetSession.ConstructionState.Snapshot;
                Assert.Multiple(() =>
                {
                    Assert.That(roundTrippedB.GroundwaterLevel, Is.EqualTo(3.75).Within(1e-9));
                    Assert.That(roundTrippedB.HasLoads, Is.False);
                    Assert.That(roundTrippedB.LayersAbovePipe.Select(layer => (layer.Order, layer.MaterialId, layer.MaterialName, layer.Thickness, layer.CalculatedLambda, layer.IsLambdaOverridden)),
                        Is.EqualTo(new[] { (0, 2, "Soil", 17d, 1.07, false) }));
                    Assert.That(roundTrippedB.LayersBelowPipe.Select(layer => (layer.Order, layer.MaterialId, layer.MaterialName, layer.Thickness, layer.CalculatedLambda, layer.IsLambdaOverridden)),
                        Is.EqualTo(new[] { (0, 1, "Sand", 318d, 0.93, false) }));
                    Assert.That(roundTrippedB.LayersAbovePipe.Concat(roundTrippedB.LayersBelowPipe).All(layer => layer.Id != Guid.Empty), Is.True);
                    Assert.That(roundTrippedB.LayersAbovePipe.Concat(roundTrippedB.LayersBelowPipe).Select(layer => layer.Id), Is.Unique);
                    Assert.That(roundTrippedB.LayersAbovePipe.Concat(roundTrippedB.LayersBelowPipe).Select(layer => layer.MaterialName),
                        Does.Not.Contain("Concrete"));
                    Assert.That(roundTrippedB.LayersAbovePipe.Concat(roundTrippedB.LayersBelowPipe).Select(layer => layer.Thickness),
                        Is.Not.EquivalentTo(new[] { 81d, 42d, 133d, 244d }));
                    Assert.That(completionCount, Is.EqualTo(2), "Each project load must produce exactly one completion without subscription multiplication.");
                    Assert.That(targetSession.IsDirty, Is.False);
                });
            }
            finally
            {
                File.Delete(projectAPath);
                File.Delete(projectBPath);
            }
        }

        [Test]
        public async Task ProjectFileService_RoundTripPreservesSchemaVersionAndJsonShape()
        {
            var projectFileService = new ProjectFileService();
            var projectData = ResultsViewModelTestHelpers.CreateReadyProjectData();
            projectData.Version = "1.1";
            projectData.ProjectNumber = "SCHEMA-T6";
            var path = System.IO.Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"schema-t6-{System.Guid.NewGuid():N}.smc");

            try
            {
                var saveResult = await projectFileService.SaveProjectResultAsync(path, projectData);
                var json = await System.IO.File.ReadAllTextAsync(path);
                var loadResult = await projectFileService.LoadProjectResultAsync(path);

                Assert.Multiple(() =>
                {
                    Assert.That(saveResult.IsSuccess, Is.True, saveResult.Error);
                    Assert.That(loadResult.IsSuccess, Is.True, loadResult.Error);
                    Assert.That(loadResult.Value!.Version, Is.EqualTo("1.1"));
                    Assert.That(loadResult.Value.ProjectNumber, Is.EqualTo("SCHEMA-T6"));
                    Assert.That(json, Does.Contain("\"version\": \"1.1\""));
                    Assert.That(json, Does.Contain("\"climateData\""));
                    Assert.That(json, Does.Contain("\"thermalData\""));
                    Assert.That(json, Does.Contain("\"hydraulicsData\""));
                    Assert.That(json, Does.Not.Contain("ResultsSnapshot"));
                    Assert.That(json, Does.Not.Contain("resultsSnapshot"));
                });
            }
            finally
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
        }

        [Test]
        public async Task ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation()
        {
            var calculationStateService = new CalculationStateService(_projectStateService.Session);
            var climateVm = CreateClimateViewModelWithCity(
                "Текущий город", "Текущий регион", -29, 4.5, 72, _projectStateService.Session);
            climateVm.SelectedCity = new CityInfo { Name = "Исходный город", Region = "Исходный регион" };

            var constructionVm = await CreateInitializedConstructionViewModelAsync();
            var liveLayer = new Layer
            {
                Material = new Material { Name = "Live construction material", LambdaA = 1.7, LambdaB = 1.9 },
                Thickness = 85,
                Position = LayerPosition.AbovePipe
            };
            constructionVm.LayersAbovePipe.Add(liveLayer);

            var thermalCalculatorMock = new Mock<IThermalCalculator>();
            var thermalVm = CreateThermalViewModel(calculationStateService, _projectStateService, thermalCalculatorMock.Object);
            thermalVm.SelectedPipe = PipeType.StandardPipes.First(pipe => pipe.Name == "RAUTHERM S 25x2,3");
            thermalVm.PipeSpacing = 300;
            thermalVm.Result = new ThermalCalculationResult
            {
                PowerUp = 50,
                PowerDown = 50,
                PowerTotal = 100,
                SupplyTemperature = 45,
                ReturnTemperature = 35,
                MeanTemperature = 40,
                DeltaT = 10,
                IsValid = true
            };

            var circuitsVm = CreateCircuitsViewModel(
                calculationStateService,
                _projectStateService,
                projectSession: _projectStateService.Session);
            circuitsVm.InputData.GlycolType = GlycolType.Propylene;
            circuitsVm.InputData.GlycolConcentration = 42;
            circuitsVm.Collectors.Clear();
            var removedCollector = CreateCollectorForLifecycle(1, ValveType.HKV_D, 2, totalPower: 4000, totalLength: 80);
            var keptCollector = CreateCollectorForLifecycle(2, ValveType.IV_1_25, 3, totalPower: 9000, totalLength: 150);
            keptCollector.Circuits[0].CircuitLength = 61;
            keptCollector.Circuits[1].CircuitLength = 62;
            keptCollector.Circuits[2].CircuitLength = 63;
            circuitsVm.Collectors.Add(removedCollector);
            circuitsVm.Collectors.Add(keptCollector);
            circuitsVm.Collectors.Remove(removedCollector);
            keptCollector.CollectorNumber = 7;
            keptCollector.Circuits.RemoveAt(0);
            keptCollector.Circuits.Add(new CircuitRow
            {
                CircuitNumber = 3,
                CircuitLength = 77,
                SupplyLength = 11,
                SupplySpacing_cm = 6,
                SupplyHeatPercent = 12,
                PipeSpacing_cm = 30,
                Power = 3333
            });
            keptCollector.Summary = new CollectorSummary
            {
                CollectorNumber = 7,
                CircuitCount = keptCollector.Circuits.Count,
                TotalPipeLength = keptCollector.Circuits.Sum(circuit => circuit.CircuitLength),
                TotalPower = 9000,
                TotalFlowRate = 450,
                PressureLoss_Operating_Pa = 21000,
                PressureLoss_Cold_Pa = 42000,
                Kv = 1.3,
                CollectorType = "IV"
            };

            var viewModel = CreateViewModel(climateVm, constructionVm, thermalVm, circuitsVm, calculationStateService);
            viewModel.RefreshAll();
            Assert.That(viewModel.SelectedCity, Is.EqualTo("Исходный город"),
                "Sanity: Results cache starts stale before live module mutations are projected.");

            climateVm.SelectedCity = new CityInfo { Name = "Текущий город", Region = "Текущий регион" };
            climateVm.AirTemperature = -32;
            climateVm.WindSpeed = 8.5;
            climateVm.SnowfallIntensity = 2.25;
            liveLayer.Thickness = 95;
            thermalVm.SelectedPipe = PipeType.StandardPipes.First(pipe => pipe.Name == "RAUTHERM S 20x2,0");
            thermalVm.PipeSpacing = 250;
            circuitsVm.InputData.GlycolConcentration = 47;

            var saved = viewModel.SaveCurrentProject();
            viewModel.RefreshAll();
            var pdfData = GetField<ResultsPdfDataBuilder>(viewModel, "_resultsPdfDataBuilder").Build(viewModel);

            var reopenedCalculationStateService = new CalculationStateService(_projectStateService.Session);
            var reopenedViewModel = CreateViewModel(
                CreateClimateViewModelWithCity("Текущий город", "Текущий регион", -29, 4.5, 72, _projectStateService.Session),
                await CreateInitializedConstructionViewModelAsync(),
                CreateThermalViewModel(reopenedCalculationStateService, _projectStateService, new Mock<IThermalCalculator>().Object),
                CreateCircuitsViewModel(reopenedCalculationStateService, _projectStateService),
                reopenedCalculationStateService);

            await reopenedViewModel.LoadProjectDataAsync(saved);

            Assert.Multiple(() =>
            {
                Assert.That(saved.Version, Is.EqualTo("1.1"));
                Assert.That(saved.ClimateData.SelectedCity, Is.EqualTo("Текущий город"));
                Assert.That(saved.ClimateData.AirTemperature, Is.EqualTo(-32));
                Assert.That(saved.ClimateData.WindSpeed, Is.EqualTo(8.5));
                Assert.That(saved.ConstructionData.Layers.Select(layer => layer.MaterialName), Does.Contain("Live construction material"));
                Assert.That(saved.ThermalData.SelectedPipe!.Name, Is.EqualTo("RAUTHERM S 20x2,0"));
                Assert.That(saved.ThermalData.PipeSpacing, Is.EqualTo(250));
                Assert.That(saved.HydraulicsData.GlycolType, Is.EqualTo(GlycolType.Propylene));
                Assert.That(saved.HydraulicsData.GlycolConcentration, Is.EqualTo(47));
                Assert.That(saved.HydraulicsData.Collectors.Select(collector => collector.CollectorNumber), Is.EqualTo(new[] { 7 }));
                Assert.That(saved.HydraulicsData.Collectors[0].Circuits.Select(circuit => circuit.CircuitLength), Does.Contain(77));
                Assert.That(saved.HydraulicsData.Collectors[0].Circuits.Select(circuit => circuit.CircuitLength), Does.Not.Contain(61));
                Assert.That(viewModel.SelectedCity, Is.EqualTo("Текущий город"));
                Assert.That(viewModel.DesignTemperature, Is.EqualTo(-32));
                Assert.That(viewModel.PipeType, Is.EqualTo("RAUTHERM S 20x2,0"));
                Assert.That(viewModel.PipeSpacing, Is.EqualTo(250));
                Assert.That(viewModel.GlycolConcentration, Is.EqualTo(47));
                Assert.That(viewModel.Collectors.Select(collector => collector.Number), Is.EqualTo(new[] { 7 }));
                Assert.That(viewModel.Circuits.Select(circuit => circuit.CircuitLength), Does.Contain(77));
                Assert.That(viewModel.CollectorEquipmentItems.Single().CircuitCount, Is.EqualTo(3));
                Assert.That(pdfData.City, Is.EqualTo("Текущий город"));
                Assert.That(pdfData.PipeType, Is.EqualTo("RAUTHERM S 20x2,0"));
                Assert.That(pdfData.PipeSpacing, Is.EqualTo(250));
                Assert.That(pdfData.GlycolConcentration, Is.EqualTo(47));
                Assert.That(pdfData.Collectors.Select(collector => collector.Number), Is.EqualTo(new[] { 7 }));
                Assert.That(pdfData.CollectorSpecifications.Select(spec => spec.Number), Is.EqualTo(new[] { 7 }));
                Assert.That(pdfData.Layers.Select(layer => layer.MaterialName), Does.Contain("Live construction material"));
                Assert.That(reopenedViewModel.SelectedCity, Is.EqualTo("Текущий город"));
                Assert.That(reopenedViewModel.PipeSpacing, Is.EqualTo(250));
                Assert.That(reopenedViewModel.Collectors.Select(collector => collector.Number), Is.EqualTo(new[] { 7 }));
                Assert.That(_projectStateService.IsDirty, Is.False);
            });
            thermalCalculatorMock.Verify(
                calculator => calculator.Calculate(
                    It.IsAny<ThermalInputs>(),
                    It.IsAny<IClimateData>(),
                    It.IsAny<IConstructionData>()),
                Times.Never);
        }

        [Test]
        public async Task ProjectRoundTrip_PreservesGroundwaterLevel()
        {
            // Arrange
            var constructionVm = await CreateInitializedConstructionViewModelAsync();
            constructionVm.GroundwaterLevel = 0.5;

            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());

            // Act
            var data = viewModel.SaveCurrentProject();

            var constructionVm2 = await CreateInitializedConstructionViewModelAsync();
            var viewModel2 = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm2,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());
            await viewModel2.LoadProjectDataAsync(data);

            // Assert
            Assert.That(constructionVm2.GroundwaterLevel, Is.EqualTo(0.5).Within(1e-9));
        }

        [Test]
        public async Task ProjectRoundTrip_PreservesHasLoads()
        {
            // Arrange
            var constructionVm = await CreateInitializedConstructionViewModelAsync();
            constructionVm.HasLoads = true;

            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());

            // Act
            var data = viewModel.SaveCurrentProject();

            var constructionVm2 = await CreateInitializedConstructionViewModelAsync();
            var viewModel2 = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm2,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());
            await viewModel2.LoadProjectDataAsync(data);

            // Assert
            Assert.That(constructionVm2.HasLoads, Is.True);
        }

        [Test]
        public async Task ProjectRoundTrip_PreservesLambdaValueButResetsOverrideFlag()
        {
            // Arrange
            var constructionVm = await CreateInitializedConstructionViewModelAsync();
            var layer = constructionVm.LayersBelowPipe.First();
            layer.IsLambdaOverridden = true;
            layer.CalculatedLambda = 9.999;

            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());

            // Act
            var data = viewModel.SaveCurrentProject();

            var constructionVm2 = await CreateInitializedConstructionViewModelAsync();
            var viewModel2 = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm2,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());
            await viewModel2.LoadProjectDataAsync(data);

            // Assert: значение λ сохранено из файла, но флаг сброшен,
            // чтобы последующее изменение УГВ могло пересчитать λ (P0-7).
            var loadedLayer = constructionVm2.LayersBelowPipe.First();
            Assert.That(loadedLayer.IsLambdaOverridden, Is.False);
            Assert.That(loadedLayer.CalculatedLambda, Is.EqualTo(9.999).Within(1e-9));
        }

        [Test]
        public async Task ProjectRoundTrip_LambdaUpdatesWhenGroundwaterLevelChanges_AfterOverride()
        {
            // Arrange
            var constructionVm = await CreateInitializedConstructionViewModelAsync();
            constructionVm.GroundwaterLevel = 2.0; // dry
            var layer = constructionVm.LayersBelowPipe.First();
            layer.IsLambdaOverridden = true;
            layer.CalculatedLambda = layer.Material.LambdaA;

            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());

            // Act
            var data = viewModel.SaveCurrentProject();

            var constructionVm2 = await CreateInitializedConstructionViewModelAsync();
            var viewModel2 = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm2,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());
            await viewModel2.LoadProjectDataAsync(data);

            // After loading, changing groundwater level should update lambda (P0-7)
            constructionVm2.GroundwaterLevel = 0.5; // wet

            // Assert
            var loadedLayer = constructionVm2.LayersBelowPipe.First();
            Assert.That(loadedLayer.CalculatedLambda, Is.EqualTo(loadedLayer.Material.LambdaB).Within(1e-9));
        }

        [Test]
        public async Task GroundwaterLevelChange_AfterProjectLoad_UpdatesLambdaForBelowPipeLayers()
        {
            // Arrange
            var constructionVm = await CreateInitializedConstructionViewModelAsync();
            var layer = constructionVm.LayersBelowPipe.First();
            layer.IsLambdaOverridden = false;
            // GroundwaterLevel remains 2.0 m (dry conditions)

            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());

            // Act
            var data = viewModel.SaveCurrentProject();

            var constructionVm2 = await CreateInitializedConstructionViewModelAsync();
            var viewModel2 = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm2,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());
            await viewModel2.LoadProjectDataAsync(data);

            constructionVm2.GroundwaterLevel = 0.5;

            // Assert
            var loadedLayer = constructionVm2.LayersBelowPipe.First();
            Assert.That(loadedLayer.CalculatedLambda, Is.EqualTo(loadedLayer.Material.LambdaB).Within(1e-9));
        }

        [Test]
        public async Task ProjectRoundTrip_LambdaUpdatesWhenGroundwaterLevelChanges()
        {
            // Arrange
            var constructionVm = await CreateInitializedConstructionViewModelAsync();
            constructionVm.GroundwaterLevel = 0.5;
            var layer = constructionVm.LayersBelowPipe.First();
            layer.IsLambdaOverridden = false;
            layer.CalculatedLambda = layer.Material.LambdaA; // deliberately stale value

            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());

            // Act
            var data = viewModel.SaveCurrentProject();

            var constructionVm2 = await CreateInitializedConstructionViewModelAsync();
            var viewModel2 = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm2,
                CreateThermalViewModel(),
                CreateCircuitsViewModel());
            await viewModel2.LoadProjectDataAsync(data);

            // Assert
            var loadedLayer = constructionVm2.LayersBelowPipe.First();
            Assert.That(loadedLayer.CalculatedLambda, Is.EqualTo(loadedLayer.Material.LambdaB).Within(1e-9));
        }

        [Test]
        public async Task ResultsViewModel_LoadProject_TwoCollectors_RestoresIndependentSummaryCards()
        {
            // Arrange: проект с двумя коллекторами с различными гидравлическими итогами.
            // Маркеры A/B подобраны так, чтобы TotalPower различался и можно было
            // однозначно сопоставить карточку с исходным коллектором после загрузки.
            const double collectorAPower = 22700.0;
            const double collectorBPower = 20700.0;
            const double collectorAFlowRate = 1187.93;
            const double collectorBFlowRate = 1082.93;

            var projectData = new ProjectData
            {
                ProjectNumber = "P-TwoCollectorsCards",
                ProjectObject = "Two Collectors Cards Test",
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
                                new CircuitProjectData { CircuitNumber = 1, CircuitLength = 110, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 },
                                new CircuitProjectData { CircuitNumber = 2, CircuitLength = 110, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 },
                                new CircuitProjectData { CircuitNumber = 3, CircuitLength = 110, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 },
                                new CircuitProjectData { CircuitNumber = 4, CircuitLength = 105, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 }
                            },
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 4,
                                TotalPipeLength = 435,
                                TotalPower = collectorAPower,
                                TotalFlowRate = collectorAFlowRate,
                                PressureLoss_Operating_Pa = 36914.65,
                                PressureLoss_Cold_Pa = 125000,
                                Kv = 1.2,
                                CollectorType = "HKV-D"
                            }
                        },
                        new CollectorProjectData
                        {
                            CollectorNumber = 2,
                            CollectorType = "HKV-D (2-12 контуров)",
                            ValveType = ValveType.HKV_D,
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData { CircuitNumber = 1, CircuitLength = 100, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 },
                                new CircuitProjectData { CircuitNumber = 2, CircuitLength = 100, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 },
                                new CircuitProjectData { CircuitNumber = 3, CircuitLength = 100, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 },
                                new CircuitProjectData { CircuitNumber = 4, CircuitLength = 100, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 }
                            },
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 4,
                                TotalPipeLength = 400,
                                TotalPower = collectorBPower,
                                TotalFlowRate = collectorBFlowRate,
                                PressureLoss_Operating_Pa = 29159.16,
                                PressureLoss_Cold_Pa = 104100,
                                Kv = 1.2,
                                CollectorType = "HKV-D"
                            }
                        }
                    }
                }
            };

            var circuitsVm = CreateCircuitsViewModel(allowRemoveCircuit: true);
            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                CreateConstructionViewModel(_projectStateService.Session),
                CreateThermalViewModel(),
                circuitsVm);

            // Act
            await viewModel.LoadProjectDataAsync(projectData);
            viewModel.LoadHydraulicsDataOnNavigate();

            // Assert: ResultsViewModel должен экспонировать публичную коллекцию
            // HydraulicSummaryCards, содержащую по одной карточке на коллектор.
            // Используем reflection, чтобы тест был валиден (компилируемым) до
            // появления самого свойства в src/.
            var prop = typeof(ResultsViewModel).GetProperty(
                "HydraulicSummaryCards",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.That(prop, Is.Not.Null,
                "ResultsViewModel должен экспонировать публичное свойство 'HydraulicSummaryCards'.");

            var value = prop!.GetValue(viewModel);
            Assert.That(value, Is.Not.Null,
                "'HydraulicSummaryCards' должен быть инициализирован после LoadHydraulicsDataOnNavigate().");

            var cards = ((System.Collections.IEnumerable)value!).Cast<object>().ToList();
            Assert.That(cards.Count, Is.EqualTo(2),
                "HydraulicSummaryCards должен содержать по одной карточке на коллектор.");

            // Сопоставляем карточки с исходными коллекторами по уникальному TotalPower.
            var byPower = new Dictionary<double, object>();
            foreach (var card in cards)
            {
                var power = GetDoubleProperty(card, "TotalPower");
                Assert.That(power, Is.Not.Null,
                    "Каждая карточка должна предоставлять свойство 'TotalPower'.");
                byPower[power!.Value] = card;
            }

            Assert.That(byPower.ContainsKey(collectorAPower), Is.True,
                "HydraulicSummaryCards должен содержать карточку первого коллектора.");
            Assert.That(byPower.ContainsKey(collectorBPower), Is.True,
                "HydraulicSummaryCards должен содержать карточку второго коллектора.");

            // Независимость: значения карточки A не должны смешиваться с B и наоборот.
            AssertCardValues(
                byPower[collectorAPower],
                expectedCircuitCount: 4,
                expectedPipeLength: 435,
                expectedFlowRate: collectorAFlowRate,
                expectedOpPressurePa: 36914.65,
                expectedColdPressurePa: 125000,
                expectedKv: 1.2);
            AssertCardValues(
                byPower[collectorBPower],
                expectedCircuitCount: 4,
                expectedPipeLength: 400,
                expectedFlowRate: collectorBFlowRate,
                expectedOpPressurePa: 29159.16,
                expectedColdPressurePa: 104100,
                expectedKv: 1.2);
        }

        [Test]
        public void ResultsPdfData_UsesCircuitRowThrottling_ForZuDrosseln()
        {
            // Arrange: один коллектор с контуром, у которого CircuitRow.Throttling
            // и OperatingResult.ZuDrosseln намеренно различаются. PDF должен
            // использовать каноническое CircuitRow.Throttling (Па -> кПа).
            const double circuitThrottlingPa = 12345.0;
            const double operatingZuDrosselnPa = 99999.0;

            var circuitsVm = CreateCircuitsViewModel();
            circuitsVm.Collectors.Clear();

            var collector = new CollectorData(1);
            var circuit = new CircuitRow
            {
                CircuitNumber = 1,
                CircuitLength = 50,
                Throttling = circuitThrottlingPa
            };
            circuit.OperatingResult.ZuDrosseln = operatingZuDrosselnPa;
            collector.Circuits.Add(circuit);
            circuitsVm.Collectors.Add(collector);

            var constructionVm = CreateConstructionViewModel(_projectStateService.Session);
            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                constructionVm,
                CreateThermalViewModel(),
                circuitsVm);

            // Act: построение PDF-модели вынесено из ResultsViewModel
            // в ResultsPdfDataBuilder (этап C2) — вызываем его напрямую.
            var builder = new ResultsPdfDataBuilder(
                new Mock<IConstructionVisualizationImageService>().Object,
                new CalculationStateService(),
                constructionVm,
                circuitsVm);
            var pdfData = builder.Build(viewModel);

            // Assert
            Assert.That(pdfData.Collectors, Has.Count.EqualTo(1));
            Assert.That(pdfData.Collectors[0].Circuits, Has.Count.EqualTo(1));

            var circuitPdf = pdfData.Collectors[0].Circuits[0];
            Assert.That(circuitPdf.ZuDrosseln, Is.EqualTo(circuitThrottlingPa / 1000.0).Within(0.001),
                "PDF ZuDrosseln должен браться из CircuitRow.Throttling (Па -> кПа), а не из OperatingResult.ZuDrosseln.");
        }

        [Test]
        public async Task ResultsViewModel_Reset_ClearsHydraulicSummaryCards()
        {
            // Arrange: проект с двумя коллекторами, который заведомо оставляет
            // HydraulicSummaryCards непустыми и проставляет ненулевые legacy-скаляры
            // (TotalCircuits / TotalFlowRate / MaxPressureLoss) после LoadHydraulicsDataOnNavigate.
            var projectData = new ProjectData
            {
                ProjectNumber = "P-ResetClear",
                ProjectObject = "Reset Clear Test",
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
                                new CircuitProjectData { CircuitNumber = 1, CircuitLength = 110, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 },
                                new CircuitProjectData { CircuitNumber = 2, CircuitLength = 110, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 }
                            },
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 2,
                                TotalPipeLength = 220,
                                TotalPower = 12000,
                                TotalFlowRate = 600.5,
                                PressureLoss_Operating_Pa = 18000,
                                PressureLoss_Cold_Pa = 60000,
                                Kv = 1.2,
                                CollectorType = "HKV-D"
                            }
                        },
                        new CollectorProjectData
                        {
                            CollectorNumber = 2,
                            CollectorType = "HKV-D (2-12 контуров)",
                            ValveType = ValveType.HKV_D,
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData { CircuitNumber = 1, CircuitLength = 100, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 }
                            },
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 1,
                                TotalPipeLength = 100,
                                TotalPower = 5000,
                                TotalFlowRate = 250.25,
                                PressureLoss_Operating_Pa = 9000,
                                PressureLoss_Cold_Pa = 30000,
                                Kv = 1.2,
                                CollectorType = "HKV-D"
                            }
                        }
                    }
                }
            };

            var circuitsVm = CreateCircuitsViewModel(allowRemoveCircuit: true);
            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                CreateConstructionViewModel(_projectStateService.Session),
                CreateThermalViewModel(),
                circuitsVm);

            // Используем reflection для доступа к HydraulicSummaryCards,
            // чтобы тест был compile-clean, даже если сигнатура read-model изменится.
            var cardsProperty = typeof(ResultsViewModel).GetProperty(
                "HydraulicSummaryCards",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.That(cardsProperty, Is.Not.Null,
                "ResultsViewModel должен экспонировать публичное свойство 'HydraulicSummaryCards'.");

            // Act 1: загружаем проект и обновляем гидравлические данные,
            // как это делает UI при переходе на вкладку Results.
            await viewModel.LoadProjectDataAsync(projectData);
            viewModel.LoadHydraulicsDataOnNavigate();

            // Assert 1: после загрузки коллекция карточек непустая и legacy-скаляры
            // отражают данные выбранного коллектора. Это sanity-проверка, что мы
            // действительно «зашли» в состояние с заполненной read-model.
            var cardsBefore = ((System.Collections.IEnumerable)cardsProperty!.GetValue(viewModel)!)
                .Cast<object>().ToList();
            Assert.That(cardsBefore.Count, Is.EqualTo(2),
                "Sanity: после загрузки проекта HydraulicSummaryCards должен содержать 2 карточки.");
            Assert.That(viewModel.TotalCircuits, Is.GreaterThan(0),
                "Sanity: TotalCircuits должен быть > 0 после загрузки.");
            Assert.That(viewModel.TotalFlowRate, Is.GreaterThan(0.0),
                "Sanity: TotalFlowRate должен быть > 0 после загрузки.");
            Assert.That(viewModel.MaxPressureLoss, Is.GreaterThan(0.0),
                "Sanity: MaxPressureLoss должен быть > 0 после загрузки.");

            // Act 2: сбрасываем ViewModel в начальное состояние.
            // Это та же операция, что вызывается из MainWindow.PerformNewCalculationReset
            // и из ApplyLoadedProjectAsync перед загрузкой нового проекта.
            viewModel.Reset();

            // Assert 2: после Reset() коллекция карточек должна быть пустой,
            // и все legacy-скаляры (TotalCircuits / TotalFlowRate / MaxPressureLoss)
            // должны быть равны нулю, потому что CollectorSummary сбрасывается в null.
            var cardsAfter = ((System.Collections.IEnumerable)cardsProperty.GetValue(viewModel)!)
                .Cast<object>().ToList();
            Assert.That(cardsAfter, Is.Empty,
                "HydraulicSummaryCards должен быть пустым после Reset().");
            Assert.That(viewModel.TotalCircuits, Is.EqualTo(0),
                "TotalCircuits должен быть 0 после Reset().");
            Assert.That(viewModel.TotalFlowRate, Is.EqualTo(0.0),
                "TotalFlowRate должен быть 0 после Reset().");
            Assert.That(viewModel.MaxPressureLoss, Is.EqualTo(0.0),
                "MaxPressureLoss должен быть 0 после Reset().");
        }

        [Test]
        public async Task ResultsViewModel_EmptyHydraulics_ZeroesKpisAndCards()
        {
            // Arrange 1: проект с одним коллектором и ненулевыми гидравлическими итогами,
            // который заведомо оставляет HydraulicSummaryCards непустыми и проставляет
            // ненулевые legacy-скаляры (TotalThermalPower_kW / TotalFlowRate /
            // MaxPressureLoss) после LoadProjectDataAsync.
            var populated = new ProjectData
            {
                ProjectNumber = "P-Populated",
                ProjectObject = "Populated Hydraulics",
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
                                new CircuitProjectData { CircuitNumber = 1, CircuitLength = 110, SupplyLength = 10, SupplySpacingCm = 5, SupplyHeatPercent = 10, PipeSpacingCm = 20 }
                            },
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 1,
                                TotalPipeLength = 110,
                                TotalPower = 12000,
                                TotalFlowRate = 600.5,
                                PressureLoss_Operating_Pa = 18000,
                                PressureLoss_Cold_Pa = 60000,
                                Kv = 1.2,
                                CollectorType = "HKV-D"
                            }
                        }
                    }
                }
            };

            // Arrange 2: проект с пустой HydraulicsData.Collectors — именно он
            // должен оставить HydraulicSummaryCards пустыми и обнулить все
            // гидравлические KPI после полного цикла RefreshAll().
            var empty = new ProjectData
            {
                ProjectNumber = "P-Empty",
                ProjectObject = "Empty Hydraulics",
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData(),
                ConstructionData = new ConstructionProjectData(),
                ThermalData = new ThermalProjectData(),
                HydraulicsData = new HydraulicsProjectData() // Collectors = new List<>()
            };

            var circuitsVm = CreateCircuitsViewModel(allowRemoveCircuit: true);
            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                CreateConstructionViewModel(_projectStateService.Session),
                CreateThermalViewModel(),
                circuitsVm);

            var cardsProperty = typeof(ResultsViewModel).GetProperty(
                "HydraulicSummaryCards",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.That(cardsProperty, Is.Not.Null,
                "ResultsViewModel должен экспонировать публичное свойство 'HydraulicSummaryCards'.");

            // Act 1: загружаем заполненный проект и обновляем гидравлику
            // тем же путём, что и UI при открытии вкладки Results.
            await viewModel.LoadProjectDataAsync(populated);
            viewModel.LoadHydraulicsDataOnNavigate();

            // Assert 1 (sanity): после заполненного проекта KPI и карточки не пусты.
            // Это гарантирует, что последующие нулевые проверки действительно
            // измеряют «обнуление stale-состояния», а не «исходно нулевое состояние».
            var cardsBefore = ((System.Collections.IEnumerable)cardsProperty!.GetValue(viewModel)!)
                .Cast<object>().ToList();
            Assert.That(cardsBefore, Is.Not.Empty,
                "Sanity: HydraulicSummaryCards должен быть непустым после загрузки заполненного проекта.");
            Assert.That(viewModel.TotalThermalPower_kW, Is.GreaterThan(0.0),
                "Sanity: TotalThermalPower_kW должен быть > 0 после загрузки заполненного проекта.");
            Assert.That(viewModel.TotalFlowRate, Is.GreaterThan(0.0),
                "Sanity: TotalFlowRate должен быть > 0 после загрузки заполненного проекта.");
            Assert.That(viewModel.MaxPressureLoss, Is.GreaterThan(0.0),
                "Sanity: MaxPressureLoss должен быть > 0 после загрузки заполненного проекта.");

            // Act 2: загружаем проект с пустыми HydraulicsData.Collectors.
            // Это НЕ вызывает Reset() (Reset() вызывается в ApplyLoadedProjectAsync,
            // а мы идём через публичный LoadProjectDataAsync, чтобы протестировать
            // именно путь RefreshAll() — production-поверхность, которая также
            // достигается при навигации на вкладку Results после ApplyLoadedProjectAsync).
            await viewModel.LoadProjectDataAsync(empty);
            viewModel.LoadHydraulicsDataOnNavigate();

            // Assert 2: после загрузки проекта с пустыми коллекторами
            // HydraulicSummaryCards и все гидравлические KPI должны быть обнулены.
            // Без минимального фикса CalculateTotalPower() возвращает ранее
            // досчитанное значение при `_circuitsViewModel.Collectors.Count == 0`,
            // поэтому проверка TotalThermalPower_kW ловит regression.
            var cardsAfter = ((System.Collections.IEnumerable)cardsProperty.GetValue(viewModel)!)
                .Cast<object>().ToList();
            Assert.That(cardsAfter, Is.Empty,
                "HydraulicSummaryCards должен быть пустым после загрузки проекта без коллекторов.");
            Assert.That(viewModel.TotalThermalPower_kW, Is.EqualTo(0.0),
                "TotalThermalPower_kW должен быть 0 после загрузки проекта без коллекторов.");
            Assert.That(viewModel.TotalFlowRate, Is.EqualTo(0.0),
                "TotalFlowRate должен быть 0 после загрузки проекта без коллекторов.");
            Assert.That(viewModel.MaxPressureLoss, Is.EqualTo(0.0),
                "MaxPressureLoss должен быть 0 после загрузки проекта без коллекторов.");
        }

        /// <summary>
        /// F5 smoke test (plan «results-hydraulic-card-stale-state.md»,
        /// final verification wave): грузит реальный файл проекта
        /// «тест 40.smc» через настоящий <see cref="ProjectFileService"/>
        /// (без моков) и проверяет, что <c>ResultsViewModel.HydraulicSummaryCards</c>
        /// содержит две карточки с реальными итогами по коллекторам.
        /// Тест НЕ требует selector/mode switching: оба коллектора видны
        /// сразу после LoadProjectDataAsync + LoadHydraulicsDataOnNavigate.
        /// </summary>
        [Test]
        public async Task ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile()
        {
            // Arrange: путь к реальному файлу проекта (F5 fixture).
            const string RealProjectPath = @"D:\IA\ace\Тест\тест 40.smc";

            // Если fixture-файл ещё не подготовлен, тест корректно skip-ается
            // (а не падает), чтобы F5-проверка оставалась compileable и discoverable
            // в любом состоянии репозитория. Когда файл появится в «Тест/»,
            // тест автоматически начнёт валидировать реальные значения.
            // Создавать файл в «Тест/» запрещено (MUST NOT из задачи).
            if (!System.IO.File.Exists(RealProjectPath))
            {
                Assert.Ignore($"F5 smoke fixture не найден: {RealProjectPath}. " +
                              "Положите файл «тест 40.smc» в «D:\\IA\\ace\\Тест\\», " +
                              "и тест начнёт проверять реальные значения.");
                return;
            }

            // Act 1: грузим JSON через настоящий ProjectFileService.LoadProjectResultAsync.
            // Это та же production-поверхность, что использует ResultsViewModel.LoadProjectFromPathAsync.
            var projectFileService = new ProjectFileService();
            var loadResult = await projectFileService.LoadProjectResultAsync(RealProjectPath);
            Assert.That(loadResult.IsSuccess, Is.True,
                $"ProjectFileService.LoadProjectResultAsync должен успешно прочитать {RealProjectPath}. " +
                $"Ошибка: {loadResult.Error}");
            Assert.That(loadResult.Value, Is.Not.Null,
                "ProjectFileService.LoadProjectResultAsync должен вернуть ненулевой ProjectData.");
            var projectData = loadResult.Value!;

            // Sanity: fixture должен содержать ровно два коллектора, как указано в задаче.
            Assert.That(projectData.HydraulicsData, Is.Not.Null,
                "HydraulicsData должен присутствовать в fixture-файле.");
            Assert.That(projectData.HydraulicsData.Collectors, Is.Not.Null,
                "HydraulicsData.Collectors должен быть инициализирован.");
            Assert.That(projectData.HydraulicsData.Collectors.Count, Is.EqualTo(2),
                "Fixture «тест 40.smc» должен содержать ровно два коллектора (A и B) с разными итогами.");

            // Act 2: создаём ResultsViewModel тем же fixture-helper, что и остальные тесты,
            // и загружаем проект через публичный production-путь:
            // LoadProjectDataAsync (через него же ходит LoadProjectFromPathAsync → ApplyLoadedProjectAsync).
            // Сразу после — LoadHydraulicsDataOnNavigate(), который перестраивает
            // HydraulicSummaryCards из _circuitsViewModel.Collectors.
            var circuitsVm = CreateCircuitsViewModel(allowRemoveCircuit: true);
            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                CreateConstructionViewModel(_projectStateService.Session),
                CreateThermalViewModel(),
                circuitsVm);

            await viewModel.LoadProjectDataAsync(projectData);
            viewModel.LoadHydraulicsDataOnNavigate();

            // Assert 1: HydraulicSummaryCards должен быть инициализирован и непуст.
            var cardsProperty = typeof(ResultsViewModel).GetProperty(
                "HydraulicSummaryCards",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.That(cardsProperty, Is.Not.Null,
                "ResultsViewModel должен экспонировать публичное свойство 'HydraulicSummaryCards'.");
            var cardsEnumerable = (System.Collections.IEnumerable)cardsProperty!.GetValue(viewModel)!;
            var cards = cardsEnumerable.Cast<object>().ToList();

            Assert.That(cards, Is.Not.Empty,
                "HydraulicSummaryCards должен содержать карточки после LoadHydraulicsDataOnNavigate.");
            Assert.That(cards.Count, Is.EqualTo(2),
                "HydraulicSummaryCards должен содержать ровно 2 карточки — по одной на коллектор (A и B).");

            // Assert 2: каждая карточка должна однозначно идентифицироваться по
            // TotalPower, как и в regression-тесте плана #1.
            // В fixture (по спецификации задачи) ожидаются TotalPower = 22700 / 20700.
            // Если в будущем fixture изменится — собираем словарь, и в конце
            // проверяем, что обе ожидаемые power-метки присутствуют.
            const double collectorAPower = 22700.0;
            const double collectorBPower = 20700.0;
            const double expectedCollectorALength = 435.0;
            const double expectedCollectorBLength = 400.0;
            const double expectedCollectorAFlow = 1187.93;
            const double expectedCollectorBFlow = 1082.93;
            const double expectedCollectorAOpPa = 36914.65;
            const double expectedCollectorBOpPa = 29159.16;

            var byPower = new Dictionary<double, object>();
            foreach (var card in cards)
            {
                var power = GetDoubleProperty(card, "TotalPower");
                Assert.That(power, Is.Not.Null,
                    "Каждая HydraulicSummaryCard должна предоставлять свойство 'TotalPower' для идентификации.");
                byPower[power!.Value] = card;
            }

            Assert.That(byPower.ContainsKey(collectorAPower), Is.True,
                $"HydraulicSummaryCards должен содержать карточку коллектора A (TotalPower={collectorAPower}). " +
                "Если fixture-файл изменился, обновите expected-значения в этом тесте.");
            Assert.That(byPower.ContainsKey(collectorBPower), Is.True,
                $"HydraulicSummaryCards должен содержать карточку коллектора B (TotalPower={collectorBPower}). " +
                "Если fixture-файл изменился, обновите expected-значения в этом тесте.");

            // Assert 3: значения карточек коллекторов A и B должны соответствовать
            // ожидаемым реальным значениям. Толерантности подобраны по задаче:
            // length ±0.1 м, flow ±0.01 л/ч, pressure ±0.1 Па. Они достаточно
            // свободны, чтобы выдержать возможное округление при JSON round-trip,
            // и при этом достаточно плотные, чтобы поймать подмену карточек.
            var cardA = byPower[collectorAPower];
            Assert.That(GetIntProperty(cardA, "CircuitCount"), Is.EqualTo(4),
                "Card A: CircuitCount должен быть 4 (по fixture «тест 40.smc»).");
            Assert.That(GetDoubleProperty(cardA, "TotalPipeLength"), Is.EqualTo(expectedCollectorALength).Within(0.1),
                $"Card A: TotalPipeLength должен быть ≈{expectedCollectorALength} м (по fixture).");
            Assert.That(GetDoubleProperty(cardA, "TotalFlowRate"), Is.EqualTo(expectedCollectorAFlow).Within(0.01),
                $"Card A: TotalFlowRate должен быть ≈{expectedCollectorAFlow} л/ч (по fixture).");
            Assert.That(GetDoubleProperty(cardA, "PressureLoss_Operating_Pa"), Is.EqualTo(expectedCollectorAOpPa).Within(0.1),
                $"Card A: OperatingPressureLossPa должен быть ≈{expectedCollectorAOpPa} Па (по fixture).");

            var cardB = byPower[collectorBPower];
            Assert.That(GetIntProperty(cardB, "CircuitCount"), Is.EqualTo(4),
                "Card B: CircuitCount должен быть 4 (по fixture «тест 40.smc»).");
            Assert.That(GetDoubleProperty(cardB, "TotalPipeLength"), Is.EqualTo(expectedCollectorBLength).Within(0.1),
                $"Card B: TotalPipeLength должен быть ≈{expectedCollectorBLength} м (по fixture).");
            Assert.That(GetDoubleProperty(cardB, "TotalFlowRate"), Is.EqualTo(expectedCollectorBFlow).Within(0.01),
                $"Card B: TotalFlowRate должен быть ≈{expectedCollectorBFlow} л/ч (по fixture).");
            Assert.That(GetDoubleProperty(cardB, "PressureLoss_Operating_Pa"), Is.EqualTo(expectedCollectorBOpPa).Within(0.1),
                $"Card B: OperatingPressureLossPa должен быть ≈{expectedCollectorBOpPa} Па (по fixture).");

            // Assert 4 (smoke-инвариант плана F5): карточки НЕ должны требовать
            // selector/mode switching — оба коллектора должны быть видны сразу
            // после LoadProjectDataAsync + LoadHydraulicsDataOnNavigate без
            // какого-либо выбора/переключения. Это покрывается выше через
            // Assert.That(cards.Count, Is.EqualTo(2)) без обращения к
            // SelectedCollectorIndex / IsOperatingMode.
        }

        private static void AssertCardValues(
            object card,
            int expectedCircuitCount,
            double expectedPipeLength,
            double expectedFlowRate,
            double expectedOpPressurePa,
            double expectedColdPressurePa,
            double expectedKv)
        {
            Assert.That(GetIntProperty(card, "CircuitCount"), Is.EqualTo(expectedCircuitCount),
                "CircuitCount на HydraulicSummaryCard должен соответствовать коллектору.");
            Assert.That(GetDoubleProperty(card, "TotalPipeLength"), Is.EqualTo(expectedPipeLength).Within(0.001),
                "TotalPipeLength на HydraulicSummaryCard должен соответствовать коллектору.");
            Assert.That(GetDoubleProperty(card, "TotalFlowRate"), Is.EqualTo(expectedFlowRate).Within(0.001),
                "TotalFlowRate на HydraulicSummaryCard должен соответствовать коллектору.");
            Assert.That(GetDoubleProperty(card, "PressureLoss_Operating_Pa"), Is.EqualTo(expectedOpPressurePa).Within(0.01),
                "PressureLoss_Operating_Pa на HydraulicSummaryCard должен соответствовать коллектору.");
            Assert.That(GetDoubleProperty(card, "PressureLoss_Cold_Pa"), Is.EqualTo(expectedColdPressurePa).Within(0.01),
                "PressureLoss_Cold_Pa на HydraulicSummaryCard должен соответствовать коллектору.");
            Assert.That(GetDoubleProperty(card, "Kv"), Is.EqualTo(expectedKv).Within(0.001),
                "Kv на HydraulicSummaryCard должен соответствовать коллектору.");
        }

        private static double? GetDoubleProperty(object obj, string name)
        {
            var p = obj.GetType().GetProperty(
                name,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (p == null) return null;
            var v = p.GetValue(obj);
            if (v == null) return null;
            return Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int? GetIntProperty(object obj, string name)
        {
            var p = obj.GetType().GetProperty(
                name,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (p == null) return null;
            var v = p.GetValue(obj);
            if (v == null) return null;
            return Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture);
        }


        private async Task<ConstructionViewModel> CreateInitializedConstructionViewModelAsync()
        {
            var vm = CreateConstructionViewModel(_projectStateService.Session);
            await vm.InitializeCommand.ExecuteAsync(null);
            return vm;
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
            return CreateViewModel(climateVm, CreateConstructionViewModel(_projectStateService.Session), CreateThermalViewModel(), circuitsVm);
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

            var calculationStateService = new CalculationStateService(_projectStateService.Session);
            var calculationContext = new CalculationContext();

            return new ResultsViewModel(
                _projectStateService,
                _projectStateService.Session,
                _projectStateService,
                _dialogServiceMock.Object,
                new Mock<IPdfExportService>().Object,
                new Mock<ICalculationReportExportService>().Object,
                _projectFileServiceMock.Object,
                calculationStateService,
                materialRepositoryMock.Object,
                constructionServiceMock.Object,
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
                    calculationContext,
                    _projectStateService.Session,
                    CreateDefaultConstructionInitializer(_projectStateService.Session)),
                new ResultsPdfDataBuilder(
                    new Mock<IConstructionVisualizationImageService>().Object,
                    calculationStateService,
                    constructionVm,
                    circuitsVm),
                new HydraulicSummaryBuilder());
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
            double humidity,
            IProjectSession? projectSession = null)
        {
            return CreateClimateViewModelWithCityAndSingleton(
                new ClimateData(),
                cityName,
                region,
                t5Days,
                windAvg,
                humidity,
                projectSession);
        }

        private static ClimateViewModel CreateClimateViewModelWithCityAndSingleton(
            IClimateData climateData,
            string cityName,
            string region,
            double t5Days,
            double windAvg,
            double humidity,
            IProjectSession? projectSession = null)
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

            if (projectSession != null)
            {
                return new ClimateViewModel(
                    climateServiceMock.Object,
                    climateData,
                    new ClimateValidator(),
                    projectSession);
            }

            return new ClimateViewModel(
                climateServiceMock.Object,
                climateData,
                new ClimateValidator(),
                new Mock<IMarkDirtyService>().Object,
                new CalculationContext());
        }

        private static ConstructionViewModel CreateConstructionViewModel(IProjectSession? projectSession = null)
        {
            var materialsById = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
            foreach (var material in new[]
            {
                new Material { Id = 1, Name = "Sand", LambdaA = 0.8, LambdaB = 0.9 },
                new Material { Id = 2, Name = "Soil", LambdaA = 1.0, LambdaB = 1.1 },
                new Material { Id = 5, Name = "Concrete", LambdaA = 1.5, LambdaB = 1.6 }
            })
            {
                materialsById[material.Id] = material;
            }

            var materials = materialsById.Values.ToList();

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);
            materialRepositoryMock.Setup(r => r.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materialsById.GetValueOrDefault(id));

            var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());
            var calculationContext = new CalculationContext();
            projectSession ??= new ProjectSession(calculationContext: calculationContext);

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepositoryMock.Object,
                new Mock<IConstructionRepository>().Object,
                new CalculationStateService(projectSession),
                calculationContext,
                new ConstructionValidator(),
                new ConstructionModel(),
                new Mock<IMarkDirtyService>().Object,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object,
                projectSession.ConstructionState,
                new ConstructionDefaultStateInitializer(materialRepositoryMock.Object, projectSession.ConstructionState));
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

            var calculationStateService = new CalculationStateService();
            var calculationContext = new CalculationContext();
            var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(calculationStateService, calculationContext);
            return new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                calculationStateService,
                validatorMock.Object,
                selectorMock.Object,
                calculationContext,
                new Mock<IMarkDirtyService>().Object,
                hydraulicsDependencies.Coordinator,
                hydraulicsDependencies.Session);
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

            var calculationContext = new CalculationContext();

            return new ResultsViewModel(
                _projectStateService,
                _projectStateService.Session,
                _projectStateService,
                _dialogServiceMock.Object,
                new Mock<IPdfExportService>().Object,
                new Mock<ICalculationReportExportService>().Object,
                _projectFileServiceMock.Object,
                calculationStateService,
                materialRepositoryMock.Object,
                constructionServiceMock.Object,
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
                    calculationContext,
                    _projectStateService.Session,
                    CreateDefaultConstructionInitializer(_projectStateService.Session)),
                new ResultsPdfDataBuilder(
                    new Mock<IConstructionVisualizationImageService>().Object,
                    calculationStateService,
                    constructionVm,
                    circuitsVm),
                new HydraulicSummaryBuilder());
        }

        private static ConstructionDefaultStateInitializer CreateDefaultConstructionInitializer(IProjectSession session)
        {
            var materials = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
            var repository = new Mock<IMaterialRepository>();
            repository.Setup(candidate => candidate.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materials.GetValueOrDefault(id));
            return new ConstructionDefaultStateInitializer(repository.Object, session.ConstructionState);
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
            IMarkDirtyService markDirtyService,
            IProjectSession? projectSession = null)
        {
            var materialsById = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
            foreach (var material in new[]
            {
                new Material { Id = 1, Name = "Sand", LambdaA = 0.8, LambdaB = 0.9 },
                new Material { Id = 2, Name = "Soil", LambdaA = 1.0, LambdaB = 1.1 },
                new Material { Id = 5, Name = "Concrete", LambdaA = 1.5, LambdaB = 1.6 }
            })
            {
                materialsById[material.Id] = material;
            }
            var materials = materialsById.Values.ToList();

            var materialRepositoryMock = new Mock<IMaterialRepository>();
            materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);
            materialRepositoryMock.Setup(r => r.GetMaterialById(It.IsAny<int>()))
                .Returns((int id) => materialsById.GetValueOrDefault(id));

            var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
            templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());
            var calculationContext = new CalculationContext();
            projectSession ??= new ProjectSession(calculationContext: calculationContext);

            return new ConstructionViewModel(
                new Mock<IConstructionService>().Object,
                materialRepositoryMock.Object,
                new Mock<IConstructionRepository>().Object,
                calculationStateService,
                calculationContext,
                new ConstructionValidator(),
                new ConstructionModel(),
                markDirtyService,
                templateRepositoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<IEditorDialogService>().Object,
                projectSession.ConstructionState,
                new ConstructionDefaultStateInitializer(materialRepositoryMock.Object, projectSession.ConstructionState));
        }

        private static ThermalViewModel CreateThermalViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService)
        {
            return CreateThermalViewModel(
                calculationStateService,
                markDirtyService,
                new Mock<IThermalCalculator>().Object);
        }

        private static ThermalViewModel CreateThermalViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService,
            IThermalCalculator thermalCalculator)
        {
            var climateData = new ClimateData();
            var constructionData = new ConstructionData();
            var thermalValidatorMock = new Mock<IValidator<ThermalInputs>>();
            thermalValidatorMock
                .Setup(validator => validator.Validate(It.IsAny<ThermalInputs>()))
                .Returns(ValidationResult.Success());
            return new ThermalViewModel(
                thermalCalculator,
                climateData,
                constructionData,
                calculationStateService,
                new CalculationContext(),
                thermalValidatorMock.Object,
                new ThermalResultValidator(),
                markDirtyService);
        }

        private static CircuitsViewModel CreateCircuitsViewModel(
            CalculationStateService calculationStateService,
            IMarkDirtyService markDirtyService,
            bool allowRemoveCircuit = false,
            IProjectSession? projectSession = null)
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

            var calculationContext = new CalculationContext();
            var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(
                calculationStateService,
                calculationContext,
                projectSession);
            return new CircuitsViewModel(
                calculatorMock.Object,
                glycolMock.Object,
                calculationStateService,
                validatorMock.Object,
                selectorMock.Object,
                calculationContext,
                markDirtyService,
                hydraulicsDependencies.Coordinator,
                hydraulicsDependencies.Session);
        }

        private static CollectorData CreateCollectorForLifecycle(
            int collectorNumber,
            ValveType valveType,
            int circuitCount,
            double totalPower,
            double totalLength)
        {
            var collector = ResultsViewModelTestHelpers.CreateCollector(collectorNumber, valveType, circuitCount);
            collector.Summary = new CollectorSummary
            {
                CollectorNumber = collectorNumber,
                CircuitCount = circuitCount,
                TotalPipeLength = totalLength,
                TotalPower = totalPower,
                TotalFlowRate = totalPower / 20,
                PressureLoss_Operating_Pa = totalPower * 2,
                PressureLoss_Cold_Pa = totalPower * 4,
                Kv = 1.2,
                CollectorType = valveType == ValveType.HKV_D ? "HKV-D" : "IV"
            };
            return collector;
        }

        private static T GetField<T>(object instance, string fieldName) where T : class
        {
            return (T)instance.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(instance)!;
        }

        [Test]
        public async Task LoadProjectFromPathAsync_WhenNullPath_DoesNothing()
        {
            // Arrange
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));

            // Act
            await _viewModel.LoadProjectFromPathAsync(null!);

            // Assert
            _projectFileServiceMock.Verify(
                p => p.LoadProjectResultAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task LoadProjectFromPathAsync_WhenEmptyPath_DoesNothing()
        {
            // Arrange
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));

            // Act
            await _viewModel.LoadProjectFromPathAsync(string.Empty);

            // Assert
            _projectFileServiceMock.Verify(
                p => p.LoadProjectResultAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task LoadProjectFromPathAsync_WhenFileNotFound_ShowsError()
        {
            // Arrange
            const string errorMessage = "Файл не найден";
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Failure(errorMessage));

            // Act
            await _viewModel.LoadProjectFromPathAsync(TestFilePath);

            // Assert
            _dialogServiceMock.Verify(
                d => d.ShowError($"Не удалось открыть проект: {errorMessage}", "Ошибка"),
                Times.Once);
            _projectFileServiceMock.Verify(
                p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()),
                Times.Once);
            Assert.That(_projectStateService.CurrentFilePath, Is.Null);
        }

        [Test]
        public async Task LoadProjectFromPathAsync_WhenSuccess_LoadsDataAndSetsCurrentFilePath()
        {
            // Arrange
            _projectFileServiceMock
                .Setup(p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<ProjectData>.Success(new ProjectData()));

            // Act
            await _viewModel.LoadProjectFromPathAsync(TestFilePath);

            // Assert
            _projectFileServiceMock.Verify(
                p => p.LoadProjectResultAsync(TestFilePath, It.IsAny<CancellationToken>()),
                Times.Once);
            Assert.That(_projectStateService.CurrentFilePath, Is.EqualTo(TestFilePath));
            Assert.That(_projectStateService.IsDirty, Is.False);
        }

        [Test]
        public async Task LoadProjectData_MissingOrInvalidThermalResult_UsesLoadOnlyFallbackAndRefreshDoesNotRecalculate()
        {
            foreach (var savedResult in new ThermalResultProjectData?[]
            {
                null,
                new ThermalResultProjectData { PowerTotal = 999, IsValid = false }
            })
            {
                var calculationStateService = new CalculationStateService(_projectStateService.Session);
                var thermalCalculatorMock = new Mock<IThermalCalculator>();
                thermalCalculatorMock
                    .Setup(calculator => calculator.Calculate(
                        It.IsAny<ThermalInputs>(),
                        It.IsAny<IClimateData>(),
                        It.IsAny<IConstructionData>()))
                    .Returns(new ThermalCalculationResult
                    {
                        PowerUp = 111,
                        PowerDown = 222,
                        PowerTotal = 333,
                        SupplyTemperature = 55,
                        ReturnTemperature = 44,
                        MeanTemperature = 49.5,
                        DeltaT = 11,
                        IsValid = true
                    });
                var thermalVm = CreateThermalViewModel(calculationStateService, _projectStateService, thermalCalculatorMock.Object);
                var viewModel = CreateViewModel(
                    CreateClimateViewModelWithCity("Тестовый город", "Тестовый регион", -25, 3, 70),
                    CreateConstructionViewModel(_projectStateService.Session),
                    thermalVm,
                    CreateCircuitsViewModel(calculationStateService, _projectStateService),
                    calculationStateService);
                var projectData = ResultsViewModelTestHelpers.CreateReadyProjectData();
                projectData.ThermalData.Result = savedResult;
                projectData.ThermalData.SelectedPipe = new PipeTypeProjectData
                {
                    Name = "RAUTHERM S 20x2,0",
                    OuterDiameter = 20,
                    InnerDiameter = 16,
                    WallThickness = 2
                };

                await viewModel.LoadProjectDataAsync(projectData);

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.TotalPowerDensity, Is.EqualTo(333));
                    Assert.That(viewModel.SupplyTemperature, Is.EqualTo(55));
                    Assert.That(_projectStateService.IsDirty, Is.False);
                });
                thermalCalculatorMock.Verify(
                    calculator => calculator.Calculate(
                        It.IsAny<ThermalInputs>(),
                        It.IsAny<IClimateData>(),
                        It.IsAny<IConstructionData>()),
                    Times.Once);

                viewModel.RefreshAll();

                Assert.That(viewModel.TotalPowerDensity, Is.EqualTo(333));
                thermalCalculatorMock.Verify(
                    calculator => calculator.Calculate(
                        It.IsAny<ThermalInputs>(),
                        It.IsAny<IClimateData>(),
                        It.IsAny<IConstructionData>()),
                    Times.Once,
                    "Results refresh must remain non-calculating after load-only fallback.");
            }
        }

        /// <summary>
        /// Todo 9 / DEC-T08 «second load»: повторная загрузка проекта без
        /// сохранённого результата полностью заменяет Thermal-состояние проекта A
        /// (входы/результат/статус) и выполняет ровно один fallback-расчёт.
        /// </summary>
        [Test]
        public async Task LoadProjectData_SecondLoadWithoutSavedResult_ReplacesAllThermalStaleValues()
        {
            var calculationStateService = new CalculationStateService(_projectStateService.Session);
            var thermalCalculatorMock = new Mock<IThermalCalculator>();
            thermalCalculatorMock
                .Setup(calculator => calculator.Calculate(
                    It.IsAny<ThermalInputs>(),
                    It.IsAny<IClimateData>(),
                    It.IsAny<IConstructionData>()))
                .Returns(new ThermalCalculationResult
                {
                    PowerUp = 11,
                    PowerDown = 22,
                    PowerTotal = 42.5,
                    SupplyTemperature = 45,
                    ReturnTemperature = 34,
                    MeanTemperature = 39.5,
                    DeltaT = 11,
                    IsValid = true
                });
            var thermalVm = CreateThermalViewModel(calculationStateService, _projectStateService, thermalCalculatorMock.Object);
            var viewModel = CreateViewModel(
                CreateClimateViewModelWithCity("Тестовый город", "Тестовый регион", -25, 3, 70),
                CreateConstructionViewModel(_projectStateService.Session),
                thermalVm,
                CreateCircuitsViewModel(calculationStateService, _projectStateService),
                calculationStateService);

            var projectA = ResultsViewModelTestHelpers.CreateReadyProjectData();
            projectA.ProjectNumber = "SEC-A";
            projectA.ThermalData.SelectedMode = OperatingMode.Intensive;
            projectA.ThermalData.SupplyTemperature = 60.0;
            projectA.ThermalData.GroundTemperature = 5.0;
            projectA.ThermalData.PipeSpacing = 300;
            projectA.ThermalData.SelectedPipe = new PipeTypeProjectData
            {
                Name = "RAUTHERM S 25x2,3",
                OuterDiameter = 25,
                InnerDiameter = 20.4,
                WallThickness = 2.3
            };
            projectA.ThermalData.Result = new ThermalResultProjectData { PowerTotal = 777.0, IsValid = true };

            await viewModel.LoadProjectDataAsync(projectA);
            Assert.That(_projectStateService.Session.ThermalState.Snapshot.Result!.PowerTotal, Is.EqualTo(777.0),
                "Sanity: first load publishes the valid saved result.");

            var projectB = ResultsViewModelTestHelpers.CreateReadyProjectData();
            projectB.ProjectNumber = "SEC-B";
            projectB.ThermalData.SelectedMode = OperatingMode.Melting;
            projectB.ThermalData.SupplyTemperature = 45.0;
            projectB.ThermalData.GroundTemperature = 2.0;
            projectB.ThermalData.PipeSpacing = 150;
            projectB.ThermalData.SelectedPipe = null;
            projectB.ThermalData.Result = null;

            await viewModel.LoadProjectDataAsync(projectB);

            var snapshot = _projectStateService.Session.ThermalState.Snapshot;
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Inputs.Mode, Is.EqualTo(OperatingMode.Melting));
                Assert.That(snapshot.Inputs.SupplyTemperature, Is.EqualTo(45.0));
                Assert.That(snapshot.Inputs.GroundTemperature, Is.EqualTo(2.0));
                Assert.That(snapshot.Inputs.PipeSpacing, Is.EqualTo(150));
                Assert.That(snapshot.Inputs.Pipe, Is.Null);
                Assert.That(snapshot.Result, Is.Not.Null);
                Assert.That(snapshot.Result!.PowerTotal, Is.EqualTo(42.5),
                    "Fresh fallback result must replace every project-A Thermal value.");
                Assert.That(snapshot.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
                Assert.That(thermalVm.Result!.PowerTotal, Is.EqualTo(42.5));
                Assert.That(_projectStateService.IsDirty, Is.False);
            });
            thermalCalculatorMock.Verify(
                calculator => calculator.Calculate(
                    It.IsAny<ThermalInputs>(),
                    It.IsAny<IClimateData>(),
                    It.IsAny<IConstructionData>()),
                Times.Once,
                "Valid saved result must not calculate; absent result must fall back exactly once.");
        }

        /// <summary>
        /// Регрессионный тест (драфт fix-load-project-climate-kpi-temperatures):
        /// KPI температур должны отражать финальный тепловой результат сразу после
        /// LoadProjectDataAsync — без ручного повторного выбора города на вкладке «Климат».
        /// Раньше RefreshAll() вызывался ДО финального расчёта, и на вкладке «Результаты»
        /// оставался снимок, снятый до него.
        /// </summary>
        [Test]
        public async Task LoadProjectData_KpiReflectSavedThermalResult_WithoutCityReselection()
        {
            // Arrange — проект с валидным сохранённым тепловым результатом (как в перм.smc)
            var projectData = new ProjectData
            {
                ProjectNumber = "P-KPI",
                ProjectObject = "KPI After Load Test",
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData
                {
                    SelectedCity = "Москва",
                    AirTemperature = -15.0,
                    WindSpeed = 2.7,
                    Humidity = 77.0,
                    SnowfallIntensity = 1.0,
                    SelectedZone = ClimateZone.Zone_M15,
                    IsHighRequirements = false
                },
                ConstructionData = new ConstructionProjectData(),
                ThermalData = new ThermalProjectData
                {
                    SelectedMode = OperatingMode.Melting,
                    SupplyTemperature = 60.0,
                    GroundTemperature = 10.0,
                    PipeSpacing = 200,
                    Result = new ThermalResultProjectData
                    {
                        PowerUp = 357.5,
                        PowerDown = 5.8,
                        PowerTotal = 363.3,
                        SupplyTemperature = 60.0,
                        ReturnTemperature = 44.31,
                        MeanTemperature = 52.16,
                        DeltaT = 15.69,
                        IsValid = true
                    }
                },
                HydraulicsData = new HydraulicsProjectData()
            };

            var climateVm = CreateClimateViewModel();
            var circuitsVm = CreateCircuitsViewModel();
            var viewModel = CreateViewModel(
                climateVm,
                CreateConstructionViewModel(_projectStateService.Session),
                CreateThermalViewModel(),
                circuitsVm);

            // Act — только загрузка проекта, БЕЗ повторного выбора города
            await viewModel.LoadProjectDataAsync(projectData);

            // Assert — KPI вкладки «Результаты» соответствуют сохранённому результату
            Assert.That(viewModel.SupplyTemperature, Is.EqualTo(60.0).Within(0.01),
                "KPI температуры подачи должен быть из финального результата, а не нулевой снимок");
            Assert.That(viewModel.ReturnTemperature, Is.EqualTo(44.31).Within(0.01));
            Assert.That(viewModel.OperatingTemperature, Is.EqualTo(52.16).Within(0.01));
            Assert.That(viewModel.TotalPowerDensity, Is.EqualTo(363.3).Within(0.01));
        }

        /// <summary>
        /// Регрессионный тест: загрузка проекта не должна помечать климат как
        /// «изменённый пользователем» — параметры восстановлены из файла.
        /// </summary>
        [Test]
        public async Task LoadProjectData_ClimateIsNotMarkedAsUserModified()
        {
            // Arrange
            var projectData = new ProjectData
            {
                ProjectNumber = "P-CLIM",
                IsOperatingMode = true,
                ClimateData = new ClimateProjectData
                {
                    SelectedCity = "Москва",
                    AirTemperature = -15.0,
                    WindSpeed = 2.7,
                    Humidity = 77.0,
                    SnowfallIntensity = 1.0,
                    SelectedZone = ClimateZone.Zone_M15,
                    IsHighRequirements = false
                },
                ConstructionData = new ConstructionProjectData(),
                ThermalData = new ThermalProjectData(),
                HydraulicsData = new HydraulicsProjectData()
            };

            var climateVm = CreateClimateViewModel();
            var circuitsVm = CreateCircuitsViewModel();
            var viewModel = CreateViewModel(climateVm, circuitsVm);

            // Act
            await viewModel.LoadProjectDataAsync(projectData);

            // Assert
            Assert.That(climateVm.HasUserModifications, Is.False,
                "Восстановление климата из файла не должно выглядеть как ручная правка");
            Assert.That(_projectStateService.IsDirty, Is.False,
                "После загрузки проекта состояние должно быть чистым");
        }

        // === Todo 10 (DEC-T08): canonical Thermal save/read seams ===

        private static readonly IReadOnlyList<PipeType> StandardPipes = PipeType.StandardPipes;

        private static ThermalInputsSnapshot CreateCanonicalInputs()
        {
            var standard = StandardPipes.First(p => p.Name == "RAUTHERM S 20x2,0");
            return new ThermalInputsSnapshot(
                OperatingMode.Intensive,
                60.0,
                5.0,
                ThermalPipeSnapshot.FromPipeType(standard),
                250);
        }

        private static ThermalResultSnapshot CreateCanonicalResult(double powerTotal)
        {
            return ThermalResultSnapshot.FromResult(new ThermalCalculationResult
            {
                PowerUp = powerTotal - 5.8,
                PowerDown = 5.8,
                PowerTotal = powerTotal,
                SupplyTemperature = 60.0,
                ReturnTemperature = 44.31,
                MeanTemperature = 52.16,
                DeltaT = 15.69,
                IsValid = true
            })!;
        }

        private static System.Text.Json.JsonSerializerOptions CreateProductionJsonOptions()
        {
            // Те же опции, что и save-путь ProjectFileService.
            return new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new System.Text.Json.Serialization.JsonStringEnumConverter(
                        System.Text.Json.JsonNamingPolicy.CamelCase)
                }
            };
        }

        /// <summary>
        /// Todo 10: save читает только канонический ThermalState snapshot —
        /// расходящийся зеркальный кэш ThermalViewModel (труба/шаг/температуры/
        /// режим/результат, изменённый под load-guard) не попадает в .smc.
        /// </summary>
        [Test]
        public async Task SaveCurrentProject_PersistsThermalStateSnapshot_NotThermalViewModelMirror()
        {
            var calculationStateService = new CalculationStateService(_projectStateService.Session);
            var thermalVm = CreateThermalViewModel(calculationStateService, _projectStateService);
            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                CreateConstructionViewModel(_projectStateService.Session),
                thermalVm,
                CreateCircuitsViewModel(calculationStateService, _projectStateService),
                calculationStateService);

            // Канонические значения X — через canonical state API.
            var inputsX = CreateCanonicalInputs();
            var resultX = CreateCanonicalResult(363.3);
            _projectStateService.Session.ThermalState.Restore(inputsX, resultX);

            // Зеркальные значения Y — только в VM-кэше: под load-guard правки
            // адаптера не маршрутизируются в каноническое состояние.
            calculationStateService.IsLoadProjectInProgress = true;
            thermalVm.SelectedPipe = StandardPipes.First(p => p.Name == "RAUTHERM S 25x2,3");
            thermalVm.PipeSpacing = 150;
            thermalVm.SupplyTemperature = 45.0;
            thermalVm.GroundTemperature = 2.0;
            thermalVm.SelectedMode = OperatingMode.Melting;
            thermalVm.Result = new ThermalCalculationResult { PowerTotal = 999, IsValid = true };
            calculationStateService.IsLoadProjectInProgress = false;

            // Sanity: зеркало действительно расходится с каноном.
            Assert.That(thermalVm.PipeSpacing, Is.Not.EqualTo(inputsX.PipeSpacing));
            Assert.That(thermalVm.Result!.PowerTotal, Is.Not.EqualTo(resultX.PowerTotal));

            // Act
            var saved = viewModel.SaveCurrentProject();

            // Assert: сохранены ровно канонические значения X.
            Assert.Multiple(() =>
            {
                Assert.That(saved.ThermalData.SelectedMode, Is.EqualTo(OperatingMode.Intensive));
                Assert.That(saved.ThermalData.SupplyTemperature, Is.EqualTo(60.0));
                Assert.That(saved.ThermalData.GroundTemperature, Is.EqualTo(5.0));
                Assert.That(saved.ThermalData.PipeSpacing, Is.EqualTo(250));
                Assert.That(saved.ThermalData.SelectedPipe!.Name, Is.EqualTo("RAUTHERM S 20x2,0"));
                Assert.That(saved.ThermalData.Result!.PowerTotal, Is.EqualTo(363.3));
                Assert.That(saved.ThermalData.Result.IsValid, Is.True);
                Assert.That(saved.ThermalData.Result.PowerTotal, Is.Not.EqualTo(999),
                    "VM mirror result must never leak into the saved wire DTO.");
            });
        }

        /// <summary>
        /// Todo 10: save/export не запускают расчёты — ноль вызовов калькулятора
        /// на SaveCurrentProject + RefreshAll + построение PDF-модели.
        /// </summary>
        [Test]
        public async Task SaveCurrentProject_AndPdfExport_TriggerZeroThermalCalculatorCalls()
        {
            var calculationStateService = new CalculationStateService(_projectStateService.Session);
            var calculatorMock = new Mock<IThermalCalculator>();
            var thermalVm = CreateThermalViewModel(calculationStateService, _projectStateService, calculatorMock.Object);
            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                CreateConstructionViewModel(_projectStateService.Session),
                thermalVm,
                CreateCircuitsViewModel(calculationStateService, _projectStateService),
                calculationStateService);

            _projectStateService.Session.ThermalState.Restore(
                CreateCanonicalInputs(),
                CreateCanonicalResult(363.3));
            // Публикуем результат в проекцию адаптера так же, как оркестратор
            // восстановления (LoadResult-путь): KPI Results читают текущую
            // проекцию адаптера, save читает канонический snapshot.
            thermalVm.Result = new ThermalCalculationResult
            {
                PowerUp = 357.5,
                PowerDown = 5.8,
                PowerTotal = 363.3,
                SupplyTemperature = 60.0,
                ReturnTemperature = 44.31,
                MeanTemperature = 52.16,
                DeltaT = 15.69,
                IsValid = true
            };

            // Act: save + refresh + PDF/export-модель.
            var saved = viewModel.SaveCurrentProject();
            viewModel.RefreshAll();
            var pdfData = GetField<ResultsPdfDataBuilder>(viewModel, "_resultsPdfDataBuilder").Build(viewModel);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(saved.ThermalData.Result!.PowerTotal, Is.EqualTo(363.3));
                Assert.That(viewModel.TotalPowerDensity, Is.EqualTo(363.3).Within(0.01));
            });
            calculatorMock.Verify(
                calculator => calculator.Calculate(
                    It.IsAny<ThermalInputs>(),
                    It.IsAny<IClimateData>(),
                    It.IsAny<IConstructionData>()),
                Times.Never,
                "Save/export must never trigger a Thermal calculation.");
        }

        [Test]
        [Category("PersistenceFailure")]
        public async Task PersistenceFailure_UnknownPipe_FallsBackToFirstStandard_NoSchemaDrift()
        {
            var calculationStateService = new CalculationStateService(_projectStateService.Session);
            var calculatorMock = new Mock<IThermalCalculator>();
            calculatorMock
                .Setup(calculator => calculator.Calculate(
                    It.IsAny<ThermalInputs>(),
                    It.IsAny<IClimateData>(),
                    It.IsAny<IConstructionData>()))
                .Returns(new ThermalCalculationResult
                {
                    PowerUp = 11, PowerDown = 22, PowerTotal = 42.5,
                    SupplyTemperature = 45, ReturnTemperature = 34,
                    MeanTemperature = 39.5, DeltaT = 11, IsValid = true
                });
            var thermalVm = CreateThermalViewModel(calculationStateService, _projectStateService, calculatorMock.Object);
            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                CreateConstructionViewModel(_projectStateService.Session),
                thermalVm,
                CreateCircuitsViewModel(calculationStateService, _projectStateService),
                calculationStateService);

            var projectA = ResultsViewModelTestHelpers.CreateReadyProjectData();
            // Валидные входные температуры: иначе замороженная атомарная валидация
            // отклонит кандидата (supply < 20) и сработает путь "дефолты +
            // сохранённый результат", а не fallback трубы.
            projectA.ThermalData.SupplyTemperature = 45.0;
            projectA.ThermalData.GroundTemperature = 5.0;
            projectA.ThermalData.SelectedPipe = new PipeTypeProjectData
            {
                Name = "PHASE4 UNKNOWN PIPE",
                OuterDiameter = 99,
                InnerDiameter = 95,
                WallThickness = 2.0
            };
            projectA.ThermalData.Result = new ThermalResultProjectData
            {
                PowerUp = 100, PowerDown = 100, PowerTotal = 200,
                SupplyTemperature = 45, ReturnTemperature = 35,
                MeanTemperature = 40, DeltaT = 10, IsValid = true
            };

            await viewModel.LoadProjectDataAsync(projectA);

            // Frozen fallback: неизвестная труба → первая стандартная; валидный
            // сохранённый результат публикуется без пересчёта.
            var snapshot = _projectStateService.Session.ThermalState.Snapshot;
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Inputs.Pipe!.Name, Is.EqualTo(StandardPipes[0].Name));
                Assert.That(snapshot.Result!.PowerTotal, Is.EqualTo(200.0));
            });
            calculatorMock.Verify(
                calculator => calculator.Calculate(
                    It.IsAny<ThermalInputs>(),
                    It.IsAny<IClimateData>(),
                    It.IsAny<IConstructionData>()),
                Times.Never);

            // Повторное сохранение не даёт schema drift: набор свойств тот же.
            var saved = viewModel.SaveCurrentProject();
            Assert.That(saved.ThermalData.SelectedPipe!.Name, Is.EqualTo(StandardPipes[0].Name));

            var json = System.Text.Json.JsonSerializer.Serialize(
                saved.ThermalData, CreateProductionJsonOptions());
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.Multiple(() =>
            {
                Assert.That(doc.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray(),
                    Is.EqualTo(new[]
                    {
                        "groundTemperature", "pipeSpacing", "result", "selectedMode",
                        "selectedPipe", "supplyTemperature"
                    }));
                Assert.That(json, Does.Not.Contain("article"));
                Assert.That(json, Does.Not.Contain("thermalConductivity"));
            });

            // Перезагрузка сохранённого файла: fallback воспроизводится семантически.
            var restoredInputs = ThermalPersistenceMapper.BuildInputsCandidate(
                saved.ThermalData, PipeType.StandardPipes);
            Assert.That(restoredInputs.Pipe!.Name, Is.EqualTo(StandardPipes[0].Name));
        }

        [Test]
        [Category("PersistenceFailure")]
        public async Task PersistenceFailure_MissingOrCorruptSavedResult_FallbackOnce_InvalidNeverCanonical()
        {
            foreach (var brokenResult in new ThermalResultProjectData?[]
            {
                null,
                new ThermalResultProjectData { PowerTotal = 999, IsValid = false }
            })
            {
                _projectStateService = new ProjectStateService();
                var calculationStateService = new CalculationStateService(_projectStateService.Session);
                var calculatorMock = new Mock<IThermalCalculator>();
                calculatorMock
                    .Setup(calculator => calculator.Calculate(
                        It.IsAny<ThermalInputs>(),
                        It.IsAny<IClimateData>(),
                        It.IsAny<IConstructionData>()))
                    .Returns(new ThermalCalculationResult
                    {
                        PowerUp = 11, PowerDown = 22, PowerTotal = 42.5,
                        SupplyTemperature = 45, ReturnTemperature = 34,
                        MeanTemperature = 39.5, DeltaT = 11, IsValid = true
                    });
                var thermalVm = CreateThermalViewModel(calculationStateService, _projectStateService, calculatorMock.Object);
                var viewModel = CreateViewModel(
                    CreateClimateViewModel(),
                    CreateConstructionViewModel(_projectStateService.Session),
                    thermalVm,
                    CreateCircuitsViewModel(calculationStateService, _projectStateService),
                    calculationStateService);

                var projectA = ResultsViewModelTestHelpers.CreateReadyProjectData();
                // Валидные входные температуры: fallback-расчёт должен быть вызван
                // отсутствием/невалидностью результата, а не отклонением кандидата.
                projectA.ThermalData.SupplyTemperature = 45.0;
                projectA.ThermalData.GroundTemperature = 5.0;
                projectA.ThermalData.Result = brokenResult;

                await viewModel.LoadProjectDataAsync(projectA);

                // Frozen fallback/error state: ровно один fallback-расчёт;
                // повреждённый/отсутствующий результат не становится каноническим.
                var snapshot = _projectStateService.Session.ThermalState.Snapshot;
                Assert.Multiple(() =>
                {
                    Assert.That(snapshot.Result, Is.Not.Null);
                    Assert.That(snapshot.Result!.IsValid, Is.True);
                    Assert.That(snapshot.Result.PowerTotal, Is.EqualTo(42.5),
                        "Fallback result must replace the missing/corrupt saved value.");
                    Assert.That(_projectStateService.IsDirty, Is.False);
                });
                calculatorMock.Verify(
                    calculator => calculator.Calculate(
                        It.IsAny<ThermalInputs>(),
                        It.IsAny<IClimateData>(),
                        It.IsAny<IConstructionData>()),
                    Times.Once);

                // Сохранение после fallback персистит именно fallback-результат
                // (не повреждённые 999) с точным восьмиполевым контрактом.
                var saved = viewModel.SaveCurrentProject();
                Assert.Multiple(() =>
                {
                    Assert.That(saved.ThermalData.Result, Is.Not.Null);
                    Assert.That(saved.ThermalData.Result!.PowerTotal, Is.EqualTo(42.5));
                    Assert.That(saved.ThermalData.Result.IsValid, Is.True);
                });
            }
        }

        [Test]
        [Category("PersistenceFailure")]
        public async Task PersistenceFailure_FailedFileOperation_PreservesErrorStateWithoutSchemaDrift()
        {
            var calculationStateService = new CalculationStateService(_projectStateService.Session);
            var thermalVm = CreateThermalViewModel(calculationStateService, _projectStateService);
            var viewModel = CreateViewModel(
                CreateClimateViewModel(),
                CreateConstructionViewModel(_projectStateService.Session),
                thermalVm,
                CreateCircuitsViewModel(calculationStateService, _projectStateService),
                calculationStateService);

            var inputsX = CreateCanonicalInputs();
            var resultX = CreateCanonicalResult(363.3);
            _projectStateService.Session.ThermalState.Restore(inputsX, resultX);
            var beforeSave = _projectStateService.Session.ThermalState.Snapshot;

            // Неуспешная файловая операция: несуществующая директория → Failure.
            var fileService = new ProjectFileService();
            var badPath = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "no-such-dir",
                $"persistence-failure-{Guid.NewGuid():N}.smc");
            var saveResult = await fileService.SaveProjectResultAsync(badPath, viewModel.SaveCurrentProject());

            Assert.Multiple(() =>
            {
                Assert.That(saveResult.IsSuccess, Is.False, "Missing target directory must fail the save.");
                Assert.That(File.Exists(badPath), Is.False);
                // Frozen error state: каноническое состояние не тронуто.
                Assert.That(_projectStateService.Session.ThermalState.Snapshot, Is.EqualTo(beforeSave));
            });

            // Успешное сохранение тех же данных не даёт schema drift.
            var saved = viewModel.SaveCurrentProject();
            var json = System.Text.Json.JsonSerializer.Serialize(
                saved.ThermalData, CreateProductionJsonOptions());
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.That(doc.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray(),
                Is.EqualTo(new[]
                {
                    "groundTemperature", "pipeSpacing", "result", "selectedMode",
                    "selectedPipe", "supplyTemperature"
                }));
        }
    }
}

        [Test]
        [Category("PersistenceCharacterization")]
        public async Task SaveProject_Success_StampsDatesAndClearsDirtyOnce()
        {
            _projectStateService.CurrentFilePath = TestFilePath;
            _projectStateService.MarkDirty();

            var startedAt = DateTime.Now;
            var previousIsDirty = _projectStateService.IsDirty;
            var cleanTransitions = 0;
            _projectStateService.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != nameof(IProjectStateService.IsDirty))
                {
                    return;
                }

                var isDirty = _projectStateService.IsDirty;
                if (previousIsDirty && !isDirty)
                {
                    cleanTransitions++;
                }

                previousIsDirty = isDirty;
            };

            ProjectData? savedData = null;
            _projectFileServiceMock
                .Setup(service => service.SaveProjectResultAsync(
                    TestFilePath,
                    It.IsAny<ProjectData>(),
                    It.IsAny<CancellationToken>()))
                .Callback((string path, ProjectData data, CancellationToken cancellationToken) => savedData = data)
                .ReturnsAsync(OperationResult<object?>.Success(null));

            await _viewModel.SaveProjectCommand.ExecuteAsync(null);

            var completedAt = DateTime.Now;
            Assert.Multiple(() =>
            {
                Assert.That(savedData, Is.Not.Null);
                Assert.That(savedData!.CreatedDate, Is.InRange(startedAt, completedAt));
                Assert.That(savedData.ModifiedDate, Is.InRange(startedAt, completedAt));
                Assert.That(_projectStateService.IsDirty, Is.False);
                Assert.That(cleanTransitions, Is.EqualTo(1));
            });

            _projectFileServiceMock.Verify(
                service => service.SaveProjectResultAsync(
                    TestFilePath,
                    It.IsAny<ProjectData>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _dialogServiceMock.Verify(
                service => service.ShowError(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Test]
        [Category("PersistenceCharacterization")]
        public async Task SaveProject_Failure_PreservesDirtyStateAndShowsError()
        {
            _projectStateService.CurrentFilePath = TestFilePath;
            _projectStateService.MarkDirty();

            var previousIsDirty = _projectStateService.IsDirty;
            var cleanTransitions = 0;
            _projectStateService.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != nameof(IProjectStateService.IsDirty))
                {
                    return;
                }

                var isDirty = _projectStateService.IsDirty;
                if (previousIsDirty && !isDirty)
                {
                    cleanTransitions++;
                }

                previousIsDirty = isDirty;
            };

            const string error = "injected save failure";
            _projectFileServiceMock
                .Setup(service => service.SaveProjectResultAsync(
                    TestFilePath,
                    It.IsAny<ProjectData>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult<object?>.Failure(error));

            await _viewModel.SaveProjectCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(_projectStateService.IsDirty, Is.True);
                Assert.That(cleanTransitions, Is.Zero);
            });

            _projectFileServiceMock.Verify(
                service => service.SaveProjectResultAsync(
                    TestFilePath,
                    It.IsAny<ProjectData>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _dialogServiceMock.Verify(
                service => service.ShowError($"Не удалось сохранить проект: {error}", "Ошибка"),
                Times.Once);
        }
