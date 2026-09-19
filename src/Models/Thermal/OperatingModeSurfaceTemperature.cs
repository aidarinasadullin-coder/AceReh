namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Явная карта «режим работы → температура поверхности t_П» (ADR-016)
    /// </summary>
    /// <remarks>
    /// Единственная точка преобразования <see cref="OperatingMode"/> в
    /// температуру поверхности: числовое значение члена enum равно t_П, °C
    /// (контракт заморожен — расчёт, Undo/Redo и отчёты; диапазон 1..7 пинится
    /// ThermalConstantsPinTests). Ранее преобразование выполнялось
    /// неявным приведением <c>(int)mode</c> в ThermalCalculator.
    /// </remarks>
    public static class OperatingModeSurfaceTemperature
    {
        /// <summary>
        /// Температура поверхности t_П, °C, для режима работы
        /// </summary>
        public static int ToSurfaceTemperature(this OperatingMode mode) => (int)mode;
    }
}
