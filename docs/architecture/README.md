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

**Внедрено 2026-09-05 (Фаза 4Б):** поле `hasLoads` выпало из `ConstructionData`
формата `.smc`; hash-пин `ProjectSnapshotFactoryTests` обновлён
(FBD2010C… → 0E3545CD…); legacy-чтение старых `.smc`/json-справочников с
`hasLoads`/`has_loads` покрыто тестами (System.Text.Json пропускает
неизвестные поля); правило толщины — всегда 40 мм. Уточнение формулировки
ADR: «hash-пинов» оказалось не множество, а один (см. выше); остальные
правки — контрактные ассерты и сигнатура конструктора снимка (4 → 3 арг).

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

### ADR-008 — 2026-09-06 — Фаза 7 редизайна: полировка и бренд-QA, state ownership без изменений

Независимый read-only ревью плана реализации Ф7 (`docs/design/redesign-plan.md`
§Ф7, «Ревью плана реализации, 2026-09-06» — approve-with-edits) до
реализации. Итог handover: **state ownership без изменений** — слайсы
`ProjectSession`, writer-паттерны R1–R6, `.smc`-контракт и списки
санкционированных writers (ADR-003) не затронуты. Зафиксированные решения
UI-слоя:

1. **Welcome и оверлей расчёта — shell UI-state, не проектное состояние.**
   `MainViewModel.IsWelcomeVisible` (init true; закрытие — первый переход по
   степперу, факт «проект открыт» по `IProjectSession.CurrentFilePath`
   [P2-1: номер в `.smc` бывает пустым], кнопка «Начать работу»; повторное
   открытие — `PerformNewCalculationReset`) и `IsCalculationOverlayVisible`
   (read-only проекция `ThermalViewModel.IsCalculating`; гидравлический
   `IsCalculating` — вычислимое свойство над сервисом без отдельной
   нотификации, в проекцию не включён). Семантика как у `IsSidebarCollapsed`
   (ADR-006 п. аналогично `IsFullMode`): в `ProjectSession`/снапшоты не входят.
2. **Петля компоновки Гидравлики закрыта прецедентом Ф5:** локальный стиль
   `Hyd.SummaryChip` (Ratio=0, Height=96) в CircuitsView — урок №6;
   `KpiChip.Root` (Ratio=2) остаётся каноном компонента для невьюпортных
   зон.
3. **Ratchet/сканер:** зона `ViewTokenHygieneTests` расширена на
   `src/Controls/**/*.xaml` + сеттерная форма `Property="FontSize"
   Value="N"`; allowlist без роста (CircuitsResultsView (0,1)→(0,0) после
   токенизации, хвосты P1-1). Themes сознательно вне зоны сканера.
4. **Сплэш/«О программе» — новые окна, состояния не вводят;**
   `ShutdownMode=OnMainWindowClose` (P2-3) — закрытие сплэша не завершает
   приложение до показа MainWindow/диалога ошибки. UiSmoke находит главное
   окно перебором top-level окон по заголовку (сплэш перехватывает
   `MainWindowHandle`, P2-4).
5. **Анимации переходов — attached property `ContentTransition`** (fade +
   slide 180 мс, свой код; XamlFlair — reject, журнал п.11). Смена Content
   — через `DependencyPropertyDescriptor` (у ContentControl нет публичного
   события ContentChanged); slide не затирает чужой RenderTransform.
6. **Семантика welcome и «Новый расчёт»:** гейтом закрытия welcome служат
   только переход по степперу, смена `CurrentFilePath` и кнопка «Начать
   работу». «Создать новый расчёт» намеренно возвращает welcome-слой, даже
   если `CurrentFilePath` ещё привязан (MarkClean путь не сбрасывает) —
   «пустое состояние при живом файле» осознанное; заголовок окна при этом
   продолжает показывать файл.

### ADR-009 — 2026-09-06 — Фаза 8 редизайна: рендер отчёта мигрирован с QuestPDF на PDFsharp/MigraDoc, state ownership без изменений

