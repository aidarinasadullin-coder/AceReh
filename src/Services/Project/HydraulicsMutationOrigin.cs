namespace SnowMeltingCalculator.Services.Project
{
    public enum HydraulicsMutationOrigin
    {
        User,
        UserReset,
        ProjectLoadReset,
        ProjectLoad,
        Calculation,
        Initialization,
        SystemApply,

        /// <summary>Undo/Redo-восстановление снимка дневника отмены (ADR-014):
        /// dirty не создаёт.</summary>
        Undo,

        /// <summary>См. <see cref="Undo"/> (ADR-014).</summary>
        Redo
    }
}
