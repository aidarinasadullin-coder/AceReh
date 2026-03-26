# Task 1.1: Исправить ValveTurnsCalculator

**Статус:** Ожидает выполнения  
**Приоритет:** Критический  
**Связанные UC:** UC-1  
**Зависимости:** Нет  

---

## 1. Цель задачи

Добавить метод `GetMaxTurns(ValveType)` для определения максимальных оборотов балансировочного клапана по типу и изменить проверку максимальных оборотов в методе `CalculateTurnsWithWarning`.

---

## 2. Проблема

**Текущее поведение:**
- Константа `MaxTurns = 8.0` используется для всех типов клапанов
- HKV-D имеет максимальные обороты **2.5**, а не 8.0
- Референсный контур получает обороты, рассчитанные по формуле, вместо максимальных

**Ожидаемое поведение:**
- HKV-D: максимальные обороты = **2.5**
- IV 1¼": максимальные обороты = **8.0**
- IV 1½": максимальные обороты = **8.0**
- Референсный контур получает максимальные обороты для типа клапана

---

## 3. Связанные юзер-кейсы

### UC-1: Расчёт максимальных оборотов клапана

**Основной сценарий:**
1. Система определяет тип коллектора (ValveType)
2. Система вызывает метод `GetMaxTurns(ValveType)`
3. Для HKV-D система возвращает **2.5** оборота
4. Для IV 1¼" система возвращает **8.0** оборотов
5. Для IV 1½" система возвращает **8.0** оборотов
6. Референсный контур получает максимальные обороты

**Критерии приёмки:**
- ✅ `GetMaxTurns(ValveType.HKV_D)` возвращает 2.5
- ✅ `GetMaxTurns(ValveType.IV_1_25)` возвращает 8.0
- ✅ `GetMaxTurns(ValveType.IV_1_5)` возвращает 8.0
- ✅ Референсный контур получает максимальные обороты (не 0!)

---

## 4. Изменения в файлах

### 4.1. Файл: `src/Services/Hydraulics/ValveTurnsCalculator.cs`

#### 4.1.1. Добавить метод GetMaxTurns

**Место:** После констант (строка ~40)

**Добавить:**

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
/// 
/// Важно: HKV-D имеет ограничение в 2.5 оборота из-за конструкции клапана.
/// Промышленные коллекторы IV имеют больший ход клапана (8 оборотов).
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

#### 4.1.2. Изменить метод CalculateTurnsWithWarning

**Текущий код (строки 87-110):**

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

    // Проверка ограничения оборотов
    if (turns > MaxTurns)  // ← ПРОБЛЕМА: MaxTurns = 8.0 для всех
    {
        warning = $"Расчётные обороты ({turns:F2}) превышают максимум ({MaxTurns}). Установлено {MaxTurns} оборотов.";
        turns = MaxTurns;
    }

    // Округление до 0.25 оборота
    turns = Math.Round(turns * 4) / 4;

    return (turns, warning);
}
```

**Новый код:**

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

    // Проверка ограничения оборотов
    if (turns > maxTurns)
    {
        warning = $"Расчётные обороты ({turns:F2}) превышают максимум ({maxTurns}). Установлено {maxTurns} оборотов.";
        turns = maxTurns;
    }

    // Округление до 0.25 оборота
    turns = Math.Round(turns * 4) / 4;

    return (turns, warning);
}
```

#### 4.1.3. Удалить или оставить константу MaxTurns

**Вариант 1 (рекомендуется):** Оставить константу для обратной совместимости, но добавить комментарий:

```csharp
/// <summary>
/// Максимальное количество оборотов клапана (для IV)
/// </summary>
/// <remarks>
/// Устарело. Использовать GetMaxTurns(ValveType) для получения максимальных оборотов по типу клапана.
/// HKV-D имеет максимальные обороты 2.5, а не 8.0.
/// </remarks>
[Obsolete("Использовать GetMaxTurns(ValveType) для получения максимальных оборотов по типу клапана")]
public const double MaxTurns = 8.0;
```

**Вариант 2:** Удалить константу, если она не используется в других местах.

---

## 5. Тест-кейсы

### 5.1. Тесты для GetMaxTurns

**Файл:** `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/ValveTurnsCalculatorTests.cs`

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
```

### 5.2. Тесты для CalculateTurnsWithWarning

```csharp
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

#endregion
```

---

## 6. Критерии приёмки

### 6.1. Функциональные

- [ ] Метод `GetMaxTurns(ValveType.HKV_D)` возвращает 2.5
- [ ] Метод `GetMaxTurns(ValveType.IV_1_25)` возвращает 8.0
- [ ] Метод `GetMaxTurns(ValveType.IV_1_5)` возвращает 8.0
- [ ] Метод выбрасывает `ArgumentException` для неподдерживаемого типа
- [ ] Метод `CalculateTurnsWithWarning` использует `GetMaxTurns` вместо константы `MaxTurns`
- [ ] Предупреждение содержит корректное значение максимальных оборотов

### 6.2. Нефункциональные

- [ ] Все существующие тесты проходят
- [ ] Новые тесты добавлены и проходят
- [ ] Код соответствует стилю проекта

---

## 7. Порядок выполнения

1. **Добавить метод `GetMaxTurns`** в `ValveTurnsCalculator.cs`
2. **Изменить метод `CalculateTurnsWithWarning`** для использования `GetMaxTurns`
3. **Добавить XML-документацию** для нового метода
4. **Добавить тесты** в `ValveTurnsCalculatorTests.cs`
5. **Запустить тесты** и убедиться, что все проходят
6. **Проверить обратную совместимость** (если используется `MaxTurns`)

---

## 8. Примечания

### 8.1. Почему HKV-D имеет максимальные обороты 2.5?

HKV-D — бытовой коллектор с ограниченным ходом клапана. Максимальные обороты 2.5 обусловлены конструкцией клапана. Промышленные коллекторы IV имеют больший ход клапана (8 оборотов).

### 8.2. Формулы оборотов

- **IV 1½":** Обороты = 5.122 × Kv - 0.2106
- **IV 1¼":** Обороты = 5.1818 × Kv - 0.23
- **HKV-D:** Обороты = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712

### 8.3. Связь с другими задачами

Эта задача является **базовой** для:
- **Task 4.1 (Балансировка):** Референсный контур должен получать максимальные обороты через `GetMaxTurns`

---

*Задача создана: 2026-03-22*