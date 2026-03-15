# Task 2.2: IGlycolDataService (Интерфейс сервиса гликолей)

**Этап:** 2 - Interfaces  
**Приоритет:** Высокий  
**Статус:** Completed  
**Зависимости:** Task 1.1, Task 1.6

---

## 1. Цель задачи

Создать интерфейс `IGlycolDataService` — контракт для сервиса свойств гликолей.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-07 | Загрузка свойств теплоносителя | Все методы интерфейса |

---

## 3. Создаваемые файлы

### 3.1. IGlycolDataService.cs

**Путь:** `src/Services/Hydraulics/IGlycolDataService.cs`

**Содержимое:**
```csharp
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Интерфейс сервиса для получения свойств гликолей
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для получения физических свойств гликолевого раствора:
    /// - Плотность (ρ)
    /// - Кинематическая вязкость (ν)
    /// - Удельная теплоёмкость (c_p)
    /// - Теплопроводность (λ)
    /// 
    /// Данные получаются интерполяцией из data/glycol_data.json
    /// для заданного типа гликоля, концентрации и температуры.
    /// 
    /// Источник данных: ASHRAE Handbook
    /// Диапазон температур: -34.4°C до 98.9°C
    /// Диапазон концентраций: 10% до 90%
    /// </remarks>
    public interface IGlycolDataService
    {
        /// <summary>
        /// Получить плотность гликоля
        /// </summary>
        /// <param name="glycolType">Тип гликоля (этиленгликоль/пропиленгликоль)</param>
        /// <param name="concentration">Концентрация, % (объёмные)</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Плотность, кг/м³</returns>
        /// <remarks>
        /// Интерполяция между значениями из таблицы glycol_data.json.
        /// 
        /// Типичные значения:
        /// - Вода (0%): ~998 кг/м³ при 20°C
        /// - 50% этиленгликоль: ~1053 кг/м³ при 40°C
        /// - 50% пропиленгликоль: ~1040 кг/м³ при 40°C
        /// </remarks>
        double GetDensity(GlycolType glycolType, double concentration, double temperature);
        
        /// <summary>
        /// Получить удельную теплоёмкость гликоля
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Удельная теплоёмкость, кДж/(кг·К)</returns>
        /// <remarks>
        /// Интерполяция между значениями из таблицы glycol_data.json.
        /// 
        /// Типичные значения:
        /// - Вода (0%): ~4.18 кДж/(кг·К) при 20°C
        /// - 50% этиленгликоль: ~3.39 кДж/(кг·К) при 40°C
        /// - 50% пропиленгликоль: ~3.50 кДж/(кг·К) при 40°C
        /// </remarks>
        double GetSpecificHeat(GlycolType glycolType, double concentration, double temperature);
        
        /// <summary>
        /// Получить кинематическую вязкость гликоля
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Кинематическая вязкость, мм²/с</returns>
        /// <remarks>
        /// Интерполяция между значениями из таблицы glycol_data.json.
        /// 
        /// ВАЖНО: Вязкость значительно возрастает при низких температурах!
        /// 
        /// Типичные значения:
        /// - Вода (0%): ~1.0 мм²/с при 20°C
        /// - 50% этиленгликоль при 40°C: ~2.16 мм²/с
        /// - 50% этиленгликоль при -15°C: ~18.17 мм²/с
        /// </remarks>
        double GetKinematicViscosity(GlycolType glycolType, double concentration, double temperature);
        
        /// <summary>
        /// Получить теплопроводность гликоля
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Теплопроводность, Вт/(м·К)</returns>
        /// <remarks>
        /// Интерполяция между значениями из таблицы glycol_data.json.
        /// 
        /// Типичные значения:
        /// - Вода (0%): ~0.60 Вт/(м·К) при 20°C
        /// - 50% этиленгликоль: ~0.42 Вт/(м·К) при 40°C
        /// </remarks>
        double GetThermalConductivity(GlycolType glycolType, double concentration, double temperature);
        
        /// <summary>
        /// Получить все свойства гликоля
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Объект со всеми свойствами гликоля</returns>
        /// <remarks>
        /// Возвращает объект GlycolProperties со всеми свойствами:
        /// - Density
        /// - SpecificHeat
        /// - KinematicViscosity
        /// - ThermalConductivity
        /// 
        /// Это более эффективный способ, чем вызов отдельных методов,
        /// так как интерполяция выполняется один раз.
        /// </remarks>
        GlycolProperties GetProperties(GlycolType glycolType, double concentration, double temperature);
        
        /// <summary>
        /// Проверить, поддерживается ли температура
        /// </summary>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>true, если температура в допустимом диапазоне</returns>
        /// <remarks>
        /// Диапазон температур: -34.4°C до 98.9°C
        /// При температуре вне диапазона используется экстраполяция.
        /// </remarks>
        bool IsTemperatureSupported(double temperature);
        
        /// <summary>
        /// Проверить, поддерживается ли концентрация
        /// </summary>
        /// <param name="concentration">Концентрация, %</param>
        /// <returns>true, если концентрация в допустимом диапазоне</returns>
        /// <remarks>
        /// Диапазон концентраций: 10% до 90%
        /// При концентрации вне диапазона используется экстраполяция.
        /// </remarks>
        bool IsConcentrationSupported(double concentration);
        
        /// <summary>
        /// Получить минимальную поддерживаемую температуру
        /// </summary>
        /// <returns>Минимальная температура, °C</returns>
        double GetMinTemperature();
        
        /// <summary>
        /// Получить максимальную поддерживаемую температуру
        /// </summary>
        /// <returns>Максимальная температура, °C</returns>
        double GetMaxTemperature();
        
        /// <summary>
        /// Получить минимальную поддерживаемую концентрацию
        /// </summary>
        /// <returns>Минимальная концентрация, %</returns>
        double GetMinConcentration();
        
        /// <summary>
        /// Получить максимальную поддерживаемую концентрацию
        /// </summary>
        /// <returns>Максимальная концентрация, %</returns>
        double GetMaxConcentration();
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты (интерфейс)

**Файл:** `tests/Services/Hydraulics/IGlycolDataServiceTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;
using Moq;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    [TestFixture]
    public class IGlycolDataServiceTests
    {
        private Mock<IGlycolDataService> _serviceMock;
        
        [SetUp]
        public void Setup()
        {
            _serviceMock = new Mock<IGlycolDataService>();
        }
        
        [Test]
        public void GetDensity_ReturnsCorrectValue()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetDensity(GlycolType.Ethylene, 50, 40))
                .Returns(1053.0);
            
            // Act
            var result = _serviceMock.Object.GetDensity(GlycolType.Ethylene, 50, 40);
            
            // Assert
            Assert.That(result, Is.EqualTo(1053.0).Within(0.1));
        }
        
        [Test]
        public void GetSpecificHeat_ReturnsCorrectValue()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetSpecificHeat(GlycolType.Ethylene, 50, 40))
                .Returns(3.39);
            
            // Act
            var result = _serviceMock.Object.GetSpecificHeat(GlycolType.Ethylene, 50, 40);
            
            // Assert
            Assert.That(result, Is.EqualTo(3.39).Within(0.01));
        }
        
        [Test]
        public void GetKinematicViscosity_ReturnsCorrectValue()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetKinematicViscosity(GlycolType.Ethylene, 50, 40))
                .Returns(2.16);
            
            // Act
            var result = _serviceMock.Object.GetKinematicViscosity(GlycolType.Ethylene, 50, 40);
            
            // Assert
            Assert.That(result, Is.EqualTo(2.16).Within(0.01));
        }
        
        [Test]
        public void GetProperties_ReturnsAllProperties()
        {
            // Arrange
            var expectedProps = new GlycolProperties
            {
                Density = 1053,
                SpecificHeat = 3.39,
                KinematicViscosity = 2.16,
                ThermalConductivity = 0.42,
                Temperature = 40,
                Concentration = 50,
                GlycolType = GlycolType.Ethylene
            };
            
            _serviceMock
                .Setup(s => s.GetProperties(GlycolType.Ethylene, 50, 40))
                .Returns(expectedProps);
            
            // Act
            var result = _serviceMock.Object.GetProperties(GlycolType.Ethylene, 50, 40);
            
            // Assert
            Assert.That(result.Density, Is.EqualTo(1053));
            Assert.That(result.SpecificHeat, Is.EqualTo(3.39));
            Assert.That(result.KinematicViscosity, Is.EqualTo(2.16));
            Assert.That(result.ThermalConductivity, Is.EqualTo(0.42));
        }
        
        [Test]
        public void IsTemperatureSupported_ReturnsTrueForValidTemperature()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.IsTemperatureSupported(40))
                .Returns(true);
            _serviceMock
                .Setup(s => s.IsTemperatureSupported(-50))
                .Returns(false);
            
            // Act & Assert
            Assert.That(_serviceMock.Object.IsTemperatureSupported(40), Is.True);
            Assert.That(_serviceMock.Object.IsTemperatureSupported(-50), Is.False);
        }
        
        [Test]
        public void IsConcentrationSupported_ReturnsTrueForValidConcentration()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.IsConcentrationSupported(50))
                .Returns(true);
            _serviceMock
                .Setup(s => s.IsConcentrationSupported(5))
                .Returns(false);
            
            // Act & Assert
            Assert.That(_serviceMock.Object.IsConcentrationSupported(50), Is.True);
            Assert.That(_serviceMock.Object.IsConcentrationSupported(5), Is.False);
        }
        
        [Test]
        public void GetMinTemperature_ReturnsCorrectValue()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetMinTemperature())
                .Returns(-34.4);
            
            // Act
            var result = _serviceMock.Object.GetMinTemperature();
            
            // Assert
            Assert.That(result, Is.EqualTo(-34.4).Within(0.1));
        }
        
        [Test]
        public void GetMaxTemperature_ReturnsCorrectValue()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetMaxTemperature())
                .Returns(98.9);
            
            // Act
            var result = _serviceMock.Object.GetMaxTemperature();
            
            // Assert
            Assert.That(result, Is.EqualTo(98.9).Within(0.1));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `IGlycolDataService.cs` создан
- [ ] Интерфейс содержит все методы из ТЗ
- [ ] Все методы имеют XML-документацию
- [ ] Интерфейс ссылается на `GlycolProperties` и `GlycolType`
- [ ] Unit-тесты с Mock проходят успешно
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Интерфейс должен быть независимым от реализации
- Методы должны поддерживать интерполяцию и экстраполяцию
- Диапазоны температур и концентраций должны быть документированы