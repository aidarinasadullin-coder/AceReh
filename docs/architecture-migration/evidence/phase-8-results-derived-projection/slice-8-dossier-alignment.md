# Slice 8 — dossier alignment

Класс: architecture evidence only. Дата: 2026-09-03.

## Scope executed

1. Ten overlay maps appended with a `Phase 8 Results-Derived-Projection Overlay`
   section each (`state-ownership`, `state-inventory`, `target-invariants`,
   `reactive`, `compile-time`, `di-runtime`, `persistence`,
   `persistence-compatibility`, `user-flow`, `characterization-tests`):
   historical rows/status cells not rewritten; `INV-008`, `INV-010`, unknown
   reactive counters and the import-removal baseline anomaly remain explicitly
   open/flagged.
2. `maps/architecture-model.json`: metadata phase/snapshot_sha/source_basis →
   Phase 8 (plan identity `EC762434820E87EA92B9A37A4FD694DCABD81181F93C1B6EA035FFF5674F5C67`);
   `INV-009` → status `verified`, target_status `implemented` with `EV-P8-*`
   evidence; `ST-003`, `ST-024`, `ST-025` current+baseline canonical blocks →
   covered/migrated-verified; `ST-026`, `ST-027` → partial with named residual
   phrases; evidence records `EV-P8-PLAN`, `EV-P8-SLICE-3..7` appended;
   limitations `LIM-P8-1` (shared CircuitRow/builder residual), `LIM-P8-2`
   (pre-existing import-removal baseline anomaly, owner decision required)
   appended.
3. `architecture-widget.html` regenerated with the deterministic generator and
   verified.
4. Amendment 1 doc (`phase-8-results-derived-projection.amendment-1-...md`,
   SHA-256 `17DFF9B3C1DDED6AC349DACA576D2B972A7124EACF07B9889B20AEE30732E72E`)
   recorded in the dossier chain.

## Verification (generation-hash receipt)

- `generate-widget.mjs --check`: PASS — deterministic, sha256 `0601835D18A6464A580B24FCAD7396FCBBD340B032ABDCC614CD786B17B6E34C`.
- `verify-widget.mjs --suite model-v2`: PASS, 33 assertions / 21 mutations.
- `verify-widget.mjs --suite runtime-v2`: PASS, 47 assertions / 20 mutations.
- `git diff --check`: clean (no whitespace errors; CRLF warnings only).
- Worker content review: each overlay statement maps to slice receipts
  `slice-1..slice-7` under `evidence/phase-8-results-derived-projection/`.

## Failure QA

Scope-creep probe: no claim of `INV-008`/`INV-010` closure, no legacy-alias
removal, no `CalculationContext` writer change, no Markdown/export feature work
anywhere in the dossier delta. Import-removal anomaly recorded as
`LIM-P8-2`/pre-existing, not silently absorbed.

## Статус

SLICE 8: PASS
