using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Services.Climate
{
    /// <summary>
    /// Интерфейс сервиса для работы с климатическими данными
    /// </summary>
    public interface IClimateDataService
    {
        /// <summary>
        /// Загрузить климатические данные
        /// </summary>
        Task LoadClimateDataAsync();

        /// <summary>
        /// Поиск городов по запросу
        /// </summary>
        /// <param name="query">Поисковый запрос (минимум 2 символа)</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Список найденных городов (максимум 20)</returns>
        Task<IEnumerable<CityInfo>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Поиск городов с приоритетом совпадений
        /// </summary>
        /// <param name="query">Поисковый запрос (минимум 1 символ)</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Список до 15 городов, отсортированных по релевантности</returns>
        Task<IEnumerable<CityInfo>> SearchCitiesWithPriorityAsync(string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Подсветка совпадений в названии и регионе
        /// </summary>
        /// <param name="city">Город</param>
        /// <param name="query">Поисковый запрос</param>
        /// <returns>Кортеж (highlightedName, highlightedRegion, matchType)</returns>
        (string highlightedName, string highlightedRegion, MatchType matchType) HighlightMatch(CityInfo city, string query);

        /// <summary>
        /// Получить последние использованные города
        /// </summary>
        /// <param name="limit">Максимальное количество (по умолчанию 10)</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Список последних городов</returns>
        Task<IEnumerable<CityInfo>> GetRecentCitiesAsync(int limit = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохранить город в историю поиска
        /// </summary>
        /// <param name="city">Город для сохранения</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task SaveToHistoryAsync(CityInfo city, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить город по названию
        /// </summary>
        CityInfo? GetCityByName(string name);

        /// <summary>
        /// Получить все города
        /// </summary>
        IEnumerable<CityInfo> GetAllCities();

        /// <summary>
        /// Определить климатическую зону по температуре
        /// </summary>
        /// <param name="t5days">Температура холодной пятидневки</param>
        /// <param name="isHighRequirements">Повышенные требования</param>
        ClimateZone DetermineZone(double t5days, bool isHighRequirements = false);

        /// <summary>
        /// Признак того, что данные загружены
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Количество загруженных городов
        /// </summary>
        int CitiesCount { get; }
    }
}