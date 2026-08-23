# phase-5-hydraulics-state - Work Plan

## TL;DR (For humans)

**Что вы получите.** Гидравлика станет четвёртым каноническим срезом состояния: `ProjectSession.HydraulicsState` — единственный writable owner глобальных входов (гликоль, концентрация, шаг, тепло-процент), коллекторов/контуров и сохранённых результатов расчёта. `CircuitsViewModel` превратится из хранилища состояния в WPF-адаптер (как уже произошло с Climate, Construction и Thermal). Сохранение `.smc` будет читаться из канонического снимка, а загрузка — применять один канонический snapshot вместо прямых записей в ViewModel.

**Почему так.** Это следующий шаг утверждённой последовательности миграции (INV-014: Climate → Construction → Thermal → Hydraulics) и закрывает целевой инвариант `INV-005` («Hydraulics inputs and collectors SHALL be owned by ProjectSession.HydraulicsState»). План зеркалит принятую и независимо проверенную структуру фазы 4: characterization-first, immutable контракт с закрытыми origin/mutations, compat-адаптер сервиса, coordinator с единственными подписками, persistence mapper, guard-suite, agent-operated UI QA, обновление шести карт/widget.

**Что НЕ сделает.** Не изменит наблюдаемое поведение: wire format `.smc` байт-совместим (Version «1.1», дубль FlowRegime/FlowRegimeString сохраняется), формулы не трогаются, количества событий/пересчётов сохраняются по замороженной таблице Behavioral Counts фазы 4. Не включает отложенные поведенческие долги (мёртвое поле `HydraulicsResults` как функциональный долг, UX invalid/reset thermal) — владелец закрыл Q1 рекомендацией «вне скоупа».

**Объём/риск.** 14 задач + F1–F4, одна sequential lane. Основные риски: (1) dual-subscriber момент при переносе подписок — гасится единым merged green boundary Todos 5–7; (2) wire-совместимость ~40 полей — доказывается немодифицированными ProjectRoundTripTests; (3) restore census ST-017 известен как неполный — закрывается characterization Todo 2 до любых ownership-правок.

**Ключевые решения.** Контракт события контекста сохраняет литерал источника `"CircuitsViewModel"` (наблюдаемый payload неизменен), физическая точка публикации переезжает в coordinator. Статусная совместимость через пере-применение текущих входов с origin SystemApply (прецедент Thermal). Предопределён contingency-мост AMZ-H1 по прецеденту AMZ-1 на случай, если characterization докажет невыразимость перехода закрытым API.

## Scope

### In scope

1. Новый канонический срез `IProjectSessionHydraulicsState` / `ProjectSessionHydraulicsState`: immutable snapshots (GlobalInputs, Collectors/Circuits c входами + сохранёнными OperatingResult/DesignResult + Summary, Status), закрытый enum `HydraulicsMutationOrigin`, закрытый mutation API, структурное равенство, defensive copies.
2. Присоединение ровно одного экземпляра среза к `ProjectSession` и продление `IProjectSession`; DI identity proof (срез создаётся ProjectSession, НЕ регистрируется отдельно).
3. Compat-адаптер `ICalculationStateService` для hydraulics-статуса: удаление backing-полей `_hydraulicsIsCalculating`/`_hydraulicsValidationMessage`, маршрутизация `SetHydraulicsCalculating`/`SetHydraulicsError`/`ResetHydraulicsState` в канонические мутации, трансляция completions в существующие `StateChanged` события.
4. Конверсия `CircuitsViewModel` в адаптер: user-мутации входов и коллекций идут через канонические мутации с origin User; UI-коллекции становятся зеркалами под guard-флагами (паттерн `_isMirroringClimateState` / construction `SyncStateFromCollections`).
5. `HydraulicsStateCoordinator` (по образцу DEC-T04A): единственные upstream-подписки приложения (`ContextChanged`, `PipeSpacingChanged`, `StateChanged`), оркестрация синхронного расчёта, единственная публикация `CalculationContext.UpdateHydraulics`, выдача immutable completion data адаптеру.
6. Lifecycle: reset/new-calculation и project restore маршрутизируются через срез с origins (`UserReset`/`ProjectLoadReset`/`ProjectLoad`); семантика пересчётов при restore следует замороженной таблице Behavioral Counts фазы 4: каждая валидная публикация `ThermalResult` — включая финализацию restore через `LoadResult` (ThermalStateCoordinator.cs:239-240) и fallback-calc при отсутствии сохранённого результата — даёт ровно один логический hydraulics-расчёт; сохранённые результаты контуров применяются из файла после него (`RestoreCircuitsResults`) без дополнительного пересчёта.
7. `HydraulicsPersistenceMapper` (static pure): `BuildHydraulicsProjectData(snapshot)` и `BuildRestoreCandidate(dto)` с точным воспроизведением текущего wire-поведения (включая парсинг FlowRegimeString→FlowRegime→Laminar); save в `ResultsViewModel` потребляет Snapshot среза; прямые записи `ProjectLoadOrchestrator` заменяются каноническим Restore.
8. Characterization-first расширение покрытия: мультиплексность мутаций/событий/расчётов, restore census ST-017, dirty-интенты, subscription hygiene.
9. Guard suite `HydraulicsStateLegacyStoreGuardTests` — 8 категорий `[NegativeFixture]`.
10. Обновление шести архитектурных карт, shared model, widget и workflow evidence; append-only запись в `TASK_CONTEXT.md`.

### Out of scope / Must-NOT-Have

