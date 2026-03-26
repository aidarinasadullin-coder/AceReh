# Техническое задание: Исправление модуля гидравлики

**Дата:** 2026-03-22  
**Статус:** На рассмотрении  
**Приоритет:** Критический  
**Модуль:** Hydraulics  

---

## 1. Общее описание

### 1.1. Краткое описание задачи

Исправление критических ошибок в расчётах гидравлики, выявленных при сравнении с эталонным Excel-файлом (gidravlica.xls). Ошибки приводят к некорректным результатам балансировки контуров и неправильному подбору настроек балансировочных клапанов.

### 1.2. Цель разработки

Привести расчёты гидравлики в полное соответствие с эталонным Excel-файлом РЕХАУ, обеспечив корректность:
- Максимальных оборотов балансировочных клапанов
- Формул потерь давления (DpVerteiler, DpVent)
- Алгоритма балансировки контуров
- Единиц измерения давления

### 1.3. Связь с существующей системой

Исправления затрагивают:
- `src/Services/Hydraulics/ValveTurnsCalculator.cs` — максимальные обороты
- `src/Services/Hydraulics/CircuitsCalculator.cs` — формулы и балансировка
- `src/Models/Hydraulics/CircuitRow.cs` — модель данных
- `src/Views/Hydraulics/CircuitsView.xaml` — отображение колонок

---

## 2. Список юзер-кейсов

### UC-1: Расчёт максимальных оборотов клапана

#### 2.1. Название
Расчёт максимальных оборотов балансировочного клапана по типу

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
- Выбран тип коллектора (HKV-D или IV)
- Инициирован расчёт гидравлики

#### 2.4. Основной сценарий
1. Система определяет тип коллектора (ValveType)
2. Система вызывает метод `GetMaxTurns(ValveType)`
3. Для HKV-D система возвращает **2.5** оборота
4. Для IV 1¼" система возвращает **8.0** оборотов
5. Для IV 1½" система возвращает **8.0** оборотов
6. Референсный контур получает максимальные обороты

#### 2.5. Альтернативные сценарии
- **A1: Неподдерживаемый тип клапана** — система выбрасывает исключение `ArgumentException`

#### 2.6. Постусловия
- Референсный контур имеет максимальные обороты для данного типа клапана
- Остальные контуры имеют обороты ≤ максимальных

#### 2.7. Критерии приёмки
- ✅ `GetMaxTurns(ValveType.HKV_D)` возвращает 2.5
- ✅ `GetMaxTurns(ValveType.IV_1_25)` возвращает 8.0
- ✅ `GetMaxTurns(ValveType.IV_1_5)` возвращает 8.0
- ✅ Референсный контур получает максимальные обороты (не 0!)

---

### UC-2: Расчёт потерь давления DpVerteiler и DpVent

#### 2.1. Название
Расчёт потерь в распределителе и вентиле с учётом типа коллектора

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
- Введены параметры контура (длина, подводка, расход)
- Выбран тип коллектора (HKV-D или IV)
- Получены свойства гликоля (плотность, вязкость)

#### 2.4. Основной сценарий для IV 1¼" / IV 1½"

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

#### 2.5. Альтернативный сценарий для HKV-D

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

#### 2.6. Постусловия
- DpVerteiler рассчитан корректно
- DpVent рассчитан корректно
- DpGesamt = DpRohr + DpVerteiler + DpVent

#### 2.7. Критерии приёмки
- ✅ Для IV: DpVerteiler = 15000 × (ρ/2000) × v²
- ✅ Для IV: DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
- ✅ Для HKV-D: DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
- ✅ Для HKV-D: DpVent = 15000 × (ρ/2000) × v²
- ✅ Значения совпадают с Excel (±1%)

---

### UC-3: Балансировка контуров

#### 2.1. Название
Балансировка контуров на коллекторе

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
- Рассчитаны все контуры коллектора
- Рассчитаны DpGesamt для каждого контура

#### 2.4. Основной сценарий

