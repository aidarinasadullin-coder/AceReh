# Issledovanie 2 — Пошаговая навигация «Назад / Вперед» для степпера расчётов

> **Назначение документа.** Полный контекст исследования и проработки задачи для
> агента-планировщика. Документ самодостаточен: перечитывать переписку не нужно.
> Роль планирующего агента — превратить разделы 6–7 в план работ. Решение открытых
> вопросов (раздел 12) — только за владельцем; пока они не решены, план должен
> фиксировать оба варианта там, где это влияет на код.

---

## 1. Постановка задачи

Добавить в работающее WPF-приложение кнопки **«Назад» и «Вперед»** для пошаговой
навигации между **существующими** экранами расчётов (Wizard/Stepper). Требования
владельца:

- чистый MVVM, CommunityToolkit.Mvvm source generators;
- сохранение валидации шагов («Вперед» уважает текущие ошибки/статусы);
- переиспользование существующего каркаса навигации, не новая система.

---

## 2. Две важные поправки к исходной постановке (проверено по коду)

### 2.1. MaterialDesignInXamlToolkit в проекте ОТСУТСТВУЕТ

- `src/SnowMeltingCalculator.csproj`: CommunityToolkit.Mvvm **8.2.2**,
  Microsoft.Extensions.DependencyInjection **8.0.0**, PDFsharp-MigraDoc 6.x.
  Пакета MaterialDesign **нет**.
- `src/Resources/Dictionary.xaml:136` прямо пишет: «Без зависимости от MaterialDesign».
- UI — собственная дизайн-система REHAU в `src/Themes/`: стили `Button.Primary`,
  `Button.Secondary`, `Shell.*`, токены `Color.*`/`Font.*`. Литералы `FontSize`/HEX
  во View запрещены ratchet-тестом `ViewTokenHygieneTests`.
- **Следствие:** XAML кнопок — на стилях репо. MaterialDesign в исходном ТЗ —
  устаревшая формулировка, не требование.

### 2.2. Степпер-каркас УЖЕ существует — NavigationStore с нуля не нужен

Приложение уже имеет: боковой степпер (кружки 1–5), смену контента, кэш
представлений, честные статусы шагов. Задача сводится к добавлению **двух
команд-перелистывателей** поверх существующего переключателя «текущий шаг».

---

## 3. Карта релевантного кода (текущее состояние)

| Файл | Роль, ключевые факты |
|---|---|
| `src/App.xaml.cs` | Ручной `ServiceCollection` (без Generic Host), статический `App.Services`, splash-first старт, явное создание MainWindow |
| `src/Configuration/ServiceCollectionExtensions.cs` | Регистрации: `AddNavigationServices`, `AddClimateModule`…, `AddValidators` (8 валидаторов, transient), `AddSingleton<MainWindow>`. **Все модульные VM — синглтоны**. Есть **forwarding-регистрации**: `AddSingleton<IProjectLoadClimateAdapter>(sp => sp.GetRequiredService<ClimateViewModel>())` — образец для варианта B DI |
| `src/ViewModels/Shell/MainViewModel.cs` | Shell-контроллер: `MenuItems` (5 шагов: `Number`, `Title`, `Icon`, `Target`, `StepStatus`), `SelectedMenuItem`, `CurrentNavigationTarget` (private set), `RefreshStepStatuses` (Draft/Ready/Error/Recalculating), `IsWelcomeVisible`, `IsSidebarCollapsed`, слоты статус-бара, редирект `HeaderCalculate` → Thermal при инвалидном вводе (прецедент гейтирования) |
| `src/MainWindow.xaml` | Сайдбар-степпер (`ShellStepperList`, биндинг `MenuItems`/`SelectedMenuItem`), центральный `ContentControl` `ModuleContentControl` (`Content="{Binding CurrentModuleView, RelativeSource AncestorType=Window}"`) с attached **`ContentTransition.Enable`** (анимация перехода уже включена), welcome-оверлей, статус-бар |
| `src/MainWindow.xaml.cs` | `CurrentModuleView`; `ResolveView` — switch по `NavigationTarget`, создаёт view, ставит `DataContext = vm`; кэш `_moduleViewCache: Dictionary<NavigationTarget, object>`; подписка `MainViewModel.PropertyChanged` → `UpdateModuleView`; **`LoadHydraulicsDataOnNavigate` — тяжёлая гидратация «Результатов» на навигации (ADR-011)**; хоткеи Ctrl+B/S/Shift+S/O/N |
| `src/Models/Navigation/` | `NavigationTarget` (enum), `MenuItem` ([ObservableProperty] `StepStatus`…), `ShellStatusKind` |
| Модульные VM (`ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`, `CircuitsViewModel`, `ResultsViewModel`) | Синглтоны; ctor-инъекция слайсов `IProjectSession`; зеркала-снимки; подписки на `.Changed` слайсов **без отписок** (цензус — тест); `[ObservableProperty] _validationMessage`; валидация — собственный seam `IValidator<T>`/`ValidationResult` (БЕЗ `INotifyDataErrorInfo`/`ObservableValidator`); команды `[RelayCommand]`, refresh — **только ручные** `XCommand.NotifyCanExecuteChanged()` (ни одного `[NotifyCanExecuteChangedFor]`) |
| `tests/SnowMeltingCalculator.Tests/Architecture/ArchitectureRulesTests.cs` | R1–R6 (раздел 4); санкционированные writers WI-1…WI-8 |
| Тесты-контракты | `MainWindowChromeLayoutTests` (единственный корневой Grid), UiSmoke (селекторы по `AutomationId`, напр. `ShellStepperList`), `ViewTokenHygieneTests` (ratchet токенов), `ReactiveSubscriptionLifecycleTests` (цензус подписок на slice-`Changed`: Climate 2 / Construction 2 / Thermal 2 / Hydraulics 4) |
| Документация | корневой `AGENTS.md`; `docs/architecture/README.md` + ADR-журнал (ADR-002, 003, 008, 011, 012, 013); `docs/agents/lessons.md` (append-only) |

