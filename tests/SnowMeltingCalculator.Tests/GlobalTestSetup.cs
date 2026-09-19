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
        }
    }
}
