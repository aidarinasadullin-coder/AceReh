// ================================================================================
// REHAU Снеготаяние - Локальный каталог «Что нового» (план 1.3, U3)
// ================================================================================

using System;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Services.Updates
{
    /// <summary>
    /// Локальный (без сети) источник строк диалога «Что нового» при старте:
    /// ключ «X.Y.Z» → выжимка CHANGELOG (2–4 строки). Пополняется в
    /// release-коммите при бампе версии (напоминание — bump-version.ps1 и
    /// warning в release.yml; гейта-теста с записью текущей версии нет —
    /// решение владельца, чек R-2026-09-21-02 находка №4).
    /// </summary>
    public static class WhatsNewCatalog
    {
        public static IReadOnlyDictionary<string, IReadOnlyList<string>> Entries { get; } =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["1.10.0"] = new[]
                {
                    "«Файл → Проверить обновления» — сверка с каналом раздачи, и диалог «Что нового» после установки новой версии",
                    "Автосохранение: после сбоя программа предложит восстановить несохранённый проект"
                }
            };

        public static IReadOnlyList<string>? Find(string normalizedVersion)
        {
            return Entries.TryGetValue(normalizedVersion, out var entry) ? entry : null;
        }
    }
}
