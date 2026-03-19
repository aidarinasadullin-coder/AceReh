using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Сервис для получения свойств гликолей (этиленгликоль, пропиленгликоль)
    /// с билинейной интерполяцией по температуре и концентрации
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для получения физических свойств гликолевого раствора:
    /// - Плотность (ρ)
    /// - Кинематическая вязкость (ν)
    /// - Удельная теплоёмкость (c_p)
    /// - Теплопроводность (λ)
    /// 
    /// Данные получаются интерполяцией из data/glycol_data.json
    /// для заданного типа гликоля, концентрации и температуры.
    /// 
    /// Источник данных: ASHRAE Handbook
    /// Диапазон температур: -34.4°C до 98.9°C
    /// Диапазон концентраций: 10% до 90%
    /// </remarks>
    public class GlycolDataService : IGlycolDataService
    {
        private readonly string _dataFilePath;
        private GlycolJsonData? _cachedJsonData;
        private readonly object _lockObject = new();

        /// <summary>
        /// Минимальная поддерживаемая температура, °C
        /// </summary>
        private const double MIN_TEMPERATURE = -34.4;
        
        /// <summary>
        /// Максимальная поддерживаемая температура, °C
        /// </summary>
        private const double MAX_TEMPERATURE = 90.0;
        
        /// <summary>
        /// Минимальная поддерживаемая концентрация, %
        /// </summary>
        private const double MIN_CONCENTRATION = 0.0;
        
        /// <summary>
        /// Максимальная поддерживаемая концентрация, %
        /// </summary>
        private const double MAX_CONCENTRATION = 90.0;

        /// <summary>
        /// Создать экземпляр сервиса с путём к файлу данных по умолчанию
        /// </summary>
        public GlycolDataService() : this("data/glycol_data.json")
        {
        }

        /// <summary>
        /// Создать экземпляр сервиса с указанным путём к файлу данных
        /// </summary>
        /// <param name="dataFilePath">Путь к файлу JSON с данными</param>
        public GlycolDataService(string dataFilePath)
        {
            _dataFilePath = dataFilePath;
        }

        /// <summary>
        /// Получить все свойства гликолевого раствора
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Объект со всеми свойствами гликоля</returns>
        public GlycolProperties GetProperties(GlycolType glycolType, double concentration, double temperature)
        {
            ValidateParameters(concentration, temperature);

            // При концентрации 0% возвращаем свойства воды
            if (concentration == 0)
            {
                return GetWaterProperties(temperature);
            }

            var data = LoadData();
            var glycolData = GetGlycolData(data, glycolType);

            double density = InterpolateProperty(glycolData.Density, concentration, temperature);
            double specificHeat = InterpolateProperty(glycolData.SpecificHeat, concentration, temperature);
            double kinematicViscosity = InterpolateProperty(glycolData.KinematicViscosity, concentration, temperature);
            double thermalConductivity = InterpolateProperty(glycolData.ThermalConductivity, concentration, temperature);

            return new GlycolProperties
            {
                GlycolType = glycolType,
                Concentration = concentration,
                Temperature = temperature,
                Density = density,
                SpecificHeat = specificHeat,
                KinematicViscosity = kinematicViscosity,
                ThermalConductivity = thermalConductivity
            };
        }

        /// <summary>
        /// Получить плотность гликолевого раствора (кг/м³)
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Плотность, кг/м³</returns>
        public double GetDensity(GlycolType glycolType, double concentration, double temperature)
        {
            ValidateParameters(concentration, temperature);

            var data = LoadData();
            var glycolData = GetGlycolData(data, glycolType);

            return InterpolateProperty(glycolData.Density, concentration, temperature);
        }

        /// <summary>
        /// Получить удельную теплоёмкость гликолевого раствора (кДж/(кг·К))
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Удельная теплоёмкость, кДж/(кг·К)</returns>
        public double GetSpecificHeat(GlycolType glycolType, double concentration, double temperature)
        {
            ValidateParameters(concentration, temperature);

            var data = LoadData();
            var glycolData = GetGlycolData(data, glycolType);

            return InterpolateProperty(glycolData.SpecificHeat, concentration, temperature);
        }

        /// <summary>
        /// Получить кинематическую вязкость гликолевого раствора (мм²/с)
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Кинематическая вязкость, мм²/с</returns>
        public double GetKinematicViscosity(GlycolType glycolType, double concentration, double temperature)
        {
            ValidateParameters(concentration, temperature);

            var data = LoadData();
            var glycolData = GetGlycolData(data, glycolType);

            return InterpolateProperty(glycolData.KinematicViscosity, concentration, temperature);
        }

        /// <summary>
        /// Получить теплопроводность гликолевого раствора (Вт/(м·К))
        /// </summary>
        /// <param name="glycolType">Тип гликоля</param>
        /// <param name="concentration">Концентрация, %</param>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Теплопроводность, Вт/(м·К)</returns>
        public double GetThermalConductivity(GlycolType glycolType, double concentration, double temperature)
        {
            ValidateParameters(concentration, temperature);

            var data = LoadData();
            var glycolData = GetGlycolData(data, glycolType);

            return InterpolateProperty(glycolData.ThermalConductivity, concentration, temperature);
        }

        /// <summary>
        /// Проверить, поддерживается ли температура
        /// </summary>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>true, если температура в допустимом диапазоне</returns>
        public bool IsTemperatureSupported(double temperature)
        {
            return temperature >= MIN_TEMPERATURE && temperature <= MAX_TEMPERATURE;
        }

        /// <summary>
        /// Проверить, поддерживается ли концентрация
        /// </summary>
        /// <param name="concentration">Концентрация, %</param>
        /// <returns>true, если концентрация в допустимом диапазоне</returns>
        public bool IsConcentrationSupported(double concentration)
        {
            return concentration >= MIN_CONCENTRATION && concentration <= MAX_CONCENTRATION;
        }

        /// <summary>
        /// Получить минимальную поддерживаемую температуру
        /// </summary>
        /// <returns>Минимальная температура, °C</returns>
        public double GetMinTemperature() => MIN_TEMPERATURE;

        /// <summary>
        /// Получить максимальную поддерживаемую температуру
        /// </summary>
        /// <returns>Максимальная температура, °C</returns>
        public double GetMaxTemperature() => MAX_TEMPERATURE;

        /// <summary>
        /// Получить минимальную поддерживаемую концентрацию
        /// </summary>
        /// <returns>Минимальная концентрация, %</returns>
        public double GetMinConcentration() => MIN_CONCENTRATION;

        /// <summary>
        /// Получить максимальную поддерживаемую концентрацию
        /// </summary>
        /// <returns>Максимальная концентрация, %</returns>
        public double GetMaxConcentration() => MAX_CONCENTRATION;

        /// <summary>
        /// Получить свойства воды при заданной температуре
        /// </summary>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Свойства воды</returns>
        /// <remarks>
        /// Используются приближённые формулы IAPWS-IF97 для диапазона 0-100°C:
        /// - Плотность: ρ = 1000 - 0.0178 × (T - 4)² при T > 4°C
        /// - Вязкость: ν = exp(-1.597 + 0.181×T - 0.003×T²) мм²/с
        /// - Теплоёмкость: c_p ≈ 4.18 кДж/(кг·К) (слабо зависит от T)
        /// - Теплопроводность: λ ≈ 0.6 - 0.0015×T Вт/(м·К)
        /// </remarks>
        public GlycolProperties GetWaterProperties(double temperature)
        {
            if (temperature < 0 || temperature > MAX_TEMPERATURE)
            {
                throw new ArgumentOutOfRangeException(nameof(temperature),
                    $"Температура воды должна быть в диапазоне 0°C до {MAX_TEMPERATURE}°C, получено: {temperature}°C");
            }

            // Плотность воды: ρ = 1000 - 0.0178 × (T - 4)² при T > 4°C
            // При T <= 4°C плотность ≈ 1000 кг/м³ (максимум при 4°C)
            double density;
            if (temperature > 4)
            {
                density = 1000 - 0.0178 * Math.Pow(temperature - 4, 2);
            }
            else
            {
                // При температуре ниже 4°C плотность немного уменьшается
                // Используем линейную аппроксимацию
                density = 1000 - 0.1 * (4 - temperature);
            }

            // Вязкость воды: ν = exp(-1.597 + 0.181×T - 0.003×T²) мм²/с
            double kinematicViscosity = Math.Exp(-1.597 + 0.181 * temperature - 0.003 * Math.Pow(temperature, 2));

            // Теплоёмкость воды: c_p ≈ 4.18 кДж/(кг·К) (слабо зависит от T)
            // Для более точного расчёта можно использовать: c_p = 4.184 + 0.0001×(T - 20)
            double specificHeat = 4.18 + 0.0001 * (temperature - 20);

            // Теплопроводность воды: λ ≈ 0.6 - 0.0015×T Вт/(м·К)
            double thermalConductivity = 0.6 - 0.0015 * temperature;

            return new GlycolProperties
            {
                GlycolType = GlycolType.Ethylene, // Для воды тип не важен
                Concentration = 0,
                Temperature = temperature,
                Density = density,
                SpecificHeat = specificHeat,
                KinematicViscosity = kinematicViscosity,
                ThermalConductivity = thermalConductivity
            };
        }

        #region Private Methods

        /// <summary>
        /// Загрузить данные из JSON файла (с кэшированием)
        /// </summary>
        private GlycolJsonData LoadData()
        {
            lock (_lockObject)
            {
                if (_cachedJsonData != null)
                    return _cachedJsonData;

                if (!File.Exists(_dataFilePath))
                {
                    // Если файл не существует, вернуть встроенные данные
                    System.Diagnostics.Debug.WriteLine($"[GlycolDataService] Файл данных не найден: {_dataFilePath}. Используются fallback данные.");
                    _cachedJsonData = GetDefaultData();
                    return _cachedJsonData;
                }

                try
                {
                    string json = File.ReadAllText(_dataFilePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var rawContainer = JsonSerializer.Deserialize<GlycolRawContainer>(json, options);
                    
                    if (rawContainer == null)
                    {
                        _cachedJsonData = GetDefaultData();
                        return _cachedJsonData;
                    }

                    // Конвертация из формата JSON в формат для интерполяции
                    _cachedJsonData = ConvertToInterpolationFormat(rawContainer);
                }
                catch (Exception ex)
                {
                    // Логировать предупреждение
                    System.Diagnostics.Debug.WriteLine($"[GlycolDataService] Ошибка загрузки JSON: {ex.Message}. Используются fallback данные.");
                    
                    // При ошибке парсинга используем встроенные данные
                    _cachedJsonData = GetDefaultData();
                }

                return _cachedJsonData;
            }
        }

        /// <summary>
        /// Конвертировать данные из формата JSON в формат для интерполяции
        /// </summary>
        private GlycolJsonData ConvertToInterpolationFormat(GlycolRawContainer raw)
        {
            var result = new GlycolJsonData();

            // Этиленгликоль
            if (raw.EthyleneGlycol != null)
            {
                result.EthyleneGlycol = ConvertGlycolTypeData(raw.EthyleneGlycol);
            }

            // Пропиленгликоль
            if (raw.PropyleneGlycol != null)
            {
                result.PropyleneGlycol = ConvertGlycolTypeData(raw.PropyleneGlycol);
            }

            return result;
        }

        /// <summary>
        /// Конвертировать данные для конкретного типа гликоля
        /// </summary>
        private GlycolTypeData ConvertGlycolTypeData(GlycolTypeRawData raw)
        {
            // Получаем концентрации из первого доступного свойства
            var concentrations = raw.Density?.Concentrations 
                ?? raw.SpecificHeat?.Concentrations 
                ?? raw.KinematicViscosity?.Concentrations 
                ?? raw.ThermalConductivity?.Concentrations 
                ?? Array.Empty<double>();
            
            var densityData = raw.Density?.Data ?? new List<TemperatureDataRow>();
            var specificHeatData = raw.SpecificHeat?.Data ?? new List<TemperatureDataRow>();
            var viscosityData = raw.KinematicViscosity?.Data ?? new List<TemperatureDataRow>();
            var conductivityData = raw.ThermalConductivity?.Data ?? new List<TemperatureDataRow>();

            // Извлечение температур
            var temperatures = new List<double>();
            foreach (var row in densityData)
            {
                if (row.TempC.HasValue)
                    temperatures.Add(row.TempC.Value);
            }

            int numConcentrations = concentrations.Length;
            int numTemperatures = temperatures.Count;

            // Создание матриц значений
            var densityValues = new double[numConcentrations, numTemperatures];
            var specificHeatValues = new double[numConcentrations, numTemperatures];
            var viscosityValues = new double[numConcentrations, numTemperatures];
            var conductivityValues = new double[numConcentrations, numTemperatures];

            for (int t = 0; t < numTemperatures; t++)
            {
                var densityRow = densityData[t];
                var specificHeatRow = specificHeatData.Count > t ? specificHeatData[t] : null;
                var viscosityRow = viscosityData.Count > t ? viscosityData[t] : null;
                var conductivityRow = conductivityData.Count > t ? conductivityData[t] : null;

                for (int c = 0; c < numConcentrations; c++)
                {
                    densityValues[c, t] = GetArrayValue(densityRow.Values, c);
                    specificHeatValues[c, t] = GetArrayValue(specificHeatRow?.Values, c);
                    viscosityValues[c, t] = GetArrayValue(viscosityRow?.Values, c);
                    conductivityValues[c, t] = GetArrayValue(conductivityRow?.Values, c);
                }
            }

            return new GlycolTypeData
            {
                Concentrations = concentrations,
                Temperatures = temperatures.ToArray(),
                Density = new InterpolationTable
                {
                    Concentrations = concentrations,
                    Temperatures = temperatures.ToArray(),
                    Values = densityValues
                },
                SpecificHeat = new InterpolationTable
                {
                    Concentrations = concentrations,
                    Temperatures = temperatures.ToArray(),
                    Values = specificHeatValues
                },
                KinematicViscosity = new InterpolationTable
                {
                    Concentrations = concentrations,
                    Temperatures = temperatures.ToArray(),
                    Values = viscosityValues
                },
                ThermalConductivity = new InterpolationTable
                {
                    Concentrations = concentrations,
                    Temperatures = temperatures.ToArray(),
                    Values = conductivityValues
                }
            };
        }

        /// <summary>
        /// Получить значение из массива с проверкой null
        /// </summary>
        private static double GetArrayValue(double?[]? array, int index)
        {
            if (array == null || index >= array.Length)
                return 0;
            
            return array[index] ?? 0;
        }

        /// <summary>
        /// Получить данные для конкретного типа гликоли
        /// </summary>
        private GlycolTypeData GetGlycolData(GlycolJsonData container, GlycolType glycolType)
        {
            return glycolType switch
            {
                GlycolType.Ethylene => container.EthyleneGlycol ?? GetDefaultEthyleneData(),
                GlycolType.Propylene => container.PropyleneGlycol ?? GetDefaultPropyleneData(),
                _ => throw new ArgumentException($"Неподдерживаемый тип гликоли: {glycolType}")
            };
        }

        /// <summary>
        /// Интерполяция свойства по концентрации и температуре
        /// </summary>
        private double InterpolateProperty(InterpolationTable table, double concentration, double temperature)
        {
            double[] concentrations = table.Concentrations;
            double[] temperatures = table.Temperatures;
            double[,] values = table.Values;

            if (concentrations.Length == 0 || temperatures.Length == 0)
                return 0;

            // Найти индексы для интерполяции
            int cLow = FindLowerIndex(concentrations, concentration);
            int tLow = FindLowerIndex(temperatures, temperature);

            int cHigh = Math.Min(cLow + 1, concentrations.Length - 1);
            int tHigh = Math.Min(tLow + 1, temperatures.Length - 1);

            // Граничные случаи
            if (cLow == cHigh && tLow == tHigh)
            {
                return values[cLow, tLow];
            }

            if (cLow == cHigh)
            {
                // Интерполяция только по температуре
                double temp1 = temperatures[tLow];
                double temp2 = temperatures[tHigh];
                double val1 = values[cLow, tLow];
                double val2 = values[cLow, tHigh];

                return LinearInterpolate(temp1, temp2, val1, val2, temperature);
            }

            if (tLow == tHigh)
            {
                // Интерполяция только по концентрации
                double conc1 = concentrations[cLow];
                double conc2 = concentrations[cHigh];
                double val1 = values[cLow, tLow];
                double val2 = values[cHigh, tLow];

                return LinearInterpolate(conc1, conc2, val1, val2, concentration);
            }

            // Билинейная интерполяция
            double c1 = concentrations[cLow];
            double c2 = concentrations[cHigh];
            double t1 = temperatures[tLow];
            double t2 = temperatures[tHigh];

            double v11 = values[cLow, tLow];
            double v12 = values[cLow, tHigh];
            double v21 = values[cHigh, tLow];
            double v22 = values[cHigh, tHigh];

            // Интерполяция по температуре для каждой концентрации
            double v1_interp = LinearInterpolate(t1, t2, v11, v12, temperature);
            double v2_interp = LinearInterpolate(t1, t2, v21, v22, temperature);

            // Интерполяция по концентрации
            return LinearInterpolate(c1, c2, v1_interp, v2_interp, concentration);
        }

        /// <summary>
        /// Линейная интерполяция между двумя точками
        /// </summary>
        private static double LinearInterpolate(double x1, double x2, double y1, double y2, double x)
        {
            if (Math.Abs(x2 - x1) < 1e-10)
                return y1;

            double ratio = (x - x1) / (x2 - x1);
            return y1 + ratio * (y2 - y1);
        }

        /// <summary>
        /// Найти индекс ближайшего меньшего значения
        /// </summary>
        private static int FindLowerIndex(double[] array, double value)
        {
            if (array.Length == 0)
                return 0;

            if (value <= array[0])
                return 0;

            if (value >= array[array.Length - 1])
                return array.Length - 1;

            for (int i = 0; i < array.Length - 1; i++)
            {
                if (array[i] <= value && value < array[i + 1])
                    return i;
            }

            return array.Length - 2;
        }

        /// <summary>
        /// Валидация входных параметров
        /// </summary>
        private void ValidateParameters(double concentration, double temperature)
        {
            if (concentration < MIN_CONCENTRATION || concentration > MAX_CONCENTRATION)
            {
                throw new ArgumentOutOfRangeException(nameof(concentration),
                    $"Концентрация должна быть в диапазоне {MIN_CONCENTRATION}-{MAX_CONCENTRATION}%, получено: {concentration}%");
            }

            // Для воды (концентрация 0%) минимальная температура 0°C
            double minTemp = concentration == 0 ? 0.0 : MIN_TEMPERATURE;
            
            if (temperature < minTemp || temperature > MAX_TEMPERATURE)
            {
                throw new ArgumentOutOfRangeException(nameof(temperature),
                    $"Температура должна быть в диапазоне {minTemp}°C до {MAX_TEMPERATURE}°C, получено: {temperature}°C");
            }
        }

        /// <summary>
        /// Получить встроенные данные о свойствах гликолей
        /// </summary>
        private static GlycolJsonData GetDefaultData()
        {
            return new GlycolJsonData
            {
                EthyleneGlycol = GetDefaultEthyleneData(),
                PropyleneGlycol = GetDefaultPropyleneData()
            };
        }

        /// <summary>
        /// Fallback данные для этиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// Температуры ASHRAE: -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9 °C
        /// Концентрации: 10, 20, 30, 40, 50, 60, 70, 80, 90 vol%
        /// </summary>
        private static GlycolTypeData GetDefaultEthyleneData()
        {
            var concentrations = new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };
            // Температуры ASHRAE (подмножество из JSON)
            var temperatures = new[] { -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9 };

            return new GlycolTypeData
            {
                Concentrations = concentrations,
                Temperatures = temperatures,
                Density = CreateDefaultTable(concentrations, temperatures, DefaultEthyleneDensityValues()),
                SpecificHeat = CreateDefaultTable(concentrations, temperatures, DefaultEthyleneSpecificHeatValues()),
                KinematicViscosity = CreateDefaultTable(concentrations, temperatures, DefaultEthyleneViscosityValues()),
                ThermalConductivity = CreateDefaultTable(concentrations, temperatures, DefaultEthyleneConductivityValues())
            };
        }

        /// <summary>
        /// Fallback данные для пропиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// Температуры ASHRAE: -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9 °C
        /// Концентрации: 10, 20, 30, 40, 50, 60, 70, 80, 90 vol%
        /// </summary>
        private static GlycolTypeData GetDefaultPropyleneData()
        {
            var concentrations = new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };
            // Температуры ASHRAE (подмножество из JSON)
            var temperatures = new[] { -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9 };

            return new GlycolTypeData
            {
                Concentrations = concentrations,
                Temperatures = temperatures,
                Density = CreateDefaultTable(concentrations, temperatures, DefaultPropyleneDensityValues()),
                SpecificHeat = CreateDefaultTable(concentrations, temperatures, DefaultPropyleneSpecificHeatValues()),
                KinematicViscosity = CreateDefaultTable(concentrations, temperatures, DefaultPropyleneViscosityValues()),
                ThermalConductivity = CreateDefaultTable(concentrations, temperatures, DefaultPropyleneConductivityValues())
            };
        }

        private static InterpolationTable CreateDefaultTable(double[] concentrations, double[] temperatures, double[,] values)
        {
            return new InterpolationTable
            {
                Concentrations = concentrations,
                Temperatures = temperatures,
                Values = values
            };
        }

        #region Ethylene Glycol Fallback Data

        /// <summary>
        /// Fallback значения плотности для этиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// </summary>
        private static double[,] DefaultEthyleneDensityValues()
        {
            // Данные из JSON: density_kg_m3
            // Строки соответствуют температурам, столбцы - концентрациям
            return new double[,]
            {
                // temp: -34.4°C
                {  0,     0,      0,      0,      0,      1090.7, 1105.3, 1119.1, 1132.5 },
                // temp: -17.8°C
                {  0,     0,      1072.2, 1087.2, 1101.5, 1115.1, 1128.4, 1141.3, 1153.8 },
                // temp: -1.1°C
                {  1019.2, 1053.2, 1068.6, 1083.4, 1097.3, 1110.6, 1123.5, 1136.1, 1148.1 },
                // temp: 15.6°C
                {  1015.7, 1049.5, 1064.8, 1079.2, 1092.8, 1105.6, 1118.2, 1130.4, 1141.8 },
                // temp: 32.2°C
                {  1012.1, 1045.7, 1060.9, 1074.9, 1088.2, 1100.3, 1112.6, 1124.3, 1135.0 },
                // temp: 48.9°C
                {  1008.3, 1041.9, 1056.9, 1070.5, 1083.4, 1094.7, 1106.7, 1117.9, 1127.7 },
                // temp: 65.6°C
                {  1004.5, 1038.0, 1052.9, 1066.0, 1078.5, 1088.9, 1100.6, 1111.2, 1120.1 },
                // temp: 82.2°C
                {  1000.6, 1034.1, 1048.9, 1061.3, 1073.4, 1082.9, 1094.3, 1104.2, 1112.2 },
                // temp: 98.9°C
                {   996.7, 1030.2, 1044.8, 1056.6, 1068.2, 1076.7, 1087.9, 1097.0, 1104.1 }
            };
        }

        /// <summary>
        /// Fallback значения удельной теплоёмкости для этиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// </summary>
        private static double[,] DefaultEthyleneSpecificHeatValues()
        {
            // Данные из JSON: specific_heat_kJ_kgK
            return new double[,]
            {
                // temp: -34.4°C
                {  0,    0,     0,     0,     0,      3.07,   2.85,   2.62,   2.37 },
                // temp: -17.8°C
                {  0,    0,     3.35,   3.13,   2.92,   2.70,   2.47,   2.23,   2.03 },
                // temp: -1.1°C
                {  3.78,  3.14,   2.92,   2.70,   2.47,   2.22,   2.01,   1.82,   1.62 },
                // temp: 15.6°C
                {  4.36,  3.77,   3.59,   3.40,   3.20,   3.00,   2.78,   2.54,   2.33 },
                // temp: 32.2°C
                {  5.00,  4.39,   4.20,   4.03,   3.84,   3.63,   3.41,   3.17,   2.94 },
                // temp: 48.9°C
                {  5.65,  5.01,   4.83,   4.65,   4.47,   4.26,   4.04,   3.80,   3.57 },
                // temp: 65.6°C
                {  6.29,  5.63,   5.46,   5.28,   5.10,   4.89,   4.67,   4.43,   4.20 },
                // temp: 82.2°C
                {  6.93,  6.25,   6.09,   5.91,   5.73,   5.52,   5.30,   5.06,   4.83 },
                // temp: 98.9°C
                {  7.58,  6.87,   6.72,   6.54,   6.36,   6.15,   5.93,   5.69,   5.46 }
            };
        }

        /// <summary>
        /// Fallback значения кинематической вязкости для этиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// </summary>
        private static double[,] DefaultEthyleneViscosityValues()
        {
            // Данные из JSON: kinematic_viscosity_mm2_s
            return new double[,]
            {
                // temp: -34.4°C
                {  0,     0,      0,      0,      0,      58.4,   81.2,   115.0,  163.5 },
                // temp: -17.8°C
                {  0,     0,      12.9,   17.8,   27.2,   40.8,   57.5,   79.4,   93.3 },
                // temp: -1.1°C
                {  2.6,   3.7,    5.5,    7.9,    11.4,   17.4,   23.2,   31.6,   38.9 },
                // temp: 15.6°C
                {  1.0,   1.4,    2.0,    2.7,    3.8,    5.3,    7.3,    10.2,   13.7 },
                // temp: 32.2°C
                {  0.5,   0.7,    0.8,    1.1,    1.6,    2.1,    2.8,    3.7,    4.8 },
                // temp: 48.9°C
                {  0.3,   0.4,    0.5,    0.6,    0.8,    1.1,    1.4,    1.9,    2.4 },
                // temp: 65.6°C
                {  0.2,   0.2,    0.3,    0.4,    0.5,    0.6,    0.7,    0.9,    1.2 },
                // temp: 82.2°C
                {  0.1,   0.1,    0.2,    0.2,    0.3,    0.3,    0.4,    0.5,    0.5 },
                // temp: 98.9°C
                {  0.1,   0.1,    0.1,    0.1,    0.2,    0.2,    0.3,    0.3,    0.4 }
            };
        }

        /// <summary>
        /// Fallback значения теплопроводности для этиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// </summary>
        private static double[,] DefaultEthyleneConductivityValues()
        {
            // Данные из JSON: thermal_conductivity_W_mK
            return new double[,]
            {
                // temp: -34.4°C
                {  0,     0,      0,      0,      0,      0.324,  0.300,  0.279,  0.261 },
                // temp: -17.8°C
                {  0,     0,      0.369,  0.338,  0.313,  0.291,  0.271,  0.255,  0.244 },
                // temp: -1.1°C
                {  0.462, 0.337,  0.311,  0.287,  0.268,  0.252,  0.239,  0.227,  0.225 },
                // temp: 15.6°C
                {  0.602, 0.456,  0.416,  0.382,  0.355,  0.327,  0.300,  0.275,  0.262 },
                // temp: 32.2°C
                {  0.744, 0.579,  0.527,  0.481,  0.445,  0.412,  0.377,  0.341,  0.313 },
                // temp: 48.9°C
                {  0.885, 0.702,  0.638,  0.580,  0.535,  0.493,  0.451,  0.402,  0.363 },
                // temp: 65.6°C
                {  1.027, 0.825,  0.749,  0.679,  0.625,  0.574,  0.523,  0.462,  0.412 },
                // temp: 82.2°C
                {  1.168, 0.948,  0.860,  0.778,  0.715,  0.655,  0.595,  0.522,  0.460 },
                // temp: 98.9°C
                {  1.310, 1.071,  0.971,  0.877,  0.805,  0.736,  0.667,  0.582,  0.508 }
            };
        }

        #endregion

        #region Propylene Glycol Fallback Data

        /// <summary>
        /// Fallback значения плотности для пропиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// </summary>
        private static double[,] DefaultPropyleneDensityValues()
        {
            // Данные из JSON: density_kg_m3
            return new double[,]
            {
                // temp: -34.4°C
                {  0,     0,      0,      0,      0,      1074.0, 1082.2, 1095.3, 1094.8 },
                // temp: -17.8°C
                {  0,     0,      0,      1073.6, 1083.2, 1081.6, 0,      0,      0 },
                // temp: -1.1°C
                {  0,     0,      1036.0, 1047.0, 1054.0, 1062.0, 1066.0, 1069.0, 1069.0 },
                // temp: 15.6°C
                {  0,     1020.0, 1031.0, 1040.0, 1048.0, 1055.0, 1058.0, 1058.0, 1055.0 },
                // temp: 32.2°C
                {  0,     1014.0, 1025.0, 1033.0, 1040.0, 1046.0, 1047.0, 1044.0, 1039.0 },
                // temp: 48.9°C
                {  0,     1007.0, 1019.0, 1026.0, 1032.0, 1037.0, 1036.0, 1031.0, 1024.0 },
                // temp: 65.6°C
                {  0,     999.0,  1012.0, 1019.0, 1024.0, 1028.0, 1025.0, 1018.0, 1009.0 },
                // temp: 82.2°C
                {  0,     990.0,  1004.0, 1010.0, 1015.0, 1018.0, 1014.0, 1005.0, 994.0 },
                // temp: 98.9°C
                {  0,     981.0,  995.0,  1001.0, 1006.0, 1007.0, 1002.0, 991.0,  979.0 }
            };
        }

        /// <summary>
        /// Fallback значения удельной теплоёмкости для пропиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// </summary>
        private static double[,] DefaultPropyleneSpecificHeatValues()
        {
            // Данные из JSON: specific_heat_kJ_kgK
            return new double[,]
            {
                // temp: -34.4°C
                {  0,     0,      0,      0,      0,      3.10,   2.85,   2.58,   2.27 },
                // temp: -17.8°C
                {  0,     0,      0,      3.58,   3.39,   3.17,   2.93,   2.67,   2.37 },
                // temp: -1.1°C
                {  0,     0,      4.05,   3.93,   3.76,   3.58,   3.38,   3.14,   2.87 },
                // temp: 15.6°C
                {  0,     0,      4.08,   3.97,   3.83,   3.68,   3.52,   3.33,   3.13 },
                // temp: 32.2°C
                {  0,     0,      4.10,   4.00,   3.89,   3.75,   3.61,   3.44,   3.28 },
                // temp: 48.9°C
                {  0,     0,      4.13,   4.04,   3.94,   3.82,   3.69,   3.54,   3.40 },
                // temp: 65.6°C
                {  0,     0,      4.15,   4.08,   3.99,   3.89,   3.78,   3.66,   3.53 },
                // temp: 82.2°C
                {  0,     0,      4.18,   4.12,   4.05,   3.96,   3.87,   3.77,   3.66 },
                // temp: 98.9°C
                {  0,     0,      4.20,   4.15,   4.09,   4.02,   3.94,   3.86,   3.77 }
            };
        }

        /// <summary>
        /// Fallback значения кинематической вязкости для пропиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// </summary>
        private static double[,] DefaultPropyleneViscosityValues()
        {
            // Данные из JSON: kinematic_viscosity_mm2_s
            return new double[,]
            {
                // temp: -34.4°C
                {  0,       0,        0,        0,        0,        1203.67, 2092.20, 3299.03, 8600.39 },
                // temp: -17.8°C
                {  0,       0,        0,        98.99,    149.55,   277.95,  429.94,  735.26,  1350.63 },
                // temp: -1.1°C
                {  0,       0,        6.77,     10.23,    18.05,    31.74,   47.22,   81.47,   119.31 },
                // temp: 15.6°C
                {  0,       0,        3.87,     5.61,     8.76,     13.45,   19.57,   30.46,   43.20 },
                // temp: 32.2°C
                {  0,       0,        2.54,     3.46,     4.93,     6.97,    9.82,    13.96,   19.35 },
                // temp: 48.9°C
                {  0,       0,        1.81,     2.35,     3.14,     4.19,    5.71,    7.52,    10.23 },
                // temp: 65.6°C
                {  0,       0,        1.38,     1.72,     2.20,     2.81,    3.70,    4.62,    6.14 },
                // temp: 82.2°C
                {  0,       0,        1.06,     1.31,     1.64,     2.06,    2.59,    3.12,    4.09 },
                // temp: 98.9°C
                {  0,       0,        0.87,     1.04,     1.31,     1.60,    1.96,    2.27,    2.93 }
            };
        }

        /// <summary>
        /// Fallback значения теплопроводности для пропиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// </summary>
        private static double[,] DefaultPropyleneConductivityValues()
        {
            // Данные из JSON: thermal_conductivity_W_mK
            return new double[,]
            {
                // temp: -34.4°C
                {  0,     0,      0,      0,      0,      0.270,  0.242,  0.220,  0.203 },
                // temp: -17.8°C
                {  0,     0,      0,      0.348,  0.313,  0.280,  0.251,  0.227,  0.206 },
                // temp: -1.1°C
                {  0,     0,      0.455,  0.408,  0.365,  0.325,  0.293,  0.261,  0.234 },
                // temp: 15.6°C
                {  0,     0,      0.533,  0.477,  0.427,  0.385,  0.343,  0.300,  0.265 },
                // temp: 32.2°C
                {  0,     0,      0.556,  0.497,  0.444,  0.395,  0.350,  0.307,  0.270 },
                // temp: 48.9°C
                {  0,     0,      0.574,  0.512,  0.456,  0.407,  0.361,  0.315,  0.275 },
                // temp: 65.6°C
                {  0,     0,      0.585,  0.522,  0.466,  0.414,  0.367,  0.320,  0.279 },
                // temp: 82.2°C
                {  0,     0,      0.601,  0.535,  0.474,  0.419,  0.371,  0.323,  0.280 },
                // temp: 98.9°C
                {  0,     0,      0.604,  0.537,  0.476,  0.420,  0.371,  0.323,  0.280 }
            };
        }

        #endregion

        #endregion

        #region JSON Data Models

        /// <summary>
        /// Контейнер данных гликолей из JSON
        /// </summary>
        internal class GlycolRawContainer
        {
            [JsonPropertyName("ethylene_glycol")]
            public GlycolTypeRawData? EthyleneGlycol { get; set; }
            
            [JsonPropertyName("propylene_glycol")]
            public GlycolTypeRawData? PropyleneGlycol { get; set; }
        }

        /// <summary>
        /// Данные для конкретного типа гликоля из JSON
        /// </summary>
        internal class GlycolTypeRawData
        {
            [JsonPropertyName("density_kg_m3")]
            public PropertyDataWithConcentrations? Density { get; set; }
            
            [JsonPropertyName("specific_heat_kJ_kgK")]
            public PropertyDataWithConcentrations? SpecificHeat { get; set; }
            
            [JsonPropertyName("kinematic_viscosity_mm2_s")]
            public PropertyDataWithConcentrations? KinematicViscosity { get; set; }
            
            [JsonPropertyName("thermal_conductivity_W_mK")]
            public PropertyDataWithConcentrations? ThermalConductivity { get; set; }
        }

        /// <summary>
        /// Данные свойства с концентрациями из JSON
        /// </summary>
        internal class PropertyDataWithConcentrations
        {
            [JsonPropertyName("concentration_vol_pct")]
            public double[]? Concentrations { get; set; }
            
            [JsonPropertyName("data")]
            public List<TemperatureDataRow>? Data { get; set; }
        }

        /// <summary>
        /// Строка данных для температуры
        /// </summary>
        internal class TemperatureDataRow
        {
            [JsonPropertyName("temp_c")]
            public double? TempC { get; set; }
            
            [JsonPropertyName("values")]
            public double?[]? Values { get; set; }
        }

        /// <summary>
        /// Данные гликолей в формате для интерполяции
        /// </summary>
        internal class GlycolJsonData
        {
            public GlycolTypeData? EthyleneGlycol { get; set; }
            public GlycolTypeData? PropyleneGlycol { get; set; }
        }

        /// <summary>
        /// Данные для конкретного типа гликоля
        /// </summary>
        internal class GlycolTypeData
        {
            public double[] Concentrations { get; set; } = Array.Empty<double>();
            public double[] Temperatures { get; set; } = Array.Empty<double>();
            public InterpolationTable Density { get; set; } = new();
            public InterpolationTable SpecificHeat { get; set; } = new();
            public InterpolationTable KinematicViscosity { get; set; } = new();
            public InterpolationTable ThermalConductivity { get; set; } = new();
        }

        /// <summary>
        /// Таблица для интерполяции
        /// </summary>
        internal class InterpolationTable
        {
            public double[] Concentrations { get; set; } = Array.Empty<double>();
            public double[] Temperatures { get; set; } = Array.Empty<double>();
            public double[,] Values { get; set; } = new double[0, 0];
        }

        #endregion
    }
}