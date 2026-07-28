namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Запись формулы для приложения формул.
    /// </summary>
    public sealed class ReportFormula
    {
        /// <summary>
        /// Символ формулы / величины.
        /// </summary>
        public string Symbol { get; init; } = string.Empty;

        /// <summary>
        /// Математическое выражение.
        /// </summary>
        public string Expression { get; init; } = string.Empty;

        /// <summary>
        /// Путь к источнику формулы в коде / документации.
        /// </summary>
        public string SourcePath { get; init; } = string.Empty;

        /// <summary>
        /// Раздел отчёта, к которому относится формула.
        /// </summary>
        public string Section { get; init; } = string.Empty;

        /// <summary>
        /// Статус привязки формулы (например, когда формула не подтверждена текущим кодом).
        /// </summary>
        public string? FormulaStatus { get; init; }
    }
}