---

## 4. Инварианты и правила (план обязан их уважать)

Машинная форма — `ArchitectureRulesTests` (методы R1–R6):

- **R1** — `ProjectSession` — aggregate root с 4 явными слайсами (климат,
  конструкция, тепло, гидравлика). Каноническое состояние только там.
- **R2** — слайс мутируется **только санкционированными writers** (WI-1…WI-7,
  MarkDirty/MarkClean, projection контекста). Списки меняются только через ADR.
- **R3** — ViewModels — WPF-адаптеры, пишут **только в свой слайс** (WI-8).
  Не канонические хранилища.
- **R4** — Services не зависят от ViewModels (reflection + using-скан); единственное
  исключение ADR-002: `ResultsPdfDataBuilder.cs`, `HydraulicSummaryBuilder.cs`.
- **R5** — Results — derived-проекция, не владеет входами модулей.
- **R6** — wire-совместимость `.smc`.

Смежные ADR и house rules:

- **ADR-008** — shell UI-state (`IsWelcomeVisible`, `IsSidebarCollapsed`) — состояние
  окна, не канона. Позиция степпера — туда же: **не персистится в `.smc`**, на
  рестарте — шаг 1.
- **ADR-011** — реактивная модель: готовность шага — чистая функция канонического
  состояния; **пересчёт по навигации запрещён**; тяжёлая гидратация «Результатов»
  остаётся в `MainWindow.ResolveView`. Пересчёт — только по команде «Рассчитать».
- **ADR-012** — честные статусы шагов (Ready = «рассчитано и валидно»);
  `MainViewModel` уже подписан на `HydraulicsState.Changed` → `RefreshShellStatus`.
- **House style** — `[RelayCommand]`; refresh команд — ручные
  `NotifyCanExecuteChanged()`; подписки без отписок допустимы (синглтоны), но
  цензус slice-подписок фиксирован тестом.
- **Процесс** (`AGENTS.md`): сдача — uncommitted собираемое дерево + зелёный
  `dotnet test`; урок — отдельным коммитом в `docs/agents/lessons.md`; ADR-запись
  **не требуется**, если state ownership не меняется (здесь — не меняется).

---

## 5. Результаты исследования (что найдено в сети, что взято за основу)

### 5.1. Паттерн Navigation Store — канон минимальной навигации (SingletonSean)

`NavigationStore` (observable `CurrentViewModel` + событие смены + Dispose старой VM)
→ `NavigationService<TViewModel>` с фабричным делегатом → `NavigateCommand` →
`MainViewModel` мостит store в `PropertyChanged`:

- https://github.com/SingletonSean/wpf-tutorials/blob/f819d2d079ac55c5a64ac3152dae12aa7e9a0f14/MVVMEssentials/Stores/NavigationStore.cs
- https://github.com/SingletonSean/wpf-tutorials/blob/f819d2d079ac55c5a64ac3152dae12aa7e9a0f14/MVVMEssentials/Services/NavigationService.cs
- Сравнение 4 подходов: https://github.com/BYJRK/WpfNavigationDemo (варианты
  SingletonSean и «Sergio»: VM→View словарь + `[RelayCommand]`).

