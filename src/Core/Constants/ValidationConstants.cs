namespace SnowMeltingCalculator.Core.Constants
{
    /// <summary>
    /// Константы валидации
    /// </summary>
    /// <remarks>
    /// Централизованные константы для валидации входных данных.
    /// </remarks>
    public static class ValidationConstants
    {
        #region Климатические данные

        /// <summary>
        /// Минимальная температура наружного воздуха, °C
        /// </summary>
        public const double MinAirTemperature = -60.0;

        /// <summary>
        /// Максимальная температура наружного воздуха, °C
        /// </summary>
        public const double MaxAirTemperature = 10.0;

        /// <summary>
        /// Минимальная скорость ветра, м/с
        /// </summary>
        public const double MinWindSpeed = 0.0;

        /// <summary>
        /// Максимальная скорость ветра, м/с
        /// </summary>
        public const double MaxWindSpeed = 50.0;

        /// <summary>
        /// Минимальная интенсивность снегопада, мм/ч
        /// </summary>
        public const double MinSnowfallIntensity = 0.0;

        /// <summary>
        /// Максимальная интенсивность снегопада, мм/ч
        /// </summary>
        public const double MaxSnowfallIntensity = 20.0;

        /// <summary>
        /// Минимальная влажность, %
        /// </summary>
        public const double MinHumidity = 0.0;

        /// <summary>
        /// Максимальная влажность, %
        /// </summary>
        public const double MaxHumidity = 100.0;

        #endregion

        #region Температуры

        /// <summary>
        /// Минимальная температура грунта, °C
        /// </summary>
        public const double MinGroundTemperature = -10.0;

        /// <summary>
        /// Максимальная температура грунта, °C
        /// </summary>
        public const double MaxGroundTemperature = 30.0;

        /// <summary>
        /// Минимальная температура подачи, °C
        /// </summary>
        public const double MinSupplyTemperature = 20.0;

        /// <summary>
        /// Максимальная температура подачи, °C
        /// </summary>
        public const double MaxSupplyTemperature = 90.0;

        /// <summary>
        /// Минимальный температурный перепад, К
        /// </summary>
        public const double MinDeltaT = 1.0;

        /// <summary>
        /// Максимальный температурный перепад, К
        /// </summary>
        public const double MaxDeltaT = 30.0;

        #endregion

        #region Трубы

        /// <summary>
        /// Минимальный наружный диаметр трубы, мм
        /// </summary>
        public const double MinOuterDiameter = 10.0;

        /// <summary>
        /// Максимальный наружный диаметр трубы, мм
        /// </summary>
        public const double MaxOuterDiameter = 100.0;

        /// <summary>
        /// Минимальная толщина стенки трубы, мм
        /// </summary>
        public const double MinWallThickness = 0.5;

        /// <summary>
        /// Максимальная толщина стенки трубы, мм
        /// </summary>
        public const double MaxWallThickness = 10.0;

        /// <summary>
        /// Минимальная теплопроводность материала трубы, Вт/м·К
        /// </summary>
        public const double MinThermalConductivity = 0.1;

        /// <summary>
        /// Максимальная теплопроводность материала трубы, Вт/м·К
        /// </summary>
        public const double MaxThermalConductivity = 500.0;

        #endregion

        #region Шаг укладки

        /// <summary>
        /// Минимальный шаг укладки трубы, мм
        /// </summary>
        public const double MinPipeSpacing = 50.0;

        /// <summary>
        /// Максимальный шаг укладки трубы, мм
        /// </summary>
        public const double MaxPipeSpacing = 500.0;

        #endregion

        #region Тепловые сопротивления

        /// <summary>
        /// Минимальное тепловое сопротивление, м²·К/Вт
        /// </summary>
        public const double MinThermalResistance = 0.0;

        /// <summary>
        /// Максимальное тепловое сопротивление, м²·К/Вт
        /// </summary>
        public const double MaxThermalResistance = 10.0;

        /// <summary>
        /// Минимальная теплопроводность стяжки, Вт/м·К
        /// </summary>
        public const double MinLambdaE = 0.1;

        /// <summary>
        /// Максимальная теплопроводность стяжки, Вт/м·К
        /// </summary>
        public const double MaxLambdaE = 5.0;

        #endregion

        #region Теплоноситель

        /// <summary>
        /// Минимальная плотность теплоносителя, кг/м³
        /// </summary>
        public const double MinCoolantDensity = 900.0;

        /// <summary>
        /// Максимальная плотность теплоносителя, кг/м³
        /// </summary>
        public const double MaxCoolantDensity = 1200.0;

        /// <summary>
        /// Минимальная теплоёмкость теплоносителя, кДж/кг·К
        /// </summary>
        public const double MinCoolantHeatCapacity = 2.0;

        /// <summary>
        /// Максимальная теплоёмкость теплоносителя, кДж/кг·К
        /// </summary>
        public const double MaxCoolantHeatCapacity = 5.0;

        #endregion

        #region Гидравлика

        /// <summary>
        /// Минимальная длина контура, м
        /// </summary>
        public const double MinCircuitLength = 1.0;

        /// <summary>
        /// Максимальная длина контура, м
        /// </summary>
        public const double MaxCircuitLength = 120.0;

        /// <summary>
        /// Минимальная площадь контура, м²
        /// </summary>
        public const double MinCircuitArea = 0.1;

        /// <summary>
        /// Максимальная площадь контура, м²
        /// </summary>
        public const double MaxCircuitArea = 100.0;

        /// <summary>
        /// Минимальный расход, л/ч
        /// </summary>
        public const double MinFlowRate = 1.0;

        /// <summary>
        /// Максимальный расход, л/ч
        /// </summary>
        public const double MaxFlowRate = 10000.0;

        /// <summary>
        /// Минимальная скорость потока, м/с
        /// </summary>
        public const double MinVelocity = 0.1;

        /// <summary>
        /// Максимальная скорость потока, м/с
        /// </summary>
        public const double MaxVelocity = 2.0;

        /// <summary>
        /// Максимальные потери давления, Па
        /// </summary>
        public const int MaxPressureLoss = 32000;

        #endregion

        #region Концентрация гликоля

        /// <summary>
        /// Минимальная концентрация гликоля, %
        /// </summary>
        public const double MinGlycolConcentration = 10.0;

        /// <summary>
        /// Максимальная концентрация гликоля, %
        /// </summary>
        public const double MaxGlycolConcentration = 90.0;

        #endregion

        #region Сообщения об ошибках

        /// <summary>
        /// Шаблон сообщения об ошибке диапазона
        /// </summary>
        public const string RangeErrorMessage = "{0} должен быть в диапазоне от {1} до {2}";

        /// <summary>
        /// Шаблон сообщения об ошибке положительного значения
        /// </summary>
        public const string PositiveErrorMessage = "{0} должен быть положительным";

        /// <summary>
        /// Шаблон сообщения об ошибке неотрицательного значения
        /// </summary>
        public const string NonNegativeErrorMessage = "{0} не может быть отрицательным";

        /// <summary>
        /// Сообщение об ошибке при отсутствии данных
        /// </summary>
        public const string NotSetErrorMessage = "{0} не задан(о)";

        #endregion
    }
}