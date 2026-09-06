using System.Globalization;
using SnowMeltingCalculator.Core;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Форматирование чисел детального отчёта (В6): десятичный разделитель —
    /// запятая, разделитель тысяч — пробел. Единственный механизм — каноническая
    /// культура приложения <see cref="AppCulture.Culture"/> (pinned ru-RU,
    /// решение владельца 2026-09-04; Ф8 использует её же в PDF-рендере),
    /// самостоятельные NumberFormatInfo не вводятся.
    /// </summary>
    public static class ReportNumber
    {
        /// <summary>Формат по умолчанию: два знака после разделителя.</summary>
        public const string DefaultFormat = "N2";

        /// <summary>
        /// Отформатировать число по канонической культуре отчёта.
        /// </summary>
        public static string Format(double value, string format = DefaultFormat)
        {
            return value.ToString(format, AppCulture.Culture);
        }

        /// <summary>
        /// Отформатировать число с заданным числом знаков после разделителя.
        /// </summary>
        public static string Format(double value, int decimals)
        {
            return value.ToString("N" + decimals.ToString(CultureInfo.InvariantCulture), AppCulture.Culture);
        }
    }
}
