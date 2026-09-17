# План: чистка раздутых тестов — четыре волны (разрезка класса-накопителя, мёртвый F5-тест, кластер дубликатов, общий builder фикстуры)

> Дата: 2026-09-18. Статус: **ожидает сигнала владельца** — решения V1–V4 не
> получены. show-me: [show-me-test-suite-slimming.html](show-me-test-suite-slimming.html).
> Основание: ручной скан раздутости тестов 2026-09-18 (после волн
> дедупликации 1a–3): `tests/` — 172 файла, 62 154 строки против
> `src/` — 260 файлов, 44 581 строка (1.39:1); ~2 360 тестовых методов,
> средний размер ~26 строк/тест — набор в целом здоровый, избыток
> сконцентрирован в нескольких файлах. Характеристика-тесты фазы 10
> (`ThermalMultiplicityCharacterizationTests` — 1 939 строк,
> census `HandlerCounts_MatchPhase10Census…` — ~274 строки) — **не
> раздутость, в скоуп не входят**.

## 0. Суть

Один файл — `tests/ViewModels/ResultsViewModelOpenProjectTests.cs`
(3 249 строк, 45 тестов, 15 локальных `Create*`-фабрик) — накопил минимум
пять несвязных тем: промпты открытия, round-trip через
`ResultsViewModel`, восстановление `LoadProjectDataAsync`, отказоустойчивость
persistence, сохранение-снапшоты. Внутри него — мёртвый груз: F5-тест на
645 строк, который **скипается при каждом прогоне** (fixture
`D:\IA\ace\Тест\тест 40.smc` отсутствует; даже при наличии файл лежит вне
репозитория — в CI тест не выполняется никогда), и кластер из пяти
тестов об одном взаимодействии «грунтовые воды ↔ λ ↔ override». Системно:
инлайн `new ProjectData` в 21 файле (19× в файле-рекордсмене, 11× в
`Project/ProjectRoundTripTests.cs`) при почти пустом `tests/Fixtures/`.

Принцип всех волн: **тесты не ослабляются** — ни один живой assert не
удаляется, не переносится в комментарий и не заменяется на более слабый;
пин каждой волны — grep-инвентаризация `Assert\.` до/после (счётчик по
файлу не уменьшается) + полный зелёный `dotnet test`. Характеристика, а
не рефакторинг: если при разрезке обнаружится скрытое расхождение копий —
расхождение фиксируется как находка ревью, поведение не «чинится» молча.

### Вопросы владельцу (рекомендации агента; решения фиксируются здесь)

| # | Вопрос | Рекомендация |
|---|---|---|
| V1 | Порядок волн | **(а)** волны A+B одним циклом (разрезка и удаление мёртвого груза в одном файле), C+D — следующим; диффы волн не смешиваются |
| V2 | F5-тест (645 строк, всегда `Assert.Ignore`) | **(а)** удалить: проверка значений реального файла остаётся ручной F5-процедурой владельца, как и была; (б) перенести в `UiSmoke/` с репозиторным fixture (требует коммита реального `.smc` с данными проекта); (в) оставить как есть |
| V3 | Round-trip-блок (~820 строк через `ResultsViewModel`) | **(а)** отдельный файл `ResultsViewModelRoundTripTests.cs` — путь через ViewModel отличается от сервисного; (б) слить в `Project/ProjectRoundTripTests.cs` |
| V4 | Общий builder `ProjectData` | **(а)** `tests/Fixtures/ProjectDataBuilder.cs`, переводим только двух крупнейших потребителей (19+11 инлайнов), остальные 19 файлов — по мере касания; (б) перевести все 21 файл одной волной |

---

## 1. Волна A — разрезка `ResultsViewModelOpenProjectTests.cs` по темам

**Поверхность: 1 файл → 5 файлов** в `tests/ViewModels/`, только перенос
кода: класс, `[SetUp]`, фабрики `Create*` делятся по темам; `[Apartment(STA)]`
сохраняется в каждом. Широкая многофайловая поверхность → план проходит
независимое атакующее ревью до имплементации, чек в `docs/reviews/`.

