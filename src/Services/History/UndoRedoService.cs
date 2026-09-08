using System;
using System.Collections.Generic;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Services.History
{
    /// <summary>
    /// Событийный memento-дневник «Отменить / Вернуть» (ADR-014, вариант Г).
    /// <para>Запись: слушает <c>Changed</c> четырёх срезов сессии; одна запись
    /// дневника = одно действие пользователя (имя + per-slice пары Before/After
    /// всех затронутых разделов). User-origins открывают/пополняют активную
    /// запись; <c>*Invalidation</c>/<c>Calculation</c> при активной записи
    /// примыкают к ней; <c>Calculation</c> вне активной записи открывает
    /// запись «Расчёт», живущую до первой user-мутации или очистки;
    /// lifecycle-origins не пишутся. Склейка посимвольного ввода — окно
    /// тишины 400 мс (закрывает только user-группы).</para>
    /// <para>Подавление (три линии): (1) события при
    /// <c>IsLoadProjectInProgress</c> игнорируются полностью; (2) origins
    /// <c>Undo</c>/<c>Redo</c> игнорируются (эхо); (3) флаг
    /// <c>_isApplying</c> вокруг всего отката.</para>
    /// <para>Откат: Before каждого затронутого раздела применяется
    /// каноническим методом с origin <c>Undo</c> под
    /// <see cref="IProjectSession.BeginProjectRestore"/>; «Вернуть» —
    /// симметрично After с <c>Redo</c>. Порядок: Climate → Construction →
    /// Thermal (координатор) → Hydraulics → повторная Hydraulics (каскад
    /// <c>ContextChanged → CalculateAll</c> синхронен).</para>
    /// <para>Dirty по точке чистоты: <see cref="SetCleanPoint"/> фиксирует
    /// позицию в дневнике; после каждой записи/отката позиция сравнивается
    /// с точкой (<see cref="IMarkDirtyService"/> — сессия). Лимит истории —
    /// 10 записей, старейшая вытесняется; вытеснение точки чистоты честно
    /// оставляет проект «изменённым».</para>
    /// </summary>
    public sealed class UndoRedoService : IUndoRedoService, IDisposable
    {
        /// <summary>Глубина истории (решение владельца, план §1.3).</summary>
        public const int MaxEntries = 10;

        /// <summary>Окно тишины склейки посимвольного ввода, мс (план §2).</summary>
        public const int UserGroupSilenceMs = 400;

        private readonly IProjectSession _projectSession;
        private readonly IThermalStateCoordinator _thermalCoordinator;
        private readonly ICalculationStateService _calculationStateService;
        private readonly System.Windows.Threading.Dispatcher? _dispatcher;

        private readonly List<UndoHistoryEntry> _undo = new();
        private readonly List<UndoHistoryEntry> _redo = new();
        private readonly List<(UndoSliceKind Kind, object Before, object After)> _orphanSlices = new();

        private UndoHistoryEntry? _activeUserEntry;
        private UndoHistoryEntry? _calculationEntry;
        private System.Windows.Threading.DispatcherTimer? _quietTimer;
        private int _cleanPosition;
        private long _lastUserMutationTicks;
        private bool _isApplying;
        private bool _disposed;

        public UndoRedoService(
            IProjectSession projectSession,
            IThermalStateCoordinator thermalCoordinator,
            ICalculationStateService calculationStateService,
            System.Windows.Threading.Dispatcher? uiDispatcher = null)
        {
            _projectSession = projectSession ?? throw new ArgumentNullException(nameof(projectSession));
            _thermalCoordinator = thermalCoordinator ?? throw new ArgumentNullException(nameof(thermalCoordinator));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _dispatcher = uiDispatcher;

            // Подписки на 4 канонических события — единственный источник записи
            // (INV-016). Синглтон на время жизни приложения — отписки не нужны
            // (house style). Подписки на IProjectSession.PropertyChanged НЕТ
            // (план, ревью P1-1: карточка проекта не в v1).
            _projectSession.ClimateState.Changed += OnClimateChanged;
            _projectSession.ConstructionState.Changed += OnConstructionChanged;
            _projectSession.ThermalState.Changed += OnThermalChanged;
            _projectSession.HydraulicsState.Changed += OnHydraulicsChanged;
        }

        /// <inheritdoc />
        public event EventHandler? HistoryChanged;

        public bool CanUndo =>
            _undo.Count > 0 && !_isApplying && !IsCalculationRunning;

        public bool CanRedo =>
            _redo.Count > 0 && !_isApplying && !IsCalculationRunning;

        public string? UndoDescription => CanUndo ? _undo[^1].Name : null;

        public string? RedoDescription => CanRedo ? _redo[^1].Name : null;

        private bool IsCalculationRunning =>
            _calculationStateService.ThermalIsCalculating
            || _calculationStateService.HydraulicsIsCalculating;

        /// <summary>Внутри ли окна тишины склейки от последней user-мутации.</summary>
        private bool IsWithinSilenceWindow() =>
            Environment.TickCount64 - _lastUserMutationTicks < UserGroupSilenceMs;

        #region Запись дневника

        private void OnClimateChanged(object? sender, ClimateStateChangedEventArgs e) =>
            OnSliceChanged(UndoSliceKind.Climate, e.Origin, e.OldSnapshot, e.NewSnapshot);

        private void OnConstructionChanged(object? sender, ConstructionStateChangedEventArgs e) =>
            OnSliceChanged(UndoSliceKind.Construction, e.Origin, e.Before, e.After);

        private void OnThermalChanged(object? sender, ThermalStateChangedEventArgs e) =>
            OnSliceChanged(UndoSliceKind.Thermal, e.Mutation.Origin, e.Mutation.Before, e.Mutation.After);

        private void OnHydraulicsChanged(object? sender, HydraulicsStateChangedEventArgs e) =>
            OnSliceChanged(UndoSliceKind.Hydraulics, e.Origin, e.OldSnapshot, e.NewSnapshot);

        private void OnSliceChanged(UndoSliceKind kind, Enum origin, object before, object after)
        {
            // Линия 1 (P0-1): тотальное подавление при загрузке/восстановлении —
            // независимо от origin (включая фантомный fallback-расчёт загрузки).
            if (_projectSession.IsLoadProjectInProgress)
            {
                return;
            }

            // Линия 3: внутри отката дневник глух.
            if (_isApplying)
            {
                return;
            }

            // Линия 2: эхо собственных Undo/Redo-применений.
            if (IsUndoRedoOrigin(origin))
            {
                return;
            }

            if (IsUserOrigin(kind, origin))
            {
                // Склейка (план §2): мутации внутри окна тишины продолжают
                // активную user-группу; по истечении окна предыдущая группа
                // форс-закрывается и открывается новая. Любая user-мутация
                // гасит ветку Redo (Word-стиль).
                var continuesGroup = _activeUserEntry is not null && IsWithinSilenceWindow();
                _lastUserMutationTicks = Environment.TickCount64;
                _redo.Clear();

                if (!continuesGroup)
                {
                    CloseActiveGroups();
                    _activeUserEntry = new UndoHistoryEntry(ResolveName(kind, origin, before, after));
                    DrainOrphanSlices(_activeUserEntry);
                }

                _activeUserEntry!.Merge(kind, before, after);
                RestartQuietTimer();
                return;
            }

            if (IsInvalidationOrigin(kind, origin))
            {
                if (_activeUserEntry is { } active)
                {
                    // Инвалидация — часть действия: гашение/возврат результатов.
                    active.Merge(kind, before, after);
                }
                else
                {
                    // Климат/конструкция поднимают DataChanged (инвалидацию тепла)
                    // ДО собственного слайсового события, поэтому активной записи
                    // ещё нет. Пара откладывается и вливается в запись, которую
                    // та же user-мутация откроет следом (синхронный стек вызовов).
                    _orphanSlices.Add((kind, before, after));
                }

                return;
            }

            if (IsCalculationOrigin(kind, origin))
            {
                if (_activeUserEntry is { } active)
                {
                    // Расчётный каскад, примыкающий к правке данных.
                    active.Merge(kind, before, after);
                    RestartQuietTimer();
                    return;
                }

                if (IsThermalCalculationBegin(kind, origin, after) && _calculationEntry is null)
                {
                    // Шапочная «Рассчитать»: каскад всегда начинается с теплового
                    // BeginCalculation — он открывает отдельную запись «Расчёт».
                    _calculationEntry = new UndoHistoryEntry("Расчёт");
                    _calculationEntry.Merge(kind, before, after);
                    RestartQuietTimer();
                    return;
                }

                if (_calculationEntry is { } calculation)
                {
                    calculation.Merge(kind, before, after);
                    RestartQuietTimer();
                    return;
                }

                // Каскад пересчёта гидравлики, запущенный правкой климата:
                // hydraulics Begin/Complete приходят ДО слайсового события
                // климата (внутри того же стека CompleteMutation). Пара
                // откладывается и вливается в городскую/конструкторскую
                // запись, которую та же user-мутация откроет следом
                // (план §1.11: выбор города = 1 запись, 3 слайса).
                _orphanSlices.Add((kind, before, after));
            }

            // Прочие lifecycle-origins (Load, Reset, Initialization, ...) не пишутся.
        }

        /// <summary>
        /// Тепловой BeginCalculation (фаза → Calculating): единственная точка,
        /// где открывается standalone-запись «Расчёт».
        /// </summary>
        private static bool IsThermalCalculationBegin(UndoSliceKind kind, Enum origin, object after) =>
            kind == UndoSliceKind.Thermal
            && origin is ThermalMutationOrigin.Calculation
            && after is ThermalStateSnapshot { Status.Phase: ThermalCalculationPhase.Calculating };

        private static bool IsUndoRedoOrigin(Enum origin) =>
            origin is ClimateMutationOrigin.Undo or ClimateMutationOrigin.Redo
                or ConstructionMutationOrigin.Undo or ConstructionMutationOrigin.Redo
                or ThermalMutationOrigin.Undo or ThermalMutationOrigin.Redo
                or HydraulicsMutationOrigin.Undo or HydraulicsMutationOrigin.Redo;

        private static bool IsUserOrigin(UndoSliceKind kind, Enum origin) => kind switch
        {
            UndoSliceKind.Climate => origin is ClimateMutationOrigin.User or ClimateMutationOrigin.UserReset,
            UndoSliceKind.Construction => origin is ConstructionMutationOrigin.User or ConstructionMutationOrigin.Template,
            UndoSliceKind.Thermal => origin is ThermalMutationOrigin.User or ThermalMutationOrigin.UserReset,
            UndoSliceKind.Hydraulics => origin is HydraulicsMutationOrigin.User or HydraulicsMutationOrigin.UserReset,
            _ => false
        };

        private static bool IsInvalidationOrigin(UndoSliceKind kind, Enum origin) =>
            kind == UndoSliceKind.Thermal
                && origin is ThermalMutationOrigin.ClimateInvalidation or ThermalMutationOrigin.ConstructionInvalidation;

        private static bool IsCalculationOrigin(UndoSliceKind kind, Enum origin) =>
            kind is UndoSliceKind.Thermal or UndoSliceKind.Hydraulics
                && (origin is ThermalMutationOrigin.Calculation or HydraulicsMutationOrigin.Calculation);

        private void DrainOrphanSlices(UndoHistoryEntry entry)
        {
            foreach (var (kind, before, after) in _orphanSlices)
            {
                entry.Merge(kind, before, after);
            }

            _orphanSlices.Clear();
        }

        /// <summary>
        /// Имя действия — реестр diff-правил по изменённым полям снимков (план §5.1).
        /// Сравнение коллекций опирается на ссылочное равенство списков внутри
        /// канонических снимков (слайсы сохраняют ссылки, не клонируют); при
        /// появлении клонирования сравнивать структурно.
        /// </summary>
        private static string ResolveName(UndoSliceKind kind, Enum origin, object before, object after)
        {
            switch (kind)
            {
                case UndoSliceKind.Climate when before is ClimateStateSnapshot climateBefore
                    && after is ClimateStateSnapshot climateAfter:
                    return !string.Equals(climateBefore.SelectedCity, climateAfter.SelectedCity, StringComparison.Ordinal)
                        ? "Выбор города"
                        : "Изменение климатических данных";
                case UndoSliceKind.Construction when before is ConstructionStateSnapshot constructionBefore
                    && after is ConstructionStateSnapshot constructionAfter:
                    if (origin is ConstructionMutationOrigin.Template)
                    {
                        return "Применение шаблона";
                    }

                    return !constructionBefore.LayersAbovePipe.Equals(constructionAfter.LayersAbovePipe)
                        || !constructionBefore.LayersBelowPipe.Equals(constructionAfter.LayersBelowPipe)
                            ? "Изменение слоёв конструкции"
                            : "Смена уровня грунтовых вод";
                case UndoSliceKind.Thermal:
                    return "Изменение тепловых входов";
                case UndoSliceKind.Hydraulics when before is HydraulicsStateSnapshot hydraulicsBefore
                    && after is HydraulicsStateSnapshot hydraulicsAfter:
                    return hydraulicsBefore.Collectors.Equals(hydraulicsAfter.Collectors)
                        ? "Изменение общих входов"
                        : "Изменение коллекторов/контуров";
                default:
                    return $"Изменение данных: {kind}";
            }
        }

        #endregion

        #region Группы и стеки

        private void CloseActiveGroups()
        {
            // ВНИМАНИЕ: не чистит _orphanSlices — вызывается и из user-ветки
            // ПЕРЕД открытием новой записи того же синхронного стека, чьи
            // orphan-пары (DataChanged стреляет раньше слайсового события)
            // сейчас будут дренированы. Терминальные точки (Undo/Redo/тихий
            // тик/SetCleanPoint/Clear) чистят буфер явно — ревью P2-3.
            DisposeQuietTimer();

            if (_calculationEntry is { } calculation)
            {
                _calculationEntry = null;
                Push(calculation);
            }

            if (_activeUserEntry is { } active)
            {
                _activeUserEntry = null;
                Push(active);
            }
        }

        private void Push(UndoHistoryEntry entry)
        {
            _undo.Add(entry);
            _redo.Clear();

            while (_undo.Count > MaxEntries)
            {
                _undo.RemoveAt(0);
                if (_cleanPosition > 0)
                {
                    _cleanPosition--;
                }
                else if (_cleanPosition == 0)
                {
                    // Точка чистоты вытеснена за предел истории: честно
                    // «навсегда изменён» до следующего сохранения (план §2).
                    _cleanPosition = -1;
                }
            }

            SyncDirty();
            RaiseHistoryChanged();
        }

        private void RestartQuietTimer()
        {
            DisposeQuietTimer();

            // DispatcherTimer тикает в UI-потоке — состояние дневника и
            // HistoryChanged остаются консистентными с мутациями. В тестовых
            // композициях диспетчер не передаётся; группы закрываются лениво
            // (Undo/новая мутация/FlushPendingForTests).
            if (_dispatcher is null)
            {
                return;
            }

            var timer = new System.Windows.Threading.DispatcherTimer(
                TimeSpan.FromMilliseconds(UserGroupSilenceMs),
                System.Windows.Threading.DispatcherPriority.Background,
                (_, _) => OnQuietTimerTick(),
                _dispatcher);
            timer.Start();
            _quietTimer = timer;
        }

        /// <summary>
        /// Тик окна тишины: пока идёт расчёт — ждём дальше (план, ревью P1-5:
        /// медленный расчёт не должен развалиться на две записи); иначе
        /// закрываем открытые группы («Расчёт» и user-группу).
        /// </summary>
        private void OnQuietTimerTick()
        {
            DisposeQuietTimer();

            if (IsCalculationRunning)
            {
                RestartQuietTimer();
                return;
            }

            CloseActiveGroups();
            _orphanSlices.Clear();
        }

        private void DisposeQuietTimer()
        {
            if (_quietTimer is not null)
            {
                _quietTimer.Stop();
                _quietTimer = null;
            }
        }

        #endregion

        #region Undo / Redo

        /// <inheritdoc />
        public void Undo()
        {
            if (!CanUndo)
            {
                return;
            }

            // Открытые группы закрываются до отката: позиция дневника
            // должна быть определена, активная запись — применена.
            CloseActiveGroups();
            _orphanSlices.Clear();

            var entry = _undo[^1];
            _undo.RemoveAt(_undo.Count - 1);
            try
            {
                ApplyEntry(entry, applyAfter: false);
            }
            finally
            {
                // Целостность истории при исключении применения: запись
                // переезжает в стек возврата в любом исходе (ревью P2-2).
                _redo.Add(entry);
                SyncDirty();
                RaiseHistoryChanged();
            }
        }

        /// <inheritdoc />
        public void Redo()
        {
            if (!CanRedo)
            {
                return;
            }

            CloseActiveGroups();
            _orphanSlices.Clear();

            var entry = _redo[^1];
            _redo.RemoveAt(_redo.Count - 1);
            try
            {
                ApplyEntry(entry, applyAfter: true);
            }
            finally
            {
                _undo.Add(entry);
                SyncDirty();
                RaiseHistoryChanged();
            }
        }

        /// <summary>
        /// Применить снимки записи каноническими методами. Порядок: Climate →
        /// Construction → Thermal (координатор) → Hydraulics → повторная
        /// Hydraulics сразу после Thermal — тепловая публикация запускает
        /// синхронный каскад пересчёта гидравлики, который перезаписывает
        /// канон (трюк <c>ProjectLoadOrchestrator</c>); повторное применение
        /// возвращает снимок записи. Весь откат выполняется под
        /// <see cref="IProjectSession.BeginProjectRestore"/>: VM-гварды и
        /// dirty подавлены, дневник — правилом тотального подавления (P0-1).
        /// </summary>
        private void ApplyEntry(UndoHistoryEntry entry, bool applyAfter)
        {
            _isApplying = true;
            try
            {
                using var restoreScope = _projectSession.BeginProjectRestore();

                if (entry.Slices.TryGetValue(UndoSliceKind.Climate, out var climate))
                {
                    _projectSession.ClimateState.ApplySnapshot(
                        (ClimateStateSnapshot)(applyAfter ? climate.After : climate.Before),
                        applyAfter ? ClimateMutationOrigin.Redo : ClimateMutationOrigin.Undo);
                }

                if (entry.Slices.TryGetValue(UndoSliceKind.Construction, out var construction))
                {
                    _projectSession.ConstructionState.ApplySnapshot(
                        (ConstructionStateSnapshot)(applyAfter ? construction.After : construction.Before),
                        applyAfter ? ConstructionMutationOrigin.Redo : ConstructionMutationOrigin.Undo);
                }

                if (entry.Slices.TryGetValue(UndoSliceKind.Thermal, out var thermal))
                {
                    _thermalCoordinator.RestoreState(
                        (ThermalStateSnapshot)(applyAfter ? thermal.After : thermal.Before),
                        applyAfter ? ThermalMutationOrigin.Redo : ThermalMutationOrigin.Undo);
                }

                if (entry.Slices.TryGetValue(UndoSliceKind.Hydraulics, out var hydraulics))
                {
                    var snapshot = (HydraulicsStateSnapshot)(applyAfter ? hydraulics.After : hydraulics.Before);
                    var origin = applyAfter ? HydraulicsMutationOrigin.Redo : HydraulicsMutationOrigin.Undo;
                    _projectSession.HydraulicsState.Restore(snapshot, origin);

                    if (entry.Slices.ContainsKey(UndoSliceKind.Thermal))
                    {
                        // Каскад от публикации тепла синхронен (план, ревью P2-5):
                        // к этому месту он завершён — перезаписываем поверх него.
                        _projectSession.HydraulicsState.Restore(snapshot, origin);
                    }
                }
            }
            finally
            {
                _isApplying = false;
            }
        }

        #endregion

        #region Точка чистоты, очистка

        /// <inheritdoc />
        public void SetCleanPoint()
        {
            // Сохранение коммитит открытое действие (Word-семантика): иначе
            // закрытие группы окном тишины ПОСЛЕ сохранения сдвинуло бы
            // позицию за точку чистоты и вернуло бы ложную «звёздочку».
            CloseActiveGroups();
            _orphanSlices.Clear();
            _cleanPosition = _undo.Count;
            SyncDirty();
        }

        /// <inheritdoc />
        public void Clear()
        {
            DisposeQuietTimer();
            _activeUserEntry = null;
            _calculationEntry = null;
            _orphanSlices.Clear();
            _undo.Clear();
            _redo.Clear();
            _cleanPosition = 0;
            RaiseHistoryChanged();
        }

        /// <summary>
        /// Dirty-коррекция по точке чистоты: <c>позиция == точка → clean</c>,
        /// иначе dirty. Дневник — санкционированный caller WI-5/WI-6 (ADR-014).
        /// </summary>
        private void SyncDirty()
        {
            if (_undo.Count == _cleanPosition)
            {
                _projectSession.MarkClean();
            }
            else
            {
                _projectSession.MarkDirty();
            }
        }

        private void RaiseHistoryChanged() => HistoryChanged?.Invoke(this, EventArgs.Empty);

        #endregion

        /// <summary>
        /// Тестовый шов: содержимое стека отмены (старейшая запись — первая).
        /// </summary>
        internal IReadOnlyList<UndoHistoryEntry> UndoStackForTests => _undo;

        /// <summary>
        /// Тестовый шов: детерминированно закрыть открытые группы без
        /// ожидания реального окна тишины (план §5.1, тестируемое время).
        /// </summary>
        internal void FlushPendingForTests()
        {
            if (!IsCalculationRunning)
            {
                CloseActiveGroups();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            DisposeQuietTimer();
            _projectSession.ClimateState.Changed -= OnClimateChanged;
            _projectSession.ConstructionState.Changed -= OnConstructionChanged;
            _projectSession.ThermalState.Changed -= OnThermalChanged;
            _projectSession.HydraulicsState.Changed -= OnHydraulicsChanged;
        }
    }
}
