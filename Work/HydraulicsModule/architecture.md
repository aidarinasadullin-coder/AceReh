# Архитектура модуля "Контура" (Гидравлический расчёт)

**Проект:** Калькулятор снеготаяния РЕХАУ  
**Версия:** 2.0  
**Дата:** 2026-03-17  
**Статус:** Утверждено

---

## 1. Обзор архитектуры

### 1.1. Назначение

Модуль "Контура" реализует гидравлический расчёт систем снеготаяния РЕХАУ с поддержкой:
- До 48 контуров (4 коллектора × 12 контуров)
- Двух температурных режимов
- Балансировки контуров
- Подбора коллекторов РЕХАУ

### 1.2. Архитектурный стиль

MVVM (Model-View-ViewModel) с использованием:
- CommunityToolkit.Mvvm для MVVM-паттерна
- Dependency Injection для сервисов
- События для интеграции между модулями

---

## 2. Компоненты модуля

### 2.1. Слои архитектуры

```
┌─────────────────────────────────────────────────────────────┐
│                      Presentation Layer                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │  CircuitsView   │  │ CollectorView   │  │  Controls    │ │
│  │  (XAML)         │  │  (XAML)         │  │  (Buttons)   │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      ViewModel Layer                         │
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │ CircuitsViewModel│  │CollectorViewModel│                  │
│  │  (MVVM)         │  │  (MVVM)         │                   │
│  └─────────────────┘  └─────────────────┘                   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       Service Layer                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │CircuitsCalculator│  │ValveTurnsCalculator│ │GlycolDataService│
│  │  (ICircuitsCalc) │  │                  │  │(IGlycolDataSvc)│ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
│  ┌─────────────────┐                                         │
│  │FlowRegimeCalc   │                                         │
│  │  (IFlowRegime)  │                                         │
│  └─────────────────┘                                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        Model Layer                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │   CircuitRow    │  │CollectorSummary │  │HydraulicInputData│
│  │                 │  │                 │  │              │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │CircuitTempResult│  │   ValveType     │  │ HydraulicMode│ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Модели (Model Layer)

### 3.1. CircuitRow

**Файл:** `src/Models/Hydraulics/CircuitRow.cs`

**Назначение:** Строка таблицы контуров

**Свойства:**
| Свойство | Тип | Описание |
|----------|-----|----------|
| CircuitNumber | int | Номер контура |
| CircuitLength | double | Длина контура L_hk (м) |
| SupplyLength | double | Длина подающего трубопровода L_zul (м) |
| TotalLength | double | Общая длина L_total (м) — вычисляемое |
| CircuitArea | double | Площадь контура S (м²) — вычисляемое |
| PipeSpacing_cm | double | Шаг укладки VA_hk (см) |
| SupplySpacing_cm | double | Шаг подводки VA_zul (см) |
| SupplyHeatPercent | double | Доля тепла от подводок q_zul (%) |
| Power | double | Мощность Q_HK (Вт) |
| FlowRate | double | Расход V_dot (л/ч) |
| Velocity | double | Скорость v (м/с) |
| OperatingResult | CircuitTemperatureResult | Результаты при рабочей температуре |
| DesignResult | CircuitTemperatureResult | Результаты при расчётной температуре |
| Throttling | double | Дросселирование zu_drosseln (Па) |
| RecommendedValveSetting | int | Рекомендуемая настройка вентиля (1-8) |
| Kv | double | Kv коэффициент для контура |
| ValveTurns | double | Обороты клапана |
| IsReferenceCircuit | bool | Референтный контур |
| IsActive | bool | Признак активного контура |
| DisplayMode | HydraulicMode | Текущий режим отображения |
| CurrentResult | CircuitTemperatureResult | Результат для текущего режима |
| FlowRegimeDescription | string | Описание режима течения |
| TotalLoss_mbar | double | Потери в мбар для текущего режима |

### 3.2. CircuitTemperatureResult

**Файл:** `src/Models/Hydraulics/CircuitRow.cs` (вложенный класс)

**Назначение:** Результаты расчёта при температуре

**Свойства:**
| Свойство | Тип | Описание |
|----------|-----|----------|
| Temperature | double | Температура теплоносителя (°C) |
| Density | double | Плотность теплоносителя (г/см³) |
| KinematicViscosity | double | Кинематическая вязкость (мм²/с) |
| ReynoldsNumber | double | Число Рейнольдса Re |
| FlowRegime | FlowRegime | Режим течения (ламинарный/переходный/турбулентный) |
| FrictionFactor | double | Коэффициент трения λ |
| PressureLossPerMeter | double | Удельные потери R (Па/м) |
| CircuitPipeLoss | double | Потери в трубе контура Δp_HK (Па) |
| SupplyPipeLoss | double | Потери в трубе подводки Δp_Zul (Па) |
| ValveLoss | double | Потери в вентиле Δp_Vent (Па) |
| TotalLoss | double | Суммарные потери Δp_total (Па) — вычисляемое |
| TotalLoss_mbar | double | Суммарные потери (мбар) — вычисляемое |

### 3.3. CollectorSummary

**Файл:** `src/Models/Hydraulics/CollectorSummary.cs`

**Назначение:** Итоги коллектора

**Свойства:**
| Свойство | Тип | Описание |
|----------|-----|----------|
| CollectorNumber | int | Номер коллектора |
| CollectorType | string | Тип коллектора (HKV-D, IV 1¼", IV 1½") |
| ValveType | ValveType | Тип балансировочного клапана |
| Kv | double | Kv коллектора (м³/ч) |
| CircuitCount | int | Количество контуров |
| TotalPipeLength | double | Общая длина труб (м) |
| TotalPower | double | Суммарная мощность (Вт) |
| TotalFlowRate | double | Суммарный расход (л/ч) |
| TotalFlowRate_m3h | double | Суммарный расход (м³/ч) — вычисляемое |
| PressureLoss_Operating_mbar | double | Потери при рабочей температуре (мбар) |
| PressureLoss_Cold_mbar | double | Потери при расчётной температуре (мбар) |
| MaxCircuitLoss | double | Максимальные потери контура (Па) |
| ReferenceCircuitNumber | int | Номер референсного контура |
| IsValid | bool | Признак валидности |
| Errors | string[] | Ошибки валидации |
| Warnings | string[] | Предупреждения |
| PressureLoss_Operating_Pa | double | Потери при рабочей температуре (Па) — вычисляемое |
| PressureLoss_Cold_Pa | double | Потери при расчётной температуре (Па) — вычисляемое |
| MaxAllowedPressure_mbar | double | Максимально допустимые потери (320 мбар) — константа |
| IsPressureExceeded | bool | Проверка превышения лимита — вычисляемое |

### 3.4. ValveType

**Файл:** `src/Models/Hydraulics/ValveType.cs`

**Назначение:** Тип балансировочного клапана

**Значения:**
| Значение | Описание |
|----------|----------|
| HKV_D | Бытовой коллектор HKV-D |
| IV_1_25 | Промышленный клапан IV 1¼" |
| IV_1_5 | Промышленный клапан IV 1½" |

### 3.5. HydraulicInputData

**Файл:** `src/Models/Hydraulics/HydraulicInputData.cs`

**Назначение:** Входные данные для расчёта

**Свойства:**
| Свойство | Источник | Описание |
|----------|----------|----------|
| PowerUp | ThermalModule | q_up (Вт/м²) |
| PowerDown | ThermalModule | q_down (Вт/м²) |
| SupplyTemperature | ThermalModule | T_supply (°C) |
| ReturnTemperature | ThermalModule | T_return (°C) |
| InnerDiameter | ThermalModule | d_inner (мм) |
| PipeSpacing_mm | ThermalModule | VA_hk (мм) |
| ColdFiveDayTemperature | ClimateModule | t_cold (°C) |
| GlycolType | Пользователь | Тип гликоля |
| GlycolConcentration | Пользователь | Концентрация (%) |
| SupplySpacing_cm | Пользователь | VA_zul (см) |
| SupplyHeatPercent | Пользователь | q_zul (%) |
| ValveType | Пользователь | Тип клапана (по умолчанию HKV_D) |

**Вычисляемые свойства:**
| Свойство | Формула | Описание |
|----------|---------|----------|
| OperatingTemperature | (T_supply + T_return) / 2 | Рабочая температура (°C) |
| DesignTemperature | t_cold | Расчётная температура (°C) |
| DeltaT | T_supply - T_return | Температурный перепад (К) |
| PipeSpacing_cm | PipeSpacing_mm / 10 | Шаг укладки (см) |

---

## 4. Сервисы (Service Layer)

### 4.1. IGlycolDataService

**Файл:** `src/Services/Hydraulics/IGlycolDataService.cs`

**Назначение:** Интерфейс сервиса свойств гликоля

**Методы:**
```csharp
public interface IGlycolDataService
{
    GlycolProperties GetProperties(GlycolType glycolType, double concentration, double temperature);
    bool IsValidConcentration(double concentration);
    bool IsValidTemperature(double temperature);
}
```

### 4.2. GlycolProperties

**Файл:** `src/Models/Hydraulics/GlycolProperties.cs`

**Назначение:** Свойства гликоля при температуре

**Свойства:**
| Свойство | Тип | Описание |
|----------|-----|----------|
| Temperature | double | Температура (°C) |
| Density | double | Плотность (г/см³) |
| KinematicViscosity | double | Кинематическая вязкость (мм²/с) |
| SpecificHeat | double | Теплоёмкость (кДж/кг·К) |

### 4.3. ICircuitsCalculator

**Файл:** `src/Services/Hydraulics/ICircuitsCalculator.cs`

**Назначение:** Интерфейс калькулятора контуров

**Методы:**
```csharp
public interface ICircuitsCalculator
{
    double CalculateCircuitPower(CircuitRow circuit, double q_up, double q_down);
    double CalculateFlowRate(double power, double deltaT, double density, double specificHeat);
    CircuitTemperatureResult CalculateAtTemperature(CircuitRow circuit, double temperature, 
        GlycolProperties glycolProps, double innerDiameter, double kv);
    List<CircuitRow> CalculateAllCircuits(List<CircuitRow> circuits, HydraulicInputData inputData);
    List<CircuitRow> CalculateBalancing(List<CircuitRow> circuits, ValveType valveType);
    CollectorSummary CalculateCollectorSummary(List<CircuitRow> circuits, int collectorNumber, ValveType valveType);
}
```

### 4.4. CircuitsCalculator

**Файл:** `src/Services/Hydraulics/CircuitsCalculator.cs`

**Назначение:** Реализация калькулятора контуров

**Зависимости:**
- IGlycolDataService — получение свойств гликоля
- FlowRegimeCalculator — расчёт режима течения

**Методы:**

#### CalculateCircuitPower
```
Q_HK = [(L_hk/(100/VA_hk)) + (L_zul/(100/VA_zul))×(q_zul/100)] × (q_up + q_down)
```

#### CalculateFlowRate
```
V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)
```

#### CalculateAtTemperature
1. Рассчитать скорость v
2. Рассчитать число Рейнольдса Re
3. Определить режим течения
4. Рассчитать коэффициент трения λ
5. Рассчитать потери R
6. Рассчитать потери на клапане Δp_Vent

#### CalculateAllCircuits
1. Получить свойства гликоля для двух температур
2. Рассчитать мощность для каждого контура
3. Рассчитать расход для каждого контура
4. Рассчитать результаты при рабочей температуре
5. Рассчитать результаты при расчётной температуре

#### CalculateBalancing
1. Найти контур с максимальными потерями (референтный)
2. Рассчитать дросселирование для остальных контуров
3. Рассчитать обороты клапана

### 4.5. ValveTurnsCalculator

**Файл:** `src/Services/Hydraulics/ValveTurnsCalculator.cs`

**Назначение:** Расчёт оборотов балансировочного клапана

**Методы:**
```csharp
public class ValveTurnsCalculator
{
    public double CalculateTurns(double kv, ValveType valveType);
    public double GetDefaultKv(ValveType valveType);
    public string GetValveTypeName(ValveType valveType);
    public bool IsValidKv(double kv, ValveType valveType);
}
```

**Формулы:**
- IV 1½": Обороты = 5.122 × Kv - 0.2106
- IV 1¼": Обороты = 5.1818 × Kv - 0.23
- HKV-D: Обороты = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712

### 4.6. FlowRegimeCalculator

**Файл:** `src/Services/Hydraulics/FlowRegimeCalculator.cs`

**Назначение:** Расчёт режима течения и коэффициента трения

**Методы:**
```csharp
public class FlowRegimeCalculator
{
    public FlowRegime DetermineRegime(double reynolds);
    public double CalculateFrictionFactor(double reynolds, double innerDiameter, double roughness);
}
```

---

## 5. ViewModels (ViewModel Layer)

### 5.1. CircuitsViewModel

**Файл:** `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

