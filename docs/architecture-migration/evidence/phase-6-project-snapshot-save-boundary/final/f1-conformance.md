# Phase 6 Final Verification Wave F1 — Conformance, Scope, Provenance, State/Plan Identity

## Audit identity

- Review: `F1`
- Subject: `phase-6-project-snapshot-save-boundary`
- Audited artifacts: canonical frozen plan, `.omo` operational ledger, `STATE.json`/archive, Task 8 consolidated receipt, `TASK_CONTEXT.md`, Phase 6 write-set (Tasks 1-7), protected baseline
- Audit mode: read-only; no staging, commit, reset, revert, clean, install, or unrelated-path edits
- Result acceptance: remains a separate owner decision and is NOT inferred here

## Machine-readable verdict

REVIEW_ID: F1
SUBJECT: phase-6-project-snapshot-save-boundary
RECEIPT: docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/final/f1-conformance.md
VERDICT: APPROVE
REASON: Canonical frozen plan SHA-256 `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92` matches the required frozen SHA (verified by read-only Get-FileHash); the `.omo` mirror is confirmed an operational execution ledger only (Tasks 1-8 checked, canonical plan Tasks 1-8 unchecked), not a second authority; `STATE.json` is absent (Test-Path=False) and archive STATE is provenance-only with no import; the 37 protected pre-existing dirty paths are excluded from Phase 6 attribution; the Phase 6 write-set (Tasks 1-7) is within the plan allow-list and contains no canonical-plan change, STATE/workflow import, DTO/version/serializer change, restore/load addition, Markdown removal, or export redesign; Task 8 receipt and TASK_CONTEXT are append-only and reference existing evidence. All required conformance gates pass. FINAL WAVE and OWNER RESULT ACCEPTANCE remain PENDING and are not claimed.

## Plan identity and frozen SHA

Frozen SHA required by plan/review provenance:

`C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92`

Fresh read-only PowerShell `Get-FileHash -Algorithm SHA256` results:

| Path | Bytes | SHA-256 | Frozen SHA match |
|---|---:|---|---:|
| `docs/architecture-migration/plans/phase-6-project-snapshot-save-boundary.md` (canonical) | 29455 | `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92` | **true** |
| `.omo/plans/phase-6-project-snapshot-save-boundary.md` (operational ledger) | 29455 | `09FF2469D2D2338539789FB71360A10BF29A6AFE8A56950EE9AC6E38D8946C03` | n/a (ledger, not authority) |
| `docs/architecture-migration/archive/STATE.json` (provenance-only) | — | `850ACDAACF8048FFACBEB10B851B97148153D20483F71B75C6908C6A658761F8` | n/a |

- Canonical plan checkbox state: Tasks 1-8 all `[ ]` (immutable frozen baseline). ✓
- `.omo` mirror checkbox state: Tasks 1-8 all `[x]` (operational ledger tracking executed work). ✓
- The canonical plan is the active authority; its SHA matches the frozen value exactly. The `.omo` mirror divergence is the documented, resolved plan-identity exception (decisions.md / TASK_CONTEXT.md 2026-08-26): it is NOT a second authority, so its hash need not equal the canonical plan.

## STATE read-only audit

- `Test-Path docs/architecture-migration/STATE.json` = **False** (absent/unchanged). No canonical import or `validate-state.mjs` run was performed (not authorized; workflow retired and file absent).
- `docs/architecture-migration/archive/STATE.json` is present and treated as provenance-only; no Phase 6 code imports or relies on it (forbidden-token scan found no `STATE.json`/`validate-state`/`workflow/` reference in any Phase 6 file).

## Scope and provenance

### Protected unrelated baseline (excluded from Phase 6 attribution)

The 37 pre-existing dirty paths recorded in `baseline.md` are protected and were NOT attributed to Phase 6. Current `git status --porcelain=v1` confirms they remain present and were not modified by this F1 audit:

