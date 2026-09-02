# Slice 3 — Climate/Construction projection re-sourcing (+ Amendment 1)

Класс: production/test. Дата: 2026-09-03.

## Scope executed

1. **Amendment 1 (owner decision B)**: `ClimateStateSnapshot` — additive 12-й
   позиционный параметр `int Period0Days = 0` (все существующие сайты
   конструирования компилируются без изменений);
   `ProjectSessionClimateState` — поле `_period0Days`, извлечение
   `city.Period_0_Days` в `ApplyCitySelection`/`ApplyProjectSnapshot`
   (`?? 0`), `ResetToCityData` (`city.Period_0_Days`), `ResetToDefaults` (0);
   `Snapshot` включает поле. `ApplyIndividualEdit` поле сохраняет.
   `ProjectLoadOrchestrator` НЕ менялся (city уже резолвится на :147-149).
2. `ResultsViewModel.LoadClimateData` — все 6 значений из
   `_projectSession.ClimateState.Snapshot`; `ColdPeriodDays` =
   `пустое имя ? 150 : Period0Days` (таблица эквивалентности — в Amendment 1).
3. `ResultsViewModel.LoadConstructionData` — R1/R2/LambdaE из
   `ConstructionState.CurrentProjection` (`IConstructionData.R1Total/
   R2Total/LambdaE`, формулы идентичны модели Construction);
   `Layers` реконструируется из `ConstructionStateSnapshot` через новый
   приватный `AppendLayers`: порядок присваиваний повторяет `Layer.Clone()`
   (Material → Thickness → CalculatedLambda → IsLambdaOverridden → Position →
   Order), `Material = GetMaterialById(id) ?? new Material { Id, Name =
   MaterialName ?? "Не указан" }`; `CalculatedR` воспроизводится автоматически
   (вычисляемое свойство с идентичной формулой). `_climateViewModel` и
   `_constructionViewModel` в методах проекции больше не читаются (поля
   остаются до slice 6 — их читают save-path/готовность).

## Commands

1. `dotnet build ... -c Debug --nologo` — exit 0, ошибок: 0 (7.26 s).
2. `dotnet test ... --no-build --filter "FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ClimateStateTests|FullyQualifiedName~ProjectSessionTests" --logger "trx;LogFileName=slice-3-climate-construction-resourcing.trx" ...` — **пройдено 111 / не пройдено 0 / пропущено 1 / всего 112** (12 s). TRX: `logs/slice-3-climate-construction-resourcing.trx`.

## Skip record

`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` —
NotExecuted: внешний fixture `D:\IA\ace\Тест\тест 40.smc` отсутствует в
worktree (`ResultsViewModelOpenProjectTests.cs:1697-1702`). Тот же
зарегистрированный skip, что и RR-004 (Phase 6); environment limitation, не
pass и не regression.

## Happy QA

Стабилизационные контракты (27), open-project characterization и 84
климатических/сессионных теста зелёные после re-sourcing: проекция выдаёт
те же значения в характеризованных сценариях, включая restore-путь
(`ApplyProjectSnapshot` теперь несёт канонический `Period0Days` из
`FindCityByName`).

## Failure QA

- Проба «письмо Results в каноническое состояние»: сеттеры не трогают
  `ProjectSession`/`CalculationContext`; реконструкция Layer создаёт новые
  объекты, canonical snapshots не мутируются.
- Проба второго расчёта: `RefreshAll` не вызывает координаторы (статически;
  рантайм-доказательство — slice 4/7).
- С known-риски записаны честно: (1) переименование материала в каталоге
  после создания слоя — реконструкция берёт текущий экземпляр репозитория
  (каталог — владелец данных материала), characterization divergences не
  обнаружил; (2) вырожденный случай repo-miss + null MaterialName закрыт
  стабом с «Не указан».

## Dirty baseline delta (этот slice)

`src/Services/Project/ClimateStateSnapshot.cs`,
`src/Services/Project/ProjectSessionClimateState.cs`,
`src/ViewModels/Results/ResultsViewModel.cs` — в границах Amendment 1 + slice 3.

## Статус

SLICE 3: PASS
