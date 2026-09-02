# Phase 8 Owner Result Acceptance

REVIEW_ID: OWNER-RESULT-ACCEPTANCE-PHASE-8
SUBJECT: phase-8-results-derived-projection final result after F1-F4 APPROVE
RECEIPT: docs/architecture-migration/evidence/phase-8-results-derived-projection/owner-result-acceptance.md
VERDICT: APPROVE
REASON: Owner explicitly accepted the Phase 8 result after the final verification wave completed with F1, F2, F3, and F4 APPROVE (`final-f4-consolidated-stop.md`).

## Owner Statement

On 2026-09-03, the owner stated:

```text
Принимаю результат Phase 8
```

## Accepted Result

- Phase: `phase-8-results-derived-projection`
- Frozen plan: `docs/architecture-migration/plans/phase-8-results-derived-projection.md` (SHA-256 `EC762434820E87EA92B9A37A4FD694DCABD81181F93C1B6EA035FFF5674F5C67`)
- Amendment 1 (owner decision B, `Period0Days` canonicalization): `docs/architecture-migration/plans/phase-8-results-derived-projection.amendment-1-coldperioddays-canonicalization.md`
- Final verification receipts:
  - `final-f1-scope-provenance.md` (APPROVE)
  - `final-f2-architecture.md` (APPROVE)
  - `final-f3-executable-qa.md` (APPROVE)
  - `final-f4-consolidated-stop.md` (APPROVE — stop for owner acceptance)
- Executed evidence: `slice-1..slice-8` receipts with TRX logs under `logs/`; full-suite regression 2023 passed / 5 pre-existing import-removal failures / 1 known external-fixture skip.
- Dossier state: `INV-009` verified/implemented with `EV-P8-*` model evidence; `ST-003`/`ST-024`/`ST-025` covered; `ST-026`/`ST-027` partial with named Phase 9 residuals; deterministic widget (sha256 `0601835D18A6464A580B24FCAD7396FCBBD340B032ABDCC614CD786B17B6E34C`, `model-v2` 33/21 and `runtime-v2` 47/20 PASS).

## Open Items Preserved by This Acceptance

Acceptance does not close these; they remain recorded and open:

1. **Staged hydraulics residuals (Phase 9)**: shared mutable `CircuitRow` objects with `CircuitsViewModel`, `HydraulicSummaryBuilder(CollectorData)` input, VM selection read in `UpdateCollectorSummary` (`LIM-P8-1`). By acceptance, the owner's "Принимаю результат Phase 8" implicitly confirms the staged slice-4 fallback (full re-source remains available as a future Phase 9 item alongside the other legacy-cleanup work).
2. **Pre-existing import-removal baseline anomaly (`LIM-P8-2`)**: restore no longer imports custom materials/templates (removed by a pre-existing dirty delta to `ProjectLoadOrchestrator.cs` before Phase 8); 5 characterization tests expect the removed import and remain failing. This is outside the Phase 8 write-set and stays open for a separate owner direction.
3. Known environment skip: external fixture `D:\IA\ace\Тест\тест 40.smc` (RR-004).

## Gate Effect

This receipt records explicit owner result acceptance. It does not start a next phase, edit the frozen plan, reopen implementation slices, or authorize additional architecture work. Per `AGENTS.md`, after result acceptance the workflow awaits a new explicit owner direction; the next phase never starts implicitly.
