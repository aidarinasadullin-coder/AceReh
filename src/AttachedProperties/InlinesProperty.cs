using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SnowMeltingCalculator.AttachedProperties
{
    /// <summary>
    /// Attached Property для привязки IEnumerable&lt;Inline&gt; к TextBlock.Inlines
    /// Позволяет использовать привязку для динамического создания форматированного текста
    /// </summary>
    public static class InlinesProperty
    {
        /// <summary>
        /// DependencyProperty для привязки Inlines
        /// </summary>
        public static readonly DependencyProperty InlinesAttachedProperty =
            DependencyProperty.RegisterAttached(
                "Inlines",
                typeof(IEnumerable<Inline>),
                typeof(InlinesProperty),
                new PropertyMetadata(null, OnInlinesChanged));

        /// <summary>
        /// Получить значение Inlines
        /// </summary>
        public static IEnumerable<Inline>? GetInlines(DependencyObject obj)
        {
            return (IEnumerable<Inline>?)obj.GetValue(InlinesAttachedProperty);
        }

        /// <summary>
        /// Установить значение Inlines
        /// </summary>
        public static void SetInlines(DependencyObject obj, IEnumerable<Inline>? value)
        {
            obj.SetValue(InlinesAttachedProperty, value);
        }

        /// <summary>
        /// Обработчик изменения Inlines
        /// </summary>
        private static void OnInlinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock textBlock)
                return;

            // Очистить существующие Inlines
            textBlock.Inlines.Clear();

            // Добавить новые Inlines
            if (e.NewValue is IEnumerable<Inline> inlines)
            {
                foreach (var inline in inlines)
                {
                    textBlock.Inlines.Add(inline);
                }
            }
        }
    }
}