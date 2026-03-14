using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Repositories
{
    /// <summary>
    /// Репозиторий для загрузки климатических данных из JSON
    /// </summary>
    public class ClimateDataRepository : IClimateDataRepository
    {
        private readonly string _dataPath;
        private List<CityInfo>? _cities;
        private readonly object _lockObject = new();

        /// <summary>
        /// Создать репозиторий
        /// </summary>
        /// <param name="dataPath">Путь к файлу climate_db.json</param>
        public ClimateDataRepository(string dataPath = "data/climate_db.json")
        {
            _dataPath = dataPath;
        }

        /// <summary>
        /// Загрузить все города из справочника
        /// </summary>
        public async Task<IEnumerable<CityInfo>> LoadCitiesAsync()
        {
            if (_cities != null)
                return _cities;

            lock (_lockObject)
            {
                if (_cities != null)
                    return _cities;
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(_dataPath);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };

                var climateData = JsonSerializer.Deserialize<ClimateDbModel>(jsonContent, options);

                _cities = climateData?.Cities?.Select(MapToCityInfo).ToList() ?? new List<CityInfo>();

                return _cities;
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException($"Файл климатических данных не найден: {_dataPath}");
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Ошибка десериализации файла климатических данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить город по названию
        /// </summary>
        public async Task<CityInfo?> GetCityByNameAsync(string name)
        {
            var cities = await LoadCitiesAsync();
            return cities.FirstOrDefault(c => 
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Получить все города (из кэша)
        /// </summary>
        public IEnumerable<CityInfo> GetAllCities()
        {
            if (_cities == null)
            {
                throw new InvalidOperationException("Данные не загружены. Сначала вызовите LoadCitiesAsync().");
            }
            return _cities.AsEnumerable();
        }

        /// <summary>
        /// Маппинг из JSON модели в CityInfo
        /// </summary>
        private static CityInfo MapToCityInfo(CityJsonModel jsonModel)
        {
            return new CityInfo
            {
                Name = jsonModel.City ?? string.Empty,
                Region = jsonModel.Region ?? string.Empty,
                T5Days092 = jsonModel.T_5days_092,
                WindMaxJan = jsonModel.Wind_Max_Jan ?? 0,
                Humidity15hCold = jsonModel.Humidity_15h_Cold ?? 0,
                TColdDays098 = jsonModel.T_Cold_Days_098 ?? 0,
                TAbsMin = jsonModel.T_Abs_Min ?? 0
            };
        }

        #region JSON Models

        /// <summary>
        /// Модель корневого объекта JSON
        /// </summary>
        private class ClimateDbModel
        {
            [JsonPropertyName("meta")]
            public ClimateMeta? Meta { get; set; }

            [JsonPropertyName("cities")]
            public List<CityJsonModel>? Cities { get; set; }
        }

        /// <summary>
        /// Модель метаданных
        /// </summary>
        private class ClimateMeta
        {
            [JsonPropertyName("date")]
            public string? Date { get; set; }

            [JsonPropertyName("total_cities")]
            public int TotalCities { get; set; }

            [JsonPropertyName("source")]
            public string? Source { get; set; }

            [JsonPropertyName("version")]
            public string? Version { get; set; }
        }

        /// <summary>
        /// Модель города в JSON
        /// </summary>
        private class CityJsonModel
        {
            [JsonPropertyName("city")]
            public string? City { get; set; }

            [JsonPropertyName("region")]
            public string? Region { get; set; }

            [JsonPropertyName("t_5days_092")]
            public double T_5days_092 { get; set; }

            [JsonPropertyName("wind_max_jan")]
            public double? Wind_Max_Jan { get; set; }

            [JsonPropertyName("humidity_15h_cold")]
            public double? Humidity_15h_Cold { get; set; }

            [JsonPropertyName("t_cold_days_098")]
            public double? T_Cold_Days_098 { get; set; }

            [JsonPropertyName("t_abs_min")]
            public double? T_Abs_Min { get; set; }

            // Дополнительные поля (не используются в текущей версии)
            [JsonPropertyName("t_cold_days_092")]
            public double? T_Cold_Days_092 { get; set; }

            [JsonPropertyName("t_094")]
            public double? T_094 { get; set; }

            [JsonPropertyName("t_5days_098")]
            public double? T_5days_098 { get; set; }

            [JsonPropertyName("period_0_temp")]
            public double? Period_0_Temp { get; set; }

            [JsonPropertyName("period_0_days")]
            public int? Period_0_Days { get; set; }

            [JsonPropertyName("period_8_temp")]
            public double? Period_8_Temp { get; set; }

            [JsonPropertyName("period_8_days")]
            public int? Period_8_Days { get; set; }

            [JsonPropertyName("period_10_temp")]
            public double? Period_10_Temp { get; set; }

            [JsonPropertyName("period_10_days")]
            public int? Period_10_Days { get; set; }

            [JsonPropertyName("wind_avg_t_le_8")]
            public double? Wind_Avg_T_Le_8 { get; set; }

            [JsonPropertyName("precip_nov_mar")]
            public double? Precip_Nov_Mar { get; set; }

            [JsonPropertyName("wind_dir_winter")]
            public string? Wind_Dir_Winter { get; set; }

            [JsonPropertyName("humidity_cold_month")]
            public double? Humidity_Cold_Month { get; set; }
        }

        #endregion
    }
}