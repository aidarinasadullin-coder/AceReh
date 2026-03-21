# Отчёт о тестировании задачи: Исправление удельных потерь давления

## Метаданные

| Параметр | Значение |
|----------|----------|
| **ID задачи** | task_units_2 |
| **Дата** | 2026-03-20 |
| **Статус** | ✅ Выполнено |

---

## 1. Выполненные изменения

### 1.1. Файл: `src/Services/Hydraulics/CircuitsCalculator.cs`

#### Изменение 1: XML-комментарий (строки 137-156)
- **Было**: Комментарий без указания единиц измерения плотности
- **Стало**: Добавлено примечание о конвертации плотности:
  ```
  Важно: Плотность ρ в формулах R и Δp_Vent должна быть в г/см³!
  GlycolProperties.Density хранит плотность в кг/м³, требуется конвертация.
  ```

#### Изменение 2: Удельные потери давления (строки 203-208)
- **Было**:
  ```csharp
  double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * glycolProps.Density * frictionFactor
      / (2 * innerDiameter) * 100;
  ```
- **Стало**:
  ```csharp
  double density_g_cm3 = glycolProps.Density / 1000.0;
  double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * density_g_cm3 * frictionFactor
      / (2 * innerDiameter) * 100;
  ```

#### Изменение 3: Потери в вентиле (строки 216-218)
- **Было**:
  ```csharp
  result.ValveLoss = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * glycolProps.Density;
  ```
- **Стало**:
  ```csharp
  result.ValveLoss = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;
  ```

#### Изменение 4: CircuitTemperatureResult.Density (строка 182)
- **Было**:
  ```csharp
  Density = glycolProps.Density,
  ```
- **Стало**:
  ```csharp
  Density = glycolProps.Density / 1000.0,  // Конвертация: кг/м³ → г/см³
  ```

### 1.2. Файл: `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs`

#### Добавлены тесты:
1. `CalculateAtTemperature_PressureLossPerMeter_UsesDensityInGramsPerCm3` — проверка удельных потерь
2. `CalculateAtTemperature_ValveLoss_UsesDensityInGramsPerCm3` — проверка потерь в вентиле

---

## 2. Результаты тестирования

### 2.1. Новые тесты

| Тест | Статус | Описание |
|------|--------|----------|
| `CalculateAtTemperature_PressureLossPerMeter_UsesDensityInGramsPerCm3` | ✅ PASSED | Проверяет, что удельные потери ≈ 592 Па/м (не 592000 Па/м) |
| `CalculateAtTemperature_ValveLoss_UsesDensityInGramsPerCm3` | ✅ PASSED | Проверяет, что потери в вентиле ≈ 5729 Па (не 5728272 Па) |

### 2.2. Регрессионные тесты

| Категория | Всего | Пройдено | Провалено |
|-----------|-------|----------|-----------|
| CircuitsCalculatorTests | 34 | 34 | 0 |

### 2.3. Детали тестов

#### Тест: CalculateAtTemperature_PressureLossPerMeter_UsesDensityInGramsPerCm3

**Входные данные**:
- FlowRate = 280 л/ч
- Density = 1053 кг/м³
- KinematicViscosity = 2.16 мм²/с
- InnerDiameter = 13 мм
- Kv = 1.2 м³/ч

**Ожидаемый результат**: PressureLossPerMeter ≈ 592 Па/м

**Фактический результат**: ✅ PASSED (в пределах допуска ±50 Па/м)

**Проверка**: Результат < 1000 Па/м (не в 1000 раз больше) ✅

#### Тест: CalculateAtTemperature_ValveLoss_UsesDensityInGramsPerCm3

**Входные данные**:
- FlowRate = 280 л/ч
- Density = 1053 кг/м³
- Kv = 1.2 м³/ч

**Ожидаемый результат**: ValveLoss ≈ 5729 Па

**Фактический результат**: ✅ PASSED (в пределах допуска ±100 Па)

**Проверка**: Результат < 10000 Па (не в 1000 раз больше) ✅

---

## 3. Проверка критериев приёмки

| Критерий | Статус | Комментарий |
|----------|--------|-------------|
| Удельные потери рассчитываются с плотностью в г/см³ | ✅ | Конвертация: `density_g_cm3 = glycolProps.Density / 1000.0` |
| Результат соответствует формуле | ✅ | R = 10000 × (v² × ρ[г/см³] × λ) / (2 × d_inner) × 100 |
| При v = 0.59 м/с, ρ = 1053 кг/м³, λ = 0.042, d = 13 мм | ✅ | Результат ≈ 592 Па/м (не 592000 Па/м) |
| XML-комментарий обновлён | ✅ | Добавлено примечание о единицах измерения |
| Unit-тесты проходят | ✅ | 34/34 тестов CircuitsCalculatorTests |

---

## 4. Формула расчёта

### Удельные потери давления:
```
R = 10000 × (v² × ρ[г/см³] × λ) / (2 × d_inner) × 100    [Па/м]
```

**Пример расчёта**:
```
Дано: v = 0.59 м/с, ρ = 1053 кг/м³ = 1.053 г/см³, λ = 0.042, d_inner = 13 мм

R = 10000 × (0.59² × 1.053 × 0.042) / (2 × 13) × 100
R = 10000 × (0.3481 × 1.053 × 0.042) / 26 × 100
R = 10000 × 0.01539 / 26 × 100
R = 10000 × 0.000592 × 100
R = 592 Па/м ✓
```

### Потери в вентиле:
```
Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ[г/см³]    [Па]
```

**Пример расчёта**:
```
Дано: V_dot = 280 л/ч, ρ = 1053 кг/м³ = 1.053 г/см³, Kv = 1.2 м³/ч

Δp_Vent = (280 / 1000 / 1.2)² × 100000 × 1.053
Δp_Vent = (0.233)² × 100000 × 1.053
Δp_Vent = 0.0544 × 100000 × 1.053
Δp_Vent = 5729 Па ✓
```

---

## 5. Итог

✅ **Задача выполнена успешно**

- Все исправления применены корректно
- Конвертация плотности из кг/м³ в г/см³ реализована
- XML-комментарии обновлены
- Unit-тесты проходят (34/34)
- Критерии приёмки выполнены

---

*Дата создания: 2026-03-20*