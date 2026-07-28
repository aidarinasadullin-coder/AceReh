using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Reports.Calculation.Builders
{
    /// <summary>
    /// Универсальный интерфейс строителя раздела детального отчёта.
    /// </summary>
    public interface IReportSectionBuilder<TSection>
    {
        /// <summary>
        /// Построить раздел и собрать метаданные параметров и формулы.
        /// </summary>
        SectionBuildResult<TSection> Build(ProjectData project, CalculationReportMode mode);
    }
}
