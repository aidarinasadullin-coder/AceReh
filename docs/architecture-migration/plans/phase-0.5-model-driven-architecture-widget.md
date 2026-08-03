# phase-0.5-model-driven-architecture-widget — Work Plan

> **Граница импорта:** этот draft следует импортировать как  
> `docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget.md`.
>
> Нельзя перезаписывать уже принятый владельцем план  
> `docs/architecture-migration/plans/phase-0-baseline.md`.
>
> Этот документ является только планом. В рамках текущей planning-сессии файлы не создавались и реализация не запускалась.

## TL;DR (For humans)

### Что будет получено

Phase 0.5 создаст новый standalone model-driven architecture widget, который:

- строится детерминированно из одного canonical architecture model;
- содержит Baseline, Current, Target и directional Diff;
- отображает шесть независимых, комбинируемых архитектурных views;
- сохраняет stable IDs, typed semantics, provenance, confidence и limitations;
- работает offline из `file://`;
- поддерживает keyboard navigation, screen-reader announcements, reduced motion и responsive layouts;
- проходит все 37 acceptance rows `WA-001`–`WA-037`;
- не изменяет исторический `architecture_widget.html`;
- не меняет production code, tests, `.smc`, persistence, DI, ViewModels или Phase 1;
- останавливает workflow на отдельном owner-acceptance gate.

### Почему выбран этот подход

Главная граница — **один атомарный runtime payload**. Виджет не должен скрыто объединять schema, Markdown maps, historical HTML, audit или второй JSON-файл. Это предотвращает stale state, семантическое расхождение шести views и ложное представление target architecture как уже реализованной.

Historical widget сохраняется как immutable input artifact, но не используется как источник данных или визуальный контракт. Новый widget имеет отдельные source, generated и evidence artifacts.

### Что намеренно не входит в фазу

- реализация `ProjectSession`;
- перенос canonical state ownership;
- Phase 1 или любой production architecture slice;
- изменение `.smc` или persistence schema;
- изменение существующих тестов и fixtures приложения;
- установка инструментов или зависимостей;
- CI, Git hooks, package scripts или OpenCode commands;
- перезапись текущего `architecture_widget.html`;
- автоматическое извлечение семантики из имён классов или старого HTML.

### Масштаб и риск

- 4 execution waves;
- 10 implementation/documentation tasks;
- 5 final-verification tasks;
- высокий риск семантической недостоверности при смешении current/target;
- средний риск scope leakage из-за уже dirty worktree;
- средний frontend-риск по accessibility, offline и responsive behavior;
- низкий production-риск, поскольку `src/`, `tests/` и persistence находятся вне allow-list.

### Зафиксированные решения

- generated artifact:  
  `docs/architecture-migration/architecture_widget.generated.html`;
- historical source остается неизменным;
- preservation copy:  
  `docs/architecture-migration/archive/architecture_widget.phase-0-historical.html`;
- canonical runtime model:  
  `docs/architecture-migration/maps/architecture-model.json`;
- отдельный additive widget contract schema:  
  `docs/architecture-migration/maps/architecture-model.widget.schema.json`;
- runtime начинает с одного embedded model и допускает только атомарную replacement одного JSON-документа;
- Target/Diff mutation fixtures создаются только в памяти;
- future rebuild выполняется одной документированной Node command без CI/hook integration;
- Diff использует stable ID и direction-sensitive canonical-field comparison;
- invalid replacement сохраняет последний полностью валидный visible state;
- Phase 1 остается заблокирована до явного owner acceptance результата Phase 0.5.

## Scope

### Planning basis

- Repository root: `D:/IA/ace v.2`.
- Текущая ветка: `master`.
- Проверенный planning HEAD:  
  `f0d19c34ac03075d64548f1059e9c6626d3596b5`.
- Phase 0 завершена и принята владельцем 2026-07-31.
- Current phase: `phase-0.5-model-driven-architecture-widget`.
- Phase 1 не запущена.
- Worktree содержит pre-existing user changes; исполнитель обязан повторно снять точный dirty ledger перед первой записью.
- `architecture_widget.html`, старый audit, старые metrics и отклонённые planning drafts считаются недоверенными входами.
- Текущий canonical baseline определён:
  - `maps/architecture-model.schema.json`;
  - `maps/architecture-model.baseline.json`;
  - шестью Markdown views;
  - `maps/state-inventory.md`;
  - `maps/target-invariants.md`;
  - `widget-spec.md`;
  - Phase 0 evidence receipts.
- Текущий widget contract содержит:
  - один canonical runtime input;
  - четыре modes;
  - шесть views;
  - stable-ID Diff;
  - 37 acceptance rows;
  - 24 mode/view pairs;
  - 12 special-state rows.
- Любые числовые model counts из Phase 0 являются planning hints, а не execution-time constants. Они должны быть пересчитаны из фактически принятого model payload.

### In scope

1. Повторное фиксирование repository/dirty/tool boundary.
2. Byte-for-byte preservation исторического widget.
3. Новый versioned canonical widget model.
4. Draft 2020-12 widget schema contract.
5. Pure model validation и semantic/reference validation.
6. Immutable runtime model loading.
7. Baseline, Current, Target и Diff.
8. Шесть независимых combinable views.
9. Search, status/risk filtering и deterministic counts.
10. Evidence, invariant и deferred-decision drill-down.
11. Offline standalone generation.
12. Design system contract в `widget/DESIGN.md`.
13. Keyboard, screen-reader, focus, reduced-motion и responsive behavior.
14. Reproducible generated HTML.
15. Agent-executable `WA-001`–`WA-037` acceptance.
16. Browser QA на 375, 768 и 1280 CSS px.
17. Scope and protected-hash gates.
18. Независимая final-verification wave.
19. Обновление workflow до `awaiting-owner-acceptance`, но не дальше.

### Exact execution write allow-list

Во время Phase 0.5 разрешены записи только по следующим путям:

- `docs/architecture-migration/archive/architecture_widget.phase-0-historical.html`
- `docs/architecture-migration/maps/architecture-model.json`
- `docs/architecture-migration/maps/architecture-model.widget.schema.json`
- `docs/architecture-migration/widget/DESIGN.md`
- `docs/architecture-migration/widget/model-contract.mjs`
- `docs/architecture-migration/widget/architecture-widget.template.html`
- `docs/architecture-migration/widget/architecture-widget.css`
- `docs/architecture-migration/widget/architecture-widget.mjs`
- `docs/architecture-migration/widget/generate-widget.mjs`
- `docs/architecture-migration/widget/verify-widget.mjs`
- `docs/architecture-migration/widget/browser-qa.mjs`
- `docs/architecture-migration/architecture_widget.generated.html`
- `docs/architecture-migration/evidence/phase-0.5-repository-snapshot.md`
- `docs/architecture-migration/evidence/phase-0.5-historical-widget-preservation.md`
- `docs/architecture-migration/evidence/phase-0.5-model-validation.md`
- `docs/architecture-migration/evidence/phase-0.5-generation.md`
- `docs/architecture-migration/evidence/phase-0.5-acceptance.json`
- `docs/architecture-migration/evidence/phase-0.5-browser-qa.md`
- `docs/architecture-migration/evidence/phase-0.5-screenshots/375-current.png`
- `docs/architecture-migration/evidence/phase-0.5-screenshots/768-diff.png`
- `docs/architecture-migration/evidence/phase-0.5-screenshots/1280-combined.png`
- `docs/architecture-migration/evidence/phase-0.5-scope-gate.md`
- `docs/architecture-migration/evidence/phase-0.5-final-verification-f1-plan-compliance.md`
- `docs/architecture-migration/evidence/phase-0.5-final-verification-f2-contract-quality.md`
- `docs/architecture-migration/evidence/phase-0.5-final-verification-f3-browser-qa.md`
- `docs/architecture-migration/evidence/phase-0.5-final-verification-f4-scope-fidelity.md`
- `docs/architecture-migration/evidence/phase-0.5-final-verification.md`
- `docs/architecture-migration/TASK_CONTEXT.md`

Approved plan может быть создан только planning/import workflow, а не implementation tasks:

- `docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget.md`

Browser profiles, temporary generations, accessibility dumps и mutation fixtures должны находиться только в task-owned temporary directory. Они не добавляются в repository.

### Must-NOT-Have constraints

- Нельзя изменять `docs/architecture-migration/architecture_widget.html`.
- Нельзя изменять:
  - `maps/architecture-model.baseline.json`;
  - `maps/architecture-model.schema.json`;
  - `widget-spec.md`;
  - `maps/target-invariants.md`;
  - шесть Phase 0 maps;
  - `maps/state-inventory.md`;
  - Phase 0 evidence;
  - historical audits и metrics;
  - archived rejected drafts.
