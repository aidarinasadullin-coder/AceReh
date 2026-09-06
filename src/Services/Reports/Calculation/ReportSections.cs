using System.Collections.Generic;

namespace SnowMeltingCalculator.Services.Reports.Calculation
{
    /// <summary>
    /// Шаг расчёта детального отчёта (ADR-010): формула → подстановка реальных
    /// значений проекта → результат. Подстановочный текст собирается билдером
    /// из тех же <see cref="ReportValue{T}"/>, что идут в таблицы раздела
    /// (Derived), поэтому числа шага и таблиц не могут разойтись.
    /// </summary>
    public sealed class CalculationStep
    {
        /// <summary>Стабильный ключ шага (например, «thermal.alpha»).</summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>Заголовок шага (название величины с обозначением).</summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>Формульная запись (обозначения, из кода/документации).</summary>
        public string FormulaText { get; init; } = string.Empty;

        /// <summary>Подстановка чисел проекта в формулу.</summary>
        public string SubstitutionText { get; init; } = string.Empty;

        /// <summary>Результат шага: значение, единица, источник.</summary>
        public ReportValue<double> Result { get; init; } = new();

        /// <summary>Примечание (физический смысл, справочные оговорки).</summary>
        public string? Note { get; init; }

        /// <summary>Входные значения подстановки — метаданные источника чисел.</summary>
        public IReadOnlyList<ReportValue<double>> Inputs { get; init; } = new List<ReportValue<double>>();
    }

    /// <summary>
    /// Запись таблицы констант расчёта (значения — из кода программы).
    /// </summary>
    public sealed class ReportConstantEntry
    {
        /// <summary>Название константы.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Обозначение в формулах.</summary>
        public string Symbol { get; init; } = string.Empty;

        /// <summary>Значение (форматирование — Derived).</summary>
        public double Value { get; init; }

        /// <summary>Количество знаков после разделителя при выводе.</summary>
        public int Decimals { get; init; }

        /// <summary>Единица измерения.</summary>
        public string Unit { get; init; } = string.Empty;

        /// <summary>Источник в коде (класс/файл).</summary>
        public string SourceDetail { get; init; } = string.Empty;
    }

    /// <summary>
    /// Референсный контур гидравлического раздела (В4): контур с максимальными
    /// потерями худшего коллектора; при ничьей — минимальный номер контура.
    /// </summary>
    public sealed class ReferenceCircuitSection
    {
        /// <summary>Номер коллектора референсного контура.</summary>
        public int CollectorNumber { get; init; }

        /// <summary>Номер контура.</summary>
        public int CircuitNumber { get; init; }

        /// <summary>Тип коллектора.</summary>
        public string CollectorType { get; init; } = string.Empty;

        /// <summary>Длина контура с подводкой, м.</summary>
        public ReportValue<double> TotalLength { get; init; } = new();

        /// <summary>Цепочка шагов: Q_HK → V̇ → v → Re → λ → R → DpRohr → DpVerteiler → DpVent → DpGesamt.</summary>
        public IReadOnlyList<CalculationStep> Steps { get; init; } = new List<CalculationStep>();

        /// <summary>Шаги примера балансировки: Δp дросселя → Kv (формула) → обороты.</summary>
        public IReadOnlyList<CalculationStep> BalancingSteps { get; init; } = new List<CalculationStep>();

        /// <summary>Примечание о правиле вычитания для типа коллектора (HKV-D/IV).</summary>
        public string? BalancingNote { get; init; }

        /// <summary>Примечание о семантике DpVent (полностью открытый клапан, не пересчитывается).</summary>
        public string? DpVentNote { get; init; }
    }

    /// <summary>
    /// Слой конструкции в отчёте.
    /// </summary>
    public sealed class ReportConstructionLayer
    {        /// <summary>
        /// Позиция слоя (над / под трубой).
        /// </summary>
        public string Position { get; init; } = string.Empty;

