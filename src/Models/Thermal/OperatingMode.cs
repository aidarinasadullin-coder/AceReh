using System.ComponentModel;

namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Режим работы системы снеготаяния.
    /// Числовое значение члена — температура поверхности t_П, °C:
    /// расчёт, Undo/Redo, .smc и отчёты работают через <c>(int)Mode</c>.
    /// Члены 1/2/4/6 — пользовательский ввод (план 2026-09-13, вариант B);
    /// имена AntiIcing/Melting/Intensive заморожены (wire-совместимость .smc).
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
        Intensive = 7,

        /// <summary>
        /// Своё значение (t_П = +1°C): температура поверхности задана вручную
        /// </summary>
        [Description("Своё значение (t_П = +1°C)")]
        Manual1 = 1,

        /// <summary>
        /// Своё значение (t_П = +2°C): температура поверхности задана вручную
        /// </summary>
        [Description("Своё значение (t_П = +2°C)")]
        Manual2 = 2,

        /// <summary>
        /// Своё значение (t_П = +4°C): температура поверхности задана вручную
        /// </summary>
        [Description("Своё значение (t_П = +4°C)")]
        Manual4 = 4,

        /// <summary>
        /// Своё значение (t_П = +6°C): температура поверхности задана вручную
        /// </summary>
        [Description("Своё значение (t_П = +6°C)")]
        Manual6 = 6
    }
}