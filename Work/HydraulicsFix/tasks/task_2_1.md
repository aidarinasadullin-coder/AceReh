# Task 2.1: Добавить DpVerteiler в модель

**Статус:** Ожидает выполнения  
**Приоритет:** Критический  
**Связанные UC:** UC-2, UC-4  
**Зависимости:** Нет  

---

## 1. Цель задачи

Добавить свойства `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt`, `ZuDrosseln` в класс `CircuitTemperatureResult` для хранения результатов гидравлического расчёта в соответствии с Excel-файлом gidravlica.xls.

---

## 2. Проблема

**Текущее поведение:**
- Модель хранит только `CircuitPipeLoss`, `SupplyPipeLoss`, `ValveLoss`, `TotalLoss`
- Нет отдельного свойства для `DpVerteiler` (потери в распределителе)
- Нет свойства для `DpGesamt` (суммарные потери)
- Нет свойства для `ZuDrosseln` (дросселирование)

**Ожидаемое поведение:**
- Модель хранит `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt`, `ZuDrosseln`
- Старые свойства помечены как `[Obsolete]`
- Значения в Паскалях (Па), а не в миллибарах (мбар)

---

## 3. Связанные юзер-кейсы

### UC-2: Расчёт потерь давления DpVerteiler и DpVent

**Постусловия:**
- DpVerteiler рассчитан корректно
- DpVent рассчитан корректно
- DpGesamt = DpRohr + DpVerteiler + DpVent

### UC-4: Отображение результатов в таблице

**Таблица содержит колонки:**
- DpRohr (Па) — потери в трубе
- DpVerteiler (Па) — потери в распределителе
- DpVent (Па) — потери в вентиле
- DpGesamt (Па) — суммарные потери
- zu_drosseln (Па) — дросселирование

---

## 4. Изменения в файлах

### 4.1. Файл: `src/Models/Hydraulics/CircuitRow.cs`

#### 4.1.1. Добавить новые свойства в класс CircuitTemperatureResult

**Место:** После свойства `PressureLossPerMeter` (строка ~50)

**Добавить:**

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
/// 
/// Соответствует столбцу K в Excel (gidravlica.xls)
/// </remarks>
public double DpRohr { get; set; }

/// <summary>
/// Потери в распределителе, Па (DpVerteiler)
/// </summary>
/// <remarks>
/// Формулы зависят от типа коллектора:
/// 
/// Для IV 1¼" и IV 1½":
/// DpVerteiler = 15000 × (ρ/2000) × v²
/// 
/// Для HKV-D:
/// DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
/// 
/// Где:
/// - ρ — плотность в кг/м³ (делить на 1000 для г/см³)
/// - v — скорость в м/с
/// - V_dot — расход в л/ч
/// 
/// Соответствует столбцу L в Excel (gidravlica.xls)
/// </remarks>
public double DpVerteiler { get; set; }

/// <summary>
/// Потери в вентиле, Па (DpVent)
/// </summary>
/// <remarks>
/// Формулы зависят от типа коллектора:
/// 
/// Для IV 1¼" и IV 1½":
/// DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
/// 
/// Для HKV-D:
/// DpVent = 15000 × (ρ/2000) × v²
/// 
/// Где:
/// - V_dot — расход в л/ч
/// - Kv — коэффициент пропускной способности, м³/ч
/// - ρ — плотность в кг/м³ (делить на 1000 для г/см³)
/// - v — скорость в м/с
/// 
/// Соответствует столбцу M в Excel (gidravlica.xls)
/// </remarks>
public double DpVent { get; set; }

/// <summary>
/// Суммарные потери, Па (DpGesamt)
/// </summary>
/// <remarks>
/// Формула: DpGesamt = DpRohr + DpVerteiler + DpVent
/// 
/// Соответствует столбцу N в Excel (gidravlica.xls)
/// </remarks>
public double DpGesamt => DpRohr + DpVerteiler + DpVent;

/// <summary>
/// Дросселирование для балансировки, Па (zu_drosseln)
/// </summary>
/// <remarks>
/// Формула: zu_drosseln = DpGesamt_max - DpGesamt_контур
/// 
/// Где:
/// - DpGesamt_max — максимальные суммарные потери в коллекторе
/// - DpGesamt_контур — суммарные потери контура
/// 
/// Соответствует столбцу O в Excel (gidravlica.xls)
/// 
/// Примечание: Это свойство вычисляется в CircuitRow, а не в CircuitTemperatureResult.
/// </remarks>
public double ZuDrosseln { get; set; }
```

#### 4.1.2. Пометить старые свойства как устаревшие

**Место:** После новых свойств

**Изменить:**

```csharp
/// <summary>
/// Потери в трубе контура, Па
/// </summary>
[Obsolete("Использовать DpRohr вместо CircuitPipeLoss. DpRohr включает потери в контуре и подводке.")]
public double CircuitPipeLoss { get; set; }

