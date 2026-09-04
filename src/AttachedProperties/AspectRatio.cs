using System;
using System.Windows;

namespace SnowMeltingCalculator.AttachedProperties
{
    /// <summary>
    /// Attached Property «фиксированное соотношение сторон» (Фаза 2 редизайна):
    /// удерживает Height = ActualWidth / Ratio у любого FrameworkElement.
    ///
    /// Считается Height, а не Width (решение ревью Ф2, ADR-006): сеттер Width
    /// перетирал бы растяжение панелью (Grid/UniformGrid/wrap-панели), тогда
    /// как Height элементу никто извне не задаёт. Рекурсия SizeChanged→Height
    /// (высота не влияет на ширину при фиксированной раскладке) гасится
    /// порогом 0.5 px.
    /// </summary>
    public static class AspectRatio
    {
        /// <summary>Толеранс сравнения высот, отсекающий цикл SizeChanged.</summary>
        private const double RecursionTolerance = 0.5;

        public static readonly DependencyProperty RatioProperty =
            DependencyProperty.RegisterAttached(
                "Ratio",
                typeof(double),
                typeof(AspectRatio),
                new PropertyMetadata(0d, OnRatioChanged));

        /// <summary>Соотношение сторон (ширина / высота); 0 — отключено.</summary>
        public static double GetRatio(DependencyObject obj) => (double)obj.GetValue(RatioProperty);

        public static void SetRatio(DependencyObject obj, double value) => obj.SetValue(RatioProperty, value);

        private static void OnRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
            {
                return;
            }

            // Отписка от предыдущего экземпляра-обёртки не нужна: обработчик
            // статический, проверяет актуальное значение Ratio перед записью.
            element.SizeChanged -= OnElementSizeChanged;

            if (!IsUsableRatio((double)e.NewValue))
            {
                return;
            }

            element.SizeChanged += OnElementSizeChanged;
            ApplyRatio(element);
        }

        private static void OnElementSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                ApplyRatio(element);
            }
        }

        private static void ApplyRatio(FrameworkElement element)
        {
            var ratio = GetRatio(element);
            if (!IsUsableRatio(ratio) || double.IsNaN(element.ActualWidth) || element.ActualWidth <= 0)
            {
                return;
            }

            var targetHeight = element.ActualWidth / ratio;
            if (Math.Abs(element.ActualHeight - targetHeight) < RecursionTolerance)
            {
                return;
            }

            element.Height = targetHeight;
        }

        private static bool IsUsableRatio(double ratio) =>
            !double.IsNaN(ratio) && !double.IsInfinity(ratio) && ratio > 0;
    }
}
