# Задача 1.4: View (XAML)

## Статус: ЗАВЕРШЕНО

## Описание
Реализовать ClimateView.xaml с использованием MaterialDesignInXamlToolkit.

## Созданные файлы

### src/Views/Climate/ClimateView.xaml
- ComboBox с автокомплитом для выбора города
- TextBox для ввода климатических параметров
- ComboBox для выбора зоны
- CheckBox для повышенных требований
- Отображение информации о зоне
- Отображение ошибок валидации

### src/Views/Climate/ClimateView.xaml.cs
- Code-behind для UserControl

### src/Converters/Converters.cs
- NullToVisibilityConverter
- StringToVisibilityConverter
- BoolToVisibilityConverter
- InverseBoolToVisibilityConverter

### src/Resources/Dictionary.xaml
- Словарь ресурсов
- Конвертеры
- Значения климатических зон для ComboBox

## Ключевые особенности

### Элементы UI
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

### Стилизация
- MaterialDesign стили для всех элементов
- Цвета бренда РЕХАУ (PrimaryHueMidBrush)
- Иконки MaterialDesign (PackIcon)

### Валидация
- Красная рамка при ошибке
- Предупреждение с иконкой Alert
- ToolTip с подсказками

## Критерии приёмки
- ✅ UI соответствует ТЗ
- ✅ Все привязки работают
- ✅ Валидация отображается
- ✅ MaterialDesign стили применены

## Следующий шаг
Задача 1.5: DI-регистрация и интеграция