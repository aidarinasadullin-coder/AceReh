using System.IO;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public class ProjectFileServiceResultTests
    {
        private ProjectFileService _service = null!;
        private string _testDir = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new ProjectFileService();
            _testDir = Path.Combine(Path.GetTempPath(), $"smc-result-{Guid.NewGuid()}");
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
        public async Task SaveProjectResultAsync_OnIoFailure_ReturnsFailureWithMessage()
        {
            // Arrange
            var badPath = "Z:/nonexistent_dir/file.smc";

            // Act
            var result = await _service.SaveProjectResultAsync(badPath, new ProjectData());

            // Assert
            Assert.That(result.IsSuccess, Is.False, "Метод должен вернуть ошибку при недоступном пути");
            Assert.That(result.Error, Is.Not.Null, "Ошибка должна содержать сообщение");
            Assert.That(result.Exception, Is.Not.Null, "Ошибка должна содержать исключение");
        }

        [Test]
        public async Task LoadProjectResultAsync_OnMissingFile_ReturnsFailureWithFileNotFound()
        {
            // Arrange
            var missingPath = Path.Combine(_testDir, "missing.smc");

            // Act
            var result = await _service.LoadProjectResultAsync(missingPath);

            // Assert
            Assert.That(result.IsSuccess, Is.False, "Метод должен вернуть ошибку для отсутствующего файла");
            Assert.That(result.Error, Is.Not.Null, "Ошибка должна содержать сообщение");
            Assert.That(result.Error, Does.Contain("не найден").Or.Contains("missing"), "Ошибка должна указывать на отсутствие файла");
        }

        [Test]
        public async Task LoadProjectResultAsync_OnCorruptJson_ReturnsFailureWithDeserializationError()
        {
            // Arrange
            var corruptPath = Path.Combine(_testDir, "corrupt.smc");
            await File.WriteAllTextAsync(corruptPath, "{ invalid json");

            // Act
            var result = await _service.LoadProjectResultAsync(corruptPath);

            // Assert
            Assert.That(result.IsSuccess, Is.False, "Метод должен вернуть ошибку для повреждённого JSON");
            Assert.That(result.Error, Is.Not.Null, "Ошибка должна содержать сообщение");
            Assert.That(result.Error, Does.Contain("Ошибка десериализации"), "Ошибка должна указывать на десериализацию");
        }

        [Test]
        public async Task SaveProjectResultAsync_OnSuccess_ReturnsSuccessWithNullValue()
        {
            // Arrange
            var filePath = Path.Combine(_testDir, "project.smc");
            var data = new ProjectData
            {
                ProjectNumber = "PRJ-001",
                ProjectObject = "Test object"
            };

            // Act
            var result = await _service.SaveProjectResultAsync(filePath, data);

            // Assert
            Assert.That(result.IsSuccess, Is.True, "Сохранение должно быть успешным");
            Assert.That(result.Value, Is.Null, "Успешное сохранение должно возвращать null");
            Assert.That(File.Exists(filePath), Is.True, "Файл проекта должен существовать");
        }
    }
}
