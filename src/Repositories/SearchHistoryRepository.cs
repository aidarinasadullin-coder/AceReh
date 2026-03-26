using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Repositories
{
    /// <summary>
    /// Репозиторий для работы с историей поиска городов в SQLite
    /// </summary>
    public class SearchHistoryRepository : ISearchHistoryRepository
    {
        private readonly string _connectionString;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _initSemaphore = new(1, 1);

        /// <summary>
        /// Создать репозиторий
        /// </summary>
        /// <param name="connectionString">Строка подключения к SQLite</param>
        public SearchHistoryRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Создать репозиторий с путём к файлу базы данных
        /// </summary>
        /// <param name="dbPath">Путь к файлу базы данных</param>
        public static SearchHistoryRepository Create(string dbPath)
        {
            var connectionString = $"Data Source={dbPath}";
            return new SearchHistoryRepository(connectionString);
        }

        /// <summary>
        /// Инициализировать таблицу
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _initSemaphore.WaitAsync();
            try
            {
                if (_isInitialized) return;

                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var createTableSql = @"
                    CREATE TABLE IF NOT EXISTS SearchHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CityId TEXT NOT NULL UNIQUE,
                        LastUsed TEXT NOT NULL,
                        UseCount INTEGER NOT NULL DEFAULT 1
                    );
                    
                    CREATE INDEX IF NOT EXISTS IX_SearchHistory_LastUsed 
                    ON SearchHistory(LastUsed DESC);
                ";

                await using var command = new SqliteCommand(createTableSql, connection);
                await command.ExecuteNonQueryAsync();

                _isInitialized = true;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        /// <summary>
        /// Получить все записи истории
        /// </summary>
        public async Task<IEnumerable<SearchHistoryEntry>> GetAllAsync()
        {
            await EnsureInitializedAsync();

            var entries = new List<SearchHistoryEntry>();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT Id, CityId, LastUsed, UseCount FROM SearchHistory ORDER BY LastUsed DESC";
            await using var command = new SqliteCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                entries.Add(new SearchHistoryEntry
                {
                    Id = reader.GetInt32(0),
                    CityId = reader.GetString(1),
                    LastUsed = DateTime.Parse(reader.GetString(2)),
                    UseCount = reader.GetInt32(3)
                });
            }

            return entries;
        }

        /// <summary>
        /// Получить запись по идентификатору
        /// </summary>
        public async Task<SearchHistoryEntry?> GetByIdAsync(int id)
        {
            await EnsureInitializedAsync();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT Id, CityId, LastUsed, UseCount FROM SearchHistory WHERE Id = @Id";
            await using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new SearchHistoryEntry
                {
                    Id = reader.GetInt32(0),
                    CityId = reader.GetString(1),
                    LastUsed = DateTime.Parse(reader.GetString(2)),
                    UseCount = reader.GetInt32(3)
                };
            }

            return null;
        }

        /// <summary>
        /// Получить запись по идентификатору города
        /// </summary>
        public async Task<SearchHistoryEntry?> GetByCityIdAsync(string cityId)
        {
            await EnsureInitializedAsync();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT Id, CityId, LastUsed, UseCount FROM SearchHistory WHERE CityId = @CityId";
            await using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@CityId", cityId);
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new SearchHistoryEntry
                {
                    Id = reader.GetInt32(0),
                    CityId = reader.GetString(1),
                    LastUsed = DateTime.Parse(reader.GetString(2)),
                    UseCount = reader.GetInt32(3)
                };
            }

            return null;
        }

        /// <summary>
        /// Добавить запись в историю
        /// </summary>
        public async Task AddAsync(SearchHistoryEntry entry)
        {
            await EnsureInitializedAsync();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO SearchHistory (CityId, LastUsed, UseCount)
                VALUES (@CityId, @LastUsed, @UseCount)
            ";
            await using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@CityId", entry.CityId);
            command.Parameters.AddWithValue("@LastUsed", entry.LastUsed.ToString("o"));
            command.Parameters.AddWithValue("@UseCount", entry.UseCount);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Обновить запись в истории
        /// </summary>
        public async Task UpdateAsync(SearchHistoryEntry entry)
        {
            await EnsureInitializedAsync();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                UPDATE SearchHistory
                SET LastUsed = @LastUsed, UseCount = @UseCount
                WHERE Id = @Id
            ";
            await using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", entry.Id);
            command.Parameters.AddWithValue("@LastUsed", entry.LastUsed.ToString("o"));
            command.Parameters.AddWithValue("@UseCount", entry.UseCount);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Удалить запись из истории
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            await EnsureInitializedAsync();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "DELETE FROM SearchHistory WHERE Id = @Id";
            await using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Очистить всю историю
        /// </summary>
        public async Task ClearAsync()
        {
            await EnsureInitializedAsync();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "DELETE FROM SearchHistory";
            await using var command = new SqliteCommand(sql, connection);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Убедиться, что таблица инициализирована
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
        }
    }
}