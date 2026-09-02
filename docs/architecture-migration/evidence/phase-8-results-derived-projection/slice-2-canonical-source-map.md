# Slice 2 — canonical source map

Класс: tests/evidence only. Дата: 2026-09-03. Production-изменений нет.

## Commands

1. Build — переиспользован успешный build slice 1 (производственных изменений с ним не было; `--no-build` корректен).
2. `dotnet test ... --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ClimateStateTests|FullyQualifiedName~ThermalStateCoordinatorTests|FullyQualifiedName~ProjectSessionHydraulicsStateTests" --logger "trx;LogFileName=slice-2-canonical-source-map.trx" ...` — **пройдено 69 / не пройдено 0 / всего 69** (146 ms). TRX: `logs/slice-2-canonical-source-map.trx`.

## Read-to-source table (every Results projection read → canonical source)

| Results read (сайт slice 1) | Канонический источник | Доказательство |
|---|---|---|
| SelectedCity name (1) | `ClimateStateSnapshot.SelectedCity` (строка имени) | `ClimateStateSnapshot.cs:9`; тесты `ProjectSessionTests`/`ClimateStateTests` |
| AirTemperature (1) | `ClimateStateSnapshot.AirTemperature` | `ClimateStateSnapshot.cs:11` |
| ClimateZone (1) | `ClimateStateSnapshot.Zone` | `ClimateStateSnapshot.cs:17` |
| WindSpeed, SnowfallIntensity (1) | `ClimateStateSnapshot.WindSpeed/.SnowfallIntensity` | `ClimateStateSnapshot.cs:15-16`; строки уже читаются `SaveCurrentProject` из снапшота (:1676-1681) |
| **ColdPeriodDays (1)** | **GAP — см. OWNER DECISION ниже** | `CityInfo.Period_0_Days` (`CityInfo.cs:47`) отсутствует в `ClimateStateSnapshot`/`ProjectSessionClimateState`; адаптер резолвит город по имени (`ClimateViewModel.cs:685`) с fallback-fabrication `new CityInfo{Name,Region}` (:687-692) |
| R1, R2, LambdaE (2) | `ConstructionStateProjection.R1Total/.R2Total/.LambdaE/.IsValid` | `ConstructionStateProjection.cs:32,39,48,51` |
| Layers (2) | `ConstructionStateSnapshot.LayersAbovePipe/.LayersBelowPipe` (`ConstructionLayerSnapshot`: Id/MaterialId/MaterialName/Thickness/CalculatedLambda/Position/Order) | `ConstructionStateSnapshot.cs:15-16`; `ConstructionLayerSnapshot.cs:12`; в slice 3 требуется пофилевое доказательство маппинга в `Layer` |
| Thermal inputs (3) | уже канонические: `ThermalState.Snapshot` | `ResultsViewModel.cs:1068-1076` |
| Thermal result (3) | `CalculationContext.ThermalResult` — publisher только `ThermalStateCoordinator` | писатели: `ThermalStateCoordinator.cs:147,166,187,239,240`; null-writer `CalculationContext.Reset()` (characterized) |
| GlycolType, GlycolConcentration (4) | `HydraulicGlobalInputsSnapshot.GlycolType/.GlycolConcentration` | `HydraulicsStateSnapshots.cs:18-21` |
| Collectors/circuits (5-9, 11) | `ProjectSession.HydraulicsState` snapshot (контуры/коллекторы) + `CalculationContext.HydraulicsResults` (publisher — `HydraulicsStateCoordinator.cs:57`) | `ST-016..ST-019`, `ST-022` (state-ownership overlay); точный пофилевый маппинг доказывается в slice 4 focused-тестами |
| Readiness: city (10) | `ClimateStateSnapshot.IsCitySelected` + непустое имя | `ClimateStateSnapshot.cs:9,18` |
| Readiness: construction valid (10) | `ConstructionStateProjection.IsValid` | `ConstructionStateProjection.cs:51` |
| Readiness: thermal result/pipe (10) | `ThermalResult?.IsValid`; `ThermalStateSnapshot` pipe (`ThermalPipeSnapshot.Pipe`) | `CalculationContext.cs:87`; `ThermalStateSnapshots.cs:345` |
| Readiness: collectors (10) | hydraulics snapshot (как Collectors выше) | slice 4 |
| Selection `SelectedCollectorIndex` (5/8) | остаётся Results-owned UI-состоянием (ST-027) — не module input | `state-inventory.md` ST-027 |
| CustomTemplates (12) | `IConstructionTemplateRepository.GetAllAsync()` — тот же репозиторий, из которого адаптер заполняет зеркало (`ClimateViewModel`-паттерн: `ConstructionViewModel.RefreshCatalogsAsync` → `GetAllAsync`) | `ProjectSnapshotPersistenceInputs.cs:30-31` («Repository-backed persistence inputs without a ViewModel dependency»); `ConstructionViewModel.cs:678-684`; ветка (a)/(b) slice 6 |
| `HasUnsavedData` (13) | мёртвый код: `[Obsolete]`, private, **0 callers** в `src/` (grep) | записывается как dead code, удаления не требует |

## OWNER DECISION REQUIRED — ColdPeriodDays

`ResultsViewModel.cs:1026`: `ColdPeriodDays = _climateViewModel.SelectedCity?.Period_0_Days ?? 150`.

Канонического поля нет; значение — атрибут глобального городского каталога, не персистится в `.smc` (в `ClimateProjectData` его нет). Семантика адаптера: пустое имя → `SelectedCity = null` → 150; имя найдено → `Period_0_Days` из каталога; имя не найдено → fabricated `CityInfo` → дефолт `Period_0_Days` (уточняется в slice 3 по `CityInfo.cs:47`).

Варианты (выносятся владельцу):
- **A. Lookup в Results**: резолв имени из `ClimateStateSnapshot.SelectedCity` через `IClimateDataService.GetCityByName` с точным воспроизведением семантики адаптера. Write-set остаётся в `ResultsViewModel` (+1 сервисная зависимость, как `IMaterialRepository`); INV-009 закрывается полностью; эквивалентность доказывается тестами.
- **B. Канонизировать поле**: добавить `Period0Days` в `ProjectSessionClimateState`/`ClimateStateSnapshot` (извлекать в `ApplyCitySelection`, где `CityInfo` уже доступен). Архитектурно чище, но расширяет write-set на state-файлы Phase 2 → требуется поправка к плану.
- **C. Отложить**: оставить единственный module-VM read; INV-009 частично, остаток — долг Phase 9.

## Failure QA

Стоп-условие сработало ровно один раз и по назначению: `ColdPeriodDays` — единственное проектируемое значение без доказанного канонического эквивалента; решение вынесено владельцу, fallback-API не изобретается, писатели `CalculationContext` не расширяются.

## Статус

SLICE 2: PASS (карта полна; 1 owner decision вынесен до начала production-изменений slice 3)
