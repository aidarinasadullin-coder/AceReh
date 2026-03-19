namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Параметры для теплового расчёта
    /// </summary>
    public class ThermalParameters
    {
        // === Режим работы ===
        
        /// <summary>
        /// Режим работы (Антиобледенение/Таяние/Интенсивное)
        /// </summary>
        public OperatingMode Mode { get; set; } = OperatingMode.Melting;
        
        // === Температуры ===
        
        /// <summary>
        /// Температура подачи, °C
        /// </summary>
        public double SupplyTemperature { get; set; } = 50.0;
        
        /// <summary>
        /// Температурный перепад, К
        /// </summary>
        public double DeltaT { get; set; } = 15.0;
        
        /// <summary>
        /// Температура грунта, °C
        /// </summary>
        public double GroundTemperature { get; set; } = 10.0;
        
        // === Труба ===
        
        /// <summary>
        /// Тип трубы
        /// </summary>
        public PipeType Pipe { get; set; } = PipeType.StandardPipes[1]; // 20x2,0 по умолчанию
        
        /// <summary>
        /// Шаг укладки трубы, мм
        /// </summary>
        public double PipeSpacing { get; set; } = 200.0;
        
        // === Конструкция (от IConstructionData) ===
        
        /// <summary>
        /// Суммарное термическое сопротивление слоёв над трубой, м²·К/Вт
        /// </summary>
        public double R1Total { get; set; }
        
        /// <summary>
        /// Суммарное термическое сопротивление слоёв под трубой, м²·К/Вт
        /// </summary>
        public double R2Total { get; set; }
        
        /// <summary>
        /// Теплопроводность стяжки (бетона) вокруг трубы, Вт/м·К
        /// </summary>
        public double LambdaE { get; set; } = 1.6;
        
        // === Климат (от IClimateData) ===
        
        /// <summary>
        /// Температура наружного воздуха, °C
        /// </summary>
        public double AirTemperature { get; set; }
        
        /// <summary>
        /// Скорость ветра, м/с
        /// </summary>
        public double WindSpeed { get; set; }
        
        /// <summary>
        /// Интенсивность снегопада, мм/ч (водяной эквивалент)
        /// </summary>
        public double SnowfallIntensity { get; set; }
        
        // === Теплоноситель ===
        
        /// <summary>
        /// Плотность теплоносителя, кг/м³
        /// </summary>
        public double CoolantDensity { get; set; } = 1053.0; // 50% гликоль
        
        /// <summary>
        /// Удельная теплоёмкость теплоносителя, кДж/кг·К
        /// </summary>
        public double CoolantHeatCapacity { get; set; } = 3.39; // 50% гликоль
        
        /// <summary>
        /// Создать копию параметров
        /// </summary>
        public ThermalParameters Clone()
        {
            return new ThermalParameters
            {
                Mode = Mode,
                SupplyTemperature = SupplyTemperature,
                DeltaT = DeltaT,
                GroundTemperature = GroundTemperature,
                Pipe = Pipe,
                PipeSpacing = PipeSpacing,
                R1Total = R1Total,
                R2Total = R2Total,
                LambdaE = LambdaE,
                AirTemperature = AirTemperature,
                WindSpeed = WindSpeed,
                SnowfallIntensity = SnowfallIntensity,
                CoolantDensity = CoolantDensity,
                CoolantHeatCapacity = CoolantHeatCapacity
            };
        }
    }
}