# fix-thermal-to-hydraulics-sync - Work Plan

## TL;DR (For humans)

**Что получите:** После изменения параметров во вкладке «Тепловой расчёт» (труба, шаг, температура подачи и т.п.) и нажатия «Рассчитать» вкладка «Гидравлический расчёт» полностью актуализирует блок «Данные укладки и мощности» (труба, диаметры, шаг, температуры, мощности) и пересчитывает контуры. Сейчас этот блок остаётся старым из-за неполного обработчика контекстной шины.

**Почему этот подход:** Рефакторинг `refactor-dedupe-params` перевёл взаимодействие модулей на `CalculationContext`, но обработчик `CircuitsViewModel.OnCalculationContextChanged` получился неполным: на `ThermalResult` он вызывает только `Calculate()` без `NotifyThermalPropertiesChanged()`, а на `ThermalInputs` не реагирует вовсе. Правильный путь (`UpdateFromThermalModule`) остался только для загрузки проекта. Фикс делает `CircuitsViewModel` чистым потребителем контекста: читает, обновляет UI и пересчитывает, но не пишет обратно тепловые данные в runtime-потоке.

**Что НЕ будет сделано:**
- НЕ переписывается `UpdateFromThermalModule` и не меняется загрузка проекта.
- НЕ подключается мёртвое поле `CalculationContext.HydraulicsResults` / `UpdateHydraulics` (отдельный долг).
- НЕ добавляется обработка невалидного/`Reset` теплового расчёта (invalid result не публикуется в контекст текущим кодом — это отдельная история).
- НЕ меняются формулы/константы калькуляторов.
- НЕ меняется `ColdFiveDayTemperature`/`AirTemperature` источник (это уже починено в `fix-design-temperature-source`).

**Усилия:** S — 2 шага, 1 файл правки + 1 файл тестов.
**Риск:** Low — минимальное изменение обработчика событий; контролируется новым интеграционным тестом и ручным QA.

**Решения, принятые за вас (любое можно отклонить одной строкой):**
- D1 — событие `ThermalInputs` => только `NotifyThermalPropertiesChanged()`, без `Calculate()` (результат ещё может быть старым). Принято: в валидном пути `ThermalViewModel.Calculate` вызовет оба события (`ThermalInputs` + `ThermalResult`), поэтому `NotifyThermalPropertiesChanged` сработает дважды — это безвредно (WPF дедуплицирует), но гарантирует актуализацию UI уже на этапе входов.
- D2 — событие `ThermalResult` => `NotifyThermalPropertiesChanged()` + `Calculate()`.
- D3 — `UpdateFromThermalModule` не трогать; он остаётся для явного пуша (загрузка проекта/тесты) и пишет в контекст с `source = "CircuitsViewModel"`, который обработчик игнорирует.
- D4 — не подключать `HydraulicsResults`/`UpdateHydraulics` (отдельный план).
- D5 — invalid/Reset thermal вне скоупа.
- D6 — тестирование TDD: сначала failing-тест, потом фикс.

**Маршрутизация:** подход уже согласован с пользователем. После плана — обычный approval gate.

**Ваш следующий шаг:** план проходит обязательную Metis-ревью; после безусловного OK можно запускать worker-сессию через `$start-work`. Исполнения не начинаю сам.

---

> TL;DR (machine): S effort, Low risk, 2 todos / 1 wave, bugfix making CircuitsViewModel a pure consumer of CalculationContext thermal events (ThermalInputs + ThermalResult) by adding NotifyThermalPropertiesChanged, with regression test for the real context path.

