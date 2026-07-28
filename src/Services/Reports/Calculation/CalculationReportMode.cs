namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Режим формирования детального расчётного отчёта.
    /// </summary>
    public enum CalculationReportMode
    {
        /// <summary>
        /// Рабочий режим (OperatingResult).
        /// </summary>
        Operating,

        /// <summary>
        /// Расчётный / холодный режим (DesignResult).
        /// </summary>
        DesignCold
    }
}
