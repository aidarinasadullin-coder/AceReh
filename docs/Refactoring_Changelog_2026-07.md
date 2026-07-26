# Changelog: рефакторинг архитектурного долга (Stage A–D)

Дата: 2026-07-26
Ветка: master, коммиты `0f07b04` … `dd6f4e8` (8 коммитов)
Проверка: на каждом шаге полный прогон тестов — зелёный; финал — Release-сборка
без ошибок и живой запуск приложения с реальным проектом (`Тест/перм.smc`).

---

## Краткая сводка

| Этап | Коммит | Суть |
|---|---|---|
| A+B | `0f07b04` | MainViewModel/MenuItem из code-behind; удалён двойной service locator |
| C1 | `47f3f6f` | `ProjectLoadOrchestrator` — оркестрация загрузки проекта |
| C2 | `04967ed` | `ResultsPdfDataBuilder` — построение PDF-модели |
| C3 | `f12eeab` | `HydraulicSummaryBuilder` — read-model итогов гидравлики |
| C4 | `3e9d389` | Project info — единый источник истины в `ProjectStateService` |
| D1 | `891233a` | Мёртвая зависимость `CalculationContext` из ResultsViewModel |
| D2+D3 | `dd6f4e8` | `CalculationContext` сужен до живого контракта; `ValidationPipeline` удалён |

Ключевые цифры:

- `ResultsViewModel`: 2437 → 1875 строк (−23 %)
- `CalculationContext`: 469 → ~270 строк (−42 %)
- Тесты: 1426 зелёных, 0 падающих, 1 пропущен (F5 smoke, self-skip по дизайну);
  счётчик уменьшился за счёт удалённых тестов мёртвого API, не за счёт регрессий

---

## Stage A+B — Shell и service locator (коммит `0f07b04`)

- `MainViewModel` вынесен из `MainWindow.xaml.cs` в `src/ViewModels/Shell/MainViewModel.cs`
  (namespace `SnowMeltingCalculator.ViewModels.Shell`), зарегистрирован в DI
  и разрешается из MainWindow.
- `MenuItem` — в `src/Models/Navigation/MenuItem.cs`
  (namespace `SnowMeltingCalculator.Models.Navigation`).
- Удалён `ViewModelLocator` (двойной service locator): все ViewModel — только через DI.
- Сопутствующее (phase-1 hardening, тот же коммит):
  - DI-лайфтаймы выровнены (`AddScoped` → `AddSingleton`), dispose в `App.OnExit`;
  - UI-нейтральные диалоги: `src/Services/Navigation/DialogContracts.cs`
    (enums `DialogResult`/`DialogButtons`/`DialogIcon`), `IDialogService` расширен
    (`ShowSaveFileDialog(defaultFileName, filter, title, defaultExt)`, `ShowPrintDialog()`);
  - `CircuitsValidator` переведён на `IDialogService`;
  - `CalculationContext.UpdateThermalInputs` сбрасывает `HydraulicsResults`
    (закрыта асимметрия инвалидации из аудита);
  - детерминированная финализация загрузки проекта: один `RefreshAll` в конце,
    `HasUserModifications = false` — убрана ложная «верификация города» при открытии .smc.

## Stage C — разбиение ResultsViewModel

### C1 — ProjectLoadOrchestrator (`src/Services/Project/ProjectLoadOrchestrator.cs`)

- Singleton в DI (`AddResultsModule`).
- `RestoreModulesFromProjectAsync(ProjectData)` — восстановление климата, пользовательских
  материалов/шаблонов, слоёв конструкции (включая миграцию порядка слоёв до/после v1.1),
  тепловых входов, трубы, коллекторов и гидравлических входов; детерминированная
  финализация: ensure-теплорезультат → восстановление результатов контуров →
  сброс `HasUserModifications`.
- `ResetModules()` — сброс CalculationContext и четырёх модульных VM перед загрузкой.
- `ResultsViewModel.LoadProjectDataAsync` (~190 → ~35 строк) оставляет за собой только:
  режим отображения, project info, единый `RefreshAll`, событие `ProjectChanged`, `MarkClean`.
- Канонический источник `SetPipeSpacing` обновлён: `ProjectLoadOrchestrator.RestoreModules`
  (guard в `CalculationStateService` + соответствующий тест).

### C2 — ResultsPdfDataBuilder (`src/Services/Results/ResultsPdfDataBuilder.cs`)

- `Build(ResultsViewModel)` — вся сборка `ResultsPdfData` (скаляры через публичную
  поверхность VM; коллекторы/контуры/спецификации; изображение схемы конструкции).
- Из VM удалён ~160-строчный `BuildResultsPdfData` и мёртвая зависимость
  `IConstructionVisualizationImageService` (нужна теперь только билдеру).

### C3 — HydraulicSummaryBuilder (`src/Services/Results/HydraulicSummaryBuilder.cs`)

- Stateless-маппер: `BuildSummaryCards`, `BuildSpecifications(isOperatingMode)`,
  `BuildEquipmentItems` (группировка по `(ValveType, CircuitCount)` с порядком первого появления).
