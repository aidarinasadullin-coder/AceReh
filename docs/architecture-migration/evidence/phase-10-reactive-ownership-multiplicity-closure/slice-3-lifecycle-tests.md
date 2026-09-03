# Slice 3 — `ReactiveSubscriptionLifecycleTests` RED probe + GREEN: the harness is binding

Phase 10 (`phase-10-reactive-ownership-multiplicity-closure`). Write-set:
test-only. The suite is the one created in Slice 2 (name fixed by the plan);
this slice proves it **cannot pass on multiplied subscriptions** (recorded RED
run) and then proves the accepted Phase 9 baseline **stable** without the
injection (recorded GREEN run), mirroring the Phase 9
`ApplicationServiceViewModelDecouplingTests` RED-then-GREEN precedent.

## Probe description (temporary scaffolding, excluded from the final tree)

One deliberate duplicate subscription was injected into the harness fixture
(`AttachProbeHandlers`): a second `Session.ThermalState.Changed += OnThermalChanged`
— duplicating, in test scaffolding, exactly the production subscription of
census row 4 (`CalculationStateService.cs:55`,
`_projectSession.ThermalState.Changed += _thermalChangedHandler`). The probe
was then removed; `grep -c "RED PROBE"` on the final test file returns `0`.

## RED run (recorded failing TRX)

Command (plan-exact):

```
dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo
dotnet test ... --filter "FullyQualifiedName~ReactiveSubscriptionLifecycleTests" --logger "trx;LogFileName=slice-3-lifecycle-RED.trx" --results-directory "docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs"
```

Result: **3 failed / 8 passed**. TRX: `logs/slice-3-lifecycle-RED.trx`.

Failed (exactly the assertions that must be sensitive to subscription
multiplication):

- `HandlerCounts_MatchPhase10Census_OnProductionShapedGraph` — handler count
  on `ProjectSessionThermalState.Changed` expected census(1)+probe(1)=2,
  actual 3.
- `ExactlyOnce_ClimateUserEdit_...` — the duplicated handler doubled the
  observed Thermal completion/Coordinator publication counts of the climate
  invalidation path (expected exactly 1 completion per logical action).
- `ExactlyOnce_ThermalUserEdit_...` — same multiplication on the thermal user
  edit path (expected exactly one User-origin completion).

The remaining 8 tests are relative-stability tests (two-run equality, per-cycle
equality, handler-count *equality* across cycles) which are insensitive to a
constant duplication present on both measured sides — by design; the exactness
and census tests carry the sensitivity proof.

## GREEN run (recorded)

Probe removed → rebuild (0 warnings / 0 errors) → same filter →
**11 passed / 0 failed**. TRX: `logs/slice-3-lifecycle-GREEN.trx`.

## GREEN assertions bound to the measured baseline (INV-010 executable heart)

For every edge with a multiplicity expectation from the census:

- **Identical handler counts before/after each lifecycle cycle** —
  `Lifecycle_LoadResetCycles_HandlerCountsAndPerCycleDeltasRemainStable` runs
  four load+reset cycles after warmup and asserts the full 12-publisher
  handler-count snapshot is unchanged after every cycle and equal to the
  census expectations at the end.
- **Exactly-once publication per completed logical change** —
  `ExactlyOnce_ClimateUserEdit_...` (1 completion → 1 `ContextClimate` → 1
  dirty → exactly 1 thermal invalidation), `ExactlyOnce_ThermalUserEdit_...`
  (edit = 1 User completion + 1 dirty; calculate = 1 Begin + 1 Complete pair,
  1 inputs + 1 result publication, 1 calculator run, 0 dirty),
  `ExactlyOnce_HydraulicsUserEdit_...` (1 User-origin commit, 1 dirty, no
  extra completions).
- **One recalculation path per valid Thermal publication** — the thermal
  calculate command yields exactly one hydraulics recalculation pass
  (`ContextHydraulics = 1` per calculate; `RE-P4-003`/two-glycol-read guard
  precedent preserved by the unmodified `ThermalMultiplicityCharacterizationTests`,
  which pass unmodified in the Slice 1 baseline and Slice 6 full regression).
- **Zero dirty transitions from load/reset/restore origins** —
  `LifecycleOrigins_NeverRaiseUserDirty_AcrossLoadResetRestore` and the
  `Dirty+ = 0` assertions across all lifecycle baseline scenarios.
- **Per-slice completion-boundary assertions with multi-field coverage** are
  consolidated separately in Slice 5 (`MutationBoundaryConsolidationTests`);
  this suite pins the reactive multiplicity half of `INV-010`.

Both TRX files sit in
`docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs/`:
`slice-3-lifecycle-RED.trx` (failing run recorded before GREEN) and
`slice-3-lifecycle-GREEN.trx`.

**SLICE 3: PASS**
