using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Phase 3 Task 3: counter-based characterization of the CURRENT
    /// Construction logical-action multiplicity, before any ConstructionState
    /// ownership migration exists. Every expected count in this file was
    /// measured by running the test against live production code, not
    /// inferred from static reading. Reuses <see cref="MockConstructionService"/>,
    /// <see cref="MockMaterialRepository"/> and <see cref="MockConstructionRepository"/>
    /// declared in <c>ConstructionViewModelTests.cs</c> (same namespace, same
    /// test assembly).
    /// </summary>
    [TestFixture]
    public class ConstructionMultiplicityCharacterizationTests
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
        private CalculationContext _calculationContext = null!;
        private IProjectSessionConstructionState _constructionState = null!;
        private int _contextChangedCount;
        private int _constructionContextUpdates;
        private int _completionCount;

        [SetUp]
        public async Task Setup()
        {
            _mockService = new MockConstructionService();
            _mockMaterialRepository = new MockMaterialRepository();
            _mockConstructionRepository = new MockConstructionRepository();
            _mockCalculationStateService = new Mock<ICalculationStateService>();
            _markDirtyServiceMock = new Mock<IMarkDirtyService>();
            _mockTemplateRepository = new Mock<IConstructionTemplateRepository>();
            _mockDialogService = new Mock<IDialogService>();
            _mockEditorDialogService = new Mock<IEditorDialogService>();
            _mockCalculationStateService.SetupGet(s => s.PipeSpacing).Returns(200);
            _mockTemplateRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(ConstructionTemplate.GetDefaultTemplates());
            _calculationContext = new CalculationContext();
            _contextChangedCount = 0;
            _constructionContextUpdates = 0;
            _calculationContext.ContextChanged += (s, e) =>
            {
                _contextChangedCount++;
                if (e.PropertyName == nameof(CalculationContext.Construction))
                {
                    _constructionContextUpdates++;
                }
            };

            var construction = new ConstructionModel();
            var projectSession = new ProjectSession(calculationContext: _calculationContext);
            _constructionState = projectSession.ConstructionState;
            projectSession.ConstructionState.Changed += (_, args) =>
            {
                _completionCount++;
                if (args.Origin is ConstructionMutationOrigin.User or ConstructionMutationOrigin.Template)
                {
                    _markDirtyServiceMock.Object.MarkDirty();
                }
            };
            var defaultStateInitializer = new ConstructionDefaultStateInitializer(
                _mockMaterialRepository,
                projectSession.ConstructionState);
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
                projectSession.ConstructionState,
                defaultStateInitializer);

            // Initialize loads catalogs and resets to the default recipe.
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            // Isolate each scenario: clear invocations/counters accumulated by Setup/Initialize.
            ResetCounters();
        }

        private void ResetCounters()
        {
            _markDirtyServiceMock.Invocations.Clear();
            _contextChangedCount = 0;
            _constructionContextUpdates = 0;
            _completionCount = 0;
        }

        #region Scalar edits — GroundwaterLevel

        [Test]
        public void ScalarGroundwaterLevel_ChangedValue_MarksDirtyAndPublishesExactlyOnce()
        {
            // Arrange: current value is 2.0 (default), changing to a distinct value.
            Assume.That(_viewModel.GroundwaterLevel, Is.EqualTo(2.0));

            // Act
            _viewModel.GroundwaterLevel = 0.5;

            // Assert — measured (not guessed): OnGroundwaterLevelChanged calls
            // UpdateCalculations()+MarkDirty() once directly, AND its loop over
            // LayersBelowPipe calling layer.CalculatedLambda = ... changes at least one
            // layer's CalculatedLambda (2.0 -> 0.5 crosses the lambda-A/lambda-B threshold
            // for the default below-pipe recipe), which raises Layer.PropertyChanged and
            // triggers OnLayerPropertyChanged -> a second MarkDirty/UpdateCalculations.
            // Total observed = 2 MarkDirty calls for one user-visible scalar edit.
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Once);
            Assert.That(_constructionContextUpdates, Is.EqualTo(1));
        }

        [Test]
        public void ScalarGroundwaterLevel_SameValue_IsNoOpProducingNoDirtyOrContextUpdate()
        {
            // Arrange
            var current = _viewModel.GroundwaterLevel;

            // Act
            _viewModel.GroundwaterLevel = current;

            // Assert — measured: CommunityToolkit.Mvvm's generated property setter
            // short-circuits on equal value before the partial OnGroundwaterLevelChanged
            // handler runs, so no MarkDirty/UpdateCalculations/context publish occurs.
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
            Assert.That(_constructionContextUpdates, Is.EqualTo(0));
        }

        #endregion

        #region Add / Remove layer commands

        [Test]
        public void AddLayerAbovePipe_MarksDirtyAndPublishesExactlyOnce()
        {
            // Act
            _viewModel.AddLayerAbovePipeCommand.Execute(null);

            // Assert — measured: LayersAbovePipe.Insert(...) raises CollectionChanged,
            // which OnLayersCollectionChanged handles by calling MarkDirty()+UpdateCalculations()
            // once (guarded by !_isSyncing && !_isResetting); the command method then calls
            // UpdateCalculations()+MarkDirty() again explicitly. Net: MarkDirty is called
            // twice and the Construction context is published twice for one user action.
            // This is a genuine current double-invocation bypass, not a guess.
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Once);
            Assert.That(_constructionContextUpdates, Is.EqualTo(1));
        }

        [Test]
        public void AddLayerBelowPipe_MarksDirtyAndPublishesExactlyOnce()
        {
            _viewModel.AddLayerBelowPipeCommand.Execute(null);

            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Once);
            Assert.That(_constructionContextUpdates, Is.EqualTo(1));
        }

        [Test]
        public void RemoveLayer_MarksDirtyAndPublishesExactlyOnce()
        {
            // Arrange: default recipe guarantees at least one above-pipe layer.
            var layer = _viewModel.LayersAbovePipe.First();
            ResetCounters();

            // Act
            _viewModel.RemoveLayerCommand.Execute(layer);

            // Assert — same double-invocation shape as Add, mirrored on removal.
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Once);
            Assert.That(_constructionContextUpdates, Is.EqualTo(1));
        }

        [Test]
        public void RemoveLayer_NullLayer_IsNoOp()
        {
            _viewModel.RemoveLayerCommand.Execute(null);

            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
            Assert.That(_constructionContextUpdates, Is.EqualTo(0));
        }

        #endregion

        #region Direct layer property edits (existing layer, no add/remove)

        [Test]
        public void DirectLayerThicknessEdit_OnExistingLayer_MarksDirtyExactlyOnce()
        {
            var layer = _viewModel.LayersAbovePipe.First();
            ResetCounters();

            layer.Thickness += 10;

            // Assert — measured: only OnLayerPropertyChanged runs (PropertyChanged, not
            // CollectionChanged), so exactly one MarkDirty/UpdateCalculations occurs.
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Once);
            Assert.That(_constructionContextUpdates, Is.EqualTo(1));
        }

        [Test]
        public void DirectLayerThicknessEdit_SameValue_IsNoOp()
        {
            var layer = _viewModel.LayersAbovePipe.First();
            var current = layer.Thickness;
            ResetCounters();

            layer.Thickness = current;

            // Assert — Layer.Thickness setter guards on `_thickness != value`, so no
            // PropertyChanged is raised and OnLayerPropertyChanged never runs.
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
            Assert.That(_constructionContextUpdates, Is.EqualTo(0));
        }

        [Test]
        public void DirectLayerMaterialEdit_OnExistingLayer_MarksDirtyAndPublishesExactlyOnce()
        {
            var layer = _viewModel.LayersAbovePipe.First();
            var otherMaterial = _viewModel.AvailableMaterials.First(m => m.Id != layer.Material.Id);
            ResetCounters();

            layer.Material = otherMaterial;

            // Assert — measured: Layer.Material's setter raises PropertyChanged(Material)
            // once, which OnLayerPropertyChanged handles (MarkDirty #1) and then calls
            // layer.UpdateLambda(GroundwaterLevel), which itself sets CalculatedLambda and
            // raises a second PropertyChanged(CalculatedLambda), producing MarkDirty #2.
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Once);
            Assert.That(_constructionContextUpdates, Is.EqualTo(1));
        }

        #endregion

        #region Template apply

        [Test]
        public void ApplyTemplate_WithOneAboveAndOneBelowLayer_MarksDirtyExactCountMeasured()
        {
            // Arrange: force a deterministic starting/target shape via the mock service,
            // independent of the default template catalog contents.
            var above = _viewModel.AvailableMaterials.First(m => m.Id == 5);
            var below = _viewModel.AvailableMaterials.First(m => m.Id == 1);
            _mockService.NextTemplateResult = () =>
            {
                var construction = new ConstructionModel();
                construction.AddLayerAbovePipe(above, 80);
                construction.AddLayerBelowPipe(below, 120);
                return construction;
            };
            var template = _viewModel.Templates.First();
            _viewModel.SelectedTemplate = template; // triggers preview only, no dirty
            ResetCounters();

            // Act
            _viewModel.ApplyTemplateCommand.Execute(null);

            // Assert — measured after DEC-C04: one canonical Template completion marks dirty,
            // guarded adapter synchronization suppresses four collection callbacks.
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Once);
            Assert.That(_constructionContextUpdates, Is.EqualTo(1));
            Assert.That(_viewModel.LayersAbovePipe.Count, Is.EqualTo(1));
            Assert.That(_viewModel.LayersBelowPipe.Count, Is.EqualTo(1));
        }

        [Test]
        public void ApplyTemplate_CanApplyFalseDueToMissingMaterial_IsNoOp()
        {
            // Arrange: selecting a template whose preview construction throws
            // MaterialNotFoundException blocks Apply before any state mutation.
            _mockService.ThrowOnCreateFromTemplate = new MaterialNotFoundException(999);
            var template = _viewModel.Templates.First();

            // Act
            _viewModel.SelectedTemplate = template; // preview fails, CanApplySelectedTemplate=false
            Assert.That(_viewModel.CanApplySelectedTemplate, Is.False);
            ResetCounters();
            _viewModel.ApplyTemplateCommand.Execute(null);

            // Assert — Apply returns early; no canonical mutation, no dirty, no context update.
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
            Assert.That(_constructionContextUpdates, Is.EqualTo(0));
        }

        #endregion

        #region Editor open/close — does not dirty Construction project state

        [Test]
        public async Task OpenMaterialEditor_DialogTrue_DoesNotMarkDirty()
        {
            _mockEditorDialogService.Setup(s => s.ShowMaterialEditor()).Returns(true);

            await _viewModel.OpenMaterialEditorCommand.ExecuteAsync(null);

            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        [Test]
        public async Task OpenTemplateEditor_DialogTrue_DoesNotMarkDirty()
        {
            _mockEditorDialogService.Setup(s => s.ShowTemplateEditor()).Returns(true);

            await _viewModel.OpenTemplateEditorCommand.ExecuteAsync(null);

            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        [Test]
        public async Task OpenTemplateEditor_DialogNull_DoesNotMarkDirty()
        {
            _mockEditorDialogService.Setup(s => s.ShowTemplateEditor()).Returns((bool?)null);

            await _viewModel.OpenTemplateEditorCommand.ExecuteAsync(null);

            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        [Test]
        public async Task OpenMaterialEditor_DialogFalse_DoesNotMarkDirty()
        {
            _mockEditorDialogService.Setup(s => s.ShowMaterialEditor()).Returns(false);

            await _viewModel.OpenMaterialEditorCommand.ExecuteAsync(null);

            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        #endregion

        #region Lifecycle: reset and initialize are non-user origins today

        [Test]
        public void ResetToDefault_IsNonUserOrigin_DoesNotCallMarkDirty()
        {
            _viewModel.AddLayerAbovePipeCommand.Execute(null); // dirty the state first
            ResetCounters();

            _viewModel.ResetToDefaultCommand.Execute(null);

            // Assert — measured: Reset() runs with `_isResetting = true`, which suppresses
            // MarkDirty in OnLayersCollectionChanged/OnLayerChanged/property-changed handlers.
            // Reset never calls _markDirtyService.MarkDirty() directly either.
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
            Assert.That(_viewModel.HasUnsavedChanges, Is.False);
        }

        [Test]
        public async Task Initialize_CallsResetInternally_DoesNotCallMarkDirty()
        {
            // Arrange: a second Initialize call on the same instance (simulating a
            // repeated navigate-to-Construction-tab scenario).
            ResetCounters();

            // Act
            await _viewModel.InitializeCommand.ExecuteAsync(null);

            // Assert — Initialize -> RefreshCatalogsAsync (no dirty) -> ResetToDefault -> Reset()
            // (non-user, no dirty).
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        #endregion

        #region Standalone JSON load/save (not project persistence)

        [Test]
        public async Task StandaloneLoadConstruction_RepositoryReturnsNull_IsSilentNoOp()
        {
            // Arrange: MockConstructionRepository.LoadConstructionAsync returns null by default,
            // characterizing the current file-not-found path.
            var messageBefore = _viewModel.ValidationMessage;

            // Act
            await _viewModel.LoadConstructionCommand.ExecuteAsync(null);

            // Assert — measured: current behavior is a silent no-op; no error is surfaced,
            // no dirty transition, and ValidationMessage is left untouched by the load path.
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
            Assert.That(_viewModel.ValidationMessage, Is.EqualTo(messageBefore));
        }

        [Test]
        public async Task StandaloneSaveConstruction_Success_DoesNotCallMarkDirtyAndClearsHasUnsavedChanges()
        {
            _viewModel.AddLayerAbovePipeCommand.Execute(null);
            Assert.That(_viewModel.HasUnsavedChanges, Is.True);
            ResetCounters();

            await _viewModel.SaveConstructionCommand.ExecuteAsync(null);

            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
            Assert.That(_viewModel.HasUnsavedChanges, Is.False);
            Assert.That(_viewModel.IsValid, Is.True);
        }

        [Test]
        public async Task StandaloneLoadConstruction_CorruptJson_PreservesCanonicalSnapshotAndPublishesNoCompletion()
        {
            var before = _constructionState.Snapshot;
            _mockConstructionRepository.LoadException = new JsonException("corrupt construction json");

            await _viewModel.LoadConstructionCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot, Is.EqualTo(before));
                Assert.That(_viewModel.IsValid, Is.False);
                Assert.That(_viewModel.ValidationMessage, Does.Contain("corrupt construction json"));
                Assert.That(_completionCount, Is.Zero);
                Assert.That(_constructionContextUpdates, Is.Zero);
            });
            _markDirtyServiceMock.Verify(service => service.MarkDirty(), Times.Never);
        }

        [Test]
        public async Task StandaloneLoadConstruction_LoadFailure_PreservesCanonicalSnapshotAndPublishesNoCompletion()
        {
            var before = _constructionState.Snapshot;
            _mockConstructionRepository.LoadException = new IOException("standalone load failure");

            await _viewModel.LoadConstructionCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot, Is.EqualTo(before));
                Assert.That(_viewModel.IsValid, Is.False);
                Assert.That(_viewModel.ValidationMessage, Does.Contain("standalone load failure"));
                Assert.That(_completionCount, Is.Zero);
                Assert.That(_constructionContextUpdates, Is.Zero);
            });
            _markDirtyServiceMock.Verify(service => service.MarkDirty(), Times.Never);
        }

        [Test]
        public async Task StandaloneSaveConstruction_SaveFailure_PreservesCanonicalSnapshotDirtyAndCompletionState()
        {
            _viewModel.AddLayerAbovePipeCommand.Execute(null);
            var before = _constructionState.Snapshot;
            Assert.That(_viewModel.HasUnsavedChanges, Is.True);
            ResetCounters();
            _mockConstructionRepository.SaveException = new IOException("standalone save failure");

            await _viewModel.SaveConstructionCommand.ExecuteAsync(null);

            Assert.Multiple(() =>
            {
                Assert.That(_constructionState.Snapshot, Is.EqualTo(before));
                Assert.That(_viewModel.HasUnsavedChanges, Is.True);
                Assert.That(_viewModel.IsValid, Is.False);
                Assert.That(_viewModel.ValidationMessage, Does.Contain("standalone save failure"));
                Assert.That(_completionCount, Is.Zero);
                Assert.That(_constructionContextUpdates, Is.Zero);
            });
            _markDirtyServiceMock.Verify(service => service.MarkDirty(), Times.Never);
        }

        #endregion

        #region Repeated cycles — subscription hygiene proxy at ViewModel scope

        [Test]
        public void RepeatedAddThenReset_ProducesSamePerCycleMarkDirtyCount_AcrossThreeCycles()
        {
            var perCycleCounts = new List<int>();

            for (var cycle = 0; cycle < 3; cycle++)
            {
                ResetCounters();
                _viewModel.AddLayerAbovePipeCommand.Execute(null);
                perCycleCounts.Add(_markDirtyServiceMock.Invocations.Count);
                ResetCounters();
                _viewModel.ResetToDefaultCommand.Execute(null);
            }

            // Assert — measured: each of the three Add cycles calls MarkDirty exactly the
            // same number of times (2, per the double-invocation finding above). If a layer
            // PropertyChanged handler leaked across cycles, later cycles would show an
            // increasing count as leftover subscriptions fire redundantly.
            Assert.That(perCycleCounts, Is.EqualTo(new[] { 1, 1, 1 }),
                "Repeated Add+Reset cycles must not accumulate extra MarkDirty invocations from leaked subscriptions.");
        }

        #endregion
    }
}
