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
        private const double MAX_TEMPERATURE = 100.0;

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
        public GlycolDataService() : this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "glycol_data.json"))
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
        /// <returns>true, если концентрация в допустимом диапазоне (0% для воды или 10-90% для гликолей)</returns>
        public bool IsConcentrationSupported(double concentration)
        {
            // Концентрация 0% разрешена для воды
            if (concentration == 0)
                return true;
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
        /// Используются табличные значения IAPWS с линейной интерполяцией для диапазона 0-100°C:
        /// - Плотность: интерполяция по таблице IAPWS
        /// - Вязкость: интерполяция по таблице IAPWS
        /// - Теплоёмкость: c_p ≈ 4.18 кДж/(кг·К) (слабо зависит от T)
        /// - Теплопроводность: интерполяция по таблице IAPWS
        /// </remarks>
        public GlycolProperties GetWaterProperties(double temperature)
        {
            if (temperature < 0 || temperature > MAX_TEMPERATURE)
            {
                throw new ArgumentOutOfRangeException(nameof(temperature),
                    $"Температура воды должна быть в диапазоне 0°C до {MAX_TEMPERATURE}°C, получено: {temperature}°C");
            }

            // Получаем свойства воды по табличным значениям IAPWS
            double density = GetWaterDensity(temperature);
            double kinematicViscosity = GetWaterKinematicViscosity(temperature);
            double specificHeat = GetWaterSpecificHeat(temperature);
            double thermalConductivity = GetWaterThermalConductivity(temperature);

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

        /// <summary>
        /// Плотность воды (кг/м³) - интерполяция по таблице IAPWS
        /// </summary>
        private static double GetWaterDensity(double temperature)
        {
            // Табличные значения плотности воды (кг/м³) по IAPWS
            // T(°C):  0,    10,   20,   30,   40,   50,   60,   70,   80,   90,   100
            // ρ:      999.8, 999.7, 998.2, 995.7, 992.2, 988.0, 983.2, 977.8, 971.8, 965.3, 958.4
            double[] temps = { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            double[] dens = { 999.8, 999.7, 998.2, 995.7, 992.2, 988.0, 983.2, 977.8, 971.8, 965.3, 958.4 };

            return LinearInterpolateTable(temps, dens, temperature);
        }

        /// <summary>
        /// Кинематическая вязкость воды (мм²/с) - интерполяция по таблице IAPWS
        /// </summary>
        private static double GetWaterKinematicViscosity(double temperature)
        {
            // Табличные значения кинематической вязкости воды (мм²/с) по IAPWS
            // T(°C):  0,    10,   20,   30,   40,   50,   60,   70,   80,   90,   100
            // ν:      1.79, 1.31, 1.00, 0.80, 0.66, 0.55, 0.47, 0.41, 0.36, 0.33, 0.30
            double[] temps = { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            double[] visc = { 1.79, 1.31, 1.00, 0.80, 0.66, 0.55, 0.47, 0.41, 0.36, 0.33, 0.30 };

            return LinearInterpolateTable(temps, visc, temperature);
        }

        /// <summary>
        /// Удельная теплоёмкость воды (кДж/(кг·К))
        /// </summary>
        private static double GetWaterSpecificHeat(double temperature)
        {
            // Теплоёмкость воды слабо зависит от температуры
            // При 20°C: c_p ≈ 4.182 кДж/(кг·К)
            // При 50°C: c_p ≈ 4.181 кДж/(кг·К)
            // При 90°C: c_p ≈ 4.205 кДж/(кг·К)
            // Используем линейную аппроксимацию
            return 4.182 + 0.0003 * (temperature - 20);
        }

        /// <summary>
        /// Теплопроводность воды (Вт/(м·К)) - интерполяция по таблице IAPWS
        /// </summary>
        private static double GetWaterThermalConductivity(double temperature)
        {
            // Табличные значения теплопроводности воды (Вт/(м·К)) по IAPWS
            // T(°C):  0,     10,    20,    30,    40,    50,    60,    70,    80,    90,    100
            // λ:      0.569, 0.580, 0.598, 0.618, 0.635, 0.648, 0.659, 0.668, 0.674, 0.678, 0.680
            double[] temps = { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            double[] cond = { 0.569, 0.580, 0.598, 0.618, 0.635, 0.648, 0.659, 0.668, 0.674, 0.678, 0.680 };

            return LinearInterpolateTable(temps, cond, temperature);
        }

        /// <summary>
        /// Линейная интерполяция по табличным значениям
        /// </summary>
        private static double LinearInterpolateTable(double[] temps, double[] values, double temperature)
        {
            if (temps.Length == 0 || values.Length == 0)
                return 0;

            // Граничные случаи
            if (temperature <= temps[0])
                return values[0];
            if (temperature >= temps[temps.Length - 1])
                return values[values.Length - 1];

            // Найти интервал для интерполяции
            int i = 0;
            while (i < temps.Length - 1 && temps[i + 1] < temperature)
                i++;

            if (i >= temps.Length - 1)
                return values[values.Length - 1];

            // Линейная интерполяция
            double t1 = temps[i];
            double t2 = temps[i + 1];
            double v1 = values[i];
            double v2 = values[i + 1];

            return v1 + (v2 - v1) * (temperature - t1) / (t2 - t1);
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
                return double.NaN;

            return array[index] ?? double.NaN;
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
                return double.NaN;

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

                return LinearInterpolateWithNaN(temp1, temp2, val1, val2, temperature);
            }

            if (tLow == tHigh)
            {
                // Интерполяция только по концентрации
                double conc1 = concentrations[cLow];
                double conc2 = concentrations[cHigh];
                double val1 = values[cLow, tLow];
                double val2 = values[cHigh, tLow];

                return LinearInterpolateWithNaN(conc1, conc2, val1, val2, concentration);
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
            double v1_interp = LinearInterpolateWithNaN(t1, t2, v11, v12, temperature);
            double v2_interp = LinearInterpolateWithNaN(t1, t2, v21, v22, temperature);

            // Интерполяция по концентрации
            return LinearInterpolateWithNaN(c1, c2, v1_interp, v2_interp, concentration);
        }

        /// <summary>
        /// Линейная интерполяция с обработкой NaN значений
        /// </summary>
        private static double LinearInterpolateWithNaN(double x1, double x2, double y1, double y2, double x)
        {
            // Если оба значения NaN, возвращаем NaN
            if (double.IsNaN(y1) && double.IsNaN(y2))
                return double.NaN;

            // Если одно значение NaN, используем другое
            if (double.IsNaN(y1))
                return y2;
            if (double.IsNaN(y2))
                return y1;

            // Обычная линейная интерполяция
            return LinearInterpolate(x1, x2, y1, y2, x);
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
            // Концентрация 0% разрешена для воды
            if (concentration == 0)
            {
                // Для воды минимальная температура 0°C
                if (temperature < 0 || temperature > MAX_TEMPERATURE)
                {
                    throw new ArgumentOutOfRangeException(nameof(temperature),
                        $"Температура воды должна быть в диапазоне 0°C до {MAX_TEMPERATURE}°C, получено: {temperature}°C");
                }
                return;
            }

            if (concentration < MIN_CONCENTRATION || concentration > MAX_CONCENTRATION)
            {
                throw new ArgumentOutOfRangeException(nameof(concentration),
                    $"Концентрация должна быть 0% (вода) или в диапазоне {MIN_CONCENTRATION}-{MAX_CONCENTRATION}%, получено: {concentration}%");
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
        /// NaN означает отсутствие данных для данной температуры/концентрации
        /// </summary>
        private static double[,] DefaultEthyleneDensityValues()
        {
            // Данные из JSON: density_kg_m3
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10% - значения для температур -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9
                {  double.NaN, double.NaN, 1019.2, 1015.7, 1012.1, 1008.3, 1004.5, 1000.6,  996.7 },
                // conc: 20%
                {  double.NaN, double.NaN, 1053.2, 1049.5, 1045.7, 1041.9, 1038.0, 1034.1, 1030.2 },
                // conc: 30%
                {  double.NaN, 1072.2, 1068.6, 1064.8, 1060.9, 1056.9, 1052.9, 1048.9, 1044.8 },
                // conc: 40%
                {  double.NaN, 1087.2, 1083.4, 1079.2, 1074.9, 1070.5, 1066.0, 1061.3, 1056.6 },
                // conc: 50%
                {  double.NaN, 1101.5, 1097.3, 1092.8, 1088.2, 1083.4, 1078.5, 1073.4, 1068.2 },
                // conc: 60%
                {  1090.7, 1115.1, 1110.6, 1105.6, 1100.3, 1094.7, 1088.9, 1082.9, 1076.7 },
                // conc: 70%
                {  1105.3, 1128.4, 1123.5, 1118.2, 1112.6, 1106.7, 1100.6, 1094.3, 1087.9 },
                // conc: 80%
                {  1119.1, 1141.3, 1136.1, 1130.4, 1124.3, 1117.9, 1111.2, 1104.2, 1097.0 },
                // conc: 90%
                {  1132.5, 1153.8, 1148.1, 1141.8, 1135.0, 1127.7, 1120.1, 1112.2, 1104.1 }
            };
        }

        /// <summary>
        /// Fallback значения удельной теплоёмкости для этиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// NaN означает отсутствие данных для данной температуры/концентрации
        /// </summary>
        private static double[,] DefaultEthyleneSpecificHeatValues()
        {
            // Данные из JSON: specific_heat_kJ_kgK
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10% - значения для температур -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9
                {  double.NaN, double.NaN, 3.78,   4.36,   5.00,   5.65,   6.29,   6.93,   7.58 },
                // conc: 20%
                {  double.NaN, double.NaN, 3.14,   3.77,   4.39,   5.01,   5.63,   6.25,   6.87 },
                // conc: 30%
                {  double.NaN, 3.35,   2.92,   3.59,   4.20,   4.83,   5.46,   6.09,   6.72 },
                // conc: 40%
                {  double.NaN, 3.13,   2.70,   3.40,   4.03,   4.65,   5.28,   5.91,   6.54 },
                // conc: 50%
                {  double.NaN, 2.92,   2.47,   3.20,   3.84,   4.47,   5.10,   5.73,   6.36 },
                // conc: 60%
                {  3.07,  2.70,   2.22,   3.00,   3.63,   4.26,   4.89,   5.52,   6.15 },
                // conc: 70%
                {  2.85,  2.47,   2.01,   2.78,   3.41,   4.04,   4.67,   5.30,   5.93 },
                // conc: 80%
                {  2.62,  2.23,   1.82,   2.54,   3.17,   3.80,   4.43,   5.06,   5.69 },
                // conc: 90%
                {  2.37,  2.03,   1.62,   2.33,   2.94,   3.57,   4.20,   4.83,   5.46 }
            };
        }

        /// <summary>
        /// Fallback значения кинематической вязкости для этиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// NaN означает отсутствие данных для данной температуры/концентрации
        /// </summary>
        private static double[,] DefaultEthyleneViscosityValues()
        {
            // Данные из JSON: kinematic_viscosity_mm2_s
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10% - значения для температур -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9
                {  double.NaN, double.NaN, 2.6,    1.0,    0.5,    0.3,    0.2,    0.1,    0.1 },
                // conc: 20%
                {  double.NaN, double.NaN, 3.7,    1.4,    0.7,    0.4,    0.2,    0.1,    0.1 },
                // conc: 30%
                {  double.NaN, 12.9,   5.5,    2.0,    0.8,    0.5,    0.3,    0.2,    0.1 },
                // conc: 40%
                {  double.NaN, 17.8,   7.9,    2.7,    1.1,    0.6,    0.4,    0.2,    0.1 },
                // conc: 50%
                {  double.NaN, 27.2,   11.4,   3.8,    1.6,    0.8,    0.5,    0.3,    0.2 },
                // conc: 60%
                {  58.4,  40.8,   17.4,   5.3,    2.1,    1.1,    0.6,    0.3,    0.2 },
                // conc: 70%
                {  81.2,  57.5,   23.2,   7.3,    2.8,    1.4,    0.7,    0.4,    0.3 },
                // conc: 80%
                {  115.0, 79.4,   31.6,   10.2,   3.7,    1.9,    0.9,    0.5,    0.3 },
                // conc: 90%
                {  163.5, 93.3,   38.9,   13.7,   4.8,    2.4,    1.2,    0.5,    0.4 }
            };
        }

        /// <summary>
        /// Fallback значения теплопроводности для этиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// NaN означает отсутствие данных для данной температуры/концентрации
        /// </summary>
        private static double[,] DefaultEthyleneConductivityValues()
        {
            // Данные из JSON: thermal_conductivity_W_mK
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10% - значения для температур -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9
                {  double.NaN, double.NaN, 0.462,  0.602,  0.744,  0.885,  1.027,  1.168,  1.310 },
                // conc: 20%
                {  double.NaN, double.NaN, 0.337,  0.456,  0.579,  0.702,  0.825,  0.948,  1.071 },
                // conc: 30%
                {  double.NaN, 0.369,  0.311,  0.416,  0.527,  0.638,  0.749,  0.860,  0.971 },
                // conc: 40%
                {  double.NaN, 0.338,  0.287,  0.382,  0.481,  0.580,  0.679,  0.778,  0.877 },
                // conc: 50%
                {  double.NaN, 0.313,  0.268,  0.355,  0.445,  0.535,  0.625,  0.715,  0.805 },
                // conc: 60%
                {  0.324,  0.291,  0.252,  0.327,  0.412,  0.493,  0.574,  0.655,  0.736 },
                // conc: 70%
                {  0.300,  0.271,  0.239,  0.300,  0.377,  0.451,  0.523,  0.595,  0.667 },
                // conc: 80%
                {  0.279,  0.255,  0.227,  0.275,  0.341,  0.402,  0.462,  0.522,  0.582 },
                // conc: 90%
                {  0.261,  0.244,  0.225,  0.262,  0.313,  0.363,  0.412,  0.460,  0.508 }
            };
        }

        #endregion

        #region Propylene Glycol Fallback Data

        /// <summary>
        /// Fallback значения плотности для пропиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// NaN означает отсутствие данных для данной температуры/концентрации
        /// </summary>
        private static double[,] DefaultPropyleneDensityValues()
        {
            // Данные из JSON: density_kg_m3
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10% - значения для температур -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9
                {  double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN },
                // conc: 20%
                {  double.NaN, double.NaN, double.NaN, 1020.0, 1014.0, 1007.0, 999.0,  990.0,  981.0 },
                // conc: 30%
                {  double.NaN, double.NaN, 1036.0, 1031.0, 1025.0, 1019.0, 1012.0, 1004.0, 995.0 },
                // conc: 40%
                {  double.NaN, double.NaN, 1047.0, 1040.0, 1033.0, 1026.0, 1019.0, 1010.0, 1001.0 },
                // conc: 50%
                {  double.NaN, 1073.6, 1054.0, 1048.0, 1040.0, 1032.0, 1024.0, 1015.0, 1006.0 },
                // conc: 60%
                {  1074.0, 1083.2, 1062.0, 1055.0, 1046.0, 1037.0, 1028.0, 1018.0, 1007.0 },
                // conc: 70%
                {  1082.2, 1081.6, 1066.0, 1058.0, 1047.0, 1036.0, 1025.0, 1014.0, 1002.0 },
                // conc: 80%
                {  1095.3, double.NaN, 1069.0, 1058.0, 1044.0, 1031.0, 1018.0, 1005.0, 991.0 },
                // conc: 90%
                {  1094.8, double.NaN, 1069.0, 1055.0, 1039.0, 1024.0, 1009.0, 994.0,  979.0 }
            };
        }

        /// <summary>
        /// Fallback значения удельной теплоёмкости для пропиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// NaN означает отсутствие данных для данной температуры/концентрации
        /// </summary>
        private static double[,] DefaultPropyleneSpecificHeatValues()
        {
            // Данные из JSON: specific_heat_kJ_kgK
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10% - значения для температур -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9
                {  double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN },
                // conc: 20%
                {  double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN },
                // conc: 30%
                {  double.NaN, double.NaN, 4.05,   4.08,   4.10,   4.13,   4.15,   4.18,   4.20 },
                // conc: 40%
                {  double.NaN, double.NaN, 3.93,   3.97,   4.00,   4.04,   4.08,   4.12,   4.15 },
                // conc: 50%
                {  double.NaN, 3.58,   3.76,   3.83,   3.89,   3.94,   3.99,   4.05,   4.09 },
                // conc: 60%
                {  3.10,  3.17,   3.58,   3.68,   3.75,   3.82,   3.89,   3.96,   4.02 },
                // conc: 70%
                {  2.85,  2.93,   3.38,   3.52,   3.61,   3.69,   3.78,   3.87,   3.94 },
                // conc: 80%
                {  2.58,  2.67,   3.14,   3.33,   3.44,   3.54,   3.66,   3.77,   3.86 },
                // conc: 90%
                {  2.27,  2.37,   2.87,   3.13,   3.28,   3.40,   3.53,   3.66,   3.77 }
            };
        }

        /// <summary>
        /// Fallback значения кинематической вязкости для пропиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// NaN означает отсутствие данных для данной температуры/концентрации
        /// </summary>
        private static double[,] DefaultPropyleneViscosityValues()
        {
            // Данные из JSON: kinematic_viscosity_mm2_s
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10% - значения для температур -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9
                {  double.NaN, double.NaN,    double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN },
                // conc: 20%
                {  double.NaN, double.NaN,    double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN },
                // conc: 30%
                {  double.NaN, double.NaN,    6.77,   3.87,   2.54,   1.81,   1.38,   1.06,   0.87 },
                // conc: 40%
                {  double.NaN, 98.99,    10.23,  5.61,   3.46,   2.35,   1.72,   1.31,   1.04 },
                // conc: 50%
                {  double.NaN, 149.55,   18.05,  8.76,   4.93,   3.14,   2.20,   1.64,   1.31 },
                // conc: 60%
                {  1203.67, 277.95,   31.74,  13.45,  6.97,   4.19,   2.81,   2.06,   1.60 },
                // conc: 70%
                {  2092.20, 429.94,   47.22,  19.57,  9.82,   5.71,   3.70,   2.59,   1.96 },
                // conc: 80%
                {  3299.03, 735.26,   81.47,  30.46,  13.96,  7.52,   4.62,   3.12,   2.27 },
                // conc: 90%
                {  8600.39, 1350.63,  119.31, 43.20,  19.35,  10.23,  6.14,   4.09,   2.93 }
            };
        }

        /// <summary>
        /// Fallback значения теплопроводности для пропиленгликоля
        /// Источник: ASHRAE Handbook - Fundamentals (2009), Dow Chemical Tables
        /// NaN означает отсутствие данных для данной температуры/концентрации
        /// </summary>
        private static double[,] DefaultPropyleneConductivityValues()
        {
            // Данные из JSON: thermal_conductivity_W_mK
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10% - значения для температур -34.4, -17.8, -1.1, 15.6, 32.2, 48.9, 65.6, 82.2, 98.9
                {  double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN },
                // conc: 20%
                {  double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN },
                // conc: 30%
                {  double.NaN, double.NaN, 0.455,  0.533,  0.556,  0.574,  0.585,  0.601,  0.604 },
                // conc: 40%
                {  double.NaN, 0.348,  0.408,  0.477,  0.497,  0.512,  0.522,  0.535,  0.537 },
                // conc: 50%
                {  double.NaN, 0.313,  0.365,  0.427,  0.444,  0.456,  0.466,  0.474,  0.476 },
                // conc: 60%
                {  0.270, 0.280,  0.325,  0.385,  0.395,  0.407,  0.414,  0.419,  0.420 },
                // conc: 70%
                {  0.242, 0.251,  0.293,  0.343,  0.350,  0.361,  0.367,  0.371,  0.371 },
                // conc: 80%
                {  0.220, 0.227,  0.261,  0.300,  0.307,  0.315,  0.320,  0.323,  0.323 },
                // conc: 90%
                {  0.203, 0.206,  0.234,  0.265,  0.270,  0.275,  0.279,  0.280,  0.280 }
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