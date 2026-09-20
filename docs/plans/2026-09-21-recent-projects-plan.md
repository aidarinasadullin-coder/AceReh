# План: «Файл → Недавние проекты» (MRU) + drag-drop .smc — пункт 1.2 роадмапа post-1.8

> Дата: 2026-09-21. Статус: ревью пройдено (см. ниже), сигнал владельца
> «запуск план на исполнение» получен 2026-09-21, реализация §1–§7 и §6
> выполнена в тот же день; полный `dotnet test` 2343/2338→2343 зелёный;
> дерево незакоммичено (handover). Основание: роадмап
> [2026-09-20-post-1.8-roadmap.md](2026-09-20-post-1.8-roadmap.md) п. 1.2,
> [Идеи.md](../Идеи.md) №6 (одобрено владельцем 2026-09-20).
> show-me: [show-me-recent-projects.html](show-me-recent-projects.html).
> Независимое атакующее ревью пройдено 2026-09-21 — APPROVE-WITH-EDITS,
> 7 находок (2×P2, 5×P3), все приняты, правки внесены (чек
> [2026-09-21-recent-projects-plan-review.md](../reviews/2026-09-21-recent-projects-plan-review.md)).
> Владелец запустил план командой «пиши план» без ответов на детальные
> вопросы — рекомендации плана вписаны в §0.1 как дефолт-решения и могут
> быть переопределены владельцем при ревью (практика plan-approval).

## 0. Суть

Два новых входа поверх существующего открытия проекта:

1. **Подменю «Файл → Недавние проекты»** — до 10 путей, хранение в
   AppSettings (`settings.json`), отсутствующие на диске файлы скрываются
   при открытии подменю.
2. **Drag-drop** — перетаскивание одного файла `.smc` на главное окно.

Оба входа ведут в существующий
`ResultsViewModel.LoadProjectFromPathAsync(filePath)`
(`src/ViewModels/Results/ResultsViewModel.cs:933`) — уже единую точку
открытия по пути: её используют Ctrl+O (через `OpenProject`), двойной клик
по `.smc` в проводнике (`InitialProjectPath` →
`MainWindow.xaml.cs:202`) — теперь и drag-drop, и MRU. Подтверждение при
несохранённых изменениях, сброс модулей, `MarkClean` — всё уже внутри;
новая логика подтверждений не пишется.

**State ownership без изменений.** Владельцы состояния не меняются,
`.smc` wire не меняется, канон-писатели не расширяются (MRU — состояние
оболочки в AppSettings, вне `ProjectSession`), undo-дневник не задет
(открытие проекта уже стирает историю — существующее поведение
`ApplyLoadedProjectAsync`, ADR-014).

### 0.1. Решения-дефолты (рекомендации агента; владелец может переопределить при ревью)

