using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Repositories.Hydraulics;

namespace SnowMeltingCalculator.ViewModels.Hydraulics
{
    /// <summary>
    /// Основная ViewModel для модуля гидравлики
    /// </summary>
    /// <remarks>
    /// Предоставляет функционал для:
    /// - Гидравлического расчёта контуров
    /// - Балансировки нескольких контуров
    /// - Подбора коллектора
    /// - Интеграции с ThermalModule
    /// </remarks>
    public partial class HydraulicsViewModel : ObservableObject, IDisposable
    {
        #region Services

        private readonly IHydraulicCalculator _hydraulicCalculator;
        private readonly IGlycolDataService _glycolService;
        private readonly ICollectorRepository _collectorRepository;
        private readonly HydraulicValidator _validator;
        private readonly IThermalCalculationResult? _thermalResult;
        private bool _disposed;

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
        private HydraulicResult? _result;

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
        private string _errorMessage = string.Empty;

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
        private Collector? _selectedCollector;

        /// <summary>
        /// Список доступных коллекторов
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Collector> _availableCollectors = new ObservableCollection<Collector>();

        /// <summary>
        /// Список доступных труб
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Models.Thermal.PipeType> _availablePipes = new ObservableCollection<Models.Thermal.PipeType>(
            Models.Thermal.PipeType.StandardPipes);

        #endregion

        #region Computed Properties

        /// <summary>
        /// Признак возможности расчёта
        /// </summary>
        public bool CanCalculate => !IsCalculating && CircuitLength > 0 && SupplyLength > 0;

        /// <summary>
        /// Признак наличия предупреждений
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;

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
                Result = await Task.Run(() => _hydraulicCalculator.Calculate(parameters));

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
                var circuitResults = new System.Collections.Generic.List<CircuitResult>();

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
                        HydraulicResult = result
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
        public HydraulicsViewModel() : this(null, null, null, null)
        {
        }

        /// <summary>
        /// Основной конструктор
        /// </summary>
        /// <param name="hydraulicCalculator">Калькулятор гидравлики</param>
        /// <param name="glycolService">Сервис свойств гликолей</param>
        /// <param name="collectorRepository">Репозиторий коллекторов</param>
        /// <param name="thermalResult">Результат теплового расчёта (для интеграции)</param>
        public HydraulicsViewModel(
            IHydraulicCalculator? hydraulicCalculator,
            IGlycolDataService? glycolService,
            ICollectorRepository? collectorRepository,
            IThermalCalculationResult? thermalResult)
        {
            _hydraulicCalculator = hydraulicCalculator ?? new HydraulicCalculator(new GlycolDataService());
            _glycolService = glycolService ?? new GlycolDataService();
            _collectorRepository = collectorRepository ?? new CollectorRepository();
            _validator = new HydraulicValidator();
            _thermalResult = thermalResult;

            // Подписка на событие изменения результата теплового расчёта
            if (_thermalResult != null)
            {
                _thermalResult.ResultChanged += OnThermalResultChanged;
            }

            // Загрузка коллекторов
            _ = LoadCollectorsAsync();
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

            // Преобразование л/ч в м³/ч
            double totalFlowRate_m3_h = totalFlowRate / 1000.0;

            SelectedCollector = _collectorRepository.SelectCollector(
                circuitCount,
                totalFlowRate_m3_h);
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

        #region ThermalModule Integration

        /// <summary>
        /// Обработчик события изменения результата теплового расчёта
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Аргументы события</param>
        private void OnThermalResultChanged(object? sender, ThermalResultChangedEventArgs e)
        {
            if (e.Result == null || !e.Result.IsValid)
                return;

            // Обновление параметров из теплового расчёта
            VolumeFlowRate = e.Result.VolumeFlowRate;
            SupplyTemperature = e.Result.SupplyTemperature;
            ReturnTemperature = e.Result.ReturnTemperature;

            // Автоматический перерасчёт при изменении данных
            if (CanCalculate)
            {
                _ = CalculateAsync();
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Освободить ресурсы
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Освободить ресурсы
        /// </summary>
        /// <param name="disposing">true, если вызван из Dispose()</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Отписка от события
                if (_thermalResult != null)
                {
                    _thermalResult.ResultChanged -= OnThermalResultChanged;
                }
            }

            _disposed = true;
        }

        #endregion
    }
}