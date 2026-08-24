using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.IntegrationTests.Hydraulics;

/// <summary>
/// Phase 5 Todo 2 characterization of the current hydraulics writers,
/// subscribers, calculation paths, restore census, and save projection.
/// </summary>
[TestFixture]
public sealed class HydraulicsMultiplicityCharacterizationTests
{
    [TestCase(nameof(HydraulicInputData.GlycolType), 2, 2)]
    [TestCase(nameof(HydraulicInputData.GlycolConcentration), 2, 2)]
    [TestCase(nameof(HydraulicInputData.SupplySpacing_cm), 2, 2)]
    [TestCase(nameof(HydraulicInputData.SupplyHeatPercent), 2, 2)]
    public void GlobalInputEdit_UsesCurrentDirtyAndCalculationMultiplicity(
        string propertyName,
        int expectedDirtyCalls,
        int expectedSummaryCalls)
    {
        var fixture = CreateFixture();
        fixture.ResetCounters();

        switch (propertyName)
        {
            case nameof(HydraulicInputData.GlycolType):
                fixture.ViewModel.InputData.GlycolType = fixture.ViewModel.InputData.GlycolType == GlycolType.Ethylene
                    ? GlycolType.Propylene
                    : GlycolType.Ethylene;
                break;
            case nameof(HydraulicInputData.GlycolConcentration):
                fixture.ViewModel.InputData.GlycolConcentration += 1;
                break;
            case nameof(HydraulicInputData.SupplySpacing_cm):
                fixture.ViewModel.InputData.SupplySpacing_cm += 1;
                break;
            case nameof(HydraulicInputData.SupplyHeatPercent):
                fixture.ViewModel.InputData.SupplyHeatPercent += 1;
                break;
            default:
                Assert.Fail($"Unexpected input {propertyName}");
                break;
        }

        Assert.Multiple(() =>
        {
            Assert.That(fixture.DirtyCalls, Is.EqualTo(expectedDirtyCalls));
            Assert.That(fixture.SummaryCalls, Is.EqualTo(expectedSummaryCalls));
        });
    }

    [Test]
    public void CollectorAndCircuitCollectionChanges_MarkDirtyOncePerCollectionEvent()
    {
        var fixture = CreateFixture();
        fixture.ResetCounters();
        var collector = fixture.ViewModel.Collectors[0];

        collector.Circuits.Add(new CircuitRow { CircuitNumber = 3, CircuitLength = 30 });
        collector.Circuits.RemoveAt(collector.Circuits.Count - 1);
        fixture.ViewModel.AddCollectorCommand.Execute(null);

        Assert.That(fixture.DirtyCalls, Is.EqualTo(3));
    }

    [Test]
    public void CircuitAndCollectorPropertyChanges_MarkDirtyOnce()
    {
        var fixture = CreateFixture();
        fixture.ResetCounters();
        var collector = fixture.ViewModel.Collectors[0];
        var circuit = collector.Circuits[0];

        circuit.CircuitLength = 25;
        collector.ValveType = ValveType.IV_1_25;

        Assert.That(fixture.DirtyCalls, Is.EqualTo(2));
    }

