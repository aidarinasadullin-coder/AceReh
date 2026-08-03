---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T16:39:53.0409465Z
working_directory: D:/IA/ace v.2
commands:
  - dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --no-build --nologo --logger "trx;LogFileName=phase-0.trx" --results-directory "docs/architecture-migration/evidence/test-results"
  - PowerShell XML parse of docs/architecture-migration/evidence/test-results/phase-0.trx
exit_code: 0
status: pass
raw_output: docs/architecture-migration/evidence/test-baseline.log
limitations:
  - test-baseline.log is retained verbatim as captured raw command output. Its Cyrillic console text is mojibake after stream capture, so it is not used as the authoritative counter source.
  - TRX XML result elements are authoritative for outcomes. Its declared `Counters/@notExecuted` attribute is inconsistent with the three `NotExecuted` result elements; this is documented as a logger/adapter limitation, not a test failure.
  - A green test process does not establish preserved application runtime behavior.
---

# Test and TRX Baseline

## Result

| Field | Value |
| --- | --- |
| Command | `dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --no-build --nologo --logger "trx;LogFileName=phase-0.trx" --results-directory "docs/architecture-migration/evidence/test-results"` |
| Test process exit code | `0` |
| Duration | `33955 ms` |
| TRX path | `docs/architecture-migration/evidence/test-results/phase-0.trx` |
| Receipt status | pass |
| Raw command output | `test-baseline.log` |

The test command ran because the prescribed build receipt recorded exit code `0`. No retry was performed. The test process returned `0`; direct outcome-element parsing shows 1537 passed and zero failed tests.

## Authoritative TRX Parse

The TRX contains 1540 `UnitTestResult` elements and 1540 test definitions. Direct grouping of result elements is authoritative:

| Counter | Value |
| --- | ---: |
| `Passed` | 1537 |
| `NotExecuted` | 3 |
| `Failed` | 0 |
| Total `UnitTestResult` elements | 1540 |

The outcome elements reconcile exactly: `1540 = 1537 Passed + 3 NotExecuted`, with `0 Failed`. The three `NotExecuted` results have intentional NUnit semantics:

| Test | NotExecuted semantic |
| --- | --- |
| `RegenerateBaseline` | NUnit `[Test, Explicit]` |
| `RegenerateCircuitsBaseline` | NUnit `[Test, Explicit]` |
| `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` | `Assert.Ignore` |

The raw console summary reports 1537 passed, 1 skipped, and 1538 total because it excludes the two Explicit tests and counts only the `Assert.Ignore` test as skipped. It is therefore compatible with the result-element outcomes after this runner-specific reporting distinction.

The logger/adapter's declared `ResultSummary/Counters` attributes remain a limitation: `total=1540`, `passed=1537`, and `notExecuted=0`, even though the result elements contain three `NotExecuted` entries. This attribute discrepancy does not alter the authoritative outcome-element accounting.

| Reconciliation assertion | Result |
| --- | --- |
| TRX exists and parses as XML | pass |
| Result element count equals TRX `total` (`1540`) | pass |
| Authoritative outcomes reconcile: `1537 Passed + 3 NotExecuted = 1540` | pass |
| Failed outcome elements | pass (`0`) |
| Raw log contains observed textual figures `1537`, `1`, and `1538` | explained: console excludes two Explicit tests |
| Declared `Counters/@notExecuted=0` conflicts with three `NotExecuted` elements | limitation: logger/adapter attribute mismatch |

`test-baseline.log` is intentionally retained unchanged even though its Russian text is mojibake. Its visible completion summary reports 1537 passed, 0 failed, 1 skipped, and 1538 total. The difference from the 1540 TRX result elements is explained by the two Explicit tests excluded from the console total. The XML result elements, not damaged console encoding or the inconsistent Counters attribute, are authoritative for outcome classification.

## Dependent-Launch Control

The test command was launched only because the recorded build exit code was `0`. The failure scenario was verified in memory with the control predicate `if ($buildExitCode -eq 0)`: a nonzero synthetic exit code selects the branch that suppresses test launch. No real failure was induced and no file outside the allow-list was modified.

## UltraQA Probes

| Class | Applicability | Probe and result |
| --- | --- | --- |
| `dirty_worktree` | applicable | Post-run status/hash comparison against Todo 1 is recorded in the Todo 2 completion verification. |
| `stale_state` | applicable | TRX and raw log are bound to the current snapshot and exact command. |
| `hung/long commands` | applicable | The command completed within the 600000 ms timeout in `33955 ms`; no hang observed. |
| `flaky tests` | applicable | One prescribed run was captured without retry; outcome-element reconciliation is explained without treating the adapter limitation as flakiness. |
| `misleading_success_output` | applicable | Exit code `0` is corroborated by direct XML parsing: 1537 Passed, 3 intentional NotExecuted, and 0 Failed. |
| `concurrency` | N/A | The dependent command ran serially after build success. |
| `idempotency` | N/A | This is an execution-time evidence capture. |
| `resource_leak` | N/A | The test process exited normally. |
| `security` | N/A | No external trust boundary or credential is involved. |
| `performance` | N/A | Duration is reported without a performance claim. |
| `accessibility` | N/A | No UI is changed. |
| `localization` | N/A | The encoding limitation concerns captured tool output only; no localized product surface is changed. |

## Decision

Todo 2's test baseline is **passed**: the prescribed process exited `0`, and authoritative TRX result elements reconcile exactly to 1537 Passed, 3 intentional NotExecuted, and 0 Failed. The `Counters/@notExecuted=0` discrepancy is a documented logger/adapter limitation, not a test failure. Per the Phase 0 boundary, no source/test/configuration fix, retry, restoration, installation, or Git mutation was performed. Green tests do not by themselves prove preserved application runtime behavior.
