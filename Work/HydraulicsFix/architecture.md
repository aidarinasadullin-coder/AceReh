# Архитектура исправлений модуля гидравлики

**Дата:** 2026-03-22  
**Статус:** Проект  
**Модуль:** Hydraulics  
**ТЗ:** D:\IA\ace\Work\HydraulicsFix\technical_specification.md  
**План:** D:\IA\ace\план-исправлений\PLAN_HYDRAULICS_FIX.md

---

## 1. Описание задачи

### 1.1. Краткое описание

Исправление критических ошибок в расчётах гидравлики, выявленных при сравнении с эталонным Excel-файлом (gidravlica.xls):

| Параметр | Excel (правильно) | Программа (ошибочно) |
|----------|-------------------|----------------------|
| Макс. обороты HKV-D | 2.5 | 8.0 |
| DpVerteiler | Есть колонка | Нет |
| Формулы DpVent | Меняются местами для HKV/IV | Одинаковые |
| Референсный контур | Макс. обороты | 0 оборотов |
| Единицы давления | Па (целые) | мбар (десятичные) |

### 1.2. Цель

Привести расчёты гидравлики в полное соответствие с эталонным Excel-файлом РЕХАУ.

### 1.3. Связанные юзер-кейсы

- **UC-1:** Расчёт максимальных оборотов клапана
- **UC-2:** Расчёт потерь давления DpVerteiler и DpVent
- **UC-3:** Балансировка контуров
- **UC-4:** Отображение результатов в таблице

---

## 2. Функциональная архитектура

### 2.1. Функциональные компоненты

#### Компонент 1: Калькулятор максимальных оборотов

**Название:** `ValveTurnsCalculator.GetMaxTurns()`

**Назначение:** Определение максимальных оборотов балансировочного клапана по типу

**Функции:**

| Метод | Входные данные | Выходные данные | Связанные UC |
|-------|----------------|-----------------|--------------|
| `GetMaxTurns(ValveType)` | ValveType.HKV_D, IV_1_25, IV_1_5 | double (2.5 или 8.0) | UC-1 |

**Логика:**
```
HKV-D → 2.5 оборота (максимум для бытового коллектора)
IV 1¼" → 8.0 оборотов
IV 1½" → 8.0 оборотов
```

**Зависимости:**
- От: `ValveType` (enum)
- К нему: `CircuitsCalculator.CalculateBalancing()`

---

#### Компонент 2: Модель результатов расчёта

**Название:** `CircuitTemperatureResult`

**Назначение:** Хранение результатов гидравлического расчёта при температуре

**Новые свойства:**

| Свойство | Тип | Описание | Формула |
|----------|-----|----------|---------|
| `DpRohr` | double | Потери в трубе, Па | `(L_hk + L_zul) × R` |
| `DpVerteiler` | double | Потери в распределителе, Па | См. формулы ниже |
| `DpVent` | double | Потери в вентиле, Па | См. формулы ниже |
| `DpGesamt` | double | Суммарные потери, Па | `DpRohr + DpVerteiler + DpVent` |
| `ZuDrosseln` | double | Дросселирование, Па | `DpGesamt_max - DpGesamt` |

**Устаревшие свойства (пометить `[Obsolete]`):**

| Свойство | Замена |
|----------|--------|
| `CircuitPipeLoss` | `DpRohr` |
| `SupplyPipeLoss` | Включено в `DpRohr` |
| `ValveLoss` | `DpVent` |
| `TotalLoss` | `DpGesamt` |

---

#### Компонент 3: Калькулятор контуров

**Название:** `CircuitsCalculator`

**Назначение:** Расчёт гидравлических параметров контуров

**Изменённые методы:**

##### 3.1. `CalculateAtTemperature()`

**Добавить параметр:** `ValveType valveType`

**Новая логика расчёта DpVerteiler и DpVent:**

