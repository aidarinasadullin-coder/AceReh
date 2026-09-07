using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Tests.Fixtures;
using SnowMeltingCalculator.Services.Reports.Calculation;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Visualization;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.Services.Project;

[TestFixture]
public sealed class ClimateThermalInvalidationRegressionTests
{
    [Test]
    public void ChangedUserResetToDefaults_InvalidatesThermalExactlyOnceAndMarksDirty()
    {
        var fixture = CreateFixture();
        fixture.ClimateState.ApplyIndividualEdit(
            new ClimateEdit(ClimateEditField.AirTemperature, -25.0),
            ClimateMutationOrigin.SystemApply);
        fixture.ThermalViewModel.LoadResult(new ThermalCalculationResult { IsValid = true });
        var events = fixture.CaptureEvents();

        fixture.ClimateViewModel.Reset();

        AssertUserReset(fixture, events);
    }

    [Test]
    public void ChangedUserResetToCityData_InvalidatesThermalExactlyOnceAndMarksDirty()
    {
        var fixture = CreateFixture();
        var city = new CityInfo
        {
            Name = "Reset city",
            Region = "Reset region",
            T5Days092 = -30.0,
            WindAvgTempLe8 = 4.0,
            Humidity15hCold = 60.0
        };
        fixture.ClimateViewModel.SelectedCity = city;
        fixture.ClimateViewModel.WindSpeed = 9.0;
        fixture.ThermalViewModel.LoadResult(new ThermalCalculationResult { IsValid = true });
        fixture.Session.MarkClean();
        var events = fixture.CaptureEvents();

        fixture.ClimateViewModel.ResetToCityDataCommand.Execute(null);

        AssertUserReset(fixture, events);
    }

