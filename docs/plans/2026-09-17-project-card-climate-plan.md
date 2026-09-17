# План: возврат редактирования «Номер проекта / Объект» — карточка на вкладке «Климат»

> Дата: 2026-09-17. Статус: концепция подтверждена владельцем 2026-09-17
> («на вкладке климат — оптимально», «подтверждаю», «делай»); независимое
> атакующее ревью пройдено 2026-09-17 — APPROVE-WITH-EDITS, 6 находок
> (P0 цензус подписок, 2×P1, P2, 2×P3), все приняты, правки внесены в §§1–6.
> Дерево не коммитится.
> Основание: исследование 2026-09-17 (редактирование удалено с Ф6, поля
> заполняются только вручную в .smc; в ПЗ/отчётах — прочерки, имя файла
> записки деградирует).
> show-me: [show-me-project-card.html](show-me-project-card.html).

## 0. Суть

Владелец номера проекта и объекта — `ProjectSession`
(`ProjectNumber`/`ProjectObject`, сохраняются в `.smc` через
`ProjectPersistenceMapper`). С фазы Ф6 редактирование из UI удалено:
шапка MainWindow показывает поля read-only, а писать их негде. Pass-through
свойства в `ResultsViewModel` остались, но недостижимы из UI.

**Решение:** карточка «Данные проекта» на вкладке «Климат» (шаг 1 из 5 —
исходные данные) — два опциональных поля в одну строку. Механика —
pass-through `ClimateViewModel` на сессию (канон не меняется: владелец тот
же `ProjectSession`, один writer на слой UI добавляется санкционированно,
ADR). `ResultsViewModel` подписывается на события сессии — шапка обновляется
при вводе на Климате и при загрузке `.smc`.

### Зафиксированные решения владельца

| # | Вопрос | Решение |
|---|---|---|
| V1 | Место ввода | Вкладка «Климат», карточка над «Город и климатические параметры» (владелец, 2026-09-17: «на вкладке климат… оптимально») |
| V2 | Undo | Правки номера/объекта остаются вне отмены v1 (прецедент P1-1 плана undo-redo) — подтверждено владельцем 2026-09-17 («undo как есть») |
| V3 | Обязательность | Поля опциональные, на расчёт не влияют; пустое значение — прочерк в отчётах (существующий `EmptyAsDash`) |
| V4 | Шапка | Точка отображения не меняется: read-only строка в шапке, биндинг к `ResultsViewModel.ProjectNumber/ProjectObject`; живость — подписка VM на сессию |

---

## 1. ClimateViewModel — pass-through сессии

| Файл | Правка |
|---|---|
| `src/ViewModels/Climate/ClimateViewModel.cs` | Поле `private readonly IProjectSession? _projectSession;` — присваивается в public ctor; public ctor валидирует сессию (`?? throw ArgumentNullException` — ревью №5). Свойства `ProjectNumber`/`ProjectObject`: `get => _projectSession?.ProjectNumber ?? string.Empty;` setter — equality-выход (`if (сессия.поле == value) return;`, как `ResultsViewModel.cs:70`); guard на null-сессию (legacy internal ctor); иначе присваивание сессии (которая сама шлёт `PropertyChanged`) + `MarkDirty()` при `!_projectSession.IsLoadProjectInProgress`. Нотификация UI — ОДИН канал: репост по подписке на сессию (ниже); сеттерный `OnPropertyChanged()` не дублируется (ревью №4) |
| там же | В public ctor — подписка `_projectSession.PropertyChanged`: события `ProjectNumber`/`ProjectObject` репостятся в VM (обновление карточки при загрузке `.smc`); подписка без отписки — паттерн ctor (`ClimateState.Changed`, комментарий ResultsViewModel.cs:582) |

Не трогается: internal legacy ctor (сессии нет — свойства пустые), климати-
ческий сброс (`ResetToDefaults`/`ResetToCityData` поля не затрагивают),
`IProjectSessionClimateState` (identity — не часть климат-слайса).

## 2. ResultsViewModel — живая шапка

| Файл | Правка |
|---|---|
| `src/ViewModels/Results/ResultsViewModel.cs` (ctor, рядом с подписками на Changed-события слайсов) | `_projectSession.PropertyChanged += OnSessionPropertyChanged;` обработчик репостит `OnPropertyChanged(nameof(ProjectNumber))`/`(nameof(ProjectObject))` — шапка (`MainWindow.xaml:129-138`) обновляется при вводе на Климате и при загрузке проекта |

Цензус подписок (ревью №1, P0): `ReactiveSubscriptionLifecycleTests`
(`HandlerCounts_MatchPhase10Census_OnProductionShapedGraph`,
expected-таблица ~строка 101) пинит `Session.PropertyChanged` на 1
production-обработчик (MainViewModel). Настоящий план добавляет +2 →
ожидание правится 1 → 3 вместе с протухшим комментарием «project card is
out of scope in v1» (~строки 87-91) — четвёртая поправка цензуса.

## 3. UI вкладки «Климат»

