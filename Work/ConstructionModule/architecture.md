# Архитектура модуля "Конструктор конструкции" ("Пирог")

**Проект:** Калькулятор снеготаяния РЕХАУ  
**Версия:** 1.0  
**Дата:** 2026-03-15  
**Статус:** Утверждена  
**ТЗ:** `Work/ConstructionModule/technical_specification.md`

---

## 1. Описание задачи

### 1.1. Краткое описание

Модуль "Конструктор конструкции" ("Пирог") предназначен для визуального проектирования слоёв конструкции системы снеготаяния. Пользователь задаёт слои материалов над трубой и под трубой, система автоматически рассчитывает термические сопротивления R1, R2 и LambdaE, передавая данные в модуль теплового расчёта через интерфейс `IConstructionData`.

### 1.2. Ключевые требования

1. **Визуализация слоёв** — отображение "пирога" конструкции с динамической отрисовкой
2. **Расчёт R1/R2** — автоматический расчёт термических сопротивлений по формуле `R = d / λ / 1000`
3. **Учёт УГВ** — автоматический выбор λА/λБ в зависимости от уровня грунтовых вод
4. **Интеграция** — передача данных в ThermalViewModel через событие `DataChanged`
5. **Валидация** — проверка минимальной стяжки, ограничений по материалам

### 1.3. Решения по открытым вопросам

| Вопрос | Решение | Обоснование |
|--------|---------|-------------|
| LambdaE | Первый слой над трубой | Автоматическое определение по категории материала |
| Drag-and-drop | Нет | В первой версии — только добавление/удаление |
| Сохранение/загрузка | Да (JSON, SQLite) | Экспорт/импорт и хранение в проектах |
| Шаблоны | Да | 3 типовых шаблона: парковка, дорожка, гараж |
| Выбор трубы | Нет | Труба выбирается в ThermalViewModel |

---

## 2. Функциональная архитектура

### 2.1. Функциональные компоненты

#### 2.1.1. Компонент "Управление материалами"

**Название:** MaterialRepository

**Назначение:** Загрузка и управление справочником материалов из `data/materials_db.json`

**Функции:**

| Функция | Описание | Входные данные | Выходные данные | Связанные UC |
|---------|----------|----------------|-----------------|--------------|
| `LoadMaterialsAsync()` | Загрузка материалов из JSON | Путь к файлу | `List<Material>` | UC-02 |
| `GetMaterialById(int id)` | Получение материала по ID | ID материала | `Material` | UC-02 |
| `GetMaterialsByCategory(string category)` | Фильтрация по категории | Категория | `List<Material>` | UC-02 |
| `GetDefaultMaterial()` | Материал по умолчанию | — | `Material` (Бетон плотный) | UC-01 |

**Зависимости:**
- От: System.Text.Json (десериализация)
- К нему: ConstructionService, ConstructionViewModel

---

#### 2.1.2. Компонент "Управление конструкцией"

**Название:** ConstructionService

**Назначение:** Расчёт термических сопротивлений и управление слоями конструкции

**Функции:**

| Функция | Описание | Входные данные | Выходные данные | Связанные UC |
|---------|----------|----------------|-----------------|--------------|
| `CalculateR1(Construction)` | Расчёт R1 (над трубой) | Конструкция | `double` (м²·К/Вт) | UC-01, UC-03 |
| `CalculateR2(Construction)` | Расчёт R2 (под трубой) | Конструкция | `double` (м²·К/Вт) | UC-01, UC-03 |
| `CalculateLambdaE(Construction)` | Определение LambdaE | Конструкция | `double` (Вт/м·К) | UC-01 |
| `GetLambdaForLayer(Material, LayerPosition, double ugw)` | Выбор λА/λБ | Материал, позиция, УГВ | `double` | UC-05 |
| `Validate(Construction, double supplyTemp, double airTemp)` | Валидация конструкции | Конструкция, температуры | `ValidationResult` | UC-06, UC-07 |

**Формулы (из `docs/Formulas_Snegotayanie.md`):**

```
R = d / λ / 1000    [м²·К/Вт]

где:
- d — толщина слоя, мм
- λ — теплопроводность материала, Вт/м·К

R1Total = Σ(R_i) для всех слоёв над трубой
R2Total = Σ(R_i) для всех слоёв под трубой

λ = {
    λА, если УГВ >= 1 м (сухие условия)
    λБ, если УГВ < 1 м (влажные условия)
}

Примечание: Только для слоёв ПОД трубой. Слои НАД трубой всегда используют λА.
```

**Зависимости:**
- От: IMaterialRepository, IClimateData (для валидации)
- К нему: ConstructionViewModel, ThermalViewModel

---

#### 2.1.3. Компонент "Хранение конструкций"

**Название:** ConstructionRepository

**Назначение:** Сохранение и загрузка конструкций (JSON, SQLite)

**Функции:**

| Функция | Описание | Входные данные | Выходные данные | Связанные UC |
|---------|----------|----------------|-----------------|--------------|
| `SaveConstructionAsync(Construction)` | Сохранение в JSON | Конструкция | Путь к файлу | — |
| `LoadConstructionAsync(string path)` | Загрузка из JSON | Путь к файлу | `Construction` | — |
| `SaveToProjectAsync(Construction, int projectId)` | Сохранение в проект | Конструкция, ID проекта | — | — |
| `LoadFromProjectAsync(int projectId)` | Загрузка из проекта | ID проекта | `Construction` | — |
| `GetTemplates()` | Получение шаблонов | — | `List<ConstructionTemplate>` | — |

**Зависимости:**
- От: System.Text.Json, Microsoft.Data.Sqlite
- К нему: ConstructionViewModel

---

#### 2.1.4. Компонент "Валидация"

**Название:** ConstructionValidator

**Назначение:** Проверка корректности конструкции

**Правила валидации:**

| Параметр | Правило | Сообщение об ошибке |
|----------|---------|---------------------|
| Толщина слоя | 10 ≤ d ≤ 1000 мм | "Толщина слоя должна быть от 10 до 1000 мм" |
| Суммарная толщина над трубой (без нагрузок) | ≥ 40 мм | "Минимальная стяжка над трубой: 40 мм" |
| Суммарная толщина над трубой (с нагрузками) | ≥ 50 мм | "Минимальная стяжка над трубой при нагрузках: 50 мм" |
| УГВ | 0 ≤ УГВ ≤ 10 м | "Уровень грунтовых вод должен быть от 0 до 10 м" |
| Бетон + температура подачи | T_подачи ≤ 50°C | "Бетон: максимальная температура подачи 50°C" |
| Асфальт + температура воздуха | T_воздуха > -15°C | "Асфальт не применяется при температуре ≤ -15°C" |

**Зависимости:**
- От: IClimateData (для проверки температуры воздуха)
- К нему: ConstructionViewModel

---

