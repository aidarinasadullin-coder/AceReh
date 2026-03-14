# Архитектура модуля климатических данных

## Калькулятор снеготаяния РЕХАУ

**Версия:** 1.0  
**Дата:** 15.03.2026  
**Статус:** Утверждено  
**Автор:** Архитектор

---

## 1. Обзор архитектуры

### 1.1. Назначение
Модуль климатических данных обеспечивает выбор и настройку климатических параметров для теплового расчёта систем снеготаяния. Модуль является первым шагом в цепочке расчёта.

### 1.2. Диаграмма компонентов

```
┌─────────────────────────────────────────────────────────────────┐
│                         View Layer                               │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    ClimateView.xaml                      │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │    │
│  │  │ CitySearch  │  │ ParamFields │  │ ZoneSelector│     │    │
│  │  │ (ComboBox)  │  │ (TextBoxes) │  │ (ComboBox)  │     │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘     │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │ Data Binding
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       ViewModel Layer                            │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                  ClimateViewModel                         │    │
│  │  - SelectedCity: CityInfo                                │    │
│  │  - AirTemperature: double                                │    │
│  │  - WindSpeed: double                                     │    │
│  │  - Humidity: double                                     │    │
│  │  - SnowfallIntensity: double                             │    │
│  │  - SelectedZone: ClimateZone                             │    │
│  │  - IsValid: bool                                         │    │
│  │  + SearchCommand, ResetCommand                           │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │ IClimateDataService
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Service Layer                             │
│  ┌───────────────────────┐  ┌───────────────────────────────┐  │
│  │  ClimateDataService   │  │  ClimateDataRepository        │  │
│  │  - LoadCitiesAsync()  │  │  - LoadFromJson()             │  │
│  │  - SearchCities()     │  │  - GetCityByName()            │  │
│  │  - ValidateData()     │  │  - GetAllCities()             │  │
│  └───────────────────────┘  └───────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │ JSON
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Data Layer                               │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              climate_db.json (550 городов)                │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

### 1.3. Поток данных

```
1. Пользователь вводит текст поиска
   ↓
2. ClimateViewModel.SearchCommand → ClimateDataService.SearchCities()
   ↓
3. Возврат отфильтрованных городов (≤20 результатов)
   ↓
4. Пользователь выбирает город
   ↓
5. ClimateViewModel.SelectedCity → автозаполнение полей
   ↓
6. Определение ClimateZone по t_5days_092
   ↓
7. Событие DataChanged → уведомление других модулей
```

---

## 2. Слои приложения

### 2.1. Model Layer (Модели данных)

#### Расположение
`src/Models/Climate/`

#### Классы

##### CityInfo.cs
```csharp
namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Информация о городе из климатического справочника
    /// </summary>
    public class CityInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public double T5Days092 { get; set; }      // Температура холодной пятидневки
        public double WindMaxJan { get; set; }     // Макс. ветер в январе
        public double Humidity15hCold { get; set; } // Влажность в 15ч холодного периода
        public double TColdDays098 { get; set; }   // Температура холодных суток (0.98)
        public double TAbsMin { get; set; }        // Абсолютный минимум
        
        public string DisplayName => $"{Name} ({Region})";
    }
}
```

##### ClimateZone.cs
```csharp
namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Климатическая зона для выбора мощности
    /// </summary>
    public enum ClimateZone
    {
        /// <summary>
        /// t_5days_092 ≥ -27°C (колонка -10°C)
        /// </summary>
        Zone_M10 = 0,
        
        /// <summary>
        /// -37°C < t_5days_092 < -27°C (колонка -15°C)
        /// </summary>
        Zone_M15 = 1,
        
        /// <summary>
        /// t_5days_092 ≤ -37°C (колонка -20°C)
        /// </summary>
        Zone_M20 = 2,
        
        /// <summary>
        /// Повышенные требования (колонка -20°C)
        /// </summary>
        Zone_M20_Plus = 3
    }
}
```

##### ClimateParameters.cs
```csharp
namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Климатические параметры для расчёта
    /// </summary>
    public class ClimateParameters
    {
        public string CityName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public double AirTemperature { get; set; } = -15.0;
        public double WindSpeed { get; set; } = 5.0;
        public double Humidity { get; set; } = 70.0;
        public double SnowfallIntensity { get; set; } = 0.3;
        public ClimateZone Zone { get; set; } = ClimateZone.Zone_M15;
        public bool IsHighRequirements { get; set; } = false;
        public bool HasUserModifications { get; set; } = false;
    }
}
```

##### ClimateData.cs
```csharp
namespace SnowMeltingCalculator.Models.Climate
{
    /// <summary>
    /// Полный набор климатических данных (реализация IClimateData)
    /// </summary>
    public class ClimateData : IClimateData
    {
        public string SelectedCity { get; set; } = string.Empty;
        public string SelectedRegion { get; set; } = string.Empty;
        public double AirTemperature { get; set; }
        public double ColdFiveDayTemperature { get; set; }
        public double WindSpeed { get; set; }
        public double Humidity { get; set; }
        public double SnowfallIntensity { get; set; }
        public ClimateZone Zone { get; set; }
        
