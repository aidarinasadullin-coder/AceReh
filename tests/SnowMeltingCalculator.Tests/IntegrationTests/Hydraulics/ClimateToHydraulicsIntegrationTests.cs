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

namespace SnowMeltingCalculator.Tests.IntegrationTests.Hydraulics
{
    /// <summary>
    /// Интеграционные тесты связи Climate → Hydraulics
    /// </summary>
    /// <remarks>
    /// Проверяет корректность передачи климатических данных в CircuitsViewModel.
    /// Критические связи:
    /// - UpdateFromClimateModule() вызывается при изменении AirTemperature
    /// - AirTemperature обновляется
    /// - DesignTemperatureValue отражает расчётную температуру
    /// - Calculate() вызывается при изменении климатических данных
    /// </remarks>
    [TestFixture]
    public class ClimateToHydraulicsIntegrationTests
    {
        private Mock<ICircuitsCalculator> _circuitsCalculatorMock = null!;
        private Mock<IGlycolDataService> _glycolServiceMock = null!;
        private Mock<IThermalCalculator> _thermalCalculatorMock = null!;
        private Mock<IClimateDataService> _climateDataServiceMock = null!;
        private Mock<ICalculationStateService> _calculationStateServiceMock = null!;
        private Mock<ICircuitsValidator> _validatorMock = null!;
        private Mock<ICollectorTypeSelector> _collectorTypeSelectorMock = null!;
        private Mock<IMarkDirtyService> _markDirtyServiceMock = null!;
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
            _markDirtyServiceMock = new Mock<IMarkDirtyService>();

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

            // Создаём ViewModel
            _viewModel = new CircuitsViewModel(
                _circuitsCalculatorMock.Object,
                _glycolServiceMock.Object,
                _calculationStateServiceMock.Object,
                _validatorMock.Object,
                _collectorTypeSelectorMock.Object,
                _calculationContext,
                _markDirtyServiceMock.Object
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

        /// <summary>
        /// Публикует результат теплового расчёта в единый контекст (T15 contract)
        /// </summary>
        private void PushThermalResultToContext(ThermalCalculationResult result, PipeType? pipe = null)
        {
            _thermalViewModel.Result = result;
            var inputs = _thermalViewModel.BuildThermalInputs();
            if (pipe != null)
            {
                inputs = inputs with { Pipe = pipe };
            }
            _calculationContext.UpdateThermalInputs(inputs, "Thermal");
            _calculationContext.UpdateThermal(result, "Thermal");
        }

        #region UpdateFromClimateModule Tests

        [Test]
        public void OnClimatePropertyChanged_WhenAirTemperatureChanged_UpdatesDesignTemperature()
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

            // Act - изменяем температуру воздуха
            _climateViewModel.AirTemperature = -28.0;

            // Assert - DesignTemperatureValue должен быть обновлён
            Assert.That(_viewModel.DesignTemperatureValue, Is.EqualTo(-28.0),
                "DesignTemperatureValue должен быть обновлён из AirTemperature");
        }

        [Test]
        public void OnClimatePropertyChanged_WhenAirTemperatureChanged_TriggersCalculate()
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

            // Act - изменяем температуру воздуха
            _climateViewModel.AirTemperature = -25.0;

