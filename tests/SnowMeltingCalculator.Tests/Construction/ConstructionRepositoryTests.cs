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
    /// Тесты для ConstructionRepository
    /// </summary>
    [TestFixture]
    public class ConstructionRepositoryTests
    {
        private ConstructionRepository _repository = null!;
        private MaterialRepository _materialRepository = null!;
        private string _testDataPath = null!;
        private string _materialsDataPath = null!;

        [SetUp]
        public void Setup()
        {
            // Создаем временные директории
            var tempDir = Path.Combine(Path.GetTempPath(), "SnowMeltingCalculator_Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            _testDataPath = Path.Combine(tempDir, "constructions");
            _materialsDataPath = Path.Combine(tempDir, "materials_db.json");

            // Создаем тестовый файл материалов
            var materialsData = @"{
  ""meta"": { ""version"": ""1.0"" },
  ""materials"": [
    { ""id"": 1, ""name"": ""Песок"", ""lambda_A"": 0.4, ""lambda_B"": 2.0, ""category"": ""грунт"" },
    { ""id"": 2, ""name"": ""Бетон плотный"", ""lambda_A"": 1.5, ""lambda_B"": 1.5, ""category"": ""бетон"", ""max_supply_temp"": 50 },
    { ""id"": 3, ""name"": ""Асфальтобетон"", ""lambda_A"": 1.5, ""lambda_B"": 1.5, ""category"": ""покрытие"", ""min_outdoor_temp"": -15 }
  ]
}";
            File.WriteAllText(_materialsDataPath, materialsData);

            _materialRepository = new MaterialRepository(_materialsDataPath);
            _materialRepository.LoadMaterialsAsync().Wait(); // Загружаем материалы
            _repository = new ConstructionRepository(_materialRepository);
        }

        [TearDown]
        public void TearDown()
        {
            var tempDir = Path.GetDirectoryName(_materialsDataPath);
            if (tempDir != null && Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        #region SaveConstructionAsync Tests

        [Test]
        public async Task SaveConstructionAsync_ValidConstruction_SavesToFile()
        {
            // Arrange
            await _materialRepository.LoadMaterialsAsync();
            var construction = CreateTestConstruction();
            var filePath = Path.Combine(_testDataPath, "test_construction.json");

            // Act
            await _repository.SaveConstructionAsync(construction, filePath);

            // Assert
            Assert.That(File.Exists(filePath), Is.True);
        }

        [Test]
        public async Task SaveConstructionAsync_CreatesDirectory_IfNotExists()
        {
            // Arrange
            await _materialRepository.LoadMaterialsAsync();
            var construction = CreateTestConstruction();
            var filePath = Path.Combine(_testDataPath, "subdir", "test_construction.json");

            // Act
            await _repository.SaveConstructionAsync(construction, filePath);

            // Assert
            Assert.That(File.Exists(filePath), Is.True);
        }

        [Test]
        public void SaveConstructionAsync_NullConstruction_ThrowsArgumentNullException()
        {
            // Arrange
            var filePath = Path.Combine(_testDataPath, "test.json");

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _repository.SaveConstructionAsync(null!, filePath));
        }

        #endregion

        #region LoadConstructionAsync Tests

        [Test]
        public async Task LoadConstructionAsync_ExistingFile_ReturnsConstruction()
        {
            // Arrange
            await _materialRepository.LoadMaterialsAsync();
            var construction = CreateTestConstruction();
            var filePath = Path.Combine(_testDataPath, "test_construction.json");
            await _repository.SaveConstructionAsync(construction, filePath);

            // Act
            var loaded = await _repository.LoadConstructionAsync(filePath);

            // Assert
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.GroundwaterLevel, Is.EqualTo(construction.GroundwaterLevel));
            Assert.That(loaded.HasLoads, Is.EqualTo(construction.HasLoads));
        }

        [Test]
        public async Task LoadConstructionAsync_NonExistingFile_ReturnsNull()
        {
            // Act
            var result = await _repository.LoadConstructionAsync("nonexistent.json");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task LoadConstructionAsync_PreservesLayers()
        {
            // Arrange
            await _materialRepository.LoadMaterialsAsync();
            var construction = CreateTestConstruction();
            var filePath = Path.Combine(_testDataPath, "test_construction.json");
            await _repository.SaveConstructionAsync(construction, filePath);

            // Act
            var loaded = await _repository.LoadConstructionAsync(filePath);

            // Assert
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.LayersAbovePipe.Count, Is.EqualTo(construction.LayersAbovePipe.Count));
            Assert.That(loaded.Layers.Count, Is.EqualTo(construction.Layers.Count));
        }

        #endregion

        #region SaveToProjectAsync Tests

        [Test]
        public async Task SaveToProjectAsync_ValidProject_SavesToFile()
        {
            // Arrange
            await _materialRepository.LoadMaterialsAsync();
            var construction = CreateTestConstruction();
            var projectId = 1;

            // Act
            await _repository.SaveToProjectAsync(construction, projectId);

            // Assert - файл должен быть создан в директории projects
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            var expectedPath = Path.Combine(projectRoot, "projects", $"project_{projectId}_construction.json");
            Assert.That(File.Exists(expectedPath), Is.True);

            // Cleanup
            if (File.Exists(expectedPath))
            {
                File.Delete(expectedPath);
            }
        }

        [Test]
        public void SaveToProjectAsync_InvalidProjectId_ThrowsArgumentException()
        {
            // Arrange
            var construction = CreateTestConstruction();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => _repository.SaveToProjectAsync(construction, 0));
            Assert.ThrowsAsync<ArgumentException>(() => _repository.SaveToProjectAsync(construction, -1));
        }

        #endregion

        #region GetSavedConstructionsAsync Tests

        [Test]
        public async Task GetSavedConstructionsAsync_ExistingDirectory_ReturnsFiles()
        {
            // Arrange
            await _materialRepository.LoadMaterialsAsync();
            Directory.CreateDirectory(_testDataPath);
            var construction = CreateTestConstruction();
            await _repository.SaveConstructionAsync(construction, Path.Combine(_testDataPath, "proj1_construction.json"));
            await _repository.SaveConstructionAsync(construction, Path.Combine(_testDataPath, "proj2_construction.json"));

            // Act
            var files = await _repository.GetSavedConstructionsAsync(_testDataPath);

            // Assert
            Assert.That(files.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetSavedConstructionsAsync_NonExistingDirectory_ReturnsEmpty()
        {
            // Act
            var files = await _repository.GetSavedConstructionsAsync("nonexistent_directory");

            // Assert
            Assert.That(files.Count(), Is.EqualTo(0));
        }

        #endregion

        #region Helper Methods

        private SnowMeltingCalculator.Models.Construction.Construction CreateTestConstruction()
        {
            var construction = new SnowMeltingCalculator.Models.Construction.Construction
            {
                GroundwaterLevel = 2.0,
                HasLoads = true
            };

            var concrete = _materialRepository.GetMaterialById(2);
            var sand = _materialRepository.GetMaterialById(1);

            if (concrete != null)
            {
                construction.AddLayerAbovePipe(concrete, 100);
            }

            if (sand != null)
            {
                construction.AddLayerBelowPipe(sand, 150);
            }

            return construction;
        }

        #endregion
    }
}