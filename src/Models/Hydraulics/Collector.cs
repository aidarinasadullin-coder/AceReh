namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Коллектор РЕХАУ для систем снеготаяния
    /// </summary>
    /// <remarks>
    /// Модель содержит технические характеристики коллекторов:
    /// - Бытовые коллекторы HKV-D (2-12 контуров)
    /// - Промышленные коллекторы IV (DN25, DN40)
    /// 
    /// Данные загружаются из data/rehau_products.json
    /// </remarks>
    public class Collector
    {
        /// <summary>
        /// Идентификатор коллектора
        /// </summary>
        /// <remarks>
        /// Формат: "HKV-D-2", "HKV-D-4", ..., "IV-1.25", "IV-1.5"
        /// </remarks>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Краткое название коллектора
        /// </summary>
        /// <example>HKV-D 4</example>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Полное название коллектора
        /// </summary>
        /// <example>Коллектор HKV-D 4 контура</example>
        public string FullName { get; set; } = string.Empty;
        
        /// <summary>
        /// Тип коллектора
        /// </summary>
        /// <remarks>
        /// HKV — бытовой коллектор
        /// IV — промышленный коллектор
        /// </remarks>
        public CollectorType Type { get; set; }
        
        /// <summary>
        /// Количество контуров
        /// </summary>
        /// <remarks>
        /// Для HKV-D: 2, 4, 6, 8, 10, 12
        /// Для IV: определяется размером подключения
        /// </remarks>
        public int Circuits { get; set; }
        
        /// <summary>
        /// Размер подключения
        /// </summary>
        /// <example>1¼", 1½"</example>
        public string ConnectionSize { get; set; } = string.Empty;
        
        /// <summary>
        /// Коэффициент пропускной способности вентиля (Kv), м³/ч
        /// </summary>
        /// <remarks>
        /// Используется для расчёта потерь давления в вентиле:
        /// - HKV-D: Kv = 1.2 м³/ч
        /// - IV 1¼": Kv = 1.45 м³/ч
        /// - IV 1½": Kv = 1.5 м³/ч
        /// </remarks>
        public double Kv { get; set; }
        
        /// <summary>
        /// Максимальный расход через коллектор, м³/ч
        /// </summary>
        /// <remarks>
        /// Для HKV-D: 1.5 м³/ч
        /// </remarks>
        public double MaxFlowRate { get; set; }
        
        /// <summary>
        /// Максимальное давление, мбар
        /// </summary>
        /// <remarks>
        /// Для HKV-D: 320 мбар
        /// </remarks>
        public double MaxPressure { get; set; }
        
        /// <summary>
        /// Максимальная настройка вентиля
        /// </summary>
        /// <remarks>
        /// Диапазон: 1-8
        /// </remarks>
        public int MaxSetting { get; set; } = 8;
        
        /// <summary>
        /// Артикул РЕХАУ
        /// </summary>
        public string? ArticleNumber { get; set; }
        
        /// <summary>
        /// Примечания
        /// </summary>
        public string? Notes { get; set; }
        
        // === Вычисляемые свойства ===
        
        /// <summary>
        /// Признак бытового коллектора
        /// </summary>
        public bool IsResidential => Type == CollectorType.HKV;
        
        /// <summary>
        /// Признак промышленного коллектора
        /// </summary>
        public bool IsIndustrial => Type == CollectorType.IV;
        
        /// <summary>
        /// Максимальное давление в Па
        /// </summary>
        public double MaxPressure_Pa => MaxPressure * 100;
        
        /// <summary>
        /// Максимальный расход в л/ч
        /// </summary>
        public double MaxFlowRate_L_h => MaxFlowRate * 1000;
        
        // === Методы ===
        
        /// <summary>
        /// Проверить, подходит ли коллектор для заданного количества контуров
        /// </summary>
        /// <param name="circuitCount">Количество контуров</param>
        /// <returns>true, если подходит</returns>
        public bool IsSuitableForCircuits(int circuitCount)
        {
            if (Type == CollectorType.HKV)
            {
                return circuitCount >= 2 && circuitCount <= Circuits;
            }
            
            // Для промышленных коллекторов проверка по расходу
            return true;
        }
        
        /// <summary>
        /// Проверить, подходит ли коллектор для заданного расхода
        /// </summary>
        /// <param name="flowRate_m3_h">Расход, м³/ч</param>
        /// <returns>true, если подходит</returns>
        public bool IsSuitableForFlowRate(double flowRate_m3_h)
        {
            return flowRate_m3_h <= MaxFlowRate;
        }
        
        /// <summary>
        /// Проверить, подходит ли коллектор для заданного давления
        /// </summary>
        /// <param name="pressure_mbar">Давление, мбар</param>
        /// <returns>true, если подходит</returns>
        public bool IsSuitableForPressure(double pressure_mbar)
        {
            return pressure_mbar <= MaxPressure;
        }
        
        /// <summary>
        /// Получить описание коллектора
        /// </summary>
        public string GetDescription()
        {
            return $"{FullName}, {Circuits} конт., Kv={Kv} м³/ч, макс. расход {MaxFlowRate} м³/ч, макс. давление {MaxPressure} мбар";
        }
    }
}