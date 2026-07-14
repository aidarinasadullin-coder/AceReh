namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Информация о городе из климатического справочника СП 131.13330.2025
    /// </summary>
    public class CityInfo
    {
        /// <summary>
        /// Название города
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Регион/субъект РФ
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// Температура холодной пятидневки (обеспеченность 0.92), °C
        /// Используется как расчётная температура наружного воздуха
        /// </summary>
        public double T5Days092 { get; set; }

        /// <summary>
        /// Средняя скорость ветра за период со средней суточной температурой ≤8°C (отопительный период), м/с
        /// </summary>
        public double WindAvgTempLe8 { get; set; }

        /// <summary>
        /// Влажность в 15 часов холодного периода, %
        /// </summary>
        public double Humidity15hCold { get; set; }

        /// <summary>
        /// Температура холодных суток (обеспеченность 0.98), °C
        /// </summary>
        public double TColdDays098 { get; set; }

        /// <summary>
        /// Абсолютный минимум температуры, °C
        /// </summary>
        public double TAbsMin { get; set; }

        /// <summary>
        /// Продолжительность периода со средней суточной температурой ≤0°C, дней
        /// </summary>
        public int Period_0_Days { get; set; }

        /// <summary>
        /// Продолжительность периода со средней суточной температурой ≤8°C (отопительный период), дней
        /// </summary>
        public int Period_8_Days { get; set; }

        /// <summary>
        /// Продолжительность периода со средней суточной температурой ≤10°C, дней
        /// </summary>
        public int Period_10_Days { get; set; }

        /// <summary>
        /// Отображаемое имя: "Город (Регион)"
        /// </summary>
        public string DisplayName => $"{Name} ({Region})";

        /// <summary>
        /// Переопределение ToString для отображения в UI
        /// </summary>
        public override string ToString() => DisplayName;
    }
}