using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Repositories.Construction
{
    /// <summary>
    /// Репозиторий для загрузки и сохранения материалов из JSON
    /// </summary>
    public class MaterialRepository : IMaterialRepository
    {
        private readonly string _dataPath;
        private List<Material>? _materials;
        private MaterialsMeta _meta = new();
        private JsonElement? _usageRulesCache;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// Признак того, что данные загружены
        /// </summary>
        public bool IsLoaded => _materials != null;

        /// <summary>
        /// Количество загруженных материалов
        /// </summary>
        public int MaterialsCount => _materials?.Count ?? 0;

        /// <summary>
        /// Создать репозиторий
        /// </summary>
        /// <param name="dataPath">Путь к файлу materials_db.json (опционально)</param>
        public MaterialRepository(string? dataPath = null)
        {
            if (dataPath == null)
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _dataPath = Path.Combine(baseDir, "data", "materials_db.json");
            }
            else
            {
                _dataPath = dataPath;
            }
        }

        /// <summary>
        /// Загрузить все материалы из базы данных
        /// </summary>
        public async Task<IEnumerable<Material>> LoadMaterialsAsync()
        {
            if (_materials != null)
                return _materials;

            await _semaphore.WaitAsync();
            try
            {
                if (_materials != null)
                    return _materials;

                try
                {
                    var jsonContent = await File.ReadAllTextAsync(_dataPath);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    };

                    var materialsData = JsonSerializer.Deserialize<MaterialsDbModel>(jsonContent, options);
                    var defaultMaterials = Material.GetDefaultMaterials();

                    _meta = materialsData?.Meta ?? new MaterialsMeta();
                    _usageRulesCache = materialsData?.UsageRules;
                    _materials = materialsData?.Materials?.Select(m => MapToMaterial(m, defaultMaterials)).ToList()
                        ?? defaultMaterials;

                    EnsureNextMaterialIdSeeded();

                    return _materials;
                }
                catch (FileNotFoundException)
                {
                    // Если файл не найден, используем материалы по умолчанию и создаём файл базы данных
                    var defaultMaterials = Material.GetDefaultMaterials();
                    foreach (var material in defaultMaterials)
                    {
                        material.IsBuiltIn = true;
                    }

                    _materials = defaultMaterials;
                    _meta = CreateDefaultMeta();
                    _usageRulesCache = null;

                    await SaveCoreAsync();

                    return _materials;
                }
                catch (JsonException ex)
                {
                    throw new JsonException($"Ошибка десериализации файла материалов: {ex.Message}");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Получить материал по идентификатору
        /// </summary>
        public Material? GetMaterialById(int id)
        {
            if (_materials == null)
            {
                throw new InvalidOperationException("Материалы не загружены. Сначала вызовите LoadMaterialsAsync().");
            }

            return _materials.FirstOrDefault(m => m.Id == id);
        }

        /// <summary>
        /// Получить материалы по категории
        /// </summary>
        public IEnumerable<Material> GetMaterialsByCategory(MaterialCategory category)
        {
            if (_materials == null)
            {
                throw new InvalidOperationException("Материалы не загружены. Сначала вызовите LoadMaterialsAsync().");
            }

            return _materials.Where(m => m.Category == category);
        }

        /// <summary>
        /// Получить все материалы (из кэша)
        /// </summary>
        public IEnumerable<Material> GetAllMaterials()
        {
            if (_materials == null)
            {
                throw new InvalidOperationException("Материалы не загружены. Сначала вызовите LoadMaterialsAsync().");
            }

            return _materials.AsEnumerable();
        }

        /// <summary>
        /// Добавить новый материал
        /// </summary>
        /// <param name="material">Материал для добавления</param>
        /// <returns>Добавленный материал с присвоенным идентификатором</returns>
        public async Task<Material> AddAsync(Material material)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material));

            await EnsureLoadedAsync();
            await _semaphore.WaitAsync();
            try
            {
                ValidateNameUnique(material.Name);

                material.Id = _meta.NextMaterialId++;
                _materials!.Add(material);

                return material;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Обновить существующий материал
        /// </summary>
        /// <param name="material">Материал с обновлёнными данными</param>
        /// <returns>Обновлённый материал</returns>
        public async Task<Material> UpdateAsync(Material material)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material));

            await EnsureLoadedAsync();
            await _semaphore.WaitAsync();
            try
            {
                var existing = _materials!.FirstOrDefault(m => m.Id == material.Id)
                    ?? throw new InvalidOperationException($"Материал с id={material.Id} не найден.");

                ValidateNameUnique(material.Name, material.Id);

                existing.Name = material.Name;
                existing.Category = material.Category;
                existing.LambdaA = material.LambdaA;
                existing.LambdaB = material.LambdaB;
                existing.MaxSupplyTemp = material.MaxSupplyTemp;
                existing.MinOutdoorTemp = material.MinOutdoorTemp;
                existing.Notes = material.Notes;

                return existing;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Удалить материал по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор материала</param>
        /// <returns>true, если материал был удалён; иначе false</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            await EnsureLoadedAsync();
            await _semaphore.WaitAsync();
            try
            {
                var material = _materials!.FirstOrDefault(m => m.Id == id);
                if (material == null)
                    return false;

                _materials!.Remove(material);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Сохранить все материалы в JSON файл атомарно
        /// </summary>
        public async Task SaveMaterialsAsync()
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
        /// Ядро сохранения: сериализация и атомарная запись файла без семафора
        /// </summary>
        private async Task SaveCoreAsync()
        {
            var dbModel = new MaterialsDbModel
            {
                Meta = _meta,
                Materials = _materials!.Select(MapToJsonModel).ToList(),
                UsageRules = _usageRulesCache
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            };

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
        /// Убедиться, что материалы загружены
        /// </summary>
        private async Task EnsureLoadedAsync()
        {
            if (_materials == null)
                await LoadMaterialsAsync();
        }

        /// <summary>
        /// Проверить уникальность названия материала (без учёта регистра)
        /// </summary>
        private void ValidateNameUnique(string name, int? excludeId = null)
        {
            var duplicate = _materials!.FirstOrDefault(m =>
                m.Id != excludeId &&
                m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
                throw new InvalidOperationException($"Материал с названием '{name}' уже существует.");
        }

        /// <summary>
        /// Установить счётчик идентификаторов, если он ещё не задан
        /// </summary>
        private void EnsureNextMaterialIdSeeded()
        {
            if (_meta.NextMaterialId > 0)
                return;

            var maxId = _materials?.Any() == true ? _materials.Max(m => m.Id) : 0;
            _meta.NextMaterialId = maxId + 1;
        }

        /// <summary>
        /// Маппинг из JSON модели в Material
        /// </summary>
        private static Material MapToMaterial(MaterialJsonModel jsonModel, List<Material> defaultMaterials)
        {
            var isBuiltIn = jsonModel.IsBuiltIn ?? IsDefaultMaterial(jsonModel, defaultMaterials);

            return new Material
            {
                Id = jsonModel.Id,
                Name = jsonModel.Name ?? string.Empty,
                Category = ParseCategory(jsonModel.Category),
                LambdaA = jsonModel.LambdaA,
                LambdaB = jsonModel.LambdaB,
                MaxSupplyTemp = jsonModel.MaxSupplyTemp,
                MinOutdoorTemp = jsonModel.MinOutdoorTemp,
                Notes = jsonModel.Notes,
                IsBuiltIn = isBuiltIn
            };
        }

        /// <summary>
        /// Маппинг из Material в JSON модель
        /// </summary>
        private static MaterialJsonModel MapToJsonModel(Material material)
        {
            return new MaterialJsonModel
            {
                Id = material.Id,
                Name = material.Name,
                LambdaA = material.LambdaA,
                LambdaB = material.LambdaB,
                Unit = "Вт/(м·К)",
                Category = FormatCategory(material.Category),
                Notes = material.Notes,
                MaxSupplyTemp = material.MaxSupplyTemp,
                MinOutdoorTemp = material.MinOutdoorTemp,
                IsBuiltIn = material.IsBuiltIn
            };
        }

        /// <summary>
        /// Проверить, соответствует ли JSON-материал одному из материалов по умолчанию
        /// по идентификатору. Имя не сравнивается: legacy-файлы могут содержать локальные
        /// варианты названий (например, "Пенополистирол (ЭППС)" против дефолтного
        /// "Пенополистирол ЭППС"), но Id является стабильным идентификатором.
        /// </summary>
        private static bool IsDefaultMaterial(MaterialJsonModel jsonModel, List<Material> defaultMaterials)
        {
            return defaultMaterials.Any(defaultMaterial => defaultMaterial.Id == jsonModel.Id);
        }

        /// <summary>
        /// Парсинг категории из строки
        /// </summary>
        private static MaterialCategory ParseCategory(string? category)
        {
            return category?.ToLowerInvariant() switch
            {
                "бетон" or "concrete" => MaterialCategory.Concrete,
                "грунт" or "soil" => MaterialCategory.Soil,
                "изоляция" or "insulation" => MaterialCategory.Insulation,
                "покрытие" or "coating" => MaterialCategory.Coating,
                "подстилающий" or "subbase" => MaterialCategory.Subbase,
                "стяжка" or "screed" => MaterialCategory.Screed,
                _ => MaterialCategory.Soil
            };
        }

        /// <summary>
        /// Форматирование категории в строку
        /// </summary>
        private static string? FormatCategory(MaterialCategory category)
        {
            return category switch
            {
                MaterialCategory.Concrete => "бетон",
                MaterialCategory.Soil => "грунт",
                MaterialCategory.Insulation => "изоляция",
                MaterialCategory.Coating => "покрытие",
                MaterialCategory.Subbase => "подстилающий",
                MaterialCategory.Screed => "стяжка",
                _ => "грунт"
            };
        }

        /// <summary>
        /// Создать метаданные по умолчанию для нового файла
        /// </summary>
        private static MaterialsMeta CreateDefaultMeta()
        {
            return new MaterialsMeta
            {
                Source = "Material.GetDefaultMaterials()",
                Version = "1.1",
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Description = "База материалов для расчёта систем снеготаяния. λА - сухие условия, λБ - влажные условия (УГВ < 1м)",
                NextMaterialId = 12
            };
        }

        #region JSON Models

        /// <summary>
        /// Модель корневого объекта JSON
        /// </summary>
        private class MaterialsDbModel
        {
            [JsonPropertyName("meta")]
            public MaterialsMeta? Meta { get; set; }

            [JsonPropertyName("materials")]
            public List<MaterialJsonModel>? Materials { get; set; }

            [JsonPropertyName("usage_rules")]
            public JsonElement? UsageRules { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement>? ExtensionData { get; set; }
        }

        /// <summary>
        /// Модель метаданных
        /// </summary>
        private class MaterialsMeta
        {
            [JsonPropertyName("source")]
            public string? Source { get; set; }

            [JsonPropertyName("version")]
            public string? Version { get; set; }

            [JsonPropertyName("date")]
            public string? Date { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("next_material_id")]
            public int NextMaterialId { get; set; }
        }

        /// <summary>
        /// Модель материала в JSON
        /// </summary>
        private class MaterialJsonModel
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("lambda_A")]
            public double LambdaA { get; set; }

            [JsonPropertyName("lambda_B")]
            public double LambdaB { get; set; }

            [JsonPropertyName("unit")]
            public string? Unit { get; set; }

            [JsonPropertyName("category")]
            public string? Category { get; set; }

            [JsonPropertyName("notes")]
            public string? Notes { get; set; }

            [JsonPropertyName("max_supply_temp")]
            public double? MaxSupplyTemp { get; set; }

            [JsonPropertyName("min_outdoor_temp")]
            public double? MinOutdoorTemp { get; set; }

            [JsonPropertyName("is_built_in")]
            public bool? IsBuiltIn { get; set; }
        }

        #endregion
    }
}
