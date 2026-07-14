using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;

namespace SnowMeltingCalculator.Tests.RefactorBaseline
{
    [TestFixture]
    public class CircuitsBaselineTests
    {
        private static readonly string BaselinePath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "baseline_refactor_dedupe.json"));

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new RoundTripDoubleConverter(), new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        private static readonly IReadOnlyList<double> CircuitLengths = new List<double> { 80.0, 120.0 };
        private static readonly IReadOnlyList<double> SupplyLengths = new List<double> { 8.0, 12.0 };
        private static readonly IReadOnlyList<ValveType> ValveTypes = new List<ValveType>
        {
            ValveType.HKV_D,
            ValveType.IV_1_25
        };
        private static readonly IReadOnlyList<double> GlycolConcentrations = new List<double> { 30.0, 50.0 };

        private const double PowerUp = 256.0;
        private const double PowerDown = 5.0;
        private const double SupplyTemperature = 50.0;
        private const double ReturnTemperature = 30.0;
        private const double ColdFiveDayTemperature = -20.0;
        private const double InnerDiameter = 16.0;
        private const double PipeSpacing_cm = 20.0;

        private static HydraulicInputData BuildInputData(double glycolConcentration, ValveType valveType)
        {
            return new HydraulicInputData
            {
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = glycolConcentration,
                SupplySpacing_cm = 5.0,
                SupplyHeatPercent = 10.0,
                ValveType = valveType
            };
        }

        private static CircuitRow BuildCircuit(double circuitLength, double supplyLength)
        {
            return new CircuitRow
            {
                CircuitNumber = 1,
                CircuitLength = circuitLength,
                SupplyLength = supplyLength,
                SupplySpacing_cm = 5.0,
                SupplyHeatPercent = 10.0
            };
        }

        private static IEnumerable<CircuitCaseDto> GenerateCases()
        {
            var glycolService = new GlycolDataService();
            var calculator = new CircuitsCalculator(glycolService);
            double operatingTemperature = (SupplyTemperature + ReturnTemperature) / 2.0;
            double deltaT = SupplyTemperature - ReturnTemperature;

            foreach (var circuitLength in CircuitLengths)
            {
                foreach (var supplyLength in SupplyLengths)
                {
                    foreach (var valveType in ValveTypes)
                    {
                        foreach (var glycolConcentration in GlycolConcentrations)
                        {
                            var inputData = BuildInputData(glycolConcentration, valveType);
                            var circuit = BuildCircuit(circuitLength, supplyLength);
                            var circuits = new List<CircuitRow> { circuit };

                            calculator.CalculateAllCircuits(circuits, inputData, PipeSpacing_cm, PowerUp, PowerDown, operatingTemperature, ColdFiveDayTemperature, deltaT, InnerDiameter);

                            yield return new CircuitCaseDto
                            {
                                CircuitLength = circuitLength,
                                SupplyLength = supplyLength,
                                ValveType = valveType.ToString(),
                                GlycolConcentration = glycolConcentration,
                                PipeSpacing_cm = PipeSpacing_cm,
                                PowerUp = PowerUp,
                                PowerDown = PowerDown,
                                SupplyTemperature = SupplyTemperature,
                                ReturnTemperature = ReturnTemperature,
                                ColdFiveDayTemperature = ColdFiveDayTemperature,
                                InnerDiameter = InnerDiameter,
                                GlycolType = inputData.GlycolType.ToString(),
                                Power = circuit.Power,
                                FlowRate = circuit.FlowRate,
                                Velocity = circuit.Velocity,
                                OperatingResult = MapResult(circuit.OperatingResult),
                                DesignResult = MapResult(circuit.DesignResult)
                            };
                        }
                    }
                }
            }
        }

        private static CircuitTemperatureResultDto MapResult(CircuitTemperatureResult result)
        {
            return new CircuitTemperatureResultDto
            {
                Temperature = result.Temperature,
                Density = result.Density,
                KinematicViscosity = result.KinematicViscosity,
                ReynoldsNumber = result.ReynoldsNumber,
                FlowRegime = result.FlowRegime,
                FrictionFactor = result.FrictionFactor,
                PressureLossPerMeter = result.PressureLossPerMeter,
                DpRohr = result.DpRohr,
                DpVerteiler = result.DpVerteiler,
                DpVent = result.DpVent,
                DpGesamt = result.DpGesamt,
                ZuDrosseln = result.ZuDrosseln
            };
        }

        [Test, Explicit]
        public void RegenerateCircuitsBaseline()
        {
            var json = File.ReadAllText(BaselinePath);
            var dto = JsonSerializer.Deserialize<BaselineDto>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize baseline");

            dto.Circuits = GenerateCases().ToList();

            var updatedJson = JsonSerializer.Serialize(dto, JsonOptions);
            File.WriteAllText(BaselinePath, updatedJson);

            Console.WriteLine($"Baseline written to: {BaselinePath}");
            Console.WriteLine($"Thermal cases preserved: {dto.Thermal.Count}");
            Console.WriteLine($"Circuits cases: {dto.Circuits.Count}");
        }

        public static IEnumerable<TestCaseData> CircuitsCases()
        {
            var json = File.ReadAllText(BaselinePath);
            var dto = JsonSerializer.Deserialize<BaselineDto>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize baseline");

            foreach (var item in dto.Circuits)
            {
                yield return new TestCaseData(item)
                    .SetName($"L{item.CircuitLength}_S{item.SupplyLength}_{item.ValveType}_Glycol{item.GlycolConcentration}");
            }
        }

        [TestCaseSource(nameof(CircuitsCases))]
        public void CircuitsOutput_MatchesBaseline(CircuitCaseDto expected)
        {
            var valveType = Enum.Parse<ValveType>(expected.ValveType);
            var inputData = BuildInputData(expected.GlycolConcentration, valveType);
            var circuit = BuildCircuit(expected.CircuitLength, expected.SupplyLength);
            var circuits = new List<CircuitRow> { circuit };

            double operatingTemperature = (expected.SupplyTemperature + expected.ReturnTemperature) / 2.0;
            double deltaT = expected.SupplyTemperature - expected.ReturnTemperature;

            var calculator = new CircuitsCalculator(new GlycolDataService());
            calculator.CalculateAllCircuits(circuits, inputData, expected.PipeSpacing_cm, expected.PowerUp, expected.PowerDown, operatingTemperature, expected.ColdFiveDayTemperature, deltaT, expected.InnerDiameter);

            Assert.That(circuit.Power, Is.EqualTo(expected.Power), nameof(circuit.Power));
            Assert.That(circuit.FlowRate, Is.EqualTo(expected.FlowRate), nameof(circuit.FlowRate));
            Assert.That(circuit.Velocity, Is.EqualTo(expected.Velocity), nameof(circuit.Velocity));
            AssertResult(circuit.OperatingResult, expected.OperatingResult, "Operating");
            AssertResult(circuit.DesignResult, expected.DesignResult, "Design");
        }

        private static void AssertResult(CircuitTemperatureResult actual, CircuitTemperatureResultDto expected, string prefix)
        {
            Assert.That(actual.Temperature, Is.EqualTo(expected.Temperature), $"{prefix}.{nameof(actual.Temperature)}");
            Assert.That(actual.Density, Is.EqualTo(expected.Density), $"{prefix}.{nameof(actual.Density)}");
            Assert.That(actual.KinematicViscosity, Is.EqualTo(expected.KinematicViscosity), $"{prefix}.{nameof(actual.KinematicViscosity)}");
            Assert.That(actual.ReynoldsNumber, Is.EqualTo(expected.ReynoldsNumber), $"{prefix}.{nameof(actual.ReynoldsNumber)}");
            Assert.That(actual.FlowRegime, Is.EqualTo(expected.FlowRegime), $"{prefix}.{nameof(actual.FlowRegime)}");
            Assert.That(actual.FrictionFactor, Is.EqualTo(expected.FrictionFactor), $"{prefix}.{nameof(actual.FrictionFactor)}");
            Assert.That(actual.PressureLossPerMeter, Is.EqualTo(expected.PressureLossPerMeter), $"{prefix}.{nameof(actual.PressureLossPerMeter)}");
            Assert.That(actual.DpRohr, Is.EqualTo(expected.DpRohr), $"{prefix}.{nameof(actual.DpRohr)}");
            Assert.That(actual.DpVerteiler, Is.EqualTo(expected.DpVerteiler), $"{prefix}.{nameof(actual.DpVerteiler)}");
            Assert.That(actual.DpVent, Is.EqualTo(expected.DpVent), $"{prefix}.{nameof(actual.DpVent)}");
            Assert.That(actual.DpGesamt, Is.EqualTo(expected.DpGesamt), $"{prefix}.{nameof(actual.DpGesamt)}");
            Assert.That(actual.ZuDrosseln, Is.EqualTo(expected.ZuDrosseln), $"{prefix}.{nameof(actual.ZuDrosseln)}");
        }

        public class BaselineDto
        {
            public List<ThermalBaselineTests.ThermalCaseDto> Thermal { get; set; } = new();
            public List<CircuitCaseDto> Circuits { get; set; } = new();
        }

        public class CircuitCaseDto
        {
            public double CircuitLength { get; set; }
            public double SupplyLength { get; set; }
            public string ValveType { get; set; } = string.Empty;
            public double GlycolConcentration { get; set; }
            public double PipeSpacing_cm { get; set; }
            public string GlycolType { get; set; } = string.Empty;
            public double PowerUp { get; set; }
            public double PowerDown { get; set; }
            public double SupplyTemperature { get; set; }
            public double ReturnTemperature { get; set; }
            public double ColdFiveDayTemperature { get; set; }
            public double InnerDiameter { get; set; }
            public double Power { get; set; }
            public double FlowRate { get; set; }
            public double Velocity { get; set; }
            public CircuitTemperatureResultDto OperatingResult { get; set; } = new();
            public CircuitTemperatureResultDto DesignResult { get; set; } = new();
        }

        public class CircuitTemperatureResultDto
        {
            public double Temperature { get; set; }
            public double Density { get; set; }
            public double KinematicViscosity { get; set; }
            public double ReynoldsNumber { get; set; }
            public FlowRegime FlowRegime { get; set; }
            public double FrictionFactor { get; set; }
            public double PressureLossPerMeter { get; set; }
            public double DpRohr { get; set; }
            public double DpVerteiler { get; set; }
            public double DpVent { get; set; }
            public double DpGesamt { get; set; }
            public double ZuDrosseln { get; set; }
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
