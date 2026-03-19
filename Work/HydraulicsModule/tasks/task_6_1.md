# Task 6.1: DI-регистрация сервисов

**Этап:** 6 - Интеграция  
**Приоритет:** Высокий  
**Статус:** К разработке  
**Зависимости:** Task 3.2 (CircuitsCalculator)

---

## 1. Цель задачи

Создать методы расширения для DI-регистрации сервисов модуля "Контура".

---

## 2. Создаваемые файлы

### 6.1. HydraulicsServiceCollectionExtensions.cs

**Путь:** `src/Services/Hydraulics/HydraulicsServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using SnowMeltingCalculator.Services.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Методы расширения для DI-регистрации сервисов модуля "Контура"
    /// </summary>
    public static class HydraulicsServiceCollectionExtensions
    {
        /// <summary>
        /// Добавить сервисы модуля "Контура" в DI-контейнер
        /// </summary>
        public static IServiceCollection AddHydraulicsServices(this IServiceCollection services)
        {
            // Сервисы для работы с гликолями
            services.AddSingleton<IGlycolDataService, GlycolDataService>();

            // Калькуляторы
            services.AddSingleton<ICircuitsCalculator, CircuitsCalculator>();

            // ViewModels
            services.AddTransient<CircuitsViewModel>();

            return services;
        }
    }
}
```

---

## 3. Использование

В `App.xaml.cs` или `MainWindow.xaml.cs`:

```csharp
services.AddHydraulicsServices();
```

---

## 4. Критерии приёмки

- [ ] Файл создан
- [ ] Все сервисы зарегистрированы
- [ ] DI контейнер работает корректно
- [ ] Зависимости разрешены

---

## 5. Примечания

- `GlycolDataService` — Singleton (один экземпляр на всё приложение)
- `CircuitsCalculator` — Singleton (без состояния)
- `CircuitsViewModel` — Transient (новый экземпляр для каждого View)

---

## 6. Связанные задачи

- Task 3.2: CircuitsCalculator — регистрируется как Singleton
- Task 4.1: CircuitsViewModel — регистрируется как Transient

---

*Дата создания: 2026-03-17*