# Обзор слоёв и зависимостей

Одно WPF-приложение (`SnowMeltingCalculator`, .NET 8) и тестовый проект
(`SnowMeltingCalculator.Tests`). Зависимости направлены только вниз;
вверх — только события (`PropertyChanged`) и производные проекции.

```mermaid
flowchart TB
    subgraph UI["Presentation (WPF)"]
        V["Views / MainWindow"]
        VM["ViewModels — адаптеры состояния<br/>Main · Climate · Construction · Thermal · Circuits · Results"]
    end
    subgraph CORE["Каноническое состояние"]
        PS["ProjectSession — aggregate root<br/>identity · dirty · restore guard"]
        C["ClimateState"]
        K["ConstructionState"]
        T["ThermalState"]
        H["HydraulicsState"]
        CC["CalculationContext —<br/>compat-проекция (DEC-001 = A)"]
    end
    subgraph SVC["Application Services"]
        CO["Координаторы<br/>ThermalStateCoordinator · HydraulicsStateCoordinator · CalculationStateService"]
        ORCH["ProjectLoadOrchestrator · ProjectSaveService"]
        VAL["Валидаторы и селекторы<br/>Climate · Construction · Circuits · Collector"]
        RES["Results-билдеры<br/>ResultsPdfDataBuilder · HydraulicSummaryBuilder"]
    end
    DB[("Хранилище<br/>.smc v1.1 · репозитории")]

    V --> VM
    VM -->|"пишет только свой срез<br/>(Apply-API)"| PS
    PS --> C
    PS --> K
    PS --> T
    PS --> H
    VM --> CO
    CO --> PS
    CO -->|"4 санкционированных<br/>projection-writers"| CC
    ORCH --> PS
    ORCH --> DB
    VAL --> DB
    VM --> RES
    RES --> DB
```

## Правила чтения диаграммы

- **Вниз можно, вверх нельзя.** Services не зависят от ViewModels
  (инвариант; единственное исключение — ADR-002: два Results-билдера
  читают read-model записи `ViewModels.Results`).
- **ViewModel — адаптеры.** Каждый ViewModel мутирует только свой срез
  через Apply-API сессии и не хранит каноническое состояние.
- **Results — производная.** `ResultsViewModel` ничего не владеет:
  identity-редактирование идёт через сессию (`MarkDirty` — identity
  adapter, санкционирован), данные пересобираются из срезов.
- **CalculationContext — не владелец.** Это совместимостная проекция для
  расчётных сервисов; пишут её ровно четыре санкционированных projection
  writer + `MainViewModel`/`ProjectLoadOrchestrator` (DEC-001 = A).
- Контроль правил: `ArchitectureRulesTests` R1–R6 (`dotnet test`).
