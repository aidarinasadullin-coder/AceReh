# Техническое задание: Исправление ошибок в переводах единиц измерения

## 1. Общее описание

### 1.1. Краткое описание задачи
Исправление критических ошибок в модуле гидравлического расчёта, связанных с неправильными единицами измерения плотности и расхода теплоносителя.

### 1.2. Цель разработки
Обеспечить корректность расчётов гидравлических параметров (расход, потери давления) в соответствии с формулами из документации `docs/Formulas_Snegotayanie.md`.

### 1.3. Связь с существующей системой
Модуль гидравлического расчёта (`CircuitsCalculator`) использует свойства гликоля из `GlycolProperties`, где плотность хранится в кг/м³. Однако формулы расчёта требуют плотность в г/см³.

---

## 2. Анализ проблем

### 2.1. Контекст: единицы измерения плотности

**Источник данных**: `data/glycol_data.json` (ASHRAE Handbook)
- Плотность в базе данных: **кг/м³** (например, 1053 кг/м³ для 50% гликоля при 40°C)

**Формулы расчёта**: `docs/Formulas_Snegotayanie.md`
- Раздел 11.8 (удельные потери): ρ в **г/см³** (например, 1,053 г/см³)
- Раздел 11.10 (потери в вентиле): ρ в **г/см³**

**Примечание из документации** (строки 331-337):
```
ρ[г/см³] = ρ[кг/м³] / 1000
ρ[кг/м³] = ρ[г/см³] × 1000

Пример: ρ = 1053 кг/м³ = 1,053 г/см³
```

---

### 2.2. ОШИБКА #1: Расход теплоносителя

#### 2.2.1. Расположение
**Файл**: `src/Services/Hydraulics/CircuitsCalculator.cs`
**Строка**: 123

#### 2.2.2. Текущий код
```csharp
// V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)
// Результат в л/ч
double flowRate = power * 3.6 / (density * specificHeat * deltaT);
return flowRate;
```

#### 2.2.3. Проблема
Результат формулы — в **м³/ч**, но ожидается в **л/ч**.

#### 2.2.4. Формула из документации
**Раздел 11.4** (строки 388-407):
```
V_dot = Q_HK × 3,6 / (ρ × c_p × ΔT)    [л/ч]
```

**Пример расчёта**:
```
Дано: Q_HK = 5246 Вт, ρ = 1053 кг/м³, c_p = 3,21 кДж/(кг·К), ΔT = 10 К

V_dot = 5246 × 3,6 / (1053 × 3,21 × 10)
V_dot = 18886 / 33801
V_dot = 0,56 м³/ч = 560 л/ч
```

#### 2.2.5. Анализ
Формула `V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)` даёт результат в **м³/ч**:
- Q_HK в Вт = Дж/с
- 3.6 = 3600 с/ч × 0.001 (перевод Дж в кДж)
- ρ в кг/м³
- c_p в кДж/(кг·К)
- ΔT в К

Результат: (Дж/с × с/ч) / (кг/м³ × кДж/кг × К) = м³/ч

Для получения л/ч нужно умножить на 1000.

#### 2.2.6. Решение
```csharp
// V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)
// Результат в м³/ч, переводим в л/ч
double flowRate_m3h = power * 3.6 / (density * specificHeat * deltaT);
double flowRate_lh = flowRate_m3h * 1000;
return flowRate_lh;
```

Или одной строкой:
```csharp
return power * 3.6 / (density * specificHeat * deltaT) * 1000;
```

---

### 2.3. ОШИБКА #2: Удельные потери давления

#### 2.3.1. Расположение
**Файл**: `src/Services/Hydraulics/CircuitsCalculator.cs`
**Строки**: 201-202

#### 2.3.2. Текущий код
```csharp
// Удельные потери: R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100
double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * glycolProps.Density * frictionFactor
    / (2 * innerDiameter) * 100;
```

#### 2.3.3. Проблема
`glycolProps.Density` в **кг/м³**, но формула требует **г/см³**.