        public event EventHandler<ClimateDataChangedEventArgs>? DataChanged;
        
        public void RaiseDataChanged(string propertyName, object? oldValue, object? newValue)
        {
            DataChanged?.Invoke(this, new ClimateDataChangedEventArgs
            {
                ChangedProperty = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                IsValid = true
            });
        }
    }
}
```

##### ClimateDataChangedEventArgs.cs
```csharp
namespace SnowMeltingCalculator.Models.Climate
{
    public class ClimateDataChangedEventArgs : EventArgs
    {
        public string ChangedProperty { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public bool IsValid { get; set; }
    }
    
    public class ValidationEventArgs : EventArgs
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
    }
}
```

---

### 2.2. ViewModel Layer

#### Расположение
`src/ViewModels/Climate/`

#### ClimateViewModel.cs
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Services.Climate;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

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

        [ObservableProperty]
        private ObservableCollection<CityInfo> _filteredCities = new();

        [ObservableProperty]
        private CityInfo? _selectedCity;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private double _airTemperature = -15.0;

        [ObservableProperty]
        private double _windSpeed = 5.0;

        [ObservableProperty]
        private double _humidity = 70.0;

        [ObservableProperty]
        private double _snowfallIntensity = 0.3;

        [ObservableProperty]
        private ClimateZone _selectedZone = ClimateZone.Zone_M15;

        [ObservableProperty]
        private bool _isHighRequirements;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        [ObservableProperty]
        private bool _hasUserModifications;

        [ObservableProperty]
        private bool _isLoading;

        #endregion

        #region Computed Properties

        public bool IsValid => ValidateAll();

        public string ZoneDescription => SelectedZone switch
        {
            ClimateZone.Zone_M10 => "Колонка -10°C (t ≥ -27°C)",
            ClimateZone.Zone_M15 => "Колонка -15°C (-37°C < t < -27°C)",
            ClimateZone.Zone_M20 => "Колонка -20°C (t ≤ -37°C)",
            ClimateZone.Zone_M20_Plus => "Колонка -20°C (повышенные требования)",
            _ => string.Empty
        };

        #endregion

        #region Events

        public event EventHandler<ClimateDataChangedEventArgs>? DataChanged;
        public event EventHandler<ValidationEventArgs>? ValidationChanged;

        #endregion

        #region Commands

        [RelayCommand]
        private async Task SearchCities()
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();

            try
            {
                var results = await _climateService.SearchCitiesAsync(
                    SearchQuery, 
                    _searchCts.Token);
                
                FilteredCities.Clear();
                foreach (var city in results.Take(20))
                {
                    FilteredCities.Add(city);
                }
            }
            catch (OperationCanceledException)
            {
                // Поиск отменён
            }
        }

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
            
            OnDataChanged("Reset", null, null);
        }

        [RelayCommand]
        private void ResetToCityData()
        {
            if (_originalCityData != null)
            {
                AirTemperature = _originalCityData.T5Days092;
                WindSpeed = _originalCityData.WindMaxJan;
                Humidity = _originalCityData.Humidity15hCold;
                SelectedZone = DetermineZone(_originalCityData.T5Days092);
                HasUserModifications = false;
            }
        }

        #endregion

        #region Constructor

        public ClimateViewModel(IClimateDataService climateService, IClimateData climateData)
        {
            _climateService = climateService;
            _climateData = climateData;
        }

        #endregion

        #region Public Methods

        public async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                await _climateService.LoadClimateDataAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

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

        #endregion

        #region Private Methods

        partial void OnSelectedCityChanged(CityInfo? value)
        {
            if (value != null)
            {
                _originalCityData = value;
                AirTemperature = value.T5Days092;
                WindSpeed = value.WindMaxJan;
                Humidity = value.Humidity15hCold;
                SelectedZone = DetermineZone(value.T5Days092);
                HasUserModifications = false;
                
                OnDataChanged("SelectedCity", null, value);
            }
        }

        partial void OnIsHighRequirementsChanged(bool value)
        {
            if (value)
            {
                SelectedZone = ClimateZone.Zone_M20_Plus;
            }
            else if (SelectedCity != null)
            {
                SelectedZone = DetermineZone(SelectedCity.T5Days092);
            }
            
            OnDataChanged("IsHighRequirements", !value, value);
        }

        private ClimateZone DetermineZone(double t5days)
        {
            if (IsHighRequirements)
                return ClimateZone.Zone_M20_Plus;
            
            if (t5days >= -27)
                return ClimateZone.Zone_M10;
            
            if (t5days > -37)
                return ClimateZone.Zone_M15;
            
            return ClimateZone.Zone_M20;
        }

        private bool ValidateAll()
        {
            var errors = new List<string>();

            if (AirTemperature < -50 || AirTemperature > 10)
                errors.Add("Температура должна быть от -50°C до +10°C");

            if (WindSpeed < 0.1 || WindSpeed > 30)
                errors.Add("Скорость ветра от 0.1 до 30 м/с");

            if (Humidity < 20 || Humidity > 100)
                errors.Add("Влажность от 20% до 100%");

            if (SnowfallIntensity < 0.1 || SnowfallIntensity > 5)
                errors.Add("Интенсивность от 0.1 до 5 см/ч");

            ValidationMessage = string.Join("; ", errors);
            var isValid = errors.Count == 0;
            
            ValidationChanged?.Invoke(this, new ValidationEventArgs
            {
                IsValid = isValid,
                Message = ValidationMessage
            });

            return isValid;
        }

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

        #endregion
    }
}
```

