// ================================================================================
// REHAU Снеготаяние - Интерфейс сервиса экспорта спецификации в Excel
// ================================================================================
//
// Паттерн IPdfExportService: путь файла приходит снаружи (диалог в VM),
// наружу — bool. Данные — готовый read-model ResultsSpecificationData.
//
// ================================================================================

using System.Threading;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Services.Results
{
    public interface IResultsExcelExportService
    {
        /// <summary>
        /// Записать спецификацию закупки в .xlsx (листы «Спецификация» и «Контуры»).
        /// </summary>
        Task<bool> ExportSpecificationToXlsxAsync(
            string filePath,
            ResultsSpecificationData data,
            CancellationToken cancellationToken = default);
    }
}
