namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// The origin of a canonical Construction mutation.
    /// User-visible origins (User, Template) mark the project dirty;
    /// non-user origins (ProjectLoad, Reset, Restore, SystemApply, Initialization)
    /// must never create user dirty/history semantics.
    /// FileLoad preserves current standalone-construction command semantics.
    /// </summary>
    public enum ConstructionMutationOrigin
    {
        User,
        Template,
        FileLoad,
        ProjectLoad,
        Reset,
        Restore,
        SystemApply,
        Initialization
    }
}
