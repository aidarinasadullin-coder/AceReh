# F1 Plan Compliance Audit - phase-1-project-session-shell

## Scope

Independent plan compliance audit for `phase-1-project-session-shell`. Maps every
Phase 1 implementation artifact to Tasks 1-10, verifies evidence receipts,
confirms the nested restore lease correction, and distinguishes protected
pre-existing dirty paths from Phase 1 write-set.

## Basis

- Repository root: `D:/IA/ace v.2` (verified by `git rev-parse --show-toplevel`).
- Branch: `master`, upstream `origin/master`, ahead 5 commits.
- HEAD: `021d4abd159aa71c4a19c7a6536851264e5a58ca`.
- Active plan: `docs/architecture-migration/plans/phase-1-project-session-shell.md`.
- Active plan SHA-256: `011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948`.
- Plan checkboxes: Tasks 1-10 are checked; F1-F4 are unchecked (F1 is the current gate).
- Basis is the dirty working tree at HEAD; Phase 1 is not committed.

## Verification commands run this audit

```bash
$env:GIT_MASTER='1'; git rev-parse --show-toplevel
```

Result: `D:/IA/ace v.2`.

```bash
$env:GIT_MASTER='1'; git rev-parse HEAD
```

Result: `021d4abd159aa71c4a19c7a6536851264e5a58ca`.

```bash
$env:GIT_MASTER='1'; git status
```

Result: on `master`, ahead of `origin/master` by 5 commits; large pre-existing dirty worktree plus Phase 1 untracked/modified artifacts.

```bash
dotnet build src/SnowMeltingCalculator.csproj -c Debug
```

Result: 0 warnings, 0 errors.

```bash
dotnet build src/SnowMeltingCalculator.csproj -c Release
```

Result: 0 warnings, 0 errors.

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectStateServiceTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests|FullyQualifiedName~CalculationStateServiceGuardTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests"
```

Result: 49 passed, 0 failed, 0 skipped.

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~MainViewModelTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~CircuitsViewModelEventLeakTests|FullyQualifiedName~ResultsStabilizationPhase1|FullyQualifiedName~DoubleCalculationPreventionTests"
```

Result: 100 passed, 0 failed, 1 pre-existing skipped.

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ProjectFileServiceAtomicityTests|FullyQualifiedName~ProjectFileServiceMutationTests"
```

Result: 18 passed, 0 failed, 0 skipped.

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release
```

Result: 1568 passed, 0 failed, 1 pre-existing skipped.

```bash
node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output <temp>
```

Result: PASS, 33 assertions, 21 mutations.

```bash
node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output <temp>
```

Result: PASS, 47 assertions, 20 mutations.

```bash
node docs/architecture-migration/widget/generate-widget.mjs --check
```

Result: 14/14 checks PASS; canonical SHA-256 `9C5188BAC257D9CAC51045C0D2186D03A4A2E6B92AFFBF0519B3B3737BBCED9F`.

## Task-to-artifact mapping

| Task | Plan clause | Implementation artifacts | Evidence receipt |
|------|-------------|--------------------------|------------------|
| 1 | Capture live baseline; protected dirty manifest | `docs/architecture-migration/evidence/phase-1-project-session-shell/baseline.md`, `baseline-git-status.bin`, `post-baseline-git-status.bin`, `post-baseline-final-git-status.bin`, `final-git-status.bin`, `baseline-build-debug.log`, `baseline-build-release.log`, `baseline-lifecycle-tests-debug.log`, `baseline-tests-release.log` | `baseline.md` |
| 2 | Add RED lifecycle-owner/contract tests | `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs`, `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionLegacyStoreGuardTests.cs` | `tdd-owner-red.md` |
| 3 | Add RED lifecycle-flow/failure characterization | `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs` | `tdd-flows-red.md` |
| 4 | Introduce `IProjectSession`/`ProjectSession` | `src/Services/Project/IProjectSession.cs`, `src/Services/Project/ProjectSession.cs`, `src/Configuration/ServiceCollectionExtensions.cs` (singleton registration), `ProjectLifecycleFlowCharacterizationTests.cs` (helper signature fix) | `project-session-contract.md` |
| 5 | Convert legacy contracts to forwarding-only | `src/Services/Results/ProjectStateService.cs` | `compatibility-adapters.md` |
| 6 | Centralize restore guard | `src/Services/Navigation/CalculationStateService.cs`, `src/ViewModels/Results/ResultsViewModel.cs`, `ProjectStateService.cs` (`Session` exposure), test helper adjustments | `restore-guard.md` |
| 7 | Register singleton DI graph | `src/Configuration/ServiceCollectionExtensions.cs`, `src/ViewModels/Results/ResultsViewModel.cs`, `src/Services/Navigation/CalculationStateService.cs`, `ProjectSessionTests.cs` (DI test) | `di-runtime.md` |
| 8 | Preserve lifecycle flows and `.smc` compatibility | `ProjectLifecycleFlowCharacterizationTests.cs` (now GREEN against implementation) | `lifecycle-user-flows.md` |
| 9 | Full gates and single-owner invariant audit | All production/test artifacts; source inspection evidence | `final-gates.md` |
| 10 | Update dossier/widget | `docs/architecture-migration/maps/architecture-model.json`, six filtered views, `widget/generate-widget.mjs`, `architecture-widget.html`, `TASK_CONTEXT.md` | `dossier-update.md` |

