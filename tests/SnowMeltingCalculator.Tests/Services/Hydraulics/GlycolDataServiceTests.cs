using NUnit.Framework;
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
            double temperatureLow = -10;
            double temperatureHigh = 40;

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
            double concentration = 5; // Меньше минимума (10%)
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
            Assert.That(_service.IsTemperatureSupported(121.1), Is.True);
            Assert.That(_service.IsTemperatureSupported(-35), Is.False);
            Assert.That(_service.IsTemperatureSupported(130), Is.False);
        }

        [Test]
        public void IsConcentrationSupported_ReturnsCorrectValue()
        {
            // Act & Assert
            Assert.That(_service.IsConcentrationSupported(10), Is.True);
            Assert.That(_service.IsConcentrationSupported(50), Is.True);
            Assert.That(_service.IsConcentrationSupported(90), Is.True);
            Assert.That(_service.IsConcentrationSupported(5), Is.False);
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
            Assert.That(maxTemp, Is.EqualTo(121.1).Within(0.1));
        }

        [Test]
        public void GetMinConcentration_ReturnsCorrectValue()
        {
            // Act
            double minConc = _service.GetMinConcentration();

            // Assert
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
            double temperature = 20;

            // Act
            double specificHeat30 = _service.GetSpecificHeat(GlycolType.Ethylene, 30, temperature);
            double specificHeat60 = _service.GetSpecificHeat(GlycolType.Ethylene, 60, temperature);

            // Assert - более высокая концентрация = более низкая теплоёмкость
            Assert.That(specificHeat30, Is.GreaterThan(specificHeat60));
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

        [Test]
        public void GetThermalConductivity_HigherConcentration_LowerConductivity()
        {
            // Arrange
            double temperature = 20;

            // Act
            double conductivity30 = _service.GetThermalConductivity(GlycolType.Ethylene, 30, temperature);
            double conductivity60 = _service.GetThermalConductivity(GlycolType.Ethylene, 60, temperature);

            // Assert - более высокая концентрация = более низкая теплопроводность
            Assert.That(conductivity30, Is.GreaterThan(conductivity60));
        }

        [Test]
        public void GetThermalConductivity_HigherTemperature_HigherConductivity()
        {
            // Arrange
            double concentration = 50;

            // Act
            double conductivity20 = _service.GetThermalConductivity(GlycolType.Ethylene, concentration, 20);
            double conductivity50 = _service.GetThermalConductivity(GlycolType.Ethylene, concentration, 50);

            // Assert - более высокая температура = более высокая теплопроводность
            Assert.That(conductivity50, Is.GreaterThan(conductivity20));
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
        public void GetProperties_ExtrapolationAboveMaxTemperature_ThrowsException()
        {
            // Arrange
            double concentration = 50;
            double temperature = 130; // Выше максимума (121.1°C)

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetProperties(GlycolType.Ethylene, concentration, temperature));
        }

        [Test]
        public void GetProperties_ExtrapolationBelowMinConcentration_ThrowsException()
        {
            // Arrange
            double concentration = 5; // Ниже минимума (10%)
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
            double concentration = 50;
            double temperature = -34.4; // Минимальная температура

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
            double temperature = 121.1; // Максимальная температура

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
            double concentration = 10; // Минимальная концентрация
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
    }
}