---

### 2.3. Service Layer

#### Расположение
`src/Services/Climate/`

#### IClimateDataService.cs
```csharp
namespace SnowMeltingCalculator.Services.Climate
{
    public interface IClimateDataService
    {
        Task LoadClimateDataAsync();
        Task<IEnumerable<CityInfo>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default);
        CityInfo? GetCityByName(string name);
        IEnumerable<CityInfo> GetAllCities();
        ClimateZone DetermineZone(double t5days, bool isHighRequirements = false);
    }
}
```

#### ClimateDataService.cs
```csharp
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Repositories;
using System.Collections.Concurrent;

namespace SnowMeltingCalculator.Services.Climate
{
    /// <summary>
    /// Сервис для работы с климатическими данными
    /// </summary>
    public class ClimateDataService : IClimateDataService
    {
        private readonly IClimateDataRepository _repository;
        private readonly ConcurrentBag<CityInfo> _citiesCache = new();
        private bool _isLoaded = false;
        private readonly object _loadLock = new();

        public ClimateDataService(IClimateDataRepository repository)
        {
            _repository = repository;
        }

        public async Task LoadClimateDataAsync()
        {
            if (_isLoaded) return;

            lock (_loadLock)
            {
                if (_isLoaded) return;
            }

            var cities = await _repository.LoadCitiesAsync();
            
            foreach (var city in cities)
            {
                _citiesCache.Add(city);
            }

            _isLoaded = true;
        }

        public Task<IEnumerable<CityInfo>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Task.FromResult(Enumerable.Empty<CityInfo>());
            }

            var results = _citiesCache
                .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Name)
                .Take(20);

            return Task.FromResult(results);
        }

        public CityInfo? GetCityByName(string name)
        {
            return _citiesCache.FirstOrDefault(c => 
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<CityInfo> GetAllCities()
        {
            return _citiesCache.AsEnumerable();
        }

        public ClimateZone DetermineZone(double t5days, bool isHighRequirements = false)
        {
            if (isHighRequirements)
                return ClimateZone.Zone_M20_Plus;

            if (t5days >= -27)
                return ClimateZone.Zone_M10;

            if (t5days > -37)
                return ClimateZone.Zone_M15;

            return ClimateZone.Zone_M20;
        }
    }
}
```