## Nested restore lease correction verification

- **Previous blocker:** `ProjectSession.BeginProjectRestore()` returned a shared `_currentLease`; nested scopes disposing inner and outer leases could leave `_restoreDepth == 1` and `IsLoadProjectInProgress == true`.
- **Current implementation verified by source inspection:**
  - `src/Services/Project/ProjectSession.cs` contains no `_currentLease` field.
  - `BeginProjectRestore()` returns a new `ProjectRestoreLease(this)` for every successful call.
  - Each lease has its own `_disposed` flag and calls `EndRestore()` at most once.
- **Regression tests verified:** `ProjectSessionTests.cs` includes:
  - `BeginProjectRestore_NestedLeases_DisposeInnerThenOuter_PreservesGuardUntilFinalExit`
  - `BeginProjectRestore_NestedLeases_DisposeOuterThenInner_PreservesGuardUntilFinalExit`
  - `BeginProjectRestore_NestedLeases_FinalExitSubscriberThrows_ClearsGuardAndKeepsLeasesIdempotent`
- **Post-correction re-check:** `dossier-update.md` records targeted 43/43, affected 83/84 (1 pre-existing skip), persistence 18/18, Debug/Release builds 0 errors, full Release 1568/1569 (1 pre-existing skip), model-v2 33/21, runtime-v2 47/20, widget check 14/14.
- This audit re-ran the same gates and obtained identical results.

## Protected dirty path classification

- Task 1 baseline `baseline-git-status.bin` records 247 non-empty status records: 246 protected pre-existing records (216 tracked modified, 2 tracked deleted, 28 untracked) plus 1 Phase 1 evidence record.
- Comparison of `baseline-git-status.bin` against current `git status --porcelain=v1 -z` shows:
  - **0** protected records removed.
  - **0** protected records changed status.
  - **38** new records since baseline, all mapping to Phase 1 tasks above.
- Phase 1 write-set is therefore limited to the allow-listed artifacts. All other dirty paths are protected pre-existing baseline paths and were not overwritten by Phase 1.

## Out-of-scope confirmation

- `CalculationContext.cs` is unchanged (no diff).
- No Climate/Construction/Thermal/Hydraulics ownership migration.
- No UI/XAML, package, SDK, formula, installer, release, transactional rollback, or `.smc` schema change was introduced by Phase 1.
- The `.gitignore`, `src/SnowMeltingCalculator.csproj`, `installer/SnowMeltingCalculator.iss`, and other non-allow-listed files remain dirty from pre-existing baseline only; their diffs are the same protected baseline state recorded in `baseline-git-status.bin`.

## Findings

- **Positive:** All Tasks 1-10 have a corresponding implementation artifact and evidence receipt.
- **Positive:** Nested restore lease correction is implemented correctly and covered by regression tests.
- **Positive:** All required gates pass in this audit.
- **Observation:** `final-gates.md` (Task 9 evidence captured before the correction) still contains the stale sentence referencing `_currentLease` in `ProjectSession.cs`. The authoritative post-correction narrative is in `dossier-update.md` (Task 10 re-check) and the `TASK_CONTEXT.md` journal entry dated 2026-08-05. Current source inspection confirms `_currentLease` is absent. This is a documentation cleanup item, not a compliance blocker.

## Verdict

VERDICT: APPROVE
