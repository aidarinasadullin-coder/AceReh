// ================================================================================
// REHAU Снеготаяние - Контракт канонической границы применения тепловых команд
// ================================================================================
//
// Phase 4 Todos 5+6+7 merged boundary (AMZ-1). DEC-T04A: sealed singleton
// coordinator owning every canonical Thermal mutation, the single dirty-intent
// path for changed user edits, DEC-T05 calculation orchestration and the sole
// upstream Climate/Construction subscriptions moved atomically from
// ThermalViewModel.
//
// ================================================================================

using System;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Итог одного расчёта, управляемого координатором: доменный результат
    /// (или <c>null</c> после исключения) и собранное сообщение валидации.
    /// </summary>
    public sealed class ThermalCalculationOutcome
    {
        /// <summary>Доменный результат расчёта; <c>null</c> только при исключении калькулятора.</summary>
        public ThermalCalculationResult? Result { get; }

        /// <summary>Сообщения пост-валидации результата либо точный текст ошибки расчёта.</summary>
        public string ValidationMessage { get; }

        public ThermalCalculationOutcome(ThermalCalculationResult? result, string validationMessage)
        {
            Result = result;
            ValidationMessage = validationMessage ?? string.Empty;
        }
    }

    /// <summary>
    /// Каноническая граница применения тепловых команд пользователя и оркестрации
    /// расчёта. Единственный писатель <see cref="IProjectSessionThermalState"/>
    /// вне persistence/restore путей; единственный владелец dirty-intent для
    /// изменённых пользовательских правок и единственный подписчик upstream
    /// Climate/Construction уведомлений (DEC-T04A).
    /// </summary>
    public interface IThermalStateCoordinator : IDisposable
    {
        /// <summary>Каноническое состояние, с которым работает координатор (reference-identical).</summary>
        IProjectSessionThermalState State { get; }

        /// <summary>
        /// Единственное каноническое завершение мутации (только Changed);
        /// адаптеры используют его для обновления привязок.
        /// </summary>
        event EventHandler<ThermalStateChangedEventArgs>? Completion;

        /// <summary>
        /// Сигнал наблюдаемого upstream-изменения (включая случаи без канонического
        /// эффекта); предназначен только для refresh-уведомлений адаптера.
        /// </summary>
        event EventHandler? UpstreamObserved;

        /// <summary>Признак выполняющегося расчёта (реентерабельность DEC-T05).</summary>
        bool IsCalculating { get; }

        /// <summary>
        /// Применить одиночную правку входных данных от имени пользователя:
        /// одна каноническая мутация + один dirty-intent при Changed;
        /// no-op/rejected — нулевой эффект.
        /// </summary>
        ThermalMutationResult ApplyInputEdit(ThermalInputEdit edit);

        /// <summary>
        /// Пользовательский/жизненный сброс адаптера. Наследуемое наблюдаемое
        /// поведение ST-013/ST-015: НЕ мутирует каноническое состояние, не создаёт
        /// событий и dirty — канонические значения заменяются только Calculate/
        /// Restore путями (DEC-T03 "preserves current observable behavior").
        /// </summary>
        void Reset();

        /// <summary>
        /// Оркестрация расчёта по точному порядку DEC-T05: BeginCalculation →
        /// публикация входов → один вызов калькулятора → Complete/Fail →
        /// публикация результата. Реентерабельный вызов во время расчёта — no-op.
        /// </summary>
        Task<ThermalCalculationOutcome> CalculateAsync(ThermalInputs inputs);

        /// <summary>
        /// Восстановительный путь загрузки проекта: канонический Restore +
        /// публикации UpdateThermalInputs затем UpdateThermal в замороженном
        /// порядке (DEC-T08).
        /// </summary>
        void LoadResult(ThermalCalculationResult result, ThermalInputs inputs);

        /// <summary>
        /// Каноническое восстановление полного теплового состояния при отмене/
        /// возврате действия (ADR-014): <c>RestoreState</c> слайса со статусом ИЗ
        /// снимка + пере-публикации <c>UpdateThermalInputs</c> затем
        /// <c>UpdateThermal</c> по образцу <see cref="LoadResult"/>. Источник —
        /// <see cref="ThermalMutationOrigin.Undo"/> либо
        /// <see cref="ThermalMutationOrigin.Redo"/>; другой origin отклоняется.
        /// </summary>
        void RestoreState(ThermalStateSnapshot snapshot, ThermalMutationOrigin origin);
    }
}
