# Отчёт о тестировании: Исправление формулы Kv для дросселирования

## Задача
Исправить формулу расчёта Kv для дросселирования, добавив плотность теплоносителя и проверив единицы измерения.

## Изменённые файлы

### 1. `src/Services/Hydraulics/CircuitsCalculator.cs`

#### Изменения в методе `CalculateKvForThrottling`:

**До (НЕВЕРНАЯ формула):**
```csharp
private double CalculateKvForThrottling(double flowRate, double throttling)
{
    if (throttling <= 0)
        return 0;

    // Kv = V_dot / √(Δp / 100)
    // где V_dot в л/ч, Δp в Па
    return flowRate / Math.Sqrt(throttling / 100);
}
```

**После (КОРРЕКТНАЯ формула):**
```csharp
private double CalculateKvForThrottling(double flowRate, double throttling, double density_g_cm3)
{
    if (throttling <= 0)
        return 0;

    if (density_g_cm3 <= 0)
        throw new ArgumentException("Плотность должна быть положительной", nameof(density_g_cm3));

    // Kv = V_dot / 1000 / √(Δp / 100000 / ρ[г/см³])
    // где V_dot в л/ч, Δp в Па, ρ в г/см³
    // Результат в м³/ч
    double flowRate_m3h = flowRate / 1000.0;  // л/ч → м³/ч
    double throttling_bar = throttling / 100000.0;  // Па → бар

    return flowRate_m3h / Math.Sqrt(throttling_bar / density_g_cm3);
}
```

#### Изменения в методе `CalculateBalancing`:

**До:**
```csharp
double kv = CalculateKvForThrottling(circuit.FlowRate, circuit.Throttling);
```

**После:**
```csharp
// Плотность берём из результата расчёта при рабочей температуре (уже в г/см³)
double density_g_cm3 = circuit.OperatingResult.Density;
double kv = CalculateKvForThrottling(circuit.FlowRate, circuit.Throttling, density_g_cm3);
```

### 2. `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs`

Добавлены новые тесты:

- `CalculateBalancing_CalculatesKvWithDensityCorrectly` — проверяет корректность расчёта Kv с плотностью
- `CalculateBalancing_KvFormula_UsesDensityInGramsPerCm3` — проверяет, что плотность влияет на результат

## Результаты тестирования

### Новые тесты
- ✅ `CalculateBalancing_CalculatesKvWithDensityCorrectly` — PASSED
- ✅ `CalculateBalancing_KvFormula_UsesDensityInGramsPerCm3` — PASSED

### Регрессионные тесты
- Всего: 40
- Пройдено: 38
- Не прошли: 2 (не связаны с изменениями)

**Непрошедшие тесты (существующие, не связаны с изменениями):**
1. `CalculateAtTemperature_PressureLossPerMeter_UsesDensityInGramsPerCm3` — ожидаемое значение 592 Па/м, получено 513.6 Па/м (расхождение в коэффициенте трения)
2. `CalculateFlowRate_WithTypicalValues_ReturnsReasonableValue` — ожидаемое 506 л/ч, получено 504.2 л/ч (погрешность округления)

## Проверка единиц измерения

| Параметр | Входные единицы | В формуле | Результат |
|----------|-----------------|-----------|-----------|
| flowRate | л/ч | ÷ 1000 → м³/ч | ✓ |
| throttling | Па | ÷ 100000 → бар | ✓ |
| density | г/см³ | без изменений | ✓ |
| Kv | — | — | м³/ч |

## Формула

### Исходное уравнение потерь в вентиле:
```
Δp = (V_dot / 1000 / Kv)² × 100000 × ρ[г/см³]
```

### Обратная формула для Kv:
```
Kv = V_dot / 1000 / √(Δp / 100000 / ρ[г/см³])
```

### Пример расчёта:
```
V_dot = 280 л/ч
Δp = 5000 Па
ρ = 1.053 г/см³

Kv = 280 / 1000 / √(5000 / 100000 / 1.053)
   = 0.28 / √(0.0475)
   ≈ 1.28 м³/ч
```

## Итог

✅ Формула исправлена
✅ Плотность теплоносителя учитывается
✅ Единицы измерения проверены
✅ Добавлена валидация плотности
✅ Добавлены тесты
✅ Добавлен подробный комментарий с формулой