Решение владельца (план §Ф8, журнал решений п.11; исследование
research-github-libraries.md): QuestPDF не используется — Community-лицензия
(порог консолидированной выручки <$1M) для REHAU не выполняется. Рендер
краткого PDF-отчёта переписан с QuestPDF 2024.12.3 на PDFsharp-MigraDoc
6.2.4 (MIT). Поверхность: `src/Services/Results/PdfExportService.cs`
(переписан), csproj (−QuestPDF, +PDFsharp-MigraDoc), док-комментарий
`CalculationReportMarkdownRenderer.cs` (упоминание QuestPDF устарело).
`ResultsPdfData`/`ResultsPdfDataBuilder`, `IPdfExportService`,
DI-регистрация и структура ADR-002 (Services → ViewModels.Results) не
менялись; persistence и `.smc`-контракт не затронуты.
Итог handover: **state ownership без изменений**, списки санкционированных
writers (ADR-003) без изменений, `ArchitectureRulesTests` в зелёном прогоне.

Зафиксированные решения:

1. **Терминология и единицы:** заголовки гидравлической таблицы отчёта
   приведены к глоссарию Ф3 («R, Па/м», «Δp трубы/коллект./клап./всего»);
   термины — глоссарий, единицы и значения — старого отчёта (кПа) — смена
   единиц вышла бы за «поблочное соответствие».
2. **Локаль чисел отчёта:** форматирование через `AppCulture.Culture`
   (ru-RU: запятая, пробел-тысячи для значений ≥1000 — N-формат), независимо
   от CurrentCulture машины прогона (старый рендер на не-RU ОС давал точки).
   Пин — `NumberFormat_UsesCanonicalRussianCulture`.
3. **Легаси-дефекты старого рендера не воспроизводятся:** почти пустая
   стр. 2 (вертикальное переполнение дашборда QuestPDF); дашборд помещается
   на стр. 1 — отчёт стал 2-страничным вместо 3-страничного. Двустрочные
   заголовки колонок остаются осознанным отклонением в обе стороны: в
   старом рендере «Скорость, м/с» переносилось внутри слова, теперь — по
   границе слов; четыре глоссарных заголовка Δp («Δp трубы, кПа» и т.д.)
   двустрочные из-за длины терминов при 8pt в колонках 55–65pt (старые
   сокращения «ΔP р-л» были однострочными) — цена терминологии глоссария,
   принята. Микроотклонения от «1:1» (улучшения, приняты): числовые колонки
   «кВт»/«м³/ч» и «Толщ./λ/R» выровнены вправо (в старом — влево);
   подсекции «Исходных данных» делятся 55/45 (в старом 50/50); PNG-схема
   конструкции вписывается по ширине (172pt) без ограничения высоты — для
   типовой широкой схемы эквивалентно старому FitArea. Двойной заголовок
   стр. 2 («Приложение…» + «ГИДРАВЛИЧЕСКИЙ РАСЧЁТ») воспроизведён осознанно
   (поблочное соответствие); выправление — отдельное решение владельца.
4. **Техфакты PDFsharp 6.2 (ревью плана — approve-with-edits, 2×P0):**
   Core-сборка без резолвера шрифтов «из коробки» →
   `GlobalFontSettings.UseWindowsFontsUnderWindows` в static ctor (один раз
   за процесс; production-альтернатива — свой IFontResolver или пакет -WPF
   — не обязательно, решение за владельцем); `ImageSource.FromBinary` в
   официальном 6.x не существует (API форка PdfSharpCore) → fileless
   base64-протокол `AddImage("base64:…")`; A4 landscape после
   `DefaultPageSetup.Clone()` задаётся явными `PageWidth`/`PageHeight`
   (связка `PageFormat`+`Orientation` даёт портрет). Тест-пины «QuestPDF»
   в `CalculationReportModelTests` сохранены как анти-зависимостный guard.