    [Test]
    public void NoOpUserResetToDefaults_IsSilent()
    {
        var fixture = CreateFixture();
        var setup = fixture.ClimateState.ResetToDefaults(ClimateMutationOrigin.SystemApply);
        Assert.That(setup.NewSnapshot, Is.EqualTo(CanonicalDefaultClimate));
        fixture.Session.MarkClean();
        var events = fixture.CaptureEvents();

        fixture.ClimateViewModel.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ClimateState.Snapshot, Is.EqualTo(CanonicalDefaultClimate));
            Assert.That(events.Completions, Is.Zero);
            Assert.That(events.CompatibilityEvents, Is.Zero);
            Assert.That(events.ContextUpdates, Is.Zero);
            Assert.That(events.ThermalStates, Is.Zero);
            Assert.That(fixture.Session.IsDirty, Is.False);
        });
    }

    [Test]
    public void ChangedProjectLoadReset_SynchronizesWithoutThermalInvalidationOrDirty()
    {
        var fixture = CreateFixture();
        fixture.ClimateState.ApplyIndividualEdit(
            new ClimateEdit(ClimateEditField.AirTemperature, -25.0),
            ClimateMutationOrigin.SystemApply);
        fixture.ThermalViewModel.LoadResult(new ThermalCalculationResult { IsValid = true });
        var events = fixture.CaptureEvents();

        fixture.Orchestrator.ResetModules();

        Assert.Multiple(() =>
        {
            Assert.That(events.Completions, Is.EqualTo(1));
            Assert.That(events.CompatibilityEvents, Is.Zero);
            Assert.That(events.ContextUpdates, Is.EqualTo(1));
            Assert.That(events.ThermalStates, Is.Zero);
            Assert.That(fixture.Session.IsDirty, Is.False);
        });
    }

    [Test]
    public void ResetModules_CanonicallyClearsRestoredThermalResultWithLifecycleOrigin()
    {
        // DEC-T08 (Todo 9): lifecycle reset применяет канонические дефолты с
        // не-пользовательским origin ProjectLoadReset — результат очищается,
        // статус нормализуется к Actual, ноль user-dirty и ноль compatibility.
        var fixture = CreateFixture();
        fixture.ThermalViewModel.LoadResult(new ThermalCalculationResult { PowerTotal = 111.0, IsValid = true });
        var thermalMutations = new List<ThermalMutationResult>();
        fixture.Session.ThermalState.Changed += (_, args) => thermalMutations.Add(args.Mutation);
        var events = fixture.CaptureEvents();

        fixture.Orchestrator.ResetModules();

        Assert.Multiple(() =>
        {
            var resetMutation = thermalMutations.Single(mutation => mutation.Origin == ThermalMutationOrigin.ProjectLoadReset);
            Assert.That(resetMutation.After.Result, Is.Null);
            Assert.That(resetMutation.After.Inputs, Is.EqualTo(ThermalInputsSnapshot.Default));
            Assert.That(resetMutation.After.Status.Phase, Is.EqualTo(ThermalCalculationPhase.Actual));
            // Статус Default → Default: трансляция в StateChanged("Thermal") молчит.
            Assert.That(events.ThermalStates, Is.Zero);
            Assert.That(events.CompatibilityEvents, Is.Zero);
            Assert.That(events.ContextUpdates, Is.Zero);
            Assert.That(fixture.Session.IsDirty, Is.False);
        });
    }

    [Test]
    public async Task ProjectLoad_DoesNotInvalidateRestoredThermalResult()
    {
        // Given: the current graph has a valid Thermal result and the incoming project has
        // different Climate data plus its own distinguishable valid saved Thermal result.
        var fixture = CreateFixture();
        fixture.ClimateState.ApplyIndividualEdit(
            new ClimateEdit(ClimateEditField.AirTemperature, -25.0),
            ClimateMutationOrigin.SystemApply);
        fixture.ThermalViewModel.LoadResult(new ThermalCalculationResult
        {
            PowerTotal = 111.0,
            IsValid = true
        });
        var thermalStates = new List<ModuleStateChangedEventArgs>();
        fixture.CalculationStateService.StateChanged += (_, args) =>
        {
            if (args.Module == "Thermal")
            {
                thermalStates.Add(args);
            }
        };
        var project = new ProjectData
        {
            ClimateData = new ClimateProjectData
            {
                SelectedCity = "Loaded city",
                Region = "Loaded region",
                AirTemperature = -20.0,
                WindSpeed = 7.0,
                Humidity = 80.0,
                SnowfallIntensity = 3.0,
                SelectedZone = ClimateZone.Zone_M20
            },
            ConstructionData = new ConstructionProjectData(),
            ThermalData = new ThermalProjectData
            {
                SelectedMode = OperatingMode.Intensive,
                SupplyTemperature = 55.0,
                GroundTemperature = 8.0,
                PipeSpacing = 200,
                Result = new ThermalResultProjectData
                {
                    PowerTotal = 777.0,
                    SupplyTemperature = 55.0,
                    ReturnTemperature = 40.0,
                    MeanTemperature = 47.5,
                    DeltaT = 15.0,
                    IsValid = true
                }
            },
            HydraulicsData = new HydraulicsProjectData()
        };

        // When: the project enters through the public Results load boundary.
        await fixture.ResultsViewModel.LoadProjectDataAsync(project);

        // Then: the saved result is restored and load lifecycle Climate publication is silent.
        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(777.0));
            Assert.That(fixture.ThermalViewModel.Result.IsValid, Is.True);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.False);
            Assert.That(thermalStates, Is.Empty);
        });
    }

    [Test]
    public async Task PostLoadUserClimateEdit_InvalidatesThermalExactlyOnceAndMarksDirtyAcrossRepeatedLoads()
    {
        var fixture = CreateFixture();
        var project = CreateProject(new ThermalResultProjectData { PowerTotal = 777.0, IsValid = true });

        await fixture.ResultsViewModel.LoadProjectDataAsync(project);
        var firstEvents = fixture.CaptureEvents();
        fixture.ClimateViewModel.WindSpeed = 8.0;

        AssertPostLoadUserEdit(fixture, firstEvents);

        await fixture.ResultsViewModel.LoadProjectDataAsync(project);
        var secondEvents = fixture.CaptureEvents();
        fixture.ClimateViewModel.WindSpeed = 9.0;

        AssertPostLoadUserEdit(fixture, secondEvents);
    }

    [Test]
    public async Task RepeatedResetAndLoad_DoesNotMultiplyClimateOrThermalEvents()
    {
        var fixture = CreateFixture();
        fixture.ClimateState.ApplyIndividualEdit(
            new ClimateEdit(ClimateEditField.AirTemperature, -25.0),
            ClimateMutationOrigin.SystemApply);
        fixture.ThermalViewModel.LoadResult(new ThermalCalculationResult { PowerTotal = 111.0, IsValid = true });
        fixture.Session.MarkClean();
        var events = fixture.CaptureEvents();
        var project = CreateProject(new ThermalResultProjectData { PowerTotal = 777.0, IsValid = true });

        await ResetAndRestoreAsync(fixture, project);
        var firstCycle = events.Snapshot();
        events.Reset();
        await ResetAndRestoreAsync(fixture, project);

        Assert.Multiple(() =>
        {
            Assert.That(firstCycle.Completions, Is.EqualTo(2));
            Assert.That(events.Completions, Is.EqualTo(firstCycle.Completions));
            Assert.That(firstCycle.ContextUpdates, Is.EqualTo(2));
            Assert.That(events.ContextUpdates, Is.EqualTo(firstCycle.ContextUpdates));
            Assert.That(firstCycle.CompatibilityEvents, Is.Zero);
            Assert.That(events.CompatibilityEvents, Is.Zero);
            Assert.That(firstCycle.ThermalStates, Is.Zero);
            Assert.That(events.ThermalStates, Is.Zero);
            Assert.That(fixture.ThermalViewModel.Result?.PowerTotal, Is.EqualTo(777.0));
            Assert.That(fixture.Session.IsDirty, Is.False);
        });
    }

    [Test]
    public async Task ProjectLoadWithoutSavedThermalResult_CalculatesOnceWithoutClimateInvalidation()
    {
        var fixture = CreateFixture();
        fixture.ClimateState.ApplyIndividualEdit(
            new ClimateEdit(ClimateEditField.AirTemperature, -25.0),
            ClimateMutationOrigin.SystemApply);
        fixture.ThermalViewModel.LoadResult(new ThermalCalculationResult { PowerTotal = 111.0, IsValid = true });
        fixture.Session.MarkClean();
        var events = fixture.CaptureEvents();

        await ResetAndRestoreAsync(fixture, CreateProject(result: null));

        Assert.Multiple(() =>
        {
            fixture.ThermalCalculator.Verify(calculator => calculator.Calculate(
                It.IsAny<ThermalInputs>(), It.IsAny<IClimateData>(), It.IsAny<IConstructionData>()), Times.Once);
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.ThermalViewModel.Result!.PowerTotal, Is.EqualTo(555.0));
            Assert.That(fixture.ThermalViewModel.Result.IsValid, Is.True);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.False);
            Assert.That(events.CompatibilityEvents, Is.Zero);
            Assert.That(events.ThermalStates, Is.EqualTo(2));
            Assert.That(fixture.Session.IsDirty, Is.False);
        });
    }

    private static readonly ClimateStateSnapshot CanonicalDefaultClimate = new(
        string.Empty,
        string.Empty,
        -15.0,
        0.0,
        5.0,
        70.0,
        0.0,
        ClimateZone.Zone_M15,
        false,
        false,
        false);

    private static ProjectData CreateProject(ThermalResultProjectData? result)
    {
        return new ProjectData
        {
            ClimateData = new ClimateProjectData
            {
                SelectedCity = "Loaded city",
                Region = "Loaded region",
                AirTemperature = -20.0,
                WindSpeed = 7.0,
                Humidity = 80.0,
                SnowfallIntensity = 3.0,
                SelectedZone = ClimateZone.Zone_M20
            },
            ConstructionData = new ConstructionProjectData(),
            ThermalData = new ThermalProjectData
            {
                SelectedMode = OperatingMode.Intensive,
                SupplyTemperature = 55.0,
                GroundTemperature = 8.0,
                PipeSpacing = 200,
                Result = result
            },
            HydraulicsData = new HydraulicsProjectData()
        };
    }

    private static async Task ResetAndRestoreAsync(Fixture fixture, ProjectData project)
    {
        fixture.Orchestrator.ResetModules();
        using var restoreScope = fixture.Session.BeginProjectRestore();
        fixture.CalculationStateService.IsLoadProjectInProgress = true;
        try
        {
            await fixture.Orchestrator.RestoreModulesFromProjectAsync(project);
            fixture.Session.MarkClean();
        }
        finally
        {
            fixture.CalculationStateService.IsLoadProjectInProgress = false;
        }
    }

    private static Fixture CreateFixture()
    {
        var context = new CalculationContext();
        var climateData = new ClimateData();
        var session = new ProjectSession(climateData, context);
        var calculationState = new CalculationStateService(session);
        var materials = Material.GetDefaultMaterials();
        var materialRepository = new Mock<IMaterialRepository>();
        materialRepository.Setup(repository => repository.LoadMaterialsAsync()).ReturnsAsync(materials);
        materialRepository.Setup(repository => repository.GetMaterialById(It.IsAny<int>()))
            .Returns((int id) => materials.FirstOrDefault(material => material.Id == id));
        var templateRepository = new Mock<IConstructionTemplateRepository>();
        templateRepository.Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());
        var constructionService = new Mock<IConstructionService>();
        constructionService.Setup(service => service.ImportProjectMaterialsAsync(It.IsAny<IEnumerable<MaterialSnapshot>>()))
            .Returns(Task.CompletedTask);
        constructionService.Setup(service => service.ImportProjectTemplatesAsync(It.IsAny<IEnumerable<ConstructionTemplate>>()))
            .Returns(Task.CompletedTask);
        var defaultInitializer = new ConstructionDefaultStateInitializer(materialRepository.Object, session.ConstructionState);
        var constructionViewModel = new ConstructionViewModel(
            constructionService.Object,
            materialRepository.Object,
            new Mock<IConstructionRepository>().Object,
            calculationState,
            context,
            new ConstructionValidator(),
            new global::SnowMeltingCalculator.Models.Construction.Construction(),
            session,
            templateRepository.Object,
            new Mock<global::SnowMeltingCalculator.Services.Navigation.IDialogService>().Object,
            new Mock<global::SnowMeltingCalculator.Services.Navigation.IEditorDialogService>().Object,
            session.ConstructionState,
            defaultInitializer);
        var climateService = new Mock<IClimateDataService>();
        climateService.Setup(service => service.LoadClimateDataAsync()).Returns(Task.CompletedTask);
        climateService.Setup(service => service.GetAllCities()).Returns(Array.Empty<CityInfo>());
        var climateViewModel = new ClimateViewModel(
            climateService.Object,
            climateData,
            new ClimateValidator(),
            session);
        var thermalValidator = new Mock<IValidator<ThermalInputs>>();
        thermalValidator.Setup(validator => validator.Validate(It.IsAny<ThermalInputs>()))
            .Returns(ValidationResult.Success());
        var thermalCalculator = new Mock<IThermalCalculator>();
        thermalCalculator.Setup(calculator => calculator.Calculate(
                It.IsAny<ThermalInputs>(), It.IsAny<IClimateData>(), It.IsAny<IConstructionData>()))
            .Returns(new ThermalCalculationResult
            {
                PowerTotal = 555.0,
                IsValid = true
            });
        var thermalViewModel = new ThermalViewModel(
            thermalCalculator.Object,
            climateData,
            session.ConstructionState.CurrentProjection,
            calculationState,
            context,
            thermalValidator.Object,
            new ThermalResultValidator(),
            session);
        var circuitsViewModel = CreateCircuitsViewModel(calculationState, context, session);
        var orchestrator = new ProjectLoadOrchestrator(
            climateViewModel,
            constructionViewModel,
            thermalViewModel,
            circuitsViewModel,
            calculationState,
            constructionService.Object,
            context,
            session,
            defaultInitializer);
        var projectState = new ProjectStateService(session);
        var resultsViewModel = new ResultsViewModel(
            session,
            new Mock<global::SnowMeltingCalculator.Services.Navigation.IDialogService>().Object,
                new Mock<IPdfExportService>().Object,
                new Mock<IProjectFileService>().Object,
            calculationState,
            materialRepository.Object,
            constructionService.Object,
            orchestrator,
            new ResultsPdfDataBuilder(
                new Mock<IConstructionVisualizationImageService>().Object,
                calculationState,
                constructionViewModel,
                circuitsViewModel),
            new HydraulicSummaryBuilder());

        return new Fixture(
            orchestrator,
            resultsViewModel,
            session,
            context,
            climateData,
            climateViewModel,
            calculationState,
            thermalViewModel,
            thermalCalculator);
    }

    private static void AssertUserReset(Fixture fixture, Events events)
    {
        Assert.Multiple(() =>
        {
            Assert.That(events.Completions, Is.EqualTo(1));
            Assert.That(events.Origins, Is.EqualTo(new[] { ClimateMutationOrigin.UserReset }));
            Assert.That(events.CompatibilityEvents, Is.EqualTo(1));
            Assert.That(events.ContextUpdates, Is.EqualTo(1));
            Assert.That(events.ThermalStates, Is.EqualTo(1));
            Assert.That(fixture.ThermalViewModel.Result, Is.Null);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.True);
            Assert.That(fixture.Session.IsDirty, Is.True);
        });
    }

    private static void AssertPostLoadUserEdit(Fixture fixture, Events events)
    {
        Assert.Multiple(() =>
        {
            Assert.That(events.Completions, Is.EqualTo(1));
            Assert.That(events.Origins, Is.EqualTo(new[] { ClimateMutationOrigin.User }));
            Assert.That(events.CompatibilityEvents, Is.EqualTo(1));
            Assert.That(events.ContextUpdates, Is.EqualTo(1));
            Assert.That(events.ThermalStates, Is.EqualTo(1));
            Assert.That(fixture.ThermalViewModel.Result, Is.Null);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.True);
            Assert.That(fixture.Session.IsDirty, Is.True);
        });
    }

    private static CircuitsViewModel CreateCircuitsViewModel(
        ICalculationStateService calculationState,
        CalculationContext context,
        global::SnowMeltingCalculator.Services.Results.IMarkDirtyService markDirtyService)
    {
        var calculator = new Mock<ICircuitsCalculator>();
        calculator.Setup(service => service.CalculateCollectorSummary(
                It.IsAny<List<CircuitRow>>(), It.IsAny<int>(), It.IsAny<ValveType>()))
            .Returns(new CollectorSummary());
        var glycol = new Mock<IGlycolDataService>();
        glycol.Setup(service => service.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(new GlycolProperties { Density = 1050, SpecificHeat = 3800, KinematicViscosity = 0.000005 });
        var selector = new Mock<ICollectorTypeSelector>();
        selector.Setup(service => service.SelectCollectorType(It.IsAny<CollectorData>()))
            .Returns(new CollectorSelectionResult { ValveType = ValveType.HKV_D });
        var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(calculationState, context);
        return new CircuitsViewModel(
            calculator.Object,
            glycol.Object,
            calculationState,
            new Mock<ICircuitsValidator>().Object,
            selector.Object,
             context,
              hydraulicsDependencies.Coordinator,
                  hydraulicsDependencies.Session);
    }

    private sealed record Fixture(
        ProjectLoadOrchestrator Orchestrator,
        ResultsViewModel ResultsViewModel,
        ProjectSession Session,
        CalculationContext Context,
        ClimateData ClimateData,
        ClimateViewModel ClimateViewModel,
        CalculationStateService CalculationStateService,
        ThermalViewModel ThermalViewModel,
        Mock<IThermalCalculator> ThermalCalculator)
    {
        public IProjectSessionClimateState ClimateState => Session.ClimateState;

        public Events CaptureEvents()
        {
            var events = new Events();
            ClimateState.Changed += (_, args) =>
            {
                events.Completions++;
                events.Origins.Add(args.Origin);
            };
            ClimateData.DataChanged += (_, _) => events.CompatibilityEvents++;
            Context.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Climate))
                {
                    events.ContextUpdates++;
                }
            };
            CalculationStateService.StateChanged += (_, args) =>
            {
                if (args.Module == "Thermal")
                {
                    events.ThermalStates++;
                }
            };
            return events;
        }
    }

    private sealed class Events
    {
        public int Completions { get; set; }
        public List<ClimateMutationOrigin> Origins { get; } = new();
        public int CompatibilityEvents { get; set; }
        public int ContextUpdates { get; set; }
        public int ThermalStates { get; set; }

        public Events Snapshot() => new()
        {
            Completions = Completions,
            CompatibilityEvents = CompatibilityEvents,
            ContextUpdates = ContextUpdates,
            ThermalStates = ThermalStates
        };

        public void Reset()
        {
            Completions = 0;
            Origins.Clear();
            CompatibilityEvents = 0;
            ContextUpdates = 0;
            ThermalStates = 0;
        }
    }
}
