namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// The outcome status of a canonical Construction mutation attempt.
    /// </summary>
    public enum ConstructionMutationStatus
    {
        /// <summary>
        /// The candidate differed from the current snapshot and was atomically applied.
        /// </summary>
        Changed,

        /// <summary>
        /// The candidate was structurally identical to the current snapshot; no change was applied.
        /// </summary>
        NoChange,

        /// <summary>
        /// The candidate failed validation; the canonical snapshot is unchanged.
        /// </summary>
        Rejected,

        /// <summary>
        /// An application boundary cancelled before canonical apply (e.g. the user declined
        /// to import a missing material). No mutation, dirty or context update occurred.
        /// </summary>
        Cancelled
    }
}