### 2.2. Диаграмма компонентов

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ConstructionModule                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────┐ │
│  │  ConstructionView   │◄──►│ ConstructionViewModel │◄──►│ Construction   │ │
│  │     (WPF View)      │    │    (MVVM ViewModel)  │    │    (Model)     │ │
│  └─────────────────────┘    └─────────────────────┘    └─────────────────┘ │
│           │                           │                           │          │
│           │                           │                           │          │
│           │                           ▼                           ▼          │
│           │              ┌─────────────────────┐    ┌─────────────────────┐  │
│           │              │ ConstructionService │    │ MaterialRepository │  │
│           │              │   (Business Logic)   │    │   (Data Access)    │  │
│           │              └─────────────────────┘    └─────────────────────┘  │
│           │                           │                           │          │
│           │                           ▼                           ▼          │
│           │              ┌─────────────────────┐    ┌─────────────────────┐  │
│           │              │ ConstructionValidator│    │ materials_db.json   │  │
│           │              │   (Validation)       │    │   (JSON File)       │  │
│           │              └─────────────────────┘    └─────────────────────┘  │
│           │                           │                                      │
│           │                           ▼                                      │
│           │              ┌─────────────────────┐                             │
│           │              │ ConstructionRepository│                            │
│           │              │   (Persistence)      │                            │
│           │              └─────────────────────┘                             │
│           │                           │                                      │
│           │                           ▼                                      │
│           │              ┌─────────────────────┐                             │
│           │              │ SQLite / JSON Files │                             │
│           │              └─────────────────────┘                             │
│           │                                                                  │
│           ▼                                                                  │
│  ┌─────────────────────┐                                                    │
│  │   IConstructionData │◄───────────────────────────────────────────────────┤
│  │     (Interface)     │                                                    │
│  └─────────────────────┘                                                    │
│           │                                                                  │
│           ▼                                                                  │
│  ┌─────────────────────┐                                                    │
│  │  ThermalViewModel   │                                                    │
│  │   (Consumer)        │                                                    │
│  └─────────────────────┘                                                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Системная архитектура

### 3.1. Архитектурный стиль

**Многоуровневая архитектура (Layered)** с MVVM:

1. **Presentation Layer** — Views (WPF UserControl)
2. **Application Layer** — ViewModels (MVVM, CommunityToolkit.Mvvm)
3. **Business Logic Layer** — Services (ConstructionService, ConstructionValidator)
4. **Data Access Layer** — Repositories (MaterialRepository, ConstructionRepository)
5. **Domain Layer** — Models (Material, Layer, Construction)

### 3.2. Структура проекта

```
src/
├── Models/
│   ├── Climate/                    # Существующий модуль
│   ├── Thermal/                    # Существующий модуль
│   │   ├── IConstructionData.cs    # Интерфейс (существует)
│   │   ├── ConstructionData.cs     # Заглушка (заменяется)
│   │   └── ...
│   └── Construction/               # НОВЫЙ МОДУЛЬ
│       ├── Material.cs             # Модель материала
│       ├── Layer.cs                # Модель слоя
│       ├── LayerPosition.cs        # Enum: AbovePipe, BelowPipe
│       ├── Construction.cs         # Модель конструкции (реализует IConstructionData)
│       ├── ConstructionTemplate.cs # Шаблон конструкции
│       └── ValidationResult.cs     # Результат валидации
│
├── Services/
│   ├── Climate/                    # Существующий модуль
│   ├── Thermal/                    # Существующий модуль
│   └── Construction/               # НОВЫЙ МОДУЛЬ
│       ├── IMaterialRepository.cs  # Интерфейс репозитория материалов
│       ├── MaterialRepository.cs   # Реализация репозитория материалов
│       ├── IConstructionService.cs # Интерфейс сервиса конструкции
│       ├── ConstructionService.cs  # Реализация сервиса конструкции
│       ├── IConstructionRepository.cs # Интерфейс репозитория конструкций
│       ├── ConstructionRepository.cs  # Реализация репозитория конструкций
│       └── ConstructionValidator.cs    # Валидатор конструкции
│
├── ViewModels/
│   ├── Climate/                    # Существующий модуль
│   ├── Thermal/                    # Существующий модуль
│   └── Construction/               # НОВЫЙ МОДУЛЬ
│       └── ConstructionViewModel.cs # ViewModel конструктора
│
├── Views/
│   ├── Climate/                    # Существующий модуль
│   ├── Thermal/                    # Существующий модуль
│   └── Construction/               # НОВЫЙ МОДУЛЬ
│       └── ConstructionView.xaml   # UserControl конструктора
│       └── ConstructionView.xaml.cs
│
├── Configuration/
│   └── ServiceCollectionExtensions.cs  # Регистрация сервисов (обновить)
│
└── Converters/
    └── Converters.cs               # Конвертеры (обновить)
```

### 3.3. Интеграция с существующим кодом

#### 3.3.1. Обновление `IConstructionData`

**Существующий интерфейс** (`src/Models/Thermal/IConstructionData.cs`):

```csharp
public interface IConstructionData
{
    double R1Total { get; }
    double R2Total { get; }
    double LambdaE { get; }
    bool IsValid { get; }
    event EventHandler<ConstructionDataChangedEventArgs>? DataChanged;
}
```

**Новая реализация** (`src/Models/Construction/Construction.cs`):

- Класс `Construction` реализует `IConstructionData`
- Заменяет заглушку `ConstructionData`
- Регистрируется в DI как `IConstructionData`

#### 3.3.2. Обновление `ServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddConstructionModule(this IServiceCollection services)
{
    // Repositories
    services.AddSingleton<IMaterialRepository, MaterialRepository>();
    services.AddSingleton<IConstructionRepository, ConstructionRepository>();

    // Services
    services.AddSingleton<IConstructionService, ConstructionService>();
    services.AddSingleton<ConstructionValidator>();

    // ViewModels
    services.AddSingleton<ConstructionViewModel>();

    // Data (заменяет заглушку ConstructionData)
    services.AddSingleton<IConstructionData, Construction>();

    return services;
}

