using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Construction;
using ConstructionModel = SnowMeltingCalculator.Models.Construction.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// Тесты сериализации MaterialSnapshot и обратной совместимости.
    /// </summary>
    [TestFixture]
    public class MaterialSnapshotTests
    {
        private ConstructionRepository _repository = null!;
        private MockMaterialRepository _materialRepository = null!;
        private string _tempDir = null!;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SMC_MaterialSnapshotTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
            _materialRepository = new MockMaterialRepository();
            _repository = new ConstructionRepository(_materialRepository);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        private ConstructionModel CreateTestConstruction()
        {
            var construction = new ConstructionModel
            {
                GroundwaterLevel = 2.0,
                HasLoads = false
            };

            var concrete = _materialRepository.GetMaterialById(5) ?? Material.GetDefaultMaterial();
            var sand = _materialRepository.GetMaterialById(1) ?? Material.GetDefaultMaterials().First(m => m.Name == "Песок");

            construction.AddLayerAbovePipe(concrete, 100);
            construction.AddLayerBelowPipe(sand, 150);

            return construction;
        }

        [Test]
        public async Task SaveConstructionAsync_SerializesMaterialSnapshots()
        {
            // Arrange
            var construction = CreateTestConstruction();
            var filePath = Path.Combine(_tempDir, "construction.json");

            // Act
            await _repository.SaveConstructionAsync(construction, filePath);
            var json = await File.ReadAllTextAsync(filePath);

            // Assert
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            Assert.That(root.TryGetProperty("material_snapshots", out var snapshots), Is.True);

            var snapshotArray = snapshots.EnumerateArray().ToList();
            Assert.That(snapshotArray.Count, Is.EqualTo(2));
            Assert.That(snapshotArray.Any(s => s.GetProperty("id").GetInt32() == 5), Is.True);
            Assert.That(snapshotArray.Any(s => s.GetProperty("id").GetInt32() == 1), Is.True);
            Assert.That(snapshotArray.Any(s => s.GetProperty("name").GetString() == "Бетон"), Is.True);
            Assert.That(snapshotArray.Any(s => s.GetProperty("name").GetString() == "Песок"), Is.True);
        }

        [Test]
        public async Task LoadConstructionAsync_OldJsonWithoutSnapshots_DeserializesAndResolvesByMaterialId()
        {
            // Arrange
            var filePath = Path.Combine(_tempDir, "old_construction.json");
            var oldJson = @"{
  ""version"": ""1.0"",
  ""groundwater_level"": 2.0,
  ""has_loads"": false,
  ""layers_above_pipe"": [
    { ""material_id"": 5, ""thickness"": 100, ""calculated_lambda"": 1.5, ""is_lambda_overridden"": false, ""position"": 0, ""order"": 0 }
  ],
  ""layers_below_pipe"": [
    { ""material_id"": 1, ""thickness"": 150, ""calculated_lambda"": 0.4, ""is_lambda_overridden"": false, ""position"": 1, ""order"": 0 }
  ]
}";
            await File.WriteAllTextAsync(filePath, oldJson);

            // Act
            var loaded = await _repository.LoadConstructionAsync(filePath);

            // Assert
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.LayersAbovePipe.Count, Is.EqualTo(1));
            Assert.That(loaded.LayersAbovePipe[0].Material?.Id, Is.EqualTo(5));
            Assert.That(loaded.Layers.Count, Is.EqualTo(1));
            Assert.That(loaded.Layers.Any(l => l.Position == LayerPosition.BelowPipe && l.Material?.Id == 1), Is.True);
        }

        [Test]
        public async Task LoadConstructionAsync_MissingMaterialWithSnapshot_PropagatesMaterialNotFoundExceptionWithSnapshot()
        {
            // Arrange
            var filePath = Path.Combine(_tempDir, "missing_with_snapshot.json");
            var json = @"{
  ""version"": ""1.1"",
  ""groundwater_level"": 2.0,
  ""has_loads"": false,
  ""layers_above_pipe"": [
    { ""material_id"": 999, ""thickness"": 100, ""calculated_lambda"": 1.5, ""is_lambda_overridden"": false, ""position"": 0, ""order"": 0 }
  ],
  ""layers_below_pipe"": [],
  ""material_snapshots"": [
    { ""id"": 999, ""name"": ""Custom"", ""category"": 0, ""lambda_a"": 1.2, ""lambda_b"": 1.2, ""max_supply_temp"": null, ""min_outdoor_temp"": null, ""notes"": null, ""is_built_in"": false }
  ]
}";
            await File.WriteAllTextAsync(filePath, json);

            // Act & Assert
            var materialEx = Assert.ThrowsAsync<MaterialNotFoundException>(() => _repository.LoadConstructionAsync(filePath))!;
            Assert.That(materialEx, Is.Not.Null);
            Assert.That(materialEx.MaterialId, Is.EqualTo(999));
            Assert.That(materialEx.Snapshot, Is.Not.Null);
            Assert.That(materialEx.Snapshot!.Id, Is.EqualTo(999));
            Assert.That(materialEx.Snapshot.Name, Is.EqualTo("Custom"));
        }

        [Test]
        public async Task LoadConstructionAsync_MissingMaterialWithoutSnapshot_PropagatesMaterialNotFoundExceptionWithoutSnapshot()
        {
            // Arrange
            var filePath = Path.Combine(_tempDir, "missing_without_snapshot.json");
            var json = @"{
  ""version"": ""1.1"",
  ""groundwater_level"": 2.0,
  ""has_loads"": false,
  ""layers_above_pipe"": [
    { ""material_id"": 999, ""thickness"": 100, ""calculated_lambda"": 1.5, ""is_lambda_overridden"": false, ""position"": 0, ""order"": 0 }
  ],
  ""layers_below_pipe"": []
}";
            await File.WriteAllTextAsync(filePath, json);

            // Act & Assert
            var materialEx = Assert.ThrowsAsync<MaterialNotFoundException>(() => _repository.LoadConstructionAsync(filePath))!;
            Assert.That(materialEx, Is.Not.Null);
            Assert.That(materialEx.MaterialId, Is.EqualTo(999));
            Assert.That(materialEx.Snapshot, Is.Null);
        }

        [Test]
        public void MaterialSnapshot_FromMaterial_MapsAllProperties()
        {
            // Arrange
            var material = Material.GetDefaultMaterials().First(m => m.Id == 5);

            // Act
            var snapshot = MaterialSnapshot.FromMaterial(material);

            // Assert
            Assert.That(snapshot.Id, Is.EqualTo(material.Id));
            Assert.That(snapshot.Name, Is.EqualTo(material.Name));
            Assert.That(snapshot.Category, Is.EqualTo(material.Category));
            Assert.That(snapshot.LambdaA, Is.EqualTo(material.LambdaA));
            Assert.That(snapshot.LambdaB, Is.EqualTo(material.LambdaB));
            Assert.That(snapshot.MaxSupplyTemp, Is.EqualTo(material.MaxSupplyTemp));
            Assert.That(snapshot.MinOutdoorTemp, Is.EqualTo(material.MinOutdoorTemp));
            Assert.That(snapshot.Notes, Is.EqualTo(material.Notes));
            Assert.That(snapshot.IsBuiltIn, Is.EqualTo(material.IsBuiltIn));
        }
    }
}
