# Slice 2 — LIM-P8-2 resolution (owner decision B: re-pin, import-less restore accepted)

Класс: production/test (изменения только в тестах). Дата: 2026-09-03.

## Решение владельца

Записано в `TASK_CONTEXT.md` (2026-09-03): `/architecture-start
phase-9-legacy-seams-cleanup` + явно выбран вариант `B` — перепинить 5
именованных characterization-тестов и принять restore без импорта как новое
характеризованное поведение. Пользовательское следствие (owner-approved):
проекты с кастомными материалами/шаблонами больше не импортируют их в
глобальные каталоги при загрузке; записи остаются project-local. Это
согласуется с принятым контрактом Phase 7 «read-only global catalog behavior
on project open» (см. `maps/target-invariants.md`, строка об accepted Phase 7
result) и комментарием в `ProjectLoadOrchestrator.cs:151-153`.

## Что изменилось (write-set — только тесты)

1. `tests/.../Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`
   - Хелпер `CreateResultsViewModel`: добавлен optional-параметр
     `orchestratorCalculationState` (мок `ICalculationStateService` только для
     оркестратора; VM/Results/receiver продолжают получать реальный сервис).
   - Оба restore-failure теста: инжекция отказа перенесена с удалённой
     границы `ImportProjectMaterialsAsync`/`ImportProjectTemplatesAsync` на
     живую границу `ICalculationStateService.SetPipeSpacing(int, string)`
     (шаг совместимости, `ProjectLoadOrchestrator.cs:188`). Все прежние
     ассерты сохранены дословно; уточнены комментарии.
   - Late-тест: чтение климата переведено с удалённого поля
     `ResultsViewModel._climateViewModel` (срезано в Phase 8) на канонический
     эквивалент `projectState.Session.ClimateState.Snapshot.AirTemperature`
     (`ApplyProjectSnapshot` пишет `data.AirTemperature` напрямую).
   - `PipeSpacing`-ассерт: `CalculationStateService.PipeSpacing` — read-through
     на `ProjectSession.ThermalState.Snapshot.Inputs.PipeSpacing`
     (`CalculationStateService.cs:141`), поэтому ожидание привязано к
     каноническому restore ДО точки отказа: 250 для проекта с ThermalData
     (Thermal-фикстура), 200 (`ThermalInputsSnapshot.Default`) для
     минимального проекта без ThermalData (Lifecycle-тест).
2. `tests/.../Services/Project/ThermalMultiplicityCharacterizationTests.cs`
   - Хелпер `CreateFixture`: optional-параметр `orchestratorCalculationState`.
   - Оба restore-failure теста: та же инжекция на живой границе; ассерты
     адаптерных дефолтов (SelectedMode/SupplyTemperature/Result) сохранены —
     зеркалирование адаптера идёт ПОСЛЕ границы; PipeSpacing-ожидание 250
     с комментарием о read-through и не-транзакционном restore.
3. `tests/.../Construction/ConstructionServiceTests.cs`
   - `ProjectData_Load_ImportsCustomMaterialsBeforeLayers` → переименован в
     `ProjectData_Load_KeepsCatalogsReadOnly_CustomMaterialsStayProjectLocal`;
     ассерт инвертирован: каталог НЕ содержит «Imported Material» после
     загрузки; слой по-прежнему загружается через каталоговый fallback
     (`ProjectLoadOrchestrator.BuildLayerSnapshots`: material miss →
     `Material.GetDefaultMaterial()`, CalculatedLambda сохраняется из файла).

Характеризуемый контракт (не ослаблен): отказ restore нетранзакционен —
частичное состояние сохраняется без отката, lease/guard сбрасывается,
non-user origins не ставят dirty, отказ surfaces наружу.

## Команды / прогоны (TRX под `logs/`)

1. `dotnet build ... -c Debug --nologo` — exit 0.
2. Прогон 1 (после первой правки): 2 passed / 3 failed — диагностировано:
   (a) GetField по срезанному `_climateViewModel`; (b/c) PipeSpacing-ассерты
   против read-through канонического значения. Причины и фиксы записаны выше.
3. Прогон 2 (финальный): `dotnet test ... --filter
   "FullyQualifiedName~LoadProjectDataAsync_EarlyRestoreFailure|
   FullyQualifiedName~LoadProjectDataAsync_LateRestoreFailure|
   FullyQualifiedName~ProjectData_Load_KeepsCatalogsReadOnly_CustomMaterialsStayProjectLocal"
   --logger "trx;LogFileName=slice-2-lim-p8-2-resolution.trx"` —
   **5 passed / 0 failed**.
4. Полная регрессия: `dotnet test ... --logger "trx;LogFileName=slice-2-full-suite.trx"`
   — **2028 passed / 0 failed / 1 skipped** (RR-004 внешний fixture), всего 2029.

## Dirty baseline delta (этот slice)

Только три тестовых файла выше. Production-код не менялся.

## Статус

SLICE 2: PASS — решение B реализовано, полный suite зелёный.
