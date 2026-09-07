using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Пин полноты точности чисел (В9, план P1, спека §7.3): в собранной
    /// модели <see cref="CalculationReportData"/> нет
    /// <see cref="ReportValue{T}"/> c <c>Value != null</c> и
    /// <c>Decimals == null</c> — каждая табличная величина и каждый
    /// «Результат» шага несут знаки по единице, назначенные билдерами.
    /// </summary>
    [TestFixture]
    public class CalculationReportDecimalsTests
    {
        private const string ReportNamespace = "SnowMeltingCalculator.Services.Reports.Calculation";

        private static readonly DateTime FixedDate = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Build_OperatingMode_EveryDoubleValueHasDecimals()
        {
            AssertEveryValueHasDecimals(CalculationReportMode.Operating);
        }

        [Test]
        public void Build_DesignColdMode_EveryDoubleValueHasDecimals()
        {
            AssertEveryValueHasDecimals(CalculationReportMode.DesignCold);
        }

        [Test]
        public void Value_ValveTurnsUnit_RendersFraction()
        {
            // План P1: обороты клапана — дробью («8», «8 ½») вместо «8,000 об»;
            // правило общее с WPF-конвертером (ValveTurnsFraction).
            Assert.Multiple(() =>
            {
                Assert.That(CalculationReportMarkdownRenderHelper.Value(
                    ReportValueFactory.Create(8.0, "об", ReportValueSource.Calculated, "CircuitResultProjectData.ValveTurns", decimals: 0)), Is.EqualTo("8"));
                Assert.That(CalculationReportMarkdownRenderHelper.Value(
                    ReportValueFactory.Create(8.5, "об", ReportValueSource.Calculated, "CircuitResultProjectData.ValveTurns", decimals: 0)), Is.EqualTo("8 ½"));
                Assert.That(CalculationReportMarkdownRenderHelper.Value(
                    ReportValueFactory.Create(2.25, "об", ReportValueSource.Calculated, "CircuitResultProjectData.ValveTurns", decimals: 0)), Is.EqualTo("2 ¼"));
                Assert.That(CalculationReportMarkdownRenderHelper.Value(
                    ReportValueFactory.Create(0.5, "об", ReportValueSource.Calculated, "CircuitResultProjectData.ValveTurns", decimals: 0)), Is.EqualTo("½"));
            });
        }

        [Test]
        public void Value_ZeroValue_NoDataMarkerOnlyWhenZeroIsInvalid()
        {
            // В2/В14: ноль → «нет данных» только при !ZeroIsValid;
            // ZeroIsValid = true (P4, дросселирование) → «0».
            Assert.Multiple(() =>
            {
                Assert.That(CalculationReportMarkdownRenderHelper.Value(
                    ReportValueFactory.Create(0.0, "Па", ReportValueSource.Calculated, "CircuitProjectData.Throttling", decimals: 0)), Is.EqualTo("нет данных"));
                Assert.That(CalculationReportMarkdownRenderHelper.Value(
                    ReportValueFactory.Create(0.0, "Па", ReportValueSource.Calculated, "CircuitProjectData.Throttling", decimals: 0, zeroIsValid: true)), Is.EqualTo("0"));
                Assert.That(CalculationReportMarkdownRenderHelper.Value(
                    new ReportValue<double> { Unit = "Па", Source = ReportValueSource.Calculated, SourceDetail = "missing", Decimals = 0 }), Is.EqualTo("нет данных"),
                    "величина без значения (0.0-заглушка) — «нет данных»");
            });
        }

        [Test]
        public void Value_DecimalsOverride_TableFormatFallback()
        {
            // В9: рендер — Decimals величины; без Decimals — формат таблицы N2.
            Assert.Multiple(() =>
            {
                Assert.That(CalculationReportMarkdownRenderHelper.Value(
                    ReportValueFactory.Create(110.0, "м", ReportValueSource.UserInput, "CircuitProjectData.CircuitLength", decimals: 1)), Is.EqualTo("110,0"));
                Assert.That(CalculationReportMarkdownRenderHelper.Value(
                    ReportValueFactory.Create(6700.0, "Вт", ReportValueSource.Calculated, "CircuitResultProjectData.Power", decimals: 0)), Is.EqualTo("6\u00A0700"));
                Assert.That(CalculationReportMarkdownRenderHelper.Value(
                    ReportValueFactory.Create(1.234, "-", ReportValueSource.Calculated, "hand-built")), Is.EqualTo("1,23"));
            });
        }

        [Test]
        public void Render_SampleProject_PerUnitDecimals_NoLegacyArtifacts()
        {
            // AC-1 (план P1): реальный прогон билдер+рендер на синтетическом
            // «Екатеринбурге» — табличные значения и «Результаты» шагов по
            // Decimals величин; артефакты «6 700,000 Вт»/«10 600,000»/«8,000 об»
            // отсутствуют. Markdown-образец пишется в каталог теста
            // (evidence ручной приёмки).
            var builder = new CalculationReportDataBuilder();
            var data = builder.Build(MakeProject(), CalculationReportMode.Operating, FixedDate, MakeDetail());
            var markdown = new CalculationReportMarkdownRenderer().Render(data);
            var samplePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "p1-sample-report-operating.md");
            File.WriteAllText(samplePath, markdown, new UTF8Encoding(false));

            var circuitRow = markdown
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Single(line => line.StartsWith("| 1 |", StringComparison.Ordinal) && line.Contains("Турбулентный", StringComparison.Ordinal));

            Assert.Multiple(() =>
            {
                // шт → 0; Вт → 0; Па → 0; м/м² → 1; л/ч и скорость → 2; Re → 0;
                // λ — N3 (не ухудшать); обороты — дробью «8».
                Assert.That(circuitRow, Is.EqualTo(
                    "| 1 | 100,0 | 20,0 | 6\u00A0700 | 320,00 | 0,44 | 10\u00A0600 | 0,031 | Турбулентный | 204 " +
                    "| 40\u00A0000 Па | 3\u00A0000 Па | 2\u00A0000 Па | 45\u00A0000 Па | нет данных Па | 8 |"),
                    "строка контура: точность по единицам §7.3, обороты дробью");
                Assert.That(markdown, Does.Contain("| Количество контуров | 1 | шт |"));
                Assert.That(markdown, Does.Contain("| Расчётная температура наружного воздуха | ProjectData.ClimateData.AirTemperature | -15,0 | °C |"));
                Assert.That(markdown, Does.Contain("| Суммарная тепловая мощность | CollectorSummaryProjectData.TotalPower | 6,70 | кВт |"));
                Assert.That(markdown, Does.Contain("| Расход насоса | CollectorSummaryProjectData.TotalFlowRate | 0,32 | м³/ч |"));
                Assert.That(markdown, Does.Contain("- Результат: **6\u00A0700 Вт**"), "«Результат» шага Q_HK — Вт → 0 знаков");
                Assert.That(markdown, Does.Contain("- Результат: **8 об**"), "обороты клапана в шаге балансировки — дробью");
                // Заглушки нехранённых величин — «нет данных» (В2), не «0,000».
                Assert.That(markdown, Does.Contain("| Плотность теплоносителя | CircuitResultProjectData.Density | нет данных |"));
                // Артефакты жёсткого N3 отсутствуют (AC-1).
                Assert.That(markdown, Does.Not.Contain("6\u00A0700,000"));
                Assert.That(markdown, Does.Not.Contain("10\u00A0600,000"));
                Assert.That(markdown, Does.Not.Contain("45\u00A0000,000"));
                Assert.That(markdown, Does.Not.Contain(",000 шт"));
                Assert.That(markdown, Does.Not.Contain(",000 об"));
                Assert.That(markdown, Does.Not.Contain(",000 Па"));
                Assert.That(markdown, Does.Not.Contain(",000 м³/ч"));
                Assert.That(markdown, Does.Not.Contain(",000 л"));
            });
        }

        [Test]
        public void Render_SampleProject_DesignCold_ModeComparisonKeepsLambdaN3()
        {
            // В9, исключение спеки §7.3: λ в сравнении режимов — явный N3
            // (значащая цифра не теряется); Re — N0; кратность — N2.
            var builder = new CalculationReportDataBuilder();
            var data = builder.Build(MakeProject(), CalculationReportMode.DesignCold, FixedDate, MakeDetail());
            var markdown = new CalculationReportMarkdownRenderer().Render(data);
            var samplePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "p1-sample-report-designcold.md");
            File.WriteAllText(samplePath, markdown, new UTF8Encoding(false));

            Assert.Multiple(() =>
            {
                Assert.That(markdown, Does.Contain("0,031"), "λ рабочий — N3");
                Assert.That(markdown, Does.Contain("0,142"), "λ пуск — N3");
                Assert.That(markdown, Does.Contain("×3,33"), "кратность — N2");
                Assert.That(markdown, Does.Contain("150\u00A0000"), "DpGesamt пуска — Па → 0 знаков");
                Assert.That(markdown, Does.Not.Contain("150\u00A0000,000"));
            });
        }

        private static void AssertEveryValueHasDecimals(CalculationReportMode mode)
        {
            var builder = new CalculationReportDataBuilder();
            var data = builder.Build(MakeProject(), mode, FixedDate, MakeDetail());

            var values = new List<ReportValue<double>>();
            CollectDoubleValues(data, values, new HashSet<object>());

            Assert.That(values, Has.Count.GreaterThan(50),
                "обход модели должен находить табличные величины всех разделов");
            // Value у ReportValue<double> — обычный double (T? для значимого
            // типа не оборачивается в Nullable), «отсутствие» — 0.0-заглушка;
            // поэтому пин требует Decimals у каждой величины модели безусловно.
            var missing = values
                .Where(v => v.Decimals == null)
                .Select(v => $"unit='{v.Unit}', source='{v.SourceDetail}', value={v.Value}")
                .Distinct()
                .ToList();
            Assert.That(missing, Is.Empty,
                "каждая ReportValue<double> со значением должна нести Decimals (спека §7.3, план P1)");
        }

        /// <summary>
        /// Обход модели отчёта: все свойства-величины, шаги (результаты и
        /// входы), таблицы слоёв/контуров/коллекторов/спецификаций — через
        /// рефлексию по типам пространства имён отчёта.
        /// </summary>
        private static void CollectDoubleValues(object? root, List<ReportValue<double>> found, HashSet<object> visited)
        {
            if (root is null || !visited.Add(root))
            {
                return;
            }

            switch (root)
            {
                case ReportValue<double> value:
                    found.Add(value);
                    return;
                case CalculationStep step:
                    CollectDoubleValues(step.Result, found, visited);
                    foreach (var input in step.Inputs)
                    {
                        CollectDoubleValues(input, found, visited);
                    }

                    return;
                case string:
                    return;
            }

            var type = root.GetType();
            if (type.IsPrimitive || type.IsEnum || type.Namespace != ReportNamespace)
            {
                return;
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                object? child;
                try
                {
                    child = property.GetValue(root);
                }
                catch (Exception)
                {
                    continue;
                }

                switch (child)
                {
                    case IEnumerable<ReportValue<double>> doubleList:
                        foreach (var item in doubleList)
                        {
                            CollectDoubleValues(item, found, visited);
                        }

                        break;
                    case IEnumerable<object> list:
                        foreach (var item in list)
                        {
                            CollectDoubleValues(item, found, visited);
                        }

                        break;
                    default:
                        CollectDoubleValues(child, found, visited);
                        break;
                }
            }
        }

        private static ThermalReportDetail MakeDetail()
        {
            return new ThermalReportDetail
            {
                Source = ThermalReportDetailSource.Snapshot,
                Alpha = 14.13,
                MeltingHeat = 47.8,
                RadiationHeat = 320.0,
                ConvectionHeat = 282.7,
                ExcessTemperature = 60.2,
                RFb = 0.1283,
                RD = 5.6374,
                ParameterM = 9.08,
                EfficiencyEtaR = 0.793,
                MassFlowRate = 22.1,
                VolumeFlowRate = 21.62
            };
        }

        /// <summary>Синтетический проект «Екатеринбург» — тот же fixture, что
        /// в пинах PDF-рендера: оба результата контура, слои, сводка.</summary>
        private static ProjectData MakeProject()
        {
            return new ProjectData
            {
                ProjectNumber = "9-100000",
                ProjectObject = "Екатеринбург",
                ClimateData = new ClimateProjectData
                {
                    SelectedCity = "Екатеринбург",
                    AirTemperature = -15.0,
                    WindSpeed = 3.1,
                    Humidity = 72.0,
                    SnowfallIntensity = 0.5
                },
                ConstructionData = new ConstructionProjectData
                {
                    GroundwaterLevel = 2.0,
                    R1 = 0.0575,
                    R2 = 5.6374,
                    LambdaE = 1.74,
                    Layers = new List<LayerProjectData>
                    {
                        new() { Position = LayerPosition.AbovePipe, MaterialName = "Бетон", Thickness = 100.0, CalculatedLambda = 1.74, CalculatedR = 0.0575 },
                        new() { Position = LayerPosition.BelowPipe, MaterialName = "Пенополистирол ЭППС", Thickness = 80.0, CalculatedLambda = 0.035, CalculatedR = 2.2857 }
                    }
                },
                ThermalData = new ThermalProjectData
                {
                    SelectedMode = OperatingMode.Melting,
                    SupplyTemperature = 53.0,
                    GroundTemperature = 10.0,
                    PipeSpacing = 200,
                    SelectedPipe = new PipeTypeProjectData { Name = "RAUTHERM S 20x2.0", OuterDiameter = 20.0, InnerDiameter = 16.0, WallThickness = 2.0 },
                    Result = new ThermalResultProjectData
                    {
                        PowerUp = 330.5,
                        PowerDown = 4.9,
                        PowerTotal = 335.4,
                        SupplyTemperature = 53.0,
                        ReturnTemperature = 37.4,
                        MeanTemperature = 45.2,
                        DeltaT = 15.6,
                        IsValid = true
                    }
                },
                HydraulicsData = new HydraulicsProjectData
                {
                    GlycolType = GlycolType.Ethylene,
                    GlycolConcentration = 40.0,
                    Collectors = new List<CollectorProjectData>
                    {
                        new()
                        {
                            CollectorNumber = 1,
                            CollectorType = "IV 1¼\"",
                            Summary = new CollectorSummaryProjectData { PressureLoss_Operating_Pa = 45000, PressureLoss_Cold_Pa = 150000, CircuitCount = 1, TotalPipeLength = 110, TotalPower = 6700, TotalFlowRate = 320, Kv = 1.45, CollectorType = "IV 1¼\"" },
                            Circuits = new List<CircuitProjectData>
                            {
                                new()
                                {
                                    CircuitNumber = 1, CircuitLength = 100.0, SupplyLength = 10.0, PipeSpacingCm = 20, SupplySpacingCm = 5.0, SupplyHeatPercent = 10.0,
                                    OperatingResult = new CircuitResultProjectData { DpGesamt = 45000, Power = 6700, FlowRate = 320, Velocity = 0.44, ReynoldsNumber = 10600, FrictionFactor = 0.031, PressureLossPerMeter = 204, DpRohr = 40000, DpVerteiler = 3000, DpVent = 2000, Throttling = 0, ValveTurns = 8, Density = 1.053, KinematicViscosity = 0.66, FlowRegime = "Турбулентный" },
                                    DesignResult = new CircuitResultProjectData { DpGesamt = 150000, Power = 6700, FlowRate = 320, Velocity = 0.44, ReynoldsNumber = 450, FrictionFactor = 0.1422, PressureLossPerMeter = 680, DpRohr = 140000, DpVerteiler = 5000, DpVent = 5000, Throttling = 0, ValveTurns = 8, Density = 1.053, KinematicViscosity = 15.64, FlowRegime = "Ламинарный" }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
