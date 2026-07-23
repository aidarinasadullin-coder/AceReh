using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Валидатор конструкций системы снеготаяния
    /// </summary>
    /// <remarks>
    /// Правила валидации:
    /// - Минимальная стяжка над трубой: 40 мм (50 мм при нагрузках)
    /// - Бетон: максимальная температура подачи 50°C
    /// - Асфальт: не применять при температуре наружного воздуха < -15°C
    /// - УГВ < 1м: использовать λБ для слоёв под трубой
    /// </remarks>
    public class ConstructionValidator : IValidator<ConstructionModel>
    {
        /// <summary>
        /// Минимальная толщина слоёв над трубой без нагрузок, мм
        /// </summary>
        private const double MinThicknessAbovePipeNoLoads = 40.0;

        /// <summary>
        /// Минимальная толщина слоёв над трубой при наличии нагрузок, мм
        /// </summary>
        private const double MinThicknessAbovePipeWithLoads = 50.0;

        /// <summary>
        /// Максимальная температура подачи для бетона, °C
        /// </summary>
        private const double MaxSupplyTempForConcrete = 50.0;

        /// <summary>
        /// Минимальная температура наружного воздуха для асфальта, °C
        /// </summary>
        private const double MinOutdoorTempForAsphalt = -15.0;

        /// <summary>
        /// Пороговый уровень грунтовых вод для использования λБ, м
        /// </summary>
        private const double GroundwaterThresholdForLambdaB = 1.0;

        /// <summary>
        /// Максимальная толщина слоя, мм
        /// </summary>
        private const double MaxLayerThickness = 1000.0;

        /// <summary>
        /// Минимальный уровень грунтовых вод, м
        /// </summary>
        private const double MinGroundwaterLevel = 0.0;

        /// <summary>
        /// Максимальный уровень грунтовых вод, м
        /// </summary>
        private const double MaxGroundwaterLevel = 10.0;

        /// <summary>
        /// Валидация конструкции
        /// </summary>
        /// <param name="construction">Конструкция для валидации</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate(ConstructionModel construction)
        {
            ArgumentNullException.ThrowIfNull(construction, nameof(construction));

            var result = new ValidationResult();

            // Проверка наличия слоёв
            ValidateLayersPresence(construction, result);

            // Если нет слоёв, дальнейшая валидация не имеет смысла
            if (!result.IsValid)
            {
                return result;
            }

            // Проверка минимальной толщины над трубой
            ValidateMinThicknessAbovePipe(construction, result);

            // Проверка толщины слоёв
            ValidateLayerThicknesses(construction, result);

            // Проверка уровня грунтовых вод
            ValidateGroundwaterLevel(construction, result);

            // Проверка материалов
            ValidateMaterials(construction, result);

            // Проверка использования λБ при высоком УГВ
            ValidateLambdaForGroundwater(construction, result);

            return result;
        }

        /// <summary>
        /// Проверка наличия слоёв
        /// </summary>
        private static void ValidateLayersPresence(ConstructionModel construction, ValidationResult result)
        {
            if (construction.LayersAbovePipe.Count == 0 && construction.Layers.Count == 0)
            {
                result.AddError("Конструкция должна содержать хотя бы один слой");
            }
        }

        /// <summary>
        /// Проверка минимальной толщины над трубой
        /// </summary>
        private void ValidateMinThicknessAbovePipe(ConstructionModel construction, ValidationResult result)
        {
            var minThickness = construction.HasLoads
                ? MinThicknessAbovePipeWithLoads
                : MinThicknessAbovePipeNoLoads;

            var totalAbove = construction.LayersAbovePipe.Sum(l => l.Thickness);

            if (construction.LayersAbovePipe.Count > 0 && totalAbove < minThickness)
            {
                var loadSuffix = construction.HasLoads ? " (при наличии нагрузок)" : "";
                result.AddError(
                    $"Минимальная толщина слоёв над трубой{loadSuffix}: {minThickness} мм " +
                    $"(текущая: {totalAbove:F0} мм)");
            }
        }

        /// <summary>
        /// Проверка толщины слоёв
        /// </summary>
        private static void ValidateLayerThicknesses(ConstructionModel construction, ValidationResult result)
        {
            foreach (var layer in construction.LayersAbovePipe.Concat(construction.Layers))
            {
                if (layer.Thickness > MaxLayerThickness)
                {
                    result.AddError(
                        $"Толщина слоя '{layer.Material?.Name ?? "Не указан"}' не может превышать " +
                        $"{MaxLayerThickness} мм (текущая: {layer.Thickness:F0} мм)");
                }
            }
        }

        /// <summary>
        /// Проверка уровня грунтовых вод
        /// </summary>
        private static void ValidateGroundwaterLevel(ConstructionModel construction, ValidationResult result)
        {
            if (construction.GroundwaterLevel < MinGroundwaterLevel ||
                construction.GroundwaterLevel > MaxGroundwaterLevel)
            {
                result.AddError(
                    $"Уровень грунтовых вод должен быть от {MinGroundwaterLevel} до {MaxGroundwaterLevel} м " +
                    $"(текущий: {construction.GroundwaterLevel:F1} м)");
            }
        }

        /// <summary>
        /// Проверка материалов
        /// </summary>
        private void ValidateMaterials(ConstructionModel construction, ValidationResult result)
        {
            foreach (var layer in construction.LayersAbovePipe)
            {
                ValidateMaterialForAbovePipe(layer, result);
            }

            foreach (var layer in construction.Layers.Where(l => l.Position == LayerPosition.BelowPipe))
            {
                ValidateMaterialForBelowPipe(layer, result);
            }
        }

        /// <summary>
        /// Проверка материала для слоя над трубой
        /// </summary>
        private void ValidateMaterialForAbovePipe(Layer layer, ValidationResult result)
        {
            if (layer.Material == null)
            {
                result.AddError("Материал слоя не указан");
                return;
            }

            // Проверка максимальной температуры подачи для бетона (Screed удалён, материалы стяжки теперь Concrete)
            if (layer.Material.Category == MaterialCategory.Concrete)
            {
                if (layer.Material.MaxSupplyTemp.HasValue)
                {
                    result.AddWarning(
                        $"Материал '{layer.Material.Name}': максимальная температура подачи " +
                        $"{layer.Material.MaxSupplyTemp.Value}°C");
                }
            }

            // Проверка асфальта
            if (layer.Material.Category == MaterialCategory.Coating)
            {
                if (layer.Material.MinOutdoorTemp.HasValue)
                {
                    result.AddWarning(
                        $"Материал '{layer.Material.Name}': не применять при температуре " +
                        $"наружного воздуха <= {layer.Material.MinOutdoorTemp.Value}°C");
                }
            }
        }

        /// <summary>
        /// Проверка материала для слоя под трубой
        /// </summary>
        private static void ValidateMaterialForBelowPipe(Layer layer, ValidationResult result)
        {
            if (layer.Material == null)
            {
                result.AddError("Материал слоя не указан");
                return;
            }

            // Дополнительные проверки для слоёв под трубой можно добавить здесь
        }

        /// <summary>
        /// Проверка использования λБ при высоком УГВ
        /// </summary>
        private void ValidateLambdaForGroundwater(ConstructionModel construction, ValidationResult result)
        {
            // Если УГВ < 1м, должны использоваться λБ для слоёв под трубой
            if (construction.GroundwaterLevel < GroundwaterThresholdForLambdaB)
            {
                result.AddWarning(
                    $"Уровень грунтовых вод ({construction.GroundwaterLevel:F1} м) < {GroundwaterThresholdForLambdaB} м. " +
                    $"Для слоёв под трубой используется λБ (влажные условия).");

                // Проверяем, что слои под трубой используют правильную λ
                foreach (var layer in construction.Layers.Where(l => l.Position == LayerPosition.BelowPipe))
                {
                    if (!layer.IsLambdaOverridden && layer.Material != null)
                    {
                        // Если λ не переопределена вручную, она должна автоматически использовать λБ
                        if (Math.Abs(layer.CalculatedLambda - layer.Material.LambdaB) > 0.001)
                        {
                            result.AddWarning(
                                $"Слой '{layer.Material.Name}' под трубой должен использовать λБ " +
                                $"({layer.Material.LambdaB} Вт/м·К) при УГВ < {GroundwaterThresholdForLambdaB} м");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Валидация конструкции для заданной температуры наружного воздуха
        /// </summary>
        /// <param name="construction">Конструкция</param>
        /// <param name="outdoorTemp">Температура наружного воздуха, °C</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult ValidateForOutdoorTemperature(ConstructionModel construction, double outdoorTemp)
        {
            var result = Validate(construction);

            // Проверка асфальта при низких температурах
            foreach (var layer in construction.LayersAbovePipe)
            {
                if (layer.Material?.MinOutdoorTemp.HasValue == true &&
                    outdoorTemp <= layer.Material.MinOutdoorTemp.Value)
                {
                    result.AddError(
                        $"Материал '{layer.Material.Name}' нельзя применять при температуре " +
                        $"наружного воздуха <= {layer.Material.MinOutdoorTemp.Value}°C " +
                        $"(текущая: {outdoorTemp:F1}°C)");
                }
            }

            return result;
        }

        /// <summary>
        /// Валидация конструкции для заданной температуры подачи
        /// </summary>
        /// <param name="construction">Конструкция</param>
        /// <param name="supplyTemp">Температура подачи, °C</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult ValidateForSupplyTemperature(ConstructionModel construction, double supplyTemp)
        {
            var result = Validate(construction);

            // Проверка максимальной температуры подачи для бетона
            foreach (var layer in construction.LayersAbovePipe)
            {
                if (layer.Material?.MaxSupplyTemp.HasValue == true &&
                    supplyTemp > layer.Material.MaxSupplyTemp.Value)
                {
                    result.AddWarning(
                        $"Температура подачи ({supplyTemp:F1}°C) превышает максимально допустимую " +
                        $"для материала '{layer.Material.Name}' ({layer.Material.MaxSupplyTemp.Value}°C)");
                }
            }

            return result;
        }
    }
}