| Новый файл | Тесты (строки в текущем файле) | Объём |
|---|---|---|
| `ResultsViewModelOpenProjectPromptTests.cs` | `OpenProject_LeavesGlobalCatalogReadOnly…` (:85), `OpenProject_WhenDirty_ShowsReplacePrompt` (:129), `OpenProject_WhenClean_DoesNotShowPrompt` (:156), `OpenProject_WhenDirtyAndUserPicksNo…` (:180), `LoadProjectFromPathAsync_×4` (:2317–2373) | ~250 строк, 8 тестов |
| `ResultsViewModelLoadRestoreTests.cs` | `LoadProjectData_SelectsFirstCollector…` (:208), `…RestoresCityAndClimateParameters` (:390), `…SyncsClimateToSingletonData` (:448), `ResultsPdfData_UsesCircuitRowThrottling…` (:1381), `LoadProjectData_*` ×7 (:2392–2742) | ~740 строк, 12 тестов |
| `ResultsViewModelSaveSnapshotTests.cs` | `SaveCurrentProject_PersistsClimateStateSnapshot…` (:602), `…ConstructionStateSnapshot…` (:678), `…ThermalStateSnapshot…` (:2830), `…AndPdfExport_TriggerZeroThermalCalculatorCalls` (:2884), `SaveProject_Success_StampsDates…` (:3141), `SaveProject_Failure…` (:3199) | ~550 строк, 6 тестов |
| `ResultsViewModelRoundTripTests.cs` (решение V3) | `ProjectRoundTrip_PipeSelectionRestored` (:258), `…DoesNotMarkDirtyOnLoad` (:296), `…CitySurvivesRealSaveLoad` (:508), `…FieldCompleteRoundTrip_SecondLoadReplacesProjectA` (:748), `ProjectFileService_RoundTripPreservesSchemaVersionAndJsonShape` (:876), `…LiveMutationsAreSavedLoadedAndExported…` (:916) | ~820 строк, 6 тестов |
| `ResultsViewModelHydraulicsSummaryCardsTests.cs` | `LoadProject_TwoCollectors_RestoresIndependentSummaryCards` (:1243), `Reset_ClearsHydraulicSummaryCards` (:1429), `EmptyHydraulics_ZeroesKpisAndCards` (:1548) + рефлексия-хелперы `GetDoubleProperty`/`GetIntProperty`/`AssertCardValues` едут сюда единственными копиями | ~430 строк, 3 теста |

Тема теста не меняется: имена методов, ассерты, fixture-данные
переносятся дословно; `[TestFixture]`-классы получают имена файлов.

## 2. Волна B — мёртвый F5-тест (решение V2)

| Файлы | Правка |
|---|---|
| `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` (:1672–2316, 645 строк) | **(а, рекомендация)** удалить целиком. Обоснование: зависит от абсолютного пути `D:\IA\ace\Тест\тест 40.smc`; файла нет → `Assert.Ignore` на каждом прогоне (единственный `Assert.Ignore` во всём наборе); в CI невыполним по построению; ожидаемые числа (22 700 / 20 700 Вт, 1187.93 л/ч…) жёстко зашиты и сломаются при любом изменении fixture. Проверка значений реального проекта остаётся ручной F5-процедурой владельца. **(б)** при V2(б): тест переезжает в `UiSmoke/`, путь — относительный fixture из `tests/Fixtures/`, файл-фикстура коммитится отдельным решением владельца |

## 3. Волна C — кластер lambda/groundwater (5 тестов → 2)

Строки :1079–1242 (~164 строки) — пять тестов об одном взаимодействии
`GroundwaterLevel ↔ Lambda ↔ override-флаг`, каждый тянет свой инлайн
`ProjectData`:

