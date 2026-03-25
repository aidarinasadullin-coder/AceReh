using System.Collections.ObjectModel;
using NUnit.Framework;
using Moq;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Climate;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.ViewModels.Thermal;
using SnowMeltingCalculator.ViewModels.Climate;

namespace SnowMeltingCalculator.Tests.ViewModels.Hydraulics
{
    /// <summary>
    /// Тесты для CircuitsViewModel
    /// </summary>
    [TestFixture]
    public class CircuitsViewModelTests
    {
        private Mock<ICircuitsCalculator> _circuitsCalculatorMock = null!;
        private Mock<IGlycolDataService> _glycolServiceMock = null!;
        private Mock<IThermalCalculator> _thermalCalculatorMock = null!;
        private Mock<IClimateDataService> _climateDataServiceMock = null!;
        private Mock<ICalculationStateService> _calculationStateServiceMock = null!;
        private ClimateData _climateData = null!;
        private ConstructionData _constructionData = null!;
        private ThermalViewModel _thermalViewModel = null!;
        private ClimateViewModel _climateViewModel = null!;
        private CircuitsViewModel _viewModel = null!;

        [SetUp]
        public void Setup()
        {
            _circuitsCalculatorMock = new Mock<ICircuitsCalculator>();
            _glycolServiceMock = new Mock<IGlycolDataService>();
            _thermalCalculatorMock = new Mock<IThermalCalculator>();
            _climateDataServiceMock = new Mock<IClimateDataService>();
            _calculationStateServiceMock = new Mock<ICalculationStateService>();
            
            // Создаём реальные объекты для ClimateData и ConstructionData
            _climateData = new ClimateData();
            _constructionData = new ConstructionData();
            
            // Создаём реальные ViewModel с моками сервисов
            _thermalViewModel = new ThermalViewModel(
                _thermalCalculatorMock.Object,
                _climateData,
                _constructionData,
                _calculationStateServiceMock.Object
            );
            
            _climateViewModel = new ClimateViewModel(
                _climateDataServiceMock.Object,
                _climateData
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

            // Создаём ViewModel
            _viewModel = new CircuitsViewModel(
                _circuitsCalculatorMock.Object,
                _glycolServiceMock.Object,
                _thermalViewModel,
                _climateViewModel,
                _calculationStateServiceMock.Object
            );
        }

        #region CanRemoveCircuit Tests

        [Test]
        public void CanRemoveCircuit_WithNullCircuit_ReturnsFalse()
        {
            // Act
            var result = InvokePrivateMethod<bool>(_viewModel, "CanRemoveCircuit", new object?[] { null });

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void CanRemoveCircuit_WithSingleCircuit_ReturnsFalse()
        {
            // Arrange - в коллекторе 1 контур (минимум)
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1 });

            var circuit = collector.Circuits[0];

            // Act
            var result = InvokePrivateMethod<bool>(_viewModel, "CanRemoveCircuit", new object[] { circuit });

            // Assert
            Assert.That(result, Is.False, "Нельзя удалить единственный контур в коллекторе");
        }

        [Test]
        public void CanRemoveCircuit_WithMultipleCircuits_ReturnsTrue()
        {
            // Arrange - в коллекторе 3 контура
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 2 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 3 });

            var circuit = collector.Circuits[1];

            // Act
            var result = InvokePrivateMethod<bool>(_viewModel, "CanRemoveCircuit", new object[] { circuit });

