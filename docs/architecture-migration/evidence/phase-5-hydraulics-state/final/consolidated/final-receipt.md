# F4 Consolidated Final Verification Receipt

- Write-set: `phase-5-hydraulics-state`
- Frozen plan: `docs/architecture-migration/plans/phase-5-hydraulics-state.md`
- Plan SHA-256: `0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38`
- STATE.json stage: `executing` (unchanged, preserved byte-for-byte)
- Owner gates: planApproval `approved`, executionAuthorization `approved`, resultAcceptance `pending`

This receipt consolidates the three independent final-verification domains. Each
domain verdict is preserved exactly as recorded in its own receipt. The
consolidated verdict is APPROVE because all three domain receipts are valid and
return APPROVE.

## Domain verdicts (preserved exactly)

### F1 Conformance / Scope / Provenance: APPROVE

Receipt: `docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f1/conformance-scope-provenance.md`

F1's receipt records APPROVE after its conformance, scope, provenance, and protected-drift checks.

### F2 Architecture / Code Quality: APPROVE

Receipt: `docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f2/architecture-quality.md`

F2's receipt records APPROVE after its architecture and code-quality audit, including the corrected User-only supply-input mirroring.

### F3 Executable QA / User Risk: APPROVE

Receipt: `docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f3/executable-qa.md`

F3's receipt records APPROVE after the fresh complete UI run, including S1-S8, the failure branch, strict Results assertions, and unchanged executable SHA.

## Reused and rerun evidence

F1 reused the task-1 protected baseline chain, task-12 reconciliation artifacts,
task-6 and task-9 owner-adjudicated notes, task-14 widget/model receipts, and
the ui-qa observations. It reran the plan SHA recompute, the H0
`validate --check-plan` gate, the full protected-drift chain against HEAD, the
evidence-path existence sweep, the DTO/wire/literal greps, and the commit sweep.

F2 reused the correction history for `f65e067`, `20e4285`, and `b9866d3` and ran
an independent source audit across the A-G checks, a focused Debug run (51
passed / 0 failed / 0 skipped), and a Debug production build (0 warnings / 0
errors).

F3 ran a fresh complete UI pass (S1 through S8 plus the F failure branch),
captured nine screenshots, confirmed the executable SHA was unchanged before and
after every launch, and kept the strict Results and S4f downstream power
assertions. The Select-Sidebar harness fix made both navigation targets
deterministic.

## Residual risks (inherited, not overriding domain verdicts)

These risks are carried from the domain receipts and do not change any APPROVE
verdict.

1. R1. No standalone `phase-5(task-11)` boundary commit; the guard-suite file
   landed inside `47faf28`. Guard evidence is complete (8/8) and H11 ordering
   was respected functionally.
2. R2. Commit-order deviation around tasks 9/10; the final tree satisfies all
   acceptance criteria.
3. R3. `CircuitsViewModel` diff magnitude (+287/-148); structural adapter
   conformance is verified, breadth assessment belongs to F2 which passed.
4. R4. `ICalculationStateService.cs` received no change (plan allowed
   doc-comments-only); surface intact.
5. R5. Double canonical `Restore(ProjectLoad)` in the orchestrator; matches the
   in-scope wording and passes lifecycle characterization.
6. R6. `STATE.json` control-plane fields remain at pre-execution values by
   design; transitions belong to owner gates after F1-F4.
7. F2 documentation debt: new public types have less XML documentation than the
   Thermal precedent. A quality debt, not a correctness blocker. Pre-existing
   service-to-VM coupling and dead code remain outside this audit.
8. F3 harness note: the prior blocker was a UIA/WPF navigation timing defect,
   not an executable-path or stale-process mismatch. Fixed in the harness, not
   in production code.

## Result acceptance pending

This consolidated receipt records the final verification gate only. Owner result
acceptance is a separate, later action and is not performed here. `STATE.json`
stays at stage `executing` with `resultAcceptance` pending and is preserved
byte-for-byte. No owner gate, plan checkbox, or control-plane field was changed
by writing this receipt.

REVIEW_ID: f4-phase5-consolidated
SUBJECT: phase-5-hydraulics-state@0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38
RECEIPT: docs/architecture-migration/evidence/phase-5-hydraulics-state/final/consolidated/final-receipt.md
VERDICT: APPROVE
REASON: All three domain receipts (F1 conformance/scope/provenance, F2 architecture/code quality, F3 executable QA/user risk) are valid and each returns APPROVE; the consolidated verdict is APPROVE. F1 fresh protected-drift chain at HEAD 84921ce gives mismatch=0 over the 39-path allow-list, H0 validate --check-plan exits 0, and exactly one dirty path (pre-existing STATE.json) is byte-stable. F2 independent A-G audit plus 51/51 focused checks pass after the corrected User-only MirrorSupplyInputs. F3 fresh complete UI run is green (S1-S8 + F, 9 screenshots, unchanged executable SHA). Inherited residual risks R1-R6 and F2/F3 notes are listed and do not override any domain verdict.
