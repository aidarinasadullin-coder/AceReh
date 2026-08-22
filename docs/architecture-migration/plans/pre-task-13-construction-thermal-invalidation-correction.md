# pre-task-13-construction-thermal-invalidation-correction - Work Plan

## TL;DR (For humans)

Исправить production-регрессию до Phase 3 Task 13 одним notification-вызовом на существующей canonical completion boundary: после обновления валидной `ConstructionStateProjection` `ProjectSessionConstructionState` поднимает её `DataChanged` ровно один раз для уже разрешённых downstream origins. Существующий `ThermalViewModel` затем сбрасывает имеющийся `Result` и через `CalculationStateService` включает оранжевый индикатор; новый успешный тепловой расчёт гасит его.

План следует TDD: сначала RED на реальном production graph, затем минимальный fix, затем positive/negative/lifecycle gates и обязательный ручной WPF walkthrough самим исполнителем. Task 13, карты/model/widget, `ThermalViewModel`, `CalculationContext`, `CalculationStateService`, DI и `ConstructionViewModel` не изменяются.

## Scope

### In scope

- Репозиторий `D:\IA\ace v.2` с текущим существенно dirty worktree.
- Единственная production boundary: `ProjectSessionConstructionState.CompleteChanged(...)`.
- Существующая read-only projection `ConstructionStateProjection` и её уже реализованный internal `RaiseDataChanged()`.
- Реальный тестовый graph:
  `ProjectSession.ConstructionState -> CurrentProjection -> ThermalViewModel -> CalculationStateService`.
- User и Template mutations; реальные material, thickness и calculated-lambda/override изменения.
- `Result`, `ThermalNeedsRecalculation`, точное количество Thermal `StateChanged` и успешный subsequent recalculation.
- NoChange, Rejected, текущая недостижимость Cancelled, Initialization, ProjectLoad, Reset и отсутствие предыдущего Thermal result.
- Targeted/affected/full Release gates, Debug/Release builds, обязательная ручная WPF-проверка и один короткий correction receipt.
- Фактическое обновление `TASK_CONTEXT.md`, которое оставляет Task 13 заблокированной до полного PASS correction lane и затем разрешает только Task 13 dossier refresh.

### Out of scope / Must-NOT-Have

- Не начинать и не включать сюда Phase 3 Task 13; не менять шесть maps, state inventory, shared architecture model/schema, widget source/generated HTML или architecture presentation artifacts.
- Не менять `ThermalViewModel`, `CalculationContext`, `CalculationStateService`, `ICalculationStateService`, `ServiceCollectionExtensions`, `ConstructionViewModel`, `ProjectLoadOrchestrator`, `MainViewModel` или Results ownership.
- Не возвращать direct `MarkDirty()`, `UpdateConstruction()` или downstream publication в `ConstructionViewModel`.
- Не поднимать событие автоматически из `ConstructionStateProjection.Update()`, не подписывать `ThermalViewModel` на `CalculationContext.ContextChanged` и не создавать второй reactive path.
- Не создавать event bus, coordinator, adapter hierarchy, второй Construction owner или новый public API.
- Не менять formulas, validation policy, `.smc` schema/version, UI/XAML/design, packages, installer или release artifacts.
- Не ослаблять/удалять тесты, не добавлять `[Ignore]`/Skip, не принимать новые failures/`NotExecuted` и не подменять real projection тестовым `ConstructionData`.
- Не выполнять `git restore`, `checkout`, `reset`, `clean`, stage, commit, amend, push или перезапись чужих dirty hunks.

### Exact allow-list

**Production**

- `src/Services/Project/ProjectSessionConstructionState.cs`

`src/Services/Project/ConstructionStateProjection.cs` является reference-only: `RaiseDataChanged()` уже существует; его изменение запрещено без отдельного owner allow-list amendment.

**Tests**

- Новый файл `tests/SnowMeltingCalculator.Tests/Services/Project/ConstructionThermalInvalidationRegressionTests.cs`.

Существующие test files используются read-only. Если самодостаточный fixture в новом файле объективно невозможен, исполнитель останавливается до production/test edits и запрашивает узкое owner amendment с точным дополнительным путём; переносить fixture в существующий dirty test file самостоятельно нельзя.

**Evidence/context**

- `docs/architecture-migration/evidence/phase-3-construction-state/pre-task-13-construction-thermal-invalidation-correction.md`
- Correction-specific raw `.trx`/logs under `tests/SnowMeltingCalculator.Tests/TestResults/`; имена должны начинаться с `pre-task-13-construction-thermal-`.
- `docs/architecture-migration/TASK_CONTEXT.md`