## Scope
### Must have
- B1 `CircuitsViewModel.OnCalculationContextChanged` обрабатывает `ThermalInputs` и `ThermalResult` от источника `Thermal` (и любого, кроме `CircuitsViewModel`): для `ThermalInputs` — `NotifyThermalPropertiesChanged()`; для `ThermalResult` — `NotifyThermalPropertiesChanged()` + `Calculate()`.
- B2 Интеграционный тест реального пути: `CalculationContext.UpdateThermalInputs/UpdateThermal` с `source="Thermal"` => в `CircuitsViewModel` вызываются `PropertyChanged` для тепловых свойств (`PowerUp`, `SupplyTemperature`, `PipeType`, `PipeSpacing_cm`) и выполняется расчёт (`circuit.Power` обновляется).
- B3 Существующие тесты `UpdateFromThermalModule_*`, `DoubleCalculationPreventionTests.*`, `ClimateToHydraulicsIntegrationTests.*` и `PipeSpacingSynchronizationTests` остаются зелёными (явный пуш и контекстный путь не сломаны).
- B4 Количество вызовов `Calculate()` на один `PushThermalResultToContext` (`UpdateThermalInputs` + `UpdateThermal`) не увеличивается — остаётся ровно 1 (notify на `ThermalInputs` не вызывает `Calculate`).
- B5 Сборка и весь набор тестов зелёные.

### Must NOT have (guardrails)
- NO увеличения количества `Calculate()` вызовов на один тепловой пересчёт (должно остаться 1).
- NO обратной записи из `CircuitsViewModel` в `CalculationContext.ThermalResult`/`ThermalInputs` в пути обработки событий (писатель остаётся только `ThermalViewModel`/загрузка проекта).
- NO изменений `UpdateFromThermalModule` (сигнатура, поведение, вызовы).
- NO изменений `Calculate()`, `ThermalViewModel.Calculate()`, формул калькуляторов.
- NO подключения `HydraulicsResults`/`UpdateHydraulics`.
- NO обработки invalid/Reset thermal.
- NO изменений `baseline_refactor_dedupe.json`, `README v.2.1.md`.

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Build gate: `dotnet build src/SnowMeltingCalculator.csproj -c Debug /p:TreatWarningsAsErrors=true` (0 errors, 0 warnings).
- Test gate: `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj --logger "console;verbosity=normal"` (100% pass).
- Regression gate: новый тест `ThermalResultChangedViaContext_NotifiesThermalPropertiesAndRecalculates` и `ThermalInputsChangedViaContext_NotifiesThermalProperties` проходят; до фикса падают оба (нет `PropertyChanged` — ни `ThermalResult` case, ни `ThermalInputs` case).
- Manual QA: запустить приложение, рассчитать тепло, перейти в гидравлику, вернуться в тепло, изменить трубу/шаг/температуру подачи, пересчитать, проверить, что блок «Данные укладки и мощности» в гидравлике обновился.
- Evidence: `.omo/evidence/fix-thermal-to-hydraulics-sync/task-<N>-*.{txt,log}`.

## Execution strategy
### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 Fix context handler + regression test (TDD) | — | 2 | — |
| 2 Full build+test gate + notepad + manual QA evidence | 1 | F1 | — |

## Todos
> Implementation + Test = ONE todo. Never separate.

