using System.Collections.Generic;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation.Builders
{
    /// <summary>
    /// Строитель раздела проекта.
    /// </summary>
    public sealed class ProjectSectionBuilder : IReportSectionBuilder<ProjectSection>
    {
        public SectionBuildResult<ProjectSection> Build(ProjectData project, CalculationReportMode mode, ThermalReportDetail? thermalDetail = null)
        {
            var section = new ProjectSection
            {
                ProjectNumber = project.ProjectNumber ?? string.Empty,
                ProjectObject = project.ProjectObject ?? string.Empty
            };

            var metadata = new List<ReportParameterMetadata>
            {
                Meta("Номер проекта", "-", "Номер проекта в системе", "-", ReportValueSource.UserInput, "ProjectData.ProjectNumber", null, "ProjectSection.ProjectNumber", "ProjectSection"),
                Meta("Наименование объекта", "-", "Название объекта проектирования", "-", ReportValueSource.UserInput, "ProjectData.ProjectObject", null, "ProjectSection.ProjectObject", "ProjectSection")
            };

            return new SectionBuildResult<ProjectSection>
            {
                Section = section,
                ParameterMetadata = metadata,
                Formulas = new List<ReportFormula>()
            };
        }

        private static ReportParameterMetadata Meta(
            string name,
            string symbol,
            string physicalMeaning,
            string unit,
            ReportValueSource source,
            string sourceDetail,
            string? formula,
            string whereCalculated,
            string whereUsed)
        {
            return new ReportParameterMetadata
            {
                Name = name,
                Symbol = symbol,
                PhysicalMeaning = physicalMeaning,
                Unit = unit,
                Source = source,
                SourceDetail = sourceDetail,
                Formula = formula,
                FormulaSource = formula == null ? string.Empty : "ProjectSectionBuilder",
                WhereCalculated = whereCalculated,
                WhereUsed = whereUsed
            };
        }
    }
}
