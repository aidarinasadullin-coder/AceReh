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
using SnowMeltingCalculator.Services.Project;

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
        private readonly IHydraulicsStateCoordinator _coordinator;
        private readonly IProjectSessionHydraulicsState _hydraulicsState;

        private PropertyChangedEventHandler? _inputDataPropertyChangedHandler;
        private bool _isMirroringHydraulicsState;
        private bool _isCalculating;
        private bool _isInitializing = true;
        private bool _isResetting;

        /// <summary>
        /// Коллекторы, к которым текущий ViewModel уже подписан на события.
        /// Используется для корректного отсоединения обработчиков при Reset,
        /// когда ObservableCollection не предоставляет OldItems.
        /// </summary>
        private readonly List<CollectorData> _subscribedCollectors = new();

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<CollectorData> _collectors = new();

        /// <summary>
        /// Карточки итогов гидравлики по всем коллекторам (для отображения в Hydraulics).
        /// </summary>
        /// <remarks>
        /// Заполняется через RebuildHydraulicSummaryCards() из Collectors.
        /// Каждая карточка — снимок CollectorData.Summary + CollectorNumber + CollectorTypeDisplayWithCount.
        /// </remarks>
        [ObservableProperty]
        private ObservableCollection<CollectorHydraulicSummaryCard> _hydraulicSummaryCards = new();

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
            _calculationStateService.PipeSpacing / 10.0;

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
            _isCalculating = true;
            try
            {
                _coordinator.Calculate(ExecuteCalculate);
            }
            finally
            {
                _isCalculating = false;
            }
        }

        private List<CollectorSummary>? ExecuteCalculate()
        {
            _isCalculating = true;
            try
            {
                ValidationMessage = string.Empty;

                if (SelectedCollector == null)
                {
                    return null;
                }

                CalculateCollector(SelectedCollector, autoSelectType: true);

                if (!string.IsNullOrEmpty(ValidationMessage))
                {
                    return null;
                }

                RebuildHydraulicSummaryCards();
                return Collectors.Where(c => c.Summary != null).Select(c => c.Summary!).ToList();
            }
            finally
            {
                _isCalculating = false;
            }
        }

        private void CalculateAllCollectors()
        {
            _isCalculating = true;
            try
            {
                _coordinator.CalculateAll(ExecuteCalculateAll);
            }
            finally
            {
                _isCalculating = false;
            }
        }

        private List<CollectorSummary>? ExecuteCalculateAll()
        {
            _isCalculating = true;
            try
            {
                ValidationMessage = string.Empty;

                foreach (var collector in Collectors)
                {
                    CalculateCollector(collector, autoSelectType: true);
                    if (!string.IsNullOrEmpty(ValidationMessage))
                    {
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(ValidationMessage))
                {
                    return null;
                }

                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(CollectorTypeDisplay));
                OnPropertyChanged(nameof(KvValue));

                RebuildHydraulicSummaryCards();
                return Collectors.Where(c => c.Summary != null).Select(c => c.Summary!).ToList();
            }
            finally
            {
                _isCalculating = false;
            }
        }

        private void CalculateCollector(CollectorData collector, bool autoSelectType)
        {
            if (collector == null) return;

            var thermalResult = _calculationContext.ThermalResult;
            var thermalInputs = _calculationContext.ThermalInputs;

            double supplyTemperature;
            double returnTemperature;
            double powerUp;
            double powerDown;

            if (thermalResult?.IsValid == true)
            {
                supplyTemperature = thermalResult.SupplyTemperature;
                returnTemperature = thermalResult.ReturnTemperature;
                powerUp = thermalResult.PowerUp;
                powerDown = thermalResult.PowerDown;
            }
            else
            {
                supplyTemperature = 35.0;
                returnTemperature = 30.0;
                powerUp = DefaultPowerUp;
                powerDown = DefaultPowerDown;
            }

            double deltaT = thermalResult?.DeltaT ?? (supplyTemperature - returnTemperature);
            if (deltaT <= 0)
            {
                deltaT = 5.0;
            }

            double innerDiameter = thermalInputs?.Pipe?.InnerDiameter ?? DefaultInnerDiameter;
            double pipeSpacing_mm = _calculationStateService.PipeSpacing;

            double operatingTemp = thermalResult?.MeanTemperature ?? 0.0;
            double designTemp = _calculationContext.AirTemperature;

            GlycolProperties glycolOperating;
            GlycolProperties glycolDesign;
            try
            {
                glycolOperating = _glycolService.GetProperties(InputData.GlycolType, InputData.GlycolConcentration, operatingTemp);
                glycolDesign = _glycolService.GetProperties(InputData.GlycolType, InputData.GlycolConcentration, designTemp);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ValidationMessage = ex.Message;
                _calculationStateService.SetHydraulicsError(ex.Message);
                _coordinator.PublishHydraulics(null);
                return;
            }

            OperatingGlycolProperties = glycolOperating;
            DesignGlycolProperties = glycolDesign;

            double pipeSpacing_cm = pipeSpacing_mm / 10.0;

            if (!_calculationStateService.IsLoadProjectInProgress)
            {
                foreach (var col in Collectors)
                {
                    foreach (var circuit in col.Circuits)
                    {
                        circuit.PipeSpacing_cm = pipeSpacing_cm;
                    }
                }
            }

            foreach (var circuit in collector.Circuits)
            {
                if (circuit.CircuitLength <= 0) continue;

                var power = _circuitsCalculator.CalculateCircuitPower(circuit, powerUp, powerDown, pipeSpacing_cm);
                circuit.Power = power;

                var flowRate = _circuitsCalculator.CalculateFlowRate(power, deltaT, glycolOperating.Density, glycolOperating.SpecificHeat);
                circuit.FlowRate = flowRate;
            }

            var summary = _circuitsCalculator.CalculateCollectorSummary(
                new List<CircuitRow>(collector.Circuits),
                collector.CollectorNumber,
                collector.ValveType
            );
            collector.Summary = summary;

            if (autoSelectType)
            {
                AutoSelectCollectorTypeFor(collector);
            }

            var kv = collector.ValveType switch
            {
                ValveType.HKV_D => 1.2,
                ValveType.IV_1_25 => 1.45,
                ValveType.IV_1_5 => 1.5,
                _ => 1.2
            };

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

            summary = _circuitsCalculator.CalculateCollectorSummary(
                new List<CircuitRow>(collector.Circuits),
                collector.CollectorNumber,
                collector.ValveType
            );
            collector.Summary = summary;

            _circuitsCalculator.CalculateBalancing(
                new List<CircuitRow>(collector.Circuits),
                collector.ValveType
            );

            foreach (var circuit in collector.Circuits)
            {
                circuit.DisplayMode = CurrentMode;
            }
        }

        /// <summary>
        /// Перестроить канонический read-model карточек итогов гидравлики по всем коллекторам.
        /// </summary>
        private void RebuildHydraulicSummaryCards()
        {
            HydraulicSummaryCards.Clear();

            foreach (var collector in Collectors)
            {
                if (collector == null) continue;
                HydraulicSummaryCards.Add(new CollectorHydraulicSummaryCard(collector));
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
                RebuildHydraulicSummaryCards();
            }
            finally
            {
                _isResetting = false;
            }
        }

        /// <summary>
        /// Mirror a canonical lifecycle snapshot into the WPF adapter.
        /// The caller owns the canonical mutation; this method only refreshes UI data.
        /// </summary>
        public void ApplyLifecycleSnapshotToAdapter(HydraulicsStateSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            _isResetting = true;
            try
            {
                Collectors.Clear();
                SetInputData(new HydraulicInputData
                {
                    GlycolType = snapshot.GlobalInputs.GlycolType,
                    GlycolConcentration = snapshot.GlobalInputs.GlycolConcentration,
                    SupplySpacing_cm = snapshot.GlobalInputs.SupplySpacingCm,
                    SupplyHeatPercent = snapshot.GlobalInputs.SupplyHeatPercent
                });

                foreach (var collectorSnapshot in snapshot.Collectors)
                {
                    var collector = new CollectorData(collectorSnapshot.CollectorNumber)
                    {
                        CollectorType = collectorSnapshot.CollectorType ?? string.Empty,
                        ValveType = collectorSnapshot.ValveType
                    };

                    if (collectorSnapshot.Summary is { } summary)
                    {
                        collector.Summary = new CollectorSummary
                        {
                            CollectorNumber = collectorSnapshot.CollectorNumber,
                            CollectorType = summary.CollectorType,
                            CircuitCount = summary.CircuitCount,
                            TotalPipeLength = summary.TotalPipeLength,
                            TotalPower = summary.TotalPower,
                            TotalFlowRate = summary.TotalFlowRate,
                            PressureLoss_Operating_Pa = summary.PressureLoss_Operating_Pa,
                            PressureLoss_Cold_Pa = summary.PressureLoss_Cold_Pa,
                            Kv = summary.Kv,
                            ValveType = collectorSnapshot.ValveType
                        };
                    }

                    foreach (var circuitSnapshot in collectorSnapshot.Circuits)
                    {
                        collector.Circuits.Add(new CircuitRow
                        {
                            CircuitNumber = circuitSnapshot.CircuitNumber,
                            CircuitLength = circuitSnapshot.CircuitLength,
                            SupplyLength = circuitSnapshot.SupplyLength,
                            SupplySpacing_cm = circuitSnapshot.SupplySpacingCm,
                            SupplyHeatPercent = circuitSnapshot.SupplyHeatPercent,
                            PipeSpacing_cm = circuitSnapshot.PipeSpacingCm,
                            Power = circuitSnapshot.OperatingResult?.Power ?? 0,
                            FlowRate = circuitSnapshot.OperatingResult?.FlowRate ?? 0,
                            Velocity = circuitSnapshot.OperatingResult?.Velocity ?? 0,
                            Throttling = circuitSnapshot.OperatingResult?.Throttling ?? 0,
                            ValveTurns = circuitSnapshot.OperatingResult?.ValveTurns ?? 0,
                            OperatingResult = ToDomainResult(circuitSnapshot.OperatingResult),
                            DesignResult = ToDomainResult(circuitSnapshot.DesignResult)
                        });
                    }

                    Collectors.Add(collector);
                }

                SelectedCollectorIndex = Collectors.Count == 0 ? -1 : 0;
                CurrentMode = HydraulicMode.OperatingTemperature;
                RebuildHydraulicSummaryCards();
                AddCollectorCommand.NotifyCanExecuteChanged();
                AddCircuitCommand.NotifyCanExecuteChanged();
                RemoveCollectorCommand.NotifyCanExecuteChanged();
                RemoveCircuitCommand.NotifyCanExecuteChanged();
            }
            finally
            {
                _isResetting = false;
            }
        }

        /// <summary>
        /// Capture the current adapter state for the canonical project slice.
        /// </summary>
        private static HydraulicCircuitResultSnapshot? ToSnapshot(CircuitTemperatureResult? result, CircuitRow circuit)
        {
            if (result is null)
            {
                return null;
            }

            return new HydraulicCircuitResultSnapshot(
                circuit.Power,
                circuit.FlowRate,
                circuit.Velocity,
                result.DpRohr,
                result.DpVerteiler,
                result.DpVent,
                result.DpGesamt,
                circuit.Throttling,
                circuit.ValveTurns,
                result.Density,
                result.KinematicViscosity,
                result.ReynoldsNumber,
                result.FrictionFactor,
                result.PressureLossPerMeter,
                result.FlowRegime);
        }

        private static HydraulicCollectorSummarySnapshot? ToSnapshot(CollectorSummary? summary)
        {
            return summary is null ? null : new HydraulicCollectorSummarySnapshot(
                summary.CircuitCount,
                summary.TotalPipeLength,
                summary.TotalPower,
                summary.TotalFlowRate,
                summary.PressureLoss_Operating_Pa,
                summary.PressureLoss_Cold_Pa,
                summary.Kv,
                summary.CollectorType);
        }

        private static CircuitTemperatureResult ToDomainResult(HydraulicCircuitResultSnapshot? snapshot)
        {
            if (snapshot is null)
            {
                return new CircuitTemperatureResult();
            }

            return new CircuitTemperatureResult
            {
                DpRohr = snapshot.DpRohr,
                DpVerteiler = snapshot.DpVerteiler,
                DpVent = snapshot.DpVent,
                ZuDrosseln = snapshot.Throttling,
                FlowRegime = snapshot.FlowRegime,
                Density = snapshot.Density,
                KinematicViscosity = snapshot.KinematicViscosity,
                ReynoldsNumber = snapshot.ReynoldsNumber,
                FrictionFactor = snapshot.FrictionFactor,
                PressureLossPerMeter = snapshot.PressureLossPerMeter
            };
        }

        private IReadOnlyList<HydraulicCollectorSnapshot> CaptureCanonicalCollectors()
        {
            return Collectors.Select(collector => new HydraulicCollectorSnapshot(
                collector.CollectorNumber,
                collector.CollectorType,
                collector.ValveType,
                collector.Circuits.Select(circuit => new HydraulicCircuitSnapshot(
                    circuit.CircuitNumber,
                    circuit.CircuitLength,
                    circuit.SupplyLength,
                    circuit.SupplySpacing_cm,
                    circuit.SupplyHeatPercent,
                    circuit.PipeSpacing_cm,
                    ToSnapshot(circuit.OperatingResult, circuit),
                    ToSnapshot(circuit.DesignResult, circuit))),
                ToSnapshot(collector.Summary))).ToList();
        }

        private void OnHydraulicsStateChanged(object? sender, HydraulicsStateChangedEventArgs e)
        {
            if (e.Origin != HydraulicsMutationOrigin.ProjectLoad)
            {
                return;
            }

            _isMirroringHydraulicsState = true;
            try
            {
                ApplyLifecycleSnapshotToAdapter(e.NewSnapshot);
            }
            finally
            {
                _isMirroringHydraulicsState = false;
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
            IMarkDirtyService markDirtyService,
            IHydraulicsStateCoordinator coordinator,
            IProjectSession projectSession)
        {
            _circuitsCalculator = circuitsCalculator ?? throw new ArgumentNullException(nameof(circuitsCalculator));
            _glycolService = glycolService ?? throw new ArgumentNullException(nameof(glycolService));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _collectorTypeSelector = collectorTypeSelector ?? throw new ArgumentNullException(nameof(collectorTypeSelector));
            _calculationContext = calculationContext ?? throw new ArgumentNullException(nameof(calculationContext));
            _markDirtyService = markDirtyService ?? throw new ArgumentNullException(nameof(markDirtyService));
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _hydraulicsState = (projectSession ?? throw new ArgumentNullException(nameof(projectSession))).HydraulicsState;
            _hydraulicsState.Changed += OnHydraulicsStateChanged;

            // Подписка на изменения коллекторов и их контуров для отслеживания изменений проекта
            // ДОЛЖНА быть до AddCollector(), чтобы первый коллектор и его контуры получили обработчики
            Collectors.CollectionChanged += OnCollectorsCollectionChanged;

            // Инициализация InputData с переподпиской
            SetInputData(new HydraulicInputData());

            AddCollector();

            _coordinator.Connect(
                ExecuteCalculate,
                ExecuteCalculateAll,
                CaptureCanonicalCollectors,
                NotifyThermalPropertiesChanged,
                UpdateFromClimateModule,
                MirrorPipeSpacing);

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
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (CollectorData collector in e.NewItems)
                        {
                            AttachCircuitEvents(collector);
                        }
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (CollectorData collector in e.OldItems)
                        {
                            DetachCircuitEvents(collector);
                        }
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                    {
                        foreach (CollectorData collector in e.OldItems)
                        {
                            DetachCircuitEvents(collector);
                        }
                    }
                    if (e.NewItems != null)
                    {
                        foreach (CollectorData collector in e.NewItems)
                        {
                            AttachCircuitEvents(collector);
                        }
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    // Reset очищает коллекцию; OldItems недоступен, поэтому отсоединяемся
                    // от всех ранее отслеживаемых коллекторов. Повторная подписка НЕ производится —
                    // новые элементы попадут сюда через отдельное событие Add.
                    foreach (var collector in _subscribedCollectors.ToList())
                    {
                        DetachCircuitEvents(collector);
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    // Перемещение не меняет подписок.
                    break;
            }

            RebuildHydraulicSummaryCards();
            if (!_isInitializing && !_isResetting && !_isMirroringHydraulicsState)
            {
                if (!_isCalculating)
                {
                    _hydraulicsState.ReplaceCollectors(CaptureCanonicalCollectors(), HydraulicsMutationOrigin.User);
                }
            }
        }

        /// <summary>
        /// Подписать ViewModel на события коллектора и его контуров.
        /// </summary>
        private void AttachCircuitEvents(CollectorData collector)
        {
            if (collector == null || _subscribedCollectors.Contains(collector))
            {
                return;
            }

            collector.PropertyChanged += OnCollectorPropertyChanged;
            collector.Circuits.CollectionChanged += OnCircuitsCollectionChanged;
            foreach (var circuit in collector.Circuits)
            {
                circuit.PropertyChanged += OnCircuitPropertyChanged;
            }

            _subscribedCollectors.Add(collector);
        }

        /// <summary>
        /// Отписать ViewModel от событий коллектора и его контуров.
        /// </summary>
        private void DetachCircuitEvents(CollectorData collector)
        {
            if (collector == null)
            {
                return;
            }

            collector.PropertyChanged -= OnCollectorPropertyChanged;
            collector.Circuits.CollectionChanged -= OnCircuitsCollectionChanged;
            foreach (var circuit in collector.Circuits)
            {
                circuit.PropertyChanged -= OnCircuitPropertyChanged;
            }

            _subscribedCollectors.Remove(collector);
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

            if (!_isInitializing && !_isResetting && !_isMirroringHydraulicsState)
            {
                if (!_isCalculating)
                {
                    _hydraulicsState.ReplaceCollectors(CaptureCanonicalCollectors(), HydraulicsMutationOrigin.User);
                }
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
                if (!_isInitializing && !_isResetting && !_isMirroringHydraulicsState)
                {
                    if (!_isCalculating)
                    {
                        _hydraulicsState.ReplaceCollectors(CaptureCanonicalCollectors(), HydraulicsMutationOrigin.User);
                    }
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
                if (!_isInitializing && !_isResetting && !_isMirroringHydraulicsState)
                {
                    if (!_isCalculating)
                    {
                        _hydraulicsState.ReplaceCollectors(CaptureCanonicalCollectors(), HydraulicsMutationOrigin.User);
                    }
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

            AutoSelectCollectorTypeFor(collector);

            // Обновить отображение типа коллектора
            OnPropertyChanged(nameof(CollectorTypeDisplay));
            OnPropertyChanged(nameof(KvValue));
        }

        /// <summary>
        /// Автоматический выбор типа коллектора по расходу для указанного коллектора.
        /// </summary>
        private void AutoSelectCollectorTypeFor(CollectorData collector)
        {
            if (collector == null) return;

            var result = _collectorTypeSelector.SelectCollectorType(collector);

            collector.CollectorType = result.CollectorType;
            collector.ValveType = result.ValveType;

            if (collector.Summary != null)
            {
                collector.Summary.Warning = result.Warning;
            }
        }

        private void MirrorPipeSpacing(double pipeSpacing_cm)
        {
            _isMirroringHydraulicsState = true;
            try
            {
                foreach (var collector in Collectors)
                {
                    foreach (var circuit in collector.Circuits)
                    {
                        circuit.PipeSpacing_cm = pipeSpacing_cm;
                    }
                }

                OnPropertyChanged(nameof(PipeSpacing_cm));
            }
            finally
            {
                _isMirroringHydraulicsState = false;
            }
        }

        private void MirrorSupplyInputs(double supplySpacing_cm, double supplyHeatPercent)
        {
            _isMirroringHydraulicsState = true;
            try
            {
                foreach (var collector in Collectors)
                {
                    foreach (var circuit in collector.Circuits)
                    {
                        circuit.SupplySpacing_cm = supplySpacing_cm;
                        circuit.SupplyHeatPercent = supplyHeatPercent;
                    }
                }

                OnPropertyChanged(nameof(SupplySpacing_cm));
                OnPropertyChanged(nameof(SupplyHeatPercent));
            }
            finally
            {
                _isMirroringHydraulicsState = false;
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
                if (_isResetting || _isInitializing || _isMirroringHydraulicsState ||
                    _calculationStateService.IsLoadProjectInProgress)
                {
                    return;
                }

                _hydraulicsState.ApplyGlobalInputs(
                    new HydraulicGlobalInputsSnapshot(
                        InputData.GlycolType,
                        InputData.GlycolConcentration,
                        InputData.SupplySpacing_cm,
                        InputData.SupplyHeatPercent),
                    HydraulicsMutationOrigin.User);

                if (e.PropertyName == nameof(HydraulicInputData.SupplySpacing_cm) ||
                    e.PropertyName == nameof(HydraulicInputData.SupplyHeatPercent))
                {
                    MirrorSupplyInputs(InputData.SupplySpacing_cm, InputData.SupplyHeatPercent);
                    _markDirtyService.MarkDirty();
                    Calculate();
                }
                else if (e.PropertyName == nameof(HydraulicInputData.GlycolType) ||
                         e.PropertyName == nameof(HydraulicInputData.GlycolConcentration))
                {
                    _markDirtyService.MarkDirty();
                    CalculateAllCollectors();
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
            CalculateAllCollectors();
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
