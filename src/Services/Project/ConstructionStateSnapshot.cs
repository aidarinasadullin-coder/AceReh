using System;
using System.Collections.Generic;
using System.Linq;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Immutable snapshot of the canonical Construction project state.
    /// Equality is explicit structural equality: scalars plus ordered
    /// sequence equality of both layer collections, field-by-field.
    /// Do NOT use default IReadOnlyList&lt;T&gt; reference equality.
    /// </summary>
    public sealed class ConstructionStateSnapshot : IEquatable<ConstructionStateSnapshot>
    {
        public double GroundwaterLevel { get; }
        public bool HasLoads { get; }
        public IReadOnlyList<ConstructionLayerSnapshot> LayersAbovePipe { get; }
        public IReadOnlyList<ConstructionLayerSnapshot> LayersBelowPipe { get; }

        public ConstructionStateSnapshot(
            double groundwaterLevel,
            bool hasLoads,
            IReadOnlyList<ConstructionLayerSnapshot> layersAbovePipe,
            IReadOnlyList<ConstructionLayerSnapshot> layersBelowPipe)
        {
            GroundwaterLevel = groundwaterLevel;
            HasLoads = hasLoads;
            LayersAbovePipe = layersAbovePipe ?? throw new ArgumentNullException(nameof(layersAbovePipe));
            LayersBelowPipe = layersBelowPipe ?? throw new ArgumentNullException(nameof(layersBelowPipe));
        }

        /// <summary>
        /// Structural equality: scalars and both layer sequences, in order.
        /// </summary>
        public bool Equals(ConstructionStateSnapshot? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Math.Abs(GroundwaterLevel - other.GroundwaterLevel) < 1e-10
                && HasLoads == other.HasLoads
                && LayersAbovePipe.SequenceEqual(other.LayersAbovePipe)
                && LayersBelowPipe.SequenceEqual(other.LayersBelowPipe);
        }

        public override bool Equals(object? obj) => obj is ConstructionStateSnapshot other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(GroundwaterLevel);
            hash.Add(HasLoads);
            foreach (var l in LayersAbovePipe) hash.Add(l);
            foreach (var l in LayersBelowPipe) hash.Add(l);
            return hash.ToHashCode();
        }

        public static bool operator ==(ConstructionStateSnapshot? left, ConstructionStateSnapshot? right)
            => left?.Equals(right) ?? right is null;

        public static bool operator !=(ConstructionStateSnapshot? left, ConstructionStateSnapshot? right)
            => !(left == right);
    }
}
