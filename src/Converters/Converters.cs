using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SnowMeltingCalculator.Core;

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
                    Models.Thermal.OperatingMode.Manual1 => "Своё значение: +1°C. Температура поверхности задана вручную.",
                    Models.Thermal.OperatingMode.Manual2 => "Своё значение: +2°C. Температура поверхности задана вручную.",
                    Models.Thermal.OperatingMode.Manual4 => "Своё значение: +4°C. Температура поверхности задана вручную.",
                    Models.Thermal.OperatingMode.Manual6 => "Своё значение: +6°C. Температура поверхности задана вручную.",
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
    /// Конвертер: человекочитаемая подпись режима работы
    /// (<see cref="Models.Thermal.OperatingModeDisplay.ToDisplayText"/>) —
    /// единый источник правил с Results и PDF (план 2026-09-13, V3).
    /// </summary>
    public class OperatingModeDisplayTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.Thermal.OperatingMode mode)
            {
                return Models.Thermal.OperatingModeDisplay.ToDisplayText(mode);
            }

            return value?.ToString() ?? string.Empty;
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
    /// Конвертер: давление → цвет текста ячейки (эталон renders/03b).
    /// </summary>
    /// <remarks>
    /// Порог задаётся ConverterParameter'ом в единицах значения:
    /// удельные потери (Па/м) — 300; суммарные Δp (Па) — 32000 (канон
    /// HydraulicsConstants.MaxPressureLoss_Pa = 320 мбар, паспортный предел
    /// HKV — ADR-016). Параметр обязан приходить строкой (parameter is string),
    /// поэтому в XAML это литерал "32000" с комментарием-каноном, а не
    /// x:Static-константа. Превышение → красный Brand.Red.Dark,
    /// иначе UnsetValue (нейтральный цвет ячейки — красными остаются
    /// только проблемные значения, как на эталоне).
    /// </remarks>
    public class PressureColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double pressure && parameter is string limitText
                && double.TryParse(limitText, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double limit))
            {
                return pressure > limit
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB6, 0x00, 0x34))
                    : System.Windows.DependencyProperty.UnsetValue;
            }

            return System.Windows.DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: давление из Па в кПа (для KPI-чипов сводки коллектора,
    /// эталон renders/03: значение и единица — раздельные тексты).
    /// NaN (волна 3.5) → прочерк «—»: локализованное «не число» в
    /// ru-RU-биндинге нечитаемо и пугает.
    /// </summary>
    public class PascalToKpaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double pressurePa)
            {
                return double.IsNaN(pressurePa) ? "—" : pressurePa / 1000.0;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Форматирование double со фолбэком «—» для NaN/Infinity (волна 3.5):
    /// WPF-биндинг с ru-RU культурой рендерит NaN как «не число» — в
    /// расчётных полях грида, чипов и инфоблоков заменяем на прочерк.
    /// ConverterParameter — формат (по умолчанию F1); культура приходит
    /// из биндинга (AppCulture pin ru-RU).
    /// </summary>
    public class NaNFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double number)
            {
                if (double.IsNaN(number) || double.IsInfinity(number))
                {
                    return "—";
                }

                var format = parameter as string ?? "F1";
                return number.ToString(format, culture);
            }

            return value ?? "—";
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
                if (double.IsNaN(pressurePa))
                {
                    return "—";
                }

                double pressureMbar = pressurePa / 100.0;

                if (pressurePa >= 1000)
                {
                    double pressureKPa = pressurePa / 1000.0;
                    return $"{(pressureKPa).ToString("F1", AppCulture.Culture)} кПа ({(pressureMbar).ToString("F0", AppCulture.Culture)} мбар)";
                }
                else
                {
                    return $"{(pressurePa).ToString("F0", AppCulture.Culture)} Па ({(pressureMbar).ToString("F2", AppCulture.Culture)} мбар)";
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

    /// <summary>
    /// Конвертер: ширина окна >= порога → Visible, иначе Collapsed.
    /// Порог передаётся параметром (например, 1680 — порог показа панели
    /// «Сводка» в каркасе Фазы 1 редизайна).
    /// </summary>
    public class WidthThresholdToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var width = value is double d ? d : 0;
            var threshold = double.TryParse(parameter as string, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
            return width >= threshold ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
