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
    public partial class CircuitsViewModel : ObservableObject
    {
        #region Services

        private readonly ICircuitsCalculator _circuitsCalculator;
        private readonly IGlycolDataService _glycolService;
        private readonly ThermalViewModel _thermalViewModel;
        private readonly ClimateViewModel _climateViewModel;
        private readonly ICalculationStateService _calculationStateService;
        private readonly ICircuitsValidator _validator;
        private readonly ICollectorTypeSelector _collectorTypeSelector;

        private PropertyChangedEventHandler? _inputDataPropertyChangedHandler;

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Summary))]
        [NotifyPropertyChangedFor(nameof(KvValue))]
        [NotifyPropertyChangedFor(nameof(CollectorTypeDisplay))]
        [NotifyPropertyChangedFor(nameof(OperatingTemperatureValue))]
        [NotifyPropertyChangedFor(nameof(DesignTemperatureValue))]
        [NotifyPropertyChangedFor(nameof(OperatingModeButtonText))]
        [NotifyPropertyChangedFor(nameof(DesignModeButtonText))]
        private HydraulicMode _currentMode = HydraulicMode.OperatingTemperature;

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
        /// Берётся из InputData.SelectedPipe.Name
        /// </remarks>
        public string PipeType => InputData.SelectedPipe?.Name ?? "Труба не выбрана";

        /// <summary>
        /// Наружный диаметр трубы, мм
        /// </summary>
        /// <remarks>
        /// Берётся из InputData.SelectedPipe.OuterDiameter
        /// </remarks>
        public double OuterDiameter => InputData.SelectedPipe?.OuterDiameter ?? 0;

        /// <summary>
        /// Толщина стенки трубы, мм
        /// </summary>
        /// <remarks>
        /// Берётся из InputData.SelectedPipe.WallThickness
        /// </remarks>
        public double WallThickness => InputData.SelectedPipe?.WallThickness ?? 0;

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
        public string GlycolTypeName => InputData.GlycolType switch
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
        /// Берётся из InputData.PipeSpacing (мм) / 10
        /// </remarks>
        public double PipeSpacing_cm => InputData.PipeSpacing / 10.0;

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
                // Убедиться, что InputData содержит тепловые входные данные из контрактов
                if (InputData.PipeSpacing <= 0)
                {
                    InputData.PipeSpacing = _calculationStateService.PipeSpacing;
                }

                if (InputData.SelectedPipe != null && InputData.InnerDiameter <= 0)
                {
                    InputData.InnerDiameter = InputData.SelectedPipe.InnerDiameter;
                }

                if (InputData.InnerDiameter <= 0)
                {
                    InputData.InnerDiameter = 14.2;
                    InputData.SupplyTemperature = 35;
                    InputData.ReturnTemperature = 30;
                    InputData.PowerUp = 180;
                    InputData.PowerDown = 80;
                    InputData.ColdFiveDayTemperature = -28;
                }

                if (InputData.PipeSpacing <= 0)
                {
                    InputData.PipeSpacing = _calculationStateService.PipeSpacing;
                }

                var collector = SelectedCollector;
                if (collector == null) return;

                var input = InputData;

                var operatingTemp = input.OperatingTemperature;
                var designTemp = _climateViewModel.AirTemperature;

                var glycolOperating = _glycolService.GetProperties(InputData.GlycolType, InputData.GlycolConcentration, operatingTemp);
                var glycolDesign = _glycolService.GetProperties(InputData.GlycolType, InputData.GlycolConcentration, designTemp);

                OperatingGlycolProperties = glycolOperating;
                DesignGlycolProperties = glycolDesign;

                // Получить шаг укладки из InputData (мм → см)
                double pipeSpacing_cm = InputData.PipeSpacing / 10.0;

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
            ICalculationStateService calculationStateService,
            ICircuitsValidator validator,
            ICollectorTypeSelector collectorTypeSelector)
        {
            _circuitsCalculator = circuitsCalculator ?? throw new ArgumentNullException(nameof(circuitsCalculator));
            _glycolService = glycolService ?? throw new ArgumentNullException(nameof(glycolService));
            _thermalViewModel = thermalViewModel ?? throw new ArgumentNullException(nameof(thermalViewModel));
            _climateViewModel = climateViewModel ?? throw new ArgumentNullException(nameof(climateViewModel));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _collectorTypeSelector = collectorTypeSelector ?? throw new ArgumentNullException(nameof(collectorTypeSelector));

            // Подписка на изменения состояния расчёта
            _calculationStateService.StateChanged += OnCalculationStateChanged;

            // Подписка на изменения результата теплового расчёта
            _thermalViewModel.PropertyChanged += OnThermalViewModelPropertyChanged;

            // Подписка на изменения климатических данных
            _climateViewModel.PropertyChanged += OnClimatePropertyChanged;

            // Инициализация InputData с переподпиской
            SetInputData(new HydraulicInputData());

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
            return _validator.ConfirmDeleteCircuit(circuitNumber);
        }

        /// <summary>
        /// Диалоговое окно подтверждения удаления коллектора
        /// </summary>
        /// <param name="collectorNumber">Номер коллектора</param>
        /// <returns>true — удалить, false — отменить</returns>
        private bool ConfirmDeleteCollector(int collectorNumber)
        {
            return _validator.ConfirmDeleteCollector(collectorNumber);
        }

        /// <summary>
        /// Проверка возможности удаления контура
        /// </summary>
        /// <param name="circuit">Контур для удаления</param>
        /// <returns>true — можно удалить, false — нельзя</returns>
        private bool CanRemoveCircuit(CircuitRow circuit)
        {
            if (circuit == null) return false;
            return _validator.CanRemoveCircuit(circuit, SelectedCollector);
        }

        /// <summary>
        /// Проверка возможности удаления коллектора
        /// </summary>
        /// <param name="collector">Коллектор для удаления</param>
        /// <returns>true — можно удалить, false — нельзя</returns>
        private bool CanRemoveCollector(CollectorData collector)
        {
            if (collector == null) return false;
            return _validator.CanRemoveCollector(collector, Collectors.Count);
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
        /// <remarks>
        /// T7: отключён. CircuitsViewModel теперь читает тепловые входные данные из InputData и контрактов,
        /// а не напрямую из полей ThermalViewModel. Подписка сохранена, т.к. T15 полностью удалит
        /// зависимость от ThermalViewModel.
        /// </remarks>
        private void OnThermalViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // T7: тело обработчика отключено; тепловые входные данные берутся из InputData и контрактов.
        }

        /// <summary>
        /// Обновить данные из ThermalModule
        /// </summary>
        /// <remarks>
        /// Заполняет InputData данными из теплового расчёта, переданными через параметры.
        /// CircuitsViewModel больше не читает поля ThermalViewModel напрямую.
        /// </remarks>
        public void UpdateFromThermalModule(IThermalCalculationResult? thermalResult, PipeType? selectedPipe)
        {
            InputData.ThermalResult = thermalResult;
            InputData.SelectedPipe = selectedPipe;

            if (thermalResult == null || !thermalResult.IsValid)
            {
                // Сбросить данные, если результат невалиден
                SetInputData(new HydraulicInputData());
                return;
            }

            // Обновить данные из ThermalResult
            InputData.PowerUp = thermalResult.PowerUp;
            InputData.PowerDown = thermalResult.PowerDown;
            InputData.SupplyTemperature = thermalResult.SupplyTemperature;
            InputData.ReturnTemperature = thermalResult.ReturnTemperature;

            // Получить внутренний диаметр трубы из выбранной трубы
            if (selectedPipe != null)
            {
                InputData.InnerDiameter = selectedPipe.InnerDiameter;
            }

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
            OnPropertyChanged(nameof(PipeSpacing_cm));
            OnPropertyChanged(nameof(PipeType));
            OnPropertyChanged(nameof(OuterDiameter));
            OnPropertyChanged(nameof(WallThickness));
            OnPropertyChanged(nameof(SupplySpacing_cm));
            OnPropertyChanged(nameof(SupplyHeatPercent));

            // Выполнить расчёт после обновления данных
            Calculate();
        }

        /// <summary>
        /// Автоматический выбор типа коллектора по расходу
        /// </summary>
        /// <remarks>
        /// Делегирует логику подбора коллектора сервису ICollectorTypeSelector.
        /// </remarks>
        private void AutoSelectCollectorType()
        {
            var collector = SelectedCollector;
            if (collector == null) return;

            var result = _collectorTypeSelector.SelectCollectorType(collector);

            // Применить результат
            collector.CollectorType = result.CollectorType;
            collector.ValveType = result.ValveType;

            if (collector.Summary != null)
            {
                collector.Summary.Warning = result.Warning;
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
        /// Установить новый InputData с переподпиской на PropertyChanged
        /// </summary>
        /// <param name="newInputData">Новые входные данные</param>
        private void SetInputData(HydraulicInputData newInputData)
        {
            // Отписаться от старого InputData
            if (InputData != null && _inputDataPropertyChangedHandler != null)
            {
                InputData.PropertyChanged -= _inputDataPropertyChangedHandler;
            }

            // Установить новый InputData
            InputData = newInputData;

            // Создать обработчик и подписаться на новый InputData
            _inputDataPropertyChangedHandler = (s, e) =>
            {
                if (e.PropertyName == nameof(HydraulicInputData.SupplySpacing_cm))
                {
                    OnPropertyChanged(nameof(SupplySpacing_cm));
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
                    foreach (var collector in Collectors)
                    {
                        foreach (var circuit in collector.Circuits)
                        {
                            circuit.SupplyHeatPercent = InputData.SupplyHeatPercent;
                        }
                    }
                    Calculate();
                }
                if (e.PropertyName == nameof(HydraulicInputData.GlycolType))
                {
                    OnPropertyChanged(nameof(GlycolTypeName));
                    Calculate();
                }
                if (e.PropertyName == nameof(HydraulicInputData.GlycolConcentration))
                {
                    Calculate();
                }
            };

            InputData.PropertyChanged += _inputDataPropertyChangedHandler;
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
        /// Обработчик изменения режима отображения (рабочая/расчётная температура)
        /// </summary>
        partial void OnCurrentModeChanged(HydraulicMode value)
        {
            UpdateCircuitDisplayMode();
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