### ADR-010 — 2026-09-07 — Детальный отчёт v2 «Пояснительная записка»: модель шагов расчёта, каноника + контрольный пересчёт, state ownership без изменений

Спецификация `docs/report-spec-v2.md` (согласована владельцем 2026-09-06,
журнал решений в спецификации), план `docs/plans/2026-09-06-report-v2-plan.md`.

Решения:

1. **Источник тепловых величин отчёта** — канонический снимок
   `ProjectSession.ThermalState.Snapshot` (DEC-T01, полный набор runtime-полей);
   fallback — ровно один контрольный пересчёт существующим
   `ThermalCalculator.Calculate` по входам проекта, результат в канонику не
   пишется, dirty не создаётся. Wire-контракт `.smc` (DEC-T08, ADR Ф4) не
   изменяется; persistence не затронута.
2. **Модель документа** — `CalculationStep` (формула → подстановка → результат
   → примечание); подстановки собираются билдером из тех же `ReportValue`,
   что идут в таблицы (Derived); рендер не вычисляет.
3. **Константы расчёта** — единственный источник `Core/Constants/
   ThermalConstants` (R1 переключает `ThermalCalculator` на него, значения не
   меняются); `HydraulicsConstants` как источник для отчёта не используется
   без построчной сверки (значения расходятся с кодом).
4. **Числа отчёта** — каноническая `AppCulture.Culture` (ADR-Ф8 применяет её
   же в PDF-рендере).
5. **UI** — пункты «Markdown — …» переименовываются в «Пояснительная
   записка — …», AutomationId без изменений.

Итог handover: **state ownership без изменений**, списки санкционированных
writers (ADR-003) без изменений, `ArchitectureRulesTests` в зелёном прогоне.

### ADR-011 — 2026-09-07 — Реактивная готовность «Результатов» (`IsDataReady`): триггер — канон, не навигация

Решение владельца: готовность вкладки 5 «Результаты» — чистая функция
канонического состояния; триггер пересчёта — события `Changed` слайсов
сессии, а не навигация. Мотивация: прежняя pull-модель (пересчёт только в
конструкторе и `RefreshAll()` — навигация/экспорт/загрузка) врала в обе
стороны: при заполненных вкладках 1–4 флаг оставался false (круг 5 серый,
PDF-кнопка неактивна) до первого захода на «Результаты», а после захода и
порчи данных (например, инвалидации тепла) оставался true — галочка в
сводке и активная PDF-кнопка при битом каноне; от ложного экспорта спасал
только рантайм-гвард внутри команд.

Поверхность:

1. `ResultsViewModel` (конструктор): подписки на
   `ClimateState/ConstructionState/ThermalState/HydraulicsState.Changed`
   → `OnCanonicalStateChanged` → только `CheckDataReadiness()` (дешёвое
   чтение канонических снимков). Тяжёлая гидратация контента
   (`RefreshAll` → KPI/карточки) остаётся на навигации
   (`LoadHydraulicsDataOnNavigate` из `MainWindow.ResolveView`).
   Подписки без отписок — паттерн `SummaryViewModel` (VM и сессия —
   синглтоны на всё время приложения); все raise-сайты `Changed` — на
   UI-потоке (проверено по координаторам).
2. `MainWindow.xaml.cs`: удалено мёртвое поле `_refreshResultsOnNavigate`
   (ни разу не присваивалось; ветка else в `ResolveView` была
   недостижима) — переход на «Результаты» безусловно вызывает
   `LoadHydraulicsDataOnNavigate()`.

Владение состоянием не меняется: `IsDataReady` остаётся кэшируемым
производным флагом `ResultsViewModel` (инвариант R5), обработчик только
читает канон; списки санкционированных writers (ADR-003) и `.smc`-контракт
без изменений.

