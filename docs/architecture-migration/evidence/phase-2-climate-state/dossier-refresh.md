# Phase 2 Task 12 architecture dossier refresh

Status: PASS

Date: 2026-08-12

Scope: Phase 2 Task 12 documentation/model/widget/evidence/control artifacts only. Final Verification Wave F1-F4 was not started.

Correction note: the initial Task 12 closeout verified the generator with `--check` but did not physically rewrite `docs/architecture-migration/architecture-widget.html`. This correction ran `node docs/architecture-migration/widget/generate-widget.mjs` without `--check`, regenerated the static HTML on disk from the current `docs/architecture-migration/maps/architecture-model.json`, and then reran the canonical model/runtime/check gates.

## Changed dossier artifacts

- `docs/architecture-migration/maps/compile-time.md`
- `docs/architecture-migration/maps/di-runtime.md`
- `docs/architecture-migration/maps/state-ownership.md`
- `docs/architecture-migration/maps/reactive.md`
- `docs/architecture-migration/maps/persistence.md`
- `docs/architecture-migration/maps/user-flow.md`
- `docs/architecture-migration/maps/state-inventory.md`
- `docs/architecture-migration/maps/characterization-tests.md`
- `docs/architecture-migration/maps/persistence-compatibility.md`
- `docs/architecture-migration/maps/target-invariants.md`
- `docs/architecture-migration/maps/architecture-model.json`
- `docs/architecture-migration/architecture-widget.html`
- `docs/architecture-migration/evidence/phase-2-climate-state/model-v2-recheck.json`
- `docs/architecture-migration/evidence/phase-2-climate-state/runtime-v2-recheck.json`
- `docs/architecture-migration/evidence/phase-2-climate-state/dossier-refresh.md`
- `docs/architecture-migration/plans/phase-2-climate-state.md`
- `docs/architecture-migration/TASK_CONTEXT.md`

No production source under `src/` and no test source under `tests/` was intentionally modified by Task 12. Existing unrelated dirty worktree entries were not staged, reverted, reset, checked out, cleaned, or normalized.

## Source and evidence basis

Task 12 refreshed the Climate architecture dossier against current source and accepted Phase 2 evidence:

- `ProjectSession.ClimateState` / `ProjectSessionClimateState` is the sole writable canonical Climate owner for project Climate values.
- `ClimateViewModel` is a WPF adapter/mirror and routes user mutations through `IProjectSessionClimateState`.
- `ClimateData` / `IClimateData` is a compatibility projection boundary updated through `ApplyProjection`.
- `CalculationContext` remains the downstream compatibility projection/invalidation seam.
- Load/reset/restore use non-user `ClimateMutationOrigin` paths through canonical state.
- `ResultsViewModel.SaveCurrentProject()` saves from `_projectSession.ClimateState.Snapshot`; existing `.smc` Climate DTO fields and version behavior are unchanged.
- Downstream invalidation follows one authoritative path: `ProjectSessionClimateState.CompleteMutation()` -> `ClimateData.ApplyProjection()` -> `CalculationContext.UpdateClimate()` -> downstream consumers.

Evidence receipts used:

- `baseline.md`
- `writer-guard.md`
- `multiplicity-characterization.md`
- `climate-state-api.md`
- `climate-data-projection.md`
- `climate-viewmodel-adapter.md`
- `restore-reset-routing.md`
- `persistence-results.md`
- `downstream-invalidation.md`
- `di-guards.md`
- `affected-gates.md`

Task 11 accepted gate counts from `affected-gates.md`:

- Targeted Release matrix after fix: total 330 / executed 329 / passed 329 / failed 0; documented existing missing-fixture skip remains.
- Full Release rerun after fix: total 1616 / executed 1613 / passed 1613 / failed 0.

## Hashes

- `docs/architecture-migration/maps/architecture-model.json` SHA-256: `BED5C535731D6036970664E9E6533C70617C250B533F02FD4C0BCEDEAF0737CC`
- `docs/architecture-migration/architecture-widget.html` old pre-correction SHA-256: `CADE742CD2136AF808A475EA40F743C6F5AEF9E3CF8BB9043C9FFBC5CA7D58A3`
- `docs/architecture-migration/architecture-widget.html` regenerated SHA-256: `A8B12B29D931AB4555F2F20F6FA0036702CB08E48BBBC587A4188FB03E840549`
- `docs/architecture-migration/maps/architecture-model.widget.schema.json` SHA-256: `574AA8A8C9B9989E1545EC02A65F8785C638F5854ADEB0FFECB594A8EA11F510`
- `docs/architecture-migration/evidence/phase-2-climate-state/model-v2-recheck.json` SHA-256: `76B056963AE2B790AC019C08803CEA1EC097A10890B9E5BF54781C602F6C0B3B`
- `docs/architecture-migration/evidence/phase-2-climate-state/runtime-v2-recheck.json` SHA-256: `89806CEDAF79FFB0C9E8337358300F99337FBD6A91CE704EB4A2FF2DA4F2E715`

## Canonical verification commands

### generate-widget write

```powershell
node docs/architecture-migration/widget/generate-widget.mjs
```

Result: PASS, exit `0`.

Observed summary: `generated D:\IA\ace v.2\docs\architecture-migration\architecture-widget.html`, bytes `15113378`. Immediate post-generation SHA-256 was `A8B12B29D931AB4555F2F20F6FA0036702CB08E48BBBC587A4188FB03E840549`, replacing old canonical SHA-256 `CADE742CD2136AF808A475EA40F743C6F5AEF9E3CF8BB9043C9FFBC5CA7D58A3`.

### model-v2

```powershell
node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output docs/architecture-migration/evidence/phase-2-climate-state/model-v2-recheck.json
```

Result: PASS, exit `0`.

Receipt: `docs/architecture-migration/evidence/phase-2-climate-state/model-v2-recheck.json`.

Observed summary: `status=PASS`, assertions `33`, mutations `21`.

### runtime-v2

```powershell
node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output docs/architecture-migration/evidence/phase-2-climate-state/runtime-v2-recheck.json
```

Result: PASS, exit `0`.

Receipt: `docs/architecture-migration/evidence/phase-2-climate-state/runtime-v2-recheck.json`.

Observed summary: verdict `PASS`, totals assertions `47`, mutations `20`, model hash `bed5c535731d6036970664e9e6533c70617c250b533f02fd4c0bcedeaf0737cc`.

### generate-widget check

```powershell
node docs/architecture-migration/widget/generate-widget.mjs --check
```

Result: PASS, exit `0`.

Observed summary: 14 PASS checks, canonical SHA-256 before/after `a8b12b29d931ab4555f2f20f6fa0036702cb08e48bbbc587a4188fb03e840549`; generated SHA-256 `a8b12b29d931ab4555f2f20f6fa0036702cb08e48bbbc587a4188fb03e840549`; `--check` did not change the regenerated canonical HTML.

## Verdict

Task 12 acceptance is fulfilled for the architecture dossier refresh after the correction regenerated `docs/architecture-migration/architecture-widget.html` on disk from `docs/architecture-migration/maps/architecture-model.json`. Climate records `ST-006`, `ST-007`, `INV-002`, `CF-005`, `RE-003`, `PP-006`, and `PP-013..PP-020` now point to current source/evidence and distinguish implemented ClimateState facts from future target slices. Final Verification Wave F1-F4 remains not started.
