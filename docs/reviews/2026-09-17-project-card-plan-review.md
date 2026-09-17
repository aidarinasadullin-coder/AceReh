REVIEW_ID: R-2026-09-17-01
SUBJECT: план «возврат редактирования Номер проекта / Объект — карточка на
вкладке Климат» (docs/plans/2026-09-17-project-card-climate-plan.md) + show-me
(docs/plans/show-me-project-card.html): pass-through ClimateViewModel на
ProjectSession, живая шапка через подписку ResultsViewModel, ADR-015/WI-5
REVIEWER: независимый read-only subagent, чистый контекст (Explore),
вектора A1–A8, 8/8 пройдены с доказательствами
DATE: 2026-09-17
RECEIPT: настоящий файл (вердикт и находки ревьюера сохранены ниже)
VERDICT: APPROVE-WITH-EDITS
REASON: P0 нет по существу замысла — владелец канона не меняется
(ProjectSession), .smc не расширяется, R1–R6 не задеты, WI-5 санкционируется
корректно. Но план пропускал цензус-тест подписок сессии
(ReactiveSubscriptionLifecycleTests: ожидание 1 → фактически 3) — красный
набор на handover без правки; show-me называл неверный writer при загрузке
.smc (урок №27); V2 имел незадокументированное следствие SyncDirty.
Шесть находок приняты автором, правки внесены в §§1–6 плана и show-me
до имплементации.

## Находки и статусы

| # | Приоритет | Находка | Статус |
|---|---|---|---|
| 1 | P0 | Цензус-тест ReactiveSubscriptionLifecycleTests (~:101) пинит Session.PropertyChanged на 1 production-обработчик (MainViewModel); план добавляет +2 подписки → тест падает, handover красный; комментарий ~:87-91 «project card is out of scope in v1» протухнет; противоречие с ADR-014 п.9 | исправлено в плане: §2 — четвёртая поправка цензуса 1 → 3 + амендант комментария; §4 — ADR-015 явно supersede'ит формулировку ADR-014 п.9; в объём имплементации добавлена правка expected-строки |
| 2 | P1 | show-me: «ProjectLoadOrchestrator пишет поля в сессию» — неверно: identity при загрузке пишут pass-through-сеттеры ResultsViewModel (BeginProjectRestore/LoadProjectDataAsync, ResultsViewModel.cs:1632-1633); в ProjectLoadOrchestrator упоминаний полей нет; в плане writer не назван (нарушение урока №27) | исправлено: план §6 фиксирует фактический writer; show-me регенерирован |
| 3 | P1, DESIGN | V2 неполон: UndoRedoService.SyncDirty (UndoRedoService.cs:545-554) после undo/redo на точке чистоты вызывает MarkClean() — dirty от правки identity гасится откатом несвязанной правки (сохранить → ввести номер → править климат → Ctrl+Z) | зафиксировано как осознанное следствие V2 (решение владельца «undo как есть»): план §6 + ADR-015; изменение SyncDirty вне объёма |
| 4 | P2 | §1 внутренне противоречив: нет equality-выхода (при этом тест §5 требует «повторное значение — без события»); двойная нотификация (сеттерный OnPropertyChanged + репост); guard «_isResetting» — поля в ClimateViewModel не существует | исправлено: §1 — equality-выход, один канал нотификации (репост по подписке), guard IsLoadProjectInProgress |
| 5 | P3 | Public ctor ClimateViewModel не валидирует сессию (projectSession?.ClimateState!) — добавляемая подписка в ctor даст NRE при null; callers с null сейчас отсутствуют, риск латентный | исправлено: §1 — валидация в public ctor |
| 6 | P3 | §3: копирование UpdateSourceTrigger=PropertyChanged дало бы эхо-перезапись TextBox при каждом символе; «Tag/ToolTip» противоречило абзацу ниже; behaviors названы без класса | исправлено: §3 — LostFocus, ToolTip + FormCard.Hint, TextBoxBehavior.* |

