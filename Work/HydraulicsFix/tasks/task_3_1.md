# Task 3.1: Исправить формулы в CircuitsCalculator

**Статус:** Ожидает выполнения  
**Приоритет:** Критический  
**Связанные UC:** UC-2  
**Зависимости:** Task 2.1 (нужны новые свойства в модели)  

---

## 1. Цель задачи

Реализовать разные формулы расчёта `DpVerteiler` и `DpVent` для типов коллекторов HKV-D и IV, а также добавить расчёт `DpRohr` и `DpGesamt`.

---

## 2. Проблема

**Текущее поведение:**
- Формулы `DpVerteiler` и `DpVent` одинаковы для всех типов коллекторов
- Нет отдельного расчёта `DpRohr` (потери в трубе)
- Нет расчёта `DpVerteiler` (потери в распределителе)
- `DpGesamt` не вычисляется

**Ожидаемое поведение:**
- Для IV 1¼" и IV 1½":
  - `DpVerteiler = 15000 × (ρ/2000) × v²`
  - `DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000`
- Для HKV-D:
  - `DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000`
  - `DpVent = 15000 × (ρ/2000) × v²`
- `DpRohr = (L_hk + L_zul) × R`
- `DpGesamt = DpRohr + DpVerteiler + DpVent`

---

## 3. Связанные юзер-кейсы

### UC-2: Расчёт потерь давления DpVerteiler и DpVent

**Основной сценарий для IV 1¼" / IV 1½":**

1. Система получает расход V_dot (л/ч)
2. Система получает плотность ρ (кг/м³)
3. Система получает скорость v (м/с)
4. Система получает Kv клапана (м³/ч)
5. **Система рассчитывает DpVerteiler:**
   ```
   DpVerteiler = 15000 × (ρ/2000) × v²
   ```
6. **Система рассчитывает DpVent:**
   ```
   DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
   ```
7. Система сохраняет результаты в модель

**Альтернативный сценарий для HKV-D:**

**Важно:** Формулы меняются местами!

1. Система получает расход V_dot (л/ч)
2. Система получает плотность ρ (кг/м³)
3. Система получает скорость v (м/с)
4. Система получает Kv = 1.2 для HKV-D
5. **Система рассчитывает DpVerteiler:**
   ```
   DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
   ```
6. **Система рассчитывает DpVent:**
   ```
   DpVent = 15000 × (ρ/2000) × v²
   ```
7. Система сохраняет результаты в модель

**Критерии приёмки:**
- ✅ Для IV: DpVerteiler = 15000 × (ρ/2000) × v²
- ✅ Для IV: DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
- ✅ Для HKV-D: DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
- ✅ Для HKV-D: DpVent = 15000 × (ρ/2000) × v²
- ✅ Значения совпадают с Excel (±1%)

---

## 4. Изменения в файлах

### 4.1. Файл: `src/Services/Hydraulics/CircuitsCalculator.cs`

#### 4.1.1. Изменить сигнатуру метода CalculateAtTemperature

**Текущий код (строки 166-171):**

```csharp
public CircuitTemperatureResult CalculateAtTemperature(
    CircuitRow circuit,
    double temperature,
    GlycolProperties glycolProps,
    double innerDiameter,
    double kv)
```

**Новый код:**

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
public CircuitTemperatureResult CalculateAtTemperature(
    CircuitRow circuit,
    double temperature,
    GlycolProperties glycolProps,
    double innerDiameter,
    double kv,
    ValveType valveType)  // ← ДОБАВИТЬ ПАРАМЕТР
```

#### 4.1.2. Изменить тело метода CalculateAtTemperature

**Текущий код (строки 185-229):**

```csharp
var result = new CircuitTemperatureResult
{
    Temperature = temperature,
    Density = glycolProps.Density / 1000.0,  // Конвертация: кг/м³ → г/см³
    KinematicViscosity = glycolProps.KinematicViscosity
};

// ... скорость, Re, режим, λ ...

// Удельные потери: R = 10000 × (v² × ρ[г/см³] × λ) / (2 × d_inner) × 100
double density_g_cm3 = glycolProps.Density / 1000.0;
double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * density_g_cm3 * frictionFactor
    / (2 * innerDiameter) * 100;
result.PressureLossPerMeter = pressureLossPerMeter;

// Потери в трубе контура: Δp_HK = L_hk × R
result.CircuitPipeLoss = circuit.CircuitLength * pressureLossPerMeter;

// Потери в трубе подводки: Δp_Zul = L_zul × R
result.SupplyPipeLoss = circuit.SupplyLength * pressureLossPerMeter;

// Потери в вентиле: Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ[г/см³]
result.ValveLoss = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;