- VM оставляет guard `IsDataReady` и наполнение своих ObservableCollection.

### C4 — project info: единый источник истины

- `ProjectNumber`/`ProjectObject` в VM — pass-through к `IProjectStateService`
  (уведомление UI + dirty с прежними guard-ами `_isResetting`/`IsLoadProjectInProgress`).
- Удалены: зеркало `_currentFilePath` (везде `ProjectStateService.CurrentFilePath`),
  метод `LoadProjectInfo`, обработчик `OnProjectPropertyChanged`.

## Stage D — сужение CalculationContext

### D1 — мёртвая зависимость

- Из `ResultsViewModel` удалены поле/ctor-параметр `CalculationContext` — после C1–C4
  VM его не использует.

### D2 — живой контракт (`src/Core/CalculationContext.cs`)

Удалено (production не использовал никогда):

- машина состояний `CalculationState`/`State`/`ErrorMessage` (писалась, никем не читалась);
- pass-through геттеры `SelectedCity`, `WindSpeed`, `SnowfallIntensity`,
  `R1Total`/`R2Total`/`LambdaE`, `PowerTotal`, `DeltaT`;
- перегрузка `UpdateHydraulics(HydraulicInputData)` + свойство `Hydraulics`;
- `IsHydraulicsValid`, `Validate()`, `GetValidationErrors()`, `IsReadyFor*`.

Оставлен контракт:

- Данные: `Climate`, `Construction`, `ThermalResult`, `ThermalInputs`, `HydraulicsResults`.
- Геттеры: `AirTemperature`, `SupplyTemperature`, `ReturnTemperature`,
  `PowerUp`, `PowerDown`, `IsThermalValid`.
- Методы: `UpdateClimate`, `UpdateConstruction`, `UpdateThermal`, `UpdateThermalInputs`,
  `UpdateHydraulics(List<CollectorSummary>?)`, `Reset` — с правилом инвалидации:
  смена входов сбрасывает downstream-результаты.
- Событие `ContextChanged` (PropertyName/OldValue/NewValue/Source).

Роли: публикаторы — Climate/Construction/Thermal/Circuits ViewModel;
потребитель — CircuitsViewModel; сброс — MainViewModel, ProjectLoadOrchestrator.

### D3 — удаление ValidationPipeline

- `ValidationPipeline`/`IValidationPipeline` удалены вместе с DI-регистрацией:
  код был зарегистрирован, но никуда не внедрялся (мёртвая «вторая валидация»).
- Валидация остаётся единым путём: модульные валидаторы (`ClimateValidator`,
  `ConstructionValidator`, `ThermalValidator`, `HydraulicValidator` и т.д.).

---

## Карта новых/изменённых компонентов

```
src/
├── Core/
│   └── CalculationContext.cs                 # сужен до живого контракта (D2)
├── Configuration/
│   └── ServiceCollectionExtensions.cs        # регистрации новых сервисов; без ValidationPipeline
├── Services/
│   ├── Navigation/
│   │   ├── DialogContracts.cs                # UI-нейтральные enums диалогов (A+B)
│   │   └── CalculationStateService.cs        # guard SetPipeSpacing: новый канонический источник
│   ├── Project/
│   │   └── ProjectLoadOrchestrator.cs        # C1
│   └── Results/
│       ├── ResultsPdfDataBuilder.cs          # C2
│       └── HydraulicSummaryBuilder.cs        # C3
├── ViewModels/
│   ├── Shell/MainViewModel.cs                # A (из code-behind)
│   └── Results/ResultsViewModel.cs           # 2437 → 1875 строк
└── Models/
    └── Navigation/MenuItem.cs                # A
```

## Обратная совместимость

- Поведение приложения сохранено: все рефакторинги проверены прогоном 1400+ тестов
  на каждом шаге; файлы проектов (.smc) читаются/пишутся без изменений формата
  (round-trip тесты + живой запуск с реальным файлом).
- Публичный API `CalculationContext` намеренно сужен — при переносе изменений
  в другую ветку обращения к удалённым членам (`State`, `ErrorMessage`,
  `IsHydraulicsValid`, `Validate()`, pass-through геттеры) нужно заменить:
  состояние гидравлики — проверкой `HydraulicsResults != null`, остальное —
  прямым чтением из модульных VM/данных.
- `SetPipeSpacing`: канонический источник загрузки проекта —
  `ProjectLoadOrchestrator.RestoreModules` (под guard `IsLoadProjectInProgress`);
  интерактивные изменения — `ThermalViewModel`.

## Что осознанно НЕ сделано

- Разделение `CalculationContext` на reader/writer интерфейсы — фактическая
  writer-дисциплина уже покрыта `CalculationContextWriterAuthorityTests`,
  выигрыш не оправдывает риск.
- `UpdateCollectorsList`/`UpdateCircuitsFilter`/`UpdateCollectorSummary` остались
  в `ResultsViewModel` — они управляют состоянием выбора коллектора на экране
  (обязанность VM, а не маппинг).
