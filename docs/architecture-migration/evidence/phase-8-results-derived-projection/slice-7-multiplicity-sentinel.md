# Slice 7 — projection multiplicity, rejected-restore preservation, fresh-vs-stale sentinel

Класс: production/test (верификация существующего покрытия; нового production-кода нет).
Дата: 2026-09-03.

## Claim → доказательство (существующие characterization-тесты)

| Claim (план slice 7) | Доказательство |
|---|---|
| Ровно один `RefreshAll()` на успешный restore | `LoadAndReopen_RefreshesProjectionAndLeavesProjectClean`; `RefreshAll_ProjectsCollectorCircuitSpecificationsEquipmentCardsAndKpi` (ровно один вызов refresh в командном пути — контракты `ResultsCommands_RefreshBeforeConsumingOrExportingData` фиксируют ровно один `RefreshAll()` на команду) |
| Rejected restore не перестраивает проекцию, guard освобождается | `LoadProjectData_SecondInvalidProjectPreservesPriorUiAndReleasesRestoreGuard` (OpenProjectTests:2481, Phase 7 slice-7 контракт) |
| Idempotency проекции (без дублей строк и пересчётов) | `RefreshAll_WhenSourceStateIsUnchanged_IsValueIdempotentWithoutDuplicateRowsOrCalculation` |
| Fresh-vs-stale sentinel: проекция несёт session-derived значения, а не stale persisted DTO | `ResultsPdfDataBuilder_AfterInputMutation_RequiresCurrentScalarAndDerivedGeneration` (скаляры/агрегаты из текущей генерации: город «PDF current city», ветер 8.5, TotalPower 9 kW от канонического сеяния), `ResultsPdfDataBuilder_UsesConstructionLayersAndImageParametersFromSameCurrentSource` (слои и параметры визуализации из одного текущего источника), `SaveCurrentProject_ProjectsLiveModuleStateInsteadOfResultsCache` |
| Second load заменяет stale-значения проекта A | `ProjectRoundTrip_FieldCompleteRoundTrip_SecondLoadReplacesProjectA`, `LoadProjectData_SecondLoadWithoutSavedResult_ReplacesAllThermalStaleValues` |
| Ровно один расчёт/публикация; RefreshAll не считает | `RefreshAll_WhenInputsChangeButValidResultIsRetained_PreservesOutputWithoutCalculation` (calculator Times.Never), `ThermalStateCoordinatorTests`/`HydraulicsMultiplicityCharacterizationTests` (slice 4) |

## Commands

`dotnet test ... --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResetOrchestrationTests" --logger "trx;LogFileName=slice-7-multiplicity-sentinel.trx"` — **пройдено 59 / не пройдено 0 / пропущено 1 (известный внешний fixture, RR-004)**. TRX: `logs/slice-7-multiplicity-sentinel.trx`. Жизненный цикл: `ProjectLifecycleFlowCharacterizationTests` — в полном прогоне slice 6 (2023 passed), 4 pre-existing import-провала классифицированы в slice-6 receipt.

## Failure QA

Мультиплисити-нарушение (второй rebuild на rejected restore) ловится
`LoadProjectData_SecondInvalidProjectPreservesPriorUi...`; stale-DTO в проекции —
свежvs-stale sentinel'ы выше (положительные ассерты на session-derived значения).

## Статус

SLICE 7: PASS
