using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Construction;
using SnowMeltingCalculator.Models.Navigation;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Navigation;
using SnowMeltingCalculator.Services.Thermal;
using SnowMeltingCalculator.Services.Results;
using SnowMeltingCalculator.Core;

namespace SnowMeltingCalculator.ViewModels.Thermal
{
    /// <summary>
    /// ViewModel для модуля теплового расчёта
    /// </summary>
    public partial class ThermalViewModel : ObservableObject
    {
        private readonly IThermalCalculator _calculator;
        private readonly IClimateData _climateData;
        private readonly IConstructionData _constructionData;
        private readonly ICalculationStateService _calculationStateService;
        private readonly CalculationContext _calculationContext;
        private readonly IValidator<ThermalInputs> _thermalValidator;
        private readonly IValidator<ThermalCalculationResult> _thermalResultValidator;
        private readonly IMarkDirtyService _markDirtyService;
        private bool _isResetting;

        #region Observable Properties

        /// <summary>
        /// Выбранный режим работы
        /// </summary>
        [ObservableProperty]
        private OperatingMode _selectedMode = OperatingMode.Melting;

        /// <summary>
        /// Температура подачи, °C
        /// </summary>
        [ObservableProperty]
        private double _supplyTemperature = 50.0;

        /// <summary>
        /// Температурный перепад, К (только для чтения, рассчитывается автоматически)
        /// </summary>
        public double? DeltaT => Result?.DeltaT;

        /// <summary>
        /// Температура грунта, °C
        /// </summary>
        [ObservableProperty]
        private double _groundTemperature = 10.0;

        /// <summary>
        /// Выбранный тип трубы
        /// </summary>
        [ObservableProperty]
        private PipeType? _selectedPipe;

        /// <summary>
        /// Шаг укладки трубы, мм
        /// </summary>
        [ObservableProperty]
        private int _pipeSpacing = 200;

        /// <summary>
        /// Термическое сопротивление слоёв над трубой, м²·К/Вт
        /// </summary>
        public double R1Total => _constructionData.R1Total;

        /// <summary>
        /// Термическое сопротивление слоёв под трубой, м²·К/Вт
        /// </summary>
        public double R2Total => _constructionData.R2Total;

        /// <summary>
        /// Доступные значения шага укладки, мм
        /// </summary>
        public int[] AvailablePipeSpacings { get; } = new[] { 150, 200, 250, 300 };

        /// <summary>
        /// Признак доступности поля Шаг укладки
        /// </summary>
        public bool IsPipeSpacingEnabled => SelectedPipe != null;

        /// <summary>
        /// Результат расчёта
        /// </summary>
        [ObservableProperty]
        private ThermalCalculationResult? _result;

        /// <summary>
        /// Уведомление об изменении результата для связанных свойств
        /// </summary>
        partial void OnResultChanged(ThermalCalculationResult? value)
        {
            OnPropertyChanged(nameof(DeltaT));
            OnPropertyChanged(nameof(RecommendedSupplyTemperature));
            OnPropertyChanged(nameof(SupplyTemperatureHint));
        }

        /// <summary>
        /// Уведомление об изменении выбранной трубы для обновления доступности шага укладки
        /// </summary>
        partial void OnSelectedPipeChanged(PipeType? value)
        {
            if (_isResetting) return;

            _markDirtyService.MarkDirty();
            OnPropertyChanged(nameof(IsPipeSpacingEnabled));

            if (Result != null)
            {
                _calculationStateService.SetThermalNeedsRecalculation("Тип трубы изменён. Требуется пересчёт.");
            }
        }

        /// <summary>
        /// Уведомление об изменении шага укладки трубы
        /// </summary>
        partial void OnPipeSpacingChanged(int value)
        {
            if (_isResetting) return;

            _markDirtyService.MarkDirty();
            // Обновляем шаг укладки в сервисе для визуализации
            _calculationStateService.SetPipeSpacing(value, "ThermalViewModel");

            if (Result != null)
            {
                _calculationStateService.SetThermalNeedsRecalculation("Шаг укладки изменён. Требуется пересчёт.");
            }
        }

        /// <summary>
        /// Уведомление об изменении температуры подачи
        /// </summary>
        partial void OnSupplyTemperatureChanged(double value)
        {
            if (_isResetting) return;

            _markDirtyService.MarkDirty();
            if (Result != null)
            {
                _calculationStateService.SetThermalNeedsRecalculation("Температура подачи изменена. Требуется пересчёт.");
            }
        }

