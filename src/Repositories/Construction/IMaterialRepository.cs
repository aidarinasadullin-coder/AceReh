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
        /// Добавить новый материал
        /// </summary>
        /// <param name="material">Материал для добавления</param>
        /// <returns>Добавленный материал с присвоенным идентификатором</returns>
        Task<Material> AddAsync(Material material);

        /// <summary>
        /// Обновить существующий материал
        /// </summary>
        /// <param name="material">Материал с обновлёнными данными</param>
        /// <returns>Обновлённый материал</returns>
        Task<Material> UpdateAsync(Material material);

        /// <summary>
        /// Удалить материал по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор материала</param>
        /// <returns>true, если материал был удалён; иначе false</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Сохранить все материалы в JSON файл
        /// </summary>
        Task SaveMaterialsAsync();

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