namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Контейнер данных гликолей из JSON
    /// </summary>
    /// <remarks>
    /// Загружается из data/glycol_data.json
    /// Содержит данные для этиленгликоля и пропиленгликоля
    /// </remarks>
    public class GlycolDataContainer
    {
        /// <summary>
        /// Доступные концентрации, %
        /// </summary>
        public double[] Concentrations { get; set; } = System.Array.Empty<double>();

        /// <summary>
        /// Доступные температуры, °C
        /// </summary>
        public double[] Temperatures { get; set; } = System.Array.Empty<double>();

        /// <summary>
        /// Данные для этиленгликоля
        /// </summary>
        public GlycolData EthyleneGlycol { get; set; } = new();

        /// <summary>
        /// Данные для пропиленгликоля
        /// </summary>
        public GlycolData PropyleneGlycol { get; set; } = new();
    }

    /// <summary>
    /// Данные для конкретного типа гликоли
    /// </summary>
    public class GlycolData
    {
        /// <summary>
        /// Доступные концентрации, %
        /// </summary>
        public double[] Concentrations { get; set; } = System.Array.Empty<double>();

        /// <summary>
        /// Доступные температуры, °C
        /// </summary>
        public double[] Temperatures { get; set; } = System.Array.Empty<double>();

        /// <summary>
        /// Таблица плотности (кг/м³)
        /// </summary>
        public GlycolDataTable Density { get; set; } = new();

        /// <summary>
        /// Таблица удельной теплоёмкости (кДж/(кг·К))
        /// </summary>
        public GlycolDataTable SpecificHeat { get; set; } = new();

        /// <summary>
        /// Таблица кинематической вязкости (мм²/с)
        /// </summary>
        public GlycolDataTable KinematicViscosity { get; set; } = new();

        /// <summary>
        /// Таблица теплопроводности (Вт/(м·К))
        /// </summary>
        public GlycolDataTable ThermalConductivity { get; set; } = new();
    }

    /// <summary>
    /// Таблица значений для билинейной интерполяции
    /// </summary>
    public class GlycolDataTable
    {
        /// <summary>
        /// Концентрации (строки)
        /// </summary>
        public double[] Concentrations { get; set; } = System.Array.Empty<double>();

        /// <summary>
        /// Температуры (столбцы)
        /// </summary>
        public double[] Temperatures { get; set; } = System.Array.Empty<double>();

        /// <summary>
        /// Значения [концентрация, температура]
        /// </summary>
        public double[,] Values { get; set; } = new double[0, 0];
    }
}