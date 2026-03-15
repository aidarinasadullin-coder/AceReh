using System;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Результат гидравлического расчёта контура
    /// </summary>
    /// <remarks>
    /// Содержит все рассчитанные параметры:
    /// - Скорость потока и число Рейнольдса
    /// - Режим течения и коэффициент трения
    /// - Потери давления в трубе и вентиле
    /// - Суммарные потери давления
    /// </remarks>
    public class HydraulicResult
    {
        // === Скорость и режим течения ===
        
        /// <summary>
        /// Скорость потока (w), м/с
        /// </summary>
        /// <remarks>
        /// Формула: w = v × 1000 / (3600 × π × di² / 4)
        /// Где:
        /// - v — расход, л/ч
        /// - di — внутренний диаметр, мм
        /// 
        /// Рекомендуемый диапазон: 0.2-1.5 м/с
        /// </remarks>
        public double Velocity { get; set; }
        
        /// <summary>
        /// Число Рейнольдса (Re), безразмерное
        /// </summary>
        /// <remarks>
        /// Формула: Re = 1000 × w × di / ν
        /// Где:
        /// - w — скорость, м/с
        /// - di — внутренний диаметр, мм
        /// - ν — кинематическая вязкость, мм²/с
        /// </remarks>
        public double ReynoldsNumber { get; set; }
        
        /// <summary>
        /// Режим течения
        /// </summary>
        /// <remarks>
        /// Определяется по числу Рейнольдса:
        /// - Re &lt; 2300 → Laminar
        /// - 2300 ≤ Re ≤ 4000 → Transitional
        /// - Re &gt; 4000 → Turbulent
        /// </remarks>
        public FlowRegime FlowRegime { get; set; }
        
        /// <summary>
        /// Коэффициент гидравлического трения (λ), безразмерный
        /// </summary>
        /// <remarks>
        /// Ламинарный режим: λ = 64 / Re (формула Пуазейля)
        /// Переходный режим: линейная интерполяция
        /// Турбулентный режим: формула Колбрука-Уайта
        /// </remarks>
        public double FrictionFactor { get; set; }
        
        // === Потери давления ===
        
        /// <summary>
        /// Удельные потери давления (R), Па/м
        /// </summary>
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
        public double PressureLossPerMeter { get; set; }
        
        /// <summary>
        /// Потери давления в контуре (Δp_HK), Па
        /// </summary>
        /// <remarks>
        /// Формула: Δp_HK = L_HK × R
        /// Где L_HK — длина контура, м
        /// </remarks>
        public double CircuitPressureLoss { get; set; }
        
        /// <summary>
        /// Потери давления в подводке (Δp_Zul), Па
        /// </summary>
        /// <remarks>
        /// Формула: Δp_Zul = L_Zul × R
        /// Где L_Zul — длина подводки, м
        /// </remarks>
        public double SupplyPressureLoss { get; set; }
        
        /// <summary>
        /// Общие потери давления в трубе (Δp_Rohr), Па
        /// </summary>
        /// <remarks>
        /// Формула: Δp_Rohr = Δp_HK + Δp_Zul
        /// Или: Δp_Rohr = (L_HK + L_Zul) × R
        /// </remarks>
        public double TotalPipePressureLoss { get; set; }
        
        /// <summary>
        /// Потери давления в вентиле (Δp_Vent), Па
        /// </summary>
        /// <remarks>
        /// Формула для HKV-D: Δp = (v / 1000 / 1.2)² × 100000 × ρ
        /// Формула для IV 1¼": Δp = (v / 1000 / 1.45)² × 100000 × ρ
        /// Формула для IV 1½": Δp = (v / 1000 / 1.5)² × 100000 × ρ
        /// </remarks>
        public double ValvePressureLoss { get; set; }
        
        /// <summary>
        /// Суммарные потери давления (Δp_total), Па
        /// </summary>
        /// <remarks>
        /// Формула: Δp_total = Δp_Rohr + Δp_Vent
        /// </remarks>
        public double TotalPressureLoss { get; set; }
        
        // === Расход ===
        
        /// <summary>
        /// Расход на контур (v), л/ч
        /// </summary>
        /// <remarks>
        /// Формула: v = VolumeFlowRate × CircuitArea
        /// Где:
        /// - VolumeFlowRate — удельный расход, л/(ч·м²)
        /// - CircuitArea — площадь контура, м²
        /// </remarks>
        public double CircuitFlowRate { get; set; }
        
        // === Валидация ===
        
        /// <summary>
        /// Признак валидности результата
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Ошибки валидации
        /// </summary>
        public string[] ValidationErrors { get; set; } = Array.Empty<string>();
        
        /// <summary>
        /// Предупреждения
        /// </summary>
        /// <remarks>
        /// Например: "Переходный режим течения"
        /// </remarks>
        public string[] Warnings { get; set; } = Array.Empty<string>();
        
        // === Вычисляемые свойства ===
        
        /// <summary>
        /// Потери давления в кПа
        /// </summary>
        public double TotalPressureLoss_kPa => TotalPressureLoss / 1000;
        
        /// <summary>
        /// Потери давления в мбар
        /// </summary>
        public double TotalPressureLoss_mbar => TotalPressureLoss / 100;
        
        /// <summary>
        /// Признак переходного режима течения
        /// </summary>
        public bool IsTransitionalFlow => FlowRegime == FlowRegime.Transitional;
        
        /// <summary>
        /// Признак низкой скорости потока
        /// </summary>
        public bool IsLowVelocity => Velocity < 0.2;
        
        /// <summary>
        /// Признак высокой скорости потока
        /// </summary>
        public bool IsHighVelocity => Velocity > 1.5;
        
        /// <summary>
        /// Признак превышения удельных потерь
        /// </summary>
        public bool IsPressureLossExceeded => PressureLossPerMeter > 300;
        
        // === Методы ===
        
        /// <summary>
        /// Создать пустой результат
        /// </summary>
        public static HydraulicResult Empty => new HydraulicResult();
        
        /// <summary>
        /// Получить описание режима течения
        /// </summary>
        public string GetFlowRegimeDescription()
        {
            return FlowRegime switch
            {
                FlowRegime.Laminar => "Ламинарный режим (Re < 2300)",
                FlowRegime.Transitional => "Переходный режим (2300 ≤ Re ≤ 4000)",
                FlowRegime.Turbulent => "Турбулентный режим (Re > 4000)",
                _ => "Неизвестный режим"
            };
        }
        
        /// <summary>
        /// Получить предупреждения о проблемах
        /// </summary>
        public List<string> GetWarnings()
        {
            var warnings = new List<string>();
            
            if (IsTransitionalFlow)
                warnings.Add("Переходный режим течения (2300 ≤ Re ≤ 4000). Рекомендуется изменить параметры для обеспечения стабильного течения.");
            
            if (IsLowVelocity)
                warnings.Add($"Низкая скорость потока ({Velocity:F3} м/с). Возможны проблемы с теплоотдачей.");
            
            if (IsHighVelocity)
                warnings.Add($"Высокая скорость потока ({Velocity:F3} м/с). Рекомендуется увеличить диаметр трубы.");
            
            if (IsPressureLossExceeded)
                warnings.Add($"Превышение удельных потерь ({PressureLossPerMeter:F1} Па/м > 300 Па/м). Рекомендуется уменьшить длину контура или увеличить диаметр трубы.");
            
            return warnings;
        }
    }
}