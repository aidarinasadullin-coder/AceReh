using System.IO;

using NUnit.Framework;

using SnowMeltingCalculator.Services.Project;
using SnowMeltingCalculator.Tests.Fixtures;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    /// <summary>
    /// Файловая политика автосохранения (план 3.1 роадмапа post-1.8, чек
    /// R-2026-09-21-04): путь — рядом с settings.json (та же песочница
    /// SNOWCALC_SETTINGS_DIR), гашение снимает .smc и .bak, чистка убирает
    /// только .tmp, предикат тика — матрица dirty/load/calculating.
    /// </summary>
    [TestFixture]
    public class ProjectAutosaveServiceTests
    {
        private ProjectAutosaveService _service = null!;
        private string _settingsDir = null!;

        [SetUp]
        public void SetUp()
        {
            ResetAppSettingsHelper.Reset();
            _service = new ProjectAutosaveService();
            _settingsDir = Path.GetDirectoryName(ResetAppSettingsHelper.SettingsPath)!;
            Directory.CreateDirectory(_settingsDir);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var stale in Directory.GetFiles(_settingsDir, "autosave*"))
            {
                File.Delete(stale);
            }
        }

        [Test]
        public void SnapshotPath_LivesNextToSettings()
        {
            Assert.That(
                Path.GetDirectoryName(_service.SnapshotPath),
                Is.EqualTo(_settingsDir));
            Assert.That(Path.GetFileName(_service.SnapshotPath), Is.EqualTo("autosave.smc"));
        }

        [Test]
        public void HasSnapshot_False_WhenNoFile()
        {
            Assert.That(_service.HasSnapshot(), Is.False);
        }

        [Test]
        public void HasSnapshot_True_AfterSnapshotCreated()
        {
            File.WriteAllText(_service.SnapshotPath, "stub");

            Assert.That(_service.HasSnapshot(), Is.True);
        }

        [Test]
        public void SnapshotTimestamp_ReturnsWriteTime_WhenFileExists()
        {
            File.WriteAllText(_service.SnapshotPath, "stub");
            File.SetLastWriteTime(_service.SnapshotPath, new DateTime(2026, 9, 21, 12, 0, 0));

            var timestamp = _service.SnapshotTimestamp();

            Assert.That(timestamp, Is.Not.Null);
            Assert.That(timestamp!.Value, Is.EqualTo(new DateTime(2026, 9, 21, 12, 0, 0)).Within(TimeSpan.FromSeconds(2)));
        }

        [Test]
        public void SnapshotTimestamp_Null_WhenNoFile()
        {
            Assert.That(_service.SnapshotTimestamp(), Is.Null);
        }

        [Test]
        public void DeleteSnapshot_RemovesSmcAndBak()
        {
            File.WriteAllText(_service.SnapshotPath, "stub");
            File.WriteAllText(_service.SnapshotPath + ".bak", "stub");

            _service.DeleteSnapshot();

            Assert.That(File.Exists(_service.SnapshotPath), Is.False);
            Assert.That(File.Exists(_service.SnapshotPath + ".bak"), Is.False);
        }

        [Test]
        public void DeleteSnapshot_WhenNothingExists_DoesNotThrow()
        {
            Assert.DoesNotThrow(_service.DeleteSnapshot);
        }

        [Test]
        public void DeleteSnapshot_KeepsUnrelatedFiles()
        {
            var neighbor = Path.Combine(_settingsDir, "project.smc");
            File.WriteAllText(neighbor, "user document");

            _service.DeleteSnapshot();

            Assert.That(File.Exists(neighbor), Is.True);
        }

        [Test]
        public void CleanupStale_RemovesTmp_KeepsSmc()
        {
            var tmpPath = Path.ChangeExtension(_service.SnapshotPath, ".tmp");
            File.WriteAllText(tmpPath, "interrupted write");
            File.WriteAllText(_service.SnapshotPath, "good snapshot");

            _service.CleanupStale();

            Assert.That(File.Exists(tmpPath), Is.False);
            Assert.That(_service.HasSnapshot(), Is.True);
        }

        [Test]
        public void ShouldSnapshot_DirtyOnly()
        {
            Assert.Multiple(() =>
            {
                Assert.That(ProjectAutosaveService.ShouldSnapshot(isDirty: true, isLoadInProgress: false, isCalculating: false), Is.True);
                Assert.That(ProjectAutosaveService.ShouldSnapshot(isDirty: false, isLoadInProgress: false, isCalculating: false), Is.False);
                Assert.That(ProjectAutosaveService.ShouldSnapshot(isDirty: true, isLoadInProgress: true, isCalculating: false), Is.False);
                Assert.That(ProjectAutosaveService.ShouldSnapshot(isDirty: true, isLoadInProgress: false, isCalculating: true), Is.False);
            });
        }
    }
}
