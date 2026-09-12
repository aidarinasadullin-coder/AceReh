namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Единые правила дискретизации климатических зон (план 2026-09-12, часть B).
    /// Автоматика города: t₅ ≥ −27 → −10; −37 < t₅ < −27 → −15; t₅ ≤ −37 → −20.
    /// Зона = ближайшая колонка итоговой расчётной температуры {−10, −15, −20}
    /// (границы −12.5 / −17.5; равенство трактуется в холодную сторону).
    /// M20_Plus не порождается — legacy-значение для чтения старых .smc.
    /// Используется каноническим состоянием, ViewModel и PDF-экспортом.
    /// </summary>
    public static class ClimateZoneRules
    {
        /// <summary>
        /// Автоматическая расчётная температура города по холодной пятидневке (без повышенных требований)
        /// </summary>
        public static double AutoAirTemperature(double t5days)
        {
            if (t5days >= -27.0)
            {
                return -10.0;
            }

            return t5days > -37.0 ? -15.0 : -20.0;
        }

        /// <summary>
        /// Ступень «Повышенных требований»: следующая более холодная колонка.
        /// Для автоматики −20 ступени нет (возвращает null).
        /// </summary>
        public static double? StepDown(double autoAirTemperature)
        {
            return autoAirTemperature switch
            {
                -10.0 => -15.0,
                -15.0 => -20.0,
                _ => null
            };
        }

        /// <summary>
        /// Зона по итоговой расчётной температуре (ближайшая колонка; tie — холодная сторона)
        /// </summary>
        public static ClimateZone FromTemperature(double airTemperature)
        {
            if (airTemperature > -12.5)
            {
                return ClimateZone.Zone_M10;
            }

            return airTemperature > -17.5 ? ClimateZone.Zone_M15 : ClimateZone.Zone_M20;
        }

        /// <summary>
        /// Человекочитаемый формат зоны: «M10 · колонка −10 °C» (UI и PDF)
        /// </summary>
        public static string ZoneText(ClimateZone zone)
        {
            return zone switch
            {
                ClimateZone.Zone_M10 => "M10 · колонка −10 °C",
                ClimateZone.Zone_M15 => "M15 · колонка −15 °C",
                ClimateZone.Zone_M20 => "M20 · колонка −20 °C",
                ClimateZone.Zone_M20_Plus => "M20 Plus · колонка −20 °C",
                _ => string.Empty
            };
        }
    }
}
