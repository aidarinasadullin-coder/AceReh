# Task 3.1: Создать IConstructionService.cs и ConstructionService.cs

**Этап:** 3. Сервисы  
**Приоритет:** P0 (Критическая)  
**Время:** 2 часа  
**Зависимости:** Task 1.5, Task 2.1

---

## 1. Цель задачи

Создать интерфейс `IConstructionService` и его реализацию `ConstructionService` для расчёта термических сопротивлений и управления слоями конструкции.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-01 | Добавление слоя материала | CreateDefaultLayer |
| UC-03 | Задание толщины слоя | CalculateR1Total, CalculateR2Total |
| UC-05 | Учёт уровня грунтовых вод | GetLambdaForLayer |
| UC-09 | Интеграция с ThermalViewModel | CalculateR1Total, CalculateR2Total, CalculateLambdaE |

---

## 3. Описание изменений

### 3.1. Создать файл IConstructionService.cs

**Путь:** `src/Services/Construction/IConstructionService.cs`

**Код:**

```csharp
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Интерфейс сервиса расчёта конструкции
    /// </summary>
    public interface IConstructionService
    {
        /// <summary>
        /// Рассчитать суммарное термическое сопротивление над трубой (R1)
        /// </summary>
        /// <param name="construction">Конструкция</param>
        /// <returns>R1Total, м²·К/Вт</returns>
        double CalculateR1Total(Construction construction);

        /// <summary>
        /// Рассчитать суммарное термическое сопротивление под трубой (R2)
        /// </summary>
        /// <param name="construction">Конструкция</param>
        /// <returns>R2Total, м²·К/Вт</returns>
        double CalculateR2Total(Construction construction);

        /// <summary>
        /// Определить теплопроводность материала вокруг трубы (LambdaE)
        /// </summary>
        /// <param name="construction">Конструкция</param>
        /// <returns>LambdaE, Вт/м·К</returns>
        double CalculateLambdaE(Construction construction);

        /// <summary>
        /// Получить λ для слоя в зависимости от УГВ
        /// </summary>
        /// <param name="material">Материал слоя</param>
        /// <param name="position">Позиция слоя</param>
        /// <param name="groundwaterLevel">Уровень грунтовых вод, м</param>
        /// <returns>Теплопроводность λ, Вт/м·К</returns>
        double GetLambdaForLayer(Material material, LayerPosition position, double groundwaterLevel);

        /// <summary>
        /// Создать слой с материалом по умолчанию
        /// </summary>
        /// <param name="position">Позиция слоя</param>
        /// <param name="groundwaterLevel">Уровень грунтовых вод, м</param>
        /// <returns>Новый слой</returns>
        Layer CreateDefaultLayer(LayerPosition position, double groundwaterLevel);

        /// <summary>
        /// Применить шаблон конструкции
        /// </summary>
        /// <param name="template">Шаблон</param>
        /// <returns>Конструкция</returns>
        Construction ApplyTemplate(ConstructionTemplate template);
    }
}
```

### 3.2. Создать файл ConstructionService.cs

**Путь:** `src/Services/Construction/ConstructionService.cs`

**Код:**