- Нельзя изменять файлы под:
  - `src/`;
  - `tests/`;
  - `data/`;
  - `resources/`;
  - `installer/`;
  - `publish/`;
  - `.opencode/`;
  - `.omo/`;
  - любыми `.smc` paths.
- Нельзя изменять formulas, UI приложения, DI, ViewModels, state ownership, load/reset, save/restore или Results behavior.
- Нельзя создавать production `ProjectSession` или state slices.
- Нельзя изменять package manifest, lockfile, CI, hooks, commands, runtime configuration или release artifacts.
- Нельзя устанавливать или обновлять Node packages, SDK, browser, LSP, workload или schema validator.
- Нельзя использовать исторический widget, audit или Markdown maps как runtime data source.
- Нельзя загружать или объединять два runtime model documents.
- Нельзя выводить runtime semantics, canonical ownership, confidence или invariant status из class names, layout или эвристик.
- Нельзя переносить historical counts в новый widget без canonical current evidence.
- Нельзя помечать target record как current, observed или implemented.
- Нельзя изменять loaded model посредством filter/search/selection/Diff/detail operations.
- Нельзя идентифицировать Diff records по display name, array position, view membership или visual position.
- Нельзя создавать mouse-only, hover-only, color-only или animation-dependent behavior.
- Нельзя использовать manual owner clicking как единственное QA-доказательство.
- Нельзя выполнять `git reset`, `git clean`, `git checkout`, `git restore`, `git stash`, broad formatting или broad staging.
- Нельзя планировать, запускать или разблокировать Phase 1.
- Нельзя переводить workflow в `completed` без отдельного owner acceptance.

### Runtime model boundary

Runtime получает ровно один architecture document:

```text
embedded canonical JSON
        |
        v
parse
        |
        v
contract validation
        |
        v
semantic/reference validation
        |
        v
immutable snapshot construction
        |
        v
atomic visible-state commit
```

Replacement следует тому же пути. Любая ошибка до commit:

- не изменяет visible records;
- не изменяет active filters;
- не изменяет selected record;
- не изменяет Diff pair;
- показывает конкретную controlled error state.

Schema является build-time contract, но не вторым runtime architecture source.

### Model and schema compatibility policy

- Contract identity определяется schema `$id` и `contract_version`.
- Runtime поддерживает только явно перечисленные `$id` и major versions.
- Неизвестный `$id` или major version отклоняется.
- Additive optional fields требуют schema revision, но не обязательно major bump.
- Изменение значения существующего поля, enum semantics, identity rules или Diff interpretation требует нового major contract version.
- Runtime не угадывает compatibility.
- Existing Phase 0 schema и fixture остаются immutable.
- New widget model использует существующие stable Phase 0 IDs.
- Target records всегда содержат explicit `unimplemented` status.
- Deferred decisions сохраняют owner-deferred state; Phase 0.5 не принимает их за владельца.

### Diff contract

- Identity: stable record ID.
- Default direction: `Current -> Target`.
- Left и right snapshots должны различаться.
- Same-snapshot selection отклоняется; предыдущая валидная пара сохраняется.
- Swap меняет направление и обращает `added`/`removed`.
- `changed` означает один stable ID в обеих snapshots с изменением canonical fields.
- Presentation-only fields исключены:
  - focus;
  - selected record;
  - expanded details;
  - filter ordering;
  - current search string;
  - live-region state;
  - visual position.
- Classification precedence:
  1. unresolved identity/evidence;
  2. added/removed;
  3. changed/unchanged.
- Invariant violation — ортогональный flag, не заменяющий directional class.

### Evidence receipt contract

Каждый Phase 0.5 Markdown receipt должен содержать:

- `phase: phase-0.5-model-driven-architecture-widget`;
- execution-time `snapshot_sha`;
- `source_basis`;
- UTC timestamp;
- canonical working directory;
- exact command/browser operation;
- tool/browser version;
- exit code;
- assertion totals;
- `status: pass|fail|degraded|blocked`;
- input/output SHA-256;
- changed-path allow-list result;
- limitations и unavailable-tool disclosure.

Generated HTML не должен содержать:

- generation timestamp;
- absolute machine path;
- random ID;
- browser/tool version;
- machine-specific environment text;
- nondeterministic record ordering.

## Verification strategy

### Test strategy

Используется **tests-after с contract-first implementation**:

1. Сначала фиксируются schema/model/design/runtime contracts.
2. Затем реализуются pure validators и runtime behavior.
3. Затем выполняются deterministic Node checks.
4. После создания generated artifact запускается browser acceptance.
5. Затем независимые F1–F4 повторяют проверки.
6. F5 агрегирует только самостоятельные immutable receipts.

Новые тесты в C# test projects запрещены. Все Phase 0.5 checks размещаются в allow-listed widget/evidence artifacts.

### Required verification layers

1. Repository and dirty-worktree integrity.
2. Historical artifact byte preservation.
3. JSON parse and contract validation.
4. Semantic/reference validation.
5. Immutable model and atomic replacement.
6. Stable-ID Diff.
7. Six-view separation and unions.
8. Deterministic generation.
9. Offline browser execution.
10. Accessibility and responsive behavior.
11. Complete `WA-001`–`WA-037` coverage.
12. Exact changed-path scope gate.
13. Independent final verification.
14. Owner acceptance as отдельный workflow gate.

### Tool policy

- Node используется только если уже установлен.
- Browser QA использует уже доступный Playwright-compatible browser surface или уже установленный Chromium/Puppeteer-compatible harness.
- Missing browser automation tooling означает `blocked`; установка запрещена.
- Browser availability probe выполняется в Task 6 до generation; реальное browser execution generated artifact выполняется только после Task 7 в Tasks 8 и 9.
- Draft 2020-12 validation:
  - если generic validator уже установлен, выполняется full schema validation;
  - иначе выполняется self-contained structural/semantic validator;
  - receipt обязан честно указывать `degraded` для generic Draft 2020-12 coverage;
  - нельзя называть custom validation полной Draft 2020-12 validation.

## Execution strategy

### Wave 1 — Freeze and contracts

Последовательно:

- Task 1;
- Task 2;
- Task 3;
- Task 4.

### Wave 2 — Independent pre-generation implementation lanes

После freeze model/runtime interfaces:

- Task 5 — semantic rendering и non-browser semantic QA;
- Task 6 — design/accessibility/responsive contract, CSS и browser-harness availability probe.

Tasks 5 и 6 могут идти параллельно только при disjoint file ownership. Task 6 не открывает и не проверяет `architecture_widget.generated.html`; generated-artifact browser assertions отложены до Tasks 8 и 9.

### Wave 3 — Generation and post-generation acceptance

Последовательно:

- Task 7 — deterministic generation;
- Task 8 — complete acceptance suite на созданном artifact;
- Task 9 — полный real-browser accessibility/responsive/offline и negative-probe pass.

### Wave 4 — Scope gate

- Task 10.

### Final verification

- F1–F4 запускаются параллельно и пишут разные immutable receipts.
- F5 запускается только после завершения F1–F4.
- Ни один parallel lane не append-ит в общий receipt.
- Только F5 пишет aggregate final verification.

### Dependency matrix

| Task | Depends on | Blocks | Parallel-safe |
|---|---|---|---|
| 1 | Owner-authorized Phase 0.5 execution | 2–10 | No |
| 2 | 1 | 3–10 | No |
| 3 | 2 | 4–10 | No |
| 4 | 3 | 5, 6, 7–10 | No |
| 5 | 3, 4 | 7–10 | With 6; owns semantic runtime/acceptance JSON |
| 6 | 3, 4 | 7–10 | With 5; owns DESIGN/CSS and pre-generation harness probe only |
| 7 | 5, 6 | 8–10 | No; creates generated artifact |
| 8 | 7 | 9–10 | No; first task allowed to execute generated artifact |
| 9 | 8 | 10 | No; full browser/accessibility/responsive QA |
| 10 | 1–9 | F1–F4 | No |
| F1 | 10 | F5 | With F2–F4 |
| F2 | 10 | F5 | With F1, F3, F4 |
| F3 | 10 | F5 | With F1, F2, F4 |
| F4 | 10 | F5 | With F1–F3 |
| F5 | F1–F4 APPROVE | Owner acceptance | No |

## Todos

- [ ] 1. Зафиксировать execution-time repository, tool и dirty-worktree boundary

**Цель:** получить полный воспроизводимый ledger состояния перед первой Phase 0.5 записью.

**References:**

- `AGENTS.md`
- `docs/architecture-migration/AGENTS.md`
- `docs/architecture-migration/TASK_CONTEXT.md`
- `docs/architecture-migration/evidence/repository-snapshot.md`
- `.git/HEAD`
- `.git/refs/heads/master`

**Действия:**

