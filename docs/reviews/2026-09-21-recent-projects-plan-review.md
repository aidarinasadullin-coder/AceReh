REVIEW_ID: R-2026-09-21-01
SUBJECT: план docs/plans/2026-09-21-recent-projects-plan.md — пункт 1.2
  роадмапа post-1.8: «Файл → Недавние проекты» (MRU в AppSettings,
  до 10 путей) + drag-drop .smc на главное окно; оба входа в существующий
  ResultsViewModel.LoadProjectFromPathAsync. Ревью плана (код ещё не
  писан); show-me docs/plans/show-me-recent-projects.html.
REVIEWER: независимый read-only subagent, чистый контекст (Explore)
DATE: 2026-09-21
RECEIPT: настоящий файл (полный отчёт ревьюера сохранён в журнале сессии,
  сводка ниже)
VERDICT: APPROVE-WITH-EDITS
REASON: точка входа и архитектура подтверждены: LoadProjectFromPathAsync —
  действительно единая точка загрузки по пути (ApplyLoadedProjectAsync
  вызывается ровно из одного места; LoadProjectResultAsync в src — один
  вызов), MRU — состояние оболочки в AppSettings, ProjectSession/канон-
  писатели/.smc wire не затронуты (A2/A4), R4-скан ArchitectureRulesTests
  накрывает новый сервис автоматически. Цензус подписок и
  MainWindowChromeLayoutTests не задеты (A3). Все 7 находок — про якоря,
  тест-гейты и формулировки решений; все приняты автором плана и внесены
  тем же днём. Реализация — после owner-signal.

## Находки и статусы

| # | Приоритет | Находка | Статус |
|---|---|---|---|
| 1 | P2 | Гейт `ViewTokenHygieneTests` не упомянут: план §5 задавал оверлей «полупрозрачный тёмный фон» без токенов — реализатор написал бы литеральный HEX, сканер `tests/.../Views/ViewTokenHygieneTests.cs:15-22,64-68` (ratchet `MainWindow.xaml = (0,0)`) красит тест, вопреки §7 «без правок ожиданий» | исправлено в плане: §5 переписан на `Brand.Black.Brush` + `Opacity=0.55`, `Brand.Red.Brush`, `Font.Size.Section/Caption` (существование токенов сверено с `Tokens.Colors.xaml:69/:71`, `Tokens.Typography.xaml:20-21`) |
| 2 | P2 | Якорь §3 «строка ~958» ложный (внутри dirty-подтверждения); реальная точка — `_projectSession.CurrentFilePath = filePath` 978, финальный `MarkClean()` 979, `StatusMessage` 981; в методе ДВА `MarkClean` (975/979) — «после MarkClean» двусмысленно; якорь §0 930 — строка XML-дока, сигнатура 933 | исправлено в плане: §0 → :933; §3 → «после финального MarkClean (строка 979), до StatusMessage (981)» с оговоркой о двух вызовах |
| 3 | P3 | Арифметика §7 ↔ show-me: в плане перечислено 8 кейсов, show-me заявлял «9 кейсов» (собственный перечень show-me — тоже 8) | исправлено: план §7 нумерует 8 тест-кейсов + пин; show-me регенерирован («8 тест-кейсов + пин», урок №27) |
| 4 | P3 | Канонический текст оверлея противоречил себе: §0.1 D4 «…проекта, чтобы открыть» vs §5 «…проекта (.smc), чтобы открыть»; подсказка «Формат .smc · один файл» из show-me в спеке XAML отсутствовала | исправлено: канон в §5 — заголовок «Отпустите файл проекта, чтобы открыть» + подсказка «Формат .smc · один файл», show-me соответствует |
| 5 | P3 | «Сохранить как...» в новый путь не пополняет MRU (`ResultsViewModel.cs:908-911` идёт мимо `ApplyLoadedProjectAsync`) — сценарий не решён и не задокументирован | исправлено: §8 — осознанное ограничение v1 («MRU = недавно открытые»), «недавно сохранённые» — явное решение владельца, может править D2 при ревью |
| 6 | P3 | §0.1 колонка «Альтернатива (отклонённая)» — агент единолично «отклонил» варианты до выбора владельца (вектор A7, урок №28) | исправлено: колонка переименована в «Альтернатива (выбор за владельцем)» |
| 7 | P3 | Кросс-фикстурный зацеп: тесты, грузящие проект через VM (`UndoRedoServiceJournalTests.cs:145`, `ResultsViewModelOpenProjectPromptTests`, `ThermalMultiplicityCharacterizationTests.cs:1134`), начнут писать MRU в общий sandbox `settings.json`; пин `Save_PersistsRecentProjects` с равенством списка был бы хрупким | исправлено: §7 — assertion пина «содержит», не равенство; кросс-загрязнение sandbox принято как осознанное (параллелизма в сборке нет) |

## Подтверждено ревьюером (выборка с путями:строками)

- **A1 якоря:** `MainWindow.xaml:192-194` («Открыть...»/Ctrl+O), `MainWindow.xaml.cs:202` и `193-210` (паттерн try/catch + AppLog + ShowError), `MainViewModel.cs:159-160` (AppSettings.Instance…Save()), `AppSettings.cs:66/:78` (Save, WriteIndented), `AppSettingsTests.cs:13-26/:64-81`, `ResultsViewModel.cs:597` — точны; два дрейфа якорей — находка №2.
- **A2 существование/единственность:** `LoadProjectFromPathAsync` public (ResultsViewModel.cs:933); `ApplyLoadedProjectAsync` — ровно один вызов (:945); `LoadProjectResultAsync` в src — один вызов; `ShowOpenFileDialog` (IDialogService.cs:34); конфликтов имён `RecentProjects*` нет.
- **A3 ripple:** у ResultsViewModel один ctor (:542); `Shell.SubMenuItem` отсутствует, но существующие вложенные пункты тоже без стиля — консистентно; `MainWindowChromeLayoutTests.cs:16` не задет (оверлей внутрь корневого Grid); AutomationId-контракт (`ThermalAutomationIdSelectorContractTests.cs:122-136`) проверяет только требуемые ID — новые безопасны.
- **A4 архитектура:** R4-скан ArchitectureRulesTests (:242-293) автоматически накрывает Services.RecentProjects; R2/R5 не задеты; .smc wire (R6) не тронут; settings.json — существующий writer.
- **A6 show-me ↔ план ↔ код:** порядок и InputGestureText меню совпадают с MainWindow.xaml:185-213; JSON show-me = §1; токены #E50040/#4FC7B5/#1D1D1B — канон.
- **A7/A8 процесс:** уроки №26 (lessons.md:521) и №27 (:539) существуют; шаблон чека существует; ManualVersionGateTests и build_manual.py существуют; раздел «Файл» в template.html:202,380-381 для правки §6 есть.
- **Технические дыры вне векторов:** e.Effects=None + Handled — штатный отказ; Items.Clear() в SubmenuOpened — дублей нет; Save() синхронный на UI-потоке (прецедент есть); List<string> сериализуется без опций; WindowChrome CaptionHeight=0 (MainWindow.xaml:23-27) с OLE-драгом не конфликтует; drag поверх модального диалога — класс поведения существующий (Ctrl+O одинаково незащищён).

---
