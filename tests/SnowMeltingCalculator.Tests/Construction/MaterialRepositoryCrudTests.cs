using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    /// <summary>
    /// CRUD, seeding, migration и atomic-write тесты для <see cref="MaterialRepository"/>.
    /// </summary>
    [TestFixture]
    public class MaterialRepositoryCrudTests
    {
        #region AddAsync

        [Test]
        public async Task AddAsync_ValidMaterial_AssignsNextMaterialIdAndAddsToCache()
        {
            // Arrange
            var (repo, path) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();
            var material = new Material
            {
                Name = "Custom Foam",
                Category = MaterialCategory.Insulation,
                LambdaA = 0.04,
                LambdaB = 0.04
            };

            // Act
            var added = await repo.AddAsync(material);

            // Assert
            Assert.That(added.Id, Is.EqualTo(3));
            Assert.That(repo.GetMaterialById(added.Id), Is.Not.Null);
            Assert.That(repo.MaterialsCount, Is.EqualTo(3));
        }

        [Test]
        public void AddAsync_NullMaterial_ThrowsArgumentNullException()
        {
            // Arrange
            var (repo, _) = CreateRepositoryWithLegacyDefaults();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => repo.AddAsync(null!));
        }

        [Test]
        public async Task AddAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            // Arrange
            var (repo, _) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();
            var duplicate = new Material
            {
                Name = "Песок",
                Category = MaterialCategory.Soil,
                LambdaA = 0.1,
                LambdaB = 0.1
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => repo.AddAsync(duplicate));
            Assert.That(ex!.Message, Does.Contain("Песок"));
        }

        [Test]
        public async Task AddAsync_DuplicateNameDifferentCase_ThrowsInvalidOperationException()
        {
            // Arrange
            var (repo, _) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();
            var duplicate = new Material
            {
                Name = "пЕСОК",
                Category = MaterialCategory.Soil,
                LambdaA = 0.1,
                LambdaB = 0.1
            };

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => repo.AddAsync(duplicate));
        }

        #endregion

        #region UpdateAsync

        [Test]
        public async Task UpdateAsync_ExistingMaterial_UpdatesAllProperties()
        {
            // Arrange
            var (repo, _) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();
            var updated = new Material
            {
                Id = 2,
                Name = "Грунт обновленный",
                Category = MaterialCategory.Soil,
                LambdaA = 0.55,
                LambdaB = 1.6,
                MaxSupplyTemp = 40,
                MinOutdoorTemp = -20,
                Notes = "Обновленные примечания"
            };

            // Act
            var result = await repo.UpdateAsync(updated);

            // Assert
            Assert.That(result.Name, Is.EqualTo("Грунт обновленный"));
            Assert.That(result.LambdaA, Is.EqualTo(0.55).Within(0.001));
            Assert.That(result.LambdaB, Is.EqualTo(1.6).Within(0.001));
            Assert.That(result.MaxSupplyTemp, Is.EqualTo(40));
            Assert.That(result.MinOutdoorTemp, Is.EqualTo(-20));
            Assert.That(result.Notes, Is.EqualTo("Обновленные примечания"));
        }

        [Test]
        public async Task UpdateAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            // Arrange
            var (repo, _) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();
            var updated = new Material
            {
                Id = 2,
                Name = "Песок",
                Category = MaterialCategory.Soil,
                LambdaA = 0.5,
                LambdaB = 1.5
            };

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(updated));
        }

        [Test]
        public async Task UpdateAsync_NonExistingId_ThrowsInvalidOperationException()
        {
            // Arrange
            var (repo, _) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();
            var updated = new Material
            {
                Id = 999,
                Name = "Missing",
                Category = MaterialCategory.Soil,
                LambdaA = 0.1,
                LambdaB = 0.1
            };

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(updated));
        }

        [Test]
        public void UpdateAsync_NullMaterial_ThrowsArgumentNullException()
        {
            // Arrange
            var (repo, _) = CreateRepositoryWithLegacyDefaults();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => repo.UpdateAsync(null!));
        }

        #endregion

        #region DeleteAsync

        [Test]
        public async Task DeleteAsync_ExistingId_RemovesMaterialAndReturnsTrue()
        {
            // Arrange
            var (repo, _) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();

            // Act
            var deleted = await repo.DeleteAsync(2);

            // Assert
            Assert.That(deleted, Is.True);
            Assert.That(repo.GetMaterialById(2), Is.Null);
            Assert.That(repo.MaterialsCount, Is.EqualTo(1));
        }

        [Test]
        public async Task DeleteAsync_NonExistingId_ReturnsFalse()
        {
            // Arrange
            var (repo, _) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();

            // Act
            var deleted = await repo.DeleteAsync(999);

            // Assert
            Assert.That(deleted, Is.False);
        }

        #endregion

        #region Save / Round-trip

        [Test]
        public async Task SaveMaterialsAsync_RoundTrip_PreservesAddedAndUpdatedMaterials()
        {
            // Arrange
            var (repo, path) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();

            await repo.AddAsync(new Material
            {
                Name = "Custom One",
                Category = MaterialCategory.Insulation,
                LambdaA = 0.03,
                LambdaB = 0.03
            });

            await repo.UpdateAsync(new Material
            {
                Id = 1,
                Name = "Песок обновленный",
                Category = MaterialCategory.Soil,
                LambdaA = 0.45,
                LambdaB = 2.1
            });

            await repo.DeleteAsync(2);

            // Act
            await repo.SaveMaterialsAsync();

            // Assert
            var reloaded = new MaterialRepository(path);
            var materials = (await reloaded.LoadMaterialsAsync()).ToList();

            Assert.That(materials.Any(m => m.Name == "Custom One"), Is.True);
            Assert.That(materials.First(m => m.Id == 1).Name, Is.EqualTo("Песок обновленный"));
            Assert.That(materials.Any(m => m.Id == 2), Is.False);
        }

        [Test]
        public async Task SaveMaterialsAsync_PreservesIsBuiltInFlags()
        {
            // Arrange
            var (repo, path) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();
            var added = await repo.AddAsync(new Material
            {
                Name = "Custom Non-BuiltIn",
                Category = MaterialCategory.Insulation,
                LambdaA = 0.03,
                LambdaB = 0.03
            });

            // Act
            await repo.SaveMaterialsAsync();

            // Assert
            var reloaded = new MaterialRepository(path);
            await reloaded.LoadMaterialsAsync();

            Assert.That(reloaded.GetMaterialById(1)!.IsBuiltIn, Is.True);
            Assert.That(reloaded.GetMaterialById(added.Id)!.IsBuiltIn, Is.False);
        }

        [Test]
        public async Task SaveMaterialsAsync_AtomicWrite_LeavesNoTempFileAndValidJson()
        {
            // Arrange
            var (repo, path) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();
            await repo.AddAsync(new Material
            {
                Name = "Another Custom",
                Category = MaterialCategory.Coating,
                LambdaA = 0.5,
                LambdaB = 0.5
            });

            // Act
            await repo.SaveMaterialsAsync();

            // Assert
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.Exists(path + ".tmp"), Is.False);

            var json = await File.ReadAllTextAsync(path);
            using var doc = JsonDocument.Parse(json);
            Assert.That(doc.RootElement.GetProperty("materials").GetArrayLength(), Is.EqualTo(3));
        }

        #endregion

        #region Seeding

        [Test]
        public async Task LoadMaterialsAsync_MissingFile_SeedsDefaultsWithBuiltInTrueAndNextId16()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var path = Path.Combine(tempDir, "materials_db.json");

            try
            {
                var repo = new MaterialRepository(path);

                // Act
                var materials = (await repo.LoadMaterialsAsync()).ToList();

                // Assert
                Assert.That(materials.Count, Is.EqualTo(15));
                Assert.That(materials.All(m => m.IsBuiltIn), Is.True);

                var json = await File.ReadAllTextAsync(path);
                using var doc = JsonDocument.Parse(json);
                var meta = doc.RootElement.GetProperty("meta");
                Assert.That(meta.GetProperty("next_material_id").GetInt32(), Is.EqualTo(16));

                var mats = doc.RootElement.GetProperty("materials").EnumerateArray().ToList();
                Assert.That(mats.All(m => m.GetProperty("is_built_in").GetBoolean()), Is.True);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        #endregion

        #region Migration

        [Test]
        public async Task LoadMaterialsAsync_LegacyFileWithoutIsBuiltIn_MigratesDefaultsTrueAndCustomFalse()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var path = Path.Combine(tempDir, "materials_db.json");

            var legacyJson = @"{
  ""meta"": { ""version"": ""1.0"" },
  ""materials"": [
    { ""id"": 1, ""name"": ""Песок"", ""lambda_A"": 0.4, ""lambda_B"": 2.0, ""category"": ""грунт"" },
    { ""id"": 2, ""name"": ""Грунт"", ""lambda_A"": 0.5, ""lambda_B"": 1.5, ""category"": ""грунт"" },
    { ""id"": 99, ""name"": ""Custom Mat"", ""lambda_A"": 0.1, ""lambda_B"": 0.1, ""category"": ""грунт"" }
  ]
}";
            await File.WriteAllTextAsync(path, legacyJson);

            try
            {
                var repo = new MaterialRepository(path);

                // Act
                var materials = (await repo.LoadMaterialsAsync()).ToList();

                // Assert
                Assert.That(materials.First(m => m.Id == 1).IsBuiltIn, Is.True);
                Assert.That(materials.First(m => m.Id == 2).IsBuiltIn, Is.True);
                Assert.That(materials.First(m => m.Id == 99).IsBuiltIn, Is.False);

                await repo.SaveMaterialsAsync();
                var savedJson = await File.ReadAllTextAsync(path);
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

        [Test]
        public async Task MigrateExistingDefaults_SetsIsBuiltInTrue()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var path = Path.Combine(tempDir, "materials_db.json");

            // Сериализуем все 11 дефолтных материалов как legacy JSON (без is_built_in),
            // но для ID 10 намеренно подменяем имя на вариант из реальной data/materials_db.json,
            // чтобы регрессия "Id 10 теряет built-in из-за несовпадения имени" была зафиксирована.
            var defaultMaterials = Material.GetDefaultMaterials();
            var inv = CultureInfo.InvariantCulture;
            var materialsJson = string.Join(",\n    ", defaultMaterials.Select(m =>
                m.Id == 10
                    ? $@"{{ ""id"": {m.Id}, ""name"": ""Пенополистирол (ЭППС)"", ""lambda_A"": {m.LambdaA.ToString(inv)}, ""lambda_B"": {m.LambdaB.ToString(inv)}, ""category"": ""{FormatCategoryRu(m.Category)}"" }}"
                    : $@"{{ ""id"": {m.Id}, ""name"": ""{m.Name}"", ""lambda_A"": {m.LambdaA.ToString(inv)}, ""lambda_B"": {m.LambdaB.ToString(inv)}, ""category"": ""{FormatCategoryRu(m.Category)}"" }}"));

            var legacyJson = $@"{{
  ""meta"": {{ ""version"": ""1.0"" }},
  ""materials"": [
    {materialsJson}
  ]
}}";
            await File.WriteAllTextAsync(path, legacyJson);

            try
            {
                var repo = new MaterialRepository(path);

                // Act
                var materials = (await repo.LoadMaterialsAsync()).ToList();

                // Assert: все 15 default Id, включая Id 10 с локальным вариантом имени, распознаны как built-in.
                Assert.That(materials.Count, Is.EqualTo(15));
                foreach (var defaultMaterial in defaultMaterials)
                {
                    var loaded = materials.FirstOrDefault(m => m.Id == defaultMaterial.Id);
                    Assert.That(loaded, Is.Not.Null, $"Material with Id={defaultMaterial.Id} not found after load");
                    Assert.That(loaded!.IsBuiltIn, Is.True, $"Material with Id={defaultMaterial.Id} must be IsBuiltIn=true after migration");
                }

                // Имя должно сохраниться из JSON, даже если оно отличается от дефолтного.
                Assert.That(materials.First(m => m.Id == 10).Name, Is.EqualTo("Пенополистирол (ЭППС)"));

                await repo.SaveMaterialsAsync();
                var savedJson = await File.ReadAllTextAsync(path);
                using var doc = JsonDocument.Parse(savedJson);
                var savedMaterials = doc.RootElement.GetProperty("materials").EnumerateArray().ToList();
                foreach (var defaultMaterial in defaultMaterials)
                {
                    var saved = savedMaterials.First(m => m.GetProperty("id").GetInt32() == defaultMaterial.Id);
                    Assert.That(saved.GetProperty("is_built_in").GetBoolean(), Is.True, $"Persisted is_built_in for Id={defaultMaterial.Id} must be true");
                }
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        private static string FormatCategoryRu(MaterialCategory category) => category switch
        {
            MaterialCategory.Concrete => "бетон",
            MaterialCategory.Soil => "грунт",
            MaterialCategory.Insulation => "изоляция",
            MaterialCategory.Coating => "покрытие",
            MaterialCategory.Subbase => "подстилающий",
            // MaterialCategory.Screed удалён
            _ => "грунт"
        };

        #endregion

        #region Corrupted JSON

        [Test]
        public async Task LoadMaterialsAsync_CorruptedJson_RethrowsJsonException()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var path = Path.Combine(tempDir, "materials_db.json");
            await File.WriteAllTextAsync(path, "{ this is not valid json");

            try
            {
                var repo = new MaterialRepository(path);

                // Act & Assert
                var ex = Assert.ThrowsAsync<JsonException>(() => repo.LoadMaterialsAsync());
                Assert.That(ex!.Message, Does.Contain("Ошибка десериализации файла материалов"));
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        #endregion

        #region NextMaterialId

        [Test]
        public async Task AddAsync_IncrementsNextMaterialId_AndPersistsAcrossReloads()
        {
            // Arrange
            var (repo, path) = CreateRepositoryWithLegacyDefaults();
            await repo.LoadMaterialsAsync();
            var added1 = await repo.AddAsync(new Material
            {
                Name = "First Custom",
                Category = MaterialCategory.Insulation,
                LambdaA = 0.03,
                LambdaB = 0.03
            });
            var added2 = await repo.AddAsync(new Material
            {
                Name = "Second Custom",
                Category = MaterialCategory.Insulation,
                LambdaA = 0.04,
                LambdaB = 0.04
            });

            // Act
            await repo.SaveMaterialsAsync();

            // Assert
            Assert.That(added2.Id, Is.EqualTo(added1.Id + 1));

            var reloaded = new MaterialRepository(path);
            await reloaded.LoadMaterialsAsync();
            var added3 = await reloaded.AddAsync(new Material
            {
                Name = "Third Custom",
                Category = MaterialCategory.Insulation,
                LambdaA = 0.05,
                LambdaB = 0.05
            });

            Assert.That(added3.Id, Is.EqualTo(added2.Id + 1));
        }

        [Test]
        public async Task LoadMaterialsAsync_MissingNextMaterialId_FallsBackToMaxIdPlusOne()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var path = Path.Combine(tempDir, "materials_db.json");

            var json = @"{
  ""meta"": { ""version"": ""1.0"" },
  ""materials"": [
    { ""id"": 1, ""name"": ""Песок"", ""lambda_A"": 0.4, ""lambda_B"": 2.0, ""category"": ""грунт"", ""is_built_in"": true },
    { ""id"": 50, ""name"": ""Custom Mat"", ""lambda_A"": 0.1, ""lambda_B"": 0.1, ""category"": ""грунт"", ""is_built_in"": false }
  ]
}";
            await File.WriteAllTextAsync(path, json);

            try
            {
                var repo = new MaterialRepository(path);
                await repo.LoadMaterialsAsync();
                var added = await repo.AddAsync(new Material
                {
                    Name = "Next After Max",
                    Category = MaterialCategory.Insulation,
                    LambdaA = 0.02,
                    LambdaB = 0.02
                });

                Assert.That(added.Id, Is.EqualTo(51));
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Создаёт временный файл с legacy JSON без is_built_in (два стандартных материала)
        /// и возвращает репозиторий + путь к файлу.
        /// </summary>
        private static (MaterialRepository Repository, string Path) CreateRepositoryWithLegacyDefaults()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var path = System.IO.Path.Combine(tempDir, "materials_db.json");

            var legacyJson = @"{
  ""meta"": { ""version"": ""1.0"" },
  ""materials"": [
    { ""id"": 1, ""name"": ""Песок"", ""lambda_A"": 0.4, ""lambda_B"": 2.0, ""category"": ""грунт"" },
    { ""id"": 2, ""name"": ""Грунт"", ""lambda_A"": 0.5, ""lambda_B"": 1.5, ""category"": ""грунт"" }
  ]
}";
            File.WriteAllText(path, legacyJson);

            return (new MaterialRepository(path), path);
        }

        #endregion
    }
}
