# F1 Conformance / Scope / Provenance

- Write-set: `phase-5.1-hydraulics-dirty-ownership-correction`
- Frozen plan SHA-256: `65999B8FF9008157ADAADC57D85138AF610829F70E57C6E9B0269F98C72EE46C`
- Verdict: `APPROVE`

## Checks

- `node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan`: PASS (`valid=true`).
- `git diff --check`: PASS.
- Fresh current baseline captured with `capture-baseline.ps1`: `27` status records, `21` protected dirty paths, `202` protected manifest entries.
- Symmetric protected verifier rerun against that manifest: `protected_mismatch_count=0`, exit `0`.
- The current implementation delta is directionally consistent with the goal: `CircuitsViewModel` no longer owns or calls `IMarkDirtyService`.
- Canonical `ProjectSessionHydraulicsState.Commit()` remains the User-only dirty owner.

## Baseline boundary

The owner-directed fresh baseline is the execution-time boundary for this verification run. It records the existing dirty/control-plane paths as pre-existing inputs and evaluates only subsequent protected drift. It is not represented as a clean historical HEAD baseline.

The post-baseline verifier returned zero protected mismatches and no files were changed during the F1 run. The current implementation/control-plane paths are therefore treated as the protected preimage for this owner-directed verification boundary. No files were reverted, reset, staged, or overwritten.

REVIEW_ID: f1-phase5.1-conformance
SUBJECT: phase-5.1-hydraulics-dirty-ownership-correction@65999B8FF9008157ADAADC57D85138AF610829F70E57C6E9B0269F98C72EE46C
VERDICT: APPROVE
REASON: Owner-directed execution-time baseline captured 27 status records and 21 protected dirty paths; validator, diff check, and symmetric protected verifier all pass with zero post-baseline protected drift.