#### 2.3.4. Формула из документации
**Раздел 11.8** (строки 498-550):
```
R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100    [Па/м]
```

**Единицы измерения** (строка 506):
```
| Плотность | ρ | г/см³ | ~1,053 для 50% гликоля |
```

**Пример расчёта** (строки 531-550):
```
Дано: v = 0,59 м/с, ρ = 1,053 г/см³, λ = 0,042, d_inner = 13 мм

R = 10000 × (0,59² × 1,053 × 0,042) / (2 × 13) × 100
R = 10000 × 0,01539 / 26 × 100
R = 10000 × 0,000592 × 100
R = 592 Па/м ✓
```

#### 2.3.5. Анализ
Если подставить ρ = 1053 кг/м³ вместо 1,053 г/см³:
```
R = 10000 × (0,59² × 1053 × 0,042) / (2 × 13) × 100
R = 10000 × 15,39 / 26 × 100
R = 10000 × 0,592 × 100
R = 592000 Па/м  ← ОШИБКА! В 1000 раз больше!
```

#### 2.3.6. Решение
```csharp
// Удельные потери: R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100
// ρ должно быть в г/см³, glycolProps.Density в кг/м³
double density_g_cm3 = glycolProps.Density / 1000.0;
double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * density_g_cm3 * frictionFactor
    / (2 * innerDiameter) * 100;
```

---

### 2.4. ОШИБКА #3: Потери в вентиле

#### 2.4.1. Расположение
**Файл**: `src/Services/Hydraulics/CircuitsCalculator.cs`
**Строка**: 212

#### 2.4.2. Текущий код
```csharp
// Потери в вентиле: Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ
result.ValveLoss = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * glycolProps.Density;
```

#### 2.4.3. Проблема
`glycolProps.Density` в **кг/м³**, но формула требует **г/см³**.

#### 2.4.4. Формула из документации
**Раздел 11.10** (строки 567-627):
```
Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ    [Па]
```

**Единицы измерения** (строка 573):
```
| Плотность | ρ | г/см³ | ~1,053 для 50% гликоля |
```

**Пример расчёта** (строки 619-627):
```
Дано: V_dot = 280 л/ч, ρ = 1,053 г/см³, Kv = 1,2 м³/ч

Δp_Vent = (280 / 1000 / 1,2)² × 100000 × 1,053
Δp_Vent = (0,233)² × 100000 × 1,053
Δp_Vent = 0,0544 × 100000 × 1,053
Δp_Vent = 5729 Па = 5,73 кПа
```

#### 2.4.5. Анализ
Если подставить ρ = 1053 кг/м³ вместо 1,053 г/см³:
```
Δp_Vent = (0,233)² × 100000 × 1053
Δp_Vent = 0,0544 × 100000 × 1053
Δp_Vent = 5728272 Па = 5728 кПа  ← ОШИБКА! В 1000 раз больше!
```

#### 2.4.6. Решение
```csharp
// Потери в вентиле: Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ
// ρ должно быть в г/см³, glycolProps.Density в кг/м³
double density_g_cm3 = glycolProps.Density / 1000.0;
result.ValveLoss = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;
```

---

### 2.5. ОШИБКА #4: CircuitTemperatureResult.Density

#### 2.5.1. Расположение
**Файл**: `src/Models/Hydraulics/CircuitRow.cs`
**Строки**: 17-19 (модель), 179 (присвоение)

#### 2.5.2. Текущий код модели
```csharp
/// <summary>
/// Плотность теплоносителя, г/см³
/// </summary>
public double Density { get; set; }
```

#### 2.5.3. Текущий код присвоения
```csharp
var result = new CircuitTemperatureResult
{
    Temperature = temperature,
    Density = glycolProps.Density,  // ← кг/м³ вместо г/см³!
    KinematicViscosity = glycolProps.KinematicViscosity
};
```

