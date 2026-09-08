namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Источник канонической мутации теплового состояния проекта (DEC-T02).
    /// Пользовательский сброс (<see cref="UserReset"/>) никогда не смешивается
    /// с жизненным циклом проекта (<see cref="ProjectLoadReset"/>, <see cref="ProjectLoad"/>).
    /// </summary>
    public enum ThermalMutationOrigin
    {
        /// <summary>Пользовательское изменение входных данных.</summary>
        User,

        /// <summary>Пользовательский сброс к дефолтам (не помечает проект грязным).</summary>
        UserReset,

        /// <summary>Сброс модулей при новом расчёте / жизненном цикле проекта.</summary>
        ProjectLoadReset,

        /// <summary>Восстановление сохранённого состояния при загрузке проекта.</summary>
        ProjectLoad,

        /// <summary>Инвалидация со стороны климатического среза.</summary>
        ClimateInvalidation,

        /// <summary>Инвалидация со стороны среза конструкции.</summary>
        ConstructionInvalidation,

        /// <summary>Жизненный цикл расчёта (начало/завершение/ошибка).</summary>
        Calculation,

        /// <summary>Начальная инициализация состояния.</summary>
        Initialization,

        /// <summary>Системное применение значений (не пользовательское).</summary>
        SystemApply,

        /// <summary>Undo-применение снимка «до» из дневника отмены (ADR-014):
        /// dirty не создаёт, статус восстанавливается из снимка.</summary>
        Undo,

        /// <summary>Redo-применение снимка «после» из дневника отмены (ADR-014).</summary>
        Redo
    }
}