- `.opencode/commands/architecture-{approve,draft,plan,resume,start}.md`
- `docs/architecture-migration/AGENTS.md`
- `docs/architecture-migration/STATE.json` (tracked deletion already present)
- `docs/architecture-migration/evidence/phase-0.5-acceptance-v2.json`
- `docs/architecture-migration/workflow/validate-state.mjs`, `validate-state.test.mjs` (tracked deletions)
- `src/ViewModels/Hydraulics/CircuitsViewModel.cs`
- pre-existing unrelated tests under `tests/SnowMeltingCalculator.Tests/{Construction,IntegrationTests/Hydraulics,Services/Navigation,Services/Project,ViewModels/Hydraulics,ViewModels/MainViewModelTests,ViewModels/ResetOrchestrationTests,ViewModels/ResultsViewModelTestHelpers}.cs`
- `docs/architecture-migration/archive/STATE.json`, `docs/architecture-migration/evidence/phase-5.1-hydraulics-dirty-ownership-correction/**`, `docs/architecture-migration/plans/phase-5.1-hydraulics-dirty-ownership-correction{.draft,}.md`

No protected path was altered by Phase 6 or by this audit.

### Phase 6 write-set (Tasks 1-7) — within plan allow-list

Production (new, untracked):
- `src/Services/Project/{IProjectDisplayModeState,ProjectDisplayModeState,IProjectSaveService,ProjectSaveService,IProjectSnapshotFactory,ProjectSnapshotFactory,ProjectPersistenceMapper,IProjectSnapshotPersistenceInputs,ProjectSnapshotPersistenceInputs,ProjectSaveDates,ProjectSnapshot}.cs`

Production (modified, in-scope):
- `src/ViewModels/Results/ResultsViewModel.cs` — minimal save-adapter slice (63 lines): adds `IProjectSaveService`/`IProjectDisplayModeState` dependencies; `SaveToFile` delegates to `_projectSaveService.SaveAsync` with a `SaveLegacyFileAsync` fallback that preserves `SaveCurrentProject` for report/export compatibility. No restore/load added; all export/Markdown commands retained.
- `src/Configuration/ServiceCollectionExtensions.cs` — 4-line registration of the four new Phase 6 services only. No STATE/workflow import.

Tests (new, untracked):
- `tests/SnowMeltingCalculator.Tests/Services/Project/{ProjectSnapshotContractTests,ProjectSnapshotFactoryTests,ProjectPersistenceMapperTests,ProjectSaveServiceTests}.cs` plus characterization additions to `ResultsViewModelOpenProjectTests.cs`.

Architecture artifacts (Task 7, in-scope):
- `docs/architecture-migration/maps/{compile-time,di-runtime,state-ownership,reactive,persistence,user-flow}.md` (each +4 lines = one `## Phase 6 Save-Boundary Overlay`)
- `docs/architecture-migration/maps/architecture-model.json` (+93)
- `docs/architecture-migration/architecture-widget.html` (regenerated, deterministic, +20199)

Evidence (this directory) and append-only notepads — created/extended by Tasks 1-8.

### Forbidden-change verification (read-only)

| Check | Result | Basis |
|---|---|---|
| Canonical plan changed | PASS (no) | Canonical SHA matches frozen; all Tasks `[ ]` |
| STATE/workflow import in Phase 6 files | PASS (none) | Case-sensitive scan: no `validate-state`/`workflow/`/`STATE.json`/`JsonSerializer`/`XmlSerializer`/`DataContractSerializer` in any Phase 6 file |
| DTO/version/serializer change | PASS (none) | `ProjectPersistenceMapper.cs` preserves existing `Version = "1.1"` wire DTO; no new serializer introduced |
| Restore/load addition | PASS (none) | `ResultsViewModel.cs` load/restore references (`ProjectLoadOrchestrator`, `LoadProject*`, `BeginProjectRestore`) are pre-existing; Phase 6 adds only the save delegation |
| Markdown removal | PASS (none) | `ExportPdf`, `ExportOperatingMarkdownReport`, `ExportDesignColdMarkdownReport`, `ExportMarkdownReportAsync`, `ExportExcel`, `PreviewPdf`, `PrintPdf` all present |
| Export redesign | PASS (none) | No export-behavior change in Phase 6 write-set |
| Unrelated paths changed by Phase 6 | PASS (none) | Protected baseline paths excluded; Phase 6 write-set limited to the paths above |

