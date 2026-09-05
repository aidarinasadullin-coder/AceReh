using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Pure mapper: projects a canonical <see cref="ConstructionStateSnapshot"/>
    /// to the .smc <see cref="ConstructionProjectData"/> DTO without changing
    /// wire schema, version, or field semantics. R1/R2/LambdaE are derived
    /// through <see cref="ConstructionStateProjection"/> to avoid duplicating
    /// formulae. <see cref="LayerProjectData.MaterialLambda"/> is resolved from
    /// the material catalog by <see cref="ConstructionLayerSnapshot.MaterialId"/>
    /// to preserve the denormalized value written by the legacy save path.
    /// </summary>
    public static class ConstructionPersistenceMapper
    {
        public static ConstructionProjectData ToProjectData(
            ConstructionStateSnapshot snapshot,
            IMaterialRepository materialRepository)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            ArgumentNullException.ThrowIfNull(materialRepository);

            var projection = new ConstructionStateProjection(snapshot);

            var layers = snapshot.LayersAbovePipe
                .Select(l => ToLayerProjectData(l, materialRepository))
                .Concat(snapshot.LayersBelowPipe
                    .Select(l => ToLayerProjectData(l, materialRepository)))
                .ToList();

            return new ConstructionProjectData
            {
                R1 = projection.R1Total,
                R2 = projection.R2Total,
                LambdaE = projection.LambdaE,
                GroundwaterLevel = snapshot.GroundwaterLevel,
                Layers = layers
            };
        }

        private static LayerProjectData ToLayerProjectData(
            ConstructionLayerSnapshot snapshot,
            IMaterialRepository materialRepository)
        {
            var material = materialRepository.GetMaterialById(snapshot.MaterialId);
            return new LayerProjectData
            {
                Position = snapshot.Position,
                MaterialName = snapshot.MaterialName,
                MaterialLambda = material?.LambdaA ?? 0,
                Thickness = snapshot.Thickness,
                CalculatedLambda = snapshot.CalculatedLambda,
                IsLambdaOverridden = snapshot.IsLambdaOverridden,
                Order = snapshot.Order
            };
        }
    }
}