- подтвердить `git rev-parse --show-toplevel`;
- записать HEAD, branch, upstream и execution timestamp;
- снять `git status --porcelain=v1 --untracked-files=all`;
- отдельно перечислить tracked diff names и pre-existing untracked paths;
- для каждого present dirty path записать SHA-256 и размер;
- для deleted path записать explicit deleted state;
- корректно сохранить rename status и Cyrillic paths;
- сравнить новый ledger с Phase 0 snapshot, не предполагая их равенство;
- записать версии Node и доступных browser engines;
- проверить наличие browser automation без установки;
- записать snapshot в:
  `docs/architecture-migration/evidence/phase-0.5-repository-snapshot.md`.

**Acceptance criteria:**

- Git root равен `D:/IA/ace v.2`;
- execution HEAD записан фактически;
- каждая porcelain row имеет ровно одну ledger row;
- каждый present dirty path имеет recomputable SHA-256;
- deleted/renamed/untracked states не теряются;
- все pre-existing dirty paths классифицированы как protected;
- отсутствующие browser tools не устанавливаются;
- receipt содержит все обязательные evidence metadata.

**Happy-path QA:**

- независимый PowerShell pass повторно собирает normalized status/hash ledger;
- количество и содержимое rows совпадает с receipt;
- evidence сохраняется в `phase-0.5-repository-snapshot.md`.

**Failure-path QA:**

- parser прогоняется на copied input с deleted, renamed, Cyrillic, untracked directory и absent upstream;
- mismatch обязан завершить task со `status: blocked`;
- никакие файлы, кроме receipt, не изменяются.

**Commit guidance:** commit запрещён во время execution.

- [ ] 2. Сохранить historical widget byte-for-byte и изолировать его от нового runtime

**Цель:** гарантировать, что существующий historical artifact не будет перезаписан или превращён в скрытый source of truth.

**References:**

- `docs/architecture-migration/architecture_widget.html`
- `docs/architecture-migration/AGENTS.md:17-27`
- `docs/architecture-migration/widget-spec.md:20-33`
- `docs/architecture-migration/TASK_CONTEXT.md`

**Действия:**

- вычислить SHA-256 и byte length исходного `architecture_widget.html`;
- создать byte-for-byte copy:
  `archive/architecture_widget.phase-0-historical.html`;
- повторно вычислить source/archive hashes;
- записать preservation receipt;
- явно отметить, что оба файла не являются runtime или generator inputs;
- проверить отсутствие reference на historical path в generator/runtime sources.

**Acceptance criteria:**

- source и archive bytes идентичны;
- source hash после копирования совпадает с pre-task hash;
- новый generated path не aliases historical path;
- generator/runtime не читают historical HTML;
- archive copy существует только по allow-listed path.

**Happy-path QA:**

- PowerShell выполняет byte comparison и SHA-256 comparison;
- source/archive hashes и byte lengths совпадают.

**Failure-path QA:**

- altered copied hash отклоняется;
- simulated generator reference на `architecture_widget.html` отклоняется;
- mismatch блокирует дальнейшие tasks.

**Evidence:**

- `evidence/phase-0.5-historical-widget-preservation.md`.

**Commit guidance:** archive относится к будущему contract/preservation commit только после verification и owner acceptance.

- [ ] 3. Создать versioned canonical widget model и Draft 2020-12 contract

**Цель:** определить один canonical model, из которого строится новый widget, не изменяя принятую Phase 0 fixture.

**References:**

- `maps/architecture-model.schema.json`
- `maps/architecture-model.baseline.json`
- `maps/compile-time.md`
- `maps/di-runtime.md`
- `maps/state-ownership.md`
- `maps/reactive.md`
- `maps/persistence.md`
- `maps/user-flow.md`
- `maps/state-inventory.md`
- `maps/target-invariants.md`
- `evidence/model-validation.md`
- `widget-spec.md`

**Действия:**

- создать `maps/architecture-model.widget.schema.json`;
- объявить Draft 2020-12 `$schema`;
- определить стабильный `$id`;
- определить `contract_version`;
- создать `maps/architecture-model.json`;
- сохранить существующие stable IDs и source semantics;
- включить:
  - model metadata;
  - snapshots;
  - six view vocabulary;
  - nodes;
  - typed edges;
  - state records;
  - ordered flows;
  - evidence;
  - invariants;
  - deferred decisions;
  - limitations;
  - freshness/provenance data;
- все current records связать с current evidence;
- target records пометить `unimplemented`;
- не менять owner-deferred decisions;
- не копировать stale historical metrics;
- описать canonical fields для Diff;
- реализовать validation primitives в `widget/model-contract.mjs`;
- записать validation receipt.

**Acceptance criteria:**

- JSON parse проходит;
- schema объявляет Draft 2020-12;
- все IDs уникальны;
- все edge endpoints разрешаются;
- evidence, state, invariant и decision references разрешаются;
- ordered flow positions непрерывны;
- vocabulary содержит ровно шесть required views;
- нет seventh/omitted view;
- target records не входят в current membership;
- все current records имеют evidence или explicit degraded limitation;
- все `INV-*` и `DEC-*` из accepted Phase 0 artifacts представлены или linked;
- baseline fixture и Phase 0 schema hashes остаются неизменными;
- counts вычисляются из execution-time model, а не hard-code;
- отсутствие generic validator честно отражается как degraded generic-schema coverage.

**Happy-path QA:**

```powershell
node "docs/architecture-migration/widget/verify-widget.mjs" `
  --schema "docs/architecture-migration/maps/architecture-model.widget.schema.json" `
  --model "docs/architecture-migration/maps/architecture-model.json"
```

Ожидается exit `0`, unique IDs, resolved references и exactly six views.

**Failure-path QA:**

In-memory copies должны отклонять:

- malformed JSON;
- duplicate ID;
- orphan endpoint;
- invalid enum;
- missing semantic record;
- missing evidence reference;
- unsupported `$id`;
- unsupported major version;
- target-as-current;
- non-contiguous flow;
- omitted view;
- seventh view;
- duplicate stable ID across record kinds.

**Evidence:**

- `evidence/phase-0.5-model-validation.md`.

**Commit guidance:** schema, model, DESIGN contract и preservation archive образуют будущий atomic contract commit.

- [ ] 4. Реализовать immutable model loading, validation, filters и stable-ID directional Diff

**Цель:** создать pure runtime state boundary без partial mutation и hidden data sources.

**References:**

- `widget-spec.md:35-94`
- `widget-spec.md:123-179`
- `maps/architecture-model.json`
- `maps/architecture-model.widget.schema.json`
- `widget/model-contract.mjs`

**Действия:**

- реализовать pure parsing/validation functions;
- deep-freeze принятую model snapshot;
- строить indexes без изменения source arrays;
- реализовать atomic replacement transaction;
- сохранить last valid visible model при ошибке;
- реализовать modes:
  - Baseline;
  - Current;
  - Target;
  - Diff;
- реализовать six-view union;
- реализовать search и status/risk filters;
- реализовать stable-ID canonical comparison;
- исключить presentation-only state из Diff;
- реализовать default `Current -> Target`;
- реализовать swap и same-snapshot rejection;
- реализовать classification precedence;
- реализовать invariant violation как orthogonal flag;
- сохранить valid-empty Target для Phase 0 fixture;
- не обращаться к network, schema Markdown, audit или historical HTML во время runtime.

**Acceptance criteria:**

- startup payload ровно один;
- replacement принимает ровно один document;
- second-document merge path отсутствует;
- invalid replacement не меняет current snapshot, filters, selection или Diff pair;
- filter order не влияет на result set;
- swapping Diff обращает added/removed при тех же stable IDs;
- same-snapshot pair отклоняется;
- loaded model нельзя мутировать;
- target никогда не получает current/observed wording;
- deterministic counts рассчитываются из model.

**Happy-path QA:**

- Node вызывает pure operations на canonical model;
- in-memory target derivative активирует Target/Diff;
- filter union и swapped Diff возвращают ожидаемые stable IDs.

**Failure-path QA:**

Отклоняются без visible-state mutation:

- second model merge;
- same-snapshot Diff;
- mutation frozen model;
- unsupported version;
- duplicate ID;
- orphan reference;
- partial replacement;
- display-name-based identity collision.

**Evidence:**

- rows в `evidence/phase-0.5-acceptance.json`;
- validation summary в `evidence/phase-0.5-model-validation.md`.

**Commit guidance:** включить в будущий implementation commit только после независимой verification.

- [ ] 5. Реализовать semantic rendering, четыре modes, шесть views и evidence drill-down

**Цель:** отображать model-backed architecture semantics без collapse между views.

**References:**

- `widget-spec.md:50-94`
- `widget-spec.md:136-179`
- `maps/architecture-model.json`
- `widget/model-contract.mjs`
- `widget/architecture-widget.template.html`
- `widget/architecture-widget.mjs`

