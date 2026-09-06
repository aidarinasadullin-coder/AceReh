using System;
using System.Collections.Generic;
using System.Globalization;
using SnowMeltingCalculator.Core;
using System.Linq;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Reports.Calculation.Builders;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Строитель данных детального расчётного отчёта.
    /// </summary>
    /// <remarks>
    /// Использует только <see cref="ProjectData"/> и не зависит от ViewModel / WPF / PDF.
    /// </remarks>
    public sealed class CalculationReportDataBuilder : ICalculationReportDataBuilder
    {
        /// <summary>
        /// Дата по умолчанию, если дата не передана.
        /// </summary>
        public static readonly DateTime DefaultReportDate = DateTime.MinValue;

        private readonly IReportSectionBuilder<ProjectSection> _projectBuilder;
        private readonly IReportSectionBuilder<ClimateSection> _climateBuilder;
        private readonly IReportSectionBuilder<ConstructionSection> _constructionBuilder;
        private readonly IReportSectionBuilder<ThermalSection> _thermalBuilder;
        private readonly IReportSectionBuilder<HydraulicsSection> _hydraulicsBuilder;
        private readonly IReportSectionBuilder<EquipmentSection> _equipmentBuilder;

        /// <summary>
        /// Создать строитель с разделными builders по умолчанию.
        /// </summary>
        public CalculationReportDataBuilder()
        {
            _projectBuilder = new ProjectSectionBuilder();
            _climateBuilder = new ClimateSectionBuilder();
            _constructionBuilder = new ConstructionSectionBuilder();
            _thermalBuilder = new ThermalSectionBuilder();
            _hydraulicsBuilder = new HydraulicsSectionBuilder();
            _equipmentBuilder = new EquipmentSectionBuilder();
        }

        /// <summary>
        /// Создать строитель с явными builders (для тестирования).
        /// </summary>
        public CalculationReportDataBuilder(
            IReportSectionBuilder<ProjectSection> projectBuilder,
            IReportSectionBuilder<ClimateSection> climateBuilder,
            IReportSectionBuilder<ConstructionSection> constructionBuilder,
            IReportSectionBuilder<ThermalSection> thermalBuilder,
            IReportSectionBuilder<HydraulicsSection> hydraulicsBuilder,
            IReportSectionBuilder<EquipmentSection> equipmentBuilder)
        {
            _projectBuilder = projectBuilder;
            _climateBuilder = climateBuilder;
            _constructionBuilder = constructionBuilder;
            _thermalBuilder = thermalBuilder;
            _hydraulicsBuilder = hydraulicsBuilder;
            _equipmentBuilder = equipmentBuilder;
        }

        /// <inheritdoc />
        public CalculationReportData Build(
            ProjectData project,
            CalculationReportMode mode,
            DateTime? reportDate = null,
            ThermalReportDetail? thermalDetail = null)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            var normalizedDate = NormalizeReportDate(reportDate);
            var warnings = CollectWarnings(project, mode, thermalDetail);

            var projectResult = _projectBuilder.Build(project, mode);
            var climateResult = _climateBuilder.Build(project, mode);
            var constructionResult = _constructionBuilder.Build(project, mode);
            var thermalResult = _thermalBuilder.Build(project, mode, thermalDetail);
            var hydraulicsResult = _hydraulicsBuilder.Build(project, mode);
            var equipmentResult = _equipmentBuilder.Build(project, mode);

            var sourceEntries = new List<ReportParameterMetadata>();
            sourceEntries.AddRange(projectResult.ParameterMetadata);
            sourceEntries.AddRange(climateResult.ParameterMetadata);
            sourceEntries.AddRange(constructionResult.ParameterMetadata);
            sourceEntries.AddRange(thermalResult.ParameterMetadata);
            sourceEntries.AddRange(hydraulicsResult.ParameterMetadata);
            sourceEntries.AddRange(equipmentResult.ParameterMetadata);

            var formulaEntries = new List<ReportFormula>();
            formulaEntries.AddRange(projectResult.Formulas);
            formulaEntries.AddRange(climateResult.Formulas);
            formulaEntries.AddRange(constructionResult.Formulas);
            formulaEntries.AddRange(thermalResult.Formulas);
            formulaEntries.AddRange(hydraulicsResult.Formulas);
            formulaEntries.AddRange(equipmentResult.Formulas);

            var distinctFormulas = formulaEntries
                .GroupBy(f => new { f.Symbol, f.Section })
                .Select(g => g.First())
                .ToList();

            return new CalculationReportData
            {
                Mode = mode,
                ReportDate = normalizedDate,
                Methodology = "Расчёт по методике REHAU",
                ProjectSection = projectResult.Section,
                ClimateSection = climateResult.Section,
                ConstructionSection = constructionResult.Section,
                ThermalSection = thermalResult.Section,
                HydraulicsSection = hydraulicsResult.Section,
                EquipmentSection = equipmentResult.Section,
                Warnings = warnings,
                SourcesAppendix = new SourcesAppendix { Entries = sourceEntries },
                FormulasAppendix = new FormulasAppendix { Formulas = distinctFormulas }
            };
        }

        /// <summary>
        /// Нормализовать дату отчёта к детерминированному значению.
        /// </summary>
        public static DateTime NormalizeReportDate(DateTime? reportDate)
        {
            return reportDate ?? DefaultReportDate;
        }

        private static IReadOnlyList<CalculationReportWarning> CollectWarnings(
            ProjectData project,
            CalculationReportMode mode,
            ThermalReportDetail? thermalDetail = null)
        {
            var warnings = new List<CalculationReportWarning>();

            if (thermalDetail is not null)
            {
                if (!thermalDetail.HasValues)
                {
                    warnings.Add(new CalculationReportWarning
                    {
                        Code = "MISSING_THERMAL_DETAIL",
                        Severity = thermalDetail.Source == ThermalReportDetailSource.RecalculationInvalid
                            ? "Error"
                            : "Warning",
                        Message = thermalDetail.Source == ThermalReportDetailSource.RecalculationInvalid
                            ? "Контрольный пересчёт теплового раздела не дал валидного результата — детальные величины в отчёте отсутствуют. Выполните тепловой расчёт и повторите экспорт."
                            : "Детальные тепловые величины недоступны — выполните тепловой расчёт и повторите экспорт.",
                        SourcePath = "SnowMeltingCalculator.Services.Reports.Calculation.ThermalReportDetail.Source",
                        RelatedValues = new List<string>(thermalDetail.ValidationErrors)
                    });
                }

                if (thermalDetail.IsStale)
                {
                    warnings.Add(new CalculationReportWarning
                    {
                        Code = "REPORT_INPUTS_STALE",
                        Severity = "Warning",
                        Message = "Тепловые входы проекта изменились после последнего расчёта — значения теплового раздела соответствуют предыдущему расчёту. Нажмите «Рассчитать» и повторите экспорт.",
                        SourcePath = "SnowMeltingCalculator.Services.Project.ThermalCalculationPhase.NeedsRecalculation",
                        RelatedValues = new List<string> { "ThermalStateSnapshot.Status.Phase" }
                    });
                }
            }

            var hydraulics = project.HydraulicsData ?? new HydraulicsProjectData();
            var collectors = hydraulics.Collectors ?? new List<CollectorProjectData>();

            foreach (var collector in collectors)
            {
                var summary = collector.Summary ?? new CollectorSummaryProjectData();
                var selectedCollectorPressureLoss = mode == CalculationReportMode.Operating
                    ? summary.PressureLoss_Operating_Pa
                    : summary.PressureLoss_Cold_Pa;

                if (selectedCollectorPressureLoss > ValidationConstants.MaxPressureLoss)
                {
                    warnings.Add(new CalculationReportWarning
                    {
                        Code = "COLLECTOR_PRESSURE_LOSS_EXCEEDED",
                        Severity = "Warning",
                        Message = $"Потери давления коллектора {collector.CollectorNumber} в режиме {mode} ({selectedCollectorPressureLoss.ToString("N0", AppCulture.Culture)} Па) превышают максимально допустимые {ValidationConstants.MaxPressureLoss.ToString("N0", AppCulture.Culture)} Па.",
                        SourcePath = "SnowMeltingCalculator.Core.Constants.ValidationConstants.MaxPressureLoss",
                        RelatedValues = new List<string>
                        {
                            mode == CalculationReportMode.Operating
                                ? "CollectorSummaryProjectData.PressureLoss_Operating_Pa"
                                : "CollectorSummaryProjectData.PressureLoss_Cold_Pa"
                        }
                    });
                }

                foreach (var circuit in collector.Circuits ?? new List<CircuitProjectData>())
                {
                    var result = mode == CalculationReportMode.Operating
                        ? circuit.OperatingResult
                        : circuit.DesignResult;

                    if (result is null)
                    {
                        warnings.Add(new CalculationReportWarning
                        {
                            Code = "MISSING_CIRCUIT_RESULT",
                            Severity = "Warning",
                            Message = $"Отсутствуют результаты расчёта для контура {circuit.CircuitNumber} коллектора {collector.CollectorNumber} в режиме {mode}.",
                            SourcePath = "SnowMeltingCalculator.Services.Reports.Calculation.CalculationReportDataBuilder.CollectWarnings",
                            RelatedValues = new List<string>
                            {
                                mode == CalculationReportMode.Operating
                                    ? "CircuitProjectData.OperatingResult"
                                    : "CircuitProjectData.DesignResult"
                            }
                        });
                        continue;
                    }

                    var velocityPath = mode == CalculationReportMode.Operating
                        ? "CircuitProjectData.OperatingResult.Velocity"
                        : "CircuitProjectData.DesignResult.Velocity";

                    if (result.Velocity < ValidationConstants.MinVelocity || result.Velocity > ValidationConstants.MaxVelocity)
                    {
                        warnings.Add(new CalculationReportWarning
                        {
                            Code = "VELOCITY_OUT_OF_RANGE",
                            Severity = "Warning",
                            Message = $"Скорость потока в контуре {circuit.CircuitNumber} коллектора {collector.CollectorNumber} в режиме {mode} ({result.Velocity.ToString("N2", AppCulture.Culture)} м/с) выходит за допустимый диапазон {ValidationConstants.MinVelocity.ToString("N1", AppCulture.Culture)}..{ValidationConstants.MaxVelocity.ToString("N1", AppCulture.Culture)} м/с.",
                            SourcePath = "SnowMeltingCalculator.Core.Constants.ValidationConstants.MinVelocity|MaxVelocity",
                            RelatedValues = new List<string> { velocityPath }
                        });
                    }

                    if (result.PressureLossPerMeter > CircuitTemperatureResult.MaxPressureLossPerMeter)
                    {
                        warnings.Add(new CalculationReportWarning
                        {
                            Code = "PRESSURE_LOSS_PER_METER_EXCEEDED",
                            Severity = "Warning",
                            Message = $"Удельные потери давления в контуре {circuit.CircuitNumber} коллектора {collector.CollectorNumber} в режиме {mode} ({result.PressureLossPerMeter.ToString("N0", AppCulture.Culture)} Па/м) превышают максимально допустимые {CircuitTemperatureResult.MaxPressureLossPerMeter.ToString("N0", AppCulture.Culture)} Па/м.",
                            SourcePath = "SnowMeltingCalculator.Models.Hydraulics.CircuitTemperatureResult.MaxPressureLossPerMeter",
                            RelatedValues = new List<string>
                            {
                                mode == CalculationReportMode.Operating
                                    ? "CircuitProjectData.OperatingResult.PressureLossPerMeter"
                                    : "CircuitProjectData.DesignResult.PressureLossPerMeter"
                            }
                        });
                    }

                    var maxTurns = ValveTurnsCalculator.GetMaxTurns(collector.ValveType);
                    if (result.ValveTurns > maxTurns)
                    {
                        warnings.Add(new CalculationReportWarning
                        {
                            Code = "VALVE_TURNS_EXCEEDED",
                            Severity = "Warning",
                            Message = $"Обороты балансировочного клапана в контуре {circuit.CircuitNumber} коллектора {collector.CollectorNumber} ({result.ValveTurns.ToString("N2", AppCulture.Culture)} об) превышают максимум {maxTurns.ToString("N2", AppCulture.Culture)} об для типа {collector.ValveType}.",
                            SourcePath = "SnowMeltingCalculator.Services.Hydraulics.ValveTurnsCalculator.GetMaxTurns",
                            RelatedValues = new List<string>
                            {
                                mode == CalculationReportMode.Operating
                                    ? "CircuitProjectData.OperatingResult.ValveTurns"
                                    : "CircuitProjectData.DesignResult.ValveTurns",
                                "CollectorProjectData.ValveType"
                            }
                        });
                    }
                }
            }

            return warnings;
        }
    }
}