public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    return services
        .AddClimateModule()
        .AddConstructionModule()  // НОВЫЙ МОДУЛЬ
        .AddThermalModule();
}
```

#### 3.3.3. Обновление `ThermalViewModel`

**Существующий код** подписывается на `ConstructionDataChanged`:

```csharp
// Подписка на изменения данных конструкции
if (_constructionData is ConstructionData constructionDataImpl)
{
    constructionDataImpl.DataChanged += OnConstructionDataChanged;
}
```

**Новый код** (без изменений, полиморфизм):

```csharp
// Подписка на изменения данных конструкции
if (_constructionData is Construction constructionImpl)
{
    constructionImpl.DataChanged += OnConstructionDataChanged;
}
```

---

## 4. Модель данных

### 4.1. Диаграмма классов

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Models.Construction                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌───────────────────┐         ┌───────────────────┐                       │
│  │     Material      │         │      Layer        │                       │
│  ├───────────────────┤         ├───────────────────┤                       │
│  │ +Id: int          │◄────────│+Id: Guid          │                       │
│  │ +Name: string     │  1    * │+Material: Material│                       │
│  │ +LambdaA: double  │         │+Thickness: double │                       │
│  │ +LambdaB: double  │         │+Lambda: double    │                       │
│  │ +Category: string │         │+IsLambdaOverridden: bool                  │
│  │ +Notes: string    │         │+Position: LayerPosition                   │
│  │ +MaxSupplyTemp:   │         │+Order: int        │                       │
│  │  double?          │         ├───────────────────┤                       │
│  │ +MinAirTemp:      │         │+ThermalResistance: double {readonly}      │
│  │  double?          │         └───────────────────┘                       │
│  └───────────────────┘                   │                                 │
│                                          │                                 │
│                                          │ *                               │
│                                          │                                 │
│  ┌───────────────────┐                   │                                 │
│  │   Construction    │                   │                                 │
│  ├───────────────────┤◄──────────────────┘                                 │
│  │ +LayersAbovePipe: │                                                   │
│  │  ObservableCollection<Layer>                                          │
│  │ +LayersBelowPipe: │                                                   │
│  │  ObservableCollection<Layer>                                          │
│  │ +GroundwaterLevel: double                                              │
│  │ +HasLoads: bool   │                                                   │
│  │ +MaterialAroundPipe: Material?                                         │
│  ├───────────────────┤                                                   │
│  │ +R1Total: double {readonly}                                           │
│  │ +R2Total: double {readonly}                                           │
│  │ +LambdaE: double {readonly}                                           │
│  │ +IsValid: bool {readonly}                                             │
│  ├───────────────────┤                                                   │
│  │ +AddLayerAbovePipe(Material, double): void                            │
│  │ +AddLayerBelowPipe(Material, double): void                            │
│  │ +RemoveLayer(Layer): void                                             │
│  │ +UpdateLambdaForGroundwater(): void                                   │
│  │ +RaiseDataChanged(string, object?, object?, bool): void               │
│  ├───────────────────┤                                                   │
│  │ +event DataChanged: EventHandler<ConstructionDataChangedEventArgs>     │
│  └───────────────────┘                                                   │
│          │                                                                │
│          │ implements                                                     │
│          ▼                                                                │
│  ┌───────────────────┐                                                   │
│  │ IConstructionData │                                                   │
│  ├───────────────────┤                                                   │
│  │ +R1Total: double  │                                                   │
│  │ +R2Total: double  │                                                   │
│  │ +LambdaE: double  │                                                   │
│  │ +IsValid: bool    │                                                   │
│  │ +event DataChanged│                                                   │
│  └───────────────────┘                                                   │
│                                                                              │
│  ┌───────────────────┐         ┌───────────────────┐                       │
│  │  LayerPosition    │         │ ValidationResult │                       │
│  ├───────────────────┤         ├───────────────────┤                       │
│  │ AbovePipe = 0     │         │ +IsValid: bool    │                       │
│  │ BelowPipe = 1     │         │ +Errors: List<string>                      │
│  └───────────────────┘         └───────────────────┘                       │
│                                                                              │
│  ┌─────────────────────────────┐                                           │
│  │   ConstructionTemplate      │                                           │
│  ├─────────────────────────────┤                                           │
│  │ +Id: int                    │                                           │
│  │ +Name: string               │                                           │
│  │ +Description: string        │                                           │
│  │ +LayersAbovePipe: List<LayerTemplate>                                   │
│  │ +LayersBelowPipe: List<LayerTemplate>                                   │
│  │ +HasLoads: bool             │                                           │
│  └─────────────────────────────┘                                           │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.2. Описание классов

#### 4.2.1. Material (Материал)

**Файл:** `src/Models/Construction/Material.cs`

**Назначение:** Модель материала из справочника `materials_db.json`

**Атрибуты:**

| Атрибут | Тип | Описание | Пример |
|---------|-----|----------|--------|
| `Id` | `int` | Идентификатор материала | 1 |
| `Name` | `string` | Название материала | "Бетон плотный" |
| `LambdaA` | `double` | Теплопроводность в сухих условиях (УГВ ≥ 1м) | 1.5 |
| `LambdaB` | `double` | Теплопроводность во влажных условиях (УГВ < 1м) | 1.5 |
| `Category` | `string` | Категория материала | "бетон", "грунт", "изоляция", "покрытие", "стяжка" |
| `Notes` | `string` | Примечания | "Не зависит от влажности" |
| `MaxSupplyTemperature` | `double?` | Макс. температура подачи (для бетона = 50°C) | 50.0 |
| `MinAirTemperature` | `double?` | Мин. температура воздуха (для асфальта = -15°C) | -15.0 |

**Категории материалов:**

| Категория | Материалы | Цвет на визуализации |
|-----------|-----------|----------------------|
| бетон | Бетон на каменном щебне, Бетон на песке, Бетон плотный, Железобетон | Серый (#808080) |
| грунт | Песок, Грунт | Коричневый (#8B4513) |
| изоляция | Пенополистирол ЭППС | Жёлтый (#FFD700) |
| покрытие | Асфальтобетон, Асфальт | Чёрный (#000000) |
| подстилающий | Щебень/Гравий | Серый (#A0A0A0) |
| стяжка | Цементно-песчаная стяжка | Светло-серый (#C0C0C0) |

---

#### 4.2.2. Layer (Слой)

**Файл:** `src/Models/Construction/Layer.cs`

**Назначение:** Модель слоя конструкции

**Атрибуты:**

| Атрибут | Тип | Описание | По умолчанию |
|---------|-----|----------|--------------|
| `Id` | `Guid` | Уникальный идентификатор слоя | `Guid.NewGuid()` |
| `Material` | `Material` | Материал слоя | — |
| `Thickness` | `double` | Толщина слоя, мм | 50.0 |
| `Lambda` | `double` | Теплопроводность (λ), Вт/м·К | — |
| `IsLambdaOverridden` | `bool` | Признак ручного изменения λ | `false` |
| `Position` | `LayerPosition` | Позиция относительно трубы | — |
| `Order` | `int` | Порядковый номер слоя | — |

**Вычисляемые свойства:**

| Свойство | Формула | Описание |
|----------|---------|----------|
| `ThermalResistance` | `Thickness / Lambda / 1000.0` | Термическое сопротивление, м²·К/Вт |

**Методы:**

| Метод | Описание |
|-------|----------|
| `UpdateLambda(double groundwaterLevel)` | Обновление λ в зависимости от УГВ |

---

#### 4.2.3. LayerPosition (Позиция слоя)

**Файл:** `src/Models/Construction/LayerPosition.cs`

**Назначение:** Enum для позиции слоя относительно трубы

```csharp
public enum LayerPosition
{
    AbovePipe = 0,  // Над трубой (к поверхности)
    BelowPipe = 1   // Под трубой (к грунту)
}
```

---

#### 4.2.4. Construction (Конструкция)

**Файл:** `src/Models/Construction/Construction.cs`

**Назначение:** Модель конструкции ("Пирог"), реализует `IConstructionData`

**Атрибуты:**

| Атрибут | Тип | Описание | По умолчанию |
|---------|-----|----------|--------------|
| `LayersAbovePipe` | `ObservableCollection<Layer>` | Слои над трубой | Пустая коллекция |
| `LayersBelowPipe` | `ObservableCollection<Layer>` | Слои под трубой | Пустая коллекция |
| `GroundwaterLevel` | `double` | Уровень грунтовых вод, м | 2.0 |
| `HasLoads` | `bool` | Признак наличия нагрузок | `false` |
| `MaterialAroundPipe` | `Material?` | Материал вокруг трубы (для LambdaE) | `null` |

**Вычисляемые свойства (из IConstructionData):**

| Свойство | Формула | Описание |
|----------|---------|----------|
| `R1Total` | `LayersAbovePipe.Sum(l => l.ThermalResistance)` | Суммарное R над трубой |
| `R2Total` | `LayersBelowPipe.Sum(l => l.ThermalResistance)` | Суммарное R под трубой |
| `LambdaE` | `MaterialAroundPipe?.LambdaA ?? 1.6` | Теплопроводность вокруг трубы |
| `IsValid` | `ValidateConstruction()` | Признак валидности |

**Методы:**

| Метод | Описание | Связанные UC |
|-------|----------|--------------|
| `AddLayerAbovePipe(Material, double)` | Добавить слой над трубой | UC-01 |
| `AddLayerBelowPipe(Material, double)` | Добавить слой под трубой | UC-01 |
| `RemoveLayer(Layer)` | Удалить слой | UC-04 |
| `UpdateLambdaForGroundwater()` | Обновить λ для всех слоёв под трубой | UC-05 |
| `RaiseDataChanged(string, object?, object?, bool)` | Вызвать событие изменения | UC-09 |

**События:**

| Событие | Тип | Описание |
|---------|-----|----------|
| `DataChanged` | `EventHandler<ConstructionDataChangedEventArgs>` | Событие изменения данных |

---

#### 4.2.5. ConstructionTemplate (Шаблон конструкции)

**Файл:** `src/Models/Construction/ConstructionTemplate.cs`

**Назначение:** Предустановленный шаблон конструкции

**Атрибуты:**

| Атрибут | Тип | Описание |
|---------|-----|----------|
| `Id` | `int` | Идентификатор шаблона |
| `Name` | `string` | Название шаблона |
| `Description` | `string` | Описание |
| `LayersAbovePipe` | `List<LayerTemplate>` | Слои над трубой |
| `LayersBelowPipe` | `List<LayerTemplate>` | Слои под трубой |
| `HasLoads` | `bool` | Признак наличия нагрузок |

**Предустановленные шаблоны:**

| ID | Название | Описание | Слои над трубой | Слои под трубой |
|----|----------|----------|-----------------|-----------------|
| 1 | Типовая парковка | Стандартная конструкция для парковок | Асфальтобетон 50мм, Бетон плотный 100мм | Песок 150мм, Грунт |
| 2 | Пешеходная дорожка | Облегчённая конструкция | Асфальтобетон 40мм, Цементно-песчаная стяжка 50мм | Песок 100мм, Грунт |
| 3 | Въезд в гараж | Усиленная конструкция | Асфальтобетон 50мм, Железобетон 150мм | Песок 200мм, Грунт |

---

#### 4.2.6. ValidationResult (Результат валидации)

**Файл:** `src/Models/Construction/ValidationResult.cs`

**Назначение:** Результат валидации конструкции

**Атрибуты:**

| Атрибут | Тип | Описание |
|---------|-----|----------|
| `IsValid` | `bool` | Признак валидности |
| `Errors` | `List<string>` | Список ошибок |

---

### 4.3. JSON-модели

#### 4.3.1. MaterialJsonModel (для десериализации)

```csharp
private class MaterialJsonModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("lambda_A")]
    public double LambdaA { get; set; }

    [JsonPropertyName("lambda_B")]
    public double LambdaB { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}
