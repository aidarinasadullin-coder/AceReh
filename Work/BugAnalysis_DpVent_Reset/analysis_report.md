# АНАЛИЗ БАГА: Сброс DpVent при переключении вкладок

## Дата анализа: 2026-03-25

---

## 1. КРАТКОЕ ОПИСАНИЕ ПРОБЛЕМЫ

**Симптом:** При переключении между вкладками значение `DpVent` сбрасывается. Помогает только нажатие кнопки "Рассчитать".

**Ожидаемое поведение:** Значения должны сохраняться при переключении вкладок.

---

## 2. АНАЛИЗ КОДА

### 2.1. Навигация между вкладками (MainWindow.xaml.cs)

**Файл:** `src/MainWindow.xaml.cs`, строки 314-345

```csharp
private void NavigateToView(MenuItem menuItem)
{
    CurrentView = menuItem.Title switch
    {
        "Климат" => new ClimateView { DataContext = _climateViewModel },
        "Тепловой расчёт" => new ThermalView { DataContext = _thermalViewModel },
        "Конструкция" => new ConstructionView { DataContext = _constructionViewModel },
        "Гидравлический расчёт" => new CircuitsView { DataContext = _circuitsViewModel },
        "Результаты" => new CircuitsResultsView { DataContext = _circuitsViewModel },
        _ => new ClimateView { DataContext = _climateViewModel }
    };
}
```

**КЛЮЧЕВАЯ ПРОБЛЕМА #1:** При каждом переключении вкладки создаётся **НОВЫЙ экземпляр View**.

**Но:** DataContext устанавливается на Singleton ViewModel (`_circuitsViewModel`), поэтому данные должны сохраняться.

### 2.2. Регистрация ViewModel (ServiceCollectionExtensions.cs)

**Файл:** `src/Configuration/ServiceCollectionExtensions.cs`, строка 112

```csharp
// ViewModels - Singleton для модуля "Контура" (сохранение состояния между навигациями)
services.AddSingleton<CircuitsViewModel>();
```

**Подтверждение:** `CircuitsViewModel` зарегистрирован как Singleton, данные сохраняются между навигациями.

### 2.3. Свойство CurrentResult (CircuitRow.cs)

**Файл:** `src/Models/Hydraulics/CircuitRow.cs`, строки 517-523

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(CurrentResult))]
[NotifyPropertyChangedFor(nameof(FlowRegimeDescription))]
[NotifyPropertyChangedFor(nameof(TotalLoss_mbar))]
private HydraulicMode _displayMode = HydraulicMode.OperatingTemperature;

public CircuitTemperatureResult CurrentResult => 
    DisplayMode == HydraulicMode.DesignTemperature ? DesignResult : OperatingResult;