```csharp
// Плотность в г/см³
double density_g_cm3 = glycolProps.Density / 1000.0;

// Скорость
double velocity = circuit.FlowRate * 4000 / (3600 * Math.PI * Math.Pow(innerDiameter, 2));

// DpRohr = потери в трубе контура + подводки
double dpRohr = (circuit.CircuitLength + circuit.SupplyLength) * pressureLossPerMeter;
result.DpRohr = dpRohr;

// DpVerteiler и DpVent — формулы меняются местами для HKV-D и IV
if (valveType == ValveType.HKV_D)
{
    // HKV-D: формулы меняются местами
    // DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
    result.DpVerteiler = Math.Pow(circuit.FlowRate / 1000.0 / 1.2, 2) * 100000 * density_g_cm3;
    
    // DpVent = 15000 × (ρ/2000) × v²
    result.DpVent = 15000 * (density_g_cm3 / 2) * Math.Pow(velocity, 2);
}
else
{
    // IV: стандартные формулы
    // DpVerteiler = 15000 × (ρ/2000) × v²
    result.DpVerteiler = 15000 * (density_g_cm3 / 2) * Math.Pow(velocity, 2);
    
    // DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
    result.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;
}

// DpGesamt = DpRohr + DpVerteiler + DpVent (вычисляется автоматически)
```

##### 3.2. `CalculateBalancing()`

**Изменённый алгоритм:**

```csharp
// Найти контур с МАКСИМАЛЬНЫМ DpGesamt (референсный)
double maxDpGesamt = activeCircuits.Max(c => c.OperatingResult.DpGesamt);

// Максимальные обороты для типа клапана
double maxTurns = ValveTurnsCalculator.GetMaxTurns(valveType);

foreach (var circuit in activeCircuits)
{
    double dpGesamt = circuit.OperatingResult.DpGesamt;
    
    // zu_drosseln = DpGesamt_max - DpGesamt_контур
    circuit.Throttling = maxDpGesamt - dpGesamt;
    
    // Референсный контур — контур с максимальным DpGesamt
    circuit.IsReferenceCircuit = Math.Abs(dpGesamt - maxDpGesamt) < 0.01;
    
    if (circuit.IsReferenceCircuit)
    {
        // Референсный контур получает МАКСИМАЛЬНЫЕ обороты
        circuit.ValveTurns = maxTurns;
        circuit.ValveTurnsWarning = null;
    }
    else
    {
        // Расчёт Kv для дросселирования
        double density_g_cm3 = circuit.OperatingResult.Density;
        double kv = CalculateKvForThrottling(circuit.FlowRate, circuit.Throttling, density_g_cm3);
        var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, valveType);
        circuit.ValveTurns = turns;
        circuit.ValveTurnsWarning = warning;
    }
}
```

---

## 3. Системная архитектура

### 3.1. Архитектурный стиль

**Многоуровневая архитектура (Layered)** с MVVM

