using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Repositories.Construction
{
    /// <summary>
    /// Интерфейс репозитория конструкций
    /// </summary>
    public interface IConstructionRepository
    {
        /// <summary>
        /// Сохранить конструкцию в файл
        /// </summary>
        /// <param name="construction">Конструкция для сохранения</param>
        /// <param name="filePath">Путь к файлу</param>
        Task SaveConstructionAsync(ConstructionModel construction, string filePath);

        /// <summary>
        /// Загрузить конструкцию из файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Загруженная конструкция или null, если файл не найден</returns>
        Task<ConstructionModel?> LoadConstructionAsync(string filePath);

        /// <summary>
        /// Сохранить конструкцию в проект
        /// </summary>
        /// <param name="construction">Конструкция для сохранения</param>
        /// <param name="projectId">Идентификатор проекта</param>
        Task SaveToProjectAsync(ConstructionModel construction, int projectId);

        /// <summary>
        /// Загрузить конструкцию из проекта
        /// </summary>
        /// <param name="projectId">Идентификатор проекта</param>
        /// <returns>Загруженная конструкция или null, если не найдена</returns>
        Task<ConstructionModel?> LoadFromProjectAsync(int projectId);

        /// <summary>
        /// Получить список сохранённых конструкций
        /// </summary>
        /// <param name="directoryPath">Путь к директории с файлами конструкций</param>
        /// <returns>Список путей к файлам конструкций</returns>
        Task<IEnumerable<string>> GetSavedConstructionsAsync(string directoryPath);
    }
}