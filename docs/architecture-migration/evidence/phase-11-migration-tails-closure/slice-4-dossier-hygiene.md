# Slice 4 — Dossier/model hygiene and deterministic widget regeneration

Phase 11 (`phase-11-migration-tails-closure`). Write-set: architecture
artifacts + four dead-using removals (cosmetic production hygiene) + dated
`TASK_CONTEXT.md` entries. No behavior changes.

## Dated `state-inventory.md` overlay (history preserved, no row rewrites)

- The Phase 1 first-block rows `ST-001..ST-005` are marked **historical** —
  their "current canonical owner" cells name `ProjectStateService`, removed
  from production in Phase 9 (test-fixture copy only). Live ownership points
  to the Phase 4+ addendum rows and the Phase 10 writer inventory (8/8).
- `ST-003` correction: live owner is `IProjectDisplayModeState`
  (`ProjectDisplayModeState`, DI singleton) since Phase 6;
  `ResultsViewModel.IsOperatingMode` is read-through write-through
  (`ResultsViewModel.cs:304-312`).
- `ST-023` wire annotation (DEC-006): `ProjectData` no longer carries
  `CustomMaterials`/`CustomTemplates`; old `.smc` files carrying those JSON
  members keep loading (unknown members ignored; re-pinned test named).

## Model refresh

- `metadata.phase` → `phase-11-migration-tails-closure`; `source_basis`
  updated; `metadata.snapshot_sha` = SHA-256 of the model file immediately
  before the metadata write (`745DC19637A9B367…` — the field is
  self-referential, so the authoritative whole-file identity is the verifier
  receipt's `hashes.model`, recorded below).
- Evidence records: `EV-P11-LIMP81` (Slice 1), `EV-P11-DEC006` (Slice 2),
  `EV-P11-REGRESSION` (Slice 3), `EV-P11-HYGIENE` (this slice) — all resolve.

## Dead-using removal (compile-proven cosmetics)

Removed `using SnowMeltingCalculator.ViewModels.Hydraulics;` from
`src/Services/Hydraulics/{CircuitsValidator,CollectorTypeSelector,ICollectorTypeSelector}.cs`
(plan-named) and — same dead-using class, same directory — from
`ICircuitsValidator.cs` (interface members use only
`Models.Hydraulics` types). Gate: **0** such usings remain in
`src/Services/Hydraulics`.

Two remaining `using SnowMeltingCalculator.ViewModels` hits in
`src/Services/Results` (`HydraulicSummaryBuilder.cs`,
`ResultsPdfDataBuilder.cs`) are **legitimate**: the builders produce
Results-owned read-model records (`CollectorSpecification`,
`CollectorEquipmentItem`, `CollectorHydraulicSummaryCard`) that currently
live in the `ViewModels.Results` namespace — Phase 9 accepted outputs; the
namespace placement of those record types is recorded as audit-P1/P2 backlog
hygiene, out of scope here.

## Widget + verifier (plan-exact commands)

- `generate-widget.mjs` twice → byte-identical generations, 16,003,494
  bytes, SHA-256 `761D5E167F173FF74429C2E44CB3002D38D87B8A78C9DA2003AEF55CE0889EE8`.
- `generate-widget.mjs --check` → PASS, count 14 (canonical before = after =
  generated).
- `verify-widget.mjs --suite model-v2` → PASS (33 assertions / 21 mutations);
  `--suite runtime-v2` → verdict PASS (47 assertions / 20 mutations).
  Receipt: `phase-0.5-acceptance-v2.json` (final model hash recorded there).
- `git diff --check` → clean (CRLF notices only).
- Post-removal build: 0 warnings / 0 errors; targeted smoke (save-service +
  factory suites) 9/9.

**SLICE 4: PASS**
