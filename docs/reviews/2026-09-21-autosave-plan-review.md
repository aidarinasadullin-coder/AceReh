REVIEW_ID: R-2026-09-21-04
SUBJECT: план docs/plans/2026-09-21-autosave-plan.md — пункт 3.1 роадмапа, автосохранение и восстановление сессии (снапшот autosave.smc через тот же IProjectSaveService, промпт восстановления после краха, гашение при штатном выходе); show-me docs/plans/show-me-autosave.html
REVIEWER: независимый read-only subagent, чистый контекст (Explore)
DATE: 2026-09-21
RECEIPT: настоящий файл (полный отчёт ревьюера сохранён ниже)
VERDICT: APPROVE-WITH-EDITS
REASON: каркас плана состоятелен — сервис-политика без VM, тот же IProjectSaveService (wire v1.1), канон только читается через ProjectSnapshotFactory, ADR не требуется (проверено по A1–A8, включая три целевых риска: один подтверждён закрытым, один закрыт, третий оказался другой гонкой — найдена и закрыта). Две P1 (гонка с InitialProjectPath-обнулением; тихая потеря «восстановил → закрыл, не редактируя») и две P2 устранены правками плана до реализации.

## Находки и статусы

| # | Приоритет | Находка | Статус |
|---|---|---|---|
| 1 | P1 | Гвард «InitialProjectPath пустой» (S6) не работает: LoadInitialProjectAsync гасит свойство в finally (MainWindow.xaml.cs:219/221) раньше, чем промпт вызывается (промпт — после splash.CloseAfterDelayAsync, App.xaml.cs:90) → при старте с двойным кликом .smc промпт появился бы поверх открытого файла, «Да» молча перезатёр его (dirty-подтверждения нет — сессия чистая после загрузки) | исправлено в этом же диффе: S6/§3 — признак `startedWithoutFile` передаётся в `ShowAutosaveRestorePromptAsync(...)` из `App.OnStartup` у источника аргументов командной строки |
| 2 | P1 | Тихая потеря данных: restore делает MarkClean (ResultsViewModel.cs:979,983) + S7 безусловно гасит снапшот при закрытии → «восстановил → закрыл, не редактируя» теряет всё без предупреждения (Closing-промпт не срабатывает: сессия «чистая») | исправлено в этом же диффе: S5/§2/§6 — после успешного restore сессия помечается dirty (MarkDirty — санкционированный писатель WI-5): «звёздочка», промпт закрытия и продолжение автосейва; smoke-сценарий дополнен |
| 3 | P2 | Гварду тика неоткуда взять флаги расчёта: у MainWindow нет ICalculationStateService (ctor :76-114), MainViewModel держит её приватно | исправлено в этом же диффе: гвард расчёта перенесён внутрь `ResultsViewModel.SaveAutosnapshotAsync` (ICalculationStateService уже в ctor VM, :546-567); MainWindow получает только IProjectAutosaveService |
| 4 | P2 | CleanupStale — мёртвый код: ни одной точки вызова в §2/§3, при этом show-me утверждает «чистятся при старте» | исправлено в этом же диффе: §3 — CleanupStale вызывается первым делом в `ShowAutosaveRestorePromptAsync` |
| 5 | P3 | S1 неточна: dirty сбрасывается не только сохранением/загрузкой, но и undo к точке чистоты (ADR-014) и новым расчётом | исправлено в этом же диффе: формулировка S1 уточнена |
| 6 | P3 | Якорные диапазоны чуть уже фактических (ctor MainWindow 76–114, LoadInitialProjectAsync 199–221, WriteProjectAtomicallyAsync 100–143) | исправлено в этом же диффе: диапазоны уточнены при внесении правок №1–№3 |
| 7 | P3 | Timestamp-формат не пинят; кнопки мока show-me («Да, восстановить»/«Нет») не соответствуют стандартным Да/Нет контракта IDialogService | исправлено в этом же диффе: формат `dd.MM.yyyy HH:mm` запинен в S11; show-me помечает кнопки иллюстративными |
| 8 | P3 | show-me не отражает S10 (UiSmoke-preclean) | исправлено в этом же диффе: show-me регенерирован с S10 |
| 9 | P3 | R-риск-2 остаточное: синхронные Serialize/Copy/Move на UI-потоке — унаследованный профиль ручного сохранения; deadlock невозможен; флага повторного входа достаточно | исправлено в этом же диффе: §3 явно требует try/finally вокруг флага и catch+AppLog.Warn (гейт SilentCatchScanTests) |

