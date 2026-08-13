using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SnowMeltingCalculator.Tests")]

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
        /// Интенсивность снегопада, мм/ч (водяной эквивалент)
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
        public string SelectedCity { get; internal set; } = string.Empty;
        public string SelectedRegion { get; internal set; } = string.Empty;
        public double AirTemperature { get; internal set; }
        public double ColdFiveDayTemperature { get; internal set; }
        public double WindSpeed { get; internal set; }
        public double Humidity { get; internal set; }
        public double SnowfallIntensity { get; internal set; }
        public ClimateZone Zone { get; internal set; }

        public event EventHandler<ClimateDataChangedEventArgs>? DataChanged;

        /// <summary>
        /// Вызвать событие изменения данных. Остаётся public для совместимости;
        /// в production должен вызываться только через утверждённый projection updater.
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
        /// Утверждённый projection updater: обновляет совместимую проекцию из источника
        /// и один раз поднимает <see cref="DataChanged"/>. Не является вторым canonical owner.
        /// </summary>
        /// <param name="source">Источник климатических значений (read-only DTO/projection).</param>
        /// <param name="isValid">Признак валидности для аргументов события.</param>
        internal void ApplyProjection(IClimateData source, bool isValid = true)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            SelectedCity = source.SelectedCity;
            SelectedRegion = source.SelectedRegion;
            AirTemperature = source.AirTemperature;
            ColdFiveDayTemperature = source.ColdFiveDayTemperature;
            WindSpeed = source.WindSpeed;
            Humidity = source.Humidity;
            SnowfallIntensity = source.SnowfallIntensity;
            Zone = source.Zone;

            RaiseDataChanged("Sync", null, null, isValid);
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