- [x] 1. Fix OnCalculationContextChanged + regression test (TDD)
  What to do / Must NOT do:
  Шаг A (failing test сначала):
  - В `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs` добавить два теста:
    1) `ThermalResultChangedViaContext_NotifiesThermalPropertiesAndRecalculates`:
       - Изоляция пути `ThermalResult` (важно для TDD): сначала seed `ThermalInputs` в контекст (`_calculationContext.UpdateThermalInputs(inputs, "Thermal")`) **до** подписки на `PropertyChanged`.
       - Затем подписаться на `_viewModel.PropertyChanged` и собрать список имён.
       - Вызвать только `_calculationContext.UpdateThermal(result, "Thermal")` с валидным `ThermalCalculationResult` (например `PowerUp=300, PowerDown=20, SupplyTemperature=55, ReturnTemperature=40, MeanTemperature=47.5, DeltaT=15, IsValid=true`).
       - Assert: собранный список содержит `nameof(CircuitsViewModel.PowerUp)`, `nameof(CircuitsViewModel.SupplyTemperature)`, `nameof(CircuitsViewModel.PipeType)`, `nameof(CircuitsViewModel.PipeSpacing_cm)`. Это проверяет именно `ThermalResult` case, а не `ThermalInputs` case.
       - Assert: хотя бы один контур выбранного коллектора получил ненулевой `circuit.Power` (или изменился) — подтверждает, что `Calculate()` выполнился.
    2) `ThermalInputsChangedViaContext_NotifiesThermalProperties`:
       - Подписаться на `PropertyChanged`, затем вызвать только `_calculationContext.UpdateThermalInputs(inputs, "Thermal")` (без `UpdateThermal`).
       - Assert: `PropertyChanged` содержит `PipeType`/`PipeSpacing_cm`/`InnerDiameter`.
  - Запустить новые тесты и подтвердить, что `ThermalResultChangedViaContext_*` падает (нет `PropertyChanged` для тепловых свойств при изолированном `UpdateThermal`). Сохранить вывод в `evidence/task-1-pre-fix-failing.log`.
  Шаг B (фикс):
  - В `src/ViewModels/Hydraulics/CircuitsViewModel.cs` метод `OnCalculationContextChanged` (строки 755-771) изменить:
    ```csharp
    private void OnCalculationContextChanged(object? sender, ContextChangedEventArgs e)
    {
        // Игнорировать собственные изменения контекста — Calculate вызывается явно
        if (e.Source == "CircuitsViewModel")
            return;

        switch (e.PropertyName)
        {
            case nameof(CalculationContext.ThermalInputs):
                NotifyThermalPropertiesChanged();
                break;

            case nameof(CalculationContext.ThermalResult):
                NotifyThermalPropertiesChanged();
                Calculate();
                break;

            case nameof(CalculationContext.Climate):
                UpdateFromClimateModule();
                break;
        }
    }
    ```
  - Обновить `<remarks>`/комментарий над методом (строки ~750-754): убрать утверждение, что `ThermalInputs` игнорируется; описать новое поведение (notify на `ThermalInputs`, notify+Calculate на `ThermalResult`).
  - `NotifyThermalPropertiesChanged()` уже существует (строки 879-896) и нотифицирует все нужные свойства. Его тело не менять.
  - НЕ трогать `UpdateFromThermalModule`, `Calculate()`.
  - НЕ добавлять запись в `CalculationContext` из этого обработчика.
  - Запустить новые тесты и подтвердить, что они проходят.
  Parallelization: Wave 1 | Blocked by: — | Blocks: 2
  References: src/ViewModels/Hydraulics/CircuitsViewModel.cs:755-771 (OnCalculationContextChanged), :879-896 (NotifyThermalPropertiesChanged), :397-547 (Calculate), :679-717 (UpdateFromThermalModule — не трогать); src/Core/CalculationContext.cs:274-307 (UpdateThermal/UpdateThermalInputs); src/ViewModels/Thermal/ThermalViewModel.cs:280-296 (Calculate — писатель); tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs:47-149 (Setup), :232-239 (SetThermalInputsInContext), :219-228 (SetupCollectorWithCircuits), :244-265 (пример UpdateFromThermalModule_WhenResultChanges_UpdatesInputData).
  Acceptance criteria (agent-executable): `dotnet test --filter "FullyQualifiedName~ThermalResultChangedViaContext_NotifiesThermalPropertiesAndRecalculates"` exit 0; `dotnet test --filter "FullyQualifiedName~ThermalInputsChangedViaContext_NotifiesThermalProperties"` exit 0; `dotnet test --filter "FullyQualifiedName~DoubleCalculationPreventionTests"` exit 0; `dotnet test --filter "FullyQualifiedName~ClimateToHydraulicsIntegrationTests"` exit 0; `dotnet build src/SnowMeltingCalculator.csproj -c Debug /p:TreatWarningsAsErrors=true` exit 0; `lsp_diagnostics` на `CircuitsViewModel.cs` — 0 новых ошибок; evidence `task-1-pre-fix-failing.log` существует и содержит `Failed: ThermalResultChangedViaContext_*`.
  QA scenarios: happy — после изолированного `UpdateThermal("Thermal")` `PropertyChanged` содержит тепловые свойства и `circuit.Power` обновлён; failure — до фикса тест `ThermalResultChangedViaContext_*` падает по отсутствию `PropertyChanged`. Evidence `.omo/evidence/fix-thermal-to-hydraulics-sync/task-1-*.{txt,log}`.
  Commit: Y | fix(hydraulics-vm): refresh thermal properties on context change

