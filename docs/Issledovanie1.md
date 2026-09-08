# Issledovanie1 — Кнопки «Назад/Вперёд» (история навигации экранов)

> **Назначение документа:** полный контекст исследования для агента-планировщика.
> Документ самодостаточен: все выводы подтверждены исследованием кода репо и поиском
> по GitHub (склонированные исходники, пермалинки за SHA). Статус: исследование
> завершено, реализация НЕ начата. Следующий шаг — планирование и решение владельца
> по 4 открытым вопросам (раздел 8).

---

## 1. Цель

Добавить в WPF-оболочку приложения кнопки **«Назад/Вперёд»** с историей переходов
между 5 экранами-шагами (как в браузере). Это навигационная история — НЕ undo/redo
данных и НЕ версии страниц.

---

## 2. Текущая архитектура навигации (проверено по коду)

### 2.1 Источник истины и «дверь» переходов

- `src/Models/Navigation/NavigationTarget.cs` — enum из 5 значений:
  `Climate / Construction / Thermal / Hydraulics / Results`.
- `src/ViewModels/Shell/MainViewModel.cs`:
  - `CurrentNavigationTarget` — текущий экран (shell UI-state);
  - `SelectedMenuItem` — публичный путь смены экрана, TwoWay-биндинг из степпера
    (`ListBox ShellStepperList` в `MainWindow.xaml`);
  - производные: `CurrentTitle`, статусы шагов в статус-баре.
- **Единая «дверь»**: любое изменение экрана проходит через
  `SelectedMenuItem` → `CurrentNavigationTarget` → материализацию view.
  Обратно из `ContentControl` напрямую экран менять нельзя.

### 2.2 Материализация View

- `src/MainWindow.xaml` — `ContentControl ModuleContentControl`.
- `src/MainWindow.xaml.cs` (code-behind):
  - `UpdateModuleView` / `ResolveView` — резолв view по цели, кэш `_moduleViewCache`;
  - хук `LoadHydraulicsDataOnNavigate()` — при каждом приходе на «Результаты»
    пересобирает данные из ProjectSession (гидратация);
  - анимация `ContentTransition`.

### 2.3 Ключевые точки

- **Все 5 модульных ViewModel — app-lifetime singletons**, держатся `MainViewModel`
  как readonly-поля. Views кэшируются. Экраны никогда не пересоздаются → состояние
  каждого экрана сохраняется само, снапшоты для возврата НЕ нужны.
- `HeaderCalculate` (кнопка «Рассчитать» в шапке) — пример **программного перехода**:
  присваивает `SelectedMenuItem` (переход на Теплотехнику).
- `PerformNewCalculationReset` (MainViewModel.cs:356–380) — сброс «Новый расчёт»:
  сбрасывает 4 канонических слайса с явными origin (`ClimateMutationOrigin.ProjectLoadReset`,
  `ThermalMutationOrigin.ProjectLoadReset`, `HydraulicsMutationOrigin.UserReset`,
  `ConstructionMutationOrigin.Reset`) и зеркалит дефолты в адаптеры.
- `src/Services/Project/ProjectSession.cs` (206 строк) — aggregate root: идентификация
  (ProjectNumber/Object), `CurrentFilePath`, `IsDirty`, guard восстановления
  `BeginProjectRestore()` (IDisposable, `IsLoadProjectInProgress`), 4 слайса
  (`ClimateState/ConstructionState/ThermalState/HydraulicsState`), каждый с `.Snapshot`
  и мутациями с origin (`MutationOrigin` enums) — заготовка под будущий data-undo/redo
  (INV-016).
- `src/Services/Navigation/` — это **диалоги и состояние расчёта, НЕ view-навигация**.
  Сервиса view-навигации в проекте нет.
- Undo/redo в коде отсутствует.
- DI: `Microsoft.Extensions.DependencyInjection` (регистрации в
  `src/Configuration/ServiceCollectionExtensions.cs`).

### 2.4 Что НЕ зависит от навигации (важно для UX-корректности)