        /// <summary>
        /// Материал слоя.
        /// </summary>
        public ReportValue<string> MaterialName { get; init; } = new();

        /// <summary>
        /// Толщина слоя.
        /// </summary>
        public ReportValue<double> Thickness { get; init; } = new();

        /// <summary>
        /// Коэффициент теплопроводности.
        /// </summary>
        public ReportValue<double> Lambda { get; init; } = new();

        /// <summary>
        /// Термическое сопротивление слоя.
        /// </summary>
        public ReportValue<double> ThermalResistance { get; init; } = new();
    }

    /// <summary>
    /// Раздел проекта.
    /// </summary>
    public sealed class ProjectSection
    {
        /// <summary>
        /// Номер проекта.
        /// </summary>
        public string ProjectNumber { get; init; } = string.Empty;

        /// <summary>
        /// Наименование объекта.
        /// </summary>
        public string ProjectObject { get; init; } = string.Empty;
    }

    /// <summary>
    /// Раздел климатических данных и входных тепловых параметров.
    /// </summary>
    public sealed class ClimateSection
    {
        /// <summary>
        /// Город.
        /// </summary>
        public ReportValue<string> City { get; init; } = new();

        /// <summary>
        /// Регион.
        /// </summary>
        public ReportValue<string> Region { get; init; } = new();

        /// <summary>
        /// Расчётная температура наружного воздуха.
        /// </summary>
        public ReportValue<double> AirTemperature { get; init; } = new();

        /// <summary>
        /// Скорость ветра.
        /// </summary>
        public ReportValue<double> WindSpeed { get; init; } = new();

        /// <summary>
        /// Относительная влажность (условно, не участвует в расчёте).
        /// </summary>
        public ReportValue<double> Humidity { get; init; } = new();

        /// <summary>
        /// Интенсивность снегопада.
        /// </summary>
        public ReportValue<double> SnowfallIntensity { get; init; } = new();

        /// <summary>
        /// Климатическая зона.
        /// </summary>
        public ReportValue<string> ClimateZone { get; init; } = new();

        /// <summary>
        /// Количество дней холодного периода.
        /// </summary>
        public ReportValue<double> ColdPeriodDays { get; init; } = new();

        /// <summary>
        /// Температура поверхности.
        /// </summary>
        public ReportValue<double> SurfaceTemperature { get; init; } = new();

        /// <summary>
        /// Температура грунта.
        /// </summary>
        public ReportValue<double> GroundTemperature { get; init; } = new();

        /// <summary>
        /// Температура подачи.
        /// </summary>
        public ReportValue<double> SupplyTemperature { get; init; } = new();

        /// <summary>
        /// Температура обратки.
        /// </summary>
        public ReportValue<double> ReturnTemperature { get; init; } = new();

        /// <summary>
        /// Средняя температура теплоносителя.
        /// </summary>
        public ReportValue<double> MeanTemperature { get; init; } = new();

        /// <summary>
        /// Температурный перепад.
        /// </summary>
        public ReportValue<double> DeltaT { get; init; } = new();
    }

    /// <summary>
    /// Раздел конструкции.
    /// </summary>
    public sealed class ConstructionSection
    {
        /// <summary>
        /// Уровень грунтовых вод.
        /// </summary>
        public ReportValue<double> GroundwaterLevel { get; init; } = new();

        /// <summary>
        /// Сопротивление вверх.
        /// </summary>
        public ReportValue<double> R1 { get; init; } = new();

        /// <summary>
        /// Сопротивление вниз.
        /// </summary>
        public ReportValue<double> R2 { get; init; } = new();

        /// <summary>
        /// Эквивалентная теплопроводность материала вокруг трубы.
        /// </summary>
        public ReportValue<double> LambdaE { get; init; } = new();

        /// <summary>
        /// Примечание о правиле выбора λА/λБ по уровню грунтовых вод.
        /// </summary>
        public string? LambdaRuleNote { get; init; }