#### IClimateDataRepository.cs
```csharp
namespace SnowMeltingCalculator.Repositories
{
    public interface IClimateDataRepository
    {
        Task<IEnumerable<CityInfo>> LoadCitiesAsync();
        Task<CityInfo?> GetCityByNameAsync(string name);
    }
}
```

#### ClimateDataRepository.cs
```csharp
using System.Text.Json;
using SnowMeltingCalculator.Models.Climate;

namespace SnowMeltingCalculator.Repositories
{
    /// <summary>
    /// Репозиторий для загрузки климатических данных из JSON
    /// </summary>
    public class ClimateDataRepository : IClimateDataRepository
    {
        private readonly string _dataPath;
        private List<CityInfo>? _cities;

        public ClimateDataRepository(string dataPath = "data/climate_db.json")
        {
            _dataPath = dataPath;
        }

        public async Task<IEnumerable<CityInfo>> LoadCitiesAsync()
        {
            if (_cities != null)
                return _cities;

            var jsonContent = await File.ReadAllTextAsync(_dataPath);
            var climateData = JsonSerializer.Deserialize<ClimateDbModel>(jsonContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            _cities = climateData?.Cities?.Select(c => new CityInfo
            {
                Name = c.City,
                Region = c.Region,
                T5Days092 = c.T_5days_092,
                WindMaxJan = c.Wind_Max_Jan,
                Humidity15hCold = c.Humidity_15h_Cold,
                TColdDays098 = c.T_Cold_Days_098,
                TAbsMin = c.T_Abs_Min
            }).ToList() ?? new List<CityInfo>();

            return _cities;
        }

        public async Task<CityInfo?> GetCityByNameAsync(string name)
        {
            var cities = await LoadCitiesAsync();
            return cities.FirstOrDefault(c => 
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        #region JSON Models

        private class ClimateDbModel
        {
            public ClimateMeta? Meta { get; set; }
            public List<CityJsonModel>? Cities { get; set; }
        }

        private class ClimateMeta
        {
            public string? Date { get; set; }
            public int Total_Cities { get; set; }
            public string? Source { get; set; }
            public string? Version { get; set; }
        }

        private class CityJsonModel
        {
            public string? City { get; set; }
            public string? Region { get; set; }
            public double T_5days_092 { get; set; }
            public double Wind_Max_Jan { get; set; }
            public double Humidity_15h_Cold { get; set; }
            public double T_Cold_Days_098 { get; set; }
            public double T_Abs_Min { get; set; }
        }

        #endregion
    }
}
```

---

### 2.4. View Layer

#### Расположение
`src/Views/Climate/`

