using System.IO;
using System.Text.Json;

using SnowMeltingCalculator.Services.Logging;
namespace SnowMeltingCalculator.Services
{
    /// <summary>
    /// Настройки приложения
    /// </summary>
    public class AppSettings
    {
        private static readonly string SettingsFilePath = ResolveSettingsFilePath();

        /// <summary>
        /// Каталог настроек: реальный %APPDATA%, либо переменная окружения
        /// <c>SNOWCALC_SETTINGS_DIR</c> (тестовая изоляция, волна «хвосты»
        /// 2026-09-20: тесты не трогают реальный settings.json пользователя —
        /// GlobalTestSetup перенаправляет в %TEMP%).
        /// </summary>
        private static string ResolveSettingsFilePath()
        {
            var overrideDir = Environment.GetEnvironmentVariable("SNOWCALC_SETTINGS_DIR");
            var baseDir = string.IsNullOrEmpty(overrideDir)
                ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                : overrideDir;
            return Path.Combine(baseDir, "SnowMeltingCalculator", "settings.json");
        }

        private static AppSettings? _instance;

        /// <summary>
        /// Экземпляр настроек (Singleton)
        /// </summary>
        public static AppSettings Instance => _instance ??= Load();

        /// <summary>
        /// Признак свёрнутой боковой панели
        /// </summary>
        public bool IsSidebarCollapsed { get; set; }

        /// <summary>
        /// Недавние проекты (MRU, план 1.2 роадмапа post-1.8): пути .smc,
        /// самый свежий первым, ёмкость 10 поддерживает
        /// RecentProjectsService. Отсутствие ключа в settings.json = пустой
        /// список (конвенция ключей AppSettings — план 1.2, §1); возможный
        /// null из повреждённого JSON гасится GetListSafe() сервиса.
        /// </summary>
        public List<string> RecentProjects { get; set; } = new();

        /// <summary>
        /// Загрузить настройки из файла
        /// </summary>
        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex) {
                AppLog.Warn(ex, "AppSettings.Load");
                // Игнорируем ошибки при загрузке настроек
            }

            return new AppSettings();
        }

        /// <summary>
        /// Сохранить настройки в файл
        /// </summary>
        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex) {
                AppLog.Warn(ex, "AppSettings.Save");
                // Игнорируем ошибки при сохранении настроек
            }
        }
    }
}