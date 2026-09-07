namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Вспомогательный фабричный метод для создания ReportValue.
    /// </summary>
    public static class ReportValueFactory
    {
        /// <summary>
        /// Создать значение с обязательными метаданными.
        /// </summary>
        /// <remarks>
        /// Для числовых величин билдеры передают <paramref name="decimals"/>
        /// по единице (спека §7.3); пин полноты проверяет, что собранная
        /// модель не содержит <see cref="ReportValue{T}"/> со значением и
        /// пустым <c>Decimals</c>.
        /// </remarks>
        public static ReportValue<T> Create<T>(
            T value,
            string unit,
            ReportValueSource source,
            string sourceDetail,
            int? decimals = null,
            string? formula = null,
            string? formulaStatus = null,
            bool zeroIsValid = false)
        {
            return new ReportValue<T>
            {
                Value = value,
                Unit = unit,
                Source = source,
                SourceDetail = sourceDetail,
                Decimals = decimals,
                Formula = formula,
                FormulaStatus = formulaStatus,
                ZeroIsValid = zeroIsValid
            };
        }
    }
}
