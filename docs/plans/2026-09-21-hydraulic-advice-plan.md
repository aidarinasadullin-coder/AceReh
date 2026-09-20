# План: гидравлические советы с лечением — пункт 2.1 роадмапа post-1.8

> Дата: 2026-09-21. Статус: план на ревью владельца (реализация запущена
> владельцем командой «пункт 2.1 в реализацию» — рекомендации §0.1 вписаны
> как дефолт-решения, практика plan-approval). Основание: роадмап
> [2026-09-20-post-1.8-roadmap.md](2026-09-20-post-1.8-roadmap.md) п. 2.1,
> [Идеи.md](../Идеи.md) №7 (одобрена владельцем 2026-09-20 с условием
> «сделать без грязи»). show-me:
> [show-me-hydraulic-advice.html](show-me-hydraulic-advice.html).
> Условия роадмапа: правила советов — в сервисе на гидравлическом срезе
> (зеркало `ThermalAdviceService`), без записей VM→VM и без новых писателей
> в канон; R1–R6 не затрагиваются; строка «state ownership без изменений» —
> §8. Паттерн-прецедент: план thermal-advice (1.6.0) —
> `src/Services/Thermal/ThermalAdviceService.cs`.

## 0. Суть

Паттерн тепловых советов (1.6.0) переносится на гидравлический срез: при
нарушении лимитов РЕХАУ карточка «Коллекторы» шага «Гидравлика» показывает
блок советов — что нарушено и чем лечить. Лимиты — канон
`HydraulicsConstants` (ADR-016):

| Лимит | Константа | Симптом → лечение |
|---|---|---|
| Потери ≤ 320 мбар | `MaxPressureLoss_Pa`/`_mbar` | разбить контур / типоразмер больше |
| Скорость ≥ 0.5 м/с | `MinVelocity` | типоразмер меньше / меньше шаг — вырастет расход |
| Скорость ≤ 2.0 м/с | `MaxVelocity` | типоразмер больше / разбить контур |
| Длина ≤ 120 м | `MaxCircuitLength_m` | разбить на два контура |

Кнопка **«Применить»** — только для советов, лечащихся одним действием
(смена типоразмера трубы). Советы — слой рекомендаций: ничего не гейтят,
канонического состояния не создают (ошибки валидации остаются гейтом в
shell), как в тепле.

**State ownership без изменений.** Владельцы состояния не меняются,
`.smc`-wire не меняется, allowlist-писатели (`SetPipeSpacing` и др.) не
расширяются. Единственная каноническая мутация, которую инициирует
гидравлика, — «Применить» смены типоразмера через **существующий**
канонический метод `IThermalStateCoordinator.ApplyInputEdit` (тот же
singleton, что использует `ThermalViewModel`); записей VM→VM нет.

### 0.1. Решения-дефолты (рекомендации агента; владелец может переопределить при ревью)

