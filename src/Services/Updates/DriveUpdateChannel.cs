// ================================================================================
// REHAU Снеготаяние - Канал обновлений на Google Drive (план 1.3)
// ================================================================================

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using SnowMeltingCalculator.Services.Logging;

namespace SnowMeltingCalculator.Services.Updates
{
    /// <summary>
    /// Чтение <c>latest.json</c> по фиксированной прямой ссылке
    /// (<see cref="UpdateChannelOptions.ManifestUrl"/>). Любой сетевой или
    /// форматный сбой — <c>ChannelResult</c> с причиной + AppLog (гейт
    /// SilentCatchScanTests); приложение не падает — «Проверить обновления»
    /// честно сообщает о недоступности канала.
    /// </summary>
    public sealed class DriveUpdateChannel : IUpdateChannel
    {
        private static readonly HttpClient HttpClient = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SnowMeltingCalculator-UpdateCheck");
            return client;
        }

        public async Task<ChannelResult> GetLatestAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(UpdateChannelOptions.ManifestUrl))
            {
                return new ChannelResult(false, null, "Канал обновлений не настроен");
            }

            try
            {
                using var response = await HttpClient
                    .GetAsync(UpdateChannelOptions.ManifestUrl, cancellationToken)
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new ChannelResult(false, null, $"Канал ответил кодом {(int)response.StatusCode}");
                }

                var json = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (!UpdateManifest.TryParse(json, out var manifest) || manifest is null)
                {
                    return new ChannelResult(false, null, "Манифест не распознан");
                }

                return new ChannelResult(true, manifest, null);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                AppLog.Warn(ex, "DriveUpdateChannel.GetLatestAsync");
                return new ChannelResult(false, null, "Превышено время ожидания канала");
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
            {
                AppLog.Warn(ex, "DriveUpdateChannel.GetLatestAsync");
                return new ChannelResult(false, null, "Сеть недоступна");
            }
        }
    }
}
