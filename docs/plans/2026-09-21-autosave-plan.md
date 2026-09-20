# План: автосохранение и восстановление сессии — пункт 3.1 роадмапа post-1.8

> Дата: 2026-09-21. Статус: план на ревью владельца; сигнал на полный цикл
> без промежуточных согласований дан владельцем 2026-09-21 («после
> атакующего ревью и правок приступай к реализации, не жди моего
> согласования») — рекомендации §0.1 вписаны как дефолт-решения (практика
> plan-approval). Основание: роадмап
> [2026-09-20-post-1.8-roadmap.md](2026-09-20-post-1.8-roadmap.md) п. 3.1,
> [Идеи.md](../Идеи.md) №8 (одобрена владельцем 2026-09-20). show-me:
> [show-me-autosave.html](show-me-autosave.html). Условия роадмапа:
> снапшот в формате .smc (тот же wire, отдельный канал), ретеншн — одна
> копия, хранение на инфраструктуре AppSettings; persistence-смежная зона:
> владение состоянием не затрагивается → ADR не требуется (§8).

## 0. Суть

При несохранённых изменениях проекта приложение раз в ~2 мин пишет
автоснапшот `autosave.smc` в служебный каталог рядом с settings.json
(формат .smc — тот же wire v1.1, тот же `IProjectSaveService`; канал
записи один — файловая политика отдельным сервисом). При штатном закрытии
снапшот гасится; если приложение завершилось крахом/было убито, при
следующем старте показывается вопрос «Восстановить проект?»; «Да» —
проект восстанавливается (без привязки к служебному файлу), «Нет» —
снапшот удаляется.

**State ownership без изменений.** Владельцы состояния не меняются:
автосейв только читает канон (через `IProjectSnapshotFactory` — тот же
путь, что ручное сохранение) и пишет служебный файл; `.smc`-wire не
меняется; allowlist-писатели не расширяются; ключей AppSettings проект
не добавляет (S9).

### 0.1. Решения-дефолты (рекомендации агента; владелец может переопределить при ревью)

