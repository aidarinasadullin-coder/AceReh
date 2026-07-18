using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnowMeltingCalculator.Core.Results;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Реализация сервиса для работы с файлами проектов
    /// </summary>
    public class ProjectFileService : IProjectFileService
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _tempDirectory;

        public ProjectFileService()
        {
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

            // Создаём временную директорию для PDF
            _tempDirectory = Path.Combine(Path.GetTempPath(), "SnowMeltingCalculator");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        /// <inheritdoc />
        [Obsolete("Use SaveProjectResultAsync/LoadProjectResultAsync")]
        public async Task<bool> SaveProjectAsync(string filePath, ProjectData data, CancellationToken cancellationToken = default)
        {
            try
            {
                // Убеждаемся, что расширение правильное
                if (!filePath.EndsWith(".smc", StringComparison.OrdinalIgnoreCase))
                {
                    filePath += ".smc";
                }

                var json = JsonSerializer.Serialize(data, _jsonOptions);

                // Временный файл на том же томе (НЕ Path.GetTempFileName() — может оказаться на другом томе)
                var tempPath = Path.ChangeExtension(filePath, ".tmp");

                // Атомарно-детерминированная запись: temp → move
                await File.WriteAllTextAsync(tempPath, json, cancellationToken);

                // Бэкап существующего файла перед move
                if (File.Exists(filePath))
                {
                    var bakPath = filePath + ".bak";
                    File.Copy(filePath, bakPath, overwrite: true);
                }

                // Atomic на одном томе NTFS
                File.Move(tempPath, filePath, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения проекта: {ex.Message}");

                // temp-файл мог остаться — почистить
                try
                {
                    var tempPath = Path.ChangeExtension(filePath, ".tmp");
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    // Игнорируем ошибки очистки
                }

                return false;
            }
        }

        /// <inheritdoc />
        [Obsolete("Use SaveProjectResultAsync/LoadProjectResultAsync")]
        public async Task<ProjectData?> LoadProjectAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var data = JsonSerializer.Deserialize<ProjectData>(json, _jsonOptions);

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки проекта: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<OperationResult<object?>> SaveProjectResultAsync(string filePath, ProjectData data, CancellationToken cancellationToken = default)
        {
            try
            {
                // Убеждаемся, что расширение правильное
                if (!filePath.EndsWith(".smc", StringComparison.OrdinalIgnoreCase))
                {
                    filePath += ".smc";
                }

                var json = JsonSerializer.Serialize(data, _jsonOptions);

                // Временный файл на том же томе
                var tempPath = Path.ChangeExtension(filePath, ".tmp");

                // Атомарно-детерминированная запись: temp → move
                await File.WriteAllTextAsync(tempPath, json, cancellationToken);

                // Бэкап существующего файла перед move
                if (File.Exists(filePath))
                {
                    var bakPath = filePath + ".bak";
                    File.Copy(filePath, bakPath, overwrite: true);
                }

                // Atomic на одном томе NTFS
                File.Move(tempPath, filePath, overwrite: true);
                return OperationResult<object?>.Success(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения проекта: {ex.Message}");

                // temp-файл мог остаться — почистить
                try
                {
                    var tempPath = Path.ChangeExtension(filePath, ".tmp");
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    // Игнорируем ошибки очистки
                }

                return OperationResult<object?>.Failure(ex.Message, ex);
            }
        }

        /// <inheritdoc />
        public async Task<OperationResult<ProjectData>> LoadProjectResultAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return OperationResult<ProjectData>.Failure($"Файл не найден: {filePath}");
                }

                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var data = JsonSerializer.Deserialize<ProjectData>(json, _jsonOptions);

                if (data == null)
                {
                    return OperationResult<ProjectData>.Failure("Ошибка десериализации: deserialized value is null");
                }

                return OperationResult<ProjectData>.Success(data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки проекта: {ex.Message}");
                return OperationResult<ProjectData>.Failure($"Ошибка десериализации: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public bool IsSmcFile(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) &&
                   filePath.EndsWith(".smc", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public string GetPreviewPdfPath()
        {
            var fileName = $"Preview_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.pdf";
            return Path.Combine(_tempDirectory, fileName);
        }

        /// <inheritdoc />
        public void CleanupTempFiles()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    var files = Directory.GetFiles(_tempDirectory, "Preview_*.pdf");
                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            // Удаляем файлы старше 24 часов
                            if (fileInfo.CreationTime < DateTime.Now.AddHours(-24))
                            {
                                File.Delete(file);
                            }
                        }
                        catch
                        {
                            // Игнорируем ошибки удаления
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки очистки
            }
        }
    }
}
