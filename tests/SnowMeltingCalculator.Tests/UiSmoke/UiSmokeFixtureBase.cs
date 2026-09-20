// ================================================================================
// Фаза 1Б редизайна — база smoke-фикстур: один запуск приложения на фикстуру.
// ================================================================================

using System;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.UiSmoke;

/// <summary>
/// База UiSmoke-фикстур: запуск реального приложения один раз на фикстуру
/// (старт SelfContained-exe долог), грейсфул-закрытие в teardown.
/// Все тесты наследника попадают в категорию UiSmoke.
/// </summary>
[Category("UiSmoke")]
[Apartment(ApartmentState.STA)]
public abstract class UiSmokeFixtureBase
{
    protected UiSmokeApplication App { get; private set; } = null!;
    /// <summary>Аргументы командной строки запуска exe (пусто — старт без проекта).</summary>
    protected virtual string[] LaunchArguments => Array.Empty<string>();

    [OneTimeSetUp]
    public void LaunchApplication()
    {
        PreseedWhatsNewShown();
        CleanupAutosaveSnapshot();
        App = UiSmokeApplication.Launch(LaunchArguments);
    }

    /// <summary>
    /// Pre-seed WhatsNewShownVersion в песочницу настроек (план 1.3, чек
    /// R-2026-09-21-02 №2): иначе после бампа с записью в WhatsNewCatalog
    /// exe покажет модальный диалог при старте, главное окно задизейблится
    /// владельцем — смоук-сценарии красные. exe наследует env тестового
    /// процесса (SNOWCALC_SETTINGS_DIR из GlobalTestSetup).
    /// </summary>
    private static void PreseedWhatsNewShown()
    {
        var version = typeof(SnowMeltingCalculator.App).Assembly.GetName().Version;
        if (version is null)
        {
            return;
        }

        var settingsPath = Fixtures.ResetAppSettingsHelper.SettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath,
            "{ \"IsSidebarCollapsed\": false, \"WhatsNewShownVersion\": \""
            + $"{version.Major}.{version.Minor}.{version.Build}\" }}");
    }

    /// <summary>
    /// Убрать служебный автоснапшот из песочницы (план 3.1, чек
    /// R-2026-09-21-04 №10/№8): смоук-exe может завершиться некротно —
    /// оставшийся autosave.smc поднял бы вопрос «Восстановить проект?»
    /// при следующем запуске и задизейблил главное окно.
    /// </summary>
    private static void CleanupAutosaveSnapshot()
    {
        var settingsPath = Fixtures.ResetAppSettingsHelper.SettingsPath;
        var dir = Path.GetDirectoryName(settingsPath);
        if (string.IsNullOrEmpty(dir))
        {
            return;
        }

        foreach (var stale in new[] { "autosave.smc", "autosave.smc.bak", "autosave.tmp" })
        {
            var path = Path.Combine(dir, stale);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [OneTimeTearDown]
    public void CloseApplication()
    {
        // OneTimeTearDown выполняется и при провале OneTimeSetUp (App ещё null)
        App?.Dispose();
    }
}
