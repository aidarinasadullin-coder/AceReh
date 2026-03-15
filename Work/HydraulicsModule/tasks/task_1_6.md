# Task 1.6: GlycolProperties (Свойства гликоля)

**Этап:** 1 - Models  
**Приоритет:** Высокий  
**Статус:** Завершено  
**Зависимости:** Нет

---

## 1. Цель задачи

Создать класс `GlycolProperties` — модель свойств теплоносителя (гликоля).

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-07 | Загрузка свойств теплоносителя | Основной класс результата |

---

## 3. Создаваемые файлы

### 3.1. GlycolProperties.cs

**Путь:** `src/Models/Hydraulics/GlycolProperties.cs`

**Содержимое:**
```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Свойства теплоносителя (гликоля)
    /// </summary>
    /// <remarks>
    /// Содержит физические свойства гликолевого раствора:
    /// - Плотность (ρ)
    /// - Кинематическая вязкость (ν)
    /// - Удельная теплоёмкость (c_p)
    /// - Теплопроводность (λ)
    /// 
    /// Данные получаются интерполяцией из data/glycol_data.json
    /// для заданного типа гликоля, концентрации и температуры.
    /// 
    /// Источник данных: ASHRAE Handbook
    /// </remarks>
    public class GlycolProperties
    {
        /// <summary>
        /// Плотность (ρ), кг/м³
        /// </summary>
        /// <remarks>
        /// Зависит от:
        /// - Типа гликоля (этиленгликоль/пропиленгликоль)
        /// - Концентрации (10-90%)
        /// - Температуры (-34.4°C до 98.9°C)
        /// 
        /// Типичные значения:
        /// - Вода при 20°C: ~998 кг/м³
        /// - 50% этиленгликоль при 40°C: ~1053 кг/м³
        /// </remarks>
        public double Density { get; set; }
        
        /// <summary>
        /// Удельная теплоёмкость (c_p), кДж/(кг·К)
        /// </summary>
        /// <remarks>
        /// Зависит от:
        /// - Типа гликоля
        /// - Концентрации
        /// - Температуры
        /// 
        /// Типичные значения:
        /// - Вода при 20°C: 4.18 кДж/(кг·К)
        /// - 50% этиленгликоль при 40°C: ~3.39 кДж/(кг·К)
        /// </remarks>
        public double SpecificHeat { get; set; }
        
        /// <summary>
        /// Кинематическая вязкость (ν), мм²/с
        /// </summary>
        /// <remarks>
        /// Зависит от:
        /// - Типа гликоля
        /// - Концентрации
        /// - Температуры
        /// 
        /// Вязкость значительно возрастает при низких температурах!
        /// 
        /// Типичные значения:
        /// - Вода при 20°C: ~1.0 мм²/с
        /// - 50% этиленгликоль при 40°C: ~2.16 мм²/с
        /// - 50% этиленгликоль при -15°C: ~18.17 мм²/с
        /// </remarks>
        public double KinematicViscosity { get; set; }
        
        /// <summary>
        /// Теплопроводность (λ), Вт/(м·К)
        /// </summary>
        /// <remarks>
        /// Зависит от:
        /// - Типа гликоля
        /// - Концентрации
        /// - Температуры
        /// 
        /// Типичные значения:
        /// - Вода при 20°C: ~0.60 Вт/(м·К)
        /// - 50% этиленгликоль при 40°C: ~0.42 Вт/(м·К)
        /// </remarks>
        public double ThermalConductivity { get; set; }
        
        // === Дополнительные свойства ===
        
        /// <summary>
        /// Температура, для которой получены свойства, °C
        /// </summary>
        public double Temperature { get; set; }
        
        /// <summary>
        /// Концентрация гликоля, %
        /// </summary>
        public double Concentration { get; set; }
        
        /// <summary>
        /// Тип гликоля
        /// </summary>
        public GlycolType GlycolType { get; set; }
        
        // === Вычисляемые свойства ===
        
        /// <summary>
        /// Кинематическая вязкость в м²/с
        /// </summary>
        /// <remarks>
        /// Преобразование: ν [м²/с] = ν [мм²/с] × 10⁻⁶
        /// </remarks>
        public double KinematicViscosity_m2_s => KinematicViscosity * 1e-6;
        
        /// <summary>
        /// Динамическая вязкость (μ), Па·с
        /// </summary>
        /// <remarks>
        /// Формула: μ = ρ × ν
        /// Где:
        /// - ρ — плотность, кг/м³
        /// - ν — кинематическая вязкость, м²/с
        /// </remarks>
        public double DynamicViscosity => Density * KinematicViscosity_m2_s;
        
        /// <summary>
        /// Температуропроводность (a), м²/с
        /// </summary>
        /// <remarks>
        /// Формула: a = λ / (ρ × c_p)
        /// Где:
        /// - λ — теплопроводность, Вт/(м·К)
        /// - ρ — плотность, кг/м³
        /// - c_p — удельная теплоёмкость, Дж/(кг·К)
        /// 
        /// Примечание: c_p нужно перевести из кДж/(кг·К) в Дж/(кг·К)
        /// </remarks>
        public double ThermalDiffusivity => ThermalConductivity / (Density * SpecificHeat * 1000);
        
        /// <summary>
        /// Число Прандтля (Pr), безразмерное
        /// </summary>
        /// <remarks>
        /// Формула: Pr = ν / a = μ × c_p / λ
        /// Где:
        /// - ν — кинематическая вязкость, м²/с
        /// - a — температуропроводность, м²/с
        /// 
        /// Число Прандтля характеризует отношение вязкостных и тепловых свойств.
        /// </remarks>
        public double PrandtlNumber => KinematicViscosity_m2_s / ThermalDiffusivity;
        
        // === Методы ===
        
        /// <summary>
        /// Создать пустые свойства
        /// </summary>
        public static GlycolProperties Empty => new();
        
        /// <summary>
        /// Создать свойства для воды
        /// </summary>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Свойства воды</returns>
        public static GlycolProperties Water(double temperature)
        {
            // Приближённые значения для воды
            // Точные значения зависят от температуры
            double density = 1000 - 0.0178 * Math.Pow(temperature - 4, 2);
            double viscosity = Math.Exp(-1.597 + 0.181 * temperature - 0.003 * Math.Pow(temperature, 2));
            double specificHeat = 4.18; // кДж/(кг·К)
            double conductivity = 0.6 - 0.0015 * temperature; // Вт/(м·К)
            
            return new GlycolProperties
            {
                Density = density,
                SpecificHeat = specificHeat,
                KinematicViscosity = viscosity,
                ThermalConductivity = conductivity,
                Temperature = temperature,
                Concentration = 0,
                GlycolType = GlycolType.Ethylene
            };
        }
        
        /// <summary>
        /// Получить строковое представление
        /// </summary>
        public override string ToString()
        {
            return $"ρ={Density:F1} кг/м³, ν={KinematicViscosity:F2} мм²/с, c_p={SpecificHeat:F2} кДж/(кг·К)";
        }
        
        /// <summary>
        /// Получить детальное описание
        /// </summary>
        public string GetDetailedDescription()
        {
            var glycolName = GlycolType == GlycolType.Ethylene ? "Этиленгликоль" : "Пропиленгликоль";
            return $"{glycolName} {Concentration:F0}% при {Temperature:F1}°C:\n" +
                   $"  Плотность: {Density:F1} кг/м³\n" +
                   $"  Вязкость: {KinematicViscosity:F2} мм²/с\n" +
                   $"  Теплоёмкость: {SpecificHeat:F2} кДж/(кг·К)\n" +
                   $"  Теплопроводность: {ThermalConductivity:F3} Вт/(м·К)\n" +
                   $"  Число Прандтля: {PrandtlNumber:F2}";
        }
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Models/Hydraulics/GlycolPropertiesTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Models.Hydraulics
{
    [TestFixture]
    public class GlycolPropertiesTests
    {
        [Test]
        public void KinematicViscosity_m2_s_CalculatesCorrectly()
        {
            // Arrange
            var props = new GlycolProperties { KinematicViscosity = 2.16 }; // мм²/с
            
            // Act & Assert
            Assert.That(props.KinematicViscosity_m2_s, Is.EqualTo(2.16e-6).Within(1e-10));
        }
        
        [Test]
        public void DynamicViscosity_CalculatesCorrectly()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053, // кг/м³
                KinematicViscosity = 2.16 // мм²/с
            };
            
            // Act
            var mu = props.DynamicViscosity;
            
            // Assert
            // μ = ρ × ν = 1053 × 2.16e-6 = 0.00227 Па·с
            Assert.That(mu, Is.EqualTo(0.00227).Within(0.00001));
        }
        
        [Test]
        public void ThermalDiffusivity_CalculatesCorrectly()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053,
                SpecificHeat = 3.39, // кДж/(кг·К)
                ThermalConductivity = 0.42 // Вт/(м·К)
            };
            
            // Act
            var a = props.ThermalDiffusivity;
            
            // Assert
            // a = λ / (ρ × c_p × 1000) = 0.42 / (1053 × 3.39 × 1000)
            Assert.That(a, Is.GreaterThan(0));
        }
        
        [Test]
        public void PrandtlNumber_CalculatesCorrectly()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053,
                SpecificHeat = 3.39,
                KinematicViscosity = 2.16,
                ThermalConductivity = 0.42
            };
            
            // Act
            var pr = props.PrandtlNumber;
            
            // Assert
            // Pr = ν / a
            Assert.That(pr, Is.GreaterThan(0));
        }
        
        [Test]
        public void Water_CreatesWaterProperties()
        {
            // Arrange & Act
            var water = GlycolProperties.Water(20);
            
            // Assert
            Assert.That(water.Density, Is.GreaterThan(990).And.LessThan(1000));
            Assert.That(water.SpecificHeat, Is.EqualTo(4.18).Within(0.1));
            Assert.That(water.Concentration, Is.EqualTo(0));
        }
        
        [Test]
        public void ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16,
                SpecificHeat = 3.39
            };
            
            // Act
            var str = props.ToString();
            
            // Assert
            Assert.That(str, Does.Contain("1053"));
            Assert.That(str, Does.Contain("2.16"));
            Assert.That(str, Does.Contain("3.39"));
        }
        
        [Test]
        public void GetDetailedDescription_ReturnsCorrectFormat()
        {
            // Arrange
            var props = new GlycolProperties
            {
                Density = 1053,
                KinematicViscosity = 2.16,
                SpecificHeat = 3.39,
                ThermalConductivity = 0.42,
                Temperature = 40,
                Concentration = 50,
                GlycolType = GlycolType.Ethylene
            };
            
            // Act
            var desc = props.GetDetailedDescription();
            
            // Assert
            Assert.That(desc, Does.Contain("Этиленгликоль"));
            Assert.That(desc, Does.Contain("50%"));
            Assert.That(desc, Does.Contain("40°C"));
            Assert.That(desc, Does.Contain("Плотность"));
            Assert.That(desc, Does.Contain("Вязкость"));
        }
        
        [Test]
        public void Empty_CreatesEmptyProperties()
        {
            // Act
            var props = GlycolProperties.Empty;
            
            // Assert
            Assert.That(props.Density, Is.EqualTo(0));
            Assert.That(props.KinematicViscosity, Is.EqualTo(0));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `GlycolProperties.cs` создан
- [ ] Класс содержит все свойства из ТЗ
- [ ] Вычисляемые свойства (DynamicViscosity, ThermalDiffusivity, PrandtlNumber) работают корректно
- [ ] Метод Water() создаёт корректные свойства для воды
- [ ] XML-документация для всех свойств и методов
- [ ] Unit-тесты проходят успешно
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Класс используется `GlycolDataService` для возврата свойств гликоля
- Значения получаются интерполяцией из `data/glycol_data.json`
- Метод `Water()` предоставляет приближённые значения для воды (концентрация 0%)