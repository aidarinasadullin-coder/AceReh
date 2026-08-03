# phase-0.5-architecture-explorer-mvp — Work Plan

## TL;DR

Создать короткий и визуально полезный Architecture Explorer MVP как один автономный локальный файл:

`docs/architecture-migration/architecture-widget.html`

Файл открывается напрямую через `file://`, без сервера, npm/packages, backend или build pipeline. Explorer повторно использует принятые model v2, schema, runtime, validator и amendment Tasks 1–5 только как read-only foundation.

Работа состоит ровно из четырёх последовательных вертикальных стадий:

1. shell и загрузка model;
2. Overview плюс Current/Target/Diff;
3. ProjectSession, core flows и evidence drill-down;
4. Migration view, минимальные smoke checks и ручная архитектурная приёмка.

После каждой стадии исполнитель обязан остановиться и получить explicit owner visual feedback. Автоматический переход к следующей стадии запрещён.

Эта фаза in-place supersedes execution scope незапущенных original Phase 0.5 Tasks 5–10. Она не создаёт параллельный источник архитектурной истины. Phase 1 остаётся blocked; verifier-entrypoint correction и amendment F1–F5 остаются deferred.

Планирование не является исполнением. После импорта выполняется один Momus review, затем отдельно требуются owner plan approval и `/architecture-start phase-0.5-architecture-explorer-mvp`.

## Scope

### In scope

- Один автономный local HTML: `docs/architecture-migration/architecture-widget.html`.
- Minimal source files для его детерминированной генерации.
- Overview текущей и целевой архитектуры.
- Current, Target и directional Diff.
- Шесть фильтров одной shared architecture model: `compile-time`, `di-runtime`, `state-ownership`, `reactive`, `persistence`, `user-flow`.
- ProjectSession как целевой aggregate root, но не как уже существующая production implementation.
- Lifecycle и четыре state slices: `ClimateState`, `ConstructionState`, `ThermalState`, `HydraulicsState`.
- Core flows: new, load, second load, edit, calculate, reset, save/reload, export.
- Evidence, limitations, invariants и deferred decisions drill-down.
- Один bounded `--check` mode с 14 practical smoke checks.
- Не более одного нового implementation acceptance document.
- Factual workflow updates в `TASK_CONTEXT.md`.

### Read-only foundation

Не изменять и не дублировать:

- `docs/architecture-migration/maps/architecture-model.json`
- `docs/architecture-migration/maps/architecture-model.widget.schema.json`
- `docs/architecture-migration/widget/model-contract.mjs`
- `docs/architecture-migration/widget/architecture-widget.mjs`
- `docs/architecture-migration/widget/verify-widget.mjs`
- accepted model v2/runtime validation evidence;
- accepted amendment Tasks 1–5 artifacts;
- six Phase 0 architecture maps;
- state inventory и target invariants.

Accepted runtime остаётся единственным источником immutable model loading, modes, view filtering, counts, stable-ID Diff, direction classification, changed fields и invariant-violation classification. Browser UI владеет только presentation state и не пересчитывает architecture semantics.

### Historical artifact boundary

Historical `docs/architecture-migration/architecture_widget.html` остаётся byte-unchanged и не используется как runtime, generator, styling или content input. Новый MVP использует отдельный путь с дефисом: `docs/architecture-migration/architecture-widget.html`.

### Exact phase write allow-list

- `docs/architecture-migration/widget/architecture-widget.template.html`
- `docs/architecture-migration/widget/architecture-widget.css`
- `docs/architecture-migration/widget/generate-widget.mjs`
- `docs/architecture-migration/architecture-widget.html`
- `docs/architecture-migration/evidence/phase-0.5-architecture-explorer-mvp-acceptance.md`
- `docs/architecture-migration/TASK_CONTEXT.md`

Каждая стадия использует только указанный в ней subset этого allow-list.

### Standalone contract

`architecture-widget.html` должен:

