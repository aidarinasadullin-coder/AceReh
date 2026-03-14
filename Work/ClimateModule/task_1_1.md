# Задача 1.1: Модели данных

## Статус: ЗАВЕРШЕНО

## Описание
Создать структуру папок проекта и классы моделей данных для климатического модуля.

## Созданные файлы

### src/Models/Climate/CityInfo.cs
- Класс информации о городе
- Свойства: Name, Region, T5Days092, WindMaxJan, Humidity15hCold, TColdDays098, TAbsMin
- DisplayName для отображения в UI

### src/Models/Climate/ClimateZone.cs
- Enum климатических зон
- Zone_M10, Zone_M15, Zone_M20, Zone_M20_Plus

### src/Models/Climate/ClimateParameters.cs
- Класс параметров для расчёта
- Все климатические параметры с дефолтными значениями
- Метод Clone()

### src/Models/Climate/ClimateData.cs
- Интерфейс IClimateData
- Класс ClimateData с реализацией
- Событие DataChanged

### src/Models/Climate/ClimateDataChangedEventArgs.cs
- Класс ClimateDataChangedEventArgs
- Класс ValidationEventArgs

## Критерии приёмки
- ✅ Все файлы созданы в правильных папках
- ✅ Классы компилируются без ошибок
- ✅ Интерфейс IClimateData определён
- ✅ Enum ClimateZone содержит все зоны

## Следующий шаг
Задача 1.2: Реализация репозитория и сервиса данных