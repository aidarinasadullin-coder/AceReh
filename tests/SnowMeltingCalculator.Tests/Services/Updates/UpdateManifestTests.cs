// ================================================================================
// Тесты парсинга манифеста обновлений (план 1.3 роадмапа post-1.8)
// ================================================================================

using NUnit.Framework;

using SnowMeltingCalculator.Services.Updates;

namespace SnowMeltingCalculator.Tests.Services.Updates
{
    [TestFixture]
    public class UpdateManifestTests
    {
        [Test]
        public void TryParse_ValidManifest_AllFields()
        {
            var json = """
                {
                  "version": "1.10.0",
                  "publishedAt": "2026-09-21",
                  "whatsNew": ["Недавние проекты", "«Что нового»"],
                  "folderUrl": "https://drive.google.com/drive/folders/abc"
                }
                """;

            var ok = UpdateManifest.TryParse(json, out var manifest);

            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(manifest, Is.Not.Null);
                Assert.That(manifest!.Version, Is.EqualTo("1.10.0"));
                Assert.That(manifest.PublishedAt, Is.EqualTo("2026-09-21"));
                Assert.That(manifest.WhatsNew, Has.Count.EqualTo(2));
                Assert.That(manifest.FolderUrl, Is.EqualTo("https://drive.google.com/drive/folders/abc"));
            });
        }

        [Test]
        public void TryParse_MissingWhatsNewAndFolder_EmptyListAndNullUrl()
        {
            var ok = UpdateManifest.TryParse("""{ "version": "1.10.0" }""", out var manifest);

            Assert.Multiple(() =>
            {
                Assert.That(ok, Is.True);
                Assert.That(manifest!.WhatsNew, Is.Empty);
                Assert.That(manifest.FolderUrl, Is.Null);
            });
        }

        [Test]
        public void TryParse_BrokenJson_False()
        {
            Assert.That(UpdateManifest.TryParse("{ не json", out _), Is.False);
        }

        [Test]
        public void TryParse_BadVersion_False()
        {
            Assert.That(
                UpdateManifest.TryParse("""{ "version": "десять" }""", out _),
                Is.False);
        }
    }
}
