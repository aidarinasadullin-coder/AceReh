// ================================================================================
// REHAU Снеготаяние - Аргументы события изменения состояния модуля
// ================================================================================
//
// Соответствует: docs/recalc_indicators_design.md (дизайн по брендбуку REHAU 2026)
// Используется: CalculationStateService
//
// ================================================================================

using SnowMeltingCalculator.Models.Enums;

namespace SnowMeltingCalculator.Models.Navigation
{
    /// <summary>
    /// Аргументы события изменения состояния модуля
    /// </summary>
    public class ModuleStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Модуль, состояние которого изменилось
        /// </summary>
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// Новое состояние модуля
        /// </summary>
        public ModuleState State { get; set; }

        /// <summary>
        /// Сообщение (для Warning)
        /// </summary>
        public string? Message { get; set; }
    }
}