        /// <summary>
        /// Шаги расчёта R1/R2 с подстановкой по слоям.
        /// </summary>
        public IReadOnlyList<CalculationStep> Steps { get; init; } = new List<CalculationStep>();

        /// <summary>
        /// Слои конструкции.
        /// </summary>
        public IReadOnlyList<ReportConstructionLayer> Layers { get; init; } = new List<ReportConstructionLayer>();
    }

    /// <summary>
    /// Раздел теплотехнического расчёта.
    /// </summary>
    public sealed class ThermalSection
    {
        /// <summary>
        /// Коэффициент теплоотдачи.
        /// </summary>
        public ReportValue<double> Alpha { get; init; } = new();

        /// <summary>
        /// Мощность на плавление снега.
        /// </summary>
        public ReportValue<double> MeltingHeat { get; init; } = new();

        /// <summary>
        /// Лучистый тепловой поток (справочно).
        /// </summary>
        public ReportValue<double> RadiationHeat { get; init; } = new();

        /// <summary>
        /// Конвективный тепловой поток.
        /// </summary>
        public ReportValue<double> ConvectionHeat { get; init; } = new();

        /// <summary>
        /// Полезная мощность вверх.
        /// </summary>
        public ReportValue<double> PowerUp { get; init; } = new();

        /// <summary>
        /// Мощность вниз.
        /// </summary>
        public ReportValue<double> PowerDown { get; init; } = new();

        /// <summary>
        /// Суммарная удельная мощность.
        /// </summary>
        public ReportValue<double> TotalPowerDensity { get; init; } = new();

        /// <summary>
        /// Полное сопротивление вверх.
        /// </summary>
        public ReportValue<double> RFb { get; init; } = new();

        /// <summary>
        /// Полное сопротивление вниз.
        /// </summary>
        public ReportValue<double> RD { get; init; } = new();

        /// <summary>
        /// Параметр затухания.
        /// </summary>
        public ReportValue<double> ParameterM { get; init; } = new();

        /// <summary>
        /// КПД ребра.
        /// </summary>
        public ReportValue<double> EfficiencyEtaR { get; init; } = new();

        /// <summary>
        /// Избыточная температура теплоносителя.
        /// </summary>
        public ReportValue<double> ExcessTemperature { get; init; } = new();

        /// <summary>
        /// Массовый расход на м².
        /// </summary>
        public ReportValue<double> MassFlowRate { get; init; } = new();

        /// <summary>
        /// Объёмный расход на м².
        /// </summary>
        public ReportValue<double> VolumeFlowRate { get; init; } = new();

        /// <summary>
        /// Плотность снега.
        /// </summary>
        public ReportValue<double> SnowDensity { get; init; } = new();

        /// <summary>
        /// Теплоёмкость льда.
        /// </summary>
        public ReportValue<double> IceHeatCapacity { get; init; } = new();

        /// <summary>
        /// Теплота плавления льда.
        /// </summary>
        public ReportValue<double> IceMeltingHeat { get; init; } = new();

        /// <summary>
        /// Теплоёмкость воды.
        /// </summary>
        public ReportValue<double> WaterHeatCapacity { get; init; } = new();

        /// <summary>
        /// Признак доступности детальных тепловых величин (ADR-010). false —
        /// рендер выводит маркер «нет данных».
        /// </summary>
        public bool IsDetailAvailable { get; init; }

        /// <summary>
        /// Источник детальных величин (снимок сессии / контрольный пересчёт) —
        /// для строки под разделом.
        /// </summary>
        public string DetailSourceDescription { get; init; } = string.Empty;

        /// <summary>
        /// Примечание провайдера (пересчёт, расхождение с сохранёнными
        /// мощностями, дефолтный теплоноситель).
        /// </summary>
        public string? DetailNote { get; init; }

