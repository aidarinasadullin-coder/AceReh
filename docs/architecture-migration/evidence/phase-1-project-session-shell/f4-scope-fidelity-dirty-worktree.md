# F4 — Scope Fidelity and Dirty-Worktree Audit

## Scope

Independent final-wave audit for `phase-1-project-session-shell`. Compare the live
working tree against the Task 1 NUL-safe baseline manifest, classify every record,
confirm the index is clean, confirm `CalculationContext.cs` is untouched, and verify
Phase 1 introduced only the allowed lifecycle-shell/test/evidence/map/widget/context
write-set with no forbidden formula/UI/package/SDK/installer/release/persistence/module-slice
change.

## Basis

- Repository root: `D:/IA/ace v.2` (verified by `git rev-parse --show-toplevel`).
- Branch: `master`, upstream `origin/master`, ahead 5 commits.
- HEAD: `021d4abd159aa71c4a19c7a6536851264e5a58ca`.
- Active plan: `docs/architecture-migration/plans/phase-1-project-session-shell.md`.
- Active plan SHA-256: `011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948`.
- F1, F2, F3 receipts in the same evidence directory each end with `VERDICT: APPROVE`;
  active plan checkboxes show F1-F3 checked and F4 unchecked/current.
- Task 1 baseline NUL-safe manifest:
  `docs/architecture-migration/evidence/phase-1-project-session-shell/baseline-git-status.bin`
  (14302 bytes, SHA-256 `45e4db912d274b91861304021c06421ab97a7639`).

## Commands and results

```bash
$env:GIT_MASTER='1'; git rev-parse --show-toplevel
```

Result: `D:/IA/ace v.2`.

```bash
$env:GIT_MASTER='1'; git diff --cached --stat
```

Result: no output; index is empty / no staged changes.

```bash
$env:GIT_MASTER='1'; git diff HEAD -- src/Core/CalculationContext.cs
```

Result: no output; `src/Core/CalculationContext.cs` is unchanged.

```bash
$env:GIT_MASTER='1'; git diff --check
```

Result: only CRLF conversion warnings for the pre-existing dirty worktree; one real
whitespace warning: `docs/architecture-migration/TASK_CONTEXT.md:945: trailing whitespace.`
This is in the owner-authorized Phase 1 context-update file and is not a scope blocker.

## NUL-safe manifest comparison

The current status stream was captured with the same command as the baseline:

```bash
$env:GIT_MASTER='1'; git status --porcelain=v1 -z --untracked-files=all
```

Current stream: 17379 bytes, 288 non-empty NUL-separated records,
SHA-256 `6ac79edd16cedb43ba75ec47db6df8a80bc14d39`.
Baseline stream: 14302 bytes, 247 non-empty NUL-separated records,
SHA-256 `45e4db912d274b91861304021c06421ab97a7639`.

Comparison symmetrically excludes only the Phase 1 evidence directory
`docs/architecture-migration/evidence/phase-1-project-session-shell/`:

| Class | Count |
| --- | --- |
| Protected baseline records (excluding Phase 1 evidence dir) | 246 |
| Protected records removed | 0 |
| Protected records changed status | 0 |
| Protected records unchanged | 246 |
| New records outside Phase 1 evidence dir | 23 |
| Phase 1 evidence records | 19 |

The 23 new records outside the Phase 1 evidence directory are all Phase 1
implementation/test/map/widget artifacts:

- `src/Services/Project/IProjectSession.cs` (Task 4)
- `src/Services/Project/ProjectSession.cs` (Task 4)
- `src/Services/Navigation/CalculationStateService.cs` (Task 6)
- `src/Services/Results/ProjectStateService.cs` (Task 5)
- `src/Configuration/ServiceCollectionExtensions.cs` (Task 7) — already dirty in baseline;
  status unchanged as ` M`