            // Assert - GetProperties должен быть вызван
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.AtLeastOnce,
                "GetProperties должен быть вызван при изменении AirTemperature");
        }

        [Test]
        public void UpdateFromClimateModule_UpdatesDesignTemperatureFromAirTemperature()
        {
            // Arrange
            var testTemperatures = new[] { -10.0, -15.0, -20.0, -28.0, -35.0 };

            foreach (var temp in testTemperatures)
            {
                // Act
                _climateViewModel.AirTemperature = temp;

                // Assert
                Assert.That(_viewModel.DesignTemperatureValue, Is.EqualTo(temp),
                    $"DesignTemperatureValue должен быть {temp}");
            }
        }

        [Test]
        public void OnClimatePropertyChanged_WhenHumidityChanged_TriggersCalculate()
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

            // Act - изменяем климатическое свойство; в T15 любое изменение климата
            // проходит через CalculationContext и вызывает пересчёт
            _climateViewModel.Humidity = 80.0;

            // Assert - Calculate должен быть вызван
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.AtLeastOnce,
                "GetProperties должен вызываться при изменении Humidity");
        }

        #endregion

        #region PropertyChanged Event Tests

        [Test]
        public void ClimateViewModel_PropertyChanged_SubscribedCorrectly()
        {
            // Arrange
            var airTemperatureChanged = false;
            _climateViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ClimateViewModel.AirTemperature))
                {
                    airTemperatureChanged = true;
                }
            };

            // Act
            _climateViewModel.AirTemperature = -30.0;

            // Assert
            Assert.That(airTemperatureChanged, Is.True,
                "Событие PropertyChanged для AirTemperature должно быть вызвано");
        }

        [Test]
        public void OnClimatePropertyChanged_WhenAirTemperatureChanged_RaisesPropertyChanged()
        {
            // Arrange
            var eventRaised = false;
            _climateViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ClimateViewModel.AirTemperature))
                {
                    eventRaised = true;
                }
            };

            // Act
            _climateViewModel.AirTemperature = -22.0;

            // Assert
            Assert.That(eventRaised, Is.True, "PropertyChanged событие должно быть вызвано");
        }

        #endregion

        #region Integration with Thermal Tests

        [Test]
        public void ClimateAndThermalChanges_BothUpdateInputData()
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

            // Act - сначала Thermal через контекст (T15 contract)
            PushThermalResultToContext(thermalResult);

            // Assert - Thermal изменения должны быть отражены
            Assert.That(_viewModel.PowerUp, Is.EqualTo(256.0), "PowerUp из Thermal");
            Assert.That(_viewModel.SupplyTemperature, Is.EqualTo(50.0), "SupplyTemperature из Thermal");

            // Act - теперь Climate
            _climateViewModel.AirTemperature = -28.0;

            // Assert - Climate изменения должны быть отражены
            Assert.That(_climateData.ColdFiveDayTemperature, Is.EqualTo(-28.0), "ColdFiveDayTemperature из Climate");
        }

        [Test]
        public void ClimateChange_AfterThermalChange_Recalculates()
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

            // Act - сначала Thermal
            _thermalViewModel.Result = thermalResult;
            var callCountAfterThermal = _glycolServiceMock.Invocations.Count;

            // Потом Climate
            _climateViewModel.AirTemperature = -28.0;

            // Assert - Calculate должен быть вызван дважды
            Assert.That(_glycolServiceMock.Invocations.Count, Is.GreaterThan(callCountAfterThermal),
                "Calculate должен быть вызван после изменения Climate");
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public void AirTemperature_ExtremeValues_UpdatesCorrectly()
        {
            // Arrange
            var extremeTemperatures = new[] { -50.0, -40.0, 0.0, 10.0 };

            foreach (var temp in extremeTemperatures)
            {
                // Act
                _climateViewModel.AirTemperature = temp;

                // Assert
                Assert.That(_viewModel.DesignTemperatureValue, Is.EqualTo(temp),
                    $"DesignTemperatureValue должен быть {temp} при AirTemperature = {temp}");
            }
        }

        [Test]
        public void AirTemperature_SameValue_DoesNotTriggerDuplicateCalculate()
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

            _climateViewModel.AirTemperature = -20.0;
            var callCountAfterFirst = _glycolServiceMock.Invocations.Count;

            // Act - устанавливаем то же значение
            _climateViewModel.AirTemperature = -20.0;

            // Assert - Calculate не должен быть вызван дополнительно
            Assert.That(_glycolServiceMock.Invocations.Count, Is.EqualTo(callCountAfterFirst),
                "Calculate не должен вызываться при установке того же значения");
        }

        #endregion

        #region Design Temperature Tests

        [Test]
        public void DesignTemperatureValue_ReturnsCorrectValue()
        {
            // Arrange
            _climateViewModel.AirTemperature = -28.0;

            // Act
            var designTemp = _viewModel.DesignTemperatureValue;

            // Assert
            Assert.That(designTemp, Is.EqualTo(-28.0),
                "DesignTemperatureValue должен возвращать AirTemperature");
        }

        [Test]
        public void DesignTemperature_UpdatesWhenClimateChanges()
        {
            // Arrange
            _climateViewModel.AirTemperature = -15.0;
            var initialDesignTemp = _viewModel.DesignTemperatureValue;

            // Act
            _climateViewModel.AirTemperature = -30.0;

            // Assert
            Assert.That(_viewModel.DesignTemperatureValue, Is.Not.EqualTo(initialDesignTemp),
                "DesignTemperatureValue должен измениться при изменении AirTemperature");
            Assert.That(_viewModel.DesignTemperatureValue, Is.EqualTo(-30.0),
                "DesignTemperatureValue должен быть равен новой AirTemperature");
        }

        public static IEnumerable<TestCaseData> CityScenarios()
        {
            yield return new TestCaseData("Сочи", -5.0, -10.0, false)
                .SetName("Сочи_T5Days-5_ZoneM10");
            yield return new TestCaseData("Москва", -23.0, -10.0, false)
                .SetName("Москва_T5Days-23_ZoneM10");
            yield return new TestCaseData("Условный", -30.0, -15.0, false)
                .SetName("Условный_T5Days-30_ZoneM15");
            yield return new TestCaseData("Норильск", -42.0, -20.0, false)
                .SetName("Норильск_T5Days-42_ZoneM20");
            yield return new TestCaseData("Сочи", -23.0, -20.0, true)
                .SetName("Сочи_HighRequirements_ZoneM20Plus");
        }

        [TestCaseSource(nameof(CityScenarios))]
        public void DesignTemperatureValue_FollowsAirTemperature_WithCitySelected(
            string cityName,
            double t5Days092,
            double expectedAirTemperature,
            bool isHighRequirements)
        {
            // Arrange
            var city = new CityInfo
            {
                Name = cityName,
                Region = "Тестовый регион",
                T5Days092 = t5Days092,
                WindAvgTempLe8 = 3.0,
                Humidity15hCold = 80.0
            };

            // Act
            _climateViewModel.IsHighRequirements = isHighRequirements;
            _climateViewModel.SelectedCity = city;

            // Assert
            Assert.That(_climateData.AirTemperature, Is.EqualTo(expectedAirTemperature),
                $"AirTemperature должна быть {expectedAirTemperature}°C по таблице 1.6 для {cityName}");
            Assert.That(_viewModel.DesignTemperatureValue, Is.EqualTo(expectedAirTemperature),
                $"DesignTemperatureValue должен следовать AirTemperature ({expectedAirTemperature}°C), а не T5Days092");
            Assert.That(_climateData.ColdFiveDayTemperature, Is.EqualTo(t5Days092),
                "ColdFiveDayTemperature должна сохранять исходное значение T5Days092");
        }

        #endregion
    }
}