```

**Важно:** `CurrentResult` — вычисляемое свойство, которое зависит от `DisplayMode`.

### 2.4. Метод Calculate() (CircuitsViewModel.cs)

**Файл:** `src/ViewModels/Hydraulics/CircuitsViewModel.cs`, строки 443-535

```csharp
[RelayCommand]
private void Calculate()
{
    // ... расчёт ...

    foreach (var circuit in collector.Circuits)
    {
        // Создаётся НОВЫЙ объект OperatingResult
        var operatingResult = _circuitsCalculator.CalculateAtTemperature(...);
        circuit.OperatingResult = operatingResult;  // <-- ПРИСВАИВАЕТСЯ НОВЫЙ ОБЪЕКТ

        // Создаётся НОВЫЙ объект DesignResult
        var designResult = _circuitsCalculator.CalculateAtTemperature(...);
        circuit.DesignResult = designResult;  // <-- ПРИСВАИВАЕТСЯ НОВЫЙ ОБЪЕКТ

        circuit.DisplayMode = CurrentMode;
    }

    // ... summary ...

    // ВЫЗЫВАЕТСЯ CalculateBalancing
    _circuitsCalculator.CalculateBalancing(
        new System.Collections.Generic.List<CircuitRow>(collector.Circuits),
        collector.ValveType
    );

    foreach (var circuit in collector.Circuits)
    {
        circuit.DisplayMode = CurrentMode;
    }
}
```

**КЛЮЧЕВАЯ ПРОБЛЕМА #2:** В `Calculate()`:
1. Создаётся **новый** `CircuitTemperatureResult` для `OperatingResult` и `DesignResult`
2. В этих новых объектах `DpVent` рассчитывается с **Kv по умолчанию** (в `CalculateAtTemperature`)
3. Затем вызывается `CalculateBalancing()`, который **ПЕРЕЗАПИСЫВАЕТ** `DpVent`

### 2.5. Метод CalculateAtTemperature() (CircuitsCalculator.cs)

**Файл:** `src/Services/Hydraulics/CircuitsCalculator.cs`, строки 234-253

```csharp
// DpVerteiler и DpVent — формулы меняются местами для HKV-D и IV
if (valveType == ValveType.HKV_D)
{
    // HKV-D: формулы меняются местами
    // DpVerteiler = (V_dot/1000/1.2)² × 100000 × ρ/1000
    result.DpVerteiler = Math.Pow(circuit.FlowRate / 1000.0 / 1.2, 2) * 100000 * density_g_cm3;

    // DpVent = 15000 × (ρ/2000) × v²
    result.DpVent = 15000 * (density_g_cm3 / 2) * Math.Pow(velocity, 2);
}
else
{
    // IV 1¼" и IV 1½": стандартные формулы
    // DpVerteiler = 15000 × (ρ/2000) × v²
    result.DpVerteiler = 15000 * (density_g_cm3 / 2) * Math.Pow(velocity, 2);

    // DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000
    result.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;
}
```

**Важно:** Для **HKV-D** формула `DpVent` **НЕ зависит от Kv**:
- `DpVent = 15000 × (ρ/2000) × v²`

### 2.6. Метод CalculateBalancing() (CircuitsCalculator.cs)

**Файл:** `src/Services/Hydraulics/CircuitsCalculator.cs`, строки 441-465

```csharp
// Пересчитать потери на клапане при текущих оборотах для ОБОИХ режимов
foreach (var circuit in activeCircuits)
{
    // Рассчитать Kv для текущих оборотов
    double kv = ValveTurnsCalculator.CalculateKvFromTurns(circuit.ValveTurns, valveType);

    // === Рабочая температура ===
    double densityOperating = circuit.OperatingResult.Density;
    circuit.OperatingResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * densityOperating;

    // === Расчётная температура (холодный пуск) ===
    if (circuit.DesignResult != null)
    {
        double densityDesign = circuit.DesignResult.Density;
        circuit.DesignResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * densityDesign;
    }
}
```

---

## 3. КОРНЕВАЯ ПРОБЛЕМА

### 3.1. НЕПРАВИЛЬНАЯ ФОРМУЛА ДЛЯ HKV-D В CalculateBalancing

**В `CalculateBalancing` для ВСЕХ типов клапанов используется формула:**
```csharp
circuit.OperatingResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * densityOperating;
```

**Но для HKV-D формула должна быть:**
```csharp
result.DpVent = 15000 * (density_g_cm3 / 2) * Math.Pow(velocity, 2);
```

**ЭТО НЕ ЗАВИСИТ ОТ Kv!**

### 3.2. ПОЧЕМУ DpVent СБРАСЫВАЕТСЯ ПРИ ПЕРЕКЛЮЧЕНИИ ВКЛАДОК?

**Сценарий:**
1. Пользователь находится на вкладке "Гидравлический расчёт"
2. Нажимает "Рассчитать" → `Calculate()` выполняется
3. `CalculateAtTemperature` рассчитывает `DpVent` по **ПРАВИЛЬНОЙ** формуле для HKV-D
4. `CalculateBalancing` **ПЕРЕЗАПИСЫВАЕТ** `DpVent` по **НЕПРАВИЛЬНОЙ** формуле
5. Пользователь переключается на вкладку "Тепловой расчёт"
6. Изменяет данные (например, температуру подачи)
7. `ThermalViewModel.Result` изменяется
8. Срабатывает событие `PropertyChanged`
9. `CircuitsViewModel` получает событие и вызывает `UpdateFromThermalModule()` → `Calculate()`
10. `Calculate()` пересоздаёт `OperatingResult` и `DesignResult`
11. `DpVent` пересчитывается по **ПРАВИЛЬНОЙ** формуле в `CalculateAtTemperature`
12. Затем `CalculateBalancing` **ПЕРЕЗАПИСЫВАЕТ** `DpVent` по **НЕПРАВИЛЬНОЙ** формуле

**Но:** Если данные НЕ изменялись, `Calculate()` НЕ вызывается, и `DpVent` должен сохраняться.

### 3.3. ДОПОЛНИТЕЛЬНАЯ ПРОБЛЕМА: АВТОМАТИЧЕСКИЙ ВЫЗОВ Calculate()

**Файл:** `src/ViewModels/Hydraulics/CircuitsViewModel.cs`, строки 686-702

```csharp
private void OnThermalViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(ThermalViewModel.Result))
    {
        UpdateFromThermalModule();  // <-- ВЫЗЫВАЕТ Calculate()
        // ...
    }
}
```

**Проблема:** При любом изменении `ThermalViewModel.Result` вызывается `Calculate()`, который пересоздаёт `OperatingResult` и `DesignResult`.

---

## 4. ДИАГНОСТИКА

### 4.1. Проверка вызова Calculate() при переключении вкладок

**Гипотеза:** `Calculate()` вызывается при переключении вкладок из-за изменения `ThermalViewModel.Result`.

**Проверка:**
1. Добавить логирование в `Calculate()`:
   ```csharp
   [RelayCommand]
   private void Calculate()
   {
       System.Diagnostics.Debug.WriteLine($"[Calculate] Вызван в {DateTime.Now:HH:mm:ss.fff}");
       // ...
   }
   ```

2. Добавить логирование в `OnThermalViewModelPropertyChanged`:
   ```csharp
   private void OnThermalViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
   {
       System.Diagnostics.Debug.WriteLine($"[OnThermalViewModelPropertyChanged] {e.PropertyName}");
       // ...
   }
   ```

3. Запустить приложение и переключаться между вкладками
4. Проверить, вызывается ли `Calculate()` при переключении

### 4.2. Проверка формулы DpVent для HKV-D

**Гипотеза:** Формула `DpVent` для HKV-D в `CalculateBalancing` неправильная.

**Проверка:**
1. Рассчитать `DpVent` вручную для HKV-D по формуле:
   - `DpVent = 15000 × (ρ/2000) × v²`
2. Сравнить с результатом в приложении
3. Если результаты разные — формула неправильная

---

## 5. РЕКОМЕНДАЦИИ ПО ИСПРАВЛЕНИЮ

### 5.1. ИСПРАВИТЬ CalculateBalancing ДЛЯ HKV-D

**Файл:** `src/Services/Hydraulics/CircuitsCalculator.cs`, строки 441-465

**Текущий код:**
```csharp
// Пересчитать потери на клапане при текущих оборотах для ОБОИХ режимов
foreach (var circuit in activeCircuits)
{
    // Рассчитать Kv для текущих оборотов
    double kv = ValveTurnsCalculator.CalculateKvFromTurns(circuit.ValveTurns, valveType);

    // === Рабочая температура ===
    double densityOperating = circuit.OperatingResult.Density;
    circuit.OperatingResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * densityOperating;

    // === Расчётная температура (холодный пуск) ===
    if (circuit.DesignResult != null)
    {
        double densityDesign = circuit.DesignResult.Density;
        circuit.DesignResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * densityDesign;
    }
}
```

**Исправленный код:**
```csharp
// Пересчитать потери на клапане при текущих оборотах для ОБОИХ режимов
foreach (var circuit in activeCircuits)
{
    // === HKV-D: DpVent НЕ зависит от Kv ===
    // DpVent = 15000 × (ρ/2000) × v²
    // НЕ пересчитываем, так как DpVent уже рассчитан в CalculateAtTemperature
    if (valveType == ValveType.HKV_D)
    {
        // Для HKV-D DpVent НЕ зависит от оборотов клапана
        // DpVent уже рассчитан в CalculateAtTemperature
        continue;
    }

    // === IV: DpVent зависит от Kv ===
    // Рассчитать Kv для текущих оборотов
    double kv = ValveTurnsCalculator.CalculateKvFromTurns(circuit.ValveTurns, valveType);

    // === Рабочая температура ===
    double densityOperating = circuit.OperatingResult.Density;
    circuit.OperatingResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * densityOperating;

#pragma warning disable CS0618 // Type or member is obsolete
    circuit.OperatingResult.ValveLoss = circuit.OperatingResult.DpVent;
#pragma warning restore CS0618

    // === Расчётная температура (холодный пуск) ===
    if (circuit.DesignResult != null)
    {
        double densityDesign = circuit.DesignResult.Density;
        circuit.DesignResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * densityDesign;

#pragma warning disable CS0618 // Type or member is obsolete
        circuit.DesignResult.ValveLoss = circuit.DesignResult.DpVent;
#pragma warning restore CS0618
    }
}
```

### 5.2. ДОПОЛНИТЕЛЬНОЕ ИСПРАВЛЕНИЕ: КЭШИРОВАНИЕ View

**Проблема:** При каждом переключении вкладки создаётся новый View.

**Решение:** Кэшировать View в MainViewModel:

```csharp
private readonly Dictionary<string, object> _viewCache = new();

