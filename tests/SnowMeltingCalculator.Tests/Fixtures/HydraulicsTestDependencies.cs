using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Project;

namespace SnowMeltingCalculator.Tests.Fixtures;

internal sealed record HydraulicsTestDependencies(
    IHydraulicsStateCoordinator Coordinator,
    IProjectSessionHydraulicsState State,
    IProjectSession Session);

internal static class HydraulicsTestDependencyFactory
{
    public static HydraulicsTestDependencies Create(
        ICalculationStateService calculationStateService,
        CalculationContext calculationContext,
        IProjectSession? session = null)
    {
        session ??= new ProjectSession(calculationContext: calculationContext);
        return new HydraulicsTestDependencies(
            new HydraulicsStateCoordinator(session.HydraulicsState, calculationStateService, calculationContext),
            session.HydraulicsState,
            session);
    }
}