- **Никаких изменений наблюдаемого поведения**: формулы гидравлики, автоподбор коллектора, cold-start deltaT fallback, тексты сообщений — без изменений.
- **Wire format `.smc`**: Version остаётся «1.1»; поля DTO `HydraulicsProjectData` и вложенных типов не переименовываются, не удаляются, не добавляются; дубль `FlowRegime`/`FlowRegimeString` сохраняется; сериализационные настройки `ProjectFileService` не меняются.
- **Отложенные поведенческие долги ВНЕ скоупа** (решение владельца по Q1, 2026-08-24): подключение/удаление «мёртвого» поля `HydraulicsResults` как функциональное изменение; обработка invalid/Reset thermal в hydraulics UX; что-либо из fix-thermal-to-hydraulics-sync D4/D5.
- **Без broad refactor `CircuitsViewModel`**: конверсия в адаптер ограничена ownership-переносом; реструктуризация команд/регионов вне необходимого запрещена.
- **ST-005 не мигрирует**: дубликат `ICalculationStateService.IsLoadProjectInProgress` остаётся (lifecycle-cleanup последующей фазы), кроме неизбежных касаний guard-ами.
- **Results projection не мигрирует** (следующая фаза по INV-014); `ResultsPdfDataBuilder` продолжает читать адаптер-зеркала.
- **Никаких версионных веток**: единственная version-ветка загрузки остаётся construction AbovePipe (<1.1).
- **Реализатор не трогает `STATE.json` stage/pendingGates** — control-plane transitions только через owner gates.
- **Никакого второго центрального среза параллельно**; никакой правки защищённых чужих dirty-файлов; никакого commit/stage/reset чужих изменений.

### Exact target contract

**Файлы состояния (новые):**

- `src/Services/Project/IProjectSessionHydraulicsState.cs` — интерфейс среза.
- `src/Services/Project/HydraulicsStateSnapshots.cs` — immutable snapshots:
  - `HydraulicsStateSnapshot` = `GlobalInputs` + `Collectors : IReadOnlyList<HydraulicCollectorSnapshot>` + `Status`;
  - `HydraulicGlobalInputsSnapshot` = GlycolType, GlycolConcentration, SupplySpacingCm, SupplyHeatPercent;
  - `HydraulicCollectorSnapshot` = CollectorNumber, CollectorType, ValveType, `Circuits : IReadOnlyList<HydraulicCircuitSnapshot>`, `Summary : HydraulicCollectorSummarySnapshot?`;
  - `HydraulicCircuitSnapshot` = входы (CircuitNumber, CircuitLength, SupplyLength, SupplySpacingCm, SupplyHeatPercent, PipeSpacingCm) + `OperatingResult?`, `DesignResult?` (immutable result snapshots с полным набором полей текущего `CircuitResultProjectData`, кроме legacy wire-дублей);
  - `HydraulicCollectorSummarySnapshot` = поля текущего `CollectorSummaryProjectData`;
  - `HydraulicsStatusSnapshot` = Phase (`Actual | Calculating | Error`) + ValidationMessage.
  - Field-by-field structural equality; reference equality запрещён; defensive copies на входе и выходе.
- `src/Services/Project/HydraulicsMutationOrigin.cs` — закрытый enum: `User, UserReset, ProjectLoadReset, ProjectLoad, Calculation, Initialization, SystemApply`. User reset никогда не смешивается с lifecycle reset.
- `src/Services/Project/ProjectSessionHydraulicsState.cs` — sealed реализация:
  - Закрытый mutation API: `ApplyGlobalInputs(candidate, origin)`, `ReplaceCollectors(collectors, origin)`, `BeginCalculation()`, `CompleteCalculation(results, summaryByCollector, origin=Calculation)`, `FailCalculation(message)`, `Restore(snapshot, ProjectLoad)`, `ResetToDefaults(origin)`;
  - Результат мутации `HydraulicsMutationResult` = `Changed | NoChange | Rejected`; ровно один changed mutation = ровно одно completion-событие `Changed` (EventArgs несёт OldSnapshot/NewSnapshot/Origin); NoChange/Rejected = ноль событий;
  - Dirty intent: только origin User поднимает `IMarkDirtyService.MarkDirty()` (один intent на changed mutation); lifecycle origins никогда;
  - Result-поддерево — canonical last derived value: писатели только `CompleteCalculation`, `FailCalculation` (status-only), `Restore`;
  - Событие: `event EventHandler<HydraulicsStateChangedEventArgs>? Changed`.

**Coordinator (новые файлы):** `src/Services/Project/IHydraulicsStateCoordinator.cs`, `src/Services/Project/HydraulicsStateCoordinator.cs` — sealed singleton, регистрируется в `src/Configuration/ServiceCollectionExtensions.cs`, eagerly materialized constructor injection во ViewModel (не service locator). Ровно пять обязанностей:
1. Перевод пользовательских команд (Calculate selected collector, CalculateAll) в закрытые мутации + вызовы `ICircuitsCalculator`/`IGlycolDataService`/`ICollectorTypeSelector`/`ICircuitsValidator`.
2. Один dirty intent на changed user input.
3. Оркестрация расчёта: валидация, glycol lookups, автоподбор типа коллектора, per-collector расчёт — синхронно, с сохранением точной текущей последовательности `CircuitsViewModel.Calculate()` (:429-459).
4. Единственная approved публикация контекста: вызов `_calculationContext.UpdateHydraulics(summaries, "CircuitsViewModel")` — строковый литерал источника сохраняется дословно (наблюдаемый payload ContextChangedEventArgs неизменен); физический call site в production ровно один.
5. Выдача immutable completion data адаптеру (ValidationMessage, IsCalculating mirror, rebuild cards).

Единственные upstream-подписки приложения, переносимые атомарно из `CircuitsViewModel` в coordinator (DEC-H04A): `CalculationContext.ContextChanged`, `ICalculationStateService.PipeSpacingChanged`, `ICalculationStateService.StateChanged`. Подписки принадлежат coordinator на lifetime приложения. Маршрутизация context events сохраняет текущее поведение: `ThermalInputs` → notify-only (ноль расчётов); `ThermalResult` valid → ровно один CalculateAll; `ThermalResult` invalid/null → notify-only (ноль расчётов); `Climate` → полный эффект текущего `UpdateFromClimateModule` (CircuitsViewModel.cs:1189-1197): обновление display-значений И ровно один `CalculateAll`; source == "CircuitsViewModel" → игнор (защита от feedback).

