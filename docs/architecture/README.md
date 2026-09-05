# Архитектура — живой контур

Это единственное живое описание архитектуры приложения. Канонические
правила — шесть инвариантов в корневом `AGENTS.md`; их машинная проверка —
`tests/SnowMeltingCalculator.Tests/Architecture/ArchitectureRulesTests.cs`
(правила R1–R6). История миграции и её карты — замороженное досье
`docs/architecture-migration/` (только чтение).

## Диаграммы

| Файл | Что показывает |
|---|---|
| [overview.md](overview.md) | Слои, направление зависимостей, место Results |
| [project-session.md](project-session.md) | Aggregate root, срезы, санкционированные writers |
| [persistence-flow.md](persistence-flow.md) | Save/load `.smc`, снимок, restore guard |

## Правило поддержки

Меняешь то, что видно на диаграмме (слои, зависимости, срезы, поток
persistence), — обнови диаграмму и добавь строку в журнал ниже **в том же
коммите**. Не меняешь — запись не нужна.

## Журнал решений (ADR-light)

### ADR-001 — 2026-09-04 — Пост-миграционный архитектурный контур

Миграция завершена (фазы 1–11, owner-accepted). Постоянные правила — шесть
инвариантов в корневом `AGENTS.md`; контроль — архитектурные тесты R1–R6;
живое описание — диаграммы здесь. Виджет (16 МБ HTML) и машинная модель
(`architecture-model.json`) выведены из эксплуатации и перемещены в
`docs/architecture-migration/archive/` как provenance; досье миграции
заморожено. Почему: документация, поддерживаемая руками, расходится с
кодом; тесты связаны с кодом напрямую и не могут протухнуть.

### ADR-002 — 2026-09-04 — Исключение R4: Results-билдеры

`src/Services/Results/ResultsPdfDataBuilder.cs` и
`src/Services/Results/HydraulicSummaryBuilder.cs` зависят от read-model
записей `ViewModels.Results` — санкционировано владельцем в фазе 11
(записанная backlog-hygiene задача). Остальные зависимости Services →
ViewModels запрещены. Проверяется тестом R4 (reflection по типам + скан
`using` в `src/Services`).

### ADR-003 — 2026-09-04 — Списки санкционированных writers

Скан-тесты R2/R3/R5 переносят writer-inventory фазы 10 (8/8 PASS,
owner-accepted) как постоянные тесты; списки допустимых writers перенесены
дословно из принятого evidence (`evidence/phase-10-reactive-ownership-
multiplicity-closure/writer-inventory.mjs`). Изменение списка = изменение
правила: только через запись в этом журнале.

### ADR-004 — 2026-09-04 — Семантика УГВ при жизненном цикле и шаблонах

Партия правок по плану
`docs/plans/2026-09-04-gwl-template-results-batch.md` (ревью momus/oracle,
owner-approved D1–D6). Владение состоянием не меняется; фиксируются три
семантических решения:

1. **Новый расчёт и сброс перед загрузкой** используют заводской УГВ 2.0 м
   (`ConstructionDefaultStateInitializer.Apply(origin)`), а не УГВ
   предыдущего проекта. Точки входа `ConstructionViewModel.Initialize/Reset`
   сознательно сохраняют текущий УГВ (D6 «фактический»).
2. **Применение шаблона** (D2): шаблон задаёт состав и толщины слоёв; λА/λБ
   определяет УГВ проекта (`Construction.GroundwaterLevel` пересчитывает λ
   при присваивании). `template.DefaultGroundwaterLevel` с этого момента
   декоративен: в UI редактора шаблонов поле отсутствует (и не
   показывалось), новым шаблонам присваивается 2.0; свойство сохранено
   в модели ради совместимости templates.json и помечено как устаревшее
   (решение владельца «спрятать», 2026-09-04).
3. **Ручные λ и persistence** (D5): восстановление проекта больше не
   сбрасывает `IsLambdaOverridden` — флаг round-trip сохраняется. Это
   отменяет поведение P0-7 («restore intentionally resets override flags»),
   закреплённое в characterization-тестах: `ProjectRoundTrip_*` в
   `ResultsViewModelOpenProjectTests` и
   `ProjectLifecycleFlowCharacterizationTests` обновлены в этой же партии.
   Комбобокс УГВ — двухпозиционный; программные присваивания опции не
   мутируют скаляр УГВ (guard `_isResetting/_isRefreshing/_isSyncing`).

### ADR-005 — 2026-09-04 — Полное снятие флага «Нагрузки на покрытие»

Решение владельца (обсуждение редизайна, `docs/design/redesign-plan.md`,
Фаза 4Б): флаг нагрузок выпиливается из программы целиком — состояние,
правило минимальной толщины 50 мм над трубой, UI и формат снимка.
Последствия зафиксированы заранее:

