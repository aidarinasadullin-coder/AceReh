namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Narrow, persistence-only source for the display mode that is not owned
    /// by a project-state slice. DEC-006 (2026-09-03): catalogs live only
    /// globally, so the persistence seam no longer exposes materials or
    /// templates and must never expose a ViewModel to the snapshot boundary.
    /// </summary>
    public interface IProjectSnapshotPersistenceInputs
    {
        bool IsOperatingMode { get; }
    }
}
