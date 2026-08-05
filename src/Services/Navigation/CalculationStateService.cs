// ================================================================================
// REHAU Снеготаяние - Реализация сервиса состояния расчёта
// ================================================================================
//
// Соответствует: design_guidelines.md
// Реализует: ICalculationStateService
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

        private bool _thermalNeedsRecalculation;
        private bool _thermalIsCalculating;
        private string _thermalValidationMessage = string.Empty;

        private bool _hydraulicsIsCalculating;
        private string _hydraulicsValidationMessage = string.Empty;

        private int _pipeSpacing = 200; // Шаг укладки по умолчанию

        private readonly IProjectSession _projectSession;
        private IDisposable? _restoreLease;

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт сервис состояния расчёта с собственным экземпляром сессии проекта.
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
        }

        #endregion

        #region ICalculationStateService Implementation

        #region Тепловой расчёт

        /// <inheritdoc/>
        public bool ThermalNeedsRecalculation => _thermalNeedsRecalculation;

        /// <inheritdoc/>
        public bool ThermalIsCalculating => _thermalIsCalculating;

        /// <inheritdoc/>
        public string ThermalValidationMessage => _thermalValidationMessage;

        /// <inheritdoc/>
        public void SetThermalNeedsRecalculation(string message)
        {
            _thermalNeedsRecalculation = true;
            _thermalValidationMessage = message;
            OnStateChanged("Thermal", ModuleState.NeedsRecalculation, message);
        }

        /// <inheritdoc/>
        public void SetThermalCalculating()
        {
            _thermalIsCalculating = true;
            _thermalNeedsRecalculation = false;
            OnStateChanged("Thermal", ModuleState.Calculating);
        }

        /// <inheritdoc/>
        public void ResetThermalState()
        {
            _thermalIsCalculating = false;
            _thermalNeedsRecalculation = false;
            _thermalValidationMessage = string.Empty;
            OnStateChanged("Thermal", ModuleState.Actual);
        }

        #endregion

        #region Гидравлический расчёт

        /// <inheritdoc/>
        public bool HydraulicsIsCalculating => _hydraulicsIsCalculating;

        /// <inheritdoc/>
        public string HydraulicsValidationMessage => _hydraulicsValidationMessage;

        /// <inheritdoc/>
        public void SetHydraulicsCalculating()
        {
            _hydraulicsIsCalculating = true;
            _hydraulicsValidationMessage = string.Empty;
            OnStateChanged("Hydraulics", ModuleState.Calculating);
        }

        /// <inheritdoc/>
        public void SetHydraulicsError(string message)
        {
            _hydraulicsIsCalculating = false;
            _hydraulicsValidationMessage = message;
            OnStateChanged("Hydraulics", ModuleState.Error, message);
        }

        /// <inheritdoc/>
        public void ResetHydraulicsState()
        {
            _hydraulicsIsCalculating = false;
            _hydraulicsValidationMessage = string.Empty;
            OnStateChanged("Hydraulics", ModuleState.Actual);
        }

        #endregion

        #region Параметры конструкции

        /// <inheritdoc/>
        public int PipeSpacing => _pipeSpacing;

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

            if (_pipeSpacing != spacing)
            {
                _pipeSpacing = spacing;
                PipeSpacingChanged?.Invoke(this, spacing);
            }
        }

        #endregion

        #region Событие

        /// <inheritdoc/>
        public event EventHandler<ModuleStateChangedEventArgs>? StateChanged;

        #endregion

        #endregion

        #region Private Methods

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