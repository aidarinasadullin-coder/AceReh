# Task 6.3: Интеграция с ClimateModule

**Этап:** 6 - Интеграция  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 1.2 (HydraulicInputData), Task 4.1 (CircuitsViewModel)

---

## 1. Цель задачи

Реализовать интеграцию с ClimateModule для получения температуры холодной пятидневки.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-07 | Интеграция с ClimateModule | Получение t_cold |

---

## 3. Изменяемые файлы

### 3.1. CircuitsViewModel.cs

**Изменения:**

```csharp
// Добавить в CircuitsViewModel.cs

using SnowMeltingCalculator.ViewModels.Climate;

public partial class CircuitsViewModel : ObservableObject
{
    private readonly ClimateViewModel _climateViewModel;

    public CircuitsViewModel(
        ICircuitsCalculator circuitsCalculator,
        IGlycolDataService glycolService,
        ThermalViewModel thermalViewModel,
        ClimateViewModel climateViewModel)
    {
        _circuitsCalculator = circuitsCalculator;
        _glycolService = glycolService;
        _thermalViewModel = thermalViewModel;
        _climateViewModel = climateViewModel;

        // Подписка на изменения климатических данных
        _climateViewModel.PropertyChanged += OnClimatePropertyChanged;
    }

    private void OnClimatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ClimateViewModel.ColdFiveDayTemperature))
        {
            UpdateFromClimateModule();
        }
    }

    private void UpdateFromClimateModule()
    {
        // Температура холодной пятидневки
        double coldFiveDayTemperature = _climateViewModel.ColdFiveDayTemperature;

        // Обновление входных данных
        foreach (var collector in Collectors)
        {
            foreach (var circuit in collector.Circuits)
            {
                circuit.ColdFiveDayTemperature = coldFiveDayTemperature;
            }
        }
    }

    // Вычисляемое свойство для расчётной температуры
    public double DesignTemperature => _climateViewModel.ColdFiveDayTemperature;
}
```

---

## 4. Получаемые данные

| Параметр | Источник | Описание |
|----------|----------|----------|
| ColdFiveDayTemperature | ClimateViewModel | Температура холодной пятидневки (°C) |

---

## 5. Критерии приёмки

- [ ] Подписка на события ClimateViewModel реализована
- [ ] Автоматическое обновление при изменении климатических данных
- [ ] Перерасчёт контуров работает
- [ ] Unit-тесты проходят

---

## 6. Примечания

- Температура холодной пятидневки используется для расчёта при "холодном пуске"
- Расчётная температура = ColdFiveDayTemperature

---

## 7. Связанные задачи

- Task1.2: HydraulicInputData — содержит ColdFiveDayTemperature
- Task 4.1: CircuitsViewModel — получает данные из ClimateViewModel

---

*Дата создания: 2026-03-17*