**Позиция CommunityToolkit** (мейнтейнер Sergio Pedri): готовой навигации в
MVVM Toolkit нет и не планируется — свой лёгкий сервис поверх DI — норма:
https://github.com/CommunityToolkit/MVVM-Samples/issues/21

### 5.2. Критичный техфакт про CanExecute (по сорцам генератора, Toolkit @ b135626)

- `RelayCommand` **не завязан на `CommandManager.RequerySuggested`** —
  `CanExecuteChanged` обычное событие; кнопка переспрашивает `CanExecute` только
  при его поднятии. → **Ручная нотификация обязательна**:
  https://github.com/CommunityToolkit/dotnet/blob/b135626dd54d33b8f05f2ff31591592c004aa848/src/CommunityToolkit.Mvvm/Input/RelayCommand.cs
- `[NotifyCanExecuteChangedFor]` на `[ObservableProperty]` генерирует
  `XCommand.NotifyCanExecuteChanged()` в сеттере
  (ObservablePropertyGenerator.Execute.cs L1282–1294, тот же commit).
- `CanExecute` может ссылаться даже на сгенерированное `[ObservableProperty]`-свойство
  (RelayCommandGenerator.Execute.cs L919–965).
- `AsyncRelayCommand` сам поднимает `CanExecuteChanged` на старте/конце выполнения
  (если не `AllowConcurrentExecutions`).

### 5.3. Wizard-примеры на CommunityToolkit.Mvvm (реальные приложения)

| Проект | Стек | Что взято |
|---|---|---|
| **Helldivers2ModManager** @ 21838c31 | WPF, net8.0-windows, Toolkit 8.4.0 | **Ближайший образец**: родитель-навигатор держит список шагов; `CanNext() => CurrentPage.IsValid() && …`; ребёнок уведомляет событием `IsValidChanged` → `NextCommand.NotifyCanExecuteChanged()`. Создаётся по мотивам, ниже — через VM-`PropertyChanged` |
| **ProtonVPN win-app** @ 4d9ac60d | WPF .NET 8 | Хуки активации шага `OnNavigatedTo(parameter, isBackNavigation)`; агрегация ошибок детей (`ItemErrorsChanged` → notify команды) |
| **FluentFlyout** @ c0d47175 | WPF, Toolkit 8.4.2 | Onboarding-wizard: `CurrentStepIndex`, `CanGoBack/CanGoNext` + `[RelayCommand(CanExecute=…)]`, ручные notify, `IsLastStep → Completed` |
| **vs-agentic** @ 27f3ab1f | WPF | Эталон атрибутной связки в одном классе: `[ObservableProperty]` + `[NotifyCanExecuteChangedFor(GoNextCommand)]` + `[RelayCommand(CanExecute = nameof(CanGoNext))]` |
| **porthole** @ d91d1158; **WpfHexEditorIDE** @ d6527fab | WPF | Пошаговая валидация: `CanGoNext => … && IsCurrentStepValid()` (switch по шагу) |
| Keboo/MaterialDesignInXaml.Examples @ 1dd91ccd | WPF + MaterialDesign | INotifyDataErrorInfo → `CanSubmit() => !HasErrors`. **Не берём**: у репо свой seam `IValidator<T>`; ObservableValidator ради кнопок — деградация согласованности |

### 5.4. DI (Microsoft.Extensions.DependencyInjection)

- Официальный документ: «Use the .NET Generic Host in a WPF app» —
  https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/how-to-use-host-builder
  (в репо, впрочем, Generic Host НЕ используется — ручной `ServiceCollection`).
- Для набора однотипных шагов: инъекция конкретных VM (у нас MainViewModel уже
  инжектит все 5) или **forwarding-регистрация маркера** —
  `AddSingleton<IMarker>(sp => sp.GetRequiredService<Concrete>())`; этот паттерн
  в репо уже есть (`IProjectLoad*Adapter`).
- Lifetime: шаги, хранящие введённое состояние между Назад/Вперёд, — синглтоны
  (у нас так и есть; состояние переживает навигацию само).

### 5.5. Итог: что взято за основу

1. **Родитель-навигатор с командами, делегирующими CanExecute активному шагу**
   (паттерн Helldivers2ModManager/ProtonVPN) — вместо NavigationStore.
