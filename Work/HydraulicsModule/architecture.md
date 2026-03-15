# Архитектура модуля гидравлического расчёта

## Калькулятор снеготаяния РЕХАУ

**Версия:** 1.0  
**Дата:** 15.03.2026  
**Статус:** Создано  
**Автор:** Архитектор

---

## 1. Описание задачи

### 1.1. Ссылка на ТЗ
`Work/HydraulicsModule/technical_specification.md`

### 1.2. Краткое резюме требований

Модуль "Гидравлический расчёт" предназначен для:
- Расчёта гидравлических параметров контуров системы снеготаяния
- Определения режима течения (ламинарный/переходный/турбулентный)
- Расчёта потерь давления в трубах и вентилях
- Подбора коллекторов РЕХАУ
- Расчёта дросселирования для балансировки контуров

### 1.3. Интеграционные точки

| Компонент | Интерфейс | Назначение |
|-----------|-----------|------------|
| ThermalModule | `IThermalCalculationResult` | Получение расхода, температур |
| ClimateModule | `IClimateData` | Получение климатических параметров |
| GlycolDataService | `IGlycolDataService` | Свойства теплоносителя |
| CollectorRepository | `ICollectorRepository` | Данные о коллекторах РЕХАУ |

---

## 2. Функциональная архитектура

### 2.1. Функциональные компоненты

#### Компонент: HydraulicCalculator

**Назначение:** Выполнение гидравлического расчёта контура

**Функции:**

| Функция | Входные данные | Выходные данные | Связанные UC |
|---------|----------------|-----------------|--------------|
| `CalculateVelocity()` | расход (л/ч), диаметр (мм) | скорость (м/с) | UC-01 |
| `CalculateReynoldsNumber()` | скорость, диаметр, вязкость | Re (безразмерный) | UC-01, UC-02 |
| `DetermineFlowRegime()` | Re | FlowRegime | UC-02 |
| `CalculateFrictionFactor()` | Re, диаметр, шероховатость | λ | UC-02 |
| `CalculatePressureLossPerMeter()` | скорость, плотность, λ, диаметр | R (Па/м) | UC-01 |
| `CalculateValvePressureLoss()` | расход, плотность, тип коллектора | Δp_вентиль (Па) | UC-04 |
| `Calculate()` | HydraulicParameters | HydraulicResult | UC-01..UC-06 |

**Зависимости:**
- От: `IGlycolDataService` (свойства теплоносителя)
- К нему: `HydraulicsViewModel`

---

#### Компонент: GlycolDataService

**Назначение:** Загрузка и интерполяция свойств гликолей

**Функции:**

| Функция | Входные данные | Выходные данные | Связанные UC |
|---------|----------------|-----------------|--------------|
| `GetDensity()` | тип гликоля, концентрация, температура | плотность (кг/м³) | UC-07 |
| `GetSpecificHeat()` | тип гликоля, концентрация, температура | c_p (кДж/кг·К) | UC-07 |
| `GetKinematicViscosity()` | тип гликоля, концентрация, температура | ν (мм²/с) | UC-07 |
| `GetProperties()` | тип гликоля, концентрация, температура | GlycolProperties | UC-07 |

**Зависимости:**
- От: `data/glycol_data.json`
- К нему: `HydraulicCalculator`

---

#### Компонент: CollectorRepository

**Назначение:** Управление данными о коллекторах РЕХАУ

**Функции:**

| Функция | Входные данные | Выходные данные | Связанные UC |
|---------|----------------|-----------------|--------------|
| `GetAllAsync()` | — | IEnumerable<Collector> | UC-05 |
| `GetByIdAsync()` | id | Collector | UC-05 |
| `GetByTypeAsync()` | CollectorType | IEnumerable<Collector> | UC-05 |
| `GetByCircuitsAsync()` | circuits | Collector | UC-05 |
| `SelectCollector()` | circuits, totalFlowRate | Collector | UC-05 |

