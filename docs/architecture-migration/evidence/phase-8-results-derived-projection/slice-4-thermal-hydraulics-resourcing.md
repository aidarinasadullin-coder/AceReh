# Slice 4 — Thermal/Hydraulics re-sourcing (staged scope)

Класс: production/test. Дата: 2026-09-03.

## Scope decision (acting-agent fallback, помечено для owner review)

Вопрос о границе гидравлической части выносился владельцу
(`OWNER_DECISION_REQUIRED`): обнаружено, что Results делит с `CircuitsViewModel`
мутабельные объекты `CircuitRow` (Results пишет `DisplayMode` в объекты модуля,
:1436), `HydraulicSummaryBuilder` принимает модель модуля `CollectorData`
(используется только Results — рефакторинг контейнирован), а
`UpdateCollectorSummary` читает выбор из VM. Ответ владельца не получен;
по best judgment и дисциплине плана выбран **staged** вариант: канонизируются
все *значения*, совместное владение *объектами* фиксируется как именованный
долг Phase 9 (roadmap относит shared seams к legacy cleanup; INV-016:
«legacy cleanup SHALL remove bypassing ViewModel mutation paths»). Решение
подлежит подтверждению при owner result acceptance.

## Re-sourced (production)

1. `LoadThermalData`: `var result = _thermalViewModel.Result` →
   `_projectSession.ThermalState.Snapshot.Result` (`ThermalResultSnapshot`;
   поля PowerUp/PowerDown/SupplyTemperature/ReturnTemperature/MeanTemperature/
   PowerTotal/IsValid — 1:1). Входы уже были канонические (:1068-1076).
2. `LoadHydraulicsData`: GlycolType/GlycolConcentration из
   `HydraulicsState.Snapshot.GlobalInputs` (`HydraulicGlobalInputsSnapshot.Default`
   = Ethylene/50.0 — семантика fallback адаптера сохранена).
3. KPI-цепочка: `CalculateTotalPower` (Σ `Summary.TotalPower`),
   `CalculateSystemVolume` (Σ `CircuitLength + SupplyLength` — формула
   `CircuitRow.TotalLength`, CircuitRow.cs:218),
   `CalculatePumpParameters`/`UpdatePumpHead` (`Summary.TotalFlowRate`,
   `PressureLoss_Operating_Pa`/`PressureLoss_Cold_Pa`) — все из
   `HydraulicsState.Snapshot.Collectors`.
4. `UpdateCollectorsList` — список/метаданные из канонических
   `HydraulicCollectorSnapshot` (`TotalFlowRate_m3h` ≡ `TotalFlowRate/1000` —
   CollectorSummary.cs:61; пустой канон ≡ старая null-ветка).
5. `CheckDataReadiness` (thermal-часть): результат из канона — тот же seam,
   что и п.1; требовался замороженным контрактом (см. ниже). Остальные части
   readiness — slice 5.

## Frozen contract re-pin (разрешено планом, записано)

`RefreshAll_WhenSourceResultIsCleared_ZerosOutputAndMarksNotReady` — прежний
механизм (`thermalViewModel.Result = null`) пинал адаптерную копию; адаптер
канон не пишет (проверено: `[ObservableProperty] _result`). Эквивалентная
каноническая ассертация: `session.ThermalState.InvalidateFromClimate(...)`
(DEC-T04: существующий результат очищается, пересчёта нет). Пользовательский
контракт «результат очищен → KPI в ноль без пересчёта» сохранён.

## Test helper seeding (записано)

`LoadReadyModulesAsync` сеет `HydraulicsProjectData` пустым — канон пуст, а
коллекторы сеялись только в VM. Два теста дополнены каноническим сеянием
(`session.HydraulicsState.ReplaceCollectors(...)` с зеркальными значениями):
`RefreshAll_ProjectsCollectorCircuitSpecificationsEquipmentCardsAndKpi`
(коллектор 7, Σ длин 180, TotalPower 12000) и
`ResultsPdfDataBuilder_AfterInputMutation_...` (коллектор 9, TotalPower 9000).

## Residuals — именованный долг Phase 9 (не скрыт)

- `UpdateCircuitsFilter`: `Results.Circuits` держит **общие** объекты
  `CircuitRow` из VM; Results пишет `circuit.DisplayMode` в объекты модуля
  (:1436). Устранение — реконструкция собственных копий из снапшотов.
- `UpdateCollectorSummary`: читает `_circuitsViewModel.SelectedCollectorIndex`
  и `SelectedCollector.Summary` (выбор из VM).
- `UpdateCollectorSpecifications`/`UpdateCollectorEquipmentItems`/
  `RebuildHydraulicSummaryCards`: `HydraulicSummaryBuilder.Build*(IEnumerable<CollectorData>)`
  — вход на модели модуля (builder используется только Results).
- `CheckDataReadiness`: город/конструкция/труба — VM; гидравлический проб
  (`CircuitLength > 0`) — VM. (Город/конструкция/труба/гидравлика — slice 5.)

## Commands

1. `dotnet build ... --nologo` — exit 0 (6.38 s).
2. Прогон 1 (до фиксов readiness/сеяния): **60 passed / 3 failed** —
   `RefreshAll_WhenSourceResultIsCleared` (readiness на адаптере),
   `RefreshAll_ProjectsCollectorCircuit...` и `ResultsPdfDataBuilder_AfterInputMutation...`
   (канон пуст при VM-сеянии). Причины зафиксированы, исправлены в этом slice.
3. Прогон 2: `dotnet test ... --filter "FullyQualifiedName~ThermalStateCoordinatorTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests|FullyQualifiedName~ResultsViewModelCollectorEquipmentItemsTests|FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests" --logger "trx;LogFileName=slice-4-thermal-hydraulics-resourcing.trx"` — **пройдено 63 / не пройдено 0 / всего 63** (393 ms). TRX: `logs/slice-4-thermal-hydraulics-resourcing.trx`.

## Failure QA

- Exactly-once расчёт сохранён: `ThermalStateCoordinatorTests` и
  `HydraulicsMultiplicityCharacterizationTests` зелёные; `RefreshAll` не
  вызывает координаторы.
- Письмо в канон: сеттеры и реконструкции Results не мутируют
  `ProjectSession`/снапшоты. Известный residual (DisplayMode в общие
  CircuitRow) — записан выше как долг Phase 9, не замаскирован.

## Dirty baseline delta (этот slice)

`src/ViewModels/Results/ResultsViewModel.cs`;
`tests/SnowMeltingCalculator.Tests/ViewModels/ResultsStabilizationPhase1BehaviorContractsTests.cs`.

## Статус

SLICE 4: PASS (staged; residual-список — вход в Phase 9)