### 3.2. Диаграмма классов

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Models Layer                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────┐    ┌─────────────────────────────────────────────┐  │
│  │    ValveType        │    │        CircuitTemperatureResult             │  │
│  │    (enum)           │    │        (class)                              │  │
│  ├─────────────────────┤    ├─────────────────────────────────────────────┤  │
│  │ + HKV_D = 0         │    │ + Temperature: double                       │  │
│  │ + IV_1_25 = 1       │    │ + Density: double (г/см³)                  │  │
│  │ + IV_1_5 = 2        │    │ + KinematicViscosity: double               │  │
│  └─────────────────────┘    │ + ReynoldsNumber: double                   │  │
│                              │ + FlowRegime: FlowRegime                    │  │
│  ┌─────────────────────┐    │ + FrictionFactor: double                   │  │
│  │    CircuitRow       │    │ + PressureLossPerMeter: double             │  │
│  │    (class)          │    │                                             │  │
│  ├─────────────────────┤    │ [НОВЫЕ СВОЙСТВА]                            │  │
│  │ + CircuitNumber     │    │ + DpRohr: double (Па)                      │  │
│  │ + CircuitLength     │    │ + DpVerteiler: double (Па)                  │  │
│  │ + SupplyLength      │    │ + DpVent: double (Па)                       │  │
│  │ + FlowRate          │    │ + DpGesamt: double (Па) ← computed          │  │
│  │ + Velocity          │    │ + ZuDrosseln: double (Па)                   │  │
│  │ + OperatingResult   │────│                                             │  │
│  │ + DesignResult      │    │ [УСТАРЕВШИЕ]                                │  │
│  │ + Throttling        │    │ - CircuitPipeLoss [Obsolete]                │  │
│  │ + ValveTurns        │    │ - SupplyPipeLoss [Obsolete]                 │  │
│  │ + IsReferenceCircuit│    │ - ValveLoss [Obsolete]                      │  │
│  └─────────────────────┘    │ - TotalLoss [Obsolete]                      │  │
│                              └─────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                             Services Layer                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                    ValveTurnsCalculator (static)                        │ │
│  ├─────────────────────────────────────────────────────────────────────────┤ │
│  │ [СУЩЕСТВУЮЩИЕ]                                                          │ │
│  │ + CalculateTurns(kv, valveType): double                                │ │
│  │ + CalculateTurnsWithWarning(kv, valveType): (double, string?)          │ │
│  │ + GetDefaultKv(valveType): double                                      │ │
│  │ + CalculateKvFromTurns(turns, valveType): double                        │ │
│  │                                                                         │ │
│  │ [НОВЫЕ]                                                                 │ │
│  │ + GetMaxTurns(valveType): double  ← ДОБАВИТЬ                            │ │
│  │   - HKV_D → 2.5                                                         │ │
│  │   - IV_1_25 → 8.0                                                       │ │
│  │   - IV_1_5 → 8.0                                                        │ │
│  │                                                                         │ │
│  │ [ИЗМЕНИТЬ]                                                              │ │
│  │ - MaxTurns: const double = 8.0  ← УДАЛИТЬ или изменить логику           │ │
│  │ + CalculateTurnsWithWarning(): использовать GetMaxTurns()              │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                    CircuitsCalculator                                   │ │
│  ├─────────────────────────────────────────────────────────────────────────┤ │
│  │ [СУЩЕСТВУЮЩИЕ]                                                          │ │
│  │ + CalculateCircuitPower(...): double                                    │ │
│  │ + CalculateFlowRate(...): double                                        │ │
│  │ + CalculateAllCircuits(...): List<CircuitRow>                           │ │
│  │ + CalculateBalancing(...): List<CircuitRow>                             │ │
│  │ + CalculateCollectorSummary(...): CollectorSummary                      │ │
│  │                                                                         │ │
│  │ [ИЗМЕНИТЬ]                                                              │ │
│  │ + CalculateAtTemperature(circuit, temperature, glycolProps,             │ │
│  │                          innerDiameter, kv, valveType) ← ДОБАВИТЬ param │ │
│  │   - Добавить расчёт DpRohr, DpVerteiler, DpVent                         │ │
│  │   - Разные формулы для HKV-D и IV                                       │ │
│  │                                                                         │ │
│  │ + CalculateBalancing(circuits, valveType)                               │ │
│  │   - Референсный контур = MAX(DpGesamt)                                  │ │
│  │   - Референсный контур получает максимальные обороты                    │ │
│  │   - zu_drosseln = DpGesamt_max - DpGesamt_контур                        │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                              Views Layer                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                    CircuitsView.xaml                                    │ │
│  ├─────────────────────────────────────────────────────────────────────────┤ │
│  │ [ИЗМЕНИТЬ КОЛОНКИ]                                                      │ │
│  │                                                                         │ │
│  │ Было:                              Должно быть:                         │ │
│  │ - Δp контур (мбар)                 - DpRohr (Па)                        │ │
│  │ - Δp клапан (мбар)                 - DpVerteiler (Па)                    │ │
│  │ - Δp сумма (мбар)                  - DpVent (Па)                         │ │
│  │                                    - DpGesamt (Па)                       │ │
│  │                                    - zu_drosseln (Па)                    │ │
│  │                                    - Обороты (дроби)                     │ │
│  │                                                                         │ │
│  │ Формат отображения: StringFormat=F0 (целые числа)                      │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.3. Зависимости между компонентами

