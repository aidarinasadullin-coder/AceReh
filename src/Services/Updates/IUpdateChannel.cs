// ================================================================================
// REHAU Снеготаяние - Канал обновлений (план 1.3 роадмапа post-1.8)
// ================================================================================

using System.Threading;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Services.Updates
{
    /// <summary>Итог чтения канала: манифест либо причина отказа.</summary>
    public sealed record ChannelResult(bool Success, UpdateManifest? Manifest, string? Error);

    /// <summary>
    /// Канал раздачи обновлений (Google Drive). Реализация — тонкая
    /// обёртка HttpClient, тестируется через фейк-канал (сети в тестах нет).
    /// </summary>
    public interface IUpdateChannel
    {
        Task<ChannelResult> GetLatestAsync(CancellationToken cancellationToken = default);
    }
}
