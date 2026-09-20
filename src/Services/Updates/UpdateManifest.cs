// ================================================================================
// REHAU Снеготаяние - Манифест канала обновлений (план 1.3 роадмапа post-1.8)
// ================================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

using SnowMeltingCalculator.Services.Logging;

namespace SnowMeltingCalculator.Services.Updates
{
    /// <summary>
    /// Манифест <c>latest.json</c> из папки выдачи Drive: версия, дата,
    /// строки «Что нового», ссылка на папку выдачи (формат — §1 плана
    /// 2026-09-21-updates-whatsnew-plan.md).
    /// </summary>
    public sealed record UpdateManifest(
        string Version,
        string PublishedAt,
        IReadOnlyList<string> WhatsNew,
        string? FolderUrl)
    {
        /// <summary>
        /// Разобрать JSON манифеста. Гварды: version обязана парситься как
        /// <see cref="Version"/>; отсутствующий whatsNew = пустой список;
        /// битый JSON — <c>false</c> с AppLog (гейт SilentCatchScanTests:
        /// без тихих catch — находка ревью №9 чека R-2026-09-21-02).
        /// </summary>
        public static bool TryParse(string json, out UpdateManifest? manifest)
        {
            manifest = null;
            // Гвард: генератор манифеста может дописать BOM — JsonDocument
            // считает \uFEFF невалидным началом значения.
            json = json.TrimStart('\uFEFF');
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                var version = root.TryGetProperty("version", out var v)
                    && v.ValueKind == JsonValueKind.String
                    ? v.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(version) || !System.Version.TryParse(version, out _))
                {
                    return false;
                }

                var publishedAt = root.TryGetProperty("publishedAt", out var p)
                    && p.ValueKind == JsonValueKind.String
                    ? p.GetString() ?? string.Empty
                    : string.Empty;

                var whatsNew = new List<string>();
                if (root.TryGetProperty("whatsNew", out var w) && w.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in w.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String
                            && item.GetString() is { Length: > 0 } line)
                        {
                            whatsNew.Add(line);
                        }
                    }
                }

                var folderUrl = root.TryGetProperty("folderUrl", out var f)
                    && f.ValueKind == JsonValueKind.String
                    ? f.GetString()
                    : null;

                manifest = new UpdateManifest(
                    version,
                    publishedAt,
                    whatsNew,
                    string.IsNullOrWhiteSpace(folderUrl) ? null : folderUrl);
                return true;
            }
            catch (JsonException ex)
            {
                AppLog.Warn(ex, "UpdateManifest.TryParse");
                return false;
            }
        }
    }
}
