using System.Collections.Generic;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Результат одной канонической мутации климатического состояния.
    /// </summary>
    public sealed class ClimateMutationResult
    {
        /// <summary>
        /// Источник мутации.
        /// </summary>
        public ClimateMutationOrigin Origin { get; }

        /// <summary>
        /// Было ли изменение фактически применено (срез изменился).
        /// </summary>
        public bool IsChanged { get; }

        /// <summary>
        /// Были ли входные данные допустимы.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Ошибки валидации, если <see cref="IsValid"/> == false.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Срез состояния до мутации.
        /// </summary>
        public ClimateStateSnapshot OldSnapshot { get; }

        /// <summary>
        /// Срез состояния после мутации.
        /// </summary>
        public ClimateStateSnapshot NewSnapshot { get; }

        public ClimateMutationResult(
            ClimateMutationOrigin origin,
            bool isChanged,
            bool isValid,
            IReadOnlyList<string> errors,
            ClimateStateSnapshot oldSnapshot,
            ClimateStateSnapshot newSnapshot)
        {
            Origin = origin;
            IsChanged = isChanged;
            IsValid = isValid;
            Errors = errors;
            OldSnapshot = oldSnapshot;
            NewSnapshot = newSnapshot;
        }
    }
}