- быть одним UTF-8 HTML-файлом;
- открываться двойным кликом или через `file:///D:/IA/ace%20v.2/docs/architecture-migration/architecture-widget.html`;
- содержать inline CSS, presentation script и один embedded payload;
- не использовать `fetch`, `XMLHttpRequest`, WebSocket, service worker или dynamic import;
- не обращаться к network и не требовать sibling files во время просмотра;
- не использовать external fonts, scripts, stylesheets, images или CDN;
- показывать controlled error screen при invalid/missing embedded payload.

### Must-NOT-Have

- Изменения production C#, XAML, ViewModels, DI, Results или state ownership.
- Реализация production `ProjectSession`.
- Изменения tests, fixtures, `.smc`, persistence format или formulas.
- Изменения accepted model v2, runtime, schema, validator или historical `architecture_widget.html`.
- Второй architecture model, runtime или source of truth.
- Server, backend, npm, packages, package manifest, lockfile, bundler, build pipeline, CI или command registration.
- Browser automation, Playwright, screenshots или responsive screenshot matrix.
- Mutation probes, SHA-bound infrastructure, receipt framework, verifier suites или verifier entrypoints.
- F-number final-verification wave.
- Возобновление original Tasks 5–10, correction или amendment F1–F5.
- Планирование, запуск или разблокирование Phase 1.
- Автоматический переход между стадиями без explicit owner feedback.

## Verification strategy

Используется visible-value-first, tests-after:

- Tasks 1–3 создают последовательные visual slices и выполняют только минимальные direct assertions.
- После каждого результата исполнитель останавливается и ждёт owner feedback.
- Task 4 добавляет один bounded `--check` mode с ровно 14 checks.
- Reusable verification framework не создаётся.

Canonical commands из repository root:

```powershell
node "docs/architecture-migration/widget/generate-widget.mjs"
node "docs/architecture-migration/widget/generate-widget.mjs" --check
```

Generation читает accepted model/schema/runtime, создаёт temporary complete HTML и заменяет canonical HTML только после успеха. `--check` ничего не записывает, не вызывает `verify-widget.mjs` и не расширяется во время execution.

## Execution strategy

| Task | Depends on | Следующий gate |
|---|---|---|
| 1 | reviewed/approved plan и explicit `/architecture-start` | Owner visual feedback 1 |
| 2 | explicit owner `PASS` для Task 1 | Owner visual feedback 2 |
| 3 | explicit owner `PASS` для Task 2 | Owner visual feedback 3 |
| 4 | explicit owner `PASS` для Task 3 | Final owner manual acceptance |

`PASS` разрешает только следующую task. `REJECT`, `BLOCKED` или отсутствие explicit response запрещают продолжение. При подтверждённой невыполнимости исполнитель останавливается, фиксирует blocker и не импровизирует новый model/runtime, не расширяет scope и не ослабляет acceptance criteria.

## Todos

- [ ] 1. Создать автономный shell и загрузить accepted model

  **Exact write-set:** template, CSS, generator, `architecture-widget.html` и factual Task 1 status в `TASK_CONTEXT.md`.

  **Implementation:** создать minimal template/CSS/deterministic Node generator; прочитать accepted model v2, schema и runtime read-only; провести model через accepted loading/validation boundary; встроить один immutable payload, CSS и presentation script; заменить output атомарно; добавить controlled startup error.

  **Visible outcome:** `Architecture Explorer`, local/offline indicator, model ID, contract version, snapshot SHA, source basis, navigation `Overview`, `ProjectSession`, `Migration`, notice о documentation-only назначении.

  **Open:** выполнить generation command и открыть `D:\IA\ace v.2\docs\architecture-migration\architecture-widget.html` двойным кликом или canonical `file://` URI.

  **Smoke:** exit `0`; HTML существует и не пуст; payload парсится; model identity совпадает; external resources отсутствуют; historical HTML не изменён; failure с missing temporary input не заменяет output.

  **Owner questions:** открывается ли HTML без сервера; понятна ли accepted model; достаточно ли ясен shell.

  **Mandatory stop:** не начинать Task 2 до explicit `PASS`. Отдельные receipts запрещены; при необходимости создать/append только единый acceptance document. Commit/staging запрещены.

