namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Immutable, self-consistent snapshot of everything a project save needs:
    /// project identity/mode plus the four canonical module state snapshots.
    /// Deliberately excludes paths, dirty flags, restore guards, dates and any
    /// transient UI/service state; later save tasks receive dates explicitly.
    /// DEC-006 (2026-09-03): catalogs live only globally — custom
    /// materials/templates are no longer carried by the save snapshot or the
    /// .smc wire format.
    /// </summary>
    public sealed class ProjectSnapshot
    {
        public string ProjectNumber { get; }
        public string ProjectObject { get; }
        public bool IsOperatingMode { get; }
        public ClimateStateSnapshot ClimateStateSnapshot { get; }
        public ConstructionStateSnapshot ConstructionStateSnapshot { get; }
        public ThermalStateSnapshot ThermalStateSnapshot { get; }
        public HydraulicsStateSnapshot HydraulicsStateSnapshot { get; }

        public ProjectSnapshot(
            string? projectNumber,
            string? projectObject,
            bool isOperatingMode,
            ClimateStateSnapshot? climateStateSnapshot,
            ConstructionStateSnapshot? constructionStateSnapshot,
            ThermalStateSnapshot? thermalStateSnapshot,
            HydraulicsStateSnapshot? hydraulicsStateSnapshot)
        {
            ProjectNumber = projectNumber ?? throw new ArgumentNullException(nameof(projectNumber));
            ProjectObject = projectObject ?? throw new ArgumentNullException(nameof(projectObject));
            IsOperatingMode = isOperatingMode;
            ClimateStateSnapshot = climateStateSnapshot ?? throw new ArgumentNullException(nameof(climateStateSnapshot));
            ConstructionStateSnapshot = constructionStateSnapshot ?? throw new ArgumentNullException(nameof(constructionStateSnapshot));
            ThermalStateSnapshot = thermalStateSnapshot ?? throw new ArgumentNullException(nameof(thermalStateSnapshot));
            HydraulicsStateSnapshot = hydraulicsStateSnapshot ?? throw new ArgumentNullException(nameof(hydraulicsStateSnapshot));
        }
    }
}
