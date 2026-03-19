namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Таблица соответствия длины трубы на 1 м² площади
    /// </summary>
    /// <remarks>
    /// Длина трубы на 1 м² = 100 / шаг_укладки_см
    /// Где шаг_укладки - расстояние между трубами в см
    /// 
    /// Например:
    /// - Шаг 150 мм (15 см): 100/15 = 6.67 м/м²
    /// - Шаг 200 мм (20 см): 100/20 = 5.00 м/м²
    /// - Шаг 250 мм (25 см): 100/25 = 4.00 м/м²
    /// - Шаг 300 мм (30 см): 100/30 = 3.33 м/м²
    /// </remarks>
    public static class PipeLengthPerArea
    {
        /// <summary>
        /// Рассчитать длину трубы на 1 м² для заданного шага
        /// </summary>
        /// <param name="pipeSpacing_cm">Шаг укладки в см</param>
        /// <returns>Длина трубы в м на 1 м² площади</returns>
        public static double Calculate(double pipeSpacing_cm)
        {
            return 100.0 / pipeSpacing_cm;
        }
        
        /// <summary>
        /// Рассчитать площадь контура по длине трубы
        /// </summary>
        /// <param name="pipeLength_m">Длина трубы в м</param>
        /// <param name="pipeSpacing_cm">Шаг укладки в см</param>
        /// <returns>Площадь в м²</returns>
        public static double CalculateArea(double pipeLength_m, double pipeSpacing_cm)
        {
            return pipeLength_m / (100.0 / pipeSpacing_cm);
        }
        
        /// <summary>
        /// Рассчитать длину трубы по площади контура
        /// </summary>
        /// <param name="area_m2">Площадь контура в м²</param>
        /// <param name="pipeSpacing_cm">Шаг укладки в см</param>
        /// <returns>Длина трубы в м</returns>
        public static double CalculateLength(double area_m2, double pipeSpacing_cm)
        {
            return area_m2 * (100.0 / pipeSpacing_cm);
        }
        
        /// <summary>
        /// Стандартные значения шага укладки (мм)
        /// </summary>
        public static readonly double[] StandardSpacings_mm = { 150, 200, 250, 300 };
        
        /// <summary>
        /// Стандартные значения шага укладки (см)
        /// </summary>
        public static readonly double[] StandardSpacings_cm = { 15, 20, 25, 30 };
        
        /// <summary>
        /// Получить длину трубы на 1 м² для стандартных шагов
        /// </summary>
        public static readonly (double Spacing_cm, double Length_m_per_m2)[] StandardValues = 
        {
            (15, 6.67),  // 150 мм
            (20, 5.00),  // 200 мм
            (25, 4.00),  // 250 мм
            (30, 3.33)   // 300 мм
        };
    }
}