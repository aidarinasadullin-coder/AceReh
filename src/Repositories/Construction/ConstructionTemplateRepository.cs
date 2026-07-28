using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Repositories.Construction
{
    /// <summary>
    /// Репозиторий для сохранения и загрузки шаблонов конструкций
    /// </summary>
    public class ConstructionTemplateRepository : IConstructionTemplateRepository
    {
        private readonly string _dataPath;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private List<ConstructionTemplate>? _templates;
        private ConstructionTemplatesMeta _meta = new();

        /// <summary>
        /// Создать репозиторий
        /// </summary>
        /// <param name="dataPath">Путь к файлу construction_templates.json (опционально)</param>
        public ConstructionTemplateRepository(string? dataPath = null)
            : this(dataPath, Environment.GetFolderPath)
        {
        }

        internal ConstructionTemplateRepository(
            string? dataPath,
            Func<Environment.SpecialFolder, Environment.SpecialFolderOption, string> folderResolver)
        {
            if (dataPath == null)
            {
                var localApplicationData = folderResolver(
                    Environment.SpecialFolder.LocalApplicationData,
                    Environment.SpecialFolderOption.Create);
                var dataDirectory = Path.Combine(localApplicationData, "SnowMeltingCalculator", "data");
                Directory.CreateDirectory(dataDirectory);
                _dataPath = Path.Combine(
                    dataDirectory,
                    "construction_templates.json");
            }
            else
            {
                _dataPath = dataPath;
            }
        }

        /// <summary>
        /// Загрузить все шаблоны конструкций
        /// </summary>
        public async Task<IEnumerable<ConstructionTemplate>> GetAllAsync()
        {
            await EnsureLoadedAsync();
            return _templates!.AsEnumerable();
        }

        /// <summary>
        /// Получить шаблон по идентификатору
        /// </summary>
        public async Task<ConstructionTemplate?> GetByIdAsync(int id)
        {
            await EnsureLoadedAsync();
            return _templates!.FirstOrDefault(t => t.Id == id);
        }

        /// <summary>
        /// Добавить новый шаблон
        /// </summary>
        public async Task<ConstructionTemplate> AddAsync(ConstructionTemplate template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            await EnsureLoadedAsync();
            await _semaphore.WaitAsync();
            try
            {
                ValidateNameUnique(template.Name);

                template.Id = _meta.NextTemplateId++;
                _templates!.Add(template);

                return template;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Обновить существующий шаблон
        /// </summary>
        public async Task<ConstructionTemplate> UpdateAsync(ConstructionTemplate template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            await EnsureLoadedAsync();
            await _semaphore.WaitAsync();
            try
            {
                var existing = _templates!.FirstOrDefault(t => t.Id == template.Id)
                    ?? throw new InvalidOperationException($"Шаблон с id={template.Id} не найден.");

                ValidateNameUnique(template.Name, template.Id);

                existing.Name = template.Name;
                existing.Description = template.Description;
                existing.HasLoads = template.HasLoads;
                existing.DefaultGroundwaterLevel = template.DefaultGroundwaterLevel;
                existing.LayersAbovePipe = template.LayersAbovePipe;
                existing.LayersBelowPipe = template.LayersBelowPipe;
                existing.IsBuiltIn = template.IsBuiltIn;

                return existing;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Удалить шаблон по идентификатору
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            await EnsureLoadedAsync();
            await _semaphore.WaitAsync();
            try
            {
                var template = _templates!.FirstOrDefault(t => t.Id == id);
                if (template == null)
                    return false;

                _templates!.Remove(template);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Сохранить все шаблоны в JSON файл атомарно
        /// </summary>
        public async Task SaveAsync()
        {
            await EnsureLoadedAsync();
            await _semaphore.WaitAsync();
            try
            {
                await SaveCoreAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Загрузить шаблоны из файла (однократно)
        /// </summary>
        private async Task LoadTemplatesAsync()
        {
            if (_templates != null)
                return;

            await _semaphore.WaitAsync();
            try
            {
                if (_templates != null)
                    return;

                try
                {
                    var jsonContent = await File.ReadAllTextAsync(_dataPath);

                    var options = CreateJsonOptions();
                    var dbModel = JsonSerializer.Deserialize<ConstructionTemplatesDbModel>(jsonContent, options);
                    var defaultTemplates = ConstructionTemplate.GetDefaultTemplates();

                    _meta = dbModel?.Meta ?? new ConstructionTemplatesMeta();
                    _templates = dbModel?.Templates?.Select(t => MapToTemplate(t, defaultTemplates)).ToList()
                        ?? defaultTemplates;

                    EnsureNextTemplateIdSeeded();
                }
                catch (FileNotFoundException)
                {
                    var defaultTemplates = ConstructionTemplate.GetDefaultTemplates();
                    foreach (var template in defaultTemplates)
                    {
                        template.IsBuiltIn = true;
                    }

                    _templates = defaultTemplates;
                    _meta = CreateDefaultMeta();

                    await SaveCoreAsync();
                }
                catch (JsonException ex)
                {
                    throw new JsonException($"Ошибка десериализации файла шаблонов конструкций: {ex.Message}");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Ядро сохранения: сериализация и атомарная запись файла без семафора
        /// </summary>
        private async Task SaveCoreAsync()
        {
            var dbModel = new ConstructionTemplatesDbModel
            {
                Meta = _meta,
                Templates = _templates!.Select(MapToJsonModel).ToList()
            };

            var options = CreateJsonOptions();
            options.WriteIndented = true;

            var directory = Path.GetDirectoryName(_dataPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = _dataPath + ".tmp";
            var jsonContent = JsonSerializer.Serialize(dbModel, options);
            await File.WriteAllTextAsync(tempPath, jsonContent);
            File.Move(tempPath, _dataPath, overwrite: true);
        }

        /// <summary>
        /// Убедиться, что шаблоны загружены
        /// </summary>
        private async Task EnsureLoadedAsync()
        {
            if (_templates == null)
                await LoadTemplatesAsync();
        }

        /// <summary>
        /// Проверить уникальность названия шаблона (без учёта регистра)
        /// </summary>
        private void ValidateNameUnique(string name, int? excludeId = null)
        {
            var duplicate = _templates!.FirstOrDefault(t =>
                t.Id != excludeId &&
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
                throw new InvalidOperationException($"Шаблон с названием '{name}' уже существует.");
        }

        /// <summary>
        /// Установить счётчик идентификаторов, если он ещё не задан
        /// </summary>
        private void EnsureNextTemplateIdSeeded()
        {
            if (_meta.NextTemplateId > 0)
                return;

            var maxId = _templates?.Any() == true ? _templates.Max(t => t.Id) : 0;
            _meta.NextTemplateId = maxId + 1;
        }

        /// <summary>
        /// Маппинг из JSON модели в ConstructionTemplate
        /// </summary>
        private static ConstructionTemplate MapToTemplate(ConstructionTemplateJsonModel jsonModel, List<ConstructionTemplate> defaultTemplates)
        {
            var isBuiltIn = jsonModel.IsBuiltIn ?? IsDefaultTemplate(jsonModel, defaultTemplates);

            return new ConstructionTemplate
            {
                Id = jsonModel.Id,
                Name = jsonModel.Name ?? string.Empty,
                Description = jsonModel.Description ?? string.Empty,
                HasLoads = jsonModel.HasLoads,
                DefaultGroundwaterLevel = jsonModel.DefaultGroundwaterLevel,
                IsBuiltIn = isBuiltIn,
                LayersAbovePipe = jsonModel.LayersAbovePipe?.Select(MapToLayerTemplate).ToList() ?? new List<LayerTemplate>(),
                LayersBelowPipe = jsonModel.LayersBelowPipe?.Select(MapToLayerTemplate).ToList() ?? new List<LayerTemplate>(),
                MaterialSnapshots = jsonModel.MaterialSnapshots ?? new List<MaterialSnapshot>()
            };
        }

        /// <summary>
        /// Маппинг из ConstructionTemplate в JSON модель
        /// </summary>
        private static ConstructionTemplateJsonModel MapToJsonModel(ConstructionTemplate template)
        {
            return new ConstructionTemplateJsonModel
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                HasLoads = template.HasLoads,
                DefaultGroundwaterLevel = template.DefaultGroundwaterLevel,
                IsBuiltIn = template.IsBuiltIn,
                LayersAbovePipe = template.LayersAbovePipe.Select(MapToJsonLayer).ToList(),
                LayersBelowPipe = template.LayersBelowPipe.Select(MapToJsonLayer).ToList(),
                MaterialSnapshots = template.MaterialSnapshots
            };
        }

        /// <summary>
        /// Маппинг слоя из JSON
        /// </summary>
        private static LayerTemplate MapToLayerTemplate(LayerTemplateJsonModel jsonModel)
        {
            return new LayerTemplate
            {
                MaterialId = jsonModel.MaterialId,
                Thickness = jsonModel.Thickness,
                Position = jsonModel.Position,
                Order = jsonModel.Order
            };
        }

        /// <summary>
        /// Маппинг слоя в JSON
        /// </summary>
        private static LayerTemplateJsonModel MapToJsonLayer(LayerTemplate layer)
        {
            return new LayerTemplateJsonModel
            {
                MaterialId = layer.MaterialId,
                Thickness = layer.Thickness,
                Position = layer.Position,
                Order = layer.Order
            };
        }

        /// <summary>
        /// Проверить, соответствует ли JSON-шаблон одному из шаблонов по умолчанию
        /// по идентификатору и названию (без учёта регистра)
        /// </summary>
        private static bool IsDefaultTemplate(ConstructionTemplateJsonModel jsonModel, List<ConstructionTemplate> defaultTemplates)
        {
            return defaultTemplates.Any(defaultTemplate =>
                defaultTemplate.Id == jsonModel.Id &&
                string.Equals(defaultTemplate.Name, jsonModel.Name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Создать метаданные по умолчанию для нового файла
        /// </summary>
        private static ConstructionTemplatesMeta CreateDefaultMeta()
        {
            var defaultTemplates = ConstructionTemplate.GetDefaultTemplates();
            return new ConstructionTemplatesMeta
            {
                Source = "ConstructionTemplate.GetDefaultTemplates()",
                Version = "1.0",
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Description = "База шаблонов конструкций для расчёта систем снеготаяния",
                NextTemplateId = defaultTemplates.Max(t => t.Id) + 1
            };
        }

        /// <summary>
        /// Создать опции сериализации JSON с snake_case
        /// </summary>
        private static JsonSerializerOptions CreateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
            };
        }

        #region JSON Models

        /// <summary>
        /// Модель корневого объекта JSON
        /// </summary>
        private class ConstructionTemplatesDbModel
        {
            [JsonPropertyName("meta")]
            public ConstructionTemplatesMeta? Meta { get; set; }

            [JsonPropertyName("templates")]
            public List<ConstructionTemplateJsonModel>? Templates { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement>? ExtensionData { get; set; }
        }

        /// <summary>
        /// Модель метаданных
        /// </summary>
        private class ConstructionTemplatesMeta
        {
            [JsonPropertyName("source")]
            public string? Source { get; set; }

            [JsonPropertyName("version")]
            public string? Version { get; set; }

            [JsonPropertyName("date")]
            public string? Date { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("next_template_id")]
            public int NextTemplateId { get; set; }
        }

        /// <summary>
        /// Модель шаблона конструкции в JSON
        /// </summary>
        private class ConstructionTemplateJsonModel
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("has_loads")]
            public bool HasLoads { get; set; }

            [JsonPropertyName("default_groundwater_level")]
            public double DefaultGroundwaterLevel { get; set; }

            [JsonPropertyName("is_built_in")]
            public bool? IsBuiltIn { get; set; }

            [JsonPropertyName("layers_above_pipe")]
            public List<LayerTemplateJsonModel>? LayersAbovePipe { get; set; }

            [JsonPropertyName("layers_below_pipe")]
            public List<LayerTemplateJsonModel>? LayersBelowPipe { get; set; }

            [JsonPropertyName("material_snapshots")]
            public List<MaterialSnapshot>? MaterialSnapshots { get; set; }
        }

        /// <summary>
        /// Модель слоя шаблона в JSON
        /// </summary>
        private class LayerTemplateJsonModel
        {
            [JsonPropertyName("material_id")]
            public int MaterialId { get; set; }

            [JsonPropertyName("thickness")]
            public double Thickness { get; set; }

            [JsonPropertyName("position")]
            public LayerPosition Position { get; set; }

            [JsonPropertyName("order")]
            public int Order { get; set; }
        }

        #endregion
    }
}