        /// <summary>
        /// Уведомление об изменении температуры грунта
        /// </summary>
        partial void OnGroundTemperatureChanged(double value)
        {
            if (_isResetting) return;

            _markDirtyService.MarkDirty();
            if (Result != null)
            {
                _calculationStateService.SetThermalNeedsRecalculation("Температура грунта изменена. Требуется пересчёт.");
            }
        }

        /// <summary>
        /// Уведомление об изменении режима работы
        /// </summary>
        partial void OnSelectedModeChanged(OperatingMode value)
        {
            if (_isResetting) return;

            _markDirtyService.MarkDirty();
            if (Result != null)
            {
                _calculationStateService.SetThermalNeedsRecalculation("Режим работы изменён. Требуется пересчёт.");
            }
        }

        /// <summary>
        /// Рекомендуемая температура подачи для ΔT ≈ 15 К
        /// </summary>
        public double? RecommendedSupplyTemperature => Result?.MeanTemperature + 7.5;

        /// <summary>
        /// Подсказка для температуры подачи
        /// </summary>
        public string SupplyTemperatureHint =>
            RecommendedSupplyTemperature.HasValue
                ? $"Рекомендуется: {RecommendedSupplyTemperature.Value:F0}°C (для ΔT ≈ 15 К)"
                : string.Empty;

        /// <summary>
        /// Признак выполнения расчёта
        /// </summary>
        [ObservableProperty]
        private bool _isCalculating;

        /// <summary>
        /// Сообщение валидации
        /// </summary>
        [ObservableProperty]
        private string _validationMessage = string.Empty;

        /// <summary>
        /// Сообщение о необходимости пересчёта
        /// Делегирует сервису ICalculationStateService
        /// </summary>
        public string RecalcMessage => _calculationStateService.ThermalValidationMessage;

        /// <summary>
        /// Признак того, что тепловой расчёт требует пересчёта
        /// </summary>
        public bool NeedsRecalculation => _calculationStateService.ThermalNeedsRecalculation;

        #endregion

        #region Collections

        /// <summary>
        /// Доступные типы труб
        /// </summary>
        public ObservableCollection<PipeType> AvailablePipes { get; }

        /// <summary>
        /// Доступные режимы работы
        /// </summary>
        public ObservableCollection<OperatingMode> AvailableModes { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Создать ViewModel
        /// </summary>
        public ThermalViewModel(
            IThermalCalculator calculator,
            IClimateData climateData,
            IConstructionData constructionData,
            ICalculationStateService calculationStateService,
            CalculationContext calculationContext,
            IValidator<ThermalInputs> thermalValidator,
            IValidator<ThermalCalculationResult> thermalResultValidator,
            IMarkDirtyService markDirtyService)
        {
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _climateData = climateData ?? throw new ArgumentNullException(nameof(climateData));
            _constructionData = constructionData ?? throw new ArgumentNullException(nameof(constructionData));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));
            _calculationContext = calculationContext ?? throw new ArgumentNullException(nameof(calculationContext));
            _thermalValidator = thermalValidator ?? throw new ArgumentNullException(nameof(thermalValidator));
            _thermalResultValidator = thermalResultValidator ?? throw new ArgumentNullException(nameof(thermalResultValidator));
            _markDirtyService = markDirtyService ?? throw new ArgumentNullException(nameof(markDirtyService));

            // Инициализация коллекций
            AvailablePipes = new ObservableCollection<PipeType>(PipeType.StandardPipes);
            AvailableModes = new ObservableCollection<OperatingMode>
            {
                OperatingMode.AntiIcing,
                OperatingMode.Melting,
                OperatingMode.Intensive
            };

            // Подписка на изменения климатических данных
            if (_climateData is ClimateData climateDataImpl)
            {
                climateDataImpl.DataChanged += OnClimateDataChanged;
            }

            // Подписка на изменения данных конструкции
            // Construction реализует IConstructionData и вызывает DataChanged
            _constructionData.DataChanged += OnConstructionDataChanged;

            // Подписка на изменения состояния расчёта
            _calculationStateService.StateChanged += OnCalculationStateChanged;

            // Подписка на изменения канонического шага укладки
            _calculationStateService.PipeSpacingChanged += OnPipeSpacingServiceChanged;

            // Инициализация команды сброса
            ResetCommand = new RelayCommand(Reset);
        }

        /// <summary>
        /// Команда сброса к дефолтным значениям
        /// </summary>
        public IRelayCommand ResetCommand { get; }

        #endregion

        #region Commands