**Назначение:** Управление таблицей контуров

**Свойства:**
| Свойство | Тип | Описание |
|----------|-----|----------|
| Collectors | ObservableCollection<CollectorViewModel> | Список коллекторов |
| SelectedCollectorIndex | int | Выбранный коллектор |
| CurrentMode | HydraulicMode | Режим отображения |
| GlycolType | GlycolType | Тип гликоля |
| GlycolConcentration | double | Концентрация гликоля |
| CanAddCollector | bool | Можно добавить коллектор |
| CanAddCircuit | bool | Можно добавить контур |

**Команды:**
| Команда | Описание |
|---------|----------|
| AddCollectorCommand | Добавить коллектор |
| RemoveCollectorCommand | Удалить коллектор |
| AddCircuitCommand | Добавить контур |
| RemoveCircuitCommand | Удалить контур |
| CalculateCommand | Выполнить расчёт |
| SwitchModeCommand | Переключить режим |

**События:**
- PropertyChanged для всех свойств
- CollectionChanged для Collectors

### 5.2. CollectorViewModel

**Файл:** `src/ViewModels/Hydraulics/CollectorViewModel.cs`

**Назначение:** Управление коллектором

**Свойства:**
| Свойство | Тип | Описание |
|----------|-----|----------|
| CollectorNumber | int | Номер коллектора |
| Circuits | ObservableCollection<CircuitRow> | Список контуров |
| Summary | CollectorSummary | Итоги коллектора |
| ValveType | ValveType | Тип клапана |
| CanAddCircuit | bool | Можно добавить контур |

