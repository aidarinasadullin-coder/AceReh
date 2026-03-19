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
        /// Потери в трубе подводки, Па
        /// </summary>
        public double SupplyPipeLoss { get; set; }
        
        /// <summary>
        /// Потери в вентиле, Па
        /// </summary>
        public double ValveLoss { get; set; }
        
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
        // === Входные данные (общие) ===
        
        /// <summary>
        /// Номер контура
        /// </summary>
        public int CircuitNumber { get; set; }
        
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
        /// Признак того, что длина введена пользователем (площадь вычислена)
        /// </summary>
        [ObservableProperty]
        private bool _isLengthUserInput;
        
        /// <summary>
        /// Признак того, что площадь введена пользователем (длина вычислена)
        /// </summary>
        [ObservableProperty]
        private bool _isAreaUserInput;
        
        /// <summary>
        /// Признак того, что поле длины заблокировано
        /// </summary>
        public bool IsLengthReadOnly => IsAreaUserInput && CircuitArea > 0;
        
        /// <summary>
        /// Признак того, что поле площади заблокировано
        /// </summary>
        public bool IsAreaReadOnly => IsLengthUserInput && CircuitLength > 0;
        
        /// <summary>
        /// Обработчик изменения длины контура
        /// </summary>
        partial void OnCircuitLengthChanged(double value)
        {
            // Если длина введена пользователем
            if (value > 0)
            {
                IsLengthUserInput = true;
                IsAreaUserInput = false;
                // Вычислить площадь: S = L / (100 / VA_hk)
                if (PipeSpacing_cm > 0)
                {
                    // Устанавливаем поле напрямую, чтобы избежать рекурсии
                    _circuitArea = value / (100.0 / PipeSpacing_cm);
                    OnPropertyChanged(nameof(CircuitArea));
                }
            }
            else
            {
                // Длина очищена — разблокировать оба поля
                IsLengthUserInput = false;
                IsAreaUserInput = false;
            }
            
            // Уведомить об изменении свойств только для чтения
            OnPropertyChanged(nameof(IsLengthReadOnly));
            OnPropertyChanged(nameof(IsAreaReadOnly));
        }
        
        /// <summary>
        /// Обработчик изменения площади контура
        /// </summary>
        partial void OnCircuitAreaChanged(double value)
        {
            // Если площадь введена пользователем
            if (value > 0)
            {
                IsAreaUserInput = true;
                IsLengthUserInput = false;
                // Вычислить длину: L = S × (100 / VA_hk)
                if (PipeSpacing_cm > 0)
                {
                    // Устанавливаем поле напрямую, чтобы избежать рекурсии
                    _circuitLength = value * (100.0 / PipeSpacing_cm);
                    OnPropertyChanged(nameof(CircuitLength));
                }
            }
            else
            {
                // Площадь очищена — разблокировать оба поля
                IsLengthUserInput = false;
                IsAreaUserInput = false;
            }
            
            // Уведомить об изменении свойств только для чтения
            OnPropertyChanged(nameof(IsLengthReadOnly));
            OnPropertyChanged(nameof(IsAreaReadOnly));
        }
        
        /// <summary>
        /// Обработчик изменения шага укладки
        /// </summary>
        partial void OnPipeSpacing_cmChanged(double value)
        {
            // При изменении шага укладки пересчитать связанное поле
            if (value > 0)
            {
                if (IsLengthUserInput && CircuitLength > 0)
                {
                    // Если была введена длина → пересчитать площадь
                    _circuitArea = CircuitLength / (100.0 / value);
                    OnPropertyChanged(nameof(CircuitArea));
                }
                else if (IsAreaUserInput && CircuitArea > 0)
                {
                    // Если была введена площадь → пересчитать длину
                    _circuitLength = CircuitArea * (100.0 / value);
                    OnPropertyChanged(nameof(CircuitLength));
                }
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
        public HydraulicMode DisplayMode { get; set; } = HydraulicMode.OperatingTemperature;
        
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