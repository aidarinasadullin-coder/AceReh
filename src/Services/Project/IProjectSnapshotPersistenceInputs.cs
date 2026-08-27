using System.Collections.Generic;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Narrow, persistence-only source for the records that are not owned by a
    /// project-state slice. Implementations may adapt a repository-backed
    /// catalog, but must not expose a ViewModel to the snapshot boundary.
    /// </summary>
    public interface IProjectSnapshotPersistenceInputs
    {
        bool IsOperatingMode { get; }
        IReadOnlyList<Material> Materials { get; }
        IReadOnlyList<ConstructionTemplate> Templates { get; }
    }
}
