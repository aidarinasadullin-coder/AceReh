using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Repositories;

namespace SnowMeltingCalculator.Services.Climate
{
    /// <summary>
    /// Сервис для управления историей поиска городов
    /// </summary>
    public class SearchHistoryService : ISearchHistoryService
    {
        private readonly ISearchHistoryRepository _repository;
        private readonly IClimateDataService _climateService;

        /// <summary>
        /// Создать сервис истории поиска
        /// </summary>
        public SearchHistoryService(
            ISearchHistoryRepository repository,
            IClimateDataService climateService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _climateService = climateService ?? throw new ArgumentNullException(nameof(climateService));
        }

        /// <summary>
        /// Получить последние N городов из истории
        /// </summary>
        public async Task<IEnumerable<SearchHistoryEntry>> GetRecentAsync(int limit = 10, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entries = await _repository.GetAllAsync();

            return entries
                .OrderByDescending(e => e.LastUsed)
                .Take(limit)
                .Select(e =>
                {
                    e.City = _climateService.GetCityByName(e.CityId);
                    return e;
                })
                .Where(e => e.City != null)
                .ToList();
        }

        /// <summary>
        /// Добавить или обновить запись в истории
        /// </summary>
        public async Task AddAsync(string cityId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(cityId))
                return;

            var existing = await _repository.GetByCityIdAsync(cityId);

            if (existing != null)
            {
                existing.LastUsed = DateTime.UtcNow;
                existing.UseCount++;
                await _repository.UpdateAsync(existing);
            }
            else
            {
                await _repository.AddAsync(new SearchHistoryEntry
                {
                    CityId = cityId,
                    LastUsed = DateTime.UtcNow,
                    UseCount = 1
                });
            }
        }

        /// <summary>
        /// Очистить историю поиска
        /// </summary>
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _repository.ClearAsync();
        }
    }
}