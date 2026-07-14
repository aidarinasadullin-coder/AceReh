using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;
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
        public async Task<bool> SaveProjectAsync(string filePath, ProjectData data)
        {
            try
            {
                // Обновляем дату изменения
                data.ModifiedDate = DateTime.Now;

                // Если это новый файл, устанавливаем дату создания
                if (data.CreatedDate == default)
                {
                    data.CreatedDate = DateTime.Now;
                }

                // Убеждаемся, что расширение правильное
                if (!filePath.EndsWith(".smc", StringComparison.OrdinalIgnoreCase))
                {
                    filePath += ".smc";
                }

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения проекта: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
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

                // Обновляем дату изменения при загрузке
                if (data != null)
                {
                    data.ModifiedDate = DateTime.Now;
                }

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки проекта: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public Task<string?> GetSaveFilePathAsync(string defaultFileName)
        {
            return Task.Run(() =>
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Проекты SMC (*.smc)|*.smc|Все файлы (*.*)|*.*",
                    DefaultExt = "smc",
                    FileName = defaultFileName,
                    Title = "Сохранить проект"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    return saveFileDialog.FileName;
                }

                return null;
            });
        }

        /// <inheritdoc />
        public Task<string?> GetOpenFilePathAsync()
        {
            return Task.Run(() =>
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Проекты SMC (*.smc)|*.smc|Все файлы (*.*)|*.*",
                    DefaultExt = "smc",
                    Title = "Открыть проект"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    return openFileDialog.FileName;
                }

                return null;
            });
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
