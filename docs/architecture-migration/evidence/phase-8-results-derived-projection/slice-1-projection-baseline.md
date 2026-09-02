# Slice 1 — projection baseline lock

Класс: tests/evidence only. Дата: 2026-09-03. План: `docs/architecture-migration/plans/phase-8-results-derived-projection.md` (SHA-256 `EC762434820E87EA92B9A37A4FD694DCABD81181F93C1B6EA035FFF5674F5C67`).

## Commands (Git Bash adaptation per AGENTS.md environment-adaptive rules)

1. `mkdir -p docs/architecture-migration/evidence/phase-8-results-derived-projection/logs` — OK.
2. `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo` — exit 0, предупреждений: 0, ошибок: 0 (13.09 s).
3. `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests|FullyQualifiedName~ResultsViewModelCollectorEquipmentItemsTests" --logger "trx;LogFileName=slice-1-projection-baseline.trx" --results-directory docs/architecture-migration/evidence/phase-8-results-derived-projection/logs` — **пройдено 27 / не пройдено 0 / пропущено 0 / всего 27** (456 ms). TRX: `logs/slice-1-projection-baseline.trx`.

Базлайн зелёный до любых production-изменений. 0-тестовый прогон исключён (27 выполнено).

## Module-ViewModel read inventory (полный, до изменений)

Поле-ссылки: `_climateViewModel`, `_constructionViewModel`, `_thermalViewModel`, `_circuitsViewModel` (`ResultsViewModel.cs:41-44`); конструктор принимает четыре конкретные module-ViewModel + `ProjectLoadOrchestrator` (:483-498).

| # | Метод / сайт | Строки | Что читается |
|---|---|---|---|
| 1 | `LoadClimateData` | 1021-1029 | `SelectedCity?.Name`, `AirTemperature`, `SelectedZone`, `SelectedCity?.Period_0_Days ?? 150`, `WindSpeed`, `SnowfallIntensity` |
| 2 | `LoadConstructionData` | 1034-1056 | `R1Total`, `R2Total`, `LambdaE`, `LayersAbovePipe`, `LayersBelowPipe` |
| 3 | `LoadThermalData` | 1061-1098 | входы уже канонические (`ThermalState.Snapshot`, :1068-1076); результат — `_thermalViewModel.Result` (:1078) |
| 4 | `LoadHydraulicsData` | 1103-1119 | `InputData?.GlycolType` (:1106), `InputData?.GlycolConcentration` (:1109), далее `UpdateCollectorsList`/контуры |
| 5 | `UpdateCollectorsList` | 1332-1347 | `_circuitsViewModel.Collectors` (элементы и количество) |
| 6 | `RebuildHydraulicSummaryCards` | 1463-1473 | `_circuitsViewModel.Collectors` (:1468) |
| 7 | `UpdateCollectorEquipmentItems` | 1447-1459 | `_circuitsViewModel.Collectors` (:1454) |
| 8 | `UpdateCircuitsFilter` | 1404-1425 | `_circuitsViewModel.Collectors?[SelectedCollectorIndex]` (:1412) — дополнено по замечанию ревью |
| 9 | `UpdateCollectorSpecifications` | 1430-1444 | `_circuitsViewModel.Collectors` (:1435) — дополнено по замечанию ревью |
| 10 | `CheckDataReadiness` | 1477-1509 | `SelectedCity != null` (:1482), `IsValid` конструкции (:1488), `Result == null/!IsValid` и `SelectedPipe` (:1494-1498), `Collectors` (:1505-1507) |
| 11 | `RecalculateKpi` + KPI-хелперы (`CalculateTotalPower` :1173, `CalculateSystemVolume` :1199, `CalculatePumpParameters` :1241, `UpdatePumpHead` :1285, `CalculateExpansionTank` :1319) | 1162-1319 | `_circuitsViewModel.Collectors` во всей цепочке |
| 12 | `SaveCurrentProject` (custom templates) | 1692-1730 | `_constructionViewModel.Templates` (:1694); остальные блоки уже читают канонические снапшоты (:1673, :1734, :1742, :1748) |
| 13 | `HasUnsavedData` (obsolete) | 1757-1766 | `SelectedCity != null`, `SelectedPipe != null`, `Collectors.Any(...)` (:1763-1765) |

Selection (`SelectedCollectorIndex`, `SelectedCollector`) читается из `_circuitsViewModel` в сайтах 5/8; в каноне остаётся Results-owned UI-состоянием (ST-027).

Сайтов вне перечисленных семей нет (строки 407 и 1594 — комментарии; `Reset()` module-VM чтений не выполняет).

## Frozen external triggers

- `ResultsPdfDataBuilder.cs:43` → `results.RefreshAll()` (перед PDF-экспортом).
- `MainWindow.xaml.cs:254` → `ResultsViewModel.LoadHydraulicsDataOnNavigate()`.
- `ResultsViewModel.LoadProjectDataAsync` (:1616-1656) — restore handoff через `ProjectLoadOrchestrator` (Phase 7 boundary), `RefreshAll()` только после успешного restore.
- `Reset()` (:1553-1611) — очистка проекции, `_isResetting` guard, `MarkClean()`.
- `ProjectChanged` event — публичная поверхность сохраняется.

## Failure QA

Проба «Results пишет обратно в module/canonical state»: статическая инспекция — сеттеры Results пишут только собственные `[ObservableProperty]`-поля или pass-through в `IProjectStateService` (с dirty-guard); вызовов мутирующих методов module-ViewModel или `ProjectSession`-слайсов из Results не найдено. Позитивное рантайм-доказательство read-only контрактов остаётся за suite'ами slice 3-6 и sentinel slice 7.

## Dirty baseline

Дельта фазы относительно существующего dirty baseline (`.opencode/commands/*` — защищённые pre-existing пути): файл плана, настоящий каталог evidence, правки `AGENTS.md` (environment-adaptive rules, control/docs-only) и dated-запись `TASK_CONTEXT.md` (гейты). Производственный код не менялся.

## Статус

SLICE 1: PASS