Никакие другие пути не входят в write-set.

### Canonical notification decision

В `ProjectSessionConstructionState.CompleteChanged(...)` сохранить существующий порядок верхнего уровня и изменить только валидную downstream branch:

```csharp
_projection.Update(newSnapshot);

if (_projection.IsValid && PublishesDownstream(origin))
{
    _projection.RaiseDataChanged();
    _calculationContext?.UpdateConstruction(_projection, "ConstructionState");
}

Changed?.Invoke(...);
// existing origin-aware dirty behavior
```

Почему boundary canonical и origin-safe:

- `CompleteChanged` вызывается только после реально принятого `Changed` mutation result; NoChange и Rejected сюда не доходят.
- `PublishesDownstream` сохраняется без изменения и разрешает только `User`, `Template`, `FileLoad`; lifecycle origins остаются silent для Thermal invalidation.
- `_projection.IsValid` сохраняет принятый Task 10 invalid-state contract.
- Вызов события стоит до `CalculationContext.UpdateConstruction`, поэтому typed projection notification предшествует compatibility context publication; затем сохраняются существующие state `Changed` и dirty semantics.
- `ThermalViewModel.OnConstructionDataChanged` остаётся единственным consumer path для этого индикатора: при `Result != null` он очищает result и один раз вызывает `SetThermalNeedsRecalculation`; без result только обновляет derived UI properties.
- Successful `ThermalViewModel.Calculate` сохраняет существующий `ResetThermalState()` и переводит индикатор обратно в Actual.

## Verification strategy

### Test strategy

- **TDD обязателен.** Сначала новый real-graph test должен завершиться RED исключительно потому, что `CurrentProjection.DataChanged` не поднимается после accepted Construction mutation.
- До production fix сохранить RED command, exit code, failing test identity и assertion в correction receipt; компиляционная ошибка, искусственно поднятое событие или test double не считаются требуемым RED.
- После fix тот же тест должен стать GREEN без изменения ожидаемого поведения.

### Real-graph fixture contract

Новый fixture должен самостоятельно создать согласованный production graph с одним `ProjectSession`/`ProjectSessionConstructionState`, его `CurrentProjection`, concrete `CalculationStateService` на той же session, concrete `ThermalViewModel`, real `CalculationContext` и минимальными детерминированными calculator/validator/climate collaborators. Запрещено:

- вручную вызывать `CurrentProjection.DataChanged`/`RaiseDataChanged`;
- использовать `ConstructionData` вместо `CurrentProjection`;
- mock-ить `ICalculationStateService.SetThermalNeedsRecalculation` вместо проверки concrete service;
- вызывать private `OnConstructionDataChanged` напрямую.

Каждый positive scenario сначала создаёт/загружает существующий `ThermalCalculationResult`, удостоверяется, что `ThermalNeedsRecalculation == false`, подписывается на concrete `CalculationStateService.StateChanged`, выполняет одну logical Construction mutation и утверждает:

- mutation result `Changed` с ожидаемым origin;
- `ThermalViewModel.Result == null`;
- `ThermalNeedsRecalculation == true`;
- ровно один Thermal `StateChanged` с `ModuleState.NeedsRecalculation`;
- projection/context publication multiplicity не превышает одну на logical completion.

### Required scenario matrix

- Parameterized User edit rows: material replacement, thickness change, calculated lambda/manual override change. Использовать реальные `ConstructionMutation` operations/snapshot transitions, а не прямую мутацию projection.
- Template apply row: один full-snapshot `Template` mutation с несколькими структурными изменениями, но одним Thermal `StateChanged`.
- No-result row: accepted User change поднимает projection notification, но при `Result == null` оставляет `ThermalNeedsRecalculation == false` и Thermal `StateChanged == 0`.
- NoChange row: structurally equal candidate; result сохраняется, flag false, events zero.
- Rejected row: invalid candidate; result сохраняется, flag false, events zero.
- Lifecycle rows: changed `Initialization`, `ProjectLoad`, `Reset`; projection values обновляются, но Thermal result сохраняется, flag false, Thermal `StateChanged == 0`.
- Cancelled contract: текущий production state API не конструирует `ConstructionMutationStatus.Cancelled`. Добавить в новый regression file deterministic source-inventory assertion, сканирующий production `src/**/*.cs` и доказывающий отсутствие `new ConstructionMutationResult(ConstructionMutationStatus.Cancelled, ...)`/эквивалентного return path. Receipt обязан сформулировать это как текущую недостижимость до `CompleteChanged`, а не как выполненный fake mutation. Если production Cancelled path уже появился к моменту исполнения, source assertion должна RED и исполнитель обязан заменить её real-graph cancelled scenario в том же новом test file, не расширяя production scope.
- Successful recalculation row: после positive invalidation выполнить реальный `CalculateCommand` с детерминированным successful calculator; результат снова non-null, `ThermalNeedsRecalculation == false`, финальный Thermal state `Actual`. Считать отдельно ожидаемые `NeedsRecalculation`, `Calculating`, `Actual`, не смешивая их с требованием «ровно один StateChanged» для исходной mutation.

