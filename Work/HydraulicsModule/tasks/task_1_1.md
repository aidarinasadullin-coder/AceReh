# Task 1.1: Enums (Перечисления)

**Этап:** 1 - Models  
**Приоритет:** Высокий  
**Статус:** Завершено ✅  
**Зависимости:** Нет

---

## 1. Цель задачи

Создать перечисления (enums) для модуля гидравлического расчёта:
- `FlowRegime` — режим течения
- `GlycolType` — тип гликоля
- `CollectorType` — тип коллектора

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-01 | Расчёт гидравлических параметров контура | FlowRegime используется для определения режима течения |
| UC-02 | Определение режима течения | FlowRegime — основной результат |
| UC-04 | Расчёт потерь давления в вентилях | CollectorType определяет тип коллектора |
| UC-05 | Подбор коллектора РЕХАУ | CollectorType для фильтрации коллекторов |
| UC-07 | Загрузка свойств теплоносителя | GlycolType для выбора типа гликоля |

---

## 3. Создаваемые файлы

### 3.1. FlowRegime.cs

**Путь:** `src/Models/Hydraulics/FlowRegime.cs`

**Содержимое:**
```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Режим течения жидкости в трубе
    /// </summary>
    /// <remarks>
    /// Определяется по числу Рейнольдса:
    /// - Ламинарный: Re &lt; 2300
    /// - Переходный: 2300 ≤ Re ≤ 4000
    /// - Турбулентный: Re &gt; 4000
    /// </remarks>
    public enum FlowRegime
    {
        /// <summary>
        /// Ламинарный режим течения (Re &lt; 2300)
        /// </summary>
        /// <remarks>
        /// Плавное, упорядоченное движение жидкости слоями.
        /// Коэффициент трения: λ = 64 / Re (формула Пуазейля)
        /// </remarks>
        Laminar = 0,
        
        /// <summary>
        /// Переходный режим течения (2300 ≤ Re ≤ 4000)
        /// </summary>
        /// <remarks>
        /// Неустойчивый режим между ламинарным и турбулентным.
        /// Коэффициент трения: линейная интерполяция между λ_lam и λ_turb
        /// </remarks>
        Transitional = 1,
        
        /// <summary>
        /// Турбулентный режим течения (Re &gt; 4000)
        /// </summary>
        /// <remarks>
        /// Хаотичное движение жидкости с вихрями.
        /// Коэффициент трения: формула Колбрука-Уайта
        /// </remarks>
        Turbulent = 2
    }
}
```

---

### 3.2. GlycolType.cs

**Путь:** `src/Models/Hydraulics/GlycolType.cs`

**Содержимое:**
```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Тип гликоля в теплоносителе
    /// </summary>
    /// <remarks>
    /// Определяет физические свойства теплоносителя:
    /// - Плотность (ρ)
    /// - Кинематическая вязкость (ν)
    /// - Удельная теплоёмкость (c_p)
    /// - Теплопроводность (λ)
    /// 
    /// Данные берутся из data/glycol_data.json
    /// </remarks>
    public enum GlycolType
    {
        /// <summary>
        /// Этиленгликоль (C2H6O2)
        /// </summary>
        /// <remarks>
        /// Более эффективен по теплофизическим свойствам.
        /// Токсичен, требует осторожности при эксплуатации.
        /// Диапазон концентраций: 10-90%
        /// </remarks>
        Ethylene = 0,
        
        /// <summary>
        /// Пропиленгликоль (C3H8O2)
        /// </summary>
        /// <remarks>
        /// Нетоксичен, безопасен для окружающей среды.
        /// Немного уступает этиленгликолю по эффективности.
        /// Диапазон концентраций: 10-90%
        /// </remarks>
        Propylene = 1
    }
}
```

---

### 3.3. CollectorType.cs

**Путь:** `src/Models/Hydraulics/CollectorType.cs`

**Содержимое:**
```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Тип коллектора РЕХАУ
    /// </summary>
    /// <remarks>
    /// Определяет коэффициент пропускной способности вентиля (Kv)
    /// и формулу расчёта потерь давления в вентиле.
    /// </remarks>
    public enum CollectorType
    {
        /// <summary>
        /// Бытовой коллектор HKV-D
        /// </summary>
        /// <remarks>
        /// Количество контуров: 2-12
        /// Kv = 1.2 м³/ч
        /// Формула потерь: Δp = (v / 1000 / 1.2)² × 100000 × ρ
        /// Максимальный расход: 1.5 м³/ч
        /// Максимальное давление: 320 мбар
        /// </remarks>
        HKV = 0,
        
        /// <summary>
        /// Промышленный коллектор IV
        /// </summary>
        /// <remarks>
        /// Размеры: 1¼" (DN25) или 1½" (DN40)
        /// Kv = 1.45 м³/ч (1¼") или 1.5 м³/ч (1½")
        /// Формула потерь: Δp = (v / 1000 / Kv)² × 100000 × ρ
        /// </remarks>
        IV = 1
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Models/Hydraulics/EnumsTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class EnumsTests
    {
        [Test]
        public void FlowRegime_HasCorrectValues()
        {
            // Assert
            Assert.That((int)FlowRegime.Laminar, Is.EqualTo(0));
            Assert.That((int)FlowRegime.Transitional, Is.EqualTo(1));
            Assert.That((int)FlowRegime.Turbulent, Is.EqualTo(2));
        }
        
        [Test]
        public void GlycolType_HasCorrectValues()
        {
            // Assert
            Assert.That((int)GlycolType.Ethylene, Is.EqualTo(0));
            Assert.That((int)GlycolType.Propylene, Is.EqualTo(1));
        }
        
        [Test]
        public void CollectorType_HasCorrectValues()
        {
            // Assert
            Assert.That((int)CollectorType.HKV, Is.EqualTo(0));
            Assert.That((int)CollectorType.IV, Is.EqualTo(1));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `FlowRegime.cs` создан с XML-документацией
- [ ] Файл `GlycolType.cs` создан с XML-документацией
- [ ] Файл `CollectorType.cs` создан с XML-документацией
- [ ] Все enum имеют значения 0, 1, 2...
- [ ] XML-комментарии содержат формулы и ссылки на документацию
- [ ] Unit-тесты проходят успешно
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Значения enum начинаются с 0 для совместимости с сериализацией JSON
- XML-документация должна содержать формулы и ссылки на ТЗ
- Все файлы размещаются в `src/Models/Hydraulics/`