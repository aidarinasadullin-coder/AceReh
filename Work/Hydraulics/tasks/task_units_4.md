# Задача: Исправить CircuitTemperatureResult.Density

## Метаданные

| Параметр | Значение |
|----------|----------|
| **ID** | task_units_4 |
| **Модуль** | Hydraulics |
| **Приоритет** | P1 (Критичный) |
| **Статус** | Ожидает |
| **Зависимости** | — |
| **Юзер-кейс** | UC-4 |

---

## 1. Цель задачи

Исправить присвоение свойства `Density` в объекте `CircuitTemperatureResult` — конвертировать плотность из **кг/м³** в **г/см³**.

---

## 2. Связь с юзер-кейсами

### UC-4: Сохранение плотности в результатах

**Проблема**: В модель `CircuitTemperatureResult` передаётся плотность в кг/м³, но ожидается г/см³.

**Решение**: Конвертировать при присвоении: `Density = glycolProps.Density / 1000.0`

---

## 3. Описание изменений

### 3.1. Файл: `src/Services/Hydraulics/CircuitsCalculator.cs`

#### 3.1.1. Метод: `CalculateAtTemperature` (присвоение Density)

**Строка**: 179

**Текущий код**:
```csharp
var result = new CircuitTemperatureResult
{
    Temperature = temperature,
    Density = glycolProps.Density,
    KinematicViscosity = glycolProps.KinematicViscosity
};
```

**Требуемый код**:
```csharp
var result = new CircuitTemperatureResult
{
    Temperature = temperature,
    Density = glycolProps.Density / 1000.0,  // Конвертация: кг/м³ → г/см³
    KinematicViscosity = glycolProps.KinematicViscosity
};
```

### 3.2. Файл: `src/Models/Hydraulics/CircuitRow.cs`

#### 3.2.1. Модель: `CircuitTemperatureResult`

**Строки**: 17-19

**Текущий код**:
```csharp
/// <summary>
/// Плотность теплоносителя, г/см³
/// </summary>
public double Density { get; set; }
```

**Требуемый код** (с уточнением комментария):
```csharp
/// <summary>
/// Плотность теплоносителя, г/см³
/// </summary>
/// <remarks>
/// Внимание: GlycolProperties.Density хранит плотность в кг/м³.
/// При присвоении требуется конвертация: Density = glycolProps.Density / 1000.0
/// 
/// Пример: 1053 кг/м³ = 1.053 г/см³
/// </remarks>
public double Density { get; set; }
```

---

## 4. Тест-кейсы

### 4.1. Тест: Корректная конвертация плотности

**Входные данные**:
- glycolProps.Density = 1053 кг/м³

**Ожидаемый результат**:
- CircuitTemperatureResult.Density = 1.053 г/см³

### 4.2. Тест: Различные значения плотности

| glycolProps.Density (кг/м³) | Ожидаемый result.Density (г/см³) |
|------------------------------|-----------------------------------|
| 1000 (вода) | 1.000 |
| 1053 (50% гликоль) | 1.053 |
| 1100 (40% гликоль) | 1.100 |

---

## 5. Критерии приёмки

- [ ] Свойство `CircuitTemperatureResult.Density` содержит значение в **г/см³**
- [ ] При `glycolProps.Density = 1053 кг/м³`
  - `result.Density = 1.053 г/см³`
- [ ] XML-комментарий обновлён с предупреждением о конвертации
- [ ] Unit-тесты проходят

---

## 6. Ссылки

- **ТЗ**: `Work/Hydraulics/technical_specification_units.md` (раздел 2.5, 4.4, 4.5)
- **Код**: `src/Services/Hydraulics/CircuitsCalculator.cs` (строка 179)
- **Код**: `src/Models/Hydraulics/CircuitRow.cs` (строки 17-19)

---

*Дата создания: 2026-03-20*