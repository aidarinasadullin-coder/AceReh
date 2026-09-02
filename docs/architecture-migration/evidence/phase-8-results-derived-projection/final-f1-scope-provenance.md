# Final F1 — Scope, Provenance and Invariant Check

Дата: 2026-09-03. ZCode session (environment-adaptive rules, AGENTS.md).

REVIEW_ID: F1-P8-SCOPE-PROVENANCE
SUBJECT: Phase 8 executed result — `phase-8-results-derived-projection` (+ Amendment 1)
RECEIPT: this file; consolidated in `final-f4-consolidated-stop.md`
VERDICT: APPROVE
REASON:

1. **Plan identity**: frozen plan `EC762434820E87EA92B9A37A4FD694DCABD81181F93C1B6EA035FFF5674F5C67` (terminal review `TERMINAL-PLAN-REVIEW-P8-ZCODE-1` APPROVE); Amendment 1 `17DFF9B3C1DDED6AC349DACA576D2B972A7124EACF07B9889B20AEE30732E72E` (owner decision B, review `TERMINAL-AMENDMENT-P8-ZCODE-1` APPROVE). Owner plan approval + execution authorization recorded in TASK_CONTEXT (2026-09-03).
2. **Must-NOT-have audit**: no `.smc`/`ProjectData` wire change (fixture-backed save tests green); `ProjectLoadOrchestrator` untouched by Phase 8; no new `CalculationContext` writers; legacy aliases untouched; no Markdown/export feature work; no second canonical store.
3. **Invariant preservation**: Phase 7 restore boundary (`BeginProjectRestore` lease, validation-before-mutation, exactly-once publication) — `ProjectSessionTests`/`ThermalStateCoordinatorTests`/`HydraulicsMultiplicityCharacterizationTests`/`ProjectLifecycleFlowCharacterizationTests` green. `DEC-001 = A` preserved (Results consumes `CalculationContext` read-only; snapshot.Result is the session-owned value).
4. **Amendment discipline**: the only mid-phase production expansion (climate `Period0Days`) went through owner decision B + amendment doc + combined review before implementation.
5. **Provenance boundary**: 5 pre-existing baseline failures (import-removal cluster) and the pre-existing dirty `ProjectLoadOrchestrator.cs`/`HydraulicsStateCoordinator.cs`/`ProjectSessionHydraulicsState.cs`/`CircuitsViewModel.cs` deltas are proven NOT Phase 8 work (git diff HEAD vs session write-set in slice-6 receipt) and are flagged (`LIM-P8-2`), not absorbed.
6. **Staged-scope fallback (slice 4)**: taken by acting-agent best judgment after an unanswered owner question; explicitly flagged for confirmation at owner result acceptance.