private void NavigateToView(MenuItem menuItem)
{
    if (!_viewCache.TryGetValue(menuItem.Title, out var view))
    {
        view = menuItem.Title switch
        {
            "Климат" => new ClimateView { DataContext = _climateViewModel },
            "Тепловой расчёт" => new ThermalView { DataContext = _thermalViewModel },
            "Конструкция" => new ConstructionView { DataContext = _constructionViewModel },
            "Гидравлический расчёт" => new CircuitsView { DataContext = _circuitsViewModel },
            "Результаты" => new CircuitsResultsView { DataContext = _circuitsViewModel },
            _ => new ClimateView { DataContext = _climateViewModel }
        };
        _viewCache[menuItem.Title] = view;
    }
    
    CurrentView = view;
    UpdateCurrentTitle();
}
```

**Но:** Это не решит проблему с `DpVent`, но улучшит производительность.

---

## 6. ВЫВОДЫ

### 6.1. КОРНЕВАЯ ПРОБЛЕМА

**В `CalculateBalancing` для HKV-D используется НЕПРАВИЛЬНАЯ формула для `DpVent`.**

Для HKV-D:
- `DpVent = 15000 × (ρ/2000) × v²` (НЕ зависит от Kv)
- Но в `CalculateBalancing`: `DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000` (зависит от Kv)

### 6.2. ПОЧЕМУ ПРОБЛЕМА ПРОЯВЛЯЕТСЯ ПРИ ПЕРЕКЛЮЧЕНИИ ВКЛАДОК?

При переключении на вкладку "Тепловой расчёт" и обратно:
1. Изменяются данные в `ThermalViewModel`
2. Срабатывает событие `PropertyChanged`
3. `CircuitsViewModel` получает событие и вызывает `Calculate()`
4. `Calculate()` пересоздаёт `OperatingResult` и `DesignResult`
5. `DpVent` пересчитывается по **ПРАВИЛЬНОЙ** формуле в `CalculateAtTemperature`
6. Затем `CalculateBalancing` **ПЕРЕЗАПИСЫВАЕТ** `DpVent` по **НЕПРАВИЛЬНОЙ** формуле

### 6.3. РЕШЕНИЕ

**В `CalculateBalancing` нужно добавить проверку типа клапана и НЕ пересчитывать `DpVent` для HKV-D.**

---

## 7. ФАЙЛЫ ДЛЯ ИЗМЕНЕНИЯ

1. **`src/Services/Hydraulics/CircuitsCalculator.cs`** — исправить `CalculateBalancing`
2. **Опционально:** `src/MainWindow.xaml.cs` — кэшировать View

---

## 8. ТЕСТИРОВАНИЕ

### 8.1. Тест-кейс 1: HKV-D

1. Создать коллектор с типом HKV-D
2. Добавить контуры
3. Нажать "Рассчитать"
4. Проверить `DpVent` для каждого контура
5. Переключиться на другую вкладку
6. Вернуться на вкладку "Гидравлический расчёт"
7. Проверить, что `DpVent` сохранился

### 8.2. Тест-кейс 2: IV 1¼"

1. Создать коллектор с типом IV 1¼"
2. Добавить контуры
3. Нажать "Рассчитать"
4. Проверить `DpVent` для каждого контура
5. Переключиться на другую вкладку
6. Вернуться на вкладку "Гидравлический расчёт"
7. Проверить, что `DpVent` сохранился

### 8.3. Тест-кейс 3: Изменение данных

1. Создать коллектор с типом HKV-D
2. Добавить контуры
3. Нажать "Рассчитать"
4. Переключиться на вкладку "Тепловой расчёт"
5. Изменить температуру подачи
6. Вернуться на вкладку "Гидравлический расчёт"
7. Проверить, что `DpVent` пересчитался правильно

---

## 9. СТАТУС

**Анализ завершён.** Требуется исправление в `CircuitsCalculator.CalculateBalancing`.