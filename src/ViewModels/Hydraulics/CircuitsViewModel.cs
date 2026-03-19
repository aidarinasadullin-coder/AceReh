using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.ViewModels.Climate;
using SnowMeltingCalculator.ViewModels.Thermal;

namespace SnowMeltingCalculator.ViewModels.Hydraulics
{
    public partial class CollectorData : ObservableObject
    {
        private int _collectorNumber;
        public int CollectorNumber
        {
            get => _collectorNumber;
            set => SetProperty(ref _collectorNumber, value);
        }

        [ObservableProperty]
        private ObservableCollection<CircuitRow> _circuits = new();

        [ObservableProperty]
        private CollectorSummary _summary = new();

        private string _collectorType = "HKV-D (2-12 контуров)";
        public string CollectorType
        {
            get => _collectorType;
            set
            {
                if (SetProperty(ref _collectorType, value))
                {
                    // Автоматически обновляем тип клапана при изменении типа коллектора
                    ValveType = value switch
                    {
                        "HKV-D (2-12 контуров)" => ValveType.HKV_D,
                        "IV 1¼\" (2-12 контуров)" => ValveType.IV_1_25,
                        "IV 1½\" (2-12 контуров)" => ValveType.IV_1_5,
                        _ => ValveType.HKV_D
                    };
                }
            }
        }

        [ObservableProperty]
        private ValveType _valveType = ValveType.HKV_D;

        public CollectorData(int collectorNumber)
        {
            CollectorNumber = collectorNumber;
        }
    }

    public partial class CircuitsViewModel : ObservableObject
    {
        #region Services

        private readonly ICircuitsCalculator _circuitsCalculator;
        private readonly IGlycolDataService _glycolService;
        private readonly ThermalViewModel _thermalViewModel;
        private readonly ClimateViewModel _climateViewModel;

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<CollectorData> _collectors = new();

        [ObservableProperty]
        private int _selectedCollectorIndex = 0;