Цензус подписчиков слайс-`Changed`: Climate 2 / Construction 2 / Thermal 2 /
Hydraulics 3 (был 1/1/1/2). Замороженный evidence
`docs/architecture-migration/evidence/phase-10-…/slice-1-reactive-census.md`
намеренно не переписывается (provenance, досье — только чтение); живой
guard `ReactiveSubscriptionLifecycleTests` обновлён с пометкой-поправкой
от 2026-09-07.

Принятые наблюдаемые эффекты (осознанные):

1. После «Новый расчёт» статус-бар на «Результатах» сразу показывает
   «Не готовы модули: …» (раньше молчал до следующей навигации/`RefreshAll`).
2. PDF-кнопка (`IsEnabled` ← `IsDataReady`) гаснет в момент инвалидации
   канона, а не при следующем входе на вкладку.
3. Галочка вкладки 5 в сводке («Шаги») появляется без посещения вкладки;
   цвет выбранного шага (Brand.Red) перекрывает «готов» — как и прежде.
4. Транзиентные сообщения статуса с 3-секундным окном («Проект загружен…»,
   сообщения экспорта) могут быть перезаписаны в окне каноническим
   `Changed`, попавшим в этот интервал: реактивный обработчик расширяет
   уже существовавший race через `RefreshAll`-по-навигации — косметика,
   принята.

Тесты: два новых реактивных (обе стороны, без навигации; ассерт
`HydraulicSummaryCards` пуст — обработчик не делает тяжёлой гидратации);
два STA-теста переехали с удалённого делегата-сема на наблюдаемую
поверхность (счёт `Reset` у `HydraulicSummaryCards` = ровно один
`RefreshAll` на вход) и Moq-инъекцию сломанной сессии для отказа refresh;
цензус-тест обновлён.

Верификация: `dotnet build` 0/0; `dotnet test` 2144 passed / 0 failed /
1 skipped (преждесуществующий скип без fixture-файла); `ArchitectureRulesTests`
зелёный без правок; независимая read-only ревизия — oracle, вердикт ниже.

Независимая read-only ревизия (oracle, 2026-09-07): **PASS, блокеров нет**
(14 findings: 11 ok-observations, 3 note). Подтверждено: R5/R2/R3 чисты —
обработчик только читает снапшоты и пишет VM-local observables; подписочный
паттерн 1:1 с `SummaryViewModel`; все raise-сайты `Changed` — UI-поток при
текущем call graph (единственный async-путь — `ThermalStateCoordinator.
CalculateAsync`, continuation на `DispatcherSynchronizationContext`,
`ConfigureAwait(false)` в цепочке нет); оба новых теста падали бы при
прежнем навигационном дизайне; цензус 2/2/2/3 совпадает с фактом.
Note-уровня (действий не требуют): на один flip `IsDataReady` — два
прохода `RefreshShellStatus` (первый читает одноступенчато-устаревший
`StatusMessage`, второй исправляет в том же синхронном UI-блоке —
невидимо); threading-гарантия — by-convention, опциональное укрепление —
`Debug.Assert` на thread affinity в `OnCanonicalStateChanged` (решение за
владельцем).

### ADR-012 — 2026-09-07 — Честная индикация степпера: «галочка = рассчитано и валидно», инвалидация результатов гидравлики при User-мутациях

Решение владельца: индикация вкладок (круги 1–5, галочки сводки «Шаги»,
PDF-гейт) означает «работа вкладки выполнена»: для расчётных вкладок —
расчёт выполнен для текущих данных и результат валиден; правка ввода
после расчёта гасит галочку до пересчёта. Мотивация: прежние условия
красили вкладки 3–4 без факта расчёта (тепло — ✓ с запуска, гидравлика —
✓ всегда, кроме расчёта/ошибок), а валидатор расчёта требовал трубу —
степпер и расчёт говорили на разных языках; пользователь не видел по
индикации, что сделано, а что нет.

Поверхность:

