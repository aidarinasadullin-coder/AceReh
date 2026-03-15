using NUnit.Framework;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Тесты для MaterialRepository
    /// </summary>
    [TestFixture]
    public class MaterialRepositoryTests
    {
        private MaterialRepository _repository = null!;
        private string _testDataPath = null!;

        [SetUp]
        public void Setup()
        {
            // Создаем временный файл с тестовыми данными
            var tempDir = Path.Combine(Path.GetTempPath(), "SnowMeltingCalculator_Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            _testDataPath = Path.Combine(tempDir, "materials_db.json");

            var testData = @"{
  ""meta"": {
    ""source"": ""Test"",
    ""version"": ""1.0"",
    ""date"": ""2026-01-01""
  },
  ""materials"": [
    {
      ""id"": 1,
      ""name"": ""Песок"",
      ""lambda_A"": 0.4,
      ""lambda_B"": 2.0,
      ""category"": ""грунт""
    },
    {
      ""id"": 2,
      ""name"": ""Бетон плотный"",
      ""lambda_A"": 1.5,
      ""lambda_B"": 1.5,
      ""category"": ""бетон"",
      ""max_supply_temp"": 50
    },
    {
      ""id"": 3,
      ""name"": ""Асфальтобетон"",
      ""lambda_A"": 1.5,
      ""lambda_B"": 1.5,
      ""category"": ""покрытие"",
      ""min_outdoor_temp"": -15
    }
  ]
}";
            File.WriteAllText(_testDataPath, testData);

            _repository = new MaterialRepository(_testDataPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_testDataPath))
            {
                File.Delete(_testDataPath);
            }

            var tempDir = Path.GetDirectoryName(_testDataPath);
            if (tempDir != null && Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        #region LoadMaterialsAsync Tests

        [Test]
        public async Task LoadMaterialsAsync_LoadsDataSuccessfully()
        {
            // Act
            var materials = await _repository.LoadMaterialsAsync();

            // Assert
            Assert.That(materials, Is.Not.Null);
            Assert.That(materials.Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task LoadMaterialsAsync_CalledTwice_LoadsOnce()
        {
            // Act
            await _repository.LoadMaterialsAsync();
            var count1 = _repository.MaterialsCount;

            await _repository.LoadMaterialsAsync();
            var count2 = _repository.MaterialsCount;

            // Assert
            Assert.That(count1, Is.EqualTo(count2));
            Assert.That(_repository.IsLoaded, Is.True);
        }

        [Test]
        public async Task LoadMaterialsAsync_FileNotFound_UsesDefaultMaterials()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".json");
            var repository = new MaterialRepository(nonExistentPath);

            // Act
            var materials = await repository.LoadMaterialsAsync();

            // Assert
            Assert.That(materials, Is.Not.Null);
            Assert.That(materials.Count(), Is.GreaterThan(0));
        }

        #endregion

        #region GetMaterialById Tests

        [Test]
        public async Task GetMaterialById_ExistingId_ReturnsMaterial()
        {
            // Arrange
            await _repository.LoadMaterialsAsync();

            // Act
            var material = _repository.GetMaterialById(1);

            // Assert
            Assert.That(material, Is.Not.Null);
            Assert.That(material!.Id, Is.EqualTo(1));
            Assert.That(material.Name, Is.EqualTo("Песок"));
        }

        [Test]
        public async Task GetMaterialById_NonExistingId_ReturnsNull()
        {
            // Arrange
            await _repository.LoadMaterialsAsync();

            // Act
            var material = _repository.GetMaterialById(999);

            // Assert
            Assert.That(material, Is.Null);
        }

        [Test]
        public void GetMaterialById_NotLoaded_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _repository.GetMaterialById(1));
        }

        #endregion

        #region GetMaterialsByCategory Tests

        [Test]
        public async Task GetMaterialsByCategory_ExistingCategory_ReturnsMaterials()
        {
            // Arrange
            await _repository.LoadMaterialsAsync();

            // Act
            var materials = _repository.GetMaterialsByCategory(MaterialCategory.Soil);

            // Assert
            Assert.That(materials.Count(), Is.EqualTo(1));
            Assert.That(materials.First().Name, Is.EqualTo("Песок"));
        }

        [Test]
        public async Task GetMaterialsByCategory_ConcreteCategory_ReturnsConcreteMaterials()
        {
            // Arrange
            await _repository.LoadMaterialsAsync();

            // Act
            var materials = _repository.GetMaterialsByCategory(MaterialCategory.Concrete);

            // Assert
            Assert.That(materials.Count(), Is.EqualTo(1));
            Assert.That(materials.First().Name, Is.EqualTo("Бетон плотный"));
        }

        [Test]
        public async Task GetMaterialsByCategory_EmptyCategory_ReturnsEmpty()
        {
            // Arrange
            await _repository.LoadMaterialsAsync();

            // Act
            var materials = _repository.GetMaterialsByCategory(MaterialCategory.Insulation);

            // Assert
            Assert.That(materials.Count(), Is.EqualTo(0));
        }

        #endregion

        #region GetAllMaterials Tests

        [Test]
        public async Task GetAllMaterials_ReturnsAllMaterials()
        {
            // Arrange
            await _repository.LoadMaterialsAsync();

            // Act
            var materials = _repository.GetAllMaterials();

            // Assert
            Assert.That(materials.Count(), Is.EqualTo(3));
        }

        [Test]
        public void GetAllMaterials_NotLoaded_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _repository.GetAllMaterials());
        }

        #endregion

        #region Material Properties Tests

        [Test]
        public async Task LoadMaterialsAsync_ParsesLambdaValuesCorrectly()
        {
            // Arrange
            await _repository.LoadMaterialsAsync();

            // Act
            var sand = _repository.GetMaterialById(1);

            // Assert
            Assert.That(sand, Is.Not.Null);
            Assert.That(sand!.LambdaA, Is.EqualTo(0.4).Within(0.001));
            Assert.That(sand.LambdaB, Is.EqualTo(2.0).Within(0.001));
        }

        [Test]
        public async Task LoadMaterialsAsync_ParsesMaxSupplyTempCorrectly()
        {
            // Arrange
            await _repository.LoadMaterialsAsync();

            // Act
            var concrete = _repository.GetMaterialById(2);

            // Assert
            Assert.That(concrete, Is.Not.Null);
            Assert.That(concrete!.MaxSupplyTemp, Is.EqualTo(50));
        }

        [Test]
        public async Task LoadMaterialsAsync_ParsesMinOutdoorTempCorrectly()
        {
            // Arrange
            await _repository.LoadMaterialsAsync();

            // Act
            var asphalt = _repository.GetMaterialById(3);

            // Assert
            Assert.That(asphalt, Is.Not.Null);
            Assert.That(asphalt!.MinOutdoorTemp, Is.EqualTo(-15));
        }

        #endregion
    }
}