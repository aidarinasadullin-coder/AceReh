# Task 2.1: Создать IMaterialRepository.cs и MaterialRepository.cs

**Этап:** 2. Репозитории  
**Приоритет:** P1 (Высокая)  
**Время:** 2 часа  
**Зависимости:** Task 1.1, Task 2.3

---

## 1. Цель задачи

Создать интерфейс `IMaterialRepository` и его реализацию `MaterialRepository` для загрузки материалов из JSON-файла `data/materials_db.json`.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-02 | Выбор материала из справочника | LoadMaterialsAsync, GetMaterialById |

---

## 3. Описание изменений

### 3.1. Создать файл IMaterialRepository.cs

**Путь:** `src/Services/Construction/IMaterialRepository.cs`

**Код:**

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Интерфейс репозитория материалов
    /// </summary>
    public interface IMaterialRepository
    {
        /// <summary>
        /// Загрузить все материалы из справочника
        /// </summary>
        /// <returns>Список материалов</returns>
        Task<IEnumerable<Material>> LoadMaterialsAsync();

        /// <summary>
        /// Получить материал по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор материала</param>
        /// <returns>Материал или null</returns>
        Material? GetMaterialById(int id);

        /// <summary>
        /// Получить материалы по категории
        /// </summary>
        /// <param name="category">Категория материала</param>
        /// <returns>Список материалов</returns>
        IEnumerable<Material> GetMaterialsByCategory(string category);

        /// <summary>
        /// Получить материал по умолчанию (Бетон плотный)
        /// </summary>
        /// <returns>Материал по умолчанию</returns>
        Material GetDefaultMaterial();

        /// <summary>
        /// Признак того, что данные загружены
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Количество загруженных материалов
        /// </summary>
        int MaterialsCount { get; }
    }
}
```

### 3.2. Создать файл MaterialRepository.cs

**Путь:** `src/Services/Construction/MaterialRepository.cs`

**Код:**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Репозиторий материалов
    /// </summary>
    /// <remarks>
    /// Загружает материалы из JSON-файла data/materials_db.json
    /// </remarks>
    public class MaterialRepository : IMaterialRepository
    {
        #region Константы

        /// <summary>
        /// Путь к файлу с материалами по умолчанию
        /// </summary>
        private const string DefaultMaterialsPath = "data/materials_db.json";

        /// <summary>
        /// ID материала по умолчанию (Бетон плотный)
        /// </summary>
        private const int DefaultMaterialId = 5;

        #endregion

        #region Поля

        private readonly string _materialsPath;
        private List<Material>? _materials;
        private Dictionary<int, Material>? _materialsById;

        #endregion

        #region Конструктор

        /// <summary>
        /// Создать репозиторий с путём по умолчанию
        /// </summary>
        public MaterialRepository() : this(DefaultMaterialsPath)
        {
        }

        /// <summary>
        /// Создать репозиторий с указанным путём
        /// </summary>
        /// <param name="materialsPath">Путь к JSON-файлу с материалами</param>
        public MaterialRepository(string materialsPath)
        {
            _materialsPath = materialsPath ?? throw new ArgumentNullException(nameof(materialsPath));
        }

        #endregion

        #region Свойства

        /// <inheritdoc/>
        public bool IsLoaded => _materials != null && _materials.Count > 0;

        /// <inheritdoc/>
        public int MaterialsCount => _materials?.Count ?? 0;

        #endregion

        #region Методы

        /// <inheritdoc/>
        public async Task<IEnumerable<Material>> LoadMaterialsAsync()
        {
            if (_materials != null)
            {
                return _materials;
            }

            try
            {
                // Чтение JSON-файла
                var jsonContent = await File.ReadAllTextAsync(_materialsPath);
                
                // Десериализация
                var jsonModel = JsonSerializer.Deserialize<MaterialsJsonModel>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (jsonModel?.Materials == null || jsonModel.Materials.Count == 0)
                {
                    throw new InvalidOperationException("Файл материалов пуст или имеет неверный формат");
                }

                // Преобразование в доменные модели
                _materials = jsonModel.Materials.Select(m => new Material
                {
                    Id = m.Id,
                    Name = m.Name,
                    LambdaA = m.LambdaA,
                    LambdaB = m.LambdaB,
                    Category = m.Category,
                    Notes = m.Notes ?? string.Empty,
                    MaxSupplyTemperature = m.MaxSupplyTemperature,
                    MinAirTemperature = m.MinAirTemperature
                }).ToList();

                // Создание словаря для быстрого поиска по ID
                _materialsById = _materials.ToDictionary(m => m.Id);

                return _materials;
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException($"Файл материалов не найден: {_materialsPath}");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Ошибка десериализации файла материалов: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public Material? GetMaterialById(int id)
        {
            if (_materialsById == null)
            {
                throw new InvalidOperationException("Материалы не загружены. Вызовите LoadMaterialsAsync() сначала.");
            }

            return _materialsById.TryGetValue(id, out var material) ? material : null;
        }

        /// <inheritdoc/>
        public IEnumerable<Material> GetMaterialsByCategory(string category)
        {
            if (_materials == null)
            {
                throw new InvalidOperationException("Материалы не загружены. Вызовите LoadMaterialsAsync() сначала.");
            }

            return _materials.Where(m => m.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public Material GetDefaultMaterial()
        {
            if (_materialsById == null)
            {
                throw new InvalidOperationException("Материалы не загружены. Вызовите LoadMaterialsAsync() сначала.");
            }

            // Возвращаем "Бетон плотный" (ID = 5)
            if (_materialsById.TryGetValue(DefaultMaterialId, out var material))
            {
                return material;
            }

            // Если не нашли по ID, возвращаем первый материал
            return _materials!.First();
        }

        #endregion

        #region Внутренние модели для JSON

        /// <summary>
        /// Модель для десериализации JSON-файла материалов
        /// </summary>
        private class MaterialsJsonModel
        {
            public MaterialsMetaJsonModel? Meta { get; set; }
            public List<MaterialJsonModel>? Materials { get; set; }
        }

        /// <summary>
        /// Мета-информация JSON-файла
        /// </summary>
        private class MaterialsMetaJsonModel
        {
            public string? Source { get; set; }
            public string? Version { get; set; }
            public string? Date { get; set; }
            public string? Description { get; set; }
        }

        /// <summary>
        /// Модель материала для десериализации
        /// </summary>
        private class MaterialJsonModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public double LambdaA { get; set; }
            public double LambdaB { get; set; }
            public string Category { get; set; } = string.Empty;
            public string? Notes { get; set; }
            public double? MaxSupplyTemperature { get; set; }
            public double? MinAirTemperature { get; set; }
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

### TC-2.1.1: Загрузка материалов

```csharp
[Fact]
public async Task MaterialRepository_LoadMaterialsAsync_ShouldLoadMaterials()
{
    // Arrange
    var repository = new MaterialRepository("data/materials_db.json");

    // Act
    var materials = await repository.LoadMaterialsAsync();

    // Assert
    Assert.NotEmpty(materials);
    Assert.True(repository.IsLoaded);
}
```

### TC-2.1.2: Получение материала по ID

```csharp
[Fact]
public async Task MaterialRepository_GetMaterialById_ShouldReturnMaterial()
{
    // Arrange
    var repository = new MaterialRepository("data/materials_db.json");
    await repository.LoadMaterialsAsync();

    // Act
    var material = repository.GetMaterialById(1);

    // Assert
    Assert.NotNull(material);
    Assert.Equal(1, material!.Id);
    Assert.Equal("Песок", material.Name);
}
```

### TC-2.1.3: Получение материалов по категории

```csharp
[Fact]
public async Task MaterialRepository_GetMaterialsByCategory_ShouldReturnMaterials()
{
    // Arrange
    var repository = new MaterialRepository("data/materials_db.json");
    await repository.LoadMaterialsAsync();

    // Act
    var materials = repository.GetMaterialsByCategory("бетон");

    // Assert
    Assert.NotEmpty(materials);
    Assert.All(materials, m => Assert.Equal("бетон", m.Category, ignoreCase: true));
}
```

### TC-2.1.4: Получение материала по умолчанию

```csharp
[Fact]
public async Task MaterialRepository_GetDefaultMaterial_ShouldReturnConcrete()
{
    // Arrange
    var repository = new MaterialRepository("data/materials_db.json");
    await repository.LoadMaterialsAsync();

    // Act
    var material = repository.GetDefaultMaterial();

    // Assert
    Assert.NotNull(material);
    Assert.Equal("Бетон плотный", material.Name);
}
```

### TC-2.1.5: Ошибка при загрузке без файла

```csharp
[Fact]
public async Task MaterialRepository_LoadMaterialsAsync_FileNotFound_ShouldThrow()
{
    // Arrange
    var repository = new MaterialRepository("nonexistent.json");

    // Act & Assert
    await Assert.ThrowsAsync<FileNotFoundException>(() => repository.LoadMaterialsAsync());
}
```

---

## 5. Критерии приёмки

- [ ] Файл `src/Services/Construction/IMaterialRepository.cs` создан
- [ ] Файл `src/Services/Construction/MaterialRepository.cs` создан
- [ ] Метод `LoadMaterialsAsync()` загружает материалы из JSON
- [ ] Метод `GetMaterialById()` возвращает материал по ID
- [ ] Метод `GetMaterialsByCategory()` фильтрует по категории
- [ ] Метод `GetDefaultMaterial()` возвращает "Бетон плотный"
- [ ] XML-документация добавлена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- Использовать `System.Text.Json` для десериализации
- Кэшировать материалы в памяти после первой загрузки
- Создать словарь `_materialsById` для быстрого поиска

---

**Конец документа**