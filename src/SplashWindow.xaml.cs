using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Animation;

namespace SnowMeltingCalculator
{
    /// <summary>
    /// Сплэш при старте (Ф7.2, рендер 06): закрывает загрузку климата и
    /// материалов; показывается ≥ <see cref="MinSplashDuration"/>, гасится
    /// фейдом и закрывается после отображения главного окна. Побочных
    /// состояний не хранит — только собственная жизнь окна.
    /// </summary>
    public partial class SplashWindow : Window
    {
        /// <summary>Суммарное время показа сплэша (рендер 06: 1–1,5 с).</summary>
        public static readonly TimeSpan MinSplashDuration = TimeSpan.FromMilliseconds(1200);

        /// <summary>Длительность fade-out при закрытии (Ф7: 150–200 мс).</summary>
        private static readonly TimeSpan FadeOutDuration = TimeSpan.FromMilliseconds(180);

        public SplashWindow()
        {
            InitializeComponent();
            VersionText.Text = FormatAppVersion();
        }

        /// <summary>
        /// Версия приложения из сборки («v1.1.2» — по csproj Version).
        /// </summary>
        internal static string FormatAppVersion()
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            return version is null ? string.Empty : $"v{version.Major}.{version.Minor}.{version.Build}";
        }

        /// <summary>
        /// Держать окно открытым ещё <paramref name="remaining"/> и закрыть
        /// с fade-out. Вызывается из App.OnStartup после Show главного окна,
        /// поэтому «последнее окно» не закрывается до появления MainWindow.
        /// </summary>
        public async System.Threading.Tasks.Task CloseAfterDelayAsync(TimeSpan remaining)
        {
            if (remaining > TimeSpan.Zero)
            {
                await System.Threading.Tasks.Task.Delay(remaining);
            }

            var fade = new DoubleAnimation(1d, 0d, FadeOutDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fade);

            await System.Threading.Tasks.Task.Delay(FadeOutDuration);
            Close();
        }
    }
}
