using System;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Core
{
    /// <summary>
    /// Конвейер валидации расчётного контекста
    /// </summary>
    /// <remarks>
    /// Выполняет полную валидацию единого контекста расчёта, вызывая
    /// зарегистрированные в DI валидаторы для каждого модуля.
    /// </remarks>
    public class ValidationPipeline : IValidationPipeline
    {
        private readonly IValidator<IClimateData> _climateValidator;
        private readonly IValidator<ConstructionModel> _constructionValidator;
        private readonly IValidator<ThermalInputs> _thermalValidator;
        private readonly IValidator<HydraulicInputData> _hydraulicValidator;

        /// <summary>
        /// Создать конвейер валидации
        /// </summary>
        /// <param name="climateValidator">Валидатор климатических данных</param>
        /// <param name="constructionValidator">Валидатор конструкции</param>
        /// <param name="thermalValidator">Валидатор тепловых входных данных</param>
        /// <param name="hydraulicValidator">Валидатор гидравлических входных данных</param>
        public ValidationPipeline(
            IValidator<IClimateData> climateValidator,
            IValidator<ConstructionModel> constructionValidator,
            IValidator<ThermalInputs> thermalValidator,
            IValidator<HydraulicInputData> hydraulicValidator)
        {
            _climateValidator = climateValidator ?? throw new ArgumentNullException(nameof(climateValidator));
            _constructionValidator = constructionValidator ?? throw new ArgumentNullException(nameof(constructionValidator));
            _thermalValidator = thermalValidator ?? throw new ArgumentNullException(nameof(thermalValidator));
            _hydraulicValidator = hydraulicValidator ?? throw new ArgumentNullException(nameof(hydraulicValidator));
        }

        /// <inheritdoc />
        public ValidationResult ValidateAll(CalculationContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            var result = new ValidationResult();

            if (context.Climate != null)
            {
                result.Merge(_climateValidator.Validate(context.Climate));
            }

            if (context.Construction is ConstructionModel construction)
            {
                result.Merge(_constructionValidator.Validate(construction));
            }

            if (context.ThermalInputs != null)
            {
                result.Merge(_thermalValidator.Validate(context.ThermalInputs));
            }

            if (context.Hydraulics != null)
            {
                result.Merge(_hydraulicValidator.Validate(context.Hydraulics));
            }

            return result;
        }

        /// <inheritdoc />
        public ValidationResult Validate<T>(T input, IValidator<T> validator)
        {
            ArgumentNullException.ThrowIfNull(validator, nameof(validator));

            return validator.Validate(input);
        }
    }
}