| # | Вопрос | Решение (дефолт) | Альтернатива (выбор за владельцем) |
|---|---|---|---|
| H1 | Канал «Применить» | `IThermalStateCoordinator.ApplyInputEdit(ThermalInputEdit.ForPipe(ThermalPipeSnapshot.FromPipeType(…)))` — тот же канонический метод, которым `ThermalViewModel` применяет правки трубы (`ThermalViewModel.cs:173`); `ApplyInputEdit` ставит `Origin=User`, один dirty-intent (`ThermalStateCoordinator.cs:109-122`). Писатель состояния остаётся прежним (`ThermalState`), гидравлическая VM — только инициатор intent | «Применить» = только переход на тепловой шаг (поле меняет пользователь сам) — теряется суть «лечение одним действием» |
| H2 | Синхронизация UI тепла после «Применить» | `ThermalViewModel.OnCoordinatorCompletion` получает эхо-ветку: `Origin == User` и `After.Inputs.Pipe` ≠ текущая каноническая труба → `SelectedPipe` присваивается под гвардом `_isResetting` (прецедент `ApplyStateSnapshotToAdapter`, `ThermalViewModel.cs:679-704`); гвард `OnSelectedPipeChanged` (`:169`) не даёт эху создать вторую мутацию | Не синхронизировать — тепловая форма покажет старую трубу до пересчёта/undo (рассинхрон UI) |
| H3 | Место показа | Единый блок «Советы» в карточке «Коллекторы» между сводкой KPI-чипов и таблицей контуров; скрыт, когда советов нет. Поля гидравлики — колонки таблицы, «под полем» как в тепле не вставить; блок под сводкой — «под полем потерь» по смыслу | Карточки-подсказки в ячейках таблицы — уплотняет dense-таблицу (Ф3.4), визуальный шум на 12 строк |
| H4 | Группировка | Один совет на правило со списком номеров контуров («в контурах 2, 5»); порядок правил стабильный | Совет на каждый контур — до 12 карточек на коллектор |
| H5 | Потери: какой режим | Проверяются оба режима (рабочая и расчётная температура); в тексте указано, какой превышен («потери (хол.) …»). Прецедент: `CollectorSummary.IsOperatingPressureExceeded`/`IsColdPressureExceeded` оба смотрят на 320 мбар (`CollectorSummary.cs:152-157`) | Только рабочая температура — «холодный» прогон (вязкость выше) молча пройдёт лимит |
| H6 | Дубль `MinVelocity` (0.1 vs 0.5) | Советы читают `HydraulicsConstants` (0.5–2.0 — инженерные лимиты РЕХАУ: воздушные пробки / шум и эрозия). `ValidationConstants.MinVelocity = 0.1` — нижняя граница физической валидации ввода (гейт отчёта, `CalculationReportDataBuilder.cs:251`) — **другая семантика**, в советы не входит. Хвост роадмапа «подтвердить две семантики» закрывается этим пунктом после ревью владельцем | Уравнять константы — сломает существующий гейт отчёта (wire-зона) |
| H7 | Когда показывать | Только после расчёта: строка попадает в факты при `OperatingResult != null && DesignResult != null` (ADR-012: правка ввода инвалидирует результаты — советы гаснут сразу, а не висят на устаревших числах) | Показывать по длине до расчёта — совет «разбить контур» на непосчитанных числах |
| H8 | Поведение после «Применить» | Автопересчёта нет: тепловая зона уходит в `NeedsRecalculation` штатно, гидравлика пересчитывается по своей кнопке «Рассчитать». **Советы после «Применить» сохраняются** — числа остаются по последнему гидравлическому расчёту: thermal-мутация не инвалидирует гидравлические результаты (ADR-012 чистит строки только на `HydraulicsMutationOrigin.User`, `CircuitsViewModel.cs:1079,1120-1169`), а `CalculationContext.ThermalInputs` обновляется лишь при thermal-расчёте. Защита от второго клика (тихий NoChange, когда предложенный типоразмер уже применён и тепловая форма пересчитана): `CanExecute` дополнительно требует, чтобы `SuggestedPipe` отличался от канонической трубы (по `InnerDiameter`). Поведение задокументировано в инструкции (§7) и smoke (§8). Вариант-альтернатива для ревью: гасить советы сразу при thermal-мутации (стейт-гвард) — отложено: советы с числами последнего расчёта информативнее пустого блока | Автокаскад thermal→hydraulics — скрытая цепочка расчётов вне запроса пользователя |
| H9 | Предложение типоразмера | Соседний типоразмер из `PipeType.StandardPipes` по внутреннему диаметру: `VELOCITY_LOW` → ближайший **меньший**, `VELOCITY_HIGH`/`PRESSURE_MAX` → ближайший **больший**; соседа нет (17×2,0 снизу / 25×2,3 сверху) → `SuggestedPipe = null`, кнопки нет, лечение текстом | Фиксированное «труба 17×2,0» из Идеи — неверно, если текущая уже 17×2,0 |
| H10 | Серьёзность | Зеркальный enum `HydraulicsAdviceSeverity` (`Warning`/`Error`); v1 — только `Warning`: слой советов ничего не гейтит, красным говорит shell (решение владельца 2026-09-15, `ThermalAdviceService.cs:45-47`) | Переиспользование `ThermalAdviceSeverity` — гидравлика зависит от `Models.Thermal` |

## 1. Модели (`src/Models/Hydraulics/HydraulicsAdvice.cs`, новый файл)

| Сущность | Состав |
|---|---|
| `HydraulicsAdviceSeverity` | Enum: `Warning`, `Error` (зеркало теплового; v1 использует `Warning`) |
| `HydraulicsAdviceCircuitFacts` | Record: `Number` (int), `Length_m` (double), `Velocity_m_s` (double), `TotalLossOperating_Pa` (double), `TotalLossDesign_Pa` (double). Компактный снимок фактов строки — сервис не зависит от `ObservableObject`-модели `CircuitRow` |
| `HydraulicsAdviceInput` | Record: `Circuits` (`IReadOnlyList<HydraulicsAdviceCircuitFacts>`), `CurrentPipe` (`PipeType?` — каноническая труба из `CalculationContext.ThermalInputs?.Pipe`) |
| `HydraulicsAdvice` | Record: `Id` (строка каталога), `Severity`, `Message` (готовый текст, форматирование по `AppCulture.Culture`), `SuggestedPipe` (`PipeType?`, null = «Применить» не показывается) |