**Зависимости:**
- От: `data/rehau_products.json`
- К нему: `HydraulicsViewModel`

---

#### Компонент: HydraulicValidator

**Назначение:** Валидация входных параметров и результатов расчёта

**Функции:**

| Функция | Входные данные | Выходные данные | Связанные UC |
|---------|----------------|-----------------|--------------|
| `Validate()` | HydraulicParameters | ValidationResult | UC-01 |
| `ValidateResult()` | HydraulicResult | ValidationResult | UC-01 |

**Правила валидации:**
- Длина контура: 10 ≤ L_HK ≤ 500 м
- Длина подводки: 1 ≤ L_Zul ≤ 100 м
- Доля гликоля: 10 ≤ Glycolanteil ≤ 90 %
- Температура подачи: 20 ≤ T_подачи ≤ 90 °C
- Температура обратки: 15 ≤ T_обратки ≤ 80 °C
- Скорость потока: 0.2 ≤ w ≤ 1.5 м/с (рекомендация)

---

### 2.2. Диаграмма компонентов

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            View Layer                                    │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                      HydraulicsView.xaml                           │  │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐              │  │
│  │  │ CircuitParams │ │ GlycolParams │ │ ResultsPanel │              │  │
│  │  │ (TextBoxes)   │ │ (ComboBoxes) │ │ (DataGrid)   │              │  │
│  │  └──────────────┘ └──────────────┘ └──────────────┘              │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                              │ Data Binding
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          ViewModel Layer                                 │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                    HydraulicsViewModel                              │  │
│  │  - CircuitLength, SupplyLength                                      │  │
│  │  - GlycolConcentration, GlycolType                                  │  │
│  │  - SelectedCollector                                                │  │
│  │  - HydraulicResult                                                  │  │
│  │  + CalculateCommand, SelectCollectorCommand                        │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                              │ IHydraulicCalculator, IGlycolDataService
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           Service Layer                                  │
│  ┌─────────────────────┐ ┌─────────────────────┐ ┌──────────────────┐ │
│  │ HydraulicCalculator │ │  GlycolDataService   │ │ HydraulicValidator│ │
│  │ - CalculateVelocity │ │ - GetDensity         │ │ - Validate        │ │
│  │ - CalculateRe       │ │ - GetViscosity       │ │ - ValidateResult  │ │
│  │ - CalculateLambda   │ │ - GetSpecificHeat    │ │                   │ │
│  │ - CalculatePressure │ │ - GetProperties      │ │                   │ │
│  └─────────────────────┘ └─────────────────────┘ └──────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
                              │ JSON Data
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                            Data Layer                                    │
│  ┌─────────────────────┐ ┌─────────────────────┐ ┌──────────────────┐ │
│  │  glycol_data.json   │ │ rehau_products.json  │ │ ThermalModule    │ │
│  │  (свойства гликолей) │ │ (коллекторы РЕХАУ)    │ │ (IThermalResult) │ │
│  └─────────────────────┘ └─────────────────────┘ └──────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

---

### 2.3. Поток данных

```
1. Пользователь открывает вкладку "Гидравлика"
   ↓
2. HydraulicsViewModel подписывается на ThermalCalculationResult.ResultChanged
   ↓
3. При изменении теплового расчёта:
   - Получение VolumeFlowRate, SupplyTemperature, ReturnTemperature
   - Получение Pipe из ThermalParameters
   ↓
4. Пользователь вводит параметры контура:
   - CircuitLength (L_HK)
   - SupplyLength (L_Zul)
   - GlycolConcentration
   - GlycolType
   ↓
5. HydraulicsViewModel.CalculateCommand:
   ↓
6. GlycolDataService.GetProperties(temperature, concentration):
   - Интерполяция из glycol_data.json
   - Возврат Density, KinematicViscosity, SpecificHeat
   ↓
7. HydraulicCalculator.Calculate(parameters):
   - CalculateVelocity()
   - CalculateReynoldsNumber()
   - DetermineFlowRegime()
   - CalculateFrictionFactor()
   - CalculatePressureLossPerMeter()
   - CalculateValvePressureLoss()
   ↓
8. HydraulicValidator.ValidateResult(result):
   - Проверка скорости (0.2-1.5 м/с)
   - Проверка режима течения
   - Добавление предупреждений
   ↓
9. Отображение результатов в HydraulicsView
```

