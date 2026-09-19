using System;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Models.Hydraulics;
using ValidationResult = SnowMeltingCalculator.Core.ValidationResult;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Валидатор входных данных гидравлического расчёта
    /// </summary>
    /// <remarks>
    /// Правила и сообщения сохранены из <see cref="HydraulicInputData"/>
    /// для обеспечения идентичного поведения при переходе на единый <see cref="ValidationResult"/>.
    /// </remarks>
    public class HydraulicValidator : IValidator<HydraulicInputData>
    {
        /// <summary>
        /// Валидировать входные данные гидравлического расчёта
        /// </summary>
        /// <param name="input">Входные данные</param>
        /// <returns>Результат валидации</returns>
        /// <exception cref="ArgumentNullException">Если <paramref name="input"/> равен null</exception>
        public ValidationResult Validate(HydraulicInputData input)
        {
            ArgumentNullException.ThrowIfNull(input, nameof(input));

            var result = new ValidationResult();

            if (input.GlycolConcentration < ValidationConstants.MinGlycolConcentration || input.GlycolConcentration > ValidationConstants.MaxGlycolConcentration)
                result.AddError($"Концентрация гликоля должна быть от {(ValidationConstants.MinGlycolConcentration).ToString("F0", AppCulture.Culture)} до {(ValidationConstants.MaxGlycolConcentration).ToString("F0", AppCulture.Culture)}% (текущая: {(input.GlycolConcentration).ToString("F0", AppCulture.Culture)}%)");

            if (input.SupplySpacing_cm <= 0)
                result.AddError("Шаг подводки должен быть положительным");

            if (input.SupplyHeatPercent < 0 || input.SupplyHeatPercent > 100)
                result.AddError($"Доля тепла от подводок должна быть от 0 до 100% (текущая: {(input.SupplyHeatPercent).ToString("F0", AppCulture.Culture)}%)");

            return result;
        }
    }
}
