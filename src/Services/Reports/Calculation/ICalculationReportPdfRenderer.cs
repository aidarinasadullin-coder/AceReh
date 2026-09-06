using MigraDoc.DocumentObjectModel;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Рендерер детального расчётного отчёта в PDF (MigraDoc).
    /// </summary>
    public interface ICalculationReportPdfRenderer
    {
        /// <summary>
        /// Сформировать MigraDoc-документ отчёта (разделы 1:1 с Markdown-версией).
        /// </summary>
        /// <param name="data">Данные отчёта.</param>
        /// <returns>Документ для рендеринга в PDF.</returns>
        Document Render(CalculationReportData data);
    }
}
