using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnowMeltingCalculator.Core.Constants;
using SnowMeltingCalculator.Models.Hydraulics;

using SnowMeltingCalculator.Services.Logging;
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
        /// Минимальная концентрация (% об.), при которой свойства типа
        /// интерполируются при заданной температуре без NaN: все четыре
        /// матрицы числовые в двух рядах, окружающих температуру.
        /// </summary>
        /// <remarks>
        /// Волна 3.6 (D9): порог для рекомендации «повысьте концентрацию до
        /// ≥ X %» вычисляется из той же матрицы, что и интерполяция, — без
        /// констант-копий (единственный источник данных). null — при этой
        /// температуре не валидна ни одна концентрация базы. Валидность
        /// колонки согласована с <see cref="InterpolateProperty"/>: соседний
        /// NaN-ряд делает интерполяцию NaN даже при попадании точки на
        /// числовой ряд.
        /// </remarks>
        public double? GetMinValidConcentration(GlycolType glycolType, double temperature)
        {
            var data = LoadData();
            var glycolData = GetGlycolData(data, glycolType);
            var temps = glycolData.Temperatures;
            if (temps.Length == 0 || temperature < temps[0] || temperature > temps[^1])
                return null;

            int hi = Array.FindIndex(temps, t => t >= temperature);
            if (hi < 0) hi = temps.Length - 1;
            int lo = Math.Max(0, hi - 1);

            for (int c = 0; c < glycolData.Concentrations.Length; c++)
            {
                bool columnValid =
                    IsMatrixCellValid(glycolData.Density, c, lo, hi) &&
                    IsMatrixCellValid(glycolData.SpecificHeat, c, lo, hi) &&
                    IsMatrixCellValid(glycolData.KinematicViscosity, c, lo, hi) &&
                    IsMatrixCellValid(glycolData.ThermalConductivity, c, lo, hi);

                if (columnValid)
                    return glycolData.Concentrations[c];
            }

            return null;
        }

        private static bool IsMatrixCellValid(InterpolationTable table, int concentrationIndex, int tLow, int tHigh)
        {
            return !double.IsNaN(table.Values[concentrationIndex, tLow])
                && !double.IsNaN(table.Values[concentrationIndex, tHigh]);
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
            return concentration >= ValidationConstants.MinGlycolConcentration
                && concentration <= ValidationConstants.MaxGlycolConcentration;
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
        public double GetMinConcentration() => ValidationConstants.MinGlycolConcentration;

        /// <summary>
        /// Получить максимальную поддерживаемую концентрацию
        /// </summary>
        /// <returns>Максимальная концентрация, %</returns>
        public double GetMaxConcentration() => ValidationConstants.MaxGlycolConcentration;

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
                    // (fallback JSON-канона — ADR-016; событие видно в журнале)
                    AppLog.Warn("GlycolDataService.LoadData: файл данных не найден, используются встроенные fallback-таблицы");
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
                catch (Exception ex) {
                    AppLog.Warn(ex, "GlycolDataService.LoadData");
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
        /// <remarks>
        /// D9 (волна 3 hardening-роадмапа): NaN в таблице — точка вне
        /// физического диапазона (зона замерзания/расслоения гликоля).
        /// Прежняя подмена «одно NaN — берём соседнее» молча считала
        /// гидравлику от значений замёрзшего теплоносителя; теперь NaN
        /// распространяется наверх и превращается в Error расчёта
        /// (<see cref="CircuitsViewModel"/> гвардит свойства после
        /// <see cref="GetProperties"/>).
        /// </remarks>
        private static double LinearInterpolateWithNaN(double x1, double x2, double y1, double y2, double x)
        {
            // Любая NaN на входе (обе границы ряда или одна) — результат NaN
            if (double.IsNaN(y1) || double.IsNaN(y2))
                return double.NaN;

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

            if (concentration < ValidationConstants.MinGlycolConcentration
                || concentration > ValidationConstants.MaxGlycolConcentration)
            {
                throw new ArgumentOutOfRangeException(nameof(concentration),
                    $"Концентрация должна быть 0% (вода) или в диапазоне {ValidationConstants.MinGlycolConcentration}-{ValidationConstants.MaxGlycolConcentration}%, получено: {concentration}%");
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
        /// Канон — data/glycol_data.json (ADR-016); таблица списана с узлов канона
        /// (канон: 29 температур -34.4…121.1, шаг ~5.6; fallback-сетка — 9 узлов -34.4…98.9). NaN — нет данных в каноне
        /// </summary>
        private static double[,] DefaultEthyleneDensityValues()
        {
            // Данные из JSON: density_kg_m3
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10%,
                { double.NaN, double.NaN, 1020.2, 1015.9, 1010.3, 1003.2, 994.9, 985.3, 974.2 },
                // conc: 20%,
                { double.NaN, double.NaN, 1037.2, 1032.4, 1026.3, 1018.9, 1010.1, 999.9, 988.5 },
                // conc: 30%,
                { double.NaN, double.NaN, 1053.4, 1048.1, 1041.5, 1033.5, 1024.4, 1013.6, 1001.6 },
                // conc: 40%,
                { double.NaN, 1072.8, 1068.4, 1062.7, 1055.6, 1047.1, 1037.4, 1026.3, 1013.8 },
                // conc: 50%,
                { 1091.2, 1087.7, 1082.7, 1076.4, 1068.9, 1059.9, 1049.7, 1038, 1025 },
                // conc: 60%,
                { 1105.8, 1101.7, 1096.3, 1089.6, 1081.4, 1072, 1061.2, 1049, 1035.4 },
                // conc: 70%,
                { 1119.7, 1115.2, 1109.3, 1101.9, 1093.3, 1083.3, 1072, 1059.1, 1045 },
                // conc: 80%,
                { 1133.3, 1128.2, 1121.6, 1113.8, 1104.5, 1093.9, 1082, 1068.8, 1054.2 },
                // conc: 90%
                { double.NaN, 1140.5, 1133.5, 1125, 1115.2, 1104, 1091.5, 1077.7, 1062.5 },
            };
        }

        /// <summary>
        /// Fallback значения удельной теплоёмкости для этиленгликоля
        /// Канон — data/glycol_data.json (ADR-016); таблица списана с узлов канона
        /// (канон: 29 температур -34.4…121.1, шаг ~5.6; fallback-сетка — 9 узлов -34.4…98.9). NaN — нет данных в каноне
        /// </summary>
        private static double[,] DefaultEthyleneSpecificHeatValues()
        {
            // Данные из JSON: specific_heat_kJ_kgK
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10%,
                { double.NaN, double.NaN, 3.936, 3.965, 3.994, 4.024, 4.053, 4.082, 4.111 },
                // conc: 20%,
                { double.NaN, double.NaN, 3.768, 3.806, 3.843, 3.885, 3.923, 3.961, 3.998 },
                // conc: 30%,
                { double.NaN, double.NaN, 3.588, 3.634, 3.684, 3.73, 3.776, 3.823, 3.873 },
                // conc: 40%,
                { double.NaN, 3.345, 3.4, 3.454, 3.513, 3.567, 3.622, 3.68, 3.735 },
                // conc: 50%,
                { 3.073, 3.136, 3.203, 3.266, 3.329, 3.395, 3.458, 3.525, 3.588 },
                // conc: 60%,
                { 2.847, 2.922, 2.994, 3.065, 3.14, 3.211, 3.287, 3.358, 3.429 },
                // conc: 70%,
                { 2.617, 2.696, 2.78, 2.86, 2.939, 3.019, 3.102, 3.182, 3.262 },
                // conc: 80%,
                { 2.374, 2.462, 2.554, 2.642, 2.73, 2.818, 2.906, 2.998, 3.086 },
                // conc: 90%
                { double.NaN, 2.219, 2.315, 2.412, 2.512, 2.608, 2.705, 2.801, 2.897 },
            };
        }

        /// <summary>
        /// Fallback значения кинематической вязкости для этиленгликоля
        /// Канон — data/glycol_data.json (ADR-016); таблица списана с узлов канона
        /// (канон: 29 температур -34.4…121.1, шаг ~5.6; fallback-сетка — 9 узлов -34.4…98.9). NaN — нет данных в каноне
        /// </summary>
        private static double[,] DefaultEthyleneViscosityValues()
        {
            // Данные из JSON: kinematic_viscosity_mm2_s
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10%,
                { double.NaN, double.NaN, 2.12, 1.33, 0.92, 0.68, 0.53, 0.44, 0.36 },
                // conc: 20%,
                { double.NaN, double.NaN, 3.03, 1.8, 1.21, 0.88, 0.67, 0.54, 0.43 },
                // conc: 30%,
                { double.NaN, double.NaN, 4.11, 2.37, 1.54, 1.08, 0.8, 0.62, 0.5 },
                // conc: 40%,
                { double.NaN, 12.83, 5.7, 3.18, 2.02, 1.39, 1.01, 0.77, 0.6 },
                // conc: 50%,
                { 58.37, 17.78, 7.83, 4.23, 2.58, 1.72, 1.22, 0.9, 0.69 },
                // conc: 60%,
                { 81.09, 27.3, 11.56, 5.81, 3.31, 2.08, 1.4, 1.01, 0.75 },
                // conc: 70%,
                { 115.02, 40.87, 16.92, 8.14, 4.44, 2.66, 1.74, 1.2, 0.88 },
                // conc: 80%,
                { 163.43, 57.65, 23.04, 10.82, 5.78, 3.41, 2.17, 1.48, 1.06 },
                // conc: 90%
                { double.NaN, 94.49, 31.68, 13.91, 7.26, 4.26, 2.73, 1.86, 1.35 },
            };
        }

        /// <summary>
        /// Fallback значения теплопроводности для этиленгликоля
        /// Канон — data/glycol_data.json (ADR-016); таблица списана с узлов канона
        /// (канон: 29 температур -34.4…121.1, шаг ~5.6; fallback-сетка — 9 узлов -34.4…98.9). NaN — нет данных в каноне
        /// </summary>
        private static double[,] DefaultEthyleneConductivityValues()
        {
            // Данные из JSON: thermal_conductivity_W_mK
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10%,
                { double.NaN, double.NaN, 0.509, 0.537, 0.559, 0.578, 0.592, 0.604, 0.609 },
                // conc: 20%,
                { double.NaN, double.NaN, 0.464, 0.486, 0.505, 0.521, 0.535, 0.543, 0.549 },
                // conc: 30%,
                { double.NaN, double.NaN, 0.422, 0.441, 0.457, 0.471, 0.479, 0.488, 0.492 },
                // conc: 40%,
                { double.NaN, 0.369, 0.384, 0.4, 0.414, 0.424, 0.433, 0.438, 0.441 },
                // conc: 50%,
                { 0.324, 0.337, 0.351, 0.363, 0.374, 0.382, 0.389, 0.395, 0.396 },
                // conc: 60%,
                { 0.299, 0.312, 0.322, 0.331, 0.339, 0.346, 0.351, 0.355, 0.357 },
                // conc: 70%,
                { 0.279, 0.287, 0.296, 0.303, 0.31, 0.315, 0.318, 0.322, 0.322 },
                // conc: 80%,
                { 0.261, 0.268, 0.275, 0.28, 0.284, 0.289, 0.291, 0.292, 0.294 },
                // conc: 90%
                { double.NaN, 0.254, 0.258, 0.261, 0.265, 0.268, 0.27, 0.272, 0.272 },
            };
        }

        #endregion

        #region Propylene Glycol Fallback Data

        /// <summary>
        /// Fallback значения плотности для пропиленгликоля
        /// Канон — data/glycol_data.json (ADR-016); таблица списана с узлов канона
        /// (канон: 29 температур -34.4…121.1, шаг ~5.6; fallback-сетка — 9 узлов -34.4…98.9). NaN — нет данных в каноне
        /// </summary>
        private static double[,] DefaultPropyleneDensityValues()
        {
            // Данные из JSON: density_kg_m3
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10%,
                { double.NaN, double.NaN, 1015.3, 1010.8, 1004.8, 997.6, 989, 978.9, 967.7 },
                // conc: 20%,
                { double.NaN, double.NaN, 1027.4, 1021.8, 1015.1, 1006.8, 997.1, 986.1, 973.6 },
                // conc: 30%,
                { double.NaN, double.NaN, 1037.8, 1031.4, 1023.7, 1014.4, 1003.9, 991.9, 978.4 },
                // conc: 40%,
                { double.NaN, 1052.6, 1046.8, 1039.6, 1030.9, 1021, 1009.5, 996.7, 982.3 },
                // conc: 50%,
                { double.NaN, 1060.9, 1054.3, 1046.5, 1037, 1026.1, 1014, 1000.4, 985.1 },
                // conc: 60%,
                { 1074, 1068.1, 1060.7, 1052.1, 1041.8, 1030.3, 1017.3, 1002.9, 987.2 },
                // conc: 70%,
                { 1080.8, 1074, 1065.9, 1056.4, 1045.5, 1033.4, 1019.7, 1004.7, 988.2 },
                // conc: 80%,
                { 1095.3, 1083.2, 1070.5, 1057.9, 1044.7, 1031.4, 1018, 1004, 990.1 },
                // conc: 90%
                { 1093.3, 1081.1, 1068.6, 1055.8, 1042.6, 1029.3, 1015.9, 1002.1, 988 },
            };
        }

        /// <summary>
        /// Fallback значения удельной теплоёмкости для пропиленгликоля
        /// Канон — data/glycol_data.json (ADR-016); таблица списана с узлов канона
        /// (канон: 29 температур -34.4…121.1, шаг ~5.6; fallback-сетка — 9 узлов -34.4…98.9). NaN — нет данных в каноне
        /// </summary>
        private static double[,] DefaultPropyleneSpecificHeatValues()
        {
            // Данные из JSON: specific_heat_kJ_kgK
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10%,
                { double.NaN, double.NaN, 4.044, 4.07, 4.099, 4.124, 4.149, 4.178, 4.204 },
                // conc: 20%,
                { double.NaN, double.NaN, 3.927, 3.965, 4.003, 4.04, 4.074, 4.111, 4.149 },
                // conc: 30%,
                { double.NaN, double.NaN, 3.793, 3.839, 3.885, 3.931, 3.977, 4.024, 4.065 },
                // conc: 40%,
                { double.NaN, 3.58, 3.634, 3.689, 3.743, 3.802, 3.856, 3.91, 3.965 },
                // conc: 50%,
                { double.NaN, 3.387, 3.454, 3.517, 3.58, 3.647, 3.71, 3.776, 3.839 },
                // conc: 60%,
                { 3.102, 3.174, 3.249, 3.32, 3.395, 3.467, 3.542, 3.617, 3.689 },
                // conc: 70%,
                { 2.847, 2.931, 3.014, 3.098, 3.182, 3.262, 3.345, 3.429, 3.513 },
                // conc: 80%,
                { 2.575, 2.667, 2.763, 2.855, 2.948, 3.04, 3.132, 3.224, 3.316 },
                // conc: 90%
                { 2.269, 2.37, 2.474, 2.575, 2.675, 2.78, 2.881, 2.981, 3.086 },
            };
        }

        /// <summary>
        /// Fallback значения кинематической вязкости для пропиленгликоля
        /// Канон — data/glycol_data.json (ADR-016); таблица списана с узлов канона
        /// (канон: 29 температур -34.4…121.1, шаг ~5.6; fallback-сетка — 9 узлов -34.4…98.9). NaN — нет данных в каноне
        /// </summary>
        private static double[,] DefaultPropyleneViscosityValues()
        {
            // Данные из JSON: kinematic_viscosity_mm2_s
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10%,
                { double.NaN, double.NaN, 2.76, 1.58, 1.04, 0.75, 0.58, 0.45, 0.37 },
                // conc: 20%,
                { double.NaN, double.NaN, 4.12, 2.27, 1.41, 0.96, 0.71, 0.55, 0.44 },
                // conc: 30%,
                { double.NaN, double.NaN, 7.19, 3.51, 1.99, 1.28, 0.91, 0.68, 0.55 },
                // conc: 40%,
                { double.NaN, 38.88, 12.53, 5.35, 2.79, 1.7, 1.15, 0.85, 0.67 },
                // conc: 50%,
                { double.NaN, 58.27, 18.51, 7.73, 3.91, 2.3, 1.51, 1.07, 0.82 },
                // conc: 60%,
                { 463.27, 107.57, 31.75, 11.97, 5.54, 3.02, 1.88, 1.29, 0.95 },
                // conc: 70%,
                { 800.24, 165.48, 46.27, 16.9, 7.65, 4.09, 2.49, 1.68, 1.23 },
                // conc: 80%,
                { 1245.04, 280.6, 75.35, 25.62, 10.81, 5.45, 3.16, 2.06, 1.47 },
                // conc: 90%
                { 3251.94, 516.45, 124.15, 40.88, 17.02, 8.46, 4.81, 3.03, 2.06 },
            };
        }

        /// <summary>
        /// Fallback значения теплопроводности для пропиленгликоля
        /// Канон — data/glycol_data.json (ADR-016); таблица списана с узлов канона
        /// (канон: 29 температур -34.4…121.1, шаг ~5.6; fallback-сетка — 9 узлов -34.4…98.9). NaN — нет данных в каноне
        /// </summary>
        private static double[,] DefaultPropyleneConductivityValues()
        {
            // Данные из JSON: thermal_conductivity_W_mK
            // Строки соответствуют концентрациям, столбцы - температурам
            // Формат: values[c, t] - концентрация c, температура t
            // NaN означает отсутствие данных (точка замерзания выше температуры)
            return new double[,]
            {
                // conc: 10%,
                { double.NaN, double.NaN, double.NaN, 0.533, 0.556, 0.575, 0.588, 0.601, 0.606 },
                // conc: 20%,
                { double.NaN, double.NaN, 0.455, 0.478, 0.497, 0.512, 0.526, 0.535, 0.54 },
                // conc: 30%,
                { double.NaN, double.NaN, 0.408, 0.427, 0.443, 0.457, 0.467, 0.474, 0.478 },
                // conc: 40%,
                { double.NaN, 0.348, 0.365, 0.381, 0.395, 0.405, 0.414, 0.419, 0.422 },
                // conc: 50%,
                { double.NaN, 0.313, 0.325, 0.337, 0.35, 0.357, 0.363, 0.369, 0.37 },
                // conc: 60%,
                { 0.27, 0.28, 0.291, 0.299, 0.308, 0.313, 0.318, 0.322, 0.324 },
                // conc: 70%,
                { 0.242, 0.251, 0.258, 0.265, 0.27, 0.275, 0.279, 0.28, 0.28 },
                // conc: 80%,
                { 0.22, 0.227, 0.232, 0.237, 0.241, 0.242, 0.246, 0.246, 0.246 },
                // conc: 90%
                { 0.202, 0.206, 0.211, 0.213, 0.216, 0.218, 0.218, 0.218, 0.218 },
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