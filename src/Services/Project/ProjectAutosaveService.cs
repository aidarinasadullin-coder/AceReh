using System.IO;

using SnowMeltingCalculator.Services.Logging;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Реализация файловой политики автосохранения: снапшот живёт рядом с
    /// settings.json (путь — <see cref="AppSettings.AutosaveFilePath"/>, та
    /// же инфраструктура <c>SNOWCALC_SETTINGS_DIR</c>). Отсутствие файлов —
    /// норма (не лог); ошибки файловых операций гасятся в журнал — сбой
    /// политики не должен мешать работе приложения.
    /// </summary>
    public sealed class ProjectAutosaveService : IProjectAutosaveService
    {
        /// <summary>Расширение временного файла атомарной записи ProjectFileService.</summary>
        private const string TempExtension = ".tmp";

        /// <summary>Расширение бэкапа предыдущей записи ProjectFileService.</summary>
        private const string BackupSuffix = ".bak";

        /// <inheritdoc />
        public string SnapshotPath => AppSettings.AutosaveFilePath;

        /// <inheritdoc />
        public bool HasSnapshot() => File.Exists(SnapshotPath);

        /// <inheritdoc />
        public DateTime? SnapshotTimestamp()
        {
            try
            {
                // GetLastWriteTime для отсутствующего файла возвращает
                // 1601-й год, а не бросает — отсутствия проверяем явно.
                return File.Exists(SnapshotPath)
                    ? File.GetLastWriteTime(SnapshotPath)
                    : null;
            }
            catch (Exception ex)
            {
                AppLog.Warn(ex, "ProjectAutosaveService.SnapshotTimestamp");
                return null;
            }
        }

        /// <inheritdoc />
        public void DeleteSnapshot()
        {
            DeleteIfExists(SnapshotPath);
            DeleteIfExists(SnapshotPath + BackupSuffix);
        }

        /// <inheritdoc />
        public void CleanupStale()
        {
            DeleteIfExists(Path.ChangeExtension(SnapshotPath, TempExtension));
        }

        /// <summary>
        /// Чистый предикат тика: писать снапшот только для dirty-проекта,
        /// когда не идёт загрузка и не крутится пересчёт (промежуточный
        /// снимок посреди мутаций не снимаем — тик догонит следующий).
        /// </summary>
        public static bool ShouldSnapshot(bool isDirty, bool isLoadInProgress, bool isCalculating) =>
            isDirty && !isLoadInProgress && !isCalculating;

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                AppLog.Warn(ex, "ProjectAutosaveService.DeleteIfExists");
            }
        }
    }
}
