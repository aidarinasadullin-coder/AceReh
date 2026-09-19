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
        public void FallbackTables_MatchJsonCanon_AtControlPoints()
        {
            // Переходный пин §6.3 роадмапа 2026-09-18 (ADR-016): встроенные
            // fallback-таблицы обязаны совпадать с JSON-каноном в узлах своей
            // сетки — иначе молчаливый fallback при битой поставке считает по
            // протухшим данным. Сверка — в узлах fallback (9 температур ×
            // 9 концентраций): fallback-сетка грубее JSON (25 температур), в
            // промежуточных точках результаты интерполяции законно различаются.
            var jsonService = new GlycolDataService("data/glycol_data.json");
            var fallbackService = new GlycolDataService("nonexistent_file.json");

            double[] concentrations = { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };
            double[] temperatures = { -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9 };
            var glycolTypes = new[] { GlycolType.Ethylene, GlycolType.Propylene };

            foreach (var glycolType in glycolTypes)
            {
                foreach (var concentration in concentrations)
                {
                    foreach (var temperature in temperatures)
                    {
                        var fromJson = jsonService.GetProperties(glycolType, concentration, temperature);
                        var fromFallback = fallbackService.GetProperties(glycolType, concentration, temperature);

                        AssertFallbackMatchesJson(fromJson.Density, fromFallback.Density, glycolType, concentration, temperature, nameof(fromJson.Density));
                        AssertFallbackMatchesJson(fromJson.SpecificHeat, fromFallback.SpecificHeat, glycolType, concentration, temperature, nameof(fromJson.SpecificHeat));
                        AssertFallbackMatchesJson(fromJson.KinematicViscosity, fromFallback.KinematicViscosity, glycolType, concentration, temperature, nameof(fromJson.KinematicViscosity));
                        AssertFallbackMatchesJson(fromJson.ThermalConductivity, fromFallback.ThermalConductivity, glycolType, concentration, temperature, nameof(fromJson.ThermalConductivity));
                    }
                }
            }
        }

        private static void AssertFallbackMatchesJson(
            double expected, double actual, GlycolType glycolType, double concentration, double temperature, string field)
        {
            string context = $"{glycolType}, {concentration}%, {temperature}°C, {field}";

            if (double.IsNaN(expected) || double.IsNaN(actual))
            {
                // Fallback не имеет права «изобретать» данные там, где канон None
                // (fallback число при JSON NaN — падение). Обратная асимметрия
                // (fallback NaN при JSON числе) известна: fallback-таблицы
                // пропилена грубее канона (см. ADR-016, чек R-2026-09-19-02).
                Assert.That(double.IsNaN(actual), Is.True, $"{context}: fallback вернул число там, где JSON-канон — None");
                return;
            }

            // Относительный допуск D6: |a−b| ≤ 1e-9·max(|a|,|b|,1)
            double tolerance = 1e-9 * System.Math.Max(System.Math.Max(System.Math.Abs(actual), System.Math.Abs(expected)), 1.0);
            Assert.That(System.Math.Abs(actual - expected), Is.LessThanOrEqualTo(tolerance), context);
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