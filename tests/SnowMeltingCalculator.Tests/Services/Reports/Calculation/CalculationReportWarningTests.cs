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
    /// Фокусные тесты предупреждений детального расчётного отчёта.
    /// </summary>
    [TestFixture]
    public class CalculationReportWarningTests
    {
        private static readonly DateTime FixedDate = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Build_MissingOperatingResult_ProducesMissingCircuitResultWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Circuits[0].OperatingResult = null;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var warning = report.Warnings.Single(w => w.Code == "MISSING_CIRCUIT_RESULT");
            Assert.That(warning.Severity, Is.EqualTo("Warning"));
            Assert.That(warning.Message, Does.Contain("Operating"));
            Assert.That(warning.Message, Does.Contain("1"));
            Assert.That(warning.RelatedValues, Does.Contain("CircuitProjectData.OperatingResult"));
        }

        [Test]
        public void Build_MissingDesignResult_ProducesMissingCircuitResultWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Circuits[0].DesignResult = null;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.DesignCold, FixedDate);

            var warning = report.Warnings.Single(w => w.Code == "MISSING_CIRCUIT_RESULT");
            Assert.That(warning.Severity, Is.EqualTo("Warning"));
            Assert.That(warning.Message, Does.Contain("DesignCold"));
            Assert.That(warning.RelatedValues, Does.Contain("CircuitProjectData.DesignResult"));
        }

        [Test]
        public void Build_ValidCircuitInOperatingMode_DoesNotProduceVelocityOrPressureWarnings()
        {
            var project = CreateProjectWithValidCircuit();
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            Assert.That(report.Warnings, Is.Empty);
        }

        [Test]
        public void Build_VelocityBelowMinimum_ProducesVelocityOutOfRangeWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Circuits[0].OperatingResult!.Velocity = 0.05;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var warning = report.Warnings.Single(w => w.Code == "VELOCITY_OUT_OF_RANGE");
            Assert.That(warning.Severity, Is.EqualTo("Warning"));
            Assert.That(warning.Message, Does.Contain("0,05"));
            Assert.That(warning.Message, Does.Contain("0,1"));
            Assert.That(warning.Message, Does.Contain("2,0"));
            Assert.That(warning.SourcePath, Does.Contain("ValidationConstants"));
            Assert.That(warning.RelatedValues, Does.Contain("CircuitProjectData.OperatingResult.Velocity"));
        }

        [Test]
        public void Build_VelocityAboveMaximum_ProducesVelocityOutOfRangeWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Circuits[0].OperatingResult!.Velocity = 2.5;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var warning = report.Warnings.Single(w => w.Code == "VELOCITY_OUT_OF_RANGE");
            Assert.That(warning.Message, Does.Contain("2,5"));
            Assert.That(warning.Message, Does.Contain("2,0"));
            Assert.That(warning.RelatedValues, Does.Contain("CircuitProjectData.OperatingResult.Velocity"));
        }

        [Test]
        public void Build_VelocityWithinRange_DoesNotProduceVelocityWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Circuits[0].OperatingResult!.Velocity = 1.5;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            Assert.That(report.Warnings.Any(w => w.Code == "VELOCITY_OUT_OF_RANGE"), Is.False);
        }

        [Test]
        public void Build_PressureLossPerMeterExceeded_ProducesWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Circuits[0].OperatingResult!.PressureLossPerMeter = 350.0;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var warning = report.Warnings.Single(w => w.Code == "PRESSURE_LOSS_PER_METER_EXCEEDED");
            Assert.That(warning.Severity, Is.EqualTo("Warning"));
            Assert.That(warning.Message, Does.Contain("350"));
            Assert.That(warning.Message, Does.Contain("300"));
            Assert.That(warning.SourcePath, Does.Contain("CircuitTemperatureResult.MaxPressureLossPerMeter"));
            Assert.That(warning.RelatedValues, Does.Contain("CircuitProjectData.OperatingResult.PressureLossPerMeter"));
        }

        [Test]
        public void Build_PressureLossPerMeterWithinLimit_DoesNotProduceWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Circuits[0].OperatingResult!.PressureLossPerMeter = 250.0;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            Assert.That(report.Warnings.Any(w => w.Code == "PRESSURE_LOSS_PER_METER_EXCEEDED"), Is.False);
        }

        [Test]
        public void Build_CollectorOperatingPressureExceeded_ProducesWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Summary.PressureLoss_Operating_Pa = 35000.0;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var warning = report.Warnings.Single(w => w.Code == "COLLECTOR_PRESSURE_LOSS_EXCEEDED");
            Assert.That(warning.Severity, Is.EqualTo("Warning"));
            Assert.That(warning.Message, Does.Contain("35 000"));
            Assert.That(warning.Message, Does.Contain("32 000"));
            Assert.That(warning.SourcePath, Does.Contain("ValidationConstants.MaxPressureLoss"));
            Assert.That(warning.RelatedValues, Does.Contain("CollectorSummaryProjectData.PressureLoss_Operating_Pa"));
        }

        [Test]
        public void Build_CollectorDesignPressureExceeded_ProducesWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Summary.PressureLoss_Cold_Pa = 33000.0;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.DesignCold, FixedDate);

            var warning = report.Warnings.Single(w => w.Code == "COLLECTOR_PRESSURE_LOSS_EXCEEDED");
            Assert.That(warning.Message, Does.Contain("DesignCold"));
            Assert.That(warning.RelatedValues, Does.Contain("CollectorSummaryProjectData.PressureLoss_Cold_Pa"));
        }

        [Test]
        public void Build_CollectorPressureWithinLimit_DoesNotProduceWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Summary.PressureLoss_Operating_Pa = 15000.0;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            Assert.That(report.Warnings.Any(w => w.Code == "COLLECTOR_PRESSURE_LOSS_EXCEEDED"), Is.False);
        }

        [Test]
        public void Build_ValveTurnsExceededForHkvD_ProducesWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].ValveType = ValveType.HKV_D;
            project.HydraulicsData.Collectors[0].Circuits[0].OperatingResult!.ValveTurns = 3.0;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var warning = report.Warnings.Single(w => w.Code == "VALVE_TURNS_EXCEEDED");
            Assert.That(warning.Severity, Is.EqualTo("Warning"));
            Assert.That(warning.Message, Does.Contain("3,00"));
            Assert.That(warning.Message, Does.Contain("2,5"));
            Assert.That(warning.Message, Does.Contain("HKV_D"));
            Assert.That(warning.SourcePath, Does.Contain("ValveTurnsCalculator.GetMaxTurns"));
            Assert.That(warning.RelatedValues, Does.Contain("CircuitProjectData.OperatingResult.ValveTurns"));
            Assert.That(warning.RelatedValues, Does.Contain("CollectorProjectData.ValveType"));
        }

        [Test]
        public void Build_ValveTurnsExceededForIv_ProducesWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].ValveType = ValveType.IV_1_25;
            project.HydraulicsData.Collectors[0].Circuits[0].OperatingResult!.ValveTurns = 9.0;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            var warning = report.Warnings.Single(w => w.Code == "VALVE_TURNS_EXCEEDED");
            Assert.That(warning.Message, Does.Contain("9,00"));
            Assert.That(warning.Message, Does.Contain("8,00"));
            Assert.That(warning.Message, Does.Contain("IV_1_25"));
        }

        [Test]
        public void Build_ValveTurnsWithinLimit_DoesNotProduceWarning()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].ValveType = ValveType.HKV_D;
            project.HydraulicsData.Collectors[0].Circuits[0].OperatingResult!.ValveTurns = 2.0;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.Operating, FixedDate);

            Assert.That(report.Warnings.Any(w => w.Code == "VALVE_TURNS_EXCEEDED"), Is.False);
        }

        [Test]
        public void Build_DesignColdPressureLossPerMeterWarningUsesDesignResult()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Circuits[0].DesignResult!.PressureLossPerMeter = 350.0;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.DesignCold, FixedDate);

            var warning = report.Warnings.Single(w => w.Code == "PRESSURE_LOSS_PER_METER_EXCEEDED");
            Assert.That(warning.RelatedValues, Does.Contain("CircuitProjectData.DesignResult.PressureLossPerMeter"));
        }

        [Test]
        public void Build_DesignColdVelocityWarningUsesDesignResult()
        {
            var project = CreateProjectWithValidCircuit();
            project.HydraulicsData.Collectors[0].Circuits[0].DesignResult!.Velocity = 0.05;
            var builder = new CalculationReportDataBuilder();

            var report = builder.Build(project, CalculationReportMode.DesignCold, FixedDate);

            var warning = report.Warnings.Single(w => w.Code == "VELOCITY_OUT_OF_RANGE");
            Assert.That(warning.RelatedValues, Does.Contain("CircuitProjectData.DesignResult.Velocity"));
        }

        private static ProjectData CreateProjectWithValidCircuit()
        {
            return new ProjectData
            {
                ProjectNumber = "P-WARN",
                ProjectObject = "Тестовая площадка предупреждений",
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
