using System.ComponentModel;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Phase 9 test support: the legacy forwarding adapter over
    /// <see cref="IProjectSession"/> moved out of production (slice 6) and is
    /// kept here only as a test seam. Behavior is identical to the removed
    /// production class: no lifecycle state of its own; all reads and writes
    /// delegate to the canonical session instance. Implements the internal
    /// <see cref="IMarkDirtyService"/> seam exactly as before.
    /// </summary>
    public class ProjectStateService : IMarkDirtyService
    {
        private readonly IProjectSession _session;

        public ProjectStateService()
            : this(new ProjectSession())
        {
        }

        public ProjectStateService(IProjectSession session)
        {
            _session = session;
            _session.PropertyChanged += OnSessionPropertyChanged;
        }

        /// <summary>
        /// Каноническая сессия проекта, к которой делегируется состояние.
        /// </summary>
        public IProjectSession Session => _session;

        public string ProjectNumber
        {
            get => _session.ProjectNumber;
            set => _session.ProjectNumber = value;
        }

        public string ProjectObject
        {
            get => _session.ProjectObject;
            set => _session.ProjectObject = value;
        }

        public string? CurrentFilePath
        {
            get => _session.CurrentFilePath;
            set => _session.CurrentFilePath = value;
        }

        public bool IsDirty => _session.IsDirty;

        public void MarkDirty() => _session.MarkDirty();

        public void MarkClean() => _session.MarkClean();

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
}
