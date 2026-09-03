# Terminal plan review receipt — phase-9-legacy-seams-cleanup

Дата: 2026-09-03. Среда: ZCode (environment-adaptive правила `AGENTS.md`; агенты
Prometheus/Momus отсутствуют). Терминальный review выполнил acting agent с одной
независимой read-only перекрёстной проверкой (subagent pass).

REVIEW_ID: TERMINAL-PLAN-REVIEW-P9-ZCODE-1
SUBJECT: frozen plan candidate `docs/architecture-migration/plans/phase-9-legacy-seams-cleanup.md`, exactly 41017 UTF-8 bytes, SHA-256 `59A2409624901C8167C4D43D40B2F9280D6D5E869D38F56FA66BABF210D1A6BB`
RECEIPT: this file (cross-check pass: independent read-only subagent, verdict `SUPPORT-APPROVE`, 3 minor findings, all incorporated before freeze; no BLOCKER findings)
VERDICT: APPROVE
REASON:

1. **Scope matches the named Phase 9 debts one-to-one.** In-scope items are
   exactly the residuals recorded at Phase 8 acceptance
   (`evidence/phase-8-results-derived-projection/owner-result-acceptance.md`):
   shared mutable `CircuitRow` objects with `CircuitsViewModel`,
   `HydraulicSummaryBuilder(CollectorData)` input, the `UpdateCollectorSummary`
   VM selection read (LIM-P8-1), `ST-026`/`ST-027` partial coverage, the
   `INV-016` Results clause, plus the deferred `INV-008` orchestrator
   decoupling, the legacy forwarding alias removal, and the LIM-P8-2 baseline
   anomaly direction request from the Phase 8 completion entry in
   `TASK_CONTEXT.md`. Nothing from the phase-8 "Explicitly out of scope" list
   is silently absorbed: `INV-010` closure, `CalculationContext` writer
   disposition (`DEC-001 = A`), Markdown removal, and export/PDF/Excel
   behavior changes are barred by "Must NOT have".
2. **Grounding verified against live code** (commit `3a077c7`, clean tracked
   baseline). All file/line anchors confirmed by the independent pass within
   tolerance; the three minor findings were corrected before freeze: the
   4-test `LoadProjectDataAsync_{Early,Late}RestoreFailure_*` cluster is split
   2+2 across `ProjectLifecycleFlowCharacterizationTests.cs` and
   `ThermalMultiplicityCharacterizationTests.cs` (slice-2 references and
   focused filter updated to cover all 5 named tests), the
   `ProjectSessionClimateState`/`ProjectSessionHydraulicsState` dirty-param
   attribution is corrected (both alias params are in the removal set), the
   slice-5 static test name is fixed as `ApplicationServiceViewModelDecouplingTests`
   so the filter cannot execute 0 tests, and the `DisplayMode` write anchor is
   corrected to Results `:1429`.
3. **Owner decisions preserved, none violated.** `DEC-001 = A` (CalculationContext
   read-projection seam, coordinator-only production writers) is preserved
   verbatim; `DEC-003 = C` is cited correctly in the LIM-P8-2 Option A
   (external catalog imports are not transactionally reversible; the receipt
   records the failure-after-import consequence rather than asserting
   reversibility); `.smc` wire compatibility is kept stricter than `DEC-002`
   requires. The two decisions this phase needs (LIM-P8-2 direction; verifier
   exemplar amendment authorization) are explicit stop points, consistent with
   the Phase 7.5 amendment precedent for `widget/verify-widget.mjs`.
4. **Commands are worker-executable and 0-test-safe.** Every slice has
   build-before-test, quoted filters referencing 13 verified real test classes
   plus the one deliberately new slice-5 class, unique TRX filenames, receipts
   under `evidence/phase-9-legacy-seams-cleanup/`, and at least one happy and
   one failure assertion. `mkdir -p` is an allowed Git Bash adaptation.
5. **Gates intact.** The plan is planning-only: no production edits, no test
   execution, no staging/commit in the planning session; approval stops at
   explicit owner plan approval and does not authorize execution. The frozen
   candidate bytes are the bytes recorded in SUBJECT; any correction creates a
   new candidate and a new terminal review.
