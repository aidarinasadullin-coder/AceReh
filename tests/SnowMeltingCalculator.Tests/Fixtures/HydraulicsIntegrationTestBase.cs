using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.Core;

namespace SnowMeltingCalculator.Tests.Fixtures
{
    /// <summary>
    /// Общий аранж интеграционных тестов гидравлики: моки сервисов,
    /// ThermalViewModel/ClimateViewModel на реальных ClimateData/ConstructionData,
    /// CircuitsViewModel с дефолтным коллектором из двух контуров.
    /// Канон — ThermalToHydraulicsIntegrationTests; копии сверены машинно
    /// 2026-09-18: Thermal/Climate/PipeSpacing/DoubleCalculation идентичны,
    /// отличия Glycol (без PipeSpacing-бэкинга) и WriterAuthority
    /// (CalculateCollectorSummary на TotalLength/IsValid) — переопределяются
    /// в наследниках, см. ConfigurePipeSpacingBacking/Setup.
    /// </summary>
    public abstract class HydraulicsIntegrationTestBase
    {
        protected Mock<ICircuitsCalculator> _circuitsCalculatorMock = null!;
        protected Mock<IGlycolDataService> _glycolServiceMock = null!;
        protected Mock<IThermalCalculator> _thermalCalculatorMock = null!;
        protected Mock<IClimateDataService> _climateDataServiceMock = null!;
        protected Mock<ICalculationStateService> _calculationStateServiceMock = null!;
        protected Mock<ICircuitsValidator> _validatorMock = null!;
        protected Mock<ICollectorTypeSelector> _collectorTypeSelectorMock = null!;
        protected Mock<IMarkDirtyService> _markDirtyServiceMock = null!;
        protected ClimateData _climateData = null!;
        protected ConstructionData _constructionData = null!;
        protected ThermalViewModel _thermalViewModel = null!;
        protected ClimateViewModel _climateViewModel = null!;
        protected CalculationContext _calculationContext = null!;
        protected CircuitsViewModel _viewModel = null!;

        [SetUp]
        public virtual void Setup()
        {
            _circuitsCalculatorMock = new Mock<ICircuitsCalculator>();
            _glycolServiceMock = new Mock<IGlycolDataService>();
            _thermalCalculatorMock = new Mock<IThermalCalculator>();
            _climateDataServiceMock = new Mock<IClimateDataService>();
            _calculationStateServiceMock = new Mock<ICalculationStateService>();
            _validatorMock = new Mock<ICircuitsValidator>();
            _collectorTypeSelectorMock = new Mock<ICollectorTypeSelector>();
            _markDirtyServiceMock = new Mock<IMarkDirtyService>();

            ConfigurePipeSpacingBacking();

            // Создаём реальные объекты для ClimateData и ConstructionData
            _climateData = new ClimateData();
            _constructionData = new ConstructionData();

            // Создаём общий контекст расчёта
            _calculationContext = new CalculationContext();
            _calculationContext.UpdateClimate(_climateData, "Climate");

            // Создаём реальные ViewModel с моками сервисов
            _thermalViewModel = new ThermalViewModel(
                _thermalCalculatorMock.Object,
                _climateData,
                _constructionData,
                _calculationStateServiceMock.Object,
                _calculationContext,
                new ThermalValidator(new ThermalCalculator(), _climateData, _constructionData),
                new ThermalResultValidator(),
                _markDirtyServiceMock.Object
            );

            _climateViewModel = new ClimateViewModel(
                _climateDataServiceMock.Object,
                _climateData,
                new ClimateValidator(),
                _markDirtyServiceMock.Object,
                _calculationContext
            );

            // Настраиваем моки для гликоля
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Настраиваем мок для калькулятора контуров
            SetupCircuitsCalculatorMocks();

            // Настраиваем мок для валидатора
            _validatorMock
                .Setup(v => v.CanRemoveCircuit(It.IsAny<CircuitRow>(), It.IsAny<CollectorData>()))
                .Returns((CircuitRow circuit, CollectorData collector) => collector != null && collector.Circuits.Count > 1);
            _validatorMock
                .Setup(v => v.CanRemoveCollector(It.IsAny<CollectorData>(), It.IsAny<int>()))
                .Returns((CollectorData collector, int count) => collector != null && count > 1);
            _validatorMock
                .Setup(v => v.ConfirmDeleteCircuit(It.IsAny<int>()))
                .Returns(true);
            _validatorMock
                .Setup(v => v.ConfirmDeleteCollector(It.IsAny<int>()))
                .Returns(true);

            // Настраиваем мок для селектора типа коллектора
            _collectorTypeSelectorMock
                .Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>()))
                .Returns(new CollectorSelectionResult
                {
                    CollectorType = "HKV-D (2-12 контуров)",
                    ValveType = ValveType.HKV_D,
                    Warning = null
                });

