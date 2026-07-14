# fix-design-temperature-source - Work Plan

## TL;DR (For humans)

**Что получите:** Гидравлический расчёт холодного пуска («Расчётная температура») снова будет использовать расчётную температуру наружного воздуха по таблице 1.6 СП 131.13330.2025 (`AirTemperature`), а не температуру холодной пятидневки (`ColdFiveDayTemperature`). Для Москвы это −10 °C вместо −23 °C; для всех городов — корректный автовыбор (M10/M15/M20) и ручное переопределение.

**Почему этот подход:** Баг внесён при рефакторинге `refactor-dedupe-params` (T13/T15) — миграция `CircuitsViewModel` на контекстную шину `CalculationContext` перепутала источник и взяла `ColdFiveDayTemperature` вместо `AirTemperature`. `ResultsViewModel` мигрировал правильно, `ClimateViewModel` хранит оба поля раздельно. Достаточно точечно переключить источник в `CircuitsViewModel` и закрыть регрессией с выбранным городом.

**Что НЕ будет сделано:**
- Не меняются формулы и константы в калькуляторах.
- Не меняется семантика `ColdFiveDayTemperature` (остаётся информационным полем для отображения).
- Не трогается `README v.2.1.md` (это спецификация).
- Не пересматривается `baseline_refactor_dedupe.json` (баг в VM, baseline-тесты вызывают калькулятор напрямую).

**Усилия:** S — 4 шага, 2 файла правки + 1 файл тестов.
**Риск:** Low — изменение одного свойства-источника; контролируется новыми тестами со выбранным городом.

**Решения, принятые за вас (любое можно отклонить одной строкой):**
- D1 — `DesignTemperature` в `CircuitsViewModel` = `CalculationContext.AirTemperature` (fallback 0.0 при отсутствии климата).
- D2 — fallback при отсутствии теплового результата тоже использует `AirTemperature` (раньше захардкожено −28).
- D3 — базовый JSON не меняется; регрессия добавляется в интеграционные тесты.

**Маршрутизация:** я трактую ваш бриф как открытый и выбрал best-practice дефолты. Если вы имели в виду более узкий или более широкий исход — скажите в одну строку, и я переключусь на уточняющие вопросы.

**Ваш следующий шаг:** план автоматически проходит двойную high-accuracy рецензию (Momus + Oracle); после их безусловного APPROVE он готов к запуску worker-сессии через `$start-work` (или Atlas). Исполнения не начинаю сам — это работа worker-сессии.

---

> TL;DR (machine): S effort, Low risk, 4 todos / 1 wave, behavior-fix switching CircuitsViewModel.DesignTemperature source from ColdFiveDayTemperature to AirTemperature per README table 1.6.

## Scope
### Must have
- B1 `CircuitsViewModel.DesignTemperature` (и `DesignTemperatureValue`) читает `CalculationContext.AirTemperature`, а не `ColdFiveDayTemperature`.
- B2 В `CircuitsViewModel.Calculate()` расчётная температура для холодного пуска (`designTemp`) берётся из `CalculationContext.AirTemperature`; fallback при отсутствии теплового результата тоже из `AirTemperature` (не −28).
- B3 Регрессионные тесты со **выбранным городом**: Москва (T5Days092=−23 → −10), Сочи (−5 → −10), условный город (−30 → −15), Норильск (−42 → −20), повышенные требования (→ −20) — `DesignTemperatureValue` == `AirTemperature`, а `ColdFiveDayTemperature` == T5Days092.
- B4 Противоречивые assertion-сообщения в `ClimateToHydraulicsIntegrationTests` исправлены (убрано «должен возвращать ColdFiveDayTemperature»).

### Must NOT have (guardrails)
- NO изменений в `ThermalCalculator.cs`, `CircuitsCalculator.cs` (формулы и константы неприкосновенны).
- NO изменений `ColdFiveDayTemperature` как информационного поля и его отображения.
- NO изменений `baseline_refactor_dedupe.json`.
- NO изменений `README v.2.1.md` или других документов.
- NO переименования публичных свойств (`DesignTemperature`, `DesignTemperatureValue`) — только источник.
- NO расширения скоупа на другие модули.

