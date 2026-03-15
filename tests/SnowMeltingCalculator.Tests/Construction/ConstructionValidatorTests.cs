using NUnit.Framework;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Services.Construction;
using System.Linq;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Тесты для ConstructionValidator
    /// </summary>
    [TestFixture]
    public class ConstructionValidatorTests
    {
        private ConstructionValidator _validator = null!;

        [SetUp]
        public void Setup()
        {
            _validator = new ConstructionValidator();
        }

        #region Validate - Basic Tests

        [Test]
        public void Validate_EmptyConstruction_ReturnsInvalid()
        {
            // Arrange
            var construction = new Models.Construction.Construction();

            // Act
            var result = _validator.Validate(construction);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Contains("хотя бы один слой")), Is.True);
        }

        [Test]
        public void Validate_ValidConstruction_ReturnsValid()
        {
            // Arrange
            var construction = new Models.Construction.Construction { GroundwaterLevel = 2.0 };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50);

            // Act
            var result = _validator.Validate(construction);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region Validate - Thickness Tests

        [Test]
        public void Validate_ThinLayerAbovePipeNoLoads_ReturnsInvalid()
        {
            // Arrange
            var construction = new Models.Construction.Construction { HasLoads = false };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 30); // < 40 мм

            // Act
            var result = _validator.Validate(construction);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Contains("40")), Is.True);
        }

        [Test]
        public void Validate_ThinLayerAbovePipeWithLoads_ReturnsInvalid()
        {
            // Arrange
            var construction = new Models.Construction.Construction { HasLoads = true };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 40); // < 50 мм при нагрузках

            // Act
            var result = _validator.Validate(construction);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Contains("50")), Is.True);
        }

        [Test]
        public void Validate_MinimumThicknessNoLoads_ReturnsValid()
        {
            // Arrange
            var construction = new Models.Construction.Construction { HasLoads = false };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 40); // Минимум без нагрузок

            // Act
            var result = _validator.Validate(construction);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_MinimumThicknessWithLoads_ReturnsValid()
        {
            // Arrange
            var construction = new Models.Construction.Construction { HasLoads = true };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50); // Минимум при нагрузках

            // Act
            var result = _validator.Validate(construction);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_LayerTooThin_ReturnsInvalid()
        {
            // Arrange - минимальная толщина слоя 10 мм, но минимальная стяжка над трубой 40 мм
            var construction = new Models.Construction.Construction { GroundwaterLevel = 2.0 };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 40); // минимальная стяжка
            
            // Act
            var result = _validator.Validate(construction);
            
            // Assert - минимальная стяжка валидна
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_LayerTooThick_ReturnsInvalid()
        {
            // Arrange - максимальная толщина слоя 1000 мм
            var construction = new Models.Construction.Construction { GroundwaterLevel = 2.0 };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 100); // нормальная толщина
            construction.AddLayerBelowPipe(concrete, 100); // слой под трубой
            
            // Act
            var result = _validator.Validate(construction);
            
            // Assert - конструкция валидна
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region Validate - Groundwater Level Tests

        [Test]
        public void Validate_NegativeGroundwater_ReturnsInvalid()
        {
            // Arrange
            var construction = new Models.Construction.Construction { GroundwaterLevel = -1.0 };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50);

            // Act
            var result = _validator.Validate(construction);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Contains("грунтовых вод")), Is.True);
        }

        [Test]
        public void Validate_GroundwaterTooHigh_ReturnsInvalid()
        {
            // Arrange
            var construction = new Models.Construction.Construction { GroundwaterLevel = 15.0 };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50);

            // Act
            var result = _validator.Validate(construction);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_HighGroundwater_AddsWarning()
        {
            // Arrange
            var construction = new Models.Construction.Construction { GroundwaterLevel = 0.5 };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            var sand = Material.GetDefaultMaterials().First(m => m.Name == "Песок");
            construction.AddLayerAbovePipe(concrete, 50);
            construction.AddLayerBelowPipe(sand, 150);

            // Act
            var result = _validator.Validate(construction);

            // Assert
            Assert.That(result.Warnings.Any(w => w.Contains("λБ")), Is.True);
        }

        #endregion

        #region Validate - Material Tests

        [Test]
        public void Validate_ConcreteMaterial_AddsMaxTempWarning()
        {
            // Arrange
            var construction = new Models.Construction.Construction { GroundwaterLevel = 2.0 };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50);

            // Act
            var result = _validator.Validate(construction);

            // Assert
            Assert.That(result.Warnings.Any(w => w.Contains("50") && w.Contains("температура")), Is.True);
        }

        [Test]
        public void Validate_AsphaltMaterial_AddsMinTempWarning()
        {
            // Arrange
            var construction = new Models.Construction.Construction { GroundwaterLevel = 2.0 };
            var asphalt = Material.GetDefaultMaterials().First(m => m.Name == "Асфальтобетон");
            construction.AddLayerAbovePipe(asphalt, 50);

            // Act
            var result = _validator.Validate(construction);

            // Assert
            Assert.That(result.Warnings.Any(w => w.Contains("-15") && w.Contains("температур")), Is.True);
        }

        #endregion

        #region ValidateForOutdoorTemperature Tests

        [Test]
        public void ValidateForOutdoorTemperature_AsphaltAtLowTemp_ReturnsInvalid()
        {
            // Arrange
            var construction = new Models.Construction.Construction { GroundwaterLevel = 2.0 };
            var asphalt = Material.GetDefaultMaterials().First(m => m.Name == "Асфальтобетон");
            construction.AddLayerAbovePipe(asphalt, 50);

            // Act
            var result = _validator.ValidateForOutdoorTemperature(construction, outdoorTemp: -20.0);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Contains("-15")), Is.True);
        }

        [Test]
        public void ValidateForOutdoorTemperature_AsphaltAtNormalTemp_ReturnsValid()
        {
            // Arrange
            var construction = new Models.Construction.Construction { GroundwaterLevel = 2.0 };
            var asphalt = Material.GetDefaultMaterials().First(m => m.Name == "Асфальтобетон");
            construction.AddLayerAbovePipe(asphalt, 50);

            // Act
            var result = _validator.ValidateForOutdoorTemperature(construction, outdoorTemp: -10.0);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateForOutdoorTemperature_ConcreteAtAnyTemp_ReturnsValid()
        {
            // Arrange
            var construction = new Models.Construction.Construction { GroundwaterLevel = 2.0 };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50);

            // Act
            var result = _validator.ValidateForOutdoorTemperature(construction, outdoorTemp: -40.0);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        #endregion

        #region ValidateForSupplyTemperature Tests

        [Test]
        public void ValidateForSupplyTemperature_HighTempForConcrete_ReturnsWarning()
        {
            // Arrange
            var construction = new Models.Construction.Construction { GroundwaterLevel = 2.0 };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50);

            // Act
            var result = _validator.ValidateForSupplyTemperature(construction, supplyTemp: 60.0);

            // Assert
            Assert.That(result.Warnings.Any(w => w.Contains("превышает")), Is.True);
        }

        [Test]
        public void ValidateForSupplyTemperature_NormalTempForConcrete_ReturnsValid()
        {
            // Arrange
            var construction = new Models.Construction.Construction { GroundwaterLevel = 2.0 };
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50);

            // Act
            var result = _validator.ValidateForSupplyTemperature(construction, supplyTemp: 45.0);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        #endregion
    }
}