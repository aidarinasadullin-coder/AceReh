REVIEW_ID: R-2026-09-21-03
SUBJECT: план docs/plans/2026-09-21-hydraulic-advice-plan.md — пункт 2.1 роадмапа, гидравлические советы с лечением (зеркало ThermalAdviceService + «Применить» типоразмера через IThermalStateCoordinator); show-me docs/plans/show-me-hydraulic-advice.html
REVIEWER: независимый read-only subagent, чистый контекст (Explore)
DATE: 2026-09-21
RECEIPT: настоящий файл (полный отчёт ревьюера сохранён ниже)
VERDICT: APPROVE-WITH-EDITS
REASON: план реализуем, якоря и сущности подтверждаются кодом, R1–R6/wire .smc/writers не задеты (проверено по векторам A1–A8, включая три целевых риска). P1 — H8 утверждал «советы гаснут после „Применить"», что неверно (thermal-мутация не инвалидирует гидравлические результаты); P2 — арифметика KPI и подпись кнопки в show-me расходились с планом/кодом. Все находки приняты; правки внесены до реализации.

## Находки и статусы

| # | Приоритет | Находка | Статус |
|---|---|---|---|
| 1 | P1 | H8: «советы гаснут по инвалидации результатов (H7)» после «Применить» неверно — ADR-012-очистка срабатывает только на HydraulicsMutationOrigin.User (CircuitsViewModel.cs:1079, 1120-1169); thermal-ApplyInputEdit (ThermalStateCoordinator.cs:109-122) строки гидравлики не трогает, ThermalInputs обновляется лишь при thermal-расчёте → советы остались бы со старыми числами, второй клик давал бы тихий NoChange | исправлено в этом же диффе: H8 переписан (советы сохраняются до пересчёта; CanExecute требует SuggestedPipe ≠ канон по InnerDiameter); §3 дополнен условием CanExecute + NotifyCanExecuteChanged; §7/§8 — документирование и smoke-строка |
| 2 | P2 | show-me KPI «Потери (раб.) 254.3 кПа / (хол.) 348.1 кПа» — 10-кратное завышение против лимита 320 мбар = 32 кПа (HydraulicsConstants.cs:20-25) и таблицы макета (макс. раб. 327 мбар) | исправлено в этом же диффе: KPI пересчитаны — 32,7 кПа / 34,8 кПа, каноническая запятая |
| 3 | P2 | Подпись кнопки show-me «Применить: RAUTHERM S 25×2,3» не совпадает с тем, что отрисует XAML-план: DisplayName = «RAUTHERM S 25x2,3 (Ø25×2,3)» (PipeType.cs:44, 73) | исправлено в этом же диффе: план §5 фиксирует короткий формат SuggestedPipe.Name («RAUTHERM S 25x2,3»); show-me приведён к нему |
| 4 | P3 | Тексты правил §2.1 с точкой («0.5 м/с») при требовании AppCulture-запятой | исправлено в этом же диффе: примеры текстов в §2.1 переведены в каноническую запятую (0,5 / 2,0) |
| 5 | P3 | Нет предостережения об обходном канале: прямой ProjectSession.ThermalState.ApplyInputEdit из CircuitsViewModel уронит ArchitectureRulesTests R2/WI-3 | исправлено в этом же диффе: строка «Предостережение» в §3 |
| 6 | P3 | §5 не называет правку RowDefinitions сетки карточки (сейчас три, CircuitsView.xaml:510-514) | исправлено в этом же диффе: §5 — «добавить четвёртый RowDefinition» |
| 7 | P3 | §9 не пинит REVIEW_ID; серия 2026-09-21 занята -01/-02 (урок №29) | исправлено в этом же диффе: §9 — R-2026-09-21-03 |
| 8 | P3 | Офф-бай якорей: SetPipeSpacing-allowlist :179-192 (не :180-191), ApplyStateSnapshotToAdapter :679-704 (не :676-700) | исправлено в этом же диффе: якоря уточнены в H2 и §8 |

## Подтверждено ревьюером (выборка с путями:строками)

