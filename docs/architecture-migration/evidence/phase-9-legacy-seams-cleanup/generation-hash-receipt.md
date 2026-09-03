# Phase 9 generation hash receipt

Дата: 2026-09-03. Worktree: `D:/IA/ace — копия` (HEAD `3a077c7` + Phase 9 write-set).

## Verifier suites (post-slice-8 model)

```
node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output <tmp>/p9-model-v2.json
node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output <tmp>/p9-runtime-v2-final.json
```

- `model-v2`: PASS — 33 assertions / 21 mutations.
- `runtime-v2`: PASS — 47 assertions / 20 mutations.

## Widget regeneration

```
node docs/architecture-migration/widget/generate-widget.mjs          # writes architecture-widget.html
node docs/architecture-migration/widget/generate-widget.mjs --check  # PASS 14/14, rebuilds byte-identical
```

## Final hashes (SHA-256)

| Артефакт | Hash |
|---|---|
| `maps/architecture-model.json` | `fddf315226eb07da7a980ffdc2823e33e06746f583ad88223b8d4400c5529c34` |
| `architecture-widget.html` | `c2a74404e1ba35a03f6c7fe91fe23098d657ea5add1b891c51e441b05eb4fd97` |
| `widget/verify-widget.mjs` | `c9ea25d6b2c7190f1b067033c38a3aa36e05610c72c0279ec6ea9de771d6d6c6` (unchanged from Phase 7.5) |

## Owner gate resolved post-acceptance (superseded by the amendment)

The exemplar re-point was owner-authorized and executed on 2026-09-03 together
with a model-consistency fix (the slice-8 INV-008 status flip had silently
missed the `invariants` array). Superseding hashes and full detail:
`verifier-exemplar-amendment.md`. Final hashes:
model `EE5C8DD95F4F80D5F17720D877FDD37C1A42E80B4489467CED9C6794FDCAB9C6`,
widget `DA21FAB79778AD06474AB013CB58D2CEEF90535F59AED9C38539120073F023FA`,
verifier `2DB68012E1FC37DD67887B36612587D19F94BA0EF6EB5613E70C41B98626A8C5`.

## Model changes in this phase

- `INV-008` → `verified` / `implemented` (EV-P9-SLICE-5, EV-P9-SLICE-6).
- `ST-026`, `ST-027` → `covered` current states (EV-P9-SLICE-3, EV-P9-SLICE-4).
- New evidence records `EV-P9-SLICE-2..7`.
- Frozen plans, `architecture-model.baseline.json`, historical evidence
  snapshots — unchanged.