---

## 3. Системная архитектура

### 3.1. Архитектурный стиль

**Многоуровневая архитектура (Layered)** с MVVM

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  Views (XAML) + ViewModels (MVVM)                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  Services (HydraulicCalculator, GlycolDataService)          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Domain Layer                            │
│  Models (HydraulicParameters, HydraulicResult, Collector)   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                      │
│  Repositories (CollectorRepository), JSON Data              │
└─────────────────────────────────────────────────────────────┘
```

### 3.2. Структура проекта

```
src/
├── Models/
│   └── Hydraulics/
│       ├── HydraulicParameters.cs      # Параметры расчёта
│       ├── HydraulicResult.cs          # Результат расчёта
│       ├── FlowRegime.cs               # Режим течения (enum)
│       ├── GlycolType.cs               # Тип гликоля (enum)
│       ├── Collector.cs                # Модель коллектора
│       ├── CollectorType.cs            # Тип коллектора (enum)
│       ├── CircuitResult.cs            # Результат контура
│       └── GlycolProperties.cs        # Свойства гликоля
│
├── ViewModels/
│   └── Hydraulics/
│       ├── HydraulicsViewModel.cs      # Основная ViewModel
│       ├── CircuitViewModel.cs         # ViewModel контура
│       └── CollectorViewModel.cs       # ViewModel коллектора
│
├── Views/
│   └── Hydraulics/
│       ├── HydraulicsView.xaml         # Основное представление
│       ├── HydraulicsView.xaml.cs
│       ├── CircuitInputView.xaml       # Ввод параметров контура
│       └── ResultsView.xaml            # Отображение результатов
│
├── Services/
│   └── Hydraulics/
│       ├── IHydraulicCalculator.cs     # Интерфейс калькулятора
│       ├── HydraulicCalculator.cs      # Реализация калькулятора
│       ├── IGlycolDataService.cs        # Интерфейс сервиса гликолей
│       ├── GlycolDataService.cs         # Реализация сервиса
│       ├── HydraulicValidator.cs       # Валидатор
│       └── FlowRegimeCalculator.cs      # Расчёт режима течения
│
├── Repositories/
│   └── Hydraulics/
│       ├── ICollectorRepository.cs      # Интерфейс репозитория
│       └── CollectorRepository.cs       # Реализация репозитория
│
└── Configuration/
    └── ServiceCollectionExtensions.cs   # DI-регистрация
