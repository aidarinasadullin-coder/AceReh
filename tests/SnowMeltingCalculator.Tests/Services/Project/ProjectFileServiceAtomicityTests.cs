using System.IO;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public class ProjectFileServiceAtomicityTests
    {
        private ProjectFileService _service = null!;
        private string _testDir = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new ProjectFileService();
            _testDir = Path.Combine(Path.GetTempPath(), $"smc-atomic-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, recursive: true);
                }
            }
            catch
            {
                // Игнорируем ошибки очистки тестовой директории
            }
        }

        [Test]
        public async Task SaveProjectAsync_IsAtomic_OriginalIntactOnWriteFailure()
        {
            // Arrange
            var originalPath = Path.Combine(_testDir, "original.smc");
            const string originalContent = "ORIG";
            await File.WriteAllTextAsync(originalPath, originalContent);

            // Act
            var result = await _service.SaveProjectAsync("Z:/nonexistent_dir/file.smc", new ProjectData());

            // Assert
            Assert.That(result, Is.False, "Метод должен вернуть false при недоступном пути");
            Assert.That(File.ReadAllText(originalPath), Is.EqualTo(originalContent), "Оригинальный файл не должен быть повреждён");
        }

        [Test]
        public async Task SaveProjectAsync_CreatesBackup_BakExistsAfterSave()
        {
            // Arrange
            var filePath = Path.Combine(_testDir, "project.smc");
            var firstData = new ProjectData
            {
                ProjectNumber = "FIRST-001",
                ProjectObject = "First version"
            };
            var secondData = new ProjectData
            {
                ProjectNumber = "SECOND-002",
                ProjectObject = "Second version"
            };

            // Act
            var firstSave = await _service.SaveProjectAsync(filePath, firstData);
            Assert.That(firstSave, Is.True, "Первое сохранение должно быть успешным");

            var firstVersionContent = await File.ReadAllTextAsync(filePath);

            var secondSave = await _service.SaveProjectAsync(filePath, secondData);
            Assert.That(secondSave, Is.True, "Второе сохранение должно быть успешным");

            // Assert
            var bakPath = filePath + ".bak";
            Assert.That(File.Exists(bakPath), Is.True, "Файл .bak должен существовать после второго сохранения");
            Assert.That(File.ReadAllText(bakPath), Is.EqualTo(firstVersionContent), ".bak должен содержать предыдущую версию файла");
        }

        [Test]
        public async Task SaveProjectAsync_TempFileCleanedUpOnFailure()
        {
            // Arrange
            var badPath = "Z:/nonexistent_dir/file.smc";
            var expectedTempPath = Path.ChangeExtension(badPath, ".tmp");

            // Act
            var result = await _service.SaveProjectAsync(badPath, new ProjectData());

            // Assert
            Assert.That(result, Is.False, "Метод должен вернуть false при недоступном пути");
            Assert.That(File.Exists(expectedTempPath), Is.False, "Временный .tmp файл не должен оставаться после ошибки");
        }
    }
}
