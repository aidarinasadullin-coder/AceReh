# Phase 3 Final Verification F1: Plan Compliance and Protected Scope

Audit date: `2026-08-19`

## Decision

The Phase 3 implementation paths can be mapped to Tasks 1-13 and the approved
pre-Task-13 correction, and no staged, removed, or status-changed path was
found. The protected boundary nevertheless does not pass the plan's strict F1
rule. One pre-existing workflow file has unexplained byte drift and two current
additions have no Phase 3 task allow-list attribution. Because the plan says
that any unexplained path or user-hunk drift is `REJECT`, this review cannot
approve the lane.

## Inputs and method

- Authority: `docs/architecture-migration/plans/phase-3-construction-state.md`,
  Task 1 receipt and its 232-row path/blob ledger, all current Phase 3 receipts,
  the pre-Task-13 correction receipt, and the four Phase 3 notepads.
- Current identity was obtained with read-only commands prefixed by
  `$env:GIT_MASTER='1';`: `git rev-parse --show-toplevel`, `git rev-parse HEAD`,
  `git symbolic-ref HEAD`, `git rev-parse --abbrev-ref '@{u}'`,
  `git rev-list --left-right --count '@{u}...HEAD`, and
  `git remote get-url origin`. All exited `0`.
- Current status was captured directly as bytes by Python
  `subprocess.check_output(['git','status','--porcelain=v1','-z','--branch'])`.
  It was split only on `0x00`. Untracked directories were expanded with
  `git ls-files --others --exclude-standard -z`; every current file was hashed
  with `git hash-object -- <path>`.
- The Task 1 side was reconstructed from the receipt's complete 232-row
  `status/path/blob` ledger. The original temporary Task 1 binary is no longer
  present in the repository; therefore the receipt's recorded raw length
  (`12543` bytes), branch chunk, counts, and complete blob ledger are the
  retained baseline authority. This is sufficient for path/status/blob set
  comparison, but the absent raw stream is recorded rather than silently
  claimed present.

## Repository identity and NUL-safe status

| Field | Task 1 | Current | Result |
| --- | --- | --- | --- |
| Git root | `D:/IA/ace v.2` | `D:/IA/ace v.2` | same |
| HEAD | `e655735dfa66c00cf9c53be93d511eda8989e8bf` | same | same |
| Branch | `refs/heads/master` | same | same |
| Upstream | `origin/master` | same | same |
| Ahead / behind | `33 / 0` | `33 / 0` | same |
| Remote | `https://github.com/aidarinasadullin-coder/AceReh.git` | same | same |
| Staged paths | `0` | `0` | same |

The current raw status stream is `15864` bytes with SHA-256
`B3DD212AFF47B80297D1B9A8B54BD15A9C238D3DD47D5BAF91495A1996524980`.
It contains 269 porcelain path records: 238 worktree-modified records, 31
collapsed untracked records, and zero staged records. Expanded to files, the
current dirty ledger has 398 paths.

## Symmetric protected-boundary result

The comparison is symmetric over the Task 1 232-file ledger and the current
398-file expanded ledger:

| Category | Count | Complete inventory |
| --- | ---: | --- |
| Removed baseline paths | 0 | none |
| Status-changed baseline paths | 0 | none |
| Staged paths | 0 | none |
| Exact baseline status/blob matches | 225 | all Task 1 rows except the seven listed below |
| Baseline paths with changed blob | 7 | listed below |
| Current added files | 166 | exhaustively grouped below |

The seven baseline paths with changed working-tree blobs are:

| Path | Task 1 blob | Current blob | Attribution |
| --- | --- | --- | --- |
| `.omo/start-work/ledger.jsonl` | `b9e57048e4ad285deec7adeaac1dbebe6c3dbd06` | `4013eddac5cf96945adf36d766d11aeef588057e` | unexplained and outside every Task 1-13 allow-list |
| `docs/architecture-migration/TASK_CONTEXT.md` | `1a490652947b5218349e54a82ea0e3fcae82367f` | `999e397d8dfab90de81a513b187d82aaef0ef1cd` | Tasks 1/13 and correction workflow record |
| `docs/architecture-migration/plans/phase-3-construction-state.md` | `96d2176893a470c3933de7094b39eb2a01a4d73a` | `0da00549dc484ad09f29ccbe522b71bd148e25cd` | parent tracking history; current canonical/tracking copies are identical |
| `src/ViewModels/Construction/ConstructionViewModel.cs` | `e8f7f88d46888984770fc19598dc0d45b9c175de` | `5064767a6edf1e2758ae595ef41598747c318205` | Tasks 6, 7, 9 recovery, 10 and approved Task 10 exception |
| `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs` | `ef961f86f85cf921a4a94352a40c44ce39981ebc` | `1f30fb1f5bb64d435be6b421c62b9d945a5b7fd9` | Tasks 3, 7, 9 and lifecycle fixture repair |
| `tests/SnowMeltingCalculator.Tests/Construction/ConstructionViewModelTests.cs` | `06d6647bd156a843fdea6d458a5ffcc054d52a77` | `f609fbb5514f567368764e7200aefb819000f02e` | Tasks 3, 6, 7, 9 recovery and 10 |
| `tests/SnowMeltingCalculator.Tests/Services/Navigation/DialogServiceThreadAffinityTests.cs` | `ad6d6342826330524454823b5e10aae4f03371ab` | `68528378974b417a0eeb88e89511b82226897d48` | Task 12.1 required-constructor fixture repair |

