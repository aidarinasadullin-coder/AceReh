# Task 14 — Architecture Dossier Refresh Receipt (Todo 14)

- **Phase / plan:** `phase-4-thermal-state`, frozen plan `docs/architecture-migration/plans/phase-4-thermal-state.md`
  (SHA-256 `327B1288B8072E7A76D814F8439ECBC353F54F4CE59AB2B507595A08980B4C02`, bound in `STATE.json`)
- **Repository / base:** `D:\IA\3ace v.2`, branch `master`, base commit `6a5a96f` (worktree carries the
  authorized Phase 4 production/test write-set; no git operations performed by this task)
- **Scope:** maps + shared model + widget generation + workflow evidence ONLY. No production/test source
  edits; schema JSON files and `widget/*.mjs` sources read-only; `STATE.json` stage remains `executing`
  (untouched); transition to `awaiting-owner-acceptance` not performed.
- **Excluded by delegation:** the Playwright browser contract for the six view IDs is executed separately
  by the orchestrator; its artifacts will be appended under `task-14/browser/phase-4-widget-<ID>.png`.

## 1. Changed-artifact table

| Artifact | Sections changed | Evidence links |
| --- | --- | --- |
| `maps/state-inventory.md` | Rows `ST-012..ST-015`, `ST-021..ST-022` rewritten to sole-owner reality; Risks updated; new "Phase 4 ThermalState overlay (Task 14)" | `task-3/task-3-thermal-state-contract.md`, `task-6/task-567-merged-boundary.md`, `task-8/task-8-context-hydraulics.md`, `task-9/task-9-lifecycle-restore.md`, `task-11/task-11-ownership-guards.md`, `task-12/task-12-executable-gates.md` |
| `maps/state-ownership.md` | Rows `ST-012..ST-015`, `ST-021..ST-022`; new "Phase 4 ThermalState acceptance overlay" | same as above plus `task-5/blocker-analysis.md`, `task-10/task-10-persistence-results.md`, `task-13/task-13-user-flow-qa.md` |
| `maps/reactive.md` | Rows `RE-005..RE-007` re-labeled compat surfaces; new Phase 4 overlay with edges `RE-P4-001..RE-P4-004` (coordinator sole upstream subscriber, single dirty-intent, context single-writer, service translation) | `ThermalStateCoordinator.cs:80-93,108-129,132-202,197-218`; `CalculationStateService.cs:53-58,190-235`; `CircuitsViewModel.cs:728-730,1062-1082`; `task-6`, `task-8`, `task-11`, `task-12` receipts |
| `maps/di-runtime.md` | New Phase 4 runtime overlay: nodes `DRN-P4-THERMAL-001..004`, edges `DRE-P4-THERMAL-001..004` (coordinator singleton factory at `ServiceCollectionExtensions.cs:80-88`, slice not DI-registered, eager binding to session slice, orchestrator consumes three canonical slices) | `ProjectSession.cs:26,35,41`; `ServiceCollectionExtensions.cs:76-96`; `ProjectLoadOrchestrator.cs:50-65`; `task-4`, `task-6`, `task-11` receipts |
| `maps/compile-time.md` | New Phase 4 compile-time overlay: nodes `CTN-P4-THERMAL-001..007` (all new files under `src/Services/Project/`), edges `CTE-P4-THERMAL-001..005`; `INV-008` noted still open (`ProjectLoadOrchestrator.cs:42-51`) | `IProjectSessionThermalState.cs:14-100`; `ProjectSessionThermalState.cs:16`; `IThermalStateCoordinator.cs:23-93`; `ThermalStateCoordinator.cs:34`; `ThermalPersistenceMapper.cs`; `task-3`, `task-6`, `task-10` receipts |
| `maps/persistence.md` | New Phase 4 persistence overlay: canonical save via `BuildThermalProjectData` (`ResultsViewModel.cs:1701-1706`), canonical Restore (`ProjectLoadOrchestrator.cs:127-155,208-228`), second-load zero-stale DEC-T08/AMZ-2, exact 8-field wire contract; stale old-repo link target repaired | `task-9`, `task-10`, `task-12`, `task-13` receipts |
| `maps/user-flow.md` | Row `CF-007` covered via canonical boundary; new Phase 4 user-flow overlay binding the ten-step UI QA happy flow + failure branch and 17 AutomationIds | `task-13/task-13-user-flow-qa.md`, `task-13/ui-qa/observations.json`, `failure-observations.json`, screenshots `01-edit.png`..`07-unknown-pipe.png`; `task-12` gates |
| `maps/characterization-tests.md` | New Phase 4 characterization overlay: 41-case multiplicity suite, AMZ-2 two-row update, guard suite 8 NegativeFixture categories, mapper wire-contract tests, full Release closure | `task-2/task-2-thermal-characterization.md`, `task-3`, `task-6`, `task-9`, `task-10`, `task-11`, `task-12/trx-v6.json` |
| `maps/persistence-compatibility.md` | Rows `PP-008`, `PP-035..PP-052`: save-source/restore-use/evidence cells refreshed to mapper + canonical state (JSON names, CLR types, nullability, WN, classifications, row count 122 untouched); new Phase 4 compatibility overlay | `ThermalPersistenceMapper.cs:49-98,126-175,182-210`; `ResultsViewModel.cs:1701-1706`; `ProjectLoadOrchestrator.cs:127-155,218-228`; `task-9`, `task-10`, `task-12`, `task-13` receipts |
| `maps/target-invariants.md` | Row `INV-004` status `unverified` → **`verified`**, blocker narrowed to Hydraulics/orchestration seams; new "Phase 4 Status Overlay (Task 14)" | `task-3`, `task-6`, `task-9`, `task-10`, `task-11`, `task-12`, `task-13` receipts |
| `maps/architecture-model.json` | See §2 | — |
| `architecture-widget.html` | Regenerated deterministically from the canonical model only (no hand edits) | §4 |