// Суммарные потери: Δp_total = Δp_HK + Δp_Zul + Δp_Vent
// (вычисляется автоматически в свойстве TotalLoss)

return result;
```

**Новый код:**

```csharp
var result = new CircuitTemperatureResult
{
    Temperature = temperature,
    Density = glycolProps.Density / 1000.0,  // Конвертация: кг/м³ → г/см³
    KinematicViscosity = glycolProps.KinematicViscosity
};

// Скорость потока: v = V_dot × 4000 / (3600 × π × d_inner²)
double velocity = circuit.FlowRate * 4000 / (3600 * Math.PI * Math.Pow(innerDiameter, 2));
circuit.Velocity = velocity;

// Число Рейнольдса: Re = 1000 × v × d_inner / ν
double reynolds = 1000 * velocity * innerDiameter / glycolProps.KinematicViscosity;
result.ReynoldsNumber = reynolds;

// Режим течения
result.FlowRegime = FlowRegimeCalculator.DetermineFlowRegime(reynolds);

// Коэффициент трения λ
double frictionFactor = FlowRegimeCalculator.CalculateFrictionFactor(reynolds, innerDiameter);
result.FrictionFactor = frictionFactor;

// Удельные потери: R = 10000 × (v² × ρ[г/см³] × λ) / (2 × d_inner) × 100
double density_g_cm3 = glycolProps.Density / 1000.0;
double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * density_g_cm3 * frictionFactor
    / (2 * innerDiameter) * 100;
result.PressureLossPerMeter = pressureLossPerMeter;

// === НОВЫЙ РАСЧЁТ ===

// DpRohr = потери в трубе контура + подводки
// Формула: DpRohr = (L_hk + L_zul) × R
double dpRohr = (circuit.CircuitLength + circuit.SupplyLength) * pressureLossPerMeter;
result.DpRohr = dpRohr;

// DpVerteiler и DpVent — формулы меняются местами для HKV-D и IV
if (valveType == ValveType.HKV_D)
{
    // HKV-D: формулы меняются местами
    // DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
    // Kv для HKV-D = 1.2
    result.DpVerteiler = Math.Pow(circuit.FlowRate / 1000.0 / 1.2, 2) * 100000 * density_g_cm3;
    
    // DpVent = 15000 × (ρ/2000) × v²
    result.DpVent = 15000 * (density_g_cm3 / 2) * Math.Pow(velocity, 2);
}
else
{
    // IV 1¼" и IV 1½": стандартные формулы
    // DpVerteiler = 15000 × (ρ/2000) × v²
    result.DpVerteiler = 15000 * (density_g_cm3 / 2) * Math.Pow(velocity, 2);
    
    // DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
    result.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;
}

// DpGesamt = DpRohr + DpVerteiler + DpVent (вычисляется автоматически)

// === УСТАРЕВШИЕ СВОЙСТВА (для обратной совместимости) ===
#pragma warning disable CS0618 // Type or member is obsolete
result.CircuitPipeLoss = circuit.CircuitLength * pressureLossPerMeter;
result.SupplyPipeLoss = circuit.SupplyLength * pressureLossPerMeter;
result.ValveLoss = result.DpVent;  // Для IV это корректно, для HKV-D — нет
#pragma warning restore CS0618

return result;
```

#### 4.1.3. Изменить метод CalculateAllCircuits

**Текущий код (строки 286-316):**

```csharp
foreach (var circuit in circuits)
{
    if (!circuit.IsActive)
        continue;

    // Расчёт мощности
    circuit.Power = CalculateCircuitPower(circuit, inputData.PowerUp, inputData.PowerDown, pipeSpacing_cm);

    // Расчёт расхода
    circuit.FlowRate = CalculateFlowRate(
        circuit.Power,
        inputData.DeltaT,
        glycolPropsOperating.Density,
        glycolPropsOperating.SpecificHeat);

    // Расчёт при рабочей температуре
    circuit.OperatingResult = CalculateAtTemperature(
        circuit,
        inputData.OperatingTemperature,
        glycolPropsOperating,
        inputData.InnerDiameter,
        kv);

    // Расчёт при расчётной температуре
    circuit.DesignResult = CalculateAtTemperature(
        circuit,
        inputData.DesignTemperature,
        glycolPropsDesign,
        inputData.InnerDiameter,
        kv);
}
```

**Новый код:**

```csharp
foreach (var circuit in circuits)
{
    if (!circuit.IsActive)
        continue;

    // Расчёт мощности
    circuit.Power = CalculateCircuitPower(circuit, inputData.PowerUp, inputData.PowerDown, pipeSpacing_cm);

    // Расчёт расхода
    circuit.FlowRate = CalculateFlowRate(
        circuit.Power,
        inputData.DeltaT,
        glycolPropsOperating.Density,
        glycolPropsOperating.SpecificHeat);

    // Расчёт при рабочей температуре
    circuit.OperatingResult = CalculateAtTemperature(
        circuit,
        inputData.OperatingTemperature,
        glycolPropsOperating,
        inputData.InnerDiameter,
        kv,
        inputData.ValveType);  // ← ДОБАВИТЬ ПАРАМЕТР

    // Расчёт при расчётной температуре
    circuit.DesignResult = CalculateAtTemperature(
        circuit,
        inputData.DesignTemperature,
        glycolPropsDesign,
        inputData.InnerDiameter,
        kv,
        inputData.ValveType);  // ← ДОБАВИТЬ ПАРАМЕТР
}
```

#### 4.1.4. Изменить интерфейс ICircuitsCalculator

**Файл:** `src/Services/Hydraulics/ICircuitsCalculator.cs`

**Текущий код:**

```csharp
CircuitTemperatureResult CalculateAtTemperature(
    CircuitRow circuit,
    double temperature,
    GlycolProperties glycolProps,
    double innerDiameter,
    double kv);
