using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Непротиворечивый срез канонического климатического состояния проекта.
    /// </summary>
    public sealed record ClimateStateSnapshot(
        string SelectedCity,
        string SelectedRegion,
        double AirTemperature,
        double ColdFiveDayTemperature,
        double WindSpeed,
        double Humidity,
        double SnowfallIntensity,
        ClimateZone Zone,
        bool IsHighRequirements,
        bool IsCitySelected,
        bool HasUserModifications) : IClimateData
    {
        public event EventHandler<ClimateDataChangedEventArgs>? DataChanged
        {
            add { }
            remove { }
        }
    }
}