```

---

## 5. Интерфейсы

### 5.1. IMaterialRepository

**Файл:** `src/Services/Construction/IMaterialRepository.cs`

**Назначение:** Интерфейс репозитория материалов

```csharp
namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Интерфейс репозитория материалов
    /// </summary>
    public interface IMaterialRepository
    {
        /// <summary>
        /// Загрузить все материалы из справочника
        /// </summary>
        Task<IEnumerable<Material>> LoadMaterialsAsync();

        /// <summary>
        /// Получить материал по идентификатору
        /// </summary>
        Material? GetMaterialById(int id);

        /// <summary>
        /// Получить материалы по категории
        /// </summary>
        IEnumerable<Material> GetMaterialsByCategory(string category);

        /// <summary>
        /// Получить материал по умолчанию (Бетон плотный)
        /// </summary>
        Material GetDefaultMaterial();

        /// <summary>
        /// Признак того, что данные загружены
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Количество загруженных материалов
        /// </summary>
        int MaterialsCount { get; }
    }
}
```

---

### 5.2. IConstructionService

**Файл:** `src/Services/Construction/IConstructionService.cs`

**Назначение:** Интерфейс сервиса расчёта конструкции

```csharp
namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Интерфейс сервиса расчёта конструкции
    /// </summary>
    public interface IConstructionService
    {
        /// <summary>
        /// Рассчитать суммарное термическое сопротивление над трубой (R1)
        /// </summary>
        double CalculateR1Total(Construction construction);

        /// <summary>
        /// Рассчитать суммарное термическое сопротивление под трубой (R2)
        /// </summary>
        double CalculateR2Total(Construction construction);

        /// <summary>
        /// Определить теплопроводность материала вокруг трубы (LambdaE)
        /// </summary>
        double CalculateLambdaE(Construction construction);

        /// <summary>
        /// Получить λ для слоя в зависимости от УГВ
        /// </summary>
        double GetLambdaForLayer(Material material, LayerPosition position, double groundwaterLevel);

        /// <summary>
        /// Создать слой с материалом по умолчанию
        /// </summary>
        Layer CreateDefaultLayer(LayerPosition position, double groundwaterLevel);

        /// <summary>
        /// Применить шаблон конструкции
        /// </summary>
        Construction ApplyTemplate(ConstructionTemplate template);
    }
}
```

---

### 5.3. IConstructionRepository

**Файл:** `src/Services/Construction/IConstructionRepository.cs`

**Назначение:** Интерфейс репозитория конструкций

```csharp
namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Интерфейс репозитория конструкций
    /// </summary>
    public interface IConstructionRepository
    {
        /// <summary>
        /// Сохранить конструкцию в JSON-файл
        /// </summary>
        Task SaveToJsonAsync(Construction construction, string filePath);

        /// <summary>
        /// Загрузить конструкцию из JSON-файла
        /// </summary>
        Task<Construction?> LoadFromJsonAsync(string filePath);

        /// <summary>
        /// Сохранить конструкцию в проект (SQLite)
        /// </summary>
        Task SaveToProjectAsync(Construction construction, int projectId);

        /// <summary>
        /// Загрузить конструкцию из проекта (SQLite)
        /// </summary>
        Task<Construction?> LoadFromProjectAsync(int projectId);

        /// <summary>
        /// Получить предустановленные шаблоны конструкций
        /// </summary>
        IEnumerable<ConstructionTemplate> GetTemplates();
    }
}
```

---

### 5.4. ConstructionValidator

**Файл:** `src/Services/Construction/ConstructionValidator.cs`

**Назначение:** Валидатор конструкции (не интерфейс, конкретный класс)

```csharp
namespace SnowMeltingCalculator.Services.Construction
{
    /// <summary>
    /// Валидатор конструкции
    /// </summary>
    public class ConstructionValidator
    {
        private readonly IClimateData _climateData;

        public ConstructionValidator(IClimateData climateData)
        {
            _climateData = climateData;
        }

