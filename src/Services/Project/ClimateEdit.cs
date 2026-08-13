namespace SnowMeltingCalculator.Services.Project
{
    /// <summary>
    /// Поле канонического климатического состояния, доступное для индивидуального редактирования.
    /// </summary>
    public enum ClimateEditField
    {
        AirTemperature,
        ColdFiveDayTemperature,
        WindSpeed,
        Humidity,
        SnowfallIntensity,
        IsHighRequirements
    }

    /// <summary>
    /// Одна индивидуальная правка канонического климатического состояния.
    /// Для булевого поля <see cref="ClimateEditField.IsHighRequirements"/>
    /// значение интерпретируется как 1.0 (true) или 0.0 (false).
    /// </summary>
    public sealed record ClimateEdit(ClimateEditField Field, double Value);
}