### Build/test conventions

- Все команды запускать из `D:\IA\ace v.2`.
- Builds: `dotnet build "src\SnowMeltingCalculator.csproj" -c Debug --nologo` и `-c Release --nologo`; ожидать exit `0`, `0 warnings`, `0 errors`.
- Tests: `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c <Configuration> --no-build --filter "..." --logger "trx;LogFileName=<name>.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"`.
- TRX parsing must record total/executed/passed/failed/notExecuted and exact identities of every `NotExecuted`; console totals alone are insufficient.
- Current accepted full Release identities are exactly `RegenerateCircuitsBaseline`, `RegenerateBaseline`, and `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`. Any additional failure or `NotExecuted` blocks completion.

### Manual WPF contract

Автоматические tests не заменяют owner-required ручную проверку. Сам исполнитель должен запустить собранное WPF-приложение и выполнить exact walkthrough из Todo 4. Скриншоты не обязательны, но receipt обязан назвать build/configuration, способ запуска, каждое действие, observed indicator state и PASS/FAIL. Невозможность реально запустить или наблюдать UI — blocker; нельзя заменить это утверждением тестов.

## Execution strategy

- Одна последовательная correction lane; параллельные production/test edits запрещены.
- Перед первым edit получить `git rev-parse --show-toplevel`, HEAD/branch/upstream и NUL-safe `git status --porcelain=v1 -z --branch`. Для каждого allow-listed pre-existing dirty/untracked path сохранить binary-safe status и SHA-256/preimage, достаточные для доказательства, что чужие hunks не потеряны.
- После каждого todo сравнивать write-set с exact allow-list. Не использовать Git rollback; исправлять только task-owned hunks минимальным inverse patch.
- Task 13 остаётся blocked на всём протяжении Todos 1-3. Только Todo 4 после всех automated/manual PASS меняет `TASK_CONTEXT.md` так, чтобы следующим разрешённым шагом стал исключительно Task 13 dossier refresh; F1-F4 Phase 3 остаются unstarted.
- Stop conditions: неожиданная необходимость менять любой production/test path вне allow-list; duplicate event после single completion; lifecycle-origin notification; изменение current no-result contract; новый failure/skip; невозможность ручного WPF walkthrough; drift чужих hunks. При stop создать/обновить только correction receipt и factual `TASK_CONTEXT.md`, затем вернуть owner варианты.

## Todos

