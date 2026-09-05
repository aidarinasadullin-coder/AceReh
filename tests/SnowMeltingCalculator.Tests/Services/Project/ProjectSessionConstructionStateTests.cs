using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Phase 3 Task 4: direct unit tests for <see cref="ProjectSessionConstructionState"/>,
    /// proving DEC-C01..C03 before any ViewModel wiring exists (Task 4 explicitly does not
    /// wire ConstructionViewModel).
    /// </summary>
    [TestFixture]
    public sealed class ProjectSessionConstructionStateTests
    {
        private Mock<IMarkDirtyService> _markDirtyServiceMock = null!;
        private ProjectSessionConstructionState _state = null!;

        [SetUp]
        public void Setup()
        {
            _markDirtyServiceMock = new Mock<IMarkDirtyService>();
            _state = new ProjectSessionConstructionState(_markDirtyServiceMock.Object);
        }

        #region Snapshot defaults

        [Test]
        public void InitialSnapshot_HasEmptyDefaults()
        {
            var snapshot = _state.Snapshot;

            Assert.That(snapshot.GroundwaterLevel, Is.EqualTo(0.0));
            Assert.That(snapshot.LayersAbovePipe, Is.Empty);
            Assert.That(snapshot.LayersBelowPipe, Is.Empty);
        }

        #endregion

        #region Task 10 authoritative completion

        [Test]
        public void ApplySnapshot_ValidUserChange_PublishesFreshProjectionExactlyOnceAndMarksDirtyOnce()
        {
            var context = new CalculationContext();
            var session = new ProjectSession(calculationContext: context);
            var state = (ProjectSessionConstructionState)session.ConstructionState;
            var contextUpdates = 0;
            context.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Construction))
                {
                    contextUpdates++;
                }
            };
            var layer = new ConstructionLayerSnapshot(
                Guid.NewGuid(), 5, "Бетон", 100, 1.74, false, LayerPosition.AbovePipe, 0);

            var result = state.ApplySnapshot(
                new ConstructionStateSnapshot(2.0, new[] { layer }, Array.Empty<ConstructionLayerSnapshot>()),
                ConstructionMutationOrigin.User);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
                Assert.That(contextUpdates, Is.EqualTo(1));
                Assert.That(context.Construction, Is.SameAs(state.Projection));
                Assert.That(context.Construction!.R1Total, Is.EqualTo(100.0 / 1.74 / 1000.0).Within(1e-10));
                Assert.That(session.IsDirty, Is.True);
            });
        }

        [Test]
        public void ApplySnapshot_NoChangeOrRejected_PublishesNothingAndDoesNotDirty()
        {
            var context = new CalculationContext();
            var session = new ProjectSession(calculationContext: context);
            var state = (ProjectSessionConstructionState)session.ConstructionState;
            var layerId = Guid.NewGuid();
            var layer = new ConstructionLayerSnapshot(
                layerId, 5, "Бетон", 100, 1.74, false, LayerPosition.AbovePipe, 0);
            var valid = new ConstructionStateSnapshot(
                2.0, new[] { layer }, Array.Empty<ConstructionLayerSnapshot>());
            state.ApplySnapshot(valid, ConstructionMutationOrigin.Initialization);
            var contextUpdates = 0;
            context.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Construction))
                {
                    contextUpdates++;
                }
            };
            var duplicate = new ConstructionStateSnapshot(
                2.0,
                new[] { layer, layer with { Order = 1 } },
                Array.Empty<ConstructionLayerSnapshot>());

            var noChange = state.ApplySnapshot(valid, ConstructionMutationOrigin.User);
            var rejected = state.ApplySnapshot(duplicate, ConstructionMutationOrigin.User);

            Assert.Multiple(() =>
            {
                Assert.That(noChange.Status, Is.EqualTo(ConstructionMutationStatus.NoChange));
                Assert.That(rejected.Status, Is.EqualTo(ConstructionMutationStatus.Rejected));
                Assert.That(contextUpdates, Is.EqualTo(0));
                Assert.That(session.IsDirty, Is.False);
            });
        }

        [TestCase(ConstructionMutationOrigin.ProjectLoad)]
        [TestCase(ConstructionMutationOrigin.Reset)]
        [TestCase(ConstructionMutationOrigin.Restore)]
        [TestCase(ConstructionMutationOrigin.SystemApply)]
        [TestCase(ConstructionMutationOrigin.Initialization)]
        public void ApplySnapshot_ValidLifecycleChange_PublishesNothingWithoutUserDirty(ConstructionMutationOrigin origin)
        {
            var context = new CalculationContext();
            var session = new ProjectSession(calculationContext: context);
            var state = (ProjectSessionConstructionState)session.ConstructionState;
            var contextUpdates = 0;
            context.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Construction))
                {
                    contextUpdates++;
                }
            };
            var layer = new ConstructionLayerSnapshot(
                Guid.NewGuid(), 5, "Бетон", 100, 1.74, false, LayerPosition.AbovePipe, 0);

            var result = state.ApplySnapshot(
                new ConstructionStateSnapshot(2.0, new[] { layer }, Array.Empty<ConstructionLayerSnapshot>()),
                origin);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
                Assert.That(contextUpdates, Is.EqualTo(0));
                Assert.That(session.IsDirty, Is.False);
            });
        }

        [Test]
        public void ApplySnapshot_InvalidUserChange_MarksDirtyButDoesNotPublish()
        {
            var context = new CalculationContext();
            var session = new ProjectSession(calculationContext: context);
            var state = (ProjectSessionConstructionState)session.ConstructionState;
            var contextUpdates = 0;
            context.ContextChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CalculationContext.Construction))
                {
                    contextUpdates++;
                }
            };

            var result = state.ApplySnapshot(
                new ConstructionStateSnapshot(2.0, Array.Empty<ConstructionLayerSnapshot>(), Array.Empty<ConstructionLayerSnapshot>()),
                ConstructionMutationOrigin.User);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
                Assert.That(state.Projection.IsValid, Is.False);
                Assert.That(contextUpdates, Is.EqualTo(0));
                Assert.That(session.IsDirty, Is.True);
            });
        }

        #endregion

        #region Structural equality (DEC-C02)

        [Test]
        public void Snapshot_TwoIndependentButEqualLayerLists_AreStructurallyEqual()
        {
            var id = Guid.NewGuid();
            var layerA = new ConstructionLayerSnapshot(id, 5, "Бетон", 100, 1.74, false, LayerPosition.AbovePipe, 0);
            var layerB = new ConstructionLayerSnapshot(id, 5, "Бетон", 100, 1.74, false, LayerPosition.AbovePipe, 0);

            var snapshotA = new ConstructionStateSnapshot(2.0, new[] { layerA }, Array.Empty<ConstructionLayerSnapshot>());
            var snapshotB = new ConstructionStateSnapshot(2.0, new[] { layerB }, Array.Empty<ConstructionLayerSnapshot>());

            Assert.That(snapshotA, Is.EqualTo(snapshotB));
            Assert.That(snapshotA == snapshotB, Is.True);
            Assert.That(snapshotA.GetHashCode(), Is.EqualTo(snapshotB.GetHashCode()));
        }

        [Test]
        public void Snapshot_DifferOnlyByOneLayerField_AreNotEqual()
        {
            var id = Guid.NewGuid();
            var layerA = new ConstructionLayerSnapshot(id, 5, "Бетон", 100, 1.74, false, LayerPosition.AbovePipe, 0);
            var layerB = new ConstructionLayerSnapshot(id, 5, "Бетон", 105, 1.74, false, LayerPosition.AbovePipe, 0);

            var snapshotA = new ConstructionStateSnapshot(2.0, new[] { layerA }, Array.Empty<ConstructionLayerSnapshot>());
            var snapshotB = new ConstructionStateSnapshot(2.0, new[] { layerB }, Array.Empty<ConstructionLayerSnapshot>());

            Assert.That(snapshotA, Is.Not.EqualTo(snapshotB));
        }

        [Test]
        public void Snapshot_ReorderedSequence_IsNotEqual_EvenWithSameElements()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var layer1 = new ConstructionLayerSnapshot(id1, 5, "Бетон", 100, 1.74, false, LayerPosition.AbovePipe, 0);
            var layer2 = new ConstructionLayerSnapshot(id2, 11, "Асфальт", 50, 0.8, false, LayerPosition.AbovePipe, 1);

            var orderAB = new ConstructionStateSnapshot(2.0, new[] { layer1, layer2 }, Array.Empty<ConstructionLayerSnapshot>());
            // Same two layers, order swapped (their own Order field also swapped to reflect the new position).
            var layer1Reordered = layer1 with { Order = 1 };
            var layer2Reordered = layer2 with { Order = 0 };
            var orderBA = new ConstructionStateSnapshot(2.0, new[] { layer2Reordered, layer1Reordered }, Array.Empty<ConstructionLayerSnapshot>());

            Assert.That(orderAB, Is.Not.EqualTo(orderBA),
                "Sequence order is semantic; a reordered sequence must not be structurally equal.");
        }

        #endregion

        #region Apply: scalar mutations

        [Test]
        public void Apply_SetGroundwaterLevel_ChangedValue_ReturnsChangedAndRaisesOneEvent()
        {
            var eventCount = 0;
            _state.Changed += (_, __) => eventCount++;

            var result = _state.Apply(new ConstructionMutation.SetGroundwaterLevel(0.5), ConstructionMutationOrigin.User);

            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
            Assert.That(result.After.GroundwaterLevel, Is.EqualTo(0.5));
            Assert.That(eventCount, Is.EqualTo(1));
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Once);
        }

        [Test]
        public void Apply_SetGroundwaterLevel_SameValue_ReturnsNoChangeAndRaisesNoEvent()
        {
            _state.Apply(new ConstructionMutation.SetGroundwaterLevel(2.0), ConstructionMutationOrigin.User);
            var eventCount = 0;
            _state.Changed += (_, __) => eventCount++;
            _markDirtyServiceMock.Invocations.Clear();

            var result = _state.Apply(new ConstructionMutation.SetGroundwaterLevel(2.0), ConstructionMutationOrigin.User);

            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.NoChange));
            Assert.That(eventCount, Is.EqualTo(0));
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        #endregion

        #region Apply: add/remove/edit/reorder (DEC-C03 required semantics)

        [Test]
        public void Apply_AddLayer_AssignsStableNonEmptyGuidAndCorrectOrder()
        {
            var result1 = _state.Apply(
                new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 5, "Бетон", 100, 1.74, false),
                ConstructionMutationOrigin.User);
            var result2 = _state.Apply(
                new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 11, "Асфальт", 50, 0.8, false),
                ConstructionMutationOrigin.User);

            Assert.That(result1.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
            Assert.That(result2.Status, Is.EqualTo(ConstructionMutationStatus.Changed));

            var layers = _state.Snapshot.LayersAbovePipe;
            Assert.That(layers.Count, Is.EqualTo(2));
            Assert.That(layers[0].Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(layers[1].Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(layers[0].Id, Is.Not.EqualTo(layers[1].Id));
            Assert.That(layers[0].Order, Is.EqualTo(0));
            Assert.That(layers[1].Order, Is.EqualTo(1));
        }

        [Test]
        public void Apply_RemoveLayer_ByStableId_RemovesExactlyThatLayerAndReindexesRemainder()
        {
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.BelowPipe, 1, "Песок", 150, 0.4, false), ConstructionMutationOrigin.User);
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.BelowPipe, 2, "Грунт", 300, 0.6, false), ConstructionMutationOrigin.User);
            var idToRemove = _state.Snapshot.LayersBelowPipe[0].Id;

            var result = _state.Apply(new ConstructionMutation.RemoveLayer(idToRemove), ConstructionMutationOrigin.User);

            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
            var remaining = _state.Snapshot.LayersBelowPipe;
            Assert.That(remaining.Count, Is.EqualTo(1));
            Assert.That(remaining[0].MaterialId, Is.EqualTo(2));
            Assert.That(remaining[0].Order, Is.EqualTo(0), "Remaining layer must be reindexed to Order 0.");
        }

        [Test]
        public void Apply_RemoveLayer_UnknownId_IsNoOp()
        {
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 5, "Бетон", 100, 1.74, false), ConstructionMutationOrigin.User);
            _markDirtyServiceMock.Invocations.Clear();

            var result = _state.Apply(new ConstructionMutation.RemoveLayer(Guid.NewGuid()), ConstructionMutationOrigin.User);

            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.NoChange));
            Assert.That(_state.Snapshot.LayersAbovePipe.Count, Is.EqualTo(1));
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        [Test]
        public void Apply_EditLayer_ByStableId_UpdatesFieldsWithoutChangingIdOrPosition()
        {
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 5, "Бетон", 100, 1.74, false), ConstructionMutationOrigin.User);
            var originalId = _state.Snapshot.LayersAbovePipe[0].Id;

            var result = _state.Apply(
                new ConstructionMutation.EditLayer(originalId, 11, "Асфальт", 60, 0.8, true),
                ConstructionMutationOrigin.User);

            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
            var edited = _state.Snapshot.LayersAbovePipe[0];
            Assert.That(edited.Id, Is.EqualTo(originalId), "Edit must preserve the stable layer Id.");
            Assert.That(edited.MaterialId, Is.EqualTo(11));
            Assert.That(edited.Thickness, Is.EqualTo(60));
            Assert.That(edited.IsLambdaOverridden, Is.True);
            Assert.That(edited.Position, Is.EqualTo(LayerPosition.AbovePipe));
        }

        [Test]
        public void Apply_EditLayer_SameValues_IsNoOp()
        {
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 5, "Бетон", 100, 1.74, false), ConstructionMutationOrigin.User);
            var id = _state.Snapshot.LayersAbovePipe[0].Id;
            _markDirtyServiceMock.Invocations.Clear();

            var result = _state.Apply(
                new ConstructionMutation.EditLayer(id, 5, "Бетон", 100, 1.74, false),
                ConstructionMutationOrigin.User);

            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.NoChange));
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        [Test]
        public void Apply_EditLayer_UnknownId_IsRejected()
        {
            var result = _state.Apply(
                new ConstructionMutation.EditLayer(Guid.NewGuid(), 5, "Бетон", 100, 1.74, false),
                ConstructionMutationOrigin.User);

            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("LayerNotFound"));
        }

        [Test]
        public void Apply_ReorderLayers_ValidPermutation_ChangesOrderAndIsIdempotentOnRepeat()
        {
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 5, "Бетон", 100, 1.74, false), ConstructionMutationOrigin.User);
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 11, "Асфальт", 50, 0.8, false), ConstructionMutationOrigin.User);
            var ids = _state.Snapshot.LayersAbovePipe.Select(l => l.Id).ToArray();
            var reversed = ids.Reverse().ToArray();

            var result = _state.Apply(new ConstructionMutation.ReorderLayers(LayerPosition.AbovePipe, reversed), ConstructionMutationOrigin.User);

            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
            var reordered = _state.Snapshot.LayersAbovePipe;
            Assert.That(reordered[0].Id, Is.EqualTo(reversed[0]));
            Assert.That(reordered[1].Id, Is.EqualTo(reversed[1]));
            Assert.That(reordered[0].Order, Is.EqualTo(0));
            Assert.That(reordered[1].Order, Is.EqualTo(1));

            // Applying the identical order again must be a no-op.
            _markDirtyServiceMock.Invocations.Clear();
            var repeat = _state.Apply(new ConstructionMutation.ReorderLayers(LayerPosition.AbovePipe, reversed), ConstructionMutationOrigin.User);
            Assert.That(repeat.Status, Is.EqualTo(ConstructionMutationStatus.NoChange));
        }

        [Test]
        public void Apply_ReorderLayers_NotAPermutation_IsRejected()
        {
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 5, "Бетон", 100, 1.74, false), ConstructionMutationOrigin.User);

            var result = _state.Apply(
                new ConstructionMutation.ReorderLayers(LayerPosition.AbovePipe, new[] { Guid.NewGuid() }),
                ConstructionMutationOrigin.User);

            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("ReorderNotAPermutation"));
        }

        #endregion

        #region Rejected mutation atomicity

        [Test]
        public void ApplySnapshot_DuplicateLayerIds_IsRejected_AndCanonicalStateUnchanged()
        {
            var sharedId = Guid.NewGuid();
            var layer1 = new ConstructionLayerSnapshot(sharedId, 5, "Бетон", 100, 1.74, false, LayerPosition.AbovePipe, 0);
            var layer2 = new ConstructionLayerSnapshot(sharedId, 11, "Асфальт", 50, 0.8, false, LayerPosition.AbovePipe, 1);
            var invalidCandidate = new ConstructionStateSnapshot(2.0, new[] { layer1, layer2 }, Array.Empty<ConstructionLayerSnapshot>());

            var eventCount = 0;
            _state.Changed += (_, __) => eventCount++;

            var result = _state.ApplySnapshot(invalidCandidate, ConstructionMutationOrigin.User);

            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("DuplicateLayerId"));
            Assert.That(_state.Snapshot.LayersAbovePipe, Is.Empty, "Rejected candidate must not mutate canonical state.");
            Assert.That(eventCount, Is.EqualTo(0));
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        #endregion

        #region ResetToDefaults

        [Test]
        public void ResetToDefaults_NonUserOrigin_ChangesStateWithoutMarkingDirty()
        {
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 5, "Бетон", 100, 1.74, false), ConstructionMutationOrigin.User);
            _markDirtyServiceMock.Invocations.Clear();

            var defaults = new ConstructionDefaults(
                2.0,
                new[] { new ConstructionLayerSnapshot(Guid.NewGuid(), 5, "Бетон", 100, 1.74, false, LayerPosition.AbovePipe, 0) },
                Array.Empty<ConstructionLayerSnapshot>());

            var result = _state.ResetToDefaults(defaults, ConstructionMutationOrigin.Reset);

            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
            _markDirtyServiceMock.Verify(m => m.MarkDirty(), Times.Never);
        }

        [Test]
        public void ResetToDefaults_EmptyLayersMatchingScalars_IsNoOp()
        {
            // Initial state: GroundwaterLevel=0.0, empty collections.
            // Defaults matching the initial empty state must be a no-op.
            var defaults = new ConstructionDefaults(0.0, Array.Empty<ConstructionLayerSnapshot>(), Array.Empty<ConstructionLayerSnapshot>());
            var result = _state.ResetToDefaults(defaults, ConstructionMutationOrigin.Reset);
            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.NoChange),
                "Resetting an already-empty/default state to the same empty/default recipe must be a no-op.");
        }

        [Test]
        public void ResetToDefaults_DifferentGroundwater_IsChanged()
        {
            var defaults = new ConstructionDefaults(2.0, Array.Empty<ConstructionLayerSnapshot>(), Array.Empty<ConstructionLayerSnapshot>());
            var result = _state.ResetToDefaults(defaults, ConstructionMutationOrigin.Reset);
            Assert.That(result.Status, Is.EqualTo(ConstructionMutationStatus.Changed));
            Assert.That(_state.Snapshot.GroundwaterLevel, Is.EqualTo(2.0));
        }

        #endregion

        #region Projection (Task 5: IConstructionData read-only boundary)

        [Test]
        public void Projection_InitialState_HasDefaultValues()
        {
            Assert.That(_state.Projection.R1Total, Is.EqualTo(0.0));
            Assert.That(_state.Projection.R2Total, Is.EqualTo(0.0));
            Assert.That(_state.Projection.LambdaE, Is.EqualTo(1.6));
        }

        [Test]
        public void Projection_AfterAddLayerAbovePipe_R1TotalMatchesSnapshot()
        {
            _state.Apply(
                new ConstructionMutation.AddLayer(
                    LayerPosition.AbovePipe, 5, "Бетон", 100, 1.74, false),
                ConstructionMutationOrigin.User);

            // R = d / λ / 1000 = 100 / 1.74 / 1000
            var expected = 100.0 / 1.74 / 1000.0;
            Assert.That(_state.Projection.R1Total, Is.EqualTo(expected).Within(1e-10));
        }

        [Test]
        public void Projection_AfterAddLayerBelowPipe_R2TotalMatchesSnapshot()
        {
            _state.Apply(
                new ConstructionMutation.AddLayer(
                    LayerPosition.BelowPipe, 1, "Песок", 150, 0.4, false),
                ConstructionMutationOrigin.User);

            var expected = 150.0 / 0.4 / 1000.0;
            Assert.That(_state.Projection.R2Total, Is.EqualTo(expected).Within(1e-10));
        }

        [Test]
        public void Projection_LambdaE_IsLastAbovePipeLayerLambda()
        {
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 11, "Асфальт", 50, 0.8, false), ConstructionMutationOrigin.User);
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 5, "Бетон", 100, 1.74, false), ConstructionMutationOrigin.User);

            // Last above-pipe layer (nearest to pipe) = Бетон, λ = 1.74
            Assert.That(_state.Projection.LambdaE, Is.EqualTo(1.74).Within(1e-10));
        }

        [Test]
        public void Projection_IsRefreshedAtomicallyAfterMutation_NeverLagsSnapshot()
        {
            // Verify that Projection reflects the state AFTER a mutation, not before.
            _state.Apply(new ConstructionMutation.AddLayer(LayerPosition.AbovePipe, 5, "Бетон", 100, 1.74, false), ConstructionMutationOrigin.User);
            var projAfterAdd = _state.Projection.R1Total;

            _state.Apply(new ConstructionMutation.RemoveLayer(_state.Snapshot.LayersAbovePipe[0].Id), ConstructionMutationOrigin.User);
            var projAfterRemove = _state.Projection.R1Total;

            Assert.That(projAfterAdd, Is.GreaterThan(0.0));
            Assert.That(projAfterRemove, Is.EqualTo(0.0),
                "Projection must reflect the current snapshot after every mutation.");
        }

        #endregion
    }
}
