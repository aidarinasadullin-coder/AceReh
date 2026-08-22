using System;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Arguments raised by <see cref="IProjectSessionConstructionState.Changed"/>
    /// when a canonical Construction mutation is completed.
    /// </summary>
    public sealed class ConstructionStateChangedEventArgs : EventArgs
    {
        public ConstructionMutationOrigin Origin { get; }
        public ConstructionStateSnapshot Before { get; }
        public ConstructionStateSnapshot After { get; }

        public ConstructionStateChangedEventArgs(
            ConstructionMutationOrigin origin,
            ConstructionStateSnapshot before,
            ConstructionStateSnapshot after)
        {
            Origin = origin;
            Before = before ?? throw new ArgumentNullException(nameof(before));
            After = after ?? throw new ArgumentNullException(nameof(after));
        }
    }
}
