using System;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Свойства теплоносителя (гликоля)
    /// </summary>
    /// <remarks>
    /// Содержит физические свойства гликолевого раствора:
    /// - Плотность (ρ)
    /// - Кинематическая вязкость (ν)
    /// - Удельная теплоёмкость (c_p)
    /// - Теплопроводность (λ)
    /// 
    /// Данные получаются интерполяцией из data/glycol_data.json
    /// для заданного типа гликоля, концентрации и температуры.
    /// 
    /// Источник данных: ASHRAE Handbook
    /// </remarks>
    public class GlycolProperties
    {
        /// <summary>
        /// Плотность (ρ), кг/м³
        /// </summary>
        /// <remarks>
        /// Зависит от:
        /// - Типа гликоля (этиленгликоль/пропиленгликоль)
        /// - Концентрации (10-90%)
        /// - Температуры (-34.4°C до 98.9°C)
        /// 
        /// Типичные значения:
        /// - Вода при 20°C: ~998 кг/м³
        /// - 50% этиленгликоль при 40°C: ~1053 кг/м³
        /// </remarks>
        public double Density { get; set; }

        /// <summary>
        /// Удельная теплоёмкость (c_p), кДж/(кг·К)
        /// </summary>
        /// <remarks>
        /// Зависит от:
        /// - Типа гликоля
        /// - Концентрации
        /// - Температуры
        /// 
        /// Типичные значения:
        /// - Вода при 20°C: 4.18 кДж/(кг·К)
        /// - 50% этиленгликоль при 40°C: ~3.39 кДж/(кг·К)
        /// </remarks>
        public double SpecificHeat { get; set; }

        /// <summary>
        /// Кинематическая вязкость (ν), мм²/с
        /// </summary>
        /// <remarks>
        /// Зависит от:
        /// - Типа гликоля
        /// - Концентрации
        /// - Температуры
        /// 
        /// Вязкость значительно возрастает при низких температурах!
        /// 
        /// Типичные значения:
        /// - Вода при 20°C: ~1.0 мм²/с
        /// - 50% этиленгликоль при 40°C: ~2.16 мм²/с
        /// - 50% этиленгликоль при -15°C: ~18.17 мм²/с
        /// </remarks>
        public double KinematicViscosity { get; set; }

        /// <summary>
        /// Теплопроводность (λ), Вт/(м·К)
        /// </summary>
        /// <remarks>
        /// Зависит от:
        /// - Типа гликоля
        /// - Концентрации
        /// - Температуры
        /// 
        /// Типичные значения:
        /// - Вода при 20°C: ~0.60 Вт/(м·К)
        /// - 50% этиленгликоль при 40°C: ~0.42 Вт/(м·К)
        /// </remarks>
        public double ThermalConductivity { get; set; }

        // === Дополнительные свойства ===

        /// <summary>
        /// Температура, для которой получены свойства, °C
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// Концентрация гликоля, %
        /// </summary>
        public double Concentration { get; set; }

        /// <summary>
        /// Тип гликоля
        /// </summary>
        public GlycolType GlycolType { get; set; }

        // === Вычисляемые свойства ===

        /// <summary>
        /// Кинематическая вязкость в м²/с
        /// </summary>
        /// <remarks>
        /// Преобразование: ν [м²/с] = ν [мм²/с] × 10⁻⁶
        /// </remarks>
        public double KinematicViscosity_m2_s => KinematicViscosity * 1e-6;

        /// <summary>
        /// Динамическая вязкость (μ), Па·с
        /// </summary>
        /// <remarks>
        /// Формула: μ = ρ × ν
        /// Где:
        /// - ρ — плотность, кг/м³
        /// - ν — кинематическая вязкость, м²/с
        /// </remarks>
        public double DynamicViscosity => Density * KinematicViscosity_m2_s;

        /// <summary>
        /// Температуропроводность (a), м²/с
        /// </summary>
        /// <remarks>
        /// Формула: a = λ / (ρ × c_p)
        /// Где:
        /// - λ — теплопроводность, Вт/(м·К)
        /// - ρ — плотность, кг/м³
        /// - c_p — удельная теплоёмкость, Дж/(кг·К)
        /// 
        /// Примечание: c_p нужно перевести из кДж/(кг·К) в Дж/(кг·К)
        /// </remarks>
        public double ThermalDiffusivity => ThermalConductivity / (Density * SpecificHeat * 1000);

        /// <summary>
        /// Число Прандтля (Pr), безразмерное
        /// </summary>
        /// <remarks>
        /// Формула: Pr = ν / a = μ × c_p / λ
        /// Где:
        /// - ν — кинематическая вязкость, м²/с
        /// - a — температуропроводность, м²/с
        /// 
        /// Число Прандтля характеризует отношение вязкостных и тепловых свойств.
        /// </remarks>
        public double PrandtlNumber => KinematicViscosity_m2_s / ThermalDiffusivity;

        // === Методы ===

        /// <summary>
        /// Создать пустые свойства
        /// </summary>
        public static GlycolProperties Empty => new();

        /// <summary>
        /// Создать свойства для воды
        /// </summary>
        /// <param name="temperature">Температура, °C</param>
        /// <returns>Свойства воды</returns>
        public static GlycolProperties Water(double temperature)
        {
            // Приближённые значения для воды
            // Точные значения зависят от температуры
            double density = 1000 - 0.0178 * Math.Pow(temperature - 4, 2);
            double viscosity = Math.Exp(-1.597 + 0.181 * temperature - 0.003 * Math.Pow(temperature, 2));
            double specificHeat = 4.18; // кДж/(кг·К)
            double conductivity = 0.6 - 0.0015 * temperature; // Вт/(м·К)

            return new GlycolProperties
            {
                Density = density,
                SpecificHeat = specificHeat,
                KinematicViscosity = viscosity,
                ThermalConductivity = conductivity,
                Temperature = temperature,
                Concentration = 0,
                GlycolType = GlycolType.Ethylene
            };
        }

        /// <summary>
        /// Получить строковое представление
        /// </summary>
        public override string ToString()
        {
            return $"ρ={Density:F1} кг/м³, ν={KinematicViscosity:F2} мм²/с, c_p={SpecificHeat:F2} кДж/(кг·К)";
        }

        /// <summary>
        /// Получить детальное описание
        /// </summary>
        public string GetDetailedDescription()
        {
            var glycolName = GlycolType == GlycolType.Ethylene ? "Этиленгликоль" : "Пропиленгликоль";
            return $"{glycolName} {Concentration:F0}% при {Temperature:F1}°C:\n" +
                   $"  Плотность: {Density:F1} кг/м³\n" +
                   $"  Вязкость: {KinematicViscosity:F2} мм²/с\n" +
                   $"  Теплоёмкость: {SpecificHeat:F2} кДж/(кг·К)\n" +
                   $"  Теплопроводность: {ThermalConductivity:F3} Вт/(м·К)\n" +
                   $"  Число Прандтля: {PrandtlNumber:F2}";
        }
    }
}