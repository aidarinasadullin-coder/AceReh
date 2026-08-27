using System;
using System.Linq;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Pure ProjectSnapshot-to-ProjectData mapper. It owns no lifecycle, file,
    /// dirty, WPF or Results state and preserves the existing Version 1.1 DTO.
    /// </summary>
    public static class ProjectPersistenceMapper
    {
        public static ProjectData ToProjectData(
            ProjectSnapshot snapshot,
            ProjectSaveDates dates,
            IMaterialRepository materialRepository)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            ArgumentNullException.ThrowIfNull(materialRepository);

            var climate = snapshot.ClimateStateSnapshot;
            return new ProjectData
            {
                Version = "1.1",
                ProjectNumber = snapshot.ProjectNumber,
                ProjectObject = snapshot.ProjectObject,
                CreatedDate = dates.CreatedDate,
                ModifiedDate = dates.ModifiedDate,
                IsOperatingMode = snapshot.IsOperatingMode,
                ClimateData = new ClimateProjectData
                {
                    SelectedCity = climate.SelectedCity,
                    Region = climate.SelectedRegion,
                    AirTemperature = climate.AirTemperature,
                    WindSpeed = climate.WindSpeed,
                    Humidity = climate.Humidity,
                    SnowfallIntensity = climate.SnowfallIntensity,
                    SelectedZone = climate.Zone,
                    IsHighRequirements = climate.IsHighRequirements
                },
                CustomMaterials = snapshot.CustomMaterials.Select(ToMaterialSnapshot).ToList(),
                CustomTemplates = snapshot.CustomTemplates.Select(ToTemplate).ToList(),
                ConstructionData = ConstructionPersistenceMapper.ToProjectData(
                    snapshot.ConstructionStateSnapshot,
                    materialRepository),
                ThermalData = ThermalPersistenceMapper.BuildThermalProjectData(
                    snapshot.ThermalStateSnapshot),
                HydraulicsData = HydraulicsPersistenceMapper.BuildHydraulicsProjectData(
                    snapshot.HydraulicsStateSnapshot)
            };
        }

        private static Models.Construction.MaterialSnapshot ToMaterialSnapshot(
            ProjectCustomMaterialRecord material) => new()
        {
            Id = material.Id,
            Name = material.Name,
            Category = material.Category,
            LambdaA = material.LambdaA,
            LambdaB = material.LambdaB,
            MaxSupplyTemp = material.MaxSupplyTemp,
            MinOutdoorTemp = material.MinOutdoorTemp,
            Notes = material.Notes,
            IsBuiltIn = material.IsBuiltIn
        };

        private static Models.Construction.ConstructionTemplate ToTemplate(
            ProjectTemplateRecord template) => new()
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            HasLoads = template.HasLoads,
            DefaultGroundwaterLevel = template.DefaultGroundwaterLevel,
            IsBuiltIn = template.IsBuiltIn,
            LayersAbovePipe = template.LayersAbovePipe.Select(ToLayer).ToList(),
            LayersBelowPipe = template.LayersBelowPipe.Select(ToLayer).ToList(),
            MaterialSnapshots = template.MaterialSnapshots.Select(ToMaterialSnapshot).ToList()
        };

        private static Models.Construction.LayerTemplate ToLayer(
            ProjectTemplateLayerRecord layer) => new()
        {
            MaterialId = layer.MaterialId,
            Thickness = layer.Thickness,
            Position = layer.Position,
            Order = layer.Order
        };
    }
}
