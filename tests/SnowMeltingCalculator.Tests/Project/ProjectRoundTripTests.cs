using System.IO;
using System.Text.Json;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Hydraulics;
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
        public async Task SaveThenLoad_ClimateFields_RoundTrip()
        {
            var data = new ProjectData
            {
                Version = "1.1",
                ProjectNumber = "CLM-RT-001",
                ProjectObject = "climate round-trip",
                ClimateData = new ClimateProjectData
                {
                    SelectedCity = "Москва",
                    Region = "Московская область",
                    AirTemperature = -18.0,
                    WindSpeed = 3.5,
                    Humidity = 65.0,
                    SnowfallIntensity = 2.5,
                    SelectedZone = ClimateZone.Zone_M15,
                    IsHighRequirements = false
                }
            };

            var tempPath = Path.Combine(Path.GetTempPath(), $"clm-rt-{Guid.NewGuid()}.smc");
            try
            {
                var saved = await _service.SaveProjectAsync(tempPath, data);
                Assert.That(saved, Is.True);

                var loaded = await _service.LoadProjectAsync(tempPath);
                Assert.That(loaded, Is.Not.Null);

                var climate = loaded!.ClimateData;
                Assert.That(climate.SelectedCity, Is.EqualTo("Москва"));
                Assert.That(climate.Region, Is.EqualTo("Московская область"));
                Assert.That(climate.AirTemperature, Is.EqualTo(-18.0));
                Assert.That(climate.WindSpeed, Is.EqualTo(3.5));
                Assert.That(climate.Humidity, Is.EqualTo(65.0));
                Assert.That(climate.SnowfallIntensity, Is.EqualTo(2.5));
                Assert.That(climate.SelectedZone, Is.EqualTo(ClimateZone.Zone_M15));
                Assert.That(climate.IsHighRequirements, Is.False);
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

        [Test]
        public async Task ProjectRoundTrip_FlowRegimeRestored()
        {
            var data = new ProjectData
            {
                Version = "1.0",
                HydraulicsData = new HydraulicsProjectData
                {
                    Collectors = new List<CollectorProjectData>
                    {
                        new CollectorProjectData
                        {
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData
                                {
                                    OperatingResult = new CircuitResultProjectData
                                    {
                                        FlowRegimeString = FlowRegime.Turbulent.ToString()
                                    },
                                    DesignResult = new CircuitResultProjectData
                                    {
                                        FlowRegimeString = FlowRegime.Laminar.ToString()
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var tempPath = Path.Combine(Path.GetTempPath(), $"t5-flowregime-{Guid.NewGuid()}.smc");
            try
            {
                var saved = await _service.SaveProjectAsync(tempPath, data);
                Assert.That(saved, Is.True);

                var loaded = await _service.LoadProjectAsync(tempPath);
                Assert.That(loaded, Is.Not.Null);

                var circuit = loaded!.HydraulicsData.Collectors[0].Circuits[0];
                Assert.That(circuit.OperatingResult, Is.Not.Null);
                Assert.That(circuit.DesignResult, Is.Not.Null);
                Assert.That(Enum.Parse<FlowRegime>(circuit.OperatingResult!.FlowRegimeString), Is.EqualTo(FlowRegime.Turbulent));
                Assert.That(Enum.Parse<FlowRegime>(circuit.DesignResult!.FlowRegimeString), Is.EqualTo(FlowRegime.Laminar));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Test]
        public async Task ProjectRoundTrip_PipeSpacingPerCircuitPreserved()
        {
            var data = new ProjectData
            {
                Version = "1.0",
                ThermalData = new ThermalProjectData
                {
                    PipeSpacing = 250
                },
                HydraulicsData = new HydraulicsProjectData
                {
                    Collectors = new List<CollectorProjectData>
                    {
                        new CollectorProjectData
                        {
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData { PipeSpacingCm = 20.0 },
                                new CircuitProjectData { PipeSpacingCm = 30.0 }
                            }
                        }
                    }
                }
            };

            var tempPath = Path.Combine(Path.GetTempPath(), $"t7-pipespacing-{Guid.NewGuid()}.smc");
            try
            {
                var saved = await _service.SaveProjectAsync(tempPath, data);
                Assert.That(saved, Is.True);

                var loaded = await _service.LoadProjectAsync(tempPath);
                Assert.That(loaded, Is.Not.Null);

                var circuits = loaded!.HydraulicsData.Collectors[0].Circuits;
                Assert.That(circuits, Has.Count.EqualTo(2));
                Assert.That(circuits[0].PipeSpacingCm, Is.EqualTo(20.0),
                    "First per-circuit PipeSpacing must be preserved on load");
                Assert.That(circuits[1].PipeSpacingCm, Is.EqualTo(30.0),
                    "Second per-circuit PipeSpacing must be preserved on load and not reset to global");
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Test]
        public async Task FullProject_RoundTrip_PreservesAllCircuitResultDetails()
        {
            var data = new ProjectData
            {
                Version = "1.0",
                HydraulicsData = new HydraulicsProjectData
                {
                    Collectors = new List<CollectorProjectData>
                    {
                        new CollectorProjectData
                        {
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData
                                {
                                    OperatingResult = new CircuitResultProjectData
                                    {
                                        FlowRegimeString = FlowRegime.Turbulent.ToString(),
                                        Density = 1.053,
                                        KinematicViscosity = 1.234,
                                        ReynoldsNumber = 5678.9,
                                        FrictionFactor = 0.031,
                                        PressureLossPerMeter = 215.5
                                    },
                                    DesignResult = new CircuitResultProjectData
                                    {
                                        FlowRegimeString = FlowRegime.Laminar.ToString(),
                                        Density = 1.071,
                                        KinematicViscosity = 2.345,
                                        ReynoldsNumber = 1234.5,
                                        FrictionFactor = 0.052,
                                        PressureLossPerMeter = 312.0
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var tempPath = Path.Combine(Path.GetTempPath(), $"t8-details-{Guid.NewGuid()}.smc");
            try
            {
                var saved = await _service.SaveProjectAsync(tempPath, data);
                Assert.That(saved, Is.True);

                var loaded = await _service.LoadProjectAsync(tempPath);
                Assert.That(loaded, Is.Not.Null);

                var circuit = loaded!.HydraulicsData.Collectors[0].Circuits[0];
                Assert.That(circuit.OperatingResult, Is.Not.Null);
                Assert.That(circuit.DesignResult, Is.Not.Null);

                AssertDetailFieldsEqual(data.HydraulicsData.Collectors[0].Circuits[0].OperatingResult!, circuit.OperatingResult!);
                AssertDetailFieldsEqual(data.HydraulicsData.Collectors[0].Circuits[0].DesignResult!, circuit.DesignResult!);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Test]
        public async Task FullProject_RoundTrip_BackwardCompatible_OldFileLoadsWithDefaults()
        {
            var data = new ProjectData
            {
                Version = "1.0",
                HydraulicsData = new HydraulicsProjectData
                {
                    Collectors = new List<CollectorProjectData>
                    {
                        new CollectorProjectData
                        {
                            Circuits = new List<CircuitProjectData>
                            {
                                new CircuitProjectData
                                {
                                    OperatingResult = new CircuitResultProjectData
                                    {
                                        FlowRegimeString = FlowRegime.Transitional.ToString()
                                    },
                                    DesignResult = new CircuitResultProjectData
                                    {
                                        FlowRegimeString = FlowRegime.Laminar.ToString()
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var tempPath = Path.Combine(Path.GetTempPath(), $"t8-compat-{Guid.NewGuid()}.smc");
            try
            {
                var saved = await _service.SaveProjectAsync(tempPath, data);
                Assert.That(saved, Is.True);

                ProjectData loaded = null!;
                Assert.DoesNotThrowAsync(async () => loaded = await _service.LoadProjectAsync(tempPath));
                Assert.That(loaded, Is.Not.Null);

                var circuit = loaded.HydraulicsData.Collectors[0].Circuits[0];
                Assert.That(circuit.OperatingResult, Is.Not.Null);
                Assert.That(circuit.DesignResult, Is.Not.Null);

                Assert.That(circuit.OperatingResult!.Density, Is.EqualTo(0.0));
                Assert.That(circuit.OperatingResult.KinematicViscosity, Is.EqualTo(0.0));
                Assert.That(circuit.OperatingResult.ReynoldsNumber, Is.EqualTo(0.0));
                Assert.That(circuit.OperatingResult.FrictionFactor, Is.EqualTo(0.0));
                Assert.That(circuit.OperatingResult.PressureLossPerMeter, Is.EqualTo(0.0));

                Assert.That(circuit.DesignResult!.Density, Is.EqualTo(0.0));
                Assert.That(circuit.DesignResult.KinematicViscosity, Is.EqualTo(0.0));
                Assert.That(circuit.DesignResult.ReynoldsNumber, Is.EqualTo(0.0));
                Assert.That(circuit.DesignResult.FrictionFactor, Is.EqualTo(0.0));
                Assert.That(circuit.DesignResult.PressureLossPerMeter, Is.EqualTo(0.0));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Test]
        public async Task ProjectRoundTrip_TwoCollectors_PreservesPerCollectorSummaries()
        {
            // Two collectors share the common summary fields (CircuitCount, Kv, CollectorType, ValveType)
            // but differ in numeric summary values (length/power/flow/pressure). If the save/load pipeline
            // collapses summaries to a single shared instance, swaps the two collectors, or drops the
            // per-collector Summary, the assertions below will fail.
            var data = new ProjectData
            {
                Version = "1.0",
                HydraulicsData = new HydraulicsProjectData
                {
                    Collectors = new List<CollectorProjectData>
                    {
                        new CollectorProjectData
                        {
                            CollectorNumber = 1,
                            CollectorType = "HKV-D",
                            ValveType = ValveType.HKV_D,
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 4,
                                TotalPipeLength = 435,
                                TotalPower = 22700,
                                TotalFlowRate = 1187.93,
                                PressureLoss_Operating_Pa = 36914.65,
                                PressureLoss_Cold_Pa = 125000,
                                Kv = 1.2,
                                CollectorType = "HKV-D"
                            }
                        },
                        new CollectorProjectData
                        {
                            CollectorNumber = 2,
                            CollectorType = "HKV-D",
                            ValveType = ValveType.HKV_D,
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 4,
                                TotalPipeLength = 400,
                                TotalPower = 20700,
                                TotalFlowRate = 1082.93,
                                PressureLoss_Operating_Pa = 29159.16,
                                PressureLoss_Cold_Pa = 104100,
                                Kv = 1.2,
                                CollectorType = "HKV-D"
                            }
                        }
                    }
                }
            };

            var tempPath = Path.Combine(Path.GetTempPath(), $"t10-twocollectors-{Guid.NewGuid()}.smc");
            try
            {
                var saved = await _service.SaveProjectAsync(tempPath, data);
                Assert.That(saved, Is.True);

                var loaded = await _service.LoadProjectAsync(tempPath);
                Assert.That(loaded, Is.Not.Null);

                var collectors = loaded!.HydraulicsData.Collectors;
                Assert.That(collectors, Has.Count.EqualTo(2),
                    "Both collectors must round-trip through save/load");

                // Key collectors by TotalPower so the test catches both
                // "summaries collapsed onto one collector" and "summaries swapped between collectors".
                var collectorA = collectors.FirstOrDefault(c => c.Summary?.TotalPower == 22700);
                var collectorB = collectors.FirstOrDefault(c => c.Summary?.TotalPower == 20700);

                Assert.That(collectorA, Is.Not.Null,
                    "Collector A (TotalPower=22700) must be present in the loaded project");
                Assert.That(collectorB, Is.Not.Null,
                    "Collector B (TotalPower=20700) must be present in the loaded project");

                Assert.That(collectorA!.Summary, Is.Not.Null,
                    "Collector A summary must be preserved (not collapsed to null)");
                Assert.That(collectorB!.Summary, Is.Not.Null,
                    "Collector B summary must be preserved (not collapsed to null)");

                // Collector-level fields
                Assert.That(collectorA.CollectorNumber, Is.EqualTo(1));
                Assert.That(collectorA.CollectorType, Is.EqualTo("HKV-D"));
                Assert.That(collectorA.ValveType, Is.EqualTo(ValveType.HKV_D));
                Assert.That(collectorB.CollectorNumber, Is.EqualTo(2));
                Assert.That(collectorB.CollectorType, Is.EqualTo("HKV-D"));
                Assert.That(collectorB.ValveType, Is.EqualTo(ValveType.HKV_D));

                // Collector A summary: full field check
                Assert.That(collectorA.Summary!.CircuitCount, Is.EqualTo(4));
                Assert.That(collectorA.Summary.TotalPipeLength, Is.EqualTo(435));
                Assert.That(collectorA.Summary.TotalPower, Is.EqualTo(22700));
                Assert.That(collectorA.Summary.TotalFlowRate, Is.EqualTo(1187.93).Within(0.001));
                Assert.That(collectorA.Summary.PressureLoss_Operating_Pa, Is.EqualTo(36914.65).Within(0.01));
                Assert.That(collectorA.Summary.PressureLoss_Cold_Pa, Is.EqualTo(125000).Within(0.01));
                Assert.That(collectorA.Summary.Kv, Is.EqualTo(1.2).Within(0.001));
                Assert.That(collectorA.Summary.CollectorType, Is.EqualTo("HKV-D"));

                // Collector B summary: full field check
                Assert.That(collectorB.Summary!.CircuitCount, Is.EqualTo(4));
                Assert.That(collectorB.Summary.TotalPipeLength, Is.EqualTo(400));
                Assert.That(collectorB.Summary.TotalPower, Is.EqualTo(20700));
                Assert.That(collectorB.Summary.TotalFlowRate, Is.EqualTo(1082.93).Within(0.001));
                Assert.That(collectorB.Summary.PressureLoss_Operating_Pa, Is.EqualTo(29159.16).Within(0.01));
                Assert.That(collectorB.Summary.PressureLoss_Cold_Pa, Is.EqualTo(104100).Within(0.01));
                Assert.That(collectorB.Summary.Kv, Is.EqualTo(1.2).Within(0.001));
                Assert.That(collectorB.Summary.CollectorType, Is.EqualTo("HKV-D"));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        private static void AssertDetailFieldsEqual(CircuitResultProjectData expected, CircuitResultProjectData actual)
        {
            Assert.That(actual.FlowRegimeString, Is.EqualTo(expected.FlowRegimeString));
            Assert.That(actual.Density, Is.EqualTo(expected.Density));
            Assert.That(actual.KinematicViscosity, Is.EqualTo(expected.KinematicViscosity));
            Assert.That(actual.ReynoldsNumber, Is.EqualTo(expected.ReynoldsNumber));
            Assert.That(actual.FrictionFactor, Is.EqualTo(expected.FrictionFactor));
            Assert.That(actual.PressureLossPerMeter, Is.EqualTo(expected.PressureLossPerMeter));
        }
    }
}
