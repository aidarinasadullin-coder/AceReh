---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T16:00:20Z
working_directory: D:/IA/ace v.2
commands:
  - dotnet build "SnowMeltingCalculator.sln" -c Debug --nologo --no-incremental
exit_code: 0
status: pass
raw_output: docs/architecture-migration/evidence/build-baseline.log
limitations:
  - A green build establishes compilation for this execution-time working tree only; it does not establish preserved runtime behavior.
  - The command may restore implicitly and create normal ignored bin/ and obj/ side effects; none were deleted or restored.
---

# Debug Build Baseline

## Result

| Field | Value |
| --- | --- |
| Command | `dotnet build "SnowMeltingCalculator.sln" -c Debug --nologo --no-incremental` |
| Exit code | `0` |
| Status | pass |
| Duration | `5487 ms` |
| Projects built | `SnowMeltingCalculator`, `SnowMeltingCalculator.Tests` |
| Warnings | `0` |
| Errors | `0` |
| Raw output | `build-baseline.log` |

The prescribed command completed successfully. It implicitly evaluated restore state and generated normal ignored `bin/` and `obj/` side effects. No source, test, project, fixture, configuration, or Git change was made to resolve or mask a build result.

## QA Evidence

| Check | Result |
| --- | --- |
| Process exit code is zero | pass |
| Log exists and contains both solution project output paths | pass |
| Log reports zero warnings and zero errors | pass |
| Dependent test command is eligible to launch only after this success | pass |

## Failure-Control Verification

The launch condition is the captured process exit code: `if ($buildExitCode -eq 0) { launch prescribed test command }`. An in-memory control-flow check evaluated a nonzero value and confirmed that it selects the suppression branch. No real build failure was induced, no file was altered, and no dependent test would be launched for a nonzero build exit code.

## UltraQA Probes

| Class | Applicability | Probe and result |
| --- | --- | --- |
| `dirty_worktree` | applicable | Post-run integrity comparison is deferred to the combined Todo 2 verification; product paths are not permitted outputs. |
| `stale_state` | applicable | Receipt is bound to the live snapshot SHA and the exact command uses `--no-incremental`. |
| `hung/long commands` | applicable | Command completed within the 600000 ms timeout in `5487 ms`; no hang observed. |
| `misleading_success_output` | applicable | Status is based on process exit code `0` plus parsed zero-error log summary. |
| `flaky tests` | N/A | This receipt covers build only. |
| `concurrency` | N/A | Build precedes the dependent test serially. |
| `idempotency` | N/A | Build output is an allowed normal side effect, not a Todo 2 mutation contract. |
| `resource_leak` | N/A | Process exited normally. |
| `security` | N/A | No trust boundary or external credential is involved. |
| `performance` | N/A | Duration is recorded but no performance threshold is claimed. |
| `accessibility` | N/A | No UI is changed. |
| `localization` | N/A | No localized product surface is changed. |
