using CommunityToolkit.Mvvm.ComponentModel;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Входные данные для гидравлического расчёта контуров
    /// </summary>
    /// <remarks>
    /// Содержит данные из ThermalModule, ClimateModule и от пользователя.
    /// Используется для расчёта таблицы контуров.
    /// </remarks>
    public partial class HydraulicInputData : ObservableObject
    {
        // === Данные из ThermalModule ===
        
        /// <summary>
        /// Мощность вверх (q_up), Вт/м²
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.PowerUp
        /// </remarks>
        [ObservableProperty]
        private double _powerUp;
        
        /// <summary>
        /// Мощность вниз (q_down), Вт/м²
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.PowerDown
        /// </remarks>
        [ObservableProperty]
        private double _powerDown;
        
        /// <summary>
        /// Температура подачи (T_supply), °C
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.SupplyTemperature
        /// </remarks>
        [ObservableProperty]
        private double _supplyTemperature;
        
        /// <summary>
        /// Температура обратки (T_return), °C
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.ReturnTemperature
        /// </remarks>
        [ObservableProperty]
        private double _returnTemperature;
        
        /// <summary>
        /// Внутренний диаметр трубы (d_inner), мм
        /// </summary>
        /// <remarks>
        /// Вычисляется: d_inner = D_ext - 2 × s
        /// Где D_ext — наружный диаметр, s — толщина стенки
        /// </remarks>
        [ObservableProperty]
        private double _innerDiameter;

        /// <summary>
        /// Шаг укладки трубы, мм
        /// </summary>
        /// <remarks>
        /// Получается из ICalculationStateService.PipeSpacing
        /// </remarks>
        public double PipeSpacing { get; set; }

        /// <summary>
        /// Выбранный тип трубы
        /// </summary>
        /// <remarks>
        /// Получается из ThermalViewModel.SelectedPipe
        /// </remarks>
        public PipeType? SelectedPipe { get; set; }

        /// <summary>
        /// Результат теплового расчёта
        /// </summary>
        public IThermalCalculationResult? ThermalResult { get; set; }

        // === Данные из ClimateModule ===
        
        /// <summary>
        /// Температура холодной пятидневки (t_cold), °C
        /// </summary>
        /// <remarks>
        /// Получается из ClimateData.ColdFiveDayTemperature
        /// Используется для расчёта при "холодном пуске"
        /// </remarks>
        [ObservableProperty]
        private double _coldFiveDayTemperature;
        
        // === Данные от пользователя ===
        
        /// <summary>
        /// Тип гликоля
        /// </summary>
        /// <remarks>
        /// Этиленгликоль или пропиленгликоль
        /// По умолчанию: этиленгликоль
        /// </remarks>
        [ObservableProperty]
        private GlycolType _glycolType = GlycolType.Ethylene;
        
        /// <summary>
        /// Концентрация гликоля, %
        /// </summary>
        /// <remarks>
        /// Диапазон: 10-90%
        /// По умолчанию: 50%
        /// </remarks>
        [ObservableProperty]
        private double _glycolConcentration = 50.0;
        
        /// <summary>
        /// Шаг подводки (VA_zul), см
        /// </summary>
        /// <remarks>
        /// По умолчанию: 5 см
        /// </remarks>
        [ObservableProperty]
        private double _supplySpacing_cm = 5.0;
        
        /// <summary>
        /// Доля тепла от подводок (q_zul), %
        /// </summary>
        /// <remarks>
        /// По умолчанию: 10%
        /// Диапазон: 0-100%
        /// </remarks>
        [ObservableProperty]
        private double _supplyHeatPercent = 10.0;

        /// <summary>
        /// Тип балансировочного клапана
        /// </summary>
        /// <remarks>
        /// По умолчанию: HKV_D
        /// Определяет kv-значение для расчёта потерь на клапане
        /// </remarks>
        [ObservableProperty]
        private ValveType _valveType = ValveType.HKV_D;

        // === Вычисляемые свойства ===
        
        /// <summary>
        /// Рабочая температура (T_operating), °C
        /// </summary>
        /// <remarks>
        /// Формула: T_operating = (T_supply + T_return) / 2
        /// </remarks>
        public double OperatingTemperature => (SupplyTemperature + ReturnTemperature) / 2.0;
        
        /// <summary>
        /// Расчётная температура (T_design), °C
        /// </summary>
        /// <remarks>
        /// Равна температуре холодной пятидневки
        /// </remarks>
        public double DesignTemperature => ColdFiveDayTemperature;
        
        /// <summary>
        /// Температурный перепад (ΔT), К
        /// </summary>
        /// <remarks>
        /// Формула: ΔT = T_supply - T_return
        /// </remarks>
        public double DeltaT => SupplyTemperature - ReturnTemperature;
        
        // === Валидация ===
        
        /// <summary>
        /// Признак валидности данных
        /// </summary>
        public bool IsValid => Validate().IsValid;
        
        /// <summary>
        /// Валидировать входные данные
        /// </summary>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (PowerUp <= 0)
                result.AddError("Мощность вверх должна быть положительной");
            
            if (PowerDown < 0)
                result.AddError("Мощность вниз не может быть отрицательной");
            
            if (SupplyTemperature <= ReturnTemperature)
                result.AddError("Температура подачи должна быть больше температуры обратки");
            
            if (InnerDiameter <= 0)
                result.AddError("Внутренний диаметр трубы должен быть положительным");
            
            if (GlycolConcentration < 10 || GlycolConcentration > 90)
                result.AddError($"Концентрация гликоля должна быть от 10 до 90% (текущая: {GlycolConcentration:F0}%)");
            
            if (SupplySpacing_cm <= 0)
                result.AddError("Шаг подводки должен быть положительным");
            
            if (SupplyHeatPercent < 0 || SupplyHeatPercent > 100)
                result.AddError($"Доля тепла от подводок должна быть от 0 до 100% (текущая: {SupplyHeatPercent:F0}%)");
            
            return result;
        }
    }
}