Thus 225 pre-existing dirty files remain byte- and status-identical. This
includes `Target`, `console.log(item))`, all four pre-existing presentation
files, formula documentation, UI/XAML, package/project, installer, publish and
release artifacts. The other six changed baseline paths have receipts and task
allow-lists. The workflow ledger does not, so the complete pre-existing user
delta set is not byte-protected.

## Complete added-path classification

All 166 current files absent from the Task 1 ledger fall into these exhaustive,
non-overlapping categories:

| Category | Count | Inventory rule / exact paths |
| --- | ---: | --- |
| Phase 3 evidence | 27 | every file under `docs/architecture-migration/evidence/phase-3-construction-state/` currently reported by Git, including Tasks 1-13, Task 9 recovery, Task 12.1, correction and model/runtime evidence |
| Raw executable evidence | 90 | every current file under `tests/SnowMeltingCalculator.Tests/TestResults/`; all are Phase 3, Task 12.1, correction, F2 or F3 named `.trx` outputs |
| Task 13 dossier/widget | 12 | `architecture-widget.html`, `architecture-model.json`, `characterization-tests.md`, `compile-time.md`, `di-runtime.md`, `persistence-compatibility.md`, `persistence.md`, `reactive.md`, `state-inventory.md`, `state-ownership.md`, `target-invariants.md`, `user-flow.md` |
| Phase 3 production | 19 | `ServiceCollectionExtensions.cs`; `ResultsViewModel.cs`; `MainViewModel.cs`; and the sixteen current files in `src/Services/Project/` added/changed after Task 1: `ConstructionDefaultStateInitializer.cs`, `ConstructionDefaults.cs`, `ConstructionLayerSnapshot.cs`, `ConstructionMutation.cs`, `ConstructionMutationOrigin.cs`, `ConstructionMutationResult.cs`, `ConstructionMutationStatus.cs`, `ConstructionPersistenceMapper.cs`, `ConstructionStateChangedEventArgs.cs`, `ConstructionStateProjection.cs`, `ConstructionStateSnapshot.cs`, `IProjectSession.cs`, `IProjectSessionConstructionState.cs`, `ProjectLoadOrchestrator.cs`, `ProjectSession.cs`, `ProjectSessionConstructionState.cs` |
| Phase 3 tests | 15 | `DiRegistrationTests.cs`; `ConstructionMultiplicityCharacterizationTests.cs`; `ConstructionViewModelEditorIntegrationTests.cs`; `MaterialImportTests.cs`; five `Services/Project` tests (`CanonicalDefaultConstructionLifecycleTests.cs`, `ConstructionStateLegacyStoreGuardTests.cs`, `ConstructionThermalInvalidationRegressionTests.cs`, `ProjectLifecycleFlowCharacterizationTests.cs`, `ProjectSessionConstructionStateTests.cs`); and six `ViewModels` tests/helpers (`MainViewModelTests.cs`, `ResetOrchestrationTests.cs`, both `ResultsStabilizationPhase1*` files, `ResultsViewModelOpenProjectTests.cs`, `ResultsViewModelTestHelpers.cs`) |
| Plan/workflow additions | 3 | `docs/architecture-migration/plans/pre-task-13-construction-thermal-invalidation-correction.md` (correction workflow), `docs/architecture-migration/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md` (not in a Phase 3 task allow-list), `.omo/boulder.json` (not in a Phase 3 task allow-list) |

Counts reconcile exactly: `27 + 90 + 12 + 19 + 15 + 3 = 166`.
The separately queued Phase 3.1 plan may be legitimate owner workflow, but it
is not attributable to Tasks 1-13 or the correction allow-list and therefore
cannot be waived by this strict Phase 3 F1 review. The same applies to
`.omo/boulder.json`.