            var hydraulicsDependencies = HydraulicsTestDependencyFactory.Create(_calculationStateServiceMock.Object, _calculationContext);

            // Создаём ViewModel
            _viewModel = new CircuitsViewModel(
                _circuitsCalculatorMock.Object,
                _glycolServiceMock.Object,
                _calculationStateServiceMock.Object,
                _validatorMock.Object,
                _collectorTypeSelectorMock.Object,
                 _calculationContext,
                  hydraulicsDependencies.Coordinator,
                  hydraulicsDependencies.Session
            );

            // Создаём коллектор с контурами и устанавливаем как выбранный
            SetupCollectorWithCircuits();
        }

        /// <summary>
        /// Канонический шаг укладки как backed-свойство с событием
        /// (ICalculationStateService.PipeSpacing read-only, поэтому используется
        /// локальный бэкинг). GlycolAutoRecalculationTests исторически не
        /// настраивал PipeSpacing — переопределяет этот шаг пустым.
        /// </summary>
        protected virtual void ConfigurePipeSpacingBacking()
        {
            var pipeSpacingBacking = 200;
            _calculationStateServiceMock.SetupGet(s => s.PipeSpacing).Returns(() => pipeSpacingBacking);
            _calculationStateServiceMock
                .Setup(s => s.SetPipeSpacing(It.IsAny<int>(), It.IsAny<string>()))
                .Callback<int, string>((spacing, source) =>
                {
                    pipeSpacingBacking = spacing;
                    _calculationStateServiceMock.Raise(s => s.PipeSpacingChanged += null, _calculationStateServiceMock.Object, spacing);
                });
            _calculationStateServiceMock
                .Setup(s => s.SetPipeSpacing(It.IsAny<int>()))
                .Callback<int>(spacing =>
                {
                    pipeSpacingBacking = spacing;
                    _calculationStateServiceMock.Raise(s => s.PipeSpacingChanged += null, _calculationStateServiceMock.Object, spacing);
                });
        }

        /// <summary>
        /// Настраивает моки для всех методов калькулятора контуров
        /// </summary>
        protected void SetupCircuitsCalculatorMocks()
        {
            _circuitsCalculatorMock
                .Setup(c => c.CalculateCircuitPower(
                    It.IsAny<CircuitRow>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns((CircuitRow circuit, double q_up, double q_down, double spacing) => 1000.0);

            _circuitsCalculatorMock
                .Setup(c => c.CalculateFlowRate(
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns((double power, double deltaT, double density, double specificHeat) => 50.0);

            _circuitsCalculatorMock
                .Setup(c => c.CalculateCollectorSummary(
                    It.IsAny<List<CircuitRow>>(),
                    It.IsAny<int>(),
                    It.IsAny<ValveType>()))
                .Returns((List<CircuitRow> circuits, int number, ValveType valveType) => new CollectorSummary
                {
                    CollectorNumber = number,
                    CircuitCount = circuits.Count,
                    TotalPipeLength = circuits.Sum(c => c.CircuitLength),
                    TotalPower = circuits.Sum(c => c.Power),
                    TotalFlowRate = circuits.Sum(c => c.FlowRate)
                });

            _circuitsCalculatorMock
                .Setup(c => c.CalculateAtTemperature(
                    It.IsAny<CircuitRow>(),
                    It.IsAny<double>(),
                    It.IsAny<GlycolProperties>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<ValveType>()))
                .Returns((CircuitRow circuit, double temp, GlycolProperties props, double diameter, double kv, ValveType valveType) =>
                    new CircuitTemperatureResult
                    {
                        Temperature = temp,
                        Density = props.Density / 1000.0,
                        KinematicViscosity = props.KinematicViscosity,
                        ReynoldsNumber = 10000,
                        FrictionFactor = 0.02,
                        PressureLossPerMeter = 100,
                        DpRohr = 1000,
                        DpVerteiler = 500,
                        DpVent = 200,
                        ZuDrosseln = 0
                    });

            _circuitsCalculatorMock
                .Setup(c => c.CalculateBalancing(
                    It.IsAny<List<CircuitRow>>(),
                    It.IsAny<ValveType>()))
                .Returns((List<CircuitRow> circuits, ValveType valveType) => circuits);
        }

        /// <summary>
        /// Создаёт коллектор с контурами и устанавливает как выбранный
        /// </summary>
        protected void SetupCollectorWithCircuits()
        {
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1, CircuitLength = 100 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 2, CircuitLength = 80 });
            _viewModel.SelectedCollectorIndex = 0;
        }
    }
}
