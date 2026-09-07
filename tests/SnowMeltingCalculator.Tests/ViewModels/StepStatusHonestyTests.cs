using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Tests.Services.Project;
using SnowMeltingCalculator.ViewModels.Shell;

namespace SnowMeltingCalculator.Tests.ViewModels
{
    /// <summary>
    /// Честная индикация степпера («галочка = рассчитано и валидно», ADR-012):
    /// вкладка 3 Ready только при валидном каноническом тепловом результате;
    /// вкладка 4 — только при «длины введены и расчёт выполнен для текущих
    /// данных» (HydraulicsStateSnapshot.IsCalculated); вкладка 5 — AND(1–4).
    /// Правка ввода после расчёта инвалидирует результаты в каноне и гасит
    /// вкладки реактивно. Сценарии — на production-shaped графе
    /// (ReactiveSubscriptionLifecycleTests.ReactiveGraph).
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class StepStatusHonestyTests
    {
        private ReactiveSubscriptionLifecycleTests.ReactiveGraph _graph = null!;

        [SetUp]
        public void SetUp()
        {
            ReactiveSubscriptionLifecycleTests.ResetAppSettingsSingleton();
            _graph = ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateProductionShaped();

            // Тепловой калькулятор графа возвращает результат, проходящий
            // пост-валидацию ThermalResultValidator (ΔT > 0, T_обратки >= 0):
            // T_подачи = 50, T_средняя = 25 → T_обратки = 0, ΔT = 10.
            _graph.ThermalCalculator
                .Setup(c => c.Calculate(
                    It.IsAny<ThermalInputs>(),
                    It.IsAny<IClimateData>(),
                    It.IsAny<IConstructionData>()))
                .Returns(new ThermalCalculationResult
                {
                    PowerTotal = 42.5,
                    SupplyTemperature = 50,
                    MeanTemperature = 25,
                    DeltaT = 10,
                    IsValid = true
                });
        }

        [TearDown]
        public void TearDown()
        {
            _graph.Dispose();
            ReactiveSubscriptionLifecycleTests.ResetAppSettingsSingleton();
        }

        private static MenuItem ThermalStep(MainViewModel vm) =>
            vm.MenuItems.Single(m => m.Target == NavigationTarget.Thermal);

        private static MenuItem HydraulicsStep(MainViewModel vm) =>
            vm.MenuItems.Single(m => m.Target == NavigationTarget.Hydraulics);

        private static MenuItem ResultsStep(MainViewModel vm) =>
            vm.MenuItems.Single(m => m.Target == NavigationTarget.Results);

        #region Вкладка 3 — тепловой расчёт

        [Test]
        public void ThermalStep_OnStartup_IsDraft()
        {
            Assert.That(ThermalStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Draft),
                "на старте приложения тепловая вкладка серая: расчёта ещё не было");
        }