**Compat-адаптер статуса:** `SetHydraulicsCalculating()` → `BeginCalculation()`; `SetHydraulicsError(msg)` → `FailCalculation(msg)`; `ResetHydraulicsState()` → re-apply текущего GlobalInputs snapshot с origin `SystemApply` (нормализация статуса к Actual — прецедент Thermal `ResetThermalState`); геттеры `HydraulicsIsCalculating`/`HydraulicsValidationMessage` транслируются из Status snapshot; трансляция canonical completions → существующие `StateChanged("Hydraulics", ...)` с сохранением event multiplicity, зафиксированной characterization Todo 2. Backing-поля удаляются.

**Contingency AMZ-H1:** если characterization Todo 2 или исполнение Todos 5-7 докажет, что какой-то production call site требует переход, невыразимый закрытым API (прецедент Thermal AMZ-1 `SetThermalNeedsRecalculation`), добавить РОВНО ОДНУ transitional mutation `ApplyNeedsRecalculation`-типа на `IProjectSessionHydraulicsState` с allow-list ровно одного production caller, задокументировать в `evidence/phase-5-hydraulics-state/task-5/blocker-analysis.md` и покрыть guard-категорией caller-set. Форма моста предопределена настоящим планом (owner pre-authorization по прецеденту AMZ-1 Option A); любая более широкая импровизация = stop rule.

**Persistence mapper (новый файл):** `src/Services/Project/HydraulicsPersistenceMapper.cs` — static pure, не знает о состоянии/событиях/dirty:
- `BuildHydraulicsProjectData(HydraulicsStateSnapshot) : HydraulicsProjectData` — поле-в-поле текущего инлайн-блока `ResultsViewModel.SaveCurrentProject()` (:1709-1785);
- `BuildRestoreCandidate(HydraulicsProjectData) : HydraulicsStateSnapshot` — поле-в-поле текущего restore-пути orchestrator (:168-206) + `RestoreCircuitsResults` (:238-331), включая парсинг FlowRegime: `OperatingResult/DesignResult.FlowRegimeString` → enum parse → fallback `Laminar`, отсутствующий Summary → null;
- XML-doc фиксирует полный wire-набор (аналог ThermalPersistenceMapper :16-37).

**Точки интеграции (существующие файлы):**

| Файл | Изменение |
|---|---|
| `src/Services/Project/ProjectSession.cs` | поле+свойство `HydraulicsState`, конструктор создаёт срез |
| `src/Services/Project/IProjectSession.cs` | свойство `IProjectSessionHydraulicsState HydraulicsState { get; }` |
| `src/Services/Navigation/ICalculationStateService.cs` | только doc-comments (поверхность методов не меняется) |
| `src/Services/Navigation/CalculationStateService.cs` | compat-адаптер статуса, удаление backing-полей |
| `src/ViewModels/Hydraulics/CircuitsViewModel.cs` | адаптер-конверсия, перенос подписок, делегирование Calculate |
| `src/Configuration/ServiceCollectionExtensions.cs` | регистрация coordinator (singleton) |
| `src/Services/Project/ProjectLoadOrchestrator.cs` | канонический Restore вместо прямых записей; удаление тела `RestoreCircuitsResults` как прямого писателя |
| `src/ViewModels/Results/ResultsViewModel.cs` | save-блок :1709-1785 → mapper over Snapshot; Reset()/LoadHydraulicsData() остаются читателями зеркал |
| `src/Views/Hydraulics/CircuitsView.xaml`, `CircuitInputView.xaml` | accessibility-only `AutomationProperties.AutomationId` каталога Todo 13 |
| `docs/architecture-migration/maps/*` (шесть карт) | refresh после зелёных гейтов |

### Execution and recovery discipline

- Одна sequential lane; Todos 5, 6, 7 образуют ОДИН merged green boundary (урок AMZ-1 фазы 4): промежуточные состояния компилируются, но commit выполняется только после зелёных гейтов всех трёх — ни в одном committed boundary не существует двух активных подписчиков одного события.
- Каждый todo заканчивается компилируемым состоянием и прогоном своих focused gates до перехода к следующему.
- Stop rules: расхождение characterization-ожиданий с фактом → остановка, фиксация blocker receipt, варианты владельцу; подтверждённая невыполнимость плана → workflow в blocked, никаких тихих послаблений acceptance criteria.
- Найденный guard-ом (Todo 11) дефект возвращается в owning todo; Todo 11 guard-only.
- Реализатор работает в dirty worktree: baseline-relative delta; чужие изменения не трогаются и не откатываются.

## Verification strategy

Командный каталог (исполнение из repo root; каждая команда exit 0):

