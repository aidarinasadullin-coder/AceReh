// ================================================================================
// REHAU Снеготаяние - Интерфейс сервиса состояния расчёта
// ================================================================================
//
// Соответствует: design_guidelines.md
// Используется: CalculationStateService, ViewModels
//
// ================================================================================

using SnowMeltingCalculator.Models.Navigation;

namespace SnowMeltingCalculator.Services.Navigation
{
    /// <summary>
    /// Сервис для управления состоянием расчёта модулей
    /// </summary>
    public interface ICalculationStateService
    {
        #region Тепловой расчёт
        
        /// <summary>
        /// Признак того, что тепловой расчёт требует пересчёта
        /// </summary>
        bool ThermalNeedsRecalculation { get; }
        
        /// <summary>
        /// Признак того, что тепловой расчёт выполняется
        /// </summary>
        bool ThermalIsCalculating { get; }
        
        /// <summary>
        /// Сообщение о необходимости пересчёта теплового расчёта
        /// </summary>
        string ThermalValidationMessage { get; }
        
        /// <summary>
        /// Установить флаг необходимости пересчёта теплового расчёта
        /// </summary>
        /// <param name="message">Сообщение о причине пересчёта</param>
        void SetThermalNeedsRecalculation(string message);
        
        /// <summary>
        /// Установить флаг выполнения расчёта теплового расчёта
        /// </summary>
        void SetThermalCalculating();
        
        /// <summary>
        /// Сбросить состояние теплового расчёта
        /// </summary>
        void ResetThermalState();
        
        #endregion
        
        #region Гидравлический расчёт
        
        /// <summary>
        /// Признак того, что гидравлический расчёт выполняется
        /// </summary>
        bool HydraulicsIsCalculating { get; }
        
        /// <summary>
        /// Установить флаг выполнения расчёта гидравлического расчёта
        /// </summary>
        void SetHydraulicsCalculating();
        
        /// <summary>
        /// Сбросить состояние гидравлического расчёта
        /// </summary>
        void ResetHydraulicsState();
        
        #endregion
        
        #region Событие
        
        /// <summary>
        /// Событие изменения состояния
        /// </summary>
        event EventHandler<ModuleStateChangedEventArgs>? StateChanged;
        
        #endregion
    }
}