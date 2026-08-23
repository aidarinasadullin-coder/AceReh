# Task 1 — Protected Phase 4 Baseline Receipt

Phase: `phase-4-thermal-state` · Plan SHA-256: `327B1288B8072E7A76D814F8439ECBC353F54F4CE59AB2B507595A08980B4C02` (frozen, canonical+mirror verified by V0)

## Environment

| Item | Value |
|---|---|
| Repository root | `D:\IA\3ace v.2` |
| Branch | `master` |
| HEAD (base) | `6a5a96f1763dd952c8d772ecd1d2536eb3b804cf` |
| .NET SDK | 8.0.418 |
| Node | v24.14.0 |
| PowerShell | 7.6.5 Core (`pwsh`) — installed by owner instruction before execution authorization |

## Gate results

| Gate | Command / check | Result | Artifact |
|---|---|---|---|
| G1 V0 state gate | `validate-state.mjs validate --check-plan` | exit 0, `valid=true` | — |
| G2 baseline capture ×2 byte-identical | `capture-baseline.ps1` | deterministic | `task-1/baseline-git-status.bin`, `baseline-manifest.json`, `baseline-index-sets.json`, `baseline-environment.json` |
| G2b determinism RE-VERIFIED after final script edits | two fresh runs into `task-1/determinism-check-a|b` | all four outputs identical A=B and equal to canonical | same names under `determinism-check-*` |
| G3 protected verifier pre/post | `verify-protected-baseline.ps1 -Baseline baseline-manifest.json -AllowedHunks todo-1-allowed-hunks.json -EvidenceRoot <ev> -Output …` | exit 0 both runs, `protected_mismatch_count=0`, `allowed_hunk_count=0`, drift=7 paths all classified `allowed` (evidence root) | `task-1/protected-pre.json`, `protected-post.json`; rerun copies `protected-rerun1/2.json` (current script versions) |
| G4 plan structure | `verify-plan-structure.ps1 -Plan plans/phase-4-thermal-state.md -Output task-1/plan-structure.json` | exit 0, ordered unique `1..14` + `F1..F4`, `v11_first_todo=11`, catalogVDefinitions=14, errors=0; rerun copy `plan-structure-rerun.json` | `task-1/plan-structure.json` |
| G5 four builds | src Debug / src Release / tests Debug / tests Release | all exit 0, 0 warnings, 0 errors | `task-1/logs/build-*.log`; durations 1158/4733/2815/2111 ms |
| G6 full suites | `dotnet test -c Debug\|Release --no-build` into distinct dirs | Debug exit 0: 1736 passed / 0 failed / 3 notExecuted (TRX identities 1739); Release exit 0: identical | `task-1/TestResults-debug/task-1-full-debug.trx`, `TestResults-release/task-1-full-release.trx`, console logs `logs/test-*-console.log` |
| G7 TRX reconciliation | `parse-trx.ps1` both TRX, compare exact identities/outcomes | `reconciled=true`, identitiesCompared=1739, mismatches=0 | `task-1/trx-debug.json`, `trx-release.json`, `trx-reconciliation.json` |
| G9 failure fixtures | isolated malformed inputs against all four verifiers | 23/23 fixtures exit nonzero as designed, canonical files untouched | `task-1/fixtures/**`, matrix `fixtures/out/fixture-matrix.json` |

## Accepted NotExecuted identities (baseline-known)

1. `SnowMeltingCalculator.Tests.RefactorBaseline.CircuitsBaselineTests.RegenerateCircuitsBaseline`
2. `SnowMeltingCalculator.Tests.RefactorBaseline.ThermalBaselineTests.RegenerateBaseline`
3. `SnowMeltingCalculator.Tests.ViewModels.ResultsViewModelOpenProjectTests.ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`

## Notes

- Baseline captured AFTER the workflow-owned dirty deltas (STATE.json execution transition, frozen plan materialization, planning receipt) — they are part of the baseline-relative delta and preserved.
- `todo-1-allowed-hunks.json` = empty manifest; `allowed_hunk_count=0` everywhere.
- Scripts were finalized (PS 5.1-invocation compatibility fix removing `||`) after the agent's original gate runs; ALL read-only gates (V0, protected ×2, plan structure) plus capture determinism were RE-RUN post-edit and remain green, so current script versions are proven.
- Completion manifest: `task-1/todo-1-completion.json` — **`todo2_unlocked=true`**.
