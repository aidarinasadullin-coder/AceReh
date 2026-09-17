# План: дедупликация кода — три волны (локальные экстракции, семейство `Meta(...)`, тестовые фикстуры)

> Дата: 2026-09-18. Статус: **концепция — ожидает сигнала владельца**;
> имплементация не начата. Независимое атакующее ревью плана — перед
> волной 2 (широкая поверхность); для волны 1 ревью выполняется вместе с
> общим ревью плана.
> Основание: скан jscpd 3.5.10 (порог 70 токенов ≈ 6 строк) + ручная
> сверка каждого кластера по коду 2026-09-18: `src/` — 19 клонов,
> 0.71 % дублированных строк (284 файла); `tests/` — 135 клонов, 6.44 %
> (166 файлов).
> show-me: [show-me-deduplication.html](show-me-deduplication.html).

## 0. Суть

Архитектурных дубликатов нет: каноническое состояние имеет единственного
владельца на значение, срезы сессии нигде не дублируются, wire-формат
`.smc` не затрагивается. Найденное — 6 локальных копипастов в `src/`
(≈100 строк), одно систематическое семейство фабрик `Meta(...)` в 7
билдерах отчёта (≈130 строк избыточности) и разросшиеся общие аранжи в
интеграционных тестах гидравлики и отчётных тестах (≈900 строк
перекрытий).

Принцип всех волн: **behavior-preserving экстракция** — ни одного
изменения видимого поведения, значений метаданных отчёта, порядка
атомарной записи файлов и ожиданий существующих тестов. Ожидания тестов
не правятся (кроме новой волны 3, которая сама меняет только тесты).

### Вопросы владельцу (зафиксируются после ответа)

| # | Вопрос | Варианты | Рекомендация |
|---|---|---|---|
| V1 | Порядок старта | (а) волна 1a сразу, 1b после хендовера текущего дерева; (б) всё после хендовера; (в) одной волной | (а): волна 1a не пересекается с незакоммиченными файлами отчётов |
| V2 | Размещение общей фабрики `Meta` | (а) static-класс `ReportMetadataFactory` в `Builders/`; (б) общий базовый класс билдеров с protected-методами | (а): билдеры объединены только интерфейсом `IReportSectionBuilder<T>`, базовый класс ради двух хелперов — лишняя связность |
| V3 | Копирование `MaterialSnapshot` | (а) `MaterialSnapshot.Clone()` на модели; (б) статический маппер в сервисном слое | (а): модель знает свои поля, второй вызыватель появится — не придётся дублировать снова |
| V4 | Волна 3 (тесты) | (а) делать в этом цикле; (б) отложить до ближайшего изменения `ProjectData`/отчётных тестов | (б): дубли тестов не гниют быстро; вынос аранжей characterization-тестов снижает их локальную читаемость |

---

## 1. Волна 1 — локальные экстракции в `src/` (behavior-preserving)

### 1a. Не пересекается с незакоммиченным деревом

| # | Файлы | Правка |
|---|---|---|
| A2 | `src/Services/Project/ProjectFileService.cs:45-155` | Общий приватный `WriteProjectAtomicallyAsync(filePath, json, ct)` (расширение → tmp → bak → move; очистка tmp в catch) + `DeleteStaleTemp(filePath)`. `SaveProjectAsync` и `SaveProjectResultAsync` остаются публичными обёртками (bool / `OperationResult`) над ним. Порядок и сообщения `Debug.WriteLine` — без изменений |
| A3 | `src/Repositories/SearchHistoryRepository.cs:110-161` | Приватный `QueryEntryAsync(string whereColumn)` (или `Func<SqliteCommand>`-хук): `GetByIdAsync`/`GetByCityIdAsync` сводятся к одному SELECT + общий маппер `ReadEntry(reader)` |
| A4 | `src/Converters/CityMatchToHighlightConverter.cs:28-140` | `Convert` делегирует в существующий статический `CreateInlines` (разбор `**` остаётся в одном месте); `Convert` — null-guard + вызов |
| A5 | `src/Services/Reports/Calculation/Builders/HydraulicsSectionBuilder.cs:96-189` | `BuildReferenceCircuit` после выбора худшего коллектора вызывает `PickWorst(worstCollector, mode)` — правило «худшего контура» (max `DpGesamt`, при ничьей минимальный номер) живёт в одном месте |
| A6 | `src/Models/Construction/MaterialSnapshot.cs`, `src/Services/Construction/ConstructionService.cs:329-352`, `src/ViewModels/Construction/TemplateEditorViewModel.cs:484-498` | `MaterialSnapshot.Clone()` (решение V3); оба места копирования 9 полей переключаются на него. Цикл «слой из шаблона + fallback на снапшот» (`ConstructionService.cs:132-159`) — локальный `ApplyTemplateLayers(...)` для над/под трубой |

### 1b. После хендовера текущего цикла отчётов (файл уже modified в дереве)

| # | Файлы | Правка |
|---|---|---|
| A1 | `src/Services/Results/PdfExportService.cs:788-832` | `SectionTitleSection` (instance, `Section`) и `SectionTitle` (static, `Cell`) получают общий приватный хелпер построения таблицы-заголовка (красная метка 7pt + текст 10pt bold, INK); различие только в точке вставки (`host.AddTable()` vs `host.Elements.AddTable()`) — уходит в параметр-делегат |

