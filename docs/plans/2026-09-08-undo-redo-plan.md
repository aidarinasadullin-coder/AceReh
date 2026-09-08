# План: «Отменить / Вернуть» (undo/redo, 10 действий)

> Дата: 2026-09-08. Статус: план + глубокое независимое ревью (3 P0, 5 P1, 5 P2 —
> все внесены в план, см. §11). Готов к реализации после owner-signal «делай».
> Коммиты — только по явной команде владельца.

---

## 1. Зафиксированные решения владельца

| # | Решение | Значение |
|---|---|---|
| 1 | Семантика | Отмена правок данных, Word-стиль (не навигация по экранам) |
| 2 | Механика | Вариант Г: событийный memento-дневник по разделам |
| 3 | Глубина | 10 действий (константа; старые вытесняются) |
| 4 | UI | 2 кнопки в шапке слева от меню «Файл» + Ctrl+Z / Ctrl+Y |
| 5 | Результаты при откате | Восстанавливаются «как было» из снимков (без принудительного пересчёта) |
| 6 | Охват | Правки 4 разделов. Карточка проекта (номер/объект) — **в v1 неактивна**: UI-редактирование удалено с Ф6 (`MainWindow.xaml:126-138` — read-only), user-мутаций не существует; вернётся вместе с возвратом редактирования (ревью P1-1) |
| 7 | Не входит в отмену | Импорт материала в общий каталог, поиск городов, экспорт PDF, легаси `construction_*.json`, `ToggleMode` (`IProjectDisplayModeState` — не слайс) |
| 8 | Стирают дневник | Открытие проекта, «Новый расчёт», старт с файлом `.smc` |
| 9 | Сохранение | Дневник НЕ стирает; запоминает «точку чистоты» (позицию в дневнике) |
| 10 | Заставка welcome | Кнопки скрыты |
| 11 | Точность | Запись = все разделы, затронутые действием (выбор города → климат+тепло+гидравлика; правка длины контура → только гидравлика) |

## 2. Механика (вариант Г) — суть

- Разделы ProjectSession и так публикуют `Changed` с `Origin + Before/After`
  (INV-016, MutationBoundaryConsolidationTests). Дневник — слушатель этих
  событий, снимки не создаёт сам.
- **Запись** = одно действие пользователя: имя + per-slice пары (Before, After)
  всех затронутых разделов.
- **Группировка:**
  - user-origin (`User`, `UserReset`, `Template`) открывает/пополняет активную
    запись; каждая новая user-мутация форс-закрывает предыдущую группу;
  - `*Invalidation` (`ClimateInvalidation`, `ConstructionInvalidation`) и
    `Calculation` **при активной записи — примыкают к ней** (их Before/After —
    часть действия: гашение/возврат результатов);
  - `Calculation` **вне активной записи** — открывает отдельную запись «Расчёт»
    (шапочная «Рассчитать»); Before теплового слайса — от `BeginCalculation`-
    мутации; такая запись живёт **до первой user-мутации или очистки** (таймер
    тишины её не закрывает — ревью P1-5: иначе медленный расчёт развалится на
    две записи);
  - lifecycle-origin (`Load`, `ProjectLoad`, `ProjectLoadReset`, `Reset`,
    `Initialization`, `SystemApply`) — не пишутся.
- **Правило тотального подавления (ревью P0-1):** события слайсов при
  `session.IsLoadProjectInProgress == true` игнорируются дневником полностью —
  независимо от origin. Загрузка всегда идёт под `BeginProjectRestore`
  (`ResultsViewModel.cs:1525`), а `Begin/Complete/FailCalculation` публикуют
  origin `Calculation` жёстко (`ProjectSessionThermalState.cs:92,116,135`) —
  fallback-расчёт при загрузке (`CalculateFromRestoreAsync`) иначе создал бы
  фантомную запись и dirty и уронил `Baseline_Load*` /
  `LoadResetRestoreAndSystemApply` (`DirtyRaised == 0`).
- **Склейка** соседних user-мутаций одного действия (посимвольный ввод) — окно
  тишины 400 мс; таймер закрывает **только user-группы**.
- **Откат:** из записи берётся Before каждого затронутого раздела и применяется
  каноническим методом с origin `Undo`; «Вернуть» — симметрично After с `Redo`.
  Во время применения — guard-скоуп (§4.3) + фильтр `Undo/Redo` origin в
  дневнике (подавление эха, двойная защита).
- **Гейт на расчёт (ревью P1-3):** `CanUndo/CanRedo = false` при
  `ThermalIsCalculating || HydraulicsIsCalculating` (`ICalculationStateService`,
  `CalculationStateService.cs:76,110`) — иначе Ctrl+Z посреди `Task.Run`
  перезаписывается последующим `CompleteCalculation`.
