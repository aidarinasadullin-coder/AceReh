using System;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Thermal
{
    /// <summary>
    /// Валидатор результата теплового расчёта
    /// </summary>
    /// <remarks>
    /// Выполняет пост-расчётные проверки, которые не покрываются входной валидацией:
    /// температуры обратки должна быть неотрицательной, а температурный перепад
    /// должен находиться в допустимом диапазоне.
    /// </remarks>
    public class ThermalResultValidator : IValidator<ThermalCalculationResult>
    {
        /// <summary>
        /// Валидировать результат теплового расчёта
        /// </summary>
        /// <param name="result">Результат теплового расчёта</param>
        /// <returns>Результат валидации</returns>
        /// <exception cref="ArgumentNullException">Если <paramref name="result"/> равен null</exception>
        public ValidationResult Validate(ThermalCalculationResult result)
        {
            ArgumentNullException.ThrowIfNull(result, nameof(result));

            var validationResult = new ValidationResult();

            // Температура обратки выводится из средней температуры и температуры подачи:
            // T_обратки = 2 * T_средняя - T_подачи
            double returnTemperature = 2.0 * result.MeanTemperature - result.SupplyTemperature;
            if (returnTemperature < 0)
            {
                validationResult.AddError(
                    "ReturnTemperature",
                    $"Расчётная температура обратки ({returnTemperature:F1}°C) отрицательна");
            }

            // Температурный перепад должен быть положительным
            if (result.DeltaT <= 0)
            {
                validationResult.AddError(
                    "DeltaT",
                    "Температурный перепад должен быть положительным");
            }
            // и не превышать максимально допустимый
            else if (result.DeltaT > ValidationConstants.MaxDeltaT)
            {
                validationResult.AddError(
                    "DeltaT",
                    $"Температурный перепад ({result.DeltaT:F1}°C) превышает максимально допустимый ({ValidationConstants.MaxDeltaT}°C)");
            }

            return validationResult;
        }
    }
}
