using System.IO;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Core.Results;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
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
    /// Тесты сохранения проекта в ResultsViewModel: save читает канонические
    /// state-снапшоты (не VM-зеркала), отказоустойчивость persistence
    /// (P1/P2-фаза 4), даты и dirty-переходы (волна A плана 2026-09-18
    /// «чистка раздутых тестов»; только перенос дословно).
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ResultsViewModelSaveSnapshotTests
    {
        private ProjectStateService _projectStateService = null!;
        private Mock<IDialogService> _dialogServiceMock = null!;
        private Mock<IProjectFileService> _projectFileServiceMock = null!;
        private Mock<IConstructionService> _constructionServiceMock = null!;
        private ResultsViewModel _viewModel = null!;

        private const string TestFilePath = ResultsViewModelTestGraph.TestFilePath;

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
            Assert.That(saved.ClimateData.SelectedZone, Is.Not.EqualTo(climateVm.MirroredZone));
            Assert.That(saved.ClimateData.IsHighRequirements, Is.Not.EqualTo(climateVm.IsHighRequirements));
        }

        [Test]
        public void SaveCurrentProject_PersistsConstructionStateSnapshot_NotConstructionViewModelMirror()
        {
            // Arrange: constructionVm подключён к канонической сессии, но VM-коллекции
            // и канонический snapshot различаются. SaveCurrentProject должен сохранить
            // именно канонический snapshot, а не stale VM-кэш.
            const double canonicalGroundwater = 0.5;
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
            Assert.That(_projectStateService.Session.ConstructionState.Snapshot.LayersAbovePipe, Has.Count.EqualTo(1));
            Assert.That(constructionVm.GroundwaterLevel, Is.Not.EqualTo(canonicalGroundwater).Within(0.001));
            Assert.That(constructionVm.LayersAbovePipe, Is.Empty);

            // Act
            var saved = viewModel.SaveCurrentProject();

            // Assert: сохранённые поля совпадают с каноническим snapshot.
            Assert.Multiple(() =>
            {
                Assert.That(saved.ConstructionData.GroundwaterLevel,
                    Is.EqualTo(canonicalGroundwater).Within(1e-9));
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
                if (args.PropertyName != nameof(IProjectSession.IsDirty))
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
                if (args.PropertyName != nameof(IProjectSession.IsDirty))
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
    }
}
