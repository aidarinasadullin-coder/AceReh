using System;

namespace SnowMeltingCalculator.Core
{
    /// <summary>
    /// Дробное представление оборотов балансировочного клапана («8», «8 ½»).
    /// Единственная реализация правила: WPF-конвертер
    /// <c>ValveTurnsToFractionConverter</c> и рендеры ПЗ делегируют сюда.
    /// </summary>
    /// <remarks>
    /// Примеры:
    /// - 0.25 → "¼"
    /// - 0.5 → "½"
    /// - 0.75 → "¾"
    /// - 1.0 → "1"
    /// - 2.25 → "2 ¼"
    /// - 2.5 → "2 ½"
    /// - 2.75 → "2 ¾"
    /// </remarks>
    public static class ValveTurnsFraction
    {
        /// <summary>Отформатировать обороты дробью с округлением до ¼ оборота.</summary>
        public static string Format(double turns)
        {
            // Округлить до 0.25
            turns = Math.Round(turns * 4) / 4;

            // Разделить на целую и дробную части
            int whole = (int)Math.Floor(turns);
            double fraction = turns - whole;

            // Определить дробную часть
            string fractionStr = fraction switch
            {
                0.25 => "¼",
                0.5 => "½",
                0.75 => "¾",
                _ => ""
            };

            // Форматирование результата
            if (whole == 0 && fraction == 0)
            {
                return "0";
            }
            else if (whole == 0)
            {
                return fractionStr;
            }
            else if (fraction == 0)
            {
                return whole.ToString();
            }
            else
            {
                return $"{whole} {fractionStr}";
            }
        }
    }
}