- [ ] 1. Real-graph RED: зафиксировать dirty boundary и добавить focused regression fixture — expect failing end-to-end Construction notification without synthetic events
  - Depends on: owner approval этого correction plan и отдельный worker start; Phase 3 Task 13 не начат.
  - References:
    - `AGENTS.md`; `docs/architecture-migration/AGENTS.md`; `docs/architecture-migration/TASK_CONTEXT.md`.
    - `docs/architecture-migration/plans/phase-3-construction-state.md`, Tasks 10, 12.1/13 and F2-F4 constraints.
    - `src/Configuration/ServiceCollectionExtensions.cs`: production `IConstructionData -> CurrentProjection` identity, read-only reference.
    - `src/ViewModels/Thermal/ThermalViewModel.cs`: constructor subscription and `OnConstructionDataChanged` behavior, read-only reference.
    - `src/Services/Navigation/CalculationStateService.cs`: exact flag/event semantics, read-only reference.
    - `tests/SnowMeltingCalculator.Tests/Thermal/ThermalViewModelTests.cs`: legacy synthetic-event coverage that the new test must not duplicate.
  - Action:
    1. Capture root/HEAD/branch/upstream, NUL-safe status, staged set and per-allow-listed preimage/hash without changing Git state.
    2. Create only `ConstructionThermalInvalidationRegressionTests.cs` with a self-contained real-graph fixture.
    3. First implement a representative User thickness-change scenario with a previous result and concrete state service; add the full scenario skeleton only if it compiles without weakening the initial assertion.
    4. Run the focused Release test before production change and record the expected RED caused specifically by missing end-to-end notification.
  - Acceptance criteria:
    - Focused test compiles and executes against `CurrentProjection`; it never raises `DataChanged` manually and never constructs `ConstructionData` as the injected construction source.
    - RED failure states that result remained non-null and/or `ThermalNeedsRecalculation`/StateChanged did not occur after a `Changed` User mutation.
    - Production files remain byte-identical during RED.
    - Staged set and unrelated status remain unchanged.
  - QA happy scenario: run `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --filter "FullyQualifiedName~ConstructionThermalInvalidationRegressionTests.UserMutation_WithExistingResult_InvalidatesThermalOnce" --logger "trx;LogFileName=pre-task-13-construction-thermal-red.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"`; expect nonzero with the intended assertion and write exact output/TRX identity into the correction receipt.
  - QA failure scenario: if compile fails, mutation is Rejected/NoChange, fixture uses a synthetic event, or failure originates elsewhere, it is not accepted RED; repair only the new test file and rerun. If repair requires another path, stop for allow-list amendment.
  - Evidence: correction receipt sections `Baseline boundary` and `TDD RED`; raw `pre-task-13-construction-thermal-red.trx`/log.
  - Commit guidance: no commit/stage/push; if later explicitly authorized, pair RED test and fix in one atomic correction commit.

- [ ] 2. Canonical production fix and complete contract matrix: emit projection notification once inside accepted downstream completion — expect User/Template invalidation and silent negative/lifecycle paths
  - Depends on: Todo 1 accepted RED.
  - References:
    - `src/Services/Project/ProjectSessionConstructionState.cs:CompleteChanged` and `PublishesDownstream`.
    - `src/Services/Project/ConstructionStateProjection.cs:Update` and `RaiseDataChanged`, reference-only.
    - `src/Models/Climate/ClimateData.cs:ApplyProjection` and `src/Services/Project/ProjectSessionClimateState.cs` as precedent for projection notification, reference-only.
    - `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionConstructionStateTests.cs`, `ConstructionStateLegacyStoreGuardTests.cs`, and `CanonicalDefaultConstructionLifecycleTests.cs`, read-only regression references.
  - Action:
    1. In the existing `if (_projection.IsValid && PublishesDownstream(origin))` block, insert `_projection.RaiseDataChanged();` immediately before `_calculationContext?.UpdateConstruction(...)`; change nothing else.
    2. Complete parameterized User rows for material, thickness and calculated-lambda/override; each starts with a result and asserts one invalidation.
    3. Add one multi-field Template snapshot row asserting one completion/one invalidation.
    4. Add NoChange, Rejected, no-result, Initialization, ProjectLoad and Reset rows plus Cancelled source-inventory contract.
    5. Assert typed `DataChanged` precedes context `ContextChanged(Construction)` for a positive row, and both occur once; lifecycle rows may update state/projection but produce neither downstream event.
  - Acceptance criteria:
    - Production diff is exactly one executable call inside the existing branch, plus at most a clarifying comment; no condition/origin/dirty/context logic changes.
    - User material/thickness/lambda and Template rows each clear `Result`, set flag true and emit exactly one `NeedsRecalculation` StateChanged.
    - NoChange/Rejected preserve result and emit zero; lifecycle changed origins preserve result and emit zero downstream notifications.
    - No-result accepted User change keeps flag false and emits zero Thermal StateChanged.
    - Cancelled assertion truthfully proves current production path cannot reach completion; no fake result is used.
    - Existing state `Changed`, context publication and dirty semantics remain at their Task 10 multiplicities.
  - QA happy scenario: run Release filter `FullyQualifiedName~ConstructionThermalInvalidationRegressionTests|FullyQualifiedName~ProjectSessionConstructionStateTests|FullyQualifiedName~ConstructionStateLegacyStoreGuardTests|FullyQualifiedName~CanonicalDefaultConstructionLifecycleTests|FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ThermalViewModelTests|FullyQualifiedName~CalculationStateServiceTests`; expect exit `0`, zero failed, and no new `NotExecuted`. Store `pre-task-13-construction-thermal-contracts.trx`.
  - QA failure scenario: a second StateChanged, lifecycle indicator, no-result indicator, changed dirty/context count, or guard failure rejects the fix. Correct only the one production file/new test file; never suppress the assertion or expand into downstream consumers.
  - Evidence: correction receipt sections `Production diff` and `Contract matrix`; raw contracts TRX/log.
  - Commit guidance: no commit/stage/push.

