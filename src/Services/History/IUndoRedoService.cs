using System;

namespace SnowMeltingCalculator.Services.History
{
    /// <summary>
    /// Канонический раздел, затронутый записью дневника отмены (ADR-014).
    /// </summary>
    public enum UndoSliceKind
    {
        Climate,
        Construction,
        Thermal,
        Hydraulics
    }

    /// <summary>
    /// Пара снимков «до»/«после» одного раздела внутри записи дневника.
    /// Снимки — read-only канонические записи; дневник их не создаёт.
    /// </summary>
    public sealed class UndoSlicePair
    {
        public UndoSliceKind Kind { get; }

        /// <summary>Снимок состояния раздела до действия (тип — канонический снимок раздела).</summary>
        public object Before { get; }

        /// <summary>Снимок состояния раздела после действия (тип — канонический снимок раздела).</summary>
        public object After { get; }

        public UndoSlicePair(UndoSliceKind kind, object before, object after)
        {
            Kind = kind;
            Before = before ?? throw new ArgumentNullException(nameof(before));
            After = after ?? throw new ArgumentNullException(nameof(after));
        }
    }

    /// <summary>
    /// Запись дневника: одно действие пользователя — имя и per-slice пары
    /// (Before, After) всех затронутых разделов.
    /// </summary>
    public sealed class UndoHistoryEntry
    {
        private readonly Dictionary<UndoSliceKind, UndoSlicePair> _slices = new();

        public string Name { get; }

        public IReadOnlyDictionary<UndoSliceKind, UndoSlicePair> Slices => _slices;

        public UndoHistoryEntry(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Дополнить запись парой раздела: Before группы фиксируется первым
        /// событием, After — последним.
        /// </summary>
        internal void Merge(UndoSliceKind kind, object before, object after)
        {
            if (_slices.TryGetValue(kind, out var existing))
            {
                _slices[kind] = new UndoSlicePair(kind, existing.Before, after);
                return;
            }

            _slices[kind] = new UndoSlicePair(kind, before, after);
        }
    }

    /// <summary>
    /// Событийный memento-дневник «Отменить / Вернуть» (ADR-014): слушает
    /// <c>Changed</c> четырёх срезов <see cref="SnowMeltingCalculator.Services.Project.IProjectSession"/>,
    /// группирует мутации в записи действий и выполняет откат/возврат
    /// каноническими методами с origins <c>Undo</c>/<c>Redo</c>.
    /// Дневник — память процесса, в <c>.smc</c> не входит.
    /// </summary>
    public interface IUndoRedoService
    {
        /// <summary>Стек отмены не пуст И не идёт расчёт (гейт ADR-014 п.7).</summary>
        bool CanUndo { get; }

        /// <summary>Стек возврата не пуст И не идёт расчёт (гейт ADR-014 п.7).</summary>
        bool CanRedo { get; }

        /// <summary>Имя действия, которое отменит <see cref="Undo"/>; null — отменять нечего.</summary>
        string? UndoDescription { get; }

        /// <summary>Имя действия, которое вернёт <see cref="Redo"/>; null — возвращать нечего.</summary>
        string? RedoDescription { get; }

        /// <summary>Уведомление кнопок/тултипов об изменении истории.</summary>
        event EventHandler? HistoryChanged;

        /// <summary>Отменить последнее действие: применить Before затронутых разделов (origin Undo).</summary>
        void Undo();

        /// <summary>Вернуть последнее отменённое действие: применить After (origin Redo).</summary>
        void Redo();

        /// <summary>Зафиксировать «точку чистоты» (позиция дневника) — из сохранения проекта.</summary>
        void SetCleanPoint();

        /// <summary>Стереть дневник (открытие проекта, «Новый расчёт», старт с файлом).</summary>
        void Clear();
    }
}
