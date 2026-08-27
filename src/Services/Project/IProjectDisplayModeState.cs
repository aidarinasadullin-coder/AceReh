namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Application-owned state seam for the persisted Results display mode.
    /// </summary>
    public interface IProjectDisplayModeState
    {
        bool IsOperatingMode { get; set; }
    }
}
