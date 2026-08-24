# Task 10 Divergence Notes

The frozen Todo 10 acceptance sequence is preserved: project DTOs are mapped
to a complete hydraulics snapshot, the canonical state is restored first, and
the WPF adapter is refreshed from that snapshot after restore. This keeps
inputs, circuit results, summaries, null result/summary values, and both
`FlowRegime` wire fields on one persistence boundary while preserving the
existing Version `1.1` format.

No behavioral divergence from the frozen plan was introduced. The adapter
does not assign `DpGesamt` or `ValveTurns` to `CircuitTemperatureResult` because
the current domain model exposes `DpGesamt` as a computed property and stores
`ValveTurns` on `CircuitRow`; the canonical snapshot and DTO retain both
values unchanged.

## Commit split note

Task 10 landed as two commits: `34b739e` (tests+evidence) and the follow-up
production commit created next, because the initial staging pass omitted the
production write-set. Both commits together form the Todo 10 boundary. Full
Release `1968 passed, 0 failed, 1 skipped` was verified on the combined tree.

The initial verification attempts hit stale/concurrent WPF build intermediates
in `src/obj` and test output locks. After terminating the compiler process and
disabling shared compilation, the focused Release characterization suite passed
13/13 and the production Release build passed with 0 warnings and 0 errors.

The full Release suite completed with 1961 passed, 7 failed, and 1 skipped.
The failures are outside this Todo 10 acceptance path: five pre-existing
`CalculationStateServiceTests` hydraulics status/event failures, one isolated
thermal repeated-load dirty-count failure, and one settings-file teardown lock.
