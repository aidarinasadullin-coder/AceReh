using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Repository-backed persistence inputs without a ViewModel dependency.
    /// </summary>
    public sealed class ProjectSnapshotPersistenceInputs : IProjectSnapshotPersistenceInputs
    {
        private readonly IProjectDisplayModeState _displayModeState;
        private readonly IMaterialRepository _materialRepository;
        private readonly IConstructionTemplateRepository _templateRepository;

        public ProjectSnapshotPersistenceInputs(
            IProjectDisplayModeState displayModeState,
            IMaterialRepository materialRepository,
            IConstructionTemplateRepository templateRepository)
        {
            _displayModeState = displayModeState ?? throw new ArgumentNullException(nameof(displayModeState));
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        }

        public bool IsOperatingMode => _displayModeState.IsOperatingMode;

        public IReadOnlyList<Material> Materials => _materialRepository.GetAllMaterials().ToList();

        public IReadOnlyList<ConstructionTemplate> Templates =>
            _templateRepository.GetAllAsync().GetAwaiter().GetResult().ToList();
    }
}