```
┌──────────────────────────────────────────────────────────────────────────┐
│                           Зависимости                                     │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  CircuitsView.xaml                                                        │
│       │                                                                   │
│       │ Binding                                                           │
│       ▼                                                                   │
│  CircuitRow.OperatingResult.DpRohr                                        │
│  CircuitRow.OperatingResult.DpVerteiler                                   │
│  CircuitRow.OperatingResult.DpVent                                        │
│  CircuitRow.OperatingResult.DpGesamt                                     │
│  CircuitRow.Throttling                                                    │
│       │                                                                   │
│       │ Заполняется                                                       │
│       ▼                                                                   │
│  CircuitsCalculator.CalculateAtTemperature()                              │
│       │                                                                   │
│       │ Использует                                                        │
│       ▼                                                                   │
│  ValveTurnsCalculator.GetMaxTurns(ValveType)                             │
│       │                                                                   │
│       │ Использует                                                        │
│       ▼                                                                   │
│  ValveType (enum)                                                         │
│                                                                           │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Модель данных

### 4.1. Изменения в CircuitTemperatureResult

**Файл:** `src/Models/Hydraulics/CircuitRow.cs`

**Добавить свойства:**

```csharp
/// <summary>
/// Потери в трубе контура, Па (DpRohr)
/// </summary>
/// <remarks>
/// Формула: DpRohr = (L_hk + L_zul) × R
/// Где:
/// - L_hk — длина контура, м
/// - L_zul — длина подводки, м
/// - R — удельные потери, Па/м
/// </remarks>
public double DpRohr { get; set; }

/// <summary>
/// Потери в распределителе, Па (DpVerteiler)
/// </summary>
/// <remarks>
/// Для IV: DpVerteiler = 15000 × (ρ/2000) × v²
/// Для HKV-D: DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
/// </remarks>
public double DpVerteiler { get; set; }

/// <summary>
/// Потери в вентиле, Па (DpVent)
/// </summary>
/// <remarks>
/// Для IV: DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
/// Для HKV-D: DpVent = 15000 × (ρ/2000) × v²
/// </remarks>
public double DpVent { get; set; }

/// <summary>
/// Суммарные потери, Па (DpGesamt)
/// </summary>
public double DpGesamt => DpRohr + DpVerteiler + DpVent;

/// <summary>
/// Дросселирование для балансировки, Па (zu_drosseln)
/// </summary>
/// <remarks>
/// zu_drosseln = DpGesamt_max - DpGesamt_контур
/// </remarks>
public double ZuDrosseln { get; set; }
```

**Пометить как устаревшие:**

```csharp
[Obsolete("Использовать DpRohr вместо CircuitPipeLoss")]
public double CircuitPipeLoss { get; set; }

[Obsolete("Использовать DpVerteiler вместо ValveLoss для HKV-D")]
public double ValveLoss { get; set; }

