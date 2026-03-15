namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Результат расчёта контура для балансировки
    /// </summary>
    /// <remarks>
    /// Используется для расчёта дросселирования при балансировке
    /// нескольких контуров на одном коллекторе.
    /// 
    /// Алгоритм балансировки:
    /// 1. Определить контур с максимальными потерями (Δp_max)
    /// 2. Для каждого контура рассчитать дросселирование:
    ///    zu_drosseln = Δp_max - Δp_контур - Δp_вентиль
    /// 3. Определить настройку вентиля (1-8)
    /// </remarks>
    public class CircuitResult
    {
        /// <summary>
        /// Номер контура
        /// </summary>
        public int CircuitNumber { get; set; }
        
        /// <summary>
        /// Название/идентификатор контура
        /// </summary>
        public string? CircuitName { get; set; }
        
        /// <summary>
        /// Длина контура (L_HK), м
        /// </summary>
        public double Length { get; set; }
        
        /// <summary>
        /// Длина подводки (L_Zul), м
        /// </summary>
        public double SupplyLength { get; set; }
        
        /// <summary>
        /// Общая длина (L_total), м
        /// </summary>
        public double TotalLength => Length + SupplyLength;
        
        /// <summary>
        /// Площадь контура, м²
        /// </summary>
        public double Area { get; set; }
        
        /// <summary>
        /// Расход на контур (v), л/ч
        /// </summary>
        public double FlowRate { get; set; }
        
        /// <summary>
        /// Потери давления в трубе контура (Δp_HK), Па
        /// </summary>
        public double CircuitPipePressureLoss { get; set; }
        
        /// <summary>
        /// Потери давления в подводке (Δp_Zul), Па
        /// </summary>
        public double SupplyPipePressureLoss { get; set; }
        
        /// <summary>
        /// Общие потери давления в трубе (Δp_Rohr), Па
        /// </summary>
        public double TotalPipePressureLoss { get; set; }
        
        /// <summary>
        /// Потери давления в вентиле (Δp_Vent), Па
        /// </summary>
        public double ValvePressureLoss { get; set; }
        
        /// <summary>
        /// Суммарные потери давления (Δp_total), Па
        /// </summary>
        /// <remarks>
        /// Формула: Δp_total = Δp_Rohr + Δp_Vent
        /// </remarks>
        public double TotalPressureLoss { get; set; }
        
        /// <summary>
        /// Дросселирование для балансировки (zu_drosseln), Па
        /// </summary>
        /// <remarks>
        /// Рассчитывается относительно контура с максимальными потерями:
        /// zu_drosseln = Δp_max - Δp_контур - Δp_вентиль
        /// </remarks>
        public double Throttling { get; set; }
        
        /// <summary>
        /// Рекомендуемая настройка вентиля (1-8)
        /// </summary>
        /// <remarks>
        /// Определяется по таблице настроек вентиля:
        /// - Настройка 1: минимальное сопротивление
        /// - Настройка 8: максимальное сопротивление
        /// </remarks>
        public int RecommendedValveSetting { get; set; }
        
        /// <summary>
        /// Детальный результат гидравлического расчёта
        /// </summary>
        public HydraulicResult HydraulicResult { get; set; } = new();
        
        // === Вычисляемые свойства ===
        
        /// <summary>
        /// Суммарные потери в кПа
        /// </summary>
        public double TotalPressureLoss_kPa => TotalPressureLoss / 1000;
        
        /// <summary>
        /// Суммарные потери в мбар
        /// </summary>
        public double TotalPressureLoss_mbar => TotalPressureLoss / 100;
        
        /// <summary>
        /// Дросселирование в кПа
        /// </summary>
        public double Throttling_kPa => Throttling / 1000;
        
        /// <summary>
        /// Дросселирование в мбар
        /// </summary>
        public double Throttling_mbar => Throttling / 100;
        
        /// <summary>
        /// Признак того, что контур требует дросселирования
        /// </summary>
        public bool RequiresThrottling => Throttling > 0;
        
        /// <summary>
        /// Признак того, что контур является опорным (максимальные потери)
        /// </summary>
        public bool IsReferenceCircuit { get; set; }
        
        // === Методы ===
        
        /// <summary>
        /// Создать пустой результат
        /// </summary>
        public static CircuitResult Empty => new();
        
        /// <summary>
        /// Получить краткое описание контура
        /// </summary>
        public string GetSummary()
        {
            return $"Контур {CircuitNumber}: L={Length:F1}м, v={FlowRate:F1}л/ч, Δp={TotalPressureLoss_mbar:F1}мбар";
        }
        
        /// <summary>
        /// Получить информацию о балансировке
        /// </summary>
        public string GetBalancingInfo()
        {
            if (IsReferenceCircuit)
            {
                return $"Контур {CircuitNumber} — опорный (макс. потери)";
            }
            
            if (RequiresThrottling)
            {
                return $"Контур {CircuitNumber}: дросселирование {Throttling_mbar:F1}мбар, вентиль {RecommendedValveSetting}";
            }
            
            return $"Контур {CircuitNumber}: балансировка не требуется";
        }
    }
}