## Подтверждено ревьюером (выборка с путями:строками)

- A1: WI-5 regex `\.MarkDirty\()` (ArchitectureRulesTests.cs:171) матчнет
  вызов ClimateViewModel — правка allowlist в §4 обязательна и запланирована;
  R3-сканер (:224) чист (identity не в vmSlices-паттернах).
- A2: identity уже в wire — ProjectPersistenceMapper.cs:27-28,
  ProjectSnapshot.cs:31, ключи projectNumber/projectObject в
  tests/Fixtures/v1-sample.smc; план новых полей не вводит.
- A3: один канал владения (оба pass-through без локальных копий, DI
  singleton — ServiceCollectionExtensions.cs:60,261).
- A4: ProjectSession.SetProperty (ProjectSession.cs:159-169) публикует
  PropertyChanged безусловно, в т.ч. под guard'ом загрузки.
- A5: все сайты конструирования ClimateViewModel перечислены; legacy-тесты
  свойства не трогают; null в public ctor не передаёт никто.
- A6: дневник подписан только на 4 Changed-слайса (UndoRedoService.cs:74-77) —
  identity в него не попадает; побочный эффект SyncDirty — находка №3.
- A7: FormCard.*/TextBox.Default/AutomationId-конвенция подтверждены на
  ClimateView.xaml; MaxLength на программную загрузку не влияет.
- A8: цепочки потребителей проверены до конца (ПЗ — ProjectSectionBuilder,
  Results — ResultsPdfDataBuilder.cs:46, Excel — ExcelExportService.cs:75-77,
  имя файла — ResultsViewModel.cs:719) — правок потребителей не требуется.

---
Сверка show-me с планом (урок №27): артефакт регенерирован из плана после
внесения находок №1–3; расхождений не осталось.

## Полный отчёт ревьюера (сохранён дословно)

VERDICT: APPROVE-WITH-EDITS

FINDINGS:

1. P0 — план не правит цензус-тест ReactiveSubscriptionLifecycleTests:
красный набор на handover. Тест пинит Session.PropertyChanged ровно на 1
production-обработчик + 1 probe: tests/SnowMeltingCalculator.Tests/Services/
Project/ReactiveSubscriptionLifecycleTests.cs:101. Граф CreateProductionShaped
строит обе VM по production-схеме с сессией (:460-464 public ctor
ClimateViewModel с session; :568-583 ResultsViewModel с session) — план §1 и
§2 добавят +2 production-подписки → фактических 4 против ожидаемых 2.
Правка: четвёртая поправка цензуса «Session.PropertyChanged 1 → 3» +
комментарий-амендант + явная фраза в ADR-015 про supersede ADR-014 п.9.

2. P1 — show-me против плана и кода (урок №27): неверный writer при загрузке
.smc. Артефакт утверждал «ProjectLoadOrchestrator пишет поля в сессию»; в
коде identity при загрузке пишет ResultsViewModel собственными
pass-through-сеттерами (ResultsViewModel.cs:1632-1633 внутри
BeginProjectRestore :1621); в ProjectLoadOrchestrator.cs совпадений нет.
Правка: план §6 фиксирует фактический путь; артефакт регенерировать.

3. P1, DESIGN — V2 неполон: unrelated undo гасит dirty-флаг, поставленный
карточкой. UndoRedoService.cs:545-554 (SyncDirty): после undo/redo позиция ==
точка чистоты → MarkClean(). Сценарий: сохранить → ввести номер (dirty,
дневника нет) → править климат (запись дневника) → Ctrl+Z → dirty исчезает,
хотя сессия отличается от файла на номер проекта, отменить правку нельзя.
Правка: записать следствие в ADR-015 как осознанное (минимум), либо решение
владельца об исключении identity-дельты из SyncDirty (DESIGN).

4. P2 — спецификация сеттера в §1 противоречива: нет equality-выхода
(ResultsViewModel.cs:70), двойная нотификация, «_isResetting» — поля не
существует (есть _isMirroringClimateState/_isLoadingProject). Правка:
equality-выход, один канал нотификации (репост), фактические guard'ы.

