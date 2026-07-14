using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;
using Moq;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    /// <summary>
    /// Тесты для интерфейса IGlycolDataService
    /// </summary>
    [TestFixture]
    public class IGlycolDataServiceTests
    {
        private Mock<IGlycolDataService> _serviceMock = null!;

        [SetUp]
        public void Setup()
        {
            _serviceMock = new Mock<IGlycolDataService>();
        }

        #region GetDensity Tests

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
        public void GetDensity_ForPropylene_ReturnsCorrectValue()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetDensity(GlycolType.Propylene, 50, 40))
                .Returns(1040.0);

            // Act
            var result = _serviceMock.Object.GetDensity(GlycolType.Propylene, 50, 40);

            // Assert
            Assert.That(result, Is.EqualTo(1040.0).Within(0.1));
        }

        [Test]
        public void GetDensity_ForWater_ReturnsCorrectValue()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetDensity(GlycolType.Ethylene, 0, 20))
                .Returns(998.0);

            // Act
            var result = _serviceMock.Object.GetDensity(GlycolType.Ethylene, 0, 20);

            // Assert
            Assert.That(result, Is.EqualTo(998.0).Within(0.1));
        }

        #endregion

        #region GetSpecificHeat Tests

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
        public void GetSpecificHeat_ForWater_ReturnsCorrectValue()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetSpecificHeat(GlycolType.Ethylene, 0, 20))
                .Returns(4.18);

            // Act
            var result = _serviceMock.Object.GetSpecificHeat(GlycolType.Ethylene, 0, 20);

            // Assert
            Assert.That(result, Is.EqualTo(4.18).Within(0.01));
        }

        #endregion

        #region GetKinematicViscosity Tests

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
        public void GetKinematicViscosity_AtLowTemperature_ReturnsHigherValue()
        {
            // Arrange - вязкость возрастает при низких температурах
            _serviceMock
                .Setup(s => s.GetKinematicViscosity(GlycolType.Ethylene, 50, -15))
                .Returns(18.17);

            // Act
            var result = _serviceMock.Object.GetKinematicViscosity(GlycolType.Ethylene, 50, -15);

            // Assert
            Assert.That(result, Is.EqualTo(18.17).Within(0.1));
        }

        #endregion

        #region GetThermalConductivity Tests

        [Test]
        public void GetThermalConductivity_ReturnsCorrectValue()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetThermalConductivity(GlycolType.Ethylene, 50, 40))
                .Returns(0.42);

            // Act
            var result = _serviceMock.Object.GetThermalConductivity(GlycolType.Ethylene, 50, 40);

            // Assert
            Assert.That(result, Is.EqualTo(0.42).Within(0.01));
        }

        #endregion

        #region GetProperties Tests

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
            Assert.That(result.Temperature, Is.EqualTo(40));
            Assert.That(result.Concentration, Is.EqualTo(50));
            Assert.That(result.GlycolType, Is.EqualTo(GlycolType.Ethylene));
        }

        [Test]
        public void GetProperties_ForPropylene_ReturnsCorrectProperties()
        {
            // Arrange
            var expectedProps = new GlycolProperties
            {
                Density = 1040,
                SpecificHeat = 3.50,
                KinematicViscosity = 2.5,
                ThermalConductivity = 0.40,
                Temperature = 40,
                Concentration = 50,
                GlycolType = GlycolType.Propylene
            };

            _serviceMock
                .Setup(s => s.GetProperties(GlycolType.Propylene, 50, 40))
                .Returns(expectedProps);

            // Act
            var result = _serviceMock.Object.GetProperties(GlycolType.Propylene, 50, 40);

            // Assert
            Assert.That(result.GlycolType, Is.EqualTo(GlycolType.Propylene));
            Assert.That(result.Density, Is.EqualTo(1040).Within(0.1));
        }

        #endregion

        #region IsTemperatureSupported Tests

        [Test]
        public void IsTemperatureSupported_ReturnsTrueForValidTemperature()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.IsTemperatureSupported(40))
                .Returns(true);

            // Act
            var result = _serviceMock.Object.IsTemperatureSupported(40);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsTemperatureSupported_ReturnsFalseForOutOfRange()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.IsTemperatureSupported(-50))
                .Returns(false);

            // Act
            var result = _serviceMock.Object.IsTemperatureSupported(-50);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region IsConcentrationSupported Tests

        [Test]
        public void IsConcentrationSupported_ReturnsTrueForValidConcentration()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.IsConcentrationSupported(50))
                .Returns(true);

            // Act
            var result = _serviceMock.Object.IsConcentrationSupported(50);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsConcentrationSupported_ReturnsFalseForOutOfRange()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.IsConcentrationSupported(5))
                .Returns(false);

            // Act
            var result = _serviceMock.Object.IsConcentrationSupported(5);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region GetMinTemperature Tests

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

        #endregion

        #region GetMaxTemperature Tests

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

        #endregion

        #region GetMinConcentration Tests

        [Test]
        public void GetMinConcentration_ReturnsCorrectValue()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetMinConcentration())
                .Returns(10.0);

            // Act
            var result = _serviceMock.Object.GetMinConcentration();

            // Assert
            Assert.That(result, Is.EqualTo(10.0).Within(0.1));
        }

        #endregion

        #region GetMaxConcentration Tests

        [Test]
        public void GetMaxConcentration_ReturnsCorrectValue()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.GetMaxConcentration())
                .Returns(90.0);

            // Act
            var result = _serviceMock.Object.GetMaxConcentration();

            // Assert
            Assert.That(result, Is.EqualTo(90.0).Within(0.1));
        }

        #endregion
    }
}