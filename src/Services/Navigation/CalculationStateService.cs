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
        public void SetHydraulicsCalculating()
        {
            _hydraulicsIsCalculating = true;
            OnStateChanged("Hydraulics", ModuleState.Calculating);
        }
        
        /// <inheritdoc/>
        public void ResetHydraulicsState()
        {
            _hydraulicsIsCalculating = false;
            OnStateChanged("Hydraulics", ModuleState.Actual);
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