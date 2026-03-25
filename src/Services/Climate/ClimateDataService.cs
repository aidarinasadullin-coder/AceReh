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
        private readonly ISearchHistoryRepository? _historyRepository;
        private readonly ConcurrentBag<CityInfo> _citiesCache = new();
        private readonly ConcurrentDictionary<string, IEnumerable<CityInfo>> _searchCache = new();
        private bool _isLoaded = false;
        private readonly object _loadLock = new();

        /// <summary>
        /// Создать сервис
        /// </summary>
        public ClimateDataService(
            IClimateDataRepository repository,
            ISearchHistoryRepository? historyRepository = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _historyRepository = historyRepository;
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

        /// <summary>
        /// Поиск городов с приоритетом совпадений
        /// </summary>
        public Task<IEnumerable<CityInfo>> SearchCitiesWithPriorityAsync(string query, CancellationToken cancellationToken = default)
        {
            // Минимум 1 символ для поиска
            if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
            {
                return Task.FromResult(Enumerable.Empty<CityInfo>());
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Проверка кэша
            if (_searchCache.TryGetValue(query, out var cached))
            {
                return Task.FromResult(cached);
            }

            // Шаг 1: Точные совпадения в начале названия (StartsWith)
            var exactMatches = _citiesCache
                .Where(c => c.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Name);

            // Шаг 2: Совпадения в названии (Contains)
            var nameMatches = _citiesCache
                .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                           !c.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Name);

            // Шаг 3: Совпадения в регионе
            var regionMatches = _citiesCache
                .Where(c => c.Region.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                           !c.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Region)
                .ThenBy(c => c.Name);

            // Объединение с приоритетом
            var results = exactMatches
                .Concat(nameMatches)
                .Concat(regionMatches)
                .Take(15)
                .ToList();

            // Кэширование
            _searchCache[query] = results;

            return Task.FromResult(results.AsEnumerable());
        }

        /// <summary>
        /// Подсветка совпадений в названии и регионе
        /// </summary>
        public (string highlightedName, string highlightedRegion, MatchType matchType) HighlightMatch(CityInfo city, string query)
        {
            if (city == null)
            {
                return (string.Empty, string.Empty, MatchType.Contains);
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return (city.Name, city.Region, MatchType.Contains);
            }

            // Определение типа совпадения
            var matchType = DetermineMatchType(city, query);
            
            // Подсветка в названии
            var highlightedName = HighlightText(city.Name, query);
            
            // Подсветка в регионе (если совпадение)
            var highlightedRegion = city.Region.Contains(query, StringComparison.OrdinalIgnoreCase)
                ? HighlightText(city.Region, query)
                : city.Region;

            return (highlightedName, highlightedRegion, matchType);
        }

        /// <summary>
        /// Определение типа совпадения
        /// </summary>
        private MatchType DetermineMatchType(CityInfo city, string query)
        {
            if (city.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                return MatchType.StartsWith;
            
            if (city.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                return MatchType.Contains;
            
            return MatchType.Region;
        }

        /// <summary>
        /// Подсветка текста
        /// </summary>
        private string HighlightText(string text, string query)
        {
            var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return text;

            var before = text.Substring(0, index);
            var match = text.Substring(index, query.Length);
            var after = text.Substring(index + query.Length);

            // Формат: "до**совпадение**после"
            return $"{before}**{match}**{after}";
        }

        /// <summary>
        /// Получить последние использованные города
        /// </summary>
        public async Task<IEnumerable<CityInfo>> GetRecentCitiesAsync(int limit = 10, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_historyRepository == null)
                return Enumerable.Empty<CityInfo>();

            var entries = await _historyRepository.GetAllAsync();
            
            return entries
                .OrderByDescending(e => e.LastUsed)
                .Take(limit)
                .Select(e => GetCityByName(e.CityId))
                .Where(c => c != null)
                .Cast<CityInfo>()
                .ToList();
        }

        /// <summary>
        /// Сохранить город в историю поиска
        /// </summary>
        public async Task SaveToHistoryAsync(CityInfo city, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_historyRepository == null || city == null)
                return;

            var existing = await _historyRepository.GetByCityIdAsync(city.Name);
            
            if (existing != null)
            {
                existing.LastUsed = DateTime.UtcNow;
                existing.UseCount++;
                await _historyRepository.UpdateAsync(existing);
            }
            else
            {
                await _historyRepository.AddAsync(new SearchHistoryEntry
                {
                    CityId = city.Name,
                    LastUsed = DateTime.UtcNow,
                    UseCount = 1
                });
            }
        }
    }
}