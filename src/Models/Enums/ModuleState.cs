// ================================================================================
// REHAU Снеготаяние - Перечисление состояний модуля расчёта
// ================================================================================
//
// Соответствует: design_guidelines.md
// Используется: CalculationStateService, ModuleStateChangedEventArgs
//
// ================================================================================

namespace SnowMeltingCalculator.Models.Enums
{
    /// <summary>
    /// Состояние модуля расчёта
    /// </summary>
    public enum ModuleState
    {
        /// <summary>
        /// Данные актуальны
        /// </summary>
        Actual,
        
        /// <summary>
        /// Требуется пересчёт
        /// </summary>
        NeedsRecalculation,
        
        /// <summary>
        /// Выполняется расчёт
        /// </summary>
        Calculating
    }
}