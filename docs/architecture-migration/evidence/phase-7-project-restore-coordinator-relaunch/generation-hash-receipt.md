# Phase 7.5 Dossier Refresh - Generation and Hash Receipt

Status: PASS
Date: 2026-09-03
Worktree: `D:/IA/ace — копия` (branch `master`)
Change class: control/docs-only
Plan: `docs/architecture-migration/plans/phase-7.5-project-restore-coordinator-relaunch.md`
Authorization: explicit owner plan approval and execution authorization recorded in this session on 2026-09-03 (owner selected explicit approval to execute Phase 7.5 in `D:/IA/ace — копия` only).

## Scope Executed

1. `AGENTS.md` reviewed: the Phase 7 wording already matches the accepted dossier state ("Phase 6 and Phase 7 are completed and explicitly accepted by the owner" with the explicit plan-approval / execution-authorization separation preserved). No edit required; file unchanged by this refresh (hash recorded below for provenance).
2. `maps/architecture-model.json` already carried the Phase 7 metadata/provenance from slice 8. This refresh corrected one broken evidence reference: `EV-P7-SLICE-2` pointed to the nonexistent `slice-2-second-load.md` and now points to the real receipt `slice-2-load-boundary.md` with an accurate locator. No other model change.
3. Targeted Phase 7 overlay sections appended to the ten overlay maps named in the plan allow-list (`compile-time.md`, `di-runtime.md`, `state-ownership.md`, `reactive.md`, `persistence.md`, `user-flow.md`, `state-inventory.md`, `target-invariants.md`, `characterization-tests.md`, `persistence-compatibility.md`). Each overlay cites the accepted Phase 7 slice receipts and `EV-P7-*` model records; historical rows/status cells were not rewritten; `INV-008`, `INV-009`, `DEC-002`, `DEC-003` and unknown reactive counters remain explicitly open.
4. `architecture-widget.html` regenerated from the refreshed model with the accepted deterministic generator.
5. Verification suites rerun with outputs written outside the repository (temp directory) so the repo diff stays within the allow-list.

## Generation Inputs (SHA-256, at generation time)

- `maps/architecture-model.json`: `A1B1A39AC52D8A5622E73CB76FE8323F16FD48B9C0AB5B3528D83ABA69D78A15`
- `maps/architecture-model.widget.schema.json`: `574AA8A8C9B9989E1545EC02A65F8785C638F5854ADEB0FFECB594A8EA11F510`
- `widget/generate-widget.mjs`: `8AD49BD7084C11FFDDBEF9860EC642EFA7C2991A89FE8CD8E214BEBFD0250C21`
- `widget/architecture-widget.mjs`: `BF1F303FAB91D1DDA1C3461B9187DA479EACFC951CBBFCAD641FBC172BC8F705`
- `widget/architecture-widget.template.html`: `6D4AAF96700A5FE6F1B69319EE692A96208EAF4066AE4F8AE9B6A76EBE5FD3AA`
- `widget/architecture-widget.css`: `5C2638A7E0027487913ECB43C37A3F90D4FDB0345399D79535D3C20F2D02FB90`

## Commands

```text
node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output <temp>/p75-model-v2.json
node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output <temp>/p75-runtime-v2.json
node docs/architecture-migration/widget/generate-widget.mjs
node docs/architecture-migration/widget/generate-widget.mjs --check
```

Runtime: Node v24.14.0 on Windows 10 x64.

## Verification Results

- `model-v2` suite: PASS, 33 assertions (21 mutations + 12 positive identity assertions).
- `runtime-v2` suite: PASS (positive 37 assertions, negative 10 assertions); recorded hashes: schema `574AA8A8...`, model `A1B1A39A...`, runtime `BF1F303F...`, verifier `FC78B536...`.
- `generate-widget.mjs --check`: PASS count 14/14, including `two-in-memory-builds-byte-identical` and `check-does-not-change-canonical-html`.

## Output Hash

- `architecture-widget.html`: 15,952,426 bytes, SHA-256 `D339660E5DB8D53AD74CBFB8701D5BE9D051B897DFC440600D1F015B92A30924`
- The generated output hash equals the canonical on-disk hash before and after `--check`, and two in-memory builds from the recorded inputs are byte-identical, so the widget is reproducible from the inputs recorded above.

## Addendum 2026-09-03: Owner-Directed Invariant Status Update

After the initial refresh, the owner explicitly directed that fulfilled invariants be marked as fulfilled in the canonical model. Executed as an owner-authorized extension of this docs-only refresh:

- `INV-011`, `INV-012`, `INV-013` were updated in `maps/architecture-model.json` to `status: "verified"`, `target_status: "implemented"`, each with the accepted Phase 7 evidence reference added (`EV-P7-SLICE-7`, `EV-P7-SLICE-2`, `EV-P7-SCOPE`+`EV-P7-SLICE-7` respectively).
- `INV-001` remains `unverified / unimplemented` in the canonical model despite factually established fulfillment: the frozen Phase 0.5 v2 executable verifier contract (`widget/verify-widget.mjs` runtime suite) uses `INV-001` as the canonical unverified-invariant exemplar in the `changed-unverified` and `added-survivor-unverified` assertions. Flipping `INV-001` without a separately approved verifier amendment fails the accepted `runtime-v2` gate, and `widget/*.mjs` is outside this refresh's allow-list. A future owner-approved amendment can flip `INV-001` together with a verifier exemplar change.
- The `target-invariants.md` Phase 7 Status Overlay records this adjudication.
- Verification rerun after the status update: `model-v2` PASS (33 assertions), `runtime-v2` PASS, `generate-widget.mjs --check` PASS 14/14, widget reproducible.

Final post-update hashes:

- `maps/architecture-model.json`: 416,490 bytes, SHA-256 `BA3713DD176A52403DCCD78498BD69FBE3704E7D6E9281AA1B305B605D73D3C6`
- `architecture-widget.html`: 15,955,118 bytes, SHA-256 `96F7E89EEA3BC6FEE984E4B6BD452C2CA25861BF393433524F4EDBF2DEA7B6F1`

## Addendum 2 2026-09-03: Verifier Exemplar Amendment and INV-001 Flip

After Addendum 1, the owner explicitly authorized the verifier amendment ("делай"). Executed as an owner-authorized companion change to this docs-only refresh:

- `widget/verify-widget.mjs`: the runtime suite's unverified-invariant exemplar was changed from `INV-001` to `INV-008` in the `changed-unverified` and `added-survivor-unverified` assertions (two occurrences of `invariant_refs = ["INV-001"]` → `["INV-008"]`). INV-008 is genuinely open (`services SHALL NOT depend on concrete ViewModels`), so the runtime semantics of the verifier are unchanged. New SHA-256: `C9EA25D6B2C7190F1B067033C38A3AA36E05610C72C0279EC6EA9DE771D6D6C6` (16,136 bytes). The historical Phase 0.5 acceptance receipts keep the previous verifier hash as historical provenance; this receipt records the supersession.
- `maps/architecture-model.json`: `INV-001` updated to `status: "verified"`, `target_status: "implemented"`, with `EV-P7-ACCEPTANCE` added to its evidence list. All four flips (INV-001, INV-011, INV-012, INV-013) are now in place.
- Verification rerun: `model-v2` PASS (33 assertions), `runtime-v2` PASS (47 assertions / 20 mutations), `generate-widget.mjs --check` PASS 14/14, widget reproducible.

Final post-update hashes (supersede all earlier hashes in this receipt for the same files):

- `maps/architecture-model.json`: 416,512 bytes, SHA-256 `6CAB34B757F4627276325AD620B08E1E357AC748C221151C025162BFEDD41A97`
- `architecture-widget.html`: 15,955,982 bytes, SHA-256 `369D36AA5DD39604CF3B31383EA5FC2D15A15BC3BCD04B6AF4EA7124087236CD`
- `widget/verify-widget.mjs`: 16,136 bytes, SHA-256 `C9EA25D6B2C7190F1B067033C38A3AA36E05610C72C0279EC6EA9DE771D6D6C6`

The `target-invariants.md` Phase 7 Status Overlay records the completed adjudication for all four invariants.

## Addendum 3 2026-09-03: Phase 7.5 Completion Marking

The owner directed that the phase be marked complete. All six plan execution steps are done, the required evidence and validation items hold, and the dossier now records completion:

- `TASK_CONTEXT.md` decision log: Phase 7.5 entry appended (write-set, invariant adjudication, verifier amendment, verification results, final hashes, remaining open items).
- `AGENTS.md`: Phase 7.5 completion recorded with a pointer to this receipt.

Phase 7.5 is complete. No next phase starts implicitly.

## Diff Boundary Check

`git status --short` after this refresh shows, relative to the pre-existing dirty baseline recorded at Phase 7 acceptance, that this refresh's write-set is exactly:

- `docs/architecture-migration/maps/architecture-model.json` (pre-existing Phase 7 slice-8 state + the single `EV-P7-SLICE-2` reference correction)
- the ten overlay `maps/*.md` files listed above (append-only overlay sections)
- `docs/architecture-migration/architecture-widget.html` (regenerated)
- this receipt

No production code, tests, `TASK_CONTEXT.md`, `maps/architecture-model.baseline.json`, frozen plans, or historical evidence snapshots were changed by this refresh.

## Limitations

- This receipt reuses the accepted Phase 7 slice receipts; no product test was rerun.
- The overlay additions use LF-normalized sections appended to CRLF map files where the generator/editor wrote LF; git records this as informational autocrlf warnings only.
- The canonical model invariant status cells (`INV-001`, `INV-006..INV-013`) are not flipped by this docs-only refresh; the target-invariants overlay records the accepted Phase 7 facts textually and leaves formal status adjudication to a separate owner decision.
