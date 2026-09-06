using System;
using System.Threading;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Сервис экспорта детального расчётного отчёта в PDF (пояснительная
    /// записка, мини-фаза PDF-PZ).
    /// </summary>
    public interface ICalculationReportPdfExportService
    {
        /// <summary>
        /// Экспортировать детальный расчётный отчёт в файл PDF.
        /// </summary>
        /// <param name="filePath">Путь к выходному файлу.</param>
        /// <param name="project">Данные проекта.</param>
        /// <param name="mode">Режим отчёта.</param>
        /// <param name="thermalDetail">Детальные тепловые величины (ADR-010); null — прежнее поведение.</param>
        /// <param name="reportDate">Дата формирования отчёта; null — значение по умолчанию.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>true, если файл успешно записан; иначе false.</returns>
        Task<bool> ExportReportAsync(
            string filePath,
            ProjectData project,
            CalculationReportMode mode,
            ThermalReportDetail? thermalDetail = null,
            DateTime? reportDate = null,
            CancellationToken cancellationToken = default);
    }
}
