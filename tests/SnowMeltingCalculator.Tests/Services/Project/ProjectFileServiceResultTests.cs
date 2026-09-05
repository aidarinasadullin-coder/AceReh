using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public class ProjectFileServiceResultTests
    {
        private ProjectFileService _service = null!;
        private string _testDir = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new ProjectFileService();
            _testDir = Path.Combine(Path.GetTempPath(), $"smc-result-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, recursive: true);
                }
            }
            catch
            {
                // Игнорируем ошибки очистки тестовой директории
            }
        }

        [Test]
        public async Task SaveProjectResultAsync_OnIoFailure_ReturnsFailureWithMessage()
        {
            // Arrange
            var badPath = "Z:/nonexistent_dir/file.smc";

            // Act
            var result = await _service.SaveProjectResultAsync(badPath, new ProjectData());

            // Assert
            Assert.That(result.IsSuccess, Is.False, "Метод должен вернуть ошибку при недоступном пути");
            Assert.That(result.Error, Is.Not.Null, "Ошибка должна содержать сообщение");
            Assert.That(result.Exception, Is.Not.Null, "Ошибка должна содержать исключение");
        }

        [Test]
        public async Task LoadProjectResultAsync_LegacyFileWithHasLoads_TreatsAsNoLoads()
        {
            // ADR-005 (Фаза 4Б): флаг «Нагрузки на покрытие» удалён из формата.
            // Старый .smc с "hasLoads": true обязан открываться; поле игнорируется,
            // проект считается «без нагрузок» (правило 40 мм).
            var legacyJson =
"""
{
  "version": "1.1",
  "climateData": { "city": "Норильск" },
  "constructionData": {
    "groundwaterLevel": 1.2,
    "hasLoads": true,
    "layers": [
      { "position": "AbovePipe", "materialName": "Бетон", "materialLambda": 1.74, "thickness": 40, "calculatedR": 0.0229, "calculatedLambda": 1.74, "isLambdaOverridden": false, "order": 0 }
    ]
  }
}
""";

            var filePath = Path.Combine(_testDir, "legacy-hasloads.smc");
            await File.WriteAllTextAsync(filePath, legacyJson);

            // Act
            var result = await _service.LoadProjectResultAsync(filePath);

            // Assert
            Assert.That(result.IsSuccess, Is.True, "Старый .smc с hasLoads обязан открываться без ошибок.");
            Assert.That(result.Value!.ConstructionData.GroundwaterLevel, Is.EqualTo(1.2).Within(1e-9));
            Assert.That(result.Value.ConstructionData.Layers, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task LoadProjectResultAsync_OnMissingFile_ReturnsFailureWithFileNotFound()
        {
            // Arrange
            var missingPath = Path.Combine(_testDir, "missing.smc");

            // Act
            var result = await _service.LoadProjectResultAsync(missingPath);

            // Assert
            Assert.That(result.IsSuccess, Is.False, "Метод должен вернуть ошибку для отсутствующего файла");
            Assert.That(result.Error, Is.Not.Null, "Ошибка должна содержать сообщение");
            Assert.That(result.Error, Does.Contain("не найден").Or.Contains("missing"), "Ошибка должна указывать на отсутствие файла");
        }

        [Test]
        public async Task LoadProjectResultAsync_OnCorruptJson_ReturnsFailureWithDeserializationError()
        {
            // Arrange
            var corruptPath = Path.Combine(_testDir, "corrupt.smc");
            await File.WriteAllTextAsync(corruptPath, "{ invalid json");

            // Act
            var result = await _service.LoadProjectResultAsync(corruptPath);

            // Assert
            Assert.That(result.IsSuccess, Is.False, "Метод должен вернуть ошибку для повреждённого JSON");
            Assert.That(result.Error, Is.Not.Null, "Ошибка должна содержать сообщение");
            Assert.That(result.Error, Does.Contain("Ошибка десериализации"), "Ошибка должна указывать на десериализацию");
        }

        [Test]
        public async Task SaveProjectResultAsync_OnSuccess_ReturnsSuccessWithNullValue()
        {
            // Arrange
            var filePath = Path.Combine(_testDir, "project.smc");
            var data = new ProjectData
            {
                ProjectNumber = "PRJ-001",
                ProjectObject = "Test object"
            };

            // Act
            var result = await _service.SaveProjectResultAsync(filePath, data);

            // Assert
            Assert.That(result.IsSuccess, Is.True, "Сохранение должно быть успешным");
            Assert.That(result.Value, Is.Null, "Успешное сохранение должно возвращать null");
            Assert.That(File.Exists(filePath), Is.True, "Файл проекта должен существовать");
        }

        [Test]
        public async Task SaveAndLoadProjectResult_RoundTripsCollectorAndCircuitResults()
        {
            // Regression lock for the .smc schema: a ProjectData with a populated
            // HydraulicsData.Collectors[*].Circuits[*] graph, including both
            // OperatingResult and DesignResult for every circuit, MUST survive
            // a SaveProjectResultAsync -> LoadProjectResultAsync round trip
            // without any value drift in hydraulic / thermal result fields.
            //
            // This guards the existing short-PDF export path before the new
            // detailed report code is introduced.

            // Arrange
            var filePath = Path.Combine(_testDir, "roundtrip.smc");
            var original = CreateProjectDataWithCollectorResults();

            // Act: save
            var saveResult = await _service.SaveProjectResultAsync(filePath, original);
            Assert.That(saveResult.IsSuccess, Is.True,
                $"SaveProjectResultAsync should succeed; got error: {saveResult.Error}");
            Assert.That(File.Exists(filePath), Is.True, ".smc file should exist on disk after save");

            // Act: load
            var loadResult = await _service.LoadProjectResultAsync(filePath);
            Assert.That(loadResult.IsSuccess, Is.True,
                $"LoadProjectResultAsync should succeed; got error: {loadResult.Error}");
            Assert.That(loadResult.Value, Is.Not.Null, "Loaded ProjectData must not be null");

            // Assert: top-level project metadata preserved
            var loaded = loadResult.Value!;
            Assert.That(loaded.ProjectNumber, Is.EqualTo(original.ProjectNumber));
            Assert.That(loaded.ProjectObject, Is.EqualTo(original.ProjectObject));
            Assert.That(loaded.Version, Is.EqualTo(original.Version));

            // Assert: collector + circuits survived structurally
            Assert.That(loaded.HydraulicsData.Collectors, Has.Count.EqualTo(1),
                "Loaded project must contain exactly one collector");
            var originalCollector = original.HydraulicsData.Collectors[0];
            var loadedCollector = loaded.HydraulicsData.Collectors[0];
            Assert.That(loadedCollector.CollectorNumber, Is.EqualTo(originalCollector.CollectorNumber));
            Assert.That(loadedCollector.CollectorType, Is.EqualTo(originalCollector.CollectorType));
            Assert.That(loadedCollector.ValveType, Is.EqualTo(originalCollector.ValveType));
            Assert.That(loadedCollector.Circuits, Has.Count.EqualTo(originalCollector.Circuits.Count));

            // Assert: per-circuit hydraulic/thermal result values preserved bit-for-bit
            for (int i = 0; i < originalCollector.Circuits.Count; i++)
            {
                var origCircuit = originalCollector.Circuits[i];
                var loadedCircuit = loadedCollector.Circuits[i];

                Assert.That(loadedCircuit.CircuitNumber, Is.EqualTo(origCircuit.CircuitNumber),
                    $"Circuit #{i} number mismatch");
                Assert.That(loadedCircuit.CircuitLength, Is.EqualTo(origCircuit.CircuitLength),
                    $"Circuit #{i} length mismatch");
                Assert.That(loadedCircuit.SupplyLength, Is.EqualTo(origCircuit.SupplyLength),
                    $"Circuit #{i} supply length mismatch");
                Assert.That(loadedCircuit.PipeSpacingCm, Is.EqualTo(origCircuit.PipeSpacingCm),
                    $"Circuit #{i} pipe spacing mismatch");
                Assert.That(loadedCircuit.Power, Is.EqualTo(origCircuit.Power),
                    $"Circuit #{i} power mismatch");
                Assert.That(loadedCircuit.FlowRate, Is.EqualTo(origCircuit.FlowRate),
                    $"Circuit #{i} flow rate mismatch");
                Assert.That(loadedCircuit.Velocity, Is.EqualTo(origCircuit.Velocity),
                    $"Circuit #{i} velocity mismatch");
                Assert.That(loadedCircuit.Throttling, Is.EqualTo(origCircuit.Throttling),
                    $"Circuit #{i} throttling mismatch");
                Assert.That(loadedCircuit.ValveTurns, Is.EqualTo(origCircuit.ValveTurns),
                    $"Circuit #{i} valve turns mismatch");

                AssertCircuitResult("OperatingResult", origCircuit.OperatingResult, loadedCircuit.OperatingResult, i);
                AssertCircuitResult("DesignResult", origCircuit.DesignResult, loadedCircuit.DesignResult, i);
            }
        }

        private static void AssertCircuitResult(
            string label,
            CircuitResultProjectData? expected,
            CircuitResultProjectData? actual,
            int circuitIndex)
        {
            Assert.That(actual, Is.Not.Null,
                $"Circuit #{circuitIndex} {label} must not be null after round trip");
            Assert.That(expected, Is.Not.Null,
                $"Test setup error: original Circuit #{circuitIndex} {label} is null");

            // expected is non-null thanks to the assertion above
            var e = expected!;
            var a = actual!;

            Assert.That(a.Power, Is.EqualTo(e.Power), $"Circuit #{circuitIndex} {label}.Power");
            Assert.That(a.FlowRate, Is.EqualTo(e.FlowRate), $"Circuit #{circuitIndex} {label}.FlowRate");
            Assert.That(a.Velocity, Is.EqualTo(e.Velocity), $"Circuit #{circuitIndex} {label}.Velocity");
            Assert.That(a.DpRohr, Is.EqualTo(e.DpRohr), $"Circuit #{circuitIndex} {label}.DpRohr");
            Assert.That(a.DpVerteiler, Is.EqualTo(e.DpVerteiler), $"Circuit #{circuitIndex} {label}.DpVerteiler");
            Assert.That(a.DpVent, Is.EqualTo(e.DpVent), $"Circuit #{circuitIndex} {label}.DpVent");
            Assert.That(a.DpGesamt, Is.EqualTo(e.DpGesamt), $"Circuit #{circuitIndex} {label}.DpGesamt");
            Assert.That(a.Throttling, Is.EqualTo(e.Throttling), $"Circuit #{circuitIndex} {label}.Throttling");
            Assert.That(a.ValveTurns, Is.EqualTo(e.ValveTurns), $"Circuit #{circuitIndex} {label}.ValveTurns");
            Assert.That(a.FlowRegime, Is.EqualTo(e.FlowRegime), $"Circuit #{circuitIndex} {label}.FlowRegime");
            Assert.That(a.FlowRegimeString, Is.EqualTo(e.FlowRegimeString),
                $"Circuit #{circuitIndex} {label}.FlowRegimeString");
            Assert.That(a.Density, Is.EqualTo(e.Density), $"Circuit #{circuitIndex} {label}.Density");
            Assert.That(a.KinematicViscosity, Is.EqualTo(e.KinematicViscosity),
                $"Circuit #{circuitIndex} {label}.KinematicViscosity");
            Assert.That(a.ReynoldsNumber, Is.EqualTo(e.ReynoldsNumber),
                $"Circuit #{circuitIndex} {label}.ReynoldsNumber");
            Assert.That(a.FrictionFactor, Is.EqualTo(e.FrictionFactor),
                $"Circuit #{circuitIndex} {label}.FrictionFactor");
            Assert.That(a.PressureLossPerMeter, Is.EqualTo(e.PressureLossPerMeter),
                $"Circuit #{circuitIndex} {label}.PressureLossPerMeter");
        }

        private static ProjectData CreateProjectDataWithCollectorResults()
        {
            // Two circuits per collector: a "standard" one and a reference
            // (max loss) one. Each circuit carries both OperatingResult and
            // DesignResult populated with distinct, non-default values so the
            // round-trip assertions can detect any silent zeroing or swapping.
            return new ProjectData
            {
                Version = "1.1",
                ProjectNumber = "SMC-ROUNDTRIP-001",
                ProjectObject = "Round-trip regression object",
                CreatedDate = new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2026, 7, 27, 1, 0, 0, DateTimeKind.Utc),
                IsOperatingMode = true,
                HydraulicsData = new HydraulicsProjectData
                {
                    GlycolType = GlycolType.Propylene,
                    GlycolConcentration = 35.0,
                    SupplySpacingCm = 5.0,
                    SupplyHeatPercent = 10.0,
                    Collectors = new List<CollectorProjectData>
                    {
                        new CollectorProjectData
                        {
                            CollectorNumber = 1,
                            CollectorType = "HKV-D (2-12 контуров)",
                            ValveType = ValveType.HKV_D,
                            Circuits = new List<CircuitProjectData>
                            {
                                CreateCircuit(1, operatingLoss: 210.0, designLoss: 285.0,
                                    operatingReynolds: 4200.5, designReynolds: 2750.25,
                                    operatingVelocity: 0.34, designVelocity: 0.21,
                                    operatingDpRohr: 9876.5, operatingDpVerteiler: 1234.5, operatingDpVent: 567.8,
                                    designDpRohr: 12345.0, designDpVerteiler: 2000.0, designDpVent: 900.0),
                                CreateCircuit(2, operatingLoss: 240.0, designLoss: 310.0,
                                    operatingReynolds: 3980.75, designReynolds: 2520.5,
                                    operatingVelocity: 0.31, designVelocity: 0.19,
                                    operatingDpRohr: 10500.0, operatingDpVerteiler: 1500.0, operatingDpVent: 600.0,
                                    designDpRohr: 13100.0, designDpVerteiler: 2200.0, designDpVent: 950.0)
                            },
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 2,
                                TotalPipeLength = 120.5,
                                TotalPower = 7200.0,
                                TotalFlowRate = 620.0,
                                PressureLoss_Operating_Pa = 12600.0,
                                PressureLoss_Cold_Pa = 16250.0,
                                Kv = 1.8,
                                CollectorType = "HKV-D"
                            }
                        }
                    }
                }
            };
        }

        private static CircuitProjectData CreateCircuit(
            int circuitNumber,
            double operatingLoss,
            double designLoss,
            double operatingReynolds,
            double designReynolds,
            double operatingVelocity,
            double designVelocity,
            double operatingDpRohr,
            double operatingDpVerteiler,
            double operatingDpVent,
            double designDpRohr,
            double designDpVerteiler,
            double designDpVent)
        {
            return new CircuitProjectData
            {
                CircuitNumber = circuitNumber,
                CircuitLength = 55.0 + circuitNumber,
                SupplyLength = 12.0,
                SupplySpacingCm = 5.0,
                SupplyHeatPercent = 10.0,
                PipeSpacingCm = 20.0,
                Power = 3500.0 + circuitNumber * 200.0,
                FlowRate = 310.0 + circuitNumber * 15.0,
                Velocity = operatingVelocity,
                Throttling = 1500.0,
                ValveTurns = 2.5,
                FlowRegimeDescription = "Турбулентный",
                OperatingResult = new CircuitResultProjectData
                {
                    Power = 3500.0 + circuitNumber * 200.0,
                    FlowRate = 310.0 + circuitNumber * 15.0,
                    Velocity = operatingVelocity,
                    DpRohr = operatingDpRohr,
                    DpVerteiler = operatingDpVerteiler,
                    DpVent = operatingDpVent,
                    DpGesamt = operatingDpRohr + operatingDpVerteiler + operatingDpVent,
                    Throttling = 1500.0,
                    ValveTurns = 2.5,
                    FlowRegime = "Turbulent",
                    FlowRegimeString = "Turbulent",
                    Density = 1.053,
                    KinematicViscosity = 0.0000046,
                    ReynoldsNumber = operatingReynolds,
                    FrictionFactor = 0.0285,
                    PressureLossPerMeter = operatingLoss
                },
                DesignResult = new CircuitResultProjectData
                {
                    Power = 4200.0 + circuitNumber * 250.0,
                    FlowRate = 290.0 + circuitNumber * 12.0,
                    Velocity = designVelocity,
                    DpRohr = designDpRohr,
                    DpVerteiler = designDpVerteiler,
                    DpVent = designDpVent,
                    DpGesamt = designDpRohr + designDpVerteiler + designDpVent,
                    Throttling = 2200.0,
                    ValveTurns = 3.5,
                    FlowRegime = "Transitional",
                    FlowRegimeString = "Transitional",
                    Density = 1.062,
                    KinematicViscosity = 0.0000091,
                    ReynoldsNumber = designReynolds,
                    FrictionFactor = 0.0342,
                    PressureLossPerMeter = designLoss
                }
            };
        }
    }
}
