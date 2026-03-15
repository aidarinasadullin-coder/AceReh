# Task 7.1: Тесты для MaterialRepository

**Этап:** 7. Тесты  
**Приоритет:** P2 (Средняя)  
**Время:** 1 час  
**Зависимости:** Task 2.1

---

## 1. Цель задачи

Создать unit-тесты для `MaterialRepository`.

---

## 2. Описание изменений

### 2.1. Создать файл MaterialRepositoryTests.cs

**Путь:** `tests/Services/Construction/MaterialRepositoryTests.cs`

**Код:**

```csharp
using System.IO;
using System.Threading.Tasks;
using SnowMeltingCalculator.Services.Construction;
using Xunit;

namespace SnowMeltingCalculator.Tests.Services.Construction
{
    /// <summary>
    /// Тесты для MaterialRepository
    /// </summary>
    public class MaterialRepositoryTests
    {
        private const string TestMaterialsPath = "data/materials_db.json";

        [Fact]
        public async Task LoadMaterialsAsync_ShouldLoadMaterials()
        {
            // Arrange
            var repository = new MaterialRepository(TestMaterialsPath);

            // Act
            var materials = await repository.LoadMaterialsAsync();

            // Assert
            Assert.NotEmpty(materials);
            Assert.True(repository.IsLoaded);
            Assert.True(repository.MaterialsCount > 0);
        }

        [Fact]
        public async Task GetMaterialById_ShouldReturnMaterial()
        {
            // Arrange
            var repository = new MaterialRepository(TestMaterialsPath);
            await repository.LoadMaterialsAsync();

            // Act
            var material = repository.GetMaterialById(1);

            // Assert
            Assert.NotNull(material);
            Assert.Equal(1, material!.Id);
            Assert.Equal("Песок", material.Name);
        }

        [Fact]
        public async Task GetMaterialById_NotFound_ShouldReturnNull()
        {
            // Arrange
            var repository = new MaterialRepository(TestMaterialsPath);
            await repository.LoadMaterialsAsync();

            // Act
            var material = repository.GetMaterialById(999);

            // Assert
            Assert.Null(material);
        }

        [Fact]
        public async Task GetMaterialsByCategory_ShouldReturnMaterials()
        {
            // Arrange
            var repository = new MaterialRepository(TestMaterialsPath);
            await repository.LoadMaterialsAsync();

            // Act
            var materials = repository.GetMaterialsByCategory("бетон");

            // Assert
            Assert.NotEmpty(materials);
            Assert.All(materials, m => Assert.Equal("бетон", m.Category, ignoreCase: true));
        }

        [Fact]
        public async Task GetDefaultMaterial_ShouldReturnConcrete()
        {
            // Arrange
            var repository = new MaterialRepository(TestMaterialsPath);
            await repository.LoadMaterialsAsync();

            // Act
            var material = repository.GetDefaultMaterial();

            // Assert
            Assert.NotNull(material);
            Assert.Equal("Бетон плотный", material.Name);
        }

        [Fact]
        public async Task LoadMaterialsAsync_FileNotFound_ShouldThrow()
        {
            // Arrange
            var repository = new MaterialRepository("nonexistent.json");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => repository.LoadMaterialsAsync());
        }

        [Fact]
        public async Task GetMaterialById_BeforeLoad_ShouldThrow()
        {
            // Arrange
            var repository = new MaterialRepository(TestMaterialsPath);

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => repository.GetMaterialById(1));
        }
    }
}
```

---

## 3. Критерии приёмки

- [ ] Файл `tests/Services/Construction/MaterialRepositoryTests.cs` создан
- [ ] Тест `LoadMaterialsAsync_ShouldLoadMaterials` проходит
- [ ] Тест `GetMaterialById_ShouldReturnMaterial` проходит
- [ ] Тест `GetMaterialsByCategory_ShouldReturnMaterials` проходит
- [ ] Тест `GetDefaultMaterial_ShouldReturnConcrete` проходит
- [ ] Тесты на ошибки проходят

---

**Конец документа**