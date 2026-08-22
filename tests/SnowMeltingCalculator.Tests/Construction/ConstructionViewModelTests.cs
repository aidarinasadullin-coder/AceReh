using NUnit.Framework;
using Moq;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Construction;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Тесты для ConstructionViewModel
    /// </summary>
    [TestFixture]
    public class ConstructionViewModelTests
    {
        private ConstructionViewModel _viewModel = null!;
        private MockConstructionService _mockService = null!;
        private MockMaterialRepository _mockMaterialRepository = null!;
        private MockConstructionRepository _mockConstructionRepository = null!;
        private Mock<ICalculationStateService> _mockCalculationStateService = null!;
        private Mock<IMarkDirtyService> _markDirtyServiceMock = null!;
        private Mock<IConstructionTemplateRepository> _mockTemplateRepository = null!;
        private Mock<IDialogService> _mockDialogService = null!;
        private Mock<IEditorDialogService> _mockEditorDialogService = null!;
        private ProjectSessionConstructionState _constructionState = null!;
        private CalculationContext _calculationContext = null!;

        [SetUp]
        public void Setup()
        {
            _mockService = new MockConstructionService();
            _mockMaterialRepository = new MockMaterialRepository();
            _mockConstructionRepository = new MockConstructionRepository();
            _mockCalculationStateService = new Mock<ICalculationStateService>();
            _markDirtyServiceMock = new Mock<IMarkDirtyService>();
            _mockTemplateRepository = new Mock<IConstructionTemplateRepository>();
            _mockDialogService = new Mock<IDialogService>();
            _mockEditorDialogService = new Mock<IEditorDialogService>();
            _calculationContext = new CalculationContext();
            _constructionState = new ProjectSessionConstructionState(
                _markDirtyServiceMock.Object,
                _calculationContext);
            _mockCalculationStateService.SetupGet(s => s.PipeSpacing).Returns(200);
            _mockTemplateRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());
            var construction = new ConstructionModel();
            _viewModel = new ConstructionViewModel(
                _mockService,
                _mockMaterialRepository,
                _mockConstructionRepository,
                _mockCalculationStateService.Object,
                _calculationContext,
                new ConstructionValidator(),
                construction,
                _markDirtyServiceMock.Object,
                _mockTemplateRepository.Object,
                _mockDialogService.Object,
                _mockEditorDialogService.Object,
                _constructionState,
                new ConstructionDefaultStateInitializer(_mockMaterialRepository, _constructionState));
        }

        #region Initialize Tests

        [Test]
        public async Task Initialize_LoadsMaterials()
        {
            // Act
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.AvailableMaterials.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task Initialize_LoadsTemplates()
        {
            // Act
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.Templates.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task Initialize_DoesNotRequireMainWindow()
        {
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot.LayersAbovePipe, Has.Count.EqualTo(1));
                Assert.That(_constructionState.Snapshot.LayersBelowPipe, Has.Count.EqualTo(6));
                Assert.That(_viewModel.LayersAbovePipe.Select(layer => layer.Id),
                    Is.EqualTo(_constructionState.Snapshot.LayersAbovePipe.Select(layer => layer.Id)));
                Assert.That(_viewModel.LayersBelowPipe.Select(layer => layer.Id),
                    Is.EqualTo(_constructionState.Snapshot.LayersBelowPipe.Select(layer => layer.Id)));
            });
        }

        [Test]
        public void AppOnStartup_PreservesMaterialLoadAwaitedViewModelInitializeThenWindowShow()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            string? appSourcePath = null;
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "src", "App.xaml.cs");
                if (File.Exists(candidate))
                {
                    appSourcePath = candidate;
                    break;
                }

                directory = directory.Parent;
            }

            Assert.That(appSourcePath, Is.Not.Null, "Repository App.xaml.cs was not found from the test directory.");
            var source = File.ReadAllText(appSourcePath!);
            var materialLoad = source.IndexOf("await materialRepository.LoadMaterialsAsync();", StringComparison.Ordinal);
            var viewModelInitialize = source.IndexOf("await constructionViewModel.InitializeCommand.ExecuteAsync(null);", StringComparison.Ordinal);
            var windowResolution = source.IndexOf("GetRequiredService<MainWindow>()", StringComparison.Ordinal);
            var windowShow = source.IndexOf("mainWindow.Show();", StringComparison.Ordinal);
            var startupCatch = source.IndexOf("catch (Exception ex)", StringComparison.Ordinal);
            var shutdown = source.IndexOf("Shutdown();", startupCatch, StringComparison.Ordinal);

            Assert.Multiple(() =>
            {
                Assert.That(materialLoad, Is.GreaterThanOrEqualTo(0));
                Assert.That(viewModelInitialize, Is.GreaterThan(materialLoad));
                Assert.That(windowResolution, Is.GreaterThan(viewModelInitialize));
                Assert.That(windowShow, Is.GreaterThan(windowResolution));
                Assert.That(startupCatch, Is.GreaterThan(windowShow));
                Assert.That(shutdown, Is.GreaterThan(startupCatch));
            });
        }

        [Test]
        public async Task InitializeFailure_PreservesCanonicalStateAndAdapter_ForStartupExceptionPath()
        {
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var stateBefore = _constructionState.Snapshot;
            var adapterAboveBefore = _viewModel.LayersAbovePipe.Select(ToLifecycleTuple).ToArray();
            var adapterBelowBefore = _viewModel.LayersBelowPipe.Select(ToLifecycleTuple).ToArray();
            var changedEvents = 0;
            _constructionState.Changed += (_, _) => changedEvents++;
            _mockMaterialRepository.MissingLookupMaterialIds.Add(10);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _viewModel.InitializeCommand.ExecuteAsync(null));

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot, Is.EqualTo(stateBefore));
                Assert.That(_viewModel.LayersAbovePipe.Select(ToLifecycleTuple), Is.EqualTo(adapterAboveBefore));
                Assert.That(_viewModel.LayersBelowPipe.Select(ToLifecycleTuple), Is.EqualTo(adapterBelowBefore));
                Assert.That(_viewModel.IsLoading, Is.False);
                Assert.That(changedEvents, Is.Zero);
            });
        }

        [Test]
        public async Task RepeatedInitializationAndReset_DoNotMultiplySubscriptionsOrDownstreamPublication()
        {
            var origins = new List<ConstructionMutationOrigin>();
            var constructionPublications = 0;
            _constructionState.Changed += (_, args) => origins.Add(args.Origin);
            _calculationContext.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Construction))
                {
                    constructionPublications++;
                }
            };

            await _viewModel.InitializeCommand.ExecuteAsync(null);
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.Reset();
            _viewModel.ResetToDefaultCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(origins, Is.EqualTo(new[]
                {
                    ConstructionMutationOrigin.Initialization,
                    ConstructionMutationOrigin.Initialization,
                    ConstructionMutationOrigin.Reset,
                    ConstructionMutationOrigin.Reset
                }));
                Assert.That(constructionPublications, Is.Zero);
                Assert.That(DirtyCalls, Is.Zero);
                Assert.That(_viewModel.HasUnsavedChanges, Is.False);
            });

            var layer = _viewModel.LayersAbovePipe.Single();
            layer.Thickness += 1.0;

            Assert.Multiple(() =>
            {
                Assert.That(origins.Last(), Is.EqualTo(ConstructionMutationOrigin.User));
                Assert.That(origins.Count, Is.EqualTo(5));
                Assert.That(constructionPublications, Is.EqualTo(1));
                Assert.That(DirtyCalls, Is.EqualTo(1));
            });
        }

        private static object ToLifecycleTuple(Layer layer) =>
            (layer.Id, layer.Position, layer.Order, layer.Material.Id, layer.Thickness, layer.CalculatedLambda, layer.IsLambdaOverridden);

        #endregion

        #region AddLayer Tests

        [Test]
        public async Task AddLayerAbovePipe_AddsLayerToCollection()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var initialCount = _viewModel.LayersAbovePipe.Count;

            // Act
            _viewModel.AddLayerAbovePipeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.LayersAbovePipe.Count, Is.EqualTo(initialCount + 1));
        }

        [Test]
        public async Task AddLayerAbovePipe_SetsCorrectPosition()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            // Act
            _viewModel.AddLayerAbovePipeCommand.Execute(null);

            // Assert
            var addedLayer = _viewModel.LayersAbovePipe.Last();
            Assert.That(addedLayer.Position, Is.EqualTo(LayerPosition.AbovePipe));
        }

        [Test]
        public async Task AddLayerBelowPipe_AddsLayerToCollection()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var initialCount = _viewModel.LayersBelowPipe.Count;

            // Act
            _viewModel.AddLayerBelowPipeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.LayersBelowPipe.Count, Is.EqualTo(initialCount + 1));
        }

        [Test]
        public async Task AddLayerBelowPipe_SetsCorrectPosition()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            // Act
            _viewModel.AddLayerBelowPipeCommand.Execute(null);

            // Assert
            var addedLayer = _viewModel.LayersBelowPipe.Last();
            Assert.That(addedLayer.Position, Is.EqualTo(LayerPosition.BelowPipe));
        }

        [Test]
        public async Task AddLayerAbovePipe_InsertsAtSurface()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();
            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5);
            _viewModel.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 100,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });

            // Act
            _viewModel.AddLayerAbovePipeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.LayersAbovePipe.Count, Is.EqualTo(2));
            Assert.That(_viewModel.LayersAbovePipe[0].Order, Is.EqualTo(0));
            Assert.That(_viewModel.LayersAbovePipe[1].Order, Is.EqualTo(1));
            Assert.That(_viewModel.LayersAbovePipe[1].Material?.Id, Is.EqualTo(concrete.Id));
        }

        [Test]
        public async Task LambdaE_StaysConcreteAfterSurfaceAdd()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();
            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5);
            var asphalt = _viewModel.AvailableMaterials.First(m => m.Id == 11); // Асфальт
            _viewModel.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 100,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });
            _viewModel.LayersAbovePipe.Insert(0, new Layer
            {
                Material = asphalt,
                Thickness = 50,
                CalculatedLambda = asphalt.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });

            // Act
            _viewModel.UpdateCalculations();

            // Assert
            Assert.That(_viewModel.LambdaE, Is.EqualTo(concrete.LambdaA));
        }

        #endregion

        #region RemoveLayer Tests

        [Test]
        public async Task RemoveLayer_RemovesLayerFromCollection()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.AddLayerAbovePipeCommand.Execute(null);
            var layerToRemove = _viewModel.LayersAbovePipe.First();

            // Act
            _viewModel.RemoveLayerCommand.Execute(layerToRemove);

            // Assert
            Assert.That(_viewModel.LayersAbovePipe, Does.Not.Contain(layerToRemove));
        }

        [Test]
        public async Task RemoveLayer_NullLayer_DoesNothing()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var initialCount = _viewModel.LayersAbovePipe.Count;

            // Act
            _viewModel.RemoveLayerCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.LayersAbovePipe.Count, Is.EqualTo(initialCount));
        }

        #endregion

        #region GroundwaterLevel Tests

        [Test]
        public async Task SetGroundwaterLevelBelow1Meter_UpdatesLambdaForBelowPipeLayers()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.AddLayerBelowPipeCommand.Execute(null);
            var layer = _viewModel.LayersBelowPipe.First();
            var material = layer.Material;
            var expectedLambda = material.LambdaB; // При УГВ < 1м используется λБ

            // Act
            _viewModel.GroundwaterLevel = 0.5;

            // Assert
            Assert.That(layer.CalculatedLambda, Is.EqualTo(expectedLambda));
        }

        [Test]
        public async Task SetGroundwaterLevelAbove1Meter_UsesLambdaAForBelowPipeLayers()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.AddLayerBelowPipeCommand.Execute(null);
            var layer = _viewModel.LayersBelowPipe.First();
            var material = layer.Material;
            var expectedLambda = material.LambdaA; // При УГВ >= 1м используется λА

            // Act
            _viewModel.GroundwaterLevel = 2.0;

            // Assert
            Assert.That(layer.CalculatedLambda, Is.EqualTo(expectedLambda));
        }

        [Test]
        public async Task ChangeMaterial_AfterProjectLoad_RecalculatesLambda()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var sand = _viewModel.AvailableMaterials.First(m => m.Name == "Песок");
            var reinforcedConcrete = _viewModel.AvailableMaterials.First(m => m.Name == "Бетон с арматурной сеткой");

            var layer = new Layer
            {
                Material = sand,
                Thickness = 100,
                CalculatedLambda = sand.LambdaA,
                IsLambdaOverridden = true, // как после загрузки проекта
                Position = LayerPosition.AbovePipe,
                Order = 0
            };
            _viewModel.LayersAbovePipe.Add(layer);

            // Act
            layer.Material = reinforcedConcrete;

            // Assert
            Assert.That(layer.CalculatedLambda, Is.EqualTo(reinforcedConcrete.LambdaA));
            Assert.That(layer.IsLambdaOverridden, Is.False);
        }

        #endregion

        #region Calculations Tests

        [Test]
        public async Task UpdateCalculations_CalculatesR1Correctly()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();

            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5); // Бетон плотный
            _viewModel.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 100,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });

            // Act
            _viewModel.UpdateCalculations();

            // Assert
            // R = d / λ / 1000 = 100 / 1.74 / 1000 = 0.05747 м²·К/Вт
            var expectedR1 = 100.0 / 1.74 / 1000.0;
            Assert.That(_viewModel.R1Total, Is.EqualTo(expectedR1).Within(0.0001));
        }

        [Test]
        public async Task UpdateCalculations_CalculatesR2Correctly()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersBelowPipe.Clear();

            var sand = _viewModel.AvailableMaterials.First(m => m.Id == 1); // Песок
            _viewModel.LayersBelowPipe.Add(new Layer
            {
                Material = sand,
                Thickness = 150,
                CalculatedLambda = sand.LambdaA,
                Position = LayerPosition.BelowPipe,
                Order = 0
            });

            // Act
            _viewModel.UpdateCalculations();

            // Assert
            // R = d / λ / 1000 = 150 / 0.4 / 1000 = 0.375 м²·К/Вт
            var expectedR2 = 150.0 / 0.4 / 1000.0;
            Assert.That(_viewModel.R2Total, Is.EqualTo(expectedR2).Within(0.0001));
        }

        [Test]
        public async Task UpdateCalculations_CalculatesLambdaEFromLastLayerAbovePipe()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();

            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5); // Бетон плотный
            var asphalt = _viewModel.AvailableMaterials.First(m => m.Id == 11); // Асфальт

            // LayersAbovePipe stores layers in physical top-to-bottom order.
            // Direct .Add appends toward the pipe; LastOrDefault selects the nearest-pipe layer.
            _viewModel.LayersAbovePipe.Add(new Layer
            {
                Material = asphalt,
                Thickness = 50,
                CalculatedLambda = asphalt.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });
            _viewModel.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 100,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 1
            });

            // Act
            _viewModel.UpdateCalculations();

            // Assert
            Assert.That(_viewModel.LambdaE, Is.EqualTo(concrete.LambdaA));
        }

        [Test]
        public async Task UpdateCalculations_NoLayersAbovePipe_ReturnsDefaultLambdaE()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();

            // Act
            _viewModel.UpdateCalculations();

            // Assert
            Assert.That(_viewModel.LambdaE, Is.EqualTo(1.6)); // Значение по умолчанию
        }

        #endregion

        #region Validation Tests

        [Test]
        public async Task Validate_NoLayers_ReturnsError()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();
            _viewModel.LayersBelowPipe.Clear();

            // Act
            _viewModel.Validate();

            // Assert
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("хотя бы один слой"));
        }

        [Test]
        public async Task Validate_ThinLayersAbovePipe_ReturnsError()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();
            _viewModel.HasLoads = true;

            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5);
            _viewModel.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 30, // Меньше минимума (50 мм при нагрузках)
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });

            // Act
            _viewModel.Validate();

            // Assert
            Assert.That(_viewModel.IsValid, Is.False);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Минимальная толщина"));
        }

        [Test]
        public async Task Validate_ValidConstruction_ReturnsTrue()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.ResetToDefaultCommand.Execute(null);

            // Act
            _viewModel.Validate();

            // Assert
            Assert.That(_viewModel.IsValid, Is.True);
        }

        [Test]
        public async Task Validate_ConcreteLayer_DoesNotShowSupplyTemperatureInfo()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();
            _viewModel.LayersBelowPipe.Clear();

            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5);
            _viewModel.LayersAbovePipe.Add(new Layer
            {
                Material = concrete,
                Thickness = 50,
                CalculatedLambda = concrete.LambdaA,
                Position = LayerPosition.AbovePipe,
                Order = 0
            });

            // Act
            _viewModel.Validate();

            // Assert
            Assert.That(_viewModel.IsValid, Is.True);
            Assert.That(_viewModel.ValidationMessage, Does.Not.Contain("максимальная температура подачи"));
            Assert.That(_viewModel.ValidationMessage, Does.Not.Contain("50°C"));
        }

        #endregion

        #region Template Tests

        [Test]
        public async Task ApplyTemplate_CreatesLayersFromTemplate()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var template = _viewModel.Templates.First(t => t.Id == 1); // Типовая парковка

            // Act
            _viewModel.SelectedTemplate = template;
            _viewModel.ApplyTemplateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.LayersAbovePipe.Count, Is.EqualTo(template.LayersAbovePipe.Count));
            Assert.That(_viewModel.LayersBelowPipe.Count, Is.EqualTo(template.LayersBelowPipe.Count));
        }

        [Test]
        public async Task ApplyTemplate_DoesNotChangeGroundwaterLevel()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var template = _viewModel.Templates.First(t => t.Id == 3); // Пешеходная дорожка
            _viewModel.GroundwaterLevel = 0.5;
            template.DefaultGroundwaterLevel = 1.5;

            // Act
            _viewModel.SelectedTemplate = template;
            _viewModel.ApplyTemplateCommand.Execute(null);

            // Assert: УГВ — настройка проекта, шаблон её не меняет
            Assert.That(_viewModel.GroundwaterLevel, Is.EqualTo(0.5));
        }

        [Test]
        public async Task ApplyTemplate_SetsHasLoads()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var template = _viewModel.Templates.First(t => t.Id == 1); // Типовая парковка
            template.HasLoads = true;

            // Act
            _viewModel.SelectedTemplate = template;
            _viewModel.ApplyTemplateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.HasLoads, Is.True);
        }

        [Test]
        public async Task ApplyTemplate_Success_EmitsExactlyOneCanonicalTemplateCompletion()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            var origins = new System.Collections.Generic.List<ConstructionMutationOrigin>();
            _constructionState.Changed += (_, args) => origins.Add(args.Origin);
            _viewModel.SelectedTemplate = _viewModel.Templates.First(t => t.Id == 1);

            // Act
            await _viewModel.ApplyTemplateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(origins, Is.EqualTo(new[] { ConstructionMutationOrigin.Template }));
        }

        #endregion

        #region Reset Tests

        [Test]
        public async Task ResetToDefault_ClearsLayersAndSetsDefaults()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.AddLayerAbovePipeCommand.Execute(null);
            _viewModel.AddLayerBelowPipeCommand.Execute(null);
            _viewModel.GroundwaterLevel = 0.5;
            _viewModel.HasLoads = true;

            // Act
            _viewModel.ResetToDefaultCommand.Execute(null);

            // Assert: сброс не меняет УГВ — это настройка проекта
            Assert.That(_viewModel.GroundwaterLevel, Is.EqualTo(0.5));
            Assert.That(_viewModel.HasLoads, Is.False);
        }

        #endregion

        #region HasUnsavedChanges Tests

        [Test]
        public async Task AddLayer_SetsHasUnsavedChanges()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.HasUnsavedChanges = false;

            // Act
            _viewModel.AddLayerAbovePipeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.HasUnsavedChanges, Is.True);
        }

        [Test]
        public async Task RemoveLayer_SetsHasUnsavedChanges()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.AddLayerAbovePipeCommand.Execute(null);
            _viewModel.HasUnsavedChanges = false;
            var layer = _viewModel.LayersAbovePipe.First();

            // Act
            _viewModel.RemoveLayerCommand.Execute(layer);

            // Assert
            Assert.That(_viewModel.HasUnsavedChanges, Is.True);
        }

        [Test]
        public async Task ChangeGroundwaterLevel_SetsHasUnsavedChanges()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.HasUnsavedChanges = false;

            // Act
            _viewModel.GroundwaterLevel = 0.5;

            // Assert
            Assert.That(_viewModel.HasUnsavedChanges, Is.True);
        }

        [Test]
        public async Task ResetToDefault_ClearsHasUnsavedChanges()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.HasUnsavedChanges = true;

            // Act
            _viewModel.ResetToDefaultCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.HasUnsavedChanges, Is.False);
        }

        [Test]
        public async Task Reset_AfterAddLayer_ClearsHasUnsavedChanges()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.AddLayerAbovePipeCommand.Execute(null);
            Assert.That(_viewModel.HasUnsavedChanges, Is.True);

            // Act
            _viewModel.Reset();

            // Assert
            Assert.That(_viewModel.HasUnsavedChanges, Is.False);
        }

        [Test]
        public async Task AddLayerAbovePipeCommand_SetsHasUnsavedChanges()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.HasUnsavedChanges = false;

            // Act
            _viewModel.AddLayerAbovePipeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.HasUnsavedChanges, Is.True);
        }

        #endregion

        #region TotalThickness Tests

        [Test]
        public async Task TotalThicknessAbovePipe_ReturnsCorrectSum()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersAbovePipe.Clear();

            var concrete = _viewModel.AvailableMaterials.First(m => m.Id == 5);
            _viewModel.LayersAbovePipe.Add(new Layer { Material = concrete, Thickness = 100, Position = LayerPosition.AbovePipe });
            _viewModel.LayersAbovePipe.Add(new Layer { Material = concrete, Thickness = 50, Position = LayerPosition.AbovePipe });

            // Act & Assert
            Assert.That(_viewModel.TotalThicknessAbovePipe, Is.EqualTo(150));
        }

        [Test]
        public async Task TotalThicknessBelowPipe_ReturnsCorrectSum()
        {
            // Arrange
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.LayersBelowPipe.Clear();

            var sand = _viewModel.AvailableMaterials.First(m => m.Id == 1);
            _viewModel.LayersBelowPipe.Add(new Layer { Material = sand, Thickness = 150, Position = LayerPosition.BelowPipe });
            _viewModel.LayersBelowPipe.Add(new Layer { Material = sand, Thickness = 200, Position = LayerPosition.BelowPipe });

            // Act & Assert
            Assert.That(_viewModel.TotalThicknessBelowPipe, Is.EqualTo(350));
        }

        #endregion

        #region Task 9 Recovery Characterization Tests

        [Test]
        public async Task DirectCollectionClear_ShadowWritesCanonicalSnapshot_AndDetachesRemovedLayers()
        {
            await InitializeRecoveryFixture();
            var removedLayers = _viewModel.LayersAbovePipe.ToArray();
            var expectedBelow = _constructionState.Snapshot.LayersBelowPipe.ToArray();
            var expectedGroundwaterLevel = _constructionState.Snapshot.GroundwaterLevel;
            var expectedHasLoads = _constructionState.Snapshot.HasLoads;
            var stateChanged = 0;
            var dataChanged = 0;
            _constructionState.Changed += (_, _) => stateChanged++;
            _viewModel.DataChanged += (_, _) => dataChanged++;
            _markDirtyServiceMock.Invocations.Clear();

            _viewModel.LayersAbovePipe.Clear();

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot.LayersAbovePipe, Is.Empty);
                Assert.That(_constructionState.Snapshot.LayersBelowPipe, Is.EqualTo(expectedBelow));
                Assert.That(_constructionState.Snapshot.GroundwaterLevel, Is.EqualTo(expectedGroundwaterLevel));
                Assert.That(_constructionState.Snapshot.HasLoads, Is.EqualTo(expectedHasLoads));
            });

            var stateChangedAfterClear = stateChanged;
            var dataChangedAfterClear = dataChanged;
            var dirtyAfterClear = DirtyCalls;
            removedLayers[0].Thickness += 7;

            Assert.Multiple(() =>
            {
                Assert.That(stateChanged, Is.EqualTo(stateChangedAfterClear));
                Assert.That(dataChanged, Is.EqualTo(dataChangedAfterClear));
                Assert.That(DirtyCalls, Is.EqualTo(dirtyAfterClear));
            });
        }

        [Test]
        public async Task DirectCollectionAdd_ShadowWritesCompleteLayer_AndSubscribesExactlyOnce()
        {
            await InitializeRecoveryFixture();
            var material = _viewModel.AvailableMaterials.First(item => item.Id == 11);
            var layer = CreateRecoveryLayer(material, 73, 2.345, true, LayerPosition.BelowPipe, 99);
            var stateChanged = 0;
            _constructionState.Changed += (_, _) => stateChanged++;

            _viewModel.LayersAbovePipe.Add(layer);

            var snapshot = _constructionState.Snapshot.LayersAbovePipe.Last();
            AssertRecoveryLayer(snapshot, layer, LayerPosition.AbovePipe, _viewModel.LayersAbovePipe.Count - 1);

            var stateChangedAfterAdd = stateChanged;
            layer.Thickness = 81;

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot.LayersAbovePipe.Last().Thickness, Is.EqualTo(81));
                Assert.That(stateChanged, Is.EqualTo(stateChangedAfterAdd + 1));
            });
        }

        [Test]
        public async Task DirectCollectionRemove_ShadowWritesCanonicalSnapshot_AndDetachesLayer()
        {
            await InitializeRecoveryFixture();
            var removed = _viewModel.LayersBelowPipe[1];
            var remainingIds = _viewModel.LayersBelowPipe.Where(layer => !ReferenceEquals(layer, removed)).Select(layer => layer.Id).ToArray();
            var stateChanged = 0;
            var dataChanged = 0;
            _constructionState.Changed += (_, _) => stateChanged++;
            _viewModel.DataChanged += (_, _) => dataChanged++;
            _markDirtyServiceMock.Invocations.Clear();

            _viewModel.LayersBelowPipe.Remove(removed);

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot.LayersBelowPipe.Select(layer => layer.Id), Is.EqualTo(remainingIds));
                Assert.That(_constructionState.Snapshot.LayersBelowPipe.Select(layer => layer.Order), Is.EqualTo(Enumerable.Range(0, remainingIds.Length)));
            });

            var stateChangedAfterRemove = stateChanged;
            var dataChangedAfterRemove = dataChanged;
            var dirtyAfterRemove = DirtyCalls;
            removed.Thickness += 9;

            Assert.Multiple(() =>
            {
                Assert.That(stateChanged, Is.EqualTo(stateChangedAfterRemove));
                Assert.That(dataChanged, Is.EqualTo(dataChangedAfterRemove));
                Assert.That(DirtyCalls, Is.EqualTo(dirtyAfterRemove));
            });
        }

        [Test]
        public async Task DirectCollectionMove_ShadowWritesOrder_AndPreservesSingleSubscription()
        {
            await InitializeRecoveryFixture();
            var moved = _viewModel.LayersBelowPipe[0];
            var stateChanged = 0;
            _constructionState.Changed += (_, _) => stateChanged++;

            _viewModel.LayersBelowPipe.Move(0, 2);

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot.LayersBelowPipe.Select(layer => layer.Id), Is.EqualTo(_viewModel.LayersBelowPipe.Select(layer => layer.Id)));
                Assert.That(_constructionState.Snapshot.LayersBelowPipe.Select(layer => layer.Order), Is.EqualTo(Enumerable.Range(0, _viewModel.LayersBelowPipe.Count)));
            });

            var stateChangedAfterMove = stateChanged;
            moved.Thickness += 13;

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot.LayersBelowPipe.Single(layer => layer.Id == moved.Id).Thickness, Is.EqualTo(moved.Thickness));
                Assert.That(stateChanged, Is.EqualTo(stateChangedAfterMove + 1));
            });
        }

        [Test]
        public async Task DirectThicknessChange_ShadowWritesCanonicalLayer()
        {
            await InitializeRecoveryFixture();
            var layer = _viewModel.LayersAbovePipe[0];
            var before = _constructionState.Snapshot;

            layer.Thickness = 147;

            var after = _constructionState.Snapshot;
            Assert.Multiple(() =>
            {
                Assert.That(after.LayersAbovePipe[0].Thickness, Is.EqualTo(147));
                Assert.That(after.LayersAbovePipe[0] with { Thickness = before.LayersAbovePipe[0].Thickness }, Is.EqualTo(before.LayersAbovePipe[0]));
                Assert.That(after.LayersBelowPipe, Is.EqualTo(before.LayersBelowPipe));
                Assert.That(after.GroundwaterLevel, Is.EqualTo(before.GroundwaterLevel));
                Assert.That(after.HasLoads, Is.EqualTo(before.HasLoads));
            });
        }

        [Test]
        public async Task DirectCalculatedLambdaChange_ShadowWritesExactCanonicalValue()
        {
            await InitializeRecoveryFixture();
            var layer = _viewModel.LayersBelowPipe[0];

            layer.CalculatedLambda = 0.43210987654321;

            Assert.That(
                _constructionState.Snapshot.LayersBelowPipe.Single(item => item.Id == layer.Id).CalculatedLambda,
                Is.EqualTo(0.43210987654321));
        }

        [Test]
        public async Task DirectMaterialChange_ShadowWritesFinalMaterialLambdaAndOverrideState()
        {
            await InitializeRecoveryFixture();
            _viewModel.GroundwaterLevel = 0.5;
            var layer = _viewModel.LayersBelowPipe[0];
            layer.IsLambdaOverridden = true;
            layer.CalculatedLambda = 9.9;
            var material = _viewModel.AvailableMaterials.First(item => item.Id == 1);

            layer.Material = material;

            var snapshot = _constructionState.Snapshot.LayersBelowPipe.Single(item => item.Id == layer.Id);
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.MaterialId, Is.EqualTo(material.Id));
                Assert.That(snapshot.MaterialName, Is.EqualTo(material.Name));
                Assert.That(snapshot.CalculatedLambda, Is.EqualTo(material.LambdaB));
                Assert.That(snapshot.IsLambdaOverridden, Is.False);
            });
        }

        [Test]
        public async Task DirectIsLambdaOverriddenChange_ShadowWritesCanonicalFlag()
        {
            await InitializeRecoveryFixture();
            var layer = _viewModel.LayersAbovePipe[0];
            var lambda = layer.CalculatedLambda;

            layer.IsLambdaOverridden = true;

            var snapshot = _constructionState.Snapshot.LayersAbovePipe.Single(item => item.Id == layer.Id);
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.IsLambdaOverridden, Is.True);
                Assert.That(snapshot.CalculatedLambda, Is.EqualTo(lambda));
            });
        }

        [Test]
        public async Task LifecycleGuards_SuppressCanonicalWriteBack_AndReconcileSubscriptions()
        {
            await InitializeRecoveryFixture();
            var staleLayers = _viewModel.LayersAbovePipe.Concat(_viewModel.LayersBelowPipe).ToArray();
            var canonical = new ConstructionStateSnapshot(
                0.75,
                true,
                new[] { new ConstructionLayerSnapshot(Guid.NewGuid(), 11, "canonical-above", 61, 1.23, true, LayerPosition.AbovePipe, 0) },
                new[] { new ConstructionLayerSnapshot(Guid.NewGuid(), 1, "canonical-below", 222, 0.4, false, LayerPosition.BelowPipe, 0) });
            _constructionState.ApplySnapshot(canonical, ConstructionMutationOrigin.ProjectLoad);
            var stateChanged = 0;
            var dataChanged = 0;
            _constructionState.Changed += (_, _) => stateChanged++;
            _viewModel.DataChanged += (_, _) => dataChanged++;
            _markDirtyServiceMock.Invocations.Clear();

            _viewModel.Reset();
            var firstResetLayers = _viewModel.LayersAbovePipe.Concat(_viewModel.LayersBelowPipe).ToArray();
            _viewModel.Reset();
            var current = _viewModel.LayersAbovePipe[0];

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot, Is.Not.EqualTo(canonical));
                Assert.That(_constructionState.Snapshot.LayersAbovePipe, Has.Count.EqualTo(1));
                Assert.That(_constructionState.Snapshot.LayersBelowPipe, Has.Count.EqualTo(6));
                Assert.That(_constructionState.Snapshot.HasLoads, Is.False);
            });

            var effectsBeforeStaleEdits = (stateChanged, dataChanged, DirtyCalls);
            staleLayers[0].Thickness += 3;
            firstResetLayers[0].Thickness += 5;
            Assert.That((stateChanged, dataChanged, DirtyCalls), Is.EqualTo(effectsBeforeStaleEdits));

            current.Thickness += 7;
            Assert.Multiple(() =>
            {
                Assert.That(stateChanged, Is.EqualTo(effectsBeforeStaleEdits.stateChanged + 1));
                Assert.That(dataChanged, Is.EqualTo(effectsBeforeStaleEdits.dataChanged));
                Assert.That(DirtyCalls, Is.EqualTo(effectsBeforeStaleEdits.DirtyCalls + 1));
            });
        }

        [Test]
        public async Task RejectedShadowWrite_PreservesLastValidCanonicalSnapshot()
        {
            await InitializeRecoveryFixture();
            var before = _constructionState.Snapshot;
            var duplicate = CreateRecoveryLayer(
                _viewModel.AvailableMaterials.First(item => item.Id == before.LayersAbovePipe[0].MaterialId),
                88,
                1.8,
                false,
                LayerPosition.BelowPipe,
                0);
            duplicate.Id = before.LayersAbovePipe[0].Id;
            var stateChanged = 0;
            _constructionState.Changed += (_, _) => stateChanged++;

            _viewModel.LayersBelowPipe.Add(duplicate);
            _viewModel.SyncToCanonicalState();

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot, Is.EqualTo(before));
                Assert.That(stateChanged, Is.Zero);
                Assert.That(_viewModel.LayersBelowPipe, Does.Contain(duplicate));
                _mockDialogService.VerifyNoOtherCalls();
            });
        }

        [Test]
        public async Task ExistingScalarShadowWrites_RemainGreen()
        {
            await InitializeRecoveryFixture();
            var aboveIds = _constructionState.Snapshot.LayersAbovePipe.Select(layer => layer.Id).ToArray();
            var belowIds = _constructionState.Snapshot.LayersBelowPipe.Select(layer => layer.Id).ToArray();
            var origins = new System.Collections.Generic.List<ConstructionMutationOrigin>();
            _constructionState.Changed += (_, args) => origins.Add(args.Origin);

            _viewModel.GroundwaterLevel = 0.5;
            _viewModel.HasLoads = true;

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot.GroundwaterLevel, Is.EqualTo(0.5));
                Assert.That(_constructionState.Snapshot.HasLoads, Is.True);
                Assert.That(_constructionState.Snapshot.LayersAbovePipe.Select(layer => layer.Id), Is.EqualTo(aboveIds));
                Assert.That(_constructionState.Snapshot.LayersBelowPipe.Select(layer => layer.Id), Is.EqualTo(belowIds));
                Assert.That(origins, Is.EqualTo(new[] { ConstructionMutationOrigin.User, ConstructionMutationOrigin.User }));
            });
        }

        private int DirtyCalls => _markDirtyServiceMock.Invocations.Count(invocation => invocation.Method.Name == nameof(IMarkDirtyService.MarkDirty));

        private async Task InitializeRecoveryFixture()
        {
            await _viewModel.InitializeCommand.ExecuteAsync(null);
            _viewModel.SyncToCanonicalState();
            _markDirtyServiceMock.Invocations.Clear();
        }

        private static Layer CreateRecoveryLayer(
            Material material,
            double thickness,
            double calculatedLambda,
            bool isLambdaOverridden,
            LayerPosition position,
            int order)
        {
            return new Layer
            {
                Material = material,
                Thickness = thickness,
                CalculatedLambda = calculatedLambda,
                IsLambdaOverridden = isLambdaOverridden,
                Position = position,
                Order = order
            };
        }

        private static void AssertRecoveryLayer(
            ConstructionLayerSnapshot snapshot,
            Layer layer,
            LayerPosition expectedPosition,
            int expectedOrder)
        {
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Id, Is.EqualTo(layer.Id));
                Assert.That(snapshot.MaterialId, Is.EqualTo(layer.Material.Id));
                Assert.That(snapshot.MaterialName, Is.EqualTo(layer.Material.Name));
                Assert.That(snapshot.Thickness, Is.EqualTo(layer.Thickness));
                Assert.That(snapshot.CalculatedLambda, Is.EqualTo(layer.CalculatedLambda));
                Assert.That(snapshot.IsLambdaOverridden, Is.EqualTo(layer.IsLambdaOverridden));
                Assert.That(snapshot.Position, Is.EqualTo(expectedPosition));
                Assert.That(snapshot.Order, Is.EqualTo(expectedOrder));
            });
        }

        #endregion
    }

    #region Mock Classes

    /// <summary>
    /// Мок-сервис для тестов ViewModel
    /// </summary>
    internal class MockConstructionService : IConstructionService
    {
        /// <summary>
        /// Optional override for <see cref="CreateFromTemplate"/> used by Phase 3
        /// characterization tests to force a deterministic candidate construction.
        /// When null, the default catalog-driven behavior below is used.
        /// </summary>
        public Func<ConstructionModel>? NextTemplateResult { get; set; }

        /// <summary>
        /// Optional exception thrown by <see cref="CreateFromTemplate"/> used by
        /// Phase 3 characterization tests to simulate a missing-material failure.
        /// When null, no exception is thrown.
        /// </summary>
        public MaterialNotFoundException? ThrowOnCreateFromTemplate { get; set; }

        public void CalculateThermalResistances(ConstructionModel construction)
        {
            foreach (var layer in construction.LayersAbovePipe)
            {
                layer.UpdateLambda(construction.GroundwaterLevel);
            }
            foreach (var layer in construction.Layers)
            {
                layer.UpdateLambda(construction.GroundwaterLevel);
            }
        }

        public double CalculateR1(System.Collections.Generic.IEnumerable<Layer> layersAbovePipe)
        {
            return layersAbovePipe.Sum(l => l.CalculatedR);
        }

        public double CalculateR2(System.Collections.Generic.IEnumerable<Layer> layersBelowPipe, double groundwaterLevel)
        {
            return layersBelowPipe.Sum(l => l.CalculatedR);
        }

        public ValidationResult ValidateConstruction(ConstructionModel construction)
        {
            var validator = new ConstructionValidator();
            return validator.Validate(construction);
        }

        public ConstructionModel CreateFromTemplate(ConstructionTemplate template, System.Collections.Generic.IEnumerable<Material> materials)
        {
            if (ThrowOnCreateFromTemplate != null)
            {
                throw ThrowOnCreateFromTemplate;
            }

            if (NextTemplateResult != null)
            {
                return NextTemplateResult();
            }

            var materialsList = materials.ToList();
            var construction = new ConstructionModel
            {
                GroundwaterLevel = template.DefaultGroundwaterLevel,
                HasLoads = template.HasLoads
            };

            foreach (var layerTemplate in template.LayersAbovePipe.OrderBy(l => l.Order))
            {
                var material = materialsList.FirstOrDefault(m => m.Id == layerTemplate.MaterialId);
                if (material != null)
                {
                    construction.AddLayerAbovePipe(material, layerTemplate.Thickness);
                }
            }

            foreach (var layerTemplate in template.LayersBelowPipe.OrderBy(l => l.Order))
            {
                var material = materialsList.FirstOrDefault(m => m.Id == layerTemplate.MaterialId);
                if (material != null)
                {
                    construction.AddLayerBelowPipe(material, layerTemplate.Thickness);
                }
            }

            return construction;
        }

        public System.Threading.Tasks.Task<Material> ImportMissingMaterialAsync(MaterialSnapshot snapshot)
        {
            return System.Threading.Tasks.Task.FromResult<Material>(null!);
        }

        public System.Threading.Tasks.Task ImportProjectMaterialsAsync(System.Collections.Generic.IEnumerable<MaterialSnapshot> snapshots)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task ImportProjectTemplatesAsync(System.Collections.Generic.IEnumerable<ConstructionTemplate> templates)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public double GetTotalThicknessAbovePipe(ConstructionModel construction)
        {
            return construction.LayersAbovePipe.Sum(l => l.Thickness);
        }

        public double GetTotalThicknessBelowPipe(ConstructionModel construction)
        {
            return construction.Layers.Where(l => l.Position == LayerPosition.BelowPipe).Sum(l => l.Thickness);
        }
    }

    /// <summary>
    /// Мок-репозиторий материалов для тестов
    /// </summary>
    internal class MockMaterialRepository : IMaterialRepository
    {
        private List<Material>? _materials;

        public HashSet<int> MissingLookupMaterialIds { get; } = new();

        public bool IsLoaded => _materials != null;
        public int MaterialsCount => _materials?.Count ?? 0;

        public Task<System.Collections.Generic.IEnumerable<Material>> LoadMaterialsAsync()
        {
            _materials = Material.GetDefaultMaterials();
            return Task.FromResult<System.Collections.Generic.IEnumerable<Material>>(_materials);
        }

        public Material? GetMaterialById(int id)
        {
            return MissingLookupMaterialIds.Contains(id)
                ? null
                : _materials?.FirstOrDefault(m => m.Id == id);
        }

        public System.Collections.Generic.IEnumerable<Material> GetMaterialsByCategory(MaterialCategory category)
        {
            return _materials?.Where(m => m.Category == category) ?? Enumerable.Empty<Material>();
        }

        public System.Collections.Generic.IEnumerable<Material> GetAllMaterials()
        {
            return _materials ?? Enumerable.Empty<Material>();
        }

        public Task<Material> AddAsync(Material material)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            _materials ??= new List<Material>();
            material.Id = _materials.Count > 0 ? _materials.Max(m => m.Id) + 1 : 1;
            _materials.Add(material);
            return Task.FromResult(material);
        }

        public Task<Material> UpdateAsync(Material material)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            var index = _materials?.FindIndex(m => m.Id == material.Id) ?? -1;
            if (index < 0)
            {
                throw new InvalidOperationException($"Материал с id={material.Id} не найден.");
            }

            _materials![index] = material;
            return Task.FromResult(material);
        }

        public Task<bool> DeleteAsync(int id)
        {
            var material = _materials?.FirstOrDefault(m => m.Id == id);
            if (material == null)
            {
                return Task.FromResult(false);
            }

            _materials!.Remove(material);
            return Task.FromResult(true);
        }

        public Task SaveMaterialsAsync()
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Мок-репозиторий конструкций для тестов
    /// </summary>
    internal class MockConstructionRepository : IConstructionRepository
    {
        public Exception? SaveException { get; set; }
        public Exception? LoadException { get; set; }

        public Task SaveConstructionAsync(ConstructionModel construction, string filePath)
        {
            if (SaveException != null)
            {
                throw SaveException;
            }

            return Task.CompletedTask;
        }

        public Task<ConstructionModel?> LoadConstructionAsync(string filePath)
        {
            if (LoadException != null)
            {
                throw LoadException;
            }

            return Task.FromResult<ConstructionModel?>(null);
        }

        public Task SaveToProjectAsync(ConstructionModel construction, int projectId)
        {
            return Task.CompletedTask;
        }

        public Task<ConstructionModel?> LoadFromProjectAsync(int projectId)
        {
            return Task.FromResult<ConstructionModel?>(null);
        }

        public Task<System.Collections.Generic.IEnumerable<string>> GetSavedConstructionsAsync(string directoryPath)
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }
    }

    #endregion
}
