using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Фокусные тесты сервиса экспорта детального расчётного отчёта в Markdown.
    /// </summary>
    [TestFixture]
    public class CalculationReportExportServiceTests
    {
        private string _testDir = null!;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ExportTests", TestContext.CurrentContext.Test.Name);
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }

            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }

        [Test]
        public async Task ExportReportAsync_OperatingMode_CreatesNonEmptyMarkdownWithOperatingLabel()
        {
            var service = CreateService();
            var filePath = Path.Combine(_testDir, "operating.md");
            var project = CreateMinimalProject();

            var result = await service.ExportReportAsync(filePath, project, CalculationReportMode.Operating);

            Assert.That(result, Is.True);
            Assert.That(File.Exists(filePath), Is.True);
            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            Assert.That(content, Is.Not.Empty);
            Assert.That(content, Does.Contain("Рабочий режим"));
            Assert.That(content, Does.Contain("## Методика"));
        }

        [Test]
        public async Task ExportReportAsync_DesignColdMode_CreatesNonEmptyMarkdownWithDesignColdLabel()
        {
            var service = CreateService();
            var filePath = Path.Combine(_testDir, "design-cold.md");
            var project = CreateMinimalProject();

            var result = await service.ExportReportAsync(filePath, project, CalculationReportMode.DesignCold);

            Assert.That(result, Is.True);
            Assert.That(File.Exists(filePath), Is.True);
            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            Assert.That(content, Is.Not.Empty);
            Assert.That(content, Does.Contain("Расчётный/холодный режим"));
            Assert.That(content, Does.Contain("## Методика"));
        }

        [Test]
        public async Task ExportReportAsync_InvalidPath_ReturnsFalseAndDoesNotThrow()
        {
            var service = CreateService();
            var project = CreateMinimalProject();
            var filePath = Path.Combine("Z:", "NonExistingRoot", "report.md");

            var result = await service.ExportReportAsync(filePath, project, CalculationReportMode.Operating);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ExportReportAsync_DirectoryDoesNotExist_CreatesDirectoryAndFile()
        {
            var service = CreateService();
            var project = CreateMinimalProject();
            var nestedDir = Path.Combine(_testDir, "sub", "dir");
            var filePath = Path.Combine(nestedDir, "report.md");

            var result = await service.ExportReportAsync(filePath, project, CalculationReportMode.Operating);

            Assert.That(result, Is.True);
            Assert.That(Directory.Exists(nestedDir), Is.True);
            Assert.That(File.Exists(filePath), Is.True);
        }

        [Test]
        public async Task ExportReportAsync_NullProject_ReturnsFalseAndDoesNotThrow()
        {
            var service = CreateService();
            var filePath = Path.Combine(_testDir, "null-project.md");

            var result = await service.ExportReportAsync(filePath, null!, CalculationReportMode.Operating);

            Assert.That(result, Is.False);
            Assert.That(File.Exists(filePath), Is.False);
        }

        [Test]
        public async Task ExportReportAsync_NullFilePath_ReturnsFalseAndDoesNotThrow()
        {
            var service = CreateService();
            var project = CreateMinimalProject();

            var result = await service.ExportReportAsync(null!, project, CalculationReportMode.Operating);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ExportReportAsync_PassesExplicitModeToBuilder()
        {
            var builder = new ModeCapturingBuilder();
            var renderer = new PassthroughRenderer();
            var service = new CalculationReportExportService(builder, renderer);
            var filePath = Path.Combine(_testDir, "mode-pass.md");
            var project = CreateMinimalProject();

            var result = await service.ExportReportAsync(filePath, project, CalculationReportMode.DesignCold);

            Assert.That(result, Is.True);
            Assert.That(builder.LastMode, Is.EqualTo(CalculationReportMode.DesignCold));
            Assert.That(renderer.LastData, Is.Not.Null);
            Assert.That(renderer.LastData.Mode, Is.EqualTo(CalculationReportMode.DesignCold));
        }

        [Test]
        public async Task ExportReportAsync_BuildsAndRendersOnce_WithoutMutatingProject()
        {
            var builder = new CountingBuilder();
            var renderer = new CountingRenderer();
            var service = new CalculationReportExportService(builder, renderer);
            var filePath = Path.Combine(_testDir, "single-pass.md");
            var project = CreateMinimalProject();
            var projectNumber = project.ProjectNumber;
            var projectObject = project.ProjectObject;

            var result = await service.ExportReportAsync(filePath, project, CalculationReportMode.Operating);

            Assert.That(result, Is.True);
            Assert.That(builder.BuildCount, Is.EqualTo(1));
            Assert.That(renderer.RenderCount, Is.EqualTo(1));
            Assert.That(project.ProjectNumber, Is.EqualTo(projectNumber));
            Assert.That(project.ProjectObject, Is.EqualTo(projectObject));
            Assert.That(project.ThermalData.Result, Is.Null);
            Assert.That(project.HydraulicsData.Collectors, Is.Empty);
        }

        [Test]
        public async Task ExportReportAsync_CancellationBeforeBuild_ThrowsOperationCanceledException()
        {
            var service = CreateService();
            var filePath = Path.Combine(_testDir, "cancelled.md");
            var project = CreateMinimalProject();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var ex = Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await service.ExportReportAsync(filePath, project, CalculationReportMode.Operating, cancellationToken: cts.Token));
            Assert.That(ex, Is.Not.Null);
        }

        [Test]
        public async Task ExportReportAsync_Utf8Encoding_PreservesRussianText()
        {
            var service = CreateService();
            var filePath = Path.Combine(_testDir, "utf8.md");
            var project = new ProjectData
            {
                ProjectNumber = "P-123",
                ProjectObject = "Площадка РЕХАУ"
            };

            var result = await service.ExportReportAsync(filePath, project, CalculationReportMode.Operating);

            Assert.That(result, Is.True);
            var bytes = await File.ReadAllBytesAsync(filePath);
            var content = Encoding.UTF8.GetString(bytes);
            Assert.That(content, Does.Contain("Площадка РЕХАУ"));
        }

        private static CalculationReportExportService CreateService()
        {
            return new CalculationReportExportService(
                new CalculationReportDataBuilder(),
                new CalculationReportMarkdownRenderer());
        }

        private static ProjectData CreateMinimalProject()
        {
            return new ProjectData
            {
                ProjectNumber = "P-001",
                ProjectObject = "Тестовая площадка",
                ClimateData = new ClimateProjectData(),
                ConstructionData = new ConstructionProjectData(),
                ThermalData = new ThermalProjectData(),
                HydraulicsData = new HydraulicsProjectData()
            };
        }

        private sealed class ModeCapturingBuilder : ICalculationReportDataBuilder
        {
            public CalculationReportMode LastMode { get; private set; }

            public CalculationReportData Build(ProjectData project, CalculationReportMode mode, DateTime? reportDate = null, ThermalReportDetail? thermalDetail = null, HydraulicsReportDetail? hydraulicsDetail = null)
            {
                LastMode = mode;
                return new CalculationReportData
                {
                    Mode = mode,
                    ReportDate = reportDate ?? DateTime.MinValue,
                    Methodology = "Расчёт по методике REHAU",
                    ProjectSection = new ProjectSection
                    {
                        ProjectNumber = project.ProjectNumber,
                        ProjectObject = project.ProjectObject
                    },
                    ClimateSection = new ClimateSection(),
                    ConstructionSection = new ConstructionSection(),
                    ThermalSection = new ThermalSection(),
                    HydraulicsSection = new HydraulicsSection(),
                    EquipmentSection = new EquipmentSection(),
                    Warnings = new System.Collections.Generic.List<CalculationReportWarning>(),
                    SourcesAppendix = new SourcesAppendix(),
                    FormulasAppendix = new FormulasAppendix()
                };
            }
        }

        private sealed class CountingBuilder : ICalculationReportDataBuilder
        {
            public int BuildCount { get; private set; }

            public CalculationReportData Build(ProjectData project, CalculationReportMode mode, DateTime? reportDate = null, ThermalReportDetail? thermalDetail = null, HydraulicsReportDetail? hydraulicsDetail = null)
            {
                BuildCount++;
                return new CalculationReportData
                {
                    Mode = mode,
                    ReportDate = reportDate ?? DateTime.MinValue,
                    Methodology = "Расчёт по методике REHAU",
                    ProjectSection = new ProjectSection
                    {
                        ProjectNumber = project.ProjectNumber,
                        ProjectObject = project.ProjectObject
                    },
                    ClimateSection = new ClimateSection(),
                    ConstructionSection = new ConstructionSection(),
                    ThermalSection = new ThermalSection(),
                    HydraulicsSection = new HydraulicsSection(),
                    EquipmentSection = new EquipmentSection(),
                    Warnings = new System.Collections.Generic.List<CalculationReportWarning>(),
                    SourcesAppendix = new SourcesAppendix(),
                    FormulasAppendix = new FormulasAppendix()
                };
            }
        }

        private sealed class CountingRenderer : ICalculationReportMarkdownRenderer
        {
            public int RenderCount { get; private set; }

            public string Render(CalculationReportData data)
            {
                RenderCount++;
                return "# report";
            }
        }

        private sealed class PassthroughRenderer : ICalculationReportMarkdownRenderer
        {
            public CalculationReportData? LastData { get; private set; }

            public string Render(CalculationReportData data)
            {
                LastData = data;
                return $"Mode: {data.Mode}\n# Детальный расчётный отчёт";
            }
        }
    }
}
