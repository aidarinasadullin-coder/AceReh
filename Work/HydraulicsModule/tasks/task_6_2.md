# Task 6.2: Интеграция с ThermalModule

**Этап:** 6 - Integration  
**Приоритет:** Высокий  
**Статус:** Не начато  
**Зависимости:** Task 4.1, Task 6.1

---

## 1. Цель задачи

Реализовать интеграцию с ThermalModule через интерфейс `IThermalCalculationResult`.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-08 | Интеграция с ThermalModule | Подписка на ResultChanged |

---

## 3. Изменения

### 3.1. HydraulicsViewModel.cs

**Изменения:**
- Добавить зависимость от `IThermalCalculationResult`
- Подписаться на событие `ResultChanged`
- Автоматически обновлять параметры при изменении теплового расчёта

```csharp
public HydraulicsViewModel(IThermalCalculationResult thermalResult)
{
    _thermalResult = thermalResult;
    _thermalResult.ResultChanged += OnThermalResultChanged;
}

private void OnThermalResultChanged(object? sender, EventArgs e)
{
    if (_thermalResult.IsValid)
    {
        VolumeFlowRate = _thermalResult.VolumeFlowRate;
        SupplyTemperature = _thermalResult.SupplyTemperature;
        ReturnTemperature = _thermalResult.ReturnTemperature;
    }
}
```

---

## 4. Критерии приёмки

- [ ] Интеграция реализована
- [ ] Автоматическое обновление работает
- [ ] Unit-тесты проходят успешно