Объём волны 1: ~100 дублированных строк → ноль; 7 файлов, в каждом
изменение локальное.

## 2. Волна 2 — семейство фабрик `Meta(...)` билдеров отчёта

**Поверхность: 7 файлов** (`ConstructionSectionBuilder`, `ProjectSectionBuilder`,
`ClimateSectionBuilder`, `HydraulicsSectionBuilder`,
`HydraulicsReportMetadataBuilder`, `EquipmentSectionBuilder`,
`ThermalSectionBuilder`) — материальное изменение по AGENTS.md →
независимое атакующее ревью плана до имплементации + чек в `docs/reviews/`.

| Файлы | Правка |
|---|---|
| `src/Services/Reports/Calculation/Builders/ReportMetadataFactory.cs` (новый, static, internal) | Единая фабрика `ReportParameterMetadata`: overloads для `ReportValue<double>` / `ReportValue<string>` (+ optional `whereUsed`, optional explicit `formula`) и полный 9-аргументный вариант; параметр `formulaSource` — имя билдера (решение V2). Внутри — текущая семантика: `Formula = value.Formula ?? value.FormulaStatus`, `FormulaSource = formula == null ? "" : formulaSource`, `WhereCalculated = value.SourceDetail` |
| 7 билдеров | Локальные `Meta(...)`/`Formula(...)` сжимаются до 1-строчных делегатов в фабрику с константой имени билдера; diff чисто механический |

**Пины сохранности:** значения `FormulaSource`/`WhereUsed` попадают в
метаданные отчёта («Где рассчитано / Где используется»). Перед правкой —
grep-инвентаризация литералов (по одному на билдер), после — полный
прогон `CalculationReport*Tests` + сравнение сгенерированного отчёта
до/после (текстовый diff markdown-рендера на существующей фикстуре).

## 3. Волна 3 — тестовые фикстуры (решение V4)

| Файлы | Правка |
|---|---|
| `tests/.../IntegrationTests/Hydraulics/` — новый `HydraulicsIntegrationFixture.cs` | Общий аранж интеграционных тестов гидравлики (создание сессии/расчёт, источник — `ThermalToHydraulicsIntegrationTests.cs:34-258`): его копии в `ClimateToHydraulicsIntegrationTests` (226L), `PipeSpacingSynchronizationTests` (210L), `DoubleCalculationPreventionTests` (209L), `GlycolAutoRecalculationTests` (137L суммарно), `CalculationContextWriterAuthorityTests` (79L) переиспользуют fixture; assertions остаются локальными |
| `tests/.../Services/Reports/Calculation/` — общий builder фикстуры отчёта | Дубликат `CalculationReportWarningTests.cs:260-394` в `CalculationReportInventoryTests.cs:146-280` (135L) и `CalculationReportDataBuilderTests.cs:440-554` (115L) — один builder; фабрика PDF-документа `PdfExportServiceTests.cs:203-273` ≈ `CalculationReportPdfRendererTests.cs:606-676` (71L) — общий fixture |

Объём: ≈900 строк перекрытий → единственные копии в fixture-классах.
Тесты не ослабляются: ни один assert не переносится/не удаляется.

## 4. Совместимость и риски

- **State ownership без изменений**: ни одна волна не добавляет writers,
  не трогает слайсы `ProjectSession`, `ArchitectureRulesTests` остаются
  зелёными без правок allowlist'ов; `docs/architecture/` и ADR-журнал —
  без изменений (запись «state ownership без изменений» в handover).
- **Wire `.smc`**: волна 1a (A2) сохраняет последовательность
  tmp → bak → move и JSON-опции байт-в-байт; риск регрессии атомарности
  закрывается существующими characterization-тестами жизненного цикла
  проекта.
- **Отчётные метаданные** (волна 2): риск — опечатка в литерале
  `formulaSource` при переносе; закрывается grep-инвентаризацией до/после
  и полным прогоном отчётных тестов.
- **Незакоммиченное дерево**: волна 1a и волна 2 не пересекаются с
  файлами текущего цикла отчётов; A1 (1b) — только после его хендовера
  (V1). Диффы волн не смешиваются: каждая волна — отдельный цикл
  handover.
- **Характеристика, а не рефакторинг тестов**: волна 3 меняет только
  аранжи; если при выносе обнаружится скрытое расхождение копий (копии
  давно разошлись в деталях) — расхождение фиксируется как находка
  ревью, поведение оригинала не «чинится» молча.

## 5. Процесс

1. Ответы владельца на V1–V4 → фиксация решений в §0.
2. Независимое атакующее ревью плана (subagent; вектора: «тихое
   изменение поведения при экстракции», «потеря под-clean catch-веток»,
   «расхождение `FormulaSource`-литералов», «смешивание диффов с
   текущим деревом», «ослабление тестов при выносе аранжей») → чек по
   шаблону `docs/reviews/2026-09-18-deduplication-plan-review.md`.
3. Волна 1a → полный зелёный `dotnet test` → handover некоммиченным
   деревом. Волна 1b — в следующем цикле после хендовера отчётов.
4. Волна 2 → grep-инвентаризация → правка → дифф markdown-отчёта
   до/после → полный `dotnet test`.
5. Волна 3 — по решению владельца (V4).
6. show-me regenerated from this plan (урок №27); сверка артефакта с
   планом — шаг ревью.