- [ ] 3. Recalculation and executable release gates: prove indicator reset and no affected regressions — expect targeted, affected, builds and full Release PASS
  - Depends on: Todo 2 GREEN.
  - References:
    - `src/ViewModels/Thermal/ThermalViewModel.cs:Calculate`: `SetThermalCalculating()` then `ResetThermalState()`.
    - `docs/architecture-migration/evidence/phase-3-construction-state/task-12-executable-gates.md` and `task-12-1-canonical-default-construction-initialization.md` for live gate conventions and accepted `NotExecuted` identities.
    - Parent plan Task 12 and F2/F3 filters.
  - Action:
    1. Add/finish successful recalculation scenario in the new test file using deterministic calculator/validators.
    2. Run focused correction Release suite.
    3. Run affected Phase 3 Debug matrix covering Construction, Thermal, CalculationState, lifecycle/reset, Results and DI tests using live class names.
    4. Run Debug and Release production builds.
    5. Run full Release suite with TRX and reconcile all counters/`NotExecuted` identities.
  - Acceptance criteria:
    - After mutation, observed sequence includes one `NeedsRecalculation`; after `CalculateCommand`, result is non-null, flag false and final Thermal state `Actual`.
    - Debug/Release builds exit `0` with zero warnings/errors.
    - Focused and affected suites have zero failed and no new `NotExecuted`.
    - Full Release exits `0`; only the three baseline-accepted identities may be `NotExecuted`.
    - Final Git/status comparison shows only allow-listed task-owned changes and no staged or unrelated-hunk drift.
  - QA happy scenario:
    1. `dotnet build "src\SnowMeltingCalculator.csproj" -c Debug --nologo`.
    2. `dotnet build "src\SnowMeltingCalculator.csproj" -c Release --nologo`.
    3. Focused Release test filter from Todo 2 plus recalculation row, output `pre-task-13-construction-thermal-focused-release.trx`.
    4. Affected Debug filter discovered from current Task 12.1 receipt and live classes, output `pre-task-13-construction-thermal-affected-debug.trx`.
    5. `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --no-build --logger "trx;LogFileName=pre-task-13-construction-thermal-full-release.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"`.
  - QA failure scenario: any build warning/error, failed test, extra `NotExecuted`, order-sensitive rerun discrepancy or protected drift blocks manual QA and Task 13. Record the exact root cause; only correction-caused failures may be fixed within the allow-list.
  - Evidence: correction receipt sections `Automated gates` and `TRX reconciliation`; all named raw logs/TRX.
  - Commit guidance: no commit/stage/push.

- [ ] 4. Manual WPF walkthrough and correction handoff: verify real orange indicator and record evidence — expect Task 13 alone to become the next allowed action
  - Depends on: Todo 3 fully GREEN.
  - References:
    - Owner-required walkthrough in this correction request.
    - `src/ViewModels/Shell/MainViewModel.cs` indicator consumption, read-only.
    - Parent Phase 3 plan Task 13 and `TASK_CONTEXT.md` workflow rules.
  - Action:
    1. Launch the Debug or Release WPF application from the verified build; record exact command/executable/configuration.
    2. On startup/new project, confirm no false orange Thermal indicator.
    3. Complete a thermal calculation; confirm a visible result exists and the indicator is off.
    4. Return to Construction and separately perform: apply a different template; replace a material; change thickness; change calculated lambda/manual override. For each action, confirm the orange indicator appears beside «Тепловой расчет».
    5. After each action, open Thermal, recalculate, confirm a new result exists and indicator disappears before proceeding to the next independent scenario.
    6. Load a project and confirm project load itself creates no false indicator; if needed repeat new-project startup separately so startup/new/load are each explicitly observed.
    7. Record exact actions, values chosen where practical, observed result/indicator states and PASS/FAIL in the correction receipt.
    8. Only after automated and manual PASS update `TASK_CONTEXT.md`: record the correction, links/counters, keep Phase 3 `executing`/acceptance pending/F1-F4 unstarted, and set next action to parent Task 13 dossier refresh only.
  - Acceptance criteria:
    - All four Construction changes visibly turn the indicator on after an existing result.
    - Every successful recalculation visibly turns it off.
    - Startup, new project and project load do not leave a false indicator.
    - Receipt ends with `VERDICT: PASS`, includes exact manual observations and automated evidence links, and does not claim Task 13/F-wave/Phase 3 completion.
    - Final allow-list/status audit finds no unexpected path, staging or unrelated hunk loss.
  - QA happy scenario: execute the seven-step walkthrough above in one recorded application session or clearly identified sessions; capture a concise table `scenario / action / prior result / indicator after action / result after recalc / indicator after recalc / verdict`.
  - QA failure scenario: inability to launch/observe UI, any missing indicator, indicator that persists after successful calculation, or false startup/new/load indicator is `FAIL`; do not document around it or start Task 13. Record blocker and return to the owner.
  - Evidence: final correction receipt and factual `TASK_CONTEXT.md` transition.
  - Commit guidance: no commit/stage/push; repository import/commit remains owner-controlled.

