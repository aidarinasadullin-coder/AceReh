# Task 1.5: Создать Construction.cs (реализация IConstructionData)

**Этап:** 1. Модели данных  
**Приоритет:** P0 (Критическая)  
**Время:** 2 часа  
**Зависимости:** Task 1.1, Task 1.2, Task 1.3

---

## 1. Цель задачи

Создать модель данных `Construction` — основную модель конструкции ("Пирога"), которая реализует интерфейс `IConstructionData` и заменяет заглушку `ConstructionData`.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-01 | Добавление слоя материала | AddLayerAbovePipe, AddLayerBelowPipe |
| UC-04 | Удаление слоя | RemoveLayer |
| UC-05 | Учёт уровня грунтовых вод | UpdateLambdaForGroundwater |
| UC-06 | Валидация минимальной стяжки | ValidateConstruction |
| UC-09 | Интеграция с ThermalViewModel | R1Total, R2Total, LambdaE, DataChanged |

---

## 3. Описание изменений

### 3.1. Удалить заглушку ConstructionData

**Файл:** `src/Models/Thermal/IConstructionData.cs`

**Изменения:**
- Удалить класс `ConstructionData` (заглушку)
- Оставить интерфейс `IConstructionData` и `ConstructionDataChangedEventArgs`

### 3.2. Создать файл Construction.cs

**Путь:** `src/Models/Construction/Construction.cs`

**Код:**

