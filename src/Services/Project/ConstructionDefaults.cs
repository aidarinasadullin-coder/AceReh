using System;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Default layer recipe passed to ResetToDefaults.
    /// Produced at the application boundary; canonical state never accesses catalogs directly.
    /// </summary>
    public sealed class ConstructionDefaults
    {
        public double GroundwaterLevel { get; }
        public IReadOnlyList<ConstructionLayerSnapshot> LayersAbovePipe { get; }
        public IReadOnlyList<ConstructionLayerSnapshot> LayersBelowPipe { get; }

        public ConstructionDefaults(
            double groundwaterLevel,
            IReadOnlyList<ConstructionLayerSnapshot> layersAbovePipe,
            IReadOnlyList<ConstructionLayerSnapshot> layersBelowPipe)
        {
            GroundwaterLevel = groundwaterLevel;
            LayersAbovePipe = layersAbovePipe ?? throw new ArgumentNullException(nameof(layersAbovePipe));
            LayersBelowPipe = layersBelowPipe ?? throw new ArgumentNullException(nameof(layersBelowPipe));
        }
    }
}
