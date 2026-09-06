using System;
using System.Collections.Generic;
using System.Text;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Вспомогательные методы форматирования Markdown-таблиц и значений.
    /// Числа — каноническая культура приложения (<see cref="ReportNumber"/>, В6).
    /// </summary>
    public static class CalculationReportMarkdownRenderHelper
    {
        /// <summary>Формат табличных чисел по умолчанию.</summary>
        public const string TableFormat = "N3";

        public static string Value(ReportValue<double> value)
        {
            double? v = value.Value;
            if (!v.HasValue)
            {
                return CalculationReportMarkdownRendererConstants.MissingValue;
            }

            return ReportNumber.Format(v.Value, TableFormat);
        }

        public static string Value(ReportValue<string> value)
        {
            if (value.Value == null)
            {
                return CalculationReportMarkdownRendererConstants.MissingValue;
            }

            return string.IsNullOrWhiteSpace(value.Value)
                ? CalculationReportMarkdownRendererConstants.MissingValue
                : EscapeCell(value.Value);
        }

        public static string ValueWithUnit(ReportValue<double> value)
        {
            var displayValue = Value(value);
            var unit = EscapeCell(value.Unit);
            return string.IsNullOrEmpty(unit)
                ? displayValue
                : $"{displayValue} {unit}";
        }

        public static string Source(ReportValue<double> value) => value.Source.ToString();

        public static string Source(ReportValue<string> value) => value.Source.ToString();

        public static string EscapeCell(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value
                .Replace("\\", "\\\\")
                .Replace("|", "\\|")
                .Replace("\n", " ")
                .Replace("\r", " ");
            return escaped;
        }

        public static string OrDash(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        public static void RenderScalarTable(StringBuilder sb, IEnumerable<(string Name, ReportValue<double> Value)> rows)
        {
            sb.AppendLine("| Параметр | Обозначение | Значение | Единица | Источник |");
            sb.AppendLine("| --- | --- | --- | --- | --- |");
            foreach (var (name, value) in rows)
            {
                sb.AppendLine($"| {EscapeCell(name)} | {EscapeCell(value.SourceDetail)} | {Value(value)} | {EscapeCell(value.Unit)} | {Source(value)} |");
            }
        }

        public static void RenderScalarTable(StringBuilder sb, IEnumerable<(string Name, ReportValue<string> Value)> rows)
        {
            sb.AppendLine("| Параметр | Обозначение | Значение | Единица | Источник |");
            sb.AppendLine("| --- | --- | --- | --- | --- |");
            foreach (var (name, value) in rows)
            {
                sb.AppendLine($"| {EscapeCell(name)} | {EscapeCell(value.SourceDetail)} | {Value(value)} | {EscapeCell(value.Unit)} | {Source(value)} |");
            }
        }
    }
}
