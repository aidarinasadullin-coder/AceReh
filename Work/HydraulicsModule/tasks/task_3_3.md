# Task 3.3: GlycolDataService (Сервис гликолей)

**Этап:** 3 - Services  
**Приоритет:** Высокий  
**Статус:** Не начато  
**Зависимости:** Task 2.2 (IGlycolDataService)

---

## 1. Цель задачи

Реализовать класс `GlycolDataService` для загрузки и интерполяции свойств гликолей из JSON.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-07 | Загрузка свойств теплоносителя | Все методы |

---

## 3. Создаваемые файлы

### 3.1. GlycolDataService.cs

**Путь:** `src/Services/Hydraulics/GlycolDataService.cs`

```csharp
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
    public class GlycolDataService : IGlycolDataService
    {
        private readonly string _dataFilePath;
        private GlycolDataContainer _cachedData;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Создать экземпляр сервиса с путём к файлу данных по умолчанию
        /// </summary>
        public GlycolDataService() : this("data/glycol_data.json")
        {
        }

        /// <summary>
        /// Создать экземпляр сервиса с указанным путём к файлу данных
        /// </summary>
        public GlycolDataService(string dataFilePath)
        {
            _dataFilePath = dataFilePath;
        }

        /// <summary>
        /// Получить все свойства гликолевого раствора
        /// </summary>
        public GlycolProperties GetProperties(GlycolType glycolType, double concentration, double temperature)
        {
            ValidateParameters(concentration, temperature);

            var data = LoadData();
            var glycolData = GetGlycolData(data, glycolType);

            double density = Interpolate2D(glycolData.Density, concentration, temperature);
            double specificHeat = Interpolate2D(glycolData.SpecificHeat, concentration, temperature);
            double kinematicViscosity = Interpolate2D(glycolData.KinematicViscosity, concentration, temperature);
            double thermalConductivity = Interpolate2D(glycolData.ThermalConductivity, concentration, temperature);

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
        public double GetDensity(GlycolType glycolType, double concentration, double temperature)
        {
            ValidateParameters(concentration, temperature);

            var data = LoadData();
            var glycolData = GetGlycolData(data, glycolType);

            return Interpolate2D(glycolData.Density, concentration, temperature);
        }

        /// <summary>
        /// Получить удельную теплоёмкость гликолевого раствора (кДж/(кг·К))
        /// </summary>
        public double GetSpecificHeat(GlycolType glycolType, double concentration, double temperature)
        {
            ValidateParameters(concentration, temperature);

            var data = LoadData();
            var glycolData = GetGlycolData(data, glycolType);

            return Interpolate2D(glycolData.SpecificHeat, concentration, temperature);
        }

        /// <summary>
        /// Получить кинематическую вязкость гликолевого раствора (мм²/с)
        /// </summary>
        public double GetKinematicViscosity(GlycolType glycolType, double concentration, double temperature)
        {
            ValidateParameters(concentration, temperature);

            var data = LoadData();
            var glycolData = GetGlycolData(data, glycolType);

            return Interpolate2D(glycolData.KinematicViscosity, concentration, temperature);
        }

        /// <summary>
        /// Получить теплопроводность гликолевого раствора (Вт/(м·К))
        /// </summary>
        public double GetThermalConductivity(GlycolType glycolType, double concentration, double temperature)
        {
            ValidateParameters(concentration, temperature);

            var data = LoadData();
            var glycolData = GetGlycolData(data, glycolType);

            return Interpolate2D(glycolData.ThermalConductivity, concentration, temperature);
        }

        /// <summary>
        /// Получить доступные концентрации гликолей
        /// </summary>
        public double[] GetAvailableConcentrations()
        {
            var data = LoadData();
            return data.Concentrations;
        }

        /// <summary>
        /// Получить доступные температуры
        /// </summary>
        public double[] GetAvailableTemperatures()
        {
            var data = LoadData();
            return data.Temperatures;
        }

        #region Private Methods

        /// <summary>
        /// Загрузить данные из JSON файла (с кэшированием)
        /// </summary>
        private GlycolDataContainer LoadData()
        {
            lock (_lockObject)
            {
                if (_cachedData != null)
                    return _cachedData;

                if (!File.Exists(_dataFilePath))
                {
                    throw new FileNotFoundException($"Файл данных гликолей не найден: {_dataFilePath}");
                }

                string json = File.ReadAllText(_dataFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                _cachedData = JsonSerializer.Deserialize<GlycolDataContainer>(json, options);

                if (_cachedData == null)
                {
                    throw new InvalidDataException("Не удалось загрузить данные гликолей из файла");
                }

                return _cachedData;
            }
        }

        /// <summary>
        /// Получить данные для конкретного типа гликоли
        /// </summary>
        private GlycolData GetGlycolData(GlycolDataContainer container, GlycolType glycolType)
        {
            return glycolType switch
            {
                GlycolType.Ethylene => container.EthyleneGlycol,
                GlycolType.Propylene => container.PropyleneGlycol,
                _ => throw new ArgumentException($"Неподдерживаемый тип гликоли: {glycolType}")
            };
        }

        /// <summary>
        /// Билинейная интерполяция по концентрации и температуре
        /// </summary>
        private double Interpolate2D(GlycolDataTable table, double concentration, double temperature)
        {
            double[] concentrations = table.Concentrations;
            double[] temperatures = table.Temperatures;
            double[,] values = table.Values;

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
                double t1 = temperatures[tLow];
                double t2 = temperatures[tHigh];
                double v1 = values[cLow, tLow];
                double v2 = values[cLow, tHigh];

                return LinearInterpolate(t1, t2, v1, v2, temperature);
            }

            if (tLow == tHigh)
            {
                // Интерполяция только по концентрации
                double c1 = concentrations[cLow];
                double c2 = concentrations[cHigh];
                double v1 = values[cLow, tLow];
                double v2 = values[cHigh, tLow];

                return LinearInterpolate(c1, c2, v1, v2, concentration);
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
            double v1 = LinearInterpolate(t1, t2, v11, v12, temperature);
            double v2 = LinearInterpolate(t1, t2, v21, v22, temperature);

            // Интерполяция по концентрации
            return LinearInterpolate(c1, c2, v1, v2, concentration);
        }

        /// <summary>
        /// Линейная интерполяция между двумя точками
        /// </summary>
        private double LinearInterpolate(double x1, double x2, double y1, double y2, double x)
        {
            if (Math.Abs(x2 - x1) < 1e-10)
                return y1;

            double ratio = (x - x1) / (x2 - x1);
            return y1 + ratio * (y2 - y1);
        }

        /// <summary>
        /// Найти индекс ближайшего меньшего значения
        /// </summary>
        private int FindLowerIndex(double[] array, double value)
        {
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
            if (concentration < 10 || concentration > 90)
            {
                throw new ArgumentOutOfRangeException(nameof(concentration),
                    $"Концентрация должна быть в диапазоне 10-90%, получено: {concentration}%");
            }

            if (temperature < -34.4 || temperature > 98.9)
            {
                throw new ArgumentOutOfRangeException(nameof(temperature),
                    $"Температура должна быть в диапазоне -34.4°C до 98.9°C, получено: {temperature}°C");
            }
        }

        #endregion
    }
}
```