## Подтверждено ревьюером (выборка с путями:строками)

- **A1 якоря:** MarkDirty/MarkClean ProjectSession.cs:85-94/97-106; CurrentFilePath :72-76; BeginProjectRestore :109-138; WriteProjectAtomicallyAsync ProjectFileService.cs:100-143 (tmp `ChangeExtension(".tmp")` :109, bak :119-121, Move overwrite :124, дописывание .smc :103-105 — путь `autosave.smc` проходит); LoadProjectResultAsync :146-170; dirty-диалог ResultsViewModel.cs:962-966; SaveProject/SaveProjectAs :889-916; LoadProjectFromPathAsync :937-950; ApplyLoadedProjectAsync :957-993 (MRU :988, CurrentFilePath :982); SaveToFile :1114-1150 (dates :1119, MarkClean :1137, SetCleanPoint :1140); `_createdDate = data.CreatedDate` :1688 (выполняется до раннего выхода :1699-1703 — дата выживает при частичном сбое модулей); MainWindow ctor :76-114, Loaded :140-148, Closing :536-579 (dirty-guard :549, YesNoCancel :554-558); App.xaml.cs:78 (присвоение InitialProjectPath ДО Show :80), :90, :95, OnExit :133-144; ServiceCollectionExtensions.cs:231; MainViewModel.cs:39.
- **A2 сущности:** IProjectSaveService.SaveAsync(IProjectSession, string, ProjectSaveDates, CancellationToken) (IProjectSaveService.cs:24-28); ProjectSaveDates с fallback MinValue→Now (ProjectSaveDates.cs:5-11); dates попадают в ProjectData (ProjectPersistenceMapper.cs:29-30); ThermalIsCalculating/HydraulicsIsCalculating (ICalculationStateService.cs:29,65); DialogButtons.YesNo/DialogIcon.Question; MainViewModel.ResultsViewModel (MainViewModel.cs:39); ResetAppSettingsHelper.SettingsPath (tests/Fixtures:17); ResultsViewModel ctor принимает IProjectSaveService?/ICalculationStateService с прецедентом тестов (ResultsViewModelOpenProjectPromptTests, UndoRedoServiceJournalTests:145); конфликтов имён SaveAutosnapshotAsync/RestoreFromAutosnapshotAsync/ProjectAutosaveService/AutosaveFilePath нет.
- **A3 ripple:** `new MainWindow(` — ноль вхождений вне DI (App.xaml.cs:75, ServiceCollectionExtensions.cs:303); вызовы LoadProjectFromPathAsync — 3 входа (MainWindow.xaml.cs:206,397,468) + тесты; ApplyLoadedProjectAsync — единственный вызов ResultsViewModel.cs:949; точка вставки preclean — UiSmokeFixtureBase.cs:25-52; SilentCatchScanTests требует AppLog/throw (:53-59).
- **A4 архитектура:** R4 — reflection+using-сканы пройдёт (Services/Project без VM, ArchitectureRulesTests.cs:242-293); ProjectSnapshotFactory.Create — чистое чтение (:21-41), сессию не мутирует; WI-allowlist не расширяется; ADR не нужен (роадмап:88 — ADR только при касании владения состоянием); строка «state ownership без изменений» есть.
- **A5/A6:** интервал, «одна копия + .bak», путь, подпись диалога, файлы каталога, токены REHAU — план ↔ show-me согласованы.
- **A7 гейты:** S1–S11 с альтернативами; S7 (гасить при любом штатном закрытии) правомерен: Идеи №8 — «после краха/некорректного закрытия», «отказался сохранять» — корректное закрытие.
- **A8 процесс:** R-2026-09-21-04 свободен; bump-version.ps1 сам напоминает про WhatsNewCatalog; CI-автопилот (ci.yml job auto-release) соответствует §7; бамп 1.10.0 корректен (csproj 1.9.0, WhatsNewCatalog.cs:23 — заглушка 1.10.0).
- **Целевые риски:** R-риск-2 (повторный вход/deadlock) — ЗАКРЫТ (нет .Result/.Wait; UI-блок Copy/Move — унаследованный профиль ручного сохранения); R-риск-3 (CreatedDate) — ЗАКРЫТ и подтверждён безопасным (dates → ProjectData :29-30; `_createdDate = data.CreatedDate` до раннего выхода); R-риск-1 (гонка старта) — НЕ закрыт в исходной формулировке, но настоящая гонка другая (finally-обнуление) → находка №1, закрыта.