- **A1 якоря:** гвард и ApplyInputEdit — ThermalViewModel.cs:167-174; OnCoordinatorCompletion :650-671; эхо-прецедент ApplyStateSnapshotToAdapter :679-704; ThermalStateCoordinator.cs:109-122 (Origin=User, один dirty-intent, синхронный Completion :118); CollectorSummary.cs:152-157; CircuitsView.xaml:574/632/861/894; ThermalView.xaml:155; ServiceCollectionExtensions.cs:316 (и AddSingleton<CircuitsViewModel> :155, IThermalStateCoordinator singleton :80 → DI даст тот же экземпляр); CircuitsViewModel.cs:1175-1215, 1646-1663, 587/635/883/961/1163, 703; CalculationStateService.cs:179-192; CalculationReportDataBuilder.cs:251; CircuitsCalculator.cs:95-97.
- **A2 сущности:** ThermalInputEdit.ForPipe / ThermalInputField.Pipe (ThermalMutationResult.cs:28-35, 72-73); ThermalPipeSnapshot.FromPipeType (ThermalStateSnapshots.cs:57); ThermalMutationOrigin.User (ThermalMutationOrigin.cs:11); ResolveStandardPipe (ThermalPersistenceMapper.cs:150-160); PipeType.StandardPipes Ø 13/16/20.4 (PipeType.cs:49-80); HydraulicsConstants 32000/320/0.5/2.0/120 (HydraulicsConstants.cs:20-49); CircuitRow.OperatingResult/DesignResult/IsActive/Velocity (CircuitRow.cs:386-418, 459); CollectorData.Circuits; CalculationContext.ThermalInputs?.Pipe (CalculationContext.cs:112); токены Icon.Warning (Icons.Fluent.xaml:71), Status.Warning[.Dark].Brush (Tokens.Colors.xaml:92-93); ValidationConstants.MinVelocity=0.1 (:231); IsLoadProjectInProgress (ICalculationStateService.cs:106).
- **A3 ripple:** все 16 точек `new CircuitsViewModel(` — в tests, 8 позиционных аргументов → 2 опциональных хвостовых параметра не ломают; AutomationId-гейт — allowlist, дубликатов HydraulicsAdviceList/HydraulicsAdviceApply нет (ThermalAutomationIdSelectorContractTests:122-136); UiSmoke не зависит от вставки строки; ViewTokenHygieneTests: CircuitsView.xaml в ratchet (0,0) — план использует токены; ManualVersionGateTests закрывается пересборкой §7.
- **A4 архитектура:** R4-скан пройдёт (новый сервис не ссылается на VM); R2/WI-3 — ThermalState остаётся единственным писателем, координатор уже в allowlist; wire .smc не меняется; условие роадмапа «без записей VM→VM и без новых писателей» соблюдено буквально (роадмап:34, Идеи.md №7).
- **Целевой R-риск-1 (петля эха): ЗАКРЫТ** — гвард `if (_isResetting) return;` первой строкой OnSelectedPipeChanged (:169) до ApplyInputEdit (:173); Completion синхронный; прецедент :679-704.
- **Целевой R-риск-3 (null DesignResult): ЗАКРЫТ** — оба результата заполняются атомарно за один прогон (CircuitsViewModel.cs:804-822; CircuitsCalculator.cs:176-190); провальные ветки чистят оба (ClearCalculatedFields).
- **Целевой R-риск-2 (устаревшие советы): ЗАКРЫТ ЧАСТИЧНО → находка №1** — гейт H7 надёжен для гидравлических правок (Velocity чистится: CircuitsViewModel.cs:1112, 1149), но не покрывает thermal-мутации; покрыто правкой H8.

---

## Полный отчёт ревьюера (входящий)

# Вердикт: APPROVE-WITH-EDITS

План реализуем, якоря и сущности почти полностью подтверждаются кодом, архитектурные гейты не задеваются. Но **риск «устаревших советов» после «Применить» закрыт планом лишь наполовину** (H8 содержит фактически неверное утверждение), и show-me расходится с планом по числам и подписи кнопки — до реализации нужны правки ниже.

## Находки