        [Test]
        public async Task ThermalStep_AfterCalculationWithValidResult_IsReady()
        {
            await _graph.ThermalVm.CalculateCommand.ExecuteAsync(null);

            Assert.That(_graph.Session.ThermalState.Snapshot.Result is { IsValid: true }, Is.True,
                "sanity: расчёт опубликовал валидный канонический результат");
            Assert.That(ThermalStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready),
                "галочка = рассчитано и валидно");
        }

        [Test]
        public void ThermalStep_WhenValidationMessagePresent_IsError()
        {
            _graph.ThermalVm.ValidationMessage = "Выберите трубу";

            Assert.That(ThermalStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Error),
                "ошибка валидации — Error, степпер и валидатор совпадают");
        }

        [Test]
        public async Task ThermalStep_EditAfterCalculation_IsDraft()
        {
            await _graph.ThermalVm.CalculateCommand.ExecuteAsync(null);
            Assert.That(ThermalStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready), "sanity: после расчёта Ready");

            _graph.ThermalVm.SupplyTemperature = 70.0;

            Assert.That(_graph.ThermalVm.NeedsRecalculation, Is.True, "sanity: правка требует пересчёта");
            Assert.That(ThermalStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Draft),
                "правка после расчёта гасит галочку до пересчёта");
        }

        [Test]
        public async Task ThermalStep_AfterProjectLoad_IsReady()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(
                    OperatingMode.Melting, 55.0, 8.0, 200));

            Assert.That(_graph.Session.ThermalState.Snapshot.Result is { IsValid: true }, Is.True,
                "sanity: загруженный проект несёт валидный результат");
            Assert.That(_graph.ThermalVm.NeedsRecalculation, Is.False, "sanity: после загрузки пересчёт не требуется");
            Assert.That(ThermalStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready),
                "загрузка .smc с валидным результатом — вкладка 3 Ready");
        }

        #endregion

        #region Вкладка 4 — гидравлика

        [Test]
        public void HydraulicsStep_OnStartup_IsDraft()
        {
            Assert.That(HydraulicsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Draft),
                "на старте приложения гидравлика серая: длин и расчёта нет");
        }

        [Test]
        public void HydraulicsStep_LengthsWithoutCalculation_IsDraft()
        {
            // «Длины введены, расчёта нет»: канонический снапшот с контурами
            // (длина > 0), но без Summary — путь загрузки (ProjectLoad), поэтому
            // результаты легитимно отсутствуют, а не инвалидируются.
            var collector = new HydraulicCollectorSnapshot(
                1, "HKV-D", ValveType.HKV_D,
                new[] { new HydraulicCircuitSnapshot(1, 110, 10, 5, 10, 20) });
            _graph.Session.HydraulicsState.Restore(
                new HydraulicsStateSnapshot(HydraulicGlobalInputsSnapshot.Default, new[] { collector }, HydraulicsStatusSnapshot.Default),
                HydraulicsMutationOrigin.ProjectLoad);

            Assert.That(_graph.Session.HydraulicsState.Snapshot.IsCalculated(), Is.False,
                "Summary нет — «рассчитано» ложно");
            Assert.That(HydraulicsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Draft),
                "длины без расчёта — вкладка серая");
        }

        [Test]
        public async Task HydraulicsStep_AfterCalculation_IsReady()
        {
            // Длины вводит пользователь (каноническая User-мутация), затем
            // тепловой расчёт публикует результат — каскад пересчитывает
            // гидравлику (UpdateFromThermal-путь через CalculateAll).
            _graph.CircuitsVm.Collectors[0].Circuits[0].CircuitLength = 100;
            await _graph.ThermalVm.CalculateCommand.ExecuteAsync(null);

            Assert.That(_graph.Session.HydraulicsState.Snapshot.IsCalculated(), Is.True,
                "sanity: расчёт выполнен, у всех коллекторов есть Summary");
            Assert.That(HydraulicsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready),
                "длины введены и расчёт выполнен — Ready");
        }

        [Test]
        public async Task HydraulicsStep_LengthEditAfterCalculation_InvalidatesCanonAndDrafts()
        {
            _graph.CircuitsVm.Collectors[0].Circuits[0].CircuitLength = 100;
            await _graph.ThermalVm.CalculateCommand.ExecuteAsync(null);
            Assert.That(HydraulicsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready), "sanity: после расчёта Ready");

            _graph.CircuitsVm.Collectors[0].Circuits[0].CircuitLength = 120;

            var snapshot = _graph.Session.HydraulicsState.Snapshot;
            var gridCircuit = _graph.CircuitsVm.Collectors[0].Circuits[0];
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Collectors.All(c => c.Summary is null), Is.True,
                    "Summary коллекторов обнулён в каноне");
                Assert.That(snapshot.Collectors.SelectMany(c => c.Circuits)
                    .All(cr => cr.OperatingResult is null && cr.DesignResult is null), Is.True,
                    "контурные результаты обнулены в каноне");
                Assert.That(snapshot.Collectors.SelectMany(c => c.Circuits)
                    .Any(cr => cr.CircuitLength > 0), Is.True,
                    "введённые длины сохраняются");
                Assert.That(snapshot.IsCalculated(), Is.False);
                Assert.That(HydraulicsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Draft),
                    "правка после расчёта гасит вкладку 4 реактивно");
                // Грид: расчётные поля существующих строк очищены (ADR-012),
                // входные поля сохранены, коллекция не пересобиралась.
                Assert.That(gridCircuit.OperatingResult, Is.Null, "грид: OperatingResult очищен");
                Assert.That(gridCircuit.DesignResult, Is.Null, "грид: DesignResult очищен");
                Assert.That(gridCircuit.Power, Is.EqualTo(0), "грид: мощность очищена");
                Assert.That(gridCircuit.FlowRate, Is.EqualTo(0), "грид: расход очищен");
                Assert.That(gridCircuit.CircuitLength, Is.EqualTo(120), "грид: введённая длина сохранена");
                Assert.That(_graph.CircuitsVm.Collectors[0].Summary, Is.Null, "грид: Summary очищен");
            });
        }

        [Test]
        public async Task HydraulicsStep_RecalculationAfterInvalidation_RestoresGridResults()
        {
            _graph.CircuitsVm.Collectors[0].Circuits[0].CircuitLength = 100;
            await _graph.ThermalVm.CalculateCommand.ExecuteAsync(null);
            _graph.CircuitsVm.Collectors[0].Circuits[0].CircuitLength = 120;
            Assert.That(_graph.CircuitsVm.Collectors[0].Summary, Is.Null, "sanity: после правки грид очищен");

            await _graph.ThermalVm.CalculateCommand.ExecuteAsync(null);

            var snapshot = _graph.Session.HydraulicsState.Snapshot;
            var gridCircuit = _graph.CircuitsVm.Collectors[0].Circuits[0];
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.IsCalculated(), Is.True, "канон снова рассчитан");
                Assert.That(HydraulicsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready));
                Assert.That(_graph.CircuitsVm.Collectors[0].Summary, Is.Not.Null, "грид: Summary восстановлен");
                Assert.That(gridCircuit.OperatingResult, Is.Not.Null, "грид: OperatingResult восстановлен");
                Assert.That(gridCircuit.DesignResult, Is.Not.Null, "грид: DesignResult восстановлен");
            });
        }

        [Test]
        public void HydraulicsStep_FirstLengthInputWithoutCalculation_DoesNotTouchGrid()
        {
            // Дельта-семантика очистки: если результатов не было и в старом
            // каноне (первый ввод длин) — грид не трогается.
            var collector = _graph.CircuitsVm.Collectors[0];
            collector.Summary = new CollectorSummary { CircuitCount = 2, TotalFlowRate = 1000 };

            collector.Circuits[0].CircuitLength = 100;

            Assert.Multiple(() =>
            {
                Assert.That(collector.Summary, Is.Not.Null,
                    "первый ввод длин не очищает итоги, которых не было в каноне");
                Assert.That(collector.Circuits[0].CircuitLength, Is.EqualTo(100));
            });
        }

        [Test]
        public async Task HydraulicsStep_FailCalculation_ClearsResultsAndIsNotReady()
        {
            _graph.CircuitsVm.Collectors[0].Circuits[0].CircuitLength = 100;
            await _graph.ThermalVm.CalculateCommand.ExecuteAsync(null);
            Assert.That(HydraulicsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready), "sanity: после расчёта Ready");

            // Провалившийся расчёт: Begin + Fail — санкционированный
            // Calculation-путь канона.
            _graph.Session.HydraulicsState.BeginCalculation();
            _graph.Session.HydraulicsState.FailCalculation("boom");

            var snapshot = _graph.Session.HydraulicsState.Snapshot;
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Collectors.All(c => c.Summary is null), Is.True);
                Assert.That(snapshot.Collectors.SelectMany(c => c.Circuits)
                    .All(cr => cr.OperatingResult is null && cr.DesignResult is null), Is.True,
                    "провалившийся расчёт не оставляет «полурезультатов»");
                Assert.That(snapshot.IsCalculated(), Is.False);
                // Существующая семантика: Phase Error транслируется
                // CalculationStateService в бейдж HasError, поэтому вкладка
                // показывает Error (ветка Error в формуле вкладки стоит выше
                // предиката IsCalculated) — «не Ready» и ошибка видна.
                Assert.That(HydraulicsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Error));
            });
        }

        [Test]
        public async Task HydraulicsStep_ProjectLoadWithResults_IsReady()
        {
            var project = ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(
                OperatingMode.Melting, 55.0, 8.0, 200);
            project.HydraulicsData = CreateCalculatedHydraulicsProjectData();

            await _graph.ResultsVm.LoadProjectDataAsync(project);

            Assert.That(_graph.Session.HydraulicsState.Snapshot.IsCalculated(), Is.True,
                "sanity: результаты файла восстановлены в каноне");
            Assert.That(HydraulicsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready),
                "загрузка .smc с результатами — вкладка 4 Ready");
        }

        #endregion

        #region Вкладка 5 — AND(1–4)

        [Test]
        public async Task ResultsStep_WhenAllModulesReady_IsReady()
        {
            await MakeAllModulesReadyAsync();

            Assert.Multiple(() =>
            {
                Assert.That(ThermalStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready), "вкладка 3");
                Assert.That(HydraulicsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready), "вкладка 4");
                Assert.That(_graph.ResultsVm.IsDataReady, Is.True, "вкладка 5: AND(1–4) истинно");
                Assert.That(ResultsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready), "вкладка 5");
            });
        }

        [Test]
        public async Task ResultsStep_HydraulicsInvalidation_GoesDraft()
        {
            await MakeAllModulesReadyAsync();
            Assert.That(_graph.ResultsVm.IsDataReady, Is.True, "sanity: все модули готовы");

            _graph.CircuitsVm.Collectors[0].Circuits[0].CircuitLength = 120;

            Assert.Multiple(() =>
            {
                Assert.That(_graph.ResultsVm.IsDataReady, Is.False, "инвалидация гидравлики гасит вкладку 5");
                Assert.That(ResultsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Draft));
            });
        }

        [Test]
        public async Task ResultsStep_ClimateInvalidation_GoesDraft()
        {
            await MakeAllModulesReadyAsync();
            Assert.That(_graph.ResultsVm.IsDataReady, Is.True, "sanity: все модули готовы");

            _graph.Session.ClimateState.ApplyIndividualEdit(
                new ClimateEdit(ClimateEditField.AirTemperature, -30.0),
                ClimateMutationOrigin.User);

            Assert.Multiple(() =>
            {
                Assert.That(_graph.ResultsVm.IsDataReady, Is.False, "инвалидация климата гасит вкладку 5");
                Assert.That(ResultsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Draft));
            });
        }

        #endregion

        #region Persistence (ADR-012: stale-результаты не сохраняются)

        [Test]
        public async Task SaveProjection_AfterEditFollowingCalculation_HasNoResults()
        {
            _graph.CircuitsVm.Collectors[0].Circuits[0].CircuitLength = 100;
            await _graph.ThermalVm.CalculateCommand.ExecuteAsync(null);
            Assert.That(HydraulicsStep(_graph.MainVm).StepStatus, Is.EqualTo(StepStatus.Ready), "sanity: после расчёта Ready");

            _graph.CircuitsVm.Collectors[0].Circuits[0].CircuitLength = 120;

            // Тот же маппер, что пишет .smc (ResultsViewModel.SaveProject).
            var persisted = HydraulicsPersistenceMapper.BuildHydraulicsProjectData(
                _graph.Session.HydraulicsState.Snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(persisted.Collectors.Select(c => c.Summary), Has.All.Null,
                    "Summary коллекторов не сохраняется после правки-после-расчёта");
                Assert.That(persisted.Collectors.SelectMany(c => c.Circuits)
                    .Select(c => c.OperatingResult), Has.All.Null, "OperatingResult не сохраняется");
                Assert.That(persisted.Collectors.SelectMany(c => c.Circuits)
                    .Select(c => c.DesignResult), Has.All.Null, "DesignResult не сохраняется");
                Assert.That(persisted.Collectors.SelectMany(c => c.Circuits)
                    .Any(c => c.CircuitLength > 0), Is.True, "введённые длины сохраняются");
            });
        }

        [Test]
        public async Task Load_WithResults_RestoresResultsInCanon()
        {
            var project = ReactiveSubscriptionLifecycleTests.ReactiveGraph.CreateThermalProjectData(
                OperatingMode.Melting, 55.0, 8.0, 200);
            project.HydraulicsData = CreateCalculatedHydraulicsProjectData();

            await _graph.ResultsVm.LoadProjectDataAsync(project);

            var snapshot = _graph.Session.HydraulicsState.Snapshot;
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Collectors.Select(c => c.Summary), Has.All.Not.Null,
                    "Summary коллекторов восстановлен из файла");
                Assert.That(snapshot.Collectors.SelectMany(c => c.Circuits)
                    .Select(c => c.OperatingResult), Has.All.Not.Null,
                    "контурные результаты восстановлены из файла");
                Assert.That(snapshot.IsCalculated(), Is.True);
            });
        }

        #endregion

        #region Helpers

        private async Task MakeAllModulesReadyAsync()
        {
            // 1. Климат: город выбран (User-мутация канона).
            _graph.Session.ClimateState.ApplyCitySelection(
                new CityInfo { Name = "Тест-город", Region = "Тест", T5Days092 = -25 },
                false,
                ClimateMutationOrigin.User);
            // 2. Конструкция: валидный канонический снапшот (слой над трубой
            //    ≥ 40 мм суммарно, УГВ в диапазоне 0–10).
            _graph.Session.ConstructionState.ApplySnapshot(
                new ConstructionStateSnapshot(
                    2.0,
                    new[] { new ConstructionLayerSnapshot(Guid.NewGuid(), 5, "Concrete", 60, 1.5, false, LayerPosition.AbovePipe, 1) },
                    Array.Empty<ConstructionLayerSnapshot>()),
                ConstructionMutationOrigin.User);
            // 3. Тепло: труба выбрана (гейт «не выбрана труба» во вкладке 5),
            //    длины гидравлики введены, расчёт — каскад считает гидравлику.
            _graph.ThermalVm.SelectedPipe = PipeType.StandardPipes[1];
            _graph.CircuitsVm.Collectors[0].Circuits[0].CircuitLength = 100;
            await _graph.ThermalVm.CalculateCommand.ExecuteAsync(null);
        }

        private static HydraulicsProjectData CreateCalculatedHydraulicsProjectData()
        {
            return new HydraulicsProjectData
            {
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 50,
                SupplySpacingCm = 5,
                SupplyHeatPercent = 10,
                Collectors =
                {
                    new CollectorProjectData
                    {
                        CollectorNumber = 1,
                        CollectorType = "HKV-D",
                        ValveType = ValveType.HKV_D,
                        Circuits =
                        {
                            new CircuitProjectData
                            {
                                CircuitNumber = 1,
                                CircuitLength = 110,
                                SupplyLength = 10,
                                SupplySpacingCm = 5,
                                SupplyHeatPercent = 10,
                                PipeSpacingCm = 20,
                                OperatingResult = new CircuitResultProjectData
                                {
                                    Power = 100,
                                    FlowRate = 0.5,
                                    Velocity = 0.4,
                                    DpRohr = 1000,
                                    DpVerteiler = 100,
                                    DpVent = 500,
                                    DpGesamt = 1600,
                                    Throttling = 0,
                                    ValveTurns = 1,
                                    Density = 1050,
                                    KinematicViscosity = 0.000005,
                                    ReynoldsNumber = 8000,
                                    FrictionFactor = 0.03,
                                    PressureLossPerMeter = 200,
                                    FlowRegime = nameof(FlowRegime.Turbulent),
                                    FlowRegimeString = nameof(FlowRegime.Turbulent)
                                }
                            }
                        },
                        Summary = new CollectorSummaryProjectData
                        {
                            CircuitCount = 1,
                            TotalPipeLength = 110,
                            TotalPower = 100,
                            TotalFlowRate = 0.5,
                            PressureLoss_Operating_Pa = 1600,
                            PressureLoss_Cold_Pa = 6000,
                            Kv = 1.2,
                            CollectorType = "HKV-D"
                        }
                    }
                }
            };
        }

        #endregion
    }
}
