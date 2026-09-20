// ================================================================================
// REHAU Снеготаяние - Недавние проекты (MRU, план 1.2 роадмапа post-1.8)
// ================================================================================

using System.Collections.Generic;

namespace SnowMeltingCalculator.Services.RecentProjects
{
    /// <summary>
    /// Список недавних проектов «Файл → Недавние проекты». Состояние —
    /// <see cref="AppSettings.RecentProjects"/> (settings.json), не канон
    /// ProjectSession: R1–R6 не задеты (state ownership без изменений).
    /// </summary>
    public interface IRecentProjectsService
    {
        /// <summary>
        /// Текущий список существующих на диске путей (отсутствующие
        /// скрываются), самый свежий первым.
        /// </summary>
        IReadOnlyList<string> GetRecent();

        /// <summary>
        /// Запомнить путь: дедуп без учёта регистра, вставка наверх,
        /// ёмкость 10, сохранение настроек.
        /// </summary>
        void Add(string filePath);

        /// <summary>
        /// Очистить список («Очистить список» подменю).
        /// </summary>
        void Clear();
    }
}
