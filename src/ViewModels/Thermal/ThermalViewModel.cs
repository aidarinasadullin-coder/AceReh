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
            ICalculationStateService calculationStateService)
        {
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _climateData = climateData ?? throw new ArgumentNullException(nameof(climateData));
            _constructionData = constructionData ?? throw new ArgumentNullException(nameof(constructionData));
            _calculationStateService = calculationStateService ?? throw new ArgumentNullException(nameof(calculationStateService));

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
        }

        #endregion

        #region Commands

        /// <summary>
        /// Команда выполнения расчёта
        /// </summary>
        [RelayCommand]
        private async Task Calculate()
        {
            if (IsCalculating) return;

            // Валидация
            if (!ValidateInput())
            {
                return;
            }

            // Установить флаг выполнения расчёта
            _calculationStateService.SetThermalCalculating();
            IsCalculating = true;
            ValidationMessage = string.Empty;

            try
            {
                // 1. Собрать ThermalParameters из свойств
                var parameters = BuildThermalParameters();

                // 2. Получить климатические данные из IClimateData
                parameters.AirTemperature = _climateData.AirTemperature;
                parameters.WindSpeed = _climateData.WindSpeed;
                parameters.SnowfallIntensity = _climateData.SnowfallIntensity;

                // 3. Получить данные конструкции из IConstructionData
                parameters.R1Total = _constructionData.R1Total;
                parameters.R2Total = _constructionData.R2Total;
                parameters.LambdaE = _constructionData.LambdaE;

                // 4. Вызвать _calculator.Calculate(parameters)
                Result = await Task.Run(() => _calculator.Calculate(parameters));

                // 5. Отобразить ошибки в ValidationMessage
                if (Result != null && !Result.IsValid && Result.ValidationErrors.Length > 0)
                {
                    ValidationMessage = string.Join("; ", Result.ValidationErrors);
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
        /// Команда сброса к дефолтным значениям
        /// </summary>
        [RelayCommand]
        private void Reset()
        {
            SelectedMode = OperatingMode.Melting;
            SupplyTemperature = 50.0;
            GroundTemperature = 10.0;
            SelectedPipe = null;
            PipeSpacing = 200;
            Result = null;
            ValidationMessage = string.Empty;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Получить параметры теплового расчёта
        /// </summary>
        public ThermalParameters BuildThermalParameters()
        {
            return new ThermalParameters
            {
                Mode = SelectedMode,
                SupplyTemperature = SupplyTemperature,
                DeltaT = 15.0, // Значение по умолчанию для совместимости с гидравлическим расчётом
                GroundTemperature = GroundTemperature,
                Pipe = SelectedPipe!, // Валидация гарантирует, что SelectedPipe не null при вызове
                PipeSpacing = PipeSpacing,
                AirTemperature = _climateData.AirTemperature,
                WindSpeed = _climateData.WindSpeed,
                SnowfallIntensity = _climateData.SnowfallIntensity,
                R1Total = _constructionData.R1Total,
                R2Total = _constructionData.R2Total,
                LambdaE = _constructionData.LambdaE
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Валидация входных данных
        /// </summary>
        private bool ValidateInput()
        {
            var errors = new List<string>();

            // Валидация выбранной трубы
            if (SelectedPipe == null)
            {
                errors.Add("Необходимо выбрать тип трубы");
            }

            // Валидация температуры подачи
            if (SupplyTemperature < 20 || SupplyTemperature > 90)
            {
                errors.Add("Температура подачи должна быть от 20°C до 90°C");
            }

            // Валидация температуры грунта
            if (GroundTemperature < -10 || GroundTemperature > 30)
            {
                errors.Add("Температура грунта должна быть от -10°C до +30°C");
            }

            // Валидация шага укладки трубы
            if (PipeSpacing < 100 || PipeSpacing > 500)
            {
                errors.Add("Шаг укладки трубы должен быть от 100 мм до 500 мм");
            }

            // Валидация климатических данных
            if (!_climateData.AirTemperature.Equals(0) && 
                (_climateData.AirTemperature < -50 || _climateData.AirTemperature > 10))
            {
                errors.Add("Климатические данные: температура должна быть от -50°C до +10°C");
            }

            if (_climateData.WindSpeed < 0.1 || _climateData.WindSpeed > 30)
            {
                errors.Add("Климатические данные: скорость ветра должна быть от 0.1 до 30 м/с");
            }

            if (_climateData.SnowfallIntensity < 0 || _climateData.SnowfallIntensity > 20)
            {
                errors.Add("Климатические данные: интенсивность снегопада должна быть от 0 до 20 мм/ч");
            }

            // Валидация данных конструкции
            if (!_constructionData.IsValid)
            {
                errors.Add("Данные конструкции не валидны");
            }

            ValidationMessage = string.Join("; ", errors);
            return errors.Count == 0;
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

        #endregion
    }
}