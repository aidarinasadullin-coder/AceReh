// ================================================================================
// Тесты недавних проектов (MRU, план 1.2 роадмапа post-1.8)
// ================================================================================
//
// Изоляция — Fixtures.ResetAppSettingsHelper (паттерн AppSettingsTests):
// settings.json резолвится в %TEMP%-песочницу (SNOWCALC_SETTINGS_DIR из
// GlobalTestSetup), реальный файл пользователя не трогается.
// GetRecent фильтрует по File.Exists — тесты порядка/дедупа/ёмкости
// работают с реально созданными temp-файлами (TearDown подчищает).
//
// ================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using SnowMeltingCalculator.Services;
using SnowMeltingCalculator.Services.RecentProjects;

namespace SnowMeltingCalculator.Tests.Services.RecentProjects
{
    [TestFixture]
    public class RecentProjectsServiceTests
    {
        private RecentProjectsService _service = null!;
        private readonly List<string> _tempDirs = new();

        [SetUp]
        public void SetUp()
        {
            Fixtures.ResetAppSettingsHelper.Reset();
            _service = new RecentProjectsService();
        }

        [TearDown]
        public void TearDown()
        {
            Fixtures.ResetAppSettingsHelper.Reset();

            foreach (var dir in _tempDirs)
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (DirectoryNotFoundException)
                {
                    // уже удалён тестом
                }
            }

            _tempDirs.Clear();
        }

        /// <summary>Реально существующий .smc в своём temp-каталоге.</summary>
        private string CreateTempProject(string fileName)
        {
            var dir = Path.Combine(Path.GetTempPath(), $"ace-mru-{Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);
            _tempDirs.Add(dir);

            var path = Path.Combine(dir, fileName);
            File.WriteAllText(path, "stub");
            return path;
        }

        [Test]
        public void Add_InsertsFirst_AndSavesSettings()
        {
            var a = CreateTempProject("A.smc");
            var b = CreateTempProject("B.smc");

            _service.Add(a);
            _service.Add(b);

            var recent = _service.GetRecent();

            Assert.Multiple(() =>
            {
                Assert.That(recent, Has.Count.EqualTo(2));
                Assert.That(recent[0], Is.EqualTo(Path.GetFullPath(b)), "свежий путь — первым");
                Assert.That(recent[1], Is.EqualTo(Path.GetFullPath(a)));
                Assert.That(File.Exists(Fixtures.ResetAppSettingsHelper.SettingsPath), Is.True,
                    "Add сохраняет settings.json");
            });
        }

        [Test]
        public void Add_SamePathTwice_NoDuplicate_MovesToTop()
        {
            var a = CreateTempProject("A.smc");
            var b = CreateTempProject("B.smc");

            _service.Add(a);
            _service.Add(b);
            _service.Add(a);

            var recent = _service.GetRecent().ToList();

            Assert.Multiple(() =>
            {
                Assert.That(recent, Has.Count.EqualTo(2), "дедуп: дубля нет");
                Assert.That(recent[0], Is.EqualTo(Path.GetFullPath(a)), "повторное открытие — наверх");
            });
        }

        [Test]
        public void Add_DedupIsCaseInsensitive()
        {
            var path = CreateTempProject("case.smc");

            _service.Add(path);
            _service.Add(path.ToUpperInvariant());

            Assert.That(_service.GetRecent(), Has.Count.EqualTo(1),
                "Windows FS: регистр диска/каталога/имени не различается");
        }

        [Test]
        public void Add_CapTen_EvictsOldest()
        {
            var paths = new List<string>();
            for (var i = 1; i <= RecentProjectsService.MaxEntries + 1; i++)
            {
                var p = CreateTempProject($"P{i:00}.smc");
                paths.Add(p);
                _service.Add(p);
            }

            var recent = _service.GetRecent();

            Assert.Multiple(() =>
            {
                Assert.That(recent, Has.Count.EqualTo(RecentProjectsService.MaxEntries));
                Assert.That(recent, Does.Not.Contain(Path.GetFullPath(paths[0])), "самый старый вытеснен");
                Assert.That(recent[0], Is.EqualTo(Path.GetFullPath(paths[^1])));
            });
        }

        [Test]
        public void GetRecent_HidesMissingFiles()
        {
            var tempFile = CreateTempProject("gone.smc");

            _service.Add(tempFile);
            Assert.That(_service.GetRecent(), Has.Count.EqualTo(1));

            File.Delete(tempFile);
            Directory.Delete(Path.GetDirectoryName(tempFile)!);

            Assert.That(_service.GetRecent(), Is.Empty,
                "отсутствующие на диске скрываются (роадмап 1.2 / Идеи №6)");
        }

        [Test]
        public void Clear_EmptiesList_AndPersists()
        {
            _service.Add(CreateTempProject("A.smc"));

            _service.Clear();

            Assert.Multiple(() =>
            {
                Assert.That(_service.GetRecent(), Is.Empty);
                Assert.That(AppSettings.Instance.RecentProjects, Is.Empty);
            });
        }

        [Test]
        public void IsProjectFile_RecognizesExtension()
        {
            Assert.Multiple(() =>
            {
                Assert.That(RecentProjectsService.IsProjectFile(@"D:\x\a.smc"), Is.True);
                Assert.That(RecentProjectsService.IsProjectFile(@"D:\x\a.SMC"), Is.True);
                Assert.That(RecentProjectsService.IsProjectFile(@"D:\x\a.txt"), Is.False);
                Assert.That(RecentProjectsService.IsProjectFile(null), Is.False);
                Assert.That(RecentProjectsService.IsProjectFile("   "), Is.False);
            });
        }

        [Test]
        public void Add_AfterNullJson_GuardsAgainstNullList()
        {
            // Повреждённый settings.json: RecentProjects = null. SetUp уже
            // сбросил singleton (и удалил файл) — пишем битый файл ПОВЕРХ и
            // читаем: AppSettings.Load занесёт null в свойство, сервис
            // обязан работать без NRE (null-гвард GetListSafe, план §1).
            Directory.CreateDirectory(Path.GetDirectoryName(Fixtures.ResetAppSettingsHelper.SettingsPath)!);
            File.WriteAllText(
                Fixtures.ResetAppSettingsHelper.SettingsPath,
                "{ \"IsSidebarCollapsed\": false, \"RecentProjects\": null }");

            Assert.That(AppSettings.Instance.RecentProjects, Is.Null);

            var service = new RecentProjectsService();
            Assert.DoesNotThrow(() => service.Add(CreateTempProject("A.smc")));
            Assert.That(service.GetRecent(), Has.Count.EqualTo(1));
        }
    }
}
