using System;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Рендерер детального расчётного отчёта в Markdown.
    /// </summary>
    public interface ICalculationReportMarkdownRenderer
    {
        /// <summary>
        /// Сформировать Markdown-представление отчёта.
        /// </summary>
        /// <param name="data">Данные отчёта.</param>
        /// <returns>Строка в формате Markdown.</returns>
        string Render(CalculationReportData data);
    }
}
