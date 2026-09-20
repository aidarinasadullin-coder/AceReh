// ================================================================================
// REHAU Снеготаяние - Проверка обновлений (план 1.3 роадмапа post-1.8)
// ================================================================================

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Services.Updates
{
    /// <summary>Итог проверки: доступно обновление / последняя / канал недоступен.</summary>
    public enum UpdateCheckKind
    {
        UpdateAvailable,
        UpToDate,
        Unavailable
    }

    /// <summary>Результат <see cref="IUpdateCheckService.CheckAsync"/>.</summary>
    public sealed record UpdateCheckOutcome(UpdateCheckKind Kind, UpdateManifest? Manifest, string? Reason);

    public interface IUpdateCheckService
    {
        Task<UpdateCheckOutcome> CheckAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Сверка манифеста канала с версией сборки. Даунгрейд/равная версия =
    /// «последняя» (U5: строковое сравнение сломалось бы на 1.10.0 vs
    /// 1.9.0). Загрузку приложение не выполняет — только уведомление.
    /// </summary>
    public sealed class UpdateCheckService : IUpdateCheckService
    {
        private readonly IUpdateChannel _channel;

        public UpdateCheckService(IUpdateChannel channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        public async Task<UpdateCheckOutcome> CheckAsync(CancellationToken cancellationToken = default)
        {
            var result = await _channel.GetLatestAsync(cancellationToken).ConfigureAwait(false);
            if (!result.Success || result.Manifest is null)
            {
                return new UpdateCheckOutcome(UpdateCheckKind.Unavailable, null, result.Error ?? "Канал недоступен");
            }

            var manifest = result.Manifest;
            if (!Version.TryParse(manifest.Version, out var latest))
            {
                return new UpdateCheckOutcome(UpdateCheckKind.Unavailable, null, "В манифесте некорректная версия");
            }

            var current = WhatsNewTracker.CurrentAssemblyVersion();
            if (current is null)
            {
                return new UpdateCheckOutcome(UpdateCheckKind.Unavailable, null, "Не удалось определить версию приложения");
            }

            return latest > current
                ? new UpdateCheckOutcome(UpdateCheckKind.UpdateAvailable, manifest, null)
                : new UpdateCheckOutcome(UpdateCheckKind.UpToDate, manifest, null);
        }
    }
}