/// <summary>
/// Потери в трубе контура, мбар
/// </summary>
[Obsolete("Использовать DpRohr / 100.0 вместо CircuitPipeLoss_mbar")]
public double CircuitPipeLoss_mbar => CircuitPipeLoss / 100.0;

/// <summary>
/// Потери в трубе подводки, Па
/// </summary>
[Obsolete("Использовать DpRohr вместо SupplyPipeLoss. DpRohr включает потери в контуре и подводке.")]
public double SupplyPipeLoss { get; set; }

/// <summary>
/// Потери в вентиле, Па
/// </summary>
[Obsolete("Использовать DpVent вместо ValveLoss для IV. Для HKV-D использовать DpVerteiler.")]
public double ValveLoss { get; set; }

/// <summary>
/// Потери в вентиле, мбар
/// </summary>
[Obsolete("Использовать DpVent / 100.0 вместо ValveLoss_mbar")]
public double ValveLoss_mbar => ValveLoss / 100.0;

/// <summary>
/// Суммарные потери, Па
/// </summary>
[Obsolete("Использовать DpGesamt вместо TotalLoss")]
public double TotalLoss => DpRohr + DpVerteiler + DpVent;

/// <summary>
/// Суммарные потери, мбар
/// </summary>
[Obsolete("Использовать DpGesamt / 100.0 вместо TotalLoss_mbar")]
public double TotalLoss_mbar => DpGesamt / 100.0;
```

#### 4.1.3. Добавить свойство ZuDrosseln в класс CircuitRow

**Место:** После свойства `_throttling` (строка ~343)

**Примечание:** Свойство `Throttling` уже существует в `CircuitRow`. Нужно убедиться, что оно используется правильно.

**Текущий код:**

```csharp
/// <summary>
/// Дросселирование для балансировки, Па
/// </summary>
/// <remarks>
/// Разница между максимальными потерями в коллекторе и потерями контура
/// Вычисляется только для рабочей температуры
/// </remarks>
[ObservableProperty]
private double _throttling;
```

**Это свойство уже существует и соответствует zu_drosseln.**

---

## 5. Структура Excel (gidravlica.xls)

| Столбец | Название | Формула | Свойство в модели |
|---------|----------|---------|-------------------|
| K | DpRohr | `=E×J` (длина × удельные потери) | `DpRohr` |
| L | DpVerteiler | IV: `15000×(ρ/2000)×v²` / HKV: `(V/1000/1.2)²×100000×ρ/1000` | `DpVerteiler` |
| M | DpVent | IV: `(V/1000/Kv)²×100000×ρ/1000` / HKV: `15000×(ρ/2000)×v²` | `DpVent` |
| N | DpGesamt | `=K+L+M` | `DpGesamt` |
| O | zu_drosseln | `=MAX(N)-N` | `Throttling` (в CircuitRow) |

---

## 6. Тест-кейсы

### 6.1. Тесты для CircuitTemperatureResult

**Файл:** `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CircuitTemperatureResultTests.cs`

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

#endregion
```

---

## 7. Критерии приёмки

### 7.1. Функциональные

- [ ] Класс `CircuitTemperatureResult` содержит свойства `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt`, `ZuDrosseln`
- [ ] Свойство `DpGesamt` вычисляется как сумма `DpRohr + DpVerteiler + DpVent`
- [ ] Старые свойства помечены атрибутом `[Obsolete]`
- [ ] Класс `CircuitRow` содержит свойство `Throttling` (уже существует)

### 7.2. Нефункциональные

- [ ] Все существующие тесты проходят (с предупреждениями об устаревших свойствах)
- [ ] Новые тесты добавлены и проходят
- [ ] Код соответствует стилю проекта
- [ ] XML-документация добавлена для всех новых свойств

---

## 8. Порядок выполнения

1. **Добавить новые свойства** в `CircuitTemperatureResult`
2. **Добавить XML-документацию** для новых свойств
3. **Пометить старые свойства** как `[Obsolete]`
4. **Добавить тесты** для `DpGesamt`
5. **Запустить тесты** и убедиться, что все проходят
6. **Проверить предупреждения компилятора** об устаревших свойствах

---

## 9. Примечания

### 9.1. Почему DpRohr вместо CircuitPipeLoss + SupplyPipeLoss?

В Excel используется одна колонка `DpRohr`, которая включает потери в контуре и подводке. Это упрощает модель и соответствует методике РЕХАУ.

### 9.2. Почему DpGesamt вычисляемое свойство?

`DpGesamt` всегда вычисляется как сумма `DpRohr + DpVerteiler + DpVent`. Нет смысла хранить его отдельно.

### 9.3. Связь с другими задачами

Эта задача является **базовой** для:
- **Task 3.1 (Формулы):** Нужно заполнить новые свойства значениями
- **Task 5.1 (Единицы):** Нужно использовать новые свойства вместо старых
- **Task 6.1 (UI):** Нужно привязать новые свойства к колонкам таблицы

---

*Задача создана: 2026-03-22*