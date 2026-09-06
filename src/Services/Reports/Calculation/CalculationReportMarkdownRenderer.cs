using System;
using System.Text;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Статический рендерер детального расчётного отчёта в Markdown.
    /// </summary>
    /// <remarks>
    /// Не зависит от WPF, PDF-рендерера, ViewModel и не выполняет новых расчётов.
    /// Форматирование секций и приложений делегировано вспомогательным классам.
    /// </remarks>
    public sealed class CalculationReportMarkdownRenderer : ICalculationReportMarkdownRenderer
    {
        /// <summary>
        /// Сформировать Markdown-представление отчёта.
        /// </summary>
        public string Render(CalculationReportData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var sb = new StringBuilder();
            CalculationReportMarkdownSectionRenderer.RenderTitle(sb, data);
            CalculationReportMarkdownSectionRenderer.RenderMethodology(sb, data);
            CalculationReportMarkdownSectionRenderer.RenderSummary(sb, data);
            CalculationReportMarkdownSectionRenderer.RenderProjectSection(sb, data.ProjectSection);
            CalculationReportMarkdownSectionRenderer.RenderClimateSection(sb, data.ClimateSection);
            CalculationReportMarkdownSectionRenderer.RenderConstructionSection(sb, data.ConstructionSection);
            CalculationReportMarkdownSectionRenderer.RenderThermalSection(sb, data.ThermalSection, data.Mode);
            CalculationReportMarkdownSectionRenderer.RenderHydraulicsSection(sb, data.HydraulicsSection);
            CalculationReportMarkdownSectionRenderer.RenderEquipmentSection(sb, data.EquipmentSection);
            CalculationReportMarkdownAppendixRenderer.RenderWarnings(sb, data.Warnings);
            CalculationReportMarkdownAppendixRenderer.RenderSourcesAppendix(sb, data.SourcesAppendix);
            CalculationReportMarkdownAppendixRenderer.RenderFormulasAppendix(sb, data.FormulasAppendix);
            return sb.ToString();
        }
    }
}
