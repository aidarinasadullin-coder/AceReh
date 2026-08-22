using System;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Closed, exhaustive command family for canonical Construction mutations.
    /// Not a bag of nullable fields: each concrete case carries only the data
    /// its operation needs. <see cref="ProjectSessionConstructionState.Apply"/>
    /// pattern-matches on the concrete type.
    /// </summary>
    public abstract record ConstructionMutation
    {
        private ConstructionMutation() { }

        /// <summary>Set GroundwaterLevel to an explicit value.</summary>
        public sealed record SetGroundwaterLevel(double Value) : ConstructionMutation;

        /// <summary>Set HasLoads to an explicit value.</summary>
        public sealed record SetHasLoads(bool Value) : ConstructionMutation;

        /// <summary>Add a new layer with a freshly generated Id to the given position.</summary>
        public sealed record AddLayer(
            SnowMeltingCalculator.Models.Construction.LayerPosition Position,
            int MaterialId,
            string MaterialName,
            double Thickness,
            double CalculatedLambda,
            bool IsLambdaOverridden) : ConstructionMutation;

        /// <summary>Remove the layer with the given stable Id from either collection.</summary>
        public sealed record RemoveLayer(Guid LayerId) : ConstructionMutation;

        /// <summary>
        /// Replace one existing layer's editable fields in place, identified by its stable Id.
        /// Id, Position and Order are not editable through this mutation.
        /// </summary>
        public sealed record EditLayer(
            Guid LayerId,
            int MaterialId,
            string MaterialName,
            double Thickness,
            double CalculatedLambda,
            bool IsLambdaOverridden) : ConstructionMutation;

        /// <summary>
        /// Reorder one collection (Above or Below) to the given sequence of layer Ids.
        /// The sequence must be a permutation of the current Ids in that collection.
        /// </summary>
        public sealed record ReorderLayers(
            SnowMeltingCalculator.Models.Construction.LayerPosition Position,
            Guid[] OrderedLayerIds) : ConstructionMutation;

        /// <summary>Remove all layers from both collections.</summary>
        public sealed record ClearLayers() : ConstructionMutation;
    }
}
