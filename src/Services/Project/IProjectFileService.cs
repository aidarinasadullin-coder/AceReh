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
        /// <returns>true в случае успеха</returns>
        Task<bool> SaveProjectAsync(string filePath, Models.Project.ProjectData data);

        /// <summary>
        /// Загрузить проект из файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Данные проекта или null в случае ошибки</returns>
        Task<Models.Project.ProjectData?> LoadProjectAsync(string filePath);

        /// <summary>
        /// Получить путь для сохранения файла через диалог
        /// </summary>
        /// <param name="defaultFileName">Имя файла по умолчанию</param>
        /// <returns>Путь к файлу или null если отменено</returns>
        Task<string?> GetSaveFilePathAsync(string defaultFileName);

        /// <summary>
        /// Получить путь к файлу для открытия через диалог
        /// </summary>
        /// <returns>Путь к файлу или null если отменено</returns>
        Task<string?> GetOpenFilePathAsync();

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
