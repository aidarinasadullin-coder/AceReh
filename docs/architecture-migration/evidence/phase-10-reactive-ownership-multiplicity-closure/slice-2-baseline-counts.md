# Slice 2 — Counting harness freeze and baseline per-edge counters

Phase 10 (`phase-10-reactive-ownership-multiplicity-closure`). Test-only
write-set: new `ReactiveSubscriptionLifecycleTests`
(`tests/SnowMeltingCalculator.Tests/Services/Project/ReactiveSubscriptionLifecycleTests.cs`,
name fixed by the frozen plan) — a counting harness over a production-shaped
singleton graph: one `ProjectSession` (with one `ClimateData` and one
`CalculationContext`), one `CalculationStateService`, one
`ThermalStateCoordinator` wired exactly as DI does
(`session.ThermalState`, shared context, shared climate data, shared
`ConstructionState.CurrentProjection`), the four module adapters
(`ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`,
`CircuitsViewModel` via `HydraulicsTestDependencyFactory`), one
`ProjectLoadOrchestrator`, one `ResultsViewModel`, one `MainViewModel`.
Probe handlers count every canonical publication surface; reflection reads the
backing delegate fields for exact handler counts. `AppSettings` singleton is
reset per test (same pattern as `MainViewModelTests`). No production code was
touched.

The harness observes every Slice 1 census surface: ContextChanged,
StateChanged, PipeSpacingChanged, the four slice `Changed` events,
`Coordinator.Completion`/`UpstreamObserved`, thermal calculator invocations,
hydraulics calculator invocations, Results projection updates
(`HydraulicSummaryCards.CollectionChanged` — the observable rebuild surface of
`RefreshAll`), session `PropertyChanged` with dirty/clean transition split,
plus per-origin completion dictionaries.

## Build-before-test

`dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`
→ 0 warnings, 0 errors.

## Command (plan-exact) and result

```
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ReactiveSubscriptionLifecycleTests" --logger "trx;LogFileName=slice-2-baseline-counts.trx" --results-directory "docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs"
```

Result: **11 passed / 0 failed / 0 skipped**. TRX:
`logs/slice-2-baseline-counts.trx`. (The run was repeated during harness
calibration; the recorded TRX is the final all-green run; determinism is
asserted *inside* the suite by comparing two consecutive identical runs per
scenario.)

## Measured baseline counters (receipt facts, not estimates)

`Results` = Results projection updates (`HydraulicSummaryCards` rebuild events
per `RefreshAll`); `Context[Other]` = the `CalculationContext.Reset()`
publication (`PropertyName="Reset"`, source `"System"`). Deltas are per single
scenario execution with identical preconditions (asserted equal across two
consecutive runs by the suite).

| Scenario (precondition) | Context publications | StateChanged | PipeSpacing | Canonical completions (by origin) | Calc (Thermal/Hydraulics) | Results proj. | Dirty+ |
|---|---|---|---|---|---|---|---|
| New calculation, clean graph (`NewCalculationCommand`) | Reset=1 | 0 | 0 | Construction: Reset=1 | 0/0 | 1 | 0 |
| Load B onto A (`LoadProjectDataAsync`) | ThermalInputs=1, ThermalResult=1, Hydraulics=2 | 4 | 1 | Thermal: ProjectLoad=1; Hydraulics: Calculation=4 | 0/0 | 1 | **0** |
| Second load A onto B | same as load | 4 | 1 | same | 0/0 | 1 | **0** |
| Reset after load (`ResetModules`) | Climate=1, Hydraulics=1, Reset=1 | 2 | 0 | Climate: ProjectLoadReset=1; Construction: Reset=1; Thermal: ProjectLoadReset=1; Hydraulics: Calculation=2 | 0/0 | 0 | **0** |
| Repeated reset on default state (steady cycle) | Reset=1 | 0 | 0 | Construction: Reset=1 | 0/0 | 0 | **0** |
| Climate user edit after load (`ApplyIndividualEdit`, User) | Climate=1, Hydraulics=1 | 3 | 0 | Climate: User=1; Thermal: ClimateInvalidation=1; Coordinator Completion=1; UpstreamObserved=1; Hydraulics: Calculation=2 | 0/0 | 0 | **1** |
| Thermal user edit (`SupplyTemperature=65`) | — | 1 | 0 | Thermal: User=1; Coordinator Completion=1 | 0/0 | 0 | **1** |
| Thermal calculate command (after edit) | ThermalInputs=1, ThermalResult=1, Hydraulics=1 | 4 | 0 | Thermal: Calculation=2 (Begin+Complete); Coordinator Completion=2; Hydraulics: Calculation=2 | 1/0 | 0 | **0** |
| Hydraulics user edit (circuit length, after clean) | — | 0 | 0 | Hydraulics: User=1 | 0/0 | 0 | **1** |
| Load+reset cycle (warmup-steady, stable ×4) | Climate=2, ThermalInputs=1, ThermalResult=1, Hydraulics=3, Reset=1 | 6 | 0 | Climate: Load=1, ProjectLoadReset=1; Construction: ProjectLoad=1, Reset=1; Thermal: ProjectLoad=1, ProjectLoadReset=1; Hydraulics: Calculation=6, ProjectLoad=1 | 0/2 | 1 | **0** |

## Contract-consistency annotations

- Exactly-once contracts hold: one publication per completed logical change
  (`RE-003`: climate user edit → 1 completion → 1 `ContextClimate`; `RE-009`:
  reset produces exactly one Reset-origin completion and no downstream
  publication; a no-op repeated reset is almost fully quiet: 1 quiet Reset
  completion + 1 context reset, zero StateChanged, zero dirty).
- Lifecycle origins (load, second load, reset, restore, new-calculation) all
  show **Dirty+ = 0** — user dirty semantics are never created by system
  paths (frozen Phase 2–9 dirty-ownership contracts).
- Hydraulics `Calculation`-origin completions are the frozen
  phase-transition bookkeeping of an attempt (`BeginCalculation` →
  `Calculating`, terminal transition → `Actual`/`Error`), not user commits;
  User-origin commits appear exactly once per logical hydraulics edit.
- Thermal `Calculation`-origin completion pairs (Begin+Complete) match the
  DEC-T05 orchestration frozen in Phase 4/7 (`RE-P4-002`, `RE-P5-HYD-003`).
- No count contradicts a frozen contract; no `OWNER_DECISION_REQUIRED`
  arises from this slice.

## Determinism proof

Every baseline test asserts two consecutive identical runs produce identical
counter deltas (`AssertDeltasEqual`); the load/reset/second-load scenarios
measure "same action, same precondition" pairs (A→B, B→A, load→reset) so the
precondition is exactly reproduced. The lifecycle test additionally holds
per-cycle deltas identical across four load+reset cycles after warmup.

## Failure-mode honesty

Initial harness calibration runs (recorded in session logs, not retained as
TRX) exposed three harness-expectation defects, fixed in the harness itself:
(1) first-vs-second load/reset compare identical-precondition pairs;
(2) thermal input edit and explicit calculate are two logical actions (edit =
one User completion + one dirty; calculate = one Begin+Complete pair + one
inputs + one result publication) — matching the frozen DEC-T05 contract;
(3) a hydraulics dirty assertion must isolate the edit's dirty transition from
the preceding add-collector commit. No production code was changed at any
point; the measured production behavior was already deterministic.

**SLICE 2: PASS**
