# Task 9 Divergence Notes

No behavioral divergence was identified. The lifecycle edits are limited to
routing reset origins through `ProjectSession.HydraulicsState`:

- `ProjectLoadOrchestrator.ResetModules()` uses `ProjectLoadReset` before the
  adapter cleanup call.
- `MainViewModel.PerformNewCalculationReset()` uses `UserReset`.
- Existing Todo 10 restore and fallback ordering remains unchanged.
- `CircuitsViewModel.Reset()` was not modified; it remains adapter cleanup.

The restore rejection probe records the canonical state contract: only
`ProjectLoad` is accepted by `Restore`; other origins return `Rejected`, retain
the previous snapshot, and emit no change event.

Two consecutive focused Release runs passed with `114 passed, 0 failed` and
one accepted `NotExecuted` fixture identity each. Debug and Release production
builds and the Release test assembly build passed with zero warnings and zero
errors.

## Dirty authority transfer

Designed divergence approved by the owner during the tasks-5-7 correction lane
(2026-08-24). After the canonical conversion, hydraulics dirty intent no longer
originates in `CircuitsViewModel`: the canonical
`ProjectSession.HydraulicsState` slice raises `IMarkDirtyService.MarkDirty()`
for user-origin mutations, per the contract line «Dirty intent: только origin
User поднимает IMarkDirtyService» — the state raises it, not the VM.

Observable consequence in
`tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`,
test `RepeatedResetCycles_DoNotDuplicateCircuitsEventSubscriptions`:

- `markDirtyMock.Verify(m => m.MarkDirty(), Times.Never, ...)` — the VM-injected
  `IMarkDirtyService` mock no longer receives direct calls from the adapter;
  this is the designed transfer, not an assertion weakening of the
  subscription-duplication guard.
- `Assert.That(session.IsDirty, Is.True)` — the aggregate-root-level proof that
  adding a circuit after repeated reset/load cycles still marks the project
  dirty exactly through the canonical owner.

The aggregate-root `IsDirty` assertion is stronger than the relocated internal
call counter because it verifies the externally visible dirty outcome while
remaining insensitive to which internal component performs the write.

## Status machine termination (per-attempt, unconditional)

Designed divergence approved during the tasks-5-7 correction lane (2026-08-24,
coordinator FIX B). Legacy `CircuitsViewModel.Calculate` terminated the status
machine conditionally:

```csharp
finally
{
    if (string.IsNullOrEmpty(ValidationMessage))
    {
        _calculationStateService.ResetHydraulicsState();
    }
}
```

so a validation error left the status sticky in Error until the next successful
recalculation. The canonical coordinator terminates unconditionally — exactly
one `ResetHydraulicsState` per calculation attempt, success or failure
(`HydraulicsStateCoordinator.RunCalculation` finally block). Rationale: FIX B
exists to guarantee that every attempt exits the Calculating phase (including
early-exit paths such as null selected collector), preventing stuck busy state.

Call-count adjudication against the failing characterization expectations:

- `Calculate_InvalidConcentration5Percent_SetsValidationMessage_ReturnsEarly`:
  one attempt -> exactly one reset (legacy expected zero). Not duplication.
- `Calculate_ThenFixConcentration_ClearsValidationMessage`: two attempts ->
  exactly two resets, one per attempt (legacy total was one). No double-fire
  within either attempt; `SetHydraulicsError` remains exactly once.
- The user-facing error channel is unchanged: `ValidationMessage` persists
  independently of the status machine and is still cleared by a successful
  recalculation.

Both tests were updated to encode the per-attempt termination contract;
`CircuitsViewModelEventLeakTests` now proves dirty exactly-once at the
aggregate root (`ProjectSession.IsDirty` transition count) instead of counting
calls on the VM-injected `IMarkDirtyService`, which is no longer a dirty
channel by design (`ProjectSessionHydraulicsState` receives
`hydraulicsDirtyService ?? this`).

## Auto-recalculation dirty churn eliminated

Fourth instance of the same designed transfer, surfaced by the full-suite run
(2026-08-24). Legacy behavior: the auto-re calculation triggered during the
second load's Climate lifecycle publication raised two session `IsDirty`
transitions per cycle (`RepeatedLoadResetCycles_DoNotMultiplyEventsSubscriptions
OrCalculations`, pinned as a fixed `+2` offset). Canonical behavior:
calculation-origin work never raises dirty — the `_isCalculating` adapter guard
suppresses user-origin canonical writes while the coordinator runs, and
`CompleteCalculation` uses the Calculation origin. The assertion now pins cycle
equality (zero multiplication) instead of the legacy surplus; the parallel
`HydraulicsCalculationDelta = firstCycle + 2` recalculation-surplus pin is
unchanged and still passes.

## DI construction-cycle deadlock fixed (production)

The correction introduced a third constructor parameter
`ProjectSession(..., IMarkDirtyService? hydraulicsDirtyService = null)` for the
canonical hydraulics dirty wiring. With the default type-activation
registration `AddSingleton<ProjectSession>()`, Microsoft.Extensions.DependencyInjection
resolved that parameter from the factory registration
`IMarkDirtyService -> sp.GetRequiredService<ProjectSession>()`, re-entering the
in-flight singleton construction of `ProjectSession` itself. Under .NET 8 DI
thread-safety locks this deadlocks the first `GetRequiredService` that touches
the aggregate root; full-DI characterization tests
(`MainViewModelTests.NewCalculation_ChangedClimateReset_*`,
`NewCalculation_ReplacesEditedConstruction_*`) hung indefinitely at provider
resolution.

Fix (minimal, composition-only): explicit factory registration in
`ServiceCollectionExtensions.AddResultsModule` resolving `IClimateData` and
`CalculationContext` explicitly and passing `hydraulicsDirtyService: null`.
That value is canonical anyway — `ProjectSessionHydraulicsState` falls back to
`markDirtyService ?? this`, keeping the aggregate root as its own dirty owner.
Diagnosis evidence: stepwise probe log (P0b→R2 hang point), vstest `--blame-hang`
dump of the hung testhost, dump string scan excluding a modal-dialog cause;
probe artifacts removed after diagnosis.
