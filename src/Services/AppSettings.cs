using System.IO;
using System.Text.Json;

namespace SnowMeltingCalculator.Services
{
    /// <summary>
    /// Настройки приложения
    /// </summary>
    public class AppSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SnowMeltingCalculator",
            "settings.json");

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
            catch
            {
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
            catch
            {
                // Игнорируем ошибки при сохранении настроек
            }
        }
    }
}