namespace SnowMeltingCalculator.Core
{
    /// <summary>
    /// Конвейер валидации расчётного контекста
    /// </summary>
    public interface IValidationPipeline
    {
        /// <summary>
        /// Выполнить полную валидацию контекста
        /// </summary>
        /// <param name="context">Контекст расчёта</param>
        /// <returns>Результат валидации</returns>
        ValidationResult ValidateAll(CalculationContext context);

        /// <summary>
        /// Выполнить валидацию входных данных указанным валидатором
        /// </summary>
        /// <typeparam name="T">Тип валидируемого объекта</typeparam>
        /// <param name="input">Входные данные</param>
        /// <param name="validator">Валидатор</param>
        /// <returns>Результат валидации</returns>
        ValidationResult Validate<T>(T input, IValidator<T> validator);
    }
}
