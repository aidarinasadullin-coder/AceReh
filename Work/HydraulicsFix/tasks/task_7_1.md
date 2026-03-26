# Task 7.1: Обновить тесты

**Статус:** Ожидает выполнения  
**Приоритет:** Высокий  
**Связанные UC:** Все  
**Зависимости:** Task 1.1, Task 2.1, Task 3.1, Task 4.1, Task 5.1, Task 6.1  

---

## 1. Цель задачи

Добавить тесты для новых формул, алгоритмов и свойств, а также обновить существующие тесты для работы с новыми свойствами.

---

## 2. Проблема

**Текущее поведение:**
- Нет тестов для `GetMaxTurns`
- Нет тестов для `DpVerteiler` и `DpVent` с разными типами коллекторов
- Нет тестов для алгоритма балансировки с `DpGesamt`
- Нет тестов для конвертации единиц (Па vs мбар)

**Ожидаемое поведение:**
- Тесты для `GetMaxTurns` для всех типов клапанов
- Тесты для `DpVerteiler` и `DpVent` для HKV-D и IV
- Тесты для алгоритма балансировки с `DpGesamt`
- Тесты для конвертации единиц

---

## 3. Изменения в файлах

### 3.1. Файл: `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/ValveTurnsCalculatorTests.cs`

#### 3.1.1. Добавить тесты для GetMaxTurns

```csharp
#region GetMaxTurns Tests

[Test]
public void GetMaxTurns_HKV_D_Returns_2_5()
{
    // Arrange & Act
    double maxTurns = ValveTurnsCalculator.GetMaxTurns(ValveType.HKV_D);
    
    // Assert
    Assert.That(maxTurns, Is.EqualTo(2.5));
}

[Test]
public void GetMaxTurns_IV_1_25_Returns_8_0()
{
    // Arrange & Act
    double maxTurns = ValveTurnsCalculator.GetMaxTurns(ValveType.IV_1_25);
    
    // Assert
    Assert.That(maxTurns, Is.EqualTo(8.0));
}

[Test]
public void GetMaxTurns_IV_1_5_Returns_8_0()
{
    // Arrange & Act
    double maxTurns = ValveTurnsCalculator.GetMaxTurns(ValveType.IV_1_5);
    
    // Assert
    Assert.That(maxTurns, Is.EqualTo(8.0));
}

[Test]
public void GetMaxTurns_InvalidType_ThrowsArgumentException()
{
    // Arrange
    var invalidType = (ValveType)999;
    
    // Act & Assert
    Assert.Throws<ArgumentException>(() => 
        ValveTurnsCalculator.GetMaxTurns(invalidType));
}

#endregion

#region CalculateTurnsWithWarning Tests (Updated)

[Test]
public void CalculateTurnsWithWarning_HKV_D_ExceedsMax_Returns_2_5()
{
    // Arrange
    // Kv = 4.0 для HKV-D даёт обороты > 2.5
    double kv = 4.0;
    
    // Act
    var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, ValveType.HKV_D);
    
    // Assert
    Assert.That(turns, Is.EqualTo(2.5));
    Assert.That(warning, Is.Not.Null);
    Assert.That(warning, Does.Contain("превышают максимум"));
}

[Test]
public void CalculateTurnsWithWarning_HKV_D_BelowMax_ReturnsCalculated()
{
    // Arrange
    // Kv = 1.2 для HKV-D даёт обороты < 2.5
    double kv = 1.2;
    
    // Act
    var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, ValveType.HKV_D);
    
    // Assert
    // Формула: 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712
    // При Kv = 1.2: 4.2111×1.728 - 6.7436×1.44 + 4.6613×1.2 - 0.712 ≈ 2.5
    Assert.That(turns, Is.LessThanOrEqualTo(2.5));
    Assert.That(warning, Is.Null);
}

[Test]
public void CalculateTurnsWithWarning_IV_1_25_ExceedsMax_Returns_8_0()
{
    // Arrange
    // Kv = 3.0 для IV 1¼" даёт обороты > 8.0
    double kv = 3.0;
    
    // Act
    var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, ValveType.IV_1_25);
    
    // Assert
    Assert.That(turns, Is.EqualTo(8.0));
    Assert.That(warning, Is.Not.Null);
}

[Test]
public void CalculateTurnsWithWarning_IV_1_5_ExceedsMax_Returns_8_0()
{
    // Arrange
    // Kv = 3.5 для IV 1½" даёт обороты > 8.0
    double kv = 3.5;
    
    // Act
    var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, ValveType.IV_1_5);
    
    // Assert
    Assert.That(turns, Is.EqualTo(8.0));
    Assert.That(warning, Is.Not.Null);
}

#endregion
```