        private HydraulicMode _currentMode = HydraulicMode.OperatingTemperature;
        public HydraulicMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (SetProperty(ref _currentMode, value))
                {
                    UpdateCircuitDisplayMode();
                }
            }
        }

        [ObservableProperty]
        private GlycolType _glycolType = GlycolType.Ethylene;

        [ObservableProperty]
        private double _glycolConcentration = 50.0;

        [ObservableProperty]
        private GlycolProperties _operatingGlycolProperties = new();

        [ObservableProperty]
        private GlycolProperties _designGlycolProperties = new();

        public bool CanAddCollector => Collectors.Count < 4;

        public bool CanAddCircuit => SelectedCollector != null && SelectedCollector.Circuits.Count < 12;

        /// <summary>
        /// Входные данные для гидравлического расчёта
        /// </summary>
        /// <remarks>
        /// Заполняются из ThermalModule при изменении результата теплового расчёта
        /// </remarks>
        [ObservableProperty]
        private HydraulicInputData _inputData = new();

        #endregion

        #region Computed Properties

        public CollectorData? SelectedCollector =>
            SelectedCollectorIndex >= 0 && SelectedCollectorIndex < Collectors.Count
                ? Collectors[SelectedCollectorIndex]
                : null;

        /// <summary>
        /// Расчётная температура (температура холодной пятидневки)
        /// </summary>
        /// <remarks>
        /// Используется для расчёта при "холодном пуске".
        /// Берётся из ClimateViewModel.ColdFiveDayTemperature.
        /// </remarks>
        public double DesignTemperature => _climateViewModel.ColdFiveDayTemperature;

        #endregion

        #region Commands

        [RelayCommand(CanExecute = nameof(CanAddCollector))]
        private void AddCollector()
        {
            var collectorNumber = Collectors.Count + 1;
            var collector = new CollectorData(collectorNumber)
            {
                ValveType = ValveType.HKV_D
            };

            for (int i = 0; i < 4; i++)
            {
                collector.Circuits.Add(new CircuitRow
                {
                    CircuitNumber = i + 1,
                    CircuitLength = 100,
                    SupplyLength = 10,
                    SupplySpacing_cm = 5,
                    SupplyHeatPercent = 10
                });
            }

            Collectors.Add(collector);
            SelectedCollectorIndex = Collectors.Count - 1;
            AddCollectorCommand.NotifyCanExecuteChanged();
            AddCircuitCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void RemoveCollector(CollectorData collector)
        {
            if (collector != null && Collectors.Contains(collector))
            {
                Collectors.Remove(collector);
                if (SelectedCollectorIndex >= Collectors.Count)
                {
                    SelectedCollectorIndex = Math.Max(0, Collectors.Count - 1);
                }
                AddCollectorCommand.NotifyCanExecuteChanged();
                AddCircuitCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddCircuit))]
        private void AddCircuit()
        {
            var collector = SelectedCollector;
            if (collector == null) return;

            var circuitNumber = collector.Circuits.Count + 1;
            collector.Circuits.Add(new CircuitRow
            {
                CircuitNumber = circuitNumber,
                CircuitLength = 100,
                SupplyLength = 10,
                SupplySpacing_cm = 5,
                SupplyHeatPercent = 10
            });

            AddCircuitCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void RemoveCircuit(CircuitRow circuit)
        {
            var collector = SelectedCollector;
            if (collector == null) return;

            if (circuit != null && collector.Circuits.Contains(circuit))
            {
                collector.Circuits.Remove(circuit);
                RenumberCircuits(collector);
                AddCircuitCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        private void Calculate()
        {
            var collector = SelectedCollector;
            if (collector == null) return;

            var input = InputData;
            if (input == null || input.InnerDiameter <= 0)
            {
                input = new HydraulicInputData
                {
                    InnerDiameter = 14.2,
                    SupplyTemperature = 35,
                    ReturnTemperature = 30,
                    PowerUp = 180,
                    PowerDown = 80,
                    ColdFiveDayTemperature = -28
                };
            }

            var kv = collector.ValveType switch
            {
                ValveType.HKV_D => 1.2,
                ValveType.IV_1_25 => 1.45,
                ValveType.IV_1_5 => 1.5,
                _ => 1.2
            };

            var operatingTemp = input.OperatingTemperature;
            var designTemp = input.DesignTemperature;

            var glycolOperating = _glycolService.GetProperties(GlycolType, GlycolConcentration, operatingTemp);
            var glycolDesign = _glycolService.GetProperties(GlycolType, GlycolConcentration, designTemp);

            OperatingGlycolProperties = glycolOperating;
            DesignGlycolProperties = glycolDesign;

            // Получить шаг укладки из ThermalViewModel (мм → см)
            double pipeSpacing_cm = _thermalViewModel.PipeSpacing / 10.0;

            foreach (var circuit in collector.Circuits)
            {
                if (circuit.CircuitLength <= 0) continue;

                var power = _circuitsCalculator.CalculateCircuitPower(circuit, input.PowerUp, input.PowerDown, pipeSpacing_cm);
                circuit.Power = power;

                var flowRate = _circuitsCalculator.CalculateFlowRate(power, 10.0, glycolOperating.Density, glycolOperating.SpecificHeat);
                circuit.FlowRate = flowRate;

                var operatingResult = _circuitsCalculator.CalculateAtTemperature(
                    circuit,
                    operatingTemp,
                    glycolOperating,
                    input.InnerDiameter,
                    kv
                );
                circuit.OperatingResult = operatingResult;

                var designResult = _circuitsCalculator.CalculateAtTemperature(
                    circuit,
                    designTemp,
                    glycolDesign,
                    input.InnerDiameter,
                    kv
                );
                circuit.DesignResult = designResult;

                circuit.DisplayMode = CurrentMode;
            }

            var summary = _circuitsCalculator.CalculateCollectorSummary(
                new System.Collections.Generic.List<CircuitRow>(collector.Circuits),
                collector.CollectorNumber,
                collector.ValveType
            );
            collector.Summary = summary;

            _circuitsCalculator.CalculateBalancing(
                new System.Collections.Generic.List<CircuitRow>(collector.Circuits),
                collector.ValveType
            );

            foreach (var circuit in collector.Circuits)
            {
                circuit.DisplayMode = CurrentMode;
            }
        }

        [RelayCommand]
        private void SwitchMode()
        {
            CurrentMode = CurrentMode == HydraulicMode.OperatingTemperature
                ? HydraulicMode.DesignTemperature
                : HydraulicMode.OperatingTemperature;
        }

        #endregion

        #region Constructor

        public CircuitsViewModel(
            ICircuitsCalculator circuitsCalculator,
            IGlycolDataService glycolService,
            ThermalViewModel thermalViewModel,
            ClimateViewModel climateViewModel)
        {
            _circuitsCalculator = circuitsCalculator ?? throw new ArgumentNullException(nameof(circuitsCalculator));
            _glycolService = glycolService ?? throw new ArgumentNullException(nameof(glycolService));
            _thermalViewModel = thermalViewModel ?? throw new ArgumentNullException(nameof(thermalViewModel));
            _climateViewModel = climateViewModel ?? throw new ArgumentNullException(nameof(climateViewModel));

            // Подписка на изменения результата теплового расчёта
            _thermalViewModel.PropertyChanged += OnThermalViewModelPropertyChanged;

            // Подписка на изменения климатических данных
            _climateViewModel.PropertyChanged += OnClimatePropertyChanged;

            AddCollector();
        }

        #endregion

        #region Private Methods

        private void RenumberCircuits(CollectorData collector)
        {
            for (int i = 0; i < collector.Circuits.Count; i++)
            {
                collector.Circuits[i].CircuitNumber = i + 1;
            }
        }

        private void UpdateCircuitDisplayMode()
        {
            foreach (var collector in Collectors)
            {
                foreach (var circuit in collector.Circuits)
                {
                    circuit.DisplayMode = CurrentMode;
                }
            }
        }

        /// <summary>
        /// Обработчик изменения свойств ThermalViewModel
        /// </summary>
        private void OnThermalViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ThermalViewModel.Result))
            {
                UpdateFromThermalModule();
            }
            else if (e.PropertyName == nameof(ThermalViewModel.PipeSpacing))
            {
                UpdatePipeSpacingInCircuits();
            }
        }
        
        /// <summary>
        /// Обновить шаг укладки во всех контурах
        /// </summary>
        private void UpdatePipeSpacingInCircuits()
        {
            var pipeSpacing_cm = _thermalViewModel.PipeSpacing / 10.0;
            
            foreach (var collector in Collectors)
            {
                foreach (var circuit in collector.Circuits)
                {
                    circuit.PipeSpacing_cm = pipeSpacing_cm;
                }
            }
        }

        /// <summary>
        /// Обновить данные из ThermalModule
        /// </summary>
        /// <remarks>
        /// Вызывается при изменении ThermalViewModel.Result.
        /// Заполняет InputData данными из теплового расчёта.
        /// </remarks>
        public void UpdateFromThermalModule()
        {
            var thermalResult = _thermalViewModel.Result;
            if (thermalResult == null || !thermalResult.IsValid)
            {
                // Сбросить данные, если результат невалиден
                InputData = new HydraulicInputData();
                return;
            }

            // Обновить данные из ThermalResult
            InputData.PowerUp = thermalResult.PowerUp;
            InputData.PowerDown = thermalResult.PowerDown;
            InputData.SupplyTemperature = thermalResult.SupplyTemperature;
            InputData.ReturnTemperature = thermalResult.ReturnTemperature;

            // Получить внутренний диаметр трубы из выбранной трубы
            var selectedPipe = _thermalViewModel.SelectedPipe;
            if (selectedPipe != null)
            {
                InputData.InnerDiameter = selectedPipe.InnerDiameter;
            }

            // Сохранить текущие настройки гликоля
            InputData.GlycolType = GlycolType;
            InputData.GlycolConcentration = GlycolConcentration;

            // Выполнить расчёт после обновления данных
            Calculate();
        }

        /// <summary>
        /// Обработчик изменения свойств ClimateViewModel
        /// </summary>
        private void OnClimatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ClimateViewModel.ColdFiveDayTemperature))
            {
                UpdateFromClimateModule();
            }
        }

        /// <summary>
        /// Обновить данные из ClimateModule
        /// </summary>
        /// <remarks>
        /// Вызывается при изменении ClimateViewModel.ColdFiveDayTemperature.
        /// Обновляет InputData.ColdFiveDayTemperature.
        /// </remarks>
        public void UpdateFromClimateModule()
        {
            // Температура холодной пятидневки
            InputData.ColdFiveDayTemperature = _climateViewModel.ColdFiveDayTemperature;

            // Выполнить расчёт после обновления данных
            Calculate();
        }

        #endregion

        #region Property Changed Handlers

        /// <summary>
        /// Обработчик изменения типа гликоля
        /// </summary>
        partial void OnGlycolTypeChanged(GlycolType value)
        {
            InputData.GlycolType = value;
            // Автоматически пересчитываем при изменении типа гликоля
            Calculate();
        }

        /// <summary>
        /// Обработчик изменения концентрации гликоля
        /// </summary>
        partial void OnGlycolConcentrationChanged(double value)
        {
            InputData.GlycolConcentration = value;
            // Автоматически пересчитываем при изменении концентрации
            Calculate();
        }

        #endregion
    }
}