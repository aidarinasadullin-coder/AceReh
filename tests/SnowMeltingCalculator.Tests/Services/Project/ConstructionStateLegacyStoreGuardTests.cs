using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Phase 3 Task 2: deterministic source-text inventory of the CURRENT
    /// Construction writer/subscriber surface, before any ConstructionState
    /// ownership migration exists. Mirrors the accepted Phase 2
    /// ClimateStateLegacyStoreGuardTests pattern, adapted to Construction's
    /// mutable Layer/collection/template/editor complexity.
    /// </summary>
    [TestFixture]
    public sealed class ConstructionStateLegacyStoreGuardTests
    {
        private static readonly string[] ConstructionViewModelMutationBoundaries =
        {
            "public void Reset()",
            "private void AddLayerAbovePipe()",
            "private void AddLayerBelowPipe()",
            "private void RemoveLayer(Layer? layer)",
            "private void ApplyTemplateCore(ConstructionTemplate template)",
            "public void OnLayerChanged(Layer layer)",
            "partial void OnGroundwaterLevelChanged(double value)",
            "partial void OnHasLoadsChanged(bool value)",
            "private void OnLayersCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)",
            "private void OnLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)",
            "private void OnConstructionDataChanged(object? sender, ConstructionDataChangedEventArgs e)",
            "private void SyncFromModel()",
            "private void SyncToModel()",
            "private void CopyConstructionData(ConstructionModel source)"
        };

        [Test]
        public void ConstructionStateLegacyStoreGuard_CapturesExactCurrentWriterInventory()
        {
            var viewModelSource = ReadSource("src/ViewModels/Construction/ConstructionViewModel.cs");
            var constructionModelSource = ReadSource("src/Models/Construction/Construction.cs");
            var orchestratorSource = ReadSource("src/Services/Project/ProjectLoadOrchestrator.cs");
            var mainViewModelSource = ReadSource("src/ViewModels/Shell/MainViewModel.cs");
            var initializerSource = ReadSource("src/Services/Project/ConstructionDefaultStateInitializer.cs");
            var resultsSource = ReadSource("src/ViewModels/Results/ResultsViewModel.cs");
            var calculationContextSource = ReadSource("src/Core/CalculationContext.cs");
            var serviceRegistrationSource = ReadSource("src/Configuration/ServiceCollectionExtensions.cs");
            var projectSessionSource = ReadSource("src/Services/Project/ProjectSession.cs");

            Assert.Multiple(() =>
            {
                // --- Every current mutation boundary must still be present (inventory completeness) ---
                Assert.That(
                    ConstructionViewModelMutationBoundaries.Where(boundary => !viewModelSource.Contains(boundary, StringComparison.Ordinal)),
                    Is.Empty,
                    "A legacy ConstructionViewModel mutation boundary disappeared from the inventory.");

                // --- Post-Task-6: MarkDirty still called directly; count reduced because Add/Remove commands
                // now shadow-write via SyncStateFromCollections (the actual count is 7 after Task 6 partial migration).
                Assert.That(GetMarkDirtyCallCount(viewModelSource), Is.EqualTo(0),
                    "ConstructionViewModel must not call MarkDirty directly after Task 10.");

                // --- ConstructionViewModel currently publishes to CalculationContext directly (bypass #2) ---
                Assert.That(viewModelSource, Does.Not.Contain("_calculationContext.UpdateConstruction("),
                    "ConstructionViewModel must not publish directly to CalculationContext after Task 10.");

                // --- ConstructionViewModel currently holds a mutable model + sync methods (bypass #3) ---
                Assert.That(viewModelSource, Does.Contain("private readonly ConstructionModel _construction;"),
                    "ConstructionViewModel currently owns a mutable Construction model instance directly.");
                Assert.That(viewModelSource, Does.Contain("private void SyncFromModel()"));
                Assert.That(viewModelSource, Does.Contain("private void SyncToModel()"));
                Assert.That(viewModelSource, Does.Contain("private void CopyConstructionData(ConstructionModel source)"));

                // --- ConstructionViewModel currently owns canonical scalar/collection backing fields (bypass #4) ---
                Assert.That(viewModelSource, Does.Contain("private double _groundwaterLevel = 2.0;"));
                Assert.That(viewModelSource, Does.Contain("private bool _hasLoads;"));
                Assert.That(viewModelSource, Does.Contain("private ObservableCollection<Layer> _layersAbovePipe = new();"));
                Assert.That(viewModelSource, Does.Contain("private ObservableCollection<Layer> _layersBelowPipe = new();"));

                // --- Construction model still mutates its own collections directly (bypass #5) ---
                Assert.That(constructionModelSource, Does.Contain("public Layer AddLayerAbovePipe(Material material, double thickness)"));
                Assert.That(constructionModelSource, Does.Contain("public Layer AddLayerBelowPipe(Material material, double thickness)"));
                Assert.That(constructionModelSource, Does.Contain("public void RemoveLayer(Layer layer)"));
                Assert.That(constructionModelSource, Does.Contain("public void ReindexLayers()"));

                // --- ProjectLoadOrchestrator currently writes Construction values directly to the ViewModel (bypass #6) ---
                Assert.That(GetDirectConstructionViewModelWrites(orchestratorSource),
                    Is.Empty,
                    "ProjectLoadOrchestrator must apply canonical lifecycle snapshots instead of direct scalar writes.");
                // Phase 9 re-pin: the dead legacy layer loader was removed from the
                // orchestrator (slice 5); direct VM collection writes no longer exist.
                Assert.That(orchestratorSource, Does.Not.Contain("_constructionViewModel.LayersAbovePipe.Clear();"));
                Assert.That(orchestratorSource, Does.Not.Contain("_constructionViewModel.LayersBelowPipe.Clear();"));
                Assert.That(orchestratorSource, Does.Not.Contain("_constructionViewModel.LayersAbovePipe.Add(layer);"));
                Assert.That(orchestratorSource, Does.Not.Contain("_constructionViewModel.LayersBelowPipe.Add(layer);"));
                Assert.That(orchestratorSource, Does.Not.Contain("_constructionViewModel.Reset();"));
                Assert.That(mainViewModelSource, Does.Not.Contain("_constructionViewModel.Reset();"));
                Assert.That(orchestratorSource, Does.Not.Contain("BuildResetConstructionSnapshot"));
                Assert.That(orchestratorSource, Does.Not.Contain("AddDefaultLayer"));
                Assert.That(orchestratorSource, Does.Contain("_constructionDefaultStateInitializer.Apply("));
                Assert.That(mainViewModelSource, Does.Contain("_constructionDefaultStateInitializer.Apply("));
                Assert.That(CountOccurrences(initializerSource, "CreateLayer("), Is.EqualTo(8),
                    "Only the canonical initializer may own the seven-layer recipe and its helper declaration.");

                // --- ResultsViewModel must remain a read/save-only site: no direct Construction writes today ---
                Assert.That(GetDirectConstructionViewModelWrites(resultsSource), Is.Empty,
                    "ResultsViewModel is a Construction save/read site and must not gain direct ConstructionViewModel setters.");

                // --- CalculationContext currently exposes the narrow downstream publication seam ---
                Assert.That(calculationContextSource, Does.Contain("public void UpdateConstruction(IConstructionData construction, string source = \"Construction\")"));
                Assert.That(calculationContextSource, Does.Contain("Construction = construction;"));

                // --- Post-Task-6: ConstructionState registered in DI as forwarding alias to ProjectSession.ConstructionState ---
                Assert.That(serviceRegistrationSource, Does.Contain("IProjectSessionConstructionState"),
                    "IProjectSessionConstructionState must be registered in DI after Task 6 so ConstructionViewModel can receive it.");
                Assert.That(projectSessionSource, Does.Contain("ConstructionState"),
                    "ProjectSession must own and expose IProjectSessionConstructionState after Task 4.");
            });
        }

        [Test]
        public void ConstructionStateLegacyStoreGuard_CapturesExactCurrentSubscriptionInventory()
        {
            var viewModelSource = ReadSource("src/ViewModels/Construction/ConstructionViewModel.cs");
            var constructionModelSource = ReadSource("src/Models/Construction/Construction.cs");

            Assert.Multiple(() =>
            {
                // --- Constructor-time subscriptions (attach sites) ---
                Assert.That(viewModelSource, Does.Contain("LayersAbovePipe.CollectionChanged += OnLayersCollectionChanged;"));
                Assert.That(viewModelSource, Does.Contain("LayersBelowPipe.CollectionChanged += OnLayersCollectionChanged;"));
                Assert.That(viewModelSource, Does.Contain("_construction.DataChanged += OnConstructionDataChanged;"));
                Assert.That(viewModelSource, Does.Contain("_calculationStateService.PipeSpacingChanged += OnPipeSpacingChanged;"));

                // --- Per-item layer subscription attach/detach inside the collection handler ---
                Assert.That(viewModelSource, Does.Contain("currentLayer.PropertyChanged += OnSubscribedLayerPropertyChanged;"),
                    "New layers must attach the canonical adapter property handler.");
                Assert.That(viewModelSource, Does.Contain("staleLayer.PropertyChanged -= OnSubscribedLayerPropertyChanged;"),
                    "Removed layers must detach the canonical adapter property handler.");

                // --- Construction model's own collection subscriptions (constructor) ---
                Assert.That(constructionModelSource, Does.Contain("LayersAbovePipe.CollectionChanged += (s, e) => OnDataChanged();"));
                Assert.That(constructionModelSource, Does.Contain("Layers.CollectionChanged += (s, e) => OnDataChanged();"));

                // --- Exactly one attach site and one detach site for the per-item PropertyChanged subscription ---
                Assert.That(CountOccurrences(viewModelSource, "currentLayer.PropertyChanged += OnSubscribedLayerPropertyChanged;"), Is.EqualTo(1),
                    "Exactly one current attach site for per-layer PropertyChanged is expected.");
                Assert.That(CountOccurrences(viewModelSource, "staleLayer.PropertyChanged -= OnSubscribedLayerPropertyChanged;"), Is.EqualTo(1),
                    "Exactly one current detach site for per-layer PropertyChanged is expected.");
            });
        }

        [Test]
        public void ConstructionStateLegacyStoreGuard_RejectsNewDirectConstructionViewModelSetterInForbiddenCallers()
        {
            const string resultsFixture = "_constructionViewModel.GroundwaterLevel = 0.5;";
            const string newBypassFixture = "_constructionViewModel.HasLoads = true;";

            Assert.Multiple(() =>
            {
                Assert.That(GetDirectConstructionViewModelWrites(resultsFixture), Is.EqualTo(new[] { "GroundwaterLevel" }),
                    "A new direct ResultsViewModel write to ConstructionViewModel must be detected by the writer guard.");
                Assert.That(GetDirectConstructionViewModelWrites(newBypassFixture), Is.EqualTo(new[] { "HasLoads" }),
                    "A new direct write must be detected even when it matches an existing allowed property name in a new caller.");
            });
        }

        [Test]
        public void ConstructionStateLegacyStoreGuard_RejectsMissingLayerPropertyChangedUnsubscribe()
        {
            const string correctDetachSource =
                "if (e.OldItems != null) { foreach (Layer layer in e.OldItems) { layer.PropertyChanged -= OnLayerPropertyChanged; } }";
            const string missingDetachSource =
                "if (e.OldItems != null) { foreach (Layer layer in e.OldItems) { /* detach removed */ } }";

            Assert.Multiple(() =>
            {
                Assert.That(HasLayerPropertyChangedDetach(correctDetachSource), Is.True,
                    "The current detach pattern must be recognized as present.");
                Assert.That(HasLayerPropertyChangedDetach(missingDetachSource), Is.False,
                    "A regression that removes the PropertyChanged detach call must be caught by this guard, " +
                    "because it would leak a handler per removed layer across repeated add/remove cycles.");
            });
        }

        [Test]
        public void CanonicalDefaultLifecycleGuard_RejectsConsumerBypassesAndDuplicateRecipes()
        {
            var sourceRoot = Path.Combine(FindRepositoryRoot(), "src");
            var initializerPath = Path.Combine(sourceRoot, "Services", "Project", "ConstructionDefaultStateInitializer.cs");
            var initializerSource = File.ReadAllText(initializerPath);
            var resultsSource = ReadSource("src/ViewModels/Results/ResultsViewModel.cs");
            var viewModelSource = ReadSource("src/ViewModels/Construction/ConstructionViewModel.cs");
            var orchestratorSource = ReadSource("src/Services/Project/ProjectLoadOrchestrator.cs");
            var mainViewModelSource = ReadSource("src/ViewModels/Shell/MainViewModel.cs");
            var recipeOwners = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => ContainsCanonicalDefaultRecipe(File.ReadAllText(path)))
                .Select(Path.GetFullPath)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(resultsSource, Does.Not.Contain("SyncToCanonicalState("),
                    "Project save must consume canonical state without save-time synchronization.");
                Assert.That(GetMarkDirtyCallCount(viewModelSource), Is.Zero);
                Assert.That(viewModelSource, Does.Not.Contain("_calculationContext.UpdateConstruction("));
                Assert.That(orchestratorSource, Does.Not.Contain("_constructionViewModel.Reset();"));
                Assert.That(mainViewModelSource, Does.Not.Contain("_constructionViewModel.Reset();"));
                Assert.That(recipeOwners, Is.EqualTo(new[] { Path.GetFullPath(initializerPath) }),
                    "ConstructionDefaultStateInitializer must remain the only production owner of the seven-layer recipe.");
                Assert.That(CountOccurrences(initializerSource, "CreateLayer("), Is.EqualTo(8));
            });
        }

        private static bool HasLayerPropertyChangedDetach(string source)
        {
            return source.Contains("layer.PropertyChanged -= OnLayerPropertyChanged;", StringComparison.Ordinal);
        }

        private static int CountOccurrences(string source, string needle)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += needle.Length;
            }

            return count;
        }

        private static int GetMarkDirtyCallCount(string source)
        {
            return Regex.Matches(source, @"_markDirtyService\.MarkDirty\(\);").Count;
        }

        private static string ReadSource(string relativePath)
        {
            var directory = FindRepositoryRoot();
            return File.ReadAllText(Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string FindRepositoryRoot()
        {
            var directory = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "SnowMeltingCalculator.sln")))
            {
                directory = Directory.GetParent(directory)?.FullName ?? string.Empty;
            }

            Assert.That(directory, Is.Not.Empty, "Could not locate the repository root from the test output directory.");
            return directory;
        }

        private static bool ContainsCanonicalDefaultRecipe(string source)
        {
            var recipeMarkers = new[]
            {
                "100.0", "10.0", "80.0", "200.0", "1000.0", "570.0",
                "LayerPosition.AbovePipe", "LayerPosition.BelowPipe"
            };
            return recipeMarkers.All(marker => source.Contains(marker, StringComparison.Ordinal));
        }

        private static string[] GetDirectConstructionViewModelWrites(string source)
        {
            return Regex.Matches(source, @"\b_constructionViewModel\.(?<property>[A-Za-z_][A-Za-z0-9_]*)\s*=(?!=)")
                .Select(match => match.Groups["property"].Value)
                .ToArray();
        }
    }
}