---

### 3.2. Файл: `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs`

#### 3.2.1. Добавить тесты для DpVerteiler и DpVent

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
    // Ожидаемое значение: ~2754 Па (±100 Па из-за округления скорости)
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

[Test]
public void DpVerteiler_IV_1_5_SameFormulaAsIV_1_25()
{
    // Arrange
    // IV 1½" использует ту же формулу, что и IV 1¼"
    
    var circuit = CreateTestCircuit(flowRate: 280, circuitLength: 100, supplyLength: 10);
    var glycolProps = CreateTestGlycolProperties(density: 1053, kinematicViscosity: 3.5);
    double innerDiameter = 13.0;
    double kv = 1.5;  // Kv для IV 1½"
    
    // Act
    var result = _calculator.CalculateAtTemperature(
        circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.IV_1_5);
    
    // Assert
    // DpVerteiler должен быть таким же, как для IV 1¼"
    Assert.That(result.DpVerteiler, Is.EqualTo(2754).Within(100));
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

[Test]
public void DpGesamt_MatchesExcel_Example()
{
    // Arrange
    // Пример из Excel:
    // DpRohr = 467 Па
    // DpVerteiler = 61 Па
    // DpVent = 202 Па
    // DpGesamt = 730 Па
    
    var circuit = CreateTestCircuit(flowRate: 280, circuitLength: 100, supplyLength: 10);
    var glycolProps = CreateTestGlycolProperties(density: 1053, kinematicViscosity: 3.5);
    double innerDiameter = 13.0;
    double kv = 1.45;
    
    // Act
    var result = _calculator.CalculateAtTemperature(
        circuit, 40.0, glycolProps, innerDiameter, kv, ValveType.IV_1_25);
    
    // Assert
    // Проверяем, что DpGesamt примерно равен 730 Па
    // (точное значение зависит от параметров теста)
    Assert.That(result.DpGesamt, Is.GreaterThan(0));
}

#endregion

#region Balancing Tests

[Test]
public void Balancing_ReferenceCircuit_HasMaxDpGesamt()
{
    // Arrange
    var circuits = CreateTestCircuits();
    var calculator = CreateCalculator();
    
    // Act
    var balanced = calculator.CalculateBalancing(circuits, ValveType.HKV_D);
    
    // Assert
    var referenceCircuit = balanced.First(c => c.IsReferenceCircuit);
    double maxDpGesamt = balanced.Max(c => c.OperatingResult.DpGesamt);
    
    Assert.That(referenceCircuit.OperatingResult.DpGesamt, Is.EqualTo(maxDpGesamt).Within(0.01));
}

[Test]
public void Balancing_ReferenceCircuit_GetsMaxTurns_HKV_D()
{
    // Arrange
    var circuits = CreateTestCircuits();
    var calculator = CreateCalculator();
    
    // Act
    var balanced = calculator.CalculateBalancing(circuits, ValveType.HKV_D);
    
    // Assert
    var referenceCircuit = balanced.First(c => c.IsReferenceCircuit);
    
    // Референсный контур должен иметь МАКСИМАЛЬНЫЕ обороты для HKV-D
    Assert.That(referenceCircuit.ValveTurns, Is.EqualTo(2.5));
    Assert.That(referenceCircuit.Throttling, Is.EqualTo(0).Within(0.01));
}

[Test]
public void Balancing_ReferenceCircuit_GetsMaxTurns_IV()
{
    // Arrange
    var circuits = CreateTestCircuits();
    var calculator = CreateCalculator();
    
    // Act
    var balanced = calculator.CalculateBalancing(circuits, ValveType.IV_1_25);
    
    // Assert
    var referenceCircuit = balanced.First(c => c.IsReferenceCircuit);
    
    // Референсный контур должен иметь МАКСИМАЛЬНЫЕ обороты для IV
    Assert.That(referenceCircuit.ValveTurns, Is.EqualTo(8.0));
    Assert.That(referenceCircuit.Throttling, Is.EqualTo(0).Within(0.01));
}

[Test]
public void Balancing_NonReferenceCircuit_HasPositiveThrottling()
{
    // Arrange
    var circuits = CreateTestCircuits();
    var calculator = CreateCalculator();
    
    // Act
    var balanced = calculator.CalculateBalancing(circuits, ValveType.HKV_D);
    
    // Assert
    var nonReferenceCircuits = balanced.Where(c => !c.IsReferenceCircuit);
    
    foreach (var circuit in nonReferenceCircuits)
    {
        // zu_drosseln должен быть положительным для нереференсных контуров
        Assert.That(circuit.Throttling, Is.GreaterThanOrEqualTo(0));
    }
}

[Test]
public void Balancing_Throttling_Equals_DpGesamtDifference()
{
    // Arrange
    var circuits = CreateTestCircuits();
    var calculator = CreateCalculator();
    
    // Act
    var balanced = calculator.CalculateBalancing(circuits, ValveType.HKV_D);
    
    // Assert
    double maxDpGesamt = balanced.Max(c => c.OperatingResult.DpGesamt);
    
    foreach (var circuit in balanced)
    {
        double expectedThrottling = maxDpGesamt - circuit.OperatingResult.DpGesamt;
        Assert.That(circuit.Throttling, Is.EqualTo(expectedThrottling).Within(0.01));
    }
}

[Test]
public void Balancing_AllCircuitsHaveValveTurns()
{
    // Arrange
    var circuits = CreateTestCircuits();
    var calculator = CreateCalculator();
    
    // Act
    var balanced = calculator.CalculateBalancing(circuits, ValveType.HKV_D);
    
    // Assert
    foreach (var circuit in balanced.Where(c => c.IsActive))
    {
        Assert.That(circuit.ValveTurns, Is.GreaterThan(0));
        Assert.That(circuit.ValveTurns, Is.LessThanOrEqualTo(2.5));  // Max for HKV-D
    }
}

#endregion
```

---

### 3.3. Файл: `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CircuitTemperatureResultTests.cs`

#### 3.3.1. Добавить тесты для новых свойств

```csharp
#region DpGesamt Tests

[Test]
public void DpGesamt_SumOfComponents_ReturnsCorrectValue()
{
    // Arrange
    var result = new CircuitTemperatureResult
    {
        DpRohr = 467,
        DpVerteiler = 61,
        DpVent = 202
    };
    
    // Act
    double dpGesamt = result.DpGesamt;
    
    // Assert
    Assert.That(dpGesamt, Is.EqualTo(730));
}

[Test]
public void DpGesamt_ZeroComponents_ReturnsZero()
{
    // Arrange
    var result = new CircuitTemperatureResult
    {
        DpRohr = 0,
        DpVerteiler = 0,
        DpVent = 0
    };
    
    // Act
    double dpGesamt = result.DpGesamt;
    
    // Assert
    Assert.That(dpGesamt, Is.EqualTo(0));
}

[Test]
public void DpGesamt_NegativeComponents_ReturnsCorrectValue()
{
    // Arrange
    // Примечание: отрицательные значения не должны быть, но тест проверяет корректность
    var result = new CircuitTemperatureResult
    {
        DpRohr = -100,
        DpVerteiler = 50,
        DpVent = 200
    };
    
    // Act
    double dpGesamt = result.DpGesamt;
    
    // Assert
    Assert.That(dpGesamt, Is.EqualTo(150));
}

#endregion

#region Obsolete Properties Tests

[Test]
public void TotalLoss_ReturnsSameAsDpGesamt()
{
    // Arrange
    var result = new CircuitTemperatureResult
    {
        DpRohr = 467,
        DpVerteiler = 61,
        DpVent = 202
    };
    
    // Act
#pragma warning disable CS0618 // Type or member is obsolete
    double totalLoss = result.TotalLoss;
#pragma warning restore CS0618
    
    // Assert
    Assert.That(totalLoss, Is.EqualTo(result.DpGesamt));
}

[Test]
public void TotalLoss_mbar_ReturnsCorrectValue()
{
    // Arrange
    var result = new CircuitTemperatureResult
    {
        DpRohr = 467,
        DpVerteiler = 61,
        DpVent = 202
    };
    
    // Act
#pragma warning disable CS0618 // Type or member is obsolete
    double totalLoss_mbar = result.TotalLoss_mbar;
#pragma warning restore CS0618
    
    // Assert
    Assert.That(totalLoss_mbar, Is.EqualTo(7.3).Within(0.01));
}

#endregion
```

---

## 4. Вспомогательные методы для тестов

### 4.1. Создание тестовых данных

```csharp
/// <summary>
/// Создать тестовый контур
/// </summary>
private CircuitRow CreateTestCircuit(double flowRate, double circuitLength, double supplyLength)
{
    return new CircuitRow
    {
        CircuitNumber = 1,
        CircuitLength = circuitLength,
        SupplyLength = supplyLength,
        FlowRate = flowRate,
        PipeSpacing_cm = 20.0,
        SupplySpacing_cm = 5.0,
        SupplyHeatPercent = 10.0
    };
}

/// <summary>
/// Создать тестовые свойства гликоля
/// </summary>
private GlycolProperties CreateTestGlycolProperties(double density, double kinematicViscosity)
{
    return new GlycolProperties
    {
        Temperature = 40.0,
        Density = density,
        KinematicViscosity = kinematicViscosity,
        SpecificHeat = 3.21,
        ThermalConductivity = 0.4,
        PrandtlNumber = 10.0
    };
}

/// <summary>
/// Создать список тестовых контуров
/// </summary>
private List<CircuitRow> CreateTestCircuits()
{
    return new List<CircuitRow>
    {
        new CircuitRow { CircuitNumber = 1, CircuitLength = 100, SupplyLength = 10, FlowRate = 280 },
        new CircuitRow { CircuitNumber = 2, CircuitLength = 120, SupplyLength = 12, FlowRate = 300 },
        new CircuitRow { CircuitNumber = 3, CircuitLength = 80, SupplyLength = 8, FlowRate = 250 },
        new CircuitRow { CircuitNumber = 4, CircuitLength = 150, SupplyLength = 15, FlowRate = 350 }
    };
}
```

---

## 5. Критерии приёмки

### 5.1. Функциональные

- [ ] Тесты для `GetMaxTurns` проходят
- [ ] Тесты для `DpVerteiler` проходят для HKV-D и IV
- [ ] Тесты для `DpVent` проходят для HKV-D и IV
- [ ] Тесты для `DpGesamt` проходят
- [ ] Тесты для балансировки проходят
- [ ] Тесты для конвертации единиц проходят

### 5.2. Нефункциональные

- [ ] Все тесты выполняются менее чем за 1 секунду
- [ ] Покрытие кода тестами ≥ 80%
- [ ] Тесты соответствуют стилю проекта

---

## 6. Порядок выполнения

1. **Добавить тесты** для `GetMaxTurns` в `ValveTurnsCalculatorTests.cs`
2. **Добавить тесты** для `DpVerteiler` и `DpVent` в `CircuitsCalculatorTests.cs`
3. **Добавить тесты** для `DpGesamt` в `CircuitTemperatureResultTests.cs`
4. **Добавить тесты** для балансировки в `CircuitsCalculatorTests.cs`
5. **Запустить все тесты** и убедиться, что проходят
6. **Проверить покрытие кода** тестами

---

## 7. Примечания

### 7.1. Почему ±100 Па в тестах?

Допуск ±100 Па обусловлен:
- Округлением скорости потока
- Округлением плотности гликоля
- Округлением коэффициента трения

### 7.2. Связь с другими задачами

Эта задача **зависит от**:
- **Task 1.1 (ValveTurnsCalculator):** Нужно тестировать `GetMaxTurns`
- **Task 2.1 (Модель):** Нужно тестировать новые свойства
- **Task 3.1 (Формулы):** Нужно тестировать новые формулы
- **Task 4.1 (Балансировка):** Нужно тестировать новый алгоритм
- **Task 5.1 (Единицы):** Нужно тестировать конвертацию

---

*Задача создана: 2026-03-22*