## 2. Сервис (`src/Services/Hydraulics/`, R4-чистые, новые файлы)

| Файл | Содержимое |
|---|---|
| `IHydraulicsAdviceService.cs` | `IReadOnlyList<HydraulicsAdvice> Build(HydraulicsAdviceInput input)` — чистая функция без состояния; null/пустой вход → пустой список (карточка в UI не рендерится) |
| `HydraulicsAdviceService.cs` | Каталог v1 — 4 правила (§2.1); условия — те же неравенства на тех же константах `HydraulicsConstants`, что у shell-индикации (`CircuitsView.xaml:861,894` — 32000 Па; статус-точки) |

### 2.1. Каталог правил v1 (порядок стабильный)

| Id | Условие (на контур) | Текст (лечение) | SuggestedPipe |
|---|---|---|---|
| `PRESSURE_MAX` | `max(TotalLossOperating_Pa, TotalLossDesign_Pa) > MaxPressureLoss_Pa` | «Потери в контуре(ах) N… — X мбар (раб.) превышают 320 мбар: возьмите трубу больше или разбейте контур на два» | сосед-больше (H9) |
| `VELOCITY_LOW` | `Velocity_m_s < MinVelocity` | «Скорость в контуре(ах) N… — X м/с ниже 0,5 м/с (риск воздушных пробок): возьмите трубу меньше или уменьшите шаг укладки — вырастет расход» | сосед-меньше |
| `VELOCITY_HIGH` | `Velocity_m_s > MaxVelocity` | «Скорость в контуре(ах) N… — X м/с выше 2,0 м/с (шум, эрозия): возьмите трубу больше или разбейте контур на два» | сосед-больше |
| `CIRCUIT_LENGTH_MAX` | `Length_m > MaxCircuitLength_m` | «Длина контура(ов) N… — X м превышает 120 м: разбейте контур на два» | нет (кнопки) |

Детали: границы «равно лимиту» — не нарушение (строгие неравенства, как у
`IsPressureLossPerMeterExceeded`); номера контуров и значения — через
`AppCulture.Culture` (каноническая запятая, прецедент
`ThermalAdviceServiceTests.Build_ReturnNegative_MessageGuidesSupplyDown_WithCanonicalComma`);
сосед ищется по `InnerDiameter` среди `PipeType.StandardPipes`
(внутр. Ø 13 / 16 / 20.4 мм); при `CurrentPipe == null` советы по скорости
и потерям строятся без «Применить» (лечение — только текстом, без конкретного типоразмера).

## 3. DI и `CircuitsViewModel` (адаптер, канон не трогает)

| Правка | Содержимое |
|---|---|
| `src/Configuration/ServiceCollectionExtensions.cs` | Рядом с `IThermalAdviceService` (`:316`): `AddTransient<IHydraulicsAdviceService, HydraulicsAdviceService>()` |
| `CircuitsViewModel` ctor (`:1175`) | +2 **опциональных** параметра в хвосте: `IHydraulicsAdviceService? adviceService = null` (null → `new HydraulicsAdviceService()`, прецедент `ThermalViewModel.cs:436-444` — точки конструирования тестов не ломаются), `IThermalStateCoordinator? thermalCoordinator = null` (singleton DI даст тот же экземпляр, что у `ThermalViewModel`; null → кнопки «Применить» неактивны — изолированные композиции без координатора продолжают работать) |
| Проекция советов | `IReadOnlyList<HydraulicsAdvice> AdviceList => AdviceService.Build(снимок фактов SelectedCollector)`; факты — активные строки с `OperatingResult != null && DesignResult != null` (H7), `CurrentPipe` — из `_calculationContext.ThermalInputs?.Pipe` |
| Точки обновления | `OnPropertyChanged(nameof(AdviceList))`: смена `SelectedCollectorIndex`/`CurrentMode`; в `NotifyThermalPropertiesChanged` (`:1646` — смена трубы/температур из тепла); в хвосте расчёта рядом с `RebuildHydraulicSummaryCards` (587, 635, 883, 961, 1163); в обработчике правки строки, где результаты инвалидируются (советы гаснут, H7) |
| Команда | `[RelayCommand] ApplyAdvice(HydraulicsAdvice)`: `CanExecute` = `thermalCoordinator != null && advice.SuggestedPipe != null && !IsCalculating && !_calculationStateService.IsLoadProjectInProgress && SuggestedPipe != каноническая труба` (последнее — по `InnerDiameter`; защита от второго клика, H8; пересчёт `NotifyCanExecuteChanged` вместе с пересборкой `AdviceList`); `Execute` = `thermalCoordinator.ApplyInputEdit(ThermalInputEdit.ForPipe(ThermalPipeSnapshot.FromPipeType(advice.SuggestedPipe)))` |
| Предостережение | Канал «Применить» — **только** `IThermalStateCoordinator.ApplyInputEdit`; обходной вызов `ProjectSession.ThermalState.ApplyInputEdit` из `CircuitsViewModel` уронит `ArchitectureRulesTests` R2/WI-3 (ThermalState — единственный писатель тепловых входов, allowlist не расширяется) |