- **Dirty по точке чистоты:** `Save` фиксирует позицию в дневнике; после каждой
  мутации (в т.ч. Undo/Redo) `IsDirty = позиция ≠ точка чистоты`; вытеснение
  точки чистоты за предел 10 → `clean = −1` (честно «навсегда изменён» до
  следующего сохранения).

## 3. Потоки данных

```
Правка:  UI → слайс.XxxApply(User) → Changed{Origin, Before, After}
         → дневник пополняет активную запись (Invalidation/Calculation примыкают)
Откат:   дневник → BeginProjectRestore() →
         Climate.ApplySnapshot(Before, Undo) → Construction.ApplySnapshot →
         ThermalStateCoordinator.RestoreState(Before, Undo) →
         Hydraulics.Restore(Before, Undo) →
         Hydraulics.Restore(Before, Undo) повторно СРАЗУ (каскад
         ContextChanged→CalculateAllCollectors синхронен — ревью P2-5;
         трюк ProjectLoadOrchestrator.cs:226-227)
         → MarkDirty/MarkClean по позиции → HistoryChanged (кнопки)
Экраны:  обновляются штатно через Changed/зеркала (§5.3)
```

## 4. Новые канонические сущности (state ownership — через ADR)

### 4.1 Новые origins (4 enum-файла)

`Undo`, `Redo` в `ClimateMutationOrigin`, `ConstructionMutationOrigin`,
`ThermalMutationOrigin`, `HydraulicsMutationOrigin`. Семантика как у
`ProjectLoad`: dirty внутри слайса — НЕТ (список dirty-origins не расширяется),
публикация `UpdateXxx` в контекст — да; `DataChanged` проекции климата — НЕТ
(иначе инвалидация тепла уничтожит возвращённый результат; прецедент
`PublishesCompatibility(Load) == false`). **Асимметрия, зафиксировать в ADR
(ревью P2-3):** `PublishesDownstream` конструкции на `Undo/Redo` НЕ расширяется
(иначе `RaiseDataChanged` → инвалидация тепла во время отката конструкции);
климат контекст публикует при любом changed-origin безусловно
(`ProjectSessionClimateState.cs:246-250`) — этого достаточно.

### 4.2 Новые/расширяемые методы записи

| Слайс | Метод | Статус |
|---|---|---|
| Climate | `ApplySnapshot(ClimateStateSnapshot, origin)` — **прямое присваивание всех 12 полей снимка** (record несёт `HasUserModifications`/`Period0Days`), без нормализации и без origin-пересчётов (ревью P2-2: «Load-ветка» наоборот воспроизводит потерю этих полей) | **новый** |
| Construction | `ApplySnapshot(..., origin)` | **есть** — разрешить `Undo/Redo` |
| Thermal | **новый метод слайса** `IProjectSessionThermalState.RestoreState(ThermalStateSnapshot, origin)` — атомарно inputs+result+**статус из снимка** (может быть `NeedsRecalculation` с сообщением; НЕ `Default`; ревью P2-4); координатор получает обёртку `ThermalStateCoordinator.RestoreState(...)` с пере-публикацией `UpdateThermalInputs`/`UpdateThermal` (по образцу `LoadResult`); существующий `Restore` (`ProjectLoad`, `ThermalStatusSnapshot.Default`) не трогается | **новый** |
| Hydraulics | `Restore(HydraulicsStateSnapshot, origin)` — ослабить guard: разрешить `Undo/Redo` (сейчас только `ProjectLoad`, `ProjectSessionHydraulicsState.cs:142`) | **есть, guard** |

Порядок применения при Undo/Redo: Climate → Construction → Thermal →
Hydraulics → **повторная** Hydraulics.Restore сразу после Thermal (каскад
`ContextChanged → CalculateAllCollectors → RunCalculation` синхронен
— ревью P2-5; «ожидание затихания» не нужно, в отличие от асинхронного
теплового расчёта; бейджи `SetHydraulicsCalculating` мигнут внутри отката —
конечное состояние честное).

### 4.3 Guard-скоуп отката — переиспользование `BeginProjectRestore()`

Откат выполняется под существующим `using var _ = _projectSession.BeginProjectRestore()`:
все VM-гварды (`IsLoadProjectInProgress`) уже глушат пользовательские
присвоения и dirty (`ThermalViewModel.cs:110-168`, `CircuitsViewModel.cs:1480`,
`ResultsViewModel.cs:70-92`), welcome не трогается; lease реентерабелен
(`ProjectSession.cs:109-204`). Подтверждено ревью. Альтернатива (отдельный
флаг `IsUndoRedoInProgress`) отклонена: потребовала бы правки каждого гварда.
Семантическое расширение имени («load-or-undo in progress») — фиксируется в ADR.

