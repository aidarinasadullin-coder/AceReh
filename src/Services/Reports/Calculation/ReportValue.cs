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

        /// <summary>
        /// Знаки после разделителя при выводе (В9, спека §7.3): назначается
        /// билдером по единице величины; null — рендер применяет формат
        /// таблицы по умолчанию (<see cref="CalculationReportMarkdownRenderHelper.TableFormat"/>).
        /// </summary>
        public int? Decimals { get; init; }

        /// <summary>
        /// Нулевое значение валидно (В14): маркер «нет данных» к нулю не
        /// применяется. По умолчанию false — ноль рендерится как «нет данных»
        /// (правило В2 для заглушек нехранённых величин).
        /// </summary>
        public bool ZeroIsValid { get; init; }
    }
}