| # | Приоритет | Находка (с доказательством) | Предлагаемая правка |
|---|---|---|---|
| 1 | **P1** | **H8 утверждает «советы гаснут по инвалидации результатов (H7)» — для сценария «Применить» это неверно.** Инвалидация результатов гидравлики (ADR-012) срабатывает только на `HydraulicsMutationOrigin.User/UserReset` (src/ViewModels/Hydraulics/CircuitsViewModel.cs:1079, очистка в `ClearStaleCalculationResults` :1120-1169 и `ClearCalculatedFields` :1102-1118). Смена трубы из гидравлики — это `Origin=User` на **тепловом** срезе: `IThermalStateCoordinator.ApplyInputEdit` (src/Services/Project/ThermalStateCoordinator.cs:109-122) не трогает строки гидравлики; `NotifyThermalPropertiesChanged` (CircuitsViewModel.cs:1646-1663) — display-only. Более того, `CalculationContext.ThermalInputs` обновляется только при thermal-расчёте (ThermalStateCoordinator.cs:148, 240, 289). Итог: после «Применить» блок советов **остаётся** со старыми числами; после теплового пересчёта (до гидравлического) факты смешиваются; CanExecute стейл-гварда не содержит. | Переписать H8/§8: «после „Применить" советы **сохраняются** до пересчёта гидравлики»; деактивировать кнопки «Применить», пока `SuggestedPipe ==` каноническая труба; зафиксировать фактическое поведение в §7 и §8. |
| 2 | **P2** | **Арифметика show-me противоречит плану и лимиту.** KPI-чипы макета: «Потери (раб.) 254.3 кПа», «Потери (хол.) 348.1 кПа». Шкала лимита: 320 мбар = 32 кПа (HydraulicsConstants.cs:20-25). 348.1 мбар = 34.81 кПа, а не 348.1 кПа (10-кратное завышение); 254.3 кПа = 2543 мбар; с таблицей (макс. раб. 327 мбар) KPI не сходится. | При регенерации show-me пересчитать KPI: раб. ≈ 32,7 кПа, хол. ≈ 34,8 кПа, запятая-канон. |
| 3 | **P2** | **Подпись кнопки в show-me не совпадает с тем, что отрисует XAML.** Show-me: «Применить: RAUTHERM S 25×2,3». Фактический `DisplayName` = `$"{Name} (Ø{OuterDiameter}×{WallThickness})"` → кнопка покажет «Применить: RAUTHERM S 25x2,3 (Ø25×2,3)». | Либо в show-me показать реальный DisplayName, либо в плане §5 явно указать короткий формат подписи как отдельное решение. |
| 4 | P3 | Тексты правил §2.1 пишут «ниже 0.5 м/с» с точкой, но §2 требует форматирования через `AppCulture.Culture` (запятая). | В §2.1 писать примеры текстов в канонической запятой. |
| 5 | P3 | Ловушка для имплементера: `ApplyInputEdit` нужно звать **только через `IThermalStateCoordinator`** — прямой вызов `projectSession.ThermalState.ApplyInputEdit(...)` уронит `ArchitectureRulesTests.R2_ThermalState` (WI-3-allowlist, ArchitectureRulesTests.cs:140-149). | Добавить в §3 строку-предостережение. |
| 6 | P3 | Вставка блока советов требует **добавить четвёртый `RowDefinition`** (сейчас 3: CircuitsView.xaml:510-514). | Дописать в §5. |
| 7 | P3 | §9 не пинит REVIEW_ID: серия 2026-09-21 имеет -01 и -02 → чек обязан быть **R-2026-09-21-03** (урок №29). | Указать в §9. |
| 8 | P3 | Офф-бай якорей: SetPipeSpacing-allowlist :179-192; ApplyStateSnapshotToAdapter :679-704. | Уточнить. |

## Отдельно: три риска, заявленные закрытыми

- **R-риск-1 (петля эха) — ЗАКРЫТ.** Гвард `_isResetting` первой строкой OnSelectedPipeChanged (:169) до ApplyInputEdit (:173); Completion синхронный; прецедент ApplyStateSnapshotToAdapter (:679-704).
- **R-риск-2 (устаревшие советы) — ЗАКРЫТ ЧАСТИЧНО (находка №1).** Velocity чистится при инвалидации (CircuitsViewModel.cs:1112, 1149) — гейт H7 надёжен для гидравлических правок; тепловые правки не покрыты.
- **R-риск-3 (null DesignResult) — ЗАКРЫТ.** Оба результата заполняются атомарно за один прогон (CircuitsViewModel.cs:804-822; CircuitsCalculator.cs:176-190); провальные ветки чистят оба.

## Подтверждено

A1/A2/A3/A4/A5/A7/A8 — см. сводку выше: якоря и сущности подтверждаются, ripple не ломается (16 точек конструирования, AutomationId-allowlist, ViewTokenHygiene ratchet, ManualVersionGate), архитектурные гейты не задеваются, решения-дефолты §0.1 оформлены с альтернативами (урок №28), H9 честно отклоняет фикс «17×2,0» из Идеи.

**Итоговая рекомендация:** внести правки №1 (обязательно, до реализации), №2-№3 (в регенерацию show-me), №4-№7 — по ходу. После правки №1 план готов к реализации без перерева.
