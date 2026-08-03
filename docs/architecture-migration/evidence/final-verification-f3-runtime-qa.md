---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: HEAD-plus-approved-dossier
generated_at_utc: 2026-07-31T07:22:07.4051559Z
working_directory: D:/IA/ace v.2
commands:
  - dotnet build "SnowMeltingCalculator.sln" -c Debug --nologo --no-incremental
  - dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --no-build --nologo --logger "trx;LogFileName=phase-0-f3.trx" --results-directory "docs/architecture-migration/evidence/test-results"
  - PowerShell XML parse of docs/architecture-migration/evidence/test-results/phase-0-f3.trx
  - PowerShell deterministic structural/reference/semantic validator extracted verbatim from evidence/model-validation.md
  - PowerShell UTF-8/NUL-safe git status and SHA-256 comparison against evidence/repository-snapshot.md
exit_code: 0
status: pass
raw_output: Inline observed command output and validation summaries below; the only generated runtime output is docs/architecture-migration/evidence/test-results/phase-0-f3.trx.
limitations:
  - The TRX logger's ResultSummary/Counters/@notExecuted is 0 although direct UnitTestResult outcomes contain 3 NotExecuted records; direct outcome elements are authoritative, as in the baseline receipt.
  - No installed full Draft 2020-12 JSON Schema validator is available. The rerun deterministic structural validator passed; full-schema validation remains degraded exactly as documented by model-validation.md.
  - A green build/test run verifies this execution-time test surface, not every interactive WPF user flow or behavior not covered by existing assertions.
---

# F3 Runtime QA Receipt

## Identity and Scope

| Field | Value |
| --- | --- |
| Final-verification lane | `F3. Real manual QA` |
| Bound snapshot / current HEAD | `f0d19c34ac03075d64548f1059e9c6626d3596b5` |
| Allowed outputs written by this lane | This receipt and `test-results/phase-0-f3.trx` only |
| Baseline artifacts retained read-only | `build-baseline.md`, `test-baseline.md`, and `test-results/phase-0.trx` |
| Normal command side effects | Ignored `src/bin`, `src/obj`, test `bin`, and test `obj` may be refreshed; they were neither deleted nor restored. |

## Prescribed Runtime Commands

The test was launched only after the prescribed build returned `0`. Neither command was retried.

| Command | Exit | Observed result |
| --- | ---: | --- |
| `dotnet build "SnowMeltingCalculator.sln" -c Debug --nologo --no-incremental` | `0` | Both `SnowMeltingCalculator` and `SnowMeltingCalculator.Tests` built; `0` warnings and `0` errors; elapsed `00:00:10.29`. |
| `dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --no-build --nologo --logger "trx;LogFileName=phase-0-f3.trx" --results-directory "docs/architecture-migration/evidence/test-results"` | `0` | Console: `0` failed, `1537` passed, `1` skipped, `1538` total, approximately `33 s`; emitted the allow-listed F3 TRX. |

The build output reported `All projects are up-to-date for restore`, the two expected Debug DLL output paths, and `Build succeeded`. The test output named three non-passing execution records: `RegenerateCircuitsBaseline`, `RegenerateBaseline`, and `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.

## TRX Parse and Baseline Comparison

`phase-0-f3.trx` parsed as XML. Direct `UnitTestResult` outcomes are the authoritative accounting source.

| Measurement | F3 | Baseline | Comparison |
| --- | ---: | ---: | --- |
| `UnitTestResult` elements | 1540 | 1540 | match |
| `UnitTest` definitions | 1540 | 1540 | match |
| `Passed` outcomes | 1537 | 1537 | match |
| `NotExecuted` outcomes | 3 | 3 | match |
| `Failed` outcomes | 0 | 0 | match |
| Outcome arithmetic | `1540 = 1537 + 3 + 0` | `1540 = 1537 + 3 + 0` | internally consistent |
| TRX `Counters/@total` | 1540 | 1540 | match |
| TRX `Counters/@notExecuted` | 0 | 0 | known logger/adapter limitation, not outcome authority |

The three `NotExecuted` outcomes match the baseline semantics: two NUnit `[Explicit]` regeneration tests and one `Assert.Ignore` test. The console excludes the two Explicit tests, explaining its `1538` total and one skipped test.

## Characterization Capability Presence

The capability matrix has only two `covered` rows. Each cited test name was present in the F3 run:

| Capability | Matrix-cited test | Present in F3 TRX | Result |
| --- | --- | --- | --- |
| `CF-011` reset | `Reset_RaisesSingleContextChangedEvent` | yes | pass |
| `CF-016` Markdown export | `ExportReportAsync_OperatingMode_CreatesNonEmptyMarkdownWithOperatingLabel` | yes | pass |

All remaining matrix rows retain their documented `partial` or `missing` status; this lane does not promote coverage based solely on a green test run.

## Canonical Model Structural Revalidation

The deterministic validator was extracted verbatim from [model-validation.md](model-validation.md) and executed read-only. It returned `result : pass` with the same model cardinalities as the baseline: 79 nodes, 112 edges, 112 edge-semantics records, 27 state records, 22 flows, 11 evidence records, and 5 coverage records. All 14 in-memory negative probes were rejected, and all six map-filter sets passed.

Full Draft 2020-12 validation remains `degraded` only because `jsonschema`, `ajv`, and `check-jsonschema` are absent; no tool was installed. This does not constitute a structural-model failure.

## Dirty-Worktree Preservation

Post-command PowerShell comparison used UTF-8/NUL-safe `git -c core.quotepath=false status --porcelain=v1 -z --untracked-files=all` and `Get-FileHash -Algorithm SHA256` against the Todo 1 ledger.

| Assertion | Result |
| --- | --- |
| Current HEAD equals bound snapshot | pass |
| 15 present pre-existing non-dossier ledger paths retain their exact SHA-256 | pass |
| 2 pre-existing deleted tracked paths remain absent | pass |
| 17 non-dossier baseline status records remain present with their original status | pass |
| No source or test status/hash changed after the F3 commands | pass |
| No baseline TRX was overwritten | pass; `phase-0.trx` was read-only input and `phase-0-f3.trx` is distinct |

## Assertions and Verdict

| # | Assertion | Result |
| ---: | --- | --- |
| 1 | Bound HEAD equals snapshot SHA | pass |
| 2 | Prescribed Debug build exits `0` | pass |
| 3 | Prescribed test exits `0` after successful build | pass |
| 4 | F3 TRX XML parses with 1540 results and 1540 definitions | pass |
| 5 | F3 TRX outcomes reconcile and have zero failed tests | pass |
| 6 | F3 outcome totals match the baseline receipt | pass |
| 7 | Every documented `covered` capability cites a test present in F3 TRX | pass (`2/2`) |
| 8 | Canonical deterministic model structural validation passes | pass |
| 9 | 15 protected present hashes remain exact | pass |
| 10 | 2 protected deleted states remain exact | pass |
| 11 | 17 protected non-dossier status records remain preserved | pass |

**Assertion count:** 11 passed, 0 failed.

**Defects:** none.

**Terminal verdict: APPROVE**
