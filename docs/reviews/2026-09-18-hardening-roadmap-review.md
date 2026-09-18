REVIEW_ID: R-2026-09-18-03
SUBJECT: план docs/plans/2026-09-18-hardening-roadmap-plan.md (амендмент II) — hardening-роадмап, восемь волн + backlog; парный show-me docs/plans/show-me-hardening-roadmap.html
REVIEWER: независимый read-only subagent, чистый контекст (Explore)
DATE: 2026-09-18
RECEIPT: настоящий файл (полный отчёт ревьюера сохранён ниже)
VERDICT: APPROVE-WITH-EDITS
REASON: план реализуем и процессно корректен (D10 открыт, owner-решения не
присвоены, wire .smc не задет — A4 подтверждён по коду, ADR-016 свободен),
но волна 1 — единственная необратимая — описывала устаревшее состояние
репо (презентации уже выведены из git в 4adecd7, LFS-цели и базлайны
размеров неверны). Все 12 находок приняты; правки внесены в план (§2.1,
§2.2, §2.3, §3.2, §5, §7.1, §A.3, §12) и show-me регенерирован до старта
какой-либо волны. Реализация — после owner-signal.

## Находки и статусы

| # | Приоритет | Находка | Статус |
|---|-----------|---------|--------|
| 1 | P1 | Волна 1 описывает несуществующее состояние репо: `docs/presentation/` и `Презентация/` выведены из git коммитом `4adecd7` (2026-09-18 22:20) ДО коммита плана (`ec3bf7e`, 23:06); строки «остаётся финальный канон» ложны; LFS-цели §2.3 мигрировали бы ноль объектов | исправлено в диффе плана: §2.2 — презентации = только исторические блобы (~65+ МБ), §2.3 — единственный LFS-кандидат `docs/architecture-migration/evidence/**/*.png` (62 файла, 56.6 МиБ); show-me регенерирован |
| 2 | P2 | Базлайн size-pack неверен: план 242.16 МиБ, факт 446.77 МиБ (замер подтверждён автором плана после ревью) | исправлено: §2.2 — замер 446.77 МиБ, порог < 60 МиБ с пометкой «уточняется dry-run» |
| 3 | P2 | Арифметика §A.3: «18/21» сходилось только при пропуске H21 (веса репо) — H21 не упомянут в волнах | исправлено: H21 явно замаплен на волну 1 (purge истории + LFS) в §A.3 |
| 4 | P2 | «10 сайтов Task.Delay» — недосчёт: фактически 18 (плюс ветки ошибок :708,:793,:845,:1000; печать :1058,:1064; SaveToFile :1092,:1104) | исправлено: §7.1 — все 18 с полным перечнем; эффект по тестам (−36 c) сохранён с оговоркой |
| 5 | P3 | Якорь `.gitignore:43` — правило `.omo/` на строке 42 | исправлено: §2.2 |
| 6 | P3 | Якорь фикстуры — фактический путь `tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc` | исправлено: §2.2 |
| 7 | P3 | Состав `Тест/`: 15 .smc + 5 .bak + 1 md = 21 (план: 6 .bak = 22) | исправлено: §2.2 (перепроверено автором: `git -c core.quotepath=off ls-files -- Тест`) |
| 8 | P3 | «5 пустых catch»: строго пустых 0; тел-только-комментарий ≥7 (AppSettings.cs:42,69; ProjectFileService.cs:135; CityAutoCompleteBox.xaml.cs:352; ConstructionVisualizationRenderer.cs:691; ClimateViewModel.cs:358,407) | исправлено: §0 (суть) и §5 |
| 9 | P3 | Ссылки на purge-пути шире «одной известной»: formulas/baseline-coverage.md:13; Планируемые_изменения.md:600,610-617; GlycolDataService_Fix упомянут в замороженном evidence-манифесте; build_temp — в evidence; PROJECT_STATUS — в append-only lessons.md:324 | исправлено: §2.2 — полный перечень + правило «повисшие ссылки замороженного provenance принимаются записью» |
| 10 | P3 | Предпосылка блокировки старта устарела: дерево чисто (параллельная сессия закоммитила работы) | исправлено: шапка и §12 — гейт «перепроверить git status перед filter-repo» (сессия активна); решение о старте — владельца |
| 11 | P3 | Process-гэп: в quality-log не внесены записи R-2026-09-17-01/02, R-2026-09-18-01/02 | отложено владельцу (журнал ведётся с начала; бэкфилл чужих чеков — owner-решение); запись настоящего ревью R-2026-09-18-03 добавлена |
| 12 | P3 | Якорь `ServiceCollectionExtensions.cs:255-259` — фактический блок :254-258, CurrentDispatcher на :257 | исправлено: шапка и §3.2 |

