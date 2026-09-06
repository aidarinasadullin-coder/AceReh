namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Поставщик детальных тепловых величин для детального отчёта (ADR-010).
    /// </summary>
    public interface IThermalReportDataProvider
    {
        /// <summary>
        /// Получить детальные тепловые величины: из канонического снимка
        /// <c>ProjectSession.ThermalState</c> либо (при нулевых runtime-полях,
        /// DEC-T08) ровно одним контрольным пересчётом существующего
        /// <c>ThermalCalculator</c> по текущим входам. Результат пересчёта
        /// в каноническое состояние не пишется.
        /// </summary>
        ThermalReportDetail Provide();
    }
}
