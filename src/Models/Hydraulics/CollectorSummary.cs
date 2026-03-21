using System;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Итоги расчёта коллектора
    /// </summary>
    public class CollectorSummary
    {
        /// <summary>
        /// Номер коллектора
        /// </summary>
        public int CollectorNumber { get; set; }
        
        /// <summary>
        /// Тип коллектора
        /// </summary>
        public string CollectorType { get; set; } = "HKV-D";
        
        /// <summary>
        /// Kv коллектора (коэффициент пропускной способности), м³/ч
        /// </summary>
        /// <remarks>
        /// HKV-D: 1.2
        /// IV 1¼": 1.45
        /// IV 1½": 1.5
        /// </remarks>
        public double Kv { get; set; } = 1.2;
        
        /// <summary>
        /// Количество контуров
        /// </summary>
        public int CircuitCount { get; set; }
        
        /// <summary>
        /// Общая длина труб, м
        /// </summary>
        public double TotalPipeLength { get; set; }
        
        /// <summary>
        /// Общая мощность, Вт
        /// </summary>
        public double TotalPower { get; set; }
        
        /// <summary>
        /// Общий расход, л/ч
        /// </summary>
        public double TotalFlowRate { get; set; }
        
        /// <summary>
        /// Общий расход, м³/ч
        /// </summary>
        public double TotalFlowRate_m3h => TotalFlowRate / 1000.0;
        
        /// <summary>
        /// Потери давления при рабочей температуре, мбар
        /// </summary>
        public double PressureLoss_Operating_mbar { get; set; }
        
        /// <summary>
        /// Потери давления при расчётной (холодной) температуре, мбар
        /// </summary>
        public double PressureLoss_Cold_mbar { get; set; }
        
        /// <summary>
        /// Максимальные потери давления контура (референсный контур), Па
        /// </summary>
        public double MaxCircuitLoss { get; set; }
        
        /// <summary>
        /// Референсный контур (с максимальными потерями)
        /// </summary>
        public int ReferenceCircuitNumber { get; set; }
        
        /// <summary>
        /// Признак валидности
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Предупреждения
        /// </summary>
        public string[] Warnings { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Предупреждение о превышении расхода
        /// </summary>
        /// <remarks>
        /// Устанавливается при автоматическом выборе коллектора, если расход > 4.0 м³/ч
        /// </remarks>
        public string? Warning { get; set; }
        
        // === Новое свойство ===
        
        /// <summary>
        /// Тип балансировочного клапана
        /// </summary>
        /// <remarks>
        /// Определяет формулу расчёта оборотов клапана:
        /// - HKV-D: бытовой коллектор, Kv = 1.2 м³/ч
        /// - IV 1¼": промышленный коллектор, Kv = 1.45 м³/ч
        /// - IV 1½": промышленный коллектор, Kv = 1.5 м³/ч
        /// </remarks>
        public ValveType ValveType { get; set; } = ValveType.HKV_D;
        
        // === Вычисляемые свойства ===
        
        /// <summary>
        /// Потери давления при рабочей температуре, Па
        /// </summary>
        public double PressureLoss_Operating_Pa => PressureLoss_Operating_mbar * 100;
        
        /// <summary>
        /// Потери давления при расчётной температуре, Па
        /// </summary>
        public double PressureLoss_Cold_Pa => PressureLoss_Cold_mbar * 100;
        
        /// <summary>
        /// Максимально допустимые потери (ограничение РЕХАУ), мбар
        /// </summary>
        public static readonly double MaxAllowedPressure_mbar = 320;
        
        /// <summary>
        /// Проверка превышения лимита потерь
        /// </summary>
        public bool IsPressureExceeded => PressureLoss_Cold_mbar > MaxAllowedPressure_mbar;
    }
}