[Obsolete("Использовать DpGesamt вместо TotalLoss")]
public double TotalLoss => DpRohr + DpVerteiler + DpVent;
```

### 4.2. Изменения в CircuitRow

**Файл:** `src/Models/Hydraulics/CircuitRow.cs`

**Добавить свойство:**

```csharp
/// <summary>
/// Дросселирование для балансировки, Па (zu_drosseln)
/// </summary>
/// <remarks>
/// Разница между максимальными DpGesamt в коллекторе и DpGesamt контура
/// Вычисляется только для рабочей температуры
/// </remarks>
[ObservableProperty]
private double _throttling;
```

---

## 5. Интерфейсы

### 5.1. ValveTurnsCalculator

**Файл:** `src/Services/Hydraulics/ValveTurnsCalculator.cs`

**Добавить метод:**

```csharp
/// <summary>
/// Получить максимальные обороты для типа клапана
/// </summary>
/// <param name="valveType">Тип клапана</param>
/// <returns>Максимальные обороты</returns>
/// <remarks>
/// HKV-D: 2.5 оборота (максимум для бытового коллектора)
/// IV 1¼": 8.0 оборотов
/// IV 1½": 8.0 оборотов
/// </remarks>
/// <exception cref="ArgumentException">Неподдерживаемый тип клапана</exception>
public static double GetMaxTurns(ValveType valveType)
{
    return valveType switch
    {
        ValveType.HKV_D => 2.5,
        ValveType.IV_1_25 => 8.0,
        ValveType.IV_1_5 => 8.0,
        _ => throw new ArgumentException($"Неподдерживаемый тип клапана: {valveType}", nameof(valveType))
    };
}
```

**Изменить метод:**

```csharp
public static (double Turns, string? Warning) CalculateTurnsWithWarning(double kv, ValveType valveType)
{
    double turns = valveType switch
    {
        ValveType.IV_1_5 => CalculateTurnsIV_1_5(kv),
        ValveType.IV_1_25 => CalculateTurnsIV_1_25(kv),
        ValveType.HKV_D => CalculateTurnsHKV_D(kv),
        _ => throw new ArgumentException($"Неподдерживаемый тип клапана: {valveType}", nameof(valveType))
    };

    string? warning = null;

    // ИЗМЕНЕНИЕ: Использовать GetMaxTurns вместо константы MaxTurns
    double maxTurns = GetMaxTurns(valveType);
    
    if (turns > maxTurns)
    {
        warning = $"Расчётные обороты ({turns:F2}) превышают максимум ({maxTurns}). Установлено {maxTurns} оборотов.";
        turns = maxTurns;
    }

    turns = Math.Round(turns * 4) / 4;

    return (turns, warning);
}
```

### 5.2. ICircuitsCalculator

**Файл:** `src/Services/Hydraulics/ICircuitsCalculator.cs`

**Изменить сигнатуру метода:**

```csharp
/// <summary>
/// Рассчитать гидравлику контура при заданной температуре
/// </summary>
/// <param name="circuit">Контур для расчёта</param>
/// <param name="temperature">Температура теплоносителя, °C</param>
/// <param name="glycolProps">Свойства гликоля при температуре</param>
/// <param name="innerDiameter">Внутренний диаметр трубы, мм</param>
/// <param name="kv">Коэффициент пропускной способности вентиля, м³/ч</param>
/// <param name="valveType">Тип клапана (для выбора формул DpVerteiler/DpVent)</param>
/// <returns>Результат расчёта при температуре</returns>
CircuitTemperatureResult CalculateAtTemperature(
    CircuitRow circuit,
    double temperature,
    GlycolProperties glycolProps,
    double innerDiameter,
    double kv,
    ValveType valveType);  // ← ДОБАВИТЬ ПАРАМЕТР
```

---

## 6. Формулы

### 6.1. Формулы DpVerteiler и DpVent

**Источник:** `docs/Formulas_Snegotayanie.md`, Excel gidravlica.xls

#### Для IV 1¼" и IV 1½":

```
DpVerteiler = 15000 × (ρ/2000) × v²

где:
- 15000 = 1000 × 15 (коэффициент)
- ρ = плотность в кг/м³ (делить на 1000 для г/см³)
- v = скорость в м/с

DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000

где:
- V_dot = расход в л/ч
- Kv = 1.45 (IV 1¼") или 1.5 (IV 1½")
- ρ = плотность в кг/м³ (делить на 1000 для г/см³)
```

#### Для HKV-D:

```
DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000

где:
- V_dot = расход в л/ч
- 1.2 = Kv для HKV-D
- ρ = плотность в кг/м³ (делить на 1000 для г/см³)

DpVent = 15000 × (ρ/2000) × v²

где:
- 15000 = 1000 × 15 (коэффициент)
- ρ = плотность в кг/м³ (делить на 1000 для г/см³)
- v = скорость в м/с
```

**Важно:** Формулы МЕНЯЮТСЯ МЕСТАМИ для HKV-D!

### 6.2. Формула DpRohr

```
DpRohr = (L_hk + L_zul) × R

где:
- L_hk = длина контура, м
- L_zul = длина подводки, м
- R = удельные потери, Па/м
```

### 6.3. Формула DpGesamt

```
DpGesamt = DpRohr + DpVerteiler + DpVent
```

### 6.4. Формула zu_drosseln

```
zu_drosseln = DpGesamt_max - DpGesamt_контур

