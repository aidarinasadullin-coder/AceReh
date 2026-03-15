# Task 4.1: HydraulicsViewModel (Основная ViewModel)

**Этап:** 4 - ViewModels  
**Приоритет:** Высокий  
**Статус:** Не начато  
**Зависимости:** Task 3.1, Task 3.3, Task 3.4, Task 3.5

---

## 1. Цель задачи

Создать основную ViewModel для модуля гидравлики с привязкой к View и интеграцией с ThermalModule.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-01 | Расчёт гидравлических параметров контура | CalculateCommand |
| UC-08 | Интеграция с ThermalModule | Подписка на ResultChanged |

---

## 3. Создаваемые файлы

### 3.1. HydraulicsViewModel.cs

**Путь:** `src/ViewModels/Hydraulics/HydraulicsViewModel.cs`

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;

namespace SnowMeltingCalculator.ViewModels.Hydraulics
{
    /// <summary>
    /// Основная ViewModel для модуля гидравлики
    /// </summary>
    public partial class HydraulicsViewModel : ObservableObject
    {
        #region Services

        private readonly IHydraulicCalculator _hydraulicCalculator;
        private readonly IGlycolDataService _glycolService;
        private readonly ICollectorRepository _collectorRepository;
        private readonly HydraulicValidator _validator;

        #endregion

        #region Observable Properties

        /// <summary>
        /// Длина контура (м)
        /// </summary>
        [ObservableProperty]
        private double _circuitLength = 100;

        /// <summary>
        /// Длина подводки (м)
        /// </summary>
        [ObservableProperty]
        private double _supplyLength = 10;

        /// <summary>
        /// Доля гликоля (%)
        /// </summary>
        [ObservableProperty]
        private double _glycolConcentration = 50;

        /// <summary>
        /// Тип гликоли
        /// </summary>
        [ObservableProperty]
        private GlycolType _glycolType = GlycolType.Ethylene;

        /// <summary>
        /// Температура подачи (°C) — из ThermalModule
        /// </summary>
        [ObservableProperty]
        private double _supplyTemperature = 50;

        /// <summary>
        /// Температура обратки (°C) — из ThermalModule
        /// </summary>
        [ObservableProperty]
        private double _returnTemperature = 30;

        /// <summary>
        /// Объёмный расход (л/ч) — из ThermalModule
        /// </summary>
        [ObservableProperty]
        private double _volumeFlowRate = 200;

        /// <summary>
        /// Площадь контура (м²) — из ThermalModule
        /// </summary>
        [ObservableProperty]
        private double _circuitArea = 20;

        /// <summary>
        /// Выбранный тип трубы
        /// </summary>
        [ObservableProperty]
        private PipeType _selectedPipe = new PipeType { OuterDiameter = 20, WallThickness = 2, Name = "RAUTHERM S 20x2.0" };

        /// <summary>
        /// Шероховатость трубы (мм)
        /// </summary>
        [ObservableProperty]
        private double _roughness = 0.007;

        /// <summary>
        /// Результат расчёта
        /// </summary>
        [ObservableProperty]
        private HydraulicResult _result;