| # | Вопрос | Решение (дефолт) | Альтернатива (выбор за владельцем) |
|---|---|---|---|
| S1 | Триггер записи | `ProjectSession.IsDirty` по тику таймера: dirty сбрасывается ручным сохранением, загрузкой проекта, undo к точке чистоты и новым расчётом (`ProjectSession.cs:85-106`, ADR-014) — это и есть «есть несохранённые изменения». Автосейв **не** делает `MarkClean`/`SetCleanPoint` и не пишет статус-бар — семантика «звёздочки» и подтверждения закрытия не трогается; пока проект остаётся dirty, каждый тик перезаписывает ту же копию | Отдельный флаг «изменено с прошлого автосейва» — лишнее состояние, расходится с условием «незавершённый проект» |
| S2 | Канал записи | Тот же `IProjectSaveService.SaveAsync(session, autosavePath, dates)` — wire v1.1 и атомарная запись tmp→move + .bak уже встроены в `ProjectFileService.WriteProjectAtomicallyAsync` (`ProjectFileService.cs:100-141`); автосейв не создаёт ни нового сериализатора, ни нового формата | Своя сериализация — второй writer wire-формата, риск расхождения |
| S3 | Даты снапшота | `new ProjectSaveDates(_createdDate, DateTime.Now)` из `ResultsViewModel` — CreatedDate проекта сохраняется: восстановление из снапшота (`LoadProjectDataAsync` → `_createdDate = data.CreatedDate`, `ResultsViewModel.cs:1688`) не испортит дату создания в отчётах | Передавать `DateTime.Now` как created — дата создания проекта «поехала» на момент краха |
| S4 | Разделение слоёв | Политика файла — `IProjectAutosaveService` (`Services/Project/`, без WPF): путь, `HasSnapshot`, `DeleteSnapshot`, `CleanupStale` (остатки .tmp), предикат тика; таймер и гварды — code-behind `MainWindow` (shell-механика, прецедент `ShowWhatsNewIfPending`); сохранение — `ResultsViewModel.SaveAutosnapshotAsync()` (единственный владелец `_createdDate`), восстановление — `ResultsViewModel.RestoreFromAutosnapshotAsync()` | Таймер в VM — нет прецедентов; сервис с зависимостью от VM — нарушение R4 |
| S5 | Привязка пути при восстановлении | Restore грузит данные **без** `CurrentFilePath` (остаётся null → «Сохранить» ведёт в SaveAs — пользователь выбирает путь сам; исходный путь проекта в .smc не хранится, привязать его неоткуда) и **без** пополнения MRU: служебный путь в MRU и в канон-пути проекта недопустим. После успешного восстановления сессия помечается **dirty** (`MarkDirty` — санкционированный писатель, WI-5): восстановленные данные не живут ни в каком пользовательском файле, поэтому честны «звёздочка», промпт закрытия и продолжение автосейва; сценарий «восстановил → закрыл, не редактируя» получает запрос «Сохранить перед закрытием?» вместо тихой потери. Рефакторинг: тело `ApplyLoadedProjectAsync` выделяется в ядро с `filePath: string?` (`null` = восстановление) | Привязать `autosave.smc` — «Сохранить» перезаписывал бы служебный файл, который гасится при выходе; restore без MarkDirty — тихая потеря при закрытии |
| S6 | Когда предлагать восстановление | Только при старте **без** файла проекта в аргументах командной строки: признак `startedWithoutFile` передаётся в промпт из `App.OnStartup`, где `startupProjectPath` ещё известен — полагаться на `InitialProjectPath` нельзя: `LoadInitialProjectAsync` гасит свойство в `finally` (`MainWindow.xaml.cs:221`) раньше, чем промпт вызывается. При старте с .smc снапшот не предлагается и не удаляется — гасится при штатном закрытии. Диалог — после `splash.CloseAfterDelayAsync` и **до** `ShowWhatsNewIfPending` (урок P1 чека R-2026-09-21-02: модальный диалог из `Loaded` виснет под Topmost-сплэшем) | Промпт и при старте с файлом — гонка двух загрузок сессии |
| S7 | Гашение при штатном выходе | `MainWindow.Closed` → `DeleteSnapshot()` при любом штатном пути закрытия (сохранил / осознанно отказался / проект чист) — снапшот не должен переживать закрытие, иначе следующий старт предложит восстановить то, что пользователь отверг. Промпт виден только после краха/убийства процесса | Удалять только при «сохранил» — отвергнутые изменения всплывали бы при каждом старте |
| S8 | Гварды тика | `IsDirty && !IsLoadProjectInProgress && !ThermalIsCalculating && !HydraulicsIsCalculating` — не снимать снапшот посреди загрузки/пересчёта (пропущенный тик догонит следующий); повторный вход тика гвардится флагом. Ошибка записи → `AppLog.Warn`, тихо, работу не мешает | Писать во время расчёта — риск неконсистентного снимка |
| S9 | AppSettings | Ключей **нет** (всегда включён в v1; тумблер — по запросу владельца позже). Файл — статическое свойство `AppSettings.AutosaveFilePath` (`<settings-каталог>\autosave.smc`, та же инфраструктура `SNOWCALC_SETTINGS_DIR` — тестовая изоляция бесплатно). Схема ключей очереди не расширяется — расширять нечем | Ключ `AutosaveEnabled` — преждевременно без UI-тумблера |
| S10 | UiSmoke | `UiSmokeFixtureBase` чистит autosave-файлы песочницы перед Launch (рядом с `PreseedWhatsNewShown`) — детерминизм независимо от способа завершения exe (`_app.Close()` грейсфул, но крах-пути не гарантируют) | — |
| S11 | Диалог | `_dialogService.Show(message, title, DialogButtons.YesNo, DialogIcon.Question)` — UI-нейтральный контракт (тот же, что подтверждение открытия dirty-проекта, `ResultsViewModel.cs:962`); текст с временной меткой файла, формат `dd.MM.yyyy HH:mm` (кнопки мока show-me иллюстративны — реально стандартные Да/Нет контракта) | Отдельное WPF-окно — дороже без пользы |

## 1. Сервис политики (`src/Services/Project/`, новые файлы)

| Файл | Содержимое |
|---|---|
| `IProjectAutosaveService.cs` | `string SnapshotPath { get; }`; `bool HasSnapshot()`; `DateTime? SnapshotTimestamp()` (mtime файла); `void DeleteSnapshot()` (`.smc` + `.bak`); `void CleanupStale()` (остатки `.tmp`); `static bool ShouldSnapshot(bool isDirty, bool isLoadInProgress, bool isCalculating)` — чистый предикат |
| `ProjectAutosaveService.cs` | Реализация: путь из `AppSettings.AutosaveFilePath`; операции файловые, без WPF и без состояния канона; отсутствие файлов — норма (не лог), ошибки файловых операций — `AppLog.Warn` + false/no-op |

