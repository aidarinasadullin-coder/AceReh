// ================================================================================
// REHAU Снеготаяние - Параметры канала обновлений (план 1.3 роадмапа post-1.8)
// ================================================================================

namespace SnowMeltingCalculator.Services.Updates
{
    public static class UpdateChannelOptions
    {
        /// <summary>
        /// Прямая ссылка на манифест <c>latest.json</c> в папке выдачи
        /// Drive (формат вида
        /// <c>https://drive.google.com/uc?export=download?id=FILE_ID</c>).
        /// Заполняется владельцем один раз после первого пуша манифеста
        /// (план 1.3, U8: scripts/publish-latest-manifest.ps1 печатает
        /// ссылку); пустая — пункт «Проверить обновления» честно сообщает
        /// «канал не настроен».
        /// </summary>
        public const string ManifestUrl = "";

        /// <summary>
        /// Гвард для <c>folderUrl</c> из манифеста (значение приходит из
        /// сети и уходит в Process.Start): разрешаем только https.
        /// </summary>
        public static bool IsSafeFolderUrl(string? url)
        {
            return !string.IsNullOrWhiteSpace(url)
                && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