#### 2.5.4. Проблема
В модели `CircuitTemperatureResult` ожидается плотность в **г/см³**, но передаётся значение в **кг/м³**.

#### 2.5.5. Решение
```csharp
var result = new CircuitTemperatureResult
{
    Temperature = temperature,
    Density = glycolProps.Density / 1000.0,  // Конвертация кг/м³ → г/см³
    KinematicViscosity = glycolProps.KinematicViscosity
};
```

---

## 3. Список юзер-кейсов

### UC-1: Расчёт расхода теплоносителя

#### 3.1.1. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 3.1.2. Предусловия
- Открыт экран "Гидравлический расчёт"
- Введены параметры контура (длина, площадь)
- Выбран теплоноситель (гликоль)
- Выполнен тепловой расчёт

#### 3.1.3. Основной сценарий
1. Пользователь нажимает кнопку "Рассчитать"
2. Система получает свойства гликоля из `GlycolDataService`
3. Система рассчитывает мощность контура Q_HK
4. Система рассчитывает расход V_dot по формуле:
   ```
   V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT) × 1000
   ```
5. **Система возвращает расход в л/ч** (исправлено)
6. UI отображает расход в л/ч

#### 3.1.4. Критерии приёмки
- ✅ При Q_HK = 5246 Вт, ρ = 1053 кг/м³, c_p = 3.21 кДж/(кг·К), ΔT = 10 К
- ✅ Результат: V_dot ≈ 560 л/ч (не 0.56 л/ч и не 560000 л/ч)

---

### UC-2: Расчёт удельных потерь давления

#### 3.2.1. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 3.2.2. Предусловия
- Открыт экран "Гидравлический расчёт"
- Рассчитан расход теплоносителя
- Выбрана труба РЕХАУ

#### 3.2.3. Основной сценарий
1. Система рассчитывает скорость потока v
2. Система рассчитывает число Рейнольдса Re
3. Система определяет режим течения и коэффициент трения λ
4. Система рассчитывает удельные потери R по формуле:
   ```
   R = 10000 × (v² × ρ[г/см³] × λ) / (2 × d_inner) × 100
   ```
5. **Система использует плотность в г/см³** (исправлено)
6. UI отображает потери в Па/м

#### 3.2.4. Критерии приёмки
- ✅ При v = 0.59 м/с, ρ = 1053 кг/м³, λ = 0.042, d = 13 мм
- ✅ Результат: R ≈ 592 Па/м (не 592000 Па/м)

---

### UC-3: Расчёт потерь в вентиле

#### 3.3.1. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 3.3.2. Предусловия
- Открыт экран "Гидравлический расчёт"
- Рассчитан расход теплоносителя
- Выбран тип вентиля (HKV-D, IV 1¼", IV 1½")

#### 3.3.3. Основной сценарий
1. Система получает Kv для выбранного вентиля
2. Система рассчитывает потери в вентиле по формуле:
   ```
   Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ[г/см³]
   ```
3. **Система использует плотность в г/см³** (исправлено)
4. UI отображает потери в Па

#### 3.3.4. Критерии приёмки
- ✅ При V_dot = 280 л/ч, ρ = 1053 кг/м³, Kv = 1.2 м³/ч
- ✅ Результат: Δp_Vent ≈ 5729 Па (не 5728272 Па)

---

### UC-4: Сохранение плотности в результатах

#### 3.4.1. Актёры
- Система (Калькулятор РЕХАУ)

#### 3.4.2. Предусловия
- Выполнен гидравлический расчёт

#### 3.4.3. Основной сценарий
1. Система создаёт объект `CircuitTemperatureResult`
2. **Система конвертирует плотность: кг/м³ → г/см³** (исправлено)
3. Система сохраняет результат в `CircuitRow.OperatingResult`
4. Свойство `Density` содержит значение в г/см³

#### 3.4.4. Критерии приёмки
- ✅ При ρ = 1053 кг/м³ в GlycolProperties
- ✅ CircuitTemperatureResult.Density = 1.053 г/см³

---

