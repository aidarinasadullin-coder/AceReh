using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Core;
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Hydraulics;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Results;

namespace SnowMeltingCalculator.ViewModels.Hydraulics
{
    public partial class CircuitsViewModel : ObservableObject
    {
        #region Constants

        private const double DefaultSupplyTemperature = 50.0;
        private const double DefaultReturnTemperature = 30.0;
        private const double DefaultPowerUp = 180.0;
        private const double DefaultPowerDown = 80.0;
        private const double DefaultInnerDiameter = 14.2;

        #endregion

        #region Services

        private readonly ICircuitsCalculator _circuitsCalculator;
        private readonly IGlycolDataService _glycolService;
        private readonly ICalculationStateService _calculationStateService;
        private readonly ICircuitsValidator _validator;
        private readonly ICollectorTypeSelector _collectorTypeSelector;
        private readonly CalculationContext _calculationContext;
        private readonly IMarkDirtyService _markDirtyService;

        private PropertyChangedEventHandler? _inputDataPropertyChangedHandler;
        private EventHandler<ContextChangedEventArgs>? _contextChangedHandler;
        private EventHandler<int>? _pipeSpacingChangedHandler;
        private bool _isInitializing = true;
        private bool _isResetting;

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
        /// Содержит только гидравлически-локальные данные (гликоль, шаг подводки,
        /// доля тепла от подводок, тип клапана). Значения из ThermalModule,
        /// ClimateModule и ICalculationStateService читаются через CalculationContext.
        /// </remarks>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SupplySpacing_cm))]
        [NotifyPropertyChangedFor(nameof(SupplyHeatPercent))]
        [NotifyPropertyChangedFor(nameof(GlycolTypeName))]
        private HydraulicInputData _inputData = new();

        /// <summary>
        /// Сообщение об ошибке валидации гликоля для отображения в UI
        /// </summary>
        /// <remarks>
        /// Заполняется при выбросе ArgumentOutOfRangeException в GlycolDataService.
        /// Очищается в начале каждой попытки расчёта. Ошибка НЕ пробрасывается
        /// в ThermalViewModel — она локализуется в гидравлическом модуле.
        /// </remarks>
        [ObservableProperty]
        private string _validationMessage = string.Empty;

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
        /// Берётся из CalculationContext.ThermalResult или ThermalInputs.
        /// </remarks>
        public double SupplyTemperature =>
            _calculationContext.IsThermalValid
                ? _calculationContext.SupplyTemperature
                : _calculationContext.ThermalInputs?.SupplyTemperature ?? DefaultSupplyTemperature;

        /// <summary>
        /// Температура обратки, °C
        /// </summary>
        /// <remarks>
        /// Берётся из CalculationContext.ThermalResult.
        /// </remarks>
        public double ReturnTemperature =>
            _calculationContext.IsThermalValid
                ? _calculationContext.ReturnTemperature
                : DefaultReturnTemperature;

        /// <summary>
        /// Тип трубы (наименование)
        /// </summary>
        /// <remarks>
        /// Берётся из CalculationContext.ThermalInputs.Pipe.Name
        /// </remarks>
        public string PipeType => _calculationContext.ThermalInputs?.Pipe?.Name ?? "Труба не выбрана";

        /// <summary>
        /// Наружный диаметр трубы, мм
        /// </summary>
        /// <remarks>
        /// Берётся из CalculationContext.ThermalInputs.Pipe.OuterDiameter
        /// </remarks>
        public double OuterDiameter => _calculationContext.ThermalInputs?.Pipe?.OuterDiameter ?? 0;

        /// <summary>
        /// Толщина стенки трубы, мм
        /// </summary>
        /// <remarks>
        /// Берётся из CalculationContext.ThermalInputs.Pipe.WallThickness
        /// </remarks>
        public double WallThickness => _calculationContext.ThermalInputs?.Pipe?.WallThickness ?? 0;

        /// <summary>
        /// Внутренний диаметр трубы, мм
        /// </summary>
        /// <remarks>
        /// Берётся из CalculationContext.ThermalInputs.Pipe.InnerDiameter
        /// </remarks>
        public double InnerDiameter =>
            _calculationContext.ThermalInputs?.Pipe?.InnerDiameter ?? DefaultInnerDiameter;

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
        /// Берётся из CalculationContext.AirTemperature (таблица 1.6 СП 131.13330.2025).
        /// </remarks>
        public double DesignTemperature => _calculationContext.AirTemperature;

