REVIEW_ID: R-2026-09-21-02
SUBJECT: план docs/plans/2026-09-21-updates-whatsnew-plan.md — пункт 1.3
  роадмапа post-1.8: «Файл → Проверить обновления» (манифест latest.json
  на Google Drive, чтение по прямой ссылке) + диалог «Что нового» при
  первом запуске новой версии (локальный каталог, без сети). Ревью плана
  (код ещё не писан); show-me docs/plans/show-me-updates-whatsnew.html.
REVIEWER: независимый read-only subagent, чистый контекст (Explore)
DATE: 2026-09-21
RECEIPT: настоящий файл (полный отчёт ревьюера сохранён в журнале сессии,
  сводка ниже)
VERDICT: APPROVE-WITH-EDITS
REASON: якоря и сущности точны (AddApplicationServices:284, версия сборки
  из <Version> csproj, AppSettings/IDialogService/AboutWindow, конвенция
  ключей 1.2 буквально предзаписала WhatsNewShownVersion). R1–R6 не
  задеты: AppSettings — оболочка, .smc wire не тронут, R4-скан накрывает
  новую зону Services.Updates автоматически. Ключевая P1 — точка показа
  «Что нового» в MainWindow_Loaded вешала модальный диалог под
  Topmost-сплэшем и блокировала его закрытие; точка перенесена в
  App.OnStartup после splash. Все 9 находок приняты и внесены тем же
  днём. Реализация — в рамках запуска пункта 1.3 владельцем.

## Находки и статусы

| # | Приоритет | Находка | Статус |
|---|---|---|---|
| 1 | P1 | Показ «Что нового» в `MainWindow_Loaded`: Loaded поднимается внутри `mainWindow.Show()` (`App.xaml.cs:75-90`), модальный диалог до `splash.CloseAfterDelayAsync` (MinSplashDuration, `SplashWindow` Topmost) держит вложенную накачку сообщений — сплэш висит над невидимым диалогом | исправлено в плане: показ перенесён в `App.OnStartup` после `CloseAfterDelayAsync` (`mainWindow.ShowWhatsNewIfPending()`, `Owner = MainWindow`) |
| 2 | P2 | UiSmoke: локальный полный прогон включает UiSmoke (CI отсекает только фильтром); на чистом `SNOWCALC_SETTINGS_DIR` после бампа с записью каталога exe покажет модальный диалог → смоук-сценарии красные (флаки-once) | исправлено: §4 — pre-seed `settings.json` с `WhatsNewShownVersion` в фикстуре запуска exe (env наследуется) |
| 3 | P2 | U8 опиралась на непроверенное «rclone copyto поверх сохраняет FILE_ID» — в репо подтверждений нет (уроки №38/№39 про другое, upload-to-drive.ps1 использует copy) | исправлено: скрипт гейтит — FILE_ID до/после `copyto` через `rclone lsjson`, падает при изменении; U8 + сверка ссылки владельцем на первом автопублике |
| 4 | P2 | Заполнение `WhatsNewCatalog` при бампе ничем не гейтится — пропуск = идея №9 молча мертва (родственно уроку №41: мета-заявка без гейта) | исправлено: warning-шаг в release.yml при отсутствии записи для `RELEASE_VERSION` + напоминание в выводе bump-version.ps1; тест-гейт с записью — вариант владельца (дефолт warning) |
| 5 | P3 | show-me: мокап меню потерял «Сохранить как...» (Ctrl+Shift+S) — фактический порядок MainWindow.xaml:193-195 | исправлено: show-me регенерирован (урок №27) |
| 6 | P3 | Канон заголовка диалога: план «Доступна версия X» vs show-me «Доступна новая версия: 1.10.0» | исправлено: канон в §4 — «Доступна новая версия: X» |
| 7 | P3 | «Шаг НЕ гейтит релиз» — единоличное решение агента без варианта A/B (урок №28), расходится с гейт-стилем соседнего шага заливки | исправлено: оформлено решением U9 (warning-only дефолт / fail — выбор владельца) |
| 8 | P3 | Алгоритм генерации манифеста дублировался (скрипт + inline pwsh в release.yml) — дрейф при правке | исправлено: release.yml вызывает тот же `scripts/publish-latest-manifest.ps1 -RcloneConf ...` |
| 9 | P3 | Пачка недоспецификаций: `SilentCatchScanTests` требует AppLog в catch (тихие catch дадут красный прогон); семантика мусорной `shownVersion` не зафиксирована; `folderUrl` из сети уходит в `Process.Start` без гварда схемы; пустой `ManifestUrl` ничем не напоминает о U8; §8 обещал «скачивается без подтверждений» | исправлено: §2 — AppLog.Warn в канале/парсере; ShouldShow: мусор → показать (тест); `IsSafeFolderUrl` https-only (тест); CI-warning при пустой константе; §8 — graceful failure |

