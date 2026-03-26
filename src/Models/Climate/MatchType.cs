namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Тип совпадения при поиске города
    /// </summary>
    public enum MatchType
    {
        /// <summary>
        /// Совпадение в начале названия (StartsWith)
        /// Приоритет: 1 (наивысший)
        /// </summary>
        StartsWith = 0,

        /// <summary>
        /// Совпадение в названии (Contains)
        /// Приоритет: 2
        /// </summary>
        Contains = 1,

        /// <summary>
        /// Совпадение в регионе
        /// Приоритет: 3 (низший)
        /// </summary>
        Region = 2
    }
}