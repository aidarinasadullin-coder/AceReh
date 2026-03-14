# План разработки: Модуль климатических данных

## Калькулятор снеготаяния РЕХАУ

**Версия:** 1.0  
**Дата:** 15.03.2026  
**Статус:** Утверждено  
**Автор:** Планировщик

---

## 1. Обзор плана

### 1.1. Цель
Реализовать модуль климатических данных для Калькулятора снеготаяния РЕХАУ в соответствии с ТЗ и архитектурой.

### 1.2. Входные документы
- `Work/ClimateModule/technical_specification.md` — Техническое задание
- `Work/ClimateModule/architecture.md` — Архитектура модуля

### 1.3. Структура плана
План разбит на 6 задач, каждая из которых представляет логически завершённый этап разработки.

---

## 2. Задачи

### Задача 1.1: Создание структуры проекта и моделей данных

**Приоритет:** Высокий  
**Зависимости:** Нет  
**Оценка:** 1 час

#### Описание
Создать структуру папок проекта и классы моделей данных для климатического модуля.

#### Файлы для создания
```
src/
├── Models/
│   └── Climate/
│       ├── CityInfo.cs
│       ├── ClimateZone.cs
│       ├── ClimateParameters.cs
│       ├── ClimateData.cs
│       └── ClimateDataChangedEventArgs.cs
```

#### Детали реализации

##### CityInfo.cs
```csharp
namespace SnowMeltingCalculator.Models.Climate
{
    public class CityInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public double T5Days092 { get; set; }
        public double WindMaxJan { get; set; }
        public double Humidity15hCold { get; set; }
        public double TColdDays098 { get; set; }
        public double TAbsMin { get; set; }
        public string DisplayName => $"{Name} ({Region})";
    }
}
```

##### ClimateZone.cs
```csharp
namespace SnowMeltingCalculator.Models.Climate
{
    public enum ClimateZone
    {
        Zone_M10 = 0,      // t ≥ -27°C
        Zone_M15 = 1,      // -37 < t < -27°C
        Zone_M20 = 2,      // t ≤ -37°C
        Zone_M20_Plus = 3  // Повышенные требования
    }
}
```