```

---

## 4. Модель данных

### 4.1. Сущности

#### HydraulicParameters

| Атрибут | Тип | Описание | Ограничения |
|---------|-----|----------|-------------|
| CircuitLength | double | Длина контура, м | 10-500 |
| SupplyLength | double | Длина подводки, м | 1-100 |
| PipeSpacing | double | Шаг укладки, см | 10-50 |
| SupplySpacing | double | Шаг подводки, см | — |
| GlycolConcentration | double | Доля гликоля, % | 10-90 |
| GlycolType | GlycolType | Тип гликоля | Ethylene/Propylene |
| SupplyTemperature | double | Температура подачи, °C | 20-90 |
| ReturnTemperature | double | Температура обратки, °C | 15-80 |
| Density | double | Плотность, кг/м³ | > 0 |
| KinematicViscosity | double | Вязкость, мм²/с | > 0 |
| SpecificHeat | double | Теплоёмкость, кДж/кг·К | > 0 |
| Pipe | PipeType | Тип трубы | — |
| Roughness | double | Шероховатость, мм | 0.007 (PE-Xa) |
| VolumeFlowRate | double | Расход, л/(ч·м²) | > 0 |
| PowerPerArea | double | Мощность, Вт/м² | > 0 |
| CircuitArea | double | Площадь контура, м² | > 0 |

#### HydraulicResult

| Атрибут | Тип | Описание |
|---------|-----|----------|
| Velocity | double | Скорость потока, м/с |
| ReynoldsNumber | double | Число Рейнольдса |
| FlowRegime | FlowRegime | Режим течения |
| FrictionFactor | double | Коэффициент трения λ |
| PressureLossPerMeter | double | Удельные потери, Па/м |
| CircuitPressureLoss | double | Потери в контуре, Па |
| SupplyPressureLoss | double | Потери в подводке, Па |
| TotalPipePressureLoss | double | Общие потери в трубе, Па |
| ValvePressureLoss | double | Потери в вентиле, Па |
| TotalPressureLoss | double | Суммарные потери, Па |
| CircuitFlowRate | double | Расход на контур, л/ч |
| IsValid | bool | Валидность результата |
| ValidationErrors | string[] | Ошибки |
| Warnings | string[] | Предупреждения |

#### Collector

| Атрибут | Тип | Описание |
|---------|-----|----------|
| Id | string | Идентификатор |
| Name | string | Название |
| FullName | string | Полное название |
| Type | CollectorType | Тип (HKV/IV) |
| Circuits | int | Количество контуров |
| ConnectionSize | string | Размер подключения (например, "1¼\"", "1½\"") |
| Kv | double | Коэффициент пропускной способности вентиля, м³/ч |
| MaxFlowRate | double | Макс. расход, м³/ч |
| MaxPressure | double | Макс. давление, мбар |
| MaxSetting | int | Макс. настройка вентиля |
| ArticleNumber | string? | Артикул |

**Определение Kv для коллекторов:**

| Тип коллектора | ConnectionSize | Kv (м³/ч) | Примечание |
|----------------|----------------|-----------|------------|
| HKV-D | — | 1.2 | Бытовой коллектор, встроенный вентиль |
| IV | 1¼" | 1.45 | Промышленный коллектор, DN25 |
| IV | 1½" | 1.5 | Промышленный коллектор, DN40 |

**Примечание:** Значения Kv используются в формуле расчёта потерь давления в вентиле (см. раздел 6.2). Kv определяется по типу коллектора и размеру подключения:
- Для HKV-D: Kv = 1.2 м³/ч (фиксированное значение)
- Для IV 1¼": Kv = 1.45 м³/ч
- Для IV 1½": Kv = 1.5 м³/ч

#### CircuitResult

| Атрибут | Тип | Описание |
|---------|-----|----------|
| CircuitNumber | int | Номер контура |
| Length | double | Длина, м |
| FlowRate | double | Расход, л/ч |
| PipePressureLoss | double | Потери в трубе, Па |
| ValvePressureLoss | double | Потери в вентиле, Па |
| TotalPressureLoss | double | Суммарные потери, Па |
| Throttling | double | Дросселирование, Па |
| RecommendedValveSetting | int | Настройка вентиля (1-8) |
| HydraulicResult | HydraulicResult | Детальный результат |

#### GlycolProperties

| Атрибут | Тип | Описание |
|---------|-----|----------|
| Density | double | Плотность, кг/м³ |
| SpecificHeat | double | Удельная теплоёмкость, кДж/(кг·К) |
| KinematicViscosity | double | Кинематическая вязкость, мм²/с |
| ThermalConductivity | double | Теплопроводность, Вт/(м·К) |

**Примечание:** Класс используется для передачи совокупности свойств теплоносителя из `GlycolDataService`. Значения получаются интерполяцией из `data/glycol_data.json` для заданного типа гликоля, концентрации и температуры.

### 4.2. Перечисления

```csharp
public enum FlowRegime
{
    Laminar,      // Re < 2300
    Transitional, // 2300 ≤ Re ≤ 4000
    Turbulent     // Re > 4000
}

