using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
        private const double MAX_TEMPERATURE = 121.1;
        
        /// <summary>
        /// Минимальная поддерживаемая концентрация, %
        /// </summary>
        private const double MIN_CONCENTRATION = 10.0;
        
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
                catch (Exception)
                {
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
            var concentrations = raw.Concentrations ?? Array.Empty<double>();
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

            if (temperature < MIN_TEMPERATURE || temperature > MAX_TEMPERATURE)
            {
                throw new ArgumentOutOfRangeException(nameof(temperature),
                    $"Температура должна быть в диапазоне {MIN_TEMPERATURE}°C до {MAX_TEMPERATURE}°C, получено: {temperature}°C");
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

        private static GlycolTypeData GetDefaultEthyleneData()
        {
            var concentrations = new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };
            var temperatures = new[] { -20.0, -10.0, 0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0 };

            return new GlycolTypeData
            {
                Concentrations = concentrations,
                Temperatures = temperatures,
                Density = CreateDefaultTable(concentrations, temperatures, DefaultDensityValues()),
                SpecificHeat = CreateDefaultTable(concentrations, temperatures, DefaultSpecificHeatValues()),
                KinematicViscosity = CreateDefaultTable(concentrations, temperatures, DefaultViscosityValues()),
                ThermalConductivity = CreateDefaultTable(concentrations, temperatures, DefaultConductivityValues())
            };
        }

        private static GlycolTypeData GetDefaultPropyleneData()
        {
            var concentrations = new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0 };
            var temperatures = new[] { -20.0, -10.0, 0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0 };

            return new GlycolTypeData
            {
                Concentrations = concentrations,
                Temperatures = temperatures,
                Density = CreateDefaultTable(concentrations, temperatures, DefaultDensityValues()),
                SpecificHeat = CreateDefaultTable(concentrations, temperatures, DefaultSpecificHeatValues()),
                KinematicViscosity = CreateDefaultTable(concentrations, temperatures, DefaultViscosityValues()),
                ThermalConductivity = CreateDefaultTable(concentrations, temperatures, DefaultConductivityValues())
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

        private static double[,] DefaultDensityValues()
        {
            return new double[,]
            {
                { 1035, 1030, 1025, 1020, 1015, 1010, 1005, 1000, 995 },
                { 1045, 1040, 1035, 1030, 1025, 1020, 1015, 1010, 1005 },
                { 1055, 1050, 1045, 1040, 1035, 1030, 1025, 1020, 1015 },
                { 1065, 1060, 1055, 1050, 1045, 1040, 1035, 1030, 1025 },
                { 1075, 1070, 1065, 1060, 1055, 1050, 1045, 1040, 1035 },
                { 1085, 1080, 1075, 1070, 1065, 1060, 1055, 1050, 1045 },
                { 1095, 1090, 1085, 1080, 1075, 1070, 1065, 1060, 1055 },
                { 1105, 1100, 1095, 1090, 1085, 1080, 1075, 1070, 1065 },
                { 1115, 1110, 1105, 1100, 1095, 1090, 1085, 1080, 1075 }
            };
        }

        private static double[,] DefaultSpecificHeatValues()
        {
            return new double[,]
            {
                { 3.90, 3.92, 3.94, 3.96, 3.98, 4.00, 4.02, 4.04, 4.06 },
                { 3.75, 3.77, 3.79, 3.81, 3.83, 3.85, 3.87, 3.89, 3.91 },
                { 3.60, 3.62, 3.64, 3.66, 3.68, 3.70, 3.72, 3.74, 3.76 },
                { 3.45, 3.47, 3.49, 3.51, 3.53, 3.55, 3.57, 3.59, 3.61 },
                { 3.30, 3.32, 3.34, 3.36, 3.38, 3.40, 3.42, 3.44, 3.46 },
                { 3.15, 3.17, 3.19, 3.21, 3.23, 3.25, 3.27, 3.29, 3.31 },
                { 3.00, 3.02, 3.04, 3.06, 3.08, 3.10, 3.12, 3.14, 3.16 },
                { 2.85, 2.87, 2.89, 2.91, 2.93, 2.95, 2.97, 2.99, 3.01 },
                { 2.70, 2.72, 2.74, 2.76, 2.78, 2.80, 2.82, 2.84, 2.86 }
            };
        }

        private static double[,] DefaultViscosityValues()
        {
            return new double[,]
            {
                { 5.0, 3.5, 2.5, 1.8, 1.4, 1.1, 0.9, 0.7, 0.6 },
                { 10.0, 6.5, 4.5, 3.0, 2.2, 1.7, 1.3, 1.0, 0.8 },
                { 20.0, 12.0, 8.0, 5.0, 3.5, 2.5, 1.9, 1.5, 1.2 },
                { 35.0, 20.0, 13.0, 8.0, 5.5, 3.8, 2.8, 2.1, 1.6 },
                { 55.0, 30.0, 19.0, 12.0, 8.0, 5.5, 4.0, 3.0, 2.3 },
                { 85.0, 45.0, 28.0, 17.0, 11.0, 7.5, 5.3, 4.0, 3.0 },
                { 130.0, 65.0, 40.0, 24.0, 15.0, 10.0, 7.0, 5.2, 4.0 },
                { 190.0, 90.0, 55.0, 32.0, 20.0, 13.0, 9.0, 6.5, 5.0 },
                { 270.0, 125.0, 75.0, 43.0, 26.0, 17.0, 11.5, 8.5, 6.5 }
            };
        }

        private static double[,] DefaultConductivityValues()
        {
            return new double[,]
            {
                { 0.48, 0.49, 0.50, 0.51, 0.52, 0.53, 0.54, 0.55, 0.56 },
                { 0.45, 0.46, 0.47, 0.48, 0.49, 0.50, 0.51, 0.52, 0.53 },
                { 0.42, 0.43, 0.44, 0.45, 0.46, 0.47, 0.48, 0.49, 0.50 },
                { 0.39, 0.40, 0.41, 0.42, 0.43, 0.44, 0.45, 0.46, 0.47 },
                { 0.36, 0.37, 0.38, 0.39, 0.40, 0.41, 0.42, 0.43, 0.44 },
                { 0.33, 0.34, 0.35, 0.36, 0.37, 0.38, 0.39, 0.40, 0.41 },
                { 0.30, 0.31, 0.32, 0.33, 0.34, 0.35, 0.36, 0.37, 0.38 },
                { 0.27, 0.28, 0.29, 0.30, 0.31, 0.32, 0.33, 0.34, 0.35 },
                { 0.24, 0.25, 0.26, 0.27, 0.28, 0.29, 0.30, 0.31, 0.32 }
            };
        }

        #endregion

        #region JSON Data Models

        /// <summary>
        /// Контейнер данных гликолей из JSON
        /// </summary>
        internal class GlycolRawContainer
        {
            public GlycolTypeRawData? EthyleneGlycol { get; set; }
            public GlycolTypeRawData? PropyleneGlycol { get; set; }
        }

        /// <summary>
        /// Данные для конкретного типа гликоля из JSON
        /// </summary>
        internal class GlycolTypeRawData
        {
            public double[]? Concentrations { get; set; }
            public PropertyData? Density { get; set; }
            public PropertyData? SpecificHeat { get; set; }
            public PropertyData? KinematicViscosity { get; set; }
            public PropertyData? ThermalConductivity { get; set; }
        }

        /// <summary>
        /// Данные свойства из JSON
        /// </summary>
        internal class PropertyData
        {
            public List<TemperatureDataRow>? Data { get; set; }
        }

        /// <summary>
        /// Строка данных для температуры
        /// </summary>
        internal class TemperatureDataRow
        {
            public double? TempC { get; set; }
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