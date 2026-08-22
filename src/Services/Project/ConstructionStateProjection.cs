using System;
using System.Linq;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Read-only <see cref="IConstructionData"/> projection produced from a
    /// <see cref="ConstructionStateSnapshot"/>. Created by
    /// <see cref="ProjectSessionConstructionState"/> after every successful mutation;
    /// never externally writable.
    ///
    /// Formulae mirror the current <c>Construction</c> model exactly:
    ///   R = Thickness / CalculatedLambda / 1000  (per layer, in m²·К/Вт)
    ///   LambdaE = CalculatedLambda of the last above-pipe layer (nearest to pipe) ?? 1.6
    /// Above-pipe layers always use λA regardless of groundwater, so CalculatedLambda
    /// is equivalent to Material.LambdaA for that collection.
    /// </summary>
    public sealed class ConstructionStateProjection : IConstructionData
    {
        private ConstructionStateSnapshot _snapshot;

        public ConstructionStateProjection(ConstructionStateSnapshot snapshot)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        /// <inheritdoc />
        /// <remarks>
        /// R1 = Σ (Thickness_i / CalculatedLambda_i / 1000) for all layers above pipe.
        /// </remarks>
        public double R1Total => _snapshot.LayersAbovePipe
            .Sum(l => l.CalculatedLambda > 0 ? l.Thickness / l.CalculatedLambda / 1000.0 : 0.0);

        /// <inheritdoc />
        /// <remarks>
        /// R2 = Σ (Thickness_i / CalculatedLambda_i / 1000) for all layers below pipe.
        /// </remarks>
        public double R2Total => _snapshot.LayersBelowPipe
            .Sum(l => l.CalculatedLambda > 0 ? l.Thickness / l.CalculatedLambda / 1000.0 : 0.0);

        /// <inheritdoc />
        /// <remarks>
        /// LambdaE = CalculatedLambda of the last above-pipe layer (nearest to pipe) or 1.6.
        /// Above-pipe layers always use λA (never λB), making CalculatedLambda equivalent
        /// to Material.LambdaA for these layers.
        /// </remarks>
        public double LambdaE => _snapshot.LayersAbovePipe.LastOrDefault()?.CalculatedLambda ?? 1.6;

        /// <inheritdoc />
        public bool IsValid => HasValidLayers()
            && _snapshot.GroundwaterLevel >= 0.0
            && _snapshot.GroundwaterLevel <= 10.0;

        /// <inheritdoc />
        public event EventHandler<ConstructionDataChangedEventArgs>? DataChanged;

        /// <summary>
        /// Raises <see cref="DataChanged"/> so downstream consumers (e.g. CalculationContext)
        /// can refresh when the canonical state changes. Called by the owner state after
        /// committing a snapshot replacement.
        /// </summary>
        internal void RaiseDataChanged()
        {
            DataChanged?.Invoke(this, new ConstructionDataChangedEventArgs
            {
                ChangedProperty = "Construction",
                IsValid = IsValid
            });
        }

        internal void Update(ConstructionStateSnapshot snapshot)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        private bool HasValidLayers()
        {
            var layers = _snapshot.LayersAbovePipe.Concat(_snapshot.LayersBelowPipe).ToArray();
            if (layers.Length == 0 || layers.Any(layer => layer.Thickness > 1000.0))
            {
                return false;
            }

            if (_snapshot.LayersAbovePipe.Count == 0)
            {
                return true;
            }

            var minimumAboveThickness = _snapshot.HasLoads ? 50.0 : 40.0;
            return _snapshot.LayersAbovePipe.Sum(layer => layer.Thickness) >= minimumAboveThickness;
        }
    }
}
