# Task 1.4: Создать ConstructionTemplate.cs

**Этап:** 1. Модели данных  
**Приоритет:** P3 (Низкая)  
**Время:** 0.5 часа  
**Зависимости:** Task 1.1, Task 1.2

---

## 1. Цель задачи

Создать модель данных `ConstructionTemplate` для хранения предустановленных шаблонов конструкций.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| — | Предустановленные шаблоны | Модель шаблона |

---

## 3. Описание изменений

### 3.1. Создать файл LayerTemplate.cs

**Путь:** `src/Models/Construction/LayerTemplate.cs`

**Код:**

```csharp
namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Шаблон слоя для предустановленных конструкций
    /// </summary>
    /// <remarks>
    /// Используется для создания Layer из шаблона
    /// </remarks>
    public class LayerTemplate
    {
        /// <summary>
        /// Идентификатор материала
        /// </summary>
        public int MaterialId { get; set; }

        /// <summary>
        /// Толщина слоя, мм
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        /// Позиция слоя
        /// </summary>
        public LayerPosition Position { get; set; }

        /// <summary>
        /// Порядковый номер
        /// </summary>
        public int Order { get; set; }
    }
}
```

### 3.2. Создать файл ConstructionTemplate.cs

**Путь:** `src/Models/Construction/ConstructionTemplate.cs`

**Код:**

```csharp
using System.Collections.Generic;

namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Предустановленный шаблон конструкции
    /// </summary>
    /// <remarks>
    /// Содержит типовые конструкции для парковок, дорожек и т.д.
    /// </remarks>
    public class ConstructionTemplate
    {
        /// <summary>
        /// Идентификатор шаблона
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Название шаблона
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Описание шаблона
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Слои над трубой (шаблоны)
        /// </summary>
        public List<LayerTemplate> LayersAbovePipe { get; set; } = new();

        /// <summary>
        /// Слои под трубой (шаблоны)
        /// </summary>
        public List<LayerTemplate> LayersBelowPipe { get; set; } = new();

        /// <summary>
        /// Признак наличия нагрузок на покрытие
        /// </summary>
        public bool HasLoads { get; set; }

        /// <summary>
        /// Уровень грунтовых вод по умолчанию, м
        /// </summary>
        public double DefaultGroundwaterLevel { get; set; } = 2.0;

        /// <summary>
        /// Предустановленные шаблоны
        /// </summary>
        public static readonly List<ConstructionTemplate> StandardTemplates = new()
        {
            new ConstructionTemplate
            {
                Id = 1,
                Name = "Типовая парковка",
                Description = "Стандартная конструкция для парковок с асфальтовым покрытием",
                HasLoads = true,
                DefaultGroundwaterLevel = 2.0,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 11, Thickness = 50, Position = LayerPosition.AbovePipe, Order = 0 }, // Асфальтобетон
                    new LayerTemplate { MaterialId = 5, Thickness = 100, Position = LayerPosition.AbovePipe, Order = 1 }  // Бетон плотный
                },
                LayersBelowPipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 1, Thickness = 150, Position = LayerPosition.BelowPipe, Order = 0 }, // Песок
                    new LayerTemplate { MaterialId = 2, Thickness = 0, Position = LayerPosition.BelowPipe, Order = 1 }    // Грунт (бесконечный)
                }
            },
            new ConstructionTemplate
            {
                Id = 2,
                Name = "Пешеходная дорожка",
                Description = "Облегчённая конструкция для пешеходных дорожек",
                HasLoads = false,
                DefaultGroundwaterLevel = 2.0,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 11, Thickness = 40, Position = LayerPosition.AbovePipe, Order = 0 }, // Асфальтобетон
                    new LayerTemplate { MaterialId = 10, Thickness = 50, Position = LayerPosition.AbovePipe, Order = 1 }  // Цементно-песчаная стяжка
                },
                LayersBelowPipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 1, Thickness = 100, Position = LayerPosition.BelowPipe, Order = 0 }, // Песок
                    new LayerTemplate { MaterialId = 2, Thickness = 0, Position = LayerPosition.BelowPipe, Order = 1 }   // Грунт
                }
            },
            new ConstructionTemplate
            {
                Id = 3,
                Name = "Въезд в гараж",
                Description = "Усиленная конструкция для въездов в гараж с арматурой",
                HasLoads = true,
                DefaultGroundwaterLevel = 1.5,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 11, Thickness = 50, Position = LayerPosition.AbovePipe, Order = 0 }, // Асфальтобетон
                    new LayerTemplate { MaterialId = 6, Thickness = 150, Position = LayerPosition.AbovePipe, Order = 1 } // Железобетон
                },
                LayersBelowPipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 1, Thickness = 200, Position = LayerPosition.BelowPipe, Order = 0 }, // Песок
                    new LayerTemplate { MaterialId = 2, Thickness = 0, Position = LayerPosition.BelowPipe, Order = 1 }    // Грунт
                }
            }
        };

        /// <summary>
        /// Строковое представление шаблона
        /// </summary>
        public override string ToString()
        {
            return $"{Name}: {LayersAbovePipe.Count} слоёв над трубой, {LayersBelowPipe.Count} слоёв под трубой";
        }
    }
}
```

---

## 4. Тест-кейсы

### TC-1.4.1: Создание шаблона

```csharp
[Fact]
public void ConstructionTemplate_Create_ShouldSetProperties()
{
    // Arrange & Act
    var template = new ConstructionTemplate
    {
        Id = 1,
        Name = "Тестовый шаблон",
        Description = "Описание",
        HasLoads = true
    };

    // Assert
    Assert.Equal(1, template.Id);
    Assert.Equal("Тестовый шаблон", template.Name);
    Assert.True(template.HasLoads);
}
```

### TC-1.4.2: Стандартные шаблоны

```csharp
[Fact]
public void ConstructionTemplate_StandardTemplates_ShouldContainThree()
{
    // Assert
    Assert.Equal(3, ConstructionTemplate.StandardTemplates.Count);
    Assert.Contains(ConstructionTemplate.StandardTemplates, t => t.Name == "Типовая парковка");
    Assert.Contains(ConstructionTemplate.StandardTemplates, t => t.Name == "Пешеходная дорожка");
    Assert.Contains(ConstructionTemplate.StandardTemplates, t => t.Name == "Въезд в гараж");
}
```

### TC-1.4.3: Шаблон "Типовая парковка"

```csharp
[Fact]
public void ConstructionTemplate_ParkingTemplate_ShouldHaveCorrectLayers()
{
    // Arrange
    var template = ConstructionTemplate.StandardTemplates[0];

    // Assert
    Assert.Equal("Типовая парковка", template.Name);
    Assert.Equal(2, template.LayersAbovePipe.Count);
    Assert.Equal(2, template.LayersBelowPipe.Count);
    Assert.True(template.HasLoads);
}
```

---

## 5. Критерии приёмки

- [ ] Файл `src/Models/Construction/LayerTemplate.cs` создан
- [ ] Файл `src/Models/Construction/ConstructionTemplate.cs` создан
- [ ] Класс `ConstructionTemplate` содержит 3 стандартных шаблона
- [ ] XML-документация добавлена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- `LayerTemplate` — упрощённая модель для хранения в шаблоне
- `MaterialId` — ссылка на материал по ID (загружается из репозитория)
- Толщина = 0 означает бесконечный слой (грунт)

---

**Конец документа**