**Действия:**

- рендерить records только из active immutable model;
- сохранять:
  - stable ID;
  - record kind;
  - edge/source kind;
  - snapshots;
  - view badges;
  - confidence;
  - state refs;
  - participants;
  - trigger/effect;
  - migration status;
  - evidence locator;
  - limitations;
  - invariant link;
  - deferred decision state;
- combined views реализовать как union;
- сохранять original semantics и per-view counts;
- details сделать non-destructive;
- evidence path всегда показывать текстом;
- missing local evidence деградирует только navigation;
- различать:
  - empty input;
  - invalid JSON;
  - invalid contract/reference;
  - unsupported version;
  - valid empty Target;
  - zero filter result;
  - stale model;
  - missing evidence;
  - offline;
- видимые metrics вычислять только из loaded model.

**Acceptance criteria:**

- `WA-001`–`WA-029` и `WA-034`–`WA-037` имеют deterministic assertions;
- все 24 mode/view pairs отображаются;
- compile-time и DI/runtime не схлопываются;
- current reactive unknown multiplicities остаются unknown;
- target records помечены unimplemented;
- no-match не выглядит как empty input;
- missing evidence не удаляет record;
- historical counts отсутствуют, если их нет в canonical model.

**Happy-path QA:**

Из repository root выполнить точную semantic suite:

```powershell
node "docs/architecture-migration/widget/verify-widget.mjs" `
  --suite semantic-rendering `
  --schema "docs/architecture-migration/maps/architecture-model.widget.schema.json" `
  --model "docs/architecture-migration/maps/architecture-model.json" `
  --spec "docs/architecture-migration/widget-spec.md" `
  --probe-set "mode-view,combined-union,drill-down" `
  --output "docs/architecture-migration/evidence/phase-0.5-acceptance.json"
```

Suite обязана создавать Target/Diff test document только как deep-cloned in-memory derivative одного canonical document и выполнить следующие concrete assertions:

1. Для каждой пары из четырёх modes и шести views получить один result row, итого `24`, без duplicate/missing pair.
2. Для каждой пары сравнить rendered stable-ID set с model records соответствующих snapshot/view memberships.
3. Для `Baseline` и `Current` подтвердить source basis/freshness labels и отсутствие target-only records.
4. Для `Target` подтвердить `unimplemented` wording; для Phase 0 valid-empty target подтвердить zero records без fabricated `ProjectSession`.
5. Для `Diff Current -> Target` подтвердить stable-ID identity, directional `added/removed/changed/unresolved` и сохранение original edge/source semantics.
6. Для combined `compile-time + di-runtime` подтвердить, что result IDs равны математическому union двух отдельных sets, duplicate IDs отсутствуют, per-view counts равны отдельным counts, а compile/DI badges и source kinds не изменены.
7. Открыть по одному deterministic representative record каждого существующего kind: node, edge, state, flow, evidence, invariant и deferred decision.
8. Для каждого detail assertion подтвердить stable ID, kind/source kind, snapshots/views, confidence, evidence path/locator, limitations и применимые participants/state refs/trigger/effect/invariant/decision fields.
9. После open/close detail подтвердить неизменность mode, selected views, search/status filters и source model hash; detail state обязан вернуть исходный record focus target.
10. Команда должна завершиться exit `0`, записать все указанные assertions как `pass`, а semantic section output — содержать `mode_view_pairs: 24`, `combined_union: pass`, `drill_down: pass`, `source_model_mutations: 0`.

**Failure-path QA:**

Из repository root выполнить точную negative suite:

```powershell
node "docs/architecture-migration/widget/verify-widget.mjs" `
  --suite semantic-rendering-negative `
  --schema "docs/architecture-migration/maps/architecture-model.widget.schema.json" `
  --model "docs/architecture-migration/maps/architecture-model.json" `
  --probe-set "missing-evidence,stale-sha,unsupported-version,empty-input,malformed-json,orphan-endpoint,no-match,target-as-current" `
  --output "docs/architecture-migration/evidence/phase-0.5-acceptance.json"
```

Каждый probe работает только с isolated in-memory copy и обязан дать следующий результат:

- `missing-evidence`: architecture record остается в result/detail, evidence navigation получает `unavailable/degraded`, конкретный missing evidence ID присутствует в error metadata;
- `stale-sha`: persistent stale state содержит mismatch reason и не содержит `verified current`;
- `unsupported-version`: normal rendering блокируется, показывается supported contract identifier, prior valid visible state не изменяется;
- `empty-input`: rendered record count равен `0`, присутствует input action и отсутствует fabricated graph;
- `malformed-json`: parse error содержит controlled failure category, partial records отсутствуют;
- `orphan-endpoint`: validation сообщает exact orphan ID, normal rendering блокируется;
- `no-match`: model и controls остаются loaded, visible result count равен `0`, state отличается от empty input;
- `target-as-current`: document отклоняется до visible commit, target record не получает current/observed label.

Команда должна завершиться exit `0` только когда все negative probes были **успешно отклонены или переведены в требуемую controlled state**, `visible_partial_commits: 0`, `source_model_mutations: 0`, и каждая probe row имеет `verdict: pass`. Любой принятый invalid input, fabricated record, hidden semantic collapse или unexpected mutation дает nonzero exit.

**Evidence:**

- `evidence/phase-0.5-acceptance.json`.

**Commit guidance:** implementation commit после verification/acceptance.

- [ ] 6. Зафиксировать design system, реализовать accessible responsive shell и проверить browser-harness availability

**Цель:** до generation определить и реализовать design/CSS/accessibility contract, а также доказать наличие уже установленного browser harness без потребления будущего generated artifact.

**References:**

- `widget-spec.md:96-135`
- `widget-spec.md:171-175`
- `architecture_widget.html` только как historical content-category reference, не как visual/data source
- `widget/architecture-widget.template.html`
- `widget/architecture-widget.css`
- `widget/architecture-widget.mjs`
- `widget/browser-qa.mjs`

**Действия:**

- до UI code создать `widget/DESIGN.md`;
- описать:
  - information hierarchy;
  - typography;
  - color tokens;
  - spacing;
  - borders/elevation;
  - status tokens;
  - focus styles;
  - layout primitives;
  - wide/narrow behavior;
  - reduced-motion policy;
  - accessibility constraints;
  - accepted design debt;
- не копировать stale metrics, layout constants или hidden data из historical widget;
- реализовать CSS в `widget/architecture-widget.css`;
- реализовать semantic markup/accessibility hooks в template/runtime;
- обеспечить контрактом:
  - visible focus;
  - deterministic tab order;
  - programmatic names;
  - text/icon/shape status encoding;
  - live-region announcements;
  - detail focus transfer и restoration;
  - отсутствие focus trap;
  - reduced-motion parity;
  - отсутствие hover-only information;
  - отсутствие page-level overflow;
  - labeled groups вместо clipped dense tables на narrow viewport;
- реализовать только browser availability probe в `widget/browser-qa.mjs`;
- не открывать, не хешировать и не проверять `architecture_widget.generated.html`;
- не создавать screenshots и не выставлять `WA-030`–`WA-033` в `pass`: actual generated-artifact assertions принадлежат Tasks 8 и 9.

**Acceptance criteria:**

- `widget/DESIGN.md` фиксирует keyboard, focus, live-region, reduced-motion, non-color и 375/768/1280 contracts до generation;
- template/runtime/CSS содержат необходимые semantic hooks без зависимости от будущего generated artifact;
- selected controls и details имеют deterministic identifiers/accessibility contract;
- browser probe способен обнаружить только already-installed compatible harness/browser;
- доступный harness подтверждается запуском blank temporary page, не generated artifact;
- отсутствующий harness дает explicit `status: blocked`;
- package/browser installation и download отсутствуют;
- Task 6 не создает `architecture_widget.generated.html`, screenshots или final browser verdict;
- Task 6 evidence содержит только design/CSS contract checks и browser availability result.

**Browser harness gate / Happy-path QA:**

Из repository root выполнить:

```powershell
node "docs/architecture-migration/widget/browser-qa.mjs" `
  --probe-browser `
  --browser auto-existing `
  --probe-page blank-temporary `
  --evidence "docs/architecture-migration/evidence/phase-0.5-browser-qa.md"
