using System;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Thermal
{
    /// <summary>
    /// Валидатор входных параметров теплового расчёта
    /// </summary>
    /// <remarks>
    /// Обёртка над <see cref="IThermalCalculator.Validate"/>,
    /// преобразующая bool + string[] в унифицированный <see cref="ValidationResult"/>.
    /// </remarks>
    public class ThermalValidator : IValidator<ThermalInputs>
    {
        private readonly IThermalCalculator _calculator;
        private readonly IClimateData _climate;
        private readonly IConstructionData _construction;

        /// <summary>
        /// Создать валидатор тепловых входных данных
        /// </summary>
        /// <param name="calculator">Калькулятор теплового расчёта</param>
        /// <param name="climate">Климатические данные</param>
        /// <param name="construction">Данные конструкции</param>
        public ThermalValidator(
            IThermalCalculator calculator,
            IClimateData climate,
            IConstructionData construction)
        {
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _climate = climate ?? throw new ArgumentNullException(nameof(climate));
            _construction = construction ?? throw new ArgumentNullException(nameof(construction));
        }

        /// <inheritdoc />
        public ValidationResult Validate(ThermalInputs input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            bool isValid = _calculator.Validate(input, _climate, _construction, out string[] errors);

            return isValid
                ? ValidationResult.Success()
                : ValidationResult.Failure(errors);
        }
    }
}