```text
# H0 — authoritative state/plan gate; expect exit 0 и JSON valid=true.
node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan

# H1 — production builds; expect exit 0, 0 errors, no warning increase vs Todo 1 baseline.
dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo
dotnet build src\SnowMeltingCalculator.csproj -c Release --nologo

# Сборка Release test assembly перед любыми H2-H5 --no-build вызовами; expect exit 0.
dotnet build tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Release --nologo

# H2 — focused canonical state/adapter/guard; failed=0, каждая новая тест-класса выполнена.
dotnet test tests\...\SnowMeltingCalculator.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ProjectSessionHydraulicsStateTests|FullyQualifiedName~DiRegistrationTests"

# H3 — upstream invalidation counts; failed=0 и точные count assertions Todo 2.
--filter "FullyQualifiedName~ThermalToHydraulicsIntegrationTests|PipeSpacingSynchronizationTests|DoubleCalculationPreventionTests|ClimateToHydraulicsIntegrationTests"

# H4 — context/Hydraulics consumers; failed=0, writer authority = coordinator.
--filter "FullyQualifiedName~CalculationContextWriterAuthorityTests|GlycolAutoRecalculationTests|CircuitsViewModelColdStartTests"

# H5 — lifecycle/persistence/Results; failed=0, все mapper/lifecycle fixtures выполнены.
--filter "FullyQualifiedName~ProjectRoundTripTests|ResultsViewModelOpenProjectTests|ProjectLifecycleFlowCharacterizationTests|HydraulicsMultiplicityCharacterizationTests"

# H6 — full Release; failed=0, no NEW NotExecuted identities vs baseline
#      (accepted baseline skips: RegenerateCircuitsBaseline, RegenerateBaseline,
#       ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile).
dotnet test tests\...\SnowMeltingCalculator.Tests.csproj -c Release --no-build

# H7 — model/runtime suites; каждый exit 0, JSON receipts записаны (см. Todo 14).
node docs/architecture-migration/workflow/validate-state.mjs validate-model docs/architecture-migration/maps/architecture-model.json

# H8 — deterministic widget; два прохода дают идентичные SHA-256; generate-widget --check все проверки.
node docs/architecture-migration/widget/generate-widget.mjs --check

# H9 — Todo 13-only UI QA harness: frozen exe SHA-256 до/после, JSON observations, скриншоты.
powershell -File evidence-path\ui-qa\run-hydraulics-ui-qa.ps1

# H10 — fail-closed structural verifier плана (адаптация verify-plan-structure.ps1 фазы 4); первый исполняемый в Todo 1.

# H11 — guard suite; впервые исполним ТОЛЬКО внутри Todo 11 (structural verifier требует v11_first_todo=11).
--filter "FullyQualifiedName~HydraulicsStateLegacyStoreGuardTests"

# H12-F2 / H12-F3 / H12-F1 — изолированные final receipts трёх доменов; invoked только после завершения всех todos.

# H13 — four-artifact binding: plan path + SHA-256 + STATE.json identity до и после каждой F-lane.
```

Agent-executed QA в каждом todo: happy-сценарий (ожидаемое значение/счётчик) + failure-сценарий (отрицательный fixture/probe, nonzero exit или rejected mutation), evidence путь указан в самом todo. Zero human intervention: UI QA через Automation harness; selector ambiguity/dialog/crash = nonzero exit без ручного fallback.

Test strategy: **characterization-first** (Todo 2 фиксирует текущее поведение до любых ownership-правок), далее tests-after для новых контрактов (Todo 3 state tests пишутся вместе с контрактом, красные до attach — допустимо, т.к. новые файлы). Production verification никогда не ослабляется.

## Execution strategy

### Dependency matrix

```text
T1 ──> T2 ──> T3 ──> T4 ──> [T5 ──> T6 ──> T7] (merged green boundary, один commit)
                                  │
                                  v
             T8 ──> T9 ──> T10 ──> T11 ──> T12 ──> T13 ──> T14 ──> [F1..F4]
```

- T2 может идти параллельно с T3 по исследованию, но commit-порядок линейный; T3 коммитится после T2 (characterization должен существовать до первого ownership-правки в T4/T6).
- T8 зависит от T7 (единственный писатель появляется в T7). T9/T10 зависят от T4+T7. T11 после T10 (guard проверяет конечное состояние). T12 после T11. T13 на стабильно зелёной сборке после T12. T14 после T13.
- F1-F4 параллельны между собой, все после T14; консолидация F4 последняя.

### Commit discipline

- Один commit на todo boundary: `phase-5(task-N): <краткое описание>`; исключение — Todos 5+6+7 = один commit `phase-5(tasks-5-7): merged green boundary ...`.
- Commit содержит только файлы allow-list своего todo; protected pre/post сравнение на каждом boundary (адаптация verify-protected-baseline.ps1).
- Никаких push; ветка и merge — вне этого плана.

## Todos

- [ ] 1. Capture protected Phase 5 baseline, tools, plan identity and dirty preimages
    - References: `docs/architecture-migration/evidence/phase-4-thermal-state/capture-baseline.ps1`, `verify-protected-baseline.ps1`, `verify-plan-structure.ps1` (образцы для адаптации); `STATE.json` (plan identity); AGENTS.md «dirty baseline-relative delta».
    - Acceptance: создан каталог `docs/architecture-migration/evidence/phase-5-hydraulics-state/` со скриптами-адаптациями (capture/verify baseline, plan structure verifier c требованием `v11_first_todo=11`); записаны `task-1/protected-pre.json` (git status snapshot защищённых путей + preimages dirty-файлов), `task-1/trx-debug.json`/`trx-release.json` (Debug+Release build logs, exit 0, 0 warnings/0 errors), `task-1/trx-full-release-inspect.json` (baseline full Release счётчики включая accepted NotExecuted identities), `task-1/todo-1-completion.json`; H0 exit 0; ни одного изменения product code/tests/maps.
    - QA happy: повторный запуск verify-protected-baseline.ps1 сразу после capture даёт mismatch=0. Evidence: `evidence/phase-5-hydraulics-state/task-1/todo-1-completion.json`.
    - QA failure: внесение синтетической чужой записи в защищённый список → verifier fail-closed nonzero (probe-режим, без реальной правки файлов). Evidence: `task-1/fixtures/out/negative-probe.json`.
    - Commit: `phase-5(task-1): baseline, tools, plan identity, dirty preimages`.

