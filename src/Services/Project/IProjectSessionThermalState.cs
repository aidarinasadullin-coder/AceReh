using System;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Канонический срез теплового состояния проекта (DEC-T01/T02).
    /// Единственный writable owner Thermal inputs, шага укладки, последнего
    /// производного результата и статуса после переноса владения. Создаётся и
    /// хранится <c>ProjectSession</c>; самостоятельно в DI не регистрируется.
    /// Семантика уровня состояния: класс не публикует контекст расчёта, не
    /// вызывает dirty-сервис и не рассылает compatibility-события — это зона
    /// последующих задач подключения (Todos 4-8).
    /// </summary>
    public interface IProjectSessionThermalState
    {
        /// <summary>
        /// Текущий непротиворечивый срез состояния.
        /// </summary>
        ThermalStateSnapshot Snapshot { get; }

        /// <summary>
        /// Единственное каноническое событие завершения. Возникает ровно один раз
        /// ПОСЛЕ атомарной замены для каждой мутации со статусом Changed;
        /// NoChange/Rejected не порождают событий. Несёт полный результат мутации.
        /// </summary>
        event EventHandler<ThermalStateChangedEventArgs>? Changed;

        /// <summary>
        /// Применить полный кандидат входных данных. Валидация/нормализация
        /// завершаются ДО атомарной замены; невалидный кандидат отклоняется без
        /// изменения состояния и без событий.
        /// </summary>
        ThermalMutationResult ApplyInputs(ThermalInputsSnapshot candidate, ThermalMutationOrigin origin);

        /// <summary>
        /// Применить одиночную правку одного поля входных данных.
        /// </summary>
        ThermalMutationResult ApplyInputEdit(ThermalInputEdit edit, ThermalMutationOrigin origin);

        /// <summary>
        /// Сбросить входные данные, результат и статус к точным дефолтам DEC-T01.
        /// Пользовательский сброс не имеет dirty-последствий (dirty вне зоны класса).
        /// </summary>
        ThermalMutationResult ResetToDefaults(ThermalMutationOrigin origin);

        /// <summary>
        /// Начать расчёт: фаза Calculating, сообщения очищаются.
        /// Повторный вызов во время расчёта — NoChange (реентерабельность).
        /// </summary>
        ThermalMutationResult BeginCalculation();

        /// <summary>
        /// Завершить расчёт: канонически сохранить результат, фаза Actual,
        /// сообщение пересчёта очищено, передано сообщение валидации результата.
        /// </summary>
        ThermalMutationResult CompleteCalculation(
            ThermalInputsSnapshot calculatedInputs,
            ThermalResultSnapshot result,
            string validationMessage);

        /// <summary>
        /// Зафиксировать ошибку расчёта: сохраняется <paramref name="compatibilityInvalidResult"/>,
        /// если он передан, иначе результат становится null; фаза Actual; точный текст
        /// сообщения об ошибке сохраняется в ValidationMessage.
        /// </summary>
        ThermalMutationResult FailCalculation(
            ThermalInputsSnapshot calculatedInputs,
            string validationMessage,
            ThermalResultSnapshot? compatibilityInvalidResult = null);

        /// <summary>
        /// Восстановить состояние при загрузке проекта. Источник мутации жёстко
        /// привязан к <see cref="ThermalMutationOrigin.ProjectLoad"/> внутри метода
        /// (сигнатура DEC-T02: Restore(inputs, savedResult, ProjectLoad)).
        /// </summary>
        ThermalMutationResult Restore(ThermalInputsSnapshot inputs, ThermalResultSnapshot? savedResult);

        /// <summary>
        /// Каноническое восстановление полного среза при отмене/возврате действия
        /// (ADR-014): атомарно inputs+result+статус ИЗ снимка — статус может быть
        /// <see cref="ThermalCalculationPhase.NeedsRecalculation"/> с сообщением
        /// (не нормализуется к <see cref="ThermalStatusSnapshot.Default"/>, в отличие
        /// от <see cref="Restore"/>). Предназначено только для origins
        /// <see cref="ThermalMutationOrigin.Undo"/>/<see cref="ThermalMutationOrigin.Redo"/>.
        /// </summary>
        /// <param name="snapshot">Полный снимок «до»/«после» из дневника отмены.</param>
        /// <param name="origin">Источник мутации (<see cref="ThermalMutationOrigin.Undo"/>
        /// или <see cref="ThermalMutationOrigin.Redo"/>).</param>
        /// <returns>Результат мутации.</returns>
        ThermalMutationResult RestoreState(ThermalStateSnapshot snapshot, ThermalMutationOrigin origin);

        /// <summary>
        /// Инвалидация от климата: результат очищается и статус переходит в
        /// NeedsRecalculation ровно один раз ТОЛЬКО если результат существовал;
        /// иначе — нулевой эффект (замороженное поведение DEC-T04).
        /// </summary>
        ThermalMutationResult InvalidateFromClimate(string message);

        /// <summary>
        /// Инвалидация от конструкции; семантика как у <see cref="InvalidateFromClimate"/>.
        /// </summary>
        ThermalMutationResult InvalidateFromConstruction(string message);

        /// <summary>
        /// ПЕРЕХОДНАЯ мутация моста AMZ-1 (владелец одобрил отклонение, см.
        /// evidence/phase-4-thermal-state/task-5/blocker-analysis.md): выражает
        /// legacy <c>SetThermalNeedsRecalculation(message)</c> без второго
        /// writable-хранилища. Сохраняет входы и результат, переводит фазу в
        /// NeedsRecalculation с точным сообщением; ровно одно завершение при
        /// изменении (идемпотентно по значению), ноль при no-op.
        /// Todo 11 обязан доказать отсутствие не-адаптерных production-вызовов.
        /// </summary>
        ThermalMutationResult ApplyNeedsRecalculation(string recalculationMessage, ThermalMutationOrigin origin);
    }
}
