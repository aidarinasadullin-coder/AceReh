# Final F4 — Consolidated Stop Check

Дата: 2026-09-03.

REVIEW_ID: F4-P8-CONSOLIDATED-STOP
SUBJECT: Phase 8 executed result — `phase-8-results-derived-projection`
RECEIPT: `docs/architecture-migration/evidence/phase-8-results-derived-projection/` (slices 1-8, F1-F3, generation-hash receipt)
VERDICT: APPROVE
REASON: The three review domains returned APPROVE (F1 scope/provenance/invariants, F2 architecture/code quality, F3 executable QA/user risk). The executed write-set matches the frozen plan + Amendment 1; all plan slices completed with executable evidence; the dossier (ten map overlays, model `INV-009` verified with `EV-P8-*` evidence, deterministic widget regeneration) matches the live boundary.

## Stop — explicit owner decisions required before closure

The phase stops here for **owner result acceptance**. Acceptance is a separate owner decision and is NOT granted by this wave. Three items need explicit owner direction:

1. **Result acceptance** of Phase 8 (`Принимаю результат Phase 8` or adjustments).
2. **Staged-scope fallback confirmation** (slice 4): the hydraulics partial re-source (shared `CircuitRow` objects, `HydraulicSummaryBuilder(CollectorData)` input, VM selection read → Phase 9) was chosen by acting-agent best judgment after an unanswered owner question. Confirm or direct a Phase 8.1 full re-source.
3. **Pre-existing baseline anomaly** (`LIM-P8-2`): restore no longer imports custom materials/templates (removed by a pre-existing dirty delta to `ProjectLoadOrchestrator.cs` before this session). Five characterization tests expect the removed import. Owner decides: restore the import (new approved change) or re-pin the tests to the current no-import behavior.

## Domain summary

| Domain | Receipt | Verdict |
|---|---|---|
| Conformance / Scope / Provenance | `final-f1-scope-provenance.md` | APPROVE |
| Architecture / Code Quality | `final-f2-architecture.md` | APPROVE |
| Executable QA / User Risk | `final-f3-executable-qa.md` | APPROVE |
| Consolidated | this file | APPROVE — stop for owner acceptance |
