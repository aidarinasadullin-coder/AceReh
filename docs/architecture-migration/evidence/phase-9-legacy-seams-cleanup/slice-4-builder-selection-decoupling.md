# Slice 4 — Summary builder canonicalization + selection re-source + module-VM decoupling (ST-026/ST-027)

Класс: production/test. Дата: 2026-09-03.

## Production-изменения

1. `src/Services/Results/HydraulicSummaryBuilder.cs` — переписан на вход
   `IReadOnlyList<HydraulicCollectorSnapshot>` (канонические снапшоты):
   - `BuildSummaryCards` строит `CollectorHydraulicSummaryCard` напрямую из
     снапшота (формат отображения `FormatCollectorTypeDisplay` сохранён 1:1 с
     `CollectorData.CollectorTypeDisplayWithCount`);
   - `BuildSpecifications`: `TotalFlowRate_m3h = TotalFlowRate/1000`,
     `PressureLoss_mbar = Pa/100` — те же формулы, что в `CollectorSummary`;
   - `BuildEquipmentItems`: группировка (ValveType, CircuitCount) без изменений.
2. `src/Models/Hydraulics/CollectorHydraulicSummaryCard.cs` — добавлен
   parameterless-конструктор (model не зависит от Services; прежний
   ctor(CollectorData) сохранён для совместимости).
3. `src/ViewModels/Results/ResultsViewModel.cs`:
   - `UpdateCollectorSummary` — сводка строится из
     `HydraulicsState.Snapshot.Collectors[SelectedCollectorIndex]` по **выбору
     Results** (ST-027; ранее читался выбор модуля `_circuitsViewModel`);
     приватный маппер `CreateCollectorSummary` — инверсия адаптерного зеркала;
   - три вызова `HydraulicSummaryBuilder.Build*` переведены на канонические
     снапшоты;
   - удалены поле/ctor-параметр `CircuitsViewModel` (`:42`, параметр, `throw`);
   - doc-комментарии (`:422`, Reset-комментарий) обновлены.
   Статическая проба: в `ResultsViewModel.cs` не осталось ни одной кодовой
   ссылки на concrete module-ViewModel (только 3 совпадения в комментариях).

## Тестовые изменения (write-set)

- `ResultsViewModelTestHelpers.cs` — из ctor-вызова убран аргумент `circuitsVm`.
- Аналогично обновлены 9 test ctor-сайтов: `ConstructionServiceTests` (×2),
  `DialogServiceThreadAffinityTests`, `ResetOrchestrationTests`,
  `MainViewModelTests`, `ResultsViewModelOpenProjectTests` (×2),
  `ProjectLifecycleFlowCharacterizationTests`,
  `ThermalMultiplicityCharacterizationTests`,
  `ClimateThermalInvalidationRegressionTests`.
- `ResultsStabilizationPhase1ContractsTests.CreateUninitializedMainWindow` —
  вместо `GetField(results, "_circuitsViewModel")` создаётся собственный
  адаптер модуля (`CreateCircuitsViewModelWithCollectors()`) для графа
  MainViewModel (замороженный контракт пинался через срезанное поле).
- `ResultsStabilizationPhase1BehaviorContractsTests` — фикстура хранит
  `_circuitsVm`; PDF-тест берёт модульный VM из фикстуры (PDF builder —
  seam слайса 5), каноническое сеяние сохранено.

## Команды / прогоны (TRX под `logs/`)

1. `dotnet build ... -c Debug --nologo` — exit 0 (после фикса ctor карточки;
   первая сборка дала CS7036, зафиксирован и исправлен в этом slice).
2. Прогон 1 focused: 58 passed / 4 failed — все четыре = NRE в `GetField`
   по срезанному `_circuitsViewModel` (2 файла стабилизационных suite);
   исправлено, зафиксировано.
3. Прогон 2 focused: `slice-4-builder-selection-decoupling.trx` —
   **62 passed / 0 failed** (включая DiRegistrationTests — DI-граф резолвится
   без removed-параметра; PDF-fixture байт-идентичен).
4. Смежные suite: `slice-4-adjacent-suites.trx` —
   **178 passed / 0 failed / 1 skip** (RR-004).

## Frozen contracts

Сохранены дословно: result-zeroing без пересчёта, кратности проекции,
fresh-vs-stale sentinel, PDF `RequiresCurrentScalarAndDerivedGeneration`.
Перепин фиксации seam (чтение `_circuitsViewModel` из Results → собственный
адаптер фикстуры) записан здесь.

## Остаток (не скрыт, слайс 5)

`ResultsPdfDataBuilder` по-прежнему держит `ConstructionViewModel` +
`CircuitsViewModel` (ctor-параметры, чтение слоёв/коллекторов для PDF) —
concrete-VM зависимость application-сервиса, попадает под зонд INV-008
слайса 5; решение (abstraction/перемещение) фиксируется в slice-5 receipt.

## Статус

SLICE 4: PASS
