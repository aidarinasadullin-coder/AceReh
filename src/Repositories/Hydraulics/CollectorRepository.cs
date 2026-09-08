using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Repositories.Hydraulics
{
    /// <summary>
    /// Репозиторий для работы с данными о коллекторах РЕХАУ
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для работы с данными о коллекторах:
    /// - Получение списка коллекторов
    /// - Поиск по идентификатору
    /// - Фильтрация по типу
    /// - Подбор по количеству контуров
    /// 
    /// Данные загружаются из data/rehau_products.json
    /// 
    /// Поддерживаемые коллекторы:
    /// - HKV-D (бытовой): 2-12 контуров
    /// - IV (промышленный): DN25 (1¼"), DN40 (1½")
    /// </remarks>
    public class CollectorRepository : ICollectorRepository
    {
        private readonly string _dataFilePath;
        private List<Collector>? _cachedCollectors;
        private readonly object _lockObject = new();

        /// <summary>
        /// Создать экземпляр репозитория с путём к файлу данных по умолчанию
        /// </summary>
        public CollectorRepository() : this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "rehau_products.json"))
        {
        }

        /// <summary>
        /// Создать экземпляр репозитория с указанным путём к файлу данных
        /// </summary>
        /// <param name="dataFilePath">Путь к файлу JSON с данными</param>
        public CollectorRepository(string dataFilePath)
        {
            _dataFilePath = dataFilePath;
        }

        /// <summary>
        /// Получить все коллекторы
        /// </summary>
        /// <returns>Список всех коллекторов</returns>
        public async Task<IEnumerable<Collector>> GetAllAsync()
        {
            return await LoadDataAsync();
        }

        /// <summary>
        /// Получить коллектор по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор коллектора</param>
        /// <returns>Коллектор или null, если не найден</returns>
        public async Task<Collector?> GetByIdAsync(string id)
        {
            var collectors = await LoadDataAsync();
            return collectors.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Получить коллекторы по типу
        /// </summary>
        /// <param name="type">Тип коллектора (HKV или IV)</param>
        /// <returns>Список коллекторов указанного типа</returns>
        public async Task<IEnumerable<Collector>> GetByTypeAsync(CollectorType type)
        {
            var collectors = await LoadDataAsync();
            return collectors.Where(c => c.Type == type);
        }

        /// <summary>
        /// Получить коллектор по количеству контуров
        /// </summary>
        /// <param name="circuits">Количество контуров</param>
        /// <returns>Коллектор или null, если не найден</returns>
        public async Task<Collector?> GetByCircuitsAsync(int circuits)
        {
            var collectors = await LoadDataAsync();
            return collectors.FirstOrDefault(c => c.Circuits == circuits);
        }

        /// <summary>
        /// Подобрать коллектор для заданного количества контуров и расхода
        /// </summary>
        /// <param name="circuits">Количество контуров</param>
        /// <param name="totalFlowRate_m3_h">Суммарный расход, м³/ч</param>
        /// <returns>Рекомендуемый коллектор или null, если не найден</returns>
        public async Task<Collector?> SelectCollectorAsync(int circuits, double totalFlowRate_m3_h)
        {
            var collectors = await LoadDataAsync();

            // Фильтрация по количеству контуров
            var candidates = collectors
                .Where(c => c.Circuits >= circuits)
                .OrderBy(c => c.Circuits)
                .ToList();

            if (!candidates.Any())
            {
                return null;
            }

            // Проверка пропускной способности
            foreach (var collector in candidates)
            {
                if (collector.MaxFlowRate >= totalFlowRate_m3_h)
                {
                    return collector;
                }
            }

            // Если не нашли подходящий по расходу, вернуть первый с достаточным количеством контуров
            return candidates.FirstOrDefault();
        }

        /// <summary>
        /// Получить список доступных количеств контуров для HKV
        /// </summary>
        /// <returns>Список количеств контуров: 2-12</returns>
        public IEnumerable<int> GetAvailableCircuitCounts()
        {
            return new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        }

        /// <summary>
        /// Проверить, подходит ли коллектор для заданных параметров
        /// </summary>
        /// <param name="collector">Коллектор</param>
        /// <param name="circuits">Количество контуров</param>
        /// <param name="totalFlowRate_m3_h">Суммарный расход, м³/ч</param>
        /// <param name="pressure_mbar">Давление, мбар</param>
        /// <returns>true, если коллектор подходит</returns>
        public bool IsCollectorSuitable(
            Collector collector,
            int circuits,
            double totalFlowRate_m3_h,
            double pressure_mbar)
        {
            if (collector == null)
                return false;

            // Проверка количества контуров
            if (circuits > collector.Circuits)
                return false;

            // Проверка расхода
            if (totalFlowRate_m3_h > collector.MaxFlowRate)
                return false;

            // Проверка давления
            if (pressure_mbar > collector.MaxPressure)
                return false;

            return true;
        }

        /// <summary>
        /// Получить максимальное количество контуров для HKV
        /// </summary>
        /// <returns>Максимальное количество контуров (12)</returns>
        public int GetMaxCircuitsForHKV() => 12;

        /// <summary>
        /// Получить максимальный расход для HKV
        /// </summary>
        /// <returns>Максимальный расход, м³/ч (1.5)</returns>
        public double GetMaxFlowRateForHKV() => 1.5;

        /// <summary>
        /// Получить максимальное давление для HKV
        /// </summary>
        /// <returns>Максимальное давление, мбар (320)</returns>
        public double GetMaxPressureForHKV() => 320;

        #region Private Methods

        /// <summary>
        /// Загрузить данные из JSON файла (с кэшированием)
        /// </summary>
        private async Task<List<Collector>> LoadDataAsync()
        {
            lock (_lockObject)
            {
                if (_cachedCollectors != null)
                    return _cachedCollectors;
            }

            List<Collector> collectors;

            if (!File.Exists(_dataFilePath))
            {
                // Если файл не существует, вернуть встроенные данные
                collectors = GetDefaultCollectors();
                lock (_lockObject)
                {
                    _cachedCollectors = collectors;
                }
                return collectors;
            }

            try
            {
                string json = await File.ReadAllTextAsync(_dataFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var container = JsonSerializer.Deserialize<RehauProductsContainer>(json, options);

                if (container == null)
                {
                    collectors = GetDefaultCollectors();
                }
                else
                {
                    collectors = ConvertToCollectors(container);
                }

                lock (_lockObject)
                {
                    _cachedCollectors = collectors;
                }
            }
            catch (Exception)
            {
                // При ошибке парсинга используем встроенные данные
                collectors = GetDefaultCollectors();
                lock (_lockObject)
                {
                    _cachedCollectors = collectors;
                }
            }

            return collectors;
        }

        /// <summary>
        /// Конвертировать данные из JSON в список коллекторов
        /// </summary>
        private List<Collector> ConvertToCollectors(RehauProductsContainer container)
        {
            var collectors = new List<Collector>();

            // HKV коллекторы
            if (container.CollectorsHkv != null)
            {
                foreach (var hkv in container.CollectorsHkv)
                {
                    collectors.Add(new Collector
                    {
                        Id = hkv.Id ?? $"HKV_{hkv.Circuits}",
                        Name = hkv.Name ?? $"HKV {hkv.Circuits}",
                        FullName = hkv.FullName ?? $"Коллектор РЕХАУ HKV {hkv.Circuits} контуров",
                        Type = CollectorType.HKV,
                        Circuits = hkv.Circuits,
                        ConnectionSize = hkv.ConnectionSize ?? "1\"",
                        Kv = 1.2, // Стандартный Kv для HKV
                        MaxFlowRate = hkv.MaxFlowM3h,
                        MaxPressure = hkv.MaxPressureMbar,
                        MaxSetting = hkv.MaxSetting,
                        ArticleNumber = hkv.ArticleNumber,
                        Notes = hkv.Notes
                    });
                }
            }

            // Промышленные коллекторы
            if (container.CollectorsIndustrial != null)
            {
                foreach (var iv in container.CollectorsIndustrial)
                {
                    collectors.Add(new Collector
                    {
                        Id = iv.Id ?? "IV_1_1_4",
                        Name = iv.Name ?? "IV",
                        FullName = iv.FullName ?? "Промышленный коллектор",
                        Type = CollectorType.IV,
                        Circuits = 1, // Промышленные коллекторы обычно на 1 контур
                        ConnectionSize = iv.ConnectionSize ?? "1¼\"",
                        Kv = iv.ConnectionSize?.Contains("1½") == true ? 2.2 : 1.45,
                        MaxFlowRate = iv.MaxFlowM3h ?? 4.0,
                        MaxPressure = iv.MaxPressureMbar,
                        MaxSetting = iv.MaxSetting,
                        ArticleNumber = iv.ArticleNumber,
                        Notes = iv.Notes
                    });
                }
            }

            // Если данных нет, используем встроенные
            if (collectors.Count == 0)
            {
                return GetDefaultCollectors();
            }

            return collectors;
        }

        /// <summary>
        /// Получить встроенные данные о коллекторах РЕХАУ
        /// </summary>
        private static List<Collector> GetDefaultCollectors()
        {
            return new List<Collector>
            {
                // HKV коллекторы (для систем снеготаяния)
                new Collector
                {
                    Id = "HKV_2",
                    Name = "HKV 2",
                    FullName = "Коллектор РЕХАУ HKV 2",
                    Type = CollectorType.HKV,
                    Circuits = 2,
                    ConnectionSize = "1\"",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },
                new Collector
                {
                    Id = "HKV_3",
                    Name = "HKV 3",
                    FullName = "Коллектор РЕХАУ HKV 3",
                    Type = CollectorType.HKV,
                    Circuits = 3,
                    ConnectionSize = "1\"",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },
                new Collector
                {
                    Id = "HKV_4",
                    Name = "HKV 4",
                    FullName = "Коллектор РЕХАУ HKV 4",
                    Type = CollectorType.HKV,
                    Circuits = 4,
                    ConnectionSize = "1\"",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },
                new Collector
                {
                    Id = "HKV_5",
                    Name = "HKV 5",
                    FullName = "Коллектор РЕХАУ HKV 5",
                    Type = CollectorType.HKV,
                    Circuits = 5,
                    ConnectionSize = "1\"",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },
                new Collector
                {
                    Id = "HKV_6",
                    Name = "HKV 6",
                    FullName = "Коллектор РЕХАУ HKV 6",
                    Type = CollectorType.HKV,
                    Circuits = 6,
                    ConnectionSize = "1\"",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },
                new Collector
                {
                    Id = "HKV_7",
                    Name = "HKV 7",
                    FullName = "Коллектор РЕХАУ HKV 7",
                    Type = CollectorType.HKV,
                    Circuits = 7,
                    ConnectionSize = "1\"",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },
                new Collector
                {
                    Id = "HKV_8",
                    Name = "HKV 8",
                    FullName = "Коллектор РЕХАУ HKV 8",
                    Type = CollectorType.HKV,
                    Circuits = 8,
                    ConnectionSize = "1\"",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },
                new Collector
                {
                    Id = "HKV_9",
                    Name = "HKV 9",
                    FullName = "Коллектор РЕХАУ HKV 9",
                    Type = CollectorType.HKV,
                    Circuits = 9,
                    ConnectionSize = "1\"",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },
                new Collector
                {
                    Id = "HKV_10",
                    Name = "HKV 10",
                    FullName = "Коллектор РЕХАУ HKV 10",
                    Type = CollectorType.HKV,
                    Circuits = 10,
                    ConnectionSize = "1\"",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },
                new Collector
                {
                    Id = "HKV_11",
                    Name = "HKV 11",
                    FullName = "Коллектор РЕХАУ HKV 11",
                    Type = CollectorType.HKV,
                    Circuits = 11,
                    ConnectionSize = "1\"",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },
                new Collector
                {
                    Id = "HKV_12",
                    Name = "HKV 12",
                    FullName = "Коллектор РЕХАУ HKV 12",
                    Type = CollectorType.HKV,
                    Circuits = 12,
                    ConnectionSize = "1\"",
                    Kv = 1.2,
                    MaxFlowRate = 1.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },

                // IV коллекторы (промышленные)
                new Collector
                {
                    Id = "IV_1_1_4",
                    Name = "IV 1¼\"",
                    FullName = "Промышленный коллектор IV 1¼\"",
                    Type = CollectorType.IV,
                    Circuits = 1,
                    ConnectionSize = "1¼\"",
                    Kv = 1.45,
                    MaxFlowRate = 2.5,
                    MaxPressure = 320,
                    MaxSetting = 8
                },
                new Collector
                {
                    Id = "IV_1_1_2",
                    Name = "IV 1½\"",
                    FullName = "Промышленный коллектор IV 1½\"",
                    Type = CollectorType.IV,
                    Circuits = 1,
                    ConnectionSize = "1½\"",
                    Kv = 2.2,
                    MaxFlowRate = 4.0,
                    MaxPressure = 320,
                    MaxSetting = 8
                }
            };
        }

        #endregion

        #region JSON Data Models

        /// <summary>
        /// Контейнер данных из JSON
        /// </summary>
        internal class RehauProductsContainer
        {
            public List<CollectorHkvJson>? CollectorsHkv { get; set; }
            public List<CollectorIndustrialJson>? CollectorsIndustrial { get; set; }
        }

        /// <summary>
        /// Данные HKV коллектора из JSON
        /// </summary>
        internal class CollectorHkvJson
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? FullName { get; set; }
            public int Circuits { get; set; }
            public string? ConnectionSize { get; set; }
            public double MaxFlowM3h { get; set; }
            public double MaxPressureMbar { get; set; }
            public int MaxSetting { get; set; } = 8;
            public string? ArticleNumber { get; set; }
            public string? Notes { get; set; }
        }

        /// <summary>
        /// Данные промышленного коллектора из JSON
        /// </summary>
        internal class CollectorIndustrialJson
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? FullName { get; set; }
            public string? ConnectionSize { get; set; }
            public double? MinFlowM3h { get; set; }
            public double? MaxFlowM3h { get; set; }
            public double MaxPressureMbar { get; set; }
            public int MaxSetting { get; set; } = 8;
            public string? ArticleNumber { get; set; }
            public string? Notes { get; set; }
        }

        #endregion
    }
}