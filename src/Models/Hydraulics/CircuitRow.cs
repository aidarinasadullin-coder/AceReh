using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Результат расчёта контура при определённой температуре
    /// </summary>
    public class CircuitTemperatureResult
    {
        /// <summary>
        /// Температура теплоносителя, °C
        /// </summary>
        public double Temperature { get; set; }
        
        /// <summary>
        /// Плотность теплоносителя, г/см³
        /// </summary>
        /// <remarks>
        /// Внимание: GlycolProperties.Density хранит плотность в кг/м³.
        /// При присвоении требуется конвертация: Density = glycolProps.Density / 1000.0
        ///
        /// Пример: 1053 кг/м³ = 1.053 г/см³
        /// </remarks>
        public double Density { get; set; }
        
        /// <summary>
        /// Кинематическая вязкость, мм²/с
        /// </summary>
        public double KinematicViscosity { get; set; }
        
        /// <summary>
        /// Число Рейнольдса
        /// </summary>
        public double ReynoldsNumber { get; set; }
        
        /// <summary>
        /// Режим течения
        /// </summary>
        public FlowRegime FlowRegime { get; set; }
        
        /// <summary>
        /// Коэффициент трения λ
        /// </summary>
        public double FrictionFactor { get; set; }
        
        /// <summary>
        /// Удельные потери давления, Па/м
        /// </summary>
        public double PressureLossPerMeter { get; set; }
        
        /// <summary>
        /// Потери в трубе контура, Па
        /// </summary>
        public double CircuitPipeLoss { get; set; }

        /// <summary>
        /// Потери в трубе контура, мбар
        /// </summary>
        public double CircuitPipeLoss_mbar => CircuitPipeLoss / 100.0;

        /// <summary>
        /// Потери в трубе подводки, Па
        /// </summary>
        public double SupplyPipeLoss { get; set; }

        /// <summary>
        /// Потери в вентиле, Па
        /// </summary>
        public double ValveLoss { get; set; }

        /// <summary>
        /// Потери в вентиле, мбар
        /// </summary>
        public double ValveLoss_mbar => ValveLoss / 100.0;

        /// <summary>
        /// Суммарные потери, Па
        /// </summary>
        public double TotalLoss => CircuitPipeLoss + SupplyPipeLoss + ValveLoss;

        /// <summary>
        /// Суммарные потери, мбар
        /// </summary>
        public double TotalLoss_mbar => TotalLoss / 100.0;
    }

    /// <summary>
    /// Строка таблицы контура для гидравлического расчёта
    /// </summary>
    public partial class CircuitRow : ObservableObject
    {
        // === Флаг для предотвращения рекурсии при пересчёте ===
        private bool _isUpdating;
        
        // === Входные данные (общие) ===
        
        /// <summary>
        /// Номер контура
        /// </summary>
        [ObservableProperty]
        private int _circuitNumber;
        
        /// <summary>
        /// Длина греющего контура (L_hk), м
        /// </summary>
        [ObservableProperty]
        private double _circuitLength;
        
        /// <summary>
        /// Длина подводки (L_zul), м
        /// </summary>
        public double SupplyLength { get; set; }
        
        /// <summary>
        /// Общая длина (L_total = L_hk + L_zul), м
        /// </summary>
        public double TotalLength => CircuitLength + SupplyLength;
        
        /// <summary>
        /// Площадь контура, м² (вычисляется по длине и шагу)
        /// </summary>
        /// <remarks>
        /// Формула: S = L_hk / (100 / VA_hk)
        /// </remarks>
        [ObservableProperty]
        private double _circuitArea;
        
        /// <summary>
        /// Шаг укладки трубы (VA_hk), см
        /// </summary>
        /// <remarks>
        /// Берётся из ThermalModule.PipeSpacing (мм) / 10
        /// </remarks>
        [ObservableProperty]
        private double _pipeSpacing_cm = 20.0;
        
        /// <summary>
        /// Шаг подводки (VA_zul), см (по умолчанию 5)
        /// </summary>
        public double SupplySpacing_cm { get; set; } = 5.0;
        
        /// <summary>
        /// Обработчик изменения длины контура
        /// </summary>
        partial void OnCircuitLengthChanged(double value)
        {
            // Предотвращение рекурсии
            if (_isUpdating) return;
            
            _isUpdating = true;
            
            try
            {
                // Вычислить площадь: Площадь = Длина * Шаг_укладки / 100
                // Шаг_укладки в см, поэтому делим на 100 для получения площади в м²
                if (PipeSpacing_cm > 0 && value > 0)
                {
                    _circuitArea = value * PipeSpacing_cm / 100.0;
                    OnPropertyChanged(nameof(CircuitArea));
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }
        
        /// <summary>
        /// Обработчик изменения площади контура
        /// </summary>
        partial void OnCircuitAreaChanged(double value)
        {
            // Предотвращение рекурсии
            if (_isUpdating) return;
            
            _isUpdating = true;
            
            try
            {
                // Вычислить длину: Длина = Площадь * 100 / Шаг_укладки
                // Шаг_укладки в см, поэтому умножаем на 100 для получения длины в м
                if (PipeSpacing_cm > 0 && value > 0)
                {
                    _circuitLength = value * 100.0 / PipeSpacing_cm;
                    OnPropertyChanged(nameof(CircuitLength));
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }
        
        /// <summary>
        /// Обработчик изменения шага укладки
        /// </summary>
        partial void OnPipeSpacing_cmChanged(double value)
        {
            // Предотвращение рекурсии
            if (_isUpdating) return;
            
            _isUpdating = true;
            
            try
            {
                // При изменении шага укладки пересчитать площадь, если есть длина
                if (value > 0 && CircuitLength > 0)
                {
                    _circuitArea = CircuitLength * value / 100.0;
                    OnPropertyChanged(nameof(CircuitArea));
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }
        
        /// <summary>
        /// Доля тепла от подводок (q_zul), % (по умолчанию 10)
        /// </summary>
        public double SupplyHeatPercent { get; set; } = 10.0;
        
        /// <summary>
        /// Мощность контура (Q_HK), Вт
        /// </summary>
        /// <remarks>
        /// Формула: Q_HK = [(L_hk/(100/VA_hk)) + (L_zul/(100/VA_zul))×(q_zul/100)] × (q_up + q_down)
        /// </remarks>
        [ObservableProperty]
        private double _power;
        
        /// <summary>
        /// Расход теплоносителя (V_dot), л/ч
        /// </summary>
        [ObservableProperty]
        private double _flowRate;
        
        /// <summary>
        /// Скорость потока (v), м/с (вычисляется)
        /// </summary>
        [ObservableProperty]
        private double _velocity;
        
        // === Результаты при рабочей температуре ===
        
        /// <summary>
        /// Результат расчёта при рабочей температуре
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentResult))]
        [NotifyPropertyChangedFor(nameof(FlowRegimeDescription))]
        [NotifyPropertyChangedFor(nameof(TotalLoss_mbar))]
        private CircuitTemperatureResult _operatingResult = new();
        
        // === Результаты при расчётной температуре ===
        
        /// <summary>
        /// Результат расчёта при расчётной (холодной) температуре
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentResult))]
        [NotifyPropertyChangedFor(nameof(FlowRegimeDescription))]
        [NotifyPropertyChangedFor(nameof(TotalLoss_mbar))]
        private CircuitTemperatureResult _designResult = new();
        
        // === Балансировка ===
        
        /// <summary>
        /// Дросселирование для балансировки, Па
        /// </summary>
        /// <remarks>
        /// Разница между максимальными потерями в коллекторе и потерями контура
        /// Вычисляется только для рабочей температуры
        /// </remarks>
        [ObservableProperty]
        private double _throttling;
        
        /// <summary>
        /// Рекомендуемая настройка вентиля (1-8)
        /// </summary>
        [ObservableProperty]
        private int _recommendedValveSetting;
        
        /// <summary>
        /// Обороты балансировочного клапана
        /// </summary>
        [ObservableProperty]
        private double _valveTurns;
        
        /// <summary>
        /// Признак референсного контура (с максимальными потерями)
        /// </summary>
        [ObservableProperty]
        private bool _isReferenceCircuit;
        
        /// <summary>
        /// Признак активного контура
        /// </summary>
        public bool IsActive => CircuitLength > 0;
        
        // === Вычисляемые свойства для отображения ===
        
        /// <summary>
        /// Текущий режим отображения
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentResult))]
        [NotifyPropertyChangedFor(nameof(FlowRegimeDescription))]
        [NotifyPropertyChangedFor(nameof(TotalLoss_mbar))]
        private HydraulicMode _displayMode = HydraulicMode.OperatingTemperature;
        
        /// <summary>
        /// Получить результат для текущего режима отображения
        /// </summary>
        public CircuitTemperatureResult CurrentResult => 
            DisplayMode == HydraulicMode.DesignTemperature ? DesignResult : OperatingResult;
        
        /// <summary>
        /// Описание режима течения для текущего режима
        /// </summary>
        public string FlowRegimeDescription => CurrentResult.FlowRegime switch
        {
            FlowRegime.Laminar => "Ламинарный",
            FlowRegime.Transitional => "Переходный",
            FlowRegime.Turbulent => "Турбулентный",
            _ => ""
        };
        
        /// <summary>
        /// Получить результат в мбар для текущего режима
        /// </summary>
        public double TotalLoss_mbar => CurrentResult.TotalLoss_mbar;
    }
}