##### ClimateParameters.cs
```csharp
namespace SnowMeltingCalculator.Models.Climate
{
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
    public interface IClimateData
    {
        string SelectedCity { get; }
        string SelectedRegion { get; }
        double AirTemperature { get; }
        double ColdFiveDayTemperature { get; }
        double WindSpeed { get; }
        double Humidity { get; }
        double SnowfallIntensity { get; }
        ClimateZone Zone { get; }
        event EventHandler<ClimateDataChangedEventArgs>? DataChanged;
    }

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

#### Критерии приёмки
- ✅ Все файлы созданы в правильных папках
- ✅ Классы компилируются без ошибок
- ✅ Интерфейс IClimateData определён
- ✅ Enum ClimateZone содержит все зоны

---

### Задача 1.2: Реализация репозитория и сервиса данных

**Приоритет:** Высокий  
**Зависимости:** Задача 1.1  
**Оценка:** 2 часа

#### Описание
Реализовать репозиторий для загрузки данных из JSON и сервис для работы с климатическими данными.

#### Файлы для создания
```
src/
├── Repositories/
│   ├── IClimateDataRepository.cs
│   └── ClimateDataRepository.cs
├── Services/
│   └── Climate/
│       ├── IClimateDataService.cs
│       └── ClimateDataService.cs
```

#### Детали реализации

##### IClimateDataRepository.cs
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

##### ClimateDataRepository.cs
- Загрузка данных из `data/climate_db.json`
- Десериализация JSON с `System.Text.Json`
- Маппинг полей из JSON в модель CityInfo
- Кэширование загруженных данных

##### IClimateDataService.cs
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

##### ClimateDataService.cs
- Реализация всех методов интерфейса
- Использование `ConcurrentBag<CityInfo>` для кэша
- Потокобезопасная инициализация
- Логика определения климатической зоны

#### Критерии приёмки
- ✅ Данные загружаются из JSON
- ✅ Поиск работает за ≤100 мс
- ✅ Определение зоны корректно для всех диапазонов
- ✅ Потокобезопасность обеспечена

---

### Задача 1.3: Реализация ViewModel

**Приоритет:** Высокий  
**Зависимости:** Задача 1.2  
**Оценка:** 3 часа

#### Описание
Реализовать ClimateViewModel с использованием CommunityToolkit.Mvvm.

#### Файлы для создания
```
src/
├── ViewModels/
│   └── Climate/
│       └── ClimateViewModel.cs
```

#### Детали реализации

##### ClimateViewModel.cs
- Наследование от `ObservableObject`
- Использование `[ObservableProperty]` для свойств
- Использование `[RelayCommand]` для команд
- Реализация валидации
- События `DataChanged` и `ValidationChanged`

#### Свойства ViewModel
| Свойство | Тип | Описание |
|----------|-----|----------|
| FilteredCities | ObservableCollection<CityInfo> | Отфильтрованные города |
| SelectedCity | CityInfo? | Выбранный город |
| SearchQuery | string | Текст поиска |
| AirTemperature | double | Температура |
| WindSpeed | double | Скорость ветра |
| Humidity | double | Влажность |
| SnowfallIntensity | double | Интенсивность снегопада |
| SelectedZone | ClimateZone | Климатическая зона |
| IsHighRequirements | bool | Повышенные требования |
| IsValid | bool | Валидность данных |
| ValidationMessage | string | Сообщение об ошибке |

#### Команды ViewModel
| Команда | Действие |
|---------|----------|
| SearchCitiesCommand | Поиск городов |
| ResetToDefaultsCommand | Сброс к дефолтным значениям |
| ResetToCityDataCommand | Сброс к данным выбранного города |

#### Критерии приёмки
- ✅ Все свойства привязаны к UI
- ✅ Команды работают корректно
- ✅ Валидация срабатывает при изменении данных
- ✅ Автозаполнение при выборе города работает

---

### Задача 1.4: Реализация View (XAML)

**Приоритет:** Высокий  
**Зависимости:** Задача 1.3  
**Оценка:** 2 часа

#### Описание
Реализовать ClimateView.xaml с использованием MaterialDesignInXamlToolkit.

#### Файлы для создания
```
src/
├── Views/
│   └── Climate/
│       ├── ClimateView.xaml
│       └── ClimateView.xaml.cs
```

#### Детали реализации

##### ClimateView.xaml
- ComboBox с автокомплитом для выбора города
- TextBox для ввода климатических параметров
- ComboBox для выбора зоны
- CheckBox для повышенных требований
- Отображение информации о зоне
- Отображение ошибок валидации

##### Элементы UI
| Элемент | Привязка | Описание |
|---------|----------|----------|
| ComboBox (город) | SelectedCity, SearchQuery | Выбор города с поиском |
| TextBox (температура) | AirTemperature | Ввод температуры |
| TextBox (ветер) | WindSpeed | Ввод скорости ветра |
| TextBox (влажность) | Humidity | Ввод влажности |
| TextBox (снегопад) | SnowfallIntensity | Ввод интенсивности |
| ComboBox (зона) | SelectedZone | Выбор зоны |
| CheckBox (повыш.) | IsHighRequirements | Повышенные требования |
| TextBlock (инфо) | ZoneDescription | Информация о зоне |
| TextBlock (ошибка) | ValidationMessage | Сообщение об ошибке |

#### Критерии приёмки
- ✅ UI соответствует ТЗ
- ✅ Все привязки работают
- ✅ Валидация отображается
- ✅ MaterialDesign стили применены

---

### Задача 1.5: DI-регистрация и интеграция

**Приоритет:** Высокий  
**Зависимости:** Задача 1.4  
**Оценка:** 1 час

#### Описание
Настроить DI-контейнер и интегрировать модуль в приложение.

#### Файлы для изменения
```
src/
├── App.xaml.cs (или Startup.cs)
├── MainWindow.xaml (добавить навигацию)
```

#### Детали реализации

##### DI-регистрация
```csharp
// ConfigureServices
services.AddSingleton<IClimateDataRepository, ClimateDataRepository>();
services.AddSingleton<IClimateDataService, ClimateDataService>();
services.AddSingleton<ClimateViewModel>();
services.AddSingleton<IClimateData, ClimateData>();
```

##### Инициализация при старте
```csharp
// App.xaml.cs или MainWindow.xaml.cs
protected override async void OnStartup(StartupEventArgs e)
{
    var climateService = _serviceProvider.GetRequiredService<IClimateDataService>();
    await climateService.LoadClimateDataAsync();
}
```

#### Критерии приёмки
- ✅ DI-контейнер настроен
- ✅ Модуль загружается при старте
- ✅ Данные городов доступны

---

### Задача 1.6: Unit тесты

**Приоритет:** Средний  
**Зависимости:** Задача 1.5  
**Оценка:** 2 часа

#### Описание
Написать unit тесты для репозитория, сервиса и ViewModel.

#### Файлы для создания
```
tests/
├── SnowMeltingCalculator.Tests/
│   └── Climate/
│       ├── ClimateDataServiceTests.cs
│       ├── ClimateDataRepositoryTests.cs
│       └── ClimateViewModelTests.cs
```

#### Тест-кейсы

##### ClimateDataServiceTests
| Тест | Описание |
|------|----------|
| SearchCitiesAsync_ValidQuery_ReturnsFilteredCities | Поиск возвращает города |
| SearchCitiesAsync_EmptyQuery_ReturnsEmpty | Пустой запрос возвращает пустой список |
| DetermineZone_AboveMinus27_ReturnsZoneM10 | t ≥ -27 → Zone_M10 |
| DetermineZone_BetweenMinus27AndMinus37_ReturnsZoneM15 | -37 < t < -27 → Zone_M15 |
| DetermineZone_BelowMinus37_ReturnsZoneM20 | t ≤ -37 → Zone_M20 |
| DetermineZone_HighRequirements_ReturnsZoneM20Plus | Повышенные требования → Zone_M20_Plus |

##### ClimateDataRepositoryTests
| Тест | Описание |
|------|----------|
| LoadCitiesAsync_ReturnsAllCities | Загрузка возвращает все города |
| LoadCitiesAsync_InvalidPath_ThrowsException | Неверный путь выбрасывает исключение |
| GetCityByNameAsync_ExistingCity_ReturnsCity | Поиск существующего города |
| GetCityByNameAsync_NonExistingCity_ReturnsNull | Поиск несуществующего города |

##### ClimateViewModelTests
| Тест | Описание |
|------|----------|
| SelectCity_AutoFillsParameters | Выбор города автозаполняет поля |
| SetHighRequirements_ChangesZone | Повышенные требования меняют зону |
| Validate_InvalidTemperature_ReturnsFalse | Невалидная температура |
| Validate_ValidData_ReturnsTrue | Валидные данные проходят |
| ResetToDefaults_ClearsAllFields | Сброс очищает поля |

#### Критерии приёмки
- ✅ Все тесты проходят
- ✅ Покрытие кода ≥ 80%
- ✅ Граничные случаи протестированы

---

## 3. Порядок выполнения задач

```
Задача 1.1 (Модели)
    │
    ▼
