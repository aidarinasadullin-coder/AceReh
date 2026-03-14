using System.Collections.Concurrent;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Repositories;

namespace SnowMeltingCalculator.Services.Climate
{
    /// <summary>
    /// Сервис для работы с климатическими данными
    /// </summary>
    public class ClimateDataService : IClimateDataService
    {
        private readonly IClimateDataRepository _repository;
        private readonly ConcurrentBag<CityInfo> _citiesCache = new();
        private bool _isLoaded = false;
        private readonly object _loadLock = new();

        /// <summary>
        /// Создать сервис
        /// </summary>
        public ClimateDataService(IClimateDataRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Признак того, что данные загружены
        /// </summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// Количество загруженных городов
        /// </summary>
        public int CitiesCount => _citiesCache.Count;

        /// <summary>
        /// Загрузить климатические данные
        /// </summary>
        public async Task LoadClimateDataAsync()
        {
            if (_isLoaded) return;

            lock (_loadLock)
            {
                if (_isLoaded) return;
            }

            var cities = await _repository.LoadCitiesAsync();

            foreach (var city in cities)
            {
                _citiesCache.Add(city);
            }

            _isLoaded = true;
        }

        /// <summary>
        /// Поиск городов по запросу
        /// </summary>
        public Task<IEnumerable<CityInfo>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default)
        {
            // Минимум 2 символа для поиска
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Task.FromResult(Enumerable.Empty<CityInfo>());
            }

            cancellationToken.ThrowIfCancellationRequested();

            var results = _citiesCache
                .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           c.Region.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Name)
                .Take(20);

            return Task.FromResult(results);
        }

        /// <summary>
        /// Получить город по названию
        /// </summary>
        public CityInfo? GetCityByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return _citiesCache.FirstOrDefault(c => 
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Получить все города
        /// </summary>
        public IEnumerable<CityInfo> GetAllCities()
        {
            return _citiesCache.AsEnumerable();
        }

        /// <summary>
        /// Определить климатическую зону по температуре
        /// </summary>
        /// <param name="t5days">Температура холодной пятидневки (t_5days_092)</param>
        /// <param name="isHighRequirements">Признак повышенных требований</param>
        /// <returns>Климатическая зона</returns>
        /// <remarks>
        /// Логика определения зоны:
        /// - t ≥ -27°C → Zone_M10 (колонка -10°C)
        /// - -37°C < t < -27°C → Zone_M15 (колонка -15°C)
        /// - t ≤ -37°C → Zone_M20 (колонка -20°C)
        /// - Повышенные требования → Zone_M20_Plus (колонка -20°C)
        /// </remarks>
        public ClimateZone DetermineZone(double t5days, bool isHighRequirements = false)
        {
            // Повышенные требования всегда используют колонку -20°C
            if (isHighRequirements)
            {
                return ClimateZone.Zone_M20_Plus;
            }

            // Определение зоны по температуре
            if (t5days >= -27)
            {
                return ClimateZone.Zone_M10;
            }

            if (t5days > -37)
            {
                return ClimateZone.Zone_M15;
            }

            return ClimateZone.Zone_M20;
        }

        /// <summary>
        /// Получить описание климатической зоны
        /// </summary>
        public static string GetZoneDescription(ClimateZone zone)
        {
            return zone switch
            {
                ClimateZone.Zone_M10 => "Колонка -10°C (t ≥ -27°C)",
                ClimateZone.Zone_M15 => "Колонка -15°C (-37°C < t < -27°C)",
                ClimateZone.Zone_M20 => "Колонка -20°C (t ≤ -37°C)",
                ClimateZone.Zone_M20_Plus => "Колонка -20°C (повышенные требования)",
                _ => string.Empty
            };
        }
    }
}