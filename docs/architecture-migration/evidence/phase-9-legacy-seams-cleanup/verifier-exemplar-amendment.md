# Verifier exemplar amendment + model consistency fix (Phase 9, post-acceptance)

Дата: 2026-09-03. Класс: architecture artifacts. Прецедент: Phase 7.5
(`INV-001` → `INV-008` amendment, owner-authorized).

## Owner authorization

In-session, 2026-09-03, the owner explicitly directed (exact statement):
`переткнуть exemplar в verify-widget.mjs с INV-008 (теперь verified) на
INV-010 по прецеденту фазы 7.5` — авторизация разовой правки exemplar'а.

## What changed

1. **Exemplar re-point (owner-authorized)**: `widget/verify-widget.mjs`
   lines 33-34 — synthetic unverified-invariant exemplar `"INV-008"` →
   `"INV-010"` (2 occurrences). Runtime semantics unchanged; INV-010 verified
   genuinely open (`status: unverified` in the model) at the time of the
   re-point, satisfying the Phase 7.5 exemplar precondition.
2. **DEFECT FIX (recorded honestly)**: the slice-8 model update script wrote
   the INV-008 flip while iterating `records`, but invariant records live in
   the separate top-level `invariants` array — the flip silently missed, while
   the slice-8 receipt/TASK_CONTEXT/F-receipts/owner acceptance declared
   `INV-008 → verified`. Post-acceptance model inspection (triggered by this
   amendment) exposed the mismatch. Corrected under the same owner direction:
   `invariants[INV-008]`: `status unverified → verified`, `evidence`
   `EV-P9-SLICE-5, EV-P9-SLICE-6, EV-CT, EV-DR`, `target_status implemented` —
   exactly the slice-8 declared state. EV-P9 evidence records and the
   ST-026/ST-027 covered states were verified as correctly landed (they target
   `evidence`/`records` collections the script did reach).

This receipt supersedes the model-hash claims in `slice-8-dossier-alignment.md`,
`generation-hash-receipt.md` (pre-amendment section), `final-f1..f4` and the
owner-acceptance receipt where they cite model `fddf3152…` / widget
`c2a74404…`; the execution receipts themselves are unaffected.

## Verification

- `model-v2`: **PASS** — 33 assertions / 21 mutations.
- `runtime-v2`: **PASS** — 47 assertions / 20 mutations.
- `generate-widget.mjs` regenerated the widget from the corrected model;
  `--check` **PASS 14/14** (two builds byte-identical).

## Final hashes (SHA-256, supersede all earlier Phase 9 hash records)

| Артефакт | Hash |
|---|---|
| `maps/architecture-model.json` | `EE5C8DD95F4F80D5F17720D877FDD37C1A42E80B4489467CED9C6794FDCAB9C6` |
| `architecture-widget.html` | `DA21FAB79778AD06474AB013CB58D2CEEF90535F59AED9C38539120073F023FA` |
| `widget/verify-widget.mjs` | `2DB68012E1FC37DD67887B36612587D19F94BA0EF6EB5613E70C41B98626A8C5` |