1. **Канон `ProjectSessionHydraulicsState`**: `ReplaceCollectors` при
   `origin ∈ {User, UserReset}` пересобирает кандидат-снапшот — у каждого
   контура `OperatingResult/DesignResult = null`, у каждого коллектора
   `Summary = null`. Прочие origin (ProjectLoad, Initialization,
   SystemApply, Calculation, ProjectLoadReset) — семантика прежняя.
   `FailCalculation` чистит и контурные результаты (раньше только
   Summary). Status не трогается, новых фаз нет, wire-формат .smc не
   меняется. Зануление происходит внутри самого slice-state —
   санкционированного владельца; новых writers нет (R1–R6 целы).
2. **Предикат `HydraulicsStateSnapshotExtensions.IsCalculated()`**:
   коллекторы непусты && у всех `Summary != null` && есть контур с
   `CircuitLength > 0`. Non-null Summary в каноне возникает только через
   `CompleteCalculation`, `Restore(ProjectLoad)` и `ApplyGlobalInputs`
   (сохраняет существующее) — User-путь всегда даёт null.
3. **Статусы**: вкладка 3 — Ready требует `!NeedsRecalculation &&
   _thermalState.Snapshot.Result is { IsValid: true }`; вкладка 4 —
   `IsCalculated()` (после Recalculating/Error-веток); вкладка 5
   (`CheckDataReadiness`) — тот же предикат вместо «длина > 0», так что
   ✓ вкладки 5 = AND(✓ 1–4). Новая подписка
   `MainViewModel → _hydraulicsState.Changed → RefreshShellStatus`
   (User-инвалидация не уведомляет VM-свойства; цензус Hydraulics 3 → 4).
4. **Зеркало грида `CircuitsViewModel.ClearStaleCalculationResults`**:
   при User/UserReset-инвалидации расчётные поля СУЩЕСТВУЮЩИХ строк грида
   очищаются по дельте «было в OldSnapshot, отсутствует в NewSnapshot»
   (сопоставление по номерам, без пересборки коллекции — фокус ячейки
   сохраняется); очищаются `Summary`, `OperatingResult`, `DesignResult` и
   производные расчётные колонки (Power/FlowRate/Velocity/Throttling/
   ValveTurns/ValveTurnsWarning/IsReferenceCircuit) + карточки итогов.
   Если результатов не было и в старом каноне (первый ввод длин) — грид
   не трогается.
5. **Nullable-каскад**: `CircuitRow.OperatingResult/DesignResult` и
   `CollectorData.Summary` стали nullable (дефолт `new()` сохранён —
   свежая строка ведёт себя как прежде); null-safe правки в
   `CircuitsCalculator` (балансировка), `ResultsPdfDataBuilder`,
   `CircuitRow` (CurrentResult/FlowRegimeDescription/TotalLoss_mbar/
   PressureLossWarning). Биндинги WPF толерантны к null — расчётные
   колонки/чипы пустеют до пересчёта.

Владение состоянием не меняется; санкционированные writers без изменений;
`.smc`-контракт: формат прежний (nullable-поля существовали и раньше),
следствие — **устаревшие результаты перестают сохраняться в .smc** после
правки ввода (считается улучшением); старые файлы с результатами
загружаются как прежде (`Restore`/ProjectLoad сохраняет результаты).

Принятые наблюдаемые эффекты (осознанные):

1. При старте приложения вкладки 3 и 4 серые; ✓ появляется только после
   фактического расчёта. Расчёт без трубы → Error (валидатор и степпер
   совпали).
2. Правка длины/коллектора после расчёта: расчётные колонки грида и чипы
   итогов пустеют до пересчёта; вкладки 4 и 5 гаснут реактивно.
3. Провалившийся расчёт (`FailCalculation`) → вкладка **Error** (не
   серый): бейдж HasError виден до успешного пересчёта, канон при этом
   полностью без результатов — семантически честнее серого, запинено
   тестом.
4. Автопересчётные пути (Add/RemoveCircuit, SupplySpacing/SupplyHeat,
   GlycolType/Concentration, обновление тепла/климата) за User-мутацией
   синхронно идут в пересчёт — транзиентный Draft невидим (один UI-тик).
