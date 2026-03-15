using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Интерфейс калькулятора гидравлического расчёта
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для расчёта гидравлических параметров:
    /// - Скорость потока
    /// - Число Рейнольдса
    /// - Режим течения
    /// - Коэффициент трения λ
    /// - Потери давления
    /// 
    /// Формулы взяты из docs/Formulas_Snegotayanie.md, раздел 11.
    /// </remarks>
    public interface IHydraulicCalculator
    {
        /// <summary>
        /// Рассчитать скорость потока
        /// </summary>
        /// <param name="flowRate_L_h">Расход, л/ч</param>
        /// <param name="innerDiameter_mm">Внутренний диаметр, мм</param>
        /// <returns>Скорость потока, м/с</returns>
        /// <remarks>
        /// Формула: w = v × 1000 / (3600 × π × di² / 4)
        /// Где:
        /// - v — расход, л/ч
        /// - di — внутренний диаметр, мм
        /// 
        /// Рекомендуемый диапазон: 0.2-1.5 м/с
        /// </remarks>
        double CalculateVelocity(double flowRate_L_h, double innerDiameter_mm);
        
        /// <summary>
        /// Рассчитать число Рейнольдса
        /// </summary>
        /// <param name="velocity_m_s">Скорость потока, м/с</param>
        /// <param name="innerDiameter_mm">Внутренний диаметр, мм</param>
        /// <param name="kinematicViscosity_mm2_s">Кинематическая вязкость, мм²/с</param>
        /// <returns>Число Рейнольдса (безразмерное)</returns>
        /// <remarks>
        /// Формула: Re = 1000 × w × di / ν
        /// Где:
        /// - w — скорость, м/с
        /// - di — внутренний диаметр, мм
        /// - ν — кинематическая вязкость, мм²/с
        /// 
        /// Режимы течения:
        /// - Re &lt; 2300 — ламинарный
        /// - 2300 ≤ Re ≤ 4000 — переходный
        /// - Re &gt; 4000 — турбулентный
        /// </remarks>
        double CalculateReynoldsNumber(
            double velocity_m_s, 
            double innerDiameter_mm, 
            double kinematicViscosity_mm2_s);
        
        /// <summary>
        /// Определить режим течения
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <returns>Режим течения</returns>
        /// <remarks>
        /// Критерии:
        /// - Re &lt; 2300 → Laminar
        /// - 2300 ≤ Re ≤ 4000 → Transitional
        /// - Re &gt; 4000 → Turbulent
        /// </remarks>
        FlowRegime DetermineFlowRegime(double reynoldsNumber);
        
        /// <summary>
        /// Рассчитать коэффициент гидравлического трения λ
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <param name="innerDiameter_mm">Внутренний диаметр, мм</param>
        /// <param name="roughness_mm">Шероховатость трубы, мм</param>
        /// <returns>Коэффициент трения λ (безразмерный)</returns>
        /// <remarks>
        /// Формулы по режимам:
        /// 
        /// Ламинарный (Re &lt; 2300):
        /// λ = 64 / Re (формула Пуазейля)
        /// 
        /// Переходный (2300 ≤ Re ≤ 4000):
        /// Линейная интерполяция между λ_lam и λ_turb
        /// 
        /// Турбулентный (Re &gt; 4000):
        /// 1 / √λ = -2 × lg(ε / (3.7 × di) + 2.51 / (Re × √λ))
        /// (формула Колбрука-Уайта, решается итерационно)
        /// 
        /// Шероховатость PE-Xa: 0.007 мм
        /// </remarks>
        double CalculateFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm);
        
        /// <summary>
        /// Рассчитать удельные потери давления
        /// </summary>
        /// <param name="velocity_m_s">Скорость потока, м/с</param>
        /// <param name="density_kg_m3">Плотность, кг/м³</param>
        /// <param name="frictionFactor">Коэффициент трения λ</param>
        /// <param name="innerDiameter_mm">Внутренний диаметр, мм</param>
        /// <returns>Удельные потери давления, Па/м</returns>
        /// <remarks>
        /// Формула: R = 1000 × (w² × ρ × λ) / (2 × di)
        /// Где:
        /// - w — скорость, м/с
        /// - ρ — плотность, кг/м³
        /// - λ — коэффициент трения
        /// - di — внутренний диаметр, мм
        /// 
        /// Ограничение: R ≤ 300 Па/м
        /// </remarks>
        double CalculatePressureLossPerMeter(
            double velocity_m_s, 
            double density_kg_m3, 
            double frictionFactor, 
            double innerDiameter_mm);
        
        /// <summary>
        /// Рассчитать потери давления в вентиле коллектора
        /// </summary>
        /// <param name="flowRate_L_h">Расход, л/ч</param>
        /// <param name="density_kg_m3">Плотность, кг/м³</param>
        /// <param name="collectorType">Тип коллектора</param>
        /// <returns>Потери давления в вентиле, Па</returns>
        /// <remarks>
        /// Формулы по типам коллекторов:
        /// 
        /// HKV-D (Kv = 1.2 м³/ч):
        /// Δp = (v / 1000 / 1.2)² × 100000 × ρ
        /// 
        /// IV 1¼" (Kv = 1.45 м³/ч):
        /// Δp = (v / 1000 / 1.45)² × 100000 × ρ
        /// 
        /// IV 1½" (Kv = 1.5 м³/ч):
        /// Δp = (v / 1000 / 1.5)² × 100000 × ρ
        /// </remarks>
        double CalculateValvePressureLoss(
            double flowRate_L_h, 
            double density_kg_m3, 
            CollectorType collectorType);
        
        /// <summary>
        /// Выполнить полный гидравлический расчёт контура
        /// </summary>
        /// <param name="parameters">Параметры расчёта</param>
        /// <returns>Результат расчёта</returns>
        /// <remarks>
        /// Выполняет полный расчёт:
        /// 1. Скорость потока
        /// 2. Число Рейнольдса
        /// 3. Режим течения
        /// 4. Коэффициент трения λ
        /// 5. Удельные потери давления
        /// 6. Потери в трубе
        /// 7. Потери в вентиле
        /// 8. Суммарные потери
        /// </remarks>
        HydraulicResult Calculate(HydraulicParameters parameters);
        
        /// <summary>
        /// Рассчитать балансировку контуров
        /// </summary>
        /// <param name="circuits">Список контуров с результатами расчёта</param>
        /// <returns>Список контуров с рассчитанным дросселированием</returns>
        /// <remarks>
        /// Алгоритм балансировки:
        /// 1. Определить контур с максимальными потерями (Δp_max)
        /// 2. Для каждого контура рассчитать дросселирование:
        ///    zu_drosseln = Δp_max - Δp_контур - Δp_вентиль
        /// 3. Определить настройку вентиля (1-8)
        /// </remarks>
        System.Collections.Generic.List<CircuitResult> CalculateBalancing(System.Collections.Generic.List<CircuitResult> circuits);
    }
}