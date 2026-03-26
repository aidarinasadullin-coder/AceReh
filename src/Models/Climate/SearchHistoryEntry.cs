namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Запись истории поиска городов
    /// </summary>
    public class SearchHistoryEntry
    {
        /// <summary>
        /// Идентификатор записи (SQLite auto-increment)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор города (CityInfo.Name)
        /// </summary>
        public string CityId { get; set; } = string.Empty;

        /// <summary>
        /// Дата и время последнего использования (UTC)
        /// </summary>
        public DateTime LastUsed { get; set; }

        /// <summary>
        /// Количество использований
        /// </summary>
        public int UseCount { get; set; }

        /// <summary>
        /// Навигационное свойство к городу (заполняется при запросе)
        /// </summary>
        public CityInfo? City { get; set; }
    }
}