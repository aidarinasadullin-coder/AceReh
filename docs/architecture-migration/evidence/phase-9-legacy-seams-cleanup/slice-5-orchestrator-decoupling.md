# Slice 5 — ProjectLoadOrchestrator decoupling (INV-008) + static architecture test

Класс: production/test. Дата: 2026-09-03.

## Дизайн (планируемый fallback: application-owned adapter-интерфейсы)

Preferred-вариант плана (полный перенос view-side эффектов в adapter/shell
слой) потребовал бы редизайна thermal fallback-расчёта
(`CalculateCommand.ExecuteAsync` внутри restore) — часть замороженного
Phase 7 exactly-once контракта, изменение которой plan запрещает. Выбран
разрешённый планом fallback: узкие adapter-интерфейсы, принадлежащие
application-слою (НЕ ViewModel), реализуемые существующими singleton-VM;
DI связывает интерфейс с тем же экземпляром. Поведение restore не меняется:
оркестратор вызывает тех же получателей через интерфейс.

## Production-изменения

1. **Новый** `src/Services/Project/ProjectRestoreAdapters.cs` —
   `IProjectLoadClimateAdapter` (SearchQuery, FindCityByName),
   `IProjectLoadConstructionAdapter` (AvailableMaterials,
   ApplyLifecycleSnapshotToAdapter), `IProjectLoadThermalAdapter`
   (AvailablePipes, SelectedMode/SupplyTemperature/GroundTemperature/
   SelectedPipe/PipeSpacing, Reset, LoadResult, CalculateFromRestoreAsync),
   `IProjectLoadHydraulicsAdapter` (Reset, ApplyLifecycleSnapshotToAdapter).
2. `src/Services/Project/ProjectLoadOrchestrator.cs` — 4 concrete-VM поля и
   ctor-параметра заменены на интерфейсы; `CalculateCommand.ExecuteAsync`
   → `CalculateFromRestoreAsync()`; удалён мёртвый метод
   `LoadLayersFromProjectDataLegacy` (~90 строк, 0 вызовов — заменён ранее
   каноническим `BuildConstructionSnapshotFromProjectData`+`ApplySnapshot`;
   он же был единственным потребителем Layers/UpdateCalculations на
   construction-адаптере); ViewModel-usings сняты.
3. **Новый** `src/Services/Results/ReportDataSources.cs` —
   `IReportConstructionLayerSource` (LayersAbovePipe/LayersBelowPipe),
   `IReportCollectorDataSource` (Collectors) для PDF-источника.
4. `src/Services/Results/ResultsPdfDataBuilder.cs` — 2 concrete-VM ctor
   параметра → интерфейсы (те же singleton-объекты: содержимое отчёта
   байт-идентично, читаются те же коллекции; `Build(ResultsViewModel)` —
   прежняя замороженная public-граница).
5. VM-объявления: `ClimateViewModel : …, IProjectLoadClimateAdapter`,
   `ConstructionViewModel : …, IProjectLoadConstructionAdapter,
   IReportConstructionLayerSource`, `ThermalViewModel : …,
   IProjectLoadThermalAdapter` (+public `CalculateFromRestoreAsync()` —
   тот же единственный `CalculateCommand.ExecuteAsync(null)`),
   `CircuitsViewModel : …, IProjectLoadHydraulicsAdapter,
   IReportCollectorDataSource`.
6. `src/Configuration/ServiceCollectionExtensions.cs` — 6 factory-связок
   интерфейс → `GetRequiredService<…ViewModel>` (тот же singleton).

## Статический тест (ApplicationServiceViewModelDecouplingTests)

`tests/SnowMeltingCalculator.Tests/Architecture/…`: сканирует все concrete
классы пространств `SnowMeltingCalculator.Services.*` (production assembly)
и отвергает ctor-параметры concrete-типов из `SnowMeltingCalculator.ViewModels.*`.

- **RED**: `logs/slice-5-static-test-RED.trx` — 1 failed. Перед прогоном в
  `ResultsPdfDataBuilder` временно возвращён concrete `CircuitsViewModel`
  (одна строка, сразу после прогона возвращена — обе операции в git diff
  отсутствуют); детектор поймал реальное нарушение.
- **GREEN**: `logs/slice-5-static-test-GREEN.trx` — 1 passed после отвязки.

## Frozen contracts / re-pin

- Phase 7 restore-контракты перепроверены без изменений: порядок валидации,
  validate-first, exactly-once publication, rejected-restore preservation,
  second-load clean replace (`ProjectLifecycleFlowCharacterizationTests`,
  `ThermalMultiplicityCharacterizationTests`,
  `HydraulicsMultiplicityCharacterizationTests` — зелёные).
- Re-pin (записан): `ProjectLoadOrchestrator_PreservesLoadOnlyThermalFallbackBoundary`
  пиннил строку `await _thermalViewModel.CalculateCommand.ExecuteAsync(null);` —
  заменён на эквивалент `await _thermalViewModel.CalculateFromRestoreAsync();`
  + новая ассертация, что `ThermalViewModel` содержит единственный
  `CalculateCommand.ExecuteAsync(null)` (exactly-once сохранён).

## Команды / прогоны (TRX под `logs/`)

1. `dotnet build` — exit 0 (первая сборка: 9×CS1061 на мёртвом
   legacy-методе → удалён как dead code, зафиксировано).
2. RED: 1 failed (см. выше). GREEN: 1 passed.
3. `slice-5-orchestrator-decoupling.trx` (финальный, 13 suite-фильтров):
   **220 passed / 0 failed / 1 skip** (RR-004) — включая restore-контракты,
   кратности Thermal/Hydraulics, DI-граф (`DiRegistrationTests` — резолв
   новых интерфейсных связок), PDF/stabilization-контракты.

## Dirty baseline delta (этот slice)

`src/Services/Project/ProjectRestoreAdapters.cs` (новый);
`src/Services/Project/ProjectLoadOrchestrator.cs`;
`src/Services/Results/ReportDataSources.cs` (новый);
`src/Services/Results/ResultsPdfDataBuilder.cs`;
`src/ViewModels/{Climate/ClimateViewModel,Construction/ConstructionViewModel,Thermal/ThermalViewModel,Hydraulics/CircuitsViewModel}.cs`;
`src/Configuration/ServiceCollectionExtensions.cs`;
`tests/.../Architecture/ApplicationServiceViewModelDecouplingTests.cs` (новый);
`tests/.../ViewModels/ResultsStabilizationPhase1BehaviorContractsTests.cs` (re-pin).

## Статус

SLICE 5: PASS — INV-008 удовлетворён с исполняемым статическим доказательством.
