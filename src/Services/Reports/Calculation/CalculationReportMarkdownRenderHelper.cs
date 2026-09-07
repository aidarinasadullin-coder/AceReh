using System;
using System.Collections.Generic;
using System.Text;
using SnowMeltingCalculator.Core;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Вспомогательные методы форматирования Markdown-таблиц и значений.
    /// Числа — каноническая культура приложения (<see cref="ReportNumber"/>, В6);
    /// точность — по <see cref="ReportValue{T}.Decimals"/> величины (В9,
    /// спека §7.3), формат таблицы — запасной. Нулевое значение → «нет данных»
    /// только при <c>!ZeroIsValid</c> (В2/В14); обороты клапана — дробью
    /// (<see cref="ValveTurnsFraction"/>).
    /// </summary>
    public static class CalculationReportMarkdownRenderHelper
    {
        /// <summary>Формат табличных чисел по умолчанию (величины без Decimals).</summary>
        public const string TableFormat = "N2";

        public static string Value(ReportValue<double> value)
        {
            double? v = value.Value;
            if (!v.HasValue)
            {
                return CalculationReportMarkdownRendererConstants.MissingValue;
            }

            // Нулевое значение → «нет данных» только когда ноль не валиден
            // (В2: заглушки нехранённых величин; В14: ZeroIsValid = true → «0»).
            if (v.Value == 0.0 && !value.ZeroIsValid)
            {
                return CalculationReportMarkdownRendererConstants.MissingValue;
            }

            // Обороты клапана — дробью («8», «8 ½»), как в UI-конвертере.
            if (IsValveTurnsUnit(value.Unit))
            {
                return ValveTurnsFraction.Format(v.Value);
            }

            // Точность величины (В9); без Decimals — формат таблицы (N2).
            return value.Decimals is { } decimals
                ? ReportNumber.Format(v.Value, decimals)
                : ReportNumber.Format(v.Value, TableFormat);
        }

        /// <summary>Единица «обороты клапана» («об», «об.») — рендер дробью.</summary>
        public static bool IsValveTurnsUnit(string unit)
        {
            return unit is "об" or "об.";
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
            RenderScalarTable(sb, rows, Value);
        }

        /// <summary>
        /// Таблица «Параметр | Обозначение | Значение | Единица | Источник»
        /// с внешним форматированием значения (например, подмена «нет данных»,
        /// В2). Форматтер получает то же <see cref="ReportValue{T}"/>, что идёт
        /// в строку — числа шагов и таблиц не расходятся по построению.
        /// </summary>
        public static void RenderScalarTable(
            StringBuilder sb,
            IEnumerable<(string Name, ReportValue<double> Value)> rows,
            Func<ReportValue<double>, string> formatValue)
        {
            sb.AppendLine("| Параметр | Обозначение | Значение | Единица | Источник |");
            sb.AppendLine("| --- | --- | --- | --- | --- |");
            foreach (var (name, value) in rows)
            {
                sb.AppendLine($"| {EscapeCell(name)} | {EscapeCell(value.SourceDetail)} | {formatValue(value)} | {EscapeCell(value.Unit)} | {Source(value)} |");
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
