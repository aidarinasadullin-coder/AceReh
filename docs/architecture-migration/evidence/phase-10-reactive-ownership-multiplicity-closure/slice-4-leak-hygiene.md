# Slice 4 — Production leak hygiene: justified no-op

Phase 10 (`phase-10-reactive-ownership-multiplicity-closure`). The frozen plan
explicitly allows this slice to be a no-op: "if the census and harness show
every edge is either application-lifetime-by-design or already correctly
unsubscribed, the receipt records that and the lane continues." That is the
measured outcome. **Zero production lines changed.**

## Why no leak was proven

1. **Census (Slice 1)**: every production subscription row is either
   - **APP** — publisher and subscriber are the same DI singletons living for
     the whole application lifetime (`HydraulicsStateCoordinator` rows 1–3,
     `CalculationStateService` rows 4–5, `ThermalStateCoordinator` rows 6–7
     with `Dispose`-based teardown, adapter rows 8–11, 16–17, 19–25, window
     row 26, model rows 27–28), or
   - **PER-ITEM** — replaceable-item subscriptions with symmetric
     attach/detach and set-based double-attach guards (`CircuitsViewModel`
     rows 12–15 with `_subscribedCollectors` and old-instance
     `InputData` unsubscribe; `ConstructionViewModel` row 18 with
     `_subscribedLayers` reconcile).
2. **Harness (Slices 2–3)**: the exact handler-count snapshot across all 12
   publishers is asserted equal to census expectations and is re-asserted
   unchanged after **every one of four** post-warmup load+reset cycles —
   the doubled cycle repetition the plan requires for post-fix proofs (the
   suite design called for two; the recorded GREEN suite holds four).
   `ProjectLifecycleFlowCharacterizationTests.
   RepeatedResetCycles_DoNotDuplicateCircuitsEventSubscriptions` (3 reset
   cycles) and `RestoreModulesFromProjectAsync_Twice_...` pin the same
   property from the accepted Phase 9 baseline.
3. **No leak fix list exists**: no census row was observed to multiply
   handlers across new/load/second-load/reset/repeated-reset cycles in any
   measured run. The RED probe (Slice 3) proved the harness detects exactly
   such multiplication, so the no-op is not a blind spot of the measurement.

## Application-lifetime subscriptions without unsubscribe — by-design justifications

- `HydraulicsStateCoordinator` (`ContextChanged`, `PipeSpacingChanged`,
  `StateChanged`): DI singleton; the only instance lives from composition to
  process exit; `Connect` callbacks are fields, not event subscriptions.
- `CalculationStateService` (`ThermalState.Changed`, `HydraulicsState.Changed`):
  DI singleton wrapping the DI-singleton session; the stored handler fields
  exist precisely so the subscription is idempotent, never duplicated.
- `ThermalStateCoordinator` upstream subscriptions: single attach per surface
  (guard-proved since Phase 4), `IDisposable` teardown implemented
  (`Dispose()` unsubscribes both upstream handlers) — the teardown rule exists
  and is intact.
- Module adapters + `MainViewModel` + `MainWindow`: singleton-view composition;
  WPF window equals application lifetime.
- View-infrastructure subscriptions (behaviors, editor views, visualization
  view): symmetric `Loaded`/`Unloaded` or `DataContextChanged` teardown, out
  of the six canonical domain views (census classification section).

## Frozen contracts unchanged — command (plan-exact) and result

Build-before-test: the tree was rebuilt immediately before this run in the
Slice 3 GREEN step on the identical write-set (`dotnet build ... -c Debug --nologo`
→ 0 warnings / 0 errors, recorded in `slice-3-lifecycle-tests.md`); the
`--no-build` test command below consumed that build.

```
dotnet test ... --filter "FullyQualifiedName~ReactiveSubscriptionLifecycleTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ClimateThermalInvalidationRegressionTests|FullyQualifiedName~ThermalMultiplicityCharacterizationTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests" --logger "trx;LogFileName=slice-4-leak-hygiene.trx" --results-directory "docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs"
```

Result: **94 passed / 0 failed / 0 skipped** (482 ms). TRX:
`logs/slice-4-leak-hygiene.trx`. Publication order, origin classification, and
event payloads are untouched — no production edit exists in this phase, so the
frozen Phase 2–9 contract suites pass unmodified by construction.

**SLICE 4: PASS (justified no-op)**