        /// <summary>
        /// Признак выполнения расчёта
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanCalculate))]
        private bool _isCalculating;

        /// <summary>
        /// Признак наличия ошибок
        /// </summary>
        [ObservableProperty]
        private bool _hasErrors;

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        [ObservableProperty]
        private string _errorMessage;

        /// <summary>
        /// Список предупреждений
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _warnings = new ObservableCollection<string>();

        /// <summary>
        /// Список контуров
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CircuitViewModel> _circuits = new ObservableCollection<CircuitViewModel>();

        /// <summary>
        /// Выбранный коллектор
        /// </summary>
        [ObservableProperty]
        private Collector _selectedCollector;

        /// <summary>
        /// Список доступных коллекторов
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Collector> _availableCollectors = new ObservableCollection<Collector>();

        #endregion

        #region Computed Properties

        /// <summary>
        /// Признак возможности расчёта
        /// </summary>
        public bool CanCalculate => !IsCalculating && CircuitLength > 0 && SupplyLength > 0;

        /// <summary>
        /// Средняя температура теплоносителя
        /// </summary>
        public double MeanTemperature => (SupplyTemperature + ReturnTemperature) / 2;

        /// <summary>
        /// Перепад температур
        /// </summary>
        public double TemperatureDelta => SupplyTemperature - ReturnTemperature;

        /// <summary>
        /// Общие потери давления (кПа)
        /// </summary>
        public double TotalPressureLossKPa => Result?.TotalPressureLoss / 1000 ?? 0;

        /// <summary>
        /// Общие потери давления (мбар)
        /// </summary>
        public double TotalPressureLossMbar => Result?.TotalPressureLoss / 100 ?? 0;

        #endregion

        #region Commands

        /// <summary>
        /// Команда расчёта
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCalculate))]
        private async Task CalculateAsync()
        {
            try
            {
                IsCalculating = true;
                HasErrors = false;
                Warnings.Clear();

                // Получение свойств гликоли
                var glycolProps = _glycolService.GetProperties(GlycolType, GlycolConcentration, MeanTemperature);

                // Формирование параметров расчёта
                var parameters = new HydraulicParameters
                {
                    CircuitLength = CircuitLength,
                    SupplyLength = SupplyLength,
                    GlycolConcentration = GlycolConcentration,
                    GlycolType = GlycolType,
                    SupplyTemperature = SupplyTemperature,
                    ReturnTemperature = ReturnTemperature,
                    Pipe = SelectedPipe,
                    Roughness = Roughness,
                    VolumeFlowRate = VolumeFlowRate,
                    CircuitArea = CircuitArea,
                    Density = glycolProps.Density,
                    KinematicViscosity = glycolProps.KinematicViscosity
                };

                // Выполнение расчёта
                Result = _hydraulicCalculator.Calculate(parameters);

                // Обработка результата
                if (!Result.IsValid)
                {
                    HasErrors = true;
                    ErrorMessage = string.Join("\n", Result.ValidationErrors);
                }
                else
                {
                    // Добавление предупреждений
                    foreach (var warning in Result.Warnings)
                    {
                        Warnings.Add(warning);
                    }
                }

                // Подбор коллектора
                await SelectCollectorAsync();
            }
            catch (Exception ex)
            {
                HasErrors = true;
                ErrorMessage = $"Ошибка расчёта: {ex.Message}";
            }
            finally
            {
                IsCalculating = false;
            }
        }

        /// <summary>
        /// Команда сброса
        /// </summary>
        [RelayCommand]
        private void Reset()
        {
            CircuitLength = 100;
            SupplyLength = 10;
            GlycolConcentration = 50;
            GlycolType = GlycolType.Ethylene;
            SelectedPipe = new PipeType { OuterDiameter = 20, WallThickness = 2, Name = "RAUTHERM S 20x2.0" };
            Roughness = 0.007;
            Result = null;
            HasErrors = false;
            ErrorMessage = string.Empty;
            Warnings.Clear();
        }

        /// <summary>
        /// Команда добавления контура
        /// </summary>
        [RelayCommand]
        private void AddCircuit()
        {
            var newCircuit = new CircuitViewModel
            {
                CircuitNumber = Circuits.Count + 1,
                CircuitName = $"Контур {Circuits.Count + 1}",
                Length = CircuitLength,
                SupplyLength = SupplyLength,
                Area = CircuitArea
            };

            Circuits.Add(newCircuit);
        }

        /// <summary>
        /// Команда удаления контура
        /// </summary>
        [RelayCommand]
        private void RemoveCircuit(CircuitViewModel circuit)
        {
            if (circuit != null && Circuits.Contains(circuit))
            {
                Circuits.Remove(circuit);

                // Перенумерация контуров
                for (int i = 0; i < Circuits.Count; i++)
                {
                    Circuits[i].CircuitNumber = i + 1;
                }
            }
        }

        /// <summary>
        /// Команда балансировки контуров
        /// </summary>
        [RelayCommand]
        private async Task BalanceCircuitsAsync()
        {
            if (Circuits.Count == 0)
                return;

            try
            {
                IsCalculating = true;

                // Расчёт для каждого контура
                var circuitResults = new List<CircuitResult>();

                foreach (var circuit in Circuits)
                {
                    var glycolProps = _glycolService.GetProperties(GlycolType, GlycolConcentration, MeanTemperature);

                    var parameters = new HydraulicParameters
                    {
                        CircuitLength = circuit.Length,
                        SupplyLength = circuit.SupplyLength,
                        GlycolConcentration = GlycolConcentration,
                        GlycolType = GlycolType,
                        SupplyTemperature = SupplyTemperature,
                        ReturnTemperature = ReturnTemperature,
                        Pipe = SelectedPipe,
                        Roughness = Roughness,
                        VolumeFlowRate = circuit.FlowRate,
                        CircuitArea = circuit.Area,
                        Density = glycolProps.Density,
                        KinematicViscosity = glycolProps.KinematicViscosity
                    };

                    var result = _hydraulicCalculator.Calculate(parameters);

                    circuitResults.Add(new CircuitResult
                    {
                        CircuitNumber = circuit.CircuitNumber,
                        CircuitName = circuit.CircuitName,
                        TotalPressureLoss = result.TotalPressureLoss,
                        Velocity = result.Velocity,
                        ReynoldsNumber = result.ReynoldsNumber,
                        FlowRegime = result.FlowRegime
                    });
                }

                // Балансировка
                var balancedResults = _hydraulicCalculator.CalculateBalancing(circuitResults);

                // Обновление контуров
                for (int i = 0; i < Circuits.Count; i++)
                {
                    var balanced = balancedResults.FirstOrDefault(r => r.CircuitNumber == Circuits[i].CircuitNumber);
                    if (balanced != null)
                    {
                        Circuits[i].Throttling = balanced.Throttling;
                        Circuits[i].ValveSetting = balanced.RecommendedValveSetting;
                        Circuits[i].IsReferenceCircuit = balanced.IsReferenceCircuit;
                    }
                }
            }
            catch (Exception ex)
            {
                HasErrors = true;
                ErrorMessage = $"Ошибка балансировки: {ex.Message}";
            }
            finally
            {
                IsCalculating = false;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор для дизайнера
        /// </summary>
        public HydraulicsViewModel() : this(null, null, null)
        {
        }

        /// <summary>
        /// Основной конструктор
        /// </summary>
        public HydraulicsViewModel(
            IHydraulicCalculator hydraulicCalculator,
            IGlycolDataService glycolService,
            ICollectorRepository collectorRepository)
        {
            _hydraulicCalculator = hydraulicCalculator ?? new HydraulicCalculator(new GlycolDataService());
            _glycolService = glycolService ?? new GlycolDataService();
            _collectorRepository = collectorRepository ?? new CollectorRepository();
            _validator = new HydraulicValidator();

            // Загрузка коллекторов
            LoadCollectorsAsync().ConfigureAwait(false);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Загрузка коллекторов
        /// </summary>
        private async Task LoadCollectorsAsync()
        {
            try
            {
                var collectors = await _collectorRepository.GetAllAsync();
                AvailableCollectors.Clear();
                foreach (var collector in collectors)
                {
                    AvailableCollectors.Add(collector);
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки загрузки
            }
        }

        /// <summary>
        /// Подбор коллектора
        /// </summary>
        private async Task SelectCollectorAsync()
        {
            if (Circuits.Count == 0)
                return;

            double totalFlowRate = Circuits.Sum(c => c.FlowRate);
            int circuitCount = Circuits.Count;

            SelectedCollector = await _collectorRepository.SelectCollectorAsync(
                CollectorType.HKV,
                circuitCount,
                totalFlowRate);
        }

        #endregion

        #region PropertyChanged Handlers

        /// <summary>
        /// Обработчик изменения длины контура
        /// </summary>
        partial void OnCircuitLengthChanged(double value)
        {
            CalculateCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Обработчик изменения длины подводки
        /// </summary>
        partial void OnSupplyLengthChanged(double value)
        {
            CalculateCommand.NotifyCanExecuteChanged();
        }

        #endregion
    }
}
```

### 3.2. CircuitResult.cs

**Путь:** `src/Models/Hydraulics/CircuitResult.cs`

```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Результат расчёта контура для балансировки
    /// </summary>
    public class CircuitResult
    {
        /// <summary>
        /// Номер контура
        /// </summary>
        public int CircuitNumber { get; set; }

        /// <summary>
        /// Название контура
        /// </summary>
        public string CircuitName { get; set; }

        /// <summary>
        /// Общие потери давления (Па)
        /// </summary>
        public double TotalPressureLoss { get; set; }

        /// <summary>
        /// Скорость потока (м/с)
        /// </summary>
        public double Velocity { get; set; }

        /// <summary>
        /// Число Рейнольдса
        /// </summary>
        public double ReynoldsNumber { get; set; }

        /// <summary>
        /// Режим течения
        /// </summary>
        public FlowRegime FlowRegime { get; set; }

        /// <summary>
        /// Дросселирование (Па)
        /// </summary>
        public double Throttling { get; set; }

        /// <summary>
        /// Рекомендуемая настройка вентиля (1-8)
        /// </summary>
        public int RecommendedValveSetting { get; set; }

        /// <summary>
        /// Признак опорного контура (с максимальными потерями)
        /// </summary>
        public bool IsReferenceCircuit { get; set; }
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/ViewModels/Hydraulics/HydraulicsViewModelTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.ViewModels.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;
using NUnit.Framework;
using Moq;
using System.Threading.Tasks;

namespace SnowMeltingCalculator.Tests.ViewModels.Hydraulics
{
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

            _viewModel = new HydraulicsViewModel(
                _calculatorMock.Object,
                _glycolServiceMock.Object,
                _collectorRepositoryMock.Object);
        }

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
        public void Reset_ResetsToDefaultValues()
        {
            // Arrange
            _viewModel.CircuitLength = 200;
            _viewModel.SupplyLength = 20;
            _viewModel.GlycolConcentration = 30;

            // Act
            _viewModel.ResetCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.CircuitLength, Is.EqualTo(100));
            Assert.That(_viewModel.SupplyLength, Is.EqualTo(10));
            Assert.That(_viewModel.GlycolConcentration, Is.EqualTo(50));
        }

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
        public void RemoveCircuit_RemovesCircuit()
        {
            // Arrange
            _viewModel.AddCircuitCommand.Execute(null);
            _viewModel.AddCircuitCommand.Execute(null);

            // Act
            _viewModel.RemoveCircuitCommand.Execute(_viewModel.Circuits[0]);

            // Assert
            Assert.That(_viewModel.Circuits.Count, Is.EqualTo(1));
            Assert.That(_viewModel.Circuits[0].CircuitNumber, Is.EqualTo(1));
        }

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
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `HydraulicsViewModel.cs` создан
- [ ] Реализованы все свойства и команды
- [ ] MVVM паттерн (CommunityToolkit.Mvvm)
- [ ] Интеграция с ThermalModule работает
- [ ] Команды Calculate, Reset, AddCircuit, RemoveCircuit работают
- [ ] Балансировка контуров работает
- [ ] Unit-тесты проходят успешно
- [ ] XML-документация для всех методов

---

## 6. Примечания

- Используется CommunityToolkit.Mvvm для MVVM
- Поддержка нескольких контуров через ObservableCollection
- Автоматический подбор коллектора
- Интеграция с ThermalModule через события