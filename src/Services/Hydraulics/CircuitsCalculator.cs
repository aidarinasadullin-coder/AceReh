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
        /// Формула: V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT) × 1000
        /// 
        /// Где:
        /// - Q_HK — мощность контура, Вт
        /// - ρ — плотность теплоносителя, кг/м³
        /// - c_p — удельная теплоёмкость, кДж/(кг·К)
        /// - ΔT — температурный перепад, К
        /// - 3.6 — коэффициент перевода Вт в кДж/ч
        /// - 1000 — коэффициент перевода м³/ч в л/ч
        /// 
        /// Примечание: Формула даёт результат в м³/ч, умножение на 1000 переводит в л/ч.
        /// 
        /// Пример:
        /// Q_HK = 5246 Вт, ρ = 1053 кг/м³, c_p = 3.21 кДж/(кг·К), ΔT = 10 К
        /// V_dot = 5246 × 3.6 / (1053 × 3.21 × 10) × 1000 = 560 л/ч
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
            // Результат в м³/ч, переводим в л/ч
            double flowRate_m3h = power * 3.6 / (density * specificHeat * deltaT);
            double flowRate_lh = flowRate_m3h * 1000;

            return flowRate_lh;
        }

        /// <summary>
        /// Рассчитать гидравлику контура при заданной температуре
        /// </summary>
        /// <param name="circuit">Контур для расчёта</param>
        /// <param name="temperature">Температура теплоносителя, °C</param>
        /// <param name="glycolProps">Свойства гликоля при температуре</param>
        /// <param name="innerDiameter">Внутренний диаметр трубы, мм</param>
        /// <param name="kv">Коэффициент пропускной способности вентиля, м³/ч</param>
        /// <param name="valveType">Тип клапана (для выбора формул DpVerteiler/DpVent)</param>
        /// <returns>Результат расчёта при температуре</returns>
        /// <remarks>
        /// Рассчитывает:
        /// - Скорость потока v
        /// - Число Рейнольдса Re
        /// - Режим течения (ламинарный/переходный/турбулентный)
        /// - Коэффициент трения λ
        /// - Удельные потери R (Па/м)
        /// - DpRohr — потери в трубе контура + подводки, Па
        /// - DpVerteiler — потери в распределителе, Па
        /// - DpVent — потери в вентиле, Па
        /// - DpGesamt — суммарные потери, Па
        /// 
        /// Формулы:
        /// - Скорость: v = V_dot × 4000 / (3600 × π × d_inner²)
        /// - Число Рейнольдса: Re = 1000 × v × d_inner / ν
        /// - Коэффициент трения: зависит от режима (Пуазейль или Колбрук-Уайт)
        /// - Удельные потери: R = 10000 × (v² × ρ[г/см³] × λ) / (2 × d_inner) × 100
        /// - DpRohr = (L_hk + L_zul) × R
        /// 
        /// Формулы DpVerteiler и DpVent зависят от типа клапана:
        /// 
        /// Для IV 1¼" и IV 1½":
        /// - DpVerteiler = 15000 × (ρ/2000) × v²
        /// - DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
        /// 
        /// Для HKV-D:
        /// - DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
        /// - DpVent = 15000 × (ρ/2000) × v²
        /// 
        /// Важно: Плотность ρ в формулах R и DpVent должна быть в г/см³!
        /// GlycolProperties.Density хранит плотность в кг/м³, требуется конвертация.
        /// </remarks>
        public CircuitTemperatureResult CalculateAtTemperature(
            CircuitRow circuit,
            double temperature,
            GlycolProperties glycolProps,
            double innerDiameter,
            double kv,
            ValveType valveType)
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
                Density = glycolProps.Density / 1000.0,  // Конвертация: кг/м³ → г/см³
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

            // Удельные потери: R = 10000 × (v² × ρ[г/см³] × λ) / (2 × d_inner) × 100
            // Важно: ρ должно быть в г/см³, glycolProps.Density в кг/м³
            double density_g_cm3 = glycolProps.Density / 1000.0;
            double pressureLossPerMeter = 10000 * Math.Pow(velocity, 2) * density_g_cm3 * frictionFactor
                / (2 * innerDiameter) * 100;
            result.PressureLossPerMeter = pressureLossPerMeter;

            // === НОВЫЙ РАСЧЁТ ===

            // DpRohr = потери в трубе контура + подводки
            // Формула: DpRohr = (L_hk + L_zul) × R
            double dpRohr = (circuit.CircuitLength + circuit.SupplyLength) * pressureLossPerMeter;
            result.DpRohr = dpRohr;

            // DpVerteiler и DpVent — формулы меняются местами для HKV-D и IV
            if (valveType == ValveType.HKV_D)
            {
                // HKV-D: формулы меняются местами
                // DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
                // Kv для HKV-D = 1.2
                result.DpVerteiler = Math.Pow(circuit.FlowRate / 1000.0 / 1.2, 2) * 100000 * density_g_cm3;

                // DpVent = 15000 × (ρ/2000) × v²
                result.DpVent = 15000 * (density_g_cm3 / 2) * Math.Pow(velocity, 2);
            }
            else
            {
                // IV 1¼" и IV 1½": стандартные формулы
                // DpVerteiler = 15000 × (ρ/2000) × v²
                result.DpVerteiler = 15000 * (density_g_cm3 / 2) * Math.Pow(velocity, 2);

                // DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
                result.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;
            }

            // DpGesamt = DpRohr + DpVerteiler + DpVent (вычисляется автоматически)

            // === УСТАРЕВШИЕ СВОЙСТВА (для обратной совместимости) ===
