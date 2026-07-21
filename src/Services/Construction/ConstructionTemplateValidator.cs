using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Валидатор шаблона конструкции
    /// </summary>
    public class ConstructionTemplateValidator : IValidator<ConstructionTemplate>
    {
        /// <summary>
        /// Минимальная длина названия шаблона
        /// </summary>
        private const int MinNameLength = 1;

        /// <summary>
        /// Максимальная длина названия шаблона
        /// </summary>
        private const int MaxNameLength = 100;

        /// <summary>
        /// Минимальная толщина слоя, мм
        /// </summary>
        private const double MinLayerThickness = 0.0;

        /// <summary>
        /// Максимальная толщина слоя, мм
        /// </summary>
        private const double MaxLayerThickness = 1000.0;

        private readonly IMaterialRepository _materialRepository;

        /// <summary>
        /// Создать валидатор шаблона конструкции
        /// </summary>
        /// <param name="materialRepository">Репозиторий материалов для проверки существования материалов</param>
        public ConstructionTemplateValidator(IMaterialRepository materialRepository)
        {
            ArgumentNullException.ThrowIfNull(materialRepository, nameof(materialRepository));
            _materialRepository = materialRepository;
        }

        /// <summary>
        /// Валидировать шаблон конструкции
        /// </summary>
        /// <param name="template">Шаблон для валидации</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate(ConstructionTemplate template)
        {
            ArgumentNullException.ThrowIfNull(template, nameof(template));

            var result = new ValidationResult();

            ValidateName(template, result);
            ValidateLayersPresence(template, result);

            // Если нет слоёв, дальнейшая валидация слоёв не имеет смысла
            if (!template.LayersAbovePipe.Any() && !template.LayersBelowPipe.Any())
            {
                return result;
            }

            ValidateLayerMaterials(template, result);
            ValidateLayerThicknesses(template, result);

            return result;
        }

        private static void ValidateName(ConstructionTemplate template, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(template.Name))
            {
                result.AddError("Название шаблона не может быть пустым");
                return;
            }

            if (template.Name.Length < MinNameLength || template.Name.Length > MaxNameLength)
            {
                result.AddError(
                    $"Название шаблона должно быть от {MinNameLength} до {MaxNameLength} символов " +
                    $"(текущая длина: {template.Name.Length})");
            }
        }

        private static void ValidateLayersPresence(ConstructionTemplate template, ValidationResult result)
        {
            if (!template.LayersAbovePipe.Any() && !template.LayersBelowPipe.Any())
            {
                result.AddError("Шаблон должен содержать хотя бы один слой");
            }
        }

        private void ValidateLayerMaterials(ConstructionTemplate template, ValidationResult result)
        {
            var allLayers = template.LayersAbovePipe.Concat(template.LayersBelowPipe);

            foreach (var layer in allLayers)
            {
                var material = _materialRepository.GetMaterialById(layer.MaterialId);
                if (material == null)
                {
                    result.AddError($"Материал с идентификатором {layer.MaterialId} не найден");
                }
            }
        }

        private static void ValidateLayerThicknesses(ConstructionTemplate template, ValidationResult result)
        {
            var allLayers = template.LayersAbovePipe.Concat(template.LayersBelowPipe);

            foreach (var layer in allLayers)
            {
                if (layer.Thickness <= MinLayerThickness || layer.Thickness > MaxLayerThickness)
                {
                    result.AddError(
                        $"Толщина слоя должна быть больше {MinLayerThickness} и не превышать {MaxLayerThickness} мм " +
                        $"(текущая: {layer.Thickness:F1} мм)");
                }
            }
        }
    }
}