## Task-to-allow-list map

| Work item | Path attribution and compliance result |
| --- | --- |
| Task 1 | Phase 3 baseline evidence and factual `TASK_CONTEXT.md`; matches Task 1 allow-list. |
| Task 2 | ownership guard/characterization test plus `task-2-writer-subscriber-inventory.md`; matches Construction guard-test/evidence allow-list. |
| Task 3 | Construction and directly affected lifecycle/round-trip characterization tests; matches test-only allow-list. |
| Task 4 | `IProjectSession.cs`, `ProjectSession.cs`, new Construction state contract/implementation files and direct state tests; matches foundation allow-list. |
| Task 5 | `ConstructionStateProjection.cs` and direct model/state/thermal compatibility coverage; formulas were preserved and no new formula path exists. |
| Task 6 | `ConstructionViewModel.cs`, direct VM tests and minimal helper changes; receipt records the adapter-only production boundary. |
| Task 7 | Construction VM application methods and template/material/editor integration tests; no repository redesign path was added. |
| Task 8 | `ProjectLoadOrchestrator.cs`, `MainViewModel.cs` and lifecycle/reset tests; matches reset/restore seam allow-list. |
| Task 9 | `ResultsViewModel.cs`, `ConstructionPersistenceMapper.cs`, direct persistence/round-trip tests and recovery evidence; no DTO schema/version path was added. |
| Task 10 | Construction state/projection completion and directly affected tests; the otherwise out-of-list `ConstructionViewModel.cs` cleanup is explicitly owner-authorized and recorded in the Task 10 blocker-resolution evidence. |
| Task 11 | `ServiceCollectionExtensions.cs`, DI and ownership tests; matches DI allow-list. |
| Task 12 | named raw logs/TRX/receipts and test-only repairs recorded with root causes; matches executable-gate allow-list. |
| Task 12.1 | default initializer, startup/lifecycle wiring and constructor-only test fixture repairs are recorded in its approved amendment receipts. |
| Pre-Task-13 correction | one `ProjectSessionConstructionState.CompleteChanged` projection notification, `ConstructionThermalInvalidationRegressionTests.cs`, correction evidence and raw outputs. The later parent F3 coverage correction changed only `ConstructionMultiplicityCharacterizationTests.cs`, `MaterialImportTests.cs`, and `ResultsViewModelOpenProjectTests.cs`, adding the mandatory standalone failure/import and field-complete second-load round-trip cases visible in current source; no formula/UI/schema/package/release path is attributed. |
| Task 13 | six maps, inventory/characterization/persistence/invariant maps, shared model, generated widget, evidence and `TASK_CONTEXT.md`; matches dossier allow-list. |

## Plan-copy and Must-NOT-Have checks

- Parent canonical and tracking plans are byte-identical: both are `35494`
  bytes with SHA-256
  `1C8C9588D89F1F926C977F0B22B69F638DF8C1F57524167DF489ED756A5DAED9`.
  There is no current checkbox-only divergence between those two copies.
- Correction copies are structurally identical after checkbox normalization.
  Canonical is `31720` bytes / SHA-256
  `D463D8E5639F659A2BDEDD5D69874A59902CB200A01F6CD30AB55BA200035903`;
  tracking is `31720` bytes / SHA-256
  `9B45A232039359250B8B9008344DC0463A6348EDA8DECB981C9DFD42814747A5`.
  Their only differences are the eight completed Todo/F1-F4 tracking
  checkboxes; the correction receipt records the owner-authorized history.
- No Phase 3-attributed formula documentation, UI/XAML design, package/project
  version, persistence DTO/schema, installer, publish or release artifact was
  added. All such Task 1 dirty paths retain their exact Task 1 blobs.
- No `ThermalState` or `HydraulicsState` ownership file is in the added Phase 3
  production set. The correction is the documented Construction projection
  notification only; the separate Climate ProjectLoad defect is not claimed
  fixed.
- No removed path, staged content, Git history mutation, or Task 13 product
  scope before its dossier task was found.

## Rejecting findings

1. `.omo/start-work/ledger.jsonl` changed from its Task 1 blob and has no Task
   1-13/correction allow-list or receipt attribution. Exact user/workflow hunk
   preservation therefore cannot be proven.
2. `.omo/boulder.json` is a current addition absent from the Task 1 ledger and
   outside every Phase 3 task allow-list.
3. `docs/architecture-migration/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md`
   is a current addition for a future queued phase, not a Task 1-13 or
   correction output. F1 cannot reclassify it as Phase 3 implementation scope.

F1 remains unchecked and workflow state must not advance on this receipt.

VERDICT: REJECT
