using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;

namespace SnowMeltingCalculator.Models.Project
{
    /// <summary>
    /// Модель данных проекта для сериализации в JSON
    /// </summary>
    public class ProjectData
    {
        /// <summary>
        /// Версия формата файла
        /// </summary>
        public string Version { get; set; } = "1.1";

        /// <summary>
        /// Номер проекта
        /// </summary>
        public string ProjectNumber { get; set; } = string.Empty;

        /// <summary>
        /// Наименование объекта
        /// </summary>
        public string ProjectObject { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания проекта
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Дата последнего изменения
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// Климатические данные
        /// </summary>
        public ClimateProjectData ClimateData { get; set; } = new();

        /// <summary>
        /// Данные конструкции
        /// </summary>
        public ConstructionProjectData ConstructionData { get; set; } = new();

        /// <summary>
        /// Данные теплового расчёта
        /// </summary>
        public ThermalProjectData ThermalData { get; set; } = new();

        /// <summary>
        /// Данные гидравлического расчёта
        /// </summary>
        public HydraulicsProjectData HydraulicsData { get; set; } = new();

        /// <summary>Режим отображения: рабочий (true) / расчётный (false)</summary>
        public bool IsOperatingMode { get; set; } = true;
    }

    /// <summary>
    /// Климатические данные для сохранения
    /// </summary>
    public class ClimateProjectData
    {
        /// <summary>
        /// Выбранный город
        /// </summary>
        public string SelectedCity { get; set; } = string.Empty;

        /// <summary>
        /// Регион города
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// Расчётная температура наружного воздуха
        /// </summary>
        public double AirTemperature { get; set; }

        /// <summary>
        /// Скорость ветра
        /// </summary>
        public double WindSpeed { get; set; }

        /// <summary>
        /// Относительная влажность
        /// </summary>
        public double Humidity { get; set; }

        /// <summary>
        /// Интенсивность снегопада
        /// </summary>
        public double SnowfallIntensity { get; set; }

        /// <summary>
        /// Климатическая зона
        /// </summary>
        public ClimateZone SelectedZone { get; set; }

        /// <summary>
        /// Признак повышенных требований
        /// </summary>
        public bool IsHighRequirements { get; set; }
    }

    /// <summary>
    /// Данные конструкции для сохранения
    /// </summary>
    public class ConstructionProjectData
    {
        /// <summary>
        /// Термическое сопротивление над трубой (R1)
        /// </summary>
        public double R1 { get; set; }

        /// <summary>
        /// Термическое сопротивление под трубой (R2)
        /// </summary>
        public double R2 { get; set; }

        /// <summary>
        /// Эквивалентная теплопроводность
        /// </summary>
        public double LambdaE { get; set; }

        /// <summary>
        /// Слои конструкции
        /// </summary>
        public List<LayerProjectData> Layers { get; set; } = new();
    }

    /// <summary>
    /// Данные слоя для сохранения
    /// </summary>
    public class LayerProjectData
    {
        /// <summary>
        /// Позиция слоя
        /// </summary>
        public LayerPosition Position { get; set; }

        /// <summary>
        /// Название материала
        /// </summary>
        public string MaterialName { get; set; } = string.Empty;

        /// <summary>
        /// Коэффициент теплопроводности материала
        /// </summary>
        public double MaterialLambda { get; set; }

        /// <summary>
        /// Толщина слоя, мм
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        /// Расчётное термическое сопротивление
        /// </summary>
        public double CalculatedR { get; set; }

        /// <summary>
        /// Расчётная теплопроводность
        /// </summary>
        public double CalculatedLambda { get; set; }

        /// <summary>
        /// Порядковый номер слоя в коллекции (0 = поверхность / ближайший к трубе для below)
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Данные теплового расчёта для сохранения
    /// </summary>
    public class ThermalProjectData
    {
        /// <summary>
        /// Выбранный режим работы
        /// </summary>
        public OperatingMode SelectedMode { get; set; }

        /// <summary>
        /// Температура подачи
        /// </summary>
        public double SupplyTemperature { get; set; }

        /// <summary>
        /// Температура грунта
        /// </summary>
        public double GroundTemperature { get; set; }

        /// <summary>
        /// Тип трубы
        /// </summary>
        public PipeTypeProjectData? SelectedPipe { get; set; }

        /// <summary>
        /// Шаг укладки трубы
        /// </summary>
        public int PipeSpacing { get; set; } = 200;

        /// <summary>
        /// Результат расчёта
        /// </summary>
        public ThermalResultProjectData? Result { get; set; }
    }

    /// <summary>
    /// Данные типа трубы для сохранения
    /// </summary>
    public class PipeTypeProjectData
    {
        /// <summary>
        /// Название трубы
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Наружный диаметр
        /// </summary>
        public double OuterDiameter { get; set; }

        /// <summary>
        /// Внутренний диаметр
        /// </summary>
        public double InnerDiameter { get; set; }

        /// <summary>
        /// Толщина стенки
        /// </summary>
        public double WallThickness { get; set; }
    }

    /// <summary>
    /// Результат теплового расчёта для сохранения
    /// </summary>
    public class ThermalResultProjectData
    {
        /// <summary>
        /// Удельная мощность вверх
        /// </summary>
        public double PowerUp { get; set; }

        /// <summary>
        /// Удельная мощность вниз
        /// </summary>
        public double PowerDown { get; set; }