## 5. Компоненты

### 5.1 `src/Services/History/IUndoRedoService.cs` + `UndoRedoService.cs` (новый, синглтон)

```
CanUndo / CanRedo : bool        // = стек не пуст И не идёт расчёт (P1-3)
UndoDescription / RedoDescription : string?   // «Выбор города», «Изменение контуров»
Undo() / Redo()
SetCleanPoint()                                // из Save
Clear()                                        // из Открыть/Новый расчёт
event HistoryChanged                           // кнопки/тултипы
```

- ctor: подписки на `Changed` 4 слайсов через `IProjectSession` (4 подписки,
  синглтон, без отписок — house style). **Подписки на
  `IProjectSession.PropertyChanged` НЕТ** (ревью P1-1: ломает цензус
  `Session.PropertyChanged = 1` и ловит lifecycle-присвоения; карточка проекта
  не в v1 — §1.6). DI-регистрация в `ServiceCollectionExtensions` (после
  сессии и координаторов).
- Запись: `HistoryEntry { Name, Dictionary<SliceKind, (Before, After)> }`.
- Стеки: `_undo` (List), `_redo` (List); push → `flushRedo`; лимит 10
  с вытеснением старейшей записи; вытеснение clean-записи → `clean = −1`.
- **Подавление (три линии):** (1) `IsLoadProjectInProgress == true` → игнор
  всего (P0-1); (2) `origin ∈ {Undo, Redo}` → игнор; (3) флаг `_isApplying`
  вокруг всего отката.
- Имена действий: реестр diff-правил по изменённым полям снимков
  (климат `SelectedCity` → «Выбор города», прочие поля климата → «Изменение
  климатических данных»; Construction слои → «Изменение слоёв конструкции»,
  УГВ → «Смена уровня грунтовых вод», шаблон → «Применение шаблона»; Thermal
  входы → «Изменение тепловых входов»; Hydraulics коллекция → «Изменение
  коллекторов/контуров», глобальные входы → «Изменение общих входов»;
  Calculation-запись → «Расчёт»). Fallback: «Изменение данных: <раздел>».
- Dirty-коррекция после каждой записанной мутации и после Undo/Redo:
  `позиция == clean → MarkClean()`, иначе `MarkDirty()` (дневник — новый
  sanctioned caller WI-5/WI-6; слайсы при `Undo/Redo`-origin MarkDirty не ставят).
- Тестируемость времени (ревью P2-5): таймер тишины — через инъекцию
  `Func<Timer>`/виртуальных часов либо внутренний `FlushPendingForTests()`;
  юнит-тесты склейки не зависят от реального Dispatcher.

### 5.2 Точки очистки/фиксации

| Точка | Действие |
|---|---|
| `ResultsViewModel.ApplyLoadedProjectAsync` (перед загрузкой; покрывает «Открыть» и старт с файлом — `MainWindow.xaml.cs:176` → `:824→832→844→852`) | `Clear()` |
| `MainViewModel.PerformNewCalculationReset` | origin гидравлики `UserReset` → **`ProjectLoadReset`** (однострочно, консистентно с климатом/теплом `:366/:372`; ревью P0-3) + `Clear()` — **строго последний оператор** |
| `ResultsViewModel.SaveToFile` (после `MarkClean:1022`) | `SetCleanPoint()` |

### 5.3 Зеркала экранов (все — через штатные сигналы, без поллинга)

| VM | Изменение |
|---|---|
| ClimateViewModel | **0 строк** — `Changed → MirrorSnapshot` зеркалит любую origin |
| ConstructionViewModel | `OnConstructionStateChanged` (сейчас пустой): `origin ∈ {Undo,Redo} → ApplyLifecycleSnapshotToAdapter(e.After)` — ~5 строк |
| CircuitsViewModel | `OnHydraulicsStateChanged`: добавить `Undo/Redo` к существующей ветке `ProjectLoad` → `ApplyLifecycleSnapshotToAdapter` — ~2 строки |
| ThermalViewModel | `OnCoordinatorCompletion`: при `origin ∈ {Undo,Redo}` → новый `ApplyStateSnapshotToAdapter(ThermalStateSnapshot)` — inputs + `Result` (статусные `RecalcMessage/NeedsRecalculation` транслируются через `CalculationStateService` сами; ревью P2-5) — ~40–60 строк |
| ResultsViewModel | `OnCanonicalStateChanged`: `origin ∈ {Undo,Redo}` → `RefreshAll()` (иначе данные на открытой вкладке «Результаты» останутся stale — ревью P2-5) — ~5 строк |

