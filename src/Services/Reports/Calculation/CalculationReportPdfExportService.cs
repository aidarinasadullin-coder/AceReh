using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MigraDoc.Rendering;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Сервис экспорта детального расчётного отчёта в PDF (мини-фаза PDF-PZ).
    /// Координирует <see cref="ICalculationReportDataBuilder"/>,
    /// <see cref="ICalculationReportPdfRenderer"/> и рендер MigraDoc → PDF;
    /// сигнатура и обработка ошибок — по образцу
    /// <see cref="CalculationReportExportService"/> (Markdown).
    /// </summary>
    public class CalculationReportPdfExportService : ICalculationReportPdfExportService
    {
        private readonly ICalculationReportDataBuilder _builder;
        private readonly ICalculationReportPdfRenderer _renderer;

        public CalculationReportPdfExportService(
            ICalculationReportDataBuilder builder,
            ICalculationReportPdfRenderer renderer)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public Task<bool> ExportReportAsync(
            string filePath,
            ProjectData project,
            CalculationReportMode mode,
            ThermalReportDetail? thermalDetail = null,
            DateTime? reportDate = null,
            CancellationToken cancellationToken = default)
        {
            if (project == null)
            {
                System.Diagnostics.Debug.WriteLine("Экспорт PDF-отчёта отменён: project равен null.");
                return Task.FromResult(false);
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                System.Diagnostics.Debug.WriteLine("Экспорт PDF-отчёта отменён: путь к файлу пустой.");
                return Task.FromResult(false);
            }

            return Task.Run(
                () =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var reportData = _builder.Build(project, mode, reportDate, thermalDetail);
                        cancellationToken.ThrowIfCancellationRequested();

                        var document = _renderer.Render(reportData);
                        cancellationToken.ThrowIfCancellationRequested();

                        // true = эмбеддинг шрифтов (обязателен для кириллицы).
                        var pdfRenderer = new PdfDocumentRenderer(true)
                        {
                            Document = document
                        };
                        pdfRenderer.RenderDocument();
                        cancellationToken.ThrowIfCancellationRequested();

                        var directory = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        pdfRenderer.PdfDocument.Save(filePath);
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
                        System.Diagnostics.Debug.WriteLine($"Ошибка при экспорте PDF-отчёта: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                        return false;
                    }
                },
                cancellationToken);
        }
    }
}
