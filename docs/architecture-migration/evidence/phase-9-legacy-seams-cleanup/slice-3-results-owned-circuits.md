# Slice 3 — Results-owned circuit projection (ST-026)

Класс: production/test. Дата: 2026-09-03.

## Production-изменения

1. **Новый файл** `src/Services/Results/HydraulicCircuitRowProjection.cs` —
   чистая фабрика Results-owned `CircuitRow` из канонического
   `HydraulicCircuitSnapshot`. Маппинг — инверсия замороженной связки
   `CircuitsViewModel.CaptureCanonicalCollectors` /
   `ApplyLifecycleSnapshotToAdapter` (`:759-774`, `ToDomainResult :835-855`);
   порядок инициализатора сохранён дословно (self-healing `CircuitArea`
   даёт идентичное конечное состояние).
2. `src/ViewModels/Results/ResultsViewModel.cs` — `UpdateCircuitsFilter`
   переведён с `_circuitsViewModel.Collectors?[SelectedCollectorIndex]`
   (общие мутабельные объекты модуля, запись `DisplayMode` в них) на
   `IProjectSession.HydraulicsState.Snapshot.Collectors[SelectedCollectorIndex]`
   + `HydraulicCircuitRowProjection.CreateRow`; `DisplayMode` пишется только
   в Results-owned копии. Поправлен doc-комментарий `:422`.

## Тесты

**Новый файл** `tests/.../ViewModels/ResultsOwnedCircuitProjectionTests.cs`:
1. `UpdateCircuitsFilter_ReconstructsResultsOwnedRows_FromCanonicalSnapshot` —
   реконструкция из снапшота с результатами (Power/FlowRate/DpRohr/DpVerteiler/
   DpVent/DpGesamt/Throttling/ValveTurns/DesignResult) 1:1.
2. `UpdateCircuitsFilter_RowsAreNotSharedWithModuleViewModel` — негативный
   проб владения: ни одна строка Results не ReferenceEquals модульной;
   состояние модульных строк (номер/длина/DisplayMode) не изменилось.
3. `ToggleMode_WritesDisplayModeOnlyOnResultsOwnedRows` — переключение режима
   меняет DisplayMode только на Results-owned копиях, модульные строки
   остаются в своём режиме.

## Команды / прогоны (TRX под `logs/`)

1. `dotnet build ... -c Debug --nologo` — exit 0.
2. `dotnet test ... --filter "FullyQualifiedName~ResultsOwnedCircuitProjectionTests|
   FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|
   FullyQualifiedName~ResultsViewModelCollectorEquipmentItemsTests|
   FullyQualifiedName~ProjectSessionHydraulicsStateTests"
   --logger "trx;LogFileName=slice-3-results-owned-circuits.trx"` —
   **26 passed / 0 failed**, включая замороженные контракты стабилизации
   (result-zeroing без пересчёта, кратности, fresh-vs-stale) и PDF-fixture
   `ResultsPdfDataBuilder_AfterInputMutation_*` — наблюдаемый вывод проекции
   байт-идентичен.

## Известный residual (не скрыт)

`HydraulicCircuitRowProjection.ToDomainResult` дублирует приватный
`CircuitsViewModel.ToDomainResult` (по плану слайс 3 не трогает модульный VM).
Дедупликация — кандидат в последующую очистку; дрейф исключён полным suite
(обе копии пинятся characterization-тестами).

## Dirty baseline delta (этот slice)

`src/Services/Results/HydraulicCircuitRowProjection.cs` (новый);
`src/ViewModels/Results/ResultsViewModel.cs`;
`tests/SnowMeltingCalculator.Tests/ViewModels/ResultsOwnedCircuitProjectionTests.cs` (новый).

## Статус

SLICE 3: PASS