| # | Вопрос | Решение (дефолт) | Альтернатива (выбор за владельцем) |
|---|---|---|---|
| D1 | Позиция подменю | Сразу после «Открыть...», до первого Separator | Внизу меню перед «Выход» |
| D2 | Когда путь попадает в MRU | После **успешной** загрузки проекта — одна точка `ApplyLoadedProjectAsync`; покрывает диалог, drag-drop и ассоциацию | При выборе файла в диалоге (до загрузки) — попадали бы битые/отменённые |
| D3 | Зона drag-drop | Всё главное окно, один файл `.smc` | Только шапка/зона welcome |
| D4 | Фидбек при перетаскивании | Полупрозрачный оверлей «Отпустите файл проекта, чтобы открыть» с гардом `DragLeave` по границам окна (без фликера на дочерних элементах) | Только курсор-иконка — выглядит как no-op |
| D5 | Пустой список | Подменю видно всегда, внутри disabled-заглушка «Нет недавних проектов» | Скрывать подменю целиком — меняется геометрия меню |
| D6 | Очистка списка | Пункт «Очистить список» внизу подменю, disabled при пустом списке | Без очистки — список живёт вечно |
| D7 | Ёмкость и дедуп | 10 путей (роадмап «до ~10»); дедуп по нормализованному пути без учёта регистра (Windows FS); свежий путь — наверх | 5 путей; чувствительный к регистру дедуп (ломается на `D:\` vs `d:\`) |
| D8 | Формат записи | Только путь (`List<string>`), имя файла и tooltip выводятся из пути при построении меню | Объект {путь, дата} — расширение формата ключа без потребности |
| D9 | Drag нескольких файлов | Берётся первый `.smc` из списка, остальные игнорируются | Блокировать перетаскивание нескольких файлов |

## 1. Схема ключей AppSettings — фиксация для очереди 1 (условие роадмапа)

Роадмап: «схема ключей AppSettings фиксируется здесь и переиспользуется
в 1.3 и 3.1». Фиксируется:

- **Новое свойство** `AppSettings`:
  `public List<string> RecentProjects { get; set; } = new();`
  JSON-массив путей, порядок newest-first.
- **Конвенция для последующих пунктов очереди** (1.3 «проверка обновлений
  + Что нового», 3.1 автосохранение): каждый пункт добавляет **свои**
  свойства в `AppSettings` без переименования и смены семантики чужих;
  отсутствие ключа в `settings.json` = дефолт (стандартное поведение
  `JsonSerializer.Deserialize` — существующее поле не перезаписывается);
  миграций нет; сериализация — существующий `Save()`
  (`WriteIndented`, `src/Services/AppSettings.cs:66`).
- Ожидаемые имена будущих ключей (фиксируются окончательно планами 1.3/3.1):
  1.3 — `WhatsNewShownVersion` (`string?`); 3.1 — префикс `Autosave*`.
- **null-гвард**: при повреждённом `settings.json`
  (`"RecentProjects": null`) десериализатор запишет `null` в свойство —
  сервис читает список через `GetListSafe()` (`Instance.RecentProjects ??
  new List<string>()`), запись в список идёт только через этот хелпер.
  При чтении null заменяется пустым списком, тест-пин в §7.

Ниже — `settings.json` после использования (реальные сущности ключа):

```json
{
  "IsSidebarCollapsed": false,
  "RecentProjects": [
    "D:\\Проекты\\Парковка ТЦ Москва.smc",
    "D:\\Проекты\\Площадь у вокзала.smc"
  ]
}
```

## 2. RecentProjectsService — новый сервис (R4-чистый)

| Файл | Правка |
|---|---|
| `src/Services/RecentProjects/IRecentProjectsService.cs` (новый) | `IReadOnlyList<string> GetRecent();` `void Add(string filePath);` `void Clear();` |
| `src/Services/RecentProjects/RecentProjectsService.cs` (новый) | Реализация без параметров ctor. Состояние — `AppSettings.Instance` (паттерн `MainViewModel.cs:159-160`: сервисы и VM пишут singleton напрямую, DI-контейнера в репо нет) |

Логика:

- `const int MaxEntries = 10` (D7).
- Нормализация пути: `filePath?.Trim()`, затем `Path.GetFullPath` —
  сравнение дедупа `StringComparer.OrdinalIgnoreCase` (D7). Битые входы
  (null/пустая/некорректная строка) молча игнорируются с `AppLog.Warn` —
  MRU — необязательная удобная фича, ошибка добавления не должна ронять
  открытие проекта.
- `Add`: нормализовать → удалить прежнюю позицию (дедуп) → вставить в
  начало → обрезать до `MaxEntries` → `AppSettings.Instance.Save()`.
  `Save()` синхронный, файл — сотни байт: приемлемо на UI-потоке
  (прецедент — `MainViewModel` пишет `Save()` при сворачивании сайдбара).
- `GetRecent`: `GetListSafe().Where(File.Exists)` — «отсутствующие
  скрываются» (роадмап, Идеи №6); вызывается только при открытии
  подменю. Осознанный риск: недоступный сетевой путь может дать задержку
  `File.Exists` — максимум 10 записей, альтернатива «показывать всё,
  disabled» отклонена (D5).
- `Clear`: очистка списка + `Save()`.
- `static bool IsProjectFile(string? path)`: расширение `.smc`,
  `OrdinalIgnoreCase` — общий фильтр для drag-drop (§5).
- R1–R6: `ProjectSession` не трогается (`MarkDirty` не вызывается),
  зависимостей от VM нет (R4), writers-канон не расширяется, `.smc` не
  пишется.

## 3. ResultsViewModel — одна точка MRU-добавления

| Файл | Правка |
|---|---|
| `src/ViewModels/Results/ResultsViewModel.cs` | Поле `private readonly IRecentProjectsService _recentProjects = new RecentProjectsService();` (без параметра ctor — минимум ripple; паттерн дефолта `ResultsViewModel.cs:597` `_excelExportService`) |
| там же, `ApplyLoadedProjectAsync` | После финального `_projectSession.MarkClean()` (строка **979**; в методе два вызова `MarkClean` — 975 и 979; вставка после второго) и до `StatusMessage` (981): `_recentProjects.Add(filePath);`. Ранние выходы метода (ошибка загрузки, отказ в подтверждении dirty) MRU не пополняют (D2) |

Покрытие всех сценариев одной вставкой: Ctrl+O/«Открыть...», drag-drop
(§5), ассоциация файлов при старте (`MainWindow.xaml.cs:202`) и клик по
подменю недавних (§4) — все идут через `LoadProjectFromPathAsync` →
`ApplyLoadedProjectAsync`.

## 4. MainWindow — подменю «Недавние проекты»

| Файл | Правка |
|---|---|
| `src/MainWindow.xaml` (меню «Файл», строки 192–194) | После `MenuItem "Открыть..."`: `<MenuItem x:Name="RecentProjectsMenuItem" Header="Недавние проекты" SubmenuOpened="RecentProjectsMenuItem_SubmenuOpened" AutomationProperties.AutomationId="RecentProjectsMenuItem"/>` (D1). Без ItemsSource в XAML — наполнение в code-behind при открытии |
| `src/MainWindow.xaml.cs` | Обработчик `RecentProjectsMenuItem_SubmenuOpened(object, RoutedEventArgs)`: очистить `Items`; `var paths = new RecentProjectsService().GetRecent();` — сервис stateless-фасад над `AppSettings.Instance`, экземпляр в code-behind независим от экземпляра VM; 0 путей → disabled `MenuItem` «Нет недавних проектов» (D5); иначе — пункт на путь: `Header = Path.GetFileName(path)`, `ToolTip = path`, `Click = RecentProjectClick` (`async void`, `await _viewModel.ResultsViewModel.LoadProjectFromPathAsync(path)`, try/catch + `AppLog.Warn` + диалог ошибки — паттерн `LoadInitialProjectAsync`, `MainWindow.xaml.cs:193-210`); внизу `Separator` + `MenuItem` «Очистить список» (`AutomationId="ClearRecentProjectsMenuItem"`, disabled при 0 путей, `Click` → `Clear()`) |

Race «файл исчез после построения меню»: `LoadProjectFromPathAsync`
покажет существующий диалог «Не удалось открыть проект» — поведение
зафиксировано, отдельная обработка не пишется.

## 5. MainWindow — drag-drop .smc

| Файл | Правка |
|---|---|
| `src/MainWindow.xaml` (корень `Window`) | `AllowDrop="True"`, хендлеры `DragOver`/`DragLeave`/`Drop`; в корневой `Grid` последним ребёнком — оверлей: `Border` `x:Name="DragDropOverlay"` — `Background="{DynamicResource Brand.Black.Brush}"` + `Opacity="0.55"` (литеральный HEX в `MainWindow.xaml` запрещён: гейт `ViewTokenHygieneTests` сканирует файл, ratchet-allowlist `(0, 0)` — любой новый хардкод красит тест, находка ревью №1), `CornerRadius="8"`, `IsHitTestVisible="False"`, `Visibility="Hidden"`; внутри белая карточка (красная рамка `BorderThickness="2"` + `BorderBrush="{DynamicResource Brand.Red.Brush}"`, токены `Tokens.Colors.xaml:69/:71`; WPF `Border` не поддерживает пунктир — сплошная рамка, синхронизировано с show-me): заголовок «Отпустите файл проекта, чтобы открыть» (`{DynamicResource Font.Size.Section}`) + подсказка «Формат .smc · один файл» (`{DynamicResource Font.Size.Caption}` — токены `Tokens.Typography.xaml:20-21`) — канонический текст оверлея (D4, находка ревью №4) |
| `src/MainWindow.xaml.cs` | `MainWindow_DragOver`: `e.Data.GetDataPresent(DataFormats.FileDrop)` → файлы; ровно 1 файл и `RecentProjectsService.IsProjectFile` → `e.Effects = DragDropEffects.Copy`, оверлей `Visible`; иначе `e.Effects = DragDropEffects.None`; `e.Handled = true` всегда. `MainWindow_DragLeave`: скрыть оверлей только если курсор за границами окна (`GetPosition(this)` вне `0..ActualWidth/ActualHeight`) — гвард против фликера при проходе над дочерними элементами (D4). `MainWindow_Drop`: оверлей `Hidden`; из `FileDrop` взять первый `.smc` (D9) → `await _viewModel.ResultsViewModel.LoadProjectFromPathAsync(path)` (try/catch + `AppLog.Warn` — паттерн §4); не-.smc/мультифайл без .smc — молча ничего |

Примечания: `DragMove`-заголовок окна (CaptionHeight=0, кастомная шапка)
не задет — OLE-драг файлов не двигает окно; `SplashWindow` drag не
получает; подтверждение dirty показывается уже из
`LoadProjectFromPathAsync` — нового UI не пишется.

## 6. Инструкция пользователя

| Файл | Правка |
|---|---|
| `docs/manual/src/template.html` | Раздел меню «Файл»: абзац про «Недавние проекты» (подменю после «Открыть...», до 10 путей, отсутствующие скрываются, «Очистить список») + абзац про drag-drop .smc на окно; скриншот не добавляется — текст достаточен, открытого меню «Файл» в эталонах `src/shots/` нет |
| пересборка | `python docs/manual/build_manual.py` после правок (UI-правка → обязательная пересборка, гейт `ManualVersionGateTests`); версия подставится из csproj |

## 7. Тесты

| Файл | Тесты |
|---|---|
| `tests/SnowMeltingCalculator.Tests/Services/RecentProjects/RecentProjectsServiceTests.cs` (новый) | `SetUp`/`TearDown` — `Fixtures.ResetAppSettingsHelper.Reset()` (паттерн `AppSettingsTests.cs:13-26`). **8 тест-кейсов:** (1) Add вставляет первым и сохраняет в `settings.json`; (2) повторный Add того же пути — без дубля, перемещение наверх; (3) дедуп без учёта регистра (`D:\X\A.smc` vs `d:\x\a.smc`); (4) cap 10 — 11-й вытесняет хвост; (5) GetRecent скрывает отсутствующий файл (temp-файл: создать → Add → удалить → не в списке); (6) Clear опустошает и сохраняет; (7) `IsProjectFile` — `[TestCase]`: `.smc`/`.SMC`/не-.smc/null; (8) null-гвард — JSON `{"RecentProjects": null}` → Add/GetRecent работают без NRE |
| `tests/SnowMeltingCalculator.Tests/Services/AppSettingsTests.cs` | Пин `Save_PersistsRecentProjects` (паттерн `Save_PersistsIsSidebarCollapsed:64-81`): путь переживает сброс singleton + перечитывание. Assertion — **«содержит», не равенство списка**: sandbox `settings.json` общий на процесс, другие фикстуры (например, `UndoRedoServiceJournalTests.cs:145` грузит проект через VM) теперь тоже пишут MRU — находка ревью №7 |
| существующие гейты | `dotnet test` полный — ArchitectureRulesTests (writers не расширялись), ViewTokenHygieneTests (оверлей §5 на токенах, allowlist не правится), ManualVersionGateTests (после §6) — без правок ожиданий: инварианты не задеты |

VM-слой (`ApplyLoadedProjectAsync` — приватный, сборка `ResultsViewModel`
тяжёлая): логика MRU полностью в сервисе (§2), VM-вставка — одна строка
без ветвлений; проверяется ручным smoke-сценарием (§8) без нового
интеграционного теста.

## 8. Совместимость и риски

- **`.smc`: не меняется.** `settings.json`: назад-совместимо — новый ключ,
  отсутствие = дефолт; старые версии приложения новый ключ игнорируют.
- **«Сохранить как...» в новый путь MRU не пополняет** (находка ревью
  №5): `SaveProjectAs` (`ResultsViewModel.cs:908-911`) идёт мимо
  `ApplyLoadedProjectAsync`. Осознанное ограничение v1 — MRU трактуется
  как «недавно открытые»; добавить «недавно сохранённые» — решение
  владельца, может быть подано при ревью плана как правка D2.
- Сетевые/недоступные пути в `File.Exists` — задержка при открытии
  подменю; риск принят (≤10 записей, D5).
- Race «удалён между построением меню и кликом» — существующий диалог
  ошибки (§4).
- MRU хранит пути вне контроля версий — синхронизация между машинами не
  предполагается (локальные настройки, как и сайдбар).
- R-инварианты: R1–R6 не задеты (§2/§3); ADR не требуется (state
  ownership, wire, writers — без изменений).
- Ручной smoke (на handover, вне CI): перетащить .smc на окно (оверлей,
  открытие, dirty-подтверждение); открыть из подменю; список
  переживает рестарт; «Очистить список»; битый файл — диалог ошибки.

## 9. Процесс

1. Независимое атакующее ревью плана (subagent, вектора A1–A8, урок
   №26) → чек `docs/reviews/2026-09-21-recent-projects-plan-review.md`.
2. show-me регенерируется из этого плана (урок №27); сверка артефакта
   с планом — шаг ревью (вектор A6).
3. Реализация §1–§6, тесты §7 — **после сигнала владельца**.
4. Полный зелёный `dotnet test`; handover — незакоммиченное дерево.
5. Коммит — по решению владельца (документация плана/ревью отдельно от
   кода — порядок очереди 1).