    [Test]
    public void CalculateCommand_PreservesCurrentStateOrderAndSinglePublication()
    {
        var fixture = CreateFixture();
        var order = new List<string>();
        fixture.StateMock
            .Setup(state => state.SetHydraulicsCalculating())
            .Callback(() => order.Add("calculating"));
        fixture.StateMock
            .Setup(state => state.ResetHydraulicsState())
            .Callback(() => order.Add("reset"));
        fixture.Context.ContextChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(CalculationContext.HydraulicsResults))
                order.Add("publish");
        };
        fixture.ViewModel.CalculateCommand.Execute(null);

        Assert.That(order, Is.EqualTo(new[] { "calculating", "publish", "reset" }));
        Assert.That(fixture.Context.HydraulicsResults, Has.Count.EqualTo(1));
        Assert.That(fixture.SummaryCalls, Is.EqualTo(2));
    }

    [Test]
    public void ThermalContextRouting_ValidResultCalculatesOnce_InvalidAndNullNotifyOnly()
    {
        var fixture = CreateFixture();
        fixture.ResetCounters();
        fixture.Context.UpdateThermal(new ThermalCalculationResult { IsValid = true }, "ThermalViewModel");
        var validSummaryCalls = fixture.SummaryCalls;
        fixture.Context.UpdateThermal(new ThermalCalculationResult { IsValid = false }, "ThermalViewModel");
        fixture.Context.UpdateThermal(null, "ThermalViewModel");

        Assert.Multiple(() =>
        {
            Assert.That(validSummaryCalls, Is.EqualTo(2));
            Assert.That(fixture.SummaryCalls, Is.EqualTo(validSummaryCalls));
        });
    }

    [Test]
    public void OwnHydraulicsPublication_DoesNotReenterCalculation()
    {
        var fixture = CreateFixture();
        fixture.ResetCounters();

        fixture.ViewModel.CalculateCommand.Execute(null);

        Assert.That(fixture.SummaryCalls, Is.EqualTo(2));
    }

    [Test]
    public void PipeSpacingChanged_UpdatesCircuitsAndCalculatesOnce_NoOpIsSilent()
    {
        var fixture = CreateFixture();
        fixture.ResetCounters();
        fixture.StateMock.Object.SetPipeSpacing(250, "ThermalViewModel");
        var changedCalls = fixture.SummaryCalls;
        fixture.StateMock.Object.SetPipeSpacing(250, "ThermalViewModel");

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ViewModel.Collectors[0].Circuits[0].PipeSpacing_cm, Is.EqualTo(25));
            Assert.That(changedCalls, Is.EqualTo(2));
            Assert.That(fixture.SummaryCalls, Is.EqualTo(changedCalls));
        });
    }

    [Test]
    public void RepeatedResetCycles_DoNotMultiplyCircuitSubscriptions()
    {
        var fixture = CreateFixture();
        var oldCircuit = fixture.ViewModel.Collectors[0].Circuits[0];
        var externalNotifications = 0;
        PropertyChangedEventHandler handler = (_, args) =>
        {
            if (args.PropertyName == nameof(CircuitRow.CircuitLength))
                externalNotifications++;
        };
        oldCircuit.PropertyChanged += handler;
        try
        {
            fixture.ViewModel.Reset();
            fixture.ViewModel.Reset();
            fixture.ViewModel.Reset();
            oldCircuit.CircuitLength = 99;
            fixture.DirtyMock.Invocations.Clear();
            fixture.ViewModel.Collectors[0].Circuits[0].CircuitLength = 88;

            Assert.That(externalNotifications, Is.EqualTo(1));
            fixture.DirtyMock.Verify(mark => mark.MarkDirty(), Times.Once);
        }
        finally
        {
            oldCircuit.PropertyChanged -= handler;
        }
    }

    [Test]
    public async Task Restore_CensusPreservesHydraulicInputsCollectorsResultsAndFlowRegimeFallback()
    {
        var fixture = CreateFixture();
        var data = CreateProjectData();

        await fixture.Orchestrator.RestoreModulesFromProjectAsync(data);
        var collector = fixture.ViewModel.Collectors.Single();
        var circuit = collector.Circuits.Single();

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ViewModel.InputData.GlycolType, Is.EqualTo(GlycolType.Propylene));
            Assert.That(fixture.ViewModel.InputData.GlycolConcentration, Is.EqualTo(42));
            Assert.That(fixture.ViewModel.InputData.SupplySpacing_cm, Is.EqualTo(7));
            Assert.That(fixture.ViewModel.InputData.SupplyHeatPercent, Is.EqualTo(17));
            Assert.That(fixture.ViewModel.SelectedCollectorIndex, Is.EqualTo(0));
            Assert.That(collector.CollectorNumber, Is.EqualTo(3));
            Assert.That(circuit.CircuitLength, Is.EqualTo(123));
            Assert.That(circuit.Power, Is.EqualTo(456));
            Assert.That(circuit.FlowRate, Is.EqualTo(12));
            Assert.That(collector.Summary!.TotalPower, Is.EqualTo(789));
            Assert.That(circuit.OperatingResult!.FlowRegime, Is.EqualTo(FlowRegime.Laminar));
            Assert.That(circuit.DesignResult!.FlowRegime, Is.EqualTo(FlowRegime.Turbulent));
        });
    }

    [Test]
    public void SaveProjection_ContainsCurrentHydraulicsWireFieldsAndVersionLiteral()
    {
        var sourceRoot = FindSourceRoot();
        var resultsSource = File.ReadAllText(Path.Combine(sourceRoot, "ViewModels", "Results", "ResultsViewModel.cs"));
        var methodStart = resultsSource.IndexOf("public ProjectData SaveCurrentProject", StringComparison.Ordinal);
        var nextMethod = resultsSource.IndexOf("\n        /// <summary>", methodStart + 1, StringComparison.Ordinal);
        var saveBody = resultsSource[methodStart..nextMethod];

        Assert.Multiple(() =>
        {
            Assert.That(saveBody, Does.Contain("Version = \"1.1\""));
            Assert.That(saveBody, Does.Contain("HydraulicsData"));
            Assert.That(saveBody, Does.Contain("BuildCanonicalSnapshot"));
            Assert.That(saveBody, Does.Contain("HydraulicsPersistenceMapper.BuildHydraulicsProjectData"));
        });
    }

    private static string FindSourceRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
            directory = directory.Parent;
        return directory == null ? throw new InvalidOperationException("Repository root not found") : Path.Combine(directory.FullName, "src");
    }

    private static ProjectData CreateProjectData() => new()
    {
        Version = "1.1",
        ClimateData = new ClimateProjectData(),
        ConstructionData = new ConstructionProjectData(),
        ThermalData = new ThermalProjectData { Result = new ThermalResultProjectData { IsValid = true } },
        HydraulicsData = new HydraulicsProjectData
        {
            GlycolType = GlycolType.Propylene,
            GlycolConcentration = 42,
            SupplySpacingCm = 7,
            SupplyHeatPercent = 17,
            Collectors = new List<CollectorProjectData>
            {
                new()
                {
                    CollectorNumber = 3,
                    CollectorType = "HKV-D (2-12 контура)",
                    ValveType = ValveType.HKV_D,
                    Summary = new CollectorSummaryProjectData { TotalPower = 789 },
                    Circuits = new List<CircuitProjectData>
                    {
                        new()
                        {
                            CircuitNumber = 4,
                            CircuitLength = 123,
                            Power = 456,
                            FlowRate = 12,
                            OperatingResult = new CircuitResultProjectData { FlowRegimeString = "unknown" },
                            DesignResult = new CircuitResultProjectData { FlowRegimeString = "Turbulent" }
                        }
                    }
                }
            }
        }
    };

    private static Fixture CreateFixture()
    {
        var calculator = new Mock<ICircuitsCalculator>();
        calculator.Setup(c => c.CalculateCircuitPower(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(100);
        calculator.Setup(c => c.CalculateFlowRate(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns(10);
        calculator.Setup(c => c.CalculateCollectorSummary(It.IsAny<List<CircuitRow>>(), It.IsAny<int>(), It.IsAny<ValveType>()))
            .Returns((List<CircuitRow> circuits, int number, ValveType valve) => new CollectorSummary { CollectorNumber = number, CircuitCount = circuits.Count, TotalPower = circuits.Sum(c => c.Power), ValveType = valve });
        calculator.Setup(c => c.CalculateAtTemperature(It.IsAny<CircuitRow>(), It.IsAny<double>(), It.IsAny<GlycolProperties>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<ValveType>()))
            .Returns(new CircuitTemperatureResult { FlowRegime = FlowRegime.Turbulent });
        calculator.Setup(c => c.CalculateBalancing(It.IsAny<List<CircuitRow>>(), It.IsAny<ValveType>())).Returns((List<CircuitRow> circuits, ValveType _) => circuits);

        var state = new Mock<ICalculationStateService>();
        var pipeSpacingBacking = 200;
        state.SetupGet(s => s.PipeSpacing).Returns(() => pipeSpacingBacking);
        state.Setup(s => s.SetPipeSpacing(It.IsAny<int>(), It.IsAny<string>()))
            .Callback<int, string>((spacing, _) =>
            {
                if (spacing == pipeSpacingBacking)
                    return;
                pipeSpacingBacking = spacing;
                state.Raise(s => s.PipeSpacingChanged += null, state.Object, spacing);
            });
        state.Setup(s => s.SetPipeSpacing(It.IsAny<int>()))
            .Callback<int>(spacing =>
            {
                if (spacing == pipeSpacingBacking)
                    return;
                pipeSpacingBacking = spacing;
                state.Raise(s => s.PipeSpacingChanged += null, state.Object, spacing);
            });
        var dirty = new Mock<IMarkDirtyService>();
        var context = new CalculationContext();
        var validator = new Mock<ICircuitsValidator>();
        validator.Setup(v => v.ConfirmDeleteCollector(It.IsAny<int>())).Returns(true);
        validator.Setup(v => v.ConfirmDeleteCircuit(It.IsAny<int>())).Returns(true);
        validator.Setup(v => v.CanRemoveCollector(It.IsAny<CollectorData>(), It.IsAny<int>())).Returns(true);
        validator.Setup(v => v.CanRemoveCircuit(It.IsAny<CircuitRow>(), It.IsAny<CollectorData>())).Returns(true);
        var selector = new Mock<ICollectorTypeSelector>();
        selector.Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>())).Returns(new CollectorSelectionResult { ValveType = ValveType.HKV_D });
        var glycol = new Mock<IGlycolDataService>();
        glycol.Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>())).Returns(new GlycolProperties { Density = 1050, SpecificHeat = 3800, KinematicViscosity = 0.000005 });
        var circuits = new CircuitsViewModel(calculator.Object, glycol.Object, state.Object, validator.Object, selector.Object, context, dirty.Object);
        circuits.Collectors[0].Circuits.Add(new CircuitRow { CircuitNumber = 1, CircuitLength = 100 });
        circuits.SelectedCollectorIndex = 0;
        var session = new ProjectSession();
        var climate = new ClimateViewModel(new Mock<IClimateDataService>().Object, new ClimateData(), new ClimateValidator(), dirty.Object, context);
        var construction = new ConstructionViewModelTestFactory().Create(session);
        var thermal = new ThermalViewModel(new Mock<IThermalCalculator>().Object, new ClimateData(), new ConstructionData(), state.Object, context, new ThermalValidator(new ThermalCalculator(), new ClimateData(), new ConstructionData()), new ThermalResultValidator(), dirty.Object);
        var orchestrator = new ProjectLoadOrchestrator(climate, construction, thermal, circuits, state.Object, new Mock<IConstructionService>().Object, context, session, new ConstructionDefaultStateInitializer(new Mock<IMaterialRepository>().Object, session.ConstructionState));
        return new Fixture(circuits, calculator, state, dirty, context, orchestrator);
    }

    private sealed class Fixture
    {
        public Fixture(CircuitsViewModel viewModel, Mock<ICircuitsCalculator> calculatorMock, Mock<ICalculationStateService> stateMock, Mock<IMarkDirtyService> dirtyMock, CalculationContext context, ProjectLoadOrchestrator orchestrator)
        {
            ViewModel = viewModel; CalculatorMock = calculatorMock; StateMock = stateMock; DirtyMock = dirtyMock; Context = context; Orchestrator = orchestrator;
            StateMock.Setup(s => s.SetHydraulicsCalculating()).Callback(() => { });
            StateMock.Setup(s => s.ResetHydraulicsState()).Callback(() => { });
        }
        public CircuitsViewModel ViewModel { get; }
        public Mock<ICircuitsCalculator> CalculatorMock { get; }
        public Mock<ICalculationStateService> StateMock { get; }
        public Mock<IMarkDirtyService> DirtyMock { get; }
        public CalculationContext Context { get; }
        public ProjectLoadOrchestrator Orchestrator { get; }
        public int DirtyCalls => DirtyMock.Invocations.Count(i => i.Method.Name == nameof(IMarkDirtyService.MarkDirty));
        public int SummaryCalls => CalculatorMock.Invocations.Count(i => i.Method.Name == nameof(ICircuitsCalculator.CalculateCollectorSummary));
        public int PipeSpacing { get; set; } = 200;
        public void ResetCounters()
        {
            DirtyMock.Invocations.Clear();
            StateMock.Invocations.Clear();
            CalculatorMock.Invocations.Clear();
        }
    }

    private sealed class ConstructionViewModelTestFactory
    {
        public ConstructionViewModel Create(IProjectSession session)
        {
            var materials = Material.GetDefaultMaterials();
            var repo = new Mock<IMaterialRepository>();
            repo.Setup(r => r.LoadMaterialsAsync()).ReturnsAsync(materials);
            repo.Setup(r => r.GetMaterialById(It.IsAny<int>())).Returns((int id) => materials.FirstOrDefault(m => m.Id == id));
            var templates = new Mock<IConstructionTemplateRepository>();
            templates.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ConstructionTemplate>());
            var context = new CalculationContext();
            var calculationState = new CalculationStateService(session);
            return new ConstructionViewModel(new Mock<IConstructionService>().Object, repo.Object, new Mock<IConstructionRepository>().Object, calculationState, context, new ConstructionValidator(), new SnowMeltingCalculator.Models.Construction.Construction(), new Mock<IMarkDirtyService>().Object, templates.Object, new Mock<IDialogService>().Object, new Mock<IEditorDialogService>().Object, session.ConstructionState, new ConstructionDefaultStateInitializer(repo.Object, session.ConstructionState));
        }
    }
}