5. Старые .smc со stale-результатами (результат не соответствует входам
   в файле) после загрузки показывают ✓ — как и прежде у тепла; принят.

Тесты: +33 (2177 total): 18 e2e-сценариев `StepStatusHonestyTests` на
production-shaped графе (обе стороны по каждой вкладке, включая «AND(1–4)»,
round-trip .smc, восстановление грида после пересчёта), 6 канонических +
5 параметризованных lifecycle-origin в `ProjectSessionHydraulicsStateTests`,
5 предикатных `HydraulicsStateSnapshotPredicateTests`, цензус-амендант
3 → 4 в `ReactiveSubscriptionLifecycleTests`. Существующие
characterization-тесты семантики не меняли (дельта-семантика очистки
сохранила все 13 сценариев с ручным Summary вне расчётного пути).

Верификация: `dotnet build` 0 ошибок (новых предупреждений нет — 18
преждесуществующих); `dotnet test` 2177 passed / 0 failed / 1 skipped
(скип преждесуществующий); `ArchitectureRulesTests` зелёный без правок;
независимая read-only ревизия (oracle, 2026-09-07) — **PASS, блокеров
нет** (16 findings: 12 ok-observations, 3 note, 1 should-fix = настоящая
запись ADR-012 + урок, обязательная до коммита). Подтверждено: R1–R6
целы, mapper толерантен к null в обе стороны, notification graph
реентерабельно безопасен (подписочный порядок гарантирует обновление
грида/бейджа до shell-refresh), nullability-каскад полон (0 новых
warnings), экспорт PDF дважды защищён (`IsDataReady`-гейт реактивен).
Note-уровня (преждесуществующие пути, диффом сужены, не созданы; действия
не требуют, кандидаты на будущий проход): после `FailCalculation` грид
может показывать частичные результаты при каноне «не рассчитано»
(очистка зеркала на Calculation-origin не расширена); окно нумерации
коллекторов между capture и `RenumberCollectors` при Remove/AddCollector
(существование до этого изменения).

### ADR-013 — 2026-09-07 — Расширение гидравлического снимка полями свойств теплоносителя (гликоля): runtime-каноника, `.smc` не расширяется

Решение владельца (план мини-фазы полировки ПЗ
`docs/plans/2026-09-07-pz-polish-plan.md`, P0/P4, решение В13): свойства
теплоносителя (плотность ρ, теплоёмкость c_p, кинематическая вязкость ν;
теплопроводность λ и число Прандтля Pr) фиксируются в каноническом
снимке `HydraulicsStateSnapshots` для Operating и Design в точке
обновления снимка результатами гидравлического расчёта — те же входы,
что переданы в `CircuitsCalculator` (тип/концентрация из
`HydraulicInputData`, `operatingTemperature`/`designTemperature`).
Мотивация: свойства являются входом гидравлического расчёта
(`CircuitsCalculator` получает их из `GlycolDataService`), но не
сохраняются — `HydraulicsSectionBuilder` вписывал заглушки `0.0` и ПЗ
показывала «нет данных» (диагноз плана, §0 п.4).

Поверхность и правила:

1. **Runtime-каноника, не wire:** новые поля не входят в формат `.smc`
   (DEC-T08 не затронут, аналог DEC-T01 для тепла); persistence и
   round-trip не меняются, старые файлы читаются как прежде. Fallback
   при отсутствии свойств в снимке (файл старой версии) — ровно один
   контрольный вызов `GetProperties(тип, концентрация, T_режима)` по
   входам каноники с примечанием «свойства теплоносителя получены
   контрольной интерполяцией» (образец — §3.2 `docs/report-spec-v2.md`;
   новый подраздел §3.4); результат вызова в канонику не пишется,
   dirty не создаётся; концентрация 0 % → свойства воды; выход за
   диапазон базы → «нет данных» + предупреждение (правило В2).
