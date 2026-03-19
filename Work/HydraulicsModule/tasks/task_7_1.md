# Task 7.1: Тесты ValveTurnsCalculator

**Этап:** 7 - Тестирование  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 3.1 (ValveTurnsCalculator)

---

## 1. Цель задачи

Создать unit-тесты для `ValveTurnsCalculator`.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-05 | Балансировка контуров | Тесты формул оборотов клапана |

---

## 3. Создаваемые файлы

### 3.1. ValveTurnsCalculatorTests.cs

**Путь:** `tests/Services/Hydraulics/ValveTurnsCalculatorTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    [TestFixture]
    public class ValveTurnsCalculatorTests
    {
        private ValveTurnsCalculator _calculator;

        [SetUp]
        public void Setup()
        {
            _calculator = new ValveTurnsCalculator();
        }

        #region CalculateTurns Tests

        [Test]
        public void CalculateTurns_HKV_D_ReturnsCorrectValue()
        {
            // Arrange
            double kv = 1.2;
            
            // Act
            double turns = _calculator.CalculateTurns(kv, ValveType.HKV_D);
            
            // Assert
            // Формула: 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712
            // Для Kv=1.2: 4.2111×1.728 - 6.7436×1.44 + 4.6613×1.2 - 0.712 ≈ 2.5
            Assert.That(turns, Is.GreaterThan(2.0));
            Assert.That(turns, Is.LessThan(3.0));
        }
        
        [Test]
        public void CalculateTurns_IV_1_25_ReturnsCorrectValue()
        {
            // Arrange
            double kv = 1.45;
            
            // Act
            double turns = _calculator.CalculateTurns(kv, ValveType.IV_1_25);
            
            // Assert
            // Формула: 5.1818 × Kv - 0.23
            // Для Kv=1.45: 5.1818 × 1.45 - 0.23 ≈ 7.28
            Assert.That(turns, Is.EqualTo(7.3).Within(0.1));
        }
        
        [Test]
        public void CalculateTurns_IV_1_5_ReturnsCorrectValue()
        {
            // Arrange
            double kv = 1.5;
            
            // Act
            double turns = _calculator.CalculateTurns(kv, ValveType.IV_1_5);
            
            // Assert
            // Формула: 5.122 × Kv - 0.2106
            // Для Kv=1.5: 5.122 × 1.5 - 0.2106 ≈ 7.47
            Assert.That(turns, Is.EqualTo(7.5).Within(0.1));
        }

        [Test]
        public void CalculateTurns_HKV_D_MinKv_ReturnsCorrectValue()
        {
            // Arrange
            double kv = 0.8; // Минимальный Kv для HKV-D
            
            // Act
            double turns = _calculator.CalculateTurns(kv, ValveType.HKV_D);
            
            // Assert
            Assert.That(turns, Is.GreaterThan(0));
        }

        [Test]
        public void CalculateTurns_HKV_D_MaxKv_ReturnsCorrectValue()
        {
            // Arrange
            double kv = 4.0; // Максимальный Kv для HKV-D
            
            // Act
            double turns = _calculator.CalculateTurns(kv, ValveType.HKV_D);
            
            // Assert
            Assert.That(turns, Is.GreaterThan(0));
        }

        #endregion

        #region GetDefaultKv Tests

        [Test]
        public void GetDefaultKv_HKV_D_ReturnsCorrectValue()
        {
            Assert.That(_calculator.GetDefaultKv(ValveType.HKV_D), Is.EqualTo(1.2));
        }

        [Test]
        public void GetDefaultKv_IV_1_25_ReturnsCorrectValue()
        {
            Assert.That(_calculator.GetDefaultKv(ValveType.IV_1_25), Is.EqualTo(1.45));
        }

        [Test]
        public void GetDefaultKv_IV_1_5_ReturnsCorrectValue()
        {
            Assert.That(_calculator.GetDefaultKv(ValveType.IV_1_5), Is.EqualTo(1.5));
        }

        #endregion

        #region IsValidKv Tests

        [Test]
        public void IsValidKv_HKV_D_ValidRange_ReturnsTrue()
        {
            Assert.That(_calculator.IsValidKv(0.8, ValveType.HKV_D), Is.True);
            Assert.That(_calculator.IsValidKv(2.0, ValveType.HKV_D), Is.True);
            Assert.That(_calculator.IsValidKv(4.0, ValveType.HKV_D), Is.True);
        }
        
        [Test]
        public void IsValidKv_HKV_D_InvalidRange_ReturnsFalse()
        {
            Assert.That(_calculator.IsValidKv(0.7, ValveType.HKV_D), Is.False);
            Assert.That(_calculator.IsValidKv(4.1, ValveType.HKV_D), Is.False);
        }

        [Test]
        public void IsValidKv_IV_1_25_ValidRange_ReturnsTrue()
        {
            Assert.That(_calculator.IsValidKv(0.5, ValveType.IV_1_25), Is.True);
            Assert.That(_calculator.IsValidKv(2.5, ValveType.IV_1_25), Is.True);
        }

        [Test]
        public void IsValidKv_IV_1_5_ValidRange_ReturnsTrue()
        {
            Assert.That(_calculator.IsValidKv(0.5, ValveType.IV_1_5), Is.True);
            Assert.That(_calculator.IsValidKv(4.0, ValveType.IV_1_5), Is.True);
        }

        #endregion

        #region GetValveTypeName Tests

        [Test]
        public void GetValveTypeName_HKV_D_ReturnsCorrectName()
        {
            Assert.That(_calculator.GetValveTypeName(ValveType.HKV_D), Is.EqualTo("HKV-D"));
        }

        [Test]
        public void GetValveTypeName_IV_1_25_ReturnsCorrectName()
        {
            Assert.That(_calculator.GetValveTypeName(ValveType.IV_1_25), Is.EqualTo("IV 1¼\""));
        }

        [Test]
        public void GetValveTypeName_IV_1_5_ReturnsCorrectName()
        {
            Assert.That(_calculator.GetValveTypeName(ValveType.IV_1_5), Is.EqualTo("IV 1½\""));
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

| Тест | Описание | Ожидаемый результат |
|------|----------|---------------------|
| CalculateTurns_HKV_D | Расчёт оборотов для HKV-D | Значение в диапазоне [2.0, 3.0] для Kv=1.2 |
| CalculateTurns_IV_1_25 | Расчёт оборотов для IV 1¼" | ≈ 7.3 для Kv=1.45 |
| CalculateTurns_IV_1_5 | Расчёт оборотов для IV 1½" | ≈ 7.5 для Kv=1.5 |
| GetDefaultKv | Получение Kv по умолчанию | HKV-D: 1.2, IV 1¼": 1.45, IV 1½": 1.5 |
| IsValidKv | Проверка диапазона Kv | true/false в зависимости от типа |

---

## 5. Критерии приёмки

- [ ] Файл тестов создан
- [ ] Все тесты проходят
- [ ] Покрытие кода > 90%
- [ ] Тесты для всех формул оборотов
- [ ] Тесты для граничных значений Kv

---

## 6. Связанные задачи

- Task 3.1: ValveTurnsCalculator — тестируемый класс
- Task 1.1: ValveType — enum для типов клапанов

---

*Дата создания: 2026-03-17*