public enum GlycolType
{
    Ethylene,     // Этиленгликоль
    Propylene     // Пропиленгликоль
}

public enum CollectorType
{
    HKV,          // Бытовой коллектор
    IV            // Промышленный коллектор
}
```

---

## 5. Интерфейсы

### 5.1. IHydraulicCalculator

```csharp
namespace SnowMeltingCalculator.Services.Hydraulics
{
    public interface IHydraulicCalculator
    {
        /// <summary>
        /// Рассчитать скорость потока
        /// Формула: w = v × 1000 / (3600 × π × di² / 4)
        /// </summary>
        double CalculateVelocity(double flowRate_L_h, double innerDiameter_mm);
        
        /// <summary>
        /// Рассчитать число Рейнольдса
        /// Формула: Re = 1000 × w × di / ν
        /// </summary>
        double CalculateReynoldsNumber(
            double velocity_m_s, 
            double innerDiameter_mm, 
            double kinematicViscosity_mm2_s);
        
        /// <summary>
        /// Определить режим течения
        /// Re < 2300 → Laminar
        /// 2300 ≤ Re ≤ 4000 → Transitional
        /// Re > 4000 → Turbulent
        /// </summary>
        FlowRegime DetermineFlowRegime(double reynoldsNumber);
        
        /// <summary>
        /// Рассчитать коэффициент трения λ
        /// Ламинарный: λ = 64 / Re
        /// Переходный: интерполяция
        /// Турбулентный: Colebrook-White
        /// </summary>
        double CalculateFrictionFactor(
            double reynoldsNumber, 
            double innerDiameter_mm, 
            double roughness_mm);
        
        /// <summary>
        /// Рассчитать удельные потери давления
        /// Формула: R = 1000 × (w² × ρ × λ) / (2 × di)
        /// </summary>
        double CalculatePressureLossPerMeter(
            double velocity_m_s, 
            double density_kg_m3, 
            double frictionFactor, 
            double innerDiameter_mm);
        
        /// <summary>
        /// Рассчитать потери давления в вентиле
        /// HKV-D: Δp = (v / 1000 / 1.2)² × 100000 × ρ
        /// IV 1¼": Δp = (v / 1000 / 1.45)² × 100000 × ρ
        /// IV 1½": Δp = (v / 1000 / 1.5)² × 100000 × ρ
        /// </summary>
        double CalculateValvePressureLoss(
            double flowRate_L_h, 
            double density_kg_m3, 
            CollectorType collectorType);
        
        /// <summary>
        /// Выполнить полный гидравлический расчёт
        /// </summary>
        HydraulicResult Calculate(HydraulicParameters parameters);
        
        /// <summary>
        /// Рассчитать балансировку контуров
        /// </summary>
        List<CircuitResult> CalculateBalancing(List<CircuitResult> circuits);
    }
}
```

### 5.2. IGlycolDataService

```csharp
namespace SnowMeltingCalculator.Services.Hydraulics
{
    public interface IGlycolDataService
    {
        /// <summary>
        /// Получить плотность гликоля
        /// Источник: data/glycol_data.json (интерполяция)
        /// </summary>
        double GetDensity(
            GlycolType glycolType, 
            double concentration, 
            double temperature);
        
        /// <summary>
        /// Получить теплоёмкость гликоля
        /// Источник: data/glycol_data.json (интерполяция)
        /// </summary>
        double GetSpecificHeat(
            GlycolType glycolType, 
            double concentration, 
            double temperature);
        
        /// <summary>
        /// Получить кинематическую вязкость гликоля
        /// Источник: data/glycol_data.json (интерполяция)
        /// </summary>
        double GetKinematicViscosity(
            GlycolType glycolType, 
            double concentration, 
            double temperature);
        
