using System;
using System.IO;
using System.Reflection;
using SnowMeltingCalculator.Services;

namespace SnowMeltingCalculator.Tests.Fixtures
{
    /// <summary>
    /// Единый сброс настроек приложения для тестов (волна «хвосты» 2026-09-20,
    /// замена четырёх локальных копий). Файл настроек резолвится тем же
    /// правилом, что <see cref="AppSettings"/>: в тестах GlobalTestSetup
    /// выставляет <c>SNOWCALC_SETTINGS_DIR</c> в %TEMP%, поэтому реальный
    /// %APPDATA%\SnowMeltingCalculator\settings.json пользователя не трогается.
    /// </summary>
    internal static class ResetAppSettingsHelper
    {
        public static string SettingsPath { get; } = ResolveSettingsPath();

        public static void Reset()
        {
            if (File.Exists(SettingsPath))
            {
                File.Delete(SettingsPath);
            }

            var field = typeof(AppSettings).GetField("_instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, null);
        }

        private static string ResolveSettingsPath()
        {
            var overrideDir = Environment.GetEnvironmentVariable("SNOWCALC_SETTINGS_DIR");
            var baseDir = string.IsNullOrEmpty(overrideDir)
                ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                : overrideDir;
            return Path.Combine(baseDir, "SnowMeltingCalculator", "settings.json");
        }
    }
}
