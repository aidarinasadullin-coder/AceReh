using System;
using System.Linq;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Assembles one save snapshot from the aggregate root. Every canonical
    /// identity/module property is read once per assembly; catalog records are
    /// copied through the narrow persistence input contract.
    /// </summary>
    public sealed class ProjectSnapshotFactory : IProjectSnapshotFactory
    {
        private readonly IProjectSnapshotPersistenceInputs _persistenceInputs;

        public ProjectSnapshotFactory(IProjectSnapshotPersistenceInputs persistenceInputs)
        {
            _persistenceInputs = persistenceInputs
                ?? throw new ArgumentNullException(nameof(persistenceInputs));
        }

        public ProjectSnapshot Create(IProjectSession projectSession)
        {
            ArgumentNullException.ThrowIfNull(projectSession);

            var projectNumber = projectSession.ProjectNumber;
            var projectObject = projectSession.ProjectObject;
            var climateSnapshot = projectSession.ClimateState.Snapshot;
            var constructionSnapshot = projectSession.ConstructionState.Snapshot;
            var thermalSnapshot = projectSession.ThermalState.Snapshot;
            var hydraulicsSnapshot = projectSession.HydraulicsState.Snapshot;
            var isOperatingMode = _persistenceInputs.IsOperatingMode;
            var materials = _persistenceInputs.Materials;
            var templates = _persistenceInputs.Templates;

            return new ProjectSnapshot(
                projectNumber,
                projectObject,
                isOperatingMode,
                climateSnapshot,
                constructionSnapshot,
                thermalSnapshot,
                hydraulicsSnapshot,
                materials.Where(material => !material.IsBuiltIn).Select(ToMaterialRecord),
                templates.Where(template => !template.IsBuiltIn)
                    .Select(template => ToTemplateRecord(template, materials)));
        }

        private static ProjectCustomMaterialRecord ToMaterialRecord(Material material) =>
            new(
                material.Id,
                material.Name,
                material.Category,
                material.LambdaA,
                material.LambdaB,
                material.MaxSupplyTemp,
                material.MinOutdoorTemp,
                material.Notes,
                material.IsBuiltIn);

        private static ProjectTemplateRecord ToTemplateRecord(
            ConstructionTemplate template,
            System.Collections.Generic.IReadOnlyList<Material> materials)
        {
            var layerIds = template.LayersAbovePipe
                .Concat(template.LayersBelowPipe)
                .Select(layer => layer.MaterialId)
                .Distinct();

            return new ProjectTemplateRecord(
                template.Id,
                template.Name,
                template.Description,
                template.LayersAbovePipe.Select(ToLayerRecord),
                template.LayersBelowPipe.Select(ToLayerRecord),
                template.HasLoads,
                template.DefaultGroundwaterLevel,
                template.IsBuiltIn,
                layerIds.Select(id => materials.FirstOrDefault(material => material.Id == id))
                    .Where(material => material is not null)
                    .Select(material => ToMaterialRecord(material!)));
        }

        private static ProjectTemplateLayerRecord ToLayerRecord(LayerTemplate layer) =>
            new(layer.MaterialId, layer.Thickness, layer.Position, layer.Order);
    }
}
