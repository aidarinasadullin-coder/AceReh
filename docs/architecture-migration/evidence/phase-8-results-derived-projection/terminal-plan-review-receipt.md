# Terminal plan review receipt — phase-8-results-derived-projection

REVIEW_ID: TERMINAL-PLAN-REVIEW-P8-ZCODE-1
SUBJECT: `docs/architecture-migration/plans/phase-8-results-derived-projection.md` — candidate identity at review: exactly `34593` UTF-8 bytes, SHA-256 `EC762434820E87EA92B9A37A4FD694DCABD81181F93C1B6EA035FFF5674F5C67`
RECEIPT: this file (inline receipt; ZCode session — no Momus agent, per `AGENTS.md` "Environment-adaptive operation (non-OpenCode sessions)")

## Review composition

One acting-agent terminal review cross-checked by one read-only independent
subagent pass (exploration-only, no writes). This is the single terminal review
for the candidate; no multi-loop chain was recreated.

## Independent pass findings (all verified against live code)

1. All referenced source files, maps, plans, and the widget verifier exist.
2. All 14 test class names used in `dotnet test --filter` arguments correspond
   to real test classes; no filter can pass with 0 tests due to a typo.
3. `ResultsViewModel` claims confirmed: constructor takes the four concrete
   module ViewModels plus `ProjectLoadOrchestrator`
   (`ResultsViewModel.cs:483-498`); read sites confirmed at the claimed
   locations, including `SaveCurrentProject` custom templates at :1694 and the
   obsolete `HasUnsavedData` at :1757-1765. Inventory additions required
   (incorporated): `UpdateCircuitsFilter` (:1404/:1412) and
   `UpdateCollectorSpecifications` (:1430/:1435) named explicitly; KPI helper
   chain covered by `RecalculateKpi` naming.
4. Canonical sources confirmed: `CalculationContext.ThermalResult` written only
   via `ThermalStateCoordinator` (:147, :166, :187, :239, :240);
   hydraulics results only via `HydraulicsStateCoordinator` (:57).
   `ConstructionStateProjection` exposes R1Total/R2Total/LambdaE/IsValid; layer
   lists live on `ConstructionStateSnapshot`. `ClimateStateSnapshot` carries
   the city name string, AirTemperature, Zone, WindSpeed, SnowfallIntensity.
5. Genuine canonical gap: `ColdPeriodDays` reads `CityInfo.Period_0_Days`
   through `ClimateViewModel.SelectedCity` (`ResultsViewModel.cs:1026`);
   `ProjectSessionClimateState` retains no `CityInfo`/`Period_0_Days`, and the
   module adapter re-resolves the city by name with a fabricated fallback
   (`ClimateViewModel.cs:681-692`). Recorded in the plan (slice 2) as the
   expected `OWNER_DECISION_REQUIRED` candidate with a required concrete owner
   choice.
6. Wording precision (incorporated): `DEC-001 = A` "only production writers"
   is worded as "only production result publishers", acknowledging the
   characterized `CalculationContext.Reset()` null-writers
   (`ProjectLoadOrchestrator.cs:80`, `MainViewModel.cs:236`).
7. No scope contradictions: no slice touches `ProjectLoadOrchestrator`
   internals, the legacy alias registrations
   (`ServiceCollectionExtensions.cs:201-203`), or adds a `CalculationContext`
   writer. `ResultsViewModel` currently has zero `CalculationContext`
   references, so the read-only projection goal starts from a clean baseline.
8. Feasibility: `tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj`
   exists; `docs/architecture-migration/widget/verify-widget.mjs` exists;
   `dotnet` SDK `8.0.418` present. Git Bash adaptation of `mkdir` redirection
   recorded (semantics preserved, per AGENTS.md).

## Candidate corrections applied before freeze (minor, one combined review)

- Todo 1 acceptance: `UpdateCircuitsFilter` and `UpdateCollectorSpecifications`
  added to the required read inventory.
- Slice 2 acceptance: the `ColdPeriodDays`/`Period_0_Days` canonical gap is
  named explicitly with the required owner choice.
- TL;DR: `DEC-001 = A` wording acknowledges the characterized `Reset()`
  null-writers.

VERDICT: APPROVE
REASON: The candidate is grounded in the live codebase (every referenced
artifact, test filter, and read site verified by an independent read-only
pass), preserves the Phase 2-7 contracts and `DEC-001 = A`, contains no
must-not-have violations, and carries agent-executable happy/failure QA with
concrete commands, receipts, and the build-before-test/zero-test rules for
every slice. The single canonical gap (`ColdPeriodDays`) is correctly routed
to an explicit owner decision instead of an invented fallback. Minor review
notes were incorporated before freeze; the review identity above covers the
corrected candidate.
