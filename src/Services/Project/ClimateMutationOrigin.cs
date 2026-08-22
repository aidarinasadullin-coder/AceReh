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
        Initialization
    }
}