- [ ] 2. Lock Hydraulics writers, subscribers, calculations, lifecycle and persistence behavior before ownership edits
    - References: `src/ViewModels/Hydraulics/CircuitsViewModel.cs:429-459` (Calculate), `:1062-1088` (OnCalculationContextChanged), `:1113-1180` (input handler), `:997-1010` (UpdateFromThermalModule), `:683-698` (Reset); `src/Services/Navigation/CalculationStateService.cs:77-137`; `src/Services/Project/ProjectLoadOrchestrator.cs:72-88,97-233,238-331`; `src/ViewModels/Results/ResultsViewModel.cs:1709-1785`; behavioral counts table: `docs/architecture-migration/evidence/phase-4-thermal-state/task-8/task-8-context-hydraulics.md`; существующие сьюты из Verification strategy H3/H4/H5.
    - Acceptance: новый `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/HydraulicsMultiplicityCharacterizationTests.cs` фиксирует ДО правок: (a) каждое user-изменение GlobalInputs → точные dirty/event/calc счётчики; (b) add/remove collector/circuit → collection protocol + dirty; (c) Calculate command → SetHydraulicsCalculating → summaries publication → RebuildCards → ResetHydraulicsState ordering; (d) thermal valid result → ровно один CalculateAll; thermal invalid/null → ноль calc, notify-only; own-source → ноль рекурсий; spacing changed → одна compat event + один consumer update, no-op → ноль; (e) restore census: полный обход прямых записей orchestrator (collectors, inputData, SelectedCollectorIndex, RestoreCircuitsResults поля, FlowRegime fallback) с точными ожиданиями; (f) reset/load циклы не размножают подписки CircuitsViewModel; (g) save строит DTO поле-в-поле из VM (snapshot-тест структуры). Все новые тесты ЗЕЛЁНЫЕ на немигрированном коде. TRX + arithmetic receipt.
    - QA happy: полный прогон нового класса, failed=0. Evidence: `evidence/phase-5-hydraulics-state/task-2/trx-characterization-release.json`.
    - QA failure: мутированный expectation (например valid→два calc) падает ровно на одном assertion (демонстрация чувствительности). Evidence: `task-2/expected-negative-test-identities.json`.
    - Commit: `phase-5(task-2): characterization lock for hydraulics behavior`.

- [ ] 3. Add immutable Hydraulics state contract, structural equality and direct state tests
    - References: раздел «Exact target contract» настоящего плана; образцы: `src/Services/Project/ProjectSessionThermalState.cs`, `ThermalStateSnapshots.cs`, `src/Services/Project/IProjectSessionThermalState.cs`; тесты-образцы `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionThermalStateTests.cs`.
    - Acceptance: четыре новых файла (`IProjectSessionHydraulicsState.cs`, `HydraulicsStateSnapshots.cs`, `HydraulicsMutationOrigin.cs`, `ProjectSessionHydraulicsState.cs`) точно соответствуют контракту (закрытые enum/API, Changed|NoChange|Rejected, одно completion на changed, defensive copies, structural equality, dirty только User-origin); `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionHydraulicsStateTests.cs` покрывает: каждое mutation happy + NoChange + Rejected ветку, event multiplicity (ровно одно/ноль), defensive copy probes (мутирование источника после Apply не влияет на snapshot), equality probes, dirty-intent matrix по всем девяти origins. H2 зелёный (без DiRegistration части — attach в Todo 4).
    - QA happy: полный прогон state tests, failed=0. Evidence: `evidence/phase-5-hydraulics-state/task-3/trx-state-debug.json`.
    - QA failure: negative probe — попытка записи в массив snapshot извне невозможна (compile-time/immutability probe), rejected mutation не эмитит событие. Evidence: `task-3/trx-state-negative.json`.
    - Commit: `phase-5(task-3): immutable hydraulics state contract`.

- [ ] 4. Attach exactly one HydraulicsState to ProjectSession and prove runtime DI identity
    - References: `src/Services/Project/ProjectSession.cs:24-42`; `src/Services/Project/IProjectSession.cs`; `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs`; паттерн Thermal attach (тот же файл).
    - Acceptance: `ProjectSession` создаёт ровно один `ProjectSessionHydraulicsState`, `IProjectSession.HydraulicsState` возвращает reference-identical экземпляр; срез НЕ зарегистрирован в DI отдельно (только через session); `DiRegistrationTests` расширен assertions: resolve(IProjectSession).HydraulicsState reference-equals resolve-через-singleton-session; полный Debug build 0 warnings/0 errors; H0+H1+H2 зелёные.
    - QA happy: DiRegistrationTests зелёные. Evidence: `evidence/phase-5-hydraulics-state/task-4/trx-di-debug.json`.
    - QA failure: временная регистрация среза в DI как separate singleton → тест падает (демонстрация guard-value, откат правки). Evidence: `task-4/di-negative-probe.md`.
    - Commit: `phase-5(task-4): attach hydraulics slice to ProjectSession`.

- [ ] 5. Convert CalculationStateService Hydraulics status into a compatibility adapter
    - References: `src/Services/Navigation/CalculationStateService.cs:56-137`; прецедент: тот же файл, thermal-половина (AMZ-1, `ResetThermalState` через SystemApply); contract раздел «Compat-адаптер статуса»; `evidence/phase-4-thermal-state/task-5/blocker-analysis.md` (формат blocker receipt для AMZ-H1).
    - Acceptance: backing-поля `_hydraulicsIsCalculating`/`_hydraulicsValidationMessage` удалены; три метода маршрутизированы в канонические мутации согласно контракту; геттеры транслируют Status snapshot; трансляция completions → `StateChanged("Hydraulics", ...)` сохраняет multiplicity из Todo 2(g)/(c); публичная поверхность `ICalculationStateService` не изменилась; если characterization доказала невыразимый переход — применён AMZ-H1 bridge строго по contingency-контракту с receipt `task-5/blocker-analysis.md`; иначе файл blocker-analysis.md фиксирует «bridge not required» с обоснованием. Часть merged boundary — commit только после Todo 7.
    - QA happy: focused suite статусных переходов (Calculating→Actual, Error message round-trip, Reset normalize) failed=0. Evidence: `evidence/phase-5-hydraulics-state/task-5/trx-status-compat-release.json`.
    - QA failure: rejection-ветка FailCalculation при отсутствии BeginCalculation не меняет snapshot и не эмитит событий. Evidence: `task-5/status-rejection-probe.json`.
    - Commit: (merged) `phase-5(tasks-5-7): hydraulics status compat + adapter VM + coordinator single-subscriber boundary`.

