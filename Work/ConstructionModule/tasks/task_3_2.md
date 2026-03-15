# Task 3.2: Реализовать расчёт λ в зависимости от УГВ

**Этап:** 3. Сервисы  
**Приоритет:** P2 (Средняя)  
**Время:** 1 час  
**Зависимости:** Task 3.1

---

## 1. Цель задачи

Реализовать логику автоматического пересчёта λ для слоёв под трубой при изменении уровня грунтовых вод (УГВ).

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-05 | Учёт уровня грунтовых вод | UpdateLambdaForGroundwater |

---

## 3. Описание изменений

### 3.1. Обновить Construction.cs

**Файл:** `src/Models/Construction/Construction.cs`

**Изменения:**

Метод `UpdateLambdaForGroundwater()` уже реализован в Task 1.5. В этой задаче нужно убедиться, что он работает корректно и добавить дополнительные тесты.

### 3.2. Обновить ConstructionService.cs

**Файл:** `src/Services/Construction/ConstructionService.cs`

**Добавить метод:**

```csharp
/// <summary>
/// Обновить λ для всех слоёв конструкции при изменении УГВ
/// </summary>
/// <param name="construction">Конструкция</param>
/// <param name="newGroundwaterLevel">Новый уровень грунтовых вод, м</param>
public void UpdateGroundwaterLevel(Construction construction, double newGroundwaterLevel)
{
    if (construction == null)
    {
        throw new ArgumentNullException(nameof(construction));
    }

    // Обновляем УГВ в конструкции
    construction.GroundwaterLevel = newGroundwaterLevel;

    // Обновляем λ для всех слоёв под трубой
    foreach (var layer in construction.LayersBelowPipe)
    {
        if (!layer.IsLambdaOverridden)
        {
            layer.Lambda = GetLambdaForLayer(layer.Material, LayerPosition.BelowPipe, newGroundwaterLevel);
        }
    }

    // Вызываем событие изменения данных
    construction.RaiseDataChanged("GroundwaterLevel", null, newGroundwaterLevel, construction.IsValid);
}
```

---

## 4. Тест-кейсы

### TC-3.2.1: Обновление λ при изменении УГВ (сухие → влажные)

```csharp
[Fact]
public void ConstructionService_UpdateGroundwaterLevel_DryToWet_ShouldUpdateLambda()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var construction = new Construction { GroundwaterLevel = 2.0 }; // Сухие условия
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };
    
    construction.AddLayerBelowPipe(material, 150.0);
    
    // До изменения: λ = 0.4 (λА, сухие условия)
    Assert.Equal(0.4, construction.LayersBelowPipe[0].Lambda);

    // Act
    service.UpdateGroundwaterLevel(construction, 0.5); // Влажные условия

    // Assert
    Assert.Equal(2.0, construction.LayersBelowPipe[0].Lambda); // λБ
    Assert.Equal(0.5, construction.GroundwaterLevel);
}
```

### TC-3.2.2: Обновление λ при изменении УГВ (влажные → сухие)

```csharp
[Fact]
public void ConstructionService_UpdateGroundwaterLevel_WetToDry_ShouldUpdateLambda()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var construction = new Construction { GroundwaterLevel = 0.5 }; // Влажные условия
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };
    
    construction.AddLayerBelowPipe(material, 150.0);
    
    // До изменения: λ = 2.0 (λБ, влажные условия)
    Assert.Equal(2.0, construction.LayersBelowPipe[0].Lambda);

    // Act
    service.UpdateGroundwaterLevel(construction, 2.0); // Сухие условия

    // Assert
    Assert.Equal(0.4, construction.LayersBelowPipe[0].Lambda); // λА
    Assert.Equal(2.0, construction.GroundwaterLevel);
}
```

### TC-3.2.3: Слои над трубой не изменяются

```csharp
[Fact]
public void ConstructionService_UpdateGroundwaterLevel_AbovePipe_ShouldNotChange()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var construction = new Construction { GroundwaterLevel = 2.0 };
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };
    
    construction.AddLayerAbovePipe(material, 50.0);
    
    var lambdaBefore = construction.LayersAbovePipe[0].Lambda;

    // Act
    service.UpdateGroundwaterLevel(construction, 0.5);

    // Assert
    Assert.Equal(lambdaBefore, construction.LayersAbovePipe[0].Lambda); // Не изменилось
}
```

### TC-3.2.4: Ручное переопределение λ сохраняется

```csharp
[Fact]
public void ConstructionService_UpdateGroundwaterLevel_OverriddenLambda_ShouldNotChange()
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var construction = new Construction { GroundwaterLevel = 2.0 };
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };
    
    construction.AddLayerBelowPipe(material, 150.0);
    
    // Ручное переопределение λ
    construction.LayersBelowPipe[0].Lambda = 1.0;
    construction.LayersBelowPipe[0].IsLambdaOverridden = true;

    // Act
    service.UpdateGroundwaterLevel(construction, 0.5);

    // Assert
    Assert.Equal(1.0, construction.LayersBelowPipe[0].Lambda); // Не изменилось
}
```

### TC-3.2.5: Граничное значение УГВ = 1.0 м

```csharp
[Theory]
[InlineData(0.99, 2.0)]  // УГВ < 1м → λБ
[InlineData(1.0, 0.4)]   // УГВ = 1м → λА
[InlineData(1.01, 0.4)] // УГВ > 1м → λА
public void ConstructionService_UpdateGroundwaterLevel_Boundary_ShouldUseCorrectLambda(
    double groundwaterLevel, double expectedLambda)
{
    // Arrange
    var materialRepo = new MockMaterialRepository();
    var service = new ConstructionService(materialRepo);
    
    var construction = new Construction { GroundwaterLevel = 2.0 };
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };
    
    construction.AddLayerBelowPipe(material, 150.0);

    // Act
    service.UpdateGroundwaterLevel(construction, groundwaterLevel);

    // Assert
    Assert.Equal(expectedLambda, construction.LayersBelowPipe[0].Lambda);
}
```

---

## 5. Критерии приёмки

- [ ] Метод `UpdateGroundwaterLevel()` в `ConstructionService` добавлен
- [ ] При изменении УГВ λ для слоёв под трубой пересчитывается
- [ ] Слои над трубой не изменяются
- [ ] Ручное переопределение λ сохраняется
- [ ] Граничное значение УГВ = 1.0 м обрабатывается корректно
- [ ] XML-документация добавлена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- УГВ < 1 м → влажные условия → λБ
- УГВ >= 1 м → сухие условия → λА
- Слои над трубой всегда используют λА
- Флаг `IsLambdaOverridden` защищает от автоматического пересчёта

---

**Конец документа**