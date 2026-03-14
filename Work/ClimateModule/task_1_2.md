# Задача 1.2: Репозиторий и сервис данных

## Статус: ЗАВЕРШЕНО

## Описание
Реализовать репозиторий для загрузки данных из JSON и сервис для работы с климатическими данными.

## Созданные файлы

### src/Repositories/IClimateDataRepository.cs
- Интерфейс репозитория
- Методы: LoadCitiesAsync, GetCityByNameAsync, GetAllCities

### src/Repositories/ClimateDataRepository.cs
- Реализация репозитория
- Загрузка из data/climate_db.json
- Десериализация JSON с System.Text.Json
- Маппинг полей из JSON в CityInfo
- Кэширование загруженных данных

### src/Services/Climate/IClimateDataService.cs
- Интерфейс сервиса
- Методы: LoadClimateDataAsync, SearchCitiesAsync, GetCityByName, GetAllCities, DetermineZone

### src/Services/Climate/ClimateDataService.cs
- Реализация сервиса
- ConcurrentBag<CityInfo> для потокобезопасного кэша
- Асинхронный поиск с CancellationToken
- Логика определения климатической зоны

## Ключевые особенности

### Маппинг JSON → CityInfo
| JSON поле | CityInfo свойство |
|-----------|-------------------|
| city | Name |
| region | Region |
| t_5days_092 | T5Days092 |
| wind_max_jan | WindMaxJan |
| humidity_15h_cold | Humidity15hCold |
| t_cold_days_098 | TColdDays098 |
| t_abs_min | TAbsMin |

### Логика определения зоны
- t ≥ -27°C → Zone_M10
- -37°C < t < -27°C → Zone_M15
- t ≤ -37°C → Zone_M20
- Повышенные требования → Zone_M20_Plus

## Критерии приёмки
- ✅ Данные загружаются из JSON
- ✅ Поиск работает за ≤100 мс
- ✅ Определение зоны корректно для всех диапазонов
- ✅ Потокобезопасность обеспечена

## Следующий шаг
Задача 1.3: Реализация ViewModel