        /// <summary>
        /// Рабочая температура теплоносителя, °C
        /// </summary>
        public double OperatingTemperatureValue =>
            _calculationContext.IsThermalValid
                ? _calculationContext.ThermalResult!.MeanTemperature
                : 0;

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
        /// Берётся из CalculationContext.ThermalResult.PowerUp
        /// </remarks>
        public double PowerUp =>
            _calculationContext.IsThermalValid
                ? _calculationContext.PowerUp
                : DefaultPowerUp;

        /// <summary>
        /// Удельная мощность вниз, Вт/м²
        /// </summary>
        /// <remarks>
        /// Берётся из CalculationContext.ThermalResult.PowerDown
        /// </remarks>
        public double PowerDown =>
            _calculationContext.IsThermalValid
                ? _calculationContext.PowerDown
                : DefaultPowerDown;

        /// <summary>
        /// Шаг укладки, см
        /// </summary>
        /// <remarks>
        /// Берётся из CalculationContext.ThermalInputs.PipeSpacing (мм) или
        /// ICalculationStateService.PipeSpacing (мм), делённый на 10.
        /// </remarks>
        public double PipeSpacing_cm =>
            (_calculationContext.ThermalInputs?.PipeSpacing ?? _calculationStateService.PipeSpacing) / 10.0;

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

            // Флаг обработки ошибки валидации гликоля — если true,
            // внешний finally не должен сбрасывать ModuleState.Error обратно в Actual
            bool errorHandled = false;

            try
            {
                // Очистить предыдущее сообщение об ошибке перед новой попыткой
                ValidationMessage = string.Empty;

                var thermalResult = _calculationContext.ThermalResult;
                var thermalInputs = _calculationContext.ThermalInputs;

                double supplyTemperature;
                double returnTemperature;
                double powerUp;
                double powerDown;

                if (_calculationContext.IsThermalValid)
                {
                    supplyTemperature = thermalResult!.SupplyTemperature;
                    returnTemperature = thermalResult.ReturnTemperature;
                    powerUp = thermalResult.PowerUp;
                    powerDown = thermalResult.PowerDown;
                }
                else
                {
                    // Холодный пуск без теплового расчёта — fallback-значения
                    supplyTemperature = 35.0;
                    returnTemperature = 30.0;
                    powerUp = DefaultPowerUp;
                    powerDown = DefaultPowerDown;
                }

                double deltaT = thermalResult?.DeltaT ?? thermalInputs?.DeltaT ?? (supplyTemperature - returnTemperature);
                if (deltaT <= 0)
                {
                    deltaT = 5.0;
                }

                double innerDiameter = thermalInputs?.Pipe?.InnerDiameter ?? DefaultInnerDiameter;
                double pipeSpacing_mm = thermalInputs?.PipeSpacing ?? _calculationStateService.PipeSpacing;

                var collector = SelectedCollector;
                if (collector == null)
                {
                    // Нет коллектора — сбросить результаты гидравлики в контексте (не оставлять stale).
                    _calculationContext.UpdateHydraulics(((List<CollectorSummary>?)null)!, "CircuitsViewModel");
                    return;
                }

                double operatingTemp = thermalResult?.MeanTemperature ?? 0.0;
                double designTemp = _calculationContext.AirTemperature;

                // Валидация гликоля: локальный перехват ArgumentOutOfRangeException
                // (концентрация вне диапазона или температура вне допустимого диапазона).
                // НЕ пробрасываем вверх — иначе ошибка уйдёт в ThermalViewModel.
                // 0% (вода) остаётся валидным: short-circuit в GlycolDataService.ValidateParameters.
                GlycolProperties glycolOperating;
                GlycolProperties glycolDesign;
                try
                {
                    glycolOperating = _glycolService.GetProperties(InputData.GlycolType, InputData.GlycolConcentration, operatingTemp);
                    glycolDesign = _glycolService.GetProperties(InputData.GlycolType, InputData.GlycolConcentration, designTemp);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    // Показать ошибку в UI гидравлики и зафиксировать её в ModuleState,
                    // не давая внешнему finally перевести Error обратно в Actual
                    ValidationMessage = ex.Message;
                    errorHandled = true;
                    _calculationStateService.SetHydraulicsError(ex.Message);
                    return;
                }

                OperatingGlycolProperties = glycolOperating;
                DesignGlycolProperties = glycolDesign;

                // Получить шаг укладки из контекста (мм → см)
                double pipeSpacing_cm = pipeSpacing_mm / 10.0;

                // Синхронизировать шаг укладки во всех контурах перед расчётом
                foreach (var col in Collectors)
                {
                    foreach (var circuit in col.Circuits)
                    {
                        circuit.PipeSpacing_cm = pipeSpacing_cm;
                    }
                }

                // === ЭТАП 1: Рассчитать FlowRate для всех контуров (не зависит от kv) ===
                foreach (var circuit in collector.Circuits)
                {
                    if (circuit.CircuitLength <= 0) continue;

                    var power = _circuitsCalculator.CalculateCircuitPower(circuit, powerUp, powerDown, pipeSpacing_cm);
                    circuit.Power = power;

                    var flowRate = _circuitsCalculator.CalculateFlowRate(power, deltaT, glycolOperating.Density, glycolOperating.SpecificHeat);
                    circuit.FlowRate = flowRate;
                }

                // === ЭТАП 2: Вычислить summary для определения типа коллектора ===
                var summary = _circuitsCalculator.CalculateCollectorSummary(
                    new List<CircuitRow>(collector.Circuits),
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
                        innerDiameter,
                        kv,
                        collector.ValveType
                    );
                    circuit.OperatingResult = operatingResult;

                    var designResult = _circuitsCalculator.CalculateAtTemperature(
                        circuit,
                        designTemp,
                        glycolDesign,
                        innerDiameter,
                        kv,
                        collector.ValveType
                    );
                    circuit.DesignResult = designResult;

                    circuit.DisplayMode = CurrentMode;
                }

