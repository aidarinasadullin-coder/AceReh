using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SnowMeltingCalculator.Converters
{
    /// <summary>
    /// Конвертер: null → Collapsed, не-null → Visible
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: пустая строка → Collapsed, не пустая → Visible
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: bool → Visibility (true → Visible, false → Collapsed)
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: bool → Visibility (true → Collapsed, false → Visible)
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: инверсия bool (true → false, false → true)
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }
    }

    /// <summary>
    /// Конвертер: получение описания enum через атрибут Description или Display
    /// </summary>
    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            var enumValue = value;
            var field = enumValue.GetType().GetField(enumValue.ToString()!);

            if (field == null)
                return enumValue.ToString() ?? string.Empty;

            // Пытаемся получить атрибут Display
            var displayAttr = field.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DataAnnotations.DisplayAttribute;

            if (displayAttr != null)
                return displayAttr.GetName() ?? enumValue.ToString() ?? string.Empty;

            // Пытаемся получить атрибут Description
            var descriptionAttr = field.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DescriptionAttribute;

            if (descriptionAttr != null)
                return descriptionAttr.Description;

            return enumValue.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: получение описания режима работы OperatingMode
    /// </summary>
    public class OperatingModeDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value is Models.Thermal.OperatingMode mode)
            {
                return mode switch
                {
                    Models.Thermal.OperatingMode.AntiIcing => "Минимальная мощность, температура поверхности +3°C. Подходит для предотвращения образования льда.",
                    Models.Thermal.OperatingMode.Melting => "Стандартный режим, температура поверхности +5°C. Оптимальный баланс мощности и эффективности.",
                    Models.Thermal.OperatingMode.Intensive => "Максимальная мощность, температура поверхности +7°C. Для интенсивного снегопада.",
                    _ => mode.ToString()
                };
            }

            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: null → false, не-null → true
    /// </summary>
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: bool → PackIcon Kind (true → Check, false → Alert)
    /// </summary>
    public class BoolToAlertIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? "Check" : "Alert";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: bool → Brush (true → Green, false → Red)
    /// </summary>
    public class BoolToValidationColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
            {
                return new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0, 128, 0)); // Зелёный
            }
            return new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 0, 0)); // Красный
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: Material → Color (HEX строка)
    /// </summary>
    public class MaterialToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Construction.Material material)
            {
                return material.GetColor();
            }
            return "#CCCCCC";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: HydraulicMode → Visibility
    /// </summary>
    /// <remarks>
    /// Используется для переключателя режима (Рабочая/Расчётная температура).
    /// Параметр: "Operating" — виден, если режим OperatingTemperature
    ///           "Design" — виден, если режим DesignTemperature
    /// </remarks>
    public class HydraulicModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Hydraulics.HydraulicMode mode && parameter is string param)
            {
                // Если параметр "Operating", показываем кнопку "Расчётная температура"
                // когда текущий режим OperatingTemperature (нужно переключиться на Design)
                if (param == "Operating")
                {
                    return mode == Models.Hydraulics.HydraulicMode.OperatingTemperature
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
                // Если параметр "Design", показываем кнопку "Рабочая температура"
                // когда текущий режим DesignTemperature (нужно переключиться на Operating)
                if (param == "Design")
                {
                    return mode == Models.Hydraulics.HydraulicMode.DesignTemperature
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: bool → Tooltip для кнопки сворачивания боковой панели
    /// </summary>
    /// <remarks>
    /// true (свёрнута) → "Развернуть панель (Ctrl+B)"
    /// false (развёрнута) → "Свернуть панель (Ctrl+B)"
    /// </remarks>
    public class SidebarTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCollapsed)
            {
                return isCollapsed
                    ? "Развернуть панель (Ctrl+B)"
                    : "Свернуть панель (Ctrl+B)";
            }
            return "Свернуть панель (Ctrl+B)";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: давление (мбар) → цвет текста
    /// </summary>
    /// <remarks>
    /// Давление ≤ 320 мбар → зелёный (#2E7D32)
    /// Давление > 320 мбар → красный (#D32F2F)
    /// </remarks>
    public class PressureColorConverter : IValueConverter
    {
        private const double PressureLimit = 320.0; // мбар

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double pressure)
            {
                return pressure > PressureLimit
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(211, 47, 47))  // Красный
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50)); // Зелёный
            }
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: пустые значения → тире
    /// </summary>
    /// <remarks>
    /// null или 0 → "—"
    /// Иначе — исходное значение
    /// Используется для отображения пустых контуров в таблице
    /// </remarks>
    public class EmptyValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "—";

            if (value is double d && d == 0)
                return "—";

            if (value is int i && i == 0)
                return "—";

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: HydraulicMode → Background Brush для табло режима
    /// </summary>
    /// <remarks>
    /// Параметр: "Operating" или "Design"
    /// Возвращает синий фон (#2196F3), если режим совпадает с параметром
    /// Возвращает прозрачный фон, если режим не совпадает
    /// </remarks>
    public class ModeToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Hydraulics.HydraulicMode mode && parameter is string param)
            {
                bool isSelected = (param == "Operating" && mode == Models.Hydraulics.HydraulicMode.OperatingTemperature) ||
                                  (param == "Design" && mode == Models.Hydraulics.HydraulicMode.DesignTemperature);
                return isSelected
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x21, 0x96, 0xF3)) // Синий
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
            }
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: HydraulicMode → Border Brush для табло режима
    /// </summary>
    /// <remarks>
    /// Параметр: "Operating" или "Design"
    /// Возвращает тёмно-синий (#1976D2), если режим совпадает с параметром
    /// Возвращает серый, если режим не совпадает
    /// </remarks>
    public class ModeToBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Hydraulics.HydraulicMode mode && parameter is string param)
            {
                bool isSelected = (param == "Operating" && mode == Models.Hydraulics.HydraulicMode.OperatingTemperature) ||
                                  (param == "Design" && mode == Models.Hydraulics.HydraulicMode.DesignTemperature);
                return isSelected
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x19, 0x76, 0xD2)) // Тёмно-синий
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
            }
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: давление (Па) → форматированная строка
    /// </summary>
    /// <remarks>
    /// Если давление < 1000 Па — выводит в Па: "XXX Па (X.XX мбар)"
    /// Если давление ≥ 1000 Па — выводит в кПа: "XX.X кПа (XXX мбар)"
    /// </remarks>
    public class PressureToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double pressurePa)
            {
                double pressureMbar = pressurePa / 100.0;

                if (pressurePa >= 1000)
                {
                    double pressureKPa = pressurePa / 1000.0;
                    return $"{pressureKPa:F1} кПа ({pressureMbar:F0} мбар)";
                }
                else
                {
                    return $"{pressurePa:F0} Па ({pressureMbar:F2} мбар)";
                }
            }
            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: Order (0, 1, 2...) → номер слоя (1, 2, 3...)
    /// </summary>
    public class OrderToNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int order)
            {
                return (order + 1).ToString();
            }
            return "1";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}