        /// <summary>
        /// Получить все свойства гликоля
        /// </summary>
        GlycolProperties GetProperties(
            GlycolType glycolType, 
            double concentration, 
            double temperature);
    }
}
```

### 5.3. ICollectorRepository

```csharp
namespace SnowMeltingCalculator.Repositories.Hydraulics
{
    public interface ICollectorRepository
    {
        /// <summary>
        /// Получить все коллекторы
        /// Источник: data/rehau_products.json
        /// </summary>
        Task<IEnumerable<Collector>> GetAllAsync();
        
        /// <summary>
        /// Получить коллектор по идентификатору
        /// </summary>
        Task<Collector?> GetByIdAsync(string id);
        
        /// <summary>
        /// Получить коллекторы по типу
        /// </summary>
        Task<IEnumerable<Collector>> GetByTypeAsync(CollectorType type);
        
        /// <summary>
        /// Получить коллектор по количеству контуров
        /// </summary>
        Task<Collector?> GetByCircuitsAsync(int circuits);
        
        /// <summary>
        /// Подобрать коллектор для заданного количества контуров
        /// </summary>
        Collector? SelectCollector(int circuits, double totalFlowRate_m3_h);
    }
}
```

### 5.4. IThermalCalculationResult (интеграция)

```csharp
namespace SnowMeltingCalculator.Models.Thermal
{
    /// <summary>
    /// Интерфейс для получения данных из ThermalModule
    /// </summary>
    public interface IThermalCalculationResult
    {
        double VolumeFlowRate { get; }      // л/(ч·м²)
        double SupplyTemperature { get; }   // °C
        double ReturnTemperature { get; }   // °C
        double PowerTotal { get; }          // Вт/м²
        bool IsValid { get; }
        
        event EventHandler ResultChanged;
    }
}
```

---

## 6. Формулы расчёта

### 6.1. Источник формул

Все формулы взяты из `docs/Formulas_Snegotayanie.md`, раздел 11 "ГИДРАВЛИЧЕСКИЙ РАСЧЁТ".

### 6.2. Основные формулы

#### Скорость потока
```
w = v × 1000 / (3600 × π × di² / 4)    [м/с]

где:
- v — расход, л/ч
- di — внутренний диаметр, мм
```

#### Число Рейнольдса
```
Re = 1000 × w × di / ν    [безразмерный]

где:
- w — скорость, м/с
- di — внутренний диаметр, мм
- ν — кинематическая вязкость, мм²/с
```

#### Режим течения
```
Re < 2300      → Ламинарный
2300 ≤ Re ≤ 4000 → Переходный
Re > 4000      → Турбулентный
```

#### Коэффициент трения λ

**Ламинарный режим (Re < 2300):**
```
λ = 64 / Re    (формула Пуазейля)
```

**Переходный режим (2300 ≤ Re ≤ 4000):**
```
λ_lam = 64 / 2300 ≈ 0.0278
λ_turb = ColebrookWhite(Re=4000)
λ = λ_lam + (Re - 2300) / 1700 × (λ_turb - λ_lam)
```

**Турбулентный режим (Re > 4000):**
```
1 / √λ = -2 × lg(ε / (3.7 × di) + 2.51 / (Re × √λ))

где ε — шероховатость трубы (PE-Xa: 0.007 мм)
```

#### Удельные потери давления
```
R = 1000 × (w² × ρ × λ) / (2 × di)    [Па/м]

где:
- w — скорость, м/с
- ρ — плотность, кг/м³
- λ — коэффициент трения
- di — внутренний диаметр, мм
```

#### Потери давления в вентиле

**HKV-D:**
```
Δp_вентиль = (v / 1000 / 1.2)² × 100000 × ρ    [Па]
```

**IV 1¼":**
```
Δp_вентиль = (v / 1000 / 1.45)² × 100000 × ρ    [Па]
```

**IV 1½":**
```
Δp_вентиль = (v / 1000 / 1.5)² × 100000 × ρ    [Па]
```

---

## 7. Интеграция с ThermalModule

> **Примечание:** Климатические данные (температура наружного воздуха, температура грунта и т.д.) получаются через `ThermalModule`, который в свою очередь получает их из `ClimateModule`. Прямое обращение `HydraulicsModule` к `ClimateModule` не требуется.

### 7.1. Получение данных

```csharp
// В HydraulicsViewModel
public class HydraulicsViewModel : ObservableObject
{
    private readonly IThermalCalculationResult _thermalResult;
    