---

## 6. Views (Presentation Layer)

### 6.1. CircuitsView.xaml

**Файл:** `src/Views/Hydraulics/CircuitsView.xaml`

**Структура:**
```xml
<UserControl>
    <Grid>
        <!-- Переключатель режима -->
        <RadioButton GroupName="Mode" Content="Рабочая температура" />
        <RadioButton GroupName="Mode" Content="Расчётная температура" />
        
        <!-- Параметры теплоносителя -->
        <ComboBox ItemsSource="{Binding GlycolTypes}" SelectedItem="{Binding GlycolType}" />
        <TextBox Text="{Binding GlycolConcentration}" />
        
        <!-- Коллекторы -->
        <ItemsControl ItemsSource="{Binding Collectors}">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <!-- Карточка коллектора -->
                    <ContentControl Content="{Binding}" />
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        
        <!-- Таблица контуров -->
        <DataGrid ItemsSource="{Binding SelectedCollector.Circuits}" />
        
        <!-- Кнопки управления -->
        <Button Command="{Binding AddCircuitCommand}" Content="+ Добавить контур" />
        <Button Command="{Binding RemoveCircuitCommand}" Content="− Удалить контур" />
        <Button Command="{Binding AddCollectorCommand}" Content="+ Добавить коллектор" />
        <Button Command="{Binding RemoveCollectorCommand}" Content="− Удалить коллектор" />
        <Button Command="{Binding CalculateCommand}" Content="Рассчитать" />
    </Grid>
</UserControl>
```