## Verification strategy
> Zero human intervention - all verification is agent-executed.
- Build gate: `dotnet build src/SnowMeltingCalculator.csproj -c Debug /p:TreatWarningsAsErrors=true` (0 errors, 0 warnings).
- Test gate: `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj --logger "console;verbosity=normal"` (100% pass).
- LSP diagnostics gate: `lsp_diagnostics` на каждом изменённом src/ файле — ни одного нового `error`.
- Grep gate: `grep -n "ColdFiveDayTemperature" src/ViewModels/Hydraulics/CircuitsViewModel.cs` → 0 совпадений (поле больше не используется в гидравлике).
- README conformance: для Москвы (T5Days092=−23) `DesignTemperatureValue` == −10; для Норильска (−42) == −20; для «повышенные требования» == −20.
- Evidence: `.omo/evidence/fix-design-temperature-source/task-<N>-*.{txt,log}`.

## Execution strategy
### Dependency matrix
| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 DesignTemperature source fix | — | 2,3,4 | — |
| 2 Regression tests (city selected) | 1 | 4 | 3 |
| 3 Fix contradictory assertions | 1 | 4 | 2 |
| 4 Full build+test gate + notepad | 1,2,3 | F1 | — |

## Todos
> Implementation + Test = ONE todo. Never separate.

- [x] 1. Fix DesignTemperature source in CircuitsViewModel
  What to do / Must NOT do: В `src/ViewModels/Hydraulics/CircuitsViewModel.cs`:
  (a) Строка 176: заменить `public double DesignTemperature => _calculationContext.Climate?.ColdFiveDayTemperature ?? 0.0;` на `public double DesignTemperature => _calculationContext.AirTemperature;` (свойство `CalculationContext.AirTemperature` уже существует, `src/Core/CalculationContext.cs:114`, и возвращает `Climate?.AirTemperature ?? 0`).
  (b) Строки 173-174 XML-комментарий: заменить «Берётся из IClimateData.ColdFiveDayTemperature.» на «Берётся из IClimateData.AirTemperature (расчётная температура по таблице 1.6 СП 131.13330.2025).»
  (c) В `Calculate()` (строки 385, 391, 401): заменить локальную переменную `coldFiveDayTemperature` на `designTemperature`, источник `_calculationContext.AirTemperature` (например `double designTemperature = _calculationContext.AirTemperature;`). В fallback-блоке при `thermalResult == null` (строка 391) убрать `coldFiveDayTemperature = -28;` и оставить `designTemperature` из `AirTemperature` (fallback уже учтён в свойстве `CalculationContext.AirTemperature`). Строка 401: `double designTemp = designTemperature;` (или использовать переменную напрямую).
  (d) Обновить `designResult` вызов (строки 469-476) — параметр `designTemp` уже равен `designTemperature`, изменений формулы нет.
  Не трогать `ThermalCalculator`, `CircuitsCalculator`, `ColdFiveDayTemperature` поле/отображение, `baseline_refactor_dedupe.json`.
  Parallelization: Wave 1 | Blocked by: — | Blocks: 2,3,4
  References: src/ViewModels/Hydraulics/CircuitsViewModel.cs:176,173-174,385,391,401,469-476; src/Core/CalculationContext.cs:114; src/ViewModels/Results/ResultsViewModel.cs:1015 (эталон правильного источника).
  Acceptance criteria (agent-executable): `grep -n "ColdFiveDayTemperature" src/ViewModels/Hydraulics/CircuitsViewModel.cs` → 0; `grep -n "AirTemperature" src/ViewModels/Hydraulics/CircuitsViewModel.cs` содержит `DesignTemperature` getter и `Calculate()`; `dotnet build src/SnowMeltingCalculator.csproj -c Debug /p:TreatWarningsAsErrors=true` exit 0; `lsp_diagnostics` на `CircuitsViewModel.cs` — 0 новых ошибок.
  QA scenarios: happy — для Москвы `DesignTemperatureValue` == −10 (см. T2); failure — временно вернуть `ColdFiveDayTemperature` и подтвердить, что новый тест T2 падаёт с −23 ≠ −10. Evidence `<attemptDir>/task-1-fix-design-temperature-source.txt`.
  Commit: Y | fix(hydraulics-vm): use AirTemperature as design temperature source per table 1.6

