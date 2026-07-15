using NUnit.Framework;
using Moq;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.Core;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace SnowMeltingCalculator.Tests.Thermal
{
    /// <summary>
    /// Тесты для ThermalViewModel
    /// </summary>
    [TestFixture]
    public class ThermalViewModelTests
    {
        private ThermalViewModel _viewModel = null!;
        private MockThermalCalculator _mockCalculator = null!;
        private ClimateData _mockClimateData = null!;
        private ConstructionData _mockConstructionData = null!;
        private Mock<ICalculationStateService> _mockCalculationStateService = null!;
        private IValidator<ThermalInputs> _thermalValidator = null!;
        private IValidator<ThermalCalculationResult> _thermalResultValidator = null!;

        [SetUp]
        public void Setup()
        {
            _mockCalculator = new MockThermalCalculator();
            _mockClimateData = new ClimateData
            {
                AirTemperature = -20.0,
                WindSpeed = 5.0,
                SnowfallIntensity = 2.0
            };
            _mockConstructionData = new ConstructionData
            {
                R1Total = 0.05,
                R2Total = 0.10,
                LambdaE = 1.6
            };
            _mockCalculationStateService = new Mock<ICalculationStateService>();
            _thermalValidator = new ThermalValidator(new ThermalCalculator(), _mockClimateData, _mockConstructionData);
            _thermalResultValidator = new ThermalResultValidator();
            _viewModel = new ThermalViewModel(
                _mockCalculator,
                _mockClimateData,
                _mockConstructionData,
                _mockCalculationStateService.Object,
                new CalculationContext(),
                _thermalValidator,
                _thermalResultValidator);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_InitializesDefaultValues()
        {
            // Assert
            Assert.That(_viewModel.SelectedMode, Is.EqualTo(OperatingMode.Melting));
            Assert.That(_viewModel.SupplyTemperature, Is.EqualTo(50.0));
            Assert.That(_viewModel.GroundTemperature, Is.EqualTo(10.0));
            Assert.That(_viewModel.PipeSpacing, Is.EqualTo(200));
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(_viewModel.IsCalculating, Is.False);
            Assert.That(_viewModel.ValidationMessage, Is.Empty);
            Assert.That(_viewModel.SelectedPipe, Is.Null);
            Assert.That(_viewModel.IsPipeSpacingEnabled, Is.False);
        }

        [Test]
        public void Constructor_InitializesCollections()
        {
            // Assert
            Assert.That(_viewModel.AvailablePipes.Count, Is.EqualTo(3));
            Assert.That(_viewModel.AvailableModes.Count, Is.EqualTo(3));
            Assert.That(_viewModel.AvailableModes, Contains.Item(OperatingMode.AntiIcing));
            Assert.That(_viewModel.AvailableModes, Contains.Item(OperatingMode.Melting));
            Assert.That(_viewModel.AvailableModes, Contains.Item(OperatingMode.Intensive));
            Assert.That(_viewModel.AvailablePipeSpacings, Is.EqualTo(new[] { 150, 200, 250, 300 }));
        }

        [Test]
        public void Constructor_SelectedPipeIsNullByDefault()
        {
            // Assert - По умолчанию труба не выбрана
            Assert.That(_viewModel.SelectedPipe, Is.Null);
        }

        [Test]
        public void Constructor_NullCalculator_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ThermalViewModel(null!, _mockClimateData, _mockConstructionData, _mockCalculationStateService.Object, new CalculationContext(), _thermalValidator, _thermalResultValidator));
        }

        [Test]
        public void Constructor_NullClimateData_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ThermalViewModel(_mockCalculator, null!, _mockConstructionData, _mockCalculationStateService.Object, new CalculationContext(), _thermalValidator, _thermalResultValidator));
        }

        [Test]
        public void Constructor_NullConstructionData_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ThermalViewModel(_mockCalculator, _mockClimateData, null!, _mockCalculationStateService.Object, new CalculationContext(), _thermalValidator, _thermalResultValidator));
        }

        [Test]
        public void Constructor_NullThermalValidator_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ThermalViewModel(_mockCalculator, _mockClimateData, _mockConstructionData, _mockCalculationStateService.Object, new CalculationContext(), null!, _thermalResultValidator));
        }

        [Test]
        public void Constructor_NullThermalResultValidator_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ThermalViewModel(_mockCalculator, _mockClimateData, _mockConstructionData, _mockCalculationStateService.Object, new CalculationContext(), _thermalValidator, null!));
        }

        #endregion

        #region Calculate Command Tests

        [Test]
        public async Task Calculate_ValidInput_SetsResult()
        {
            // Arrange
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];

            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Not.Null);
            Assert.That(_viewModel.Result!.IsValid, Is.True);
            Assert.That(_viewModel.ValidationMessage, Is.Empty);
        }

        [Test]
        public async Task Calculate_InvalidInput_SetsValidationMessage()
        {
            // Arrange
            _viewModel.SupplyTemperature = 100; // Выше допустимого

            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Температура подачи"));
        }

        [Test]
        public async Task Calculate_SetsIsCalculatingDuringExecution()
        {
            // Arrange
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];
            var wasCalculating = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ThermalViewModel.IsCalculating) && _viewModel.IsCalculating)
                {
                    wasCalculating = true;
                }
            };

            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(wasCalculating, Is.True);
            Assert.That(_viewModel.IsCalculating, Is.False);
        }

        [Test]
        public async Task Calculate_UsesClimateData()
        {
            // Arrange
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];
            _mockClimateData.AirTemperature = -30.0;
            _mockClimateData.WindSpeed = 8.0;
            _mockClimateData.SnowfallIntensity = 3.0;

            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Not.Null);
            // Проверяем, что калькулятор получил данные из климатического модуля
            Assert.That(_mockCalculator.LastClimateData!.AirTemperature, Is.EqualTo(-30.0));
            Assert.That(_mockCalculator.LastClimateData.WindSpeed, Is.EqualTo(8.0));
            Assert.That(_mockCalculator.LastClimateData.SnowfallIntensity, Is.EqualTo(3.0));
        }

        [Test]
        public async Task Calculate_UsesConstructionData()
        {
            // Arrange
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];
            _mockConstructionData.R1Total = 0.08;
            _mockConstructionData.R2Total = 0.12;
            _mockConstructionData.LambdaE = 1.8;

            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Not.Null);
            Assert.That(_mockCalculator.LastConstructionData!.R1Total, Is.EqualTo(0.08));
            Assert.That(_mockCalculator.LastConstructionData.R2Total, Is.EqualTo(0.12));
            Assert.That(_mockCalculator.LastConstructionData.LambdaE, Is.EqualTo(1.8));
        }

        [Test]
        public async Task Calculate_InvalidClimateData_ShowsError()
        {
            // Arrange
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];
            _mockClimateData.AirTemperature = 20.0; // Недопустимо высокая

            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Температура наружного воздуха"));
        }

        #endregion

        #region Reset Command Tests

        [Test]
        public void Reset_ResetsAllPropertiesToDefaults()
        {
            // Arrange
            _viewModel.SelectedMode = OperatingMode.Intensive;
            _viewModel.SupplyTemperature = 70.0;
            _viewModel.GroundTemperature = 15.0;
            _viewModel.SelectedPipe = PipeType.StandardPipes[0];
            _viewModel.PipeSpacing = 300;
            _viewModel.Result = new ThermalCalculationResult();
            _viewModel.ValidationMessage = "Ошибка";

            // Act
            _viewModel.ResetCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.SelectedMode, Is.EqualTo(OperatingMode.Melting));
            Assert.That(_viewModel.SupplyTemperature, Is.EqualTo(50.0));
            Assert.That(_viewModel.GroundTemperature, Is.EqualTo(10.0));
            Assert.That(_viewModel.SelectedPipe, Is.Null);
            Assert.That(_viewModel.PipeSpacing, Is.EqualTo(200));
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(_viewModel.ValidationMessage, Is.Empty);
        }

        #endregion

        #region Validation Tests

        [Test]
        public void Validate_SupplyTemperatureTooLow_ReturnsFalse()
        {
            // Arrange
            _viewModel.SupplyTemperature = 10.0; // Ниже минимума

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Температура подачи"));
        }

        [Test]
        public void Validate_SupplyTemperatureTooHigh_ReturnsFalse()
        {
            // Arrange
            _viewModel.SupplyTemperature = 100.0; // Выше максимума

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Температура подачи"));
        }

        [Test]
        public void Validate_GroundTemperatureTooLow_ReturnsFalse()
        {
            // Arrange
            _viewModel.GroundTemperature = -15.0; // Ниже минимума

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Температура грунта"));
        }

        [Test]
        public void Validate_GroundTemperatureTooHigh_ReturnsFalse()
        {
            // Arrange
            _viewModel.GroundTemperature = 40.0; // Выше максимума

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Температура грунта"));
        }

        [Test]
        public void Validate_PipeSpacingTooLow_ReturnsFalse()
        {
            // Arrange
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];
            _viewModel.PipeSpacing = 49; // Ниже минимума

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Шаг укладки"));
        }

        [Test]
        public void Validate_PipeSpacingTooHigh_ReturnsFalse()
        {
            // Arrange
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];
            _viewModel.PipeSpacing = 600; // Выше максимума

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Шаг укладки"));
        }

        [Test]
        public void Validate_ValidInput_ReturnsTrue()
        {
            // Arrange - все значения по умолчанию валидны
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Not.Null);
            Assert.That(_viewModel.ValidationMessage, Is.Empty);
        }

        [Test]
        public void Validate_SelectedPipeNull_ReturnsFalse()
        {
            // Arrange - труба не выбрана
            _viewModel.SelectedPipe = null;

            // Act
            _viewModel.CalculateCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(_viewModel.ValidationMessage, Does.Contain("Тип трубы"));
        }

        #endregion

        #region BuildThermalInputs Tests

        [Test]
        public void BuildThermalInputs_ReturnsCorrectParameters()
        {
            // Arrange
            _viewModel.SelectedMode = OperatingMode.Intensive;
            _viewModel.SupplyTemperature = 60.0;
            _viewModel.GroundTemperature = 5.0;
            _viewModel.SelectedPipe = PipeType.StandardPipes[2]; // 25x2.3
            _viewModel.PipeSpacing = 150;

            // Act
            var parameters = _viewModel.BuildThermalInputs();

            // Assert
            Assert.That(parameters.Mode, Is.EqualTo(OperatingMode.Intensive));
            Assert.That(parameters.SupplyTemperature, Is.EqualTo(60.0));
            Assert.That(parameters.DeltaT, Is.EqualTo(15.0)); // Значение по умолчанию
            Assert.That(parameters.GroundTemperature, Is.EqualTo(5.0));
            Assert.That(parameters.Pipe.Name, Is.EqualTo("RAUTHERM S 25x2,3"));
            Assert.That(parameters.PipeSpacing, Is.EqualTo(150.0));
        }

        [Test]
        public async Task Calculate_PassesClimateData()
        {
            // Arrange
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];
            _mockClimateData.AirTemperature = -25.0;
            _mockClimateData.WindSpeed = 6.0;
            _mockClimateData.SnowfallIntensity = 1.5;

            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_mockCalculator.LastClimateData, Is.Not.Null);
            Assert.That(_mockCalculator.LastClimateData!.AirTemperature, Is.EqualTo(-25.0));
            Assert.That(_mockCalculator.LastClimateData.WindSpeed, Is.EqualTo(6.0));
            Assert.That(_mockCalculator.LastClimateData.SnowfallIntensity, Is.EqualTo(1.5));
        }

        [Test]
        public async Task Calculate_PassesConstructionData()
        {
            // Arrange
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];
            _mockConstructionData.R1Total = 0.07;
            _mockConstructionData.R2Total = 0.15;
            _mockConstructionData.LambdaE = 1.9;

            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_mockCalculator.LastConstructionData, Is.Not.Null);
            Assert.That(_mockCalculator.LastConstructionData!.R1Total, Is.EqualTo(0.07));
            Assert.That(_mockCalculator.LastConstructionData.R2Total, Is.EqualTo(0.15));
            Assert.That(_mockCalculator.LastConstructionData.LambdaE, Is.EqualTo(1.9));
        }

        #endregion

        #region Mode Selection Tests

        [Test]
        public void SelectedMode_AntiIcing_SetsCorrectValue()
        {
            // Act
            _viewModel.SelectedMode = OperatingMode.AntiIcing;

            // Assert
            Assert.That(_viewModel.SelectedMode, Is.EqualTo(OperatingMode.AntiIcing));
        }

        [Test]
        public void SelectedMode_Melting_SetsCorrectValue()
        {
            // Act
            _viewModel.SelectedMode = OperatingMode.Melting;

            // Assert
            Assert.That(_viewModel.SelectedMode, Is.EqualTo(OperatingMode.Melting));
        }

        [Test]
        public void SelectedMode_Intensive_SetsCorrectValue()
        {
            // Act
            _viewModel.SelectedMode = OperatingMode.Intensive;

            // Assert
            Assert.That(_viewModel.SelectedMode, Is.EqualTo(OperatingMode.Intensive));
        }

        #endregion

        #region Pipe Selection Tests

        [Test]
        public void SelectedPipe_CanSelectDifferentPipes()
        {
            // Act & Assert
            _viewModel.SelectedPipe = PipeType.StandardPipes[0];
            Assert.That(_viewModel.SelectedPipe!.Name, Is.EqualTo("RAUTHERM S 17x2,0"));

            _viewModel.SelectedPipe = PipeType.StandardPipes[1];
            Assert.That(_viewModel.SelectedPipe!.Name, Is.EqualTo("RAUTHERM S 20x2,0"));

            _viewModel.SelectedPipe = PipeType.StandardPipes[2];
            Assert.That(_viewModel.SelectedPipe!.Name, Is.EqualTo("RAUTHERM S 25x2,3"));
        }

        [Test]
        public void IsPipeSpacingEnabled_FalseWhenNoPipeSelected()
        {
            // Arrange
            _viewModel.SelectedPipe = null;

            // Assert
            Assert.That(_viewModel.IsPipeSpacingEnabled, Is.False);
        }

        [Test]
        public void IsPipeSpacingEnabled_TrueWhenPipeSelected()
        {
            // Arrange
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];

            // Assert
            Assert.That(_viewModel.IsPipeSpacingEnabled, Is.True);
        }

        [Test]
        public void IsPipeSpacingEnabled_RaisesPropertyChangedWhenPipeChanges()
        {
            // Arrange
            var propertyChanged = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ThermalViewModel.IsPipeSpacingEnabled))
                    propertyChanged = true;
            };

            // Act
            _viewModel.SelectedPipe = PipeType.StandardPipes[1];

            // Assert
            Assert.That(propertyChanged, Is.True);
        }

        #endregion

        #region Climate Data Change Tests

        [Test]
        public void ClimateDataChanged_ClearsResult()
        {
            // Arrange
            _viewModel.Result = new ThermalCalculationResult { PowerTotal = 100 };
            string? capturedMessage = null;
            _mockCalculationStateService
                .Setup(s => s.SetThermalNeedsRecalculation(It.IsAny<string>()))
                .Callback<string>(msg => capturedMessage = msg);

            // Act
            _mockClimateData.RaiseDataChanged("AirTemperature", -20.0, -25.0, true);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(capturedMessage, Does.Contain("Климатические данные"));
        }

        #endregion

        #region Construction Data Change Tests

        [Test]
        public void ConstructionDataChanged_ClearsResult()
        {
            // Arrange
            _viewModel.Result = new ThermalCalculationResult { PowerTotal = 100 };
            string? capturedMessage = null;
            _mockCalculationStateService
                .Setup(s => s.SetThermalNeedsRecalculation(It.IsAny<string>()))
                .Callback<string>(msg => capturedMessage = msg);

            // Act
            _mockConstructionData.RaiseDataChanged("R1Total", 0.05, 0.06, true);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
            Assert.That(capturedMessage, Does.Contain("Данные конструкции"));
        }

        #endregion
    }

    /// <summary>
    /// Мок-калькулятор для тестов ViewModel
    /// </summary>
    internal class MockThermalCalculator : IThermalCalculator
    {
        public ThermalInputs? LastParameters { get; private set; }

        public double CalculateHeatTransferCoefficient(double surfaceTemp, double airTemp, double windSpeed)
        {
            // Простая формула для тестов
            return 2.26 * Math.Pow(Math.Max(surfaceTemp - airTemp, 1), 0.33) + 2.6 * windSpeed;
        }

        public double CalculatePowerUp(double snowfallIntensity, double surfaceTemp, double airTemp, double alpha)
        {
            // Упрощённая формула для тестов
            var meltingHeat = snowfallIntensity * 100; // Упрощение
            var convection = alpha * (surfaceTemp - airTemp);
            return meltingHeat + convection;
        }

        public (double RFb, double RD) CalculateThermalResistance(double r1Total, double r2Total, double alpha)
        {
            return (r1Total + 1.0 / alpha, r2Total);
        }

        public (double ParameterM, double EfficiencyEtaR) CalculateRodTheory(double rFb, double rD, double lambdaE, double dE, double spacing)
        {
            var m = 0.6 * Math.Sqrt((1.0 / rFb + 1.0 / rD) / (lambdaE * dE));
            var etaR = Math.Tanh(m * spacing / 2) / (m * spacing / 2);
            return (m, etaR);
        }

        public IClimateData? LastClimateData { get; private set; }
        public IConstructionData? LastConstructionData { get; private set; }

        public double CalculateExcessTemperature(ThermalInputs parameters, double powerUp, double rFb, double rD, double etaR, IClimateData climate, IConstructionData construction)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (climate == null) throw new ArgumentNullException(nameof(climate));
            if (construction == null) throw new ArgumentNullException(nameof(construction));
            if (etaR > 1.0) throw new ArgumentOutOfRangeException(nameof(etaR));
            return powerUp * rFb / etaR;
        }

        public ThermalCalculationResult Calculate(ThermalInputs inputs, IClimateData climate, IConstructionData construction)
        {
            LastParameters = inputs;
            LastClimateData = climate;
            LastConstructionData = construction;

            var alpha = CalculateHeatTransferCoefficient((int)inputs.Mode, climate.AirTemperature, climate.WindSpeed);
            var powerUp = CalculatePowerUp(climate.SnowfallIntensity, (int)inputs.Mode, climate.AirTemperature, alpha);
            var (rFb, rD) = CalculateThermalResistance(construction.R1Total, construction.R2Total, alpha);
            var (m, etaR) = CalculateRodTheory(rFb, rD, inputs.LambdaE, inputs.Pipe.OuterDiameter / 1000.0, inputs.PipeSpacing / 1000.0);
            var excessTemp = CalculateExcessTemperature(inputs, powerUp, rFb, rD, etaR, climate, construction);

            return new ThermalCalculationResult
            {
                Alpha = alpha,
                PowerUp = powerUp,
                PowerDown = powerUp * 0.1,
                PowerTotal = powerUp * 1.1,
                MeltingHeat = climate.SnowfallIntensity * 100,
                RadiationHeat = 0.3,
                ConvectionHeat = powerUp - climate.SnowfallIntensity * 100 - 0.3,
                ExcessTemperature = excessTemp,
                MeanTemperature = inputs.SupplyTemperature - inputs.DeltaT / 2,
                SupplyTemperature = inputs.SupplyTemperature,
                ReturnTemperature = inputs.SupplyTemperature - inputs.DeltaT,
                DeltaT = inputs.DeltaT,
                RFb = rFb,
                RD = rD,
                ParameterM = m,
                EfficiencyEtaR = etaR,
                MassFlowRate = 100,
                VolumeFlowRate = 95,
                IsValid = true,
                ValidationErrors = Array.Empty<string>()
            };
        }

        public bool Validate(ThermalInputs inputs, IClimateData climate, IConstructionData construction, out string[] errors)
        {
            errors = Array.Empty<string>();
            if (inputs == null)
            {
                errors = new[] { "Параметры не заданы" };
                return false;
            }
            if (climate == null)
            {
                errors = new[] { "Климатические данные не заданы" };
                return false;
            }
            if (construction == null)
            {
                errors = new[] { "Данные конструкции не заданы" };
                return false;
            }
            if (climate.WindSpeed < 0)
            {
                errors = new[] { "Скорость ветра не может быть отрицательной" };
                return false;
            }
            return true;
        }
    }
}