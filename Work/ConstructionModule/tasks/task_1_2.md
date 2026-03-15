# Task 1.2: Создать Layer.cs и LayerPosition.cs

**Этап:** 1. Модели данных  
**Приоритет:** P0 (Критическая)  
**Время:** 1 час  
**Зависимости:** Task 1.1

---

## 1. Цель задачи

Создать модель данных `Layer` для представления слоя конструкции и enum `LayerPosition` для позиции слоя относительно трубы.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-01 | Добавление слоя материала | Модель слоя |
| UC-03 | Задание толщины слоя | Thickness, ThermalResistance |
| UC-04 | Удаление слоя | Id, Position |
| UC-05 | Учёт уровня грунтовых вод | Lambda, IsLambdaOverridden |

---

## 3. Описание изменений

### 3.1. Создать файл LayerPosition.cs

**Путь:** `src/Models/Construction/LayerPosition.cs`

**Код:**

```csharp
namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Позиция слоя относительно трубы
    /// </summary>
    public enum LayerPosition
    {
        /// <summary>
        /// Над трубой (к поверхности)
        /// </summary>
        /// <remarks>
        /// Слои над трубой всегда используют λА
        /// </remarks>
        AbovePipe = 0,

        /// <summary>
        /// Под трубой (к грунту)
        /// </summary>
        /// <remarks>
        /// Слои под трубой используют λА или λБ в зависимости от УГВ
        /// </remarks>
        BelowPipe = 1
    }
}
```

### 3.2. Создать файл Layer.cs

**Путь:** `src/Models/Construction/Layer.cs`

**Код:**

```csharp
namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Слой конструкции ("Пирога")
    /// </summary>
    /// <remarks>
    /// Представляет один слой материала над трубой или под трубой
    /// </remarks>
    public class Layer
    {
        /// <summary>
        /// Уникальный идентификатор слоя
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Материал слоя
        /// </summary>
        public Material Material { get; set; } = null!;

        /// <summary>
        /// Толщина слоя, мм
        /// </summary>
        /// <remarks>
        /// Диапазон: 10-1000 мм
        /// </remarks>
        public double Thickness { get; set; } = 50.0;

        /// <summary>
        /// Теплопроводность (λ), Вт/м·К
        /// </summary>
        /// <remarks>
        /// Автоматически подставляется из Material, но может быть изменена вручную
        /// </remarks>
        public double Lambda { get; set; }

        /// <summary>
        /// Признак того, что λ изменена вручную
        /// </summary>
        /// <remarks>
        /// Если true, то при изменении УГВ λ не пересчитывается автоматически
        /// </remarks>
        public bool IsLambdaOverridden { get; set; } = false;

        /// <summary>
        /// Позиция слоя относительно трубы
        /// </summary>
        public LayerPosition Position { get; set; }

        /// <summary>
        /// Порядковый номер слоя (от поверхности)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Термическое сопротивление слоя, м²·К/Вт
        /// </summary>
        /// <remarks>
        /// Формула: R = d / λ / 1000
        /// где d - толщина в мм, λ - теплопроводность в Вт/м·К
        /// </remarks>
        public double ThermalResistance => Thickness / Lambda / 1000.0;

        /// <summary>
        /// Обновить λ в зависимости от УГВ
        /// </summary>
        /// <param name="groundwaterLevel">Уровень грунтовых вод, м</param>
        public void UpdateLambda(double groundwaterLevel)
        {
            if (IsLambdaOverridden)
            {
                // Если λ изменена вручную, не пересчитываем
                return;
            }

            // Слои над трубой всегда используют λА
            if (Position == LayerPosition.AbovePipe)
            {
                Lambda = Material.LambdaA;
            }
            else
            {
                // Слои под трубой: λБ при УГВ < 1м, λА при УГВ >= 1м
                Lambda = groundwaterLevel < 1.0 
                    ? Material.LambdaB 
                    : Material.LambdaA;
            }
        }

        /// <summary>
        /// Создать копию слоя
        /// </summary>
        public Layer Clone()
        {
            return new Layer
            {
                Id = Id,
                Material = Material,
                Thickness = Thickness,
                Lambda = Lambda,
                IsLambdaOverridden = IsLambdaOverridden,
                Position = Position,
                Order = Order
            };
        }

        /// <summary>
        /// Строковое представление слоя
        /// </summary>
        public override string ToString()
        {
            var position = Position == LayerPosition.AbovePipe ? "над трубой" : "под трубой";
            return $"{Material.Name} {Thickness} мм ({position}), R={ThermalResistance:F4} м²·К/Вт";
        }
    }
}
```

