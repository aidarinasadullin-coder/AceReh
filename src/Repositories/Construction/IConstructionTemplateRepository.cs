using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Repositories.Construction
{
    /// <summary>
    /// Интерфейс репозитория шаблонов конструкций
    /// </summary>
    public interface IConstructionTemplateRepository
    {
        /// <summary>
        /// Загрузить все шаблоны конструкций
        /// </summary>
        /// <returns>Список шаблонов</returns>
        Task<IEnumerable<ConstructionTemplate>> GetAllAsync();

        /// <summary>
        /// Получить шаблон по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор шаблона</param>
        /// <returns>Шаблон или null, если не найден</returns>
        Task<ConstructionTemplate?> GetByIdAsync(int id);

        /// <summary>
        /// Добавить новый шаблон
        /// </summary>
        /// <param name="template">Шаблон для добавления</param>
        /// <returns>Добавленный шаблон с присвоенным идентификатором</returns>
        Task<ConstructionTemplate> AddAsync(ConstructionTemplate template);

        /// <summary>
        /// Обновить существующий шаблон
        /// </summary>
        /// <param name="template">Шаблон с обновлёнными данными</param>
        /// <returns>Обновлённый шаблон</returns>
        Task<ConstructionTemplate> UpdateAsync(ConstructionTemplate template);

        /// <summary>
        /// Удалить шаблон по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор шаблона</param>
        /// <returns>true, если шаблон был удалён; иначе false</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Сохранить все шаблоны в JSON файл
        /// </summary>
        Task SaveAsync();
    }
}
