using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Repositories
{
    /// <summary>
    /// Интерфейс репозитория климатических данных
    /// </summary>
    public interface IClimateDataRepository
    {
        /// <summary>
        /// Загрузить все города из справочника
        /// </summary>
        Task<IEnumerable<CityInfo>> LoadCitiesAsync();

        /// <summary>
        /// Получить все города (из кэша)
        /// </summary>
        IEnumerable<CityInfo> GetAllCities();
    }
}