- [x] 2. Add regression tests with a selected city (all zones + high requirements)
  What to do / Must NOT do: Добавить в `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs` (или в `CircuitsViewModelTests.cs`, если там удобнее создавать `CircuitsViewModel` с реальным `CalculationContext`) тест-кейс «DesignTemperatureValue_FollowsAirTemperature_WithCitySelected», который:
  - Создаёт `CityInfo` для нескольких сценариев и присваивает `_climateViewModel.SelectedCity = city` (триггерит `OnSelectedCityChanged` → `SyncToClimateData` → `CalculationContext.UpdateClimate`).
  - Сценарии (все из таблицы 1.6 README):
    | Город | T5Days092 | Ожидаемый AirTemperature | Ожидаемый DesignTemperatureValue | ColdFiveDayTemperature |
    | --- | --- | --- | --- | --- |
    | Сочи | -5 | -10 (Zone_M10) | -10 | -5 |
    | Москва | -23 | -10 (Zone_M10) | -10 | -23 |
    | Условный | -30 | -15 (Zone_M15) | -15 | -30 |
    | Норильск | -42 | -20 (Zone_M20) | -20 | -42 |
    | Любой + IsHighRequirements=true | любое | -20 (Zone_M20_Plus) | -20 | T5Days092 |
  - Для каждого: `Assert.That(_viewModel.DesignTemperatureValue, Is.EqualTo(expectedAirTemp))` и `Assert.That(_climateData.ColdFiveDayTemperature, Is.EqualTo(t5days))` (подтверждает, что `ColdFiveDayTemperature` не подменяет `AirTemperature`).
  - Также добавить прямой assert: `Assert.That(_climateData.AirTemperature, Is.EqualTo(expectedAirTemp))`.
  Не менять существующие assertion-значения тестов без выбранного города (они остаются корректными через fallback). Не трогать `baseline_refactor_dedupe.json`.
  Parallelization: Wave 1 | Blocked by: 1 | Blocks: 4 | Can parallelize with: 3
  References: tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs:245-538; tests/SnowMeltingCalculator.Tests/Climate/ClimateViewModelTests.cs:31-79 (эталон создания CityInfo и проверок таблицы 1.6); src/ViewModels/Climate/ClimateViewModel.cs:515-563 (OnSelectedCityChanged), 711-728 (SyncToClimateData).
  Acceptance criteria: `dotnet test --filter "DesignTemperatureValue_FollowsAirTemperature_WithCitySelected"` exit 0; новый тест покрывает все 4 зоны + повышенные требования; при ручном revert T1 тест падает ( Moscow −23 → DesignTemperatureValue == −23, expected −10).
  QA scenarios: happy — все 5 сценариев зелёные; failure — временно откатить T1 и подтвердить, что сценарий «Москва −23» падает. Evidence `<attemptDir>/task-2-fix-design-temperature-source.{txt,cs}`.
  Commit: Y | test(hydraulics): regression for design temperature source with city selected

- [x] 3. Fix contradictory assertions in existing integration tests
  What to do / Must NOT do: В `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs`:
  - Строки 519-520: заменить сообщение `"DesignTemperatureValue должен возвращать ColdFiveDayTemperature"` на `"DesignTemperatureValue должен возвращать AirTemperature"`. Само значение (-28) корректно (без города fallback), меняется только описание.
  - Проверить строки 245-308, 463-478, 523-538: они уже ожидают `DesignTemperatureValue == AirTemperature` — оставить как есть, при необходимости поправить комментарии, утверждающие связь с `ColdFiveDayTemperature` (строка 25 «IClimateData.ColdFiveDayTemperature обновляется» в файле remarks — заменить на «AirTemperature обновляется» если это про design temp).
  - При необходимости переименовать тест `UpdateFromClimateModule_UpdatesColdFiveDayTemperature` (строка 294) в `UpdateFromClimateModule_UpdatesDesignTemperatureFromAirTemperature`, если он проверяет `DesignTemperatureValue` (а не поле `ColdFiveDayTemperature`). Если тест также проверяет `_climateData.ColdFiveDayTemperature` (строка 421) — оставить отдельный assert на `ColdFiveDayTemperature` только для информационного поля, но `DesignTemperatureValue` должен идти от `AirTemperature`.
  Не удалять существующие тесты; не менять `baseline_refactor_dedupe.json`.
  Parallelization: Wave 1 | Blocked by: 1 | Blocks: 4 | Can parallelize with: 2
  References: tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs:25-26,245-308,421,463-478,510-538.
  Acceptance criteria: `grep -n "ColdFiveDayTemperature" tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs` — нет совпадений в assertion-сообщениях для `DesignTemperatureValue` (остаются только для информационного поля `ColdFiveDayTemperature`); `dotnet test --filter "ClimateToHydraulicsIntegrationTests"` exit 0.
  QA scenarios: happy — все тесты зелёные, сообщения корректны; failure — временно revert T1 и подтвердить, что тест со выбранным городом (T2) падает, а старые fallback-тесты остаются зелёными (демонстрирует, что регрессия ловит именно баг с городом). Evidence `<attemptDir>/task-3-fix-design-temperature-source.txt`.
  Commit: Y | test(hydraulics): clarify design temperature source assertions (AirTemperature not ColdFiveDay)