        /// <summary>
        /// Ошибки валидации расчёта/пересчёта (В7) — выводятся в разделе
        /// «Проверки».
        /// </summary>
        public IReadOnlyList<string> DetailValidationErrors { get; init; } = new List<string>();

        /// <summary>
        /// Пошаговый расчёт (α → Qтаяния → … → расходы).
        /// </summary>
        public IReadOnlyList<CalculationStep> Steps { get; init; } = new List<CalculationStep>();

        /// <summary>
        /// Таблица констант расчёта (значения из кода программы).
        /// </summary>
        public IReadOnlyList<ReportConstantEntry> Constants { get; init; } = new List<ReportConstantEntry>();

        /// <summary>
        /// Коэффициенты A–E теории стержня (таблицей, значения пересчёта).
        /// </summary>
        public IReadOnlyList<ReportValue<double>> RodTheoryCoefficients { get; init; } = new List<ReportValue<double>>();
    }

    /// <summary>
    /// Контур в гидравлическом разделе.
    /// </summary>
    public sealed class ReportCircuit
    {
        /// <summary>
        /// Номер контура.
        /// </summary>
        public int CircuitNumber { get; init; }

        /// <summary>
        /// Длина греющего участка.
        /// </summary>
        public ReportValue<double> CircuitLength { get; init; } = new();

        /// <summary>
        /// Площадь контура.
        /// </summary>
        public ReportValue<double> CircuitArea { get; init; } = new();

        /// <summary>
        /// Длина подводки.
        /// </summary>
        public ReportValue<double> SupplyLength { get; init; } = new();

        /// <summary>
        /// Общая длина контура.
        /// </summary>
        public ReportValue<double> TotalLength { get; init; } = new();

        /// <summary>
        /// Шаг укладки.
        /// </summary>
        public ReportValue<double> PipeSpacing { get; init; } = new();

        /// <summary>
        /// Шаг подводки.
        /// </summary>
        public ReportValue<double> SupplySpacing { get; init; } = new();

        /// <summary>
        /// Доля тепла подводки.
        /// </summary>
        public ReportValue<double> SupplyHeatPercent { get; init; } = new();

        /// <summary>
        /// Мощность контура.
        /// </summary>
        public ReportValue<double> Power { get; init; } = new();

        /// <summary>
        /// Расход теплоносителя.
        /// </summary>
        public ReportValue<double> FlowRate { get; init; } = new();

        /// <summary>
        /// Скорость потока.
        /// </summary>
        public ReportValue<double> Velocity { get; init; } = new();

        /// <summary>
        /// Плотность теплоносителя.
        /// </summary>
        public ReportValue<double> Density { get; init; } = new();

        /// <summary>
        /// Кинематическая вязкость.
        /// </summary>
        public ReportValue<double> KinematicViscosity { get; init; } = new();

        /// <summary>
        /// Число Рейнольдса.
        /// </summary>
        public ReportValue<double> ReynoldsNumber { get; init; } = new();

        /// <summary>
        /// Коэффициент трения.
        /// </summary>
        public ReportValue<double> FrictionFactor { get; init; } = new();

        /// <summary>
        /// Удельные потери давления.
        /// </summary>
        public ReportValue<double> PressureLossPerMeter { get; init; } = new();

        /// <summary>
        /// Потери в трубе.
        /// </summary>
        public ReportValue<double> DpRohr { get; init; } = new();

        /// <summary>
        /// Потери в распределителе.
        /// </summary>
        public ReportValue<double> DpVerteiler { get; init; } = new();

        /// <summary>
        /// Потери в вентиле.
        /// </summary>
        public ReportValue<double> DpVent { get; init; } = new();

        /// <summary>
        /// Суммарные потери контура.
        /// </summary>
        public ReportValue<double> DpGesamt { get; init; } = new();

        /// <summary>
        /// Дросселирование для балансировки.
        /// </summary>
        public ReportValue<double> Throttling { get; init; } = new();