```

`browser-qa.mjs` обязан искать только уже доступный Playwright-compatible browser surface или уже установленную Chromium/Puppeteer-compatible связку, записать фактически выбранные harness/module/browser executable/version и завершиться:

- exit `0`, если один уже установленный harness и browser executable успешно запускают task-owned blank temporary page;
- dedicated exit `3` и `status: blocked`, если доступного harness/browser нет;
- nonzero failure при найденном, но не запускающемся harness.

Fallback означает только переход к другому **уже установленному** compatible harness, обнаруженному probe. Установка package/browser, изменение manifest/lockfile или download browser запрещены. При exit `3` Task 6 и Tasks 7–10 блокируются: Task 7 не должен создавать artifact, который невозможно проверить обязательным browser gate. Нельзя подменять browser QA DOM-only Node checks или ручным просмотром.

Дополнительно выполнить pre-generation static contract check:

```powershell
node "docs/architecture-migration/widget/verify-widget.mjs" `
  --suite accessibility-contract `
  --design "docs/architecture-migration/widget/DESIGN.md" `
  --template "docs/architecture-migration/widget/architecture-widget.template.html" `
  --runtime "docs/architecture-migration/widget/architecture-widget.mjs" `
  --css "docs/architecture-migration/widget/architecture-widget.css"
```

Static suite обязана подтвердить наличие объявленных accessibility hooks, named regions/controls, live-region contract, focus restoration identifiers, reduced-motion rule, non-color status contract и responsive breakpoints для 375/768/1280. Ожидается exit `0`, `contract_assertions > 0`, `missing_hooks: 0`, `generated_artifact_reads: 0`.

**Failure-path QA:**

Изолированные probe scenarios должны подтвердить:

- отсутствие compatible harness → exit `3`, `status: blocked`, никаких installs/downloads;
- найденный, но не запускающийся executable → nonzero failure с exact harness/executable;
- static contract без accessible name hook, focus hook, live region, reduced-motion rule, non-color token или required viewport rule → nonzero exit с exact missing contract;
- попытка Task 6 открыть `architecture_widget.generated.html`, создать screenshot или выставить `WA-030`–`WA-033: pass` → nonzero scope failure;
- probe работает только с task-owned blank temporary page и не изменяет repository artifacts.

Actual DOM/accessibility assertions и deliberate browser defect probes для keyboard, focus, live region, reduced motion, blocked network и viewports не удаляются: они выполняются после Task 7 точными commands в Tasks 8 и 9.

**Evidence:**

- `widget/DESIGN.md`;
- pre-generation harness/contract section в `evidence/phase-0.5-browser-qa.md`;
- screenshots отсутствуют до Tasks 8/9.

**Commit guidance:** DESIGN входит в contract commit; CSS/UI implementation — в implementation commit после acceptance.

- [ ] 7. Реализовать deterministic standalone generation

**Цель:** создавать byte-reproducible offline HTML из одного canonical model.

**References:**

- `maps/architecture-model.json`
- `maps/architecture-model.widget.schema.json`
- `widget/model-contract.mjs`
- `widget/architecture-widget.template.html`
- `widget/architecture-widget.css`
- `widget/architecture-widget.mjs`

**Действия:**

- запускать Task 7 только после успешного Task 6 harness gate;
- реализовать `widget/generate-widget.mjs`;
- generator принимает только:
  - `--model`;
  - `--output`;
- generator валидирует model до replacement output;
- inline-ит normalized CSS, JavaScript и один JSON payload;
- использует UTF-8 и LF;
- использует fixed section order;
- сортирует records по stable ID там, где порядок не semantic;
- сохраняет flow position там, где порядок semantic;
- безопасно экранирует inline JSON;
- не включает timestamps, random IDs, absolute paths или environment data;
- не оставляет external assets/network dependencies;
- пишет output только после успешного полного generation;
- при ошибке не повреждает существующий valid output.

**Canonical command:**

```powershell
node "docs/architecture-migration/widget/generate-widget.mjs" `
  --model "docs/architecture-migration/maps/architecture-model.json" `
  --output "docs/architecture-migration/architecture_widget.generated.html"
```

**Acceptance criteria:**

- две generation runs из identical input дают identical bytes и SHA-256;
- output подготовлен для последующего открытия через `file://` в Tasks 8/9;
- CSS, JavaScript и startup payload embedded;
- external runtime asset отсутствует;
- source fingerprint и snapshot SHA embedded;
- historical widget hash неизменен;
- invalid input не заменяет valid output;
- Task 7 не заявляет browser/accessibility pass: generated-artifact execution начинается в Task 8.

**Happy-path QA:**

- дважды генерировать в temporary paths;
- сравнить bytes и SHA-256;
- сравнить temp output с allow-listed generated artifact;
- static parser подтверждает ровно один embedded architecture payload и отсутствие external runtime assets.

**Failure-path QA:**

Должны завершаться nonzero без replacement valid output:

- malformed JSON;
- unsupported version;
- target-as-current;
- orphan endpoint;
- missing required view;
- output path, совпадающий с historical widget.

**Evidence:**

- `evidence/phase-0.5-generation.md`.

**Commit guidance:** generated HTML включается вместе с generator/runtime sources, если воспроизводится byte-for-byte.

- [ ] 8. Реализовать и выполнить complete `WA-001`–`WA-037` acceptance suite на generated artifact

**Цель:** после Task 7 доказать весь принятый widget contract без ручных пропусков; Task 8 является первым task, который выполняет `architecture_widget.generated.html` в browser.

**References:**

- `widget-spec.md:136-206`
- `widget/verify-widget.mjs`
- `widget/browser-qa.mjs`
- `architecture_widget.generated.html`
- Task 6 browser-harness probe receipt
- `widget/DESIGN.md`

**Действия:**

- потребовать успешный Task 6 harness receipt и существующий Task 7 generated artifact;
- реализовать non-browser assertions в `verify-widget.mjs`;
- реализовать browser-observable assertions в `browser-qa.mjs`;
- создать один result object для каждого `WA-001`–`WA-037`;
- каждый result содержит:
  - acceptance ID;
  - setup;
  - action;
  - expected state;
  - actual state;
  - expected/actual accessibility announcement;
  - tool;
  - evidence locator;
  - verdict;
- Target/Diff synthetic modifications выполнять только на deep-cloned in-memory model;
- не сохранять synthetic target fixture как production truth;
- выполнить generated-artifact accessibility/responsive happy-path assertions, ранее определённые Task 6 contract.

**Acceptance criteria:**

- ровно 37 unique acceptance IDs;
- ровно 24 mode/view pairs;
- ровно 12 required special-state rows;
- отсутствуют skipped/manual-only rows;
- каждая row имеет verdict `pass`;
- нет второго runtime document;
- все mutations остаются in-memory;
- `WA-030`–`WA-033` подтверждены actual generated-artifact browser assertions, а не pre-generation static checks;
- generated artifact выполняется только после Task 7.

**Happy-path QA:**

Сначала выполнить complete acceptance command:

```powershell
node "docs/architecture-migration/widget/browser-qa.mjs" `
  --suite complete-acceptance `
  --artifact "docs/architecture-migration/architecture_widget.generated.html" `
  --browser auto-existing `
  --network blocked `
  --reduced-motion both `
  --viewports "375x812,768x1024,1280x800" `
  --output "docs/architecture-migration/evidence/phase-0.5-acceptance.json" `
  --evidence "docs/architecture-migration/evidence/phase-0.5-browser-qa.md"
```

Команда обязана:

1. проверить successful Task 6 harness selection;
2. открыть Task 7 artifact через `file://` в task-owned temporary browser profile;
3. выполнить `WA-001`–`WA-037`;
4. проверить все 24 mode/view pairs;
5. проверить combined union и drill-down;
6. проверить actual screen-reader-visible text;
7. проверить keyboard/focus/live-region/reduced-motion/offline/responsive rows;
8. использовать Target/Diff только как one-document in-memory derivatives;
9. завершиться exit `0` только при `37/37 pass`, `24/24 mode_view_pairs`, `12/12 special_states`, `source_model_mutations: 0`, `unexpected_network_requests: 0`.

Для generated-artifact accessibility-responsive happy path выполнить concrete assertions:

1. **Keyboard order:** последовательные `Tab`/`Shift+Tab` проходят группы input → mode → views → search/status → results → details → evidence; каждый focused element имеет programmatic accessible name и не имеет `tabindex > 0`.
2. **Visible focus:** каждый control имеет видимое computed focus отличие; active element видим и находится в viewport.
3. **Detail focus:** keyboard activation representative record переносит focus в named detail heading/region; `Escape` или Close возвращает focus тому же stable ID; focus trap отсутствует.
4. **Live region:** mode/filter/count/detail/stale/error/no-result transitions дают expected non-empty announcement; unchanged повтор не создает duplicate announcement.
5. **Reduced motion:** `no-preference` и `reduce` дают одинаковые information/accessibility tree/focus sequence; при `reduce` nonessential animation/transition отключена.
6. **Blocked network/offline:** каждый `http:`/`https:` request блокируется; unexpected request count равен `0`; local model, controls и evidence text работают; offline state объявлен.
7. **Responsive 375/768/1280:** `document.documentElement.scrollWidth <= clientWidth`; primary controls/result/detail имеют ненулевые bounding boxes и не выходят за viewport; на 375 dense data представлены labeled groups.
8. **Non-color semantics:** status/diff имеют visible text либо accessible icon/shape label; injected color removal не удаляет meaning.
9. **Console/runtime:** uncaught page errors, console errors и failed local resource loads равны `0`.

