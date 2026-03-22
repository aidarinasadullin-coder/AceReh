using System.Collections.Generic;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Интерфейс калькулятора контуров
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
    /// </remarks>
    public interface ICircuitsCalculator
    {
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
        double CalculateCircuitPower(CircuitRow circuit, double q_up, double q_down, double pipeSpacing_cm);
        
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
        double CalculateFlowRate(double power, double deltaT, double density, double specificHeat);
        
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
        /// - Потери в трубе контура Δp_HK (Па)
        /// - Потери в трубе подводки Δp_Zul (Па)
        /// - Потери в вентиле Δp_Vent (Па)
        /// - Суммарные потери Δp_total (Па)
        /// 
        /// Формулы:
        /// - Скорость: v = V_dot / (π × d²/4 × 3600 × 1000)
        /// - Число Рейнольдса: Re = v × d / ν
        /// - Коэффициент трения: зависит от режима (Пуазейль или Колбрук-Уайт)
        /// - Удельные потери: R = λ × (ρ × v²) / (2 × d)
        /// - Потери в трубе: Δp = R × L
        /// - Потери в вентиле: Δp_Vent = (V_dot / Kv)² × 100
        /// 
        /// Новые формулы для DpVerteiler и DpVent (зависят от типа клапана):
        /// 
        /// Для IV 1¼" и IV 1½":
        /// - DpVerteiler = 15000 × (ρ/2000) × v²
        /// - DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
        /// 
        /// Для HKV-D:
        /// - DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
        /// - DpVent = 15000 × (ρ/2000) × v²
        /// </remarks>
        CircuitTemperatureResult CalculateAtTemperature(
            CircuitRow circuit,
            double temperature,
            GlycolProperties glycolProps,
            double innerDiameter,
            double kv,
            ValveType valveType);
        
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
        List<CircuitRow> CalculateAllCircuits(
            List<CircuitRow> circuits,
            HydraulicInputData inputData,
            double pipeSpacing_cm);
        
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
        List<CircuitRow> CalculateBalancing(
            List<CircuitRow> circuits,
            ValveType valveType);
        
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
        CollectorSummary CalculateCollectorSummary(
            List<CircuitRow> circuits,
            int collectorNumber,
            ValveType valveType);
    }
}