1. Система находит контур с **максимальным DpGesamt**
2. Система помечает этот контур как **референсный**
3. **Референсный контур получает МАКСИМАЛЬНЫЕ обороты:**
   - HKV-D: 2.5 оборота
   - IV: 8.0 оборотов
4. Для каждого контура система рассчитывает zu_drosseln:
   ```
   zu_drosseln = DpGesamt_max - DpGesamt_контур
   ```
5. Для нереференсных контуров система рассчитывает Kv для дросселирования:
   ```
   Kv = V_dot / 1000 / √(zu_drosseln / 100000 / ρ)
   ```
6. Система рассчитывает обороты по формуле umdreh1(Kv, type)

#### 2.5. Альтернативные сценарии
- **A1: Все контуры с одинаковыми потерями** — все получают максимальные обороты
- **A2: Превышение максимальных оборотов** — система выдаёт предупреждение

#### 2.6. Постусловия
- Референсный контур имеет максимальные обороты
- zu_drosseln ≥ 0 для всех контуров
- Обороты ≤ максимальных для типа клапана

#### 2.7. Критерии приёмки
- ✅ Референсный контур = контур с MAX(DpGesamt)
- ✅ Референсный контур имеет максимальные обороты (не 0!)
- ✅ zu_drosseln = DpGesamt_max - DpGesamt_контур
- ✅ Обороты рассчитаны по формулам umdreh1

---

### UC-4: Отображение результатов в таблице

#### 2.1. Название
Отображение результатов гидравлического расчёта в таблице контуров

#### 2.2. Актёры
- Пользователь (инженер-проектировщик)
- Система (Калькулятор РЕХАУ)

#### 2.3. Предусловия
- Выполнен расчёт гидравлики
- Открыта вкладка "Гидравлика"

#### 2.4. Основной сценарий

1. Система отображает таблицу контуров
2. **Таблица содержит колонки:**
   - № (номер контура)
   - Длина (м)
   - Подводка (м)
   - Площадь (м²)
   - Шаг (см)
   - Мощность (Вт)
   - Расход (л/ч)
   - Скорость (м/с)
   - Re (число Рейнольдса)
   - λ (коэффициент трения)
   - Режим
   - Уд.потери (Па/м)
   - **DpRohr (Па)** — потери в трубе
   - **DpVerteiler (Па)** — потери в распределителе
   - **DpVent (Па)** — потери в вентиле
   - **DpGesamt (Па)** — суммарные потери
   - **zu_drosseln (Па)** — дросселирование
   - **Обороты** — настройка клапана (дробь)
3. Все значения давления отображаются в **Паскалях (целые числа)**
4. Обороты отображаются в виде **дробей** (2/4, 2 1/2)

#### 2.5. Альтернативные сценарии
- **A1: Режим отображения "Расчётная температура"** — отображаются результаты при расчётной температуре

#### 2.6. Постусловия
- Пользователь видит корректные значения
- Единицы измерения соответствуют Excel

#### 2.7. Критерии приёмки
- ✅ DpRohr отображается в Па (целые числа)
- ✅ DpVerteiler отображается в Па (целые числа)
- ✅ DpVent отображается в Па (целые числа)
- ✅ DpGesamt отображается в Па (целые числа)
- ✅ zu_drosseln отображается в Па (целые числа)
- ✅ Обороты отображаются как дроби (2/4, 2 1/2)
- ✅ Значения совпадают с Excel (±1%)

---

## 3. Нефункциональные требования

### 3.1. Производительность
- Расчёт 48 контуров должен выполняться менее чем за 1 секунду
- Обновление UI при изменении параметров — менее 100 мс

### 3.2. Точность расчётов
- Отклонение от Excel-эталона не более ±1%
- Все промежуточные значения должны совпадать с Excel

### 3.3. Совместимость
- Windows 10+
- .NET 8
- Разрешение экрана от 1366×768

### 3.4. Локализация
- Русский язык (основной)
- Возможность добавления EN/DE в будущем

---

## 4. Ограничения и допущения

