using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Тесты применения шаблона конструкции с учётом снимков материалов.
    /// </summary>
    [TestFixture]
    public class ConstructionServiceTemplateImportTests
    {
        private ConstructionService _service = null!;
        private Mock<IMaterialRepository> _materialRepositoryMock = null!;

        [SetUp]
        public void Setup()
        {
            _materialRepositoryMock = new Mock<IMaterialRepository>();
            _service = new ConstructionService(
                new ConstructionValidator(),
                _materialRepositoryMock.Object,
                new Mock<IConstructionTemplateRepository>().Object);
        }

        private static MaterialSnapshot CreateSnapshot(int id, string name) => new MaterialSnapshot
        {
            Id = id,
            Name = name,
            Category = MaterialCategory.Concrete,
            LambdaA = 1.2,
            LambdaB = 1.3,
            MaxSupplyTemp = 50,
            MinOutdoorTemp = -15,
            Notes = "snapshot",
            IsBuiltIn = false
        };

        [Test]
        public void CreateFromTemplate_MaterialExistsWithSnapshot_ReturnsConstruction()
        {
            // Arrange
            var template = new ConstructionTemplate
            {
                Id = 1,
                Name = "Existing material with snapshot",
                DefaultGroundwaterLevel = 2.0,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 5, Thickness = 100, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                MaterialSnapshots = new List<MaterialSnapshot> { CreateSnapshot(5, "Бетон плотный") }
            };
            var materials = Material.GetDefaultMaterials();

            // Act
            var construction = _service.CreateFromTemplate(template, materials);

            // Assert
            Assert.That(construction, Is.Not.Null);
            Assert.That(construction.LayersAbovePipe.Count, Is.EqualTo(1));
            Assert.That(construction.LayersAbovePipe[0].Material?.Id, Is.EqualTo(5));
        }

        [Test]
        public void CreateFromTemplate_MissingMaterialWithSnapshot_ThrowsMaterialNotFoundExceptionWithSnapshot()
        {
            // Arrange
            var snapshot = CreateSnapshot(999, "Missing material");
            var template = new ConstructionTemplate
            {
                Id = 2,
                Name = "Missing above-pipe material",
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 999, Thickness = 100, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                MaterialSnapshots = new List<MaterialSnapshot> { snapshot }
            };
            var materials = Material.GetDefaultMaterials();

            // Act & Assert
            var ex = Assert.Throws<MaterialNotFoundException>(() => _service.CreateFromTemplate(template, materials));
            Assert.That(ex!.MaterialId, Is.EqualTo(999));
            Assert.That(ex.Snapshot, Is.Not.Null);
            Assert.That(ex.Snapshot!.Id, Is.EqualTo(999));
            Assert.That(ex.Snapshot.Name, Is.EqualTo("Missing material"));
        }

        [Test]
        public void CreateFromTemplate_MissingMaterialWithoutSnapshot_ThrowsMaterialNotFoundExceptionWithoutSnapshot()
        {
            // Arrange
            var template = new ConstructionTemplate
            {
                Id = 3,
                Name = "Missing without snapshot",
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 999, Thickness = 100, Position = LayerPosition.AbovePipe, Order = 0 }
                }
            };
            var materials = Material.GetDefaultMaterials();

            // Act & Assert
            var ex = Assert.Throws<MaterialNotFoundException>(() => _service.CreateFromTemplate(template, materials));
            Assert.That(ex!.MaterialId, Is.EqualTo(999));
            Assert.That(ex.Snapshot, Is.Null);
        }

        [Test]
        public void CreateFromTemplate_MissingMaterialBelowPipeWithSnapshot_ThrowsMaterialNotFoundExceptionWithSnapshot()
        {
            // Arrange
            var snapshot = CreateSnapshot(888, "Missing below-pipe material");
            var template = new ConstructionTemplate
            {
                Id = 4,
                Name = "Missing below-pipe material",
                LayersBelowPipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 888, Thickness = 150, Position = LayerPosition.BelowPipe, Order = 0 }
                },
                MaterialSnapshots = new List<MaterialSnapshot> { snapshot }
            };
            var materials = Material.GetDefaultMaterials();

            // Act & Assert
            var ex = Assert.Throws<MaterialNotFoundException>(() => _service.CreateFromTemplate(template, materials));
            Assert.That(ex!.MaterialId, Is.EqualTo(888));
            Assert.That(ex.Snapshot, Is.Not.Null);
            Assert.That(ex.Snapshot!.Id, Is.EqualTo(888));
        }

        [Test]
        public void CreateFromTemplate_MissingMaterialWithSnapshot_MessageContainsSnapshotNameAndId()
        {
            // Arrange
            var snapshot = CreateSnapshot(999, "Missing material");
            var template = new ConstructionTemplate
            {
                Id = 5,
                Name = "Missing with snapshot message",
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 999, Thickness = 100, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                MaterialSnapshots = new List<MaterialSnapshot> { snapshot }
            };
            var materials = Material.GetDefaultMaterials();

            // Act & Assert
            var ex = Assert.Throws<MaterialNotFoundException>(() => _service.CreateFromTemplate(template, materials));
            Assert.That(ex!.Message, Does.Contain(snapshot.Name));
            Assert.That(ex.Message, Does.Contain("999"));
        }
    }
}