### 3.2. Модели данных для JSON

**Путь:** `src/Models/Hydraulics/GlycolDataModels.cs`

```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Контейнер данных гликолей из JSON
    /// </summary>
    public class GlycolDataContainer
    {
        public double[] Concentrations { get; set; }
        public double[] Temperatures { get; set; }
        public GlycolData EthyleneGlycol { get; set; }
        public GlycolData PropyleneGlycol { get; set; }
    }

    /// <summary>
    /// Данные для конкретного типа гликоли
    /// </summary>
    public class GlycolData
    {
        public double[] Concentrations { get; set; }
        public double[] Temperatures { get; set; }
        public GlycolDataTable Density { get; set; }
        public GlycolDataTable SpecificHeat { get; set; }
        public GlycolDataTable KinematicViscosity { get; set; }
        public GlycolDataTable ThermalConductivity { get; set; }
    }

    /// <summary>
    /// Таблица значений для билинейной интерполяции
    /// </summary>
    public class GlycolDataTable
    {
        public double[] Concentrations { get; set; }
        public double[] Temperatures { get; set; }
        public double[,] Values { get; set; }
    }
}
```

### 3.3. Пример JSON файла данных

**Путь:** `data/glycol_data.json`

```json
{
  "concentrations": [10, 20, 30, 40, 50, 60, 70, 80, 90],
  "temperatures": [-34.4, -20, -10, 0, 10, 20, 30, 40, 50, 60, 70, 80, 98.9],
  "ethyleneGlycol": {
    "density": {
      "concentrations": [10, 20, 30, 40, 50, 60, 70, 80, 90],
      "temperatures": [-34.4, -20, -10, 0, 10, 20, 30, 40, 50, 60, 70, 80, 98.9],
      "values": [
        [1021, 1025, 1028, 1030, 1032, 1033, 1033, 1032, 1030, 1027, 1023, 1018, 1010],
        [1035, 1040, 1044, 1047, 1050, 1052, 1053, 1053, 1052, 1050, 1046, 1041, 1032],
        [1048, 1055, 1060, 1065, 1068, 1071, 1073, 1073, 1072, 1070, 1066, 1061, 1051],
        [1060, 1069, 1076, 1082, 1087, 1090, 1092, 1093, 1092, 1090, 1086, 1081, 1070],
        [1070, 1081, 1090, 1098, 1104, 1108, 1111, 1112, 1112, 1110, 1106, 1100, 1088],
        [1078, 1091, 1102, 1112, 1119, 1125, 1129, 1131, 1131, 1129, 1125, 1119, 1106],
        [1084, 1099, 1112, 1124, 1133, 1140, 1145, 1148, 1149, 1147, 1143, 1137, 1123],
        [1088, 1105, 1120, 1134, 1145, 1154, 1161, 1165, 1167, 1166, 1162, 1156, 1141],
        [1090, 1109, 1126, 1142, 1155, 1166, 1174, 1180, 1183, 1183, 1179, 1173, 1158]
      ]
    },
    "specificHeat": {
      "concentrations": [10, 20, 30, 40, 50, 60, 70, 80, 90],
      "temperatures": [-34.4, -20, -10, 0, 10, 20, 30, 40, 50, 60, 70, 80, 98.9],
      "values": [
        [3.89, 3.95, 3.99, 4.02, 4.05, 4.07, 4.09, 4.11, 4.13, 4.14, 4.15, 4.16, 4.17],
        [3.72, 3.78, 3.82, 3.86, 3.89, 3.92, 3.94, 3.96, 3.98, 3.99, 4.00, 4.01, 4.02],
        [3.55, 3.61, 3.66, 3.70, 3.74, 3.77, 3.80, 3.82, 3.84, 3.85, 3.86, 3.87, 3.88],
        [3.39, 3.45, 3.50, 3.54, 3.58, 3.62, 3.65, 3.67, 3.69, 3.71, 3.72, 3.73, 3.74],
        [3.22, 3.29, 3.34, 3.39, 3.43, 3.47, 3.50, 3.53, 3.55, 3.57, 3.58, 3.59, 3.60],
        [3.06, 3.13, 3.18, 3.23, 3.28, 3.32, 3.36, 3.39, 3.41, 3.43, 3.45, 3.46, 3.47],
        [2.89, 2.97, 3.03, 3.08, 3.13, 3.18, 3.22, 3.25, 3.28, 3.30, 3.32, 3.33, 3.34],
        [2.73, 2.81, 2.87, 2.93, 2.99, 3.04, 3.08, 3.12, 3.15, 3.17, 3.19, 3.21, 3.22],
        [2.56, 2.65, 2.72, 2.78, 2.84, 2.89, 2.94, 2.98, 3.01, 3.04, 3.06, 3.08, 3.09]
      ]
    },
    "kinematicViscosity": {
      "concentrations": [10, 20, 30, 40, 50, 60, 70, 80, 90],
      "temperatures": [-34.4, -20, -10, 0, 10, 20, 30, 40, 50, 60, 70, 80, 98.9],
      "values": [
        [15.2, 5.8, 3.5, 2.3, 1.6, 1.2, 0.9, 0.7, 0.6, 0.5, 0.4, 0.4, 0.3],
        [35.4, 11.2, 6.0, 3.6, 2.4, 1.7, 1.3, 1.0, 0.8, 0.7, 0.6, 0.5, 0.4],
        [72.8, 20.5, 10.0, 5.6, 3.5, 2.4, 1.7, 1.3, 1.0, 0.8, 0.7, 0.6, 0.5],
        [140.0, 35.2, 15.8, 8.2, 4.8, 3.1, 2.2, 1.6, 1.2, 1.0, 0.8, 0.7, 0.5],
        [248.0, 56.5, 23.5, 11.5, 6.4, 4.0, 2.7, 1.9, 1.5, 1.2, 0.9, 0.8, 0.6],
        [410.0, 85.0, 33.0, 15.5, 8.3, 5.0, 3.3, 2.3, 1.7, 1.4, 1.1, 0.9, 0.7],
        [640.0, 122.0, 45.0, 20.0, 10.5, 6.2, 4.0, 2.8, 2.0, 1.5, 1.2, 1.0, 0.8],
        [950.0, 170.0, 60.0, 25.5, 13.0, 7.5, 4.8, 3.3, 2.4, 1.8, 1.4, 1.1, 0.9],
        [1350.0, 230.0, 78.0, 32.0, 16.0, 9.0, 5.7, 3.9, 2.8, 2.1, 1.6, 1.3, 1.0]
      ]
    },
    "thermalConductivity": {
      "concentrations": [10, 20, 30, 40, 50, 60, 70, 80, 90],
      "temperatures": [-34.4, -20, -10, 0, 10, 20, 30, 40, 50, 60, 70, 80, 98.9],
      "values": [
        [0.48, 0.50, 0.51, 0.52, 0.53, 0.54, 0.55, 0.56, 0.57, 0.58, 0.59, 0.60, 0.61],
        [0.45, 0.47, 0.48, 0.49, 0.50, 0.51, 0.52, 0.53, 0.54, 0.55, 0.56, 0.57, 0.58],
        [0.42, 0.44, 0.45, 0.46, 0.47, 0.48, 0.49, 0.50, 0.51, 0.52, 0.53, 0.54, 0.55],
        [0.39, 0.41, 0.42, 0.43, 0.44, 0.45, 0.46, 0.47, 0.48, 0.49, 0.50, 0.51, 0.52],
        [0.36, 0.38, 0.39, 0.40, 0.41, 0.42, 0.43, 0.44, 0.45, 0.46, 0.47, 0.48, 0.49],
        [0.33, 0.35, 0.36, 0.37, 0.38, 0.39, 0.40, 0.41, 0.42, 0.43, 0.44, 0.45, 0.46],
        [0.30, 0.32, 0.33, 0.34, 0.35, 0.36, 0.37, 0.38, 0.39, 0.40, 0.41, 0.42, 0.43],
        [0.27, 0.29, 0.30, 0.31, 0.32, 0.33, 0.34, 0.35, 0.36, 0.37, 0.38, 0.39, 0.40],
        [0.24, 0.26, 0.27, 0.28, 0.29, 0.30, 0.31, 0.32, 0.33, 0.34, 0.35, 0.36, 0.37]
      ]
    }
  },
  "propyleneGlycol": {
    "density": {
      "concentrations": [10, 20, 30, 40, 50, 60, 70, 80, 90],
      "temperatures": [-34.4, -20, -10, 0, 10, 20, 30, 40, 50, 60, 70, 80, 98.9],
      "values": [
        [1020, 1023, 1025, 1027, 1028, 1029, 1029, 1028, 1026, 1024, 1021, 1017, 1009],
        [1032, 1037, 1040, 1043, 1045, 1047, 1048, 1048, 1047, 1045, 1042, 1038, 1029],
        [1043, 1050, 1055, 1059, 1062, 1065, 1066, 1067, 1066, 1064, 1061, 1056, 1046],
        [1053, 1062, 1069, 1075, 1080, 1083, 1085, 1086, 1085, 1083, 1079, 1074, 1063],
        [1061, 1072, 1082, 1090, 1096, 1101, 1104, 1105, 1105, 1103, 1099, 1094, 1082],
        [1067, 1081, 1093, 1103, 1111, 1117, 1121, 1123, 1124, 1122, 1118, 1113, 1100],
        [1072, 1088, 1102, 1114, 1124, 1132, 1138, 1141, 1142, 1141, 1137, 1131, 1118],
        [1075, 1093, 1109, 1123, 1135, 1145, 1152, 1157, 1159, 1158, 1155, 1149, 1135],
        [1077, 1097, 1115, 1131, 1145, 1157, 1166, 1172, 1175, 1175, 1172, 1166, 1152]
      ]
    },
    "specificHeat": {
      "concentrations": [10, 20, 30, 40, 50, 60, 70, 80, 90],
      "temperatures": [-34.4, -20, -10, 0, 10, 20, 30, 40, 50, 60, 70, 80, 98.9],
      "values": [
        [3.92, 3.98, 4.02, 4.05, 4.08, 4.10, 4.12, 4.14, 4.16, 4.17, 4.18, 4.19, 4.20],
        [3.78, 3.84, 3.88, 3.92, 3.95, 3.98, 4.00, 4.02, 4.04, 4.05, 4.06, 4.07, 4.08],
        [3.64, 3.70, 3.75, 3.79, 3.83, 3.86, 3.89, 3.91, 3.93, 3.94, 3.95, 3.96, 3.97],
        [3.50, 3.56, 3.61, 3.66, 3.70, 3.74, 3.77, 3.80, 3.82, 3.84, 3.85, 3.86, 3.87],
        [3.35, 3.42, 3.48, 3.53, 3.58, 3.62, 3.66, 3.69, 3.71, 3.73, 3.75, 3.76, 3.77],
        [3.21, 3.28, 3.34, 3.40, 3.45, 3.50, 3.54, 3.57, 3.60, 3.62, 3.64, 3.66, 3.67],
        [3.07, 3.14, 3.21, 3.27, 3.33, 3.38, 3.42, 3.46, 3.49, 3.52, 3.54, 3.55, 3.57],
        [2.92, 3.00, 3.07, 3.14, 3.20, 3.25, 3.30, 3.34, 3.38, 3.41, 3.43, 3.45, 3.46],
        [2.78, 2.86, 2.94, 3.01, 3.08, 3.13, 3.18, 3.23, 3.27, 3.30, 3.33, 3.35, 3.36]
      ]
    },
    "kinematicViscosity": {
      "concentrations": [10, 20, 30, 40, 50, 60, 70, 80, 90],
      "temperatures": [-34.4, -20, -10, 0, 10, 20, 30, 40, 50, 60, 70, 80, 98.9],
      "values": [
        [18.5, 6.8, 4.0, 2.6, 1.8, 1.3, 1.0, 0.8, 0.6, 0.5, 0.4, 0.4, 0.3],
        [48.0, 14.5, 7.5, 4.3, 2.8, 1.9, 1.4, 1.1, 0.9, 0.7, 0.6, 0.5, 0.4],
        [110.0, 28.0, 13.0, 7.0, 4.2, 2.7, 1.9, 1.4, 1.1, 0.9, 0.7, 0.6, 0.5],
        [240.0, 52.0, 22.0, 11.0, 6.2, 3.8, 2.5, 1.8, 1.3, 1.0, 0.8, 0.7, 0.5],
        [480.0, 95.0, 37.0, 17.5, 9.2, 5.3, 3.4, 2.3, 1.7, 1.3, 1.0, 0.8, 0.6],
        [880.0, 160.0, 58.0, 26.0, 13.0, 7.2, 4.5, 3.0, 2.1, 1.6, 1.2, 1.0, 0.7],
        [1500.0, 255.0, 88.0, 37.0, 18.0, 9.8, 5.9, 3.9, 2.7, 2.0, 1.5, 1.2, 0.9],
        [2400.0, 390.0, 130.0, 52.0, 24.0, 13.0, 7.6, 5.0, 3.5, 2.5, 1.9, 1.5, 1.1],
        [3600.0, 560.0, 180.0, 70.0, 31.0, 16.5, 9.5, 6.2, 4.3, 3.1, 2.3, 1.8, 1.3]
      ]
    },
    "thermalConductivity": {
      "concentrations": [10, 20, 30, 40, 50, 60, 70, 80, 90],
      "temperatures": [-34.4, -20, -10, 0, 10, 20, 30, 40, 50, 60, 70, 80, 98.9],
      "values": [
        [0.47, 0.49, 0.50, 0.51, 0.52, 0.53, 0.54, 0.55, 0.56, 0.57, 0.58, 0.59, 0.60],
        [0.44, 0.46, 0.47, 0.48, 0.49, 0.50, 0.51, 0.52, 0.53, 0.54, 0.55, 0.56, 0.57],
        [0.41, 0.43, 0.44, 0.45, 0.46, 0.47, 0.48, 0.49, 0.50, 0.51, 0.52, 0.53, 0.54],
        [0.38, 0.40, 0.41, 0.42, 0.43, 0.44, 0.45, 0.46, 0.47, 0.48, 0.49, 0.50, 0.51],
        [0.35, 0.37, 0.38, 0.39, 0.40, 0.41, 0.42, 0.43, 0.44, 0.45, 0.46, 0.47, 0.48],
        [0.32, 0.34, 0.35, 0.36, 0.37, 0.38, 0.39, 0.40, 0.41, 0.42, 0.43, 0.44, 0.45],
        [0.29, 0.31, 0.32, 0.33, 0.34, 0.35, 0.36, 0.37, 0.38, 0.39, 0.40, 0.41, 0.42],
        [0.26, 0.28, 0.29, 0.30, 0.31, 0.32, 0.33, 0.34, 0.35, 0.36, 0.37, 0.38, 0.39],
        [0.23, 0.25, 0.26, 0.27, 0.28, 0.29, 0.30, 0.31, 0.32, 0.33, 0.34, 0.35, 0.36]
      ]
    }
  }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Services/Hydraulics/GlycolDataServiceTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;
using System;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    [TestFixture]
    public class GlycolDataServiceTests
    {
        private GlycolDataService _service;

        [SetUp]
        public void Setup()
        {
            _service = new GlycolDataService("data/glycol_data.json");
        }

        [Test]
        public void GetDensity_EthyleneGlycol50Percent_20C_ReturnsCorrectValue()
        {
            // Arrange
            double concentration = 50;
            double temperature = 20;

            // Act
            double density = _service.GetDensity(GlycolType.Ethylene, concentration, temperature);

            // Assert
            // При 50% и 20°C плотность этиленгликоля ≈ 1108 кг/м³
            Assert.That(density, Is.EqualTo(1108).Within(5));
        }

        [Test]
        public void GetKinematicViscosity_EthyleneGlycol50Percent_20C_ReturnsCorrectValue()
        {
            // Arrange
            double concentration = 50;
            double temperature = 20;

            // Act
            double viscosity = _service.GetKinematicViscosity(GlycolType.Ethylene, concentration, temperature);

            // Assert
            // При 50% и 20°C вязкость ≈ 4.0 мм²/с
            Assert.That(viscosity, Is.EqualTo(4.0).Within(0.5));
        }

        [Test]
        public void GetProperties_ReturnsAllProperties()
        {
            // Arrange
            double concentration = 40;
            double temperature = 30;

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert
            Assert.That(properties.Density, Is.GreaterThan(1000));
            Assert.That(properties.SpecificHeat, Is.GreaterThan(3.0));
            Assert.That(properties.KinematicViscosity, Is.GreaterThan(0));
            Assert.That(properties.ThermalConductivity, Is.GreaterThan(0));
        }

        [Test]
        public void GetProperties_InterpolationBetweenTemperatures()
        {
            // Arrange
            double concentration = 50;
            double temperature = 25; // Между 20 и 30

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert
            // Значение должно быть между значениями при 20°C и 30°C
            var props20 = _service.GetProperties(GlycolType.Ethylene, concentration, 20);
            var props30 = _service.GetProperties(GlycolType.Ethylene, concentration, 30);

            Assert.That(properties.Density, Is.Between(props20.Density - 1, props30.Density + 1));
        }

        [Test]
        public void GetProperties_InterpolationBetweenConcentrations()
        {
            // Arrange
            double concentration = 45; // Между 40 и 50
            double temperature = 20;

            // Act
            var properties = _service.GetProperties(GlycolType.Ethylene, concentration, temperature);

            // Assert
            var props40 = _service.GetProperties(GlycolType.Ethylene, 40, temperature);
            var props50 = _service.GetProperties(GlycolType.Ethylene, 50, temperature);

            Assert.That(properties.Density, Is.Between(props40.Density, props50.Density));
        }

        [Test]
        public void GetProperties_PropyleneGlycol_ReturnsCorrectValues()
        {
            // Arrange
            double concentration = 50;
            double temperature = 20;

            // Act
            var properties = _service.GetProperties(GlycolType.Propylene, concentration, temperature);

            // Assert
            // Пропиленгликоль имеет меньшую плотность и большую вязкость
            Assert.That(properties.Density, Is.GreaterThan(1090));
            Assert.That(properties.KinematicViscosity, Is.GreaterThan(4.0));
        }

        [Test]
        public void GetProperties_InvalidConcentration_ThrowsException()
        {
            // Arrange
            double concentration = 5; // Меньше минимума (10%)
            double temperature = 20;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetProperties(GlycolType.Ethylene, concentration, temperature));
        }

        [Test]
        public void GetProperties_InvalidTemperature_ThrowsException()
        {
            // Arrange
            double concentration = 50;
            double temperature = -40; // Меньше минимума (-34.4°C)

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _service.GetProperties(GlycolType.Ethylene, concentration, temperature));
        }

        [Test]
        public void GetAvailableConcentrations_ReturnsArray()
        {
            // Act
            var concentrations = _service.GetAvailableConcentrations();

            // Assert
            Assert.That(concentrations, Is.Not.Null);
            Assert.That(concentrations.Length, Is.GreaterThan(0));
            Assert.That(concentrations, Contains.Item(10.0));
            Assert.That(concentrations, Contains.Item(50.0));
            Assert.That(concentrations, Contains.Item(90.0));
        }

        [Test]
        public void GetAvailableTemperatures_ReturnsArray()
        {
            // Act
            var temperatures = _service.GetAvailableTemperatures();

            // Assert
            Assert.That(temperatures, Is.Not.Null);
            Assert.That(temperatures.Length, Is.GreaterThan(0));
            Assert.That(temperatures[0], Is.EqualTo(-34.4).Within(0.1));
            Assert.That(temperatures[temperatures.Length - 1], Is.EqualTo(98.9).Within(0.1));
        }
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `GlycolDataService.cs` создан
- [ ] Реализован интерфейс `IGlycolDataService`
- [ ] Билинейная интерполяция работает корректно
- [ ] Данные загружаются из JSON
- [ ] Кэширование данных работает
- [ ] Валидация параметров реализована
- [ ] Unit-тесты проходят успешно
- [ ] XML-документация для всех методов

---

## 6. Примечания

- Диапазон температур: -34.4°C до 98.9°C
- Диапазон концентраций: 10% до 90%
- Поддержка этиленгликоля и пропиленгликоля
- Кэширование данных в памяти для производительности
- Билинейная интерполяция обеспечивает точность расчётов