### 4.1. Технические ограничения
- Язык: C# .NET 8
- Фреймворк: WPF, MVVM
- База данных: SQLite (локальная)

### 4.2. Бизнес-ограничения
- Соответствие методике РЕХАУ
- Соответствие Excel-файлу gidravlica.xls
- Максимальные обороты HKV-D: 2.5 (не 8.0!)

### 4.3. Допущения
- Плотность гликоля из базы данных ASHRAE
- Kv для HKV-D = 1.2 м³/ч
- Kv для IV 1¼" = 1.45 м³/ч
- Kv для IV 1½" = 1.5 м³/ч

---

## 5. Детальные спецификации

### 5.1. Изменения в ValveTurnsCalculator.cs

#### 5.1.1. Добавить метод GetMaxTurns

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

#### 5.1.2. Изменить проверку MaxTurns

В методе `CalculateTurnsWithWarning` заменить:
```csharp
// Было:
if (turns > MaxTurns)  // MaxTurns = 8.0 для всех

// Должно быть:
double maxTurns = GetMaxTurns(valveType);
if (turns > maxTurns)
{
    warning = $"Расчётные обороты ({turns:F2}) превышают максимум ({maxTurns}). Установлено {maxTurns} оборотов.";
    turns = maxTurns;
}
```

---

### 5.2. Изменения в CircuitTemperatureResult

#### 5.2.1. Добавить новые свойства

```csharp
/// <summary>
/// Потери в трубе контура, Па (DpRohr)
/// </summary>
/// <remarks>
/// Формула: DpRohr = L_hk × R (длина контура × удельные потери)
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

#### 5.2.2. Удалить/пометить как устаревшие

```csharp
[Obsolete("Использовать DpRohr вместо CircuitPipeLoss")]
public double CircuitPipeLoss { get; set; }

[Obsolete("Использовать DpVerteiler вместо ValveLoss для HKV-D")]
public double ValveLoss { get; set; }
```

---

### 5.3. Изменения в CircuitsCalculator.cs

#### 5.3.1. Метод CalculateAtTemperature

Добавить расчёт DpVerteiler и DpVent с учётом типа коллектора:

```csharp
public CircuitTemperatureResult CalculateAtTemperature(
    CircuitRow circuit,
    double temperature,
    GlycolProperties glycolProps,
    double innerDiameter,
    double kv,
    ValveType valveType)  // Добавить параметр
{
    // ... существующий код ...
    
    // DpRohr = потери в трубе контура + подводки
    double dpRohr = (circuit.CircuitLength + circuit.SupplyLength) * pressureLossPerMeter;
    result.DpRohr = dpRohr;
    
    // Плотность в г/см³
    double density_g_cm3 = glycolProps.Density / 1000.0;
    
    // Скорость
    double velocity = circuit.FlowRate * 4000 / (3600 * Math.PI * Math.Pow(innerDiameter, 2));
    
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
    
    return result;
}
```

#### 5.3.2. Метод CalculateBalancing

Изменить алгоритм балансировки:

```csharp
public List<CircuitRow> CalculateBalancing(List<CircuitRow> circuits, ValveType valveType)
{
    // ... фильтрация активных контуров ...
    
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
    
    return circuits;
}
```

---

### 5.4. Изменения в CircuitsView.xaml

#### 5.4.1. Заменить колонки таблицы

Заменить:
```xml
<!-- Было: -->
<DataGridTextColumn Header="Δp контур (мбар)"
                    Binding="{Binding CurrentResult.CircuitPipeLoss_mbar, ...}"/>
<DataGridTextColumn Header="Δp клапан (мбар)"
                    Binding="{Binding CurrentResult.ValveLoss_mbar, ...}"/>
<DataGridTextColumn Header="Δp сумма (мбар)"
                    Binding="{Binding CurrentResult.TotalLoss_mbar, ...}"/>
```

На:
```xml
<!-- Должно быть: -->
<DataGridTextColumn Header="DpRohr (Па)"
                    Binding="{Binding CurrentResult.DpRohr, Mode=OneWay, StringFormat=F0}"
                    IsReadOnly="True" Width="90"/>
                    
