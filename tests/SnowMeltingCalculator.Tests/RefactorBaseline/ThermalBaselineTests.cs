using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Thermal;

namespace SnowMeltingCalculator.Tests.RefactorBaseline
{
    [TestFixture]
    public class ThermalBaselineTests
    {
        private static readonly string BaselinePath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "baseline_refactor_dedupe.json"));

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new RoundTripDoubleConverter() }
        };

        private static readonly IReadOnlyList<CityInput> Cities = new List<CityInput>
        {
            new("Moscow", -28.0, 5.0, 70.0, 0.0),
            new("Sochi", -3.0, 6.0, 80.0, 0.0),
            new("Norilsk", -41.0, 7.0, 75.0, 0.0)
        };

        private static readonly IReadOnlyList<OperatingMode> Modes = new List<OperatingMode>
        {
            OperatingMode.AntiIcing,
            OperatingMode.Melting,
            OperatingMode.Intensive
        };

        private static readonly IReadOnlyList<int> PipeIndices = new List<int> { 0, 1, 2 };

        private static ThermalInputs BuildParameters(CityInput city, OperatingMode mode, int pipeIndex)
        {
            return new ThermalInputs
            {
                Mode = mode,
                SupplyTemperature = 50.0,
                GroundTemperature = 10.0,
                Pipe = PipeType.StandardPipes[pipeIndex],
                PipeSpacing = 200.0,
                LambdaE = 1.6,
                CoolantDensity = 1053.0,
                CoolantHeatCapacity = 3.39
            };
        }

        private static IClimateData BuildClimateData(CityInput city)
        {
            return new ClimateData
            {
                AirTemperature = city.AirTemperature,
                WindSpeed = city.WindSpeed,
                SnowfallIntensity = city.SnowfallIntensity
            };
        }

        private static IConstructionData BuildConstructionData(int pipeIndex)
        {
            return new ConstructionData
            {
                R1Total = 0.05,
                R2Total = 0.1,
                LambdaE = 1.6
            };
        }

        private static IEnumerable<ThermalCaseDto> GenerateCases()
        {
            var calculator = new ThermalCalculator();
            foreach (var city in Cities)
            {
                foreach (var mode in Modes)
                {
                    foreach (var pipeIndex in PipeIndices)
                    {
                        var parameters = BuildParameters(city, mode, pipeIndex);
                        var climate = BuildClimateData(city);
                        var construction = BuildConstructionData(pipeIndex);
                        var result = calculator.Calculate(parameters, climate, construction);

                        yield return new ThermalCaseDto
                        {
                            City = city.Name,
                            Mode = mode.ToString(),
                            PipeIndex = pipeIndex,
                            Alpha = result.Alpha,
                            PowerUp = result.PowerUp,
                            PowerDown = result.PowerDown,
                            PowerTotal = result.PowerTotal,
                            ExcessTemperature = result.ExcessTemperature,
                            MeanTemperature = result.MeanTemperature,
                            SupplyTemperature = result.SupplyTemperature,
                            ReturnTemperature = result.ReturnTemperature,
                            DeltaT = result.DeltaT,
                            MassFlowRate = result.MassFlowRate,
                            VolumeFlowRate = result.VolumeFlowRate
                        };
                    }
                }
            }
        }

        [Test, Explicit]
        public void RegenerateBaseline()
        {
            var dto = new BaselineDto
            {
                Thermal = GenerateCases().ToList(),
                Circuits = new List<object>()
            };

            var json = JsonSerializer.Serialize(dto, JsonOptions);
            File.WriteAllText(BaselinePath, json);

            Console.WriteLine($"Baseline written to: {BaselinePath}");
            Console.WriteLine($"Thermal cases: {dto.Thermal.Count}");
        }

        public static IEnumerable<TestCaseData> ThermalCases()
        {
            var json = File.ReadAllText(BaselinePath);
            var dto = JsonSerializer.Deserialize<BaselineDto>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize baseline");

            foreach (var item in dto.Thermal)
            {
                yield return new TestCaseData(item)
                    .SetName($"{item.City}_{item.Mode}_Pipe{item.PipeIndex}");
            }
        }

        [TestCaseSource(nameof(ThermalCases))]
        public void ThermalOutput_MatchesBaseline(ThermalCaseDto expected)
        {
            var mode = Enum.Parse<OperatingMode>(expected.Mode);
            var city = Cities.Single(c => c.Name == expected.City);
            var parameters = BuildParameters(city, mode, expected.PipeIndex);
            var climate = BuildClimateData(city);
            var construction = BuildConstructionData(expected.PipeIndex);
            var result = new ThermalCalculator().Calculate(parameters, climate, construction);

            Assert.That(result.Alpha, Is.EqualTo(expected.Alpha), nameof(result.Alpha));
            Assert.That(result.PowerUp, Is.EqualTo(expected.PowerUp), nameof(result.PowerUp));
            Assert.That(result.PowerDown, Is.EqualTo(expected.PowerDown), nameof(result.PowerDown));
            Assert.That(result.PowerTotal, Is.EqualTo(expected.PowerTotal), nameof(result.PowerTotal));
            Assert.That(result.ExcessTemperature, Is.EqualTo(expected.ExcessTemperature), nameof(result.ExcessTemperature));
            Assert.That(result.MeanTemperature, Is.EqualTo(expected.MeanTemperature), nameof(result.MeanTemperature));
            Assert.That(result.SupplyTemperature, Is.EqualTo(expected.SupplyTemperature), nameof(result.SupplyTemperature));
            Assert.That(result.ReturnTemperature, Is.EqualTo(expected.ReturnTemperature), nameof(result.ReturnTemperature));
            Assert.That(result.DeltaT, Is.EqualTo(expected.DeltaT), nameof(result.DeltaT));
            Assert.That(result.MassFlowRate, Is.EqualTo(expected.MassFlowRate), nameof(result.MassFlowRate));
            Assert.That(result.VolumeFlowRate, Is.EqualTo(expected.VolumeFlowRate), nameof(result.VolumeFlowRate));
        }

        private sealed record CityInput(
            string Name,
            double AirTemperature,
            double WindSpeed,
            double Humidity,
            double SnowfallIntensity);

        public class BaselineDto
        {
            public List<ThermalCaseDto> Thermal { get; set; } = new();
            public List<object> Circuits { get; set; } = new();
        }

        public class ThermalCaseDto
        {
            public string City { get; set; } = string.Empty;
            public string Mode { get; set; } = string.Empty;
            public int PipeIndex { get; set; }
            public double Alpha { get; set; }
            public double PowerUp { get; set; }
            public double PowerDown { get; set; }
            public double PowerTotal { get; set; }
            public double ExcessTemperature { get; set; }
            public double MeanTemperature { get; set; }
            public double SupplyTemperature { get; set; }
            public double ReturnTemperature { get; set; }
            public double DeltaT { get; set; }
            public double MassFlowRate { get; set; }
            public double VolumeFlowRate { get; set; }
        }

        private sealed class RoundTripDoubleConverter : JsonConverter<double>
        {
            public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.GetDouble();
            }

            public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
            {
                writer.WriteRawValue(value.ToString("R", CultureInfo.InvariantCulture));
            }
        }
    }
}
