using System;
using System.Threading;
using System.Threading.Tasks;
using SnowMeltingCalculator.Core.Results;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Default application save boundary. Creates exactly one
    /// <see cref="ProjectSnapshot"/> per save, maps it once through
    /// <see cref="ProjectPersistenceMapper"/> and delegates file persistence
    /// exactly once to <see cref="IProjectFileService.SaveProjectResultAsync"/>.
    /// Serializer/I/O work, extension normalization and exception-to-failure
    /// conversion stay in the file service; this type owns no lifecycle,
    /// dirty, path or UI state.
    /// </summary>
    public sealed class ProjectSaveService : IProjectSaveService
    {
        private readonly IProjectSnapshotFactory _snapshotFactory;
        private readonly IMaterialRepository _materialRepository;
        private readonly IProjectFileService _fileService;

        public ProjectSaveService(
            IProjectSnapshotFactory snapshotFactory,
            IMaterialRepository materialRepository,
            IProjectFileService fileService)
        {
            _snapshotFactory = snapshotFactory ?? throw new ArgumentNullException(nameof(snapshotFactory));
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        /// <inheritdoc />
        public async Task<OperationResult<object?>> SaveAsync(
            IProjectSession projectSession,
            string filePath,
            ProjectSaveDates dates,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(projectSession);
            ArgumentNullException.ThrowIfNull(filePath);

            var snapshot = _snapshotFactory.Create(projectSession);
            var data = ProjectPersistenceMapper.ToProjectData(snapshot, dates, _materialRepository);

            return await _fileService.SaveProjectResultAsync(filePath, data, cancellationToken);
        }
    }
}