Регистрация в DI: `AddSingleton<IProjectAutosaveService, ProjectAutosaveService>()` рядом с `IProjectSaveService` (`ServiceCollectionExtensions.cs:231`).

## 2. `ResultsViewModel` (адаптер, канон не трогает)

| Правка | Содержимое |
|---|---|
| `SaveAutosnapshotAsync(string path)` | Гвард (внутри VM — там `ICalculationStateService` уже в ctor, `ResultsViewModel.cs:546-567`; MainWindow расчётные флаги не нужны): `ProjectAutosaveService.ShouldSnapshot(_projectSession.IsDirty, _projectSession.IsLoadProjectInProgress, _calculationStateService.ThermalIsCalculating \|\| _calculationStateService.HydraulicsIsCalculating)` → false = тихий выход; запись: `await _projectSaveService.SaveAsync(_projectSession, path, new ProjectSaveDates(_createdDate, DateTime.Now))` — **без** `MarkClean`/`SetCleanPoint`/статус-бара (S1); публичный, вызывается тиком MainWindow |
| `RestoreFromAutosnapshotAsync(string path)` | Загрузка через `_projectFileService.LoadProjectResultAsync(path)`; успех → общее ядро применения данных с `filePath: null` + `_projectSession.MarkDirty()` (S5 — восстановленное нигде не сохранено); неуспех → `ShowError` + удаление снапшота (битый файл не маячит при каждом старте) |
| Рефакторинг ядра | `ApplyLoadedProjectAsync(filePath, data)` → ядро `ApplyProjectDataAsync(data, filePath: string?)`: подтверждение dirty, Clear undo, Reset, ResetModules, MarkClean, LoadProjectDataAsync — общие; привязка `CurrentFilePath`, пополнение MRU и статус «Проект загружен» — только при `filePath != null`; для `null` — статус «Проект восстановлен после аварийного завершения». Публичная точка `LoadProjectFromPathAsync` и её сигнатура не меняются (единственная точка входов 1.2 сохранена) |

## 3. Shell (`MainWindow.xaml.cs`, `App.xaml.cs`)

| Правка | Содержимое |
|---|---|
| `MainWindow` ctor | +1 параметр `IProjectAutosaveService autosaveService` (singleton DI); поле `_autosave` |
| Таймер | `DispatcherTimer` 2 мин, Start в ctor после InitializeComponent; тик — async-обработчик: флаг повторного входа под `try/finally`, `catch` + `AppLog.Warn` (гейт `SilentCatchScanTests`); гвард расчёта/загрузки — внутри `SaveAutosnapshotAsync` (§2), тик просто вызывает его. Stop + `DeleteSnapshot()` в `Closed` (S7) |
| `ShowAutosaveRestorePromptAsync(bool startedWithoutFile)` | Публичный, код-бихайнд (прецедент `ShowWhatsNewIfPending`, `MainWindow.xaml.cs:291`): первым делом `_autosave.CleanupStale()` (чистка .tmp-остатков — находка №4 ревью); `startedWithoutFile` И `HasSnapshot()` → диалог с меткой времени `dd.MM.yyyy HH:mm` (S11): Да → `RestoreFromAutosnapshotAsync`; Нет → `DeleteSnapshot`. `try/catch` + `AppLog.Warn` — сбой не роняет старт |
| `App.OnStartup` | После `splash.CloseAfterDelayAsync`, перед `ShowWhatsNewIfPending` (`App.xaml.cs:90-95`): `await mainWindow.ShowAutosaveRestorePromptAsync(startedWithoutFile: string.IsNullOrEmpty(startupProjectPath));` — признак берётся у источника, не у `InitialProjectPath` (обнуляется в `LoadInitialProjectAsync`, `MainWindow.xaml.cs:221`) |

## 4. Тесты

