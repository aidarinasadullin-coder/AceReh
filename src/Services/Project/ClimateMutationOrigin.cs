namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Источник канонической мутации климатического состояния проекта.
    /// </summary>
    public enum ClimateMutationOrigin
    {
        User,
        UserReset,
        Load,
        ProjectLoadReset,
        Restore,
        SystemApply,
        Initialization,

        /// <summary>
        /// Каноническое применение снимка «до» при отмене действия
        /// (memento-дневник, ADR-014). Dirty не создаёт, DataChanged не поднимает.
        /// </summary>
        Undo,

        /// <summary>
        /// Каноническое применение снимка «после» при возврате отменённого
        /// действия (memento-дневник, ADR-014). Dirty не создаёт, DataChanged не поднимает.
        /// </summary>
        Redo
    }
}
