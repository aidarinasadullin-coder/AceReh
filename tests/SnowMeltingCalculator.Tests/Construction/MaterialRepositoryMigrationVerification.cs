using System.IO;
using System.Text.Json;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    public class MaterialRepositoryMigrationVerification
    {
        [Test]
        public async Task MissingFile_CreatesSeededFile_WithBuiltInMaterialsAndNextId()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var dataPath = Path.Combine(tempDir, "materials_db.json");

            try
            {
                var repository = new MaterialRepository(dataPath);
                var materials = (await repository.LoadMaterialsAsync()).ToList();

                Assert.That(File.Exists(dataPath), Is.True);
                Assert.That(materials.Count, Is.EqualTo(9));
                foreach (var m in materials)
                    Assert.That(m.IsBuiltIn, Is.True);

                var json = await File.ReadAllTextAsync(dataPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var meta = root.GetProperty("meta");
                Assert.That(meta.GetProperty("next_material_id").GetInt32(), Is.EqualTo(14));

                var mats = root.GetProperty("materials").EnumerateArray().ToList();
                Assert.That(mats.Count, Is.EqualTo(9));
                foreach (var m in mats)
                    Assert.That(m.GetProperty("is_built_in").GetBoolean(), Is.True);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Test]
        public async Task ExistingFile_WithoutIsBuiltIn_MigratesOnSave()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var dataPath = Path.Combine(tempDir, "materials_db.json");

            var legacyJson = @"{
  ""meta"": { ""version"": ""1.1"" },
  ""materials"": [
    { ""id"": 1, ""name"": ""Песок"", ""lambda_A"": 0.4, ""lambda_B"": 2.0, ""category"": ""грунт"" },
    { ""id"": 2, ""name"": ""Грунт"", ""lambda_A"": 0.5, ""lambda_B"": 1.5, ""category"": ""грунт"" },
    { ""id"": 99, ""name"": ""Custom Mat"", ""lambda_A"": 0.1, ""lambda_B"": 0.1, ""category"": ""грунт"" }
  ]
}";
            await File.WriteAllTextAsync(dataPath, legacyJson);

            try
            {
                var repository = new MaterialRepository(dataPath);
                var materials = (await repository.LoadMaterialsAsync()).ToList();

                Assert.That(materials.First(m => m.Id == 1).IsBuiltIn, Is.True);
                Assert.That(materials.First(m => m.Id == 2).IsBuiltIn, Is.True);
                Assert.That(materials.First(m => m.Id == 99).IsBuiltIn, Is.False);

                await repository.SaveMaterialsAsync();

                var savedJson = await File.ReadAllTextAsync(dataPath);
                using var doc = JsonDocument.Parse(savedJson);
                var savedMaterials = doc.RootElement.GetProperty("materials").EnumerateArray().ToList();
                Assert.That(savedMaterials.First(m => m.GetProperty("id").GetInt32() == 1).GetProperty("is_built_in").GetBoolean(), Is.True);
                Assert.That(savedMaterials.First(m => m.GetProperty("id").GetInt32() == 2).GetProperty("is_built_in").GetBoolean(), Is.True);
                Assert.That(savedMaterials.First(m => m.GetProperty("id").GetInt32() == 99).GetProperty("is_built_in").GetBoolean(), Is.False);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
