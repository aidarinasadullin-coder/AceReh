using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.Services.Project;

[TestFixture]
public sealed class ConstructionThermalInvalidationRegressionTests
{
    [TestCase(ConstructionChange.Material)]
    [TestCase(ConstructionChange.Thickness)]
    [TestCase(ConstructionChange.CalculatedLambda)]
    public void UserMutation_WithExistingResult_InvalidatesThermalOnce(ConstructionChange change)
    {
        var fixture = CreateFixture();
        var events = fixture.SubscribePublicationEvents();

        var mutation = fixture.State.Apply(CreateUserMutation(fixture.LayerId, change), ConstructionMutationOrigin.User);

        AssertThermalInvalidated(fixture, mutation, ConstructionMutationOrigin.User, events);
    }

    [Test]
    public async Task SuccessfulRecalculation_AfterInvalidation_ReturnsThermalToActual()
    {
        var fixture = CreateFixture();
        var events = fixture.SubscribePublicationEvents();
        var mutation = fixture.State.Apply(
            new ConstructionMutation.EditLayer(fixture.LayerId, 5, "Concrete", 120.0, 1.6, false),
            ConstructionMutationOrigin.User);
        AssertThermalInvalidated(fixture, mutation, ConstructionMutationOrigin.User, events);
        fixture.ThermalViewModel.SelectedPipe = PipeType.StandardPipes[1];

        await fixture.ThermalViewModel.CalculateCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.False);
            Assert.That(events.ThermalStates.Select(args => args.State).ToArray(),
                Is.EqualTo(new[] { ModuleState.NeedsRecalculation, ModuleState.Calculating, ModuleState.Actual }));
        });
    }

    [Test]
    public void TemplateMutation_WithExistingResult_InvalidatesThermalOnce()
    {
        var fixture = CreateFixture();
        var events = fixture.SubscribePublicationEvents();
        var templateSnapshot = new ConstructionStateSnapshot(
            fixture.Snapshot.GroundwaterLevel,
            new[]
            {
                new ConstructionLayerSnapshot(fixture.LayerId, 5, "Concrete", 130.0, 1.6, false, LayerPosition.AbovePipe, 0),
                new ConstructionLayerSnapshot(Guid.NewGuid(), 7, "Asphalt", 50.0, 0.8, false, LayerPosition.AbovePipe, 1)
            },
            fixture.Snapshot.LayersBelowPipe);

        var mutation = fixture.State.ApplySnapshot(templateSnapshot, ConstructionMutationOrigin.Template);

        AssertThermalInvalidated(fixture, mutation, ConstructionMutationOrigin.Template, events);
    }

    [Test]
    public void UserMutation_WithoutExistingResult_PublishesProjectionButDoesNotInvalidateThermal()
    {
        var fixture = CreateFixture(loadResult: false);
        var events = fixture.SubscribePublicationEvents();

        var mutation = fixture.State.Apply(
            new ConstructionMutation.EditLayer(fixture.LayerId, 5, "Concrete", 120.0, 1.6, false),
            ConstructionMutationOrigin.User);

        Assert.That(mutation.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
        Assert.That(fixture.ThermalViewModel.Result, Is.Null);
        Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.False);
        Assert.That(events.ThermalStates, Is.Empty);
        Assert.That(events.ProjectionNotifications, Is.EqualTo(1));
        Assert.That(events.ContextPublications, Is.EqualTo(1));
    }

    [Test]
    public void NoChange_WithExistingResult_IsSilent()
    {
        var fixture = CreateFixture();
        var events = fixture.SubscribePublicationEvents();

        var mutation = fixture.State.Apply(
            new ConstructionMutation.EditLayer(fixture.LayerId, 5, "Concrete", 100.0, 1.6, false),
            ConstructionMutationOrigin.User);

        AssertSilent(fixture, mutation, ConstructionMutationStatus.NoChange, events);
    }

    [Test]
    public void Rejected_WithExistingResult_IsSilent()
    {
        var fixture = CreateFixture();
        var events = fixture.SubscribePublicationEvents();

        var mutation = fixture.State.Apply(
            new ConstructionMutation.EditLayer(Guid.NewGuid(), 5, "Missing", 120.0, 1.6, false),
            ConstructionMutationOrigin.User);

        AssertSilent(fixture, mutation, ConstructionMutationStatus.Rejected, events);
    }

    [TestCase(ConstructionMutationOrigin.Initialization)]
    [TestCase(ConstructionMutationOrigin.ProjectLoad)]
    [TestCase(ConstructionMutationOrigin.Reset)]
    public void LifecycleMutation_WithExistingResult_IsSilent(ConstructionMutationOrigin origin)
    {
        var fixture = CreateFixture();
        var events = fixture.SubscribePublicationEvents();
        var candidate = new ConstructionStateSnapshot(
            fixture.Snapshot.GroundwaterLevel + 1.0,
            fixture.Snapshot.LayersAbovePipe,
            fixture.Snapshot.LayersBelowPipe);

        var mutation = fixture.State.ApplySnapshot(candidate, origin);

        AssertSilent(fixture, mutation, ConstructionMutationStatus.Changed, events);
        Assert.That(fixture.State.Snapshot, Is.EqualTo(candidate));
    }

    [Test]
    public void Cancelled_IsNotReachableFromCurrentProductionMutationPaths()
    {
        var sourceRoot = Path.Combine(FindRepositoryRoot(), "src");
        var cancelledCompletion = new Regex(
            @"(?:new\s+ConstructionMutationResult|return[^;]*ConstructionMutationStatus)\s*\([^;]*ConstructionMutationStatus\.Cancelled",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        var matches = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => cancelledCompletion.IsMatch(File.ReadAllText(path)))
            .Select(Path.GetFullPath)
            .ToArray();

        Assert.That(matches, Is.Empty,
            "Cancelled is currently an application-boundary status and must not have a canonical production completion path.");
    }

    private static void AssertThermalInvalidated(
        Fixture fixture,
        ConstructionMutationResult mutation,
        ConstructionMutationOrigin origin,
        PublicationEvents events)
    {
        Assert.That(mutation.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
        Assert.That(mutation.Origin, Is.EqualTo(origin));
        Assert.Multiple(() =>
        {
            Assert.That(fixture.ThermalViewModel.Result, Is.Null);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.True);
            Assert.That(events.ThermalStates.Select(args => args.State).ToArray(),
                Is.EqualTo(new[] { ModuleState.NeedsRecalculation }));
            Assert.That(events.ProjectionNotifications, Is.EqualTo(1));
            Assert.That(events.ContextPublications, Is.EqualTo(1));
            Assert.That(events.PublicationOrder, Is.EqualTo(new[] { "projection", "context" }));
        });
    }

    private static void AssertSilent(
        Fixture fixture,
        ConstructionMutationResult mutation,
        ConstructionMutationStatus expectedStatus,
        PublicationEvents events)
    {
        Assert.Multiple(() =>
        {
            Assert.That(mutation.Status, Is.EqualTo(expectedStatus));
            Assert.That(fixture.ThermalViewModel.Result, Is.Not.Null);
            Assert.That(fixture.CalculationStateService.ThermalNeedsRecalculation, Is.False);
            Assert.That(events.ThermalStates, Is.Empty);
            Assert.That(events.ProjectionNotifications, Is.Zero);
            Assert.That(events.ContextPublications, Is.Zero);
            Assert.That(events.PublicationOrder, Is.Empty);
        });
    }

    private static ConstructionMutation CreateUserMutation(Guid layerId, ConstructionChange change)
    {
        return change switch
        {
            ConstructionChange.Material => new ConstructionMutation.EditLayer(layerId, 7, "Asphalt", 100.0, 1.6, false),
            ConstructionChange.Thickness => new ConstructionMutation.EditLayer(layerId, 5, "Concrete", 120.0, 1.6, false),
            ConstructionChange.CalculatedLambda => new ConstructionMutation.EditLayer(layerId, 5, "Concrete", 100.0, 1.2, true),
            _ => throw new ArgumentOutOfRangeException(nameof(change), change, null)
        };
    }

    private static Fixture CreateFixture(bool loadResult = true)
    {
        var calculationContext = new CalculationContext();
        var projectSession = new ProjectSession(calculationContext: calculationContext);
        var layerId = Guid.NewGuid();
        var snapshot = new ConstructionStateSnapshot(
            2.0,
            new[] { new ConstructionLayerSnapshot(layerId, 5, "Concrete", 100.0, 1.6, false, LayerPosition.AbovePipe, 0) },
            new[] { new ConstructionLayerSnapshot(Guid.NewGuid(), 2, "Ground", 200.0, 1.5, false, LayerPosition.BelowPipe, 0) });
        var initialization = projectSession.ConstructionState.ApplySnapshot(snapshot, ConstructionMutationOrigin.Initialization);
        Assert.That(initialization.IsChanged, Is.True);

        var climateData = new ClimateData { AirTemperature = -10.0, WindSpeed = 5.0, SnowfallIntensity = 2.0, Humidity = 70.0 };
        var calculator = new ThermalCalculator();
        var calculationStateService = new CalculationStateService(projectSession);
        var thermalViewModel = new ThermalViewModel(
            calculator,
            climateData,
            projectSession.ConstructionState.CurrentProjection,
            calculationStateService,
            calculationContext,
            new ThermalValidator(calculator, climateData, projectSession.ConstructionState.CurrentProjection),
            new ThermalResultValidator(),
            projectSession);
        if (loadResult)
        {
            thermalViewModel.LoadResult(new ThermalCalculationResult { IsValid = true });
        }

        return new Fixture(projectSession, calculationContext, layerId, snapshot, calculationStateService, thermalViewModel);
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "SnowMeltingCalculator.sln")))
        {
            directory = Directory.GetParent(directory)?.FullName ?? string.Empty;
        }

        Assert.That(directory, Is.Not.Empty);
        return directory;
    }

    private sealed class Fixture
    {
        public Fixture(ProjectSession session, CalculationContext context, Guid layerId, ConstructionStateSnapshot snapshot, CalculationStateService calculationStateService, ThermalViewModel thermalViewModel)
        {
            Session = session;
            Context = context;
            LayerId = layerId;
            Snapshot = snapshot;
            CalculationStateService = calculationStateService;
            ThermalViewModel = thermalViewModel;
        }

        public ProjectSession Session { get; }
        public CalculationContext Context { get; }
        public IProjectSessionConstructionState State => Session.ConstructionState;
        public Guid LayerId { get; }
        public ConstructionStateSnapshot Snapshot { get; }
        public CalculationStateService CalculationStateService { get; }
        public ThermalViewModel ThermalViewModel { get; }

        public PublicationEvents SubscribePublicationEvents()
        {
            var events = new PublicationEvents();
            State.CurrentProjection.DataChanged += (_, _) => events.Record("projection");
            Context.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Construction))
                {
                    events.Record("context");
                }
            };
            CalculationStateService.StateChanged += (_, args) =>
            {
                if (args.Module == "Thermal")
                {
                    events.ThermalStates.Add(args);
                }
            };
            return events;
        }
    }

    private sealed class PublicationEvents
    {
        public List<ModuleStateChangedEventArgs> ThermalStates { get; } = new();
        public List<string> PublicationOrder { get; } = new();
        public int ProjectionNotifications => PublicationOrder.Count(name => name == "projection");
        public int ContextPublications => PublicationOrder.Count(name => name == "context");
        public void Record(string publication) => PublicationOrder.Add(publication);
    }

    public enum ConstructionChange
    {
        Material,
        Thickness,
        CalculatedLambda
    }
}
