# Task 3.3: Обновить FlowRegimeCalculator.cs

**Этап:** 3 - Сервисы расчёта  
**Приоритет:** Средний  
**Статус:** К разработке  
**Зависимости:** Task 2.1 (ICircuitsCalculator)

---

## 1. Цель задачи

Добавить метод `CalculateFrictionFactor` в существующий класс `FlowRegimeCalculator`.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-03 | Расчёт при двух температурах | Коэффициент трения λ |
| UC-04 | Расчёт потерь давления | Коэффициент трения λ |

---

## 3. Изменяемые файлы

### 3.1. FlowRegimeCalculator.cs

**Путь:** `src/Services/Hydraulics/FlowRegimeCalculator.cs`

**Изменения:**

Добавить публичный метод:

```csharp
/// <summary>
/// Рассчитать коэффициент трения λ для любого режима
/// </summary>
/// <param name="reynolds">Число Рейнольдса</param>
/// <param name="innerDiameter">Внутренний диаметр трубы (мм)</param>
/// <param name="roughness">Шероховатость трубы (мм), по умолчанию 0.007 для PE-Xa</param>
/// <returns>Коэффициент трения λ</returns>
public static double CalculateFrictionFactor(
    double reynolds, 
    double innerDiameter, 
    double roughness = 0.007)
{
    var regime = DetermineFlowRegime(reynolds);
    
    return regime switch
    {
        FlowRegime.Laminar => CalculateLaminarFrictionFactor(reynolds),
        FlowRegime.Transitional => CalculateTransitionalFrictionFactor(
            reynolds, innerDiameter, roughness),
        FlowRegime.Turbulent => CalculateTurbulentFrictionFactor(
            reynolds, innerDiameter, roughness),
        _ => throw new ArgumentOutOfRangeException(nameof(regime))
    };
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Services/Hydraulics/FlowRegimeCalculatorTests.cs`

```csharp
[Test]
public void CalculateFrictionFactor_Laminar_ReturnsCorrectValue()
{
    // Arrange
    double re = 2000;
    double diameter = 16;
    
    // Act
    double lambda = FlowRegimeCalculator.CalculateFrictionFactor(re, diameter);
    
    // Assert
    // Ламинарный: λ = 64 / Re
    Assert.That(lambda, Is.EqualTo(0.032).Within(0.0001));
}

[Test]
public void CalculateFrictionFactor_Transitional_ReturnsInterpolatedValue()
{
    // Arrange
    double re = 3000;
    double diameter = 16;
    
    // Act
    double lambda = FlowRegimeCalculator.CalculateFrictionFactor(re, diameter);
    
    // Assert
    // Должно быть между λ_lam и λ_turb
    Assert.That(lambda, Is.GreaterThan(0.02));
    Assert.That(lambda, Is.LessThan(0.05));
}

[Test]
public void CalculateFrictionFactor_Turbulent_ReturnsCorrectValue()
{
    // Arrange
    double re = 10000;
    double diameter = 16;
    
    // Act
    double lambda = FlowRegimeCalculator.CalculateFrictionFactor(re, diameter);
    
    // Assert
    // Турбулентный: формула Колбрука-Уайта
    Assert.That(lambda, Is.GreaterThan(0.02));
    Assert.That(lambda, Is.LessThan(0.05));
}

[Test]
public void CalculateFrictionFactor_WithCustomRoughness_WorksCorrectly()
{
    // Arrange
    double re = 10000;
    double diameter = 16;
    double roughness = 0.01;
    
    // Act
    double lambda = FlowRegimeCalculator.CalculateFrictionFactor(re, diameter, roughness);
    
    // Assert
    Assert.That(lambda, Is.GreaterThan(0));
}
```

---

## 5. Критерии приёмки

- [ ] Метод `CalculateFrictionFactor` добавлен
- [ ] Работает для всех режимов течения
- [ ] Значение по умолчанию для шероховатости: 0.007 мм (PE-Xa)
- [ ] Unit-тесты проходят успешно
- [ ] XML-документация добавлена

---

## 6. Примечания

- Метод объединяет существующие методы для ламинарного, переходного и турбулентного режимов
- По умолчанию используется шероховатость PE-Xa труб (0.007 мм)
- Метод статический, не требует DI

---

## 7. Связанные задачи

- Task 3.2: CircuitsCalculator — использует этот метод

---

*Дата создания: 2026-03-17*