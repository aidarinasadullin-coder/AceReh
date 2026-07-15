namespace SnowMeltingCalculator.Core
{
    /// <summary>
    /// Валидатор для входных данных типа <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">Тип валидируемого объекта</typeparam>
    public interface IValidator<T>
    {
        /// <summary>
        /// Валидировать входные данные
        /// </summary>
        /// <param name="input">Входные данные</param>
        /// <returns>Результат валидации</returns>
        ValidationResult Validate(T input);
    }
}
