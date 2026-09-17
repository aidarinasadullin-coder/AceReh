REVIEW_ID: R-2026-09-18-01
SUBJECT: план docs/plans/2026-09-18-deduplication-plan.md — дедупликация кода, три волны (волна 1a: A2–A6 behavior-preserving экстракции)
REVIEWER: независимый read-only subagent, чистый контекст (general-purpose, атакующий режим, вектора A1–A8)
DATE: 2026-09-18
RECEIPT: настоящий файл (полный отчёт ревьюера сохранён ниже)
VERDICT: APPROVE-WITH-EDITS
REASON: копии в волне 1a подтверждены построчно идентичными (A1 — расхождений
        нет), ссылки плана на строки/методы точны (A8), state ownership и
        wire .smc не затрагиваются (A4/архитектура), allowlist'ов
        ArchitectureRulesTests правки не требуют (A5). Две находки P1 —
        пробелы декомпозиции (A5-плана: PickWorst возвращает только результат
        без контура; A2-плана: не зафиксирована catch-семантика helper'а
        записи), обе закрываются правкой формулировок плана до имплементации.

## Находки и статусы

| # | Приоритет | Находка | Статус |
|---|---|---|---|
| 1 | P1 | A5-плана: `PickWorst` возвращает только `CircuitResultProjectData?` (HydraulicsSectionBuilder.cs:121), а `BuildReferenceCircuit` использует и контур, и результат (:190, :204, :208-210). Декомпозиция «вызывает PickWorst» невыполнима как сформулирована | исправлено в плане §1a: сигнатура `PickWorst` → возврат пары `(CircuitProjectData? Circuit, CircuitResultProjectData? Result)`; вызов в `BuildModeComparison` (:72-73) адаптируется на `.Result` |
| 2 | P1 | A2-плана: не зафиксирована catch-семантика `WriteProjectAtomicallyAsync`; helper, глотающий исключение, сломает `OperationResult.Failure(ex.Message, ex)` — пин `ProjectFileServiceResultTests.cs:41-53` | исправлено в плане §1a: helper чистит tmp в catch и ретроутит; сериализация остаётся в try обёрток; catch-ветки обёрток дословно (включая проглатывание OperationCanceledException); Debug.WriteLine дословно |
| 3 | P2 | A3-плана неполон: третий байт-в-байт клон маппера в `GetAllAsync` (SearchHistoryRepository.cs:91-100) остаётся за скоупом | исправлено в плане §1a: `GetAllAsync` включён в общий маппер `ReadEntry` |
| 4 | P2 | Посылка плана об «незакоммиченном дереве отчётов» устарела: дерево чистое (HEAD 1f7c510), 1b больше ничем не заблокирована | исправлено в плане §0/§1b: состояние дерева обновлено; V1 владельца переформулирован (1a сейчас, 1b/2 — следующими циклами без смешивания диффов) |
| 5 | P2 | A3-плана: `DateTime.Parse` без культуры во всех трёх копиях — сохранить как есть (запись через ToString("o"), «улучшайзинг» на InvariantCulture меняет поведение); из двух вариантов хука предпочесть `Func<SqliteCommand>`; `EnsureInitializedAsync` остаётся в публичных методах (гранулярность семафора) | исправлено в плане §1a (все три ограничения зафиксированы) |
| 6 | P3 | Волна 1b, гигиена: `SectionTitle` записан без отступа (PdfExportService.cs:811) — шумная дифф-строка при экстракции | отложено в план §1b: выровнять отступ до правки или отдельным no-op коммитом |

## Подтверждено ревьюером (выборка с путями:строками)

- A1 (тихая смена поведения): ProjectFileService Save/SaveResult — идентичны
  построчно кроме возврата; SearchHistoryRepository GetById/GetByCityId —
  различие только WHERE; DateTime.Parse без культуры одинаков в трёх копиях
  (:97, :126, :155); CityMatchToHighlightConverter Convert/CreateInlines —
  одинаковый split и кисти, различие только guard; PickWorst и инлайн-цикл
  HydraulicsSectionBuilder идентичны включая тю-брейк; PdfExportService
  SectionTitle/SectionTitleSection идентичны кроме точки вставки.
- A2: контракты пинятся тестами Atomicity (false/.bak/tmp удалён) и Result
  (Failure с Exception); ct уходит в WriteAllTextAsync в обеих копиях.
- A3 (литералы): имена билдеров в tests/ не пинятся; единственный
  `WhereUsed = "Climate"` (CalculationReportMarkdownRendererTests.cs:393) —
  собственный объект теста; для волны 2 дифф markdown-рендера остаётся валидной
  защитой.
- A4: все файлы волны 1a чистые в git; смешивания диффов нет.
- A5 (пины структуры): ArchitectureRulesTests/DiRegistrationTests приватные
  экстракции не задевают; CityMatchToHighlightConverterTests пинит публичный
  CreateInlines — сохраняется.
- A6: `new MaterialSnapshot` в src/ — ровно три (фабрика FromMaterial и два
  клона из плана); обратные маппинги snapshot→Material — другой тип, вне скоупа
  корректно.
- A7: ConfigureAwait/ручных контекстов в затронутых файлах нет.
- A8: все ссылки плана файл:строка сверены — точны; show-me существует рядом.

---
