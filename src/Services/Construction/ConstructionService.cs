using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Сервис для работы с конструкциями
    /// </summary>
    public class ConstructionService : IConstructionService
    {
        private readonly IValidator<ConstructionModel> _validator;

        /// <summary>
        /// Создать сервис
        /// </summary>
        /// <param name="validator">Валидатор конструкции</param>
        public ConstructionService(IValidator<ConstructionModel> validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Рассчитать термические сопротивления для всех слоёв конструкции
        /// </summary>
        public void CalculateThermalResistances(ConstructionModel construction)
        {
            ArgumentNullException.ThrowIfNull(construction, nameof(construction));

            // Обновляем λ для всех слоёв в зависимости от УГВ
            foreach (var layer in construction.LayersAbovePipe)
            {
                layer.UpdateLambda(construction.GroundwaterLevel);
            }

            foreach (var layer in construction.Layers)
            {
                layer.UpdateLambda(construction.GroundwaterLevel);
            }
        }

        /// <summary>
        /// Рассчитать суммарное термическое сопротивление слоёв над трубой (R1)
        /// </summary>
        /// <remarks>
        /// Формула: R1 = Σ(d_i / λ_i / 1000), где d_i - толщина слоя в мм, λ_i - теплопроводность
        /// </remarks>
        public double CalculateR1(IEnumerable<Layer> layersAbovePipe)
        {
            ArgumentNullException.ThrowIfNull(layersAbovePipe, nameof(layersAbovePipe));

            return layersAbovePipe.Sum(layer =>
            {
                if (layer.CalculatedLambda <= 0)
                {
                    throw new InvalidOperationException(
                        $"Теплопроводность слоя '{layer.Material?.Name ?? "Не указан"}' должна быть положительной");
                }

                // R = d / λ / 1000 (толщина в мм, переводим в метры)
                return layer.Thickness / layer.CalculatedLambda / 1000.0;
            });
        }

        /// <summary>
        /// Рассчитать суммарное термическое сопротивление слоёв под трубой (R2)
        /// </summary>
        /// <remarks>
        /// Формула: R2 = Σ(d_i / λ_i / 1000), где d_i - толщина слоя в мм, λ_i - теплопроводность
        /// При УГВ < 1м используется λБ, иначе λА
        /// </remarks>
        public double CalculateR2(IEnumerable<Layer> layersBelowPipe, double groundwaterLevel)
        {
            ArgumentNullException.ThrowIfNull(layersBelowPipe, nameof(layersBelowPipe));

            if (groundwaterLevel < 0)
            {
                throw new ArgumentException("Уровень грунтовых вод не может быть отрицательным", nameof(groundwaterLevel));
            }

            return layersBelowPipe.Sum(layer =>
            {
                // Обновляем λ в зависимости от УГВ
                layer.UpdateLambda(groundwaterLevel);

                if (layer.CalculatedLambda <= 0)
                {
                    throw new InvalidOperationException(
                        $"Теплопроводность слоя '{layer.Material?.Name ?? "Не указан"}' должна быть положительной");
                }

                // R = d / λ / 1000 (толщина в мм, переводим в метры)
                return layer.Thickness / layer.CalculatedLambda / 1000.0;
            });
        }

        /// <summary>
        /// Получить теплопроводность материала вокруг трубы (LambdaE)
        /// </summary>
        /// <remarks>
        /// LambdaE = λ материала первого слоя над трубой (стяжки/бетона вокруг трубы)
        /// Если слой не указан, используется значение по умолчанию 1.6 Вт/м·К
        /// </remarks>
        public double GetLambdaE(Layer? firstLayerAbovePipe)
        {
            if (firstLayerAbovePipe == null)
            {
                // Значение по умолчанию для бетона
                return 1.6;
            }

            if (firstLayerAbovePipe.Material == null)
            {
                throw new InvalidOperationException("Материал слоя не указан");
            }

            // Для слоёв над трубой всегда используем λА
            return firstLayerAbovePipe.Material.LambdaA;
        }

        /// <summary>
        /// Валидация конструкции
        /// </summary>
        public ValidationResult ValidateConstruction(ConstructionModel construction)
        {
            ArgumentNullException.ThrowIfNull(construction, nameof(construction));

            return _validator.Validate(construction);
        }

        /// <summary>
        /// Создать конструкцию из шаблона
        /// </summary>
        public ConstructionModel CreateFromTemplate(ConstructionTemplate template, IEnumerable<Material> materials)
        {
            ArgumentNullException.ThrowIfNull(template, nameof(template));
            ArgumentNullException.ThrowIfNull(materials, nameof(materials));

            var materialsList = materials.ToList();
            var construction = new ConstructionModel
            {
                GroundwaterLevel = template.DefaultGroundwaterLevel,
                HasLoads = template.HasLoads
            };

            // Добавляем слои над трубой
            foreach (var layerTemplate in template.LayersAbovePipe.OrderBy(l => l.Order))
            {
                var material = materialsList.FirstOrDefault(m => m.Id == layerTemplate.MaterialId);
                if (material == null)
                {
                    throw new InvalidOperationException(
                        $"Материал с идентификатором {layerTemplate.MaterialId} не найден");
                }

                construction.AddLayerAbovePipe(material, layerTemplate.Thickness);
            }

            // Добавляем слои под трубой
            foreach (var layerTemplate in template.LayersBelowPipe.OrderBy(l => l.Order))
            {
                var material = materialsList.FirstOrDefault(m => m.Id == layerTemplate.MaterialId);
                if (material == null)
                {
                    throw new InvalidOperationException(
                        $"Материал с идентификатором {layerTemplate.MaterialId} не найден");
                }

                construction.AddLayerBelowPipe(material, layerTemplate.Thickness);
            }

            // Рассчитываем термические сопротивления
            CalculateThermalResistances(construction);

            return construction;
        }

        /// <summary>
        /// Получить общую толщину слоёв над трубой
        /// </summary>
        public double GetTotalThicknessAbovePipe(ConstructionModel construction)
        {
            ArgumentNullException.ThrowIfNull(construction, nameof(construction));

            return construction.LayersAbovePipe.Sum(l => l.Thickness);
        }

        /// <summary>
        /// Получить общую толщину слоёв под трубой
        /// </summary>
        public double GetTotalThicknessBelowPipe(ConstructionModel construction)
        {
            ArgumentNullException.ThrowIfNull(construction, nameof(construction));

            return construction.Layers
                .Where(l => l.Position == LayerPosition.BelowPipe)
                .Sum(l => l.Thickness);
        }
    }
}