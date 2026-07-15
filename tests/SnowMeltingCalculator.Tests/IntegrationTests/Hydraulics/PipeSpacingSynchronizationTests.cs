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
    /// Интеграционные тесты синхронизации шага укладки трубы
    /// </summary>
    /// <remarks>
    /// Проверяет синхронизацию шага укладки между ThermalViewModel и CircuitsViewModel.
    /// Критические связи:
    /// - PipeSpacing_cm в CircuitRow обновляется при изменении ThermalViewModel.PipeSpacing
    /// - Все контуры во всех коллекторах обновляются
    /// </remarks>
    [TestFixture]
    public class PipeSpacingSynchronizationTests
    {
        private Mock<ICircuitsCalculator> _circuitsCalculatorMock = null!;
        private Mock<IGlycolDataService> _glycolServiceMock = null!;
        private Mock<IThermalCalculator> _thermalCalculatorMock = null!;
        private Mock<IClimateDataService> _climateDataServiceMock = null!;
        private Mock<ICalculationStateService> _calculationStateServiceMock = null!;
        private Mock<ICircuitsValidator> _validatorMock = null!;
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
            _validatorMock = new Mock<ICircuitsValidator>();
            _collectorTypeSelectorMock = new Mock<ICollectorTypeSelector>();

            // Настраиваем канонический шаг укладки как backed-свойство с событием
            // (ICalculationStateService.PipeSpacing read-only, поэтому используем локальный бэкинг)
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
                new ClimateValidator(),
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

            // Создаём ViewModel
            _viewModel = new CircuitsViewModel(
                _circuitsCalculatorMock.Object,
                _glycolServiceMock.Object,
                _calculationStateServiceMock.Object,
                _validatorMock.Object,
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

        #region PipeSpacing_cm Property Tests

        [Test]
        public void PipeSpacing_cm_ReturnsCorrectValue()
        {
            // Arrange
            _thermalViewModel.PipeSpacing = 200; // мм

            // Act
            var pipeSpacing_cm = _viewModel.PipeSpacing_cm;

            // Assert
            Assert.That(pipeSpacing_cm, Is.EqualTo(20.0),
                "PipeSpacing_cm должен быть равен PipeSpacing / 10");
        }

        [Test]
        public void PipeSpacing_cm_DifferentValues_ReturnsCorrectValue()
        {
            // Arrange
            var testCases = new[]
            {
                (PipeSpacing_mm: 150, Expected_cm: 15.0),
                (PipeSpacing_mm: 200, Expected_cm: 20.0),
                (PipeSpacing_mm: 250, Expected_cm: 25.0),
                (PipeSpacing_mm: 300, Expected_cm: 30.0)
            };

            foreach (var (pipeSpacing_mm, expected_cm) in testCases)
            {
                // Act
                _thermalViewModel.PipeSpacing = pipeSpacing_mm;
                var pipeSpacing_cm = _viewModel.PipeSpacing_cm;

                // Assert
                Assert.That(pipeSpacing_cm, Is.EqualTo(expected_cm),
                    $"PipeSpacing_cm должен быть {expected_cm} при PipeSpacing = {pipeSpacing_mm}");
            }
        }

        #endregion

        #region UpdatePipeSpacingInCircuits Tests

        [Test]
        public void OnThermalViewModelPropertyChanged_WhenPipeSpacingChanged_UpdatesCircuits()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1, CircuitLength = 100 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 2, CircuitLength = 80 });

            // Устанавливаем начальный шаг укладки
            _thermalViewModel.PipeSpacing = 200;

            // Act - изменяем шаг укладки
            _thermalViewModel.PipeSpacing = 250;

            // Assert - все контуры должны иметь обновлённый шаг укладки
            foreach (var circuit in collector.Circuits)
            {
                Assert.That(circuit.PipeSpacing_cm, Is.EqualTo(25.0),
                    "PipeSpacing_cm должен быть обновлён во всех контурах");
            }
        }

        [Test]
        public void UpdatePipeSpacingInCircuits_UpdatesAllCircuits()
        {
            // Arrange
            // Добавляем второй коллектор
            _viewModel.Collectors.Add(new CollectorData(2));

            var collector1 = _viewModel.Collectors[0];
            var collector2 = _viewModel.Collectors[1];

            collector1.Circuits.Clear();
            collector1.Circuits.Add(new CircuitRow { CircuitNumber = 1, CircuitLength = 100 });
            collector1.Circuits.Add(new CircuitRow { CircuitNumber = 2, CircuitLength = 80 });

            collector2.Circuits.Clear();
            collector2.Circuits.Add(new CircuitRow { CircuitNumber = 1, CircuitLength = 120 });

            // Устанавливаем начальный шаг укладки
            _thermalViewModel.PipeSpacing = 200;

            // Act - изменяем шаг укладки
            _thermalViewModel.PipeSpacing = 300;

            // Assert - все контуры во всех коллекторах должны быть обновлены
            foreach (var circuit in collector1.Circuits)
            {
                Assert.That(circuit.PipeSpacing_cm, Is.EqualTo(30.0),
                    "PipeSpacing_cm в коллекторе 1 должен быть обновлён");
            }

            foreach (var circuit in collector2.Circuits)
            {
                Assert.That(circuit.PipeSpacing_cm, Is.EqualTo(30.0),
                    "PipeSpacing_cm в коллекторе 2 должен быть обновлён");
            }
        }

        [Test]
        public void UpdatePipeSpacingInCircuits_WithMultipleCollectors_UpdatesAll()
        {
            // Arrange
            _viewModel.Collectors.Clear();
            _viewModel.Collectors.Add(new CollectorData(1));
            _viewModel.Collectors.Add(new CollectorData(2));
            _viewModel.Collectors.Add(new CollectorData(3));

            foreach (var collector in _viewModel.Collectors)
            {
                collector.Circuits.Clear();
                collector.Circuits.Add(new CircuitRow { CircuitNumber = 1, CircuitLength = 100 });
                collector.Circuits.Add(new CircuitRow { CircuitNumber = 2, CircuitLength = 80 });
            }

            _thermalViewModel.PipeSpacing = 200;

            // Act
            _thermalViewModel.PipeSpacing = 150;

            // Assert
            foreach (var collector in _viewModel.Collectors)
            {
                foreach (var circuit in collector.Circuits)
                {
                    Assert.That(circuit.PipeSpacing_cm, Is.EqualTo(15.0),
                        $"PipeSpacing_cm должен быть 15.0 в коллекторе {collector.CollectorNumber}");
                }
            }
        }

        #endregion

        #region PropertyChanged Event Tests

        [Test]
        public void ThermalViewModel_PipeSpacingChanged_RaisesPropertyChanged()
        {
            // Arrange
            var eventRaised = false;
            _thermalViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ThermalViewModel.PipeSpacing))
                {
                    eventRaised = true;
                }
            };

            // Act
            _thermalViewModel.PipeSpacing = 250;

            // Assert
            Assert.That(eventRaised, Is.True,
                "PropertyChanged для PipeSpacing должен быть вызван");
        }

        [Test]
        public void PipeSpacing_SameValue_DoesNotRaisePropertyChanged()
        {
            // Arrange
            _thermalViewModel.PipeSpacing = 200;
            var eventCount = 0;
            _thermalViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ThermalViewModel.PipeSpacing))
                {
                    eventCount++;
                }
            };

            // Act - устанавливаем то же значение
            _thermalViewModel.PipeSpacing = 200;

            // Assert - событие не должно быть вызвано (CommunityToolkit.Mvvm оптимизация)
            Assert.That(eventCount, Is.EqualTo(0),
                "PropertyChanged не должен вызываться при установке того же значения");
        }

        #endregion

        #region Integration with Calculation Tests

        [Test]
        public void PipeSpacingChange_TriggersRecalculation()
        {
            // Arrange
            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };

            _thermalViewModel.Result = thermalResult;

            // Act
            _thermalViewModel.PipeSpacing = 250;

            // Assert - GetProperties должен быть вызван (дважды: для рабочей и расчётной температуры)
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.AtLeast(2),
                "GetProperties должен быть вызван при изменении PipeSpacing");
        }

        [Test]
        public void PipeSpacing_UsedInCalculation()
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

            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };

            _thermalViewModel.Result = thermalResult;
            _thermalViewModel.PipeSpacing = 250;

            // Act
            _thermalViewModel.PipeSpacing = 300;

            // Assert - GetProperties должен быть вызван (дважды: для рабочей и расчётной температуры)
            // Это косвенно проверяет, что Calculate() вызывается при изменении PipeSpacing
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.AtLeast(2),
                "GetProperties должен быть вызван при изменении PipeSpacing");
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public void PipeSpacing_MinimumValue_UpdatesCorrectly()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1, CircuitLength = 100 });

            // Act
            _thermalViewModel.PipeSpacing = 100; // Минимальное значение

            // Assert
            Assert.That(_viewModel.PipeSpacing_cm, Is.EqualTo(10.0));
            foreach (var circuit in collector.Circuits)
            {
                Assert.That(circuit.PipeSpacing_cm, Is.EqualTo(10.0));
            }
        }

        [Test]
        public void PipeSpacing_MaximumValue_UpdatesCorrectly()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1, CircuitLength = 100 });

            // Act
            _thermalViewModel.PipeSpacing = 500; // Максимальное значение

            // Assert
            Assert.That(_viewModel.PipeSpacing_cm, Is.EqualTo(50.0));
            foreach (var circuit in collector.Circuits)
            {
                Assert.That(circuit.PipeSpacing_cm, Is.EqualTo(50.0));
            }
        }

        [Test]
        public void PipeSpacing_NoCircuits_DoesNotThrow()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();

            // Act & Assert - не должно выбросить исключение
            Assert.DoesNotThrow(() => _thermalViewModel.PipeSpacing = 250);
        }

        #endregion
    }
}