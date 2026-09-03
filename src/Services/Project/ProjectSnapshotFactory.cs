using System;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Assembles one save snapshot from the aggregate root. Every canonical
    /// identity/module property is read once per assembly.
    /// DEC-006 (2026-09-03): catalogs live only globally — the factory no
    /// longer reads materials/templates into the snapshot.
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

            return new ProjectSnapshot(
                projectNumber,
                projectObject,
                isOperatingMode,
                climateSnapshot,
                constructionSnapshot,
                thermalSnapshot,
                hydraulicsSnapshot);
        }
    }
}
