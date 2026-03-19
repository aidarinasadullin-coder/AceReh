using System.IO;
using NUnit.Framework;
using SnowMeltingCalculator.Services;

namespace SnowMeltingCalculator.Tests.Services
{
    /// <summary>
    /// Тесты для AppSettings
    /// </summary>
    [TestFixture]
    public class AppSettingsTests
    {
        private string _settingsPath;

        [SetUp]
        public void SetUp()
        {
            // Сбрасываем singleton для каждого теста
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SnowMeltingCalculator",
                "settings.json");
            
            // Удаляем файл настроек перед каждым тестом
            if (File.Exists(_settingsPath))
            {
                File.Delete(_settingsPath);
            }
            
            // Сбрасываем singleton через рефлексию
            var field = typeof(AppSettings).GetField("_instance", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(null, null);
        }

        [TearDown]
        public void TearDown()
        {
            // Удаляем файл настроек после каждого теста
            if (File.Exists(_settingsPath))
            {
                File.Delete(_settingsPath);
            }
        }

        [Test]
        public void Instance_ReturnsSingleton()
        {
            // Arrange & Act
            var settings1 = AppSettings.Instance;
            var settings2 = AppSettings.Instance;

            // Assert
            Assert.That(settings1, Is.SameAs(settings2));
        }

        [Test]
        public void IsSidebarCollapsed_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var settings = AppSettings.Instance;

            // Assert
            Assert.That(settings.IsSidebarCollapsed, Is.False);
        }

        [Test]
        public void Save_CreatesSettingsFile()
        {
            // Arrange
            var settings = AppSettings.Instance;
            settings.IsSidebarCollapsed = true;

            // Act
            settings.Save();

            // Assert
            Assert.That(File.Exists(_settingsPath), Is.True);
        }

        [Test]
        public void Save_PersistsIsSidebarCollapsed()
        {
            // Arrange
            var settings = AppSettings.Instance;
            settings.IsSidebarCollapsed = true;
            settings.Save();

            // Сбрасываем singleton
            var field = typeof(AppSettings).GetField("_instance",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(null, null);

            // Act
            var loadedSettings = AppSettings.Instance;

            // Assert
            Assert.That(loadedSettings.IsSidebarCollapsed, Is.True);
        }

        [Test]
        public void Save_WhenCollapsedFalse_PersistsFalse()
        {
            // Arrange
            var settings = AppSettings.Instance;
            settings.IsSidebarCollapsed = false;
            settings.Save();

            // Сбрасываем singleton
            var field = typeof(AppSettings).GetField("_instance",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(null, null);

            // Act
            var loadedSettings = AppSettings.Instance;

            // Assert
            Assert.That(loadedSettings.IsSidebarCollapsed, Is.False);
        }

        [Test]
        public void Load_WhenFileNotExists_ReturnsNewInstance()
        {
            // Arrange - файл не существует (удалён в SetUp)

            // Сбрасываем singleton
            var field = typeof(AppSettings).GetField("_instance",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(null, null);

            // Act
            var settings = AppSettings.Instance;

            // Assert
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.IsSidebarCollapsed, Is.False);
        }

        [Test]
        public void Save_CreatesDirectoryIfNotExists()
        {
            // Arrange
            var directory = Path.GetDirectoryName(_settingsPath);
            if (Directory.Exists(directory!))
            {
                Directory.Delete(directory, true);
            }

            var settings = AppSettings.Instance;
            settings.IsSidebarCollapsed = true;

            // Act
            settings.Save();

            // Assert
            Assert.That(Directory.Exists(directory), Is.True);
            Assert.That(File.Exists(_settingsPath), Is.True);
        }
    }
}