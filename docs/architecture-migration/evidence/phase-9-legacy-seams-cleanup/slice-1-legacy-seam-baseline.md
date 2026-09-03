# Slice 1 — Legacy-seam baseline lock + inventories

Класс: tests/evidence only (production не менялся). Дата: 2026-09-03.
Baseline: HEAD `3a077c7`, отслеживаемое дерево чистое; сборка exit 0.

## Прогоны (TRX под `logs/`)

1. `slice-1-legacy-seam-baseline.trx` — фильтр плана (4 suite):
   **38 passed / 2 failed / 0 skipped**. Оба failure — pre-existing кластер
   LIM-P8-2: `LoadProjectDataAsync_EarlyRestoreFailure_LeavesPartialStateAndClearsGuard`,
   `LoadProjectDataAsync_LateRestoreFailure_LeavesPartialStateAndClearsGuard`.
2. `slice-1-lim-p8-2-cluster.trx` — целевой кластер:
   **0 passed / 5 failed / 1 skipped / всего 6**. Ровно 5 именованных
   LIM-P8-2-тестов падают; `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
   — RR-004 skip (внешний fixture), не pass.

## (a) Сайты `_circuitsViewModel` в `ResultsViewModel.cs`

| Сайт | Метод | Что делает | Кандидат замены |
|---|---|---|---|
| `:42, :509, :530` | поле/ctor/присвоение | последняя module-VM ссылка (staged residual Phase 8) | удаление после слайсов 3-4 |
| `:1399-1408` | `UpdateCollectorSummary` | читает `_circuitsViewModel.SelectedCollectorIndex`, `Collectors`, `SelectedCollector.Summary` → `CollectorSummary` | свой выбор Results (`SelectedCollectorIndex`) + summary из канонического `HydraulicCollectorSnapshot.Summary` (Results уже строит `CollectorInfo` из канона в `UpdateCollectorsList:1343-1390`) |
| `:1422` | `UpdateCircuitsFilter` | берёт `CollectorData` из VM по **своему** индексу Results | тот же индекс в канонических `HydraulicsState.Snapshot.Collectors` |
| `:1425-1433` | `UpdateCircuitsFilter` | **общие** объекты `CircuitRow` модуля: пишет `DisplayMode` (`:1429`) и добавляет их в `Circuits` | реконструкция Results-owned `CircuitRow` из `HydraulicCircuitSnapshot` (маппинг-инверсия `CircuitsViewModel.ToDomainResult`/`ApplyLifecycleSnapshotToAdapter`) |
| `:1445, :1464, :1478` | `UpdateCollectorSpecifications`/`UpdateCollectorEquipmentItems`/`RebuildHydraulicSummaryCards` | `HydraulicSummaryBuilder.Build*(_circuitsViewModel.Collectors)` — вход `IEnumerable<CollectorData>` | перегрузки builder'а на `IReadOnlyList<HydraulicCollectorSnapshot>` (builder используется только Results) |
| `:422` | doc-комментарий | ссылка на VM в доке | правка текста |
| `:1597-1601` | `Reset()` | комментарий о порядке очистки карточек vs `CircuitsViewModel.Reset()` | после отвязки комментарий не нужен; поведение очистки сохранить |

Примечание: `UpdateCircuitsFilter` уже использует **собственный** `SelectedCollectorIndex`
Results (`:1419`), а `UpdateCollectorSummary` — выбор **модуля**; расхождение
двух выборов — текущее characterized поведение, слайс 4 сохраняет наблюдаемый
результат (сводка по выбору Results, как в `SelectCollector:575-591`), любое
расхождение фиксируется в receipt слайса 4.

## (b) Потребители алиасов (`IProjectStateService` / `IProjectInfoService` / `IMarkDirtyService` / legacy `ProjectStateService`)

Живые production-потребители:

| Сайт | Члены |
|---|---|
| `MainWindow.xaml.cs:35,55,181,204` | `IsDirty` (guard закрытия/действий) |
| `MainViewModel.cs:29,51,78,166-168,182-183,195,211,238,249` | подписка `PropertyChanged` (`IsDirty`, `CurrentFilePath`), `IsDirty`, `CurrentFilePath`, `MarkClean()` |
| `ResultsViewModel.cs:29,60-95,499,1632-1639` | `ProjectNumber`/`ProjectObject` pass-through + `MarkDirty()` (`:72,:92`) |
| `ServiceCollectionExtensions.cs:201-203` | forwarding-регистрации трёх интерфейсов → singleton `ProjectSession`; `:83` — `IMarkDirtyService` в factory `ThermalStateCoordinator` |
| `ProjectSession.cs:16,41` | класс **реализует** оба алиаса; ctor-параметр `IMarkDirtyService? hydraulicsDirtyService` |
| `ProjectSessionClimateState.cs:17,35`; `ProjectSessionConstructionState.cs:16,26`; `ProjectSessionHydraulicsState.cs:41,44` | внутренние optional `IMarkDirtyService` (сессионные dirty-швы) |
| `ThermalStateCoordinator.cs:44,64` | `IMarkDirtyService` (публикация результата → dirty) |

Мёртвые параметры (без call-sites вызова):

- `ClimateViewModel.cs:254`, `ConstructionViewModel.cs:227`, `ThermalViewModel.cs:235,291` — ctor-параметры `IMarkDirtyService` без вызовов (legacy-совместимость).

`IProjectInfoService` самостоятельных потребителей не имеет (только база `IProjectStateService`).

Legacy `ProjectStateService` (`src/Services/Results/ProjectStateService.cs`, ctors `()` и `(IProjectSession)`) — **test seam**: в production DI не регистрируется; тесты: `ConstructionServiceTests.cs:62`, `DialogServiceThreadAffinityTests.cs:82`, `ClimateThermalInvalidationRegressionTests.cs:418` (`(session)`), `ProjectLifecycleFlowCharacterizationTests.cs:49,65`.

## (c) VM-члены, которых касается `ProjectLoadOrchestrator` (+ кандидат границы)

| Сайт | Член | Кандидат |
|---|---|---|
| `:82, :148` | `_climateViewModel.SearchQuery` (сброс / установка выбранного города) | view-side эффект — перенести в адаптерный слой по завершении restore |
| `:147` | `_climateViewModel.FindCityByName(...)` | каталог городов — сервис/репозиторий, а не VM |
| `:83, :161` | `_constructionViewModel.ApplyLifecycleSnapshotToAdapter(result.After)` | adapter-mirror по `ProjectLoad` origin (у гидравлики уже есть паттерн: подписка на `Changed` с фильтром `ProjectLoad`, `CircuitsViewModel.OnHydraulicsStateChanged:875-891`) |
| `:296, :342, :361` | `_constructionViewModel.AvailableMaterials` (резолв материалов слоёв) | `IMaterialRepository` |
| `:337-389` | `LayersAbovePipe`/`LayersBelowPipe`, `GroundwaterLevel`, `UpdateCalculations()` | каноническая реконструкция ConstructionState + adapter-mirror; вычисления — application boundary, не VM |
| `:88` | `_thermalViewModel.Reset()` | adapter-mirror по restore-событию |
| `:107, :197` | `_thermalViewModel.AvailablePipes` + `ThermalPersistenceMapper.ResolveStandardPipe` | каталог труб — сервис/репозиторий |
| `:192-198` | `SelectedMode/SupplyTemperature/GroundTemperature/SelectedPipe/PipeSpacing` | запись кандидата в `ThermalState` (приложение), mirror в VM |
| `:216` | `_thermalViewModel.LoadResult(...)` | публикация результата — уже есть coordinator-путь `CompleteCalculation`; VM-зеркало по событию |
| `:223` | `_thermalViewModel.CalculateCommand.ExecuteAsync(null)` | расчёт — application service; VM-команда вызывать не должна оркестрацию restore |
| `:90, :202, :230` | `_circuitsViewModel.Reset()`, `ApplyLifecycleSnapshotToAdapter(...)` x2 | adapter-mirror по `ProjectLoad` origin (паттерн уже есть в `CircuitsViewModel`) |

Прочие application-сервисы с concrete-VM зависимостями: grep по `src/Services/`
даёт совпадения только в комментариях/именах типов; подтверждение — статический
тест слайса 5.

## (d) LIM-P8-2 на чистой базе

Ровно 5 pre-existing failures (см. прогон 2), ни одного «нового» отказа; RR-004 —
skip. Полное имя теста `ProjectData_Load_ImportsCustomMaterialsBeforeLayers` —
`ConstructionServiceTests.cs:973`; rest — `ProjectLifecycleFlowCharacterizationTests.cs:281,325`,
`ThermalMultiplicityCharacterizationTests.cs:1278,1312`.

## Статус

SLICE 1: PASS — база зелёная кроме известных 5, инвентаризации (a)-(d) полные.
