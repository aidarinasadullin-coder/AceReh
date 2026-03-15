using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Repositories.Construction
{
    /// <summary>
    /// Репозиторий для загрузки материалов из JSON
    /// </summary>
    public class MaterialRepository : IMaterialRepository
    {
        private readonly string _dataPath;
        private List<Material>? _materials;
        private readonly object _lockObject = new();

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
                var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
                _dataPath = Path.Combine(projectRoot, "data", "materials_db.json");
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

            lock (_lockObject)
            {
                if (_materials != null)
                    return _materials;
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(_dataPath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };

                var materialsData = JsonSerializer.Deserialize<MaterialsDbModel>(jsonContent, options);

                _materials = materialsData?.Materials?.Select(MapToMaterial).ToList() 
                    ?? Material.GetDefaultMaterials();

                return _materials;
            }
            catch (FileNotFoundException)
            {
                // Если файл не найден, используем материалы по умолчанию
                _materials = Material.GetDefaultMaterials();
                return _materials;
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Ошибка десериализации файла материалов: {ex.Message}");
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
        /// Маппинг из JSON модели в Material
        /// </summary>
        private static Material MapToMaterial(MaterialJsonModel jsonModel)
        {
            return new Material
            {
                Id = jsonModel.Id,
                Name = jsonModel.Name ?? string.Empty,
                Category = ParseCategory(jsonModel.Category),
                LambdaA = jsonModel.LambdaA,
                LambdaB = jsonModel.LambdaB,
                MaxSupplyTemp = jsonModel.MaxSupplyTemp,
                MinOutdoorTemp = jsonModel.MinOutdoorTemp,
                Notes = jsonModel.Notes
            };
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
        }

        #endregion
    }
}