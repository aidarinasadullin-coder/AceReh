# Slice 5 — readiness + display mode re-sourcing

Класс: production/test. Дата: 2026-09-03.

## Scope executed

1. `CheckDataReadiness` — все проверки из канонических источников:
   - климат: `ClimateState.Snapshot.IsCitySelected` (флаг ставится в
     `ApplyCitySelection`/`ApplyProjectSnapshot`; эквивалентен
     `SelectedCity == null` адаптера — MirrorSnapshot даёт null ⇔ пустое имя);
   - конструкция: `ConstructionState.CurrentProjection.IsValid`;
   - thermal: результат из `ThermalState.Snapshot.Result` (переведён в slice 4),
     труба — `ThermalState.Snapshot.Inputs.Pipe` (вместо `_thermalViewModel.SelectedPipe`);
   - гидравлика: проб `CircuitLength > 0` по каноническим `Collectors`.
2. Display mode (ST-003): `IsOperatingMode` — read-through к app-owned
   `IProjectDisplayModeState` (когда зарегистрирован), VM-поле `_isOperatingMode`
   — fallback для legacy-сборки без seam; ручные `OnPropertyChanged` для
   `CurrentModeText`/`IsDesignMode`/`MaxPressureLoss`; write-through в ctor
   сохранён; устаревший partial-хук удалён. Wire-поведение `.smc`
   (`IsOperatingMode` в ProjectData) не изменено.
3. Fix по ходу slice (пойман characterization-тестом): ранний return
   `UpdateCollectorsList` при пустом каноне удалён — старый код различал null
   (ранний return) и пустой список (loop + refresh summary/filter); канон
   null не бывает, empty должен вести себя как «пустой список», иначе
   `CollectorSummary` оставался stale (`ResultsViewModel_EmptyHydraulics_ZeroesKpisAndCards`:
   TotalFlowRate 600.5 вместо 0 после загрузки пустого проекта).

## Frozen-equivalent test update (записано)

`ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`:
тест мутирует `thermalVm.Result` напрямую — это адаптерное зеркало
(`[ObservableProperty]`, канон не пишет — проверено), в production результат
публикуется координатором. Канонический эквивалент — публикация через
`session.ThermalState.CompleteCalculation(inputs, ThermalResultSnapshot.FromResult(...), "")`
(без фазовых требований, статус Actual — как у координатора). Без этого save
(читает канон) не нес результата, reopened-VM получала
«Тепловой расчёт - нет результата» → IsDataReady=false → пустой
`CollectorEquipmentItems` → `Single()` бросал.

## Commands

1. Прогон 1: build OK (4.63 s); тест **71 passed / 2 failed** —
   `ResultsViewModel_EmptyHydraulics_ZeroesKpisAndCards` (stale TotalFlowRate —
   ранний return) и `ProjectRoundTrip_...` (канон без результата — адаптерная
   мутация). Обе причины устранены в этом slice.
2. Прогон 2: **73 passed / 0 failed / 1 skipped** (12 s). TRX:
   `logs/slice-5-readiness-display-mode.trx`.
3. Skip: `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` —
   известный внешний fixture `D:\IA\ace\Тест\тест 40.smc` (RR-004), не pass.

## Failure QA

- Readiness из канона: смена флага без пересчёта покрытия не даёт ложной
  готовности (тесты not-ready сценариев зелёные).
- `.smc` wire: `IsOperatingMode` поведение сохранено (ProjectSaveServiceTests
  зелёные; fixture-хэши не затронуты — формат не менялся).
- Display mode: ToggleMode/load/reset идут через seam; legacy-путь (без seam)
  эквивалентен прежнему.

## Dirty baseline delta (этот slice)

`src/ViewModels/Results/ResultsViewModel.cs`;
`tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`.

## Статус

SLICE 5: PASS
