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
