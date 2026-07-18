namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Входные параметры для теплового расчёта
    /// </summary>
    /// <remarks>
    /// Тепловые параметры, вводимые пользователем. Не содержит климатических
    /// и конструктивных данных, которые передаются в калькулятор отдельно
    /// через контрактные объекты <see cref="IClimateData"/>
    /// и <see cref="IConstructionData"/>.
    /// </remarks>
    public sealed record ThermalInputs
    {
        /// <summary>
        /// Режим работы (Антиобледенение/Таяние/Интенсивное)
        /// </summary>
        public OperatingMode Mode { get; init; } = OperatingMode.Melting;

        /// <summary>
        /// Температура подачи, °C
        /// </summary>
        public double SupplyTemperature { get; init; } = 50.0;

        /// <summary>
        /// Температура грунта, °C
        /// </summary>
        public double GroundTemperature { get; init; } = 10.0;

        /// <summary>
        /// Тип трубы
        /// </summary>
        public PipeType Pipe { get; init; } = PipeType.StandardPipes[1]; // 20x2,0 по умолчанию

        /// <summary>
        /// Шаг укладки трубы, мм
        /// </summary>
        public double PipeSpacing { get; init; } = 200.0;

        /// <summary>
        /// Теплопроводность стяжки (бетона) вокруг трубы, Вт/м·К
        /// </summary>
        public double LambdaE { get; init; } = 1.6;

        /// <summary>
        /// Плотность теплоносителя, кг/м³
        /// </summary>
        public double CoolantDensity { get; init; } = 1053.0; // 50% гликоль

        /// <summary>
        /// Удельная теплоёмкость теплоносителя, кДж/кг·К
        /// </summary>
        public double CoolantHeatCapacity { get; init; } = 3.39; // 50% гликоль

    }
}
