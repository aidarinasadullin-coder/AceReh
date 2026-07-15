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
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.Core;

namespace SnowMeltingCalculator.Tests.IntegrationTests.Hydraulics
{
    /// <summary>
    /// Интеграционные тесты автопересчёта при изменении теплоносителя
    /// </summary>
    /// <remarks>
    /// Проверяет автоматический пересчёт при изменении параметров теплоносителя.
    /// Критические связи:
    /// - InputData.GlycolType изменяется и вызывает Calculate()
    /// - InputData.GlycolConcentration изменяется и вызывает Calculate()
    /// </remarks>
    [TestFixture]
    public class GlycolAutoRecalculationTests
    {
        private Mock<ICircuitsCalculator> _circuitsCalculatorMock = null!;
        private Mock<IGlycolDataService> _glycolServiceMock = null!;
        private Mock<IThermalCalculator> _thermalCalculatorMock = null!;
        private Mock<IClimateDataService> _climateDataServiceMock = null!;
        private Mock<ICalculationStateService> _calculationStateServiceMock = null!;
        private Mock<ICircuitsValidator> _circuitsValidatorMock = null!;
        private Mock<ICollectorTypeSelector> _collectorTypeSelectorMock = null!;
        private ClimateData _climateData = null!;
        private ConstructionData _constructionData = null!;
        private ThermalViewModel _thermalViewModel = null!;
        private ClimateViewModel _climateViewModel = null!;
        private CalculationContext _calculationContext = null!;
        private CircuitsViewModel _viewModel = null!;

        [SetUp]
        public void Setup()
        {
            _circuitsCalculatorMock = new Mock<ICircuitsCalculator>();
            _glycolServiceMock = new Mock<IGlycolDataService>();
            _thermalCalculatorMock = new Mock<IThermalCalculator>();
            _climateDataServiceMock = new Mock<IClimateDataService>();
            _calculationStateServiceMock = new Mock<ICalculationStateService>();
            _circuitsValidatorMock = new Mock<ICircuitsValidator>();
            _collectorTypeSelectorMock = new Mock<ICollectorTypeSelector>();

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
                new ThermalResultValidator()
            );

            _climateViewModel = new ClimateViewModel(
                _climateDataServiceMock.Object,
                _climateData,
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
            _circuitsValidatorMock
                .Setup(v => v.CanRemoveCircuit(It.IsAny<CircuitRow>(), It.IsAny<CollectorData?>()))
                .Returns((CircuitRow circuit, CollectorData? collector) =>
                    collector != null && collector.Circuits.Count > 1);

            _circuitsValidatorMock
                .Setup(v => v.CanRemoveCollector(It.IsAny<CollectorData>(), It.IsAny<int>()))
                .Returns((CollectorData collector, int count) => count > 1);

            _circuitsValidatorMock
                .Setup(v => v.ConfirmDeleteCircuit(It.IsAny<int>()))
                .Returns(true);

            _circuitsValidatorMock
                .Setup(v => v.ConfirmDeleteCollector(It.IsAny<int>()))
                .Returns(true);

            // Настраиваем мок для выбора типа коллектора
            _collectorTypeSelectorMock
                .Setup(s => s.SelectCollectorType(It.IsAny<CollectorData>()))
                .Returns((CollectorData collector) => new CollectorSelectionResult
                {
                    CollectorType = "HKV-D (2-12 контуров)",
                    ValveType = ValveType.HKV_D,
                    Warning = null
                });

            // Создаём ViewModel
            _viewModel = new CircuitsViewModel(
                _circuitsCalculatorMock.Object,
                _glycolServiceMock.Object,
                _calculationStateServiceMock.Object,
                _circuitsValidatorMock.Object,
                _collectorTypeSelectorMock.Object,
                _calculationContext
            );

            // Создаём коллектор с контурами и устанавливаем как выбранный
            SetupCollectorWithCircuits();
        }