Ожидается exit `0`, `WA-030`–`WA-033: pass`, actual harness/browser metadata и отсутствие screenshot requirement на этом task; canonical screenshots создаются Task 9.

**Failure-path QA:**

Проверить copied acceptance result aggregation:

- missing ID;
- duplicate ID;
- missing mode/view pair;
- missing special-state row;
- skipped browser assertion;
- manual-only verdict;
- second-runtime-document setup;
- target record с observed/current label.

Каждый defect должен дать `REJECT`; canonical generated artifact, canonical model и prior valid browser state не изменяются.

Также выполнить generated-artifact accessibility negative probes:

```powershell
node "docs/architecture-migration/widget/browser-qa.mjs" `
  --suite accessibility-responsive `
  --artifact "docs/architecture-migration/architecture_widget.generated.html" `
  --browser auto-existing `
  --network blocked `
  --viewports "375x812,768x1024,1280x800" `
  --negative-probes "missing-name,hidden-focus,duplicate-live,color-only,hover-only,overflow,motion-dependent,clipped-table" `
  --expect-negative-rejection `
  --output "docs/architecture-migration/evidence/phase-0.5-acceptance.json" `
  --evidence "docs/architecture-migration/evidence/phase-0.5-browser-qa.md"
```

Probes изменяют только temporary browser DOM/CSS и обязаны быть обнаружены:

- `missing-name` → control с пустым accessible name отклонён;
- `hidden-focus` → отсутствующий visible focus indicator отклонён;
- `duplicate-live` → повторное unchanged announcement отклонено;
- `color-only` → status без text/icon/shape semantics отклонён;
- `hover-only` → information, недоступная keyboard/focus, отклонена;
- `overflow` → synthetic `scrollWidth > clientWidth` отклонён на каждом viewport;
- `motion-dependent` → information/focus divergence под reduced motion отклонено;
- `clipped-table` → narrow dense table без labeled group semantics отклонена.

Команда завершается exit `0` только если каждая deliberate defect получила `rejected-as-expected`; пропущенный defect, browser crash, unexpected network request или repository mutation дает nonzero exit. Если Task 6 harness receipt отсутствует или harness больше недоступен, Task 8 получает `status: blocked`; установка запрещена.

**Evidence:**

- `evidence/phase-0.5-acceptance.json`;
- acceptance/browser assertion section в `evidence/phase-0.5-browser-qa.md`.

**Commit guidance:** acceptance evidence только после независимой проверки.

- [ ] 9. Выполнить полный offline browser, accessibility, responsive и visual QA

**Цель:** после successful Task 8 повторно проверить generated artifact в реальном browser environment, выполнить canonical screenshots и зафиксировать окончательный browser receipt.

**References:**

- `architecture_widget.generated.html`
- `widget/browser-qa.mjs`
- `widget/DESIGN.md`
- `widget-spec.md:96-179`
- Task 6 harness probe
- Task 8 `phase-0.5-acceptance.json`

**Действия:**

- использовать только уже доступный и зафиксированный Task 6 browser/harness;
- запускать generated artifact через `file://`;
- запретить HTTP/HTTPS requests;
- собирать console/page errors;
- проверять accessibility tree;
- повторно проверить:
  - keyboard order;
  - visible focus;
  - focus restoration;
  - live regions;
  - reduced motion;
  - offline state;
  - invalid atomic replacement;
  - same-snapshot Diff;
  - combined views;
  - evidence details;
  - 375/768/1280 layouts;
- повторно выполнить deliberate accessibility/responsive negative probes;
- сохранить screenshots только по allow-listed paths;
- не считать screenshot самостоятельным доказательством behavior.

**Acceptance criteria:**

- zero unexpected network requests;
- zero console/page errors;
- zero page-level horizontal overflow;
- all accessible names present;
- focus assertions проходят;
- generated hash остается стабильным;
- `WA-030`–`WA-033` совпадают с Task 8 pass results;
- screenshots соответствуют passing states;
- screenshots не содержат stale historical metrics;
- actual generated-artifact checks выполняются после Task 7;
- browser receipt связывает artifact SHA-256, Task 6 harness identity и Task 8 acceptance hash.

**Happy-path QA:**

Из repository root выполнить canonical browser command:

```powershell
node "docs/architecture-migration/widget/browser-qa.mjs" `
  --suite accessibility-responsive `
  --artifact "docs/architecture-migration/architecture_widget.generated.html" `
  --browser auto-existing `
  --network blocked `
  --reduced-motion both `
  --viewports "375x812,768x1024,1280x800" `
  --screenshots "docs/architecture-migration/evidence/phase-0.5-screenshots" `
  --acceptance "docs/architecture-migration/evidence/phase-0.5-acceptance.json" `
  --evidence "docs/architecture-migration/evidence/phase-0.5-browser-qa.md"
```

Harness обязан открыть artifact через `file://` в task-owned temporary browser profile и повторить concrete assertions:

1. **Keyboard order:** `Tab`/`Shift+Tab` дают input → mode → views → search/status → results → details → evidence; accessible names присутствуют; `tabindex > 0` отсутствует.
2. **Visible focus:** computed focus indicator видим; active element не скрыт и находится в viewport.
3. **Detail focus:** keyboard activation переносит focus в named detail; close возвращает тому же stable ID; focus trap отсутствует.
4. **Live region:** expected announcements присутствуют; duplicate unchanged announcement отсутствует.
5. **Reduced motion:** information, accessible tree и focus sequence совпадают; nonessential motion отключена.
6. **Blocked network/offline:** unexpected `http:`/`https:` requests равны `0`; local behavior и evidence text сохранены; offline state объявлен.
7. **Responsive 375/768/1280:** `scrollWidth <= clientWidth`; regions видимы и не выходят за viewport; narrow dense records — labeled groups.
8. **Non-color semantics:** status/diff meaning сохраняется после color suppression.
9. **Console/runtime:** page/console/local-resource errors равны `0`.

Команда должна завершиться exit `0`, подтвердить `WA-030`–`WA-033`, `unexpected_network_requests: 0`, `page_errors: 0`, `horizontal_overflow_failures: 0` и создать ровно три allow-listed screenshots:

- `375-current.png`;
- `768-diff.png`;
- `1280-combined.png`.

**Failure-path QA:**

Выполнить:

```powershell
node "docs/architecture-migration/widget/browser-qa.mjs" `
  --suite accessibility-responsive `
  --artifact "docs/architecture-migration/architecture_widget.generated.html" `
  --browser auto-existing `
  --network blocked `
  --viewports "375x812,768x1024,1280x800" `
  --negative-probes "missing-name,hidden-focus,duplicate-live,color-only,hover-only,overflow,motion-dependent,clipped-table" `
  --expect-negative-rejection `
  --evidence "docs/architecture-migration/evidence/phase-0.5-browser-qa.md"
```

Каждая deliberate defect должна получить `rejected-as-expected`. Дополнительно controlled behavior подтверждается для:

- blocked network;
- missing evidence;
- invalid replacement;
- same-snapshot Diff;
- unsupported version;
- stale snapshot;
- viewport overflow probe;
- focus restoration failure probe.

Любой пропущенный defect, unexpected request, partial invalid replacement, screenshot из failing state, artifact hash drift или repository mutation дает nonzero exit. Если already-installed harness недоступен, task блокируется без установки.

**Evidence:**

- `evidence/phase-0.5-browser-qa.md`;
- `evidence/phase-0.5-acceptance.json`;
- `evidence/phase-0.5-screenshots/375-current.png`;
- `evidence/phase-0.5-screenshots/768-diff.png`;
- `evidence/phase-0.5-screenshots/1280-combined.png`.

**Commit guidance:** screenshots и browser receipts относятся к evidence commit после acceptance.

- [ ] 10. Выполнить Phase 0.5 scope gate и перейти только в verification

**Цель:** доказать completeness и отсутствие scope leakage до независимой final wave.

**References:**

- все Task 1–9 receipts;
- exact allow-list этого плана;
- `TASK_CONTEXT.md`;
- Phase 0 protected artifacts;
- `architecture_widget.html`;
- `maps/architecture-model.baseline.json`;
- `maps/architecture-model.schema.json`.

**Действия:**

- повторно выполнить model validation;
- повторно выполнить deterministic generation;
- повторно выполнить 37 acceptance rows;
- повторно выполнить browser QA;
- подтвердить, что actual browser execution произошло только после generation;
- сравнить historical widget hash;
- сравнить Phase 0 schema/baseline hashes;
- сравнить все pre-existing dirty path statuses/hashes;
- проверить exact changed-path allow-list;
- проверить отсутствие Phase 1 artifacts;
- записать `evidence/phase-0.5-scope-gate.md`;
- обновить только factual/workflow sections `TASK_CONTEXT.md`;
- при полном pass перейти только в `verification`;
- не устанавливать owner acceptance;
- не переводить phase в completed.

