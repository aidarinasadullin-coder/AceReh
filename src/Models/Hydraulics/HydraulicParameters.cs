using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Параметры для гидравлического расчёта контура
    /// </summary>
    /// <remarks>
    /// Содержит все входные данные для расчёта гидравлики:
    /// - Параметры контура (длина, шаг укладки)
    /// - Параметры теплоносителя (гликоль, температура)
    /// - Параметры трубы (тип, шероховатость)
    /// - Данные из теплового расчёта (расход, мощность)
    /// </remarks>
    public class HydraulicParameters
    {
        // === Параметры контура ===
        
        /// <summary>
        /// Длина контура (L_HK), м
        /// </summary>
        /// <remarks>
        /// Диапазон: 10-500 м
        /// Формула: L_HK = S × 1000 / lR
        /// Где S — площадь контура, lR — шаг укладки
        /// </remarks>
        public double CircuitLength { get; set; }
        
        /// <summary>
        /// Длина подводки (L_Zul), м
        /// </summary>
        /// <remarks>
        /// Диапазон: 1-100 м
        /// Сумма длин подающей и обратной подводок
        /// </remarks>
        public double SupplyLength { get; set; }
        
        /// <summary>
        /// Шаг укладки (VAHK), см
        /// </summary>
        /// <remarks>
        /// Диапазон: 10-50 см
        /// Рекомендуемые значения: 15, 20, 25, 30 см
        /// </remarks>
        public double PipeSpacing { get; set; }
        
        /// <summary>
        /// Шаг подводки (VAZul), см
        /// </summary>
        /// <remarks>
        /// Условный шаг для расчёта тепла от подводки
        /// Обычно 5 см
        /// </remarks>
        public double SupplySpacing { get; set; } = 5.0;
        
        // === Параметры теплоносителя ===
        
        /// <summary>
        /// Доля гликоля (Glycolanteil), % объёмные
        /// </summary>
        /// <remarks>
        /// Диапазон: 10-90%
        /// По умолчанию: 50%
        /// </remarks>
        public double GlycolConcentration { get; set; } = 50.0;
        
        /// <summary>
        /// Тип гликоля
        /// </summary>
        /// <remarks>
        /// Этиленгликоль или пропиленгликоль
        /// </remarks>
        public GlycolType GlycolType { get; set; } = GlycolType.Ethylene;
        
        /// <summary>
        /// Температура подачи (T_VL), °C
        /// </summary>
        /// <remarks>
        /// Диапазон: 20-90°C
        /// Получается из теплового расчёта
        /// </remarks>
        public double SupplyTemperature { get; set; }
        
        /// <summary>
        /// Температура обратки (T_RL), °C
        /// </summary>
        /// <remarks>
        /// Диапазон: 15-80°C
        /// Получается из теплового расчёта
        /// </remarks>
        public double ReturnTemperature { get; set; }
        
        /// <summary>
        /// Средняя температура теплоносителя, °C
        /// </summary>
        /// <remarks>
        /// Формула: T_mean = (T_VL + T_RL) / 2
        /// Используется для определения свойств гликоля
        /// </remarks>
        public double MeanTemperature => (SupplyTemperature + ReturnTemperature) / 2.0;
        
        /// <summary>
        /// Плотность теплоносителя (ρ), кг/м³
        /// </summary>
        /// <remarks>
        /// Получается из GlycolDataService по температуре и концентрации
        /// </remarks>
        public double Density { get; set; }
        
        /// <summary>
        /// Кинематическая вязкость (ν), мм²/с
        /// </summary>
        /// <remarks>
        /// Получается из GlycolDataService по температуре и концентрации
        /// </remarks>
        public double KinematicViscosity { get; set; }
        
        /// <summary>
        /// Удельная теплоёмкость (c_p), кДж/(кг·К)
        /// </summary>
        /// <remarks>
        /// Получается из GlycolDataService по температуре и концентрации
        /// </remarks>
        public double SpecificHeat { get; set; }
        
        // === Параметры трубы ===
        
        /// <summary>
        /// Тип трубы
        /// </summary>
        /// <remarks>
        /// Только RAUTHERM S (PE-Xa)
        /// </remarks>
        public PipeType? Pipe { get; set; }
        
        /// <summary>
        /// Шероховатость трубы (ε), мм
        /// </summary>
        /// <remarks>
        /// Для PE-Xa: 0.007 мм
        /// </remarks>
        public double Roughness { get; set; } = 0.007;
        
        /// <summary>
        /// Внутренний диаметр трубы (di), мм
        /// </summary>
        /// <remarks>
        /// Вычисляется: di = d - 2 × s
        /// Где d — наружный диаметр, s — толщина стенки
        /// </remarks>
        public double InnerDiameter => Pipe != null 
            ? Pipe.OuterDiameter - 2 * Pipe.WallThickness 
            : 0;
        
        // === Параметры из теплового расчёта ===
        
        /// <summary>
        /// Удельный расход теплоносителя (V_dot), л/(ч·м²)
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.VolumeFlowRate
        /// </remarks>
        public double VolumeFlowRate { get; set; }
        
        /// <summary>
        /// Мощность контура (q_total), Вт/м²
        /// </summary>
        /// <remarks>
        /// Получается из ThermalCalculationResult.PowerTotal
        /// </remarks>
        public double PowerPerArea { get; set; }
        
        /// <summary>
        /// Площадь контура (S), м²
        /// </summary>
        /// <remarks>
        /// Вводится пользователем
        /// </remarks>
        public double CircuitArea { get; set; }
        
        /// <summary>
        /// Расход на контур (v), л/ч
        /// </summary>
        /// <remarks>
        /// Формула: v = VolumeFlowRate × CircuitArea
        /// </remarks>
        public double CircuitFlowRate => VolumeFlowRate * CircuitArea;
        
        // === Валидация ===
        
        /// <summary>
        /// Признак валидности параметров
        /// </summary>
        public bool IsValid => Validate().IsValid;
        
        /// <summary>
        /// Валидировать параметры
        /// </summary>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            if (CircuitLength < 10 || CircuitLength > 500)
                result.AddError($"Длина контура должна быть от 10 до 500 м (текущая: {CircuitLength:F1} м)");
            
            if (SupplyLength < 1 || SupplyLength > 100)
                result.AddError($"Длина подводки должна быть от 1 до 100 м (текущая: {SupplyLength:F1} м)");
            
            if (GlycolConcentration < 10 || GlycolConcentration > 90)
                result.AddError($"Доля гликоля должна быть от 10 до 90% (текущая: {GlycolConcentration:F0}%)");
            
            if (SupplyTemperature < 20 || SupplyTemperature > 90)
                result.AddError($"Температура подачи должна быть от 20 до 90°C (текущая: {SupplyTemperature:F1}°C)");
            
            if (ReturnTemperature < 15 || ReturnTemperature > 80)
                result.AddError($"Температура обратки должна быть от 15 до 80°C (текущая: {ReturnTemperature:F1}°C)");
            
            if (Pipe == null)
                result.AddError("Тип трубы не задан");
            
            if (Density <= 0)
                result.AddError("Плотность теплоносителя должна быть положительной");
            
            if (KinematicViscosity <= 0)
                result.AddError("Кинематическая вязкость должна быть положительной");
            
            return result;
        }
    }
}