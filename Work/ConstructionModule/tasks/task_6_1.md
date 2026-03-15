# Task 6.1: Обновить ServiceCollectionExtensions.cs (DI)

**Этап:** 6. Интеграция  
**Приоритет:** P0 (Критическая)  
**Время:** 1 час  
**Зависимости:** Task 2.1, Task 2.2, Task 3.1, Task 4.1

---

## 1. Цель задачи

Обновить регистрацию сервисов в DI для модуля "Конструктор конструкции".

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|----------|----------|
| UC-09 | Интеграция с ThermalViewModel | DI регистрация |

---

## 3. Описание изменений

### 3.1. Обновить файл ServiceCollectionExtensions.cs

**Файл:** `src/Configuration/ServiceCollectionExtensions.cs`

**Добавить метод:**

```csharp
/// <summary>
/// Регистрация сервисов модуля "Конструктор конструкции"
/// </summary>
public static IServiceCollection AddConstructionModule(this IServiceCollection services)
{
    // Repositories
    services.AddSingleton<IMaterialRepository, MaterialRepository>();
    services.AddSingleton<IConstructionRepository, ConstructionRepository>();

    // Services
    services.AddSingleton<IConstructionService, ConstructionService>();
    services.AddSingleton<ConstructionValidator>();

    // Models
    services.AddSingleton<Construction>(); // Реализация IConstructionData

    // ViewModels
    services.AddSingleton<ConstructionViewModel>();

    // Регистрация IConstructionData (заменяет заглушку ConstructionData)
    services.AddSingleton<IConstructionData>(sp => sp.GetRequiredService<Construction>());

    return services;
}
```

### 3.2. Обновить метод AddApplicationServices

```csharp
/// <summary>
/// Регистрация всех сервисов приложения
/// </summary>
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    return services
        .AddClimateModule()
        .AddConstructionModule()  // НОВЫЙ МОДУЛЬ
        .AddThermalModule();
}
```

### 3.3. Удалить заглушку ConstructionData

**Файл:** `src/Models/Thermal/IConstructionData.cs`

**Удалить класс `ConstructionData` (заглушку)**, оставить только интерфейс:

```csharp
namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Интерфейс для передачи данных конструкции другим модулям
    /// </summary>
    public interface IConstructionData
    {
        /// <summary>
        /// Суммарное термическое сопротивление слоёв над трубой, м²·К/Вт
        /// </summary>
        double R1Total { get; }

        /// <summary>
        /// Суммарное термическое сопротивление слоёв под трубой, м²·К/Вт
        /// </summary>
        double R2Total { get; }

        /// <summary>
        /// Теплопроводность стяжки (бетона) вокруг трубы, Вт/м·К
        /// </summary>
        double LambdaE { get; }

        /// <summary>
        /// Признак валидности данных конструкции
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Событие изменения данных
        /// </summary>
        event EventHandler<ConstructionDataChangedEventArgs>? DataChanged;
    }

    /// <summary>
    /// Аргументы события изменения данных конструкции
    /// </summary>
    public class ConstructionDataChangedEventArgs : EventArgs
    {
        public string? ChangedProperty { get; set; }
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public bool IsValid { get; set; } = true;
    }
}
```

---

## 4. Тест-кейсы

### TC-6.1.1: Регистрация сервисов

```csharp
[Fact]
public void ServiceCollection_AddConstructionModule_ShouldRegisterServices()
{
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddConstructionModule();

    // Assert
    Assert.Contains(services, s => s.ServiceType == typeof(IMaterialRepository));
    Assert.Contains(services, s => s.ServiceType == typeof(IConstructionRepository));
    Assert.Contains(services, s => s.ServiceType == typeof(IConstructionService));
    Assert.Contains(services, s => s.ServiceType == typeof(ConstructionValidator));
    Assert.Contains(services, s => s.ServiceType == typeof(Construction));
    Assert.Contains(services, s => s.ServiceType == typeof(ConstructionViewModel));
    Assert.Contains(services, s => s.ServiceType == typeof(IConstructionData));
}
```

### TC-6.1.2: Разрешение IConstructionData

```csharp
[Fact]
public void ServiceProvider_GetRequiredService_IConstructionData_ShouldReturnConstruction()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddConstructionModule();
    var serviceProvider = services.BuildServiceProvider();

    // Act
    var constructionData = serviceProvider.GetRequiredService<IConstructionData>();

    // Assert
    Assert.IsType<Construction>(constructionData);
}
```

### TC-6.1.3: Разрешение ConstructionViewModel

```csharp
[Fact]
public void ServiceProvider_GetRequiredService_ConstructionViewModel_ShouldResolve()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddConstructionModule();
    var serviceProvider = services.BuildServiceProvider();

    // Act
    var viewModel = serviceProvider.GetRequiredService<ConstructionViewModel>();

    // Assert
    Assert.NotNull(viewModel);
    Assert.NotNull(viewModel.AvailableMaterials);
}
```

---

## 5. Критерии приёмки

- [ ] Метод `AddConstructionModule()` добавлен
- [ ] Все сервисы зарегистрированы в DI
- [ ] `IConstructionData` разрешается в `Construction`
- [ ] `ConstructionViewModel` разрешается с зависимостями
- [ ] Заглушка `ConstructionData` удалена
- [ ] Unit-тесты проходят

---

## 6. Примечания

- `Construction` регистрируется как Singleton
- `IConstructionData` указывает на тот же экземпляр `Construction`
- `ConstructionViewModel` получает зависимости через конструктор

---

**Конец документа**