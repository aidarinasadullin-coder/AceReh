namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Режим гидравлического расчёта
    /// </summary>
    public enum HydraulicMode
    {
        /// <summary>
        /// Расчёт при рабочей температуре теплоносителя
        /// (средняя температура подачи и обратки)
        /// </summary>
        OperatingTemperature,

        /// <summary>
        /// Расчёт при расчётной температуре (холодный пуск)
        /// (температура холодной пятидневки из климатологии)
        /// </summary>
        DesignTemperature
    }
}