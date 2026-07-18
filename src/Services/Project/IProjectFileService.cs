using System.Threading;
using SnowMeltingCalculator.Core.Results;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Интерфейс сервиса для работы с файлами проектов
    /// </summary>
    public interface IProjectFileService
    {
        /// <summary>
        /// Сохранить проект в файл
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="data">Данные проекта</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>true в случае успеха</returns>
        [Obsolete("Use SaveProjectResultAsync/LoadProjectResultAsync")]
        Task<bool> SaveProjectAsync(string filePath, Models.Project.ProjectData data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Загрузить проект из файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Данные проекта или null в случае ошибки</returns>
        [Obsolete("Use SaveProjectResultAsync/LoadProjectResultAsync")]
        Task<Models.Project.ProjectData?> LoadProjectAsync(string filePath);

        /// <summary>
        /// Сохранить проект в файл с детальным результатом операции
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="data">Данные проекта</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат операции сохранения</returns>
        Task<OperationResult<object?>> SaveProjectResultAsync(string filePath, Models.Project.ProjectData data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Загрузить проект из файла с детальным результатом операции
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат операции загрузки</returns>
        Task<OperationResult<Models.Project.ProjectData>> LoadProjectResultAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверить, является ли файл проектом SMC
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>true если файл имеет расширение .smc</returns>
        bool IsSmcFile(string filePath);

        /// <summary>
        /// Получить путь к файлу PDF для предпросмотра
        /// </summary>
        /// <returns>Путь к временному PDF файлу</returns>
        string GetPreviewPdfPath();

        /// <summary>
        /// Очистить временные файлы
        /// </summary>
        void CleanupTempFiles();
    }
}
