namespace SnowMeltingCalculator.Services.Project
{
    public interface IProjectSnapshotFactory
    {
        ProjectSnapshot Create(IProjectSession projectSession);
    }
}
