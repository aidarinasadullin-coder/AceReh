using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SnowMeltingCalculator.AttachedProperties
{
    /// <summary>
    /// Attached property перехода контента (Ф7.4 редизайна): при смене
    /// <see cref="ContentControl.Content"/> проигрывает короткий fade/slide
    /// (180 мс — план Ф7 «150–200 мс») над новым контентом. Свой код вместо
    /// библиотек — XamlFlair reject (заморожен, утечка хэндлов; журнал
    /// решений п.11).
    ///
    /// Применение: <c>attached:ContentTransition.Enable="True"</c> на
    /// ContentControl (ModuleContentControl каркаса). Старый контент не
    /// сохраняется — анимируется только появление нового (переходы модулей
    /// каркаса идут через полную замену контента).
    ///
    /// Детали: смена Content отслеживается DependencyPropertyDescriptor —
    /// у ContentControl нет публичного события ContentChanged; slide
    /// применяется только к контенту без собственного RenderTransform
    /// (чужой трансформ не затирается — играет один fade). Элементы при
    /// Opacity=0 остаются в UIA-дереве — FlaUI smoke не зависит от фазы
    /// анимации.
    /// </summary>
    public static class ContentTransition
    {
        /// <summary>Длительность перехода (план Ф7: 150–200 мс).</summary>
        private static readonly Duration TransitionDuration = new(TimeSpan.FromMilliseconds(180));

        /// <summary>Стартовое смещение по вертикали, px.</summary>
        private const double SlideOffset = 12;

        private static readonly DependencyPropertyDescriptor ContentPropertyDescriptor =
            DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(ContentTransition),
                new PropertyMetadata(false, OnEnableChanged));

        /// <summary>Включить переход контента у ContentControl.</summary>
        public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);

        public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ContentControl control)
            {
                return;
            }

            control.Loaded -= OnControlLoaded;
            ContentPropertyDescriptor.RemoveValueChanged(control, OnContentChanged);

            if ((bool)e.NewValue)
            {
                // Первый контент (стартовая вьюха) тоже проигрывает переход.
                control.Loaded += OnControlLoaded;
                ContentPropertyDescriptor.AddValueChanged(control, OnContentChanged);
            }
        }

        private static void OnControlLoaded(object sender, EventArgs e)
        {
            if (sender is ContentControl { Content: not null } control)
            {
                PlayTransition(control);
            }
        }

        private static void OnContentChanged(object? sender, EventArgs e)
        {
            if (sender is ContentControl { Content: not null } control)
            {
                PlayTransition(control);
            }
        }

        /// <summary>
        /// Fade (Opacity 0→1) и, если контент не держит собственный
        /// RenderTransform, slide (TranslateY 12→0). Opacity/трансформ
        /// задаются на самом контенте — окно и соседние слои не затрагиваются.
        /// </summary>
        private static void PlayTransition(ContentControl control)
        {
            if (control.Content is not UIElement content)
            {
                return;
            }

            // Сброс незавершённой анимации предыдущего перехода.
            content.BeginAnimation(UIElement.OpacityProperty, null);

            var fade = new DoubleAnimation(0d, 1d, TransitionDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            content.BeginAnimation(UIElement.OpacityProperty, fade);

            if (content.RenderTransform is null or TranslateTransform)
            {
                var translate = content.RenderTransform as TranslateTransform ?? new TranslateTransform();
                content.RenderTransform = translate;
                translate.BeginAnimation(TranslateTransform.YProperty, null);

                var slide = new DoubleAnimation(SlideOffset, 0d, TransitionDuration)
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                translate.BeginAnimation(TranslateTransform.YProperty, slide);
            }
        }
    }
}
