using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Services.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Repositories.Construction
{
    /// <summary>
    /// Репозиторий для сохранения и загрузки конструкций
    /// </summary>
    public class ConstructionRepository : IConstructionRepository
    {
        private readonly IMaterialRepository _materialRepository;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Создать репозиторий конструкций
        /// </summary>
        /// <param name="materialRepository">Репозиторий материалов</param>
        public ConstructionRepository(IMaterialRepository materialRepository)
        {
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        /// <summary>
        /// Сохранить конструкцию в файл
        /// </summary>
        public async Task SaveConstructionAsync(ConstructionModel construction, string filePath)
        {
            ArgumentNullException.ThrowIfNull(construction, nameof(construction));
            ArgumentException.ThrowIfNullOrEmpty(filePath, nameof(filePath));

            try
            {
                var dto = MapToDto(construction);
                var jsonContent = JsonSerializer.Serialize(dto, _jsonOptions);

                // Создаём директорию, если не существует
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                throw new IOException($"Ошибка при сохранении конструкции в файл '{filePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Загрузить конструкцию из файла
        /// </summary>
        public async Task<ConstructionModel?> LoadConstructionAsync(string filePath)
        {
            ArgumentException.ThrowIfNullOrEmpty(filePath, nameof(filePath));

            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var dto = JsonSerializer.Deserialize<ConstructionDto>(jsonContent, _jsonOptions);

                if (dto == null)
                {
                    return null;
                }

                return await MapFromDtoAsync(dto);
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Ошибка десериализации файла конструкции '{filePath}': {ex.Message}", ex);
            }
            catch (MaterialNotFoundException)
            {
                // Пробрасываем как есть, чтобы вызывающий код мог использовать
                // сохранённый снимок материала (MaterialSnapshot) для импорта
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException($"Ошибка при загрузке конструкции из файла '{filePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Сохранить конструкцию в проект
        /// </summary>
        public async Task SaveToProjectAsync(ConstructionModel construction, int projectId)
        {
            ArgumentNullException.ThrowIfNull(construction, nameof(construction));

            if (projectId <= 0)
            {
                throw new ArgumentException("Идентификатор проекта должен быть положительным числом", nameof(projectId));
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            var projectsDir = Path.Combine(projectRoot, "projects");

            if (!Directory.Exists(projectsDir))
            {
                Directory.CreateDirectory(projectsDir);
            }

            var filePath = Path.Combine(projectsDir, $"project_{projectId}_construction.json");
            await SaveConstructionAsync(construction, filePath);
        }

        /// <summary>
        /// Загрузить конструкцию из проекта
        /// </summary>
        public async Task<ConstructionModel?> LoadFromProjectAsync(int projectId)
        {
            if (projectId <= 0)
            {
                throw new ArgumentException("Идентификатор проекта должен быть положительным числом", nameof(projectId));
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            var filePath = Path.Combine(projectRoot, "projects", $"project_{projectId}_construction.json");

            return await LoadConstructionAsync(filePath);
        }

        /// <summary>
        /// Получить список сохранённых конструкций
        /// </summary>
        public Task<IEnumerable<string>> GetSavedConstructionsAsync(string directoryPath)
        {
            ArgumentException.ThrowIfNullOrEmpty(directoryPath, nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
            {
                return Task.FromResult(Enumerable.Empty<string>());
            }

            var files = Directory.GetFiles(directoryPath, "*_construction.json")
                .OrderByDescending(f => File.GetLastWriteTime(f));

            return Task.FromResult(files.AsEnumerable());
        }

        #region Mapping

        /// <summary>
        /// Преобразовать конструкцию в DTO
        /// </summary>
        private static ConstructionDto MapToDto(ConstructionModel construction)
        {
            var allLayers = construction.LayersAbovePipe.Concat(construction.Layers).ToList();

            return new ConstructionDto
            {
                Version = "1.1",
                GroundwaterLevel = construction.GroundwaterLevel,
                LayersAbovePipe = construction.LayersAbovePipe.Select(MapLayerToDto).ToList(),
                LayersBelowPipe = construction.Layers
                    .Where(l => l.Position == LayerPosition.BelowPipe)
                    .Select(MapLayerToDto).ToList(),
                MaterialSnapshots = allLayers
                    .Select(l => l.Material)
                    .Where(m => m != null)
                    .DistinctBy(m => m!.Id)
                    .Select(m => MaterialSnapshot.FromMaterial(m!))
                    .ToList()
            };
        }

        /// <summary>
        /// Преобразовать слой в DTO
        /// </summary>
        private static LayerDto MapLayerToDto(Layer layer)
        {
            return new LayerDto
            {
                MaterialId = layer.Material.Id,
                Thickness = layer.Thickness,
                CalculatedLambda = layer.CalculatedLambda,
                IsLambdaOverridden = layer.IsLambdaOverridden,
                Position = layer.Position,
                Order = layer.Order
            };
        }

        /// <summary>
        /// Преобразовать DTO в конструкцию
        /// </summary>
        private async Task<ConstructionModel> MapFromDtoAsync(ConstructionDto dto)
        {
            // Убеждаемся, что материалы загружены
            if (!_materialRepository.IsLoaded)
            {
                await _materialRepository.LoadMaterialsAsync();
            }

            var construction = new ConstructionModel
            {
                GroundwaterLevel = dto.GroundwaterLevel
            };

            // v1.0: chronological Add order = [near pipe, ..., surface].
            // v1.1: physical top-to-bottom = [surface, ..., near pipe].
            var aboveLayers = dto.LayersAbovePipe.OrderBy(l => l.Order).ToList();
            if (string.Compare(dto.Version, "1.1", StringComparison.OrdinalIgnoreCase) < 0)
                aboveLayers.Reverse(); // реверс после сортировки, иначе OrderBy отменит reverse

            // Добавляем слои над трубой
            foreach (var layerDto in aboveLayers)
            {
                var material = _materialRepository.GetMaterialById(layerDto.MaterialId);
                if (material == null)
                {
                    throw CreateMaterialNotFoundException(layerDto.MaterialId, dto.MaterialSnapshots);
                }

                var layer = construction.AddLayerAbovePipe(material, layerDto.Thickness);
                layer.CalculatedLambda = layerDto.CalculatedLambda;
                layer.IsLambdaOverridden = layerDto.IsLambdaOverridden;
                layer.Order = layerDto.Order;
            }

            // Добавляем слои под трубой
            foreach (var layerDto in dto.LayersBelowPipe.OrderBy(l => l.Order))
            {
                var material = _materialRepository.GetMaterialById(layerDto.MaterialId);
                if (material == null)
                {
                    throw CreateMaterialNotFoundException(layerDto.MaterialId, dto.MaterialSnapshots);
                }

                var layer = construction.AddLayerBelowPipe(material, layerDto.Thickness);
                layer.CalculatedLambda = layerDto.CalculatedLambda;
                layer.IsLambdaOverridden = layerDto.IsLambdaOverridden;
                layer.Order = layerDto.Order;
            }

            // After all layers loaded, reindex so Order matches the new physical indices
            construction.ReindexLayers();

            return construction;
        }

        /// <summary>
        /// Создать исключение об отсутствующем материале, прикрепив снимок если он есть
        /// </summary>
        private static MaterialNotFoundException CreateMaterialNotFoundException(int materialId, List<MaterialSnapshot> snapshots)
        {
            var snapshot = snapshots.FirstOrDefault(s => s.Id == materialId);
            if (snapshot != null)
            {
                return new MaterialNotFoundException(materialId, snapshot);
            }

            return new MaterialNotFoundException(materialId);
        }

        #endregion

        #region DTO Classes

        /// <summary>
        /// DTO для сериализации конструкции
        /// </summary>
        private class ConstructionDto
        {
            [JsonPropertyName("version")]
            public string Version { get; set; } = "1.0";

            [JsonPropertyName("groundwater_level")]
            public double GroundwaterLevel { get; set; }

            [JsonPropertyName("layers_above_pipe")]
            public List<LayerDto> LayersAbovePipe { get; set; } = new();

            [JsonPropertyName("layers_below_pipe")]
            public List<LayerDto> LayersBelowPipe { get; set; } = new();

            [JsonPropertyName("material_snapshots")]
            public List<MaterialSnapshot> MaterialSnapshots { get; set; } = new();
        }

        /// <summary>
        /// DTO для сериализации слоя
        /// </summary>
        private class LayerDto
        {
            [JsonPropertyName("material_id")]
            public int MaterialId { get; set; }

            [JsonPropertyName("thickness")]
            public double Thickness { get; set; }

            [JsonPropertyName("calculated_lambda")]
            public double CalculatedLambda { get; set; }

            [JsonPropertyName("is_lambda_overridden")]
            public bool IsLambdaOverridden { get; set; }

            [JsonPropertyName("position")]
            public LayerPosition Position { get; set; }

            [JsonPropertyName("order")]
            public int Order { get; set; }
        }

        #endregion
    }
}