namespace SnowMeltingCalculator.Core.Constants
{
    /// <summary>
    /// Константы гидравлического расчёта
    /// </summary>
    /// <remarks>
    /// Все константы соответствуют методике расчёта РЕХАУ.
    /// См. docs/Formulas_Snegotayanie.md
    /// </remarks>
    public static class HydraulicsConstants
    {
        #region Ограничения РЕХАУ

        /// <summary>
        /// Максимально допустимые потери давления, Па
        /// </summary>
        /// <remarks>
        /// Ограничение РЕХАУ: 320 мбар = 32000 Па
        /// </remarks>
        public const int MaxPressureLoss_Pa = 32000;

        /// <summary>
        /// Максимально допустимые потери давления, мбар
        /// </summary>
        public const double MaxPressureLoss_mbar = 320.0;

        /// <summary>
        /// Максимальная длина контура, м
        /// </summary>
        /// <remarks>
        /// Ограничение РЕХАУ для систем снеготаяния.
        /// </remarks>
        public const int MaxCircuitLength_m = 120;

        /// <summary>
        /// Минимальная скорость потока, м/с
        /// </summary>
        /// <remarks>
        /// Для предотвращения воздушных пробок.
        /// </remarks>
        public const double MinVelocity = 0.5;

        /// <summary>
        /// Максимальная скорость потока, м/с
        /// </summary>
        /// <remarks>
        /// Для предотвращения шума и эрозии.
        /// </remarks>
        public const double MaxVelocity = 2.0;

        #endregion

        #region Коэффициенты Kv для вентилей

        /// <summary>
        /// Kv для вентиля HKV-D (по умолчанию)
        /// </summary>
        /// <remarks>
        /// Kv = 1.2 м³/ч для HKV-D
        /// </remarks>
        public const double Kv_HKV_D = 1.2;

        /// <summary>
        /// Kv для вентиля IV DN25
        /// </summary>
        /// <remarks>
        /// Kv = 1.45 м³/ч для IV DN25
        /// </remarks>
        public const double Kv_IV_DN25 = 1.45;

        /// <summary>
        /// Kv для вентиля IV DN32
        /// </summary>
        /// <remarks>
        /// Kv = 1.5 м³/ч для IV DN32
        /// </remarks>
        public const double Kv_IV_DN32 = 1.5;

        /// <summary>
        /// Kv для вентиля IV DN40
        /// </summary>
        /// <remarks>
        /// Kv = 2.0 м³/ч для IV DN40
        /// </remarks>
        public const double Kv_IV_DN40 = 2.0;

        #endregion

        #region Обороты вентилей

        /// <summary>
        /// Максимальное число оборотов для вентиля HKV-D
        /// </summary>
        public const int MaxTurns_HKV_D = 4;

        /// <summary>
        /// Максимальное число оборотов для вентиля IV
        /// </summary>
        public const int MaxTurns_IV = 4;

        #endregion

        #region Коэффициенты для расчёта потерь

        /// <summary>
        /// Коэффициент для расчёта потерь в распределителе (HKV-D)
        /// </summary>
        /// <remarks>
        /// DpVerteiler = 15000 × (ρ/2) × v² для HKV-D
        /// </remarks>
        public const double DistributorLossCoefficient = 15000.0;

        /// <summary>
        /// Коэффициент для расчёта потерь в вентиле (HKV-D)
        /// </summary>
        /// <remarks>
        /// DpVent = 15000 × (ρ/2) × v² для HKV-D
        /// </remarks>
        public const double ValveLossCoefficient_HKV_D = 15000.0;

        /// <summary>
        /// Коэффициент для конвертации давления
        /// </summary>
        /// <remarks>
        /// 1 бар = 100000 Па
        /// </remarks>
        public const double PressureConversionFactor = 100000.0;

        /// <summary>
        /// Коэффициент для конвертации расхода
        /// </summary>
        /// <remarks>
        /// Конвертация л/ч в м³/ч
        /// </remarks>
        public const double FlowRateConversionFactor = 1000.0;

        #endregion

        #region Число Рейнольдса

        /// <summary>
        /// Граница ламинарного течения (Re &lt; 2300)
        /// </summary>
        public const double ReynoldsLaminar = 2300.0;

        /// <summary>
        /// Граница переходного течения (2300 &lt; Re &lt; 4000)
        /// </summary>
        public const double ReynoldsTransitional = 4000.0;

        #endregion

        #region Константы для расчёта расхода

        /// <summary>
        /// Коэффициент для расчёта массового расхода
        /// </summary>
        /// <remarks>
        /// ṁ = q_total / (c_p / 3.6) / ΔT
        /// </remarks>
        public const double MassFlowCoefficient = 3.6;

        /// <summary>
        /// Коэффициент для конвертации массового расхода в объёмный
        /// </summary>
        /// <remarks>
        /// V_dot = ṁ / ρ × 1000
        /// </remarks>
        public const double VolumeFlowCoefficient = 1000.0;

        #endregion

        #region Константы для расчёта скорости

        /// <summary>
        /// Коэффициент для расчёта скорости потока
        /// </summary>
        /// <remarks>
        /// v = FlowRate × 4000 / (3600 × π × d²)
        /// </remarks>
        public const double VelocityCoefficient = 4000.0;

        /// <summary>
        /// Коэффициент для конвертации секунд в часы
        /// </summary>
        public const double SecondsPerHour = 3600.0;

        #endregion

        #region Константы для расчёта числа Рейнольдса

        /// <summary>
        /// Коэффициент для расчёта числа Рейнольдса
        /// </summary>
        /// <remarks>
        /// Re = 1000 × v × d / ν
        /// </remarks>
        public const double ReynoldsCoefficient = 1000.0;

        #endregion

        #region Константы для расчёта потерь на трение

        /// <summary>
        /// Коэффициент для расчёта потерь на трение
        /// </summary>
        /// <remarks>
        /// R = 10000 × v² × ρ × λ / (2 × d) × 100
        /// </remarks>
        public const double FrictionLossCoefficient = 10000.0;

        /// <summary>
        /// Коэффициент для конвертации в Па/м
        /// </summary>
        public const double PressurePerMeterFactor = 100.0;

        #endregion

        #region Константы для расчёта Kv

        /// <summary>
        /// Формула для расчёта Kv: Kv = Q / √(Δp / ρ)
        /// </summary>
        /// <remarks>
        /// Q - расход в м³/ч
        /// Δp - перепад давления в бар
        /// ρ - плотность в г/см³
        /// </remarks>
        public const double KvFormulaDenominator = 100000.0; // Конвертация Па в бар

        #endregion

        #region Константы для расчёта мощности контура

        /// <summary>
        /// Коэффициент для расчёта длины на единицу площади
        /// </summary>
        /// <remarks>
        /// lengthPerArea = L_hk / (100 / pipeSpacing_cm)
        /// </remarks>
        public const double LengthPerAreaCoefficient = 100.0;

        #endregion
    }
}