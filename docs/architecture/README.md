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
