# Phase 6 Consolidated Receipt — Tasks 1-7 Evidence Assembly

Date: 2026-08-26
Receipt type: phase-6-consolidated-receipt (Task 8 bookkeeping/evidence lane)
Canonical plan: `docs/architecture-migration/plans/phase-6-project-snapshot-save-boundary.md`
Canonical plan SHA-256: `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92`
Operational mirror: `.omo/plans/phase-6-project-snapshot-save-boundary.md` (execution ledger only, NOT a second authority)

## Plan identity and authority

- Canonical plan is the active authority. Read-only `Get-FileHash -Algorithm SHA256` on 2026-08-26 returned exactly `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92`, matching the frozen SHA. The canonical plan bytes are 29455 and its Tasks 1-7 checkboxes remain unchecked as the immutable approved baseline.
- The `.omo` mirror is an operational execution ledger. It now carries Tasks 1-7 checked to record already-executed Tasks 1-7; its current SHA-256 is `69E7CE15D5D2EFDE03AAC81456D2D3100F064D45BE89DA9D5F4A433F073D6F1A` (diverges by design, not a frozen-mirror contradiction). No canonical plan byte, `STATE.json`, or workflow file was edited by this Task 8 lane.
- `docs/architecture-migration/STATE.json` is absent (`Test-Path` = False). `docs/architecture-migration/archive/STATE.json` exists as provenance-only. No canonical state import or `validate-state.mjs` run was performed because it is not authorized.

## Task 8 lane write-set (this bookkeeping task only)