2. **Маркер-интерфейс шага с default-реализациями** (C# default interface methods)
   — шаги переопределяют только то, что реально гейтится.
3. **Ручной refresh команд** (house style репо; техфакт 5.2 делает его обязательным);
   атрибутная связка 5.3/vs-agentic — как опция в простых случаях.
4. **Переиспользование существующего степпера** вместо ContentControl+implicit
   DataTemplates (последние — опциональная фаза 2, см. раздел 12).

---

## 6. Выбранное решение (архитектура интеграции)

**Не строить NavigationStore.** Переиспользовать: `MainViewModel.MenuItems`,
`SelectedMenuItem`, `CurrentNavigationTarget`, `MainWindow.ResolveView` (+кэш),
честные статусы ADR-012. Добавить: маркер `IWizardStep`, две команды в shell,
хук активации, кнопки в XAML.

Ключевые решения (каждое увязано с инвариантами):

1. **`IWizardStep` — интерфейс с default-реализациями** (`CanNavigateBack => true`,
   `CanNavigateNext => true`, `OnStepActivated() {}`): модульные VM реализуют
   только `Target` + то, что реально гейтится. Гейты — **Derived-читатели**
   существующих `ValidationMessage`/статусов; новых writers нет (R2 не задет).
2. **Команды в `MainViewModel`** (`GoBackCommand`/`GoNextCommand`,
   `[RelayCommand(CanExecute=…)]`): предикаты = «не край индекса» И «активный шаг
   разрешает». `MainViewModel` уже инжектит все 5 VM — DI можно не менять (вариант A).
3. **Refresh команд** по house style — ручные `NotifyCanExecuteChanged()` в трёх
   точках: смена шага; `PropertyChanged` пяти VM-адаптеров (сигналы
   `ValidationMessage`, `IsCalculating`); существующие точки обновления статусов.
   Подписки на **VM**, не на слайсы → **цензус slice-подписок не растёт**
   (`ReactiveSubscriptionLifecycleTests` не задет).
4. **Хук `OnStepActivated()`** вызывается из сеттера `SelectedMenuItem` (или
   `OnSelectedMenuItemChanged`, если свойство переведено на генератор). Только
   лёгкая step-local UI-работа. Тяжёлая гидратация «Результатов» остаётся в
   `ResolveView` (ADR-011 не задет).
5. **Навигационное состояние = shell UI-state** (как `IsSidebarCollapsed`):
   не попадает в `ProjectSession`/`.smc`, на рестарте — шаг 1.
6. **Направление данных однонаправленное:** канон → `Changed` → VM-зеркало →
   `CanNavigateNext` → кнопка. UI никогда не пишет в канон через навигацию.
7. **`Steps` собрать один раз** в readonly-поле в конструкторе (не на каждый вызов).

---

## 7. Готовый код (C# + XAML)

### 7.1. Маркер `IWizardStep` (новый файл)

```csharp
// src/Models/Navigation/IWizardStep.cs
namespace SnowMeltingCalculator.Models.Navigation;

/// <summary>
/// Shell-контракт шага расчётного степпера. Реализуют модульные ViewModel-адаптеры.
/// Гейты — Derived-читатели существующего состояния шага (ValidationMessage, статусы);
/// новых writers канона не появляется, state ownership не меняется.
/// </summary>
public interface IWizardStep
{
    /// <summary>Целевой экран шага (идентичность в MainViewModel.MenuItems).</summary>
    NavigationTarget Target { get; }

    /// <summary>Разрешён ли уход с шага назад. Default: разрешён.</summary>
    bool CanNavigateBack => true;

    /// <summary>Разрешён ли уход с шага вперёд. Default: разрешён (гейтит шаг).</summary>
    bool CanNavigateNext => true;

    /// <summary>Хук активации шага (степпер, Назад/Вперед, хоткеи).
    /// ТОЛЬКО лёгкая UI-работа; пересчёты здесь запрещены (ADR-011). Default: нет операций.</summary>
    void OnStepActivated() { }
}
```

### 7.2. Управляющая логика в `MainViewModel` (добавления к существующему классу)

```csharp
using SnowMeltingCalculator.Models.Navigation;

public partial class MainViewModel : ObservableObject
{
    // ... существующие поля, MenuItems, CurrentNavigationTarget, RefreshStepStatuses ...

    private readonly IWizardStep[] _steps;   // собрать ОДИН раз в ctor из уже
                                             // инжектированных VM (вариант A DI)

    private int CurrentStepIndex =>
        Array.FindIndex(MenuItems, m => m.Target == CurrentNavigationTarget);

    private IWizardStep? ActiveStep =>
        CurrentStepIndex >= 0 ? _steps[CurrentStepIndex] : null;

    // --- Команды Назад/Вперед: CanExecute делегирует активному шагу ---
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack() => NavigateStep(CurrentStepIndex - 1);

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void GoNext() => NavigateStep(CurrentStepIndex + 1);

    private bool CanGoBack() =>
        CurrentStepIndex > 0 && (ActiveStep?.CanNavigateBack ?? false);

    private bool CanGoNext() =>
        CurrentStepIndex >= 0 &&
        CurrentStepIndex < MenuItems.Length - 1 &&
        (ActiveStep?.CanNavigateNext ?? false);

    private void NavigateStep(int index)
    {
        SelectedMenuItem = MenuItems[index];          // существующий сеттер: title,
                                                      // статусы, welcome, хук (ниже)
        GoBackCommand.NotifyCanExecuteChanged();      // house style: ручная нотификация
        GoNextCommand.NotifyCanExecuteChanged();
    }

    // Если SelectedMenuItem — plain-свойство: те же 3 вызова добавить в КОНЕЦ
    // существующего сеттера. Если перевести на генератор — вариант на атрибутах:
    //
    // [ObservableProperty]
    // [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    // [NotifyCanExecuteChangedFor(nameof(GoNextCommand))]
    // private MenuItem? _selectedMenuItem;
    // + логика сеттера переезжает в partial void OnSelectedMenuItemChanged(MenuItem? value)

    private void OnStepActivated()
    {
        if (CurrentStepIndex >= 0)
            _steps[CurrentStepIndex].OnStepActivated();
    }

    // --- Ребёнок → родитель: refresh команд на изменение данных шага ---
    // Подписки на VM-адаптеры (синглтоны, без отписок — паттерн SummaryViewModel).
    // Цензус подписок slice-Changed НЕ меняется (подписываемся на VM, не на канон).
    private void SubscribeStepNotifications()
    {
        _climateViewModel.PropertyChanged      += (_, e) => OnStepInputChanged(e);
        _constructionViewModel.PropertyChanged += (_, e) => OnStepInputChanged(e);
        _thermalViewModel.PropertyChanged      += (_, e) => OnStepInputChanged(e);
        _circuitsViewModel.PropertyChanged     += (_, e) => OnStepInputChanged(e);
        _resultsViewModel.PropertyChanged      += (_, e) => OnStepInputChanged(e);
    }

    private void OnStepInputChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ClimateViewModel.ValidationMessage)
                           or nameof(ThermalViewModel.ValidationMessage)
                           or nameof(CircuitsViewModel.ValidationMessage)
                           or nameof(ThermalViewModel.IsCalculating)
                           or nameof(CircuitsViewModel.IsCalculating))
        {
            GoBackCommand.NotifyCanExecuteChanged();
            GoNextCommand.NotifyCanExecuteChanged();
        }
    }
}
```

### 7.3. Адаптация шагов (гейт там, где валидация уже есть)

```csharp
// src/ViewModels/Climate/ClimateViewModel.cs — правки декларации и добавления
public partial class ClimateViewModel : ObservableObject, IWizardStep
{
    public NavigationTarget Target => NavigationTarget.Climate;

    // «Шаг 1 → Шаг 2» разрешён, когда ввод климата валиден (тот же сигнал,
    // что уже читает shell в статус-бар и RefreshStepStatuses — ADR-012).
    public bool CanNavigateNext => string.IsNullOrEmpty(ValidationMessage);
}

// src/ViewModels/Thermal/ThermalViewModel.cs
public partial class ThermalViewModel : ObservableObject, IWizardStep
{
    public NavigationTarget Target => NavigationTarget.Thermal;

    public bool CanNavigateNext => string.IsNullOrEmpty(ValidationMessage)
        /* или строже — «рассчитано и валидно» по ADR-012:
           !_needsRecalculation && _thermalState.Snapshot.Result is { IsValid: true } */;

    public void OnStepActivated()
    {
        // лёгкая работа по входу на шаг (фокус, скролл); пересчёты ЗАПРЕЩЕНЫ (ADR-011)
    }
}

// Остальные три VM: достаточно добавить «, IWizardStep» к объявлению класса
// + Target. Гейты default (true). Для ResultsViewModel:
// public NavigationTarget Target => NavigationTarget.Results;
// (CanNavigateNext не нужен — на последнем шаге команда гасится индексом.)
```

### 7.4. DI — два уровня (на выбор; lifetimes НЕ менять)

**Вариант A (рекомендуется, ноль правок DI):** `MainViewModel` уже получает все
5 VM конструктором — массив `_steps` собирается из них (см. 7.2). Все VM —
синглтоны: введённые данные переживают Назад/Вперёд автоматически.

**Вариант B (если нужен тестируемый сервис-навигатор):** forwarding-регистрации
маркера — по образцу существующих `IProjectLoad*Adapter`:

```csharp
// src/Configuration/ServiceCollectionExtensions.cs
public static IServiceCollection AddWizardNavigation(this IServiceCollection services) => services
    .AddSingleton<IWizardStep>(sp => sp.GetRequiredService<ClimateViewModel>())
    .AddSingleton<IWizardStep>(sp => sp.GetRequiredService<ConstructionViewModel>())
    .AddSingleton<IWizardStep>(sp => sp.GetRequiredService<ThermalViewModel>())
    .AddSingleton<IWizardStep>(sp => sp.GetRequiredService<CircuitsViewModel>())
    .AddSingleton<IWizardStep>(sp => sp.GetRequiredService<ResultsViewModel>());
// впрыскивается в MainViewModel как IEnumerable<IWizardStep>; порядок = порядок регистрации
```

**Ограничение R4:** `IWizardStep` может потреблять ТОЛЬКО shell/UI-слой. Любая
инъекция `IEnumerable<IWizardStep>` в Services — нарушение R4.

### 7.5. XAML — кнопки «Назад»/«Вперед» (стили репо, НЕ MaterialDesign)

Вставка в `src/MainWindow.xaml` — **внутрь существующей сетки колонки контента**
(новая строка под `ModuleContentControl`; корневой Grid не трогать — контракт
`MainWindowChromeLayoutTests`):

```xml
<!-- Навигационная строка степпера под карточкой контента -->
<Grid Grid.Row="X" Grid.Column="1">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="Auto" />
    </Grid.ColumnDefinitions>

    <!-- Опциональные хоткеи (проверить коллизии с Ctrl+B/S/Shift+S/O/N в MainWindow.xaml.cs) -->
    <!--<Window.InputBindings>
        <KeyBinding Modifiers="Alt" Key="Left"  Command="{Binding GoBackCommand}" />
        <KeyBinding Modifiers="Alt" Key="Right" Command="{Binding GoNextCommand}" />
    </Window.InputBindings>-->

    <Button Grid.Column="1"
            AutomationProperties.AutomationId="ShellBackButton"
            Content="Назад"
            Margin="0,0,8,0"
            Command="{Binding GoBackCommand}"
            Style="{StaticResource Button.Secondary}" />

    <Button Grid.Column="2"
            AutomationProperties.AutomationId="ShellNextButton"
            Content="Далее"
            Command="{Binding GoNextCommand}"
            Style="{StaticResource Button.Primary}" />
</Grid>
```

Переходы анимируются бесплатно — на `ModuleContentControl` уже висит attached
`ContentTransition.Enable` (ADR-008 п.5). Иконки — опционально из
`src/Themes/Icons.Fluent.xaml` (ключи уточнить по месту; без литералов токенов).

### 7.6. Implicit DataTemplates — опциональная фаза 2 (в базовый план НЕ включать)

Текущий механизм (`ResolveView` + кэш + `LoadHydraulicsDataOnNavigate`) работает
и зафиксирован ADR-011. Пример классической замены (для полноты):

```xml
<Window.Resources>
    <DataTemplate DataType="{x:Type vm:ClimateViewModel}">      <views:ClimateView />      </DataTemplate>
    <DataTemplate DataType="{x:Type vm:ConstructionViewModel}"> <views:ConstructionView /> </DataTemplate>
    <DataTemplate DataType="{x:Type vm:ThermalViewModel}">      <views:ThermalView />      </DataTemplate>
    <DataTemplate DataType="{x:Type vm:CircuitsViewModel}">     <views:CircuitsView />     </DataTemplate>
    <DataTemplate DataType="{x:Type vm:ResultsViewModel}">      <views:ResultsView />      </DataTemplate>
</Window.Resources>

<ContentControl Content="{Binding ClimateViewModel}" ... />  <!-- Content = сама VM -->
```

**Trade-off:** теряются кэш вьюх и хук `LoadHydraulicsDataOnNavigate` (пришлось бы
перенести в `ResultsViewModel.OnStepActivated()` — семантика «на навигации»
сохраняется, но задевает формулировку ADR-011 → требует независимого read-only
ревью). Фаза 2 — отдельная мини-фаза, только по решению владельца.

---

## 8. Объём изменений (что меняется / что нет)

| Где | Что | Объём |
|---|---|---|
| `src/Models/Navigation/IWizardStep.cs` | **новый** файл-контракт | ~30 строк |
| `src/ViewModels/Shell/MainViewModel.cs` | + команды с охранниками, `_steps`, подписки, хук | +60–80 строк |
| 5 модульных VM | `IWizardStep` в объявлении + `Target` + (где нужно) гейт 1–2 строки | 3–5 строк на VM |
| `src/MainWindow.xaml` | блок из двух кнопок (+опц. хоткеи) | ~20 строк |
| `src/MainWindow.xaml.cs` | **ничего** (ResolveView, кэш, LoadHydraulicsDataOnNavigate как есть) | 0 |
| ProjectSession, 4 слайса, расчётные модули Core, 8 валидаторов, сборщик PDF | **ничего** | 0 |

Большие модульные VM **не переписываются**: меняется строка объявления класса
+ пара строк гейта. Вся внутренность (зеркала, подписки, команды расчёта) не трогается.

## 9. Красные линии (что было бы деградацией архитектуры)

1. **Персистить позицию шага** в ProjectSession/снапшот/`.smc` — превратило бы
   shell UI-state в канон (R1/R6, потребовало бы ADR + hash-пин). Позиция живёт
   рядом с `IsWelcomeVisible`/`IsSidebarCollapsed` (ADR-008), на рестарте — шаг 1.
2. **`OnStepActivated` с пересчётами/`RefreshAll`** — возврат к pull-модели
   «пересчёт по навигации», которую ADR-011 сознательно убил. Хук — только лёгкая
   step-local UI-работа.
3. **`IWizardStep` в Services** — любая инъекция в сервис = R4-нарушение.
   Маркер — только для shell.
4. **Мутация канона из гейтов/хуков** вне санкционированных writer-путей — R2.

Верификация по R1–R6 (все — «нет влияния» при соблюдении красных линий):

- R1: навигация не становится слайсом/свойством сессии.
- R2: команды и гейты ничего не пишут в канон; списки writers не расширяются.
- R3: `IWizardStep` — UI-контракт, не состояние; записи, если появятся, идут
  через уже санкционированные пути самой VM.
- R4: маркер потребляет только shell; forwarding-регистрации — в Configuration.
- R5: гейт шага 5 — чтение; гидратация остаётся в `ResolveView`.
- R6: позиция степпера не персистится.

`ArchitectureRulesTests` правок НЕ требует.

## 10. Влияние на тесты

- **UiSmoke / селекторные контракты:** добавить `ShellBackButton`/`ShellNextButton`
  (имена согласовать с существующим контрактом `AutomationId`).
- **`MainWindowChromeLayoutTests`:** кнопки добавлять во внутреннюю сетку колонки
  контента; корневой Grid не трогать. Прогнать тест.
- **`ReactiveSubscriptionLifecycleTests`:** не задевается (подписки на VM-`PropertyChanged`,
  не на slice-`Changed`; цензус 2/2/2/4 не растёт). При прогоне убедиться, что нет
  теста, пинящего VM-подписки `MainViewModel` (в картах не встречался).
- **`ViewTokenHygieneTests`:** XAML без литералов FontSize/HEX.
- **`ArchitectureRulesTests`:** зелёный без правок.
- **Документация:** урок в `docs/agents/lessons.md` (append-only, отдельный коммит,
  НЕ смешивать с диффом фазы); `docs/architecture/` — без изменений (state ownership
  не меняется), в handover записать «state ownership без изменений».

---

## 11. Механика простым языком (для сверки понимания с владельцем)

ProjectSession — «рабочая тетрадь» с 4 разделами; экраны — окна в неё; кнопки
«Назад/Вперед» — шторки, двигающие то, какое окно открыто. Содержимое тетради
при навигации неподвижно.

Что происходит при нажатии «Назад»:

```
Клик «Назад» (если кнопка не серая)
  → охранник команды: «не первый шаг И активный шаг разрешает?»
  → команда меняет РОВНО ОДНУ вещь: «текущий шаг = предыдущий» (SelectedMenuItem)
  → сеттер делает уже существующую хозяйственную часть:
      title, статусы, закрытие welcome + (новое) OnStepActivated
  → MainViewModel объявляет «шаг сменился» → окно достаёт страницу из кэша
    (ResolveView) и показывает с уже включённой анимацией ContentTransition
  → обе кнопки переспрашивают охранника (серый/активный)
```

- Данные не пропадают: каждому экрану соответствует один синглтон-объект (адаптер)
  на всё время работы; экран — «окошко» в него; при «Назад» ничего не
  пересоздаётся/не пересчитывается/не сохраняется.
- 5 «состояний» — это 5 окон (одно число «какое окно открыто»), а НЕ версии
  разделов. Разделов — 4, страниц по одной на раздел, новых страниц не появляется;
  правка перезаписывает страницу на месте (истории/undo нет).
- Правка пользователя = запись в ОДИН раздел → слайс кричит «изменился» →
  зависимые зеркала помечают себя устаревшими → статусы честно падают → кнопки
  переспрашивают гейт. Волна только вперёд; пересчёт только по «Рассчитать» (ADR-011).
- Пояснительная записка (PDF) — артефакт-снимок: собирается из текущего состояния
  в момент генерации; после правок сохранённый PDF сам не меняется — пересчитать
  и перегенерировать. Кнопки навигации ни тетрадь, ни отчёт не трогают.

## 12. Открытые вопросы (решает владелец; план должен зафиксировать выбор)

1. **Семантика «Далее»: мягкий или жёсткий wizard?**
   (а) мягкий — гейт только там, где валидация уже блокирует расчёт
   (`ValidationMessage` пуст), свободное перемещение сохраняется — рекомендация;
   (б) жёсткий — «Далее» гаснет, пока шаг не «рассчитан и валиден» (предикаты
   ADR-012). Код поддерживает оба (только переопределения `CanNavigateNext`).
2. **Кнопки при открытом welcome-экране:** скрывать или разрешить «Далее»
   как закрытие welcome (сеттер `SelectedMenuItem` уже закрывает welcome)?
3. **Хоткеи:** добавлять ли Alt+←/→ (проверить коллизии с Ctrl+B/S/Shift+S/O/N)?
4. **Фаза 2 (implicit DataTemplates):** включать или нет (см. 7.6; задевает
   ADR-011 → требует независимого read-only ревью).
5. **Имена AutomationId** `ShellBackButton`/`ShellNextButton` — подтвердить/переименовать
   под контракт UiSmoke.
6. **Строгий гейт для Thermal:** «валиден» = `ValidationMessage` пуст ИЛИ
   «рассчитано и валидно» (ADR-012)? Влияет только на жёсткий режим.

## 13. Чек-лист приёмки (для исполнителя)

- [ ] Новый файл `IWizardStep.cs`; 5 VM реализуют маркер (3–5 строк каждая).
- [ ] `MainViewModel`: команды `[RelayCommand(CanExecute=…)]`, ручные
      `NotifyCanExecuteChanged()` (БЕЗ новых slice-подписок), `_steps` — readonly,
      собирается в ctor.
- [ ] Хук `OnStepActivated` — только лёгкая UI-работа; `LoadHydraulicsDataOnNavigate`
      остаётся в `MainWindow.ResolveView`.
- [ ] XAML: кнопки на `Button.Secondary`/`Button.Primary`, без литералов токенов,
      во внутренней сетке (корневой Grid не тронут), с AutomationId.
- [ ] DI: вариант A (ноль правок) или B (forwarding по образцу `IProjectLoad*Adapter`);
      lifetimes VM не меняются.
- [ ] `dotnet test` зелёный: ArchitectureRulesTests (без правок),
      ReactiveSubscriptionLifecycleTests (цензус 2/2/2/4), UiSmoke (обновлённые
      селекторы), MainWindowChromeLayoutTests, ViewTokenHygieneTests.
- [ ] Урок — отдельным коммитом в `docs/agents/lessons.md`; в handover — запись
      «state ownership без изменений».
- [ ] Сдача: uncommitted собираемое дерево + зелёный `dotnet test` (по AGENTS.md).

---

*Источник: сессия исследования (4 параллельных агента: 3 librarian по внешним
паттернам + 1 explore по карте кода) + сверка с `docs/architecture/` и
`ArchitectureRulesTests`. Все внешние ссылки — фиксированные пермалинки (SHA).*

