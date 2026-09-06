using System.Reflection;
using System.Windows;

namespace SnowMeltingCalculator
{
    /// <summary>
    /// Лёгкий диалог «О программе» (Ф7.2, рендер 06b): версия, данные,
    /// слоган. Модальный, без собственного состояния.
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            VersionText.Text = version is null
                ? string.Empty
                : $"v{version.Major}.{version.Minor}.{version.Build}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
