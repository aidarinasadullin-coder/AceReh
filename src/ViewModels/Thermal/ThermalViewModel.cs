using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Thermal;
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
        /// Температурный перепад, К
        /// </summary>
        [ObservableProperty]
        private double _deltaT = 15.0;

        /// <summary>
        /// Температура грунта, °C
        /// </summary>
        [ObservableProperty]
        private double _groundTemperature = 10.0;

        /// <summary>
        /// Выбранный тип трубы
        /// </summary>
        [ObservableProperty]
        private PipeType _selectedPipe = PipeType.StandardPipes[1]; // RAUTHERM S 20x2,0

        /// <summary>
        /// Шаг укладки трубы, мм
        /// </summary>
        [ObservableProperty]
        private double _pipeSpacing = 200.0;

        /// <summary>
        /// Результат расчёта
        /// </summary>
        [ObservableProperty]
        private ThermalCalculationResult? _result;

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
            IConstructionData constructionData)
        {
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _climateData = climateData ?? throw new ArgumentNullException(nameof(climateData));
            _constructionData = constructionData ?? throw new ArgumentNullException(nameof(constructionData));

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
            if (_constructionData is ConstructionData constructionDataImpl)
            {
                constructionDataImpl.DataChanged += OnConstructionDataChanged;
            }
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
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Ошибка расчёта: {ex.Message}";
                Result = null;
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
            DeltaT = 15.0;
            GroundTemperature = 10.0;
            SelectedPipe = PipeType.StandardPipes[1]; // RAUTHERM S 20x2,0
            PipeSpacing = 200.0;
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
                DeltaT = DeltaT,
                GroundTemperature = GroundTemperature,
                Pipe = SelectedPipe,
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

            // Валидация температуры подачи
            if (SupplyTemperature < 20 || SupplyTemperature > 90)
            {
                errors.Add("Температура подачи должна быть от 20°C до 90°C");
            }

            // Валидация температурного перепада
            if (DeltaT < 5 || DeltaT > 25)
            {
                errors.Add("Температурный перепад должен быть от 5 К до 25 К");
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

            if (_climateData.SnowfallIntensity < 0 || _climateData.SnowfallIntensity > 5)
            {
                errors.Add("Климатические данные: интенсивность снегопада должна быть от 0 до 5 см/ч");
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
            // При изменении климатических данных можно автоматически пересчитать
            // или просто сбросить результат
            if (Result != null)
            {
                Result = null;
                ValidationMessage = "Климатические данные изменены. Требуется пересчёт.";
            }
        }

        /// <summary>
        /// Обработчик изменения данных конструкции
        /// </summary>
        private void OnConstructionDataChanged(object? sender, ConstructionDataChangedEventArgs e)
        {
            // При изменении данных конструкции можно автоматически пересчитать
            // или просто сбросить результат
            if (Result != null)
            {
                Result = null;
                ValidationMessage = "Данные конструкции изменены. Требуется пересчёт.";
            }
        }

        #endregion
    }
}