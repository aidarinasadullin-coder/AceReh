# Архитектурный аудит — Калькулятор снеготаяния РЕХАУ

**Дата:** 29.07.2026
**Объект:** `D:\IA\ace\src` (173 .cs-файла, 265 классов/интерфейсов, тесты: 108 файлов)
**Метод:** автоматический анализ (метрики LOC, граф зависимостей по namespace, DI-регистрации) + ручной разбор ключевых файлов

---

## Краткий вердикт

Ваша интуиция верна: архитектура **нечистая**. Формально заявлен MVVM со слоями Views → ViewModels → Services → Repositories, но фактически слой **Services зависит от ViewModels**, а ViewModels используются как общее хранилище состояния. Обнаружено **14 циклических зависимостей** между модулями. Хорошая новость: проблема сфокусирована — один корневой паттерн («ViewModel как state-store») порождает большинство нарушений, и его можно убрать поэтапно, не останавливая разработку.

---

## P0 — Критично (блокирует развитие)

### 1. Циклические зависимости Services ↔ ViewModels (14 циклов)

Сервисы получают ViewModels в конструктор — инверсия слоёв. Файлы-нарушители:

| Файл | Нарушение |
|---|---|
| `Services/Project/ProjectLoadOrchestrator.cs` | Инжектит `ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`, `CircuitsViewModel` (конкретные классы!) и пишет данные прямо в них |
| `Services/Results/HydraulicSummaryBuilder.cs` | Читает `ViewModels.Hydraulics` |
| `Services/Results/ResultsPdfDataBuilder.cs` | Читает `ViewModels.Construction/Hydraulics/Results` |
| `Services/Hydraulics/CircuitsValidator.cs` | Зависит от `ViewModels.Hydraulics` |
| `Services/Hydraulics/CollectorTypeSelector.cs` | Зависит от `ViewModels.Hydraulics` |

Примеры циклов (namespace-уровень, найдены алгоритмом):

```
Services.Results → ViewModels.Results → Services.Project → ViewModels.Climate → Services.Results
ViewModels.Hydraulics → Services.Hydraulics → ViewModels.Hydraulics
Services.Hydraulics → ViewModels.Hydraulics → Services.Results → ViewModels.Results → Services.Reports → Services.Hydraulics
```

В `ProjectLoadOrchestrator` есть честный комментарий: *«Вынесен из ResultsViewModel (архитектурный долг, этап C1)»* — долг осознан, но не закрыт.

### 2. ResultsViewModel — god class

- **1946 строк**, **16 зависимостей** в конструкторе (9 интерфейсов + 4 других ViewModel как конкретные классы + 3 конкретных билдера/оркестратора), **49 методов**.
- Файл дополнительно содержит 3 класса (`CollectorInfo`, `CollectorSpecification`, `CollectorEquipmentItem`).
- Смешаны обязанности: загрузка проекта, сводка результатов, экспорт PDF, навигация, маркировка «грязного» состояния.

### 3. Репозиторий зависит от сервиса

`Repositories/Construction/ConstructionRepository.cs` → `using Services.Construction` — прямой цикл `Repositories.Construction ↔ Services.Construction` и перевёрнутая стрелка слоя данных.

### 4. ViewModels ходят в репозитории в обход сервисов

`ConstructionViewModel`, `MaterialEditorViewModel`, `TemplateEditorViewModel`, `CollectorViewModel`, `ResultsViewModel` напрямую используют `Repositories.*` — слой сервисов обойдён, правила валидации дублируются.

---

## P1 — Серьёзно (архитектурный долг)

### 5. Состояние размазано по синглтонам

Все сервисы и ViewModels зарегистрированы как **Singleton**. Состояние расчёта живёт одновременно в:
- singleton ViewModels (как поля VM),
- singleton моделях `IClimateData`, `Construction` (`IConstructionData`),
- `CalculationStateService` (171 строка, 18 публичных членов) — дублирующий «контроллер состояния».

Нет единого доменного объекта «Проект/Сессия расчёта» — поэтому VM тянут друг друга, а сервисы тянут VM.

### 6. AppSettings — статический синглтон

`public static AppSettings Instance => _instance ??= Load();` — скрытая глобальная зависимость, недоступная для DI и тестов.

### 7. Логика в code-behind

| Файл | Строк |
|---|---|
| `Controls/Climate/CityAutoCompleteBox.xaml.cs` | 439 |
| `Views/Shared/ConstructionVisualizationView.xaml.cs` | 318 |
| `Views/Construction/ConstructionView.xaml.cs` | 118 |

`Views.Shared` также напрямую использует `Services.Visualization` — View в обход ViewModel.

### 8. GlycolDataService — мультитул

1108 строк: сервис + JSON-парсинг + интерполяция + **8 DTO-классов в одном файле** (`GlycolRawContainer`, `InterpolationTable` и др.).

---

## P2 — Косметика / документация

9. **README врёт**: заявлена структура `Core/UI/Data`, фактическая — MVVM-домены. Нужно синхронизировать.
10. `Services.Results → Services.Visualization` — кросс-доменная связь без правил.
11. Зависимости от конкретных классов вместо интерфейсов (`ProjectLoadOrchestrator`, `ResultsPdfDataBuilder`, `HydraulicSummaryBuilder` в конструкторе ResultsViewModel).

---

## Целевая архитектура (гибрид: MVVM + чёткие границы)

Парадигму не меняем — WPF/MVVM остаётся. Вводим **одно правило направления зависимостей** и **единый доменный объект состояния**.

```
Views ──► ViewModels ──► Application Services ──► Domain ──◄── Data (Repositories)
                          (только модели!)        (Core + Models + ProjectSession)
```

Ключевые решения:

1. **`ProjectSession` (Domain)** — единый объект состояния расчёта: климат, пирог, тепловой и гидравлический результаты. Заменяет «VM-как-хранилище», `IClimateData`-синглтоны и половину `CalculationStateService`.
2. **Application Services работают только с Domain** — ни один сервис не знает слова «ViewModel». `ProjectLoadOrchestrator` заполняет `ProjectSession`, а не четыре VM.
3. **ViewModels читают `ProjectSession` и вызывают сервисы** — запрет на зависимости VM→VM; связь шагов мастера — через session, а не через инъекцию друг друга.
4. **Repositories зависят только от Models** — разрыв цикла `ConstructionRepository ↔ ConstructionService`.
5. **Интерфейсы для билдеров и оркестратора**, ResultsViewModel делится на секции (summary / export / navigation).

## Roadmap рефакторинга

| Этап | Действие | Разрывает |
|---|---|---|
| **1** | Ввести `ProjectSession` в Domain; перевести `ProjectLoadOrchestrator` на заполнение session | Главный цикл Services→ViewModels |
| **2** | Убрать VM→VM зависимости из `ResultsViewModel` (чтение через session/services) | 8 циклов через Services.Results |
| **3** | `ConstructionRepository` → только Models; VM убирают прямой доступ к репозиториям | Цикл Repos↔Services, обход слоя |
| **4** | Разбить `ResultsViewModel` (summary / export / navigation); интерфейсы для билдеров | God class |
| **5** | Code-behind → behaviors/controls; `AppSettings` → сервис в DI; разнести `GlycolDataService` | Тестопригодность |

Этапы 1–3 можно делать параллельно с текущей доработкой гидравлики контуров — они затрагивают связность, а не расчётную математику.

---

*Метрики: `audit_metrics.json` (сгенерирован `audit_metrics.py`).*
