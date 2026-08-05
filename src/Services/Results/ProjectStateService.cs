using System.ComponentModel;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Forwarding-only compatibility adapter over <see cref="IProjectSession"/>.
    /// Holds no lifecycle state of its own; all reads and writes are delegated to
    /// the canonical session instance.
    /// </summary>
    public class ProjectStateService : IProjectStateService, IMarkDirtyService
    {
        private readonly IProjectSession _session;

        /// <summary>
        /// Creates a new adapter backed by its own <see cref="ProjectSession"/>.
        /// </summary>
        public ProjectStateService()
            : this(new ProjectSession())
        {
        }

        /// <summary>
        /// Creates an adapter that forwards to the specified canonical session.
        /// </summary>
        public ProjectStateService(IProjectSession session)
        {
            _session = session;
            _session.PropertyChanged += OnSessionPropertyChanged;
        }

        /// <summary>
        /// Каноническая сессия проекта, к которой делегируется состояние.
        /// </summary>
        public IProjectSession Session => _session;

        /// <inheritdoc />
        public string ProjectNumber
        {
            get => _session.ProjectNumber;
            set => _session.ProjectNumber = value;
        }

        /// <inheritdoc />
        public string ProjectObject
        {
            get => _session.ProjectObject;
            set => _session.ProjectObject = value;
        }

        /// <inheritdoc />
        public string? CurrentFilePath
        {
            get => _session.CurrentFilePath;
            set => _session.CurrentFilePath = value;
        }

        /// <inheritdoc />
        public bool IsDirty => _session.IsDirty;

        /// <inheritdoc />
        public void MarkDirty() => _session.MarkDirty();

        /// <inheritdoc />
        public void MarkClean() => _session.MarkClean();

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
}
