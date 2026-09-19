using System.IO;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests
{
    /// <summary>
    /// Глобальная настройка тестового прогона (волна 4, D2): журнал
    /// приложения в тестах уходит во временный каталог — тесты и CI не
    /// пишут в реальный %LOCALAPPDATA%\SnowMeltingCalculator\logs
    /// (находка №3 ревью волны 4).
    /// </summary>
    [SetUpFixture]
    public class GlobalTestSetup
    {
        [OneTimeSetUp]
        public void RedirectLogRoot()
        {
            var logRoot = Path.Combine(Path.GetTempPath(), "snowcalc-test-logs");
            Directory.CreateDirectory(logRoot);
            Environment.SetEnvironmentVariable("SNOWCALC_LOG_DIR", logRoot);

            // Волна «хвосты» 2026-09-20: настройки (AppSettings) в тестах
            // изолируются так же, как журнал, — реальный
            // %APPDATA%\SnowMeltingCalculator\settings.json не трогается.
            var settingsDir = Path.Combine(Path.GetTempPath(), "snowcalc-test-settings");
            Directory.CreateDirectory(settingsDir);
            Environment.SetEnvironmentVariable("SNOWCALC_SETTINGS_DIR", settingsDir);
        }
    }
}
