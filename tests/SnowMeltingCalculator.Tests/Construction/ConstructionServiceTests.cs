using NUnit.Framework;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Services.Construction;
using System;
using System.Linq;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Тесты для ConstructionService
    /// </summary>
    [TestFixture]
    public class ConstructionServiceTests
    {
        private ConstructionService _service = null!;

        [SetUp]
        public void Setup()
        {
            _service = new ConstructionService();
        }

        #region CalculateR1 Tests

        [Test]
        public void CalculateR1_SingleLayer_ReturnsCorrectValue()
        {
            // Arrange
            var material = Material.GetDefaultMaterial();
            var layer = new Layer
            {
                Material = material,
                Thickness = 100, // 100 мм
                CalculatedLambda = 1.5, // Вт/м·К
                Position = LayerPosition.AbovePipe
            };

            // Act
            var r1 = _service.CalculateR1(new[] { layer });

            // Assert
            // R = d / λ / 1000 = 100 / 1.5 / 1000 = 0.0667 м²·К/Вт
            Assert.That(r1, Is.EqualTo(0.0667).Within(0.0001));
        }

        [Test]
        public void CalculateR1_MultipleLayers_ReturnsSum()
        {
            // Arrange
            var layers = new[]
            {
                new Layer { Material = Material.GetDefaultMaterial(), Thickness = 50, CalculatedLambda = 1.5, Position = LayerPosition.AbovePipe },
                new Layer { Material = Material.GetDefaultMaterial(), Thickness = 100, CalculatedLambda = 1.2, Position = LayerPosition.AbovePipe }
            };

            // Act
            var r1 = _service.CalculateR1(layers);

            // Assert
            // R1 = 50/1.5/1000 + 100/1.2/1000 = 0.0333 + 0.0833 = 0.1167 м²·К/Вт
            Assert.That(r1, Is.EqualTo(0.1167).Within(0.0001));
        }

        [Test]
        public void CalculateR1_EmptyCollection_ReturnsZero()
        {
            // Act
            var r1 = _service.CalculateR1(Enumerable.Empty<Layer>());

            // Assert
            Assert.That(r1, Is.EqualTo(0));
        }

        [Test]
        public void CalculateR1_ZeroLambda_ThrowsInvalidOperationException()
        {
            // Arrange
            var layer = new Layer
            {
                Material = Material.GetDefaultMaterial(),
                Thickness = 100,
                CalculatedLambda = 0, // Некорректное значение
                Position = LayerPosition.AbovePipe
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _service.CalculateR1(new[] { layer }));
        }

        [Test]
        public void CalculateR1_NegativeLambda_ThrowsInvalidOperationException()
        {
            // Arrange
            var layer = new Layer
            {
                Material = Material.GetDefaultMaterial(),
                Thickness = 100,
                CalculatedLambda = -1.5, // Некорректное значение
                Position = LayerPosition.AbovePipe
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _service.CalculateR1(new[] { layer }));
        }

        #endregion

        #region CalculateR2 Tests

        [Test]
        public void CalculateR2_SingleLayer_ReturnsCorrectValue()
        {
            // Arrange
            var material = Material.GetDefaultMaterials().First(m => m.Name == "Песок");
            var layer = new Layer
            {
                Material = material,
                Thickness = 150, // 150 мм
                CalculatedLambda = material.LambdaA, // 0.4 Вт/м·К
                Position = LayerPosition.BelowPipe
            };

            // Act
            var r2 = _service.CalculateR2(new[] { layer }, groundwaterLevel: 2.0);

            // Assert
            // R = d / λ / 1000 = 150 / 0.4 / 1000 = 0.375 м²·К/Вт
            Assert.That(r2, Is.EqualTo(0.375).Within(0.0001));
        }

        [Test]
        public void CalculateR2_HighGroundwater_UsesLambdaB()
        {
            // Arrange
            var material = Material.GetDefaultMaterials().First(m => m.Name == "Песок");
            var layer = new Layer
            {
                Material = material,
                Thickness = 150,
                Position = LayerPosition.BelowPipe
            };

            // Act - УГВ < 1м, должна использоваться λБ
            var r2 = _service.CalculateR2(new[] { layer }, groundwaterLevel: 0.5);

            // Assert
            // При УГВ < 1м используется λБ = 2.0 для песка
            // R = 150 / 2.0 / 1000 = 0.075 м²·К/Вт
            Assert.That(r2, Is.EqualTo(0.075).Within(0.0001));
        }

        [Test]
        public void CalculateR2_LowGroundwater_UsesLambdaA()
        {
            // Arrange
            var material = Material.GetDefaultMaterials().First(m => m.Name == "Песок");
            var layer = new Layer
            {
                Material = material,
                Thickness = 150,
                Position = LayerPosition.BelowPipe
            };

            // Act - УГВ >= 1м, должна использоваться λА
            var r2 = _service.CalculateR2(new[] { layer }, groundwaterLevel: 2.0);

            // Assert
            // При УГВ >= 1м используется λА = 0.4 для песка
            // R = 150 / 0.4 / 1000 = 0.375 м²·К/Вт
            Assert.That(r2, Is.EqualTo(0.375).Within(0.0001));
        }

        [Test]
        public void CalculateR2_NegativeGroundwater_ThrowsArgumentException()
        {
            // Arrange
            var layer = new Layer
            {
                Material = Material.GetDefaultMaterial(),
                Thickness = 100,
                Position = LayerPosition.BelowPipe
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.CalculateR2(new[] { layer }, groundwaterLevel: -1.0));
        }

        #endregion

        #region GetLambdaE Tests

        [Test]
        public void GetLambdaE_WithLayer_ReturnsLambdaA()
        {
            // Arrange
            var material = Material.GetDefaultMaterial();
            var layer = new Layer
            {
                Material = material,
                Thickness = 100,
                Position = LayerPosition.AbovePipe
            };

            // Act
            var lambdaE = _service.GetLambdaE(layer);

            // Assert
            Assert.That(lambdaE, Is.EqualTo(material.LambdaA));
        }

        [Test]
        public void GetLambdaE_NullLayer_ReturnsDefaultValue()
        {
            // Act
            var lambdaE = _service.GetLambdaE(null);

            // Assert
            Assert.That(lambdaE, Is.EqualTo(1.6)); // Значение по умолчанию для бетона
        }

        [Test]
        public void GetLambdaE_LayerWithoutMaterial_ThrowsInvalidOperationException()
        {
            // Arrange
            var layer = new Layer
            {
                Material = null!,
                Thickness = 100,
                Position = LayerPosition.AbovePipe
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _service.GetLambdaE(layer));
        }

        #endregion

        #region ValidateConstruction Tests

        [Test]
        public void ValidateConstruction_ValidConstruction_ReturnsValidResult()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction
            {
                GroundwaterLevel = 2.0,
                HasLoads = false
            };

            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50);

            // Act
            var result = _service.ValidateConstruction(construction);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateConstruction_NoLayers_ReturnsInvalidResult()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction();

            // Act
            var result = _service.ValidateConstruction(construction);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ValidateConstruction_ThinLayerAbovePipe_ReturnsInvalidResult()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction
            {
                HasLoads = false
            };

            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 30); // Меньше минимума (40 мм)

            // Act
            var result = _service.ValidateConstruction(construction);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Contains("Минимальная толщина")), Is.True);
        }

        [Test]
        public void ValidateConstruction_WithLoads_RequiresThickerLayer()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction
            {
                HasLoads = true
            };

            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 40); // Меньше минимума при нагрузках (50 мм)

            // Act
            var result = _service.ValidateConstruction(construction);

            // Assert
            Assert.That(result.IsValid, Is.False);
        }

        #endregion

        #region CreateFromTemplate Tests

        [Test]
        public void CreateFromTemplate_ValidTemplate_ReturnsConstruction()
        {
            // Arrange
            var template = ConstructionTemplate.GetDefaultTemplates().First();
            var materials = Material.GetDefaultMaterials();

            // Act
            var construction = _service.CreateFromTemplate(template, materials);

            // Assert
            Assert.That(construction, Is.Not.Null);
            Assert.That(construction.LayersAbovePipe.Count, Is.EqualTo(template.LayersAbovePipe.Count));
            Assert.That(construction.HasLoads, Is.EqualTo(template.HasLoads));
        }

        [Test]
        public void CreateFromTemplate_InvalidMaterialId_ThrowsInvalidOperationException()
        {
            // Arrange
            var template = new ConstructionTemplate
            {
                Id = 999,
                Name = "Test",
                LayersAbovePipe = new System.Collections.Generic.List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 9999, Thickness = 100, Position = LayerPosition.AbovePipe }
                }
            };
            var materials = Material.GetDefaultMaterials();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _service.CreateFromTemplate(template, materials));
        }

        #endregion

        #region GetTotalThickness Tests

        [Test]
        public void GetTotalThicknessAbovePipe_MultipleLayers_ReturnsSum()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction();
            var concrete = Material.GetDefaultMaterials().First(m => m.Name == "Бетон плотный");
            construction.AddLayerAbovePipe(concrete, 50);
            construction.AddLayerAbovePipe(concrete, 100);

            // Act
            var thickness = _service.GetTotalThicknessAbovePipe(construction);

            // Assert
            Assert.That(thickness, Is.EqualTo(150));
        }

        [Test]
        public void GetTotalThicknessBelowPipe_MultipleLayers_ReturnsSum()
        {
            // Arrange
            var construction = new SnowMeltingCalculator.Models.Construction.Construction();
            var sand = Material.GetDefaultMaterials().First(m => m.Name == "Песок");
            construction.AddLayerBelowPipe(sand, 150);
            construction.AddLayerBelowPipe(sand, 200);

            // Act
            var thickness = _service.GetTotalThicknessBelowPipe(construction);

            // Assert
            Assert.That(thickness, Is.EqualTo(350));
        }

        #endregion
    }
}