```

**Новый код:**

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

## 5. Формулы

### 5.1. DpRohr (потери в трубе)

```
DpRohr = (L_hk + L_zul) × R

где:
- L_hk = длина контура, м
- L_zul = длина подводки, м
- R = удельные потери, Па/м
```

### 5.2. DpVerteiler (потери в распределителе)

**Для IV 1¼" и IV 1½":**

```
DpVerteiler = 15000 × (ρ/2000) × v²

где:
- 15000 = 1000 × 15 (коэффициент)
- ρ = плотность в кг/м³ (делить на 1000 для г/см³)
- v = скорость в м/с
```

**Для HKV-D:**

```
DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000

где:
- V_dot = расход в л/ч
- 1.2 = Kv для HKV-D
- ρ = плотность в кг/м³ (делить на 1000 для г/см³)
```

### 5.3. DpVent (потери в вентиле)

**Для IV 1¼" и IV 1½":**

```
DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000

где:
- V_dot = расход в л/ч
- Kv = 1.45 (IV 1¼") или 1.5 (IV 1½")
- ρ = плотность в кг/м³ (делить на 1000 для г/см³)
```

**Для HKV-D:**

```
DpVent = 15000 × (ρ/2000) × v²

где:
- 15000 = 1000 × 15 (коэффициент)
- ρ = плотность в кг/м³ (делить на 1000 для г/см³)
- v = скорость в м/с
```

### 5.4. DpGesamt (суммарные потери)

```
DpGesamt = DpRohr + DpVerteiler + DpVent
```

---

## 6. Тест-кейсы

### 6.1. Тесты для DpVerteiler и DpVent

**Файл:** `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs`

```csharp
#region DpVerteiler Tests

[Test]
public void DpVerteiler_IV_CorrectFormula()
{
    // Arrange
    // Для IV: DpVerteiler = 15000 × (ρ/2000) × v²
    // При ρ = 1053 кг/м³, v = 0.59 м/с
    // DpVerteiler = 15000 × (1.053/2) × 0.59² = 2754 Па
    
    var circuit = CreateTestCircuit(flowRate: 280, circuitLength: 100, supplyLength: 10);
    var glycolProps = CreateTestGlycolProperties(density: 1053, kinematicViscosity: 3.5);
    double innerDiameter = 13.0;
    double kv = 1.45;
    
    // Act
    var result = _calculator.CalculateAtTemperature(
        circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.IV_1_25);
    
    // Assert
    // Ожидаемое значение: ~2754 Па (±50 Па из-за округления скорости)
    Assert.That(result.DpVerteiler, Is.EqualTo(2754).Within(100));
}

[Test]
public void DpVerteiler_HKV_D_CorrectFormula()
{
    // Arrange
    // Для HKV-D: DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
    // При V_dot = 280 л/ч, ρ = 1053 кг/м³
    // DpVerteiler = (0.28/1.2)² × 100000 × 1.053 = 5735 Па
    
    var circuit = CreateTestCircuit(flowRate: 280, circuitLength: 100, supplyLength: 10);
    var glycolProps = CreateTestGlycolProperties(density: 1053, kinematicViscosity: 3.5);
    double innerDiameter = 13.0;
    double kv = 1.2;  // Kv для HKV-D
    
    // Act
    var result = _calculator.CalculateAtTemperature(
        circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.HKV_D);
    
    // Assert
    // Ожидаемое значение: ~5735 Па
    Assert.That(result.DpVerteiler, Is.EqualTo(5735).Within(100));
}

