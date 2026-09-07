using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Фокусные тесты рендерера детального расчётного отчёта в Markdown.
    /// </summary>
    [TestFixture]
    public class CalculationReportMarkdownRendererTests
    {
        private static readonly DateTime FixedReportDate = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Render_MinimalReport_ContainsRequiredHeadings()
        {
            var data = CreateMinimalReport();
            var markdown = Render(data);

            Assert.That(markdown, Does.Contain("# Детальный расчётный отчёт"));
            Assert.That(markdown, Does.Contain("## Методика"));
            Assert.That(markdown, Does.Contain("## Краткая сводка"));
            Assert.That(markdown, Does.Contain("## Исходные данные проекта"));
            Assert.That(markdown, Does.Contain("## Климатические данные"));
            Assert.That(markdown, Does.Contain("## Конструкция"));
            Assert.That(markdown, Does.Contain("## Теплотехнический расчёт"));
            Assert.That(markdown, Does.Contain("## Гидравлический расчёт"));
            Assert.That(markdown, Does.Contain("## Оборудование и KPI"));
            Assert.That(markdown, Does.Contain("## Предупреждения и ограничения"));
            Assert.That(markdown, Does.Contain("## Приложение: источники значений"));
            Assert.That(markdown, Does.Contain("## Приложение: формулы и обозначения"));
        }

        [Test]
        public void Render_OperatingMode_LabelIsRussianOperating()
        {
            var data = CreateMinimalReport(mode: CalculationReportMode.Operating);
            var markdown = Render(data);

            Assert.That(markdown, Does.Contain("**Режим отчёта:** Рабочий режим"));
            Assert.That(markdown, Does.Contain("| Выбранный режим отчёта | Рабочий режим | - | Derived |"));
        }

        [Test]
        public void Render_DesignColdMode_LabelIsRussianDesignCold()
        {
            var data = CreateMinimalReport(mode: CalculationReportMode.DesignCold);
            var markdown = Render(data);

            Assert.That(markdown, Does.Contain("**Режим отчёта:** Расчётный/холодный режим"));
            Assert.That(markdown, Does.Contain("| Выбранный режим отчёта | Расчётный/холодный режим | - | Derived |"));
        }

        [Test]
        public void Render_ContainsExactMethodologyQuote()
        {
            var data = CreateMinimalReport();
            var markdown = Render(data);

            var expected = "Расчётные данные приведены по внутренней методике REHAU, реализованной в приложении SnowMeltingCalculator. " +
                           "Отчёт не заявляет соответствие ГОСТ/СП, если конкретный источник данных явно не указывает такой источник.";
            Assert.That(markdown, Does.Contain(expected));
        }

        [Test]
        public void Render_DoesNotContainUnsupportedGostSpClaim()
        {
            var data = CreateMinimalReport();
            var markdown = Render(data);

            Assert.That(markdown, Does.Contain("Отчёт не заявляет соответствие ГОСТ/СП"));
            Assert.That(markdown, Does.Not.Match(".*соответствие\\s+ГОСТ/СП.*(сертифицирован|подтвержден|заявляем|обеспечивает|соответствует).*"));
        }

        [Test]
        public void Render_NoWarnings_EmitsExactSentinel()
        {
            var data = CreateMinimalReport();
            var markdown = Render(data);

            Assert.That(markdown, Does.Contain("Предупреждения по доступным данным проекта и программы не сформированы."));
        }

        [Test]
        public void Render_WithWarnings_RendersWarningTable()
        {
            var data = CreateMinimalReport(warnings: new List<CalculationReportWarning>
            {
                new CalculationReportWarning
                {
                    Code = "MISSING_CIRCUIT_RESULT",
                    Severity = "Warning",
                    Message = "Missing selected-mode result",
                    SourcePath = "Hydraulics.Collectors[0].Circuits[0].OperatingResult",
                    RelatedValues = new List<string> { "OperatingResult", "DesignResult" }
                }
            });
            var markdown = Render(data);

            Assert.That(markdown, Does.Contain("| Код | Уровень | Сообщение | Путь | Связанные значения |"));
            Assert.That(markdown, Does.Contain("| MISSING_CIRCUIT_RESULT | Warning | Missing selected-mode result | Hydraulics.Collectors[0].Circuits[0].OperatingResult | OperatingResult, DesignResult |"));
            Assert.That(markdown, Does.Not.Contain("Предупреждения по доступным данным проекта и программы не сформированы."));
        }

        [Test]
        public void Render_MissingNumericValue_RendersNoData()
        {
            var data = CreateMinimalReport();
            var markdown = Render(data);

            Assert.That(markdown, Does.Contain("нет данных"));
        }

        [Test]
        public void Render_Deterministic_IdenticalInputProducesSameOutput()
        {
            var data = CreateFullReport();
            var first = Render(data);
            var second = Render(data);

            Assert.That(Encoding.UTF8.GetBytes(first), Is.EqualTo(Encoding.UTF8.GetBytes(second)));
        }

        [Test]
        public void Render_HydraulicsPressureColumns_PreserveOriginalUnits()
        {
            var data = CreateFullReport();
            var markdown = Render(data);
            var lines = markdown.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var circuitLines = lines.Where(l => l.StartsWith("| 1 |", StringComparison.Ordinal)).ToList();

            Assert.That(circuitLines, Is.Not.Empty);
            foreach (var line in circuitLines)
            {
                var cells = line.Split('|').Select(c => c.Trim()).ToList();
                Assert.That(cells.Count, Is.GreaterThan(16));
                Assert.That(cells[11], Does.Contain("кПа"));
                Assert.That(cells[12], Does.Contain("кПа"));
                Assert.That(cells[13], Does.Contain("кПа"));
                Assert.That(cells[14], Does.Contain("кПа"));
                Assert.That(cells[15], Does.Contain("кПа"));
            }
        }

        [Test]
        public void Render_HydraulicsPressureColumns_MixedUnits_DoesNotRelabel()
        {
            var data = CreateFullReport();
            var mixed = new ReportCircuit
            {
                CircuitNumber = 2,
                CircuitLength = ReportValueFactory.Create(100.0, "м", ReportValueSource.Project, "Circuit.Length"),
                CircuitArea = ReportValueFactory.Create(50.0, "м²", ReportValueSource.Calculated, "Circuit.Area"),
                Power = ReportValueFactory.Create(15000.0, "Вт", ReportValueSource.Calculated, "Circuit.Power"),
                FlowRate = ReportValueFactory.Create(1200.0, "л/ч", ReportValueSource.Calculated, "Circuit.FlowRate"),
                Velocity = ReportValueFactory.Create(0.8, "м/с", ReportValueSource.Calculated, "Circuit.Velocity"),
                ReynoldsNumber = ReportValueFactory.Create(15000.0, "-", ReportValueSource.Calculated, "Circuit.Reynolds"),
                FrictionFactor = ReportValueFactory.Create(0.03, "-", ReportValueSource.Calculated, "Circuit.FrictionFactor"),
                FlowRegime = ReportValueFactory.Create("Турбулентный", "-", ReportValueSource.Calculated, "Circuit.FlowRegime"),
                PressureLossPerMeter = ReportValueFactory.Create(150.0, "Па/м", ReportValueSource.Calculated, "Circuit.PressureLossPerMeter"),
                DpRohr = ReportValueFactory.Create(15000.0, "Па", ReportValueSource.Calculated, "Circuit.DpRohr"),
                DpVerteiler = ReportValueFactory.Create(5.0, "кПа", ReportValueSource.Calculated, "Circuit.DpVerteiler"),
                DpVent = ReportValueFactory.Create(2.0, "бар", ReportValueSource.Calculated, "Circuit.DpVent"),
                DpGesamt = ReportValueFactory.Create(22.0, "кПа", ReportValueSource.Calculated, "Circuit.DpGesamt"),
                Throttling = ReportValueFactory.Create(3.0, "бар", ReportValueSource.Calculated, "Circuit.Throttling"),
                ZuDrosseln = ReportValueFactory.Create(3.0, "бар", ReportValueSource.Calculated, "Circuit.ZuDrosseln"),
                ValveTurns = ReportValueFactory.Create(1.5, "об.", ReportValueSource.Calculated, "Circuit.ValveTurns")
            };
            var collector = data.HydraulicsSection.Collectors.First();
            var collectorWithMixed = new ReportCollector
            {
                Number = collector.Number,
                Type = collector.Type,
                Circuits = collector.Circuits.Append(mixed).ToList(),
                Summary = collector.Summary
            };
            data = new CalculationReportData
            {
                Mode = data.Mode,
                ReportDate = data.ReportDate,
                Methodology = data.Methodology,
                ProjectSection = data.ProjectSection,
                ClimateSection = data.ClimateSection,
                ConstructionSection = data.ConstructionSection,
                ThermalSection = data.ThermalSection,
                HydraulicsSection = new HydraulicsSection
                {
                    GlycolType = data.HydraulicsSection.GlycolType,
                    GlycolConcentration = data.HydraulicsSection.GlycolConcentration,
                    Density = data.HydraulicsSection.Density,
                    SpecificHeat = data.HydraulicsSection.SpecificHeat,
                    KinematicViscosity = data.HydraulicsSection.KinematicViscosity,
                    Collectors = new List<ReportCollector> { collectorWithMixed }
                },
                EquipmentSection = data.EquipmentSection,
                Warnings = data.Warnings,
                SourcesAppendix = data.SourcesAppendix,
                FormulasAppendix = data.FormulasAppendix
            };

            var markdown = Render(data);
            var lines = markdown.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var circuitLine = lines.Single(l => l.StartsWith("| 2 |", StringComparison.Ordinal));
            var cells = circuitLine.Split('|').Select(c => c.Trim()).ToList();
            Assert.That(cells.Count, Is.GreaterThan(16));
            Assert.That(cells[11], Does.Contain("Па").And.Not.Contain("кПа"));
            Assert.That(cells[12], Does.Contain("кПа"));
            Assert.That(cells[13], Does.Contain("бар"));
            Assert.That(cells[14], Does.Contain("кПа"));
            Assert.That(cells[15], Does.Contain("бар"));
        }

        [Test]
        public void Render_FormulasAppendix_DeduplicatesAndGroupsBySection()
        {
            var data = CreateFullReport();
            var markdown = Render(data);

            Assert.That(markdown, Does.Contain("### Thermal"));
            Assert.That(markdown, Does.Contain("| PowerUp | Q_вверх | ThermalCalculator.cs | - |"));

            var first = markdown.IndexOf("| PowerUp | Q_вверх | ThermalCalculator.cs | - |", StringComparison.Ordinal);
            var last = markdown.LastIndexOf("| PowerUp | Q_вверх | ThermalCalculator.cs | - |", StringComparison.Ordinal);
            Assert.That(first, Is.EqualTo(last));
        }

        [Test]
        public void Render_UnconfirmedFormula_RendersMvpSentinel()
        {
            var data = CreateFullReport();
            var markdown = Render(data);

            Assert.That(markdown, Does.Contain("не включена в MVP"));
        }

        [Test]
        public void Render_SourcesAppendix_ContainsMetadataEntries()
        {
            var data = CreateFullReport();
            var markdown = Render(data);

            Assert.That(markdown, Does.Contain("## Приложение: источники значений"));
            Assert.That(markdown, Does.Contain("| Путь | Название | Обозначение | Физический смысл | Единица | Источник | Деталь источника | Формула | Источник формулы | Где рассчитывается | Где используется |"));
            Assert.That(markdown, Does.Contain("| ClimateInput.City | Город | City | Местоположение объекта | - | UserInput | ClimateInput.City | - | - | - | Climate |"));
        }

        [Test]
        public void Render_SourcesAppendix_RendersFormulaAndStatus()
        {
            var data = CreateFullReport();
            var markdown = Render(data);

            Assert.That(markdown, Does.Contain("Формула | Источник формулы | Где рассчитывается | Где используется |"));
        }

        [Test]
        public void Render_NullData_ThrowsArgumentNullException()
        {
            var renderer = new CalculationReportMarkdownRenderer();
            Assert.That(() => renderer.Render(null!), Throws.ArgumentNullException);
        }

        private static string Render(CalculationReportData data)
        {
            var renderer = new CalculationReportMarkdownRenderer();
            return renderer.Render(data);
        }

        private static CalculationReportData CreateMinimalReport(
            CalculationReportMode mode = CalculationReportMode.Operating,
            List<CalculationReportWarning>? warnings = null)
        {
            return new CalculationReportData
            {
                Mode = mode,
                ReportDate = FixedReportDate,
                Methodology = "Расчёт по методике REHAU",
                ProjectSection = new ProjectSection
                {
                    ProjectNumber = "P-001",
                    ProjectObject = "Тестовая площадка"
                },
                ClimateSection = new ClimateSection(),
                ConstructionSection = new ConstructionSection(),
                ThermalSection = new ThermalSection(),
                HydraulicsSection = new HydraulicsSection(),
                EquipmentSection = new EquipmentSection(),
                Warnings = warnings ?? new List<CalculationReportWarning>(),
                SourcesAppendix = new SourcesAppendix(),
                FormulasAppendix = new FormulasAppendix()
            };
        }

        private static CalculationReportData CreateFullReport()
        {
            var collector = new ReportCollector
            {
                Number = 1,
                Type = "HKV-D",
                Circuits = new List<ReportCircuit>
                {
                    new ReportCircuit
                    {
                        CircuitNumber = 1,
                        CircuitLength = ReportValueFactory.Create(100.0, "м", ReportValueSource.Project, "Circuit.Length"),
                        CircuitArea = ReportValueFactory.Create(50.0, "м²", ReportValueSource.Calculated, "Circuit.Area"),
                        Power = ReportValueFactory.Create(15000.0, "Вт", ReportValueSource.Calculated, "Circuit.Power"),
                        FlowRate = ReportValueFactory.Create(1200.0, "л/ч", ReportValueSource.Calculated, "Circuit.FlowRate"),
                        Velocity = ReportValueFactory.Create(0.8, "м/с", ReportValueSource.Calculated, "Circuit.Velocity"),
                        ReynoldsNumber = ReportValueFactory.Create(15000.0, "-", ReportValueSource.Calculated, "Circuit.Reynolds"),
                        FrictionFactor = ReportValueFactory.Create(0.03, "-", ReportValueSource.Calculated, "Circuit.FrictionFactor"),
                        FlowRegime = ReportValueFactory.Create("Турбулентный", "-", ReportValueSource.Calculated, "Circuit.FlowRegime"),
                        PressureLossPerMeter = ReportValueFactory.Create(150.0, "Па/м", ReportValueSource.Calculated, "Circuit.PressureLossPerMeter"),
                        DpRohr = ReportValueFactory.Create(15000.0, "кПа", ReportValueSource.Calculated, "Circuit.DpRohr"),
                        DpVerteiler = ReportValueFactory.Create(5000.0, "кПа", ReportValueSource.Calculated, "Circuit.DpVerteiler"),
                        DpVent = ReportValueFactory.Create(2000.0, "кПа", ReportValueSource.Calculated, "Circuit.DpVent"),
                        DpGesamt = ReportValueFactory.Create(22000.0, "кПа", ReportValueSource.Calculated, "Circuit.DpGesamt"),
                        Throttling = ReportValueFactory.Create(3000.0, "кПа", ReportValueSource.Calculated, "Circuit.Throttling"),
                        ZuDrosseln = ReportValueFactory.Create(3000.0, "кПа", ReportValueSource.Calculated, "Circuit.ZuDrosseln"),
                        ValveTurns = ReportValueFactory.Create(1.5, "об.", ReportValueSource.Calculated, "Circuit.ValveTurns")
                    }
                },
                Summary = new ReportCollectorSummary
                {
                    CollectorType = ReportValueFactory.Create("HKV-D 9", "-", ReportValueSource.Project, "Collector.Type"),
                    CircuitCount = ReportValueFactory.Create(1.0, "-", ReportValueSource.Project, "Collector.CircuitCount"),
                    TotalPipeLength = ReportValueFactory.Create(100.0, "м", ReportValueSource.Calculated, "Collector.TotalPipeLength"),
                    TotalPower = ReportValueFactory.Create(15000.0, "Вт", ReportValueSource.Calculated, "Collector.TotalPower"),
                    TotalFlowRate = ReportValueFactory.Create(1200.0, "л/ч", ReportValueSource.Calculated, "Collector.TotalFlowRate"),
                    PressureLoss = ReportValueFactory.Create(22000.0, "кПа", ReportValueSource.Calculated, "Collector.PressureLoss"),
                    Kv = ReportValueFactory.Create(4.0, "м³/ч", ReportValueSource.Calculated, "Collector.Kv")
                }
            };

            return new CalculationReportData
            {
                Mode = CalculationReportMode.Operating,
                ReportDate = FixedReportDate,
                Methodology = "Расчёт по методике REHAU",
                ProjectSection = new ProjectSection
                {
                    ProjectNumber = "P-001",
                    ProjectObject = "Тестовая площадка"
                },
                ClimateSection = new ClimateSection
                {
                    City = ReportValueFactory.Create("Москва", "-", ReportValueSource.UserInput, "ClimateInput.City"),
                    AirTemperature = ReportValueFactory.Create(-28.0, "°C", ReportValueSource.ProgramDatabase, "ClimateDb.Temperature"),
                    WindSpeed = ReportValueFactory.Create(3.5, "м/с", ReportValueSource.UserInput, "ClimateInput.WindSpeed")
                },
                ConstructionSection = new ConstructionSection
                {
                    GroundwaterLevel = ReportValueFactory.Create(1.5, "м", ReportValueSource.UserInput, "ConstructionInput.GroundwaterLevel"),
                    R1 = ReportValueFactory.Create(0.1, "м²·К/Вт", ReportValueSource.Calculated, "Construction.R1"),
                    R2 = ReportValueFactory.Create(0.2, "м²·К/Вт", ReportValueSource.Calculated, "Construction.R2"),
                    LambdaE = ReportValueFactory.Create(1.5, "Вт/(м·К)", ReportValueSource.Calculated, "Construction.LambdaE")
                },
                ThermalSection = new ThermalSection
                {
                    PowerUp = ReportValueFactory.Create(275.0, "Вт/м²", ReportValueSource.Calculated, "ThermalCalculationResult.PowerUp", formula: "Q_вверх")
                },
                HydraulicsSection = new HydraulicsSection
                {
                    GlycolType = ReportValueFactory.Create("Этиленгликоль", "-", ReportValueSource.UserInput, "HydraulicsInput.GlycolType"),
                    GlycolConcentration = ReportValueFactory.Create(30.0, "%", ReportValueSource.UserInput, "HydraulicsInput.GlycolConcentration"),
                    Collectors = new List<ReportCollector> { collector }
                },
                EquipmentSection = new EquipmentSection
                {
                    TotalThermalPower = ReportValueFactory.Create(15000.0, "Вт", ReportValueSource.Calculated, "Equipment.TotalThermalPower"),
                    PumpFlowRate = ReportValueFactory.Create(1200.0, "л/ч", ReportValueSource.Calculated, "Equipment.PumpFlowRate"),
                    PumpHead = ReportValueFactory.Create(22.0, "кПа", ReportValueSource.Calculated, "Equipment.PumpHead"),
                    TotalPipeLength = ReportValueFactory.Create(100.0, "м", ReportValueSource.Calculated, "Equipment.TotalPipeLength"),
                    RzsCount = ReportValueFactory.Create(1.0, "-", ReportValueSource.Calculated, "Equipment.RzsCount")
                },
                Warnings = new List<CalculationReportWarning>(),
                SourcesAppendix = new SourcesAppendix
                {
                    Entries = new List<ReportParameterMetadata>
                    {
                        new ReportParameterMetadata
                        {
                            Name = "Город",
                            Symbol = "City",
                            PhysicalMeaning = "Местоположение объекта",
                            Unit = "-",
                            Source = ReportValueSource.UserInput,
                            SourceDetail = "ClimateInput.City",
                            WhereUsed = "Climate"
                        }
                    }
                },
                FormulasAppendix = new FormulasAppendix
                {
                    Formulas = new List<ReportFormula>
                    {
                        new ReportFormula
                        {
                            Symbol = "PowerUp",
                            Expression = "Q_вверх",
                            SourcePath = "ThermalCalculator.cs",
                            Section = "Thermal"
                        },
                        new ReportFormula
                        {
                            Symbol = "Unconfirmed",
                            Expression = "",
                            SourcePath = "-",
                            Section = "Thermal",
                            FormulaStatus = "требуется привязка к существующей формуле"
                        }
                    }
                }
            };
        }
    }
}
