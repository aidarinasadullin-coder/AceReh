// ================================================================================
// Тесты трекера показа «Что нового» (план 1.3, U7)
// ================================================================================

using System;

using NUnit.Framework;

using SnowMeltingCalculator.Services;
using SnowMeltingCalculator.Services.Updates;

namespace SnowMeltingCalculator.Tests.Services.Updates
{
    [TestFixture]
    public class WhatsNewTrackerTests
    {
        private static readonly Version Current = new(1, 10, 0);

        [Test]
        public void ShouldShowForEntry_ShownNull_True()
        {
            Assert.That(
                WhatsNewTracker.ShouldShowForEntry(null, Current, hasEntry: true),
                Is.True,
                "первый запуск версии — показать");
        }

        [Test]
        public void ShouldShowForEntry_ShownEqualsCurrent_False()
        {
            Assert.That(
                WhatsNewTracker.ShouldShowForEntry("1.10.0", Current, hasEntry: true),
                Is.False,
                "уже показано этой версии");
        }

        [Test]
        public void ShouldShowForEntry_ShownOlder_True_NormalUpdate()
        {
            // Основной сценарий фичи: обновился 1.9.0 → 1.10.0, shown ещё
            // "1.9.0" ≠ current → показать (ревью плана не поймало ошибку
            // семантики, поймал прогон; чек R-2026-09-21-02 дополнен).
            Assert.That(
                WhatsNewTracker.ShouldShowForEntry("1.9.0", Current, hasEntry: true),
                Is.True,
                "нормальное обновление — показать");
        }

        [Test]
        public void ShouldShowForEntry_ShownNewer_True_Downgrade()
        {
            Assert.That(
                WhatsNewTracker.ShouldShowForEntry("1.11.0", Current, hasEntry: true),
                Is.True,
                "даунгрейд: показываем, что в этой версии (U7)");
        }

        [Test]
        public void ShouldShowForEntry_GarbageShown_True()
        {
            Assert.That(
                WhatsNewTracker.ShouldShowForEntry("  битый json  ", Current, hasEntry: true),
                Is.True,
                "повреждённый settings.json не глушит фичу (чек R-2026-09-21-02 №9)");
        }

        [Test]
        public void ShouldShowForEntry_NoEntry_False()
        {
            Assert.That(
                WhatsNewTracker.ShouldShowForEntry(null, Current, hasEntry: false),
                Is.False,
                "нет записи — нет диалога");
        }

        [Test]
        public void ShouldShow_BridgesCatalog_NoEntry_False()
        {
            // 999.999.999 гарантированно отсутствует в каталоге.
            Assert.That(
                WhatsNewTracker.ShouldShow(null, new Version(999, 999, 999)),
                Is.False,
                "мост ShouldShow ↔ каталог: без записи показа нет");
        }

        [Test]
        public void Normalize_TrimsRevision()
        {
            Assert.That(WhatsNewTracker.Normalize(new Version(7, 8, 9, 42)), Is.EqualTo("7.8.9"));
        }

        [Test]
        public void MarkShown_WritesNormalizedKey_AndSaves()
        {
            Fixtures.ResetAppSettingsHelper.Reset();
            try
            {
                var settings = AppSettings.Instance;

                WhatsNewTracker.MarkShown(settings, new Version(7, 8, 9, 42));

                Assert.Multiple(() =>
                {
                    Assert.That(settings.WhatsNewShownVersion, Is.EqualTo("7.8.9"));
                    Assert.That(
                        System.IO.File.Exists(Fixtures.ResetAppSettingsHelper.SettingsPath),
                        Is.True,
                        "MarkShown сохраняет настройки");
                });
            }
            finally
            {
                Fixtures.ResetAppSettingsHelper.Reset();
            }
        }
    }
}
