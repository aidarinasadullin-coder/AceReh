# Task 7.2: Тесты для ConstructionService

**Этап:** 7. Тесты  
**Приоритет:** P2 (Средняя)  
**Время:** 2 часа  
**Зависимости:** Task 3.1

---

## 1. Цель задачи

Создать unit-тесты для `ConstructionService`.

---

## 2. Описание изменений

### 2.1. Создать файл ConstructionServiceTests.cs

**Путь:** `tests/Services/Construction/ConstructionServiceTests.cs`

**Код:**

```csharp
using System;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Services.Construction;
using Xunit;

namespace SnowMeltingCalculator.Tests.Services.Construction
{
    /// <summary>
    /// Тесты для ConstructionService
    /// </summary>
    public class ConstructionServiceTests
    {
        private readonly IConstructionService _service;
        private readonly MockMaterialRepository _materialRepository;

        public ConstructionServiceTests()
        {
            _materialRepository = new MockMaterialRepository();
            _service = new ConstructionService(_materialRepository);
        }

        [Fact]
        public void CalculateR1Total_ShouldCalculateCorrectly()
        {
            // Arrange
            var construction = new Construction();
            var material = _materialRepository.GetDefaultMaterial();

            construction.AddLayerAbovePipe(material, 50.0);  // R = 50/1.5/1000 = 0.0333
            construction.AddLayerAbovePipe(material, 100.0); // R = 100/1.5/1000 = 0.0667

            // Act
            var r1Total = _service.CalculateR1Total(construction);

            // Assert
            Assert.Equal(0.1, r1Total, 3);
        }

        [Fact]
        public void CalculateR2Total_ShouldCalculateCorrectly()
        {
            // Arrange
            var construction = new Construction { GroundwaterLevel = 2.0 };
            var material = _materialRepository.GetDefaultMaterial();

            construction.AddLayerBelowPipe(material, 150.0); // R = 150/1.5/1000 = 0.1

            // Act
            var r2Total = _service.CalculateR2Total(construction);

            // Assert
            Assert.Equal(0.1, r2Total, 3);
        }

        [Fact]
        public void CalculateLambdaE_ShouldReturnFirstLayerLambda()
        {
            // Arrange
            var construction = new Construction();
            var material = _materialRepository.GetDefaultMaterial();

            construction.AddLayerAbovePipe(material, 50.0);

            // Act
            var lambdaE = _service.CalculateLambdaE(construction);

            // Assert
            Assert.Equal(1.5, lambdaE);
        }

        [Fact]
        public void CalculateLambdaE_EmptyConstruction_ShouldReturnDefault()
        {
            // Arrange
            var construction = new Construction();

            // Act
            var lambdaE = _service.CalculateLambdaE(construction);

            // Assert
            Assert.Equal(1.6, lambdaE);
        }

        [Fact]
        public void GetLambdaForLayer_AbovePipe_ShouldReturnLambdaA()
        {
            // Arrange
            var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };

            // Act
            var lambda = _service.GetLambdaForLayer(material, LayerPosition.AbovePipe, groundwaterLevel: 0.5);

            // Assert
            Assert.Equal(0.4, lambda);
        }

        [Theory]
        [InlineData(0.5, 2.0)]  // УГВ < 1м → λБ
        [InlineData(1.0, 0.4)]  // УГВ = 1м → λА
        [InlineData(2.0, 0.4)]  // УГВ > 1м → λА
        public void GetLambdaForLayer_BelowPipe_ShouldReturnCorrectLambda(double groundwaterLevel, double expectedLambda)
        {
            // Arrange
            var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };

            // Act
            var lambda = _service.GetLambdaForLayer(material, LayerPosition.BelowPipe, groundwaterLevel);

            // Assert
            Assert.Equal(expectedLambda, lambda);
        }

        [Fact]
        public void CreateDefaultLayer_ShouldCreateLayerWithDefaultMaterial()
        {
            // Act
            var layer = _service.CreateDefaultLayer(LayerPosition.AbovePipe, groundwaterLevel: 2.0);

            // Assert
            Assert.NotNull(layer.Material);
            Assert.Equal("Бетон плотный", layer.Material.Name);
            Assert.Equal(50.0, layer.Thickness);
            Assert.Equal(LayerPosition.AbovePipe, layer.Position);
            Assert.False(layer.IsLambdaOverridden);
        }

        [Fact]
        public void CreateDefaultLayer_BelowPipe_WetConditions_ShouldUseLambdaB()
        {
            // Act
            var layer = _service.CreateDefaultLayer(LayerPosition.BelowPipe, groundwaterLevel: 0.5);

            // Assert
            Assert.Equal(2.0, layer.Lambda); // λБ для песка при УГВ < 1м
        }

        [Fact]
        public void CalculateR1Total_NullConstruction_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.CalculateR1Total(null!));
        }

        [Fact]
        public void CalculateR2Total_NullConstruction_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.CalculateR2Total(null!));
        }

        [Fact]
        public void GetLambdaForLayer_NullMaterial_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.GetLambdaForLayer(null!, LayerPosition.AbovePipe, 2.0));
        }
    }

    /// <summary>
    /// Мок-репозиторий материалов для тестов
    /// </summary>
    public class MockMaterialRepository : IMaterialRepository
    {
        private readonly List<Material> _materials;

        public MockMaterialRepository()
        {
            _materials = new List<Material>
            {
                new Material { Id = 1, Name = "Песок", LambdaA = 0.4, LambdaB = 2.0, Category = "грунт" },
                new Material { Id = 2, Name = "Грунт", LambdaA = 0.5, LambdaB = 1.5, Category = "грунт" },
                new Material { Id = 5, Name = "Бетон плотный", LambdaA = 1.5, LambdaB = 1.5, Category = "бетон" }
            };
        }

        public Task<IEnumerable<Material>> LoadMaterialsAsync()
        {
            IsLoaded = true;
            return Task.FromResult<IEnumerable<Material>>(_materials);
        }

        public Material? GetMaterialById(int id)
        {
            return _materials.FirstOrDefault(m => m.Id == id);
        }

        public IEnumerable<Material> GetMaterialsByCategory(string category)
        {
            return _materials.Where(m => m.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        public Material GetDefaultMaterial()
        {
            return _materials.First(m => m.Id == 5);
        }

        public bool IsLoaded { get; private set; }
        public int MaterialsCount => _materials.Count;
    }
}
```

---

## 3. Критерии приёмки

- [ ] Файл `tests/Services/Construction/ConstructionServiceTests.cs` создан
- [ ] Тесты расчёта R1Total проходят
- [ ] Тесты расчёта R2Total проходят
- [ ] Тесты расчёта LambdaE проходят
- [ ] Тесты GetLambdaForLayer проходят
- [ ] Тесты CreateDefaultLayer проходят
- [ ] Тесты на null-параметры проходят

---

**Конец документа**