где:
- DpGesamt_max = максимальные суммарные потери в коллекторе
- DpGesamt_контур = суммарные потери контура
```

---

## 7. Алгоритм балансировки

### 7.1. Текущий алгоритм (ошибочный)

```
1. Найти контур с максимальными потерями в ТРУБЕ (CircuitPipeLoss + SupplyPipeLoss)
2. Референсный контур получает обороты = CalculateTurns(maxKv, valveType)
3. zu_drosseln = maxPipeLoss - pipeLoss
```

### 7.2. Новый алгоритм (правильный)

```
1. Найти контур с МАКСИМАЛЬНЫМ DpGesamt
2. Референсный контур получает МАКСИМАЛЬНЫЕ обороты:
   - HKV-D: 2.5 оборота
   - IV: 8.0 оборотов
3. zu_drosseln = DpGesamt_max - DpGesamt_контур
4. Для нереференсных контуров:
   - Рассчитать Kv для дросселирования
   - Рассчитать обороты по формуле umdreh1(Kv, type)
```

---

## 8. Изменения в файлах

### 8.1. Список файлов для изменения

| Файл | Изменения |
|------|-----------|
| `src/Services/Hydraulics/ValveTurnsCalculator.cs` | Добавить `GetMaxTurns()`, изменить проверку MaxTurns |
| `src/Services/Hydraulics/CircuitsCalculator.cs` | Добавить параметр `ValveType` в `CalculateAtTemperature`, изменить формулы, изменить балансировку |
| `src/Models/Hydraulics/CircuitRow.cs` | Добавить свойства `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt`, `ZuDrosseln` в `CircuitTemperatureResult` |
| `src/Views/Hydraulics/CircuitsView.xaml` | Заменить колонки таблицы |
| `tests/.../ValveTurnsCalculatorTests.cs` | Добавить тесты `GetMaxTurns` |
| `tests/.../CircuitsCalculatorTests.cs` | Добавить тесты формул и балансировки |

### 8.2. Детальные изменения по файлам

#### 8.2.1. ValveTurnsCalculator.cs

**Добавить:**
- Метод `GetMaxTurns(ValveType valveType)`

**Изменить:**
- Метод `CalculateTurnsWithWarning()` — использовать `GetMaxTurns()` вместо константы `MaxTurns`

#### 8.2.2. CircuitsCalculator.cs

**Изменить:**
- Метод `CalculateAtTemperature()` — добавить параметр `ValveType valveType`
- Добавить расчёт `DpRohr`, `DpVerteiler`, `DpVent` с разными формулами для HKV-D и IV
- Метод `CalculateBalancing()` — изменить алгоритм (референсный контур = MAX(DpGesamt))
- Метод `CalculateAllCircuits()` — передавать `ValveType` в `CalculateAtTemperature()`

#### 8.2.3. CircuitRow.cs (CircuitTemperatureResult)

**Добавить:**
- Свойство `DpRohr`
- Свойство `DpVerteiler`
- Свойство `DpVent`
- Свойство `DpGesamt` (вычисляемое)
- Свойство `ZuDrosseln`

**Пометить как устаревшие:**
- `CircuitPipeLoss`
- `SupplyPipeLoss`
- `ValveLoss`
- `TotalLoss`

#### 8.2.4. CircuitsView.xaml

**Заменить колонки:**

| Было | Должно быть |
|------|-------------|
| Δp контур (мбар) | DpRohr (Па) |
| Δp клапан (мбар) | DpVerteiler (Па) |
| Δp сумма (мбар) | DpVent (Па) |
| — | DpGesamt (Па) |
| — | zu_drosseln (Па) |

**Формат отображения:** `StringFormat=F0` (целые числа)

---

## 9. Тестовые сценарии

### 9.1. Тесты для ValveTurnsCalculator

```csharp
[Test]
public void GetMaxTurns_HKV_D_Returns_2_5()
{
    Assert.That(ValveTurnsCalculator.GetMaxTurns(ValveType.HKV_D), Is.EqualTo(2.5));
}

