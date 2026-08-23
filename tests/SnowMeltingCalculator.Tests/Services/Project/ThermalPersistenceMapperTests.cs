using System;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Todo 9 (DEC-T08): restore-половина чистого маппера persistence DTO →
    /// канонический тепловой кандидат. Todo 10 добавляет save-половину
    /// (Snapshot → DTO) и proof точного wire-контракта сериализации.
    /// </summary>
    [TestFixture]
    public sealed class ThermalPersistenceMapperTests
    {
        private static readonly IReadOnlyList<PipeType> StandardPipes = PipeType.StandardPipes;

        [Test]
        public void BuildInputsCandidate_PersistedPipeMatchesStandard_UsesMatchingStandardDefinition()
        {
            var data = new ThermalProjectData
            {
                SelectedMode = OperatingMode.Intensive,
                SupplyTemperature = 55.0,
                GroundTemperature = 8.0,
                PipeSpacing = 250,
                SelectedPipe = new PipeTypeProjectData
                {
                    Name = "RAUTHERM S 20x2,0",
                    OuterDiameter = 20,
                    InnerDiameter = 16,
                    WallThickness = 2.0
                }
            };

            var candidate = ThermalPersistenceMapper.BuildInputsCandidate(data, StandardPipes);

            Assert.Multiple(() =>
            {
                Assert.That(candidate.Mode, Is.EqualTo(OperatingMode.Intensive));
                Assert.That(candidate.SupplyTemperature, Is.EqualTo(55.0));
                Assert.That(candidate.GroundTemperature, Is.EqualTo(8.0));
                Assert.That(candidate.PipeSpacing, Is.EqualTo(250));
                Assert.That(candidate.Pipe, Is.Not.Null);
                // Каноническая труба берётся из стандартного определения: Article и
                // ThermalConductivity приходят из каталога, а не из wire-DTO.
                var standard = StandardPipes.Single(p => p.Name == "RAUTHERM S 20x2,0");
                Assert.That(candidate.Pipe!.Article, Is.EqualTo(standard.Article));
                Assert.That(candidate.Pipe.ThermalConductivity, Is.EqualTo(standard.ThermalConductivity));
                Assert.That(candidate.Pipe.OuterDiameter, Is.EqualTo(20));
                Assert.That(candidate.Pipe.InnerDiameter, Is.EqualTo(16));
                Assert.That(candidate.Pipe.WallThickness, Is.EqualTo(2.0));
            });
        }

        [Test]
        public void BuildInputsCandidate_PersistedPipeUnknown_FallsBackToFirstStandardPipe()
        {
            var data = new ThermalProjectData
            {
                SelectedMode = OperatingMode.Melting,
                SupplyTemperature = 50.0,
                GroundTemperature = 10.0,
                PipeSpacing = 200,
                SelectedPipe = new PipeTypeProjectData
                {
                    Name = "UNKNOWN PIPE 99x9,9",
                    OuterDiameter = 99,
                    InnerDiameter = 90,
                    WallThickness = 9.9
                }
            };

            var candidate = ThermalPersistenceMapper.BuildInputsCandidate(data, StandardPipes);

            var firstStandard = ThermalPipeSnapshot.FromPipeType(StandardPipes[0]);
            Assert.That(candidate.Pipe, Is.EqualTo(firstStandard),
                "Unknown persisted pipe must fall back to the first available standard pipe.");
        }

        [Test]
        public void BuildInputsCandidate_PersistedPipeNull_PipeRemainsNull()
        {
            var data = new ThermalProjectData
            {
                SelectedMode = OperatingMode.AntiIcing,
                SupplyTemperature = 40.0,
                GroundTemperature = 5.0,
                PipeSpacing = 150,
                SelectedPipe = null
            };

            var candidate = ThermalPersistenceMapper.BuildInputsCandidate(data, StandardPipes);

            Assert.That(candidate.Pipe, Is.Null,
                "Persisted null pipe must remain null after lifecycle restore (DEC-T08).");
        }

        [Test]
        public void BuildInputsCandidate_MissingLegacySpacing_DefaultsTo200()
        {
            // Свежий DTO без явного PipeSpacing эмулирует legacy-файл без поля:
            // JSON-десериализация оставляет инициализатор 200.
            var data = new ThermalProjectData
            {
                SelectedMode = OperatingMode.Melting,
                SupplyTemperature = 50.0,
                GroundTemperature = 10.0
            };
            Assert.That(data.PipeSpacing, Is.EqualTo(200), "DTO initializer contract.");

            var candidate = ThermalPersistenceMapper.BuildInputsCandidate(data, StandardPipes);

            Assert.That(candidate.PipeSpacing, Is.EqualTo(200),
                "Missing legacy spacing must restore as 200 (DEC-T08).");
        }

        [Test]
        public void BuildInputsCandidate_NullThermalData_ReturnsExactDefaults()
        {
            var candidate = ThermalPersistenceMapper.BuildInputsCandidate(null, StandardPipes);

            Assert.That(candidate, Is.EqualTo(ThermalInputsSnapshot.Default));
        }

        [Test]
        public void BuildSavedResult_ValidResult_MapsExactlyEightWireFields()
        {
            var result = new ThermalResultProjectData
            {
                PowerUp = 357.5,
                PowerDown = 5.8,
                PowerTotal = 363.3,
                SupplyTemperature = 60.0,
                ReturnTemperature = 44.31,
                MeanTemperature = 52.16,
                DeltaT = 15.69,
                IsValid = true
            };

            var snapshot = ThermalPersistenceMapper.BuildSavedResult(result);

            Assert.Multiple(() =>
            {
                Assert.That(snapshot, Is.Not.Null);
                Assert.That(snapshot!.PowerUp, Is.EqualTo(357.5));
                Assert.That(snapshot.PowerDown, Is.EqualTo(5.8));
                Assert.That(snapshot.PowerTotal, Is.EqualTo(363.3));
                Assert.That(snapshot.SupplyTemperature, Is.EqualTo(60.0));
                Assert.That(snapshot.ReturnTemperature, Is.EqualTo(44.31));
                Assert.That(snapshot.MeanTemperature, Is.EqualTo(52.16));
                Assert.That(snapshot.DeltaT, Is.EqualTo(15.69));
                Assert.That(snapshot.IsValid, Is.True);
                // Runtime-only поля восстанавливаются CLR-дефолтами (DEC-T08).
                Assert.That(snapshot.Alpha, Is.Zero);
                Assert.That(snapshot.MeltingHeat, Is.Zero);
                Assert.That(snapshot.RadiationHeat, Is.Zero);
                Assert.That(snapshot.ConvectionHeat, Is.Zero);
                Assert.That(snapshot.ExcessTemperature, Is.Zero);
                Assert.That(snapshot.RFb, Is.Zero);
                Assert.That(snapshot.RD, Is.Zero);
                Assert.That(snapshot.ParameterM, Is.Zero);
                Assert.That(snapshot.EfficiencyEtaR, Is.Zero);
                Assert.That(snapshot.MassFlowRate, Is.Zero);
                Assert.That(snapshot.VolumeFlowRate, Is.Zero);
                Assert.That(snapshot.ValidationErrors, Is.Empty);
            });
        }

        [Test]
        public void BuildSavedResult_InvalidResult_IsNotCanonical()
        {
            var result = new ThermalResultProjectData { PowerTotal = 999, IsValid = false };

            var snapshot = ThermalPersistenceMapper.BuildSavedResult(result);

            Assert.That(snapshot, Is.Null,
                "Invalid saved result must not become the canonical result (DEC-T08).");
        }

        [Test]
        public void BuildSavedResult_NullResult_ReturnsNull()
        {
            Assert.That(ThermalPersistenceMapper.BuildSavedResult(null), Is.Null);
        }

        [Test]
        public void ToDomainResult_MapsEightWireFieldsOnly()
        {
            var snapshot = ThermalPersistenceMapper.BuildSavedResult(new ThermalResultProjectData
            {
                PowerUp = 1.0,
                PowerDown = 2.0,
                PowerTotal = 3.0,
                SupplyTemperature = 60.0,
                ReturnTemperature = 45.0,
                MeanTemperature = 52.5,
                DeltaT = 15.0,
                IsValid = true
            })!;

            var domain = ThermalPersistenceMapper.ToDomainResult(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(domain.PowerUp, Is.EqualTo(1.0));
                Assert.That(domain.PowerDown, Is.EqualTo(2.0));
                Assert.That(domain.PowerTotal, Is.EqualTo(3.0));
                Assert.That(domain.SupplyTemperature, Is.EqualTo(60.0));
                Assert.That(domain.ReturnTemperature, Is.EqualTo(45.0));
                Assert.That(domain.MeanTemperature, Is.EqualTo(52.5));
                Assert.That(domain.DeltaT, Is.EqualTo(15.0));
                Assert.That(domain.IsValid, Is.True);
            });
        }

        [Test]
        public void ResolveStandardPipe_FromSnapshot_AppliesSameMatchAndFallbackRules()
        {
            var matched = ThermalPipeSnapshot.FromPipeType(StandardPipes[2]);
            var unknown = new ThermalPipeSnapshot("GHOST", "000", 42, 38, 4.0, 0.35);

            Assert.Multiple(() =>
            {
                Assert.That(
                    ThermalPersistenceMapper.ResolveStandardPipe(matched, StandardPipes),
                    Is.SameAs(StandardPipes[2]));
                Assert.That(
                    ThermalPersistenceMapper.ResolveStandardPipe(unknown, StandardPipes),
                    Is.SameAs(StandardPipes[0]));
                Assert.That(
                    ThermalPersistenceMapper.ResolveStandardPipe((ThermalPipeSnapshot?)null, StandardPipes),
                    Is.Null);
            });
        }

        // === Todo 10: save-половина (Snapshot → DTO) и wire-контракт ===

        private static ThermalStateSnapshot CreateFullSnapshot()
        {
            var standard = StandardPipes.Single(p => p.Name == "RAUTHERM S 20x2,0");
            return new ThermalStateSnapshot(
                new ThermalInputsSnapshot(
                    OperatingMode.Intensive,
                    55.0,
                    8.0,
                    ThermalPipeSnapshot.FromPipeType(standard),
                    250),
                new ThermalResultSnapshot(
                    alpha: 0.35,
                    powerUp: 357.5,
                    powerDown: 5.8,
                    powerTotal: 363.3,
                    meltingHeat: 1.1,
                    radiationHeat: 2.2,
                    convectionHeat: 3.3,
                    excessTemperature: 4.4,
                    meanTemperature: 52.16,
                    supplyTemperature: 60.0,
                    returnTemperature: 44.31,
                    deltaT: 15.69,
                    rFb: 5.5,
                    rD: 6.6,
                    parameterM: 7.7,
                    efficiencyEtaR: 8.8,
                    massFlowRate: 9.9,
                    volumeFlowRate: 10.10,
                    isValid: true,
                    validationErrors: null),
                new ThermalStatusSnapshot(
                    ThermalCalculationPhase.NeedsRecalculation,
                    "статус не персистится",
                    "сообщение не персистится"));
        }

        [Test]
        public void BuildThermalProjectData_MapsExactWireFields_FromCanonicalSnapshot()
        {
            var snapshot = CreateFullSnapshot();

            var dto = ThermalPersistenceMapper.BuildThermalProjectData(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(dto.SelectedMode, Is.EqualTo(OperatingMode.Intensive));
                Assert.That(dto.SupplyTemperature, Is.EqualTo(55.0));
                Assert.That(dto.GroundTemperature, Is.EqualTo(8.0));
                Assert.That(dto.PipeSpacing, Is.EqualTo(250));
                Assert.That(dto.SelectedPipe, Is.Not.Null);
                Assert.That(dto.SelectedPipe!.Name, Is.EqualTo("RAUTHERM S 20x2,0"));
                Assert.That(dto.SelectedPipe.OuterDiameter, Is.EqualTo(20.0));
                Assert.That(dto.SelectedPipe.InnerDiameter, Is.EqualTo(16.0));
                Assert.That(dto.SelectedPipe.WallThickness, Is.EqualTo(2.0));
                Assert.That(dto.Result, Is.Not.Null);
                Assert.That(dto.Result!.PowerUp, Is.EqualTo(357.5));
                Assert.That(dto.Result.PowerDown, Is.EqualTo(5.8));
                Assert.That(dto.Result.PowerTotal, Is.EqualTo(363.3));
                Assert.That(dto.Result.SupplyTemperature, Is.EqualTo(60.0));
                Assert.That(dto.Result.ReturnTemperature, Is.EqualTo(44.31));
                Assert.That(dto.Result.MeanTemperature, Is.EqualTo(52.16));
                Assert.That(dto.Result.DeltaT, Is.EqualTo(15.69));
                Assert.That(dto.Result.IsValid, Is.True);
            });
        }

        [Test]
        public void BuildThermalProjectData_NullResultAndNullPipe_StaysNullInDto()
        {
            var snapshot = new ThermalStateSnapshot(
                new ThermalInputsSnapshot(
                    OperatingMode.Melting, 50.0, 10.0, pipe: null, pipeSpacing: 200),
                result: null,
                ThermalStatusSnapshot.Default);

            var dto = ThermalPersistenceMapper.BuildThermalProjectData(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(dto.SelectedPipe, Is.Null);
                Assert.That(dto.Result, Is.Null);
                Assert.That(dto.PipeSpacing, Is.EqualTo(200));
                Assert.That(dto.SelectedMode, Is.EqualTo(OperatingMode.Melting));
            });
        }

        [Test]
        public void BuildThermalProjectData_NullSnapshot_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => ThermalPersistenceMapper.BuildThermalProjectData(null!));
        }

        /// <summary>
        /// Production wire-contract proof (Todo 10): сериализация выполняется теми
        /// же опциями, что и save-путь ProjectFileService (WriteIndented +
        /// CamelCase + WhenWritingNull + JsonStringEnumConverter(camelCase)).
        /// Набор имён свойств thermalData/selectedPipe/result равен точному
        /// ожидаемому множеству — ни добавлений, ни удалений; статус/сообщения/
        /// origins/Article/ThermalConductivity/runtime-only поля отсутствуют.
        /// </summary>
        [Test]
        public void BuildThermalProjectData_SerializedPropertySet_IsExactWireContract()
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new System.Text.Json.Serialization.JsonStringEnumConverter(
                        System.Text.Json.JsonNamingPolicy.CamelCase)
                }
            };

            var fullDto = ThermalPersistenceMapper.BuildThermalProjectData(CreateFullSnapshot());
            var emptyDto = ThermalPersistenceMapper.BuildThermalProjectData(new ThermalStateSnapshot(
                ThermalInputsSnapshot.Default, null, ThermalStatusSnapshot.Default));

            var fullJson = System.Text.Json.JsonSerializer.Serialize(fullDto, options);
            var emptyJson = System.Text.Json.JsonSerializer.Serialize(emptyDto, options);

            using var fullDoc = System.Text.Json.JsonDocument.Parse(fullJson);
            using var emptyDoc = System.Text.Json.JsonDocument.Parse(emptyJson);
            var fullRoot = fullDoc.RootElement;
            var emptyRoot = emptyDoc.RootElement;

            var expectedRoot = new[]
            {
                "selectedMode", "supplyTemperature", "groundTemperature",
                "selectedPipe", "pipeSpacing", "result"
            };
            var expectedEmptyRoot = new[]
            {
                "selectedMode", "supplyTemperature", "groundTemperature", "pipeSpacing"
            };
            var expectedPipe = new[] { "name", "outerDiameter", "innerDiameter", "wallThickness" };
            var expectedResult = new[]
            {
                "powerUp", "powerDown", "powerTotal", "supplyTemperature",
                "returnTemperature", "meanTemperature", "deltaT", "isValid"
            };

            Assert.Multiple(() =>
            {
                Assert.That(fullRoot.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray(),
                    Is.EqualTo(expectedRoot.OrderBy(n => n).ToArray()),
                    "thermalData property set must be exactly the DEC-T08 wire contract.");
                Assert.That(emptyRoot.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray(),
                    Is.EqualTo(expectedEmptyRoot.OrderBy(n => n).ToArray()),
                    "Null pipe/result must be omitted by WhenWritingNull without adding properties.");

                var pipe = fullRoot.GetProperty("selectedPipe");
                Assert.That(pipe.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray(),
                    Is.EqualTo(expectedPipe.OrderBy(n => n).ToArray()));

                var result = fullRoot.GetProperty("result");
                Assert.That(result.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray(),
                    Is.EqualTo(expectedResult.OrderBy(n => n).ToArray()),
                    "Persisted result subset must be exactly the eight-property contract.");

                // Запрещённые имена нигде в thermalData не встречаются.
                var forbidden = new[]
                {
                    "article", "thermalConductivity", "status", "phase",
                    "recalculationMessage", "validationMessage", "origin",
                    "validationErrors", "alpha", "meltingHeat", "radiationHeat",
                    "convectionHeat", "excessTemperature", "rFb", "rD",
                    "parameterM", "efficiencyEtaR", "massFlowRate", "volumeFlowRate"
                };
                foreach (var name in forbidden)
                {
                    Assert.That(fullJson, Does.Not.Contain("\"" + name + "\""),
                        $"Runtime-only/status field '{name}' must never be persisted.");
                }
            });
        }

        /// <summary>
        /// Семантический round-trip через обе половины маппера: save → restore
        /// восстанавливает входы поэлементно; результат — по точному
        /// восьмиполевому контракту, runtime-only поля — CLR-дефолты (DEC-T08).
        /// </summary>
        [Test]
        public void BuildThermalProjectData_RoundTripThroughRestoreHalf_IsSemanticallyEqual()
        {
            var snapshot = CreateFullSnapshot();

            var dto = ThermalPersistenceMapper.BuildThermalProjectData(snapshot);
            var restoredInputs = ThermalPersistenceMapper.BuildInputsCandidate(dto, StandardPipes);
            var restoredResult = ThermalPersistenceMapper.BuildSavedResult(dto.Result);

            Assert.Multiple(() =>
            {
                Assert.That(restoredInputs, Is.EqualTo(snapshot.Inputs),
                    "Inputs must survive the DTO boundary field-by-field.");
                Assert.That(restoredResult, Is.Not.Null);
                Assert.That(restoredResult!.PowerUp, Is.EqualTo(snapshot.Result!.PowerUp));
                Assert.That(restoredResult.PowerDown, Is.EqualTo(snapshot.Result.PowerDown));
                Assert.That(restoredResult.PowerTotal, Is.EqualTo(snapshot.Result.PowerTotal));
                Assert.That(restoredResult.SupplyTemperature, Is.EqualTo(snapshot.Result.SupplyTemperature));
                Assert.That(restoredResult.ReturnTemperature, Is.EqualTo(snapshot.Result.ReturnTemperature));
                Assert.That(restoredResult.MeanTemperature, Is.EqualTo(snapshot.Result.MeanTemperature));
                Assert.That(restoredResult.DeltaT, Is.EqualTo(snapshot.Result.DeltaT));
                Assert.That(restoredResult.IsValid, Is.True);
                Assert.That(restoredResult.Alpha, Is.Zero, "Runtime-only fields restore as CLR defaults.");
                Assert.That(restoredResult.ValidationErrors, Is.Empty);
            });
        }
    }
}
