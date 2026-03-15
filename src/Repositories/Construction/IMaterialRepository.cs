using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Repositories.Construction
{
    /// <summary>
    /// Интерфейс репозитория материалов
    /// </summary>
    public interface IMaterialRepository
    {
        /// <summary>
        /// Загрузить все материалы из базы данных
        /// </summary>
        /// <returns>Список материалов</returns>
        Task<IEnumerable<Material>> LoadMaterialsAsync();

        /// <summary>
        /// Получить материал по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор материала</param>
        /// <returns>Материал или null, если не найден</returns>
        Material? GetMaterialById(int id);

        /// <summary>
        /// Получить материалы по категории
        /// </summary>
        /// <param name="category">Категория материала</param>
        /// <returns>Список материалов указанной категории</returns>
        IEnumerable<Material> GetMaterialsByCategory(MaterialCategory category);

        /// <summary>
        /// Получить все материалы (из кэша)
        /// </summary>
        /// <returns>Список всех материалов</returns>
        IEnumerable<Material> GetAllMaterials();

        /// <summary>
        /// Признак того, что данные загружены
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Количество загруженных материалов
        /// </summary>
        int MaterialsCount { get; }
    }
}