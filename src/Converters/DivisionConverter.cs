using System;
using System.Globalization;
using System.Windows.Data;

namespace SnowMeltingCalculator.Converters
{
    /// <summary>
    /// Конвертер для деления значения на указанный делитель
    /// </summary>
    public class DivisionConverter : IValueConverter
    {
        public double Divisor { get; set; } = 1.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return doubleValue / Divisor;
            }
            if (value is int intValue)
            {
                return intValue / Divisor;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return doubleValue * Divisor;
            }
            if (value is int intValue)
            {
                return intValue * Divisor;
            }
            return value;
        }
    }
}
