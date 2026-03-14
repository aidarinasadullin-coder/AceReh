using System.ComponentModel;

namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Режим работы системы снеготаяния
    /// </summary>
    public enum OperatingMode
    {
        /// <summary>
        /// Антиобледенение (t_П = +3°C)
        /// Минимальная мощность
        /// </summary>
        [Description("Антиобледенение (t_П = +3°C) - минимальная мощность")]
        AntiIcing = 3,
        
        /// <summary>
        /// Таяние (t_П = +5°C)
        /// Стандартный режим
        /// </summary>
        [Description("Таяние (t_П = +5°C) - стандартный режим")]
        Melting = 5,
        
        /// <summary>
        /// Интенсивное (t_П = +7°C)
        /// Максимальная мощность
        /// </summary>
        [Description("Интенсивное (t_П = +7°C) - максимальная мощность")]
        Intensive = 7
    }
}