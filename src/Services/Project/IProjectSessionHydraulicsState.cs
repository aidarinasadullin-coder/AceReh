using System;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Services.Project
{
    public interface IProjectSessionHydraulicsState
    {
        HydraulicsStateSnapshot Snapshot { get; }
        event EventHandler<HydraulicsStateChangedEventArgs>? Changed;
        HydraulicsMutationResult ApplyGlobalInputs(HydraulicGlobalInputsSnapshot candidate, HydraulicsMutationOrigin origin);
        HydraulicsMutationResult ReplaceCollectors(IEnumerable<HydraulicCollectorSnapshot> collectors, HydraulicsMutationOrigin origin);
        HydraulicsMutationResult BeginCalculation();
        HydraulicsMutationResult CompleteCalculation(IReadOnlyList<HydraulicCollectorSnapshot> results, IReadOnlyDictionary<int, HydraulicCollectorSummarySnapshot> summaryByCollector, HydraulicsMutationOrigin origin = HydraulicsMutationOrigin.Calculation);

        /// <summary>Фиксация результатов вместе со свойствами теплоносителя
        /// расчёта (ADR-013); glycol-параметры null — свойства не фиксируются.</summary>
        HydraulicsMutationResult CompleteCalculation(IReadOnlyList<HydraulicCollectorSnapshot> results, IReadOnlyDictionary<int, HydraulicCollectorSummarySnapshot> summaryByCollector, GlycolPropertiesSnapshot? operatingGlycol, GlycolPropertiesSnapshot? designGlycol, HydraulicsMutationOrigin origin = HydraulicsMutationOrigin.Calculation);
        HydraulicsMutationResult FailCalculation(string message);
        HydraulicsMutationResult Restore(HydraulicsStateSnapshot snapshot, HydraulicsMutationOrigin origin);
        HydraulicsMutationResult ResetToDefaults(HydraulicsMutationOrigin origin);
    }
}