        /// <summary>
        /// Валидация конструкции
        /// </summary>
        public ValidationResult Validate(Construction construction, double supplyTemperature)
        {
            var errors = new List<string>();

            // Проверка минимальной стяжки над трубой
            var minThickness = construction.HasLoads ? 50.0 : 40.0;
            var totalAbove = construction.LayersAbovePipe.Sum(l => l.Thickness);
            if (totalAbove < minThickness)
            {
                errors.Add($"Минимальная стяжка над трубой: {minThickness} мм (текущая: {totalAbove} мм)");
            }

            // Проверка толщины слоёв
            foreach (var layer in construction.LayersAbovePipe.Concat(construction.LayersBelowPipe))
            {
                if (layer.Thickness < 10 || layer.Thickness > 1000)
                {
                    errors.Add($"Толщина слоя '{layer.Material.Name}' должна быть от 10 до 1000 мм");
                }
            }

            // Проверка материалов
            foreach (var layer in construction.LayersAbovePipe)
            {
                // Бетон: макс. температура подачи 50°C
                if (layer.Material.Category == "бетон" && supplyTemperature > 50)
                {
                    errors.Add($"Бетон: максимальная температура подачи 50°C (текущая: {supplyTemperature}°C)");
                }

                // Асфальт: не применять при t ≤ -15°C
                if (layer.Material.Name.Contains("Асфальт") && _climateData.AirTemperature <= -15)
                {
                    errors.Add($"Асфальт не применяется при температуре наружного воздуха ≤ -15°C");
                }
            }

            // Проверка УГВ
            if (construction.GroundwaterLevel < 0 || construction.GroundwaterLevel > 10)
            {
                errors.Add("Уровень грунтовых вод должен быть от 0 до 10 м");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }
    }
}
```

---

## 6. ViewModel

### 6.1. ConstructionViewModel

**Файл:** `src/ViewModels/Construction/ConstructionViewModel.cs`

**Назначение:** MVVM ViewModel для конструктора конструкции

**Базовый класс:** `ObservableObject` (CommunityToolkit.Mvvm)

**Зависимости:**
- `IMaterialRepository` — загрузка материалов
- `IConstructionService` — расчёт R1/R2/LambdaE
- `IConstructionRepository` — сохранение/загрузка конструкций
- `Construction` — модель конструкции (реализует `IConstructionData`)
- `IClimateData` — климатические данные (для валидации)
- `ConstructionValidator` — валидация

**Observable Properties:**

| Свойство | Тип | Описание | По умолчанию |
|----------|-----|----------|--------------|
| `LayersAbovePipe` | `ObservableCollection<Layer>` | Слои над трубой | Пустая коллекция |
| `LayersBelowPipe` | `ObservableCollection<Layer>` | Слои под трубой | Пустая коллекция |
| `AvailableMaterials` | `ObservableCollection<Material>` | Доступные материалы | Загружается из репозитория |
| `GroundwaterLevel` | `double` | Уровень грунтовых вод, м | 2.0 |
| `HasLoads` | `bool` | Признак наличия нагрузок | `false` |
| `ValidationMessage` | `string` | Сообщение валидации | `""` |
| `IsValid` | `bool` | Признак валидности | `true` |

**Computed Properties:**

| Свойство | Формула | Описание |
|----------|---------|----------|
| `R1Total` | `_construction.R1Total` | Суммарное R над трубой |
| `R2Total` | `_construction.R2Total` | Суммарное R под трубой |
| `LambdaE` | `_construction.LambdaE` | Теплопроводность вокруг трубы |

**Commands:**

| Команда | Метод | Описание | Связанные UC |
|---------|-------|----------|--------------|
| `AddLayerAbovePipeCommand` | `AddLayerAbovePipe()` | Добавить слой над трубой | UC-01 |
| `AddLayerBelowPipeCommand` | `AddLayerBelowPipe()` | Добавить слой под трубой | UC-01 |
| `RemoveLayerCommand` | `RemoveLayer(Layer)` | Удалить слой | UC-04 |
| `SaveConstructionCommand` | `SaveConstruction()` | Сохранить конструкцию | — |
| `LoadConstructionCommand` | `LoadConstruction()` | Загрузить конструкцию | — |
| `ApplyTemplateCommand` | `ApplyTemplate(ConstructionTemplate)` | Применить шаблон | — |

**Методы:**

| Метод | Описание |
|-------|----------|
| `OnGroundwaterLevelChanged(double)` | Обработчик изменения УГВ |
| `OnHasLoadsChanged(bool)` | Обработчик изменения флага нагрузок |
| `Validate()` | Валидация конструкции |
| `UpdateConstruction()` | Обновление модели и вызов `DataChanged` |

**События:**

| Событие | Описание |
|---------|----------|
| `DataChanged` | Событие изменения данных (проброс из `Construction`) |

---

### 6.2. Диаграмма состояний ViewModel

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ConstructionViewModel                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────┐                                                            │
│  │   Initial   │                                                            │
│  │   State     │                                                            │
│  └──────┬──────┘                                                            │
│         │                                                                    │
│         │ LoadMaterialsAsync()                                               │
│         ▼                                                                    │
│  ┌─────────────┐                                                            │
│  │   Loaded    │                                                            │
│  │   State     │                                                            │
│  └──────┬──────┘                                                            │
│         │                                                                    │
│         │ AddLayerAbovePipe() / AddLayerBelowPipe()                          │
│         ▼                                                                    │
│  ┌─────────────┐                                                            │
│  │   Editing   │◄───────────────────────────────────────────────────────────┐│
│  │   State     │                                                            ││
│  └──────┬──────┘                                                            ││
│         │                                                                    ││
│         │ Validate()                                                         ││
│         ▼                                                                    ││
│  ┌─────────────┐                                                            ││
│  │  Validating │                                                            ││
│  │   State     │                                                            ││
│  └──────┬──────┘                                                            ││
│         │                                                                    ││
│         │ IsValid?                                                           ││
│         │                                                                    ││
│    ┌────┴────┐                                                              ││
│    │         │                                                              ││
│    ▼         ▼                                                              ││
│ ┌────────┐ ┌────────┐                                                      ││
│ │ Valid  │ │Invalid │                                                      ││
│ │ State  │ │ State  │                                                      ││
│ └────┬───┘ └────┬───┘                                                      ││
│      │          │                                                          ││
│      │          │ UpdateValidationMessage()                                  ││
│      │          │                                                           ││
│      └────┬─────┘                                                           ││
│           │                                                                 ││
│           │ UpdateConstruction()                                             ││
│           ▼                                                                 ││
│  ┌─────────────┐                                                            ││
│  │   Updated   │                                                            ││
│  │   State     │                                                            ││
│  └──────┬──────┘                                                            ││
│         │                                                                    ││
│         │ RaiseDataChanged()                                                 ││
│         ▼                                                                    ││
│  ┌─────────────┐                                                            ││
│  │  Notifying  │                                                            ││
│  │   State     │────────────────────────────────────────────────────────────┘│
│  └─────────────┘                                                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. View

### 7.1. ConstructionView.xaml

**Файл:** `src/Views/Construction/ConstructionView.xaml`

**Назначение:** WPF UserControl для визуализации и редактирования конструкции

**Разметка:**

```xml
<UserControl x:Class="SnowMeltingCalculator.Views.Construction.ConstructionView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:construction="clr-namespace:SnowMeltingCalculator.ViewModels.Construction"
             d:DataContext="{d:DesignInstance construction:ConstructionViewModel}"
             mc:Ignorable="d"
             d:DesignHeight="600" d:DesignWidth="900">

