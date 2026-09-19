// ================================================================================
// REHAU Снеготаяние - Репозиторий фитингов (секция «fittings» rehau_products.json)
// ================================================================================
//
// Назначение: загрузка резьбозажимных соединений из data/rehau_products.json.
// Паттерн — CollectorRepository (тот же файл данных, кэш, fallback на пустой
// список: спецификация продолжит с пустым артикулом, экспорт не блокируется).
// Поля json в snake_case — маппинг через JsonPropertyName (PropertyNameCaseInsensitive
// подчёркивания не убирает).
//
// ================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Fittings;

using SnowMeltingCalculator.Services.Logging;
namespace SnowMeltingCalculator.Repositories.Fittings
{
    public class FittingsRepository : IFittingsRepository
    {
        private readonly string _dataFilePath;
        private List<RzsFitting>? _cachedFittings;
        private readonly object _lockObject = new();

        public FittingsRepository() : this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "rehau_products.json"))
        {
        }

        public FittingsRepository(string dataFilePath)
        {
            _dataFilePath = dataFilePath;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<RzsFitting>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                if (_cachedFittings != null)
                    return _cachedFittings;
            }

            if (!File.Exists(_dataFilePath))
            {
                return CacheAndReturn(new List<RzsFitting>());
            }

            try
            {
                var json = await File.ReadAllTextAsync(_dataFilePath, cancellationToken);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var container = JsonSerializer.Deserialize<FittingsContainer>(json, options);

                var fittings = new List<RzsFitting>();
                if (container?.Fittings != null)
                {
                    foreach (var f in container.Fittings)
                    {
                        fittings.Add(new RzsFitting
                        {
                            Id = f.Id ?? string.Empty,
                            Name = f.Name ?? string.Empty,
                            FullName = f.FullName ?? f.Name ?? string.Empty,
                            ArticleNumber = string.IsNullOrWhiteSpace(f.ArticleNumber) ? null : f.ArticleNumber,
                            PipeOuterDiameterMm = f.PipeOuterDiameterMm,
                            PipeWallThicknessMm = f.PipeWallThicknessMm,
                            Notes = f.Notes
                        });
                    }
                }

                return CacheAndReturn(fittings);
            }
            catch (Exception ex) {
                AppLog.Warn(ex, "FittingsRepository.GetAllAsync");
                // Ошибка парсинга не должна блокировать экспорт спецификации —
                // вернём пустой каталог (артикулы РЗС будут пустыми).
                return CacheAndReturn(new List<RzsFitting>());
            }
        }

        private IReadOnlyList<RzsFitting> CacheAndReturn(List<RzsFitting> fittings)
        {
            lock (_lockObject)
            {
                _cachedFittings = fittings;
            }

            return fittings;
        }

        #region JSON Data Models

        private sealed class FittingsContainer
        {
            [JsonPropertyName("fittings")]
            public List<FittingJson>? Fittings { get; set; }
        }

        private sealed class FittingJson
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("full_name")]
            public string? FullName { get; set; }

            [JsonPropertyName("article_number")]
            public string? ArticleNumber { get; set; }

            [JsonPropertyName("pipe_outer_diameter_mm")]
            public double PipeOuterDiameterMm { get; set; }

            [JsonPropertyName("pipe_wall_thickness_mm")]
            public double PipeWallThicknessMm { get; set; }

            [JsonPropertyName("notes")]
            public string? Notes { get; set; }
        }

        #endregion
    }
}
