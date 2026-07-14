using System.IO;
using System.Text.Json;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Project
{
    [TestFixture]
    public class ProjectRoundTripTests
    {
        private static string FixturePath => Path.Combine(
            Path.GetDirectoryName(typeof(ProjectRoundTripTests).Assembly.Location)!,
            "..", "..", "..", "Fixtures", "v1-sample.smc");

        private ProjectFileService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new ProjectFileService();
        }

        [Test]
        public async Task Load_v1_Fixture_PreservesCanonicalFields()
        {
            var path = Path.GetFullPath(FixturePath);
            var data = await _service.LoadProjectAsync(path);

            Assert.That(data, Is.Not.Null);
            Assert.That(data!.Version, Is.EqualTo("1.0"));
            Assert.That(data.ThermalData.PipeSpacing, Is.EqualTo(250));
            Assert.That(data.ThermalData.SelectedPipe, Is.Not.Null);
            Assert.That(data.ThermalData.SelectedPipe!.Name, Is.EqualTo("RAUTHERM S 20x2,0"));
            Assert.That(data.ThermalData.SelectedPipe.OuterDiameter, Is.EqualTo(20.0));
            Assert.That(data.ThermalData.SelectedPipe.InnerDiameter, Is.EqualTo(16.0));
            Assert.That(data.ThermalData.SelectedPipe.WallThickness, Is.EqualTo(2.0));
            Assert.That(data.ConstructionData.R1, Is.EqualTo(0.0875));
            Assert.That(data.ConstructionData.R2, Is.EqualTo(0.175));
            Assert.That(data.HydraulicsData.Collectors, Has.Count.EqualTo(1));
            Assert.That(data.HydraulicsData.Collectors[0].Circuits, Has.Count.EqualTo(1));
            Assert.That(data.HydraulicsData.Collectors[0].Circuits[0].PipeSpacingCm, Is.EqualTo(25.0));
        }

        [Test]
        public async Task SaveThenLoad_NewProject_RoundTripsFields()
        {
            var data = new ProjectData
            {
                Version = "1.0",
                ProjectNumber = "T19-RT-001",
                ProjectObject = "round-trip sample",
                ThermalData = new ThermalProjectData
                {
                    PipeSpacing = 300,
                    SelectedPipe = new PipeTypeProjectData
                    {
                        Name = "RAUTHERM S 25x2,3",
                        OuterDiameter = 25.0,
                        InnerDiameter = 20.4,
                        WallThickness = 2.3
                    }
                },
                ConstructionData = new ConstructionProjectData
                {
                    R1 = 0.1,
                    R2 = 0.2
                },
                HydraulicsData = new HydraulicsProjectData
                {
                    Collectors = new List<CollectorProjectData>
                    {
                        new CollectorProjectData
                        {
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData { PipeSpacingCm = 30.0 }
                            }
                        }
                    }
                }
            };

            var tempPath = Path.Combine(Path.GetTempPath(), $"t19-rt-{Guid.NewGuid()}.smc");
            try
            {
                var saved = await _service.SaveProjectAsync(tempPath, data);
                Assert.That(saved, Is.True);

                var loaded = await _service.LoadProjectAsync(tempPath);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.ThermalData.PipeSpacing, Is.EqualTo(300));
                Assert.That(loaded.ThermalData.SelectedPipe, Is.Not.Null);
                Assert.That(loaded.ThermalData.SelectedPipe!.Name, Is.EqualTo("RAUTHERM S 25x2,3"));
                Assert.That(loaded.ConstructionData.R1, Is.EqualTo(0.1));
                Assert.That(loaded.ConstructionData.R2, Is.EqualTo(0.2));
                Assert.That(loaded.HydraulicsData.Collectors[0].Circuits[0].PipeSpacingCm, Is.EqualTo(30.0));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Test]
        public async Task Load_MissingPipeSpacing_FallsBackToDefault()
        {
            var originalPath = Path.GetFullPath(FixturePath);
            var json = await File.ReadAllTextAsync(originalPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                foreach (var property in root.EnumerateObject())
                {
                    if (property.NameEquals("thermalData"))
                    {
                        writer.WritePropertyName(property.Name);
                        writer.WriteStartObject();
                        foreach (var thermalProp in property.Value.EnumerateObject())
                        {
                            if (!thermalProp.NameEquals("pipeSpacing"))
                            {
                                thermalProp.WriteTo(writer);
                            }
                        }
                        writer.WriteEndObject();
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }
                writer.WriteEndObject();
            }

            var tempPath = Path.Combine(Path.GetTempPath(), $"t19-missing-{Guid.NewGuid()}.smc");
            await File.WriteAllBytesAsync(tempPath, stream.ToArray());

            try
            {
                var loaded = await _service.LoadProjectAsync(tempPath);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.ThermalData.PipeSpacing, Is.EqualTo(200));
                Assert.That(loaded.ThermalData.SelectedPipe, Is.Not.Null);
                Assert.That(loaded.ConstructionData.R1, Is.EqualTo(0.0875));
                Assert.That(loaded.HydraulicsData.Collectors[0].Circuits[0].PipeSpacingCm, Is.EqualTo(25.0));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }
    }
}
