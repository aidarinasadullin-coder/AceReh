using System;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Pure ProjectSnapshot-to-ProjectData mapper. It owns no lifecycle, file,
    /// dirty, WPF or Results state and preserves the existing Version 1.1 DTO.
    /// DEC-006 (2026-09-03): custom catalogs live only globally — the mapper
    /// no longer carries custom catalog records into the wire DTO.
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
                ConstructionData = ConstructionPersistenceMapper.ToProjectData(
                    snapshot.ConstructionStateSnapshot,
                    materialRepository),
                ThermalData = ThermalPersistenceMapper.BuildThermalProjectData(
                    snapshot.ThermalStateSnapshot),
                HydraulicsData = HydraulicsPersistenceMapper.BuildHydraulicsProjectData(
                    snapshot.HydraulicsStateSnapshot)
            };
        }
    }
}