| Файл | Тесты |
|---|---|
| `tests/.../Project/ProjectAutosaveServiceTests.cs` (новый) | Песочница `SNOWCALC_SETTINGS_DIR`: HasSnapshot false→true после создания файла; SnapshotTimestamp отдаёт mtime; DeleteSnapshot удаляет .smc и .bak, отсутствие — не ошибка; CleanupStale убирает .tmp и не трогает .smc; ShouldSnapshot — матрица (dirty-only, load/calculating гасят) |
| `tests/.../Results/` (восстановление, при наличии прецедента конструирования ResultsViewModel в тестах — сверить при реализации) | RestoreFromAutosnapshotAsync: файл битый → результат неуспеха + снапшот удалён; успешный → данные применены, `CurrentFilePath` остался null, MRU не пополнился (пин «не содержит»), статус восстановительного текста |
| существующие гейты | Полный `dotnet test`; `ArchitectureRulesTests` (новый сервис — R4-скан автоматически); `SilentCatchScanTests` (каждый catch — журналирование); `ManualVersionGateTests` — после пересборки инструкции |

Интеграционная цепочка (тик → файл → промпт → restore) — ручной smoke на
handover: юнит-тесты покрывают политику и восстановление, DispatcherTimer —
UI-объект вне CI (прецедент UiSmoke).

## 5. Инструкция пользователя

`docs/manual/src/template.html`: раздел про автосохранение — когда пишется
(раз в ~2 мин при несохранённых изменениях), где лежит (служебный файл
рядом с настройками, не .smc-документ пользователя), что при старте после
сбоя (вопрос «Восстановить?»; «Нет» — копия удаляется), при штатном
закрытии копия гасится, восстановленный проект не привязан к файлу —
«Сохранить» предложит выбрать путь. Пересборка `python docs/manual/build_manual.py`.

## 6. Совместимость и риски

- **`.smc`: не меняется** (тот же `IProjectSaveService`, wire v1.1);
  settings.json: ключей нет; канон: только чтение.
- Риск «восстановили отвергнутое»: закрыт S7 (гашение при любом штатном
  закрытии).
- Риск «восстановили и тихо потеряли при закрытии»: закрыт S5 (restore →
  dirty: промпт закрытия спросит «Сохранить?»).
- Риск гонки тика с загрузкой/расчётом: закрыт S8 (+флаг повторного входа
  под try/finally).
- Риск битого снапшота (крах во время записи): атомарная запись
  tmp→move из `ProjectFileService` + честная деградация restore (ошибка →
  удалить, продолжить без восстановления); .tmp-остатки чистятся при
  старте (`CleanupStale`).
- Риск MRU/пути: закрыт S5 (restore без привязки пути и MRU).
- Риск «старт с файлом из проводника затирается промптом»: закрыт S6
  (признак `startedWithoutFile` берётся в `App.OnStartup` у источника
  аргументов, а не у обнуляемого `InitialProjectPath`).
- R-инварианты: R1–R6 не задеты; ADR не требуется — владение состоянием
  не меняется (автосейв читает канон и пишет файл, новые writers не
  вводятся; `MarkDirty` после restore — санкционированный писатель WI-5).
- Ручной smoke (на handover): поработать 3 мин → проверить появление
  autosave.smc; закрыть штатно → файл исчез; убить процесс с dirty →
  старт → вопрос → «Да» → данные восстановлены (проект dirty),
  «Сохранить» открывает SaveAs, закрытие без сохранения спрашивает
  подтверждение; «Нет» → файл удалён; битый файл → ошибка и продолжение;
  старт с .smc из проводника при живом снапшоте → промпта нет, файл
  не тронут.

## 7. Процесс

1. Атакующее ревью плана (subagent, вектора A1–A8, урок №26) → чек
   `docs/reviews/2026-09-21-autosave-plan-review.md`,
   `REVIEW_ID: R-2026-09-21-04`.
2. show-me регенерируется из плана (урок №27); сверка — шаг ревью (A6).
3. Реализация §1–§3, тесты §4 — сигнал владельца дан («не жди моего
   согласования», 2026-09-21); рекомендации §0.1 — дефолт-решения.
4. Полный зелёный `dotnet test`; пересборка инструкции (§5).
5. Релиз в том же цикле (решение владельца, 2026-09-21: «коммит, пуш,
   заливка инсталла на гугл-диск»): бамп 1.10.0 (`bump-version.ps1`),
   CHANGELOG, запись `WhatsNewCatalog` для 1.10.0, пересборка инструкции,
   коммиты (документы отдельно от кода — практика очереди 1), push →
   CI → автопилот `release.yml` → сетап на Drive → тег v1.10.0.
   Шаг владельца U8 канала обновлений остаётся за ним (локальный rclone):
   до его выполнения «Проверить обновления» честно сообщает «канал не
   настроен» (CI-warning напомнит).
