# Task 4.1: Исправить балансировку

**Статус:** Ожидает выполнения  
**Приоритет:** Критический  
**Связанные UC:** UC-3  
**Зависимости:** Task 1.1 (GetMaxTurns), Task 3.1 (DpGesamt)  

---

## 1. Цель задачи

Изменить алгоритм балансировки контуров: референсный контур должен определяться по максимальному `DpGesamt` и получать максимальные обороты для типа клапана.

---

## 2. Проблема

**Текущее поведение:**
- Референсный контур определяется по максимальным потерям в ТРУБЕ (`CircuitPipeLoss + SupplyPipeLoss`)
- Референсный контур получает обороты, рассчитанные по формуле `CalculateTurns(maxKv, valveType)`
- `zu_drosseln` рассчитывается как разница между максимальными потерями в трубе и потерями контура

**Ожидаемое поведение:**
- Референсный контур определяется по максимальному `DpGesamt`
- Референсный контур получает **максимальные обороты** для типа клапана:
  - HKV-D: 2.5 оборота
  - IV: 8.0 оборотов
- `zu_drosseln` рассчитывается как разница между максимальным `DpGesamt` и `DpGesamt` контура

---

## 3. Связанные юзер-кейсы

### UC-3: Балансировка контуров

**Основной сценарий:**

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

**Критерии приёмки:**
- ✅ Референсный контур = контур с MAX(DpGesamt)
- ✅ Референсный контур имеет максимальные обороты (не 0!)
- ✅ zu_drosseln = DpGesamt_max - DpGesamt_контур
- ✅ Обороты рассчитаны по формулам umdreh1

---

## 4. Изменения в файлах

### 4.1. Файл: `src/Services/Hydraulics/CircuitsCalculator.cs`

#### 4.1.1. Изменить метод CalculateBalancing

**Текущий код (строки 347-415):**

```csharp
public List<CircuitRow> CalculateBalancing(List<CircuitRow> circuits, ValveType valveType)
{
    if (circuits == null || circuits.Count == 0)
        return new List<CircuitRow>();

    // Фильтруем только активные контуры
    var activeCircuits = circuits.Where(c => c.IsActive && c.OperatingResult != null).ToList();

    if (activeCircuits.Count == 0)
        return circuits;

    // Найти контур с максимальными потерями в ТРУБЕ (референсный)
    // Важно: не включаем потери на клапане, так как они зависят от настройки
    double maxPipeLoss = activeCircuits.Max(c => 
        c.OperatingResult.CircuitPipeLoss + c.OperatingResult.SupplyPipeLoss);

    // Получить максимальный Kv для типа клапана (полностью открытый клапан)
    double maxKv = ValveTurnsCalculator.GetDefaultKv(valveType);

    // Рассчитать дросселирование для каждого контура
    foreach (var circuit in activeCircuits)
    {
        // Потери в трубе контура (без потерь на клапане)
        double pipeLoss = circuit.OperatingResult.CircuitPipeLoss + 
                          circuit.OperatingResult.SupplyPipeLoss;

        // zu_drosseln = Δp_max_pipe - Δp_pipe
        // Это потери, которые нужно создать на клапане для балансировки
        circuit.Throttling = maxPipeLoss - pipeLoss;

        // Референсный контур — контур с максимальными потерями в трубе
        circuit.IsReferenceCircuit = Math.Abs(pipeLoss - maxPipeLoss) < 0.01;

        // Расчёт оборотов клапана
        if (circuit.Throttling > 0)
        {
            // Kv для дросселирования
            // Плотность берём из результата расчёта при рабочей температуре (уже в г/см³)
            double density_g_cm3 = circuit.OperatingResult.Density;
            double kv = CalculateKvForThrottling(circuit.FlowRate, circuit.Throttling, density_g_cm3);
            var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, valveType);
            circuit.ValveTurns = turns;
            circuit.ValveTurnsWarning = warning;
        }
        else
        {
            // Референсный контур — максимальные обороты (клапан полностью открыт)
            // Kv = максимальный для данного типа клапана
            var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(maxKv, valveType);
            circuit.ValveTurns = turns;
            circuit.ValveTurnsWarning = warning;
        }
    }

    // Пересчитать потери на клапане при текущих оборотах
    foreach (var circuit in activeCircuits)
    {
        // Рассчитать Kv для текущих оборотов
        double kv = ValveTurnsCalculator.CalculateKvFromTurns(circuit.ValveTurns, valveType);
        
        // Пересчитать потери на клапане
        double density_g_cm3 = circuit.OperatingResult.Density;
        circuit.OperatingResult.ValveLoss = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;
        
        // Обновить суммарные потери (TotalLoss рассчитывается автоматически)
    }

    return circuits;
}
```

**Новый код:**

