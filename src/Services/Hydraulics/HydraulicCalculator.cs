using System;
using System.Collections.Generic;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Реализация калькулятора гидравлического расчёта
    /// </summary>
    /// <remarks>
    /// Выполняет расчёт гидравлических параметров контура:
    /// - Скорость потока
    /// - Число Рейнольдса
    /// - Режим течения
    /// - Коэффициент трения λ
    /// - Потери давления
    /// 
    /// Формулы взяты из docs/Formulas_Snegotayanie.md, раздел 11
    /// </remarks>
    public class HydraulicCalculator : IHydraulicCalculator
    {
        private readonly IGlycolDataService _glycolService;
        private readonly HydraulicValidator _validator;

        /// <summary>
        /// Создать экземпляр калькулятора
        /// </summary>
        /// <param name="glycolService">Сервис свойств гликолей</param>
        public HydraulicCalculator(IGlycolDataService glycolService)
        {
            _glycolService = glycolService ?? throw new ArgumentNullException(nameof(glycolService));
            _validator = new HydraulicValidator();
        }

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
        public double CalculateVelocity(double flowRate_L_h, double innerDiameter_mm)
        {
            if (flowRate_L_h <= 0)
                throw new ArgumentException("Расход должен быть положительным", nameof(flowRate_L_h));
            
            if (innerDiameter_mm <= 0)
                throw new ArgumentException("Диаметр должен быть положительным", nameof(innerDiameter_mm));

            // Площадь сечения трубы, мм²
            double area_mm2 = Math.PI * Math.Pow(innerDiameter_mm, 2) / 4.0;
            
            // Скорость: v [л/ч] × 1000 [мм³/л] / (3600 [с/ч] × площадь [мм²])
            double velocity = flowRate_L_h * 1000.0 / (3600.0 * area_mm2);
            
            return velocity;
        }

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
        public double CalculateReynoldsNumber(
            double velocity_m_s, 
            double innerDiameter_mm, 
            double kinematicViscosity_mm2_s)
        {
            if (velocity_m_s <= 0)
                throw new ArgumentException("Скорость должна быть положительной", nameof(velocity_m_s));
            
            if (innerDiameter_mm <= 0)
                throw new ArgumentException("Диаметр должен быть положительным", nameof(innerDiameter_mm));
            
            if (kinematicViscosity_mm2_s <= 0)
                throw new ArgumentException("Вязкость должна быть положительной", nameof(kinematicViscosity_mm2_s));

            // Re = w × di / ν
            // При di в мм и ν в мм²/с: Re = 1000 × w × di / ν
            double re = 1000.0 * velocity_m_s * innerDiameter_mm / kinematicViscosity_mm2_s;
            
            return re;
        }

        /// <summary>
        /// Определить режим течения
        /// </summary>
        /// <param name="reynoldsNumber">Число Рейнольдса</param>
        /// <returns>Режим течения</returns>
        public FlowRegime DetermineFlowRegime(double reynoldsNumber)
        {
            return FlowRegimeCalculator.DetermineFlowRegime(reynoldsNumber);
        }

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
        public double CalculateFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm)
        {
            return FlowRegimeCalculator.CalculateFrictionFactor(reynoldsNumber, innerDiameter_mm, roughness_mm);
        }

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
        public double CalculatePressureLossPerMeter(
            double velocity_m_s, 
            double density_kg_m3, 
            double frictionFactor, 
            double innerDiameter_mm)
        {
            if (velocity_m_s < 0)
                throw new ArgumentException("Скорость не может быть отрицательной", nameof(velocity_m_s));
            
            if (density_kg_m3 <= 0)
                throw new ArgumentException("Плотность должна быть положительной", nameof(density_kg_m3));
            
            if (frictionFactor <= 0)
                throw new ArgumentException("Коэффициент трения должен быть положительным", nameof(frictionFactor));
            
            if (innerDiameter_mm <= 0)
                throw new ArgumentException("Диаметр должен быть положительным", nameof(innerDiameter_mm));

            // R = (w² × ρ × λ) / (2 × di) × 1000
            // При di в мм: R = 1000 × (w² × ρ × λ) / (2 × di)
            double pressureLoss = 1000.0 * Math.Pow(velocity_m_s, 2) * density_kg_m3 * frictionFactor 
                / (2.0 * innerDiameter_mm);
            
            return pressureLoss;
        }

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
        /// Δp = (v / 1000 / 1.2)² × 100 × ρ  [Па]
        /// 
        /// IV 1¼" (Kv = 1.45 м³/ч):
        /// Δp = (v / 1000 / 1.45)² × 100 × ρ  [Па]
        /// 
        /// IV 1½" (Kv = 1.5 м³/ч):
        /// Δp = (v / 1000 / 1.5)² × 100 × ρ  [Па]
        /// </remarks>
        public double CalculateValvePressureLoss(
            double flowRate_L_h, 
            double density_kg_m3, 
            CollectorType collectorType)
        {
            if (flowRate_L_h < 0)
                throw new ArgumentException("Расход не может быть отрицательным", nameof(flowRate_L_h));
            
            if (density_kg_m3 <= 0)
                throw new ArgumentException("Плотность должна быть положительной", nameof(density_kg_m3));

            // Kv для разных типов коллекторов
            double kv = collectorType switch
            {
                CollectorType.HKV => 1.2,
                CollectorType.IV => 1.45, // DN25 по умолчанию
                _ => 1.2
            };

            // Δp = (v / 1000 / Kv)² × 100 × ρ
            // Где v - расход в л/ч, Kv - коэффициент пропускной способности в м³/ч
            double pressureLoss = Math.Pow(flowRate_L_h / 1000.0 / kv, 2) * 100.0 * density_kg_m3;
            
            return pressureLoss;
        }

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
        public HydraulicResult Calculate(HydraulicParameters parameters)
        {
            // Валидация параметров
            var validationResult = _validator.Validate(parameters);
            if (!validationResult.IsValid)
            {
                return new HydraulicResult
                {
                    IsValid = false,
                    ValidationErrors = validationResult.Errors.ToArray()
                };
            }

            // Получение свойств теплоносителя
            var glycolProps = _glycolService.GetProperties(
                parameters.GlycolType,
                parameters.GlycolConcentration,
                parameters.MeanTemperature);

            // Расчёт
            double flowRate = parameters.CircuitFlowRate;
            double di = parameters.InnerDiameter;

            double velocity = CalculateVelocity(flowRate, di);
            double re = CalculateReynoldsNumber(velocity, di, glycolProps.KinematicViscosity);
            var regime = DetermineFlowRegime(re);
            double lambda = CalculateFrictionFactor(re, di, parameters.Roughness);
            double pressureLossPerMeter = CalculatePressureLossPerMeter(
                velocity, glycolProps.Density, lambda, di);

            double circuitPressureLoss = parameters.CircuitLength * pressureLossPerMeter;
            double supplyPressureLoss = parameters.SupplyLength * pressureLossPerMeter;
            double totalPipePressureLoss = circuitPressureLoss + supplyPressureLoss;

            double valvePressureLoss = CalculateValvePressureLoss(
                flowRate, glycolProps.Density, CollectorType.HKV);

            double totalPressureLoss = totalPipePressureLoss + valvePressureLoss;

            // Валидация результата
            var resultValidation = _validator.ValidateResult(new HydraulicResult
            {
                Velocity = velocity,
                ReynoldsNumber = re,
                FlowRegime = regime,
                PressureLossPerMeter = pressureLossPerMeter
            });

            return new HydraulicResult
            {
                Velocity = velocity,
                ReynoldsNumber = re,
                FlowRegime = regime,
                FrictionFactor = lambda,
                PressureLossPerMeter = pressureLossPerMeter,
                CircuitPressureLoss = circuitPressureLoss,
                SupplyPressureLoss = supplyPressureLoss,
                TotalPipePressureLoss = totalPipePressureLoss,
                ValvePressureLoss = valvePressureLoss,
                TotalPressureLoss = totalPressureLoss,
                CircuitFlowRate = flowRate,
                IsValid = true,
                Warnings = resultValidation.Warnings.ToArray()
            };
        }

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
        public List<CircuitResult> CalculateBalancing(List<CircuitResult> circuits)
        {
            if (circuits == null || circuits.Count == 0)
                return new List<CircuitResult>();

            // Найти контур с максимальными потерями
            double maxPressureLoss = 0;
            foreach (var circuit in circuits)
            {
                if (circuit.TotalPressureLoss > maxPressureLoss)
                {
                    maxPressureLoss = circuit.TotalPressureLoss;
                }
            }

            // Рассчитать дросселирование для каждого контура
            foreach (var circuit in circuits)
            {
                circuit.Throttling = maxPressureLoss - circuit.TotalPressureLoss;
                circuit.IsReferenceCircuit = Math.Abs(circuit.TotalPressureLoss - maxPressureLoss) < 0.01;

                // Определить настройку вентиля (1-8)
                circuit.RecommendedValveSetting = CalculateValveSetting(circuit.Throttling);
            }

            return circuits;
        }

        /// <summary>
        /// Определить настройку вентиля по дросселированию
        /// </summary>
        /// <param name="throttling_Pa">Дросселирование, Па</param>
        /// <returns>Настройка вентиля (1-8)</returns>
        private static int CalculateValveSetting(double throttling_Pa)
        {
            // Таблица настроек вентиля (примерная)
            // Настройка 1: минимальное сопротивление
            // Настройка 8: максимальное сопротивление

            double throttling_mbar = throttling_Pa / 100.0;

            if (throttling_mbar <= 0)
                return 1;
            else if (throttling_mbar <= 40)
                return 2;
            else if (throttling_mbar <= 80)
                return 3;
            else if (throttling_mbar <= 120)
                return 4;
            else if (throttling_mbar <= 160)
                return 5;
            else if (throttling_mbar <= 200)
                return 6;
            else if (throttling_mbar <= 240)
                return 7;
            else
                return 8;
        }
    }
}