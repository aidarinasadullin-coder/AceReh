# Task 5.1: Изменить единицы давления

**Статус:** Ожидает выполнения  
**Приоритет:** Высокий  
**Связанные UC:** UC-4  
**Зависимости:** Task 2.1 (новые свойства в модели)  

---

## 1. Цель задачи

Перевести все значения давления из миллибар (мбар) в Паскали (Па) с отображением целых чисел.

---

## 2. Проблема

**Текущее поведение:**
- Значения давления хранятся в Паскалях (Па)
- Отображаются в миллибарах (мбар) с десятичными дробями
- Свойства `*_mbar` используются для отображения

**Ожидаемое поведение:**
- Значения давления хранятся в Паскалях (Па)
- Отображаются в Паскалях (Па) как целые числа
- Свойства `*_mbar` помечены как `[Obsolete]`

---

## 3. Связанные юзер-кейсы

### UC-4: Отображение результатов в таблице

**Таблица содержит колонки:**
- DpRohr (Па) — потери в трубе
- DpVerteiler (Па) — потери в распределителе
- DpVent (Па) — потери в вентиле
- DpGesamt (Па) — суммарные потери
- zu_drosseln (Па) — дросселирование

**Критерии приёмки:**
- ✅ DpRohr отображается в Па (целые числа)
- ✅ DpVerteiler отображается в Па (целые числа)
- ✅ DpVent отображается в Па (целые числа)
- ✅ DpGesamt отображается в Па (целые числа)
- ✅ zu_drosseln отображается в Па (целые числа)

---

## 4. Изменения в файлах

### 4.1. Файл: `src/Models/Hydraulics/CircuitRow.cs`

#### 4.1.1. Пометить свойства *_mbar как устаревшие

**Текущий код (строки 59-85):**

```csharp
/// <summary>
/// Потери в трубе контура, мбар
/// </summary>
public double CircuitPipeLoss_mbar => CircuitPipeLoss / 100.0;

// ...

/// <summary>
/// Потери в вентиле, мбар
/// </summary>
public double ValveLoss_mbar => ValveLoss / 100.0;

/// <summary>
/// Суммарные потери, мбар
/// </summary>
public double TotalLoss_mbar => TotalLoss / 100.0;
```

**Новый код:**

```csharp
/// <summary>
/// Потери в трубе контура, мбар
/// </summary>
[Obsolete("Использовать DpRohr / 100.0 вместо CircuitPipeLoss_mbar. Значения в Па.")]
public double CircuitPipeLoss_mbar => CircuitPipeLoss / 100.0;

// ...

/// <summary>
/// Потери в вентиле, мбар
/// </summary>
[Obsolete("Использовать DpVent / 100.0 вместо ValveLoss_mbar. Значения в Па.")]
public double ValveLoss_mbar => ValveLoss / 100.0;

/// <summary>
/// Суммарные потери, мбар
/// </summary>
[Obsolete("Использовать DpGesamt / 100.0 вместо TotalLoss_mbar. Значения в Па.")]
public double TotalLoss_mbar => TotalLoss / 100.0;
```

#### 4.1.2. Добавить свойства для отображения в Па

**Примечание:** Новые свойства `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt` уже добавлены в Task 2.1. Они возвращают значения в Паскалях.

**Для отображения целых чисел использовать `StringFormat=F0` в XAML.**

---

### 4.2. Файл: `src/Models/Hydraulics/CollectorSummary.cs`

#### 4.2.1. Проверить свойства давления

**Текущий код:**

```csharp
/// <summary>
/// Потери при рабочей температуре, мбар
/// </summary>
public double PressureLoss_Operating_mbar { get; set; }

/// <summary>
/// Потери при расчётной температуре, мбар
/// </summary>
public double PressureLoss_Cold_mbar { get; set; }
```

**Новый код:**

```csharp
/// <summary>
/// Потери при рабочей температуре, Па
/// </summary>
public double PressureLoss_Operating_Pa { get; set; }

/// <summary>
/// Потери при расчётной температуре, Па
/// </summary>
public double PressureLoss_Cold_Pa { get; set; }

/// <summary>
/// Потери при рабочей температуре, мбар
/// </summary>
[Obsolete("Использовать PressureLoss_Operating_Pa / 100.0")]
public double PressureLoss_Operating_mbar => PressureLoss_Operating_Pa / 100.0;

/// <summary>
/// Потери при расчётной температуре, мбар
/// </summary>
[Obsolete("Использовать PressureLoss_Cold_Pa / 100.0")]
public double PressureLoss_Cold_mbar => PressureLoss_Cold_Pa / 100.0;
```

