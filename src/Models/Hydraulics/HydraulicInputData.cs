namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Входные данные для гидравлического расчёта контуров
    /// </summary>
    /// <remarks>
    /// Содержит данные из ThermalModule, ClimateModule и от пользователя.
    /// Используется для расчёта таблицы контуров.
    /// </remarks>
    public class HydraulicInputData
    {
        // === Данные из ThermalModule ===
        
        /// <summary>
        /// Мощность вверх (q_up), Вт/м²
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.PowerUp
        /// </remarks>
        public double PowerUp { get; set; }
        
        /// <summary>
        /// Мощность вниз (q_down), Вт/м²
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.PowerDown
        /// </remarks>
        public double PowerDown { get; set; }
        
        /// <summary>
        /// Температура подачи (T_supply), °C
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.SupplyTemperature
        /// </remarks>
        public double SupplyTemperature { get; set; }
        
        /// <summary>
        /// Температура обратки (T_return), °C
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.ReturnTemperature
        /// </remarks>
        public double ReturnTemperature { get; set; }
        
        /// <summary>
        /// Внутренний диаметр трубы (d_inner), мм
        /// </summary>
        /// <remarks>
        /// Вычисляется: d_inner = D_ext - 2 × s
        /// Где D_ext — наружный диаметр, s — толщина стенки
        /// </remarks>
        public double InnerDiameter { get; set; }
        
        // === Данные из ClimateModule ===
        
        /// <summary>
        /// Температура холодной пятидневки (t_cold), °C
        /// </summary>
        /// <remarks>
        /// Получается из ClimateData.ColdFiveDayTemperature
        /// Используется для расчёта при "холодном пуске"
        /// </remarks>
        public double ColdFiveDayTemperature { get; set; }
        
        // === Данные от пользователя ===
        
        /// <summary>
        /// Тип гликоля
        /// </summary>
        /// <remarks>
        /// Этиленгликоль или пропиленгликоль
        /// По умолчанию: этиленгликоль
        /// </remarks>
        public GlycolType GlycolType { get; set; } = GlycolType.Ethylene;
        
        /// <summary>
        /// Концентрация гликоля, %
        /// </summary>
        /// <remarks>
        /// Диапазон: 10-90%
        /// По умолчанию: 50%
        /// </remarks>
        public double GlycolConcentration { get; set; } = 50.0;
        
        /// <summary>
        /// Шаг подводки (VA_zul), см
        /// </summary>
        /// <remarks>
        /// По умолчанию: 5 см
        /// </remarks>
        public double SupplySpacing_cm { get; set; } = 5.0;
        
        /// <summary>
        /// Доля тепла от подводок (q_zul), %
        /// </summary>
        /// <remarks>
        /// По умолчанию: 10%
        /// Диапазон: 0-100%
        /// </remarks>
        public double SupplyHeatPercent { get; set; } = 10.0;

        /// <summary>
        /// Тип балансировочного клапана
        /// </summary>
        /// <remarks>
        /// По умолчанию: HKV_D
        /// Определяет kv-значение для расчёта потерь на клапане
        /// </remarks>
        public ValveType ValveType { get; set; } = ValveType.HKV_D;

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