## 6. UI

- `MainWindow.xaml`, шапка: колонка меню (`Grid.Column=5`, 9 колонок) →
  `StackPanel` горизонтально: [⟲ Отменить] [⟳ Вернуть] + `Menu «Файл»`
  (индексы колонок не сдвигаются; `MainWindowChromeLayoutTests` проверяет
  только стиль корневого Grid — подтверждено ревью). Стили `Button.Secondary`,
  глифы `Path` из `Icons.Fluent` (литералы FontSize/HEX запрещены
  `ViewTokenHygieneTests`). Тултипы: «Отменить: {UndoDescription}» /
  «Вернуть: {RedoDescription}»; `IsEnabled ← CanUndo/CanRedo` (учитывает гейт
  расчёта, §2); `Visibility` — скрыты при `IsWelcomeVisible`.
- `AutomationProperties.AutomationId`: `ShellUndoButton`, `ShellRedoButton`
  (уникальны для `ThermalAutomationIdSelectorContractTests` — подтверждено).
- **Хоткеи через `PreviewKeyDown`, не `KeyDown`** (ревью P1-4: bubbling
  `KeyDown` перехватывается `TextBoxBase` — а почти все цели отката из
  текстбоксов). Политика (записать в ADR): глобальный undo/redo перекрывает
  посимвольный undo текстбокса; `Ctrl+Z`/`Ctrl+Y` в `MainWindow` PreviewKeyDown.

## 7. Этапы

| Этап | Содержание | Тесты-чекпоинты | Оценка |
|---|---|---|---|
| 0 | ADR-запись в `docs/architecture/README.md` (+ диаграмма `project-session.md` в том же коммите; политика Ctrl+Z vs TextBox) | — | ~30 мин |
| 1 | Фундамент: 4 origins; `Climate.ApplySnapshot`; `IProjectSessionThermalState.RestoreState` + координаторская обёртка; `Hydraulics.Restore` guard; фикс origin `PerformNewCalculationReset` (P0-3) | unit: round-trip снимка побитово (вкл. `Period0Days`/`HasUserModifications`/статус); ровно одно `Changed`; dirty не ставится; `DataChanged` климата и `PublishesDownstream` конструкции не срабатывают; контекст пере-публикуется | ~220–280 + ~200 тест-строк |
| 2 | Дневник: сервис, группировка+Invalidation-примыкание, тотальное подавление, лимит, clean-позиция, эхо, гейт расчёта, тестируемое время | unit: «выбор города» = 1 запись/3 слайса (климат+инвалидация тепла+гидравлика); склейка посимвольного ввода; «Рассчитать» = запись, живёт до user-мутации; событие под `IsLoadProjectInProgress` игнорируется (в т.ч. `CalculateFromRestoreAsync`); вытеснение clean; undo не пишет сам себя | ~280–330 + ~240 |
| 3 | Зеркала + интеграция: Construction/Circuits/Thermal/Results, очистки/точка чистоты, порядок применения с повторной гидравликой | интеграционные: откат города (тепло/гидравлика вернулись, статусы честные), откат контура (только гидравлика), undo→правка→redo гаснет, undo во время расчёта невозможен | ~90–120 + ~130 |
| 4 | UI: кнопки, PreviewKeyDown-хоткеи, тултипы | UiSmoke AutomationId, `MainWindowChromeLayoutTests`, `ViewTokenHygieneTests` | ~70 + smoke |
| 5 | Сдача: полный `dotnet test`, `MutationBoundaryConsolidationTests` + undo-сценарии, независимое read-only ревью диффа, handover | полный прогон зелёный; round-trip `.smc` не трогается | — |

Итого: ~660–800 строк кода, ~570 тест-строк, 2–3 сессии.

## 8. Влияние на инварианты и тесты

- **R1** — не задет (дневник не хранит канон; снимки — read-only records).
- **R2** — списки **и паттерны** writers расширяются через ADR-0xx (ревью P2-1:
  иначе новые писатели невидимы скан-тестам — «вакуумный» проход):
  WI-1: паттерн + `ApplySnapshot`, allowlist + `UndoRedoService.cs`;
  WI-3: паттерн + `RestoreState` (regex `Restore\(` не матчит `RestoreState\(`),
  allowlist + `UndoRedoService.cs` (вызов координатора);
  WI-4: паттерн + `Restore`, allowlist + `UndoRedoService.cs`;
  WI-5/WI-6: allowlist + `UndoRedoService.cs`. Dirty-origins слайсов не расширяются.