```csharp
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Models.Construction
{
    /// <summary>
    /// Конструкция ("Пирог") системы снеготаяния
    /// </summary>
    /// <remarks>
    /// Реализует интерфейс IConstructionData для интеграции с ThermalViewModel
    /// </remarks>
    public class Construction : IConstructionData
    {
        #region Поля

        private double _groundwaterLevel = 2.0;
        private bool _hasLoads = false;

        #endregion

        #region Конструктор

        /// <summary>
        /// Создать пустую конструкцию
        /// </summary>
        public Construction()
        {
            // Подписка на изменения коллекций
            LayersAbovePipe.CollectionChanged += OnLayersChanged;
            LayersBelowPipe.CollectionChanged += OnLayersChanged;
        }

        #endregion

        #region Коллекции слоёв

        /// <summary>
        /// Слои над трубой (к поверхности)
        /// </summary>
        public ObservableCollection<Layer> LayersAbovePipe { get; } = new();

        /// <summary>
        /// Слои под трубой (к грунту)
        /// </summary>
        public ObservableCollection<Layer> LayersBelowPipe { get; } = new();

        #endregion

        #region Параметры конструкции

        /// <summary>
        /// Уровень грунтовых вод, м
        /// </summary>
        /// <remarks>
        /// Влияет на выбор λА или λБ для слоёв под трубой
        /// </remarks>
        public double GroundwaterLevel
        {
            get => _groundwaterLevel;
            set
            {
                if (_groundwaterLevel != value)
                {
                    _groundwaterLevel = value;
                    UpdateLambdaForGroundwater();
                }
            }
        }

        /// <summary>
        /// Признак наличия нагрузок на покрытие
        /// </summary>
        /// <remarks>
        /// Влияет на минимальную толщину стяжки (40 мм без нагрузок, 50 мм с нагрузками)
        /// </remarks>
        public bool HasLoads
        {
            get => _hasLoads;
            set
            {
                if (_hasLoads != value)
                {
                    _hasLoads = value;
                    OnDataChanged();
                }
            }
        }

        /// <summary>
        /// Материал вокруг трубы (для LambdaE)
        /// </summary>
        /// <remarks>
        /// Определяется автоматически как материал первого слоя над трубой
        /// </remarks>
        public Material? MaterialAroundPipe => LayersAbovePipe.FirstOrDefault()?.Material;

        #endregion

        #region IConstructionData

        /// <summary>
        /// Суммарное термическое сопротивление слоёв над трубой, м²·К/Вт
        /// </summary>
        /// <remarks>
        /// Формула: R1Total = Σ(R_i) для всех слоёв над трубой
        /// </remarks>
        public double R1Total => LayersAbovePipe.Sum(l => l.ThermalResistance);

        /// <summary>
        /// Суммарное термическое сопротивление слоёв под трубой, м²·К/Вт
        /// </summary>
        /// <remarks>
        /// Формула: R2Total = Σ(R_i) для всех слоёв под трубой
        /// </remarks>
        public double R2Total => LayersBelowPipe.Sum(l => l.ThermalResistance);

        /// <summary>
        /// Теплопроводность стяжки (бетона) вокруг трубы, Вт/м·К
        /// </summary>
        /// <remarks>
        /// Определяется как λ материала первого слоя над трубой
        /// </remarks>
        public double LambdaE => MaterialAroundPipe?.LambdaA ?? 1.6;

        /// <summary>
        /// Признак валидности данных конструкции
        /// </summary>
        public bool IsValid => ValidateConstruction();

        /// <summary>
        /// Событие изменения данных
        /// </summary>
        public event EventHandler<ConstructionDataChangedEventArgs>? DataChanged;

        #endregion

        #region Методы управления слоями

        /// <summary>
        /// Добавить слой над трубой
        /// </summary>
        /// <param name="material">Материал слоя</param>
        /// <param name="thickness">Толщина слоя, мм</param>
        public void AddLayerAbovePipe(Material material, double thickness)
        {
            var layer = new Layer
            {
                Material = material,
                Thickness = thickness,
                Lambda = material.LambdaA, // Слои над трубой всегда используют λА
                Position = LayerPosition.AbovePipe,
                Order = LayersAbovePipe.Count
            };

            LayersAbovePipe.Add(layer);
            // Событие OnDataChanged вызовется автоматически через CollectionChanged
        }

        /// <summary>
        /// Добавить слой под трубой
        /// </summary>
        /// <param name="material">Материал слоя</param>
        /// <param name="thickness">Толщина слоя, мм</param>
        public void AddLayerBelowPipe(Material material, double thickness)
        {
            var lambda = GroundwaterLevel < 1.0 ? material.LambdaB : material.LambdaA;

            var layer = new Layer
            {
                Material = material,
                Thickness = thickness,
                Lambda = lambda,
                Position = LayerPosition.BelowPipe,
                Order = LayersBelowPipe.Count
            };

            LayersBelowPipe.Add(layer);
            // Событие OnDataChanged вызовется автоматически через CollectionChanged
        }

        /// <summary>
        /// Удалить слой
        /// </summary>
        /// <param name="layer">Слой для удаления</param>
        public void RemoveLayer(Layer layer)
        {
            if (layer.Position == LayerPosition.AbovePipe)
            {
                LayersAbovePipe.Remove(layer);
                // Перенумерация порядковых номеров
                for (int i = 0; i < LayersAbovePipe.Count; i++)
                {
                    LayersAbovePipe[i].Order = i;
                }
            }
            else
            {
                LayersBelowPipe.Remove(layer);
                // Перенумерация порядковых номеров
                for (int i = 0; i < LayersBelowPipe.Count; i++)
                {
                    LayersBelowPipe[i].Order = i;
                }
            }
            // Событие OnDataChanged вызовется автоматически через CollectionChanged
        }

        /// <summary>
        /// Очистить все слои
        /// </summary>
        public void Clear()
        {
            LayersAbovePipe.Clear();
            LayersBelowPipe.Clear();
        }

        #endregion

        #region Методы расчёта

        /// <summary>
        /// Обновить λ для всех слоёв под трубой при изменении УГВ
        /// </summary>
        public void UpdateLambdaForGroundwater()
        {
            foreach (var layer in LayersBelowPipe)
            {
                layer.UpdateLambda(GroundwaterLevel);
            }
            OnDataChanged();
        }

        /// <summary>
        /// Валидация конструкции
        /// </summary>
        /// <returns>true если конструкция валидна</returns>
        private bool ValidateConstruction()
        {
            // Проверка наличия слоёв
            if (LayersAbovePipe.Count == 0 && LayersBelowPipe.Count == 0)
            {
                return false;
            }

            // Проверка минимальной стяжки над трубой
            var minThickness = HasLoads ? 50.0 : 40.0;
            var totalAbove = LayersAbovePipe.Sum(l => l.Thickness);
            if (totalAbove < minThickness)
            {
                return false;
            }

            // Проверка толщины слоёв
            foreach (var layer in LayersAbovePipe.Concat(LayersBelowPipe))
            {
                if (layer.Thickness < 10 || layer.Thickness > 1000)
                {
                    return false;
                }
            }

            // Проверка УГВ
            if (GroundwaterLevel < 0 || GroundwaterLevel > 10)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region События

        /// <summary>
        /// Обработчик изменения коллекций слоёв
        /// </summary>
        private void OnLayersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnDataChanged();
        }

        /// <summary>
        /// Вызвать событие изменения данных
        /// </summary>
        public void RaiseDataChanged(string propertyName, object? oldValue, object? newValue, bool isValid = true)
        {
            DataChanged?.Invoke(this, new ConstructionDataChangedEventArgs
            {
                ChangedProperty = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                IsValid = isValid
            });
        }

        /// <summary>
        /// Внутренний метод вызова события
        /// </summary>
        private void OnDataChanged()
        {
            RaiseDataChanged("Construction", null, null, IsValid);
        }

        #endregion

        #region Статические методы

        /// <summary>
        /// Создать конструкцию из шаблона
        /// </summary>
        /// <param name="template">Шаблон конструкции</param>
        /// <param name="materials">Словарь материалов по ID</param>
        /// <returns>Конструкция</returns>
        public static Construction FromTemplate(ConstructionTemplate template, Dictionary<int, Material> materials)
        {
            var construction = new Construction
            {
                HasLoads = template.HasLoads,
                GroundwaterLevel = template.DefaultGroundwaterLevel
            };

            // Добавить слои над трубой
            foreach (var layerTemplate in template.LayersAbovePipe.OrderBy(t => t.Order))
            {
                if (materials.TryGetValue(layerTemplate.MaterialId, out var material))
                {
                    construction.AddLayerAbovePipe(material, layerTemplate.Thickness);
                }
            }

            // Добавить слои под трубой
            foreach (var layerTemplate in template.LayersBelowPipe.OrderBy(t => t.Order))
            {
                if (materials.TryGetValue(layerTemplate.MaterialId, out var material))
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

### TC-1.5.1: Создание пустой конструкции

```csharp
[Fact]
public void Construction_Create_ShouldBeEmpty()
{
    // Arrange & Act
    var construction = new Construction();

    // Assert
    Assert.Empty(construction.LayersAbovePipe);
    Assert.Empty(construction.LayersBelowPipe);
    Assert.Equal(2.0, construction.GroundwaterLevel);
    Assert.False(construction.HasLoads);
}
```

### TC-1.5.2: Добавление слоя над трубой

```csharp
[Fact]
public void Construction_AddLayerAbovePipe_ShouldAddLayer()
{
    // Arrange
    var construction = new Construction();
    var material = new Material { Id = 1, Name = "Бетон", LambdaA = 1.5, LambdaB = 1.5 };

    // Act
    construction.AddLayerAbovePipe(material, 100.0);

    // Assert
    Assert.Single(construction.LayersAbovePipe);
    Assert.Equal(100.0, construction.LayersAbovePipe[0].Thickness);
    Assert.Equal(1.5, construction.LayersAbovePipe[0].Lambda);
}
```

### TC-1.5.3: Добавление слоя под трубой (влажные условия)

```csharp
[Fact]
public void Construction_AddLayerBelowPipe_WetConditions_ShouldUseLambdaB()
{
    // Arrange
    var construction = new Construction { GroundwaterLevel = 0.5 }; // УГВ < 1м
    var material = new Material { Id = 1, Name = "Песок", LambdaA = 0.4, LambdaB = 2.0 };

    // Act
    construction.AddLayerBelowPipe(material, 150.0);

    // Assert
    Assert.Single(construction.LayersBelowPipe);
    Assert.Equal(2.0, construction.LayersBelowPipe[0].Lambda); // λБ
}
```

### TC-1.5.4: Расчёт R1Total

```csharp
[Fact]
public void Construction_R1Total_ShouldCalculateCorrectly()
{
    // Arrange
    var construction = new Construction();
    var material = new Material { LambdaA = 1.5, LambdaB = 1.5 };

    construction.AddLayerAbovePipe(material, 50.0);  // R = 50/1.5/1000 = 0.0333
    construction.AddLayerAbovePipe(material, 100.0); // R = 100/1.5/1000 = 0.0667

    // Act
    var r1Total = construction.R1Total;

    // Assert
    Assert.Equal(0.1, r1Total, precision: 3);
}
```

### TC-1.5.5: Расчёт R2Total

```csharp
[Fact]
public void Construction_R2Total_ShouldCalculateCorrectly()
{
    // Arrange
    var construction = new Construction { GroundwaterLevel = 2.0 };
    var material = new Material { LambdaA = 0.4, LambdaB = 2.0 };

    construction.AddLayerBelowPipe(material, 150.0); // R = 150/0.4/1000 = 0.375

    // Act
    var r2Total = construction.R2Total;

    // Assert
    Assert.Equal(0.375, r2Total, precision: 3);
}
```

### TC-1.5.6: Валидация — нет слоёв

```csharp
[Fact]
public void Construction_IsValid_NoLayers_ShouldBeFalse()
{
    // Arrange
    var construction = new Construction();

    // Assert
    Assert.False(construction.IsValid);
}
```

### TC-1.5.7: Валидация — минимальная стяжка

```csharp
[Fact]
public void Construction_IsValid_MinThickness_ShouldBeFalse()
{
    // Arrange
    var construction = new Construction { HasLoads = false };
    var material = new Material { LambdaA = 1.5, LambdaB = 1.5 };

    construction.AddLayerAbovePipe(material, 30.0); // < 40 мм

    // Assert
    Assert.False(construction.IsValid);
}
```

### TC-1.5.8: Событие DataChanged

```csharp
[Fact]
public void Construction_DataChanged_ShouldRaiseOnAddLayer()
{
    // Arrange
    var construction = new Construction();
    var material = new Material { LambdaA = 1.5, LambdaB = 1.5 };
    var eventRaised = false;

    construction.DataChanged += (sender, e) => eventRaised = true;

    // Act
    construction.AddLayerAbovePipe(material, 50.0);

    // Assert
    Assert.True(eventRaised);
}
```

---

## 5. Критерии приёмки

- [ ] Файл `src/Models/Construction/Construction.cs` создан
- [ ] Класс `Construction` реализует `IConstructionData`
- [ ] Заглушка `ConstructionData` удалена из `IConstructionData.cs`
- [ ] Свойства `R1Total`, `R2Total`, `LambdaE` вычисляются корректно
- [ ] Методы `AddLayerAbovePipe`, `AddLayerBelowPipe`, `RemoveLayer` работают
- [ ] Метод `UpdateLambdaForGroundwater` работает
- [ ] Событие `DataChanged` вызывается при изменениях
- [ ] XML-документация добавлена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- `ObservableCollection` автоматически уведомляет об изменениях
- `MaterialAroundPipe` определяется как материал первого слоя над трубой
- При изменении `GroundwaterLevel` автоматически обновляются λ для слоёв под трубой

---

**Конец документа**