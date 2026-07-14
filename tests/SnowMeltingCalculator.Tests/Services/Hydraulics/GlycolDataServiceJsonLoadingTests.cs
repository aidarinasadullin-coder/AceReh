using System.IO;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    /// <summary>
    /// Тесты загрузки данных гликолей из JSON
    /// </summary>
    [TestFixture]
    public class GlycolDataServiceJsonLoadingTests
    {
        [Test]
        public void GlycolDataService_LoadsFromJsonFile()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act
            var properties = service.GetProperties(GlycolType.Ethylene, 50, 40);

            // Assert
            Assert.That(properties.Density, Is.GreaterThan(0));
            Assert.That(properties.SpecificHeat, Is.GreaterThan(0));
            Assert.That(properties.KinematicViscosity, Is.GreaterThan(0));
            Assert.That(properties.ThermalConductivity, Is.GreaterThan(0));
        }

        [Test]
        public void GlycolDataService_ReturnsDefaultDataWhenFileNotFound()
        {
            // Arrange
            var service = new GlycolDataService("nonexistent_file.json");

            // Act
            var properties = service.GetProperties(GlycolType.Ethylene, 50, 20);

            // Assert - должны вернуться встроенные данные
            Assert.That(properties.Density, Is.GreaterThan(0));
            Assert.That(properties.SpecificHeat, Is.GreaterThan(0));
        }

        [Test]
        public void GlycolDataService_InterpolatesDensity()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act - интерполяция между точками
            var density = service.GetDensity(GlycolType.Ethylene, 50, 40);

            // Assert
            Assert.That(density, Is.InRange(1000, 1100)); // Разумный диапазон для 50% этиленгликоля при 40°C
        }

        [Test]
        public void GlycolDataService_InterpolatesViscosity()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act
            var viscosity = service.GetKinematicViscosity(GlycolType.Ethylene, 50, 40);

            // Assert
            Assert.That(viscosity, Is.GreaterThan(0));
            // Вязкость 50% этиленгликоля при 40°C должна быть около 2-5 мм²/с
            Assert.That(viscosity, Is.InRange(1, 10));
        }

        [Test]
        public void GlycolDataService_SupportsBothGlycolTypes()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act
            var ethylene = service.GetProperties(GlycolType.Ethylene, 50, 40);
            var propylene = service.GetProperties(GlycolType.Propylene, 50, 40);

            // Assert
            Assert.That(ethylene.Density, Is.GreaterThan(0));
            Assert.That(propylene.Density, Is.GreaterThan(0));
            // Пропиленгликоль обычно имеет меньшую плотность
        }

        [Test]
        public void GlycolDataService_CachesData()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act - несколько вызовов должны использовать кэш
            var props1 = service.GetProperties(GlycolType.Ethylene, 50, 40);
            var props2 = service.GetProperties(GlycolType.Ethylene, 50, 40);
            var props3 = service.GetProperties(GlycolType.Ethylene, 50, 40);

            // Assert - значения должны быть одинаковыми
            Assert.That(props1.Density, Is.EqualTo(props2.Density));
            Assert.That(props1.Density, Is.EqualTo(props3.Density));
        }

        [Test]
        public void GlycolDataService_ThrowsOnInvalidConcentration()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.GetProperties(GlycolType.Ethylene, 5, 40)); // Концентрация < 10%

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.GetProperties(GlycolType.Ethylene, 95, 40)); // Концентрация > 90%
        }

        [Test]
        public void GlycolDataService_ThrowsOnInvalidTemperature()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.GetProperties(GlycolType.Ethylene, 50, -50)); // Температура < MIN

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.GetProperties(GlycolType.Ethylene, 50, 150)); // Температура > MAX
        }

        [Test]
        public void GlycolDataService_IsTemperatureSupported()
        {
            // Arrange
            var service = new GlycolDataService();

            // Act & Assert
            Assert.That(service.IsTemperatureSupported(-50), Is.False);
            Assert.That(service.IsTemperatureSupported(0), Is.True);
            Assert.That(service.IsTemperatureSupported(40), Is.True);
            Assert.That(service.IsTemperatureSupported(100), Is.True);
            Assert.That(service.IsTemperatureSupported(150), Is.False);
        }

        [Test]
        public void GlycolDataService_IsConcentrationSupported()
        {
            // Arrange
            var service = new GlycolDataService();

            // Act & Assert
            Assert.That(service.IsConcentrationSupported(5), Is.False);
            Assert.That(service.IsConcentrationSupported(10), Is.True);
            Assert.That(service.IsConcentrationSupported(50), Is.True);
            Assert.That(service.IsConcentrationSupported(90), Is.True);
            Assert.That(service.IsConcentrationSupported(95), Is.False);
        }

        [Test]
        public void GlycolDataService_GetMinTemperature()
        {
            // Arrange
            var service = new GlycolDataService();

            // Act
            var minTemp = service.GetMinTemperature();

            // Assert
            Assert.That(minTemp, Is.EqualTo(-34.4));
        }

        [Test]
        public void GlycolDataService_GetMaxTemperature()
        {
            // Arrange
            var service = new GlycolDataService();

            // Act
            var maxTemp = service.GetMaxTemperature();

            // Assert
            // Максимальная температура - 100°C (округление от 98.9°C в данных JSON)
            Assert.That(maxTemp, Is.EqualTo(100.0).Within(0.1));
        }

        [Test]
        public void GlycolDataService_GetMinConcentration()
        {
            // Arrange
            var service = new GlycolDataService();

            // Act
            var minConc = service.GetMinConcentration();

            // Assert
            Assert.That(minConc, Is.EqualTo(10.0));
        }

        [Test]
        public void GlycolDataService_GetMaxConcentration()
        {
            // Arrange
            var service = new GlycolDataService();

            // Act
            var maxConc = service.GetMaxConcentration();

            // Assert
            Assert.That(maxConc, Is.EqualTo(90.0));
        }

        [Test]
        public void GlycolDataService_InterpolationAccuracy()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act - интерполяция между точками
            var props1 = service.GetProperties(GlycolType.Ethylene, 50, 40);
            var props2 = service.GetProperties(GlycolType.Ethylene, 50, 45);
            var props3 = service.GetProperties(GlycolType.Ethylene, 50, 50);

            // Assert - значения должны плавно изменяться
            // Плотность уменьшается с ростом температуры
            Assert.That(props1.Density, Is.GreaterThan(props2.Density));
            Assert.That(props2.Density, Is.GreaterThan(props3.Density));
        }

        [Test]
        public void GlycolDataService_AllPropertiesConsistent()
        {
            // Arrange
            var service = new GlycolDataService("data/glycol_data.json");

            // Act
            var props = service.GetProperties(GlycolType.Ethylene, 50, 40);

            // Assert - все свойства должны быть согласованы
            Assert.That(props.GlycolType, Is.EqualTo(GlycolType.Ethylene));
            Assert.That(props.Concentration, Is.EqualTo(50));
            Assert.That(props.Temperature, Is.EqualTo(40));
            Assert.That(props.Density, Is.EqualTo(service.GetDensity(GlycolType.Ethylene, 50, 40)));
            Assert.That(props.SpecificHeat, Is.EqualTo(service.GetSpecificHeat(GlycolType.Ethylene, 50, 40)));
            Assert.That(props.KinematicViscosity, Is.EqualTo(service.GetKinematicViscosity(GlycolType.Ethylene, 50, 40)));
            Assert.That(props.ThermalConductivity, Is.EqualTo(service.GetThermalConductivity(GlycolType.Ethylene, 50, 40)));
        }
    }
}