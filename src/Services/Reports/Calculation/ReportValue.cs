namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Значение с трассировкой источника, единицей измерения и привязкой к формуле.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    public sealed class ReportValue<T>
    {
        /// <summary>
        /// Значение.
        /// </summary>
        public T? Value { get; init; }

        /// <summary>
        /// Единица измерения.
        /// </summary>
        public string Unit { get; init; } = string.Empty;

        /// <summary>
        /// Категория источника значения.
        /// </summary>
        public ReportValueSource Source { get; init; }

        /// <summary>
        /// Деталь источника (путь к свойству, базе данных, формуле и т.п.).
        /// </summary>
        public string SourceDetail { get; init; } = string.Empty;

        /// <summary>
        /// Символьное обозначение / формула, использованная для получения значения.
        /// </summary>
        public string? Formula { get; init; }

        /// <summary>
        /// Статус привязки к существующей формуле.
        /// </summary>
        /// <remarks>
        /// Например: "требуется привязка к существующей формуле".
        /// </remarks>
        public string? FormulaStatus { get; init; }
    }
}
