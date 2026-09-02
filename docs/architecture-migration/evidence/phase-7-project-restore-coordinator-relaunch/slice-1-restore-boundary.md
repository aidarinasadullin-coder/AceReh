# Phase 7 Slice 1: Restore Boundary

Status: PASS
Date: 2026-08-31

## Provenance

- Frozen plan: `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md`
- Plan SHA-256: `D403860BA03A52B96CACD43D993743A0D7B4E2B23F1F83DA7923E553A029E86A`
- Execution authorization: owner-issued `/architecture-start phase-7-project-restore-coordinator-relaunch` in the preceding session; authorization was not reopened.
- Baseline discipline: the worktree contained pre-existing user/control-plane changes. No unrelated dirty path was edited, staged, committed, reset, reverted, or cleaned.

## Boundary Evidence

- Canonical restore entrypoint: `src/ViewModels/Results/ResultsViewModel.cs`, `LoadProjectDataAsync` (lines 1616-1652) acquires the session restore lease and calls `ProjectLoadOrchestrator.RestoreModulesFromProjectAsync` (line 1637). The path is `LoadProjectFromPathAsync` -> `ApplyLoadedProjectAsync` -> `LoadProjectDataAsync` -> orchestrator restore.
- Canonical restore guard: `src/Services/Project/IProjectSession.cs`, `BeginProjectRestore`, implemented by `src/Services/Project/ProjectSession.cs`. `ProjectSession` owns `_restoreDepth` and `_isLoadProjectInProgress`; each call returns a distinct idempotent `ProjectRestoreLease`, and the guard clears only when the outermost lease is disposed.
- Four canonical slices: `ProjectSession` constructs and exposes exactly `ClimateState`, `ConstructionState`, `ThermalState`, and `HydraulicsState` through `IProjectSession`.
- Orchestrator ownership: `src/Services/Project/ProjectLoadOrchestrator.cs` receives the session and captures those four slice interfaces. It is the existing module restore boundary; no new coordinator was introduced.

## Negative Probe

The live symbol/call-path inspection checked `BeginProjectRestore`, `RestoreModulesFromProjectAsync`, restore-like entrypoints, and coordinator candidates. It found one lifecycle restore entrypoint and one session-owned guard. Slice-level `Restore` calls inside the orchestrator are state-slice operations, not alternate project restore boundaries. No second restore coordinator or direct bypass of `BeginProjectRestore` was identified.

The orchestrator currently invokes hydraulics slice `Restore` during restore and again during finalization. This is retained as an observed downstream calculation/publication concern for later frozen slices 3-4; it is not changed in Slice 1 because Slice 1 does not authorize calculation-path edits.

## Characterization Coverage

Existing tests already cover the Slice 1 contract, so no production or test source was changed:

- `ProjectSessionTests`: guard activation, nested leases in both disposal orders, idempotent disposal, subscriber-failure cleanup, stable canonical slice ownership, and DI identity.
- `ProjectLifecycleFlowCharacterizationTests`: successful load guard release, repeated load, restore through the orchestrator, and early/late restore failure guard release.
- `ProjectSessionLegacyStoreGuardTests`: no duplicate lifecycle backing fields in legacy state services.

## Executed Gates

Build was completed before the focused `--no-build` test command:

```text
dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo
Result: PASS, 0 warnings, 0 errors
```

```text
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests" --logger "trx;LogFileName=slice-1-restore-boundary.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"
Result: PASS, 38 passed, 0 failed, 0 skipped, 38 total
```

TRX: `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs/slice-1-restore-boundary.trx`

## Gate Decision

Slice 1 is PASS. The canonical restore boundary, session-owned guard, four canonical slices, and success/failure guard-release behavior are characterized without a production/test write. Todo 2 is released by the frozen plan, but is not executed as part of this Slice 1 checkpoint.

Residual risk remains intentionally recorded for later slices: restore validation-before-mutation, exactly-once calculation publication, and fresh report/UI source-of-truth proof are not established by this receipt.