5. P3 — null-сессия в подписке ctor: public ctor не валидирует сессию
(ClimateViewModel.cs:226) — после добавления подписки NRE при null.
Callers с null отсутствуют, риск латентный. Правка: null-check.

6. P3 — UI-детали §3: копирование UpdateSourceTrigger=PropertyChanged даёт
эхо (ClimateView.xaml:103,125,137,149 — паттерн числовых полей); «Tag/ToolTip»
противоречило абзацу ниже; behaviors — attached-свойства TextBoxBehavior.
Правка: LostFocus, Tag убрать, TextBoxBehavior.*

По векторам чисто: A1 (WI-5-regex подтверждён, R3-сканер не матчит), A2
(wire .smc не расширяется), A3 (один канал владения, DI singleton), A4
(SetProperty публикует безусловно, identity пишется до Dispose lease), A5
(все сайты конструирования перечислены, legacy-тесты не трогают свойства),
A6 (дневник подписан только на 4 слайса — identity не попадает), A7 (стили
и конвенции подтверждены, MaxLength на загрузку не влияет), A8 (цепочки
потребителей проверены до конца — правок не требуется).

---

## Имплементация (пост-ревью диффа, 2026-09-17)

REVIEW_ID: R-2026-09-17-02 (в дополнение к план-ревью R-2026-09-17-01)
SUBJECT: дифф имплементации плана 2026-09-17-project-card-climate-plan.md
(ClimatVM pass-through, ResultsViewModel репост, карточка XAML, ADR-015,
WI-5/цензус/легаси-гард, 6 новых тестов)
REVIEWER: независимый read-only subagent, чистый контекст (Explore), вектора B1–B7
VERDICT: APPROVE
REASON: имплементация 1:1 с планом и находками №1–6; NRE-риска нет, wire
.smc и R1–R6 не тронуты, calculation-слой не затронут; независимый контроль
остался в WI-5 (allowlist) и цензусе (3 подписки).

| # | Приоритет | Находка | Статус |
|---|---|---|---|
| 1 | P3 | Null-валидация public ctor недостижима: chain-вызов упадёт раньше (guard internal ctor бросает ArgumentNullException по climateState) | принято как есть: добавлен комментарий к chain в ClimateViewModel (диагностика прямого вызова), поведение безопасно |
| 2 | P3 | Hint-текст карточки «Опционально · попадают в шапку…» шире формулировки плана §3 | зафиксировано: формулировка оставлена (соответствует V3 «поля опциональные») |
| 3 | P3, процесс | В том же незакоммиченном дереве лежит предшествующая работа (темы 1–2: REHAU→РЕХАУ, поток секции PDF) | принято: handover размечен по фазам; при коммите не смешивать |

Подтверждено (выборка): equality-выход и один канал нотификации
(ClimateViewModel.cs:49-75 — сеттеры без OnPropertyChanged); internal-ctor
шов без подписки (NRE нет); XAML — RowDefinition/AutomationId/MaxLength/
LostFocus по плану, RestoreOnEscape конфликтует корректно (Esc → LostFocus →
equality-выход); цензус 3+1 подтверждён в production-shaped графе; тесты
проверяют заявленное (dirty, equality, репост, reset); SyncDirty не менялся
(следствие V2 задокументировано, не реализовано) — соответствует ADR-015.

Визуальная приёмка карточки: судья (скриншот вкладки после ввода
«2026-014» / «Парковка у ТЦ, Москва» с клавиатуры) — PASS: карточка над
климатической, стиль согласован, значения читаемы, шапка показывает
«2026-014 · Парковка у ТЦ, Москва» (живая шапка подтверждена end-to-end).

Версия: 1.6.0 → 1.7.0 (правило №20): csproj, INSTALL.md ×3, README.md ×1,
.iss (шапочный комментарий), CHANGELOG 1.7.0.
