namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Файловая политика автосохранения (план 3.1 роадмапа post-1.8):
    /// путь служебного снапшота, наличие/гашение/чистка. Без WPF, без
    /// знания о каноне и VM — состояние сессии оценивает вызывающий
    /// (<c>ResultsViewModel.SaveAutosnapshotAsync</c> через предикат
    /// <see cref="ProjectAutosaveService.ShouldSnapshot"/>); таймер — в shell.
    /// </summary>
    public interface IProjectAutosaveService
    {
        /// <summary>Путь служебного снапшота (<c>autosave.smc</c> рядом с настройками).</summary>
        string SnapshotPath { get; }

        /// <summary>Снапшот существует (нормальная ситуация при отсутствии — false, не ошибка).</summary>
        bool HasSnapshot();

        /// <summary>Метка времени снапшота (mtime файла); снапшота нет — null.</summary>
        DateTime? SnapshotTimestamp();

        /// <summary>Гасить снапшот: удалить <c>.smc</c> и <c>.bak</c>; отсутствие файлов — норма.</summary>
        void DeleteSnapshot();

        /// <summary>Чистка остатков <c>.tmp</c> (крах между записью и move); вызвать при старте.</summary>
        void CleanupStale();
    }
}