- [ ] 6. Make CircuitsViewModel a canonical-state adapter and move calculation orchestration out of writable UI fields
    - References: `src/ViewModels/Hydraulics/CircuitsViewModel.cs` целиком; паттерны: `ClimateViewModel.MirrorSnapshot` + `_isMirroringClimateState` (`src/ViewModels/Climate/ClimateViewModel.cs:669-708`), construction `SyncStateFromCollections(origin)` (`src/ViewModels/Construction/ConstructionViewModel.cs:1110-1117`); contract разделы «CircuitsViewModel adapter» выше.
    - Acceptance: user-мутации входов (glycol/concentration/spacing/heat%) и коллекций (add/remove collector/circuit, edit rows) публикуются в срез как ApplyGlobalInputs/ReplaceCollectors с origin User; зеркалирование snapshot→UI под sync-guard без рекурсивных canonical writes; ViewModel больше не является canonical store для ST-016/ST-017 значений (зеркала допустимы); Calculate command делегирует coordinator; UpdateFromThermalModule сохранён как explicit-push seam для тестов с прежней семантикой. Часть merged boundary.
    - QA happy: focused adapter suite (user edit → state snapshot changed → UI mirror updated once) failed=0. Evidence: `evidence/phase-5-hydraulics-state/task-6/trx-adapter-release.json`.
    - QA failure: mirror-loop probe — применение lifecycle snapshot НЕ порождает User-dirty и НЕ публикует User-origin мутацию обратно. Evidence: `task-6/mirror-loop-probe.json`.
    - Commit: (merged — см. Todo 5).

- [ ] 7. Move upstream subscriptions to one canonical HydraulicsStateCoordinator
    - References: `src/Services/Project/ThermalStateCoordinator.cs:34-96` (структура, upstream subscriptions в конструкторе); DEC-T04A описание; contract раздел «Coordinator»; `src/Configuration/ServiceCollectionExtensions.cs`; `tests/SnowMeltingCalculator.Tests/Services/Project/ThermalStateCoordinatorTests.cs` (формат тестов).
    - Acceptance: созданы `IHydraulicsStateCoordinator`/`HydraulicsStateCoordinator` с ровно пятью обязанностями контракта; подписки `ContextChanged`/`PipeSpacingChanged`/`StateChanged` атомарно перенесены из CircuitsViewModel (VM их больше не ставит); coordinator — единственный production call site `UpdateHydraulics(...,"CircuitsViewModel")`; маршрутизация thermal/climate/spacing событий сохраняет счётчики Todo 2(d); регистрация singleton + eager materialization; `HydraulicsStateCoordinatorTests` покрывают каждую ветку маршрутизации; H2+H3+H4 зелёные. Merged boundary закрыт: полный focused прогон Todos 5-7 + H1 builds зелёные → один commit.
    - QA happy: coordinator suite + H3 counts suite failed=0. Evidence: `evidence/phase-5-hydraulics-state/task-7/trx-coordinator-release.json`.
    - QA failure: duplicate-subscriber probe — вторая подписка на ContextChanged в VM обнаруживается source-scan guard-предикатом (in-memory probe). Evidence: `task-7/duplicate-subscriber-probe.json`.
    - Commit: (merged — см. Todo 5).

- [ ] 8. Publish one Hydraulics projection through CalculationContext and preserve consumer counts
    - References: behavioral counts table (task-8 receipt фазы 4, секция Behavioral Counts); `src/Models/Navigation/CalculationContext.cs` (UpdateHydraulics, ContextChanged args); потребители: `ResultsViewModel.LoadHydraulicsData :1071-1090`, `UpdateCircuitsFilter :1372-1393`, `ResultsPdfDataBuilder`.
    - Acceptance: единственная публикация summaries за logical calc (никаких двойных ContextChanged); все потребители Results получают те же значения/порядок; V4/H4 набор тестов зелёный БЕЗ ослаблений; writer-authority test обновлён честно: единственный approved hydraulics writer в production = HydraulicsStateCoordinator (литерал payload сохранён); счётчики (valid thermal→один calc; invalid→ноль; own-source→ноль; spacing→один/ноль) подтверждены повторным прогоном.
    - QA happy: H4 filter run failed=0. Evidence: `evidence/phase-5-hydraulics-state/task-8/arithmetic.json` + TRX.
    - QA failure: synthetic double-publication probe в тесте → count assertion падает (демонстрация чувствительности). Evidence: `task-8/double-publish-negative.json`.
    - Commit: `phase-5(task-8): single canonical hydraulics projection`.

- [ ] 9. Route lifecycle reset, project restore and fallback calculation through HydraulicsState
    - References: `src/Services/Project/ProjectLoadOrchestrator.cs:72-88` (ResetModules), `:97-233` (restore sequence), `:218-228` (fallback calc), `:238-331` (RestoreCircuitsResults); routing facts phase-3.1 Task 9 (`UserReset`/`ProjectLoadReset`/silent `Load`); `src/ViewModels/Shell/MainViewModel.cs` (reset entrypoints); `ResultsViewModel.Reset() :1521-1579`.
    - Acceptance: ResetModules вызывает `_hydraulicsState.ResetToDefaults(ProjectLoadReset)` в правильном месте последовательности (до `_circuitsViewModel.Reset()`, который теперь только адаптер-очистка); MainViewModel new-calculation reset использует `UserReset`; restore применяет ОДИН канонический `Restore(candidate, ProjectLoad)` вместо прямых записей :168-206; адаптер обновляется одним completion; порядок относительно thermal restore/publication/fallback сохранён посимвольно (характеризация Todo 2(e)); второй load полностью заменяет первый проект без stale значений; повторные reset/load не размножают подписки; H5 зелёный.
    - QA happy: ProjectLifecycleFlowCharacterizationTests + новые lifecycle тесты failed=0. Evidence: `evidence/phase-5-hydraulics-state/task-9/trx-lifecycle.json` + `arithmetic.json`.
    - QA failure: restore-failure probe — reject кандидата оставляет предыдущий snapshot нетронутым и не эмитит событий. Evidence: `task-9/restore-rejection-probe.json`.
    - Commit: `phase-5(task-9): lifecycle and restore through canonical hydraulics state`.