- [x] 4. Full green build + test gate + notepad
  What to do / Must NOT do: Запустить `dotnet build src/SnowMeltingCalculator.csproj -c Debug /p:TreatWarningsAsErrors=true` и `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj --logger "console;verbosity=normal"`. Проверить grep-gates из Scope. Дописать `.omo/notepads/refactor-dedupe-params/learnings.md` раздел `## Fix: design temperature source` с описанием бага и фикса.
  Не подавлять warnings через `#pragma`. Не менять baseline.
  Parallelization: Wave 1 | Blocked by: 1,2,3 | Blocks: F1
  References: весь изменённый набор; `.omo/notepads/refactor-dedupe-params/learnings.md`.
  Acceptance criteria: build green; `dotnet test` 100% pass; grep-gates выполнены; notepad обновлён.
  QA scenarios: happy — full green; failure — revert T1 и подтвердить, что T2 падает. Evidence `<attemptDir>/task-4-fix-design-temperature-source.{txt,log}`.
  Commit: N | (verification gate, no commit)

## Final verification wave
> Runs after ALL todos. ALL must APPROVE.
- [x] F1. Plan compliance audit
  Verify diff touches only `src/ViewModels/Hydraulics/CircuitsViewModel.cs`, `tests/.../ClimateToHydraulicsIntegrationTests.cs` (и/или `CircuitsViewModelTests.cs`), `.omo/notepads/...`. Tool: `git diff --name-only`. Agent: explore.
- [x] F2. Code quality review
  `dotnet format --verify-no-changes` на изменённых файлах + `lsp_diagnostics`. Agent: oracle.
- [x] F3. Manual QA (light)
  Запустить приложение, выбрать Москву, убедиться, что на вкладке «Гидравлика» кнопка «Расчётная температура» показывает −10 °C (а не −23). Tool: `dotnet run` + screenshot/inspection. Agent: unspecified-high.
- [x] F4. Scope fidelity
  Подтвердить: формулы/константы не тронуты; `ColdFiveDayTemperature` поле осталось; `baseline_refactor_dedupe.json` не изменён. Agent: oracle.

## Commit strategy
- Один коммит на todo с `Commit: Y`; T4 — без коммита.
- Сообщения в Conventional Commits, scope `hydraulics-vm`/`hydraulics`, summary на русском в стиле существующего `git log`.
- Без squash между todo.

## Success criteria
- `grep -n "ColdFiveDayTemperature" src/ViewModels/Hydraulics/CircuitsViewModel.cs` → 0.
- `DesignTemperature` getter в `CircuitsViewModel` читает `_calculationContext.AirTemperature`.
- Для Москвы (T5Days092=−23) `DesignTemperatureValue` == −10.
- Для Норильска (−42) `DesignTemperatureValue` == −20.
- `dotnet build /p:TreatWarningsAsErrors=true` green; `dotnet test` 100% pass.
- `baseline_refactor_dedupe.json` не изменён.
- `ThermalCalculator.cs`, `CircuitsCalculator.cs` — 0 изменений.