### 6.2. CircuitsView.xaml.cs

**Файл:** `src/Views/Hydraulics/CircuitsView.xaml.cs`

**Назначение:** Code-behind для CircuitsView

**Функционал:**
- Инициализация DataContext
- Обработка событий DataGrid
- Валидация ввода

---

## 7. Интеграция с другими модулями

### 7.1. Интеграция с ThermalModule

**События:**
- ThermalViewModel.Calculated — расчёт завершён

**Данные:**
- PowerUp (q_up)
- PowerDown (q_down)
- SupplyTemperature (T_supply)
- ReturnTemperature (T_return)
- InnerDiameter (d_inner)
- PipeSpacing (VA_hk)

### 7.2. Интеграция с ClimateModule

**События:**
- ClimateViewModel.CityChanged — город изменён

**Данные:**
- ColdFiveDayTemperature (t_cold)

### 7.3. DI-регистрация

**Файл:** `src/Services/Hydraulics/ServiceCollectionExtensions.cs`

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHydraulicsServices(this IServiceCollection services)
    {
        services.AddSingleton<IGlycolDataService, GlycolDataService>();
        services.AddSingleton<ICircuitsCalculator, CircuitsCalculator>();
        services.AddSingleton<ValveTurnsCalculator>();
        
        return services;
    }
}
```

---

## 8. Потоки данных

### 8.1. Поток расчёта

```
Пользователь вводит параметры
        │
        ▼
