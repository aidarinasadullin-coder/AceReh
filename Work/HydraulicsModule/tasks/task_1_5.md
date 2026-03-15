# Task 1.5: CircuitResult (Результат контура)

**Этап:** 1 - Models  
**Приоритет:** Средний  
**Статус:** Завершено  
**Зависимости:** Task 1.3 (HydraulicResult)

---

## 1. Цель задачи

Создать класс `CircuitResult` — модель результата расчёта контура для балансировки.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-06 | Расчёт дросселирования контуров | Основной класс для балансировки |

---

## 3. Создаваемые файлы

### 3.1. CircuitResult.cs

**Путь:** `src/Models/Hydraulics/CircuitResult.cs`

**Содержимое:**
```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Результат расчёта контура для балансировки
    /// </summary>
    /// <remarks>
    /// Используется для расчёта дросселирования при балансировке
    /// нескольких контуров на одном коллекторе.
    /// 
    /// Алгоритм балансировки:
    /// 1. Определить контур с максимальными потерями (Δp_max)
    /// 2. Для каждого контура рассчитать дросселирование:
    ///    zu_drosseln = Δp_max - Δp_контур - Δp_вентиль
    /// 3. Определить настройку вентиля (1-8)
    /// </remarks>
    public class CircuitResult
    {
        /// <summary>
        /// Номер контура
        /// </summary>
        public int CircuitNumber { get; set; }
        
        /// <summary>
        /// Название/идентификатор контура
        /// </summary>
        public string? CircuitName { get; set; }
        
        /// <summary>
        /// Длина контура (L_HK), м
        /// </summary>
        public double Length { get; set; }
        
        /// <summary>
        /// Длина подводки (L_Zul), м
        /// </summary>
        public double SupplyLength { get; set; }
        
        /// <summary>
        /// Общая длина (L_total), м
        /// </summary>
        public double TotalLength => Length + SupplyLength;
        
        /// <summary>
        /// Площадь контура, м²
        /// </summary>
        public double Area { get; set; }
        
        /// <summary>
        /// Расход на контур (v), л/ч
        /// </summary>
        public double FlowRate { get; set; }
        
        /// <summary>
        /// Потери давления в трубе контура (Δp_HK), Па
        /// </summary>
        public double CircuitPipePressureLoss { get; set; }
        
        /// <summary>
        /// Потери давления в подводке (Δp_Zul), Па
        /// </summary>
        public double SupplyPipePressureLoss { get; set; }
        
        /// <summary>
        /// Общие потери давления в трубе (Δp_Rohr), Па
        /// </summary>
        public double TotalPipePressureLoss { get; set; }
        
        /// <summary>
        /// Потери давления в вентиле (Δp_Vent), Па
        /// </summary>
        public double ValvePressureLoss { get; set; }
        
        /// <summary>
        /// Суммарные потери давления (Δp_total), Па
        /// </summary>
        /// <remarks>
        /// Формула: Δp_total = Δp_Rohr + Δp_Vent
        /// </remarks>
        public double TotalPressureLoss { get; set; }
        
        /// <summary>
        /// Дросселирование для балансировки (zu_drosseln), Па
        /// </summary>
        /// <remarks>
        /// Рассчитывается относительно контура с максимальными потерями:
        /// zu_drosseln = Δp_max - Δp_контур - Δp_вентиль
        /// </remarks>
        public double Throttling { get; set; }
        
        /// <summary>
        /// Рекомендуемая настройка вентиля (1-8)
        /// </summary>
        /// <remarks>
        /// Определяется по таблице настроек вентиля:
        /// - Настройка 1: минимальное сопротивление
        /// - Настройка 8: максимальное сопротивление
        /// </remarks>
        public int RecommendedValveSetting { get; set; }
        
        /// <summary>
        /// Детальный результат гидравлического расчёта
        /// </summary>
        public HydraulicResult HydraulicResult { get; set; } = new();
        
        // === Вычисляемые свойства ===
        
        /// <summary>
        /// Суммарные потери в кПа
        /// </summary>
        public double TotalPressureLoss_kPa => TotalPressureLoss / 1000;
        
        /// <summary>
        /// Суммарные потери в мбар
        /// </summary>
        public double TotalPressureLoss_mbar => TotalPressureLoss / 100;
        
        /// <summary>
        /// Дросселирование в кПа
        /// </summary>
        public double Throttling_kPa => Throttling / 1000;
        
        /// <summary>
        /// Дросселирование в мбар
        /// </summary>
        public double Throttling_mbar => Throttling / 100;
        
        /// <summary>
        /// Признак того, что контур требует дросселирования
        /// </summary>
        public bool RequiresThrottling => Throttling > 0;
        
        /// <summary>
        /// Признак того, что контур является опорным (максимальные потери)
        /// </summary>
        public bool IsReferenceCircuit { get; set; }
        
        // === Методы ===
        
        /// <summary>
        /// Создать пустой результат
        /// </summary>
        public static CircuitResult Empty => new();
        
        /// <summary>
        /// Получить краткое описание контура
        /// </summary>
        public string GetSummary()
        {
            return $"Контур {CircuitNumber}: L={Length:F1}м, v={FlowRate:F1}л/ч, Δp={TotalPressureLoss_mbar:F1}мбар";
        }
        
        /// <summary>
        /// Получить информацию о балансировке
        /// </summary>
        public string GetBalancingInfo()
        {
            if (IsReferenceCircuit)
            {
                return $"Контур {CircuitNumber} — опорный (макс. потери)";
            }
            
            if (RequiresThrottling)
            {
                return $"Контур {CircuitNumber}: дросселирование {Throttling_mbar:F1}мбар, вентиль {RecommendedValveSetting}";
            }
            
            return $"Контур {CircuitNumber}: балансировка не требуется";
        }
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Models/Hydraulics/CircuitResultTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class CircuitResultTests
    {
        [Test]
        public void TotalLength_CalculatesCorrectly()
        {
            // Arrange
            var result = new CircuitResult
            {
                Length = 100,
                SupplyLength = 20
            };
            
            // Act & Assert
            Assert.That(result.TotalLength, Is.EqualTo(120));
        }
        
        [Test]
        public void TotalPressureLoss_kPa_CalculatesCorrectly()
        {
            // Arrange
            var result = new CircuitResult { TotalPressureLoss = 5000 };
            
            // Act & Assert
            Assert.That(result.TotalPressureLoss_kPa, Is.EqualTo(5));
        }
        
        [Test]
        public void TotalPressureLoss_mbar_CalculatesCorrectly()
        {
            // Arrange
            var result = new CircuitResult { TotalPressureLoss = 32000 };
            
            // Act & Assert
            Assert.That(result.TotalPressureLoss_mbar, Is.EqualTo(320));
        }
        
        [Test]
        public void Throttling_mbar_CalculatesCorrectly()
        {
            // Arrange
            var result = new CircuitResult { Throttling = 5000 };
            
            // Act & Assert
            Assert.That(result.Throttling_mbar, Is.EqualTo(50));
        }
        
        [Test]
        public void RequiresThrottling_ReturnsTrueWhenPositive()
        {
            // Arrange
            var result = new CircuitResult { Throttling = 100 };
            
            // Act & Assert
            Assert.That(result.RequiresThrottling, Is.True);
        }
        
        [Test]
        public void RequiresThrottling_ReturnsFalseWhenZero()
        {
            // Arrange
            var result = new CircuitResult { Throttling = 0 };
            
            // Act & Assert
            Assert.That(result.RequiresThrottling, Is.False);
        }
        
        [Test]
        public void GetSummary_ReturnsCorrectString()
        {
            // Arrange
            var result = new CircuitResult
            {
                CircuitNumber = 1,
                Length = 100,
                FlowRate = 200,
                TotalPressureLoss = 20000
            };
            
            // Act
            var summary = result.GetSummary();
            
            // Assert
            Assert.That(summary, Does.Contain("Контур 1"));
            Assert.That(summary, Does.Contain("100м"));
            Assert.That(summary, Does.Contain("200л/ч"));
            Assert.That(summary, Does.Contain("200мбар"));
        }
        
        [Test]
        public void GetBalancingInfo_ReturnsReferenceCircuitInfo()
        {
            // Arrange
            var result = new CircuitResult
            {
                CircuitNumber = 1,
                IsReferenceCircuit = true
            };
            
            // Act
            var info = result.GetBalancingInfo();
            
            // Assert
            Assert.That(info, Does.Contain("опорный"));
        }
        
        [Test]
        public void GetBalancingInfo_ReturnsThrottlingInfo()
        {
            // Arrange
            var result = new CircuitResult
            {
                CircuitNumber = 2,
                Throttling = 5000,
                RecommendedValveSetting = 5
            };
            
            // Act
            var info = result.GetBalancingInfo();
            
            // Assert
            Assert.That(info, Does.Contain("дросселирование"));
            Assert.That(info, Does.Contain("50мбар"));
            Assert.That(info, Does.Contain("вентиль 5"));
        }
        
        [Test]
        public void Empty_CreatesEmptyResult()
        {
            // Act
            var result = CircuitResult.Empty;
            
            // Assert
            Assert.That(result.CircuitNumber, Is.EqualTo(0));
            Assert.That(result.TotalPressureLoss, Is.EqualTo(0));
            Assert.That(result.HydraulicResult, Is.Not.Null);
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `CircuitResult.cs` создан
- [ ] Класс содержит все свойства из ТЗ
- [ ] Вычисляемые свойства работают корректно
- [ ] Методы GetSummary() и GetBalancingInfo() работают корректно
- [ ] XML-документация для всех свойств и методов
- [ ] Unit-тесты проходят успешно
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Класс ссылается на `HydraulicResult` для хранения детальных результатов
- Дросселирование рассчитывается относительно контура с максимальными потерями
- Настройка вентиля определяется по таблице настроек (1-8)