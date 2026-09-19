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
            Assert.That(warning.Message, Does.Contain("рабочем режиме"));
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
            Assert.That(warning.Message, Does.Contain("холодного пуска"));
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
            Assert.That(warning.Message, Does.Contain("холодного пуска"));
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
            return CalculationReportTestProjectFactory.CreateValidCircuitProject("P-WARN", "Тестовая площадка предупреждений");
        }
    }
}