- `src/ViewModels/Results/ResultsViewModel.cs` (Task 6) — already dirty in baseline;
  status unchanged as ` M`
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs` (Task 2)
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionLegacyStoreGuardTests.cs` (Task 2)
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs` (Task 3)
- `tests/SnowMeltingCalculator.Tests/ViewModels/MainViewModelTests.cs` (Task 8)
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs` (Task 8)
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs` (Task 8)
- `docs/architecture-migration/maps/architecture-model.json` (Task 10)
- `docs/architecture-migration/maps/compile-time.md` (Task 10)
- `docs/architecture-migration/maps/di-runtime.md` (Task 10)
- `docs/architecture-migration/maps/state-ownership.md` (Task 10)
- `docs/architecture-migration/maps/reactive.md` (Task 10)
- `docs/architecture-migration/maps/persistence.md` (Task 10)
- `docs/architecture-migration/maps/user-flow.md` (Task 10)
- `docs/architecture-migration/maps/characterization-tests.md` (Task 10)
- `docs/architecture-migration/maps/state-inventory.md` (Task 10)
- `docs/architecture-migration/maps/persistence-compatibility.md` (Task 10)
- `docs/architecture-migration/maps/target-invariants.md` (Task 10)
- `docs/architecture-migration/widget/generate-widget.mjs` (Task 10)
- `docs/architecture-migration/architecture-widget.html` (Task 10)

Every changed tracked file retains its original baseline status (` M`);
no protected file was removed, had its status changed, was staged, reverted, or bundled.

The 19 Phase 1 evidence records are the expected receipts in
`docs/architecture-migration/evidence/phase-1-project-session-shell/`:

- `baseline-git-status.bin`, `baseline.md`, `baseline-build-debug.log`,
  `baseline-build-release.log`, `baseline-lifecycle-tests-debug.log`,
  `baseline-tests-release.log`, `post-baseline-git-status.bin`,
  `post-baseline-final-git-status.bin`, `final-git-status.bin`
- `tdd-owner-red.md`, `tdd-flows-red.md`, `project-session-contract.md`,
  `compatibility-adapters.md`, `restore-guard.md`, `di-runtime.md`,
  `lifecycle-user-flows.md`, `final-gates.md`, `dossier-update.md`
- `task10-model-v2-recheck.json`, `task10-runtime-v2-recheck.json`
- `f1-plan-compliance.md`, `f2-code-quality-architecture.md`, `f3-real-lifecycle-qa.md`

## Out-of-scope and forbidden-change scan

- Formula files: no new or modified formula source or `docs/Formulas_Snegotayanie.md`.
- UI/XAML: no new `.xaml` or `.xaml.cs` files added; pre-existing dirty XAML files retain
  baseline status with no status change.
- Packages/SDK: `src/SnowMeltingCalculator.csproj` and `tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj`
  remain in their baseline dirty state; no package reference or target framework change was introduced.
- Installer: `installer/SnowMeltingCalculator.iss` remains dirty with baseline status only.
- Release/publish artifacts: `publish/` and `build_temp/` remain dirty with baseline status only;
  no new release artifact was added by Phase 1.
- Persistence: `src/Services/Project/ProjectFileService.cs`, `src/Models/Project/ProjectData.cs`,
  `.smc` fixtures, and serialization code retain baseline status only; no schema/version-policy
  change, no transactional rollback, no `.bak` recovery logic.
- Module-slice migration: `ClimateState`, `ConstructionState`, `ThermalState`, `HydraulicsState`
  were not migrated into `ProjectSession`; `ProjectSession` contains only lifecycle/identity/path/dirty/restore-guard members.
- `CalculationContext`: `src/Core/CalculationContext.cs` diff is empty; no facade, replacement,
  constructor change, or new subscription introduced.

## Index and staged-change check

`git diff --cached --stat` produced no output. No Phase 1 or protected change is staged.

## Whitespace / diff-check

`git diff --check` emitted only CRLF-conversion warnings for the pre-existing dirty
worktree and a single trailing-whitespace warning on
`docs/architecture-migration/TASK_CONTEXT.md:945`. The trailing whitespace is in the
owner-authorized Phase 1 context-update file and is not a forbidden scope change.

## Observations

1. F1 evidence states 38 new records since baseline. The independent F4 recount
   finds 42 new records (23 implementation/test/map/widget + 19 Phase 1 evidence files).
   All 42 records map to documented Phase 1 tasks; the difference is an F1 count
   inconsistency, not an unexplained drift.
2. One trailing-whitespace warning exists in `TASK_CONTEXT.md`; it is not a scope blocker.

## Verdict

VERDICT: APPROVE