## Final verification wave

- [ ] F1. Plan and allow-list compliance audit — expect exact one-call production diff and zero Task 13 scope
  - Verify every changed path maps to the allow-list; production executable diff is the single `RaiseDataChanged()` call in the specified branch/order; no downstream consumer, maps/model/widget, schema/UI/package file changed.
  - Reconcile NUL-safe baseline/final status and staged set. Any unexplained path or lost pre-existing hunk is `REJECT`.
  - Record verdict in the final section of the correction receipt; do not create a separate architecture F-wave artifact.

- [ ] F2. Reactive contract and multiplicity audit — expect one canonical notification and silent negative/lifecycle paths
  - Independently inspect the real-graph test and TRX: no manual event, no `ConstructionData` replacement, concrete state service, User/Template/material/thickness/lambda coverage, exactly one mutation `NeedsRecalculation`, no-result zero, NoChange/Rejected zero, lifecycle zero, truthful Cancelled reachability proof, successful recalculation Actual.
  - Confirm `Update()` itself remains silent and `ThermalViewModel` remains unsubscribed from `CalculationContext.ContextChanged`.
  - Any duplicate path or untested mandatory row is `REJECT`.

- [ ] F3. Executable and manual evidence audit — expect reproducible green gates plus observed WPF behavior
  - Recheck Debug/Release build logs and focused/affected/full TRX counters, exact accepted `NotExecuted` identities, and manual walkthrough table.
  - Manual statements without launch command/configuration and per-scenario observations are insufficient. Any automated/manual mismatch is `REJECT`.

- [ ] F4. Workflow fidelity audit — expect correction PASS releases only Task 13
  - Verify correction receipt ends PASS and `TASK_CONTEXT.md` records Phase 3 still `executing`, result acceptance pending, parent F1-F4 unstarted, and Task 13 as the only next action.
  - Verify no Task 13 dossier artifact was changed by this lane. Any completion/acceptance claim or premature dossier refresh is `REJECT`.

## Commit strategy

- Planning does not commit. Execution also performs no stage/commit/push unless the owner separately requests it after PASS.
- If a commit is later explicitly authorized, use one atomic correction commit containing only:
  - `ProjectSessionConstructionState.cs` minimal fix;
  - the new regression test file;
  - correction receipt/raw evidence policy as authorized;
  - factual `TASK_CONTEXT.md` update.
- Suggested message: `fix(construction): restore thermal invalidation notification`.
- Before any authorized commit, inspect the staged diff and reject every non-allow-listed path; never include pre-existing unrelated hunks.

## Success criteria

- Exactly one canonical path exists:
  `changed valid User/Template Construction completion -> CurrentProjection.DataChanged -> ThermalViewModel -> Result null -> ThermalNeedsRecalculation true -> MainViewModel orange indicator`.
- User material, thickness and lambda/override edits and Template apply pass on the real production graph with one mutation `StateChanged` each.
- NoChange, Rejected and currently unreachable Cancelled do not enable the indicator; Initialization, ProjectLoad and Reset update canonical/projection state silently.
- Without a previous Thermal result, current no-indicator semantics remain unchanged.
- Successful recalculation creates a new result and returns Thermal state/indicator to Actual/off.
- Targeted/affected tests, Debug/Release builds and full Release gate pass with no new failures, warnings or `NotExecuted` identities.
- The executor's real WPF walkthrough passes all requested scenarios and is recorded with exact actions/results.
- Final write-set is exactly allow-listed; no чужие dirty changes are reverted, overwritten, staged, committed or pushed.
- Task 13 remains separate and starts only after this correction PASS; it may document the restored flow but never carry the production fix.
