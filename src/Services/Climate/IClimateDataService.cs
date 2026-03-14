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