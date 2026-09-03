# Архитектура — живой контур

Это единственное живое описание архитектуры приложения. Канонические
правила — шесть инвариантов в корневом `AGENTS.md`; их машинная проверка —
`tests/SnowMeltingCalculator.Tests/Architecture/ArchitectureRulesTests.cs`
(правила R1–R6). История миграции и её карты — замороженное досье
`docs/architecture-migration/` (только чтение).

## Диаграммы

| Файл | Что показывает |
|---|---|
| [overview.md](overview.md) | Слои, направление зависимостей, место Results |
| [project-session.md](project-session.md) | Aggregate root, срезы, санкционированные writers |
| [persistence-flow.md](persistence-flow.md) | Save/load `.smc`, снимок, restore guard |

## Правило поддержки

Меняешь то, что видно на диаграмме (слои, зависимости, срезы, поток
persistence), — обнови диаграмму и добавь строку в журнал ниже **в том же
коммите**. Не меняешь — запись не нужна.

## Журнал решений (ADR-light)

### ADR-001 — 2026-09-04 — Пост-миграционный архитектурный контур

Миграция завершена (фазы 1–11, owner-accepted). Постоянные правила — шесть
инвариантов в корневом `AGENTS.md`; контроль — архитектурные тесты R1–R6;
живое описание — диаграммы здесь. Виджет (16 МБ HTML) и машинная модель
(`architecture-model.json`) выведены из эксплуатации и перемещены в
`docs/architecture-migration/archive/` как provenance; досье миграции
заморожено. Почему: документация, поддерживаемая руками, расходится с
кодом; тесты связаны с кодом напрямую и не могут протухнуть.

### ADR-002 — 2026-09-04 — Исключение R4: Results-билдеры

`src/Services/Results/ResultsPdfDataBuilder.cs` и
`src/Services/Results/HydraulicSummaryBuilder.cs` зависят от read-model
записей `ViewModels.Results` — санкционировано владельцем в фазе 11
(записанная backlog-hygiene задача). Остальные зависимости Services →
ViewModels запрещены. Проверяется тестом R4 (reflection по типам + скан
`using` в `src/Services`).

### ADR-003 — 2026-09-04 — Списки санкционированных writers

Скан-тесты R2/R3/R5 переносят writer-inventory фазы 10 (8/8 PASS,
owner-accepted) как постоянные тесты; списки допустимых writers перенесены
дословно из принятого evidence (`evidence/phase-10-reactive-ownership-
multiplicity-closure/writer-inventory.mjs`). Изменение списка = изменение
правила: только через запись в этом журнале.

### ADR-004 — 2026-09-04 — Семантика УГВ при жизненном цикле и шаблонах

Партия правок по плану
`docs/plans/2026-09-04-gwl-template-results-batch.md` (ревью momus/oracle,
owner-approved D1–D6). Владение состоянием не меняется; фиксируются три
семантических решения:

1. **Новый расчёт и сброс перед загрузкой** используют заводской УГВ 2.0 м
   (`ConstructionDefaultStateInitializer.Apply(origin)`), а не УГВ
   предыдущего проекта. Точки входа `ConstructionViewModel.Initialize/Reset`
   сознательно сохраняют текущий УГВ (D6 «фактический»).
2. **Применение шаблона** (D2): шаблон задаёт состав и толщины слоёв; λА/λБ
   определяет УГВ проекта (`Construction.GroundwaterLevel` пересчитывает λ
   при присваивании). `template.DefaultGroundwaterLevel` с этого момента
   декоративен: в UI редактора шаблонов поле отсутствует (и не
   показывалось), новым шаблонам присваивается 2.0; свойство сохранено
   в модели ради совместимости templates.json и помечено как устаревшее
   (решение владельца «спрятать», 2026-09-04).
3. **Ручные λ и persistence** (D5): восстановление проекта больше не
   сбрасывает `IsLambdaOverridden` — флаг round-trip сохраняется. Это
   отменяет поведение P0-7 («restore intentionally resets override flags»),
   закреплённое в characterization-тестах: `ProjectRoundTrip_*` в
   `ResultsViewModelOpenProjectTests` и
   `ProjectLifecycleFlowCharacterizationTests` обновлены в этой же партии.
   Комбобокс УГВ — двухпозиционный; программные присваивания опции не
   мутируют скаляр УГВ (guard `_isResetting/_isRefreshing/_isSyncing`).
