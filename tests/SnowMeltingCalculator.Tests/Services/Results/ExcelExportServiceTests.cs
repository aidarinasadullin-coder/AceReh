// ================================================================================
// Тесты ExcelExportService — smoke выгрузки спецификации (.xlsx)
// ================================================================================
//
// По образцу PdfExportServiceTests: реальный файл в WorkDirectory (Guid-имя,
// удаление в TearDown), magic-header zip (PK), обратное открытие ClosedXML,
// числовые ячейки (суммируемость), невалидный путь → false.
//
// ================================================================================

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using NUnit.Framework;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Tests.Services.Results
{
    [TestFixture]
    public class ExcelExportServiceTests
    {
        private string _testDir = null!;
        private string _filePath = null!;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "xlsx_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            _filePath = Path.Combine(_testDir, Guid.NewGuid().ToString("N") + ".xlsx");
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

        private static ResultsSpecificationData CreateData()
        {
            var data = new ResultsSpecificationData
            {
                ProjectNumber = "П-123",
                ProjectObject = "Пермь, площадка",
                TotalCircuits = 10,
                TotalPipeLength_m = 523.0,
                TotalPower_kW = 9.8
            };
            data.Rows.Add(new SpecificationRow
            {
                Section = "Труба",
                Name = "RAUTHERM S 17x2,0",
                Article = "1200170000",
                Unit = "м",
                Quantity = 523.0
            });
            data.Rows.Add(new SpecificationRow
            {
                Section = "Резьбозажимные соединения",
                Name = "Резьбозажимное соединение THERM S 17x2 (G3/4\")",
                Article = "12506073002",
                Unit = "шт",
                Quantity = 20
            });
            data.Rows.Add(new SpecificationRow
            {
                Section = "Итого по проекту",
                Name = "Мощность",
                Unit = "кВт",
                Quantity = 9.8
            });
            data.CircuitRows.Add(new SpecificationCircuitRow
            {
                CollectorNumber = 1,
                CollectorType = "HKV-D (6 контуров)",
                CircuitNumber = 1,
                LoopLength_m = 54.0,
                SupplyLength_m = 2.5,
                Area_m2 = 8.1,
                Power_W = 1050,
                FlowRate_lh = 181,
                PressureLoss_Pa = 18400,
                ValveTurns = 3.0,
                ZuDrosseln_Pa = 2500
            });
            return data;
        }

        [Test]
        public async Task Export_WritesFileWithZipMagicHeader()
        {
            var service = new ExcelExportService();

            var success = await service.ExportSpecificationToXlsxAsync(_filePath, CreateData());

            Assert.That(success, Is.True);
            Assert.That(File.Exists(_filePath), Is.True);
            var bytes = File.ReadAllBytes(_filePath);
            Assert.That(bytes.Length, Is.GreaterThan(4));
            Assert.That(bytes[0], Is.EqualTo(0x50)); // 'P'
            Assert.That(bytes[1], Is.EqualTo(0x4B)); // 'K'
        }

        [Test]
        public async Task Export_BothSheetsReadableBackWithExpectedContent()
        {
            var service = new ExcelExportService();
            Assert.That(await service.ExportSpecificationToXlsxAsync(_filePath, CreateData()), Is.True);

            using var workbook = new XLWorkbook(_filePath);

            var spec = workbook.Worksheet("Спецификация");
            Assert.That(spec.Cell(1, 1).GetString(), Does.Contain("спецификация закупки"));
            Assert.That(spec.Cell(5, 1).GetString(), Is.EqualTo("Наименование"));
            Assert.That(spec.Cell(5, 4).GetString(), Is.EqualTo("Кол-во"));

            var pipeRow = spec.RowsUsed().Single(r => r.Cell(1).GetString() == "RAUTHERM S 17x2,0");
            Assert.That(pipeRow.Cell(2).GetString(), Is.EqualTo("1200170000"));
            Assert.That(pipeRow.Cell(4).GetDouble(), Is.EqualTo(523.0).Within(0.001));

            var rzsRow = spec.RowsUsed().Single(r => r.Cell(2).GetString() == "12506073002");
            Assert.That(rzsRow.Cell(4).GetDouble(), Is.EqualTo(20.0).Within(0.001));

            var circuits = workbook.Worksheet("Контуры");
            Assert.That(circuits.Cell(1, 1).GetString(), Is.EqualTo("Наладка по контурам"));
            Assert.That(circuits.Cell(4, 3).GetString(), Is.EqualTo("Петля, м"));
            Assert.That(circuits.Cell(4, 10).GetString(), Is.EqualTo("Преднастройка, кПа"));

            // Структура листа: 1 титул, 2 итоги, 3 пусто, 4 шапка, 5 секция, 6+ данные
            Assert.That(circuits.Cell(6, 3).GetDouble(), Is.EqualTo(54.0).Within(0.001));
            Assert.That(circuits.Cell(6, 7).GetDouble(), Is.EqualTo(181.0).Within(0.001));
            Assert.That(circuits.Cell(6, 8).GetDouble(), Is.EqualTo(18.4).Within(0.001));
            Assert.That(circuits.Cell(6, 10).GetDouble(), Is.EqualTo(2.5).Within(0.001));
        }

        [Test]
        public async Task Export_QuantityCellsAreNumeric_Summable()
        {
            var service = new ExcelExportService();
            Assert.That(await service.ExportSpecificationToXlsxAsync(_filePath, CreateData()), Is.True);

            using var workbook = new XLWorkbook(_filePath);
            var spec = workbook.Worksheet("Спецификация");
            var pipeRow = spec.RowsUsed().Single(r => r.Cell(1).GetString() == "RAUTHERM S 17x2,0");

            Assert.That(pipeRow.Cell(4).DataType, Is.EqualTo(XLDataType.Number));
        }

        [Test]
        public async Task Export_InvalidPath_ReturnsFalseWithoutThrow()
        {
            var service = new ExcelExportService();
            // Путь указывает на существующий каталог — запись файла невозможна
            var badPath = Path.Combine(_testDir, "as_directory");

            var success = await service.ExportSpecificationToXlsxAsync(badPath, CreateData());

            Assert.That(success, Is.False);
        }
    }
}