---

## Полный отчёт ревьюера (входящий)

Вердикт: APPROVE-WITH-EDITS. Каркас плана состоятелен (сервис-политика без VM, тот же IProjectSaveService, канон только читается, ADR не требуется — подтверждено), но найдены 2 P1-дыры в дизайне S6/S7, которые при реализации дадут тихую потерю пользовательских данных и нарушение собственного правила плана, плюс 2 P2-пробела (недостающая ctor-зависимость, точка вызова CleanupStale).

Находки (сводка с доказательствами и правками — см. таблицу выше; полный текст в исходном отчёте ревьюера):

1. P1 — S6-гвард «InitialProjectPath пустой» проверяется после обнуления свойства в finally (MainWindow.xaml.cs:219) → промпт при старте с файлом появился бы поверх открытого .smc, «Да» молча перезатирал бы его. Правка: фиксировать «старт с файлом» однократно у источника (параметр из App.OnStartup, где startupProjectPath в руках, App.xaml.cs:72-79).
2. P1 — restore→MarkClean + безусловное S7: «восстановил → закрыл, не редактируя» теряет всё без единого предупреждения (Closing :549 не спрашивает у «чистой» сессии). Правка: после restore ставить сессию dirty (MarkDirty санкционирован R2/WI-5, ArchitectureRulesTests.cs:172-179).
3. P2 — гварду тика неоткуда взять Thermal/HydraulicsIsCalculating (MainWindow их не имеет; MainViewModel держит приватно). Правка: проверку расчёта внутрь ResultsViewModel.SaveAutosnapshotAsync (ICalculationStateService уже в ctor, ResultsViewModel.cs:546-567).
4. P2 — CleanupStale никто не вызывает; окно потери .tmp существует (крах между WriteAllTextAsync :114 и Move :124). Правка: вызов в ShowAutosaveRestorePromptAsync перед HasSnapshot.
5. P3 — S1 «dirty сбрасывается только сохранением/загрузкой» неточна (ещё undo к точке чистоты — ADR-014 — и новый расчёт).
6. P3 — якорные диапазоны чуть уже фактических (76–114, 199–221, 100–143).
7. P3 — timestamp-формат не пинят; кнопки мока show-me ≠ стандартные Да/Нет контракта.
8. P3 — show-me не отражает S10.
9. P3 — синхронные Serialize/Copy/Move на UI-потоке — профиль ручного сохранения; deadlock невозможен; флага достаточно; потребовать try/finally.

Целевые риски: R-риск-1 — настоящая гонка в finally-обнулении (не в порядке присвоения/Show: App.xaml.cs:78→:80) — находка №1; R-риск-2 — закрыт; R-риск-3 — закрыт и подтверждён безопасным (ProjectPersistenceMapper.cs:29-30; ResultsViewModel.cs:1688 до раннего выхода :1699-1703; ProjectSaveDates.cs:7-8).

Итоговая рекомендация: внести правки №1-№2 (обязательно, до реализации), №3-№4, №5-№9 — по ходу. После правок план готов к реализации без перерева.
