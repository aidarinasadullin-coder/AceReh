using System.IO;
using System.Text;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Сервис экспорта детального расчётного отчёта в Markdown.
    /// Координирует <see cref="ICalculationReportDataBuilder"/>,
    /// <see cref="ICalculationReportMarkdownRenderer"/> и асинхронную запись UTF-8 файла.
    /// </summary>
    public class CalculationReportExportService : ICalculationReportExportService
    {
        private readonly ICalculationReportDataBuilder _builder;
        private readonly ICalculationReportMarkdownRenderer _renderer;

        public CalculationReportExportService(
            ICalculationReportDataBuilder builder,
            ICalculationReportMarkdownRenderer renderer)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public async Task<bool> ExportReportAsync(
            string filePath,
            ProjectData project,
            CalculationReportMode mode,
            ThermalReportDetail? thermalDetail = null,
            DateTime? reportDate = null,
            HydraulicsReportDetail? hydraulicsDetail = null,
            CancellationToken cancellationToken = default)
        {
            if (project == null)
            {
                System.Diagnostics.Debug.WriteLine("Экспорт отчёта отменён: project равен null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                System.Diagnostics.Debug.WriteLine("Экспорт отчёта отменён: путь к файлу пустой.");
                return false;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var reportData = _builder.Build(project, mode, reportDate, thermalDetail, hydraulicsDetail);
                cancellationToken.ThrowIfCancellationRequested();

                var markdown = _renderer.Render(reportData);
                cancellationToken.ThrowIfCancellationRequested();

                var bytes = Encoding.UTF8.GetBytes(markdown);
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(filePath, bytes, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            when (ex is IOException
                  || ex is UnauthorizedAccessException
                  || ex is ArgumentException
                  || ex is NotSupportedException)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при экспорте Markdown-отчёта: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }
    }
}