        /// <summary>
        /// Настраивает моки для всех методов калькулятора контуров
        /// </summary>
        private void SetupCircuitsCalculatorMocks()
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
        private void SetupCollectorWithCircuits()
        {
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1, CircuitLength = 100 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 2, CircuitLength = 80 });
            _viewModel.SelectedCollectorIndex = 0;
        }

        #region OnGlycolTypeChanged Tests

        [Test]
        public void OnGlycolTypeChanged_TriggersCalculate()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act - изменяем тип гликоля через InputData
            _viewModel.InputData.GlycolType = GlycolType.Propylene;

            // Assert - GetProperties должен быть вызван для получения свойств гликоля
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    GlycolType.Propylene,
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.AtLeastOnce,
                "GetProperties должен быть вызван при изменении GlycolType");
        }

        [Test]
        public void OnGlycolTypeChanged_UpdatesInputData()
        {
            // Arrange
            var initialType = _viewModel.InputData.GlycolType;

            // Act
            _viewModel.InputData.GlycolType = GlycolType.Propylene;

            // Assert
            Assert.That(_viewModel.InputData.GlycolType, Is.EqualTo(GlycolType.Propylene),
                "InputData.GlycolType должен быть обновлён");
        }

        [Test]
        public void OnGlycolTypeChanged_FromEthyleneToPropylene_UpdatesCorrectly()
        {
            // Arrange
            _viewModel.InputData.GlycolType = GlycolType.Ethylene;
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act
            _viewModel.InputData.GlycolType = GlycolType.Propylene;

            // Assert
            Assert.That(_viewModel.InputData.GlycolType, Is.EqualTo(GlycolType.Propylene));
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    GlycolType.Propylene,
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.AtLeastOnce,
                "GetProperties должен быть вызван при изменении типа гликоля");
        }

        [Test]
        public void OnGlycolTypeChanged_FromPropyleneToEthylene_UpdatesCorrectly()
        {
            // Arrange
            _viewModel.InputData.GlycolType = GlycolType.Propylene;
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act
            _viewModel.InputData.GlycolType = GlycolType.Ethylene;

            // Assert
            Assert.That(_viewModel.InputData.GlycolType, Is.EqualTo(GlycolType.Ethylene));
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    GlycolType.Ethylene,
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.AtLeastOnce,
                "GetProperties должен быть вызван при изменении типа гликоля");
        }

        #endregion

        #region OnGlycolConcentrationChanged Tests

        [Test]
        public void OnGlycolConcentrationChanged_TriggersCalculate()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act - изменяем концентрацию гликоля через InputData
            _viewModel.InputData.GlycolConcentration = 40.0;

            // Assert - GetProperties должен быть вызван для получения свойств гликоля
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    40.0,
                    It.IsAny<double>()),
                Times.AtLeastOnce,
                "GetProperties должен быть вызван при изменении GlycolConcentration");
        }

        [Test]
        public void OnGlycolConcentrationChanged_UpdatesInputData()
        {
            // Arrange
            var initialConcentration = _viewModel.InputData.GlycolConcentration;

            // Act
            _viewModel.InputData.GlycolConcentration = 40.0;

            // Assert
            Assert.That(_viewModel.InputData.GlycolConcentration, Is.EqualTo(40.0),
                "InputData.GlycolConcentration должен быть обновлён");
        }

        [Test]
        public void OnGlycolConcentrationChanged_DifferentValues_UpdatesCorrectly()
        {
            // Arrange
            var concentrations = new[] { 10.0, 25.0, 50.0, 75.0, 90.0 };

            foreach (var concentration in concentrations)
            {
                // Act
                _viewModel.InputData.GlycolConcentration = concentration;

                // Assert
                Assert.That(_viewModel.InputData.GlycolConcentration, Is.EqualTo(concentration),
                    $"InputData.GlycolConcentration должен быть {concentration}");
            }
        }

        [Test]
        public void OnGlycolConcentrationChanged_SameValue_DoesNotTriggerDuplicateCalculate()
        {
            // Arrange
            _viewModel.InputData.GlycolConcentration = 50.0;
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act - устанавливаем то же значение
            _viewModel.InputData.GlycolConcentration = 50.0;

            // Assert - Calculate не должен быть вызван дополнительно
            // (только если PropertyChanged не вызывается при том же значении)
            // Примечание: CommunityToolkit.Mvvm не вызывает PropertyChanged при том же значении
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.AtMost(1),
                "Calculate не должен вызываться при установке того же значения");
        }

        #endregion

        #region Combined Changes Tests

        [Test]
        public void GlycolTypeAndConcentration_BothChanged_UpdatesInputData()
        {
            // Arrange
            _viewModel.InputData.GlycolType = GlycolType.Ethylene;
            _viewModel.InputData.GlycolConcentration = 50.0;

            // Act
            _viewModel.InputData.GlycolType = GlycolType.Propylene;
            _viewModel.InputData.GlycolConcentration = 40.0;

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_viewModel.InputData.GlycolType, Is.EqualTo(GlycolType.Propylene),
                    "GlycolType должен быть Propylene");
                Assert.That(_viewModel.InputData.GlycolConcentration, Is.EqualTo(40.0),
                    "GlycolConcentration должен быть 40.0");
            });
        }

        [Test]
        public void GlycolProperties_UsedInCalculation()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1040,
                    SpecificHeat = 3700,
                    KinematicViscosity = 0.000004
                });

            // Act
            _viewModel.InputData.GlycolType = GlycolType.Propylene;
            _viewModel.InputData.GlycolConcentration = 40.0;

            // Assert - сервис гликоля должен быть вызван для получения свойств
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    GlycolType.Propylene,
                    40.0,
                    It.IsAny<double>()),
                Times.AtLeastOnce,
                "GetProperties должен быть вызван с правильными параметрами");
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public void GlycolConcentration_MinimumValue_UpdatesCorrectly()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act
            _viewModel.InputData.GlycolConcentration = 10.0; // Минимальная концентрация

            // Assert
            Assert.That(_viewModel.InputData.GlycolConcentration, Is.EqualTo(10.0));
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    10.0,
                    It.IsAny<double>()),
                Times.AtLeastOnce);
        }

        [Test]
        public void GlycolConcentration_MaximumValue_UpdatesCorrectly()
        {
            // Arrange
            _glycolServiceMock.Reset();
            _glycolServiceMock
                .Setup(g => g.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1050,
                    SpecificHeat = 3800,
                    KinematicViscosity = 0.000005
                });

            // Act
            _viewModel.InputData.GlycolConcentration = 90.0; // Максимальная концентрация

            // Assert
            Assert.That(_viewModel.InputData.GlycolConcentration, Is.EqualTo(90.0));
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    90.0,
                    It.IsAny<double>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region PropertyChanged Tests

        [Test]
        public void GlycolType_PropertyChanged_IsRaised()
        {
            // Arrange
            var eventRaised = false;
            _viewModel.InputData.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HydraulicInputData.GlycolType))
                {
                    eventRaised = true;
                }
            };

            // Act
            _viewModel.InputData.GlycolType = GlycolType.Propylene;

            // Assert
            Assert.That(eventRaised, Is.True, "PropertyChanged для GlycolType должен быть вызван");
        }

        [Test]
        public void GlycolConcentration_PropertyChanged_IsRaised()
        {
            // Arrange
            var eventRaised = false;
            _viewModel.InputData.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HydraulicInputData.GlycolConcentration))
                {
                    eventRaised = true;
                }
            };

            // Act
            _viewModel.InputData.GlycolConcentration = 40.0;

            // Assert
            Assert.That(eventRaised, Is.True, "PropertyChanged для GlycolConcentration должен быть вызван");
        }

        #endregion
    }
}