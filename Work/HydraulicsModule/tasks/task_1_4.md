# Task 1.4: Collector (Модель коллектора)

**Этап:** 1 - Models  
**Приоритет:** Средний  
**Статус:** Завершено  
**Зависимости:** Task 1.1 (Enums)

---

## 1. Цель задачи

Создать класс `Collector` — модель коллектора РЕХАУ для подбора и расчёта.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-04 | Расчёт потерь давления в вентилях | Kv для расчёта потерь |
| UC-05 | Подбор коллектора РЕХАУ | Основная модель для подбора |

---

## 3. Создаваемые файлы

### 3.1. Collector.cs

**Путь:** `src/Models/Hydraulics/Collector.cs`

**Содержимое:**
```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Коллектор РЕХАУ для систем снеготаяния
    /// </summary>
    /// <remarks>
    /// Модель содержит технические характеристики коллекторов:
    /// - Бытовые коллекторы HKV-D (2-12 контуров)
    /// - Промышленные коллекторы IV (DN25, DN40)
    /// 
    /// Данные загружаются из data/rehau_products.json
    /// </remarks>
    public class Collector
    {
        /// <summary>
        /// Идентификатор коллектора
        /// </summary>
        /// <remarks>
        /// Формат: "HKV-D-2", "HKV-D-4", ..., "IV-1.25", "IV-1.5"
        /// </remarks>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Краткое название коллектора
        /// </summary>
        /// <example>HKV-D 4</example>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Полное название коллектора
        /// </summary>
        /// <example>Коллектор HKV-D 4 контура</example>
        public string FullName { get; set; } = string.Empty;
        
        /// <summary>
        /// Тип коллектора
        /// </summary>
        /// <remarks>
        /// HKV — бытовой коллектор
        /// IV — промышленный коллектор
        /// </remarks>
        public CollectorType Type { get; set; }
        
        /// <summary>
        /// Количество контуров
        /// </summary>
        /// <remarks>
        /// Для HKV-D: 2, 4, 6, 8, 10, 12
        /// Для IV: определяется размером подключения
        /// </remarks>
        public int Circuits { get; set; }
        
        /// <summary>
        /// Размер подключения
        /// </summary>
        /// <example>1¼", 1½"</example>
        public string ConnectionSize { get; set; } = string.Empty;
        
        /// <summary>
        /// Коэффициент пропускной способности вентиля (Kv), м³/ч
        /// </summary>
        /// <remarks>
        /// Используется для расчёта потерь давления в вентиле:
        /// - HKV-D: Kv = 1.2 м³/ч
        /// - IV 1¼": Kv = 1.45 м³/ч
        /// - IV 1½": Kv = 1.5 м³/ч
        /// </remarks>
        public double Kv { get; set; }
        
        /// <summary>
        /// Максимальный расход через коллектор, м³/ч
        /// </summary>
        /// <remarks>
        /// Для HKV-D: 1.5 м³/ч
        /// </remarks>
        public double MaxFlowRate { get; set; }
        
        /// <summary>
        /// Максимальное давление, мбар
        /// </summary>
        /// <remarks>
        /// Для HKV-D: 320 мбар
        /// </remarks>
        public double MaxPressure { get; set; }
        
        /// <summary>
        /// Максимальная настройка вентиля
        /// </summary>
        /// <remarks>
        /// Диапазон: 1-8
        /// </remarks>
        public int MaxSetting { get; set; } = 8;
        
        /// <summary>
        /// Артикул РЕХАУ
        /// </summary>
        public string? ArticleNumber { get; set; }
        
        /// <summary>
        /// Примечания
        /// </summary>
        public string? Notes { get; set; }
        
        // === Вычисляемые свойства ===
        
        /// <summary>
        /// Признак бытового коллектора
        /// </summary>
        public bool IsResidential => Type == CollectorType.HKV;
        
        /// <summary>
        /// Признак промышленного коллектора
        /// </summary>
        public bool IsIndustrial => Type == CollectorType.IV;
        
        /// <summary>
        /// Максимальное давление в Па
        /// </summary>
        public double MaxPressure_Pa => MaxPressure * 100;
        
        /// <summary>
        /// Максимальный расход в л/ч
        /// </summary>
        public double MaxFlowRate_L_h => MaxFlowRate * 1000;
        
        // === Методы ===
        
        /// <summary>
        /// Проверить, подходит ли коллектор для заданного количества контуров
        /// </summary>
        /// <param name="circuitCount">Количество контуров</param>
        /// <returns>true, если подходит</returns>
        public bool IsSuitableForCircuits(int circuitCount)
        {
            if (Type == CollectorType.HKV)
            {
                return circuitCount >= 2 && circuitCount <= Circuits;
            }
            
            // Для промышленных коллекторов проверка по расходу
            return true;
        }
        
        /// <summary>
        /// Проверить, подходит ли коллектор для заданного расхода
        /// </summary>
        /// <param name="flowRate_m3_h">Расход, м³/ч</param>
        /// <returns>true, если подходит</returns>
        public bool IsSuitableForFlowRate(double flowRate_m3_h)
        {
            return flowRate_m3_h <= MaxFlowRate;
        }
        
        /// <summary>
        /// Проверить, подходит ли коллектор для заданного давления
        /// </summary>
        /// <param name="pressure_mbar">Давление, мбар</param>
        /// <returns>true, если подходит</returns>
        public bool IsSuitableForPressure(double pressure_mbar)
        {
            return pressure_mbar <= MaxPressure;
        }
        
        /// <summary>
        /// Получить описание коллектора
        /// </summary>
        public string GetDescription()
        {
            return $"{FullName}, {Circuits} конт., Kv={Kv} м³/ч, макс. расход {MaxFlowRate} м³/ч, макс. давление {MaxPressure} мбар";
        }
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Models/Hydraulics/CollectorTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class CollectorTests
    {
        [Test]
        public void IsResidential_ReturnsTrueForHKV()
        {
            // Arrange
            var collector = new Collector { Type = CollectorType.HKV };
            
            // Act & Assert
            Assert.That(collector.IsResidential, Is.True);
            Assert.That(collector.IsIndustrial, Is.False);
        }
        
        [Test]
        public void IsIndustrial_ReturnsTrueForIV()
        {
            // Arrange
            var collector = new Collector { Type = CollectorType.IV };
            
            // Act & Assert
            Assert.That(collector.IsIndustrial, Is.True);
            Assert.That(collector.IsResidential, Is.False);
        }
        
        [Test]
        public void MaxPressure_Pa_CalculatesCorrectly()
        {
            // Arrange
            var collector = new Collector { MaxPressure = 320 };
            
            // Act & Assert
            Assert.That(collector.MaxPressure_Pa, Is.EqualTo(32000));
        }
        
        [Test]
        public void MaxFlowRate_L_h_CalculatesCorrectly()
        {
            // Arrange
            var collector = new Collector { MaxFlowRate = 1.5 };
            
            // Act & Assert
            Assert.That(collector.MaxFlowRate_L_h, Is.EqualTo(1500));
        }
        
        [Test]
        public void IsSuitableForCircuits_ReturnsTrueForValidCount()
        {
            // Arrange
            var collector = new Collector
            {
                Type = CollectorType.HKV,
                Circuits = 4
            };
            
            // Act & Assert
            Assert.That(collector.IsSuitableForCircuits(2), Is.True);
            Assert.That(collector.IsSuitableForCircuits(4), Is.True);
            Assert.That(collector.IsSuitableForCircuits(6), Is.False);
        }
        
        [Test]
        public void IsSuitableForFlowRate_ReturnsTrueForValidFlow()
        {
            // Arrange
            var collector = new Collector { MaxFlowRate = 1.5 };
            
            // Act & Assert
            Assert.That(collector.IsSuitableForFlowRate(1.0), Is.True);
            Assert.That(collector.IsSuitableForFlowRate(1.5), Is.True);
            Assert.That(collector.IsSuitableForFlowRate(2.0), Is.False);
        }
        
        [Test]
        public void IsSuitableForPressure_ReturnsTrueForValidPressure()
        {
            // Arrange
            var collector = new Collector { MaxPressure = 320 };
            
            // Act & Assert
            Assert.That(collector.IsSuitableForPressure(200), Is.True);
            Assert.That(collector.IsSuitableForPressure(320), Is.True);
            Assert.That(collector.IsSuitableForPressure(400), Is.False);
        }
        
        [Test]
        public void GetDescription_ReturnsCorrectDescription()
        {
            // Arrange
            var collector = new Collector
            {
                FullName = "Коллектор HKV-D 4 контура",
                Circuits = 4,
                Kv = 1.2,
                MaxFlowRate = 1.5,
                MaxPressure = 320
            };
            
            // Act
            var description = collector.GetDescription();
            
            // Assert
            Assert.That(description, Does.Contain("HKV-D 4"));
            Assert.That(description, Does.Contain("Kv=1.2"));
            Assert.That(description, Does.Contain("1.5 м³/ч"));
            Assert.That(description, Does.Contain("320 мбар"));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `Collector.cs` создан
- [ ] Класс содержит все свойства из ТЗ
- [ ] Вычисляемые свойства работают корректно
- [ ] Методы проверки IsSuitableFor... работают корректно
- [ ] XML-документация для всех свойств и методов
- [ ] Unit-тесты проходят успешно
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Kv — коэффициент пропускной способности вентиля, используется для расчёта потерь давления
- Данные о коллекторах должны загружаться из `data/rehau_products.json`
- Класс должен быть сериализуемым в JSON