**Acceptance criteria:**

- все required Phase 0.5 artifacts существуют;
- no forbidden path changed;
- historical и Phase 0 protected artifacts сохранили hashes;
- generated HTML byte-reproducible;
- Task 6 содержит только pre-generation harness/contract evidence;
- Tasks 8/9 содержат generated-artifact browser evidence;
- все 37 rows pass;
- browser/accessibility checks pass;
- отсутствуют Phase 1 artifacts;
- workflow stage равен `verification`;
- `Phase result acceptance` остается pending.

**Happy-path QA:**

- один deterministic PowerShell scope verifier возвращает exit `0`;
- receipt содержит assertion totals и zero mismatches;
- dependency chronology подтверждает Task 6 probe → Task 7 generation → Tasks 8/9 browser execution.

**Failure-path QA:**

Scope gate блокируется при:

- altered historical hash;
- extra changed path;
- missing acceptance row;
- nondeterministic generated output;
- target-as-current record;
- changed Phase 0 schema/fixture;
- browser execution до Task 7 generation;
- missing Task 6 harness probe;
- missing Tasks 8/9 generated-artifact browser evidence;
- premature owner gate;
- Phase 1 artifact.

**Evidence:**

- `evidence/phase-0.5-scope-gate.md`.

**Commit guidance:** commit запрещён; сначала final verification и owner acceptance.

## Final verification wave

F1–F4 выполняются параллельно, но пишут разные immutable receipts. Они не изменяют implementation artifacts. F5 является единственным агрегатором.

- [ ] F1. Проверить compliance с планом, dependencies и exact allow-list

**References:**

- imported Phase 0.5 plan;
- Task 1 repository snapshot;
- Task 1–10 receipts;
- dependency matrix;
- scope gate.

**Проверки:**

- распарсить все task rows;
- подтвердить наличие evidence для Tasks 1–10;
- подтвердить порядок dependencies;
- подтвердить, что Task 6 не потреблял generated artifact;
- подтвердить Task 7 generation до Tasks 8/9 browser execution;
- повторно получить current changed paths;
- сравнить exact allow-list;
- повторно вычислить protected hashes;
- проверить отсутствие запрещённых Git/tool actions;
- проверить переход в final verification только из `verification`.

**Happy-path QA:**

- все tasks/evidence/dependencies/paths совпадают;
- pre-generation и post-generation QA ownership не пересекаются;
- verdict `APPROVE`.

**Failure-path QA:**

Copied evidence с одним из следующих defects должно дать `REJECT`:

- missing task;
- missing receipt;
- forbidden path;
- hash mismatch;
- dependency inversion;
- Task 6 browser execution generated artifact;
- Tasks 8/9 browser execution до Task 7;
- premature owner transition.

**Write only:**

- `evidence/phase-0.5-final-verification-f1-plan-compliance.md`.

- [ ] F2. Независимо проверить canonical contract и implementation quality

**References:**

- widget schema/model;
- `model-contract.mjs`;
- runtime/generator sources;
- model validation;
- acceptance results.

**Проверки:**

- повторно проверить unique IDs/references/views/snapshots/semantics;
- проверить immutable replacement;
- проверить stable-ID Diff;
- проверить exactly-one runtime payload;
- просканировать hidden historical/schema/Markdown/network sources;
- проверить current/target distinction;
- проверить отсутствие unsupported semantic inference;
- проверить отсутствие historical metrics;
- проверить deterministic ordering и output guards.

**Happy-path QA:**

- все contract and semantic checks проходят;
- verdict `APPROVE`.

**Failure-path QA:**

Должны быть отклонены:

- second source;
- target-as-current;
- duplicate ID;
- mutable model;
- display-name Diff;
- semantic collapse;
- failed replacement с partial visible mutation;
- hidden network fetch.

**Write only:**

- `evidence/phase-0.5-final-verification-f2-contract-quality.md`.

- [ ] F3. Независимо повторить real-browser acceptance и visual QA

**References:**

- generator;
- generated widget;
- Task 6 browser-harness probe;
- Tasks 8/9 browser QA;
- `DESIGN.md`;
- `WA-001`–`WA-037`.

**Проверки:**

- подтвердить, что Task 6 выполнял только availability probe;
- regenerate во временный путь;
- подтвердить byte identity;
- запустить все 37 rows;
- отключить network;
- проверить console/page errors;
- повторить keyboard/focus/live-region/reduced-motion tests;
- повторить atomic replacement и Diff tests;
- повторить 375/768/1280 responsive tests;
- проверить screenshots и visual states.

**Happy-path QA:**

- 37/37 pass;
- zero network/error/overflow defects;
- deterministic output;
- generated-artifact execution происходит после regeneration;
- verdict `APPROVE`.

**Failure-path QA:**

Любое из следующего дает `REJECT`:

- missing acceptance row;
- unexpected request;
- page overflow;
- inaccessible control;
- focus loss;
- repeated stale live announcement;
- nondeterministic output;
- partial invalid-model render;
- pre-generation browser execution claim.

**Write only:**

- `evidence/phase-0.5-final-verification-f3-browser-qa.md`.

- [ ] F4. Проверить scope fidelity, historical preservation и Phase 1 blockade

**References:**

- Task 1 snapshot;
- exact allow-list;
- historical widget;
- Phase 0 protected model/evidence;
- current worktree;
- `TASK_CONTEXT.md`.

**Проверки:**

- сравнить current worktree с Phase 0.5 snapshot;
- recompute historical widget hash;
- recompute Phase 0 schema/baseline hashes;
- проверить production/tests/fixtures/`.smc`;
- проверить commands/config/packages/release paths;
- проверить unrelated owner path statuses/hashes;
- проверить отсутствие Phase 1 plan/implementation;
- проверить, что owner acceptance еще требуется.

**Happy-path QA:**

- protected state неизменен;
- Phase 1 blocked;
- verdict `APPROVE`.

**Failure-path QA:**

Любое из следующего дает `REJECT`:

- protected-path change;
- generated output по historical path;
- package/config/hook change;
- source/test/fixture change;
- Phase 1 artifact;
- owner gate crossed;
- unrelated dirty path altered.

**Write only:**

- `evidence/phase-0.5-final-verification-f4-scope-fidelity.md`.

- [ ] F5. Последовательно агрегировать F1–F4 и остановиться на owner acceptance

**Depends on:** четыре terminal `APPROVE`.

**Действия:**

- прочитать ровно четыре F1–F4 receipts;
- проверить их regular-file identity;
- проверить common phase/snapshot/source metadata;
- проверить distinct receipt ownership;
- проверить terminal verdict syntax;
- проверить assertion totals;
- проверить отсутствие unresolved blocking findings;
- повторно выполнить final allow-list/protected-hash comparison;
- записать aggregate receipt;
- при полном pass перевести `TASK_CONTEXT.md` только в `awaiting-owner-acceptance`;
- при любом failure записать aggregate `REJECT` и оставить workflow в `verification` или `blocked`.

**Acceptance criteria:**

- F1–F4 существуют как distinct regular files;
- все четыре имеют terminal `APPROVE`;
- common metadata совпадает;
- live protected hashes совпадают;
- aggregate verdict `APPROVE`;
- workflow равен `awaiting-owner-acceptance`;
- Phase 0.5 не помечена completed;
- Phase 1 остается blocked.

**Happy-path QA:**

Из repository root выполнить единственный sequential aggregation command:

```powershell
node "docs/architecture-migration/widget/verify-widget.mjs" `
  --suite final-aggregation `
  --receipts `
    "docs/architecture-migration/evidence/phase-0.5-final-verification-f1-plan-compliance.md,docs/architecture-migration/evidence/phase-0.5-final-verification-f2-contract-quality.md,docs/architecture-migration/evidence/phase-0.5-final-verification-f3-browser-qa.md,docs/architecture-migration/evidence/phase-0.5-final-verification-f4-scope-fidelity.md" `
  --snapshot "docs/architecture-migration/evidence/phase-0.5-repository-snapshot.md" `
  --scope-gate "docs/architecture-migration/evidence/phase-0.5-scope-gate.md" `
  --context "docs/architecture-migration/TASK_CONTEXT.md" `
  --expected-stage verification `
  --next-stage awaiting-owner-acceptance `
  --output "docs/architecture-migration/evidence/phase-0.5-final-verification.md"
