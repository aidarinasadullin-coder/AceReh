using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Services.Climate;

namespace SnowMeltingCalculator.ViewModels.Climate
{
    /// <summary>
    /// ViewModel для экрана климатических данных
    /// </summary>
    public partial class ClimateViewModel : ObservableObject
    {
        private readonly IClimateDataService _climateService;
        private readonly IClimateData _climateData;
        private CityInfo? _originalCityData;
        private CancellationTokenSource? _searchCts;

        #region Observable Properties

        /// <summary>
        /// Отфильтрованный список городов для отображения
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CityInfo> _filteredCities = new();

        /// <summary>
        /// Выбранный город
        /// </summary>
        [ObservableProperty]
        private CityInfo? _selectedCity;

        /// <summary>
        /// Текст поиска города
        /// </summary>
        [ObservableProperty]
        private string _searchQuery = string.Empty;

        partial void OnSearchQueryChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                LoadAllCitiesCommand.ExecuteAsync(null);
            }
            else if (value.Length >= 2)
            {
                SearchCitiesCommand.ExecuteAsync(null);
            }
        }

        /// <summary>
        /// Расчётная температура наружного воздуха, °C
        /// Диапазон: -50 до +10°C
        /// </summary>
        [ObservableProperty]
        private double _airTemperature = -15.0;

        /// <summary>
        /// Скорость ветра, м/с
        /// Диапазон: 0.1 до 30 м/с
        /// </summary>
        [ObservableProperty]
        private double _windSpeed = 5.0;

        /// <summary>
        /// Относительная влажность, %
        /// Диапазон: 20 до 100%
        /// </summary>
        [ObservableProperty]
        private double _humidity = 70.0;

        /// <summary>
        /// Интенсивность снегопада, см/ч
        /// Диапазон: 0 до 5 см/ч
        /// НЕ берётся из СП 131.13330.2025
        /// </summary>
        [ObservableProperty]
        private double _snowfallIntensity = 0.3;

        /// <summary>
        /// Выбранная климатическая зона
        /// </summary>
        [ObservableProperty]
        private ClimateZone _selectedZone = ClimateZone.Zone_M15;

        /// <summary>
        /// Признак повышенных требований
        /// </summary>
        [ObservableProperty]
        private bool _isHighRequirements;

        /// <summary>
        /// Сообщение об ошибке валидации
        /// </summary>
        [ObservableProperty]
        private string _validationMessage = string.Empty;

        /// <summary>
        /// Признак того, что пользователь изменил данные вручную
        /// </summary>
        [ObservableProperty]
        private bool _hasUserModifications;

        /// <summary>
        /// Признак загрузки данных
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Признак валидности данных
        /// </summary>
        public bool IsValid => ValidateAll();

        /// <summary>
        /// Описание климатической зоны
        /// </summary>
        public string ZoneDescription => SelectedZone switch
        {
            ClimateZone.Zone_M10 => "Колонка -10°C (t ≥ -27°C)",
            ClimateZone.Zone_M15 => "Колонка -15°C (-37°C < t < -27°C)",
            ClimateZone.Zone_M20 => "Колонка -20°C (t ≤ -37°C)",
            ClimateZone.Zone_M20_Plus => "Колонка -20°C (повышенные требования)",
            _ => string.Empty
        };

        /// <summary>
        /// Признак того, что данные загружены
        /// </summary>
        public bool IsDataLoaded => _climateService.IsLoaded;

        /// <summary>
        /// Количество загруженных городов
        /// </summary>
        public int CitiesCount => _climateService.CitiesCount;

        #endregion

        #region Events

        /// <summary>
        /// Событие изменения климатических данных
        /// </summary>
        public event EventHandler<ClimateDataChangedEventArgs>? DataChanged;

        /// <summary>
        /// Событие изменения валидации
        /// </summary>
        public event EventHandler<ValidationEventArgs>? ValidationChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Создать ViewModel
        /// </summary>
        public ClimateViewModel(IClimateDataService climateService, IClimateData climateData)
        {
            _climateService = climateService ?? throw new ArgumentNullException(nameof(climateService));
            _climateData = climateData ?? throw new ArgumentNullException(nameof(climateData));
        }

        #endregion

        #region Commands

        /// <summary>
        /// Команда поиска городов
        /// </summary>
        [RelayCommand]
        private async Task SearchCities()
        {
            // Отмена предыдущего поиска
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();

            try
            {
                var results = await _climateService.SearchCitiesAsync(
                    SearchQuery,
                    _searchCts.Token);

                FilteredCities.Clear();
                foreach (var city in results)
                {
                    FilteredCities.Add(city);
                }
            }
            catch (OperationCanceledException)
            {
                // Поиск отменён - это нормально
            }
        }

        /// <summary>
        /// Команда загрузки всех городов (для выпадающего списка)
        /// </summary>
        [RelayCommand]
        private async Task LoadAllCities()
        {
            if (!_climateService.IsLoaded)
            {
                await _climateService.LoadClimateDataAsync();
            }

            var cities = _climateService.GetAllCities().OrderBy(c => c.Name).Take(100);
            
            FilteredCities.Clear();
            foreach (var city in cities)
            {
                FilteredCities.Add(city);
            }
        }

        /// <summary>
        /// Команда сброса к дефолтным значениям
        /// </summary>
        [RelayCommand]
        private void ResetToDefaults()
        {
            SelectedCity = null;
            AirTemperature = -15.0;
            WindSpeed = 5.0;
            Humidity = 70.0;
            SnowfallIntensity = 0.3;
            SelectedZone = ClimateZone.Zone_M15;
            IsHighRequirements = false;
            HasUserModifications = false;
            SearchQuery = string.Empty;
            _originalCityData = null;

            OnDataChanged("Reset", null, null);
            SyncToClimateData();
        }

        /// <summary>
        /// Команда сброса к данным выбранного города
        /// </summary>
        [RelayCommand]
        private void ResetToCityData()
        {
            if (_originalCityData != null)
            {
                AirTemperature = _originalCityData.T5Days092;
                WindSpeed = _originalCityData.WindMaxJan;
                Humidity = _originalCityData.Humidity15hCold;
                SelectedZone = _climateService.DetermineZone(_originalCityData.T5Days092, IsHighRequirements);
                HasUserModifications = false;

                OnDataChanged("ResetToCity", null, _originalCityData);
                SyncToClimateData();
            }
        }

        /// <summary>
        /// Команда загрузки данных
        /// </summary>
        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                await _climateService.LoadClimateDataAsync();
                OnPropertyChanged(nameof(IsDataLoaded));
                OnPropertyChanged(nameof(CitiesCount));
                
                if (FilteredCities.Count == 0)
                {
                    await LoadAllCitiesCommand.ExecuteAsync(null);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Загрузить климатические данные
        /// </summary>
        public async Task LoadDataAsync()
        {
            await LoadDataCommand.ExecuteAsync(null);
        }

        /// <summary>
        /// Получить климатические данные для передачи другим модулям
        /// </summary>
        public IClimateData GetClimateData()
        {
            return new ClimateData
            {
                SelectedCity = SelectedCity?.Name ?? string.Empty,
                SelectedRegion = SelectedCity?.Region ?? string.Empty,
                AirTemperature = AirTemperature,
                ColdFiveDayTemperature = SelectedCity?.T5Days092 ?? AirTemperature,
                WindSpeed = WindSpeed,
                Humidity = Humidity,
                SnowfallIntensity = SnowfallIntensity,
                Zone = SelectedZone
            };
        }

        /// <summary>
        /// Установить данные из ClimateParameters
        /// </summary>
        public void SetClimateParameters(ClimateParameters parameters)
        {
            AirTemperature = parameters.AirTemperature;
            WindSpeed = parameters.WindSpeed;
            Humidity = parameters.Humidity;
            SnowfallIntensity = parameters.SnowfallIntensity;
            SelectedZone = parameters.Zone;
            IsHighRequirements = parameters.IsHighRequirements;
            HasUserModifications = parameters.HasUserModifications;
        }

        #endregion

        #region Property Changed Handlers

        /// <summary>
        /// Обработчик изменения выбранного города
        /// </summary>
        partial void OnSelectedCityChanged(CityInfo? value)
        {
            if (value != null)
            {
                _originalCityData = value;
                AirTemperature = value.T5Days092;
                WindSpeed = value.WindMaxJan;
                Humidity = value.Humidity15hCold;
                SelectedZone = _climateService.DetermineZone(value.T5Days092, IsHighRequirements);
                HasUserModifications = false;

                OnDataChanged("SelectedCity", null, value);
                SyncToClimateData();
            }
        }

        /// <summary>
        /// Обработчик изменения признака повышенных требований
        /// </summary>
        partial void OnIsHighRequirementsChanged(bool value)
        {
            if (value)
            {
                SelectedZone = ClimateZone.Zone_M20_Plus;
            }
            else if (SelectedCity != null)
            {
                SelectedZone = _climateService.DetermineZone(SelectedCity.T5Days092, false);
            }
            else
            {
                // Определить зону по текущей температуре
                SelectedZone = _climateService.DetermineZone(AirTemperature, false);
            }

            OnDataChanged("IsHighRequirements", !value, value);
            SyncToClimateData();
        }

        /// <summary>
        /// Обработчик изменения температуры
        /// </summary>
        partial void OnAirTemperatureChanged(double value)
        {
            HasUserModifications = true;
            ValidateAll();
            OnDataChanged("AirTemperature", null, value);
            SyncToClimateData();
        }

        /// <summary>
        /// Обработчик изменения скорости ветра
        /// </summary>
        partial void OnWindSpeedChanged(double value)
        {
            HasUserModifications = true;
            ValidateAll();
            OnDataChanged("WindSpeed", null, value);
            SyncToClimateData();
        }

        /// <summary>
        /// Обработчик изменения влажности
        /// </summary>
        partial void OnHumidityChanged(double value)
        {
            HasUserModifications = true;
            ValidateAll();
            OnDataChanged("Humidity", null, value);
            SyncToClimateData();
        }

        /// <summary>
        /// Обработчик изменения интенсивности снегопада
        /// </summary>
        partial void OnSnowfallIntensityChanged(double value)
        {
            HasUserModifications = true;
            ValidateAll();
            OnDataChanged("SnowfallIntensity", null, value);
            SyncToClimateData();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Валидация всех данных
        /// </summary>
        private bool ValidateAll()
        {
            var errors = new List<string>();

            if (AirTemperature < -50 || AirTemperature > 10)
            {
                errors.Add("Температура должна быть от -50°C до +10°C");
            }

            if (WindSpeed < 0.1 || WindSpeed > 30)
            {
                errors.Add("Скорость ветра от 0.1 до 30 м/с");
            }

            if (Humidity < 20 || Humidity > 100)
            {
                errors.Add("Влажность от 20% до 100%");
            }

            if (SnowfallIntensity < 0 || SnowfallIntensity > 5)
            {
                errors.Add("Интенсивность от 0 до 5 см/ч");
            }

            ValidationMessage = string.Join("; ", errors);
            var isValid = errors.Count == 0;

            ValidationChanged?.Invoke(this, new ValidationEventArgs
            {
                IsValid = isValid,
                Message = ValidationMessage
            });

            return isValid;
        }

        /// <summary>
        /// Вызвать событие изменения данных
        /// </summary>
        private void OnDataChanged(string propertyName, object? oldValue, object? newValue)
        {
            DataChanged?.Invoke(this, new ClimateDataChangedEventArgs
            {
                ChangedProperty = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                IsValid = IsValid
            });
        }

        /// <summary>
        /// Синхронизировать данные с singleton IClimateData
        /// </summary>
        private void SyncToClimateData()
        {
            if (_climateData is ClimateData data)
            {
                data.SelectedCity = SelectedCity?.Name ?? string.Empty;
                data.SelectedRegion = SelectedCity?.Region ?? string.Empty;
                data.AirTemperature = AirTemperature;
                data.WindSpeed = WindSpeed;
                data.Humidity = Humidity;
                data.SnowfallIntensity = SnowfallIntensity;
                data.Zone = SelectedZone;
                data.ColdFiveDayTemperature = SelectedCity?.T5Days092 ?? AirTemperature;

                data.RaiseDataChanged("Sync", null, null, IsValid);
            }
        }

        #endregion
    }
}