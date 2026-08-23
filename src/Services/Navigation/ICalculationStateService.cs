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
        /// Установить флаг необходимости пересчёта теплового расчёта.
        /// Мост AMZ-1: маршрутизируется в каноническую переходную мутацию
        /// ApplyNeedsRecalculation (см. evidence/phase-4-thermal-state/task-5/
        /// blocker-analysis.md); production-вызовов после Todos 5-7 не осталось.
        /// </summary>
        /// <param name="message">Сообщение о причине пересчёта</param>
        void SetThermalNeedsRecalculation(string message);

        /// <summary>
        /// Установить флаг выполнения расчёта теплового расчёта.
        /// Мост AMZ-1: маршрутизируется в канонический BeginCalculation.
        /// </summary>
        void SetThermalCalculating();

        /// <summary>
        /// Сбросить состояние теплового расчёта.
        /// Мост AMZ-1: маршрутизируется в каноническое применение текущих входов
        /// с origin SystemApply (нормализация статуса к Actual).
        /// </summary>
        void ResetThermalState();

        #endregion

        #region Гидравлический расчёт

        /// <summary>
        /// Признак того, что гидравлический расчёт выполняется
        /// </summary>
        bool HydraulicsIsCalculating { get; }

        /// <summary>
        /// Сообщение об ошибке валидации гидравлического расчёта
        /// </summary>
        string HydraulicsValidationMessage { get; }

        /// <summary>
        /// Установить флаг выполнения расчёта гидравлического расчёта
        /// </summary>
        void SetHydraulicsCalculating();

        /// <summary>
        /// Установить флаг ошибки валидации гидравлического расчёта
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        void SetHydraulicsError(string message);

        /// <summary>
        /// Сбросить состояние гидравлического расчёта
        /// </summary>
        void ResetHydraulicsState();

        #endregion

        #region Параметры конструкции

        /// <summary>
        /// Шаг укладки труб, мм
        /// Используется для визуализации в модуле "Конструкция"
        /// </summary>
        int PipeSpacing { get; }

        /// <summary>
        /// Событие изменения шага укладки
        /// </summary>
        event EventHandler<int>? PipeSpacingChanged;

        /// <summary>
        /// Признак выполнения загрузки проекта
        /// </summary>
        bool IsLoadProjectInProgress { get; set; }

        /// <summary>
        /// Установить шаг укладки труб
        /// </summary>
        /// <param name="spacing">Шаг укладки, мм</param>
        void SetPipeSpacing(int spacing);

        /// <summary>
        /// Установить шаг укладки труб с указанием источника вызова
        /// </summary>
        /// <param name="spacing">Шаг укладки, мм</param>
        /// <param name="source">Источник вызова</param>
        void SetPipeSpacing(int spacing, string source);

        #endregion

        #region Событие

        /// <summary>
        /// Событие изменения состояния
        /// </summary>
        event EventHandler<ModuleStateChangedEventArgs>? StateChanged;

        #endregion
    }
}