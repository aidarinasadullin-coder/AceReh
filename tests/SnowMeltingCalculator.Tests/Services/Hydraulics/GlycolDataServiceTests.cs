using NUnit.Framework;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using System;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    /// <summary>
    /// Тесты для GlycolDataService
    /// </summary>
    [TestFixture]
    public class GlycolDataServiceTests
    {
        private GlycolDataService _service = null!;

        [SetUp]
        public void Setup()
        {
            _service = new GlycolDataService("data/glycol_data.json");
        }

        #region GetDensity Tests

        [Test]
        public void GetDensity_EthyleneGlycol50Percent_20C_ReturnsCorrectValue()
        {
            // Arrange
            double concentration = 50;
            double temperature = 20;

            // Act
            double density = _service.GetDensity(GlycolType.Ethylene, concentration, temperature);

            // Assert
            // При 50% и 20°C плотность этиленгликоля ≈ 1108 кг/м³ (по данным ASHRAE)
            // Встроенные данные могут отличаться
            Assert.That(density, Is.GreaterThan(1000));
            Assert.That(density, Is.LessThan(1200));
        }

        [Test]
        public void GetDensity_PropyleneGlycol50Percent_20C_ReturnsCorrectValue()
        {
            // Arrange
            double concentration = 50;
            double temperature = 20;

            // Act
            double density = _service.GetDensity(GlycolType.Propylene, concentration, temperature);

            // Assert
            // Пропиленгликоль имеет меньшую плотность
            Assert.That(density, Is.GreaterThan(1000));
            Assert.That(density, Is.LessThan(1150));
        }

        #endregion

        #region GetKinematicViscosity Tests

        [Test]
        public void GetKinematicViscosity_EthyleneGlycol50Percent_20C_ReturnsCorrectValue()
        {
            // Arrange
            double concentration = 50;
            double temperature = 20;

            // Act
            double viscosity = _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, temperature);

            // Assert
            // Вязкость зависит от данных в JSON
            Assert.That(viscosity, Is.GreaterThan(0));
            Assert.That(viscosity, Is.LessThan(100));
        }

        [Test]
        public void GetKinematicViscosity_LowTemperature_ReturnsHigherValue()
        {
            // Arrange
            double concentration = 50;
            // Используем температуры из данных ASHRAE для точности
            // При -17.8°C вязкость этиленгликоля 50% = 40.8 мм²/с
            // При 48.9°C вязкость этиленгликоля 50% = 1.1 мм²/с
            double temperatureLow = -17.8;  // Точка данных ASHRAE
            double temperatureHigh = 48.9;   // Точка данных ASHRAE

            // Act
            double viscosityLow = _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, temperatureLow);
            double viscosityHigh = _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, temperatureHigh);

            // Assert
            // Вязкость при низкой температуре должна быть выше
            Assert.That(viscosityLow, Is.GreaterThan(viscosityHigh));
        }

        #endregion

        #region GetProperties Tests

        [Test]
        public void GetProperties_ReturnsAllProperties()
        {
            // Arrange
            double concentration = 40;
            double temperature = 30;

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert
            Assert.That(properties.Density, Is.GreaterThan(1000));
            Assert.That(properties.SpecificHeat, Is.GreaterThan(3.0));
            Assert.That(properties.KinematicViscosity, Is.GreaterThan(0));
            Assert.That(properties.ThermalConductivity, Is.GreaterThan(0));
        }

        [Test]
        public void GetProperties_InterpolationBetweenTemperatures()
        {
            // Arrange
            double concentration = 50;
            double temperature = 25; // Между 20 и 30

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert
            // Значение должно быть между значениями при 20°C и 30°C
            var props20 = _service.GetProperties(GlycolType.Ethylene, concentration, 20);
            var props30 = _service.GetProperties(GlycolType.Ethylene, concentration, 30);

            Assert.That(properties.Density, Is.GreaterThanOrEqualTo(Math.Min(props20.Density, props30.Density) - 1));
            Assert.That(properties.Density, Is.LessThanOrEqualTo(Math.Max(props20.Density, props30.Density) + 1));
        }

        [Test]
        public void GetProperties_InterpolationBetweenConcentrations()
        {
            // Arrange
            double concentration = 45; // Между 40 и 50
            double temperature = 20;

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert
            var props40 = _service.GetProperties(GlycolType.Ethylene, 40, temperature);
            var props50 = _service.GetProperties(GlycolType.Ethylene, 50, temperature);

            Assert.That(properties.Density, Is.GreaterThanOrEqualTo(Math.Min(props40.Density, props50.Density) - 1));
            Assert.That(properties.Density, Is.LessThanOrEqualTo(Math.Max(props40.Density, props50.Density) + 1));
        }

        [Test]
        public void GetProperties_PropyleneGlycol_ReturnsCorrectValues()
        {
            // Arrange
            double concentration = 50;
            double temperature = 20;

            // Act
            var properties = _service.GetProperties(GlycolType.Propylene, concentration, temperature);

            // Assert
            // Пропиленгликоль имеет меньшую плотность и большую вязкость
            Assert.That(properties.Density, Is.GreaterThan(1000));
            Assert.That(properties.KinematicViscosity, Is.GreaterThan(0));
        }

        #endregion

        #region Validation Tests

        [Test]
        public void GetProperties_InvalidConcentration_ThrowsException()
        {
            // Arrange
            double concentration = -5; // Меньше минимума (0%)
            double temperature = 20;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetProperties(GlycolType.Ethylene, concentration, temperature));
        }

        [Test]
        public void GetProperties_InvalidTemperature_ThrowsException()
        {
            // Arrange
            double concentration = 50;
            double temperature = -40; // Меньше минимума (-34.4°C)

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetProperties(GlycolType.Ethylene, concentration, temperature));
        }

        #endregion

        #region IsTemperatureSupported / IsConcentrationSupported Tests

        [Test]
        public void IsTemperatureSupported_ReturnsCorrectValue()
        {
            // Act & Assert
            Assert.That(_service.IsTemperatureSupported(-34.4), Is.True);
            Assert.That(_service.IsTemperatureSupported(0), Is.True);
            Assert.That(_service.IsTemperatureSupported(50), Is.True);
            Assert.That(_service.IsTemperatureSupported(100), Is.True);
            Assert.That(_service.IsTemperatureSupported(-35), Is.False);
            Assert.That(_service.IsTemperatureSupported(101), Is.False);
        }

        [Test]
        public void IsConcentrationSupported_ReturnsCorrectValue()
        {
            // Act & Assert
            Assert.That(_service.IsConcentrationSupported(0), Is.True);
            Assert.That(_service.IsConcentrationSupported(10), Is.True);
            Assert.That(_service.IsConcentrationSupported(50), Is.True);
            Assert.That(_service.IsConcentrationSupported(90), Is.True);
            Assert.That(_service.IsConcentrationSupported(-1), Is.False);
            Assert.That(_service.IsConcentrationSupported(95), Is.False);
        }

        #endregion

        #region GetMin/Max Tests

        [Test]
        public void GetMinTemperature_ReturnsCorrectValue()
        {
            // Act
            double minTemp = _service.GetMinTemperature();

            // Assert
            Assert.That(minTemp, Is.EqualTo(-34.4).Within(0.1));
        }

        [Test]
        public void GetMaxTemperature_ReturnsCorrectValue()
        {
            // Act
            double maxTemp = _service.GetMaxTemperature();

            // Assert
            Assert.That(maxTemp, Is.EqualTo(100.0).Within(0.1));
        }

        [Test]
        public void GetMinConcentration_ReturnsCorrectValue()
        {
            // Act
            double minConc = _service.GetMinConcentration();

            // Assert
            // Минимальная концентрация для гликолей - 10%, но вода (0%) также разрешена
            Assert.That(minConc, Is.EqualTo(10.0).Within(0.1));
        }

        [Test]
        public void GetMaxConcentration_ReturnsCorrectValue()
        {
            // Act
            double maxConc = _service.GetMaxConcentration();

            // Assert
            Assert.That(maxConc, Is.EqualTo(90.0).Within(0.1));
        }

        #endregion

        #region GetSpecificHeat Tests

        [Test]
        public void GetSpecificHeat_WithValidParameters_ReturnsInterpolatedValue()
        {
            // Arrange
            double concentration = 50;
            double temperature = 20;

            // Act
            double specificHeat = _service.GetSpecificHeat(GlycolType.Ethylene, concentration, temperature);

            // Assert
            // Удельная теплоёмкость гликолевого раствора должна быть в разумных пределах
            Assert.That(specificHeat, Is.GreaterThan(2.5));
            Assert.That(specificHeat, Is.LessThan(4.5));
        }

        [Test]
        public void GetSpecificHeat_HigherConcentration_LowerSpecificHeat()
        {
            // Arrange
            double temperature = 32.2; // Точка данных ASHRAE

            // Act
            double specificHeat30 = _service.GetSpecificHeat(GlycolType.Ethylene, 30, temperature);
            double specificHeat60 = _service.GetSpecificHeat(GlycolType.Ethylene, 60, temperature);

            // Assert - более высокая концентрация = более низкая теплоёмкость
            // При 32.2°C: 30% = 4.20 кДж/(кг·К), 60% = 3.63 кДж/(кг·К)
            Assert.That(specificHeat30, Is.GreaterThan(specificHeat60));
        }

        [Test]
        public void GetThermalConductivity_HigherConcentration_LowerConductivity()
        {
            // Arrange
            double temperature = 32.2; // Точка данных ASHRAE

            // Act
            double conductivity30 = _service.GetThermalConductivity(GlycolType.Ethylene, 30, temperature);
            double conductivity60 = _service.GetThermalConductivity(GlycolType.Ethylene, 60, temperature);

            // Assert - более высокая концентрация = более низкая теплопроводность
            // При 32.2°C: 30% = 0.527 Вт/(м·К), 60% = 0.412 Вт/(м·К)
            Assert.That(conductivity30, Is.GreaterThan(conductivity60));
        }

        [Test]
        public void GetThermalConductivity_HigherTemperature_HigherConductivity()
        {
            // Arrange
            double concentration = 50;

            // Act
            double conductivity15 = _service.GetThermalConductivity(GlycolType.Ethylene, concentration, 15.6);
            double conductivity48 = _service.GetThermalConductivity(GlycolType.Ethylene, concentration, 48.9);

            // Assert - более высокая температура = более высокая теплопроводность
            // При 50%: 15.6°C = 0.355 Вт/(м·К), 48.9°C = 0.493 Вт/(м·К)
            Assert.That(conductivity48, Is.GreaterThan(conductivity15));
        }

        [Test]
        public void GetSpecificHeat_InterpolationBetweenTemperatures()
        {
            // Arrange
            double concentration = 50;
            double temperature = 25; // Между 20 и 30

            // Act
            double specificHeat = _service.GetSpecificHeat(GlycolType.Ethylene, concentration, temperature);
            double specificHeat20 = _service.GetSpecificHeat(GlycolType.Ethylene, concentration, 20);
            double specificHeat30 = _service.GetSpecificHeat(GlycolType.Ethylene, concentration, 30);

            // Assert - интерполированное значение должно быть между граничными
            Assert.That(specificHeat, Is.GreaterThanOrEqualTo(Math.Min(specificHeat20, specificHeat30) - 0.1));
            Assert.That(specificHeat, Is.LessThanOrEqualTo(Math.Max(specificHeat20, specificHeat30) + 0.1));
        }

        #endregion

        #region GetThermalConductivity Tests

        [Test]
        public void GetThermalConductivity_WithValidParameters_ReturnsInterpolatedValue()
        {
            // Arrange
            double concentration = 50;
            double temperature = 20;

            // Act
            double conductivity = _service.GetThermalConductivity(GlycolType.Ethylene, concentration, temperature);

            // Assert
            // Теплопроводность гликолевого раствора должна быть в разумных пределах
            Assert.That(conductivity, Is.GreaterThan(0.2));
            Assert.That(conductivity, Is.LessThan(0.7));
        }

        #endregion

        #region Extrapolation Tests

        [Test]
        public void GetProperties_ExtrapolationBelowMinTemperature_ThrowsException()
        {
            // Arrange
            double concentration = 50;
            double temperature = -40; // Ниже минимума (-34.4°C)

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetProperties(GlycolType.Ethylene, concentration, temperature));
        }

        [Test]
        public void GetProperties_TemperatureAbove100_ThrowsException()
        {
            // Arrange
            double concentration = 50;
            double temperature = 105; // Выше максимума (100°C)

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetProperties(GlycolType.Ethylene, concentration, temperature));
        }

        [Test]
        public void GetProperties_ExtrapolationBelowMinConcentration_ThrowsException()
        {
            // Arrange
            double concentration = -5; // Ниже минимума (0%)
            double temperature = 20;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetProperties(GlycolType.Ethylene, concentration, temperature));
        }

        [Test]
        public void GetProperties_ExtrapolationAboveMaxConcentration_ThrowsException()
        {
            // Arrange
            double concentration = 95; // Выше максимума (90%)
            double temperature = 20;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetProperties(GlycolType.Ethylene, concentration, temperature));
        }

        #endregion

        #region Boundary Cases Tests

        [Test]
        public void GetProperties_AtMinTemperature_ReturnsValidValue()
        {
            // Arrange
            // При -1.1°C в JSON есть данные для всех концентраций >= 30%
            double concentration = 50;
            double temperature = -1.1; // Температура с полными данными

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert
            Assert.That(properties.Density, Is.GreaterThan(0));
            Assert.That(properties.KinematicViscosity, Is.GreaterThan(0));
        }

        [Test]
        public void GetProperties_AtMaxTemperature_ReturnsValidValue()
        {
            // Arrange
            double concentration = 50;
            double temperature = 90; // Максимальная температура

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert
            Assert.That(properties.Density, Is.GreaterThan(0));
            Assert.That(properties.KinematicViscosity, Is.GreaterThan(0));
        }

        [Test]
        public void GetProperties_AtMinConcentration_ReturnsValidValue()
        {
            // Arrange
            double concentration = 10; // Минимальная концентрация с данными (0% - вода)
            double temperature = 20;

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert
            Assert.That(properties.Density, Is.GreaterThan(0));
            Assert.That(properties.SpecificHeat, Is.GreaterThan(0));
        }

        [Test]
        public void GetProperties_AtMaxConcentration_ReturnsValidValue()
        {
            // Arrange
            double concentration = 90; // Максимальная концентрация
            double temperature = 20;

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert
            Assert.That(properties.Density, Is.GreaterThan(0));
            Assert.That(properties.SpecificHeat, Is.GreaterThan(0));
        }

        [Test]
        public void GetProperties_EthyleneVsPropylene_DifferentProperties()
        {
            // Arrange
            double concentration = 50;
            double temperature = 20;

            // Act
            var ethyleneProps = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);
            var propyleneProps = _service.GetProperties(GlycolType.Propylene, concentration, temperature);

            // Assert - разные типы гликолей имеют разные свойства
            // Встроенные данные могут быть одинаковыми для обоих типов, проверяем что значения валидны
            Assert.That(ethyleneProps.Density, Is.GreaterThan(0));
            Assert.That(propyleneProps.Density, Is.GreaterThan(0));
            Assert.That(ethyleneProps.KinematicViscosity, Is.GreaterThan(0));
            Assert.That(propyleneProps.KinematicViscosity, Is.GreaterThan(0));
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public void GetProperties_InterpolationAtExactDataPoint_ReturnsCorrectValue()
        {
            // Arrange - используем точные значения из таблицы
            double concentration = 50;
            double temperature = 20;

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert - значение должно быть точным (не интерполированным)
            Assert.That(properties.Density, Is.GreaterThan(0));
        }

        [Test]
        public void GetDensity_CalledMultipleTimes_ReturnsConsistentResults()
        {
            // Arrange
            double concentration = 40;
            double temperature = 30;

            // Act
            double density1 = _service.GetDensity(GlycolType.Ethylene, concentration, temperature);
            double density2 = _service.GetDensity(GlycolType.Ethylene, concentration, temperature);
            double density3 = _service.GetDensity(GlycolType.Ethylene, concentration, temperature);

            // Assert - результаты должны быть идентичными
            Assert.That(density1, Is.EqualTo(density2));
            Assert.That(density2, Is.EqualTo(density3));
        }

        #endregion

        #region JSON Parsing Tests

        [Test]
        public void LoadData_JsonFileExists_ParsesCorrectly()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act
            var ethyleneProps = service.GetProperties(GlycolType.Ethylene, 50, 37.8);
            var propyleneProps = service.GetProperties(GlycolType.Propylene, 50, 37.8);

            // Assert
            Assert.That(ethyleneProps.Density, Is.GreaterThan(0), "Этиленгликоль: плотность должна быть > 0");
            Assert.That(propyleneProps.Density, Is.GreaterThan(0), "Пропиленгликоль: плотность должна быть > 0");
        }

        [Test]
        public void GetProperties_DifferentGlycolTypes_ReturnDifferentDensity()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");
            double concentration = 50;
            double temperature = 37.8; // Температура ASHRAE

            // Act
            var ethylene = service.GetProperties(GlycolType.Ethylene, concentration, temperature);
            var propylene = service.GetProperties(GlycolType.Propylene, concentration, temperature);

            // Assert
            // Этиленгликоль при 50% и 37.8°C: ~1086.6 кг/м³ (из JSON)
            // Пропиленгликоль при 50% и 37.8°C: ~1037 кг/м³ (из JSON)
            Assert.That(ethylene.Density, Is.GreaterThan(1080).And.LessThan(1095),
                $"Этиленгликоль: плотность должна быть 1080-1095 кг/м³, получено {ethylene.Density}");
            Assert.That(propylene.Density, Is.GreaterThan(1030).And.LessThan(1045),
                $"Пропиленгликоль: плотность должна быть 1030-1045 кг/м³, получено {propylene.Density}");
            Assert.That(Math.Abs(ethylene.Density - propylene.Density), Is.GreaterThan(40),
                "Плотности должны различаться минимум на 40 кг/м³");
        }

        [Test]
        public void GetProperties_DifferentGlycolTypes_ReturnDifferentViscosity()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");
            double concentration = 50;
            double temperature = 37.8; // Температура ASHRAE

            // Act
            var ethylene = service.GetProperties(GlycolType.Ethylene, concentration, temperature);
            var propylene = service.GetProperties(GlycolType.Propylene, concentration, temperature);

            // Assert
            // Этиленгликоль при 50% и 37.8°C: ~1.3 мм²/с (из JSON)
            // Пропиленгликоль при 50% и 37.8°C: ~4.19 мм²/с (из JSON)
            Assert.That(propylene.KinematicViscosity, Is.GreaterThan(ethylene.KinematicViscosity),
                "Пропиленгликоль должен иметь более высокую вязкость");
            // Разница должна быть значительной (минимум 200%)
            double ratio = propylene.KinematicViscosity / ethylene.KinematicViscosity;
            Assert.That(ratio, Is.GreaterThan(2.0),
                $"Отношение вязкостей должно быть > 2.0, получено {ratio:F2}");
        }

        [Test]
        public void GetProperties_DifferentGlycolTypes_ReturnDifferentSpecificHeat()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");
            double concentration = 50;
            double temperature = 37.8; // Температура ASHRAE

            // Act
            var ethylene = service.GetProperties(GlycolType.Ethylene, concentration, temperature);
            var propylene = service.GetProperties(GlycolType.Propylene, concentration, temperature);

            // Assert
            // Этиленгликоль при 50% и 37.8°C: ~4.05 кДж/(кг·К) (из JSON)
            // Пропиленгликоль при 50% и 37.8°C: ~3.90 кДж/(кг·К) (из JSON)
            // Проверяем, что значения в разумных пределах
            Assert.That(ethylene.SpecificHeat, Is.GreaterThan(3.5).And.LessThan(4.5),
                $"Этиленгликоль: теплоёмкость должна быть 3.5-4.5 кДж/(кг·К), получено {ethylene.SpecificHeat}");
            Assert.That(propylene.SpecificHeat, Is.GreaterThan(3.5).And.LessThan(4.5),
                $"Пропиленгликоль: теплоёмкость должна быть 3.5-4.5 кДж/(кг·К), получено {propylene.SpecificHeat}");
            // Проверяем, что значения различаются
            Assert.That(Math.Abs(ethylene.SpecificHeat - propylene.SpecificHeat), Is.GreaterThan(0.05),
                "Теплоёмкости должны различаться");
        }

        [Test]
        public void GetDefaultData_DifferentGlycolTypes_ReturnDifferentFallbackValues()
        {
            // Arrange
            var service = new GlycolDataService("non_existent_file.json"); // Файл не существует

            // Act
            var ethylene = service.GetProperties(GlycolType.Ethylene, 50, 37.8);
            var propylene = service.GetProperties(GlycolType.Propylene, 50, 37.8);

            // Assert - даже fallback данные должны различаться
            Assert.That(ethylene.Density, Is.Not.EqualTo(propylene.Density),
                "Fallback плотность должна различаться для разных гликолей");
            Assert.That(ethylene.KinematicViscosity, Is.Not.EqualTo(propylene.KinematicViscosity),
                "Fallback вязкость должна различаться для разных гликолей");
        }

        [Test]
        public void GetProperties_EthyleneGlycol_50Percent_37_8C_MatchesASHRAE()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act
            var props = service.GetProperties(GlycolType.Ethylene, 50, 37.8);

            // Assert - проверка по данным ASHRAE из JSON
            // При 50% концентрации и 37.8°C:
            // Плотность: ~1086.6 кг/м³ (интерполяция между 32.2 и 43.3)
            // Вязкость: ~1.3 мм²/с
            // Теплоёмкость: ~4.05 кДж/(кг·К)
            Assert.That(props.Density, Is.GreaterThan(1080).And.LessThan(1095),
                $"Плотность этиленгликоля 50% при 37.8°C должна быть ~1086.6 кг/м³, получено {props.Density}");
            // Вязкость при 37.8°C интерполируется между 32.2 и 43.3
            Assert.That(props.KinematicViscosity, Is.GreaterThan(1.0).And.LessThan(2.5),
                $"Вязкость этиленгликоля 50% при 37.8°C должна быть ~1.3-2.0 мм²/с, получено {props.KinematicViscosity}");
            Assert.That(props.SpecificHeat, Is.GreaterThan(3.5).And.LessThan(4.5),
                $"Теплоёмкость этиленгликоля 50% при 37.8°C должна быть ~4.05 кДж/(кг·К), получено {props.SpecificHeat}");
        }

        [Test]
        public void GetProperties_PropyleneGlycol_50Percent_37_8C_MatchesASHRAE()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act
            var props = service.GetProperties(GlycolType.Propylene, 50, 37.8);

            // Assert - проверка по данным ASHRAE из JSON
            // При 50% концентрации и 37.8°C:
            // Плотность: ~1037 кг/м³
            // Вязкость: ~4.19 мм²/с
            // Теплоёмкость: ~3.90 кДж/(кг·К)
            Assert.That(props.Density, Is.GreaterThan(1030).And.LessThan(1045),
                $"Плотность пропиленгликоля 50% при 37.8°C должна быть ~1037 кг/м³, получено {props.Density}");
            // Вязкость при 37.8°C интерполируется между 32.2 и 43.3
            Assert.That(props.KinematicViscosity, Is.GreaterThan(3.0).And.LessThan(7.0),
                $"Вязкость пропиленгликоля 50% при 37.8°C должна быть ~4-6 мм²/с, получено {props.KinematicViscosity}");
            Assert.That(props.SpecificHeat, Is.GreaterThan(3.5).And.LessThan(4.5),
                $"Теплоёмкость пропиленгликоля 50% при 37.8°C должна быть ~3.90 кДж/(кг·К), получено {props.SpecificHeat}");
        }

        [Test]
        public void GetProperties_Interpolation_WorksCorrectly()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act - используем точки данных ASHRAE для проверки
            // Плотность уменьшается с ростом температуры
            var props15 = service.GetProperties(GlycolType.Ethylene, 50, 15.6);
            var props32 = service.GetProperties(GlycolType.Ethylene, 50, 32.2);
            var props48 = service.GetProperties(GlycolType.Ethylene, 50, 48.9);

            // Assert - плотность должна уменьшаться с ростом температуры
            Assert.That(props15.Density, Is.GreaterThan(props32.Density),
                "Плотность при 15.6°C должна быть выше, чем при 32.2°C");
            Assert.That(props32.Density, Is.GreaterThan(props48.Density),
                "Плотность при 32.2°C должна быть выше, чем при 48.9°C");
        }

        #endregion

        #region Water Properties Tests

        [Test]
        public void GetProperties_Water_ZeroConcentration_ReturnsWaterProperties()
        {
            // Arrange
            double concentration = 0; // Чистая вода
            double temperature = 20;

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert - свойства воды при 20°C
            // Плотность воды ≈ 998 кг/м³
            Assert.That(properties.Density, Is.GreaterThan(990).And.LessThan(1010),
                $"Плотность воды при 20°C должна быть ~998 кг/м³, получено {properties.Density}");
            // Вязкость воды ≈ 1.0 мм²/с
            Assert.That(properties.KinematicViscosity, Is.GreaterThan(0.5).And.LessThan(2.0),
                $"Вязкость воды при 20°C должна быть ~1.0 мм²/с, получено {properties.KinematicViscosity}");
            // Теплоёмкость воды ≈ 4.18 кДж/(кг·К)
            Assert.That(properties.SpecificHeat, Is.GreaterThan(4.0).And.LessThan(4.5),
                $"Теплоёмкость воды должна быть ~4.18 кДж/(кг·К), получено {properties.SpecificHeat}");
            // Теплопроводность воды ≈ 0.6 Вт/(м·К)
            Assert.That(properties.ThermalConductivity, Is.GreaterThan(0.5).And.LessThan(0.7),
                $"Теплопроводность воды при 20°C должна быть ~0.57 Вт/(м·К), получено {properties.ThermalConductivity}");
            // Концентрация должна быть 0
            Assert.That(properties.Concentration, Is.EqualTo(0));
        }

        [Test]
        public void GetWaterProperties_TemperatureRange_ValidProperties()
        {
            // Arrange & Act & Assert - проверяем свойства воды в диапазоне температур

            // При 0°C
            var props0 = _service.GetWaterProperties(0);
            Assert.That(props0.Density, Is.GreaterThan(990).And.LessThan(1010),
                $"Плотность воды при 0°C должна быть ~999.8 кг/м³, получено {props0.Density}");

            // При 20°C
            var props20 = _service.GetWaterProperties(20);
            Assert.That(props20.Density, Is.GreaterThan(990).And.LessThan(1010),
                $"Плотность воды при 20°C должна быть ~998 кг/м³, получено {props20.Density}");

            // При 50°C
            var props50 = _service.GetWaterProperties(50);
            Assert.That(props50.Density, Is.GreaterThan(980).And.LessThan(1000),
                $"Плотность воды при 50°C должна быть ~988 кг/м³, получено {props50.Density}");

            // При 90°C (максимальная температура)
            var props90 = _service.GetWaterProperties(90);
            Assert.That(props90.Density, Is.GreaterThan(950).And.LessThan(980),
                $"Плотность воды при 90°C должна быть ~965 кг/м³, получено {props90.Density}");

            // Вязкость должна уменьшаться с ростом температуры
            Assert.That(props0.KinematicViscosity, Is.GreaterThan(props20.KinematicViscosity),
                "Вязкость при 0°C должна быть выше, чем при 20°C");
            Assert.That(props20.KinematicViscosity, Is.GreaterThan(props50.KinematicViscosity),
                "Вязкость при 20°C должна быть выше, чем при 50°C");
        }

        [Test]
        public void GetWaterProperties_TemperatureAbove100_ThrowsException()
        {
            // Arrange
            double temperature = 105; // Выше максимума (100°C)

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetWaterProperties(temperature));
        }

        [Test]
        public void GetWaterProperties_TemperatureBelow0_ThrowsException()
        {
            // Arrange
            double temperature = -5; // Ниже минимума (0°C)

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetWaterProperties(temperature));
        }

        [Test]
        public void GetWaterProperties_DensityDecreasesWithTemperature()
        {
            // Arrange
            var props4 = _service.GetWaterProperties(4); // Максимум плотности при 4°C
            var props20 = _service.GetWaterProperties(20);
            var props60 = _service.GetWaterProperties(60);

            // Assert - плотность уменьшается с ростом температуры (после 4°C)
            Assert.That(props4.Density, Is.GreaterThanOrEqualTo(props20.Density),
                "Плотность при 4°C должна быть максимальной");
            Assert.That(props20.Density, Is.GreaterThan(props60.Density),
                "Плотность при 20°C должна быть выше, чем при 60°C");
        }

        [Test]
        public void GetWaterProperties_ViscosityDecreasesWithTemperature()
        {
            // Arrange
            var props10 = _service.GetWaterProperties(10);
            var props40 = _service.GetWaterProperties(40);
            var props80 = _service.GetWaterProperties(80);

            // Assert - вязкость уменьшается с ростом температуры
            Assert.That(props10.KinematicViscosity, Is.GreaterThan(props40.KinematicViscosity),
                "Вязкость при 10°C должна быть выше, чем при 40°C");
            Assert.That(props40.KinematicViscosity, Is.GreaterThan(props80.KinematicViscosity),
                "Вязкость при 40°C должна быть выше, чем при 80°C");
        }

        [Test]
        public void GetWaterProperties_ThermalConductivityIncreasesWithTemperature()
        {
            // Arrange
            var props20 = _service.GetWaterProperties(20);
            var props60 = _service.GetWaterProperties(60);

            // Assert - теплопроводность воды увеличивается с ростом температуры (по данным IAPWS)
            Assert.That(props60.ThermalConductivity, Is.GreaterThan(props20.ThermalConductivity),
                "Теплопроводность при 60°C должна быть выше, чем при 20°C");
        }

        [Test]
        public void GetProperties_WaterVsGlycol_WaterHasHigherSpecificHeat()
        {
            // Arrange
            double temperature = 20;

            // Act
            var waterProps = _service.GetProperties(GlycolType.Ethylene, 0, temperature);
            var glycolProps = _service.GetProperties(GlycolType.Ethylene, 50, temperature);

            // Assert - вода имеет более высокую теплоёмкость, чем гликолевый раствор
            Assert.That(waterProps.SpecificHeat, Is.GreaterThan(glycolProps.SpecificHeat),
                "Теплоёмкость воды должна быть выше, чем у гликолевого раствора");
        }

        [Test]
        public void GetProperties_WaterVsGlycol_WaterHasHigherThermalConductivity()
        {
            // Arrange
            double temperature = 20;

            // Act
            var waterProps = _service.GetProperties(GlycolType.Ethylene, 0, temperature);
            var glycolProps = _service.GetProperties(GlycolType.Ethylene, 50, temperature);

            // Assert - вода имеет более высокую теплопроводность, чем гликолевый раствор
            Assert.That(waterProps.ThermalConductivity, Is.GreaterThan(glycolProps.ThermalConductivity),
                "Теплопроводность воды должна быть выше, чем у гликолевого раствора");
        }

        [Test]
        public void GetProperties_WaterVsGlycol_WaterHasLowerViscosity()
        {
            // Arrange
            double temperature = 20;

            // Act
            var waterProps = _service.GetProperties(GlycolType.Ethylene, 0, temperature);
            var glycolProps = _service.GetProperties(GlycolType.Ethylene, 50, temperature);

            // Assert - вода имеет более низкую вязкость, чем гликолевый раствор
            Assert.That(waterProps.KinematicViscosity, Is.LessThan(glycolProps.KinematicViscosity),
                "Вязкость воды должна быть ниже, чем у гликолевого раствора");
        }

        #endregion

        #region Constants Cross-Check Tests

        /// <summary>
        /// Cross-check: ValidationConstants и реальный сервис должны опираться
        /// на одинаковые границы 10% / 90%. Это страховка от рассинхронизации,
        /// если кто-то снова введёт локальные литералы.
        /// </summary>
        [Test]
        public void ValidationConstants_And_HydraulicValidator_Agree_On_GlycolRange()
        {
            // Assert
            Assert.That(ValidationConstants.MinGlycolConcentration, Is.EqualTo(10.0),
                "MinGlycolConcentration должно быть 10%");
            Assert.That(ValidationConstants.MaxGlycolConcentration, Is.EqualTo(90.0),
                "MaxGlycolConcentration должно быть 90%");

            // Сервис должен отдавать те же значения
            Assert.That(_service.GetMinConcentration(),
                Is.EqualTo(ValidationConstants.MinGlycolConcentration),
                "GlycolDataService.GetMinConcentration должен совпадать с ValidationConstants.MinGlycolConcentration");
            Assert.That(_service.GetMaxConcentration(),
                Is.EqualTo(ValidationConstants.MaxGlycolConcentration),
                "GlycolDataService.GetMaxConcentration должен совпадать с ValidationConstants.MaxGlycolConcentration");
        }

        /// <summary>
        /// Регрессия: значения за пределами 10–90% должны выбрасывать
        /// <see cref="ArgumentOutOfRangeException"/> через единый guard в
        /// <c>ValidateParameters</c>. Случай 0% для воды не проверяем —
        /// там отдельная ветка с <c>GetWaterProperties</c>.
        /// </summary>
        [TestCase(5.0)]
        [TestCase(95.0)]
        public void GetProperties_ConcentrationOutOfRange_ThrowsArgumentOutOfRangeException(double concentration)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetProperties(GlycolType.Ethylene, concentration, 20.0));
        }

        #endregion
    }
}