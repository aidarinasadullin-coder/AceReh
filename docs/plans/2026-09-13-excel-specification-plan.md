# План: экспорт спецификации закупки в Excel (.xlsx) из «Результатов»

> Дата: 2026-09-13. Статус: **принято владельцем 2026-09-13** — реализация
> выполнена, smoke «Отчёт PDF ▾ → Спецификация XLSX» и сверка show-me
> выполнены владельцем, закоммичено по команде владельца (полный прогон:
> 2275 passed / 0 failed / 1 skipped — предсуществующий условный).
> Owner-signal «делай» 2026-09-13: ОВ-5 — включать РЗС 25×2,3 с пустым
> артикулом и примечанием, норма 2 шт/контур подтверждена.
> Основание: идея №3 из [docs/Идеи.md](../Идеи.md) — одобрена владельцем
> 2026-09-13 (обсуждение презентации для руководства).
> Ревью: независимый read-only subagent — **APPROVE-WITH-EDITS** (2 P1, 2 P2,
> 6 P3; все внесены в этот же дифф, см. §7):
> [docs/reviews/2026-09-13-excel-specification-plan-review.md](../reviews/2026-09-13-excel-specification-plan-review.md).
> show-me: [show-me-excel-specification.html](show-me-excel-specification.html).

## 0. Суть

В `ResultsViewModel` уже стоит заглушка экспорта в Excel
(`ResultsViewModel.cs:784-796`, текст «Экспорт в Excel будет реализован в
следующей версии»), команда `ExportExcelCommand` не привязана ни к одному
элементу UI. Артикулы при этом есть в данных — `rehau_products.json`
(трубы: `article_number`; коллекторы HKV: `article_number` →
`Collector.ArticleNumber`), но не выводятся ни в PDF-отчёт, ни куда-либо ещё.

**Цель:** выгрузка `.xlsx` из шага «Результаты» с двумя листами:

- **«Спецификация»** — позиции закупки: труба (типоразмер, артикул, метраж),
  резьбозажимные соединения (РЗС) по типоразмерам контуров, коллекторы HKV
  (тип, артикул, количество по типам);
- **«Контуры»** — наладка по контурам: длины, площадь, мощность, расход,
  потери давления, обороты клапана, преднастройка.

Файл открывается в Excel/LibreOffice без зависимостей от установленного
Office. Стоимость/цены — вне скоупа: цен в `rehau_products.json` нет; если
понадобятся — отдельная фаза с решением об источнике прайса.

### Предлагаемые решения (утверждает владелец на приёмке show-me)

