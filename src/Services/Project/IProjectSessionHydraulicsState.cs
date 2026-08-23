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
        HydraulicsMutationResult FailCalculation(string message);
        HydraulicsMutationResult Restore(HydraulicsStateSnapshot snapshot, HydraulicsMutationOrigin origin);
        HydraulicsMutationResult ResetToDefaults(HydraulicsMutationOrigin origin);
    }
}
