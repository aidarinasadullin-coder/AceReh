# Task 3.3: Создать ConstructionValidator.cs

**Этап:** 3. Сервисы  
**Приоритет:** P2 (Средняя)  
**Время:** 2 часа  
**Зависимости:** Task 1.5, Task 1.3

---

## 1. Цель задачи

Создать класс `ConstructionValidator` для валидации конструкции по правилам из ТЗ.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-06 | Валидация минимальной стяжки | Validate (толщина стяжки) |
| UC-07 | Проверка ограничений по материалам | Validate (бетон, асфальт) |

---

## 3. Описание изменений

### 3.1. Создать файл ConstructionValidator.cs

**Путь:** `src/Services/Construction/ConstructionValidator.cs`

**Код:**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Валидатор конструкции
    /// </summary>
    /// <remarks>
    /// Проверяет конструкцию на соответствие правилам:
    /// - Минимальная толщина стяжки над трубой (40 мм без нагрузок, 50 мм с нагрузками)
    /// - Толщина слоёв (10-1000 мм)
    /// - Уровень грунтовых вод (0-10 м)
    /// - Ограничения по материалам (бетон: макс. температура подачи 50°C, асфальт: мин. температура воздуха -15°C)
    /// </remarks>
    public class ConstructionValidator
    {
        #region Константы

        /// <summary>
        /// Минимальная толщина стяжки без нагрузок, мм
        /// </summary>
        private const double MinThicknessWithoutLoads = 40.0;

        /// <summary>
        /// Минимальная толщина стяжки с нагрузками, мм
        /// </summary>
        private const double MinThicknessWithLoads = 50.0;

        /// <summary>
        /// Минимальная толщина слоя, мм
        /// </summary>
        private const double MinLayerThickness = 10.0;

        /// <summary>
        /// Максимальная толщина слоя, мм
        /// </summary>
        private const double MaxLayerThickness = 1000.0;

        /// <summary>
        /// Минимальный уровень грунтовых вод, м
        /// </summary>
        private const double MinGroundwaterLevel = 0.0;

        /// <summary>
        /// Максимальный уровень грунтовых вод, м
        /// </summary>
        private const double MaxGroundwaterLevel = 10.0;

        /// <summary>
        /// Максимальная температура подачи для бетона, °C
        /// </summary>
        private const double MaxSupplyTemperatureForConcrete = 50.0;

        /// <summary>
        /// Минимальная температура воздуха для асфальта, °C
        /// </summary>
        private const double MinAirTemperatureForAsphalt = -15.0;

        #endregion

        #region Методы валидации

        /// <summary>
        /// Валидация конструкции
        /// </summary>
        /// <param name="construction">Конструкция для валидации</param>
        /// <param name="supplyTemperature">Температура подачи, °C</param>
        /// <param name="airTemperature">Температура наружного воздуха, °C</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate(Construction construction, double supplyTemperature = 50.0, double airTemperature = -20.0)
        {
            if (construction == null)
            {
                throw new ArgumentNullException(nameof(construction));
            }

            var result = new ValidationResult();

            // 1. Проверка наличия слоёв
            ValidateLayersPresence(construction, result);

            // 2. Проверка минимальной стяжки над трубой
            ValidateMinScreedThickness(construction, result);

            // 3. Проверка толщины слоёв
            ValidateLayerThickness(construction, result);

            // 4. Проверка уровня грунтовых вод
            ValidateGroundwaterLevel(construction, result);

            // 5. Проверка ограничений по материалам
            ValidateMaterialConstraints(construction, supplyTemperature, airTemperature, result);

            return result;
        }

        /// <summary>
        /// Быстрая проверка валидности (без сообщений об ошибках)
        /// </summary>
        /// <param name="construction">Конструкция</param>
        /// <returns>true если конструкция валидна</returns>
        public bool IsValid(Construction construction)
        {
            var result = Validate(construction);
            return result.IsValid;
        }

        #endregion

        #region Приватные методы валидации

        /// <summary>
        /// Проверка наличия слоёв
        /// </summary>
        private void ValidateLayersPresence(Construction construction, ValidationResult result)
        {
            if (construction.LayersAbovePipe.Count == 0 && construction.LayersBelowPipe.Count == 0)
            {
                result.AddError("Конструкция должна содержать хотя бы один слой");
            }
        }

        /// <summary>
        /// Проверка минимальной толщины стяжки над трубой
        /// </summary>
        private void ValidateMinScreedThickness(Construction construction, ValidationResult result)
        {
            var minThickness = construction.HasLoads ? MinThicknessWithLoads : MinThicknessWithoutLoads;
            var totalAbove = construction.LayersAbovePipe.Sum(l => l.Thickness);

            if (totalAbove < minThickness)
            {
                var loadsText = construction.HasLoads ? "при нагрузках" : "без нагрузок";
                result.AddError($"Минимальная толщина слоёв над трубой {loadsText}: {minThickness} мм (текущая: {totalAbove:F0} мм)");
            }
        }

        /// <summary>
        /// Проверка толщины слоёв
        /// </summary>
        private void ValidateLayerThickness(Construction construction, ValidationResult result)
        {
            foreach (var layer in construction.LayersAbovePipe.Concat(construction.LayersBelowPipe))
            {
                if (layer.Thickness < MinLayerThickness)
                {
                    result.AddError($"Толщина слоя '{layer.Material.Name}' должна быть не менее {MinLayerThickness} мм (текущая: {layer.Thickness:F0} мм)");
                }
                else if (layer.Thickness > MaxLayerThickness)
                {
                    result.AddError($"Толщина слоя '{layer.Material.Name}' должна быть не более {MaxLayerThickness} мм (текущая: {layer.Thickness:F0} мм)");
                }
            }
        }

        /// <summary>
        /// Проверка уровня грунтовых вод
        /// </summary>
        private void ValidateGroundwaterLevel(Construction construction, ValidationResult result)
        {
            if (construction.GroundwaterLevel < MinGroundwaterLevel || construction.GroundwaterLevel > MaxGroundwaterLevel)
            {
                result.AddError($"Уровень грунтовых вод должен быть от {MinGroundwaterLevel} до {MaxGroundwaterLevel} м (текущий: {construction.GroundwaterLevel:F1} м)");
            }
        }

        /// <summary>
        /// Проверка ограничений по материалам
        /// </summary>
        private void ValidateMaterialConstraints(Construction construction, double supplyTemperature, double airTemperature, ValidationResult result)
        {
            foreach (var layer in construction.LayersAbovePipe)
            {
                // Проверка бетона: макс. температура подачи 50°C
                if (layer.Material.Category.Equals("бетон", StringComparison.OrdinalIgnoreCase) ||
                    layer.Material.MaxSupplyTemperature.HasValue)
                {
                    var maxTemp = layer.Material.MaxSupplyTemperature ?? MaxSupplyTemperatureForConcrete;
                    if (supplyTemperature > maxTemp)
                    {
                        result.AddError($"Материал '{layer.Material.Name}': максимальная температура подачи {maxTemp}°C (текущая: {supplyTemperature:F1}°C)");
                    }
                }

                // Проверка асфальта: мин. температура воздуха -15°C
                if (layer.Material.Name.Contains("Асфальт", StringComparison.OrdinalIgnoreCase) ||
                    layer.Material.MinAirTemperature.HasValue)
                {
                    var minTemp = layer.Material.MinAirTemperature ?? MinAirTemperatureForAsphalt;
                    if (airTemperature <= minTemp)
                    {
                        result.AddError($"Материал '{layer.Material.Name}' не применяется при температуре наружного воздуха ≤ {minTemp}°C (текущая: {airTemperature:F1}°C)");
                    }
                }
            }
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

### TC-3.3.1: Валидация пустой конструкции

```csharp
[Fact]
public void ConstructionValidator_Validate_EmptyConstruction_ShouldFail()
{
    // Arrange
    var validator = new ConstructionValidator();
    var construction = new Construction();

    // Act
    var result = validator.Validate(construction);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("хотя бы один слой", result.Errors[0]);
}
```

### TC-3.3.2: Валидация минимальной стяжки (без нагрузок)

```csharp
[Fact]
public void ConstructionValidator_Validate_MinScreedWithoutLoads_ShouldFail()
{
    // Arrange
    var validator = new ConstructionValidator();
    var construction = new Construction { HasLoads = false };
    var material = new Material { Name = "Бетон", Category = "бетон", LambdaA = 1.5, LambdaB = 1.5 };
    
    construction.AddLayerAbovePipe(material, 30.0); // < 40 мм

    // Act
    var result = validator.Validate(construction);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("40 мм", result.Errors[0]);
}
```

### TC-3.3.3: Валидация минимальной стяжки (с нагрузками)

```csharp
[Fact]
public void ConstructionValidator_Validate_MinScreedWithLoads_ShouldFail()
{
    // Arrange
    var validator = new ConstructionValidator();
    var construction = new Construction { HasLoads = true };
    var material = new Material { Name = "Бетон", Category = "бетон", LambdaA = 1.5, LambdaB = 1.5 };
    
    construction.AddLayerAbovePipe(material, 40.0); // < 50 мм

    // Act
    var result = validator.Validate(construction);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("50 мм", result.Errors[0]);
}
```

### TC-3.3.4: Валидация толщины слоя

```csharp
[Theory]
[InlineData(5.0, "не менее")]   // < 10 мм
[InlineData(1500.0, "не более")] // > 1000 мм
public void ConstructionValidator_Validate_LayerThickness_ShouldFail(double thickness, string expectedMessage)
{
    // Arrange
    var validator = new ConstructionValidator();
    var construction = new Construction { HasLoads = false };
    var material = new Material { Name = "Бетон", Category = "бетон", LambdaA = 1.5, LambdaB = 1.5 };
    
    construction.AddLayerAbovePipe(material, thickness);

    // Act
    var result = validator.Validate(construction);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains(expectedMessage, result.Errors[0]);
}
```

### TC-3.3.5: Валидация УГВ

```csharp
[Theory]
[InlineData(-1.0)]
[InlineData(15.0)]
public void ConstructionValidator_Validate_GroundwaterLevel_ShouldFail(double groundwaterLevel)
{
    // Arrange
    var validator = new ConstructionValidator();
    var construction = new Construction { GroundwaterLevel = groundwaterLevel };
    var material = new Material { Name = "Бетон", Category = "бетон", LambdaA = 1.5, LambdaB = 1.5 };
    
    construction.AddLayerAbovePipe(material, 50.0);

    // Act
    var result = validator.Validate(construction);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("грунтовых вод", result.Errors[0]);
}
```

### TC-3.3.6: Валидация бетона (температура подачи)

```csharp
[Fact]
public void ConstructionValidator_Validate_ConcreteTemperature_ShouldFail()
{
    // Arrange
    var validator = new ConstructionValidator();
    var construction = new Construction { HasLoads = false };
    var material = new Material 
    { 
        Name = "Бетон плотный", 
        Category = "бетон", 
        LambdaA = 1.5, 
        LambdaB = 1.5,
        MaxSupplyTemperature = 50.0
    };
    
    construction.AddLayerAbovePipe(material, 50.0);

    // Act
    var result = validator.Validate(construction, supplyTemperature: 60.0);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("50°C", result.Errors[0]);
}
```

### TC-3.3.7: Валидация асфальта (температура воздуха)

```csharp
[Fact]
public void ConstructionValidator_Validate_AsphaltTemperature_ShouldFail()
{
    // Arrange
    var validator = new ConstructionValidator();
    var construction = new Construction { HasLoads = false };
    var material = new Material 
    { 
        Name = "Асфальт", 
        Category = "покрытие", 
        LambdaA = 0.75, 
        LambdaB = 0.75,
        MinAirTemperature = -15.0
    };
    
    construction.AddLayerAbovePipe(material, 50.0);

    // Act
    var result = validator.Validate(construction, supplyTemperature: 50.0, airTemperature: -20.0);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("-15°C", result.Errors[0]);
}
```

### TC-3.3.8: Валидная конструкция

```csharp
[Fact]
public void ConstructionValidator_Validate_ValidConstruction_ShouldPass()
{
    // Arrange
    var validator = new ConstructionValidator();
    var construction = new Construction { HasLoads = false, GroundwaterLevel = 2.0 };
    var concrete = new Material { Name = "Бетон плотный", Category = "бетон", LambdaA = 1.5, LambdaB = 1.5 };
    var sand = new Material { Name = "Песок", Category = "грунт", LambdaA = 0.4, LambdaB = 2.0 };
    
    construction.AddLayerAbovePipe(concrete, 50.0);
    construction.AddLayerBelowPipe(sand, 150.0);

    // Act
    var result = validator.Validate(construction, supplyTemperature: 50.0, airTemperature: -10.0);

    // Assert
    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
}
```

---

## 5. Критерии приёмки

- [ ] Файл `src/Services/Construction/ConstructionValidator.cs` создан
- [ ] Валидация минимальной стяжки работает (40 мм без нагрузок, 50 мм с нагрузками)
- [ ] Валидация толщины слоёв работает (10-1000 мм)
- [ ] Валидация УГВ работает (0-10 м)
- [ ] Валидация ограничений по материалам работает (бетон, асфальт)
- [ ] XML-документация добавлена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- Валидация выполняется при каждом изменении конструкции
- Результат валидации передаётся в ThermalViewModel через `IsValid`
- Предупреждения отображаются в UI через `ValidationMessage`

---

**Конец документа**