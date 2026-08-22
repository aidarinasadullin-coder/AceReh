using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Services.Project
{
    public sealed class ConstructionDefaultStateInitializer
    {
        private static readonly int[] RequiredMaterialIds = { 2, 5, 6, 10, 13 };

        private readonly IMaterialRepository _materialRepository;
        private readonly IProjectSessionConstructionState _constructionState;

        public ConstructionDefaultStateInitializer(
            IMaterialRepository materialRepository,
            IProjectSessionConstructionState constructionState)
        {
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _constructionState = constructionState ?? throw new ArgumentNullException(nameof(constructionState));
        }

        public ConstructionMutationResult Apply(
            double groundwaterLevel,
            ConstructionMutationOrigin origin)
        {
            var materials = RequiredMaterialIds
                .Select(id => (Id: id, Material: _materialRepository.GetMaterialById(id)))
                .ToArray();
            var missingMaterialIds = materials
                .Where(entry => entry.Material is null)
                .Select(entry => entry.Id)
                .ToArray();

            if (missingMaterialIds.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Required default construction materials are missing: {string.Join(", ", missingMaterialIds)}.");
            }

            var materialsById = materials.ToDictionary(entry => entry.Id, entry => entry.Material!);
            var defaults = new ConstructionDefaults(
                groundwaterLevel,
                new[]
                {
                    CreateLayer(materialsById[5], 100.0, LayerPosition.AbovePipe, 0)
                },
                new[]
                {
                    CreateLayer(materialsById[5], 10.0, LayerPosition.BelowPipe, 0),
                    CreateLayer(materialsById[6], 10.0, LayerPosition.BelowPipe, 1),
                    CreateLayer(materialsById[10], 80.0, LayerPosition.BelowPipe, 2),
                    CreateLayer(materialsById[13], 200.0, LayerPosition.BelowPipe, 3),
                    CreateLayer(materialsById[2], 1000.0, LayerPosition.BelowPipe, 4),
                    CreateLayer(materialsById[2], 570.0, LayerPosition.BelowPipe, 5)
                });

            return _constructionState.ResetToDefaults(defaults, origin);
        }

        private static ConstructionLayerSnapshot CreateLayer(
            Material material,
            double thickness,
            LayerPosition position,
            int order)
        {
            return new ConstructionLayerSnapshot(
                Guid.NewGuid(),
                material.Id,
                material.Name,
                thickness,
                material.LambdaA,
                false,
                position,
                order);
        }
    }
}