CircuitsViewModel
        │
        ├──► ThermalModule (получить q_up, q_down, температуры)
        │
        ├──► ClimateModule (получить t_cold)
        │
        ▼
CircuitsCalculator.CalculateAllCircuits()
        │
        ├──► GlycolDataService.GetProperties() (свойства гликоля)
        │
        ├──► FlowRegimeCalculator.CalculateFrictionFactor() (λ)
        │
        ├──► ValveTurnsCalculator.CalculateTurns() (обороты)
        │
        ▼
CircuitRow (результаты)
        │
        ▼
CircuitsView (отображение)
```

### 8.2. Поток балансировки

```
CircuitsCalculator.CalculateBalancing()
        │
        ├──► Найти контур с max(Δp_total)
        │
        ├──► Рассчитать zu_drosseln для остальных
        │
        ├──► ValveTurnsCalculator.CalculateTurns()
        │
        ▼
CircuitRow (балансировка)
```

---

## 9. Обработка ошибок

### 9.1. Валидация ввода

| Параметр | Валидация |
|----------|-----------|
| Length | > 0, ≤ 500 м |
| SupplyLength | ≥ 0, ≤ 100 м |
| PipeSpacing | ≥ 5, ≤ 50 см |
| GlycolConcentration | ≥ 0, ≤ 100 % |

### 9.2. Обработка исключений

| Исключение | Обработка |
|------------|-----------|
| ArgumentNullException | Логирование, возврат пустого результата |
| DivideByZeroException | Проверка ΔT > 0 |
| InvalidOperationException | Логирование, отображение сообщения |

---

## 10. Тестирование

### 10.1. Unit-тесты

| Компонент | Тесты |
|-----------|-------|
| ValveTurnsCalculator | Формулы оборотов, валидация Kv |
| CircuitsCalculator | Мощность, расход, потери, балансировка |
| CircuitsViewModel | Команды, свойства, PropertyChanged |

### 10.2. Интеграционные тесты

| Сценарий | Описание |
|----------|----------|
| FullCalculation | Полный цикл расчёта |
| IntegrationWithThermalModule | Получение данных из ThermalModule |
| IntegrationWithClimateModule | Получение данных из ClimateModule |

---

## 11. История изменений

| Версия | Дата | Автор | Изменения |
|--------|------|-------|-----------|
| 1.0 | 2026-03-15 | Архитектор | Начальная версия |
| 2.0 | 2026-03-17 | Архитектор | Добавлена таблица контуров, две температуры |

---

*Архитектура утверждена: 2026-03-17*