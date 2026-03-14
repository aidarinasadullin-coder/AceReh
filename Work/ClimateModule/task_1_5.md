# Задача 1.5: DI и интеграция

## Статус: ЗАВЕРШЕНО

## Описание
Настроить DI-контейнер и интегрировать модуль в приложение.

## Созданные файлы

### src/Configuration/ServiceCollectionExtensions.cs
- Метод AddClimateModule() для регистрации сервисов
- Метод AddApplicationServices() для всех сервисов

### src/Configuration/ViewModelLocator.cs
- Локатор ViewModels для привязки в XAML
- Инициализация сервисов при старте

### src/App.xaml.cs
- Настройка DI при запуске
- Загрузка климатических данных
- Создание главного окна

### src/App.xaml
- MaterialDesign темы
- Словарь ресурсов
- ViewModelLocator

## Ключевые особенности

### DI-регистрация
```csharp
services.AddSingleton<IClimateDataRepository, ClimateDataRepository>();
services.AddSingleton<IClimateDataService, ClimateDataService>();
services.AddSingleton<ClimateViewModel>();
services.AddSingleton<IClimateData, ClimateData>();
```

### Инициализация при старте
```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    var services = new ServiceCollection();
    services.AddApplicationServices();
    _serviceProvider = services.BuildServiceProvider();
    
    var climateService = _serviceProvider.GetRequiredService<IClimateDataService>();
    await climateService.LoadClimateDataAsync();
    
    var mainWindow = new MainWindow();
    mainWindow.Show();
}
```

### ViewModelLocator
- Используется в XAML для привязки ViewModel
- DataContext="{Binding ClimateViewModel, Source={StaticResource ViewModelLocator}}"

## Критерии приёмки
- ✅ DI-контейнер настроен
- ✅ Модуль загружается при старте
- ✅ Данные городов доступны

## Следующий шаг
Задача 1.6: Unit тесты