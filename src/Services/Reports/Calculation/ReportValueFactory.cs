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
        public static ReportValue<T> Create<T>(
            T value,
            string unit,
            ReportValueSource source,
            string sourceDetail,
            string? formula = null,
            string? formulaStatus = null)
        {
            return new ReportValue<T>
            {
                Value = value,
                Unit = unit,
                Source = source,
                SourceDetail = sourceDetail,
                Formula = formula,
                FormulaStatus = formulaStatus
            };
        }
    }
}
