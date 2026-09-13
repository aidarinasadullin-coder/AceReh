# Реестр «правило → проверка»

> Одна строка на правило: ID / формулировка / первоисточник / тип проверки /
> как проверить (план docs/plans/2026-09-12-process-gates-plan.md, решение 5).
> Грамматика колонки «Как проверить»: при типе `test` — точные имена
> тест-классов/методов через запятую, без прозы (по ним работает
> reflection-резолв верификатора); проза допустима только при `script` и
> `review-step`. Новое правило получает строку в том же коммите, что и само
> правило, — верификатор ловит пропуски. Не всё машинно проверяемо — и это
> фиксируется честно (review-step).
>
> Верификатор: `RulesRegistryTests` — (A) каждый test-токен существует в
> тестовой сборке; (B) каждая ADR-запись `docs/architecture/README.md`
> упомянута в реестре; (C) каждый урок `lessons.md` с заголовком
> «правило владельца» / «Урок №N» и каждый номер №N в заголовках покрыт.

| ID | Правило | Первоисточник | Проверка | Как проверить |
|---|---|---|---|---|
| ARCH-R1 | `ProjectSession` — aggregate root с четырьмя явными слайсами | AGENTS.md «Architecture invariants» (R1), ADR-001 | test | R1_ProjectSession_IsAggregateRoot_WithFourExplicitSlices |
| ARCH-R2 | каждое значение — ровно один канонический writer (WI-1…WI-7); списки меняются только через ADR | AGENTS.md (R2), ADR-003, ADR-014 | test | R2_ClimateState_MutatedOnlyBySanctionedWriters, R2_ConstructionState_MutatedOnlyBySanctionedWriters, R2_ThermalState_MutatedOnlyBySanctionedWriters, R2_HydraulicsState_MutatedOnlyBySanctionedWriters, R2_SessionDirtyAndIdentity_MutatedOnlyBySanctionedWriters, R2_CalculationContextProjection_WrittenOnlyBySanctionedWriters |
| ARCH-R3 | ViewModels — WPF-адаптеры, мутируют только свой слайс, не канонические сторы | AGENTS.md (R3) | test | R3_ViewModels_MutateOnlyTheirOwnSlice |
| ARCH-R4 | Services не зависят от ViewModels; исключение — Results-билдеры | AGENTS.md (R4), ADR-002 | test | R4_Services_DoNotDependOnViewModels, R4_Services_ViewModelsUsings_OnlySanctionedResultsBuilders |
| ARCH-R5 | Results — производная, не владеет входами модулей | AGENTS.md (R5) | test | R5_Results_IsDerivedProjection_DoesNotOwnModuleInputs |
| ARCH-SMC | wire-совместимость `.smc` (R6): hash-пин, round-trip, runtime-каноника без расширения формата | AGENTS.md (R6), ADR-005, ADR-013 | test | ProjectSnapshotContractTests, ProjectSnapshotFactoryTests, ProjectFileServiceResultTests |
| ARCH-SUBS | реактивные подписки не текут: цензус подписок при жизненном цикле | ADR-011 | test | ReactiveSubscriptionLifecycleTests |
| ARCH-READY | готовность «Результатов» (`IsDataReady`) триггерится каноном, не навигацией | ADR-011 | test | ResultsStabilizationPhase1ContractsTests |
| UI-STEP | честная индикация степпера: User-мутации инвалидируют гидравлику, галочка = рассчитано и валидно | ADR-012 | test | StepStatusHonestyTests |
| ARCH-UNDO | undo/redo — событийный memento-дневник по разделам; склейка, окно тишины, orphan-буфер | ADR-014, урок №21 | test | UndoRedoServiceJournalTests, CitySelection_GroupsIntoOneEntry_WithClimateThermalAndHydraulics, HeaderCalculate_OpensStandaloneCalculationEntry_AndClosesOnSilence, PerCharacterEdits_AreStitchedIntoSingleEntry, UndoThenEdit_KillsRedo, Save_SetsCleanPoint_UndoBackToSavedState_ClearsDirty |
| ARCH-XAML | ratchet-гигиена токенов XAML: только канон `Tokens.Colors.xaml` | ADR-006, ADR-007, ADR-008 | test | ViewTokenHygieneTests |
| ARCH-AUTID | AutomationId-контракты UI-элементов закреплены тестами | контракт AutomationId (Thermal) | test | ThermalAutomationIdSelectorContractTests |
| ARCH-CULT | культура WPF-биндинга числовых полей закреплена (ru-RU пин на запятую) | урок 2026-09-04 (культура биндинга) | test | RussianNumberCultureTests |
| ARCH-DEDUPE | ratchet `baseline_refactor_dedupe.json`: дедупликация не откатывается | baseline-файл фазы дедупликации | test | ThermalBaselineTests, CircuitsBaselineTests |
| CONSTR-UGV | семантика УГВ при жизненном цикле проекта и шаблонах | ADR-004 | test | ConstructionDefaultStateInitializerGroundwaterTests, ConstructionServiceTests, ConstructionViewModelTests |
| REPORTS | PDF-отчёты: рендер на PDFsharp/MigraDoc; ПЗ v2 — модель шагов расчёта и контрольный пересчёт | ADR-009, ADR-010 | review-step | экспорт PDF/ПЗ проверяется при handover фаз отчётности (скрины UiSmoke — локально); регрессии структуры ловятся ревью |
| REPO-HYGIENE | docs/workspace/ и publish/ и output/ вне git; сборка сетапа напрямую + sanity-чек перед handover релиза | уроки 2026-09-06 и 2026-09-08 | script | по `.gitignore`: каталоги docs/workspace/, publish/, output/ игнорируются; sanity-чек — локальная сборка и запуск сетапа |
| RULE-VER | каждый новый инсталл — новая версия; одна каноническая строка в девяти местах | урок №20 (правило владельца: каждый новый инсталл — новая версия) | test | VersionSyncTests |
| RULE-SHOWME | план сопровождается визуализацией show-me, владелец открывает артефакт; артефакт регенерируется из плана и не правится вручную | урок №21, урок №27, AGENTS.md «Review» | review-step | шаг ревью плана: show-me рядом с планом (`docs/plans/show-me-*.html`), сверка артефакта с планом и каноном токенов (вектор A6) |
| RULE-CLIMATE22 | UI-вход публикуется в каноническое состояние; симметрия вкл/выкл мутаций; зона — производная температуры | урок №22 | test | ApplyIndividualEdit_IsHighRequirements_Off_RestoresCityAutomatics, ApplyCitySelection_ResetsHighRequirements_NewCityStartsFromAutomatics, ApplyIndividualEdit_AirTemperature_ZoneFollowsTemperature, ApplyProjectSnapshot_LegacyM20Plus_ZoneNormalizedToTemperature |
| RULE-BIND22 | цепочка «контрол → VM → мутация состояния → Changed» проверяется у каждого TwoWay-биндинга | урок №22 | review-step | code review биндингов XAML: у двусторонней привязки есть обработчик и мутация канона (тест этого не ловит — контрол выглядел живым) |
| RULE-SMOKE23 | срабатывание пунктов меню автоматизация не проверяет (Popup-HWND) — финальный smoke выполняет владелец | урок №23 | review-step | ручной smoke владельцем: клик по новому пункту меню в реальной сборке |
| RULE-UI24 | производные UI-поля форс-синхронизируются в Reset/Undo/Load; парс после среза знака — без знакового флага | урок №24 | test | Reset_AfterInvalidEntry_ClearsErrorAndResyncsField, SurfaceTemperatureEntry_DoublePlus_IsRejected |
| RULE-JSON25 | snake_case JSON-каталоги — `[JsonPropertyName]` на всех ключах; пин реального каталога, не дефолтов | урок №25 | test | RealProductCatalog_BindsArticlesFromJson |
| RULE-ATK26 | независимое ревью — атакующий режим (вектора A1–A8); журнал качества ведётся; 3 нуля подряд → ротация векторов | урок №26, AGENTS.md «Review» | review-step | чек ревью с находками в `docs/reviews/`; записи в `docs/agents/quality-log.md` (проверка A8); счётчик нулей — сигнал ротации |
| RULE-AB28 | спорные проектные решения плана — минимум два варианта с явным выбором владельца | урок №28 | review-step | вектор A7 ревью: единоличный выбор агента — находка P2 |
| RULE-ID29 | перечни идентификаторов (REVIEW_ID/ADR/уроки) — полностью, без тире-шорткатов | урок №29 | review-step | вектор A3 ревью: существование каждого ID сверяется с `docs/reviews/` |
| RULE-CI | CI на каждый push (Release + Architecture + полный набор без UiSmoke); зелёный прогон — условие готовности handover; красный master блокирует старт фаз; флейки не отключаются молча | AGENTS.md «Review» (промоция 8d70d27), план process-gates реш. 12/14 | review-step | статус последнего прогона во вкладке Actions на последнем коммите; падение — по флейк-политике, отключение только с записью |
| RULE-CRLF | файловые пины и парсеры — CRLF-стойкие: построчные проверки/нормализация концов строк, без якорей `$` по сырой строке | урок №30 | review-step | зелёный CI на windows-latest (чекаут autocrlf=true); локальная симуляция — клон с `-c core.autocrlf=true` + Release-прогон |
| RULE-OWNERREV | нет коммита без owner-ревью; handover — некоммиченное зелёное дерево | AGENTS.md «Review» | review-step | перед handover: `git status` показывает незакоммиченные изменения, тесты зелёные |
| RULE-WRITERS | списки санкционированных writers меняются только через ADR-запись | AGENTS.md, шапка `ArchitectureRulesTests` | review-step | изменение allowlist в R2-тестах сопровождается записью в `docs/architecture/README.md` |
| RULE-LESSONS | `lessons.md` — append-only; промоция правила в AGENTS.md — только решением владельца, отдельным коммитом | AGENTS.md «Эволюция правил и манера работы» | review-step | diff `lessons.md` не содержит правок существующих записей; правка AGENTS.md — отдельный коммит |
| RULE-CHK | материальные изменения проходят независимое read-only ревью чеком по шаблону; ADR и коммит ссылаются на путь чека | план process-gates реш. 8 (§5), AGENTS.md «Review» | review-step | чек `docs/reviews/YYYY-MM-DD-<topic>-review.md` существует: `REVIEW_ID`/`VERDICT`/`FINDINGS` со статусами; вектора атаки A1–A8 в промпте ревью |
