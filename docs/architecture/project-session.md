# ProjectSession — aggregate root и срезы

`src/Services/Project/ProjectSession.cs` (`ProjectSession : IProjectSession,
IMarkDirtyService`). Держит identity проекта, dirty-state и load guard;
канонические данные разделены на четыре явных среза.

```mermaid
flowchart TB
    PS["ProjectSession<br/>ProjectNumber · ProjectObject · CurrentFilePath<br/>IsDirty · IsLoadProjectInProgress<br/>BeginProjectRestore() → lease (load guard)"]
    C["IProjectSessionClimateState<br/>ApplyCitySelection · ApplyIndividualEdit<br/>ApplyProjectSnapshot · ResetToCityData"]
    K["IProjectSessionConstructionState<br/>Apply · ApplySnapshot · ResetToDefaults"]
    T["IProjectSessionThermalState<br/>ApplyInputs · ApplyInputEdit · ApplyNeedsRecalculation<br/>Begin/Complete/FailCalculation · Restore<br/>InvalidateFromClimate · InvalidateFromConstruction"]
    H["IProjectSessionHydraulicsState<br/>ApplyGlobalInputs · ReplaceCollectors<br/>Begin/Complete/FailCalculation · ApplySnapshot"]
    PS --> C
    PS --> K
    PS --> T
    PS --> H
```

## Санкционированные writers (инвариант «один writable owner»)

Списки перенесены дословно из writer-inventory фазы 10 (ADR-003); их
проверяют тесты R2/R3/R5.

| Срез / хранилище | Санкционированные writers (файлы) |
|---|---|
| ClimateState | `ProjectSessionClimateState.cs`, `ClimateViewModel.cs`, `ProjectLoadOrchestrator.cs`, `MainViewModel.cs`, `ResultsViewModel.cs`, `ProjectSession.cs` |
| ConstructionState | `ProjectSessionConstructionState.cs`, `ConstructionViewModel.cs`, `ProjectLoadOrchestrator.cs`, `MainViewModel.cs`, `ConstructionDefaultStateInitializer.cs` |
| ThermalState | `ProjectSessionThermalState.cs`, `ThermalStateCoordinator.cs` (через `_state.`), `CalculationStateService.cs`, `ProjectLoadOrchestrator.cs`, `ThermalViewModel.cs` |
| HydraulicsState | `ProjectSessionHydraulicsState.cs`, `HydraulicsStateCoordinator.cs` (через `_state.`), `CircuitsViewModel.cs`, `ProjectLoadOrchestrator.cs`, `CalculationStateService.cs` |
| Dirty / identity (`MarkDirty`/`MarkClean`) | Срезы сессии, координаторы, `ProjectSession.cs`, `ResultsViewModel.cs` (identity adapter), `MainViewModel.cs` |
| CalculationContext (compat-проекция, DEC-001 = A) | `ProjectSessionClimateState.cs`, `ProjectSessionConstructionState.cs`, `ThermalStateCoordinator.cs`, `HydraulicsStateCoordinator.cs`, `MainViewModel.cs`, `ProjectLoadOrchestrator.cs` |

## Правила

- ViewModel может мутировать **только свой** срез: `ClimateViewModel` →
  ClimateState, `ConstructionViewModel` → ConstructionState, и т.д.
  Чужие срезы не трогаются никогда (тест R3).
- Invalidate-поток: изменение климата/конструкции инвалидирует thermal
  (`InvalidateFromClimate/Construction`), точное количество перерасчётов
  зафиксировано characterization-тестами.
- Load guard: на время восстановления проекта `BeginProjectRestore()`
  выдаёт lease, подавляющий грязевые/реактивные эффекты; снятие —
  детерминированное.