    <Grid Margin="10">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" />
            <ColumnDefinition Width="3*" />
        </Grid.ColumnDefinitions>

        <!-- Левая панель: Визуализация "Пирога" -->
        <Border Grid.Column="0" Background="White" BorderBrush="Gray" BorderThickness="1" Margin="0,0,10,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                </Grid.RowDefinitions>

                <TextBlock Grid.Row="0" Text="Визуализация конструкции" 
                           FontWeight="Bold" Margin="5" />

                <!-- Canvas для визуализации слоёв -->
                <Canvas x:Name="ConstructionCanvas" Grid.Row="1" 
                        Background="White" Margin="5">
                    <!-- Слои над трубой (сверху вниз) -->
                    <!-- Труба (фиксированная позиция) -->
                    <!-- Слои под трубой (снизу вверх) -->
                </Canvas>
            </Grid>
        </Border>

        <!-- Правая панель: Ввод данных -->
        <ScrollViewer Grid.Column="1" VerticalScrollBarVisibility="Auto">
            <StackPanel Margin="10">

                <!-- Параметры УГВ -->
                <TextBlock Text="Уровень грунтовых вод (м):" FontWeight="Bold" Margin="0,0,0,5" />
                <TextBox Text="{Binding GroundwaterLevel, UpdateSourceTrigger=PropertyChanged, 
                         StringFormat=F1}" Margin="0,0,0,10" />

                <!-- Флаг нагрузок -->
                <CheckBox Content="Наличие нагрузок на покрытие" 
                          IsChecked="{Binding HasLoads}" Margin="0,0,0,10" />

                <!-- Слои над трубой -->
                <TextBlock Text="Слои над трубой:" FontWeight="Bold" Margin="0,10,0,5" />
                <Button Content="Добавить слой" 
                        Command="{Binding AddLayerAbovePipeCommand}" Margin="0,0,0,5" />
                <DataGrid ItemsSource="{Binding LayersAbovePipe}" 
                          AutoGenerateColumns="False"
                          CanUserAddRows="False"
                          Height="150">
                    <DataGrid.Columns>
                        <DataGridComboBoxColumn Header="Материал" 
                                               ItemsSource="{Binding DataContext.AvailableMaterials, 
                                                        RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                               DisplayMemberPath="Name"
                                               SelectedValueBinding="{Binding Material, Mode=TwoWay, 
                                                                        UpdateSourceTrigger=PropertyChanged}" />
                        <DataGridTextColumn Header="Толщина (мм)" 
                                           Binding="{Binding Thickness, Mode=TwoWay, 
                                                    UpdateSourceTrigger=PropertyChanged, StringFormat=F0}" />
                        <DataGridTextColumn Header="λ (Вт/м·К)" 
                                           Binding="{Binding Lambda, Mode=TwoWay, 
                                                    UpdateSourceTrigger=PropertyChanged, StringFormat=F3}" />
                        <DataGridTemplateColumn Header="Действия">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="Удалить" 
                                            Command="{Binding DataContext.RemoveLayerCommand, 
                                                     RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                            CommandParameter="{Binding}" />
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>

                <!-- Слои под трубой -->
                <TextBlock Text="Слои под трубой:" FontWeight="Bold" Margin="0,10,0,5" />
                <Button Content="Добавить слой" 
                        Command="{Binding AddLayerBelowPipeCommand}" Margin="0,0,0,5" />
                <DataGrid ItemsSource="{Binding LayersBelowPipe}" 
                          AutoGenerateColumns="False"
                          CanUserAddRows="False"
                          Height="150">
                    <!-- Аналогично DataGrid для слоёв над трубой -->
                </DataGrid>

                <!-- Результаты -->
                <TextBlock Text="Результаты расчёта:" FontWeight="Bold" Margin="0,10,0,5" />
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>

                    <TextBlock Grid.Row="0" Grid.Column="0" Text="R1 (над трубой):" Margin="0,0,10,0" />
                    <TextBlock Grid.Row="0" Grid.Column="1" 
                               Text="{Binding R1Total, StringFormat='{}{0:F4} м²·К/Вт'}" />

                    <TextBlock Grid.Row="1" Grid.Column="0" Text="R2 (под трубой):" Margin="0,0,10,0" />
                    <TextBlock Grid.Row="1" Grid.Column="1" 
                               Text="{Binding R2Total, StringFormat='{}{0:F4} м²·К/Вт'}" />

                    <TextBlock Grid.Row="2" Grid.Column="0" Text="λE (вокруг трубы):" Margin="0,0,10,0" />
                    <TextBlock Grid.Row="2" Grid.Column="1" 
                               Text="{Binding LambdaE, StringFormat='{}{0:F2} Вт/м·К'}" />
                </Grid>

                <!-- Валидация -->
                <TextBlock Text="{Binding ValidationMessage}" 
                          Foreground="Red" 
                          TextWrapping="Wrap"
                          Margin="0,10,0,0" />

                <!-- Шаблоны -->
                <TextBlock Text="Шаблоны:" FontWeight="Bold" Margin="0,10,0,5" />
                <StackPanel Orientation="Horizontal">
                    <Button Content="Типовая парковка" 
                            Command="{Binding ApplyTemplateCommand}"
                            CommandParameter="{Binding Templates[0]}" Margin="0,0,5,0" />
                    <Button Content="Пешеходная дорожка" 
                            Command="{Binding ApplyTemplateCommand}"
                            CommandParameter="{Binding Templates[1]}" Margin="0,0,5,0" />
                    <Button Content="Въезд в гараж" 
                            Command="{Binding ApplyTemplateCommand}"
                            CommandParameter="{Binding Templates[2]}" />
                </StackPanel>

            </StackPanel>
        </ScrollViewer>
    </Grid>