```csharp
/// <summary>
/// Рассчитать балансировку контуров
/// </summary>
/// <param name="circuits">Список контуров</param>
/// <param name="valveType">Тип балансировочного клапана</param>
/// <returns>Список контуров с рассчитанной балансировкой</returns>
/// <remarks>
/// Алгоритм балансировки:
/// 1. Определить контур с максимальным DpGesamt (референсный)
/// 2. Референсный контур получает максимальные обороты:
///    - HKV-D: 2.5 оборота
///    - IV: 8.0 оборотов
/// 3. Рассчитать дросселирование для каждого контура:
///    zu_drosseln = DpGesamt_max - DpGesamt_контур
/// 4. Для нереференсных контуров рассчитать Kv для дросселирования
/// 5. Рассчитать обороты по формуле umdreh1(Kv, type)
/// 
/// Балансировка выполняется только для рабочей температуре.
/// 
/// Важно: Референсный контур — это контур с максимальным DpGesamt,
/// а не с максимальными потерями в трубе. Это необходимо для корректной
/// балансировки, так как DpGesamt включает все потери.
/// </remarks>
public List<CircuitRow> CalculateBalancing(List<CircuitRow> circuits, ValveType valveType)
{
    if (circuits == null || circuits.Count == 0)
        return new List<CircuitRow>();

    // Фильтруем только активные контуры
    var activeCircuits = circuits.Where(c => c.IsActive && c.OperatingResult != null).ToList();

    if (activeCircuits.Count == 0)
        return circuits;

    // === ИЗМЕНЕНИЕ: Найти контур с МАКСИМАЛЬНЫМ DpGesamt (референсный) ===
    double maxDpGesamt = activeCircuits.Max(c => c.OperatingResult.DpGesamt);

    // === ИЗМЕНЕНИЕ: Максимальные обороты для типа клапана ===
    double maxTurns = ValveTurnsCalculator.GetMaxTurns(valveType);

    // Рассчитать дросселирование для каждого контура
    foreach (var circuit in activeCircuits)
    {
        double dpGesamt = circuit.OperatingResult.DpGesamt;

        // === ИЗМЕНЕНИЕ: zu_drosseln = DpGesamt_max - DpGesamt_контур ===
        circuit.Throttling = maxDpGesamt - dpGesalt;

        // === ИЗМЕНЕНИЕ: Референсный контур — контур с максимальным DpGesamt ===
        circuit.IsReferenceCircuit = Math.Abs(dpGesamt - maxDpGesamt) < 0.01;

        if (circuit.IsReferenceCircuit)
        {
            // === ИЗМЕНЕНИЕ: Референсный контур получает МАКСИМАЛЬНЫЕ обороты ===
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

    // Пересчитать потери на клапане при текущих оборотах
    foreach (var circuit in activeCircuits)
    {
        // Рассчитать Kv для текущих оборотов
        double kv = ValveTurnsCalculator.CalculateKvFromTurns(circuit.ValveTurns, valveType);
        
        // Пересчитать потери на клапане
        double density_g_cm3 = circuit.OperatingResult.Density;
        
        // === ИЗМЕНЕНИЕ: Использовать DpVent вместо ValveLoss ===
        // Примечание: DpVent уже рассчитан в CalculateAtTemperature
        // Здесь мы пересчитываем DpVent для текущих оборотов
        circuit.OperatingResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;
        
        // Обновить DpGesamt (вычисляется автоматически)
        // DpGesamt = DpRohr + DpVerteiler + DpVent
    }

    return circuits;
}
```

---

## 5. Алгоритм балансировки

### 5.1. Текущий алгоритм (ошибочный)

```
1. Найти контур с максимальными потерями в ТРУБЕ (CircuitPipeLoss + SupplyPipeLoss)
2. Референсный контур получает обороты = CalculateTurns(maxKv, valveType)
3. zu_drosseln = maxPipeLoss - pipeLoss
```

### 5.2. Новый алгоритм (правильный)

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

## 6. Тест-кейсы

### 6.1. Тесты для балансировки

**Файл:** `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs`

```csharp
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

## 7. Критерии приёмки

### 7.1. Функциональные

- [ ] Референсный контур = контур с MAX(DpGesamt)
- [ ] Референсный контур получает максимальные обороты (2.5 для HKV-D, 8.0 для IV)
- [ ] zu_drosseln = DpGesamt_max - DpGesamt_контур
- [ ] zu_drosseln ≥ 0 для всех контуров
- [ ] Обороты ≤ максимальных для типа клапана
- [ ] Нереференсные контуры имеют обороты < максимальных

### 7.2. Нефункциональные

- [ ] Все существующие тесты проходят
- [ ] Новые тесты добавлены и проходят
- [ ] Код соответствует стилю проекта
- [ ] XML-документация обновлена

---

## 8. Порядок выполнения

1. **Изменить метод** `CalculateBalancing` — использовать `DpGesamt` вместо потерь в трубе
2. **Изменить расчёт** референсного контура — использовать `GetMaxTurns`
3. **Изменить расчёт** `zu_drosseln` — использовать `DpGesamt`
4. **Добавить тесты** для балансировки
5. **Запустить тесты** и убедиться, что все проходят
6. **Проверить значения** по Excel-примерам

---

## 9. Примечания

### 9.1. Почему референсный контур определяется по DpGesamt?

В Excel-файле gidravlica.xls референсный контур определяется по максимальному `DpGesamt` (столбец N), а не по потерям в трубе. Это необходимо для корректной балансировки, так как `DpGesamt` включает все потери (труба, распределитель, вентиль).

### 9.2. Почему референсный контур получает максимальные обороты?

Референсный контур имеет максимальные потери, поэтому клапан должен быть полностью открыт (максимальные обороты). Остальные контуры дросселируются для выравнивания потерь.

### 9.3. Связь с другими задачами

Эта задача **зависит от**:
- **Task 1.1 (ValveTurnsCalculator):** Нужно `GetMaxTurns` для получения максимальных оборотов
- **Task 3.1 (Формулы):** Нужно `DpGesamt` для определения референсного контура

---

*Задача создана: 2026-03-22*