using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Services.Results
{
    /// <summary>
    /// Данные контура для PDF экспорта
    /// </summary>
    public class CircuitPdfData
    {
        /// <summary>Номер контура</summary>
        public int CircuitNumber { get; set; }

        /// <summary>Длина контура, м</summary>
        public double Length { get; set; }

        /// <summary>Площадь контура, м²</summary>
        public double Area { get; set; }

        /// <summary>Мощность контура, Вт</summary>
        public double Power { get; set; }

        /// <summary>Расход, л/ч</summary>
        public double FlowRate { get; set; }

        /// <summary>Скорость потока, м/с</summary>
        public double Velocity { get; set; }

        /// <summary>Режим течения</summary>
        public string FlowRegime { get; set; } = string.Empty;

        /// <summary>Удельные потери давления, Па/м</summary>
        public double PressureLossPerMeter { get; set; }

        /// <summary>Потери в трубе, кПа</summary>
        public double DpRohr { get; set; }

        /// <summary>Потери в распределителе, кПа</summary>
        public double DpVerteiler { get; set; }

        /// <summary>Потери в вентиле, кПа</summary>
        public double DpVent { get; set; }

        /// <summary>Суммарные потери, кПа</summary>
        public double DpGesamt { get; set; }

        /// <summary>Дросселирование, кПа</summary>
        public double Throttling { get; set; }

        /// <summary>Значение дросселирования (ZuDrosseln), кПа</summary>
        public double ZuDrosseln { get; set; }

        /// <summary>Обороты клапана</summary>
        public double ValveTurns { get; set; }
    }

    /// <summary>
    /// Итоги по коллектору для PDF
    /// </summary>
    public class CollectorSummaryPdfData
    {
        /// <summary>Количество контуров</summary>
        public int CircuitCount { get; set; }

        /// <summary>Общая длина труб, м</summary>
        public double TotalPipeLength { get; set; }

        /// <summary>Общая мощность, Вт</summary>
        public double TotalPower { get; set; }

        /// <summary>Общий расход, л/ч</summary>
        public double TotalFlowRate { get; set; }

        /// <summary>Максимальные потери давления при рабочей температуре, кПа</summary>
        public double PressureLoss_Operating_kPa { get; set; }

        /// <summary>Максимальные потери давления при холодной температуре, кПа</summary>
        public double PressureLoss_Cold_kPa { get; set; }

        /// <summary>Kv коллектора</summary>
        public double Kv { get; set; }

        /// <summary>Тип коллектора</summary>
        public string CollectorType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Данные коллектора для PDF экспорта
    /// </summary>
    public class CollectorPdfData
    {
        /// <summary>Номер коллектора</summary>
        public int Number { get; set; }

        /// <summary>Тип коллектора</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Список контуров</summary>
        public List<CircuitPdfData> Circuits { get; set; } = new();

        /// <summary>Итоги по коллектору</summary>
        public CollectorSummaryPdfData Summary { get; set; } = new();
    }

    /// <summary>
    /// Данные слоя конструкции для PDF
    /// </summary>
    public class LayerPdfData
    {
        /// <summary>Название материала</summary>
        public string MaterialName { get; set; } = string.Empty;

        /// <summary>Толщина, мм</summary>
        public double Thickness { get; set; }

        /// <summary>Теплопроводность, Вт/м·К</summary>
        public double Lambda { get; set; }

        /// <summary>Термическое сопротивление, м²·К/Вт</summary>
        public double R { get; set; }

        /// <summary>Позиция (над/под трубой)</summary>
        public string Position { get; set; } = string.Empty;
    }

    /// <summary>
    /// Спецификация коллектора для PDF
    /// </summary>
    public class CollectorSpecPdfData
    {
        /// <summary>Номер коллектора</summary>
        public int Number { get; set; }

        /// <summary>Тип коллектора</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Количество контуров</summary>
        public int CircuitCount { get; set; }

        /// <summary>Суммарная мощность, кВт</summary>
        public double TotalPower_kW { get; set; }

        /// <summary>Суммарный расход, м³/ч</summary>
        public double TotalFlowRate_m3h { get; set; }

        /// <summary>Потери давления, мбар</summary>
        public double PressureLoss_mbar { get; set; }

        /// <summary>Kv клапана</summary>
        public double Kv { get; set; }
    }

    /// <summary>
    /// Модель данных для экспорта результатов в PDF
    /// </summary>
    public class ResultsPdfData
    {
        #region Информация о проекте

        /// <summary>Номер проекта</summary>
        public string ProjectNumber { get; set; } = string.Empty;

        /// <summary>Наименование объекта</summary>
        public string ProjectObject { get; set; } = string.Empty;

        /// <summary>Дата создания отчёта</summary>
        public DateTime ReportDate { get; set; } = DateTime.Now;

        #endregion

        #region KPI показатели

        /// <summary>Суммарная тепловая мощность, кВт</summary>
        public double TotalThermalPower_kW { get; set; }

        /// <summary>Объём системы, литры</summary>
        public double SystemVolume_L { get; set; }

        /// <summary>Расход насоса, м³/ч</summary>
        public double PumpFlowRate_m3h { get; set; }

        /// <summary>Напор насоса, кПа</summary>
        public double PumpHead_kPa { get; set; }

        /// <summary>Объём расширительного бака, литры</summary>
        public double ExpansionTankVolume_L { get; set; }

        #endregion

        #region Температуры

        /// <summary>Температура подачи, °C</summary>
        public double SupplyTemperature { get; set; }

        /// <summary>Температура обратки, °C</summary>
        public double ReturnTemperature { get; set; }

        /// <summary>Рабочая температура, °C</summary>
        public double OperatingTemperature { get; set; }

        /// <summary>Температура грунта, °C</summary>
        public double GroundTemperature { get; set; }

        /// <summary>Температура поверхности, °C</summary>
        public int SurfaceTemperature { get; set; }

        #endregion

        #region Климатические данные

        /// <summary>Город</summary>
        public string City { get; set; } = string.Empty;

        /// <summary>Расчётная температура наружного воздуха, °C</summary>
        public double DesignTemperature { get; set; }

        /// <summary>Скорость ветра, м/с</summary>
        public double WindSpeed { get; set; }

        /// <summary>Интенсивность снегопада, мм/ч</summary>
        public double SnowfallIntensity { get; set; }

        /// <summary>Климатическая зона</summary>
        public ClimateZone ClimateZone { get; set; }

        /// <summary>Количество дней холодного периода</summary>
        public int ColdPeriodDays { get; set; }

        #endregion

        #region Параметры трубы

        /// <summary>Тип трубы</summary>
        public string PipeType { get; set; } = string.Empty;

        /// <summary>Шаг укладки, мм</summary>
        public int PipeSpacing { get; set; }

        #endregion

        #region Режим работы и теплоноситель

        /// <summary>Режим работы</summary>
        public OperatingMode OperatingMode { get; set; }

        /// <summary>Тип гликоля</summary>
        public GlycolType GlycolType { get; set; }

        /// <summary>Концентрация гликоля, %</summary>
        public double GlycolConcentration { get; set; }

        /// <summary>Название теплоносителя</summary>
        public string GlycolTypeDisplayName => GetGlycolTypeName(GlycolType);

        private static string GetGlycolTypeName(GlycolType type)
        {
            return type switch
            {
                GlycolType.Ethylene => "Этиленгликоль",
                GlycolType.Propylene => "Пропиленгликоль",
                _ => "Вода"
            };
        }

        #endregion

        #region Конструкция

        /// <summary>Термическое сопротивление над трубой (R1), м²·К/Вт</summary>
        public double R1 { get; set; }

        /// <summary>Термическое сопротивление под трубой (R2), м²·К/Вт</summary>
        public double R2 { get; set; }

        /// <summary>Теплопроводность материала вокруг трубы (LambdaE), Вт/м·К</summary>
        public double LambdaE { get; set; }

        /// <summary>Удельная мощность вверх, Вт/м²</summary>
        public double PowerUp { get; set; }

        /// <summary>Удельная мощность вниз, Вт/м²</summary>
        public double PowerDown { get; set; }

        /// <summary>Суммарная удельная мощность, Вт/м²</summary>
        public double TotalPowerDensity { get; set; }

        /// <summary>Слои конструкции</summary>
        public List<LayerPdfData> Layers { get; set; } = new();

        /// <summary>Изображение схемы конструкции (PNG)</summary>
        public byte[]? ConstructionImageBytes { get; set; }

        #endregion

        #region Гидравлический расчёт

        /// <summary>Список коллекторов с контурами</summary>
        public List<CollectorPdfData> Collectors { get; set; } = new();

        #endregion

        #region Оборудование

        /// <summary>Спецификации коллекторов</summary>
        public List<CollectorSpecPdfData> CollectorSpecifications { get; set; } = new();

        /// <summary>Общая длина труб, м</summary>
        public double TotalPipeLength { get; set; }

        /// <summary>Количество РЗС</summary>
        public int RzsCount { get; set; }

        #endregion
    }
}