```csharp
using System;
using System.Linq;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Сервис расчёта конструкции
    /// </summary>
    /// <remarks>
    /// Реализует расчёт термических сопротивлений R1, R2, LambdaE
    /// </remarks>
    public class ConstructionService : IConstructionService
    {
        #region Поля

        private readonly IMaterialRepository _materialRepository;

        #endregion

        #region Конструктор

        /// <summary>
        /// Создать сервис расчёта конструкции
        /// </summary>
        /// <param name="materialRepository">Репозиторий материалов</param>
        public ConstructionService(IMaterialRepository materialRepository)
        {
            _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
        }

        #endregion

        #region Методы расчёта

        /// <inheritdoc/>
        public double CalculateR1Total(Construction construction)
        {
            if (construction == null)
            {
                throw new ArgumentNullException(nameof(construction));
            }

            // R1Total = Σ(R_i) для всех слоёв над трубой
            // R_i = d / λ / 1000
            return construction.LayersAbovePipe.Sum(layer => layer.ThermalResistance);
        }

        /// <inheritdoc/>
        public double CalculateR2Total(Construction construction)
        {
            if (construction == null)
            {
                throw new ArgumentNullException(nameof(construction));
            }

            // R2Total = Σ(R_i) для всех слоёв под трубой
            // R_i = d / λ / 1000
            return construction.LayersBelowPipe.Sum(layer => layer.ThermalResistance);
        }

        /// <inheritdoc/>
        public double CalculateLambdaE(Construction construction)
        {
            if (construction == null)
            {
                throw new ArgumentNullException(nameof(construction));
            }

            // LambdaE = λ материала первого слоя над трубой
            // Если слоёв нет, используем значение по умолчанию 1.6 Вт/м·К (бетон)
            var firstLayer = construction.LayersAbovePipe.FirstOrDefault();
            
            if (firstLayer == null)
            {
                return 1.6; // Значение по умолчанию
            }

            return firstLayer.Lambda;
        }

        /// <inheritdoc/>
        public double GetLambdaForLayer(Material material, LayerPosition position, double groundwaterLevel)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            // Слои над трубой всегда используют λА
            if (position == LayerPosition.AbovePipe)
            {
                return material.LambdaA;
            }

            // Слои под трубой: λБ при УГВ < 1м, λА при УГВ >= 1м
            return groundwaterLevel < 1.0 ? material.LambdaB : material.LambdaA;
        }

        #endregion

        #region Методы создания

        /// <inheritdoc/>
        public Layer CreateDefaultLayer(LayerPosition position, double groundwaterLevel)
        {
            // Получаем материал по умолчанию (Бетон плотный)
            var defaultMaterial = _materialRepository.GetDefaultMaterial();

            // Определяем λ в зависимости от позиции и УГВ
            var lambda = GetLambdaForLayer(defaultMaterial, position, groundwaterLevel);

            return new Layer
            {
                Material = defaultMaterial,
                Thickness = 50.0, // Толщина по умолчанию
                Lambda = lambda,
                Position = position,
                Order = 0,
                IsLambdaOverridden = false
            };
        }

        /// <inheritdoc/>
        public Construction ApplyTemplate(ConstructionTemplate template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            // Убедимся, что материалы загружены
            if (!_materialRepository.IsLoaded)
            {
                _materialRepository.LoadMaterialsAsync().GetAwaiter().GetResult();
            }

            // Создаём конструкцию из шаблона
            var construction = new Construction
            {
                HasLoads = template.HasLoads,
                GroundwaterLevel = template.DefaultGroundwaterLevel
            };

            // Добавляем слои над трубой
            foreach (var layerTemplate in template.LayersAbovePipe.OrderBy(t => t.Order))
            {
                var material = _materialRepository.GetMaterialById(layerTemplate.MaterialId);
                if (material != null)
                {
                    construction.AddLayerAbovePipe(material, layerTemplate.Thickness);
                }
            }

            // Добавляем слои под трубой
            foreach (var layerTemplate in template.LayersBelowPipe.OrderBy(t => t.Order))
            {
                var material = _materialRepository.GetMaterialById(layerTemplate.MaterialId);
                if (material != null)
                {
                    construction.AddLayerBelowPipe(material, layerTemplate.Thickness);
                }
            }

            return construction;
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

### TC-3.1.1: Расчёт R1Total

```csharp
[Fact]
public void ConstructionService_CalculateR1Total_ShouldCalculateCorrectly()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var construction = new Construction();
    var material = materialRepo.GetDefaultMaterial();
    
    construction.AddLayerAbovePipe(material, 50.0);  // R = 50/1.5/1000 = 0.0333
    construction.AddLayerAbovePipe(material, 100.0); // R = 100/1.5/1000 = 0.0667

    // Act
    var r1Total = service.CalculateR1Total(construction);

    // Assert
    Assert.Equal(0.1, r1Total, precision: 3);
}
```

### TC-3.1.2: Расчёт R2Total

```csharp
[Fact]
public void ConstructionService_CalculateR2Total_ShouldCalculateCorrectly()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var construction = new Construction { GroundwaterLevel = 2.0 };
    var material = materialRepo.GetDefaultMaterial();
    
    construction.AddLayerBelowPipe(material, 150.0); // R = 150/1.5/1000 = 0.1

    // Act
    var r2Total = service.CalculateR2Total(construction);

    // Assert
    Assert.Equal(0.1, r2Total, precision: 3);
}
```

### TC-3.1.3: Расчёт LambdaE

```csharp
[Fact]
public void ConstructionService_CalculateLambdaE_ShouldReturnFirstLayerLambda()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var construction = new Construction();
    var material = materialRepo.GetDefaultMaterial();
    
    construction.AddLayerAbovePipe(material, 50.0);

    // Act
    var lambdaE = service.CalculateLambdaE(construction);

    // Assert
    Assert.Equal(1.5, lambdaE); // LambdaA материала по умолчанию
}
```

### TC-3.1.4: LambdaE для пустой конструкции

```csharp
[Fact]
public void ConstructionService_CalculateLambdaE_EmptyConstruction_ShouldReturnDefault()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var construction = new Construction();

    // Act
    var lambdaE = service.CalculateLambdaE(construction);

    // Assert
    Assert.Equal(1.6, lambdaE); // Значение по умолчанию
}
```

### TC-3.1.5: GetLambdaForLayer — над трубой

```csharp
[Fact]
public void ConstructionService_GetLambdaForLayer_AbovePipe_ShouldReturnLambdaA()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };

    // Act
    var lambda = service.GetLambdaForLayer(material, LayerPosition.AbovePipe, groundwaterLevel: 0.5);

    // Assert
    Assert.Equal(0.4, lambda); // Всегда λА для слоёв над трубой
}
```

### TC-3.1.6: GetLambdaForLayer — под трубой (влажные условия)

```csharp
[Fact]
public void ConstructionService_GetLambdaForLayer_BelowPipe_WetConditions_ShouldReturnLambdaB()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };

    // Act
    var lambda = service.GetLambdaForLayer(material, LayerPosition.BelowPipe, groundwaterLevel: 0.5);

    // Assert
    Assert.Equal(2.0, lambda); // λБ при УГВ < 1м
}
```

### TC-3.1.7: GetLambdaForLayer — под трубой (сухие условия)

```csharp
[Fact]
public void ConstructionService_GetLambdaForLayer_BelowPipe_DryConditions_ShouldReturnLambdaA()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };

    // Act
    var lambda = service.GetLambdaForLayer(material, LayerPosition.BelowPipe, groundwaterLevel: 2.0);

    // Assert
    Assert.Equal(0.4, lambda); // λА при УГВ >= 1м
}
```

### TC-3.1.8: Создание слоя по умолчанию

```csharp
[Fact]
public void ConstructionService_CreateDefaultLayer_ShouldCreateLayerWithDefaultMaterial()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);

    // Act
    var layer = service.CreateDefaultLayer(LayerPosition.AbovePipe, groundwaterLevel: 2.0);

    // Assert
    Assert.NotNull(layer.Material);
    Assert.Equal("Бетон плотный", layer.Material.Name);
    Assert.Equal(50.0, layer.Thickness);
    Assert.Equal(LayerPosition.AbovePipe, layer.Position);
}
```

---

## 5. Критерии приёмки

- [ ] Файл `src/Services/Construction/IConstructionService.cs` создан
- [ ] Файл `src/Services/Construction/ConstructionService.cs` создан
- [ ] Метод `CalculateR1Total()` корректно вычисляет R1
- [ ] Метод `CalculateR2Total()` корректно вычисляет R2
- [ ] Метод `CalculateLambdaE()` возвращает λ первого слоя
- [ ] Метод `GetLambdaForLayer()` учитывает УГВ
- [ ] Метод `CreateDefaultLayer()` создаёт слой с материалом по умолчанию
- [ ] XML-документация добавлена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- Формула расчёта R: `R = d / λ / 1000` (d в мм, λ в Вт/м·К)
- LambdaE определяется как λ первого слоя над трубой
- При отсутствии слоёв LambdaE = 1.6 Вт/м·К (значение по умолчанию)

---

**Конец документа**