#### ClimateView.xaml
```xml
<UserControl x:Class="SnowMeltingCalculator.Views.Climate.ClimateView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:vm="clr-namespace:SnowMeltingCalculator.ViewModels.Climate"
             DataContext="{Binding ClimateViewModel, Source={StaticResource ViewModelLocator}}">

    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Заголовок -->
        <TextBlock Grid.Row="0" 
                   Text="Климатические данные"
                   Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                   Margin="0,0,0,16"/>

        <!-- Выбор города -->
        <materialDesign:Card Grid.Row="1" Margin="0,0,0,16">
            <StackPanel Margin="16">
                <TextBlock Text="Выбор города"
                          Style="{StaticResource MaterialDesignSubtitle1TextBlock}"
                          Margin="0,0,0,8"/>
                
                <ComboBox ItemsSource="{Binding FilteredCities}"
                         SelectedItem="{Binding SelectedCity}"
                         Text="{Binding SearchQuery, UpdateSourceTrigger=PropertyChanged}"
                         materialDesign:ComboBoxAssist.ClassicMode="True"
                         IsEditable="True"
                         StaysOpenOnEdit="True">
                    <ComboBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel>
                                <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                                <TextBlock Text="{Binding Region}" FontSize="11" Foreground="Gray"/>
                            </StackPanel>
                        </DataTemplate>
                    </ComboBox.ItemTemplate>
                </ComboBox>
            </StackPanel>
        </materialDesign:Card>

        <!-- Параметры -->
        <materialDesign:Card Grid.Row="2" Margin="0,0,0,16">
            <Grid Margin="16">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <!-- Температура -->
                <TextBox Grid.Row="0" Grid.Column="0"
                        Text="{Binding AirTemperature, StringFormat={}{0:F1}}"
                        materialDesign:TextFieldAssist.PrefixText="Температура:"
                        materialDesign:TextFieldAssist.SuffixText="°C"
                        Margin="0,0,8,8"/>

                <!-- Ветер -->
                <TextBox Grid.Row="0" Grid.Column="1"
                        Text="{Binding WindSpeed, StringFormat={}{0:F1}}"
                        materialDesign:TextFieldAssist.PrefixText="Скорость ветра:"
                        materialDesign:TextFieldAssist.SuffixText="м/с"
                        Margin="8,0,0,8"/>

                <!-- Влажность -->
                <TextBox Grid.Row="1" Grid.Column="0"
                        Text="{Binding Humidity, StringFormat={}{0:F0}}"
                        materialDesign:TextFieldAssist.PrefixText="Влажность:"
                        materialDesign:TextFieldAssist.SuffixText="%"
                        Margin="0,0,8,8"/>

                <!-- Интенсивность снегопада -->
                <TextBox Grid.Row="1" Grid.Column="1"
                        Text="{Binding SnowfallIntensity, StringFormat={}{0:F1}}"
                        materialDesign:TextFieldAssist.PrefixText="Интенсивность снегопада:"
                        materialDesign:TextFieldAssist.SuffixText="см/ч"
                        Margin="8,0,0,8"/>

                <!-- Зона -->
                <ComboBox Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2"
                         ItemsSource="{Binding Source={StaticResource ZoneValues}}"
                         SelectedItem="{Binding SelectedZone}"
                         materialDesign:TextFieldAssist.PrefixText="Климатическая зона:"
                         Margin="0,0,0,8"/>

                <!-- Повышенные требования -->
                <CheckBox Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="2"
                         Content="Повышенные требования (колонка -20°C)"
                         IsChecked="{Binding IsHighRequirements}"
                         Margin="0,8,0,0"/>
            </Grid>
        </materialDesign:Card>

        <!-- Информация о зоне -->
        <materialDesign:Card Grid.Row="3" Margin="0,0,0,16">
            <StackPanel Margin="16" Orientation="Horizontal">
                <materialDesign:PackIcon Kind="Information" 
                                        VerticalAlignment="Center"
                                        Margin="0,0,8,0"/>
                <TextBlock Text="{Binding ZoneDescription}"
                          VerticalAlignment="Center"/>
            </StackPanel>
        </materialDesign:Card>

        <!-- Сообщение об ошибке -->
        <TextBlock Grid.Row="4"
                  Text="{Binding ValidationMessage}"
                  Foreground="Red"
                  Visibility="{Binding ValidationMessage, Converter={StaticResource StringToVisibilityConverter}}"/>
    </Grid>
</UserControl>
```

---

## 3. Взаимодействие компонентов

### 3.1. DI-регистрация

```csharp
// App.xaml.cs или Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Repositories
    services.AddSingleton<IClimateDataRepository, ClimateDataRepository>();
    
    // Services
    services.AddSingleton<IClimateDataService, ClimateDataService>();
    
    // ViewModels
    services.AddSingleton<ClimateViewModel>();
    
    // Data
    services.AddSingleton<IClimateData, ClimateData>();
}
```