        /// <summary>
        /// Значение ZuDrosseln.
        /// </summary>
        public ReportValue<double> ZuDrosseln { get; init; } = new();

        /// <summary>
        /// Обороты балансировочного клапана.
        /// </summary>
        public ReportValue<double> ValveTurns { get; init; } = new();

        /// <summary>
        /// Режим течения.
        /// </summary>
        public ReportValue<string> FlowRegime { get; init; } = new();
    }

    /// <summary>
    /// Сводка по коллектору.
    /// </summary>
    public sealed class ReportCollectorSummary
    {
        /// <summary>
        /// Тип коллектора.
        /// </summary>
        public ReportValue<string> CollectorType { get; init; } = new();

        /// <summary>
        /// Количество контуров.
        /// </summary>
        public ReportValue<double> CircuitCount { get; init; } = new();

        /// <summary>
        /// Общая длина труб.
        /// </summary>
        public ReportValue<double> TotalPipeLength { get; init; } = new();

        /// <summary>
        /// Общая мощность.
        /// </summary>
        public ReportValue<double> TotalPower { get; init; } = new();

        /// <summary>
        /// Общий расход.
        /// </summary>
        public ReportValue<double> TotalFlowRate { get; init; } = new();

        /// <summary>
        /// Максимальные потери давления (выбранный режим).
        /// </summary>
        public ReportValue<double> PressureLoss { get; init; } = new();

        /// <summary>
        /// Kv коллектора / клапана.
        /// </summary>
        public ReportValue<double> Kv { get; init; } = new();
    }

    /// <summary>
    /// Коллектор в гидравлическом разделе.
    /// </summary>
    public sealed class ReportCollector
    {
        /// <summary>
        /// Номер коллектора.
        /// </summary>
        public int Number { get; init; }

        /// <summary>
        /// Тип коллектора.
        /// </summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>
        /// Контуры коллектора.
        /// </summary>
        public IReadOnlyList<ReportCircuit> Circuits { get; init; } = new List<ReportCircuit>();

        /// <summary>
        /// Сводка по коллектору.
        /// </summary>
        public ReportCollectorSummary Summary { get; init; } = new();
    }

    /// <summary>
    /// Строка сравнения «рабочий vs холодный пуск» по коллектору (В3).
    /// Значения — сохранённые результаты соответствующего режима.
    /// </summary>
    public sealed class ModeComparisonRow
    {
        /// <summary>Номер коллектора.</summary>
        public int CollectorNumber { get; init; }

        /// <summary>Тип коллектора.</summary>
        public string CollectorType { get; init; } = string.Empty;

        /// <summary>Вязкость (рабочий/холодный), мм²/с.</summary>
        public double WorkingViscosity { get; init; }
        public double ColdViscosity { get; init; }

        /// <summary>Re худшего контура (рабочий/холодный).</summary>
        public double WorkingReynolds { get; init; }
        public double ColdReynolds { get; init; }

        /// <summary>λ худшего контура (рабочий/холодный).</summary>
        public double WorkingFriction { get; init; }
        public double ColdFriction { get; init; }

        /// <summary>Потери давления коллектора (рабочий/холодный), Па.</summary>
        public double WorkingPressureLossPa { get; init; }
        public double ColdPressureLossPa { get; init; }

        /// <summary>Кратность роста потерь (холодный/рабочий; Derived).</summary>
        public double GrowthRatio { get; init; }
    }

    /// <summary>
    /// Раздел гидравлического расчёта.
    /// </summary>
    public sealed class HydraulicsSection
    {
        /// <summary>
        /// Референсный контур с цепочкой шагов и примером балансировки (В4).
        /// null — результаты выбранного режима отсутствуют (missing-data).
        /// </summary>
        public ReferenceCircuitSection? ReferenceCircuit { get; init; }

        /// <summary>
        /// Сравнение «рабочий vs холодный пуск» (В3, режим DesignCold).
        /// </summary>
        public IReadOnlyList<ModeComparisonRow> ModeComparison { get; init; } = new List<ModeComparisonRow>();

