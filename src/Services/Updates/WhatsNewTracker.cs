// ================================================================================
// REHAU Снеготаяние - Трекер показа «Что нового» (план 1.3, U7)
// ================================================================================

using System;
using System.Reflection;

namespace SnowMeltingCalculator.Services.Updates
{
    /// <summary>
    /// Решение «показывать ли «Что нового» при старте» и фиксация показа.
    /// Только локальное сравнение — сеть при старте не трогается (U2).
    /// </summary>
    public static class WhatsNewTracker
    {
        /// <summary>Версия запущенной сборки в формате «X.Y.Z».</summary>
        public static Version? CurrentAssemblyVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName().Version;
        }

        public static string Normalize(Version version)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        /// <summary>
        /// Показывать, если для текущей версии есть запись каталога И она
        /// ещё не показана. Мусорная/непарсящаяся <paramref
        /// name="shownVersion"/> (повреждённый settings.json) трактуется
        /// как «не показывалось» — фича не глушится (чек R-2026-09-21-02,
        /// находка №9). Даунгрейд (shown новее текущей) — показываем:
        /// пользователь откатился, ему полезно знать эту версию (U7).
        /// </summary>
        public static bool ShouldShow(string? shownVersion, Version current)
        {
            return ShouldShowForEntry(
                shownVersion,
                current,
                WhatsNewCatalog.Find(Normalize(current)) is not null);
        }

        /// <summary>
        /// Чистая логика решения (тестируема без записей каталога —
        /// каталог пуст между релизами).
        /// </summary>
        public static bool ShouldShowForEntry(string? shownVersion, Version current, bool hasEntry)
        {
            if (!hasEntry)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(shownVersion))
            {
                return true;
            }

            return !string.Equals(shownVersion.Trim(), Normalize(current), StringComparison.Ordinal);
        }

        public static void MarkShown(AppSettings settings, Version current)
        {
            settings.WhatsNewShownVersion = Normalize(current);
            settings.Save();
        }
    }
}
