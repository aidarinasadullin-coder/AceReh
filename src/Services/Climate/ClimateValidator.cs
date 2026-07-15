using System;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Services.Climate
{
    /// <summary>
    /// Валидатор климатических данных
    /// </summary>
    /// <remarks>
    /// Правила и сообщения сохранены из <see cref="ViewModels.Climate.ClimateViewModel.ValidateAll()"/>
    /// для обеспечения идентичного поведения при переходе на единый <see cref="ValidationResult"/>.
    /// </remarks>
    public class ClimateValidator : IValidator<IClimateData>
    {
        /// <summary>
        /// Минимальная температура наружного воздуха, °C
        /// </summary>
        /// <remarks>
        /// Значение -50°C сохранено из ClimateViewModel.ValidateAll(); в ValidationConstants MinAirTemperature = -60°C.
        /// </remarks>
        private const double MinAirTemperature = -50.0;

        /// <summary>
        /// Минимальная скорость ветра, м/с
        /// </summary>
        /// <remarks>
        /// Значение 0.1 м/с сохранено из ClimateViewModel.ValidateAll(); в ValidationConstants MinWindSpeed = 0 м/с.
        /// </remarks>
        private const double MinWindSpeed = 0.1;

        /// <summary>
        /// Максимальная скорость ветра, м/с
        /// </summary>
        /// <remarks>
        /// Значение 30 м/с сохранено из ClimateViewModel.ValidateAll(); в ValidationConstants MaxWindSpeed = 50 м/с.
        /// </remarks>
        private const double MaxWindSpeed = 30.0;

        /// <summary>
        /// Валидировать климатические данные
        /// </summary>
        /// <param name="data">Кlimатические данные</param>
        /// <returns>Результат валидации</returns>
        /// <exception cref="ArgumentNullException">Если <paramref name="data"/> равен null</exception>
        public ValidationResult Validate(IClimateData data)
        {
            ArgumentNullException.ThrowIfNull(data, nameof(data));

            var result = new ValidationResult();

            // Температура наружного воздуха: от -50°C до +10°C
            if (data.AirTemperature < MinAirTemperature || data.AirTemperature > ValidationConstants.MaxAirTemperature)
            {
                result.AddError("Температура должна быть от -50°C до +10°C");
            }

            // Скорость ветра: от 0.1 до 30 м/с
            if (data.WindSpeed < MinWindSpeed || data.WindSpeed > MaxWindSpeed)
            {
                result.AddError("Скорость ветра от 0.1 до 30 м/с");
            }

            // Интенсивность снегопада: от 0 до 20 мм/ч
            if (data.SnowfallIntensity < ValidationConstants.MinSnowfallIntensity || data.SnowfallIntensity > ValidationConstants.MaxSnowfallIntensity)
            {
                result.AddError("Интенсивность от 0 до 20 мм/ч");
            }

            return result;
        }
    }
}
