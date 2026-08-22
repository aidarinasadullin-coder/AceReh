using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using SnowMeltingCalculator.Configuration;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.ViewModels.Construction;
using SnowMeltingCalculator.ViewModels.Results;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.Tests.Services.Project;

[TestFixture]
[Apartment(System.Threading.ApartmentState.STA)]
public sealed class CanonicalDefaultConstructionLifecycleTests
{
    private static IEnumerable<int[]> MissingMaterialCases()
    {
        yield return new[] { 2 };
        yield return new[] { 5 };
        yield return new[] { 6 };
        yield return new[] { 10 };
        yield return new[] { 13 };
        yield return new[] { 2, 6, 13 };
    }

    [Test]
    public void Initializer_Apply_CreatesExactCanonicalDefaultRecipeWithFreshLayerIds()
    {
        var materials = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
        var state = new ProjectSessionConstructionState();
        var initializer = new ConstructionDefaultStateInitializer(CreateMaterialRepository(materials), state);

        var first = initializer.Apply(2.0, ConstructionMutationOrigin.Initialization);
        var firstIds = first.After.LayersAbovePipe.Concat(first.After.LayersBelowPipe).Select(layer => layer.Id).ToArray();
        var second = initializer.Apply(2.0, ConstructionMutationOrigin.Initialization);
        var secondIds = second.After.LayersAbovePipe.Concat(second.After.LayersBelowPipe).Select(layer => layer.Id).ToArray();

        AssertDefaultSnapshot(first.After, materials);
        AssertDefaultSnapshot(second.After, materials);
        Assert.Multiple(() =>
        {
            Assert.That(first.IsChanged, Is.True);
            Assert.That(second.IsChanged, Is.True);
            Assert.That(firstIds, Has.All.Not.EqualTo(Guid.Empty));
            Assert.That(firstIds.Distinct().Count(), Is.EqualTo(7));
            Assert.That(secondIds, Is.Not.EqualTo(firstIds));
        });
    }

    [Test]
    public void MissingRequiredDefaultMaterial_DoesNotPartiallyResetStateOrAdapter()
    {
        var materials = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddSingleton<IMaterialRepository>(CreateMaterialRepository(materials, 10));
        using var provider = services.BuildServiceProvider();
        var state = provider.GetRequiredService<IProjectSessionConstructionState>();
        var adapter = provider.GetRequiredService<ConstructionViewModel>();
        var customSnapshot = new ConstructionStateSnapshot(
            0.5,
            true,
            new[] { CreateLayer(materials[5], 333.0, LayerPosition.AbovePipe, 0) },
            Array.Empty<ConstructionLayerSnapshot>());
        state.ApplySnapshot(customSnapshot, ConstructionMutationOrigin.Initialization);
        adapter.ApplyLifecycleSnapshotToAdapter(customSnapshot);
        var stateBefore = state.Snapshot;
        var adapterAboveBefore = adapter.LayersAbovePipe.Select(ToRecipeTuple).ToArray();
        var adapterBelowBefore = adapter.LayersBelowPipe.Select(ToRecipeTuple).ToArray();
        var changedEvents = 0;
        state.Changed += (_, _) => changedEvents++;

        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<ConstructionDefaultStateInitializer>()
                .Apply(2.0, ConstructionMutationOrigin.Reset));