        /// <summary>
        /// Суммарная удельная мощность
        /// </summary>
        public double PowerTotal { get; set; }

        /// <summary>
        /// Температура подачи
        /// </summary>
        public double SupplyTemperature { get; set; }

        /// <summary>
        /// Температура обратки
        /// </summary>
        public double ReturnTemperature { get; set; }

        /// <summary>
        /// Средняя температура
        /// </summary>
        public double MeanTemperature { get; set; }

        /// <summary>
        /// Температурный перепад
        /// </summary>
        public double DeltaT { get; set; }

        /// <summary>
        /// Признак валидности результата
        /// </summary>
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// Данные гидравлического расчёта для сохранения
    /// </summary>
    public class HydraulicsProjectData
    {
        /// <summary>
        /// Тип гликоля
        /// </summary>
        public GlycolType GlycolType { get; set; }

        /// <summary>
        /// Концентрация гликоля
        /// </summary>
        public double GlycolConcentration { get; set; }

        /// <summary>
        /// Шаг подводки, см
        /// </summary>
        public double SupplySpacingCm { get; set; }

        /// <summary>
        /// Доля потерь в подводке, %
        /// </summary>
        public double SupplyHeatPercent { get; set; }

        /// <summary>
        /// Коллекторы
        /// </summary>
        public List<CollectorProjectData> Collectors { get; set; } = new();
    }

    /// <summary>
    /// Результат расчёта контура для сохранения
    /// </summary>
    public class CircuitResultProjectData
    {
        public double Power { get; set; }
        public double FlowRate { get; set; }
        public double Velocity { get; set; }
        public double DpRohr { get; set; }
        public double DpVerteiler { get; set; }
        public double DpVent { get; set; }
        public double DpGesamt { get; set; }
        public double Throttling { get; set; }
        public double ValveTurns { get; set; }
        public string FlowRegime { get; set; } = string.Empty;

        /// <summary>
        /// Режим течения как строка (Laminar, Transitional, Turbulent)
        /// </summary>
        public string FlowRegimeString { get; set; } = string.Empty;

        public double Density { get; set; }
        public double KinematicViscosity { get; set; }
        public double ReynoldsNumber { get; set; }
        public double FrictionFactor { get; set; }
        public double PressureLossPerMeter { get; set; }
    }

    /// <summary>
    /// Итоги коллектора для сохранения
    /// </summary>
    public class CollectorSummaryProjectData
    {
        public int CircuitCount { get; set; }
        public double TotalPipeLength { get; set; }
        public double TotalPower { get; set; }
        public double TotalFlowRate { get; set; }
        public double PressureLoss_Operating_Pa { get; set; }
        public double PressureLoss_Cold_Pa { get; set; }
        public double Kv { get; set; }
        public string CollectorType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Данные коллектора для сохранения
    /// </summary>
    public class CollectorProjectData
    {
        /// <summary>
        /// Номер коллектора
        /// </summary>
        public int CollectorNumber { get; set; }

        /// <summary>
        /// Тип коллектора (строка для отображения)
        /// </summary>
        public string CollectorType { get; set; } = "HKV-D (2-12 контуров)";

        /// <summary>
        /// Тип клапана
        /// </summary>
        public ValveType ValveType { get; set; }

        /// <summary>
        /// Контуры
        /// </summary>
        public List<CircuitProjectData> Circuits { get; set; } = new();

        /// <summary>
        /// Итоги по коллектору
        /// </summary>
        public CollectorSummaryProjectData? Summary { get; set; }
    }

    /// <summary>
    /// Данные контура для сохранения
    /// </summary>
    public class CircuitProjectData
    {
        /// <summary>
        /// Номер контура
        /// </summary>
        public int CircuitNumber { get; set; }

        /// <summary>
        /// Длина контура
        /// </summary>
        public double CircuitLength { get; set; }

        /// <summary>
        /// Длина подводки
        /// </summary>
        public double SupplyLength { get; set; }

        /// <summary>
        /// Шаг подводки, см
        /// </summary>
        public double SupplySpacingCm { get; set; }

        /// <summary>
        /// Доля потерь в подводке, %
        /// </summary>
        public double SupplyHeatPercent { get; set; }

        /// <summary>
        /// Шаг укладки, см
        /// </summary>
        public double PipeSpacingCm { get; set; }

        /// <summary>
        /// Результат при рабочей температуре
        /// </summary>
        public CircuitResultProjectData? OperatingResult { get; set; }

        /// <summary>
        /// Результат при расчётной температуре
        /// </summary>
        public CircuitResultProjectData? DesignResult { get; set; }

        /// <summary>
        /// Мощность контура, Вт
        /// </summary>
        public double Power { get; set; }

        /// <summary>
        /// Расход теплоносителя, л/ч
        /// </summary>
        public double FlowRate { get; set; }

        /// <summary>
        /// Скорость потока, м/с
        /// </summary>
        public double Velocity { get; set; }

        /// <summary>
        /// Описание режима течения
        /// </summary>
        public string FlowRegimeDescription { get; set; } = string.Empty;

        /// <summary>
        /// Дросселирование для балансировки, Па
        /// </summary>
        public double Throttling { get; set; }

        /// <summary>
        /// Обороты балансировочного клапана
        /// </summary>
        public double ValveTurns { get; set; }
    }
}
