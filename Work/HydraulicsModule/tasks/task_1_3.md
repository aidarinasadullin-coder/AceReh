# Task 1.3: Обновить CollectorSummary.cs

**Этап:** 1 - Модели данных  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 1.1 (ValveType)

---

## 1. Цель задачи

Обновить класс `CollectorSummary` — добавить свойство `ValveType` для поддержки балансировочных клапанов.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-05 | Балансировка контуров | ValveType определяет формулу расчёта оборотов |
| UC-06 | Подбор коллектора | ValveType связан с типом коллектора |

---

## 3. Изменяемые файлы

### 3.1. CollectorSummary.cs

**Путь:** `src/Models/Hydraulics/CollectorSummary.cs`

**Изменения:**

Добавить свойство `ValveType`:

```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Итоги расчёта коллектора
    /// </summary>
    public class CollectorSummary
    {
        // === Существующие свойства ===
        
        /// <summary>
        /// Номер коллектора
        /// </summary>
        public int CollectorNumber { get; set; }
        
        /// <summary>
        /// Тип коллектора
        /// </summary>
        public string CollectorType { get; set; } = "HKV-D";
        
        /// <summary>
        /// Kv коллектора (коэффициент пропускной способности), м³/ч
        /// </summary>
        public double Kv { get; set; } = 1.2;
        
        /// <summary>
        /// Количество контуров
        /// </summary>
        public int CircuitCount { get; set; }
        
        /// <summary>
        /// Общая длина труб, м
        /// </summary>
        public double TotalPipeLength { get; set; }
        
        /// <summary>
        /// Общая мощность, Вт
        /// </summary>
        public double TotalPower { get; set; }
        
        /// <summary>
        /// Общий расход, л/ч
        /// </summary>
        public double TotalFlowRate { get; set; }
        
        /// <summary>
        /// Общий расход, м³/ч
        /// </summary>
        public double TotalFlowRate_m3h => TotalFlowRate / 1000.0;
        
        /// <summary>
        /// Потери при рабочей температуре, мбар
        /// </summary>
        public double PressureLoss_Operating_mbar { get; set; }
        
        /// <summary>
        /// Потери при расчётной температуре, мбар
        /// </summary>
        public double PressureLoss_Cold_mbar { get; set; }
        
        /// <summary>
        /// Максимальные потери контура, Па
        /// </summary>
        public double MaxCircuitLoss { get; set; }
        
        /// <summary>
        /// Номер референсного контура
        /// </summary>
        public int ReferenceCircuitNumber { get; set; }
        
        /// <summary>
        /// Признак валидности
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Ошибки валидации
        /// </summary>
        public string[] Errors { get; set; } = Array.Empty<string>();
        
        /// <summary>
        /// Предупреждения
        /// </summary>
        public string[] Warnings { get; set; } = Array.Empty<string>();
        
        // === Новое свойство ===
        
        /// <summary>
        /// Тип балансировочного клапана
        /// </summary>
        /// <remarks>
        /// Определяет формулу расчёта оборотов клапана:
        /// - HKV-D: бытовой коллектор, Kv = 1.2 м³/ч
        /// - IV 1¼": промышленный коллектор, Kv = 1.45 м³/ч
        /// - IV 1½": промышленный коллектор, Kv = 1.5 м³/ч
        /// </remarks>
        public ValveType ValveType { get; set; } = ValveType.HKV_D;
        
        // === Вычисляемые свойства ===
        
        /// <summary>
        /// Потери при рабочей температуре, Па
        /// </summary>
        public double PressureLoss_Operating_Pa => PressureLoss_Operating_mbar * 100;
        
        /// <summary>
        /// Потери при расчётной температуре, Па
        /// </summary>
        public double PressureLoss_Cold_Pa => PressureLoss_Cold_mbar * 100;
        
        /// <summary>
        /// Максимально допустимые потери (ограничение РЕХАУ), мбар
        /// </summary>
        public static readonly double MaxAllowedPressure_mbar = 320;
        
        /// <summary>
        /// Проверка превышения лимита потерь
        /// </summary>
        public bool IsPressureExceeded => PressureLoss_Cold_mbar > MaxAllowedPressure_mbar;
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Models/Hydraulics/CollectorSummaryTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class CollectorSummaryTests
    {
        [Test]
        public void ValveType_DefaultValue_IsHKV_D()
        {
            // Arrange & Act
            var summary = new CollectorSummary();
            
            // Assert
            Assert.That(summary.ValveType, Is.EqualTo(ValveType.HKV_D));
        }
        
        [Test]
        public void ValveType_CanBeSet()
        {
            // Arrange
            var summary = new CollectorSummary();
            
            // Act
            summary.ValveType = ValveType.IV_1_5;
            
            // Assert
            Assert.That(summary.ValveType, Is.EqualTo(ValveType.IV_1_5));
        }
        
        [Test]
        public void OperatingPressureLoss_mbar_ConvertsCorrectly()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Operating_mbar = 320 // мбар
            };
            
            // Assert
            Assert.That(summary.PressureLoss_Operating_Pa, Is.EqualTo(32000)); // Па
        }
        
        [Test]
        public void DesignPressureLoss_mbar_ConvertsCorrectly()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Cold_mbar = 450 // мбар
            };
            
            // Assert
            Assert.That(summary.PressureLoss_Cold_Pa, Is.EqualTo(45000)); // Па
        }
        
        [Test]
        public void IsPressureExceeded_ReturnsTrueWhenExceeded()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Cold_mbar = 350 // 350 мбар > 320 мбар
            };
            
            // Assert
            Assert.That(summary.IsPressureExceeded, Is.True);
        }
        
        [Test]
        public void IsPressureExceeded_ReturnsFalseWhenNotExceeded()
        {
            // Arrange
            var summary = new CollectorSummary
            {
                PressureLoss_Cold_mbar = 300 // 300 мбар < 320 мбар
            };
            
            // Assert
            Assert.That(summary.IsPressureExceeded, Is.False);
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Свойство `ValveType` добавлено в `CollectorSummary.cs`
- [ ] Значение по умолчанию: `HKV_D`
- [ ] XML-документация для свойства
- [ ] Вычисляемые свойства работают корректно
- [ ] Unit-тесты проходят успешно
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Свойство `ValveType` используется в `CircuitsCalculator.CalculateBalancing()`
- Значение по умолчанию `HKV_D` соответствует бытовому коллектору
- Вычисляемые свойства `OperatingPressureLoss_mbar` и `DesignPressureLoss_mbar` конвертируют Па в мбар

---

## 7. Связанные задачи

- Task 1.1: ValveType — используется в этом классе
- Task 3.2: CircuitsCalculator — использует CollectorSummary
- Task 4.2: CollectorViewModel — использует CollectorSummary

---

*Дата создания: 2026-03-17*