### 3.2. События и подписки

```csharp
// Подписка на изменения данных в другом модуле
public class ThermalCalculationViewModel
{
    private readonly IClimateData _climateData;

    public ThermalCalculationViewModel(IClimateData climateData)
    {
        _climateData = climateData;
        _climateData.DataChanged += OnClimateDataChanged;
    }

    private void OnClimateDataChanged(object? sender, ClimateDataChangedEventArgs e)
    {
        // Пересчитать при изменении климатических данных
        Recalculate();
    }
}
```

---

## 4. Хранение данных

### 4.1. Загрузка JSON

```csharp
// Асинхронная загрузка при старте приложения
public async Task InitializeAsync()
{
    await _climateService.LoadClimateDataAsync();
}
```

### 4.2. Кэширование

- Справочник городов загружается один раз при старте
- Хранится в `ConcurrentBag<CityInfo>` для потокобезопасности
- Поиск выполняется в памяти (≤100 мс для 550 записей)

### 4.3. Потокобезопасность

- `ConcurrentBag<T>` для кэша городов
- `CancellationToken` для отмены поиска
- `lock` для инициализации

---

## 5. Обработка ошибок

### 5.1. Исключения

| Ситуация | Исключение | Обработка |
|----------|------------|-----------|
| Файл JSON не найден | `FileNotFoundException` | Показать сообщение, использовать дефолтные значения |
| Файл JSON повреждён | `JsonException` | Показать сообщение, использовать дефолтные значения |
| Город не найден | — | Показать сообщение «Город не найден» |
| Неверный ввод | — | Валидация на уровне ViewModel |

### 5.2. Валидация

```csharp
// Правила валидации
private bool ValidateAll()
{
    var errors = new List<string>();

    if (AirTemperature < -50 || AirTemperature > 10)
        errors.Add("Температура должна быть от -50°C до +10°C");

    if (WindSpeed < 0.1 || WindSpeed > 30)
        errors.Add("Скорость ветра от 0.1 до 30 м/с");

    if (Humidity < 20 || Humidity > 100)
        errors.Add("Влажность от 20% до 100%");

    if (SnowfallIntensity < 0.1 || SnowfallIntensity > 5)
        errors.Add("Интенсивность от 0.1 до 5 см/ч");

    return errors.Count == 0;
}
```

---

## 6. Тестирование

### 6.1. Unit тесты

```csharp
// Tests/Services/ClimateDataServiceTests.cs

[Test]
public async Task SearchCitiesAsync_WithValidQuery_ReturnsFilteredCities()
{
    // Arrange
    var service = CreateService();
    await service.LoadClimateDataAsync();

    // Act
    var results = await service.SearchCitiesAsync("Моск");

    // Assert
    Assert.That(results.Count(), Is.GreaterThan(0));
    Assert.That(results.All(c => c.Name.Contains("Моск")), Is.True);
}

[Test]
public void DetermineZone_WithTemperatureAboveMinus27_ReturnsZoneM10()
{
    // Arrange
    var service = CreateService();

    // Act
    var zone = service.DetermineZone(-20);

    // Assert
    Assert.That(zone, Is.EqualTo(ClimateZone.Zone_M10));
}

[Test]
public void DetermineZone_WithHighRequirements_ReturnsZoneM20Plus()
{
    // Arrange
    var service = CreateService();

    // Act
    var zone = service.DetermineZone(-20, isHighRequirements: true);

    // Assert
    Assert.That(zone, Is.EqualTo(ClimateZone.Zone_M20_Plus));
}
```

### 6.2. Интеграционные тесты

