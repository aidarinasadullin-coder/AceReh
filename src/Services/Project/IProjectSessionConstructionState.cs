using System;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Canonical Construction project state slice owned by <see cref="ProjectSession"/>.
    /// Single writable canonical owner of GroundwaterLevel, ordered
    /// LayersAbovePipe and ordered LayersBelowPipe.
    /// </summary>
    public interface IProjectSessionConstructionState
    {
        /// <summary>Current immutable snapshot of the canonical Construction state.</summary>
        ConstructionStateSnapshot Snapshot { get; }

        /// <summary>Stable read-only projection consumed by thermal-facing services.</summary>
        IConstructionData CurrentProjection { get; }

        /// <summary>
        /// Raised exactly once per changed logical mutation, containing origin and before/after snapshots.
        /// Not raised for NoChange, Rejected or Cancelled results.
        /// </summary>
        event EventHandler<ConstructionStateChangedEventArgs> Changed;

        /// <summary>
        /// Apply one closed-family mutation command (scalar edit, add, remove, edit,
        /// reorder). Returns Changed, NoChange or Rejected.
        /// </summary>
        ConstructionMutationResult Apply(ConstructionMutation mutation, ConstructionMutationOrigin origin);

        /// <summary>
        /// Apply a full candidate snapshot, detecting no-op via structural equality.
        /// Returns Changed, NoChange or Rejected.
        /// </summary>
        ConstructionMutationResult ApplySnapshot(ConstructionStateSnapshot candidate, ConstructionMutationOrigin origin);

        /// <summary>
        /// Reset to the provided default recipe. Returns Changed or NoChange.
        /// </summary>
        ConstructionMutationResult ResetToDefaults(ConstructionDefaults defaults, ConstructionMutationOrigin origin);
    }
}