```

`final-aggregation` обязан выполнить воспроизводимо и в указанном порядке:

1. Разобрать comma-separated list и потребовать ровно четыре canonical paths, совпадающие с F1–F4 allow-list.
2. Для каждого path выполнить `lstat`, потребовать существующий regular file, отклонить symlink/reparse-point/directory и вычислить SHA-256 из bytes того же открытого файла.
3. Потребовать четыре distinct canonical paths, distinct file identities и distinct receipt ownership values `F1`, `F2`, `F3`, `F4`.
4. Из каждого receipt извлечь `phase`, `snapshot_sha`, `source_basis`, terminal verdict, assertion totals и blocking findings.
5. Потребовать одинаковые:
   - `phase: phase-0.5-model-driven-architecture-widget`;
   - execution-time `snapshot_sha`;
   - `source_basis`.
6. Потребовать terminal verdict `APPROVE` ровно один раз в каждом receipt, positive assertion totals и zero unresolved blocking findings.
7. Повторно прочитать Task 1 snapshot и scope gate, пересчитать live status/hash каждого protected pre-existing path и exact changed-path allow-list.
8. Потребовать zero protected-hash/status mismatches, zero forbidden paths и unchanged historical/Phase 0 protected artifacts.
9. До записи проверить, что `TASK_CONTEXT.md` содержит:
   - current phase `phase-0.5-model-driven-architecture-widget`;
   - stage `verification`;
   - owner result acceptance `pending`;
   - Phase 1 blocked/not started.
10. Сформировать aggregate receipt во временном файле в task-owned temp directory, перечитать и проверить его, затем atomically заменить только allow-listed aggregate output.
11. Только после успешной aggregate validation atomically обновить required factual/workflow sections `TASK_CONTEXT.md` с переходом `verification -> awaiting-owner-acceptance`.
12. После update перечитать `TASK_CONTEXT.md` и подтвердить:
   - stage ровно `awaiting-owner-acceptance`;
   - owner result acceptance всё ещё `pending`;
   - Phase 0.5 не `completed`;
   - Phase 1 всё ещё blocked/not started.
13. При любой ошибке до или после temporary aggregate validation не изменять stage; записывать terminal `REJECT` только в aggregate receipt, если безопасная atomic output запись возможна.

Ожидаемый результат:

- command exit `0`;
- четыре regular-file receipts validated;
- четыре distinct receipt identities;
- common metadata match;
- terminal verdicts `4/4 APPROVE`;
- protected mismatches `0`;
- forbidden paths `0`;
- aggregate verdict `APPROVE`;
- aggregate receipt существует по exact output path;
- `TASK_CONTEXT.md` находится ровно в `awaiting-owner-acceptance`;
- owner result acceptance остается `pending`;
- Phase 1 остается blocked.

**Failure-path QA:**

В task-owned temporary directory создать copies F1–F4 и snapshot/context inputs и последовательно повторить тот же command с `--dry-run --expect-reject`, подставляя по одному defect:

- отсутствующий receipt;
- directory или symlink вместо regular file;
- duplicated receipt path/file identity;
- mismatched `snapshot_sha`;
- mismatched `phase` или `source_basis`;
- malformed или duplicated terminal verdict;
- nonpositive/missing assertion totals;
- unresolved blocking finding;
- altered protected hash/status;
- extra forbidden changed path;
- input context со stage не `verification`;
- input context с уже установленным owner acceptance;
- input context с Phase 1 не blocked.

Для каждого probe ожидается:

- invalid condition названа exact path/field;
- aggregate verdict `REJECT`;
- process завершает probe как `rejected-as-expected`;
- canonical F1–F4 receipts не изменяются;
- canonical protected files не изменяются;
- canonical aggregate output не заменяется dry-run данными;
- canonical `TASK_CONTEXT.md` не изменяется;
- переход в `awaiting-owner-acceptance` отсутствует.

После negative probes повторно выполнить happy-path command только на canonical receipts. Любой accepted defect, non-regular receipt, metadata drift, protected-hash mismatch, premature transition или изменение canonical inputs дает nonzero exit и запрещает owner-acceptance transition.

**Write only:**

- `evidence/phase-0.5-final-verification.md`;
- required factual/workflow sections в `TASK_CONTEXT.md`.

## Commit strategy

Это только planning guidance. План не разрешает staging или commits.

После успешной verification и отдельного owner acceptance рекомендуются три path-selective atomic commits.

### Commit 1 — Contract and preservation

```text
docs(architecture): define model-driven widget contract
```

Включить только:

- historical archive copy;
- canonical widget schema;
- canonical widget model;
- `widget/DESIGN.md`.

### Commit 2 — Widget implementation

```text
feat(architecture): generate standalone model-driven widget
```

Включить только:

- model/runtime modules;
- template;
- CSS;
- generator;
- validators;
- reproducible generated HTML.

### Commit 3 — Evidence and workflow

```text
docs(architecture): record phase 0.5 verification
```

Включить только:

- Phase 0.5 evidence;
- screenshots;
- approved Phase 0.5 plan;
- owner-accepted `TASK_CONTEXT.md` transition.

### Commit safeguards

- Не использовать broad `git add`.
- Не включать pre-existing dirty paths.
- Не включать `.omo/`, `.opencode/`, `.codegraph/`, production, tests, publish/installer или user presentations.
- Если migration dossier остается untracked вместе с unrelated material, до отдельного Git authorization не выполнять staging.
- Owner acceptance результата фазы само по себе не разрешает commit; нужен отдельный Git request.

## Success criteria

Phase 0.5 готова к owner acceptance только если одновременно выполнено следующее:

- historical `architecture_widget.html` byte-identical исходному;
- preservation copy имеет тот же SHA-256;
- существует один versioned canonical widget model;
- все шесть views являются filters одного shared model;
- Current и Target семантически разделены;
- target records всегда unimplemented;
- runtime начинает с одного embedded payload;
- replacement принимает только один document;
- invalid replacement атомарно сохраняет последний valid state;
- нет hidden schema/Markdown/audit/historical/second-model/network source;
- Diff использует stable IDs и direction-sensitive semantics;
- invariant violations остаются orthogonal flags;
- combined views не схлопывают edge/source semantics;
- provenance, source basis, fingerprint, confidence, limitations и evidence locators видимы;
- generated HTML standalone и offline;
- generated HTML byte-reproducible;
- Task 6 ограничен design/CSS contract и pre-generation browser availability probe;
- actual generated-artifact browser execution происходит только после Task 7 в Tasks 8/9;
- `WA-001`–`WA-037` дают 37/37 pass;
- все 24 mode/view pairs присутствуют;
- все 12 special states присутствуют;
- keyboard, focus, live-region, reduced-motion и responsive checks проходят;
- zero unexpected network requests;
- zero console/page errors;
- zero page-level horizontal overflow на 375/768/1280;
- F1–F4 независимо дают `APPROVE`;
- F5 дает aggregate `APPROVE`;
- никакой path вне exact allow-list не изменён Phase 0.5;
- protected Phase 0 artifacts не изменены;
- production/tests/fixtures/`.smc` не изменены;
- `TASK_CONTEXT.md` остановлен на `awaiting-owner-acceptance`;
- Phase 1 остается blocked;
- `completed` не устанавливается до явного owner acceptance.

## Rollback boundary

Rollback является path-specific и требует явного owner authorization.

Разрешено:

- удалить только файлы, отмеченные Task 1 ledger как `phase-0.5-created`;
- восстановить только allow-listed pre-existing files по сохранённым pre-execution bytes/hashes;
- сохранить failure receipts, если они нужны для анализа rejection.

Запрещено:

- `git reset`;
- `git clean`;
- broad `git checkout`;
- broad `git restore`;
- изменение unrelated dirty files;
- удаление ignored build output без отдельного подтверждения;
- изменение `.codegraph/`, `.omo/`, user presentations или Phase 0 evidence;
- восстановление historical widget, потому что он вообще не должен изменяться.

Если generated widget не проходит acceptance:

1. Phase 1 остается blocked.
2. Workflow не пересекает owner acceptance.
3. Failure receipts сохраняются.
4. Откат ограничивается Phase 0.5-created/modified allow-listed paths.
5. Любой фактический rollback выполняется только после отдельного решения владельца.

## Completion handoff

После исполнения этого плана должны существовать:

- один canonical widget model;
- один additive Draft 2020-12 widget schema;
- один design contract;
- deterministic generator;
- immutable runtime;
- standalone generated widget;
- 37-row acceptance evidence;
- browser/accessibility/responsive receipts;
- independent F1–F5 verification;
- workflow state `awaiting-owner-acceptance`.

План содержит **10 implementation todos** и **5 final-verification todos** в **4 execution waves**. Он не разрешает выполнение в этой planning-сессии. Реализация должна запускаться отдельно только после прохождения architecture plan review/owner approval через соответствующий worker workflow, например `/architecture-start phase-0.5-model-driven-architecture-widget`.