This Task 8 lane changed exactly three artifact classes and nothing else:
- One dated append-only entry in `docs/architecture-migration/TASK_CONTEXT.md` (this receipt's provenance).
- This consolidated receipt: `docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/phase-6-consolidated-receipt.md`.
- Append-only entries in `.omo/notepads/phase-6-project-snapshot-save-boundary/learnings.md` and `decisions.md`.

No production code, tests, fixtures, maps, model, widget, frozen canonical plan, `.omo` plan checkbox, `STATE.json`, archive STATE, workflow scripts, or unrelated dirty paths were changed by this lane.

### Pre-existing unrelated dirty paths excluded from the Task 8 write-set

The current working tree contains pre-existing unrelated dirty paths that are protected baseline and were NOT attributed to Task 8 (nor to Phase 6 production). They are excluded from the Task 8 write-set and were not modified by this lane:
- `.opencode/commands/architecture-approve.md`, `architecture-draft.md`, `architecture-plan.md`, `architecture-resume.md`, `architecture-start.md`
- `docs/architecture-migration/AGENTS.md`
- `docs/architecture-migration/STATE.json` (tracked deletion already present), `docs/architecture-migration/workflow/validate-state.mjs` (tracked deletion), `docs/architecture-migration/workflow/validate-state.test.mjs` (tracked deletion)
- `src/ViewModels/Hydraulics/CircuitsViewModel.cs`
- pre-existing unrelated tests under `tests/SnowMeltingCalculator.Tests/Construction`, `IntegrationTests/Hydraulics`, `Services/Navigation`, `Services/Project` (except the Phase 6 save-boundary files), `ViewModels/Hydraulics`, `ViewModels/MainViewModelTests.cs`, `ViewModels/ResetOrchestrationTests.cs`, `ViewModels/ResultsViewModelTestHelpers.cs`
- `docs/architecture-migration/archive/STATE.json`, `docs/architecture-migration/evidence/phase-5.1-hydraulics-dirty-ownership-correction/**`, `docs/architecture-migration/plans/phase-5.1-hydraulics-dirty-ownership-correction.draft.md`, `docs/architecture-migration/plans/phase-5.1-hydraulics-dirty-ownership-correction.md`

The Phase 6 production/test/map/widget paths present in the dirty tree (e.g. `src/Services/Project/*`, `src/ViewModels/Results/ResultsViewModel.cs`, `src/Configuration/ServiceCollectionExtensions.cs`, the six maps, `architecture-model.json`, `architecture-widget.html`, and the Phase 6 evidence directory) are the result of Tasks 1-7 execution and are outside the Task 8 bookkeeping write-set; they were neither created nor modified by this lane.

## Exact write-set (Phase 6 Tasks 1-7)

Production:
- `src/Services/Project/ProjectSnapshot.cs`
- `src/Services/Project/IProjectSnapshotFactory.cs`, `ProjectSnapshotFactory.cs`
- `src/Services/Project/ProjectPersistenceMapper.cs`
- `src/Services/Project/IProjectSaveService.cs`, `ProjectSaveService.cs`
- `src/Services/Project/ProjectSaveDates.cs`
- `src/Services/Project/IProjectSnapshotPersistenceInputs.cs`, `ProjectSnapshotPersistenceInputs.cs`
- `src/Services/Project/IProjectDisplayModeState.cs`, `ProjectDisplayModeState.cs`
- `src/ViewModels/Results/ResultsViewModel.cs` (minimal save-adapter slice)
- `src/Configuration/ServiceCollectionExtensions.cs` (minimal registration)

Tests:
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSnapshotContractTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSnapshotFactoryTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectPersistenceMapperTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSaveServiceTests.cs`
- characterization additions to `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`

Architecture artifacts:
- `docs/architecture-migration/maps/{compile-time,di-runtime,state-ownership,reactive,persistence,user-flow}.md` (each exactly one `## Phase 6 Save-Boundary Overlay`)
- `docs/architecture-migration/maps/architecture-model.json`
- `docs/architecture-migration/architecture-widget.html` (regenerated, deterministic)

Evidence (this directory):
- `baseline.md`, `task-2-characterization.md`, `task-3-snapshot-contract.md`, `task-4-snapshot-mapper.md`, `task-5-save-boundary.md`, `task-6-persistence-fixtures-and-guards.md`, `task-6-fixture-manifest.txt`, `task-6-negative-probes.txt`, `task-6-release.trx`, `task-6-release-correction.trx`, `task-7-architecture-dossier-refresh.md`, `task-7-model-v2.json`, `task-7-runtime-v2.json`, `task-7-correction-model-v2.json`, `task-7-correction-runtime-v2.json`, `terminal-plan-review-receipt.md`
- `final/f1-conformance.md`, `final/f2-architecture.md`, `final/f3-executable-qa.md` (informational historical; invalid for acceptance until fresh F1-F4 wave)

Append-only notepads:
- `.omo/notepads/phase-6-project-snapshot-save-boundary/learnings.md`
- `.omo/notepads/phase-6-project-snapshot-save-boundary/decisions.md`

## Evidence paths and current hashes

| Artifact | Status | SHA-256 / note |
|---|---|---|
| Canonical plan `docs/architecture-migration/plans/phase-6-project-snapshot-save-boundary.md` | verified | `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92` (matches frozen) |
| Architecture model `docs/architecture-migration/maps/architecture-model.json` | verified | `554C3E171A6AEF42AA92ED2E88E24BFA9DD7D6B69E9DD91F7D6D216F734A52BF` |
| Widget HTML `docs/architecture-migration/architecture-widget.html` | verified | `2B9D48ED6DC3E15FF6622F3D56737AB31C2B3E67F20F2F95AF061C0EBD472C3B` (matches Task 7 receipt) |
| This consolidated receipt | self-hash NOT recorded | circular; intentionally not hashed |
| `.omo` operational mirror | verified (ledger) | `69E7CE15D5D2EFDE03AAC81456D2D3100F064D45BE89DA9D5F4A433F073D6F1A` (diverges by design) |

All 19 referenced phase-6 evidence artifacts exist (verified by `Test-Path` on 2026-08-26).

## Closed / partial / deferred disposition

### Closed save-side claims (evidence passed)
- One immutable `ProjectSnapshot` (sealed, get-only, defensive copies, no dates/paths/dirty/UI/ViewModel) assembled from canonical `ProjectSession` snapshots.
- Pure `ProjectPersistenceMapper` (`ProjectSnapshot -> ProjectData`, `Version = "1.1"`, delegates existing module mappers, no UI/load/file dependency).
- `IProjectSaveService`/`ProjectSaveService` delegating exactly one snapshot create, one map, one `IProjectFileService.SaveProjectResultAsync`.
- `ResultsViewModel` save adapter wired to the new boundary in the `SaveToFile` slice; legacy `SaveCurrentProject` retained for report/export compatibility.
- Minimal DI registration; six architecture maps, shared model, and deterministic widget regenerated for the save boundary only.

### Partial identifiers (do NOT mark complete)
- `INV-001` partial: save boundary reads canonical session snapshots; aggregate-wide ownership cleanup remains later.
- `INV-006`, `INV-007`, `INV-009`, `INV-012`: only the save-boundary portions proven by guards.
- `INV-014` partial: sequential persistence boundary only; not restore.
- Lifecycle `ST-001..ST-005`: persisted metadata inclusion/exclusion and clean/path semantics only; runtime guard/dirty ownership remains existing behavior.
- `ST-006..ST-019`: save projections for four slices only.
- `CF-013`, `CF-014`: save and save-failure flows.
- `CF-020`, `CF-021`: failure preserves dirty; successful save transitions clean.
- `PN-*`, `PP-*`, `SMC-*`: only save-side nodes/edges and fixture rows.
- `EV-P2`, `EV-P3`, `EV-P4`, `EV-P5`: reused or superseded with phase-6 evidence, never rewritten.

### Deferred to Phase 7+ (not claimed complete)
Full `ProjectData -> ProjectSession` restore coordinator, transactional restore, restore order, `ProjectLoadOrchestrator` changes, Results derived projection migration, `CalculationContext` redesign, formula/invalidation/dirty multiplicity changes, PDF/Excel/Preview/Print behavior changes, broad legacy-owner removal, and Markdown generation removal. Any identifier whose definition includes these remains deferred.

## Task 6 fixture / negative details

- Release persistence/compatibility and negative-guard lane: **124 passed / 1 skipped / 0 failed / 125 total**, exit code 0.
- Single skip: `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` because the external legacy fixture `D:\IA\ace\Тест\тест 40.smc` is absent in this worktree. Recorded as an explicit skip, not a pass.
- Fixture integrity: all **28 tracked `.smc` fixtures** enumerated, sized, and SHA-256 hashed; `MISSING_COUNT=0`, `HASH_INVALID_COUNT=0`, `SMC_DIFF_COUNT=0`; `git diff --name-only -- '*.smc'` returned no paths before and after.
- Negative probes: required architecture guards passed (VM/WPF source dependency, `ProjectData` DTO boundary, duplicate snapshot store/owner, concrete `ProjectFileService`/ViewModel dependency, canonical save slice). Existing invalid-input probes passed (missing file, corrupt JSON, I/O failure, unknown pipe/schema drift, missing/corrupt saved result).
- Standalone invalid-fixture process probe: **`STATUS=NOT_PRESENT`** (honest absence, not a fabricated nonzero result).
- Evidence: `task-6-persistence-fixtures-and-guards.md`, `task-6-fixture-manifest.txt`, `task-6-negative-probes.txt`, `task-6-release.trx`, `task-6-release-correction.trx`.

## Task 7 model / widget hashes and gates

- Six maps each contain exactly one `## Phase 6 Save-Boundary Overlay` (duplicate blocks removed during correction; unrelated `user-flow.md` line restored byte-faithfully).
- Model/widget gates (fresh, explicit args):
  - `verify-widget.mjs --suite model-v2`: exit 0; **33 assertions / 21 mutations**.
  - `verify-widget.mjs --suite runtime-v2`: exit 0; **47 assertions / 20 mutations**.
  - `generate-widget.mjs --check`: exit 0; **14/14 checks**.
  - Two sequential `generate-widget.mjs` runs: exit 0, 0; both outputs **15,945,248 bytes**, byte-identical, SHA-256 `2b9d48ed6dc3e15ff6622f3d56737ab31c2b3e67f20f2f95af061c0ebd472c3b`.
- Validator negative mutations passed in the mandatory suites; standalone invalid-ID and missing-evidence-edge process probes remain `NOT_PRESENT`.
- Evidence: `task-7-architecture-dossier-refresh.md`, `task-7-model-v2.json`, `task-7-runtime-v2.json`, `task-7-correction-model-v2.json`, `task-7-correction-runtime-v2.json`.

## Markdown / wire / dirty decisions

- **Markdown**: removal is a separate future owner-approved change; existing Markdown buttons, UI, and AutomationId remain. Not claimed complete in Phase 6.
- **Wire compatibility**: existing `.smc` wire schema (`Version = "1.1"`, DTO names/fields, enum/string representation, serializer behavior) unchanged; 28 tracked fixtures valid; external `D:\IA\ace\Тест\тест 40.smc` skip.
- **Dirty semantics**: save success invokes the existing clean transition exactly once as characterized; save failure does not clean and preserves dirty state and existing error behavior. No new dirty event, recalculation, invalidation, or restore event introduced.

## Residual risks

- `ProjectSnapshotPersistenceInputs.Templates` uses sync-over-async (`GetAllAsync().GetAwaiter().GetResult()`), deadlock-prone on the UI thread, safe only on the cache-hit fast path (documented, non-gating).
- Headless environment: no manual WPF button/dialog/print QA executed (manual-QA gap, not a gate failure).
- Standalone invalid-ID and missing-evidence-edge process probes are `NOT_PRESENT` (honest absence).
- `.omo` mirror diverges from canonical plan by design (operational ledger), not a second authority.

## Fresh final verification status

- The phase-wide **F1-F4 final verification wave has NOT run as a fresh wave after Tasks 6-8**. It remains REQUIRED.
- Prior `final/f1-conformance.md` (VERDICT: REJECT on plan identity, since resolved as a documented exception), `final/f2-architecture.md` (VERDICT: APPROVE), `final/f3-executable-qa.md` (VERDICT: APPROVE) are retained as informational historical artifacts only and are invalid for Phase 6 acceptance until the fresh F1-F4 wave completes.
- Owner result acceptance is a separate gate and remains PENDING.

## Gates

FINAL WAVE: PENDING
OWNER RESULT ACCEPTANCE: PENDING

## Verdict

TASK 8: PASS
