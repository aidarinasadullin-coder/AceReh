# Review Receipt — R-2026-09-13-02

```text
REVIEW_ID: R-2026-09-13-02
SUBJECT: план docs/plans/2026-09-13-excel-specification-plan.md
         + show-me docs/plans/show-me-excel-specification.html
         (экспорт спецификации закупки в .xlsx из «Результатов»)
REVIEWER: независимый read-only subagent, чистый контекст (Explore)
DATE: 2026-09-13
RECEIPT: настоящий файл (полный отчёт ревьюера сохранён в «Находках»)
VERDICT: APPROVE-WITH-EDITS
REASON: якоря файл:строка проверены по коду — точны; предложенный дизайн
        (read-model билдер по снимкам ProjectSession + сервис экспорта)
        проверку R1–R6 не задевает: R4-скан ловит только зависимости
        Services.* от ViewModels.* (санкционированы ResultsPdfDataBuilder.cs
        и HydraulicSummaryBuilder.cs), R2-скан — только мутации срезов;
        билдер по плану читает и не мутирует. После внесения находок P1–P3
        в этот же дифф дизайн не меняется; реализация — после owner-signal.
```

## Находки и статусы

| # | Приоритет | Находка | Статус |
|---|---|---|---|
| 1 | P1 | Ф1 утверждала «каталог передаёт VM, который уже имеет ICollectorRepository» — в `ResultsViewModel` (ctor 513-530) `ICollectorRepository` нет (потребляется только `CollectorViewModel`); API репозитория async, а `Build` в плане был синхронным; ripple нового ctor-параметра (~10 тестовых мест конструирования) в объёме §6 не учтён | **исправлено в этом же диффе** (Ф1: билдер ctor-инжектит `ICollectorRepository`, `BuildAsync(IProjectSession, CancellationToken)`; VM инжектит только `IResultsExcelExportService`; §6 дополнен ripple-строкой) |
| 2 | P1 | Колонка «Преднастройка» (`CircuitRow.Presetting`) не имела источника: в `CircuitPdfData` и `HydraulicCircuitResultSnapshot` поля `Presetting` нет (есть `ValveTurns`/`ZuDrosseln` в PDF-модели, `ValveTurns`/`Throttling` в снапшоте) | **исправлено в этом же диффе** (поле переименовано в `ZuDrosseln`, источник — `HydraulicCircuitResultSnapshot.Throttling`, то же значение, что `ZuDrosseln` отчётной модели; единица уточняется при реализации по `CircuitPdfData`) |
| 3 | P2 | `CircuitRow.Area_m2` не выводился из названных источников: в `HydraulicCircuitSnapshot` поля Area нет; формула существует в коде (`HydraulicCircuitRowProjection.cs:18`) | **исправлено в этом же диффе** (Ф1/P5: площадь = CircuitLength × PipeSpacingCm / 100; оба поля в снапшоте есть) |
| 4 | P2 | §5: «8 мест по чек-листу урока №20» — арифметика неверна: csproj 1 + .iss 3 + INSTALL.md 3 + README 1 + CHANGELOG 1 = 9 | **исправлено в этом же диффе** (§5: 9 мест, перечень сохранён) |
| 5 | P3 | show-me: `--gray-300:#BFBFBF` — имя канонического токена `Brand.Gray.300` с чужим hex (канон `#B0B0B0`, Tokens.Colors.xaml:34) | **исправлено в этом же диффе** |
| 6 | P3 | show-me: карточка P2 описывала резолв арта в билдере (вариант ОВ-1«б»), тогда как план P2 и предвыбранная опция — вариант «а» | **исправлено в этом же диффе** (карточка переформулирована под вариант «а») |
| 7 | P3 | show-me: мокап меню искажал реальные подписи (`MainWindow.xaml:268-287`: «Предпросмотр PDF», «Печать», «Сохранить PDF...», «Пояснительная записка (PDF) — рабочий режим / холодный пуск») | **исправлено в этом же диффе** (подписи заменены на фактические) |
| 8 | P3 | show-me: демо-итоги не сходились (итог «по контурам, м» ≠ сумме колонки «Петля»; петля+подача ≠ метражу листа «Спецификация»; мощность 9,4 ≠ сумме строк 9,8) | **исправлено в этом же диффе** (суммы согласованы: петля 505,2 + подача 24,5 = труба 529,7 м; мощность 9,8 кВт) |
| 9 | P3 | Ф3: «IsEnabled-гейт IsDataReady — как у остальных (246)» — 246 это гейт самой сплит-кнопки; у MenuItem ContextMenu пер-пунктовых гейтов нет | **исправлено в этом же диффе** (Ф3: гейт живёт на кнопке, новый пункт добавляется без собственного IsEnabled) |
| 10 | P3 | Ф2: «# ##0» — не код числового формата Excel (канон «#,##0»); Ф4: фактическое имя API — `TestContext.CurrentContext.WorkDirectory` | **исправлено в этом же диффе** |

## Подтверждено ревьюером по коду

- Заглушка `ExportExcel` существует (`ResultsViewModel.cs:784-796`), `ExportExcelCommand` не привязан ни в одном XAML/cs.
- Меню отчёта: сплит-кнопка + ContextMenu из 5 PDF-пунктов (`MainWindow.xaml:238-290`), гейт `IsDataReady` на кнопке (246); `IDialogService.ShowSaveFileDialog` (23-27) покрывает потребности.
- Данные: `TotalPipeLength = Σ(CircuitLength + SupplyLength)` (`ResultsKpiPresenter.cs:84`); `CircuitPdfData` (ResultsPdfData.cs:11-57) артов не содержит; `ThermalPipeSnapshot.Article` (ThermalStateSnapshots.cs:29-32) течёт из `PipeType.StandardPipes` (PipeType.cs:49-78, арты 12180501001/…2001/…3001); json-канон — 1200170000/1200200000/1200250000; `fittings`/`accessories` в json пустые; HKV-артикулы совпадают с show-me; вне снапшот-механики `.Article` не читается; в тестах единственное вхождение — тестовая фикстура, не пин.
- Архитектура: R4 — reflection-скан зависимостей Services.* → ViewModels.* + regex `using SnowMeltingCalculator.ViewModels` по src/Services/** (санкционированы два файла); R2 — regex мутаций срезов; R5 — скан ResultsViewModel.cs. Новый билдер/сервис по плану сканов не задевает; списки writers план не трогает.
- Библиотеки: Office/xlsx-пакетов в csproj нет; централизованного package management нет; лицензии (ClosedXML MIT, EPPlus 5+ Polyform Noncommercial, OpenXml MIT, NPOI Apache-2.0) — согласны.
- Версионирование: места урока №20 существуют (их 9, не 8); `VersionSyncTests` в мастере нет — ручной grep уместен; `docs/manual/README.html` существует.
- show-me: самодостаточен (нет внешних ресурсов); интерактив жив (табы и выбор ОВ, непустые токены классов, инициализация в конце body); бренд-написание по брендбуку §7 (латиничное REHAU — только блок-логотип).