## Task 8 receipt and TASK_CONTEXT append-only

- `phase-6-consolidated-receipt.md` (Task 8) is a new bookkeeping artifact that references existing evidence (lists 19 phase-6 evidence artifacts, model/widget hashes, plan SHA, fixture/negative details). Its declared Task 8 write-set is exactly three classes: one `TASK_CONTEXT.md` append, this consolidated receipt, and notepad appends. No production/test/map/widget/frozen-plan/STATE change.
- `TASK_CONTEXT.md` Phase 6 entries are appended (dated 2026-08-26) and reference existing evidence (plan SHA, evidence paths, partial/deferred identifiers). Append-only confirmed; no retroactive edit of prior entries.
- Both are consistent with the AGENTS.md append-only protocol (material decision / completed gate).

## Current hashes — Phase 6 write-set

| Artifact | SHA-256 |
|---|---|
| `src/Services/Project/IProjectDisplayModeState.cs` | `46394C01F86F25F4B544D2CBAFBE16728587763A2CE47D52AD7925B7E04B80D8` |
| `src/Services/Project/IProjectSaveService.cs` | `CA5FAA2F011452019D0425A9A9527F68A68CC092A57AE2B56982E94042E51424` |
| `src/Services/Project/IProjectSnapshotFactory.cs` | `0B233D0940BC43B06131B2A7D2F7DEB76237CAC0474E34625F8EAA5B627924BA` |
| `src/Services/Project/IProjectSnapshotPersistenceInputs.cs` | `6005A71EA7CED53D285AA7E186B9ED3D96BFDD937DB868C62E420A264BE30CCA` |
| `src/Services/Project/ProjectDisplayModeState.cs` | `D9650E3F4D8202C7ECF16C97202CFCC25F75090C966CA9605D939C62C003EFDE` |
| `src/Services/Project/ProjectPersistenceMapper.cs` | `55BA9C4F1CE645A943E40A8E00493DD98E94C8A402E6421E15AE10E67B55E890` |
| `src/Services/Project/ProjectSaveDates.cs` | `C9A88054AA5BF6B75F41CEE62EBB061A0BA30346A181A026F0BEB7EA956B664E` |
| `src/Services/Project/ProjectSaveService.cs` | `1736199DF68CC966162B548770087252670C6C2CC2719D5DC1A1AE316BE8ADE9` |
| `src/Services/Project/ProjectSnapshot.cs` | `DCB5A58E5C11523DA88D66F4EE35B7ABD8709AD288A3D9BAB994C14EB72D04C6` |
| `src/Services/Project/ProjectSnapshotFactory.cs` | `95E3FE5047114AAFAF23CAFA1E7958108ADCC356B22789CA8C2DBE695D542350` |
| `src/Services/Project/ProjectSnapshotPersistenceInputs.cs` | `0F7240CD21A4D785B63B93EBEE5C2713CBD45AD8110FC121B76BE63A9F6D9E35` |
| `src/ViewModels/Results/ResultsViewModel.cs` | `26E94BBAB4EB7924379BFC85FD77DE78133B1AD89043571F07889E7AE76B3072` |
| `src/Configuration/ServiceCollectionExtensions.cs` | `FBD629D55967502177D6F7F7D2110A044ECC954D0202884148FE7C4D6AB5A690` |
| `docs/architecture-migration/maps/architecture-model.json` | `554C3E171A6AEF42AA92ED2E88E24BFA9DD7D6B69E9DD91F7D6D216F734A52BF` |
| `docs/architecture-migration/architecture-widget.html` | `2B9D48ED6DC3E15FF6622F3D56737AB31C2B3E67F20F2F95AF061C0EBD472C3B` |

## Residual risks (recorded, non-gating)