| # | Вопрос | Предложение |
|---|---|---|
| P1 | Excel-библиотека | **ClosedXML (MIT)** — пишет/читает .xlsx без установленного Office, зрелый API. EPPlus отклонён (лицензия Polyform Noncommercial не подходит коммерческому продукту); DocumentFormat.OpenXml — официально, но многословен; NPOI — Apache-2.0, API старее. Пакет добавляется в `src/SnowMeltingCalculator.csproj` (централизованного package management в репо нет) |
| P2 | Артикулы труб | **Привести `PipeType.StandardPipes` к канону `rehau_products.json`** (ОВ-1, вариант «а»): сейчас в `PipeType.cs:49-78` зашиты `12180501001/12180502001/12180503001`, в json — `1200170000` (17×2,0), `1200200000` (20×2,0), `1200250000` (25×2,3). Тогда `ThermalPipeSnapshot.Article` становится единым источником арта для спецификации — без json-резолва в рантайме. Вне снапшот-механики `.Article` никто не читает; из тестов строка встречается один раз как тестовая фикстура (не пин константы) |
| P3 | Точка входа UI | Пункт **«Спецификация XLSX»** в меню сплит-кнопки «Отчёт PDF ▾» (`MainWindow.xaml:238-290`) рядом с PDF-пунктами; команда — реализованная заглушка `ExportExcelCommand`. Переименование кнопки — ОВ-3 |
| P4 | Состав листа «Спецификация» | Секции: «Труба» (типоразмер, артикул, ед. «м», метраж), «Резьбозажимные соединения» (наименование, артикул, ед. «шт», количество), «Коллекторы HKV» (тип, артикул, ед. «шт», количество по типоразмерам), «Итого по проекту» (объект, контуров, общий метраж, суммарная мощность). РЗС — данные владельца 2026-09-13: 17×2 → `12506073002`, 20×2 → `12506173002` (оба × G3/4" евроконус); количество — **2 шт на каждый контур** (подача + обратка — «на каждом подключении к коллектору»), группировка по типоразмеру трубы контуров. РЗС для 25×2,3 — ОВ-5. Прочий крепёж — не включаем (доменных моделей нет) |
| P5 | Состав листа «Контуры» | По коллектору (номер, тип) — по контуру: №, длина петли, подача, площадь, мощность, расход, Δp общая (рабочая), обороты клапана, преднастройка (`ZuDrosseln`). Источник — канонические снимки: `HydraulicCircuitSnapshot` (`HydraulicsStateSnapshots.cs:114-137`) и `HydraulicCircuitResultSnapshot` (44-58); площадь — расчётная, `CircuitLength × PipeSpacingCm / 100` (та же формула, что в `HydraulicCircuitRowProjection.cs:18`); преднастройка — `OperatingResult.Throttling` (то же значение, что `ZuDrosseln` отчётной модели `CircuitPdfData`). Точный состав колонок — владелец на приёмке show-me |
| P6 | Запас метража | Не добавляем: в спецификацию идёт расчётный метраж `TotalPipeLength = Σ(CircuitLength + SupplyLength)` (`ResultsKpiPresenter.cs:84`). Коэффициент запаса — решение владельца (ОВ-4) |

### Открытые вопросы владельца

| # | Вопрос | Варианты |
|---|---|---|
| ОВ-1 | Канонический источник артикулов труб: `StandardPipes` (`12180501001…`) vs json (`1200170000…`) | (а) привести `StandardPipes` к json-артам — рекомендую; (б) резолвить арт в билдере спецификации по имени типоразмера из json; (в) оставить как есть (спецификация покажет `12180501001…`) |
| ОВ-2 | Показывать ли артикулы в PDF-отчёте «Результаты» | сейчас их нигде нет; идея №3 предлагает решить заодно (может быть отдельной мини-фазой после xlsx) |
| ОВ-3 | Имя сплит-кнопки: остаётся «Отчёт PDF ▾» или переименовать в «Экспорт ▾» | пункт xlsx в меню «Отчёт PDF ▾» делает имя неточным |
| ОВ-4 | Коэффициент запаса на трубу | нет (расчётный метраж как есть) / фикс. % / поле в UI. По умолчанию — нет |
| ОВ-5 | РЗС под трубу 25×2,3 | Розничный артикул не подтверждён (поиск 2026-09-13; паттерн 1250627… в рознице не находится). Фитинг существует: идёт в комплекте промышленных коллекторов IVKK (12488901001, отводы ¾", краны/вентили с евроконусом под 25×2,3). Варианты: (а) включать позицию с пустым артикулом + примечанием; (б) не включать при контурах из 25-й трубы; (в) владелец уточняет артикул у дистрибьютора (REHAU.Про / Хогарт / Русклимат) — вписать в json позже |

---

## 1. Ф1 — Данные спецификации (read-model)

Новый read-model и билдер **без зависимости от ViewModels** (R4): билдер
читает канонические снимки `ProjectSession` и каталог коллекторов, мутаций
не делает — R2/R5 не задеваются, санкционированные writers не меняются.

| Файл | Правка |
|---|---|
| `src/Services/Results/ResultsSpecificationData.cs` (новый) | `SpecificationRow { Section, Name, Article, Unit, Quantity }`; `CircuitRow { CollectorNumber, CollectorType, CircuitNumber, LoopLength_m, SupplyLength_m, Area_m2, Power_W, FlowRate_lh, PressureLoss_Pa, ValveTurns, ZuDrosseln }`; `ResultsSpecificationData { ProjectName, Rows, CircuitRows, TotalCircuits, TotalPipeLength_m, TotalPower_kW }`. Числа — `double`/`int` без строкового форматирования (форматирует сервис) |
| `src/Services/Results/ResultsSpecificationDataBuilder.cs` (новый) | Ctor-инжект `ICollectorRepository` (async-каталог — поэтому сборка асинхронная) и json-каталога фитингов. `Task<ResultsSpecificationData> BuildAsync(IProjectSession session, CancellationToken)`. Труба: `ThermalState.Snapshot.Inputs.Pipe` → Name/Article (P2). РЗС: контуры группируются по типоразмеру трубы, количество = 2 × число контуров типоразмера (P4 — подача + обратка); артикул — резолв из секции `fittings` json по типоразмеру. Коллекторы: `HydraulicsState.Snapshot.Collectors` (`HydraulicCollectorSnapshot`, `HydraulicsStateSnapshots.cs:139-145`) → группировка по типу; артикул — резолв через инжектированный `ICollectorRepository` (по количеству контуров, та же логика, что `SelectCollectorAsync`). Контуры: `HydraulicCircuitSnapshot` + `OperatingResult`; площадь — формула P5. Отсутствие арта → пустая строка (не блокирует экспорт) |
| `src/Models/Thermal/PipeType.cs:49-78` | (ОВ-1а) три константы `Article` → json-канон `1200170000/1200200000/1200250000` |
| `data/rehau_products.json` | Секция `fittings` пополняется записями РЗС по аналогии с `pipes` (id, name, full_name, article_number, pipe_size, notes): 17×2 → `12506073002`, 20×2 → `12506173002` (данные владельца 2026-09-13); 25×2,3 — по решению ОВ-5 (запись с пустым артикулом + примечание или отсутствие записи) |
| `src/Configuration/ServiceCollectionExtensions.cs` | `AddResultsModule` (191-259): `AddSingleton<ResultsSpecificationDataBuilder>()` (DI сам подставит `ICollectorRepository`, зарегистрированный в `AddHydraulicsModule`, строка 134) и `AddSingleton<IResultsExcelExportService, ExcelExportService>()` (см. Ф2) |

VM не получает ни билдер, ни репозиторий — только сервис экспорта (Ф3).

## 2. Ф2 — Сервис экспорта

| Файл | Правка |
|---|---|
| `src/Services/Results/IResultsExcelExportService.cs` (новый) | `Task<bool> ExportSpecificationToXlsxAsync(string filePath, ResultsSpecificationData data, CancellationToken cancellationToken = default);` — паттерн `IPdfExportService` (`IPdfExportService.cs:3-9`): путь приходит снаружи, наружу — `bool` |
| `src/Services/Results/ExcelExportService.cs` (новый) | ClosedXML. Лист «Спецификация»: шапка-плашка (Brand.Red `#E50040`, белый текст) «РЕХАУ Калькулятор снеготаяния — спецификация закупки», затем секции P4. Лист «Контуры»: шапка-плашка (Brand.Teal.Deep `#2F776D`), таблица P5. **Числовые значения — числовые ячейки** (не строки с запятой): Excel сам рисует разделитель по своей локали, ячейки остаются суммируемыми; форматы столбцов — «0.00» / «#,##0» (код формата Excel локаленезависим, отрисовка — по локали пользователя). Текстовые подписи — канон `AppCulture.Culture` (`src/Core/AppCulture.cs:21-27`). Границы — серый `#E4E4E4`, зебра строк — светлый тинт (Brand.Gray.100 / Brand.Teal.Pale) |
| `src/Configuration/ServiceCollectionExtensions.cs` | `AddSingleton<IResultsExcelExportService, ExcelExportService>()` |

Ошибки — по паттерну `PdfExportService` (`PdfExportService.cs:42-72`):
try/catch вокруг записи, `false` наружу; человекочитаемая причина — в
`StatusMessage` VM (не глотаем молча — урок о silent failure).

## 3. Ф3 — UI

| Файл | Правка |
|---|---|
| `src/ViewModels/Results/ResultsViewModel.cs` | Новый ctor-параметр `IResultsExcelExportService` (рядом с `IPdfExportService`, ctor 513-530); ripple по тестовым местам конструирования — см. §6. Заглушка `ExportExcel` (784-796) → реализация по образцу `ExportPdf` (651-697): guard `IsDataReady` (1476); `IDialogService.ShowSaveFileDialog` (`IDialogService.cs:23-27`, filter «Книга Excel (*.xlsx)\|*.xlsx», имя по умолчанию «Спецификация {объект}.xlsx»); `await _excelExportService.ExportSpecificationToXlsxAsync(path, data, …)`; `StatusMessage` «Спецификация сохранена: {путь}» / «Ошибка экспорта: {причина}» |
| `src/MainWindow.xaml` | ContextMenu сплит-кнопки (238-290): новый пункт «Спецификация XLSX» → `ResultsViewModel.ExportExcelCommand`, `AutomationId="ReportExportSpecificationXlsx"` (в ряду `ReportExportPreview/Print/SavePdf/…`). Гейт данных живёт на самой сплит-кнопке (`IsEnabled="{Binding ResultsViewModel.IsDataReady}"`, строка 246) — у пунктов ContextMenu пер-пунктовых `IsEnabled`-гейтов нет, новый пункт добавляется без собственного |

## 4. Ф4 — Тесты

| Тест | Проверяет |
|---|---|
| `tests/…/Services/Results/ResultsSpecificationDataBuilderTests.cs` | Сборка из фикстурных снимков: труба → 1 позиция с артом; РЗС → 2 × число контуров по типоразмеру (смешанные 17/20 → две позиции); 2 коллектора разных типов → 2 позиции с количествами; контуры → строки с длинами/ValveTurns/ZuDrosseln и площадью по формуле P5; пустой арт → пустая строка, не исключение |
| `tests/…/Services/Results/ExcelExportServiceTests.cs` | Smoke по образцу `PdfExportServiceTests`: файл в `TestContext.CurrentContext.WorkDirectory` (Guid-имя, удаление в `finally`); magic-header `PK\x03\x04` (zip); обратное открытие ClosedXML — имена листов, заголовки секций, числовые ячейки как `double` (суммируемость), состав колонок листа «Контуры»; невалидный путь → `false` без исключения наружу |
| `ArchitectureRulesTests` | Без правок: билдер читает снимки, не VM (R4); writers не расширяются (R2); Results не пишет срезы (R5) — набор обязан оставаться зелёным |

## 5. Ф5 — Версия и документация (правило №20)

- Версия **1.4.0 → 1.5.0** (фича): **9 мест** по чек-листу урока №20 —
  csproj (`Version`, 1); .iss (`#define MyAppVersion`, `OutputBaseFilename`,
  шапочный комментарий — 3); INSTALL.md (имя сетапа ×2 + подвал — 3);
  README.md (подвал — 1); CHANGELOG.md (секция «Спецификация XLSX» — 1).
  Пока `VersionSyncTests` из плана процессных гейтов не в мастере — ручной
  grep-чек по всем 9 местам.
- `docs/report-spec-v2.md` §7.2: строка про формат xlsx (бренд-применение —
  плашки/цвета листов по брендбуку).
- `docs/manual/README.html`: новый пункт меню «Спецификация XLSX» в раздел
  про строку состояния/меню (перегенерация self-contained HTML; smoke
  срабатывания пункта — за владельцем, урок №23).

## 6. Порядок работ и handover

Ф1 → Ф2 → Ф3 → Ф4 → Ф5. Объём: 4 новых файла (модель, билдер, интерфейс,
сервис), правки `PipeType`/VM/меню/DI, 2 новых тест-файла, версия и доки.
`dotnet test` зелёный на каждом шаге; handover — buildable дерево с зелёным
прогоном, коммиты по явной команде владельца.

Ripple Ф3: новый ctor-параметр `ResultsViewModel` требует обновления тестовых
мест конструирования (по факту ревью — ~10 мест: `ResultsViewModelTestHelpers.cs:80`,
`ResetOrchestrationTests.cs:313`, `ResultsViewModelOpenProjectTests.cs:1868`,
`DialogServiceThreadAffinityTests.cs:146`, `ClimateThermalInvalidationRegressionTests.cs:419`,
`ProjectLifecycleFlowCharacterizationTests.cs:601`, `ThermalMultiplicityCharacterizationTests.cs:1739`,
`ReactiveSubscriptionLifecycleTests.cs:568` и др.) — входит в объём фазы.

Зависимости: ни от какого другого плана не зависит. Если фаза B процессных
гейтов (реестр правил) попадёт в мастер раньше — сверка версии пойдёт через
`VersionSyncTests`, а не grep.

## 7. Ревью

Независимое read-only ревью (чистый контекст, Explore) — **APPROVE-WITH-EDITS**,
2 P1 + 2 P2 + 6 P3, все внесены в этот же дифф, дизайн не менялся:
[docs/reviews/2026-09-13-excel-specification-plan-review.md](../reviews/2026-09-13-excel-specification-plan-review.md)
(R-2026-09-13-02). Ключевые правки: билдер ctor-инжектит `ICollectorRepository`
и асинхронен (в VM репозитория нет); колонка «Преднастройка» — `ZuDrosseln`
из снапшотного `Throttling`; площадь — формула из снимка; мест версии — 9.

**Дополнение после ревью (данные владельца, 2026-09-13, вне находок ревью):**
в состав спецификации добавлена секция «Резьбозажимные соединения» —
17×2 → `12506073002`, 20×2 → `12506173002`, 2 шт на каждый контур
(подача + обратка); РЗС под 25×2,3 — новый ОВ-5 (розничный артикул не
подтверждён, фитинг поставляется в комплекте промышленных IVKK);
исправлена терминология «коллекторы РЗС» → «коллекторы HKV» (РЗС —
резьбозажимное соединение). Расширение состава позиций дизайн фазы
(модель/билдер/сервис/UI) не меняет — новая секция ложится на существующие
`SpecificationRow`/секции листа.
