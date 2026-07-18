using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Project;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Services.Project
{
    [TestFixture]
    public class ProjectFileServiceMutationTests
    {
        private ProjectFileService _service = null!;
        private string _testDir = null!;
        private JsonSerializerOptions _jsonOptions = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new ProjectFileService();
            _testDir = Path.Combine(Path.GetTempPath(), $"smc-mutation-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);

            // Mirror the serialization options used by ProjectFileService
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
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
        public async Task LoadProjectAsync_DoesNotModify_ModifiedDate()
        {
            // Arrange
            var expectedDate = new DateTime(2020, 1, 1);
            var filePath = Path.Combine(_testDir, "project.smc");
            var originalData = new ProjectData
            {
                ProjectNumber = "PRJ-001",
                ProjectObject = "Test object",
                ModifiedDate = expectedDate
            };
            var json = JsonSerializer.Serialize(originalData, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            // Act
            var result = await _service.LoadProjectAsync(filePath);

            // Assert
            Assert.That(result, Is.Not.Null, "Загруженные данные не должны быть null");
            Assert.That(result!.ModifiedDate, Is.EqualTo(expectedDate), "LoadProjectAsync не должен изменять ModifiedDate");
        }

        [Test]
        public async Task SaveProjectAsync_DoesNotModify_ModifiedDate()
        {
            // Arrange
            var expectedDate = new DateTime(2020, 1, 1);
            var filePath = Path.Combine(_testDir, "project.smc");
            var data = new ProjectData
            {
                ProjectNumber = "PRJ-001",
                ProjectObject = "Test object",
                ModifiedDate = expectedDate
            };

            // Act
            var success = await _service.SaveProjectAsync(filePath, data);

            // Assert
            Assert.That(success, Is.True, "Сохранение должно быть успешным");
            Assert.That(File.Exists(filePath), Is.True, "Файл проекта должен существовать");

            var savedJson = await File.ReadAllTextAsync(filePath);
            var savedData = JsonSerializer.Deserialize<ProjectData>(savedJson, _jsonOptions);

            Assert.That(savedData, Is.Not.Null, "Сохранённые данные должны десериализоваться");
            Assert.That(savedData!.ModifiedDate, Is.EqualTo(expectedDate), "SaveProjectAsync не должен изменять ModifiedDate");
        }
    }
}