R1–R6: `ProjectSession` не трогается (гидравлическое состояние читается
как прежде), зависимостей от чужих VM нет, канон-писатели не расширяются —
`ApplyInputEdit` существует и уже является единственной точкой мутации
тепловых входов; `.smc`-wire не меняется.

## 4. `ThermalViewModel` — эхо-синхронизация (H2)

`OnCoordinatorCompletion` (`:650`): после существующих веток — если
`mutation.Origin == ThermalMutationOrigin.User` и труба из
`mutation.After.Inputs.Pipe` (резолв `ThermalPersistenceMapper.ResolveStandardPipe`
в `AvailablePipes`) отличается от текущего `SelectedPipe` → присвоить под
гвардом `_isResetting` (эхо: `OnSelectedPipeChanged` гвардится `:169`,
второй мутации не создаёт; при равенстве присвоение — no-op без события).
Без этого тепловая форма после «Применить» с гидравлики показывала бы
старую трубу до undo/пересчёта.## 5. UI (`src/Views/Hydraulics/CircuitsView.xaml`)

| Правка | Содержимое |
|---|---|
| Сетка карточки «Коллекторы» | Добавить четвёртый `RowDefinition` в сетку карточки (сейчас три, `CircuitsView.xaml:510-514`); вставить строку между сводкой KPI-чипов (`Grid.Row="1"`, `:574`) и таблицей (`Grid.Row="2"`, `:632`): блок «Советы» получает `Row=2`, таблица смещается в `Row=3` |
| Блок «Советы» | `ItemsControl` с `ItemsSource="{Binding AdviceList}"`, Visibility по непустому списку (прецедент `StringToVisibilityConverter`); стиль строки — локальный ресурс вьюхи (зона `src/Views` держит стили локально, гейт `ViewTokenHygieneTests` сканирует токены): иконка `Icon.Warning` на `Status.Warning.Brush` + текст `TextWrapping` (цвет `Status.Warning.Dark.Brush` — как советы в `ThermalView.xaml:155`) + кнопка «Применить: <Name>» (`Button.Secondary`, подпись из `SuggestedPipe.Name` — «RAUTHERM S 25x2,3», короткий формат без Ø-суффикса DisplayName; `Command="{Binding DataContext.ApplyAdviceCommand, …}"`, `CommandParameter="{Binding}"`, Visibility при `SuggestedPipe != null` + CanExecute, ToolTip «Меняет типоразмер на шаге „Тепловой расчёт“ — после применения пересчитайте оба шага») |
| AutomationId | `HydraulicsAdviceList` (ItemsControl), `HydraulicsAdviceApply` (кнопка) — прецедент контракта `ThermalAdviceSupplyHint` |

## 6. Тесты

