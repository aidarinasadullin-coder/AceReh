using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Калькулятор режима течения и коэффициента трения
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для:
    /// - Определения режима течения по числу Рейнольдса
    /// - Расчёта коэффициента трения λ для разных режимов
    /// 
    /// Режимы течения:
    /// - Ламинарный: Re &lt; 2300
    /// - Переходный: 2300 ≤ Re ≤ 4000
    /// - Турбулентный: Re &gt; 4000
    /// 
    /// Формулы взяты из docs/Formulas_Snegotayanie.md, раздел 11.7
    /// </remarks>
    public static class FlowRegimeCalculator
    {
        /// <summary>
        /// Граница ламинарного режима
        /// </summary>
        public const double LaminarBoundary = 2300;
        
        /// <summary>
        /// Граница турбулентного режима
        /// </summary>
        public const double TurbulentBoundary = 4000;
        
        /// <summary>
        /// Шероховатость PE-Xa труб, мм
        /// </summary>
        public const double PEXaRoughness = 0.007;
        
        /// <summary>
        /// Определить режим течения по числу Рейнольдса
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <returns>Режим течения</returns>
        public static FlowRegime DetermineFlowRegime(double reynoldsNumber)
        {
            if (reynoldsNumber < LaminarBoundary)
                return FlowRegime.Laminar;
            else if (reynoldsNumber <= TurbulentBoundary)
                return FlowRegime.Transitional;
            else
                return FlowRegime.Turbulent;
        }
        
        /// <summary>
        /// Проверить, является ли режим ламинарным
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <returns>true, если режим ламинарный</returns>
        public static bool IsLaminar(double reynoldsNumber)
        {
            return reynoldsNumber < LaminarBoundary;
        }
        
        /// <summary>
        /// Проверить, является ли режим переходным
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <returns>true, если режим переходный</returns>
        public static bool IsTransitional(double reynoldsNumber)
        {
            return reynoldsNumber >= LaminarBoundary && reynoldsNumber <= TurbulentBoundary;
        }
        
        /// <summary>
        /// Проверить, является ли режим турбулентным
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <returns>true, если режим турбулентный</returns>
        public static bool IsTurbulent(double reynoldsNumber)
        {
            return reynoldsNumber > TurbulentBoundary;
        }
        
        /// <summary>
        /// Рассчитать коэффициент трения для ламинарного режима
        /// Формула Пуазейля: λ = 64 / Re
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <returns>Коэффициент трения λ</returns>
        /// <exception cref="System.ArgumentException">Если Re ≤ 0</exception>
        public static double CalculateLaminarFrictionFactor(double reynoldsNumber)
        {
            if (reynoldsNumber <= 0)
                throw new System.ArgumentException("Число Рейнольдса должно быть положительным", nameof(reynoldsNumber));
            
            return 64.0 / reynoldsNumber;
        }
        
        /// <summary>
        /// Рассчитать коэффициент трения для переходного режима
        /// Линейная интерполяция между ламинарным и турбулентным
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса (2300-4000)</param>
        /// <param name="innerDiameter_mm">Внутренний диаметр трубы, мм</param>
        /// <param name="roughness_mm">Шероховатость трубы, мм</param>
        /// <returns>Коэффициент трения λ</returns>
        /// <exception cref="System.ArgumentException">Если Re вне диапазона 2300-4000</exception>
        public static double CalculateTransitionalFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm)
        {
            if (reynoldsNumber < LaminarBoundary || reynoldsNumber > TurbulentBoundary)
                throw new System.ArgumentException(
                    $"Число Рейнольдса должно быть в диапазоне [{LaminarBoundary}, {TurbulentBoundary}]",
                    nameof(reynoldsNumber));
            
            // Коэффициент трения на границе ламинарного режима
            double lambda_lam = CalculateLaminarFrictionFactor(LaminarBoundary);
            
            // Коэффициент трения на границе турбулентного режима
            double lambda_turb = CalculateTurbulentFrictionFactor(TurbulentBoundary, innerDiameter_mm, roughness_mm);
            
            // Линейная интерполяция
            double ratio = (reynoldsNumber - LaminarBoundary) / (TurbulentBoundary - LaminarBoundary);
            return lambda_lam + ratio * (lambda_turb - lambda_lam);
        }
        
        /// <summary>
        /// Рассчитать коэффициент трения для турбулентного режима
        /// Формула Колбрука-Уайта (итерационное решение)
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса (&gt; 4000)</param>
        /// <param name="innerDiameter_mm">Внутренний диаметр трубы, мм</param>
        /// <param name="roughness_mm">Шероховатость трубы, мм</param>
        /// <returns>Коэффициент трения λ</returns>
        /// <exception cref="System.ArgumentException">Если Re ≤ 4000</exception>
        public static double CalculateTurbulentFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm)
        {
            if (reynoldsNumber < TurbulentBoundary)
                throw new System.ArgumentException(
                    $"Число Рейнольдса должно быть не менее {TurbulentBoundary}",
                    nameof(reynoldsNumber));
            
            // Начальное приближение (формула Блазиуса)
            double lambda = 0.316 / System.Math.Pow(reynoldsNumber, 0.25);
            
            // Итерационное решение формулы Колбрука-Уайта
            // 1 / √λ = -2 × lg(ε / (3.7 × di) + 2.51 / (Re × √λ))
            
            for (int i = 0; i < 20; i++)
            {
                double sqrtLambda = System.Math.Sqrt(lambda);
                double term1 = roughness_mm / (3.7 * innerDiameter_mm);
                double term2 = 2.51 / (reynoldsNumber * sqrtLambda);
                
                double newLambda = System.Math.Pow(-2 * System.Math.Log10(term1 + term2), -2);
                
                if (System.Math.Abs(newLambda - lambda) < 1e-10)
                    break;
                
                lambda = newLambda;
            }
            
            return lambda;
        }
        
        /// <summary>
        /// Рассчитать коэффициент трения для любого режима
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <param name="innerDiameter_mm">Внутренний диаметр трубы, мм</param>
        /// <param name="roughness_mm">Шероховатость трубы, мм (по умолчанию 0.007 мм для PE-Xa)</param>
        /// <returns>Коэффициент трения λ</returns>
        public static double CalculateFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm = PEXaRoughness)
        {
            var regime = DetermineFlowRegime(reynoldsNumber);
            
            return regime switch
            {
                FlowRegime.Laminar => CalculateLaminarFrictionFactor(reynoldsNumber),
                FlowRegime.Transitional => CalculateTransitionalFrictionFactor(
                    reynoldsNumber, innerDiameter_mm, roughness_mm),
                FlowRegime.Turbulent => CalculateTurbulentFrictionFactor(
                    reynoldsNumber, innerDiameter_mm, roughness_mm),
                _ => throw new System.ArgumentOutOfRangeException()
            };
        }
        
        /// <summary>
        /// Получить описание режима течения
        /// </summary>
        /// <param name="regime">Режим течения</param>
        /// <returns>Текстовое описание режима</returns>
        public static string GetFlowRegimeDescription(FlowRegime regime)
        {
            return regime switch
            {
                FlowRegime.Laminar => "Ламинарный режим (Re < 2300). Плавное, упорядоченное движение жидкости слоями.",
                FlowRegime.Transitional => "Переходный режим (2300 ≤ Re ≤ 4000). Неустойчивый режим между ламинарным и турбулентным.",
                FlowRegime.Turbulent => "Турбулентный режим (Re > 4000). Хаотичное движение жидкости с вихрями.",
                _ => "Неизвестный режим"
            };
        }
        
        /// <summary>
        /// Получить рекомендации по режиму течения
        /// </summary>
        /// <param name="regime">Режим течения</param>
        /// <returns>Текстовые рекомендации</returns>
        public static string GetFlowRegimeRecommendation(FlowRegime regime)
        {
            return regime switch
            {
                FlowRegime.Laminar => "Рекомендуется увеличить расход или уменьшить диаметр трубы для перехода в турбулентный режим.",
                FlowRegime.Transitional => "ВНИМАНИЕ: Переходный режим нестабилен. Рекомендуется изменить параметры для обеспечения стабильного течения.",
                FlowRegime.Turbulent => "Оптимальный режим для теплообмена. Рекомендуется поддерживать Re > 4000.",
                _ => ""
            };
        }
    }
}