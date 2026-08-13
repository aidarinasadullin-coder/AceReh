namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Аргументы события изменения канонического климатического состояния.
    /// </summary>
    public sealed class ClimateStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Источник мутации, вызвавшей изменение.
        /// </summary>
        public ClimateMutationOrigin Origin { get; }

        /// <summary>
        /// Срез состояния до мутации.
        /// </summary>
        public ClimateStateSnapshot OldSnapshot { get; }

        /// <summary>
        /// Срез состояния после мутации.
        /// </summary>
        public ClimateStateSnapshot NewSnapshot { get; }

        public ClimateStateChangedEventArgs(ClimateMutationOrigin origin, ClimateStateSnapshot oldSnapshot, ClimateStateSnapshot newSnapshot)
        {
            Origin = origin;
            OldSnapshot = oldSnapshot;
            NewSnapshot = newSnapshot;
        }
    }
}
