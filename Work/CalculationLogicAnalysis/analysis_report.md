# АНАЛИТИЧЕСКИЙ ОТЧЁТ: ЛОГИКА РАСЧЁТОВ В ПРИЛОЖЕНИИ

**Дата:** 2026-03-25  
**Автор:** Аналитик  
**Версия:** 1.0

---

## СОДЕРЖАНИЕ

1. [Схема зависимостей между модулями](#1-схема-зависимостей-между-модулями)
2. [Тепловой расчёт](#2-тепловой-расчёт)
3. [Гидравлический расчёт](#3-гидравлический-расчёт)
4. [Анализ бага с DpVent](#4-анализ-бага-с-dpvent)
5. [Рекомендации по улучшению](#5-рекомендации-по-улучшению)

---

## 1. СХЕМА ЗАВИСИМОСТЕЙ МЕЖДУ МОДУЛЯМИ

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           КЛИМАТИЧЕСКИЙ МОДУЛЬ                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ClimateViewModel                                                    │   │
│  │ - AirTemperature (расчётная температура)                            │   │
│  │ - WindSpeed (скорость ветра)                                        │   │
│  │ - Humidity (влажность)                                              │   │
│  │ - SnowfallIntensity (интенсивность снегопада)                       │   │
│  │ - ColdFiveDayTemperature (температура холодной пятидневки)          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│                                    │ IClimateData (singleton)              │
│                                    │ DataChanged event                      │
│                                    ▼                                        │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                    ┌────────────────┴────────────────┐
                    │                                 │
                    ▼                                 ▼
┌───────────────────────────────────┐ ┌───────────────────────────────────────┐
│      ТЕПЛОВОЙ РАСЧЁТ              │ │      КОНСТРУКЦИЯ                      │
│  ┌─────────────────────────────┐  │ │  ┌─────────────────────────────────┐  │
│  │ ThermalViewModel            │  │ │  │ ConstructionViewModel           │  │
│  │ - SelectedMode (режим)      │  │ │  │ - LayersAbovePipe (слои над)   │  │
│  │ - SupplyTemperature (подача) │  │ │  │ - LayersBelowPipe (слои под)   │  │
│  │ - GroundTemperature (грунт)  │  │ │  │ - GroundwaterLevel (УГВ)       │  │
│  │ - SelectedPipe (труба)       │  │ │  │ - R1Total, R2Total, LambdaE    │  │
│  │ - PipeSpacing (шаг укладки)  │  │ │  └─────────────────────────────────┘  │
│  │ - Result (результат)         │  │ │                    │                  │
│  └─────────────────────────────┘  │ │                    │                  │
│                │                   │ │                    │ IConstructionData│
│                │ PropertyChanged   │ │                    │ DataChanged event │
│                ▼                   │ │                    ▼                  │
└───────────────────────────────────┘ └───────────────────────────────────────┘
                    │                                 │
                    │ Result.PropertyChanged          │
                    │ (PowerUp, PowerDown, etc.)      │
                    ▼                                 │
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ГИДРАВЛИЧЕСКИЙ РАСЧЁТ                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ CircuitsViewModel                                                   │   │
│  │ - InputData (входные данные из ThermalViewModel.Result)            │   │
│  │ - Collectors (коллекторы с контурами)                              │   │
│  │ - GlycolType, GlycolConcentration (гликоль)                        │   │
│  │ - CurrentMode (рабочая/расчётная температура)                      │   │
│  │                                                                     │   │
│  │ Автоматические пересчёты:                                          │   │
│  │ ✓ При изменении ThermalViewModel.Result → Calculate()              │   │
│  │ ✓ При изменении ClimateViewModel.AirTemperature → Calculate()       │   │
│  │ ✓ При изменении GlycolType/GlycolConcentration → Calculate()       │   │
│  │ ✓ При добавлении/удалении контура → Calculate()                    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Ключевые зависимости:

| Модуль | Зависит от | Тип связи | Событие |
|--------|-----------|-----------|---------|
| ThermalViewModel | ClimateData | IClimateData (singleton) | DataChanged |
| ThermalViewModel | ConstructionData | IConstructionData (singleton) | DataChanged |
| CircuitsViewModel | ThermalViewModel | PropertyChanged | Result.PropertyChanged |
| CircuitsViewModel | ClimateViewModel | PropertyChanged | AirTemperature.PropertyChanged |

---

## 2. ТЕПЛОВОЙ РАСЧЁТ

### 2.1. Кнопка "Расчёт" (CalculateCommand)

**Расположение:** `ThermalViewModel.cs`, строки 175-221

**Что делает:**
1. Валидирует входные данные (труба, температура подачи, температура грунта, шаг укладки)
2. Собирает `ThermalParameters` из свойств ViewModel
3. Получает климатические данные из `IClimateData`
4. Получает данные конструкции из `IConstructionData`
5. Вызывает `_calculator.Calculate(parameters)`
6. Сохраняет результат в `Result`

### 2.2. Автоматические пересчёты

| Событие | Метод | Действие |
|---------|-------|----------|
| Изменение климатических данных | `OnClimateDataChanged()` | **Сброс результата**, сообщение "Требуется пересчёт" |
| Изменение данных конструкции | `OnConstructionDataChanged()` | **Сброс результата**, сообщение "Требуется пересчёт" |

**Важно:** Автоматический пересчёт **НЕ выполняется**. Только сброс результата.

### 2.3. Параметры, пересчитываемые автоматически

**НЕТ автоматически пересчитываемых параметров.**

Все параметры требуют нажатия кнопки "Расчёт":
- Коэффициент теплоотдачи α
- Мощность вверх q_FB
- Мощность вниз q_D
- Суммарная мощность
- Средняя температура
- Температура обратки
- Расход
- КПД ребра η_R

### 2.4. Параметры, требующие нажатия кнопки

**ВСЕ параметры** требуют нажатия кнопки "Расчёт".

### 2.5. Почему нет автоматического пересчёта?

**Причина:** Тепловой расчёт — это сложная операция, которая:
1. Требует валидации всех входных данных
2. Может занимать время (асинхронный вызов)
3. Зависит от множества параметров

**Решение:** Сброс результата с сообщением "Требуется пересчёт" — это правильный подход, так как:
- Пользователь видит, что данные устарели
- Пользователь может изменить несколько параметров перед пересчётом
- Избегаются лишние пересчёты

---

## 3. ГИДРАВЛИЧЕСКИЙ РАСЧЁТ

### 3.1. Кнопка "Рассчитать" (CalculateCommand)

**Расположение:** `CircuitsViewModel.cs`, строки 444-535

**Что делает:**
1. Получает данные из `InputData` (заполняется из `ThermalViewModel.Result`)
2. Получает свойства гликоля для рабочей и расчётной температур
3. Для каждого контура:
   - Рассчитывает мощность `CalculateCircuitPower()`
   - Рассчитывает расход `CalculateFlowRate()`
   - Рассчитывает результаты при рабочей температуре `CalculateAtTemperature()`
   - Рассчитывает результаты при расчётной температуре `CalculateAtTemperature()`
   - Устанавливает `DisplayMode`
4. Рассчитывает итоги коллектора `CalculateCollectorSummary()`
5. Автоматически выбирает тип коллектора `AutoSelectCollectorType()`
6. Выполняет балансировку `CalculateBalancing()`

### 3.2. Автоматические пересчёты

| Событие | Метод | Действие |
|---------|-------|----------|
| Изменение `ThermalViewModel.Result` | `OnThermalViewModelPropertyChanged()` | `UpdateFromThermalModule()` → `Calculate()` |
| Изменение `ClimateViewModel.AirTemperature` | `OnClimatePropertyChanged()` | `UpdateFromClimateModule()` → `Calculate()` |
| Изменение `GlycolType` | `OnGlycolTypeChanged()` | `Calculate()` |
| Изменение `GlycolConcentration` | `OnGlycolConcentrationChanged()` | `Calculate()` |
| Добавление контура | `AddCircuit()` | `Calculate()` |
| Удаление контура | `RemoveCircuit()` | `Calculate()` |

### 3.3. Параметры, пересчитываемые автоматически

**ВСЕ параметры** пересчитываются автоматически при изменении:
- Результатов теплового расчёта
- Климатических данных
- Типа/концентрации гликоля
- Добавления/удаления контура

### 3.4. Параметры, требующие нажатия кнопки

**НЕТ параметров, требующих нажатия кнопки.**

Все параметры пересчитываются автоматически.

### 3.5. Почему автоматический пересчёт работает?

**Причина:** Гидравлический расчёт:
1. Быстрый (синхронный)
2. Зависит от уже валидированных данных (из теплового расчёта)
3. Пользователь ожидает мгновенного обновления

---

## 4. АНАЛИЗ БАГА С DpVent

### 4.1. Описание бага

**Симптом:** При переключении между вкладками "Рабочая температура" и "Расчётная температура" значение DpVent меняется, а при нажатии "Рассчитать" возвращается к правильному значению.

### 4.2. Корневая причина

**Проблема в `CircuitsCalculator.CalculateBalancing()` (строки 442-462):**

```csharp
// Пересчитать потери на клапане при текущих оборотах
foreach (var circuit in activeCircuits)
{
    // Рассчитать Kv для текущих оборотов
    double kv = ValveTurnsCalculator.CalculateKvFromTurns(circuit.ValveTurns, valveType);

    // Пересчитать потери на клапане
    double density_g_cm3 = circuit.OperatingResult.Density;

    // === ПРОБЛЕМА: Обновляется только OperatingResult.DpVent ===
    circuit.OperatingResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_g_cm3;
}
```

**Код обновляет `OperatingResult.DpVent`, но НЕ обновляет `DesignResult.DpVent`!**

### 4.3. Схема потока данных

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Calculate() в CircuitsViewModel                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. CalculateAtTemperature(operatingTemp) → OperatingResult                │
│     - DpVent рассчитывается по формуле                                      │
│     - Для HKV-D: DpVent = 15000 × (ρ/2000) × v²                            │
│     - Для IV: DpVent = (V_dot/1000/Kv)² × 100000 × ρ/1000                  │
│                                                                             │
│  2. CalculateAtTemperature(designTemp) → DesignResult                       │
│     - DpVent рассчитывается по формуле                                      │
│     - Значение отличается из-за другой температуры (другая вязкость)       │
│                                                                             │
│  3. CalculateBalancing()                                                    │
│     - Находит референсный контур                                            │
│     - Рассчитывает обороты клапана                                          │
│     - ПЕРЕСЧИТЫВАЕТ OperatingResult.DpVent для текущих оборотов            │
│     - ❌ НЕ ПЕРЕСЧИТЫВАЕТ DesignResult.DpVent                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Отображение в таблице                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Binding: {Binding CurrentResult.DpVent}                                   │
│                                                                             │
│  CurrentResult = DisplayMode == OperatingTemperature                        │
│                  ? OperatingResult                                          │
│                  : DesignResult                                             │
│                                                                             │
│  При переключении DisplayMode:                                              │
│  - OperatingResult.DpVent = обновлённое значение (после балансировки)       │
│  - DesignResult.DpVent = первоначальное значение (до балансировки)          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.4. Почему при нажатии "Рассчитать" всё исправляется?

Потому что `Calculate()` вызывает:
1. `CalculateAtTemperature()` для **обоих** режимов (OperatingResult и DesignResult)
2. `CalculateBalancing()` обновляет **только** OperatingResult

После этого:
- OperatingResult.DpVent = корректное значение (после балансировки)
- DesignResult.DpVent = корректное значение (из CalculateAtTemperature)

### 4.5. Почему это не было замечено раньше?

1. **Режим по умолчанию** — OperatingTemperature, поэтому пользователи видят корректное значение
2. **Переключение режима** — редкая операция, пользователи редко смотрят на расчётную температуру
3. **Нажатие "Рассчитать"** — исправляет проблему, но она возникает снова при следующем автоматическом пересчёте

---

## 5. РЕКОМЕНДАЦИИ ПО УЛУЧШЕНИЮ

### 5.1. Исправление бага с DpVent

**Проблема:** `CalculateBalancing()` обновляет только `OperatingResult.DpVent`

**Решение:** Добавить пересчёт `DesignResult.DpVent` в методе `CalculateBalancing()`:

```csharp
// Пересчитать потери на клапане при текущих оборотах
foreach (var circuit in activeCircuits)
{
    // Рассчитать Kv для текущих оборотов
    double kv = ValveTurnsCalculator.CalculateKvFromTurns(circuit.ValveTurns, valveType);

    // Пересчитать потери на клапане для OperatingResult
    double density_operating = circuit.OperatingResult.Density;
    circuit.OperatingResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_operating;

    // Пересчитать потери на клапане для DesignResult
    double density_design = circuit.DesignResult.Density;
    circuit.DesignResult.DpVent = Math.Pow(circuit.FlowRate / 1000.0 / kv, 2) * 100000 * density_design;
}
```

### 5.2. Улучшение логики автоматического пересчёта в тепловом расчёте

**Проблема:** При изменении климатических данных или конструкции результат сбрасывается, но не пересчитывается.

**Варианты решения:**

#### Вариант A: Автоматический пересчёт (рекомендуется)

```csharp
private void OnClimateDataChanged(object? sender, ClimateDataChangedEventArgs e)
{
    // Автоматический пересчёт
    if (Result != null && ValidateInput())
    {
        CalculateCommand.Execute(null);
    }
}
```

**Преимущества:**
- Пользователь всегда видит актуальные данные
- Нет необходимости нажимать кнопку

**Недостатки:**
- Может быть много пересчётов при изменении нескольких параметров
- Требует валидации перед каждым пересчётом

#### Вариант B: Отложенный пересчёт (debounce)

```csharp
private CancellationTokenSource? _recalculationCts;

private async void OnClimateDataChanged(object? sender, ClimateDataChangedEventArgs e)
{
    // Отмена предыдущего пересчёта
    _recalculationCts?.Cancel();
    _recalculationCts = new CancellationTokenSource();

    try
    {
        // Ждать 500 мс перед пересчётом
        await Task.Delay(500, _recalculationCts.Token);
        
        if (Result != null && ValidateInput())
        {
            CalculateCommand.Execute(null);
        }
    }
    catch (OperationCanceledException)
    {
        // Отменено — это нормально
    }
}
```

**Преимущества:**
- Избегает лишних пересчётов
- Пользователь видит актуальные данные

**Недостатки:**
- Более сложная реализация
- Требует управления CancellationToken

### 5.3. Унификация логики пересчёта

**Проблема:** Разная логика в тепловом и гидравлическом расчётах.

**Рекомендация:** Создать общий паттерн:

```csharp
public abstract class CalculationViewModelBase : ObservableObject
{
    protected bool _autoRecalculate = true;
    protected CancellationTokenSource? _recalculationCts;

    protected async void OnInputDataChanged()
    {
        if (!_autoRecalculate) return;

        _recalculationCts?.Cancel();
        _recalculationCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _recalculationCts.Token);
            await RecalculateAsync();
        }
        catch (OperationCanceledException)
        {
            // Отменено — это нормально
        }
    }

    protected abstract Task RecalculateAsync();
}
```

### 5.4. Добавление индикатора устаревших данных

**Проблема:** Пользователь не видит, что данные устарели.

**Рекомендация:** Добавить визуальный индикатор:

```csharp
[ObservableProperty]
private bool _isDataStale;

private void OnClimateDataChanged(object? sender, ClimateDataChangedEventArgs e)
{
    if (Result != null)
    {
        IsDataStale = true;
        ValidationMessage = "Климатические данные изменены. Требуется пересчёт.";
    }
}

private async Task Calculate()
{
    // ... расчёт ...
    IsDataStale = false;
}
```

```xml
<TextBlock Text="⚠ Данные устарели"
           Foreground="Orange"
           Visibility="{Binding IsDataStale, Converter={StaticResource BooleanToVisibilityConverter}}"/>
```

---

## 6. ИТОГОВАЯ ТАБЛИЦА

| Модуль | Автоматический пересчёт | Кнопка расчёта | Причина |
|--------|------------------------|----------------|---------|
| **Тепловой расчёт** | ❌ Нет | ✅ Да | Сложный расчёт, требует валидации |
| **Гидравлический расчёт** | ✅ Да | ✅ Да (опционально) | Быстрый расчёт, зависит от валидированных данных |

| Параметр | Автоматический пересчёт | Примечание |
|----------|------------------------|------------|
| Климатические данные → Тепловой расчёт | ❌ Нет | Сброс результата |
| Конструкция → Тепловой расчёт | ❌ Нет | Сброс результата |
| Тепловой расчёт → Гидравлический расчёт | ✅ Да | Автоматический пересчёт |
| Климатические данные → Гидравлический расчёт | ✅ Да | Автоматический пересчёт |
| Гликоль → Гидравлический расчёт | ✅ Да | Автоматический пересчёт |
| Контур → Гидравлический расчёт | ✅ Да | Автоматический пересчёт |

---

## 7. ВЫВОДЫ

1. **Тепловой расчёт** использует **ручной** пересчёт (кнопка "Расчёт"), что правильно для сложных вычислений.

2. **Гидравлический расчёт** использует **автоматический** пересчёт, что правильно для быстрых вычислений.

3. **Баг с DpVent** вызван тем, что `CalculateBalancing()` обновляет только `OperatingResult.DpVent`, но не `DesignResult.DpVent`.

4. **Рекомендации:**
   - Исправить баг с DpVent (добавить пересчёт DesignResult)
   - Рассмотреть автоматический пересчёт для теплового расчёта (с debounce)
   - Добавить визуальный индикатор устаревших данных
   - Унифицировать логику пересчёта между модулями