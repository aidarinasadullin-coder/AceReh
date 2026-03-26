using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Services.Climate
{
    /// <summary>
    /// Интерфейс сервиса для управления историей поиска городов
    /// </summary>
    public interface ISearchHistoryService
    {
        /// <summary>
        /// Получить последние N городов из истории
        /// </summary>
        /// <param name="limit">Максимальное количество городов (по умолчанию 10)</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Список записей истории, отсортированный по дате использования</returns>
        Task<IEnumerable<SearchHistoryEntry>> GetRecentAsync(int limit = 10, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Добавить или обновить запись в истории
        /// </summary>
        /// <param name="cityId">Идентификатор города (CityInfo.Name)</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task AddAsync(string cityId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Очистить историю поиска
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}