## 4. Технические требования

### 4.1. Исправление CalculateFlowRate

**Файл**: `src/Services/Hydraulics/CircuitsCalculator.cs`
**Метод**: `CalculateFlowRate`
**Строки**: 107-126

**Текущий код**:
```csharp
public double CalculateFlowRate(double power, double deltaT, double density, double specificHeat)
{
    // ... валидация ...
    
    // V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)
    // Результат в л/ч
    double flowRate = power * 3.6 / (density * specificHeat * deltaT);
    
    return flowRate;
}
```

**Требуемый код**:
```csharp
public double CalculateFlowRate(double power, double deltaT, double density, double specificHeat)
{
    // ... валидация ...
    
    // V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)
    // Результат в м³/ч, переводим в л/ч
    double flowRate_m3h = power * 3.6 / (density * specificHeat * deltaT);
    double flowRate_lh = flowRate_m3h * 1000;
    
    return flowRate_lh;
}
```

**Обновить XML-комментарий**:
```csharp
/// <summary>
/// Рассчитать расход теплоносителя V_dot
/// </summary>
/// <param name="power">Мощность контура, Вт</param>
/// <param name="deltaT">Температурный перепад, К</param>
/// <param name="density">Плотность теплоносителя, кг/м³</param>
/// <param name="specificHeat">Удельная теплоёмкость, кДж/(кг·К)</param>
/// <returns>Расход, л/ч</returns>
/// <remarks>
/// Формула: V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT) × 1000
/// 
/// Где:
/// - Q_HK — мощность контура, Вт
/// - ρ — плотность теплоносителя, кг/м³
/// - c_p — удельная теплоёмкость, кДж/(кг·К)
/// - ΔT — температурный перепад, К
/// - 3.6 — коэффициент перевода Вт в кДж/ч
/// - 1000 — коэффициент перевода м³/ч в л/ч
/// 
/// Примечание: Формула даёт результат в м³/ч, умножение на 1000 переводит в л/ч.
/// 
/// Пример:
/// Q_HK = 5246 Вт, ρ = 1053 кг/м³, c_p = 3.21 кДж/(кг·К), ΔT = 10 К
/// V_dot = 5246 × 3.6 / (1053 × 3.21 × 10) × 1000 = 560 л/ч
/// </remarks>
```

---

### 4.2. Исправление CalculateAtTemperature (удельные потери)

**Файл**: `src/Services/Hydraulics/CircuitsCalculator.cs`
**Метод**: `CalculateAtTemperature`
**Строки**: 200-203

**Текущий код**:
```csharp
// Удельные потери: R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100
double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * glycolProps.Density * frictionFactor
    / (2 * innerDiameter) * 100;
```

**Требуемый код**:
```csharp
// Удельные потери: R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100
// Важно: ρ должно быть в г/см³, glycolProps.Density в кг/м³
double density_g_cm3 = glycolProps.Density / 1000.0;
double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * density_g_cm3 * frictionFactor
    / (2 * innerDiameter) * 100;
```

**Обновить XML-комментарий** (строки 143-156):
```csharp
/// <remarks>
/// Рассчитывает:
/// - Скорость потока v
/// - Число Рейнольдса Re
/// - Режим течения (ламинарный/переходный/турбулентный)
/// - Коэффициент трения λ
/// - Удельные потери R (Па/м)
/// - Потери в трубе контура Δp_HK (Па)
/// - Потери в трубе подводки Δp_Zul (Па)
/// - Потери в вентиле Δp_Vent (Па)
/// - Суммарные потери Δp_total (Па)
/// 
/// Формулы:
/// - Скорость: v = V_dot × 4000 / (3600 × π × d_inner²)
/// - Число Рейнольдса: Re = 1000 × v × d_inner / ν
/// - Коэффициент трения: зависит от режима (Пуазейль или Колбрук-Уайт)
/// - Удельные потери: R = 10000 × (v² × ρ[г/см³] × λ) / (2 × d_inner) × 100
/// - Потери в трубе: Δp = R × L
/// - Потери в вентиле: Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ[г/см³]
/// 
/// Важно: Плотность ρ в формулах R и Δp_Vent должна быть в г/см³!
/// GlycolProperties.Density хранит плотность в кг/м³, требуется конвертация.
/// </remarks>
```

