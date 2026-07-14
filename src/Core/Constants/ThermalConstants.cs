namespace SnowMeltingCalculator.Core.Constants
{
    /// <summary>
    /// Константы теплового расчёта
    /// </summary>
    /// <remarks>
    /// Все константы соответствуют методике расчёта РЕХАУ.
    /// См. docs/Formulas_Snegotayanie.md
    /// </remarks>
    public static class ThermalConstants
    {
        #region Физические константы

        /// <summary>
        /// Плотность снега, кг/м³
        /// </summary>
        /// <remarks>
        /// Используется для расчёта теплоты плавления снега.
        /// Типичное значение для свежевыпавшего снега.
        /// </remarks>
        public const double SnowDensity = 900.0;

        /// <summary>
        /// Удельная теплоёмкость льда, Дж/кг·К
        /// </summary>
        /// <remarks>
        /// Используется для расчёта теплоты нагрева льда до 0°C.
        /// </remarks>
        public const double IceHeatCapacity = 2100.0;

        /// <summary>
        /// Удельная теплота плавления льда, Дж/кг
        /// </summary>
        /// <remarks>
        /// Используется для расчёта теплоты плавления снега.
        /// </remarks>
        public const double IceMeltingHeat = 330000.0;

        /// <summary>
        /// Удельная теплоёмкость воды, Дж/кг·К
        /// </summary>
        /// <remarks>
        /// Используется для расчёта теплоты нагрева воды до температуры поверхности.
        /// </remarks>
        public const double WaterHeatCapacity = 4200.0;

        /// <summary>
        /// Постоянная Стефана-Больцмана, Вт/м²·К⁴
        /// </summary>
        /// <remarks>
        /// Используется для расчёта лучистого теплообмена.
        /// </remarks>
        public const double StefanBoltzmann = 5.77e-8;

        /// <summary>
        /// Коэффициент излучения поверхности
        /// </summary>
        /// <remarks>
        /// Используется для расчёта лучистого теплообмена.
        /// </remarks>
        public const double EmissionCoefficient = 0.055;

        /// <summary>
        /// Коэффициент теплоотдачи снизу (адиабатические условия)
        /// </summary>
        /// <remarks>
        /// Большое значение для моделирования адиабатических условий.
        /// </remarks>
        public const double AlphaBottom = 999999999.0;

        /// <summary>
        /// Коэффициент для расчёта параметра m в теории стержня
        /// </summary>
        /// <remarks>
        /// m = 0.6 × √[(1/RFb + 1/RD) / (λE × dE)]
        /// </remarks>
        public const double RodCoefficient = 0.6;

        #endregion

        #region Коэффициенты формулы теплоотдачи

        /// <summary>
        /// Коэффициент A в формуле теплоотдачи
        /// </summary>
        /// <remarks>
        /// α = A × (t_П - t_H)^B + C × v_H
        /// </remarks>
        public const double HeatTransferCoefficientA = 2.26;

        /// <summary>
        /// Показатель степени B в формуле теплоотдачи
        /// </summary>
        /// <remarks>
        /// α = A × (t_П - t_H)^B + C × v_H
        /// </remarks>
        public const double HeatTransferCoefficientB = 0.33;

        /// <summary>
        /// Коэффициент C в формуле теплоотдачи
        /// </summary>
        /// <remarks>
        /// α = A × (t_П - t_H)^B + C × v_H
        /// </remarks>
        public const double HeatTransferCoefficientC = 2.6;

        #endregion

        #region Ограничения температур

        /// <summary>
        /// Минимальная температура наружного воздуха, °C
        /// </summary>
        public const double MinAirTemperature = -60.0;

        /// <summary>
        /// Максимальная температура наружного воздуха, °C
        /// </summary>
        public const double MaxAirTemperature = 10.0;

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
        /// <remarks>
        /// Общее ограничение. Для PE-Xa: макс. 65°C, для бетона: макс. 50°C
        /// </remarks>
        public const double MaxSupplyTemperature = 90.0;

        /// <summary>
        /// Максимальная температура подачи для PE-Xa труб, °C
        /// </summary>
        public const double MaxSupplyTemperaturePEXa = 65.0;

        /// <summary>
        /// Максимальная температура подачи для бетонной стяжки, °C
        /// </summary>
        public const double MaxSupplyTemperatureConcrete = 50.0;

        /// <summary>
        /// Минимальный температурный перепад, К
        /// </summary>
        public const double MinDeltaT = 1.0;

        /// <summary>
        /// Максимальный температурный перепад, К
        /// </summary>
        public const double MaxDeltaT = 30.0;

        #endregion

        #region Ограничения скорости ветра

        /// <summary>
        /// Максимальная скорость ветра, м/с
        /// </summary>
        public const double MaxWindSpeed = 50.0;

        #endregion

        #region Ограничения интенсивности снегопада

        /// <summary>
        /// Максимальная интенсивность снегопада, мм/ч
        /// </summary>
        public const double MaxSnowfallIntensity = 20.0;

        #endregion

        #region Режимы работы (температура поверхности)

        /// <summary>
        /// Температура поверхности для режима "Таяние", °C
        /// </summary>
        public const int SurfaceTempMelting = 2;

        /// <summary>
        /// Температура поверхности для режима "Предотвращение", °C
        /// </summary>
        public const int SurfaceTempPrevention = 0;

        /// <summary>
        /// Температура поверхности для режима "Антилёд", °C
        /// </summary>
        public const int SurfaceTempAntiIce = -2;

        #endregion
    }
}