#pragma warning disable CS0618 // Type or member is obsolete
            result.CircuitPipeLoss = circuit.CircuitLength * pressureLossPerMeter;
            result.SupplyPipeLoss = circuit.SupplyLength * pressureLossPerMeter;
            result.ValveLoss = result.DpVent;  // Для IV это корректно, для HKV-D — нет
#pragma warning restore CS0618

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
                    kv,
                    inputData.ValveType);  // Передаём тип клапана

                // Расчёт при расчётной температуре
                circuit.DesignResult = CalculateAtTemperature(
                    circuit,
                    inputData.DesignTemperature,
                    glycolPropsDesign,
                    inputData.InnerDiameter,
                    kv,
                    inputData.ValveType);  // Передаём тип клапана
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
        /// 1. Определить контур с максимальным DpGesamt (референсный)
        /// 2. Референсный контур получает максимальные обороты:
        ///    - HKV-D: 2.5 оборота
        ///    - IV: 8.0 оборотов
        /// 3. Референсный контур имеет Throttling = 0 (не требует дросселирования)
        /// 4. Рассчитать дросселирование для нереференсных контуров:
        ///    zu_drosseln = DpGesamt_max - DpGesamt_контур
        /// 5. Для нереференсных контуров рассчитать Kv для дросселирования
        /// 6. Рассчитать обороты по формуле umdreh1(Kv, type)
        /// 
        /// Балансировка выполняется только для рабочей температуре.
        /// 
        /// Важно: Референсный контур — это контур с максимальным DpGesamt,
        /// а не с максимальными потерями в трубе. Это необходимо для корректной
        /// балансировки, так как DpGesamt включает все потери.
        /// 
        /// Важно: Референсный контур НЕ требует дросселирования (Throttling = 0),
        /// так как он имеет максимальные потери и определяет требуемый напор насоса.
        /// </remarks>
        public List<CircuitRow> CalculateBalancing(List<CircuitRow> circuits, ValveType valveType)
        {
            if (circuits == null || circuits.Count == 0)
                return new List<CircuitRow>();

            // Фильтруем только активные контуры
            var activeCircuits = circuits.Where(c => c.IsActive && c.OperatingResult != null).ToList();

            if (activeCircuits.Count == 0)
                return circuits;

            // === ШАГ 1: Найти контур с МАКСИМАЛЬНЫМ DpGesamt (референсный) ===
            double maxDpGesamt = activeCircuits.Max(c => c.OperatingResult.DpGesamt);

            // === ШАГ 2: Максимальные обороты для типа клапана ===
            double maxTurns = ValveTurnsCalculator.GetMaxTurns(valveType);

            // === ШАГ 3: Определить референсный контур и рассчитать параметры ===
            foreach (var circuit in activeCircuits)
            {
                double dpGesamt = circuit.OperatingResult.DpGesamt;

                // Определить, является ли контур референсным
                circuit.IsReferenceCircuit = Math.Abs(dpGesamt - maxDpGesamt) < 0.01;

                if (circuit.IsReferenceCircuit)
                {
                    // === ВАЖНО: Референсный контур НЕ требует дросселирования ===
                    circuit.Throttling = 0;
                    circuit.ValveTurns = maxTurns;
                    circuit.ValveTurnsWarning = null;
                    // Сохранить kv для последующих расчётов (вычислить из максимальных оборотов)
                    circuit.KvFromValveTurns = ValveTurnsCalculator.CalculateKvFromTurns(maxTurns, valveType);
                }
                else
                {
                    // === ШАГ 4: Рассчитать дросселирование для нереференсных контуров ===
                    // zu_drosseln зависит от типа клапана
                    if (valveType == ValveType.HKV_D)
                    {
                        // HKV-D: zu_drosseln = DpGesamt_max - DpRohr - DpVent
                        circuit.Throttling = maxDpGesamt - (circuit.OperatingResult.DpRohr + circuit.OperatingResult.DpVent);
                    }
                    else
                    {
                        // IV: zu_drosseln = DpGesamt_max - DpRohr - DpVerteiler
                        circuit.Throttling = maxDpGesamt - (circuit.OperatingResult.DpRohr + circuit.OperatingResult.DpVerteiler);
                    }

                    // === ШАГ 5: Рассчитать Kv и обороты для дросселирования ===
                    double density_g_cm3 = circuit.OperatingResult.Density;
                    double kv = CalculateKvForThrottling(circuit.FlowRate, circuit.Throttling, density_g_cm3);
                    var (turns, warning) = ValveTurnsCalculator.CalculateTurnsWithWarning(kv, valveType);
                    circuit.ValveTurns = turns;
                    circuit.ValveTurnsWarning = warning;
                    // Сохранить kv для последующих расчётов
                    circuit.KvFromValveTurns = kv;
                }
            }

            // === ВАЖНО: DpVent для HKV-D НЕ пересчитывается, так как не зависит от Kv ===
            // Для HKV-D: DpVent = 15000 × (ρ/2000) × v² — не зависит от Kv
            // Для IV: DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000 — зависит от Kv
            if (valveType != ValveType.HKV_D)
            {
                // Пересчитать потери на клапане при текущих оборотах ТОЛЬКО для IV
                foreach (var circuit in activeCircuits)
                {
                    // Рассчитать Kv для текущих оборотов
                    double kv = ValveTurnsCalculator.CalculateKvFromTurns(circuit.ValveTurns, valveType);
                    
                    // Сохранить kv для последующих расчётов
                    circuit.KvFromValveTurns = kv;

                    // === Рабочая температура ===
                    double densityOperating = circuit.OperatingResult.Density;
                    circuit.OperatingResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * densityOperating;

#pragma warning disable CS0618 // Type or member is obsolete
                    circuit.OperatingResult.ValveLoss = circuit.OperatingResult.DpVent;
#pragma warning restore CS0618

                    // === Расчётная температура (холодный пуск) ===
                    if (circuit.DesignResult != null)
                    {
                        double densityDesign = circuit.DesignResult.Density;
                        circuit.DesignResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * densityDesign;

#pragma warning disable CS0618 // Type or member is obsolete
                        circuit.DesignResult.ValveLoss = circuit.DesignResult.DpVent;
#pragma warning restore CS0618
                    }
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
                // === ИЗМЕНЕНИЕ: Использовать DpGesamt вместо TotalLoss_mbar ===
                PressureLoss_Operating_Pa = activeCircuits.Max(c => c.OperatingResult?.DpGesamt ?? 0),
                PressureLoss_Cold_Pa = activeCircuits.Max(c => c.DesignResult?.DpGesamt ?? 0)
            };

            // Найти референсный контур
            var referenceCircuit = activeCircuits.FirstOrDefault(c => c.IsReferenceCircuit);
            if (referenceCircuit != null)
            {
                summary.ReferenceCircuitNumber = referenceCircuit.CircuitNumber;
            }

            // Проверка превышения давления
            var warnings = new List<string>();
            if (summary.PressureLoss_Cold_Pa > CollectorSummary.MaxAllowedPressure_Pa)
            {
                warnings.Add($"Превышение давления: {summary.PressureLoss_Cold_Pa / 100.0:F1} мбар > {CollectorSummary.MaxAllowedPressure_Pa / 100.0:F0} мбар");
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
        /// <param name="density_g_cm3">Плотность теплоносителя, г/см³</param>
        /// <returns>Kv (м³/ч)</returns>
        /// <remarks>
        /// Формула выводится из уравнения потерь в вентиле:
        /// Δp = (V_dot / 1000 / Kv)² × 100000 × ρ[г/см³]
        /// 
        /// Обратная формула для Kv:
        /// Kv = V_dot / 1000 / √(Δp / 100000 / ρ[г/см³])
        /// 
        /// Единицы измерения:
        /// - V_dot: л/ч → переводим в м³/ч (делим на 1000)
        /// - Δp: Па → переводим в бар (делим на 100000)
        /// - ρ: г/см³ (уже в нужных единицах)
        /// - Kv: м³/ч
        /// 
        /// Пример:
        /// V_dot = 280 л/ч, Δp = 5000 Па, ρ = 1.053 г/см³
        /// Kv = 280 / 1000 / √(5000 / 100000 / 1.053) = 0.28 / √(0.0475) ≈ 1.28 м³/ч
        /// </remarks>
        private double CalculateKvForThrottling(double flowRate, double throttling, double density_g_cm3)
        {
            if (throttling <= 0)
                return 0;

            if (density_g_cm3 <= 0)
                throw new ArgumentException("Плотность должна быть положительной", nameof(density_g_cm3));

            // Kv = V_dot / 1000 / √(Δp / 100000 / ρ[г/см³])
            // где V_dot в л/ч, Δp в Па, ρ в г/см³
            // Результат в м³/ч
            double flowRate_m3h = flowRate / 1000.0;  // л/ч → м³/ч
            double throttling_bar = throttling / 100000.0;  // Па → бар

            return flowRate_m3h / Math.Sqrt(throttling_bar / density_g_cm3);
        }

        #endregion
    }
}