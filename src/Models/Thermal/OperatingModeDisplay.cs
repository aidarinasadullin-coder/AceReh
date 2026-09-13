namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Единый источник человекочитаемых подписей режима работы
    /// (план 2026-09-13, решение V3): Results и PDF «Результаты расчёта»
    /// берут текст отсюда, а не из <c>mode.ToString()</c>.
    /// </summary>
    public static class OperatingModeDisplay
    {
        /// <summary>
        /// Человекочитаемая подпись режима: пресеты — по семантическому имени,
        /// ручные значения — «Пользовательский (+N °C)»;
        /// неопределённое значение — прочерк (как раньше в PDF).
        /// </summary>
        public static string ToDisplayText(this OperatingMode mode) => mode switch
        {
            OperatingMode.AntiIcing => "Антиобледенение (+3 °C)",
            OperatingMode.Melting => "Таяние (+5 °C)",
            OperatingMode.Intensive => "Интенсивное (+7 °C)",
            OperatingMode.Manual1 => "Пользовательский (+1 °C)",
            OperatingMode.Manual2 => "Пользовательский (+2 °C)",
            OperatingMode.Manual4 => "Пользовательский (+4 °C)",
            OperatingMode.Manual6 => "Пользовательский (+6 °C)",
            _ => "—"
        };
    }
}
