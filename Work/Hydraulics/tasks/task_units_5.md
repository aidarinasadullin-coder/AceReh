# Задача: Добавить Unit-тесты

## Метаданные

| Параметр | Значение |
|----------|----------|
| **ID** | task_units_5 |
| **Модуль** | Hydraulics |
| **Приоритет** | P1 (Критичный) |
| **Статус** | Ожидает |
| **Зависимости** | task_units_1, task_units_2, task_units_3, task_units_4 |
| **Юзер-кейсы** | UC-1, UC-2, UC-3, UC-4 |

---

## 1. Цель задачи

Добавить Unit-тесты для проверки корректности исправлений в переводах единиц измерения.

---

## 2. Связь с юзер-кейсами

| Юзер-кейс | Тест |
|-----------|------|
| UC-1 | `CalculateFlowRate_ReturnsCorrectValueInLitersPerHour` |
| UC-2 | `CalculateAtTemperature_PressureLossPerMeter_UsesDensityInGramsPerCm3` |
| UC-3 | `CalculateAtTemperature_ValveLoss_UsesDensityInGramsPerCm3` |
| UC-4 | `CalculateAtTemperature_Density_ConvertsKgPerM3ToGramsPerCm3` |

---

## 3. Описание изменений

### 3.1. Файл: `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs`

**Создать/дополнить** файл тестов.

---

## 4. Тест-кейсы

### 4.1. Тест: CalculateFlowRate_ReturnsCorrectValueInLitersPerHour

```csharp
[Fact]
public void CalculateFlowRate_ReturnsCorrectValueInLitersPerHour()
{
    // Arrange
    var calculator = new CircuitsCalculator(mockGlycolService.Object);
    double power = 5246;        // Вт
    double deltaT = 10;         // К
    double density = 1053;      // кг/м³
    double specificHeat = 3.21; // кДж/(кг·К)
    
    // Ожидаемый результат: V_dot = 5246 × 3.6 / (1053 × 3.21 × 10) × 1000 ≈ 560 л/ч
    double expected = 560;
    
    // Act
    double actual = calculator.CalculateFlowRate(power, deltaT, density, specificHeat);
    
    // Assert
    Assert.Equal(expected, actual, 0); // Точность до целых
}
```

### 4.2. Тест: CalculateFlowRate_VariousInputs_ReturnsCorrectValues

```csharp
[Theory]
[InlineData(5246, 10, 1053, 3.21, 560)]      // 50% гликоль
[InlineData(5000, 15, 1000, 4.18, 287)]       // Вода
[InlineData(10000, 10, 1053, 3.21, 1068)]     // Большая мощность
public void CalculateFlowRate_VariousInputs_ReturnsCorrectValues(
    double power, double deltaT, double density, double specificHeat, double expected)
{
    // Arrange
    var calculator = new CircuitsCalculator(mockGlycolService.Object);
    
    // Act
    double actual = calculator.CalculateFlowRate(power, deltaT, density, specificHeat);
    
    // Assert
    Assert.Equal(expected, actual, 0);
}
```

### 4.3. Тест: CalculateAtTemperature_PressureLossPerMeter_UsesDensityInGramsPerCm3

```csharp
[Fact]
public void CalculateAtTemperature_PressureLossPerMeter_UsesDensityInGramsPerCm3()
{
    // Arrange
    var circuit = new CircuitRow { CircuitLength = 100, SupplyLength = 20, FlowRate = 280 };
    var glycolProps = new GlycolProperties
    {
        Density = 1053,              // кг/м³
        KinematicViscosity = 2.16    // мм²/с
    };
    double innerDiameter = 13;       // мм
    double kv = 1.2;                 // м³/ч
    
    // Ожидаемый результат:
    // v = 280 × 4000 / (3600 × π × 13²) ≈ 0.59 м/с
    // Re = 1000 × 0.59 × 13 / 2.16 ≈ 3551
    // λ ≈ 0.042 (турбулентный режим)
    // R = 10000 × (0.59² × 1.053 × 0.042) / (2 × 13) × 100 ≈ 592 Па/м
    
    // Act
    var result = calculator.CalculateAtTemperature(circuit, 40, glycolProps, innerDiameter, kv);
    
    // Assert
    Assert.Equal(592, result.PressureLossPerMeter, 0); // ±0 Па/м
}
```

### 4.4. Тест: CalculateAtTemperature_PressureLossPerMeter_WrongDensityGivesWrongResult

