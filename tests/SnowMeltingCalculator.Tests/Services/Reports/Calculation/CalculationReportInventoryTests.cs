using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Фокусные тесты приложений источников и формул детального отчёта.
    /// </summary>
    [TestFixture]
    public class CalculationReportInventoryTests
    {
        private static readonly DateTime FixedDate = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Build_SourcesAppendix_ContainsExpectedParameters()
        {
            var project = CreateProjectWithResults();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var names = report.SourcesAppendix.Entries.Select(e => e.Name).ToList();

            // Project
            Assert.That(names, Does.Contain("Номер проекта"));
            Assert.That(names, Does.Contain("Наименование объекта"));

            // Climate
            Assert.That(names, Does.Contain("Город"));
            Assert.That(names, Does.Contain("Расчётная температура воздуха"));
            Assert.That(names, Does.Contain("Температура подачи"));

            // Construction
            Assert.That(names, Does.Contain("Уровень грунтовых вод"));
            Assert.That(names, Does.Contain("Сопротивление над трубой"));
            Assert.That(names, Does.Contain("Сопротивление под трубой"));

            // Thermal
            Assert.That(names, Does.Contain("Коэффициент теплоотдачи"));
            Assert.That(names, Does.Contain("Мощность на плавление снега"));
            Assert.That(names, Does.Contain("Полезная мощность вверх"));
            Assert.That(names, Does.Contain("Плотность снега"));

            // Hydraulics
            Assert.That(names, Does.Contain("Тип гликоля"));
            Assert.That(names, Does.Contain("Длина греющего участка"));
            Assert.That(names, Does.Contain("Расход теплоносителя"));

            // Equipment
            Assert.That(names, Does.Contain("Суммарная тепловая мощность"));
            Assert.That(names, Does.Contain("Расход насоса"));
            Assert.That(names, Does.Contain("Объём системы"));
        }

        [Test]
        public void Build_FormulasAppendix_ContainsUniqueConfirmedFormulas()
        {
            var project = CreateProjectWithResults();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var symbols = report.FormulasAppendix.Formulas.Select(f => f.Symbol).ToList();

            Assert.That(symbols, Does.Contain("alpha"));
            Assert.That(symbols, Does.Contain("Q_таяние"));
            Assert.That(symbols, Does.Contain("PowerUp"));
            Assert.That(symbols, Does.Contain("RFb"));
            Assert.That(symbols, Does.Contain("RD"));
            Assert.That(symbols, Does.Contain("m"));
            Assert.That(symbols, Does.Contain("etaR"));
            Assert.That(symbols, Does.Contain("JHmu"));
            Assert.That(symbols, Does.Contain("PowerDown"));
            Assert.That(symbols, Does.Contain("TotalPowerDensity"));
            Assert.That(symbols, Does.Contain("DeltaT"));

            // Uniqueness: no duplicate symbols in the same section.
            var duplicates = report.FormulasAppendix.Formulas
                .GroupBy(f => new { f.Symbol, f.Section })
                .Where(g => g.Count() > 1)
                .ToList();
            Assert.That(duplicates, Is.Empty);
        }

        [Test]
        public void Build_SourcesAppendix_ConditionalParameters_MarkedNotCalculated()
        {
            var project = CreateProjectWithResults();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var radiationHeat = report.SourcesAppendix.Entries
                .SingleOrDefault(e => e.Symbol == "Q_изл");
            Assert.That(radiationHeat, Is.Not.Null);
            Assert.That(radiationHeat!.Formula, Is.EqualTo("справочно, не включается в PowerUp"));

            // P4/ADR-013: Pr — реальная величина раздела (FormulaStatusUnconfirmed снят).
            var pr = report.SourcesAppendix.Entries
                .SingleOrDefault(e => e.Symbol == "Pr");
            Assert.That(pr, Is.Not.Null);
            Assert.That(pr!.Formula, Is.Null,
                "Pr больше не условный источник — значение выводится в разделе «Гидравлический расчёт»");
        }

        [Test]
        public void Build_SourcesAppendix_ColdPeriodDays_NotPresentedAsNormalResult()
        {
            var project = CreateProjectWithResults();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var coldPeriod = report.SourcesAppendix.Entries
                .SingleOrDefault(e => e.Name == "Холодный период");
            Assert.That(coldPeriod, Is.Not.Null);
            Assert.That(coldPeriod!.Formula, Is.EqualTo("условно: данные отсутствуют в ProjectData"));
        }

        [Test]
        public void Build_SourcesAppendix_UnavailableParameters_MarkedAsUnavailable()
        {
            var project = CreateProjectWithResults();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var freezing = report.SourcesAppendix.Entries
                .SingleOrDefault(e => e.Name == "Температура замерзания");
            Assert.That(freezing, Is.Not.Null);
            Assert.That(freezing!.Formula, Is.EqualTo("недоступно в текущем коде"));
        }

        private static ProjectData CreateProjectWithResults()
        {
            return new ProjectData
            {
                ProjectNumber = "P-INV",
                ProjectObject = "Инвентаризация параметров",
                ClimateData = new ClimateProjectData
                {
                    SelectedCity = "Москва",
                    Region = "Московская область",
                    AirTemperature = -28.0,
                    WindSpeed = 3.5,
                    Humidity = 85.0,
                    SnowfallIntensity = 0.5,
                    SelectedZone = ClimateZone.Zone_M20
                },
                ConstructionData = new ConstructionProjectData
                {
                    GroundwaterLevel = 2.0,
                    R1 = 0.05,
                    R2 = 0.02,
                    LambdaE = 1.6,
                    Layers = new List<LayerProjectData>
                    {
                        new LayerProjectData
                        {
                            Position = LayerPosition.AbovePipe,
                            MaterialName = "Бетон",
                            MaterialLambda = 1.5,
                            CalculatedLambda = 1.5,
                            Thickness = 80.0,
                            CalculatedR = 0.05,
                            Order = 0
                        }
                    }
                },
                ThermalData = new ThermalProjectData
                {
                    SelectedMode = OperatingMode.Melting,
                    GroundTemperature = 10.0,
                    SupplyTemperature = 55.0,
                    PipeSpacing = 200,
                    SelectedPipe = new PipeTypeProjectData
                    {
                        Name = "RAUTHERM S 20x2.0",
                        OuterDiameter = 20.0,
                        InnerDiameter = 16.0,
                        WallThickness = 2.0
                    },
                    Result = new ThermalResultProjectData
                    {
                        PowerUp = 250.0,
                        PowerDown = 50.0,
                        PowerTotal = 300.0,
                        SupplyTemperature = 55.0,
                        ReturnTemperature = 40.0,
                        MeanTemperature = 47.5,
                        DeltaT = 15.0,
                        IsValid = true
                    }
                },
                HydraulicsData = new HydraulicsProjectData
                {
                    GlycolType = GlycolType.Ethylene,
                    GlycolConcentration = 50.0,
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
                                new CircuitProjectData
                                {
                                    CircuitNumber = 1,
                                    CircuitLength = 80.0,
                                    SupplyLength = 10.0,
                                    SupplySpacingCm = 5.0,
                                    SupplyHeatPercent = 10.0,
                                    PipeSpacingCm = 20.0,
                                    OperatingResult = new CircuitResultProjectData
                                    {
                                        Power = 1200.0,
                                        FlowRate = 100.0,
                                        Velocity = 0.8,
                                        DpRohr = 8000.0,
                                        DpVerteiler = 4000.0,
                                        DpVent = 3000.0,
                                        DpGesamt = 15000.0,
                                        Throttling = 5000.0,
                                        ValveTurns = 2.0,
                                        FlowRegime = "Turbulent",
                                        Density = 1.05,
                                        KinematicViscosity = 1.5,
                                        ReynoldsNumber = 8000.0,
                                        FrictionFactor = 0.03,
                                        PressureLossPerMeter = 166.67
                                    },
                                    DesignResult = new CircuitResultProjectData
                                    {
                                        Power = 2400.0,
                                        FlowRate = 200.0,
                                        Velocity = 1.2,
                                        DpRohr = 16000.0,
                                        DpVerteiler = 8000.0,
                                        DpVent = 6000.0,
                                        DpGesamt = 30000.0,
                                        Throttling = 10000.0,
                                        ValveTurns = 3.0,
                                        FlowRegime = "Turbulent",
                                        Density = 1.08,
                                        KinematicViscosity = 2.5,
                                        ReynoldsNumber = 12000.0,
                                        FrictionFactor = 0.025,
                                        PressureLossPerMeter = 333.33
                                    }
                                }
                            },
                            Summary = new CollectorSummaryProjectData
                            {
                                CircuitCount = 1,
                                TotalPipeLength = 90.0,
                                TotalPower = 1200.0,
                                TotalFlowRate = 100.0,
                                PressureLoss_Operating_Pa = 15000.0,
                                PressureLoss_Cold_Pa = 30000.0,
                                Kv = 1.2,
                                CollectorType = "HKV-D"
                            }
                        }
                    }
                }
            };
        }
    }
}
