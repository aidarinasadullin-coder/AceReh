namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Результат поиска города с подсветкой совпадений
    /// </summary>
    public class CityMatchResult
    {
        /// <summary>
        /// Исходные данные города
        /// </summary>
        public CityInfo City { get; set; } = null!;

        /// <summary>
        /// Название города с подсветкой совпадения
        /// Формат: "Мос**ква**" или "Моск**ва**" (подсвеченная часть между **)
        /// </summary>
        public string HighlightedName { get; set; } = string.Empty;

        /// <summary>
        /// Регион с подсветкой совпадения
        /// Формат: "Московская **область**" или без подсветки
        /// </summary>
        public string HighlightedRegion { get; set; } = string.Empty;

        /// <summary>
        /// Тип совпадения (StartsWith, Contains, Region)
        /// </summary>
        public MatchType MatchType { get; set; }

        /// <summary>
        /// Отображение температуры: "t = -28°C"
        /// </summary>
        public string TemperatureDisplay => $"t = {City.T5Days092:F0}°C";

        /// <summary>
        /// Отображение климатической зоны: "Зона M15"
        /// </summary>
        public string ZoneDisplay { get; set; } = string.Empty;

        /// <summary>
        /// Индекс совпадения в названии (для подсветки)
        /// </summary>
        public int MatchIndex { get; set; }

        /// <summary>
        /// Длина совпадения (для подсветки)
        /// </summary>
        public int MatchLength { get; set; }
    }
}