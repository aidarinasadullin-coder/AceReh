# Задача 1.6: Unit тесты

## Статус: ЗАВЕРШЕНО

## Описание
Написать unit тесты для репозитория, сервиса и ViewModel.

## Созданные файлы

### tests/SnowMeltingCalculator.Tests/Climate/ClimateDataServiceTests.cs
- Тесты для ClimateDataService
- MockClimateDataRepository для тестов

### tests/SnowMeltingCalculator.Tests/Climate/ClimateViewModelTests.cs
- Тесты для ClimateViewModel
- MockClimateDataService для тестов

## Тест-кейсы

### ClimateDataServiceTests
| Тест | Описание |
|------|----------|
| SearchCitiesAsync_ValidQuery_ReturnsFilteredCities | Поиск возвращает города |
| SearchCitiesAsync_EmptyQuery_ReturnsEmpty | Пустой запрос возвращает пустой список |
| SearchCitiesAsync_ShortQuery_ReturnsEmpty | Короткий запрос возвращает пустой список |
| SearchCitiesAsync_ReturnsMax20Results | Максимум 20 результатов |
| DetermineZone_AboveMinus27_ReturnsZoneM10 | t ≥ -27 → Zone_M10 |
| DetermineZone_ExactlyMinus27_ReturnsZoneM10 | t = -27 → Zone_M10 |
| DetermineZone_BetweenMinus27AndMinus37_ReturnsZoneM15 | -37 < t < -27 → Zone_M15 |
| DetermineZone_ExactlyMinus37_ReturnsZoneM20 | t = -37 → Zone_M20 |
| DetermineZone_BelowMinus37_ReturnsZoneM20 | t ≤ -37 → Zone_M20 |
| DetermineZone_HighRequirements_ReturnsZoneM20Plus | Повышенные требования → Zone_M20_Plus |
| GetCityByName_ExistingCity_ReturnsCity | Поиск существующего города |
| GetCityByName_NonExistingCity_ReturnsNull | Поиск несуществующего города |
| GetCityByName_CaseInsensitive_ReturnsCity | Поиск без учёта регистра |
| LoadClimateDataAsync_LoadsDataSuccessfully | Загрузка данных |
| LoadClimateDataAsync_CalledTwice_LoadsOnce | Загрузка один раз |

### ClimateViewModelTests
| Тест | Описание |
|------|----------|
| SelectCity_AutoFillsParameters | Выбор города автозаполняет поля |
| SelectCity_DeterminesCorrectZone | Выбор города определяет зону |
| SetHighRequirements_ChangesZone | Повышенные требования меняют зону |
| UnsetHighRequirements_RestoresZone | Отмена повышенных требований восстанавливает зону |
| Validate_InvalidTemperature_ReturnsFalse | Невалидная температура |
| Validate_InvalidWindSpeed_ReturnsFalse | Невалидная скорость ветра |
| Validate_InvalidHumidity_ReturnsFalse | Невалидная влажность |
| Validate_InvalidSnowfallIntensity_ReturnsFalse | Невалидная интенсивность |
| Validate_ValidData_ReturnsTrue | Валидные данные проходят |
| ResetToDefaults_ClearsAllFields | Сброс очищает поля |
| ResetToCityData_RestoresCityValues | Сброс к данным города |
| GetClimateData_ReturnsCorrectData | Получение данных |

## Критерии приёмки
- ✅ Все тесты проходят
- ✅ Покрытие кода ≥ 80%
- ✅ Граничные случаи протестированы

## Следующий шаг
Финальное ревью кода и завершение разработки модуля