[Test]
public void GetMaxTurns_IV_1_25_Returns_8_0()
{
    Assert.That(ValveTurnsCalculator.GetMaxTurns(ValveType.IV_1_25), Is.EqualTo(8.0));
}

[Test]
public void GetMaxTurns_IV_1_5_Returns_8_0()
{
    Assert.That(ValveTurnsCalculator.GetMaxTurns(ValveType.IV_1_5), Is.EqualTo(8.0));
}
```

### 9.2. Тесты для CircuitsCalculator

```csharp
[Test]
public void DpVerteiler_IV_CorrectFormula()
{
    // Для IV: DpVerteiler = 15000 × (ρ/2000) × v²
    // При ρ = 1053 кг/м³, v = 0.59 м/с
    // DpVerteiler = 15000 × (1.053/2) × 0.59² = 2754 Па
    
    var result = CalculateAtTemperature(..., ValveType.IV_1_25);
    Assert.That(result.DpVerteiler, Is.EqualTo(2754).Within(10));
}

[Test]
public void DpVerteiler_HKV_D_CorrectFormula()
{
    // Для HKV-D: DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
    // При V_dot = 280 л/ч, ρ = 1053 кг/м³
    // DpVerteiler = (0.28/1.2)² × 100000 × 1.053 = 5735 Па
    
    var result = CalculateAtTemperature(..., ValveType.HKV_D);
    Assert.That(result.DpVerteiler, Is.EqualTo(5735).Within(50));
}

[Test]
public void Balancing_ReferenceCircuit_GetsMaxTurns()
{
    var circuits = CreateTestCircuits();
    var balanced = calculator.CalculateBalancing(circuits, ValveType.HKV_D);
    
    var referenceCircuit = balanced.First(c => c.IsReferenceCircuit);
    
    // Референсный контур должен иметь МАКСИМАЛЬНЫЕ обороты
    Assert.That(referenceCircuit.ValveTurns, Is.EqualTo(2.5));
    Assert.That(referenceCircuit.Throttling, Is.EqualTo(0).Within(0.01));
}
```

---

## 10. Порядок реализации

1. **Задача 1:** Добавить `GetMaxTurns()` в `ValveTurnsCalculator`
2. **Задача 2:** Добавить свойства в `CircuitTemperatureResult`
3. **Задача 3:** Изменить формулы в `CircuitsCalculator`
4. **Задача 4:** Изменить алгоритм балансировки
5. **Задача 5:** Обновить UI (колонки таблицы)
6. **Задача 6:** Написать тесты
7. **Задача 7:** Провести валидацию по Excel

---

## 11. Ожидаемый результат

После исправлений программа должна показывать те же значения, что и Excel:

| Контур | DpRohr (Па) | DpVerteiler (Па) | DpVent (Па) | DpGesamt (Па) | zu_drosseln (Па) | Обороты |
|--------|-------------|------------------|-------------|---------------|------------------|---------|
| 1 | 467 | 61 | 202 | 730 | 1069 | 2/4 |
| 2 | 582 | 73 | 244 | 899 | 900 | 2/4 |
| 3 | 861 | 102 | 339 | 1303 | 496 | 3/4 |
| 4 | 1212 | 136 | 450 | 1798 | 0 | 2 1/2 |

**Примечание:** Значения приблизительные, точные значения из Excel.

---

## 12. Риски и ограничения

### 12.1. Риски

| Риск | Вероятность | Влияние | Митигация |
|------|-------------|---------|-----------|
| Несовпадение с Excel | Средняя | Высокое | Детальное тестирование по примерам |
| Обратная совместимость | Низкая | Среднее | Пометить устаревшие свойства `[Obsolete]` |
| Регрессия в UI | Средняя | Среднее | Обновить привязки в XAML |

### 12.2. Ограничения

- Формулы взяты из Excel gidravlica.xls и не подлежат изменению
- Максимальные обороты HKV-D = 2.5 (не 8.0!)
- Единицы давления: Па (не мбар)

---

*Архитектура создана: 2026-03-22*