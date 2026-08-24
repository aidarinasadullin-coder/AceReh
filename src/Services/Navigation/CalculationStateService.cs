// ================================================================================
// REHAU Снеготаяние - Реализация сервиса состояния расчёта
// ================================================================================
//
// Соответствует: design_guidelines.md
// Реализует: ICalculationStateService
//
// Phase 4 (AMZ-1, DEC-T06/T07): все Thermal backing stores удалены. Геттеры
// читают живой канонический срез IProjectSession.ThermalState; канонические
// завершения транслируются в legacy StateChanged/PipeSpacingChanged ровно один
// раз; SetPipeSpacing остаётся временной поверхностью записи шага укладки
// (guard сохранён) и применяет правку напрямую к каноническому состоянию с
// flow-through статусом и без dirty.
//
// ================================================================================

using System;
using SnowMeltingCalculator.Models.Enums;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Services.Navigation
{
    /// <summary>
    /// Реализация сервиса для управления состоянием расчёта модулей
    /// </summary>
    public class CalculationStateService : ICalculationStateService
    {
        #region Private Fields

        private readonly IProjectSession _projectSession;
        private readonly EventHandler<ThermalStateChangedEventArgs> _thermalChangedHandler;
        private readonly EventHandler<HydraulicsStateChangedEventArgs> _hydraulicsChangedHandler;
        private IDisposable? _restoreLease;

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт сервис состояния расчёта с собственным изолированным экземпляром сессии проекта.
        /// </summary>
        public CalculationStateService()
            : this(new ProjectSession())
        {
        }

        /// <summary>
        /// Создаёт сервис состояния расчёта, делегирующий guard загрузки указанной сессии.
        /// </summary>
        public CalculationStateService(IProjectSession projectSession)
        {
            _projectSession = projectSession ?? throw new ArgumentNullException(nameof(projectSession));
            _thermalChangedHandler = OnThermalStateChanged;
            _projectSession.ThermalState.Changed += _thermalChangedHandler;
            _hydraulicsChangedHandler = OnHydraulicsStateChanged;
            _projectSession.HydraulicsState.Changed += _hydraulicsChangedHandler;
        }

        /// <summary>
        /// Сессия проекта, с которой связан сервис (шов для композиции координатора).
        /// </summary>
        internal IProjectSession Session => _projectSession;

        #endregion

        #region ICalculationStateService Implementation

        #region Тепловой расчёт

        /// <inheritdoc/>
        public bool ThermalNeedsRecalculation =>
            _projectSession.ThermalState.Snapshot.Status.Phase == ThermalCalculationPhase.NeedsRecalculation;

        /// <inheritdoc/>
        public bool ThermalIsCalculating =>
            _projectSession.ThermalState.Snapshot.Status.Phase == ThermalCalculationPhase.Calculating;

        /// <inheritdoc/>
        public string ThermalValidationMessage =>
            _projectSession.ThermalState.Snapshot.Status.RecalculationMessage;

        /// <inheritdoc/>
        public void SetThermalNeedsRecalculation(string message)
        {
            // Мост AMZ-1: переходная каноническая мутация сохраняет входы/результат
            // и воспроизводит legacy-наблюдаемое (ровно один StateChanged).
            _projectSession.ThermalState.ApplyNeedsRecalculation(message, ThermalMutationOrigin.User);
        }

        /// <inheritdoc/>
        public void SetThermalCalculating()
        {
            _projectSession.ThermalState.BeginCalculation();
        }

        /// <inheritdoc/>
        public void ResetThermalState()
        {
            _projectSession.ThermalState.ApplyInputs(
                _projectSession.ThermalState.Snapshot.Inputs,
                ThermalMutationOrigin.SystemApply);
        }

        #endregion

        #region Гидравлический расчёт

        /// <inheritdoc/>
        public bool HydraulicsIsCalculating =>
            _projectSession.HydraulicsState.Snapshot.Status.Phase == HydraulicsCalculationPhase.Calculating;

        /// <inheritdoc/>
        public string HydraulicsValidationMessage => _projectSession.HydraulicsState.Snapshot.Status.ValidationMessage;

        /// <inheritdoc/>
        public void SetHydraulicsCalculating()
        {
            _projectSession.HydraulicsState.BeginCalculation();
        }

        /// <inheritdoc/>
        public void SetHydraulicsError(string message)
        {
            _projectSession.HydraulicsState.FailCalculation(message);
        }

        /// <inheritdoc/>
        public void ResetHydraulicsState()
        {
            _projectSession.HydraulicsState.ApplyGlobalInputs(
                _projectSession.HydraulicsState.Snapshot.GlobalInputs,
                HydraulicsMutationOrigin.SystemApply);
        }

        #endregion

        #region Параметры конструкции

        /// <inheritdoc/>
        public int PipeSpacing => _projectSession.ThermalState.Snapshot.Inputs.PipeSpacing;

        /// <inheritdoc/>
        public event EventHandler<int>? PipeSpacingChanged;

        /// <inheritdoc/>
        public bool IsLoadProjectInProgress
        {
            get => _projectSession.IsLoadProjectInProgress;
            set
            {
                if (value)
                {
                    if (_restoreLease != null)
                    {
                        return;
                    }

                    _restoreLease = _projectSession.BeginProjectRestore();
                }
                else
                {
                    var lease = _restoreLease;
                    _restoreLease = null;
                    lease?.Dispose();
                }
            }
        }

        /// <inheritdoc/>
        public void SetPipeSpacing(int spacing)
        {
            SetPipeSpacing(spacing, "ThermalViewModel");
        }

        /// <inheritdoc/>
        public void SetPipeSpacing(int spacing, string source)
        {
            if (source != "ThermalViewModel" &&
                !(source == "ProjectLoadOrchestrator.RestoreModules" && IsLoadProjectInProgress))
            {
                throw new InvalidOperationException($"SetPipeSpacing called from non-canonical source: {source}");
            }

            // Временная поверхность записи шага укладки (DEC-T06): правка уходит
            // напрямую в каноническое состояние без dirty; origin с flow-through
            // статусом гарантирует наследуемое поведение "только PipeSpacingChanged,
            // ноль StateChanged" (заморожено characterization Todo 2).
            _projectSession.ThermalState.ApplyInputEdit(
                ThermalInputEdit.ForPipeSpacing(spacing),
                ThermalMutationOrigin.Calculation);
        }

        #endregion

        #region Событие

        /// <inheritdoc/>
        public event EventHandler<ModuleStateChangedEventArgs>? StateChanged;

        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Трансляция канонического завершения в legacy-события: изменение шага
        /// укладки даёт ровно одно PipeSpacingChanged; изменение статуса даёт ровно
        /// одно StateChanged с фазой канонического состояния; NoChange/Rejected не
        /// дают событий.
        /// </summary>
        private void OnThermalStateChanged(object? sender, ThermalStateChangedEventArgs e)
        {
            var mutation = e.Mutation;
            if (!mutation.IsChanged)
            {
                return;
            }

            // Сброс жизненного цикла (ProjectLoadReset, Todo 9 / AMZ-2) канонически
            // применяет дефолты, но сохраняет замороженную наблюдаемую тишину
            // legacy-поверхности (StateChanged/PipeSpacingChanged): каноническое
            // завершение доступно подписчикам ThermalState.Changed.
            if (mutation.Origin == ThermalMutationOrigin.ProjectLoadReset)
            {
                return;
            }

            if (mutation.Before.Inputs.PipeSpacing != mutation.After.Inputs.PipeSpacing)
            {
                PipeSpacingChanged?.Invoke(this, mutation.After.Inputs.PipeSpacing);
            }

            if (!mutation.Before.Status.Equals(mutation.After.Status))
            {
                var phase = MapPhase(mutation.After.Status.Phase);
                var message = phase == ModuleState.NeedsRecalculation
                    ? mutation.After.Status.RecalculationMessage
                    : null;
                OnStateChanged("Thermal", phase, message);
            }
        }

        private void OnHydraulicsStateChanged(object? sender, HydraulicsStateChangedEventArgs e)
        {
            if (!e.OldSnapshot.Status.Equals(e.NewSnapshot.Status))
            {
                var state = e.NewSnapshot.Status.Phase switch
                {
                    HydraulicsCalculationPhase.Actual => ModuleState.Actual,
                    HydraulicsCalculationPhase.Calculating => ModuleState.Calculating,
                    HydraulicsCalculationPhase.Error => ModuleState.Error,
                    _ => throw new ArgumentOutOfRangeException(nameof(e), e.NewSnapshot.Status.Phase, "Unknown hydraulics phase.")
                };
                OnStateChanged("Hydraulics", state,
                    state == ModuleState.Error ? e.NewSnapshot.Status.ValidationMessage : null);
            }
        }

        private static ModuleState MapPhase(ThermalCalculationPhase phase)
        {
            return phase switch
            {
                ThermalCalculationPhase.Actual => ModuleState.Actual,
                ThermalCalculationPhase.NeedsRecalculation => ModuleState.NeedsRecalculation,
                ThermalCalculationPhase.Calculating => ModuleState.Calculating,
                _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown thermal phase.")
            };
        }

        /// <summary>
        /// Вызвать событие изменения состояния
        /// </summary>
        /// <param name="module">Модуль</param>
        /// <param name="state">Новое состояние</param>
        /// <param name="message">Сообщение (опционально)</param>
        protected virtual void OnStateChanged(string module, ModuleState state, string? message = null)
        {
            StateChanged?.Invoke(this, new ModuleStateChangedEventArgs
            {
                Module = module,
                State = state,
                Message = message
            });
        }

        #endregion
    }
}
