using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Repositories
{
    /// <summary>
    /// Интерфейс репозитория для работы с историей поиска городов
    /// </summary>
    public interface ISearchHistoryRepository
    {
        /// <summary>
        /// Получить все записи истории
        /// </summary>
        Task<IEnumerable<SearchHistoryEntry>> GetAllAsync();

        /// <summary>
        /// Получить запись по идентификатору
        /// </summary>
        Task<SearchHistoryEntry?> GetByIdAsync(int id);

        /// <summary>
        /// Получить запись по идентификатору города
        /// </summary>
        Task<SearchHistoryEntry?> GetByCityIdAsync(string cityId);

        /// <summary>
        /// Добавить запись в историю
        /// </summary>
        Task AddAsync(SearchHistoryEntry entry);

        /// <summary>
        /// Обновить запись в истории
        /// </summary>
        Task UpdateAsync(SearchHistoryEntry entry);

        /// <summary>
        /// Удалить запись из истории
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Очистить всю историю
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Инициализировать таблицу (создать если не существует)
        /// </summary>
        Task InitializeAsync();
    }
}