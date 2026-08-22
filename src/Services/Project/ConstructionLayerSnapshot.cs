using System;
using SnowMeltingCalculator.Models.Construction;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Immutable snapshot of one canonical layer within <see cref="ConstructionStateSnapshot"/>.
    /// Field-by-field equality is provided by the record's synthesized Equals/GetHashCode.
    /// </summary>
    public sealed record ConstructionLayerSnapshot(
        Guid Id,
        int MaterialId,
        string MaterialName,
        double Thickness,
        double CalculatedLambda,
        bool IsLambdaOverridden,
        LayerPosition Position,
        int Order);
}
