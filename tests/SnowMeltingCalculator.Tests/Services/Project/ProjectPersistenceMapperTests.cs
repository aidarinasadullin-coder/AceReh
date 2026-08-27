using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Construction;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public sealed class ProjectPersistenceMapperTests
    {
        [Test]
        public void ToProjectData_MapsLifecycleDatesAndAllFourModules()
        {
            var snapshot = CreateSnapshot();
            var now = new DateTime(2026, 8, 25, 12, 30, 0, DateTimeKind.Utc);
            var priorCreated = new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc);

            var data = ProjectPersistenceMapper.ToProjectData(
                snapshot,
                new ProjectSaveDates(priorCreated, now),
                new MaterialRepositoryFake());

            Assert.Multiple(() =>
            {
                Assert.That(data.Version, Is.EqualTo("1.1"));
                Assert.That(data.ProjectNumber, Is.EqualTo("PR-42"));
                Assert.That(data.ProjectObject, Is.EqualTo("Object"));
                Assert.That(data.CreatedDate, Is.EqualTo(priorCreated));
                Assert.That(data.ModifiedDate, Is.EqualTo(now));
                Assert.That(data.IsOperatingMode, Is.False);
                Assert.That(data.ClimateData.SelectedCity, Is.EqualTo("Москва"));
                Assert.That(data.ConstructionData.HasLoads, Is.True);
                Assert.That(data.ThermalData.SelectedMode, Is.EqualTo(OperatingMode.Melting));
                Assert.That(data.HydraulicsData.GlycolType, Is.EqualTo(GlycolType.Propylene));
            });
        }

        [Test]
        public void ToProjectData_FirstSaveUsesNowForCreatedAndModified()
        {
            var now = new DateTime(2026, 8, 25, 13, 0, 0, DateTimeKind.Utc);

            var data = ProjectPersistenceMapper.ToProjectData(
                CreateSnapshot(),
                new ProjectSaveDates(DateTime.MinValue, now),
                new MaterialRepositoryFake());

            Assert.Multiple(() =>
            {
                Assert.That(data.CreatedDate, Is.EqualTo(now));
                Assert.That(data.ModifiedDate, Is.EqualTo(now));
            });
        }

        [Test]
        public void ToProjectData_MapsCustomMaterialsAndTemplatesWithoutFieldOrOrderLoss()
        {
            var material = new ProjectCustomMaterialRecord(
                77, "Custom", MaterialCategory.Insulation, 0.31, 0.42, 45, -18, "note", false);
            var template = new ProjectTemplateRecord(
                19,
                "Template",
                "Description",
                new[] { new ProjectTemplateLayerRecord(77, 20, LayerPosition.AbovePipe, 4) },
                new[] { new ProjectTemplateLayerRecord(5, 30, LayerPosition.BelowPipe, 0) },
                true,
                0.9,
                false,
                new[] { material });
            var snapshot = CreateSnapshot(new[] { material }, new[] { template });

            var data = ProjectPersistenceMapper.ToProjectData(
                snapshot,
                new ProjectSaveDates(DateTime.MinValue, DateTime.UtcNow),
                new MaterialRepositoryFake());

            var actualMaterial = data.CustomMaterials.Single();
            var actualTemplate = data.CustomTemplates.Single();
            Assert.Multiple(() =>
            {
                Assert.That(actualMaterial.Id, Is.EqualTo(77));
                Assert.That(actualMaterial.LambdaB, Is.EqualTo(0.42));
                Assert.That(actualMaterial.MaxSupplyTemp, Is.EqualTo(45));
                Assert.That(actualTemplate.Id, Is.EqualTo(19));
                Assert.That(actualTemplate.LayersAbovePipe[0].MaterialId, Is.EqualTo(77));
                Assert.That(actualTemplate.LayersAbovePipe[0].Order, Is.EqualTo(4));
                Assert.That(actualTemplate.MaterialSnapshots[0].Name, Is.EqualTo("Custom"));
            });
        }

        [Test]
        public void ToProjectData_NullInputsThrowArgumentNullException()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => ProjectPersistenceMapper.ToProjectData(null!, default, new MaterialRepositoryFake()), Throws.ArgumentNullException);
                Assert.That(() => ProjectPersistenceMapper.ToProjectData(CreateSnapshot(), default, null!), Throws.ArgumentNullException);
            });
        }

        [Test]
        public void ToProjectData_SerializedWireNamesAndEnumValuesMatchLiveOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
            var data = ProjectPersistenceMapper.ToProjectData(
                CreateSnapshot(),
                new ProjectSaveDates(DateTime.MinValue, DateTime.UtcNow),
                new MaterialRepositoryFake());

            using var document = JsonDocument.Parse(JsonSerializer.Serialize(data, options));
            var root = document.RootElement;
            Assert.Multiple(() =>
            {
                Assert.That(root.GetProperty("version").GetString(), Is.EqualTo("1.1"));
                Assert.That(root.GetProperty("climateData").GetProperty("selectedZone").GetString(), Is.EqualTo("zone_M15"));
                Assert.That(root.GetProperty("hydraulicsData").GetProperty("glycolType").GetString(), Is.EqualTo("propylene"));
                Assert.That(root.TryGetProperty("createdDate", out _), Is.True);
                Assert.That(root.TryGetProperty("modifiedDate", out _), Is.True);
            });
        }

        private static ProjectSnapshot CreateSnapshot(
            IEnumerable<ProjectCustomMaterialRecord>? materials = null,
            IEnumerable<ProjectTemplateRecord>? templates = null) =>
            new(
                "PR-42",
                "Object",
                false,
                new ClimateStateSnapshot("Москва", "Московская область", -15, -28, 5, 70, 1, ClimateZone.Zone_M15, false, true, false),
                new ConstructionStateSnapshot(
                    1.5,
                    true,
                    new[] { new ConstructionLayerSnapshot(Guid.NewGuid(), 5, "Concrete", 100, 0.2, false, LayerPosition.AbovePipe, 0) },
                    Array.Empty<ConstructionLayerSnapshot>()),
                ThermalStateSnapshot.Default,
                new HydraulicsStateSnapshot(
                    new HydraulicGlobalInputsSnapshot(GlycolType.Propylene, 35, 5, 10),
                    Array.Empty<HydraulicCollectorSnapshot>(),
                    HydraulicsStatusSnapshot.Default),
                materials ?? Array.Empty<ProjectCustomMaterialRecord>(),
                templates ?? Array.Empty<ProjectTemplateRecord>());

        private sealed class MaterialRepositoryFake : IMaterialRepository
        {
            private readonly Material _concrete = new() { Id = 5, LambdaA = 1.74, Name = "Concrete" };

            public Material? GetMaterialById(int id) => id == 5 ? _concrete : null;
            public IEnumerable<Material> GetAllMaterials() => new[] { _concrete };
            public Task<IEnumerable<Material>> LoadMaterialsAsync() => Task.FromResult<IEnumerable<Material>>(GetAllMaterials());
            public IEnumerable<Material> GetMaterialsByCategory(MaterialCategory category) => GetAllMaterials().Where(m => m.Category == category);
            public Task<Material> AddAsync(Material material) => Task.FromResult(material);
            public Task<Material> UpdateAsync(Material material) => Task.FromResult(material);
            public Task<bool> DeleteAsync(int id) => Task.FromResult(false);
            public Task SaveMaterialsAsync() => Task.CompletedTask;
            public bool IsLoaded => true;
            public int MaterialsCount => 1;
        }
    }
}
