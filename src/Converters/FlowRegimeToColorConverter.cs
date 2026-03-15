using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Converters
{
    /// <summary>
    /// Конвертер режима течения в цвет
    /// </summary>
    /// <remarks>
    /// Ламинарный режим - зелёный (стабильное течение)
    /// Переходный режим - оранжевый (требует внимания)
    /// Турбулентный режим - синий (нормальный режим для систем отопления)
    /// </remarks>
    public class FlowRegimeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FlowRegime regime)
            {
                return regime switch
                {
                    FlowRegime.Laminar => new SolidColorBrush(Color.FromRgb(46, 125, 50)),    // Зелёный
                    FlowRegime.Transitional => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Оранжевый
                    FlowRegime.Turbulent => new SolidColorBrush(Color.FromRgb(33, 150, 243)),  // Синий
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}