        Assert.Multiple(() =>
        {
            Assert.That(state.Snapshot, Is.EqualTo(stateBefore));
            Assert.That(adapter.GroundwaterLevel, Is.EqualTo(0.5));
            Assert.That(adapter.HasLoads, Is.True);
            Assert.That(adapter.LayersAbovePipe.Select(ToRecipeTuple), Is.EqualTo(adapterAboveBefore));
            Assert.That(adapter.LayersBelowPipe.Select(ToRecipeTuple), Is.EqualTo(adapterBelowBefore));
            Assert.That(changedEvents, Is.Zero);
        });
    }

    [TestCaseSource(nameof(MissingMaterialCases))]
    public void Initializer_MissingOneOrSeveralRequiredMaterials_ThrowsBeforeApply(int[] missingMaterialIds)
    {
        var materials = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
        var state = new ProjectSessionConstructionState();
        var initializer = new ConstructionDefaultStateInitializer(
            CreateMaterialRepository(materials, missingMaterialIds),
            state);
        var before = state.Snapshot;
        var changedEvents = 0;
        state.Changed += (_, _) => changedEvents++;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            initializer.Apply(2.0, ConstructionMutationOrigin.Initialization));

        Assert.Multiple(() =>
        {
            Assert.That(state.Snapshot, Is.EqualTo(before));
            Assert.That(changedEvents, Is.Zero);
            foreach (var missingMaterialId in missingMaterialIds)
            {
                Assert.That(exception!.Message, Does.Contain(missingMaterialId.ToString()));
            }
        });
    }

    [Test]
    public void Initializer_Success_RaisesOneChangedEventAndForwardsLifecycleOrigin()
    {
        var materials = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
        var state = new ProjectSessionConstructionState();
        var initializer = new ConstructionDefaultStateInitializer(CreateMaterialRepository(materials), state);
        var changedEvents = 0;
        ConstructionMutationOrigin? observedOrigin = null;
        state.Changed += (_, args) =>
        {
            changedEvents++;
            observedOrigin = args.Origin;
        };

        var result = initializer.Apply(2.0, ConstructionMutationOrigin.Reset);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsChanged, Is.True);
            Assert.That(result.Origin, Is.EqualTo(ConstructionMutationOrigin.Reset));
            Assert.That(changedEvents, Is.EqualTo(1));
            Assert.That(observedOrigin, Is.EqualTo(ConstructionMutationOrigin.Reset));
        });
    }

    [Test]
    public void Initializer_LifecycleApply_DoesNotDirtySessionOrPublishConstructionContext()
    {
        var materials = Material.GetDefaultMaterials().ToDictionary(material => material.Id);
        var context = new CalculationContext();
        var session = new ProjectSession(calculationContext: context);
        var initializer = new ConstructionDefaultStateInitializer(
            CreateMaterialRepository(materials),
            session.ConstructionState);
        var constructionPublications = 0;
        context.ContextChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(CalculationContext.Construction))
            {
                constructionPublications++;
            }
        };

        initializer.Apply(2.0, ConstructionMutationOrigin.Initialization);

        Assert.Multiple(() =>
        {
            Assert.That(session.IsDirty, Is.False);
            Assert.That(constructionPublications, Is.Zero);
            Assert.That(context.Construction, Is.Null);
        });
    }

    [Test]
    public void DependencyInjection_InitializerIsSingletonOverProjectSessionConstructionState()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<ConstructionDefaultStateInitializer>();
        var second = provider.GetRequiredService<ConstructionDefaultStateInitializer>();
        var state = provider.GetRequiredService<IProjectSessionConstructionState>();
        var session = provider.GetRequiredService<ProjectSession>();
        var initializerState = typeof(ConstructionDefaultStateInitializer)
            .GetField("_constructionState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(first);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(state, Is.SameAs(session.ConstructionState));
            Assert.That(initializerState, Is.SameAs(state));
        });
    }

    [Test]
    public async Task ColdStartup_DefaultUiExistsButCanonicalConstructionIsInitialized()
    {
        using var fixture = await ColdStartupFixture.CreateAsync();

        AssertDefaultAdapter(fixture.ConstructionViewModel, fixture.Materials);
        AssertDefaultSnapshot(fixture.State.Snapshot, fixture.Materials);
        AssertAdapterParity(fixture.ConstructionViewModel, fixture.State.Snapshot);
        Assert.Multiple(() =>
        {
            Assert.That(fixture.State.CurrentProjection.R1Total, Is.GreaterThan(0.0));
            Assert.That(fixture.State.CurrentProjection.R2Total, Is.GreaterThan(0.0));
            Assert.That(fixture.State.CurrentProjection.LambdaE,
                Is.EqualTo(fixture.Materials[5].LambdaA).Within(1e-10));
            Assert.That(fixture.Session.IsDirty, Is.False);
            Assert.That(fixture.Origins, Is.EqualTo(new[] { ConstructionMutationOrigin.Initialization }));
            Assert.That(fixture.ConstructionContextPublications, Is.Zero);
        });
    }

    [Test]
    public async Task ColdStartup_ImmediateSavePersistsAndRoundTripsCanonicalDefaultConstruction()
    {
        using var fixture = await ColdStartupFixture.CreateAsync();
        var project = fixture.ResultsViewModel.SaveCurrentProject();

        var filePath = Path.Combine(Path.GetTempPath(), $"canonical-default-{Guid.NewGuid():N}.smc");
        try
        {
            var fileService = new ProjectFileService();
            var saveResult = await fileService.SaveProjectResultAsync(filePath, project);
            var loadResult = await fileService.LoadProjectResultAsync(filePath);

            Assert.Multiple(() =>
            {
                Assert.That(saveResult.IsSuccess, Is.True, saveResult.Error);
                Assert.That(loadResult.IsSuccess, Is.True, loadResult.Error);
                Assert.That(loadResult.Value, Is.Not.Null);
            });

            AssertDefaultProjectData(project, fixture.Materials);
            AssertDefaultProjectData(loadResult.Value!, fixture.Materials);
            Assert.That(
                JsonNode.DeepEquals(
                    JsonSerializer.SerializeToNode(project),
                    JsonSerializer.SerializeToNode(loadResult.Value)),
                Is.True,
                "Immediate canonical-default save/load must preserve the complete project semantics.");
        }
        finally
        {
            File.Delete(filePath);
            File.Delete(filePath + ".bak");
            File.Delete(Path.ChangeExtension(filePath, ".tmp"));
        }
    }

    [Test]
    public async Task ColdStartup_ImmediateThermalUsesCanonicalDefaultProjection()
    {
        using var fixture = await ColdStartupFixture.CreateAsync();
        var climate = (ClimateData)fixture.Provider.GetRequiredService<IClimateData>();
        climate.AirTemperature = -5.0;
        climate.WindSpeed = 1.0;
        climate.Humidity = 70.0;
        climate.SnowfallIntensity = 0.5;

        var viewModel = fixture.Provider.GetRequiredService<ThermalViewModel>();
        viewModel.SelectedPipe = PipeType.StandardPipes[1];
        viewModel.SupplyTemperature = 60.0;
        var inputs = viewModel.BuildThermalInputs();
        var controlProjection = fixture.State.CurrentProjection;
        var control = new ThermalCalculator().Calculate(inputs with { LambdaE = controlProjection.LambdaE }, climate, controlProjection);

        await viewModel.CalculateCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.State.CurrentProjection.R1Total, Is.GreaterThan(0.0));
            Assert.That(fixture.State.CurrentProjection.R2Total, Is.GreaterThan(0.0));
            Assert.That(viewModel.Result, Is.Not.Null);
            Assert.That(viewModel.Result!.DeltaT, Is.EqualTo(control.DeltaT).Within(1e-10));
            Assert.That(viewModel.Result.ReturnTemperature, Is.EqualTo(control.ReturnTemperature).Within(1e-10));
            Assert.That(viewModel.Result.PowerDown, Is.EqualTo(control.PowerDown).Within(1e-10));
            Assert.That(double.IsFinite(viewModel.Result.DeltaT), Is.True);
            Assert.That(double.IsFinite(viewModel.Result.ReturnTemperature), Is.True);
            Assert.That(double.IsFinite(viewModel.Result.PowerDown), Is.True);
        });
    }

    internal static ConstructionStateSnapshot BuildDefaultSnapshot(IReadOnlyDictionary<int, Material> materials)
    {
        var above = new[] { CreateLayer(materials[5], 100.0, LayerPosition.AbovePipe, 0) };
        var below = new[]
        {
            CreateLayer(materials[5], 10.0, LayerPosition.BelowPipe, 0),
            CreateLayer(materials[6], 10.0, LayerPosition.BelowPipe, 1),
            CreateLayer(materials[10], 80.0, LayerPosition.BelowPipe, 2),
            CreateLayer(materials[13], 200.0, LayerPosition.BelowPipe, 3),
            CreateLayer(materials[2], 1000.0, LayerPosition.BelowPipe, 4),
            CreateLayer(materials[2], 570.0, LayerPosition.BelowPipe, 5)
        };
        return new ConstructionStateSnapshot(2.0, false, above, below);
    }

    internal static void AssertDefaultProjectData(ProjectData project, IReadOnlyDictionary<int, Material> materials)
    {
        var expected = BuildDefaultSnapshot(materials);
        Assert.Multiple(() =>
        {
            Assert.That(project.Version, Is.EqualTo("1.1"));
            Assert.That(project.ConstructionData.GroundwaterLevel, Is.EqualTo(2.0));
            Assert.That(project.ConstructionData.HasLoads, Is.False);
            Assert.That(project.ConstructionData.R1, Is.GreaterThan(0.0));
            Assert.That(project.ConstructionData.R2, Is.GreaterThan(0.0));
            Assert.That(project.ConstructionData.LambdaE, Is.EqualTo(materials[5].LambdaA).Within(1e-10));
            Assert.That(project.ConstructionData.Layers, Has.Count.EqualTo(7));
        });
        Assert.That(project.ConstructionData.Layers.Select(ToDtoRecipeTuple),
            Is.EqualTo(expected.LayersAbovePipe.Concat(expected.LayersBelowPipe).Select(ToDtoRecipeTuple)));
    }

    internal static void AssertDefaultSnapshot(ConstructionStateSnapshot snapshot, IReadOnlyDictionary<int, Material> materials)
    {
        var expected = BuildDefaultSnapshot(materials);
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.GroundwaterLevel, Is.EqualTo(2.0));
            Assert.That(snapshot.HasLoads, Is.False);
            Assert.That(snapshot.LayersAbovePipe.Select(ToRecipeTuple), Is.EqualTo(expected.LayersAbovePipe.Select(ToRecipeTuple)));
            Assert.That(snapshot.LayersBelowPipe.Select(ToRecipeTuple), Is.EqualTo(expected.LayersBelowPipe.Select(ToRecipeTuple)));
            Assert.That(snapshot.LayersAbovePipe.Concat(snapshot.LayersBelowPipe).All(layer => !layer.IsLambdaOverridden), Is.True);
        });
    }

    internal static void AssertAdapterParity(ConstructionViewModel adapter, ConstructionStateSnapshot snapshot)
    {
        Assert.Multiple(() =>
        {
            Assert.That(adapter.GroundwaterLevel, Is.EqualTo(snapshot.GroundwaterLevel));
            Assert.That(adapter.HasLoads, Is.EqualTo(snapshot.HasLoads));
            Assert.That(adapter.LayersAbovePipe.Select(ToRecipeTuple), Is.EqualTo(snapshot.LayersAbovePipe.Select(ToRecipeTuple)));
            Assert.That(adapter.LayersBelowPipe.Select(ToRecipeTuple), Is.EqualTo(snapshot.LayersBelowPipe.Select(ToRecipeTuple)));
        });
    }

    private static void AssertDefaultAdapter(ConstructionViewModel adapter, IReadOnlyDictionary<int, Material> materials)
    {
        var expected = BuildDefaultSnapshot(materials);
        Assert.Multiple(() =>
        {
            Assert.That(adapter.LayersAbovePipe.Select(ToRecipeTuple), Is.EqualTo(expected.LayersAbovePipe.Select(ToRecipeTuple)));
            Assert.That(adapter.LayersBelowPipe.Select(ToRecipeTuple), Is.EqualTo(expected.LayersBelowPipe.Select(ToRecipeTuple)));
            Assert.That(adapter.R1Total, Is.GreaterThan(0.0));
            Assert.That(adapter.R2Total, Is.GreaterThan(0.0));
            Assert.That(adapter.LambdaE, Is.EqualTo(materials[5].LambdaA).Within(1e-10));
        });
    }

    private static ConstructionLayerSnapshot CreateLayer(Material material, double thickness, LayerPosition position, int order) =>
        new(Guid.NewGuid(), material.Id, material.Name, thickness, material.LambdaA, false, position, order);

    private static IMaterialRepository CreateMaterialRepository(
        IReadOnlyDictionary<int, Material> materials,
        params int[] missingMaterialIds)
    {
        var missing = missingMaterialIds.ToHashSet();
        var repository = new Mock<IMaterialRepository>();
        repository.Setup(candidate => candidate.GetMaterialById(It.IsAny<int>()))
            .Returns((int id) => missing.Contains(id) ? null : materials.GetValueOrDefault(id));
        return repository.Object;
    }

    private static object ToRecipeTuple(ConstructionLayerSnapshot layer) =>
        (layer.Position, layer.Order, layer.MaterialId, layer.Thickness, layer.CalculatedLambda, layer.IsLambdaOverridden);

    private static object ToRecipeTuple(Layer layer) =>
        (layer.Position, layer.Order, layer.Material!.Id, layer.Thickness, layer.CalculatedLambda, layer.IsLambdaOverridden);

    private static object ToDtoRecipeTuple(ConstructionLayerSnapshot layer) =>
        (layer.Position, layer.Order, layer.MaterialName, layer.Thickness, layer.CalculatedLambda, layer.IsLambdaOverridden);

    private static object ToDtoRecipeTuple(LayerProjectData layer) =>
        (layer.Position, layer.Order, layer.MaterialName, layer.Thickness, layer.CalculatedLambda, layer.IsLambdaOverridden);

    private sealed class ColdStartupFixture : IDisposable
    {
        private ColdStartupFixture(ServiceProvider provider)
        {
            Provider = provider;
            Session = provider.GetRequiredService<IProjectSession>();
            State = provider.GetRequiredService<IProjectSessionConstructionState>();
            ConstructionViewModel = provider.GetRequiredService<ConstructionViewModel>();
            ResultsViewModel = provider.GetRequiredService<ResultsViewModel>();
            var context = provider.GetRequiredService<CalculationContext>();
            State.Changed += (_, args) => Origins.Add(args.Origin);
            context.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Construction))
                {
                    ConstructionContextPublications++;
                }
            };
        }

        public ServiceProvider Provider { get; }
        public IProjectSession Session { get; }
        public IProjectSessionConstructionState State { get; }
        public ConstructionViewModel ConstructionViewModel { get; }
        public ResultsViewModel ResultsViewModel { get; }
        public IReadOnlyDictionary<int, Material> Materials { get; private set; } = null!;
        public List<ConstructionMutationOrigin> Origins { get; } = new();
        public int ConstructionContextPublications { get; private set; }

        public static async Task<ColdStartupFixture> CreateAsync()
        {
            var services = new ServiceCollection();
            services.AddApplicationServices();
            var fixture = new ColdStartupFixture(services.BuildServiceProvider());
            await fixture.ConstructionViewModel.InitializeCommand.ExecuteAsync(null);
            fixture.Materials = fixture.Provider.GetRequiredService<IMaterialRepository>()
                .GetAllMaterials().ToDictionary(material => material.Id);
            return fixture;
        }

        public void Dispose() => Provider.Dispose();
    }
}
