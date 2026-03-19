# Task 6.2: Интеграция с ThermalModule

**Этап:** 6 - Интеграция  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 1.2 (HydraulicInputData), Task 4.1 (CircuitsViewModel)

---

## 1. Цель задачи

Реализовать интеграцию с ThermalModule для получения тепловых данных.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-07 | Интеграция с ThermalModule | Получение q_up, q_down, температур |

---

## 3. Изменяемые файлы

### 3.1. CircuitsViewModel.cs

**Изменения:**

```csharp
// Добавить в CircuitsViewModel.cs

using SnowMeltingCalculator.ViewModels.Thermal;

public partial class CircuitsViewModel : ObservableObject
{
    private readonly ThermalViewModel _thermalViewModel;

    public CircuitsViewModel(
        ICircuitsCalculator circuitsCalculator,
        IGlycolDataService glycolService,
        ThermalViewModel thermalViewModel)
    {
        _circuitsCalculator = circuitsCalculator;
        _glycolService = glycolService;
        _thermalViewModel = thermalViewModel;

        // Подписка на изменения теплового расчёта
        _thermalViewModel.PropertyChanged += OnThermalPropertyChanged;
    }

    private void OnThermalPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ThermalViewModel.Result))
        {
            UpdateFromThermalModule();
        }
    }

    private void UpdateFromThermalModule()
    {
        var thermalResult = _thermalViewModel.Result;
        if (thermalResult == null || !thermalResult.IsValid)
            return;

        // Обновление входных данных из теплового расчёта
        // Эти данные хранятся в HydraulicInputData, а не в CircuitRow
        _inputData.PowerUp = thermalResult.PowerUp;
        _inputData.PowerDown = thermalResult.PowerDown;
        _inputData.SupplyTemperature = thermalResult.SupplyTemperature;
        _inputData.ReturnTemperature = thermalResult.ReturnTemperature;
        _inputData.InnerDiameter = _inputData.Pipe.OuterDiameter - 2 * _inputData.Pipe.WallThickness;
        _inputData.PipeSpacing_mm = thermalResult.PipeSpacing;

        // Перерасчёт
        CalculateCommand.Execute(null);
    }
}
```

---

## 4. Получаемые данные

| Параметр | Источник | Описание |
|----------|----------|----------|
| PowerUp | ThermalResult | Мощность вверх (Вт/м²) |
| PowerDown | ThermalResult | Мощность вниз (Вт/м²) |
| SupplyTemperature | ThermalResult | Температура подачи (°C) |
| ReturnTemperature | ThermalResult | Температура обратки (°C) |
| InnerDiameter | ThermalResult | Внутренний диаметр трубы (мм) |
| PipeSpacing | ThermalResult | Шаг укладки (мм) |

---

## 5. Критерии приёмки

- [ ] Подписка на события ThermalViewModel реализована
- [ ] Автоматическое обновление при изменении теплового расчёта
- [ ] Перерасчёт контуров работает
- [ ] Unit-тесты проходят

---

## 6. Примечания

- Используется PropertyChanged для подписки на изменения
- Перерасчёт выполняется автоматически при изменении теплового расчёта

---

## 7. Связанные задачи

- Task 1.2: HydraulicInputData — содержит данные из ThermalModule
- Task 4.1: CircuitsViewModel — получает данные из ThermalViewModel

---

*Дата создания: 2026-03-17*