using System;
using System.Collections.Generic;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Core.Extensions;
using SnowMeltingCalculator.Models.Hydraulics;
using ValidationResult = SnowMeltingCalculator.Core.ValidationResult;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Валидатор строки контура гидравлического расчёта
    /// </summary>
    /// <remarks>
    /// Проверяет длину, скорость потока и суммарные потери давления контура
    /// через существующие методы расширения <see cref="ValidationExtensions"/>.
    /// </remarks>
    public class CircuitValidator : IValidator<CircuitRow>
    {
        /// <summary>
        /// Валидировать строку контура
        /// </summary>
        /// <param name="input">Строка контура</param>
        /// <returns>Результат валидации</returns>
        /// <exception cref="ArgumentNullException">Если <paramref name="input"/> равен null</exception>
        public ValidationResult Validate(CircuitRow input)
        {
            ArgumentNullException.ThrowIfNull(input, nameof(input));

            var errors = new List<string>();

            input.CircuitLength.ValidateCircuitLength(errors);
            input.Velocity.ValidateVelocity(errors);

            if (input.OperatingResult != null)
            {
                input.OperatingResult.DpGesamt.ValidatePressureLoss(errors);
            }

            var result = new ValidationResult();
            foreach (var error in errors)
            {
                result.AddError(error);
            }

            return result;
        }
    }
}
