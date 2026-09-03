# Final F1 — Scope, Provenance and Invariant Check

Дата: 2026-09-03.

REVIEW_ID: F1-P9-SCOPE-PROVENANCE
SUBJECT: Phase 9 executed result — scope, provenance, invariant audit
RECEIPT: this file; consolidated in `final-f4-consolidated-stop.md`
VERDICT: APPROVE
REASON:

1. **Plan identity**: executed against the frozen canonical plan
   `docs/architecture-migration/plans/phase-9-legacy-seams-cleanup.md`
   (41017 bytes, SHA-256 `59A2409624901C8167C4D43D40B2F9280D6D5E869D38F56FA66BABF210D1A6BB`),
   byte-identical to the terminally reviewed candidate (terminal receipt
   `TERMINAL-PLAN-REVIEW-P9-ZCODE-1`, owner approval
   `OWNER-PLAN-APPROVAL-PHASE-9`). No post-freeze plan edits; deviations are
   recorded as in-slice receipts (slice-5 interface fallback permitted by the
   plan; slice-6 IMarkDirtyService internal-seam retention with disproven
   "dead params" premise — both recorded in receipts and TASK_CONTEXT).
2. **Scope conformance**: every in-scope item delivered (shared CircuitRow
   seams, builder/selection canonicalization, INV-008 decoupling with static
   test, alias removal, LIM-P8-2 decision B). No `INV-010` closure work, no
   `CalculationContext` writer change (`DEC-001 = A` intact; ST-020..022
   untouched), no `.smc` format change (`git diff -- '*.smc'` empty), no
   Markdown/export work, no second canonical store or restore-boundary
   redesign.
3. **Owner gates honored**: plan approval and execution authorization are
   separate recorded decisions; LIM-P8-2 option B recorded verbatim; result
   acceptance NOT claimed — the phase stops for it. The verifier exemplar
   amendment was NOT executed without authorization (recorded PENDING).
4. **Provenance**: receipts slice-1..slice-8 + TRX logs under
   `evidence/phase-9-legacy-seams-cleanup/logs/`; model evidence EV-P9-SLICE-2..7;
   dirty baseline-relative write-set matches the plan's named files (plus the
   plan-sanctioned additions: `ProjectRestoreAdapters.cs`, `ReportDataSources.cs`,
   `HydraulicCircuitRowProjection.cs`, `ResultsOwnedCircuitProjectionTests.cs`,
   `ApplicationServiceViewModelDecouplingTests.cs`, test-support
   `ProjectStateService.cs`, `CollectorHydraulicSummaryCard` additive ctor).
5. **Preserved invariants**: Phase 7 restore contracts re-proven (slice 5),
   Phase 8 projection contracts green (stabilization suites), `INV-012` wire
   compatibility (fixtures untouched), `DEC-002`/`DEC-003` respected
   (import-less restore consistent with read-only-catalog contract; DEC-003
   non-transactional semantics characterized).
