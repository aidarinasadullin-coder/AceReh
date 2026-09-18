using Moq;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Services.Visualization;
using NUnit.Framework;
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
    /// Тесты восстановления проекта через ResultsViewModel.LoadProjectDataAsync:
    /// session-репосты шапки, климат/коллекторы, KPI и guard'ы оркестратора
    /// (волна A плана 2026-09-18 «чистка раздутых тестов»; только перенос
    /// дословно).
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ResultsViewModelLoadRestoreTests
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
            return CreateViewModel(ResultsViewModelTestGraph.CreateCircuitsViewModel());
        }

        private ResultsViewModel CreateViewModel(CircuitsViewModel circuitsVm)
        {
            return CreateViewModel(ResultsViewModelTestGraph.CreateClimateViewModel(), circuitsVm);
        }

        private ResultsViewModel CreateViewModel(ClimateViewModel climateVm, CircuitsViewModel circuitsVm)
        {
            return CreateViewModel(
                climateVm,
                ResultsViewModelTestGraph.CreateConstructionViewModel(_projectStateService.Session),
                ResultsViewModelTestGraph.CreateThermalViewModel(),
                circuitsVm);
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

        [Test]
        public void Session_IdentityChangedExternally_ViewModelRepostsToHeader()
        {
            // ADR-015 (план 2026-09-17): шапка биндится к ProjectNumber/
            // ProjectObject; правка с карточки «Климат» и загрузка .smc идут
            // через сессию — VM обязана репостить события в UI.
            var notifications = new List<string>();
            _viewModel.PropertyChanged += (_, e) => notifications.Add(e.PropertyName ?? string.Empty);

            _projectStateService.Session.ProjectNumber = "2026-014";
            _projectStateService.Session.ProjectObject = "Дворовая территория";

            Assert.That(notifications, Contains.Item(nameof(ResultsViewModel.ProjectNumber)),
                "событие сессии репостится — шапка обновляется без навигации");
            Assert.That(notifications, Contains.Item(nameof(ResultsViewModel.ProjectObject)));
            Assert.That(_viewModel.ProjectNumber, Is.EqualTo("2026-014"));
            Assert.That(_viewModel.ProjectObject, Is.EqualTo("Дворовая территория"));
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
            // Зона нормализуется по сохранённой температуре (план 2026-09-12, B5):
            // savedAirTemperature = -18 → M20, несмотря на SelectedZone=M15 в файле.
            Assert.That(climateVm.MirroredZone, Is.EqualTo(ClimateZone.Zone_M20));
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
            // Singleton получает зону, нормализованную по температуре (-18 → M20)
            Assert.That(climateDataSingleton.Zone, Is.EqualTo(ClimateZone.Zone_M20));
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
        public async Task LoadProjectData_WhenRestorePreflightRejects_DoesNotPublishSuccessOrPath()
        {
            var projectChangedCount = 0;
            _viewModel.ProjectChanged += (_, _) => projectChangedCount++;
            var projectData = new ProjectData
            {
                ProjectNumber = "REJECTED",
                ProjectObject = "Rejected restore",
                ClimateData = new ClimateProjectData(),
                ConstructionData = new ConstructionProjectData(),
                ThermalData = new ThermalProjectData
                {
                    SelectedMode = OperatingMode.Melting,
                    SupplyTemperature = 10.0,
                    GroundTemperature = 5.0,
                    PipeSpacing = 200,
                    SelectedPipe = new PipeTypeProjectData
                    {
                        Name = "RAUTHERM S 20x2,0",
                        OuterDiameter = 20.0,
                        InnerDiameter = 16.0,
                        WallThickness = 2.0
                    }
                },
                HydraulicsData = new HydraulicsProjectData()
            };

            await _viewModel.LoadProjectDataAsync(projectData);

            Assert.Multiple(() =>
            {
                Assert.That(projectChangedCount, Is.Zero);
                Assert.That(_projectStateService.CurrentFilePath, Is.Null);
                Assert.That(_projectStateService.Session.IsLoadProjectInProgress, Is.False);
                Assert.That(_projectStateService.IsDirty, Is.False);
            });
        }

        [Test]
        public async Task LoadProjectData_SecondInvalidProjectPreservesPriorUiAndReleasesRestoreGuard()
        {
            var projectChangedCount = 0;
            _viewModel.ProjectChanged += (_, _) => projectChangedCount++;
            var projectA = ResultsViewModelTestHelpers.CreateReadyProjectData();
            projectA.ProjectNumber = "PROJECT-A";
            var projectB = ResultsViewModelTestHelpers.CreateReadyProjectData();
            projectB.ProjectNumber = "PROJECT-B";
            projectB.ThermalData.SupplyTemperature = 10.0;

            await _viewModel.LoadProjectDataAsync(projectA);
            await _viewModel.LoadProjectDataAsync(projectB);

            Assert.Multiple(() =>
            {
                Assert.That(_viewModel.TotalPowerDensity, Is.EqualTo(100));
                Assert.That(_viewModel.SupplyTemperature, Is.EqualTo(45));
                Assert.That(projectChangedCount, Is.EqualTo(1));
                Assert.That(_projectStateService.Session.IsLoadProjectInProgress, Is.False);
                Assert.That(_projectStateService.IsDirty, Is.False);
            });
        }

        [Test]
        public async Task LoadProjectData_InvalidSavedResultPublishesFreshUiAndPdfValuesOnce()
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
            projectData.ThermalData.SupplyTemperature = 45.0;
            projectData.ThermalData.GroundTemperature = 5.0;
            projectData.ThermalData.Result = new ThermalResultProjectData
            {
                PowerTotal = 999999,
                SupplyTemperature = 45,
                IsValid = false
            };

            await viewModel.LoadProjectDataAsync(projectData);
            var pdfData = GetField<ResultsPdfDataBuilder>(viewModel, "_resultsPdfDataBuilder").Build(viewModel);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.TotalPowerDensity, Is.EqualTo(333));
                Assert.That(viewModel.SupplyTemperature, Is.EqualTo(55));
                Assert.That(pdfData.TotalPowerDensity, Is.EqualTo(333));
                Assert.That(pdfData.SupplyTemperature, Is.EqualTo(55));
            });
            thermalCalculatorMock.Verify(
                calculator => calculator.Calculate(
                    It.IsAny<ThermalInputs>(),
                    It.IsAny<IClimateData>(),
                    It.IsAny<IConstructionData>()),
                Times.Once);
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
    }
}