| Файл | Тесты |
|---|---|
| `tests/.../Hydraulics/HydraulicsAdviceServiceTests.cs` (новый) | Чистый вход → пусто; каждое из 4 правил: внутри лимита → нет совета, на границе (равно) → нет, за лимитом → совет с текстом лечения; потери: превышение только в холодном режиме → совет с «(хол.)»; склейка: два контура нарушают одно правило → один совет с обоими номерами; порядок правил стабильный; `CurrentPipe` = 17×2,0 + `VELOCITY_LOW` → `SuggestedPipe == null` (соседа нет); `CurrentPipe` = 20×2,0 + `VELOCITY_LOW` → сосед 17×2,0; `VELOCITY_HIGH` при 20×2,0 → сосед 25×2,3; `CIRCUIT_LENGTH_MAX` → `SuggestedPipe == null`; `CurrentPipe == null` → советы без «Применить»; культура: каноническая запятая независимо от `CurrentCulture` |
| `tests/.../ViewModels/Hydraulics/CircuitsViewModelTests.cs` (дополнить) | `AdviceList` пуст до расчёта (результаты строк null → строки не входят в факты, H7); после подстановки результатов с нарушением → совет выбранного коллектора; смена `SelectedCollectorIndex` пересобирает список; `ApplyAdviceCommand.CanExecute`: без координатора / `SuggestedPipe == null` / `IsCalculating` → false; `Execute` с Moq-координатором → проверен вызов `ApplyInputEdit` со снимком `Field == Pipe` и нужным `InnerDiameter` |
| существующие гейты | Полный `dotnet test`; `ArchitectureRulesTests` (новый сервис — R4-скан автоматически); `ManualVersionGateTests` — после пересборки инструкции |

Сеть и файлы в тестах отсутствуют; `IThermalStateCoordinator` — Moq.

## 7. Инструкция пользователя

`docs/manual/src/template.html`, раздел «Гидравлика»: абзац про блок
«Советы» — когда появляется (после расчёта), какие лимиты проверяет
(320 мбар, 0.5–2.0 м/с, 120 м), что делает кнопка «Применить» (меняет
типоразмер трубы на шаге «Тепловой расчёт»; после неё пересчитать тепловой
и гидравлический шаги), почему советы исчезают при правке ввода.
Пересборка `python docs/manual/build_manual.py`.

## 8. Совместимость и риски

- **`.smc`: не меняется.** `settings.json`/AppSettings: не трогаются.
- Writers-канон: `SetPipeSpacing`-allowlist (`CalculationStateService.cs:179-192`) не расширяется; «Применить» идёт через `IThermalStateCoordinator.ApplyInputEdit` — существующую точку мутации тепловых входов (тот же метод, что у правок трубы из тепловой формы).
- Риск петли эха (H2): «Применить» → `Completion` → эхо-присваивание `SelectedPipe` под `_isResetting` → гвард не пускает в `ApplyInputEdit`; вторая ветка защиты — присвоение при равенстве не создаёт события. Тестовая реплика: эхо при внешней Pipe-мутации не порождает второй `ApplyInputEdit` (Moq-координатор, `Verify` … `Times.Once`).
- Поведение советов после «Применить» (H8): советы сохраняются с числами последнего гидравлического расчёта до пересчёта; кнопка деактивируется, когда предложенный типоразмер уже канонический. Ручной smoke: после «Применить» советы висят до пересчёта — ожидаемо; после правки длины строки советы гаснут (ADR-012); после теплового пересчёта без гидравлического — блок показывает смешение (новая труба в `CurrentPipe`, старые числа фактов) до гидравлического пересчёта.
- Риск устаревших советов: закрыт H7 (факты только при ненулевых результатах обеих температур).
- R-инварианты: R1–R6 не задеты; ADR не требуется.
- Ручной smoke (на handover): контур с длиной > 120 / скоростью вне 0,5–2,0 / потерями > 320 → советы по одному правилу; «Применить» меняет трубу в тепловой форме (эхо) и помечает пересчёт; советы сохраняются до пересчёта (H8 — ожидаемо); правка длины строки гасит советы; переключение табов коллекторов меняет список.

## 9. Процесс

1. Атакующее ревью плана (subagent, вектора A1–A8, урок №26) → чек
   `docs/reviews/2026-09-21-hydraulic-advice-plan-review.md`,
   `REVIEW_ID: R-2026-09-21-03` (серия дня: -01 recent-projects,
   -02 updates-whatsnew).
2. show-me регенерируется из плана (урок №27); сверка — шаг ревью (A6).
3. Реализация §1–§5, тесты §6 — запуск владельца получен
   («пункт 2.1 в реализацию», 2026-09-21); рекомендации §0.1 — дефолт-решения.
4. Полный зелёный `dotnet test`; пересборка инструкции (§7).
5. Handover — незакоммиченное дерево (вместе с незакоммиченным 1.3);
   решение по хвосту H6 (две семантики `MinVelocity`) — строка для
   владельца в handover-сообщении.
