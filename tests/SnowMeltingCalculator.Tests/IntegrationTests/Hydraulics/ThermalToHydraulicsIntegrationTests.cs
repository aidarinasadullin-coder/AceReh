using System.Collections.ObjectModel;
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
    /// Интеграционные тесты связи Thermal → Hydraulics
    /// </summary>
    /// <remarks>
    /// Проверяет корректность передачи данных из ThermalViewModel в CircuitsViewModel.
    /// Критические связи:
    /// - UpdateFromThermalModule() вызывается при изменении Result
    /// - ViewModel.PowerUp, PowerDown обновляются
    /// - ViewModel.SupplyTemperature, ReturnTemperature обновляются
    /// - ViewModel.InnerDiameter обновляется при изменении SelectedPipe
    /// </remarks>
    [TestFixture]
    public class ThermalToHydraulicsIntegrationTests
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
        /// Помещает данные о выбранной трубе в контекст, чтобы UpdateFromThermalModule
        /// мог определить InnerDiameter (T15 contract)
        /// </summary>
        private void SetThermalInputsInContext(PipeType? pipe = null)
        {
            var inputs = _thermalViewModel.BuildThermalInputs();
            if (pipe != null)
            {
                inputs = inputs with { Pipe = pipe };
            }
            _calculationContext.UpdateThermalInputs(inputs, "Thermal");
        }

        #region UpdateFromThermalModule Tests

        [Test]
        public void UpdateFromThermalModule_WhenResultChanges_UpdatesInputData()
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

            // Act - напрямую через публичный метод UpdateFromThermalModule (T15 contract)
            _viewModel.UpdateFromThermalModule(thermalResult, null);

            // Assert - проверяем, что InputData обновился
            Assert.That(_viewModel.PowerUp, Is.EqualTo(256.0), "PowerUp должен быть обновлён");
            Assert.That(_viewModel.PowerDown, Is.EqualTo(5.0), "PowerDown должен быть обновлён");
            Assert.That(_viewModel.SupplyTemperature, Is.EqualTo(50.0), "SupplyTemperature должен быть обновлён");
            Assert.That(_viewModel.ReturnTemperature, Is.EqualTo(30.0), "ReturnTemperature должен быть обновлён");
        }

        [Test]
        public void UpdateFromThermalModule_WhenResultIsNull_ResetsInputData()
        {
            // Arrange - сначала устанавливаем валидный результат
            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };
            _viewModel.UpdateFromThermalModule(thermalResult, null);

            // Act - устанавливаем null
            _viewModel.UpdateFromThermalModule(null, null);

            // Assert - InputData сброшен, свойства возвращают fallback-значения
            Assert.That(_viewModel.PowerUp, Is.EqualTo(180.0), "PowerUp fallback");
            Assert.That(_viewModel.PowerDown, Is.EqualTo(80.0), "PowerDown fallback");
            Assert.That(_viewModel.SupplyTemperature, Is.EqualTo(50.0), "SupplyTemperature fallback");
            Assert.That(_viewModel.ReturnTemperature, Is.EqualTo(30.0), "ReturnTemperature fallback");
        }

        [Test]
        public void UpdateFromThermalModule_WhenResultInvalid_ResetsInputData()
        {
            // Arrange - сначала устанавливаем валидный результат
            var validResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };
            _viewModel.UpdateFromThermalModule(validResult, null);

            // Act - устанавливаем невалидный результат
            var invalidResult = new ThermalCalculationResult
            {
                PowerUp = 100.0,
                PowerDown = 10.0,
                SupplyTemperature = 60.0,
                ReturnTemperature = 40.0,
                IsValid = false,
                ValidationErrors = new[] { "Ошибка валидации" }
            };
            _viewModel.UpdateFromThermalModule(invalidResult, null);

            // Assert - InputData сброшен, свойства возвращают fallback-значения
            Assert.That(_viewModel.PowerUp, Is.EqualTo(180.0), "PowerUp fallback при невалидном результате");
        }

        [Test]
        public void UpdateFromThermalModule_WhenSelectedPipeChanges_UpdatesInnerDiameter()
        {
            // Arrange
            var pipe = new PipeType
            {
                Name = "RAUTHERM S 20x2,0",
                OuterDiameter = 20,
                InnerDiameter = 16,
                WallThickness = 2.0
            };

            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };

            // Act - помещаем параметры трубы в контекст и вызываем UpdateFromThermalModule
            SetThermalInputsInContext(pipe);
            _viewModel.UpdateFromThermalModule(thermalResult, pipe);

            // Assert - InnerDiameter должен быть обновлён
            Assert.That(_viewModel.InnerDiameter, Is.EqualTo(16.0), "InnerDiameter должен быть обновлён из SelectedPipe");
        }

        [Test]
        public void UpdateFromThermalModule_WhenPipeIsNull_DoesNotUpdateInnerDiameter()
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

            // Устанавливаем начальный диаметр через ThermalInputs в контексте
            var seedPipe = new PipeType
            {
                Name = "Seed",
                OuterDiameter = 13.0,
                InnerDiameter = 13.0,
                WallThickness = 0
            };
            SetThermalInputsInContext(seedPipe);
            _viewModel.UpdateFromThermalModule(new ThermalCalculationResult { IsValid = true }, seedPipe);

            // Act - устанавливаем результат без трубы
            _viewModel.UpdateFromThermalModule(thermalResult, null);

            // Assert - InnerDiameter должен остаться прежним
            Assert.That(_viewModel.InnerDiameter, Is.EqualTo(13.0), "InnerDiameter не должен измениться при null трубе");
        }

        #endregion

        #region PropertyChanged Event Tests

        [Test]
        public void OnThermalViewModelPropertyChanged_WhenResultChanged_TriggersUpdate()
        {
            // Arrange
            var eventRaised = false;
            _thermalViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ThermalViewModel.Result))
                {
                    eventRaised = true;
                }
            };

            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };

            // Act
            _thermalViewModel.Result = thermalResult;

            // Assert
            Assert.That(eventRaised, Is.True, "Событие PropertyChanged для Result должно быть вызвано");
        }

        [Test]
        public void ThermalViewModel_PropertyChanged_SubscribedCorrectly()
        {
            // Arrange
            var resultChanged = false;
            _thermalViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ThermalViewModel.Result))
                {
                    resultChanged = true;
                }
            };

            // Act
            _thermalViewModel.Result = new ThermalCalculationResult { IsValid = true };

            // Assert
            Assert.That(resultChanged, Is.True, "Подписка на PropertyChanged должна работать");
        }

        #endregion

        #region Context Change Tests

        [Test]
        public void ThermalResultChangedViaContext_NotifiesThermalPropertiesAndRecalculates()
        {
            // Arrange - seed ThermalInputs (pipe + spacing) before subscribing to events
            var pipe = new PipeType
            {
                Name = "RAUTHERM S 20x2,0",
                OuterDiameter = 20,
                InnerDiameter = 16,
                WallThickness = 2.0
            };
            var inputs = new ThermalInputs { Pipe = pipe, PipeSpacing = 200 };
            _calculationContext.UpdateThermalInputs(inputs, "Thermal");

            var changedProperties = new List<string>();
            _viewModel.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

            var result = new ThermalCalculationResult
            {
                PowerUp = 300,
                PowerDown = 20,
                SupplyTemperature = 55,
                ReturnTemperature = 40,
                MeanTemperature = 47.5,
                DeltaT = 15,
                IsValid = true
            };

            // Act - publish thermal result via context only
            _calculationContext.UpdateThermal(result, "Thermal");

            // Assert - UI block properties were notified
            Assert.That(changedProperties, Contains.Item(nameof(CircuitsViewModel.PowerUp)), "PowerUp должен уведомлять UI");
            Assert.That(changedProperties, Contains.Item(nameof(CircuitsViewModel.SupplyTemperature)), "SupplyTemperature должен уведомлять UI");
            Assert.That(changedProperties, Contains.Item(nameof(CircuitsViewModel.PipeType)), "PipeType должен уведомлять UI");
            Assert.That(changedProperties, Contains.Item(nameof(CircuitsViewModel.PipeSpacing_cm)), "PipeSpacing_cm должен уведомлять UI");

            // Assert - calculation ran for the selected collector's circuits
            Assert.That(
                _viewModel.SelectedCollector!.Circuits.Any(c => c.Power != 0),
                Is.True,
                "Хотя бы один контур выбранного коллектора должен получить ненулевую мощность");
        }

        [Test]
        public void ThermalInputsChangedViaContext_NotifiesThermalProperties()
        {
            // Arrange
            var changedProperties = new List<string>();
            _viewModel.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

            var pipe = new PipeType
            {
                Name = "RAUTHERM S 25x2,3",
                OuterDiameter = 25,
                InnerDiameter = 20.4,
                WallThickness = 2.3
            };
            var inputs = new ThermalInputs { Pipe = pipe, PipeSpacing = 250 };

            // Act - publish thermal inputs via context only
            _calculationContext.UpdateThermalInputs(inputs, "Thermal");

            // Assert
            Assert.That(changedProperties, Contains.Item(nameof(CircuitsViewModel.PipeType)), "PipeType должен уведомлять UI");
            Assert.That(changedProperties, Contains.Item(nameof(CircuitsViewModel.PipeSpacing_cm)), "PipeSpacing_cm должен уведомлять UI");
            Assert.That(changedProperties, Contains.Item(nameof(CircuitsViewModel.InnerDiameter)), "InnerDiameter должен уведомлять UI");
        }

        #endregion

        #region Calculate Call Tests

        [Test]
        public void UpdateFromThermalModule_TriggersCalculate()
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

            // Act - напрямую через публичный метод UpdateFromThermalModule (T15 contract)
            _viewModel.UpdateFromThermalModule(thermalResult, null);

            // Assert - GetProperties должен быть вызван (дважды: для рабочей и расчётной температуры)
            _glycolServiceMock.Verify(
                g => g.GetProperties(
                    It.IsAny<GlycolType>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.AtLeastOnce,
                "GetProperties должен быть вызван при изменении Result");
        }

        #endregion

        #region Multiple Property Updates Tests

        [Test]
        public void MultiplePropertyChanges_UpdatesAllInputDataFields()
        {
            // Arrange
            var pipe = new PipeType
            {
                Name = "RAUTHERM S 25x2,3",
                OuterDiameter = 25,
                InnerDiameter = 20.4,
                WallThickness = 2.3
            };

            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 300.0,
                PowerDown = 10.0,
                SupplyTemperature = 60.0,
                ReturnTemperature = 40.0,
                IsValid = true
            };

            // Act - параметры трубы в контекст, результат через UpdateFromThermalModule
            SetThermalInputsInContext(pipe);
            _viewModel.UpdateFromThermalModule(thermalResult, pipe);

            // Assert - все поля должны быть обновлены
            Assert.Multiple(() =>
            {
                Assert.That(_viewModel.PowerUp, Is.EqualTo(300.0), "PowerUp");
                Assert.That(_viewModel.PowerDown, Is.EqualTo(10.0), "PowerDown");
                Assert.That(_viewModel.SupplyTemperature, Is.EqualTo(60.0), "SupplyTemperature");
                Assert.That(_viewModel.ReturnTemperature, Is.EqualTo(40.0), "ReturnTemperature");
                Assert.That(_viewModel.InnerDiameter, Is.EqualTo(20.4), "InnerDiameter");
            });
        }

        [Test]
        public void UpdateFromThermalModule_PreservesGlycolSettings()
        {
            // Arrange
            _viewModel.InputData.GlycolType = GlycolType.Propylene;
            _viewModel.InputData.GlycolConcentration = 40.0;

            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = 5.0,
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };

            // Act
            _thermalViewModel.Result = thermalResult;

            // Assert - настройки гликоля должны сохраниться
            Assert.That(_viewModel.InputData.GlycolType, Is.EqualTo(GlycolType.Propylene), "GlycolType должен сохраниться");
            Assert.That(_viewModel.InputData.GlycolConcentration, Is.EqualTo(40.0), "GlycolConcentration должен сохраниться");
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public void UpdateFromThermalModule_WithZeroValues_UpdatesInputData()
        {
            // Arrange
            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 0.0,
                PowerDown = 0.0,
                SupplyTemperature = 0.0,
                ReturnTemperature = 0.0,
                IsValid = true
            };

            // Act
            _viewModel.UpdateFromThermalModule(thermalResult, null);

            // Assert - нулевые значения должны быть переданы
            Assert.That(_viewModel.PowerUp, Is.EqualTo(0.0));
            Assert.That(_viewModel.PowerDown, Is.EqualTo(0.0));
        }

        [Test]
        public void UpdateFromThermalModule_WithNegativePowerDown_UpdatesInputData()
        {
            // Arrange
            var thermalResult = new ThermalCalculationResult
            {
                PowerUp = 256.0,
                PowerDown = -5.0, // Отрицательное значение (потери вниз)
                SupplyTemperature = 50.0,
                ReturnTemperature = 30.0,
                IsValid = true
            };

            // Act
            _viewModel.UpdateFromThermalModule(thermalResult, null);

            // Assert
            Assert.That(_viewModel.PowerDown, Is.EqualTo(-5.0), "Отрицательный PowerDown должен быть передан");
        }

        #endregion
    }
}