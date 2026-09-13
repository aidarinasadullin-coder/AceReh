// ================================================================================
// Тесты ResultsSpecificationDataBuilder — данные спецификации закупки (.xlsx)
// ================================================================================
//
// Smoke/структурные по образцу PdfExportServiceTests: фикстурные снимки
// сессии (Moq) + временный json-каталог (fittings + collectors_hkv).
//
// ================================================================================

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Repositories.Fittings;
using SnowMeltingCalculator.Repositories.Hydraulics;
using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Tests.Services.Results
{
    [TestFixture]
    public class ResultsSpecificationDataBuilderTests
    {
        private string _testDir = null!;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "spec_" + Guid.NewGuid().ToString("N"));
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
            catch (IOException)
            {
            }
        }

        private string WriteCatalogJson()
        {
            var path = Path.Combine(_testDir, "rehau_products.json");
            File.WriteAllText(path, """
                {
                  "collectors_hkv": [
                    { "id": "HKV_6", "name": "HKV 6", "full_name": "Коллектор РЕХАУ HKV 6", "circuits": 6,
                      "connection_size": "1\"", "max_flow_m3h": 1.5, "max_pressure_mbar": 320, "max_setting": 8,
                      "article_number": "12180613001", "notes": "" },
                    { "id": "HKV_4", "name": "HKV 4", "full_name": "Коллектор РЕХАУ HKV 4", "circuits": 4,
                      "connection_size": "1\"", "max_flow_m3h": 1.5, "max_pressure_mbar": 320, "max_setting": 8,
                      "article_number": "12180413001", "notes": "" }
                  ],
                  "fittings": [
                    { "id": "RZS_17x2", "name": "РЗС 17x2", "full_name": "Резьбозажимное соединение THERM S 17x2 (G3/4\")",
                      "article_number": "12506073002", "pipe_outer_diameter_mm": 17.0, "pipe_wall_thickness_mm": 2.0,
                      "notes": "На каждом подключении контура к коллектору — 2 шт на контур" },
                    { "id": "RZS_25x2_3", "name": "РЗС 25x2,3", "full_name": "Резьбозажимное соединение THERM S 25x2,3",
                      "article_number": null, "pipe_outer_diameter_mm": 25.0, "pipe_wall_thickness_mm": 2.3,
                      "notes": "Артикул уточняется" }
                  ]
                }
                """);
            return path;
        }

        private static ThermalPipeSnapshot MakePipe(double outer, double wall, string article)
        {
            return new ThermalPipeSnapshot($"RAUTHERM S {outer}x{wall}", article, outer, outer - 2 * wall, wall, 0.35);
        }

        private static HydraulicCircuitSnapshot MakeCircuit(int number, double loop, double supply, double spacingCm)
        {
            var result = new HydraulicCircuitResultSnapshot(
                power: 1050,
                flowRate: 181,
                velocity: 0.3,
                dpRohr: 16000,
                dpVerteiler: 400,
                dpVent: 2000,
                dpGesamt: 18400,
                throttling: 2500,
                valveTurns: 3.0,
                density: 1.0,
                kinematicViscosity: 1.0,
                reynoldsNumber: 10000,
                frictionFactor: 0.03,
                pressureLossPerMeter: 300);
            return new HydraulicCircuitSnapshot(number, loop, supply, 0, 0, spacingCm, result, null);
        }

        private static IProjectSession MakeSession(ThermalPipeSnapshot? pipe, params HydraulicCollectorSnapshot[] collectors)
        {
            var thermalState = new Mock<IProjectSessionThermalState>();
            thermalState.SetupGet(s => s.Snapshot).Returns(new ThermalStateSnapshot(
                new ThermalInputsSnapshot(OperatingMode.Melting, 35, 5, pipe, 15),
                null,
                ThermalStatusSnapshot.Default));

            var hydraulicsState = new Mock<IProjectSessionHydraulicsState>();
            hydraulicsState.SetupGet(s => s.Snapshot).Returns(new HydraulicsStateSnapshot(
                HydraulicGlobalInputsSnapshot.Default,
                collectors,
                HydraulicsStatusSnapshot.Default));

            var session = new Mock<IProjectSession>();
            session.SetupGet(s => s.ThermalState).Returns(thermalState.Object);
            session.SetupGet(s => s.HydraulicsState).Returns(hydraulicsState.Object);
            return session.Object;
        }

        private ResultsSpecificationDataBuilder MakeBuilder()
        {
            var path = WriteCatalogJson();
            return new ResultsSpecificationDataBuilder(
                new CollectorRepository(path),
                new FittingsRepository(path));
        }

        private static HydraulicCollectorSnapshot[] MakeCollectors()
        {
            var six = Enumerable.Range(1, 6)
                .Select(i => MakeCircuit(i, 54.0, 2.5, 15.0))
                .ToArray();
            var four = Enumerable.Range(1, 4)
                .Select(i => MakeCircuit(i, 44.0, 2.0, 15.0))
                .ToArray();
            return new[]
            {
                new HydraulicCollectorSnapshot(1, "HKV-D", ValveType.HKV_D, six, null),
                new HydraulicCollectorSnapshot(2, "HKV-D", ValveType.HKV_D, four, null)
            };
        }

        [Test]
        public async Task BuildAsync_Pipe17TwoCollectors_RowsAndCircuitsMatch()
        {
            var session = MakeSession(MakePipe(17.0, 2.0, "1200170000"), MakeCollectors());
            var builder = MakeBuilder();

            var data = await builder.BuildAsync(session, "П-123", "Пермь, площадка", CancellationToken.None);

            // Труба: метраж = Σ(петля + подача) = 6×56,5 + 4×46 = 523
            var pipeRow = data.Rows.Single(r => r.Section == "Труба");
            Assert.That(pipeRow.Article, Is.EqualTo("1200170000"));
            Assert.That(pipeRow.Unit, Is.EqualTo("м"));
            Assert.That(pipeRow.Quantity, Is.EqualTo(523.0).Within(0.001));

            // РЗС: 2 шт на каждый из 10 контуров, артикул из json-каталога
            var rzsRow = data.Rows.Single(r => r.Section == "Резьбозажимные соединения");
            Assert.That(rzsRow.Article, Is.EqualTo("12506073002"));
            Assert.That(rzsRow.Unit, Is.EqualTo("шт"));
            Assert.That(rzsRow.Quantity, Is.EqualTo(20.0).Within(0.001));
            Assert.That(rzsRow.Notes, Is.Not.Null);

            // Коллекторы: группировка по (ValveType, контуров), артикулы из каталога
            var collectorRows = data.Rows.Where(r => r.Section == "Коллекторы HKV").ToList();
            Assert.That(collectorRows, Has.Count.EqualTo(2));
            Assert.That(collectorRows[0].Name, Is.EqualTo("HKV-D (6 контуров)"));
            Assert.That(collectorRows[0].Article, Is.EqualTo("12180613001"));
            Assert.That(collectorRows[0].Quantity, Is.EqualTo(1.0).Within(0.001));
            Assert.That(collectorRows[1].Name, Is.EqualTo("HKV-D (4 контура)"));
            Assert.That(collectorRows[1].Article, Is.EqualTo("12180413001"));

            // Итого по проекту
            Assert.That(data.Rows.Single(r => r.Section == "Итого по проекту" && r.Name == "Контуры").Quantity,
                Is.EqualTo(10.0).Within(0.001));
            Assert.That(data.TotalPower_kW, Is.EqualTo(10.5).Within(0.001));

            // Контуры: площадь = петля × шаг / 100, преднастройка из Throttling
            Assert.That(data.CircuitRows, Has.Count.EqualTo(10));
            var first = data.CircuitRows[0];
            Assert.That(first.CollectorNumber, Is.EqualTo(1));
            Assert.That(first.LoopLength_m, Is.EqualTo(54.0).Within(0.001));
            Assert.That(first.SupplyLength_m, Is.EqualTo(2.5).Within(0.001));
            Assert.That(first.Area_m2, Is.EqualTo(54.0 * 15.0 / 100.0).Within(0.001));
            Assert.That(first.FlowRate_lh, Is.EqualTo(181.0).Within(0.001));
            Assert.That(first.PressureLoss_Pa, Is.EqualTo(18400.0).Within(0.001));
            Assert.That(first.ZuDrosseln_Pa, Is.EqualTo(2500.0).Within(0.001));
            Assert.That(first.ValveTurns, Is.EqualTo(3.0).Within(0.001));
        }

        [Test]
        public async Task BuildAsync_Pipe25_RzsArticleEmptyWithNote()
        {
            var session = MakeSession(MakePipe(25.0, 2.3, "1200250000"), MakeCollectors());
            var builder = MakeBuilder();

            var data = await builder.BuildAsync(session, "П-1", "Объект", CancellationToken.None);

            var rzsRow = data.Rows.Single(r => r.Section == "Резьбозажимные соединения");
            Assert.That(rzsRow.Article, Is.Empty);
            Assert.That(rzsRow.Notes, Is.Not.Null.And.Contains("уточняется"));
            Assert.That(rzsRow.Quantity, Is.EqualTo(20.0).Within(0.001));
        }

        [Test]
        public async Task BuildAsync_NoPipe_NoPipeAndRzsRows()
        {
            var session = MakeSession(null, MakeCollectors());
            var builder = MakeBuilder();

            var data = await builder.BuildAsync(session, "П-1", "Объект", CancellationToken.None);

            Assert.That(data.Rows.Where(r => r.Section == "Труба").ToList(), Is.Empty);
            Assert.That(data.Rows.Where(r => r.Section == "Резьбозажимные соединения").ToList(), Is.Empty);
            Assert.That(data.Rows.Where(r => r.Section == "Коллекторы HKV").ToList(), Has.Count.EqualTo(2));
            Assert.That(data.CircuitRows, Has.Count.EqualTo(10));
        }

        [Test]
        public async Task BuildAsync_EmptyHydraulics_OnlyTotals()
        {
            var session = MakeSession(MakePipe(17.0, 2.0, "1200170000"));
            var builder = MakeBuilder();

            var data = await builder.BuildAsync(session, "П-1", "Объект", CancellationToken.None);

            Assert.That(data.TotalCircuits, Is.EqualTo(0));
            Assert.That(data.CircuitRows, Is.Empty);
            Assert.That(data.Rows.Where(r => r.Section == "Резьбозажимные соединения").ToList(), Is.Empty);
            Assert.That(data.Rows.Single(r => r.Section == "Итого по проекту" && r.Name == "Труба").Quantity,
                Is.EqualTo(0.0).Within(0.001));
        }

        [Test]
        public async Task BuildAsync_CatalogMissing_RzsNameFallsBackToPipeName()
        {
            var emptyDir = Path.Combine(_testDir, "empty");
            Directory.CreateDirectory(emptyDir);
            var builder = new ResultsSpecificationDataBuilder(
                new CollectorRepository(Path.Combine(emptyDir, "missing.json")),
                new FittingsRepository(Path.Combine(emptyDir, "missing.json")));

            var session = MakeSession(MakePipe(17.0, 2.0, "1200170000"), MakeCollectors());
            var data = await builder.BuildAsync(session, "П-1", "Объект", CancellationToken.None);

            var rzsRow = data.Rows.Single(r => r.Section == "Резьбозажимные соединения");
            Assert.That(rzsRow.Article, Is.Empty);
            Assert.That(rzsRow.Name, Does.StartWith("Резьбозажимное соединение"));
            Assert.That(rzsRow.Quantity, Is.EqualTo(20.0).Within(0.001));

            var collectorRows = data.Rows.Where(r => r.Section == "Коллекторы HKV").ToList();
            Assert.That(collectorRows.All(r => r.Article == string.Empty), Is.True);
        }

        /// <summary>
        /// Пин реального data/rehau_products.json: json-ключи snake_case обязаны
        /// биндиться в DTO (PropertyNameCaseInsensitive подчёркивания не убирает —
        /// без JsonPropertyName каталог молча подменялся дефолтами, урок №25).
        /// </summary>
        [Test]
        public async Task RealProductCatalog_BindsArticlesFromJson()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "data", "rehau_products.json")))
            {
                dir = dir.Parent;
            }

            Assert.That(dir, Is.Not.Null, "data/rehau_products.json не найден выше каталога тестов");
            var path = Path.Combine(dir!.FullName, "data", "rehau_products.json");

            var collectors = await new CollectorRepository(path).GetAllAsync();
            var hkv6 = collectors.FirstOrDefault(c => c.Name == "HKV 6");
            Assert.That(hkv6, Is.Not.Null);
            Assert.That(hkv6!.ArticleNumber, Is.EqualTo("12180613001"));

            var fittings = await new FittingsRepository(path).GetAllAsync();
            var rzs17 = fittings.FirstOrDefault(f => Math.Abs(f.PipeOuterDiameterMm - 17.0) < 0.01);
            Assert.That(rzs17, Is.Not.Null);
            Assert.That(rzs17!.ArticleNumber, Is.EqualTo("12506073002"));
        }
    }
}
