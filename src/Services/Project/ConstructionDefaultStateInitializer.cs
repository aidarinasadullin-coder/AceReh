using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Services.Project
{
    public sealed class ConstructionDefaultStateInitializer
    {
        /// <summary>
        /// Заводской УГВ нового расчёта («сухие условия»): жизненный цикл
        /// нового расчёта и сброса перед загрузкой не наследует УГВ
        /// предыдущего проекта (план 2026-09-04, D1).
        /// </summary>
        public const double DefaultGroundwaterLevel = 2.0;

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

        /// <summary>
        /// Применить канонические дефолты с заводским УГВ.
        /// </summary>
        public ConstructionMutationResult Apply(ConstructionMutationOrigin origin)
        {
            return Apply(DefaultGroundwaterLevel, origin);
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
                    CreateLayer(materialsById[5], 100.0, LayerPosition.AbovePipe, 0, groundwaterLevel)
                },
                new[]
                {
                    CreateLayer(materialsById[5], 10.0, LayerPosition.BelowPipe, 0, groundwaterLevel),
                    CreateLayer(materialsById[6], 10.0, LayerPosition.BelowPipe, 1, groundwaterLevel),
                    CreateLayer(materialsById[10], 80.0, LayerPosition.BelowPipe, 2, groundwaterLevel),
                    CreateLayer(materialsById[13], 200.0, LayerPosition.BelowPipe, 3, groundwaterLevel),
                    CreateLayer(materialsById[2], 1000.0, LayerPosition.BelowPipe, 4, groundwaterLevel),
                    CreateLayer(materialsById[2], 570.0, LayerPosition.BelowPipe, 5, groundwaterLevel)
                });

            return _constructionState.ResetToDefaults(defaults, origin);
        }

        private static ConstructionLayerSnapshot CreateLayer(
            Material material,
            double thickness,
            LayerPosition position,
            int order,
            double groundwaterLevel)
        {
            // Семантика Layer.UpdateLambda: λА над трубой всегда; под трубой
            // λБ при УГВ < 1 м, иначе λА — дефолты согласованы с УГВ (D6).
            var lambda = position == LayerPosition.AbovePipe || groundwaterLevel >= 1.0
                ? material.LambdaA
                : material.LambdaB;

            return new ConstructionLayerSnapshot(
                Guid.NewGuid(),
                material.Id,
                material.Name,
                thickness,
                lambda,
                false,
                position,
                order);
        }
    }
}