        /// <summary>
        /// Команда выполнения расчёта
        /// </summary>
        [RelayCommand]
        private async Task Calculate()
        {
            if (IsCalculating) return;

            // Валидация входных данных
            var inputValidation = ValidateInput();
            if (!inputValidation.IsValid)
            {
                ValidationMessage = string.Join("; ", inputValidation.Errors.Select(e => e.Message));
                return;
            }

            // Установить флаг выполнения расчёта
            _calculationStateService.SetThermalCalculating();
            IsCalculating = true;
            ValidationMessage = string.Empty;

            try
            {
                // 1. Собрать ThermalInputs из свойств
                var parameters = BuildThermalInputs();
                _calculationContext.UpdateThermalInputs(parameters, "Thermal");

                // 2. Вызвать _calculator.Calculate(parameters, _climateData, _constructionData)
                Result = await Task.Run(() => _calculator.Calculate(parameters, _climateData, _constructionData));

                // 3. Пост-расчётная валидация результата
                var resultValidation = Result != null ? _thermalResultValidator.Validate(Result) : ValidationResult.Success();

                // 5. Отобразить ошибки в ValidationMessage
                var messages = new List<string>();
                if (Result != null && !Result.IsValid && Result.ValidationErrors.Length > 0)
                {
                    messages.AddRange(Result.ValidationErrors);
                }

                if (!resultValidation.IsValid)
                {
                    messages.AddRange(resultValidation.Errors.Select(e => e.Message));
                }

                if (messages.Count > 0)
                {
                    ValidationMessage = string.Join("; ", messages);
                }

                // Публикуем валидный результат в общий контекст
                if (Result != null && Result.IsValid && resultValidation.IsValid)
                {
                    _calculationContext.UpdateThermal(Result, "Thermal");
                }

                // Сбросить состояние после успешного расчёта
                _calculationStateService.ResetThermalState();
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Ошибка расчёта: {ex.Message}";
                Result = null;
                // При ошибке также сбросить состояние
                _calculationStateService.ResetThermalState();
            }
            finally
            {
                IsCalculating = false;
            }
        }

        /// <summary>
        /// Сбросить ViewModel к дефолтным значениям
        /// </summary>
        public void Reset()
        {
            _isResetting = true;
            try
            {
                SelectedMode = OperatingMode.Melting;
                SupplyTemperature = 50.0;
                GroundTemperature = 10.0;
                SelectedPipe = null;
                PipeSpacing = 200;
                Result = null;
                ValidationMessage = string.Empty;
            }
            finally
            {
                _isResetting = false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Получить параметры теплового расчёта
        /// </summary>
        public ThermalInputs BuildThermalInputs()
        {
            return new ThermalInputs
            {
                Mode = SelectedMode,
                SupplyTemperature = SupplyTemperature,
                DeltaT = 15.0, // Значение по умолчанию для совместимости с гидравлическим расчётом
                GroundTemperature = GroundTemperature,
                Pipe = SelectedPipe!, // Валидация гарантирует, что SelectedPipe не null при вызове
                PipeSpacing = PipeSpacing,
                LambdaE = _constructionData.LambdaE
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Валидация входных данных
        /// </summary>
        /// <returns>Результат валидации</returns>
        private ValidationResult ValidateInput()
        {
            var parameters = BuildThermalInputs();
            return _thermalValidator.Validate(parameters);
        }

        /// <summary>
        /// Обработчик изменения климатических данных
        /// </summary>
        private void OnClimateDataChanged(object? sender, ClimateDataChangedEventArgs e)
        {
            if (Result != null)
            {
                Result = null;
                _calculationStateService.SetThermalNeedsRecalculation("Климатические данные изменены. Требуется пересчёт.");
            }
        }

        /// <summary>
        /// Обработчик изменения данных конструкции
        /// </summary>
        private void OnConstructionDataChanged(object? sender, ConstructionDataChangedEventArgs e)
        {
            OnPropertyChanged(nameof(R1Total));
            OnPropertyChanged(nameof(R2Total));

            if (Result != null)
            {
                Result = null;
                _calculationStateService.SetThermalNeedsRecalculation("Данные конструкции изменены. Требуется пересчёт.");
            }
        }

        /// <summary>
        /// Обработчик изменения состояния расчёта
        /// </summary>
        private void OnCalculationStateChanged(object? sender, ModuleStateChangedEventArgs e)
        {
            // Уведомить UI об изменении свойств RecalcMessage и NeedsRecalculation
            OnPropertyChanged(nameof(RecalcMessage));
            OnPropertyChanged(nameof(NeedsRecalculation));
        }

        /// <summary>
        /// Обработчик изменения канонического шага укладки из ICalculationStateService
        /// </summary>
        private void OnPipeSpacingServiceChanged(object? sender, int spacing)
        {
            PipeSpacing = spacing;
        }

        #endregion
    }
}