---

### 4.3. Исправление CalculateAtTemperature (потери в вентиле)

**Файл**: `src/Services/Hydraulics/CircuitsCalculator.cs`
**Метод**: `CalculateAtTemperature`
**Строка**: 212

**Текущий код**:
```csharp
// Потери в вентиле: Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ
result.ValveLoss = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * glycolProps.Density;
```

**Требуемый код**:
```csharp
// Потери в вентиле: Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ
// Важно: ρ должно быть в г/см³, glycolProps.Density в кг/м³
double density_g_cm3 = glycolProps.Density / 1000.0;
result.ValveLoss = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;
```

**Примечание**: Переменную `density_g_cm3` можно объявить один раз в начале метода и использовать для обоих расчётов.

---

### 4.4. Исправление присвоения Density в CircuitTemperatureResult

**Файл**: `src/Services/Hydraulics/CircuitsCalculator.cs`
**Метод**: `CalculateAtTemperature`
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

---

### 4.5. Обновление документации модели CircuitTemperatureResult

**Файл**: `src/Models/Hydraulics/CircuitRow.cs`
**Строки**: 17-19

**Текущий код**:
```csharp
/// <summary>
/// Плотность теплоносителя, г/см³
/// </summary>
public double Density { get; set; }
```

**Требуемый код** (без изменений, но с уточнением комментария):
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

## 5. Unit-тесты

### 5.1. Тест CalculateFlowRate

**Файл**: `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs`

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

---

### 5.2. Тест удельных потерь давления

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

---

### 5.3. Тест потерь в вентиле

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

---

### 5.4. Тест CircuitTemperatureResult.Density

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

## 6. Нефункциональные требования

### 6.1. Производительность
- Расчёт одного контура < 10 мс
- Расчёт 48 контуров < 500 мс

### 6.2. Точность
- Относительная погрешность < 1% по сравнению с Excel-расчётом
- Абсолютная погрешность < 1 Па для потерь давления

### 6.3. Тестируемость
- Unit-тесты для всех исправленных методов
- Тесты с граничными значениями
- Тесты с реальными данными из Excel

---

## 7. Ограничения и допущения

### 7.1. Технические ограничения
- .NET 8, C# 12
- Данные гликоля из `data/glycol_data.json` (ASHRAE)
- Плотность в базе данных всегда в кг/м³

### 7.2. Бизнес-ограничения
- Соответствие формулам из `docs/Formulas_Snegotayanie.md`
- Совместимость с Excel-расчётом РЕХАУ

### 7.3. Допущения
- Плотность гликоля не зависит от давления (только от температуры)
- Концентрация гликоля постоянна по контуру

---

## 8. План исправления

### 8.1. Приоритет 1 (Критично)
1. **Исправить CalculateFlowRate** — добавить умножение на 1000
2. **Исправить удельные потери** — конвертировать плотность в г/см³
3. **Исправить потери в вентиле** — конвертировать плотность в г/см³
4. **Исправить присвоение Density** — конвертировать кг/м³ в г/см³

### 8.2. Приоритет 2 (Важно)
5. **Обновить XML-комментарии** — документировать единицы измерения
6. **Добавить Unit-тесты** — проверить корректность расчётов

### 8.3. Приоритет 3 (Рекомендуется)
7. **Добавить интеграционные тесты** — сравнить с Excel-расчётом
8. **Обновить документацию** — добавить примеры расчётов

---

## 9. Файлы для изменения