- [x] 2. Full green build + test gate + notepad + manual QA evidence
  What to do / Must NOT do:
  - Запустить `dotnet build src/SnowMeltingCalculator.csproj -c Debug /p:TreatWarningsAsErrors=true` и `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj --logger "console;verbosity=normal"`.
  - Проверить, что существующие тесты `UpdateFromThermalModule_*`, `DoubleCalculationPreventionTests.*`, `ClimateToHydraulicsIntegrationTests.*` и `PipeSpacingSynchronizationTests` проходят.
  - Ручное QA: WPF desktop-приложение, Playwright не применим. Использовать WPF UI-автоматизацию (как в `.omo/evidence/fix-design-temperature-source/f3-manual-qa.txt` — запуск `SnowMeltingCalculator.exe`, перечисление контролов, чтение значений) **или** ручную проверку человеком. Шаги: рассчитать тепло, перейти в гидравлику, вернуться в тепло, изменить трубу/шаг/температуру подачи, пересчитать, убедиться, что блок «Данные укладки и мощности» в гидравлике обновился. Сохранить скриншот/лог в evidence.
  - Дописать `.omo/notepads/refactor-dedupe-params/learnings.md` раздел `## Fix: thermal-to-hydraulics sync` с описанием root cause и фикса.
  - Не подавлять warnings через `#pragma`. Не менять baseline.
  Parallelization: Wave 1 | Blocked by: 1 | Blocks: F1
  References: весь изменённый набор; `.omo/notepads/refactor-dedupe-params/learnings.md`.
  Acceptance criteria: build green; `dotnet test` 100% pass; manual QA подтверждает обновление блока; notepad обновлён.
  QA scenarios: happy — full green + UI обновляется; failure — revert T1 и подтвердить, что тест падает, а UI не обновляется. Evidence `.omo/evidence/fix-thermal-to-hydraulics-sync/task-2-*.{txt,log,png}`.
  Commit: N | (verification gate, no commit)

## Final verification wave
> Runs after ALL todos. ALL must APPROVE.
- [x] F1. Plan compliance audit
  Verify diff touches only `src/ViewModels/Hydraulics/CircuitsViewModel.cs`, `tests/.../ThermalToHydraulicsIntegrationTests.cs`, `.omo/notepads/...`. Tool: `git diff --name-only HEAD -- src/ViewModels/Hydraulics/CircuitsViewModel.cs tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs .omo/notepads` (учитывать dirty worktree от refactor-dedupe-params — изолировать delta через `git stash` при необходимости). Agent: explore.
- [x] F2. Code quality review
  `dotnet format --verify-no-changes` на изменённых файлах + `lsp_diagnostics`. Agent: oracle.
- [x] F3. Manual QA (light)
  Запустить приложение, изменить параметры тепла, пересчитать, проверить обновление блока гидравлики. Agent: unspecified-high.
- [x] F4. Scope fidelity
  Подтвердить: `UpdateFromThermalModule` не изменён; `Calculate()` не изменён; формулы/константы не тронуты; `baseline_refactor_dedupe.json` не изменён. Agent: oracle.

## Commit strategy
- Один коммит на todo с `Commit: Y`; T2 — без коммита.
- Сообщения в Conventional Commits, scope `hydraulics-vm`/`hydraulics`, summary на русском в стиле существующего `git log`.
- Без squash между todo.

## Success criteria
- `OnCalculationContextChanged` содержит case `ThermalInputs` (notify) и `ThermalResult` (notify + Calculate).
- После `UpdateThermal("Thermal")` в `CircuitsViewModel` вызываются `PropertyChanged` для `PowerUp`, `SupplyTemperature`, `PipeType`, `PipeSpacing_cm`.
- `UpdateFromThermalModule` остаётся неизменённым и его тесты зелёные.
- `dotnet build /p:TreatWarningsAsErrors=true` green; `dotnet test` 100% pass.
- В ручном QA блок гидравлики обновляется после изменения и пересчёта тепла.
