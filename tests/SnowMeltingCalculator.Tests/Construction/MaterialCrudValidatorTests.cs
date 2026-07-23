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
    /// Тесты для MaterialCrudValidator
    /// </summary>
    [TestFixture]
    public class MaterialCrudValidatorTests
    {
        private MaterialCrudValidator _validator = null!;
        private TestMaterialRepository _repository = null!;

        [SetUp]
        public void Setup()
        {
            _repository = new TestMaterialRepository();
            _repository.Seed(Material.GetDefaultMaterials());
            _validator = new MaterialCrudValidator(_repository);
        }

        [Test]
        public void Validate_ValidMaterial_ReturnsValid()
        {
            // Arrange
            var material = CreateValidMaterial();

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_EmptyName_ReturnsInvalid()
        {
            // Arrange
            var material = CreateValidMaterial();
            material.Name = "";

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Название")), Is.True);
        }

        [Test]
        public void Validate_LongName_ReturnsInvalid()
        {
            // Arrange
            var material = CreateValidMaterial();
            material.Name = new string('A', 101);

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("100")), Is.True);
        }

        [Test]
        public void Validate_InvalidLambdaA_ReturnsInvalid()
        {
            // Arrange
            var material = CreateValidMaterial();
            material.LambdaA = 0;

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("λА")), Is.True);
        }

        [Test]
        public void Validate_InvalidLambdaB_ReturnsInvalid()
        {
            // Arrange
            var material = CreateValidMaterial();
            material.LambdaB = -1;

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("λБ")), Is.True);
        }

        [Test]
        public void Validate_MaxSupplyTempOutOfRange_ReturnsInvalid()
        {
            // Arrange
            var material = CreateValidMaterial();
            material.MaxSupplyTemp = 250;

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Максимальная температура подачи")), Is.True);
        }

        [Test]
        public void Validate_MinOutdoorTempOutOfRange_ReturnsInvalid()
        {
            // Arrange
            var material = CreateValidMaterial();
            material.MinOutdoorTemp = -70;

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Минимальная температура наружного воздуха")), Is.True);
        }

        [Test]
        public void Validate_NullTemperatures_ReturnsValid()
        {
            // Arrange
            var material = CreateValidMaterial();
            material.MaxSupplyTemp = null;
            material.MinOutdoorTemp = null;

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_DuplicateName_ReturnsInvalid()
        {
            // Arrange
            var material = CreateValidMaterial();
            material.Name = "Бетон";

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("уже существует")), Is.True);
        }

        [Test]
        public void Validate_DuplicateNameDifferentCase_ReturnsInvalid()
        {
            // Arrange
            var material = CreateValidMaterial();
            material.Name = "БЕТОН";

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("уже существует")), Is.True);
        }

        [Test]
        public void Validate_SelfUpdateWithSameName_ReturnsValid()
        {
            // Arrange
            var existing = _repository.GetAllMaterials().First(m => m.Name == "Бетон");
            var material = new Material
            {
                Id = existing.Id,
                Name = existing.Name,
                Category = existing.Category,
                LambdaA = existing.LambdaA,
                LambdaB = existing.LambdaB
            };

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_InvalidCategory_ReturnsInvalid()
        {
            // Arrange
            var material = CreateValidMaterial();
            material.Category = (MaterialCategory)999;

            // Act
            var result = _validator.Validate(material);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Категория")), Is.True);
        }

        private static Material CreateValidMaterial()
        {
            return new Material
            {
                Id = 100,
                Name = "Новый тестовый материал",
                Category = MaterialCategory.Concrete,
                LambdaA = 1.0,
                LambdaB = 1.2,
                MaxSupplyTemp = 50,
                MinOutdoorTemp = -10
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
