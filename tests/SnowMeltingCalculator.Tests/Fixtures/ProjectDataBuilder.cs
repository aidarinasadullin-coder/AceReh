using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Models.Project;

namespace SnowMeltingCalculator.Tests.Fixtures
{
    /// <summary>
    /// Fluent-builder тестового ProjectData (план 2026-09-18 «чистка раздутых
    /// тестов», волна D, решение V4(a)). Стартует пустым — эквивалентен
    /// <c>new ProjectData()</c>: слайс, который не затронут, остаётся null, как
    /// в заменяемом инлайне (wire-характеристика частичных проектов в
    /// ProjectRoundTripTests сохраняется). <see cref="WithDefaultSlices"/>
    /// инициализирует все четыре слайса пустыми — эквивалент явных
    /// <c>= new XxxProjectData()</c> в инлайнах ResultsViewModel-семейства.
    /// </summary>
    public sealed class ProjectDataBuilder
    {
        private readonly ProjectData _data = new();

        public ProjectDataBuilder WithVersion(string version)
        {
            _data.Version = version;
            return this;
        }

        public ProjectDataBuilder WithNumber(string projectNumber)
        {
            _data.ProjectNumber = projectNumber;
            return this;
        }

        public ProjectDataBuilder WithObject(string projectObject)
        {
            _data.ProjectObject = projectObject;
            return this;
        }

        public ProjectDataBuilder OperatingMode()
        {
            _data.IsOperatingMode = true;
            return this;
        }

        public ProjectDataBuilder WithDefaultSlices()
        {
            _data.ClimateData ??= new ClimateProjectData();
            _data.ConstructionData ??= new ConstructionProjectData();
            _data.ThermalData ??= new ThermalProjectData();
            _data.HydraulicsData ??= new HydraulicsProjectData();
            return this;
        }

        public ProjectDataBuilder WithClimate(Action<ClimateProjectData> configure)
        {
            _data.ClimateData ??= new ClimateProjectData();
            configure(_data.ClimateData);
            return this;
        }

        public ProjectDataBuilder WithConstruction(Action<ConstructionProjectData> configure)
        {
            _data.ConstructionData ??= new ConstructionProjectData();
            configure(_data.ConstructionData);
            return this;
        }

        public ProjectDataBuilder WithThermal(Action<ThermalProjectData> configure)
        {
            _data.ThermalData ??= new ThermalProjectData();
            configure(_data.ThermalData);
            return this;
        }

        public ProjectDataBuilder WithThermal(ThermalProjectData thermalData)
        {
            _data.ThermalData = thermalData;
            return this;
        }

        public ProjectDataBuilder WithHydraulics(Action<HydraulicsProjectData> configure)
        {
            _data.HydraulicsData ??= new HydraulicsProjectData();
            configure(_data.HydraulicsData);
            return this;
        }

        public ProjectData Build() => _data;

        /// <summary>
        /// Типовой HKV-D коллектор с нумерованными контурами — дефолты повторяют
        /// инлайны карточных/restore-тестов (SupplyLength 10, SupplySpacingCm 5,
        /// SupplyHeatPercent 10, PipeSpacingCm 20); нестандартные поля контура
        /// потребитель правит на полученном коллекторе явно.
        /// </summary>
        public static CollectorProjectData HkvDCollector(
            int collectorNumber,
            params double[] circuitLengths)
        {
            return new CollectorProjectData
            {
                CollectorNumber = collectorNumber,
                CollectorType = "HKV-D (2-12 контуров)",
                ValveType = ValveType.HKV_D,
                Circuits = circuitLengths.Select((circuitLength, index) => new CircuitProjectData
                {
                    CircuitNumber = index + 1,
                    CircuitLength = circuitLength,
                    SupplyLength = 10,
                    SupplySpacingCm = 5,
                    SupplyHeatPercent = 10,
                    PipeSpacingCm = 20
                }).ToList()
            };
        }
    }
}
