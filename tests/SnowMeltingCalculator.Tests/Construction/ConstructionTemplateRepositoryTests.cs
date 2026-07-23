using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Repositories.Construction;

namespace SnowMeltingCalculator.Tests.Construction
{
    [TestFixture]
    public class ConstructionTemplateRepositoryTests
    {
        private string _tempDir = null!;
        private string _dataPath = null!;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SnowMeltingCalculator_Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
            _dataPath = Path.Combine(_tempDir, "construction_templates.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private ConstructionTemplateRepository CreateRepository(string? path = null)
            => new ConstructionTemplateRepository(path ?? _dataPath);

        private static ConstructionTemplate CreateTemplate(string name, bool isBuiltIn = false)
        {
            return new ConstructionTemplate
            {
                Name = name,
                Description = "Test template",
                HasLoads = false,
                DefaultGroundwaterLevel = 2.0,
                IsBuiltIn = isBuiltIn,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 7, Thickness = 50, Position = LayerPosition.AbovePipe, Order = 0 }
                },
                LayersBelowPipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 1, Thickness = 150, Position = LayerPosition.BelowPipe, Order = 0 }
                }
            };
        }

        private static ConstructionTemplate CreateLayeredTemplate(string name)
        {
            return new ConstructionTemplate
            {
                Name = name,
                Description = "Layered test template",
                HasLoads = true,
                DefaultGroundwaterLevel = 1.5,
                IsBuiltIn = false,
                LayersAbovePipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 7, Thickness = 50, Position = LayerPosition.AbovePipe, Order = 0 },
                    new LayerTemplate { MaterialId = 5, Thickness = 100, Position = LayerPosition.AbovePipe, Order = 1 }
                },
                LayersBelowPipe = new List<LayerTemplate>
                {
                    new LayerTemplate { MaterialId = 1, Thickness = 150, Position = LayerPosition.BelowPipe, Order = 0 },
                    new LayerTemplate { MaterialId = 2, Thickness = 200, Position = LayerPosition.BelowPipe, Order = 1 }
                }
            };
        }

        [Test]
        public async Task GetAllAsync_MissingFile_SeedsDefaultsWithIsBuiltInTrue()
        {
            var repository = CreateRepository();
            var templates = (await repository.GetAllAsync()).ToList();

            Assert.That(templates.Count, Is.EqualTo(3));
            Assert.That(templates.All(t => t.IsBuiltIn), Is.True);
            Assert.That(File.Exists(_dataPath), Is.True);

            await repository.SaveAsync();

            var savedJson = await File.ReadAllTextAsync(_dataPath);
            using var doc = JsonDocument.Parse(savedJson);
            Assert.That(doc.RootElement.GetProperty("meta").GetProperty("next_template_id").GetInt32(), Is.EqualTo(5));
        }

        [Test]
        public async Task GetByIdAsync_ExistingTemplate_ReturnsTemplate()
        {
            var repository = CreateRepository();
            var template = await repository.GetByIdAsync(1);

            Assert.That(template, Is.Not.Null);
            Assert.That(template!.Name, Is.EqualTo("Парковка / площадка — бетон"));
        }

        [Test]
        public async Task GetByIdAsync_MissingTemplate_ReturnsNull()
        {
            var repository = CreateRepository();
            var template = await repository.GetByIdAsync(999);

            Assert.That(template, Is.Null);
        }

        [Test]
        public async Task AddAsync_AssignsIdAndIncrementsNextTemplateId()
        {
            var repository = CreateRepository();
            var template = CreateTemplate("Пользовательский шаблон");

            var added = await repository.AddAsync(template);

            Assert.That(added.Id, Is.EqualTo(5));
            Assert.That(added.IsBuiltIn, Is.False);

            await repository.SaveAsync();

            var savedJson = await File.ReadAllTextAsync(_dataPath);
            using var doc = JsonDocument.Parse(savedJson);
            Assert.That(doc.RootElement.GetProperty("meta").GetProperty("next_template_id").GetInt32(), Is.EqualTo(6));
        }

        [Test]
        public void AddAsync_NullTemplate_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();
            Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!));
        }

        [Test]
        public async Task AddAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            var repository = CreateRepository();
            await repository.AddAsync(CreateTemplate("Custom"));

            var duplicate = CreateTemplate("custom");
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(duplicate));

            Assert.That(ex!.Message, Does.Contain("Custom").IgnoreCase);
        }

        [Test]
        public async Task UpdateAsync_ExistingTemplate_UpdatesProperties()
        {
            var repository = CreateRepository();
            var added = await repository.AddAsync(CreateTemplate("Original"));

            var update = CreateTemplate("Updated");
            update.Id = added.Id;
            update.Description = "Updated description";
            update.HasLoads = true;
            update.DefaultGroundwaterLevel = 3.0;
            update.LayersAbovePipe.Add(new LayerTemplate
            {
                MaterialId = 6,
                Thickness = 120,
                Position = LayerPosition.AbovePipe,
                Order = 1
            });

            var updated = await repository.UpdateAsync(update);

            Assert.That(updated.Name, Is.EqualTo("Updated"));
            Assert.That(updated.Description, Is.EqualTo("Updated description"));
            Assert.That(updated.HasLoads, Is.True);
            Assert.That(updated.DefaultGroundwaterLevel, Is.EqualTo(3.0));
            Assert.That(updated.LayersAbovePipe.Count, Is.EqualTo(2));
            Assert.That(updated.LayersAbovePipe[1].MaterialId, Is.EqualTo(6));
        }

        [Test]
        public void UpdateAsync_NullTemplate_ThrowsArgumentNullException()
        {
            var repository = CreateRepository();
            Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateAsync(null!));
        }

        [Test]
        public void UpdateAsync_NonExistingTemplate_ThrowsInvalidOperationException()
        {
            var repository = CreateRepository();
            var template = CreateTemplate("Ghost");
            template.Id = 999;

            Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(template));
        }

        [Test]
        public async Task UpdateAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            var repository = CreateRepository();
            var first = await repository.AddAsync(CreateTemplate("First"));
            await repository.AddAsync(CreateTemplate("Second"));

            var update = CreateTemplate("second");
            update.Id = first.Id;

            Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(update));
        }

        [Test]
        public async Task DeleteAsync_ExistingTemplate_ReturnsTrueAndRemoves()
        {
            var repository = CreateRepository();
            var added = await repository.AddAsync(CreateTemplate("ToDelete"));

            var deleted = await repository.DeleteAsync(added.Id);
            var loaded = await repository.GetByIdAsync(added.Id);

            Assert.That(deleted, Is.True);
            Assert.That(loaded, Is.Null);
        }

        [Test]
        public async Task DeleteAsync_NonExistingTemplate_ReturnsFalse()
        {
            var repository = CreateRepository();
            var result = await repository.DeleteAsync(999);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task SaveAsync_RoundTrip_PreservesTemplatesAndLayers()
        {
            var repository = CreateRepository();
            var added = await repository.AddAsync(CreateLayeredTemplate("RoundTrip"));
            await repository.SaveAsync();

            var roundTrip = CreateRepository();
            var loaded = (await roundTrip.GetAllAsync()).ToList();
            var template = loaded.First(t => t.Id == added.Id);

            Assert.That(template.Name, Is.EqualTo("RoundTrip"));
            Assert.That(template.LayersAbovePipe.Count, Is.EqualTo(2));
            Assert.That(template.LayersBelowPipe.Count, Is.EqualTo(2));
            Assert.That(template.LayersAbovePipe[0].Position, Is.EqualTo(LayerPosition.AbovePipe));
            Assert.That(template.LayersBelowPipe[1].Thickness, Is.EqualTo(200));
        }

        [Test]
        public async Task IsBuiltIn_PreservedThroughRoundTrip()
        {
            var repository = CreateRepository();
            await repository.AddAsync(CreateTemplate("UserTemplate", isBuiltIn: false));
            await repository.SaveAsync();

            var roundTrip = CreateRepository();
            var templates = (await roundTrip.GetAllAsync()).ToList();

            Assert.That(templates.Where(t => t.IsBuiltIn).Count(), Is.EqualTo(3));
            Assert.That(templates.Any(t => t.Name == "UserTemplate" && !t.IsBuiltIn), Is.True);
        }

        [Test]
        public async Task BuiltInTemplate_UpdateAndDelete_RepositoryAllowsChanges()
        {
            var repository = CreateRepository();
            var builtIn = await repository.GetByIdAsync(1);
            Assert.That(builtIn, Is.Not.Null);

            builtIn!.Name = "Типовая парковка (modified)";
            await repository.UpdateAsync(builtIn);

            var afterUpdate = await repository.GetByIdAsync(1);
            Assert.That(afterUpdate!.Name, Is.EqualTo("Типовая парковка (modified)"));

            var deleted = await repository.DeleteAsync(3);
            Assert.That(deleted, Is.True);
            Assert.That(await repository.GetByIdAsync(3), Is.Null);
        }

        [Test]
        public async Task MigrateExistingDefaults_SetsIsBuiltInTrue()
        {
            var legacyJson = @"{
  ""meta"": { ""version"": ""1.0"" },
  ""templates"": [
    {
      ""id"": 1,
      ""name"": ""Парковка / площадка — бетон"",
      ""description"": ""Стандартная конструкция для парковок"",
      ""has_loads"": true,
      ""default_groundwater_level"": 2.0,
      ""layers_above_pipe"": [
        { ""material_id"": 7, ""thickness"": 50, ""position"": ""above_pipe"", ""order"": 0 }
      ],
      ""layers_below_pipe"": [
        { ""material_id"": 1, ""thickness"": 150, ""position"": ""below_pipe"", ""order"": 0 }
      ]
    },
    {
      ""id"": 99,
      ""name"": ""Пользовательский шаблон"",
      ""description"": ""Custom"",
      ""has_loads"": false,
      ""default_groundwater_level"": 2.0,
      ""layers_above_pipe"": [],
      ""layers_below_pipe"": []
    }
  ]
}";
            await File.WriteAllTextAsync(_dataPath, legacyJson);

            var repository = CreateRepository();
            var templates = (await repository.GetAllAsync()).ToList();

            Assert.That(templates.First(t => t.Id == 1).IsBuiltIn, Is.True);
            Assert.That(templates.First(t => t.Id == 99).IsBuiltIn, Is.False);

            await repository.SaveAsync();

            var savedJson = await File.ReadAllTextAsync(_dataPath);
            using var doc = JsonDocument.Parse(savedJson);
            var savedTemplates = doc.RootElement.GetProperty("templates").EnumerateArray().ToList();
            Assert.That(savedTemplates.First(t => t.GetProperty("id").GetInt32() == 1).GetProperty("is_built_in").GetBoolean(), Is.True);
            Assert.That(savedTemplates.First(t => t.GetProperty("id").GetInt32() == 99).GetProperty("is_built_in").GetBoolean(), Is.False);
        }

        [Test]
        public async Task MigrateExistingJson_WithoutNextTemplateId_SeedsFromMaxId()
        {
            var legacyJson = @"{
  ""meta"": { ""version"": ""1.0"" },
  ""templates"": [
    {
      ""id"": 1,
      ""name"": ""Типовая парковка"",
      ""description"": """",
      ""has_loads"": true,
      ""default_groundwater_level"": 2.0,
      ""layers_above_pipe"": [],
      ""layers_below_pipe"": []
    },
    {
      ""id"": 99,
      ""name"": ""Legacy custom"",
      ""description"": """",
      ""has_loads"": false,
      ""default_groundwater_level"": 2.0,
      ""layers_above_pipe"": [],
      ""layers_below_pipe"": []
    }
  ]
}";
            await File.WriteAllTextAsync(_dataPath, legacyJson);

            var repository = CreateRepository();
            await repository.GetAllAsync();

            var added = await repository.AddAsync(CreateTemplate("NewAfterLegacy"));

            Assert.That(added.Id, Is.EqualTo(100));

            await repository.SaveAsync();
            var savedJson = await File.ReadAllTextAsync(_dataPath);
            using var doc = JsonDocument.Parse(savedJson);
            Assert.That(doc.RootElement.GetProperty("meta").GetProperty("next_template_id").GetInt32(), Is.EqualTo(101));
        }
    }
}
