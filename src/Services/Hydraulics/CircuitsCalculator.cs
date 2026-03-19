using System;
using System.Collections.Generic;
using System.Linq;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Реализация калькулятора контуров
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для расчёта гидравлических параметров
    /// таблицы контуров систем снеготаяния РЕХАУ.
    /// 
    /// Поддерживает:
    /// - Расчёт мощности контура Q_HK
    /// - Расчёт расхода теплоносителя V_dot
    /// - Расчёт при двух температурах (рабочая и расчётная)
    /// - Балансировку контуров на коллекторе
    /// - Подбор коллекторов РЕХАУ
    /// 
    /// Формулы взяты из docs/Formulas_Snegotayanie.md
    /// </remarks>
    public class CircuitsCalculator : ICircuitsCalculator
    {
        private readonly IGlycolDataService _glycolService;

        /// <summary>
        /// Создать калькулятор контуров
        /// </summary>
        /// <param name="glycolService">Сервис свойств гликоля</param>
        /// <exception cref="ArgumentNullException">Если glycolService равен null</exception>
        public CircuitsCalculator(IGlycolDataService glycolService)
        {
            _glycolService = glycolService ?? throw new ArgumentNullException(nameof(glycolService));
        }

        /// <summary>
        /// Рассчитать мощность контура Q_HK
        /// </summary>
        /// <param name="circuit">Контур для расчёта</param>
        /// <param name="q_up">Мощность вверх, Вт/м²</param>
        /// <param name="q_down">Мощность вниз, Вт/м²</param>
        /// <param name="pipeSpacing_cm">Шаг укладки трубы, см</param>
        /// <returns>Мощность контура, Вт</returns>
        /// <remarks>
        /// Формула: Q_HK = [(L_hk/(100/VA_hk)) + (L_zul/(100/VA_zul))×(q_zul/100)] × (q_up + q_down)
        /// 
        /// Где:
        /// - L_hk — длина контура (CircuitLength), м
        /// - VA_hk — шаг укладки (pipeSpacing_cm), см
        /// - L_zul — длина подводки (SupplyLength), м
        /// - VA_zul — шаг подводки (SupplySpacing_cm), см
        /// - q_zul — доля тепла от подводок (SupplyHeatPercent), %
        /// - q_up — мощность вверх, Вт/м²
        /// - q_down — мощность вниз, Вт/м²
        /// </remarks>
        public double CalculateCircuitPower(CircuitRow circuit, double q_up, double q_down, double pipeSpacing_cm)
        {
            if (circuit == null)
                throw new ArgumentNullException(nameof(circuit));

            if (q_up < 0)
                throw new ArgumentException("Мощность вверх не может быть отрицательной", nameof(q_up));

            if (q_down < 0)
                throw new ArgumentException("Мощность вниз не может быть отрицательной", nameof(q_down));

            if (pipeSpacing_cm <= 0)
                throw new ArgumentException("Шаг укладки должен быть положительным", nameof(pipeSpacing_cm));

            // Длина контура на единицу площади
            double lengthPerArea = circuit.CircuitLength / (100.0 / pipeSpacing_cm);

            // Длина подводки на единицу площади
            double supplyLengthPerArea = circuit.SupplyLength / (100.0 / circuit.SupplySpacing_cm);

            // Доля тепла от подводок
            double supplyHeatFactor = circuit.SupplyHeatPercent / 100.0;

            // Мощность контура
            double power = (lengthPerArea + supplyLengthPerArea * supplyHeatFactor) * (q_up + q_down);

            return power;
        }

        /// <summary>
        /// Рассчитать расход теплоносителя V_dot
        /// </summary>
        /// <param name="power">Мощность контура, Вт</param>
        /// <param name="deltaT">Температурный перепад, К</param>
        /// <param name="density">Плотность теплоносителя, кг/м³</param>
        /// <param name="specificHeat">Удельная теплоёмкость, кДж/(кг·К)</param>
        /// <returns>Расход, л/ч</returns>
        /// <remarks>
        /// Формула: V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)
        /// 
        /// Где:
        /// - Q_HK — мощность контура, Вт
        /// - ρ — плотность теплоносителя, кг/м³
        /// - c_p — удельная теплоёмкость, кДж/(кг·К)
        /// - ΔT — температурный перепад, К
        /// 
        /// Примечание: коэффициент 3.6 используется для перевода Вт в кДж/ч
        /// и получения результата в л/ч.
        /// </remarks>
        public double CalculateFlowRate(double power, double deltaT, double density, double specificHeat)
        {
            if (power <= 0)
                throw new ArgumentException("Мощность должна быть положительной", nameof(power));

            if (deltaT <= 0)
                throw new ArgumentException("Температурный перепад должен быть положительным", nameof(deltaT));

            if (density <= 0)
                throw new ArgumentException("Плотность должна быть положительной", nameof(density));

            if (specificHeat <= 0)
                throw new ArgumentException("Удельная теплоёмкость должна быть положительной", nameof(specificHeat));

            // V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)
            // Результат в л/ч
            double flowRate = power * 3.6 / (density * specificHeat * deltaT);

            return flowRate;
        }

        /// <summary>
        /// Рассчитать гидравлику контура при заданной температуре
        /// </summary>
        /// <param name="circuit">Контур для расчёта</param>
        /// <param name="temperature">Температура теплоносителя, °C</param>
        /// <param name="glycolProps">Свойства гликоля при температуре</param>
        /// <param name="innerDiameter">Внутренний диаметр трубы, мм</param>
        /// <param name="kv">Коэффициент пропускной способности вентиля, м³/ч</param>
        /// <returns>Результат расчёта при температуре</returns>
        /// <remarks>
        /// Рассчитывает:
        /// - Скорость потока v
        /// - Число Рейнольдса Re
        /// - Режим течения (ламинарный/переходный/турбулентный)
        /// - Коэффициент трения λ
        /// - Удельные потери R (Па/м)
        /// - Потери в трубе контура Δp_HK (Па)
        /// - Потери в трубе подводки Δp_Zul (Па)
        /// - Потери в вентиле Δp_Vent (Па)
        /// - Суммарные потери Δp_total (Па)
        /// 
        /// Формулы:
        /// - Скорость: v = V_dot × 4 / (3600 × π × d_inner²) × 1000
        /// - Число Рейнольдса: Re = 1000 × v × d_inner / ν
        /// - Коэффициент трения: зависит от режима (Пуазейль или Колбрук-Уайт)
        /// - Удельные потери: R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100
        /// - Потери в трубе: Δp = R × L
        /// - Потери в вентиле: Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ
        /// </remarks>
        public CircuitTemperatureResult CalculateAtTemperature(
            CircuitRow circuit,
            double temperature,
            GlycolProperties glycolProps,
            double innerDiameter,
            double kv)
        {
            if (circuit == null)
                throw new ArgumentNullException(nameof(circuit));

            if (glycolProps == null)
                throw new ArgumentNullException(nameof(glycolProps));

            if (innerDiameter <= 0)
                throw new ArgumentException("Внутренний диаметр должен быть положительным", nameof(innerDiameter));

            if (kv <= 0)
                throw new ArgumentException("Kv должен быть положительным", nameof(kv));

            var result = new CircuitTemperatureResult
            {
                Temperature = temperature,
                Density = glycolProps.Density,
                KinematicViscosity = glycolProps.KinematicViscosity
            };

            // Скорость потока: v = V_dot × 4000 / (3600 × π × d_inner²)
            // где V_dot в л/ч, d_inner в мм
            // Пример: V_dot=280 л/ч, d=13 мм → v = 280×4000/(3600×π×169) ≈ 0.59 м/с
            double velocity = circuit.FlowRate * 4000 / (3600 * Math.PI * Math.Pow(innerDiameter, 2));
            circuit.Velocity = velocity;

            // Число Рейнольдса: Re = 1000 × v × d_inner / ν
            double reynolds = 1000 * velocity * innerDiameter / glycolProps.KinematicViscosity;
            result.ReynoldsNumber = reynolds;

            // Режим течения
            result.FlowRegime = FlowRegimeCalculator.DetermineFlowRegime(reynolds);

            // Коэффициент трения λ
            double frictionFactor = FlowRegimeCalculator.CalculateFrictionFactor(reynolds, innerDiameter);
            result.FrictionFactor = frictionFactor;

            // Удельные потери: R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100
            double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * glycolProps.Density * frictionFactor
                / (2 * innerDiameter) * 100;
            result.PressureLossPerMeter = pressureLossPerMeter;

            // Потери в трубе контура: Δp_HK = L_hk × R
            result.CircuitPipeLoss = circuit.CircuitLength * pressureLossPerMeter;

            // Потери в трубе подводки: Δp_Zul = L_zul × R
            result.SupplyPipeLoss = circuit.SupplyLength * pressureLossPerMeter;

            // Потери в вентиле: Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ
            result.ValveLoss = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * glycolProps.Density;

            // Суммарные потери: Δp_total = Δp_HK + Δp_Zul + Δp_Vent
            // (вычисляется автоматически в свойстве TotalLoss)

            return result;
        }

        /// <summary>
        /// Рассчитать все контура коллектора
        /// </summary>
        /// <param name="circuits">Список контуров</param>
        /// <param name="inputData">Входные данные для расчёта</param>
        /// <param name="pipeSpacing_cm">Шаг укладки трубы, см</param>
        /// <returns>Список контуров с рассчитанными параметрами</returns>
        /// <remarks>
        /// Выполняет расчёт для двух температур:
        /// - Рабочая температура: T_operating = (T_supply + T_return) / 2
        /// - Расчётная температура: T_design = t_cold
        /// 
        /// Результаты сохраняются в:
        /// - circuit.OperatingResult — для рабочей температуры
        /// - circuit.DesignResult — для расчётной температуры
        /// 
        /// Алгоритм:
        /// 1. Получить свойства гликоля для двух температур
        /// 2. Рассчитать мощность для каждого контура
        /// 3. Рассчитать расход для каждого контура
        /// 4. Рассчитать результаты при рабочей температуре
        /// 5. Рассчитать результаты при расчётной температуре
        /// </remarks>
        public List<CircuitRow> CalculateAllCircuits(List<CircuitRow> circuits, HydraulicInputData inputData, double pipeSpacing_cm)
        {
            if (circuits == null || circuits.Count == 0)
                return new List<CircuitRow>();

            if (inputData == null)
                throw new ArgumentNullException(nameof(inputData));

            if (pipeSpacing_cm <= 0)
                throw new ArgumentException("Шаг укладки должен быть положительным", nameof(pipeSpacing_cm));

            // Валидация входных данных
            var validationResult = inputData.Validate();
            if (!validationResult.IsValid)
                throw new ArgumentException($"Некорректные входные данные: {string.Join(", ", validationResult.Errors)}");

            // Получение свойств гликоля при рабочей температуре
            var glycolPropsOperating = _glycolService.GetProperties(
                inputData.GlycolType,
                inputData.GlycolConcentration,
                inputData.OperatingTemperature);

            // Получение свойств гликоля при расчётной температуре
            var glycolPropsDesign = _glycolService.GetProperties(
                inputData.GlycolType,
                inputData.GlycolConcentration,
                inputData.DesignTemperature);

            // Kv клапана
            double kv = ValveTurnsCalculator.GetDefaultKv(inputData.ValveType);

            foreach (var circuit in circuits)
            {
                if (!circuit.IsActive)
                    continue;

                // Расчёт мощности
                circuit.Power = CalculateCircuitPower(circuit, inputData.PowerUp, inputData.PowerDown, pipeSpacing_cm);

                // Расчёт расхода
                circuit.FlowRate = CalculateFlowRate(
                    circuit.Power,
                    inputData.DeltaT,
                    glycolPropsOperating.Density,
                    glycolPropsOperating.SpecificHeat);

                // Расчёт при рабочей температуре
                circuit.OperatingResult = CalculateAtTemperature(
                    circuit,
                    inputData.OperatingTemperature,
                    glycolPropsOperating,
                    inputData.InnerDiameter,
                    kv);

                // Расчёт при расчётной температуре
                circuit.DesignResult = CalculateAtTemperature(
                    circuit,
                    inputData.DesignTemperature,
                    glycolPropsDesign,
                    inputData.InnerDiameter,
                    kv);
            }

            return circuits;
        }

        /// <summary>
        /// Рассчитать балансировку контуров
        /// </summary>
        /// <param name="circuits">Список контуров</param>
        /// <param name="valveType">Тип балансировочного клапана</param>
        /// <returns>Список контуров с рассчитанной балансировкой</returns>
        /// <remarks>
        /// Алгоритм балансировки:
        /// 1. Определить контур с максимальными потерями (референсный)
        /// 2. Рассчитать дросселирование для каждого контура:
        ///    zu_drosseln = Δp_max - Δp_total
        /// 3. Рассчитать обороты балансировочного клапана
        /// 
        /// Балансировка выполняется только для рабочей температуры.
        /// 
        /// Формулы оборотов клапана:
        /// - IV 1½": Обороты = 5.122 × Kv - 0.2106
        /// - IV 1¼": Обороты = 5.1818 × Kv - 0.23
        /// - HKV-D: Обороты = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712
        /// </remarks>
        public List<CircuitRow> CalculateBalancing(List<CircuitRow> circuits, ValveType valveType)
        {
            if (circuits == null || circuits.Count == 0)
                return new List<CircuitRow>();

            // Фильтруем только активные контуры
            var activeCircuits = circuits.Where(c => c.IsActive && c.OperatingResult != null).ToList();

            if (activeCircuits.Count == 0)
                return circuits;

            // Найти контур с максимальными потерями (референсный)
            double maxPressureLoss = activeCircuits.Max(c => c.OperatingResult.TotalLoss);

            // Рассчитать дросселирование для каждого контура
            foreach (var circuit in activeCircuits)
            {
                // zu_drosseln = Δp_max - Δp_total
                circuit.Throttling = maxPressureLoss - circuit.OperatingResult.TotalLoss;

                // Референсный контур
                circuit.IsReferenceCircuit = Math.Abs(circuit.OperatingResult.TotalLoss - maxPressureLoss) < 0.01;

                // Расчёт оборотов клапана
                if (circuit.Throttling > 0)
                {
                    // Kv для дросселирования
                    double kv = CalculateKvForThrottling(circuit.FlowRate, circuit.Throttling);
                    circuit.ValveTurns = ValveTurnsCalculator.CalculateTurns(kv, valveType);
                }
                else
                {
                    circuit.ValveTurns = 0;
                }
            }

            return circuits;
        }

        /// <summary>
        /// Рассчитать итоги коллектора
        /// </summary>
        /// <param name="circuits">Список контуров коллектора</param>
        /// <param name="collectorNumber">Номер коллектора</param>
        /// <param name="valveType">Тип балансировочного клапана</param>
        /// <returns>Итоги расчёта коллектора</returns>
        /// <remarks>
        /// Рассчитывает:
        /// - Количество контуров
        /// - Общую длину труб
        /// - Суммарную мощность
        /// - Суммарный расход
        /// - Потери при рабочей температуре
        /// - Потери при расчётной температуре
        /// - Номер референсного контура
        /// - Предупреждения (превышение давления > 320 мбар)
        /// 
        /// Максимально допустимые потери: 320 мбар (ограничение РЕХАУ)
        /// </remarks>
        public CollectorSummary CalculateCollectorSummary(
            List<CircuitRow> circuits,
            int collectorNumber,
            ValveType valveType)
        {
            if (circuits == null || circuits.Count == 0)
                return new CollectorSummary { CollectorNumber = collectorNumber };

            // Фильтруем только активные контуры
            var activeCircuits = circuits.Where(c => c.IsActive).ToList();

            if (activeCircuits.Count == 0)
                return new CollectorSummary { CollectorNumber = collectorNumber };

            var summary = new CollectorSummary
            {
                CollectorNumber = collectorNumber,
                CircuitCount = activeCircuits.Count,
                ValveType = valveType,
                Kv = ValveTurnsCalculator.GetDefaultKv(valveType),
                TotalPipeLength = activeCircuits.Sum(c => c.TotalLength),
                TotalPower = activeCircuits.Sum(c => c.Power),
                TotalFlowRate = activeCircuits.Sum(c => c.FlowRate),
                PressureLoss_Operating_mbar = activeCircuits.Max(c => c.OperatingResult?.TotalLoss_mbar ?? 0),
                PressureLoss_Cold_mbar = activeCircuits.Max(c => c.DesignResult?.TotalLoss_mbar ?? 0)
            };

            // Найти референсный контур
            var referenceCircuit = activeCircuits.FirstOrDefault(c => c.IsReferenceCircuit);
            if (referenceCircuit != null)
            {
                summary.ReferenceCircuitNumber = referenceCircuit.CircuitNumber;
            }

            // Проверка превышения давления
            var warnings = new List<string>();
            if (summary.PressureLoss_Cold_mbar > CollectorSummary.MaxAllowedPressure_mbar)
            {
                warnings.Add($"Превышение давления: {summary.PressureLoss_Cold_mbar:F1} мбар > {CollectorSummary.MaxAllowedPressure_mbar} мбар");
            }

            summary.Warnings = warnings.ToArray();
            summary.IsValid = warnings.Count == 0;

            return summary;
        }

        #region Приватные методы

        /// <summary>
        /// Рассчитать Kv для дросселирования
        /// </summary>
        /// <param name="flowRate">Расход, л/ч</param>
        /// <param name="throttling">Дросселирование, Па</param>
        /// <returns>Kv (м³/ч)</returns>
        /// <remarks>
        /// Формула: Kv = V_dot / √(Δp / 100)
        /// 
        /// Вывод из формулы потерь в вентиле:
        /// Δp = (V_dot / 1000 / Kv)² × 100000 × ρ
        /// 
        /// Упрощённо (при ρ ≈ 1000 кг/м³):
        /// Kv ≈ V_dot / √(Δp / 100)
        /// </remarks>
        private double CalculateKvForThrottling(double flowRate, double throttling)
        {
            if (throttling <= 0)
                return 0;

            // Kv = V_dot / √(Δp / 100)
            // где V_dot в л/ч, Δp в Па
            return flowRate / Math.Sqrt(throttling / 100);
        }

        #endregion
    }
}