- [ ] 2. Реализовать Overview и Current/Target/Diff

  **Exact write-set:** template, CSS, generator, `architecture-widget.html` и factual Task 2 status в `TASK_CONTEXT.md`.

  **Implementation:** показать Overview из одного payload; разделить Current/Target, пометив Target `unimplemented`; получить Diff rows/changed fields из accepted runtime по stable IDs; добавить ровно шесть view filters и union combined filters; controlled no-match/valid-empty; не пересчитывать semantics в browser.

  **Visible outcome:** Overview объясняет model/snapshot/source basis; видны Current, Target, `Current -> Target`, five diff classes, changed fields, invariant marker и шесть views без ложного implemented Target.

  **Open:** regenerate, заново открыть canonical local file, перейти Overview -> Current -> Target -> Diff.

  **Smoke:** metadata совпадает; ровно шесть views; один shared payload; stable Diff IDs/runtime changed fields; Target `unimplemented`; combined views без duplicates; invalid mode/view дают controlled state.

  **Owner questions:** ясны ли Current/Target; полезен ли Diff; различимы ли шесть views; нет ли впечатления реализованного Target.

  **Mandatory stop:** не начинать Task 3 до explicit `PASS`; замечания исправлять только в Task 2 write-set и повторно показать стадию. Commit/staging запрещены.

- [ ] 3. Реализовать ProjectSession, core flows и evidence drill-down

  **Exact write-set:** template, CSS, generator, `architecture-widget.html` и factual Task 3 status в `TASK_CONTEXT.md`.

  **Implementation:** показать ProjectSession как Target; lifecycle/identity/dirty/restore guard; четыре state slices; stable ID, current/target owner, migration status и coverage; forbidden god-object responsibilities; ViewModels как adapters, Results как derived projection, application services без concrete ViewModels; восемь flow groups и constituent records; evidence/limitations/invariants/deferred decisions drill-down; missing evidence не скрывает record.

  **Visible outcome:** понятный ProjectSession screen, соседние current/target owners, отдельные slices, core flows, architecture risks и source evidence.

  **Open:** regenerate, открыть canonical local file, выбрать ProjectSession, открыть по одной row каждого slice, Core flows и representative evidence details.

  **Smoke:** ProjectSession Target/unimplemented; lifecycle и четыре slices; все IDs разрешаются; ровно восемь flow groups с accepted records; references разрешаются; detail не меняет mode/views/payload; synthetic records отсутствуют.

  **Owner questions:** ясно ли, что переходит в ProjectSession/slices; что остаётся вне root; почему текущая архитектура связана; достаточны ли flows/evidence.

  **Mandatory stop:** не начинать Task 4 до explicit `PASS`; rejection переводит workflow в `blocked`. Commit/staging запрещены.