</UserControl>
```

---

### 7.2. Визуализация "Пирога"

**Алгоритм отрисовки:**

1. **Определение масштаба:**
   - Общая толщина = Σ(слои над трубой) + Σ(слои под трубой)
   - Масштаб = Высота Canvas / Общая толщина

2. **Отрисовка слоёв над трубой (сверху вниз):**
   - Для каждого слоя в `LayersAbovePipe`:
     - Высота прямоугольника = Толщина × Масштаб
     - Цвет = Категория материала → Цвет
     - Текст = Название материала + Толщина

3. **Отрисовка трубы:**
   - Фиксированная позиция между слоями над трубой и под трубой
   - Размер = Наружный диаметр трубы (из ThermalParameters)

4. **Отрисовка слоёв под трубой (снизу вверх):**
   - Для каждого слоя в `LayersBelowPipe`:
     - Высота прямоугольника = Толщина × Масштаб
     - Цвет = Категория материала → Цвет
     - Текст = Название материала + Толщина

**Цвета по категориям:**

| Категория | Цвет | HEX |
|-----------|------|-----|
| бетон | Серый | #808080 |
| грунт | Коричневый | #8B4513 |
| изоляция | Жёлтый | #FFD700 |
| покрытие | Чёрный | #000000 |
| подстилающий | Светло-серый | #A0A0A0 |
| стяжка | Светло-серый | #C0C0C0 |

---

## 8. Потоки данных

### 8.1. Диаграмма потока данных

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Потоки данных                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────┐                                                        │
│  │ materials_db.json│                                                        │
│  └────────┬────────┘                                                        │
│           │                                                                  │
│           │ LoadMaterialsAsync()                                             │
│           ▼                                                                  │
│  ┌─────────────────┐                                                        │
│  │MaterialRepository│                                                        │
│  └────────┬────────┘                                                        │
│           │                                                                  │
│           │ GetMaterials()                                                   │
│           ▼                                                                  │
│  ┌─────────────────┐                                                        │
│  │ConstructionViewModel│                                                    │
│  └────────┬────────┘                                                        │
│           │                                                                  │
│           │ AddLayerAbovePipe() / AddLayerBelowPipe()                       │
│           ▼                                                                  │
│  ┌─────────────────┐                                                        │
│  │  Construction   │                                                        │
│  │  (IConstructionData)│                                                     │
│  └────────┬────────┘                                                        │
│           │                                                                  │
│           │ DataChanged event                                                │
│           ▼                                                                  │
│  ┌─────────────────┐                                                        │
│  │ ThermalViewModel │                                                        │
│  └────────┬────────┘                                                        │
│           │                                                                  │
│           │ Update ThermalParameters                                         │
│           ▼                                                                  │
│  ┌─────────────────┐                                                        │
│  │ThermalCalculator│                                                        │
│  └─────────────────┘                                                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 8.2. Последовательность действий при добавлении слоя

```
User → ConstructionView → ConstructionViewModel → Construction → IConstructionData
  │                           │                        │                │
  │ 1. Click "Добавить слой"  │                        │                │
  ├──────────────────────────►│                        │                │
  │                           │ 2. AddLayerAbovePipe() │                │
  │                           ├───────────────────────►│                │
  │                           │                        │ 3. Create Layer│
  │                           │                        ├───────────────►│
  │                           │                        │                │
  │                           │                        │ 4. Calculate R │
  │                           │                        │    R1Total     │
  │                           │                        │    R2Total     │
  │                           │                        │    LambdaE     │
  │                           │                        │                │
  │                           │                        │ 5. Validate()  │
  │                           │                        ├───────────────►│
  │                           │                        │                │
  │                           │                        │ 6. RaiseDataChanged()
  │                           │                        ├───────────────►│
  │                           │                        │                │
  │                           │                        │ 7. Event: DataChanged
  │                           │                        │◄───────────────┤
  │                           │                        │                │
  │                           │ 8. OnDataChanged()     │                │
  │                           │◄───────────────────────┤                │
  │                           │                        │                │
  │                           │ 9. Update UI           │                │
  │                           │    (R1Total, R2Total, LambdaE)           │
  │                           │                        │                │
  │ 10. UI Updated            │                        │                │
  │◄──────────────────────────┤                        │                │
  │                           │                        │                │
  │                           │                        │                │
  │                           │                        │ 11. ThermalViewModel receives event
  │                           │                        │                │
  │                           │                        │ 12. Update ThermalParameters
  │                           │                        │                │
  │                           │                        │ 13. Recalculate thermal
  │                           │                        │                │
```

### 8.3. Последовательность действий при изменении УГВ

```
User → ConstructionView → ConstructionViewModel → Construction
  │                           │                        │
  │ 1. Change GroundwaterLevel│                        │
  ├──────────────────────────►│                        │
  │                           │ 2. OnGroundwaterLevelChanged()
  │                           ├───────────────────────►│
  │                           │                        │ 3. UpdateLambdaForGroundwater()
  │                           │                        ├──────────────────────────────┐
  │                           │                        │                              │
  │                           │                        │ For each layer in LayersBelowPipe:
  │                           │                        │   if (!IsLambdaOverridden)
  │                           │                        │     Lambda = GetLambdaForLayer()
  │                           │                        │                              │
  │                           │                        │◄─────────────────────────────┤
  │                           │                        │
  │                           │                        │ 4. Recalculate R2Total
  │                           │                        │
  │                           │                        │ 5. RaiseDataChanged()
  │                           │                        ├──────────────────────────────►
  │                           │                        │                              │
  │                           │ 6. OnDataChanged()     │                              │
  │                           │◄───────────────────────┤                              │
  │                           │                        │                              │
  │ 7. UI Updated             │                        │                              │
  │◄──────────────────────────┤                        │                              │
```

---

## 9. Интеграция с существующим кодом

### 9.1. Обновление `IConstructionData`

**Существующий интерфейс** (`src/Models/Thermal/IConstructionData.cs`):

```csharp
namespace SnowMeltingCalculator.Models.Thermal
{
    public interface IConstructionData
    {
        double R1Total { get; }
        double R2Total { get; }
        double LambdaE { get; }
        bool IsValid { get; }
        event EventHandler<ConstructionDataChangedEventArgs>? DataChanged;
    }
}
```

**Новая реализация** (`src/Models/Construction/Construction.cs`):

```csharp
namespace SnowMeltingCalculator.Models.Construction
{
    public class Construction : IConstructionData
    {
        // Реализация IConstructionData
        public double R1Total => LayersAbovePipe.Sum(l => l.ThermalResistance);
        public double R2Total => LayersBelowPipe.Sum(l => l.ThermalResistance);
        public double LambdaE => MaterialAroundPipe?.LambdaA ?? 1.6;
        public bool IsValid => ValidateConstruction();

        public event EventHandler<ConstructionDataChangedEventArgs>? DataChanged;

        // Дополнительные свойства и методы
        public ObservableCollection<Layer> LayersAbovePipe { get; } = new();
        public ObservableCollection<Layer> LayersBelowPipe { get; } = new();
        public double GroundwaterLevel { get; set; } = 2.0;
        public bool HasLoads { get; set; } = false;
        public Material? MaterialAroundPipe { get; set; }

        // Методы управления слоями
        public void AddLayerAbovePipe(Material material, double thickness) { ... }
        public void AddLayerBelowPipe(Material material, double thickness) { ... }
        public void RemoveLayer(Layer layer) { ... }
        public void UpdateLambdaForGroundwater() { ... }

        // Вызов события
        public void RaiseDataChanged(string propertyName, object? oldValue, object? newValue, bool isValid = true)
        {
            DataChanged?.Invoke(this, new ConstructionDataChangedEventArgs
            {
                ChangedProperty = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                IsValid = isValid
            });
        }
    }
}
```

### 9.2. Обновление DI

**Файл:** `src/Configuration/ServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddConstructionModule(this IServiceCollection services)
{
    // Repositories
    services.AddSingleton<IMaterialRepository, MaterialRepository>();
    services.AddSingleton<IConstructionRepository, ConstructionRepository>();

    // Services
    services.AddSingleton<IConstructionService, ConstructionService>();
    services.AddSingleton<ConstructionValidator>();

    // ViewModels
    services.AddSingleton<ConstructionViewModel>();

    // Data (заменяет заглушку ConstructionData)
    services.AddSingleton<IConstructionData, Construction>();

    return services;
}

