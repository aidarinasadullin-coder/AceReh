namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Климатические параметры для расчёта системы снеготаяния
    /// </summary>
    public class ClimateParameters
    {
        /// <summary>
        /// Название выбранного города
        /// </summary>
        public string CityName { get; set; } = string.Empty;

        /// <summary>
        /// Регион города
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// Расчётная температура наружного воздуха, °C
        /// По умолчанию -15°C
        /// Диапазон: -50 до +10°C
        /// </summary>
        public double AirTemperature { get; set; } = -15.0;

        /// <summary>
        /// Скорость ветра, м/с
        /// По умолчанию 5 м/с
        /// Диапазон: 0.1 до 30 м/с
        /// </summary>
        public double WindSpeed { get; set; } = 5.0;

        /// <summary>
        /// Относительная влажность воздуха, %
        /// По умолчанию 70%
        /// Диапазон: 20 до 100%
        /// </summary>
        public double Humidity { get; set; } = 70.0;

        /// <summary>
        /// Интенсивность снегопада, мм/ч (водяной эквивалент)
        /// НЕ берётся из СП 131.13330.2025, вводится вручную
        /// По умолчанию 0 мм/ч
        /// Диапазон: 0 до 20 мм/ч
        /// </summary>
        public double SnowfallIntensity { get; set; } = 0;

        /// <summary>
        /// Климатическая зона
        /// </summary>
        public ClimateZone Zone { get; set; } = ClimateZone.Zone_M15;

        /// <summary>
        /// Признак повышенных требований
        /// Если true, используется Zone_M20_Plus
        /// </summary>
        public bool IsHighRequirements { get; set; } = false;

        /// <summary>
        /// Признак того, что пользователь изменил данные вручную
        /// </summary>
        public bool HasUserModifications { get; set; } = false;

        /// <summary>
        /// Создать копию параметров
        /// </summary>
        public ClimateParameters Clone()
        {
            return new ClimateParameters
            {
                CityName = CityName,
                Region = Region,
                AirTemperature = AirTemperature,
                WindSpeed = WindSpeed,
                Humidity = Humidity,
                SnowfallIntensity = SnowfallIntensity,
                Zone = Zone,
                IsHighRequirements = IsHighRequirements,
                HasUserModifications = HasUserModifications
            };
        }
    }
}