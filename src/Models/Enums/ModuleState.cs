// ================================================================================
// REHAU Снеготаяние - Перечисление состояний модуля расчёта
// ================================================================================
//
// Соответствует: docs/recalc_indicators_design.md (дизайн по брендбуку REHAU 2026)
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
        Calculating,

        /// <summary>
        /// Ошибка валидации входных данных
        /// </summary>
        Error
    }
}