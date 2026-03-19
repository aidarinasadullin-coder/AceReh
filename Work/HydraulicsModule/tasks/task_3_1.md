# Task 3.1: Создать ValveTurnsCalculator.cs

**Этап:** 3 - Сервисы расчёта  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 1.1 (ValveType)

---

## 1. Цель задачи

Создать класс `ValveTurnsCalculator` для расчёта оборотов балансировочного клапана.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-05 | Балансировка контуров | Расчёт оборотов клапана |

---

## 3. Создаваемые файлы

### 3.1. ValveTurnsCalculator.cs

**Путь:** `src/Services/Hydraulics/ValveTurnsCalculator.cs`

**Содержимое:**
```csharp
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Калькулятор оборотов балансировочного клапана
    /// </summary>
    /// <remarks>
    /// Рассчитывает количество оборотов балансировочного клапана
    /// в зависимости от коэффициента пропускной способности (Kv).
    /// 
    /// Поддерживаемые типы клапанов:
    /// - HKV-D: бытовой коллектор, Kv = 1.2 м³/ч
    /// - IV 1¼": промышленный коллектор, Kv = 1.45 м³/ч
    /// - IV 1½": промышленный коллектор, Kv = 1.5 м³/ч
    /// </remarks>
    public static class ValveTurnsCalculator
    {
        #region Константы

        /// <summary>
        /// Kv для HKV-D (бытовой коллектор)
        /// </summary>
        public const double KV_HKV_D = 1.2;

        /// <summary>
        /// Kv для IV 1¼" (промышленный коллектор)
        /// </summary>
        public const double KV_IV_1_25 = 1.45;

        /// <summary>
        /// Kv для IV 1½" (промышленный коллектор)
        /// </summary>
        public const double KV_IV_1_5 = 1.5;

        #endregion

        #region Основные методы

        /// <summary>
        /// Рассчитать обороты балансировочного клапана
        /// </summary>
        /// <param name="kv">Коэффициент пропускной способности (м³/ч)</param>
        /// <param name="valveType">Тип клапана</param>
        /// <returns>Количество оборотов (округлено до 0.1)</returns>
        /// <remarks>
        /// Формулы расчёта:
        /// - IV 1½": Обороты = 5.122 × Kv - 0.2106
        /// - IV 1¼": Обороты = 5.1818 × Kv - 0.23
        /// - HKV-D: Обороты = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712
        /// </remarks>
        public static double CalculateTurns(double kv, ValveType valveType)
        {
            double turns = valveType switch
            {
                ValveType.IV_1_5 => CalculateTurnsIV_1_5(kv),
                ValveType.IV_1_25 => CalculateTurnsIV_1_25(kv),
                ValveType.HKV_D => CalculateTurnsHKV_D(kv),
                _ => throw new ArgumentException($"Неподдерживаемый тип клапана: {valveType}")
            };

            // Округление до 0.1 оборота
            return Math.Round(turns, 1);
        }

        /// <summary>
        /// Получить Kv по типу клапана
        /// </summary>
        /// <param name="valveType">Тип клапана</param>
        /// <returns>Kv (м³/ч)</returns>
        public static double GetDefaultKv(ValveType valveType)
        {
            return valveType switch
            {
                ValveType.HKV_D => KV_HKV_D,
                ValveType.IV_1_25 => KV_IV_1_25,
                ValveType.IV_1_5 => KV_IV_1_5,
                _ => throw new ArgumentException($"Неподдерживаемый тип клапана: {valveType}")
            };
        }

        /// <summary>
        /// Получить название клапана
        /// </summary>
        /// <param name="valveType">Тип клапана</param>
        /// <returns>Название клапана</returns>
        public static string GetValveTypeName(ValveType valveType)
        {
            return valveType switch
            {
                ValveType.HKV_D => "HKV-D (бытовой коллектор)",
                ValveType.IV_1_25 => "IV 1¼\" (промышленный коллектор)",
                ValveType.IV_1_5 => "IV 1½\" (промышленный коллектор)",
                _ => "Неизвестный тип"
            };
        }

        /// <summary>
        /// Проверить валидность Kv для типа клапана
        /// </summary>
        /// <param name="kv">Коэффициент пропускной способности</param>
        /// <param name="valveType">Тип клапана</param>
        /// <returns>True, если Kv в допустимом диапазоне</returns>
        public static bool IsValidKv(double kv, ValveType valveType)
        {
            return valveType switch
            {
                ValveType.HKV_D => kv >= 0.8 && kv <= 4.0,
                ValveType.IV_1_25 => kv >= 0.5 && kv <= 3.0,
                ValveType.IV_1_5 => kv >= 0.5 && kv <= 3.5,
                _ => false
            };
        }

        #endregion

        #region Приватные методы

        /// <summary>
        /// Расчёт оборотов для IV 1½"
        /// Формула: Обороты = 5.122 × Kv - 0.2106
        /// </summary>
        private static double CalculateTurnsIV_1_5(double kv)
        {
            return 5.122 * kv - 0.2106;
        }

        /// <summary>
        /// Расчёт оборотов для IV 1¼"
        /// Формула: Обороты = 5.1818 × Kv - 0.23
        /// </summary>
        private static double CalculateTurnsIV_1_25(double kv)
        {
            return 5.1818 * kv - 0.23;
        }

        /// <summary>
        /// Расчёт оборотов для HKV-D
        /// Формула: Обороты = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712
        /// </summary>
        private static double CalculateTurnsHKV_D(double kv)
        {
            return 4.2111 * Math.Pow(kv, 3) 
                   - 6.7436 * Math.Pow(kv, 2) 
                   + 4.6613 * kv 
                   - 0.712;
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Services/Hydraulics/ValveTurnsCalculatorTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    [TestFixture]
    public class ValveTurnsCalculatorTests
    {
        [Test]
        public void CalculateTurns_HKV_D_ReturnsCorrectValue()
        {
            // Arrange
            double kv = 1.2; // Kv для HKV-D
            
            // Act
            double turns = ValveTurnsCalculator.CalculateTurns(kv, ValveType.HKV_D);
            
            // Assert
            // Формула: 4.2111×1.2³ - 6.7436×1.2² + 4.6613×1.2 - 0.712
            // Ожидаемое значение: ~2.5 оборота
            Assert.That(turns, Is.GreaterThan(2.0));
            Assert.That(turns, Is.LessThan(3.0));
        }
        
        [Test]
        public void CalculateTurns_IV_1_25_ReturnsCorrectValue()
        {
            // Arrange
            double kv = 1.45; // Kv для IV 1¼"
            
            // Act
            double turns = ValveTurnsCalculator.CalculateTurns(kv, ValveType.IV_1_25);
            
            // Assert
            // Формула: 5.1818 × 1.45 - 0.23
            // Ожидаемое значение: ~7.3 оборота
            Assert.That(turns, Is.EqualTo(7.3).Within(0.1));
        }
        
        [Test]
        public void CalculateTurns_IV_1_5_ReturnsCorrectValue()
        {
            // Arrange
            double kv = 1.5; // Kv для IV 1½"
            
            // Act
            double turns = ValveTurnsCalculator.CalculateTurns(kv, ValveType.IV_1_5);
            
            // Assert
            // Формула: 5.122 × 1.5 - 0.2106
            // Ожидаемое значение: ~7.5 оборота
            Assert.That(turns, Is.EqualTo(7.5).Within(0.1));
        }
        
        [Test]
        public void GetDefaultKv_HKV_D_ReturnsCorrectValue()
        {
            // Act
            double kv = ValveTurnsCalculator.GetDefaultKv(ValveType.HKV_D);
            
            // Assert
            Assert.That(kv, Is.EqualTo(1.2));
        }
        
        [Test]
        public void GetDefaultKv_IV_1_25_ReturnsCorrectValue()
        {
            // Act
            double kv = ValveTurnsCalculator.GetDefaultKv(ValveType.IV_1_25);
            
            // Assert
            Assert.That(kv, Is.EqualTo(1.45));
        }
        
        [Test]
        public void GetDefaultKv_IV_1_5_ReturnsCorrectValue()
        {
            // Act
            double kv = ValveTurnsCalculator.GetDefaultKv(ValveType.IV_1_5);
            
            // Assert
            Assert.That(kv, Is.EqualTo(1.5));
        }
        
        [Test]
        public void GetValveTypeName_HKV_D_ReturnsCorrectName()
        {
            // Act
            string name = ValveTurnsCalculator.GetValveTypeName(ValveType.HKV_D);
            
            // Assert
            Assert.That(name, Does.Contain("HKV-D"));
            Assert.That(name, Does.Contain("бытовой"));
        }
        
        [Test]
        public void GetValveTypeName_IV_1_25_ReturnsCorrectName()
        {
            // Act
            string name = ValveTurnsCalculator.GetValveTypeName(ValveType.IV_1_25);
            
            // Assert
            Assert.That(name, Does.Contain("IV 1¼"));
            Assert.That(name, Does.Contain("промышленный"));
        }
        
        [Test]
        public void IsValidKv_HKV_D_ValidRange_ReturnsTrue()
        {
            // Act & Assert
            Assert.That(ValveTurnsCalculator.IsValidKv(0.8, ValveType.HKV_D), Is.True);
            Assert.That(ValveTurnsCalculator.IsValidKv(2.0, ValveType.HKV_D), Is.True);
            Assert.That(ValveTurnsCalculator.IsValidKv(4.0, ValveType.HKV_D), Is.True);
        }
        
        [Test]
        public void IsValidKv_HKV_D_InvalidRange_ReturnsFalse()
        {
            // Act & Assert
            Assert.That(ValveTurnsCalculator.IsValidKv(0.7, ValveType.HKV_D), Is.False);
            Assert.That(ValveTurnsCalculator.IsValidKv(4.1, ValveType.HKV_D), Is.False);
        }
        
        [Test]
        public void CalculateTurns_RoundsToTenth()
        {
            // Arrange
            double kv = 1.5;
            
            // Act
            double turns = ValveTurnsCalculator.CalculateTurns(kv, ValveType.IV_1_5);
            
            // Assert - проверяем, что результат округлён до 0.1
            double fractional = turns - Math.Floor(turns);
            Assert.That(fractional, Is.LessThanOrEqualTo(0.1).Or.GreaterThanOrEqualTo(0.9 - 0.001));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `ValveTurnsCalculator.cs` создан в `src/Services/Hydraulics/`
- [ ] Формулы реализованы корректно для всех типов клапанов
- [ ] Результат округляется до 0.1 оборота
- [ ] Методы GetDefaultKv, GetValveTypeName, IsValidKv работают
- [ ] XML-документация для всех методов
- [ ] Unit-тесты проходят успешно
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Класс статический, не требует DI
- Формулы взяты из документации РЕХАУ
- Kv — коэффициент пропускной способности (м³/ч)
- Диапазоны Kv для каждого типа клапана указаны в XML-документации

---

## 7. Связанные задачи

- Task 1.1: ValveType — используется в этом классе
- Task 3.2: CircuitsCalculator — использует ValveTurnsCalculator

---

*Дата создания: 2026-03-17*