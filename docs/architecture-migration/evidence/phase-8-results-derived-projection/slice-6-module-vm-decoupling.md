# Slice 6 — module-ViewModel decoupling + DI + save-path templates (branch a)

Класс: production/test. Дата: 2026-09-03.

## Scope executed

1. **`ResultsViewModel` decoupled от трёх module-ViewModel**: удалены ctor-параметры
   и поля `_climateViewModel`, `_constructionViewModel`, `_thermalViewModel`;
   удалено публичное свойство `ConstructionViewModel` (потребителей нет —
   grep по src, MainWindow использует MainViewModel-ское). В DI изменение
   не требовалось: `AddSingleton<ResultsViewModel>()` авто-резолвит новый ctor.
2. **Save-path CustomTemplates — branch (a)**: чтение
   `_constructionViewModel.Templates` заменено на канонический persistence-seam
   `IProjectSnapshotPersistenceInputs.Templates` (опциональный ctor-параметр
   `persistenceInputs`; тот же репозиторий шаблонов, что Phase 6 file-save через
   `ProjectSnapshotFactory`). Это устраняет расхождение источников между
   file-save и отчётом: оба теперь читают репозиторий.
3. **Dead code удалён**: `HasUnsavedData()` (`[Obsolete]`, private, 0 callers —
   доказательство в slice-2 receipt) — его чтения трёх module-VM блокировали
   декомпозицию.
4. **Staged residual (Phase 9, не скрыт)**: `_circuitsViewModel` остаётся
   (общие CircuitRow-объекты + `HydraulicSummaryBuilder(CollectorData)` +
   чтение выбора — именованный долг slice-4 receipt).
5. **Тесты**: 8 сайтов `new ResultsViewModel(...)` обновлены (6 файлов); хелпер
   `ResultsViewModelTestHelpers.CreateResultsViewModel` получил out-overload
   (старые 2-арг вызовы не сломаны); behavior-contracts фикстура хранит module-VM
   в полях фикстуры; `CreateUninitializedMainWindow` (contracts) принимает
   module-VM параметрами (MainViewModel-зависимости остаются легитимными);
   `ReplaceCollectorsCanonical` — зеркалит сеяние коллекторов в канонический
   HydraulicsState (готовность/KPI по канону); thermal-результат round-trip
   теста публикуется канонически (`CompleteCalculation`); шаблоны сеются в
   канонический `templateRepo` + `persistenceInputs` в локальном хелпере;
   name-based lookup кастомного шаблона (дефолтные шаблоны не помечены
   IsBuiltIn и тоже попадают в CustomTemplates — индексная ассертация была
   артефактом VM-сеяния).

## Pre-existing baseline failures (НЕ регрессии Phase 8 — доказано)

`ProjectLoadOrchestrator.cs` несёт **pre-existing dirty-дельту** (вне этой
сессии; git diff HEAD: 105 строк, вызовы
`ImportProjectMaterialsAsync`/`ImportProjectTemplatesAsync` удалены из restore —
в HEAD их 2, в worktree 0). Следующие 5 тестов ожидают import-throw/импорт и
падают независимо от Phase 8:

1. `LoadProjectDataAsync_EarlyRestoreFailure_LeavesPartialStateAndClearsGuard`
2. `LoadProjectDataAsync_LateRestoreFailure_LeavesPartialStateAndClearsGuard`
3. `LoadProjectDataAsync_EarlyRestoreFailure_ClearsLeasePreservesPartialThermalDefaults`
4. `LoadProjectDataAsync_LateRestoreFailure_ClearsLeaseThermalRetainsPreFailureDefaults`
5. `ProjectData_Load_ImportsCustomMaterialsBeforeLayers`

Восстановление import — изменение production-поведения, требующее отдельного
owner-approved изменения (нельзя чинить молча в Phase 8). Вынесено владельцу в
consolidated stop. Требует owner-решения: либо restore снова импортирует
каталоги (обновить Phase 7-семантику отдельной поправкой), либо тесты
перефиксируются на текущее поведение.

## Commands

1. Прогон 1 (после декомпозиции): 131 passed / **8 failed** (NRE — тесты
   доставали module-VM через GetField удалённых полей) — исправлено
   out-overload + fixture-поля + параметрами.
2. Полный прогон 2: 2016 passed / **12 failed** — из них 6 моих
   (4 equipment-теста: канон пуст при VM-сеянии; ThenEdit: GetField;
   CustomTemplates: пусто) и 6 pre-existing (import-кластер). Мои исправлены:
   `ReplaceCollectorsCanonical`, канонический climate-edit, repo-сеяние шаблонов.
3. Финальный полный прогон: **2023 passed / 0 failed моей вины / 5 pre-existing
   (import-кластер) / 1 skipped (внешний fixture)** — TRX:
   `logs/slice-6-full-regression-2.trx`; точечные прогоны:
   `logs/slice-6-module-vm-decoupling.trx` (139/140), `logs/slice-6-fixes.trx`.

## Failure QA

- Статический проб: `ResultsViewModel` больше не содержит ссылок на
  `ClimateViewModel`/`ConstructionViewModel`/`ThermalViewModel` (grep = 0);
  остающиеся ссылки: `CircuitsViewModel` (staged residual, Phase 9),
  `_persistenceInputs` (канонический seam, не VM).
- Save/export output: ProjectSaveServiceTests/ProjectFileServiceResultTests/
  стабилизация зелёные — wire-вывод не изменился.

## Dirty baseline delta (этот slice)

`src/ViewModels/Results/ResultsViewModel.cs`;
tests: `ResultsViewModelTestHelpers.cs`, `ResultsStabilizationPhase1BehaviorContractsTests.cs`,
`ResultsStabilizationPhase1ContractsTests.cs`, `ResultsViewModelOpenProjectTests.cs`,
`ResetOrchestrationTests.cs`, `MainViewModelTests.cs`, `DialogServiceThreadAffinityTests.cs`,
`ConstructionServiceTests.cs`, `ProjectLifecycleFlowCharacterizationTests.cs`,
`ThermalMultiplicityCharacterizationTests.cs`, `ResultsViewModelCollectorEquipmentItemsTests.cs`.

## Статус

SLICE 6: PASS (branch (a) выполнен; 5 pre-existing baseline-провалов
зафиксированы и вынесены владельцу)