- [ ] 10. Complete Hydraulics persistence mapping and make Results save/read canonical projections
    - References: `src/Services/Project/ThermalPersistenceMapper.cs` (shape), `ConstructionPersistenceMapper.cs`; wire DTOs `src/Models/Project/ProjectData.cs:311-487`; inline save block `ResultsViewModel.cs:1709-1785`; orchestrator direct-write блоки; `ProjectFileService.cs:19-28,115-164` (НЕ менять).
    - Acceptance: `HydraulicsPersistenceMapper` создан static pure с двумя методами контракта и XML-doc полного wire-набора; save-блок заменён вызовом mapper over `HydraulicsState.Snapshot` (Version «1.1» не меняется); orchestrator consume candidates из mapper; тело RestoreCircuitsResults как прямой писатель удалено; **ProjectRoundTripTests и ResultsViewModelOpenProjectTests проходят БЕЗ модификаций** (wire-compat proof; допустимо только добавление новых тестов, не изменение существующих assertions); H5 зелёный; byte-level round trip: save→load→save двух fixtures даёт идентичные hydraulicsData секции.
    - QA happy: round-trip suite + byte-comparison script pass. Evidence: `evidence/phase-5-hydraulics-state/task-10/trx-persistence-results.json` + `task-10/wire-byte-compare.json`.
    - QA failure: negative probe — намеренно изменённое поле в mapper → byte-compare падает с diff-отчётом (исправлено обратно в том же lane). Evidence: `task-10/wire-diff-negative.json`.
    - Commit: `phase-5(task-10): canonical hydraulics persistence mapping`.

- [ ] 11. Enforce final sole-owner, immutable projection, subscription and DI guards
    - References: `tests/SnowMeltingCalculator.Tests/Services/Project/ThermalStateLegacyStoreGuardTests.cs` (структура 8 категорий); contract раздел «Guard suite»; V11/H11 first-executable-here правило.
    - Acceptance: `HydraulicsStateLegacyStoreGuardTests.cs` с 8 категориями `[NegativeFixture]`: VmWritableStore (нет canonical backing InputData/Collectors-store в VM — зеркала разрешены, canonical writes только через срез), ServiceHydraulicsStore (нет backing-полей статуса в сервисе), OrchestratorDirectAssign (нет прямых присваиваний в VM из orchestrator), ResultsNonCanonicalSave (save читает только Snapshot/mapper), ContextUnapprovedWriter (production writers UpdateHydraulics = ровно coordinator; UpdateThermal* = ровно ThermalStateCoordinator), SnapshotMutability (все snapshot-типы immutable), DuplicateUpstreamSubscriber (ContextChanged/PipeSpacingChanged/StateChanged подписчики = ровно coordinator), DiIndependentStateRegistration (срез не в DI отдельно). Механизм: source-scan по `src/**` + in-memory behavioral probes с синтетическими violating inputs. Все категории зелёные; H11 исполняется впервые здесь.
    - QA happy: guard suite failed=0. Evidence: `evidence/phase-5-hydraulics-state/task-11/trx-guards-release.json`.
    - QA failure: каждая категория содержит self-check: подача violating input в предикат → детект (fixture identities recorded). Evidence: `task-11/guard-self-checks.json`.
    - Commit: `phase-5(task-11): sole-owner guard suite`.

- [ ] 12. Run Debug/Release builds, focused/affected/full suites and reconcile exact executable evidence
    - References: командный каталог H1/H6 + все focused фильтры; baseline счётчики Todo 1; формат reconciliation `evidence/phase-4-thermal-state/task-9/arithmetic.json`.
    - Acceptance: Debug build 0 warnings/0 errors; Release build 0/0; полный Release suite failed=0; NotExecuted identities ⊆ baseline accepted set (никаких новых); arithmetic receipt сходится (число тестов = passed+failed+skipped+NotExecuted); protected pre/post drift=0 вне allow-list todos.
    - QA happy: все команды exit 0. Evidence: `evidence/phase-5-hydraulics-state/task-12/` (TRX + arithmetic + protected-post.json).
    - QA failure: reconciliation-скрипт обнаруживает расхождение счётчиков → nonzero + отчёт (probe на исторических данных фазы 4). Evidence: `task-12/reconciliation-negative-probe.json`.
    - Commit: `phase-5(task-12): executable evidence reconciliation`.