- `ProjectRoundTrip_PreservesGroundwaterLevel` (:1079)
- `ProjectRoundTrip_PreservesLambdaValueAndOverrideFlag` (:1107)
- `ProjectRoundTrip_OverrideLambdaSurvivesGroundwaterLevelChange_AfterLoad` (:1141)
- `GroundwaterLevelChange_AfterProjectLoad_UpdatesLambdaForBelowPipeLayers` (:1178)
- `ProjectRoundTrip_LambdaUpdatesWhenGroundwaterLevelChanges` (:1211)

Сжатие: один round-trip-тест сохранения (параметризованный:
groundwater / lambda / override-флаг — `[TestCase]` на тройку полей) и
один интеракционный (override переживает смену groundwater после load +
λ пересчитывается для слоёв ниже трубы). Ассерты всех пяти сохраняются в
консолидированных телах; инвентаризация assert'ов до/после — пин волны.
Оценка: −70…−80 строк, покрытие не сужается.

## 4. Волна D — общий builder фикстуры `ProjectData` (решение V4)

| Файлы | Правка |
|---|---|
| `tests/Fixtures/ProjectDataBuilder.cs` (новый) | Fluent-builder типового `ProjectData` (номер, климат, конструкция, thermal-результат, гидравлика, коллекторы A/B) с дефолтами, повторяющими текущие инлайны;readonly-данные — обычными свойствами-инициализаторами. Аналог по назначению: `CalculationReportTestProjectFactory` (волна 3 дедупликации), но для project-домена |
| `ResultsViewModelRoundTripTests.cs` (19→после разрезки) и `Project/ProjectRoundTripTests.cs` (11 инлайнов) | Инлайновые `new ProjectData { … }` переводятся на builder; различия фиксируются именованными override'ами, не копипастой. Остальные 19 файлов с инлайнами не трогаются в этой волне (V4(а)) |

Оценка: −120…−180 строк; builder не попадает в canonical-слой (только
тестовая инфраструктура), state ownership не затрагивает.

## 5. Совместимость и риски

- **State ownership без изменений**: волны правят только тестовый
  проект; `ArchitectureRulesTests` остаются зелёными без правок
  allowlist'ов; `docs/architecture/` и ADR-журнал — без изменений
  (запись «state ownership без изменений» в handover).
- **Тесты не ослабляются**: пин волны — grep-инвентаризация `Assert\.`
  и имён тестов до/после: набор имён меняется только по воле V2/V3
  (удаление F5, слияние кластера), счётчик assert'ов в каждой теме не
  уменьшается. Ни один assert не правится вручную.
- **Characterization-фактура фазы 10** (`ThermalMultiplicityCharacterizationTests`,
  `ReactiveSubscriptionLifecycleTests`, census) — вне скоупа; трогать
  только по отдельному решению владельца.
- **Диффы волн не смешиваются**: A+B — один цикл (V1(а)), C и D —
  следующие; каждая волна завершается полным зелёным `dotnet test` и
  handover некоммиченным деревом.
- **Risk разрезки**: `[SetUp]`/фабрики делятся по фактическому
  использованию; тест, случайно потерявший фабрику, не скомпилируется —
  риск ловится сборкой, а не прогоном.

## 6. Процесс

1. Решения владельца V1–V4 — фиксируются в §0 (имплементация не
   начинается до сигнала).
2. Независимое атакующее ревью плана (материальная поверхность: разрезка
   на 5 файлов + новая инфраструктура fixture) — чек по шаблону
   `docs/agents/review-receipt-template.md` в `docs/reviews/`; show-me
   регенерируется из плана (урок №27), сверка артефакта с планом —
   обязательный шаг ревью.
3. Волна A+B (текущий цикл по V1(а)): разрезка, удаление F5 → grep-пин
   assert'ов → полный зелёный `dotnet test` → handover некоммиченным
   деревом.
4. Волна C, затем D — следующими циклами, каждый с зелёным прогоном.
5. Метрика успеха: `tests/` 62 154 → ~61 300 строк (−850…−900);
   файл-рекордсмен исчезает (максимум файла набора ≤ ~950 строк);
   число тестов 45 → 44 (только V2(а)); число `Assert.Ignore` → 0.