- **R3/WI-8** — не задет (дневник не VM).
- **R4** — дневник зависит только от `IProjectSession`, `IThermalStateCoordinator`;
  ViewModels не знает.
- **R5** — не задет (Results не пишет в чужие слайсы; собственный `RefreshAll`
  — read-only проекция).
- **R6** — `.smc` wire не меняется; дневник — память процесса.
- `ReactiveSubscriptionLifecycleTests` — **цензус обновляется** (ревью P1-2:
  цензус считает все обработчики, не только VM): 2/2/2/4 → 3/3/3/5, константы
  и `because`-строки; фиксстура `ReactiveGraph.CreateProductionShaped()`
  расширяется сервисом — один коммит. Проверить отсутствие пина
  `Session.PropertyChanged` (мы её не добавляем).
- `MutationBoundaryConsolidationTests`: новые origins вписываются в сценарий 7
  (lifecycle-подобные, 0 dirty — обеспечено подавлением P0-1); добавляются
  undo-сценарии.

## 9. Ревью-пункты для диффа (остаточные)

1. Точный состав публикаций `Hydraulics.Restore` при origin `Undo/Redo`: если
   слайс не публикует `UpdateHydraulics` — добавить публикацию в обвязку
   применения (иначе контекст гидравлики останется stale после отката без
   пересчёта).
2. Состав `ThermalStateCoordinator.RestoreState`: публикации строго как
   `LoadResult` (входы + результат), без `DataChanged`-проекций.
3. Открытые диалоги-редакторы (модальные): undo вне диалога — кнопки шапки
   недоступны (модальность), провер smoke-ом.
4. Поведение `CanUndo` при несохранённом dirty-проекте без записей дневника
   (после стирания) — кнопка погашена, звёздочка остаётся: сверить UX с Word.

## 10. Критерии приёмки

1. Отмена/возврат работают для правок 4 разделов; «Отменить: <имя>» в тултипе;
   края истории гасят кнопки; на заставке кнопок нет; во время расчёта гаснут.
2. Откат города возвращает климат+тепло (вкл. результат)+гидравлику одним
   действием; откат правки контура — только гидравлику; чужие статусы не
   дёргаются; открытая вкладка «Результаты» обновляется.
3. Сохранение ставит точку чистоты: откат ровно к сохранённому состоянию
   снимает «звёздочку»; вытеснение точки за 10 — честно оставляет «изменён».
4. Открытие проекта/«Новый расчёт»/старт с файлом стирают дневник (включая
   фантомные записи fallback-расчёта загрузки); сохранение — нет.
5. `dotnet test` полный зелёный; `ArchitectureRulesTests` обновлён по ADR
   (паттерны+allowlist); round-trip `.smc` зелёный; независимое read-only
   ревью диффа пройдено.
6. Handover: uncommitted собираемое дерево; запись «state ownership расширен
   санкционированными писателями через ADR-0xx».

## 11. История ревью плана (2026-09-08)

Независимое ревью (read-only, сверка с кодом): вердикт «требует правок» —
3 P0, 5 P1, 5 P2; все внесены:

- **P0-1** → §2 правило тотального подавления `IsLoadProjectInProgress`
  (фантомная запись «Расчёт» от fallback-расчёта загрузки; падение
  `Baseline_Load*`/сценария 7).
- **P0-2** → §2 `*Invalidation` примыкают к записи (иначе откат города не
  возвращал бы тепловой результат).
- **P0-3** → §5.2 origin `UserReset`→`ProjectLoadReset` в
  `PerformNewCalculationReset` + `Clear()` последним оператором.
- **P1-1** → §1.6 карточка проекта неактивна в v1 (UI-редактирование удалено
  с Ф6); подписки на `session.PropertyChanged` нет.
- **P1-2** → §8 цензус обновляется 2/2/2/4 → 3/3/3/5.
- **P1-3** → §2 гейт `CanUndo/CanRedo` при расчёте.
- **P1-4** → §6 хоткеи через `PreviewKeyDown` + записанная политика.
- **P1-5** → §2 Calculation-запись живёт до первой user-мутации (таймер её
  не закрывает).
- **P2-1..P2-5** → §8 (паттерны WI), §4.2 (формулировка `Climate.ApplySnapshot`;
  метод слайса `RestoreState`), §4.1 (асимметрия `PublishesDownstream`),
  §5.1/§7 (тестируемое время), §5.3 (Results `RefreshAll`; Thermal-оценка), §3
  (повторная гидравлика сразу — каскад синхронен).