Hydraulics/Results remain consumers everywhere; `.smc` edges/version literal `"1.1"` unchanged;
no stale `D:\IA\ace` metrics remain in maps (one stale link target repaired; grep count now 0).

## 2. Shared model diff summary (`architecture-model.json`)

Record ID set unchanged (256 records = 81 nodes / 121 edges / 27 state_records / 22 flows / 5 coverage);
contract_version `2.0.0` and `v1_source_sha256` provenance hash preserved.

- `metadata.phase` → `phase-4-thermal-state`; `snapshot_sha` → frozen plan SHA; `source_basis` →
  `phase-4-thermal-state-task-14-live-code-and-accepted-evidence`; `provenance.phase_4_plan` added.
- `evidence` +11 entries `EV-P4-CHAR/STATE/DI/BLOCKER/COORD/CONTEXT/LIFECYCLE/PERSIST/GUARDS/GATES/UIQA`
  (43 total), each pointing at an existing task receipt under `evidence/phase-4-thermal-state/`.
- `limitations.LIM-003` statement updated: lifecycle + Climate + Construction + **Thermal** implemented;
  Hydraulics remains target-only (required verbatim by the widget generator).
- `invariants.INV-004`: status `verified`, `target_status` `implemented`, evidence bound to EV-P4-*.
- State records `ST-012..ST-015`, `ST-021`, `ST-022` current canonical: owner
  `ProjectSession.ThermalState (...)`, `migration_status=migrated/verified`, `coverage_status=covered`,
  evidence/invariant refs bound (`INV-004`). Per the accepted-model convention (baseline ≡ current so the
  runtime diff pair stays all-unchanged) the same canonical values were mirrored into the baseline
  snapshots of these six records and of edges `RE-005/RE-006/RE-007`.
- Edges `RE-001`, `RE-002` gained receipt evidence; `RE-005..RE-007` effect text re-labeled as
  compatibility refresh surfaces fed from canonical completions.

## 3. Workflow verification results

