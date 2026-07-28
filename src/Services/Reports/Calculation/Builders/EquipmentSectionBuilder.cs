using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation.Builders
{
    /// <summary>
    /// Строитель раздела оборудования и KPI.
    /// </summary>
    public sealed class EquipmentSectionBuilder : IReportSectionBuilder<EquipmentSection>
    {
        public SectionBuildResult<EquipmentSection> Build(ProjectData project, CalculationReportMode mode)
        {
            var hydraulics = project.HydraulicsData ?? new HydraulicsProjectData();
            var collectors = hydraulics.Collectors ?? new List<CollectorProjectData>();

            var specifications = collectors
                .Select(collector => new ReportCollectorSpecification
                {
                    Number = collector.CollectorNumber,
                    Type = collector.CollectorType ?? string.Empty,
                    CircuitCount = collector.Summary?.CircuitCount ?? 0,
                    TotalPower = ReportValueFactory.Create(collector.Summary?.TotalPower ?? 0.0, "Вт", ReportValueSource.Calculated, "CollectorSummaryProjectData.TotalPower"),
                    TotalFlowRate = ReportValueFactory.Create(collector.Summary?.TotalFlowRate ?? 0.0, "л/ч", ReportValueSource.Calculated, "CollectorSummaryProjectData.TotalFlowRate"),
                    PressureLoss = ReportValueFactory.Create(
                        mode == CalculationReportMode.Operating
                            ? (collector.Summary?.PressureLoss_Operating_Pa ?? 0.0)
                            : (collector.Summary?.PressureLoss_Cold_Pa ?? 0.0),
                        "Па",
                        ReportValueSource.Calculated,
                        mode == CalculationReportMode.Operating
                            ? "CollectorSummaryProjectData.PressureLoss_Operating_Pa"
                            : "CollectorSummaryProjectData.PressureLoss_Cold_Pa",
                        formula: "max(DpGesamt)"),
                    Kv = ReportValueFactory.Create(collector.Summary?.Kv ?? 0.0, "-", ReportValueSource.Calculated, "CollectorSummaryProjectData.Kv")
                })
                .ToList();

            var totalPower = collectors.Sum(c => c.Summary?.TotalPower ?? 0.0);
            var totalFlowRate = collectors.Sum(c => c.Summary?.TotalFlowRate ?? 0.0);
            var pressureLoss = collectors.Any()
                ? (mode == CalculationReportMode.Operating
                    ? collectors.Max(c => c.Summary?.PressureLoss_Operating_Pa ?? 0.0)
                    : collectors.Max(c => c.Summary?.PressureLoss_Cold_Pa ?? 0.0))
                : 0.0;
            var totalPipeLength = collectors.Sum(c => c.Summary?.TotalPipeLength ?? 0.0);
            var systemVolume = totalPipeLength * Math.PI *
                Math.Pow((project.ThermalData?.SelectedPipe?.InnerDiameter ?? 0.0) / 1000.0 / 2.0, 2) * 1000.0;
            var expansionTankVolume = systemVolume * 0.034 * 1.2;

            var totalThermalPower = ReportValueFactory.Create(totalPower / 1000.0, "кВт", ReportValueSource.Calculated, "CollectorSummaryProjectData.TotalPower", formula: "sum(TotalPower) / 1000");
            var systemVolumeValue = ReportValueFactory.Create(systemVolume, "л", ReportValueSource.Calculated, "ProjectData.ThermalData.SelectedPipe.InnerDiameter", formula: "PI * d_inner² / 4 * totalLength * 1000");
            var pumpFlowRate = ReportValueFactory.Create(totalFlowRate / 1000.0, "м³/ч", ReportValueSource.Calculated, "CollectorSummaryProjectData.TotalFlowRate", formula: "sum(TotalFlowRate) / 1000");
            var pumpHead = ReportValueFactory.Create(pressureLoss / 1000.0, "кПа", ReportValueSource.Calculated, "CollectorSummaryProjectData.PressureLoss", formula: "max(PressureLoss) / 1000");
            var expansionTankVolumeValue = ReportValueFactory.Create(expansionTankVolume, "л", ReportValueSource.Calculated, "EquipmentSection.SystemVolume", formula: "SystemVolume * 0.034 * 1.2");
            var totalPipeLengthValue = ReportValueFactory.Create(totalPipeLength, "м", ReportValueSource.Calculated, "CollectorSummaryProjectData.TotalPipeLength", formula: "sum(TotalLength)");
            var rzsCount = ReportValueFactory.Create((double)collectors.Count, "шт", ReportValueSource.Calculated, "HydraulicsProjectData.Collectors", formula: "Count");

            var section = new EquipmentSection
            {
                TotalThermalPower = totalThermalPower,
                SystemVolume = systemVolumeValue,
                PumpFlowRate = pumpFlowRate,
                PumpHead = pumpHead,
                ExpansionTankVolume = expansionTankVolumeValue,
                TotalPipeLength = totalPipeLengthValue,
                RzsCount = rzsCount,
                CollectorSpecifications = specifications
            };

            var metadata = new List<ReportParameterMetadata>
            {
                Meta("Суммарная тепловая мощность", "Q_total_project", "Суммарная тепловая мощность проекта", totalThermalPower),
                Meta("Объём системы", "V_system", "Объём труб системы", systemVolumeValue),
                Meta("Расход насоса", "Q_pump", "Суммарный расход насоса", pumpFlowRate),
                Meta("Напор насоса", "H_pump", "Напор насоса по максимальным потерям", pumpHead),
                Meta("Объём расширительного бака", "V_tank", "Объём расширительного бака", expansionTankVolumeValue),
                Meta("Общая длина труб", "L_total_pipe", "Суммарная длина труб проекта", totalPipeLengthValue),
                Meta("Количество коллекторов / РЗС", "-", "Количество коллекторов / РЗС", rzsCount)
            };

            foreach (var spec in specifications)
            {
                metadata.Add(Meta("Тип коллектора", "-", "Тип коллектора", spec.Type, "EquipmentSection.CollectorSpecifications"));
                metadata.Add(Meta("Количество контуров коллектора", "-", "Количество контуров коллектора", spec.CircuitCount));
                metadata.Add(Meta("Суммарная мощность коллектора", "-", "Суммарная мощность коллектора", spec.TotalPower));
                metadata.Add(Meta("Суммарный расход коллектора", "-", "Суммарный расход коллектора", spec.TotalFlowRate));
                metadata.Add(Meta("Потери давления коллектора", "-", "Потери давления коллектора", spec.PressureLoss));
                metadata.Add(Meta("Kv коллектора/клапана", "Kv", "Коэффициент пропускной способности клапана", spec.Kv));
            }

            var formulas = new List<ReportFormula>
            {
                Formula("Q_total_project", "sum(TotalPower) / 1000", "ResultsViewModel.cs / ResultsPdfData", "Equipment"),
                Formula("V_system", "PI * d_inner² / 4 * totalLength * 1000", "ResultsViewModel.cs / EquipmentSectionBuilder", "Equipment"),
                Formula("Q_pump", "sum(TotalFlowRate) / 1000", "ResultsViewModel.cs / ResultsPdfData", "Equipment"),
                Formula("H_pump", "max(PressureLoss) / 1000", "ResultsViewModel.cs / ResultsPdfData", "Equipment"),
                Formula("V_tank", "SystemVolume * 0.034 * 1.2", "ResultsViewModel.cs / ResultsPdfData", "Equipment"),
                Formula("L_total_pipe", "sum(TotalLength)", "ResultsViewModel.cs / ResultsPdfData", "Equipment"),
                Formula("RzsCount", "Count", "ResultsViewModel.cs / ResultsPdfData", "Equipment")
            };

            return new SectionBuildResult<EquipmentSection>
            {
                Section = section,
                ParameterMetadata = metadata,
                Formulas = formulas
            };
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, ReportValue<double> value)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = value.Unit,
                Source = value.Source,
                SourceDetail = value.SourceDetail,
                Formula = value.Formula ?? value.FormulaStatus,
                FormulaSource = "EquipmentSectionBuilder",
                WhereCalculated = value.SourceDetail,
                WhereUsed = "EquipmentSection"
            };
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, int value)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = "шт",
                Source = ReportValueSource.Calculated,
                SourceDetail = "CollectorSummaryProjectData.CircuitCount",
                Formula = null,
                FormulaSource = "EquipmentSectionBuilder",
                WhereCalculated = "CollectorSummaryProjectData.CircuitCount",
                WhereUsed = "EquipmentSection.CollectorSpecifications"
            };
        }

        private static ReportParameterMetadata Meta(string name, string symbol, string physicalMeaning, string value, string whereUsed)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = "-",
                Source = ReportValueSource.Calculated,
                SourceDetail = "CollectorSummaryProjectData.CollectorType",
                Formula = null,
                FormulaSource = "EquipmentSectionBuilder",
                WhereCalculated = "CollectorSummaryProjectData.CollectorType",
                WhereUsed = whereUsed
            };
        }

        private static ReportFormula Formula(string symbol, string expression, string sourcePath, string section)
        {
            return new ReportFormula
            {
                Symbol = symbol,
                Expression = expression,
                SourcePath = sourcePath,
                Section = section
            };
        }
    }
}