## Подтверждено ревьюером (выборка с путями:строками)

- **A1 якоря:** `AddApplicationServices` — ровно `src/Configuration/ServiceCollectionExtensions.cs:284`; net8 SDK генерирует AssemblyVersion из `<Version>1.9.0</Version>` → `GetName().Version` = 1.9.0.0, нормализация плана даёт «1.9.0»; `1.10.0 > 1.9.0` в Version-сравнении честно.
- **A2 существование:** `AppSettings.IsSidebarCollapsed`/:39, `Save()`/:75; конвенция плана 1.2 §1 буквально предзаписала имя `WhatsNewShownVersion` (string?); `IDialogService.Show(...)` + `DialogIcon.Information` существуют; `AboutWindow` SizeToContent/ShowInTaskbar/CenterOwner — образец точен; CHANGELOG.md формат `## [1.9.0] - 2026-09-21` + `### Добавлено/Исправлено` — парс реалистичен (скрипт должен брать подсекции независимо от порядка).
- **A3 ripple:** конфликтов имён Updates/WhatsNew нет; поля MainWindow не пересекаются; прецедент сервиса в code-behind — `_recentProjects` (1.2); `ViewTokenHygieneTests` сканирует только Views/Controls/MainWindow.xaml — `src/WhatsNewWindow.xaml` вне гейта (стиль токенами сознателен); AutomationId-контракт не ломается (прецедент RecentProjectsMenuItem); System.Net.Http — shared framework net8, без PackageReference.
- **A4 архитектура:** R4-скан ArchitectureRulesTests накрывает новую зону Services.Updates (чисто); запретов на сеть/HttpClient нет; AppSettings — санкционированная оболочка; .smc wire не тронут.
- **A5 арифметика:** 4 тест-файла; кейсы парсер 4 / чек-сервис 6 / трекер 5 / каталог 1 сходятся с show-me; метрики согласованы; JSON примеров идентичен.
- **A6:** позиция U6 существует (MainWindow.xaml:215-222); кнопки/поля диалога совпадают (кроме #5/#6, исправлены).
- **A7/A8 процесс:** уроки №26/:521 и №27/:539 существуют; шаблон чека существует; автопубликация манифеста не нарушает решение владельца об автопилоте релиза (роадмап:26-27); Идеи №5 (Идеи.md:61) и №9 (:119) покрыты; U8 явно назначена владельцу.

## Дополнение (2026-09-21, по итогам реализации)

- **Прогон поймал ошибку семантики, пропущенную ревью:** формулировка
  «shown старее → false» в §6 противоречила основному сценарию фичи
  (обновление 1.9.0 → 1.10.0, shown="1.9.0" ≠ current — единственный
  момент, когда диалог и должен показаться). Исправлено: ShouldShow =
  hasEntry && normalized(shown) != current; тест
  `ShouldShowForEntry_ShownOlder_True_NormalUpdate`, план §6
  синхронизирован. Урок в духе №26: «подтверди корректность»-проход не
  ловит семантические инверсии — ловят красные тесты на живом коде.
- Мелочь реализации: `FakeUpdateChannel`-тест «не настроен → без сетевого
  вызова» заменён честным тестом самого `DriveUpdateChannel` (ранний
  return до HttpClient); BOM-гвард `\uFEFF` в `TryParse` (генератор
  манифеста может дописать BOM — JsonDocument таковой отвергает).

---