```csharp
// Tests/Integration/ClimateModuleIntegrationTests.cs

[Test]
public async Task ClimateViewModel_SelectCity_AutoFillsParameters()
{
    // Arrange
    var viewModel = CreateViewModel();
    await viewModel.LoadDataAsync();

    // Act
    viewModel.SearchQuery = "Майкоп";
    await viewModel.SearchCitiesCommand.ExecuteAsync(null);
    viewModel.SelectedCity = viewModel.FilteredCities.First();

    // Assert
    Assert.That(viewModel.AirTemperature, Is.EqualTo(-15)); // t_5days_092 для Майкопа
    Assert.That(viewModel.WindSpeed, Is.EqualTo(5.4));
    Assert.That(viewModel.Humidity, Is.EqualTo(68));
}
```

---

## 7. Диаграмма классов (текстовая)

```
┌─────────────────────────────────────────────────────────────────┐
│                         Models.Climate                           │
├─────────────────────────────────────────────────────────────────┤
│  CityInfo                                                        │
│  ├── Name: string                                                │
│  ├── Region: string                                              │
│  ├── T5Days092: double                                          │
│  ├── WindMaxJan: double                                         │
│  └── Humidity15hCold: double                                    │
│                                                                  │
│  ClimateZone (enum)                                              │
│  ├── Zone_M10                                                    │
│  ├── Zone_M15                                                    │
│  ├── Zone_M20                                                    │
│  └── Zone_M20_Plus                                               │
│                                                                  │
│  ClimateParameters                                                │
│  ├── CityName: string                                           │
│  ├── AirTemperature: double                                      │
│  ├── WindSpeed: double                                          │
│  ├── Humidity: double                                            │
│  ├── SnowfallIntensity: double                                   │
│  ├── Zone: ClimateZone                                          │
│  └── IsHighRequirements: bool                                    │
│                                                                  │
│  ClimateData : IClimateData                                      │
│  └── (реализация интерфейса)                                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       ViewModels.Climate                         │
├─────────────────────────────────────────────────────────────────┤
│  ClimateViewModel : ObservableObject                            │
│  ├── FilteredCities: ObservableCollection<CityInfo>             │
│  ├── SelectedCity: CityInfo?                                     │
│  ├── AirTemperature: double                                      │
│  ├── WindSpeed: double                                          │
│  ├── Humidity: double                                            │
│  ├── SnowfallIntensity: double                                   │
│  ├── SelectedZone: ClimateZone                                  │
│  ├── IsHighRequirements: bool                                    │
│  ├── IsValid: bool (computed)                                   │
│  │                                                               │
│  ├── + SearchCitiesCommand                                       │
│  ├── + ResetToDefaultsCommand                                   │
│  ├── + ResetToCityDataCommand                                   │
│  │                                                               │
│  ├── event DataChanged                                           │
│  └── event ValidationChanged                                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Services.Climate                          │
├─────────────────────────────────────────────────────────────────┤
│  IClimateDataService                                             │
│  ├── LoadClimateDataAsync()                                      │
│  ├── SearchCitiesAsync(query)                                    │
│  ├── GetCityByName(name)                                         │
│  ├── GetAllCities()                                              │
│  └── DetermineZone(t5days, isHighRequirements)                   │
│                                                                  │
│  ClimateDataService : IClimateDataService                        │
│  └── (реализация)                                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Repositories                             │
├─────────────────────────────────────────────────────────────────┤
│  IClimateDataRepository                                          │
│  ├── LoadCitiesAsync()                                           │
│  └── GetCityByNameAsync(name)                                    │
│                                                                  │
│  ClimateDataRepository : IClimateDataRepository                  │
│  └── (загрузка из JSON)                                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 8. Расширяемость

### 8.1. Добавление новых климатических параметров
1. Добавить поле в `CityInfo`
2. Обновить маппинг в `ClimateDataRepository`
3. Добавить свойство в `ClimateViewModel`

### 8.2. Добавление новых зон
1. Добавить значение в `ClimateZone`
2. Обновить метод `DetermineZone`

### 8.3. Интеграция с онлайн-сервисами погоды
1. Создать `IWeatherApiService`
2. Реализовать `OpenWeatherMapService`
3. Добавить fallback на локальные данные

---

## 9. История изменений

| Версия | Дата | Автор | Изменения |
|--------|------|-------|-----------|
| 1.0 | 15.03.2026 | Архитектор | Начальная версия |