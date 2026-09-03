using System;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Display-mode persistence input without a ViewModel dependency.
    /// DEC-006 (2026-09-03): catalog reads left the persistence seam —
    /// custom materials/templates live only in their global repositories.
    /// </summary>
    public sealed class ProjectSnapshotPersistenceInputs : IProjectSnapshotPersistenceInputs
    {
        private readonly IProjectDisplayModeState _displayModeState;

        public ProjectSnapshotPersistenceInputs(IProjectDisplayModeState displayModeState)
        {
            _displayModeState = displayModeState ?? throw new ArgumentNullException(nameof(displayModeState));
        }

        public bool IsOperatingMode => _displayModeState.IsOperatingMode;
    }
}
