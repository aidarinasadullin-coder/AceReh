# Task 6.1: DI-регистрация сервисов

**Этап:** 6 - Integration  
**Приоритет:** Высокий  
**Статус:** Не начато  
**Зависимости:** Task 3.1, Task 3.3, Task 3.4, Task 3.5, Task 4.1

---

## 1. Цель задачи

Создать методы расширения для DI-регистрации сервисов модуля гидравлики.

---

## 2. Создаваемые файлы

### 6.1. HydraulicsServiceCollectionExtensions.cs

**Путь:** `src/Configuration/HydraulicsServiceCollectionExtensions.cs`

**Регистрация:**
```csharp
services.AddSingleton<ICollectorRepository, CollectorRepository>();
services.AddSingleton<IHydraulicCalculator, HydraulicCalculator>();
services.AddSingleton<IGlycolDataService, GlycolDataService>();
services.AddSingleton<HydraulicValidator>();
services.AddSingleton<HydraulicsViewModel>();
```

---

## 3. Критерии приёмки

- [ ] Файл создан
- [ ] Все сервисы зарегистрированы
- [ ] DI работает корректно