#endregion

#region DpVent Tests

[Test]
public void DpVent_IV_CorrectFormula()
{
    // Arrange
    // Для IV: DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
    // При V_dot = 280 л/ч, Kv = 1.45, ρ = 1053 кг/м³
    // DpVent = (0.28/1.45)² × 100000 × 1.053 = 3925 Па
    
    var circuit = CreateTestCircuit(flowRate: 280, circuitLength: 100, supplyLength: 10);
    var glycolProps = CreateTestGlycolProperties(density: 1053, kinematicViscosity: 3.5);
    double innerDiameter = 13.0;
    double kv = 1.45;
    
    // Act
    var result = _calculator.CalculateAtTemperature(
        circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.IV_1_25);
    
    // Assert
    // Ожидаемое значение: ~3925 Па
    Assert.That(result.DpVent, Is.EqualTo(3925).Within(100));
}

[Test]
public void DpVent_HKV_D_CorrectFormula()
{
    // Arrange
    // Для HKV-D: DpVent = 15000 × (ρ/2000) × v²
    // При ρ = 1053 кг/м³, v = 0.59 м/с
    // DpVent = 15000 × (1.053/2) × 0.59² = 2754 Па
    
    var circuit = CreateTestCircuit(flowRate: 280, circuitLength: 100, supplyLength: 10);
    var glycolProps = CreateTestGlycolProperties(density: 1053, kinematicViscosity: 3.5);
    double innerDiameter = 13.0;
    double kv = 1.2;
    
    // Act
    var result = _calculator.CalculateAtTemperature(
        circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.HKV_D);
    
    // Assert
    // Ожидаемое значение: ~2754 Па
    Assert.That(result.DpVent, Is.EqualTo(2754).Within(100));
}

#endregion

#region DpGesamt Tests

[Test]
public void DpGesamt_SumOfComponents_ReturnsCorrectValue()
{
    // Arrange
    var circuit = CreateTestCircuit(flowRate: 280, circuitLength: 100, supplyLength: 10);
    var glycolProps = CreateTestGlycolProperties(density: 1053, kinematicViscosity: 3.5);
    double innerDiameter = 13.0;
    double kv = 1.45;
    
    // Act
    var result = _calculator.CalculateAtTemperature(
        circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.IV_1_25);
    
    // Assert
    Assert.That(result.DpGesamt, Is.EqualTo(result.DpRohr + result.DpVerteiler + result.DpVent));
}

#endregion
```

---

## 7. Критерии приёмки

### 7.1. Функциональные

- [ ] Метод `CalculateAtTemperature` принимает параметр `ValveType`
- [ ] Для IV: `DpVerteiler = 15000 × (ρ/2000) × v²`
- [ ] Для IV: `DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000`
- [ ] Для HKV-D: `DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000`
- [ ] Для HKV-D: `DpVent = 15000 × (ρ/2000) × v²`
- [ ] `DpRohr = (L_hk + L_zul) × R`
- [ ] `DpGesamt = DpRohr + DpVerteiler + DpVent`
- [ ] Значения совпадают с Excel (±1%)

### 7.2. Нефункциональные

- [ ] Все существующие тесты проходят
- [ ] Новые тесты добавлены и проходят
- [ ] Код соответствует стилю проекта
- [ ] XML-документация обновлена

---

## 8. Порядок выполнения

1. **Изменить интерфейс** `ICircuitsCalculator` — добавить параметр `ValveType`
2. **Изменить метод** `CalculateAtTemperature` — реализовать разные формулы
3. **Изменить метод** `CalculateAllCircuits` — передавать `ValveType`
4. **Добавить тесты** для разных типов коллекторов
5. **Запустить тесты** и убедиться, что все проходят
6. **Проверить значения** по Excel-примерам

---

## 9. Примечания

### 9.1. Почему формулы меняются местами для HKV-D?

В Excel-файле gidravlica.xls формулы для HKV-D отличаются от IV:
- Для HKV-D: `DpVerteiler` рассчитывается по формуле вентиля
- Для HKV-D: `DpVent` рассчитывается по формуле распределителя

Это связано с конструктивными особенностями бытового коллектора HKV-D.

### 9.2. Связь с другими задачами

Эта задача **зависит от**:
- **Task 2.1 (Модель):** Нужно, чтобы свойства `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt` существовали

Эта задача является **базовой для**:
- **Task 4.1 (Балансировка):** Нужно `DpGesamt` для определения референсного контура
- **Task 6.1 (UI):** Нужно отображать новые свойства

---

*Задача создана: 2026-03-22*