- `ProjectSnapshotPersistenceInputs.Templates` uses sync-over-async (`GetAllAsync().GetAwaiter().GetResult()`), deadlock-prone on the UI thread, safe only on the cache-hit fast path (documented in Task 7/8).
- Headless environment: no manual WPF button/dialog/print QA executed (manual-QA gap, not a gate failure).
- Standalone invalid-ID and missing-evidence-edge process probes are `NOT_PRESENT` (honest absence, not fabricated).
- `.omo` mirror diverges from canonical plan by design (operational ledger), not a second authority.

## External fixture skip (Task 6)

- Task 6 Release persistence/compatibility lane: **124 passed / 1 skipped / 0 failed / 125 total**. The single skip is `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` because the external legacy fixture `D:\IA\ace\Тест\тест 40.smc` is absent in this worktree. Recorded as an explicit skip, not a pass. All 28 tracked `.smc` fixtures valid (`MISSING_COUNT=0`, `HASH_INVALID_COUNT=0`, `SMC_DIFF_COUNT=0`).

## NOT_PRESENT negative process probes

- Standalone invalid-architecture-dependency fixture process probe: `STATUS=NOT_PRESENT` (honest absence, not a fabricated nonzero result). Required invalid-input behavior is evidenced by existing passing tests; standalone invalid-ID and missing-evidence-edge process probes also `NOT_PRESENT`.

## Discrepancy (documentation staleness, not a scope/provenance failure)

- The Task 8 consolidated receipt recorded the `.omo` mirror SHA as `69E7CE15D5D2EFDE03AAC81456D2D3100F064D45BE89DA9D5F4A433F073D6F1A`, but the fresh hash is `09FF2469D2D2338539789FB71360A10BF29A6AFE8A56950EE9AC6E38D8946C03`. This is because the operational ledger was updated to check Task 8 after the receipt's hash was captured. The mirror is explicitly an operational ledger (not a second authority), so its hash is allowed to change as tasks are tracked; the canonical plan SHA remains `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92` (matches frozen). No canonical plan byte, `STATE.json`, or workflow file was changed. This is a documentation-staleness observation only.

## Pending

- `FINAL WAVE: PENDING` and `OWNER RESULT ACCEPTANCE: PENDING` (per Task 8 receipt). This F1 APPROVE is the conformance/scope/provenance gate only; it does NOT constitute owner result acceptance or Phase 7+ completion. F2/F3/F4 remain independent pending gates.

## Commands and exit codes

All commands ran from `D:\IA\3ace v.2` (read-only):

| Command | Exit | Observed |
|---|---:|---|
| `git status --porcelain=v1` | 0 | protected baseline + Phase 6 write-set + Task 8 evidence |
| `git diff --name-only` | 0 | tracked changed paths (protected + Phase 6) |
| `git diff --check` | 0 | no whitespace errors in touched files |
| `Get-FileHash` canonical plan | 0 | `C56E66...A4E92` (matches frozen) |
| `Get-FileHash` `.omo` mirror | 0 | `09FF2469...` (operational ledger) |
| `Get-FileHash` archive STATE | 0 | `850ACDAA...` (provenance-only) |
| `Test-Path STATE.json` | 0 | `False` (absent) |
| Forbidden-token scan (Phase 6 files) | 0 | 0 STATE/workflow/serializer hits; `Version="1.1"` preserved |

REVIEW_ID: F1
SUBJECT: phase-6-project-snapshot-save-boundary
RECEIPT: docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/final/f1-conformance.md
VERDICT: APPROVE
REASON: Canonical frozen plan SHA-256 C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92 matches frozen; .omo mirror is operational ledger only (Tasks 1-8 checked, canonical all unchecked); STATE.json absent, archive provenance-only, no import; 37 protected dirty paths excluded; Phase 6 write-set within allow-list with no canonical-plan/STATE/workflow/DTO-version-serializer/restore-load/Markdown-removal/export-redesign change; Task 8 receipt and TASK_CONTEXT append-only referencing existing evidence. FINAL WAVE and OWNER RESULT ACCEPTANCE remain PENDING.