<DataGridTextColumn Header="DpVerteiler (Па)"
                    Binding="{Binding CurrentResult.DpVerteiler, Mode=OneWay, StringFormat=F0}"
                    IsReadOnly="True" Width="100"/>
                    
<DataGridTextColumn Header="DpVent (Па)"
                    Binding="{Binding CurrentResult.DpVent, Mode=OneWay, StringFormat=F0}"
                    IsReadOnly="True" Width="90"/>
                    
<DataGridTextColumn Header="DpGesamt (Па)"
                    Binding="{Binding CurrentResult.DpGesamt, Mode=OneWay, StringFormat=F0}"
                    IsReadOnly="True" Width="100"/>
                    
<DataGridTextColumn Header="zu_drosseln (Па)"
                    Binding="{Binding Throttling, Mode=OneWay, StringFormat=F0}"
                    IsReadOnly="True" Width="100"/>
```

---

## 6. Тестовые сценарии

### 6.1. Тест максимальных оборотов

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

### 6.2. Тест формул DpVerteiler/DpVent

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
```

### 6.3. Тест балансировки

```csharp
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

## 7. Открытые вопросы

### 7.1. Вопрос о единицах измерения в UI

**Вопрос:** Должны ли мы полностью убрать отображение в мбар или оставить оба варианта (Па и мбар)?

**Рекомендация:** Полностью перейти на Па для соответствия Excel. Мбар можно оставить только в итоговой сводке коллектора для совместимости с европейскими стандартами.

### 7.2. Вопрос о конвертере ValveTurnsToFractionConverter

**Вопрос:** Текущий конвертер отображает обороты как дроби (2/4, 2 1/2). Нужно ли его изменять?

**Рекомендация:** Оставить как есть, но убедиться, что максимальные обороты для HKV-D (2.5) корректно отображаются как "2 1/2".

---

## 8. Файлы для изменения

| Файл | Изменения |
|------|-----------|
| `src/Services/Hydraulics/ValveTurnsCalculator.cs` | Добавить `GetMaxTurns()`, изменить проверку MaxTurns |
| `src/Services/Hydraulics/CircuitsCalculator.cs` | Добавить параметр `ValveType` в `CalculateAtTemperature`, изменить формулы, изменить балансировку |
| `src/Models/Hydraulics/CircuitRow.cs` | Добавить свойства `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt`, `ZuDrosseln` |
| `src/Views/Hydraulics/CircuitsView.xaml` | Заменить колонки таблицы |
| `tests/.../ValveTurnsCalculatorTests.cs` | Добавить тесты `GetMaxTurns` |
| `tests/.../CircuitsCalculatorTests.cs` | Добавить тесты формул и балансировки |

---

## 9. Порядок выполнения

1. **Задача 1:** Добавить `GetMaxTurns()` в `ValveTurnsCalculator`
2. **Задача 2:** Добавить свойства в `CircuitTemperatureResult`
3. **Задача 3:** Изменить формулы в `CircuitsCalculator`
4. **Задача 4:** Изменить алгоритм балансировки
5. **Задача 5:** Обновить UI (колонки таблицы)
6. **Задача 6:** Написать тесты
7. **Задача 7:** Провести валидацию по Excel

---

## 10. Ожидаемый результат

После исправлений программа должна показывать те же значения, что и Excel:

| Контур | DpRohr (Па) | DpVerteiler (Па) | DpVent (Па) | DpGesamt (Па) | zu_drosseln (Па) | Обороты |
|--------|-------------|------------------|-------------|---------------|-----------------|---------|
| 1 | 467 | 61 | 202 | 730 | 1069 | 2/4 |
| 2 | 582 | 73 | 244 | 899 | 900 | 2/4 |
| 3 | 861 | 102 | 339 | 1303 | 496 | 3/4 |
| 4 | 1212 | 136 | 450 | 1798 | 0 | 2 1/2 |

**Примечание:** Значения приблизительные, точные значения из Excel.

---

*ТЗ создано: 2026-03-22*