using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;
using SnowMeltingCalculator.Services;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Reports.Calculation;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.Tests.Fixtures;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Shell;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Phase 10 (INV-010): subscription-lifecycle counting harness over a
    /// production-shaped singleton graph (one ProjectSession, one
    /// CalculationContext, the DI-shaped coordinators and adapters).
    /// Repeated new/load/second-load/reset/repeated-reset cycles must keep
    /// handler counts, per-cycle publication counts and dirty transitions
    /// stable — the measured heart of the reactive ownership closure.
    /// Test-only code: no production edits in this suite.
    /// </summary>
    [TestFixture]
    public class ReactiveSubscriptionLifecycleTests
    {
        private ReactiveGraph _graph = null!;

        [SetUp]
        public void SetUp()
        {
            ResetAppSettingsSingleton();
            _graph = ReactiveGraph.CreateProductionShaped();
        }

        [TearDown]
        public void TearDown()
        {
            _graph.Dispose();
            ResetAppSettingsSingleton();
        }

        #region Census: exact handler counts on every publisher

        [Test]
        public void HandlerCounts_MatchPhase10Census_OnProductionShapedGraph()
        {
            // Each row: (publisher, event field, census expectation, probe handlers the
            // harness itself subscribes, because). Census values come from
            // `evidence/phase-10-.../slice-1-reactive-census.md`; the probe column is
            // the harness's own counting subscription on that surface.
            var expected = new (object publisher, string field, int census, int probes, string because)[]
            {
                (_graph.Context, nameof(CalculationContext.ContextChanged), 1, 1, "RE-P5-HYD-001: the hydraulics coordinator holds the only production ContextChanged subscription"),
                (_graph.CalcState, nameof(ICalculationStateService.StateChanged), 3, 1, "MainViewModel + ThermalViewModel + HydraulicsStateCoordinator(no-op)"),
                (_graph.CalcState, nameof(ICalculationStateService.PipeSpacingChanged), 3, 1, "HydraulicsStateCoordinator + ThermalViewModel + ConstructionViewModel"),
                (_graph.Session.ClimateState, "Changed", 1, 1, "ClimateViewModel adapter mirror"),
                (_graph.Session.ConstructionState, "Changed", 1, 1, "ConstructionViewModel adapter"),
                (_graph.Session.ThermalState, "Changed", 1, 1, "CalculationStateService legacy translation"),
                (_graph.Session.HydraulicsState, "Changed", 2, 1, "CalculationStateService translation + CircuitsViewModel ProjectLoad mirror"),
                (_graph.Session, nameof(INotifyPropertyChanged.PropertyChanged), 1, 1, "MainViewModel window-title watcher"),
                (_graph.Coordinator, nameof(ThermalStateCoordinator.Completion), 1, 1, "ThermalViewModel adapter"),
                (_graph.Coordinator, nameof(ThermalStateCoordinator.UpstreamObserved), 1, 1, "ThermalViewModel refresh signal"),
                (_graph.ClimateData, "DataChanged", 1, 0, "ThermalStateCoordinator upstream (RE-P4-001)"),
                (_graph.ConstructionProjection, "DataChanged", 1, 0, "ThermalStateCoordinator upstream (RE-P4-001)")
            };

            foreach (var (publisher, field, census, probes, because) in expected)
            {
                Assert.That(HandlerCount(publisher, field), Is.EqualTo(census + probes),
                    $"Handler count on {publisher.GetType().Name}.{field} drifted from the Phase 10 census ({because}) [census={census}, harness probes={probes}].");
            }
        }

        #endregion

        #region Slice 2: baseline per-scenario counters, determinism across two consecutive runs

        [Test]
        public async Task Baseline_NewCalculation_TwoConsecutiveRunsProduceIdenticalCounters()
        {
            var first = await MeasureAsync(() => _graph.MainVm.NewCalculationCommand.ExecuteAsync(null));
            var second = await MeasureAsync(() => _graph.MainVm.NewCalculationCommand.ExecuteAsync(null));

            AssertDeltasEqual("new-calculation", first, second);
            Assert.That(first.DirtyRaised, Is.Zero, "A clean new-calculation reset must not raise user dirty.");
            RecordCounters("new-calculation", first);
        }

        [Test]
        public async Task Baseline_Load_TwoConsecutiveRunsProduceIdenticalCounters()
        {
            var projectA = ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200);
            var projectB = ReactiveGraph.CreateThermalProjectData(OperatingMode.Intensive, 60.0, 5.0, 300);

            // Both measured runs load B onto the state left by A — identical preconditions.
            await _graph.ResultsVm.LoadProjectDataAsync(projectA);
            var first = await MeasureAsync(() => _graph.ResultsVm.LoadProjectDataAsync(projectB));
            await _graph.ResultsVm.LoadProjectDataAsync(projectA);
            var second = await MeasureAsync(() => _graph.ResultsVm.LoadProjectDataAsync(projectB));

            AssertDeltasEqual("load", first, second);
            Assert.That(first.DirtyRaised, Is.Zero, "Load origins must not create user dirty transitions.");
            RecordCounters("load", first);
        }

        [Test]
        public async Task Baseline_SecondLoad_TwoConsecutiveRunsProduceIdenticalCounters()
        {
            var projectA = ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200);
            var projectB = ReactiveGraph.CreateThermalProjectData(OperatingMode.Intensive, 60.0, 5.0, 300);

            // Both measured runs load A onto the state left by B — identical preconditions.
            await _graph.ResultsVm.LoadProjectDataAsync(projectB);
            var first = await MeasureAsync(() => _graph.ResultsVm.LoadProjectDataAsync(projectA));
            await _graph.ResultsVm.LoadProjectDataAsync(projectB);
            var second = await MeasureAsync(() => _graph.ResultsVm.LoadProjectDataAsync(projectA));

            AssertDeltasEqual("second-load", first, second);
            Assert.That(first.DirtyRaised, Is.Zero, "Second load must stay clean (no stale user dirty).");
            RecordCounters("second-load", first);
        }

        [Test]
        public async Task Baseline_Reset_TwoConsecutiveRunsProduceIdenticalCounters()
        {
            var projectA = ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200);

            // Both measured runs reset the state left by loading A — identical preconditions.
            await _graph.ResultsVm.LoadProjectDataAsync(projectA);
            var first = Measure(() => _graph.Orchestrator.ResetModules());
            await _graph.ResultsVm.LoadProjectDataAsync(projectA);
            var second = Measure(() => _graph.Orchestrator.ResetModules());

            AssertDeltasEqual("reset", first, second);
            Assert.That(first.DirtyRaised, Is.Zero, "Reset origins must not create user dirty transitions.");
            RecordCounters("reset", first);
        }

        [Test]
        public async Task Baseline_RepeatedReset_PerCycleCountersIdenticalAfterWarmup()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200));

            await MeasureAsync(() => _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200))); // ensure non-default state before warmup
            Measure(() => _graph.Orchestrator.ResetModules()); // warmup
            var steady = Measure(() => _graph.Orchestrator.ResetModules());

            for (var i = 0; i < 3; i++)
            {
                var cycle = Measure(() => _graph.Orchestrator.ResetModules());
                AssertDeltasEqual($"repeated-reset-{i + 1}", steady, cycle);
                Assert.That(cycle.DirtyRaised, Is.Zero, "Repeated reset cycles must never raise user dirty.");
            }

            RecordCounters("repeated-reset", steady);
        }

        #endregion

        #region Slice 3: the INV-010 lifecycle heart

        [Test]
        public async Task Lifecycle_LoadResetCycles_HandlerCountsAndPerCycleDeltasRemainStable()
        {
            var data = ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200);
            var handlersBefore = _graph.HandlerCountSnapshot();

            async Task CycleAsync()
            {
                await _graph.ResultsVm.LoadProjectDataAsync(data);
                _graph.Orchestrator.ResetModules();
            }

            await CycleAsync(); // warmup
            var steady = await MeasureAsync(CycleAsync);

            for (var i = 0; i < 4; i++)
            {
                var handlersNow = _graph.HandlerCountSnapshot();
                Assert.That(handlersNow, Is.EqualTo(handlersBefore),
                    $"Cycle {i + 1}: production handler counts multiplied — a subscription leaked across a load/reset cycle.");

                var cycle = await MeasureAsync(CycleAsync);
                AssertDeltasEqual($"load-reset-cycle-{i + 1}", steady, cycle);
                Assert.That(cycle.DirtyRaised, Is.Zero, "Load/reset origins must not create user dirty transitions.");
            }

            Assert.That(_graph.HandlerCountSnapshot(), Is.EqualTo(handlersBefore),
                "Final handler counts must equal the Phase 10 census expectations.");
            RecordCounters("load-reset-cycle", steady);
        }

        [Test]
        public async Task ExactlyOnce_ClimateUserEdit_PublishesOneCompletionOneProjectionAndOneDirty()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200));
            Assert.That(_graph.Session.ThermalState.Snapshot.Result, Is.Not.Null, "Sanity: the loaded project carries a thermal result.");

            var delta = Measure(() => _graph.Session.ClimateState.ApplyIndividualEdit(
                new ClimateEdit(ClimateEditField.AirTemperature, -25.0),
                ClimateMutationOrigin.User));

            Assert.That(delta.ClimateCompletions, Is.EqualTo(1), "One climate user action = exactly one canonical completion.");
            Assert.That(delta.ClimateByOrigin[ClimateMutationOrigin.User], Is.EqualTo(1));
            Assert.That(delta.ContextClimate, Is.EqualTo(1), "The completion publishes exactly one CalculationContext.Climate projection.");
            Assert.That(delta.DirtyRaised, Is.EqualTo(1), "A changed user climate edit marks the project dirty exactly once.");
            Assert.That(delta.ThermalCompletions, Is.EqualTo(1), "Upstream climate invalidation completes Thermal exactly once.");
            Assert.That(delta.CoordinatorCompletions, Is.EqualTo(1), "The adapter observes exactly one coordinator Completion.");
            RecordCounters("climate-user-edit", delta);
        }

        [Test]
        public async Task ExactlyOnce_ThermalUserEdit_OneLogicalActionOneRecalculationChain()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200));

            // Logical action 1 — the user input edit: one canonical completion, one dirty intent.
            var edit = Measure(() => { _graph.ThermalVm.SupplyTemperature = 65.0; });

            Assert.That(edit.ThermalCompletions, Is.EqualTo(1), "One user input edit = exactly one User-origin canonical completion.");
            Assert.That(edit.ThermalUserCompletions, Is.EqualTo(1));
            Assert.That(edit.DirtyRaised, Is.EqualTo(1), "A changed user thermal edit marks the project dirty exactly once.");
            Assert.That(edit.ThermalCalculatorInvocations, Is.EqualTo(0), "The input edit alone must not run the calculator.");

            // Logical action 2 — the explicit calculate command: exactly one
            // Begin + one Complete completion, one inputs publication, one
            // result publication, one calculator run, no user dirty.
            var calculate = await MeasureAsync(() => _graph.ThermalVm.CalculateCommand.ExecuteAsync(null));

            Assert.That(calculate.ThermalCompletions, Is.EqualTo(2),
                "One calculate attempt = exactly one BeginCalculation + one CompleteCalculation canonical completion.");
            Assert.That(calculate.ThermalCalculationCompletions, Is.EqualTo(2));
            Assert.That(calculate.ContextThermalInputs, Is.EqualTo(1), "Exactly one ThermalInputs projection publication per calculate attempt.");
            Assert.That(calculate.ContextThermalResult, Is.EqualTo(1), "Exactly one ThermalResult publication per calculate attempt.");
            Assert.That(calculate.ThermalCalculatorInvocations, Is.EqualTo(1), "The calculator runs exactly once per calculate attempt.");
            Assert.That(calculate.ContextHydraulics, Is.EqualTo(1), "The valid thermal result triggers exactly one hydraulics recalculation pass.");
            Assert.That(calculate.DirtyRaised, Is.Zero, "The Calculation-origin recalculation must not raise user dirty.");
            RecordCounters("thermal-user-edit(edit)", edit);
            RecordCounters("thermal-user-edit(calculate)", calculate);
        }

        [Test]
        public async Task ExactlyOnce_HydraulicsUserEdit_ProducesOneCanonicalCommitOneDirtyOnePass()
        {
            await _graph.ResultsVm.LoadProjectDataAsync(
                ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200));

            // The loaded empty hydraulics project leaves the adapter without
            // collectors; a user first adds one (its own User-origin commit)
            // and then edits a circuit length — the logical action under test.
            _graph.CircuitsVm.AddCollectorCommand.Execute(null);
            var added = _graph.CircuitsVm.Collectors[^1];
            Assert.That(added.Circuits, Is.Not.Empty, "Sanity: a freshly added collector carries its default circuits.");
            _graph.Session.MarkClean(); // isolate the edit's dirty transition from the add's one

            var delta = Measure(() => { added.Circuits[0].CircuitLength = 42.0; });

            Assert.That(delta.HydraulicsCompletions, Is.EqualTo(1),
                "Exactly one canonical completion per logical hydraulics user edit.");
            Assert.That(delta.HydraulicsByOrigin[HydraulicsMutationOrigin.User], Is.EqualTo(1),
                "Exactly one User-origin canonical commit per logical hydraulics action.");
            Assert.That(delta.DirtyRaised, Is.EqualTo(1), "A changed user hydraulics edit marks the project dirty exactly once.");
            RecordCounters("hydraulics-user-edit", delta);
        }

        [Test]
        public async Task LifecycleOrigins_NeverRaiseUserDirty_AcrossLoadResetRestore()
        {
            var dataA = ReactiveGraph.CreateThermalProjectData(OperatingMode.Melting, 55.0, 8.0, 200);
            var dataB = ReactiveGraph.CreateThermalProjectData(OperatingMode.Intensive, 60.0, 5.0, 300);

            var loadDelta = await MeasureAsync(() => _graph.ResultsVm.LoadProjectDataAsync(dataA));
            Assert.That(loadDelta.DirtyRaised, Is.Zero);

            var secondLoadDelta = await MeasureAsync(() => _graph.ResultsVm.LoadProjectDataAsync(dataB));
            Assert.That(secondLoadDelta.DirtyRaised, Is.Zero);

            var resetDelta = Measure(() => _graph.Orchestrator.ResetModules());
            Assert.That(resetDelta.DirtyRaised, Is.Zero);

            using (_graph.Session.BeginProjectRestore())
            {
                var restoreDelta = await MeasureAsync(() => _graph.Orchestrator.RestoreModulesFromProjectAsync(dataA));
                Assert.That(restoreDelta.DirtyRaised, Is.Zero, "Restore origins must not create user dirty transitions.");
            }

            Assert.That(_graph.Session.IsDirty, Is.False, "Lifecycle-only traffic must leave the project clean.");
        }

        #endregion

        #region Measurement plumbing

        private CountersSnapshot Measure(Action scenario)
        {
            var before = _graph.Counters.Snapshot();
            scenario();
            return _graph.Counters.DeltaSince(before);
        }

        private async Task<CountersSnapshot> MeasureAsync(Func<Task> scenario)
        {
            var before = _graph.Counters.Snapshot();
            await scenario();
            return _graph.Counters.DeltaSince(before);
        }

        private static void AssertDeltasEqual(string scenario, CountersSnapshot expected, CountersSnapshot actual)
        {
            Assert.That(actual.Describe(), Is.EqualTo(expected.Describe()),
                $"Scenario '{scenario}' produced different reactive counters on an identical consecutive run — baseline is not deterministic.");
        }

        private static void RecordCounters(string scenario, CountersSnapshot snapshot)
        {
            TestContext.Out.WriteLine($"[phase-10 counters] {scenario}: {snapshot.Describe()}");
        }

        private static int HandlerCount(object publisher, string fieldName)
        {
            var type = publisher.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return (field.GetValue(publisher) as Delegate)?.GetInvocationList().Length ?? 0;
                }

                type = type.BaseType;
            }

            throw new InvalidOperationException($"Event backing field '{fieldName}' not found on {publisher.GetType().Name}.");
        }

        private static void ResetAppSettingsSingleton()
        {
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SnowMeltingCalculator",
                "settings.json");
            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }

            var field = typeof(AppSettings).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, null);
        }

        #endregion

        #region Reactive graph (production-shaped singleton composition)

        internal sealed class ReactiveGraph : IDisposable
        {
            public ProjectSession Session { get; private set; } = null!;
            public CalculationContext Context { get; private set; } = null!;
            public ClimateData ClimateData { get; private set; } = null!;
            public IConstructionData ConstructionProjection { get; private set; } = null!;
            public CalculationStateService CalcState { get; private set; } = null!;
            public ThermalStateCoordinator Coordinator { get; private set; } = null!;
            public ClimateViewModel ClimateVm { get; private set; } = null!;
            public ConstructionViewModel ConstructionVm { get; private set; } = null!;
            public ThermalViewModel ThermalVm { get; private set; } = null!;
            public CircuitsViewModel CircuitsVm { get; private set; } = null!;
            public ResultsViewModel ResultsVm { get; private set; } = null!;
            public MainViewModel MainVm { get; private set; } = null!;
            public ProjectLoadOrchestrator Orchestrator { get; private set; } = null!;
            public Mock<IThermalCalculator> ThermalCalculator { get; private set; } = null!;
            public Mock<IProjectFileService> FileServiceMock { get; private set; } = null!;
            public ReactiveCounters Counters { get; private set; } = null!;

            public static ReactiveGraph CreateProductionShaped()
            {
                var counters = new ReactiveCounters();
                var context = new CalculationContext();
                var climateData = new ClimateData();
                var session = new ProjectSession(climateData, context, hydraulicsDirtyService: null);
                var calcState = new CalculationStateService(session);
                var constructionModel = new ConstructionModel();

                var thermalCalculator = new Mock<IThermalCalculator>();
                thermalCalculator
                    .Setup(calculator => calculator.Calculate(It.IsAny<ThermalInputs>(), It.IsAny<IClimateData>(), It.IsAny<IConstructionData>()))
                    .Returns(new ThermalCalculationResult { PowerTotal = 42.5, IsValid = true })
                    .Callback(() => counters.ThermalCalculatorInvocations++);

                var thermalInputValidator = new Mock<IValidator<ThermalInputs>>();
                thermalInputValidator
                    .Setup(validator => validator.Validate(It.IsAny<ThermalInputs>()))
                    .Returns(ValidationResult.Success());

                var constructionProjection = session.ConstructionState.CurrentProjection;
                var coordinator = new ThermalStateCoordinator(
                    session.ThermalState,
                    context,
                    session,
                    thermalCalculator.Object,
                    climateData,
                    constructionProjection,
                    thermalInputValidator.Object,
                    new ThermalResultValidator());

                var climateServiceMock = new Mock<IClimateDataService>();
                climateServiceMock.Setup(s => s.LoadClimateDataAsync()).Returns(Task.CompletedTask);
                climateServiceMock.Setup(s => s.GetAllCities()).Returns(new List<CityInfo>());
                climateServiceMock.Setup(s => s.DetermineZone(It.IsAny<double>(), It.IsAny<bool>()))
                    .Returns(ClimateZone.Zone_M15);
                var climateVm = new ClimateViewModel(
                    climateServiceMock.Object,
                    climateData,
                    new ClimateValidator(),
                    session);

                var materials = Material.GetDefaultMaterials().ToList();
                var materialRepositoryMock = new Mock<IMaterialRepository>();
                materialRepositoryMock.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);
                materialRepositoryMock.Setup(r => r.GetMaterialById(It.IsAny<int>()))
                    .Returns((int id) => materials.FirstOrDefault(material => material.Id == id));
                materialRepositoryMock.Setup(r => r.GetAllMaterials()).Returns(materials);
                var templateRepositoryMock = new Mock<IConstructionTemplateRepository>();
                templateRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());
                var constructionServiceMock = new Mock<IConstructionService>();
                constructionServiceMock
                    .Setup(s => s.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
                    .Returns(Task.CompletedTask);
                constructionServiceMock
                    .Setup(s => s.ImportProjectTemplatesAsync(It.IsAny<IEnumerable<ConstructionTemplate>>()))
                    .Returns(Task.CompletedTask);

                var constructionVm = new ConstructionViewModel(
                    constructionServiceMock.Object,
                    materialRepositoryMock.Object,
                    new Mock<IConstructionRepository>().Object,
                    calcState,
                    context,
                    new ConstructionValidator(),
                    constructionModel,
                    session,
                    templateRepositoryMock.Object,
                    new Mock<IDialogService>().Object,
                    new Mock<IEditorDialogService>().Object,
                    session.ConstructionState,
                    new ConstructionDefaultStateInitializer(materialRepositoryMock.Object, session.ConstructionState));
                foreach (var material in materials)
                {
                    constructionVm.AvailableMaterials.Add(material);
                }

                var thermalVm = new ThermalViewModel(
                    thermalCalculator.Object,
                    climateData,
                    constructionProjection,
                    calcState,
                    context,
                    thermalInputValidator.Object,
                    new ThermalResultValidator(),
                    session,
                    coordinator);

                var circuitsCalculator = new Mock<ICircuitsCalculator>();
                circuitsCalculator
                    .Setup(c => c.CalculateCircuitPower(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                    .Returns(0.0);
                circuitsCalculator
                    .Setup(c => c.CalculateFlowRate(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                    .Returns(0.0);
                circuitsCalculator
                    .Setup(c => c.CalculateCollectorSummary(It.IsAny<List<CircuitRow>>(), It.IsAny<int>(), It.IsAny<ValveType>()))
                    .Returns(new CollectorSummary())
                    .Callback(() => counters.HydraulicsCalculatorInvocations++);
                circuitsCalculator
                    .Setup(c => c.CalculateAtTemperature(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<GlycolProperties>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<ValveType>()))
                    .Returns(new CircuitTemperatureResult());
                circuitsCalculator
                    .Setup(c => c.CalculateBalancing(It.IsAny<List<CircuitRow>>(), It.IsAny<ValveType>()))
                    .Returns((List<CircuitRow> circuits, ValveType _) => circuits);

                var glycolMock = new Mock<IGlycolDataService>();
                glycolMock
                    .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                    .Returns(new GlycolProperties { Density = 1050, SpecificHeat = 3800, KinematicViscosity = 0.000005 });

                var selectorMock = new Mock<ICollectorTypeSelector>();
                selectorMock
                    .Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>()))
                    .Returns(new CollectorSelectionResult { ValveType = ValveType.HKV_D });

                var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(calcState, context, session);
                var circuitsVm = new CircuitsViewModel(
                    circuitsCalculator.Object,
                    glycolMock.Object,
                    calcState,
                    new Mock<ICircuitsValidator>().Object,
                    selectorMock.Object,
                    context,
                    hydraulicsDependencies.Coordinator,
                    hydraulicsDependencies.Session);

                var initializer = new ConstructionDefaultStateInitializer(materialRepositoryMock.Object, session.ConstructionState);
                var orchestrator = new ProjectLoadOrchestrator(
                    climateVm,
                    constructionVm,
                    thermalVm,
                    circuitsVm,
                    calcState,
                    constructionServiceMock.Object,
                    context,
                    session,
                    initializer);

                var fileServiceMock = new Mock<IProjectFileService>();
                var resultsVm = new ResultsViewModel(
                    session,
                    new Mock<IDialogService>().Object,
                    new Mock<IPdfExportService>().Object,
                    new Mock<ICalculationReportExportService>().Object,
                    fileServiceMock.Object,
                    calcState,
                    materialRepositoryMock.Object,
                    constructionServiceMock.Object,
                    orchestrator,
                    new ResultsPdfDataBuilder(
                        new Mock<IConstructionVisualizationImageService>().Object,
                        calcState,
                        constructionVm,
                        circuitsVm),
                    new HydraulicSummaryBuilder());

                var mainVm = new MainViewModel(
                    climateVm,
                    thermalVm,
                    constructionVm,
                    circuitsVm,
                    resultsVm,
                    calcState,
                    session,
                    new Mock<IDialogService>().Object,
                    context,
                    session,
                    initializer);

                var graph = new ReactiveGraph
                {
                    Session = session,
                    Context = context,
                    ClimateData = climateData,
                    ConstructionProjection = constructionProjection,
                    CalcState = calcState,
                    Coordinator = coordinator,
                    ClimateVm = climateVm,
                    ConstructionVm = constructionVm,
                    ThermalVm = thermalVm,
                    CircuitsVm = circuitsVm,
                    ResultsVm = resultsVm,
                    MainVm = mainVm,
                    Orchestrator = orchestrator,
                    ThermalCalculator = thermalCalculator,
                    FileServiceMock = fileServiceMock,
                    Counters = counters
                };

                graph.AttachProbeHandlers();
                return graph;
            }

            private void AttachProbeHandlers()
            {
                Context.ContextChanged += OnContextChanged;
                CalcState.StateChanged += OnStateChanged;
                CalcState.PipeSpacingChanged += OnPipeSpacingChanged;
                Session.ClimateState.Changed += OnClimateChanged;
                Session.ConstructionState.Changed += OnConstructionChanged;
                Session.ThermalState.Changed += OnThermalChanged;
                Session.HydraulicsState.Changed += OnHydraulicsChanged;
                Coordinator.Completion += OnCoordinatorCompletion;
                Coordinator.UpstreamObserved += OnUpstreamObserved;
                ResultsVm.HydraulicSummaryCards.CollectionChanged += OnResultsProjectionChanged;
                Session.PropertyChanged += OnSessionPropertyChanged;
            }

            private void OnContextChanged(object? sender, ContextChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case nameof(CalculationContext.Climate):
                        Counters.ContextClimate++;
                        break;
                    case nameof(CalculationContext.Construction):
                        Counters.ContextConstruction++;
                        break;
                    case nameof(CalculationContext.ThermalInputs):
                        Counters.ContextThermalInputs++;
                        break;
                    case nameof(CalculationContext.ThermalResult):
                        Counters.ContextThermalResult++;
                        break;
                    case nameof(CalculationContext.HydraulicsResults):
                        Counters.ContextHydraulics++;
                        break;
                    default:
                        Counters.ContextOther++;
                        break;
                }
            }

            private void OnStateChanged(object? sender, ModuleStateChangedEventArgs e) => Counters.StateChangedPublications++;

            private void OnPipeSpacingChanged(object? sender, int e) => Counters.PipeSpacingPublications++;

            private void OnClimateChanged(object? sender, ClimateStateChangedEventArgs e)
            {
                Counters.ClimateCompletions++;
                Counters.ClimateByOrigin[e.Origin] = Counters.ClimateByOrigin.TryGetValue(e.Origin, out var value) ? value + 1 : 1;
            }

            private void OnConstructionChanged(object? sender, ConstructionStateChangedEventArgs e)
            {
                Counters.ConstructionCompletions++;
                Counters.ConstructionByOrigin[e.Origin] = Counters.ConstructionByOrigin.TryGetValue(e.Origin, out var value) ? value + 1 : 1;
            }

            private void OnThermalChanged(object? sender, ThermalStateChangedEventArgs e)
            {
                Counters.ThermalCompletions++;
                Counters.ThermalByOrigin[e.Mutation.Origin] = Counters.ThermalByOrigin.TryGetValue(e.Mutation.Origin, out var value) ? value + 1 : 1;
                if (e.Mutation.Origin == ThermalMutationOrigin.User)
                {
                    Counters.ThermalUserCompletions++;
                }

                if (e.Mutation.Origin == ThermalMutationOrigin.Calculation)
                {
                    Counters.ThermalCalculationCompletions++;
                }
            }

            private void OnHydraulicsChanged(object? sender, HydraulicsStateChangedEventArgs e)
            {
                Counters.HydraulicsCompletions++;
                Counters.HydraulicsByOrigin[e.Origin] = Counters.HydraulicsByOrigin.TryGetValue(e.Origin, out var value) ? value + 1 : 1;
            }

            private void OnCoordinatorCompletion(object? sender, ThermalStateChangedEventArgs e) => Counters.CoordinatorCompletions++;

            private void OnUpstreamObserved(object? sender, EventArgs e) => Counters.UpstreamObservedPublications++;

            private void OnResultsProjectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Counters.ResultsProjectionUpdates++;

            private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                Counters.SessionPropertyChanged++;
                if (e.PropertyName == nameof(IProjectSession.IsDirty))
                {
                    if (Session.IsDirty)
                    {
                        Counters.DirtyRaised++;
                    }
                    else
                    {
                        Counters.CleanTransitions++;
                    }
                }
            }

            public Dictionary<string, int> HandlerCountSnapshot() => new()
            {
                ["ContextChanged"] = HandlerCount(Context, nameof(CalculationContext.ContextChanged)),
                ["StateChanged"] = HandlerCount(CalcState, nameof(ICalculationStateService.StateChanged)),
                ["PipeSpacingChanged"] = HandlerCount(CalcState, nameof(ICalculationStateService.PipeSpacingChanged)),
                ["ClimateState.Changed"] = HandlerCount(Session.ClimateState, "Changed"),
                ["ConstructionState.Changed"] = HandlerCount(Session.ConstructionState, "Changed"),
                ["ThermalState.Changed"] = HandlerCount(Session.ThermalState, "Changed"),
                ["HydraulicsState.Changed"] = HandlerCount(Session.HydraulicsState, "Changed"),
                ["Session.PropertyChanged"] = HandlerCount(Session, nameof(INotifyPropertyChanged.PropertyChanged)),
                ["Coordinator.Completion"] = HandlerCount(Coordinator, nameof(ThermalStateCoordinator.Completion)),
                ["Coordinator.UpstreamObserved"] = HandlerCount(Coordinator, nameof(ThermalStateCoordinator.UpstreamObserved)),
                ["ClimateData.DataChanged"] = HandlerCount(ClimateData, "DataChanged"),
                ["ConstructionProjection.DataChanged"] = HandlerCount(ConstructionProjection, "DataChanged")
            };

            public static ProjectData CreateThermalProjectData(
                OperatingMode mode,
                double supplyTemperature,
                double groundTemperature,
                int pipeSpacing)
            {
                return new ProjectData
                {
                    ProjectNumber = "PH10",
                    ProjectObject = "Reactive lifecycle",
                    IsOperatingMode = true,
                    ClimateData = new ClimateProjectData(),
                    ConstructionData = new ConstructionProjectData(),
                    ThermalData = new ThermalProjectData
                    {
                        SelectedMode = mode,
                        SupplyTemperature = supplyTemperature,
                        GroundTemperature = groundTemperature,
                        PipeSpacing = pipeSpacing,
                        SelectedPipe = null,
                        Result = new ThermalResultProjectData { PowerTotal = 42.5, IsValid = true }
                    },
                    HydraulicsData = new HydraulicsProjectData()
                };
            }

            public void Dispose() => Coordinator.Dispose();
        }

        #endregion

        #region Counters

        internal sealed class ReactiveCounters
        {
            public int ContextClimate;
            public int ContextConstruction;
            public int ContextThermalInputs;
            public int ContextThermalResult;
            public int ContextHydraulics;
            public int ContextOther;
            public int StateChangedPublications;
            public int PipeSpacingPublications;
            public int ClimateCompletions;
            public int ConstructionCompletions;
            public int ThermalCompletions;
            public int ThermalUserCompletions;
            public int ThermalCalculationCompletions;
            public int HydraulicsCompletions;
            public int CoordinatorCompletions;
            public int UpstreamObservedPublications;
            public int ThermalCalculatorInvocations;
            public int HydraulicsCalculatorInvocations;
            public int ResultsProjectionUpdates;
            public int SessionPropertyChanged;
            public int DirtyRaised;
            public int CleanTransitions;
            public Dictionary<Enum, int> ClimateByOrigin { get; } = new();
            public Dictionary<Enum, int> ConstructionByOrigin { get; } = new();
            public Dictionary<Enum, int> ThermalByOrigin { get; } = new();
            public Dictionary<Enum, int> HydraulicsByOrigin { get; } = new();

            public CountersSnapshot Snapshot() => CountersSnapshot.CopyFrom(this);

            public CountersSnapshot DeltaSince(CountersSnapshot before) => CountersSnapshot.Delta(before, Snapshot());
        }

        internal sealed class CountersSnapshot
        {
            public int ContextClimate;
            public int ContextConstruction;
            public int ContextThermalInputs;
            public int ContextThermalResult;
            public int ContextHydraulics;
            public int ContextOther;
            public int StateChangedPublications;
            public int PipeSpacingPublications;
            public int ClimateCompletions;
            public int ConstructionCompletions;
            public int ThermalCompletions;
            public int ThermalUserCompletions;
            public int ThermalCalculationCompletions;
            public int HydraulicsCompletions;
            public int CoordinatorCompletions;
            public int UpstreamObservedPublications;
            public int ThermalCalculatorInvocations;
            public int HydraulicsCalculatorInvocations;
            public int ResultsProjectionUpdates;
            public int SessionPropertyChanged;
            public int DirtyRaised;
            public int CleanTransitions;
            public Dictionary<Enum, int> ClimateByOrigin = new();
            public Dictionary<Enum, int> ConstructionByOrigin = new();
            public Dictionary<Enum, int> ThermalByOrigin = new();
            public Dictionary<Enum, int> HydraulicsByOrigin = new();

            public static CountersSnapshot CopyFrom(ReactiveCounters source)
            {
                var snapshot = new CountersSnapshot
                {
                    ContextClimate = source.ContextClimate,
                    ContextConstruction = source.ContextConstruction,
                    ContextThermalInputs = source.ContextThermalInputs,
                    ContextThermalResult = source.ContextThermalResult,
                    ContextHydraulics = source.ContextHydraulics,
                    ContextOther = source.ContextOther,
                    StateChangedPublications = source.StateChangedPublications,
                    PipeSpacingPublications = source.PipeSpacingPublications,
                    ClimateCompletions = source.ClimateCompletions,
                    ConstructionCompletions = source.ConstructionCompletions,
                    ThermalCompletions = source.ThermalCompletions,
                    ThermalUserCompletions = source.ThermalUserCompletions,
                    ThermalCalculationCompletions = source.ThermalCalculationCompletions,
                    HydraulicsCompletions = source.HydraulicsCompletions,
                    CoordinatorCompletions = source.CoordinatorCompletions,
                    UpstreamObservedPublications = source.UpstreamObservedPublications,
                    ThermalCalculatorInvocations = source.ThermalCalculatorInvocations,
                    HydraulicsCalculatorInvocations = source.HydraulicsCalculatorInvocations,
                    ResultsProjectionUpdates = source.ResultsProjectionUpdates,
                    SessionPropertyChanged = source.SessionPropertyChanged,
                    DirtyRaised = source.DirtyRaised,
                    CleanTransitions = source.CleanTransitions,
                    ClimateByOrigin = new Dictionary<Enum, int>(source.ClimateByOrigin),
                    ConstructionByOrigin = new Dictionary<Enum, int>(source.ConstructionByOrigin),
                    ThermalByOrigin = new Dictionary<Enum, int>(source.ThermalByOrigin),
                    HydraulicsByOrigin = new Dictionary<Enum, int>(source.HydraulicsByOrigin)
                };
                return snapshot;
            }

            public static CountersSnapshot Delta(CountersSnapshot before, CountersSnapshot after)
            {
                var delta = new CountersSnapshot
                {
                    ContextClimate = after.ContextClimate - before.ContextClimate,
                    ContextConstruction = after.ContextConstruction - before.ContextConstruction,
                    ContextThermalInputs = after.ContextThermalInputs - before.ContextThermalInputs,
                    ContextThermalResult = after.ContextThermalResult - before.ContextThermalResult,
                    ContextHydraulics = after.ContextHydraulics - before.ContextHydraulics,
                    ContextOther = after.ContextOther - before.ContextOther,
                    StateChangedPublications = after.StateChangedPublications - before.StateChangedPublications,
                    PipeSpacingPublications = after.PipeSpacingPublications - before.PipeSpacingPublications,
                    ClimateCompletions = after.ClimateCompletions - before.ClimateCompletions,
                    ConstructionCompletions = after.ConstructionCompletions - before.ConstructionCompletions,
                    ThermalCompletions = after.ThermalCompletions - before.ThermalCompletions,
                    ThermalUserCompletions = after.ThermalUserCompletions - before.ThermalUserCompletions,
                    ThermalCalculationCompletions = after.ThermalCalculationCompletions - before.ThermalCalculationCompletions,
                    HydraulicsCompletions = after.HydraulicsCompletions - before.HydraulicsCompletions,
                    CoordinatorCompletions = after.CoordinatorCompletions - before.CoordinatorCompletions,
                    UpstreamObservedPublications = after.UpstreamObservedPublications - before.UpstreamObservedPublications,
                    ThermalCalculatorInvocations = after.ThermalCalculatorInvocations - before.ThermalCalculatorInvocations,
                    HydraulicsCalculatorInvocations = after.HydraulicsCalculatorInvocations - before.HydraulicsCalculatorInvocations,
                    ResultsProjectionUpdates = after.ResultsProjectionUpdates - before.ResultsProjectionUpdates,
                    SessionPropertyChanged = after.SessionPropertyChanged - before.SessionPropertyChanged,
                    DirtyRaised = after.DirtyRaised - before.DirtyRaised,
                    CleanTransitions = after.CleanTransitions - before.CleanTransitions,
                    ClimateByOrigin = DeltaDictionary(after.ClimateByOrigin, before.ClimateByOrigin),
                    ConstructionByOrigin = DeltaDictionary(after.ConstructionByOrigin, before.ConstructionByOrigin),
                    ThermalByOrigin = DeltaDictionary(after.ThermalByOrigin, before.ThermalByOrigin),
                    HydraulicsByOrigin = DeltaDictionary(after.HydraulicsByOrigin, before.HydraulicsByOrigin)
                };
                return delta;
            }

            private static Dictionary<Enum, int> DeltaDictionary(Dictionary<Enum, int> after, Dictionary<Enum, int> before)
            {
                var keys = after.Keys.Union(before.Keys).ToList();
                var result = new Dictionary<Enum, int>();
                foreach (var key in keys)
                {
                    after.TryGetValue(key, out var a);
                    before.TryGetValue(key, out var b);
                    if (a - b != 0)
                    {
                        result[key] = a - b;
                    }
                }

                return result;
            }

            public string Describe() =>
                $"Context[Climate={ContextClimate},Construction={ContextConstruction},ThermalInputs={ContextThermalInputs},ThermalResult={ContextThermalResult},Hydraulics={ContextHydraulics},Other={ContextOther}] " +
                $"StateChanged={StateChangedPublications} PipeSpacing={PipeSpacingPublications} " +
                $"Completions[Climate={ClimateCompletions},Construction={ConstructionCompletions},Thermal={ThermalCompletions}(U={ThermalUserCompletions},C={ThermalCalculationCompletions}),Hydraulics={HydraulicsCompletions},Coord={CoordinatorCompletions},Upstream={UpstreamObservedPublications}] " +
                $"Calc[Thermal={ThermalCalculatorInvocations},Hydraulics={HydraulicsCalculatorInvocations}] " +
                $"Results={ResultsProjectionUpdates} SessionPC={SessionPropertyChanged} Dirty+={DirtyRaised} Clean={CleanTransitions} " +
                $"Origins[C={Dict(ClimateByOrigin)},Con={Dict(ConstructionByOrigin)},T={Dict(ThermalByOrigin)},H={Dict(HydraulicsByOrigin)}]";

            private static string Dict<TKey>(Dictionary<TKey, int> values) =>
                string.Join(",", values.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"));
        }

        #endregion
    }
}
