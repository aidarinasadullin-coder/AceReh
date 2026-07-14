namespace SnowMeltingCalculator.Services.Results
{
    public interface IPdfExportService
    {
        Task<bool> ExportResultsToPdfAsync(
            string filePath,
            ResultsPdfData data,
            CancellationToken cancellationToken = default);
    }
}
