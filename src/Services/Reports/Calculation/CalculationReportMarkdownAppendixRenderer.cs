using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Рендеринг приложений (предупреждения, источники, формулы) детального отчёта в Markdown.
    /// </summary>
    public static class CalculationReportMarkdownAppendixRenderer
    {
        public static void RenderWarnings(StringBuilder sb, IReadOnlyList<CalculationReportWarning> warnings)
        {
            RenderWarnings(sb, warnings, new List<string>());
        }

        /// <summary>
        /// Раздел «Предупреждения и ограничения»: предупреждения v1-лимитов и
        /// (В7) примечания валидации результата расчёта/пересчёта.
        /// </summary>
        public static void RenderWarnings(
            StringBuilder sb,
            IReadOnlyList<CalculationReportWarning> warnings,
            IReadOnlyList<string> validationNotes)
        {
            sb.AppendLine("## Предупреждения и ограничения");
            if (validationNotes.Count > 0)
            {
                foreach (var note in validationNotes)
                {
                    sb.AppendLine($"- {CalculationReportMarkdownRenderHelper.EscapeCell(note)}");
                }

                sb.AppendLine();
            }

            if (warnings.Count == 0)
            {
                sb.AppendLine(CalculationReportMarkdownRendererConstants.NoWarningSentinel);
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| Код | Уровень | Сообщение | Путь | Связанные значения |");
            sb.AppendLine("| --- | --- | --- | --- | --- |");
            foreach (var warning in warnings)
            {
                var related = warning.RelatedValues.Count > 0
                    ? string.Join(", ", warning.RelatedValues)
                    : "-";
                sb.AppendLine($"| {CalculationReportMarkdownRenderHelper.EscapeCell(warning.Code)} | {CalculationReportMarkdownRenderHelper.EscapeCell(warning.Severity)} | {CalculationReportMarkdownRenderHelper.EscapeCell(warning.Message)} | {CalculationReportMarkdownRenderHelper.EscapeCell(warning.SourcePath)} | {CalculationReportMarkdownRenderHelper.EscapeCell(related)} |");
            }

            sb.AppendLine();
        }

        public static void RenderSourcesAppendix(StringBuilder sb, SourcesAppendix appendix)
        {
            sb.AppendLine("## Приложение: источники значений");
            if (appendix.Entries.Count == 0)
            {
                sb.AppendLine("Нет записей.");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("| Путь | Название | Обозначение | Физический смысл | Единица | Источник | Деталь источника | Формула | Источник формулы | Где рассчитывается | Где используется |");
            sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            foreach (var entry in appendix.Entries)
            {
                sb.AppendLine(
                    $"| {CalculationReportMarkdownRenderHelper.EscapeCell(CalculationReportMarkdownRenderHelper.OrDash(entry.SourceDetail))} | " +
                    $"{CalculationReportMarkdownRenderHelper.EscapeCell(entry.Name)} | " +
                    $"{CalculationReportMarkdownRenderHelper.EscapeCell(entry.Symbol)} | " +
                    $"{CalculationReportMarkdownRenderHelper.EscapeCell(entry.PhysicalMeaning)} | " +
                    $"{CalculationReportMarkdownRenderHelper.EscapeCell(entry.Unit)} | " +
                    $"{entry.Source} | " +
                    $"{CalculationReportMarkdownRenderHelper.EscapeCell(CalculationReportMarkdownRenderHelper.OrDash(entry.SourceDetail))} | " +
                    $"{CalculationReportMarkdownRenderHelper.EscapeCell(CalculationReportMarkdownRenderHelper.OrDash(entry.Formula))} | " +
                    $"{CalculationReportMarkdownRenderHelper.EscapeCell(CalculationReportMarkdownRenderHelper.OrDash(entry.FormulaSource))} | " +
                    $"{CalculationReportMarkdownRenderHelper.EscapeCell(CalculationReportMarkdownRenderHelper.OrDash(entry.WhereCalculated))} | " +
                    $"{CalculationReportMarkdownRenderHelper.EscapeCell(CalculationReportMarkdownRenderHelper.OrDash(entry.WhereUsed))} |");
            }

            sb.AppendLine();
        }

        public static void RenderFormulasAppendix(StringBuilder sb, FormulasAppendix appendix)
        {
            sb.AppendLine("## Приложение: формулы и обозначения");
            if (appendix.Formulas.Count == 0)
            {
                sb.AppendLine("Нет записей.");
                sb.AppendLine();
                return;
            }

            var grouped = appendix.Formulas
                .OrderBy(f => f.Section)
                .ThenBy(f => f.Symbol)
                .GroupBy(f => f.Section)
                .ToList();
            foreach (var group in grouped)
            {
                sb.AppendLine($"### {CalculationReportMarkdownRenderHelper.EscapeCell(group.Key)}");
                sb.AppendLine("| Символ | Выражение | Источник | Статус |");
                sb.AppendLine("| --- | --- | --- | --- |");
                foreach (var formula in group)
                {
                    var expression = string.IsNullOrWhiteSpace(formula.Expression)
                        ? CalculationReportMarkdownRendererConstants.FormulaNotInMvp
                        : CalculationReportMarkdownRenderHelper.EscapeCell(formula.Expression);
                    var status = string.IsNullOrWhiteSpace(formula.FormulaStatus)
                        ? "-"
                        : CalculationReportMarkdownRenderHelper.EscapeCell(formula.FormulaStatus);
                    if (string.IsNullOrWhiteSpace(formula.Expression) && !string.IsNullOrWhiteSpace(formula.FormulaStatus))
                    {
                        status = CalculationReportMarkdownRendererConstants.FormulaNotInMvp;
                    }

                    sb.AppendLine($"| {CalculationReportMarkdownRenderHelper.EscapeCell(formula.Symbol)} | {expression} | {CalculationReportMarkdownRenderHelper.EscapeCell(formula.SourcePath)} | {status} |");
                }
            }

            sb.AppendLine();
        }
    }
}
