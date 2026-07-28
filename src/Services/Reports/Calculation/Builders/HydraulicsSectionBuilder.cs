using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation.Builders
{
    /// <summary>
    /// Строитель раздела гидравлического расчёта.
    /// </summary>
    public sealed class HydraulicsSectionBuilder : IReportSectionBuilder<HydraulicsSection>
    {
        public SectionBuildResult<HydraulicsSection> Build(ProjectData project, CalculationReportMode mode)
        {
            var hydraulics = project.HydraulicsData ?? new HydraulicsProjectData();
            var collectors = (hydraulics.Collectors ?? new List<CollectorProjectData>())
                .Select(collector => BuildReportCollector(collector, mode))
                .ToList();

            var section = new HydraulicsSection
            {
                GlycolType = ReportValueFactory.Create(hydraulics.GlycolType.ToString(), "-", ReportValueSource.UserInput, "ProjectData.HydraulicsData.GlycolType"),
                GlycolConcentration = ReportValueFactory.Create(hydraulics.GlycolConcentration, "%", ReportValueSource.UserInput, "ProjectData.HydraulicsData.GlycolConcentration"),
                Density = ReportValueFactory.Create(0.0, "г/см³", ReportValueSource.Calculated, "CircuitResultProjectData.Density", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                SpecificHeat = ReportValueFactory.Create(0.0, "кДж/(кг·К)", ReportValueSource.Calculated, "GlycolProperties.SpecificHeat", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                KinematicViscosity = ReportValueFactory.Create(0.0, "мм²/с", ReportValueSource.Calculated, "CircuitResultProjectData.KinematicViscosity", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                Collectors = collectors
            };

            return new SectionBuildResult<HydraulicsSection>
            {
                Section = section,
                ParameterMetadata = HydraulicsReportMetadataBuilder.BuildMetadata(section),
                Formulas = HydraulicsReportMetadataBuilder.BuildFormulas()
            };
        }

        private static ReportCollector BuildReportCollector(CollectorProjectData collector, CalculationReportMode mode)
        {
            var circuits = (collector.Circuits ?? new List<CircuitProjectData>())
                .Select(circuit => BuildReportCircuit(collector, circuit, mode))
                .ToList();

            var summary = collector.Summary ?? new CollectorSummaryProjectData();

            return new ReportCollector
            {
                Number = collector.CollectorNumber,
                Type = collector.CollectorType ?? string.Empty,
                Circuits = circuits,
                Summary = new ReportCollectorSummary
                {
                    CollectorType = ReportValueFactory.Create(summary.CollectorType ?? string.Empty, "-", ReportValueSource.Calculated, "CollectorSummaryProjectData.CollectorType", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                    CircuitCount = ReportValueFactory.Create((double)summary.CircuitCount, "шт", ReportValueSource.Calculated, "CollectorSummaryProjectData.CircuitCount"),
                    TotalPipeLength = ReportValueFactory.Create(summary.TotalPipeLength, "м", ReportValueSource.Calculated, "CollectorSummaryProjectData.TotalPipeLength"),
                    TotalPower = ReportValueFactory.Create(summary.TotalPower, "Вт", ReportValueSource.Calculated, "CollectorSummaryProjectData.TotalPower"),
                    TotalFlowRate = ReportValueFactory.Create(summary.TotalFlowRate, "л/ч", ReportValueSource.Calculated, "CollectorSummaryProjectData.TotalFlowRate"),
                    PressureLoss = ReportValueFactory.Create(
                        mode == CalculationReportMode.Operating ? summary.PressureLoss_Operating_Pa : summary.PressureLoss_Cold_Pa,
                        "Па",
                        ReportValueSource.Calculated,
                        mode == CalculationReportMode.Operating ? "CollectorSummaryProjectData.PressureLoss_Operating_Pa" : "CollectorSummaryProjectData.PressureLoss_Cold_Pa",
                        formula: "max(DpGesamt)"),
                    Kv = ReportValueFactory.Create(summary.Kv, "-", ReportValueSource.Calculated, "CollectorSummaryProjectData.Kv", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed)
                }
            };
        }

        private static ReportCircuit BuildReportCircuit(CollectorProjectData collector, CircuitProjectData circuit, CalculationReportMode mode)
        {
            var resultValues = (mode == CalculationReportMode.Operating ? circuit.OperatingResult : circuit.DesignResult)
                ?? new CircuitResultProjectData();

            return new ReportCircuit
            {
                CircuitNumber = circuit.CircuitNumber,
                CircuitLength = ReportValueFactory.Create(circuit.CircuitLength, "м", ReportValueSource.UserInput, "CircuitProjectData.CircuitLength"),
                CircuitArea = ReportValueFactory.Create(circuit.CircuitLength * circuit.PipeSpacingCm / 100.0, "м²", ReportValueSource.Calculated, "CircuitRow.CircuitArea", formula: "L_HK * VAHK / 100"),
                SupplyLength = ReportValueFactory.Create(circuit.SupplyLength, "м", ReportValueSource.UserInput, "CircuitProjectData.SupplyLength"),
                TotalLength = ReportValueFactory.Create(circuit.CircuitLength + circuit.SupplyLength, "м", ReportValueSource.Calculated, "CircuitRow.TotalLength", formula: "CircuitLength + SupplyLength"),
                PipeSpacing = ReportValueFactory.Create(circuit.PipeSpacingCm, "см", ReportValueSource.UserInput, "CircuitProjectData.PipeSpacingCm"),
                SupplySpacing = ReportValueFactory.Create(circuit.SupplySpacingCm, "см", ReportValueSource.UserInput, "CircuitProjectData.SupplySpacingCm"),
                SupplyHeatPercent = ReportValueFactory.Create(circuit.SupplyHeatPercent, "%", ReportValueSource.UserInput, "CircuitProjectData.SupplyHeatPercent"),
                Power = ReportValueFactory.Create(resultValues.Power, "Вт", ReportValueSource.Calculated, "CircuitResultProjectData.Power", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                FlowRate = ReportValueFactory.Create(resultValues.FlowRate, "л/ч", ReportValueSource.Calculated, "CircuitResultProjectData.FlowRate", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                Velocity = ReportValueFactory.Create(resultValues.Velocity, "м/с", ReportValueSource.Calculated, "CircuitResultProjectData.Velocity", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                Density = ReportValueFactory.Create(resultValues.Density, "г/см³", ReportValueSource.Calculated, "CircuitResultProjectData.Density", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                KinematicViscosity = ReportValueFactory.Create(resultValues.KinematicViscosity, "мм²/с", ReportValueSource.Calculated, "CircuitResultProjectData.KinematicViscosity", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                ReynoldsNumber = ReportValueFactory.Create(resultValues.ReynoldsNumber, "-", ReportValueSource.Calculated, "CircuitResultProjectData.ReynoldsNumber", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                FrictionFactor = ReportValueFactory.Create(resultValues.FrictionFactor, "-", ReportValueSource.Calculated, "CircuitResultProjectData.FrictionFactor", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                PressureLossPerMeter = ReportValueFactory.Create(resultValues.PressureLossPerMeter, "Па/м", ReportValueSource.Calculated, "CircuitResultProjectData.PressureLossPerMeter", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                DpRohr = ReportValueFactory.Create(resultValues.DpRohr, "Па", ReportValueSource.Calculated, "CircuitResultProjectData.DpRohr", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                DpVerteiler = ReportValueFactory.Create(resultValues.DpVerteiler, "Па", ReportValueSource.Calculated, "CircuitResultProjectData.DpVerteiler", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                DpVent = ReportValueFactory.Create(resultValues.DpVent, "Па", ReportValueSource.Calculated, "CircuitResultProjectData.DpVent", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                DpGesamt = ReportValueFactory.Create(resultValues.DpGesamt, "Па", ReportValueSource.Calculated, "CircuitResultProjectData.DpGesamt", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                Throttling = ReportValueFactory.Create(circuit.Throttling, "Па", ReportValueSource.Calculated, "CircuitProjectData.Throttling", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                ZuDrosseln = ReportValueFactory.Create(resultValues.Throttling, "Па", ReportValueSource.Calculated, "CircuitResultProjectData.Throttling", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                ValveTurns = ReportValueFactory.Create(resultValues.ValveTurns, "об", ReportValueSource.Calculated, "CircuitResultProjectData.ValveTurns", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed),
                FlowRegime = ReportValueFactory.Create(resultValues.FlowRegime ?? resultValues.FlowRegimeString ?? string.Empty, "-", ReportValueSource.Calculated, "CircuitResultProjectData.FlowRegime", formulaStatus: HydraulicsReportMetadataBuilder.FormulaStatusUnconfirmed)
            };
        }
    }
}