                // === ЭТАП 6: Обновить summary с новыми результатами ===
                summary = _circuitsCalculator.CalculateCollectorSummary(
                    new List<CircuitRow>(collector.Circuits),
                    collector.CollectorNumber,
                    collector.ValveType
                );
                collector.Summary = summary;

                // === ЭТАП 7: Балансировка ===
                _circuitsCalculator.CalculateBalancing(
                    new List<CircuitRow>(collector.Circuits),
                    collector.ValveType
                );

                foreach (var circuit in collector.Circuits)
                {
                    circuit.DisplayMode = CurrentMode;
                }

                // === ЭТАП 8: Опубликовать результаты гидравлики в общий контекст ===
                var summaries = Collectors
                    .Where(c => c.Summary != null)
                    .Select(c => c.Summary!)
                    .ToList();
                _calculationContext.UpdateHydraulics(summaries, "CircuitsViewModel");
            }
            finally
            {
                if (errorHandled)
                {
                    // Сбросить результаты гидравлики в контексте, т.к. они могли стать stale.
                    _calculationContext.UpdateHydraulics(((List<CollectorSummary>?)null)!, "CircuitsViewModel");
                }
                if (!errorHandled) _calculationStateService.ResetHydraulicsState();
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

        #region Public Methods

        /// <summary>
        /// Сбросить ViewModel к начальному состоянию
        /// </summary>
        public void Reset()
        {
            _isResetting = true;
            try
            {
                Collectors.Clear();
                SetInputData(new HydraulicInputData());
                AddCollector();
                CurrentMode = HydraulicMode.OperatingTemperature;
            }
            finally
            {
                _isResetting = false;
            }
        }

        #endregion

        #region Constructor

        public CircuitsViewModel(
            ICircuitsCalculator circuitsCalculator,
            IGlycolDataService glycolService,
            ICalculationStateService calculationStateService,
            ICircuitsValidator validator,
            ICollectorTypeSelector collectorTypeSelector,
            CalculationContext calculationContext,
            IMarkDirtyService markDirtyService)
        {
            _circuitsCalculator = circuitsCalculator ?? throw new ArgumentNullException(nameof(circuitsCalculator));
            _glycolService = glycolService ?? throw new ArgumentNullException(nameof(glycolService));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _collectorTypeSelector = collectorTypeSelector ?? throw new ArgumentNullException(nameof(collectorTypeSelector));
            _calculationContext = calculationContext ?? throw new ArgumentNullException(nameof(calculationContext));
            _markDirtyService = markDirtyService ?? throw new ArgumentNullException(nameof(markDirtyService));

            // Подписка на изменения состояния расчёта
            _calculationStateService.StateChanged += OnCalculationStateChanged;

            // Подписка на изменения шага укладки (ThermalViewModel -> CalculationStateService)
            _pipeSpacingChangedHandler = OnPipeSpacingChanged;
            _calculationStateService.PipeSpacingChanged += _pipeSpacingChangedHandler;

            // Подписка на изменения единого контекста расчёта (T15)
            _contextChangedHandler = OnCalculationContextChanged;
            _calculationContext.ContextChanged += _contextChangedHandler;

            // Подписка на изменения коллекторов и их контуров для отслеживания изменений проекта
            // ДОЛЖНА быть до AddCollector(), чтобы первый коллектор и его контуры получили обработчики
            Collectors.CollectionChanged += OnCollectorsCollectionChanged;

            // Инициализация InputData с переподпиской
            SetInputData(new HydraulicInputData());

            AddCollector();

            _isInitializing = false;
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
        /// Обработчик изменения коллекции коллекторов
        /// </summary>
        private void OnCollectorsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CollectorData collector in e.NewItems)
                {
                    collector.PropertyChanged += OnCollectorPropertyChanged;
                    collector.Circuits.CollectionChanged += OnCircuitsCollectionChanged;
                    foreach (var circuit in collector.Circuits)
                    {
                        circuit.PropertyChanged += OnCircuitPropertyChanged;
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (CollectorData collector in e.OldItems)
                {
                    collector.PropertyChanged -= OnCollectorPropertyChanged;
                    collector.Circuits.CollectionChanged -= OnCircuitsCollectionChanged;
                    foreach (var circuit in collector.Circuits)
                    {
                        circuit.PropertyChanged -= OnCircuitPropertyChanged;
                    }
                }
            }

            if (!_isInitializing && !_isResetting)
            {
                _markDirtyService.MarkDirty();
            }
        }

        /// <summary>
        /// Обработчик изменения коллекции контуров внутри коллектора
        /// </summary>
        private void OnCircuitsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CircuitRow circuit in e.NewItems)
                {
                    circuit.PropertyChanged += OnCircuitPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (CircuitRow circuit in e.OldItems)
                {
                    circuit.PropertyChanged -= OnCircuitPropertyChanged;
                }
            }

