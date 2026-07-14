using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Repositories;

namespace SnowMeltingCalculator.Tests.Repositories.Climate
{
    /// <summary>
    /// Тесты для SearchHistoryRepository
    /// </summary>
    [TestFixture]
    public class SearchHistoryRepositoryTests
    {
        private string _dbPath = string.Empty;
        private SearchHistoryRepository _repository = null!;

        [SetUp]
        public void Setup()
        {
            // Создаём уникальный путь к БД для каждого теста
            var tempPath = Path.Combine(Path.GetTempPath(), "SnowMeltingCalculator_Tests");
            Directory.CreateDirectory(tempPath);
            _dbPath = Path.Combine(tempPath, $"test_{Guid.NewGuid():N}.db");
            _repository = SearchHistoryRepository.Create(_dbPath);
        }

        [TearDown]
        public void TearDown()
        {
            // Освобождаем ресурсы и ждём завершения GC
            _repository = null!;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Удаляем тестовую БД
            if (File.Exists(_dbPath))
            {
                // Пробуем удалить несколько раз, так как SQLite может держать файл
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        File.Delete(_dbPath);
                        break;
                    }
                    catch (IOException)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
        }

        #region UT-001: InitializeAsync

        [Test]
        public async Task InitializeAsync_ShouldCreateTable()
        {
            // Act
            await _repository.InitializeAsync();

            // Assert
            Assert.That(File.Exists(_dbPath), Is.True);
        }

        [Test]
        public async Task InitializeAsync_ShouldBeIdempotent()
        {
            // Act - вызываем дважды
            await _repository.InitializeAsync();
            await _repository.InitializeAsync();

            // Assert - не должно быть исключения
            Assert.That(File.Exists(_dbPath), Is.True);
        }

        #endregion

        #region UT-002: AddAsync - новая запись

        [Test]
        public async Task AddAsync_ShouldAddNewEntry()
        {
            // Arrange
            await _repository.InitializeAsync();
            var entry = new SearchHistoryEntry
            {
                CityId = "Москва",
                LastUsed = DateTime.UtcNow,
                UseCount = 1
            };

            // Act
            await _repository.AddAsync(entry);

            // Assert
            var allEntries = await _repository.GetAllAsync();
            Assert.That(allEntries.Count(), Is.EqualTo(1));
            var savedEntry = allEntries.First();
            Assert.That(savedEntry.CityId, Is.EqualTo("Москва"));
            Assert.That(savedEntry.UseCount, Is.EqualTo(1));
        }

        #endregion

        #region UT-003: AddAsync - дубликат CityId

        [Test]
        public async Task AddAsync_DuplicateCityId_ShouldThrowException()
        {
            // Arrange
            await _repository.InitializeAsync();
            var entry1 = new SearchHistoryEntry
            {
                CityId = "Москва",
                LastUsed = DateTime.UtcNow,
                UseCount = 1
            };
            var entry2 = new SearchHistoryEntry
            {
                CityId = "Москва",
                LastUsed = DateTime.UtcNow,
                UseCount = 2
            };

            // Act & Assert
            await _repository.AddAsync(entry1);
            var ex = Assert.ThrowsAsync<SqliteException>(() => _repository.AddAsync(entry2));
            Assert.That(ex!.Message, Does.Contain("UNIQUE constraint failed"));
        }

        #endregion

        #region UT-004: GetByIdAsync

        [Test]
        public async Task GetByIdAsync_ShouldReturnEntry()
        {
            // Arrange
            await _repository.InitializeAsync();
            var entry = new SearchHistoryEntry
            {
                CityId = "Москва",
                LastUsed = DateTime.UtcNow,
                UseCount = 5
            };
            await _repository.AddAsync(entry);

            // Получаем Id добавленной записи
            var allEntries = await _repository.GetAllAsync();
            var addedEntry = allEntries.First();

            // Act
            var result = await _repository.GetByIdAsync(addedEntry.Id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.CityId, Is.EqualTo("Москва"));
            Assert.That(result.UseCount, Is.EqualTo(5));
        }

        [Test]
        public async Task GetByIdAsync_NotFound_ShouldReturnNull()
        {
            // Arrange
            await _repository.InitializeAsync();

            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region UT-005: GetByCityIdAsync

        [Test]
        public async Task GetByCityIdAsync_ShouldReturnEntry()
        {
            // Arrange
            await _repository.InitializeAsync();
            var entry = new SearchHistoryEntry
            {
                CityId = "Санкт-Петербург",
                LastUsed = DateTime.UtcNow,
                UseCount = 3
            };
            await _repository.AddAsync(entry);

            // Act
            var result = await _repository.GetByCityIdAsync("Санкт-Петербург");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.CityId, Is.EqualTo("Санкт-Петербург"));
            Assert.That(result.UseCount, Is.EqualTo(3));
        }

