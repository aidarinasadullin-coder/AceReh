using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace SnowMeltingCalculator.Converters
{
    /// <summary>
    /// Конвертер для преобразования текста с подсветкой в XAML Run элементы
    /// Формат: "до**совпадение**после" — подсвеченная часть между **
    /// </summary>
    public class CityMatchToHighlightConverter : IValueConverter
    {
        /// <summary>
        /// Кисть для подсвеченного текста (бирюзовый REHAU)
        /// </summary>
        private static readonly SolidColorBrush HighlightBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xC7, 0xB5));

        /// <summary>
        /// Кисть для обычного текста (чёрный REHAU)
        /// </summary>
        private static readonly SolidColorBrush NormalBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x1D, 0x1B));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string highlightedText || string.IsNullOrEmpty(highlightedText))
            {
                return new List<Inline>();
            }

            // Разбор формата "до**совпадение**после"
            var inlines = new List<Inline>();
            var parts = highlightedText.Split(new[] { "**" }, StringSplitOptions.None);

            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;

                var run = new Run { Text = parts[i] };
                
                // Нечётные части — подсвеченные
                if (i % 2 == 1)
                {
                    run.FontWeight = FontWeights.Bold;
                    run.Foreground = HighlightBrush;
                }
                else
                {
                    run.Foreground = NormalBrush;
                }
                
                inlines.Add(run);
            }

            return inlines;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Создать TextBlock с подсвеченным текстом
        /// </summary>
        /// <param name="highlightedText">Текст в формате "до**совпадение**после"</param>
        /// <param name="highlightBrush">Кисть для подсветки (опционально)</param>
        /// <param name="normalBrush">Кисть для обычного текста (опционально)</param>
        /// <returns>TextBlock с подсвеченными Run элементами</returns>
        public static System.Windows.Controls.TextBlock CreateHighlightedTextBlock(
            string highlightedText,
            Brush? highlightBrush = null,
            Brush? normalBrush = null)
        {
            var textBlock = new System.Windows.Controls.TextBlock();
            var inlines = CreateInlines(highlightedText, highlightBrush, normalBrush);
            
            foreach (var inline in inlines)
            {
                textBlock.Inlines.Add(inline);
            }
            
            return textBlock;
        }

        /// <summary>
        /// Создать список Inline элементов из подсвеченного текста
        /// </summary>
        /// <param name="highlightedText">Текст в формате "до**совпадение**после"</param>
        /// <param name="highlightBrush">Кисть для подсветки (опционально)</param>
        /// <param name="normalBrush">Кисть для обычного текста (опционально)</param>
        /// <returns>Список Inline элементов</returns>
        public static List<Inline> CreateInlines(
            string highlightedText,
            Brush? highlightBrush = null,
            Brush? normalBrush = null)
        {
            var inlines = new List<Inline>();
            
            if (string.IsNullOrEmpty(highlightedText))
            {
                return inlines;
            }

            var hBrush = highlightBrush ?? HighlightBrush;
            var nBrush = normalBrush ?? NormalBrush;

            var parts = highlightedText.Split(new[] { "**" }, StringSplitOptions.None);

            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;

                var run = new Run { Text = parts[i] };
                
                // Нечётные части — подсвеченные
                if (i % 2 == 1)
                {
                    run.FontWeight = FontWeights.Bold;
                    run.Foreground = hBrush;
                }
                else
                {
                    run.Foreground = nBrush;
                }
                
                inlines.Add(run);
            }

            return inlines;
        }
    }
}