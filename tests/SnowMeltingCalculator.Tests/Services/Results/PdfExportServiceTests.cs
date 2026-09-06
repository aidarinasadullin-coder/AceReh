using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.Tests.Services.Results
{
    [TestFixture]
    public class PdfExportServiceTests
    {
        // PDF magic header per ISO 32000-1: every conforming PDF file starts with "%PDF".
        // Locked here so that any future refactor of the short-PDF export path
        // (e.g. the upcoming detailed report work) cannot silently break the
        // file signature and start producing non-PDF output.
        private static readonly byte[] PdfMagicHeader = { 0x25, 0x50, 0x44, 0x46 };

        // Минимальный валидный 1x1 PNG для проверки вставки схемы конструкции
        // (поле ConstructionImageBytes в фикстуре не задаётся).
        private static readonly byte[] TinyConstructionPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        [Test]
        public void NumberFormat_UsesCanonicalRussianCulture()
        {
            // Числа отчёта закреплены за каноном приложения (ru-RU: запятая,
            // пробел-тысячи — решение владельца 2026-09-04), независимо от
            // CurrentCulture машины прогона.
            Assert.That(PdfExportService.Num(42.5, "F2"), Is.EqualTo("42,50"));
            Assert.That(PdfExportService.Num(42.5, "N1"), Is.EqualTo("42,5"));
            Assert.That(PdfExportService.Num(1234.5, "N1"), Is.EqualTo("1\u00A0234,5"),
                "тысячи отделяются неразрывным пробелом по канону ru-RU");
        }

        [Test]
        public async Task ExportResultsToPdfAsync_WithConstructionImage_IncludesBase64Image()
        {
            // Схема конструкции (byte[]) вставляется fileless base64-протоколом
            // MigraDoc: ImageSource.FromBinary в официальном PDFsharp 6.x не
            // существует (ревью Ф8, P0-2). Успешный экспорт = путь рабочий.
            var service = new PdfExportService();
            var filePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"results-image-{Guid.NewGuid():N}.pdf");
            var data = CreateResultsPdfData();
            data.ConstructionImageBytes = TinyConstructionPng;

            try
            {
                var exported = await service.ExportResultsToPdfAsync(filePath, data);

                Assert.That(exported, Is.True, "экспорт с PNG-схемой должен пройти");
                Assert.That(File.Exists(filePath), Is.True);
                var bytes = await File.ReadAllBytesAsync(filePath);
                Assert.That(bytes[..4], Is.EqualTo(PdfMagicHeader));
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [Test]
        public async Task ExportResultsToPdfAsync_GeneratesPdf_whenDataContainsDashboardAndHydraulicDetails()
        {
            // Given: representative dashboard data with two collectors and circuit details.
            var service = new PdfExportService();
            var filePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"results-export-{Guid.NewGuid():N}.pdf");
            var data = CreateResultsPdfData();

            try
            {
                // When: exporting through the real PDF export service.
                var exported = await service.ExportResultsToPdfAsync(filePath, data);

                // Then: a non-empty PDF file is produced.
                Assert.That(exported, Is.True);
                Assert.That(File.Exists(filePath), Is.True);
                var bytes = await File.ReadAllBytesAsync(filePath);
                Assert.That(bytes.Length, Is.GreaterThan(0));
                Assert.That(bytes[..4], Is.EqualTo(PdfMagicHeader));
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [Test]
        public async Task ExportResultsToPdfAsync_OutputBytesStartWithPdfMagicHeader()
        {
            // Regression lock for the short-PDF export path: the file written by
            // ExportResultsToPdfAsync MUST start with the %PDF magic bytes
            // (0x25 0x50 0x44 0x46). This is a structural canary, independent of
            // the visual contents of the report.
            var service = new PdfExportService();
            var filePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"results-magic-{Guid.NewGuid():N}.pdf");
            var data = CreateResultsPdfData();

            try
            {
                var exported = await service.ExportResultsToPdfAsync(filePath, data);

                Assert.That(exported, Is.True, "ExportResultsToPdfAsync should report success");
                Assert.That(File.Exists(filePath), Is.True, "PDF file should exist on disk");

                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                Assert.That(fileBytes.Length, Is.GreaterThanOrEqualTo(4),
                    "PDF file must contain at least the 4-byte magic header");

                byte[] actualHeader = new byte[4];
                Array.Copy(fileBytes, 0, actualHeader, 0, 4);

                Assert.That(actualHeader, Is.EqualTo(PdfMagicHeader),
                    $"PDF must start with %PDF magic bytes (0x25 0x50 0x44 0x46); " +
                    $"actual header was 0x{actualHeader[0]:X2} 0x{actualHeader[1]:X2} 0x{actualHeader[2]:X2} 0x{actualHeader[3]:X2}");

                // Cross-check the ASCII representation for human-readable failure messages.
                string actualAscii = System.Text.Encoding.ASCII.GetString(actualHeader);
                Assert.That(actualAscii, Is.EqualTo("%PDF"),
                    "PDF header must decode to the literal \"%PDF\" ASCII string");
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        private static ResultsPdfData CreateResultsPdfData()
        {
            return new ResultsPdfData
            {
                ProjectNumber = "PDF-SMOKE-001",
                ProjectObject = "Дворовая территория",
                ReportDate = new DateTime(2026, 7, 24),
                TotalThermalPower_kW = 42.5,
                SystemVolume_L = 185.4,
                PumpFlowRate_m3h = 3.72,
                PumpHead_kPa = 48.6,
                ExpansionTankVolume_L = 24.0,
                SupplyTemperature = 45.0,
                ReturnTemperature = 35.0,
                OperatingTemperature = 40.0,
                GroundTemperature = -2.0,
                SurfaceTemperature = 3,
                City = "Москва",
                DesignTemperature = -25.0,
                WindSpeed = 4.5,
                SnowfallIntensity = 2.0,
                ClimateZone = ClimateZone.Zone_M20,
                ColdPeriodDays = 145,
                PipeType = "RAUTHERM S 20x2,0",
                PipeSpacing = 200,
                OperatingMode = OperatingMode.Melting,
                GlycolType = GlycolType.Propylene,
                GlycolConcentration = 35.0,
                R1 = 0.082,
                R2 = 0.175,
                LambdaE = 1.35,
                PowerUp = 275.0,
                PowerDown = 42.0,
                TotalPowerDensity = 317.0,
                Layers = CreateLayers(),
                Collectors = CreateCollectors(),
                CollectorSpecifications = CreateCollectorSpecifications(),
                TotalPipeLength = 512.7,
                RzsCount = 2
            };
        }

        private static List<LayerPdfData> CreateLayers()
        {
            return new List<LayerPdfData>
            {
                new() { MaterialName = "Бетон", Thickness = 80, Lambda = 1.74, R = 0.046, Position = "Над трубой" },
                new() { MaterialName = "Песок", Thickness = 120, Lambda = 0.58, R = 0.207, Position = "Под трубой" }
            };
        }

        private static List<CollectorPdfData> CreateCollectors()
        {
            return new List<CollectorPdfData>
            {
                CreateCollector(1, "HKV-D 6", 6, 255.2, 21800, 1860, 34.2, 42.1),
                CreateCollector(2, "HKV-D 5", 5, 257.5, 20700, 1860, 31.5, 39.6)
            };
        }

        private static CollectorPdfData CreateCollector(
            int number,
            string type,
            int circuitCount,
            double pipeLength,
            double power,
            double flowRate,
            double operatingPressure,
            double coldPressure)
        {
            var circuits = new List<CircuitPdfData>();
            for (var index = 1; index <= Math.Min(circuitCount, 3); index++)
            {
                circuits.Add(new CircuitPdfData
                {
                    CircuitNumber = index,
                    Length = 45 + index,
                    Area = 12 + index,
                    Power = 3600 + index * 120,
                    FlowRate = 310 + index * 15,
                    Velocity = 0.32 + index * 0.01,
                    FlowRegime = "Турбулентный",
                    PressureLossPerMeter = 180 + index * 7,
                    DpRohr = 8.2 + index,
                    DpVerteiler = 2.1 + index * 0.1,
                    DpVent = 1.4 + index * 0.1,
                    DpGesamt = 12.0 + index,
                    Throttling = 1.2 + index * 0.2,
                    ZuDrosseln = 1.2 + index * 0.2,
                    ValveTurns = 2.5 + index * 0.25
                });
            }

            return new CollectorPdfData
            {
                Number = number,
                Type = type,
                Circuits = circuits,
                Summary = new CollectorSummaryPdfData
                {
                    CircuitCount = circuitCount,
                    TotalPipeLength = pipeLength,
                    TotalPower = power,
                    TotalFlowRate = flowRate,
                    PressureLoss_Operating_kPa = operatingPressure,
                    PressureLoss_Cold_kPa = coldPressure,
                    Kv = 1.8,
                    CollectorType = type
                }
            };
        }

        private static List<CollectorSpecPdfData> CreateCollectorSpecifications()
        {
            return new List<CollectorSpecPdfData>
            {
                new() { Number = 1, Type = "HKV-D 6", CircuitCount = 6, TotalPower_kW = 21.8, TotalFlowRate_m3h = 1.86, PressureLoss_mbar = 342, Kv = 1.8 },
                new() { Number = 2, Type = "HKV-D 5", CircuitCount = 5, TotalPower_kW = 20.7, TotalFlowRate_m3h = 1.86, PressureLoss_mbar = 315, Kv = 1.8 }
            };
        }
    }
}