- [ ] 4. Завершить Migration screen, выполнить 14 smoke checks и провести ручную архитектурную приёмку

  **Exact write-set:** template, CSS, generator, `architecture-widget.html`, единый acceptance document и factual final MVP sections в `TASK_CONTEXT.md`.

  **Implementation:** сгруппировать additions/removals/ownership moves из accepted Diff; показать expected dependency changes, target invariants, deferred decisions и next safe production refactor как recommendation; добавить ровно 14 direct `--check` assertions; записать concise acceptance document; оставить Phase 1 blocked.

  **Visible outcome:** Migration объясняет текущие препятствия, ownership moves, dependency additions/removals, защищаемые flows, первый safe refactor и явную границу «Explorer ничего не мигрирует».

  **Open:** выполнить generation и `--check`, открыть canonical HTML и пройти Overview, ProjectSession, Migration.

  **Exact 14 smoke checks:**

  1. HTML существует и является non-empty UTF-8 document.
  2. Существует ровно один embedded payload.
  3. Payload парсится и содержит accepted model/contract/source identity.
  4. Присутствуют Overview, ProjectSession и Migration.
  5. Присутствуют Current, Target и Diff.
  6. Присутствуют ровно шесть accepted views.
  7. Diff rows имеют stable ID и accepted direction.
  8. ProjectSession помечен Target/unimplemented.
  9. Присутствуют lifecycle и четыре state slices.
  10. Присутствуют ровно восемь core-flow groups.
  11. Displayed evidence/limitation/invariant/decision references разрешаются.
  12. External/network/runtime file dependencies отсутствуют.
  13. Два generation passes при одинаковых inputs дают byte-identical HTML.
  14. `--check` ничего не записывает и не изменяет canonical HTML.

  **Failure smoke:** orphan evidence, seventh view, invalid Diff direction или external script/fetch в temporary copy дают nonzero; failure не заменяет последний успешный HTML.

  **Final eight owner questions:**

  1. Понятно ли, почему текущая архитектура замедляет development: distributed writable state, ViewModel coupling, Results/load-reset orchestration и reactive side effects?
  2. Понятно ли, что переходит в ProjectSession: lifecycle, identity, dirty/restore guard и canonical module inputs?
  3. Понятно ли разделение ClimateState, ConstructionState, ThermalState, HydraulicsState с одним writable canonical owner?
  4. Понятно ли, что ProjectSession не должен быть flat god object, владеть derived Results/UI behavior или связывать services с ViewModels?
  5. Понятны ли добавляемые узкие contracts между aggregate root, slices, adapters, persistence boundary и projections?
  6. Понятны ли удаляемые application-service-to-ViewModel coupling, cross-ViewModel ownership, duplicate stores и Results ownership of inputs?
  7. Понятны ли сохраняемые flows: new, load, second load, edit, calculate, reset, save/reload, export?
  8. Понятен ли следующий safe refactor: после отдельного Phase 1 plan/approval создать ProjectSession shell и один narrow state contract, затем мигрировать только один vertical slice?

  **Acceptance:** восемь `YES` означают только готовность к отдельному owner result acceptance; любой `NO` переводит workflow в `blocked`; `completed` и Phase 1 не разрешаются автоматически.

  **Mandatory stop:** остановиться после демонстрации; не запускать correction, F1–F5, original Task 5 или Phase 1.

  **Evidence:** единственный future acceptance document `docs/architecture-migration/evidence/phase-0.5-architecture-explorer-mvp-acceptance.md`; factual `TASK_CONTEXT.md` update. Commit запрещён до отдельного owner result acceptance.

## Commit strategy

После отдельного owner acceptance допускается один atomic commit `feat(architecture): add architecture explorer MVP`, включающий только фактически изменённые allow-listed paths. Использовать exact path staging; broad `git add .`, amend/rebase/squash запрещены без отдельного запроса.

## Rollback boundary

До первого owner `PASS` удалять только новые MVP files и фиксировать `blocked`. После `PASS` последний принятый `architecture-widget.html` остаётся visual checkpoint; failed next generation его не заменяет; откатывается только текущая stage в allow-list. Accepted foundation и owner feedback не переписываются. Destructive/broad Git rollback запрещён.

## Success criteria

- `architecture-widget.html` существует, открывается offline и historical underscore-file не изменён.
- Overview, ProjectSession и Migration отвечают на owner architecture questions.
- Current/Target/Diff используют accepted semantics; шесть views фильтруют один model.
- ProjectSession/four slices показаны Target/unimplemented; восемь flow groups связаны с evidence.
- 14 checks проходят; четыре stage gates получили explicit `PASS`; восемь final answers — `YES`.
- Не более одного acceptance document; accepted foundation неизменён.
- Original Tasks 5–10 superseded in place; correction/F1–F5 deferred; Phase 1 blocked.
- Workflow остановлен перед отдельным owner result acceptance.
