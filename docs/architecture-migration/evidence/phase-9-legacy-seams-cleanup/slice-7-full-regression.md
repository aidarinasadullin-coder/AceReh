# Slice 7 — Full-suite regression + invariant/multiplicity proof

Класс: tests/evidence (один тестовый re-pin зафиксирован). Дата: 2026-09-03.

## Прогон полной регрессии

`dotnet test` (full suite), TRX `logs/slice-7-full-regression.trx`:

| Прогон | Результат |
|---|---|
| 1 | 2031 passed / **1 failed** / 1 skipped — `ConstructionStateLegacyStoreGuard_CapturesExactCurrentWriterInventory` (source-пин на удалённый в slice 5 мёртвый legacy-загрузчик) |
| 2 (финальный) | **2032 passed / 0 failed / 1 skipped** (RR-004 внешний fixture) |

## Re-pin (записан)

`ConstructionStateLegacyStoreGuardTests`: guard пиннил наличие в исходнике
`ProjectLoadOrchestrator` прямых записей в коллекции VM
(`_constructionViewModel.Layers{Above,Below}Pipe.Clear/Add`) — часть
инвентаря bypass #6 из мёртвого `LoadLayersFromProjectDataLegacy`, удалённого
в slice 5 (0 вызовов; заменён каноническим `ApplySnapshot` + adapter-mirror).
Четыре ассерта инвертированы в `Does.Not.Contain` с комментарием Phase 9 —
инвентарь текущих писателей стал строже (прямых VM-записей больше нет);
остальные bypass-пины (#2-#5, scalar-writes guard) не тронуты и зелёные.

## Контракты, подтверждённые полной регрессией

- Кратности: `ThermalMultiplicityCharacterizationTests`,
  `HydraulicsMultiplicityCharacterizationTests` — повторные
  new/load/second-load/reset циклы без размножения обработчиков/расчётов;
  `INV-011`-стиль счётчиков стабилен.
- Dirty-семантика: save success → MarkClean однократно; failure → dirty
  сохраняется; restore/reset — non-user origins без dirty
  (`ProjectLifecycleFlowCharacterizationTests`,
  `ResetOrchestrationTests`, `ClimateThermalInvalidationRegressionTests`).
- Fresh-vs-stale sentinel и кратности проекции Results —
  `ResultsStabilizationPhase1*`, `ResultsOwnedCircuitProjectionTests`,
  `ResultsViewModelOpenProjectTests`.
- Save/report fixtures: `ProjectSaveServiceTests`,
  `ProjectFileServiceResultTests` — без изменений.
- `.smc` fixtures: `git diff --name-only -- '*.smc'` — пусто.

## Статус

SLICE 7: PASS — полный suite зелёный, 1 известный skip (RR-004), 0 regression.
