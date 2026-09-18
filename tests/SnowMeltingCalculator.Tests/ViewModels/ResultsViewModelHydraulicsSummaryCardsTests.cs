using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
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
    /// Тесты HydraulicSummaryCards в ResultsViewModel: независимость карточек
    /// двух коллекторов, сброс Reset() и обнуление KPI при пустой гидравлике
    /// (волна A плана 2026-09-18 «чистка раздутых тестов»; только перенос
    /// дословно; карточные рефлексия-хелперы живут здесь единственной копией).
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ResultsViewModelHydraulicsSummaryCardsTests
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
            return CreateViewModel(
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
    }
}