| Файл | Изменение | Приоритет |
|------|-----------|-----------|
| `src/Services/Hydraulics/CircuitsCalculator.cs` | Исправить `CalculateFlowRate` | P1 |
| `src/Services/Hydraulics/CircuitsCalculator.cs` | Исправить `CalculateAtTemperature` (удельные потери) | P1 |
| `src/Services/Hydraulics/CircuitsCalculator.cs` | Исправить `CalculateAtTemperature` (потери в вентиле) | P1 |
| `src/Services/Hydraulics/CircuitsCalculator.cs` | Исправить присвоение `Density` | P1 |
| `src/Models/Hydraulics/CircuitRow.cs` | Обновить комментарий `Density` | P2 |
| `tests/.../CircuitsCalculatorTests.cs` | Добавить тесты | P2 |

---

## 10. Критерии приёмки

### 10.1. Функциональные критерии
- ✅ Расход теплоносителя в л/ч (не м³/ч)
- ✅ Удельные потери давления в Па/м (не кПа/м)
- ✅ Потери в вентиле в Па (не кПа)
- ✅ Density в CircuitTemperatureResult в г/см³ (не кг/м³)

### 10.2. Точность расчётов
- ✅ Расход: V_dot = 560 л/ч при Q_HK = 5246 Вт, ρ = 1053 кг/м³, c_p = 3.21 кДж/(кг·К), ΔT = 10 К
- ✅ Удельные потери: R = 592 Па/м при v = 0.59 м/с, ρ = 1053 кг/м³, λ = 0.042, d = 13 мм
- ✅ Потери в вентиле: Δp_Vent = 5729 Па при V_dot = 280 л/ч, ρ = 1053 кг/м³, Kv = 1.2 м³/ч

### 10.3. Unit-тесты
- ✅ Все тесты проходят
- ✅ Тесты проверяют конвертацию единиц
- ✅ Тесты проверяют граничные значения

---

## 11. Ссылки

- `docs/Formulas_Snegotayanie.md` — формулы расчёта (разделы 11.4, 11.8, 11.10)
- `src/Services/Hydraulics/CircuitsCalculator.cs` — калькулятор контуров
- `src/Models/Hydraulics/GlycolProperties.cs` — свойства гликоля
- `src/Models/Hydraulics/CircuitRow.cs` — модель контура
- `data/glycol_data.json` — база данных гликоля (ASHRAE)

---

## 12. Примеры расчётов для верификации

### 12.1. Расход теплоносителя
```
Входные данные:
- Q_HK = 5246 Вт
- ρ = 1053 кг/м³
- c_p = 3.21 кДж/(кг·К)
- ΔT = 10 К

Расчёт:
V_dot = 5246 × 3.6 / (1053 × 3.21 × 10) × 1000
V_dot = 18886 / 33801 × 1000
V_dot = 0.558 × 1000
V_dot = 558 л/ч ≈ 560 л/ч

Ожидаемый результат: 560 л/ч
```

### 12.2. Удельные потери давления
```
Входные данные:
- v = 0.59 м/с
- ρ = 1053 кг/м³ = 1.053 г/см³
- λ = 0.042
- d_inner = 13 мм

Расчёт:
R = 10000 × (0.59² × 1.053 × 0.042) / (2 × 13) × 100
R = 10000 × (0.3481 × 1.053 × 0.042) / 26 × 100
R = 10000 × 0.01539 / 26 × 100
R = 10000 × 0.000592 × 100
R = 592 Па/м

Ожидаемый результат: 592 Па/м
```

### 12.3. Потери в вентиле
```
Входные данные:
- V_dot = 280 л/ч
- ρ = 1053 кг/м³ = 1.053 г/см³
- Kv = 1.2 м³/ч

Расчёт:
Δp_Vent = (280 / 1000 / 1.2)² × 100000 × 1.053
Δp_Vent = (0.233)² × 100000 × 1.053
Δp_Vent = 0.0544 × 100000 × 1.053
Δp_Vent = 5729 Па

Ожидаемый результат: 5729 Па
```

---

*Дата создания: 2026-03-20*
*Источник: docs/Formulas_Snegotayanie.md*