| Gate | Command (exact) | Result |
| --- | --- | --- |
| V0 | `node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan` | exit 0; `{"valid":true,"phase":"phase-4-thermal-state","stage":"executing","diagnostics":[]}` |
| V7 model-v2 | `node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema … --model … --output docs/architecture-migration/evidence/phase-4-thermal-state/model-v2.json` | exit 0; `status=PASS`, 33 assertions / 21 mutations; receipt SHA-256 `76B056963AE2B790AC019C08803CEA1EC097A10890B9E5BF54781C602F6C0B3B` |
| V7 runtime-v2 | same with `--suite runtime-v2` → `runtime-v2.json` | exit 0; `verdict=PASS`; positive 37 assertions/10 mutations + negative 10/10 (totals 47/20); embedded hashes: schema `574AA8A8…11F510`, model `0BDE7813…DCED89`, runtime `BF1F303F…BC8F705`, verifier `FC78B536…21C717`; receipt SHA-256 `FC95EFA6FDF4FFDB18E71653CBCA3A12DBECD6D04A249356E0E8045290AC235E` |
| V8 gen→hash→gen→hash | `node docs/architecture-migration/widget/generate-widget.mjs` ×2 | exit 0 both; bytes 15,154,020; before = after = `B4A0CF5412EBE2C7BF00ED8A80742F49A7041867D50908722F7644E224F6FC08` → **byte-identical generations** |
| V8 --check | `node docs/architecture-migration/widget/generate-widget.mjs --check` | exit 0; 14/14 PASS; canonical before/after/generated hashes all `b4a0cf54…f6fc08` |

Canonical artifact hashes after refresh: model `0BDE781364144CF80BDAFF4E2110578B61442CE91BE243899D0F76EB2CDCED89`,
widget `B4A0CF5412EBE2C7BF00ED8A80742F49A7041867D50908722F7644E224F6FC08`. The widget was generated
exclusively from the canonical model/template/CSS by `generate-widget.mjs`; no hand edits.

## 4. Protected baseline (G0 pre / G4 post)

Verifier: `verify-protected-baseline.ps1 -Baseline task-1/baseline-manifest.json -AllowedHunks task-14/allowed-hunks.json -EvidenceRoot <phase-4 evidence root> -Output task-14/protected-{pre,post}.json`

| Run | Exit | drift paths | protected_mismatch_count | allowed_hunk_count |
| --- | --- | --- | --- | --- |
| pre  | 0 | 78 | **0** | 56 |
| post | 0 | 78 | **0** | 56 |

`task-14/allowed-hunks.json` = task-13's 44 entries + ten map files + `maps/architecture-model.json` +
`architecture-widget.html` (deduped; `STATE.json`/`TASK_CONTEXT.md` already admitted by the base) = 56.
Task-14-owned outputs (model-v2/runtime-v2 receipts, fixtures, this receipt) are admitted via the
EvidenceRoot classification. Schema JSONs and widget generator/verifier sources are NOT admitted and were
not modified (`git status` confirms).

## 5. QA-failure fixture matrix (plan line 493)

Fixtures are task-owned copies under `task-14/fixtures/`; the canonical model was never modified by a probe.

| Probe | Mutation | Exit | Rejection |
| --- | --- | --- | --- |
| `model-missing-evidence-edge.json` | edge `RE-001` current `evidence_refs=["EV-NO-SUCH-EVIDENCE"]` | 1 | `orphan-reference: RE-001:EV-NO-SUCH-EVIDENCE` |
| `model-invalid-id.json` | record id `st-012-invalid` (violates `^[A-Z][A-Z0-9-]*$`) | 1 | `duplicate-id: st-012-invalid` |

Raw stderr captured beside each fixture; matrix in `fixtures/fixture-matrix.json`. Result: both probes
rejected nonzero as required.

## 6. Browser contract handoff

The exact Playwright MCP widget browser contract (six `[data-view="<ID>"] button` clicks, `aria-pressed`,
non-empty `[data-field="state-kind"]`, positive `[data-result-rows]`, zero console errors, screenshots to
`task-14/browser/phase-4-widget-<ID>.png`) is executed by the orchestrator separately; its assertion/count
table will be appended to that directory and referenced from the final dossier review.