Задача 1.2 (Репозиторий и сервис)
    │
    ▼
Задача 1.3 (ViewModel)
    │
    ▼
Задача 1.4 (View)
    │
    ▼
Задача 1.5 (DI и интеграция)
    │
    ▼
Задача 1.6 (Тесты)
```

---

## 4. Оценка времени

| Задача | Оценка | Приоритет |
|--------|--------|-----------|
| 1.1 Модели данных | 1 час | Высокий |
| 1.2 Репозиторий и сервис | 2 часа | Высокий |
| 1.3 ViewModel | 3 часа | Высокий |
| 1.4 View (XAML) | 2 часа | Высокий |
| 1.5 DI и интеграция | 1 час | Высокий |
| 1.6 Тесты | 2 часа | Средний |
| **Итого** | **11 часов** | |

---

## 5. Риски и митигация

| Риск | Вероятность | Влияние | Митигация |
|------|-------------|---------|-----------|
| Несовпадение структуры JSON | Средняя | Высокое | Проверить структуру climate_db.json перед реализацией |
| Проблемы с MaterialDesign | Низкая | Среднее | Использовать стандартные стили как fallback |
| Медленный поиск | Низкая | Среднее | Оптимизировать с помощью индексов |

---

## 6. Критерии завершения модуля

### Функциональные критерии
- ✅ UC-1: Выбор города из справочника работает
- ✅ UC-2: Ручной ввод параметров работает
- ✅ UC-3: Повышенные требования переключают зону
- ✅ UC-4: Интенсивность снегопада вводится вручную

### Нефункциональные критерии
- ✅ Поиск города ≤ 100 мс
- ✅ Загрузка справочника ≤ 500 мс
- ✅ Все тесты проходят
- ✅ Покрытие кода ≥ 80%

---

## 7. История изменений

| Версия | Дата | Автор | Изменения |
|--------|------|-------|-----------|
| 1.0 | 15.03.2026 | Планировщик | Начальная версия |