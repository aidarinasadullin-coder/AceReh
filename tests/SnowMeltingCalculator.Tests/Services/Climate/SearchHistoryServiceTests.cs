using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Tests.Services.Climate
{
    /// <summary>
    /// Тесты для SearchHistoryService
    /// </summary>
    [TestFixture]
    public class SearchHistoryServiceTests
    {
        private SearchHistoryService _service = null!;
        private MockSearchHistoryRepository _mockRepository = null!;
        private MockClimateDataService _mockClimateService = null!;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new MockSearchHistoryRepository();
            _mockClimateService = new MockClimateDataService();
            _service = new SearchHistoryService(_mockRepository, _mockClimateService);
        }

        #region GetRecentAsync Tests

        [Test]
        public async Task GetRecentAsync_ReturnsSortedByLastUsed()
        {
            // Arrange
            await _mockRepository.AddAsync(new SearchHistoryEntry { CityId = "Москва", LastUsed = DateTime.UtcNow.AddDays(-2), UseCount = 1 });
            await _mockRepository.AddAsync(new SearchHistoryEntry { CityId = "Санкт-Петербург", LastUsed = DateTime.UtcNow.AddDays(-1), UseCount = 1 });
            await _mockRepository.AddAsync(new SearchHistoryEntry { CityId = "Сочи", LastUsed = DateTime.UtcNow, UseCount = 1 });

            // Act
            var results = await _service.GetRecentAsync(10);
            var resultsList = results.ToList();

            // Assert
            Assert.That(resultsList.Count, Is.EqualTo(3));
            Assert.That(resultsList[0].CityId, Is.EqualTo("Сочи"));
            Assert.That(resultsList[1].CityId, Is.EqualTo("Санкт-Петербург"));
            Assert.That(resultsList[2].CityId, Is.EqualTo("Москва"));
        }

        [Test]
        public async Task GetRecentAsync_RespectsLimit()
        {
            // Arrange - используем города, которые есть в MockClimateDataService
            await _mockRepository.AddAsync(new SearchHistoryEntry
            {
                CityId = "Москва",
                LastUsed = DateTime.UtcNow.AddDays(-1),
                UseCount = 1
            });
            await _mockRepository.AddAsync(new SearchHistoryEntry
            {
                CityId = "Санкт-Петербург",
                LastUsed = DateTime.UtcNow.AddDays(-2),
                UseCount = 1
            });
            await _mockRepository.AddAsync(new SearchHistoryEntry
            {
                CityId = "Сочи",
                LastUsed = DateTime.UtcNow.AddDays(-3),
                UseCount = 1
            });

            // Act
            var results = await _service.GetRecentAsync(2);
            var resultsList = results.ToList();

            // Assert
            Assert.That(resultsList.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetRecentAsync_DefaultLimit_Is10()
        {
            // Arrange - используем города, которые есть в MockClimateDataService
            await _mockRepository.AddAsync(new SearchHistoryEntry { CityId = "Москва", LastUsed = DateTime.UtcNow, UseCount = 1 });
            await _mockRepository.AddAsync(new SearchHistoryEntry { CityId = "Санкт-Петербург", LastUsed = DateTime.UtcNow.AddDays(-1), UseCount = 1 });
            await _mockRepository.AddAsync(new SearchHistoryEntry { CityId = "Сочи", LastUsed = DateTime.UtcNow.AddDays(-2), UseCount = 1 });

            // Act
            var results = await _service.GetRecentAsync();
            var resultsList = results.ToList();

            // Assert
            Assert.That(resultsList.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task GetRecentAsync_FiltersOutNonExistingCities()
        {
            // Arrange
            await _mockRepository.AddAsync(new SearchHistoryEntry { CityId = "Москва", LastUsed = DateTime.UtcNow, UseCount = 1 });
            await _mockRepository.AddAsync(new SearchHistoryEntry { CityId = "НесуществующийГород", LastUsed = DateTime.UtcNow, UseCount = 1 });

            // Act
            var results = await _service.GetRecentAsync(10);
            var resultsList = results.ToList();

            // Assert
            Assert.That(resultsList.Count, Is.EqualTo(1));
            Assert.That(resultsList[0].CityId, Is.EqualTo("Москва"));
        }

        [Test]
        public async Task GetRecentAsync_EmptyHistory_ReturnsEmpty()
        {
            // Act
            var results = await _service.GetRecentAsync(10);

            // Assert
            Assert.That(results.Count(), Is.EqualTo(0));
        }

        #endregion

        #region AddAsync Tests

        [Test]
        public async Task AddAsync_NewEntry_CreatesEntry()
        {
            // Act
            await _service.AddAsync("Москва");

            // Assert
            var entries = await _mockRepository.GetAllAsync();
            var entry = entries.FirstOrDefault(e => e.CityId == "Москва");

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.UseCount, Is.EqualTo(1));
        }

        [Test]
        public async Task AddAsync_ExistingEntry_IncrementsUseCount()
        {
            // Arrange
            await _mockRepository.AddAsync(new SearchHistoryEntry
            {
                CityId = "Москва",
                LastUsed = DateTime.UtcNow.AddDays(-1),
                UseCount = 1
            });

            // Act
            await _service.AddAsync("Москва");

            // Assert
            var entries = await _mockRepository.GetAllAsync();
            var entry = entries.FirstOrDefault(e => e.CityId == "Москва");

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.UseCount, Is.EqualTo(2));
        }

        [Test]
        public async Task AddAsync_ExistingEntry_UpdatesLastUsed()
        {
            // Arrange
            var oldTime = DateTime.UtcNow.AddDays(-1);
            await _mockRepository.AddAsync(new SearchHistoryEntry
            {
                CityId = "Москва",
                LastUsed = oldTime,
                UseCount = 1
            });

            // Act
            await _service.AddAsync("Москва");

            // Assert
            var entries = await _mockRepository.GetAllAsync();
            var entry = entries.FirstOrDefault(e => e.CityId == "Москва");

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.LastUsed, Is.GreaterThan(oldTime));
        }

        [Test]
        public async Task AddAsync_EmptyCityId_DoesNothing()
        {
            // Act
            await _service.AddAsync("");

            // Assert
            var entries = await _mockRepository.GetAllAsync();
            Assert.That(entries.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task AddAsync_NullCityId_DoesNothing()
        {
            // Act
            await _service.AddAsync(null!);

            // Assert
            var entries = await _mockRepository.GetAllAsync();
            Assert.That(entries.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task AddAsync_WhitespaceCityId_DoesNothing()
        {
            // Act
            await _service.AddAsync("   ");

            // Assert
            var entries = await _mockRepository.GetAllAsync();
            Assert.That(entries.Count(), Is.EqualTo(0));
        }

        #endregion

        #region ClearAsync Tests

        [Test]
        public async Task ClearAsync_RemovesAllEntries()
        {
            // Arrange
            await _mockRepository.AddAsync(new SearchHistoryEntry { CityId = "Москва", LastUsed = DateTime.UtcNow, UseCount = 1 });
            await _mockRepository.AddAsync(new SearchHistoryEntry { CityId = "Санкт-Петербург", LastUsed = DateTime.UtcNow, UseCount = 1 });

            // Act
            await _service.ClearAsync();

            // Assert
            var entries = await _mockRepository.GetAllAsync();
            Assert.That(entries.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task ClearAsync_EmptyHistory_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _service.ClearAsync());
        }

        #endregion
    }

    /// <summary>
    /// Мок-репозиторий истории поиска для тестов
    /// </summary>
    internal class MockSearchHistoryRepository : ISearchHistoryRepository
    {
        private readonly List<SearchHistoryEntry> _entries = new();
        private int _nextId = 1;

        public Task<IEnumerable<SearchHistoryEntry>> GetAllAsync()
        {
            return Task.FromResult(_entries.AsEnumerable());
        }

        public Task<SearchHistoryEntry?> GetByIdAsync(int id)
        {
            return Task.FromResult(_entries.FirstOrDefault(e => e.Id == id));
        }

        public Task<SearchHistoryEntry?> GetByCityIdAsync(string cityId)
        {
            return Task.FromResult(_entries.FirstOrDefault(e =>
                e.CityId.Equals(cityId, StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddAsync(SearchHistoryEntry entry)
        {
            entry.Id = _nextId++;
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SearchHistoryEntry entry)
        {
            var existing = _entries.FirstOrDefault(e => e.Id == entry.Id);
            if (existing != null)
            {
                existing.LastUsed = entry.LastUsed;
                existing.UseCount = entry.UseCount;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == id);
            if (entry != null)
            {
                _entries.Remove(entry);
            }
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _entries.Clear();
            return Task.CompletedTask;
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Мок-сервис климатических данных для тестов
    /// </summary>
    internal class MockClimateDataService : IClimateDataService
    {
        private readonly List<CityInfo> _cities = new()
        {
            new CityInfo { Name = "Москва", Region = "Московская область", T5Days092 = -28 },
            new CityInfo { Name = "Санкт-Петербург", Region = "Ленинградская область", T5Days092 = -26 },
            new CityInfo { Name = "Сочи", Region = "Краснодарский край", T5Days092 = -5 }
        };

        public bool IsLoaded => true;

        public int CitiesCount => _cities.Count;

        public Task LoadClimateDataAsync() => Task.CompletedTask;

        public Task<IEnumerable<CityInfo>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Task.FromResult(Enumerable.Empty<CityInfo>());

            return Task.FromResult(_cities
                .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           c.Region.Contains(query, StringComparison.OrdinalIgnoreCase))
                .AsEnumerable());
        }

        public Task<IEnumerable<CityInfo>> SearchCitiesWithPriorityAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
                return Task.FromResult(Enumerable.Empty<CityInfo>());

            return Task.FromResult(_cities
                .Where(c => c.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ||
                           c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           c.Region.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(15)
                .AsEnumerable());
        }

        public (string highlightedName, string highlightedRegion, MatchType matchType) HighlightMatch(CityInfo city, string query)
        {
            if (city == null)
                return (string.Empty, string.Empty, MatchType.Contains);

            if (string.IsNullOrWhiteSpace(query))
                return (city.Name, city.Region, MatchType.Contains);

            var index = city.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var before = city.Name.Substring(0, index);
                var match = city.Name.Substring(index, query.Length);
                var after = city.Name.Substring(index + query.Length);
                var highlightedName = $"{before}**{match}**{after}";

                var matchType = city.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                    ? MatchType.StartsWith
                    : MatchType.Contains;

                return (highlightedName, city.Region, matchType);
            }

            var regionIndex = city.Region.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (regionIndex >= 0)
            {
                var before = city.Region.Substring(0, regionIndex);
                var match = city.Region.Substring(regionIndex, query.Length);
                var after = city.Region.Substring(regionIndex + query.Length);
                var highlightedRegion = $"{before}**{match}**{after}";

                return (city.Name, highlightedRegion, MatchType.Region);
            }

            return (city.Name, city.Region, MatchType.Contains);
        }

        public Task<IEnumerable<CityInfo>> GetRecentCitiesAsync(int limit = 10, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<CityInfo>());
        }

        public Task SaveToHistoryAsync(CityInfo city, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public CityInfo? GetCityByName(string name)
        {
            return _cities.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<CityInfo> GetAllCities() => _cities.AsEnumerable();

        public ClimateZone DetermineZone(double t5days, bool isHighRequirements = false)
        {
            if (isHighRequirements)
                return ClimateZone.Zone_M20_Plus;

            if (t5days >= -27)
                return ClimateZone.Zone_M10;

            if (t5days > -37)
                return ClimateZone.Zone_M15;

            return ClimateZone.Zone_M20;
        }
    }
}