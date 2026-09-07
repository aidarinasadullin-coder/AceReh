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
        /// <param name="project">Данные проекта.</param>
        /// <param name="mode">Режим отчёта.</param>
        /// <param name="thermalDetail">Детальные тепловые величины (ADR-010); используется тепловым разделом.</param>
        /// <param name="hydraulicsDetail">Детальные величины гидравлики (ADR-013/В13); используется гидравлическим разделом.</param>
        SectionBuildResult<TSection> Build(
            ProjectData project,
            CalculationReportMode mode,
            ThermalReportDetail? thermalDetail = null,
            HydraulicsReportDetail? hydraulicsDetail = null);
    }
}
