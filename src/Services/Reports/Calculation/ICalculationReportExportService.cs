namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Сервис экспорта детального расчётного отчёта в Markdown.
    /// </summary>
    public interface ICalculationReportExportService
    {
        /// <summary>
        /// Экспортировать детальный расчётный отчёт в файл Markdown.
        /// </summary>
        /// <param name="filePath">Путь к выходному файлу.</param>
        /// <param name="project">Данные проекта.</param>
        /// <param name="mode">Режим отчёта.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>true, если файл успешно записан; иначе false.</returns>
        Task<bool> ExportReportAsync(
            string filePath,
            Models.Project.ProjectData project,
            CalculationReportMode mode,
            CancellationToken cancellationToken = default);
    }
}