---

### 4.3. Файл: `src/Services/Hydraulics/CircuitsCalculator.cs`

#### 4.3.1. Изменить расчёт итогов коллектора

**Текущий код (строки 460-461):**

```csharp
PressureLoss_Operating_mbar = activeCircuits.Max(c => c.OperatingResult?.TotalLoss_mbar ?? 0),
PressureLoss_Cold_mbar = activeCircuits.Max(c => c.DesignResult?.TotalLoss_mbar ?? 0)
```

**Новый код:**

```csharp
PressureLoss_Operating_Pa = activeCircuits.Max(c => c.OperatingResult?.DpGesamt ?? 0),
PressureLoss_Cold_Pa = activeCircuits.Max(c => c.DesignResult?.DpGesamt ?? 0)
```

---

## 5. Конвертация единиц

### 5.1. Соотношение единиц

```
1 мбар = 100 Па
1 Па = 0.01 мбар
```

### 5.2. Примеры конвертации

| Па | мбар |
|----|------|
| 730 | 7.3 |
| 1798 | 17.98 |
| 32000 | 320 |

### 5.3. Отображение

- **Па:** целые числа (730, 1798, 32000)
- **мбар:** десятичные дроби (7.3, 17.98, 320.0)

---

## 6. Тест-кейсы

### 6.1. Тесты для конвертации единиц

**Файл:** `tests/SnowMeltingCalculator.Tests/Models/Hydraulics/CircuitTemperatureResultTests.cs`

```csharp
#region Unit Conversion Tests

[Test]
public void DpGesamt_InPascals_ReturnsCorrectValue()
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
    Assert.That(dpGesamt, Is.EqualTo(730));  // В Паскалях
}

[Test]
public void Obsolete_TotalLoss_mbar_ReturnsCorrectValue()
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
    Assert.That(totalLoss_mbar, Is.EqualTo(7.3).Within(0.01));  // В миллибарах
}

#endregion
```

---

## 7. Критерии приёмки

### 7.1. Функциональные

- [ ] Новые свойства `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt` возвращают значения в Па
- [ ] Старые свойства `*_mbar` помечены как `[Obsolete]`
- [ ] `CollectorSummary.PressureLoss_Operating_Pa` возвращает значение в Па
- [ ] `CollectorSummary.PressureLoss_Cold_Pa` возвращает значение в Па

### 7.2. Нефункциональные

- [ ] Все существующие тесты проходят (с предупреждениями об устаревших свойствах)
- [ ] Новые тесты добавлены и проходят
- [ ] Код соответствует стилю проекта

---

## 8. Порядок выполнения

1. **Пометить свойства** `*_mbar` как `[Obsolete]` в `CircuitTemperatureResult`
2. **Добавить свойства** `PressureLoss_Operating_Pa` и `PressureLoss_Cold_Pa` в `CollectorSummary`
3. **Изменить расчёт** итогов коллектора — использовать `DpGesamt` вместо `TotalLoss_mbar`
4. **Добавить тесты** для конвертации единиц
5. **Запустить тесты** и убедиться, что все проходят
6. **Проверить предупреждения** компилятора об устаревших свойствах

---

## 9. Примечания

### 9.1. Почему Паскали вместо миллибар?

В Excel-файле gidravlica.xls все значения давления указаны в Паскалях (Па). Это соответствует международной системе единиц (СИ) и упрощает расчёты.

### 9.2. Почему целые числа?

В Excel значения давления отображаются как целые числа (730, 1798, 32000). Это упрощает чтение и сравнение результатов.

### 9.3. Связь с другими задачами

Эта задача **зависит от**:
- **Task 2.1 (Модель):** Нужно, чтобы свойства `DpRohr`, `DpVerteiler`, `DpVent`, `DpGesamt` существовали

Эта задача является **базовой для**:
- **Task 6.1 (UI):** Нужно отображать новые свойства в Паскалях

---

*Задача создана: 2026-03-22*