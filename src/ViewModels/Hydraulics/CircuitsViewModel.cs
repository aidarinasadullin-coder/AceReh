using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
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
                    // Уведомить об изменении отображаемого типа с количеством контуров
                    OnPropertyChanged(nameof(CollectorTypeDisplayWithCount));
                }
            }
        }

        [ObservableProperty]
        private ValveType _valveType = ValveType.HKV_D;

        /// <summary>
        /// Отображаемое название типа коллектора с фактическим количеством контуров
        /// </summary>
        /// <remarks>
        /// Формат: "HKV-D (3 контура)", "IV 1¼\" (5 контуров)", "IV 1½\" (8 контуров)"
        /// </remarks>
        public string CollectorTypeDisplayWithCount
        {
            get
            {
                string typeName = ValveType switch
                {
                    ValveType.HKV_D => "HKV-D",
                    ValveType.IV_1_25 => "IV 1¼\"",
                    ValveType.IV_1_5 => "IV 1½\"",
                    _ => "Unknown"
                };

                int count = Circuits.Count;
                string countText = count switch
                {
                    1 => "1 контур",
                    2 or 3 or 4 => $"{count} контура",
                    _ => $"{count} контуров"
                };

                return $"{typeName} ({countText})";
            }
        }

        public CollectorData(int collectorNumber)
        {
            CollectorNumber = collectorNumber;
            // Подписка на изменение коллекции контуров для обновления отображаемого типа
            Circuits.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CollectorTypeDisplayWithCount));
        }

        /// <summary>
        /// Обработчик изменения типа клапана
        /// </summary>
        partial void OnValveTypeChanged(ValveType value)
        {
            OnPropertyChanged(nameof(CollectorTypeDisplayWithCount));
        }
    }

    public partial class CircuitsViewModel : ObservableObject
    {
        #region Services

        private readonly ICircuitsCalculator _circuitsCalculator;
        private readonly IGlycolDataService _glycolService;
        private readonly ThermalViewModel _thermalViewModel;
        private readonly ClimateViewModel _climateViewModel;
        private readonly ICalculationStateService _calculationStateService;

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<CollectorData> _collectors = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedCollector))]
        [NotifyPropertyChangedFor(nameof(Summary))]
        [NotifyPropertyChangedFor(nameof(CollectorTypeDisplay))]
        [NotifyPropertyChangedFor(nameof(KvValue))]
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
                    // DpVent больше не нужно пересчитывать при переключении режима,
                    // так как он уже рассчитан с дефолтным Kv в CalculateAtTemperature()
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
        /// Выбранный контур в DataGrid
        /// </summary>
        /// <remarks>
        /// Используется для команды удаления контура.
        /// Привязан к SelectedItem DataGrid.
        /// </remarks>
        [ObservableProperty]
        private CircuitRow? _selectedCircuit;

        /// <summary>
        /// Входные данные для гидравлического расчёта
        /// </summary>
        /// <remarks>
        /// Заполняются из ThermalModule при изменении результата теплового расчёта
        /// </remarks>
        [ObservableProperty]
        private HydraulicInputData _inputData = new();

        #endregion

        #region Calculation State

        /// <summary>
        /// Признак того, что гидравлический расчёт выполняется
        /// </summary>
        public bool IsCalculating => _calculationStateService.HydraulicsIsCalculating;

        #endregion

        #region Computed Properties

        public CollectorData? SelectedCollector =>
            SelectedCollectorIndex >= 0 && SelectedCollectorIndex < Collectors.Count
                ? Collectors[SelectedCollectorIndex]
                : null;

        // === Свойства для блока "Входные данные" ===

        /// <summary>
        /// Температура подачи, °C
        /// </summary>
        /// <remarks>
        /// Берётся из InputData.SupplyTemperature (из ThermalViewModel.Result)
        /// </remarks>
        public double SupplyTemperature => InputData.SupplyTemperature;

        /// <summary>
        /// Температура обратки, °C
        /// </summary>
        /// <remarks>
        /// Берётся из InputData.ReturnTemperature (из ThermalViewModel.Result)
        /// </remarks>
        public double ReturnTemperature => InputData.ReturnTemperature;

        /// <summary>
        /// Тип трубы (наименование)
        /// </summary>
        /// <remarks>
        /// Берётся из ThermalViewModel.SelectedPipe.Name
        /// </remarks>
        public string PipeType => _thermalViewModel.SelectedPipe?.Name ?? "Труба не выбрана";

        /// <summary>
        /// Наружный диаметр трубы, мм
        /// </summary>
        /// <remarks>
        /// Берётся из ThermalViewModel.SelectedPipe.OuterDiameter
        /// </remarks>
        public double OuterDiameter => _thermalViewModel.SelectedPipe?.OuterDiameter ?? 0;

        /// <summary>
        /// Толщина стенки трубы, мм
        /// </summary>
        /// <remarks>
        /// Берётся из ThermalViewModel.SelectedPipe.WallThickness
        /// </remarks>
        public double WallThickness => _thermalViewModel.SelectedPipe?.WallThickness ?? 0;

        /// <summary>
        /// Внутренний диаметр трубы, мм
        /// </summary>
        /// <remarks>
        /// Берётся из InputData.InnerDiameter
        /// </remarks>
        public double InnerDiameter => InputData.InnerDiameter;

        /// <summary>
        /// Шероховатость трубы, мм (константа для PE-Xa)
        /// </summary>
        public double PipeRoughness => 0.007;

        /// <summary>
        /// Тип гликоля (на русском)
        /// </summary>
        public string GlycolTypeName => GlycolType switch
        {
            GlycolType.Ethylene => "Этиленгликоль",
            GlycolType.Propylene => "Пропиленгликоль",
            _ => "Не указан"
        };

        /// <summary>
        /// Расчётная температура (М10/М15/М20)
        /// </summary>
        /// <remarks>
        /// Используется для расчёта при "холодном пуске".
        /// Берётся из ClimateViewModel.AirTemperature.
        /// </remarks>
        public double DesignTemperature => _climateViewModel.AirTemperature;

        /// <summary>
        /// Рабочая температура теплоносителя, °C
        /// </summary>
        public double OperatingTemperatureValue => InputData.OperatingTemperature;

        /// <summary>
        /// Расчётная температура наружного воздуха, °C (М10/М15/М20)
        /// </summary>
        public double DesignTemperatureValue => DesignTemperature;

        /// <summary>
        /// Текст кнопки для режима рабочей температуры
        /// </summary>
        public string OperatingModeButtonText => $"Рабочая температура: {OperatingTemperatureValue:F1}°C";

        /// <summary>
        /// Текст кнопки для режима расчётной температуры
        /// </summary>
        public string DesignModeButtonText => $"Расчётная температура: {DesignTemperatureValue:F1}°C";

        // === Свойства для блока "Данные укладки и мощности" ===

        /// <summary>
        /// Удельная мощность вверх, Вт/м²
        /// </summary>
        /// <remarks>
        /// Берётся из InputData.PowerUp (из ThermalViewModel.Result.PowerUp)
        /// </remarks>
        public double PowerUp => InputData.PowerUp;

        /// <summary>
        /// Удельная мощность вниз, Вт/м²
        /// </summary>
        /// <remarks>
        /// Берётся из InputData.PowerDown (из ThermalViewModel.Result.PowerDown)
        /// </remarks>
        public double PowerDown => InputData.PowerDown;

        /// <summary>
        /// Шаг укладки, см
        /// </summary>
        /// <remarks>
        /// Берётся из ThermalViewModel.PipeSpacing (мм) / 10
        /// </remarks>
        public double PipeSpacing_cm => _thermalViewModel.PipeSpacing / 10.0;

        /// <summary>
        /// Шаг подводки, см
        /// </summary>
        public double SupplySpacing_cm => InputData.SupplySpacing_cm;

        /// <summary>
        /// Доля потерь в подводке, %
        /// </summary>
        public double SupplyHeatPercent => InputData.SupplyHeatPercent;

        // === Свойства для блока "Результаты коллектора" ===

        /// <summary>
        /// Итоги коллектора для отображения
        /// </summary>
        public CollectorSummary? Summary => SelectedCollector?.Summary;

        /// <summary>
        /// Тип коллектора для отображения (с фактическим количеством контуров)
        /// </summary>
        public string CollectorTypeDisplay => SelectedCollector?.CollectorTypeDisplayWithCount ?? "—";

        /// <summary>
        /// Kv клапана для отображения
        /// </summary>
        public double KvValue => SelectedCollector?.Summary?.Kv ?? 0;

        #endregion

        #region Commands

        [RelayCommand(CanExecute = nameof(CanAddCollector))]
        private void AddCollector()
        {
            // Проверка лимита коллекторов (максимум 4)
            if (Collectors.Count >= 4)
            {
                return;
            }

            var collectorNumber = Collectors.Count + 1;
            var collector = new CollectorData(collectorNumber)
            {
                ValveType = ValveType.HKV_D
            };

            for (int i = 0; i < 2; i++)
            {
                collector.Circuits.Add(new CircuitRow
                {
                    CircuitNumber = i + 1,
                    CircuitLength = 0,
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

        [RelayCommand(CanExecute = nameof(CanRemoveCollector))]
        private void RemoveCollector(CollectorData collector)
        {
            if (collector == null)
                return;

            if (!ConfirmDeleteCollector(collector.CollectorNumber))
                return;

            if (Collectors.Contains(collector))
            {
                Collectors.Remove(collector);
                RenumberCollectors();
                if (SelectedCollectorIndex >= Collectors.Count)
                {
                    SelectedCollectorIndex = Math.Max(0, Collectors.Count - 1);
                }
                AddCollectorCommand.NotifyCanExecuteChanged();
                AddCircuitCommand.NotifyCanExecuteChanged();
                RemoveCollectorCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddCircuit))]
        private void AddCircuit()
        {
            var collector = SelectedCollector;
            if (collector == null) return;

            // Проверка лимита контуров (максимум 12)
            if (collector.Circuits.Count >= 12)
            {
                return;
            }

            var circuitNumber = collector.Circuits.Count + 1;
            collector.Circuits.Add(new CircuitRow
            {
                CircuitNumber = circuitNumber,
                CircuitLength = 0,
                SupplyLength = 10,
                SupplySpacing_cm = 5,
                SupplyHeatPercent = 10
            });

            AddCircuitCommand.NotifyCanExecuteChanged();
            
            // === Пересчитать гидравлику после добавления контура ===
            // Это необходимо для обновления референсного контура и балансировки
            Calculate();
        }

        [RelayCommand(CanExecute = nameof(CanRemoveCircuit))]
        private void RemoveCircuit(CircuitRow circuit)
        {
            if (circuit == null)
                return;

            if (!ConfirmDeleteCircuit(circuit.CircuitNumber))
                return;

            var collector = SelectedCollector;
            if (collector == null) return;

            if (collector.Circuits.Contains(circuit))
            {
                collector.Circuits.Remove(circuit);
                RenumberCircuits(collector);
                AddCircuitCommand.NotifyCanExecuteChanged();
                RemoveCircuitCommand.NotifyCanExecuteChanged();
                
                // === Пересчитать гидравлику после удаления контура ===
                // Это необходимо для обновления референсного контура и балансировки
                Calculate();
            }
        }

        [RelayCommand]
        private void Calculate()
        {
            // Установить флаг выполнения расчёта
            _calculationStateService.SetHydraulicsCalculating();

            try
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

                var operatingTemp = input.OperatingTemperature;
                var designTemp = _climateViewModel.AirTemperature;

                var glycolOperating = _glycolService.GetProperties(GlycolType, GlycolConcentration, operatingTemp);
                var glycolDesign = _glycolService.GetProperties(GlycolType, GlycolConcentration, designTemp);

                OperatingGlycolProperties = glycolOperating;
                DesignGlycolProperties = glycolDesign;

                // Получить шаг укладки из ThermalViewModel (мм → см)
                double pipeSpacing_cm = _thermalViewModel.PipeSpacing / 10.0;

                // === ЭТАП 1: Рассчитать FlowRate для всех контуров (не зависит от kv) ===
                foreach (var circuit in collector.Circuits)
                {
                    if (circuit.CircuitLength <= 0) continue;

                    var power = _circuitsCalculator.CalculateCircuitPower(circuit, input.PowerUp, input.PowerDown, pipeSpacing_cm);
                    circuit.Power = power;

                    var flowRate = _circuitsCalculator.CalculateFlowRate(power, input.DeltaT, glycolOperating.Density, glycolOperating.SpecificHeat);
                    circuit.FlowRate = flowRate;
                }

                // === ЭТАП 2: Вычислить summary для определения типа коллектора ===
                var summary = _circuitsCalculator.CalculateCollectorSummary(
                    new System.Collections.Generic.List<CircuitRow>(collector.Circuits),
                    collector.CollectorNumber,
                    collector.ValveType
                );
                collector.Summary = summary;

                // === ЭТАП 3: Автоматический выбор типа коллектора по расходу ===
                // ВАЖНО: Вызывается ДО вычисления kv, чтобы тип коллектора был правильным
                AutoSelectCollectorType();

                // === ЭТАП 4: Вычислить kv для правильного типа коллектора ===
                var kv = collector.ValveType switch
                {
                    ValveType.HKV_D => 1.2,
                    ValveType.IV_1_25 => 1.45,
                    ValveType.IV_1_5 => 1.5,
                    _ => 1.2
                };

                // === ЭТАП 5: Рассчитать OperatingResult и DesignResult с правильным kv ===
                foreach (var circuit in collector.Circuits)
                {
                    if (circuit.CircuitLength <= 0) continue;

                    var operatingResult = _circuitsCalculator.CalculateAtTemperature(
                        circuit,
                        operatingTemp,
                        glycolOperating,
                        input.InnerDiameter,
                        kv,
                        collector.ValveType
                    );
                    circuit.OperatingResult = operatingResult;

                    var designResult = _circuitsCalculator.CalculateAtTemperature(
                        circuit,
                        designTemp,
                        glycolDesign,
                        input.InnerDiameter,
                        kv,
                        collector.ValveType
                    );
                    circuit.DesignResult = designResult;

                    circuit.DisplayMode = CurrentMode;
                }

                // === ЭТАП 6: Обновить summary с новыми результатами ===
                summary = _circuitsCalculator.CalculateCollectorSummary(
                    new System.Collections.Generic.List<CircuitRow>(collector.Circuits),
                    collector.CollectorNumber,
                    collector.ValveType
                );
                collector.Summary = summary;

                // === ЭТАП 7: Балансировка ===
                _circuitsCalculator.CalculateBalancing(
                    new System.Collections.Generic.List<CircuitRow>(collector.Circuits),
                    collector.ValveType
                );

                foreach (var circuit in collector.Circuits)
                {
                    circuit.DisplayMode = CurrentMode;
                }
            }
            finally
            {
                // Сбросить состояние после расчёта
                _calculationStateService.ResetHydraulicsState();
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
            ClimateViewModel climateViewModel,
            ICalculationStateService calculationStateService)
        {
            _circuitsCalculator = circuitsCalculator ?? throw new ArgumentNullException(nameof(circuitsCalculator));
            _glycolService = glycolService ?? throw new ArgumentNullException(nameof(glycolService));
            _thermalViewModel = thermalViewModel ?? throw new ArgumentNullException(nameof(thermalViewModel));
            _climateViewModel = climateViewModel ?? throw new ArgumentNullException(nameof(climateViewModel));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));

            // Подписка на изменения состояния расчёта
            _calculationStateService.StateChanged += OnCalculationStateChanged;

            // Подписка на изменения результата теплового расчёта
            _thermalViewModel.PropertyChanged += OnThermalViewModelPropertyChanged;

            // Подписка на изменения климатических данных
            _climateViewModel.PropertyChanged += OnClimatePropertyChanged;

            // Подписка на изменения InputData для обновления свойств укладки
            InputData.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HydraulicInputData.SupplySpacing_cm))
                {
                    OnPropertyChanged(nameof(SupplySpacing_cm));
                    // Синхронизировать значения во всех контурах
                    foreach (var collector in Collectors)
                    {
                        foreach (var circuit in collector.Circuits)
                        {
                            circuit.SupplySpacing_cm = InputData.SupplySpacing_cm;
                        }
                    }
                    Calculate();
                }
                if (e.PropertyName == nameof(HydraulicInputData.SupplyHeatPercent))
                {
                    OnPropertyChanged(nameof(SupplyHeatPercent));
                    // Синхронизировать значения во всех контурах
                    foreach (var collector in Collectors)
                    {
                        foreach (var circuit in collector.Circuits)
                        {
                            circuit.SupplyHeatPercent = InputData.SupplyHeatPercent;
                        }
                    }
                    Calculate();
                }
            };

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

        private void RenumberCollectors()
        {
            for (int i = 0; i < Collectors.Count; i++)
            {
                Collectors[i].CollectorNumber = i + 1;
            }
        }

        /// <summary>
        /// Диалоговое окно подтверждения удаления контура
        /// </summary>
        /// <param name="circuitNumber">Номер контура</param>
        /// <returns>true — удалить, false — отменить</returns>
        private bool ConfirmDeleteCircuit(int circuitNumber)
        {
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить контур №{circuitNumber}?",
                "Удаление контура",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Диалоговое окно подтверждения удаления коллектора
        /// </summary>
        /// <param name="collectorNumber">Номер коллектора</param>
        /// <returns>true — удалить, false — отменить</returns>
        private bool ConfirmDeleteCollector(int collectorNumber)
        {
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить коллектор №{collectorNumber}?\nВсе контуры этого коллектора будут удалены.",
                "Удаление коллектора",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Проверка возможности удаления контура
        /// </summary>
        /// <param name="circuit">Контур для удаления</param>
        /// <returns>true — можно удалить, false — нельзя</returns>
        private bool CanRemoveCircuit(CircuitRow circuit)
        {
            // Нельзя удалить, если:
            // 1. Контур не выбран (circuit == null)
            // 2. В коллекторе только 1 контур (минимум 1 контур должен остаться)
            if (circuit == null)
                return false;

            var collector = SelectedCollector;
            if (collector == null)
                return false;

            return collector.Circuits.Count > 1;
        }

        /// <summary>
        /// Проверка возможности удаления коллектора
        /// </summary>
        /// <param name="collector">Коллектор для удаления</param>
        /// <returns>true — можно удалить, false — нельзя</returns>
        private bool CanRemoveCollector(CollectorData collector)
        {
            // Нельзя удалить, если:
            // 1. Коллектор не выбран (collector == null)
            // 2. В системе только 1 коллектор (минимум 1 коллектор должен остаться)
            if (collector == null)
                return false;

            return Collectors.Count > 1;
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

            // Уведомить об изменении свойств для отображения в блоках
            OnPropertyChanged(nameof(OperatingTemperatureValue));
            OnPropertyChanged(nameof(DesignTemperatureValue));
            OnPropertyChanged(nameof(OperatingModeButtonText));
            OnPropertyChanged(nameof(DesignModeButtonText));
        }

        /// <summary>
        /// Обработчик изменения свойств ThermalViewModel
        /// </summary>
        private void OnThermalViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ThermalViewModel.Result))
            {
                UpdateFromThermalModule();

                // Уведомить об изменении свойств для отображения в блоках
                OnPropertyChanged(nameof(SupplyTemperature));
                OnPropertyChanged(nameof(ReturnTemperature));
                OnPropertyChanged(nameof(PowerUp));
                OnPropertyChanged(nameof(PowerDown));
                OnPropertyChanged(nameof(InnerDiameter));
                OnPropertyChanged(nameof(OperatingTemperatureValue));
                OnPropertyChanged(nameof(DesignTemperatureValue));
                OnPropertyChanged(nameof(OperatingModeButtonText));
                OnPropertyChanged(nameof(DesignModeButtonText));
            }
            else if (e.PropertyName == nameof(ThermalViewModel.PipeSpacing))
            {
                UpdatePipeSpacingInCircuits();
                OnPropertyChanged(nameof(PipeSpacing_cm));
            }
            else if (e.PropertyName == nameof(ThermalViewModel.SelectedPipe))
            {
                // Обновить внутренний диаметр при смене трубы
                UpdateInnerDiameterFromSelectedPipe();

                // Уведомить об изменении свойств трубы
                OnPropertyChanged(nameof(PipeType));
                OnPropertyChanged(nameof(OuterDiameter));
                OnPropertyChanged(nameof(WallThickness));
                OnPropertyChanged(nameof(InnerDiameter));
            }
        }

        /// <summary>
        /// Обновить внутренний диаметр из выбранной трубы
        /// </summary>
        private void UpdateInnerDiameterFromSelectedPipe()
        {
            var selectedPipe = _thermalViewModel.SelectedPipe;
            if (selectedPipe != null)
            {
                InputData.InnerDiameter = selectedPipe.InnerDiameter;
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

            // Уведомить об изменении свойств для отображения в блоках
            OnPropertyChanged(nameof(OperatingTemperatureValue));
            OnPropertyChanged(nameof(DesignTemperatureValue));
            OnPropertyChanged(nameof(OperatingModeButtonText));
            OnPropertyChanged(nameof(DesignModeButtonText));
            OnPropertyChanged(nameof(InnerDiameter));
            OnPropertyChanged(nameof(SupplyTemperature));
            OnPropertyChanged(nameof(ReturnTemperature));
            OnPropertyChanged(nameof(PowerUp));
            OnPropertyChanged(nameof(PowerDown));
            OnPropertyChanged(nameof(SupplySpacing_cm));
            OnPropertyChanged(nameof(SupplyHeatPercent));

            // Выполнить расчёт после обновления данных
            Calculate();
        }

        /// <summary>
        /// Автоматический выбор типа коллектора по расходу
        /// </summary>
        /// <remarks>
        /// Правила выбора:
        /// - ≤ 1.5 м³/ч → HKV-D (2-12 контуров)
        /// - 1.5 < G < 2.5 м³/ч → IV 1¼" (2-12 контуров)
        /// - 2.5 ≤ G < 7 м³/ч → IV 1½" (2-12 контуров)
        /// - ≥ 7 м³/ч → предупреждение о превышении расхода
        /// 
        /// Дополнительно проверяется:
        /// - Δp ≤ 320 мбар (32000 Па) — ограничение РЕХАУ
        ///   Проверка выполняется для ОБОИХ режимов: рабочего и холодного пуска
        /// </remarks>
        private void AutoSelectCollectorType()
        {
            var collector = SelectedCollector;
            if (collector == null) return;

            var summary = collector.Summary;
            if (summary == null) return;

            // Суммарный расход в м³/ч
            var totalFlowRate_m3h = summary.TotalFlowRate / 1000.0;
            var circuitsCount = collector.Circuits.Count;

            // Проверка превышения давления (320 мбар = 32000 Па = 32 кПа)
            // Проверка выполняется для ОБОИХ режимов: рабочего и холодного пуска
            var warnings = new System.Collections.Generic.List<string>();
            
            // Рабочий режим
            if (summary.PressureLoss_Operating_Pa > CollectorSummary.MaxAllowedPressure_Pa)
            {
                double pressureKPa = summary.PressureLoss_Operating_Pa / 1000.0;
                warnings.Add($"Превышение давления (рабочий режим): {pressureKPa:F1} кПа > 32 кПа");
            }
            
            // Холодный пуск
            if (summary.PressureLoss_Cold_Pa > CollectorSummary.MaxAllowedPressure_Pa)
            {
                double pressureKPa = summary.PressureLoss_Cold_Pa / 1000.0;
                warnings.Add($"Превышение давления (холодный пуск): {pressureKPa:F1} кПа > 32 кПа");
            }
            
            bool flowRateExceeded = totalFlowRate_m3h >= 7.0;

            // Установка предупреждений
            if (warnings.Count > 0)
            {
                // Объединяем предупреждения о давлении
                summary.Warning = string.Join("\n", warnings);
            }
            // Предупреждение о расходе (только если давление в норме)
            else if (flowRateExceeded)
            {
                // Предупреждение о превышении расхода
                // Используем инвариантную культуру для форматирования (точка как разделитель)
                summary.Warning = $"Превышение расхода: {totalFlowRate_m3h.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} м³/ч ≥ 7.0 м³/ч. Рекомендуется разделить на несколько коллекторов.";
            }
            else
            {
                summary.Warning = null;
            }

            // Автоматический выбор типа коллектора по расходу
            // (не зависит от предупреждений о давлении)
            if (totalFlowRate_m3h >= 2.5)
            {
                collector.CollectorType = "IV 1½\" (2-12 контуров)";
                collector.ValveType = ValveType.IV_1_5;
            }
            else if (totalFlowRate_m3h > 1.5)
            {
                collector.CollectorType = "IV 1¼\" (2-12 контуров)";
                collector.ValveType = ValveType.IV_1_25;
            }
            else
            {
                collector.CollectorType = "HKV-D (2-12 контуров)";
                collector.ValveType = ValveType.HKV_D;
            }

            // Обновить отображение типа коллектора
            OnPropertyChanged(nameof(CollectorTypeDisplay));
            OnPropertyChanged(nameof(KvValue));
        }

        /// <summary>
        /// Обработчик изменения свойств ClimateViewModel
        /// </summary>
        private void OnClimatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ClimateViewModel.AirTemperature))
            {
                UpdateFromClimateModule();

                // Уведомить об изменении расчётной температуры
                OnPropertyChanged(nameof(DesignTemperatureValue));
                OnPropertyChanged(nameof(DesignModeButtonText));
            }
        }

        /// <summary>
        /// Обновить данные из ClimateModule
        /// </summary>
        /// <remarks>
        /// Вызывается при изменении ClimateViewModel.AirTemperature.
        /// Обновляет InputData.ColdFiveDayTemperature и выполняет пересчёт.
        /// </remarks>
        public void UpdateFromClimateModule()
        {
            // Расчётная температура (М10/М15/М20)
            InputData.ColdFiveDayTemperature = _climateViewModel.AirTemperature;

            // Уведомить об изменении расчётной температуры
            OnPropertyChanged(nameof(DesignTemperatureValue));
            OnPropertyChanged(nameof(DesignModeButtonText));

            // Выполнить расчёт после обновления данных
            Calculate();
        }

        /// <summary>
        /// Обработчик изменения состояния расчёта
        /// </summary>
        private void OnCalculationStateChanged(object? sender, ModuleStateChangedEventArgs e)
        {
            // Уведомить UI об изменении свойства IsCalculating
            OnPropertyChanged(nameof(IsCalculating));
        }

        #endregion

        #region Property Changed Handlers

        /// <summary>
        /// Обработчик изменения типа гликоля
        /// </summary>
        partial void OnGlycolTypeChanged(GlycolType value)
        {
            InputData.GlycolType = value;
            OnPropertyChanged(nameof(GlycolTypeName));
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

        /// <summary>
        /// Обработчик изменения выбранного коллектора
        /// </summary>
        partial void OnSelectedCollectorIndexChanged(int value)
        {
            // Сбросить выбранный контур при переключении коллектора
            SelectedCircuit = null;
            OnPropertyChanged(nameof(SelectedCollector));
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(CollectorTypeDisplay));
            OnPropertyChanged(nameof(KvValue));
            AddCircuitCommand.NotifyCanExecuteChanged();
            RemoveCircuitCommand.NotifyCanExecuteChanged();
        }

        #endregion
    }
}