// ================================================================================
// REHAU Снеготаяние - Недавние проекты (MRU, план 1.2 роадмапа post-1.8)
// ================================================================================
//
// Состояние — AppSettings.Instance.RecentProjects (settings.json в
// %APPDATA%), сервис — stateless-фасад над ним: экземпляров может быть
// несколько (ResultsViewModel, code-behind шелла), хранилище одно.
// DI-контейнера в репо нет — сервисы пишут singleton напрямую
// (прецедент MainViewModel.IsSidebarCollapsed). Зависимостей от VM нет
// (R4), ProjectSession не трогается (state ownership без изменений).
//
// ================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SnowMeltingCalculator.Services.Logging;

namespace SnowMeltingCalculator.Services.RecentProjects
{
    public class RecentProjectsService : IRecentProjectsService
    {
        /// <summary>Ёмкость списка (роадмап 1.2: «до ~10 путей»).</summary>
        public const int MaxEntries = 10;

        /// <summary>
        /// Расширение файла проекта — общий фильтр drag-drop и добавления.
        /// </summary>
        public static bool IsProjectFile(string? path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && Path.GetExtension(path).Equals(".smc", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Чтение списка с null-гвардом: повреждённый settings.json
        /// («RecentProjects»: null) не должен давать NRE (план §1).
        /// </summary>
        private static List<string> GetListSafe()
        {
            return AppSettings.Instance.RecentProjects ?? new List<string>();
        }

        /// <summary>
        /// Нормализация пути для дедупа: полный путь, сравнение без учёта
        /// регистра (Windows FS: «D:\X\A.smc» = «d:\x\a.smc»).
        /// </summary>
        private static string? Normalize(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            try
            {
                return Path.GetFullPath(filePath.Trim());
            }
            catch (Exception ex)
            {
                // MRU — необязательная удобная фича: битый путь не роняет
                // открытие проекта.
                AppLog.Warn(ex, "RecentProjectsService.Normalize");
                return null;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetRecent()
        {
            // «Отсутствующие скрываются» (роадмап 1.2, Идеи №6). Вызывается
            // только при открытии подменю.
            return GetListSafe()
                .Where(File.Exists)
                .ToList();
        }

        /// <inheritdoc />
        public void Add(string filePath)
        {
            if (!IsProjectFile(filePath))
            {
                return;
            }

            var normalized = Normalize(filePath);
            if (normalized is null)
            {
                return;
            }

            var list = GetListSafe();
            list.RemoveAll(p =>
                string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase));
            list.Insert(0, normalized);

            if (list.Count > MaxEntries)
            {
                list.RemoveRange(MaxEntries, list.Count - MaxEntries);
            }

            AppSettings.Instance.RecentProjects = list;
            AppSettings.Instance.Save();
        }

        /// <inheritdoc />
        public void Clear()
        {
            AppSettings.Instance.RecentProjects = new List<string>();
            AppSettings.Instance.Save();
        }
    }
}
