# Контекст архитектурной миграции

Этот файл является постоянной точкой входа в задачу. Перед любым анализом,
планированием или изменением архитектуры сначала прочитать этот файл, затем
сверить указанные факты с текущим рабочим деревом.

## Репозиторий

- Рабочая директория: `D:\IA\ace v.2`
- Git root подтверждён командой `git rev-parse --show-toplevel`.
- Ветка на момент создания контекста: `master`, tracking `origin/master`.
- Аудит из `architecture_audit.md` первоначально выполнялся для
  `D:\IA\ace`, поэтому его метрики являются гипотезами до повторной проверки
  в `ace v.2`.
- Рабочее дерево уже содержит пользовательские изменения. Не включать их в
  архитектурные изменения и не откатывать без прямого разрешения владельца.

## Цель

Без изменения наблюдаемого поведения привести WPF/MVVM-приложение к модели,
в которой `ProjectSession` является aggregate root текущего проекта, а
каноническое состояние разделено на явные срезы:

```text
ProjectSession
|- Project lifecycle / identity / dirty state / restore guard
|- ClimateState
|- ConstructionState
|- ThermalState
`- HydraulicsState
```

`ProjectSession` не должен становиться новым god object. ViewModels остаются
WPF-адаптерами, application services не зависят от ViewModels, Results является
производной проекцией, а существующий формат `.smc` сохраняется.

## Обязательные архитектурные представления

Нужно поддерживать шесть логических карт одной системы:

1. **Compile-time** - проекты, namespace, типы, интерфейсы и ссылки.
2. **DI/runtime** - регистрации, lifetime, экземпляры и порядок создания.
3. **State** - canonical owner, копии, writers, readers и будущий owner.
4. **Reactive** - события, `PropertyChanged`, команды, подписки, recalculation
   и dirty-state.
5. **Persistence** - чтение, версия, преобразование, validation, restore,
   save и backup для `.smc` и связанных форматов.
6. **User flow** - действия пользователя и наблюдаемый результат для new,
   load, second load, edit, calculate, reset, save/reload и export.

Это не шесть независимых приложений. В виджете они должны быть шестью
фильтрами над общим набором узлов и типизированных связей.

## Обязательная страховочная сетка

До переноса владельцев состояния нужны characterization tests как минимум для:

- создания нового проекта;
- загрузки существующего и legacy `.smc`;
- загрузки второго проекта после первого;
- изменения climate, construction, thermal и hydraulics inputs;
- invalidation и точного количества перерасчётов;
- reset и повторных reset/load без размножения обработчиков;
- save и повторной загрузки;
- summary, PDF и других export flows;
- dirty-state, load guard и навигации.

Тесты должны фиксировать не только конечные значения, но и количество событий,
перерасчётов и обновлений Results, отсутствие stale state и правильный
dirty-state.

## State inventory

Для каждого значимого значения фиксировать:

| Поле | Содержание |
|---|---|
| State | Значение или группа данных |
| Current canonical owner | Текущий авторитетный источник |
| Copies / projections | Кэши, UI-проекции и дубли |
| Writers | Кто может изменять значение |
| Readers | Кто его читает |
| Reactive effects | Какие события и invalidation запускаются |
| Persistence | В каком поле и формате сохраняется |
| Target owner | Будущий state-срез внутри `ProjectSession` |
| Migration status | Legacy / seam / migrated / legacy removed / verified |

Нельзя завершать миграцию с двумя writable canonical stores. Переходный dual
path допустим только внутри короткого компилируемого этапа и должен быть явно
помечен как архитектурный риск.

## Подтверждённые текущие наблюдения

- По явному разрешению владельца установлен глобальный `csharp-ls 0.16.0`,
  совместимый с текущим .NET SDK 8.0.418; `lsp_status` определяет C# server как
  installed. Внешний `lsp_diagnostics` harness всё ещё ошибочно выбирает
  workspace root `C:\Users\Admin`, поэтому C# correctness gates остаются за
  прямыми `dotnet build/test`, когда production C# входит в разрешённый scope.
- Codegraph доступен для анализа символов и связей.
- Phase 2 Task 6 принят 2026-08-07: `ClimateViewModel` больше не вызывает
  `IMarkDirtyService.MarkDirty()` или `CalculationContext.UpdateClimate()`
  напрямую, user city/scalar/reset mutations идут через
  `ProjectSession.ClimateState`, legacy test/helper constructor сохраняет старые
  call sites без хранения dirty/context services во ViewModel, а evidence
  записан в
  `docs/architecture-migration/evidence/phase-2-climate-state/climate-viewmodel-adapter.md`.
- Planning-only live inspection для `phase-2-climate-state` подтвердила текущий
  Climate ownership seam: `ClimateViewModel` хранит writable UI state, напрямую
  ставит dirty, публикует собственный `DataChanged`, копирует значения в
  singleton `ClimateData` и затем вызывает `CalculationContext.UpdateClimate`.
  `ProjectLoadOrchestrator` восстанавливает климат прямыми присваиваниями в
  concrete ViewModel под `BeginLoadProject`/`EndLoadProject`, а
  `ResultsViewModel.SaveCurrentProject()` строит `.smc` climate snapshot также
  из ViewModel. DI регистрирует `ClimateViewModel`, `IClimateData`,
  `CalculationContext`, `ProjectSession` и `ProjectLoadOrchestrator` как
  singleton. Существующие Climate tests покрывают значения, формулы, reset и
  singleton sync, но не дают полного доказательства exact dirty/event/context/
  recalculation multiplicity, mutation origins, logical-action grouping,
  repeated load/reset subscription hygiene и отсутствия bypass writers.
- `ResultsViewModel` всё ещё хранит прямые ссылки на четыре module ViewModels и
  вручную очищает большое производное состояние в `Reset()`.
- `CircuitsViewModel` содержит значимый reactive-протокол событий, коллекций,
  dirty-state и recalculation.
- `ConstructionRepository` имеет compile-time зависимость от
  `Services.Construction` ради `MaterialNotFoundException`; это type-level
  coupling, а не доказательство runtime-вызова construction service.
- Historical architecture claims reconciled against the current working tree:
  45 rows classify 15 claims as `confirmed`, 8 as `changed`, 18 as
  `not-reproducible`, and 4 as `not-applicable`; 35 material claim keys have
  explicit bindings. Historical cycle counts `14` and `8` remain
  `not-reproducible`, while qualitative compile-time coupling is recorded
  separately.
- Task 1 for `phase-0.5-model-driven-architecture-widget` is independently
  confirmed after the narrow list typo correction. Receipt
  `docs/architecture-migration/evidence/phase-0.5-repository-snapshot.md`
  records snapshot HEAD `f0d19c34ac03075d64548f1059e9c6626d3596b5`, normalized
  self-hash `780C5339EF661216B632C6955D25F5230DBB7D769B52AF12F2C244C02B8BE4C1`,
  62 protected rows, 10 tracked, 52 untracked, 60 present, 2 deleted, and
  exactly one Task 1-created receipt.
- Task 1 detection-only probes did not find browser modules or browser
  executables in the current execution boundary. This is a recorded baseline
  fact, not a waiver: Task 6 remains the authoritative browser harness gate.
- Task 2 for `phase-0.5-model-driven-architecture-widget` is independently
  confirmed after correcting the inaccurate all-62-hashes scope claim to the
  mandatory post-Task-1 `TASK_CONTEXT.md` workflow update. Source and archive
  are byte-identical at 37294 bytes with SHA-256
  `D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA`.
  The historical source and archived copy are provenance-only artifacts, not
  runtime or generator inputs. The generated widget path remains absent after
  Task 2, and Task 6 remains the authoritative browser harness gate.
- Task 3 for `phase-0.5-model-driven-architecture-widget` is independently
  confirmed after an initial verifier rejection and correction lane. Canonical
  validator exit is `0`; accepted model counts are
  `79/112/27/22/5/112/11/15/6/3`, global IDs `280`, exact views
  `compile-time`, `di-runtime`, `persistence`, `reactive`, `state-ownership`,
  `user-flow`, accepted invariants `15/15`, and deferred decisions `6/6`.
  Schema/model SHA-256 are
  `7afbce3cfadc8b77443462e52d0bb15c7bdd31569de41fadcaae941791b07194` and
  `ee9e0069d71f3abffa5fa1e9b698a97507694597fbe85b84c272c30d3f55efbd`.
  Real-CLI mutations reject `53/53`; generic Draft 2020-12 validation remains
  honestly `degraded` because no package validator is installed or was
   installed.
- Amendment Task 3 and Task 4 are independently confirmed on the live v2
  artifacts. Task 3 verifier session `ses_0433e179cffe1loy2UknSQJdFc`
  confirms `33` assertions and `21` mutations; Task 4 verifier session
  `ses_0431ae962ffeusBzpV3RQl11wc` confirms `47` assertions and `20`
  mutations plus `78` direct assertions. The Task 5 scope gate independently
  re-ran both suites, reconstructed the accepted retry-1 `74`-row, `4830`-byte
  porcelain basis with SHA-256
  `2C23AEA259D3233DD9FD8B959C29951EB259E795CD81A3B34BDAADD3DC6FDC96`,
  and found protected mismatches `0` and forbidden changed paths `0`.
- Владелец выбрал reviewed correction с canonical entrypoints
  `v2-amendment-f1` through `v2-amendment-f5`; это разрешает только planning,
  не изменение verifier-а. Planning-only Metis pre-analysis завершён в session
  `ses_04251de93ffeVZCamLK6AdVNll` и рекомендует отдельный узкий correction
  overlay: сохранить parent Phase 0.5 и approved v2 amendment как authority,
  не переоткрывать принятые amendment Tasks 1-5, не менять F1-F5 criteria и
  ограничить будущую реализацию canonical dispatch в `widget/verify-widget.mjs`,
  versioned correction evidence и factual context transition. Следующий gate -
  отдельная primary Prometheus Plan Mode session; verifier implementation,
  amendment F1-F5, original Task 5 и Phase 1 по-прежнему не авторизованы.
- Phase 0.5 Task 4 implementation exists in five allow-listed paths and its
  self-suites report `37` pass rows, but it is not independently accepted.
  Executor session `ses_04744edb2ffelVnSBNoZwmyW4b` reached verifier session
  `ses_047288711ffedvSwSIvF0lpUA3`, which returned final `VERDICT: REJECTED`.
- Current blocking findings for rejected Task 4 are canonical: the v1
  one-record top-level-values contract plus `snapshots` membership cannot
  honestly represent different current and target canonical values for one
  stable ID; runtime `diff()` currently has an unchanged/unchanged branch and
  cannot emit `changed`; the validator permits the same ID in one group across
  disjoint snapshots while runtime `by_id` overwrites one representation; the
  acceptance suite has no real changed assertion; and valid-empty/no-match
  semantics still have a verifier-reported defect.
- Protected schema/model hashes remain unchanged after the rejected Task 4
  verification: schema SHA-256
  `7afbce3cfadc8b77443462e52d0bb15c7bdd31569de41fadcaae941791b07194`, model
  SHA-256 `ee9e0069d71f3abffa5fa1e9b698a97507694597fbe85b84c272c30d3f55efbd`.
  Canonical Task 3 CLI remains green.
- Architecture Explorer MVP Task 1 создана и независимо подтверждена. Четыре
  implementation artifacts формируют один автономный local/offline HTML через
  accepted `createState` loading/validation boundary. Два generation passes
  дали byte-identical `383453`-byte output с SHA-256
  `A1FA7F599A0DEC0607DDC10E97DABEE49C69100E4EE1693577B202150EB47824`;
  exactly-one payload парсится и содержит accepted model identity. Isolated
  missing-input probe завершился nonzero и сохранил sentinel output; historical
  underscore HTML остался `37294` bytes с SHA-256
  `D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA`.
  TypeScript LSP не установлен по ранее зафиксированному отказу, поэтому syntax
  gate выполнен через `node --check`.
- Architecture Explorer MVP Task 2 технически завершена и independently
  confirmed. Canonical generated artifact
  `docs/architecture-migration/architecture-widget.html` после двух
  owner-requested visual corrections имеет ровно `13981737` bytes и SHA-256
  `B3D5DBF5B7FCCC9F6ED67E46CD7068B23D8BF40769E3035089FA9D15009D1C74`; внутри
  ровно один payload. Независимо подтверждены Current/Target/Diff runtime parity,
  ровно шесть views и все `63` непустые combinations без duplicate IDs,
  stable-ID Diff fields/reasons/invariant markers, honest Target
  unimplemented/valid-empty semantics, пять supported classes включая zero-count
  classes, controlled state hooks и standalone local/offline boundary без Task
  3/4 scope leak. Historical underscore HTML остаётся provenance-only artifact:
  `37294` bytes, SHA-256
  `D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA`.
  Isolated failure preserves existing output and cleans temp files. Current
  accepted v2 schema/model hashes:
  `8A0BC79C00FBD8F1D2C2E52E70085DF0472E3675B99B9C5EC9209FA8EEB4C97B` и
  `F573E175C28AA9BEB9DD1809EB49E34B41F182BA2742132E93C40639804EFA97B`; значения
  `7AFB...` и `EE9E...` из
  `docs/architecture-migration/evidence/phase-0.5-model-validation-v2.md`
  являются historical v1 source hashes, а не current v2 hashes. Earlier verifier
  session `ses_03c84fa39ffeGij7yNJ762JHuy` честно сохраняется как `REJECTED`
  zero-assertion incomplete verdict only because it incorrectly treated those
  historical v1 hashes as current. Этот verdict superseded by fresh full
  verification: verifier session `ses_03c773098ffeQbTL4jjvY05kCB`, final
  `CONFIRMED`, `101932` assertions. TypeScript LSP недоступен из-за ранее
  отклонённой установки; `node --check` passed.

Все метрики, количества файлов, циклов, тестов и LOC должны пересчитываться.
Не копировать их из старого аудита без проверки.

## Последовательность работ

```text
Baseline и reconciliation старого аудита
-> шесть карт и state inventory
-> characterization tests и persistence fixtures
-> target invariants и architecture validator
-> ProjectSession shell и узкие state contracts
-> Climate vertical slice
-> Construction vertical slice
-> Thermal vertical slice
-> Hydraulics vertical slice
-> Project snapshot/save boundary
-> Project restore coordinator
-> Results projection
-> удаление legacy owners и DI cleanup
-> полный user-flow и release gate
```

Production-срезы выполняются последовательно одним implementation lane.
Параллельно допустимы только исследования, fixtures, независимые tests и QA,
не затрагивающие центральные файлы состояния, DI, load/reset и Results.

## Ворота каждой фазы

Фаза завершена только после:

1. Targeted tests.
2. Связанных integration tests.
3. Проверки архитектурных инвариантов.
4. Обновления виджета и evidence.
5. `dotnet build`.
6. Полного `dotnet test` на фазовом gate.
7. Ручного или автоматизированного прогона затронутого user flow.

## Файлы задачи

Ожидаемые входные файлы в этой папке:

- `architecture_widget.html` - исходный интерактивный виджет;
- `architecture_audit.md` - исходный аудит, требующий reconciliation;
- `TASK_CONTEXT.md` - этот постоянный контекст.

Подкаталоги:

- `maps/` - шесть архитектурных карт и state inventory;
- `evidence/` - receipts сборки, тестов, Codegraph и user-flow QA;
- `plans/` - утверждённые планы фаз и rollback boundaries;
- `archive/` - предыдущие сгенерированные версии виджета и baseline snapshots.

## Правила обновления контекста

После каждого существенного решения или завершённой фазы обновить:

- `Текущий статус`;
- `Принятые решения`;
- `Открытые вопросы`;
- `Следующее действие`;
- ссылки на evidence и актуальную версию виджета.

Не заменять историю молча: существенные изменения решения кратко записывать в
журнал с датой и причиной.

## Текущий статус

- Phase 0 baseline dossier выполнена, прошла независимые F1-F5 verification
  gates с итоговым `APPROVE` и явно принята владельцем 2026-07-31.
- Workflow Phase 0 завершён; production-код, tests, fixtures, `.smc`, runtime
  configuration, release artifacts и текущий `architecture_widget.html` в этой
  фазе не изменялись.
- Phase 1 не запущена, автоматически не планируется и остаётся заблокированной,
  пока `phase-0.5-model-driven-architecture-widget` не будет завершена и
  отдельно принята владельцем.
- По решению владельца перед Phase 1 выделена отдельная
  `phase-0.5-model-driven-architecture-widget`; владелец явно разрешил её
  исполнение 2026-07-31 через `/architecture-start`; Task 1, Task 2 и Task 3
   завершены и независимо подтверждены; Task 4 attempted, but final
   independent acceptance failed, so workflow must remain blocked pending an
   explicit owner decision.
- Канонический план Phase 0.5 импортирован из отдельной primary Prometheus
  session, исправлен по замечаниям Momus и прошёл повторный Sisyphus/Momus
  review. Владелец явно утвердил план 2026-07-31, затем отдельным
  `/architecture-start phase-0.5-model-driven-architecture-widget`
  авторизовал исполнение; authorization gate повторно подтверждён, workflow
  перешёл в `executing`.
- Phase 0.5 Task 1, capture the execution-time repository/tool/dirty-worktree
  boundary, завершён и независимо подтверждён после узкого исправления typo в
  verifier list. Final verifier verdict: confirmed with high confidence;
  `Stage` остаётся `executing`, `Phase result acceptance` остаётся `pending`,
  а Phase 1 остаётся blocked/not started до полного завершения и отдельного
  owner acceptance всей Phase 0.5.
- Phase 0.5 Task 2, preserve the historical architecture widget before any
  generated-artifact work, завершён и независимо подтверждён после исправления
  receipt scope: первоначально была отклонена только неточная формулировка про
  all-62-hashes, затем receipt был исправлен и финально re-verified. Final
  verifier verdict: confirmed with high confidence; `Stage` остаётся
  `executing`, `Phase result acceptance` остаётся `pending`, Phase 1 остаётся
  blocked/not started; после Task 2 execution переходил к Task 3.
- Phase 0.5 Task 3, define the canonical widget model/schema/validator,
  завершён и независимо подтверждён после первоначального verifier rejection и
  correction lane. Initial executor session
  `ses_047a23911ffeMsVaDF5W1g94LF`, correction executor session
  `ses_0477d57c1ffe5g1inrp3pd23nL`, verifier session
  `ses_0479222f7ffeeS4phaES1DoAuT`; final `VERDICT: CONFIRMED`, high
  confidence. Task 3 write-set был пять artifacts: widget schema, widget model,
  `widget/model-contract.mjs`, `widget/verify-widget.mjs` и validation receipt;
  correction lane затронул только validator/receipt. Historical HTML остаётся
  provenance-only. `Stage` остаётся `executing`, `Phase result acceptance`
  остаётся `pending`, Phase 1 остаётся blocked/not started, а execution
  переходит к Task 4.
- Phase 0.5 Task 4, pure immutable runtime loading, indexes, atomic
  replacement, modes, six-view union, filters и stable-ID directional Diff,
  был attempted и реализован в пяти allow-listed paths, self-suites report
  `37` pass rows, но независимую приёмку не прошёл. Executor session
  `ses_04744edb2ffelVnSBNoZwmyW4b`; verifier session
  `ses_047288711ffedvSwSIvF0lpUA3`; final `VERDICT: REJECTED`. Primary blocker:
  v1 contract с one-record top-level values и `snapshots` membership не может
  честно представить разные current и target canonical values для одного stable
  ID. Additional blocking findings: runtime `diff()` сейчас имеет
  unchanged/unchanged branch и не может emit `changed`; validator допускает
  same ID в одной группе при disjoint snapshots, пока runtime `by_id`
  перезаписывает одну representation; acceptance suite не содержит real changed
  assertion; valid-empty/no-match semantics имеют verifier-reported defect.
  Protected schema/model hashes unchanged, canonical Task 3 CLI remains green,
  but Task 4 is not accepted. Workflow must transition from `executing` to
  `blocked`; `Phase result acceptance` remains `pending`; Tasks 5-10, F1-F5 and
  Phase 1 stay blocked until an explicit owner decision chooses the next
  contract path.
- Владелец явно выбрал направление `Перейти на v2 (Recommended)` для Phase 0.5
  Task 4 contract blocker. Каноническое направление теперь: v2 major contract с
  `contract_version=2.0.0`, одним unique stable ID на запись, одним runtime
  document, одним `snapshot_states` source of snapshot presence/values, без
  параллельного `snapshots` membership source of truth, с model-level canonical
  diff policy и честными состояниями `added`/`removed`/`changed`/`unchanged`/
  `unresolved`. Так как этот contract ещё не внешне выпущен, v1 compatibility
  shim не требуется. Этот owner choice authorizes planning only: schema/model/
  runtime пока не редактируются, Task 4 не считается complete, Tasks 5-10,
  F1-F5 и Phase 1 не стартуют. Tasks 3 и 4 могут быть formally reopened только
  после decision-complete amendment, его review и отдельного owner approval plus
  execution authorization через существующие owner gates.
- Planning-only Metis pre-analysis для v2 amendment завершён в session
  `ses_046bb4fecffeqVxPgX7SXpsiF9`; metadata подтверждает identity
  `Metis - Plan Consultant`. Анализ зафиксировал major v2 boundary, один stable
  ID и один `snapshot_states` source of truth, per-kind identity/snapshot fields,
  snapshot-local edge/flow validation, честную Target-модель без выдуманного
  implementation status, исполняемый `canonical_diff_fields`, сохранение v1
  evidence как provenance и совместное reopening Tasks 3-4 только после review,
  owner approval и отдельной execution authorization. Workflow готов только к
  отдельной primary Prometheus Plan Mode session; implementation по-прежнему
  не авторизована.
- Владелец явно утвердил reviewed v2 amendment plan с SHA-256
  `B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536`.
  Утверждение не является execution authorization. Дополнительный обязательный
  stop-rule владельца: если во время исполнения появляется подтверждённая
  невыполнимость плана, выполнение немедленно прекращается, workflow переводится
  в `blocked`, факты и blocker фиксируются без дальнейших edits, а владельцу
  предлагаются варианты решения; запрещено молча импровизировать, ослаблять
  acceptance criteria или расширять scope.
- 2026-08-01 владелец остановил дальнейшее усложнение verification framework и
  поставил workflow на паузу. Canonical model v2, runtime и принятые amendment
  Tasks 1-5 сохраняются как фундамент. Reviewed correction для
  `v2-amendment-f1`-`v2-amendment-f5` не утверждается и переводится в
  `deferred`; его исполнение, F1-F5 и original Phase 0.5 Task 5 не запускаются.
  Новый planning-only приоритет - короткая Phase 0.5 Architecture Explorer MVP:
  сначала получить открываемый локальный HTML с Overview, ProjectSession,
  Migration/Diff и evidence drill-down, провести ручную приёмку по восьми
  owner-вопросам и только затем решить, какая минимальная автоматическая
  верификация действительно нужна. Это решение не является authorization на
  создание плана, HTML или изменение production-кода.
- Architecture Explorer MVP Task 2 generated-artifact stage технически завершена,
  independently confirmed и получила explicit owner visual `PASS` сообщением
  `отлично task 2 утверждаю` 2026-08-03. Владелец одновременно явно остановил
  переход к Task 3 сообщением `пока 3 не начинаем`, поэтому `Stage = blocked`
  как безопасная пауза, а `Phase result acceptance` остаётся `pending`. Phase 1,
  correction и `v2-amendment-f1` through `v2-amendment-f5` остаются
  blocked/deferred. По запросу владельца карточка
  `Выбранная проекция` теперь занимает всю 12-колоночную сетку, а принудительные
  `min-width` таблицы и горизонтальная прокрутка удалены; browser-check на
   `375/768/1280` подтвердил отсутствие page/table overflow.
- Владелец 2026-08-03 явно снял паузу перед Architecture Explorer MVP Task 3
  сообщением `csharp-ls, потом приступаем к Task 3`. Совместимый `csharp-ls
  0.16.0` установлен без изменения project packages или .NET SDK. Workflow
  возвращён в `Stage = executing` только для утверждённого Task 3: экран
  ProjectSession, core flows и evidence drill-down. Task 4, Phase 1, correction
  и amendment F1-F5 остаются blocked/deferred; после Task 3 обязателен отдельный
  owner visual checkpoint.
- Architecture Explorer MVP Task 3 технически завершена и визуально принята
  владельцем 2026-08-03 exact message `фиксируем`. Canonical standalone
  `docs/architecture-migration/architecture-widget.html` после двух
  byte-identical generation passes имеет `14073817` bytes и SHA-256
  `F0E9F32EB0872BC504DF935D9BF96C037CE9D9523CAD4E8558A521853EE5FF65`.
  Direct assertions подтвердили один payload, lifecycle `ST-001..ST-005`, четыре
  slices с `ST-006..ST-019`, outside-root `ST-020..ST-027`, восемь boundary
  invariants, ровно восемь flow groups с 22 unique `CF-001..CF-022`, полный
  drill-down catalog `11/3/15/6`, разрешённые references и отсутствие synthetic
  ProjectSession model record. Browser QA на exact `375/768/1280` подтвердила
  отсутствие root/screen overflow, console/page errors, warnings и внешних
  requests; mouse/keyboard navigation и сохранение Overview state работают.
  Fresh independent visual QA session `ses_039e0c59cffeu3Ccq64vsVQPiZ` вернула
  `PASS`, high confidence, без blockers. Accepted model/schema/runtime/validator
  и historical underscore HTML остались read-only; Task 4 не начиналась.
- Architecture Explorer MVP Task 4 технически и визуально завершена. Canonical
  standalone `docs/architecture-migration/architecture-widget.html` после двух
  byte-identical generation passes имеет `14279246` bytes и SHA-256
  `11E7BDC33E5BC03AA09553844234263FC311EADD29D3CD9ECB0F1D711C6B6ACF`.
  Exact `14/14` checks и четыре isolated failure probes прошли; browser QA для
  Overview, ProjectSession и Migration на `375/768/1280` подтвердила один
  активный screen, `aria-current=page`, отсутствие horizontal overflow, чистую
  console и единственный local HTTP `200` request. Fresh image-capable visual
  inspection и source/functional Oracle session
  `ses_038d77042ffePHmGYUUrqGbLIV` дали `PASS` без blockers. Acceptance receipt:
  `docs/architecture-migration/evidence/phase-0.5-architecture-explorer-mvp-acceptance.md`.
  Восемь owner questions остаются `Pending`; это техническое завершение не
  является owner result acceptance и не разблокирует Phase 1.
- Владелец 2026-08-03 ответил `YES` на все восемь owner questions и exact
  statements `YES по всем 8 вопросам. Результат Phase 0.5 Architecture Explorer
  MVP принимаю.` и `Phase 0.5 принята.` явно принял результат Architecture
  Explorer MVP. Acceptance зафиксирована в
  `docs/architecture-migration/evidence/phase-0.5-architecture-explorer-mvp-acceptance.md`.
  Это завершает только `phase-0.5-architecture-explorer-mvp`; Phase 1 не начата
  и не авторизована.
- Planning-only Metis pre-analysis для `phase-1-project-session-shell` завершён
  в session `ses_037db6f56ffe7zEyJPEiRLvk4t`; metadata подтверждает identity
  `Metis - Plan Consultant`. Анализ подтвердил текущие lifecycle/state seams,
  риск двух writable owners, необходимость live reinspection dirty relevant
  files, characterization для new/load/second-load/reset/dirty/failure flows и
  узкую границу Phase 1 без migration Climate, Construction, Thermal или
  Hydraulics ownership. Production code, tests, maps и widget не изменялись.
- Separate primary Prometheus session `ses_037ba1831ffep3dtJaDlFJeHVR`
  подготовила decision-complete Phase 1 plan. После Sisyphus finding о неверном
  `$start-work` handoff и первого Momus `REJECT` из-за отсутствующего decision
  ledger та же primary session встроила exact `IProjectSession` API/restore
  lease semantics и обязательные `/architecture-approve` затем
  `/architecture-start` gates. Исправленный source byte-identically импортирован
  в `docs/architecture-migration/plans/phase-1-project-session-shell.md`,
  SHA-256 `011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948`.
  Continued Momus session `ses_0379213acffeHdyOTC2EpH6I3K` вернула `[OKAY]`.
- Владелец 2026-08-04 явно утвердил reviewed Phase 1 plan SHA-256
  `011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948`.
  Workflow переведён только в `approved`; execution authorization остаётся
  `pending`, и Phase 1 может быть запущена только отдельной командой
  `/architecture-start phase-1-project-session-shell`.
- Владелец 2026-08-04 отдельной явной командой
  `/architecture-start phase-1-project-session-shell` авторизовал исполнение
  reviewed Phase 1 plan. Authorization gate повторно подтвердил current phase,
  `Stage = approved`, owner plan approval, exact active plan SHA-256, terminal
  Momus `[OKAY]`, реальные Metis/Prometheus/Momus identities, Git root и
  защищённый dirty worktree. Workflow переведён в `executing`; до завершения
  Task 1 разрешён только read-only baseline capture и новые Phase 1 evidence.
- Phase 1 Task 1 baseline artifacts существуют в
  `docs/architecture-migration/evidence/phase-1-project-session-shell/`:
  `baseline.md`, `baseline-git-status.bin`, build/test logs и post/final status
  binaries. `baseline.md` заявляет `status: PASS`, Debug/Release build exit `0`,
  filtered lifecycle tests `78/77/1`, full Release tests `1538/1537/1` и final
  symmetric protected-drift comparison `protected_removed=0` /
  `protected_added=0`. Узкая read-only финализация владельцем ограниченного
  вопроса `247 vs 248` подтвердила factual off-by-one в исходном receipt:
  `baseline-git-status.bin` имеет 247 NUL bytes, 248 split parts из-за trailing
  empty part, 247 non-empty status records, 0 rename/copy companion path records,
  статусы `216` modified, `2` deleted, `29` untracked, из них 1 Phase 1 evidence
  record и 246 pre-existing protected records. `baseline.md` исправлен только в
  этих factual count строках; production code и tests в Phase 1 всё ещё не
  изменялись этой execution lane.
- Phase 1 execution is BLOCKED as of 2026-08-05. A read-only audit found a
  blocking defect in `ProjectSession.BeginProjectRestore()`: nested calls return
  the same `_currentLease`, so disposing the inner and outer scopes can leave
  restore depth at `1` and `IsLoadProjectInProgress == true`. This invalidates
  prior GREEN/accepted claims for Tasks 4, 6, 8 and 9 despite green suites; Task
  10 dossier/widget updates are stopped and must not be treated as accepted.
  Current status is explicitly desynchronized: previous `TASK_CONTEXT.md` entries
  claimed Task 9/Task 10 GREEN and pointed to F1-F4, while the active plan must be
  returned to a blocked/re-evaluation state before any final wave. No F1-F4 final
  verification is authorized until a regression test proves the nested restore
  bug, the lease implementation is minimally fixed, targeted/affected/
  persistence/build gates pass again, and Tasks 4-9 are re-evaluated.

## Workflow State

Этот раздел является каноническим состоянием управляющей цепочки. Команды
OpenCode обязаны обновлять его после каждого завершённого шага и читать перед
продолжением работы.

| Поле | Значение |
|---|---|
| Current phase | `phase-2-climate-state` |
| Stage | `executing` |
| Active plan | `docs/architecture-migration/plans/phase-2-climate-state.md` |
| Active plan SHA-256 | `D79D7C46CA73EBCFF161164FC41EE3EB041B9B172631944CB611FA42BA998A6B` |
| Parent phase | `phase-1-project-session-shell` |
| Parent plan | `docs/architecture-migration/plans/phase-1-project-session-shell.md` |
| Parent plan SHA-256 | `011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948` |
| Parent execution state | `completed and explicitly accepted by owner 2026-08-05; acceptance authorizes neither Phase 2 plan approval nor execution` |
| Metis analysis | `completed; identity validated as Metis - Plan Consultant; six-view Climate seams, single-owner and dual-write risks, DEC-001 narrow compatibility seam, mutation/completion/origin contract, characterization gates, protected dirty paths, scope exclusions and stop conditions analyzed` |
| Metis session ID | `ses_02e61739dffenW8itUC4j80ikX; metadata reports only Metis - Plan Consultant` |
| Planning draft | `complete: 12 implementation todos, F1-F4, explicit ClimateState contract, characterization-first gates, protected baseline, v1.0/v1.1 compatibility, rollback/stop rules and separate owner/execution gates` |
| Primary planning session ID | `ses_02e464159ffeLYpJkhNkBJErT1` |
| Primary planning receipt | `valid: metadata reports Prometheus - Plan Builder plus technical compaction in separate session ses_02e464159ffeLYpJkhNkBJErT1; owner approved artifact writing only; final .omo source was imported byte-identically to the repository plan at SHA-256 D79D7C46CA73EBCFF161164FC41EE3EB041B9B172631944CB611FA42BA998A6B` |
| Child planning discovery | `none for Phase 2; child task plans remain invalid primary receipts` |
| Child planning receipt | `not applicable` |
| Sisyphus review | `PASS on exact imported SHA: Climate-only scope, single-owner boundary, explicit origin/completion/no-op contract, sequencing, persistence guardrails, dirty-worktree protection, executable QA and separate owner gates are consistent` |
| Momus review | `OKAY on exact .omo plan source; production/test/dossier references exist and match their stated roles; each todo and final wave has executable QA` |
| Momus session ID | `ses_02d0d549affeUPch83Lkox97qE; identity validated as Momus - Plan Critic` |
| Owner plan approval | `approved by owner 2026-08-05 for reviewed Phase 2 plan SHA-256 D79D7C46CA73EBCFF161164FC41EE3EB041B9B172631944CB611FA42BA998A6B` |
| Execution authorization | `approved by owner 2026-08-05 via explicit /architecture-start phase-2-climate-state after live authorization-gate revalidation` |
| V2 amendment Metis analysis | `completed; identity validated as Metis - Plan Consultant; major v2, one stable ID, one snapshot_states source, per-kind identity/snapshot fields, snapshot-local edge/flow validity, honest Target, executable canonical_diff_fields, provenance and reopening gates analyzed` |
| V2 amendment Metis session ID | `ses_046bb4fecffeqVxPgX7SXpsiF9` |
| V2 amendment plan | `docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget-v2-amendment.md` |
| V2 amendment plan SHA-256 | `B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536` |
| V2 amendment draft | `complete corrected Markdown imported byte-identically from the separate primary Prometheus planning artifact; 5 sequential implementation tasks, F1-F5, exact supersession/scope/rollback and owner gates present` |
| V2 amendment primary planning session ID | `ses_046a6bb36ffe6zQIErApbaCXid` |
| V2 amendment primary planning receipt | `valid: metadata reports Prometheus - Plan Builder; final response identifies the complete planning artifact; imported target is 30476 bytes and SHA-256-identical to the reviewed source` |
| V2 amendment Sisyphus review | `PASS; imported artifact is narrowly scoped to reopened Tasks 3-4, decision-complete, executable, history-preserving, and retains owner approval/execution gates` |
| V2 amendment Momus review | `OKAY on exact SHA-bound source; session ses_04677ca75ffeOkpw7mGdz9x2cv; imported artifact is byte-identical` |
| V2 amendment Oracle review | `OKAY on exact SHA-bound source; session ses_04677334bffekGA8FIsTxVdXuf; imported artifact is byte-identical` |
| V2 amendment owner approval | `approved by owner 2026-08-01 for plan SHA-256 B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536; execution stop-rule requires immediate blocked transition and owner options if plan infeasibility is confirmed` |
| V2 amendment execution authorization | `approved by owner 2026-08-01 via fresh explicit /architecture-start phase-0.5-model-driven-architecture-widget-v2-amendment invocation after workflow correction and live authorization-gate revalidation` |
| V2 amendment completion overlay | `Tasks 1-4 technically accepted; Task 5 scope PASS at docs/architecture-migration/evidence/phase-0.5-v2-amendment-scope-gate.md. F1-F5 are unstarted and blocked because verify-widget.mjs has no v2-amendment-f1..f5 suite entrypoints. Amendment plan/hash remain historical overlay metadata, not active workflow authority.` |
| V2 amendment Task 3 confirmation | `CONFIRMED: ses_0433e179cffe1loy2UknSQJdFc; 33 assertions, 21 mutations.` |
| V2 amendment Task 4 confirmation | `CONFIRMED: ses_0431ae962ffeusBzpV3RQl11wc; 47 assertions, 20 mutations, plus 78 direct assertions.` |
| Verifier entrypoint correction direction | `owner selected reviewed canonical v2-amendment-f1..f5 entrypoint correction; planning only, no verifier implementation authorization` |
| Verifier entrypoint correction Metis analysis | `completed; separate narrow correction overlay recommended, preserving accepted amendment Tasks 1-5, original Phase 0.5 authority, immutable historical receipts, unchanged F1-F5 criteria and separate owner approval/execution gates` |
| Verifier entrypoint correction Metis session ID | `ses_04251de93ffeVZCamLK6AdVNll` |
| Verifier entrypoint correction primary planning session ID | `ses_046a6bb36ffe6zQIErApbaCXid; metadata confirms Prometheus - Plan Builder; this is separate from the current Sisyphus session` |
| Verifier entrypoint correction draft | `complete option-B source at .omo/plans/phase-0-5-v2-amendment-verifier-entrypoints-correction.md; 46902 bytes; SHA-256 AA62A63C523F8E28EC52E8151F4786792AE134C207BAAF7B1819F6E5D967C925; structural/live-binding PASS` |
| Verifier entrypoint correction plan | `docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget-v2-amendment-verifier-entrypoints-correction.md` |
| Verifier entrypoint correction plan SHA-256 | `AA62A63C523F8E28EC52E8151F4786792AE134C207BAAF7B1819F6E5D967C925` |
| Verifier entrypoint correction repository import | `byte-identical to the primary Prometheus source: 46902 bytes and SHA-256 AA62A63C523F8E28EC52E8151F4786792AE134C207BAAF7B1819F6E5D967C925; canonical repository-facing authority after owner approval` |
| Verifier entrypoint correction Sisyphus review | `PASS on exact canonical SHA; trusted-local eight-step drift contract, five negative probes, canonical path bindings, 8 todos/4 finals and separate owner gates are consistent` |
| Verifier entrypoint correction Momus review | `OKAY on exact 46902-byte SHA-bound source: ses_0419b9238ffeRyVqOBNDzSXF5Z; canonical repository import is byte-identical. Repository-path wrapper ses_041942fe6ffek0d1kujsTLCjQC rejected only because that wrapper accepts .omo/plans/*.md, not due to a content finding.` |
| Verifier entrypoint correction Oracle review | `not used as approval receipt: ses_0419b1ddfffet7haQTRXmgwJQy produced no terminal deliverable; mandatory architecture-plan chain requires Momus, which passed` |
| Verifier entrypoint correction owner approval | `not approved; owner deferred the reviewed correction on 2026-08-01` |
| Verifier entrypoint correction execution authorization | `not authorized; correction, amendment F1-F5 and F5 aggregation are deferred` |
| Architecture Explorer MVP direction | `owner-selected planning-only priority: one local HTML; Overview, ProjectSession and Migration/Diff; evidence drill-down; eight-question manual acceptance; minimal 10-20 structural checks only after visible usefulness is established` |
| Architecture Explorer MVP plan | `imported from the separate primary Prometheus session; 19880 bytes; SHA-256 9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1; four vertical stages, architecture-widget.html on Stage 1, no F-wave or correction infrastructure` |
| Architecture Explorer MVP Metis analysis | `completed; recommends an in-place narrowing/supersession of unstarted original Phase 0.5 Tasks 5-10 rather than a parallel architecture authority; accepted model v2/runtime remain the only truth; four visible vertical stages and owner visual stop after each stage` |
| Architecture Explorer MVP Metis session ID | `ses_04153ac3bfferXuFJEvXVWDyWf; metadata validated as Metis - Plan Consultant` |
| Architecture Explorer MVP primary planning session ID | `ses_04145a922ffe1bfEwxVLALfgqG; metadata validated as Prometheus - Plan Builder; separate primary Plan Mode session` |
| Architecture Explorer MVP planning review | `OKAY on a one-time byte-identical .omo review mirror authorized by the owner on 2026-08-02; continued session ses_0412cb8cfffeQ0Qr4hAyIL5tyO (identity validated as Momus - Plan Critic) found the four-stage plan executable and internally consistent; mirror and canonical plan share SHA-256 9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1` |
| Architecture Explorer MVP owner plan approval | `approved by owner 2026-08-02 for plan SHA-256 9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1` |
| Architecture Explorer MVP execution authorization | `approved by owner 2026-08-02 via explicit /architecture-start phase-0.5-architecture-explorer-mvp invocation after live authorization-gate revalidation` |
| Architecture Explorer MVP Task 1 | `CONFIRMED and owner visual PASS recorded from exact owner message согласовано on 2026-08-02; four exact implementation artifacts; deterministic 383453-byte standalone HTML SHA-256 A1FA7F599A0DEC0607DDC10E97DABEE49C69100E4EE1693577B202150EB47824; releases only Task 2` |
| Architecture Explorer MVP Task 2 | `CONFIRMED technically complete by verifier session ses_03c773098ffeQbTL4jjvY05kCB with 101932 assertions; after owner-requested localization/legend and results-panel layout corrections generated architecture-widget.html is 13981737 bytes SHA-256 B3D5DBF5B7FCCC9F6ED67E46CD7068B23D8BF40769E3035089FA9D15009D1C74; 375/768/1280 browser overflow check passed; explicit owner visual PASS received 2026-08-03` |
| Architecture Explorer MVP Task 3 | `technically complete and explicit owner visual PASS received via exact message фиксируем on 2026-08-03; standalone HTML 14073817 bytes SHA-256 F0E9F32EB0872BC504DF935D9BF96C037CE9D9523CAD4E8558A521853EE5FF65; exact ProjectSession partitions, 8/22 flows, 11/3/15/6 reference catalogs and exact-width 375/768/1280 browser QA passed; fresh independent visual QA PASS/HIGH in ses_039e0c59cffeu3Ccq64vsVQPiZ` |
| Architecture Explorer MVP Task 4 | `technically and visually complete; canonical HTML 14279246 bytes SHA-256 11E7BDC33E5BC03AA09553844234263FC311EADD29D3CD9ECB0F1D711C6B6ACF; 14/14 checks, four isolated negative probes, exact-width 375/768/1280 browser QA and fresh image-capable visual inspection passed; source/functional Oracle PASS/HIGH in ses_038d77042ffePHmGYUUrqGbLIV; all eight owner answers are YES and the phase result is explicitly accepted` |
| Architecture Explorer MVP owner answers | `8 of 8 YES, recorded 2026-08-03 in docs/architecture-migration/evidence/phase-0.5-architecture-explorer-mvp-acceptance.md` |
| Phase result acceptance | `pending for Phase 2; Phase 1 was accepted by owner 2026-08-05 via exact statement Результат Phase 1 принимаю` |
| Last completed action | `2026-08-13 Phase 2 Final Verification Wave completed: F1 plan compliance APPROVE, F2 code quality/single-owner APPROVE, F3 real lifecycle QA APPROVE, F4 architecture dossier fidelity APPROVE. Task 12 widget correction remained in place: architecture-widget.html regenerated to 15113378 bytes / SHA-256 A8B12B29D931AB4555F2F20F6FA0036702CB08E48BBBC587A4188FB03E840549. Final gates passed: Debug build 0 warnings/errors, F2 guards 6/6, F3 targeted Release 329 passed / 1 skipped / 330 total, F3 full Release 1613 passed / 1 skipped / 1614 total, model-v2 33 assertions / 21 mutations, runtime-v2 47 assertions / 20 mutations, generate-widget --check 14 checks. Evidence: f1-plan-compliance.md, f2-code-quality-architecture.md, f3-real-lifecycle-qa.md, f4-scope-fidelity-dirty-worktree.md.` |
| Next action | `Owner acceptance checkpoint for Phase 2 result. Do not start Phase 3 planning or implementation until the owner explicitly accepts Phase 2 and separately authorizes the next phase workflow.` |

Допустимые стадии:

```text
awaiting-plan-start
-> metis-analysis
-> prometheus-draft
-> awaiting-primary-plan
-> sisyphus-review
-> momus-review
-> prometheus-corrections (только при замечаниях)
-> momus-final-review
-> awaiting-owner-approval
-> approved
-> executing
-> verification
-> awaiting-owner-acceptance
-> completed
```

Из любой активной стадии возможен переход в `blocked` с записью причины и
следующего безопасного действия. `resume` не имеет права автоматически
переходить через `awaiting-owner-approval` или `awaiting-owner-acceptance`.

Approval gates:

- `/architecture-plan` останавливается на `awaiting-owner-approval`.
- `/architecture-approve` разрешён только после успешного Momus review и
  переводит workflow в `approved`, не исполняя план.
- `/architecture-start` разрешён только из `approved`.
- Принятие результата Phase 0 остаётся отдельным решением владельца.

## Принятые решения

- Для drafting `phase-2-climate-state` принят рекомендуемый planning default по
  `DEC-001`: `ProjectSession.ClimateState` является единственным writable
  canonical owner Climate values; `CalculationContext` в Phase 2 не заменяется
  и остаётся узким downstream compatibility seam/read projection. Завершённое
  logical Climate change публикуется в него ровно через один явный adapter/
  coordinator boundary. `IClimateData`/`ClimateData` допустимы только как
  временная read-only либо forwarding-only compatibility projection, а не второй
  writable owner. Этот default можно пересмотреть в отдельной primary planning
  session, но его отклонение, требующее широкой миграции context, является
  blocking architecture decision и требует возврата владельцу до исполнения.
- Phase 2 ограничена Climate ownership vertical slice. Она не мигрирует
  Construction, Thermal или Hydraulics ownership, не выполняет общий redesign
  `CalculationContext`/`ProjectLoadOrchestrator`, не меняет `.smc` schema/version,
  formulas, UI, packages, transactional restore или Results ownership и не
  реализует Undo/Redo stack/history/UI commands. City selection должна
  представлять одно grouped user action; individual edits - по одному user
  action; load/reset/restore/system apply используют отличимые non-user origins.
- Future Undo/Redo compatibility является сквозным обязательным ограничением для
  всех последующих фаз миграции `ProjectSession`, а не отдельной задачей Phase 2.
  Каждая фаза `ClimateState`, `ConstructionState`, `ThermalState` и
  `HydraulicsState` обязана определить явные state/application mutation
  boundaries и boundary завершённого логического изменения. Все изменения
  canonical state проходят через явные операции state slice; ViewModel остаётся
  адаптером и не является mutation boundary. User mutations должны быть отличимы
  от load, reset, restore и других system mutations; несколько низкоуровневых
  изменений могут составлять одно логическое пользовательское действие; после
  его завершения существует явная event/result boundary для downstream
  invalidation и будущей history recording. Snapshot/save и restore применяют
  состояние через явно обозначенный non-user path и не создают пользовательскую
  undo entry. `Results` потребляет завершённые изменения как derived projection,
  не становится mutation owner; legacy cleanup удаляет обходные ViewModel
  mutation paths. Каждая будущая state-slice phase обязана включать mutation
  API/event contract и acceptance tests. Это решение не реализует undo stack,
  redo stack, history persistence, command history или UI-команды Undo/Redo и не
  предписывает конкретный framework либо преждевременную форму API.
- Reviewed Phase 1 plan SHA-256
  `011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948`
  фиксирует `ProjectSession` как единственного writable owner для project
  identity, current path, dirty state и restore guard. Legacy interfaces
  остаются forwarding-only compatibility surfaces; `CalculationContext` и
  четыре module owners не мигрируют. Current partial restore semantics и
  accepted `.smc` corpus сохраняются без transactional rollback или schema
  policy change. План прошёл Sisyphus PASS и Momus `[OKAY]` и явно утверждён
  владельцем 2026-08-04; это approval плана, но не execution authorization.
- Phase 1 planning-only workflow открыт после принятия Phase 0.5. Metis receipt
  `ses_037db6f56ffe7zEyJPEiRLvk4t` валиден; workflow остановлен на
  `Stage = awaiting-primary-plan`. Это не owner plan approval и не execution
  authorization. Phase 1 production code, tests, maps и widget остаются
  read-only до отдельного reviewed plan, owner approval и `/architecture-start`.
- Phase 1 ограничена минимальным `ProjectSession` shell и узкими lifecycle/state
  contracts. Climate, Construction, Thermal и Hydraulics ownership, formulas,
  UI, `.smc` wire format, packages, installer, releases, deferred correction,
  amendment F1-F5 и original parent Task 5 не входят в scope.
- `ProjectSession` реализован как единственный writable owner идентификации
  проекта, пути, dirty-флага и restore-guard. Legacy-интерфейсы
  `IProjectInfoService`, `IProjectStateService` и `IMarkDirtyService` теперь
  разрешаются в DI в один и тот же экземпляр `ProjectSession`.
- Phase 1 implementation executed 2026-08-04: `ProjectStateService` and
  `CalculationStateService` were converted to forwarding-only compatibility
  surfaces with no mutable lifecycle backing fields; `ProjectSessionTests`,
  `ProjectSessionLegacyStoreGuardTests`, and
  `ProjectLifecycleFlowCharacterizationTests` are green; full Release suite is
  `1565/1566` (1 pre-existing skip); architecture model and widget were updated
  and verified. `CalculationContext` and module ownership slices were not
  migrated.
- Владелец 2026-08-03 exact statements `YES по всем 8 вопросам. Результат Phase
  0.5 Architecture Explorer MVP принимаю.` и `Phase 0.5 принята.` ответил `YES`
  на все восемь owner questions и явно принял результат
  `phase-0.5-architecture-explorer-mvp`. Workflow переведён в
  `Stage = completed`. Acceptance относится только к Architecture Explorer MVP:
  Phase 1 не начата и не наследует plan approval или execution authorization
  Phase 0.5.
- Architecture Explorer MVP Task 4 прошла technical/browser/visual gate на
  canonical SHA-256
  `11E7BDC33E5BC03AA09553844234263FC311EADD29D3CD9ECB0F1D711C6B6ACF`.
  Workflow остановлен на `Stage = awaiting-owner-acceptance`. Ни один из восьми
  owner questions не заполнен за владельца; `Phase result acceptance` остаётся
  `pending`, а Phase 1, correction, F1-F5 и original Task 5 не запускаются.
- Владелец 2026-08-03 exact message `фиксируем` дал explicit visual `PASS` для
  Architecture Explorer MVP Task 3 после fresh independent visual QA `PASS`.
  Это закрывает только Task 3 и не является authorization на Task 4 или Phase 1.
  Workflow поставлен на безопасную паузу `Stage = blocked` перед Task 4;
  `Phase result acceptance` остаётся `pending`.
- Владелец 2026-08-03 явно снял прежнюю паузу перед Architecture Explorer MVP
  Task 3 сообщением `csharp-ls, потом приступаем к Task 3`. Разрешён только Task
  3 утверждённого плана; accepted model/schema/runtime/validator остаются
  read-only, production C#/XAML/tests не входят в write-set. После технической
  проверки обязателен новый owner visual stop; Task 4 автоматически не
  разблокируется.
- Владелец 2026-08-03 дал explicit Architecture Explorer MVP Task 2 visual
  `PASS` сообщением `отлично task 2 утверждаю`. Это закрывает только Task 2.
  Тем же сообщением `пока 3 не начинаем` владелец явно поставил execution на
  паузу перед Task 3. Task 3 не авторизована к запуску до нового explicit owner
  resume; Phase result acceptance остаётся pending, Phase 1 и deferred
  correction/F1-F5 не разблокированы.
- Владелец 2026-08-03 запросил узкую Task 2 UI-коррекцию без тяжёлой волны
  review: убрать горизонтальную прокрутку у карточки `Выбранная проекция` и
  сделать карточку шире. Реализация ограничена CSS source, детерминированной
  регенерацией canonical HTML и коротким browser-check. Карточка занимает всю
  сетку, семь колонок сохранены, длинные значения переносятся внутри ячеек.
  Последующий owner visual `PASS` принят отдельно и всё равно не является
  authorization на Task 3 из-за явной owner pause.
- Владелец явно дал Architecture Explorer MVP Task 1 visual `PASS` сообщением
  `согласовано` 2026-08-02. Это release только для Architecture Explorer MVP
  Task 2 в рамках active plan SHA-256
  `9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1`; `Stage`
  остаётся `executing`, `Phase result acceptance` остаётся `pending`, Task 3
  требует отдельный owner `PASS`, а Phase 1, correction и F1-F5 остаются
  blocked/deferred.
- Architecture Explorer MVP Task 1 считается технически завершённой только в
  границах shell/loading stage: generated HTML подтверждён independent verifier,
  а explicit owner visual `PASS` получен 2026-08-02 сообщением `согласовано`.
  Этот checkpoint не является принятием результата всей фазы, не разблокирует
  Phase 1 и разрешает только Task 2.
- Architecture Explorer MVP Task 2 завершена в границах generated-artifact
  stage, independently confirmed и визуально принята владельцем 2026-08-03.
  Это не acceptance всей фазы и не authorization на Task 3. Task 3 остаётся на
  явной owner pause; Phase 1, correction и `v2-amendment-f1` through
  `v2-amendment-f5` остаются blocked/deferred.
- Для текущего accepted v2 provenance authoritative schema/model hashes:
  `8A0BC79C00FBD8F1D2C2E52E70085DF0472E3675B99B9C5EC9209FA8EEB4C97B` и
  `F573E175C28AA9BEB9DD1809EB49E34B41F182BA2742132E93C40639804EFA97B`.
  Значения `7AFB...` и `EE9E...` трактуются только как historical v1 source
  hashes per `phase-0.5-model-validation-v2.md`, а не как current accepted v2
  hashes.
- Владелец явно возобновил Architecture Explorer MVP сообщением `продолжай`
  2026-08-02. Safe checkpoint reconciled: все четыре Task 1 implementation
  artifacts отсутствуют, поэтому незавершённый worker claim не принят и
  артефактного расхождения нет. Workflow возвращён в `executing` только для
  Task 1; Task 2 остаётся заблокированной до explicit owner visual `PASS`.
- Владелец отдельно авторизовал исполнение Architecture Explorer MVP 2026-08-02
  явным `/architecture-start phase-0.5-architecture-explorer-mvp`. Gate повторно
  подтвердил exact active plan SHA-256, `Stage = approved`, owner plan approval,
  terminal Momus `OKAY`, реальные Metis/Prometheus/Momus identities, Git root и
  защищённую dirty-worktree boundary. Разрешена только Task 1; Task 2 остаётся
  заблокированной до explicit owner `PASS` после visual review.
- Владелец утвердил Architecture Explorer MVP plan SHA-256
  `9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1`
  2026-08-02. Это только plan approval: execution authorization остаётся
  `pending`, а реализация может начаться только после отдельного явного
  `/architecture-start phase-0.5-architecture-explorer-mvp`.
- `ProjectSession` нужен как aggregate root, но не как плоский god object.
- Шесть карт обязательны как разные представления одной архитектурной модели.
- Characterization tests и state inventory предшествуют переносу состояния.
- Старый аудит не является источником актуальных метрик для `ace v.2`.
- Владелец принял результат Phase 0; это завершает только
  `phase-0-baseline` и не авторизует планирование или запуск Phase 1.
- Перед Phase 1 должен быть отдельно спланирован, реализован, проверен и принят
  владельцем model-driven architecture widget; Phase 1 до этого заблокирована.
- План Phase 0.5 считается готовым только к owner plan approval. Успешные
  Sisyphus/Momus reviews не являются execution authorization; запуск возможен
  только отдельным owner action после утверждения плана.
- Владелец утвердил проверенный план Phase 0.5 с SHA-256
  `2C056AAFCE062E3E749EC9961E0B55237C4667D8CFFEF5F438CE7F108C2E452E`.
  Само утверждение перевело workflow только в `approved`, затем отдельный
  owner action `/architecture-start phase-0.5-model-driven-architecture-widget`
  разрешил исполнение плана и перевёл workflow в `executing` перед Task 1.
- Task 2 historical widget source and archive are preserved only as provenance
  artifacts. They are not runtime inputs and not generator inputs for the future
  model-driven widget; the generated widget path remains absent until a later
  plan task creates it.
- Task 3 establishes the canonical widget contract data for Phase 0.5: one
  accepted widget schema/model pair plus pure validator entrypoints. This is not
  full generic Draft 2020-12 compliance; generic validation remains `degraded`
  until an approved installed validator exists.
- Rejected Task 4 does not change the accepted boundary. Its implementation may
  exist in five allow-listed paths with self-reported green rows, but it is not
  independently accepted, so Task 3 remains the last completed accepted action.
- Владелец явно выбрал `Перейти на v2 (Recommended)` как direction для Phase 0.5
  Task 4 contract blocker. Канонические принципы будущей поправки: `contract_version=2.0.0`;
  один unique stable ID; один runtime document; один `snapshot_states` source of
  snapshot presence/values; отсутствие параллельного `snapshots` membership
  source of truth; model-level canonical diff policy; честные
  `added`/`removed`/`changed`/`unchanged`/`unresolved`; отсутствие v1
  compatibility shim, потому что contract ещё не externally released.
- Этот owner choice разрешает только planning amendment. Он не авторизует edits
  в schema/model/runtime, не считает Task 4 complete, не запускает Task 5 или
  Phase 1 и не пересекает существующие owner gates.
- Tasks 3 и 4 могут быть formally reopened только после того, как amendment
  будет drafted, reviewed, явно approved владельцем и отдельно
  execution-authorized через существующий workflow.
- Владелец утвердил v2 amendment plan SHA-256
  `B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536`.
  Это только plan approval; отдельный `/architecture-start` остаётся обязателен.
- При подтверждённой невыполнимости утверждённого плана execution должен
  немедленно остановиться с `Stage = blocked`; необходимо зафиксировать blocker
  и предложить владельцу варианты. Нельзя самостоятельно менять цель, scope или
  критерии приёмки, чтобы продолжить выполнение.
- Владелец выбрал workflow repair option A: на время amendment она является
  `Current phase`, а её reviewed plan является `Active plan`. Исходная
  `phase-0.5-model-driven-architecture-widget` и plan SHA-256
  `2C056AAFCE062E3E749EC9961E0B55237C4667D8CFFEF5F438CE7F108C2E452E`
  сохранены как paused parent authority; successful amendment F5 возвращает
  workflow только к original Task 5. Эта context correction не является
  execution authorization и требует нового явного `/architecture-start`.
- Владелец выбрал вариант `Добавить entrypoints (Recommended)` для blocker-а
  amendment F1-F5. Решение разрешает planning-only correction с canonical
  suites в существующем `widget/verify-widget.mjs`; standalone verifiers и
  waiver F1-F5 не выбраны. Correction обязана иметь отдельный reviewed plan,
  owner approval и execution authorization, не менять accepted Tasks 1-5 или
  F1-F5 criteria и не разблокировать original Task 5 до `F5 APPROVE`.
- Владелец впоследствии отложил это correction-направление до возможной будущей
  потребности в CI-grade verification. Ближайшая цель после явного снятия паузы
  - отдельный минимальный Architecture Explorer MVP поверх уже принятого model
  v2/runtime фундамента. Сначала требуется видимый и лично полезный HTML;
  усиленная verifier/receipt infrastructure не является MVP prerequisite.

## Открытые вопросы

- Primary Prometheus plan должен определить точную форму Climate state/
  application mutation API, immutable/read projection и completion event/result,
  не превращая выбранную форму в framework-specific Undo/Redo implementation.
- До Phase 2 ownership edits требуется live characterization exact counts для
  dirty, state completion, `CalculationContext` update, downstream Circuits
  recalculation и Results refresh на city selection, individual edits, reset,
  load, second load и repeated load/reset. Эти числа не выводятся из текущих
  green tests и должны быть измерены до закрепления acceptance expectations.
- Нужно live-верифицировать полный Climate writer inventory и выбрать минимальный
  allow-list для protected dirty paths. Любой путь из существующего dirty
  worktree сначала требует binary-safe baseline и сохранения unrelated deltas.
- Какая конкретная форма event/result будет обозначать завершение логического
  изменения в state/application API каждого slice, не связывая контракт с
  будущим Undo/Redo framework?
- Как последующие slice plans будут задавать grouping нескольких внутренних
  field/collection changes в одно пользовательское действие, включая границы
  ошибок и отменённого изменения?
- Как конкретно будет представлена mutation origin (`user`, load, reset,
  restore, другой system apply) и где будет находиться будущий history recorder?
  Это детали реализации; обязательность явных mutation/completion boundaries и
  distinguishable non-user paths уже принята и не является открытым вопросом.
- Решено: единственным writable owner project lifecycle identity, current path,
  dirty state и restore guard в Phase 1 является `ProjectSession`.
  `ProjectStateService` и `CalculationStateService` теперь forwarding-only
  compatibility surfaces.
- Должен ли будущий OpenCode skill храниться внутри репозитория или в
  пользовательской конфигурации OpenCode?
- Каков обязательный срок read-совместимости старых `.smc`?
- Нужно ли гарантировать transactional in-memory restore при ошибке одного из
  модулей или на следующем этапе сохранить текущее поведение?
- Как именно существующий `CalculationContext` должен соотноситься с будущим
  `ProjectSession`: остаться отдельным seam, стать facade или быть заменённым?
  Phase 1 оставила `CalculationContext` без изменений.
- Какой модуль-срез (Climate, Construction, Thermal, Hydraulics) мигрировать
  следующим после Phase 1, и какой будет контракт доступа через `ProjectSession`?
- Task 6 (browser/runtime QA harness) остаётся обязательным для Phase 2+;
  Phase 1 relied on test-harness characterization tests.

## Следующее действие

Текущий authoritative next action: owner acceptance checkpoint for Phase 2
result. Phase 2 implementation Tasks 1-12 and Final Verification Wave F1-F4 are
accepted by the execution workflow. F1 plan compliance APPROVE, F2 code quality /
single-owner APPROVE, F3 real lifecycle QA APPROVE, and F4 architecture dossier
fidelity APPROVE are recorded under
`docs/architecture-migration/evidence/phase-2-climate-state/`. Task 12 widget
correction remains in place: `docs/architecture-migration/architecture-widget.html`
was physically regenerated through `node docs/architecture-migration/widget/generate-widget.mjs`
to `15113378` bytes and SHA-256
`A8B12B29D931AB4555F2F20F6FA0036702CB08E48BBBC587A4188FB03E840549`; subsequent
`generate-widget.mjs --check` matched canonical/generated hashes. Final executable
gates passed: Debug build 0 warnings/errors, F2 guards 6/6, F3 targeted Release
329 passed / 1 skipped / 330 total, F3 full Release 1613 passed / 1 skipped /
1614 total, model-v2 33 assertions / 21 mutations, runtime-v2 47 assertions / 20
mutations, and widget check 14 checks. Phase 2 is ready for owner acceptance, but
Phase 3 planning/implementation remains blocked until the owner explicitly
accepts Phase 2 and separately authorizes the next workflow.
При проектировании любой последующей state-slice phase (`ClimateState`,
`ConstructionState`, `ThermalState`, `HydraulicsState`) план обязан переносить
сквозной Future Undo/Redo compatibility constraint: явные user mutation и
logical-change completion boundaries, distinguishable non-user load/reset/
restore/system-apply paths, grouping нескольких внутренних изменений в одно
user action и acceptance tests, доказывающие, что перехват ViewModel internals не
нужен. Snapshot/save и restore plans обязаны использовать обозначенный non-user
apply path; Results plan обязан оставлять Results derived projection; legacy
cleanup plan обязан удалить обходные ViewModel mutation paths. Это planning
constraint, а не разрешение реализовывать Undo/Redo или начинать следующую фазу.

Нижеследующие записи в этом разделе сохранены как исторические next-action
checkpoints. Они superseded authoritative инструкцией выше и не разрешают
исполнение ранее указанных задач.

Открыть canonical local artifact
`D:\IA\ace v.2\docs\architecture-migration\architecture-widget.html` напрямую
через `file://` и получить explicit owner visual feedback по Task 2 generated
artifact: открывается ли HTML без сервера, понятны ли Overview,
ProjectSession, Migration/Diff и evidence drill-down, достаточно ли ясен MVP.
До owner `PASS` не начинать Task 3 и не расширять Task 2 write-set.

Owner pause снята явным `продолжай` 2026-08-02. Reconciliation подтвердила, что
`architecture-widget.template.html`, `architecture-widget.css`,
`generate-widget.mjs` и generated `architecture-widget.html` отсутствуют;
следовательно, продолжить реализацию и independent verification только Task 1,
затем остановиться для owner visual review. Task 2 не начинать.

Workflow поставлен владельцем на безопасную паузу 2026-08-02 во время Task 1,
до независимой проверки и visual review. До отдельного explicit owner resume не
выполнять edits, generation, verification или browser/open actions. После resume
сначала проверить фактический Task 1 write-set и только затем завершить Task 1;
Task 2 остаётся заблокированной до отдельного owner visual `PASS`.

Текущий authoritative next action: выполнить только Task 1 утверждённого
`phase-0.5-architecture-explorer-mvp` plan, создать автономный shell через
accepted model-loading boundary, провести Task 1 smoke checks и остановиться
для explicit owner visual feedback. Task 2 до owner `PASS` не начинать.

Workflow поставлен владельцем на паузу. Unapproved verifier-entrypoint
correction и amendment F1-F5 переведены в `deferred`; original Task 5 и Phase 1
не запускаются. Принятые canonical model v2, runtime и amendment Tasks 1-5
сохраняются без переписывания. Новый приоритет после отдельного явного resume -
спланировать короткую `phase-0.5-architecture-explorer-mvp`, которая сначала
даст открываемый локальный HTML и пройдёт ручную оценку полезности. До resume не
создавать MVP plan, не менять `architecture_widget.html`, verifier, model,
runtime, production-код или evidence.

Planning-only pause снята 2026-08-01 для
`phase-0.5-architecture-explorer-mvp`. Metis analysis подтверждает, что MVP
следует оформить как узкое in-place supersession незапущенных original Phase
0.5 Tasks 5-10, а не как параллельный источник архитектурной истины. Stage
остановлена на `awaiting-primary-plan`: обязательный следующий шаг - отдельная
primary Plan Mode session с
`/architecture-draft phase-0.5-architecture-explorer-mvp`. Implementation,
browser, correction, F1-F5, original Task 5 и Phase 1 не авторизованы.

Separate primary Prometheus session `ses_04145a922ffe1bfEwxVLALfgqG` produced a
corrected short plan, imported at
`plans/phase-0.5-architecture-explorer-mvp.md` (19880 bytes, SHA-256
`9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1`).
The initial Momus invocation in session `ses_0412cb8cfffeQ0Qr4hAyIL5tyO`
rejected only because its wrapper accepts `.omo/plans/*.md` and refused the
canonical repository path; it made no content finding. On 2026-08-02 the owner
subsequently authorized one byte-identical `.omo/plans` review mirror and one
content-review attempt. The mirror and canonical plan both have SHA-256
`9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1`; the
continued Momus session returned `OKAY`, finding the four-stage plan executable
and internally consistent. This review is not plan approval or execution
authorization. Workflow remains `awaiting-owner-approval`; implementation
remains unauthorized.

The following amendment Task 3 direction was the historical next action before
the accepted Task 3, Task 4, and Task 5 scope-gate results:

Исполнять только amendment Task 3 в стадии `executing`. Active phase —
`phase-0.5-model-driven-architecture-widget-v2-amendment`, parent Phase 0.5
сохранена paused. Task 2 mapping независимо подтверждён в session
`ses_0435ffd52ffeRkE7OnxSpEnhdQ`: `98` passed, `0` failed, `0` blockers. Он
связывает exact v1 hashes и полностью reconciles 16 top-level keys, 245 records,
112 edge semantics, 280 global IDs и 355 reference occurrences с нулём
unexplained и без fabricated Target. Следующее действие — только Task 3 v2
schema/model/semantic validator. До его независимой приёмки не начинать runtime
Task 4. Последним
independently accepted completed action
остаётся Task 3, verifier session `ses_0479222f7ffeeS4phaES1DoAuT`,
`VERDICT: CONFIRMED`, high confidence: canonical CLI exit `0`, counts
`79/112/27/22/5/112/11/15/6/3`, 280 global IDs, exact six views, `15/15` INV,
`6/6` DEC, schema/model SHA-256
`7afbce3cfadc8b77443462e52d0bb15c7bdd31569de41fadcaae941791b07194` and
`ee9e0069d71f3abffa5fa1e9b698a97507694597fbe85b84c272c30d3f55efbd`, and
`53/53` real-CLI mutations rejected. Task 4 executor session
`ses_04744edb2ffelVnSBNoZwmyW4b` and verifier session
`ses_047288711ffedvSwSIvF0lpUA3` ended with `VERDICT: REJECTED`. Owner has now
explicitly selected `Перейти на v2 (Recommended)`: canonical amendment target is
v2 `snapshot_states` with `contract_version=2.0.0`, one unique stable ID, one
runtime document, no parallel `snapshots` membership source of truth,
model-level canonical diff policy and honest
`added`/`removed`/`changed`/`unchanged`/`unresolved`, with no v1 compatibility
shim because this contract is not externally released. Metis v2 pre-analysis
completed in session `ses_046bb4fecffeqVxPgX7SXpsiF9`. Primary Prometheus
session `ses_046a6bb36ffe6zQIErApbaCXid` produced the reviewed amendment imported
at `docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget-v2-amendment.md`,
30476 bytes, SHA-256
`B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536`.
Sisyphus review is PASS; hash-bound Momus session
`ses_04677ca75ffeOkpw7mGdz9x2cv` and Oracle session
`ses_04677334bffekGA8FIsTxVdXuf` are both OKAY. Owner approved this exact plan
on 2026-08-01. Failed receipt
`docs/architecture-migration/evidence/phase-0.5-v2-amendment-repository-snapshot.md`
остаётся immutable; independently accepted corrected basis —
`phase-0.5-v2-amendment-repository-snapshot-retry-1.md`. Original Tasks
5-10/F1-F5 and Phase 1 remain blocked until amendment F5.

## Журнал решений

- 2026-08-12: Phase 2 Task 11 affected gates accepted after a test-only blocker
  correction. Initial targeted Release matrix failed only
  `SaveCurrentProject_ProjectsLiveModuleStateInsteadOfResultsCache`: expected
  saved Climate values from the live module state, but actual values were the
  canonical session defaults (`SelectedCity = string.Empty`, `WindSpeed = 5.0`).
  Root cause investigation proved fixture/runtime instance divergence:
  `ResultsViewModelTestHelpers.CreateResultsViewModel` created `ClimateViewModel`
  via the legacy no-session helper, which owns a standalone
  `ProjectSessionClimateState`, while `ResultsViewModel.SaveCurrentProject()`
  correctly reads `projectStateService.Session.ClimateState.Snapshot`. Minimal
  correction was limited to
  `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs`:
  a session-backed `CreateClimateViewModel(IProjectSession)` overload is used by
  the shared Results helper, the same session is forwarded to
  `ProjectLoadOrchestrator`, and the legacy no-session overload remains for older
  tests. Production save path and `.smc` wire format were unchanged. Gates after
  fix: Debug build PASS 0 warnings/0 errors; Release build PASS 0 warnings/0
  errors; isolated blocker test PASS; targeted Release matrix PASS with TRX
  counters total 330 / executed 329 / passed 329 / failed 0; first full Release
  run had an order-sensitive `ThermalViewModelTests.Validate_ValidInput_ReturnsTrue`
  failure, that test passed in isolation, and full Release rerun PASSed with TRX
  counters total 1616 / executed 1613 / passed 1613 / failed 0. LSP diagnostics
  reproduced the known cwd harness issue. Evidence is
  `docs/architecture-migration/evidence/phase-2-climate-state/affected-gates.md`.
  Active plan checkbox Task 11 marked completed. Next action is Phase 2 Task 12
  dossier refresh; Task 12 and Final Verification Wave were not started.

- 2026-08-06: Phase 2 Task 3 multiplicity characterization independently
  verified and accepted. Added
  `tests/SnowMeltingCalculator.Tests/Climate/ClimateMultiplicityCharacterizationTests.cs`
  and evidence
  `docs/architecture-migration/evidence/phase-2-climate-state/multiplicity-characterization.md`.
  Current legacy counts are encoded for MarkDirty/ClimateData.DataChanged/
  ClimateViewModel.DataChanged/CalculationContext.ContextChanged respectively:
  selected-city `3/3/3/3`, scalar edit `1/1/1/1`, high-requirements toggle
  `2/2/2/2`, reset `0/1/1/1`, reset-to-city-data `3/4/4/4`, same-value scalar
  no-op `0/0/0/0`, same-city no-op `0/0/0/0`, load and second load `0/0/0/0`,
  and repeated resets each `0/1/1/1`. Downstream Thermal/Circuits/Results counts
  are documented as covered by existing relevant suites rather than invented in
  the narrow VM probe. Gates: `ClimateMultiplicity` 9/9 PASS;
  `ClimateMultiplicity|ClimateStateLegacyStoreGuard|ClimateViewModelTests` 33/33
  PASS; wider Climate/CalculationContext/Thermal/Hydraulics relevant filter
  203/203 PASS; diff-check PASS with only the known TASK_CONTEXT LF/CRLF warning.
  LSP diagnostics remains blocked by the known workspace-root harness issue.
  Active Phase 2 plan checkbox Task 3 marked completed. Workflow stays
  `executing`; next action is Phase 2 Task 4 ClimateState API. Task 4 was not
  started; production code, Phase 1 docs, maps, model and widget were not changed.
- 2026-08-06: Phase 2 Task 2 writer guard independently verified and accepted.
  Added `tests/SnowMeltingCalculator.Tests/Climate/ClimateStateLegacyStoreGuardTests.cs`
  as a deterministic source-text inventory for the current legacy Climate writer
  surface. The accepted current bypass/projection list records writable
  `ClimateViewModel` backing fields/mutation boundaries, `SyncToClimateData()`
  concrete `ClimateData` writes, public settable `ClimateData` properties,
  `CalculationContext.UpdateClimate()`, `ProjectLoadOrchestrator` restore/reset
  Climate VM writes, and `ResultsViewModel.SaveCurrentProject()` as read/
  projection-only with zero direct Climate VM setters. A negative fixture proves
  direct setters in forbidden callers are detected. Evidence is
  `docs/architecture-migration/evidence/phase-2-climate-state/writer-guard.md`.
  Gates: `ClimateStateLegacyStoreGuard` 2/2 PASS;
  `ClimateStateLegacyStoreGuard|ClimateViewModelTests` 24/24 PASS; diff-check
  PASS with only the known TASK_CONTEXT LF/CRLF warning. LSP diagnostics remains
  blocked by the known workspace-root harness issue. Active Phase 2 plan checkbox
  Task 2 marked completed. Workflow stays `executing`; next action is Phase 2
  Task 3 multiplicity tests. Task 3 was not started; production code, Phase 1
  docs, maps, model and widget were not changed.
- 2026-08-05: Phase 2 Task 1 baseline/recovery independently verified and
  accepted. Verifier recalculated the four retained immutable receipts and matched
  repaired `baseline.md`: baseline/post status SHA
  `42BD4EE368BAAE77D37E2B306780945F436A2D2FD5FCBD09202EC3E3113597C3`, baseline
  diff SHA `574252FE538E15F64722BAC6909294675F0001809133FE3E1C956B9B94475FD0`,
  cached diff empty SHA
  `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855`, 225 status
  records + branch header, 215 modified, 0 deleted, 10 untracked, 0 staged, and
  baseline-vs-post drift 0/0/0 after excluding only the Task 1 evidence
  directory. Placeholder/mojibake scan passed, and `git diff --check` for
  `baseline.md` plus `TASK_CONTEXT.md` produced no errors. Active Phase 2 plan
  checkbox Task 1 marked completed. Workflow returned from `blocked` to
  `executing`; next action is Phase 2 Task 2 writer guard. Task 2 was not started,
  and production code/tests/Phase 1 docs were not changed.
- 2026-08-05: по owner instruction `Phase 1 не трогаем. Phase 2 не начинаем
  заново` выполнена только repair операция Task 1 baseline report. Новый
  repository snapshot не создавался; использованы только четыре retained raw
  receipts: `baseline-git-status.bin`, `baseline-git-diff-name-only.bin`,
  `baseline-git-cached-diff-name-only.bin`, `post-git-status.bin`. Invalid
  placeholder `baseline.md` заменён factual report: baseline/post status SHA
  `42BD4EE368BAAE77D37E2B306780945F436A2D2FD5FCBD09202EC3E3113597C3`, diff
  SHA `574252FE538E15F64722BAC6909294675F0001809133FE3E1C956B9B94475FD0`, cached
  diff empty SHA `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855`,
  225 status records + branch header, 215 modified, 0 deleted, 10 untracked,
  0 staged, and 0 baseline-vs-post removed/added/status-changed records after
  excluding only the Task 1 evidence directory. Placeholder/mojibake verification
  passed. Workflow remains `blocked` pending independent verification; Task 2 was
  not started.
- 2026-08-05: Phase 2 execution остановлена на Task 1 и переведена в `blocked`.
  Первый worker `ses_02ce7a7c2ffef6kq75r4Zv4oLf` не смог захватить NUL stream
  через PowerShell 5.1. Replacement worker `ses_02cd56513ffe2GzG7M0dsSKBiv`
  успешно сохранил четыре raw receipts через `cmd.exe` с exit `0`, но не
  завершил post-processing. Recovery worker `ses_02cc9d2c2ffetA3w8wd4xB878F`
  создал только invalid placeholder `baseline.md`: фактические SHA/count/drift
  значения и перечисленные CSV/JSON отсутствуют. Поэтому Task 1 не принят,
  independent verification не запускалась, plan checkbox остаётся unchecked,
  Tasks 2-12/F1-F4 не начинались. Production code и tests этой execution lane
  не изменялись. Safe recovery ограничен детерминированным parsing четырёх
  immutable `.bin` и заменой placeholder evidence с последующей независимой
  проверкой.
- 2026-08-05: владелец отдельной явной командой `/architecture-start
  phase-2-climate-state` авторизовал исполнение reviewed plan SHA-256
  `D79D7C46CA73EBCFF161164FC41EE3EB041B9B172631944CB611FA42BA998A6B`.
  Authorization gate повторно подтвердил `Current phase`, `Stage = approved`,
  exact live plan hash, owner approval, Sisyphus PASS, terminal Momus `OKAY`,
  реальные Metis/Prometheus/Momus identities, Git root `D:\IA\ace v.2` и
  существенно dirty protected worktree. Workflow переведён в `executing`;
  первым разрешён только Task 1 binary-safe baseline capture. Commits, push,
  installs и external changes не авторизованы.
- 2026-08-05: владелец явно утвердил reviewed Phase 2 plan точной формулировкой
  `Утверждаю план Phase 2 SHA-256
  D79D7C46CA73EBCFF161164FC41EE3EB041B9B172631944CB611FA42BA998A6B`.
  Live hash канонического
  `docs/architecture-migration/plans/phase-2-climate-state.md` совпал; current
  phase, `awaiting-owner-approval`, Sisyphus PASS, Momus OKAY и реальные
  Metis/Prometheus/Momus session identities повторно подтверждены. Workflow
  переведён только в `approved`; `Execution authorization = pending`.
  Production-код, tests, maps, widget и execution evidence не изменялись.
  Следующий допустимый переход требует отдельной явной
  `/architecture-start phase-2-climate-state`.
- 2026-08-05: после явного owner approval только на запись planning artifact
  primary Prometheus session `ses_02e464159ffeLYpJkhNkBJErT1` создала
  `.omo/plans/phase-2-climate-state.md`: 12 implementation todos, F1-F4,
  explicit ClimateState owner/mutation/origin/completion/no-op contract,
  characterization-first TDD, protected dirty baseline, `.smc` v1.0/v1.1
  compatibility, downstream multiplicity, DI/single-owner guards, six-map/
  model/widget gates и отдельные approval/execution boundaries. Sisyphus review
  дал PASS. Обязательный Momus review дал `OKAY` в session
  `ses_02d0d549affeUPch83Lkox97qE`. Source импортирован byte-identically в
  `docs/architecture-migration/plans/phase-2-climate-state.md`; обе копии имеют
  SHA-256 `D79D7C46CA73EBCFF161164FC41EE3EB041B9B172631944CB611FA42BA998A6B`.
  Workflow переведён в `awaiting-owner-approval`; production/tests/maps/widget
  не изменялись, execution не авторизована.
- 2026-08-05: получен и проверен receipt отдельной primary Plan Mode session
  `ses_02e464159ffeLYpJkhNkBJErT1`. Metadata: 54 messages, полный transcript,
  agents `Prometheus - Plan Builder` и техническая compaction; topology lock
  подтверждает узкую Climate-only migration, canonical
  `ProjectSession.ClimateState`, adapter/projection boundary, сохранение `.smc`
  и characterization-first TDD. Один background falsification task завершился
  `Session error: Aborted` и самим Prometheus корректно исключён из evidence.
  Prometheus не записал `.omo/plans/phase-2-climate-state.md`, поскольку
  остановился на явном pre-write owner approval gate. Переданный session ID не
  интерпретирован как approval; workflow остаётся `awaiting-primary-plan`.
- 2026-08-05: по отдельному явному запросу владельца продолжен planning-only
  workflow `phase-2-climate-state`. После fresh проверки `D:\IA\ace v.2`, Git
  root, большого protected dirty worktree и актуальных Climate ownership,
  reactive, persistence, DI и test seams выполнен обязательный Metis
  pre-analysis в session `ses_02e61739dffenW8itUC4j80ikX`; metadata подтверждает
  единственного agent `Metis - Plan Consultant`. Принят drafting default:
  `ProjectSession.ClimateState` - один writable owner, `CalculationContext` -
  узкий downstream compatibility seam/read projection, ViewModel - adapter,
  city selection - одно logical user action, load/reset/restore/system apply -
  отличимые non-user paths. Зафиксированы characterization и exact multiplicity
  gates, `.smc` v1.1 compatibility, protected dirty paths, узкие exclusions и
  stop conditions. Workflow переведён только в `awaiting-primary-plan`; plan,
  production/tests/maps/widget/evidence не создавались и не изменялись.
- 2026-08-05: владелец явно принял результат Phase 1 точной формулировкой
  `Результат Phase 1 принимаю`. Workflow `phase-1-project-session-shell`
  переведён из `awaiting-owner-acceptance` в `completed` после завершения Tasks
  1-10 и Final Verification Wave F1-F4 с итоговыми verdicts `APPROVE`. Это
  принятие закрывает только Phase 1: Phase 2 не начата, не спланирована, не
  утверждена и не авторизована; следующий workflow требует отдельного явного
  запроса владельца. Production-код этим acceptance update не изменялся.

- 2026-08-05: planning-only принято сквозное решение Future Undo/Redo
  compatibility для всех последующих фаз миграции `ProjectSession`.
  - `ClimateState`, `ConstructionState`, `ThermalState` и `HydraulicsState`
    должны предоставлять явные state/application mutation boundaries и одну
    идентифицируемую boundary завершения логического изменения пользователя.
  - User mutation отличается от load, reset, restore и другого system apply;
    несколько внутренних field/collection changes могут образовывать одно user
    action; downstream invalidation и будущая history recording начинаются после
    completion boundary.
  - ViewModel остаётся адаптером и не является обязательной точкой перехвата;
    Results остаётся derived projection; snapshot/save и restore используют
    явно обозначенный non-user path; legacy cleanup удаляет обходные ViewModel
    mutation paths.
  - Каждая будущая state-slice phase должна включать mutation API/event contract
    и acceptance tests с универсальным критерием: `Every user-visible mutation
    of the migrated slice crosses an explicit state/application mutation
    boundary and produces one identifiable logical-change completion boundary.
    Load, reset, restore, and system apply use distinguishable non-user paths. No
    future Undo/Redo implementation needs to intercept ViewModel internals, and
    multiple internal field changes can represent one user action.`
  - Undo/redo stacks, history persistence, command history, snapshots и UI
    commands не проектировались и не реализовывались; production code и
    завершённый Phase 1 plan не изменялись.

- 2026-08-04: Phase 1 Task 8 GREEN implementation completed.
  - No production code changes were required; the implementation from Tasks 4-7
    already preserves lifecycle orchestration, partial-failure semantics, and
    `.smc` compatibility.
  - `ProjectLifecycleFlowCharacterizationTests` pass 6/6, proving successful load,
    second-load identity replacement, post-load edit dirtiness, repeated
    reset/load cycle event hygiene, and injected early/late restore failures that
    leave partial state and clear the guard without rollback.
  - Full Release test suite passes 1565/1566 with one pre-existing skip
    (`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`).
  - Targeted owner/guard/flow lane: 46/46; affected lifecycle lane: 100/101 (1
    skipped); persistence lane: 18/18; Debug/Release production builds: 0 errors.
- 2026-08-04: Phase 1 Task 10 GREEN — shared architecture dossier and widget updated.
  - Updated `docs/architecture-migration/maps/architecture-model.json` to record
    `ProjectSession` lifecycle ownership, 10 Phase 1 evidence entries, migrated
    `ST-001`/`ST-002`/`ST-004`/`ST-005`, new `DRN-PS`/`DRN-IPS` nodes, and 9 Phase 1
    DI/runtime/state/reactive edges.
  - Refreshed all six filtered views (`compile-time.md`, `di-runtime.md`,
    `state-ownership.md`, `reactive.md`, `persistence.md`, `user-flow.md`) with
    Phase 1 overlay sections and updated frontmatter.
  - Regenerated `docs/architecture-migration/architecture-widget.html`
    (15,100,929 bytes, SHA-256 `A9505F6B5957FCCCA40E6FB6F9141E4CA65CF0E98985E1A0DD854A1DEDE27893`);
    `generate-widget.mjs --check` passes 14/14.
  - `verify-widget.mjs` model-v2 (33 assertions, 21 mutations) and runtime-v2
    (47 assertions, 20 mutations) both PASS.
  - Added evidence receipt `dossier-update.md` and updated `TASK_CONTEXT.md`
    workflow state, decisions, open questions, next action, and this journal.
  - No production code, test code, `.smc` schema, packages, UI, or installer
    changes occurred in Task 10.
  - SUPERSEDED 2026-08-05: this Task 10 GREEN claim is invalid. A read-only audit
    found the nested restore lease defect in `ProjectSession.BeginProjectRestore()`;
    Task 10 was stopped and F1-F4 must not run until regression, fix, gates and
    Tasks 4-9 re-evaluation complete.
  
- 2026-08-05: BLOCKED — Phase 1 Task 10/F-wave stopped because
  `ProjectSession.BeginProjectRestore()` has a nested lease correctness defect.
  Nested calls return the same `_currentLease`; disposing inner and outer scopes
  can leave `_restoreDepth == 1` and `IsLoadProjectInProgress == true`. This
  invalidates prior accepted/GREEN status for Tasks 4, 6, 8 and 9 despite green
  suites, and makes Task 10 dossier/widget updates non-authoritative. Active plan
  checkboxes were returned to re-evaluation state for Tasks 4-10. Required next
  action: add the nested restore regression, minimally fix lease creation/disposal
  semantics, rerun targeted/affected/persistence/build gates, then re-evaluate
  Tasks 4-9 and only then reconsider Task 10/F1-F4.

- 2026-08-05: nested restore lease correction completed and independently
  verified by Atlas after the single owner-authorized narrow executor. Added
  regression coverage in `ProjectSessionTests.cs` for distinct nested leases,
  inner-then-outer disposal, outer-then-inner disposal, repeated disposal no-op,
  and final-exit subscriber exception semantics. Minimal production fix in
  `ProjectSession.cs`: removed shared `_currentLease`, returns a new
  `ProjectRestoreLease(this)` for every successful `BeginProjectRestore()`, and
  keeps only canonical `_restoreDepth`. LSP diagnostics still fail because the
  harness rejects the workspace path, matching the known tooling issue. Gates:
  targeted owner/guard 43/43, affected lifecycle 83/84 with one existing skipped
  real-project test, persistence 18/18, Debug build 0 warnings/0 errors, Release
  build 0 warnings/0 errors, full Release suite 1568/1569 with one existing skip.
  Tasks 4, 6, 8 and 9 are re-evaluated as accepted; Task 5 and Task 7 remain
  accepted because forwarding/DI invariants still pass through the same gates.
  Task 10 remains pending re-QA/update because its dossier/widget outputs were
  generated before this correction; F1-F4 remain blocked.

- 2026-08-05: Task 10 re-accepted after nested restore lease correction. Atlas
  re-ran model/widget checks without launching additional subagents: model-v2
  PASS with 33 assertions and 21 mutations, runtime-v2 PASS with 47 assertions
  and 20 mutations, and `generate-widget.mjs --check` PASS with 14/14 checks.
  The generated widget remained byte-stable with SHA-256
  `9C5188BAC257D9CAC51045C0D2186D03A4A2E6B92AFFBF0519B3B3737BBCED9F`; no model,
  maps or widget rewrite was necessary. `dossier-update.md` was updated with the
  post-correction re-check and correction gate counts. Active plan checkbox Task
  10 marked completed again. F1-F4 were not run.

- 2026-08-05: Phase 1 Final Verification Wave completed and approved.
  - F1 plan compliance audit: `VERDICT: APPROVE` in
    `docs/architecture-migration/evidence/phase-1-project-session-shell/f1-plan-compliance.md`.
  - F2 code quality and architecture review: `VERDICT: APPROVE` in
    `docs/architecture-migration/evidence/phase-1-project-session-shell/f2-code-quality-architecture.md`.
  - F3 real lifecycle QA: `VERDICT: APPROVE` in
    `docs/architecture-migration/evidence/phase-1-project-session-shell/f3-real-lifecycle-qa.md`.
  - F4 scope fidelity and dirty-worktree audit: `VERDICT: APPROVE` in
    `docs/architecture-migration/evidence/phase-1-project-session-shell/f4-scope-fidelity-dirty-worktree.md`.
  - Active plan checkboxes Tasks 1-10 and F1-F4 are all marked completed.
  - Workflow stage moved to `awaiting-owner-acceptance`; Phase 1 is ready for
    owner acceptance, not self-accepted. Phase 2 remains blocked until explicit
    owner acceptance and separate authorization.
  - Evidence: `docs/architecture-migration/evidence/phase-1-project-session-shell/lifecycle-user-flows.md`.
  - Active plan checkbox Task 8 marked completed.
  - Next: Task 9 — full gates and scope/single-owner invariant audit.

- 2026-08-04: Phase 1 Task 7 GREEN implementation completed.
  - `ProjectSession` is registered once as a singleton and mapped to
    `IProjectSession`, `IProjectInfoService`, `IProjectStateService`, and
    `IMarkDirtyService` in `AddApplicationServices`.
  - `ResultsViewModel` and `CalculationStateService` constructors consume the
    canonical `IProjectSession` through DI.
  - Added `DependencyInjection_LifecycleConsumersShareCanonicalSession` to
    `ProjectSessionTests`; it proves `ResultsViewModel` and
    `CalculationStateService` receive the same `IProjectSession` instance.
  - `DiRegistrationTests` pass 8/8; no circular dependencies or transient
    adapters introduced.
  - Targeted owner/guard lane: 40/40; affected lifecycle lane: 100/101 (1
    skipped); persistence lane: 18/18; Debug/Release production builds: 0 errors.
  - Evidence: `docs/architecture-migration/evidence/phase-1-project-session-shell/di-runtime.md`.
  - Active plan checkbox Task 7 marked completed.
  - Next: Task 8 — preserve lifecycle orchestration, partial-failure semantics,
    and `.smc` compatibility through the new owner.

- 2026-08-04: Phase 1 Task 6 GREEN implementation completed.
  - Moved restore-guard ownership to `ProjectSession`.
  - `CalculationStateService` now injects `IProjectSession`, delegates
    `IsLoadProjectInProgress` reads to it, and implements the compatibility
    lease setter rule with one `_restoreLease` field and no local bool/depth
    store.
  - `ResultsViewModel` now accepts `IProjectSession` and uses
    `using var restoreScope = _projectSession.BeginProjectRestore()` in
    `LoadProjectDataAsync`; `ProjectNumber`/`ProjectObject` dirty checks use the
    canonical session guard directly.
  - Added `ProjectStateService.Session` exposure so test adapters can share the
    canonical session during the transition; updated test helpers to construct
    `CalculationStateService` with the same session as `ResultsViewModel`.
  - `SetPipeSpacing` source validation preserved; legacy guard tests pass.
  - Targeted owner/guard lane: 39/39; affected lifecycle lane: 100/101 (1
    skipped); persistence lane: 18/18; Debug/Release production builds: 0 errors.
  - Evidence: `docs/architecture-migration/evidence/phase-1-project-session-shell/restore-guard.md`.
  - Active plan checkbox Task 6 marked completed.
  - Next: Task 7 — DI singleton lifecycle graph registration and consumer rewire.

- 2026-08-04: Phase 1 Task 5 GREEN implementation completed.
  - Converted `src/Services/Results/ProjectStateService.cs` into a forwarding-only
    compatibility adapter backed by a single `IProjectSession` reference.
  - Removed all mutable lifecycle fields from `ProjectStateService`; identity,
    path, dirty and guard state now live only in `ProjectSession`.
  - `ProjectStateServiceTests` continue to pass; the class is retained as a
    compatibility surface rather than deleted.
  - Targeted adapter/guard lane: 35/36 passed; the single remaining failure is
    `CalculationStateService_HasNoLocalRestoreGuardBackingField`, which belongs to
    Task 6.
  - Affected lifecycle lane (100/101) and persistence lane (18/18) remain green.
  - Evidence: `docs/architecture-migration/evidence/phase-1-project-session-shell/compatibility-adapters.md`.
  - Active plan checkbox Task 5 marked completed.
  - Next: Task 6 — centralize restore guard in `ProjectSession`.

- 2026-08-04: Phase 1 Task 4 GREEN implementation completed.
  - Created `src/Services/Project/IProjectSession.cs` and `src/Services/Project/ProjectSession.cs`
    with the exact lifecycle-only contract: initial values, ordinal equality/idempotency,
    `ArgumentNullException` for null identity setters, single event per real mutation,
    and nested exception-safe `BeginProjectRestore()` leases.
  - Registered `ProjectSession` as singleton and mapped `IProjectSession`,
    `IProjectInfoService`, `IProjectStateService`, and `IMarkDirtyService` to the same
    instance in `src/Configuration/ServiceCollectionExtensions.cs`.
  - `ProjectStateService` and `CalculationStateService` remain untouched; their duplicate
    lifecycle stores will be removed in Tasks 5 and 6.
  - Fixed `ProjectLifecycleFlowCharacterizationTests.cs` test helper signature
    (`IProjectStateService` -> `ProjectStateService`) to satisfy both
    `IProjectStateService` and `IMarkDirtyService` consumers during the transitional
    contract surface.
  - Evidence: `docs/architecture-migration/evidence/phase-1-project-session-shell/project-session-contract.md`.
  - Active plan checkboxes Tasks 3 and 4 marked completed.
  - Next: Task 5 (legacy contract forwarding-only surfaces and removing duplicate
    lifecycle stores).

- 2026-08-04: Phase 1 Task 2 lifecycle-owner and compatibility RED tests завершены
  sequentially after Task 1 acceptance. Added tests are limited to
  `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs` and
  `ProjectSessionLegacyStoreGuardTests.cs`; evidence is
  `docs/architecture-migration/evidence/phase-1-project-session-shell/tdd-owner-red.md`.
  Atlas re-ran the targeted command and confirmed expected RED compile failure
  `CS0246` for missing future `IProjectSession`; no production files under `src/`
  were edited. Active plan checkbox Task 2 marked completed. Next sequential
  task is Task 3 RED lifecycle-flow/failure characterization; Task 4 GREEN
  implementation remains forbidden until Task 3 is complete.
- 2026-08-04: владелец принял Phase 1 Task 1 baseline checkpoint сообщением
  `если да - то принимаю` после узкой read-only финализации count mismatch.
  Разрешённый вопрос `247 vs 248` решён: `baseline-git-status.bin` содержит 247
  non-empty status records, trailing NUL создаёт 248 split parts, rename/copy
  companion records отсутствуют, исходная формулировка `248` была factual receipt
  error. `baseline.md` исправлен только в factual count строках, checkbox Task 1
  в active plan отмечен completed. Production code/tests не изменялись; Task 2 и
  Task 3 не начаты.
- 2026-08-04: владелец отдельной явной командой
  `/architecture-start phase-1-project-session-shell` авторизовал исполнение
  reviewed canonical plan SHA-256
  `011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948`.
  Перед переходом повторно прочитаны repository/dossier instructions, context и
  active plan; подтверждены requested/current phase equality, `Stage =
  approved`, owner approval, terminal Momus `[OKAY]`, identities `Metis - Plan
  Consultant`, `Prometheus - Plan Builder`, `Momus - Plan Critic`, Git root
  `D:/IA/ace v.2` и существующий большой dirty worktree. Workflow переведён в
  `executing`, execution authorization записана. Первый и единственный
  разрешённый шаг — Task 1 read-only baseline capture; production code и tests
  до его независимой приёмки не изменяются.
- 2026-08-04: по owner stop-request после текущего verifier-вызова новые agents
  не запускались и следующий пункт плана не начинался. Фактически в
  `docs/architecture-migration/evidence/phase-1-project-session-shell/` уже
  существуют Task 1 baseline artifacts, включая `baseline.md`,
  `baseline-git-status.bin`, build/test logs и post/final status binaries.
  Однако independent verifier session `ses_032cb3c54ffehlXmlNcDW7yXjN` не дошла
  до terminal verdict: её результат содержит только частичную проверку
  root/HEAD/branch/upstream/ahead-behind и намерение выполнить binary/log
  verification. Поэтому Task 1 остаётся unaccepted/in progress; Task 2, Task 3 и
  любые production/test edits остаются запрещены до завершения независимой
  проверки Task 1.
- 2026-08-04: владелец явно утвердил reviewed canonical Phase 1 plan
  `docs/architecture-migration/plans/phase-1-project-session-shell.md` с
  SHA-256 `011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948`.
  Перед записью подтверждены `Current phase = phase-1-project-session-shell`,
  `Stage = awaiting-owner-approval`, exact plan hash, Sisyphus PASS, terminal
  Momus `[OKAY]` и реальные identities `Metis - Plan Consultant`, `Prometheus -
  Plan Builder` и `Momus - Plan Critic`; Git root и защищённый dirty worktree
  также повторно проверены. Workflow переведён только в `approved`, `Execution
  authorization` оставлена `pending`; production code, tests, maps, model и
  widget не изменялись. Следующий отдельный gate:
  `/architecture-start phase-1-project-session-shell`.
- 2026-08-03: owner передал separate primary Plan Mode session
  `ses_037ba1831ffep3dtJaDlFJeHVR`; metadata подтверждает
  `Prometheus - Plan Builder`, а final artifact находился в
  `.omo/plans/phase-1-project-session-shell.md`. Первый Sisyphus review нашёл
  неверный `$start-work` execution handoff. Первый Momus review в session
  `ses_0379213acffeHdyOTC2EpH6I3K` вернул `[REJECT]`, потому что Task 4 ссылалась
  на отсутствующий `.omo/drafts/phase-1-project-session-shell.md`. Та же primary
  Prometheus session встроила exact public `IProjectSession` API, null/event/
  idempotency и nested exception-safe restore lease semantics, убрала missing
  ledger dependency и закрепила обязательные `/architecture-approve` затем
  `/architecture-start` gates без `$start-work`/PR/ship bypass. Corrected source
  и repository import byte-identical, SHA-256
  `011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948`.
  Sisyphus review после correction дал PASS; continued Momus session identity
  подтверждена как `Momus - Plan Critic` и вернула terminal `[OKAY]`. Workflow
  переведён только в `Stage = awaiting-owner-approval`; production code, tests,
  maps, model и widget не изменялись. Следующий отдельный gate:
  `/architecture-approve phase-1-project-session-shell`.
- 2026-08-03: после явного запроса продолжить planning workflow завершён
  Phase 1 Metis pre-analysis в существующей session
  `ses_037db6f56ffe7zEyJPEiRLvk4t`; metadata подтверждает agent
  `Metis - Plan Consultant`. Metis зафиксировала source-backed current seams,
  single-writer и restore/reactive risks, обязательную characterization сетку,
  dirty-worktree protections, narrow shell-only scope и пять owner decisions.
  Git root повторно подтверждён как `D:/IA/ace v.2`; branch `master` опережает
  `origin/master` на пять commits, существующий dirty worktree сохранён без
  staging/revert/overwrite. Phase 1 plan artifact отсутствует. Workflow
  planning-only переведён с завершённой parent Phase 0.5 на
  `Current phase = phase-1-project-session-shell`,
  `Stage = awaiting-primary-plan`. Следующий gate - отдельная primary Plan Mode
  session с `/architecture-draft phase-1-project-session-shell`; implementation,
  owner plan approval и execution authorization не подразумеваются.
- 2026-08-03: владелец exact statements `YES по всем 8 вопросам. Результат Phase
  0.5 Architecture Explorer MVP принимаю.` и `Phase 0.5 принята.` ответил `YES`
  на все восемь owner questions и явно принял результат Architecture Explorer
  MVP. Это supersedes предыдущий `awaiting-owner-acceptance` checkpoint;
  workflow переведён в `Stage = completed`. Phase 1 остаётся not started и
  требует отдельного planning, review, owner plan approval и execution
  authorization. Acceptance-only transition не меняла production code, widget,
  accepted architecture foundation, deferred correction, amendment F1-F5 или
  original parent Task 5 и не авторизовала их исполнение.
- 2026-08-03: Architecture Explorer MVP Task 4 завершила финальный
  technical/browser/visual gate. После исправления реального DOM wrapper для
  Migration group header и mobile labeled-record layout два generation passes
  дали byte-identical `14279246`-byte HTML, SHA-256
  `11E7BDC33E5BC03AA09553844234263FC311EADD29D3CD9ECB0F1D711C6B6ACF`.
  Exact `14/14` checks, четыре isolated failure probes и browser matrix
  Overview/ProjectSession/Migration x `375/768/1280` прошли; console/network и
  overflow чистые. Fresh image-capable inspection дала `PASS`; source/functional
  Oracle session `ses_038d77042ffePHmGYUUrqGbLIV` дала `PASS`, high confidence,
  blockers empty. Workflow переведён в `awaiting-owner-acceptance`; восемь owner
  answers и Phase result acceptance остаются pending, Phase 1 не запущена.
- 2026-08-03: после технического завершения Architecture Explorer MVP Task 3 и
  fresh exact-width browser/visual gate владелец exact message `фиксируем` дал
  explicit Task 3 visual `PASS`. Canonical standalone HTML имеет `14073817`
  bytes, SHA-256
  `F0E9F32EB0872BC504DF935D9BF96C037CE9D9523CAD4E8558A521853EE5FF65`;
  generation deterministic. Direct assertions подтвердили exact lifecycle,
  четыре slices, outside-root, 8 boundary invariants, 8/22 flows, full
  `11/3/15/6` reference catalogs, reference integrity и отсутствие synthetic
  model records. Atomic Playwright pass на `375/768/1280` дал no overflow,
  errors, warnings или external requests; independent session
  `ses_039e0c59cffeu3Ccq64vsVQPiZ` вернула `PASS`, high confidence, blockers
  empty. Workflow переведён в `Stage = blocked` как safe pause before Task 4;
  Task 4 и Phase 1 не авторизованы.
- 2026-08-03: владелец exact instruction `csharp-ls, потом приступаем к Task 3`
  снял прежнюю safe pause перед Architecture Explorer MVP Task 3. Для текущего
  .NET SDK 8.0.418 установлен совместимый global tool `csharp-ls 0.16.0`;
  `lsp_status` сообщает C# server installed. Workflow переведён из `blocked` в
  `executing` только для Task 3 approved write-set. Accepted model/schema/
  runtime/validator и production code остаются read-only; Task 4, Phase 1,
  correction и F1-F5 заблокированы до отдельного owner gate.
- 2026-08-03: владелец после проверки исправленного Architecture Explorer MVP
  Task 2 artifact дал explicit visual `PASS` exact message `отлично task 2
  утверждаю`. В том же сообщении exact instruction `пока 3 не начинаем`
  установила явную паузу перед Task 3. Task 2 считается технически и визуально
  принятой в своей stage; workflow переведён в `Stage = blocked` как безопасный
  checkpoint, `Phase result acceptance` остаётся `pending`. Никакая работа Task
  3 не начата; возобновление требует нового explicit owner instruction.
- 2026-08-03: по явному запросу владельца выполнена узкая UI-коррекция Task 2
  без новой тяжёлой review-wave. В
  `widget/architecture-widget.css` `.panel-results` переведена на полный span
  сетки; таблица получила fixed fluid layout и перенос содержимого вместо
  forced `860px/720px` minimum widths и горизонтальной прокрутки. Семь колонок
  сохранены. Два generation passes byte-identical: canonical
  `architecture-widget.html` имеет `13981737` bytes и SHA-256
  `B3D5DBF5B7FCCC9F6ED67E46CD7068B23D8BF40769E3035089FA9D15009D1C74`.
  Browser metrics на `375/768/1280` показывают равные `scrollWidth` и
  `clientWidth` для root и table container; карточка имеет `grid-column: 1 / -1`.
  Owner visual gate остаётся pending, Task 3 и Phase 1 не разблокированы.
- 2026-08-02: владелец после visual review Architecture Explorer MVP Task 1
  shell дал explicit visual `PASS` exact message `согласовано`. Это подтверждает
  только Task 1 visual gate после уже записанной independent technical
  verification и releases only Architecture Explorer MVP Task 2 as the sole
  authorized next action. Workflow остаётся `Stage = executing`; active plan
  остаётся
  `docs/architecture-migration/plans/phase-0.5-architecture-explorer-mvp.md` с
  SHA-256
  `9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1`; `Phase
  result acceptance` остаётся `pending`. Task 3 требует отдельный owner `PASS`
  после Task 2 visual gate; Phase 1, correction и F1-F5 остаются
  blocked/deferred.
- 2026-08-02: Architecture Explorer MVP Task 2 persisted generated artifact и
  independently confirmed technical completion. Earlier verifier session
  `ses_03c84fa39ffeGij7yNJ762JHuy` сохранилась как честная история: final
  `REJECTED` with zero assertions only because it incorrectly treated
  historical v1 hashes `7AFB...`/`EE9E...` as current v2 hashes. После v2
  receipt reconciliation fresh full verification in session
  `ses_03c773098ffeQbTL4jjvY05kCB` returned final `CONFIRMED` with `101932`
  assertions. Canonical generated artifact
  `docs/architecture-migration/architecture-widget.html` has exact size
  `17962258` bytes and SHA-256
  `465832FCA43682FE5D908C20C8F1EE76742B3FA3EAB13C7643F5877163047D86`; exactly
  one payload is embedded. Verified facts: Current/Target/Diff runtime parity;
  exactly six views and all `63` non-empty combinations; no duplicate IDs;
  stable-ID Diff fields/reasons/invariant markers; Target unimplemented with
  honest valid-empty semantics; five supported classes including zero-count
  classes; controlled state hooks; standalone local/offline behavior; no Task
  3/4 scope leak. Historical underscore HTML remains provenance-only at
  `37294` bytes, SHA-256
  `D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA`.
  Isolated failure preserved output and cleaned temp files. Current accepted v2
  schema/model hashes are
  `8A0BC79C00FBD8F1D2C2E52E70085DF0472E3675B99B9C5EC9209FA8EEB4C97B` and
  `F573E175C28AA9BEB9DD1809EB49E34B41F182BA2742132E93C40639804EFA97B`; older
  `7AFB...`/`EE9E...` remain historical v1 source hashes per
  `phase-0.5-model-validation-v2.md`, not current v2 hashes. TypeScript LSP
  remained unavailable due to prior declined installation; `node --check`
  passed. Workflow remains `executing`, `Phase result acceptance` remains
  `pending`, mandatory next stop is owner visual review, Task 3 remains
  forbidden until separate explicit owner `PASS`, and Phase 1/correction/
  `v2-amendment-f1` through `v2-amendment-f5` remain blocked/deferred.
- 2026-08-02: Architecture Explorer MVP Task 1 реализована в четырёх exact
  paths и независимо подтверждена verifier session
  `ses_03cfba227ffeq5cdY5SRKrrc87` с verdict `confirmed`, high confidence.
  Canonical generation exit `0`; два passes byte-identical: `383453` bytes,
  SHA-256 `A1FA7F599A0DEC0607DDC10E97DABEE49C69100E4EE1693577B202150EB47824`.
  Exactly-one embedded payload парсится, accepted identity совпадает; network/
  external/runtime sibling dependencies отсутствуют; controlled startup error
  присутствует; isolated missing-input probe nonzero и sentinel-preserving;
  repository temp files отсутствуют. Historical `architecture_widget.html`
  сохранил exact accepted bytes/hash. TypeScript LSP недоступен из-за ранее
  отклонённой установки, но `node --check` прошёл. Workflow остаётся
  `executing`; следующий обязательный gate — owner visual review. Task 2 не
  начиналась и требует explicit owner `PASS`.
- 2026-08-02: владелец явно снял safe pause сообщением `продолжай`. Перед
  возобновлением повторно прочитаны repository/dossier instructions и полный
  context, подтверждены Git root и защищённый dirty worktree. Фактическая
  проверка показала отсутствие всех четырёх ожидаемых Task 1 implementation
  artifacts; это согласуется с записью, что DoneClaim не был принят, и не
  требует rollback. Workflow возвращён из `blocked` в `executing` только для
  Task 1. После implementation и independent verification обязателен owner
  visual stop; Task 2 остаётся неавторизованной.
- 2026-08-02: владелец потребовал поставить работу на паузу во время Task 1.
  Дальнейшее исполнение остановлено до проверки результата implementation lane:
  DoneClaim и independent verification ещё не приняты, generation/visual review
  владельцу не предъявлялись, Task 1 не отмечена completed. Workflow переведён
  в `blocked` как safe checkpoint. После explicit resume разрешено сначала
  inspect/verify только Task 1 write-set и остановиться на owner visual gate;
  Task 2 не авторизована.
- 2026-08-02: владелец явно вызвал
  `/architecture-start phase-0.5-architecture-explorer-mvp`. Authorization gate
  повторно прочитал оба `AGENTS.md`, полный `TASK_CONTEXT.md` и active plan;
  подтвердил repository root `D:/IA/ace v.2`, branch `master`, защищённый dirty
  worktree, exact plan size 19880 bytes и SHA-256
  `9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1`, owner
  plan approval, terminal Momus `[OKAY]` и identities `Metis - Plan Consultant`,
  `Prometheus - Plan Builder`, `Momus - Plan Critic`. Workflow переведён из
  `approved` в `executing`; execution authorization записана. Разрешена только
  Task 1 с обязательной остановкой до explicit owner visual `PASS`.
- 2026-08-02: владелец явно утвердил
  `docs/architecture-migration/plans/phase-0.5-architecture-explorer-mvp.md`
  сообщением `утверждаю план`. Перед записью подтверждены `Current phase`,
  `Stage = awaiting-owner-approval`, наличие exact 19880-byte plan с SHA-256
  `9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1`,
  terminal Momus `[OKAY]` и реальные identities Metis, Prometheus Plan Builder
  и Momus. Workflow переведён только в `approved`; Architecture Explorer MVP
  execution authorization оставлена `pending`, реализация, HTML и browser не
  запускались. Следующий отдельный gate —
  `/architecture-start phase-0.5-architecture-explorer-mvp`.
- 2026-08-02: владелец сообщением `OKAY` явно разрешил предложенные одноразовый
  planning-only mirror `.omo/plans/phase-0.5-architecture-explorer-mvp.md` и
  один Momus content review. До review подтверждена byte identity mirror и
  canonical plan: SHA-256
  `9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1`.
  Продолженная Momus session `ses_0412cb8cfffeQ0Qr4hAyIL5tyO` вернула
  terminal `[OKAY]`: четырёхэтапный путь, exact write-sets, read-only
  foundation, owner gates и deterministic generation/check commands признаны
  исполнимыми и internally consistent. Canonical plan не изменён. Этот review
  не является owner plan approval или execution authorization; workflow
  остаётся `awaiting-owner-approval`, реализация не запускалась. Следующий
  отдельный gate —
  `/architecture-approve phase-0.5-architecture-explorer-mvp`.
- 2026-08-02: владелец выбрал option 2 и явно отказался от дополнительного
  Momus content review для Architecture Explorer MVP. Единственный разрешённый
  Momus round `ses_0412cb8cfffeQ0Qr4hAyIL5tyO` сохранился как честный wrapper
  `REJECT` без content finding; `.omo/plans` mirror и повторный review не
  разрешены и не создавались. Canonical plan SHA-256
  `9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1`
  остаётся неизменным и прошёл Sisyphus scope/decision-completeness review.
  Workflow переведён только в `awaiting-owner-approval`. Waiver не является
  plan approval или execution authorization; следующий отдельный gate —
  `/architecture-approve phase-0.5-architecture-explorer-mvp`.
- 2026-08-01: separate primary Prometheus session
  `ses_04145a922ffe1bfEwxVLALfgqG` (identity validated as
  `Prometheus - Plan Builder`) produced a corrected short Architecture Explorer
  MVP plan after Sisyphus removed the wrong generated path, dirty-worktree
  reconstruction and F-wave. Canonical import
  `plans/phase-0.5-architecture-explorer-mvp.md` is 19880 bytes with SHA-256
  `9C23655B3786C26AFD715FF8441E2215FEDF0D42DE6A12720BF5E94D3AF59BF1`.
  It contains exactly four vertical stages, creates `architecture-widget.html`
  in Stage 1 and keeps correction/F1-F5/Phase 1 deferred. The single allowed
  Momus round `ses_0412cb8cfffeQ0Qr4hAyIL5tyO` (identity validated as
  `Momus - Plan Critic`) returned terminal wrapper `REJECT` because it accepts
  only `.omo/plans/*.md`; no content review or content finding occurred. Per
  owner no-loop instruction no second review or mirror was started. Workflow is
  `blocked` before owner approval and execution authorization, pending an owner
  decision on a one-time `.omo` review mirror versus waiving Momus content
  review.
- 2026-08-01: владелец снял паузу только для planning-only этапа
  `phase-0.5-architecture-explorer-mvp`. Metis session
  `ses_04153ac3bfferXuFJEvXVWDyWf` (identity подтверждена как
  `Metis - Plan Consultant`) рекомендовала не создавать параллельную phase
  authority: новый короткий plan должен узко supersede execution scope
  незапущенных original Tasks 5-10, сохранив accepted model v2/runtime/schema/
  validator и amendment Tasks 1-5. Первый implementation stage обязан создать
  открываемый `architecture-widget.html`; всего допускается не более четырёх
  вертикальных stages с owner visual stop после каждого. Workflow переведён в
  `awaiting-primary-plan`; plan artifact, Momus review, approval и execution
  authorization ещё отсутствуют. Следующий gate - отдельная primary Plan Mode
  session `/architecture-draft phase-0.5-architecture-explorer-mvp`.
- 2026-08-01: владелец поставил architecture workflow на паузу, поскольку
  verification process стал непропорционально сложным и отложил видимый
  результат. Reviewed correction plan SHA-256
  `AA62A63C523F8E28EC52E8151F4786792AE134C207BAAF7B1819F6E5D967C925` не
  утверждён и сохранён только как `deferred` planning artifact; execution
  authorization отсутствует. Canonical model v2, runtime и принятые amendment
  Tasks 1-5 сохранены. Следующий возможный этап после отдельного owner resume -
  новый короткий Architecture Explorer MVP plan: локальный HTML, Overview,
  ProjectSession, Migration/Diff, evidence drill-down и ручная приёмка по восьми
  вопросам до решения об объёме автоматических проверок. Никакое исполнение,
  F1-F5, original Task 5 или Phase 1 этим решением не запущены.
- 2026-08-01: владелец явно разрешил узкую context-only workflow repair.
  `Current phase` временно установлена в
  `phase-0-5-v2-amendment-verifier-entrypoints-correction`, а `Active plan` — в
  canonical reviewed correction plan SHA-256
  `AA62A63C523F8E28EC52E8151F4786792AE134C207BAAF7B1819F6E5D967C925`.
  Original Phase 0.5 plan/hash сохранены без изменений в parent fields и paused
  до будущего amendment F5 APPROVE. `Stage` возвращён в
  `awaiting-owner-approval`; prior approval statement не был автоматически
  переиспользован, owner approval и execution authorization остаются pending.
  Verifier, amendment F1-F5, original Task 5 и Phase 1 не запускались.
- 2026-08-01: владелец явно сообщил `утверждаю план` для correction plan
  SHA-256 `AA62A63C523F8E28EC52E8151F4786792AE134C207BAAF7B1819F6E5D967C925`.
  Обязательный `/architecture-approve` gate остановился до записи approval:
  requested phase `phase-0-5-v2-amendment-verifier-entrypoints-correction` не
  совпадает с persisted `Current phase=phase-0.5-model-driven-architecture-widget`,
  а `Active plan` всё ещё указывает parent plan. Approval не записан и
  execution authorization остаётся pending. Workflow переведён в `blocked` до
  явного owner выбора узкой context-only repair, делающей correction временной
  current/active authority при сохранении original Phase 0.5 как paused parent.
- 2026-08-01: владелец выбрал option B и сузил filesystem threat model до
  cross-platform обнаружения случайного drift в доверенной локальной среде.
  Primary Prometheus session `ses_046a6bb36ffe6zQIErApbaCXid` заменила
  descriptor-relative no-follow/same-descriptor requirements на exact eight-step
  contract: realpath, repository containment, regular pre-stat, one byte read и
  SHA-256, post-stat, stable `dev`/`ino`/`size`/`mtimeMs`, expected hash и
  fail-closed без acceptance receipt. Active attacker, intentional TOCTOU и
  malicious reparse races явно вне threat model; native helper, FFI и новые
  dependencies запрещены. Five negatives: outside-root, directory,
  between-check mutation, hash mismatch и disappearance. Final source/import
  имеют 46902 bytes и SHA-256
  `AA62A63C523F8E28EC52E8151F4786792AE134C207BAAF7B1819F6E5D967C925`.
  Sisyphus review дал PASS; Momus session
  `ses_0419b9238ffeRyVqOBNDzSXF5Z` дала `OKAY` на exact SHA. Canonical import
  byte-identical. Wrapper session `ses_041942fe6ffek0d1kujsTLCjQC` отклонила
  только repository-path namespace, поскольку принимает исключительно
  `.omo/plans/*.md`; это не content finding. Workflow остановлен на
  `awaiting-owner-approval`; implementation, F1-F5, original Task 5 и Phase 1
  не начинались.
- 2026-08-01: repository-facing Momus session
  `ses_041bbf06dffe5YcFTKSfCywLbj` rejected imported correction plan SHA-256
  `E350B89332EDD380424F50337825F816795E9DF4101899D0413B3EA731654688` because
  Todo 8 and Final F1/F4 bound verification to the `.omo` source path instead
  of the canonical repository plan path. The same primary Prometheus session
  corrected its source to 40602 bytes / SHA-256
  `42145A3E9BE96E8CD6371939449554F7A634E2EB5B6DF987EC293AD6ADAA4F58`, but
  mandatory review round `verifier-entrypoints-r7-20260801` was not accepted:
  Momus `ses_041b2a120ffeyUgAMFMk6K4D8e` could not establish the required
  descriptor-relative no-follow/same-descriptor hashing runtime and Oracle
  `ses_041b2598bffeuLdcQbUF6e86V4` returned no terminal review deliverable.
  Therefore the corrected source was not imported, the earlier repository plan
  is rejected/non-executable, and workflow moved to `blocked` under the owner
  stop-rule. Verifier implementation and amendment F1-F5 remain unstarted.
- 2026-08-01: переданная owner-side session
  `ses_046a6bb36ffe6zQIErApbaCXid` проверена: metadata подтверждает primary
  `Prometheus - Plan Builder`, а latest correction work сохранил durable draft
  `.omo/drafts/phase-0-5-model-driven-architecture-widget-v2-amendment-verifier-entrypoints-correction.md`.
  Draft имеет `status: awaiting-approval` и `plan_sha256: null`; его approval
  brief явно требует owner approval только для записи decision-complete `.omo`
  plan. Поэтому draft не импортирован как план, Sisyphus/Momus review не
  запускался, workflow остаётся `awaiting-primary-plan`, implementation и F1-F5
  не авторизованы.
- 2026-08-01: владелец выбрал `Добавить entrypoints (Recommended)` для
  подтверждённого F1-F5 CLI blocker-а. Planning-only Metis session
  `ses_04251de93ffeVZCamLK6AdVNll` рекомендовала отдельный узкий correction
  overlay: canonical suites `v2-amendment-f1` through `v2-amendment-f5` должны
  быть добавлены в существующий `widget/verify-widget.mjs` без standalone
  verifiers, изменения approved amendment plan/F1-F5 criteria или переоткрытия
  independently accepted Tasks 1-5. Correction должна сохранить retry-1 как
  единственную Task 1 authority, иметь narrow verifier pre/post boundary,
  regression reruns `model-v2`/`runtime-v2` и отдельные owner plan approval и
  execution authorization. Workflow переведён в `awaiting-primary-plan`; план
  correction ещё не создан, verifier не изменён, F1-F5, original Task 5 и
  Phase 1 остаются unstarted/blocked.
- 2026-08-01: after amendment Task 5 was independently confirmed, the required
  F1 canonical command was probed without a repository output. It exited
  nonzero before suite dispatch with `cli-arguments: suite, schema, model and
  output are required`. Current `widget/verify-widget.mjs` supports only
  `model-v2` and `runtime-v2`; the approved F1-F4 lanes are read-only except for
  their distinct receipts and therefore cannot add the missing shared verifier
  entrypoints. Under the owner infeasibility stop-rule, `Stage` changed to
  `blocked`. Original Task 5, amendment F1-F5 and Phase 1 remain unstarted.
  Owner options: approve a reviewed correction adding F1-F5 entrypoints;
  authorize standalone receipt verifiers; or waive/replan the amendment final
  verification wave.
- 2026-08-01: amendment Task 5 scope gate completed with terminal `PASS`.
  Read-only re-runs of `model-v2` and `runtime-v2` exited `0`, confirming Task 3
  `33/21` and Task 4 `47/20` plus `78` direct assertions. The accepted retry-1
  ledger reconstructed exactly `74` persisted rows, `4830` bytes, SHA-256
  `2C23AEA259D3233DD9FD8B959C29951EB259E795CD81A3B34BDAADD3DC6FDC96`;
  protected mismatches and forbidden changed paths were both `0`. Workflow is
  restored to original Phase 0.5 authority with the original plan hash active,
  `Stage=executing`, phase acceptance pending, and original Task 5 as the sole
  next action. Amendment plan/hash and technical acceptance remain overlay
  metadata; amendment F1-F5 are unstarted; Phase 1 remains blocked.
- 2026-08-01: amendment Task 2 produced only
  `evidence/phase-0.5-v1-to-v2-mapping.json`. Independent verifier session
  `ses_0435ffd52ffeRkE7OnxSpEnhdQ` returned `VERDICT: CONFIRMED` with 98 passed,
  0 failed and 0 blockers. Exact v1 source hashes, 16 top-level keys, 245
  records, 112 edge semantics, 280 global IDs and 355 references reconcile with
  zero unexplained items; only approved transformations are present and Target
  remains unfabricated. Task 3 is the sole next action; runtime Task 4 remains
  blocked.
- 2026-08-01: safe checkpoint resumed. The corrected Task 1 retry receipt now
  persists all 74 binary-safe ledger rows. Independent verifier session
  `ses_04371f0b0ffeGcDIR4Hbf2edMM` reproduced the exact 4830-byte porcelain
  basis and SHA-256
  `2C23AEA259D3233DD9FD8B959C29951EB259E795CD81A3B34BDAADD3DC6FDC96`,
  validated protected hashes and adversarial parser cases, and returned
  `VERDICT: CONFIRMED` with 34 passed, 0 failed, 0 blockers. `Stage` returned to
  `executing`; Task 2 is the sole next action and schema/model/runtime remain
  untouched by the amendment.
- 2026-08-01: execution paused safely at owner request before shutdown. No
  background task remains. The current Task 1 retry receipt is independently
  `REJECTED` only because its claimed 74-row ledger was not persisted in the
  Markdown; the raw 4830-byte basis and SHA-256
  `2C23AEA259D3233DD9FD8B959C29951EB259E795CD81A3B34BDAADD3DC6FDC96`,
  protected hashes, failed-receipt immutability and one-path delta were
  reproduced. Several correction lanes returned planning text but made no
  edits. `Stage` is `blocked` as a safe checkpoint. Resume by correcting only
  retry-1 and independently accepting Task 1; Task 2 remains unstarted.
- 2026-08-01: владелец выбрал option 1 и явно разрешил узкое исключение для
  одного нового Task 1 evidence path:
  `evidence/phase-0.5-v2-amendment-repository-snapshot-retry-1.md`. Failed
  receipt остаётся byte-immutable; retry обязан проверить взаимное соответствие
  raw NUL stream, parsed ledger count/paths и reconstructed hash. Plan SHA,
  архитектурный scope и acceptance criteria не меняются. `Stage` возвращён в
  `executing` только для исправленного Task 1; Task 2 остаётся blocked до его
  независимой приёмки.
- 2026-08-01: amendment Task 1 attempt created only
  `evidence/phase-0.5-v2-amendment-repository-snapshot.md` and stopped with
  terminal `BLOCKED`. Independent read-only verification proved that the
  receipt's recorded raw porcelain SHA already represented 73 pre-existing
  rows, but its rendered 70-row ledger dropped three pre-existing Unicode
  paths. Their bytes match earlier accepted snapshots; active plan, v1 schema,
  v1 model and every non-receipt path remain unchanged. This is a correctable
  parser/capture defect, not confirmed v2-plan infeasibility. Because failed v2
  receipts are immutable and the approved allow-list has no retry path,
  `Stage` is `blocked` pending an explicit owner exception for one versioned
  Task 1 retry receipt or a plan revision. No Task 2 work is authorized.
- 2026-08-01: fresh explicit owner command
  `/architecture-start phase-0.5-model-driven-architecture-widget-v2-amendment`
  accepted after live authorization-gate revalidation. Exact active plan hash,
  owner approval, Metis/Prometheus/Momus identities, repository
  root/branch/upstream/HEAD and complete porcelain-v1 dirty boundary matched the
  approved workflow. `Stage` changed from `approved` to `executing`; amendment
  Task 1 is the only released implementation task. Parent Task 5 and Phase 1
  remain blocked, and the owner infeasibility stop-rule remains binding.
- 2026-07-30: создано отдельное migration dossier; выбран `ProjectSession` как
  составной aggregate root со state-срезами.
- 2026-07-30: добавлен persisted workflow с двумя обязательными owner gates:
  утверждение плана и принятие результата фазы.
- 2026-07-30: первый planning run признан недействительным. Команда была
  привязана к built-in `build`, а session metadata показала `explore` для
  якобы Metis (`ses_04d4d84dfffee72H6C2V2426z2`), Prometheus
  (`ses_04d49ae1effePgsUqP0TUfeQRC`), Sisyphus review
  (`ses_04d3ca7faffeI4P21v5608BEdQ`) и Momus
  (`ses_04d3b883ffferv2PCEKnaVwgkN`). Draft архивирован; receipts сброшены;
   команды требуют реальные agent identities `metis`, `plan`, `momus`.
- 2026-07-30: уточнена граница identity evidence. Session
  `ses_04d2743feffeNNR10nF6erGcRi` доказала `agent=plan`, модель
  `customapi/gpt-5.6-sol` и `Parent=Sisyphus`, но не effective mapping роли
  Prometheus. Active OpenCode config доказывает загрузку OMO plugin; отдельный
  active OMO mapping config не найден, а backup не является runtime receipt.
  Child session сохранена только как discovery input. Draft перенесён в
  отдельную primary Plan Mode session через `/architecture-draft`.
- 2026-07-30: primary session `ses_04cd7fd1bffeKlpQzMsOOW1zOa` подтверждена
  как `Prometheus - Plan Builder`; план импортирован побайтово идентично в
  `plans/phase-0-baseline.md` и прошёл primary Sisyphus scope review. Momus
  session `ses_04ca60154ffeY92oqBYKN1XYbZ` вернула REJECT: F1-F4 не содержат
  конкретных QA-сценариев и конфликтуют из-за параллельного append в общий
  `final-verification.md`. Workflow переведён в `prometheus-corrections` без
  owner approval или исполнения Phase 0.
- 2026-07-30: Prometheus исправил оба blocking findings в той же primary
  session: F1-F4 получили конкретные agent-executable QA-сценарии и отдельные
  immutable receipts, F5 выполняет последовательную агрегацию. Канонический
  план синхронизирован с SHA-256
  `BB6F92470A4BF786FE90F8A86F2B34F3B04BEE3C5AC2654C9A45AEB75F87CC6E`.
  Продолженная Momus session `ses_04ca60154ffeY92oqBYKN1XYbZ` вернула PASS;
  workflow остановлен на `awaiting-owner-approval` без исполнения Phase 0.
- 2026-07-30: владелец явно утвердил проверенный план
  `plans/phase-0-baseline.md` с SHA-256
  `BB6F92470A4BF786FE90F8A86F2B34F3B04BEE3C5AC2654C9A45AEB75F87CC6E`.
  Workflow переведён в `approved`; `Execution authorization` оставлен
  `pending`, Phase 0 не запускалась. Следующий отдельный gate:
  `/architecture-start phase-0-baseline`.
- 2026-07-30: владелец явно запустил
  `/architecture-start phase-0-baseline`. Authorization gate повторно проверен:
  active plan SHA-256 совпадает, Stage и owner approval допустимы, Metis,
  primary Prometheus и Momus receipts подтверждены metadata. Workflow переведён
  в `executing`, execution authorization записана; первый шаг — Todo 1 snapshot.
- 2026-07-30: Todo 1 repository snapshot завершён после независимой
  adversarial verification. Зафиксированы 10 dirty tracked и 20 pre-existing
  untracked paths (30 total), 28 present SHA-256 и 2 deleted states; единственный
  post-capture output — `evidence/repository-snapshot.md`. Следующий шаг — Todo 2.
- 2026-07-30: Todo 2 baseline receipts завершён и независимо подтверждён в
  verifier session `ses_04c1f9b2effeqDVhYGr1kTHl50` без mismatches после
  исправления только `generated_at_utc` в `evidence/test-baseline.md`.
  Зафиксированы `evidence/environment.md`, `evidence/build-baseline.md`,
  `evidence/test-baseline.md` и `evidence/test-results/phase-0.trx`; snapshot
  SHA `f0d19c34ac03075d64548f1059e9c6626d3596b5`. Build receipt показывает exit
  0, 0 warnings, 0 errors; test receipt показывает exit 0. Namespace-safe TRX
  reconciliation даёт 1537 Passed, 3 NotExecuted, 0 Failed; консольное 1538
  объясняется двумя `Explicit` и одним `Assert.Ignore`. Расхождение с
  `Counters/@notExecuted` задокументировано как ограничение logger/adapter и не
  меняет зелёный статус, но сами green tests не доказывают сохранение runtime
  behavior. Следующий шаг — Todo 3.
- 2026-07-30: Todo 3 filtered source metrics завершён и независимо подтверждён
  verifier session `ses_04c0e6ee3ffeoVpGIdN7XgBGfH` без mismatches относительно
  worker session `ses_04c15ce25ffe9ogCaUZXSKBoZJ`. `evidence/metrics-baseline.json`
  после исправления `generated_at_utc` совпадает с независимыми recounts:
  tracked C# 276, raw 360, filtered 275, excluded 85, physical LOC 70635,
  nonblank LOC 60559, lexical declared types 395. Verifier подтвердил zero
  forbidden filtered paths и успешное in-memory исключение `obj/Injected.cs`;
  snapshot SHA остаётся `f0d19c34ac03075d64548f1059e9c6626d3596b5`. SCC count
  остаётся `null/degraded`, cycle count `null/not-reproducible`; эти graph
  metrics не были воспроизведены и не должны выдумываться. Лексические метрики
  declared types не являются compiler-semantic facts. Следующий шаг — Todo 4.
- 2026-07-30: Todo 4 Codegraph provenance and source-evidence coverage завершён
  и независимо подтверждён verifier session
  `ses_04c00e4cbffeZX3ANu6tAEUbgj` без mismatches относительно worker session
  `ses_04c058b71ffepQwGAs1YzXi5uL`. `evidence/codegraph-baseline.md` фиксирует
  пять exact queries CG-01..CG-05, required architecture areas, шесть
  representative typed views и отсутствие stale/disabled Codegraph banner.
  Compile-time, DI/runtime, state, reactive, persistence и user-flow edges
  остаются раздельными типами evidence и не схлопываются в один graph.
  Выборочные Codegraph evidence подтверждены как текущий source-backed baseline,
  но не являются proof repository-wide graph completeness; SCC/cycle/repository-
  wide completeness boundary остаётся явно `degraded`. Следующий шаг — Todo 5.
- 2026-07-30: Todo 5 historical audit reconciliation завершён после исправления
  пяти blocking findings независимого verifier-а и повторно подтверждён в
  session `ses_04bf24477ffeEWzLDp1Y3TJr7n`. `evidence/audit-reconciliation.md`
  содержит 45 уникальных reconciliation rows и 35 material claim bindings:
  15 `confirmed`, 8 `changed`, 18 `not-reproducible`, 4 `not-applicable`.
  Исполняемая structural QA подтвердила zero broken bindings/columns и current
  evidence для всех confirmed rows; exhaustive production search проверил 173
  `src/**/*.cs` файла и не нашёл declaration/token `ProjectSession`.
   Historical counts `14` и `8` cycles не воспроизведены и не являются current
   metrics. Todo 6 разблокирован; следующая волна — partitioned Todos 6-9.
- 2026-07-31: преждевременный worker claim о завершении Todo 10 был удалён из
  workflow state до независимой приёмки. Первый Oracle review в session
  `ses_04b2d3b71ffeX5zQdCJdWhQNQP` вернул `REJECTED`: canonical model терял
  исходные DI/reactive semantics, а receipt не воспроизводил весь заявленный
  deterministic contract. Исправлены только три allow-listed Todo 10 artifacts:
  schema получила конечный `sourceKind` enum и обязательные semantic records;
  baseline model сохранила точные source kinds и structured reactive participants;
  validation receipt проверяет required fields/enums/references, 112 semantic
  records, 14 negative probes и exact six-map sets. Точный inline PowerShell
  block завершился с exit 0; повторный Oracle review в той же session вернул
  `VERDICT: CONFIRMED`. Stage остаётся `executing`, Phase result acceptance —
  `pending`; следующий шаг — Todo 11, без перехода к Todo 12 или owner gate.
- 2026-07-31: Todo 11 создал только два allow-listed specification artifacts.
  `maps/target-invariants.md` фиксирует 15 target invariants и 6 owner-deferred
  decisions; exact QA разрешает cited canonical IDs/ranges и отклоняет
  in-memory target-as-current mutation. `widget-spec.md` задаёт один будущий
  canonical runtime payload, 4 modes, 6 отдельных combinable views,
  direction-sensitive Diff, evidence/accessibility/responsive/offline/error
  contracts и 37 acceptance rows: 24 mode/view pairs и 12 special states.
  Первый Oracle review в session `ses_0493a968bffeOWAqBSX5GI9Z9T` вернул
  `REJECTED` по семи gaps; после исправления single-input/Target, Diff pair и
  precedence, empty-input coverage и обоих validators повторный review вернул
  `VERDICT: CONFIRMED`. `architecture_widget.html`, production, tests, fixtures
  и config не изменялись. Stage остаётся `executing`; следующий шаг — Todo 12.
- 2026-07-31: Todo 12 создал `evidence/dossier-gate.md` и выполнил точный
  self-contained PowerShell gate. После исправления ошибок самого verifier-а
  в обработке case-sensitive vocabulary, fixture-row format и absolute local
  Markdown links полный прогон завершился с exit 0: 607/607 assertions, 26
  required artifacts, 27 Phase 0 outputs и все 30 Todo 1 ledger rows; exact
  allow-list/plan hash, embedded model/target/widget validators, matrices,
  fixture hash, green baseline receipts/TRX и link integrity прошли. In-memory
  altered hash `.gitignore` и orphan endpoint `CTE-001 -> NODE-ORPHAN` были
  отклонены. Full Draft 2020-12 validation остаётся честно `degraded` из-за
   отсутствия установленного validator. Workflow переведён только в
   `verification`; следующий шаг — F1-F4, owner gate не пересечён.
- 2026-07-31: F1-F4 завершились отдельными immutable receipts с terminal
  `APPROVE`. Последовательный F5 повторно проверил их common metadata,
  snapshot/source basis, distinct regular-file identity, terminal syntax (включая
  F3 `**Terminal verdict: APPROVE**`), assertion totals F1 `186/186`, F2
  `2506/2506`, F3 TRX `1540 = 1537 Passed + 3 NotExecuted + 0 Failed` и model
  validator, F4 `445/445`, exact allow-list, отсутствие cross-lane ownership
  violation и сохранность всех 30 Todo 1 status/hash записей, включая Cyrillic
  и deleted paths. Aggregate receipt `evidence/final-verification.md` имеет
  `verdict: APPROVE`; workflow переведён только в `awaiting-owner-acceptance`.
   `Phase result acceptance` остаётся pending до явного решения владельца; Phase
   1 и `completed` не авторизованы.
- 2026-07-31: владелец явно принял результат Phase 0 сообщением
  `ок. принимаю фазу 0`. `Phase result acceptance` переведён в `accepted by
  owner`, workflow — в `completed`. Это принятие не планирует и не запускает
  Phase 1; дальнейшие действия требуют отдельного явного запроса владельца.
- 2026-07-31: владелец решил сначала реализовать model-driven architecture
  widget и только после его отдельного принятия переходить к Phase 1. Создана
  planning-only фаза `phase-0.5-model-driven-architecture-widget`; workflow
  переведён в `metis-analysis`. Реализация Phase 0.5 и любые действия Phase 1
  пока не авторизованы.
- 2026-07-31: Metis pre-analysis завершён в session
  `ses_048ca80c5ffe570LjBtr5wPu7o`; metadata подтверждает agent
  `Metis - Plan Consultant`. Зафиксированы обязательные решения и риски:
  exactly-one runtime payload, сохранение исторического HTML, deterministic
  generation, immutable atomic replacement, stable-ID directional Diff,
  reviewed semantic curation, 37-row browser/accessibility/offline QA и точный
  scope gate. Workflow остановлен на `awaiting-primary-plan`; следующий шаг
  требует отдельной primary Plan Mode session.
- 2026-07-31: session `ses_048ba0157ffemOKK077Y0eU2dj` отклонена как primary
  Prometheus receipt. Metadata сообщает `Agents Used: Sisyphus - ultraworker,
  plan`, transcript показывает delegation через task, а полный Markdown draft
  находится только в child `plan` session `ses_048ba005dffetexUzoA49BmBJI`.
  Согласно identity gate child plan receipt не доказывает отдельную primary
  Prometheus topology. Draft не импортирован, Sisyphus/Momus review не запускался,
  workflow переведён в `blocked`; Phase 0.5 execution и Phase 1 не авторизованы.
- 2026-07-31: отдельная primary session `ses_0489e3f28ffeRPe76tcgM123AS`
  подтверждена как `Prometheus - Plan Builder`; её полный Phase 0.5 draft
  импортирован в `plans/phase-0.5-model-driven-architecture-widget.md`.
  Primary Sisyphus review дал PASS. Momus session
  `ses_048675c40ffebL4qrkfNTFdZBL` сначала отклонила plan из-за недостаточно
  конкретных QA-сценариев Task 5, Task 6 и F5, затем обнаружила dependency cycle
  между Task 6 browser QA и создающим generated artifact Task 7.
- 2026-07-31: Prometheus в той же primary session исправил QA commands,
  assertions и dependency flow: Task 6 ограничен pre-generation design/CSS,
  static contract и blank-page browser availability probe; Task 7 создаёт
  generated artifact; Task 8 впервые исполняет его в browser; Task 9 выполняет
  полный browser/accessibility/responsive/negative-probe pass; F5 имеет
  воспроизводимую aggregation command. Канонический plan и Momus review mirror
  синхронизированы с SHA-256
  `2C056AAFCE062E3E749EC9961E0B55237C4667D8CFFEF5F438CE7F108C2E452E`.
  Продолженная Momus session вернула `OKAY`; workflow остановлен на
  `awaiting-owner-approval`. Owner plan approval, execution authorization и
  Phase result acceptance остаются pending; Phase 1 остаётся blocked.
- 2026-07-31: владелец явно утвердил план Phase 0.5 сообщением
  `ок, план утверждаю`. Перед записью повторно подтверждены active plan SHA-256
  `2C056AAFCE062E3E749EC9961E0B55237C4667D8CFFEF5F438CE7F108C2E452E`, Stage
  `awaiting-owner-approval`, финальный Momus `OKAY` и реальные identities Metis,
  Prometheus и Momus. Workflow переведён только в `approved`; `Execution
  authorization` оставлен `pending`, реализация не запускалась, Phase 1 остаётся
  blocked. Следующий отдельный gate:
  `/architecture-start phase-0.5-model-driven-architecture-widget`.
- 2026-07-31: владелец явно авторизовал исполнение сообщением
  `/architecture-start phase-0.5-model-driven-architecture-widget`. Перед
  переходом повторно подтверждены current phase
  `phase-0.5-model-driven-architecture-widget`, Stage `approved`, owner plan
  approval, active plan SHA-256
  `2C056AAFCE062E3E749EC9961E0B55237C4667D8CFFEF5F438CE7F108C2E452E`, реальные
  identities Metis, Prometheus и Momus, а также финальный Momus `OKAY`.
  Authorization gate успешно revalidated, workflow переведён в `executing`,
  `Execution authorization` записан. Реализация ещё не завершена, `Phase result
  acceptance` остаётся `pending`, Phase 1 остаётся blocked/not started. Первый
  execution action: Task 1, capture the execution-time repository/tool/dirty-
  worktree boundary.
- 2026-07-31: Phase 0.5 Task 1 receipt
  `docs/architecture-migration/evidence/phase-0.5-repository-snapshot.md`
  независимо повторно подтверждён после узкого исправления list typo.
  Executor session `ses_0480d7f79ffeh4f9xeaHazlTph` и verifier session
  `ses_047feeee1ffex1772BYdt6MVdP` сошлись на финальном verdict `confirmed`
  with high confidence. Receipt фиксирует snapshot HEAD
  `f0d19c34ac03075d64548f1059e9c6626d3596b5`, normalized self-hash
  `780C5339EF661216B632C6955D25F5230DBB7D769B52AF12F2C244C02B8BE4C1`, 62
  protected rows, 10 tracked, 52 untracked, 60 present, 2 deleted и ровно один
  Task 1-created receipt. Detection-only probes не нашли browser modules или
  browser executables; это baseline-факт, но не browser gate waiver, потому что
  authoritative browser harness gate остаётся за Task 6. Workflow остаётся в
   `executing`, `Phase result acceptance` остаётся `pending`, следующий шаг —
   Task 2 historical-widget preservation, а Phase 1 остаётся blocked/not
   started.
- 2026-07-31: Phase 0.5 Task 2 historical-widget preservation финально
  подтверждён после исправления единственного rejection: receipt больше не
  заявляет неточный all-62-hashes scope и корректно отделяет mandatory
  post-Task-1 `TASK_CONTEXT.md` workflow update. Executor session
  `ses_047e4cff0ffex3m1yC2qE5qGKy` и verifier session
  `ses_047db634effe5iG3DvsIWPCndT` подтвердили byte, hash, isolation,
  negative-probe и write-set claims. Source/archive identity: 37294 bytes,
  SHA-256
  `D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA`.
  Outputs: `docs/architecture-migration/archive/architecture_widget.phase-0-historical.html`
  and `docs/architecture-migration/evidence/phase-0.5-historical-widget-preservation.md`.
  Оба historical artifacts являются provenance-only, не runtime/generator inputs;
  generated path после Task 2 остаётся отсутствующим. Workflow остаётся в
  `executing`, `Phase result acceptance` остаётся `pending`, следующий шаг -
  Task 3 canonical widget model/schema/validator, Task 6 остаётся browser gate,
  Phase 1 остаётся blocked/not started.
- 2026-07-31: Phase 0.5 Task 3 canonical widget model/schema/validator финально
  подтверждён после первоначального независимого rejection и correction lane.
  Initial executor session `ses_047a23911ffeMsVaDF5W1g94LF`, correction executor
  session `ses_0477d57c1ffe5g1inrp3pd23nL`, verifier session
  `ses_0479222f7ffeeS4phaES1DoAuT`; final `VERDICT: CONFIRMED`, high
  confidence. Canonical CLI exit `0`; model counts
  `79/112/27/22/5/112/11/15/6/3`, global IDs `280`, exact views
  `compile-time`, `di-runtime`, `persistence`, `reactive`, `state-ownership`,
  `user-flow`, invariants `15/15`, deferred decisions `6/6`. Schema/model
  hashes: `7afbce3cfadc8b77443462e52d0bb15c7bdd31569de41fadcaae941791b07194`
  and `ee9e0069d71f3abffa5fa1e9b698a97507694597fbe85b84c272c30d3f55efbd`.
  Real-CLI mutation probes reject `53/53`, including original 13 and correction
  bypass classes; generic Draft 2020-12 validation remains `degraded`, no
  package validator was installed. Task 3 write-set был пять artifacts:
  `maps/architecture-model.widget.schema.json`, `maps/architecture-model.json`,
  `widget/model-contract.mjs`, `widget/verify-widget.mjs` and
  `evidence/phase-0.5-model-validation.md`; correction lane affected only
  validator/receipt, while historical HTML remains provenance-only. This entry
  also explicitly corrects the previous Task 2 journal output path typo from
  `archive/architecture_widget.phase-0.5-task-2.html` to the actual approved
  `archive/architecture_widget.phase-0-historical.html`; this was a context
  typo, not a file rename. Workflow remains `executing`, `Phase result
  acceptance` remains `pending`, Phase 1 remains blocked/not started. Next
  action is exactly Task 4 immutable runtime loading/filters/stable-ID
  directional Diff.
- 2026-07-31: Phase 0.5 Task 4 attempted implementation was independently
  rejected. Executor session `ses_04744edb2ffelVnSBNoZwmyW4b` produced a
  five-path allow-listed write-set and self-suites reporting `37` pass rows,
  but verifier session `ses_047288711ffedvSwSIvF0lpUA3` returned final
  `VERDICT: REJECTED`. Canonical blocker is the v1 contract shape itself: one
  record with top-level values plus `snapshots` membership cannot honestly
  encode different current and target canonical values for one stable ID.
  Additional blocking findings are also canonical: runtime `diff()` has an
  unchanged/unchanged branch and cannot emit `changed`; the validator permits
  the same ID in one group across disjoint snapshots while runtime `by_id`
  overwrites one representation; the acceptance suite lacks a real changed
  assertion; valid-empty/no-match semantics still have a verifier-reported
  defect. Protected Task 3 schema/model hashes remain unchanged at
  `7afbce3cfadc8b77443462e52d0bb15c7bdd31569de41fadcaae941791b07194` and
  `ee9e0069d71f3abffa5fa1e9b698a97507694597fbe85b84c272c30d3f55efbd`, and the
  canonical Task 3 CLI remains green. Workflow therefore transitions from
  `executing` to `blocked`; `Phase result acceptance` remains `pending`; Task 3
  remains the last completed accepted action; Tasks 5-10, F1-F5 and Phase 1
  remain blocked. Proposed clean resolution for owner consideration only, not
  an approved decision: reopen Tasks 3 and 4 under a v2 major contract with one
  stable ID plus `snapshot_states` and no parallel `snapshots` source of truth.
  Alternative owner path: formally weaken the approved Diff requirement and
  replan.
- 2026-07-31: владелец явно выбрал `Перейти на v2 (Recommended)` как решение
  Phase 0.5 Task 4 contract blocker. Каноническое направление теперь: v2
  `snapshot_states` contract с `contract_version=2.0.0`, одним unique stable
  ID, одним runtime document, одним source of truth для snapshot presence/values,
  без параллельного `snapshots` membership source of truth, с model-level
  canonical diff policy и честными
  `added`/`removed`/`changed`/`unchanged`/`unresolved`. Так как contract ещё не
  externally released, v1 compatibility shim не требуется. Это решение
  authorizes planning amendment only, не authorizes schema/model/runtime edits,
  не завершает Task 4, не запускает Task 5, Tasks 5-10, F1-F5 или Phase 1 и не
  пересекает owner gates. Safe next gate: подготовить decision-complete
  amendment для formal reopening Tasks 3 и 4, затем пройти review, explicit
  owner approval и отдельную execution authorization; до этого workflow
  остаётся `blocked`, current approved plan/history сохраняются без изменений.
- 2026-07-31: planning-only Metis pre-analysis v2 amendment завершён в session
  `ses_046bb4fecffeqVxPgX7SXpsiF9`; session metadata подтверждает agent
  `Metis - Plan Consultant`. Binding findings: major v2 contract; один unique
  stable ID и один runtime document; `snapshot_states` как единственный source
  snapshot presence/values без параллельного `snapshots`; per-kind
  identity-level и snapshot-level fields; snapshot-local edge endpoint и flow
  ordering validation; honest Target без fabricated implementation facts;
  исполняемый model-level `canonical_diff_fields`; v1 evidence сохраняется как
  provenance; Tasks 3 и 4 reopen jointly только после reviewed amendment,
  explicit owner approval и отдельной execution authorization. Workflow
  переведён из `blocked` в `awaiting-primary-plan`. Amendment draft и primary
  planning session ID остаются pending; исторический approved v1 plan/hash и
  его прежние owner approvals сохранены только как история. Следующий gate:
  владелец открывает отдельную primary Prometheus Plan Mode session и запускает
  `/architecture-draft phase-0.5-model-driven-architecture-widget-v2-amendment`;
  child-task draft недопустим. Schema/model/runtime, Tasks 3-10, F1-F5 и Phase 1
  остаются заблокированы.
- 2026-07-31: отдельная primary Prometheus Plan Mode session
  `ses_046a6bb36ffe6zQIErApbaCXid` подтверждена metadata как
  `Prometheus - Plan Builder`. Её окончательный planning artifact импортирован
  byte-identically в
  `plans/phase-0.5-model-driven-architecture-widget-v2-amendment.md`: 30476 bytes,
  SHA-256 `B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536`.
  Sisyphus scope/decision-completeness review дал PASS. Momus session
  `ses_04677ca75ffeOkpw7mGdz9x2cv` и Oracle session
  `ses_04677334bffekGA8FIsTxVdXuf` независимо дали `OKAY`, каждый был привязан к
  exact source hash; imported artifact имеет те же bytes/hash. Дополнительный
  Momus wrapper session `ses_04660f9bbffeZZxZVbr82HN2km` не выполняла
  содержательный review и вернула input-path REJECT только потому, что wrapper
  принимает `.omo/plans/*.md`, а не repository-facing `docs/...` path; это не
  finding плана и не отменяет hash-bound Momus `OKAY`. Workflow переведён в
  `awaiting-owner-approval`; amendment owner approval и execution authorization
  остаются pending. Исторический approved Phase 0.5 plan и его прежняя execution
  authorization не распространяются автоматически на amendment. Schema/model/
  runtime не изменялись; amendment Tasks 1-5/F1-F5, original Tasks 5-10/F1-F5
  и Phase 1 остаются заблокированы до explicit approval и отдельного start gate.
- 2026-08-01: владелец явно утвердил
  `plans/phase-0.5-model-driven-architecture-widget-v2-amendment.md` с SHA-256
  `B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536`.
  Перед записью подтверждены Stage `awaiting-owner-approval`, наличие и exact
  hash плана, Sisyphus PASS, Momus/Oracle OKAY и реальные identities Metis,
  primary Prometheus и Momus. Workflow переведён только в `approved`; v2
  amendment execution authorization остаётся `pending`, schema/model/runtime и
  execution tasks не запускались, Phase result acceptance остаётся `pending`,
  Phase 1 заблокирована. Владелец добавил обязательный execution stop-rule: при
  подтверждённой невыполнимости плана немедленно прекратить выполнение,
  перевести workflow в `blocked`, зафиксировать blocker и предложить варианты;
  не импровизировать, не ослаблять acceptance criteria и не расширять scope.
  Следующий отдельный gate:
  `/architecture-start phase-0.5-model-driven-architecture-widget-v2-amendment`.
- 2026-08-01: явный owner invocation
  `/architecture-start phase-0.5-model-driven-architecture-widget-v2-amendment`
  остановлен до execution authorization и до Task 1. Обязательный gate требует
  буквального совпадения requested phase с `Current phase`, но persisted значения
  различаются: requested
  `phase-0.5-model-driven-architecture-widget-v2-amendment`, Current phase
  `phase-0.5-model-driven-architecture-widget`. Это inconsistency workflow-модели
  amendment overlay, а не доказательство невыполнимости schema/runtime плана.
  Согласно owner stop-rule workflow переведён из `approved` в `blocked`, v2
  execution authorization оставлена `pending`, никаких implementation/evidence
  tasks не запускалось. Варианты для владельца: A (recommended) — planning/context-
  only correction, делающая amendment текущей фазой и active plan при сохранении
  parent plan как истории, затем повторный explicit start; B — отдельно изменить
  command contract, чтобы он признавал approved amendment overlay при parent
  Current phase; C — отказаться от исполнения amendment и оставить Task 4
  rejected/pending. Запускать parent phase для обхода equality gate запрещено.
- 2026-08-01: владелец выбрал вариант A. Выполнена только planning/context
  correction: `Current phase` переведена на
  `phase-0.5-model-driven-architecture-widget-v2-amendment`, `Active plan` — на
  reviewed amendment SHA-256
  `B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536`, Stage
  возвращён в `approved`. Original Phase 0.5 plan/hash сохранены отдельными
  parent fields и paused после rejected Task 4; successful amendment F5 должен
  вернуть workflow только к original Task 5. Предыдущий failed start не стал
  authorization: `V2 amendment execution authorization` остаётся `pending`, ни
  один implementation task не запускался. Требуется новый явный
   `/architecture-start phase-0.5-model-driven-architecture-widget-v2-amendment`.

- 2026-08-05: Phase 1 Task 10 dossier/widget implementation completed; Final
  Verification Wave F1-F4 was not started and the plan checkbox was not changed.
  - Canonical `maps/architecture-model.json` validates with `model-v2` (33
    assertions, 21 mutations) and `runtime-v2` (47 assertions, 20 mutations).
    It records `ProjectSession` as current owner of lifecycle `ST-001`, `ST-002`,
    `ST-004`, and `ST-005`; legacy interfaces are aliases/forwarders over the
    same owner, while `CalculationContext`, Climate, Construction, Thermal,
    Hydraulics, Results projection, persistence schema, formulas, UI, packages,
    installers, and release artifacts remain unchanged by this phase.
  - All six filtered views now carry the Phase 1 lifecycle overlay. Coverage,
    persistence compatibility, target invariants, and state inventory retain
    prior history and record accepted v1.0/v1.1 fixture behavior, current
    partial restore semantics, and deferred transactional restore. `ST-021` is
    corrected factually: `CalculationContext.Reset()` does not assign
    `ThermalInputs`; no runtime behavior changed.
  - `widget/generate-widget.mjs` now labels the ProjectSession lifecycle shell
    implemented while leaving module slices target-only. The canonical
    `architecture-widget.html` was regenerated only through the generator.
    Generator `--check` passed 14/14 deterministic/reference checks with
    SHA-256 `9C5188BAC257D9CAC51045C0D2186D03A4A2E6B92AFFBF0519B3B3737BBCED9F`.
  - Final Task 10 local gate: `dotnet build src/SnowMeltingCalculator.csproj -c
    Debug` exit 0, `0 warnings, 0 errors`. Parent QA results are recorded without
    re-running final waves: owner/guard 40 passed; flow 83 passed, 1 skipped;
    persistence 18 passed; DI 8 passed; full Release 1565 passed, 1 skipped.
  - Evidence: `docs/architecture-migration/evidence/phase-1-project-session-shell/dossier-update.md`.
  - Next: independent Task 10 QA by the parent orchestrator; only after its
    acceptance may it dispatch F1-F4.
