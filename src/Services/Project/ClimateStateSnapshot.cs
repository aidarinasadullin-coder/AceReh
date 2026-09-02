using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Непротиворечивый срез канонического климатического состояния проекта.
    /// </summary>
    /// <remarks>
    /// <see cref="Period0Days"/> — канонический снимок <c>CityInfo.Period_0_Days</c>,
    /// извлекается в точках, где состояние получает <c>CityInfo</c> (выбор города,
    /// восстановление проекта, сброс к городу). Не персистится в <c>.smc</c>;
    /// 0 означает «город не найден в каталоге / город не выбран».
    /// </remarks>
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
        bool HasUserModifications,
        int Period0Days = 0) : IClimateData
    {
        public event EventHandler<ClimateDataChangedEventArgs>? DataChanged
        {
            add { }
            remove { }
        }
    }
}