| Файл | Правка |
|---|---|
| `src/Views/Climate/ClimateView.xaml` | Новая карточка `FormCard.Root` «Данные проекта» между заголовком страницы и форм-карточкой климата (третья `RowDefinition`): подпись-хинт «Попадают в шапку, пояснительную записку и отчёты», Grid 2 колонки — «Номер проекта» (`TextBox.Default`, `MaxLength=100`, ToolTip «Например, 2026-014») и «Объект» (`MaxLength=200`, ToolTip «Например, парковка у ТЦ, Москва»); биндинг `UpdateSourceTrigger=LostFocus` (ревью №6 — без эха на каждый символ); attached-свойства `TextBoxBehavior.SelectAllOnFocus`/`RestoreOnEscape`; AutomationId `ClimateProjectNumber` / `ClimateProjectObject` |

Placeholder-текст — существующими средствами (без нового контрола): WPF
`TextBox` без нативного placeholder; используем `ToolTip` + `FormCard.Hint`
под полями. Отдельный attached-property placeholder в объём не входит.

## 4. Архитектурное санкционирование

| Файл | Правка |
|---|---|
| `docs/architecture/README.md` | ADR-015: `ClimateViewModel` — санкционированный writer session identity (`ProjectNumber`/`ProjectObject`) через pass-through + `MarkDirty`; список WI-5 дополнен; state ownership не меняется (владелец прежний — `ProjectSession`), `.smc` не расширяется, undo-охват не меняется (V2). Явно: ADR-015 supersede'ит формулировку ADR-014 п.9 «подписки на `IProjectSession.PropertyChanged` нет» (ревью №1) — после плана их три (MainViewModel, ResultsViewModel, ClimateViewModel) |
| `tests/.../Architecture/ArchitectureRulesTests.cs` | WI-5 (MarkDirty) allowlist: + `ClimateViewModel.cs` |

## 5. Тесты

| Файл | Тесты |
|---|---|
| `tests/.../Climate/ClimateViewModelTests.cs` | setter пишет в сессию и вызывает `MarkDirty` (dirty-флаг); повторное то же значение — без события и без `MarkDirty` (equality-выход); событие сессии `ProjectNumber` репостится в VM; сброс климата поля не трогает |
| `tests/.../ViewModels/ResultsViewModelOpenProjectTests.cs` (или соседний файл) | запись в сессию извне → `ResultsViewModel.PropertyChanged` для `ProjectNumber`/`ProjectObject` |
| `tests/.../Services/Project/ReactiveSubscriptionLifecycleTests.cs` | цензус `Session.PropertyChanged` 1 → 3 (правка expected-строки + комментарий-амендант) |
| `tests/.../Architecture/ArchitectureRulesTests.cs` | прогон WI-5 с новым allowlist (существующий тест) |

Пин на «одно значение — один владелец»: карточка не хранит локальных копий
(геттер всегда читает сессию) — проверяется тестом «сессия изменилась извне →
геттер VM отдаёт новое значение без сеттера».

## 6. Совместимость и риски

- `.smc`: формат не меняется (поля уже в `ProjectData`/снапшоте).
- Потребители подхватят значения без правок — читают сессию/VM: ПЗ
  (`ProjectSectionBuilder`), отчёт «Результаты» (`ResultsPdfDataBuilder`),
  Excel (`ExcelExportService` B2/B3), имя файла записки
  (`ResultsViewModel.cs:719`).
- **Writer identity при загрузке** (ревью №2, урок №27 — сверка артефакта):
  при открытии `.smc` поля пишут pass-through-сеттеры **ResultsViewModel**
  (`BeginProjectRestore`/`LoadProjectDataAsync`, `ResultsViewModel.cs:1632-1633`,
  под guard'ом загрузки), не `ProjectLoadOrchestrator`. Карточка Климата
  обновляется репостом `PropertyChanged` сессии (§1).
- Undo: правки не попадают в дневник (V2) — известное ограничение,
  зафиксировано в ADR. **Осознанное следствие** (ревью №3, DESIGN-минимум):
  `UndoRedoService.SyncDirty` после любого undo/redo, когда позиция дневника
  совпадает с точкой чистоты, вызывает `MarkClean()` — dirty-флаг, поставленный
  правкой номера/объекта, будет погашен откатом несвязанной правки данных
  (сценарий: сохранить → ввести номер → править климат → Ctrl+Z). Поскольку
  identity в дневнике и не откатывается (V2, решение владельца), следствие
  принимается как есть и документируется в ADR-015; изменение `SyncDirty`
  (исключение identity-дельты) не входит в объём.
- Legacy internal ctor: свойства без сессии возвращают пустую строку,
  setter не пишет — только тестовый шов, в DI не используется.
- R3 (VMs mutate only their own slice): не задет — identity не входит в
  списки слайсов теста; WI-5 — санкционированное расширение (ADR-015).

## 7. Процесс

1. Независимое атакующее ревью плана (subagent, вектора A1–A8) → чек
   `docs/reviews/2026-09-17-project-card-plan-review.md`.
2. Имплементация §1–§4, тесты §5.
3. Полный зелёный `dotnet test`; handover — некоммиченное дерево.
4. show-me regenerated from this plan (урок №27); сверка артефакта с
   планом — шаг ревью.