- **Статусы шагов** (степпер/статус-бар) и **пояснительная записка**
  (`src/ViewModels/Shell/SummaryViewModel.cs`) читают снапшоты ProjectSession и
  обновляются по событиям изменения данных (`Changed`), а не по переходам.
  Переход назад/вперёд ничего в данных не меняет → записка и статусы остаются
  соответствовать реальности автоматически. Гидратация Результатов выполняется
  при каждом приходе на экран, включая возврат «Вперёд».

---

## 3. Исследование решений на GitHub (краткий каталог)

### 3.1 Prism — `IRegionNavigationJournal` (эталон двойного стека)

Пермалинк: [RegionNavigationJournal.cs](https://github.com/PrismLibrary/Prism/blob/358118cd640d9a22ff8cf21c8ad197fa038b7990/src/Prism.Core/Navigation/Regions/RegionNavigationJournal.cs),
[RegionNavigationJournalEntry.cs](https://github.com/PrismLibrary/Prism/blob/358118cd640d9a22ff8cf21c8ad197fa038b7990/src/Prism.Core/Navigation/Regions/RegionNavigationJournalEntry.cs),
[доки](https://docs.prismlibrary.com/docs/9.0/navigation/regions/navigation-journal/)

- Два стека (`backStack`/`forwardStack`), записи = `Uri` + `INavigationParameters`.
- `RecordNavigation`: если не внутренняя навигация → push текущей записи в back,
  **`forwardStack.Clear()`**; opt-out — `IJournalAware.PersistInHistory()`.
- `GoBack/GoForward` через `InternalNavigate` c флагом `isNavigatingInternal`
  (журнальный переход не записывает себя повторно — guard).
- Переиспользование экрана — контракт `INavigationAware.IsNavigationTarget`.
- Для ace: принимается **скелет** (2 стека + guard + forward.Clear). Фреймворк целиком
  — избыточен (regions/Uri-навигация не нужны, экранов 5, параметров нет).

### 3.2 ILSpy — `NavigationHistory<T>` (лучший прод-референс политики записи)

Пермалинки: [NavigationHistory.cs](https://github.com/icsharpcode/ILSpy/blob/d09e3c1f67e8744bbfd038b3fe54ea05835b65d3/ILSpy/NavigationHistory.cs),
[NavigationEntry.cs](https://github.com/icsharpcode/ILSpy/blob/d09e3c1f67e8744bbfd038b3fe54ea05835b65d3/ILSpy/NavigationEntry.cs),
[DockWorkspace.cs](https://github.com/icsharpcode/ILSpy/blob/d09e3c1f67e8744bbfd038b3fe54ea05835b65d3/ILSpy/Docking/DockWorkspace.cs),
[тесты](https://github.com/icsharpcode/ILSpy/blob/master/ILSpy.Tests/Navigation/NavigationTests.cs)
(исторически WPF, мастер уже на Avalonia — паттерн UI-независим).

- `Record(T)`: дедупликация (`IEquatable` + окно 0.5 с от double-fire),
  `back.RemoveAll(n => n.Equals(entry))` (без дублей цели), `forward.Clear()`.
- `suppressHistoryRecording` — во время воспроизведения back/forward события
  не пишутся в историю (защита от рекурсии).
- `PruneHistory` — удаление записей, чьи сущности выгружены (анти-stale).
- `BackEntries`/`ForwardEntries` — read-only списки для dropdown-меню на кнопках;
  `GoTo` — прыжок на N шагов; хоткеи **Alt+Left/Alt+Right**; поведение покрыто тестами.
- Для ace: принимается **политика записи** (дедупликация, suppress, очистка при
  reset/загрузке). Не нужны: view-state снапшоты, prune по выгрузке, дескрипторы
  `(Tab, Node)` (у нас записи = enum, экраны вечные).

### 3.3 Прочие референсы

| Решение | Суть | Вывод для ace |
|---|---|---|
| [ReactiveUI `RoutingState`](https://github.com/ReactiveUI/ReactiveUI/blob/d2c79cf324b1d3f6172b0fcc3bd54bbf18f27151/src/ReactiveUI.Shared/Routing/RoutingState.cs) | один стек живых VM, **back-only**; forward нет ни в 11.5.35 (2020), ни в 24.2.0 (2026); [issue #2798](https://github.com/reactiveui/ReactiveUI/issues/2798) — community достраивает forward сами | Контрпример: хранить живые VM в стеках не стоит |
| [Windows Template Studio WPF `NavigationService`](https://github.com/microsoft/WindowsTemplateStudio/blob/559e7fd27d2598b2c75ed7a33624eb2c7c9481eb/code/TemplateStudioForWPF/Templates/_comp/MT/Project/Services/NavigationService.cs) | Frame-обёртка, back-only; канонический PreventDuplicate: `if (_frame.Content?.GetType() != pageType \|\| (parameter != null && !parameter.Equals(_lastParameterUsed)))` | Frame-based не подходит; дедупликацию по «тип+параметр» взять на заметку |
| [FluentAvalonia `FAFrame`](https://github.com/amwx/FluentAvalonia/blob/e6def96e6281b79cb859e7984c7807e911e90878/src/FluentAvalonia/UI/Controls/Frame/FAFrame.cs) | BackStack/ForwardStack + **`CacheSize` = 10 (лимит глубины)** + `IsNavigationStackEnabled` (opt-out записи) | Единственный с лимитом глубины; для 5 экранов лимит не нужен, но паттерн opt-out — да |
| Отдельные библиотеки: [AsyncNavigation](https://github.com/NeverMorewd/AsyncNavigation) (41★, свежая), [WPF_MVVMC](https://github.com/michaelscodingspot/WPF_MVVMC) (65★, заморожена 2021), [MvvmNavigation](https://github.com/Egor92/MvvmNavigation) (36★), [Mvvm.Navigation](https://github.com/HavenDV/Mvvm.Navigation) (25★) | нишевые, все <100★ | Bus-factor риск; не брать |
| CommunityToolkit.Mvvm | навигации/журнала нет | Ничего не даёт и не мешает |

Ещё dual-stack в проде (для сверки): GitExtensions, CKAN (generic + тесты),
ValveResourceFormat (List+курсор, мышиные кнопки XButton1/2), Mesen2.

**Итог исследования:** готовой «золотой» WPF-библиотеки нет; берём гибрид —
скелет Prism + политика записи ILSpy, собственная реализация ~100–150 строк.

---

## 4. Предварительно принятое решение (эскиз)

**Собственное решение: история = два стека enum-значений в оболочке.**

```csharp
// Ядро (~40 строк) — калька Prism/ILSpy без лишнего:
public sealed class NavigationHistory
{
    private readonly Stack<NavigationTarget> _back = new();
    private readonly Stack<NavigationTarget> _forward = new();
    private bool _suppress;                      // guard (Prism isNavigatingInternal / ILSpy suppress)

    public bool CanGoBack => _back.Count > 0;
    public bool CanGoForward => _forward.Count > 0;
    public IEnumerable<NavigationTarget> BackEntries => ...;    // для dropdown (ILSpy), опционально
    public IEnumerable<NavigationTarget> ForwardEntries => ...;

    public void Record(NavigationTarget target)  // вызывает MainViewModel при смене цели
    {
        if (_suppress) return;
        if (target == _current) return;          // дедупликация (enum-равенство, окно 0.5с не нужно)
        _back.Push(_current);
        _forward.Clear();                        // браузерная семантика
        _current = target;
    }

    public NavigationTarget GoBack()   { _suppress = true;  try { /* pop; current -> forward */ } finally { _suppress = false; } }
    public NavigationTarget GoForward(){ _suppress = true;  try { /* симметрично */ } finally { _suppress = false; } }
    public void Clear() { _back.Clear(); _forward.Clear(); _current = ...; }
}
```

Интеграция (всё в shell-слое):

- `MainViewModel`: смена `CurrentNavigationTarget`/`SelectedMenuItem` → `Record`;
  команды Назад/Вперёд выставляют цель **через тот же `SelectedMenuItem`** —
  автоматически срабатывают `ResolveView`, гидратация Результатов, заголовок,
  подсветка степпера, статус-бар.
- `MainWindow.xaml`: 2 кнопки в шапке рядом с заголовком
  (`IsEnabled ← CanGoBack/CanGoForward`), хоткеи Alt+←/→ (InputBindings).
- Очистка истории: `PerformNewCalculationReset` (новый расчёт) и при открытии
  другого проекта; на время загрузки/restore — suppress-флаг (служебные переходы
  не пишутся).
- Сброс `Clear()` при закрытии проекта — по аналогии с «Undo/Redo очищается»
  (см. `docs/Планируемые_изменения.md`).

### Простая модель (для единообразия терминологии в плане/UX)

- История — «блокнот с двумя списками»: «откуда пришёл» (Назад) и
  «куда можно вернуться» (Вперёд); в записях — только **номера шагов**, не страницы.
- Страниц не существует в нескольких версиях: 5 экранов × 1 текущая версия данных.
  История — **одна общая лента визитов** на всё приложение, не по ленте на раздел.
- «Вернуться на прошлый экран» ≠ «вернуть данные как было» (undo/redo — отдельная
  будущая функция, к истории экранов отношения не имеет).

---

## 5. Совместимость с ProjectSession (обязательные правила)

ProjectSession — единственный writable owner состояния жизненного цикла проекта
(идентификация, путь, dirty, 4 канонических слайса с origin-мутациями).

1. **История навигации НЕ входит в ProjectSession и в `.smc`.** Категория —
   shell UI-state, прецедент: ADR-008 п.1 (welcome/оверлей — как `IsSidebarCollapsed`).
   Формат файла не меняется.
2. **R1–R6 не затрагиваются**: новая каноническая величина не появляется; sanctioned
   writers не меняются; `ArchitectureRulesTests` остаются зелёными без правок.
3. **R4 (Services не зависят от concrete ViewModels)**: записи истории — только
   `NavigationTarget` (enum). Никаких ссылок на VM/View в истории.
4. **INV-016**: навигация — не мутация данных, origin не требуется. Не смешивать
   навигационную историю с будущим data-undo/redo (обе истории будут в UI, но это
   разные вещи и разные «хозяева»).
5. **`IsDirty`**: переходы не вызывают `MarkDirty()` — зафиксировать тестом.
6. **ADR-011 (Results is derived)**: возврат «Вперёд» на Результаты обязан идти через
   единый путь (`SelectedMenuItem` → `ResolveView` → `LoadHydraulicsDataOnNavigate`),
   чтобы гидратация отработала и не было двух путей материализации.

Швы интеграции (все точки уже существуют, искать по коду не надо):
- `PerformNewCalculationReset` (MainViewModel.cs:356–380) → добавить `history.Clear()`;
- загрузка проекта / `BeginProjectRestore()` → suppress на время restore + `Clear()`;
- `HeaderCalculate` → решение по политике (вопрос 3 ниже).

---

## 6. Объём изменений

| Трогается | Объём | Не трогается |
|---|---|---|
| `src/ViewModels/Shell/MainViewModel.cs` | +80–100 строк: история, команды, Record, Clear, suppress | Вся Гидравлика (вкл. `CircuitsViewModel` ~1500 строк), Результаты (~1000+), Теплотехника, Конструкция, Климат — **ни строки** |
| `src/MainWindow.xaml` | 2 кнопки + InputBindings (Alt+←/→) | `SummaryViewModel` (пояснительная записка) |
| Новые тесты | контракт переходов, семантика стеков, «навигация не делает проект грязным» | `ProjectSession`, 4 слайса, `.smc`-сериализация |
| `docs/agents/lessons.md` | запись урока (append-only, по правилам репо) | `ArchitectureRulesTests` |

Переписывать модули **не нужно**: модульные VM видят только «я стал активным» —
как при обычном клике. Вся механика — в оболочке.

---

## 7. Критерии приёмки (черновик для плана)

1. Назад/Вперёд работают через единую дверь `SelectedMenuItem`; все побочные
   эффекты штатного перехода сохраняются (заголовок, степпер, гидратация Результатов,
   welcome-состояние, статус-бар).
2. `forward`-стек очищается при любом новом пользовательском переходе; повторные
   нажатия/двойные события не порождают дублей.
3. Программные переходы при загрузке проекта не попадают в историю; при
   «Новом расчёте» и открытии другого проекта история очищается.
4. Навигация не меняет `IsDirty`, не пишет в ProjectSession, не меняет `.smc`.
5. Кнопки гаснут (`CanGoBack`/`CanGoForward`), хоткеи работают, в начале работы
   «Назад» недоступна.
6. `dotnet test` зелёный, включая `ArchitectureRulesTests`; новые unit-тесты на
   семантику стеков (по образцу тестов ILSpy/CKAN).

---

## 8. Открытые вопросы (требуют решения владельца; планировщик — включить в план как развилки)

1. **Охват**: только 5 шагов степпера, или также внутренние табы коллекторов
   Гидравлики (`SelectedCollectorIndex`)? Внутренние табы = иерархическая история,
   заметно сложнее.
2. **Размещение**: поле/логика внутри `MainViewModel` (проще, shell-state уже там)
   или отдельный `INavigationHistoryService` в DI (чище шов для тестов, чуть больше
   ceremony)? Оба варианта совместимы с R4 (enum-записи).
3. **Программный переход `HeaderCalculate` → Thermal**: пушить в back-стек
   (считать пользовательским) или считать системным (не записывать)?
4. **UI**: просто 2 кнопки или кнопки с dropdown-списками истории
   (`BackEntries`/`ForwardEntries`, как у ILSpy)? Нужны ли хоткеи Alt+←/→?

---

## 9. Ограничения по правилам репо (AGENTS.md)

- Инварианты R1–R6 проверяются `tests/SnowMeltingCalculator.Tests/Architecture/ArchitectureRulesTests.cs`;
  списки sanctioned writers меняются только через ADR — здесь не требуются.
- Handover: незакоммиченное собираемое дерево + зелёный `dotnet test`; коммиты —
  только по явной просьбе владельца.
- Урок сессии записать в `docs/agents/lessons.md` (append-only).
- Досье `docs/architecture-migration/` не трогать (это provenance); при изменении
  state ownership потребовалось бы обновить `docs/architecture/` — здесь изменения
  state ownership НЕТ, достаточно записи «state ownership без изменений».

---

## 10. Пермалинки (исходники исследования, закреплены за SHA)

- Prism `358118cd`: RegionNavigationJournal.cs, RegionNavigationJournalEntry.cs,
  docs.prismlibrary.com/docs/9.0/navigation/regions/navigation-journal/
- ILSpy `d09e3c1f`: ILSpy/NavigationHistory.cs, ILSpy/NavigationEntry.cs,
  ILSpy/Docking/DockWorkspace.cs, ILSpy/Commands/BrowseBackCommand.cs,
  ILSpy.Tests/Navigation/NavigationTests.cs
- ReactiveUI `d2c79cf3` (+ теги 11.5.35/16.4.15/18.4.44 — все одностековые):
  src/ReactiveUI.Shared/Routing/RoutingState.cs; issue #2798
- WTS `559e7fd2`: code/TemplateStudioForWPF/Templates/_comp/MT/Project/Services/NavigationService.cs
- FluentAvalonia `e6def96e`: FAFrame.cs, FAFrame.properties.cs (CacheSize=10),
  FAFrameNavigationOptions.cs, FrameTests.cs
- Локальные копии склонированных репо (если нужно показать фрагменты):
  `C:\Users\KUVAEV~1\AppData\Local\Temp\opencode\{prism,ReactiveUI,ILSpy,WTS}`

---

*Документ подготовлен по результатам исследования 2026-09-08. Реализация не начата.
Следующий шаг: планирование (другой агент) → ответы владельца на вопросы раздела 8 → реализация.*
