# Задача 1.3: ViewModel

## Статус: ЗАВЕРШЕНО

## Описание
Реализовать ClimateViewModel с использованием CommunityToolkit.Mvvm.

## Созданные файлы

### src/ViewModels/Climate/ClimateViewModel.cs
- Наследование от ObservableObject
- Использование [ObservableProperty] для свойств
- Использование [RelayCommand] для команд
- Реализация валидации
- События DataChanged и ValidationChanged

## Ключевые особенности

### Observable Properties
| Свойство | Тип | Описание |
|----------|-----|----------|
| FilteredCities | ObservableCollection<CityInfo> | Отфильтрованные города |
| SelectedCity | CityInfo? | Выбранный город |
| SearchQuery | string | Текст поиска |
| AirTemperature | double | Температура (-50 до +10°C) |
| WindSpeed | double | Скорость ветра (0.1-30 м/с) |
| Humidity | double | Влажность (20-100%) |
| SnowfallIntensity | double | Интенсивность снегопада (0-50 мм/ч) |
| SelectedZone | ClimateZone | Климатическая зона |
| IsHighRequirements | bool | Повышенные требования |
| IsValid | bool | Валидность (computed) |
| ValidationMessage | string | Сообщение об ошибке |

### Commands
| Команда | Действие |
|---------|----------|
| SearchCitiesCommand | Поиск городов |
| ResetToDefaultsCommand | Сброс к дефолтным значениям |
| ResetToCityDataCommand | Сброс к данным города |
| LoadDataCommand | Загрузка данных |

### Property Changed Handlers
- OnSelectedCityChanged — автозаполнение при выборе города
- OnIsHighRequirementsChanged — изменение зоны
- OnAirTemperatureChanged — валидация
- OnWindSpeedChanged — валидация
- OnHumidityChanged — валидация
- OnSnowfallIntensityChanged — валидация

### Валидация
- Температура: -50 до +10°C
- Скорость ветра: 0.1 до 30 м/с
- Влажность: 20 до 100%
- Интенсивность снегопада: 0 до 50 мм/ч

## Критерии приёмки
- ✅ Все свойства привязаны к UI
- ✅ Команды работают корректно
- ✅ Валидация срабатывает при изменении данных
- ✅ Автозаполнение при выборе города работает

## Следующий шаг
Задача 1.4: Реализация View (XAML)