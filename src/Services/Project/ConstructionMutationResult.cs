namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Result returned by every canonical Construction mutation method.
    /// </summary>
    public sealed class ConstructionMutationResult
    {
        public ConstructionMutationStatus Status { get; }
        public ConstructionMutationOrigin Origin { get; }
        public ConstructionStateSnapshot Before { get; }
        public ConstructionStateSnapshot After { get; }
        public string? ErrorCode { get; }

        public bool IsChanged => Status == ConstructionMutationStatus.Changed;
        public bool IsNoChange => Status == ConstructionMutationStatus.NoChange;
        public bool IsRejected => Status == ConstructionMutationStatus.Rejected;
        public bool IsCancelled => Status == ConstructionMutationStatus.Cancelled;

        public ConstructionMutationResult(
            ConstructionMutationStatus status,
            ConstructionMutationOrigin origin,
            ConstructionStateSnapshot before,
            ConstructionStateSnapshot after,
            string? errorCode = null)
        {
            Status = status;
            Origin = origin;
            Before = before ?? throw new System.ArgumentNullException(nameof(before));
            After = after ?? throw new System.ArgumentNullException(nameof(after));
            ErrorCode = errorCode;
        }
    }
}
