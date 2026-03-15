# Task 2.2: Создать IConstructionRepository.cs и ConstructionRepository.cs

**Этап:** 2. Репозитории  
**Приоритет:** P3 (Низкая)  
**Время:** 2 часа  
**Зависимости:** Task 1.5, Task 1.4

---

## 1. Цель задачи

Создать интерфейс `IConstructionRepository` и его реализацию `ConstructionRepository` для сохранения и загрузки конструкций из JSON-файлов и SQLite.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| — | Сохранение/загрузка конструкций | SaveToJsonAsync, LoadFromJsonAsync |
| — | Предустановленные шаблоны | GetTemplates |

---

## 3. Описание изменений

### 3.1. Создать файл IConstructionRepository.cs

**Путь:** `src/Services/Construction/IConstructionRepository.cs`

**Код:**

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Интерфейс репозитория конструкций
    /// </summary>
    public interface IConstructionRepository
    {
        /// <summary>
        /// Сохранить конструкцию в JSON-файл
        /// </summary>
        /// <param name="construction">Конструкция для сохранения</param>
        /// <param name="filePath">Путь к файлу</param>
        Task SaveToJsonAsync(Construction construction, string filePath);

        /// <summary>
        /// Загрузить конструкцию из JSON-файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Конструкция или null</returns>
        Task<Construction?> LoadFromJsonAsync(string filePath);

        /// <summary>
        /// Сохранить конструкцию в проект (SQLite)
        /// </summary>
        /// <param name="construction">Конструкция для сохранения</param>
        /// <param name="projectId">ID проекта</param>
        Task SaveToProjectAsync(Construction construction, int projectId);

        /// <summary>
        /// Загрузить конструкцию из проекта (SQLite)
        /// </summary>
        /// <param name="projectId">ID проекта</param>
        /// <returns>Конструкция или null</returns>
        Task<Construction?> LoadFromProjectAsync(int projectId);

        /// <summary>
        /// Получить предустановленные шаблоны конструкций
        /// </summary>
        /// <returns>Список шаблонов</returns>
        IEnumerable<ConstructionTemplate> GetTemplates();
    }
}
```

### 3.2. Создать файл ConstructionRepository.cs

**Путь:** `src/Services/Construction/ConstructionRepository.cs`

**Код:**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Репозиторий конструкций
    /// </summary>
    /// <remarks>
    /// Реализует сохранение/загрузку конструкций в JSON и SQLite
    /// </remarks>
    public class ConstructionRepository : IConstructionRepository
    {
        #region Поля

        private readonly IMaterialRepository _materialRepository;

        #endregion

        #region Конструктор

        /// <summary>
        /// Создать репозиторий конструкций
        /// </summary>
        /// <param name="materialRepository">Репозиторий материалов</param>
        public ConstructionRepository(IMaterialRepository materialRepository)
        {
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
        }

        #endregion

        #region Методы

        /// <inheritdoc/>
        public async Task SaveToJsonAsync(Construction construction, string filePath)
        {
            if (construction == null)
            {
                throw new ArgumentNullException(nameof(construction));
            }

            var jsonModel = new ConstructionJsonModel
            {
                GroundwaterLevel = construction.GroundwaterLevel,
                HasLoads = construction.HasLoads,
                LayersAbovePipe = ConvertLayersToJson(construction.LayersAbovePipe),
                LayersBelowPipe = ConvertLayersToJson(construction.LayersBelowPipe)
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonContent = JsonSerializer.Serialize(jsonModel, options);
            await File.WriteAllTextAsync(filePath, jsonContent);
        }

        /// <inheritdoc/>
        public async Task<Construction?> LoadFromJsonAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var jsonModel = JsonSerializer.Deserialize<ConstructionJsonModel>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (jsonModel == null)
                {
                    return null;
                }

                // Убедимся, что материалы загружены
                if (!_materialRepository.IsLoaded)
                {
                    await _materialRepository.LoadMaterialsAsync();
                }

                var construction = new Construction
                {
                    GroundwaterLevel = jsonModel.GroundwaterLevel,
                    HasLoads = jsonModel.HasLoads
                };

                // Загрузить слои над трубой
                foreach (var layerJson in jsonModel.LayersAbovePipe)
                {
                    var material = _materialRepository.GetMaterialById(layerJson.MaterialId);
                    if (material != null)
                    {
                        construction.AddLayerAbovePipe(material, layerJson.Thickness);
                    }
                }

                // Загрузить слои под трубой
                foreach (var layerJson in jsonModel.LayersBelowPipe)
                {
                    var material = _materialRepository.GetMaterialById(layerJson.MaterialId);
                    if (material != null)
                    {
                        construction.AddLayerBelowPipe(material, layerJson.Thickness);
                    }
                }

                return construction;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public Task SaveToProjectAsync(Construction construction, int projectId)
        {
            // TODO: Реализовать сохранение в SQLite
            // В первой версии используем только JSON
            throw new NotImplementedException("Сохранение в SQLite будет реализовано в следующей версии");
        }

        /// <inheritdoc/>
        public Task<Construction?> LoadFromProjectAsync(int projectId)
        {
            // TODO: Реализовать загрузку из SQLite
            // В первой версии используем только JSON
            throw new NotImplementedException("Загрузка из SQLite будет реализована в следующей версии");
        }

        /// <inheritdoc/>
        public IEnumerable<ConstructionTemplate> GetTemplates()
        {
            return ConstructionTemplate.StandardTemplates;
        }

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Преобразовать слои в JSON-модель
        /// </summary>
        private List<LayerJsonModel> ConvertLayersToJson(IEnumerable<Layer> layers)
        {
            var result = new List<LayerJsonModel>();
            foreach (var layer in layers)
            {
                result.Add(new LayerJsonModel
                {
                    MaterialId = layer.Material.Id,
                    Thickness = layer.Thickness,
                    Lambda = layer.Lambda,
                    IsLambdaOverridden = layer.IsLambdaOverridden,
                    Order = layer.Order
                });
            }
            return result;
        }

        #endregion

        #region Внутренние модели для JSON

        /// <summary>
        /// Модель для сериализации конструкции в JSON
        /// </summary>
        private class ConstructionJsonModel
        {
            public double GroundwaterLevel { get; set; }
            public bool HasLoads { get; set; }
            public List<LayerJsonModel> LayersAbovePipe { get; set; } = new();
            public List<LayerJsonModel> LayersBelowPipe { get; set; } = new();
        }

        /// <summary>
        /// Модель для сериализации слоя в JSON
        /// </summary>
        private class LayerJsonModel
        {
            public int MaterialId { get; set; }
            public double Thickness { get; set; }
            public double Lambda { get; set; }
            public bool IsLambdaOverridden { get; set; }
            public int Order { get; set; }
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

### TC-2.2.1: Сохранение конструкции в JSON

```csharp
[Fact]
public async Task ConstructionRepository_SaveToJsonAsync_ShouldSaveFile()
{
    // Arrange
    var materialRepo = new MaterialRepository("data/materials_db.json");
    await materialRepo.LoadMaterialsAsync();
    var repository = new ConstructionRepository(materialRepo);

    var construction = new Construction { GroundwaterLevel = 2.0, HasLoads = true };
    var material = materialRepo.GetDefaultMaterial();
    construction.AddLayerAbovePipe(material, 100.0);

    var filePath = Path.Combine(Path.GetTempPath(), "test_construction.json");

    // Act
    await repository.SaveToJsonAsync(construction, filePath);

    // Assert
    Assert.True(File.Exists(filePath));

    // Cleanup
    File.Delete(filePath);
}
```

### TC-2.2.2: Загрузка конструкции из JSON

```csharp
[Fact]
public async Task ConstructionRepository_LoadFromJsonAsync_ShouldLoadConstruction()
{
    // Arrange
    var materialRepo = new MaterialRepository("data/materials_db.json");
    await materialRepo.LoadMaterialsAsync();
    var repository = new ConstructionRepository(materialRepo);

    // Сначала сохраняем
    var construction = new Construction { GroundwaterLevel = 2.0, HasLoads = true };
    var material = materialRepo.GetDefaultMaterial();
    construction.AddLayerAbovePipe(material, 100.0);

    var filePath = Path.Combine(Path.GetTempPath(), "test_construction.json");
    await repository.SaveToJsonAsync(construction, filePath);

    // Act
    var loaded = await repository.LoadFromJsonAsync(filePath);

    // Assert
    Assert.NotNull(loaded);
    Assert.Equal(2.0, loaded!.GroundwaterLevel);
    Assert.True(loaded.HasLoads);
    Assert.Single(loaded.LayersAbovePipe);

    // Cleanup
    File.Delete(filePath);
}
```

### TC-2.2.3: Получение шаблонов

```csharp
[Fact]
public void ConstructionRepository_GetTemplates_ShouldReturnTemplates()
{
    // Arrange
    var materialRepo = new MaterialRepository("data/materials_db.json");
    var repository = new ConstructionRepository(materialRepo);

    // Act
    var templates = repository.GetTemplates();

    // Assert
    Assert.Equal(3, templates.Count());
}
```

---

## 5. Критерии приёмки

- [ ] Файл `src/Services/Construction/IConstructionRepository.cs` создан
- [ ] Файл `src/Services/Construction/ConstructionRepository.cs` создан
- [ ] Метод `SaveToJsonAsync()` сохраняет конструкцию в JSON
- [ ] Метод `LoadFromJsonAsync()` загружает конструкцию из JSON
- [ ] Метод `GetTemplates()` возвращает стандартные шаблоны
- [ ] XML-документация добавлена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- SQLite сохранение будет реализовано в следующей версии
- Использовать `System.Text.Json` для сериализации
- При загрузке из JSON необходимо загрузить материалы из репозитория

---

**Конец документа**