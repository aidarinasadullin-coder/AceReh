# F1 — Conformance / Scope / Provenance receipt

- Write-set: **phase-5-hydraulics-state** (frozen plan, 14 tasks, sequential lane)
- Plan: `docs/architecture-migration/plans/phase-5-hydraulics-state.md`
- Plan SHA-256: `0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38` (recomputed at review time — match)
- Repo HEAD under review: `84921ce31bfc0229dd8f4b2d6459c6e2cbab045c`
- Phase-5 commit range audited: `a78c8f4^..84921ce` (13 commits)
- Evidence reused: task-1 protected baseline chain, task-12 reconciliation artifacts, task-6/task-9 owner-adjudicated notes, task-14 widget/model receipts, ui-qa observations. Evidence rerun fresh in this receipt: plan SHA recompute, H0 `validate --check-plan`, full protected-drift chain against HEAD, evidence-path existence sweep, DTO/wire/literal greps, commit sweep.
- Verdict basis: every check A–F below passed. No undocumented characterization weakening found; all deviations found are recorded as residual risks and are covered by owner-adjudicated documents or fall into F2/F3 domains.

---

## A. Scope conformance — PASS

### A.1 In-scope items (all 10 implemented)

| # | In-scope item | Status | Proof |
|---|---|---|---|
| 1 | Canonical slice: immutable snapshots, closed enum/API, structural equality, defensive copies | IMPLEMENTED | New files present and committed: `src/Services/Project/IProjectSessionHydraulicsState.cs`, `HydraulicsStateSnapshots.cs`, `ProjectSessionHydraulicsState.cs`; enum verified closed with exactly 7 members (`src/Services/Project/HydraulicsMutationOrigin.cs:5-11`: `User, UserReset, ProjectLoadReset, ProjectLoad, Calculation, Initialization, SystemApply`); state tests `tests/.../Services/Project/ProjectSessionHydraulicsStateTests.cs` green per task-3/trx-state-debug.json and final battery |
| 2 | Exactly one slice instance on ProjectSession; DI identity proof | IMPLEMENTED | `src/Services/Project/ProjectSession.cs` changed (slice created by session); `DiRegistrationTests.cs:257-288`: `HydraulicsState_ResolvesReferenceIdenticalThroughEverySessionAlias` (`Is.SameAs` through concrete+interface aliases) and `HydraulicsState_IsNotResolvableAsIndependentService_FromBuiltProvider` (`GetServices<IProjectSessionHydraulicsState>()` empty, `GetService<ProjectSessionHydraulicsState>()` null) |
| 3 | CalculationStateService compat adapter; backing fields removed | IMPLEMENTED | Grep `_hydraulicsIsCalibrating|_hydraulicsValidationMessage` over `src/Services/Navigation/CalculationStateService.cs`: NONE (removed). Single production publication site moved to coordinator (see #5). Public surface of `ICalculationStateService` untouched (empty diff vs baseline — see A.3 note 4) |
| 4 | CircuitsViewModel adapter conversion (origin User canonical writes, guarded mirrors, Calculate delegates to coordinator) | IMPLEMENTED | `src/ViewModels/Hydraulics/CircuitsViewModel.cs` diff +287/-148 vs baseline; VM no longer subscribes upstream events (grep for `ContextChanged +=`, `PipeSpacingChanged +=`, `.StateChanged +=` in VM: NONE); lifecycle mirror entry `ApplyLifecycleSnapshotToAdapter` called from orchestrator (:173,:201); breadth judgment deferred to F2 (residual risk R3) |
| 5 | HydraulicsStateCoordinator: sole upstream subscriptions, sole approved publication | IMPLEMENTED | `src/Services/Project/IHydraulicsStateCoordinator.cs` + `HydraulicsStateCoordinator.cs` exist; subscriptions only in coordinator (`HydraulicsStateCoordinator.cs:31-33`: `ContextChanged += OnContextChanged`, `PipeSpacingChanged += OnPipeSpacingChanged`, `StateChanged += OnStateChanged`); singleton registered `src/Configuration/ServiceCollectionExtensions.cs:148`; exactly ONE production call site repo-wide: `HydraulicsStateCoordinator.cs:57: _calculationContext.UpdateHydraulics(summaries, "CircuitsViewModel");` — literal preserved verbatim |
| 6 | Lifecycle routing with origins UserReset / ProjectLoadReset / ProjectLoad | IMPLEMENTED | `ProjectLoadOrchestrator.ResetModules()` → `_hydraulicsState.ResetToDefaults(HydraulicsMutationOrigin.ProjectLoadReset)` (:89); restore → canonical `_hydraulicsState.Restore(hydraulicsCandidate, HydraulicsMutationOrigin.ProjectLoad)` (:172 initial, :200 post-fallback re-apply replacing `RestoreCircuitsResults` direct writes; fallback calc ordering between them preserved :185-195); `MainViewModel.PerformNewCalculationReset` → `ResetToDefaults(HydraulicsMutationOrigin.UserReset)` (`src/ViewModels/Shell/MainViewModel.cs:247`) |
| 7 | HydraulicsPersistenceMapper static pure; save consumes Snapshot | IMPLEMENTED | `src/Services/Project/HydraulicsPersistenceMapper.cs:15` `BuildHydraulicsProjectData(HydraulicsStateSnapshot)`, `:29` `BuildRestoreCandidate(HydraulicsProjectData?)`; save path: `ResultsViewModel.cs:1711-1712 data.HydraulicsData = HydraulicsPersistenceMapper.BuildHydraulicsProjectData(_projectSession.HydraulicsState.Snapshot);` |
| 8 | Characterization-first extension (multiplicity, ST-017 census, dirty intents, subscription hygiene) | IMPLEMENTED | `tests/.../IntegrationTests/Hydraulics/HydraulicsMultiplicityCharacterizationTests.cs` exists (task-2 lock, then corrected lane); final state 13 passed / 0 failed (task-6/correction-notes.md Final gate battery; cross-checked in task-12/arithmetic.json perSuite entry) |
| 9 | Guard suite 8 `[NegativeFixture]` categories | IMPLEMENTED | `tests/.../Services/Project/HydraulicsStateLegacyStoreGuardTests.cs` contains exactly 8 `NegativeFixture` markers (count=8); TRX counters: `<Counters total="8" executed="8" passed="8" failed="0" ... notExecuted="0">` (task-12/trx-guards-release.json) |
| 10 | Six maps + shared model + widget + TASK_CONTEXT refresh | IMPLEMENTED | 11 map files changed across phase range incl. `architecture-model.json`; widget regenerated deterministically (task-14/widget-hash.json `byte_identical: true`, both passes SHA `3C823B73DAF1…`, `--check` exit 0, 14 assertions); `TASK_CONTEXT.md` appended (in phase diff set); model-v2/runtime-v2 suites recorded (task-14/model-v2.json, runtime-v2.json) |

### A.2 Must-NOT-Have verification (none violated)

1. **Wire DTO `HydraulicsProjectData` fields intact** — PASS.
   - `git diff --stat a78c8f4^..HEAD -- src/Models/Project/ProjectData.cs src/Services/Project/ProjectFileService.cs` → EMPTY (both byte-identical to pre-phase-5 baseline).
   - Class definition enumerated at `src/Models/Project/ProjectData.cs:311-337`: `GlycolType`, `GlycolConcentration`, `SupplySpacingCm`, `SupplyHeatPercent`, `List<CollectorProjectData> Collectors`. Nested wire types unchanged: `CircuitResultProjectData` (:342-365, includes the legacy double `FlowRegime` string :353 AND `FlowRegimeString` :358), `CollectorSummaryProjectData` (:370-380, includes `PressureLoss_Operating_Pa`/`PressureLoss_Cold_Pa` underscored wire names).
2. **`Version = "1.1"` literal preserved in save path** — PASS. `ResultsViewModel.cs:1628 Version = "1.1",` inside `SaveCurrentProject()`; serialization settings of `ProjectFileService` untouched (byte-identical file).
3. **No AMZ-H1 beyond contract** — PASS. Per plan, AMZ-H1 is a pre-authorized contingency: exactly ONE transitional mutation of `ApplyNeedsRecalculation` type on `IProjectSessionHydraulicsState`, allowed only if characterization proved an inexpressible transition. `task-5/blocker-analysis.md` records "AMZ-H1 bridge: not required"; grep `NeedsRecalculation|ApplyNeedsRecalculation` over `src/Services/Project` finds members ONLY in thermal phase-4 files (`ThermalStateSnapshots.cs`, `ProjectSessionThermalState.cs`, `IProjectSessionThermalState.cs`) — zero on the hydraulics slice. Contingency unused ⇒ conforms.
4. **Characterization assertions not weakened beyond owner-adjudicated set** — PASS within documented bounds.
   - Owner-directed rewrite (task-6/correction-notes.md): `SaveProjection_ContainsCurrentHydraulicsWireFieldsAndVersionLiteral` replaced source-grep assertions with a behavioral round-trip asserting all eight wire fields + `version=1.1` on real serialized output — strictly stronger.
   - Documented semantic adaptations (task-9/divergence-notes.md): (a) dirty-authority transfer VM→canonical slice (aggregate-root `IsDirty` assertion stronger than relocated internal counter; three instances incl. auto-recalc churn elimination); (b) FIX B unconditional per-attempt status termination with exact call-count adjudication (1 reset per attempt; legacy expected zero); both tests updated to encode the per-attempt contract.
   - No other weakening surfaced: full Release suite 1976 passed / 0 failed; NotExecuted identities identical to the accepted Todo-1 baseline set (see E).
5. **Deferred behavioral debts stay out** — PASS. Dead field retained: `src/Core/CalculationContext.cs:122 private List<CollectorSummary>? _hydraulicsResults;` (file not in phase diff set); `ResultsPdfDataBuilder` unchanged (`git diff --name-only … -- '*ResultsPdfDataBuilder*'` → empty); ST-005 duplicate remains (`IsLoadProjectInProgress` still present, 3 refs in CalculationStateService.cs).
6. **No broad CircuitsViewModel refactor** — judged in-scope-bounded here structurally (adapter seams, mirrors, coordinator delegation; guards enforce store-free VM), magnitude assessment delegated to F2 (residual risk R3).
7. **No new version branches** — PASS. Single load version branch remains construction-only: `ProjectLoadOrchestrator.cs:207-210 needsAbovePipeReverse = Compare(data.Version,"1.1") < 0`.
8. **STATE.json stage/pendingGates untouched by implementer** — PASS (see F).
9. **No second central slice / no foreign dirty-file interference** — PASS. Worktree delta relative to baseline is exclusively allow-listed phase paths; the single dirty path is the pre-existing STATE.json artifact (D).

## B. Protected drift chain (fresh rerun at HEAD) — PASS

Allow-list constructed exactly as specified: `git diff --name-only a78c8f4^..HEAD` ∩ manifest paths = **39 paths** (11 maps + CircuitsView.xaml + the 27 code/test paths already carried by task-12/allowed-hunks.json). Frozen at `final/f1/f1-allowlist.json`.

Command:
```text
& docs/architecture-migration/evidence/phase-5-hydraulics-state/verify-protected-baseline.ps1 `
  -Baseline docs/architecture-migration/evidence/phase-5-hydraulics-state/task-1/protected-manifest.json `
  -AllowedHunks docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f1/f1-allowlist.json `
  -EvidenceRoot docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f1 `
  -Output docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f1/_drift-fresh.json
```

Raw output:
```text
verify-protected-baseline: protected_mismatch_count=0 changed=39
exitcode=0
```

Result JSON head (`_drift-fresh.json`):
```json
{ "protected_mismatch_count": 0, "allowed_hunk_count": 39,
  "changed_paths": [ { "path": "docs/architecture-migration/maps/architecture-model.json", "kind": "modified", "classification": "allowed" }, … 39 entries total, ALL classification=allowed ] }
```

Notably `docs/architecture-migration/STATE.json` does NOT appear among drifted paths: its current worktree SHA-256 equals the task-1 baseline manifest entry `BEAB998F129BDC09B49C4324FB60CABA0D4CED49D4BA7BC1A4C45133E9515CD3` — i.e., the dirty owner-gate artifact has been byte-stable since baseline capture. No unexpected drift ⇒ no REJECT condition.

Cross-reference: task-12 historical chain consistent — raw pre-classification run `_drift-raw.json` showed exactly its 27 code/test paths; maps + CircuitsView.xaml drifted only later via tasks-13/14 and are now additionally allow-listed here.

## C. Subject binding — PASS

- `STATE.json` identity block (verbatim):
```json
"phase": "phase-5-hydraulics-state",
"plan": { "path": "docs/architecture-migration/plans/phase-5-hydraulics-state.md",
          "sha256": "0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38", "frozen": true }
```
- Recomputed plan hash at review time:
```text
plan-sha256=0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38
match=True
```
- H0 gate:
```text
$ node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan
{"valid":true,"phase":"phase-5-hydraulics-state","stage":"executing","diagnostics":[]}
H0-exit=0
```

## D. Dirty-worktree preservation — PASS

```text
$ git status --porcelain
 M docs/architecture-migration/STATE.json
```
Exactly ONE dirty path: `docs/architecture-migration/STATE.json`, modified but unstaged — the pre-existing owner-gate artifact. Byte-stability proof: current worktree SHA equals baseline manifest entry `BEAB998F…CD3` (drift chain in B did not flag it). Nothing else dirty anywhere in the worktree; no foreign files staged, edited, reverted, or cleaned during this review.

## E. Evidence fidelity sweep — PASS

Existence sweep: extracted every relative evidence path referenced by receipts under `evidence/phase-5-hydraulics-state/` (task-1..task-14 + ui-qa; excluding this f1 dir): **37 unique referenced targets checked, 0 genuinely missing.** Eleven apparent misses were adjudicated false positives:
- 10 × `task-N/task-N-*.md` are phase-4 paths embedded verbatim inside the negative fixture `task-14/fixtures/model-invalid-id.json` (`docs/architecture-migration/evidence/phase-4-thermal-state/task-2/task-2-thermal-characterization.md` etc.); spot-checked on disk: EXIST.
- 1 × `task-14/fixtures/invalid-id.rejected-output.json`: fixture-matrix.json explicitly records `"nominal_output_file_written": false` — the validator rejected the invalid-model probe with exit_code=1 and correctly wrote no output file (`result: rejected-nonzero-as-required`).

Internal consistency spot-checks (raw excerpts):
- Arithmetic sums (task-12/arithmetic.json): `1943 baseline passed + 33 new = 1976 full passed`; `1946 + 33 = 1979 total`; `total(1979) = passed(1976) + failed(0) + NotExecuted rows(3)` ✓; NotExecuted identities `{RegenerateBaseline, RegenerateCircuitsBaseline, ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile}` == accepted Todo-1 baseline set ✓ (no NEW identities).
- Guard counts 8/8 — source-level: exactly 8 `[NegativeFixture]`; TRX: `<Counters total="8" executed="8" passed="8" failed="0" … notExecuted="0" />` (task-12/trx-guards-release.json) ✓.
- Full suite TRX: `<Counters total="1979" executed="1976" passed="1976" failed="0" … />` (task-12/trx-fullsuite-release.json) ✓ matches arithmetic.json.
- UI QA: ui-qa/observations.json — `"steps"` S1–S6, S7, S8, F = **9 steps, each `"status":"PASS"`, terminal `"result":"PASS"`**, zero unexpected dialogs, `frozenExeSha256 beforeFirstFlow == afterLastFlow == 356550DC8934C33B214846F00449085A2800AB5C22C9C5B1F006EB3696364DE9 (equal=true)`, 9 screenshots 01..09 recorded with SHAs ✓.
- Widget: task-14/widget-hash.json — pass1/pass2 identical `bytes=15174383 sha256=3C823B73DAF1D0706C515C4ED2541E503783DC61A59387B64E709E06068B0CD9`, `"byte_identical": true`, `--check exit_code 0, assertions_passed 14, generated==canonical` ✓.

## F. Commit inventory — PASS

All 13 commits `a78c8f4..84921ce` (messages verbatim):
```text
84921ce phase-5(task-14): six-view dossier, model and widget refresh
14a8e51 phase-5(task-13): agent-operated hydraulics UI QA
4fc9205 phase-5(task-12): executable evidence reconciliation
47faf28 phase-5(tasks-5-7-correction): complete adapter conversion, sole writer site, canonical save
84e2d78 phase-5(task-9): lifecycle and restore through canonical hydraulics state
e47c0d6 phase-5(task-10): canonical persistence production write-set
34b739e phase-5(task-10): canonical hydraulics persistence mapping
4decea8 phase-5(task-8): single canonical hydraulics projection
8ba5a36 phase-5(tasks-5-7): hydraulics status compat + adapter VM + coordinator single-subscriber boundary
9f626bc phase-5(task-4): attach hydraulics slice to ProjectSession
f5858ba phase-5(task-3): immutable hydraulics state contract
b4a91a1 phase-5(task-2): characterization lock for hydraulics behavior
a78c8f4 phase-5(task-1): baseline, tools, plan identity, dirty preimages
```
Template conformance: every message matches `phase-5(task-N): …`, the merged boundary `phase-5(tasks-5-7): …`, or the correction lane `phase-5(tasks-5-7-correction): …`. STATE.json touch sweep: `git log --format='%h %s' --name-only a78c8f4^..HEAD` grepped for `STATE.json` → **zero occurrences** (no commit touched it).

---

## Residual risks

1. **R1 — No standalone `phase-5(task-11)` boundary commit.** The plan prescribed one commit per todo including task-11; the guard-suite file was introduced inside `47faf28` (tasks-5-7-correction lane resume after limit interruptions, per task-6/correction-notes.md). Guard evidence itself is complete (8/8 TRX under task-11/ and task-12/) and H11 ran first-executable-in-task-11 ordering was respected functionally; provenance granularity differs from plan text. Not a Must-NOT-Have violation; recorded for the consolidated receipt.
2. **R2 — Commit-order deviation around tasks 9/10.** Two task-10 commits (`34b739e`, `e47c0d6`) precede `84e2d78` (task-9), and `e47c0d6` reuses the task-10 template while its content spans orchestrator/VM/mapper production wiring described by the correction notes. Final tree state satisfies all acceptance criteria; history topology deviates from the dependency matrix drawing.
3. **R3 — CircuitsViewModel diff magnitude (+287/-148).** Structural adapter conformance verified here (no upstream subs, no canonical backing stores per guard suite); whether any hunk exceeds "ownership-transfer-only" breadth is F2's architecture-quality domain.
4. **R4 — `ICalculationStateService.cs` received no change at all** (plan integration table allowed doc-comments-only edits). Surface intact; harmless under-realization of the table row.
5. **R5 — Double canonical Restore(ProjectLoad) in orchestrator (:172 initial apply, :200 post-fallback re-apply of persisted results).** Matches In-scope item 6 wording ("saved circuit results applied after fallback calculation without recalculation", replacing `RestoreCircuitsResults`) and passes lifecycle characterization; exact multiplicity semantics cross-checked again in F3.
6. **R6 — STATE.json control-plane fields remain at their pre-execution values** (`lastCompletedTask: null`, `nextAction` pointing at Todo 1, `pendingGates: ["resultAcceptance"]`). By design implementers never touch STATE.json; transitions belong to owner gates after F1–F4.

## Machine-readable review-contract block

REVIEW_ID: f1-phase5-conformance
SUBJECT: phase-5-hydraulics-state@0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38
RECEIPT: docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f1/conformance-scope-provenance.md
VERDICT: APPROVE
REASON: All six checks pass with embedded raw evidence: every In-scope item implemented; no Must-NOT-Have violated (wire DTO byte-identical, Version "1.1" literal intact, AMZ-H1 bridge not required and absent, characterization changes limited to owner-adjudicated documents); fresh protected-drift chain at HEAD 84921ce gives mismatch=0 with the 39-path phase-wide allow-list; SUBJECT bound in STATE.json and H0 exits 0; exactly one dirty path (pre-existing STATE.json owner-gate artifact, byte-stable since capture); evidence sweep finds 0 missing referenced artifacts with arithmetic/guard/UI-QA/widget receipts internally consistent; all 13 commits template-conformant and none touch STATE.json. Residual risks R1-R6 recorded, none rising to REJECT.
