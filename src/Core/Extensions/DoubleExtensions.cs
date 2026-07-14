using System;

namespace SnowMeltingCalculator.Core.Extensions
{
    /// <summary>
    /// Расширения для типа double
    /// </summary>
    public static class DoubleExtensions
    {
        #region Округление

        /// <summary>
        /// Округлить до указанного количества знаков после запятой
        /// </summary>
        /// <param name="value">Значение для округления</param>
        /// <param name="decimalPlaces">Количество знаков после запятой</param>
        /// <returns>Округлённое значение</returns>
        public static double RoundTo(this double value, int decimalPlaces)
        {
            return Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Округлить до одного знака после запятой
        /// </summary>
        /// <param name="value">Значение для округления</param>
        /// <returns>Округлённое значение</returns>
        public static double RoundTo1(this double value)
        {
            return Math.Round(value, 1, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Округлить до двух знаков после запятой
        /// </summary>
        /// <param name="value">Значение для округления</param>
        /// <returns>Округлённое значение</returns>
        public static double RoundTo2(this double value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Округлить до трёх знаков после запятой
        /// </summary>
        /// <param name="value">Значение для округления</param>
        /// <returns>Округлённое значение</returns>
        public static double RoundTo3(this double value)
        {
            return Math.Round(value, 3, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Округлить до целого числа
        /// </summary>
        /// <param name="value">Значение для округления</param>
        /// <returns>Округлённое значение</returns>
        public static int RoundToInt(this double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        #endregion

        #region Сравнение

        /// <summary>
        /// Проверить равенство с учётом погрешности
        /// </summary>
        /// <param name="value">Значение для сравнения</param>
        /// <param name="other">Другое значение</param>
        /// <param name="epsilon">Погрешность (по умолчанию 1e-9)</param>
        /// <returns>true если значения равны с учётом погрешности</returns>
        public static bool IsEqual(this double value, double other, double epsilon = 1e-9)
        {
            return Math.Abs(value - other) < epsilon;
        }

        /// <summary>
        /// Проверить, что значение близко к нулю
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <param name="epsilon">Погрешность (по умолчанию 1e-9)</param>
        /// <returns>true если значение близко к нулю</returns>
        public static bool IsZero(this double value, double epsilon = 1e-9)
        {
            return Math.Abs(value) < epsilon;
        }

        /// <summary>
        /// Проверить, что значение положительное
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <returns>true если значение > 0</returns>
        public static bool IsPositive(this double value)
        {
            return value > 0;
        }

        /// <summary>
        /// Проверить, что значение отрицательное
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <returns>true если значение &lt; 0</returns>
        public static bool IsNegative(this double value)
        {
            return value < 0;
        }

        /// <summary>
        /// Проверить, что значение неотрицательное
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <returns>true если значение >= 0</returns>
        public static bool IsNonNegative(this double value)
        {
            return value >= 0;
        }

        /// <summary>
        /// Проверить, что значение в заданном диапазоне
        /// </summary>
        /// <param name="value">Значение для проверки</param>
        /// <param name="min">Минимальное значение (включительно)</param>
        /// <param name="max">Максимальное значение (включительно)</param>
        /// <returns>true если значение в диапазоне [min, max]</returns>
        public static bool IsInRange(this double value, double min, double max)
        {
            return value >= min && value <= max;
        }

        #endregion

        #region Конвертация

        /// <summary>
        /// Конвертировать мм в м
        /// </summary>
        /// <param name="value">Значение в мм</param>
        /// <returns>Значение в м</returns>
        public static double MmToM(this double value)
        {
            return value / 1000.0;
        }

        /// <summary>
        /// Конвертировать м в мм
        /// </summary>
        /// <param name="value">Значение в м</param>
        /// <returns>Значение в мм</returns>
        public static double MToMm(this double value)
        {
            return value * 1000.0;
        }

        /// <summary>
        /// Конвертировать Па в мбар
        /// </summary>
        /// <param name="value">Значение в Па</param>
        /// <returns>Значение в мбар</returns>
        public static double PaToMbar(this double value)
        {
            return value / 100.0;
        }

        /// <summary>
        /// Конвертировать мбар в Па
        /// </summary>
        /// <param name="value">Значение в мбар</param>
        /// <returns>Значение в Па</returns>
        public static double MbarToPa(this double value)
        {
            return value * 100.0;
        }

        /// <summary>
        /// Конвертировать л/ч в м³/ч
        /// </summary>
        /// <param name="value">Значение в л/ч</param>
        /// <returns>Значение в м³/ч</returns>
        public static double LhToM3h(this double value)
        {
            return value / 1000.0;
        }

        /// <summary>
        /// Конвертировать м³/ч в л/ч
        /// </summary>
        /// <param name="value">Значение в м³/ч</param>
        /// <returns>Значение в л/ч</returns>
        public static double M3hToLh(this double value)
        {
            return value * 1000.0;
        }

        /// <summary>
        /// Конвертировать °C в K
        /// </summary>
        /// <param name="value">Значение в °C</param>
        /// <returns>Значение в K</returns>
        public static double CelsiusToKelvin(this double value)
        {
            return value + 273.15;
        }

        /// <summary>
        /// Конвертировать K в °C
        /// </summary>
        /// <param name="value">Значение в K</param>
        /// <returns>Значение в °C</returns>
        public static double KelvinToCelsius(this double value)
        {
            return value - 273.15;
        }

        #endregion

        #region Ограничение

        /// <summary>
        /// Ограничить значение в заданном диапазоне
        /// </summary>
        /// <param name="value">Значение для ограничения</param>
        /// <param name="min">Минимальное значение</param>
        /// <param name="max">Максимальное значение</param>
        /// <returns>Значение в диапазоне [min, max]</returns>
        public static double Clamp(this double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        /// <summary>
        /// Ограничить значение снизу
        /// </summary>
        /// <param name="value">Значение для ограничения</param>
        /// <param name="min">Минимальное значение</param>
        /// <returns>Значение >= min</returns>
        public static double ClampMin(this double value, double min)
        {
            return Math.Max(min, value);
        }

        /// <summary>
        /// Ограничить значение сверху
        /// </summary>
        /// <param name="value">Значение для ограничения</param>
        /// <param name="max">Максимальное значение</param>
        /// <returns>Значение &lt;= max</returns>
        public static double ClampMax(this double value, double max)
        {
            return Math.Min(max, value);
        }

        #endregion
    }
}