2. **Owner/writer:** единственный writable owner новых полей —
   гидравлический расчётный пайплайн (заполнение в точке обновления
   снимка). Изменение списка санкционированных writers снапшота
   фиксируется настоящей записью (ADR-003: изменение списка = изменение
   правила); реализация P4 расширяет соответствующие списки тестов
   вместе с кодом.
3. **ПЗ — Derived-читатель:** `HydraulicsSectionBuilder` и экспортные
   провайдеры читают свойства из `ProjectSession.HydraulicsState.
   Snapshot`; билдер сам `GlycolDataService` не вызывает. Пин: снимок ==
   `GetProperties(входы)` (сервис singleton, интерполяция
   детерминирована). Значения констант и входы расчёта не меняются.

Спецификация отчёта дополнена подразделом §3.4 «Свойства теплоносителя»
(`docs/report-spec-v2.md`, журнал В9–В17); словарь обозначений ПЗ —
`docs/design/glossary-report-designations.md`. Реализация — P4 плана.

Итог handover (P0, docs-only): расширение канонического снимка
зафиксировано до реализации; входы/результаты расчётов и wire-контракт
`.smc` не изменяются; изменения state ownership — только описанное выше
добавление полей с единственным writer-пайплайном.

### ADR-014 — 2026-09-08 — «Отменить / Вернуть» (undo/redo, 10 действий): событийный memento-дневник по разделам, state ownership расширен санкционированными писателями

Решение владельца (план
`docs/plans/2026-09-08-undo-redo-plan.md`, §1 — 11 зафиксированных решений):
отмена правок данных в Word-стиле для 4 разделов (Климат, Конструкция,
Тепловой, Гидравлика), глубина 10 действий, кнопки в шапке слева от меню
«Файл» + Ctrl+Z/Ctrl+Y. Механика — вариант Г: **событийный memento-дневник**
`UndoRedoService` (singleton, `src/Services/History/`): сервис слушает
существующие `Changed`-события четырёх срезов (INV-016) и накапливает
записи «одно действие пользователя → per-slice пары (Before, After) всех
затронутых разделов». Дневник снимки не создаёт и каноном не владеет —
R1 не задет.

Расширение state ownership (санкционируется настоящей записью; ADR-003:
изменение списка = изменение правила):

1. **Новые origins `Undo`/`Redo`** во всех четырёх enum
   (`ClimateMutationOrigin`, `ConstructionMutationOrigin`,
   `ThermalMutationOrigin`, `HydraulicsMutationOrigin`). Семантика как у
   `ProjectLoad`: dirty внутри слайсов — НЕТ (списки dirty-origins не
   расширяются), публикация контекста — ДА; `DataChanged`-проекция климата —
   НЕТ (`PublishesCompatibility(Undo/Redo) == false`, прецедент `Load`).
   **Асимметрия зафиксирована:** `PublishesDownstream` конструкции на
   `Undo/Redo` НЕ расширяется — иначе `RaiseDataChanged` вызвал бы
   инвалидацию тепла во время отката конструкции; климат публикует контекст
   при любом changed-origin безусловно, этого достаточно.
2. **Новые методы записи:** `IProjectSessionClimateState.ApplySnapshot(snapshot,
   origin)` — прямое присваивание всех 12 полей снимка (включая
   `HasUserModifications`/`Period0Days`), без нормализации и без
   origin-пересчётов; `IProjectSessionThermalState.RestoreState(snapshot,
   origin)` — атомарное восстановление inputs+result+статуса ИЗ снимка
   (статус может быть `NeedsRecalculation` с сообщением), обёртка
   `IThermalStateCoordinator.RestoreState` с пере-публикацией
   `UpdateThermalInputs`/`UpdateThermal` по образцу `LoadResult`; guard
   `ProjectSessionHydraulicsState.Restore` ослаблен с «только `ProjectLoad`»
   до «`ProjectLoad`, `Undo`, `Redo`». Существующий
   `ThermalState.Restore(inputs, savedResult)` не тронут.