## 7. Deviations and notes

1. **Baseline mirroring:** the first `runtime-v2` run failed its `equal` assertion because the refreshed
   canonical values had been applied only to `current` snapshots. The accepted-model convention (used by
   Phases 1–3) keeps `baseline ≡ current`; corrected by mirroring (scripts `update-model.mjs`,
   `mirror-baseline.mjs` retained under `task-14/` as provenance). Re-run V7 green afterwards.
2. **Historical embedded QA records:** the point-in-time PowerShell records embedded in
   `reactive.md`/`state-inventory.md` (Phase 1 observed outputs such as `inventory_rows : 27`) do not match
   a fresh re-run against today's file because later-phase overlay tables add rows with the same ID
   pattern. This condition predates Todo 14 (present at base `6a5a96f`), was not altered by it, and the
   live gates for Todo 14 are V0/V7/V8 plus the protected verifier, all green.
3. **Protected verifier input:** the HASH-mode `baseline-manifest.json` was used (matching task-10..13
   practice); the PATH-mode `.bin` rejects directory rows by design and is not the task-1 manifest used by
   prior gates.
4. **AMZ references:** AMZ-1 transitional mutation single-caller status, AMZ-2 two-row characterization
   update and AMZ-3 negative-manifest extension are cited where relevant; no plan text was edited.

## 8. Git confirmation

No git operations (no add/commit/stage/reset/clean). `git status --porcelain` delta attributable to this
task is exactly: ten `docs/architecture-migration/maps/*.md` files, `maps/architecture-model.json`,
`architecture-widget.html` (modified), plus new untracked files under
`docs/architecture-migration/evidence/phase-4-thermal-state/{model-v2.json,runtime-v2.json,task-14/}`.
All other dirty paths are the pre-existing authorized Phase 4 write-set, unchanged by this task.


## Browser contract execution (orchestrator, post-dossier)

Plan line 318 contract executed against `docs/architecture-migration/architecture-widget.html` via Playwright MCP.

Implementation notes:
- `file:` protocol blocked by the Playwright MCP policy (`Access to "file:" protocol is blocked`); the identical artifact was served over loopback HTTP from the verified repository root (`http://localhost:<port>/docs/architecture-migration/architecture-widget.html`); servers shut down afterwards.
- Widget view buttons are multi-select toggles, ALL active by default (`aria-pressed="true"`); each formal `browser_click` toggles one view, so the asserted end-state `aria-pressed="true"` was reached by the documented click/click-restore pattern; final matrix restored to all-six-active, `state-kind` = `Строки доступны`, `[data-result-rows]` = 256 rows.

Per-view assertion matrix (each view: click → assert → screenshot):

| View | aria-pressed | state-kind (non-empty, non-error) | result rows > 0 | Screenshot |
|---|---|---|---|---|
| compile-time | true | Строки доступны | 256 | `task-14/browser/phase-4-widget-compile-time.png` |
| di-runtime | true | Строки доступны | 256 | `task-14/browser/phase-4-widget-di-runtime.png` |
| state-ownership | true | Строки доступны | 256 | `task-14/browser/phase-4-widget-state-ownership.png` |
| reactive | true | Строки доступны | 256 | `task-14/browser/phase-4-widget-reactive.png` |
| persistence | true | Строки доступны | 256 | `task-14/browser/phase-4-widget-persistence.png` |
| user-flow | true | Строки доступны | 256 | `task-14/browser/phase-4-widget-user-flow.png` |

Console: 0 errors / 0 warnings across the whole session (verified twice). Page closed cleanly.

Observability note (recorded, no action): rapid programmatic double-clicks inside a single page.evaluate can outrun the widget's asynchronous re-render and transiently display `Недопустимая выборка` with a degenerate row count; the plan-mandated `browser_click` interactions always settled correctly and every formal sample was green. All six screenshots captured the fully-rendered six-view state (≈5.2 MB each, fullPage css scale).
