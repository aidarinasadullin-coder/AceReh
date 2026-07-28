using System.Collections.Generic;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Предупреждение детального расчётного отчёта.
    /// </summary>
    public sealed class CalculationReportWarning
    {
        /// <summary>
        /// Код предупреждения.
        /// </summary>
        public string Code { get; init; } = string.Empty;

        /// <summary>
        /// Уровень серьёзности (Info, Warning, Error).
        /// </summary>
        public string Severity { get; init; } = string.Empty;

        /// <summary>
        /// Сообщение.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Путь к источнику значения / проверки.
        /// </summary>
        public string SourcePath { get; init; } = string.Empty;

        /// <summary>
        /// Связанные обозначения значений.
        /// </summary>
        public IReadOnlyList<string> RelatedValues { get; init; } = new List<string>();
    }
}