1. Контракт `.smc` меняется: поле выпадает из снимка; persistence-фикстуры
   и hash-пины (`ProjectSnapshotContractTests` и связанные) обновляются
   сознательно в фазе внедрения.
2. Старые проекты, сохранённые с включёнными нагрузками, после миграции
   считаются как «нагрузок нет» — расчётные результаты таких проектов
   изменятся. Принято владельцем.
3. Списки санкционированных writers R2 (ADR-003) не меняются — паттерны
   тестов проверяют вызовы методов, а не свойства; правятся
   persistence- и characterization-тесты, фиксировавшие поведение флага.

Внедрение — Фаза 4Б плана редизайна; до её явного запуска владельцем флаг
остаётся как есть (в UI — до этого момента без изменений).

### ADR-006 — 2026-09-05 — Фаза 2 редизайна: findings ревью, меняющие реализацию UI-слоя

Независимый read-only ревью дизайна Фазы 2 (компонентная библиотека,
`docs/design/redesign-plan.md` §Ф2) до реализации. State ownership,
санкционированные writers и `.smc`-контракт не затрагиваются; записи ниже —
только про UI-слой (Themes/Dictionary/App.xaml, attached properties):

1. **Implicit-стили TextBox/ComboBox — без размеров и Padding.** Стилевые
   `MinHeight`/`Padding` применяются даже при локальном `Height="26"` на
   элементе → обрезанный ввод/выросшие строки (голые поля CircuitsView
   с `Height="26"` — строки 241, 255, 308, 323 рабочего дерева, а также
   CircuitInputView, ячеечные комбобоксы Construction/TemplateEditor).
   Высота 34 и паддинги — только в явных ключах; implicit-стили задают
   только шаблон и кисти.
2. **Порядок мержа App.xaml:** канонические ключи `Controls.*.xaml` грузятся
   ДО `Dictionary.xaml` (алиасы `RehauTextBoxStyle`/`RehauComboBoxStyle`
   держат `BasedOn={StaticResource …}`), порядок: Tokens → Icons.Fluent →
   Components.* → Controls.* → RecalcIndicators → Dictionary → Shell.
3. **Aspect.Ratio (чип 2:1) — через Height, не Width:** `Height =
   ActualWidth / ratio` с guard'ом от рекурсии; сеттер Width ломал бы
   растяжение в Grid/UniformGrid и будущих wrap-панелях.
4. **Фокус-рамка полей — без смены толщины:** двойной Border (внешний 1px
   hairline + внутренний 1px-кольцо Brand.Red при фокусе) вместо 1→2px —
   метрики не прыгают, паттерн эталона (tokens.css `.in.err`).
5. **Ratchet-ожидания скорректированы:** чистка ресурсов ResultsView даёт
   HEX 1→0, FontSize остаётся 92 (литералы в разметке body — вне объёма
   Ф2.5); ratchet CircuitsView не меняется (5,68) — сам файл получил лишь
   две токен-замены Setter'ов (FontSize 14 → Font.Size.Body).
   Allowlist уменьшается только по факту.
6. Мёртвые легаси-ключи `RehauDataGridHeaderStyle`/`RehauDataGridRowStyle`
   удаляются (0 использований); канонические `DataGrid.*` переезжают в
   `Controls.DataGrid.xaml` как заготовка Ф3 (вьюхи подключат их там).

### ADR-007 — 2026-09-05 — Фаза 3 редизайна: findings ревью, меняющие реализацию (Гидравлика)

Независимый read-only ревью дизайна Фазы 3 (`docs/design/redesign-plan.md`
§Ф3, эталоны `docs/design/renders/03*.png`) до реализации. State ownership
не меняется: `HydraulicsState` — слайс `ProjectSession`, VM — read-only
адаптер, writer-паттерны R1–R6 и расчётное ядро не затрагиваются. Записи —
только про UI-слой:

1. **Видимость колонок DataGrid — через BindingProxy.** `DataGridColumn`
   лежит вне визуального/логического дерева: DataContext не наследуется,
   `RelativeSource AncestorType` на колонке молча не разрешается. Для
   двухрежимной таблицы («Компактно/Полностью») колонки биндят
   `Visibility` на `CircuitsViewModel.IsCompactMode` через
   `BindingProxy : Freezable` (ресурс вьюхи). `IsCompactMode` — чистое
   UI-состояние адаптера: observable-свойство VM, **не** входит в
   `HydraulicsState`/снапшоты/`Reset()` и осознанно не сбрасывается при
   загрузке проекта (семантика «настройки сессии», в отличие от
   `CurrentMode`). Примечание (по ревью диффа): в реализации свойство
   названо `IsFullMode` (true = «Полностью») с вычисляемой парой
   `IsCompactView`/`IsFullView` для сегмента — семантика инвертирована
   относительно названия в этом пункте.
