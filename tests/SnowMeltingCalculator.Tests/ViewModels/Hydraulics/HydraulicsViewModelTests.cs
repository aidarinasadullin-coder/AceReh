using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;
using NUnit.Framework;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SnowMeltingCalculator.Tests.ViewModels.Hydraulics
{
    /// <summary>
    /// Тесты для HydraulicsViewModel
    /// </summary>
    [TestFixture]
    public class HydraulicsViewModelTests
    {
        private Mock<IHydraulicCalculator> _calculatorMock;
        private Mock<IGlycolDataService> _glycolServiceMock;
        private Mock<ICollectorRepository> _collectorRepositoryMock;
        private HydraulicsViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _calculatorMock = new Mock<IHydraulicCalculator>();
            _glycolServiceMock = new Mock<IGlycolDataService>();
            _collectorRepositoryMock = new Mock<ICollectorRepository>();

            _glycolServiceMock
                .Setup(s => s.GetProperties(It.IsAny<GlycolType>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(new GlycolProperties
                {
                    Density = 1053,
                    KinematicViscosity = 2.16,
                    SpecificHeat = 3.39
                });

            _calculatorMock
                .Setup(c => c.Calculate(It.IsAny<HydraulicParameters>()))
                .Returns(new HydraulicResult
                {
                    IsValid = true,
                    Velocity = 0.5,
                    ReynoldsNumber = 3700,
                    FlowRegime = FlowRegime.Turbulent,
                    FrictionFactor = 0.04,
                    PressureLossPerMeter = 100,
                    TotalPressureLoss = 10000
                });

            _calculatorMock
                .Setup(c => c.CalculateBalancing(It.IsAny<List<CircuitResult>>()))
                .Returns((List<CircuitResult> circuits) => circuits);

            _collectorRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Collector>
                {
                    new Collector
                    {
                        Id = "HKV-D-4",
                        Name = "HKV-D 4",
                        Type = CollectorType.HKV,
                        Circuits = 4,
                        Kv = 1.2,
                        MaxFlowRate = 1.5,
                        MaxPressure = 320
                    }
                });

            _viewModel = new HydraulicsViewModel(
                _calculatorMock.Object,
                _glycolServiceMock.Object,
                _collectorRepositoryMock.Object,
                null);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_InitializesDefaultValues()
        {
            // Assert
            Assert.That(_viewModel.CircuitLength, Is.EqualTo(100));
            Assert.That(_viewModel.SupplyLength, Is.EqualTo(10));
            Assert.That(_viewModel.GlycolConcentration, Is.EqualTo(50));
            Assert.That(_viewModel.GlycolType, Is.EqualTo(GlycolType.Ethylene));
        }

        [Test]
        public void Constructor_InitializesCollections()
        {
            // Assert
            Assert.That(_viewModel.Circuits, Is.Not.Null);
            Assert.That(_viewModel.Warnings, Is.Not.Null);
            Assert.That(_viewModel.AvailableCollectors, Is.Not.Null);
        }

        #endregion

        #region Calculate Tests

        [Test]
        public async Task CalculateAsync_WithValidParameters_ReturnsResult()
        {
            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Not.Null);
            Assert.That(_viewModel.HasErrors, Is.False);
        }

        [Test]
        public async Task CalculateAsync_WithInvalidParameters_SetsHasErrors()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.Calculate(It.IsAny<HydraulicParameters>()))
                .Returns(new HydraulicResult
                {
                    IsValid = false,
                    ValidationErrors = new[] { "Ошибка валидации" }
                });

            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.HasErrors, Is.True);
            Assert.That(_viewModel.ErrorMessage, Does.Contain("Ошибка"));
        }

        [Test]
        public async Task CalculateAsync_SetsIsCalculating()
        {
            // Arrange
            bool isCalculatingDuringExecution = false;
            _calculatorMock
                .Setup(c => c.Calculate(It.IsAny<HydraulicParameters>()))
                .Returns(() =>
                {
                    isCalculatingDuringExecution = _viewModel.IsCalculating;
                    return new HydraulicResult { IsValid = true };
                });

            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(isCalculatingDuringExecution, Is.True);
            Assert.That(_viewModel.IsCalculating, Is.False);
        }

        [Test]
        public async Task CalculateAsync_WithWarnings_AddsWarnings()
        {
            // Arrange
            _calculatorMock
                .Setup(c => c.Calculate(It.IsAny<HydraulicParameters>()))
                .Returns(new HydraulicResult
                {
                    IsValid = true,
                    Warnings = new[] { "Предупреждение 1", "Предупреждение 2" }
                });

            // Act
            await _viewModel.CalculateCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.Warnings.Count, Is.EqualTo(2));
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_ResetsToDefaultValues()
        {
            // Arrange
            _viewModel.CircuitLength = 200;
            _viewModel.SupplyLength = 20;
            _viewModel.GlycolConcentration = 30;
            _viewModel.GlycolType = GlycolType.Propylene;

            // Act
            _viewModel.ResetCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.CircuitLength, Is.EqualTo(100));
            Assert.That(_viewModel.SupplyLength, Is.EqualTo(10));
            Assert.That(_viewModel.GlycolConcentration, Is.EqualTo(50));
            Assert.That(_viewModel.GlycolType, Is.EqualTo(GlycolType.Ethylene));
        }

        [Test]
        public void Reset_ClearsResult()
        {
            // Arrange
            _viewModel.Result = new HydraulicResult { IsValid = true };

            // Act
            _viewModel.ResetCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Result, Is.Null);
        }

        [Test]
        public void Reset_ClearsErrors()
        {
            // Arrange
            _viewModel.HasErrors = true;
            _viewModel.ErrorMessage = "Test error";

            // Act
            _viewModel.ResetCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.HasErrors, Is.False);
            Assert.That(_viewModel.ErrorMessage, Is.Empty);
        }

        #endregion

        #region AddCircuit Tests

        [Test]
        public void AddCircuit_AddsNewCircuit()
        {
            // Act
            _viewModel.AddCircuitCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Circuits.Count, Is.EqualTo(1));
            Assert.That(_viewModel.Circuits[0].CircuitNumber, Is.EqualTo(1));
        }

        [Test]
        public void AddCircuit_SetsCircuitProperties()
        {
            // Arrange
            _viewModel.CircuitLength = 150;
            _viewModel.SupplyLength = 15;
            _viewModel.CircuitArea = 25;

            // Act
            _viewModel.AddCircuitCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Circuits[0].Length, Is.EqualTo(150));
            Assert.That(_viewModel.Circuits[0].SupplyLength, Is.EqualTo(15));
            Assert.That(_viewModel.Circuits[0].Area, Is.EqualTo(25));
        }

        [Test]
        public void AddCircuit_MultipleCircuits_IncrementsNumber()
        {
            // Act
            _viewModel.AddCircuitCommand.Execute(null);
            _viewModel.AddCircuitCommand.Execute(null);
            _viewModel.AddCircuitCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Circuits.Count, Is.EqualTo(3));
            Assert.That(_viewModel.Circuits[0].CircuitNumber, Is.EqualTo(1));
            Assert.That(_viewModel.Circuits[1].CircuitNumber, Is.EqualTo(2));
            Assert.That(_viewModel.Circuits[2].CircuitNumber, Is.EqualTo(3));
        }

        #endregion

        #region RemoveCircuit Tests

        [Test]
        public void RemoveCircuit_RemovesCircuit()
        {
            // Arrange
            _viewModel.AddCircuitCommand.Execute(null);
            _viewModel.AddCircuitCommand.Execute(null);

            // Act
            _viewModel.RemoveCircuitCommand.Execute(_viewModel.Circuits[0]);

            // Assert
            Assert.That(_viewModel.Circuits.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveCircuit_RenumbersCircuits()
        {
            // Arrange
            _viewModel.AddCircuitCommand.Execute(null);
            _viewModel.AddCircuitCommand.Execute(null);
            _viewModel.AddCircuitCommand.Execute(null);

            // Act
            _viewModel.RemoveCircuitCommand.Execute(_viewModel.Circuits[0]);

            // Assert
            Assert.That(_viewModel.Circuits.Count, Is.EqualTo(2));
            Assert.That(_viewModel.Circuits[0].CircuitNumber, Is.EqualTo(1));
            Assert.That(_viewModel.Circuits[1].CircuitNumber, Is.EqualTo(2));
        }

        [Test]
        public void RemoveCircuit_WithNull_DoesNotThrow()
        {
            // Arrange
            _viewModel.AddCircuitCommand.Execute(null);

            // Act & Assert
            Assert.DoesNotThrow(() => _viewModel.RemoveCircuitCommand.Execute(null));
        }

        #endregion

        #region BalanceCircuits Tests

        [Test]
        public async Task BalanceCircuits_WithNoCircuits_ReturnsEarly()
        {
            // Act
            await _viewModel.BalanceCircuitsCommand.ExecuteAsync(null);

            // Assert
            _calculatorMock.Verify(c => c.CalculateBalancing(It.IsAny<List<CircuitResult>>()), Times.Never);
        }

        [Test]
        public async Task BalanceCircuits_WithCircuits_CalculatesBalancing()
        {
            // Arrange
            _viewModel.AddCircuitCommand.Execute(null);
            _viewModel.Circuits[0].FlowRate = 200;

            // Act
            await _viewModel.BalanceCircuitsCommand.ExecuteAsync(null);

            // Assert
            _calculatorMock.Verify(c => c.CalculateBalancing(It.IsAny<List<CircuitResult>>()), Times.Once);
        }

        #endregion

        #region Computed Properties Tests

        [Test]
        public void MeanTemperature_CalculatesCorrectly()
        {
            // Arrange
            _viewModel.SupplyTemperature = 50;
            _viewModel.ReturnTemperature = 30;

            // Assert
            Assert.That(_viewModel.MeanTemperature, Is.EqualTo(40));
        }

        [Test]
        public void TemperatureDelta_CalculatesCorrectly()
        {
            // Arrange
            _viewModel.SupplyTemperature = 50;
            _viewModel.ReturnTemperature = 30;

            // Assert
            Assert.That(_viewModel.TemperatureDelta, Is.EqualTo(20));
        }

        [Test]
        public void TotalPressureLossKPa_ReturnsZeroWhenNoResult()
        {
            // Arrange
            _viewModel.Result = null;

            // Assert
            Assert.That(_viewModel.TotalPressureLossKPa, Is.EqualTo(0));
        }

        [Test]
        public void TotalPressureLossKPa_ConvertsCorrectly()
        {
            // Arrange
            _viewModel.Result = new HydraulicResult { TotalPressureLoss = 10000 };

            // Assert
            Assert.That(_viewModel.TotalPressureLossKPa, Is.EqualTo(10));
        }

        [Test]
        public void TotalPressureLossMbar_ConvertsCorrectly()
        {
            // Arrange
            _viewModel.Result = new HydraulicResult { TotalPressureLoss = 10000 };

            // Assert
            Assert.That(_viewModel.TotalPressureLossMbar, Is.EqualTo(100));
        }

        #endregion

        #region CanCalculate Tests

        [Test]
        public void CanCalculate_WhenCalculating_ReturnsFalse()
        {
            // Arrange
            _viewModel.IsCalculating = true;

            // Assert
            Assert.That(_viewModel.CanCalculate, Is.False);
        }

        [Test]
        public void CanCalculate_WithValidParameters_ReturnsTrue()
        {
            // Assert
            Assert.That(_viewModel.CanCalculate, Is.True);
        }

        [Test]
        public void CanCalculate_WithZeroCircuitLength_ReturnsFalse()
        {
            // Arrange
            _viewModel.CircuitLength = 0;

            // Assert
            Assert.That(_viewModel.CanCalculate, Is.False);
        }

        [Test]
        public void CanCalculate_WithZeroSupplyLength_ReturnsFalse()
        {
            // Arrange
            _viewModel.SupplyLength = 0;

            // Assert
            Assert.That(_viewModel.CanCalculate, Is.False);
        }

        #endregion

        #region Property Change Notification Tests

        [Test]
        public void CircuitLengthChange_NotifiesCanExecuteChanged()
        {
            // Arrange
            bool canExecuteChanged = false;
            _viewModel.CalculateCommand.CanExecuteChanged += (s, e) => canExecuteChanged = true;

            // Act
            _viewModel.CircuitLength = 50;

            // Assert
            Assert.That(canExecuteChanged, Is.True);
        }

        [Test]
        public void SupplyLengthChange_NotifiesCanExecuteChanged()
        {
            // Arrange
            bool canExecuteChanged = false;
            _viewModel.CalculateCommand.CanExecuteChanged += (s, e) => canExecuteChanged = true;

            // Act
            _viewModel.SupplyLength = 5;

            // Assert
            Assert.That(canExecuteChanged, Is.True);
        }

        #endregion
    }
}