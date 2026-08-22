using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Canonical implementation of the Construction project-state slice.
    /// Created and held by <see cref="ProjectSession"/>; not registered in DI.
    /// Does not access dialogs, repositories or the ViewModel.
    /// </summary>
    public sealed class ProjectSessionConstructionState : IProjectSessionConstructionState
    {
        private readonly IMarkDirtyService? _markDirtyService;
        private readonly CalculationContext? _calculationContext;
        private ConstructionStateProjection _projection;

        private double _groundwaterLevel;
        private bool _hasLoads;
        private List<ConstructionLayerSnapshot> _layersAbovePipe = new();
        private List<ConstructionLayerSnapshot> _layersBelowPipe = new();

        public ProjectSessionConstructionState(
            IMarkDirtyService? markDirtyService = null,
            CalculationContext? calculationContext = null)
        {
            _markDirtyService = markDirtyService;
            _calculationContext = calculationContext;
            _projection = new ConstructionStateProjection(Snapshot);
        }

        /// <summary>
        /// Current read-only <see cref="IConstructionData"/> projection derived from the
        /// canonical snapshot. Refreshed atomically after every successful mutation.
        /// Consumers that need <see cref="SnowMeltingCalculator.Models.Thermal.IConstructionData"/>
        /// (e.g. CalculationContext) should read this property, not the mutable Construction model.
        /// </summary>
        public ConstructionStateProjection Projection => _projection;

        public SnowMeltingCalculator.Models.Thermal.IConstructionData CurrentProjection => _projection;

        public ConstructionStateSnapshot Snapshot => new(
            _groundwaterLevel,
            _hasLoads,
            _layersAbovePipe.ToArray(),
            _layersBelowPipe.ToArray());

        public event EventHandler<ConstructionStateChangedEventArgs>? Changed;

        public ConstructionMutationResult Apply(ConstructionMutation mutation, ConstructionMutationOrigin origin)
        {
            if (mutation is null)
            {
                throw new ArgumentNullException(nameof(mutation));
            }

            var oldSnapshot = Snapshot;

            switch (mutation)
            {
                case ConstructionMutation.SetGroundwaterLevel setGroundwater:
                    return ApplyScalar(oldSnapshot, origin, () =>
                    {
                        _groundwaterLevel = setGroundwater.Value;
                    });

                case ConstructionMutation.SetHasLoads setHasLoads:
                    return ApplyScalar(oldSnapshot, origin, () =>
                    {
                        _hasLoads = setHasLoads.Value;
                    });

                case ConstructionMutation.AddLayer addLayer:
                    return ApplyAddLayer(oldSnapshot, origin, addLayer);

                case ConstructionMutation.RemoveLayer removeLayer:
                    return ApplyRemoveLayer(oldSnapshot, origin, removeLayer.LayerId);

                case ConstructionMutation.EditLayer editLayer:
                    return ApplyEditLayer(oldSnapshot, origin, editLayer);

                case ConstructionMutation.ReorderLayers reorder:
                    return ApplyReorder(oldSnapshot, origin, reorder);

                case ConstructionMutation.ClearLayers:
                    return ApplyClearLayers(oldSnapshot, origin);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mutation), mutation, "Unknown construction mutation.");
            }
        }

        public ConstructionMutationResult ApplySnapshot(ConstructionStateSnapshot candidate, ConstructionMutationOrigin origin)
        {
            if (candidate is null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            var oldSnapshot = Snapshot;

            var normalized = Normalize(candidate);
            if (!TryValidate(normalized, out var errorCode))
            {
                return new ConstructionMutationResult(
                    ConstructionMutationStatus.Rejected,
                    origin,
                    oldSnapshot,
                    oldSnapshot,
                    errorCode);
            }

            if (normalized.Equals(oldSnapshot))
            {
                return new ConstructionMutationResult(
                    ConstructionMutationStatus.NoChange,
                    origin,
                    oldSnapshot,
                    oldSnapshot);
            }

            CommitSnapshot(normalized);
            return CompleteChanged(oldSnapshot, origin);
        }

        public ConstructionMutationResult ResetToDefaults(ConstructionDefaults defaults, ConstructionMutationOrigin origin)
        {
            if (defaults is null)
            {
                throw new ArgumentNullException(nameof(defaults));
            }

            var oldSnapshot = Snapshot;

            var candidate = new ConstructionStateSnapshot(
                defaults.GroundwaterLevel,
                false,
                defaults.LayersAbovePipe.ToArray(),
                defaults.LayersBelowPipe.ToArray());

            var normalized = Normalize(candidate);

            if (normalized.Equals(oldSnapshot))
            {
                return new ConstructionMutationResult(
                    ConstructionMutationStatus.NoChange,
                    origin,
                    oldSnapshot,
                    oldSnapshot);
            }

            CommitSnapshot(normalized);
            return CompleteChanged(oldSnapshot, origin);
        }

        private ConstructionMutationResult ApplyScalar(
            ConstructionStateSnapshot oldSnapshot,
            ConstructionMutationOrigin origin,
            Action mutate)
        {
            mutate();
            var candidate = Snapshot;

            if (candidate.Equals(oldSnapshot))
            {
                // Revert speculative field write is unnecessary because the value is unchanged.
                return new ConstructionMutationResult(
                    ConstructionMutationStatus.NoChange,
                    origin,
                    oldSnapshot,
                    oldSnapshot);
            }

            return CompleteChanged(oldSnapshot, origin);
        }

        private ConstructionMutationResult ApplyAddLayer(
            ConstructionStateSnapshot oldSnapshot,
            ConstructionMutationOrigin origin,
            ConstructionMutation.AddLayer addLayer)
        {
            var target = addLayer.Position == Models.Construction.LayerPosition.AbovePipe
                ? _layersAbovePipe
                : _layersBelowPipe;

            var newLayer = new ConstructionLayerSnapshot(
                Guid.NewGuid(),
                addLayer.MaterialId,
                addLayer.MaterialName,
                addLayer.Thickness,
                addLayer.CalculatedLambda,
                addLayer.IsLambdaOverridden,
                addLayer.Position,
                target.Count);

            target.Add(newLayer);
            Reindex();

            // Add always changes state (a fresh Guid never structurally equals the prior snapshot).
            return CompleteChanged(oldSnapshot, origin);
        }

        private ConstructionMutationResult ApplyRemoveLayer(
            ConstructionStateSnapshot oldSnapshot,
            ConstructionMutationOrigin origin,
            Guid layerId)
        {
            var removedAbove = _layersAbovePipe.RemoveAll(l => l.Id == layerId) > 0;
            var removedBelow = !removedAbove && _layersBelowPipe.RemoveAll(l => l.Id == layerId) > 0;

            if (!removedAbove && !removedBelow)
            {
                return new ConstructionMutationResult(
                    ConstructionMutationStatus.NoChange,
                    origin,
                    oldSnapshot,
                    oldSnapshot);
            }

            Reindex();
            return CompleteChanged(oldSnapshot, origin);
        }

        private ConstructionMutationResult ApplyEditLayer(
            ConstructionStateSnapshot oldSnapshot,
            ConstructionMutationOrigin origin,
            ConstructionMutation.EditLayer editLayer)
        {
            var aboveIndex = _layersAbovePipe.FindIndex(l => l.Id == editLayer.LayerId);
            List<ConstructionLayerSnapshot> collection;
            int index;

            if (aboveIndex >= 0)
            {
                collection = _layersAbovePipe;
                index = aboveIndex;
            }
            else
            {
                var belowIndex = _layersBelowPipe.FindIndex(l => l.Id == editLayer.LayerId);
                if (belowIndex < 0)
                {
                    return new ConstructionMutationResult(
                        ConstructionMutationStatus.Rejected,
                        origin,
                        oldSnapshot,
                        oldSnapshot,
                        "LayerNotFound");
                }

                collection = _layersBelowPipe;
                index = belowIndex;
            }

            var existing = collection[index];
            var updated = existing with
            {
                MaterialId = editLayer.MaterialId,
                MaterialName = editLayer.MaterialName,
                Thickness = editLayer.Thickness,
                CalculatedLambda = editLayer.CalculatedLambda,
                IsLambdaOverridden = editLayer.IsLambdaOverridden
            };

            if (updated.Equals(existing))
            {
                return new ConstructionMutationResult(
                    ConstructionMutationStatus.NoChange,
                    origin,
                    oldSnapshot,
                    oldSnapshot);
            }

            collection[index] = updated;
            return CompleteChanged(oldSnapshot, origin);
        }

        private ConstructionMutationResult ApplyReorder(
            ConstructionStateSnapshot oldSnapshot,
            ConstructionMutationOrigin origin,
            ConstructionMutation.ReorderLayers reorder)
        {
            var target = reorder.Position == Models.Construction.LayerPosition.AbovePipe
                ? _layersAbovePipe
                : _layersBelowPipe;

            var currentIds = target.Select(l => l.Id).ToArray();
            var requestedIds = reorder.OrderedLayerIds ?? Array.Empty<Guid>();

            if (currentIds.Length != requestedIds.Length
                || !new HashSet<Guid>(currentIds).SetEquals(requestedIds))
            {
                return new ConstructionMutationResult(
                    ConstructionMutationStatus.Rejected,
                    origin,
                    oldSnapshot,
                    oldSnapshot,
                    "ReorderNotAPermutation");
            }

            var byId = target.ToDictionary(l => l.Id);
            var reordered = requestedIds.Select(id => byId[id]).ToList();

            var unchanged = true;
            for (var i = 0; i < currentIds.Length; i++)
            {
                if (currentIds[i] != requestedIds[i])
                {
                    unchanged = false;
                    break;
                }
            }

            if (unchanged)
            {
                return new ConstructionMutationResult(
                    ConstructionMutationStatus.NoChange,
                    origin,
                    oldSnapshot,
                    oldSnapshot);
            }

            if (reorder.Position == Models.Construction.LayerPosition.AbovePipe)
            {
                _layersAbovePipe = reordered;
            }
            else
            {
                _layersBelowPipe = reordered;
            }

            Reindex();
            return CompleteChanged(oldSnapshot, origin);
        }

        private ConstructionMutationResult ApplyClearLayers(
            ConstructionStateSnapshot oldSnapshot,
            ConstructionMutationOrigin origin)
        {
            if (_layersAbovePipe.Count == 0 && _layersBelowPipe.Count == 0)
            {
                return new ConstructionMutationResult(
                    ConstructionMutationStatus.NoChange,
                    origin,
                    oldSnapshot,
                    oldSnapshot);
            }

            _layersAbovePipe.Clear();
            _layersBelowPipe.Clear();

            return CompleteChanged(oldSnapshot, origin);
        }

        private void Reindex()
        {
            for (var i = 0; i < _layersAbovePipe.Count; i++)
            {
                _layersAbovePipe[i] = _layersAbovePipe[i] with { Order = i, Position = Models.Construction.LayerPosition.AbovePipe };
            }

            for (var i = 0; i < _layersBelowPipe.Count; i++)
            {
                _layersBelowPipe[i] = _layersBelowPipe[i] with { Order = i, Position = Models.Construction.LayerPosition.BelowPipe };
            }
        }

        private void CommitSnapshot(ConstructionStateSnapshot normalized)
        {
            _groundwaterLevel = normalized.GroundwaterLevel;
            _hasLoads = normalized.HasLoads;
            _layersAbovePipe = normalized.LayersAbovePipe.ToList();
            _layersBelowPipe = normalized.LayersBelowPipe.ToList();
        }

        private static ConstructionStateSnapshot Normalize(ConstructionStateSnapshot candidate)
        {
            var above = candidate.LayersAbovePipe
                .Select((l, i) => l with { Order = i, Position = Models.Construction.LayerPosition.AbovePipe })
                .ToArray();

            var below = candidate.LayersBelowPipe
                .Select((l, i) => l with { Order = i, Position = Models.Construction.LayerPosition.BelowPipe })
                .ToArray();

            return new ConstructionStateSnapshot(candidate.GroundwaterLevel, candidate.HasLoads, above, below);
        }

        private static bool TryValidate(ConstructionStateSnapshot candidate, out string? errorCode)
        {
            var allIds = candidate.LayersAbovePipe.Select(l => l.Id)
                .Concat(candidate.LayersBelowPipe.Select(l => l.Id))
                .ToList();

            if (allIds.Count != allIds.Distinct().Count())
            {
                errorCode = "DuplicateLayerId";
                return false;
            }

            if (allIds.Any(id => id == Guid.Empty))
            {
                errorCode = "EmptyLayerId";
                return false;
            }

            errorCode = null;
            return true;
        }

        private ConstructionMutationResult CompleteChanged(ConstructionStateSnapshot oldSnapshot, ConstructionMutationOrigin origin)
        {
            var newSnapshot = Snapshot;

            _projection.Update(newSnapshot);

            if (_projection.IsValid && PublishesDownstream(origin))
            {
                _projection.RaiseDataChanged();
                _calculationContext?.UpdateConstruction(_projection, "ConstructionState");
            }

            Changed?.Invoke(this, new ConstructionStateChangedEventArgs(origin, oldSnapshot, newSnapshot));

            if (origin == ConstructionMutationOrigin.User || origin == ConstructionMutationOrigin.Template)
            {
                _markDirtyService?.MarkDirty();
            }

            return new ConstructionMutationResult(
                ConstructionMutationStatus.Changed,
                origin,
                oldSnapshot,
                newSnapshot);
        }

        private static bool PublishesDownstream(ConstructionMutationOrigin origin)
        {
            return origin == ConstructionMutationOrigin.User
                || origin == ConstructionMutationOrigin.Template
                || origin == ConstructionMutationOrigin.FileLoad;
        }
    }
}