2. **Сегмент-контрол «Рабочая / Расчётная»** — пара `RadioButton` (view-local
   стили), сеттеры `IsOperatingMode`/`IsDesignMode` игнорируют `false`
   (`if (value) CurrentMode = …`), `OnCurrentModeChanged` нотифицирует оба —
   иначе снятие выделения пушит `false` в оба свойства и режим «двоится».
   Code-behind табло (`OnOperatingModeClick`/`OnDesignModeClick`) удаляется;
   мёртвые конвертеры `ModeToBackgroundConverter`/`ModeToBorderConverter`/
   `HydraulicModeToVisibilityConverter` удаляются из `Converters.cs`.
3. **Сводка коллектора — `UniformGrid Columns=6`, не VirtualizingWrapPanel.**
   Эталон — fluid-сетка `repeat(6,1fr)` с чипами пропорциональной ширины;
   VWP требует явных `ItemWidth/ItemHeight` (фиксированные ширины ломают
   эталон и конфликтуют с `Aspect.Ratio`), виртуализация при n=6 бесполезна.
   Пакет `VirtualizingWrapPanel` в `src`-csproj в Ф3 **не добавляется** —
   переносится в Ф6 (дашборд Результатов, адаптив 4+4 с ItemWidth). Чипы
   биндят `SelectedCollector.Summary` (живой ObservableObject), а не
   снимки `HydraulicSummaryCards` (канонический read-model для страницы
   Результаты).
4. **Свёртываемые справочные блоки — Expander-шаблон `InfoBlock.Collapsible`**
   в `Components.InfoBlock.xaml` (компонент, без размеров — ADR-006 п.1).
   Контент Expander материализуется сразу при загрузке — биндинги живые и
   в свёрнутом состоянии («данные сохраняются»); UIA-паттерн ExpandCollapse
   доступен smoke-набору. Новый словарь не вводится, порядок мержа
   `App.xaml` и зеркало в `DiRegistrationTests` не меняются.
5. **Типографика уплотнённой таблицы — токены, не литералы.** Задача «11 px»
   трактуется как «уплотнение»: кегль ячеек/заголовков «Полностью» —
   `Font.Size.Caption` (12), уплотнение — высотой строки/паддингами.
   Литерал `FontSize="11"` запрещён ratchet-целью CircuitsView → (0,0).
   Табличные цифры (OpenType tnum) для Inter через WPF `Typography`
   недоступны — канон: правое выравнивание + фиксированные десятичные.
6. **Индикатор пересчёта страницы — бирюзовая ветка**
   (`Color.Border.Processing`/`Status.Info` + `Icon.Clock`); Material-синие
   `#E3F2FD/#2196F3/#1976D2` удаляются из вьюхи. На экране остаются два
   индикатора (страничный `IsCalculating` + каркасный `ShellRecalcMessage`)
   — семантика разведена: «локальный пересчёт модуля» vs «каскад»; рендер
   03 показывает только статус-бар, страничный сохранён по плану Ф3.6.
7. **Отклонения от эталона 03/03b (осознанные):** таб-полоса «Ввод |
   Результаты» из 03 не переносится — замещена сегментом «Компактно |
   Полностью» из 03b (чипы сводки видны всегда); вкладки «Коллектор №1..N»
   в эталоне не изображены (мультиколлекторность) — реализуются таб-полосой
   на ListBox в шапке карточки (TabControl → ListBox + ContentControl,
   DataContext содержимого = `CollectorData` сохранён) + сегментом справа.
   Тулбар: «+ Контур», «+ Коллектор», «− Коллектор» (confirm-семантика
   `_validator` сохранена), «Рассчитать» (`HydraulicsCalculateButton`);
   удаление контура — hover-✕ в строке в обоих режимах.
8. **Kv/тип коллектора/примечание о холодном пуске** (не вошли в 6 чипов)
   переезжают в aux-строку заголовка карточки и подписи свёрнутого
   InfoBlock «Результаты»; `Summary.Warning` остаётся в статус-баре
   каркаса (`ShellValidationMessage`) + ⚠-чип в строке таблицы.
9. **AutomationId-контракт:** `HydraulicsPipeSpacing`/
   `HydraulicsSupplyTemperature`/`HydraulicsReturnTemperature` остаются
   `TextBlock` ровно в одном экземпляре на файл (селекторный контракт
   `ThermalAutomationIdSelectorContractTests`); при переносе в
   свёрнутые блоки ID не дублируются. Остальные `Hydraulics*` ID
   сохраняются как есть (`HydraulicsGlycolType` — якорь UiSmoke).
10. **Терминология:** DpRohr/DpVerteiler/DpVent/DpGesamt/zu_drosseln →
    «Δp трубы / Δp коллект. / Δp клап. / Δp всего / Дросс.» — глоссарий
    `docs/design/glossary-hydraulics.md` (создаётся в Ф3, переиспользуется
    Ф8); тестовых пинов на заголовки колонок в XAML нет.