            // Assert
            Assert.That(result, Is.True, "Можно удалить контур, если в коллекторе больше 1 контура");
        }

        [Test]
        public void CanRemoveCircuit_WithTwoCircuits_ReturnsTrue()
        {
            // Arrange - в коллекторе 2 контура (минимум для удаления)
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 2 });

            var circuit = collector.Circuits[0];

            // Act
            var result = InvokePrivateMethod<bool>(_viewModel, "CanRemoveCircuit", new object[] { circuit });

            // Assert
            Assert.That(result, Is.True, "Можно удалить контур, если в коллекторе 2 контура");
        }

        #endregion

        #region CanRemoveCollector Tests

        [Test]
        public void CanRemoveCollector_WithNullCollector_ReturnsFalse()
        {
            // Act
            var result = InvokePrivateMethod<bool>(_viewModel, "CanRemoveCollector", new object?[] { null });

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void CanRemoveCollector_WithSingleCollector_ReturnsFalse()
        {
            // Arrange - в системе 1 коллектор (минимум)
            _viewModel.Collectors.Clear();
            _viewModel.Collectors.Add(new CollectorData(1));
            var collector = _viewModel.Collectors[0];

            // Act
            var result = InvokePrivateMethod<bool>(_viewModel, "CanRemoveCollector", new object[] { collector });

            // Assert
            Assert.That(result, Is.False, "Нельзя удалить единственный коллектор в системе");
        }

        [Test]
        public void CanRemoveCollector_WithMultipleCollectors_ReturnsTrue()
        {
            // Arrange - в системе 2 коллектора
            _viewModel.Collectors.Clear();
            _viewModel.Collectors.Add(new CollectorData(1));
            _viewModel.Collectors.Add(new CollectorData(2));
            var collector = _viewModel.Collectors[1];

            // Act
            var result = InvokePrivateMethod<bool>(_viewModel, "CanRemoveCollector", new object[] { collector });

            // Assert
            Assert.That(result, Is.True, "Можно удалить коллектор, если в системе больше 1 коллектора");
        }

        [Test]
        public void CanRemoveCollector_WithTwoCollectors_ReturnsTrue()
        {
            // Arrange - в системе 2 коллектора (минимум для удаления)
            _viewModel.Collectors.Clear();
            _viewModel.Collectors.Add(new CollectorData(1));
            _viewModel.Collectors.Add(new CollectorData(2));
            var collector = _viewModel.Collectors[0];

            // Act
            var result = InvokePrivateMethod<bool>(_viewModel, "CanRemoveCollector", new object[] { collector });

            // Assert
            Assert.That(result, Is.True, "Можно удалить коллектор, если в системе 2 коллектора");
        }

        #endregion

        #region RenumberCollectors Tests

        [Test]
        public void RenumberCollectors_AfterRemoval_RenumbersCorrectly()
        {
            // Arrange - 3 коллектора
            _viewModel.Collectors.Clear();
            _viewModel.Collectors.Add(new CollectorData(1));
            _viewModel.Collectors.Add(new CollectorData(2));
            _viewModel.Collectors.Add(new CollectorData(3));

            // Удаляем коллектор №2
            _viewModel.Collectors.RemoveAt(1);

            // Act - вызываем RenumberCollectors
            InvokePrivateMethod(_viewModel, "RenumberCollectors");

            // Assert - коллекторы должны быть перенумерованы: 1, 2
            Assert.That(_viewModel.Collectors[0].CollectorNumber, Is.EqualTo(1));
            Assert.That(_viewModel.Collectors[1].CollectorNumber, Is.EqualTo(2));
        }

        [Test]
        public void RenumberCollectors_WithSingleCollector_DoesNotChange()
        {
            // Arrange - 1 коллектор
            _viewModel.Collectors.Clear();
            _viewModel.Collectors.Add(new CollectorData(5)); // Номер 5

            // Act
            InvokePrivateMethod(_viewModel, "RenumberCollectors");

            // Assert - номер должен стать 1
            Assert.That(_viewModel.Collectors[0].CollectorNumber, Is.EqualTo(1));
        }

        [Test]
        public void RenumberCollectors_WithFourCollectors_RenumbersCorrectly()
        {
            // Arrange - 4 коллектора
            _viewModel.Collectors.Clear();
            _viewModel.Collectors.Add(new CollectorData(10));
            _viewModel.Collectors.Add(new CollectorData(20));
            _viewModel.Collectors.Add(new CollectorData(30));
            _viewModel.Collectors.Add(new CollectorData(40));

            // Act
            InvokePrivateMethod(_viewModel, "RenumberCollectors");

            // Assert
            Assert.That(_viewModel.Collectors[0].CollectorNumber, Is.EqualTo(1));
            Assert.That(_viewModel.Collectors[1].CollectorNumber, Is.EqualTo(2));
            Assert.That(_viewModel.Collectors[2].CollectorNumber, Is.EqualTo(3));
            Assert.That(_viewModel.Collectors[3].CollectorNumber, Is.EqualTo(4));
        }

        #endregion

        #region RenumberCircuits Tests

        [Test]
        public void RenumberCircuits_AfterRemoval_RenumbersCorrectly()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 2 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 3 });

            // Удаляем контур №2
            collector.Circuits.RemoveAt(1);

            // Act
            InvokePrivateMethod(_viewModel, "RenumberCircuits", new object[] { collector });

            // Assert - контуры должны быть перенумерованы: 1, 2
            Assert.That(collector.Circuits[0].CircuitNumber, Is.EqualTo(1));
            Assert.That(collector.Circuits[1].CircuitNumber, Is.EqualTo(2));
        }

        [Test]
        public void RenumberCircuits_WithSingleCircuit_DoesNotChange()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 5 }); // Номер 5

            // Act
            InvokePrivateMethod(_viewModel, "RenumberCircuits", new object[] { collector });

            // Assert - номер должен стать 1
            Assert.That(collector.Circuits[0].CircuitNumber, Is.EqualTo(1));
        }

        #endregion

        #region AddCollector Tests

        [Test]
        public void AddCollector_IncreasesCollectorCount()
        {
            // Arrange
            var initialCount = _viewModel.Collectors.Count;

            // Act
            _viewModel.AddCollectorCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Collectors.Count, Is.EqualTo(initialCount + 1));
        }

        [Test]
        public void AddCollector_SetsCorrectCollectorNumber()
        {
            // Arrange
            _viewModel.Collectors.Clear();
            _viewModel.Collectors.Add(new CollectorData(1));

            // Act
            _viewModel.AddCollectorCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Collectors[1].CollectorNumber, Is.EqualTo(2));
        }

        [Test]
        public void AddCollector_MaximumFourCollectors()
        {
            // Arrange - добавляем 4 коллектора
            _viewModel.Collectors.Clear();
            for (int i = 0; i < 4; i++)
            {
                _viewModel.AddCollectorCommand.Execute(null);
            }

            // Act - пытаемся добавить 5-й
            _viewModel.AddCollectorCommand.Execute(null);

            // Assert - должно остаться 4
            Assert.That(_viewModel.Collectors.Count, Is.EqualTo(4));
        }

        [Test]
        public void AddCollector_CreatesTwoDefaultCircuits()
        {
            // Arrange
            _viewModel.Collectors.Clear();

            // Act
            _viewModel.AddCollectorCommand.Execute(null);

            // Assert - по умолчанию создаётся 2 контура
            Assert.That(_viewModel.Collectors[0].Circuits.Count, Is.EqualTo(2));
        }

        #endregion

        #region AddCircuit Tests

        [Test]
        public void AddCircuit_IncreasesCircuitCount()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            var initialCount = collector.Circuits.Count;

            // Act
            _viewModel.AddCircuitCommand.Execute(null);

            // Assert
            Assert.That(collector.Circuits.Count, Is.EqualTo(initialCount + 1));
        }

        [Test]
        public void AddCircuit_SetsCorrectCircuitNumber()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 2 });

            // Act
            _viewModel.AddCircuitCommand.Execute(null);

            // Assert
            Assert.That(collector.Circuits[2].CircuitNumber, Is.EqualTo(3));
        }

        [Test]
        public void AddCircuit_MaximumTwelveCircuits()
        {
            // Arrange - добавляем 12 контуров
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            for (int i = 0; i < 12; i++)
            {
                collector.Circuits.Add(new CircuitRow { CircuitNumber = i + 1 });
            }

            // Act - пытаемся добавить 13-й
            _viewModel.AddCircuitCommand.Execute(null);

            // Assert - должно остаться 12
            Assert.That(collector.Circuits.Count, Is.EqualTo(12));
        }

        #endregion

        #region SwitchMode Tests

        [Test]
        public void CurrentMode_DefaultValue_IsOperatingTemperature()
        {
            // Assert
            Assert.That(_viewModel.CurrentMode, Is.EqualTo(HydraulicMode.OperatingTemperature));
        }

        [Test]
        public void SwitchMode_FromOperatingToDesign_ChangesMode()
        {
            // Arrange
            _viewModel.CurrentMode = HydraulicMode.OperatingTemperature;

            // Act
            _viewModel.SwitchModeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.CurrentMode, Is.EqualTo(HydraulicMode.DesignTemperature));
        }

        [Test]
        public void SwitchMode_FromDesignToOperating_ChangesMode()
        {
            // Arrange
            _viewModel.CurrentMode = HydraulicMode.DesignTemperature;

            // Act
            _viewModel.SwitchModeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.CurrentMode, Is.EqualTo(HydraulicMode.OperatingTemperature));
        }

        [Test]
        public void SwitchMode_Twice_ReturnsToOriginalMode()
        {
            // Arrange
            var originalMode = _viewModel.CurrentMode;

            // Act
            _viewModel.SwitchModeCommand.Execute(null);
            _viewModel.SwitchModeCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.CurrentMode, Is.EqualTo(originalMode));
        }

        [Test]
        public void OperatingModeButtonText_ContainsTemperature()
        {
            // Assert
            Assert.That(_viewModel.OperatingModeButtonText, Does.Contain("Рабочая температура"));
            Assert.That(_viewModel.OperatingModeButtonText, Does.Contain("°C"));
        }

        [Test]
        public void DesignModeButtonText_ContainsTemperature()
        {
            // Assert
            Assert.That(_viewModel.DesignModeButtonText, Does.Contain("Расчётная температура"));
            Assert.That(_viewModel.DesignModeButtonText, Does.Contain("°C"));
        }

        [Test]
        public void UpdateCircuitDisplayMode_UpdatesAllCircuits()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 2 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 3 });

            // Set initial mode
            foreach (var circuit in collector.Circuits)
            {
                circuit.DisplayMode = HydraulicMode.OperatingTemperature;
            }

            // Act
            _viewModel.CurrentMode = HydraulicMode.DesignTemperature;

            // Assert
            foreach (var circuit in collector.Circuits)
            {
                Assert.That(circuit.DisplayMode, Is.EqualTo(HydraulicMode.DesignTemperature));
            }
        }

        [Test]
        public void CurrentResult_ReturnsCorrectResultBasedOnMode()
        {
            // Arrange
            var circuit = new CircuitRow
            {
                CircuitNumber = 1,
                CircuitLength = 100
            };

            // Set different results for different temperatures
            circuit.OperatingResult = new CircuitTemperatureResult
            {
                Temperature = 32.5,
                ReynoldsNumber = 10000,
                FrictionFactor = 0.03
            };

            circuit.DesignResult = new CircuitTemperatureResult
            {
                Temperature = -28.0,
                ReynoldsNumber = 5000,
                FrictionFactor = 0.04
            };

            // Act & Assert - Operating mode
            circuit.DisplayMode = HydraulicMode.OperatingTemperature;
            Assert.That(circuit.CurrentResult.Temperature, Is.EqualTo(32.5));
            Assert.That(circuit.CurrentResult.ReynoldsNumber, Is.EqualTo(10000));

            // Act & Assert - Design mode
            circuit.DisplayMode = HydraulicMode.DesignTemperature;
            Assert.That(circuit.CurrentResult.Temperature, Is.EqualTo(-28.0));
            Assert.That(circuit.CurrentResult.ReynoldsNumber, Is.EqualTo(5000));
        }

        #endregion

        #region AutoSelectCollectorType Tests

        [Test]
        public void AutoSelectCollectorType_FlowRateBelow1_5_SelectsHKV_D()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "IV 1½\" (2-12 контуров)"; // Начинаем с другого типа
            collector.ValveType = ValveType.IV_1_5;
            
            // Устанавливаем расход 1000 л/ч = 1.0 м³/ч (< 1.5 м³/ч)
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 1000, // л/ч
                CircuitCount = 4
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert
            Assert.That(collector.CollectorType, Is.EqualTo("HKV-D (2-12 контуров)"));
            Assert.That(collector.ValveType, Is.EqualTo(ValveType.HKV_D));
            Assert.That(collector.Summary.Warning, Is.Null);
        }

        [Test]
        public void AutoSelectCollectorType_FlowRate1_5To2_5_SelectsIV_1_25()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "HKV-D (2-12 контуров)";
            collector.ValveType = ValveType.HKV_D;
            
            // Устанавливаем расход 2000 л/ч = 2.0 м³/ч (1.5 < x < 2.5)
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 2000, // л/ч
                CircuitCount = 4
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert
            Assert.That(collector.CollectorType, Is.EqualTo("IV 1¼\" (2-12 контуров)"));
            Assert.That(collector.ValveType, Is.EqualTo(ValveType.IV_1_25));
            Assert.That(collector.Summary.Warning, Is.Null);
        }

        [Test]
        public void AutoSelectCollectorType_FlowRate2_5To7_0_SelectsIV_1_5()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "HKV-D (2-12 контуров)";
            collector.ValveType = ValveType.HKV_D;
            
            // Устанавливаем расход 3000 л/ч = 3.0 м³/ч (2.5 ≤ x < 7.0)
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 3000, // л/ч
                CircuitCount = 4
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert
            Assert.That(collector.CollectorType, Is.EqualTo("IV 1½\" (2-12 контуров)"));
            Assert.That(collector.ValveType, Is.EqualTo(ValveType.IV_1_5));
            Assert.That(collector.Summary.Warning, Is.Null);
        }

        [Test]
        public void AutoSelectCollectorType_FlowRateAbove7_0_SetsWarning()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "HKV-D (2-12 контуров)";
            collector.ValveType = ValveType.HKV_D;
            
            // Устанавливаем расход 8000 л/ч = 8.0 м³/ч (≥ 7.0 м³/ч)
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 8000, // л/ч
                CircuitCount = 4
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert
            Assert.That(collector.Summary.Warning, Is.Not.Null);
            Assert.That(collector.Summary.Warning, Does.Contain("Превышение расхода"));
            Assert.That(collector.Summary.Warning, Does.Contain("8.00 м³/ч"));
            Assert.That(collector.Summary.Warning, Does.Contain("7.0 м³/ч"));
        }

        [Test]
        public void AutoSelectCollectorType_FlowRate5_0_SelectsIV_1_5()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "HKV-D (2-12 контуров)";
            collector.ValveType = ValveType.HKV_D;
            
            // Устанавливаем расход 5000 л/ч = 5.0 м³/ч (2.5 ≤ x < 7.0)
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 5000, // л/ч
                CircuitCount = 4
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert
            Assert.That(collector.CollectorType, Is.EqualTo("IV 1½\" (2-12 контуров)"));
            Assert.That(collector.ValveType, Is.EqualTo(ValveType.IV_1_5));
            Assert.That(collector.Summary.Warning, Is.Null);
        }

        [Test]
        public void AutoSelectCollectorType_FlowRateExactly1_5_SelectsHKV_D()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "IV 1¼\" (2-12 контуров)";
            collector.ValveType = ValveType.IV_1_25;
            
            // Устанавливаем расход ровно 1500 л/ч = 1.5 м³/ч (граница)
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 1500, // л/ч
                CircuitCount = 4
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert - при ровно 1.5 должен выбрать HKV-D (≤ 1.5)
            Assert.That(collector.CollectorType, Is.EqualTo("HKV-D (2-12 контуров)"));
            Assert.That(collector.ValveType, Is.EqualTo(ValveType.HKV_D));
        }

        [Test]
        public void AutoSelectCollectorType_FlowRateExactly2_5_SelectsIV_1_5()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "IV 1¼\" (2-12 контуров)";
            collector.ValveType = ValveType.IV_1_25;
            
            // Устанавливаем расход ровно 2500 л/ч = 2.5 м³/ч (граница)
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 2500, // л/ч
                CircuitCount = 4
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert - при ровно 2.5 должен выбрать IV 1½" (≥ 2.5)
            Assert.That(collector.CollectorType, Is.EqualTo("IV 1½\" (2-12 контуров)"));
            Assert.That(collector.ValveType, Is.EqualTo(ValveType.IV_1_5));
        }

        [Test]
        public void AutoSelectCollectorType_FlowRateExactly7_0_SetsWarning()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "IV 1½\" (2-12 контуров)";
            collector.ValveType = ValveType.IV_1_5;
            
            // Устанавливаем расход ровно 7000 л/ч = 7.0 м³/ч (граница)
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 7000, // л/ч
                CircuitCount = 4
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert - при ровно 7.0 должно быть предупреждение (≥ 7.0)
            Assert.That(collector.Summary.Warning, Is.Not.Null);
            Assert.That(collector.Summary.Warning, Does.Contain("Превышение расхода"));
            Assert.That(collector.Summary.Warning, Does.Contain("7.00 м³/ч"));
        }

        [Test]
        public void AutoSelectCollectorType_FlowRateJustBelow7_0_SelectsIV_1_5()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "HKV-D (2-12 контуров)";
            collector.ValveType = ValveType.HKV_D;
            
            // Устанавливаем расход 6990 л/ч = 6.99 м³/ч (чуть меньше 7.0)
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 6990, // л/ч
                CircuitCount = 4
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert - при 6.99 должен выбрать IV 1½" без предупреждения
            Assert.That(collector.CollectorType, Is.EqualTo("IV 1½\" (2-12 контуров)"));
            Assert.That(collector.ValveType, Is.EqualTo(ValveType.IV_1_5));
            Assert.That(collector.Summary.Warning, Is.Null);
        }

        [Test]
        public void AutoSelectCollectorType_ClearsWarningWhenFlowRateDecreases()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            
            // Сначала устанавливаем предупреждение
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 8000, // л/ч (≥ 7.0 м³/ч)
                Warning = "Превышение расхода"
            };

            // Затем уменьшаем расход
            collector.Summary.TotalFlowRate = 2000; // л/ч (2.0 м³/ч)

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert - предупреждение должно быть очищено
            Assert.That(collector.Summary.Warning, Is.Null);
        }

        [Test]
        public void AutoSelectCollectorType_OperatingPressureExceeded_SetsWarning()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "HKV-D (2-12 контуров)";
            collector.ValveType = ValveType.HKV_D;
            
            // Устанавливаем давление в рабочем режиме выше лимита (43.7 кПа > 32 кПа)
            // и нормальное давление в холодном режиме
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 1000, // л/ч (1.0 м³/ч) - нормальный расход
                PressureLoss_Operating_Pa = 43700, // 43.7 кПа > 32 кПа (превышение)
                PressureLoss_Cold_Pa = 25000 // 25 кПа < 32 кПа (норма)
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert
            Assert.That(collector.Summary.Warning, Is.Not.Null);
            Assert.That(collector.Summary.Warning, Does.Contain("Превышение давления"));
            Assert.That(collector.Summary.Warning, Does.Contain("рабочий режим"));
            Assert.That(collector.Summary.Warning, Does.Contain("32 кПа"));
            // Проверяем, что значение давления присутствует (формат зависит от локали)
            Assert.That(collector.Summary.Warning, Does.Contain("43")); // 43.7 или 43,7
        }

        [Test]
        public void AutoSelectCollectorType_ColdPressureExceeded_SetsWarning()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "HKV-D (2-12 контуров)";
            collector.ValveType = ValveType.HKV_D;
            
            // Устанавливаем нормальное давление в рабочем режиме
            // и давление в холодном режиме выше лимита
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 1000, // л/ч (1.0 м³/ч) - нормальный расход
                PressureLoss_Operating_Pa = 25000, // 25 кПа < 32 кПа (норма)
                PressureLoss_Cold_Pa = 45000 // 45 кПа > 32 кПа (превышение)
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert
            Assert.That(collector.Summary.Warning, Is.Not.Null);
            Assert.That(collector.Summary.Warning, Does.Contain("Превышение давления"));
            Assert.That(collector.Summary.Warning, Does.Contain("холодный пуск"));
            Assert.That(collector.Summary.Warning, Does.Contain("32 кПа"));
            // Проверяем, что значение давления присутствует (формат зависит от локали)
            Assert.That(collector.Summary.Warning, Does.Contain("45")); // 45.0 или 45,0
        }

        [Test]
        public void AutoSelectCollectorType_BothPressuresExceeded_SetsBothWarnings()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "HKV-D (2-12 контуров)";
            collector.ValveType = ValveType.HKV_D;
            
            // Устанавливаем превышение давления в обоих режимах
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 1000, // л/ч (1.0 м³/ч) - нормальный расход
                PressureLoss_Operating_Pa = 43700, // 43.7 кПа > 32 кПа (превышение)
                PressureLoss_Cold_Pa = 52000 // 52 кПа > 32 кПа (превышение)
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert
            Assert.That(collector.Summary.Warning, Is.Not.Null);
            // Должны быть оба предупреждения
            Assert.That(collector.Summary.Warning, Does.Contain("рабочий режим"));
            Assert.That(collector.Summary.Warning, Does.Contain("холодный пуск"));
            // Проверяем, что значения давления присутствуют (формат зависит от локали)
            Assert.That(collector.Summary.Warning, Does.Contain("43")); // 43.7 или 43,7
            Assert.That(collector.Summary.Warning, Does.Contain("52")); // 52.0 или 52,0
        }

        [Test]
        public void AutoSelectCollectorType_NormalPressures_NoWarning()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "HKV-D (2-12 контуров)";
            collector.ValveType = ValveType.HKV_D;
            
            // Устанавливаем нормальное давление в обоих режимах
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 1000, // л/ч (1.0 м³/ч) - нормальный расход
                PressureLoss_Operating_Pa = 20000, // 20 кПа < 32 кПа (норма)
                PressureLoss_Cold_Pa = 25000 // 25 кПа < 32 кПа (норма)
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert
            Assert.That(collector.Summary.Warning, Is.Null);
        }

        [Test]
        public void AutoSelectCollectorType_PressureExceededTakesPriorityOverFlowRate()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "HKV-D (2-12 контуров)";
            collector.ValveType = ValveType.HKV_D;
            
            // Устанавливаем превышение давления И расхода
            // Предупреждение о давлении должно иметь приоритет
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 8000, // л/ч (8.0 м³/ч) - превышение расхода
                PressureLoss_Operating_Pa = 45000, // 45 кПа > 32 кПа (превышение)
                PressureLoss_Cold_Pa = 50000 // 50 кПа > 32 кПа (превышение)
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert
            Assert.That(collector.Summary.Warning, Is.Not.Null);
            // Должно быть предупреждение о давлении, а не о расходе
            Assert.That(collector.Summary.Warning, Does.Contain("Превышение давления"));
            Assert.That(collector.Summary.Warning, Does.Not.Contain("Превышение расхода"));
        }

        [Test]
        public void AutoSelectCollectorType_PressureExactlyAtLimit_NoWarning()
        {
            // Arrange
            var collector = _viewModel.Collectors[0];
            collector.CollectorType = "HKV-D (2-12 контуров)";
            collector.ValveType = ValveType.HKV_D;
            
            // Устанавливаем давление ровно на лимите (32 кПа = 32000 Па)
            collector.Summary = new CollectorSummary
            {
                TotalFlowRate = 1000, // л/ч (1.0 м³/ч) - нормальный расход
                PressureLoss_Operating_Pa = 32000, // ровно 32 кПа (на лимите)
                PressureLoss_Cold_Pa = 32000 // ровно 32 кПа (на лимите)
            };

            // Act
            InvokePrivateMethod(_viewModel, "AutoSelectCollectorType");

            // Assert - на лимите предупреждения быть не должно (только при >)
            Assert.That(collector.Summary.Warning, Is.Null);
        }

        #endregion

        #region CollectorTypeDisplayWithCount Tests

        [Test]
        public void CollectorTypeDisplayWithCount_HKV_D_WithOneCircuit_ReturnsCorrectFormat()
        {
            // Arrange
            var collector = new CollectorData(1);
            collector.ValveType = ValveType.HKV_D;
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1 });

            // Act
            var result = collector.CollectorTypeDisplayWithCount;

            // Assert
            Assert.That(result, Is.EqualTo("HKV-D (1 контур)"));
        }

        [Test]
        public void CollectorTypeDisplayWithCount_HKV_D_WithTwoCircuits_ReturnsCorrectFormat()
        {
            // Arrange
            var collector = new CollectorData(1);
            collector.ValveType = ValveType.HKV_D;
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1 });
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 2 });

            // Act
            var result = collector.CollectorTypeDisplayWithCount;

            // Assert
            Assert.That(result, Is.EqualTo("HKV-D (2 контура)"));
        }

        [Test]
        public void CollectorTypeDisplayWithCount_HKV_D_WithThreeCircuits_ReturnsCorrectFormat()
        {
            // Arrange
            var collector = new CollectorData(1);
            collector.ValveType = ValveType.HKV_D;
            collector.Circuits.Clear();
            for (int i = 0; i < 3; i++)
            {
                collector.Circuits.Add(new CircuitRow { CircuitNumber = i + 1 });
            }

            // Act
            var result = collector.CollectorTypeDisplayWithCount;

            // Assert
            Assert.That(result, Is.EqualTo("HKV-D (3 контура)"));
        }

        [Test]
        public void CollectorTypeDisplayWithCount_HKV_D_WithFiveCircuits_ReturnsCorrectFormat()
        {
            // Arrange
            var collector = new CollectorData(1);
            collector.ValveType = ValveType.HKV_D;
            collector.Circuits.Clear();
            for (int i = 0; i < 5; i++)
            {
                collector.Circuits.Add(new CircuitRow { CircuitNumber = i + 1 });
            }

            // Act
            var result = collector.CollectorTypeDisplayWithCount;

            // Assert
            Assert.That(result, Is.EqualTo("HKV-D (5 контуров)"));
        }

        [Test]
        public void CollectorTypeDisplayWithCount_HKV_D_WithTwelveCircuits_ReturnsCorrectFormat()
        {
            // Arrange
            var collector = new CollectorData(1);
            collector.ValveType = ValveType.HKV_D;
            collector.Circuits.Clear();
            for (int i = 0; i < 12; i++)
            {
                collector.Circuits.Add(new CircuitRow { CircuitNumber = i + 1 });
            }

            // Act
            var result = collector.CollectorTypeDisplayWithCount;

            // Assert
            Assert.That(result, Is.EqualTo("HKV-D (12 контуров)"));
        }

        [Test]
        public void CollectorTypeDisplayWithCount_IV_1_25_WithFiveCircuits_ReturnsCorrectFormat()
        {
            // Arrange
            var collector = new CollectorData(1);
            collector.ValveType = ValveType.IV_1_25;
            collector.Circuits.Clear();
            for (int i = 0; i < 5; i++)
            {
                collector.Circuits.Add(new CircuitRow { CircuitNumber = i + 1 });
            }

            // Act
            var result = collector.CollectorTypeDisplayWithCount;

            // Assert
            Assert.That(result, Is.EqualTo("IV 1¼\" (5 контуров)"));
        }

        [Test]
        public void CollectorTypeDisplayWithCount_IV_1_5_WithEightCircuits_ReturnsCorrectFormat()
        {
            // Arrange
            var collector = new CollectorData(1);
            collector.ValveType = ValveType.IV_1_5;
            collector.Circuits.Clear();
            for (int i = 0; i < 8; i++)
            {
                collector.Circuits.Add(new CircuitRow { CircuitNumber = i + 1 });
            }

            // Act
            var result = collector.CollectorTypeDisplayWithCount;

            // Assert
            Assert.That(result, Is.EqualTo("IV 1½\" (8 контуров)"));
        }

        [Test]
        public void CollectorTypeDisplayWithCount_FourCircuits_ReturnsCorrectPlural()
        {
            // Arrange
            var collector = new CollectorData(1);
            collector.ValveType = ValveType.HKV_D;
            collector.Circuits.Clear();
            for (int i = 0; i < 4; i++)
            {
                collector.Circuits.Add(new CircuitRow { CircuitNumber = i + 1 });
            }

            // Act
            var result = collector.CollectorTypeDisplayWithCount;

            // Assert - "4 контура" (2, 3, 4)
            Assert.That(result, Is.EqualTo("HKV-D (4 контура)"));
        }

        [Test]
        public void CollectorTypeDisplayWithCount_UpdatesWhenCircuitsChange()
        {
            // Arrange
            var collector = new CollectorData(1);
            collector.ValveType = ValveType.HKV_D;
            collector.Circuits.Clear();
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 1 });

            // Act - начальное значение
            var result1 = collector.CollectorTypeDisplayWithCount;
            
            // Добавляем контур
            collector.Circuits.Add(new CircuitRow { CircuitNumber = 2 });
            var result2 = collector.CollectorTypeDisplayWithCount;

            // Assert
            Assert.That(result1, Is.EqualTo("HKV-D (1 контур)"));
            Assert.That(result2, Is.EqualTo("HKV-D (2 контура)"));
        }

        [Test]
        public void CollectorTypeDisplayWithCount_UpdatesWhenValveTypeChanges()
        {
            // Arrange
            var collector = new CollectorData(1);
            collector.ValveType = ValveType.HKV_D;
            collector.Circuits.Clear();
            for (int i = 0; i < 3; i++)
            {
                collector.Circuits.Add(new CircuitRow { CircuitNumber = i + 1 });
            }

            // Act - начальное значение
            var result1 = collector.CollectorTypeDisplayWithCount;
            
            // Меняем тип клапана
            collector.ValveType = ValveType.IV_1_5;
            var result2 = collector.CollectorTypeDisplayWithCount;

            // Assert
            Assert.That(result1, Is.EqualTo("HKV-D (3 контура)"));
            Assert.That(result2, Is.EqualTo("IV 1½\" (3 контура)"));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Вызывает приватный метод через рефлексию
        /// </summary>
        private static T InvokePrivateMethod<T>(object obj, string methodName, object?[]? parameters = null)
        {
            var method = obj.GetType().GetMethod(methodName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (method == null)
                throw new System.Exception($"Метод {methodName} не найден");
            
            var result = method.Invoke(obj, parameters);
            return (T)result!;
        }

        /// <summary>
        /// Вызывает приватный метод без возвращаемого значения
        /// </summary>
        private static void InvokePrivateMethod(object obj, string methodName, object?[]? parameters = null)
        {
            var method = obj.GetType().GetMethod(methodName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (method == null)
                throw new System.Exception($"Метод {methodName} не найден");
            
            method.Invoke(obj, parameters);
        }

        #endregion
    }
}