3. **`UndoRedoService.cs` добавлен в allowlists WI-1, WI-3, WI-4, WI-5,
   WI-6** (паттерн WI-1 расширен `ApplySnapshot`, WI-3 — `RestoreState`,
   WI-4 — `Restore`); списки dirty-origins и WI-2/WI-7/WI-8 не меняются.
   Публикация `UpdateHydraulics` из обвязки отката НЕ требуется
   (закрытие ревью-пункта §9.1 плана): читателей данных
   `CalculationContext.HydraulicsResults` в `src` нет — контекст является
   только триггером реактивного каскада и перезаписывается следующим
   расчётом.
4. **Guard-скоуп отката:** откат выполняется под существующим
   `BeginProjectRestore()` (lease реентерабелен). Семантическое расширение
   имени («load-or-undo in progress») фиксируется: все VM-гварды
   `IsLoadProjectInProgress` глушат пользовательские присвоения и dirty во
   время отката без правки каждого гварда. Порядок применения: Climate →
   Construction → Thermal (координатор) → Hydraulics → повторная Hydraulics
   сразу после Thermal (каскад `ContextChanged → CalculateAllCollectors`
   синхронен; трюк `ProjectLoadOrchestrator`).
5. **Правило тотального подавления:** события слайсов при
   `IsLoadProjectInProgress == true` дневником игнорируются полностью,
   независимо от origin (фантомные записи fallback-расчёта загрузки);
   вторая линия — фильтр origins `Undo`/`Redo` (эхо), третья —
   `_isApplying` вокруг всего отката.
6. **Группировка:** user-origins (`User`, `UserReset`, `Template`)
   открывают/пополняют активную запись и форс-закрывают предыдущую;
   `*Invalidation` и `Calculation` при активной записи примыкают к ней;
   `Calculation` вне активной записи открывает отдельную запись «Расчёт»,
   живущую до первой user-мутации или очистки (таймер тишины её не
   закрывает); lifecycle-origins не пишутся. Склейка посимвольного ввода —
   окно тишины 400 мс (закрывает только user-группы). Лимит истории 10
   записей, старейшая вытесняется. `Save` ставит «точку чистоты» — позицию
   в дневнике; `IsDirty` после каждой мутации/отката корректируется
   сравнением с точкой (`UndoRedoService` — новый санкционированный caller
   `MarkDirty`/`MarkClean`); вытеснение точки за лимит честно оставляет
   «изменён».
7. **Гейт расчёта:** `CanUndo/CanRedo == false` при
   `ThermalIsCalculating || HydraulicsIsCalculating` — иначе Ctrl+Z
   посреди `Task.Run` перезаписывается последующим `CompleteCalculation`.
8. **Политика хоткеев (запись правила):** глобальный undo/redo шелла
   перекрывает посимвольный undo текстбокса; `Ctrl+Z`/`Ctrl+Y`
   обрабатываются в `MainWindow.PreviewKeyDown` (tunneling — bubbling
   `KeyDown` перехватывается `TextBoxBase`, а почти все цели отката —
   из текстбоксов). Навигация по экранам, импорт в каталоги, поиск
   городов, экспорт PDF, легаси `construction_*.json`, `ToggleMode` и
   карточка проекта (UI-редактирование удалено с Ф6) в отмену v1 не входят.
9. **Цензус подписок:** дневник подписан на 4 события `Changed`
   срезов; подписки на `IProjectSession.PropertyChanged` НЕТ (карточка
   проекта не в v1). Цензус `ReactiveSubscriptionLifecycleTests` обновлён
   2/2/2/4 → 3/3/3/5. Дневник — память процесса, `.smc` wire не меняется
   (R6 цел).

R1–R6 проверены обновлённым `ArchitectureRulesTests`;
`MutationBoundaryConsolidationTests` дополнен undo-сценариями (новые
origins — lifecycle-подобные, 0 dirty).