## Подтверждено ревьюером (выборка с путями:строками)

- A1/A2 якоря и сущности: ResultsViewModel.cs:1035-1066 (Verb="print" без
  UseShellExecute, Process без using), IDialogService.ShowPrintDialog
  существует (src/Services/Navigation/IDialogService.cs:47);
  ThermalCalculator.cs:54/:411/:607+/дубли :90-99≡:487-499 и :300-310≡:370-376;
  ThermalConstants.HeatTransferCoefficientA/B/C (:89/:97/:105);
  HydraulicsConstants.MaxPressureLoss_Pa=32000 (:20), копии 32000 в
  ValidationConstants.cs:241, CollectorSummary.cs:140, CollectorRepository.cs:183
  (+328), CollectorTypeSelector.cs:46 («32 кПа»), Converters.cs:289;
  CircuitsCalculator.cs:35 (деление без гварда) и литералы :56-116;
  Kv-триплет ValveTurnsCalculator.cs:24-34 / CircuitsViewModel.cs:705-711 /
  HydraulicsConstants (:69); fallback 35/30 — CircuitsViewModel.cs:633-643;
  GlycolDataService.cs:591-594 (NaN-подмена) и :683-836 (fallback-таблицы);
  data/glycol_data.json + glycol_data.yaml, сервис читает только json (:48);
  ConstructionViewModel.cs:554-577 (Save без диалога), :582-595 (Load
  фиксированного файла), дедуп :452-490≡:598-636; тестовые якоря
  ThermalBaselineTests.cs:155-165 (:116 Explicit), CircuitsBaselineTests.cs:178-198
  (:134), TextBoxBehaviorTests.cs:204-211 (таутология подтверждена),
  4 копии ResetAppSettings, ResultsViewModelSaveSnapshotTests.cs:616,646
  (DateTime.Now); CityAutoCompleteBox.xaml.cs:336-356, :281-290;
  ThermalStateCoordinator.cs:155-187.
- A3/A4: .smc хранит `bool IsOperatingMode` (ProjectData.cs:59,
  ProjectSnapshot.cs:16) — карта enum→температура wire не меняет; «логгер
  первым в DI» не влияет на диспетчер UndoRedoService (фикс захвата —
  волна 2, раньше волны 4); ArchitectureRulesTests существует; ADR-016
  свободен (журнал кончается ADR-015); CI без lfs — согласовано с LFS-политикой;
  `.gitattributes` отсутствует (конфликта нет); комментарий
  TreatWarningsAsErrors в tests csproj:11 ложный (настройки нет) — H15
  подтверждён.
- A5 арифметика (дёшево пересчитанное): catch=79 ✓; Debug.WriteLine=29 ✓;
  .omo=124 файла ✓; build_temp=20 (16 sqlite) ✓; строки VM 1843/1583/1352 ✓;
  baseline 43 кейса ✓; size-pack — находка №2.
- A6 show-me ↔ план: 8 волн, D1–D9+D10, гейты, backlog-триггеры, метрики
  шапки — совпадают; унаследованные ошибки (18/21, недосчёт Task.Delay)
  исправлены в обоих артефактах при регенерации.
- A7 гейты: owner-решения не присвоены (D10 открыт, .rfa «подтвердить»,
  удаление встроенных таблиц гликоля и канал UI-предупреждения — на ревью
  волн).
- A8 процесс: quality-log и docs/reviews/ существуют; REVIEW_ID R-2026-09-18-03
  корректен (заняты -01, -02).

---

## Вектора атаки (канонический блок промпта ревью — урок №26)

Промпт ревьюера содержал канонические вектора A1–A8 из
`docs/agents/review-receipt-template.md` (якоря файл:строка; существование
полей/методов/типов; ripple — ctor/тесты/XAML/деплой/AutomationId и
существование названных ID; сканы R1–R6/writers/wire .smc; арифметика
плана и show-me; сверка show-me ↔ план; гейты owner-решений; процесс
quality-log) плюс 12 предметных групп обязательных якорей (печать,
ThermalCalculator, CircuitsCalculator, Kv/320, GlycolDataService,
DI-диспетчер, ConstructionViewModel, 8 тестовых якорей, CityAutoCompleteBox,
ThermalStateCoordinator, purge-цели vs живые ссылки, сериализация enum в
wire).

Правила режима соблюдены: вердикт с находками (12), show-me регенерирован
из плана (урок №27), запись в quality-log добавлена.
