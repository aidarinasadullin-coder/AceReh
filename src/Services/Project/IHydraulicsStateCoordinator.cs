using System;
using System.Collections.Generic;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Project
{
    public interface IHydraulicsStateCoordinator
    {
        void Connect(Func<List<CollectorSummary>?> calculateSelected, Func<List<CollectorSummary>?> calculateAll, Action notifyThermal, Action notifyClimate, Action<double> mirrorPipeSpacing);
        void Calculate(Func<List<CollectorSummary>?> calculation);
        void CalculateAll(Func<List<CollectorSummary>?> calculation);
        void ApplyPipeSpacing(int spacing, Action<double> mirror);
    }
}
