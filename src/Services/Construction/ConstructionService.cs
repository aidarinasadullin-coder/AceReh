using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Сервис для работы с конструкциями
    /// </summary>
    public class ConstructionService : IConstructionService
    {
        private readonly IValidator<ConstructionModel> _validator;
        private readonly IMaterialRepository _materialRepository;
        private readonly IConstructionTemplateRepository _templateRepository;

        /// <summary>
        /// Создать сервис
        /// </summary>
        /// <param name="validator">Валидатор конструкции</param>
        /// <param name="materialRepository">Репозиторий материалов</param>
        /// <param name="templateRepository">Репозиторий шаблонов конструкций</param>
        public ConstructionService(
            IValidator<ConstructionModel> validator,
            IMaterialRepository materialRepository,
            IConstructionTemplateRepository templateRepository)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        }

        /// <summary>
        /// Рассчитать термические сопротивления для всех слоёв конструкции
        /// </summary>
        public void CalculateThermalResistances(ConstructionModel construction)
        {
            ArgumentNullException.ThrowIfNull(construction, nameof(construction));

            // Обновляем λ для всех слоёв в зависимости от УГВ
            foreach (var layer in construction.LayersAbovePipe)
            {
                layer.UpdateLambda(construction.GroundwaterLevel);
            }

            foreach (var layer in construction.Layers)
            {
                layer.UpdateLambda(construction.GroundwaterLevel);
            }
        }

        /// <summary>
        /// Рассчитать суммарное термическое сопротивление слоёв над трубой (R1)
        /// </summary>
        /// <remarks>
        /// Формула: R1 = Σ(d_i / λ_i / 1000), где d_i - толщина слоя в мм, λ_i - теплопроводность
        /// </remarks>
        public double CalculateR1(IEnumerable<Layer> layersAbovePipe)
        {
            ArgumentNullException.ThrowIfNull(layersAbovePipe, nameof(layersAbovePipe));

            return layersAbovePipe.Sum(layer =>
            {
                if (layer.CalculatedLambda <= 0)
                {
                    throw new InvalidOperationException(
                        $"Теплопроводность слоя '{layer.Material?.Name ?? "Не указан"}' должна быть положительной");
                }

                // R = d / λ / 1000 (толщина в мм, переводим в метры)
                return layer.Thickness / layer.CalculatedLambda / 1000.0;
            });
        }

        /// <summary>
        /// Рассчитать суммарное термическое сопротивление слоёв под трубой (R2)
        /// </summary>
        /// <remarks>
        /// Формула: R2 = Σ(d_i / λ_i / 1000), где d_i - толщина слоя в мм, λ_i - теплопроводность
        /// При УГВ < 1м используется λБ, иначе λА
        /// </remarks>
        public double CalculateR2(IEnumerable<Layer> layersBelowPipe, double groundwaterLevel)
        {
            ArgumentNullException.ThrowIfNull(layersBelowPipe, nameof(layersBelowPipe));

            if (groundwaterLevel < 0)
            {
                throw new ArgumentException("Уровень грунтовых вод не может быть отрицательным", nameof(groundwaterLevel));
            }

            return layersBelowPipe.Sum(layer =>
            {
                // Обновляем λ в зависимости от УГВ
                layer.UpdateLambda(groundwaterLevel);

                if (layer.CalculatedLambda <= 0)
                {
                    throw new InvalidOperationException(
                        $"Теплопроводность слоя '{layer.Material?.Name ?? "Не указан"}' должна быть положительной");
                }

                // R = d / λ / 1000 (толщина в мм, переводим в метры)
                return layer.Thickness / layer.CalculatedLambda / 1000.0;
            });
        }

        /// <summary>
        /// Валидация конструкции
        /// </summary>
        public ValidationResult ValidateConstruction(ConstructionModel construction)
        {
            ArgumentNullException.ThrowIfNull(construction, nameof(construction));

            return _validator.Validate(construction);
        }

        /// <summary>
        /// Создать конструкцию из шаблона
        /// </summary>
        public ConstructionModel CreateFromTemplate(ConstructionTemplate template, IEnumerable<Material> materials)
        {
            ArgumentNullException.ThrowIfNull(template, nameof(template));
            ArgumentNullException.ThrowIfNull(materials, nameof(materials));

            var materialsList = materials.ToList();
            var construction = new ConstructionModel
            {
                GroundwaterLevel = template.DefaultGroundwaterLevel,
                HasLoads = template.HasLoads
            };

            // Добавляем слои над трубой
            foreach (var layerTemplate in template.LayersAbovePipe.OrderBy(l => l.Order))
            {
                var material = materialsList.FirstOrDefault(m => m.Id == layerTemplate.MaterialId);
                if (material == null)
                {
                    var snapshot = template.MaterialSnapshots.FirstOrDefault(s => s.Id == layerTemplate.MaterialId);
                    throw snapshot is null
                        ? new MaterialNotFoundException(layerTemplate.MaterialId)
                        : new MaterialNotFoundException(layerTemplate.MaterialId, snapshot);
                }

                construction.AddLayerAbovePipe(material, layerTemplate.Thickness);
            }

            // Добавляем слои под трубой
            foreach (var layerTemplate in template.LayersBelowPipe.OrderBy(l => l.Order))
            {
                var material = materialsList.FirstOrDefault(m => m.Id == layerTemplate.MaterialId);
                if (material == null)
                {
                    var snapshot = template.MaterialSnapshots.FirstOrDefault(s => s.Id == layerTemplate.MaterialId);
                    throw snapshot is null
                        ? new MaterialNotFoundException(layerTemplate.MaterialId)
                        : new MaterialNotFoundException(layerTemplate.MaterialId, snapshot);
                }

                construction.AddLayerBelowPipe(material, layerTemplate.Thickness);
            }

            // Рассчитываем термические сопротивления
            CalculateThermalResistances(construction);

            return construction;
        }

        /// <summary>
        /// Импортировать отсутствующий материал из снимка в справочник материалов
        /// </summary>
        public async Task<Material> ImportMissingMaterialAsync(MaterialSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));

            await _materialRepository.LoadMaterialsAsync();

            var name = MakeUniqueName(snapshot.Name);

            var material = new Material
            {
                Name = name,
                Category = snapshot.Category,
                LambdaA = snapshot.LambdaA,
                LambdaB = snapshot.LambdaB,
                MaxSupplyTemp = snapshot.MaxSupplyTemp,
                MinOutdoorTemp = snapshot.MinOutdoorTemp,
                Notes = snapshot.Notes,
                IsBuiltIn = false
            };

            var addedMaterial = await _materialRepository.AddAsync(material);
            await _materialRepository.SaveMaterialsAsync();

            return addedMaterial;
        }

        /// <summary>
        /// Сделать название уникальным, добавляя суффикс " (импортирован)" при конфликте
        /// </summary>
        private string MakeUniqueName(string baseName)
        {
            var existingNames = _materialRepository.GetAllMaterials()
                .Select(m => m.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!existingNames.Contains(baseName))
            {
                return baseName;
            }

            var suffix = " (импортирован)";
            var candidate = baseName + suffix;
            while (existingNames.Contains(candidate))
            {
                candidate += suffix;
            }

            return candidate;
        }

        /// <summary>
        /// Импортировать материалы из проекта в справочник материалов.
        /// Пропускает снимки, у которых Id уже существует или имя (без учёта регистра) уже занято.
        /// </summary>
        public async Task ImportProjectMaterialsAsync(IEnumerable<MaterialSnapshot> snapshots)
        {
            ArgumentNullException.ThrowIfNull(snapshots, nameof(snapshots));

            await _materialRepository.LoadMaterialsAsync();

            var existingIds = _materialRepository.GetAllMaterials()
                .Select(m => m.Id)
                .ToHashSet();
            var existingNames = _materialRepository.GetAllMaterials()
                .Select(m => m.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var snapshot in snapshots)
            {
                if (existingIds.Contains(snapshot.Id))
                {
                    continue;
                }

                if (existingNames.Contains(snapshot.Name))
                {
                    continue;
                }

                var material = new Material
                {
                    Name = snapshot.Name,
                    Category = snapshot.Category,
                    LambdaA = snapshot.LambdaA,
                    LambdaB = snapshot.LambdaB,
                    MaxSupplyTemp = snapshot.MaxSupplyTemp,
                    MinOutdoorTemp = snapshot.MinOutdoorTemp,
                    Notes = snapshot.Notes,
                    IsBuiltIn = false
                };

                var added = await _materialRepository.AddAsync(material);
                existingIds.Add(added.Id);
                existingNames.Add(added.Name);
            }

            await _materialRepository.SaveMaterialsAsync();
        }

        /// <summary>
        /// Импортировать пользовательские шаблоны конструкций из проекта в глобальный каталог.
        /// Пропускает шаблоны с уже существующим именем и шаблоны, материалы которых
        /// не удалось разрешить по имени через локальный справочник.
        /// </summary>
        public async Task ImportProjectTemplatesAsync(IEnumerable<ConstructionTemplate> templates)
        {
            ArgumentNullException.ThrowIfNull(templates, nameof(templates));

            var existingTemplates = (await _templateRepository.GetAllAsync()).ToList();
            var existingNames = existingTemplates
                .Select(t => t.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            await _materialRepository.LoadMaterialsAsync();
            var localMaterials = _materialRepository.GetAllMaterials().ToList();
            var localMaterialByName = localMaterials
                .ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var template in templates)
            {
                if (existingNames.Contains(template.Name))
                {
                    continue;
                }

                var snapshotById = template.MaterialSnapshots
                    .ToDictionary(s => s.Id);

                var canRemap = true;
                var remappedLayers = new List<LayerTemplate>();

                foreach (var layer in template.LayersAbovePipe.Concat(template.LayersBelowPipe))
                {
                    if (!snapshotById.TryGetValue(layer.MaterialId, out var snapshot))
                    {
                        canRemap = false;
                        break;
                    }

                    if (!localMaterialByName.TryGetValue(snapshot.Name, out var localMaterial))
                    {
                        canRemap = false;
                        break;
                    }

                    remappedLayers.Add(new LayerTemplate
                    {
                        MaterialId = localMaterial.Id,
                        Thickness = layer.Thickness,
                        Position = layer.Position,
                        Order = layer.Order
                    });
                }

                if (!canRemap)
                {
                    continue;
                }

                var layerCountAbove = template.LayersAbovePipe.Count;
                var importTemplate = new ConstructionTemplate
                {
                    Name = template.Name,
                    Description = template.Description,
                    HasLoads = template.HasLoads,
                    DefaultGroundwaterLevel = template.DefaultGroundwaterLevel,
                    IsBuiltIn = false,
                    LayersAbovePipe = remappedLayers.Take(layerCountAbove).ToList(),
                    LayersBelowPipe = remappedLayers.Skip(layerCountAbove).ToList(),
                    MaterialSnapshots = template.MaterialSnapshots
                        .Select(s => new MaterialSnapshot
                        {
                            Id = s.Id,
                            Name = s.Name,
                            Category = s.Category,
                            LambdaA = s.LambdaA,
                            LambdaB = s.LambdaB,
                            MaxSupplyTemp = s.MaxSupplyTemp,
                            MinOutdoorTemp = s.MinOutdoorTemp,
                            Notes = s.Notes,
                            IsBuiltIn = s.IsBuiltIn
                        })
                        .ToList()
                };

                await _templateRepository.AddAsync(importTemplate);
                existingNames.Add(importTemplate.Name);
            }

            await _templateRepository.SaveAsync();
        }

        /// <summary>
        /// Получить общую толщину слоёв над трубой
        /// </summary>
        public double GetTotalThicknessAbovePipe(ConstructionModel construction)
        {
            ArgumentNullException.ThrowIfNull(construction, nameof(construction));

            return construction.LayersAbovePipe.Sum(l => l.Thickness);
        }

        /// <summary>
        /// Получить общую толщину слоёв под трубой
        /// </summary>
        public double GetTotalThicknessBelowPipe(ConstructionModel construction)
        {
            ArgumentNullException.ThrowIfNull(construction, nameof(construction));

            return construction.Layers
                .Where(l => l.Position == LayerPosition.BelowPipe)
                .Sum(l => l.Thickness);
        }
    }
}