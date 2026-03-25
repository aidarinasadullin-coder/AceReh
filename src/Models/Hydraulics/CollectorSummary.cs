using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Итоги расчёта коллектора
    /// </summary>
    public partial class CollectorSummary : ObservableObject
    {
        /// <summary>
        /// Номер коллектора
        /// </summary>
        [ObservableProperty]
        private int _collectorNumber;
        
        /// <summary>
        /// Тип коллектора
        /// </summary>
        [ObservableProperty]
        private string _collectorType = "HKV-D";
        
        /// <summary>
        /// Kv коллектора (коэффициент пропускной способности), м³/ч
        /// </summary>
        /// <remarks>
        /// HKV-D: 1.2
        /// IV 1¼": 1.45
        /// IV 1½": 1.5
        /// </remarks>
        [ObservableProperty]
        private double _kv = 1.2;
        
        /// <summary>
        /// Количество контуров
        /// </summary>
        [ObservableProperty]
        private int _circuitCount;
        
        /// <summary>
        /// Общая длина труб, м
        /// </summary>
        [ObservableProperty]
        private double _totalPipeLength;
        
        /// <summary>
        /// Общая мощность, Вт
        /// </summary>
        [ObservableProperty]
        private double _totalPower;
        
        /// <summary>
        /// Общий расход, л/ч
        /// </summary>
        [ObservableProperty]
        private double _totalFlowRate;
        
        /// <summary>
        /// Общий расход, м³/ч
        /// </summary>
        public double TotalFlowRate_m3h => TotalFlowRate / 1000.0;

        /// <summary>
        /// Потери давления при рабочей температуре, Па
        /// </summary>
        [ObservableProperty]
        private double _pressureLoss_Operating_Pa;

        /// <summary>
        /// Потери давления при рабочей температуре, мбар
        /// </summary>
        public double PressureLoss_Operating_mbar => PressureLoss_Operating_Pa / 100.0;

        /// <summary>
        /// Потери давления при расчётной (холодной) температуре, Па
        /// </summary>
        [ObservableProperty]
        private double _pressureLoss_Cold_Pa;

        /// <summary>
        /// Потери давления при расчётной температуре, мбар
        /// </summary>
        public double PressureLoss_Cold_mbar => PressureLoss_Cold_Pa / 100.0;
        
        /// <summary>
        /// Максимальные потери давления контура (референсный контур), Па
        /// </summary>
        [ObservableProperty]
        private double _maxCircuitLoss;
        
        /// <summary>
        /// Референсный контур (с максимальными потерями)
        /// </summary>
        [ObservableProperty]
        private int _referenceCircuitNumber;
        
        /// <summary>
        /// Признак валидности
        /// </summary>
        [ObservableProperty]
        private bool _isValid;
        
        /// <summary>
        /// Предупреждения
        /// </summary>
        [ObservableProperty]
        private string[] _warnings = Array.Empty<string>();

        /// <summary>
        /// Предупреждение о превышении давления или расхода
        /// </summary>
        /// <remarks>
        /// Устанавливается при автоматическом выборе коллектора, если:
        /// - Давление > 320 мбар
        /// - Расход ≥ 7.0 м³/ч
        /// </remarks>
        [ObservableProperty]
        private string? _warning;
        
        /// <summary>
        /// Тип балансировочного клапана
        /// </summary>
        /// <remarks>
        /// Определяет формулу расчёта оборотов клапана:
        /// - HKV-D: бытовой коллектор, Kv = 1.2 м³/ч
        /// - IV 1¼": промышленный коллектор, Kv = 1.45 м³/ч
        /// - IV 1½": промышленный коллектор, Kv = 1.5 м³/ч
        /// </remarks>
        [ObservableProperty]
        private ValveType _valveType = ValveType.HKV_D;
        
        /// <summary>
        /// Максимально допустимые потери (ограничение РЕХАУ), мбар
        /// </summary>
        public static readonly double MaxAllowedPressure_mbar = 320;
        
        /// <summary>
        /// Максимально допустимые потери, Па
        /// </summary>
        public static readonly double MaxAllowedPressure_Pa = 32000;
        
        /// <summary>
        /// Проверка превышения лимита потерь (холодный пуск)
        /// </summary>
        public bool IsColdPressureExceeded => PressureLoss_Cold_Pa > MaxAllowedPressure_Pa;
        
        /// <summary>
        /// Проверка превышения лимита потерь (рабочий режим)
        /// </summary>
        public bool IsOperatingPressureExceeded => PressureLoss_Operating_Pa > MaxAllowedPressure_Pa;
        
        /// <summary>
        /// Проверка превышения лимита потерь (устаревшее свойство для обратной совместимости)
        /// </summary>
        [Obsolete("Используйте IsColdPressureExceeded или IsOperatingPressureExceeded")]
        public bool IsPressureExceeded => IsColdPressureExceeded;
    }
}