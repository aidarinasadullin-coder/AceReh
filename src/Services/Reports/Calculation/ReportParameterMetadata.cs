namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Метаданные параметра для приложения источников.
    /// </summary>
    public sealed class ReportParameterMetadata
    {
        /// <summary>
        /// Название параметра.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Символьное обозначение.
        /// </summary>
        public string Symbol { get; init; } = string.Empty;

        /// <summary>
        /// Физический смысл.
        /// </summary>
        public string PhysicalMeaning { get; init; } = string.Empty;

        /// <summary>
        /// Единица измерения.
        /// </summary>
        public string Unit { get; init; } = string.Empty;

        /// <summary>
        /// Категория источника.
        /// </summary>
        public ReportValueSource Source { get; init; }

        /// <summary>
        /// Деталь источника.
        /// </summary>
        public string SourceDetail { get; init; } = string.Empty;

        /// <summary>
        /// Формула / выражение.
        /// </summary>
        public string? Formula { get; init; }

        /// <summary>
        /// Источник формулы (файл, класс, строка).
        /// </summary>
        public string FormulaSource { get; init; } = string.Empty;

        /// <summary>
        /// Где вычисляется значение.
        /// </summary>
        public string WhereCalculated { get; init; } = string.Empty;

        /// <summary>
        /// Где используется значение.
        /// </summary>
        public string WhereUsed { get; init; } = string.Empty;
    }
}
