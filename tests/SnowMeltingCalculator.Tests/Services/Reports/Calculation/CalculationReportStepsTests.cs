using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Reports.Calculation;

namespace SnowMeltingCalculator.Tests.Services.Reports.Calculation
{
    /// <summary>
    /// T2-06/T2-10/T2-13: пошаговое содержимое теплового раздела, выбор
    /// референсного контура и missing-data гидравлики.
    /// </summary>
    [TestFixture]
    public class CalculationReportStepsTests
    {
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
                        new() { Position = LayerPosition.BelowPipe, MaterialName = "Пенополистирол ЭППС", Thickness = 80.0, CalculatedLambda = 0.035, CalculatedR = 2.2857 },
                        new() { Position = LayerPosition.BelowPipe, MaterialName = "Грунт", Thickness = 1000.0, CalculatedLambda = 0.5, CalculatedR = 2.0 }
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
                HydraulicsData = new HydraulicsProjectData()
            };
        }

        [Test]
        public void Build_ThermalSteps_Present_WithoutZeros()
        {
            // T2-06: с деталями — 15 шагов, ни один результат не нулевой,
            // подстановки непустые.
            var builder = new CalculationReportDataBuilder();
            var data = builder.Build(MakeProject(), CalculationReportMode.Operating, thermalDetail: MakeDetail());

            var steps = data.ThermalSection.Steps;

            Assert.Multiple(() =>
            {
                Assert.That(data.ThermalSection.IsDetailAvailable, Is.True);
                Assert.That(steps, Has.Count.EqualTo(15));
                Assert.That(steps.Select(s => s.Result.Value), Has.None.EqualTo(0.0));
                Assert.That(steps, Has.All.Property("SubstitutionText").Not.Empty);
                Assert.That(steps.First(s => s.Key == "thermal.alpha").SubstitutionText, Does.Contain("2,26"));
                Assert.That(steps.First(s => s.Key == "thermal.jhmu").Note, Does.Contain("A–E"));
            });
        }

        [Test]
        public void Build_ThermalConstants_Present()
        {
            var builder = new CalculationReportDataBuilder();
            var data = builder.Build(MakeProject(), CalculationReportMode.Operating, thermalDetail: MakeDetail());

            var constants = data.ThermalSection.Constants;

            Assert.Multiple(() =>
            {
                Assert.That(constants, Has.Count.EqualTo(10));
                Assert.That(constants.First(c => c.Symbol == "ρ_снега").Value, Is.EqualTo(900.0));
                Assert.That(constants.First(c => c.Symbol == "L_плавл").Value, Is.EqualTo(330000.0));
                Assert.That(constants.First(c => c.Symbol == "ε_тр").Value, Is.EqualTo(0.007));
            });
        }

        [Test]
        public void Build_ConstructionSteps_R1R2Substitutions()
        {
            var builder = new CalculationReportDataBuilder();
            var data = builder.Build(MakeProject(), CalculationReportMode.Operating);

            var steps = data.ConstructionSection.Steps;

            Assert.Multiple(() =>
            {
                Assert.That(steps, Has.Count.EqualTo(2));
                var r1 = steps.First(s => s.Key == "construction.r1");
                Assert.That(r1.SubstitutionText, Does.Contain("0,100/1,74"));
                Assert.That(r1.SubstitutionText, Does.Contain("0,0575"));
                Assert.That(data.ConstructionSection.LambdaRuleNote, Does.Contain("λА (сухие условия)"));
            });
        }

        [Test]
        public void Build_ReferenceCircuit_WorstCollectorWorstCircuit_WithTieBreak()
        {
            // T2-10: худший коллектор — №2 (потери 500 Па > 400 Па);
            // у него два контура с равными DpGesamt → референс — минимальный номер.
            var project = MakeProject();
            project.HydraulicsData = new HydraulicsProjectData
            {
                GlycolType = GlycolType.Ethylene,
                GlycolConcentration = 40.0,
                Collectors = new List<CollectorProjectData>
                {
                    new()
                    {
                        CollectorNumber = 1,
                        CollectorType = "IV 1¼\"",
                        Summary = new CollectorSummaryProjectData { PressureLoss_Operating_Pa = 400.0, PressureLoss_Cold_Pa = 900.0 },
                        Circuits = new List<CircuitProjectData>
                        {
                            new() { CircuitNumber = 1, CircuitLength = 90.0, SupplyLength = 10.0, PipeSpacingCm = 20, OperatingResult = new CircuitResultProjectData { DpGesamt = 400.0, Power = 6000, FlowRate = 300, Velocity = 0.4, ReynoldsNumber = 9000, FrictionFactor = 0.032, PressureLossPerMeter = 200, DpRohr = 20000, DpVerteiler = 1000, DpVent = 4000, Throttling = 0, ValveTurns = 8 } }
                        }
                    },
                    new()
                    {
                        CollectorNumber = 2,
                        CollectorType = "IV 1¼\"",
                        Summary = new CollectorSummaryProjectData { PressureLoss_Operating_Pa = 500.0, PressureLoss_Cold_Pa = 1000.0 },
                        Circuits = new List<CircuitProjectData>
                        {
                            new() { CircuitNumber = 1, CircuitLength = 100.0, SupplyLength = 10.0, PipeSpacingCm = 20, OperatingResult = new CircuitResultProjectData { DpGesamt = 500.0, Power = 6700, FlowRate = 320, Velocity = 0.44, ReynoldsNumber = 10600, FrictionFactor = 0.031, PressureLossPerMeter = 204, DpRohr = 400, DpVerteiler = 50, DpVent = 50, Throttling = 0, ValveTurns = 8 } },
                            new() { CircuitNumber = 2, CircuitLength = 100.0, SupplyLength = 10.0, PipeSpacingCm = 20, OperatingResult = new CircuitResultProjectData { DpGesamt = 500.0, Power = 6700, FlowRate = 320, Velocity = 0.44, ReynoldsNumber = 10600, FrictionFactor = 0.031, PressureLossPerMeter = 204, DpRohr = 400, DpVerteiler = 50, DpVent = 50, Throttling = 500, ValveTurns = 4.5 } }
                        }
                    }
                }
            };

            var builder = new CalculationReportDataBuilder();
            var data = builder.Build(project, CalculationReportMode.Operating, thermalDetail: MakeDetail());

            var reference = data.HydraulicsSection.ReferenceCircuit;

            Assert.Multiple(() =>
            {
                Assert.That(reference, Is.Not.Null);
                Assert.That(reference!.CollectorNumber, Is.EqualTo(2));
                Assert.That(reference.CircuitNumber, Is.EqualTo(1), "при ничьей — минимальный номер контура");
                Assert.That(reference.Steps, Has.Count.EqualTo(10));
                Assert.That(reference.BalancingSteps, Has.Count.EqualTo(3));
                Assert.That(reference.Steps.First(s => s.Key == "hyd.ref.dpgesamt").SubstitutionText, Does.Contain("400 + 50 + 50 = 500"));
                Assert.That(reference.BalancingNote, Does.Contain("IV"));
            });
        }

        [Test]
        public void Render_Deterministic_TwoRunsIdentical()
        {
            // T2-08: два рендера подряд — байт-в-байт одинаковый вывод.
            var renderer = new CalculationReportMarkdownRenderer();
            var builder = new CalculationReportDataBuilder();
            var data = builder.Build(MakeProject(), CalculationReportMode.Operating, new DateTime(2026, 9, 7, 12, 0, 0), MakeDetail());

            var first = renderer.Render(data);
            var second = renderer.Render(data);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Does.Contain("Пошаговый расчёт"));
        }

        [Test]
        public void Render_OperatingMode_FullThermalSteps_AndReferenceCircuit()
        {
            // T2-09 (Operating): полный пошаговый расчёт + референсный контур.
            var project = MakeProject();
            project.HydraulicsData = new HydraulicsProjectData
            {
                Collectors = new List<CollectorProjectData>
                {
                    new()
                    {
                        CollectorNumber = 1,
                        CollectorType = "IV 1¼\"",
                        Summary = new CollectorSummaryProjectData { PressureLoss_Operating_Pa = 45000, PressureLoss_Cold_Pa = 150000 },
                        Circuits = new List<CircuitProjectData>
                        {
                            new() { CircuitNumber = 1, CircuitLength = 100.0, SupplyLength = 10.0, PipeSpacingCm = 20,
                                OperatingResult = new CircuitResultProjectData { DpGesamt = 45000, Power = 6700, FlowRate = 320, Velocity = 0.44, ReynoldsNumber = 10600, FrictionFactor = 0.031, PressureLossPerMeter = 204, DpRohr = 400, DpVerteiler = 50, DpVent = 50, Throttling = 0, ValveTurns = 8 },
                                DesignResult = new CircuitResultProjectData { DpGesamt = 150000, Power = 6700, FlowRate = 320, Velocity = 0.44, ReynoldsNumber = 450, FrictionFactor = 0.14, PressureLossPerMeter = 680, DpRohr = 140000, DpVerteiler = 500, DpVent = 500, Throttling = 0, ValveTurns = 8 } }
                        }
                    }
                }
            };

            var renderer = new CalculationReportMarkdownRenderer();
            var builder = new CalculationReportDataBuilder();
            var operating = renderer.Render(builder.Build(project, CalculationReportMode.Operating, thermalDetail: MakeDetail()));

            Assert.Multiple(() =>
            {
                Assert.That(operating, Does.Contain("Пошаговый расчёт"));
                Assert.That(operating, Does.Contain("Референсный контур"));
                Assert.That(operating, Does.Not.Contain("Сравнение режимов"));
                Assert.That(operating, Does.Contain("Режим отчёта:** Рабочий"));
            });
        }

        [Test]
        public void Render_DesignCold_ShortThermalSummary_AndModeComparison()
        {
            // T2-09 (DesignCold): краткая тепловая справка вместо шагов +
            // сравнение «рабочий vs пуск» (В3) + гидравлика DesignResult.
            var project = MakeProject();
            project.HydraulicsData = new HydraulicsProjectData
            {
                Collectors = new List<CollectorProjectData>
                {
                    new()
                    {
                        CollectorNumber = 1,
                        CollectorType = "IV 1¼\"",
                        Summary = new CollectorSummaryProjectData { PressureLoss_Operating_Pa = 45000, PressureLoss_Cold_Pa = 150000 },
                        Circuits = new List<CircuitProjectData>
                        {
                            new() { CircuitNumber = 1, CircuitLength = 100.0, SupplyLength = 10.0, PipeSpacingCm = 20,
                                OperatingResult = new CircuitResultProjectData { DpGesamt = 45000, Power = 6700, FlowRate = 320, Velocity = 0.44, ReynoldsNumber = 10600, FrictionFactor = 0.031, PressureLossPerMeter = 204, DpRohr = 400, DpVerteiler = 50, DpVent = 50, Throttling = 0, ValveTurns = 8 },
                                DesignResult = new CircuitResultProjectData { DpGesamt = 150000, Power = 6700, FlowRate = 320, Velocity = 0.44, ReynoldsNumber = 450, FrictionFactor = 0.14, PressureLossPerMeter = 680, DpRohr = 140000, DpVerteiler = 500, DpVent = 500, Throttling = 0, ValveTurns = 8 } }
                        }
                    }
                }
            };

            var renderer = new CalculationReportMarkdownRenderer();
            var builder = new CalculationReportDataBuilder();
            var cold = renderer.Render(builder.Build(project, CalculationReportMode.DesignCold, thermalDetail: MakeDetail()));

            Assert.Multiple(() =>
            {
                Assert.That(cold, Does.Contain("Краткая тепловая справка"));
                Assert.That(cold, Does.Not.Contain("Пошаговый расчёт"));
                Assert.That(cold, Does.Contain("Сравнение режимов"));
                Assert.That(cold, Does.Contain("×3,3"));
                Assert.That(cold, Does.Contain("150"));
                Assert.That(cold, Does.Contain("Режим отчёта:** Расчётный/холодный"));
            });
        }

        [Test]
        public void Render_WithoutDetail_MissingDataMarkerShown()
        {
            // В2: детальные величины отсутствуют (старый файл, пересчёт
            // невозможен) — маркер «нет данных» + MISSING_THERMAL_DETAIL.
            var detail = new ThermalReportDetail
            {
                Source = ThermalReportDetailSource.RecalculationInvalid,
                ValidationErrors = new[] { "Мощность вниз (потери) не может быть отрицательной." }
            };
            var builder = new CalculationReportDataBuilder();
            var data = builder.Build(MakeProject(), CalculationReportMode.Operating, thermalDetail: detail);
            var markdown = new CalculationReportMarkdownRenderer().Render(data);

            Assert.Multiple(() =>
            {
                Assert.That(data.ThermalSection.IsDetailAvailable, Is.False);
                Assert.That(markdown, Does.Contain("нет данных"));
                Assert.That(markdown, Does.Contain("MISSING_THERMAL_DETAIL") , "предупреждение в разделе предупреждений");
            });
        }

        [Test]
        public void Build_MissingCircuitResults_ReferenceCircuitNull()
        {
            // T2-13: результатов выбранного режима нет — референсный контур
            // не строится (missing-data), пересчёта нет.
            var project = MakeProject();
            project.HydraulicsData = new HydraulicsProjectData
            {
                Collectors = new List<CollectorProjectData>
                {
                    new()
                    {
                        CollectorNumber = 1,
                        CollectorType = "HKV-D (2-12 контуров)",
                        Summary = new CollectorSummaryProjectData { PressureLoss_Operating_Pa = 0.0, PressureLoss_Cold_Pa = 0.0 },
                        Circuits = new List<CircuitProjectData>
                        {
                            new() { CircuitNumber = 1, CircuitLength = 100.0, SupplyLength = 10.0, PipeSpacingCm = 20 }
                        }
                    }
                }
            };

            var builder = new CalculationReportDataBuilder();
            var data = builder.Build(project, CalculationReportMode.Operating, thermalDetail: MakeDetail());

            Assert.Multiple(() =>
            {
                Assert.That(data.HydraulicsSection.ReferenceCircuit, Is.Null);
                Assert.That(data.Warnings.Any(w => w.Code == "MISSING_CIRCUIT_RESULT"), Is.True);
            });
        }
    }
}