        [Test]
        public async Task GetByCityIdAsync_NotFound_ShouldReturnNull()
        {
            // Arrange
            await _repository.InitializeAsync();

            // Act
            var result = await _repository.GetByCityIdAsync("Несуществующий город");

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region UT-006: GetAllAsync

        [Test]
        public async Task GetAllAsync_ShouldReturnAllEntriesOrderedByLastUsed()
        {
            // Arrange
            await _repository.InitializeAsync();
            var entry1 = new SearchHistoryEntry
            {
                CityId = "Москва",
                LastUsed = DateTime.UtcNow.AddHours(-2),
                UseCount = 1
            };
            var entry2 = new SearchHistoryEntry
            {
                CityId = "Казань",
                LastUsed = DateTime.UtcNow,
                UseCount = 1
            };
            var entry3 = new SearchHistoryEntry
            {
                CityId = "Новосибирск",
                LastUsed = DateTime.UtcNow.AddHours(-1),
                UseCount = 1
            };

            await _repository.AddAsync(entry1);
            await _repository.AddAsync(entry2);
            await _repository.AddAsync(entry3);

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            var entries = result.ToList();
            Assert.That(entries.Count, Is.EqualTo(3));
            // Порядок: по убыванию LastUsed
            Assert.That(entries[0].CityId, Is.EqualTo("Казань"));
            Assert.That(entries[1].CityId, Is.EqualTo("Новосибирск"));
            Assert.That(entries[2].CityId, Is.EqualTo("Москва"));
        }

        [Test]
        public async Task GetAllAsync_EmptyTable_ShouldReturnEmptyCollection()
        {
            // Arrange
            await _repository.InitializeAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region UT-007: UpdateAsync

        [Test]
        public async Task UpdateAsync_ShouldUpdateEntry()
        {
            // Arrange
            await _repository.InitializeAsync();
            var entry = new SearchHistoryEntry
            {
                CityId = "Москва",
                LastUsed = DateTime.UtcNow.AddHours(-1),
                UseCount = 1
            };
            await _repository.AddAsync(entry);

            var allEntries = await _repository.GetAllAsync();
            var addedEntry = allEntries.First();

            // Обновляем запись
            addedEntry.LastUsed = DateTime.UtcNow;
            addedEntry.UseCount = 10;

            // Act
            await _repository.UpdateAsync(addedEntry);

            // Assert
            var result = await _repository.GetByIdAsync(addedEntry.Id);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.UseCount, Is.EqualTo(10));
        }

        #endregion

        #region UT-008: DeleteAsync

        [Test]
        public async Task DeleteAsync_ShouldRemoveEntry()
        {
            // Arrange
            await _repository.InitializeAsync();
            var entry = new SearchHistoryEntry
            {
                CityId = "Москва",
                LastUsed = DateTime.UtcNow,
                UseCount = 1
            };
            await _repository.AddAsync(entry);

            var allEntries = await _repository.GetAllAsync();
            var addedEntry = allEntries.First();

            // Act
            await _repository.DeleteAsync(addedEntry.Id);

            // Assert
            var result = await _repository.GetByIdAsync(addedEntry.Id);
            Assert.That(result, Is.Null);
        }

        #endregion

        #region UT-009: ClearAsync

        [Test]
        public async Task ClearAsync_ShouldRemoveAllEntries()
        {
            // Arrange
            await _repository.InitializeAsync();
            for (int i = 0; i < 5; i++)
            {
                var entry = new SearchHistoryEntry
                {
                    CityId = $"Город{i}",
                    LastUsed = DateTime.UtcNow,
                    UseCount = 1
                };
                await _repository.AddAsync(entry);
            }

            // Act
            await _repository.ClearAsync();

            // Assert
            var result = await _repository.GetAllAsync();
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region Интеграционные тесты

        [Test]
        public async Task Integration_SaveAndUpdateHistory()
        {
            // Arrange
            await _repository.InitializeAsync();
            var cityId = "Москва";

            // Act - первое сохранение
            var entry = new SearchHistoryEntry
            {
                CityId = cityId,
                LastUsed = DateTime.UtcNow,
                UseCount = 1
            };
            await _repository.AddAsync(entry);

            // Act - обновление при повторном выборе
            var existingEntry = await _repository.GetByCityIdAsync(cityId);
            existingEntry!.LastUsed = DateTime.UtcNow;
            existingEntry.UseCount++;
            await _repository.UpdateAsync(existingEntry);

            // Assert
            var result = await _repository.GetByCityIdAsync(cityId);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.UseCount, Is.EqualTo(2));
        }

        [Test]
        public async Task Integration_MultipleCities_ShouldMaintainOrder()
        {
            // Arrange
            await _repository.InitializeAsync();
            var cities = new[] { "Москва", "Казань", "Новосибирск", "Екатеринбург" };

            // Act - добавляем города с разными временами
            for (int i = 0; i < cities.Length; i++)
            {
                var entry = new SearchHistoryEntry
                {
                    CityId = cities[i],
                    LastUsed = DateTime.UtcNow.AddHours(-i),
                    UseCount = 1
                };
                await _repository.AddAsync(entry);
            }

            // Assert - проверяем порядок по убыванию LastUsed
            var result = await _repository.GetAllAsync();
            var orderedCities = result.Select(e => e.CityId).ToList();
            Assert.That(orderedCities, Is.EqualTo(cities.ToList()));
        }

        #endregion
    }
}