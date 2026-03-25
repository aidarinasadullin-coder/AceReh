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
        /// Максимально допустимые удельные потери, Па/м
        /// </summary>
        /// <remarks>
        /// Ограничение РЕХАУ: R ≤ 300 Па/м для рабочей температуры.
        /// При холодном пуске удельные потери могут превышать 300 Па/м из-за повышенной вязкости.
        /// </remarks>
        public static readonly double MaxPressureLossPerMeter = 300.0;

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
        /// Признак превышения удельных потерь (R > 300 Па/м)
        /// </summary>
        /// <remarks>
        /// Проверка выполняется только для рабочей температуры.
        /// При холодном пуске удельные потери могут превышать 300 Па/м из-за повышенной вязкости.
        /// </remarks>
        public bool IsPressureLossPerMeterExceeded => PressureLossPerMeter > MaxPressureLossPerMeter;
        
        #region Новые свойства для гидравлики (DpRohr, DpVerteiler, DpVent, DpGesamt)
        
        /// <summary>
        /// Потери в трубе контура, Па (DpRohr)
        /// </summary>
        /// <remarks>
        /// Формула: DpRohr = (L_hk + L_zul) × R
        /// Где:
        /// - L_hk — длина контура, м
        /// - L_zul — длина подводки, м
        /// - R — удельные потери, Па/м
        /// 
        /// Соответствует столбцу K в Excel (gidravlica.xls)
        /// </remarks>
        public double DpRohr { get; set; }

        /// <summary>
        /// Потери в распределителе, Па (DpVerteiler)
        /// </summary>
        /// <remarks>
        /// Формулы зависят от типа коллектора:
        /// 
        /// Для IV 1¼" и IV 1½":
        /// DpVerteiler = 15000 × (ρ/2000) × v²
        /// 
        /// Для HKV-D:
        /// DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
        /// 
        /// Где:
        /// - ρ — плотность в кг/м³ (делить на 1000 для г/см³)
        /// - v — скорость в м/с
        /// - V_dot — расход в л/ч
        /// 
        /// Соответствует столбцу L в Excel (gidravlica.xls)
        /// </remarks>
        public double DpVerteiler { get; set; }

        /// <summary>
        /// Потери в вентиле, Па (DpVent)
        /// </summary>
        /// <remarks>
        /// Формулы зависят от типа коллектора:
        /// 
        /// Для IV 1¼" и IV 1½":
        /// DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
        /// 
        /// Для HKV-D:
        /// DpVent = 15000 × (ρ/2000) × v²
        /// 
        /// Где:
        /// - V_dot — расход в л/ч
        /// - Kv — коэффициент пропускной способности, м³/ч
        /// - ρ — плотность в кг/м³ (делить на 1000 для г/см³)
        /// - v — скорость в м/с
        /// 
        /// Соответствует столбцу M в Excel (gidravlica.xls)
        /// </remarks>
        public double DpVent { get; set; }

        /// <summary>
        /// Суммарные потери, Па (DpGesamt)
        /// </summary>
        /// <remarks>
        /// Формула: DpGesamt = DpRohr + DpVerteiler + DpVent
        /// 
        /// Соответствует столбцу N в Excel (gidravlica.xls)
        /// </remarks>
        public double DpGesamt => DpRohr + DpVerteiler + DpVent;

        /// <summary>
        /// Дросселирование для балансировки, Па (zu_drosseln)
        /// </summary>
        /// <remarks>
        /// Формула: zu_drosseln = DpGesamt_max - DpGesamt_контур
        /// 
        /// Где:
        /// - DpGesamt_max — максимальные суммарные потери в коллекторе
        /// - DpGesamt_контур — суммарные потери контура
        /// 
        /// Соответствует столбцу O в Excel (gidravlica.xls)
        /// 
        /// Примечание: Это свойство вычисляется в CircuitRow, а не в CircuitTemperatureResult.
        /// </remarks>
        public double ZuDrosseln { get; set; }
        
        #endregion
        
        #region Устаревшие свойства (для обратной совместимости)
        
        /// <summary>
        /// Потери в трубе контура, Па
        /// </summary>
        [Obsolete("Использовать DpRohr вместо CircuitPipeLoss. DpRohr включает потери в контуре и подводке.")]
        public double CircuitPipeLoss { get; set; }

        /// <summary>
        /// Потери в трубе контура, мбар
        /// </summary>
        [Obsolete("Использовать DpRohr / 100.0 вместо CircuitPipeLoss_mbar")]
        public double CircuitPipeLoss_mbar => CircuitPipeLoss / 100.0;

        /// <summary>
        /// Потери в трубе подводки, Па
        /// </summary>
        [Obsolete("Использовать DpRohr вместо SupplyPipeLoss. DpRohr включает потери в контуре и подводке.")]
        public double SupplyPipeLoss { get; set; }

        /// <summary>
        /// Потери в вентиле, Па
        /// </summary>
        [Obsolete("Использовать DpVent вместо ValveLoss для IV. Для HKV-D использовать DpVerteiler.")]
        public double ValveLoss { get; set; }

        /// <summary>
        /// Потери в вентиле, мбар
        /// </summary>
        [Obsolete("Использовать DpVent / 100.0 вместо ValveLoss_mbar")]
        public double ValveLoss_mbar => ValveLoss / 100.0;

        /// <summary>
        /// Суммарные потери, Па
        /// </summary>
        [Obsolete("Использовать DpGesamt вместо TotalLoss")]
        public double TotalLoss => DpRohr + DpVerteiler + DpVent;

        /// <summary>
        /// Суммарные потери, мбар
        /// </summary>
        [Obsolete("Использовать DpGesamt / 100.0 вместо TotalLoss_mbar")]
        public double TotalLoss_mbar => DpGesamt / 100.0;
        
        #endregion
    }

    /// <summary>
    /// Строка таблицы контура для гидравлического расчёта
    /// </summary>
    public partial class CircuitRow : ObservableObject
    {
        // === Флаг для предотвращения рекурсии при пересчёте ===
        private bool _isUpdating;
        
        // === Флаги режима ввода ===
        
        /// <summary>
        /// Признак того, что пользователь ввёл длину (а не площадь)
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLengthReadOnly))]
        [NotifyPropertyChangedFor(nameof(IsAreaReadOnly))]
        private bool _isLengthUserInput;
        
        /// <summary>
        /// Признак того, что пользователь ввёл площадь (а не длину)
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLengthReadOnly))]
        [NotifyPropertyChangedFor(nameof(IsAreaReadOnly))]
        private bool _isAreaUserInput;
        
        /// <summary>
        /// Поле длины заблокировано для ввода (когда пользователь ввёл площадь)
        /// </summary>
        public bool IsLengthReadOnly => IsAreaUserInput && CircuitArea > 0;
        
        /// <summary>
        /// Поле площади заблокировано для ввода (когда пользователь ввёл длину)
        /// </summary>
        public bool IsAreaReadOnly => IsLengthUserInput && CircuitLength > 0;
        
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
                // Установить флаги режима ввода
                if (value > 0)
                {
                    IsLengthUserInput = true;
                    IsAreaUserInput = false;
                }
                else
                {
                    // При очистке поля сбросить оба флага
                    IsLengthUserInput = false;
                    IsAreaUserInput = false;
                }
                
                // Вычислить площадь: Площадь = Длина * Шаг_укладки / 100
                // Шаг_укладки в см, поэтому делим на 100 для получения площади в м²
                if (PipeSpacing_cm > 0 && value > 0)
                {
                    CircuitArea = value * PipeSpacing_cm / 100.0;
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
                // Установить флаги режима ввода
                if (value > 0)
                {
                    IsAreaUserInput = true;
                    IsLengthUserInput = false;
                }
                else
                {
                    // При очистке поля сбросить оба флага
                    IsAreaUserInput = false;
                    IsLengthUserInput = false;
                }
                
                // Вычислить длину: Длина = Площадь * 100 / Шаг_укладки
                // Шаг_укладки в см, поэтому умножаем на 100 для получения длины в м
                if (PipeSpacing_cm > 0 && value > 0)
                {
                    CircuitLength = value * 100.0 / PipeSpacing_cm;
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
                // При изменении шага укладки пересчитать связанное значение
                // только если был пользовательский ввод
                if (value > 0)
                {
                    if (IsLengthUserInput && CircuitLength > 0)
                    {
                        // Пользователь ввёл длину - пересчитать площадь
                        CircuitArea = CircuitLength * value / 100.0;
                    }
                    else if (IsAreaUserInput && CircuitArea > 0)
                    {
                        // Пользователь ввёл площадь - пересчитать длину
                        CircuitLength = CircuitArea * 100.0 / value;
                    }
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
        [NotifyPropertyChangedFor(nameof(FlowRate_Ls))]
        private double _flowRate;

        /// <summary>
        /// Расход теплоносителя в л/с (вычисляется из FlowRate)
        /// </summary>
        /// <remarks>
        /// Формула: FlowRate_Ls = FlowRate / 3600
        /// </remarks>
        public double FlowRate_Ls => FlowRate / 3600.0;

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
        [NotifyPropertyChangedFor(nameof(PressureLossWarning))]
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
        /// Предупреждение об оборотах клапана (если превышен максимум)
        /// </summary>
        [ObservableProperty]
        private string? _valveTurnsWarning;

        /// <summary>
        /// Признак референсного контура (с максимальными потерями)
        /// </summary>
        [ObservableProperty]
        private bool _isReferenceCircuit;
        
        /// <summary>
        /// Признак активного контура
        /// </summary>
        public bool IsActive => CircuitLength > 0;

        /// <summary>
        /// Предупреждение о превышении удельных потерь (только для рабочей температуры)
        /// </summary>
        /// <remarks>
        /// Проверка R ≤ 300 Па/м выполняется только для рабочей температуры.
        /// При холодном пуске удельные потери могут превышать 300 Па/м из-за повышенной вязкости.
        /// </remarks>
        public string? PressureLossWarning =>
            OperatingResult?.PressureLossPerMeter > CircuitTemperatureResult.MaxPressureLossPerMeter
                ? $"Удельные потери {OperatingResult.PressureLossPerMeter:F0} Па/м > {CircuitTemperatureResult.MaxPressureLossPerMeter:F0} Па/м"
                : null;
        
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
        public double TotalLoss_mbar => CurrentResult.DpGesamt / 100.0;
    }
}