- [ ] 13. Execute agent-operated Hydraulics user flows on the stable build
    - References: harness образцы `evidence/phase-4-thermal-state/verify-frozen-release.ps1`, `parse-trx.ps1`, task-13 fixtures generator; existing AutomationIds `HydraulicsPipeSpacing`/`HydraulicsSupplyTemperature`/`HydraulicsReturnTemperature`; views `src/Views/Hydraulics/CircuitsView.xaml`, `CircuitInputView.xaml`, `CircuitsResultsView.xaml`; fixture источник `tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc`.
    - Acceptance: (a) заранее, одним каталогом: добавлены accessibility-only AutomationIds `HydraulicsGlycolType`, `HydraulicsGlycolConcentration`, `HydraulicsSupplyHeatPercent`, `HydraulicsCalculateButton`, `HydraulicsValidationMessage` (+ при необходимости `HydraulicsCircuitLengthFirst`) — без иных XAML-правок; (b) adapted harness в `evidence/phase-5-hydraulics-state/ui-qa/` с frozen exe SHA-256 до/после каждого запуска; (c) потоки: launch project-a → outputs match; edit glycol → recalc → outputs change per fixture math; edit circuit length → summary card updates; Ctrl+S save → reload → identical outputs; second load project-b → clean replace; reset → defaults; corrupt unknown-pipe.smc → graceful validation, no crash; (d) скриншоты 01..NN; (e) selector ambiguity/dialog/crash = nonzero exit; (f) observations.json + failure-observations.json записаны.
    - QA happy: полный прогон harness exit 0, все шаги observed. Evidence: `evidence/phase-5-hydraulics-state/ui-qa/observations.json`.
    - QA failure: failure-ветка corrupt fixture проходит отдельным прогоном с корректным graceful behavior. Evidence: `ui-qa/failure-observations.json`.
    - Commit: `phase-5(task-13): agent-operated hydraulics UI QA`.

- [ ] 14. Refresh all six architecture views, shared model, widget and workflow evidence
    - References: `docs/architecture-migration/maps/*.md` + `architecture-model.json` + schemas; widget pipeline `docs/architecture-migration/widget/generate-widget.mjs` + `model-contract.mjs` + `verify-widget.mjs`; update-model образец `evidence/phase-4-thermal-state/task-14/update-model.mjs`; TASK_CONTEXT.md (append-only journal).
    - Acceptance: шесть карт отражают: ST-016..ST-019 → migrated/verified; INV-005 verified с evidence refs; compile-time/di-runtime/reactive/persistence/user-flow обновлены по фактическим изменениям; `update-model.mjs` адаптирован и прогнан; model-v2/runtime-v2 suites pass (assertion/mutation counts recorded); ДВА generation passes дают byte-identical HTML (SHA-256 recorded); `generate-widget.mjs --check` все проверки; TASK_CONTEXT.md append history entry (решения, evidence links); STATE.json НЕ тронут реализатором.
    - QA happy: H7+H8 exit 0, hashes equal across two passes. Evidence: `evidence/phase-5-hydraulics-state/task-14/model-v2.json`, `runtime-v2.json`, widget hash receipt.
    - QA failure: invalid-model fixture (missing evidence edge) отвергается validator'ом (recorded negative fixtures, паттерн фазы 4). Evidence: `task-14/fixtures/model-invalid-id.json`.
    - Commit: `phase-5(task-14): six-view dossier, model and widget refresh`.

## Final verification wave

Runs after ALL todos; three independent domains + consolidation; ALL must APPROVE.

- [ ] F1. Verify Conformance / Scope / Provenance, dirty-worktree preservation and architecture-dossier fidelity
    - References: план (этот файл) + `STATE.json` identity; protected baseline chain Todos 1-14; AGENTS.md review contract.
    - Acceptance: каждый In-scope пункт реализован; ни один Must-NOT-Have не нарушен (проверка wire DTO полей, Version literal, отсутствие AMZ-H1 сверх контракта либо его соответствие); protected drift = allow-listed only; evidence paths существуют и согласованы; SUBJECT binding `<phase>@<plan-sha256>` зафиксирован.
    - Evidence: `evidence/phase-5-hydraulics-state/final/f1/conformance-scope-provenance.md`.
- [ ] F2. Audit architecture/code quality and sole Hydraulics ownership
    - Acceptance: независимый аудит исходников: один writable owner на каждое значение ST-016..ST-019; ViewModels-адаптеры; services не зависят от concrete VM; snapshot immutability; guard suite честный (self-checks реальные).
    - Evidence: `final/f2/architecture-quality.md`.
- [ ] F3. Re-run executable lifecycle, persistence, downstream and real user-flow QA
    - Acceptance: свежие прогоны H3/H4/H5/H6 выборочно + полный UI QA harness повторно на финальной сборке (fresh SHA check); TRX identities сверены с Todo 12.
    - Evidence: `final/f3/executable-qa.md` (+ trx-identities.json, ui-qa rerun observations).
- [ ] F4. Consolidate the three immutable final-domain receipts without overriding any verdict
    - Acceptance: consolidated receipt называет write-set, reused/rerun evidence, residual risks; вердикты F1-F3 не переопределены; machine-readable блок VERDICT по review contract.
    - Evidence: `final/consolidated/final-receipt.md`.

## Commit strategy

Один commit на todo boundary (Todos 5-7 слиты в один); сообщения по шаблону `phase-5(task-N): ...`; allow-list файлов на каждый commit фиксируется в allowed-hunks.json соответствующего task-каталога; F1-F4 фиксируются отдельными control commits после каждого домена; push и merge — вне плана.

## Success criteria

1. `ProjectSession.HydraulicsState` — единственный writable owner значений ST-016, ST-017, ST-018, ST-019; guard suite доказывает отсутствие bypass-писателей.
2. `INV-005` переведён в verified; карты ST-строки обновлены; widget перегенерирован детерминированно.
3. Wire compatibility доказана немодифицированными ProjectRoundTripTests + byte-level round trip.
4. Поведенческие счётчики фазы 4 (Behavioral Counts) выполняются без изменений.
5. Все семь ворот фазы: targeted tests; integration tests; architectural invariant checks; widget/evidence refresh; `dotnet build` (Debug+Release 0/0); полный `dotnet test` failed=0 без новых NotExecuted; agent-operated прогон затронутых user flows.
6. F1-F4 APPROVE и consolidated receipt записан; результат передаётся владельцу для отдельного result acceptance gate — исполнение и приёмка раздельны.
