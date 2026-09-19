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
        [SetUp]
        public void SetUp()
        {
            // Волна «хвосты» 2026-09-20: единый хелпер; путь резолвится как у
            // AppSettings — в тестах это %TEMP%-песочница (SNOWCALC_SETTINGS_DIR),
            // реальный settings.json пользователя не трогается.
            Fixtures.ResetAppSettingsHelper.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Fixtures.ResetAppSettingsHelper.Reset();
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
            Assert.That(File.Exists(Fixtures.ResetAppSettingsHelper.SettingsPath), Is.True);
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
            var directory = Path.GetDirectoryName(Fixtures.ResetAppSettingsHelper.SettingsPath);
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
            Assert.That(File.Exists(Fixtures.ResetAppSettingsHelper.SettingsPath), Is.True);
        }
    }
}