    public HydraulicsViewModel(IThermalCalculationResult thermalResult)
    {
        _thermalResult = thermalResult;
        _thermalResult.ResultChanged += OnThermalResultChanged;
    }
    
    private void OnThermalResultChanged(object? sender, EventArgs e)
    {
        if (_thermalResult.IsValid)
        {
            // Автоматическое обновление параметров
            VolumeFlowRate = _thermalResult.VolumeFlowRate;
            SupplyTemperature = _thermalResult.SupplyTemperature;
            ReturnTemperature = _thermalResult.ReturnTemperature;
            
            // Пересчёт средней температуры
            MeanTemperature = (SupplyTemperature + ReturnTemperature) / 2;
        }
    }
}
```

### 7.2. Расчёт расхода на контур

```csharp
// Расход на контур = Удельный расход × Площадь контура
double circuitFlowRate = VolumeFlowRate * CircuitArea;  // л/ч
```

---

## 8. DI-регистрация

```csharp
// Services/Hydraulics/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHydraulicsServices(
        this IServiceCollection services)
    {
        // Repositories
        services.AddSingleton<ICollectorRepository, CollectorRepository>();
        
        // Services
        services.AddSingleton<IHydraulicCalculator, HydraulicCalculator>();
        services.AddSingleton<IGlycolDataService, GlycolDataService>();
        services.AddSingleton<HydraulicValidator>();
        
        // ViewModels
        services.AddSingleton<HydraulicsViewModel>();
        
        return services;
    }
}
```

---

## 9. Обработка ошибок

### 9.1. Исключения

| Ситуация | Исключение | Обработка |
|----------|------------|-----------|
| Тепловой расчёт невалиден | `InvalidOperationException` | Показать сообщение "Сначала выполните тепловой расчёт" |
| Файл glycol_data.json не найден | `FileNotFoundException` | Показать ошибку, использовать дефолтные значения |
| Температура вне диапазона | `ArgumentOutOfRangeException` | Интерполяция/экстраполяция |
| Re < 0 | `ArgumentException` | Ошибка валидации |
| Переходный режим | — | Предупреждение пользователю |

### 9.2. Валидация

```csharp
public ValidationResult Validate(HydraulicParameters parameters)
{
    var errors = new List<string>();
    var warnings = new List<string>();
    
    // Проверка длины контура
    if (parameters.CircuitLength < 10 || parameters.CircuitLength > 500)
        errors.Add($"Длина контура должна быть от 10 до 500 м");
    
    // Проверка доли гликоля
    if (parameters.GlycolConcentration < 10 || parameters.GlycolConcentration > 90)
        errors.Add($"Доля гликоля должна быть от 10 до 90%");
    
    // Проверка температур
    if (parameters.SupplyTemperature < 20 || parameters.SupplyTemperature > 90)
        errors.Add($"Температура подачи должна быть от 20 до 90°C");
    
    return new ValidationResult
    {
        IsValid = errors.Count == 0,
        Errors = errors,
        Warnings = warnings
    };
}
```

---

## 10. Тестирование

### 10.1. Unit-тесты

```csharp
// Tests/Services/HydraulicCalculatorTests.cs

[Test]
public void CalculateVelocity_WithValidInput_ReturnsCorrectValue()
{
    // Arrange
    var calculator = new HydraulicCalculator();
    double flowRate = 100;  // л/ч
    double diameter = 16;   // мм
    
    // Act
    double velocity = calculator.CalculateVelocity(flowRate, diameter);
    
    // Assert
    Assert.That(velocity, Is.EqualTo(0.138).Within(0.001));
}

