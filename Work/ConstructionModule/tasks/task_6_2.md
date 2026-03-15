# Task 6.2: Обновить ThermalViewModel для работы с Construction

**Этап:** 6. Интеграция  
**Приоритет:** P1 (Высокая)  
**Время:** 1 час  
**Зависимости:** Task 1.5, Task 6.1

---

## 1. Цель задачи

Обновить `ThermalViewModel` для работы с реальной реализацией `IConstructionData` (класс `Construction`).

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-09 | Интеграция с ThermalViewModel | Подписка на DataChanged |

---

## 3. Описание изменений

### 3.1. Обновить ThermalViewModel.cs

**Файл:** `src/ViewModels/Thermal/ThermalViewModel.cs`

**Изменения:**

```csharp
// ИЗМЕНИТЬ: Подписка на изменения данных конструкции
// Было:
if (_constructionData is ConstructionData constructionDataImpl)
{
    constructionDataImpl.DataChanged += OnConstructionDataChanged;
}

// Стало:
if (_constructionData is Construction constructionImpl)
{
    constructionImpl.DataChanged += OnConstructionDataChanged;
}
```

### 3.2. Добавить using

```csharp
using SnowMeltingCalculator.Models.Construction;
```

### 3.3. Полный код изменений

**Файл:** `src/ViewModels/Thermal/ThermalViewModel.cs`

**Изменить конструктор:**

```csharp
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;  // ДОБАВИТЬ
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Thermal;

namespace SnowMeltingCalculator.ViewModels.Thermal
{
    public partial class ThermalViewModel : ObservableObject
    {
        // ... существующий код ...

        public ThermalViewModel(
            IThermalCalculator calculator,
            IClimateData climateData,
            IConstructionData constructionData)
        {
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _climateData = climateData ?? throw new ArgumentNullException(nameof(climateData));
            _constructionData = constructionData ?? throw new ArgumentNullException(nameof(constructionData));

            // ... существующий код ...

            // Подписка на изменения климатических данных
            if (_climateData is ClimateData climateDataImpl)
            {
                climateDataImpl.DataChanged += OnClimateDataChanged;
            }

            // ИЗМЕНЕНИЕ: Подписка на изменения данных конструкции
            if (_constructionData is Construction constructionImpl)
            {
                constructionImpl.DataChanged += OnConstructionDataChanged;
            }
        }

        // ... остальной код без изменений ...
    }
}
```

---

## 4. Тест-кейсы

### TC-6.2.1: Подписка на событие DataChanged

```csharp
[Fact]
public void ThermalViewModel_Construction_ShouldSubscribeToDataChanged()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddApplicationServices();
    var serviceProvider = services.BuildServiceProvider();

    var construction = serviceProvider.GetRequiredService<Construction>();
    var thermalViewModel = serviceProvider.GetRequiredService<ThermalViewModel>();

    var eventRaised = false;
    thermalViewModel.PropertyChanged += (s, e) => eventRaised = true;

    // Act
    var material = new Material { LambdaA = 1.5, LambdaB = 1.5 };
    construction.AddLayerAbovePipe(material, 50.0);

    // Assert
    Assert.True(eventRaised);
}
```

### TC-6.2.2: Обновление R1Total при изменении конструкции

```csharp
[Fact]
public void ThermalViewModel_ConstructionChanged_ShouldUpdateR1Total()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddApplicationServices();
    var serviceProvider = services.BuildServiceProvider();

    var construction = serviceProvider.GetRequiredService<Construction>();
    var thermalViewModel = serviceProvider.GetRequiredService<ThermalViewModel>();

    // Act
    var material = new Material { LambdaA = 1.5, LambdaB = 1.5 };
    construction.AddLayerAbovePipe(material, 100.0);

    // Assert
    Assert.Equal(0.0667, thermalViewModel.R1Total, precision: 3);
}
```

### TC-6.2.3: Сброс результата при изменении конструкции

```csharp
[Fact]
public async Task ThermalViewModel_ConstructionChanged_ShouldResetResult()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddApplicationServices();
    var serviceProvider = services.BuildServiceProvider();

    var construction = serviceProvider.GetRequiredService<Construction>();
    var thermalViewModel = serviceProvider.GetRequiredService<ThermalViewModel>();

    // Выполняем расчёт
    await thermalViewModel.CalculateCommand.ExecuteAsync(null);
    Assert.NotNull(thermalViewModel.Result);

    // Act
    var material = new Material { LambdaA = 1.5, LambdaB = 1.5 };
    construction.AddLayerAbovePipe(material, 50.0);

    // Assert
    Assert.Null(thermalViewModel.Result);
    Assert.Contains("Требуется пересчёт", thermalViewModel.ValidationMessage);
}
```

---

## 5. Критерии приёмки

- [ ] `using SnowMeltingCalculator.Models.Construction` добавлен
- [ ] Подписка на `DataChanged` изменена на `Construction`
- [ ] При изменении конструкции `ThermalViewModel` получает уведомление
- [ ] При изменении конструкции результат расчёта сбрасывается
- [ ] Unit-тесты проходят

---

## 6. Примечания

- `Construction` реализует `IConstructionData`
- Событие `DataChanged` вызывается при любом изменении слоёв
- `ThermalViewModel` сбрасывает результат и отображает сообщение

---

**Конец документа**