---

## 4. Тест-кейсы

### TC-1.2.1: Создание слоя

```csharp
[Fact]
public void Layer_Create_ShouldSetDefaultValues()
{
    // Arrange & Act
    var layer = new Layer();

    // Assert
    Assert.NotEqual(Guid.Empty, layer.Id);
    Assert.Equal(50.0, layer.Thickness);
    Assert.False(layer.IsLambdaOverridden);
}
```

### TC-1.2.2: Расчёт термического сопротивления

```csharp
[Fact]
public void Layer_ThermalResistance_ShouldCalculateCorrectly()
{
    // Arrange
    var layer = new Layer
    {
        Thickness = 100.0,  // мм
        Lambda = 1.5        // Вт/м·К
    };

    // Act
    var r = layer.ThermalResistance;

    // Assert
    // R = d / λ / 1000 = 100 / 1.5 / 1000 = 0.0667 м²·К/Вт
    Assert.Equal(0.0667, r, precision: 4);
}
```

### TC-1.2.3: Обновление λ для слоя над трубой

```csharp
[Fact]
public void Layer_UpdateLambda_AbovePipe_ShouldUseLambdaA()
{
    // Arrange
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };
    var layer = new Layer
    {
        Material = material,
        Position = LayerPosition.AbovePipe,
        IsLambdaOverridden = false
    };

    // Act
    layer.UpdateLambda(groundwaterLevel: 0.5); // УГВ < 1м

    // Assert
    Assert.Equal(0.4, layer.Lambda); // Всегда λА для слоёв над трубой
}
```

### TC-1.2.4: Обновление λ для слоя под трубой (влажные условия)

```csharp
[Fact]
public void Layer_UpdateLambda_BelowPipe_WetConditions_ShouldUseLambdaB()
{
    // Arrange
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };
    var layer = new Layer
    {
        Material = material,
        Position = LayerPosition.BelowPipe,
        IsLambdaOverridden = false
    };

    // Act
    layer.UpdateLambda(groundwaterLevel: 0.5); // УГВ < 1м

    // Assert
    Assert.Equal(2.0, layer.Lambda); // λБ при УГВ < 1м
}
```

### TC-1.2.5: Обновление λ для слоя под трубой (сухие условия)

```csharp
[Fact]
public void Layer_UpdateLambda_BelowPipe_DryConditions_ShouldUseLambdaA()
{
    // Arrange
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };
    var layer = new Layer
    {
        Material = material,
        Position = LayerPosition.BelowPipe,
        IsLambdaOverridden = false
    };

    // Act
    layer.UpdateLambda(groundwaterLevel: 2.0); // УГВ >= 1м

    // Assert
    Assert.Equal(0.4, layer.Lambda); // λА при УГВ >= 1м
}
```

### TC-1.2.6: Ручное переопределение λ

```csharp
[Fact]
public void Layer_UpdateLambda_Overridden_ShouldNotChange()
{
    // Arrange
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };
    var layer = new Layer
    {
        Material = material,
        Lambda = 1.0, // Ручное значение
        Position = LayerPosition.BelowPipe,
        IsLambdaOverridden = true
    };

    // Act
    layer.UpdateLambda(groundwaterLevel: 0.5);

    // Assert
    Assert.Equal(1.0, layer.Lambda); // Не изменилось
}
```

---

## 5. Критерии приёмки

- [ ] Файл `src/Models/Construction/LayerPosition.cs` создан
- [ ] Файл `src/Models/Construction/Layer.cs` создан
- [ ] Enum `LayerPosition` содержит AbovePipe и BelowPipe
- [ ] Класс `Layer` содержит все свойства из ТЗ
- [ ] Свойство `ThermalResistance` вычисляется по формуле R = d / λ / 1000
- [ ] Метод `UpdateLambda()` работает корректно
- [ ] XML-документация добавлена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- `ThermalResistance` — вычисляемое свойство (readonly)
- `IsLambdaOverridden` — флаг для защиты от автоматического пересчёта
- `Id` — Guid для уникальной идентификации слоя

---

**Конец документа**