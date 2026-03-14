namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Интерфейс для передачи климатических данных другим модулям
    /// </summary>
    public interface IClimateData
    {
        /// <summary>
        /// Выбранный город
        /// </summary>
        string SelectedCity { get; }

        /// <summary>
        /// Регион города
        /// </summary>
        string SelectedRegion { get; }

        /// <summary>
        /// Расчётная температура наружного воздуха, °C
        /// </summary>
        double AirTemperature { get; }

        /// <summary>
        /// Температура холодной пятидневки из СП 131.13330.2025, °C
        /// </summary>
        double ColdFiveDayTemperature { get; }

        /// <summary>
        /// Скорость ветра, м/с
        /// </summary>
        double WindSpeed { get; }

        /// <summary>
        /// Относительная влажность, %
        /// </summary>
        double Humidity { get; }

        /// <summary>
        /// Интенсивность снегопада, см/ч
        /// </summary>
        double SnowfallIntensity { get; }

        /// <summary>
        /// Климатическая зона
        /// </summary>
        ClimateZone Zone { get; }

        /// <summary>
        /// Событие изменения данных
        /// </summary>
        event EventHandler<ClimateDataChangedEventArgs>? DataChanged;
    }

    /// <summary>
    /// Реализация интерфейса климатических данных
    /// </summary>
    public class ClimateData : IClimateData
    {
        public string SelectedCity { get; set; } = string.Empty;
        public string SelectedRegion { get; set; } = string.Empty;
        public double AirTemperature { get; set; }
        public double ColdFiveDayTemperature { get; set; }
        public double WindSpeed { get; set; }
        public double Humidity { get; set; }
        public double SnowfallIntensity { get; set; }
        public ClimateZone Zone { get; set; }

        public event EventHandler<ClimateDataChangedEventArgs>? DataChanged;

        /// <summary>
        /// Вызвать событие изменения данных
        /// </summary>
        public void RaiseDataChanged(string propertyName, object? oldValue, object? newValue, bool isValid = true)
        {
            DataChanged?.Invoke(this, new ClimateDataChangedEventArgs
            {
                ChangedProperty = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                IsValid = isValid
            });
        }

        /// <summary>
        /// Создать копию данных
        /// </summary>
        public ClimateData Clone()
        {
            return new ClimateData
            {
                SelectedCity = SelectedCity,
                SelectedRegion = SelectedRegion,
                AirTemperature = AirTemperature,
                ColdFiveDayTemperature = ColdFiveDayTemperature,
                WindSpeed = WindSpeed,
                Humidity = Humidity,
                SnowfallIntensity = SnowfallIntensity,
                Zone = Zone
            };
        }
    }
}