[Test]
public void CalculateReynoldsNumber_WithValidInput_ReturnsCorrectValue()
{
    // Arrange
    var calculator = new HydraulicCalculator();
    double velocity = 0.5;     // м/с
    double diameter = 16;      // мм
    double viscosity = 2.16;   // мм²/с
    
    // Act
    double re = calculator.CalculateReynoldsNumber(velocity, diameter, viscosity);
    
    // Assert
    Assert.That(re, Is.EqualTo(3704).Within(1));
}

[Test]
public void DetermineFlowRegime_Laminar_ReturnsLaminar()
{
    var calculator = new HydraulicCalculator();
    
    Assert.That(calculator.DetermineFlowRegime(2000), Is.EqualTo(FlowRegime.Laminar));
    Assert.That(calculator.DetermineFlowRegime(3000), Is.EqualTo(FlowRegime.Transitional));
    Assert.That(calculator.DetermineFlowRegime(5000), Is.EqualTo(FlowRegime.Turbulent));
}

[Test]
public void CalculateFrictionFactor_Laminar_ReturnsCorrectValue()
{
    // Arrange
    var calculator = new HydraulicCalculator();
    
    // Act
    double lambda = calculator.CalculateFrictionFactor(2000, 16, 0.007);
    
    // Assert
    // Ламинарный режим: λ = 64 / Re
    Assert.That(lambda, Is.EqualTo(64.0 / 2000).Within(0.0001));
}
```

### 10.2. Интеграционные тесты

```csharp
[Test]
public async Task GlycolDataService_GetProperties_ReturnsInterpolatedValues()
{
    // Arrange
    var service = new GlycolDataService("data/glycol_data.json");
    
    // Act
    var props = service.GetProperties(GlycolType.Propylene, 50, 41);
    
    // Assert
    Assert.That(props.Density, Is.GreaterThan(1050));
    Assert.That(props.KinematicViscosity, Is.GreaterThan(2));
}
```

---

## 11. Ограничения и допущения

### 11.1. Технические ограничения

| Ограничение | Значение |
|-------------|----------|
| Платформа | Windows 10+ |
| Фреймворк | .NET 8, WPF |
| Архитектура | MVVM (CommunityToolkit.Mvvm) |
| DI | Microsoft.Extensions.DependencyInjection |

### 11.2. Бизнес-ограничения

| Ограничение | Значение |
|-------------|----------|
| Трубы | Только RAUTHERM S (PE-Xa) |
| Коллекторы | Только РЕХАУ HKV-D и IV |
| Шероховатость | 0.007 мм (PE-Xa) |
| Макс. контуров на коллектор | 12 |
| Макс. потери давления | 320 мбар |

### 11.3. Допущения

1. Температура теплоносителя постоянна по длине контура
2. Вязкость определяется по средней температуре
3. Потери давления в фитингах не учитываются
4. Подводка имеет тот же диаметр, что и контур

---

## 12. Открытые вопросы

### 12.1. Требующие уточнения

| Вопрос | Варианты | Рекомендация |
|--------|----------|--------------|
| Учёт местных сопротивлений | a) Не учитывать<br>b) Коэффициент запаса 15%<br>c) Формула Дарси-Вейсбаха | Вариант (b) |
| Разные диаметры подводки и контура | Да/Нет | Нет в первой версии |
| Расчёт нескольких коллекторов | Да/Нет | Добавить в следующей версии |
| Экспорт результатов | Excel/PDF | Excel |

---

## 13. История изменений

| Версия | Дата | Автор | Изменения |
|--------|------|-------|-----------|
| 1.1 | 15.03.2026 | Архитектор | Добавлен класс GlycolProperties; добавлено примечание об интеграции с ClimateModule через ThermalModule; добавлено свойство Kv и описание ConnectionSize для коллекторов |
| 1.0 | 15.03.2026 | Архитектор | Начальная версия |