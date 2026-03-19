# Task 1.1: Создать ValveType.cs

**Этап:** 1 - Модели данных  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Нет

---

## 1. Цель задачи

Создать enum `ValveType` для типов балансировочных клапанов РЕХАУ.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-05 | Балансировка контуров | ValveType определяет формулу расчёта оборотов |
| UC-06 | Подбор коллектора | ValveType связан с типом коллектора |

---

## 3. Создаваемые файлы

### 3.1. ValveType.cs

**Путь:** `src/Models/Hydraulics/ValveType.cs`

**Содержимое:**
```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Тип балансировочного клапана
    /// </summary>
    /// <remarks>
    /// Определяет коэффициент пропускной способности (Kv)
    /// и формулу расчёта оборотов балансировочного клапана.
    /// 
    /// Типы клапанов:
    /// - HKV-D: бытовой коллектор, Kv = 1.2 м³/ч
    /// - IV 1¼": промышленный коллектор, Kv = 1.45 м³/ч
    /// - IV 1½": промышленный коллектор, Kv = 1.5 м³/ч
    /// </remarks>
    public enum ValveType
    {
        /// <summary>
        /// HKV-D (бытовой коллектор)
        /// </summary>
        /// <remarks>
        /// Kv = 1.2 м³/ч
        /// Формула оборотов: Обороты = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712
        /// Диапазон Kv: 0.8 - 4.0
        /// </remarks>
        HKV_D = 0,
        
        /// <summary>
        /// IV 1¼" (промышленный коллектор)
        /// </summary>
        /// <remarks>
        /// Kv = 1.45 м³/ч
        /// Формула оборотов: Обороты = 5.1818 × Kv - 0.23
        /// Диапазон Kv: 0.5 - 3.0
        /// </remarks>
        IV_1_25 = 1,
        
        /// <summary>
        /// IV 1½" (промышленный коллектор)
        /// </summary>
        /// <remarks>
        /// Kv = 1.5 м³/ч
        /// Формула оборотов: Обороты = 5.122 × Kv - 0.2106
        /// Диапазон Kv: 0.5 - 3.5
        /// </remarks>
        IV_1_5 = 2
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Models/Hydraulics/ValveTypeTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class ValveTypeTests
    {
        [Test]
        public void ValveType_HasCorrectValues()
        {
            // Assert
            Assert.That((int)ValveType.HKV_D, Is.EqualTo(0));
            Assert.That((int)ValveType.IV_1_25, Is.EqualTo(1));
            Assert.That((int)ValveType.IV_1_5, Is.EqualTo(2));
        }
        
        [Test]
        public void ValveType_HasThreeValues()
        {
            // Assert
            var values = Enum.GetValues<ValveType>();
            Assert.That(values.Length, Is.EqualTo(3));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `ValveType.cs` создан в `src/Models/Hydraulics/`
- [ ] Enum содержит три значения: HKV_D, IV_1_25, IV_1_5
- [ ] XML-документация для каждого значения
- [ ] XML-документация содержит формулы и диапазоны Kv
- [ ] Unit-тесты проходят успешно
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Значения enum начинаются с 0 для совместимости с сериализацией JSON
- XML-документация должна содержать формулы расчёта оборотов
- Файл размещается в `src/Models/Hydraulics/`
- Связан с `ValveTurnsCalculator` (Task 3.1)

---

## 7. Связанные задачи

- Task 1.3: Обновить CollectorSummary.cs — добавить свойство ValveType
- Task 3.1: Создать ValveTurnsCalculator.cs — использовать ValveType
- Task 3.2: Создать CircuitsCalculator.cs — использовать ValveType

---

*Дата создания: 2026-03-17*