        /// <summary>
        /// Тип гликоля.
        /// </summary>
        public ReportValue<string> GlycolType { get; init; } = new();

        /// <summary>
        /// Концентрация гликоля.
        /// </summary>
        public ReportValue<double> GlycolConcentration { get; init; } = new();

        /// <summary>
        /// Плотность теплоносителя.
        /// </summary>
        public ReportValue<double> Density { get; init; } = new();

        /// <summary>
        /// Удельная теплоёмкость.
        /// </summary>
        public ReportValue<double> SpecificHeat { get; init; } = new();

        /// <summary>
        /// Кинематическая вязкость.
        /// </summary>
        public ReportValue<double> KinematicViscosity { get; init; } = new();

        /// <summary>
        /// Коллекторы.
        /// </summary>
        public IReadOnlyList<ReportCollector> Collectors { get; init; } = new List<ReportCollector>();
    }

    /// <summary>
    /// Спецификация коллектора для раздела оборудования.
    /// </summary>
    public sealed class ReportCollectorSpecification
    {
        /// <summary>
        /// Номер коллектора.
        /// </summary>
        public int Number { get; init; }

        /// <summary>
        /// Тип коллектора.
        /// </summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>
        /// Количество контуров.
        /// </summary>
        public int CircuitCount { get; init; }

        /// <summary>
        /// Суммарная мощность.
        /// </summary>
        public ReportValue<double> TotalPower { get; init; } = new();

        /// <summary>
        /// Суммарный расход.
        /// </summary>
        public ReportValue<double> TotalFlowRate { get; init; } = new();

        /// <summary>
        /// Потери давления.
        /// </summary>
        public ReportValue<double> PressureLoss { get; init; } = new();

        /// <summary>
        /// Kv клапана.
        /// </summary>
        public ReportValue<double> Kv { get; init; } = new();
    }

    /// <summary>
    /// Раздел оборудования и KPI.
    /// </summary>
    public sealed class EquipmentSection
    {
        /// <summary>
        /// Суммарная тепловая мощность.
        /// </summary>
        public ReportValue<double> TotalThermalPower { get; init; } = new();

        /// <summary>
        /// Объём системы.
        /// </summary>
        public ReportValue<double> SystemVolume { get; init; } = new();

        /// <summary>
        /// Расход насоса.
        /// </summary>
        public ReportValue<double> PumpFlowRate { get; init; } = new();

        /// <summary>
        /// Напор насоса.
        /// </summary>
        public ReportValue<double> PumpHead { get; init; } = new();

        /// <summary>
        /// Объём расширительного бака.
        /// </summary>
        public ReportValue<double> ExpansionTankVolume { get; init; } = new();

        /// <summary>
        /// Общая длина труб.
        /// </summary>
        public ReportValue<double> TotalPipeLength { get; init; } = new();

        /// <summary>
        /// Количество РЗС / коллекторов.
        /// </summary>
        public ReportValue<double> RzsCount { get; init; } = new();

        /// <summary>
        /// Спецификации коллекторов.
        /// </summary>
        public IReadOnlyList<ReportCollectorSpecification> CollectorSpecifications { get; init; } = new List<ReportCollectorSpecification>();
    }

    /// <summary>
    /// Приложение источников.
    /// </summary>
    public sealed class SourcesAppendix
    {
        /// <summary>
        /// Записи метаданных параметров.
        /// </summary>
        public IReadOnlyList<ReportParameterMetadata> Entries { get; init; } = new List<ReportParameterMetadata>();
    }

    /// <summary>
    /// Приложение формул.
    /// </summary>
    public sealed class FormulasAppendix
    {
        /// <summary>
        /// Записи формул.
        /// </summary>
        public IReadOnlyList<ReportFormula> Formulas { get; init; } = new List<ReportFormula>();
    }
}
