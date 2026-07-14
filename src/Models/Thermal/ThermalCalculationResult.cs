using System;

namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Интерфейс результата теплового расчёта
    /// </summary>
    /// <remarks>
    /// Содержит только выходные величины расчёта. Поля, дублировавшие
    /// входные данные (Pipe, PipeSpacing, R1Total, R2Total), удалены;
    /// температурные параметры SupplyTemperature, ReturnTemperature и DeltaT
    /// остаются как распространяемые выходные значения.
    /// </remarks>
    public interface IThermalCalculationResult
    {
        // === Коэффициенты ===

        /// <summary>
        /// Коэффициент теплоотдачи на поверхности, Вт/м²·К
        /// </summary>
        double Alpha { get; }

        // === Мощности ===

        /// <summary>
        /// Мощность вверх (требуемая), Вт/м²
        /// </summary>
        double PowerUp { get; }

        /// <summary>
        /// Мощность вниз (потери), Вт/м²
        /// </summary>
        double PowerDown { get; }

        /// <summary>
        /// Суммарная мощность, Вт/м²
        /// </summary>
        double PowerTotal { get; }

        // === Составляющие мощности ===

        /// <summary>
        /// Теплота плавления снега, Вт/м²
        /// </summary>
        double MeltingHeat { get; }

        /// <summary>
        /// Лучистый тепловой поток, Вт/м²
        /// </summary>
        double RadiationHeat { get; }

        /// <summary>
        /// Конвективный тепловой поток, Вт/м²
        /// </summary>
        double ConvectionHeat { get; }

        // === Температуры ===

        /// <summary>
        /// Избыточная температура теплоносителя, °C
        /// </summary>
        double ExcessTemperature { get; }

        /// <summary>
        /// Средняя температура теплоносителя, °C
        /// </summary>
        double MeanTemperature { get; }

        /// <summary>
        /// Температура подачи, °C
        /// </summary>
        double SupplyTemperature { get; }

        /// <summary>
        /// Температура обратки, °C
        /// </summary>
        double ReturnTemperature { get; }

        /// <summary>
        /// Температурный перепад, К
        /// </summary>
        double DeltaT { get; }

        // === Сопротивления ===

        /// <summary>
        /// Полное сопротивление вверх, м²·К/Вт
        /// </summary>
        double RFb { get; }

        /// <summary>
        /// Полное сопротивление вниз, м²·К/Вт
        /// </summary>
        double RD { get; }

        // === Теория стержня ===

        /// <summary>
        /// Параметр m, 1/м
        /// </summary>
        double ParameterM { get; }

        /// <summary>
        /// КПД ребра (коэффициент эффективности)
        /// </summary>
        double EfficiencyEtaR { get; }

        // === Расходы ===

        /// <summary>
        /// Массовый расход, кг/(ч·м²)
        /// </summary>
        double MassFlowRate { get; }

        /// <summary>
        /// Объёмный расход, л/(ч·м²)
        /// </summary>
        double VolumeFlowRate { get; }

        // === Валидация ===

        /// <summary>
        /// Признак валидности результата
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Ошибки валидации
        /// </summary>
        string[] ValidationErrors { get; }

        // === Событие ===

        /// <summary>
        /// Событие изменения результата
        /// </summary>
        event EventHandler<ThermalResultChangedEventArgs>? ResultChanged;
    }

    /// <summary>
    /// Результат теплового расчёта
    /// </summary>
    /// <remarks>
    /// Содержит только выходные величины расчёта. Поля, дублировавшие
    /// входные данные (Pipe, PipeSpacing, R1Total, R2Total), удалены;
    /// температурные параметры SupplyTemperature, ReturnTemperature и DeltaT
    /// остаются как распространяемые выходные значения.
    /// </remarks>
    public class ThermalCalculationResult : IThermalCalculationResult
    {
        // === Коэффициенты ===

        public double Alpha { get; set; }

        // === Мощности ===

        public double PowerUp { get; set; }
        public double PowerDown { get; set; }
        public double PowerTotal { get; set; }

        // === Составляющие мощности ===

        public double MeltingHeat { get; set; }
        public double RadiationHeat { get; set; }
        public double ConvectionHeat { get; set; }

        // === Температуры ===

        public double ExcessTemperature { get; set; }
        public double MeanTemperature { get; set; }
        public double SupplyTemperature { get; set; }
        public double ReturnTemperature { get; set; }
        public double DeltaT { get; set; }

        // === Сопротивления ===

        public double RFb { get; set; }
        public double RD { get; set; }

        // === Теория стержня ===

        public double ParameterM { get; set; }
        public double EfficiencyEtaR { get; set; }

        // === Расходы ===

        public double MassFlowRate { get; set; }
        public double VolumeFlowRate { get; set; }

        // === Валидация ===

        public bool IsValid { get; set; }
        public string[] ValidationErrors { get; set; } = Array.Empty<string>();

        // === Событие ===

        public event EventHandler<ThermalResultChangedEventArgs>? ResultChanged;

        public void RaiseResultChanged()
        {
            ResultChanged?.Invoke(this, new ThermalResultChangedEventArgs { Result = this });
        }

        /// <summary>
        /// Создать строковое представление результата
        /// </summary>
        public override string ToString()
        {
            return $"Мощность: {PowerTotal:F1} Вт/м², " +
                   $"T_подачи: {SupplyTemperature:F1}°C, " +
                   $"T_обратки: {ReturnTemperature:F1}°C, " +
                   $"Расход: {VolumeFlowRate:F2} л/(ч·м²)";
        }
    }

    /// <summary>
    /// Аргументы события изменения результата
    /// </summary>
    public class ThermalResultChangedEventArgs : EventArgs
    {
        public ThermalCalculationResult? Result { get; set; }
    }
}