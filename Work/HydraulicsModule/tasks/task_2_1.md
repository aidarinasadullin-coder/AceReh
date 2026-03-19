# Task 2.1: Создать ICircuitsCalculator.cs

**Этап:** 2 - Интерфейсы сервисов  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 1.1 (ValveType), Task 1.2 (HydraulicInputData)

---

## 1. Цель задачи

Создать интерфейс `ICircuitsCalculator` для калькулятора контуров.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-02 | Расчёт мощности контура Q_HK | CalculateCircuitPower() |
| UC-03 | Расчёт при двух температурах | CalculateAtTemperature() |
| UC-04 | Расчёт потерь давления | CalculateAtTemperature() |
| UC-05 | Балансировка контуров | CalculateBalancing() |
| UC-06 | Подбор коллектора | CalculateCollectorSummary() |

---

## 3. Создаваемые файлы

### 3.1. ICircuitsCalculator.cs

**Путь:** `src/Services/Hydraulics/ICircuitsCalculator.cs`

**Содержимое:**
```csharp
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
        /// <returns>Мощность контура, Вт</returns>
        /// <remarks>
        /// Формула: Q_HK = [(L_hk/(100/VA_hk)) + (L_zul/(100/VA_zul))×(q_zul/100)] × (q_up + q_down)
        /// </remarks>
        double CalculateCircuitPower(CircuitRow circuit, double q_up, double q_down);
        
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
        /// <returns>Результат расчёта при температуре</returns>
        /// <remarks>
        /// Рассчитывает:
        /// - Скорость потока v
        /// - Число Рейнольдса Re
        /// - Режим течения
        /// - Коэффициент трения λ
        /// - Удельные потери R
        /// - Потери в трубе Δp_Rohr
        /// - Потери в вентиле Δp_Vent
        /// - Суммарные потери Δp_total
        /// </remarks>
        CircuitTemperatureResult CalculateAtTemperature(
            CircuitRow circuit,
            double temperature,
            GlycolProperties glycolProps,
            double innerDiameter,
            double kv);
        
        /// <summary>
        /// Рассчитать все контура коллектора
        /// </summary>
        /// <param name="circuits">Список контуров</param>
        /// <param name="inputData">Входные данные для расчёта</param>
        /// <returns>Список контуров с рассчитанными параметрами</returns>
        /// <remarks>
        /// Выполняет расчёт для двух температур:
        /// - Рабочая температура: T_operating = (T_supply + T_return) / 2
        /// - Расчётная температура: T_design = t_cold
        /// 
        /// Результаты сохраняются в:
        /// - circuit.OperatingResult — для рабочей температуры
        /// - circuit.DesignResult — для расчётной температуры
        /// </remarks>
        List<CircuitRow> CalculateAllCircuits(
            List<CircuitRow> circuits,
            HydraulicInputData inputData);
        
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
        /// </remarks>
        CollectorSummary CalculateCollectorSummary(
            List<CircuitRow> circuits,
            int collectorNumber,
            ValveType valveType);
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Services/Hydraulics/ICircuitsCalculatorTests.cs`

Тесты будут реализованы в Task 3.2 (CircuitsCalculator).

---

## 5. Критерии приёмки

- [ ] Файл `ICircuitsCalculator.cs` создан в `src/Services/Hydraulics/`
- [ ] Интерфейс содержит все методы из ТЗ
- [ ] XML-документация для каждого метода
- [ ] XML-документация содержит формулы
- [ ] Код компилируется без предупреждений

---

## 6. Примечания

- Интерфейс должен быть независимым от реализации
- Все методы должны иметь XML-документацию с формулами
- Файл размещается в `src/Services/Hydraulics/`

---

## 7. Связанные задачи

- Task 1.1: ValveType — используется в CalculateBalancing()
- Task 1.2: HydraulicInputData — используется в CalculateAllCircuits()
- Task 3.2: CircuitsCalculator — реализация интерфейса

---

*Дата создания: 2026-03-17*