            if (!_isInitializing && !_isResetting)
            {
                _markDirtyService.MarkDirty();
            }
        }

        /// <summary>
        /// Обработчик изменения свойств коллектора
        /// </summary>
        private void OnCollectorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CollectorData.ValveType) ||
                e.PropertyName == nameof(CollectorData.CollectorType))
            {
                if (!_isInitializing && !_isResetting)
                {
                    _markDirtyService.MarkDirty();
                }
            }
        }

        /// <summary>
        /// Обработчик изменения свойств контура
        /// </summary>
        private void OnCircuitPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CircuitRow.CircuitLength) ||
                e.PropertyName == nameof(CircuitRow.SupplyLength) ||
                e.PropertyName == nameof(CircuitRow.SupplySpacing_cm) ||
                e.PropertyName == nameof(CircuitRow.SupplyHeatPercent) ||
                e.PropertyName == nameof(CircuitRow.PipeSpacing_cm))
            {
                if (!_isInitializing && !_isResetting)
                {
                    _markDirtyService.MarkDirty();
                }
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
        /// Обновить данные из ThermalModule
        /// </summary>
        /// <remarks>
        /// Заполняет единый CalculationContext данными из теплового расчёта.
        /// CircuitsViewModel больше не хранит тепловые параметры в InputData.
        /// </remarks>
        public void UpdateFromThermalModule(IThermalCalculationResult? thermalResult, PipeType? selectedPipe)
        {
            if (thermalResult == null || !thermalResult.IsValid)
            {
                SetInputData(new HydraulicInputData());
                NotifyThermalPropertiesChanged();
                return;
            }

            // Тепловые данные уже в CalculationContext (опубликованы ThermalViewModel.LoadResult или ThermalViewModel.Calculate).
            // CircuitsViewModel — чистый потребитель.
            NotifyThermalPropertiesChanged();
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
        /// Обработчик изменения единого контекста расчёта
        /// </summary>
        /// <remarks>
        /// Реагируем на ThermalInputs, ThermalResult и Climate.
        /// - ThermalInputs: уведомляем UI о смене трубы/шага укладки (без пересчёта,
        ///   т.к. изменение входных данных ещё не означает готовности результата).
        /// - ThermalResult (логическое завершение теплового расчёта): уведомляем UI.
        ///   Пересчёт гидравлики запускается только при валидном результате;
        ///   invalid/null оставляет fallback в UI без расчёта по невалидным данным.
        /// - Climate: обновляем расчётную температуру и пересчитываем гидравлику.
        /// Собственные изменения контекста (Source == "CircuitsViewModel") игнорируем,
        /// чтобы избежать двойного пересчёта — Calculate вызывается явно.
        /// </remarks>
        private void OnCalculationContextChanged(object? sender, ContextChangedEventArgs e)
        {
            // Игнорировать собственные изменения контекста — Calculate вызывается явно
            if (e.Source == "CircuitsViewModel")
                return;

            switch (e.PropertyName)
            {
                case nameof(CalculationContext.ThermalInputs):
                    NotifyThermalPropertiesChanged();
                    break;

                case nameof(CalculationContext.ThermalResult):
                    NotifyThermalPropertiesChanged();
                    // Только валидный результат вызывает пересчёт;
                    // invalid/null показывает fallback в UI без расчёта по невалидным данным.
                    if (_calculationContext.ThermalResult?.IsValid == true)
                    {
                        Calculate();
                    }
                    break;

                case nameof(CalculationContext.Climate):
                    UpdateFromClimateModule();
                    break;
            }
        }

        /// <summary>
        /// Обработчик изменения шага укладки из ICalculationStateService
        /// </summary>
        private void OnPipeSpacingChanged(object? sender, int spacing)
        {
            var pipeSpacing_cm = spacing / 10.0;

            foreach (var collector in Collectors)
            {
                foreach (var circuit in collector.Circuits)
                {
                    circuit.PipeSpacing_cm = pipeSpacing_cm;
                }
            }

            OnPropertyChanged(nameof(PipeSpacing_cm));
            Calculate();
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
                if (_isResetting) return;

                if (!_isInitializing)
                {
                    _markDirtyService.MarkDirty();
                }

                if (e.PropertyName == nameof(HydraulicInputData.SupplySpacing_cm))
                {
                    _markDirtyService.MarkDirty();
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
                else if (e.PropertyName == nameof(HydraulicInputData.SupplyHeatPercent))
                {
                    _markDirtyService.MarkDirty();
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
                else if (e.PropertyName == nameof(HydraulicInputData.GlycolType))
                {
                    _markDirtyService.MarkDirty();
                    OnPropertyChanged(nameof(GlycolTypeName));
                    Calculate();
                }
                else if (e.PropertyName == nameof(HydraulicInputData.GlycolConcentration))
                {
                    _markDirtyService.MarkDirty();
                    Calculate();
                }
            };

            InputData.PropertyChanged += _inputDataPropertyChangedHandler;

            // Уведомить об изменении локальных свойств при замене InputData
            OnPropertyChanged(nameof(SupplySpacing_cm));
            OnPropertyChanged(nameof(SupplyHeatPercent));
            OnPropertyChanged(nameof(GlycolTypeName));
        }

        /// <summary>
        /// Обновить данные из ClimateModule
        /// </summary>
        /// <remarks>
        /// Вызывается при изменении CalculationContext.Climate (в том числе AirTemperature).
        /// Выполняет пересчёт и уведомляет UI об изменении расчётной температуры.
        /// </remarks>
        public void UpdateFromClimateModule()
        {
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

        private void NotifyThermalPropertiesChanged()
        {
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
