using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Фокусные тесты строителя данных детального расчётного отчёта.
    /// </summary>
    [TestFixture]
    public class CalculationReportDataBuilderTests
    {
        private static readonly DateTime FixedDate = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        private static readonly string[] ForbiddenTypeNames = new[]
        {
            "ResultsViewModel",
            "ConstructionViewModel",
            "CircuitsViewModel",
            "ResultsPdfDataBuilder"
        };

        [Test]
        public void Build_OperatingMode_ReadsOperatingResultValues()
        {
            var project = CreateProjectWithCircuitResults();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var circuit = report.HydraulicsSection.Collectors[0].Circuits[0];
            Assert.That(circuit.Power.Value, Is.EqualTo(1200.0));
            Assert.That(circuit.FlowRate.Value, Is.EqualTo(100.0));
            Assert.That(circuit.DpGesamt.Value, Is.EqualTo(15000.0));
            Assert.That(report.Mode, Is.EqualTo(CalculationReportMode.Operating));
        }

        [Test]
        public void Build_DesignColdMode_ReadsDesignResultValues()
        {
            var project = CreateProjectWithCircuitResults();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.DesignCold, FixedDate);

            var circuit = report.HydraulicsSection.Collectors[0].Circuits[0];
            Assert.That(circuit.Power.Value, Is.EqualTo(2400.0));
            Assert.That(circuit.FlowRate.Value, Is.EqualTo(200.0));
            Assert.That(circuit.DpGesamt.Value, Is.EqualTo(30000.0));
            Assert.That(report.Mode, Is.EqualTo(CalculationReportMode.DesignCold));
        }

        [Test]
        public void Build_DomainValidZeroInputs_AreMarkedZeroIsValidAndRenderAsZero()
        {
            // Ревью P1–P2 (находка №1), семантика В2: «нет данных» — для
            // заглушек нехранённых величин; доменно-валидные нули входов —
            // хранимые значения → рендерятся «0», а не «нет данных».
            var project = CreateProjectWithCircuitResults();
            project.ClimateData.AirTemperature = 0.0;
            project.ClimateData.Humidity = 0.0;
            project.ClimateData.SnowfallIntensity = 0.0;
            project.ThermalData.GroundTemperature = 0.0;
            project.ConstructionData.GroundwaterLevel = 0.0;
            project.HydraulicsData.GlycolConcentration = 0.0;
            var builder = new CalculationReportDataBuilder();
            // Детали тепла — чтобы шаги строились (без ThermalReportDetail
            // шаги заменяются маркером, T2-07), их Inputs читают те же
            // климатические нули.
            var report = builder.Build(
                project,
                CalculationReportMode.Operating,
                FixedDate,
                new ThermalReportDetail
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
                });

            var stepInputs = report.ThermalSection.Steps.SelectMany(s => s.Inputs).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(report.ClimateSection.AirTemperature.ZeroIsValid, Is.True, "t_H = 0 °C валиден (−50…+10)");
                Assert.That(report.ClimateSection.Humidity.ZeroIsValid, Is.True, "влажность 0 % не валидируется");
                Assert.That(report.ClimateSection.SnowfallIntensity.ZeroIsValid, Is.True, "снегопад 0 мм/ч валиден (0…20)");
                Assert.That(report.ClimateSection.GroundTemperature.ZeroIsValid, Is.True, "t_G = 0 °C валиден (−10…30)");
                Assert.That(report.ConstructionSection.GroundwaterLevel.ZeroIsValid, Is.True, "уровень грунтовых вод 0 м валиден (0…10)");
                Assert.That(report.HydraulicsSection.GlycolConcentration.ZeroIsValid, Is.True, "концентрация 0 % — вода (спека §3.4)");
                Assert.That(report.ClimateSection.ColdPeriodDays.ZeroIsValid, Is.False, "заглушка нехранённых дней остаётся «нет данных»");
                Assert.That(report.ClimateSection.SupplyTemperature.ZeroIsValid, Is.False, "t_подачи = 0 вне диапазона (20…90)");
                foreach (var key in new[] { "t_H", "v_H", "h", "t_G" })
                {
                    var inputs = stepInputs.Where(v => v.SourceDetail == key).ToList();
                    Assert.That(inputs, Is.Not.Empty, $"входы шагов с ключом {key} присутствуют");
                    Assert.That(inputs.All(v => v.ZeroIsValid), Is.True, $"входы шагов {key} наследуют ZeroIsValid источника");
                }
            });

            var markdown = new CalculationReportMarkdownRenderer().Render(report);
            Assert.Multiple(() =>
            {
                Assert.That(markdown, Does.Contain("| 0,0 | °C |"), "t_H/t_G = 0 рендерятся значением");
                Assert.That(markdown, Does.Contain("| 0,000 | % |"), "влажность 0 % рендерится значением");
                Assert.That(markdown, Does.Contain("| 0,000 | мм/ч |"), "снегопад 0 мм/ч рендерится значением");
                Assert.That(markdown, Does.Contain("| 0,0 | м |"), "уровень грунтовых вод 0 м рендерится значением");
                Assert.That(markdown, Does.Not.Contain("| нет данных | °C |"));
                Assert.That(markdown, Does.Contain("нет данных"), "заглушки (ColdPeriodDays и пр.) остаются «нет данных»");
            });
        }

        [Test]
        public void Build_UsesCurrentProjection_WhenPersistedDtoHasStaleSentinel()
        {
            var persistedProject = CreateProjectWithCircuitResults();
            persistedProject.HydraulicsData.Collectors[0].Circuits[0].OperatingResult!.Power = 999999.0;
            var currentProject = CreateProjectWithCircuitResults();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(currentProject, CalculationReportMode.Operating, FixedDate);

            var circuit = report.HydraulicsSection.Collectors[0].Circuits[0];
            Assert.That(circuit.Power.Value, Is.EqualTo(1200.0));
            Assert.That(circuit.Power.Value, Is.Not.EqualTo(
                persistedProject.HydraulicsData.Collectors[0].Circuits[0].OperatingResult!.Power));
        }

        [Test]
        public void Build_MissingDesignResult_ProducesWarning()
        {
            var project = CreateProjectWithCircuitResults();
            project.HydraulicsData.Collectors[0].Circuits[0].DesignResult = null;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.DesignCold, FixedDate);

            Assert.That(report.Warnings, Is.Not.Empty);
            var warning = report.Warnings[0];
            Assert.That(warning.Code, Is.EqualTo("MISSING_CIRCUIT_RESULT"));
            Assert.That(warning.Severity, Is.EqualTo("Warning"));
            Assert.That(warning.Message, Does.Contain("DesignCold"));
            Assert.That(warning.Message, Does.Contain("1"));
            Assert.That(warning.RelatedValues, Does.Contain("CircuitProjectData.DesignResult"));
        }

        [Test]
        public void Build_RepeatedBuildsWithSameInputs_AreEqual()
        {
            var project = CreateProjectWithCircuitResults();
            var builder = new CalculationReportDataBuilder();

            var first = builder.Build(project, CalculationReportMode.Operating, FixedDate);
            var second = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            Assert.That(second.ReportDate, Is.EqualTo(first.ReportDate));
            Assert.That(second.Mode, Is.EqualTo(first.Mode));
            Assert.That(second.ProjectSection.ProjectNumber, Is.EqualTo(first.ProjectSection.ProjectNumber));
            Assert.That(second.ClimateSection.City.Value, Is.EqualTo(first.ClimateSection.City.Value));

            var firstCircuit = first.HydraulicsSection.Collectors[0].Circuits[0];
            var secondCircuit = second.HydraulicsSection.Collectors[0].Circuits[0];
            Assert.That(secondCircuit.Power.Value, Is.EqualTo(firstCircuit.Power.Value));
            Assert.That(secondCircuit.DpGesamt.Value, Is.EqualTo(firstCircuit.DpGesamt.Value));
            Assert.That(second.HydraulicsSection.Collectors[0].Summary.PressureLoss.Value,
                Is.EqualTo(first.HydraulicsSection.Collectors[0].Summary.PressureLoss.Value));
        }

        [Test]
        public void Build_SectionsAreNonNull()
        {
            var project = CreateMinimalProject();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            Assert.That(report.ProjectSection, Is.Not.Null);
            Assert.That(report.ClimateSection, Is.Not.Null);
            Assert.That(report.ConstructionSection, Is.Not.Null);
            Assert.That(report.ThermalSection, Is.Not.Null);
            Assert.That(report.HydraulicsSection, Is.Not.Null);
            Assert.That(report.EquipmentSection, Is.Not.Null);
            Assert.That(report.Warnings, Is.Not.Null);
            Assert.That(report.SourcesAppendix, Is.Not.Null);
            Assert.That(report.FormulasAppendix, Is.Not.Null);
        }

        [Test]
        public void Build_DefaultReportDate_IsNormalized()
        {
            var project = CreateMinimalProject();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating);

            Assert.That(report.ReportDate, Is.EqualTo(CalculationReportDataBuilder.DefaultReportDate));
        }

        [Test]
        public void PublicApi_DoesNotReferenceForbiddenViewModelsOrPdfBuilder()
        {
            var builderType = typeof(CalculationReportDataBuilder);
            var interfaceType = typeof(ICalculationReportDataBuilder);

            foreach (var type in new[] { builderType, interfaceType })
            {
                foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                {
                    foreach (var parameter in constructor.GetParameters())
                    {
                        Assert.That(
                            ForbiddenTypeNames.Any(forbidden => parameter.ParameterType.Name.Contains(forbidden, StringComparison.Ordinal)),
                            Is.False,
                            $"Constructor parameter {parameter.Name} of {type.Name} references forbidden type {parameter.ParameterType.Name}.");
                    }
                }

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => m.DeclaringType == type))
                {
                    foreach (var parameter in method.GetParameters())
                    {
                        Assert.That(
                            ForbiddenTypeNames.Any(forbidden => parameter.ParameterType.Name.Contains(forbidden, StringComparison.Ordinal)),
                            Is.False,
                            $"Method {method.Name} parameter {parameter.Name} of {type.Name} references forbidden type {parameter.ParameterType.Name}.");
                    }

                    if (method.ReturnType != typeof(void))
                    {
                        Assert.That(
                            ForbiddenTypeNames.Any(forbidden => method.ReturnType.Name.Contains(forbidden, StringComparison.Ordinal)),
                            Is.False,
                            $"Method {method.Name} of {type.Name} returns forbidden type {method.ReturnType.Name}.");
                    }
                }
            }
        }

        [Test]
        public void Build_NullProject_ThrowsArgumentNullException()
        {
            var builder = new CalculationReportDataBuilder();

            Assert.That(() => builder.Build(null!, CalculationReportMode.Operating, FixedDate),
                Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("project"));
        }

        [Test]
        public void Build_PopulatesClimateSectionFromProjectData()
        {
            var project = CreateMinimalProject();
            project.ClimateData.SelectedCity = "Москва";
            project.ClimateData.Region = "Московская область";
            project.ClimateData.AirTemperature = -28.0;
            project.ClimateData.WindSpeed = 3.5;
            project.ClimateData.Humidity = 85.0;
            project.ClimateData.SnowfallIntensity = 0.5;
            project.ClimateData.SelectedZone = ClimateZone.Zone_M20;
            project.ThermalData.SelectedMode = OperatingMode.Melting;
            project.ThermalData.GroundTemperature = 10.0;
            project.ThermalData.SupplyTemperature = 55.0;
            project.ThermalData.Result = new ThermalResultProjectData
            {
                ReturnTemperature = 40.0,
                MeanTemperature = 47.5,
                DeltaT = 15.0
            };

            var builder = new CalculationReportDataBuilder();
            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            Assert.That(report.ClimateSection.City.Value, Is.EqualTo("Москва"));
            Assert.That(report.ClimateSection.Region.Value, Is.EqualTo("Московская область"));
            Assert.That(report.ClimateSection.AirTemperature.Value, Is.EqualTo(-28.0));
            Assert.That(report.ClimateSection.WindSpeed.Value, Is.EqualTo(3.5));
            Assert.That(report.ClimateSection.Humidity.Value, Is.EqualTo(85.0));
            Assert.That(report.ClimateSection.SnowfallIntensity.Value, Is.EqualTo(0.5));
            Assert.That(report.ClimateSection.ClimateZone.Value, Is.EqualTo(ClimateZone.Zone_M20.ToString()));
            Assert.That(report.ClimateSection.SurfaceTemperature.Value, Is.EqualTo(5.0));
            Assert.That(report.ClimateSection.GroundTemperature.Value, Is.EqualTo(10.0));
            Assert.That(report.ClimateSection.SupplyTemperature.Value, Is.EqualTo(55.0));
            Assert.That(report.ClimateSection.ReturnTemperature.Value, Is.EqualTo(40.0));
            Assert.That(report.ClimateSection.MeanTemperature.Value, Is.EqualTo(47.5));
            Assert.That(report.ClimateSection.DeltaT.Value, Is.EqualTo(15.0));
        }

        [Test]
        public void Build_PopulatesHydraulicsSectionFromProjectData()
        {
            var project = CreateProjectWithCircuitResults();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            Assert.That(report.HydraulicsSection.Collectors, Has.Count.EqualTo(1));
            var collector = report.HydraulicsSection.Collectors[0];
            Assert.That(collector.Number, Is.EqualTo(1));
            Assert.That(collector.Type, Is.EqualTo("HKV-D (2-12 контуров)"));
            Assert.That(collector.Circuits, Has.Count.EqualTo(1));

            var circuit = collector.Circuits[0];
            Assert.That(circuit.CircuitNumber, Is.EqualTo(1));
            Assert.That(circuit.CircuitLength.Value, Is.EqualTo(80.0));
            Assert.That(circuit.CircuitArea.Value, Is.EqualTo(80.0 * 20.0 / 100.0));
            Assert.That(circuit.SupplyLength.Value, Is.EqualTo(10.0));
            Assert.That(circuit.TotalLength.Value, Is.EqualTo(90.0));
            Assert.That(circuit.PipeSpacing.Value, Is.EqualTo(20.0));
            Assert.That(circuit.SupplySpacing.Value, Is.EqualTo(5.0));
            Assert.That(circuit.SupplyHeatPercent.Value, Is.EqualTo(10.0));

            Assert.That(collector.Summary.CircuitCount.Value, Is.EqualTo(1.0));
            Assert.That(collector.Summary.TotalPipeLength.Value, Is.EqualTo(90.0));
            Assert.That(collector.Summary.TotalPower.Value, Is.EqualTo(1200.0));
            Assert.That(collector.Summary.TotalFlowRate.Value, Is.EqualTo(100.0));
            Assert.That(collector.Summary.PressureLoss.Value, Is.EqualTo(15000.0));
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

        private static ProjectData CreateProjectWithCircuitResults()
        {
            var project = new ProjectData
            {
                ProjectNumber = "P-002",
                ProjectObject = "Тестовая площадка с контуром",
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
                                    },
                                    Throttling = 5000.0,
                                    ValveTurns = 2.0
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

            return project;
        }
    }
}
