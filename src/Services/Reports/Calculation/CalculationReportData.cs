using System;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Полный снимок детального расчётного отчёта.
    /// </summary>
    /// <remarks>
    /// Независимая от рендерера (Markdown / PDF / WPF) модель данных.
    /// </remarks>
    public sealed class CalculationReportData
    {
        /// <summary>
        /// Режим отчёта.
        /// </summary>
        public CalculationReportMode Mode { get; init; }

        /// <summary>
        /// Дата формирования отчёта.
        /// </summary>
        public DateTime ReportDate { get; init; }

        /// <summary>
        /// Методология расчёта.
        /// </summary>
        public string Methodology { get; init; } = string.Empty;

        /// <summary>
        /// Раздел проекта.
        /// </summary>
        public ProjectSection ProjectSection { get; init; } = new();

        /// <summary>
        /// Раздел климатических данных.
        /// </summary>
        public ClimateSection ClimateSection { get; init; } = new();

        /// <summary>
        /// Раздел конструкции.
        /// </summary>
        public ConstructionSection ConstructionSection { get; init; } = new();

        /// <summary>
        /// Раздел теплотехнического расчёта.
        /// </summary>
        public ThermalSection ThermalSection { get; init; } = new();

        /// <summary>
        /// Раздел гидравлического расчёта.
        /// </summary>
        public HydraulicsSection HydraulicsSection { get; init; } = new();

        /// <summary>
        /// Раздел оборудования и KPI.
        /// </summary>
        public EquipmentSection EquipmentSection { get; init; } = new();

        /// <summary>
        /// Предупреждения отчёта.
        /// </summary>
        public IReadOnlyList<CalculationReportWarning> Warnings { get; init; } = new List<CalculationReportWarning>();

        /// <summary>
        /// Приложение источников.
        /// </summary>
        public SourcesAppendix SourcesAppendix { get; init; } = new();

        /// <summary>
        /// Приложение формул.
        /// </summary>
        public FormulasAppendix FormulasAppendix { get; init; } = new();
    }
}
