# Phase 8 — Generation and Hash Receipt

Status: PASS
Date: 2026-09-03
Worktree: `D:/IA/ace — копия` (branch `master`)
Change class: architecture artifacts (+ production/test lane documented in slice receipts)
Plan: `docs/architecture-migration/plans/phase-8-results-derived-projection.md` (SHA-256 `EC762434820E87EA92B9A37A4FD694DCABD81181F93C1B6EA035FFF5674F5C67`)
Amendment 1: `docs/architecture-migration/plans/phase-8-results-derived-projection.amendment-1-coldperioddays-canonicalization.md` (SHA-256 `17DFF9B3C1DDED6AC349DACA576D2B972A7124EACF07B9889B20AEE30732E72E`)

## Generation Inputs (SHA-256, at generation time)

- `maps/architecture-model.json`: `FC25B7AFE899EEF14128F91B7D361D06944F372F294E81CE186CF81AD6A17298`
- `maps/architecture-model.widget.schema.json`: unchanged from Phase 7.5 receipt (`574AA8A8C9B9989E1545EC02A65F8785C638F5854ADEB0FFECB594A8EA11F510`)
- `widget/generate-widget.mjs`, `widget/architecture-widget.mjs`, `widget/architecture-widget.template.html`, `widget/architecture-widget.css`: unchanged from Phase 7.5 receipt

## Outputs

- `architecture-widget.html`: `0601835D18A6464A580B24FCAD7396FCBBD340B032ABDCC614CD786B17B6E34C` (15,970,676 bytes)
- Two sequential generations byte-identical (`--check` PASS).

## Commands

```text
node docs/architecture-migration/widget/generate-widget.mjs
node docs/architecture-migration/widget/generate-widget.mjs --check
node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output <temp>/p8-model-v2.json
node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output <temp>/p8-runtime-v2.json
```

Runtime: Node v24.x on Windows 10 x64.

## Verification Results

- `model-v2`: PASS, 33 assertions / 21 mutations.
- `runtime-v2`: PASS (verdict PASS), 47 assertions / 20 mutations.
- `git diff --check`: clean.
