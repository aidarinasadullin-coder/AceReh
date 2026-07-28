using System.Collections.Generic;

namespace SnowMeltingCalculator.Services.Reports.Calculation.Builders
{
    /// <summary>
    /// Результат работы строителя раздела: сам раздел + метаданные параметров + формулы.
    /// </summary>
    public sealed class SectionBuildResult<TSection>
    {
        /// <summary>
        /// Построенный раздел.
        /// </summary>
        public TSection Section { get; init; } = default!;

        /// <summary>
        /// Метаданные параметров для приложения источников.
        /// </summary>
        public IReadOnlyList<ReportParameterMetadata> ParameterMetadata { get; init; } = new List<ReportParameterMetadata>();

        /// <summary>
        /// Формулы для приложения формул.
        /// </summary>
        public IReadOnlyList<ReportFormula> Formulas { get; init; } = new List<ReportFormula>();
    }
}
