namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Строковые константы и литералы, используемые Markdown-рендерером отчёта.
    /// </summary>
    public static class CalculationReportMarkdownRendererConstants
    {
        public const string NoWarningSentinel = "Предупреждения по доступным данным проекта и программы не сформированы.";
        public const string MissingValue = "нет данных";
        public const string FormulaNotInMvp = "не включена в MVP";

        /// <summary>
        /// Текст статуса «величина считается по формуле, не привязанной к коду».
        /// Единственный владелец текста: билдеры выставляют типизированный флаг
        /// <see cref="ReportValue{T}.FormulaUnconfirmed"/>, рендеры выводят эту константу.
        /// </summary>
        public const string FormulaStatusUnconfirmed = "требуется привязка к существующей формуле";
    }
}