public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    return services
        .AddClimateModule()
        .AddConstructionModule()  // НОВЫЙ МОДУЛЬ
        .AddThermalModule();
}
```

### 9.3. Обновление ThermalViewModel

**Существующий код** (`src/ViewModels/Thermal/ThermalViewModel.cs`):

```csharp
// Подписка на изменения данных конструкции
if (_constructionData is ConstructionData constructionDataImpl)
{
    constructionDataImpl.DataChanged += OnConstructionDataChanged;
}
```

**Новый код** (полиморфизм, без изменений):

```csharp
// Подписка на изменения данных конструкции
// Работает как с ConstructionData (заглушка), так и с Construction (реализация)
if (_constructionData is Construction constructionImpl)
{
    constructionImpl.DataChanged += OnConstructionDataChanged;
}
```

---

## 10. Нефункциональные требования

### 10.1. Производительность

| Параметр | Требование | Обоснование |
|----------|------------|-------------|
| Время отклика при добавлении/удалении слоя | < 100 мс | UX |
| Время пересчёта R1/R2 | < 50 мс | Расчёт |
| Время отрисовки визуализации | < 100 мс | UX |
| Максимальное количество слоёв | 20 над трубой + 20 под трубой | Ограничение |

### 10.2. Надёжность

| Требование | Реализация |
|------------|------------|
| Автосохранение | При каждом изменении — сохранение в SQLite |
| Восстановление | При запуске — загрузка последней конструкции |
| Валидация | Все входные данные валидируются |

### 10.3. Тестируемость

| Компонент | Тест |
|-----------|------|
| `MaterialRepository` | Unit-тесты загрузки JSON |
| `ConstructionService` | Unit-тесты расчёта R1/R2/LambdaE |
| `ConstructionValidator` | Unit-тесты валидации |
| `ConstructionViewModel` | Integration-тесты с mock-объектами |

### 10.4. Расширяемость

| Точка расширения | Реализация |
|------------------|------------|
| Новые материалы | Добавление в `materials_db.json` |
| Новые шаблоны | Добавление в `ConstructionRepository.GetTemplates()` |
| Новые правила валидации | Расширение `ConstructionValidator` |

---

## 11. Открытые вопросы

### 11.1. Вопросы для уточнения

| # | Вопрос | Статус | Решение |
|---|--------|--------|---------|
| 1 | Нужна ли визуализация трубы с учётом диаметра? | Открыт | Труба отображается схематично, диаметр из ThermalParameters |
| 2 | Нужен ли экспорт конструкции в PDF/Excel? | Открыт | В следующей версии |
| 3 | Нужна ли история изменений конструкции? | Открыт | В следующей версии |

### 11.2. Риски

| Риск | Вероятность | Влияние | Митигация |
|------|-------------|---------|-----------|
| Несоответствие формул ТЗ | Низкая | Высокое | Тестирование по формулам из `docs/Formulas_Snegotayanie.md` |
| Проблемы производительности при большом количестве слоёв | Низкая | Среднее | Ограничение 20+20 слоёв |
| Несовместимость с существующим ThermalViewModel | Низкая | Высокое | Полиморфизм через `IConstructionData` |

---

## 12. План реализации

### 12.1. Этапы разработки

| Этап | Задачи | Оценка |
|------|--------|--------|
| 1. Модели | Material, Layer, LayerPosition, Construction, ConstructionTemplate, ValidationResult | 2 дня |
| 2. Репозитории | MaterialRepository, ConstructionRepository | 1 день |
| 3. Сервисы | ConstructionService, ConstructionValidator | 2 дня |
| 4. ViewModel | ConstructionViewModel | 2 дня |
| 5. View | ConstructionView.xaml, визуализация "пирога" | 3 дня |
| 6. Интеграция | Обновление DI, ThermalViewModel | 1 день |
| 7. Тестирование | Unit-тесты, Integration-тесты | 2 дня |
| **Итого** | | **13 дней** |

### 12.2. Приоритеты

| Приоритет | Компонент | Обоснование |
|-----------|-----------|-------------|
| P0 | Construction (модель) | Базовый компонент |
| P0 | ConstructionService | Расчёт R1/R2/LambdaE |
| P1 | ConstructionViewModel | MVVM |
| P1 | ConstructionView | UX |
| P2 | ConstructionRepository | Сохранение/загрузка |
| P2 | Шаблоны | Удобство |

---

## 13. Приложение: Примеры кода

### 13.1. Пример расчёта R1/R2

```csharp
// Расчёт термического сопротивления слоя
double CalculateThermalResistance(Layer layer)
{
    // R = d / λ / 1000    [м²·К/Вт]
    return layer.Thickness / layer.Lambda / 1000.0;
}

// Расчёт R1Total (над трубой)
double CalculateR1Total(Construction construction)
{
    return construction.LayersAbovePipe.Sum(l => CalculateThermalResistance(l));
}

// Расчёт R2Total (под трубой)
double CalculateR2Total(Construction construction)
{
    return construction.LayersBelowPipe.Sum(l => CalculateThermalResistance(l));
}

// Определение LambdaE
double CalculateLambdaE(Construction construction)
{
    // LambdaE = λ материала вокруг трубы (первый слой над трубой)
    if (construction.LayersAbovePipe.Count > 0)
    {
        return construction.LayersAbovePipe[0].Lambda;
    }
    
    // Значение по умолчанию (бетон)
    return 1.6;
}
```

### 13.2. Пример выбора λА/λБ

```csharp
double GetLambdaForLayer(Material material, LayerPosition position, double groundwaterLevel)
{
    if (position == LayerPosition.AbovePipe)
    {
        // Слои над трубой всегда используют λА
        return material.LambdaA;
    }
    else
    {
        // Слои под трубой: λБ при УГВ < 1м, λА при УГВ >= 1м
        return groundwaterLevel < 1.0 ? material.LambdaB : material.LambdaA;
    }
}
```

### 13.3. Пример валидации

```csharp
ValidationResult Validate(Construction construction, double supplyTemperature, double airTemperature)
{
    var errors = new List<string>();

    // Проверка минимальной стяжки над трубой
    var minThickness = construction.HasLoads ? 50.0 : 40.0;
    var totalAbove = construction.LayersAbovePipe.Sum(l => l.Thickness);
    if (totalAbove < minThickness)
    {
        errors.Add($"Минимальная стяжка над трубой: {minThickness} мм (текущая: {totalAbove} мм)");
    }

    // Проверка толщины слоёв
    foreach (var layer in construction.LayersAbovePipe.Concat(construction.LayersBelowPipe))
    {
        if (layer.Thickness < 10 || layer.Thickness > 1000)
        {
            errors.Add($"Толщина слоя '{layer.Material.Name}' должна быть от 10 до 1000 мм");
        }
    }

    // Проверка материалов
    foreach (var layer in construction.LayersAbovePipe)
    {
        // Бетон: макс. температура подачи 50°C
        if (layer.Material.Category == "бетон" && supplyTemperature > 50)
        {
            errors.Add($"Бетон: максимальная температура подачи 50°C (текущая: {supplyTemperature}°C)");
        }

        // Асфальт: не применять при t ≤ -15°C
        if (layer.Material.Name.Contains("Асфальт") && airTemperature <= -15)
        {
            errors.Add($"Асфальт не применяется при температуре наружного воздуха ≤ -15°C");
        }
    }

    // Проверка УГВ
    if (construction.GroundwaterLevel < 0 || construction.GroundwaterLevel > 10)
    {
        errors.Add("Уровень грунтовых вод должен быть от 0 до 10 м");
    }

    return new ValidationResult
    {
        IsValid = errors.Count == 0,
        Errors = errors
    };
}
```

---

**Конец документа**