```csharp
[Fact]
public void CalculateAtTemperature_PressureLossPerMeter_WrongDensityGivesWrongResult()
{
    // Arrange
    var circuit = new CircuitRow { CircuitLength = 100, SupplyLength = 20, FlowRate = 280 };
    var glycolProps = new GlycolProperties
    {
        Density = 1053,              // кг/м³ (БЕЗ конвертации будет ошибка!)
        KinematicViscosity = 2.16
    };
    double innerDiameter = 13;
    double kv = 1.2;
    
    // Если использовать плотность в кг/м³ без конвертации:
    // R_wrong = 10000 × (0.59² × 1053 × 0.042) / (2 × 13) × 100 ≈ 592000 Па/м
    // Это в 1000 раз больше правильного значения!
    
    // Act
    var result = calculator.CalculateAtTemperature(circuit, 40, glycolProps, innerDiameter, kv);
    
    // Assert
    Assert.NotEqual(592000, result.PressureLossPerMeter); // НЕ должно быть 592000!
    Assert.Equal(592, result.PressureLossPerMeter, 0);     // Должно быть 592
}
```

### 4.5. Тест: CalculateAtTemperature_ValveLoss_UsesDensityInGramsPerCm3

```csharp
[Fact]
public void CalculateAtTemperature_ValveLoss_UsesDensityInGramsPerCm3()
{
    // Arrange
    var circuit = new CircuitRow { CircuitLength = 100, SupplyLength = 20, FlowRate = 280 };
    var glycolProps = new GlycolProperties
    {
        Density = 1053,              // кг/м³
        KinematicViscosity = 2.16
    };
    double innerDiameter = 13;
    double kv = 1.2;                 // м³/ч
    
    // Ожидаемый результат:
    // Δp_Vent = (280 / 1000 / 1.2)² × 100000 × 1.053 ≈ 5729 Па
    
    // Act
    var result = calculator.CalculateAtTemperature(circuit, 40, glycolProps, innerDiameter, kv);
    
    // Assert
    Assert.Equal(5729, result.ValveLoss, 0); // ±0 Па
}
```

### 4.6. Тест: CalculateAtTemperature_ValveLoss_WrongDensityGivesWrongResult

```csharp
[Fact]
public void CalculateAtTemperature_ValveLoss_WrongDensityGivesWrongResult()
{
    // Arrange
    var circuit = new CircuitRow { CircuitLength = 100, SupplyLength = 20, FlowRate = 280 };
    var glycolProps = new GlycolProperties
    {
        Density = 1053,              // кг/м³ (БЕЗ конвертации будет ошибка!)
        KinematicViscosity = 2.16
    };
    double innerDiameter = 13;
    double kv = 1.2;
    
    // Если использовать плотность в кг/м³ без конвертации:
    // Δp_Vent_wrong = (0.233)² × 100000 × 1053 ≈ 5728272 Па
    // Это в 1000 раз больше правильного значения!
    
    // Act
    var result = calculator.CalculateAtTemperature(circuit, 40, glycolProps, innerDiameter, kv);
    
    // Assert
    Assert.NotEqual(5728272, result.ValveLoss); // НЕ должно быть 5728272!
    Assert.Equal(5729, result.ValveLoss, 0);     // Должно быть 5729
}
```

### 4.7. Тест: CalculateAtTemperature_Density_ConvertsKgPerM3ToGramsPerCm3

```csharp
[Fact]
public void CalculateAtTemperature_Density_ConvertsKgPerM3ToGramsPerCm3()
{
    // Arrange
    var circuit = new CircuitRow { CircuitLength = 100, SupplyLength = 20, FlowRate = 280 };
    var glycolProps = new GlycolProperties
    {
        Density = 1053,              // кг/м³
        KinematicViscosity = 2.16
    };
    double innerDiameter = 13;
    double kv = 1.2;
    
    // Act
    var result = calculator.CalculateAtTemperature(circuit, 40, glycolProps, innerDiameter, kv);
    
    // Assert
    Assert.Equal(1.053, result.Density, 3); // 1053 кг/м³ = 1.053 г/см³
}
```

---

## 5. Критерии приёмки

- [ ] Все 7 тестов проходят
- [ ] Тесты проверяют конвертацию единиц измерения
- [ ] Тесты проверяют граничные значения (ошибка ×1000)
- [ ] Тесты используют реальные данные из ТЗ
- [ ] Точность: ±1 для целых значений, ±0.001 для дробных

---

## 6. Зависимости

Эта задача должна выполняться **после** завершения задач:
- `task_units_1` — Исправить расход теплоносителя
- `task_units_2` — Исправить удельные потери давления
- `task_units_3` — Исправить потери в вентиле
- `task_units_4` — Исправить CircuitTemperatureResult.Density

---

## 7. Ссылки

- **ТЗ**: `Work/Hydraulics/technical_specification_units.md` (раздел 5)
- **Код**: `src/Services/Hydraulics/CircuitsCalculator.cs`
- **Тесты**: `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs`

---

*Дата создания: 2026-03-20*