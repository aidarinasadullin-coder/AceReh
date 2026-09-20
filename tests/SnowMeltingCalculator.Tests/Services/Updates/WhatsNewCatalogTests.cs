// ================================================================================
// Тесты каталога «Что нового» и гварда ссылки папки (план 1.3)
// ================================================================================

using System;

using NUnit.Framework;

using SnowMeltingCalculator.Services.Updates;

namespace SnowMeltingCalculator.Tests.Services.Updates
{
    [TestFixture]
    public class WhatsNewCatalogTests
    {
        [Test]
        public void Entries_KeysParseAsVersions()
        {
            // Каталог пуст между релизами — тест защищает структуру при
            // пополнении (опечатки в ключах/пустые строки).
            foreach (var key in WhatsNewCatalog.Entries.Keys)
            {
                Assert.That(
                    Version.TryParse(key, out _),
                    Is.True,
                    $"ключ каталога «Что нового» должен быть X.Y.Z: «{key}»");
            }
        }

        [Test]
        public void Entries_ValuesAreNonEmptyLines()
        {
            foreach (var entry in WhatsNewCatalog.Entries.Values)
            {
                Assert.That(entry, Is.Not.Empty);
                foreach (var line in entry)
                {
                    Assert.That(line, Is.Not.Empty.And.Not.Null);
                }
            }
        }

        [Test]
        public void IsSafeFolderUrl_HttpsOnly()
        {
            Assert.Multiple(() =>
            {
                Assert.That(UpdateChannelOptions.IsSafeFolderUrl("https://drive.google.com/drive/folders/abc"), Is.True);
                Assert.That(UpdateChannelOptions.IsSafeFolderUrl("http://drive.google.com/folders/abc"), Is.False);
                Assert.That(UpdateChannelOptions.IsSafeFolderUrl("ftp://example.com"), Is.False);
                Assert.That(UpdateChannelOptions.IsSafeFolderUrl("javascript:alert(1)"), Is.False);
                Assert.That(UpdateChannelOptions.IsSafeFolderUrl(null), Is.False);
                Assert.That(UpdateChannelOptions.IsSafeFolderUrl("   "), Is.False);
            });
        }
    }
}
