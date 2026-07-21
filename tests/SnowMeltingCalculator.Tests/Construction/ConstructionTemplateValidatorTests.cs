using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Тесты для ConstructionTemplateValidator
    /// </summary>
    [TestFixture]
    public class ConstructionTemplateValidatorTests
    {
        private ConstructionTemplateValidator _validator = null!;
        private TestMaterialRepository _repository = null!;

        [SetUp]
        public void Setup()
        {
            _repository = new TestMaterialRepository();
            _repository.Seed(Material.GetDefaultMaterials());
            _validator = new ConstructionTemplateValidator(_repository);
        }

        [Test]
        public void Validate_ValidTemplate_ReturnsValid()
        {
            // Arrange
            var template = CreateValidTemplate();

            // Act
            var result = _validator.Validate(template);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_EmptyName_ReturnsInvalid()
        {
            // Arrange
            var template = CreateValidTemplate();
            template.Name = "";

            // Act
            var result = _validator.Validate(template);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Название")), Is.True);
        }

        [Test]
        public void Validate_NoLayers_ReturnsInvalid()
        {
            // Arrange
            var template = new ConstructionTemplate
            {
                Id = 1,
                Name = "Пустой шаблон"
            };

            // Act
            var result = _validator.Validate(template);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("хотя бы один слой")), Is.True);
        }

        [Test]
        public void Validate_NonExistentMaterial_ReturnsInvalid()
        {
            // Arrange
            var template = CreateValidTemplate();
            template.LayersAbovePipe[0].MaterialId = 9999;

            // Act
            var result = _validator.Validate(template);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("не найден")), Is.True);
        }

        [Test]
        public void Validate_ThicknessZero_ReturnsInvalid()
        {
            // Arrange
            var template = CreateValidTemplate();
            template.LayersAbovePipe[0].Thickness = 0;

            // Act
            var result = _validator.Validate(template);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Толщина")), Is.True);
        }

        [Test]
        public void Validate_ThicknessNegative_ReturnsInvalid()
        {
            // Arrange
            var template = CreateValidTemplate();
            template.LayersAbovePipe[0].Thickness = -5;

            // Act
            var result = _validator.Validate(template);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Толщина")), Is.True);
        }

        [Test]
        public void Validate_ThicknessTooLarge_ReturnsInvalid()
        {
            // Arrange
            var template = CreateValidTemplate();
            template.LayersAbovePipe[0].Thickness = 1001;

            // Act
            var result = _validator.Validate(template);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("1000")), Is.True);
        }

        [Test]
        public void Validate_LongName_ReturnsInvalid()
        {
            // Arrange
            var template = CreateValidTemplate();
            template.Name = new string('A', 101);

            // Act
            var result = _validator.Validate(template);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("100")), Is.True);
        }

        [Test]
        public void Validate_LayerBelowPipeOnly_ReturnsValid()
        {
            // Arrange
            var template = new ConstructionTemplate
            {
                Id = 1,
                Name = "Только под трубой"
            };
            template.LayersBelowPipe.Add(new LayerTemplate
            {
                MaterialId = 1,
                Thickness = 100,
                Position = LayerPosition.BelowPipe,
                Order = 0
            });

            // Act
            var result = _validator.Validate(template);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        private static ConstructionTemplate CreateValidTemplate()
        {
            return new ConstructionTemplate
            {
                Id = 1,
                Name = "Тестовый шаблон",
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate
                    {
                        MaterialId = 5,
                        Thickness = 50,
                        Position = LayerPosition.AbovePipe,
                        Order = 0
                    }
                }
            };
        }

        /// <summary>
        /// Тестовый репозиторий материалов
        /// </summary>
        private class TestMaterialRepository : IMaterialRepository
        {
            private List<Material>? _materials;

            public bool IsLoaded => _materials != null;
            public int MaterialsCount => _materials?.Count ?? 0;

            public Task<IEnumerable<Material>> LoadMaterialsAsync()
            {
                _materials ??= new List<Material>();
                return Task.FromResult<IEnumerable<Material>>(_materials);
            }

            public Material? GetMaterialById(int id)
            {
                return _materials?.FirstOrDefault(m => m.Id == id);
            }

            public IEnumerable<Material> GetMaterialsByCategory(MaterialCategory category)
            {
                return _materials?.Where(m => m.Category == category) ?? Enumerable.Empty<Material>();
            }

            public IEnumerable<Material> GetAllMaterials()
            {
                return _materials ?? Enumerable.Empty<Material>();
            }

            public Task<Material> AddAsync(Material material)
            {
                _materials ??= new List<Material>();
                material.Id = _materials.Count > 0 ? _materials.Max(m => m.Id) + 1 : 1;
                _materials.Add(material);
                return Task.FromResult(material);
            }

            public Task<Material> UpdateAsync(Material material)
            {
                var index = _materials?.FindIndex(m => m.Id == material.Id) ?? -1;
                if (index < 0)
                {
                    throw new InvalidOperationException($"Material with id {material.Id} not found");
                }
                _materials![index] = material;
                return Task.FromResult(material);
            }

            public Task<bool> DeleteAsync(int id)
            {
                var material = _materials?.FirstOrDefault(m => m.Id == id);
                if (material == null)
                {
                    return Task.FromResult(false);
                }
                _materials!.Remove(material);
                return Task.FromResult(true);
            }

            public Task SaveMaterialsAsync()
            {
                return Task.CompletedTask;
            }

            public void Seed(IEnumerable<Material> materials)
            {
                _materials = new List<Material>(materials);
            }
        }
    }
}
