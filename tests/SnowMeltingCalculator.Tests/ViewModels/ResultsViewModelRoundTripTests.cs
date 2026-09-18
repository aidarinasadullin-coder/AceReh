using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.Tests.Fixtures;
using static SnowMeltingCalculator.Tests.ViewModels.ResultsViewModelTestGraph;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Полный round-trip проекта через ResultsViewModel (save → load) и
    /// кластер lambda/groundwater (волна C плана 2026-09-18 — консолидация
    /// 5 → 2 отдельным циклом; при разрезке кластер временно живёт здесь).
    /// Волна A «чистки раздутых тестов»: только перенос дословно.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ResultsViewModelRoundTripTests
    {
        private ProjectStateService _projectStateService = null!;
        private Mock<IDialogService> _dialogServiceMock = null!;
        private Mock<IProjectFileService> _projectFileServiceMock = null!;
        private Mock<IConstructionService> _constructionServiceMock = null!;
        private ResultsViewModel _viewModel = null!;

        [SetUp]
        public void SetUp()
        {
            _projectStateService = new ProjectStateService();
            _dialogServiceMock = new Mock<IDialogService>();
            _projectFileServiceMock = new Mock<IProjectFileService>();
            _constructionServiceMock = new Mock<IConstructionService>();
            _viewModel = CreateViewModel();
        }

        private ResultsViewModel CreateViewModel()
        {
            return ResultsViewModelTestGraph.CreateViewModel(
                _projectStateService.Session,
                _dialogServiceMock,
                _projectFileServiceMock,
                _constructionServiceMock,
                CreateClimateViewModel(),
                CreateConstructionViewModel(_projectStateService.Session),
                CreateThermalViewModel(),
                CreateCircuitsViewModel());
        }

        private ResultsViewModel CreateViewModel(
            ClimateViewModel climateVm,
            ConstructionViewModel constructionVm,
            ThermalViewModel thermalVm,
            CircuitsViewModel circuitsVm)
        {
            return ResultsViewModelTestGraph.CreateViewModel(
                _projectStateService.Session,
                _dialogServiceMock,
                _projectFileServiceMock,
                _constructionServiceMock,
                climateVm,
                constructionVm,
                thermalVm,
                circuitsVm);
        }

        private ResultsViewModel CreateViewModel(
            ClimateViewModel climateVm,
            ConstructionViewModel constructionVm,
            ThermalViewModel thermalVm,
            CircuitsViewModel circuitsVm,
            CalculationStateService calculationStateService)
        {
            return ResultsViewModelTestGraph.CreateViewModel(
                _projectStateService.Session,
                _dialogServiceMock,
                _projectFileServiceMock,
                climateVm,
                constructionVm,
                thermalVm,
                circuitsVm,
                calculationStateService);
        }

        private async Task<ConstructionViewModel> CreateInitializedConstructionViewModelAsync()
        {
            var vm = CreateConstructionViewModel(_projectStateService.Session);
            await vm.InitializeCommand.ExecuteAsync(null);
            return vm;
        }

        [Test]
        public async Task ProjectRoundTrip_PipeSelectionRestored()
        {
            // Arrange
            const string pipeName = "RAUTHERM S 25x2,3";
            var projectData = new ProjectDataBuilder()
                .WithNumber("P-T4")
                .WithObject("Pipe Restore Test")
                .OperatingMode()
                .WithDefaultSlices()
                .WithThermal(thermal => thermal.SelectedPipe = new PipeTypeProjectData
                {
                    Name = pipeName,
                    OuterDiameter = 25.0,
                    InnerDiameter = 20.4,
                    WallThickness = 2.3
                })
                .Build();

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
            var projectData = new ProjectDataBuilder()
                .WithNumber("P-T6")
                .WithObject("Dirty Load Test")
                .OperatingMode()
                .WithDefaultSlices()
                .WithClimate(climate =>
                {
                    climate.SelectedCity = "Москва";
                    climate.AirTemperature = -18.0;
                    climate.WindSpeed = 3.5;
                    climate.Humidity = 65.0;
                    climate.SnowfallIntensity = 2.5;
                    climate.SelectedZone = ClimateZone.Zone_M15;
                    climate.IsHighRequirements = false;
                })
                .WithConstruction(construction =>
                {
                    construction.R1 = 0.1;
                    construction.R2 = 0.2;
                })
                .WithThermal(thermal =>
                {
                    thermal.SelectedMode = OperatingMode.Melting;
                    thermal.SupplyTemperature = 45.0;
                    thermal.GroundTemperature = 5.0;
                    thermal.PipeSpacing = 250;
                    thermal.SelectedPipe = new PipeTypeProjectData
                    {
                        Name = "RAUTHERM S 20x2,0",
                        OuterDiameter = 20.0,
                        InnerDiameter = 16.0,
                        WallThickness = 2.0
                    };
                    thermal.Result = new ThermalResultProjectData
                    {
                        PowerUp = 100.0,
                        PowerDown = 100.0,
                        PowerTotal = 200.0,
                        SupplyTemperature = 45.0,
                        ReturnTemperature = 35.0,
                        MeanTemperature = 40.0,
                        DeltaT = 10.0,
                        IsValid = true
                    };
                })
                .WithHydraulics(hydraulics =>
                {
                    hydraulics.GlycolType = GlycolType.Ethylene;
                    hydraulics.GlycolConcentration = 30.0;
                    hydraulics.SupplySpacingCm = 10.0;
                    hydraulics.SupplyHeatPercent = 20.0;
                    var collector = ProjectDataBuilder.HkvDCollector(1, 50.0);
                    collector.Circuits[0].PipeSpacingCm = 25.0;
                    hydraulics.Collectors.Add(collector);
                })
                .Build();

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
                        WindMaxJan = 4.0,
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
                    // Ручные переопределения λ переживают загрузку: флаги из
                    // .smc восстанавливаются как есть (план 2026-09-04, D5;
                    // отменяет прежнее P0-7 «restore resets override flags»).
                    Assert.That(roundTrippedA.LayersAbovePipe.Concat(roundTrippedA.LayersBelowPipe).All(layer => layer.IsLambdaOverridden), Is.True,
                        ".smc stores override flags, and project restore preserves them together with the lambda values.");
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
                    Assert.That(roundTrippedB.LayersAbovePipe.Select(layer => (layer.Order, layer.MaterialId, layer.MaterialName, layer.Thickness, layer.CalculatedLambda, layer.IsLambdaOverridden)),
                        Is.EqualTo(new[] { (0, 2, "Soil", 17d, 1.07, true) }));
                    Assert.That(roundTrippedB.LayersBelowPipe.Select(layer => (layer.Order, layer.MaterialId, layer.MaterialName, layer.Thickness, layer.CalculatedLambda, layer.IsLambdaOverridden)),
                        Is.EqualTo(new[] { (0, 1, "Sand", 318d, 0.93, true) }));
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

            // Phase 8: ThermalViewModel.Result — адаптерное зеркало (канон не пишет).
            // Канонический эквивалент «живой» мутации результата — публикация через
            // ThermalState (как делает координатор при расчёте): иначе save/restore
            // и готовность по канону не увидят результат.
            var liveSession = _projectStateService.Session;
            liveSession.ThermalState.CompleteCalculation(
                liveSession.ThermalState.Snapshot.Inputs,
                ThermalResultSnapshot.FromResult(new ThermalCalculationResult
                {
                    PowerUp = 50,
                    PowerDown = 50,
                    PowerTotal = 100,
                    SupplyTemperature = 45,
                    ReturnTemperature = 35,
                    MeanTemperature = 40,
                    DeltaT = 10,
                    IsValid = true
                })!,
                string.Empty);

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

        /// <summary>
        /// Волна C (план 2026-09-18 «чистка раздутых тестов», §3): слияние
        /// ProjectRoundTrip_PreservesGroundwaterLevel и
        /// ProjectRoundTrip_PreservesLambdaValueAndOverrideFlag — round-trip
        /// сохраняет уровень грунтовых вод, а при ручном override — и значение λ,
        /// и флаг (план 2026-09-04, D5; отменяет прежнее P0-7
        /// «флаг сбрасывается при загрузке»).
        /// </summary>
        [TestCase(0.5, false, 0.0)]
        [TestCase(2.0, true, 9.999)]
        public async Task ProjectRoundTrip_PreservesGroundwaterLevel_AndLambdaOverrideAfterLoad(
            double groundwaterLevel,
            bool overrideLambda,
            double manualLambda)
        {
            // Arrange
            var constructionVm = await CreateInitializedConstructionViewModelAsync();
            constructionVm.GroundwaterLevel = groundwaterLevel;
            if (overrideLambda)
            {
                var layer = constructionVm.LayersBelowPipe.First();
                layer.IsLambdaOverridden = true;
                layer.CalculatedLambda = manualLambda;
            }

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

            // Assert: УГВ переживает round-trip; при override переживают и λ, и флаг.
            Assert.That(constructionVm2.GroundwaterLevel, Is.EqualTo(groundwaterLevel).Within(1e-9));
            if (overrideLambda)
            {
                var loadedLayer = constructionVm2.LayersBelowPipe.First();
                Assert.That(loadedLayer.IsLambdaOverridden, Is.True);
                Assert.That(loadedLayer.CalculatedLambda, Is.EqualTo(manualLambda).Within(1e-9));
            }
        }

        /// <summary>
        /// Волна C: слияние ProjectRoundTrip_OverrideLambdaSurvivesGroundwaterLevelChange_AfterLoad,
        /// GroundwaterLevelChange_AfterProjectLoad_UpdatesLambdaForBelowPipeLayers и
        /// ProjectRoundTrip_LambdaUpdatesWhenGroundwaterLevelChanges. Три сценария
        /// одного взаимодействия «УГВ ↔ λ»; каждый — на свежей сессии (прецедент
        /// FieldCompleteRoundTrip), все ассерты исходных трёх сохранены.
        /// </summary>
        [Test]
        public async Task ProjectRoundTrip_GroundwaterLevelChange_OverrideLambdaProtected_OthersRecalculated()
        {
            // Сценарий A: ручная λ защищена флагом — после load переключение УГВ
            // её НЕ пересчитывает (план 2026-09-04, D5).
            _projectStateService = new ProjectStateService();
            {
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

                var data = viewModel.SaveCurrentProject();

                var constructionVm2 = await CreateInitializedConstructionViewModelAsync();
                var viewModel2 = CreateViewModel(
                    CreateClimateViewModel(),
                    constructionVm2,
                    CreateThermalViewModel(),
                    CreateCircuitsViewModel());
                await viewModel2.LoadProjectDataAsync(data);

                constructionVm2.GroundwaterLevel = 0.5; // wet

                var loadedLayer = constructionVm2.LayersBelowPipe.First();
                Assert.That(loadedLayer.IsLambdaOverridden, Is.True);
                Assert.That(loadedLayer.CalculatedLambda, Is.EqualTo(loadedLayer.Material.LambdaA).Within(1e-9));
            }

            // Сценарий B: слой без override — после load переключение УГВ с dry на wet
            // пересчитывает λ (LambdaB для wet-условий).
            _projectStateService = new ProjectStateService();
            {
                var constructionVm = await CreateInitializedConstructionViewModelAsync();
                var layer = constructionVm.LayersBelowPipe.First();
                layer.IsLambdaOverridden = false;
                // GroundwaterLevel remains 2.0 m (dry conditions)

                var viewModel = CreateViewModel(
                    CreateClimateViewModel(),
                    constructionVm,
                    CreateThermalViewModel(),
                    CreateCircuitsViewModel());

                var data = viewModel.SaveCurrentProject();

                var constructionVm2 = await CreateInitializedConstructionViewModelAsync();
                var viewModel2 = CreateViewModel(
                    CreateClimateViewModel(),
                    constructionVm2,
                    CreateThermalViewModel(),
                    CreateCircuitsViewModel());
                await viewModel2.LoadProjectDataAsync(data);

                constructionVm2.GroundwaterLevel = 0.5;

                var loadedLayer = constructionVm2.LayersBelowPipe.First();
                Assert.That(loadedLayer.CalculatedLambda, Is.EqualTo(loadedLayer.Material.LambdaB).Within(1e-9));
            }

            // Сценарий C: УГВ переключён ещё ДО save — load восстанавливает wet
            // и λ пересчитана, несмотря на устаревшее значение в слое.
            _projectStateService = new ProjectStateService();
            {
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

                var data = viewModel.SaveCurrentProject();

                var constructionVm2 = await CreateInitializedConstructionViewModelAsync();
                var viewModel2 = CreateViewModel(
                    CreateClimateViewModel(),
                    constructionVm2,
                    CreateThermalViewModel(),
                    CreateCircuitsViewModel());
                await viewModel2.LoadProjectDataAsync(data);

                var loadedLayer = constructionVm2.LayersBelowPipe.First();
                Assert.That(loadedLayer.CalculatedLambda, Is.EqualTo(loadedLayer.Material.LambdaB).Within(1e-9));
            }
        }
    }
}